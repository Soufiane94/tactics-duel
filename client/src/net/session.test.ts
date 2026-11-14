import { describe, expect, it } from "vitest";
import type { GameView, PlayerSide } from "../game/types";
import { readLaunchMode, startLocalGame, type LobbyApi } from "./session";

const gameId = "0b8f6e2a-61c4-7d0e-9a35-2f1c8d4e5b6a";

/** A small but complete view of a fresh game (Player One to move), as `side` sees it. */
function freshView(side: PlayerSide): GameView {
  return {
    gameId,
    sequence: 0,
    yourSide: side,
    currentPlayer: "one",
    turnNumber: 1,
    status: "inProgress",
    winner: null,
    opponentJoined: side === "two",
    board: { width: 8, height: 8, obstacles: [] },
    unitTypes: [],
    units: [],
    availableActions: [],
  };
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
  });
});
