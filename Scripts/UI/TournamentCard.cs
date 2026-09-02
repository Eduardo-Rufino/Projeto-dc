using Godot;
using ProjetoDC.Scripts.Data;
using ProjetoDC.Scripts.Managers;
using System;
using System.Linq;

namespace ProjetoDC.Scripts.UI
{
    /// <summary>Uma linha da lista de campeonatos: nome, formato, nível recomendado,
    /// oponentes fixos e recompensa - usado pela TournamentListScreen.</summary>
    public partial class TournamentCard : PanelContainer
    {
        private Label _nameLabel;
        private Label _formatLabel;
        private Label _levelLabel;
        private Label _opponentsLabel;
        private Label _rewardLabel;
        private Button _challengeButton;

        private TournamentData _tournament;

        public event Action<TournamentData> Challenged;

        public override void _Ready()
        {
            _nameLabel = GetNode<Label>("Pad/VBoxContainer/HeaderRow/NameLabel");
            _formatLabel = GetNode<Label>("Pad/VBoxContainer/HeaderRow/FormatLabel");
            _levelLabel = GetNode<Label>("Pad/VBoxContainer/LevelLabel");
            _opponentsLabel = GetNode<Label>("Pad/VBoxContainer/OpponentsLabel");
            _rewardLabel = GetNode<Label>("Pad/VBoxContainer/FooterRow/RewardLabel");
            _challengeButton = GetNode<Button>("Pad/VBoxContainer/FooterRow/ChallengeButton");

            _challengeButton.Pressed += () => Challenged?.Invoke(_tournament);
        }

        public void SetTournament(TournamentData tournament, bool cleared)
        {
            _tournament = tournament;

            _nameLabel.Text = cleared ? $"{tournament.Name} ✓" : tournament.Name;
            _formatLabel.Text = tournament.Opponents.Count == 1 ? "1x1" : "3x3";
            _levelLabel.Text = $"Nível recomendado: {tournament.RecommendedLevel}";

            var opponentNames = tournament.Opponents.Select(o =>
            {
                var data = DatabaseManager.Instance.GetDigimon(o.DigimonId);

                return $"{data?.Name ?? "?"} Lv{o.Level}";
            });

            _opponentsLabel.Text = "Oponentes: " + string.Join(", ", opponentNames);

            _rewardLabel.Text = cleared
                ? "Recompensa já resgatada"
                : $"Recompensa: {DescribeReward(tournament)}";
        }

        private static string DescribeReward(TournamentData tournament)
        {
            var parts = new System.Collections.Generic.List<string>();

            if (tournament.CapacityReward > 0)
                parts.Add($"+{tournament.CapacityReward} Capacidade");

            if (tournament.BitsReward > 0)
                parts.Add($"+{tournament.BitsReward:N0} Bits");

            return parts.Count > 0 ? string.Join(" | ", parts) : "-";
        }
    }
}
