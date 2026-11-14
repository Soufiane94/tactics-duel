import Phaser from "phaser";
import { GAME_TITLE } from "../config";
import { createPlaceholderTextures } from "../media/placeholder";
import { fetchHelloMessage } from "../net/hello";
import { readLaunchMode } from "../net/session";
import { GameScene } from "./GameScene";

/**
 * The first scene Phaser runs (it is first in the game config's `scene` array). It makes the placeholder textures,
 * then decides where to go from the URL:
 * - `?mode=local`: it starts GameScene, a local (pass & play) game;
 * - anything else: it shows the title and the API's message (Step 0), with a hint. Step 10 puts a menu here.
 */
export class BootScene extends Phaser.Scene {
  static readonly KEY = "BootScene";

  constructor() {
    // The scene key: other scenes use it to start/stop this one, like a route name.
    super(BootScene.KEY);
  }

  /** Only shown on the boot screen; in local mode, the scene stops before creating it. */
  private status?: Phaser.GameObjects.Text;

  /** Lifecycle: runs once, after `preload` (we have nothing to load). ≈ Angular's ngOnInit. */
  create(): void {
    // The Texture Manager belongs to the whole game, so the textures made here are ready in every scene.
    createPlaceholderTextures(this);

    if (readLaunchMode(window.location.search) === "local") {
      // start() stops this scene and starts GameScene. It's queued: it happens at the start of the next frame.
      this.scene.start(GameScene.KEY);
      return;
    }

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
    this.add
      .text(centerX, centerY + 100, "Add ?mode=local to the address to play a local game.", { fontSize: "18px" })
      .setOrigin(0.5);

    fetchHelloMessage()
      .then((message) => this.status?.setText(message))
      .catch((error: unknown) => {
        const reason = error instanceof Error ? error.message : String(error);
        this.status?.setText(`API unreachable: ${reason}`);
      });
  }

  update(time: number): void {
    // `?.`: when GameScene is starting, this scene still gets one update before it stops, and has no status text.
    this.status?.setAlpha(0.5 + 0.5 * Math.sin(time / 300));
  }
}
