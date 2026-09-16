// tools.js

import { State, TILE_SIZE, BLOCK_DATA } from "./state.js";

import {
  pixelToCell,
  normalizeRect,
  rectContainsCell,
} from "./grid.js";

const hitBlock = (c) => {
  for (let i = State.stage.Blocks.length - 1; i >= 0; i--) {
    const d = BLOCK_DATA[State.stage.Blocks[i]];
    const pos = State.stage.BlockPositions[i];

    if (
      d.t.some(
        ([row, col]) =>
          pos.x + row === c.x &&
          pos.y + col === c.y,
      )
    ) {
      return i;
    }
  }

  return -1;
};

const toggle = (key, i) => {
  const a = State.stage[key];
  const p = a.indexOf(i);

  p < 0 ? a.push(i) : a.splice(p, 1);
};

export function attachTools(
  canvas,
  { onPreviewChange, onHoverChange, onCommit },
) {
  let dragging = false;
  let start = null;

  // Canvas pixel → project cell(row, col)
  const cell = (e) => {
    const r = canvas.getBoundingClientRect();

    return pixelToCell(
      e.clientX - r.left,
      e.clientY - r.top,
    );
  };

  // Canvas point → project (row, col)
  //
  // Input / Output are positioned on grid points,
  // so round() is intentional.
  const point = (e) => {
    const r = canvas.getBoundingClientRect();

    return {
      x: Math.round(
        (e.clientY - r.top) / TILE_SIZE,
      ), // row

      y: Math.round(
        (e.clientX - r.left) / TILE_SIZE,
      ), // col
    };
  };

  // Find nearest horizontal / vertical edge.
  //
  // Returned coordinates are also (row, col).
  const edge = (e) => {
    const r = canvas.getBoundingClientRect();

    const col =
      (e.clientX - r.left) / TILE_SIZE;

    const row =
      (e.clientY - r.top) / TILE_SIZE;

    const dc = Math.abs(col - Math.round(col));
    const dr = Math.abs(row - Math.round(row));

    // Closer to a vertical grid line → V edge
    if (dc < dr) {
      return {
        kind: "v",
        x: Math.floor(row),
        y: Math.round(col),
      };
    }

    // Closer to a horizontal grid line → H edge
    return {
      kind: "h",
      x: Math.round(row),
      y: Math.floor(col),
    };
  };

  // x = row → rows
  // y = col → cols
  const clamp = (c) => ({
    x: Math.max(
      0,
      Math.min(
        c.x,
        State.workspace.rows - 1,
      ),
    ),

    y: Math.max(
      0,
      Math.min(
        c.y,
        State.workspace.cols - 1,
      ),
    ),
  });

  canvas.addEventListener("pointerdown", (e) => {
    const c = clamp(cell(e));
    const t = State.tool;

    // -------------------------
    // Block
    // -------------------------

    if (t === "block-paint") {
      State.stage.Blocks.push(
        State.selectedBlock,
      );

      State.stage.BlockPositions.push(c);

      onCommit();
      return;
    }

    // -------------------------
    // Input / Output
    // -------------------------

    if (
      t === "input-paint" ||
      t === "output-paint"
    ) {
      const p = point(e);

      const list =
        t === "input-paint"
          ? State.stage.Inputs
          : State.stage.Outputs;

      list.push({
        pos: p,
        expr: State.selectedIO.expr,
      });

      onCommit();
      return;
    }

    // -------------------------
    // Barrier
    // -------------------------

    if (
      t === "hbarrier-paint" ||
      t === "vbarrier-paint"
    ) {
      const q = edge(e);

      const kind =
        t === "hbarrier-paint"
          ? "h"
          : "v";

      const key =
        t === "hbarrier-paint"
          ? "HBarriers"
          : "VBarriers";

      if (
        q.kind === kind &&
        !State.stage[key].some(
          (v) =>
            v.x === q.x &&
            v.y === q.y,
        )
      ) {
        State.stage[key].push({
          x: q.x,
          y: q.y,
        });
      }

      onCommit();
      return;
    }

    // -------------------------
    // Block transform
    // -------------------------

    const map = {
      "rotate-cw": "RotateCWIndex",
      "rotate-ccw": "RotateCCWIndex",
      "flip-x": "FlipXIndex",
      "flip-y": "FlipYIndex",
    };

    if (map[t]) {
      const bi = hitBlock(c);

      if (bi >= 0) {
        toggle(map[t], bi);
      }

      onCommit();
      return;
    }

    // -------------------------
    // Eraser
    // -------------------------

    if (t === "eraser") {
      const bi = hitBlock(c);

      // Block
      if (bi >= 0) {
        State.stage.Blocks.splice(
          bi,
          1,
        );

        State.stage.BlockPositions.splice(
          bi,
          1,
        );

        for (const k of [
          "RotateCWIndex",
          "RotateCCWIndex",
          "FlipXIndex",
          "FlipYIndex",
        ]) {
          State.stage[k] = State.stage[k]
            .filter((i) => i !== bi)
            .map((i) =>
              i > bi ? i - 1 : i,
            );
        }

        onCommit();
        return;
      }

      // Input / Output
      const p = point(e);

      for (const k of [
        "Inputs",
        "Outputs",
      ]) {
        State.stage[k] =
          State.stage[k].filter(
            (v) =>
              v.pos.x !== p.x ||
              v.pos.y !== p.y,
          );
      }

      // Barrier
      const q = edge(e);

      for (const k of [
        "HBarriers",
        "VBarriers",
      ]) {
        State.stage[k] =
          State.stage[k].filter(
            (v) =>
              v.x !== q.x ||
              v.y !== q.y,
          );
      }

      onCommit();
      return;
    }

    // -------------------------
    // Circuit erase
    // -------------------------

    if (t === "circuit-erase") {
      for (
        let i =
          State.stage.Circuits.length - 1;
        i >= 0;
        i--
      ) {
        if (
          rectContainsCell(
            State.stage.Circuits[i],
            c.x,
            c.y,
          )
        ) {
          State.stage.Circuits.splice(
            i,
            1,
          );

          onCommit();
          break;
        }
      }

      return;
    }

    // -------------------------
    // Drag start
    // -------------------------

    dragging = true;
    start = c;

    canvas.setPointerCapture(
      e.pointerId,
    );

    if (t === "bg-paint") {
      resize(c);
    } else if (t === "circuit-paint") {
      onPreviewChange({
        rect: normalizeRect(
          c.x,
          c.y,
          c.x,
          c.y,
        ),
      });
    }
  });

  // -------------------------
  // Drag
  // -------------------------

  canvas.addEventListener("pointermove", (e) => {
    const c = clamp(cell(e));

    onHoverChange(c);

    if (!dragging) return;

    if (State.tool === "bg-paint") {
      resize(c);
    }

    if (State.tool === "circuit-paint") {
      onPreviewChange({
        rect: normalizeRect(
          start.x,
          start.y,
          c.x,
          c.y,
        ),
      });
    }
  });

  // -------------------------
  // Drag end
  // -------------------------

  window.addEventListener("pointerup", (e) => {
    if (!dragging) return;

    dragging = false;

    if (State.tool === "circuit-paint") {
      const c = clamp(cell(e));

      const r = normalizeRect(
        start.x,
        start.y,
        c.x,
        c.y,
      );

      State.stage.Circuits.push({
        width: r.width,
        height: r.height,

        pos: {
          x: r.x, // row
          y: r.y, // col
        },
      });

      onPreviewChange(null);
    }

    onCommit();
    start = null;
  });

  // -------------------------
  // Background resize
  // -------------------------

  function resize(c) {
    // c.x = row → height
    // c.y = col → width

    State.stage.BgWidth = c.y + 1;
    State.stage.BgHeight = c.x + 1;

    onCommit();
  }
}

export function clearBackground(f) {
  State.stage.BgWidth = 0;
  State.stage.BgHeight = 0;

  f();
}