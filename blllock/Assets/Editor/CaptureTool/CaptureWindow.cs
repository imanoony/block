using System.IO;
using UnityEditor;
using UnityEngine;

public class CaptureWindow : EditorWindow
{
    private Camera cam = null;
    private int width = 960;
    private int height = 540;
    private int superSample = 2;
    private string folderPath = "Assets/Thumbnails";
    private string fileName = "";
    private bool overwrite = true;

    [MenuItem("Tools/Capture Tool")]
    public static void ShowWindow()
    {
        CaptureWindow window = GetWindow<CaptureWindow>("Capture Tool");
        window.minSize = new Vector2(300, 200);
    }

    void OnEnable()
    {
        if (cam == null)
        {
            cam = Camera.main;
        }
    }

    void OnGUI()
    {
        EditorGUILayout.Space(4);

        cam = (Camera)EditorGUILayout.ObjectField("Camera", cam, typeof(Camera), true);

        using (new EditorGUILayout.HorizontalScope())
        {
            width = EditorGUILayout.IntField("Size (W x H)", width);
            height = EditorGUILayout.IntField(height);
        }

        superSample = EditorGUILayout.IntSlider("Super Sample", superSample, 1, 4);
        folderPath = EditorGUILayout.TextField("Folder Path", folderPath);
        fileName = EditorGUILayout.TextField("File Name", fileName);
        overwrite = EditorGUILayout.Toggle("Overwrite", overwrite);

        EditorGUILayout.Space(8);

        using (new EditorGUI.DisabledScope(cam == null || fileName == ""))
        {
            if (GUILayout.Button("Capture", GUILayout.Height(36)))
            {
                Capture();
            }
        }

        if (cam == null)
        {
            EditorGUILayout.HelpBox("No camera selected", MessageType.Warning);
        }

        if (fileName == "")
        {
            EditorGUILayout.HelpBox("File name is empty", MessageType.Warning);
        }
    }

    private void Capture()
    {
        int w = width * superSample;
        int h = height * superSample;

        RenderTexture big = new(w, h, 24)
        {
            antiAliasing = superSample
        };

        RenderTexture small = new(width, height, 0);

        RenderTexture old = cam.targetTexture;
        RenderTexture oldActive = RenderTexture.active;

        cam.targetTexture = big;
        cam.Render();

        Graphics.Blit(big, small);
        RenderTexture.active = small;

        Texture2D tex = new(width, height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();

        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string dir = Path.Combine(projectRoot, folderPath);
        string file = Path.Combine(dir, fileName + ".png");
        Directory.CreateDirectory(dir);

        if (!overwrite)
        {
            int i = 1;
            while (File.Exists(file))
                file = Path.Combine(dir, $"{fileName}_{i++}.png");
        }

        File.WriteAllBytes(file, tex.EncodeToPNG());

        cam.targetTexture = old;
        RenderTexture.active = oldActive;

        big.Release();
        small.Release();

        DestroyImmediate(big);
        DestroyImmediate(small);
        DestroyImmediate(tex);

        AssetDatabase.Refresh();
    }
}