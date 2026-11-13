using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Tactics.Api.Contracts;
using Tactics.Core.Model;

namespace Tactics.Api.Tests.Helpers;

/// <summary>
/// One player at the table: every request carries this player's token, so a test reads like a game
/// (<c>await one.PlayAsync(...)</c>). Several players can share one <see cref="HttpClient"/>.
/// </summary>
public sealed class ApiPlayer(HttpClient http, Guid gameId, PlayerSide side, string token)
{
    /// <summary>The game this player sits at.</summary>
    public Guid GameId { get; } = gameId;

    /// <summary>The seat, as the server gave it.</summary>
    public PlayerSide Side { get; } = side;

    /// <summary>The player token, sent as "Authorization: Bearer …".</summary>
    public string Token { get; } = token;

    /// <summary>The game's URL.</summary>
    public string GameUrl => $"/api/games/{GameId}";

    /// <summary><c>GET</c> the game. The caller checks the answer.</summary>
    public Task<HttpResponseMessage> GetGameAsync(CancellationToken ct) => SendAsync(HttpMethod.Get, GameUrl, null, ct);

    /// <summary>The game as this player sees it. Fails the test unless the answer is 200.</summary>
    public async Task<GameView> GetViewAsync(CancellationToken ct)
    {
        using var response = await GetGameAsync(ct);
        return await ApiJson.ReadAsync<GameView>(response, HttpStatusCode.OK, ct);
    }

    /// <summary>Posts an action (a <see cref="TestActions"/> object) with the given expected sequence.</summary>
    public Task<HttpResponseMessage> PostActionAsync(int expectedSequence, object action, CancellationToken ct) =>
        SendAsync(HttpMethod.Post, $"{GameUrl}/actions", JsonContent.Create(new { expectedSequence, action }), ct);

    /// <summary>Posts a body as is, for requests the API must refuse.</summary>
    public Task<HttpResponseMessage> PostRawActionAsync(string body, CancellationToken ct) =>
        SendAsync(HttpMethod.Post, $"{GameUrl}/actions", new StringContent(body, Encoding.UTF8, "application/json"), ct);

    /// <summary>
    /// Plays an action from the current view, like the client will: fetch the view, send its sequence.
    /// Fails the test unless the server accepts it.
    /// </summary>
    public async Task<SubmitActionResponse> PlayAsync(object action, CancellationToken ct)
    {
        var view = await GetViewAsync(ct);
        using var response = await PostActionAsync(view.Sequence, action, ct);
        return await ApiJson.ReadAsync<SubmitActionResponse>(response, HttpStatusCode.OK, ct);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, HttpContent? content, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, url) { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        return await http.SendAsync(request, ct);
    }
}
