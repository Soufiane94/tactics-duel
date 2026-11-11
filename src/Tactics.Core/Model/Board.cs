using System.Collections.Immutable;

namespace Tactics.Core.Model;

/// <summary>
/// The grid: its size and its impassable tiles. It never changes during a game,
/// so it holds no units; <see cref="GameState.Units"/> does.
/// </summary>
/// <param name="Width">Number of columns; X goes from 0 to Width - 1.</param>
/// <param name="Height">Number of rows; Y goes from 0 to Height - 1.</param>
/// <param name="Obstacles">Tiles no unit can enter or stop on. In v1 they don't block attacks.</param>
public sealed record Board(int Width, int Height, ImmutableHashSet<Position> Obstacles)
{
    /// <summary>True if the tile is on the board. Neighbors of edge tiles can fall outside it.</summary>
    public bool Contains(Position position) =>
        position.X >= 0 && position.X < Width && position.Y >= 0 && position.Y < Height;

    /// <summary>True if the tile is an obstacle.</summary>
    public bool IsObstacle(Position position) => Obstacles.Contains(position);
}
