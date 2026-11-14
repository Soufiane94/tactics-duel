import type { GameView, Position, UnitActionsView, UnitTypeView, UnitView } from "./types";

/*
 * Questions the client asks a view: which unit stands on a tile, what a unit may do. They're lookups, not rules:
 * the answers are already in the server's view, like Core's `GameState.UnitAt` and `FindUnit`. No Phaser import.
 */

/** The unit standing on a tile, if any. */
export function unitAt(view: GameView, tile: Position): UnitView | undefined {
  return view.units.find((unit) => unit.x === tile.x && unit.y === tile.y);
}

/** A unit by id, if it's still on the board. */
export function findUnit(view: GameView, unitId: string): UnitView | undefined {
  return view.units.find((unit) => unit.id === unitId);
}

/**
 * What a unit may still do this turn, straight from `availableActions`. Undefined means "nothing": it isn't your unit,
 * it isn't your turn, or the unit has nothing left to do (the server lists only units with a move or a target).
 */
export function optionsOf(view: GameView, unitId: string): UnitActionsView | undefined {
  return view.availableActions.find((options) => options.unitId === unitId);
}

/** A unit type's stats, from this game's own setup snapshot (`view.unitTypes`). */
export function typeOf(view: GameView, typeId: string): UnitTypeView {
  const unitType = view.unitTypes.find((candidate) => candidate.id === typeId);
  if (unitType === undefined) {
    // The server validates content at startup, so this means client and server disagree: a bug, not bad data.
    throw new Error(`The view has no unit type "${typeId}".`);
  }
  return unitType;
}
