const { app, BrowserWindow, ipcMain } = require("electron");
const path = require("path");
const fs = require("fs");

function createWindow() {
    const win = new BrowserWindow({
        width: 1600,
        height: 900,

        webPreferences: {
            preload: path.join(__dirname, "preload.js"),
            contextIsolation: true,
            nodeIntegration: false,
        },
    });

    win.loadFile(path.join(__dirname, "../index.html"));
}

ipcMain.handle("save-file", async (event, { fileName, content }) => {
    const projectDir = path.resolve(__dirname, "..");
    const outputDir = path.join(projectDir, "output");

    fs.mkdirSync(outputDir, {
        recursive: true,
    });

    const filePath = path.join(outputDir, fileName);

    fs.writeFileSync(
        filePath,
        content,
        "utf8"
    );

    return filePath;
});

app.whenReady().then(createWindow);