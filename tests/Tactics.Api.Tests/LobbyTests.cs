using System.Net;
using System.Net.Http.Json;
using Tactics.Api.Contracts;
using Tactics.Api.Tests.Helpers;
using Tactics.Core.Model;

namespace Tactics.Api.Tests;

/// <summary>Creating a game and taking seat two: tokens and an invite code instead of accounts.</summary>
public sealed class LobbyTests(TestApiFactory factory) : IClassFixture<TestApiFactory>, IDisposable
{
    private readonly HttpClient _http = factory.CreateClient();

    public void Dispose() => _http.Dispose();

    [Fact]
    public async Task Creating_a_game_gives_seat_one_a_token_an_invite_code_and_the_view()
    {
        var ct = TestContext.Current.CancellationToken;

        using var response = await _http.PostAsync("/api/games", content: null, ct);

        var created = await ApiJson.ReadAsync<CreateGameResponse>(response, HttpStatusCode.Created, ct);
        Assert.Equal(PlayerSide.One, created.Side);
        Assert.Matches("^[A-Za-z0-9_-]{43}$", created.PlayerToken);
        Assert.Matches("^[A-HJ-NP-Z2-9]{8}$", created.InviteCode);
        Assert.Equal(7, created.GameId.Version); // time-ordered ids

        var view = created.View;
        Assert.Equal((created.GameId, 0, PlayerSide.One), (view.GameId, view.Sequence, view.YourSide));
        Assert.False(view.OpponentJoined);
        // Player One may play turn 1 before anyone joins.
        Assert.NotEmpty(view.AvailableActions);
    }

    [Fact]
    public async Task The_new_game_s_Location_header_points_to_the_game()
    {
        var ct = TestContext.Current.CancellationToken;
        using var response = await _http.PostAsync("/api/games", content: null, ct);
        var created = await ApiJson.ReadAsync<CreateGameResponse>(response, HttpStatusCode.Created, ct);

        Assert.Equal($"/api/games/{created.GameId}", response.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task Joining_with_the_invite_code_takes_seat_two()
    {
        var ct = TestContext.Current.CancellationToken;
        var created = await Lobby.CreateGameAsync(_http, ct);

        using var response = await Lobby.PostJoinAsync(_http, created.GameId, created.InviteCode, ct);

        var joined = await ApiJson.ReadAsync<JoinGameResponse>(response, HttpStatusCode.OK, ct);
        Assert.Equal((created.GameId, PlayerSide.Two), (joined.GameId, joined.Side));
        Assert.NotEqual(created.PlayerToken, joined.PlayerToken);
        Assert.Equal(PlayerSide.Two, joined.View.YourSide);
        Assert.True(joined.View.OpponentJoined);
        Assert.Empty(joined.View.AvailableActions); // it's Player One's turn

        // Player One now sees an opponent too.
        var one = new ApiPlayer(_http, created.GameId, PlayerSide.One, created.PlayerToken);
        Assert.True((await one.GetViewAsync(ct)).OpponentJoined);
    }

    [Fact]
    public async Task A_wrong_invite_code_is_403_InvalidInviteCode()
    {
        var ct = TestContext.Current.CancellationToken;
        var created = await Lobby.CreateGameAsync(_http, ct);

        using var response = await Lobby.PostJoinAsync(_http, created.GameId, "WRONGXYZ", ct);

        var problem = await ApiJson.ReadProblemAsync(response, HttpStatusCode.Forbidden, ct);
        Assert.Equal("InvalidInviteCode", problem.Code);
    }

    [Fact]
    public async Task Joining_twice_is_409_AlreadyJoined()
    {
        var ct = TestContext.Current.CancellationToken;
        var created = await Lobby.CreateGameAsync(_http, ct);
        await Lobby.JoinGameAsync(_http, created.GameId, created.InviteCode, ct);

        // The right code, but seat two is taken: an invite works once.
        using var response = await Lobby.PostJoinAsync(_http, created.GameId, created.InviteCode, ct);

        var problem = await ApiJson.ReadProblemAsync(response, HttpStatusCode.Conflict, ct);
        Assert.Equal("AlreadyJoined", problem.Code);
    }

    [Fact]
    public async Task Joining_an_unknown_game_is_404()
    {
        var ct = TestContext.Current.CancellationToken;

        using var response = await Lobby.PostJoinAsync(_http, Guid.NewGuid(), "K7QF3MZP", ct);

        await ApiJson.ReadProblemAsync(response, HttpStatusCode.NotFound, ct);
    }

    [Fact]
    public async Task A_join_without_an_invite_code_is_400()
    {
        var ct = TestContext.Current.CancellationToken;
        var created = await Lobby.CreateGameAsync(_http, ct);

        using var response = await _http.PostAsJsonAsync($"/api/games/{created.GameId}/join", new { }, ct);

        await ApiJson.ReadProblemAsync(response, HttpStatusCode.BadRequest, ct);
    }
}
