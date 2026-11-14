/**
 * Player-visible game title. This is the ONLY place in code where it is written
 * (CLAUDE.md, naming rule): the boot screen, the browser tab and later the menu all read it.
 * Renaming the game = changing this one line.
 */
export const GAME_TITLE = "the rumble";

/**
 * Base (logical) resolution, in game pixels. Every scene draws in this 960×640 space;
 * Phaser's Scale Manager then stretches the canvas to fit the window (like an SVG viewBox).
 */
export const GAME_WIDTH = 960;
export const GAME_HEIGHT = 640;

/**
 * The size of one board tile, in game pixels. Every position on the board derives from it (`game/grid.ts`).
 * An 8×8 board at 64 px is 512×512, which fits the 640 px height with room to spare.
 * With real art (Step 9), match it to the size of your tile images.
 */
export const TILE_SIZE = 64;

/**
 * Crisp scaling for pixel art. When true, Phaser turns antialiasing off: textures scale with nearest-neighbour
 * sampling (hard pixel edges, no blur), the browser upscales the canvas the same way, and positions snap to whole
 * pixels (`roundPixels`). Keep it false for smooth art and for today's generated shapes, which look best smooth.
 * Decide it together with your art style (Step 9).
 */
export const PIXEL_ART = false;

/**
 * Draw order inside GameScene, from back to front. Phaser draws a higher `depth` on top, whatever the creation
 * order, like CSS `z-index`. Gaps of 10 leave room for in-between layers later.
 * The HUD isn't listed: it's a separate scene, drawn after (so above) the whole GameScene.
 */
export const DEPTH = {
  tiles: 0,
  highlights: 10,
  units: 20,
  effects: 30,
} as const;
