// grid.js

import { TILE_SIZE } from "./state.js";

// Project cell coordinate:
// x = row
// y = col
//
// Canvas pixel coordinate:
// pixel x = col
// pixel y = row

export function pixelToCell(px, py) {
  return {
    x: Math.floor(py / TILE_SIZE), // row
    y: Math.floor(px / TILE_SIZE), // col
  };
}

export function cellToPixel(row, col) {
  return {
    x: col * TILE_SIZE,
    y: row * TILE_SIZE,
  };
}

// Builds a normalized rectangle from two arbitrary
// corner cells.
//
// Returned rect:
// x      = starting row
// y      = starting col
// width  = number of columns (horizontal)
// height = number of rows    (vertical)
export function normalizeRect(row0, col0, row1, col1) {
  const minRow = Math.min(row0, row1);
  const minCol = Math.min(col0, col1);
  const maxRow = Math.max(row0, row1);
  const maxCol = Math.max(col0, col1);

  return {
    x: minRow,
    y: minCol,
    width: maxCol - minCol + 1,
    height: maxRow - minRow + 1,
  };
}

export function rectContainsCell(rect, row, col) {
  const startRow = rect.pos ? rect.pos.x : rect.x;
  const startCol = rect.pos ? rect.pos.y : rect.y;

  return (
    row >= startRow &&
    row < startRow + rect.height &&
    col >= startCol &&
    col < startCol + rect.width
  );
}