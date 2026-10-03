using Godot;
using ProjetoDC.Enums;
using ProjetoDC.Scripts.Gameplay;
using ProjetoDC.Scripts.Managers;
using System;
using System.Collections.Generic;

namespace ProjetoDC.Scripts.UI
{
    /// <summary>
    /// Tela de seleção de 2 Digimons pra tentar uma fusão Jogress (ver
    /// GameManager.TryToFuse) - mesmo padrão de TeamSelectionScreen reaproveitando
    /// DigimonSelectCard, só que com seleção máxima de 2 em vez de 1/3. Nada muta no
    /// Center até o clique de confirmar - fechar a tela a qualquer momento não perde
    /// nenhum dos dois candidatos.
    /// </summary>
    public partial class JogressPickScreen : Control
    {
        private const int RequiredSelection = 2;

        private GridContainer _grid;
        private Label _previewLabel;
        private Button _confirmButton;
        private Button _backButton;

        // Linha principal da fusão (passivas herdadas): índice em _selected do pai cuja linha vem
        // primeiro no histórico do fundido. Botão criado em código, só aparece com uma receita válida.
        private Button _principalButton;
        private int _principalIndex;

        private PackedScene _cardScene;

        private readonly List<DigimonSelectCard> _cards = new();
        private readonly List<DigimonInstance> _selected = new();

        public event Action BackPressed;

        private GameManager Game => GameManager.Instance;

        public override void _Ready()
        {
            // O Center pausa a árvore inteira enquanto essa tela está aberta (ver
            // HUD.SyncPauseWithBlockingScreens) - sem isso os botões parariam de responder.
            ProcessMode = ProcessModeEnum.Always;

            _grid = GetNode<GridContainer>("Window/MarginContainer/VBoxContainer/ScrollContainer/GridContainer");
            _previewLabel = GetNode<Label>("Window/MarginContainer/VBoxContainer/PreviewLabel");
            _confirmButton = GetNode<Button>("Window/MarginContainer/VBoxContainer/HBoxContainer/ConfirmButton");
            _backButton = GetNode<Button>("Window/MarginContainer/VBoxContainer/HBoxContainer/BackButton");

            _cardScene = GD.Load<PackedScene>("res://Scenes/Battle/DigimonSelectCard.tscn");

            _confirmButton.Pressed += OnConfirmPressed;
            _backButton.Pressed += OnBackPressed;

            _principalButton = new Button
            {
                Visible = false,
                CustomMinimumSize = new Vector2(220, 0),
                TooltipText = "Linha principal: a linha evolutiva desse Digimon vem primeiro no histórico do fundido, " +
                              "e a do outro entra em seguida. Clique pra trocar.",
            };
            _principalButton.AddThemeStyleboxOverride("normal", _backButton.GetThemeStylebox("normal"));
            _principalButton.Pressed += OnPrincipalPressed;

            var buttons = _confirmButton.GetParent();
            buttons.AddChild(_principalButton);
            buttons.MoveChild(_principalButton, _confirmButton.GetIndex());
        }

        public void Open()
        {
            RefreshUI();

            Visible = true;
        }

        private void RefreshUI()
        {
            foreach (var card in _cards)
                card.QueueFree();

            _cards.Clear();
            _selected.Clear();
            _principalIndex = 0;

            foreach (var digimon in Game.CenterService.GetAllDigimons())
            {
                var card = _cardScene.Instantiate<DigimonSelectCard>();

                _grid.AddChild(card);

                card.SetDigimon(digimon);
                card.ToggledSelection += OnCardToggled;

                _cards.Add(card);
            }

            UpdatePreview();
        }

        private void OnCardToggled(DigimonSelectCard card)
        {
            if (card.ButtonPressed)
            {
                if (_selected.Count >= RequiredSelection)
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

            _principalIndex = 0;
            UpdatePreview();
        }

        private void UpdatePreview()
        {
            _principalButton.Visible = false;

            if (_selected.Count != RequiredSelection)
            {
                _previewLabel.Text = "Selecione 2 Digimons pra tentar uma fusão Jogress.";
                _confirmButton.Disabled = true;
                return;
            }

            var digimonA = _selected[0];
            var digimonB = _selected[1];

            var recipe = DatabaseManager.Instance.GetJogressRecipe(
                digimonA.BaseData.Id,
                digimonB.BaseData.Id
            );

            if (recipe == null)
            {
                _previewLabel.Text = "Essa combinação não corresponde a nenhuma fusão Jogress conhecida.";
                _confirmButton.Disabled = true;
                return;
            }

            var resultForm = DatabaseManager.Instance.GetDigimon(recipe.ToDigimonId);

            int requiredCapacity = DigimonInstance.GetCapacityCostForStage(resultForm.Stage);
            int delta = requiredCapacity - (digimonA.CapacityCost + digimonB.CapacityCost);
            bool hasCapacity = Game.Save.Center.CapacityUsed + delta <= Game.Save.Center.CapacityLimit;

            _previewLabel.Text = hasCapacity
                ? $"{digimonA.DisplayName} + {digimonB.DisplayName} → {resultForm.Name}"
                : $"{resultForm.Name} exige mais capacidade do que o Center tem livre.";

            _confirmButton.Disabled = !hasCapacity;

            _principalButton.Visible = true;
            _principalButton.Text = $"Linha principal: {_selected[_principalIndex].DisplayName}";
        }

        private void OnConfirmPressed()
        {
            if (_selected.Count != RequiredSelection)
                return;

            var principal = _selected[_principalIndex];
            var secondary = _selected[1 - _principalIndex];

            var attempt = Game.TryToFuse(principal, secondary);

            if (attempt.Outcome == JogressOutcome.Fused)
            {
                Visible = false;
                return;
            }

            // BlockedByCapacity/NotEligible aqui significaria que o estado mudou desde a
            // prévia - não deveria acontecer (a árvore fica pausada com essa tela aberta),
            // mas nada muda de qualquer forma: os dois selecionados continuam no Center.
            UpdatePreview();
        }

        private void OnPrincipalPressed()
        {
            _principalIndex = 1 - _principalIndex;
            UpdatePreview();
        }

        private void OnBackPressed()
        {
            BackPressed?.Invoke();
        }
    }
}
