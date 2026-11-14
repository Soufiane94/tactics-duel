import { describe, expect, it } from "vitest";
import { view } from "../test/fixtures";
import { statusLine } from "./turnText";

describe("statusLine", () => {
  it("names the turn and the player to move", () => {
    expect(statusLine(view({ turnNumber: 3, currentPlayer: "two" }))).toBe("Turn 3 · Player Two to play");
  });

  it("names the winner once the game is over", () => {
    expect(statusLine(view({ status: "finished", winner: "one" }))).toBe("Player One wins!");
  });

  it("still says the game is over without a winner", () => {
    expect(statusLine(view({ status: "finished", winner: null }))).toBe("Game over");
  });
});
