# Creative hooks

Where your own ideas plug in: data, visuals, rules and feel. Each step adds rows.

| Hook | Where | Since step |
|---|---|---|
| The game's title | `GAME_TITLE` in `client/src/config.ts` (one line; the tab and the boot screen follow) | 0 |
| Base resolution and aspect ratio | `GAME_WIDTH` / `GAME_HEIGHT` in `client/src/config.ts`. Decide before Step 6 lays out the board. | 0 |
| Unit stats and new unit types | `content/units.json`, one line per type. Field rules are in `content/README.md`; `dotnet test` checks your edits. | 1 |
| Board size, obstacles, starting positions | `content/scenarios/classic.json` (the scenario the game uses). Other scenario files only get picked once a scenario choice exists (backlog). | 1 |
| What blocks movement (for example, walking *through* allies but not stopping on them) | `IsWalkable` in `src/Tactics.Core/Rules/Pathfinding.cs`. "Pass" and "stop" become two different checks; the `PathfindingTests` pictures show the effect. | 2 |
| 8-way movement (diagonals) | `Position.Neighbors()` in `src/Tactics.Core/Model/Position.cs`. Ranges use Manhattan distance, so they'd need a matching rule too. | 2 |
| Terrain costs (a forest costs 2 steps) | BFS treats every step as 1. Costs need Dijkstra: the same search with a priority queue instead of a queue (backlog). | 2 |
| Which of several equally short paths a unit walks | The neighbor order in `Position.Neighbors()`. Only tie-breaking changes; the reachable tiles stay the same. | 2 |
| The words players read when a move is refused | `WhyNotReachable` and the `Reject(...)` messages in `src/Tactics.Core/GameEngine.Move.cs`. Tests check codes, not wording. | 2 |
