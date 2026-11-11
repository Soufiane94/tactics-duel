using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.Json;
using Tactics.Core.Model;
using Tactics.Core.Serialization;

namespace Tactics.Core.Content;

/// <summary>
/// Turns the two hand-edited JSON files into a validated <see cref="GameSetup"/>.
/// It takes JSON text, not file paths, so Core stays free of I/O: the API and the tests read the files.
/// </summary>
public static class ContentLoader
{
    private const string UnitsLabel = "Units file";

    /// <summary>
    /// Parses and validates both files. Throws one <see cref="ContentException"/> that lists every problem,
    /// so you can fix them all in one go.
    /// </summary>
    /// <param name="unitsJson">The text of <c>content/units.json</c>.</param>
    /// <param name="scenarioJson">The text of one scenario, e.g. <c>content/scenarios/classic.json</c>.</param>
    public static GameSetup Load(string unitsJson, string scenarioJson)
    {
        ArgumentNullException.ThrowIfNull(unitsJson);
        ArgumentNullException.ThrowIfNull(scenarioJson);

        var errors = new List<ContentError>();
        var unitsFile = Parse<UnitsFile>(unitsJson, UnitsLabel, errors);
        var scenarioFile = Parse<ScenarioFile>(scenarioJson, "Scenario file", errors);
        if (unitsFile is null || scenarioFile is null)
        {
            // We can't check the content of a file we can't read.
            throw new ContentException(errors);
        }

        var (unitTypes, declaredTypeIds) = ReadUnitTypes(unitsFile, errors);
        var (scenarioId, board, placements) = ReadScenario(scenarioFile, unitTypes, declaredTypeIds, errors);

        if (errors.Count > 0)
        {
            throw new ContentException(errors);
        }

        // With no errors, the board was read successfully.
        return new GameSetup(scenarioId, board ?? throw new UnreachableException(), unitTypes, placements);
    }

    // ---- JSON parsing -------------------------------------------------------------------------

    private static T? Parse<T>(string json, string fileLabel, List<ContentError> errors)
        where T : class
    {
        try
        {
            var file = JsonSerializer.Deserialize<T>(json, TacticsJson.Options);
            if (file is null)
            {
                errors.Add(new(ContentErrorCode.InvalidJson, $"{fileLabel}: the file contains only null."));
            }

            return file;
        }
        catch (JsonException ex)
        {
            errors.Add(new(ContentErrorCode.InvalidJson, $"{fileLabel}: {DescribeJsonError(ex)}"));
            return null;
        }
    }

    // Turns a System.Text.Json error into a sentence with a 1-based line and column.
    private static string DescribeJsonError(JsonException ex)
    {
        var where = ex.LineNumber is { } line
            ? $"line {line + 1}" + (ex.BytePositionInLine is { } column ? $", column {column + 1}" : "")
            : "an unknown position";
        var path = ex.Path is { Length: > 1 } p ? $" (at {p})" : ""; // "$" alone is the root: not useful
        var reason = ex.Message.Contains("could not be converted", StringComparison.Ordinal)
            ? "this value has the wrong type, for example text where a whole number is expected."
            : WithoutPosition(ex.Message);
        return $"invalid JSON at {where}{path}: {reason}";
    }

    // System.Text.Json appends " Path: … | LineNumber: … | BytePositionInLine: …". We already report those.
    private static string WithoutPosition(string message)
    {
        var cut = message.IndexOf(" Path:", StringComparison.Ordinal);
        if (cut < 0)
        {
            cut = message.IndexOf(" LineNumber:", StringComparison.Ordinal);
        }

        return (cut < 0 ? message : message[..cut]).Trim();
    }

    // ---- Units file ---------------------------------------------------------------------------

