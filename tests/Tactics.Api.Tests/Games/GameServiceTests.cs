using Microsoft.Extensions.Logging.Abstractions;
using Tactics.Api.Contracts;
using Tactics.Api.Games;
using Tactics.Api.Tests.Helpers;
using Tactics.Core;

namespace Tactics.Api.Tests.Games;

/// <summary>
/// <see cref="GameService"/> called directly, without HTTP: the order of its checks, and its clock.
/// It's a plain class with constructor injection, so a test builds it like any other object.
/// </summary>
public sealed class GameServiceTests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 13, 9, 0, 0, TimeSpan.Zero);

    private readonly InMemoryGameRepository _repository = new();
    private readonly FakeClock _clock = new(Start);
    private readonly GameService _service;

    public GameServiceTests() =>
        _service = new GameService(_repository, new SetupProvider(TestScenario.Setup), _clock, NullLogger<GameService>.Instance);

    [Fact]
    public async Task Every_timestamp_comes_from_the_injected_clock()
    {
        var ct = TestContext.Current.CancellationToken;

        var created = await _service.CreateGameAsync(ct);
        _clock.Now = Start.AddMinutes(1);
        await _service.JoinGameAsync(created.GameId, created.InviteCode, ct);
        var afterJoin = await _repository.GetAsync(created.GameId, ct);
        _clock.Now = Start.AddMinutes(2);
        await _service.SubmitActionAsync(created.GameId, created.PlayerToken, 0, new EndTurnActionDto(), ct);
        var afterAction = await _repository.GetAsync(created.GameId, ct);

        Assert.NotNull(afterJoin);
        Assert.NotNull(afterAction);
        Assert.Equal((Start, Start.AddMinutes(1)), (afterJoin.CreatedAt, afterJoin.UpdatedAt));
        Assert.Equal((Start, Start.AddMinutes(2)), (afterAction.CreatedAt, afterAction.UpdatedAt));
    }

    [Fact]
    public async Task A_game_id_is_a_version_7_GUID_that_starts_with_the_clock_s_time()
    {
        var created = await _service.CreateGameAsync(TestContext.Current.CancellationToken);

        Assert.Equal(7, created.GameId.Version);
        // The first 48 bits of a version 7 GUID are a Unix time in milliseconds: 12 hex digits.
        Assert.Equal(Start.ToUnixTimeMilliseconds(), Convert.ToInt64(created.GameId.ToString("N")[..12], 16));
    }

    [Fact]
    public async Task A_stranger_is_turned_away_before_anything_else_is_checked()
    {
        var ct = TestContext.Current.CancellationToken;
        var (gameId, _, _) = await StartGameAsync();

        // A made-up token, an old sequence and an illegal action, all at once: the first check answers.
        var result = await _service.SubmitActionAsync(gameId, "made-up", 99, new EndTurnActionDto(), ct);

        Assert.Equal(ServiceErrorKind.NotAPlayer, ErrorOf(result).Kind);
    }

    [Fact]
    public async Task An_out_of_date_view_is_reported_before_a_broken_rule()
    {
        var ct = TestContext.Current.CancellationToken;
        var (gameId, _, twoToken) = await StartGameAsync();

        // Player Two is out of turn AND acts from an old view. The view comes first: once refreshed, Player Two
        // sees whose turn it is and won't even try.
        var result = await _service.SubmitActionAsync(gameId, twoToken, 5, new EndTurnActionDto(), ct);

        Assert.Equal(ServiceErrorKind.SequenceConflict, ErrorOf(result).Kind);
    }

    [Fact]
    public async Task A_rule_violation_carries_Core_s_code_and_message()
    {
        var ct = TestContext.Current.CancellationToken;
        var (gameId, _, twoToken) = await StartGameAsync();

        var result = await _service.SubmitActionAsync(gameId, twoToken, 0, new EndTurnActionDto(), ct);

        var error = ErrorOf(result);
        Assert.Equal((ServiceErrorKind.RuleViolation, RuleViolation.NotYourTurn), (error.Kind, error.Rule));
        Assert.Equal("It's Player One's turn.", error.Message);
    }

    [Fact]
    public async Task A_wrong_invite_code_says_nothing_about_the_seat()
    {
        var ct = TestContext.Current.CancellationToken;
        var (gameId, _, _) = await StartGameAsync();

        // Seat two is taken, but a wrong code only learns that the code is wrong.
        var result = await _service.JoinGameAsync(gameId, "WRONGXYZ", ct);

        Assert.Equal(ServiceErrorKind.InvalidInviteCode, ErrorOf(result).Kind);
    }

    private async Task<(Guid GameId, string OneToken, string TwoToken)> StartGameAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        var created = await _service.CreateGameAsync(ct);
        var joined = await _service.JoinGameAsync(created.GameId, created.InviteCode, ct);
        var seatTwo = Assert.IsType<ServiceResult<JoinGameResponse>.Succeeded>(joined).Value;
        return (created.GameId, created.PlayerToken, seatTwo.PlayerToken);
    }

    private static ServiceError ErrorOf<T>(ServiceResult<T> result) =>
        Assert.IsType<ServiceResult<T>.Failed>(result).Error;
}
