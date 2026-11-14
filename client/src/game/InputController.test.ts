import { describe, expect, it } from "vitest";
import { options, unit, view } from "../test/fixtures";
import { acceptsInput, stateForView, transition, type InputEvent, type InputState } from "./InputController";

/*
 * Player One to move. The lists are what the server would send: the client doesn't check ranges, so neither do
 * these tests.
 *
 *      x: 0 1 2 3 4 5 6 7
 *   y=1   . . . . . . a .     a = two-archer, out of everyone's reach
 *   y=4   . w . . . . . .     w = two-warrior, in the archer's range
 *   y=6   . . . W . H . .     W = one-warrior (injured, can move) · H = one-healer (moved, can heal W)
 *   y=7   . A . . M . . .     A = one-archer (can move or shoot w) · M = one-mage (done for this turn)
 */
const yourTurn = view({
  units: [
    unit("one-archer", 1, 7),
    unit("one-warrior", 3, 6, { hp: 20 }),
    unit("one-healer", 5, 6, { hasMoved: true }),
    unit("one-mage", 4, 7, { hasMoved: true, hasActed: true }),
    unit("two-warrior", 1, 4),
    unit("two-archer", 6, 1),
  ],
  availableActions: [
    options("one-archer", [{ x: 1, y: 6 }, { x: 0, y: 7 }], ["two-warrior"]),
    options("one-warrior", [{ x: 3, y: 5 }]),
    options("one-healer", [], ["one-warrior"]),
  ],
});

const idle: InputState = { kind: "idle" };
const opponentTurn: InputState = { kind: "opponentTurn" };
const gameOver: InputState = { kind: "gameOver" };
const selected = (unitId: string): InputState => ({ kind: "unitSelected", unitId });
const busy = (reselect: string | null): InputState => ({ kind: "busy", reselect });

const click = (x: number, y: number): InputEvent => ({ type: "tileClicked", tile: { x, y } });
const cancel: InputEvent = { type: "cancelled" };
const endTurn: InputEvent = { type: "endTurnPressed" };
const viewReceived: InputEvent = { type: "viewReceived" };
const requestFailed: InputEvent = { type: "requestFailed" };

/** The answer to the archer's move: it can no longer move, but it can still shoot. */
const archerMoved = view({
  sequence: 1,
  units: [unit("one-archer", 1, 6, { hasMoved: true }), unit("two-warrior", 1, 4)],
  availableActions: [options("one-archer", [], ["two-warrior"])],
});

describe("stateForView", () => {
  it("is idle on your turn", () => {
    expect(stateForView(yourTurn)).toEqual(idle);
  });

  it("is the opponent's turn when the other side is to move", () => {
    expect(stateForView(view({ currentPlayer: "two" }))).toEqual(opponentTurn);
  });

  it("is game over once the game is finished, even for the winner, who is still the player to move", () => {
    expect(stateForView(view({ status: "finished", winner: "one" }))).toEqual(gameOver);
  });
});

describe("acceptsInput", () => {
  it.each([
    [idle, true],
    [selected("one-archer"), true],
    [busy("one-archer"), false],
    [opponentTurn, false],
    [gameOver, false],
  ])("is %o → %s", (state, expected) => {
    expect(acceptsInput(state)).toBe(expected);
  });
});

describe("idle", () => {
  it("selects one of your units that the server lists", () => {
    expect(transition(idle, click(1, 7), yourTurn)).toEqual({ state: selected("one-archer"), request: null });
  });

  it.each([
    ["a unit with nothing left to do", click(4, 7)],
    ["an enemy", click(1, 4)],
    ["an empty tile", click(7, 0)],
    ["Esc", cancel],
  ])("ignores %s", (_what, event) => {
    expect(transition(idle, event, yourTurn)).toEqual({ state: idle, request: null });
  });

  it("ends the turn on E, with no unit to reselect afterwards", () => {
    expect(transition(idle, endTurn, yourTurn)).toEqual({ state: busy(null), request: { type: "endTurn" } });
  });
});

