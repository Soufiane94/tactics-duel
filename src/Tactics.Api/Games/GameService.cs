using System.Diagnostics;
using Tactics.Api.Contracts;
using Tactics.Core;
using Tactics.Core.Actions;
using Tactics.Core.Model;

namespace Tactics.Api.Games;

/// <summary>
/// The referee's desk: every game request goes through here, and every rule decision goes to <c>Tactics.Core</c>.
/// It stores only setups and actions, and rebuilds the state for each request with <see cref="GameEngine.Replay"/>.
/// It knows nothing about HTTP: it returns values or <see cref="ServiceError"/>s, and the endpoints turn them into
/// responses. It's the only class here that reads the clock, through <see cref="TimeProvider"/>.
/// </summary>
public sealed partial class GameService(
    IGameRepository games,
    SetupProvider setups,
    TimeProvider time,
    ILogger<GameService> logger)
{
    /// <summary>Creates a game from the default setup. The caller holds seat one and gets the invite code for seat two.</summary>
    public async Task<CreateGameResponse> CreateGameAsync(CancellationToken cancellationToken)
    {
        var now = time.GetUtcNow();
        var token = PlayerSecrets.NewToken();
        var game = new GameRecord(
            // A version 7 id starts with its creation time, so new ids sort last: kind to Step 12's database index.
            Guid.CreateVersion7(now),
            setups.Default,
            PlayerSecrets.Hash(token),
            PlayerTwoTokenHash: null,
            PlayerSecrets.NewInviteCode(),
            Actions: [],
            CreatedAt: now,
            UpdatedAt: now);

        await games.AddAsync(game, cancellationToken);
        LogGameCreated(logger, game.Id, game.Setup.ScenarioId);

        return new CreateGameResponse(game.Id, PlayerSide.One, token, game.InviteCode, ViewFor(game, PlayerSide.One));
    }

    /// <summary>Gives seat two to whoever has the invite code, once. Checks: the game exists, the code, the free seat.</summary>
    public async Task<ServiceResult<JoinGameResponse>> JoinGameAsync(
        Guid gameId, string inviteCode, CancellationToken cancellationToken)
    {
        if (await games.GetAsync(gameId, cancellationToken) is not { } game)
        {
            return GameNotFound(gameId);
        }

        // The code comes before the seat, so a stranger with a wrong code learns nothing about the game.
        if (!PlayerSecrets.InviteCodeMatches(inviteCode, game.InviteCode))
        {
            return new ServiceError(
                ServiceErrorKind.InvalidInviteCode, "This invite code doesn't match the game. Check the invite link.");
        }

        if (game.HasPlayerTwo)
        {
            return AlreadyJoined();
        }

        var token = PlayerSecrets.NewToken();
        var tokenHash = PlayerSecrets.Hash(token);
        var now = time.GetUtcNow();

        // The repository checks the seat again, under the game's lock: of two joins at the same moment, one wins.
        if (!await games.TryJoinAsync(gameId, tokenHash, now, cancellationToken))
        {
            return AlreadyJoined();
        }

        LogPlayerJoined(logger, gameId);
        var joined = game with { PlayerTwoTokenHash = tokenHash, UpdatedAt = now };
        return new JoinGameResponse(gameId, PlayerSide.Two, token, ViewFor(joined, PlayerSide.Two));
    }

    /// <summary>The game as the token's player sees it.</summary>
    public async Task<ServiceResult<GameView>> GetViewAsync(Guid gameId, string? token, CancellationToken cancellationToken)
    {
        if (await games.GetAsync(gameId, cancellationToken) is not { } game)
        {
            return GameNotFound(gameId);
        }

        if (SideOf(game, token) is not { } side)
        {
            return NotAPlayer(token);
        }

        return ViewFor(game, side);
    }

    /// <summary>
    /// Plays one action for the token's player. The checks run in a fixed order: the game (404), the player (401),
    /// the sequence (409), then the rules (422). Only then is the action stored, and only if no other action got in.
    /// </summary>
    public async Task<ServiceResult<SubmitActionResponse>> SubmitActionAsync(
        Guid gameId, string? token, int expectedSequence, ActionDto action, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(action);

        // 1. Which game, and who's asking? Nothing below runs for a stranger.
        if (await games.GetAsync(gameId, cancellationToken) is not { } game)
        {
            return GameNotFound(gameId);
        }

        if (SideOf(game, token) is not { } side)
        {
            return NotAPlayer(token);
        }

        // 2. Was the request made from the game as it is now? Asked before the rules: a verdict on a game the player
        //    hasn't seen means nothing. The client fetches the view again and decides with fresh information.
        if (expectedSequence != game.Sequence)
        {
            return SequenceConflict(
                $"Your view is out of date: you sent expectedSequence {expectedSequence}, but {game.Sequence} actions " +
                "have been played. Fetch the game again.");
        }

        // 3. The referee: rebuild the state, then let Core judge the action. The side comes from the token, never
        //    from the request, so a client can't play the other side's turn.
        var state = GameEngine.Replay(game.Setup, game.Actions);
        var coreAction = action.ToCore(side);

        switch (GameEngine.Apply(state, coreAction))
        {
            case ActionOutcome.Rejected rejected:
                LogActionRejected(logger, gameId, rejected.Code, coreAction);
                return new ServiceError(ServiceErrorKind.RuleViolation, rejected.Message, rejected.Code);

            case ActionOutcome.Accepted accepted:
                // 4. Store it as action expectedSequence + 1, unless another action got in since step 2.
                var now = time.GetUtcNow();
                if (!await games.TryAppendAsync(gameId, expectedSequence, coreAction, now, cancellationToken))
                {
                    return SequenceConflict("Another action was played just before yours. Fetch the game again.");
                }

                var played = game with { Actions = game.Actions.Add(coreAction), UpdatedAt = now };
                LogActionAccepted(logger, gameId, played.Sequence, coreAction);
                return new SubmitActionResponse(
                    played.Sequence,
                    [.. accepted.Events.Select(e => e.ToDto())],
                    played.ToView(accepted.State, side));

            default:
                throw new UnreachableException("An outcome is either accepted or rejected.");
        }
    }

    private static GameView ViewFor(GameRecord game, PlayerSide side) =>
        game.ToView(GameEngine.Replay(game.Setup, game.Actions), side);

    // The side this token plays, or null. Seat two may still be free.
    private static PlayerSide? SideOf(GameRecord game, string? token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return null;
        }

        if (PlayerSecrets.Matches(token, game.PlayerOneTokenHash))
        {
            return PlayerSide.One;
        }

        return game.PlayerTwoTokenHash is { } twoHash && PlayerSecrets.Matches(token, twoHash) ? PlayerSide.Two : null;
    }

    private static ServiceError GameNotFound(Guid gameId) =>
        new(ServiceErrorKind.GameNotFound, $"There's no game with the id {gameId}.");

    private static ServiceError NotAPlayer(string? token) => new(
        ServiceErrorKind.NotAPlayer,
        string.IsNullOrEmpty(token)
            ? "This needs your player token, sent as \"Authorization: Bearer <token>\". You got it when you created or joined the game."
            : "This token isn't a player token of this game.");

    private static ServiceError AlreadyJoined() =>
        new(ServiceErrorKind.AlreadyJoined, "This game already has two players.");

    private static ServiceError SequenceConflict(string message) => new(ServiceErrorKind.SequenceConflict, message);

    // Structured logs, shown in the Aspire dashboard. Never log a token or an invite code: whoever reads them could play.
    [LoggerMessage(Level = LogLevel.Information, Message = "Game {GameId} created from scenario {ScenarioId}")]
    private static partial void LogGameCreated(ILogger logger, Guid gameId, string scenarioId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Game {GameId}: Player Two joined")]
    private static partial void LogPlayerJoined(ILogger logger, Guid gameId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Game {GameId}: action {Sequence} accepted: {Action}")]
    private static partial void LogActionAccepted(ILogger logger, Guid gameId, int sequence, GameAction action);

    [LoggerMessage(Level = LogLevel.Information, Message = "Game {GameId}: action rejected ({Rule}): {Action}")]
    private static partial void LogActionRejected(ILogger logger, Guid gameId, RuleViolation rule, GameAction action);
}
