import { FEEL } from "./feel";
import type { WorldPoint } from "./grid";

/*
 * The maths behind what a unit shows: how full its HP bar is, its idle "breathing", and the direction of its lunge.
 * No Phaser import, so Vitest tests it. `UnitSprite` and `SceneAnimator` apply these numbers to game objects.
 */

/**
 * How full the HP bar is: 0 (defeated) to 1 (full health). It's clamped, so odd data can never draw a bar longer
 * than its frame or with a negative width.
 */
export function hpFraction(hp: number, maxHp: number): number {
  if (maxHp <= 0) {
    return 0;
  }
  return Math.min(1, Math.max(0, hp / maxHp));
}

/**
 * A unit's idle scale at a moment in time: a slow sine wave around 1 (its speed and size are in `FEEL.idle`).
 * It's computed from the clock (`timeMs`, milliseconds since the game started), not counted in frames, so it looks
 * the same at 30, 60 or 144 frames per second: a late frame simply reads the wave a little further along. Adding a
 * fixed step every frame (`scale += 0.001`) would breathe 2.4 times faster on a 144 Hz screen than on a 60 Hz one.
 */
export function breathingScale(timeMs: number, phase: number = 0): number {
  const { breathPeriodMs, breathAmount } = FEEL.idle;
  const breaths = timeMs / breathPeriodMs + phase; // how many full breaths since the start
  return 1 + breathAmount * Math.sin(breaths * 2 * Math.PI);
}

/**
 * How far an attacker lunges, as an offset from where it stands: `distance` pixels straight toward its target.
 * The direction is normalised (divided by its length), so an archer three tiles away lunges exactly as far as a
 * warrior next door, and a target off to the side gets a slanted lunge. Zero if both points are the same.
 */
export function lungeOffset(from: WorldPoint, to: WorldPoint, distance: number): WorldPoint {
  const dx = to.x - from.x;
  const dy = to.y - from.y;
  const length = Math.hypot(dx, dy);
  if (length === 0) {
    return { x: 0, y: 0 };
  }
  return { x: (dx / length) * distance, y: (dy / length) * distance };
}
