import { describe, expect, it } from "vitest";
import { planReconcile } from "./reconcile";

const unit = (id: string, hp = 10) => ({ id, hp });

describe("planReconcile", () => {
  it("creates a sprite for every unit on the first render", () => {
    const plan = planReconcile([], [unit("one-warrior"), unit("two-mage")]);

    expect(plan.create.map((u) => u.id)).toEqual(["one-warrior", "two-mage"]);
    expect(plan.update).toEqual([]);
    expect(plan.destroy).toEqual([]);
  });

  it("updates the sprites that already exist, even when the list comes in another order", () => {
    const plan = planReconcile(["one-warrior", "two-mage"], [unit("two-mage", 4), unit("one-warrior")]);

    expect(plan.create).toEqual([]);
    expect(plan.update).toEqual([unit("two-mage", 4), unit("one-warrior")]);
    expect(plan.destroy).toEqual([]);
  });

  it("destroys the sprite of a unit that left the board", () => {
    // two-mage was defeated: the server's view no longer lists it.
    const plan = planReconcile(["one-warrior", "two-mage", "two-archer"], [unit("one-warrior"), unit("two-archer")]);

    expect(plan.update.map((u) => u.id)).toEqual(["one-warrior", "two-archer"]);
    expect(plan.destroy).toEqual(["two-mage"]);
  });

  it("creates, updates and destroys in one pass", () => {
    const plan = planReconcile(new Set(["a", "b"]), [unit("b"), unit("c")]);

    expect(plan.create.map((u) => u.id)).toEqual(["c"]);
    expect(plan.update.map((u) => u.id)).toEqual(["b"]);
    expect(plan.destroy).toEqual(["a"]);
  });
});
