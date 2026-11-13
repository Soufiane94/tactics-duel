using Tactics.Core.Content;
using Tactics.Core.Model;

namespace Tactics.Api.Tests.Helpers;

/// <summary>
/// The small, fixed game that API tests play, so editing <c>content/</c> never breaks an API test (the Core tests
/// follow the same rule). It goes through the real <see cref="ContentLoader"/>, from JSON shaped like <c>content/</c>:
/// <code>
///      x: 0 1 2 3
///   y=0   . . . #
///   y=1   . m # .
///   y=2   . W . .
///   y=3   . . . .
/// </code>
/// W is <c>one-warrior</c> (Player One), m is <c>two-mage</c> (Player Two). They start side by side, so a whole
/// game is short: the warrior hits for 10 (mage 14 → 4), the mage hits back for 9, the warrior finishes it.
/// </summary>
public static class TestScenario
{
    /// <summary>The unit types: the plan's placeholder warrior and mage, written here so nobody's balancing changes them.</summary>
    public const string UnitsJson = """
        {
          "unitTypes": [
            { "id": "warrior", "name": "Warrior", "maxHp": 28, "power": 10, "defense": 3, "move": 3, "minRange": 1, "maxRange": 1, "ability": "damage" },
            { "id": "mage", "name": "Mage", "maxHp": 14, "power": 12, "defense": 0, "move": 2, "minRange": 1, "maxRange": 2, "ability": "damage" }
          ]
        }
        """;

    /// <summary>The board above. The obstacles are listed out of reading order on purpose: the API sorts them.</summary>
    public const string ScenarioJson = """
        {
          "id": "test-duel",
          "name": "Test duel",
          "board": { "width": 4, "height": 4, "obstacles": [ { "x": 2, "y": 1 }, { "x": 3, "y": 0 } ] },
          "placements": [
            { "id": "one-warrior", "typeId": "warrior", "owner": "one", "x": 1, "y": 2 },
            { "id": "two-mage", "typeId": "mage", "owner": "two", "x": 1, "y": 1 }
          ]
        }
        """;

    /// <summary>The validated setup.</summary>
    public static GameSetup Setup { get; } = ContentLoader.Load(UnitsJson, ScenarioJson);
}
