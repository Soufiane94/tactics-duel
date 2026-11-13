# content/

**This folder is game data you can edit.** No code lives here.

- `units.json` holds the unit types and their stats.
- `scenarios/classic.json` holds the board, its obstacles and the starting positions. It's the scenario the game uses.

How it's used:
- `Tactics.Core` builds a game's setup from these files (`ContentLoader`). The rules never hard-code a stat.
- Each new game takes a **snapshot** of this data. Editing a file changes new games only, never running ones.
- All values start as *placeholders* from `PLAN.md`. Balancing them is your call.

## units.json
One unit type per line, in `"unitTypes": [ … ]`:

| Field | Meaning | Allowed |
|---|---|---|
| `id` | Key that placements use, e.g. `"archer"` | unique, not empty |
| `name` | Display name | not empty |
| `maxHp` | HP at full health | at least 1 |
| `power` | Damage dealt, or HP healed | at least 1 |
| `defense` | Subtracted from incoming damage | at least 0 |
| `move` | Tiles per move (4 directions) | at least 1 |
| `minRange` / `maxRange` | Target distance, in tiles (Manhattan) | at least 1, and min ≤ max |
| `ability` | `"damage"` (hits enemies) or `"heal"` (heals injured allies) | one of the two |

`power` and `defense` only meet in one file: `src/Tactics.Core/Rules/Combat.cs`. Today a hit does `power − defense` (at least 1), and a heal restores `power` (never past `maxHp`).

## scenarios/*.json
- `id`, `name`: the scenario's key (e.g. `"classic"`) and display name.
- `board`: `width`, `height`, and `obstacles` as `{ "x", "y" }` tiles. The origin is top-left, and y grows down.
- `placements`: `{ "id", "typeId", "owner", "x", "y" }` per unit.
  - `owner` is `"one"` (bottom, plays first) or `"two"`.
  - Ids must be unique. Tiles must be on the board, not on an obstacle, and not shared.
  - Each side needs at least one unit.

## When you make a mistake
- `dotnet test` fails, and the message lists **every** problem with its file, unit and tile. Example:
  `Scenario "classic": placement "one-mage" at (2, 5) is on an obstacle.`
- Fields the loader doesn't know are ignored, so you can add your own notes fields.
