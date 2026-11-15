import { describe, expect, it } from "vitest";
import { FEEL } from "./feel";
import { breathingScale, hpFraction, lungeOffset } from "./unitDisplay";

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
  const { breathPeriodMs, breathAmount } = FEEL.idle;

  it("starts at the normal size", () => {
    expect(breathingScale(0)).toBe(1);
  });

  it("is biggest a quarter into a breath and smallest at three quarters", () => {
    expect(breathingScale(breathPeriodMs / 4)).toBeCloseTo(1 + breathAmount);
    expect(breathingScale((breathPeriodMs * 3) / 4)).toBeCloseTo(1 - breathAmount);
  });

  it("repeats every breath", () => {
    for (const time of [0, 333, 1000, 1800]) {
      expect(breathingScale(time + breathPeriodMs)).toBeCloseTo(breathingScale(time));
    }
  });
});

describe("lungeOffset", () => {
  const from = { x: 100, y: 100 };

  it("points straight at a target in the same row or column", () => {
    expect(lungeOffset(from, { x: 164, y: 100 }, 20)).toEqual({ x: 20, y: 0 }); // right
    expect(lungeOffset(from, { x: 100, y: 36 }, 20)).toEqual({ x: 0, y: -20 }); // up: y grows down
  });

  it("has the same length for a target next door and three tiles away", () => {
    expect(lungeOffset(from, { x: 164, y: 100 }, 20)).toEqual(lungeOffset(from, { x: 292, y: 100 }, 20));
  });

  it("slants toward a target off to the side, keeping its length", () => {
    // A 3-4-5 triangle: the target is 3 units right and 4 down, 5 away.
    const offset = lungeOffset(from, { x: 130, y: 140 }, 20);
    expect(offset.x).toBeCloseTo(12);
    expect(offset.y).toBeCloseTo(16);
    expect(Math.hypot(offset.x, offset.y)).toBeCloseTo(20);
  });

  it("is zero when the target is where the attacker stands (no division by zero)", () => {
    expect(lungeOffset(from, from, 20)).toEqual({ x: 0, y: 0 });
  });
});
