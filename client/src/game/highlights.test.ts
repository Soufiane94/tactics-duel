import { describe, expect, it } from "vitest";
import { options, unit, view } from "../test/fixtures";
import { highlightsFor } from "./highlights";

const game = view({
  units: [unit("one-archer", 1, 7), unit("two-warrior", 1, 4), unit("two-mage", 2, 5)],
  availableActions: [options("one-archer", [{ x: 1, y: 6 }, { x: 0, y: 7 }], ["two-warrior", "two-mage"])],
});

describe("highlightsFor", () => {
  it("lights the selected unit, then its moves, then the tiles of its targets, all from availableActions", () => {
    expect(highlightsFor({ kind: "unitSelected", unitId: "one-archer" }, game)).toEqual([
      { tile: { x: 1, y: 7 }, kind: "selected" },
      { tile: { x: 1, y: 6 }, kind: "move" },
      { tile: { x: 0, y: 7 }, kind: "move" },
      { tile: { x: 1, y: 4 }, kind: "act" },
      { tile: { x: 2, y: 5 }, kind: "act" },
    ]);
  });

  it.each([
    { kind: "idle" },
    { kind: "busy", reselect: "one-archer" },
    { kind: "opponentTurn" },
    { kind: "gameOver" },
  ] as const)("lights nothing when the state is $kind", (state) => {
    expect(highlightsFor(state, game)).toEqual([]);
  });

  it("lights nothing for a unit the server doesn't list", () => {
    expect(highlightsFor({ kind: "unitSelected", unitId: "two-warrior" }, game)).toEqual([]);
  });

  it("skips a target that isn't on the board", () => {
    const stale = view({
      units: [unit("one-archer", 1, 7)],
      availableActions: [options("one-archer", [], ["two-warrior"])],
    });
    expect(highlightsFor({ kind: "unitSelected", unitId: "one-archer" }, stale)).toEqual([
      { tile: { x: 1, y: 7 }, kind: "selected" },
    ]);
  });
});
