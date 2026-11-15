import type Phaser from "phaser";
import { GAME_HEIGHT, GAME_WIDTH, TILE_SIZE } from "../config";
import { FEEL } from "../game/feel";
import { tween, wait } from "../game/tweens";
import { HUD_SHAPES, HUD_TEXT } from "../media/placeholder";

/*
 * The two big announcements, both played by events and drawn by the HUD, above the board: the turn banner
 * (`turnEnded`) and the victory overlay (`gameWon`). Each `play` resolves when GameScene may go on.
 */

/** What the victory overlay says under the result, until Step 10 brings a menu. */
const PLAY_AGAIN_HINT = "Reload the page to play again.";

/** A strip across the middle of the screen: it slides in, holds, then slides out. */
export class TurnBanner {
  private readonly scene: Phaser.Scene;
  private readonly container: Phaser.GameObjects.Container;
  private readonly label: Phaser.GameObjects.Text;

  constructor(scene: Phaser.Scene, depth: number) {
    this.scene = scene;
    const { height, colour, alpha } = HUD_SHAPES.bannerStrip;
    // The children are placed around the container's (0, 0), which sits at the screen's centre.
    const strip = scene.add.rectangle(0, 0, GAME_WIDTH, height, colour, alpha);
    this.label = scene.add.text(0, 0, "", HUD_TEXT.banner).setOrigin(0.5);
    this.container = scene.add
      .container(GAME_WIDTH / 2, GAME_HEIGHT / 2, [strip, this.label])
      .setDepth(depth)
      .setVisible(false);
  }

  /** Plays the banner with `text`, and resolves once it's gone. */
  async play(text: string): Promise<void> {
    const { inMs, inEase, holdMs, outMs, outEase, slide } = FEEL.banner;
    const centreX = GAME_WIDTH / 2;
    const slidePx = slide * TILE_SIZE;

    this.label.setText(text);
    this.container.setPosition(centreX - slidePx, GAME_HEIGHT / 2).setAlpha(0).setVisible(true);
    await tween(this.scene, { targets: this.container, x: centreX, alpha: 1, duration: inMs, ease: inEase });
    await wait(this.scene, holdMs);
    await tween(this.scene, { targets: this.container, x: centreX + slidePx, alpha: 0, duration: outMs, ease: outEase });
    this.container.setVisible(false);
  }
}

/** A dark layer over the whole screen, with the result in the middle. Once shown, it stays. */
export class VictoryOverlay {
  private readonly scene: Phaser.Scene;
  private readonly container: Phaser.GameObjects.Container;
  private readonly title: Phaser.GameObjects.Text;

  constructor(scene: Phaser.Scene, depth: number) {
    this.scene = scene;
    const centreX = GAME_WIDTH / 2;
    const centreY = GAME_HEIGHT / 2;
    const { colour, alpha } = HUD_SHAPES.overlay;
    const dim = scene.add.rectangle(centreX, centreY, GAME_WIDTH, GAME_HEIGHT, colour, alpha);
    this.title = scene.add.text(centreX, centreY - 24, "", HUD_TEXT.victory).setOrigin(0.5);
    const hint = scene.add.text(centreX, centreY + 36, PLAY_AGAIN_HINT, HUD_TEXT.victoryHint).setOrigin(0.5);
    this.container = scene.add.container(0, 0, [dim, this.title, hint]).setDepth(depth).setVisible(false);
  }

  /** Fades the overlay in with `text`, and resolves once it's fully shown. */
  async play(text: string): Promise<void> {
    const { fadeMs, ease } = FEEL.victory;
    this.title.setText(text);
    this.container.setAlpha(0).setVisible(true);
    await tween(this.scene, { targets: this.container, alpha: 1, duration: fadeMs, ease });
  }
}
