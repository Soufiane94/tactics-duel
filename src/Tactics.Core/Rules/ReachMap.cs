using System.Collections.Immutable;
using Tactics.Core.Model;

namespace Tactics.Core.Rules;

/// <summary>
/// Every tile one unit can walk to, with the shortest path to each. <see cref="Pathfinding.Reachable"/> builds it
/// from one state, so it is a snapshot: it never changes, even when the game moves on.
/// </summary>
public sealed class ReachMap
{
    // For each destination, the tile the search came from: a trail of breadcrumbs back to the start.
    private readonly ImmutableDictionary<Position, Position> cameFrom;

    internal ReachMap(Position start, ImmutableList<Position> destinations, ImmutableDictionary<Position, Position> cameFrom)
    {
        Start = start;
        Destinations = destinations;
        this.cameFrom = cameFrom;
    }

    /// <summary>The unit's own tile. It is never a destination: staying put isn't a move.</summary>
    public Position Start { get; }

    /// <summary>
    /// Every tile the unit can end a move on, nearest first (the order the search found them).
    /// A fixed order keeps the list the same on every run, which tests and the API rely on.
    /// </summary>
    public ImmutableList<Position> Destinations { get; }

    /// <summary>True if the unit can end a move on this tile.</summary>
    public bool CanReach(Position tile) => cameFrom.ContainsKey(tile);

    /// <summary>
    /// The shortest path to a destination: every tile stepped on, in order, without the start and with the
    /// destination. That's the path the client animates. Throws for a tile that isn't a destination, so check
    /// <see cref="CanReach"/> first.
    /// </summary>
    public ImmutableList<Position> PathTo(Position destination)
    {
        if (!CanReach(destination))
        {
            throw new ArgumentException($"{destination} is not a destination from {Start}.", nameof(destination));
        }

        // Follow the breadcrumbs back from the destination to the start, then flip the list.
        var path = new List<Position>();
        for (var tile = destination; tile != Start; tile = cameFrom[tile])
        {
            path.Add(tile);
        }

        path.Reverse();
        return path.ToImmutableList();
    }
}
