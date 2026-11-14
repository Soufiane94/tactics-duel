import Phaser from "phaser";
import { DEPTH, GAME_HEIGHT, GAME_WIDTH } from "../config";
import { BoardRenderer } from "../game/BoardRenderer";
import { installDebugCommands } from "../game/debugCommands";
import { planReconcile } from "../game/reconcile";
import type { GameAction, GameView, SubmitActionResponse, UnitTypeView } from "../game/types";
import { UnitSprite } from "../game/UnitSprite";
import { ApiClient, errorMessage } from "../net/ApiClient";
import { startLocalGame, type GameSession } from "../net/session";
import { HudScene } from "./HudScene";

/**
 * The board and its units, drawn from the server's view of the game. It decides no rule: it shows `GameView`s, and
 * sends actions for the server to judge (from the dev console for now, from clicks in Step 7).
 */
export class GameScene extends Phaser.Scene {
  static readonly KEY = "GameScene";

  private readonly api = new ApiClient();
  private session: GameSession | null = null;
  private view: GameView | null = null;
  private board: BoardRenderer | null = null;
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
        removeDebugCommands = installDebugCommands({
          submit: (action) => this.submit(action),
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
   * Makes the screen match a view: "render from state". The board is drawn once, then the units are reconciled by
   * id: new ones get a sprite, known ones are updated, and the sprites of units that left the board are destroyed.
   * Drawing the same view twice changes nothing, and any view can be drawn from scratch.
   */
  private renderView(view: GameView): void {
    this.view = view;
    const board = (this.board ??= new BoardRenderer(this, view.board));

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

  /**
   * Plays one action: posts it with the current player's token and the sequence of the view on screen, then draws the
   * view the server sends back. The server is the referee: a refused action rejects with an `ApiError` (a 422 and its
   * rule code), and the screen stays as it was. Step 7 calls this from clicks; for now, the dev console does.
   */
  private async submit(action: GameAction): Promise<SubmitActionResponse> {
    if (this.session === null || this.view === null) {
      throw new Error("The game hasn't started yet.");
    }

    const token = this.session.tokenFor(this.view.currentPlayer);
    const response = await this.api.submitAction(this.session.gameId, token, {
      expectedSequence: this.view.sequence,
      action,
    });
    if (this.running) {
      this.renderView(response.view);
    }
    return response;
  }
}

/** A unit's stats, from this game's own setup snapshot (`view.unitTypes`). */
function typeOf(view: GameView, typeId: string): UnitTypeView {
  const unitType = view.unitTypes.find((candidate) => candidate.id === typeId);
  if (unitType === undefined) {
    // The server validates content at startup, so this means client and server disagree: a bug, not bad data.
    throw new Error(`The view has no unit type "${typeId}".`);
  }
  return unitType;
}
