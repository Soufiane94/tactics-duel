using System.Collections.Concurrent;
using Tactics.Core.Actions;

namespace Tactics.Api.Games;

/// <summary>
/// Keeps games in memory for as long as the API runs (Step 12 adds the database). Records are immutable, so a
/// reader gets a snapshot that later writes can't change. Each game has its own lock, so games never wait for
/// each other, and the lock is only held to check and write: replays and rules run outside it.
/// </summary>
public sealed class InMemoryGameRepository : IGameRepository
{
    private readonly ConcurrentDictionary<Guid, Slot> _games = new();

    /// <inheritdoc />
    public Task AddAsync(GameRecord game, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(game);
        if (!_games.TryAdd(game.Id, new Slot(game)))
        {
            throw new InvalidOperationException($"A game with the id {game.Id} already exists.");
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<GameRecord?> GetAsync(Guid gameId, CancellationToken cancellationToken) =>
        Task.FromResult(_games.TryGetValue(gameId, out var slot) ? slot.Read() : null);

    /// <inheritdoc />
    public Task<bool> TryJoinAsync(
        Guid gameId, string playerTwoTokenHash, DateTimeOffset at, CancellationToken cancellationToken) =>
        Task.FromResult(_games.TryGetValue(gameId, out var slot) && slot.TryChange(game =>
            game.HasPlayerTwo ? null : game with { PlayerTwoTokenHash = playerTwoTokenHash, UpdatedAt = at }));

    /// <inheritdoc />
    public Task<bool> TryAppendAsync(
        Guid gameId, int expectedSequence, GameAction action, DateTimeOffset at, CancellationToken cancellationToken) =>
        Task.FromResult(_games.TryGetValue(gameId, out var slot) && slot.TryChange(game =>
            game.Sequence != expectedSequence ? null : game with { Actions = game.Actions.Add(action), UpdatedAt = at }));

    // One game's place in memory. Without the lock, two requests could both see 12 actions and both append
    // action 13. With it, "check, then write" is one step: the second request sees 13 actions and is refused.
    private sealed class Slot(GameRecord initial)
    {
        private readonly Lock _gate = new();
        private GameRecord _current = initial;

        public GameRecord Read()
        {
            lock (_gate)
            {
                return _current;
            }
        }

        // The change gets the current record and returns the next one, or null to refuse.
        public bool TryChange(Func<GameRecord, GameRecord?> change)
        {
            lock (_gate)
            {
                if (change(_current) is not { } next)
                {
                    return false;
                }

                _current = next;
                return true;
            }
        }
    }
}
