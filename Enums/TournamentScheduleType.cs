namespace ProjetoDC.Enums
{
    /// <summary>Quando um campeonato fica aberto no calendário (TournamentData.ScheduleType, ver
    /// CalendarSystem.IsOpen). A trava por outro campeonato (UnlockAfterTournamentId) é separada e
    /// vale com qualquer tipo.</summary>
    public enum TournamentScheduleType
    {
        /// <summary>Sempre aberto (comportamento de antes do calendário).</summary>
        Always,

        /// <summary>Abre por DurationDays a cada IntervalDays, deslocado por OffsetDays (conta dias
        /// corridos, não dias do ano - se o intervalo não divide o ano, as datas andam de um ano pro outro).</summary>
        Recurring,

        /// <summary>Aberto de StartDayOfYear a EndDayOfYear (inclusive), todo ano; se Start &gt; End a
        /// janela atravessa a virada do ano.</summary>
        Window,

        /// <summary>Abre no StartDayOfYear e fica DurationDays aberto, todo ano (pode atravessar a
        /// virada do ano).</summary>
        Fixed,

        /// <summary>Sem data: depois de destravado (UnlockAfterTournamentId), fica sempre aberto.</summary>
        Unlockable,
    }
}
