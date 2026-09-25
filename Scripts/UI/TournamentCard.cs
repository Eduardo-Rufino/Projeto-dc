using Godot;
using ProjetoDC.Enums;
using ProjetoDC.Scripts.Data;
using ProjetoDC.Scripts.Managers;
using ProjetoDC.Scripts.Systems.Calendar;
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
        private Label _availabilityLabel;

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

            // Situação no calendário ("Abre em N dias" etc.), logo abaixo do nível recomendado.
            _availabilityLabel = new Label();
            _availabilityLabel.AddThemeFontSizeOverride("font_size", 11);
            var levelLabel = GetNode<Label>("Pad/VBoxContainer/LevelLabel");
            levelLabel.AddSibling(_availabilityLabel);
        }

        public void SetTournament(TournamentData tournament, bool cleared, TournamentAvailability availability)
        {
            _tournament = tournament;

            _challengeButton.Disabled = !availability.CanStart;
            _availabilityLabel.Text = DescribeAvailability(tournament, availability);
            _availabilityLabel.Visible = _availabilityLabel.Text != "";
            _availabilityLabel.AddThemeColorOverride("font_color", availability.CanStart
                ? new Color(0.55f, 0.9f, 0.6f)
                : new Color(1f, 0.6f, 0.4f));

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

        private static string DescribeAvailability(TournamentData tournament, TournamentAvailability availability)
        {
            switch (availability.Status)
            {
                case TournamentAvailabilityStatus.Locked:
                    var blocker = DatabaseManager.Instance.GetTournament(tournament.UnlockAfterTournamentId);
                    return $"Bloqueado: vença {blocker?.Name ?? "?"}";

                case TournamentAvailabilityStatus.Closed:
                    return availability.Days < 0 ? "Fechado" : $"Abre em {Days(availability.Days)}";

                case TournamentAvailabilityStatus.WonThisEdition:
                    return "Concluído nesta edição";

                default:
                    return availability.Days > 0 ? $"Aberto por mais {Days(availability.Days)}" : "";
            }
        }

        private static string Days(int n) => n == 1 ? "1 dia" : $"{n} dias";

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
