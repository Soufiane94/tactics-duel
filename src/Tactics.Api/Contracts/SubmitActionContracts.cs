namespace Tactics.Api.Contracts;

/// <summary>The body of <c>POST /api/games/{gameId}/actions</c>.</summary>
/// <param name="ExpectedSequence">
/// The <c>sequence</c> of the view you're acting on. If another action came first (a double click, a second tab),
/// the server answers 409 instead of applying yours to a game you haven't seen.
/// </param>
/// <param name="Action">What you want to do.</param>
public sealed record SubmitActionRequest(int ExpectedSequence, ActionDto Action);

/// <summary>The answer to an accepted action: what happened, then where the game is now.</summary>
/// <param name="Sequence">The action's number, which is also the new view's sequence.</param>
/// <param name="Events">What happened, in order. Animate these, then show the view.</param>
/// <param name="View">The game after the action. It's the truth: snap to it after the animations.</param>
public sealed record SubmitActionResponse(int Sequence, IReadOnlyList<EventDto> Events, GameView View);
