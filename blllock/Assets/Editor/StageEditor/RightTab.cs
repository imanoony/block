using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Editor.StageEditor
{  
    public class RightTab
    {
        private readonly Interaction interaction;

        private VisualElement root;
        public VisualElement Root => root;

        private VisualElement brush;
        private ScrollView brushScroll;
        private VisualElement brushMain;

        private VisualElement palette;
        private ScrollView paletteScroll;
        private VisualElement paletteMain;

        public RightTab(Interaction interaction)
        {
            this.interaction = interaction;

            CreateUI();
        }

        private void CreateUI()
        {
            root = new VisualElement();
            root.AddToClassList("right-tab");
            
            CreatePalette();
            CreateBrush();
        }

        private void CreateBrush()
        {
            brush = new VisualElement();
            brush.AddToClassList("brush");

            brushScroll = new ScrollView(
                ScrollViewMode.Vertical
            );
            brushScroll.AddToClassList("brush-scroll");

            brushMain = new VisualElement();
            brushMain.AddToClassList("brush-main");

            brushScroll.Add(brushMain);
            brush.Add(brushScroll);

            root.Add(brush);

            RenderBrushes();
        }

        private void CreatePalette()
        {
            palette = new VisualElement();
            palette.AddToClassList("palette");

            paletteScroll = new ScrollView(
                ScrollViewMode.Vertical
            );
            paletteScroll.AddToClassList("palette-scroll");

            paletteMain = new VisualElement();
            paletteMain.AddToClassList("palette-main");

            paletteScroll.Add(paletteMain);
            palette.Add(paletteScroll);

            root.Add(palette);

            RenderPalette();
        }

        private Image selectedBrush;
        private const string paintTool = "d_Grid.PaintTool";
        private const string eraserTool = "d_Grid.EraserTool";
        private const string moveTool = "d_Grid.MoveTool";

        private void RenderBrushes()
        {
            Image paintBrush = new()
            {
                image = EditorGUIUtility.IconContent(paintTool).image
            };
            paintBrush.AddToClassList("brush-item");
            paintBrush.userData = Interaction.EditState.Paint;
            paintBrush.RegisterCallback<ClickEvent>(evt =>
            {
                interaction.SelectBrush(paintBrush);
                // TODO: interaction 처리
            });
            brushMain.Add(paintBrush);

            Image eraserBrush = new()
            {
                image = EditorGUIUtility.IconContent(eraserTool).image
            };
            eraserBrush.AddToClassList("brush-item");
            eraserBrush.userData = Interaction.EditState.Eraser;
            eraserBrush.RegisterCallback<ClickEvent>(evt =>
            {
                interaction.SelectBrush(eraserBrush);
                // TODO: interaction 처리
            });
            brushMain.Add(eraserBrush);

            Image moveBrush = new()
            {
                image = EditorGUIUtility.IconContent(moveTool).image
            };
            moveBrush.AddToClassList("brush-item");
            moveBrush.userData = Interaction.EditState.Move;
            moveBrush.RegisterCallback<ClickEvent>(evt =>
            {
                interaction.SelectBrush(moveBrush);
                // TODO: interaction 처리
            });
            brushMain.Add(moveBrush);
        }

        // 팔레트 아이템 목록 정리 (16개)
        // Background 타일
        // Circuit 타일
        // Output Node: X(both/out), Y(both/out), Z(both/out)
        // Input Node: X(both/out), Y(both/out), Z(both/out)
        // Barrier: Horz, Vert
        
        private void RenderPalette()
        {
            // Background 타일
            VisualElement backgroundTile = new();
            backgroundTile.AddToClassList("palette-item");
            backgroundTile.AddToClassList("background-tile");
            backgroundTile.RegisterCallback<ClickEvent>(evt =>
            {
                interaction.SelectPalette(backgroundTile);
                // TODO: interaction 처리
            });
            backgroundTile.userData = Interaction.PaletteType.Background;
            paletteMain.Add(backgroundTile);

            // Circuit 타일
            VisualElement circuitTile = new();
            circuitTile.AddToClassList("palette-item");
            circuitTile.AddToClassList("circuit-tile");
            circuitTile.RegisterCallback<ClickEvent>(evt =>
            {
                interaction.SelectPalette(circuitTile);
                // TODO: interaction 처리
            });
            circuitTile.userData = Interaction.PaletteType.Circuit;
            paletteMain.Add(circuitTile);

            string[] vars = new[] { "x", "y", "z" };

            // Output Node - both
            for (int i = 0; i < 3; i++)
            {
                VisualElement parent = new();
                VisualElement node = new();

                parent.AddToClassList("palette-item");
                node.AddToClassList("node");
                node.AddToClassList("output");
                node.AddToClassList("both");
                node.AddToClassList(vars[i]);
                parent.Add(node);
                parent.RegisterCallback<ClickEvent>(evt =>
                {
                    interaction.SelectPalette(parent);
                    // TODO: interaction 처리
                });
                parent.userData = Interaction.PaletteType.Node;
                paletteMain.Add(parent);
            }

            // Output Node - out
            for (int i = 0; i < 3; i++)
            {
                VisualElement parent = new();
                VisualElement node = new();

                parent.AddToClassList("palette-item");
                node.AddToClassList("node");
                node.AddToClassList("output");
                node.AddToClassList("out");
                node.AddToClassList(vars[i]);
                parent.Add(node);
                parent.RegisterCallback<ClickEvent>(evt =>
                {
                    interaction.SelectPalette(parent);
                    // TODO: interaction 처리
                });
                parent.userData = Interaction.PaletteType.Node;
                paletteMain.Add(parent);
            }

            // Input Node - both
            for (int i = 0; i < 3; i++)
            {
                VisualElement parent = new();
                VisualElement node = new();

                parent.AddToClassList("palette-item");
                node.AddToClassList("node");
                node.AddToClassList("input");
                node.AddToClassList("both");
                node.AddToClassList(vars[i]);
                parent.Add(node);
                parent.RegisterCallback<ClickEvent>(evt =>
                {
                    interaction.SelectPalette(parent);
                    // TODO: interaction 처리
                });
                parent.userData = Interaction.PaletteType.Node;
                paletteMain.Add(parent);
            }

            // Input Node - out
            for (int i = 0; i < 3; i++)
            {
                VisualElement parent = new();
                VisualElement node = new();

                parent.AddToClassList("palette-item");
                node.AddToClassList("node");
                node.AddToClassList("input");
                node.AddToClassList("out");
                node.AddToClassList(vars[i]);
                parent.Add(node);
                parent.RegisterCallback<ClickEvent>(evt =>
                {
                    interaction.SelectPalette(parent);
                    // TODO: interaction 처리
                });
                parent.userData = Interaction.PaletteType.Node;
                paletteMain.Add(parent);
            }

            // Vertical Barrier
            VisualElement vbarParent = new();
            VisualElement vbar = new();
            vbarParent.AddToClassList("palette-item");
            vbar.AddToClassList("vbarrier");
            vbarParent.Add(vbar);
            vbarParent.RegisterCallback<ClickEvent>(evt =>
            {
                interaction.SelectPalette(vbarParent);
                // TODO: interaction 처리
            });
            vbarParent.userData = Interaction.PaletteType.VBarrier;
            paletteMain.Add(vbarParent);

            // Horizontal Barrier
            VisualElement hvarParent = new();
            VisualElement hvar = new();
            hvarParent.AddToClassList("palette-item");
            hvar.AddToClassList("hbarrier");
            hvarParent.Add(hvar);
            hvarParent.RegisterCallback<ClickEvent>(evt =>
            {
                interaction.SelectPalette(hvarParent);
                // TODO: interaction 처리
            });
            hvarParent.userData = Interaction.PaletteType.HBarrier;
            paletteMain.Add(hvarParent);
        }
    }
}