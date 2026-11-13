using System.Collections.Immutable;
using Tactics.Core.Actions;
using Tactics.Core.Events;
using Tactics.Core.Model;

namespace Tactics.Core.Tests.Helpers;

/// <summary>
/// Plays a game action by action, the way the server will (Step 5): each action goes through
/// <see cref="GameEngine.Apply"/>, must be accepted, and joins the log. The setup plus the log is the whole game,
/// and <see cref="GameEngine.Replay"/> rebuilds it.
/// </summary>
public sealed class Playthrough
{
    private readonly List<GameAction> log = [];

    /// <summary>Starts a new game on a board picture (see <see cref="AsciiBoard"/>): turn 1, everyone at full HP.</summary>
    public Playthrough(string map)
    {
        Setup = AsciiBoard.ParseSetup(map);
        State = GameEngine.CreateInitialState(Setup);
    }

    /// <summary>What the game started from.</summary>
    public GameSetup Setup { get; }

    /// <summary>The game now, after every action played so far.</summary>
    public GameState State { get; private set; }

    /// <summary>Every accepted action, in order. A rejected action never gets here.</summary>
    public IReadOnlyList<GameAction> Log => log;

    /// <summary>
    /// Plays one action and returns its events. A rejection fails the test with the engine's own message,
    /// so a mistake in a test points straight at the rule it broke.
    /// </summary>
    public ImmutableList<GameEvent> Play(GameAction action)
    {
        var outcome = GameEngine.Apply(State, action);
        if (outcome is ActionOutcome.Rejected rejected)
        {
            Assert.Fail($"{action} was rejected ({rejected.Code}): {rejected.Message}");
        }

        var accepted = Assert.IsType<ActionOutcome.Accepted>(outcome);
        State = accepted.State;
        log.Add(action);
        return accepted.Events;
    }
}
