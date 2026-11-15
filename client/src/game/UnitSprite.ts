import Phaser from "phaser";
import { DEPTH } from "../config";
import {
  HIT_FLASH_COLOUR,
  LETTER_STYLE,
  PLACEHOLDER_SIZES,
  PLACEHOLDER_TEXTURES,
  PLACEHOLDER_THEME,
  unitLetter,
} from "../media/placeholder";
import { FEEL } from "./feel";
import { tileToWorld, type BoardLayout, type WorldPoint } from "./grid";
import { stopTweensOf, tween, tweenChain, wait } from "./tweens";
import type { Position, UnitTypeView, UnitView } from "./types";
import { breathingScale, hpFraction } from "./unitDisplay";

/**
 * One unit on screen. It's a Container: a game object that holds other game objects and moves them as one.
 * Children are placed relative to the container, like elements inside a `position: relative` div, so (0, 0) is the
 * centre of the unit's tile. The parts, from back to front:
 * - the body: a white circle, tinted with the owner's debug colour;
 * - the first letter of the unit's type;
 * - the HP bar: a dark back, and a fill that shrinks with the HP.
 * It shows what `applyState` receives from the server's view, and decides nothing. Its animations (`walk`, `lunge…`,
 * `flash`, `showHp`, `vanish`) only show events; each returns a promise that resolves when it ends.
 */
export class UnitSprite extends Phaser.GameObjects.Container {
  // Not named `body` or `type`: every Phaser game object already has those (its physics body, and "Container").
  private readonly bodyImage: Phaser.GameObjects.Image;
  private readonly hpFill: Phaser.GameObjects.Rectangle;
  private readonly unitType: UnitTypeView;
  private readonly layout: BoardLayout;
  private readonly breathPhase: number;
  private readonly teamColour: number;
  /** Where the unit stood when its lunge started: `lungeBack` returns there. Null when it isn't lunging. */
  private lungeStart: WorldPoint | null = null;

  constructor(scene: Phaser.Scene, layout: BoardLayout, unit: UnitView, unitType: UnitTypeView) {
    super(scene);
    this.layout = layout;
    this.unitType = unitType;
    const sizes = PLACEHOLDER_SIZES;
    this.teamColour = PLACEHOLDER_THEME.team[unit.owner];
    this.breathPhase = ((unit.x * 3 + unit.y * 5) % 8) / 8;
    // Tint multiplies the texture's colours (MULTIPLY is Phaser 4's default tint mode), and white × blue = blue.
    // So both teams share one texture, and each Image carries its own colour.
    this.bodyImage = scene.add.image(0, sizes.unitOffsetY, PLACEHOLDER_TEXTURES.unit).setTint(this.teamColour);
    const letter = scene.add.text(0, sizes.unitOffsetY, unitLetter(unitType), LETTER_STYLE).setOrigin(0.5);

    // The bar's origin is its left end (0, 0.5), so a smaller scaleX shrinks it towards the left, like a gauge.
    const barLeft = -sizes.hpBarWidth / 2;
    const hpBack = scene.add
      .rectangle(barLeft, sizes.hpBarOffsetY, sizes.hpBarWidth, sizes.hpBarHeight, PLACEHOLDER_THEME.hpBarBack)
      .setOrigin(0, 0.5);
    this.hpFill = scene.add
      .rectangle(barLeft, sizes.hpBarOffsetY, sizes.hpBarWidth, sizes.hpBarHeight, this.teamColour)
      .setOrigin(0, 0.5);

    // add() moves the parts from the scene's display list into the container, which draws them in this order.
    this.add([this.bodyImage, letter, hpBack, this.hpFill]);
    // The container itself goes on the scene's display list, on the units layer: above the tiles and highlights.
    this.setDepth(DEPTH.units);
    scene.add.existing(this);

    this.applyState(unit);
  }

