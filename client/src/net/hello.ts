/** Body of `GET /api/hello` (mirrors `Tactics.Api.Contracts.HelloResponse`). */
export interface HelloResponse {
  message: string;
}

/** If the API is down, the dev proxy can hang; give up after this long and say so. */
const HELLO_TIMEOUT_MS = 5000;

/**
 * Asks the API for its smoke-test message.
 * The URL is relative: in dev, the Vite server proxies `/api` to the API, so the browser
 * only ever talks to one origin (no CORS). `fetchFn` is injectable so tests can fake the network.
 * No Phaser import here on purpose: network code stays testable without a canvas.
 */
export async function fetchHelloMessage(fetchFn: typeof fetch = fetch): Promise<string> {
  const response = await fetchFn("/api/hello", { signal: AbortSignal.timeout(HELLO_TIMEOUT_MS) });
  if (!response.ok) {
    throw new Error(`GET /api/hello failed with HTTP ${response.status}`);
  }

  const body: unknown = await response.json();
  if (!isHelloResponse(body)) {
    throw new Error("GET /api/hello returned an unexpected body");
  }
  return body.message;
}

function isHelloResponse(value: unknown): value is HelloResponse {
  return (
    typeof value === "object" &&
    value !== null &&
    "message" in value &&
    typeof value.message === "string"
  );
}
