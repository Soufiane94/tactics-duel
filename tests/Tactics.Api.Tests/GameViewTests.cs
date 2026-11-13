using System.Net;
using System.Text.Json.Nodes;
using Tactics.Api.Tests.Helpers;
using Tactics.Core.Model;

namespace Tactics.Api.Tests;

/// <summary><c>GET /api/games/{gameId}</c>: each player gets their own view, and only players get one.</summary>
public sealed class GameViewTests(TestApiFactory factory) : IClassFixture<TestApiFactory>, IDisposable
{
    private readonly HttpClient _http = factory.CreateClient();

    public void Dispose() => _http.Dispose();

    [Fact]
    public async Task Each_player_sees_their_own_side_and_only_the_current_player_gets_actions()
    {
        var ct = TestContext.Current.CancellationToken;
        var (one, two) = await Lobby.StartGameAsync(_http, ct);

        var oneView = await one.GetViewAsync(ct);
        var twoView = await two.GetViewAsync(ct);

        Assert.Equal((PlayerSide.One, PlayerSide.Two), (oneView.YourSide, twoView.YourSide));
        Assert.Equal(oneView.Units, twoView.Units); // the same game, seen from two seats

        var warrior = Assert.Single(oneView.AvailableActions);
        Assert.Equal("one-warrior", warrior.UnitId);
        Assert.Equal(10, warrior.MoveTargets.Count); // count them in TestScenario's picture: 3 + 4 + 3 steps away
        Assert.Equal("two-mage", Assert.Single(warrior.ActTargets));
        Assert.Empty(twoView.AvailableActions);
    }

    [Fact]
    public async Task The_waiting_player_s_view_has_the_contract_shape()
    {
        var ct = TestContext.Current.CancellationToken;
        var (_, two) = await Lobby.StartGameAsync(_http, ct);

        using var response = await two.GetGameAsync(ct);
        var json = await response.Content.ReadAsStringAsync(ct);

        // PLAN.md's contract, character for character: camelCase names, enums as camelCase strings, winner written
        // as null, obstacles in reading order, unit types sorted by id, no actions for the side that waits.
        var expected = $$"""
            {
              "gameId": "{{two.GameId}}",
              "sequence": 0,
              "yourSide": "two",
              "currentPlayer": "one",
              "turnNumber": 1,
              "status": "inProgress",
              "winner": null,
              "opponentJoined": true,
              "board": { "width": 4, "height": 4, "obstacles": [{ "x": 3, "y": 0 }, { "x": 2, "y": 1 }] },
              "unitTypes": [
                { "id": "mage", "name": "Mage", "maxHp": 14, "power": 12, "defense": 0, "move": 2, "minRange": 1, "maxRange": 2, "ability": "damage" },
                { "id": "warrior", "name": "Warrior", "maxHp": 28, "power": 10, "defense": 3, "move": 3, "minRange": 1, "maxRange": 1, "ability": "damage" }
              ],
              "units": [
                { "id": "one-warrior", "typeId": "warrior", "owner": "one", "x": 1, "y": 2, "hp": 28, "hasMoved": false, "hasActed": false },
                { "id": "two-mage", "typeId": "mage", "owner": "two", "x": 1, "y": 1, "hp": 14, "hasMoved": false, "hasActed": false }
              ],
              "availableActions": []
            }
            """;
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(Compact(expected), json);
    }

    [Fact]
    public async Task A_missing_token_is_401_with_a_Bearer_challenge()
    {
        var ct = TestContext.Current.CancellationToken;
        var (one, _) = await Lobby.StartGameAsync(_http, ct);

        using var response = await _http.GetAsync(one.GameUrl, ct); // no Authorization header

        var problem = await ApiJson.ReadProblemAsync(response, HttpStatusCode.Unauthorized, ct);
        Assert.Equal("Bearer", response.Headers.WwwAuthenticate.ToString());
        Assert.Contains("Authorization: Bearer", problem.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_made_up_token_is_401()
    {
        var ct = TestContext.Current.CancellationToken;
        var (one, _) = await Lobby.StartGameAsync(_http, ct);
        var guesser = new ApiPlayer(_http, one.GameId, PlayerSide.One, "let-me-in");

        using var response = await guesser.GetGameAsync(ct);

        await ApiJson.ReadProblemAsync(response, HttpStatusCode.Unauthorized, ct);
    }

    [Fact]
    public async Task Another_game_s_token_is_401_because_tokens_belong_to_one_game()
    {
        var ct = TestContext.Current.CancellationToken;
        var (mine, _) = await Lobby.StartGameAsync(_http, ct);
        var (theirs, _) = await Lobby.StartGameAsync(_http, ct);
        var intruder = new ApiPlayer(_http, mine.GameId, PlayerSide.One, theirs.Token);

        using var response = await intruder.GetGameAsync(ct);

        await ApiJson.ReadProblemAsync(response, HttpStatusCode.Unauthorized, ct);
    }

    [Fact]
    public async Task An_unknown_game_is_404()
    {
        var ct = TestContext.Current.CancellationToken;
        var (one, _) = await Lobby.StartGameAsync(_http, ct);
        var lost = new ApiPlayer(_http, Guid.NewGuid(), PlayerSide.One, one.Token);

        using var response = await lost.GetGameAsync(ct);

        await ApiJson.ReadProblemAsync(response, HttpStatusCode.NotFound, ct);
    }

    [Fact]
    public async Task A_game_id_that_isn_t_a_GUID_is_a_404_ProblemDetails_too()
    {
        var ct = TestContext.Current.CancellationToken;

        // No route matches, so it never reaches GameService: UseStatusCodePages writes the ProblemDetails.
        using var response = await _http.GetAsync("/api/games/not-a-guid", ct);

        await ApiJson.ReadProblemAsync(response, HttpStatusCode.NotFound, ct);
    }

    // The same JSON without the indentation, so a readable expected value compares with the compact API output.
    private static string Compact(string json) => JsonNode.Parse(json)?.ToJsonString() ?? "";
}
