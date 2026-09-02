using Godot;
using ProjetoDC.Scripts.Data;
using ProjetoDC.Scripts.Managers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjetoDC.Scripts.UI
{
    /// <summary>Lista todos os campeonatos cadastrados (DatabaseManager.GetAllTournaments) -
    /// cada um mostra os oponentes fixos, nível recomendado e recompensa. Escolher um leva
    /// pra montagem de time (TeamSelectionScreen) com o tamanho travado no formato do
    /// campeonato.</summary>
    public partial class TournamentListScreen : Control
    {
        private VBoxContainer _itemsContainer;
        private Label _emptyLabel;
        private Button _backButton;

        private PackedScene _cardScene;

        private readonly List<TournamentCard> _cards = new();

        public event Action BackPressed;
        public event Action<TournamentData> TournamentSelected;

        private GameManager Game => GameManager.Instance;

        public override void _Ready()
        {
            // O Center pausa a árvore inteira enquanto essa tela está aberta (ver
            // HUD.SyncPauseWithBlockingScreens) - sem isso os botões parariam de responder.
            ProcessMode = ProcessModeEnum.Always;

            _itemsContainer = GetNode<VBoxContainer>(
                "Window/MarginContainer/VBoxContainer/ItemsScroll/ItemsContainer"
            );

            _emptyLabel = GetNode<Label>(
                "Window/MarginContainer/VBoxContainer/EmptyLabel"
            );

            _backButton = GetNode<Button>(
                "Window/MarginContainer/VBoxContainer/BackButton"
            );

            _backButton.Pressed += () => BackPressed?.Invoke();

            _cardScene = GD.Load<PackedScene>("res://Scenes/Battle/TournamentCard.tscn");
        }

        public void RefreshUI()
        {
            foreach (var card in _cards)
                card.QueueFree();

            _cards.Clear();

            var tournaments = DatabaseManager.Instance.GetAllTournaments().ToList();

            _emptyLabel.Visible = tournaments.Count == 0;

            foreach (var tournament in tournaments)
            {
                var card = _cardScene.Instantiate<TournamentCard>();

                _itemsContainer.AddChild(card);

                bool cleared = Game.Save.Center.ClearedTournamentIds.Contains(tournament.Id);

                card.SetTournament(tournament, cleared);
                card.Challenged += OnCardChallenged;

                _cards.Add(card);
            }
        }

        private void OnCardChallenged(TournamentData tournament)
        {
            TournamentSelected?.Invoke(tournament);
        }
    }
}
