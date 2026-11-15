import type { GameAction, GameView, Position } from "./types";
import { optionsOf, unitAt } from "./viewQueries";

/*
 * The client's input state machine: what a click or a key means depends on the state the input is in.
 * The same click on an empty tile moves the selected unit, or does nothing when no unit is selected, or is ignored
 * while an answer is awaited. Naming each state makes those cases explicit, instead of a pile of booleans
 * (`isSelected && !isWaiting && isMyTurn…`) that can contradict each other.
 *
 * It's one pure function, `transition`, like an NgRx reducer: (state, event) in, new state out. It never sends
 * anything itself: it returns the action to send, and the scene does the I/O. It decides no rule either. Every
 * choice comes from the server's `availableActions`. No Phaser import, so Vitest tests every transition.
 */

/**
 * Where the input stands. Exactly one state at a time, each carrying only the data it needs, like a closed
 * hierarchy of C# records (`kind` is the discriminator).
 */
export type InputState =
  /** Your turn, nothing selected. */
  | { readonly kind: "idle" }
  /** Your turn, and one of your units is selected: its moves and targets are highlighted. */
  | { readonly kind: "unitSelected"; readonly unitId: string }
  /**
   * Waiting: an action is on its way, or its answer's animation is playing. Input is ignored, so clicks and keys
   * during an animation do nothing. `reselect` is the unit to select again once the answer is on screen, if it can
   * still do something. Null after End Turn.
   */
  | { readonly kind: "busy"; readonly reselect: string | null }
  /** The other player is playing. */
  | { readonly kind: "opponentTurn" }
  /** Someone won. Nothing more can be played. */
  | { readonly kind: "gameOver" };

/** What can happen to the input: the player's clicks and keys, and how requests end. */
export type InputEvent =
  /** A click on a board tile. A click outside the board is `cancelled`. */
  | { readonly type: "tileClicked"; readonly tile: Position }
  /** `Esc`, or a click outside the board: drop the selection. */
  | { readonly type: "cancelled" }
  /** The `E` key or the End Turn button. */
  | { readonly type: "endTurnPressed" }
  /**
   * A new view is on screen: the answer to a request (after its animation), a fresh copy after a conflict, or
   * (Step 11) a push.
   */
  | { readonly type: "viewReceived" }
  /** The request ended without a new view (a refusal, no network): the view on screen still holds. */
  | { readonly type: "requestFailed" };

/** One step of the machine: the next state, and the action to send to the server, if any. */
export interface Transition {
  readonly state: InputState;
  /** The scene sends it, then reports how it went: `viewReceived` or `requestFailed`. */
  readonly request: GameAction | null;
}

/**
 * The state that a view alone implies, with nothing selected. Game over comes first: when the game ends, the winner
 * is still the player to move, but there's nothing left to play.
 */
export function stateForView(view: GameView): InputState {
  if (view.status === "finished") {
    return { kind: "gameOver" };
  }
  return view.currentPlayer === view.yourSide ? { kind: "idle" } : { kind: "opponentTurn" };
}

/** True when clicks and keys can do something: your turn, and no answer awaited. Drives the hover and the button. */
export function acceptsInput(state: InputState): boolean {
  return state.kind === "idle" || state.kind === "unitSelected";
}

/**
 * The whole machine: the next state after `event`. `view` is the view on screen after the event, so for
 * `viewReceived` it's already the new one. Busy, opponent's turn and game over ignore the player's input.
 */
export function transition(state: InputState, event: InputEvent, view: GameView): Transition {
  switch (event.type) {
    case "viewReceived":
      // A new view can keep a selection (the unit moved and can still act) or end it (nothing left to do).
      return stay(settle(selectionOf(state), view));

    case "requestFailed":
      // Nothing changed on the server, so the old view holds and the selection comes back.
      return stay(state.kind === "busy" ? settle(state.reselect, view) : state);

    case "tileClicked":
      return clickTile(state, event.tile, view);

    case "cancelled":
      return stay(state.kind === "unitSelected" ? { kind: "idle" } : state);

    case "endTurnPressed":
      return acceptsInput(state) ? send(null, { type: "endTurn" }) : stay(state);
  }
}

function clickTile(state: InputState, tile: Position, view: GameView): Transition {
  const clicked = unitAt(view, tile);

  if (state.kind === "idle") {
    // Only a unit the server lists can be selected: on your turn, that's one of yours with something left to do.
    return stay(clicked !== undefined && optionsOf(view, clicked.id) !== undefined ? selected(clicked.id) : state);
  }
  if (state.kind !== "unitSelected") {
    return stay(state);
  }

  const options = optionsOf(view, state.unitId);
  if (options === undefined) {
    // The selected unit has nothing left. `viewReceived` already drops such a selection; this is a safety net.
    return stay({ kind: "idle" });
  }

  // A highlighted target wins, even when it's one of your own units: a healer's click on an injured ally heals it.
  if (clicked !== undefined && options.actTargets.includes(clicked.id)) {
    return send(state.unitId, { type: "act", actorId: state.unitId, targetId: clicked.id });
  }
  if (options.moveTargets.some((target) => target.x === tile.x && target.y === tile.y)) {
    return send(state.unitId, { type: "move", unitId: state.unitId, to: { x: tile.x, y: tile.y } });
  }
  // Another of your units with something to do: select it instead.
  if (clicked !== undefined && clicked.id !== state.unitId && optionsOf(view, clicked.id) !== undefined) {
    return stay(selected(clicked.id));
  }
  // Anything else, the selected unit included: deselect.
  return stay({ kind: "idle" });
}

/** The state a view implies, with `unitId` selected again if the server still lists it. */
function settle(unitId: string | null, view: GameView): InputState {
  const resting = stateForView(view);
  if (resting.kind === "idle" && unitId !== null && optionsOf(view, unitId) !== undefined) {
    return selected(unitId);
  }
  return resting;
}

/**
 * The unit a state would like to keep selected: the selected one, or the one to reselect after an answer. The HUD
 * shows its panel, so the panel stays up while the unit's action plays.
 */
export function selectionOf(state: InputState): string | null {
  switch (state.kind) {
    case "unitSelected":
      return state.unitId;
    case "busy":
      return state.reselect;
    default:
      return null;
  }
}

function selected(unitId: string): InputState {
  return { kind: "unitSelected", unitId };
}

/** A transition that sends nothing. */
function stay(state: InputState): Transition {
  return { state, request: null };
}

/** A transition that sends an action and waits for the answer. */
function send(reselect: string | null, request: GameAction): Transition {
  return { state: { kind: "busy", reselect }, request };
}
