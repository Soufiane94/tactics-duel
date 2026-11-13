using System.Collections.Immutable;
using Tactics.Core.Model;
using Tactics.Core.Rules;

namespace Tactics.Core;

public static partial class GameEngine
{
    /// <summary>
    /// Everything one side can do right now, unit by unit, so the client can highlight it without knowing the rules.
    /// Only units that can still move or act are listed: on your turn, an empty list means only ending the turn is left.
    /// It's empty for the side that waits and once the game is over. Ending the turn is always allowed, so it isn't listed.
    /// </summary>
    public static ImmutableList<UnitActions> GetAvailableActions(GameState state, PlayerSide side)
    {
        ArgumentNullException.ThrowIfNull(state);

        // The same checks Apply runs first, in the same order.
        if (state.Status == GameStatus.Finished || side != state.CurrentPlayer)
        {
            return [];
        }

        return state.UnitsOf(side)
            .Select(unit => new UnitActions(unit.Id, MoveTargetsOf(state, unit), ActTargetsOf(state, unit)))
            .Where(options => options.MoveTargets.Count > 0 || options.ActTargets.Count > 0)
            .ToImmutableList();
    }

    // The same search ApplyMove runs, so every highlighted tile is a move that Apply accepts.
    private static ImmutableList<Position> MoveTargetsOf(GameState state, Unit unit) =>
        unit.HasMoved || unit.HasActed ? [] : Pathfinding.Reachable(state, unit).Destinations;

    // The same target check ApplyAct runs, asked about every unit on the board (the actor too: a heal with
    // MinRange 0 could target itself). Range counts from where the unit stands now, not from where it could move.
    private static ImmutableList<string> ActTargetsOf(GameState state, Unit actor)
    {
        if (actor.HasActed)
        {
            return [];
        }

        var actorType = state.TypeOf(actor);
        return state.Units
            .Where(target => CheckTarget(actor, actorType, target, state.TypeOf(target)) is null)
            .Select(target => target.Id)
            .ToImmutableList();
    }
}
