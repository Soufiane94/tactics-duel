# Creative hooks

Where your own ideas plug in: data, visuals, rules and feel. Each step adds rows.

| Hook | Where | Since step |
|---|---|---|
| The game's title | `GAME_TITLE` in `client/src/config.ts` (one line; the tab and the boot screen follow) | 0 |
| Base resolution and aspect ratio | `GAME_WIDTH` / `GAME_HEIGHT` in `client/src/config.ts`. Decide before Step 6 lays out the board. | 0 |
| Unit stats and new unit types | `content/units.json`, one line per type. Field rules are in `content/README.md`; `dotnet test` checks your edits. | 1 |
| Board size, obstacles, starting positions | `content/scenarios/classic.json` (the scenario the game uses). Other scenario files only get picked once a scenario choice exists (backlog). | 1 |
