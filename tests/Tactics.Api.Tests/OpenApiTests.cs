using System.Net;
using System.Text.Json.Nodes;
using Tactics.Api.Tests.Helpers;

namespace Tactics.Api.Tests;

/// <summary>
/// The OpenAPI document that Scalar reads (Development only). It must say which endpoints take the player token,
/// or Scalar can't send it, and it must describe the actions the way clients write them.
/// </summary>
public sealed class OpenApiTests(TestApiFactory factory) : IClassFixture<TestApiFactory>, IDisposable
{
    private readonly HttpClient _http = factory.CreateClient();

    public void Dispose() => _http.Dispose();

    [Fact]
    public async Task The_player_token_is_a_Bearer_scheme_on_exactly_the_endpoints_that_need_it()
    {
        var document = await GetDocumentAsync();

        var scheme = document["components"]?["securitySchemes"]?["playerToken"];
        Assert.Equal(("http", "bearer"), ((string?)scheme?["type"], (string?)scheme?["scheme"]));

        string[] expected = ["get /api/games/{gameId}", "post /api/games/{gameId}/actions"];
        Assert.Equal(expected, OperationsWithSecurity(document));
    }

    [Fact]
    public async Task An_action_is_described_by_its_type_name()
    {
        var document = await GetDocumentAsync();

        var mapping = document["components"]?["schemas"]?["ActionDto"]?["discriminator"]?["mapping"]?.AsObject();

        Assert.NotNull(mapping);
        string[] expected = ["act", "endTurn", "move"];
        Assert.Equal(expected, mapping.Select(entry => entry.Key).Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task Scalar_is_served_in_Development()
    {
        using var response = await _http.GetAsync("/scalar/", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
    }

    private async Task<JsonNode> GetDocumentAsync()
    {
        var json = await _http.GetStringAsync("/openapi/v1.json", TestContext.Current.CancellationToken);
        return JsonNode.Parse(json) ?? throw new InvalidOperationException("The OpenAPI document is empty.");
    }

    // "get /api/games/{gameId}" for every operation that lists a security requirement, sorted.
    private static string[] OperationsWithSecurity(JsonNode document) =>
        [.. (document["paths"]?.AsObject() ?? [])
            .SelectMany(path => (path.Value?.AsObject() ?? [])
                .Where(operation => operation.Value?["security"]?.AsArray().Count > 0)
                .Select(operation => $"{operation.Key} {path.Key}"))
            .Order(StringComparer.Ordinal)];
}
