import { GAME_HEIGHT, GAME_WIDTH, TILE_SIZE } from "../config";
import type { BoardView, Position } from "./types";

/*
 * Two coordinate systems, both with the origin at the top-left, x to the right and y down:
 * - grid space counts tiles: (0, 0) is the board's top-left tile, as in the server's `Position`;
 * - world space counts game pixels: (0, 0) is the top-left corner of the 960×640 canvas.
 * Converting between them is a scale (the tile size) plus an offset (where the board starts on screen).
 * No Phaser import: these are plain functions, tested with Vitest.
 */

/** Where the board sits in world space, and how big it is. */
export interface BoardLayout {
  /** World x of the board's left edge (the left side of column 0). */
  readonly originX: number;
  /** World y of the board's top edge (the top side of row 0). */
  readonly originY: number;
  /** One tile's width and height, in game pixels. */
  readonly tileSize: number;
  /** The board's size, in tiles. */
  readonly width: number;
  readonly height: number;
}

/** A point in world space (game pixels). */
export interface WorldPoint {
  readonly x: number;
  readonly y: number;
}

/**
 * Centres a board in the base resolution. The board's size comes from the scenario on the server, so the layout is
 * computed, not hard-coded: a 6×6 or a 10×8 scenario is centred too. It's rounded down to whole pixels, so tile
 * edges fall on pixel boundaries instead of being blurred across two pixels.
 */
export function layoutBoard(
  board: Pick<BoardView, "width" | "height">,
  tileSize: number = TILE_SIZE,
  area: { readonly width: number; readonly height: number } = { width: GAME_WIDTH, height: GAME_HEIGHT },
): BoardLayout {
  return {
    originX: Math.floor((area.width - board.width * tileSize) / 2),
    originY: Math.floor((area.height - board.height * tileSize) / 2),
    tileSize,
    width: board.width,
    height: board.height,
  };
}

/**
 * The world position of a tile's centre. That's where a unit on the tile is drawn: Phaser positions images by their
 * centre by default (origin 0.5).
 */
export function tileToWorld(layout: BoardLayout, tile: Position): WorldPoint {
  return {
    x: layout.originX + tile.x * layout.tileSize + layout.tileSize / 2,
    y: layout.originY + tile.y * layout.tileSize + layout.tileSize / 2,
  };
}

/**
 * The tile under a world position (a mouse click, from Step 7), or null outside the board.
 * `Math.floor`, not `Math.trunc`: a point 1 pixel left of the board gives -0.02 tiles, which `floor` turns into -1
 * (outside) but `trunc` would turn into 0 (the first column).
 */
export function worldToTile(layout: BoardLayout, x: number, y: number): Position | null {
  const tileX = Math.floor((x - layout.originX) / layout.tileSize);
  const tileY = Math.floor((y - layout.originY) / layout.tileSize);
  const onBoard = tileX >= 0 && tileX < layout.width && tileY >= 0 && tileY < layout.height;
  return onBoard ? { x: tileX, y: tileY } : null;
}

/**
 * A tile as a string key, `"x,y"`, for a `Map` or a `Set`. JavaScript compares objects by reference, so two
 * `{ x: 1, y: 2 }` literals are two different keys. Strings compare by value, like a C# record struct.
 */
export function posKey(tile: Position): string {
  return `${tile.x},${tile.y}`;
}
