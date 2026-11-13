using Tactics.Core.Actions;
using Tactics.Core.Events;
using Tactics.Core.Model;
using Tactics.Core.Rules;
using Tactics.Core.Tests.Helpers;

namespace Tactics.Core.Tests;

/// <summary>
/// <see cref="GameEngine.Apply"/> with a <see cref="MoveAction"/>: validate, then apply.
/// Every broken rule comes back as a Rejected outcome with its code, never as an exception.
/// </summary>
public sealed class GameEngineMoveTests
{
    // Shared by many tests. That's safe because a state is immutable: no test can change it for the others.
    private static readonly GameState Classic = AsciiBoard.Parse(Maps.Classic);

    public static TheoryData<string> ClassicUnitIds => new(Classic.Units.Select(u => u.Id));

    [Fact]
    public void A_move_puts_the_unit_on_its_destination_and_marks_it_as_moved()
    {
        var state = AsciiBoard.Parse("""
            . . . .
            W . # a
            """);

        var outcome = GameEngine.Apply(state, new MoveAction(PlayerSide.One, "one-warrior", new Position(2, 0)));

        var after = Assert.IsType<ActionOutcome.Accepted>(outcome).State;
        Assert.Equal("""
            . . W .
            . . # a
            """, AsciiBoard.Draw(after), ignoreLineEndingDifferences: true);
        var warrior = after.FindUnit("one-warrior")!;
        Assert.True(warrior.HasMoved);
        Assert.False(warrior.HasActed); // it can still act
    }

    [Fact]
    public void A_move_emits_one_UnitMoved_event_with_the_path()
    {
        var outcome = GameEngine.Apply(Classic, new MoveAction(PlayerSide.One, "one-archer", new Position(1, 5)));

        // The same event as the example in PLAN.md, section 3 (Events).
        var accepted = Assert.IsType<ActionOutcome.Accepted>(outcome);
        var moved = Assert.IsType<UnitMoved>(Assert.Single(accepted.Events));
        Assert.Equal("one-archer", moved.UnitId);
        Assert.Equal(new Position(1, 7), moved.From);
        Assert.Equal(new Position(1, 5), moved.To);
        Position[] path = [new(1, 6), new(1, 5)];
        Assert.Equal(path, moved.Path);
    }

    [Fact]
    public void A_move_returns_a_new_state_and_leaves_the_old_one_untouched()
    {
        var outcome = GameEngine.Apply(Classic, new MoveAction(PlayerSide.One, "one-archer", new Position(1, 5)));

        var after = Assert.IsType<ActionOutcome.Accepted>(outcome).State;
        Assert.Equal(new Position(1, 7), Classic.FindUnit("one-archer")!.Position); // the old state is still valid
        Assert.Equal(Classic.Units.Select(u => u.Id), after.Units.Select(u => u.Id)); // same units, same order
        Assert.Equal(Classic.Units.Where(u => u.Id != "one-archer"), after.Units.Where(u => u.Id != "one-archer"));
        Assert.Equal(PlayerSide.One, after.CurrentPlayer); // moving doesn't end the turn
        Assert.Equal(1, after.TurnNumber);
    }

    [Fact]
    public void A_finished_game_rejects_every_move()
    {
        var finished = Classic with { Status = GameStatus.Finished, Winner = PlayerSide.Two };

        var outcome = GameEngine.Apply(finished, new MoveAction(PlayerSide.One, "one-archer", new Position(1, 5)));

        Assert.Equal(RuleViolation.GameOver, Assert.IsType<ActionOutcome.Rejected>(outcome).Code);
    }

    [Fact]
    public void A_player_cannot_move_on_the_other_players_turn()
    {
        // Turn 1 belongs to Player One.
        var outcome = GameEngine.Apply(Classic, new MoveAction(PlayerSide.Two, "two-archer", new Position(6, 1)));

        Assert.Equal(RuleViolation.NotYourTurn, Assert.IsType<ActionOutcome.Rejected>(outcome).Code);
    }

    [Fact]
    public void Moving_a_unit_that_is_not_on_the_board_is_rejected()
    {
        var outcome = GameEngine.Apply(Classic, new MoveAction(PlayerSide.One, "one-knight", new Position(1, 5)));

        Assert.Equal(RuleViolation.UnitNotFound, Assert.IsType<ActionOutcome.Rejected>(outcome).Code);
    }

    [Fact]
    public void A_player_cannot_move_an_enemy_unit()
    {
        var outcome = GameEngine.Apply(Classic, new MoveAction(PlayerSide.One, "two-archer", new Position(6, 1)));

        Assert.Equal(RuleViolation.NotYourUnit, Assert.IsType<ActionOutcome.Rejected>(outcome).Code);
    }

