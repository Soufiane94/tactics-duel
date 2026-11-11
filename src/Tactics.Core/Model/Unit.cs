namespace Tactics.Core.Model;

/// <summary>
/// One piece on the board at one moment. It holds only what changes during a game; the stats live in
/// its <see cref="UnitType"/>. Immutable: a move or a hit makes a new <see cref="Unit"/> with <c>with</c>.
/// </summary>
/// <param name="Id">Unique within a game, e.g. "one-archer".</param>
/// <param name="TypeId">Key of its <see cref="UnitType"/>.</param>
/// <param name="Owner">The side that controls it.</param>
/// <param name="Position">The tile it stands on.</param>
/// <param name="Hp">Current hit points, from 1 to its type's MaxHp while it is on the board.</param>
/// <param name="HasMoved">True once it has moved this turn. A unit moves at most once per turn.</param>
/// <param name="HasActed">True once it has acted this turn. Acting also ends its movement.</param>
public sealed record Unit(
    string Id,
    string TypeId,
    PlayerSide Owner,
    Position Position,
    int Hp,
    bool HasMoved,
    bool HasActed);
