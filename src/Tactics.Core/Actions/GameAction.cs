using System.Text.Json.Serialization;
using Tactics.Core.Model;

namespace Tactics.Core.Actions;

/// <summary>
/// A player's request to change the game: a command stored as plain data. Because it is data, the engine can
/// check it before anything changes, and the server can save it and replay the game from it (Step 4).
/// Every action type is listed here, so all the cases sit in one place. In JSON, "type" says which one it is.
/// </summary>
/// <param name="Player">The side asking. The server fills it in from the player's token, never from the request.</param>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(MoveAction), "move")]
public abstract record GameAction(PlayerSide Player);

/// <summary>Walk one unit to a tile. The player only says where; the engine picks the shortest path.</summary>
/// <param name="Player">The side asking.</param>
/// <param name="UnitId">The unit to move, e.g. "one-archer".</param>
/// <param name="To">The tile to end on.</param>
public sealed record MoveAction(PlayerSide Player, string UnitId, Position To) : GameAction(Player);
