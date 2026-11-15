import type Phaser from "phaser";
import { GAME_HEIGHT, GAME_WIDTH } from "../config";
import { FEEL } from "../game/feel";
import { stopTweensOf, tween } from "../game/tweens";
import { HUD_TEXT } from "../media/placeholder";

/**
 * A short message at the bottom of the screen (a refusal, a network error): it fades in, stays a few seconds, then
 * fades out. A newer message replaces the one on screen and restarts the countdown. "Toast" because it pops up, like
 * bread from a toaster.
 */
export class Toast {
  private readonly scene: Phaser.Scene;
  private readonly label: Phaser.GameObjects.Text;
  private hideTimer: Phaser.Time.TimerEvent | null = null;

  constructor(scene: Phaser.Scene, depth: number) {
    this.scene = scene;
    this.label = scene.add
      .text(GAME_WIDTH / 2, GAME_HEIGHT - 32, "", { ...HUD_TEXT.toast, wordWrap: { width: GAME_WIDTH - 80 } })
      .setOrigin(0.5)
      .setDepth(depth)
      .setAlpha(0);
  }

  /** Shows `text` now, for `FEEL.toast.showMs`. */
  show(text: string): void {
    const { showMs, fadeMs } = FEEL.toast;
    // A message still fading in or out stops where it is: this one takes over from there.
    stopTweensOf(this.scene, [this.label]);
    this.hideTimer?.remove();

    this.label.setText(text);
    void tween(this.scene, { targets: this.label, alpha: 1, duration: fadeMs });
    // The scene's clock, not `setTimeout`: it pauses with the scene, and its timers are destroyed when the scene stops.
    this.hideTimer = this.scene.time.delayedCall(showMs, () => {
      void tween(this.scene, { targets: this.label, alpha: 0, duration: fadeMs });
    });
  }
}
