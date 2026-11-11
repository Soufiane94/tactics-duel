# Start here

This kit makes Claude Code build **Tactics Duel** with you one step at a time. After every step it writes a learning report and stops.

## What's in the kit
- `CLAUDE.md`: rules Claude Code loads every session (workflow, learning-report format, art rule).
- `PLAN.md`: game rules, architecture, API contracts and 15 steps (0–14).
- `.claude/skills/next-step/`: the `/next-step` command.
- `.claude/skills/explain/`: the `/explain <thing>` command.
- `START_HERE.md`: this file.

`.claude` is a **hidden folder**. Make sure it gets copied.

## 1. Install (once)
- .NET 10 SDK
- Node.js LTS (22+)
- Git
- Docker Desktop (or Podman)
- Aspire CLI (aspire.dev → Get started)
- Claude Code

In Step 0, Claude checks all of these and tells you what's missing.

## 2. Set up
1. Create an empty folder (for example `tactics-duel`) and copy the whole kit into it, including `.claude/`.
2. Open a terminal in that folder and run `claude`.
3. Paste the kickoff prompt below.

## 3. Kickoff prompt (paste once)
```text
Read CLAUDE.md and PLAN.md completely. Don't create or change any files yet.
1. In at most 10 bullets, tell me how you understand: the game, the architecture, the step workflow and the learning-report format.
2. Check my machine for the Step 0 prerequisites (dotnet --list-sdks, node -v, npm -v, git --version, docker --version, aspire --version). Tell me what's missing and give me official install links.
3. Point out anything in PLAN.md that looks outdated or risky for my setup (versions, commands, APIs).
Then stop. When I reply "go", start Step 0 with its brief.
```

## 4. The loop (every step)
1. `/clear`. This starts a fresh context; `CLAUDE.md`, `PLAN.md` and the reports carry the memory.
2. Optional: switch to **plan mode** so Claude can only plan, not edit. Press `Shift+Tab` until the status bar shows plan mode, or start your message with `/plan`. On Windows, if `Shift+Tab` doesn't cycle, try `Alt+M`.
3. `/next-step`. Claude posts the step brief. Ask your questions, then reply `ok` (or approve the plan).
4. Claude builds, runs the checks, writes the learning report and stops.
5. Your turn: read the report, run **See it yourself**, then do **Try it yourself**. Don't skip it; it's where the learning sticks.
6. Reply `approved`. Claude commits. Back to 1.

Anytime: `/explain <file, function or concept>`.

## Where your creativity goes
- `content/`: unit stats and scenarios (from Step 1).
- `docs/CREATIVE_HOOKS.md`: the map of every hook, updated at each step.
- Step 9, art & sound: Claude points you to resources and explains licences. The style is yours. You can keep working on art while Steps 10–14 continue.

## Pace
Plan for about 1–2 hours per step, including reading and the exercise.
Steps 1–4 are pure C# with nothing on screen yet. That's on purpose: the rules engine is the heart of the game.
