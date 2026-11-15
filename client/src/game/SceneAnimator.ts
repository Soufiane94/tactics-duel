import type Phaser from "phaser";
import { DEPTH } from "../config";
import type { HudBus } from "../hud/hudEvents";
import { FLOATING_NUMBER_STYLE } from "../media/placeholder";
import type { Animator } from "./EventPlayer";
import { FEEL } from "./feel";
import type { BoardLayout } from "./grid";
import { bannerText, resultText, type Viewer } from "./turnText";
import type { GameWon, TurnEnded, UnitDamaged, UnitDefeated, UnitHealed, UnitMoved } from "./types";
import { lungeOffset } from "./unitDisplay";
import type { UnitSprite } from "./UnitSprite";

/** What SceneAnimator draws with. GameScene owns all of it; the animator only borrows it. */
export interface Stage {
  readonly scene: Phaser.Scene;
  /** The sprites on screen, by unit id: GameScene's own map, so units created or destroyed later are seen too. */
  readonly sprites: ReadonlyMap<string, UnitSprite>;
  readonly layout: BoardLayout;
  /** To ask the HUD for the turn banner and the victory overlay: they're drawn in screen space, above the board. */
  readonly hud: HudBus;
  /** Whom the banner and the victory text speak to. */
  readonly viewer: Viewer;
}

/**
 * How each event looks, with Phaser. It reads only the event: the server has decided everything already.
 * Inside one event, `await` means "then", and `Promise.all` means "at the same time".
 * A missing sprite means there's nothing to animate: the method returns, and the snap to the view sorts it out.
 */
export class SceneAnimator implements Animator {
  private readonly stage: Stage;

  constructor(stage: Stage) {
    this.stage = stage;
  }

  /** The unit walks its path, tile by tile. */
  async unitMoved(event: UnitMoved): Promise<void> {
    await this.sprites.get(event.unitId)?.walk(event.path);
  }

  /**
   * The attacker lunges, then the hit lands: the target flashes, its HP bar drains, a "−N" rises, the camera
   * shakes, and the attacker steps back. The beat ends when the attacker is back and the bar has drained; the
   * number and the shake are decoration, so the next event doesn't wait for them.
   */
  async unitDamaged(event: UnitDamaged): Promise<void> {
    const target = this.sprites.get(event.unitId);
    if (target === undefined) {
      return;
    }
    const attacker = this.sprites.get(event.byUnitId);
    if (attacker !== undefined) {
      await attacker.lungeOut(lungeOffset(attacker, target, FEEL.lunge.distance * this.stage.layout.tileSize));
    }

    // The moment of impact.
    this.shake();
    this.floatNumber(target, `−${event.amount}`);
    await Promise.all([attacker?.lungeBack(), target.flash(), target.showHp(event.hpAfter)]);
  }

  /** A "+N" rises from the healed unit while its HP bar fills. */
  async unitHealed(event: UnitHealed): Promise<void> {
    const target = this.sprites.get(event.unitId);
    if (target === undefined) {
      return;
    }
    this.floatNumber(target, `+${event.amount}`);
    await target.showHp(event.hpAfter);
  }

  /** The defeated unit fades and shrinks away. The snap then removes its sprite. */
  async unitDefeated(event: UnitDefeated): Promise<void> {
    await this.sprites.get(event.unitId)?.vanish();
  }

  /** The HUD's banner announces who plays now. */
  turnEnded(event: TurnEnded): Promise<void> {
    return this.askHud("banner", bannerText(event.nextPlayer, this.stage.viewer));
  }

  /** The HUD's victory overlay fades in. */
  gameWon(event: GameWon): Promise<void> {
    return this.askHud("victory", resultText(event.winner, this.stage.viewer));
  }

  private get sprites(): ReadonlyMap<string, UnitSprite> {
    return this.stage.sprites;
  }

  /**
   * A number that rises from a unit and fades out. Pure decoration: nothing waits for it, and it destroys itself
   * when its tween ends. It's on the effects layer, above every unit.
   */
  private floatNumber(at: UnitSprite, text: string): void {
    const { scene, layout } = this.stage;
    const { startAbove, rise, ms, fadeDelayMs, ease } = FEEL.floatingNumber;
    const label = scene.add
      .text(at.x, at.y - startAbove * layout.tileSize, text, FLOATING_NUMBER_STYLE)
      .setOrigin(0.5)
      .setDepth(DEPTH.effects);
    scene.tweens.add({
      targets: label,
      y: label.y - rise * layout.tileSize,
      // A property can have its own timing: the fade starts later and ends with the rise.
      alpha: { value: 0, delay: fadeDelayMs, duration: ms - fadeDelayMs },
      duration: ms,
      ease,
      onComplete: () => label.destroy(),
    });
  }

  /**
   * A tiny camera shake. It moves GameScene's camera only: the HUD has its own, so it stays still. A shake started
   * while another runs is ignored (`force` is false), and events play one at a time, so shakes never pile up.
   */
  private shake(): void {
    this.stage.scene.cameras.main.shake(FEEL.hit.shakeMs, FEEL.hit.shakeIntensity);
  }

  /**
   * Asks the HUD to play something, and waits until it calls `done`. If no HUD is listening, `emit` returns false and
   * there's nothing to wait for: a missing HUD must never keep the input locked.
   */
  private askHud(message: "banner" | "victory", text: string): Promise<void> {
    return new Promise((resolve) => {
      const heard = this.stage.hud.emit(message, text, () => resolve());
      if (!heard) {
        resolve();
      }
    });
  }
}
