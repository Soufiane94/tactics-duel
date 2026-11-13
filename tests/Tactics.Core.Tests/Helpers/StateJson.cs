using System.Text.Json;
using Tactics.Core.Model;
using Tactics.Core.Serialization;

namespace Tactics.Core.Tests.Helpers;

/// <summary>
/// Compares game states through their JSON. A record compares a list by reference, so two states holding the same
/// units are not Equal (see <see cref="GameEngineReplayTests"/>). The JSON holds every value, so the same JSON
/// means the same game, and a property added later is compared too, with no code to update.
/// </summary>
public static class StateJson
{
    /// <summary>The state as JSON, with the options the whole project shares.</summary>
    public static string Of(GameState state) => JsonSerializer.Serialize(state, TacticsJson.Options);

    /// <summary>
    /// Fails unless both states have the same JSON, and shows both if they differ. Compare states that come from the
    /// same setup: a hash set or a dictionary (obstacles, unit types) lists its items in an order you don't control.
    /// </summary>
    public static void AssertSame(GameState expected, GameState actual) => Assert.Equal(Of(expected), Of(actual));
}
