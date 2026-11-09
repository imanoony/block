import { Config } from "./config.js";

function loadImage(src) {
	return new Promise((r) => {
		const i = new Image();
		i.onload = () => r({ img: i, ok: true });
		i.onerror = () => r({ img: null, ok: false });
		i.src = src;
	});
}

function sliceSpriteSheet(image, rows, cols) {
	const tileWidth = image.width / cols;
	const tileHeight = image.height / rows;

	return Array.from({ length: rows * cols }, (_, index) => {
		const row = Math.floor(index / cols);
		const col = index % cols;
		const canvas = document.createElement("canvas");
		canvas.width = tileWidth;
		canvas.height = tileHeight;
		canvas.getContext("2d").drawImage(
			image,
			col * tileWidth,
			row * tileHeight,
			tileWidth,
			tileHeight,
			0,
			0,
			tileWidth,
			tileHeight,
		);
		return { img: canvas, ok: true };
	});
}

export const Assets = {
	bg: {},
	circuit: {},
	blocks: [],
	tileBarriers: [],

	async load() {
		const root = Config.sprites.spritesRoot;
		const tileBarrierPath = Config.sprites.assets.spriteTileBarrier
			.split("/")
			.filter(Boolean)
			.join("/");
		const [bg, circuit, tileBarrierSheet, ...blocks] = await Promise.all(
			[
				loadImage(`${root}/${Config.sprites.assets.spriteBackground}`),
				loadImage(`${root}/${Config.sprites.assets.spriteCircuit}`),
				loadImage(`${root}/${tileBarrierPath}`),
				
				...Array.from(
					{ length: Config.sprites.blockCount },
					(_, i) => loadImage(`${root}/${Config.sprites.assets.spriteBlock}${i}_0.png`)
				),
			]
		);
		const tagRoot = `${root}/${Config.sprites.assets.spriteTag.root}`;
		const [rotateCW, rotateCCW, flipX, flipY] = await Promise.all(
			[
				loadImage(`${tagRoot}/${Config.sprites.assets.spriteTag.rotateCW}`),
				loadImage(`${tagRoot}/${Config.sprites.assets.spriteTag.rotateCCW}`),
				loadImage(`${tagRoot}/${Config.sprites.assets.spriteTag.flipX}`),
				loadImage(`${tagRoot}/${Config.sprites.assets.spriteTag.flipY}`)
			]
		);

		this.bg = bg;
		this.circuit = circuit;
		this.blocks = blocks;
		this.tileBarriers = tileBarrierSheet.ok
			? sliceSpriteSheet(tileBarrierSheet.img, 2, 2)
			: [];
		this.tag = {
			rotateCW,
			rotateCCW,
			flipX,
			flipY
		};

		return {
			bgOk: bg.ok,
			circuitOk: circuit.ok,
			tileBarriersOk: tileBarrierSheet.ok,
		};
	}
};
