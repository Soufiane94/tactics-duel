namespace Tactics.Core.Content;

// The JSON files exactly as written by hand: the "file DTOs".
// Every field is nullable, so a missing value becomes a clear "missing" error instead of a silent 0.
// Enums stay strings here, so a typo like "owner": "three" gets a friendly message, not a parser crash.
// ContentLoader validates these, then builds the real domain records (Tactics.Core.Model).

internal sealed record UnitsFile(IReadOnlyList<UnitTypeEntry?>? UnitTypes);

internal sealed record UnitTypeEntry(
    string? Id,
    string? Name,
    int? MaxHp,
    int? Power,
    int? Defense,
    int? Move,
    int? MinRange,
    int? MaxRange,
    string? Ability);

internal sealed record ScenarioFile(
    string? Id,
    string? Name,
    BoardEntry? Board,
    IReadOnlyList<PlacementEntry?>? Placements);

internal sealed record BoardEntry(int? Width, int? Height, IReadOnlyList<PositionEntry?>? Obstacles);

internal sealed record PositionEntry(int? X, int? Y);

internal sealed record PlacementEntry(string? Id, string? TypeId, string? Owner, int? X, int? Y);
