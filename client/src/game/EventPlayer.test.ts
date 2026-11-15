import { describe, expect, it } from "vitest";
import { EventPlayer, type AnimationLog, type Animator } from "./EventPlayer";
import type { GameEvent } from "./types";

/** Lets everything already scheduled run: pending timers of 0 ms, and the promise callbacks they trigger. */
const settle = (): Promise<void> => new Promise((resolve) => setTimeout(resolve, 0));

/**
 * A fake Animator. Each animation writes "start …" when it begins and "end …" when it finishes.
 * By default it finishes on the next turn of the event loop, like a very short tween. With `manual`, it waits for
 * the test to call `finish()`, so a test can look at the moment between two animations.
 */
function fakeAnimator({ manual = false } = {}) {
  const log: string[] = [];
  const running: (() => void)[] = [];

  const animation = (label: string): Promise<void> => {
    log.push(`start ${label}`);
    return new Promise((resolve) => {
      const end = (): void => {
        log.push(`end ${label}`);
        resolve();
      };
      if (manual) {
        running.push(end);
      } else {
        setTimeout(end, 0);
      }
    });
  };

  const animator: Animator = {
    unitMoved: (e) => animation(`unitMoved ${e.unitId} to ${e.to.x},${e.to.y} in ${e.path.length} steps`),
    unitDamaged: (e) => animation(`unitDamaged ${e.unitId} −${e.amount} by ${e.byUnitId}, ${e.hpAfter} left`),
    unitHealed: (e) => animation(`unitHealed ${e.unitId} +${e.amount} by ${e.byUnitId}, ${e.hpAfter} left`),
    unitDefeated: (e) => animation(`unitDefeated ${e.unitId}`),
    turnEnded: (e) => animation(`turnEnded ${e.previousPlayer} → ${e.nextPlayer}, turn ${e.turnNumber}`),
    gameWon: (e) => animation(`gameWon ${e.winner}`),
  };

  /** Finishes the oldest animation still running (manual mode). */
  const finish = (): void => running.shift()?.();
  return { animator, log, finish };
}

/** Stands in for the console: keeps the warnings, so a test can read them (and the test output stays clean). */
function fakeReporter() {
  const warnings: unknown[][] = [];
  const reporter: AnimationLog = { warn: (...args: unknown[]) => warnings.push(args) };
  return { reporter, warnings };
}

// The archer walks two tiles, then shoots the injured mage, which falls: the last enemy, so Player One wins.
const moved: GameEvent = {
  type: "unitMoved",
  unitId: "one-archer",
  from: { x: 1, y: 7 },
  to: { x: 1, y: 5 },
  path: [{ x: 1, y: 6 }, { x: 1, y: 5 }],
};
const damaged: GameEvent = { type: "unitDamaged", unitId: "two-mage", byUnitId: "one-archer", amount: 8, hpAfter: 0 };
const defeated: GameEvent = { type: "unitDefeated", unitId: "two-mage" };
const won: GameEvent = { type: "gameWon", winner: "one" };
const healed: GameEvent = { type: "unitHealed", unitId: "one-warrior", byUnitId: "one-healer", amount: 5, hpAfter: 25 };
const turnEnded: GameEvent = { type: "turnEnded", previousPlayer: "one", nextPlayer: "two", turnNumber: 2 };

