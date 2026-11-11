import { describe, expect, it } from "vitest";
import { fetchHelloMessage } from "./hello";

/** A fake `fetch` that always answers with the given status and JSON body. */
function fakeFetch(status: number, body: unknown): typeof fetch {
  return () => Promise.resolve(new Response(JSON.stringify(body), { status }));
}

describe("fetchHelloMessage", () => {
  it("returns the API's message", async () => {
    const message = await fetchHelloMessage(fakeFetch(200, { message: "API is alive" }));
    expect(message).toBe("API is alive");
  });

  it("throws on an HTTP error", async () => {
    await expect(fetchHelloMessage(fakeFetch(502, {}))).rejects.toThrow("HTTP 502");
  });

  it("throws when the body has the wrong shape", async () => {
    await expect(fetchHelloMessage(fakeFetch(200, { msg: 42 }))).rejects.toThrow("unexpected body");
  });
});
