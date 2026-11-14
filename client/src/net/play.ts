import type { ApiErrorCode, GameAction, GameEvent, GameView, SubmitActionResponse } from "../game/types";
import { ApiError, errorMessage, type ApiClient } from "./ApiClient";
import { viewToShow, type GameSession } from "./session";

/** The part of `ApiClient` that playing needs. Tests pass a small fake instead. */
export type PlayApi = Pick<ApiClient, "submitAction" | "getView">;

/**
 * How one action went. It's Core's `ActionOutcome` (accepted or rejected) seen from the client, plus a third case
 * that only a client has: the view it decided on was out of date.
 */
export type PlayOutcome =
  /**
   * The server played it. `view` is the one to show now (in pass & play, the next player's after End Turn), and
   * `events` say what happened, for Step 8's animations. `message` stays null unless that view failed to load.
   */
  | {
      readonly kind: "accepted";
      readonly view: GameView;
      readonly events: readonly GameEvent[];
      readonly message: string | null;
    }
  /** Nothing was played: the rules refused it (422), or the server couldn't be reached. The view on screen holds. */
  | { readonly kind: "rejected"; readonly message: string; readonly code: ApiErrorCode | null }
  /** Nothing was played, because the view on screen was out of date (409). `view` is the game as it is now. */
  | { readonly kind: "resynced"; readonly view: GameView; readonly message: string };

/** What the player reads after a 409. The server's own sentence is for developers: "Fetch the game again." */
const OUT_OF_DATE_MESSAGE = "The game had changed, so your action wasn't played. The board is up to date now.";

/**
 * Plays one action. It never throws: every way it can end is a `PlayOutcome`.
 * 1. POST the action with the token of the player to move and the `sequence` of the view on screen. That says "I
 *    decided this on view N" (optimistic concurrency, Step 5).
 * 2. Accepted: show the view of the player to move (`viewToShow`).
 * 3. 409 `SequenceConflict`: the action was decided on old information, so it isn't sent again. The player gets the
 *    fresh view and decides again.
 * 4. Anything else (a 422 rule refusal, no network): a message to show.
 */
export async function playAction(
  api: PlayApi,
  session: GameSession,
  view: GameView,
  action: GameAction,
): Promise<PlayOutcome> {
  const token = session.tokenFor(view.currentPlayer);

  let response: SubmitActionResponse;
  try {
    response = await api.submitAction(session.gameId, token, { expectedSequence: view.sequence, action });
  } catch (error) {
    return error instanceof ApiError && error.code === "SequenceConflict"
      ? resync(api, session, token)
      : rejected(error);
  }

  try {
    const next = await viewToShow(api, session, response.view);
    return { kind: "accepted", view: next, events: response.events, message: null };
  } catch (error) {
    // The action counts: the server stored it. Only the next player's view is missing, so show the sender's: the
    // same game, seen from the other seat.
    const message = `Played, but the next player's view didn't load. ${errorMessage(error)}`;
    return { kind: "accepted", view: response.view, events: response.events, message };
  }
}

/** After a 409: fetch the game as it is now, from the seat to move if this browser holds it. */
async function resync(api: PlayApi, session: GameSession, token: string): Promise<PlayOutcome> {
  try {
    const fresh = await viewToShow(api, session, await api.getView(session.gameId, token));
    return { kind: "resynced", view: fresh, message: OUT_OF_DATE_MESSAGE };
  } catch (error) {
    return rejected(error);
  }
}

function rejected(error: unknown): PlayOutcome {
  const code = error instanceof ApiError ? (error.code ?? null) : null;
  return { kind: "rejected", message: errorMessage(error), code };
}
