// renderer.js

// --------------------------------------------------------
// imports
// --------------------------------------------------------
import {
    TILE_SIZE,
    BG_COLS,
    BG_ROWS,
    CIRCUIT_COLS,
    CIRCUIT_ROWS,
    BLOCK_DATA
} from "./state.js";
// --------------------------------------------------------

export function resizeCanvas(canvas, width, height) {
    canvas.width = width * TILE_SIZE;
    canvas.height = height * TILE_SIZE;
}

export function render(ctx, canvas, state, assets, ui) {
    ctx.clearRect(0, 0, canvas.width, canvas.height);
    renderGrid(ctx, canvas);
    renderBg(ctx, state, assets);
    renderCircuits(ctx, state, assets);
    renderBarriers(ctx, state);
    renderTileBarriers(ctx, state, assets);
    renderBlocks(ctx, state, assets);
    renderFixedBlocks(ctx, state, assets);
    renderIos(ctx, state);
    if (ui.dragPreview) renderGhost(ctx, ui.dragPreview.rect);
}

function renderGrid(ctx, canvas) {
    ctx.strokeStyle = "#ddd";
    for (let x = 0; x <= canvas.width; x += TILE_SIZE) {
        ctx.beginPath();
        ctx.moveTo(x + 0.5, 0);
        ctx.lineTo(x + 0.5, canvas.height);
        ctx.stroke();
    }
    for (let y = 0; y <= canvas.height; y += TILE_SIZE) {
        ctx.beginPath();
        ctx.moveTo(0, y + 0.5);
        ctx.lineTo(canvas.width, y + 0.5);
        ctx.stroke();
    }
}

function renderBg(ctx, state, assets) {
    if (!assets.bg?.ok) return;

    const img = assets.bg.img;
    const tileWidth = img.width / BG_COLS;
    const tileHeight = img.height / BG_ROWS;

    for (let row = 0; row < state.BgHeight; row++) {
        for (let col = 0; col < state.BgWidth; col++) {
        const tileRow = row % BG_ROWS;
        const tileCol = col % BG_COLS;

        ctx.drawImage(
            img,

            // source
            tileCol * tileWidth,
            tileRow * tileHeight,
            tileWidth,
            tileHeight,

            // destination
            col * TILE_SIZE,
            row * TILE_SIZE,
            TILE_SIZE,
            TILE_SIZE,
        );
        }
    }
}

function renderCircuits(ctx, state, assets) {
    if (!assets.circuit?.ok) return;

    const img = assets.circuit.img;
    const tileWidth = img.width / CIRCUIT_COLS;
    const tileHeight = img.height / CIRCUIT_ROWS;

    state.Circuits.forEach(
        (circuit) => {
            for (let row = -1; row < circuit.height + 1; row++) {
                for (let col = -1; col < circuit.width + 1; col++) {
                    const tileRow = row == -1 ? 0 : row == circuit.height ? 4 : 2;
                    const tileCol = col == -1 ? 0 : col == circuit.width ? 4 : 2;

                    ctx.drawImage(
                        img,

                        // source
                        tileCol * tileWidth,
                        tileRow * tileHeight,
                        tileWidth,
                        tileHeight,

                        // destination
                        (circuit.pos.y + col) * TILE_SIZE,
                        (circuit.pos.x + row) * TILE_SIZE,
                        TILE_SIZE,
                        TILE_SIZE
                    );
                }
            }
        }
    );
}

function renderBarriers(ctx, state) {
    ctx.strokeStyle = "#111";
    ctx.lineWidth = 4;

    for (let v of state.HBarriers) {
        ctx.beginPath();
        ctx.moveTo(v.y * TILE_SIZE, v.x * TILE_SIZE);
        ctx.lineTo((v.y + 1) * TILE_SIZE, v.x * TILE_SIZE);
        ctx.stroke();
    }
    for (let v of state.VBarriers) {
        ctx.beginPath();
        ctx.moveTo(v.y * TILE_SIZE, v.x * TILE_SIZE);
        ctx.lineTo(v.y * TILE_SIZE, (v.x + 1) * TILE_SIZE);
        ctx.stroke();
    }
    ctx.lineWidth = 1;
}

function renderTileBarriers(ctx, state, assets) {
    const variants = assets.tileBarriers || [];
    if (variants.length === 0) return;

    (state.TileBarriers || []).forEach((pos) => {
        const variantIndex = Math.abs(pos.x * 31 + pos.y * 17) % variants.length;
        const variant = variants[variantIndex];
        if (!variant?.ok) return;

        ctx.drawImage(
            variant.img,
            pos.y * TILE_SIZE,
            pos.x * TILE_SIZE,
            TILE_SIZE,
            TILE_SIZE,
        );
    });
}

/* RotateCWIndex: [],
		RotateCCWIndex: [],
		FlipXIndex: [],
		FlipYIndex: [], */

