import { describe, expect, it } from "vitest";
import { GAME_TITLE } from "./config";

describe("config", () => {
  it("defines a non-empty game title", () => {
    expect(GAME_TITLE.trim()).not.toBe("");
  });
});
