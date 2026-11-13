namespace Tactics.Core;

/// <summary>
/// Why the engine refused an action, as a stable code. Tests check it and the API turns it into an HTTP 422
/// (Step 5), so the message next to it can be reworded without breaking either.
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

    /// <summary>No unit has the target's id, for example because it was already defeated.</summary>
    TargetNotFound,

    /// <summary>
    /// The target is the right kind but at the wrong distance: closer than the actor's MinRange or farther
    /// than its MaxRange (Manhattan, from where the actor stands now).
    /// </summary>
    TargetOutOfRange,

    /// <summary>
    /// The wrong kind of target for the actor's ability: an attacker targets an ally or itself, or a healer
    /// targets an enemy or a unit at full HP. Checked before the range, because no move can fix it.
    /// </summary>
    InvalidTarget,
}
