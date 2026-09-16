using Godot;
using System;

namespace ProjetoDC.Scripts.UI
{
    /// <summary>
    /// Menu de pausa da BattleArena (ver BattleArena.TogglePauseMenu) - permite continuar a
    /// luta ou desistir dela. Existe principalmente pro caso de dois Digimons muito tanques
    /// ficarem trocando dano por tempo indefinido sem nenhum morrer: sem essa saída, o
    /// jogador ficaria preso na arena.
    /// </summary>
    public partial class BattlePauseScreen : Control
    {
        private Control _mainPanel;
        private Control _confirmPanel;

        private Button _resumeButton;
        private Button _surrenderButton;
        private Button _confirmSurrenderButton;
        private Button _cancelSurrenderButton;

        public event Action ResumePressed;

        /// <summary>Jogador confirmou a desistência - BattleArena encerra a luta como
        /// BattleResult.EnemyWon (conta como derrota, com as mesmas consequências de perder
        /// pra valer: Felicidade, XP de consolação, etc.).</summary>
        public event Action SurrenderConfirmed;

        public override void _Ready()
        {
            SetAnchorsPreset(LayoutPreset.FullRect);
            SetOffsetsPreset(LayoutPreset.FullRect);

            _mainPanel = GetNode<Control>("CenterContainer/MainPanel");
            _confirmPanel = GetNode<Control>("CenterContainer/ConfirmPanel");

            _resumeButton = GetNode<Button>(
                "CenterContainer/MainPanel/MarginContainer/VBoxContainer/ResumeButton");

            _surrenderButton = GetNode<Button>(
                "CenterContainer/MainPanel/MarginContainer/VBoxContainer/SurrenderButton");

            _confirmSurrenderButton = GetNode<Button>(
                "CenterContainer/ConfirmPanel/MarginContainer/VBoxContainer/ConfirmButton");

            _cancelSurrenderButton = GetNode<Button>(
                "CenterContainer/ConfirmPanel/MarginContainer/VBoxContainer/CancelButton");

            ZIndex = 90;

            _resumeButton.Pressed += () => ResumePressed?.Invoke();
            _surrenderButton.Pressed += ShowConfirmSurrender;
            _cancelSurrenderButton.Pressed += ShowMainPanel;
            _confirmSurrenderButton.Pressed += () => SurrenderConfirmed?.Invoke();

            Visible = false;
        }

        public void Open()
        {
            Size = GetViewportRect().Size;
            CustomMinimumSize = Size;

            ShowMainPanel();

            Visible = true;
            MoveToFront();
        }

        public void Close()
        {
            Visible = false;
        }

        private void ShowMainPanel()
        {
            _mainPanel.Visible = true;
            _confirmPanel.Visible = false;
        }

        private void ShowConfirmSurrender()
        {
            _mainPanel.Visible = false;
            _confirmPanel.Visible = true;
        }
    }
}
