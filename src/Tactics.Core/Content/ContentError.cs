namespace Tactics.Core.Content;

/// <summary>Stable codes for content problems, so tests and tools never depend on how a message is worded.</summary>
public enum ContentErrorCode
{
    /// <summary>The file isn't valid JSON, or a value has the wrong JSON type.</summary>
    InvalidJson,

    /// <summary>A required field is missing or empty.</summary>
    MissingField,

    /// <summary>A value isn't one of the allowed ones: an unknown owner or ability, a board size below 1.</summary>
    InvalidValue,

    /// <summary>A unit stat is too low: every stat must be at least 1, except defense, which can be 0.</summary>
    InvalidStat,

    /// <summary>A unit type's minRange is greater than its maxRange.</summary>
    RangeOrder,

    /// <summary>Two unit types, or two placements, share an id.</summary>
    DuplicateId,

    /// <summary>A placement uses a unit type id that the units file doesn't declare.</summary>
    UnknownUnitType,

    /// <summary>A placement or an obstacle is outside the board.</summary>
    OffBoard,

    /// <summary>A placement stands on an obstacle.</summary>
    OnObstacle,

    /// <summary>Two placements share a tile.</summary>
    Overlap,

    /// <summary>A side has no units, so the game would start already won.</summary>
    EmptySide,
}

/// <summary>One problem in the content files: a stable code for tests and tools, and a message for you.</summary>
/// <param name="Code">The kind of problem.</param>
/// <param name="Message">What is wrong and where, in plain words.</param>
public sealed record ContentError(ContentErrorCode Code, string Message);
