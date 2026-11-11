using Tactics.Core.Model;

namespace Tactics.Core.Tests.Model;

public sealed class PositionTests
{
    [Fact]
    public void ManhattanTo_adds_the_horizontal_and_vertical_steps()
    {
        var archer = new Position(1, 7);
        var warrior = new Position(4, 1);

        Assert.Equal(9, archer.ManhattanTo(warrior)); // 3 across + 6 up
        Assert.Equal(9, warrior.ManhattanTo(archer)); // the same both ways
        Assert.Equal(0, archer.ManhattanTo(archer));
    }

    [Fact]
    public void Neighbors_come_in_the_fixed_order_up_right_down_left()
    {
        // Y grows down, so "up" is Y - 1.
        Position[] expected = [new(3, 2), new(4, 3), new(3, 4), new(2, 3)];

        Assert.Equal(expected, new Position(3, 3).Neighbors());
    }

    [Fact]
    public void Neighbors_of_a_corner_include_tiles_outside_the_board()
    {
        // Position knows nothing about the board; Board.Contains filters these out.
        var neighbors = new Position(0, 0).Neighbors();

        Assert.Contains(new Position(0, -1), neighbors);
        Assert.Contains(new Position(-1, 0), neighbors);
    }

    [Fact]
    public void Positions_compare_by_value()
    {
        var tiles = new HashSet<Position> { new(2, 3) };

        Assert.Equal(new Position(2, 3), new Position(2, 3));
        Assert.Contains(new Position(2, 3), tiles);
        Assert.DoesNotContain(new Position(3, 2), tiles);
    }

    [Fact]
    public void ToString_is_short_enough_for_messages()
    {
        Assert.Equal("(1, 7)", new Position(1, 7).ToString());
    }
}
