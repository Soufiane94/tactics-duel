import type { GameEvent, GameWon, TurnEnded, UnitDamaged, UnitDefeated, UnitHealed, UnitMoved } from "./types";

/*
 * Plays the server's events as animations, one after another. An accepted action answers with two things: the
 * events (what happened, in order) and the view (where the game is now). The events are only for the eye: once they
 * have played, the scene snaps to the view, which is the truth (CLAUDE.md, rule 3). So an animation can be skipped,
 * or be slightly off, and the screen still ends up right.
 * No Phaser import: the sequencing is tested with a fake `Animator`.
 */

/**
 * Draws one event. Each method resolves when the next event may start: the end of the event's "beat".
 * It may leave purely decorative effects running (a floating number, a camera shake), but never anything the snap to
 * the view redraws: a unit's position, its HP bar or its fading must be finished, or the snap would cut them short.
 * `SceneAnimator` draws with Phaser; tests pass a fake. It decides no rule: the event says what happened.
 */
export interface Animator {
  unitMoved(event: UnitMoved): Promise<void>;
  unitDamaged(event: UnitDamaged): Promise<void>;
  unitHealed(event: UnitHealed): Promise<void>;
  unitDefeated(event: UnitDefeated): Promise<void>;
  turnEnded(event: TurnEnded): Promise<void>;
  gameWon(event: GameWon): Promise<void>;
}

/** Where failed animations are reported: the browser console, or a fake in tests. */
export type AnimationLog = Pick<Console, "warn">;

/**
 * Plays batches of events through an `Animator`. A batch is the `events` list of one answer.
 * - In a batch, each event starts when the previous one's beat ends: `await` in a loop, not `Promise.all`, which
 *   would start them all at once (the lunge before the walk, the fade before the hit).
 * - A batch that arrives while another one plays waits for it: a queue. Two answers never animate at once. From
 *   Step 11, the opponent's actions arrive by push, sometimes in quick succession.
 * - An animation that fails (a bug, a missing sprite) is reported and skipped. The snap to the view fixes the screen
 *   anyway, and the input, locked until playback ends, never stays locked because of it.
 */
export class EventPlayer {
  private readonly animator: Animator;
  private readonly log: AnimationLog;
  /** Settles once everything queued so far has played. It never rejects: failures are caught one event at a time. */
  private queue: Promise<void> = Promise.resolve();

  constructor(animator: Animator, log: AnimationLog = console) {
    this.animator = animator;
    this.log = log;
  }

  /** Queues a batch, and resolves once it has played: the moment to snap to the new view. */
  play(events: readonly GameEvent[]): Promise<void> {
    const batch = this.queue.then(() => this.playInOrder(events));
    this.queue = batch;
    return batch;
  }

  private async playInOrder(events: readonly GameEvent[]): Promise<void> {
    for (const event of events) {
      try {
        await animate(this.animator, event);
      } catch (error) {
        this.log.warn(`The ${event.type} animation failed, so it was skipped.`, error);
      }
    }
  }
}

/**
 * Sends one event to its Animator method. There's no `default`: every case returns, so when a new event type joins
 * `GameEvent`, TypeScript reports this function (TS2366, "lacks ending return statement") until it's animated.
 */
function animate(animator: Animator, event: GameEvent): Promise<void> {
  switch (event.type) {
    case "unitMoved":
      return animator.unitMoved(event);
    case "unitDamaged":
      return animator.unitDamaged(event);
    case "unitHealed":
      return animator.unitHealed(event);
    case "unitDefeated":
      return animator.unitDefeated(event);
    case "turnEnded":
      return animator.turnEnded(event);
    case "gameWon":
      return animator.gameWon(event);
  }
}
