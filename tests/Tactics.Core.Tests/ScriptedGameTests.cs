using Tactics.Core.Actions;
using Tactics.Core.Events;
using Tactics.Core.Model;
using Tactics.Core.Tests.Helpers;

namespace Tactics.Core.Tests;

/// <summary>
/// A whole game with no UI: a short story in three turns, from the first move to the win. Read it top to bottom.
/// At the end, the game is rebuilt from its log alone, and it comes out the same.
/// </summary>
public sealed class ScriptedGameTests
{
    [Fact]
    public void An_archer_and_a_healer_beat_a_mage_in_three_turns()
    {
        // Player One (uppercase): an archer and a healer. Player Two (lowercase): a mage, alone.
        var game = new Playthrough("""
            . . . . . m
            A . . . . .
            H . . . . .
            """);

        // ── Turn 1, Player One ──
        // The archer walks 3 tiles, then shoots the mage from 3 tiles away: 8 power − 0 defense = 8 damage.
        game.Play(new MoveAction(PlayerSide.One, "one-archer", new Position(3, 1)));
        var arrow = game.Play(new ActAction(PlayerSide.One, "one-archer", "two-mage"));
        Assert.Equal(new UnitDamaged("two-mage", "one-archer", Amount: 8, HpAfter: 6), Assert.Single(arrow));

        // The healer follows, and Player One ends the turn.
        game.Play(new MoveAction(PlayerSide.One, "one-healer", new Position(2, 2)));
        var endOfTurn1 = game.Play(new EndTurnAction(PlayerSide.One));
        Assert.Equal(new TurnEnded(PlayerSide.One, PlayerSide.Two, TurnNumber: 2), Assert.Single(endOfTurn1));
        Assert.Equal("""
            . . . . . m
            . . . A . .
            . . H . . .
            """, AsciiBoard.Draw(game.State), ignoreLineEndingDifferences: true);

        // ── Turn 2, Player Two ──
        // The mage steps next to the archer, into its blind spot (the archer hits at 2–3 tiles), and blasts it.
        game.Play(new MoveAction(PlayerSide.Two, "two-mage", new Position(4, 1)));
        var blast = game.Play(new ActAction(PlayerSide.Two, "two-mage", "one-archer"));
        Assert.Equal(new UnitDamaged("one-archer", "two-mage", Amount: 11, HpAfter: 7), Assert.Single(blast));
        game.Play(new EndTurnAction(PlayerSide.Two));
        Assert.Equal("""
            . . . . . .
            . . . A m .
            . . H . . .
            """, AsciiBoard.Draw(game.State), ignoreLineEndingDifferences: true);

        // ── Turn 3, Player One ──
        // A new turn, so both units can move and act again. First the healer patches up the archer.
        var heal = game.Play(new ActAction(PlayerSide.One, "one-healer", "one-archer"));
        Assert.Equal(new UnitHealed("one-archer", "one-healer", Amount: 5, HpAfter: 12), Assert.Single(heal));

        // The mage is 1 tile away: too close for the archer. The engine says no, and nothing changes.
        var tooClose = GameEngine.Apply(game.State, new ActAction(PlayerSide.One, "one-archer", "two-mage"));
        Assert.Equal(RuleViolation.TargetOutOfRange, Assert.IsType<ActionOutcome.Rejected>(tooClose).Code);

        // So the archer steps back and shoots. The mage had 6 HP left, and it was Player Two's last unit.
        game.Play(new MoveAction(PlayerSide.One, "one-archer", new Position(2, 1)));
        var lastArrow = game.Play(new ActAction(PlayerSide.One, "one-archer", "two-mage"));
        GameEvent[] theEnd =
        [
            new UnitDamaged("two-mage", "one-archer", Amount: 8, HpAfter: 0),
            new UnitDefeated("two-mage"),
            new GameWon(PlayerSide.One),
        ];
        Assert.Equal(theEnd, lastArrow);
        Assert.Equal("""
            . . . . . .
            . . A . . .
            . . H . . .
            """, AsciiBoard.Draw(game.State), ignoreLineEndingDifferences: true);

        // ── After the game ──
        // Nobody can play any more, not even the side that lost.
        Assert.Equal(GameStatus.Finished, game.State.Status);
        Assert.Equal(PlayerSide.One, game.State.Winner);
        var tooLate = GameEngine.Apply(game.State, new EndTurnAction(PlayerSide.Two));
        Assert.Equal(RuleViolation.GameOver, Assert.IsType<ActionOutcome.Rejected>(tooLate).Code);

        // The whole game is its setup plus these 10 actions (refused ones were never logged).
        // Replaying them rebuilds exactly the same final state: that's all the server will need to store.
        Assert.Equal(10, game.Log.Count);
        StateJson.AssertSame(game.State, GameEngine.Replay(game.Setup, game.Log));
    }
}
