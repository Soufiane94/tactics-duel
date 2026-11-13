using System.Text.Json.Serialization;
using Tactics.Core.Model;

namespace Tactics.Core.Actions;

/// <summary>
/// A player's request to change the game: a command stored as plain data. Because it is data, the engine can
/// check it before anything changes, and the server can save it and rebuild the game from the saved list
/// (<see cref="GameEngine.Replay"/>).
/// Every action type is listed here, so all the cases sit in one place. In JSON, "type" says which one it is.
/// </summary>
/// <param name="Player">The side asking. The server fills it in from the player's token, never from the request.</param>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(MoveAction), "move")]
[JsonDerivedType(typeof(ActAction), "act")]
[JsonDerivedType(typeof(EndTurnAction), "endTurn")]
public abstract record GameAction(PlayerSide Player);

/// <summary>Walk one unit to a tile. The player only says where; the engine picks the shortest path.</summary>
/// <param name="Player">The side asking.</param>
/// <param name="UnitId">The unit to move, e.g. "one-archer".</param>
/// <param name="To">The tile to end on.</param>
public sealed record MoveAction(PlayerSide Player, string UnitId, Position To) : GameAction(Player);

/// <summary>
/// Use one unit's ability on another unit. The actor's type decides what happens (damage an enemy or heal
/// an ally), so one action covers both. Acting uses the actor up: it can't move or act again until its side's next turn.
/// </summary>
/// <param name="Player">The side asking.</param>
/// <param name="ActorId">The unit that acts, e.g. "one-archer".</param>
/// <param name="TargetId">The unit it acts on, e.g. "two-warrior".</param>
public sealed record ActAction(PlayerSide Player, string ActorId, string TargetId) : GameAction(Player);

/// <summary>
/// Hand the turn to the other side, whose units get their move and act back. Always legal on your own turn,
/// even if your units did nothing, so it needs no other data.
/// </summary>
/// <param name="Player">The side asking: the one whose turn ends.</param>
public sealed record EndTurnAction(PlayerSide Player) : GameAction(Player);
