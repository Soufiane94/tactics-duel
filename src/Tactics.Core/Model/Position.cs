namespace Tactics.Core.Model;

/// <summary>
/// A tile on the board. The origin is the top-left corner: X grows to the right and Y grows down,
/// like screen pixels. A value type, so positions compare by value and work as set and dictionary keys.
/// </summary>
/// <param name="X">Column, from 0 at the left edge.</param>
/// <param name="Y">Row, from 0 at the top edge.</param>
public readonly record struct Position(int X, int Y)
{
    /// <summary>
    /// Grid distance with 4-way movement: |dx| + |dy|, the number of steps on an empty board.
    /// Attack and heal ranges use it.
    /// </summary>
    public int ManhattanTo(Position other) => Math.Abs(X - other.X) + Math.Abs(Y - other.Y);

    /// <summary>
    /// The 4 adjacent tiles, always in the order up, right, down, left. A fixed order keeps
    /// pathfinding deterministic: the same board always gives the same path. Can include tiles
    /// outside the board; callers check <see cref="Board.Contains"/>.
    /// </summary>
    public IReadOnlyList<Position> Neighbors() =>
    [
        new(X, Y - 1), // up (Y grows down, so up is Y - 1)
        new(X + 1, Y), // right
        new(X, Y + 1), // down
        new(X - 1, Y), // left
    ];

    /// <summary>Short form for messages and test output, e.g. "(1, 7)".</summary>
    public override string ToString() => $"({X}, {Y})";
}
