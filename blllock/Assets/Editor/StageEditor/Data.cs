using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Editor.StageEditor
{
    public enum ItemType
    {
        None,
        Block,
        Node,
        HBarrier,
        VBarrier,
        Circuit,
        Background
    }

    public abstract class Item
    {
        public abstract ItemType Type { get; }
        public Vector2Int Pos { get; private set; }
        public void SetPos(Vector2Int pos) => this.Pos = pos;
    }

    public class ItemBlock : Item
    {
        // Common
        public override ItemType Type { get; } = ItemType.Block;

        // Original
        public int ID { get; private set; }
        public ItemBlock(int id)
        {
            this.ID = id;
        }

        public bool CanRotateCW { get; private set; } = false;
        public bool CanRotateCCW { get; private set; } = false;
        public bool CanFlipX { get; private set; } = false;
        public bool CanFlipY { get; private set; } = false;
        public void SetCanRotateCW(bool canRotateCW) => this.CanRotateCW = canRotateCW;
        public void SetCanRotateCCW(bool canRotateCCW) => this.CanRotateCCW = canRotateCCW;
        public void SetCanFlipX(bool canFlipX) => this.CanFlipX = canFlipX;
        public void SetCanFlipY(bool canFlipY) => this.CanFlipY = canFlipY;
    }

    public class ItemNode : Item
    {
        // Common
        public override ItemType Type { get; } = ItemType.Node;

        // Original
        public bool IsInput { get; private set; }
        public void SetIsInput(bool isInput) => this.IsInput = isInput;
        public string Expr { get; private set; }
        public void SetExpr(string expr) => this.Expr = expr;
    }

    public class ItemHBarrier : Item
    {
        public override ItemType Type { get; } = ItemType.HBarrier;
    }

    public class ItemVBarrier : Item
    {
        public override ItemType Type { get; } = ItemType.VBarrier;
    }

    public class ItemCircuit : Item
    {
        public override ItemType Type { get; } = ItemType.Circuit;
    }

    public class ItemBackground : Item
    {
        public override ItemType Type { get; } = ItemType.Background;
    }

}