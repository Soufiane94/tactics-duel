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
| The whole look today: colours, sizes, shapes, letters | `client/src/media/placeholder.ts`, the one PLACEHOLDER file. Other files only arrange things. Step 9 swaps it for your art. | 6 |
| What a unit shows (dim the units that already acted, HP as numbers, a shadow) | The `UnitSprite` constructor and `applyState` in `client/src/game/UnitSprite.ts`. The view already has `hasMoved` and `hasActed`. | 6 |
| The idle animation (speed, size, a bob instead of breathing, units out of step) | `breathingScale` in `client/src/game/unitDisplay.ts`, its numbers in `FEEL.idle` (`client/src/game/feel.ts`), and `UnitSprite.breathe`. | 6 |
| Tile size and pixel-art scaling | `TILE_SIZE` and `PIXEL_ART` in `client/src/config.ts`. Everything follows `TILE_SIZE`. Decide both with your art style (Step 9). | 6 |
| Where the board sits on screen (centred today; room for a side panel) | `layoutBoard` in `client/src/game/grid.ts`. Units, and clicks from Step 7, all ask `BoardRenderer.layout`. | 6 |
| Your own console commands (print the available actions, play a scripted opening) | `createDebugCommands` in `client/src/game/debugCommands.ts`. Dev builds only. | 6 |
| What a click means (click an enemy to inspect it, keep the selection on a plain tile, confirm End Turn while units can still act) | `transition` and `clickTile` in `client/src/game/InputController.ts`. `InputController.test.ts` spells out today's rules: change one and watch which tests fail. | 7 |
| Controls (Tab to cycle your units, Space to end the turn, right-click to cancel) | `listenToPlayer` in `client/src/scenes/GameScene.ts`. Each key or button becomes an `InputEvent`, and the state machine decides what it means. | 7 |
| How options look (the selected tile, move tiles, targets, the hover marker) | `HIGHLIGHT_LOOK` and the highlight colours in `client/src/media/placeholder.ts`. Which tiles light up is `highlightsFor` in `client/src/game/highlights.ts`. | 7 |
| Selection feedback (a sound or a bounce when you select a unit) | `GameScene.renderInput` in `client/src/scenes/GameScene.ts` runs after every state change. A tween (`client/src/game/tweens.ts`) or a Step 9 sound can start there. | 7 |
| The words players read (the turn line, the "game had changed" message) | `turnLine` in `client/src/game/turnText.ts` and `OUT_OF_DATE_MESSAGE` in `client/src/net/play.ts`. Refusals use the server's own sentences (see the Step 2–5 rows). | 7 |
| How long a message stays on screen, and its fades | `FEEL.toast` in `client/src/game/feel.ts`. The message box itself is `client/src/hud/Toast.ts`. | 7 |
| Animation timing and feel (durations, easing curves, the shake's strength) | `FEEL` in `client/src/game/feel.ts`, one table. Set `speed: 0.25` to watch everything in slow motion while you tune. | 8 |
| What each event looks like (an arrow for archers, a pulse on the healer, a bigger shake on a killing blow, a pause before the victory) | One method per event in `client/src/game/SceneAnimator.ts`; a unit's own moves (`walk`, `lungeOut`, `flash`, `vanish`) in `client/src/game/UnitSprite.ts`. Inside a method, `await` means "then" and `Promise.all` means "together". | 8 |
| The HUD's layout (where the turn line, the unit card and the End Turn button sit) | `LAYOUT` in `client/src/scenes/HudScene.ts`, measured from the screen's edges, not from the board. | 8 |
| The HUD's look (text sizes, card and banner backgrounds, the side swatches) | `HUD_TEXT` and `HUD_SHAPES` in `client/src/media/placeholder.ts`. Step 9 swaps them for your style. | 8 |
| What a unit's card shows (enemies on hover, icons instead of words, what it can still do this turn) | `unitPanelFor` and `hudModelFor` in `client/src/hud/hudModel.ts`; `client/src/hud/UnitPanel.ts` only draws. Hover-inspect is the Step 8 "Try it yourself". | 8 |
| The big announcements ("Your move!", "Defeat", the turn number in the banner) | `bannerText` and `resultText` in `client/src/game/turnText.ts`, each with a shared-screen and a one-seat wording. The victory hint is `PLAY_AGAIN_HINT` in `client/src/hud/Banner.ts`. | 8 |
| New HUD messages (a "units left" counter, a hint when only End Turn is left) | A field in `HudModel` (`client/src/hud/hudModel.ts`) if it follows the state; otherwise a new message in `HudEvents` (`client/src/hud/hudEvents.ts`), sent by `GameScene` and drawn by `HudScene`. | 8 |
