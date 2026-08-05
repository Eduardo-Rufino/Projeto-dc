using Godot;
using ProjetoDC.Enums;
using ProjetoDC.Scripts.World;

namespace ProjetoDC.Scripts.Models.World
{
    public partial class CenterArea : Node2D
    {
        public CenterAreaType AreaType { get; private set; }

        [Export]
        public Vector2I GridPosition { get; set; } = Vector2I.Zero;

        public override void _Ready()
        {
            AreaType = CenterAreaType.Neutral;

            Position = CenterGrid.GridToWorld(GridPosition);

            GD.Print(
                $"CenterArea criada: {Name} | Tipo {AreaType} | Grid: {GridPosition} | Position: {Position}"
            );

            var neighbors = CenterGrid.GetNeighbors(GridPosition);

            foreach (var neighbor in neighbors)
            {
                GD.Print($"Vizinhos possíveis: {neighbor}");
            }
        }
    }
}