function renderBlocks(ctx, state, assets) {
    state.BlockPositions.forEach(
        (pos, i) => {
            let id = state.Blocks[i],
            block = BLOCK_DATA[id],
            row = pos.x * TILE_SIZE,
            col = pos.y * TILE_SIZE,
            blockImg = assets.blocks[id];
            if (blockImg?.ok) {
                ctx.drawImage(
                    blockImg.img, 
                    
                    col, 
                    row, 
                    block.w * TILE_SIZE, 
                    block.h * TILE_SIZE
                );
            }
            
            // block은 단위 길이가 400px
            // tag는 단위 길이가 208px
            // 즉 가로, 세로는 TILE_SIZE * (208/400)이 된다
            let tagWidth = TILE_SIZE * (150/400);
            let blockCenterX = col + block.w * TILE_SIZE / 2;
            let blockCenterY = row + block.h * TILE_SIZE / 2;
            let tagX = blockCenterX - tagWidth/2 + block.tagPos.x*TILE_SIZE;
            let tagY = blockCenterY - tagWidth/2 + block.tagPos.y*TILE_SIZE;
            
            let tagImg = null
            if (state.RotateCWIndex.includes(i)) {
                tagImg = assets.tag.rotateCW;
            }
            else if (state.RotateCCWIndex.includes(i)) {
                tagImg = assets.tag.rotateCCW;
            }
            else if (state.FlipXIndex.includes(i)) {
                tagImg = assets.tag.flipX;
            }
            else if (state.FlipYIndex.includes(i)) {
                tagImg = assets.tag.flipY;
            }
            
            if (tagImg != null && tagImg?.ok) {
                ctx.drawImage(
                    tagImg.img,
                    tagX, tagY, tagWidth, tagWidth
                );
            }
        }
    );
}

const FIXED_BLOCK_TINT = "#575757";
const fixedTintCache = new WeakMap();

function getTintedBlockImage(image) {
    let tinted = fixedTintCache.get(image);
    if (tinted) return tinted;

    tinted = document.createElement("canvas");
    tinted.width = image.naturalWidth || image.width;
    tinted.height = image.naturalHeight || image.height;
    const tintCtx = tinted.getContext("2d");
    tintCtx.drawImage(image, 0, 0);
    tintCtx.globalCompositeOperation = "multiply";
    tintCtx.fillStyle = FIXED_BLOCK_TINT;
    tintCtx.fillRect(0, 0, tinted.width, tinted.height);
    tintCtx.globalCompositeOperation = "destination-in";
    tintCtx.drawImage(image, 0, 0);
    fixedTintCache.set(image, tinted);
    return tinted;
}

function renderFixedBlocks(ctx, state, assets) {
    (state.FixedBlocks || []).forEach((fixedBlock) => {
        const id = Number(fixedBlock.id ?? fixedBlock.ID);
        const pos = fixedBlock.pos;
        const block = BLOCK_DATA[id];
        const blockImg = assets.blocks[id];
        if (!block || !pos) return;

        const row = pos.x * TILE_SIZE;
        const col = pos.y * TILE_SIZE;
        const width = block.w * TILE_SIZE;
        const height = block.h * TILE_SIZE;

        if (blockImg?.ok) {
            ctx.drawImage(
                getTintedBlockImage(blockImg.img),
                col,
                row,
                width,
                height,
            );
        }

        ctx.save();
        ctx.fillStyle = "#242424";
        ctx.font = "10px monospace";
        ctx.textAlign = "center";
        ctx.textBaseline = "bottom";
        ctx.fillText(
            "fixed",
            col + width / 2,
            Math.max(10, row - 2),
        );
        ctx.restore();
    });
}

function renderIos(ctx, state) {
    ctx.font = "15px monospace";
    ctx.textAlign = "center";
    for (const [list, label, shape] of [[state.Inputs, "I", "circle"], [state.Outputs, "O", "square"]]) {
        list.forEach(
            (v) => {
                let x = v.pos.x * TILE_SIZE,
                    y = v.pos.y * TILE_SIZE;
                if (shape === "circle") {
                    ctx.fillStyle = "#54DCE3";
                    ctx.beginPath();
                    ctx.arc(y, x, 7, 0, Math.PI * 2);
                    ctx.fill();
                } 
                else {
                    ctx.fillStyle = "#B8B8B8";
                    ctx.fillRect(y - 6, x - 6, 12, 12);
                }
                ctx.fillStyle = "#111";
                ctx.fillText(label, y, x + 3);
                ctx.fillText(v.expr, y, x - 11);
            }
        );
    }
}

function renderGhost(ctx, rect) {
    ctx.strokeStyle = "#111";
    ctx.setLineDash([5, 3]);

    ctx.strokeRect(
        rect.y * TILE_SIZE,       // Canvas X = col
        rect.x * TILE_SIZE,       // Canvas Y = row
        rect.width * TILE_SIZE,   // width = cols
        rect.height * TILE_SIZE   // height = rows
    );

    ctx.setLineDash([]);
}
