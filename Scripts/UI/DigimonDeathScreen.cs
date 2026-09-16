using Godot;
using System.Collections.Generic;

namespace ProjetoDC.Scripts.UI
{
    /// <summary>
    /// Aviso mostrado quando um Digimon morre de vez (velhice ou maus-tratos - ver
    /// GameManager.DigimonDied/KillDigimon). Enfileira mensagens em vez de sobrepor telas se
    /// mais de uma morte acontecer perto uma da outra (ex.: dois Digimons de velhice no
    /// mesmo dia), mostrando uma de cada vez.
    /// </summary>
    public partial class DigimonDeathScreen : Control
    {
        private Label _messageLabel;
        private Button _okButton;

        private readonly Queue<string> _pendingMessages = new();

        public override void _Ready()
        {
            // O Center pausa a árvore inteira enquanto essa tela está aberta (ver
            // HUD.SyncPauseWithBlockingScreens) - sem isso o botão pararia de responder.
            ProcessMode = ProcessModeEnum.Always;

            _messageLabel = GetNode<Label>("Window/MarginContainer/VBoxContainer/MessageLabel");
            _okButton = GetNode<Button>("Window/MarginContainer/VBoxContainer/OkButton");

            _okButton.Pressed += OnOkPressed;
        }

        public void Notify(string message)
        {
            _pendingMessages.Enqueue(message);

            if (!Visible)
                ShowNext();
        }

        private void ShowNext()
        {
            if (_pendingMessages.Count == 0)
            {
                Visible = false;
                return;
            }

            _messageLabel.Text = _pendingMessages.Dequeue();
            Visible = true;
        }

        private void OnOkPressed()
        {
            ShowNext();
        }
    }
}
