using System.Collections.Immutable;
using System.Text.Json.Serialization;
using Tactics.Core.Model;

namespace Tactics.Core.Events;

/// <summary>
/// A fact about something that just happened, named in the past tense. The client plays events as
/// animations, then snaps to the new state, which stays the truth. Every event type is listed here,
/// so all the cases sit in one place. In JSON, "type" says which one it is.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(UnitMoved), "unitMoved")]
[JsonDerivedType(typeof(UnitDamaged), "unitDamaged")]
[JsonDerivedType(typeof(UnitHealed), "unitHealed")]
[JsonDerivedType(typeof(UnitDefeated), "unitDefeated")]
public abstract record GameEvent;

/// <summary>A unit walked to another tile. The path lets the client animate the walk step by step.</summary>
/// <param name="UnitId">The unit that moved.</param>
/// <param name="From">Where it started.</param>
/// <param name="To">Where it stopped.</param>
/// <param name="Path">Every tile it stepped on, in order: without From, with To.</param>
public sealed record UnitMoved(string UnitId, Position From, Position To, ImmutableList<Position> Path) : GameEvent;

/// <summary>
/// A unit was hit and lost HP. If HpAfter is 0, a <see cref="UnitDefeated"/> event comes right after it:
/// the cause first, then the effect, which is the order the client animates them.
/// </summary>
/// <param name="UnitId">The unit that was hit.</param>
/// <param name="ByUnitId">The unit that attacked.</param>
/// <param name="Amount">How hard the hit was, from the damage formula. On a killing blow it can be more than the HP the unit had left (overkill).</param>
/// <param name="HpAfter">The unit's HP after the hit. Never below 0.</param>
public sealed record UnitDamaged(string UnitId, string ByUnitId, int Amount, int HpAfter) : GameEvent;

/// <summary>A unit got HP back from a healer.</summary>
/// <param name="UnitId">The unit that was healed.</param>
/// <param name="ByUnitId">The healer.</param>
/// <param name="Amount">The HP restored: the healer's power, or less if the unit was missing less.</param>
/// <param name="HpAfter">The unit's HP after the heal. Never above its MaxHp.</param>
public sealed record UnitHealed(string UnitId, string ByUnitId, int Amount, int HpAfter) : GameEvent;

/// <summary>A unit reached 0 HP and left the board. No later state contains it.</summary>
/// <param name="UnitId">The unit that was defeated.</param>
public sealed record UnitDefeated(string UnitId) : GameEvent;
