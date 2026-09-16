const { app, BrowserWindow, ipcMain } = require("electron");
const path = require("path");
const fs = require("fs");
const configPath = path.join(__dirname, "../config.json");
const Config = JSON.parse(fs.readFileSync(configPath, "utf8"));
const stageDirectory = Config.stages.stagesRoot;

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
    fs.mkdirSync(stageDirectory, {
        recursive: true,
    });

    const filePath = path.join(stageDirectory, fileName);

    fs.writeFileSync(
        filePath,
        content,
        "utf8"
    );

    return filePath;
});

ipcMain.handle("load-first-stage-file", () => {
    const files = fs.readdirSync(stageDirectory)
                .filter(file => file.toLowerCase().endsWith(".json"))
                .sort((a,b)=>a.localeCompare(b, undefined, {numeric: true}));
    if (files.length == 0)
        return null;

    const fileName = files[0];
    return {
        name: fileName,
        content: fs.readFileSync(
            path.join(stageDirectory, fileName),
            "utf8"
        )
    };
});

app.whenReady().then(createWindow);