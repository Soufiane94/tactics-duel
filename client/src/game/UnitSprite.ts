import Phaser from "phaser";
import { DEPTH } from "../config";
import { LETTER_STYLE, PLACEHOLDER_SIZES, PLACEHOLDER_TEXTURES, PLACEHOLDER_THEME, unitLetter } from "../media/placeholder";
import { tileToWorld, type BoardLayout } from "./grid";
import type { UnitTypeView, UnitView } from "./types";
import { breathingScale, hpFraction } from "./unitDisplay";

/**
 * One unit on screen. It's a Container: a game object that holds other game objects and moves them as one.
 * Children are placed relative to the container, like elements inside a `position: relative` div, so (0, 0) is the
 * centre of the unit's tile. The parts, from back to front:
 * - the body: a white circle, tinted with the owner's debug colour;
 * - the first letter of the unit's type;
 * - the HP bar: a dark back, and a fill that shrinks with the HP.
 * It shows what `applyState` receives from the server's view, and decides nothing.
 */
export class UnitSprite extends Phaser.GameObjects.Container {
  // Not named `body` or `type`: every Phaser game object already has those (its physics body, and "Container").
  private readonly bodyImage: Phaser.GameObjects.Image;
  private readonly hpFill: Phaser.GameObjects.Rectangle;
  private readonly unitType: UnitTypeView;
  private readonly layout: BoardLayout;
  private readonly breathPhase: number;
  constructor(scene: Phaser.Scene, layout: BoardLayout, unit: UnitView, unitType: UnitTypeView) {
    super(scene);
    this.layout = layout;
    this.unitType = unitType;
    const sizes = PLACEHOLDER_SIZES;
    const teamColour = PLACEHOLDER_THEME.team[unit.owner];
    this.breathPhase = ((unit.x * 3 + unit.y * 5) % 8) / 8;
    // Tint multiplies the texture's colours (MULTIPLY is Phaser 4's default tint mode), and white × blue = blue.
    // So both teams share one texture, and each Image carries its own colour.
    this.bodyImage = scene.add.image(0, sizes.unitOffsetY, PLACEHOLDER_TEXTURES.unit).setTint(teamColour);
    const letter = scene.add.text(0, sizes.unitOffsetY, unitLetter(unitType), LETTER_STYLE).setOrigin(0.5);

    // The bar's origin is its left end (0, 0.5), so a smaller scaleX shrinks it towards the left, like a gauge.
    const barLeft = -sizes.hpBarWidth / 2;
    const hpBack = scene.add
      .rectangle(barLeft, sizes.hpBarOffsetY, sizes.hpBarWidth, sizes.hpBarHeight, PLACEHOLDER_THEME.hpBarBack)
      .setOrigin(0, 0.5);
    this.hpFill = scene.add
      .rectangle(barLeft, sizes.hpBarOffsetY, sizes.hpBarWidth, sizes.hpBarHeight, teamColour)
      .setOrigin(0, 0.5);

    // add() moves the parts from the scene's display list into the container, which draws them in this order.
    this.add([this.bodyImage, letter, hpBack, this.hpFill]);
    // The container itself goes on the scene's display list, on the units layer: above the tiles and highlights.
    this.setDepth(DEPTH.units);
    scene.add.existing(this);

    this.applyState(unit);
  }

  /** Snaps the sprite to the unit's state from the server: its tile and its HP. */
  applyState(unit: UnitView): void {
    const centre = tileToWorld(this.layout, unit);
    this.setPosition(centre.x, centre.y);
    this.hpFill.setScale(hpFraction(unit.hp, this.unitType.maxHp), 1);
  }

  /**
   * The idle animation, called by GameScene every frame. Only the body breathes: the letter and the HP bar stay
   * steady, and the container's own scale stays free for Step 8's tweens (a defeated unit shrinking away).
   */
  breathe(time: number): void {
    this.bodyImage.setScale(breathingScale(time, this.breathPhase));
  }
}
