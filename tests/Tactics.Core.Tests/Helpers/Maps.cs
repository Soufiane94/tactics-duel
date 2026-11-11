namespace Tactics.Core.Tests.Helpers;

/// <summary>Board pictures shared by many tests. Parse them with <see cref="AsciiBoard.Parse"/>.</summary>
public static class Maps
{
    /// <summary>The classic layout, copied as is from PLAN.md, section 1 (labels included).</summary>
    public const string Classic = """
             x: 0 1 2 3 4 5 6 7
        y=0     . h . m . . a .
        y=1     . . . . w . . .
        y=2     . . . . . # . .
        y=3     . . # . . . . .
        y=4     . . . . . # . .
        y=5     . . # . . . . .
        y=6     . . . W . . . .
        y=7     . A . . M . H .
        """;
}
