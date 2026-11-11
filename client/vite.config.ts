import { defineConfig } from "vitest/config";

/**
 * Where the dev server forwards `/api` requests.
 * - Under Aspire (`aspire run`): `.WithReference(api)` injects the API's URL as API_HTTP / API_HTTPS.
 * - Without Aspire (`npm run dev` + `dotnet run` on the API): the "http" profile
 *   in src/Tactics.Api/Properties/launchSettings.json.
 */
const apiUrl = process.env.API_HTTP ?? process.env.API_HTTPS ?? "http://localhost:5257";

/**
 * Aspire starts Vite with `--port <n>` (and sets PORT). Without Aspire, Vite uses its default 5173.
 */
const runningUnderAspire = process.env.PORT !== undefined;

export default defineConfig({
  server: {
    // Under Aspire, fail fast if that port is busy, instead of silently moving to a port
    // the dashboard doesn't know about.
    strictPort: runningUnderAspire,
    proxy: {
      // The browser calls /api/... on the Vite origin; Vite forwards it to the API (no CORS needed).
      // `secure: false` accepts the local ASP.NET Core dev certificate if the URL is https.
      "/api": { target: apiUrl, changeOrigin: true, secure: false },
      // "/hubs" (SignalR, with ws: true) arrives in Step 11.
    },
  },
  build: {
    // Phaser alone is ~1.4 MB minified (~360 kB gzipped), above Vite's 500 kB default warning.
    // The limit sits just above it, so a real size regression in our own code still warns.
    chunkSizeWarningLimit: 1500,
  },
  test: {
    include: ["src/**/*.test.ts"],
  },
});
