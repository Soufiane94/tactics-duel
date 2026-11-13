using System.Collections.Immutable;
using Tactics.Core.Model;

namespace Tactics.Core;

/// <summary>
/// What one unit can still do this turn, from <see cref="GameEngine.GetAvailableActions"/>. The client highlights
/// exactly these tiles and targets and computes none of its own, so what it shows is what the engine accepts.
/// </summary>
/// <param name="UnitId">The unit, e.g. "one-archer".</param>
/// <param name="MoveTargets">Every tile it can move to, nearest first. Empty once it has moved or acted.</param>
/// <param name="ActTargets">
/// The ids of the units it can act on from the tile it stands on now, in the order of <see cref="GameState.Units"/>.
/// Empty once it has acted.
/// </param>
public sealed record UnitActions(string UnitId, ImmutableList<Position> MoveTargets, ImmutableList<string> ActTargets);
