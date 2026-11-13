using System.Net.Http.Headers;

namespace Tactics.Api.Http;

/// <summary>
/// Reads a player token from <c>Authorization: Bearer &lt;token&gt;</c>, the standard header for tokens.
/// There's no ASP.NET Identity and no authentication middleware: a token only means something for one game,
/// so <c>GameService</c> checks it against that game's stored hashes.
/// </summary>
public static class BearerToken
{
    /// <summary>The token, or null if the header is missing or isn't a Bearer one.</summary>
    public static string? Read(HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return AuthenticationHeaderValue.TryParse(request.Headers.Authorization, out var header)
            && string.Equals(header.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(header.Parameter)
                ? header.Parameter
                : null;
    }
}
