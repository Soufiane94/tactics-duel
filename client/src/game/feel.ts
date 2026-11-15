/*
 * Game feel: every animation timing, easing curve and shake strength, in one table. All values are *placeholder*:
 * tune them by playing. "Juice" is the name for these small, exaggerated reactions (a lunge, a flash, a shake, a
 * number that pops up): they don't change the rules, but they make each action read clearly and feel heavy.
 * - Durations are in milliseconds.
 * - Distances are in tiles, so they follow `TILE_SIZE` (`config.ts`) when your art changes the tile size.
 * - Easings are Phaser's curve names. `Linear` moves at a steady speed; `easeOut` starts fast and settles, like a
 *   thrown object; `easeIn` starts slow and speeds up, like a fall; `easeInOut` does both, like a person walking.
 * No Phaser import: it's plain data, so the pure modules and their tests can read it.
 */

/** The families of easing curves Phaser knows by name. */
type EaseFamily = "Quad" | "Cubic" | "Quart" | "Quint" | "Sine" | "Expo" | "Circ" | "Elastic" | "Back" | "Bounce";

/**
 * Every easing name Phaser looks up, such as "Sine.easeInOut". A template literal type builds them from the parts,
 * so a typo here is a compile error. Phaser itself wouldn't complain: it silently falls back to `Linear`.
 */
export type EaseName = "Linear" | `${EaseFamily}.${"easeIn" | "easeOut" | "easeInOut"}`;

/** What `FEEL` may hold: numbers grouped by effect, and easing names. Used only to check the table below. */
type FeelTable = { readonly [effect: string]: number | { readonly [setting: string]: number | EaseName } };

export const FEEL = {
  /**
   * Plays every tween and game timer at this speed: 1 is normal, 0.25 is slow motion (to study an animation), 2 is
   * double speed. Camera shakes keep their own speed.
   */
  speed: 1,

  /** The idle breathing of waiting units: one breath (in and out), and how much they grow (0.03 = ±3%). */
  idle: { breathPeriodMs: 2400, breathAmount: 0.03 },

  /** A unit walking its path, one tile at a time. */
  walk: { msPerTile: 140, ease: "Sine.easeInOut" },

  /** The attacker's lunge toward its target, then back. Every attacker lunges the same distance, near or far. */
  lunge: { distance: 0.3, outMs: 90, outEase: "Quad.easeOut", backMs: 150, backEase: "Quad.easeInOut" },

  /**
   * The moment a hit lands: the target flashes white, and the camera shakes. The shake's strength is a share of the
   * screen size: 0.005 moves the board by up to about 5 pixels.
   */
  hit: { flashMs: 100, shakeMs: 120, shakeIntensity: 0.005 },

  /** An HP bar draining after a hit, or filling after a heal. */
  hpBar: { ms: 260, ease: "Cubic.easeOut" },

  /** The "−N" and "+N" that appear `startAbove` the unit's centre, rise, then fade (starting `fadeDelayMs` in). */
  floatingNumber: { startAbove: 0.25, rise: 0.6, ms: 900, fadeDelayMs: 400, ease: "Cubic.easeOut" },

  /** A defeated unit fading and shrinking away. `Back.easeIn` pulls back a little before it goes. */
  defeat: { ms: 450, endScale: 0.2, ease: "Back.easeIn" },

  /**
   * The turn banner: it slides in (from `slide` tiles to the left, fading in), holds, then slides out to the right.
   * The next player waits for all three before playing.
   */
  banner: { inMs: 220, inEase: "Cubic.easeOut", holdMs: 700, outMs: 220, outEase: "Cubic.easeIn", slide: 0.75 },

  /** The victory overlay fading in. It stays on screen afterwards. */
  victory: { fadeMs: 500, ease: "Quad.easeOut" },

  /** A message (a refusal, a network error): how long it stays, and its fade in and out. */
  toast: { showMs: 4000, fadeMs: 200 },
} as const satisfies FeelTable;
