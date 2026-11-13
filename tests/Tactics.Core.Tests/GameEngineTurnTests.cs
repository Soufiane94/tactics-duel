using Tactics.Core.Actions;
using Tactics.Core.Events;
using Tactics.Core.Model;
using Tactics.Core.Tests.Helpers;

namespace Tactics.Core.Tests;

/// <summary>
/// <see cref="GameEngine.Apply"/> with an <see cref="EndTurnAction"/>: the turn passes to the other side,
/// whose units get their move and act back. Turn 1 is Player One's, turn 2 Player Two's, and so on.
/// </summary>
public sealed class GameEngineTurnTests
{
    [Fact]
    public void Ending_the_turn_passes_play_to_the_other_side()
    {
        // Nobody moved or acted, and that's fine: you can end your turn at any time (PLAN.md, section 1).
        var state = AsciiBoard.Parse("A . . w");

        var outcome = GameEngine.Apply(state, new EndTurnAction(PlayerSide.One));

        // The example from PLAN.md, section 3 (Events): turn 1 ends, so turn 2 begins.
        var accepted = Assert.IsType<ActionOutcome.Accepted>(outcome);
        Assert.Equal(new TurnEnded(PlayerSide.One, PlayerSide.Two, TurnNumber: 2), Assert.Single(accepted.Events));
        Assert.Equal(PlayerSide.Two, accepted.State.CurrentPlayer);
        Assert.Equal(2, accepted.State.TurnNumber);
    }

    [Fact]
    public void The_next_side_gets_its_move_and_act_back()
    {
        // Every unit is spent: Player One's from this turn, Player Two's still from its own last turn.
        var start = AsciiBoard.Parse("A . . w h");
        var spent = start with { Units = [.. start.Units.Select(u => u with { HasMoved = true, HasActed = true })] };

        var outcome = GameEngine.Apply(spent, new EndTurnAction(PlayerSide.One));

        var after = Assert.IsType<ActionOutcome.Accepted>(outcome).State;
        Assert.All(after.UnitsOf(PlayerSide.Two), u =>
        {
            Assert.False(u.HasMoved);
            Assert.False(u.HasActed);
        });

        // The side that just played keeps its flags until its own next turn begins.
        var archer = after.FindUnit("one-archer")!;
        Assert.True(archer.HasMoved);
        Assert.True(archer.HasActed);
    }

    [Fact]
    public void Ending_the_turn_changes_nothing_else()
    {
        var state = AsciiBoard.Parse("""
            A . # w
            . H . m
            """).WithUnit("two-mage", u => u with { Hp = 5 });

        var outcome = GameEngine.Apply(state, new EndTurnAction(PlayerSide.One));

        // Same tiles, same HP, same order: only the turn, the player and the next side's flags change.
        var after = Assert.IsType<ActionOutcome.Accepted>(outcome).State;
        Assert.Equal(AsciiBoard.Draw(state), AsciiBoard.Draw(after));
        Assert.Equal(state.Units.Select(u => (u.Id, u.Hp)), after.Units.Select(u => (u.Id, u.Hp)));
        Assert.Equal(GameStatus.InProgress, after.Status);
    }

    [Fact]
    public void Only_the_player_whose_turn_it_is_can_end_it()
    {
        var state = AsciiBoard.Parse("A . . w");
        var twosTurn = Assert.IsType<ActionOutcome.Accepted>(GameEngine.Apply(state, new EndTurnAction(PlayerSide.One))).State;

        var twoTooEarly = GameEngine.Apply(state, new EndTurnAction(PlayerSide.Two));
        var oneAgain = GameEngine.Apply(twosTurn, new EndTurnAction(PlayerSide.One)); // a double click on "End turn"

        Assert.Equal(RuleViolation.NotYourTurn, Assert.IsType<ActionOutcome.Rejected>(twoTooEarly).Code);
        Assert.Equal(RuleViolation.NotYourTurn, Assert.IsType<ActionOutcome.Rejected>(oneAgain).Code);
    }

    [Fact]
    public void Turns_alternate_and_the_turn_number_counts_every_turn()
    {
        var state = AsciiBoard.Parse("A . . w");
        var turns = new List<(PlayerSide, int)>();

        for (var i = 0; i < 4; i++)
        {
            var outcome = GameEngine.Apply(state, new EndTurnAction(state.CurrentPlayer));
            state = Assert.IsType<ActionOutcome.Accepted>(outcome).State;
            turns.Add((state.CurrentPlayer, state.TurnNumber));
        }

        // The number counts single turns, not rounds: Player One plays the odd turns, Player Two the even ones.
        (PlayerSide, int)[] expected = [(PlayerSide.Two, 2), (PlayerSide.One, 3), (PlayerSide.Two, 4), (PlayerSide.One, 5)];
        Assert.Equal(expected, turns);
    }

    [Fact]
    public void A_unit_that_acted_can_act_again_on_its_sides_next_turn()
    {
        var state = AsciiBoard.Parse("A . . w");
        var shot = Assert.IsType<ActionOutcome.Accepted>(
            GameEngine.Apply(state, new ActAction(PlayerSide.One, "one-archer", "two-warrior"))).State;

        var sameTurn = GameEngine.Apply(shot, new ActAction(PlayerSide.One, "one-archer", "two-warrior"));
        var turn2 = Assert.IsType<ActionOutcome.Accepted>(GameEngine.Apply(shot, new EndTurnAction(PlayerSide.One))).State;
        var turn3 = Assert.IsType<ActionOutcome.Accepted>(GameEngine.Apply(turn2, new EndTurnAction(PlayerSide.Two))).State;
        var nextTurn = GameEngine.Apply(turn3, new ActAction(PlayerSide.One, "one-archer", "two-warrior"));

        Assert.Equal(RuleViolation.AlreadyActed, Assert.IsType<ActionOutcome.Rejected>(sameTurn).Code);
        var accepted = Assert.IsType<ActionOutcome.Accepted>(nextTurn);
        Assert.Equal(18, accepted.State.FindUnit("two-warrior")!.Hp); // 28 − 5 − 5: one arrow per turn
    }

    [Fact]
    public void A_finished_game_rejects_ending_the_turn()
    {
        var finished = AsciiBoard.Parse("A . . w") with { Status = GameStatus.Finished, Winner = PlayerSide.One };

        var outcome = GameEngine.Apply(finished, new EndTurnAction(PlayerSide.One));

        Assert.Equal(RuleViolation.GameOver, Assert.IsType<ActionOutcome.Rejected>(outcome).Code);
    }
}
