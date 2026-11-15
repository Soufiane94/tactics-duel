/*
 * ████ PLACEHOLDER ████
 * The only file that decides what the game looks like today (CLAUDE.md, art rule): plain shapes and debug colours,
 * drawn in code, with no image files. Other files decide where things go and what's in front; this one decides how
 * they look. Your own art and style replace it in Step 9.
 */
import type Phaser from "phaser";
import { TILE_SIZE } from "../config";
import type { HighlightKind } from "../game/highlights";
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
  /** Highlights: the selected unit's tile, its move tiles, the units it can act on, and the tile under the pointer. */
  selectedHighlight: 0x00ffff,
  moveHighlight: 0xffffff,
  actHighlight: 0xffff00,
  hoverHighlight: 0xffffff,
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
  /** The line of the hover marker's square outline. */
  outlineWidth: 3,
} as const;

/** The style of a unit's letter: a size only. The font stays Phaser's default, because choosing one is your call. */
export const LETTER_STYLE: Phaser.Types.GameObjects.Text.TextStyle = {
  fontSize: `${Math.round(TILE_SIZE * 0.375)}px`,
};

/** The "−N" and "+N" that rise from units: a size only, in Phaser's default font and colour (white). */
export const FLOATING_NUMBER_STYLE: Phaser.Types.GameObjects.Text.TextStyle = { fontSize: "24px" };

/**
 * The hit flash: for a moment, the unit's body is drawn in this one colour. Tint FILL mode replaces the texture's
 * colours, where the usual MULTIPLY mixes them (white × blue = blue, but FILL white = white).
 */
export const HIT_FLASH_COLOUR = 0xffffff;

/** How faint the End Turn button gets while it can't be used (alpha: 0 is invisible, 1 is opaque). */
export const DISABLED_ALPHA = 0.4;

/** The HUD's text styles: sizes only, in Phaser's default font, because choosing a font is your call (Step 9). */
type TextStyle = Phaser.Types.GameObjects.Text.TextStyle;
export const HUD_TEXT = {
  turnLine: { fontSize: "22px" },
  button: { fontSize: "20px" },
  panelTitle: { fontSize: "22px" },
  panelLines: { fontSize: "17px", lineSpacing: 6 },
  banner: { fontSize: "40px" },
  victory: { fontSize: "48px" },
  victoryHint: { fontSize: "20px" },
  /** A plain black box behind the message, three-quarters opaque. */
  toast: { fontSize: "18px", align: "center", backgroundColor: "rgba(0, 0, 0, 0.75)", padding: { x: 12, y: 6 } },
} as const satisfies Record<string, TextStyle>;

/** PLACEHOLDER HUD shapes: plain black see-through backgrounds, and a square in a side's debug colour. */
export const HUD_SHAPES = {
  /** The square next to the turn line and the panel's title, in the side's colour (`PLACEHOLDER_THEME.team`). */
  swatchSize: 14,
  /** The unit panel: its width, the space inside its edges, and its background. */
  panel: { width: 176, padding: 12, colour: 0x000000, alpha: 0.35 },
  /** The turn banner: a strip across the whole screen. */
  bannerStrip: { height: 96, colour: 0x000000, alpha: 0.6 },
  /** The victory overlay dims everything under it. */
  overlay: { colour: 0x000000, alpha: 0.6 },
} as const;

/** The texture keys: the names the other files ask the Texture Manager for. */
export const PLACEHOLDER_TEXTURES = {
  tile: "placeholder-tile",
  obstacle: "placeholder-obstacle",
  unit: "placeholder-unit",
  highlight: "placeholder-highlight",
  outline: "placeholder-outline",
} as const;

/** One highlight's look: a white texture, its tint, and how see-through it is. */
export interface HighlightLook {
  readonly texture: string;
  readonly colour: number;
  readonly alpha: number;
}

/**
 * How each highlight looks. They're see-through, so the tile still shows underneath. Units are drawn above the
 * highlights (`DEPTH`), so a target's highlight shows around its body.
 */
export const HIGHLIGHT_LOOK = {
  selected: { texture: PLACEHOLDER_TEXTURES.highlight, colour: PLACEHOLDER_THEME.selectedHighlight, alpha: 0.45 },
  move: { texture: PLACEHOLDER_TEXTURES.highlight, colour: PLACEHOLDER_THEME.moveHighlight, alpha: 0.25 },
  act: { texture: PLACEHOLDER_TEXTURES.highlight, colour: PLACEHOLDER_THEME.actHighlight, alpha: 0.45 },
  hover: { texture: PLACEHOLDER_TEXTURES.outline, colour: PLACEHOLDER_THEME.hoverHighlight, alpha: 0.8 },
} as const satisfies Record<HighlightKind | "hover", HighlightLook>;

/** The letter on a unit: the first letter of its type's name ("Archer" → "A"). New types get theirs automatically. */
export function unitLetter(type: Pick<UnitTypeView, "name">): string {
  return type.name.trim().charAt(0).toUpperCase() || "?";
}

/**
 * Draws each shape once with a Graphics object, then bakes it into a texture (`generateTexture`): an image kept in
 * the Texture Manager, which every scene shares. Any number of Images can then show that texture, each with its own
 * position, scale and tint. That's cheaper than one Graphics object per tile, which rebuilds its shapes every frame.
 * The unit, highlight and outline shapes are white on purpose: tint multiplies colours, and white × blue = blue.
 */
export function createPlaceholderTextures(scene: Phaser.Scene): void {
  // `make` creates a game object without putting it on the display list (`add` would): an off-screen sketchpad.
  const pen = scene.make.graphics({}, false);
  const { tileBorder, unitRadius, outlineWidth } = PLACEHOLDER_SIZES;
  const inner = TILE_SIZE - 2 * tileBorder;
  // A stroke is centred on its path, so half the line falls outside the rectangle. Inset the path by half the line,
  // and the whole outline stays inside the tile's inner area.
  const outlineInset = tileBorder + outlineWidth / 2;

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
  bake(PLACEHOLDER_TEXTURES.outline, TILE_SIZE, TILE_SIZE, () =>
    pen
      .lineStyle(outlineWidth, 0xffffff)
      .strokeRect(outlineInset, outlineInset, TILE_SIZE - 2 * outlineInset, TILE_SIZE - 2 * outlineInset),
  );
  bake(PLACEHOLDER_TEXTURES.unit, unitRadius * 2, unitRadius * 2, () =>
    pen.fillStyle(0xffffff).fillCircle(unitRadius, unitRadius, unitRadius),
  );

  pen.destroy();
}
