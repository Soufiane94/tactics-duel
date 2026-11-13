using Tactics.Core.Model;

namespace Tactics.Core.Rules;

/// <summary>
/// The win condition, and nothing else: who has won, if anyone. The engine asks after every accepted action,
/// so a new way to win (hold a tile, survive N turns) means editing this one file.
/// </summary>
public static class Victory
{
    /// <summary>
    /// The winner, or null while the game goes on. The v1 rule: a side with no units left has lost.
    /// It only reads the state; what winning changes is the engine's job (<see cref="GameEngine.Apply"/>).
    /// </summary>
    public static PlayerSide? WinnerOf(GameState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        var oneHasUnits = state.UnitsOf(PlayerSide.One).Any();
        var twoHasUnits = state.UnitsOf(PlayerSide.Two).Any();

        // Two yes/no questions give four cases. The compiler checks that all four are here, so no "_" arm.
        return (oneHasUnits, twoHasUnits) switch
        {
            (true, true) => null, // both sides can still fight
            (true, false) => PlayerSide.One,
            (false, true) => PlayerSide.Two,

            // Only a hit removes a unit, one at a time, so both sides can't run out together in v1. A rule that
            // could do it (area damage, counter-attacks) must first decide what it means, so this fails loudly.
            (false, false) => throw new InvalidOperationException(
                "Both sides have no units left. v1 has no draws: decide in Victory.WinnerOf what this means."),
        };
    }
}
