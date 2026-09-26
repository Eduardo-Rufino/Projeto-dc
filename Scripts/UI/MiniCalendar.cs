using Godot;
using ProjetoDC.Scripts.Systems.Calendar;
using System.Collections.Generic;

namespace ProjetoDC.Scripts.UI
{
    /// <summary>
    /// Minicalendário do HUD: título "Estação • Ano N", grade com os dias da estação atual (o dia de hoje em
    /// destaque, os que já passaram esmaecidos) e uma linha de informação embaixo (medalhas e o próximo
    /// campeonato, montada pelo HUD). Só apresentação - a data vem de CalendarSystem, sem regra própria.
    /// </summary>
    public partial class MiniCalendar : VBoxContainer
    {
        private static readonly Color TodayColor = new(1f, 0.86f, 0.31f);
        private static readonly Color CellColor = new(0.19f, 0.21f, 0.24f);
        private static readonly Color CellBorderColor = new(0.33f, 0.36f, 0.39f);

        private Label _titleLabel;
        private GridContainer _dayGrid;
        private Label _infoLabel;

        private readonly List<(PanelContainer Cell, Label Label)> _cells = new();
        private int _shownDay = -1;

        public override void _Ready()
        {
            _titleLabel = GetNode<Label>("TitleLabel");
            _dayGrid = GetNode<GridContainer>("DayGrid");
            _infoLabel = GetNode<Label>("InfoLabel");
        }

        /// <summary>Atualiza pro dia <paramref name="currentDay"/> (WorldState.CurrentDay) - a grade só é
        /// refeita quando o dia muda. <paramref name="info"/> vai na linha de baixo.</summary>
        public void Refresh(int currentDay, string info)
        {
            _infoLabel.Text = info;

            if (currentDay == _shownDay)
                return;

            _shownDay = currentDay;

            if (!CalendarSystem.IsLoaded)
            {
                _titleLabel.Text = "Calendário";
                return;
            }

            var date = CalendarSystem.GetDateFromCurrentDay(currentDay);
            _titleLabel.Text = $"{date.SeasonName} • Ano {date.Year}";

            EnsureCells(CalendarSystem.Data.DaysPerSeason);

            for (int i = 0; i < _cells.Count; i++)
            {
                int day = i + 1;
                var (cell, label) = _cells[i];
                bool today = day == date.DayOfSeason;
                bool past = day < date.DayOfSeason;

                var style = new StyleBoxFlat
                {
                    BgColor = today ? TodayColor : CellColor,
                    BorderColor = today ? TodayColor : CellBorderColor,
                };
                style.SetBorderWidthAll(1);
                style.SetCornerRadiusAll(3);
                cell.AddThemeStyleboxOverride("panel", style);

                label.AddThemeColorOverride("font_color", today ? new Color(0.1f, 0.1f, 0.12f) : Colors.White);
                cell.Modulate = new Color(1f, 1f, 1f, past ? 0.35f : 1f);
                cell.TooltipText = today ? $"Hoje: dia {day} de {date.SeasonName}" : $"Dia {day} de {date.SeasonName}";
            }
        }

        private void EnsureCells(int count)
        {
            if (_cells.Count == count)
                return;

            foreach (var (cell, _) in _cells)
                cell.QueueFree();
            _cells.Clear();

            for (int day = 1; day <= count; day++)
            {
                var cell = new PanelContainer
                {
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                    CustomMinimumSize = new Vector2(0, 12),
                    MouseFilter = MouseFilterEnum.Pass,
                };
                var label = new Label
                {
                    Text = day.ToString(),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    MouseFilter = MouseFilterEnum.Ignore,
                };
                label.AddThemeFontSizeOverride("font_size", 9);
                label.AddThemeConstantOverride("line_spacing", 0);
                cell.AddChild(label);
                _dayGrid.AddChild(cell);
                _cells.Add((cell, label));
            }
        }
    }
}
