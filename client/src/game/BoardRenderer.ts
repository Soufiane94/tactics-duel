import type Phaser from "phaser";
import { DEPTH } from "../config";
import { HIGHLIGHT_LOOK, PLACEHOLDER_TEXTURES, type HighlightLook } from "../media/placeholder";
import { layoutBoard, posKey, tileToWorld, type BoardLayout } from "./grid";
import type { Highlight } from "./highlights";
import type { BoardView, Position } from "./types";

/**
 * The board on screen. It decides where the board sits (`layout`), which every other piece asks to place things on
 * tiles, and it draws the board once: one Image per tile, floor or obstacle, on the back layer (`DEPTH.tiles`).
 * The board never changes during a game, so it's never redrawn. The highlights and the hover marker sit on their own
 * layer (`DEPTH.highlights`), above the tiles and below the units.
 */
export class BoardRenderer {
  readonly layout: BoardLayout;
  private readonly scene: Phaser.Scene;
  /** The images of the highlights on screen, replaced as a whole by `showHighlights`. */
  private highlightImages: Phaser.GameObjects.Image[] = [];
  /** One image that follows the pointer from tile to tile. */
  private readonly hover: Phaser.GameObjects.Image;

  constructor(scene: Phaser.Scene, board: BoardView) {
    this.scene = scene;
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

    // One step above the other highlights, so the marker stays visible on a lit tile. Still below the units.
    this.hover = this.addHighlight({ x: 0, y: 0 }, HIGHLIGHT_LOOK.hover)
      .setDepth(DEPTH.highlights + 1)
      .setVisible(false);
  }

  /**
   * Replaces the highlights on screen with a new list. They change on clicks and answers, a few times per turn, so
   * destroying and recreating a few dozen images costs nothing. Something redrawn every frame would keep its images
   * and reuse them instead (a pool).
   */
  showHighlights(highlights: readonly Highlight[]): void {
    for (const image of this.highlightImages) {
      image.destroy();
    }
    this.highlightImages = highlights.map((highlight) =>
      this.addHighlight(highlight.tile, HIGHLIGHT_LOOK[highlight.kind]),
    );
  }

  /** Puts the hover marker on a tile, or hides it (null: the pointer is off the board, or clicks do nothing now). */
  showHover(tile: Position | null): void {
    if (tile === null) {
      this.hover.setVisible(false);
      return;
    }
    const centre = tileToWorld(this.layout, tile);
    this.hover.setPosition(centre.x, centre.y).setVisible(true);
  }

  /** One highlight image: a white texture, tinted (white × colour = colour, as for the units' bodies). */
  private addHighlight(tile: Position, look: HighlightLook): Phaser.GameObjects.Image {
    const centre = tileToWorld(this.layout, tile);
    return this.scene.add
      .image(centre.x, centre.y, look.texture)
      .setTint(look.colour)
      .setAlpha(look.alpha)
      .setDepth(DEPTH.highlights);
  }
}
