using Godot;
using ProjetoDC.Scripts.Data;
using ProjetoDC.Scripts.Managers;
using ProjetoDC.Scripts.Systems.Calendar;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjetoDC.Scripts.UI
{
    /// <summary>Medalhas da temporada (o ano do calendário): cada campeonato que dá medalha, marcado como
    /// conquistado nesta temporada, pendente (ainda dá pra conquistar), encerrado (não abre mais neste ano)
    /// ou bloqueado (falta vencer outro campeonato), e o total "X de 20". Só leitura - a medalha sai de
    /// CenterState.LastWonEdition (ver CalendarSystem.HasSeasonMedal).</summary>
    public partial class MedalsScreen : Control
    {
        private VBoxContainer _itemsContainer;
        private Label _emptyLabel;
        private Label _totalLabel;
        private Button _backButton;

        private readonly List<Control> _rows = new();

        public event Action BackPressed;

        private GameManager Game => GameManager.Instance;

        public override void _Ready()
        {
            // O Center pausa a árvore inteira enquanto essa tela está aberta (ver
            // HUD.SyncPauseWithBlockingScreens) - sem isso o botão pararia de responder.
            ProcessMode = ProcessModeEnum.Always;

            _itemsContainer = GetNode<VBoxContainer>("Window/MarginContainer/VBoxContainer/ItemsScroll/ItemsContainer");
            _emptyLabel = GetNode<Label>("Window/MarginContainer/VBoxContainer/EmptyLabel");
            _totalLabel = GetNode<Label>("Window/MarginContainer/VBoxContainer/HeaderRow/TotalLabel");
            _backButton = GetNode<Button>("Window/MarginContainer/VBoxContainer/BackButton");

            _backButton.Pressed += () => BackPressed?.Invoke();
        }

        public void RefreshUI()
        {
            foreach (var row in _rows)
                row.QueueFree();

            _rows.Clear();

            var medals = Game.GetSeasonMedals();
            _totalLabel.Text = $"{medals.Earned} de {medals.Total}";

            var tournaments = DatabaseManager.Instance.GetAllTournaments().Where(t => t.MedalValue > 0).ToList();
            _emptyLabel.Visible = tournaments.Count == 0;

            var season = CalendarSystem.IsLoaded
                ? CalendarSystem.GetDateFromCurrentDay(Game.Save.World.CurrentDay).Year
                : 1;

            var header = MakeLabel($"Temporada: Ano {season} — ainda dá pra conquistar {medals.Available}", 12, new Color(0.7f, 0.72f, 0.75f));
            _itemsContainer.AddChild(header);
            _rows.Add(header);

            foreach (var t in tournaments)
            {
                var (status, color) = Describe(t);

                var row = new HBoxContainer();
                row.AddChild(MakeLabel($"{t.Name} (+{t.MedalValue})", 14, Colors.White, expand: true));
                row.AddChild(MakeLabel(status, 13, color));

                _itemsContainer.AddChild(row);
                _rows.Add(row);
            }
        }

        private (string Text, Color Color) Describe(TournamentData t)
        {
            var green = new Color(0.55f, 0.9f, 0.6f);
            var yellow = new Color(1f, 0.86f, 0.31f);
            var gray = new Color(0.6f, 0.62f, 0.65f);
            var orange = new Color(1f, 0.6f, 0.4f);

            if (!CalendarSystem.IsLoaded)
                return ("-", gray);

            var cal = CalendarSystem.Data;
            int dayIndex = CalendarSystem.DayIndexFromCurrentDay(Game.Save.World.CurrentDay);
            var center = Game.Save.Center;

            if (CalendarSystem.HasSeasonMedal(cal, t, dayIndex, center.LastWonEdition))
                return ("🏅 Conquistada", green);

            if (!CalendarSystem.IsUnlocked(t, center.ClearedTournamentIds))
            {
                var blocker = DatabaseManager.Instance.GetTournament(t.UnlockAfterTournamentId);
                return ($"🔒 Bloqueada (vença {blocker?.Name ?? "?"})", orange);
            }

            if (!CalendarSystem.IsMedalReachable(cal, t, dayIndex, center.ClearedTournamentIds, center.LastWonEdition))
                return ("Encerrada nesta temporada", gray);

            int until = CalendarSystem.DaysUntilOpen(cal, t, dayIndex);
            return (until == 0 ? "Pendente (aberto agora)" : $"Pendente (abre em {until} {(until == 1 ? "dia" : "dias")})", yellow);
        }

        private static Label MakeLabel(string text, int size, Color color, bool expand = false)
        {
            var label = new Label { Text = text };
            label.AddThemeFontSizeOverride("font_size", size);
            label.AddThemeColorOverride("font_color", color);

            if (expand)
                label.SizeFlagsHorizontal = SizeFlags.ExpandFill;

            return label;
        }
    }
}
