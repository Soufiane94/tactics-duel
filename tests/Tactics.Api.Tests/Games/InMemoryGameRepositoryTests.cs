using Tactics.Api.Games;
using Tactics.Api.Tests.Helpers;
using Tactics.Core.Actions;
using Tactics.Core.Model;

namespace Tactics.Api.Tests.Games;

/// <summary>
/// The store's two promises: a record you read never changes, and a write that expects an old state loses cleanly.
/// The per-game lock is what keeps the second promise when requests arrive at the same moment.
/// </summary>
public sealed class InMemoryGameRepositoryTests
{
    private static readonly DateTimeOffset Created = new(2026, 11, 13, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Later = Created.AddMinutes(5);
    private static readonly GameAction EndTurn = new EndTurnAction(PlayerSide.One);

    private readonly InMemoryGameRepository _repository = new();

    [Fact]
    public async Task A_stored_game_reads_back_and_an_unknown_id_reads_as_null()
    {
        var ct = TestContext.Current.CancellationToken;
        var game = await AddGameAsync();

        Assert.Same(game, await _repository.GetAsync(game.Id, ct));
        Assert.Null(await _repository.GetAsync(Guid.NewGuid(), ct));
    }

    [Fact]
    public async Task Adding_a_second_game_with_the_same_id_throws()
    {
        var game = await AddGameAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _repository.AddAsync(game, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task An_append_with_the_current_sequence_becomes_the_next_action()
    {
        var ct = TestContext.Current.CancellationToken;
        var game = await AddGameAsync();

        Assert.True(await _repository.TryAppendAsync(game.Id, expectedSequence: 0, EndTurn, Later, ct));

        var stored = await _repository.GetAsync(game.Id, ct);
        Assert.NotNull(stored);
        Assert.Equal(1, stored.Sequence);
        Assert.Equal(EndTurn, stored.Actions[0]);
        Assert.Equal(Later, stored.UpdatedAt);
        Assert.Equal(Created, stored.CreatedAt);
    }

    [Fact]
    public async Task An_append_that_expects_an_old_sequence_is_refused()
    {
        var ct = TestContext.Current.CancellationToken;
        var game = await AddGameAsync();

        // A double click: both requests were made from the same view, with 0 actions played.
        Assert.True(await _repository.TryAppendAsync(game.Id, expectedSequence: 0, EndTurn, Later, ct));
        Assert.False(await _repository.TryAppendAsync(game.Id, expectedSequence: 0, EndTurn, Later, ct));

        Assert.Equal(1, (await _repository.GetAsync(game.Id, ct))?.Sequence);
    }

    [Fact]
    public async Task Only_one_of_many_simultaneous_appends_with_the_same_sequence_wins()
    {
        var ct = TestContext.Current.CancellationToken;
        var game = await AddGameAsync();

        // 50 requests that all saw 0 actions, released at the same moment: 50 tabs clicking at once.
        var results = await RaceAsync(50, () => _repository.TryAppendAsync(game.Id, 0, EndTurn, Later, ct), ct);

        Assert.Single(results, appended => appended);
        Assert.Equal(1, (await _repository.GetAsync(game.Id, ct))?.Sequence);
    }

    [Fact]
    public async Task Seat_two_can_be_taken_only_once_even_by_simultaneous_joins()
    {
        var ct = TestContext.Current.CancellationToken;
        var game = await AddGameAsync();

        var results = await RaceAsync(50, () => _repository.TryJoinAsync(game.Id, PlayerSecrets.Hash("two"), Later, ct), ct);

        Assert.Single(results, joined => joined);
        var stored = await _repository.GetAsync(game.Id, ct);
        Assert.NotNull(stored);
        Assert.True(stored.HasPlayerTwo);
        Assert.Equal(Later, stored.UpdatedAt);
    }

    [Fact]
    public async Task A_record_read_earlier_does_not_change_when_the_game_does()
    {
        var ct = TestContext.Current.CancellationToken;
        var game = await AddGameAsync();
        var before = await _repository.GetAsync(game.Id, ct);

        await _repository.TryJoinAsync(game.Id, PlayerSecrets.Hash("two"), Later, ct);
        await _repository.TryAppendAsync(game.Id, 0, EndTurn, Later, ct);

        // Immutable records: the reader keeps a consistent snapshot, with no lock and no copy.
        Assert.NotNull(before);
        Assert.Equal(0, before.Sequence);
        Assert.False(before.HasPlayerTwo);
    }

    [Fact]
    public async Task Writes_to_an_unknown_game_are_refused()
    {
        var ct = TestContext.Current.CancellationToken;
        var unknown = Guid.NewGuid();

        Assert.False(await _repository.TryJoinAsync(unknown, PlayerSecrets.Hash("two"), Later, ct));
        Assert.False(await _repository.TryAppendAsync(unknown, 0, EndTurn, Later, ct));
    }

    private async Task<GameRecord> AddGameAsync()
    {
        var game = new GameRecord(
            Guid.NewGuid(),
            TestScenario.Setup,
            PlayerSecrets.Hash("one"),
            PlayerTwoTokenHash: null,
            InviteCode: "K7QF3MZP",
            Actions: [],
            CreatedAt: Created,
            UpdatedAt: Created);
        await _repository.AddAsync(game, TestContext.Current.CancellationToken);
        return game;
    }

    // Starts `count` copies of the same call on thread-pool threads and releases them together,
    // so they really compete for the game's lock.
    private static async Task<bool[]> RaceAsync(int count, Func<Task<bool>> call, CancellationToken ct)
    {
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var racers = Enumerable.Range(0, count)
            .Select(_ => Task.Run(
                async () =>
                {
                    await start.Task;
                    return await call();
                },
                ct))
            .ToArray();

        start.SetResult();
        return await Task.WhenAll(racers);
    }
}
