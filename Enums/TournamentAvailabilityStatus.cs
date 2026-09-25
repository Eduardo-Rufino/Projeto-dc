namespace ProjetoDC.Enums
{
    /// <summary>Se um campeonato pode ser iniciado agora (ver CalendarSystem.GetAvailability). Quando
    /// mais de um motivo vale, a ordem de prioridade é Locked, Closed, WonThisEdition.</summary>
    public enum TournamentAvailabilityStatus
    {
        Available,

        /// <summary>Precisa vencer antes o campeonato UnlockAfterTournamentId.</summary>
        Locked,

        /// <summary>Fora da data (IsOpen falso).</summary>
        Closed,

        /// <summary>Já vencido na edição que está aberta agora - reabre na próxima edição.</summary>
        WonThisEdition,
    }
}
