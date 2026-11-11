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