  /**
   * Snaps the sprite to the unit's state from the server: its tile and its HP. It first clears whatever an
   * animation may have left behind (a tween still running, a fade, a flash, a raised depth), so the snap always
   * wins: events are for animation, the state is the truth.
   */
  applyState(unit: UnitView): void {
    stopTweensOf(this.scene, [this, this.hpFill]);
    this.endFlash();
    this.lungeStart = null;
    const centre = tileToWorld(this.layout, unit);
    this.setPosition(centre.x, centre.y).setAlpha(1).setScale(1).setDepth(DEPTH.units);
    this.hpFill.setScale(hpFraction(unit.hp, this.unitType.maxHp), 1);
  }

  /**
   * The idle animation, called by GameScene every frame. Only the body breathes: the letter and the HP bar stay
   * steady, and the container's own scale stays free for the tweens (a defeated unit shrinking away).
   */
  breathe(time: number): void {
    this.bodyImage.setScale(breathingScale(time, this.breathPhase));
  }

  /**
   * Walks along `path` (the tiles stepped on, without the start), one tween per tile. A chain plays them one after
   * another, so the unit turns corners instead of cutting through walls in a straight line.
   */
  walk(path: readonly Position[]): Promise<void> {
    if (path.length === 0) {
      return Promise.resolve();
    }
    const { msPerTile, ease } = FEEL.walk;
    return tweenChain(this.scene, {
      targets: this,
      tweens: path.map((tile) => ({ targets: this, ...tileToWorld(this.layout, tile), duration: msPerTile, ease })),
    });
  }

  /** Lunges by `offset` (pixels), toward a target. `lungeBack` brings it back. */
  lungeOut(offset: WorldPoint): Promise<void> {
    this.lungeStart = { x: this.x, y: this.y };
    // One step above the units layer while it lunges, so the attacker overlaps its target instead of slipping under.
    this.setDepth(DEPTH.units + 1);
    const { outMs, outEase } = FEEL.lunge;
    return tween(this.scene, {
      targets: this,
      x: this.x + offset.x,
      y: this.y + offset.y,
      duration: outMs,
      ease: outEase,
    });
  }

  /** Returns to where the lunge started, then back down to the units layer. */
  async lungeBack(): Promise<void> {
    const start = this.lungeStart ?? { x: this.x, y: this.y };
    const { backMs, backEase } = FEEL.lunge;
    await tween(this.scene, { targets: this, x: start.x, y: start.y, duration: backMs, ease: backEase });
    this.setDepth(DEPTH.units);
    this.lungeStart = null;
  }

  /**
   * The hit flash: the body turns solid white for a moment. Tint FILL mode draws the tint colour instead of mixing it
   * with the texture. Phaser 4 has no `setTintFill` any more: it's `setTint` plus `setTintMode`.
   */
  async flash(): Promise<void> {
    this.bodyImage.setTint(HIT_FLASH_COLOUR).setTintMode(Phaser.TintModes.FILL);
    await wait(this.scene, FEEL.hit.flashMs);
    this.endFlash();
  }

  /** Slides the HP bar to `hp`: down after a hit, up after a heal. */
  showHp(hp: number): Promise<void> {
    const { ms, ease } = FEEL.hpBar;
    return tween(this.scene, {
      targets: this.hpFill,
      scaleX: hpFraction(hp, this.unitType.maxHp),
      duration: ms,
      ease,
    });
  }

  /** Fades and shrinks away: defeated. The sprite itself stays until the snap's reconcile destroys it. */
  vanish(): Promise<void> {
    const { ms, endScale, ease } = FEEL.defeat;
    return tween(this.scene, { targets: this, alpha: 0, scale: endScale, duration: ms, ease });
  }

  /** Back to the team colour after a flash: the default MULTIPLY mode, with the colour the constructor set. */
  private endFlash(): void {
    this.bodyImage.setTint(this.teamColour).setTintMode(Phaser.TintModes.MULTIPLY);
  }
}
