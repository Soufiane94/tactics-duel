using Tactics.Core.Model;
using Tactics.Core.Rules;
using Tactics.Core.Tests.Helpers;

namespace Tactics.Core.Tests.Rules;

/// <summary>
/// Breadth-first search on the grid. Each picture shows how many steps it takes to reach every tile,
/// so the test is also the explanation. Fixed test stats: the warrior, archer and healer move 3, the mage 2.
/// </summary>
public sealed class PathfindingTests
{
    [Fact]
    public void On_an_open_board_the_reach_is_a_diamond()
    {
        var state = AloneInTheMiddle('W');

        var reach = Pathfinding.Reachable(state, state.Units[0]);

        Assert.Equal("""
            . . . 3 . . .
            . . 3 2 3 . .
            . 3 2 1 2 3 .
            3 2 1 W 1 2 3
            . 3 2 1 2 3 .
            . . 3 2 3 . .
            . . . 3 . . .
            """, AsciiBoard.Draw(state, reach), ignoreLineEndingDifferences: true);
        Assert.Equal(24, reach.Destinations.Count); // 4 tiles at 1 step + 8 at 2 + 12 at 3
    }

    [Theory]
    [InlineData('W', 24)]
    [InlineData('A', 24)]
    [InlineData('H', 24)]
    [InlineData('M', 12)] // the mage moves 2: 4 + 8
    public void The_Move_stat_sets_the_size_of_the_diamond(char letter, int expectedTiles)
    {
        var state = AloneInTheMiddle(letter);

        var reach = Pathfinding.Reachable(state, state.Units[0]);

        Assert.Equal(expectedTiles, reach.Destinations.Count);
    }

    [Fact]
    public void The_start_tile_is_not_a_destination()
    {
        var state = AloneInTheMiddle('W');
        var warrior = state.Units[0];

        var reach = Pathfinding.Reachable(state, warrior);

        Assert.Equal(warrior.Position, reach.Start);
        Assert.False(reach.CanReach(warrior.Position));
        Assert.DoesNotContain(warrior.Position, reach.Destinations);
    }

    [Fact]
    public void The_board_edge_cuts_the_diamond()
    {
        var state = AsciiBoard.Parse("""
            W . . . .
            . . . . .
            . . . . .
            . . . . .
            . . . . .
            """);

        var reach = Pathfinding.Reachable(state, state.Units[0]);

        Assert.Equal("""
            W 1 2 3 .
            1 2 3 . .
            2 3 . . .
            3 . . . .
            . . . . .
            """, AsciiBoard.Draw(state, reach), ignoreLineEndingDifferences: true);
        Assert.Equal(9, reach.Destinations.Count);
    }

    [Fact]
    public void Obstacles_block_so_the_path_goes_around_them()
    {
        var state = AsciiBoard.Parse("""
            . . . . .
            . # # # .
            . . W . .
            """);
        var warrior = state.Units[0];

        var reach = Pathfinding.Reachable(state, warrior);

        Assert.Equal("""
            . . . . .
            3 # # # 3
            2 1 W 1 2
            """, AsciiBoard.Draw(state, reach), ignoreLineEndingDifferences: true);

        // (2, 0) is only 2 tiles away as the crow flies (Manhattan), but 6 steps on foot around the wall.
        Assert.Equal(2, warrior.Position.ManhattanTo(new Position(2, 0)));
        Assert.False(reach.CanReach(new Position(2, 0)));
    }