describe("EventPlayer", () => {
  it("plays the events in order, each one starting only after the previous one has ended", async () => {
    const { animator, log } = fakeAnimator();

    await new EventPlayer(animator).play([moved, damaged, defeated, won]);

    // Playing them all at once (Promise.all) would log the four starts first.
    expect(log).toEqual([
      "start unitMoved one-archer to 1,5 in 2 steps",
      "end unitMoved one-archer to 1,5 in 2 steps",
      "start unitDamaged two-mage −8 by one-archer, 0 left",
      "end unitDamaged two-mage −8 by one-archer, 0 left",
      "start unitDefeated two-mage",
      "end unitDefeated two-mage",
      "start gameWon one",
      "end gameWon one",
    ]);
  });

  it("sends each event type to its own Animator method, with the event's data", async () => {
    const { animator, log } = fakeAnimator();

    await new EventPlayer(animator).play([moved, damaged, healed, defeated, turnEnded, won]);

    expect(log.filter((line) => line.startsWith("start"))).toEqual([
      "start unitMoved one-archer to 1,5 in 2 steps",
      "start unitDamaged two-mage −8 by one-archer, 0 left",
      "start unitHealed one-warrior +5 by one-healer, 25 left",
      "start unitDefeated two-mage",
      "start turnEnded one → two, turn 2",
      "start gameWon one",
    ]);
  });

  it("resolves only once the last animation has ended: the moment the scene snaps to the view", async () => {
    const { animator, log, finish } = fakeAnimator({ manual: true });
    let played = false;

    const playing = new EventPlayer(animator).play([moved, damaged]).then(() => {
      played = true;
    });

    await settle();
    expect(log).toEqual(["start unitMoved one-archer to 1,5 in 2 steps"]);

    finish(); // the walk ends: the shot starts, and the batch is still playing
    await settle();
    expect(log.at(-1)).toBe("start unitDamaged two-mage −8 by one-archer, 0 left");
    expect(played).toBe(false);

    finish(); // the shot ends: nothing left
    await playing;
    expect(played).toBe(true);
  });

  it("resolves at once for an empty list, animating nothing", async () => {
    const { animator, log } = fakeAnimator();

    await new EventPlayer(animator).play([]);

    expect(log).toEqual([]);
  });

  it("skips an animation that fails, plays the rest, and reports it", async () => {
    const { animator, log } = fakeAnimator();
    const { reporter, warnings } = fakeReporter();
    const brokenHit: Animator = { ...animator, unitDamaged: () => Promise.reject(new Error("no sprite")) };

    await new EventPlayer(brokenHit, reporter).play([moved, damaged, defeated]);

    expect(log).toEqual([
      "start unitMoved one-archer to 1,5 in 2 steps",
      "end unitMoved one-archer to 1,5 in 2 steps",
      "start unitDefeated two-mage",
      "end unitDefeated two-mage",
    ]);
    expect(warnings).toEqual([["The unitDamaged animation failed, so it was skipped.", new Error("no sprite")]]);
  });

  it("also survives an animation that throws instead of returning a promise", async () => {
    const { animator, log } = fakeAnimator();
    const { reporter, warnings } = fakeReporter();
    const brokenWalk: Animator = {
      ...animator,
      unitMoved: () => {
        throw new Error("no path");
      },
    };

    await new EventPlayer(brokenWalk, reporter).play([moved, turnEnded]);

    expect(log).toEqual(["start turnEnded one → two, turn 2", "end turnEnded one → two, turn 2"]);
    expect(warnings).toHaveLength(1);
  });

  it("queues a batch that arrives while another one plays: they never interleave", async () => {
    const { animator, log } = fakeAnimator();
    const player = new EventPlayer(animator);

    // Two answers, the second one arriving before the first has finished playing (Step 11's pushes).
    const first = player.play([moved, damaged]);
    const second = player.play([turnEnded]);
    await Promise.all([first, second]);

    expect(log).toEqual([
      "start unitMoved one-archer to 1,5 in 2 steps",
      "end unitMoved one-archer to 1,5 in 2 steps",
      "start unitDamaged two-mage −8 by one-archer, 0 left",
      "end unitDamaged two-mage −8 by one-archer, 0 left",
      "start turnEnded one → two, turn 2",
      "end turnEnded one → two, turn 2",
    ]);
  });

  it("keeps the queue going after a batch with a failed animation", async () => {
    const { animator, log } = fakeAnimator();
    const { reporter } = fakeReporter();
    const player = new EventPlayer({ ...animator, gameWon: () => Promise.reject(new Error("no HUD")) }, reporter);

    await player.play([won]);
    await player.play([turnEnded]);

    expect(log).toEqual(["start turnEnded one → two, turn 2", "end turnEnded one → two, turn 2"]);
  });
});
