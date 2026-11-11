namespace Tactics.Core.Tests.Helpers;

/// <summary>
/// Reads the real, hand-edited files from content/. The test project copies them next to the test DLL.
/// File I/O lives here in the tests, not in Tactics.Core, which only ever sees JSON text.
/// </summary>
public static class RealContent
{
    /// <summary>The text of content/units.json.</summary>
    public static string UnitsJson => Read("units.json");

    /// <summary>The text of content/scenarios/classic.json.</summary>
    public static string ClassicScenarioJson => Read(Path.Combine("scenarios", "classic.json"));

    private static string Read(string relativePath) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "content", relativePath));
}
