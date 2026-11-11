# CLAUDE.md — Tactics Duel

Instructions for Claude Code. Loaded automatically every session.

## Who I am and how to work with me
- I'm an experienced .NET developer (C#, ASP.NET Core, SQL, Angular/TypeScript). I'm **new to game development**.
- This project has two goals of equal weight:
  1. **I learn game development**: concepts, vocabulary and how game logic flows.
  2. **A clean .NET full-stack portfolio piece** that I can show to employers.
- Talk to me in English unless I write in French. Keep standard tech terms in English.
- Be direct. If my request or this plan has a weak spot, say so and propose a better option.
- Explain new game-dev ideas by comparing them to C#/.NET/Angular things I already know.

## Source of truth
- `PLAN.md` has the game rules (v1), architecture, API contracts, every step and the progress checklist. Read it at the start of every session.
- `docs/learning/` holds one learning report per step, my course notes. `docs/learning/GLOSSARY.md` collects every new term.
- `docs/CREATIVE_HOOKS.md` maps where I can plug in my own ideas (data, visuals, rules, feel).
- `docs/DECISIONS.md` is a short log of technical decisions (decision · why · date).
- `content/` holds game data I edit by hand (unit stats, scenarios).

## Stack (don't change without asking me)
- .NET 10 (LTS), C#, ASP.NET Core Minimal APIs, SignalR, EF Core 10 + Npgsql (PostgreSQL)
- .NET 11 is out, so `dotnet add package` and `dotnet tool install` without a version now pick 11.0.0. Keep ASP.NET Core, EF Core, the SignalR client and `dotnet-ef` on 10.0.x: set the versions in `Directory.Packages.props` and the tool manifest.
- Aspire 13.x for local orchestration (AppHost + ServiceDefaults)
- Tests: xUnit v3, WebApplicationFactory, Testcontainers (PostgreSQL). Use xUnit `Assert`. Don't add FluentAssertions (commercial license since v8).
- Client: Vite + TypeScript (strict) + Phaser 4.x, Vitest, `@microsoft/signalr`
- Node.js LTS (22+). Docker for Testcontainers, Aspire's Postgres container and the final image.
- Use the same PostgreSQL major version everywhere (Aspire, Testcontainers, hosting).

## Phaser 4 warning
- Most Phaser code online, and most of your training data, is **Phaser 3**. Phaser 4 changed several APIs: the renderer, tint, FX/filters, and some v3 helpers such as `Geom.Point` and `Math.PI2` are gone.
- Before writing Phaser code, read the relevant official Phaser skills in `.claude/skills/` (copied from `client/node_modules/phaser/skills/` in Step 0). Start with `v3-to-v4-migration`. If you're still unsure, check docs.phaser.io. Never guess an API.
- After upgrading Phaser, copy the skills again.

