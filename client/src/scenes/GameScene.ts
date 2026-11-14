import Phaser from "phaser";
import { DEPTH, GAME_HEIGHT, GAME_WIDTH } from "../config";
import { BoardRenderer } from "../game/BoardRenderer";
import { installDebugCommands } from "../game/debugCommands";
import { worldToTile } from "../game/grid";
import { highlightsFor } from "../game/highlights";
import { acceptsInput, transition, type InputEvent, type InputState } from "../game/InputController";
import { planReconcile } from "../game/reconcile";
import { TurnControls } from "../game/TurnControls";
import type { GameAction, GameView, Position } from "../game/types";
import { UnitSprite } from "../game/UnitSprite";
import { typeOf } from "../game/viewQueries";
import { ApiClient, errorMessage } from "../net/ApiClient";
import { playAction, type PlayOutcome } from "../net/play";
import { startLocalGame, type GameSession } from "../net/session";
import { HudScene } from "./HudScene";

/** The input state while the scene waits for the server's first view: busy, with nothing to reselect. */
const WAITING_FOR_SERVER: InputState = { kind: "busy", reselect: null };

/** One action at a time: a second one, sent before the first one's answer, is refused with this. */
const WAIT_FOR_ANSWER: PlayOutcome = {
  kind: "rejected",
  message: "Wait for the previous action to finish.",
  code: null,
};

/**
 * The board, its units and the player's input, all drawn from the server's view of the game. It decides no rule:
 * clicks and keys go through the input state machine (`game/InputController.ts`), which picks actions from
 * `view.availableActions`, and the server judges every action.
 */
export class GameScene extends Phaser.Scene {
  static readonly KEY = "GameScene";

  private readonly api = new ApiClient();
  private session: GameSession | null = null;
  private view: GameView | null = null;
  private board: BoardRenderer | null = null;
  private controls: TurnControls | null = null;
  /** Where the player's input stands. Not named `input`: that's Phaser's input plugin, `this.input`. */
  private inputState: InputState = WAITING_FOR_SERVER;
  /** True while an action is on its way, whoever sent it: a click or the console. */
  private sending = false;
  /** One sprite per unit on the board, by unit id: the "trackBy" key. */
  private readonly sprites = new Map<string, UnitSprite>();
  /** False once the scene shuts down, so a late answer from the server doesn't draw into a dead scene. */
  private running = false;

  constructor() {
    super(GameScene.KEY);
  }

  /** Lifecycle: runs each time the scene starts (the constructor runs only once), so per-game state resets here. */
  init(): void {
    this.session = null;
    this.view = null;
    this.board = null;
    this.controls = null;
    this.inputState = WAITING_FOR_SERVER;
    this.sending = false;
    this.sprites.clear();
  }

  /** Lifecycle: runs once, after `init`. Starts the HUD, then a local game on the server. */
  create(): void {
    this.running = true;
    // The HUD is a second scene that runs at the same time and draws on top (Step 8 fills it).
    this.scene.launch(HudScene.KEY);

    const status = this.add
      .text(GAME_WIDTH / 2, GAME_HEIGHT / 2, "Starting a local game...", { fontSize: "24px", align: "center" })
      .setOrigin(0.5)
      .setDepth(DEPTH.effects);

    let removeDebugCommands = (): void => {};
    this.events.once(Phaser.Scenes.Events.SHUTDOWN, () => {
      this.running = false;
      removeDebugCommands();
    });

    startLocalGame(this.api)
      .then(({ session, view }) => {
        if (!this.running) {
          return;
        }
        this.session = session;
        status.destroy();
        this.renderView(view);
        this.handle({ type: "viewReceived" });
        this.listenToPlayer();
        removeDebugCommands = installDebugCommands({
          play: (action) => this.playFromConsole(action),
          currentView: () => this.view,
        });
      })
      .catch((error: unknown) => {
        if (this.running) {
          status.setText(`Couldn't start a local game.\n${errorMessage(error)}`);
        }
      });
  }

  /**
   * Lifecycle: Phaser calls this once per frame (about 60 times a second), before it draws the frame.
   * - `time`: milliseconds since the game started. A value computed *from* it, like the breathing, looks the same at
   *   any frame rate.
   * - `delta`: milliseconds since the previous frame (about 16.7 at 60 FPS, 6.9 at 144 FPS). Anything that *adds up*
   *   frame after frame must be scaled by it (`x += speed * delta / 1000`), or it runs faster on faster screens.
   */
  update(time: number, _delta: number): void {
    for (const sprite of this.sprites.values()) {
      sprite.breathe(time);
    }
  }

