using Godot;

namespace ProjetoDC.Scripts.Systems.Battle
{
    /// <summary>
    /// Número de dano flutuante que aparece quando uma unidade toma um ataque: sobe um
    /// pouco, desaparece aos poucos, e se destrói sozinho.
    /// </summary>
    public partial class DamageIndicator : Node2D
    {
        private const float RiseDistance = 40f;
        private const double Duration = 0.9;

        private Label _label;
        private double _elapsed;
        private Vector2 _startPosition;

        public override void _Ready()
        {
            _label = GetNode<Label>("Label");
        }

        public void Initialize(Vector2 position, string text, Color color)
        {
            GlobalPosition = position;
            _startPosition = position;

            _label.Text = text;
            _label.AddThemeColorOverride("font_color", color);
        }

        public override void _Process(double delta)
        {
            _elapsed += delta;

            float t = (float)(_elapsed / Duration);

            if (t >= 1f)
            {
                QueueFree();
                return;
            }

            GlobalPosition = _startPosition + new Vector2(0, -RiseDistance * t);

            Modulate = new Color(1f, 1f, 1f, 1f - t);
        }
    }
}
