import type { InputState } from "./InputController";
import type { GameView, Position } from "./types";
import { findUnit, optionsOf } from "./viewQueries";

/** What a lit tile means. Each kind has its own look, in `media/placeholder.ts`. */
export type HighlightKind = "selected" | "move" | "act";

export interface Highlight {
  readonly tile: Position;
  readonly kind: HighlightKind;
}

/**
 * The tiles to light up in a state: the selected unit's tile, the tiles it can move to, and the tiles of the units it
 * can act on. All of it is read from the server's `availableActions`: the client works out no range and no path, so
 * a highlighted tile is always an action the server accepts. Nothing is lit without a selection, or while busy.
 * Pure, so Vitest tests it; `BoardRenderer.showHighlights` draws the result.
 */
export function highlightsFor(state: InputState, view: GameView): Highlight[] {
  if (state.kind !== "unitSelected") {
    return [];
  }
  const selected = findUnit(view, state.unitId);
  const options = optionsOf(view, state.unitId);
  if (selected === undefined || options === undefined) {
    return [];
  }

  const targets = options.actTargets
    .map((id) => findUnit(view, id))
    .filter((target) => target !== undefined);

  return [
    { tile: { x: selected.x, y: selected.y }, kind: "selected" },
    ...options.moveTargets.map((tile): Highlight => ({ tile, kind: "move" })),
    ...targets.map((target): Highlight => ({ tile: { x: target.x, y: target.y }, kind: "act" })),
  ];
}
