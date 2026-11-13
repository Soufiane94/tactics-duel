using System.Net;
using System.Net.Http.Json;
using Tactics.Api.Contracts;
using Tactics.Core.Model;

namespace Tactics.Api.Tests.Helpers;

/// <summary>Creating and joining games, the way the client will: plain HTTP, no token needed.</summary>
public static class Lobby
{
    /// <summary><c>POST /api/games</c>. Fails the test unless the answer is 201.</summary>
    public static async Task<CreateGameResponse> CreateGameAsync(HttpClient http, CancellationToken ct)
    {
        using var response = await http.PostAsync("/api/games", content: null, ct);
        return await ApiJson.ReadAsync<CreateGameResponse>(response, HttpStatusCode.Created, ct);
    }

    /// <summary><c>POST /api/games/{gameId}/join</c>. The caller checks the answer.</summary>
    public static Task<HttpResponseMessage> PostJoinAsync(HttpClient http, Guid gameId, string inviteCode, CancellationToken ct) =>
        http.PostAsJsonAsync($"/api/games/{gameId}/join", new { inviteCode }, ct);

    /// <summary>Joins as Player Two. Fails the test unless the answer is 200.</summary>
    public static async Task<JoinGameResponse> JoinGameAsync(HttpClient http, Guid gameId, string inviteCode, CancellationToken ct)
    {
        using var response = await PostJoinAsync(http, gameId, inviteCode, ct);
        return await ApiJson.ReadAsync<JoinGameResponse>(response, HttpStatusCode.OK, ct);
    }

    /// <summary>Creates a game and joins it: both seats taken, Player One to play.</summary>
    public static async Task<(ApiPlayer One, ApiPlayer Two)> StartGameAsync(HttpClient http, CancellationToken ct)
    {
        var created = await CreateGameAsync(http, ct);
        var joined = await JoinGameAsync(http, created.GameId, created.InviteCode, ct);
        return (
            new ApiPlayer(http, created.GameId, PlayerSide.One, created.PlayerToken),
            new ApiPlayer(http, created.GameId, PlayerSide.Two, joined.PlayerToken));
    }
}

/// <summary>Actions as plain JSON objects, shaped the way the TypeScript client will send them.</summary>
public static class TestActions
{
    /// <summary><c>{ "type": "move", "unitId": …, "to": { "x": …, "y": … } }</c></summary>
    public static object Move(string unitId, int x, int y) => new { type = "move", unitId, to = new { x, y } };

    /// <summary><c>{ "type": "act", "actorId": …, "targetId": … }</c></summary>
    public static object Act(string actorId, string targetId) => new { type = "act", actorId, targetId };

    /// <summary><c>{ "type": "endTurn" }</c></summary>
    public static object EndTurn { get; } = new { type = "endTurn" };
}
