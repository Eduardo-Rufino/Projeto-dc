using Godot;
using ProjetoDC.Enums;
using ProjetoDC.Scripts.Data;
using ProjetoDC.Scripts.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjetoDC.Scripts.UI
{
    public partial class ShopScreen : Control
    {
        private Label _bitsLabel;
        private Label _meatLabel;
        private Label _medicineLabel;
        private Label _messageLabel;

        private Button _buyMeatButton;
        private Button _buyMedicineButton;
        private Button _buyEggButton;

        private readonly Dictionary<CenterAreaType, Button> _buyAreaButtons = new();

        public event Action BackPressed;
        public event Action<EggData> EggPurchased;

        /// <summary>Disparado quando uma área é comprada com sucesso - o Center escuta isso
        /// pra fechar a loja e entrar no modo de posicionamento (ver Center.StartAreaPlacement).</summary>
        public event Action<CenterAreaType> AreaPurchased;

        private GameManager Game => GameManager.Instance;

        public override void _Ready()
        {
            // O Center pausa a árvore inteira enquanto essa tela está aberta (ver
            // HUD.SyncPauseWithBlockingScreens) - sem isso os botões parariam de responder.
            ProcessMode = ProcessModeEnum.Always;

            InitializeUIComponents();
            RefreshUI();
        }

        private const int EggPrice = 500;

        private static readonly (CenterAreaType Type, string RowName)[] AreaRows =
        {
            (CenterAreaType.Training, "TrainingAreaRow"),
            (CenterAreaType.TrainingHealthPoints, "TrainingHpAreaRow"),
            (CenterAreaType.TrainingAttack, "TrainingAttackAreaRow"),
            (CenterAreaType.TrainingDefense, "TrainingDefenseAreaRow"),
            (CenterAreaType.TrainingSpecialAttack, "TrainingSpecialAttackAreaRow"),
            (CenterAreaType.TrainingSpecialDefense, "TrainingSpecialDefenseAreaRow"),
            (CenterAreaType.TrainingSpeed, "TrainingSpeedAreaRow"),
            (CenterAreaType.Dormitory, "DormitoryAreaRow"),
            (CenterAreaType.Restaurant, "RestaurantAreaRow"),
            (CenterAreaType.Hospital, "HospitalAreaRow"),
        };

        private void InitializeUIComponents()
        {
            var itemsPath = "Window/MarginContainer/VBoxContainer/ItemsScroll/ItemsContainer/";

            _bitsLabel = GetNodeOrNull<Label>(
                "Window/MarginContainer/VBoxContainer/HeaderRow/BitsLabel"
            );

            _meatLabel = GetNodeOrNull<Label>(
                $"{itemsPath}MeatRow/Pad/HBoxContainer/InfoContainer/MeatLabel"
            );

            _medicineLabel = GetNodeOrNull<Label>(
                $"{itemsPath}MedicineRow/Pad/HBoxContainer/InfoContainer/MedicineLabel"
            );

            _buyMeatButton = GetNodeOrNull<Button>(
                $"{itemsPath}MeatRow/Pad/HBoxContainer/BuyMeatButton"
            );

            _buyMedicineButton = GetNodeOrNull<Button>(
                $"{itemsPath}MedicineRow/Pad/HBoxContainer/BuyMedicineButton"
            );

            _buyEggButton = GetNodeOrNull<Button>(
                $"{itemsPath}EggRow/Pad/HBoxContainer/BuyEggButton"
            );

            _messageLabel = GetNodeOrNull<Label>(
                "Window/MarginContainer/VBoxContainer/MessageLabel"
            );

            // Meat/Medicine/Egg já são ligados via [connection] no .tscn - só os botões de
            // área (dinâmicos, um por CenterAreaType) precisam de ligação por código aqui.
            foreach (var (areaType, rowName) in AreaRows)
            {
                var button = GetNodeOrNull<Button>(
                    $"{itemsPath}{rowName}/Pad/HBoxContainer/BuyAreaButton"
                );

                button.Pressed += () => OnBuyAreaPressed(areaType);

                _buyAreaButtons[areaType] = button;
            }
        }

        public void RefreshUI()
        {
            GD.Print($"Bits: {Game.Save.Center.Bits}");

            _bitsLabel.Text = $"💰 {Game.Save.Center.Bits:N0}";
            _meatLabel.Text = $"Você tem: {Game.Save.Center.Meat}";
            _medicineLabel.Text = $"Você tem: {Game.Save.Center.Medicine}";

            _buyMeatButton.Disabled = Game.Save.Center.Bits < 20;
            _buyMedicineButton.Disabled = Game.Save.Center.Bits < 100;
            _buyEggButton.Disabled = Game.Save.Center.Bits < EggPrice || !Game.HasCapacityForEgg();

            foreach (var (areaType, button) in _buyAreaButtons)
                button.Disabled = Game.Save.Center.Bits < GameManager.GetAreaPrice(areaType);
        }

        /// <summary>Mostra o motivo de uma compra ter falhado (ex.: "Não há capacidade
        /// suficiente") por alguns segundos, em vez de só imprimir no log - sem isso o
        /// jogador clicava em Comprar e nada parecia acontecer.</summary>
        private void ShowMessage(string text)
        {
            if (_messageLabel == null)
                return;

            _messageLabel.Text = text;
            _messageLabel.Visible = true;

            GetTree().CreateTimer(3.0).Timeout += () =>
            {
                if (IsInstanceValid(_messageLabel))
                    _messageLabel.Visible = false;
            };
        }

        private void OnBuyMeatPressed()
        {
            var result = Game.BuyMeat();

            if (result.Success)
            {
                RefreshUI();
            }
            else
            {
                ShowMessage(result.Reason);
            }
        }

        private void OnBuyMedicinePressed()
        {
            var result = Game.BuyMedicine();

            if (result.Success)
                RefreshUI();
            else
                ShowMessage(result.Reason);
        }

        private void OnBuyEggPressed()
        {
            var result = Game.BuyEgg();

            if (result.Success)
            {
                RefreshUI();

                EggPurchased?.Invoke(Game.Save.Center.Eggs.Last());
            }
            else
            {
                ShowMessage(result.Reason);
            }
        }

        private void OnBuyAreaPressed(CenterAreaType areaType)
        {
            var result = Game.BuyArea(areaType);

            if (result.Success)
            {
                RefreshUI();

                AreaPurchased?.Invoke(areaType);
            }
            else
            {
                ShowMessage(result.Reason);
            }
        }

        private void OnBackPressed()
        {
            BackPressed?.Invoke();
        }
    }
}
