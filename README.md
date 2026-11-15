# Tactics Duel

A two-player, turn-based fantasy tactics game that runs in the browser.
**The server is the referee:** an ASP.NET Core API and a pure C# rules engine decide every move.
**The browser is the view:** Phaser 4 + TypeScript draws the board and animates what happened.

> Working title. Built step by step as a learning project. See [`PLAN.md`](PLAN.md) for the rules, architecture and progress.

## Status
Step 8: a whole game plays in the browser, and it shows what happens. `/?mode=local` starts a game on the server for two players on one screen. Click a unit to light up where it can move and what it can hit, as the server lists it, then click to move or act, and press `E` to end the turn. The server judges every action; its events then play as animations (walking, lunges, hit flashes, damage numbers, fading units, turn banners) before the screen snaps to the server's state. A HUD shows the turn, the selected unit's stats and the End Turn button. The art and sound pipeline is next.

## Stack
- **Server:** .NET 10, ASP.NET Core Minimal APIs (SignalR and EF Core + PostgreSQL come later)
- **Rules engine:** `Tactics.Core`, pure C# with BCL only, deterministic
- **Client:** Vite, TypeScript (strict), Phaser 4, Vitest
- **Dev orchestration:** Aspire 13 (AppHost + dashboard)
- **Tests:** xUnit v3 on Microsoft.Testing.Platform, `WebApplicationFactory`

## Prerequisites
- .NET 10 SDK (pinned by `global.json`)
- Node.js LTS (22+)
- Aspire CLI 13.x ([aspire.dev](https://aspire.dev))
- Docker Desktop or Podman (needed from the persistence step onward)

One-time setup: trust the local HTTPS development certificate, so the dashboard opens without a browser warning.

```sh
aspire certs trust
```

## Run it
```sh
aspire run
```
Open the dashboard link printed in the terminal, then click the `client` URL.
Add `/?mode=local` to that address to start a local game (two players, one screen). Click one of your units, then a lit tile to move or a yellow target to act. `Esc` deselects, and `E` ends the turn.
To try the API from the browser, click the **Scalar (API docs)** link next to `api` (Development only).

Without Aspire, run these in two terminals:
```sh
dotnet run --project src/Tactics.Api --launch-profile http
cd client && npm run dev        # then open http://localhost:5173
```

## Test it
```sh
dotnet build        # warnings are errors: must be 0
dotnet test         # all .NET tests
cd client
npm run typecheck
npm test
npm run build
```

## Repo map
| Path | What lives there |
|---|---|
| `src/Tactics.Core` | The rules engine: pure, deterministic C# |
| `src/Tactics.Api` | The game server: HTTP endpoints, tokens, in-memory games (later: SignalR hub, PostgreSQL) |
| `src/Tactics.ServiceDefaults` | Shared telemetry, health checks, resilience |
| `src/Tactics.AppHost` | Aspire: runs everything locally with one command |
| `tests/` | xUnit v3 test projects |
| `client/` | Vite + TypeScript + Phaser 4 browser client |
| `content/` | Game data (unit stats, scenarios) |
| `docs/` | Learning reports, glossary, decisions, creative hooks |
