using System.Collections.Immutable;

namespace Tactics.Core.Model;

/// <summary>
/// The whole game at one moment, and the single source of truth: the screen only draws it.
/// Immutable: rules never edit a state, they return a new one, so every past state stays valid
/// (replays, undo, tests).
/// </summary>
/// <param name="Board">The grid and its obstacles.</param>
/// <param name="UnitTypes">Unit stats by type id, from this game's own setup.</param>
/// <param name="Units">Units still on the board, in a stable order. Defeated units are removed (Step 3).</param>
/// <param name="CurrentPlayer">The side whose turn it is.</param>
/// <param name="TurnNumber">Starts at 1 and goes up by 1 each time a turn ends.</param>
/// <param name="Status">In progress or finished.</param>
/// <param name="Winner">The winning side once the game is finished; otherwise null.</param>
public sealed record GameState(
    Board Board,
    ImmutableDictionary<string, UnitType> UnitTypes,
    ImmutableList<Unit> Units,
    PlayerSide CurrentPlayer,
    int TurnNumber,
    GameStatus Status,
    PlayerSide? Winner)
{
    // A linear scan is fine in the helpers below: a game has about 8 units.

    /// <summary>The unit standing on a tile, or null if the tile is empty.</summary>
    public Unit? UnitAt(Position position) => Units.FirstOrDefault(u => u.Position == position);

    /// <summary>The unit with this id, or null if there is none (for example, it was defeated).</summary>
    public Unit? FindUnit(string id) => Units.FirstOrDefault(u => u.Id == id);

    /// <summary>The stats of a unit. Its type always exists: content validation guarantees it.</summary>
    public UnitType TypeOf(Unit unit) => UnitTypes[unit.TypeId];

    /// <summary>The units one side controls, in the same order as <see cref="Units"/>.</summary>
    public IEnumerable<Unit> UnitsOf(PlayerSide side) => Units.Where(u => u.Owner == side);
}