describe("a unit selected", () => {
  const archer = selected("one-archer");

  it("moves to a highlighted tile, then waits, remembering the unit to reselect", () => {
    expect(transition(archer, click(1, 6), yourTurn)).toEqual({
      state: busy("one-archer"),
      request: { type: "move", unitId: "one-archer", to: { x: 1, y: 6 } },
    });
  });

  it("acts on a highlighted target", () => {
    expect(transition(archer, click(1, 4), yourTurn)).toEqual({
      state: busy("one-archer"),
      request: { type: "act", actorId: "one-archer", targetId: "two-warrior" },
    });
  });

  it("heals an injured ally rather than selecting it, even though that ally could move", () => {
    expect(transition(selected("one-healer"), click(3, 6), yourTurn)).toEqual({
      state: busy("one-healer"),
      request: { type: "act", actorId: "one-healer", targetId: "one-warrior" },
    });
  });

  it("switches to another of your units that the server lists", () => {
    expect(transition(archer, click(3, 6), yourTurn)).toEqual({ state: selected("one-warrior"), request: null });
  });

  it.each([
    ["the unit itself", click(1, 7)],
    ["a tile that isn't highlighted", click(7, 0)],
    ["an enemy out of reach", click(6, 1)],
    ["one of your units with nothing left to do", click(4, 7)],
    ["Esc", cancel],
  ])("deselects on %s", (_what, event) => {
    expect(transition(archer, event, yourTurn)).toEqual({ state: idle, request: null });
  });

  it("ends the turn on E", () => {
    expect(transition(archer, endTurn, yourTurn)).toEqual({ state: busy(null), request: { type: "endTurn" } });
  });

  it("keeps its selection when a new view still lists the unit", () => {
    expect(transition(archer, viewReceived, archerMoved)).toEqual({ state: archer, request: null });
  });

  it("drops its selection when a new view no longer lists the unit", () => {
    const archerDone = view({ units: [unit("one-archer", 1, 7, { hasActed: true })] });
    expect(transition(archer, viewReceived, archerDone)).toEqual({ state: idle, request: null });
  });
});

describe("busy", () => {
  it.each([
    ["a click on a unit", click(1, 7)],
    ["a click on a tile", click(1, 6)],
    ["Esc", cancel],
    ["E", endTurn],
  ])("ignores %s", (_what, event) => {
    expect(transition(busy("one-archer"), event, yourTurn)).toEqual({ state: busy("one-archer"), request: null });
  });

  it("selects the unit again when the answer still lists it", () => {
    expect(transition(busy("one-archer"), viewReceived, archerMoved)).toEqual({
      state: selected("one-archer"),
      request: null,
    });
  });

  it("goes back to idle when the unit has nothing left to do", () => {
    const archerActed = view({ units: [unit("one-archer", 1, 7, { hasMoved: true, hasActed: true })] });
    expect(transition(busy("one-archer"), viewReceived, archerActed)).toEqual({ state: idle, request: null });
  });

  it("shows the opponent's turn when the answer says so", () => {
    const turnPassed = view({ currentPlayer: "two" });
    expect(transition(busy(null), viewReceived, turnPassed)).toEqual({ state: opponentTurn, request: null });
  });

  it("shows game over when the action won the game", () => {
    const won = view({ status: "finished", winner: "one" });
    expect(transition(busy("one-archer"), viewReceived, won)).toEqual({ state: gameOver, request: null });
  });

  it("restores the selection when the request fails, because the old view still holds", () => {
    expect(transition(busy("one-archer"), requestFailed, yourTurn)).toEqual({
      state: selected("one-archer"),
      request: null,
    });
  });

  it("goes back to idle when an End Turn fails", () => {
    expect(transition(busy(null), requestFailed, yourTurn)).toEqual({ state: idle, request: null });
  });
});

describe("opponent's turn", () => {
  const waiting = view({ currentPlayer: "two" });

  it.each([
    ["a click", click(1, 7)],
    ["E", endTurn],
  ])("ignores %s", (_what, event) => {
    expect(transition(opponentTurn, event, waiting)).toEqual({ state: opponentTurn, request: null });
  });

  it("becomes idle when a view says it's your turn again", () => {
    expect(transition(opponentTurn, viewReceived, yourTurn)).toEqual({ state: idle, request: null });
  });
});

describe("game over", () => {
  const won = view({ status: "finished", winner: "two", currentPlayer: "two" });

  it.each([
    ["a click", click(1, 7)],
    ["E", endTurn],
    ["a new view", viewReceived],
  ])("ignores %s: a finished game stays finished", (_what, event) => {
    expect(transition(gameOver, event, won)).toEqual({ state: gameOver, request: null });
  });
});

describe("requestFailed outside busy", () => {
  it("changes nothing: the console's request failed, not one of the player's", () => {
    expect(transition(selected("one-archer"), requestFailed, yourTurn)).toEqual({
      state: selected("one-archer"),
      request: null,
    });
  });
});
