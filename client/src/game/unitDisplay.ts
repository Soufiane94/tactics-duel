/*
 * The maths behind what a unit shows: how full its HP bar is, and its idle "breathing".
 * No Phaser import, so Vitest tests it. `UnitSprite` applies these numbers to game objects.
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

/** One full breath (in and out), in milliseconds. *Placeholder* feel; Step 8 gathers such numbers in `feel.ts`. */
export const BREATH_PERIOD_MS = 2400;

/** How much a unit grows and shrinks while breathing: 0.03 is ±3% of its size. *Placeholder* feel. */
export const BREATH_AMOUNT = 0.03;

/**
 * A unit's idle scale at a moment in time: a slow sine wave around 1.
 * It's computed from the clock (`timeMs`, milliseconds since the game started), not counted in frames, so it looks
 * the same at 30, 60 or 144 frames per second: a late frame simply reads the wave a little further along. Adding a
 * fixed step every frame (`scale += 0.001`) would breathe 2.4 times faster on a 144 Hz screen than on a 60 Hz one.
 */
export function breathingScale(timeMs: number, phase: number = 0): number {
  const breaths = timeMs / BREATH_PERIOD_MS + phase; // how many full breaths since the start
  return 1 + BREATH_AMOUNT * Math.sin(breaths * 2 * Math.PI);
}
