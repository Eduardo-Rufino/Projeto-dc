using Godot;
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

        private Button _buyMeatButton;
        private Button _buyMedicineButton;
        private Button _buyEggButton;



        public event Action BackPressed;

        private GameManager Game => GameManager.Instance;

        public override void _Ready()
        {
            InitializeUIComponents();
            RefreshUI();
        }

        private const int EggPrice = 500;

        private void InitializeUIComponents()
        {
            var itemsPath = "Window/MarginContainer/VBoxContainer/ItemsContainer/";

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
        }

        public void RefreshUI()
        {
            GD.Print($"Bits: {Game.Save.Center.Bits}");

            _bitsLabel.Text = $"💰 {Game.Save.Center.Bits:N0}";
            _meatLabel.Text = $"Você tem: {Game.Save.Center.Meat}";
            _medicineLabel.Text = $"Você tem: {Game.Save.Center.Medicine}";

            _buyMeatButton.Disabled = Game.Save.Center.Bits < 20;
            _buyMedicineButton.Disabled = Game.Save.Center.Bits < 100;
            _buyEggButton.Disabled = Game.Save.Center.Bits < EggPrice;
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
                GD.Print(result.Reason);
            }
        }

        private void OnBuyMedicinePressed()
        {
            var result = Game.BuyMedicine();

            if (result.Success)
                RefreshUI();
        }

        private void OnBuyBotamonEggPressed()
        {
            var result = Game.BuyEgg(25);

            if (result.Success)
            {
                RefreshUI();
            }
            else
            {
                GD.Print(result.Reason);
            }
        }

        private void OnBackPressed()
        {
            BackPressed?.Invoke();
        }
    }
}
