using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Editor.StageEditor
{
    

    public class Canvas
    {
        public enum Layer
        {
            BackGround,
            Circuit,
            Node,
            Barrier,
            Block
        }

        public enum Placement
        {
            Cell,
            Point,
            HEdge,
            VEdge
        }

        private readonly Interaction interaction;

        private VisualElement root; // canvas
        public VisualElement Root => root;

        private ScrollView canvasScroll;
        private VisualElement canvasMain; // wrapper
        private VisualElement canvasContent; // scale target

        // Layers
        private VisualElement layerGrid;
        private VisualElement layerBackground;
        private VisualElement layerCircuit;
        private VisualElement layerNode;
        private VisualElement layerBarrier;
        private VisualElement layerBlock;

        public List<Vector2Int> DataBackground = new();
        public List<Vector2Int> DataCircuit = new(); 


        private int width;
        private int height;

        private float zoom = 1f;

        private const float CellSize = 50f;
        private const float MinZoom = 0.25f;
        private const float MaxZoom = 3f;
        private const float ZoomStep = 0.1f;

        private Layer layer;
        private Placement placement;


        public Canvas(int width, int height, Interaction interaction)
        {
            this.width = Mathf.Max(1, width);
            this.height = Mathf.Max(1, height);
            this.interaction = interaction;

            CreateCanvas();

            SetCanvasSize(this.width, this.height);
            SetCanvasZoom(zoom);
        }

        private void CreateCanvas()
        {
            // root (canvas)
            root = new VisualElement();
            root.AddToClassList("canvas");

            // canvas scroll
            canvasScroll = new ScrollView(
                ScrollViewMode.VerticalAndHorizontal
            );
            canvasScroll.AddToClassList("canvas-scroll");

            // canvas main
            canvasMain = new VisualElement();
            canvasMain.AddToClassList("canvas-main");

            // canvas content
            canvasContent = new VisualElement();
            canvasContent.AddToClassList("canvas-content");

            // layers
            layerGrid = new VisualElement();
            layerGrid.AddToClassList("layer");
            layerGrid.generateVisualContent += DrawGrid;

            layerBackground = new VisualElement();
            layerBackground.AddToClassList("layer");
            layerCircuit = new VisualElement();
            layerCircuit.AddToClassList("layer");
            layerNode = new VisualElement();
            layerNode.AddToClassList("layer");
            layerBarrier = new VisualElement();
            layerBarrier.AddToClassList("layer");
            layerBlock = new VisualElement();
            layerBlock.AddToClassList("layer");

            // hierarchy
            root.Add(canvasScroll);
            canvasScroll.Add(canvasMain);
            canvasMain.Add(canvasContent);

            canvasContent.Add(layerGrid);
            canvasContent.Add(layerBackground);
            canvasContent.Add(layerCircuit);
            canvasContent.Add(layerNode);
            canvasContent.Add(layerBarrier);
            canvasContent.Add(layerBlock);

            // events
            canvasScroll.RegisterCallback<WheelEvent>(OnCanvasWheel);
            canvasContent.RegisterCallback<PointerDownEvent>(OnCanvasPointerDown);
        }

        public void SetCanvasSize(int width, int height)
        {
            this.width = Mathf.Max(1, width);
            this.height = Mathf.Max(1, height);

            float canvasWidth = this.width * CellSize;
            float canvasHeight = this.height * CellSize;

            canvasContent.style.width = canvasWidth;
            canvasContent.style.height = canvasHeight;

            canvasMain.style.width = canvasWidth * zoom;
            canvasMain.style.height = canvasHeight * zoom;
        }
        
        private void SetCanvasZoom(float zoom)
        {
            this.zoom = Mathf.Clamp(zoom, MinZoom, MaxZoom);
            canvasContent.style.scale = new Scale(
                new Vector3(this.zoom, this.zoom, 1f)
            );

            canvasMain.style.width = width * CellSize * this.zoom;
            canvasMain.style.height = height * CellSize * this.zoom;

            layerGrid.MarkDirtyRepaint();
        }

        private void OnCanvasWheel(WheelEvent evt)
        {
            if (!evt.ctrlKey) return;

            if (evt.delta.y < 0) SetCanvasZoom(zoom + ZoomStep);
            else SetCanvasZoom(zoom - ZoomStep);

            evt.StopPropagation();
        }

        private void OnCanvasPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0) return;

            Interaction.EditState editState = interaction.State;
            switch (editState)
            {
                case Interaction.EditState.None:
                    break;
                case Interaction.EditState.Block:
                    GetPlacementCoordinate(evt.localPosition, Placement.Cell);
                    PaintBlock(evt.localPosition);
                    break;
                case Interaction.EditState.Paint:
                    Paint(evt.localPosition);
                    break;
                case Interaction.EditState.Eraser:
                    GetPlacementCoordinate(evt.localPosition, Placement.HEdge);
                    Eraser(evt.position);
                    break;
                case Interaction.EditState.Move:
                    GetPlacementCoordinate(evt.localPosition, Placement.VEdge);
                    break;
                default:
                    break;
            }
            // TODO
        }

        private void PaintBlock(Vector3 localPos)
        {
            Image block = interaction.SelectedBlock;
            Vector2Int coord = GetPlacementCoordinate(localPos, Placement.Cell);
            Vector2 pos = CellCoord2Pos(coord);

            Image clone = new();
            clone.sprite = block.sprite;
            clone.userData = block.userData;
            float width = clone.sprite.rect.width * (1f/8f);
            float height = clone.sprite.rect.height * (1f/8f);
            clone.style.width = width;
            clone.style.height = height;

            clone.style.position = Position.Absolute;
            clone.style.left = pos.x;
            clone.style.top = pos.y;
            clone.AddToClassList("painted");
            layerBlock.Add(clone);
        }

        private void Paint(Vector3 localPos)
        {
            Interaction.PaletteType palette = interaction.Palette;
            Vector2Int coord;
            Vector2 pos;
            VisualElement ve;

            switch (palette)
            {
                case Interaction.PaletteType.Background:
                    coord = GetPlacementCoordinate(localPos, Placement.Cell);
                    pos = CellCoord2Pos(coord);

                    if (DataBackground.Contains(coord)) break;
                    DataBackground.Add(coord);

                    ve = new();
                    ve.AddToClassList("palette-item");
                    ve.AddToClassList("background-tile");
                    ve.AddToClassList("painted");
                    ve.style.position = Position.Absolute;
                    ve.style.left = pos.x;
                    ve.style.top = pos.y;
                    layerBackground.Add(ve);

                    break;
                case Interaction.PaletteType.Circuit:
                    coord = GetPlacementCoordinate(localPos, Placement.Cell);
                    pos = CellCoord2Pos(coord);

                    if (DataCircuit.Contains(coord)) break;
                    DataCircuit.Add(coord);

                    ve = new();
                    ve.AddToClassList("palette-item");
                    ve.AddToClassList("circuit-tile");
                    ve.AddToClassList("painted");
                    ve.style.position = Position.Absolute;
                    ve.style.left = pos.x;
                    ve.style.top = pos.y;
                    layerCircuit.Add(ve);
                    break;
                case Interaction.PaletteType.Node:
                    break;
                case Interaction.PaletteType.HBarrier:
                    break;
                case Interaction.PaletteType.VBarrier:
                    break;
                default:
                    break;
            }
            // 현재 팔레트 정보 가져오기
            // Background면: 
            //      레이어 Background, 색칠 타입 Cell
            //      색칠 그래픽 처리
            //      색칠 데이터 처리
            // Circuit이면:
            //      레이어 Circuit, 색칠 타입 Cell
            //      색칠 그래픽 처리
            //      색칠 데이터 처리
            // Node면:
            //      레이어 Node, 색칠 타입 Point
            //      색칠 그래픽 처리
            //      색칠 데이터 처리
        }

        private void Eraser(Vector2 panelPos)
        {
            List<VisualElement> pickedList = new();
            root.panel.PickAll(panelPos, pickedList);

            Debug.Log($"Eraser, picked count: {pickedList.Count}");


            // Block 레이어 검사
            foreach (VisualElement picked in pickedList)
            {
                if (!layerBlock.Contains(picked)) continue;
                if (!picked.ClassListContains("painted")) continue;

                picked.RemoveFromHierarchy();

                // 데이터 처리
                return;
            }

            // Barrier 레이어 검사

            // Node 레이어 검사

            // Circuit 레이어 검사
            foreach (VisualElement picked in pickedList)
            {
                if (!layerCircuit.Contains(picked)) continue;
                if (!picked.ClassListContains("painted")) continue;

                picked.RemoveFromHierarchy();

                // 데이터 처리
                return;
            }

            // Background 레이어 검사
            foreach (VisualElement picked in pickedList)
            {
                if (!layerBackground.Contains(picked)) continue;
                if (!picked.ClassListContains("painted")) continue;

                picked.RemoveFromHierarchy();

                // 데이터 처리
                return;
            }

            // 위 레이어부터 검사
            VisualElement[] layers =
            {
                layerBlock,
                layerBarrier,
                layerNode,
                layerCircuit,
                layerBackground
            };

            foreach (VisualElement layer in layers)
            {
                foreach (VisualElement picked in pickedList)
                {
                    // 해당 레이어에 속하지 않은 VE는 무시
                    if (!layer.Contains(picked))
                        continue;

                    // 실제 배치된 VE가 아니면 무시
                    if (!picked.ClassListContains("painted"))
                        continue;

                    Debug.Log($"Eraser remove: {picked.name}");

                    picked.RemoveFromHierarchy();
                    return;
                }
            }
        }

        private void DrawGrid(MeshGenerationContext ctx)
        {
            var painter = ctx.painter2D;

            painter.strokeColor = new Color(
                0.4f,
                0.4f,
                0.4f
            );

            painter.lineWidth = 1f;

            float cellSize = CellSize;

            // Vertical
            for (int x = 0; x <= width; x++)
            {
                float px = x * cellSize;

                painter.BeginPath();

                painter.MoveTo(
                    new Vector2(
                        px,
                        0
                    )
                );

                painter.LineTo(
                    new Vector2(
                        px,
                        height * cellSize
                    )
                );

                painter.Stroke();
            }

            // Horizontal
            for (int y = 0; y <= height; y++)
            {
                float py = y * cellSize;

                painter.BeginPath();

                painter.MoveTo(
                    new Vector2(
                        0,
                        py
                    )
                );

                painter.LineTo(
                    new Vector2(
                        width * cellSize,
                        py
                    )
                );

                painter.Stroke();
            }
        }

        private Vector2Int GetPlacementCoordinate(
            Vector2 position,
            Placement placement
        )
        {
            return placement switch
            {
                Placement.Cell => GetCellCoordinate(position),
                Placement.Point => GetPointCoordinate(position),
                Placement.HEdge => GetHEdgeCoordinate(position),
                Placement.VEdge => GetVEdgeCoordinate(position),
                _ => default
            };
        }

        private Vector2Int GetCellCoordinate(Vector2 position)
        {
            int row = Mathf.FloorToInt(position.y / CellSize);
            int col = Mathf.FloorToInt(position.x / CellSize);

            Debug.Log($"[STAGE EDITOR] [Cooordinate] [Cell] row: {row}, col: {col}");

            return new(row, col);
        }

        private Vector2Int GetPointCoordinate(Vector2 position)
        {
            int row = Mathf.RoundToInt(position.y / CellSize);
            int col = Mathf.RoundToInt(position.x / CellSize);

            Debug.Log($"[STAGE EDITOR] [Cooordinate] [Point] row: {row}, col: {col}");

            return new(row, col);
        }


        private Vector2Int GetHEdgeCoordinate(Vector2 position)
        {
            int row = Mathf.RoundToInt(position.y / CellSize);
            int col = Mathf.FloorToInt(position.x / CellSize);

            Debug.Log($"[STAGE EDITOR] [Cooordinate] [HEdge] row: {row}, col: {col}");

            return new(row, col);
        }

        private Vector2Int GetVEdgeCoordinate(Vector2 position)
        {
            int row = Mathf.FloorToInt(position.y / CellSize);
            int col = Mathf.RoundToInt(position.x / CellSize);

            Debug.Log($"[STAGE EDITOR] [Cooordinate] [VEdge] row: {row}, col: {col}");

            return new(row, col);
        }

        

        private Vector2 CellCoord2Pos(Vector2Int coord)
        {
            return new(coord.y * CellSize, coord.x * CellSize);
        }
        private Vector2 PointCoord2Pos(Vector2Int coord)
        {
            return new(coord.y * CellSize, coord.x * CellSize);
        }
        private Vector2 HEdgeCoord2Pos(Vector2Int coord)
        {
            return new(coord.y * CellSize, coord.x * CellSize);
        }
        private Vector2 VEdgeCoord2Pos(Vector2Int coord)
        {
            return new(coord.y * CellSize, coord.x * CellSize);
        }
    }
}