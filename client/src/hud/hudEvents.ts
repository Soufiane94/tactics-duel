import type { EventBus } from "../game/EventBus";
import type { HudModel } from "./hudModel";

/**
 * Every message between GameScene and the HUD: the HUD's whole contract. Each entry is a message name and the
 * arguments its listeners receive. GameScene creates the bus and hands it to HudScene when it launches it, so the
 * two scenes never hold a reference to each other.
 */
export interface HudEvents {
  // ---- GameScene → HUD

  /** Draw this: the turn line, the End Turn button, the selected unit's panel. Sent after every change. */
  show: [model: HudModel];
  /** Show a short message for a few seconds: a refusal, a network error. */
  toast: [text: string];
  /** Play the turn banner, then call `done`. GameScene waits for it before the next event, so input waits too. */
  banner: [text: string, done: () => void];
  /** Play the victory overlay's entrance, then call `done`. The overlay stays on screen afterwards. */
  victory: [text: string, done: () => void];

  // ---- HUD → GameScene

  /**
   * The HUD listens now. It starts a frame after GameScene, and messages sent before anyone listens are lost, so
   * GameScene answers by sending the current model again.
   */
  ready: [];
  /** The End Turn button was pressed. The input state machine decides what it means (nothing, while busy). */
  endTurnPressed: [];
}

/** The bus that carries them. */
export type HudBus = EventBus<HudEvents>;
