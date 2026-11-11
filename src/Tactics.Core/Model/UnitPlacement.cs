namespace Tactics.Core.Model;

/// <summary>Where one unit starts. A scenario is a board plus a list of placements.</summary>
/// <param name="Id">The unit's id for the whole game, e.g. "one-archer".</param>
/// <param name="TypeId">Key of its <see cref="UnitType"/>.</param>
/// <param name="Owner">The side that controls it.</param>
/// <param name="Position">Its starting tile.</param>
public sealed record UnitPlacement(string Id, string TypeId, PlayerSide Owner, Position Position);
