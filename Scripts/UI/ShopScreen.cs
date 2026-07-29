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

        private void InitializeUIComponents()
        {
            var basePath = "MarginContainer/VBoxContainer/";

            _meatLabel = GetNodeOrNull<Label>($"{basePath}MeatLabel");
            _medicineLabel = GetNodeOrNull<Label>($"{basePath}MedicineLabel");
            _bitsLabel = GetNodeOrNull<Label>($"{basePath}BitsLabel");

            _buyMeatButton = GetNodeOrNull<Button>($"{basePath}BuyMeatButton");
            _buyMedicineButton = GetNodeOrNull<Button>($"{basePath}BuyMedicineButton");
            _buyEggButton = GetNodeOrNull<Button>($"{basePath}BuyEggButton");

        }

        public void RefreshUI()
        {
            GD.Print($"Bits: {Game.Save.Center.Bits}");

            _bitsLabel.Text = $"Bits: {Game.Save.Center.Bits}";
            _meatLabel.Text = $"Meat: {Game.Save.Center.Meat}";
            _medicineLabel.Text = $"Medicine: {Game.Save.Center.Medicine}";

            _buyMeatButton.Disabled = Game.Save.Center.Bits < 20;
            _buyMedicineButton.Disabled = Game.Save.Center.Bits < 100;
            _buyEggButton.Disabled = Game.Save.Center.Bits < 25;
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
