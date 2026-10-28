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

        // Linha da cozinha (criada por código): só aparece com o Meramon recrutado - ver
        // GameManager.CookMeat.
        private HBoxContainer _kitchenRow;
        private Label _kitchenLabel;
        private Button _cookOneButton;
        private Button _cookAllButton;

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

            BuildKitchenRow();

            _cardScene = GD.Load<PackedScene>("res://Scenes/Center/InventoryItemCard.tscn");
        }

        public void RefreshUI()
        {
            foreach (var card in _cards)
                card.QueueFree();

            _cards.Clear();

            // Só o que o jogador tem de verdade: item zerado na lista só confundia.
            var ownedItems = DatabaseManager.Instance.GetAllItems()
                .Where(item => Game.Save.Center.GetItemQuantity(item.Id) > 0)
                .ToList();

            _emptyLabel.Visible = ownedItems.Count == 0;

            RefreshKitchenRow();

            foreach (var item in ownedItems)
            {
                int quantity = Game.Save.Center.GetItemQuantity(item.Id);

                var card = _cardScene.Instantiate<InventoryItemCard>();

                _itemsContainer.AddChild(card);

                card.SetItem(item, quantity);

                _cards.Add(card);
            }
        }

        private void BuildKitchenRow()
        {
            var column = _backButton.GetParent();

            _kitchenRow = new HBoxContainer { Visible = false };
            _kitchenRow.AddThemeConstantOverride("separation", 10);

            _kitchenLabel = new Label
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                VerticalAlignment = VerticalAlignment.Center,
                // Sem quebra de linha o texto define a largura mínima da linha e empurra a janela toda.
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                CustomMinimumSize = new Vector2(120, 0),
                TooltipText =
                    $"O Meramon assa a Carne crua do estoque por {GameManager.CookingPricePerMeat} Bits cada e leva {GameManager.CookingMinutes} minutos " +
                    "(a encomenda toda de uma vez). A Carne Assada sacia mais fome e demora mais pra estragar (no Refeitório, ainda mais).",
            };

            _cookOneButton = new Button { Text = "Assar 1" };
            _cookAllButton = new Button { Text = "Assar todas" };

            _cookOneButton.Pressed += () => OnCookPressed(1);
            _cookAllButton.Pressed += () => OnCookPressed(int.MaxValue);

            _kitchenRow.AddChild(_kitchenLabel);
            _kitchenRow.AddChild(_cookOneButton);
            _kitchenRow.AddChild(_cookAllButton);

            column.AddChild(_kitchenRow);

            // Logo acima do separador que antecede o botão Voltar.
            column.MoveChild(_kitchenRow, _backButton.GetIndex() - 1);
        }

        private void RefreshKitchenRow()
        {
            _kitchenRow.Visible = Game.HasRecruitedKitchenCook;

            if (!_kitchenRow.Visible)
                return;

            var center = Game.Save.Center;
            int meat = center.Meat;

            if (center.IsCooking)
            {
                _kitchenLabel.Text =
                    $"🔥 Meramon assando {center.CookingMeatAmount} Carne(s)... faltam {center.CookingMinutesLeft} min";
            }
            else
            {
                _kitchenLabel.Text =
                    $"🔥 Meramon: {meat} Carne(s) crua(s) - {GameManager.CookingPricePerMeat} Bits cada, " +
                    $"{GameManager.CookingMinutes} min";
            }

            // Uma encomenda por vez, e cada Carne custa Bits: sem Carne, sem dinheiro ou com o fogão
            // ocupado os botões ficam desligados.
            bool canCook = !center.IsCooking && meat > 0 && center.Bits >= GameManager.CookingPricePerMeat;

            int allAmount = Math.Min(meat, center.Bits / GameManager.CookingPricePerMeat);

            _cookOneButton.Disabled = !canCook;
            _cookAllButton.Disabled = !canCook;
            _cookOneButton.TooltipText = $"Assar 1 Carne por {GameManager.CookingPricePerMeat} Bits";
            _cookAllButton.TooltipText =
                $"Assar {allAmount} Carne(s) por {allAmount * GameManager.CookingPricePerMeat} Bits (tudo o que o estoque e os Bits permitem)";
        }

        private void OnCookPressed(int amount)
        {
            Game.CookMeat(amount);

            RefreshUI();
        }

        private void OnBackPressed()
        {
            BackPressed?.Invoke();
        }
    }
}
