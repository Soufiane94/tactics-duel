using System.Diagnostics;
using Microsoft.AspNetCore.Http.HttpResults;
using Tactics.Api.Games;

namespace Tactics.Api.Http;

/// <summary>
/// Turns a <see cref="ServiceError"/> into a ProblemDetails response (RFC 9457): PLAN.md's HTTP error table, in one
/// switch. Clients branch on the status and on the <c>code</c> extension, never on the wording of <c>detail</c>.
/// </summary>
public static class ApiProblems
{
    /// <summary>The ProblemDetails extension that carries a stable code, e.g. <c>"code": "NotYourTurn"</c>.</summary>
    public const string CodeExtension = "code";

    /// <summary>The response for a refused request.</summary>
    public static ProblemHttpResult From(ServiceError error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return error.Kind switch
        {
            ServiceErrorKind.GameNotFound =>
                Problem(StatusCodes.Status404NotFound, "Game not found", error.Message),
            ServiceErrorKind.NotAPlayer =>
                Problem(StatusCodes.Status401Unauthorized, "Not a player of this game", error.Message),
            ServiceErrorKind.InvalidInviteCode =>
                Problem(StatusCodes.Status403Forbidden, "Invalid invite code", error.Message, "InvalidInviteCode"),
            ServiceErrorKind.AlreadyJoined =>
                Problem(StatusCodes.Status409Conflict, "Already joined", error.Message, "AlreadyJoined"),
            ServiceErrorKind.SequenceConflict =>
                Problem(StatusCodes.Status409Conflict, "Sequence conflict", error.Message, "SequenceConflict"),
            // The code is the rule's name exactly as Core spells it: "TargetOutOfRange".
            ServiceErrorKind.RuleViolation =>
                Problem(StatusCodes.Status422UnprocessableEntity, "Rule violation", error.Message, error.Rule?.ToString()),
            _ => throw new UnreachableException($"There's no HTTP status for {error.Kind}."),
        };
    }

    /// <summary>
    /// Adds the header every 401 must carry (RFC 9110): <c>WWW-Authenticate: Bearer</c> names the credential to
    /// send. Plugged into <c>AddProblemDetails</c>, so it runs for every ProblemDetails response.
    /// </summary>
    public static void AddChallenge(ProblemDetailsContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.ProblemDetails.Status == StatusCodes.Status401Unauthorized)
        {
            context.HttpContext.Response.Headers.WWWAuthenticate = "Bearer";
        }
    }

    private static ProblemHttpResult Problem(int status, string title, string detail, string? code = null) =>
        TypedResults.Problem(
            detail: detail,
            statusCode: status,
            title: title,
            extensions: code is null ? null : new Dictionary<string, object?> { [CodeExtension] = code });
}
