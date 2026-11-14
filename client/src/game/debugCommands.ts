import { ApiError, errorMessage } from "../net/ApiClient";
import type { GameAction, GameView, SubmitActionResponse } from "./types";

/** What GameScene lends the console: a way to play an action, and the view on screen. */
export interface DebugTarget {
  submit(action: GameAction): Promise<SubmitActionResponse>;
  currentView(): GameView | null;
}

/** The commands you can type in the browser console: `tactics.units()`, `tactics.move(...)`, and so on. */
export interface DebugCommands {
  /** Prints whose turn it is, and a table of the units: id, tile, HP, what they did this turn. */
  units(): void;
  /** Moves a unit to tile (x, y). */
  move(unitId: string, x: number, y: number): Promise<void>;
  /** Makes a unit attack an enemy or heal an ally. */
  act(actorId: string, targetId: string): Promise<void>;
  /** Ends the current player's turn. */
  endTurn(): Promise<void>;
}

declare global {
  interface Window {
    /** Dev builds only: see `installDebugCommands`. */
    tactics?: DebugCommands;
  }
}

type Log = Pick<Console, "info" | "warn" | "table">;

/**
 * Builds the commands. Each one sends a real action, which the server judges exactly as it will judge a click
 * (Step 7). The console then shows the server's answer: the events, or the refusal with its rule code.
 */
export function createDebugCommands(target: DebugTarget, log: Log = console): DebugCommands {
  const play = async (action: GameAction): Promise<void> => {
    try {
      const { sequence, events } = await target.submit(action);
      log.info(`Action ${sequence} accepted. What happened:`, events);
    } catch (error) {
      const code = error instanceof ApiError && error.code !== undefined ? ` (${error.code})` : "";
      log.warn(`Not played${code}. ${errorMessage(error)}`);
    }
  };

  return {
    units: () => {
      const view = target.currentView();
      if (view === null) {
        log.warn("The game hasn't started yet.");
        return;
      }
      log.info(`Turn ${view.turnNumber}: player ${view.currentPlayer} to move. Tiles are (x, y) from the top-left.`);
      log.table(view.units.map(({ id, x, y, hp, hasMoved, hasActed }) => ({ id, x, y, hp, hasMoved, hasActed })));
    },
    move: (unitId, x, y) => play({ type: "move", unitId, to: { x, y } }),
    act: (actorId, targetId) => play({ type: "act", actorId, targetId }),
    endTurn: () => play({ type: "endTurn" }),
  };
}

/**
 * In dev builds only, puts the commands on `window.tactics`, so you can play from the browser console before Step 7
 * adds clicks. `npm run build` sets `import.meta.env.DEV` to false, so the bundler drops this code from production.
 * Returns a function that removes the commands again.
 */
export function installDebugCommands(target: DebugTarget): () => void {
  if (!import.meta.env.DEV) {
    return () => {};
  }

  window.tactics = createDebugCommands(target);
  console.info(
    "Debug commands: tactics.units() · tactics.move(unitId, x, y) · tactics.act(actorId, targetId) · tactics.endTurn()",
  );
  return () => {
    delete window.tactics;
  };
}
