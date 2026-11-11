namespace Tactics.Core.Content;

/// <summary>
/// Thrown when the content files are invalid. It lists every problem at once, so you can fix them all in one go.
/// Throwing is right here: bad content is a setup bug, and the server must not start a game with it.
/// </summary>
public sealed class ContentException : Exception
{
    /// <summary>Creates the exception from the problems found.</summary>
    public ContentException(IReadOnlyList<ContentError> errors)
        : base(BuildMessage(errors))
    {
        Errors = [.. errors];
    }

    /// <summary>Every problem found, in file order: the units file first, then the scenario.</summary>
    public IReadOnlyList<ContentError> Errors { get; }

    // "\n" rather than Environment.NewLine: the same input gives the same text on every OS.
    private static string BuildMessage(IReadOnlyList<ContentError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        var plural = errors.Count == 1 ? "" : "s";
        return $"The game content has {errors.Count} problem{plural}:"
            + string.Concat(errors.Select(e => $"\n- {e.Message}"));
    }
}
