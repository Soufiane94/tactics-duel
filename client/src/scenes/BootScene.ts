import Phaser from "phaser";
import { GAME_TITLE } from "../config";
import { fetchHelloMessage } from "../net/hello";

/**
 * The first scene Phaser runs (it is first in the game config's `scene` array).
 * Step 0: shows the title and the API's message, proving browser → Vite proxy → API.
 * From Step 6 it will also generate the placeholder textures, then start GameScene.
 */
export class BootScene extends Phaser.Scene {
  constructor() {
    // The scene key: other scenes use it to start/stop this one, like a route name.
    super("BootScene");
  }

  private status!: Phaser.GameObjects.Text;

  /** Lifecycle: runs once, after `preload` (we have nothing to load yet). ≈ Angular's ngOnInit. */
  create(): void {
    // Scale Manager size = the base resolution (960×640), whatever the window size.
    const { width, height } = this.scale;
    const centerX = width / 2;
    const centerY = height / 2;

    // Phaser's default text style (font, colour) on purpose: visuals are a later, creative step.
    // setOrigin(0.5) anchors each text by its centre instead of its top-left corner.
    this.add.text(centerX, centerY - 40, GAME_TITLE, { fontSize: "48px" }).setOrigin(0.5);
    this.status = this.add
      .text(centerX, centerY + 30, "Contacting the API...", { fontSize: "24px" })
      .setOrigin(0.5);

    fetchHelloMessage()
      .then((message) => this.status.setText(message))
      .catch((error: unknown) => {
        const reason = error instanceof Error ? error.message : String(error);
        this.status.setText(`API unreachable: ${reason}`);
      });
  }

  update(time: number): void {
    this.status.setAlpha(0.5 + 0.5 * Math.sin(time / 300));
  }
}
