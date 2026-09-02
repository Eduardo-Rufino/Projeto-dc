using Godot;
using ProjetoDC.Scripts.Managers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjetoDC.Scripts.UI
{
    /// <summary>
    /// Mostra todos os itens conhecidos (DatabaseManager.GetAllItems) e a quantidade que o
    /// jogador tem de cada um (CenterState.Inventory) - ícone, nome, quantidade e descrição.
    /// Puramente informativo por enquanto (sem usar/descartar itens por aqui).
    /// </summary>
    public partial class InventoryScreen : Control
    {
        private GridContainer _itemsContainer;
        private Label _emptyLabel;
        private Button _backButton;

        private PackedScene _cardScene;

        private readonly List<InventoryItemCard> _cards = new();

        public event Action BackPressed;

        private GameManager Game => GameManager.Instance;

        public override void _Ready()
        {
            // O Center pausa a árvore inteira enquanto essa tela está aberta (ver
            // HUD.SyncPauseWithBlockingScreens) - sem isso os botões parariam de responder.
            ProcessMode = ProcessModeEnum.Always;

            _itemsContainer = GetNode<GridContainer>(
                "Window/MarginContainer/VBoxContainer/ItemsScroll/ItemsContainer"
            );

            _emptyLabel = GetNode<Label>(
                "Window/MarginContainer/VBoxContainer/EmptyLabel"
            );

            _backButton = GetNode<Button>(
                "Window/MarginContainer/VBoxContainer/BackButton"
            );

            _backButton.Pressed += OnBackPressed;

            _cardScene = GD.Load<PackedScene>("res://Scenes/Center/InventoryItemCard.tscn");
        }

        public void RefreshUI()
        {
            foreach (var card in _cards)
                card.QueueFree();

            _cards.Clear();

            var allItems = DatabaseManager.Instance.GetAllItems().ToList();

            _emptyLabel.Visible = allItems.Count == 0;

            foreach (var item in allItems)
            {
                int quantity = Game.Save.Center.GetItemQuantity(item.Id);

                var card = _cardScene.Instantiate<InventoryItemCard>();

                _itemsContainer.AddChild(card);

                card.SetItem(item, quantity);

                _cards.Add(card);
            }
        }

        private void OnBackPressed()
        {
            BackPressed?.Invoke();
        }
    }
}
