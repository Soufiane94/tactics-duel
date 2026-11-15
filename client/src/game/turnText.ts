import type { GameView, PlayerSide } from "./types";

/*
 * The words players read about turns and results: the HUD's turn line, the turn banner, the victory overlay.
 * No Phaser import, so Vitest tests them.
 */

/**
 * Whom the texts speak to:
 * - null: the screen belongs to no single side. In pass & play both players share it, so "your turn" would mean
 *   nobody: the texts name the sides ("Player Two to play").
 * - a side: the screen plays for that seat (online, from Step 10), so the texts say "Your turn", "You win!".
 * `viewerOf` (`net/session.ts`) picks it from the seats this browser holds.
 */
export type Viewer = PlayerSide | null;

/** A side as players read it. */
export function sideName(side: PlayerSide): string {
  return side === "one" ? "Player One" : "Player Two";
}

/** The HUD's turn line: the turn and who plays, or the result once the game is over. */
export function turnLine(view: GameView, viewer: Viewer): string {
  if (view.status === "finished") {
    return resultText(view.winner, viewer);
  }
  return `Turn ${view.turnNumber} · ${whoPlays(view.currentPlayer, viewer)}`;
}

/** The turn banner, when `next` starts playing. */
export function bannerText(next: PlayerSide, viewer: Viewer): string {
  if (viewer === null) {
    return `${sideName(next)}'s turn`;
  }
  return next === viewer ? "Your turn" : "Opponent's turn";
}

/** The result, said to this screen. v1 has no draws, but the contract allows a finished game without a winner. */
export function resultText(winner: PlayerSide | null, viewer: Viewer): string {
  if (winner === null) {
    return "Game over";
  }
  if (viewer === null) {
    return `${sideName(winner)} wins!`;
  }
  return winner === viewer ? "You win!" : "You lose";
}

function whoPlays(current: PlayerSide, viewer: Viewer): string {
  if (viewer === null) {
    return `${sideName(current)} to play`;
  }
  return current === viewer ? "Your turn" : "Opponent's turn";
}
