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

    /// <summary>Resultado de CalendarSystem.GetAvailability. Days: Closed = dias até abrir (-1 = nunca);
    /// Available = dias que ainda fica aberto contando hoje (-1 = não fecha); nos outros, 0. Edition =
    /// edição do dia consultado (ver EditionId).</summary>
    public readonly record struct TournamentAvailability(TournamentAvailabilityStatus Status, int Days, int Edition)
    {
        public bool CanStart => Status == TournamentAvailabilityStatus.Available;
    }

    /// <summary>Um campeonato destravado e ainda não vencido na edição, com DaysUntilOpen (0 = aberto
    /// hoje) e DaysRemaining (dias abertos contando o dia da abertura/hoje; -1 = não fecha).</summary>
    public readonly record struct UpcomingTournament(TournamentData Tournament, int DaysUntilOpen, int DaysRemaining)
    {
        public bool IsOpen => DaysUntilOpen == 0;
    }

    /// <summary>Aviso antecipado de um campeonato no dia (ver CalendarSystem.GetAnnouncements). Days:
    /// OpensSoon = dias até abrir; OpenedToday = dias que fica aberto; ClosesTomorrow = 1.</summary>
    public readonly record struct TournamentAnnouncement(TournamentData Tournament, TournamentAnnouncementKind Kind, int Days);

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

        /// <summary>
        /// Identifica a edição do campeonato no dia <paramref name="dayIndex"/> - vencer bloqueia só a
        /// edição em que venceu (CenterState.LastWonEdition). Recurring: número do ciclo desde OffsetDays
        /// (-1 antes da primeira abertura). Window/Fixed: o ano em que a abertura mais recente COMEÇOU, pra
        /// uma agenda que atravessa a virada do ano (ex.: 58 a 2) continuar sendo uma edição só.
        /// Always/Unlockable: o ano (vencer bloqueia até o ano seguinte).
        /// </summary>
        public static int EditionId(CalendarData cal, TournamentData tournament, int dayIndex)
        {
            int index = ClampIndex(dayIndex);
            int year = Year(cal, index);

            switch (tournament.ScheduleType)
            {
                case TournamentScheduleType.Recurring:
                    int sinceOffset = index - tournament.OffsetDays;
                    return sinceOffset < 0 || tournament.IntervalDays <= 0 ? -1 : sinceOffset / tournament.IntervalDays;

                case TournamentScheduleType.Window:
                case TournamentScheduleType.Fixed:
                    return DayOfYear(cal, index) >= tournament.StartDayOfYear ? year : year - 1;

                default:
                    return year;
            }
        }

        /// <summary>Quantos dias (contando hoje) o campeonato ainda fica aberto: 0 se está fechado, -1 se
        /// não fecha (Always/Unlockable).</summary>
        public static int DaysOpenRemaining(CalendarData cal, TournamentData tournament, int dayIndex)
        {
            if (tournament.ScheduleType is TournamentScheduleType.Always or TournamentScheduleType.Unlockable)
                return -1;

            int index = ClampIndex(dayIndex);
            int days = 0;

            while (days < cal.DaysPerYear && IsOpen(cal, tournament, index + days))
                days++;

            return days;
        }

        /// <summary>
        /// Se o campeonato pode ser iniciado no dia <paramref name="dayIndex"/>: precisa estar aberto
        /// (IsOpen), destravado (IsUnlocked), ter as medalhas da temporada exigidas (MedalsRequired - só é
        /// checado quando <paramref name="allTournaments"/> é passado) e não ter sido vencido na edição atual
        /// (LastWonEdition). Perder não entra aqui - só vencer registra a edição.
        /// </summary>
        public static TournamentAvailability GetAvailability(
            CalendarData cal,
            TournamentData tournament,
            int dayIndex,
            IEnumerable<int> clearedTournamentIds,
            IReadOnlyDictionary<int, int> lastWonEdition,
            IReadOnlyCollection<TournamentData> allTournaments = null)
        {
            int edition = EditionId(cal, tournament, dayIndex);

            if (!IsUnlocked(tournament, clearedTournamentIds))
                return new TournamentAvailability(TournamentAvailabilityStatus.Locked, 0, edition);

            if (!IsOpen(cal, tournament, dayIndex))
                return new TournamentAvailability(TournamentAvailabilityStatus.Closed, DaysUntilOpen(cal, tournament, dayIndex), edition);

            if (tournament.MedalsRequired > 0 && allTournaments != null)
            {
                int earned = MedalsThisSeason(cal, allTournaments, dayIndex, lastWonEdition);

                if (earned < tournament.MedalsRequired)
                {
                    var status = MedalsPotential(cal, allTournaments, dayIndex, clearedTournamentIds, lastWonEdition) >= tournament.MedalsRequired
                        ? TournamentAvailabilityStatus.NeedsMedals
                        : TournamentAvailabilityStatus.MedalsUnreachable;

                    return new TournamentAvailability(status, earned, edition);
                }
            }

            if (lastWonEdition != null && lastWonEdition.TryGetValue(tournament.Id, out int wonEdition) && wonEdition == edition)
                return new TournamentAvailability(TournamentAvailabilityStatus.WonThisEdition, 0, edition);

            return new TournamentAvailability(TournamentAvailabilityStatus.Available, DaysOpenRemaining(cal, tournament, dayIndex), edition);
        }

        /// <summary>Recurring com intervalo menor que isso não gera aviso antecipado (abriria a cada poucos
        /// dias, virando spam) - a lista de campeonatos e a HUD continuam mostrando.</summary>
        public const int MinAnnouncedRecurringInterval = 15;

        /// <summary>
        /// Campeonatos destravados e não vencidos na edição (a atual, se aberto; a da próxima abertura, se
        /// fechado), ordenados por dias até abrir, depois dias restantes, depois Id. Os que nunca abrem
        /// ficam de fora.
        /// </summary>
        public static List<UpcomingTournament> GetUpcoming(
            CalendarData cal,
            IEnumerable<TournamentData> tournaments,
            int dayIndex,
            IEnumerable<int> clearedTournamentIds,
            IReadOnlyDictionary<int, int> lastWonEdition)
        {
            var cleared = clearedTournamentIds.ToList();
            var result = new List<UpcomingTournament>();

            foreach (var t in tournaments)
            {
                if (!IsUnlocked(t, cleared))
                    continue;

                int until = DaysUntilOpen(cal, t, dayIndex);

                if (until < 0)
                    continue;

                int openDay = ClampIndex(dayIndex) + until;

                if (lastWonEdition != null && lastWonEdition.TryGetValue(t.Id, out int won) && won == EditionId(cal, t, openDay))
                    continue;

                result.Add(new UpcomingTournament(t, until, DaysOpenRemaining(cal, t, openDay)));
            }

            return result
                .OrderBy(u => u.DaysUntilOpen)
                .ThenBy(u => u.DaysRemaining < 0 ? int.MaxValue : u.DaysRemaining)
                .ThenBy(u => u.Tournament.Id)
                .ToList();
        }

        /// <summary>
        /// Avisos do dia <paramref name="dayIndex"/> (a HUD chama na virada do dia): "abre em N dias" quando
        /// faltam exatamente AnnounceDaysBefore, "está aberto por N dias" no primeiro dia aberto e "Último
        /// dia: X fecha à meia-noite" no último. Só pra quem está em GetUpcoming (destravado e não vencido na edição); sem
        /// data (Always/Unlockable) e Recurring com intervalo &lt; MinAnnouncedRecurringInterval não avisam.
        /// </summary>
        public static List<TournamentAnnouncement> GetAnnouncements(
            CalendarData cal,
            IEnumerable<TournamentData> tournaments,
            int dayIndex,
            IEnumerable<int> clearedTournamentIds,
            IReadOnlyDictionary<int, int> lastWonEdition)
        {
            var result = new List<TournamentAnnouncement>();

            foreach (var u in GetUpcoming(cal, tournaments, dayIndex, clearedTournamentIds, lastWonEdition))
            {
                var t = u.Tournament;

                if (t.ScheduleType is TournamentScheduleType.Always or TournamentScheduleType.Unlockable)
                    continue;

                if (t.ScheduleType == TournamentScheduleType.Recurring && t.IntervalDays < MinAnnouncedRecurringInterval)
                    continue;

                if (!u.IsOpen)
                {
                    if (t.AnnounceDaysBefore > 0 && u.DaysUntilOpen == t.AnnounceDaysBefore)
                        result.Add(new TournamentAnnouncement(t, TournamentAnnouncementKind.OpensSoon, u.DaysUntilOpen));

                    continue;
                }

                if (dayIndex <= 0 || !IsOpen(cal, t, dayIndex - 1))
                    result.Add(new TournamentAnnouncement(t, TournamentAnnouncementKind.OpenedToday, u.DaysRemaining));

                if (u.DaysRemaining == 1)
                    result.Add(new TournamentAnnouncement(t, TournamentAnnouncementKind.ClosesTomorrow, 1));
            }

            return result;
        }

        /// <summary>Texto do aviso; <paramref name="missingEligible"/> &gt; 0 acrescenta quantos Digimon
        /// elegíveis faltam no Center pra montar o time.</summary>
        /// <summary>Texto do aviso; <paramref name="missingEligible"/> &gt; 0 acrescenta quantos Digimon
        /// elegíveis faltam; num campeonato que exige medalhas, <paramref name="medalsEarned"/> &gt;= 0
        /// acrescenta "Medalhas: X/N".</summary>
        public static string FormatAnnouncement(TournamentAnnouncement a, int missingEligible, int medalsEarned)
        {
            string text = FormatAnnouncement(a, missingEligible);

            return a.Tournament.MedalsRequired > 0 && medalsEarned >= 0
                ? $"{text} • Medalhas: {medalsEarned}/{a.Tournament.MedalsRequired}"
                : text;
        }

        public static string FormatAnnouncement(TournamentAnnouncement a, int missingEligible)
        {
            string text = a.Kind switch
            {
                TournamentAnnouncementKind.OpensSoon => $"{a.Tournament.Name} abre em {DaysText(a.Days)}",
                TournamentAnnouncementKind.OpenedToday => $"{a.Tournament.Name} está aberto por {DaysText(a.Days)}",
                _ => $"Último dia: {a.Tournament.Name} fecha à meia-noite",
            };

            return missingEligible switch
            {
                <= 0 => text,
                1 => $"{text} (falta 1 Digimon elegível)",
                _ => $"{text} (faltam {missingEligible} Digimon elegíveis)",
            };
        }

        /// <summary>Linha curta da HUD: o primeiro de GetUpcoming ("Aberto: X (3d)" ou "Próx.: X em 3d"),
        /// ou vazio se não há nenhum.</summary>
        public static string FormatNext(IReadOnlyList<UpcomingTournament> upcoming)
        {
            if (upcoming.Count == 0)
                return "";

            var u = upcoming[0];

            if (!u.IsOpen)
                return $"Próx.: {u.Tournament.Name} em {u.DaysUntilOpen}d";

            return u.DaysRemaining > 0
                ? $"Aberto: {u.Tournament.Name} ({u.DaysRemaining}d)"
                : $"Aberto: {u.Tournament.Name}";
        }

        private static string DaysText(int n) => n == 1 ? "1 dia" : $"{n} dias";

        // ------------------------------------------------------------------ medalhas da temporada (funções puras)
        // Temporada = ano do calendário. Não há estado novo: a medalha sai de LastWonEdition - vale se a
        // edição vencida COMEÇOU na temporada atual.

        /// <summary>Temporada (ano) em que a edição <paramref name="edition"/> do campeonato começa.
        /// Window/Fixed: a própria edição (já é o ano). Recurring: o ano da abertura daquele ciclo.</summary>
        public static int EditionSeason(CalendarData cal, TournamentData tournament, int edition)
        {
            if (tournament.ScheduleType != TournamentScheduleType.Recurring)
                return edition;

            if (edition < 0 || tournament.IntervalDays <= 0)
                return 0;

            return Year(cal, tournament.OffsetDays + edition * tournament.IntervalDays);
        }

        /// <summary>Se o campeonato dá medalha e foi vencido numa edição que começou na temporada do dia.</summary>
        public static bool HasSeasonMedal(CalendarData cal, TournamentData tournament, int dayIndex, IReadOnlyDictionary<int, int> lastWonEdition) =>
            tournament.MedalValue > 0 &&
            lastWonEdition != null &&
            lastWonEdition.TryGetValue(tournament.Id, out int won) &&
            EditionSeason(cal, tournament, won) == Year(cal, dayIndex);

        /// <summary>Soma de MedalValue dos campeonatos vencidos nesta temporada.</summary>
        public static int MedalsThisSeason(CalendarData cal, IEnumerable<TournamentData> tournaments, int dayIndex, IReadOnlyDictionary<int, int> lastWonEdition) =>
            tournaments.Where(t => HasSeasonMedal(cal, t, dayIndex, lastWonEdition)).Sum(t => t.MedalValue);

        /// <summary>Soma de MedalValue de todos os campeonatos (o máximo de uma temporada).</summary>
        public static int MedalsTotal(IEnumerable<TournamentData> tournaments) => tournaments.Sum(t => t.MedalValue);

        /// <summary>Se ainda dá pra conquistar a medalha nesta temporada: dá medalha, está destravado, não
        /// foi conquistada e o campeonato está aberto agora ou abre de novo antes da virada do ano.</summary>
        public static bool IsMedalReachable(
            CalendarData cal, TournamentData tournament, int dayIndex,
            IEnumerable<int> clearedTournamentIds, IReadOnlyDictionary<int, int> lastWonEdition)
        {
            if (tournament.MedalValue <= 0 || HasSeasonMedal(cal, tournament, dayIndex, lastWonEdition))
                return false;

            if (!IsUnlocked(tournament, clearedTournamentIds))
                return false;

            int until = DaysUntilOpen(cal, tournament, dayIndex);
            return until >= 0 && Year(cal, ClampIndex(dayIndex) + until) == Year(cal, dayIndex);
        }

        /// <summary>
        /// O máximo de medalhas que ainda dá pra ter nesta temporada: as já conquistadas + as que ainda abrem
        /// no ano, contando também as travadas cuja trava ainda pode cair a tempo (o campeonato que destrava
        /// já foi vencido algum dia, ou ainda abre neste ano antes da próxima abertura do travado). É o que
        /// decide "Inalcançável nesta temporada" - MedalsAvailable, por definição, só conta as destravadas.
        /// </summary>
        public static int MedalsPotential(
            CalendarData cal, IReadOnlyCollection<TournamentData> tournaments, int dayIndex,
            IEnumerable<int> clearedTournamentIds, IReadOnlyDictionary<int, int> lastWonEdition)
        {
            var cleared = clearedTournamentIds.ToList();
            int year = Year(cal, dayIndex);
            int index = ClampIndex(dayIndex);
            int potential = MedalsThisSeason(cal, tournaments, dayIndex, lastWonEdition);

            // próximo dia (índice) em que abre ainda nesta temporada, ou -1
            int NextOpenThisSeason(TournamentData t)
            {
                int until = DaysUntilOpen(cal, t, index);
                return until >= 0 && Year(cal, index + until) == year ? index + until : -1;
            }

            // dá pra estar destravado até o dia <by>? (trava em cadeia, sem ciclos no dado)
            bool CanUnlockBy(TournamentData t, int by, int depth)
            {
                if (IsUnlocked(t, cleared))
                    return true;

                var blocker = tournaments.FirstOrDefault(o => o.Id == t.UnlockAfterTournamentId);
                if (blocker == null || depth > tournaments.Count)
                    return false;

                int blockerOpens = NextOpenThisSeason(blocker);
                return blockerOpens >= 0 && blockerOpens < by && CanUnlockBy(blocker, blockerOpens, depth + 1);
            }

            foreach (var t in tournaments)
            {
                if (t.MedalValue <= 0 || HasSeasonMedal(cal, t, dayIndex, lastWonEdition))
                    continue;

                int opens = NextOpenThisSeason(t);
                if (opens >= 0 && CanUnlockBy(t, opens + Math.Max(0, DaysOpenRemaining(cal, t, opens) - 1) + 1, 0))
                    potential += t.MedalValue;
            }

            return potential;
        }

        /// <summary>Soma de MedalValue ainda alcançável nesta temporada (ver IsMedalReachable).</summary>
        public static int MedalsAvailable(
            CalendarData cal, IEnumerable<TournamentData> tournaments, int dayIndex,
            IEnumerable<int> clearedTournamentIds, IReadOnlyDictionary<int, int> lastWonEdition)
        {
            var cleared = clearedTournamentIds.ToList();
            return tournaments.Where(t => IsMedalReachable(cal, t, dayIndex, cleared, lastWonEdition)).Sum(t => t.MedalValue);
        }

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
