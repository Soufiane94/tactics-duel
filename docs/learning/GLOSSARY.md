# Glossary

Every new term, in alphabetical order, with the step where it first appears.

- **Aspire AppHost** — a C# program that describes the dev environment (services, apps, databases) and runs it with `aspire run`. *(Step 0)*
- **Aspire dashboard** — a web page that shows every resource's state, logs, traces and environment variables. *(Step 0)*
- **Base resolution** — the fixed logical size the game draws in (here 960×640), whatever the window size. *(Step 0)*
- **Canvas 2D renderer** — draws with the browser's CPU-based 2D API. Simple, but deprecated in Phaser 4. *(Step 0)*
- **Central Package Management (CPM)** — every NuGet version lives once in `Directory.Packages.props`; projects list packages without versions. *(Step 0)*
- **Dev proxy** — the Vite server forwards some paths (`/api`) to another server, so the browser sees one origin. *(Step 0)*
- **Frame** — one redraw of the whole canvas. At 60 FPS, a frame lasts about 16.7 ms. *(Step 0)*
- **Game engine** — an editor-based tool (Unity, Godot) that hosts your code inside its own app. *(Step 0)*
- **Game framework** — a code library (Phaser) that you call from your own program. *(Step 0)*
- **Game loop** — the endless cycle of update → render, run once per frame. *(Step 0)*
- **Health check** — an endpoint (`/health`) that says whether a service is ready. Aspire polls it. *(Step 0)*
- **HMR (Hot Module Replacement)** — Vite swaps changed modules in the running page without a full reload. *(Step 0)*
- **Letterboxing** — the bars around the canvas when the window's shape differs from the game's aspect ratio. *(Step 0)*
- **Microsoft.Testing.Platform (MTP)** — the new .NET test runner. Test projects are executables. *(Step 0)*
- **Origin (game object)** — the anchor point of an object. `0.5` means it's positioned by its centre, `0` by its top-left corner. *(Step 0)*
- **Renderer** — the part of Phaser that turns game objects into pixels on the canvas. *(Step 0)*
- **Resource (Aspire)** — one thing the AppHost runs: a project, an executable, a container. *(Step 0)*
- **Scale mode (`FIT`)** — how Phaser sizes the canvas to the window. `FIT` keeps the aspect ratio. *(Step 0)*
- **Scene** — one self-contained screen of the game, with its own objects and lifecycle. *(Step 0)*
- **Scene key** — a scene's unique name (`"BootScene"`), used to start or stop it. *(Step 0)*
- **Scene lifecycle** — `init → preload → create`, then `update` every frame. *(Step 0)*
- **Service defaults** — shared Aspire setup for every service: telemetry, health checks, resilience, service discovery. *(Step 0)*
- **Smoke test** — a tiny test that proves the wiring works end to end, not the logic. *(Step 0)*
- **Vite** — the dev server and bundler for the client: serves TypeScript on the fly, then builds `dist/` for production. *(Step 0)*
- **WebGL renderer** — draws with the GPU through shaders. Fast, and Phaser 4's main renderer. *(Step 0)*
