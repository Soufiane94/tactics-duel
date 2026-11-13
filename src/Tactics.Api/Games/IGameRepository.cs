using Tactics.Core.Actions;

namespace Tactics.Api.Games;

/// <summary>
/// Where games live. Step 5 keeps them in memory; Step 12 adds a PostgreSQL version behind the same interface,
/// so it's async already. The two writes are compare-and-swap: each says what it expects to find, and does
/// nothing (returns false) when another request changed the game first.
/// </summary>
public interface IGameRepository
{
    /// <summary>Stores a new game. Its id must not be in use.</summary>
    Task AddAsync(GameRecord game, CancellationToken cancellationToken);

    /// <summary>The game as stored right now, or null if no game has this id.</summary>
    Task<GameRecord?> GetAsync(Guid gameId, CancellationToken cancellationToken);

    /// <summary>
    /// Takes seat two if it's still free. False if someone took it first, or if the game doesn't exist.
    /// </summary>
    Task<bool> TryJoinAsync(Guid gameId, string playerTwoTokenHash, DateTimeOffset at, CancellationToken cancellationToken);

    /// <summary>
    /// Appends an action if the game still has exactly <paramref name="expectedSequence"/> actions, so it becomes
    /// action number <c>expectedSequence + 1</c>. False if another action got in first, or if the game doesn't exist.
    /// </summary>
    Task<bool> TryAppendAsync(
        Guid gameId, int expectedSequence, GameAction action, DateTimeOffset at, CancellationToken cancellationToken);
}
