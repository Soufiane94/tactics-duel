import Phaser from "phaser";
import { DEPTH, GAME_HEIGHT, GAME_WIDTH } from "../config";
import { CONTROLS_TEXT_STYLE, DISABLED_ALPHA } from "../media/placeholder";
import type { BoardLayout } from "./grid";
import { statusLine } from "./turnText";
import type { GameView } from "./types";

/** How long a message stays on screen. *Placeholder* feel: Step 8 gathers such numbers in `feel.ts`. */
const MESSAGE_DURATION_MS = 4000;

/**
 * Temporary on-canvas controls, until Step 8 builds the HUD in HudScene:
 * - a status line above the board: whose turn it is, or who won;
 * - an End Turn button to the right of the board;
 * - a message line below the board, for refusals and errors.
 * They're placed in the bands around the board, so they follow its layout. They show what they're given and decide
 * nothing: the button only reports the press, and the state machine decides what it means.
 */
export class TurnControls {
  private readonly scene: Phaser.Scene;
  private readonly status: Phaser.GameObjects.Text;
  private readonly endTurnButton: Phaser.GameObjects.Text;
  private readonly message: Phaser.GameObjects.Text;
  private messageTimer: Phaser.Time.TimerEvent | null = null;

  constructor(scene: Phaser.Scene, layout: BoardLayout, onEndTurn: () => void) {
    this.scene = scene;
    const boardRight = layout.originX + layout.width * layout.tileSize;
    const boardBottom = layout.originY + layout.height * layout.tileSize;

    this.status = scene.add
      .text(GAME_WIDTH / 2, layout.originY / 2, "", CONTROLS_TEXT_STYLE)
      .setOrigin(0.5)
      .setDepth(DEPTH.effects);

    this.endTurnButton = scene.add
      .text((boardRight + GAME_WIDTH) / 2, GAME_HEIGHT / 2, "[ End turn (E) ]", CONTROLS_TEXT_STYLE)
      .setOrigin(0.5)
      .setDepth(DEPTH.effects)
      // Interactive: Phaser now hit-tests this object under the pointer, and shows the hand cursor over it.
      .setInteractive({ useHandCursor: true });
    this.endTurnButton.on(
      Phaser.Input.Events.GAMEOBJECT_POINTER_DOWN,
      (_pointer: Phaser.Input.Pointer, _x: number, _y: number, event: Phaser.Types.Input.EventData) => {
        // Phaser reports a click on an object first, then to the whole scene. Stop it here, or GameScene would also
        // read this click outside the board as "deselect".
        event.stopPropagation();
        onEndTurn();
      },
    );

    this.message = scene.add
      .text(GAME_WIDTH / 2, (boardBottom + GAME_HEIGHT) / 2, "", {
        ...CONTROLS_TEXT_STYLE,
        align: "center",
        wordWrap: { width: GAME_WIDTH - 40 },
      })
      .setOrigin(0.5)
      .setDepth(DEPTH.effects);
  }

  /** Shows whose turn it is, or who won. */
  setStatus(view: GameView): void {
    this.status.setText(statusLine(view));
  }

  /** Makes the button look usable or not. A press is reported either way, and the state machine ignores it if busy. */
  setEndTurnEnabled(enabled: boolean): void {
    this.endTurnButton.setAlpha(enabled ? 1 : DISABLED_ALPHA);
  }

  /** Shows a message below the board for a few seconds. A newer message replaces it and restarts the countdown. */
  showMessage(text: string): void {
    this.message.setText(text);
    this.messageTimer?.remove();
    // The scene's clock, not `setTimeout`: it pauses with the scene, and its timers are destroyed when the scene stops.
    this.messageTimer = this.scene.time.delayedCall(MESSAGE_DURATION_MS, () => this.message.setText(""));
  }
}
