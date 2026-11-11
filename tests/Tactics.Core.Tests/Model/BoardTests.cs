using Tactics.Core.Model;

namespace Tactics.Core.Tests.Model;

public sealed class BoardTests
{
    private static readonly Board Board8x8 = new(8, 8, [new Position(2, 3)]);

    [Theory]
    [InlineData(0, 0)]
    [InlineData(7, 0)]
    [InlineData(0, 7)]
    [InlineData(7, 7)]
    public void Contains_includes_every_corner(int x, int y)
    {
        Assert.True(Board8x8.Contains(new Position(x, y)));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(8, 0)]
    [InlineData(0, -1)]
    [InlineData(0, 8)]
    public void Contains_excludes_tiles_past_an_edge(int x, int y)
    {
        Assert.False(Board8x8.Contains(new Position(x, y)));
    }

    [Fact]
    public void IsObstacle_is_true_only_on_obstacle_tiles()
    {
        Assert.True(Board8x8.IsObstacle(new Position(2, 3)));
        Assert.False(Board8x8.IsObstacle(new Position(3, 2))); // x and y swapped: a different tile
    }
}