  /**
   * Plugs the mouse and the keyboard into the state machine. A canvas is a single DOM element, with no buttons or
   * tiles inside for the browser to find under the pointer. Phaser listens to the canvas's own events, converts their
   * coordinates to game pixels, and re-emits them here; `worldToTile` then finds the tile. Phaser removes these
   * listeners itself when the scene shuts down.
   */
  private listenToPlayer(): void {
    this.input.mouse?.disableContextMenu();
    this.input.on(Phaser.Input.Events.POINTER_DOWN, (pointer: Phaser.Input.Pointer) => {

      if (pointer.rightButtonDown()) { this.handle({ type: "cancelled" }); return; }
      const tile = this.tileUnder(pointer);
      this.handle(tile === null ? { type: "cancelled" } : { type: "tileClicked", tile });
    });
    this.input.on(Phaser.Input.Events.POINTER_MOVE, (pointer: Phaser.Input.Pointer) => {
      this.showHover(this.tileUnder(pointer));
    });
    // The pointer left the canvas, so no tile is under it any more.
    this.input.on(Phaser.Input.Events.GAME_OUT, () => this.showHover(null));

    // Null if the game config turns the keyboard off. Ours doesn't, but the type has to allow it.
    const keyboard = this.input.keyboard;
    keyboard?.on("keydown-ESC", () => this.handle({ type: "cancelled" }));
    keyboard?.on("keydown-E", (event: KeyboardEvent) => {
      // Holding a key makes the browser repeat keydown. Without this check, holding E would also end the next
      // player's turn as soon as their view arrives.
      if (!event.repeat) {
        this.handle({ type: "endTurnPressed" });
      }
    });
  }

  /**
   * The tile under the pointer, or null off the board. `worldX`/`worldY` are the pointer's position in world space:
   * through the camera, the space the board is drawn in. They match the canvas position (`x`/`y`) only while the
   * camera hasn't moved; Step 8's camera shake will move it.
   */
  private tileUnder(pointer: Phaser.Input.Pointer): Position | null {
    return this.board === null ? null : worldToTile(this.board.layout, pointer.worldX, pointer.worldY);
  }

  /** The hover marker only shows while a click can do something: not while busy, nor during the opponent's turn. */
  private showHover(tile: Position | null): void {
    this.board?.showHover(acceptsInput(this.inputState) ? tile : null);
  }

  /**
   * Feeds one event to the state machine, then does what it says: draw the new state, and send its action if there
   * is one. It's the only place where `inputState` changes.
   */
  private handle(event: InputEvent): void {
    if (this.view === null) {
      return;
    }
    const { state, request } = transition(this.inputState, event, this.view);
    this.inputState = state;
    this.renderInput();
    if (request !== null) {
      void this.play(request);
    }
  }

  /** "Render from state" for the input: highlights, the End Turn button's look and the hover marker follow it. */
  private renderInput(): void {
    if (this.view === null || this.board === null || this.controls === null) {
      return;
    }
    this.board.showHighlights(highlightsFor(this.inputState, this.view));
    this.controls.setEndTurnEnabled(acceptsInput(this.inputState));
    if (!acceptsInput(this.inputState)) {
      this.board.showHover(null);
    }
  }

  /**
   * Plays one action, whoever asked: a click (through `handle`) or the console. It sends the action, shows the
   * outcome, and tells the state machine how the request ended, so a click's busy state always ends.
   */
  private async play(action: GameAction): Promise<PlayOutcome> {
    const outcome = await this.send(action);
    if (this.running) {
      this.showOutcome(outcome);
    }
    return outcome;
  }

  /**
   * The console's way in. While another action is on its way, it's refused here, before `play`: the state machine may
   * be waiting for that other action's answer, and a `requestFailed` would end its wait too early.
   */
  private playFromConsole(action: GameAction): Promise<PlayOutcome> {
    return this.sending ? Promise.resolve(WAIT_FOR_ANSWER) : this.play(action);
  }

  private async send(action: GameAction): Promise<PlayOutcome> {
    if (this.session === null || this.view === null) {
      return { kind: "rejected", message: "The game hasn't started yet.", code: null };
    }
    if (this.sending) {
      // A click while the console's action is on its way: the state machine doesn't know about the console's.
      return WAIT_FOR_ANSWER;
    }

    this.sending = true;
    try {
      return await playAction(this.api, this.session, this.view, action);
    } finally {
      this.sending = false;
    }
  }

  private showOutcome(outcome: PlayOutcome): void {
    if (outcome.kind === "rejected") {
      this.handle({ type: "requestFailed" });
    } else {
      // Accepted, or a fresh copy after a conflict: no animation yet (Step 8), the screen snaps to the new view.
      this.renderView(outcome.view);
      this.handle({ type: "viewReceived" });
    }
    if (outcome.message !== null) {
      this.controls?.showMessage(outcome.message);
    }
  }

  /**
   * Makes the screen match a view: "render from state". The board and the turn controls are created once, then the
   * units are reconciled by id: new ones get a sprite, known ones are updated, and the sprites of units that left the
   * board are destroyed. Drawing the same view twice changes nothing, and any view can be drawn from scratch.
   */
  private renderView(view: GameView): void {
    this.view = view;
    const board = (this.board ??= new BoardRenderer(this, view.board));
    const controls = (this.controls ??= new TurnControls(this, board.layout, () =>
      this.handle({ type: "endTurnPressed" }),
    ));
    controls.setStatus(view);

    const plan = planReconcile(this.sprites.keys(), view.units);
    for (const id of plan.destroy) {
      this.sprites.get(id)?.destroy();
      this.sprites.delete(id);
    }
    for (const unit of plan.update) {
      this.sprites.get(unit.id)?.applyState(unit);
    }
    for (const unit of plan.create) {
      this.sprites.set(unit.id, new UnitSprite(this, board.layout, unit, typeOf(view, unit.typeId)));
    }
  }
}
