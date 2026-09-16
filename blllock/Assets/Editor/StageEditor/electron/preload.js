const { contextBridge, ipcRenderer } = require("electron");

contextBridge.exposeInMainWorld("electronAPI", {
    saveFile: (fileName, content) =>
        ipcRenderer.invoke("save-file", {
            fileName,
            content,
        }),
});