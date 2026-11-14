import { describe, expect, it } from "vitest";
import type { PlayOutcome } from "../net/play";
import { view as fakeView } from "../test/fixtures";
import { createDebugCommands, type DebugTarget } from "./debugCommands";
import type { GameAction, GameView } from "./types";

/** A fake console that keeps what was printed. */
function fakeLog() {
  const lines: unknown[][] = [];
  return {
    lines,
    log: {
      info: (...data: unknown[]) => lines.push(["info", ...data]),
      warn: (...data: unknown[]) => lines.push(["warn", ...data]),
      table: (data: unknown) => lines.push(["table", data]),
    },
  };
}

/** A fake scene that records the actions it's asked to play, and answers with `outcome`. */
function fakeTarget(outcome: PlayOutcome, view: GameView | null = null) {
  const played: GameAction[] = [];
  const target: DebugTarget = {
    play: (action) => {
      played.push(action);
      return Promise.resolve(outcome);
    },
    currentView: () => view,
  };
  return { played, target };
}

const accepted = (sequence: number): PlayOutcome => ({
  kind: "accepted",
  view: fakeView({ sequence }),
  events: [],
  message: null,
});

describe("createDebugCommands", () => {
  it("sends the actions of the API contract", async () => {
    const { played, target } = fakeTarget(accepted(1));
    const { log } = fakeLog();
    const tactics = createDebugCommands(target, log);

    await tactics.move("one-warrior", 3, 4);
    await tactics.act("one-archer", "two-warrior");
    await tactics.endTurn();

    expect(played).toEqual([
      { type: "move", unitId: "one-warrior", to: { x: 3, y: 4 } },
      { type: "act", actorId: "one-archer", targetId: "two-warrior" },
      { type: "endTurn" },
    ]);
  });

  it("prints the server's events when it accepts the action", async () => {
    const { target } = fakeTarget(accepted(7));
    const { lines, log } = fakeLog();

    await createDebugCommands(target, log).endTurn();

    expect(lines).toEqual([["info", "Action 7 accepted. What happened:", []]]);
  });

  it("prints the rule code when the server refuses the action", async () => {
    const { target } = fakeTarget({
      kind: "rejected",
      message: "Warrior can't reach (0, 0) this turn.",
      code: "DestinationNotReachable",
    });
    const { lines, log } = fakeLog();

    await createDebugCommands(target, log).move("one-warrior", 0, 0);

    expect(lines).toEqual([["warn", "Not played (DestinationNotReachable). Warrior can't reach (0, 0) this turn."]]);
  });

  it("prints only the message when a refusal has no code (no network)", async () => {
    const { target } = fakeTarget({ kind: "rejected", message: "Can't reach the server.", code: null });
    const { lines, log } = fakeLog();

    await createDebugCommands(target, log).endTurn();

    expect(lines).toEqual([["warn", "Not played. Can't reach the server."]]);
  });

  it("says when the view on screen was out of date", async () => {
    const { target } = fakeTarget({
      kind: "resynced",
      view: fakeView({ sequence: 9 }),
      message: "The game had changed.",
    });
    const { lines, log } = fakeLog();

    await createDebugCommands(target, log).endTurn();

    expect(lines).toEqual([["warn", "Not played (SequenceConflict). The game had changed."]]);
  });

  it("lists the units of the view on screen", () => {
    const view = {
      turnNumber: 3,
      currentPlayer: "two",
      units: [{ id: "two-mage", typeId: "mage", owner: "two", x: 3, y: 0, hp: 14, hasMoved: false, hasActed: false }],
    } as GameView; // only the fields `units()` reads
    const { target } = fakeTarget(accepted(1), view);
    const { lines, log } = fakeLog();

    createDebugCommands(target, log).units();

    expect(lines).toEqual([
      ["info", "Turn 3: player two to move. Tiles are (x, y) from the top-left."],
      ["table", [{ id: "two-mage", x: 3, y: 0, hp: 14, hasMoved: false, hasActed: false }]],
    ]);
  });
});
