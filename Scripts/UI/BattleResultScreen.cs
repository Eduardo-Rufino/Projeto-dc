using Godot;
using ProjetoDC.Enums;
using System;

namespace ProjetoDC.Scripts.UI
{
    public partial class BattleResultScreen : Control
    {
        private Label _resultLabel;
        private Label _rewardLabel;
        private Button _okButton;

        public event Action OkPressed;

        public override void _Ready()
        {
            _resultLabel = GetNode<Label>(
                "CenterContainer/Panel/MarginContainer/VBoxContainer/ResultLabel");

            _rewardLabel = GetNode<Label>(
                "CenterContainer/Panel/MarginContainer/VBoxContainer/RewardLabel");

            _okButton = GetNode<Button>(
                "CenterContainer/Panel/MarginContainer/VBoxContainer/Button");

            SetAnchorsPreset(LayoutPreset.FullRect);
            SetOffsetsPreset(LayoutPreset.FullRect);

            GD.Print(_resultLabel);
            GD.Print(_rewardLabel);
            GD.Print(_okButton);

            GD.Print($"Size: {Size}");
            GD.Print($"Position: {Position}");
            GD.Print($"Visible: {Visible}");

            ZIndex = 100;

            _okButton.Pressed += OnOkPressed;
        }

        public void ShowResult(BattleResult result)
        {
            Size = GetViewportRect().Size;
            CustomMinimumSize = Size;

            Visible = true;
            MoveToFront();

            switch (result)
            {
                case BattleResult.PlayerWon:
                    _resultLabel.Text = "VITÓRIA!";
                    _rewardLabel.Text = "Parabéns!";
                    break;

                case BattleResult.EnemyWon:
                    _resultLabel.Text = "DERROTA";
                    _rewardLabel.Text = "Melhor sorte da próxima vez.";
                    break;
            }
        }

        private void OnOkPressed()
        {
            Visible = false;

            OkPressed?.Invoke();
        }
    }
}
