namespace Tactics.Core.Model;

/// <summary>Whether the game still accepts actions. A finished game rejects every action with GameOver.</summary>
public enum GameStatus
{
    /// <summary>Players are taking turns.</summary>
    InProgress,

    /// <summary>One side has no units left; <see cref="GameState.Winner"/> says who won.</summary>
    Finished,
}
