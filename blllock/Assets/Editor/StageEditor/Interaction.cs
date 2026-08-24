using UnityEngine.UIElements;

namespace Assets.Editor.StageEditor
{
    public class Interaction
    {
        public enum EditState
        {
            None,
            Block,
            Paint,
            Eraser,
            Move
        }

        public enum PaletteType
        {
            None,
            Background,
            Circuit,
            Node,
            HBarrier,
            VBarrier
        }

        public EditState State { get; private set; } = EditState.None;
        public PaletteType Palette { get; private set; } = PaletteType.None;
        public Image SelectedBlock { get; private set; } = null;
        public Image SelectedBrush { get; private set; } = null;
        public VisualElement SelectedPalette { get; private set; } = null;

        private const string selected = "selected";

        public void SetEditState(EditState state)
        {
            State = state;
        }

        public void SelectBlock(Image block)
        {
            if (
                State == EditState.Paint || 
                State == EditState.Eraser || 
                State == EditState.Move
            )
            {
                SelectedBrush.RemoveFromClassList(selected);
                SelectedBrush = null;
                SelectedPalette?.RemoveFromClassList(selected);
                SelectedPalette = null;
                Palette = PaletteType.None;

                SelectedBlock = block;
                SelectedBlock.AddToClassList(selected);

                SetEditState(EditState.Block);
            }

            else if (State == EditState.Block)
            {
                if (SelectedBlock == block)
                {
                    SelectedBlock.RemoveFromClassList(selected);
                    SelectedBlock = null;
                    
                    SetEditState(EditState.None);
                }
                else
                {
                    SelectedBlock.RemoveFromClassList(selected);
                    SelectedBlock = block;
                    SelectedBlock.AddToClassList(selected);
                }
            }

            else if (State == EditState.None)
            {
                SelectedBlock = block;
                SelectedBlock.AddToClassList(selected);
                SetEditState(EditState.Block);
            }
        }

        public void SelectBrush(Image brush)
        {
            EditState editState = (EditState)brush.userData;

            if (
                State == EditState.Paint || 
                State == EditState.Eraser || 
                State == EditState.Move
            )
            {
                if (State == editState)
                {
                    SelectedBrush.RemoveFromClassList(selected);
                    SelectedBrush = null;
                    SetEditState(EditState.None);
                }
                else
                {
                    SelectedBrush.RemoveFromClassList(selected);
                    SelectedBrush = brush;
                    SelectedBrush.AddToClassList(selected);
                    SetEditState(editState);
                }
                SelectedPalette?.RemoveFromClassList(selected);
                SelectedPalette = null;
                Palette = PaletteType.None;
            }

            else if (State == EditState.Block)
            {
                SelectedBlock.RemoveFromClassList(selected);
                SelectedBlock = null;
                SelectedBrush = brush;
                SelectedBrush.AddToClassList(selected);
                SetEditState(editState);
            }

            else if (State == EditState.None)
            {
                SelectedBrush = brush;
                SelectedBrush.AddToClassList(selected);
                SetEditState(editState);
            }
        }

        public void SelectPalette(VisualElement palette)
        {
            if (State != EditState.Paint) return;

            if (SelectedPalette == palette)
            {
                SelectedPalette.RemoveFromClassList(selected);
                SelectedPalette = null;
                Palette = PaletteType.None;
            }
            else if (SelectedPalette != null)
            {
                SelectedPalette.RemoveFromClassList(selected);
                SelectedPalette = palette;
                SelectedPalette.AddToClassList(selected);
                Palette = (PaletteType)palette.userData;
            }
            else
            {
                SelectedPalette = palette;
                SelectedPalette.AddToClassList(selected);
                Palette = (PaletteType)palette.userData;
            }
        }
    }
}