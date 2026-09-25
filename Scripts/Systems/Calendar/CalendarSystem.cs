using Godot;
using ProjetoDC.Enums;
using ProjetoDC.Scripts.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace ProjetoDC.Scripts.Systems.Calendar
{
    /// <summary>Uma data do calendário. Season, DayOfSeason e DayOfYear começam em 1.</summary>
    public readonly record struct CalendarDate(int Year, int Season, string SeasonName, int DayOfSeason, int DayOfYear);

    /// <summary>
    /// Calendário de estações derivado de WorldState.CurrentDay - só leitura do tempo, não guarda
    /// nada no save. As constantes (dias por estação, nomes das estações) vêm de
    /// Data/Calendar/calendar.json.
    ///
    /// As funções puras recebem um ÍNDICE de dia que começa em 0 (0 = primeiro dia do Ano 1).
    /// WorldState.CurrentDay começa em 1, então o jogo converte com DayIndexFromCurrentDay
    /// (CurrentDay - 1) - ou usa direto as sobrecargas *FromCurrentDay.
    /// </summary>
    public static class CalendarSystem
    {
        private const string CalendarPath = "res://Data/Calendar/calendar.json";

        private static CalendarData _data;
        private static bool _loadAttempted;

        /// <summary>Config carregada do JSON, ou null se o arquivo faltar/for inválido (erro no log).</summary>
        public static CalendarData Data
        {
            get
            {
                if (!_loadAttempted)
                {
                    _loadAttempted = true;
                    _data = Load();
                }

                return _data;
            }
        }

        public static bool IsLoaded => Data != null;

        private static CalendarData Load()
        {
            using var file = FileAccess.Open(CalendarPath, FileAccess.ModeFlags.Read);

            if (file == null)
            {
                GD.PrintErr($"Calendário: não foi possível abrir {CalendarPath}");
                return null;
            }

            try
            {
                var data = JsonSerializer.Deserialize<CalendarData>(
                    file.GetAsText(),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (data == null || data.DaysPerSeason <= 0 || data.SeasonCount == 0)
                {
                    GD.PrintErr($"Calendário: {CalendarPath} inválido (DaysPerSeason > 0 e ao menos uma estação)");
                    return null;
                }

                return data;
            }
            catch (JsonException e)
            {
                GD.PrintErr($"Calendário: erro ao ler {CalendarPath}: {e.Message}");
                return null;
            }
        }

        public static int DayIndexFromCurrentDay(int currentDay) => Math.Max(0, currentDay - 1);

        // ------------------------------------------------------------------ funções puras

        public static int Year(CalendarData cal, int dayIndex) => ClampIndex(dayIndex) / cal.DaysPerYear + 1;

        /// <summary>Dia do ano, 1..DaysPerYear.</summary>
        public static int DayOfYear(CalendarData cal, int dayIndex) => ClampIndex(dayIndex) % cal.DaysPerYear + 1;

        /// <summary>Estação, 1..SeasonCount.</summary>
        public static int Season(CalendarData cal, int dayIndex) => (DayOfYear(cal, dayIndex) - 1) / cal.DaysPerSeason + 1;

        public static string SeasonName(CalendarData cal, int season) => cal.Seasons[season - 1];

        /// <summary>Dia da estação, 1..DaysPerSeason.</summary>
        public static int DayOfSeason(CalendarData cal, int dayIndex) => (DayOfYear(cal, dayIndex) - 1) % cal.DaysPerSeason + 1;

        /// <summary>
        /// Quantos dias faltam, a partir do dia <paramref name="dayIndex"/>, até o próximo
        /// <paramref name="targetDayOfYear"/> (1..DaysPerYear), atravessando a virada de ano se
        /// preciso. 0 = é hoje; o resultado fica em 0..DaysPerYear-1.
        /// </summary>
        public static int DaysUntil(CalendarData cal, int dayIndex, int targetDayOfYear)
        {
            if (targetDayOfYear < 1 || targetDayOfYear > cal.DaysPerYear)
                throw new ArgumentOutOfRangeException(nameof(targetDayOfYear), targetDayOfYear, $"precisa estar em 1..{cal.DaysPerYear}");

            int diff = targetDayOfYear - DayOfYear(cal, dayIndex);
            return diff >= 0 ? diff : diff + cal.DaysPerYear;
        }

        public static CalendarDate GetDate(CalendarData cal, int dayIndex)
        {
            int season = Season(cal, dayIndex);
            return new CalendarDate(Year(cal, dayIndex), season, SeasonName(cal, season), DayOfSeason(cal, dayIndex), DayOfYear(cal, dayIndex));
        }

        /// <summary>Texto do calendário na HUD: "Ano X • Estação • Dia N".</summary>
        public static string Format(CalendarDate date) => $"Ano {date.Year} • {date.SeasonName} • Dia {date.DayOfSeason}";

        private static int ClampIndex(int dayIndex) => Math.Max(0, dayIndex);

        // ------------------------------------------------------------------ agenda dos campeonatos (funções puras)
        // Só o calendário: a trava por outro campeonato (UnlockAfterTournamentId) é IsUnlocked, à parte.
        // Nenhum fluxo do jogo chama isso ainda.

        /// <summary>Se o campeonato está aberto no dia <paramref name="dayIndex"/> (índice 0-based).</summary>
        public static bool IsOpen(CalendarData cal, TournamentData tournament, int dayIndex)
        {
            int index = ClampIndex(dayIndex);
            int dayOfYear = DayOfYear(cal, index);
            int duration = Math.Max(1, tournament.DurationDays);

            switch (tournament.ScheduleType)
            {
                case TournamentScheduleType.Recurring:
                    if (tournament.IntervalDays <= 0)
                        return false;
                    int sinceOffset = index - tournament.OffsetDays;
                    if (sinceOffset < 0)
                        return false;
                    return sinceOffset % tournament.IntervalDays < duration;

                case TournamentScheduleType.Window:
                    return InCircularRange(cal, dayOfYear, tournament.StartDayOfYear, tournament.EndDayOfYear);

                case TournamentScheduleType.Fixed:
                    int end = (tournament.StartDayOfYear - 1 + duration - 1) % cal.DaysPerYear + 1;
                    return duration >= cal.DaysPerYear || InCircularRange(cal, dayOfYear, tournament.StartDayOfYear, end);

                default: // Always, Unlockable
                    return true;
            }
        }

        /// <summary>
        /// Quantos dias faltam pro campeonato abrir a partir de <paramref name="dayIndex"/>: 0 se já está
        /// aberto, -1 se a agenda nunca abre (dado inválido).
        /// </summary>
        public static int DaysUntilOpen(CalendarData cal, TournamentData tournament, int dayIndex)
        {
            int index = ClampIndex(dayIndex);

            // Recurring conta dias corridos: o ciclo inteiro cabe em Offset + Interval; as outras agendas se
            // repetem a cada ano.
            int horizon = tournament.ScheduleType == TournamentScheduleType.Recurring
                ? Math.Max(0, tournament.OffsetDays - index) + Math.Max(1, tournament.IntervalDays)
                : cal.DaysPerYear;

            for (int i = 0; i < horizon; i++)
            {
                if (IsOpen(cal, tournament, index + i))
                    return i;
            }

            return -1;
        }

        /// <summary>Se a trava do campeonato (UnlockAfterTournamentId) já foi cumprida - sem trava é sempre true.</summary>
        public static bool IsUnlocked(TournamentData tournament, IEnumerable<int> clearedTournamentIds) =>
            tournament.UnlockAfterTournamentId <= 0 || clearedTournamentIds.Contains(tournament.UnlockAfterTournamentId);

        public static bool IsOpen(TournamentData tournament, int dayIndex) => IsOpen(Data, tournament, dayIndex);

        public static int DaysUntilOpen(TournamentData tournament, int dayIndex) => DaysUntilOpen(Data, tournament, dayIndex);

        // start..end (dias do ano, inclusive), atravessando a virada do ano se start > end.
        private static bool InCircularRange(CalendarData cal, int dayOfYear, int start, int end)
        {
            if (start < 1 || start > cal.DaysPerYear || end < 1 || end > cal.DaysPerYear)
                return false;

            return start <= end
                ? dayOfYear >= start && dayOfYear <= end
                : dayOfYear >= start || dayOfYear <= end;
        }

        // ------------------------------------------------------------------ atalhos pro jogo (config carregada + CurrentDay)

        public static CalendarDate GetDateFromCurrentDay(int currentDay) => GetDate(Data, DayIndexFromCurrentDay(currentDay));

        public static int DaysUntilFromCurrentDay(int currentDay, int targetDayOfYear) =>
            DaysUntil(Data, DayIndexFromCurrentDay(currentDay), targetDayOfYear);
    }
}
