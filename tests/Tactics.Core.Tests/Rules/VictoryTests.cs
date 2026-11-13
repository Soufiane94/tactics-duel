using Tactics.Core.Model;
using Tactics.Core.Rules;
using Tactics.Core.Tests.Helpers;

namespace Tactics.Core.Tests.Rules;

/// <summary>
/// <see cref="Victory.WinnerOf"/>, the win condition on its own: it reads a state and names the winner, if any.
/// What winning changes (status, winner, the GameWon event) is the engine's job: see <see cref="GameEngineVictoryTests"/>.
/// </summary>
public sealed class VictoryTests
{
    [Theory]
    [InlineData("W . w", null)] // both sides still have units: the game goes on
    [InlineData("W A .", PlayerSide.One)] // Player Two has no units left
    [InlineData(". . w", PlayerSide.Two)] // Player One has no units left
    public void A_side_with_no_units_left_has_lost(string map, PlayerSide? winner)
    {
        Assert.Equal(winner, Victory.WinnerOf(AsciiBoard.Parse(map)));
    }

    [Fact]
    public void Only_whether_units_are_left_counts_not_how_many_or_their_HP()
    {
        // A mage with 1 HP left against a full army: the game isn't over yet.
        var state = AsciiBoard.Parse("""
            W A M H
            . . . m
            """).WithUnit("two-mage", u => u with { Hp = 1 });

        Assert.Null(Victory.WinnerOf(state));
    }

    [Fact]
    public void A_board_with_no_units_at_all_is_a_bug_not_a_draw()
    {
        // v1 has no draws, and no action removes two units at once. If a new rule ever makes this possible,
        // the exception points to the one place where you decide what it means.
        Assert.Throws<InvalidOperationException>(() => Victory.WinnerOf(AsciiBoard.Parse(". # .")));
    }
}
