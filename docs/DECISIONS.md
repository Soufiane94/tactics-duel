# Decisions

A short log of technical decisions: what we chose, why, and when. Newest at the bottom.

| Date | Decision | Why |
|---|---|---|
| 2026-11-11 | **Phaser 4** for the client, code-first (not an editor-based engine) | It's a TypeScript framework you call from code, so it fits a .NET/Angular workflow. v4 is the current major version, and starting on v3 would mean migrating later. |
| 2026-11-11 | **Server is the referee.** All rules live in pure C# (`Tactics.Core`). | One source of truth, no client-side cheating, and the same inputs always give the same outputs. That makes replays and tests possible. |
| 2026-11-11 | **.NET 10 LTS** rather than .NET 11 | Both are supported until November 2028 (STS releases now get 24 months). The ecosystem is still catching up with 11: Npgsql's EF Core 11 provider is a release candidate. |
| 2026-11-11 | **Aspire** for local orchestration | One command starts the API and the client. The dashboard shows logs and traces, and URLs are injected, not hard-coded. It replaces docker-compose plus manual ports. |
| 2026-11-11 | **PostgreSQL** for persistence (from Step 12) | Free, open source, and has a first-class EF Core provider (Npgsql). It runs as a container in Aspire and Testcontainers. Same major version everywhere. |
| 2026-11-11 | Solution file is **`Tactics.slnx`** | The XML format is the .NET 10 default. It's short, readable and gives clean diffs. |
| 2026-11-11 | **`TreatWarningsAsErrors` on every project**, tests included, plus `EnforceCodeStyleInBuild` | The build check requires 0 warnings anyway. xUnit's analyzers catch real test mistakes, such as a missing cancellation token. |
| 2026-11-11 | AppHost health-checks the API on its **`http` endpoint** | With an untrusted HTTPS dev certificate (fresh machine, CI), the https probe failed. The API stayed "Unhealthy" and `WaitFor` blocked the client forever. |
| 2026-11-11 | The Vite proxy prefers **`API_HTTP`** over `API_HTTPS` | The browser → Vite → API hop stays on localhost, so plain http avoids dev-certificate problems. `secure: false` covers the https fallback. |
| 2026-11-11 | **TypeScript 7** (native compiler), with `strict: true` written explicitly | It's what create-vite 9 scaffolds. Writing `strict` explicitly documents the rule from `CLAUDE.md`, whatever the compiler's default. |
| 2026-11-11 | Vite `chunkSizeWarningLimit: 1500` | Phaser alone is ~1.4 MB minified (~360 kB gzipped). The limit sits just above it, so growth in our own code still warns. Splitting Phaser into its own cached chunk can wait for deploy polish. |
| 2026-11-11 | Tab title set at runtime (`document.title = GAME_TITLE`); `<title>` in `index.html` stays empty | It keeps the title in exactly one place in code. Trade-off: static HTML (link previews) sees no title. Revisit at Step 14. |
| 2026-11-11 | Network code lives outside Phaser (`client/src/net/`), with an injectable `fetch` | Vitest can test it without a canvas or a real server, much like constructor injection in .NET. |
