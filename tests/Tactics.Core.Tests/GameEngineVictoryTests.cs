using Tactics.Core.Actions;
using Tactics.Core.Events;
using Tactics.Core.Model;
using Tactics.Core.Rules;
using Tactics.Core.Tests.Helpers;

namespace Tactics.Core.Tests;

/// <summary>
/// The engine's last step: after every accepted action it asks <see cref="Victory.WinnerOf"/>, and a winner
/// finishes the game. A finished game then rejects every action with GameOver.
/// </summary>
public sealed class GameEngineVictoryTests
{
    // Player Two's last unit, a mage, is down to 3 HP: one more arrow ends the game.
    private static readonly GameState LastStand = AsciiBoard.Parse("A . m").WithUnit("two-mage", u => u with { Hp = 3 });

    [Fact]
    public void Defeating_the_last_enemy_wins_the_game()
    {
        var outcome = GameEngine.Apply(LastStand, new ActAction(PlayerSide.One, "one-archer", "two-mage"));

        // Cause before effect, all the way: the hit, the defeat, then the win.
        var accepted = Assert.IsType<ActionOutcome.Accepted>(outcome);
        GameEvent[] events =
        [
            new UnitDamaged("two-mage", "one-archer", Amount: 8, HpAfter: 0),
            new UnitDefeated("two-mage"),
            new GameWon(PlayerSide.One),
        ];
        Assert.Equal(events, accepted.Events);
        Assert.Equal(GameStatus.Finished, accepted.State.Status);
        Assert.Equal(PlayerSide.One, accepted.State.Winner);
    }

    [Fact]
    public void Player_Two_can_win_too()
    {
        // Player One's last unit, a warrior, has 5 HP left. Player One ends turn 1, then the archer shoots: 8 − 3 = 5.
        var state = AsciiBoard.Parse("W . . a").WithUnit("one-warrior", u => u with { Hp = 5 });
        var turn2 = Assert.IsType<ActionOutcome.Accepted>(GameEngine.Apply(state, new EndTurnAction(PlayerSide.One))).State;

        var outcome = GameEngine.Apply(turn2, new ActAction(PlayerSide.Two, "two-archer", "one-warrior"));

        var accepted = Assert.IsType<ActionOutcome.Accepted>(outcome);
        Assert.Equal(new GameWon(PlayerSide.Two), accepted.Events[^1]); // the win is always the last event
        Assert.Equal(PlayerSide.Two, accepted.State.Winner);
    }

    [Fact]
    public void A_defeat_does_not_end_the_game_while_the_enemy_has_units_left()
    {
        var state = AsciiBoard.Parse("A . m w").WithUnit("two-mage", u => u with { Hp = 3 });

        var outcome = GameEngine.Apply(state, new ActAction(PlayerSide.One, "one-archer", "two-mage"));

        var accepted = Assert.IsType<ActionOutcome.Accepted>(outcome);
        Assert.IsType<UnitDefeated>(accepted.Events[^1]); // no GameWon after it: the warrior fights on
        Assert.Equal(GameStatus.InProgress, accepted.State.Status);
        Assert.Null(accepted.State.Winner);
    }

    [Fact]
    public void A_won_game_rejects_every_action_from_both_sides()
    {
        var won = Assert.IsType<ActionOutcome.Accepted>(
            GameEngine.Apply(LastStand, new ActAction(PlayerSide.One, "one-archer", "two-mage"))).State;

        // The game-over check comes first, so it decides the code even when other rules are broken too.
        GameAction[] attempts =
        [
            new MoveAction(PlayerSide.One, "one-archer", new Position(1, 0)),
            new ActAction(PlayerSide.One, "one-archer", "two-mage"), // also: that mage is gone
            new EndTurnAction(PlayerSide.One),
            new EndTurnAction(PlayerSide.Two), // also: it isn't Player Two's turn
        ];

        Assert.All(attempts, attempt =>
            Assert.Equal(RuleViolation.GameOver, Assert.IsType<ActionOutcome.Rejected>(GameEngine.Apply(won, attempt)).Code));
    }

    [Fact]
    public void The_win_check_runs_after_every_action_not_only_after_a_defeat()
    {
        // A board with one side only is already won, so its first accepted action ends it, even a turn end.
        // Content validation never allows such a board, but a test board can: give every test board both sides.
        var outcome = GameEngine.Apply(AsciiBoard.Parse("W . ."), new EndTurnAction(PlayerSide.One));

        var accepted = Assert.IsType<ActionOutcome.Accepted>(outcome);
        GameEvent[] events = [new TurnEnded(PlayerSide.One, PlayerSide.Two, TurnNumber: 2), new GameWon(PlayerSide.One)];
        Assert.Equal(events, accepted.Events);
        Assert.Equal(GameStatus.Finished, accepted.State.Status);
    }
}
