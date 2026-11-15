import { describe, expect, it } from "vitest";
import type { InputState } from "../game/InputController";
import { unit, view } from "../test/fixtures";
import { hudModelFor, unitPanelFor } from "./hudModel";

// Player One to move, turn 2: an injured warrior, a healer, and one enemy archer.
const game = view({
  turnNumber: 2,
  units: [
    unit("one-warrior", 3, 6, { hp: 20 }),
    unit("one-healer", 5, 6, { hasMoved: true }),
    unit("two-archer", 6, 1),
  ],
});

const idle: InputState = { kind: "idle" };
const selected = (unitId: string): InputState => ({ kind: "unitSelected", unitId });
const busy = (reselect: string | null): InputState => ({ kind: "busy", reselect });

describe("hudModelFor", () => {
  it("on your turn with nothing selected: the turn line, an enabled End Turn, no panel", () => {
    expect(hudModelFor(game, idle, null)).toEqual({
      turnLine: "Turn 2 · Player One to play",
      turnSide: "one",
      endTurnEnabled: true,
      unitPanel: null,
    });
  });

  it("speaks to the seat when the screen plays one side", () => {
    expect(hudModelFor(game, idle, "one").turnLine).toBe("Turn 2 · Your turn");
  });

  it("dims End Turn during the opponent's turn", () => {
    const theirTurn = view({ turnNumber: 2, currentPlayer: "two" });

    const model = hudModelFor(theirTurn, { kind: "opponentTurn" }, "one");

    expect(model).toMatchObject({ turnLine: "Turn 2 · Opponent's turn", turnSide: "two", endTurnEnabled: false });
  });

  it("shows the selected unit's panel", () => {
    expect(hudModelFor(game, selected("one-warrior"), null).unitPanel?.title).toBe("Warrior");
  });

  it("keeps the panel up while the selected unit's action plays, and dims End Turn", () => {
    const model = hudModelFor(game, busy("one-warrior"), null);

    expect(model.unitPanel?.title).toBe("Warrior");
    expect(model.endTurnEnabled).toBe(false);
  });

  it("hides the panel while End Turn plays: there's nothing to select again", () => {
    expect(hudModelFor(game, busy(null), null).unitPanel).toBeNull();
  });

  it("shows the result once the game is over, with the winner's swatch", () => {
    const over = view({ status: "finished", winner: "two" });

    expect(hudModelFor(over, { kind: "gameOver" }, null)).toEqual({
      turnLine: "Player Two wins!",
      turnSide: "two",
      endTurnEnabled: false,
      unitPanel: null,
    });
  });
});

describe("unitPanelFor", () => {
  it("lists the unit's HP and its type's stats from the view's unit types", () => {
    expect(unitPanelFor(game, "one-warrior")).toEqual({
      title: "Warrior",
      owner: "one",
      lines: ["HP 20 / 28", "Power 10", "Defense 3", "Move 3", "Range 1", "Damages enemies"],
    });
  });

  it("writes a range band as min–max, and says what a healer does", () => {
    expect(unitPanelFor(game, "two-archer")?.lines).toContain("Range 2–3");
    expect(unitPanelFor(game, "one-healer")?.lines).toEqual([
      "HP 16 / 16",
      "Power 5",
      "Defense 1",
      "Move 3",
      "Range 1–2",
      "Heals allies",
    ]);
  });

  it("is null for a unit that isn't on the board", () => {
    expect(unitPanelFor(game, "two-mage")).toBeNull();
  });
});
