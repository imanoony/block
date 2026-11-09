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
	const fixedRoot = document.getElementById("fixed-block-palette");

	const appendBlockButton = (paletteRoot, id, isFixed) => {
		const b = document.createElement("button");

		b.className = "tool-btn block-item";
		b.dataset.id = id;

		if (isFixed) b.classList.add("fixed-block-item");

		// Block preview
		const preview = document.createElement("img");
		preview.className = "block-preview";

		const blockImg = assets.blocks[id];

		if (blockImg?.ok) {
		preview.src = isFixed
			? createTintedPreview(blockImg.img)
			: blockImg.img.src;
		}

		b.appendChild(preview);

		// Block name
		const label = document.createElement("span");
		label.textContent = `Block ${id}`;

		b.appendChild(label);

		// Select block
		b.onclick = () => {
			State.selectedBlock = id;
			State.setTool(isFixed ? "fixed-block-paint" : "block-paint");
			updateActiveButton();
		};

		paletteRoot.appendChild(b);
	};

	BLOCKS.forEach((id) => appendBlockButton(root, id, false));
	BLOCKS.forEach((id) => appendBlockButton(fixedRoot, id, true));

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
		document
		.querySelectorAll(".block-item")
		.forEach((b) => {
			const isFixed = b.classList.contains("fixed-block-item");
			b.classList.toggle(
				"is-active",
				State.tool === (isFixed ? "fixed-block-paint" : "block-paint") &&
					Number(b.dataset.id) === State.selectedBlock,
			);
		});
	}

	return {
		updateActiveButton,
	};
}

function createTintedPreview(image) {
	const canvas = document.createElement("canvas");
	canvas.width = image.naturalWidth || image.width;
	canvas.height = image.naturalHeight || image.height;
	const ctx = canvas.getContext("2d");
	ctx.drawImage(image, 0, 0);
	ctx.globalCompositeOperation = "multiply";
	ctx.fillStyle = "#E6E6E6";
	ctx.fillRect(0, 0, canvas.width, canvas.height);
	ctx.globalCompositeOperation = "destination-in";
	ctx.drawImage(image, 0, 0);
	return canvas.toDataURL();
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

