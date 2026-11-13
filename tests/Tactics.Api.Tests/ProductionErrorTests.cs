using System.Net;
using Tactics.Api.Tests.Helpers;

namespace Tactics.Api.Tests;

/// <summary>
/// The API outside Development, as it will run in Docker (Step 13). Errors are still ProblemDetails, but they never
/// show internals. Development's error page echoes the exception and every request header, the player token too.
/// </summary>
public sealed class ProductionErrorTests(ProductionApiFactory factory) : IClassFixture<ProductionApiFactory>, IDisposable
{
    private readonly HttpClient _http = factory.CreateClient();

    public void Dispose() => _http.Dispose();

    [Fact]
    public async Task A_malformed_request_is_a_plain_400_that_echoes_nothing()
    {
        var ct = TestContext.Current.CancellationToken;
        var (one, _) = await Lobby.StartGameAsync(_http, ct);

        using var response = await one.PostRawActionAsync("""{ "expectedSequence": 0, "action": { "unitId": "one-warrior" } }""", ct);

        await ApiJson.ReadProblemAsync(response, HttpStatusCode.BadRequest, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        Assert.DoesNotContain(one.Token, body, StringComparison.Ordinal);
        Assert.DoesNotContain("exception", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task An_unknown_URL_is_a_404_ProblemDetails()
    {
        var ct = TestContext.Current.CancellationToken;

        using var response = await _http.GetAsync("/api/nothing-here", ct);

        await ApiJson.ReadProblemAsync(response, HttpStatusCode.NotFound, ct);
    }

    [Theory]
    [InlineData("/openapi/v1.json")]
    [InlineData("/scalar")]
    public async Task The_API_docs_are_a_Development_tool_only(string url)
    {
        using var response = await _http.GetAsync(url, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
