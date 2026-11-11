namespace Tactics.Core.Model;

/// <summary>The two seats at the table. The rules only ever talk about sides, never about accounts.</summary>
public enum PlayerSide
{
    /// <summary>Starts at the bottom of the board and plays first.</summary>
    One,

    /// <summary>Starts at the top of the board and plays second.</summary>
    Two,
}

/// <summary>Helpers for <see cref="PlayerSide"/>, kept next to the enum they extend.</summary>
public static class PlayerSideExtensions
{
    /// <summary>The other side. Used to pass the turn and to tell enemies from allies.</summary>
    public static PlayerSide Opponent(this PlayerSide side) => side switch
    {
        PlayerSide.One => PlayerSide.Two,
        PlayerSide.Two => PlayerSide.One,
        _ => throw new ArgumentOutOfRangeException(nameof(side), side, "Unknown player side."),
    };
}
