namespace Tactics.Core;

/// <summary>
/// Why the engine refused an action, as a stable code. Tests check it and the API turns it into an HTTP 422
/// (Step 5), so the message next to it can be reworded without breaking either. Step 3 adds the target codes.
/// </summary>
public enum RuleViolation
{
    /// <summary>The game is finished, so it accepts no more actions.</summary>
    GameOver,

    /// <summary>The action comes from the side that isn't playing.</summary>
    NotYourTurn,

    /// <summary>No unit has this id, for example because it was defeated.</summary>
    UnitNotFound,

    /// <summary>The unit belongs to the other side.</summary>
    NotYourUnit,

    /// <summary>The unit already moved this turn. A unit moves at most once per turn.</summary>
    AlreadyMoved,

    /// <summary>The unit already acted this turn. Acting ends its activity, so it can't move or act again.</summary>
    AlreadyActed,

    /// <summary>The unit can't end a move on that tile: too far, blocked, taken, off the board, or its own tile.</summary>
    DestinationNotReachable,
}
