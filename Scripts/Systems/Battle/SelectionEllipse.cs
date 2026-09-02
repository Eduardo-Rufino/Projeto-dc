using Godot;

namespace ProjetoDC.Scripts.Systems.Battle
{
    /// <summary>Elipse achatada desenhada aos pés de um BattleUnit, como um anel de
    /// "selecionado" no chão, na mesma cor da barra de vida (verde time do jogador,
    /// vermelho inimigo) - só um indicador visual de lado, sem lógica própria.</summary>
    public partial class SelectionEllipse : Node2D
    {
        [Export]
        public float RadiusX { get; set; } = 26f;

        [Export]
        public float RadiusY { get; set; } = 10f;

        [Export]
        public float LineWidth { get; set; } = 2.5f;

        private Color _color = Colors.White;

        public void SetColor(Color color)
        {
            _color = color;

            QueueRedraw();
        }

        public override void _Draw()
        {
            const int segments = 32;

            var points = new Vector2[segments + 1];

            for (int i = 0; i <= segments; i++)
            {
                float angle = Mathf.Tau * i / segments;

                points[i] = new Vector2(
                    Mathf.Cos(angle) * RadiusX,
                    Mathf.Sin(angle) * RadiusY
                );
            }

            DrawPolyline(points, _color, LineWidth, true);
        }
    }
}
