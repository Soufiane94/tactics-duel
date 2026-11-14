import { describe, expect, it } from "vitest";
import { GAME_ID, view } from "../test/fixtures";
import type { GameView, SubmitActionResponse } from "../game/types";
import { ApiError, ApiUnreachableError } from "./ApiClient";
import { playAction, type PlayApi } from "./play";
import { localSession, type GameSession } from "./session";

/** Pass & play: this browser holds both seats. */
const bothSeats = localSession(GAME_ID, { one: "token-one", two: "token-two" });

/** Online (Step 10): this browser holds seat one only. */
const seatOne: GameSession = { gameId: GAME_ID, holds: (side) => side === "one", tokenFor: () => "token-one" };

interface Script {
  readonly submit: () => Promise<SubmitActionResponse>;
  readonly getView?: (token: string) => Promise<GameView>;
}

/** A fake API that writes down every request and answers from a script. */
function fakeApi(script: Script) {
  const calls: string[] = [];
  const api: PlayApi = {
    submitAction: (gameId, token, request) => {
      calls.push(`POST ${gameId} ${request.action.type} on view ${request.expectedSequence} with ${token}`);
      return script.submit();
    },
    getView: (gameId, token) => {
      calls.push(`GET ${gameId} with ${token}`);
      return script.getView === undefined ? Promise.reject(new Error("unexpected GET")) : script.getView(token);
    },
  };
  return { api, calls };
}

const answer = (next: GameView): Promise<SubmitActionResponse> =>
  Promise.resolve({ sequence: next.sequence, events: [], view: next });

const refusal = new ApiError({
  title: "Rule violation",
  status: 422,
  code: "TargetOutOfRange",
  detail: "Archer hits targets 2–3 tiles away; this target is 1 tile away.",
});

const conflict = new ApiError({ title: "Sequence conflict", status: 409, code: "SequenceConflict", detail: "…" });

/** What `ApiClient` throws when nothing answers. */
const unreachable = new ApiUnreachableError(new TypeError("Failed to fetch"));

const onScreen = view({ sequence: 7, currentPlayer: "two", yourSide: "two" });
const move = { type: "move", unitId: "two-archer", to: { x: 1, y: 2 } } as const;

describe("playAction", () => {
  it("posts the action with the token of the player to move and the sequence of the view on screen", async () => {
    const next = view({ sequence: 8, currentPlayer: "two", yourSide: "two" });
    const { api, calls } = fakeApi({ submit: () => answer(next) });

    await playAction(api, bothSeats, onScreen, move);

    expect(calls).toEqual([`POST ${GAME_ID} move on view 7 with token-two`]);
  });

  it("shows the answer's view while the same player is still to move: one request", async () => {
    const next = view({ sequence: 8, currentPlayer: "two", yourSide: "two" });
    const { api, calls } = fakeApi({ submit: () => answer(next) });

    const outcome = await playAction(api, bothSeats, onScreen, move);

    expect(outcome).toEqual({ kind: "accepted", view: next, events: [], message: null });
    expect(calls).toHaveLength(1);
  });

  it("after End Turn in pass & play, fetches the next player's view with their token", async () => {
    const sendersView = view({ sequence: 8, currentPlayer: "one", yourSide: "two" });
    const nextPlayersView = view({ sequence: 8, currentPlayer: "one", yourSide: "one" });
    const { api, calls } = fakeApi({
      submit: () => answer(sendersView),
      getView: () => Promise.resolve(nextPlayersView),
    });

    const outcome = await playAction(api, bothSeats, onScreen, { type: "endTurn" });

    expect(calls).toEqual([`POST ${GAME_ID} endTurn on view 7 with token-two`, `GET ${GAME_ID} with token-one`]);
    expect(outcome).toEqual({ kind: "accepted", view: nextPlayersView, events: [], message: null });
  });

  it("with one seat (online), keeps the answer's view after End Turn: now it's the opponent's turn", async () => {
    const myView = view({ sequence: 3, currentPlayer: "one", yourSide: "one" });
    const waiting = view({ sequence: 4, currentPlayer: "two", yourSide: "one" });
    const { api, calls } = fakeApi({ submit: () => answer(waiting) });

    const outcome = await playAction(api, seatOne, myView, { type: "endTurn" });

    expect(calls).toEqual([`POST ${GAME_ID} endTurn on view 3 with token-one`]);
    expect(outcome).toEqual({ kind: "accepted", view: waiting, events: [], message: null });
  });

  it("422: rejected with the server's sentence and the rule's code", async () => {
    const { api } = fakeApi({ submit: () => Promise.reject(refusal) });

    expect(await playAction(api, bothSeats, onScreen, move)).toEqual({
      kind: "rejected",
      message: "Archer hits targets 2–3 tiles away; this target is 1 tile away.",
      code: "TargetOutOfRange",
    });
  });

  it("409: fetches the game again and hands it over, without sending the action twice", async () => {
    const fresh = view({ sequence: 9, currentPlayer: "two", yourSide: "two" });
    const { api, calls } = fakeApi({ submit: () => Promise.reject(conflict), getView: () => Promise.resolve(fresh) });

    const outcome = await playAction(api, bothSeats, onScreen, move);

    expect(calls).toEqual([`POST ${GAME_ID} move on view 7 with token-two`, `GET ${GAME_ID} with token-two`]);
    expect(outcome).toEqual({
      kind: "resynced",
      view: fresh,
      message: "The game had changed, so your action wasn't played. The board is up to date now.",
    });
  });

  it("409, then the fresh view fails to load: rejected, with why", async () => {
    const { api } = fakeApi({ submit: () => Promise.reject(conflict), getView: () => Promise.reject(unreachable) });

    expect(await playAction(api, bothSeats, onScreen, move)).toEqual({
      kind: "rejected",
      message: "Can't reach the server.",
      code: null,
    });
  });

  it("no answer at all: rejected with a sentence for the player", async () => {
    const { api } = fakeApi({ submit: () => Promise.reject(unreachable) });

    expect(await playAction(api, bothSeats, onScreen, move)).toEqual({
      kind: "rejected",
      message: "Can't reach the server.",
      code: null,
    });
  });

  it("played, but the next view didn't load: still accepted, with the sender's view and a message", async () => {
    const sendersView = view({ sequence: 8, currentPlayer: "one", yourSide: "two" });
    const { api } = fakeApi({ submit: () => answer(sendersView), getView: () => Promise.reject(unreachable) });

    expect(await playAction(api, bothSeats, onScreen, { type: "endTurn" })).toEqual({
      kind: "accepted",
      view: sendersView,
      events: [],
      message: "Played, but the next player's view didn't load. Can't reach the server.",
    });
  });
});
