import { describe, expect, it } from "vitest";
import { view } from "../test/fixtures";
import { bannerText, resultText, turnLine } from "./turnText";

// `null`: a shared screen (pass & play), so the texts name the sides. A side: a screen that plays one seat (online).

describe("turnLine", () => {
  it("on a shared screen, names the turn and the side to play", () => {
    expect(turnLine(view({ turnNumber: 3, currentPlayer: "two" }), null)).toBe("Turn 3 · Player Two to play");
  });

  it("for one seat, says whose turn it is from that seat", () => {
    expect(turnLine(view({ turnNumber: 3, currentPlayer: "two" }), "two")).toBe("Turn 3 · Your turn");
    expect(turnLine(view({ turnNumber: 4, currentPlayer: "one" }), "two")).toBe("Turn 4 · Opponent's turn");
  });

  it("shows the result once the game is over", () => {
    expect(turnLine(view({ status: "finished", winner: "one" }), null)).toBe("Player One wins!");
    expect(turnLine(view({ status: "finished", winner: "one" }), "two")).toBe("You lose");
  });
});

describe("bannerText", () => {
  it("names the side about to play on a shared screen", () => {
    expect(bannerText("two", null)).toBe("Player Two's turn");
  });

  it("speaks to the seat otherwise", () => {
    expect(bannerText("one", "one")).toBe("Your turn");
    expect(bannerText("two", "one")).toBe("Opponent's turn");
  });
});

describe("resultText", () => {
  it("names the winner on a shared screen", () => {
    expect(resultText("two", null)).toBe("Player Two wins!");
  });

  it("tells the seat whether it won", () => {
    expect(resultText("one", "one")).toBe("You win!");
    expect(resultText("one", "two")).toBe("You lose");
  });

  it("still says the game is over without a winner", () => {
    expect(resultText(null, null)).toBe("Game over");
    expect(resultText(null, "one")).toBe("Game over");
  });
});
