import Phaser from "phaser";
import { GAME_HEIGHT, GAME_WIDTH } from "../config";
import type { PlayerSide } from "../game/types";
import { applyFeelSpeed } from "../game/tweens";
import { TurnBanner, VictoryOverlay } from "../hud/Banner";
import type { HudBus } from "../hud/hudEvents";
import type { HudModel } from "../hud/hudModel";
import { Toast } from "../hud/Toast";
import { UnitPanel } from "../hud/UnitPanel";
import { DISABLED_ALPHA, HUD_SHAPES, HUD_TEXT, PLACEHOLDER_THEME } from "../media/placeholder";

/** What GameScene hands the HUD when it launches it: the bus, the HUD's only link to the game. */
export interface HudSceneData {
  readonly bus: HudBus;
}

/**
 * Where the HUD's parts sit, measured from the screen's edges. The HUD lives in screen space: it doesn't care where
 * the board is. With the 8×8 board, the side bands (224 px) and the top and bottom bands (64 px) are free.
 */
const LAYOUT = {
  turnLine: { x: GAME_WIDTH / 2, y: 32 },
  unitPanel: { x: 24, y: 96 },
  endTurnButton: { x: GAME_WIDTH - 112, y: GAME_HEIGHT / 2 },
} as const;

/** Draw order inside the HUD: messages stay readable above the banner and the victory overlay. */
const HUD_DEPTH = { panels: 0, banner: 10, overlay: 20, toast: 30 } as const;

/**
 * The heads-up display: the turn line, the selected unit's panel, the End Turn button, messages, the turn banner and
 * the victory overlay. It's a scene of its own, launched by GameScene and running at the same time:
 * - It's drawn on top: scenes draw in the order of the game config's `scene` list, and this one comes last.
 * - It has its own camera, so GameScene's camera shake doesn't move it.
 * - It gets input first. A click on the End Turn button stays here: Phaser hands input to the top scene first, and
 *   a scene that hits one of its interactive objects keeps the click (`globalTopOnly`). The board never sees it.
 * It knows nothing of the game's rules or of GameScene: it draws what the bus brings, and reports the button.
 */
export class HudScene extends Phaser.Scene {
  static readonly KEY = "HudScene";

  private bus: HudBus | null = null;

  constructor() {
    super(HudScene.KEY);
  }

  /** Lifecycle: runs each time the scene starts, with the data `scene.launch` passed. */
  init(data: Partial<HudSceneData>): void {
    this.bus = data.bus ?? null;
  }

  /** Lifecycle: builds the HUD's parts, listens to the bus, then tells GameScene it's ready. */
  create(): void {
    const bus = this.bus;
    if (bus === null) {
      throw new Error("HudScene needs the HUD bus: GameScene launches it with { bus }.");
    }
    applyFeelSpeed(this);

    const turnLine = new TurnLine(this);
    const unitPanel = new UnitPanel(this, LAYOUT.unitPanel.x, LAYOUT.unitPanel.y);
    const endTurnButton = this.createEndTurnButton(bus);
    const banner = new TurnBanner(this, HUD_DEPTH.banner);
    const victory = new VictoryOverlay(this, HUD_DEPTH.overlay);
    const toast = new Toast(this, HUD_DEPTH.toast);

    const stopListening = [
      bus.on("show", (model: HudModel) => {
        turnLine.show(model.turnLine, model.turnSide);
        unitPanel.show(model.unitPanel);
        endTurnButton.setAlpha(model.endTurnEnabled ? 1 : DISABLED_ALPHA);
      }),
      bus.on("toast", (text) => toast.show(text)),
      bus.on("banner", (text, done) => void banner.play(text).then(done)),
      bus.on("victory", (text, done) => void victory.play(text).then(done)),
    ];
    this.events.once(Phaser.Scenes.Events.SHUTDOWN, () => {
      for (const stop of stopListening) {
        stop();
      }
    });

    // GameScene may have sent the model before this scene existed (it starts a frame later): ask for it again.
    bus.emit("ready");
  }

  /** The End Turn button. It only reports the press: the input state machine decides what it means. */
  private createEndTurnButton(bus: HudBus): Phaser.GameObjects.Text {
    const button = this.add
      .text(LAYOUT.endTurnButton.x, LAYOUT.endTurnButton.y, "[ End turn (E) ]", HUD_TEXT.button)
      .setOrigin(0.5)
      .setDepth(HUD_DEPTH.panels)
      // Interactive: Phaser now hit-tests this object under the pointer, and shows the hand cursor over it.
      .setInteractive({ useHandCursor: true });
    button.on(Phaser.Input.Events.GAMEOBJECT_POINTER_DOWN, () => bus.emit("endTurnPressed"));
    return button;
  }
}

/** The line at the top of the screen, with a swatch in the colour of the side it's about. */
class TurnLine {
  private readonly text: Phaser.GameObjects.Text;
  private readonly swatch: Phaser.GameObjects.Rectangle;

  constructor(scene: Phaser.Scene) {
    const { x, y } = LAYOUT.turnLine;
    const size = HUD_SHAPES.swatchSize;
    this.text = scene.add.text(x, y, "", HUD_TEXT.turnLine).setOrigin(0.5).setDepth(HUD_DEPTH.panels);
    this.swatch = scene.add.rectangle(x, y, size, size, 0xffffff).setDepth(HUD_DEPTH.panels).setVisible(false);
  }

  show(line: string, side: PlayerSide | null): void {
    this.text.setText(line);
    if (side === null) {
      this.swatch.setVisible(false);
      return;
    }
    // The text is centred, so its left edge moves with its width: the swatch follows it, 10 pixels to its left.
    const left = this.text.x - this.text.width / 2;
    this.swatch
      .setFillStyle(PLACEHOLDER_THEME.team[side])
      .setX(left - 10 - HUD_SHAPES.swatchSize / 2)
      .setVisible(true);
  }
}
