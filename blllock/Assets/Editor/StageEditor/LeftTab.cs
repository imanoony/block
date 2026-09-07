using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Editor.StageEditor
{
    public class LeftTab
    {
        private readonly Interaction interaction;

        private VisualElement root;

        private VisualElement block;
        private ScrollView blockScroll;
        private VisualElement blockMain;

        private VisualElement tag;

        public VisualElement Root => root;

        public LeftTab(Interaction interaction)
        {
            this.interaction = interaction;

            CreateRoot();
            CreateBlock();
            CreateTag();
        }

        private void CreateRoot()
        {
            root = new VisualElement();
            root.AddToClassList("left-tab");
        }

        private void CreateBlock()
        {
            block = new VisualElement();
            block.AddToClassList("block");

            blockScroll = new ScrollView(
                ScrollViewMode.Vertical
            );
            blockScroll.AddToClassList("block-scroll");

            blockMain = new VisualElement();
            blockMain.AddToClassList("block-main");

            blockScroll.Add(blockMain);
            block.Add(blockScroll);

            root.Add(block);

            RenderBlocks();
        }

        private void CreateTag()
        {
            tag = new VisualElement();
            tag.AddToClassList("tag");

            root.Add(tag);
        }

        // GameManager.Instance.BlockLibrary[id]에서 블록 데이터를 읽어와서
        // BlockPlacer의 blockSprites에서 스프라이트를 가져온 후,
        // block main 하위 element로 각 스프라이트들을 추가한다.
        // 이 클래스 하위에 block elem<->block data 매칭을 가지는 자료형이 필요하다. 

        private DataParser dataParser = new();
        private Sprite[] blockSprites;
        private const string blockPath = "Block";
        private Dictionary<int, BlockData> blockLibrary;

        private void RenderBlocks()
        {
            blockLibrary = dataParser.ParseBlockData(blockPath);
            BlockPlacer blockPlacer = Object.FindFirstObjectByType<BlockPlacer>();

            if (blockPlacer == null)
            {
                Debug.LogError("[STAGE EDITOR] BlockPlacer not found in the scene.");
                return;
            }

            blockSprites = blockPlacer.BlockSprites;

            foreach (int id in blockLibrary.Keys)
            {
                RenderBlock(id);
            }
        }

        private float blockScale = 0.125f;
        private void RenderBlock(int id)
        {
            BlockData blockData = blockLibrary[id];
            Sprite sprite = blockSprites[id];

            Image image = new()
            {
                sprite = sprite
            };
            image.AddToClassList("block-item");
            image.userData = blockData;

            float width = sprite.rect.width * blockScale;
            float height = sprite.rect.height * blockScale;
            image.style.width = width;
            image.style.height = height;

            image.RegisterCallback<PointerDownEvent>(
                evt =>
                {
                    interaction.SelectBlock(image);
                }
            );

            blockMain.Add(image);
        }
    }
}