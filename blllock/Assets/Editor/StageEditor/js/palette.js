// palette.js

// --------------------------------------------------------
// imports
// --------------------------------------------------------
import { 
	State,
	BLOCKS,
	BLOCK_DATA
} from "./state.js";
// --------------------------------------------------------

export function initPalette(assets, onCommit) {
	const root = document.getElementById("block-palette");

	BLOCKS.forEach((id) => {
		const b = document.createElement("button");

		b.className = "tool-btn block-item";
		b.dataset.id = id;

		// Block preview
		const preview = document.createElement("img");
		preview.className = "block-preview";

		const blockImg = assets.blocks[id];

		if (blockImg?.ok) {
		preview.src = blockImg.img.src;
		}

		b.appendChild(preview);

		// Block name
		const label = document.createElement("span");
		label.textContent = `Block ${id}`;

		b.appendChild(label);

		// Select block
		b.onclick = () => {
		State.selectedBlock = id;
		State.setTool("block-paint");
		updateActiveButton();
		};

		root.appendChild(b);
	});

	// Other tools
	document.querySelectorAll("[data-tool]").forEach((b) => {
		b.onclick = () => {
		State.setTool(b.dataset.tool);
		updateActiveButton();
		};
	});

	function updateActiveButton() {
		// Tool buttons
		document
		.querySelectorAll("[data-tool]")
		.forEach((b) => {
			b.classList.toggle(
			"is-active",
			b.dataset.tool === State.tool,
			);
		});

		// Block buttons
		root
		.querySelectorAll(".block-item")
		.forEach((b) => {
			b.classList.toggle(
			"is-active",
			State.tool === "block-paint" &&
				Number(b.dataset.id) === State.selectedBlock,
			);
		});
	}

	return {
		updateActiveButton,
	};
}

function drawBlockPreview(canvas, block) {
  const ctx = canvas.getContext("2d");

  const cellSize = 18;

  canvas.width = block.w * cellSize;
  canvas.height = block.h * cellSize;

  ctx.clearRect(0, 0, canvas.width, canvas.height);

  ctx.fillStyle = "#111";

  block.t.forEach(([row, col]) => {
    ctx.fillRect(
      col * cellSize,
      row * cellSize,
      cellSize,
      cellSize,
    );
  });
}

