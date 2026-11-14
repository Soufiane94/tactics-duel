import { describe, expect, it } from "vitest";
import type { SubmitActionRequest } from "../game/types";
import { ApiClient, ApiError, ApiUnreachableError, errorMessage } from "./ApiClient";

const gameId = "0b8f6e2a-61c4-7d0e-9a35-2f1c8d4e5b6a";

/** What the fake `fetch` received. */
interface Sent {
  url: string;
  method: string;
  headers: Headers;
  body: unknown;
}

/** A fake `fetch` that records each request and answers with the given status and body (JSON, or raw text). */
function fakeFetch(status: number, body: unknown, sent: Sent[] = [], statusText = ""): typeof fetch {
  return (input, init) => {
    sent.push({
      url: String(input),
      method: init?.method ?? "GET",
      headers: new Headers(init?.headers),
      body: typeof init?.body === "string" ? JSON.parse(init.body) : undefined,
    });
    const text = typeof body === "string" ? body : JSON.stringify(body);
    return Promise.resolve(new Response(text, { status, statusText }));
  };
}

/** A fake `fetch` that never gets an answer: it rejects like the browser's does. */
function failingFetch(error: unknown): typeof fetch {
  return () => Promise.reject(error);
}

describe("ApiClient requests", () => {
  it("creates a game without a token", async () => {
    const sent: Sent[] = [];
    const created = { gameId, side: "one", playerToken: "token-one", inviteCode: "K7QF3MZP", view: {} };
    const api = new ApiClient(fakeFetch(201, created, sent));

    expect(await api.createGame()).toEqual(created);
    expect(sent[0]).toMatchObject({ url: "/api/games", method: "POST", body: undefined });
    expect(sent[0]?.headers.has("Authorization")).toBe(false);
  });

  it("joins with the invite code as JSON", async () => {
    const sent: Sent[] = [];
    const api = new ApiClient(fakeFetch(200, { gameId, side: "two", playerToken: "token-two", view: {} }, sent));

    await api.joinGame(gameId, "K7QF3MZP");

    expect(sent[0]).toMatchObject({ url: `/api/games/${gameId}/join`, method: "POST", body: { inviteCode: "K7QF3MZP" } });
    expect(sent[0]?.headers.get("Content-Type")).toBe("application/json");
  });

  it("sends the player token as a Bearer token", async () => {
    const sent: Sent[] = [];
    const api = new ApiClient(fakeFetch(200, { gameId }, sent));

    await api.getView(gameId, "token-one");

    expect(sent[0]).toMatchObject({ url: `/api/games/${gameId}`, method: "GET" });
    expect(sent[0]?.headers.get("Authorization")).toBe("Bearer token-one");
  });

  it("posts an action with its expected sequence", async () => {
    const sent: Sent[] = [];
    const request: SubmitActionRequest = { expectedSequence: 0, action: { type: "endTurn" } };
    const api = new ApiClient(fakeFetch(200, { sequence: 1, events: [], view: {} }, sent));

    const response = await api.submitAction(gameId, "token-one", request);

    expect(response.sequence).toBe(1);
    expect(sent[0]).toMatchObject({ url: `/api/games/${gameId}/actions`, method: "POST", body: request });
    expect(sent[0]?.headers.get("Authorization")).toBe("Bearer token-one");
  });
});

describe("ApiClient errors", () => {
  it("turns a rule violation into an ApiError with its code", async () => {
    const problem = { title: "Rule violation", status: 422, code: "NotYourTurn", detail: "It's Player Two's turn." };
    const api = new ApiClient(fakeFetch(422, problem));

    const error = await api.getView(gameId, "token-one").catch((e: unknown) => e);

    expect(error).toBeInstanceOf(ApiError);
    expect(error).toMatchObject({ status: 422, code: "NotYourTurn", message: "It's Player Two's turn." });
  });

  it("keeps errors without a code, such as a 401", async () => {
    const problem = { title: "Not a player of this game", status: 401, detail: "This token isn't a player token of this game." };
    const api = new ApiClient(fakeFetch(401, problem));

    await expect(api.getView(gameId, "stolen")).rejects.toMatchObject({
      status: 401,
      code: undefined,
      message: "This token isn't a player token of this game.",
    });
  });

  it("copes with an error body that isn't ProblemDetails, like the dev proxy's when the API is down", async () => {
    const api = new ApiClient(fakeFetch(502, "Bad Gateway: connect ECONNREFUSED", [], "Bad Gateway"));

    await expect(api.createGame()).rejects.toMatchObject({ status: 502, message: "Bad Gateway" });
  });

  it("reports a request that got no answer at all", async () => {
    const offline = new ApiClient(failingFetch(new TypeError("Failed to fetch")));
    const slow = new ApiClient(failingFetch(new DOMException("signal timed out", "TimeoutError")));

    const error = await offline.createGame().catch((e: unknown) => e);
    expect(error).toBeInstanceOf(ApiUnreachableError);
    expect(error).toMatchObject({ cause: expect.any(TypeError) }); // the original error stays available
    expect(errorMessage(error)).toBe("Can't reach the server.");

    await expect(slow.createGame()).rejects.toThrow("The server didn't answer in time.");
  });
});
