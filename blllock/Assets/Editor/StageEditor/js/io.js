// io.js

// --------------------------------------------------------
// imports
// --------------------------------------------------------
import { 
    State, 
	emptyStage 
} from "./state.js";
// --------------------------------------------------------

export function normalizeDocument(data) {
	if (Array.isArray(data)) return { Stages: data };
	if (data && Array.isArray(data.Stages)) return data;
	throw new Error("최상위에 Stages 배열이 없습니다.");
}

export function importStageFromFile(file) {
	return new Promise((res, rej) => {
		const r = new FileReader();
		r.onload = () => {
			try {
				res(normalizeDocument(JSON.parse(r.result)));
			} catch (e) {
				rej(e);
			}
		};
		r.onerror = () => rej(r.error);
		r.readAsText(file);
	});
}

export function newDocument() {
	return { Stages: [] };
}

export function selectStageById(id) {
  const i = State.document.data.Stages.findIndex(
    (s) => Number(s.ID) === Number(id),
  );
  if (i < 0) throw new Error("해당 ID의 스테이지가 없습니다.");
  State.loadStage(structuredClone(State.document.data.Stages[i]), i);
}

export function createStage() {
  State.loadStage(emptyStage(), -1);
}

export function saveCurrentStage() {
	const stages = State.document.data.Stages;
	const copy = structuredClone(State.stage);
	if (State.document.index >= 0) stages[State.document.index] = copy;
	else {
		const dup = stages.some((s) => Number(s.ID) === Number(copy.ID));
		if (dup) throw new Error("같은 ID가 이미 존재합니다.");
		stages.push(copy);
		State.document.index = stages.length - 1;
	}
	return State.document.data;
}

export async function saveDocument() {
    saveCurrentStage();

    const content = JSON.stringify(
        State.document.data,
        null,
        4
    );

    const fileName =
        State.document.name || "untitled.json";

    return await window.electronAPI.saveFile(
        fileName,
        content
    );
}
