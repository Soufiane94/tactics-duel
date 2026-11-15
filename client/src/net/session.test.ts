import { describe, expect, it } from "vitest";
import type { GameView, PlayerSide } from "../game/types";
import { GAME_ID as gameId, view } from "../test/fixtures";
import {
  localSession,
  readLaunchMode,
  startLocalGame,
  viewerOf,
  viewToShow,
  type GameSession,
  type LobbyApi,
} from "./session";

/** A view of a fresh game (Player One to move), as `side` sees it. */
function freshView(side: PlayerSide): GameView {
  return view({ yourSide: side, opponentJoined: side === "two" });
}

describe("readLaunchMode", () => {
  it("starts a local game for ?mode=local", () => {
    expect(readLaunchMode("?mode=local")).toBe("local");
    expect(readLaunchMode("?game=abc&mode=local")).toBe("local");
  });

  it("shows the boot screen otherwise", () => {
    expect(readLaunchMode("")).toBe("none");
    expect(readLaunchMode("?mode=online")).toBe("none");
  });
});

describe("startLocalGame", () => {
  it("creates the game, joins it with the invite code, then fetches the view as the player to move", async () => {
    const calls: string[] = [];
    const playerOnesView = freshView("one");
    const api: LobbyApi = {
      createGame: () => {
        calls.push("create");
        return Promise.resolve({ gameId, side: "one", playerToken: "token-one", inviteCode: "K7QF3MZP", view: freshView("one") });
      },
      joinGame: (id, inviteCode) => {
        calls.push(`join ${id} with ${inviteCode}`);
        return Promise.resolve({ gameId, side: "two", playerToken: "token-two", view: freshView("two") });
      },
      getView: (id, token) => {
        calls.push(`get ${id} with ${token}`);
        return Promise.resolve(playerOnesView);
      },
    };

    const { session, view } = await startLocalGame(api);

    // Player One moves first, so the first view on screen is fetched with Player One's token.
    expect(calls).toEqual(["create", `join ${gameId} with K7QF3MZP`, `get ${gameId} with token-one`]);
    expect(view).toBe(playerOnesView);
    expect(session.gameId).toBe(gameId);
    expect(session.tokenFor("one")).toBe("token-one");
    expect(session.tokenFor("two")).toBe("token-two");
    expect(session.holds("one") && session.holds("two")).toBe(true);
  });
});

describe("viewerOf", () => {
  /** A session that holds exactly the seats listed. */
  const holding = (...seats: PlayerSide[]): GameSession => ({
    gameId,
    holds: (side) => seats.includes(side),
    tokenFor: () => "token",
  });

  it("plays for nobody in particular in pass & play, so the texts name the sides", () => {
    expect(viewerOf(localSession(gameId, { one: "token-one", two: "token-two" }))).toBeNull();
  });

  it("plays for the one seat it holds online", () => {
    expect(viewerOf(holding("one"))).toBe("one");
    expect(viewerOf(holding("two"))).toBe("two");
  });

  it("plays for nobody when it holds no seat (a spectator)", () => {
    expect(viewerOf(holding())).toBeNull();
  });
});

describe("viewToShow", () => {
  const bothSeats = localSession(gameId, { one: "token-one", two: "token-two" });
  const seatOne: GameSession = { gameId, holds: (side) => side === "one", tokenFor: () => "token-one" };

  /** A fake `getView` that writes down the tokens it's called with. */
  function fakeApi(answer: GameView) {
    const tokens: string[] = [];
    const api = {
      getView: (_id: string, token: string) => {
        tokens.push(token);
        return Promise.resolve(answer);
      },
    };
    return { api, tokens };
  }

  it("keeps a view whose player is the one to move, without a request", async () => {
    const { api, tokens } = fakeApi(freshView("two"));
    const playerOnesTurn = freshView("one");

    expect(await viewToShow(api, bothSeats, playerOnesTurn)).toBe(playerOnesTurn);
    expect(tokens).toEqual([]);
  });

  it("fetches the view of the player to move when this browser holds that seat (pass & play)", async () => {
    const playerOnesView = freshView("one");
    const { api, tokens } = fakeApi(playerOnesView);

    expect(await viewToShow(api, bothSeats, freshView("two"))).toBe(playerOnesView);
    expect(tokens).toEqual(["token-one"]);
  });

  it("keeps the view when this browser doesn't hold the seat to move (online)", async () => {
    const { api, tokens } = fakeApi(freshView("two"));
    const waiting = view({ yourSide: "one", currentPlayer: "two" });

    expect(await viewToShow(api, seatOne, waiting)).toBe(waiting);
    expect(tokens).toEqual([]);
  });

  it("keeps the view of a finished game: there's nothing left to play from either seat", async () => {
    const { api, tokens } = fakeApi(freshView("one"));
    const finished = view({ yourSide: "two", currentPlayer: "one", status: "finished", winner: "one" });

    expect(await viewToShow(api, bothSeats, finished)).toBe(finished);
    expect(tokens).toEqual([]);
  });
});
