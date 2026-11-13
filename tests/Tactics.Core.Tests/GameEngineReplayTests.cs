using Tactics.Core.Actions;
using Tactics.Core.Model;
using Tactics.Core.Tests.Helpers;

namespace Tactics.Core.Tests;

/// <summary>
/// <see cref="GameEngine.Replay"/>: the setup plus the action log is the whole game. Replaying the log rebuilds the
/// same state, and a log that doesn't replay is a bug, so Replay throws instead of returning Rejected.
/// </summary>
public sealed class GameEngineReplayTests
{
    private const string Skirmish = """
        . a . . w
        . . # . .
        W . . . A
        """;

    [Fact]
    public void An_empty_log_replays_to_the_initial_state()
    {
        var setup = AsciiBoard.ParseSetup(Skirmish);

        var replayed = GameEngine.Replay(setup, []);

        StateJson.AssertSame(GameEngine.CreateInitialState(setup), replayed);
    }

    [Fact]
    public void Replaying_the_log_rebuilds_the_state_that_playing_it_reached()
    {
        var game = PlayTwoTurns();

        var replayed = GameEngine.Replay(game.Setup, game.Log);

        StateJson.AssertSame(game.State, replayed);
        Assert.Equal(3, replayed.TurnNumber); // two turns played, so turn 3 is Player One's
        Assert.Equal(23, replayed.FindUnit("two-warrior")!.Hp); // 28 − 5: the arrow from turn 1 is still there
    }

    [Fact]
    public void Two_replays_of_a_log_are_the_same_game_but_not_Equal_records()
    {
        // The trap behind StateJson. A record compares each property with Equals, and a list's Equals compares
        // references. Each replay builds its own Units list, so the two states are not Equal...
        var game = PlayTwoTurns();
        var first = GameEngine.Replay(game.Setup, game.Log);
        var second = GameEngine.Replay(game.Setup, game.Log);

        Assert.NotEqual(first, second);
        Assert.Equal(first.Units, second.Units); // ...yet xUnit compares two lists item by item, and the units match
        StateJson.AssertSame(first, second); // and so does the JSON, which holds every value

        // The JSON really holds the units (an empty "{}" would make every comparison pass).
        Assert.Contains("\"id\":\"two-warrior\",\"typeId\":\"warrior\",\"owner\":\"two\"", StateJson.Of(first), StringComparison.Ordinal);
    }

    [Fact]
    public void A_log_that_breaks_a_rule_throws_and_names_the_bad_action()
    {
        var setup = AsciiBoard.ParseSetup(Skirmish);
        GameAction[] log =
        [
            new MoveAction(PlayerSide.One, "one-warrior", new Position(1, 1)),
            new EndTurnAction(PlayerSide.One),
            new EndTurnAction(PlayerSide.One), // damaged: Player One again, on Player Two's turn
        ];

        var thrown = Assert.Throws<ReplayException>(() => GameEngine.Replay(setup, log));

        Assert.Equal(3, thrown.Sequence); // counting from 1
        Assert.Equal(RuleViolation.NotYourTurn, thrown.Code);
        Assert.Same(log[2], thrown.Action);
    }

    // Undo is a replay without the last action
    [Fact]
    public void Undo_is_a_replay_without_the_last_action()
    {
       var game = PlayTwoTurns(); 
       var undone = GameEngine.Replay(game.Setup, game.Log.SkipLast(1));
       Assert.Equal(PlayerSide.Two, undone.CurrentPlayer);
    }

    // Two turns: each side walks one unit and shoots once with its archer (8 − 3 = 5 damage on a warrior).
    private static Playthrough PlayTwoTurns()
    {
        var game = new Playthrough(Skirmish);
        game.Play(new MoveAction(PlayerSide.One, "one-warrior", new Position(1, 1)));
        game.Play(new ActAction(PlayerSide.One, "one-archer", "two-warrior"));
        game.Play(new EndTurnAction(PlayerSide.One));
        game.Play(new MoveAction(PlayerSide.Two, "two-archer", new Position(2, 0)));
        game.Play(new ActAction(PlayerSide.Two, "two-archer", "one-warrior"));
        game.Play(new EndTurnAction(PlayerSide.Two));
        return game;
    }
}
