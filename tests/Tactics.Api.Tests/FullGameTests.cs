using System.Net;
using Tactics.Api.Contracts;
using Tactics.Api.Tests.Helpers;
using Tactics.Core.Model;

namespace Tactics.Api.Tests;

/// <summary>
/// A whole game over HTTP, from creation to victory, on <see cref="TestScenario"/>. Every line is a real request
/// with a real token, so this is what a client does. Read it top to bottom: it's the game.
/// </summary>
public sealed class FullGameTests(TestApiFactory factory) : IClassFixture<TestApiFactory>, IDisposable
{
    private readonly HttpClient _http = factory.CreateClient();

    public void Dispose() => _http.Dispose();

    [Fact]
    public async Task A_whole_game_plays_over_HTTP_until_Player_One_wins()
    {
        var ct = TestContext.Current.CancellationToken;
        var (one, two) = await Lobby.StartGameAsync(_http, ct);

        // Turn 1. The warrior hits the mage next to it: 10 power − 0 defense = 10, so 14 → 4. Then it ends the turn.
        var hit = await one.PlayAsync(TestActions.Act("one-warrior", "two-mage"), ct);
        Assert.Equal(new UnitDamagedDto("two-mage", "one-warrior", Amount: 10, HpAfter: 4), Assert.Single(hit.Events));
        var endOfOne = await one.PlayAsync(TestActions.EndTurn, ct);
        Assert.Equal(new TurnEndedDto(PlayerSide.One, PlayerSide.Two, TurnNumber: 2), Assert.Single(endOfOne.Events));

        // Turn 2. The mage strikes back: 12 power − 3 defense = 9, so 28 → 19.
        var strike = await two.PlayAsync(TestActions.Act("two-mage", "one-warrior"), ct);
        Assert.Equal(new UnitDamagedDto("one-warrior", "two-mage", Amount: 9, HpAfter: 19), Assert.Single(strike.Events));
        await two.PlayAsync(TestActions.EndTurn, ct);

        // Turn 3. The warrior finishes the mage: the hit, the defeat, then the win, in the order they're animated.
        var finish = await one.PlayAsync(TestActions.Act("one-warrior", "two-mage"), ct);
        Assert.Equal<EventDto>(
            [new UnitDamagedDto("two-mage", "one-warrior", 10, 0), new UnitDefeatedDto("two-mage"), new GameWonDto(PlayerSide.One)],
            finish.Events);
        Assert.Equal(5, finish.Sequence);
        Assert.Equal((GameStatus.Finished, PlayerSide.One), (finish.View.Status, finish.View.Winner));

        // Player Two sees the same ending, has nothing left to do, and can't play on.
        var twoView = await two.GetViewAsync(ct);
        Assert.Equal((GameStatus.Finished, PlayerSide.One, 5), (twoView.Status, twoView.Winner, twoView.Sequence));
        Assert.Empty(twoView.AvailableActions);

        using var tooLate = await two.PostActionAsync(5, TestActions.EndTurn, ct);
        var problem = await ApiJson.ReadProblemAsync(tooLate, (HttpStatusCode)422, ct);
        Assert.Equal("GameOver", problem.Code);
    }
}
