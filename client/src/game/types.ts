/**
 * TypeScript mirrors of the API's JSON contract: `src/Tactics.Api/Contracts` and PLAN.md, section "Contracts".
 * Written by hand for v1, so when a C# DTO changes, change it here too (backlog: generate them from OpenAPI).
 * Names are the C# DTO names without their "Dto" suffix; the unions are named like Core's base records.
 *
 * The client only reads these shapes and sends requests. It never decides whether a move is legal:
 * `GameView.availableActions` already says what each unit may do.
 */

// ---- Building blocks ----------------------------------------------------------------------------

/** The two seats. Player One starts at the bottom of the board and plays first. */
export type PlayerSide = "one" | "two";

export type GameStatus = "inProgress" | "finished";

/** What acting does: damage an enemy, or heal an injured ally. */
export type Ability = "damage" | "heal";

/** A tile. The origin is the top-left corner: x grows to the right, y grows down, like screen pixels. */
export interface Position {
  x: number;
  y: number;
}

// ---- The view: GET /api/games/{gameId}, and the "view" after every action ------------------------

export interface GameView {
  gameId: string;
  /** How many actions have been played. Send it back as `expectedSequence` with the next action. */
  sequence: number;
  /** Your side, from your player token. */
  yourSide: PlayerSide;
  currentPlayer: PlayerSide;
  turnNumber: number;
  status: GameStatus;
  winner: PlayerSide | null;
  /** False while seat two is free. Player One can already play turn 1. */
  opponentJoined: boolean;
  board: BoardView;
  /** Stats from this game's own setup snapshot: editing content/ never changes a running game. */
  unitTypes: UnitTypeView[];
  units: UnitView[];
  /** What your units can still do. Empty when it isn't your turn or the game is over. */
  availableActions: UnitActionsView[];
}

export interface BoardView {
  width: number;
  height: number;
  /** Tiles no unit can enter, in reading order (row by row, left to right). */
  obstacles: Position[];
}

export interface UnitTypeView {
  id: string;
  name: string;
  maxHp: number;
  power: number;
  defense: number;
  move: number;
  minRange: number;
  maxRange: number;
  ability: Ability;
}

export interface UnitView {
  id: string;
  typeId: string;
  owner: PlayerSide;
  /** The unit's tile, flattened: a unit is { x, y }, not { position: { x, y } }. */
  x: number;
  y: number;
  hp: number;
  hasMoved: boolean;
  hasActed: boolean;
}

/** What one of your units can still do this turn. Highlight exactly these; compute none of your own. */
export interface UnitActionsView {
  unitId: string;
  /** Tiles it can move to, nearest first. */
  moveTargets: Position[];
  /** Ids of the units it can act on from where it stands. */
  actTargets: string[];
}

// ---- Actions: POST /api/games/{gameId}/actions ----------------------------------------------------
// No action has a "player" field: the server takes the side from the player token (rule 6).

export interface MoveAction {
  type: "move";
  unitId: string;
  to: Position;
}

export interface ActAction {
  type: "act";
  actorId: string;
  targetId: string;
}

export interface EndTurnAction {
  type: "endTurn";
}

/** A discriminated union: `type` tells TypeScript which fields exist, like a C# switch on record types. */
export type GameAction = MoveAction | ActAction | EndTurnAction;

export interface SubmitActionRequest {
  /** The `sequence` of the view you acted on. If it's out of date, the server answers 409 SequenceConflict. */
  expectedSequence: number;
  action: GameAction;
}

export interface SubmitActionResponse {
  /** The action's number, which is also the new view's sequence. */
  sequence: number;
  /** What happened, in order: animate these first. */
  events: GameEvent[];
  /** Where the game is now: snap to it after the animations. */
  view: GameView;
}

// ---- Events: what happened, in order -------------------------------------------------------------

export interface UnitMoved {
  type: "unitMoved";
  unitId: string;
  from: Position;
  to: Position;
  /** Every tile stepped on, in order: without `from`, with `to`. */
  path: Position[];
}

export interface UnitDamaged {
  type: "unitDamaged";
  unitId: string;
  byUnitId: string;
  /** How hard the hit was. On a killing blow it can exceed the HP the unit had left. */
  amount: number;
  hpAfter: number;
}

export interface UnitHealed {
  type: "unitHealed";
  unitId: string;
  byUnitId: string;
  amount: number;
  hpAfter: number;
}

export interface UnitDefeated {
  type: "unitDefeated";
  unitId: string;
}

export interface TurnEnded {
  type: "turnEnded";
  previousPlayer: PlayerSide;
  nextPlayer: PlayerSide;
  /** The number of the turn that starts now. */
  turnNumber: number;
}

export interface GameWon {
  type: "gameWon";
  winner: PlayerSide;
}

export type GameEvent = UnitMoved | UnitDamaged | UnitHealed | UnitDefeated | TurnEnded | GameWon;

// ---- Lobby ---------------------------------------------------------------------------------------

/** POST /api/games (201). */
export interface CreateGameResponse {
  gameId: string;
  side: "one";
  /** Your secret for this game, sent as "Authorization: Bearer <token>". Shown only once. */
  playerToken: string;
  /** Give it to your opponent: it lets them take seat two, once. */
  inviteCode: string;
  view: GameView;
}

/** POST /api/games/{gameId}/join. */
export interface JoinGameRequest {
  inviteCode: string;
}

/** POST /api/games/{gameId}/join (200). */
export interface JoinGameResponse {
  gameId: string;
  side: "two";
  playerToken: string;
  view: GameView;
}

// ---- Errors (always ProblemDetails, RFC 9457) ----------------------------------------------------

/** Why the rules engine refused an action (HTTP 422). Same names as Core's `RuleViolation` enum. */
export type RuleViolationCode =
  | "GameOver"
  | "NotYourTurn"
  | "UnitNotFound"
  | "NotYourUnit"
  | "AlreadyMoved"
  | "AlreadyActed"
  | "DestinationNotReachable"
  | "TargetNotFound"
  | "TargetOutOfRange"
  | "InvalidTarget";

/** Every `code` the API sends: 403 InvalidInviteCode, 409 AlreadyJoined or SequenceConflict, 422 a rule. */
export type ApiErrorCode = RuleViolationCode | "InvalidInviteCode" | "AlreadyJoined" | "SequenceConflict";

/** Every error body. Branch on `status` and `code`, never on the wording of `detail`. */
export interface ProblemDetails {
  type?: string;
  title: string;
  status: number;
  /** A sentence for the player, e.g. "It's Player One's turn." */
  detail?: string;
  /** Present for 403, 409 and 422. 400, 401 and 404 need no code: the status says it all. */
  code?: ApiErrorCode;
}
