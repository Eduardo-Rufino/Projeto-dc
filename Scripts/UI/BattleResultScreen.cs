using Godot;
using ProjetoDC.Enums;
using ProjetoDC.Scripts.Systems.Battle;
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

        public void ShowResult(BattleResult result, BattleReward reward = null)
        {
            Size = GetViewportRect().Size;
            CustomMinimumSize = Size;

            Visible = true;
            MoveToFront();

            switch (result)
            {
                case BattleResult.PlayerWon:
                    _resultLabel.Text = "VITÓRIA!";
                    _rewardLabel.Text = reward != null
                        ? $"+{reward.Experience} XP por membro | +{reward.Bits} Bits" +
                          (reward.CapacityGained > 0 ? $" | +{reward.CapacityGained} Capacidade!" : "") +
                          (reward.BonusBitsGained > 0 ? $" | +{reward.BonusBitsGained} Bits de bônus!" : "")
                        : "Parabéns!";
                    break;

                case BattleResult.EnemyWon:
                    _resultLabel.Text = "DERROTA";
                    _rewardLabel.Text = reward != null && reward.Experience > 0
                        ? $"+{reward.Experience} XP de consolação por membro | Melhor sorte da próxima vez."
                        : "Melhor sorte da próxima vez.";
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
