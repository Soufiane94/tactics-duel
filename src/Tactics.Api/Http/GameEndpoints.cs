using System.Diagnostics;
using Microsoft.AspNetCore.Http.HttpResults;
using Tactics.Api.Contracts;
using Tactics.Api.Games;

namespace Tactics.Api.Http;

/// <summary>
/// The four game endpoints from PLAN.md's Contracts section. Each one reads the request, calls
/// <see cref="GameService"/> and turns its result into HTTP. No rule lives here, and no request flow either.
/// </summary>
public static class GameEndpoints
{
    private const string GetGameRoute = "GetGame";

    /// <summary>Maps <c>/games</c> and its routes under <paramref name="api"/> (the <c>/api</c> group).</summary>
    public static RouteGroupBuilder MapGames(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        var games = api.MapGroup("/games").WithTags("Games");

        games.MapPost("", CreateGame)
            .WithSummary("Create a game")
            .WithDescription("You take seat one (Player One plays first). The answer holds your player token "
                + "and the invite code for seat two.");

        games.MapPost("/{gameId:guid}/join", JoinGame)
            .WithSummary("Join a game")
            .WithDescription("Take seat two with the invite code. The answer holds your own player token.")
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        games.MapGet("/{gameId:guid}", GetGame)
            .WithName(GetGameRoute)
            .WithSummary("Get the game as you see it")
            .WithPlayerToken()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        games.MapPost("/{gameId:guid}/actions", SubmitAction)
            .WithSummary("Play an action")
            .WithDescription("Move, act or end your turn. The server checks the action against the rules, then "
                + "answers with what happened (events) and the new view.")
            .WithPlayerToken()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        return api;
    }

    // 201 Created, with a Location header that points to the new game (GET /api/games/{gameId}).
    private static async Task<CreatedAtRoute<CreateGameResponse>> CreateGame(
        GameService service, CancellationToken cancellationToken)
    {
        var created = await service.CreateGameAsync(cancellationToken);
        return TypedResults.CreatedAtRoute(created, GetGameRoute, new RouteValueDictionary { ["gameId"] = created.GameId });
    }

    private static async Task<Results<Ok<JoinGameResponse>, ProblemHttpResult>> JoinGame(
        Guid gameId, JoinGameRequest body, GameService service, CancellationToken cancellationToken) =>
        ToHttp(await service.JoinGameAsync(gameId, body.InviteCode, cancellationToken));

    private static async Task<Results<Ok<GameView>, ProblemHttpResult>> GetGame(
        Guid gameId, HttpRequest request, GameService service, CancellationToken cancellationToken) =>
        ToHttp(await service.GetViewAsync(gameId, BearerToken.Read(request), cancellationToken));

    private static async Task<Results<Ok<SubmitActionResponse>, ProblemHttpResult>> SubmitAction(
        Guid gameId, SubmitActionRequest body, HttpRequest request, GameService service, CancellationToken cancellationToken) =>
        ToHttp(await service.SubmitActionAsync(
            gameId, BearerToken.Read(request), body.ExpectedSequence, body.Action, cancellationToken));

    // 200 with the value, or the error's ProblemDetails. Typed results also describe both in the OpenAPI document.
    private static Results<Ok<T>, ProblemHttpResult> ToHttp<T>(ServiceResult<T> result) => result switch
    {
        ServiceResult<T>.Succeeded success => TypedResults.Ok(success.Value),
        ServiceResult<T>.Failed failure => ApiProblems.From(failure.Error),
        _ => throw new UnreachableException("A service result either succeeded or failed."),
    };
}
