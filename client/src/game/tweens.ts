import type Phaser from "phaser";
import { FEEL } from "./feel";

/*
 * Phaser says a tween has ended through callbacks (`onComplete`, `onStop`). These helpers wrap them in Promises, so
 * an animation reads as steps, `await walk(); await lunge();`, instead of callbacks nested in callbacks. Like
 * wrapping an event-based .NET API in a `TaskCompletionSource`.
 * Don't pass your own `onComplete` or `onStop`: the helpers replace them.
 * A tween destroyed with its scene calls neither, so its promise never settles. Nothing waits by then: that game is
 * over, and GameScene makes a new EventPlayer for the next one.
 */

/** Plays one tween, and resolves when it completes, or when it's stopped. */
export function tween(scene: Phaser.Scene, config: Phaser.Types.Tweens.TweenBuilderConfig): Promise<void> {
  return new Promise((resolve) => {
    scene.tweens.add({ ...config, onComplete: () => resolve(), onStop: () => resolve() });
  });
}

/** Plays tweens one after another (a chain), and resolves when the last one completes, or the chain is stopped. */
export function tweenChain(scene: Phaser.Scene, config: Phaser.Types.Tweens.TweenChainBuilderConfig): Promise<void> {
  return new Promise((resolve) => {
    scene.tweens.chain({ ...config, onComplete: () => resolve(), onStop: () => resolve() });
  });
}

/** Waits on the scene's clock, which follows `FEEL.speed` and pauses with the scene, unlike `setTimeout`. */
export function wait(scene: Phaser.Scene, ms: number): Promise<void> {
  return new Promise((resolve) => {
    scene.time.delayedCall(ms, () => resolve());
  });
}

/**
 * Stops every tween running on these objects, leaving them where they are. A stopped tween calls `onStop`, so
 * anything awaiting it carries on. (`tweens.killTweensOf` destroys them without a call: the awaiting promise would
 * hang.) Tween chains count too: they report the objects their tweens move.
 */
export function stopTweensOf(scene: Phaser.Scene, targets: readonly object[]): void {
  for (const target of targets) {
    for (const running of scene.tweens.getTweensOf(target)) {
      running.stop();
    }
  }
}

/** Plays this scene's tweens and game timers at `FEEL.speed`: 1 is normal, 0.25 is slow motion. */
export function applyFeelSpeed(scene: Phaser.Scene): void {
  scene.tweens.timeScale = FEEL.speed;
  scene.time.timeScale = FEEL.speed;
}
