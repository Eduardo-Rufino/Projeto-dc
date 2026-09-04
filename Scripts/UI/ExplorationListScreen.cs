using Godot;
using ProjetoDC.Scripts.Data;
using ProjetoDC.Scripts.Managers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjetoDC.Scripts.UI
{
    /// <summary>Lista todas as áreas de exploração cadastradas (DatabaseManager.
    /// GetAllExplorationMaps). Escolher uma leva pra escolha do Digimon explorador
    /// (ExplorationDigimonPickScreen).</summary>
    public partial class ExplorationListScreen : Control
    {
        private VBoxContainer _itemsContainer;
        private Label _emptyLabel;
        private Button _backButton;

        private PackedScene _cardScene;

        private readonly List<ExplorationMapCard> _cards = new();

        public event Action BackPressed;
        public event Action<ExplorationMapData> MapSelected;

        public override void _Ready()
        {
            // O Center pausa a árvore inteira enquanto essa tela está aberta (ver
            // HUD.SyncPauseWithBlockingScreens) - sem isso os botões parariam de responder.
            ProcessMode = ProcessModeEnum.Always;

            _itemsContainer = GetNode<VBoxContainer>(
                "Window/MarginContainer/VBoxContainer/ItemsScroll/ItemsContainer"
            );

            _emptyLabel = GetNode<Label>("Window/MarginContainer/VBoxContainer/EmptyLabel");
            _backButton = GetNode<Button>("Window/MarginContainer/VBoxContainer/BackButton");

            _backButton.Pressed += () => BackPressed?.Invoke();

            _cardScene = GD.Load<PackedScene>("res://Scenes/Exploration/ExplorationMapCard.tscn");
        }

        public void RefreshUI()
        {
            foreach (var card in _cards)
                card.QueueFree();

            _cards.Clear();

            var maps = DatabaseManager.Instance.GetAllExplorationMaps().ToList();

            _emptyLabel.Visible = maps.Count == 0;

            foreach (var map in maps)
            {
                var card = _cardScene.Instantiate<ExplorationMapCard>();

                _itemsContainer.AddChild(card);

                card.SetMap(map);
                card.Entered += OnCardEntered;

                _cards.Add(card);
            }
        }

        private void OnCardEntered(ExplorationMapData map)
        {
            MapSelected?.Invoke(map);
        }
    }
}
