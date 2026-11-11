# content/

**This folder is game data you can edit.** No code lives here.

- `units.json` holds the unit types and their stats. (Step 1)
- `scenarios/*.json` holds board layouts and starting positions. (Step 1)

How it's used:
- `Tactics.Core` loads these files to build a game's setup. The rules never hard-code a stat.
- Each new game takes a **snapshot** of this data. Editing a file changes new games only, never running ones.
- All values start as *placeholders* from `PLAN.md`. Balancing them is your call.
