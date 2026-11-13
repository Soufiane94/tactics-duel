using Tactics.Core.Actions;
using Tactics.Core.Events;
using Tactics.Core.Model;
using Tactics.Core.Rules;

namespace Tactics.Core;

public static partial class GameEngine
{
    // Validate, then apply. Every check runs before anything changes, and a failed check returns a Rejected
    // value instead of throwing, so a bad click is an ordinary answer. Then the move itself is a few lines.
    private static ActionOutcome ApplyMove(GameState state, MoveAction move)
    {
        // 1. Validate, in the order PLAN.md gives.
        if (state.FindUnit(move.UnitId) is not { } unit)
        {
            return Reject(RuleViolation.UnitNotFound, $"There is no unit \"{move.UnitId}\" on the board.");
        }

        var type = state.TypeOf(unit);
        if (unit.Owner != move.Player)
        {
            return Reject(RuleViolation.NotYourUnit, $"That {type.Name} belongs to {Describe(unit.Owner)}.");
        }

        // "Acted" before "moved": acting sets HasMoved too (GameEngine.Act.cs), and then acting is the real reason.
        if (unit.HasActed)
        {
            return Reject(RuleViolation.AlreadyActed, $"This {type.Name} has already acted this turn, so it can't move.");
        }

        if (unit.HasMoved)
        {
            return Reject(RuleViolation.AlreadyMoved, $"This {type.Name} has already moved this turn.");
        }

        var reach = Pathfinding.Reachable(state, unit);
        if (!reach.CanReach(move.To))
        {
            return Reject(RuleViolation.DestinationNotReachable, WhyNotReachable(state, unit, type, move.To));
        }

        // 2. Apply. Nothing above changed anything, and nothing below can fail.
        // Replace finds the old unit by value (records compare by value), so the list keeps its order.
        var moved = unit with { Position = move.To, HasMoved = true };
        var next = state with { Units = state.Units.Replace(unit, moved) };
        return new ActionOutcome.Accepted(next, [new UnitMoved(unit.Id, unit.Position, move.To, reach.PathTo(move.To))]);
    }

    // The reason behind DestinationNotReachable, in words. The search already decided; this only explains.
    private static string WhyNotReachable(GameState state, Unit unit, UnitType type, Position to)
    {
        if (!state.Board.Contains(to))
        {
            return $"{to} is off the board.";
        }

        if (state.Board.IsObstacle(to))
        {
            return $"{to} is an obstacle.";
        }

        if (to == unit.Position)
        {
            return $"This {type.Name} is already on {to}.";
        }

        if (state.UnitAt(to) is { } other)
        {
            var whose = other.Owner == unit.Owner ? "your" : "an enemy";
            return $"{to} is taken by {whose} {state.TypeOf(other).Name}.";
        }

        var distance = unit.Position.ManhattanTo(to);
        return distance > type.Move
            ? $"{type.Name} moves at most {Tiles(type.Move)}; {to} is {Tiles(distance)} away."
            : $"{type.Name} can't reach {to}: the way is blocked.";
    }
}
