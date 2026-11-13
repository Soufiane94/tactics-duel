using System.Net;
using System.Net.Http.Json;
using Tactics.Core.Serialization;

namespace Tactics.Api.Tests.Helpers;

/// <summary>Reads API answers in tests: checks the status first, and shows the body when it's not the expected one.</summary>
public static class ApiJson
{
    /// <summary>Reads a successful answer's body.</summary>
    public static async Task<T> ReadAsync<T>(HttpResponseMessage response, HttpStatusCode expected, CancellationToken ct)
    {
        await AssertStatusAsync(response, expected, because: null, ct);
        var value = await response.Content.ReadFromJsonAsync<T>(TacticsJson.Options, ct);
        Assert.NotNull(value);
        return value;
    }

    /// <summary>Reads an error, checking that it's a ProblemDetails body with the expected status.</summary>
    public static async Task<ProblemBody> ReadProblemAsync(
        HttpResponseMessage response, HttpStatusCode expected, CancellationToken ct, string? because = null)
    {
        await AssertStatusAsync(response, expected, because, ct);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemBody>(TacticsJson.Options, ct);
        Assert.NotNull(problem);
        Assert.Equal((int)expected, problem.Status);
        return problem;
    }

    private static async Task AssertStatusAsync(
        HttpResponseMessage response, HttpStatusCode expected, string? because, CancellationToken ct)
    {
        if (response.StatusCode != expected)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            var reason = because is null ? "" : $" for {because}";
            Assert.Fail($"Expected HTTP {(int)expected}{reason}, got {(int)response.StatusCode}. Body: {body}");
        }
    }
}

/// <summary>The ProblemDetails fields the tests look at. <c>code</c> is the API's own extension.</summary>
public sealed record ProblemBody(int Status, string Title, string? Detail, string? Code);
