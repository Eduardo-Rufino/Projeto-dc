using Godot;
using ProjetoDC.Scripts.Data;
using ProjetoDC.Scripts.Managers;

namespace ProjetoDC.Scripts.Gameplay
{
    /// <summary>
    /// Item colecionável no chão de uma área de exploração - clicar soma no inventário do
    /// Center (ver GameManager.CollectExplorationItem) e some pra sempre daquele save (não
    /// respawna, diferente dos selvagens).
    /// </summary>
    public partial class ExplorationItemVisual : Node2D
    {
        private ItemPickupData _pickup;

        public override void _Ready()
        {
            var clickArea = GetNode<Area2D>("ClickArea");
            clickArea.InputEvent += OnClickAreaInputEvent;
        }

        public void Initialize(ItemPickupData pickup, Texture2D icon)
        {
            _pickup = pickup;

            var iconRect = GetNodeOrNull<TextureRect>("Icon");

            if (iconRect != null && icon != null)
                iconRect.Texture = icon;
        }

        private void OnClickAreaInputEvent(Node viewport, InputEvent @event, long shapeIdx)
        {
            if (@event is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
                return;

            GetViewport().SetInputAsHandled();

            var result = GameManager.Instance.CollectExplorationItem(_pickup);

            if (result.Success)
                QueueFree();
        }
    }
}
