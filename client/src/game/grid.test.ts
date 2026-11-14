import { describe, expect, it } from "vitest";
import { layoutBoard, posKey, tileToWorld, worldToTile } from "./grid";

// The classic scenario: 8×8 tiles of 64 px (512×512) in the 960×640 base resolution.
const classic = layoutBoard({ width: 8, height: 8 }, 64, { width: 960, height: 640 });

describe("layoutBoard", () => {
  it("centres the board in the base resolution", () => {
    // (960 - 512) / 2 = 224 and (640 - 512) / 2 = 64.
    expect(classic).toEqual({ originX: 224, originY: 64, tileSize: 64, width: 8, height: 8 });
  });

  it("rounds the origin down to a whole pixel", () => {
    const layout = layoutBoard({ width: 1, height: 1 }, 50, { width: 101, height: 101 });
    // (101 - 50) / 2 = 25.5: tile edges would be blurred across two pixels.
    expect([layout.originX, layout.originY]).toEqual([25, 25]);
  });
});

describe("tileToWorld", () => {
  it("gives the centre of a tile", () => {
    expect(tileToWorld(classic, { x: 0, y: 0 })).toEqual({ x: 256, y: 96 });
    expect(tileToWorld(classic, { x: 7, y: 7 })).toEqual({ x: 704, y: 544 });
    expect(tileToWorld(classic, { x: 3, y: 6 })).toEqual({ x: 448, y: 480 }); // one-warrior's start
  });
});

describe("worldToTile", () => {
  it("finds every tile again from its centre", () => {
    for (let y = 0; y < 8; y++) {
      for (let x = 0; x < 8; x++) {
        const centre = tileToWorld(classic, { x, y });
        expect(worldToTile(classic, centre.x, centre.y)).toEqual({ x, y });
      }
    }
  });

  it("gives a tile its top-left pixel, but not the pixel where the next tile starts", () => {
    expect(worldToTile(classic, 224, 64)).toEqual({ x: 0, y: 0 });
    expect(worldToTile(classic, 287.9, 127.9)).toEqual({ x: 0, y: 0 });
    expect(worldToTile(classic, 288, 64)).toEqual({ x: 1, y: 0 });
    expect(worldToTile(classic, 224, 128)).toEqual({ x: 0, y: 1 });
  });

  it("returns null outside the board", () => {
    expect(worldToTile(classic, 223, 100)).toBeNull(); // 1 px left of column 0: Math.trunc would say column 0
    expect(worldToTile(classic, 300, 63)).toBeNull(); // above row 0
    expect(worldToTile(classic, 736, 100)).toBeNull(); // 224 + 512: the first pixel right of the board
    expect(worldToTile(classic, 300, 576)).toBeNull(); // 64 + 512: the first pixel below the board
    expect(worldToTile(classic, 10, 10)).toBeNull(); // far away, in the margin
  });
});

describe("posKey", () => {
  it("gives equal tiles equal keys, so a Set or Map can find them", () => {
    expect(posKey({ x: 3, y: 4 })).toBe("3,4");

    const obstacles = new Set([posKey({ x: 2, y: 3 }), posKey({ x: 5, y: 4 })]);
    expect(obstacles.has(posKey({ x: 2, y: 3 }))).toBe(true); // a new object literal, the same key
    expect(obstacles.has(posKey({ x: 3, y: 2 }))).toBe(false);
  });
});