    [Fact]
    public void A_unit_moves_only_once_per_turn()
    {
        var first = GameEngine.Apply(Classic, new MoveAction(PlayerSide.One, "one-archer", new Position(1, 5)));
        var afterFirst = Assert.IsType<ActionOutcome.Accepted>(first).State;

        var again = GameEngine.Apply(afterFirst, new MoveAction(PlayerSide.One, "one-archer", new Position(1, 4)));
        var otherUnit = GameEngine.Apply(afterFirst, new MoveAction(PlayerSide.One, "one-warrior", new Position(3, 5)));

        Assert.Equal(RuleViolation.AlreadyMoved, Assert.IsType<ActionOutcome.Rejected>(again).Code);
        Assert.IsType<ActionOutcome.Accepted>(otherUnit); // the other units still have their move
    }

    [Theory]
    [InlineData(true)] // what acting really does: it sets both flags (GameEngineActTests plays it for real)
    [InlineData(false)]
    public void A_unit_that_acted_cannot_move(bool hasMoved)
    {
        var archer = Classic.FindUnit("one-archer")!;
        var state = Classic with
        {
            Units = Classic.Units.Replace(archer, archer with { HasActed = true, HasMoved = hasMoved }),
        };

        var outcome = GameEngine.Apply(state, new MoveAction(PlayerSide.One, "one-archer", new Position(1, 5)));

        Assert.Equal(RuleViolation.AlreadyActed, Assert.IsType<ActionOutcome.Rejected>(outcome).Code);
    }

    [Theory]
    [InlineData("one-archer", 1, 8)] // off the board, below
    [InlineData("one-archer", -1, 7)] // off the board, to the left
    [InlineData("one-archer", 2, 5)] // an obstacle
    [InlineData("one-archer", 3, 6)] // taken by an ally (the warrior)
    [InlineData("one-warrior", 4, 1)] // taken by an enemy (their warrior)
    [InlineData("one-archer", 1, 7)] // its own tile: staying put isn't a move
    [InlineData("one-archer", 1, 3)] // too far: 4 tiles, and the archer moves 3
    [InlineData("one-healer", 3, 7)] // only 3 tiles away, but the mage and the warrior block the way
    public void A_tile_the_unit_cannot_reach_is_rejected(string unitId, int x, int y)
    {
        var to = new Position(x, y);

        var outcome = GameEngine.Apply(Classic, new MoveAction(PlayerSide.One, unitId, to));

        var rejected = Assert.IsType<ActionOutcome.Rejected>(outcome);
        Assert.Equal(RuleViolation.DestinationNotReachable, rejected.Code);
        Assert.Contains(to.ToString(), rejected.Message, StringComparison.Ordinal); // the message names the tile
    }

    [Fact]
    public void When_several_rules_are_broken_the_first_check_decides_the_code()
    {
        // The checks run in PLAN.md's order, so the same mistake always gets the same answer.
        var nowhere = new Position(9, 9);
        var finished = Classic with { Status = GameStatus.Finished, Winner = PlayerSide.One };
        var enemyArcher = Classic.FindUnit("two-archer")!;
        var enemyActed = Classic with
        {
            Units = Classic.Units.Replace(enemyArcher, enemyArcher with { HasActed = true }),
        };

        var gameOver = GameEngine.Apply(finished, new MoveAction(PlayerSide.Two, "nobody", nowhere));
        var notYourTurn = GameEngine.Apply(Classic, new MoveAction(PlayerSide.Two, "nobody", nowhere));
        var notYourUnit = GameEngine.Apply(enemyActed, new MoveAction(PlayerSide.One, "two-archer", nowhere));

        Assert.Equal(RuleViolation.GameOver, Assert.IsType<ActionOutcome.Rejected>(gameOver).Code);
        Assert.Equal(RuleViolation.NotYourTurn, Assert.IsType<ActionOutcome.Rejected>(notYourTurn).Code);
        Assert.Equal(RuleViolation.NotYourUnit, Assert.IsType<ActionOutcome.Rejected>(notYourUnit).Code);
    }

    [Theory]
    [MemberData(nameof(ClassicUnitIds))]
    public void Apply_accepts_exactly_the_tiles_that_Reachable_lists(string unitId)
    {
        // Every tile of the board, plus a ring just outside it. Apply must agree with the search, and never throw.
        var unit = Classic.FindUnit(unitId)!;
        var state = Classic with { CurrentPlayer = unit.Owner }; // give the turn to the unit's side
        var reach = Pathfinding.Reachable(state, unit);

        for (var y = -1; y <= state.Board.Height; y++)
        {
            for (var x = -1; x <= state.Board.Width; x++)
            {
                var to = new Position(x, y);

                var outcome = GameEngine.Apply(state, new MoveAction(unit.Owner, unit.Id, to));

                if (reach.CanReach(to))
                {
                    var accepted = Assert.IsType<ActionOutcome.Accepted>(outcome);
                    var moved = Assert.IsType<UnitMoved>(Assert.Single(accepted.Events));
                    Assert.Equal(reach.PathTo(to), moved.Path);
                }
                else
                {
                    Assert.Equal(RuleViolation.DestinationNotReachable, Assert.IsType<ActionOutcome.Rejected>(outcome).Code);
                }
            }
        }
    }
}
