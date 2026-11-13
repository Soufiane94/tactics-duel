using System.Diagnostics;
using Tactics.Core.Actions;
using Tactics.Core.Model;

namespace Tactics.Core;

public static partial class GameEngine
{
    /// <summary>
    /// Rebuilds a game from its setup and its action log, so the server only has to store the actions.
    /// The rules are deterministic: the same log always gives the same state. Every logged action was accepted
    /// once, so a rejection means a bug or a damaged log, and it throws <see cref="ReplayException"/>.
    /// </summary>
    public static GameState Replay(GameSetup setup, IEnumerable<GameAction> actions)
    {
        ArgumentNullException.ThrowIfNull(setup);
        ArgumentNullException.ThrowIfNull(actions);

        // A fold: start from the initial state, and let each action turn the current state into the next one.
        // It's LINQ's Aggregate, or an NgRx reducer. A loop also counts the actions, for the error message.
        var state = CreateInitialState(setup);
        var sequence = 0;
        foreach (var action in actions)
        {
            sequence++;
            state = Apply(state, action) switch
            {
                ActionOutcome.Accepted accepted => accepted.State,
                ActionOutcome.Rejected rejected => throw new ReplayException(sequence, action, rejected),
                _ => throw new UnreachableException("An outcome is either accepted or rejected."),
            };
        }

        return state;
    }
}
