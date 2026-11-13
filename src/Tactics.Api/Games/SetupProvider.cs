using Tactics.Core.Content;
using Tactics.Core.Model;

namespace Tactics.Api.Games;

/// <summary>
/// The setup every new game starts from, built once from the hand-edited files in <c>content/</c>.
/// A game keeps its own reference to it (<see cref="GameRecord.Setup"/>). Setups are immutable, so that
/// reference already is a snapshot: whatever happens to the files later, a running game keeps its stats.
/// </summary>
/// <param name="defaultSetup">The setup new games get.</param>
public sealed class SetupProvider(GameSetup defaultSetup)
{
    /// <summary>The scenario new games use, until players can choose one (backlog).</summary>
    public const string DefaultScenarioId = "classic";

    /// <summary>The setup a new game starts from.</summary>
    public GameSetup Default { get; } = defaultSetup;

    /// <summary>
    /// Reads and validates <c>units.json</c> and <c>scenarios/classic.json</c> from a content folder.
    /// A broken file throws <see cref="ContentException"/>, which lists every problem: the API must not start with it.
    /// </summary>
    /// <param name="contentDirectory">The folder that holds <c>units.json</c> and <c>scenarios/</c>.</param>
    public static SetupProvider LoadFrom(string contentDirectory)
    {
        var unitsJson = File.ReadAllText(Path.Combine(contentDirectory, "units.json"));
        var scenarioJson = File.ReadAllText(Path.Combine(contentDirectory, "scenarios", $"{DefaultScenarioId}.json"));
        return new SetupProvider(ContentLoader.Load(unitsJson, scenarioJson));
    }
}
