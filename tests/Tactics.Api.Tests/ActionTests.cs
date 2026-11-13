using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Tactics.Api.Contracts;
using Tactics.Api.Tests.Helpers;
using Tactics.Core.Serialization;

namespace Tactics.Api.Tests;

/// <summary>
/// <c>POST /api/games/{gameId}/actions</c>: the server is the referee. It checks the player, then the sequence,
/// then the rules, and only stores what passed all three.
/// </summary>
public sealed class ActionTests(TestApiFactory factory) : IClassFixture<TestApiFactory>, IDisposable
{
    private const HttpStatusCode UnprocessableContent = (HttpStatusCode)422;

    private readonly HttpClient _http = factory.CreateClient();

    public static TheoryData<string, string> MalformedBodies => new()
    {
        { "an action without a type", """{ "expectedSequence": 0, "action": { "unitId": "one-warrior", "to": { "x": 2, "y": 2 } } }""" },
        { "an unknown action type", """{ "expectedSequence": 0, "action": { "type": "teleport", "unitId": "one-warrior" } }""" },
        { "no expectedSequence", """{ "action": { "type": "endTurn" } }""" },
        { "a number written as text", """{ "expectedSequence": "0", "action": { "type": "endTurn" } }""" },
        { "no action", """{ "expectedSequence": 0 }""" },
        { "a move without its unit", """{ "expectedSequence": 0, "action": { "type": "move", "to": { "x": 2, "y": 2 } } }""" },
        { "a null unit id", """{ "expectedSequence": 0, "action": { "type": "move", "unitId": null, "to": { "x": 2, "y": 2 } } }""" },
        { "text that isn't JSON", "move the warrior please" },
    };

    public void Dispose() => _http.Dispose();

    [Fact]
    public async Task A_move_answers_with_its_events_and_the_new_view()
    {
        var ct = TestContext.Current.CancellationToken;
        var (one, _) = await Lobby.StartGameAsync(_http, ct);

        using var response = await one.PostActionAsync(expectedSequence: 0, TestActions.Move("one-warrior", 2, 2), ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = JsonNode.Parse(await response.Content.ReadAsStringAsync(ct));
        Assert.NotNull(body);
        Assert.Equal(1, (int?)body["sequence"]);

        // The event exactly as PLAN.md's contract spells it: "type" first, and a path without the start tile.
        Assert.Equal(
            """[{"type":"unitMoved","unitId":"one-warrior","from":{"x":1,"y":2},"to":{"x":2,"y":2},"path":[{"x":2,"y":2}]}]""",
            body["events"]?.ToJsonString());

        var view = body["view"].Deserialize<GameView>(TacticsJson.Options);
        Assert.NotNull(view);
        Assert.Equal(1, view.Sequence);
        var warrior = Assert.Single(view.Units, unit => unit.Id == "one-warrior");
        Assert.Equal((2, 2, true, false), (warrior.X, warrior.Y, warrior.HasMoved, warrior.HasActed));
        // From (2, 2) the mage is 2 tiles away, beyond the warrior's reach: only ending the turn is left.
        Assert.Empty(view.AvailableActions);
    }

    [Fact]
    public async Task Playing_out_of_turn_is_422_NotYourTurn()
    {
        var ct = TestContext.Current.CancellationToken;
        var (_, two) = await Lobby.StartGameAsync(_http, ct);

        using var response = await two.PostActionAsync(0, TestActions.Move("two-mage", 0, 1), ct);

        var problem = await ApiJson.ReadProblemAsync(response, UnprocessableContent, ct);
        Assert.Equal(("Rule violation", "NotYourTurn"), (problem.Title, problem.Code));
        Assert.Equal("It's Player One's turn.", problem.Detail); // Core's own message
    }

    [Fact]
    public async Task A_client_can_t_claim_to_be_the_other_player()
    {
        var ct = TestContext.Current.CancellationToken;
        var (_, two) = await Lobby.StartGameAsync(_http, ct);

        // A cheating client adds "player": "one" to end Player One's turn with Player Two's token.
        using var response = await two.PostActionAsync(0, new { type = "endTurn", player = "one" }, ct);

        // The server ignores the field: the side comes from the token. Still Player Two, still not their turn.
        var problem = await ApiJson.ReadProblemAsync(response, UnprocessableContent, ct);
        Assert.Equal("NotYourTurn", problem.Code);
    }

    [Fact]
    public async Task A_second_click_with_the_same_sequence_is_409_SequenceConflict()
    {
        var ct = TestContext.Current.CancellationToken;
        var (one, _) = await Lobby.StartGameAsync(_http, ct);
        using var first = await one.PostActionAsync(0, TestActions.EndTurn, ct);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        // The same request again, as a double click sends it: made from a view that's out of date now.
        using var second = await one.PostActionAsync(0, TestActions.EndTurn, ct);

        // 409, not 422 NotYourTurn: the sequence is checked before the rules.
        var problem = await ApiJson.ReadProblemAsync(second, HttpStatusCode.Conflict, ct);
        Assert.Equal("SequenceConflict", problem.Code);
        Assert.Equal(1, (await one.GetViewAsync(ct)).Sequence); // played once, not twice
    }

    [Fact]
    public async Task A_broken_rule_is_422_with_Core_s_code_and_nothing_is_stored()
    {
        var ct = TestContext.Current.CancellationToken;
        var (one, _) = await Lobby.StartGameAsync(_http, ct);

        // (2, 1) is an obstacle.
        using var response = await one.PostActionAsync(0, TestActions.Move("one-warrior", 2, 1), ct);

        var problem = await ApiJson.ReadProblemAsync(response, UnprocessableContent, ct);
        Assert.Equal("DestinationNotReachable", problem.Code);
        Assert.False(string.IsNullOrWhiteSpace(problem.Detail));
        Assert.Equal(0, (await one.GetViewAsync(ct)).Sequence);
    }

    [Fact]
    public async Task Playing_needs_a_player_token()
    {
        var ct = TestContext.Current.CancellationToken;
        var (one, _) = await Lobby.StartGameAsync(_http, ct);

        using var response = await _http.PostAsJsonAsync(
            $"{one.GameUrl}/actions", new { expectedSequence = 0, action = TestActions.EndTurn }, ct);

        await ApiJson.ReadProblemAsync(response, HttpStatusCode.Unauthorized, ct);
        Assert.Equal(0, (await one.GetViewAsync(ct)).Sequence);
    }

    [Theory]
    [MemberData(nameof(MalformedBodies))]
    public async Task A_malformed_request_is_400_ProblemDetails(string what, string body)
    {
        var ct = TestContext.Current.CancellationToken;
        var (one, _) = await Lobby.StartGameAsync(_http, ct);

        using var response = await one.PostRawActionAsync(body, ct);

        await ApiJson.ReadProblemAsync(response, HttpStatusCode.BadRequest, ct, because: what);
        Assert.Equal(0, (await one.GetViewAsync(ct)).Sequence);
    }
}
