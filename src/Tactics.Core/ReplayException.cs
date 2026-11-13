using Tactics.Core.Actions;

namespace Tactics.Core;

/// <summary>
/// Thrown when an action log doesn't replay: one of its actions is rejected. Every logged action was accepted
/// when it was played, and the rules are deterministic, so this is a bug or a damaged log, never a player's
/// mistake. That's why it throws, unlike <see cref="GameEngine.Apply"/>.
/// </summary>
public sealed class ReplayException : Exception
{
    /// <summary>Creates the exception for the action that the rules rejected.</summary>
    public ReplayException(int sequence, GameAction action, ActionOutcome.Rejected rejection)
        : base(BuildMessage(sequence, action, rejection))
    {
        Sequence = sequence;
        Action = action;
        Code = rejection.Code;
    }

    /// <summary>The action's place in the log, counting from 1 (the API's sequence numbers count the same way).</summary>
    public int Sequence { get; }

    /// <summary>The action that was rejected.</summary>
    public GameAction Action { get; }

    /// <summary>The rule it broke. Tests check this code, so the message can change freely.</summary>
    public RuleViolation Code { get; }

    private static string BuildMessage(int sequence, GameAction action, ActionOutcome.Rejected rejection)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(rejection);
        return $"Action {sequence} of the log was rejected ({rejection.Code}): {rejection.Message} The action: {action}";
    }
}
