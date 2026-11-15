import Phaser from "phaser";
import { DEPTH, GAME_HEIGHT, GAME_WIDTH } from "../config";
import { BoardRenderer } from "../game/BoardRenderer";
import { installDebugCommands } from "../game/debugCommands";
import { EventBus } from "../game/EventBus";
import { EventPlayer } from "../game/EventPlayer";
import { worldToTile } from "../game/grid";
import { highlightsFor } from "../game/highlights";
import { acceptsInput, transition, type InputEvent, type InputState } from "../game/InputController";
import { planReconcile } from "../game/reconcile";
import { SceneAnimator } from "../game/SceneAnimator";
import type { Viewer } from "../game/turnText";
import { applyFeelSpeed } from "../game/tweens";
import type { GameAction, GameView, Position } from "../game/types";
import { UnitSprite } from "../game/UnitSprite";
import { typeOf, unitAt } from "../game/viewQueries";
import type { HudBus, HudEvents } from "../hud/hudEvents";
import { hudModelFor } from "../hud/hudModel";
import { ApiClient, errorMessage } from "../net/ApiClient";
import { playAction, type PlayOutcome } from "../net/play";
import { startLocalGame, viewerOf, type GameSession } from "../net/session";
import { HudScene, type HudSceneData } from "./HudScene";

/** The input state while the scene waits for the server's first view: busy, with nothing to reselect. */
const WAITING_FOR_SERVER: InputState = { kind: "busy", reselect: null };

/** One action at a time: a second one, sent before the first one is on screen, is refused with this. */
const WAIT_FOR_ANSWER: PlayOutcome = {
  kind: "rejected",
  message: "Wait for the previous action to finish.",
  code: null,
};

/**
 * The board, its units and the player's input, all drawn from the server's view of the game. It decides no rule:
 * clicks and keys go through the input state machine (`game/InputController.ts`), which picks actions from
 * `view.availableActions`, and the server judges every action. Each answer's events play as animations
 * (`game/EventPlayer.ts`), then the screen snaps to the answer's view. The HUD runs in its own scene, on top.
 */
export class GameScene extends Phaser.Scene {
  static readonly KEY = "GameScene";

