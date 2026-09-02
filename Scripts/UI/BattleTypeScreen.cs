using Godot;
using System;

namespace ProjetoDC.Scripts.UI
{
    /// <summary>Primeira tela ao clicar no troféu: escolher entre Campeonato (lista de
    /// batalhas fixas pré-definidas, ver TournamentListScreen) ou Batalha Livre (time
    /// inimigo gerado e calibrado pelo poder do time do jogador, o fluxo que já existia).</summary>
    public partial class BattleTypeScreen : Control
    {
        private Button _tournamentButton;
        private Button _freeBattleButton;
        private Button _backButton;

        public event Action TournamentSelected;
        public event Action FreeBattleSelected;
        public event Action BackPressed;

        public override void _Ready()
        {
            // O Center pausa a árvore inteira enquanto essa tela está aberta (ver
            // HUD.SyncPauseWithBlockingScreens) - sem isso os botões parariam de responder.
            ProcessMode = ProcessModeEnum.Always;

            _tournamentButton = GetNode<Button>(
                "Window/MarginContainer/VBoxContainer/TournamentButton"
            );

            _freeBattleButton = GetNode<Button>(
                "Window/MarginContainer/VBoxContainer/FreeBattleButton"
            );

            _backButton = GetNode<Button>(
                "Window/MarginContainer/VBoxContainer/BackButton"
            );

            _tournamentButton.Pressed += () => TournamentSelected?.Invoke();
            _freeBattleButton.Pressed += () => FreeBattleSelected?.Invoke();
            _backButton.Pressed += () => BackPressed?.Invoke();
        }
    }
}
