export const TILE_SIZE = 64;
export const BG_COLS = 10;
export const BG_ROWS = 6;
export const CIRCUIT_COLS = 5;
export const CIRCUIT_ROWS = 5;
export const BLOCKS = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
export const BLOCK_DATA = {
    0: { 
        w: 1, 
        h: 1, 
        t: [
            [0, 0]
        ],
		tagPos: {
			x: 0,
			y: 0
		}
    },
    1: {
		w: 2,
		h: 1,
		t: [
			[0, 0],
			[0, 1],
		],
		tagPos: {
			x: 0,
			y: 0
		}
    },
    2: {
		w: 3,
		h: 1,
		t: [
			[0, 0],
			[0, 1],
			[0, 2],
		],
		tagPos: {
			x: 0,
			y: 0
		}
    },
    3: {
		w: 2,
		h: 1,
		t: [
			[0, 0],
			[0, 1],
		],
		tagPos: {
			x: 0,
			y: 0
		}
    },
    4: {
		w: 3,
		h: 1,
		t: [
			[0, 0],
			[0, 1],
			[0, 2],
		],
		tagPos: {
			x: 0,
			y: 0
		}
    },
    5: { 
		w: 1, 
		h: 1, 
		t: [
			[0, 0]
		],
		tagPos: {
			x: 0,
			y: 0
		}
	},
    6: {
		w: 2,
		h: 1,
		t: [
			[0, 0],
			[0, 1],
		],
		tagPos: {
			x: 0,
			y: 0
		}
    },
    7: {
		w: 2,
		h: 2,
		t: [
			[0, 0],
			[0, 1],
			[1, 1],
		],
		tagPos: {
			x: 0.45,
			y: 0.15
		}
    },
    8: { 
		w: 1, 
		h: 1, 
		t: [
			[0, 0]
		],
		tagPos: {
			x: 0,
			y: 0
		}
	},
    9: {
		w: 2,
		h: 1,
		t: [
			[0, 0],
			[0, 1],
		],
		tagPos: {
			x: 0,
			y: 0
		}
    },
    10: {
		w: 1,
		h: 2,
		t: [
			[0, 0],
			[1, 0],
		],
		tagPos: {
			x: 0,
			y: 0
		}
    },
};

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