    private static (ImmutableDictionary<string, UnitType> Valid, ImmutableHashSet<string> Declared) ReadUnitTypes(
        UnitsFile file, List<ContentError> errors)
    {
        var valid = ImmutableDictionary.CreateBuilder<string, UnitType>(StringComparer.Ordinal);

        // Every id the file declares, even for types with errors. A placement that uses a broken type then
        // gets no extra "unknown unit type" error: you only see the real problem.
        var declared = ImmutableHashSet.CreateBuilder<string>(StringComparer.Ordinal);

        if (file.UnitTypes is null)
        {
            errors.Add(Missing(UnitsLabel, "unitTypes", "a list of unit types"));
            return (valid.ToImmutable(), declared.ToImmutable());
        }

        for (var i = 0; i < file.UnitTypes.Count; i++)
        {
            var entry = file.UnitTypes[i];
            var label = $"{UnitsLabel}: unit type #{i + 1}";
            if (entry is null)
            {
                errors.Add(new(ContentErrorCode.MissingField, $"{label} is empty."));
                continue;
            }

            if (string.IsNullOrWhiteSpace(entry.Id))
            {
                errors.Add(new(ContentErrorCode.MissingField, $"{label} has no \"id\"."));
                continue;
            }

            if (!declared.Add(entry.Id))
            {
                errors.Add(new(
                    ContentErrorCode.DuplicateId,
                    $"{UnitsLabel}: two unit types use the id \"{entry.Id}\". Unit type ids must be unique."));
                continue;
            }

            label = $"{UnitsLabel}: unit type \"{entry.Id}\"";
            var name = RequireText(entry.Name, label, "name", errors);
            var maxHp = RequireAtLeast(entry.MaxHp, 1, label, "maxHp", ContentErrorCode.InvalidStat, errors);
            var power = RequireAtLeast(entry.Power, 1, label, "power", ContentErrorCode.InvalidStat, errors);
            var defense = RequireAtLeast(entry.Defense, 0, label, "defense", ContentErrorCode.InvalidStat, errors);
            var move = RequireAtLeast(entry.Move, 1, label, "move", ContentErrorCode.InvalidStat, errors);
            var minRange = RequireAtLeast(entry.MinRange, 1, label, "minRange", ContentErrorCode.InvalidStat, errors);
            var maxRange = RequireAtLeast(entry.MaxRange, 1, label, "maxRange", ContentErrorCode.InvalidStat, errors);
            var ability = RequireEnum<Ability>(entry.Ability, label, "ability", errors);

            if (minRange is { } min && maxRange is { } max && min > max)
            {
                errors.Add(new(
                    ContentErrorCode.RangeOrder,
                    $"{label}: \"minRange\" ({min}) is greater than \"maxRange\" ({max})."));
            }

            if (name is not null && maxHp is { } hp && power is { } pw && defense is { } df && move is { } mv
                && minRange is { } mn && maxRange is { } mx && mn <= mx && ability is { } ab)
            {
                valid.Add(entry.Id, new UnitType(entry.Id, name, hp, pw, df, mv, mn, mx, ab));
            }
        }

        return (valid.ToImmutable(), declared.ToImmutable());
    }

    // ---- Scenario file ------------------------------------------------------------------------

    private static (string Id, Board? Board, ImmutableList<UnitPlacement> Placements) ReadScenario(
        ScenarioFile file,
        ImmutableDictionary<string, UnitType> unitTypes,
        ImmutableHashSet<string> declaredTypeIds,
        List<ContentError> errors)
    {
        string label;
        if (string.IsNullOrWhiteSpace(file.Id))
        {
            label = "Scenario file";
            errors.Add(Missing(label, "id", "a short name like \"classic\""));
        }
        else
        {
            label = $"Scenario \"{file.Id}\"";
        }

        var board = ReadBoard(file.Board, label, errors);
        var placements = ReadPlacements(file.Placements, label, board, unitTypes, declaredTypeIds, errors);
        return (file.Id ?? "", board, placements);
    }

    private static Board? ReadBoard(BoardEntry? entry, string scenarioLabel, List<ContentError> errors)
    {
        if (entry is null)
        {
            errors.Add(Missing(scenarioLabel, "board", "its width, height and obstacles"));
            return null;
        }

        var label = $"{scenarioLabel}: board";
        var width = RequireAtLeast(entry.Width, 1, label, "width", ContentErrorCode.InvalidValue, errors);
        var height = RequireAtLeast(entry.Height, 1, label, "height", ContentErrorCode.InvalidValue, errors);
        if (width is not { } w || height is not { } h)
        {
            return null;
        }

        var board = new Board(w, h, []);
        var obstacles = ImmutableHashSet.CreateBuilder<Position>();
        var entries = entry.Obstacles ?? []; // no "obstacles" field = an open board
        for (var i = 0; i < entries.Count; i++)
        {
            var obstacle = entries[i];
            var obstacleLabel = $"{scenarioLabel}: obstacle #{i + 1}";
            if (obstacle is null)
            {
                errors.Add(new(ContentErrorCode.MissingField, $"{obstacleLabel} is empty."));
                continue;
            }

            var x = RequireNumber(obstacle.X, obstacleLabel, "x", errors);
            var y = RequireNumber(obstacle.Y, obstacleLabel, "y", errors);
            if (x is not { } ox || y is not { } oy)
            {
                continue;
            }

            var position = new Position(ox, oy);
            if (board.Contains(position))
            {
                obstacles.Add(position);
            }
            else
            {
                errors.Add(new(
                    ContentErrorCode.OffBoard,
                    $"{scenarioLabel}: obstacle at {position} is off the board ({DescribeBounds(board)})."));
            }
        }

        return board with { Obstacles = obstacles.ToImmutable() };
    }

