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
| Combat formulas (damage, heal) | `src/Tactics.Core/Rules/Combat.cs`, the only place with formulas. Ideas: percentage defense, bonuses against some types, counter-attacks. Critical hits need seeded randomness first (backlog). The damage table in `CombatTests` shows the effect. | 3 |
| Who may target whom (friendly fire, line of sight, healing at full HP) | `CheckTarget` in `src/Tactics.Core/GameEngine.Act.cs`. `GetAvailableActions` reuses it, so a rule change shows up in the client's highlights too. | 3 |
| Healers healing themselves | Allow `minRange: 0` in `ContentLoader` (it requires at least 1 today), then set it in `content/units.json`. The engine already copes with a unit that is both the actor and the target (`A_unit_with_MinRange_0_can_heal_itself`). | 3 |
| New abilities (push, buff, area damage) | A new value in `src/Tactics.Core/Model/Ability.cs`, a case in `CheckTarget`, a case in `ApplyAct`'s switch, and new events if needed. | 3 |
| What happens at 0 HP | `Hit` in `src/Tactics.Core/GameEngine.Act.cs` removes the unit. A revive ability would need defeated units kept somewhere, for example a list in `GameState`. | 3 |
| The words players read when an act is refused | The `Reject(...)` messages and `WhyOutOfRange` in `src/Tactics.Core/GameEngine.Act.cs`. Tests check codes, not wording. | 3 |
| Win conditions (hold a tile, survive until turn N, a draw when both sides fall at once) | `Victory.WinnerOf` in `src/Tactics.Core/Rules/Victory.cs`. It runs after every accepted action and only reads the state, so positions and `TurnNumber` are there. A draw also needs a `Winner` of `null` with `Finished`, and its own event. | 4 |
| What a new turn brings (units that waited heal a little, poison ticks, reinforcements) | `ApplyEndTurn` in `src/Tactics.Core/GameEngine.Turn.cs`. Add an event if the client should show it. | 4 |
| A "danger zone" preview: the tiles enemies could hit next turn | A new function next to `GetAvailableActions` in `src/Tactics.Core/GameEngine.AvailableActions.cs`, built from `Pathfinding.Reachable` and `CheckTarget`. A classic tactics-game aid. | 4 |
| Undo in local mode, a replay viewer, "show me the last turn" | `GameEngine.Replay(setup, log.SkipLast(n))`: any earlier moment of a game is one call away. | 4 |
| An AI opponent (backlog) | It can try every entry of `GetAvailableActions`, plus ending the turn, and score the states that `Apply` returns. | 4 |
| The invite code's look (length, characters, words instead of letters) | `InviteCodeAlphabet` and `InviteCodeLength` in `src/Tactics.Api/Games/PlayerSecrets.cs`. Shorter codes are easier to type and easier to guess. `PlayerSecretsTests` checks the rules. | 5 |
| The words players read when a request is refused (not a player, out of date, seat taken) | The messages at the bottom of `src/Tactics.Api/Games/GameService.cs`, and the titles in `src/Tactics.Api/Http/ApiProblems.cs`. Clients branch on `code`, so the wording is free. | 5 |
| Which scenario new games use, and later a scenario choice | `SetupProvider.DefaultScenarioId` in `src/Tactics.Api/Games/SetupProvider.cs`. A choice means loading more scenario files and adding a `scenarioId` to `POST /api/games`. | 5 |
| Hidden information (fog of war: you only see enemies near your units) | `ContractMapping.ToView` in `src/Tactics.Api/Contracts/ContractMapping.cs` builds each player's view. Filter `units` there; the rules still see everything. | 5 |