  private readonly api = new ApiClient();
  private session: GameSession | null = null;
  /** Whom the texts speak to: null in pass & play, so they name the sides ("Player Two to play"). */
  private viewer: Viewer = null;
  private view: GameView | null = null;
  private board: BoardRenderer | null = null;
  /** The HUD's channel (`hud/hudEvents.ts`). A new one per game, so a stopped game's listeners never hear the next. */
  private hud: HudBus = new EventBus<HudEvents>();
  /** Plays each answer's events before the snap to its view. Made with the board; one per game. */
  private player: EventPlayer | null = null;
  /** Where the player's input stands. Not named `input`: that's Phaser's input plugin, `this.input`. */
  private inputState: InputState = WAITING_FOR_SERVER;
  /** The tile under the pointer, or null off the board. Kept, so the hover marker comes back when input unlocks. */
  private hoveredTile: Position | null = null;
  /** True from sending an action until its answer is on screen, animation included, whoever sent it. */
  private playing = false;
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
    this.viewer = null;
    this.view = null;
    this.board = null;
    this.hud = new EventBus<HudEvents>();
    this.player = null;
    this.inputState = WAITING_FOR_SERVER;
    this.hoveredTile = null;
    this.playing = false;
    this.sprites.clear();
  }

  /** Lifecycle: runs once, after `init`. Starts the HUD, then a local game on the server. */
  create(): void {
    this.running = true;
    applyFeelSpeed(this);
    this.startHud();

    const status = this.add
      .text(GAME_WIDTH / 2, GAME_HEIGHT / 2, "Starting a local game...", { fontSize: "24px", align: "center" })
      .setOrigin(0.5)
      .setDepth(DEPTH.effects);

    let removeDebugCommands = (): void => {};
    this.events.once(Phaser.Scenes.Events.SHUTDOWN, () => {
      this.running = false;
      removeDebugCommands();
      // The HUD lives and dies with the game.
      this.hud.clear();
      this.scene.stop(HudScene.KEY);
    });

    startLocalGame(this.api)
      .then(({ session, view }) => {
        if (!this.running) {
          return;
        }
        this.session = session;
        this.viewer = viewerOf(session);
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
   *   Tweens do that for us.
   */
  update(time: number, _delta: number): void {
    for (const sprite of this.sprites.values()) {
      sprite.breathe(time);
    }
  }

  /**
   * Launches the HUD: a second scene that runs at the same time, on top of this one. The bus is its only link to
   * this scene. The HUD starts a frame later, and says `ready` when it listens: then it gets the current model.
   */
  private startHud(): void {
    this.hud.on("ready", () => this.renderHud());
    this.hud.on("endTurnPressed", () => this.handle({ type: "endTurnPressed" }));
    const data: HudSceneData = { bus: this.hud };
    this.scene.launch(HudScene.KEY, data);
  }

  /**
   * Plugs the mouse and the keyboard into the state machine. A canvas is a single DOM element, with no buttons or
   * tiles inside for the browser to find under the pointer. Phaser listens to the canvas's own events, converts their
   * coordinates to game pixels, and re-emits them here; `worldToTile` then finds the tile. Phaser removes these
   * listeners itself when the scene shuts down. A click on the HUD's button never gets here: the HUD keeps it.
   */
  private listenToPlayer(): void {
    this.input.mouse?.disableContextMenu();
    this.input.on(Phaser.Input.Events.POINTER_DOWN, (pointer: Phaser.Input.Pointer) => {
      if (pointer.rightButtonDown()) {
        this.handle({ type: "cancelled" });
        return;
      }
      const tile = this.tileUnder(pointer);
      this.handle(tile === null ? { type: "cancelled" } : { type: "tileClicked", tile });
    });
    this.input.on(Phaser.Input.Events.POINTER_MOVE, (pointer: Phaser.Input.Pointer) => {
      this.hoveredTile = this.tileUnder(pointer);
      this.showHover();
      this.renderHud();
    });
    // The pointer left the canvas, so no tile is under it any more.
    this.input.on(Phaser.Input.Events.GAME_OUT, () => {
      this.hoveredTile = null;
      this.showHover();
      this.renderHud();
    });

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
   * camera stays still; a hit's camera shake moves it.
   */
  private tileUnder(pointer: Phaser.Input.Pointer): Position | null {
    return this.board === null ? null : worldToTile(this.board.layout, pointer.worldX, pointer.worldY);
  }

  /** The hover marker only shows while a click can do something: not while busy, nor during the opponent's turn. */
  private showHover(): void {
    this.board?.showHover(acceptsInput(this.inputState) ? this.hoveredTile : null);
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

  /** "Render from state" for the input: the highlights, the hover marker and the HUD follow it. */
  private renderInput(): void {
    if (this.view === null || this.board === null) {
      return;
    }
    this.board.showHighlights(highlightsFor(this.inputState, this.view));
    this.showHover();
    this.renderHud();
  }

  /** Sends the HUD a fresh model of what to show. The HUD draws only that: render from state, again. */
  private renderHud(): void {
    if (this.view !== null) {
      const hovered = this.hoveredTile === null ? undefined : unitAt(this.view!, this.hoveredTile);
      this.hud.emit("show", hudModelFor(this.view, this.inputState, this.viewer, hovered?.id ?? null));
    }
  }

  /**
   * Plays one action, whoever asked: a click (through `handle`) or the console. It sends the action, shows the
   * outcome (the answer's animation, then the snap to its view), and only then tells the state machine how the
   * request ended. The input stays busy all along, so clicks and keys during an animation do nothing.
   */
  private async play(action: GameAction): Promise<PlayOutcome> {
    if (this.playing) {
      // A click while the console's action plays: the state machine doesn't know about that one. The click is
      // refused, and showing the refusal ends its wait.
      await this.showOutcome(WAIT_FOR_ANSWER);
      return WAIT_FOR_ANSWER;
    }

    this.playing = true;
    try {
      const outcome = await this.send(action);
      if (this.running) {
        await this.showOutcome(outcome);
      }
      return outcome;
    } finally {
      this.playing = false;
    }
  }

  /**
   * The console's way in. While another action plays (on its way, or animating), it's refused here, before `play`:
   * the state machine may be waiting for that other action, and a `requestFailed` would end its wait too early.
   */
  private playFromConsole(action: GameAction): Promise<PlayOutcome> {
    return this.playing ? Promise.resolve(WAIT_FOR_ANSWER) : this.play(action);
  }

  private send(action: GameAction): Promise<PlayOutcome> {
    if (this.session === null || this.view === null) {
      return Promise.resolve({ kind: "rejected", message: "The game hasn't started yet.", code: null });
    }
    return playAction(this.api, this.session, this.view, action);
  }

  /**
   * Shows how an action ended. Accepted: first its events, as animations ("what happened"), then the snap to its
   * view ("where the game is now": the truth). Only then does the state machine hear `viewReceived`, which ends the
   * busy state. A fresh view after a conflict comes without events, so it snaps at once. A refusal changes nothing.
   */
  private async showOutcome(outcome: PlayOutcome): Promise<void> {
    if (outcome.kind === "accepted") {
      await this.player?.play(outcome.events);
      if (!this.running) {
        return;
      }
    }

    if (outcome.kind === "rejected") {
      this.handle({ type: "requestFailed" });
    } else {
      this.renderView(outcome.view);
      this.handle({ type: "viewReceived" });
    }
    if (outcome.message !== null) {
      this.hud.emit("toast", outcome.message);
    }
  }

  /**
   * Makes the screen match a view: "render from state". The board and the event player are created with the first
   * view, which brings the board's size. Then the units are reconciled by id: new ones get a sprite, known ones are
   * snapped to their state, and the sprites of units that left the board are destroyed. Drawing the same view twice
   * changes nothing, and any view can be drawn from scratch.
   */
  private renderView(view: GameView): void {
    this.view = view;
    const board = (this.board ??= new BoardRenderer(this, view.board));
    this.player ??= new EventPlayer(
      new SceneAnimator({ scene: this, sprites: this.sprites, layout: board.layout, hud: this.hud, viewer: this.viewer }),
    );

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
