using Tactics.Core.Model;

namespace Tactics.Core.Tests.Helpers;

/// <summary>Small edits for setting up a test, such as an injured unit or one that already acted.</summary>
public static class GameStateExtensions
{
    /// <summary>
    /// A copy of the state with one unit changed, e.g. <c>state.WithUnit("two-mage", u => u with { Hp = 3 })</c>.
    /// The unit keeps its place in the list, and the original state stays as it was. An unknown id throws,
    /// so a typo fails the test loudly instead of quietly testing a different board.
    /// </summary>
    public static GameState WithUnit(this GameState state, string unitId, Func<Unit, Unit> change)
    {
        var unit = state.FindUnit(unitId)
            ?? throw new ArgumentException($"There is no unit \"{unitId}\" in this state.", nameof(unitId));

        return state with { Units = state.Units.Replace(unit, change(unit)) };
    }
}
