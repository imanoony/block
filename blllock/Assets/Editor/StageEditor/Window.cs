using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Editor.StageEditor
{
    

    /// <summary>
    /// StageEditorWindow
    /// `- root
    ///     |- toolbar
    ///     |   |- width
    ///     |   `- height
    ///     `- main
    ///         |- left-tab
    ///         |   |- block
    ///         |   |   `- block-scroll
    ///         |   |       `- block-main
    ///         |   `- tag
    ///         |- canvas
    ///         |   `- canvas-scroll
    ///         |       `- canvas-main
    ///         `- right-tab
    ///             |- brush
    ///             |   `- brush-scroll
    ///             |       `- brush-main
    ///             `- palette
    ///                 `- palette-scroll
    ///                     `- palette-main
    /// </summary>
    public class Window : EditorWindow
    {
        private const string StyleSheetPath =
            "Assets/Editor/StageEditor/StageEditorWindow.uss";

        private StyleSheet styleSheet;

        private Interaction interaction = new();

        // Components
        private LeftTab leftTabEditor;
        private Canvas canvasEditor;
        private RightTab rightTabEditor;

        // Toolbar
        private IntegerField widthField;
        private IntegerField heightField;

        private int width = 20;
        private int height = 20;

        // Layout
        private VisualElement main;

        [MenuItem("Tools/Stage Editor")]
        public static void ShowWindow()
        {
            GetWindow<Window>("Stage Editor");
        }

        private void CreateGUI()
        {
            LoadStyleSheet();

            RenderToolBar();
            RenderMain();
        }

        #region Initialization

        private void LoadStyleSheet()
        {
            styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                StyleSheetPath
            );

            if (styleSheet != null)
            {
                rootVisualElement.styleSheets.Add(styleSheet);
            }
        }

        #endregion

        #region ToolBar

        private void RenderToolBar()
        {
            VisualElement toolbar = new();
            toolbar.AddToClassList("toolbar");

            widthField = new IntegerField("Width")
            {
                value = width
            };
            widthField.AddToClassList("dimension-field");

            heightField = new IntegerField("Height")
            {
                value = height
            };
            heightField.AddToClassList("dimension-field");

            widthField.RegisterValueChangedCallback(
                evt =>
                {
                    width = Mathf.Max(1, evt.newValue);

                    widthField.SetValueWithoutNotify(width);

                    canvasEditor?.SetCanvasSize(
                        width,
                        height
                    );
                }
            );

            heightField.RegisterValueChangedCallback(
                evt =>
                {
                    height = Mathf.Max(1, evt.newValue);

                    heightField.SetValueWithoutNotify(height);

                    canvasEditor?.SetCanvasSize(
                        width,
                        height
                    );
                }
            );

            toolbar.Add(widthField);
            toolbar.Add(heightField);

            rootVisualElement.Add(toolbar);
        }

        #endregion

        #region Main

        private void RenderMain()
        {
            main = new VisualElement();
            main.AddToClassList("main");

            RenderLeftTab();
            RenderCanvas();
            RenderRightTab();

            rootVisualElement.Add(main);
        }

        private void RenderLeftTab()
        {
            leftTabEditor = new LeftTab(interaction);

            main.Add(leftTabEditor.Root);
        }

        private void RenderCanvas()
        {
            canvasEditor = new Canvas(
                width,
                height,
                interaction
            );

            main.Add(canvasEditor.Root);
        }

        private void RenderRightTab()
        {
            rightTabEditor = new RightTab(interaction);

            main.Add(rightTabEditor.Root);
        }

        #endregion
    }
}