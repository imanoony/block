export const TILE_SIZE = 64;
export const BG_COLS = 10;
export const BG_ROWS = 6;
export const CIRCUIT_COLS = 5;
export const CIRCUIT_ROWS = 5;
export const BLOCKS = [];
export const BLOCK_DATA = {};

function parseBlockCsv(csv) {
	const lines = csv
		.replace(String.fromCharCode(0xFEFF), "")
		.split(/\r?\n/)
		.map((line) => line.trim())
		.filter(Boolean);
	const blocks = [];

	for (let lineIndex = 0; lineIndex < lines.length; lineIndex++) {
		const fields = lines[lineIndex].split(",").map((field) => field.trim());
		if (lineIndex === 0 && fields[0] === "ID") continue;
		if (fields.length !== 7) {
			throw new Error("Block.csv line " + (lineIndex + 1) + ": expected 7 columns.");
		}

		const [rawId, rawWidth, rawHeight, rawTiles, , , rawTagPos] = fields;
		const id = Number(rawId);
		const w = Number(rawWidth);
		const h = Number(rawHeight);
		const tagPos = rawTagPos.split(";").map(Number);

		if (
			!Number.isInteger(id) ||
			!Number.isFinite(w) ||
			!Number.isFinite(h) ||
			w <= 0 ||
			h <= 0 ||
			tagPos.length !== 2 ||
			!tagPos.every(Number.isFinite)
		) {
			throw new Error("Block.csv line " + (lineIndex + 1) + ": invalid block metadata.");
		}

		const t = rawTiles.split(";").map((tile) => {
			const match = tile.match(/^\((-?\d+)\.(-?\d+)\)$/);
			if (!match) {
				throw new Error("Block.csv line " + (lineIndex + 1) + ": invalid tile coordinate.");
			}
			return [Number(match[1]), Number(match[2])];
		});

		if (t.length === 0) {
			throw new Error("Block.csv line " + (lineIndex + 1) + ": block has no tiles.");
		}

		blocks.push({
			id,
			data: {
				w,
				h,
				t,
				tagPos: { x: tagPos[0], y: tagPos[1] },
			},
		});
	}

	if (blocks.length === 0) {
		throw new Error("Block.csv contains no block rows.");
	}

	blocks.sort((a, b) => a.id - b.id);
	return blocks;
}

export async function loadBlockData() {
	const response = await fetch("../../Resources/Data/Blocks/Block1.csv");
	if (!response.ok && response.status !== 0) {
		throw new Error("Could not load Block.csv (HTTP " + response.status + ").");
	}

	const blocks = parseBlockCsv(await response.text());
	BLOCKS.splice(0, BLOCKS.length, ...blocks.map(({ id }) => id));
	for (const id of Object.keys(BLOCK_DATA)) delete BLOCK_DATA[id];
	for (const { id, data } of blocks) BLOCK_DATA[id] = data;
}

// ToolCounts can arrive in a few different shapes depending on where the
// stage data came from (hand-authored JSON, an older export, a map keyed by
// type, PascalCase keys, etc). This normalizes any of those into the
// canonical [{ type, count }] shape the editor uses internally, WITHOUT
// throwing the data away just because its shape doesn't match exactly.
export function normalizeToolCounts(raw) {
  const toEntry = (type, count) => ({
    type: Number(type),
    count: Math.max(0, Math.floor(Number(count) || 0)),
  });

  let entries = [];

  if (Array.isArray(raw)) {
    entries = raw
      .filter((e) => e && (e.type !== undefined || e.Type !== undefined))
      .map((e) => toEntry(e.type ?? e.Type, e.count ?? e.Count));
  } else if (raw && typeof raw === "object") {
    // e.g. { "0": 5, "1": 2 }
    entries = Object.entries(raw).map(([type, count]) =>
      toEntry(type, count),
    );
  }

  return entries.filter((e) => Number.isFinite(e.type) && e.count > 0);
}

export function emptyStage() {
	return {
		ID: 0,
		BgWidth: 0,
		BgHeight: 0,
		Circuits: [],
		Inputs: [],
		Outputs: [],
		Desc: "",
		TutorialID: -1,
		Blocks: [],
		BlockPositions: [],
		RotateCWIndex: [],
		RotateCCWIndex: [],
		FlipXIndex: [],
		FlipYIndex: [],
		SIndex: [],
		HBarriers: [],
		VBarriers: [],
		TileBarriers: [],
		FixedBlocks: [],
		ToolCounts: [],
	};
}

export const KNOWN_KEYS = Object.keys(emptyStage());

export const State = {
	stage: emptyStage(),
	document: { 
		name: "untitled.json", 
		data: { Stages: [] }, 
		index: -1 
	},
	workspace: { 
		cols: 40, rows: 24 
	},
	tool: "bg-paint",
	selectedBlock: 0,
	selectedIO: { 
		kind: "input", expr: "X" 
	},
	listeners: [],

	reset() {
		this.stage = emptyStage();
		this.document.index = -1;
		this.emit("reset");
	},
	
	loadStage(data, index = this.document.index) {
		const base = emptyStage(),
		m = { ...base, ...data };
		for (const k of KNOWN_KEYS) {
			if (Array.isArray(base[k]) && !Array.isArray(m[k])) {
				m[k] = [];
			}
		}
		
		m.ToolCounts = normalizeToolCounts(m.ToolCounts);
		let n = Math.min(m.Blocks.length, m.BlockPositions.length);
		m.Blocks = m.Blocks.slice(0, n);
		m.BlockPositions = m.BlockPositions.slice(0, n);

		this.stage = m;
		this.document.index = index;
		this.emit("load");
	},

	newStage() {
		this.loadStage(emptyStage(), -1);
	},

	setTool(t) {
		this.tool = t;
		this.emit("tool");
	},

	on(f) {
		this.listeners.push(f);
	},
	
	emit(r) {
		this.listeners.forEach((f) => f(this.stage, r));
	},
};