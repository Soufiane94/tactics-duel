using Tactics.Core.Model;
using Tactics.Core.Tests.Helpers;

namespace Tactics.Core.Tests.Model;

public sealed class GameStateTests
{
    private static readonly GameState State = AsciiBoard.Parse("""
        . a .
        . # .
        W . h
        """);

    [Fact]
    public void UnitAt_finds_the_unit_on_a_tile_or_returns_null()
    {
        Assert.Equal("one-warrior", State.UnitAt(new Position(0, 2))?.Id);
        Assert.Null(State.UnitAt(new Position(1, 2))); // an empty tile
        Assert.Null(State.UnitAt(new Position(1, 1))); // an obstacle holds no unit
    }

    [Fact]
    public void FindUnit_finds_a_unit_by_id_or_returns_null()
    {
        Assert.Equal(new Position(2, 2), State.FindUnit("two-healer")?.Position);
        Assert.Null(State.FindUnit("two-dragon"));
    }

    [Fact]
    public void TypeOf_returns_the_stats_of_a_unit()
    {
        var archer = State.FindUnit("two-archer")!;

        Assert.Equal(18, State.TypeOf(archer).MaxHp);
    }

    [Fact]
    public void UnitsOf_returns_only_that_side()
    {
        Assert.Equal(["one-warrior"], State.UnitsOf(PlayerSide.One).Select(u => u.Id).ToArray());
        Assert.Equal(["two-archer", "two-healer"], State.UnitsOf(PlayerSide.Two).Select(u => u.Id).ToArray());
    }

    [Fact]
    public void With_makes_a_changed_copy_and_leaves_the_original_untouched()
    {
        var warrior = State.FindUnit("one-warrior")!;

        // "Move" the warrior: a new Unit, inside a new list, inside a new GameState.
        var moved = warrior with { Position = new Position(1, 2), HasMoved = true };
        var next = State with { Units = State.Units.Replace(warrior, moved) };

        Assert.Equal(new Position(1, 2), next.FindUnit("one-warrior")!.Position);
        Assert.Equal(new Position(0, 2), State.FindUnit("one-warrior")!.Position); // the old state is still valid
        Assert.False(warrior.HasMoved);
    }

    [Fact]
    public void My_first_board_test()
    {
        var state = AsciiBoard.Parse("""
            . m
            A #
            """);
        Assert.Equal("one-archer", state.UnitAt(new Position(0, 1))?.Id);
    }

}