## Architecture rules (non-negotiable)
1. **The server is the referee.** All rules live in `Tactics.Core` (C#). The client never decides whether a move is legal. It shows the `availableActions` the server sends and posts requests.
2. **`Tactics.Core` is pure.** No I/O, no database, no `DateTime.Now`, no `Random`, no NuGet packages (BCL only). Same inputs always give the same outputs. That's what makes replays and tests possible.
3. **State is truth, events are for animation.** Every accepted action returns events (what happened) and the new state (where we are now). The client animates the events, then snaps to the state.
4. **Immutable records** for game state. Changes return new instances (`with`).
5. **Data-driven.** Stats and scenarios come from `content/*.json`, not from code.
6. **The server fills in the player.** Client requests never say which player they are. The server derives it from the player token.
7. **Placeholder visuals only** (see the art rule).

## Art & style rule
- Never choose, generate, download or recommend a specific art style, palette, font, asset pack or sound. That's my creative part of the project.
- Use only generated placeholder shapes (rectangles, circles, letters) defined in one file marked `PLACEHOLDER`. Team colours can be two plain debug colours, nothing more.
- At the art step (Step 9), point me to resources and explain trade-offs neutrally. Don't pick for me.
- The only exception is when I explicitly ask for a specific art task.

## Naming rule (the title will change)
- "Tactics Duel" is a working title. I'll probably rename the game, so renaming it in code must stay a one-line change.
- The player-visible title is defined once: `GAME_TITLE` in `client/src/config.ts`. The boot screen, menu, page title and "Your turn" tab title all read it. Never type the title anywhere else in code. The server doesn't need it.
- Code names use the genre word, not the title: `Tactics.*` projects and namespaces, `TacticsJson`, the `tactics` database. They stay when the title changes. No `TacticsDuel`-style identifiers.
- Never put the title in names other things depend on: `localStorage` keys, database and table names, Aspire resource names, Docker images and volumes, connection-string names. Renaming those breaks saved games, sessions and scripts.
- Docs (README, these files, learning reports) can say "Tactics Duel" for now. They're a find-and-replace at rename time.
- Before Step 14 (public URL, GitHub repo name), ask me whether the final name is chosen.

## Step workflow (follow it every time)
Work on ONE step of `PLAN.md` at a time. Never start the next step on your own.

**Phase 1 — Brief (no file changes).** Post:
- `Step NN — <title>` and its goal in one sentence
- Files you'll create or change
- Main types and functions you'll write (one line each)
- Tests and checks you'll run
- New game-dev concepts I'll learn
- Open questions: only real ones, each with your recommended answer

Then wait for my `ok`, or for plan approval if I'm in plan mode.

**Phase 2 — Build.**
- Work in small, reviewable increments. Build and run tests after each increment.
- Follow `PLAN.md`. If something in it is wrong or outdated (an API changed, a version doesn't exist), stop and propose a `PLAN.md` change. Don't deviate silently.
- Ask before adding any dependency `PLAN.md` doesn't name.

**Phase 3 — Verify.**
- Run every check listed in the step (build with 0 warnings, all tests, typecheck, etc.). Fix until everything passes.
- Never say "done" without having run them.
- If something needs my eyes (the visual result in the browser), tell me exactly what to run and what I should see.

**Phase 4 — Teach.**
- Write the learning report (template below) to `docs/learning/step-NN-<slug>.md`, then print it in the chat.
- Add new terms to `docs/learning/GLOSSARY.md` and link the report in `docs/learning/README.md`.
- Add new hooks to `docs/CREATIVE_HOOKS.md`. Log non-obvious choices in `docs/DECISIONS.md`.
- Tick the step in the Progress list of `PLAN.md`. Note any deviation in its "Deviations log".

**Phase 5 — Stop.**
- End with a suggested commit message and: "Reply `approved` to commit, or give me feedback."
- On `approved`: `git add -A`, commit with that message, and stop. I'll run `/clear` and then `/next-step`.
- On feedback: fix it, verify again, update the report if needed, then stop again.

## Learning report template
File: `docs/learning/step-NN-<slug>.md`. Fill in every section and delete none.

```markdown
# Step NN — <Title>

**In one sentence:** <what the game can do now that it couldn't before>

## What we built
- <3–6 bullets: the thing + its file in backticks>

## New game-dev ideas
- **<Concept>** — <what it is, 1–2 short sentences>. *Like in .NET/Angular:* <analogy>.
- (3–6 concepts at most)

## Main functions
- `Name(params)` — `path/to/file` — <what it does> · <why it exists>
- (5–10 entries, in the order they run)

## How the logic flows
1. <trigger, e.g. "You click a tile">
2. <what happens next, naming the function>
3. …
(Add a small Mermaid diagram only when 3+ parts talk to each other.)

## See it yourself
- Run: `<command>` · Open: `<url>` · You should see: <what>

## Try it yourself (10–15 min)
- <one small change I make myself> → <what I should observe>
- Hint: <the file to open>

## Your creative hooks
- <where my own ideas can plug in after this step: file + what to change>

## Words to know
- **<term>** — <one-line definition>

## Watch out
- <1–3 gotchas or common mistakes>
```

### Writing rules for reports
- Bullets everywhere. Short sentences (under about 20 words). Plain English.
- Define every new term in one line the first time it appears.
- Explain **why**, not only what.
- Use real names in backticks: functions, files, commands.
- Keep it to one or two screens (120 lines at most). If it's longer, cut.
- No filler, no hype.

## Code conventions
- C#: file-scoped namespaces, records for data, `sealed` by default, nullable enabled, `TreatWarningsAsErrors`, no `async void`. Give every public type and method in `Tactics.Core` a 1–2 line XML doc comment that explains the *why*.
- TypeScript: `strict`, no `any`, ES modules, small files. Add short comments where game-dev logic isn't obvious (coordinates, timing, depth).
- Names follow the domain: `Unit`, `Board`, `Position`, `MoveAction`, `ActAction`, `EndTurnAction`, `GameView`, events…
- Keep pure logic (grid math, the input state machine, event sequencing) out of Phaser classes so Vitest can test it.
- Cross-platform: I may be on Windows. Prefer `dotnet` and `npm` commands. No bash-only scripts in the repo.
- Never commit secrets. Connection strings come from Aspire in dev and from environment variables in production.
- Don't put personal or employer information in the repo.

## Commands (keep this section up to date)
- Run everything (dev): `aspire run` at the repo root → open the dashboard link it prints → click the `client` URL. Stop with Ctrl+C.
  - Background mode for scripted checks: `aspire run --detach` · `aspire wait client` · `aspire describe` · `aspire logs <resource>` · `aspire stop`
  - One-time: `aspire certs trust` (trusts the HTTPS dev certificate for the dashboard)
- Without Aspire: `dotnet run --project src/Tactics.Api --launch-profile http` + `npm run dev` in `client/` → http://localhost:5173
- .NET build: `dotnet build` (warnings are errors in every project: must be 0)
- .NET tests: `dotnet test` (all test projects, Microsoft.Testing.Platform via `global.json`)
  - Core only (fast): `dotnet test --project tests/Tactics.Core.Tests`
  - While `aspire run` is running, a root `dotnet build` fails with file locks (MSB3027) on the API's DLLs: stop it first, or build only the project you changed.
- Client (in `client/`): `npm run dev` · `npm run build` · `npm run typecheck` · `npm test`
- After upgrading Phaser: copy every folder in `client/node_modules/phaser/skills/` into `.claude/skills/` again
