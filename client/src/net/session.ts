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
  /** The token to send while `side` is the player to move. */
  tokenFor(side: PlayerSide): string;
}

/**
 * A session that holds both seats of one game. The tokens live in memory only, so reloading the page starts a new
 * game (Step 10 keeps sessions in `localStorage`).
 */
export function localSession(gameId: string, tokens: Readonly<Record<PlayerSide, string>>): GameSession {
  return { gameId, tokenFor: (side) => tokens[side] };
}

/** The part of `ApiClient` that starting a game needs. Tests pass a small fake instead. */
export type LobbyApi = Pick<ApiClient, "createGame" | "joinGame" | "getView">;

/**
 * Starts a local game: two players, one screen. It's the real lobby, played by one browser:
 * 1. create the game: we hold seat one and get the invite code;
 * 2. join it with that code: we hold seat two as well;
 * 3. fetch the view with the current player's token. A view belongs to one player (its `yourSide` and
 *    `availableActions` depend on the token), and the first one on screen should be the player to move.
 */
export async function startLocalGame(api: LobbyApi): Promise<{ session: GameSession; view: GameView }> {
  const created = await api.createGame();
  const joined = await api.joinGame(created.gameId, created.inviteCode);
  const session = localSession(created.gameId, { one: created.playerToken, two: joined.playerToken });
  const view = await api.getView(session.gameId, session.tokenFor(joined.view.currentPlayer));
  return { session, view };
}
