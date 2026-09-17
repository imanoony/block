// main.js

// --------------------------------------------------------
// imports
// --------------------------------------------------------
import * as state from "./state.js";
import * as assets from "./assets.js";
import * as renderer from "./renderer.js";
import * as tools from "./tools.js";
import * as io from "./io.js";
import * as meta from "./meta.js";
import * as palette from "./palette.js";
// --------------------------------------------------------

function renderAll(ctx, canvas, ui, mt) {
	
	// renderGrid
	// renderBg
	// renderCircuits
	// renderBarriers
	// renderBlocks
	// renderIos
	// renderGhost
	renderer.render(
		ctx, 
		canvas, 
		state.State.stage, 
		assets.Assets, 
		ui
	);

	// render-meta-id
	// render-meta-bgWidth
	// render-meta-bgHeight
	// render-meta-desc
	// render-meta-tutorialId
	// render-meta-cableCount
	// render-meta-resistorCount
	mt.refresh(state.State.stage);

	// render stage select options
	refreshStages();

	// render file name
	// render stage info
	info();
}

function refreshStages() {
	const select = document.querySelector("#stage-select"),
	old = state.State.document.index;
	select.innerHTML = '<option value="-1">새 스테이지</option>';
		
	state.State.document.data.Stages.forEach(
		(s, i) => {
			const o = new Option(`${s.ID}`, i);
			if (i === old) o.selected = true;
			select.add(o);
		}
	);
}

function info() {
	document.querySelector("#file-name").textContent = state.State.document.name;
	document.querySelector("#io-info").textContent =
		`Input ${state.State.stage.Inputs.length} · Output ${state.State.stage.Outputs.length} · Block ${state.State.stage.Blocks.length}`;
}

function connectDocument(ctx, canvas, ui, mt) {

	// 새 파일 (btn-new-file)
	document.querySelector("#btn-new-file").onclick = () => {
		state.State.document.data = io.newDocument();
		state.State.document.name = "untitled.json";
		state.State.newStage();
		renderAll(ctx, canvas, ui, mt);
	};

	// 파일 열기 (btn-import)
	document.querySelector("#btn-import").onclick = () =>
		document.querySelector("#file-import").click();
	document.querySelector("#file-import").onchange = async (e) => {
		try {
			state.State.document.data = await io.importStageFromFile(e.target.files[0]);
			state.State.document.name = e.target.files[0].name;

			if (state.State.document.data.Stages.length) {
				state.State.loadStage(
					structuredClone(state.State.document.data.Stages[0]), 
					0
				);
			}
			else {
				io.createStage();
			}
			renderAll(ctx, canvas, ui, mt);
		} catch (x) {
			alert(x.message);
		}
		e.target.value = "";
	};

	// 스테이지 선택 (stage-select)
	document.querySelector("#stage-select").onchange = (e) => {
		if (+e.target.value < 0) {
			io.createStage();
		}
		else {
			state.State.loadStage(
				structuredClone(state.State.document.data.Stages[+e.target.value]),
				+e.target.value,
			);
		}
		renderAll(ctx, canvas, ui, mt);
	};

	// 새 스테이지 (btn-new-stage)
	document.querySelector("#btn-new-stage").onclick = () => {
		io.createStage();
		renderAll(ctx, canvas, ui, mt);
	};

	// 저장 (btn-save)
	document.querySelector("#btn-save").onclick = async () => {
		try {
			await io.saveDocument();
			renderAll(ctx, canvas, ui, mt);
		} catch (e) {
			alert(e.message);
		}
	};

	const ids = ["io-lu", "io-ld", "io-ru", "io-rd"];
	ids.forEach(
		(id) => {
			document.querySelector("#" + id).value = "X";
		}
	);

	function expr() {
		return ids
		.map((id) => document.querySelector("#" + id).value.trim())
		.join(";");
	}

	document.querySelector("#io-expr-preview").textContent = state.State.selectedIO.expr;

	document.querySelector("#btn-io-apply").onclick = () => {
		state.State.selectedIO.expr = expr();
		document.querySelector("#io-expr-preview").textContent = state.State.selectedIO.expr;
	};
	document.querySelector("#status-assets").textContent = "에셋 로드 완료";
}

async function main() {
	const canvas = document.querySelector("#stage-canvas");
	const ctx = canvas.getContext("2d");
	const ui = {
		dragPreview: null
	};

	await assets.Assets.load();

	renderer.resizeCanvas(canvas, 40, 24);

	const mt = meta.initMetaForm(() => renderAll(ctx, canvas, ui, mt));
	const pal = palette.initPalette(assets.Assets, () => renderAll(ctx, canvas, ui, mt));

	tools.attachTools(
		canvas, 
		{
			onPreviewChange: (p) => {
				ui.dragPreview = p;
				renderAll(ctx, canvas, ui, mt);
			},
			onHoverChange: (c) => {
				document.querySelector("#status-hover").textContent = c
					? `(${c.x}, ${c.y})`
					: "-";
				},
			onCommit: () => {
				renderAll(ctx, canvas, ui, mt);
			},
		}
	);

	state.State.on(
		() => {
			document.querySelector("#status-tool").textContent = state.State.tool;
			pal.updateActiveButton();
		}
	);

	connectDocument(ctx, canvas, ui, mt);

	renderAll(ctx, canvas, ui, mt);
}

main();