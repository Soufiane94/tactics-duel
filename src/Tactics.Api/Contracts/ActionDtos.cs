using System.Text.Json.Serialization;

namespace Tactics.Api.Contracts;

/// <summary>
/// An action as a client sends it. It never says which player sends it: the server takes the side from the player
/// token (rule 6), so a client can't play for the other side. In JSON, "type" says which action it is.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(MoveActionDto), "move")]
[JsonDerivedType(typeof(ActActionDto), "act")]
[JsonDerivedType(typeof(EndTurnActionDto), "endTurn")]
public abstract record ActionDto;

/// <summary>Move one of your units. You only say where: the server picks the shortest path.</summary>
/// <param name="UnitId">The unit to move, e.g. "one-archer".</param>
/// <param name="To">The tile to end on. It must be one of the unit's <c>moveTargets</c>.</param>
public sealed record MoveActionDto(string UnitId, PositionDto To) : ActionDto;

/// <summary>Use one of your units' ability on another unit: damage an enemy, or heal an injured ally.</summary>
/// <param name="ActorId">The unit that acts, e.g. "one-archer".</param>
/// <param name="TargetId">The unit it acts on. It must be one of the actor's <c>actTargets</c>.</param>
public sealed record ActActionDto(string ActorId, string TargetId) : ActionDto;

/// <summary>End your turn. Always allowed on your turn.</summary>
public sealed record EndTurnActionDto : ActionDto;
