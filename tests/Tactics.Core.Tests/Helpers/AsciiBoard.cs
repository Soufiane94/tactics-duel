using System.Collections.Immutable;
using Tactics.Core.Model;

namespace Tactics.Core.Tests.Helpers;

/// <summary>
/// Builds a <see cref="GameState"/> from a text picture, so a rule test shows the board it tests.
/// Legend (PLAN.md, section 1): uppercase = Player One, lowercase = Player Two, '#' = obstacle, '.' = empty.
/// W/w warrior, A/a archer, M/m mage, H/h healer. Cells are separated by spaces.
/// The "x: 0 1 2 …" header line and the "y=N" row labels are optional, so the picture from PLAN.md works as is.
/// </summary>
public static class AsciiBoard
{
    /// <summary>
    /// Fixed test stats: the placeholder values from PLAN.md, section 1. They are NOT read from
    /// content/units.json on purpose, so balancing the real game never breaks a rule test.
    /// </summary>
    public static ImmutableDictionary<string, UnitType> DefaultUnitTypes { get; } = new[]
    {
        new UnitType("warrior", "Warrior", MaxHp: 28, Power: 10, Defense: 3, Move: 3, MinRange: 1, MaxRange: 1, Ability.Damage),
        new UnitType("archer", "Archer", MaxHp: 18, Power: 8, Defense: 1, Move: 3, MinRange: 2, MaxRange: 3, Ability.Damage),
        new UnitType("mage", "Mage", MaxHp: 14, Power: 12, Defense: 0, Move: 2, MinRange: 1, MaxRange: 2, Ability.Damage),
        new UnitType("healer", "Healer", MaxHp: 16, Power: 5, Defense: 1, Move: 3, MinRange: 1, MaxRange: 2, Ability.Heal),
    }.ToImmutableDictionary(type => type.Id);

    private static readonly Dictionary<char, string> TypeIdByLetter = new()
    {
        ['w'] = "warrior",
        ['a'] = "archer",
        ['m'] = "mage",
        ['h'] = "healer",
    };

    /// <summary>
    /// Parses the picture into the initial state of a game: turn 1, Player One to play, everyone at full HP.
    /// Unit ids follow the real content: "one-warrior", then "one-warrior-2" for a second one, in reading order.
    /// </summary>
    public static GameState Parse(string map)
    {
        var rows = map.Split('\n')
            .Select(line => line.Trim()) // also removes the '\r' of Windows line endings
            .Where(line => line.Length > 0 && !line.StartsWith("x:", StringComparison.Ordinal))
            .Select(WithoutRowLabel)
            .Select(line => line.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .ToList();

        if (rows.Count == 0)
        {
            throw new ArgumentException("The map has no rows.", nameof(map));
        }

        var width = rows[0].Length;
        var obstacles = ImmutableHashSet.CreateBuilder<Position>();
        var placements = ImmutableList.CreateBuilder<UnitPlacement>();
        var countById = new Dictionary<string, int>();

        for (var y = 0; y < rows.Count; y++)
        {
            if (rows[y].Length != width)
            {
                throw new ArgumentException($"Row y={y} has {rows[y].Length} cells; row y=0 has {width}.", nameof(map));
            }

            for (var x = 0; x < width; x++)
            {
                var cell = rows[y][x];
                if (cell.Length != 1)
                {
                    throw new ArgumentException($"Cell \"{cell}\" at ({x}, {y}) should be one character.", nameof(map));
                }

                var symbol = cell[0];
                if (symbol == '.')
                {
                    continue;
                }

                if (symbol == '#')
                {
                    obstacles.Add(new Position(x, y));
                    continue;
                }

                if (!TypeIdByLetter.TryGetValue(char.ToLowerInvariant(symbol), out var typeId))
                {
                    throw new ArgumentException($"Unknown symbol '{symbol}' at ({x}, {y}).", nameof(map));
                }

                var owner = char.IsUpper(symbol) ? PlayerSide.One : PlayerSide.Two;
                var baseId = $"{(owner == PlayerSide.One ? "one" : "two")}-{typeId}";
                var count = countById.GetValueOrDefault(baseId) + 1;
                countById[baseId] = count;
                var id = count == 1 ? baseId : $"{baseId}-{count}";

                placements.Add(new UnitPlacement(id, typeId, owner, new Position(x, y)));
            }
        }

        var board = new Board(width, rows.Count, obstacles.ToImmutable());
        var setup = new GameSetup("ascii", board, DefaultUnitTypes, placements.ToImmutable());
        return GameEngine.CreateInitialState(setup);
    }

    // "y=0     . h . m" → ". h . m"
    private static string WithoutRowLabel(string line)
    {
        if (!line.StartsWith("y=", StringComparison.Ordinal))
        {
            return line;
        }

        var firstSpace = line.IndexOf(' ', StringComparison.Ordinal);
        return firstSpace < 0 ? "" : line[(firstSpace + 1)..];
    }
}
