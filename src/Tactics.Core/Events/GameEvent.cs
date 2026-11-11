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
public abstract record GameEvent;

/// <summary>A unit walked to another tile. The path lets the client animate the walk step by step.</summary>
/// <param name="UnitId">The unit that moved.</param>
/// <param name="From">Where it started.</param>
/// <param name="To">Where it stopped.</param>
/// <param name="Path">Every tile it stepped on, in order: without From, with To.</param>
public sealed record UnitMoved(string UnitId, Position From, Position To, ImmutableList<Position> Path) : GameEvent;
