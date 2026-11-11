using Tactics.Core.Model;

namespace Tactics.Core.Tests.Helpers;

public sealed class AsciiBoardTests
{
    [Fact]
    public void Parses_the_classic_picture_from_the_plan()
    {
        var state = AsciiBoard.Parse(Maps.Classic);

        Assert.Equal(8, state.Board.Width);
        Assert.Equal(8, state.Board.Height);
        Position[] obstacles = [new(5, 2), new(2, 3), new(5, 4), new(2, 5)];
        Assert.True(state.Board.Obstacles.SetEquals(obstacles));

        // Reading order: row by row from the top, left to right.
        (string Id, int X, int Y)[] expected =
        [
            ("two-healer", 1, 0), ("two-mage", 3, 0), ("two-archer", 6, 0), ("two-warrior", 4, 1),
            ("one-warrior", 3, 6), ("one-archer", 1, 7), ("one-mage", 4, 7), ("one-healer", 6, 7),
        ];
        Assert.Equal(expected, state.Units.Select(u => (u.Id, u.Position.X, u.Position.Y)).ToArray());
        Assert.Equal(PlayerSide.One, state.FindUnit("one-archer")!.Owner); // uppercase
        Assert.Equal(PlayerSide.Two, state.FindUnit("two-archer")!.Owner); // lowercase
    }

    [Fact]
    public void Row_labels_and_the_header_are_optional()
    {
        var compact = AsciiBoard.Parse("""
            . h . m . . a .
            . . . . w . . .
            . . . . . # . .
            . . # . . . . .
            . . . . . # . .
            . . # . . . . .
            . . . W . . . .
            . A . . M . H .
            """);

        var labelled = AsciiBoard.Parse(Maps.Classic);

        Assert.Equal(labelled.Units, compact.Units);
        Assert.True(labelled.Board.Obstacles.SetEquals(compact.Board.Obstacles));
    }

    [Fact]
    public void A_second_unit_of_the_same_kind_gets_a_numbered_id()
    {
        var state = AsciiBoard.Parse("W W w");

        Assert.Equal(["one-warrior", "one-warrior-2", "two-warrior"], state.Units.Select(u => u.Id).ToArray());
    }

    [Fact]
    public void Units_get_the_fixed_test_stats()
    {
        var state = AsciiBoard.Parse("A");

        Assert.Same(AsciiBoard.DefaultUnitTypes, state.UnitTypes);
        Assert.Equal(18, state.Units[0].Hp); // the placeholder archer from PLAN.md, section 1
    }

    [Theory]
    [InlineData("W x")] // unknown symbol
    [InlineData("W .\n.")] // rows of different lengths
    [InlineData("WW .")] // two symbols in one cell
    [InlineData("")] // no rows at all
    public void Rejects_a_malformed_picture(string map)
    {
        Assert.Throws<ArgumentException>(() => AsciiBoard.Parse(map));
    }
}
