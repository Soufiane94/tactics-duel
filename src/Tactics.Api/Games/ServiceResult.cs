using Tactics.Core;

namespace Tactics.Api.Games;

/// <summary>
/// Why <see cref="GameService"/> refused a request. Each kind maps to exactly one HTTP status
/// (<c>ApiProblems.From</c>), so the service itself knows nothing about HTTP and Step 11's hub can reuse it.
/// </summary>
public enum ServiceErrorKind
{
    /// <summary>No game has this id. HTTP 404.</summary>
    GameNotFound,

    /// <summary>The player token is missing, or isn't one of this game's two tokens. HTTP 401.</summary>
    NotAPlayer,

    /// <summary>The invite code doesn't match the game's. HTTP 403.</summary>
    InvalidInviteCode,

    /// <summary>Seat two is already taken. HTTP 409.</summary>
    AlreadyJoined,

    /// <summary>The request was made from an out-of-date view: other actions were played since. HTTP 409.</summary>
    SequenceConflict,

    /// <summary>The rules engine refused the action. <see cref="ServiceError.Rule"/> says which rule. HTTP 422.</summary>
    RuleViolation,
}

/// <summary>A refused request: its kind, a message a player can read, and the broken rule for a rule violation.</summary>
/// <param name="Kind">What went wrong.</param>
/// <param name="Message">The explanation, in plain words. It ends up in ProblemDetails' <c>detail</c>.</param>
/// <param name="Rule">The rule's stable code, for <see cref="ServiceErrorKind.RuleViolation"/> only.</param>
public sealed record ServiceError(ServiceErrorKind Kind, string Message, RuleViolation? Rule = null);

/// <summary>
/// What <see cref="GameService"/> returns: a value or an error, nothing else. A refused request is a normal answer,
/// not an exception, like Core's <c>ActionOutcome</c>. The two implicit conversions let the service write
/// <c>return view;</c> or <c>return error;</c>.
/// </summary>
/// <typeparam name="T">The value of a successful request.</typeparam>
public abstract record ServiceResult<T>
{
    private ServiceResult()
    {
    }

    /// <summary>The request was done; here's its result.</summary>
    /// <param name="Value">The result.</param>
    public sealed record Succeeded(T Value) : ServiceResult<T>;

    /// <summary>The request was refused; nothing changed.</summary>
    /// <param name="Error">Why.</param>
    public sealed record Failed(ServiceError Error) : ServiceResult<T>;

    /// <summary>Wraps a value as a success.</summary>
    public static implicit operator ServiceResult<T>(T value) => new Succeeded(value);

    /// <summary>Wraps an error as a failure.</summary>
    public static implicit operator ServiceResult<T>(ServiceError error) => new Failed(error);
}
