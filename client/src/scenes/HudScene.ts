import Phaser from "phaser";

/**
 * The heads-up display (Step 8): the turn, the selected unit, the End Turn button and messages. Empty for now.
 * It's a scene of its own, launched by GameScene and running at the same time. Scenes draw in the order of the game
 * config's `scene` list, and this one comes last, so it's always on top. It also has its own camera, so when
 * GameScene's camera shakes or zooms (Step 8), the HUD stays still.
 */
export class HudScene extends Phaser.Scene {
  static readonly KEY = "HudScene";

  constructor() {
    super(HudScene.KEY);
  }
}
