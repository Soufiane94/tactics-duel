import type { Viewer } from "../game/turnText";
import type { GameView, PlayerSide } from "../game/types";
import type { ApiClient } from "./ApiClient";

/** How the page was opened, read from its URL. Step 10 adds online games (`?game=…&invite=…`). */
export type LaunchMode = "local" | "none";

/** `?mode=local` starts a local game (pass & play). Anything else shows the boot screen. */
export function readLaunchMode(search: string): LaunchMode {
  return new URLSearchParams(search).get("mode") === "local" ? "local" : "none";
}

/**
 * The seat or seats this browser holds in one game, so which token goes with each request. Code that plays only asks
 * `tokenFor(view.currentPlayer)`, so it works the same way with both kinds of session:
 * - local (pass & play): both seats on one screen, so the token follows the turn;
 * - online (Step 10): one seat, so it's always that seat's token.
 */
export interface GameSession {
  readonly gameId: string;
  /** True if this browser holds `side`'s seat: both seats in pass & play, one online. */
  holds(side: PlayerSide): boolean;
  /** The token to send while `side` is the player to move. */
  tokenFor(side: PlayerSide): string;
}

/**
 * A session that holds both seats of one game. The tokens live in memory only, so reloading the page starts a new
 * game (Step 10 keeps sessions in `localStorage`).
 */
export function localSession(gameId: string, tokens: Readonly<Record<PlayerSide, string>>): GameSession {
  return { gameId, holds: () => true, tokenFor: (side) => tokens[side] };
}

/**
 * Whom the texts on screen speak to (`Viewer`, in `game/turnText.ts`). A browser that holds one seat plays for it:
 * "Your turn". One that holds both (pass & play), or none (a spectator, someday), plays for nobody in particular, so
 * the texts name the sides: "Player Two to play".
 */
export function viewerOf(session: GameSession): Viewer {
  const holdsOne = session.holds("one");
  if (holdsOne === session.holds("two")) {
    return null;
  }
  return holdsOne ? "one" : "two";
}

/**
 * The view to put on screen. A view belongs to one player: its `yourSide` and `availableActions` depend on the token
 * it was fetched with, and the server answers an action with the sender's view. So after End Turn, the answer is the
 * view of the player who now waits, with nothing to click. In pass & play, this browser also holds the seat to move,
 * so it fetches that seat's view: one more request per turn. Online, it holds one seat, so the view stays as it is.
 */
export async function viewToShow(
  api: Pick<ApiClient, "getView">,
  session: GameSession,
  view: GameView,
): Promise<GameView> {
  const toMove = view.currentPlayer;
  if (view.status === "finished" || view.yourSide === toMove || !session.holds(toMove)) {
    return view;
  }
  return api.getView(session.gameId, session.tokenFor(toMove));
}

/** The part of `ApiClient` that starting a game needs. Tests pass a small fake instead. */
export type LobbyApi = Pick<ApiClient, "createGame" | "joinGame" | "getView">;

/**
 * Starts a local game: two players, one screen. It's the real lobby, played by one browser:
 * 1. create the game: we hold seat one and get the invite code;
 * 2. join it with that code: we hold seat two as well;
 * 3. show the view of the player to move. Joining answers with seat two's view, but Player One moves first, so
 *    `viewToShow` fetches seat one's.
 */
export async function startLocalGame(api: LobbyApi): Promise<{ session: GameSession; view: GameView }> {
  const created = await api.createGame();
  const joined = await api.joinGame(created.gameId, created.inviteCode);
  const session = localSession(created.gameId, { one: created.playerToken, two: joined.playerToken });
  const view = await viewToShow(api, session, joined.view);
  return { session, view };
}
