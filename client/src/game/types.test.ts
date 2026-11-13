import { describe, expect, it } from "vitest";
import type {
  CreateGameResponse,
  GameAction,
  GameEvent,
  GameView,
  ProblemDetails,
  SubmitActionRequest,
} from "./types";

/*
 * PLAN.md's contract examples, typed with the mirrors in types.ts. `npm run typecheck` compiles this file too, so
 * if a mirror drifts from the documented JSON, typecheck fails right here. On its own, typecheck would only prove
 * that types.ts compiles.
 */

const view: GameView = {
  gameId: "0b8f6e2a-61c4-7d0e-9a35-2f1c8d4e5b6a",
  sequence: 12,
  yourSide: "one",
  currentPlayer: "two",
  turnNumber: 4,
  status: "inProgress",
  winner: null,
  opponentJoined: true,
  board: { width: 8, height: 8, obstacles: [{ x: 2, y: 3 }] },
  unitTypes: [
    { id: "archer", name: "Archer", maxHp: 18, power: 8, defense: 1, move: 3, minRange: 2, maxRange: 3, ability: "damage" },
  ],
  units: [{ id: "one-archer", typeId: "archer", owner: "one", x: 1, y: 7, hp: 18, hasMoved: false, hasActed: false }],
  availableActions: [{ unitId: "one-archer", moveTargets: [{ x: 1, y: 6 }], actTargets: ["two-warrior"] }],
};

const created: CreateGameResponse = {
  gameId: view.gameId,
  side: "one",
  playerToken: "Q3Ut8-xhgNEQ28y7DUqJUEFxer21IvXzhC8OyIryuGg",
  inviteCode: "K7QF3MZP",
  view,
};

const requests: SubmitActionRequest[] = [
  { expectedSequence: 12, action: { type: "move", unitId: "one-archer", to: { x: 1, y: 5 } } },
  { expectedSequence: 13, action: { type: "act", actorId: "one-archer", targetId: "two-warrior" } },
  { expectedSequence: 14, action: { type: "endTurn" } },
];

const events: GameEvent[] = [
  { type: "unitMoved", unitId: "one-archer", from: { x: 1, y: 7 }, to: { x: 1, y: 5 }, path: [{ x: 1, y: 6 }, { x: 1, y: 5 }] },
  { type: "unitDamaged", unitId: "two-warrior", byUnitId: "one-archer", amount: 5, hpAfter: 23 },
  { type: "unitHealed", unitId: "one-warrior", byUnitId: "one-healer", amount: 5, hpAfter: 25 },
  { type: "unitDefeated", unitId: "two-mage" },
  { type: "turnEnded", previousPlayer: "one", nextPlayer: "two", turnNumber: 2 },
  { type: "gameWon", winner: "one" },
];

const ruleViolation: ProblemDetails = {
  title: "Rule violation",
  status: 422,
  code: "TargetOutOfRange",
  detail: "Archer hits targets 2–3 tiles away; this target is 1 tile away.",
};

/**
 * One line of text per event. The `never` line makes the switch exhaustive: add an event type to `GameEvent`
 * and this function stops compiling until it handles it. Step 8's EventPlayer will use the same trick.
 */
function summarize(event: GameEvent): string {
  switch (event.type) {
    case "unitMoved":
      return `${event.unitId} walks ${event.path.length} tiles`; // TypeScript knows `path` exists here
    case "unitDamaged":
      return `${event.unitId} loses ${event.amount} HP`;
    case "unitHealed":
      return `${event.unitId} gets ${event.amount} HP back`;
    case "unitDefeated":
      return `${event.unitId} is defeated`;
    case "turnEnded":
      return `turn ${event.turnNumber}: player ${event.nextPlayer}`;
    case "gameWon":
      return `player ${event.winner} wins`;
    default: {
      const unhandled: never = event;
      return unhandled;
    }
  }
}

describe("the API contract mirrors", () => {
  it("accept PLAN.md's documented examples", () => {
    expect(created.view.availableActions[0]?.actTargets).toEqual(["two-warrior"]);
    expect(requests.map((request) => request.action.type)).toEqual(["move", "act", "endTurn"]);
    expect(ruleViolation.code).toBe("TargetOutOfRange");
  });

  it("let a switch on `type` handle every event", () => {
    expect(events.map(summarize)).toEqual([
      "one-archer walks 2 tiles",
      "two-warrior loses 5 HP",
      "one-warrior gets 5 HP back",
      "two-mage is defeated",
      "turn 2: player two",
      "player one wins",
    ]);
  });

  it("give actions no player field: the server takes the side from the token", () => {
    // @ts-expect-error: "player" doesn't exist on any action (rule 6). If someone adds it, typecheck fails here.
    const cheat: GameAction = { type: "endTurn", player: "one" };
    expect(cheat.type).toBe("endTurn");
  });
});
