using Tactics.Core.Actions;
using Tactics.Core.Events;
using Tactics.Core.Model;
using Tactics.Core.Rules;

namespace Tactics.Core;

public static partial class GameEngine
{
    // Validate, then apply, like a move. The checks go from the actor (who) to the kind of target (what)
    // to the distance (where), so the same mistake always gets the same code.
    private static ActionOutcome ApplyAct(GameState state, ActAction act)
    {
        // 1. Validate the actor: the same checks as for a move, except that having moved is fine.
        if (state.FindUnit(act.ActorId) is not { } actor)
        {
            return Reject(RuleViolation.UnitNotFound, $"There is no unit \"{act.ActorId}\" on the board.");
        }

        var actorType = state.TypeOf(actor);
        if (actor.Owner != act.Player)
        {
            return Reject(RuleViolation.NotYourUnit, $"That {actorType.Name} belongs to {Describe(actor.Owner)}.");
        }

        if (actor.HasActed)
        {
            return Reject(RuleViolation.AlreadyActed, $"This {actorType.Name} has already acted this turn.");
        }

        // Then the target.
        if (state.FindUnit(act.TargetId) is not { } target)
        {
            return Reject(RuleViolation.TargetNotFound, $"There is no unit \"{act.TargetId}\" on the board to target.");
        }

        var targetType = state.TypeOf(target);
        if (CheckTarget(actor, actorType, target, targetType) is { } refused)
        {
            return refused;
        }

        // 2. Apply. Nothing above changed anything, and nothing below can fail.
        // Acting ends the actor's activity for this turn, so it can't move afterwards either.
        var spent = state with { Units = state.Units.Replace(actor, actor with { HasActed = true, HasMoved = true }) };

        // Read the target from the new state. It's the same record, unless a unit acts on itself (a heal with
        // MinRange 0, which content forbids today): then the old copy is no longer in the list.
        var current = spent.FindUnit(target.Id)!;

        // One case per ability. C# can't tell that this list is complete, hence the last arm.
        return actorType.Ability switch
        {
            Ability.Damage => Hit(spent, actor, actorType, current, targetType),
            Ability.Heal => Heal(spent, actor, actorType, current, targetType),
            _ => throw new ArgumentException($"The engine has no rules for the {actorType.Ability} ability.", nameof(state)),
        };
    }

    // The target rules: the right kind of target first (what), then the distance (where). Null means the actor
    // can act on it. Step 4's available actions will ask this same function about every unit, so the targets
    // the client highlights are exactly the ones Apply accepts.
    private static ActionOutcome.Rejected? CheckTarget(Unit actor, UnitType actorType, Unit target, UnitType targetType)
    {
        var isAlly = target.Owner == actor.Owner;

        // What: damage is for enemies, a heal for injured allies. Moving can't fix the wrong kind of target.
        if (actorType.Ability == Ability.Damage && isAlly)
        {
            return Reject(RuleViolation.InvalidTarget, target.Id == actor.Id
                ? $"This {actorType.Name} can't attack itself."
                : $"This {actorType.Name} only attacks enemies, and that {targetType.Name} is on your side.");
        }

        if (actorType.Ability == Ability.Heal && !isAlly)
        {
            return Reject(RuleViolation.InvalidTarget, $"This {actorType.Name} only heals allies, and that {targetType.Name} is an enemy.");
        }

        if (actorType.Ability == Ability.Heal && target.Hp >= targetType.MaxHp)
        {
            return Reject(RuleViolation.InvalidTarget, $"That {targetType.Name} is at full HP: there's nothing to heal.");
        }

        // Where: the Manhattan distance from where the actor stands now. There's no line of sight in v1, so
        // obstacles and units in between don't block. MinRange is at least 1, so a healer can't heal itself.
        var distance = actor.Position.ManhattanTo(target.Position);
        if (distance < actorType.MinRange || distance > actorType.MaxRange)
        {
            return Reject(RuleViolation.TargetOutOfRange, WhyOutOfRange(actorType, distance));
        }

        return null;
    }

    // A hit: the formula says how hard it is, and HP stops at 0. A unit at 0 HP is defeated and leaves the board.
    private static ActionOutcome.Accepted Hit(GameState state, Unit actor, UnitType actorType, Unit target, UnitType targetType)
    {
        var damage = Combat.CalculateDamage(actorType, targetType);
        var hpAfter = Math.Max(0, target.Hp - damage); // the event keeps the full damage, even past 0 (overkill)
        var hit = new UnitDamaged(target.Id, actor.Id, damage, hpAfter);

        if (hpAfter > 0)
        {
            return new ActionOutcome.Accepted(state with { Units = state.Units.Replace(target, target with { Hp = hpAfter }) }, [hit]);
        }

        // Cause before effect: the client shows the hit, then the defeat. The other units keep their order.
        return new ActionOutcome.Accepted(state with { Units = state.Units.Remove(target) }, [hit, new UnitDefeated(target.Id)]);
    }

    // A heal: the formula already caps it, so HP never goes past the target's MaxHp.
    private static ActionOutcome.Accepted Heal(GameState state, Unit actor, UnitType actorType, Unit target, UnitType targetType)
    {
        var healed = Combat.CalculateHeal(actorType, targetType, target.Hp);
        var hpAfter = target.Hp + healed;
        var next = state with { Units = state.Units.Replace(target, target with { Hp = hpAfter }) };
        return new ActionOutcome.Accepted(next, [new UnitHealed(target.Id, actor.Id, healed, hpAfter)]);
    }

    // The reason behind TargetOutOfRange, in words: "Archer hits targets 2–3 tiles away; this target is 1 tile away."
    private static string WhyOutOfRange(UnitType type, int distance)
    {
        var verb = type.Ability == Ability.Heal ? "heals" : "hits";
        var range = type.MinRange == type.MaxRange ? Tiles(type.MinRange) : $"{type.MinRange}–{type.MaxRange} tiles";
        return distance == 0
            ? $"{type.Name} {verb} targets {range} away, so it can't target itself."
            : $"{type.Name} {verb} targets {range} away; this target is {Tiles(distance)} away.";
    }
}
