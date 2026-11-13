namespace Tactics.Api.Tests.Helpers;

/// <summary>
/// A clock that only moves when a test moves it. <see cref="TimeProvider"/> is an abstract class, so overriding one
/// method is all it takes: no mocking library, no waiting.
/// </summary>
/// <param name="now">The time it shows until a test changes <see cref="Now"/>.</param>
public sealed class FakeClock(DateTimeOffset now) : TimeProvider
{
    /// <summary>The time the clock shows.</summary>
    public DateTimeOffset Now { get; set; } = now;

    /// <inheritdoc />
    public override DateTimeOffset GetUtcNow() => Now;
}
