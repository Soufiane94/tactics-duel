using System.Text.Json.Serialization;
using Tactics.Core.Model;

namespace Tactics.Api.Contracts;

/// <summary>
/// Something that just happened, for the client to animate in order before it shows the new view.
/// In JSON, "type" says which event it is. Each one mirrors a Core event, field for field.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(UnitMovedDto), "unitMoved")]
[JsonDerivedType(typeof(UnitDamagedDto), "unitDamaged")]
[JsonDerivedType(typeof(UnitHealedDto), "unitHealed")]
[JsonDerivedType(typeof(UnitDefeatedDto), "unitDefeated")]
[JsonDerivedType(typeof(TurnEndedDto), "turnEnded")]
[JsonDerivedType(typeof(GameWonDto), "gameWon")]
public abstract record EventDto;

/// <summary>A unit walked to another tile.</summary>
/// <param name="UnitId">The unit that moved.</param>
/// <param name="From">Where it started.</param>
/// <param name="To">Where it stopped.</param>
/// <param name="Path">Every tile it stepped on, in order: without From, with To.</param>
public sealed record UnitMovedDto(string UnitId, PositionDto From, PositionDto To, IReadOnlyList<PositionDto> Path) : EventDto;

/// <summary>A unit was hit. If <c>hpAfter</c> is 0, a <c>unitDefeated</c> event follows.</summary>
/// <param name="UnitId">The unit that was hit.</param>
/// <param name="ByUnitId">The attacker.</param>
/// <param name="Amount">How hard the hit was. On a killing blow it can exceed the HP the unit had left.</param>
/// <param name="HpAfter">Its HP after the hit, never below 0.</param>
public sealed record UnitDamagedDto(string UnitId, string ByUnitId, int Amount, int HpAfter) : EventDto;

/// <summary>A unit got HP back from a healer.</summary>
/// <param name="UnitId">The unit that was healed.</param>
/// <param name="ByUnitId">The healer.</param>
/// <param name="Amount">The HP restored.</param>
/// <param name="HpAfter">Its HP after the heal, never above its max HP.</param>
public sealed record UnitHealedDto(string UnitId, string ByUnitId, int Amount, int HpAfter) : EventDto;

/// <summary>A unit reached 0 HP and left the board.</summary>
/// <param name="UnitId">The unit that was defeated.</param>
public sealed record UnitDefeatedDto(string UnitId) : EventDto;

/// <summary>A turn ended, and the other side plays now.</summary>
/// <param name="PreviousPlayer">The side whose turn ended.</param>
/// <param name="NextPlayer">The side that plays now. Its units can move and act again.</param>
/// <param name="TurnNumber">The number of the turn that starts now.</param>
public sealed record TurnEndedDto(PlayerSide PreviousPlayer, PlayerSide NextPlayer, int TurnNumber) : EventDto;

/// <summary>The game is over. Always the last event of the action that ended it.</summary>
/// <param name="Winner">The side that won.</param>
public sealed record GameWonDto(PlayerSide Winner) : EventDto;
