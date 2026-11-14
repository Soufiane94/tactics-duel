import type {
  ApiErrorCode,
  CreateGameResponse,
  GameView,
  JoinGameRequest,
  JoinGameResponse,
  ProblemDetails,
  SubmitActionRequest,
  SubmitActionResponse,
} from "../game/types";

/** If the API doesn't answer (for example, it's down and the dev proxy keeps waiting), give up after this long. */
const REQUEST_TIMEOUT_MS = 10_000;

/**
 * The server refused a request: any HTTP status outside 200–299. It carries the server's ProblemDetails.
 * Branch on `status` and `code` (422 + "NotYourTurn"), and show `message` (the server's `detail`) to the player.
 */
export class ApiError extends Error {
  readonly status: number;
  readonly problem: ProblemDetails;

  constructor(problem: ProblemDetails) {
    super(problem.detail ?? problem.title);
    this.name = "ApiError";
    this.status = problem.status;
    this.problem = problem;
  }

  /** The stable code of a 403, 409 or 422, such as "SequenceConflict". Undefined for 400, 401 and 404. */
  get code(): ApiErrorCode | undefined {
    return this.problem.code;
  }
}

/** No answer at all: no network, nothing listening, or no reply before the timeout. The original error is `cause`. */
export class ApiUnreachableError extends Error {
  constructor(cause: unknown) {
    const timedOut = cause instanceof DOMException && cause.name === "TimeoutError";
    super(timedOut ? "The server didn't answer in time." : "Can't reach the server.", { cause });
    this.name = "ApiUnreachableError";
  }
}

/** A sentence for the player, whatever was thrown. */
export function errorMessage(error: unknown): string {
  return error instanceof Error ? error.message : String(error);
}

interface RequestOptions {
  /** The player token, sent as `Authorization: Bearer <token>`. */
  readonly token?: string;
  /** Sent as JSON. */
  readonly body?: unknown;
}

/**
 * The game API over HTTP, typed with the mirrors in `game/types.ts`. It's the only client code that knows URLs and
 * headers. URLs are relative: in dev, the Vite server proxies `/api` to the API, so the browser talks to one origin
 * (no CORS). No Phaser import: Vitest tests it with a fake `fetch`, like a fake `HttpMessageHandler` in .NET.
 */
export class ApiClient {
  private readonly fetchFn: typeof fetch;

  /**
   * The default wraps the global `fetch` in an arrow function on purpose: called as `this.fetchFn(...)`, the
   * browser's own `fetch` would receive the ApiClient as `this`, and browsers then throw "Illegal invocation".
   */
  constructor(fetchFn: typeof fetch = (input, init) => fetch(input, init)) {
    this.fetchFn = fetchFn;
  }

  /** `POST /api/games`: you create a game and hold seat one. The answer has your token and the invite code. */
  createGame(): Promise<CreateGameResponse> {
    return this.send("POST", "/api/games");
  }

  /** `POST /api/games/{gameId}/join`: you take seat two with the invite code. */
  joinGame(gameId: string, inviteCode: string): Promise<JoinGameResponse> {
    const body: JoinGameRequest = { inviteCode };
    return this.send("POST", `${gamePath(gameId)}/join`, { body });
  }

  /** `GET /api/games/{gameId}`: the game as the token's player sees it. */
  getView(gameId: string, token: string): Promise<GameView> {
    return this.send("GET", gamePath(gameId), { token });
  }

  /** `POST /api/games/{gameId}/actions`: plays one action. The answer has what happened (events) and the new view. */
  submitAction(gameId: string, token: string, request: SubmitActionRequest): Promise<SubmitActionResponse> {
    return this.send("POST", `${gamePath(gameId)}/actions`, { token, body: request });
  }

  private async send<T>(method: "GET" | "POST", path: string, options: RequestOptions = {}): Promise<T> {
    const headers = new Headers({ Accept: "application/json" });
    if (options.token !== undefined) {
      headers.set("Authorization", `Bearer ${options.token}`);
    }

    let body: string | undefined;
    if (options.body !== undefined) {
      headers.set("Content-Type", "application/json");
      body = JSON.stringify(options.body);
    }

    let response: Response;
    try {
      response = await this.fetchFn(path, { method, headers, body, signal: AbortSignal.timeout(REQUEST_TIMEOUT_MS) });
    } catch (error) {
      throw new ApiUnreachableError(error);
    }

    if (!response.ok) {
      throw new ApiError(await readProblem(response));
    }

    // A success body is trusted, not checked field by field: the API is ours, and both sides pin its JSON shape
    // (the API's contract tests and `types.test.ts`). Error bodies are read defensively instead: see readProblem.
    return (await response.json()) as T;
  }
}

function gamePath(gameId: string): string {
  return `/api/games/${encodeURIComponent(gameId)}`;
}

/**
 * The error body as ProblemDetails. Not every error comes from our API: when it's down, the dev proxy answers with
 * plain text, and a production proxy may send an HTML page. Those get a generic problem with the real status.
 */
async function readProblem(response: Response): Promise<ProblemDetails> {
  const generic: ProblemDetails = { title: response.statusText || `HTTP ${response.status}`, status: response.status };

  let body: unknown;
  try {
    body = await response.json();
  } catch {
    return generic;
  }
  return isProblemDetails(body) ? { ...body, status: response.status } : generic;
}

function isProblemDetails(value: unknown): value is ProblemDetails {
  return typeof value === "object" && value !== null && "title" in value && typeof value.title === "string";
}
