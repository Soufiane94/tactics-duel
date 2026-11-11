using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Tactics.Api.Contracts;

namespace Tactics.Api.Tests;

/// <summary>
/// Smoke test: boots the real API in memory (no network, no port) and calls it over HTTP.
/// </summary>
public sealed class HelloEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Get_hello_returns_the_alive_message()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/hello", ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<HelloResponse>(ct);
        Assert.NotNull(body);
        Assert.Equal("API is alive", body.Message);
    }

    [Fact]
    public async Task Get_hello_uses_camelCase_json()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = factory.CreateClient();

        var json = await client.GetStringAsync("/api/hello", ct);

        // The client (TypeScript) reads "message", not "Message" (PLAN.md, Contracts: JSON is camelCase).
        Assert.Equal("""{"message":"API is alive"}""", json);
    }
}
