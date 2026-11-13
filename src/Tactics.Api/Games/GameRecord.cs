using System.Collections.Immutable;
using Tactics.Core.Actions;
using Tactics.Core.Model;

namespace Tactics.Api.Games;

/// <summary>
/// One stored game: who may play it and what was played, never the state itself. The state is rebuilt on
/// demand with <c>GameEngine.Replay(Setup, Actions)</c>. Immutable: the repository swaps whole records.
/// </summary>
/// <param name="Id">The game's id, used in URLs and invite links.</param>
/// <param name="Setup">The setup snapshot taken at creation: board, unit stats, placements.</param>
/// <param name="PlayerOneTokenHash">SHA-256 of Player One's token, as hex. The token itself is never stored.</param>
/// <param name="PlayerTwoTokenHash">The same for Player Two, or null until someone joins with the invite code.</param>
/// <param name="InviteCode">The code Player Two needs to join.</param>
/// <param name="Actions">Every accepted action, in order. An action's sequence number is its position, from 1.</param>
/// <param name="CreatedAt">When the game was created.</param>
/// <param name="UpdatedAt">When it last changed: a join or an action.</param>
public sealed record GameRecord(
    Guid Id,
    GameSetup Setup,
    string PlayerOneTokenHash,
    string? PlayerTwoTokenHash,
    string InviteCode,
    ImmutableList<GameAction> Actions,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    /// <summary>The number of actions played so far. Clients send it back as <c>expectedSequence</c>.</summary>
    public int Sequence => Actions.Count;

    /// <summary>True once seat two is taken.</summary>
    public bool HasPlayerTwo => PlayerTwoTokenHash is not null;
}
