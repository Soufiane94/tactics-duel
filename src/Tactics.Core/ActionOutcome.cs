using System.Collections.Immutable;
using Tactics.Core.Events;
using Tactics.Core.Model;

namespace Tactics.Core;

/// <summary>
/// What <see cref="GameEngine.Apply"/> returns: the action was accepted or rejected, nothing else.
/// A closed hierarchy: the constructor is private, so only the two nested records derive from it.
/// Callers handle both cases with a switch expression, the way a TypeScript union is handled.
/// </summary>
public abstract record ActionOutcome
{
    private ActionOutcome()
    {
    }

    /// <summary>The action followed the rules. The state is the new truth; the events say what happened.</summary>
    /// <param name="State">The game after the action.</param>
    /// <param name="Events">What happened, in the order the client should animate it.</param>
    public sealed record Accepted(GameState State, ImmutableList<GameEvent> Events) : ActionOutcome;

    /// <summary>The action broke a rule. Nothing changed: the caller keeps the state it has.</summary>
    /// <param name="Code">Which rule, as a stable code.</param>
    /// <param name="Message">What went wrong, in plain words, for the player.</param>
    public sealed record Rejected(RuleViolation Code, string Message) : ActionOutcome;
}
