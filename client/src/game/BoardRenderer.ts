import type Phaser from "phaser";
import { DEPTH } from "../config";
import { PLACEHOLDER_TEXTURES } from "../media/placeholder";
import { layoutBoard, posKey, tileToWorld, type BoardLayout } from "./grid";
import type { BoardView } from "./types";

/**
 * The board on screen. It decides where the board sits (`layout`), which every other piece asks to place things on
 * tiles, and it draws the board once: one Image per tile, floor or obstacle, on the back layer (`DEPTH.tiles`).
 * The board never changes during a game, so nothing is redrawn. Step 7 adds highlights here, on their own layer.
 */
export class BoardRenderer {
  readonly layout: BoardLayout;

  constructor(scene: Phaser.Scene, board: BoardView) {
    this.layout = layoutBoard(board);

    const obstacles = new Set(board.obstacles.map(posKey));
    for (let y = 0; y < board.height; y++) {
      for (let x = 0; x < board.width; x++) {
        const tile = { x, y };
        const texture = obstacles.has(posKey(tile)) ? PLACEHOLDER_TEXTURES.obstacle : PLACEHOLDER_TEXTURES.tile;
        const centre = tileToWorld(this.layout, tile);
        scene.add.image(centre.x, centre.y, texture).setDepth(DEPTH.tiles);
      }
    }
  }
}
