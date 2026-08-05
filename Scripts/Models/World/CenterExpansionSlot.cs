using Godot;

namespace ProjetoDC.Scripts.Models.World
{
    public class CenterExpansionSlot
    {
        public Vector2I GridPosition { get; }

        public CenterExpansionSlot(Vector2I gridPosition)
        {
            GridPosition = gridPosition;
        }
    }
}