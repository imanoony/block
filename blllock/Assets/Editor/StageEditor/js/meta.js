// meta.js

// --------------------------------------------------------
// imports
// --------------------------------------------------------
import * as state from "./state.js";
// --------------------------------------------------------

function setToolCount(type, count) {
	if (!Array.isArray(state.State.stage.ToolCounts)) {
      	state.State.stage.ToolCounts = [];
    }

    const index = state.State.stage.ToolCounts.findIndex(
      	tool => tool.type === type
    );

    // 0이면 데이터에서 제거
    if (count <= 0) {
		if (index !== -1) {
			state.State.stage.ToolCounts.splice(index, 1);
		}
		return;
    }

    if (index !== -1) {
      	state.State.stage.ToolCounts[index].count = count;
    } 
	else {
      	state.State.stage.ToolCounts.push(
			{
				type,
				count
			}
		);
    }
}

function getToolCount(stage, type) {
    const tool = stage.ToolCounts?.find(
      	tool => tool.type === type
    );

    return tool?.count ?? 0;
}

export function initMetaForm(onCommit) {
	const elements = {
		id: document.getElementById("meta-id"),
		bgWidth: document.getElementById("meta-bg-width"),
		bgHeight: document.getElementById("meta-bg-height"),
		desc: document.getElementById("meta-desc"),
		tutorialId: document.getElementById("meta-tutorial-id"),
		cableCount: document.getElementById("meta-tool-cable"),
		resistorCount: document.getElementById("meta-tool-resistor")
	};

	elements.id.addEventListener(
		"change", 
		() => {
			state.State.stage.ID = Number(elements.id.value) || 0;
			onCommit("meta");
		}
	);

	elements.bgWidth.addEventListener(
		"change", 
		() => {
			state.State.stage.BgWidth = Math.max(
				0,
				Math.floor(Number(elements.bgWidth.value) || 0)
			);
			onCommit("meta");
		}
	);

	elements.bgHeight.addEventListener(
		"change", 
		() => {
			state.State.stage.BgHeight = Math.max(
				0,
				Math.floor(Number(elements.bgHeight.value) || 0)
			);
			onCommit("meta");
		}
	);

	elements.desc.addEventListener(
		"input", 
		() => {
			state.State.stage.Desc = elements.desc.value;
		}
	);

	elements.tutorialId.addEventListener(
		"change", 
		() => {
			const v = Number(elements.tutorialId.value);
			state.State.stage.TutorialID = Number.isFinite(v) ? v : -1;
			onCommit("meta");
	});

  	// type 0 = Cable
	elements.cableCount.addEventListener(
		"change", 
		() => {
			setToolCount(
				0,
				Math.max(0, Math.floor(Number(elements.cableCount.value) || 0))
			);
			onCommit("meta");
		}
	);

	// type 1 = Resistor
	elements.resistorCount.addEventListener(
		"change", 
		() => {
			setToolCount(
				1,
				Math.max(0, Math.floor(Number(elements.resistorCount.value) || 0))
			);
			onCommit("meta");
		}
	);

	function refresh(stage) {
		elements.id.value = stage.ID;
		elements.bgWidth.value = stage.BgWidth;
		elements.bgHeight.value = stage.BgHeight;
		elements.desc.value = stage.Desc;
		elements.tutorialId.value = stage.TutorialID;
		elements.cableCount.value = getToolCount(stage, 0);
		elements.resistorCount.value = getToolCount(stage, 1);
	}

	refresh(state.State.stage);

	return { refresh };
}