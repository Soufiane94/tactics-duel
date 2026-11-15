import { acceptsInput, selectionOf, type InputState } from "../game/InputController";
import { turnLine, type Viewer } from "../game/turnText";
import type { Ability, GameView, PlayerSide } from "../game/types";
import { findUnit, typeOf } from "../game/viewQueries";

/*
 * What the HUD shows, computed from the view on screen and the input state: "render from state" for the HUD, as
 * `highlightsFor` is for the board. HudScene only draws the model it receives. No Phaser import, so Vitest tests it.
 */

/** Everything the HUD shows that follows the state. Messages, the turn banner and the victory overlay are separate. */
export interface HudModel {
  /** "Turn 3 · Player Two to play", "Turn 3 · Your turn", or the result: "Player One wins!". */
  readonly turnLine: string;
  /** The side the turn line is about, for its colour swatch: the side to play, or the winner. Null: no winner. */
  readonly turnSide: PlayerSide | null;
  /** Whether the End Turn button looks usable: your turn, and no answer or animation awaited. */
  readonly endTurnEnabled: boolean;
  /** The selected unit's panel, or null to hide the panel. */
  readonly unitPanel: UnitPanelModel | null;
}

/** One unit's card: what it is, whose it is, and its stats, ready to draw. */
export interface UnitPanelModel {
  /** The unit type's name: "Archer". */
  readonly title: string;
  /** Whose unit it is, for the panel's colour swatch. */
  readonly owner: PlayerSide;
  /** One line per stat: "HP 12 / 18", "Power 8", … */
  readonly lines: readonly string[];
}

/**
 * The HUD for a view and an input state. The panel follows the selection, including the unit whose action is
 * playing (busy, about to be selected again), so the panel doesn't blink on every action. `viewer` picks the words
 * (`Viewer`, in `game/turnText.ts`).
 */
export function hudModelFor(view: GameView, input: InputState, viewer: Viewer, hoveredUnitId: string | null = null): HudModel {
  const panelUnitId = selectionOf(input) ?? hoveredUnitId;
  return {
    turnLine: turnLine(view, viewer),
    turnSide: view.status === "finished" ? view.winner : view.currentPlayer,
    endTurnEnabled: acceptsInput(input),
    unitPanel: panelUnitId === null ? null : unitPanelFor(view, panelUnitId),
  };
}

/**
 * The panel of any unit on the board, or null if it isn't on the board (any more). Its stats come from
 * `view.unitTypes`, this game's own setup snapshot; its HP from the unit itself.
 */
export function unitPanelFor(view: GameView, unitId: string): UnitPanelModel | null {
  const unit = findUnit(view, unitId);
  if (unit === undefined) {
    return null;
  }
  const type = typeOf(view, unit.typeId);
  return {
    title: type.name,
    owner: unit.owner,
    lines: [
      `HP ${unit.hp} / ${type.maxHp}`,
      `Power ${type.power}`,
      `Defense ${type.defense}`,
      `Move ${type.move}`,
      `Range ${rangeText(type.minRange, type.maxRange)}`,
      abilityText(type.ability),
    ],
  };
}

/** "2–3", or a single number when the range band is one distance wide: the warrior's "1". */
function rangeText(min: number, max: number): string {
  return min === max ? `${min}` : `${min}–${max}`;
}

function abilityText(ability: Ability): string {
  return ability === "heal" ? "Heals allies" : "Damages enemies";
}
