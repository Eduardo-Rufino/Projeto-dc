using Godot;
using ProjetoDC.Scripts.Managers;
using System;

namespace ProjetoDC.Scripts.UI
{
    /// <summary>
    /// Tela de configurações - por enquanto só o volume da música (ver MusicManager), mas é o
    /// lugar certo pra qualquer preferência futura que não seja estado de save (gráficos,
    /// idioma, etc.).
    /// </summary>
    public partial class SettingsScreen : Control
    {
        private HSlider _musicVolumeSlider;
        private Label _musicVolumeValueLabel;
        private Button _backButton;
        private CheckBox _autoTutorialsCheck;

        public event Action BackPressed;

        public override void _Ready()
        {
            // O Center pausa a árvore inteira enquanto essa tela está aberta (ver
            // HUD.SyncPauseWithBlockingScreens) - sem isso os botões parariam de responder.
            ProcessMode = ProcessModeEnum.Always;

            _musicVolumeSlider = GetNode<HSlider>(
                "Window/MarginContainer/VBoxContainer/MusicRow/MusicVolumeSlider"
            );

            _musicVolumeValueLabel = GetNode<Label>(
                "Window/MarginContainer/VBoxContainer/MusicRow/MusicVolumeValueLabel"
            );

            _musicVolumeSlider.ValueChanged += OnMusicVolumeChanged;

            _backButton = GetNode<Button>("Window/MarginContainer/VBoxContainer/BackButton");
            _backButton.Pressed += OnBackPressed;

            BuildAutoTutorialsOption();
        }

        // Opção criada em código (a janela da .tscn tem tamanho fixo, então cresce junto). Só aparece
        // dentro do jogo (Center): no menu principal o save ainda pode ser trocado pelo Novo Jogo.
        private void BuildAutoTutorialsOption()
        {
            var box = GetNode<VBoxContainer>("Window/MarginContainer/VBoxContainer");
            var musicRow = GetNode<Control>("Window/MarginContainer/VBoxContainer/MusicRow");

            _autoTutorialsCheck = new CheckBox
            {
                Text = "📖 Mostrar tutoriais ao usar algo pela 1ª vez",
                TooltipText = "Mostra o tutorial de cada tela/função na primeira vez que ela é usada. Os que você já viu não aparecem de novo - todos continuam no botão 📖.",
            };
            _autoTutorialsCheck.Toggled += OnAutoTutorialsToggled;

            box.AddChild(_autoTutorialsCheck);
            box.MoveChild(_autoTutorialsCheck, musicRow.GetIndex() + 1);

            var window = GetNode<Control>("Window");
            window.OffsetTop -= 22f;
            window.OffsetBottom += 22f;
            window.OffsetLeft -= 30f;
            window.OffsetRight += 30f;
        }

        private void OnAutoTutorialsToggled(bool enabled)
        {
            var center = GameManager.Instance?.Save?.Center;

            if (center != null)
                center.AutoTutorialsEnabled = enabled;
        }

        public void Open()
        {
            int percent = MusicManager.Instance != null
                ? Mathf.RoundToInt(MusicManager.Instance.VolumeLinear * 100f)
                : 100;

            // Só reflete o valor atual, sem disparar OnMusicVolumeChanged (que salvaria de
            // novo à toa toda vez que a tela abre).
            _musicVolumeSlider.SetValueNoSignal(percent);
            _musicVolumeValueLabel.Text = $"{percent}%";

            var center = GameManager.Instance?.Save?.Center;
            bool inGame = GetTree().CurrentScene is global::Center && center != null;
            _autoTutorialsCheck.Visible = inGame;

            if (inGame)
                _autoTutorialsCheck.SetPressedNoSignal(center.AutoTutorialsEnabled);

            Visible = true;
        }

        private void OnMusicVolumeChanged(double value)
        {
            _musicVolumeValueLabel.Text = $"{(int)value}%";

            MusicManager.Instance?.SetVolume((float)(value / 100.0));
        }

        private void OnBackPressed()
        {
            Visible = false;

            BackPressed?.Invoke();
        }
    }
}
