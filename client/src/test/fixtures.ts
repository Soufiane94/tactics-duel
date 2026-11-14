/*
 * Fake game data for tests: complete objects built from a few overrides, so each test spells out only what it checks.
 * Test code only. Vitest runs `*.test.ts` files, so it doesn't run this one, and the game never imports it, so the
 * production bundle doesn't contain it.
 */
import type { GameView, PlayerSide, Position, UnitActionsView, UnitTypeView, UnitView } from "../game/types";

export const GAME_ID = "0b8f6e2a-61c4-7d0e-9a35-2f1c8d4e5b6a";

/** The four unit types of PLAN.md, section 1. */
export const UNIT_TYPES: UnitTypeView[] = [
  { id: "warrior", name: "Warrior", maxHp: 28, power: 10, defense: 3, move: 3, minRange: 1, maxRange: 1, ability: "damage" },
  { id: "archer", name: "Archer", maxHp: 18, power: 8, defense: 1, move: 3, minRange: 2, maxRange: 3, ability: "damage" },
  { id: "mage", name: "Mage", maxHp: 14, power: 12, defense: 0, move: 2, minRange: 1, maxRange: 2, ability: "damage" },
  { id: "healer", name: "Healer", maxHp: 16, power: 5, defense: 1, move: 3, minRange: 1, maxRange: 2, ability: "heal" },
];

/**
 * A unit on tile (x, y). Its owner and type come from its id, as in the scenario files: "one-archer" is Player One's
 * archer. It starts at full HP, with nothing done this turn.
 */
export function unit(id: string, x: number, y: number, changes: Partial<UnitView> = {}): UnitView {
  const [owner, typeId] = id.split("-");
  if (!isSide(owner) || typeId === undefined) {
    throw new Error(`Test unit ids look like "one-archer", not "${id}".`);
  }
  const maxHp = UNIT_TYPES.find((type) => type.id === typeId)?.maxHp ?? 10;
  return { id, typeId, owner, x, y, hp: maxHp, hasMoved: false, hasActed: false, ...changes };
}

/** What one unit may do, as the server lists it in `availableActions`. */
export function options(unitId: string, moveTargets: Position[] = [], actTargets: string[] = []): UnitActionsView {
  return { unitId, moveTargets, actTargets };
}

/** A view of a game in progress on an empty 8×8 board: turn 1, Player One to move, seen by Player One. */
export function view(changes: Partial<GameView> = {}): GameView {
  return {
    gameId: GAME_ID,
    sequence: 0,
    yourSide: "one",
    currentPlayer: "one",
    turnNumber: 1,
    status: "inProgress",
    winner: null,
    opponentJoined: true,
    board: { width: 8, height: 8, obstacles: [] },
    unitTypes: UNIT_TYPES,
    units: [],
    availableActions: [],
    ...changes,
  };
}

function isSide(value: string | undefined): value is PlayerSide {
  return value === "one" || value === "two";
}