    [Fact]
    public void Units_block_the_way_allies_and_enemies_alike()
    {
        var state = AsciiBoard.Parse("""
            # # # # # # #
            . a . W . A .
            # # # # # # #
            """);

        var reach = Pathfinding.Reachable(state, state.FindUnit("one-warrior")!);

        Assert.Equal("""
            # # # # # # #
            . a 1 W 1 A .
            # # # # # # #
            """, AsciiBoard.Draw(state, reach), ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void A_boxed_in_unit_can_reach_nothing()
    {
        var state = AsciiBoard.Parse("""
            . # .
            # W A
            . # .
            """);

        var reach = Pathfinding.Reachable(state, state.FindUnit("one-warrior")!);

        Assert.Empty(reach.Destinations);
    }

    [Fact]
    public void The_classic_warrior_reaches_17_tiles()
    {
        var state = AsciiBoard.Parse(Maps.Classic);

        var reach = Pathfinding.Reachable(state, state.FindUnit("one-warrior")!);

        Assert.Equal("""
            . h . m . . a .
            . . . . w . . .
            . . . . . # . .
            . . # 3 . . . .
            . . 3 2 3 # . .
            . 3 # 1 2 3 . .
            3 2 1 W 1 2 3 .
            . A 2 1 M 3 H .
            """, AsciiBoard.Draw(state, reach), ignoreLineEndingDifferences: true);
        Assert.Equal(17, reach.Destinations.Count);
    }

    [Fact]
    public void Destinations_come_nearest_first()
    {
        var state = AsciiBoard.Parse(Maps.Classic);

        var reach = Pathfinding.Reachable(state, state.FindUnit("one-warrior")!);

        // The first ring is the warrior's neighbors, in the fixed order: up, right, down, left.
        Position[] firstRing = [new(3, 5), new(4, 6), new(3, 7), new(2, 6)];
        Assert.Equal(firstRing, reach.Destinations.Take(4));
        var steps = reach.Destinations.Select(tile => reach.PathTo(tile).Count).ToList();
        Assert.Equal(steps.Order(), steps);
    }

    [Fact]
    public void Every_path_steps_from_tile_to_adjacent_tile_through_empty_tiles()
    {
        var state = AsciiBoard.Parse(Maps.Classic);

        foreach (var unit in state.Units)
        {
            var reach = Pathfinding.Reachable(state, unit);
            foreach (var destination in reach.Destinations)
            {
                var path = reach.PathTo(destination);

                Assert.Equal(destination, path[^1]);
                Assert.InRange(path.Count, 1, state.TypeOf(unit).Move);
                var previous = unit.Position;
                foreach (var tile in path)
                {
                    Assert.Equal(1, previous.ManhattanTo(tile)); // one step: no diagonals, no jumps
                    Assert.True(state.Board.Contains(tile));
                    Assert.False(state.Board.IsObstacle(tile));
                    Assert.Null(state.UnitAt(tile));
                    previous = tile;
                }
            }
        }
    }

    [Fact]
    public void On_an_open_board_every_path_is_as_short_as_the_Manhattan_distance()
    {
        var state = AloneInTheMiddle('W');
        var warrior = state.Units[0];

        var reach = Pathfinding.Reachable(state, warrior);

        Assert.All(reach.Destinations, tile => Assert.Equal(warrior.Position.ManhattanTo(tile), reach.PathTo(tile).Count));
    }

    [Fact]
    public void Ties_between_equally_short_paths_follow_the_neighbor_order()
    {
        // From (3, 3), several paths are equally short. The search tries up, right, down, left,
        // in that order, and keeps the first path it finds.
        var state = AloneInTheMiddle('W');

        var reach = Pathfinding.Reachable(state, state.Units[0]);

        // Up and to the right: "up" comes first in the order, so the path starts by going up.
        Position[] upThenRight = [new(3, 2), new(4, 2), new(5, 2)];
        Assert.Equal(upThenRight, reach.PathTo(new Position(5, 2)));

        // Down and to the right: "right" comes before "down", so the path starts by going right.
        Position[] rightThenDown = [new(4, 3), new(5, 3), new(5, 4)];
        Assert.Equal(rightThenDown, reach.PathTo(new Position(5, 4)));
    }

    [Fact]
    public void PathTo_refuses_a_tile_that_is_not_a_destination()
    {
        var state = AsciiBoard.Parse("""
            W #
            . .
            """);

        var reach = Pathfinding.Reachable(state, state.Units[0]);

        Assert.Throws<ArgumentException>(() => reach.PathTo(new Position(0, 0))); // the start
        Assert.Throws<ArgumentException>(() => reach.PathTo(new Position(1, 0))); // an obstacle
        Assert.Throws<ArgumentException>(() => reach.PathTo(new Position(5, 5))); // off the board
    }

    [Fact]
    public void Reachable_is_geometry_only_and_ignores_the_turn_flags()
    {
        // Whether a unit may still move is the engine's rule, not the map's.
        var state = AloneInTheMiddle('W');
        var tired = state with { Units = [state.Units[0] with { HasMoved = true, HasActed = true }] };

        var fresh = Pathfinding.Reachable(state, state.Units[0]);
        var used = Pathfinding.Reachable(tired, tired.Units[0]);

        Assert.Equal(fresh.Destinations, used.Destinations);
    }

    // A 7×7 empty board with one unit in the middle, at (3, 3).
    private static GameState AloneInTheMiddle(char letter) => AsciiBoard.Parse($"""
        . . . . . . .
        . . . . . . .
        . . . . . . .
        . . . {letter} . . .
        . . . . . . .
        . . . . . . .
        . . . . . . .
        """);
}
