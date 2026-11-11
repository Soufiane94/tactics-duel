using System.Collections.Immutable;
using Tactics.Core.Model;

namespace Tactics.Core.Rules;

/// <summary>
/// Where units can walk. A breadth-first search (BFS) explores the board ring by ring from the unit:
/// first every tile 1 step away, then 2 steps, and so on. So the first time it reaches a tile, it got there
/// by a shortest path.
/// </summary>
public static class Pathfinding
{
    /// <summary>
    /// Every tile the unit can reach in at most its type's Move steps, in 4 directions, through empty tiles only.
    /// Obstacles, other units (allies too) and the board edge block it. Geometry only: it doesn't check whose turn
    /// it is or whether the unit already moved. The engine checks those rules, so this stays reusable.
    /// </summary>
    public static ReachMap Reachable(GameState state, Unit unit)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(unit);

        var maxSteps = state.TypeOf(unit).Move;
        var occupied = state.Units.Select(u => u.Position).ToHashSet();

        // Mutable collections are fine here: they're local, so nobody outside can see them change.
        // The function stays pure: same state in, same map out.
        var distance = new Dictionary<Position, int> { [unit.Position] = 0 };
        var cameFrom = ImmutableDictionary.CreateBuilder<Position, Position>();
        var destinations = ImmutableList.CreateBuilder<Position>();
        var frontier = new Queue<Position>(); // tiles found but not explored yet, nearest first
        frontier.Enqueue(unit.Position);

        while (frontier.Count > 0)
        {
            var current = frontier.Dequeue();
            if (distance[current] == maxSteps)
            {
                continue; // no movement left: don't look past this tile
            }

            // Neighbors() always gives up, right, down, left. When two paths are equally short,
            // the one found first wins, so this order decides: the same board always gives the same path.
            foreach (var next in current.Neighbors())
            {
                if (distance.ContainsKey(next) || !IsWalkable(state.Board, occupied, next))
                {
                    continue; // already found (by a path at least as short), or blocked
                }

                distance[next] = distance[current] + 1;
                cameFrom[next] = current;
                destinations.Add(next);
                frontier.Enqueue(next);
            }
        }

        return new ReachMap(unit.Position, destinations.ToImmutable(), cameFrom.ToImmutable());
    }

    // An empty tile on the board. The moving unit's own tile counts as occupied, but the search starts there anyway.
    private static bool IsWalkable(Board board, HashSet<Position> occupied, Position tile) =>
        board.Contains(tile) && !board.IsObstacle(tile) && !occupied.Contains(tile);
}
