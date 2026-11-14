import Phaser from "phaser";
import { GAME_HEIGHT, GAME_TITLE, GAME_WIDTH, PIXEL_ART } from "./config";
import { BootScene } from "./scenes/BootScene";
import { GameScene } from "./scenes/GameScene";
import { HudScene } from "./scenes/HudScene";

// The browser tab shows the same constant as the in-game title (naming rule).
document.title = GAME_TITLE;

// Creating the Game boots everything: renderer, canvas, Scale Manager, and the game loop
// (about 60 frames per second), then starts the first scene in `scene`.
new Phaser.Game({
  // WebGL renderer. Phaser 4 deprecates Canvas; WEBGL (unlike AUTO) never silently falls back.
  type: Phaser.WEBGL,
  // The <div id="game"> in index.html receives the <canvas>.
  parent: "game",
  // Shown in Phaser's console banner.
  title: GAME_TITLE,
  // Crisp scaling for pixel art: off for now (see PIXEL_ART in config.ts).
  pixelArt: PIXEL_ART,
  scale: {
    // Draw at 960×640, then scale the canvas to fit the window, keeping the aspect ratio
    // (letterboxing), and centre it both ways.
    mode: Phaser.Scale.FIT,
    autoCenter: Phaser.Scale.CENTER_BOTH,
    width: GAME_WIDTH,
    height: GAME_HEIGHT,
  },
  // Only the first scene starts by itself: BootScene starts GameScene, which launches HudScene.
  // The list is also the drawing order, so HudScene, last, is drawn on top of GameScene.
  scene: [BootScene, GameScene, HudScene],
});
