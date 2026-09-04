using Godot;
using ProjetoDC.Scripts.Data;
using ProjetoDC.Scripts.Gameplay;
using ProjetoDC.Scripts.Managers;
using System;
using System.Collections.Generic;

namespace ProjetoDC.Scripts.UI
{
    /// <summary>
    /// Escolha de 1 Digimon do roster pra explorar (ver game_idea.txt - só 1 por vez, não o
    /// time inteiro) - grid de DigimonSelectCard (mesmo card da TeamSelectionScreen), só que
    /// com seleção única via ButtonGroup em vez de até 3.
    /// </summary>
    public partial class ExplorationDigimonPickScreen : Control
    {
        private GridContainer _grid;
        private Label _warningLabel;
        private Button _exploreButton;
        private Button _backButton;

        private PackedScene _cardScene;

        private readonly List<DigimonSelectCard> _cards = new();
        private readonly ButtonGroup _group = new();

        private ExplorationMapData _map;
        private DigimonInstance _selected;

        public event Action BackPressed;

        private GameManager Game => GameManager.Instance;

        public override void _Ready()
        {
            // O Center pausa a árvore inteira enquanto essa tela está aberta (ver
            // HUD.SyncPauseWithBlockingScreens) - sem isso os botões parariam de responder.
            ProcessMode = ProcessModeEnum.Always;

            _grid = GetNode<GridContainer>("Window/MarginContainer/VBoxContainer/ScrollContainer/GridContainer");
            _warningLabel = GetNode<Label>("Window/MarginContainer/VBoxContainer/WarningLabel");
            _exploreButton = GetNode<Button>("Window/MarginContainer/VBoxContainer/HBoxContainer/ExploreButton");
            _backButton = GetNode<Button>("Window/MarginContainer/VBoxContainer/HBoxContainer/BackButton");

            _cardScene = GD.Load<PackedScene>("res://Scenes/Battle/DigimonSelectCard.tscn");

            _exploreButton.Pressed += OnExplorePressed;
            _backButton.Pressed += OnBackPressed;
        }

        public void Open(ExplorationMapData map)
        {
            _map = map;

            RefreshUI();
        }

        private void RefreshUI()
        {
            foreach (var card in _cards)
                card.QueueFree();

            _cards.Clear();
            _selected = null;

            var roster = Game.CenterService.GetAllDigimons();

            _warningLabel.Visible = roster.Count == 0;
            _warningLabel.Text = "Você não tem nenhum Digimon no Center pra explorar.";

            foreach (var digimon in roster)
            {
                var card = _cardScene.Instantiate<DigimonSelectCard>();

                _grid.AddChild(card);

                card.SetDigimon(digimon);
                card.ButtonGroup = _group;
                card.ToggledSelection += OnCardToggled;

                _cards.Add(card);
            }

            UpdateSelectionState();
        }

        private void OnCardToggled(DigimonSelectCard card)
        {
            // Não confia no ButtonPressed do card que disparou o evento nem na ordem dos
            // sinais - trocar de seleção dentro do mesmo ButtonGroup dispara Toggled(false) no
            // card antigo e Toggled(true) no novo, e o Godot não garante em qual ordem (ver
            // BaseButton/ButtonGroup) - só ler o estado real do grupo depois do toggle é
            // confiável, senão _selected ocasionalmente fica null com um card visivelmente
            // marcado, travando o botão Explorar.
            _selected = (_group.GetPressedButton() as DigimonSelectCard)?.Digimon;

            UpdateSelectionState();
        }

        private void UpdateSelectionState()
        {
            _exploreButton.Disabled = _selected == null;
        }

        private void OnExplorePressed()
        {
            if (_selected == null || _map == null)
                return;

            Visible = false;

            Game.StartExploration(_selected, _map);
        }

        private void OnBackPressed()
        {
            BackPressed?.Invoke();
        }
    }
}
