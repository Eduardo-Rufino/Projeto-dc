using Godot;
using ProjetoDC.Scripts.Data;

namespace ProjetoDC.Scripts.Gameplay
{
    /// <summary>
    /// Item colecionável no chão de uma área de exploração. O clique só é avisado (Clicked): a
    /// ExplorationArea confere se o explorador está perto, coleta (GameManager.
    /// CollectExplorationItem - não respawna, diferente dos selvagens) e some com o item ou,
    /// no baú do mapa isométrico, deixa ele aberto.
    /// </summary>
    public partial class ExplorationItemVisual : Node2D
    {
        private ItemPickupData _pickup;
        private Area2D _clickArea;

        public event System.Action<ExplorationItemVisual> Clicked;

        public ItemPickupData Pickup => _pickup;

        public override void _Ready()
        {
            _clickArea = GetNode<Area2D>("ClickArea");
            _clickArea.InputEvent += OnClickAreaInputEvent;
        }

        /// <summary>Liga/desliga o clique (baú já aberto não reage mais).</summary>
        public void SetClickable(bool clickable) => _clickArea.InputPickable = clickable;

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
            Clicked?.Invoke(this);
        }
    }
}
