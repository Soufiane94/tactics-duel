import type { GameView, PlayerSide } from "./types";

/*
 * The words of the temporary turn controls (`TurnControls`), until Step 8's HUD. No Phaser import, so Vitest tests
 * them.
 */

/** A side as players read it. */
export function sideName(side: PlayerSide): string {
  return side === "one" ? "Player One" : "Player Two";
}

/**
 * The line above the board: whose turn it is, or who won. Both players share the screen in pass & play, so it names
 * the side instead of saying "your turn".
 */
export function statusLine(view: GameView): string {
  if (view.status === "finished") {
    // v1 has no draws, but the contract allows a finished game without a winner.
    return view.winner === null ? "Game over" : `${sideName(view.winner)} wins!`;
  }
  return `Turn ${view.turnNumber} · ${sideName(view.currentPlayer)} to play`;
}
