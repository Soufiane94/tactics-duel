/**
 * What to do with the sprites on screen so that they match a new list of items (units): the core of
 * "render from state". Items are matched to sprites by id, never by position in the list, like Angular's `trackBy`.
 * A unit that moves keeps its sprite, and only a defeated unit's sprite goes away.
 */
export interface ReconcilePlan<T> {
  /** Items with no sprite yet: create one. On the first render, that's every item. */
  readonly create: readonly T[];
  /** Items that already have a sprite: update it from the item. */
  readonly update: readonly T[];
  /** Ids that have a sprite but no item any more: destroy the sprite (a defeated unit). */
  readonly destroy: readonly string[];
}

/**
 * Compares the ids that have a sprite with the items of the new state. It only plans; the scene does the work.
 * Pure, so Vitest checks it without Phaser. `create` and `update` keep the items' order.
 */
export function planReconcile<T extends { readonly id: string }>(
  existingIds: Iterable<string>,
  items: readonly T[],
): ReconcilePlan<T> {
  const existing = new Set(existingIds);
  const create: T[] = [];
  const update: T[] = [];
  for (const item of items) {
    (existing.has(item.id) ? update : create).push(item);
  }

  const wanted = new Set(items.map((item) => item.id));
  const destroy = [...existing].filter((id) => !wanted.has(id));
  return { create, update, destroy };
}
