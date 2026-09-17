const { contextBridge, ipcRenderer } = require("electron");

contextBridge.exposeInMainWorld("electronAPI", {
    loadFirstStageFile: () =>
        ipcRenderer.invoke("load-first-stage-file"),
    saveFile: (fileName, content) =>
        ipcRenderer.invoke("save-file", {
            fileName,
            content,
        }),
});