import { describe, expect, it } from "vitest";
import { BREATH_AMOUNT, BREATH_PERIOD_MS, breathingScale, hpFraction } from "./unitDisplay";

describe("hpFraction", () => {
  it("is the share of HP left", () => {
    expect(hpFraction(18, 18)).toBe(1);
    expect(hpFraction(9, 18)).toBe(0.5);
    expect(hpFraction(0, 28)).toBe(0);
  });

  it("stays between 0 and 1, whatever the data", () => {
    expect(hpFraction(-5, 28)).toBe(0);
    expect(hpFraction(30, 28)).toBe(1);
    expect(hpFraction(5, 0)).toBe(0); // no division by zero
  });
});

describe("breathingScale", () => {
  it("starts at the normal size", () => {
    expect(breathingScale(0)).toBe(1);
  });

  it("is biggest a quarter into a breath and smallest at three quarters", () => {
    expect(breathingScale(BREATH_PERIOD_MS / 4)).toBeCloseTo(1 + BREATH_AMOUNT);
    expect(breathingScale((BREATH_PERIOD_MS * 3) / 4)).toBeCloseTo(1 - BREATH_AMOUNT);
  });

  it("repeats every breath", () => {
    for (const time of [0, 333, 1000, 1800]) {
      expect(breathingScale(time + BREATH_PERIOD_MS)).toBeCloseTo(breathingScale(time));
    }
  });
});
