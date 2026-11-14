/*
 * ████ PLACEHOLDER ████
 * The only file that decides what the game looks like today (CLAUDE.md, art rule): plain shapes and debug colours,
 * drawn in code, with no image files. Other files decide where things go and what's in front; this one decides how
 * they look. Your own art and style replace it in Step 9.
 */
import type Phaser from "phaser";
import { TILE_SIZE } from "../config";
import type { PlayerSide, UnitTypeView } from "../game/types";

/** PLACEHOLDER colours, as `0xRRGGBB` numbers (the form Phaser takes): neutral greys, two debug team colours. */
export const PLACEHOLDER_THEME = {
  /** A floor tile. Its 1-pixel border stays empty, so the black background shows through as grid lines. */
  tile: 0x5a5a5a,
  /** An obstacle tile, darker than the floor. */
  obstacle: 0x202020,
  /** One plain debug colour per side, for the unit's body and its HP bar. */
  team: { one: 0x4080ff, two: 0xff4040 } satisfies Record<PlayerSide, number>,
  /** The empty part of an HP bar. */
  hpBarBack: 0x000000,
  /** Highlights (Step 7): the tiles a unit can move to, and the units it can act on. */
  moveHighlight: 0xffffff,
  actHighlight: 0xffff00,
} as const;

/** PLACEHOLDER sizes, in game pixels. They follow `TILE_SIZE`, so the look scales with the tiles. */
export const PLACEHOLDER_SIZES = {
  /** The empty border on each side of a tile: two neighbours leave a 2-pixel line between them. */
  tileBorder: 1,
  /** The radius of a unit's round body. */
  unitRadius: Math.round(TILE_SIZE * 0.34),
  /** The body sits a little above the tile's centre, to leave room for the HP bar. */
  unitOffsetY: -Math.round(TILE_SIZE * 0.06),
  /** The HP bar, centred under the body. */
  hpBarWidth: TILE_SIZE - 16,
  hpBarHeight: 6,
  hpBarOffsetY: Math.round(TILE_SIZE * 0.36),
} as const;

/** The style of a unit's letter: a size only. The font stays Phaser's default, because choosing one is your call. */
export const LETTER_STYLE: Phaser.Types.GameObjects.Text.TextStyle = {
  fontSize: `${Math.round(TILE_SIZE * 0.375)}px`,
};

/** The texture keys: the names the other files ask the Texture Manager for. */
export const PLACEHOLDER_TEXTURES = {
  tile: "placeholder-tile",
  obstacle: "placeholder-obstacle",
  unit: "placeholder-unit",
  highlight: "placeholder-highlight",
} as const;

/** The letter on a unit: the first letter of its type's name ("Archer" → "A"). New types get theirs automatically. */
export function unitLetter(type: Pick<UnitTypeView, "name">): string {
  return type.name.trim().charAt(0).toUpperCase() || "?";
}

/**
 * Draws each shape once with a Graphics object, then bakes it into a texture (`generateTexture`): an image kept in
 * the Texture Manager, which every scene shares. Any number of Images can then show that texture, each with its own
 * position, scale and tint. That's cheaper than one Graphics object per tile, which rebuilds its shapes every frame.
 * The unit and highlight shapes are white on purpose: tint multiplies colours, and white × blue = blue.
 */
export function createPlaceholderTextures(scene: Phaser.Scene): void {
  // `make` creates a game object without putting it on the display list (`add` would): an off-screen sketchpad.
  const pen = scene.make.graphics({}, false);
  const { tileBorder, unitRadius } = PLACEHOLDER_SIZES;
  const inner = TILE_SIZE - 2 * tileBorder;

  const bake = (key: string, width: number, height: number, draw: () => void): void => {
    // Texture keys are game-wide and unique: a texture can't be added twice (a restarted scene would try).
    if (scene.textures.exists(key)) {
      return;
    }
    pen.clear();
    draw();
    pen.generateTexture(key, width, height);
  };

  bake(PLACEHOLDER_TEXTURES.tile, TILE_SIZE, TILE_SIZE, () =>
    pen.fillStyle(PLACEHOLDER_THEME.tile).fillRect(tileBorder, tileBorder, inner, inner),
  );
  bake(PLACEHOLDER_TEXTURES.obstacle, TILE_SIZE, TILE_SIZE, () =>
    pen.fillStyle(PLACEHOLDER_THEME.obstacle).fillRect(tileBorder, tileBorder, inner, inner),
  );
  bake(PLACEHOLDER_TEXTURES.highlight, TILE_SIZE, TILE_SIZE, () =>
    pen.fillStyle(0xffffff).fillRect(tileBorder, tileBorder, inner, inner),
  );
  bake(PLACEHOLDER_TEXTURES.unit, unitRadius * 2, unitRadius * 2, () =>
    pen.fillStyle(0xffffff).fillCircle(unitRadius, unitRadius, unitRadius),
  );

  pen.destroy();
}
