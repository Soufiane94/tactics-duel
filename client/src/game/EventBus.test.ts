import { describe, expect, it } from "vitest";
import { EventBus } from "./EventBus";

/** A small message list for the tests: names, and the arguments each one carries. */
interface TestMessages {
  greeted: [name: string];
  scored: [points: number, by: string];
  pinged: [];
}

describe("EventBus", () => {
  it("delivers a message's arguments to every listener of that name, and only of that name", () => {
    const bus = new EventBus<TestMessages>();
    const heard: string[] = [];
    bus.on("scored", (points, by) => heard.push(`first: ${by} +${points}`));
    bus.on("scored", (points, by) => heard.push(`second: ${by} +${points}`));
    bus.on("greeted", (name) => heard.push(`hello ${name}`));

    bus.emit("scored", 3, "archer");

    expect(heard).toEqual(["first: archer +3", "second: archer +3"]);
  });

  it("says whether anyone was listening", () => {
    const bus = new EventBus<TestMessages>();
    expect(bus.emit("pinged")).toBe(false);

    bus.on("pinged", () => {});
    expect(bus.emit("pinged")).toBe(true);
  });

  it("stops delivering once a listener unsubscribes, and unsubscribing twice is harmless", () => {
    const bus = new EventBus<TestMessages>();
    const heard: string[] = [];
    const stop = bus.on("greeted", (name) => heard.push(name));

    bus.emit("greeted", "Ada");
    stop();
    stop();

    expect(bus.emit("greeted", "Grace")).toBe(false);
    expect(heard).toEqual(["Ada"]);
  });

  it("lets a listener unsubscribe while a message is delivered, without the others missing it", () => {
    const bus = new EventBus<TestMessages>();
    const heard: string[] = [];
    const stopFirst = bus.on("pinged", () => {
      heard.push("first");
      stopFirst();
    });
    bus.on("pinged", () => heard.push("second"));

    bus.emit("pinged");
    bus.emit("pinged");

    expect(heard).toEqual(["first", "second", "second"]);
  });

  it("forgets every listener on clear", () => {
    const bus = new EventBus<TestMessages>();
    bus.on("greeted", () => {});
    bus.on("pinged", () => {});

    bus.clear();

    expect(bus.emit("greeted", "Ada")).toBe(false);
    expect(bus.emit("pinged")).toBe(false);
  });

  it("checks names and arguments at compile time: `npm run typecheck` fails if a line below compiles", () => {
    const bus = new EventBus<TestMessages>();

    // @ts-expect-error -- "greeted" carries a name (a string), not a number.
    bus.emit("greeted", 42);
    // @ts-expect-error -- "scored" needs two arguments.
    bus.emit("scored", 3);
    // @ts-expect-error -- there's no message called "greet".
    bus.on("greet", () => {});

    expect(bus.emit("pinged")).toBe(false);
  });
});
