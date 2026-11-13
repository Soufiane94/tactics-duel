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

    /// <summary>
    /// Applies the project's JSON conventions to options created elsewhere, such as ASP.NET Core's HTTP options.
    /// The API's wire format and Core's JSON then come from this one method and can't drift apart.
    /// </summary>
    /// <param name="options">Options that aren't read-only yet. They get one enum converter per call.</param>
    public static void Configure(JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        // camelCase property names ("unitId"), read back ignoring case. These are also the web defaults.
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.PropertyNameCaseInsensitive = true;

        // Read the "type" field of actions and events wherever it is in the object, not only first.
        // PostgreSQL jsonb reorders keys (shortest first), so a stored move comes back as
        // {"to": …, "type": "move", …}. Hand-written JSON gets the same freedom. The cost is extra
        // buffering, only when "type" isn't first.
        options.AllowOutOfOrderMetadataProperties = true;

        // Enums as camelCase strings. Numbers such as "owner": 0 are rejected: unreadable in a hand-edited file.
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
    }

    private static JsonSerializerOptions CreateOptions()
    {
        // Web defaults: camelCase property names, case-insensitive reading.
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            // Named explicitly, so these options work even in apps that switch reflection-based JSON off
            // by default (.NET 10 single-file apps, Native AOT). Otherwise MakeReadOnly throws there.
            TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        };

        Configure(options);
        options.MakeReadOnly();
        return options;
    }
}
