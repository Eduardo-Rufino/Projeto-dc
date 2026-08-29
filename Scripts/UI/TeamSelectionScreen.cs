using Godot;
using ProjetoDC.Scripts.Gameplay;
using ProjetoDC.Scripts.Managers;
using System;
using System.Collections.Generic;

namespace ProjetoDC.Scripts.UI
{
    /// <summary>
    /// Tela de montagem do time de batalha 3x3: o jogador escolhe livremente 3 Digimons do
    /// roster do Center, sem restrição de Role. Substitui a antiga EnemySelectionScreen
    /// (que só deixava escolher 1 inimigo pra um duelo 1x1).
    /// </summary>
    public partial class TeamSelectionScreen : Control
    {
        private const int TeamSize = 3;

        private GridContainer _grid;
        private Label _countLabel;
        private Label _warningLabel;
        private Button _battleButton;
        private Button _backButton;

        private PackedScene _cardScene;

        private readonly List<DigimonSelectCard> _cards = new();
        private readonly List<DigimonInstance> _selected = new();

        public event Action BackPressed;

        private GameManager Game => GameManager.Instance;

        public override void _Ready()
        {
            _grid = GetNode<GridContainer>("VBoxContainer/ScrollContainer/GridContainer");
            _countLabel = GetNode<Label>("VBoxContainer/CountLabel");
            _warningLabel = GetNode<Label>("VBoxContainer/WarningLabel");
            _battleButton = GetNode<Button>("VBoxContainer/HBoxContainer/BattleButton");
            _backButton = GetNode<Button>("VBoxContainer/HBoxContainer/BackButton");

            _cardScene = GD.Load<PackedScene>("res://Scenes/Battle/DigimonSelectCard.tscn");

            _battleButton.Pressed += OnBattlePressed;
            _backButton.Pressed += OnBackPressed;
        }

        public void RefreshUI()
        {
            foreach (var card in _cards)
                card.QueueFree();

            _cards.Clear();
            _selected.Clear();

            var roster = Game.CenterService.GetAllDigimons();

            _warningLabel.Visible = roster.Count < TeamSize;

            foreach (var digimon in roster)
            {
                var card = _cardScene.Instantiate<DigimonSelectCard>();

                _grid.AddChild(card);

                card.SetDigimon(digimon);
                card.ToggledSelection += OnCardToggled;

                _cards.Add(card);
            }

            UpdateSelectionState();
        }

        private void OnCardToggled(DigimonSelectCard card)
        {
            if (card.ButtonPressed)
            {
                if (_selected.Count >= TeamSize)
                {
                    card.SetPressedNoSignal(false);
                    return;
                }

                _selected.Add(card.Digimon);
            }
            else
            {
                _selected.Remove(card.Digimon);
            }

            UpdateSelectionState();
        }

        private void UpdateSelectionState()
        {
            _countLabel.Text = $"{_selected.Count}/{TeamSize} selecionados";

            _battleButton.Disabled = _selected.Count != TeamSize;
        }

        private void OnBattlePressed()
        {
            if (_selected.Count != TeamSize)
                return;

            Game.StartTeamBattle(new List<DigimonInstance>(_selected));

            Visible = false;
        }

        private void OnBackPressed()
        {
            BackPressed?.Invoke();
        }
    }
}
