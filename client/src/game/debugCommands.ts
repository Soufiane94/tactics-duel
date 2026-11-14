import type { PlayOutcome } from "../net/play";
import type { GameAction, GameView } from "./types";

/** What GameScene lends the console: the way clicks play an action, and the view on screen. */
export interface DebugTarget {
  /** Plays an action exactly as a click does: same request, same screen update, same message on the canvas. */
  play(action: GameAction): Promise<PlayOutcome>;
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
 * Builds the commands. Each one plays a real action through the same path as a click, so the server judges it the
 * same way and the canvas shows the result. The console adds the details: the events, or the refusal's rule code.
 * Clicks only send what `availableActions` lists, so the console is the way to watch the server refuse something.
 */
export function createDebugCommands(target: DebugTarget, log: Log = console): DebugCommands {
  const play = async (action: GameAction): Promise<void> => {
    const outcome = await target.play(action);
    switch (outcome.kind) {
      case "accepted":
        log.info(`Action ${outcome.view.sequence} accepted. What happened:`, outcome.events);
        if (outcome.message !== null) {
          log.warn(outcome.message);
        }
        break;
      case "rejected":
        log.warn(`Not played${outcome.code === null ? "" : ` (${outcome.code})`}. ${outcome.message}`);
        break;
      case "resynced":
        log.warn(`Not played (SequenceConflict). ${outcome.message}`);
        break;
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
 * In dev builds only, puts the commands on `window.tactics`, so you can play or test from the browser console.
 * `npm run build` sets `import.meta.env.DEV` to false, so the bundler drops this code from production.
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
