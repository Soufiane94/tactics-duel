using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Tactics.Core.Serialization;

/// <summary>
/// The one JSON setup shared by content files, replays and the API, so every JSON in the project
/// has the same shape: camelCase names, and enums as camelCase strings ("one", "inProgress").
/// </summary>
public static class TacticsJson
{
    /// <summary>
    /// Shared serializer options, read-only so nobody can change them after first use.
    /// Reusing one instance also lets System.Text.Json cache what it learns about each type.
    /// </summary>
    public static JsonSerializerOptions Options { get; } = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        // Web defaults: camelCase property names, case-insensitive reading.
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            // Named explicitly, so these options work even in apps that switch reflection-based JSON off
            // by default (.NET 10 single-file apps, Native AOT). Otherwise MakeReadOnly throws there.
            TypeInfoResolver = new DefaultJsonTypeInfoResolver(),

            // Read the "type" field of actions and events wherever it is in the object, not only first.
            // PostgreSQL jsonb reorders keys (shortest first), so a stored move comes back as
            // {"to": …, "type": "move", …}. Hand-written JSON gets the same freedom. The cost is extra
            // buffering, only when "type" isn't first.
            AllowOutOfOrderMetadataProperties = true,
        };

        // Enums as camelCase strings. Numbers such as "owner": 0 are rejected: unreadable in a hand-edited file.
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));

        options.MakeReadOnly();
        return options;
    }
}
