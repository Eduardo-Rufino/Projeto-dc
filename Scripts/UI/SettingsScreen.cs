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
