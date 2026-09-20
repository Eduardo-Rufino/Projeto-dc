using Godot;

namespace ProjetoDC.Scripts.Systems.Audit
{
    /// <summary>
    /// Só pro teste de longa duração (DEBUG_SOAK): fecha sozinho os diálogos que pausam o jogo esperando o
    /// jogador ("Um Digimon faleceu" -> Entendido; evolução sem capacidade -> Manter sem evoluir), pra a
    /// simulação do Center seguir por dias de jogo sem ninguém na frente da tela.
    /// </summary>
    public partial class SoakAutoDismiss : Node
    {
        private double _timer;
        private int _dismissed;

        public override void _Ready()
        {
            ProcessMode = ProcessModeEnum.Always;
        }

        public override void _Process(double delta)
        {
            _timer += delta;

            if (_timer < 0.25)
                return;

            _timer = 0;
            Press(GetTree().Root);
        }

        private void Press(Node node)
        {
            foreach (var child in node.GetChildren())
            {
                if (child is Button b && b.IsVisibleInTree() && !b.Disabled && (b.Text == "Entendido" || b.Text == "Manter sem evoluir"))
                {
                    _dismissed++;
                    GD.Print($"[SOAK] fechando diálogo '{b.Text}' (#{_dismissed})");
                    b.EmitSignal(BaseButton.SignalName.Pressed);
                    return;
                }

                Press(child);
            }
        }
    }
}