    private static ImmutableList<UnitPlacement> ReadPlacements(
        IReadOnlyList<PlacementEntry?>? entries,
        string scenarioLabel,
        Board? board,
        ImmutableDictionary<string, UnitType> unitTypes,
        ImmutableHashSet<string> declaredTypeIds,
        List<ContentError> errors)
    {
        var placements = ImmutableList.CreateBuilder<UnitPlacement>();
        if (entries is null)
        {
            errors.Add(Missing(scenarioLabel, "placements", "the list of units and their starting tiles"));
            return placements.ToImmutable();
        }

        var ids = new HashSet<string>(StringComparer.Ordinal);
        var occupiedBy = new Dictionary<Position, string>(); // tile → id of the unit already there
        var sidesWithUnits = new HashSet<PlayerSide>();

        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            var label = $"{scenarioLabel}: placement #{i + 1}";
            if (entry is null)
            {
                errors.Add(new(ContentErrorCode.MissingField, $"{label} is empty."));
                continue;
            }

            if (string.IsNullOrWhiteSpace(entry.Id))
            {
                errors.Add(new(ContentErrorCode.MissingField, $"{label} has no \"id\"."));
                continue;
            }

            if (!ids.Add(entry.Id))
            {
                errors.Add(new(
                    ContentErrorCode.DuplicateId,
                    $"{scenarioLabel}: two placements use the id \"{entry.Id}\". Unit ids must be unique."));
                continue;
            }

            label = $"{scenarioLabel}: placement \"{entry.Id}\"";
            var typeId = RequireText(entry.TypeId, label, "typeId", errors);
            if (typeId is not null && !declaredTypeIds.Contains(typeId))
            {
                errors.Add(new(ContentErrorCode.UnknownUnitType, $"{label} uses unknown unit type \"{typeId}\". {KnownTypes(declaredTypeIds)}"));
                typeId = null;
            }

            var owner = RequireEnum<PlayerSide>(entry.Owner, label, "owner", errors);
            if (owner is { } side)
            {
                sidesWithUnits.Add(side);
            }

            var x = RequireNumber(entry.X, label, "x", errors);
            var y = RequireNumber(entry.Y, label, "y", errors);
            if (x is not { } px || y is not { } py)
            {
                continue;
            }

            var position = new Position(px, py);
            if (board is not null && !board.Contains(position))
            {
                errors.Add(new(ContentErrorCode.OffBoard, $"{label} at {position} is off the board ({DescribeBounds(board)})."));
                continue;
            }

            if (board is not null && board.IsObstacle(position))
            {
                errors.Add(new(ContentErrorCode.OnObstacle, $"{label} at {position} is on an obstacle."));
                continue;
            }

            if (occupiedBy.TryGetValue(position, out var otherId))
            {
                errors.Add(new(
                    ContentErrorCode.Overlap,
                    $"{scenarioLabel}: placements \"{otherId}\" and \"{entry.Id}\" are both at {position}."));
                continue;
            }

            occupiedBy[position] = entry.Id;
            if (typeId is not null && unitTypes.ContainsKey(typeId) && owner is { } validOwner)
            {
                placements.Add(new UnitPlacement(entry.Id, typeId, validOwner, position));
            }
        }

        foreach (var side in Enum.GetValues<PlayerSide>())
        {
            if (!sidesWithUnits.Contains(side))
            {
                errors.Add(new(
                    ContentErrorCode.EmptySide,
                    $"{scenarioLabel}: player \"{JsonName(side)}\" has no units. Each side needs at least one."));
            }
        }

        return placements.ToImmutable();
    }

    // ---- Small helpers ------------------------------------------------------------------------

    private static ContentError Missing(string label, string field, string? hint = null) =>
        new(ContentErrorCode.MissingField, $"{label} is missing \"{field}\"" + (hint is null ? "." : $" ({hint})."));

    private static string? RequireText(string? value, string label, string field, List<ContentError> errors)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        errors.Add(Missing(label, field));
        return null;
    }

    private static int? RequireNumber(int? value, string label, string field, List<ContentError> errors)
    {
        if (value is null)
        {
            errors.Add(Missing(label, field));
        }

        return value;
    }

    private static int? RequireAtLeast(
        int? value, int minimum, string label, string field, ContentErrorCode code, List<ContentError> errors)
    {
        if (value is not { } number)
        {
            errors.Add(Missing(label, field));
            return null;
        }

        if (number < minimum)
        {
            errors.Add(new(code, $"{label}: \"{field}\" must be at least {minimum} (it is {number})."));
            return null;
        }

        return number;
    }

    // Accepts the same names as the JSON enums ("one", "damage"), ignoring case.
    private static TEnum? RequireEnum<TEnum>(string? text, string label, string field, List<ContentError> errors)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            errors.Add(Missing(label, field));
            return null;
        }

        foreach (var value in Enum.GetValues<TEnum>())
        {
            if (string.Equals(JsonName(value), text, StringComparison.OrdinalIgnoreCase))
            {
                return value;
            }
        }

        var allowed = string.Join(" or ", Enum.GetValues<TEnum>().Select(v => $"\"{JsonName(v)}\""));
        errors.Add(new(ContentErrorCode.InvalidValue, $"{label}: \"{field}\" must be {allowed} (it is \"{text}\")."));
        return null;
    }

    private static string JsonName<TEnum>(TEnum value)
        where TEnum : struct, Enum =>
        JsonNamingPolicy.CamelCase.ConvertName(value.ToString());

    private static string KnownTypes(ImmutableHashSet<string> declaredTypeIds) =>
        declaredTypeIds.IsEmpty
            ? "The units file declares no unit types."
            : $"Known types: {string.Join(", ", declaredTypeIds.Order(StringComparer.Ordinal))}.";

    private static string DescribeBounds(Board board) =>
        $"the board is {board.Width}×{board.Height}: x goes from 0 to {board.Width - 1}, y from 0 to {board.Height - 1}";
}
