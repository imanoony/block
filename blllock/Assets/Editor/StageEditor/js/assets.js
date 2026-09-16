import { Config } from "./config.js";

function loadImage(src) {
	return new Promise((r) => {
		const i = new Image();
		i.onload = () => r({ img: i, ok: true });
		i.onerror = () => r({ img: null, ok: false });
		i.src = src;
	});
}

export const Assets = {
	bg: {},
	circuit: {},
	blocks: [],

	async load() {
		const root = Config.sprites.spritesRoot;
		const [bg, circuit, ...blocks] = await Promise.all(
			[
				loadImage(`${root}/${Config.sprites.assets.spriteBackground}`),
				loadImage(`${root}/${Config.sprites.assets.spriteCircuit}`),
				
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
		this.tag = {
			rotateCW,
			rotateCCW,
			flipX,
			flipY
		};

		return {
			bgOk: bg.ok,
			circuitOk: circuit.ok,
		};
	}
};
