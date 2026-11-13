using Tactics.Core.Model;

namespace Tactics.Api.Contracts;

/// <summary>The answer to <c>POST /api/games</c>: you created a game and hold seat one.</summary>
/// <param name="GameId">The new game's id.</param>
/// <param name="Side">Always "one": the creator plays first.</param>
/// <param name="PlayerToken">
/// Your secret for this game. Send it as <c>Authorization: Bearer &lt;token&gt;</c>. It's shown only once, and
/// whoever has it plays your seat.
/// </param>
/// <param name="InviteCode">Give it to your opponent: it lets them take seat two, once.</param>
/// <param name="View">The game as you see it now.</param>
public sealed record CreateGameResponse(Guid GameId, PlayerSide Side, string PlayerToken, string InviteCode, GameView View);

/// <summary>The body of <c>POST /api/games/{gameId}/join</c>.</summary>
/// <param name="InviteCode">The code Player One received when creating the game. Case doesn't matter.</param>
public sealed record JoinGameRequest(string InviteCode);

/// <summary>The answer to a successful join: you hold seat two.</summary>
/// <param name="GameId">The game's id.</param>
/// <param name="Side">Always "two".</param>
/// <param name="PlayerToken">Your secret for this game, as for Player One. It's shown only once.</param>
/// <param name="View">The game as you see it now.</param>
public sealed record JoinGameResponse(Guid GameId, PlayerSide Side, string PlayerToken, GameView View);
