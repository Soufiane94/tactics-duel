using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Tactics.Core.Serialization;

namespace Tactics.Api.Contracts;

/// <summary>
/// The API's JSON settings, in one place: Core's conventions for every body, plus strict reading of requests,
/// so a malformed request is always a 400 and never a silent default or a 500.
/// </summary>
public static class ContractJson
{
    /// <summary>Applies the settings to ASP.NET Core's HTTP JSON options (or to any options not yet read-only).</summary>
    public static void Configure(JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        // camelCase, enums as camelCase strings, "type" anywhere in an object: the same JSON as Core's.
        TacticsJson.Configure(options);

        // A missing or null required field is an error, not a default: a forgotten "expectedSequence" would
        // otherwise read as 0, and a missing "unitId" as null.
        options.RespectRequiredConstructorParameters = true;
        options.RespectNullableAnnotations = true;

        // Numbers are JSON numbers. The web defaults also accept "12" in quotes, and the OpenAPI document then
        // advertises every number as "integer or string".
        options.NumberHandling = JsonNumberHandling.Strict;

        options.TypeInfoResolver = (options.TypeInfoResolver ?? new DefaultJsonTypeInfoResolver())
            .WithAddedModifier(RequireActionType);
    }

    // An action without "type" must be a JSON error (HTTP 400), as an unknown "type" already is. Without this,
    // System.Text.Json falls back to ActionDto itself, can't create it (it's abstract) and throws
    // NotSupportedException, which ASP.NET Core reports as a 500: a client's mistake would look like a server bug.
    // With a "type", System.Text.Json creates the derived record and never calls this.
    private static void RequireActionType(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Type == typeof(ActionDto))
        {
            typeInfo.CreateObject = () =>
                throw new JsonException("An action needs a \"type\": \"move\", \"act\" or \"endTurn\".");
        }
    }
}
