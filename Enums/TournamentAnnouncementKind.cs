namespace ProjetoDC.Enums
{
    /// <summary>Tipo de aviso antecipado de campeonato (ver CalendarSystem.GetAnnouncements).</summary>
    public enum TournamentAnnouncementKind
    {
        /// <summary>Faltam exatamente AnnounceDaysBefore dias pra abrir: "abre em N dias".</summary>
        OpensSoon,

        /// <summary>Primeiro dia aberto: "está aberto por N dias".</summary>
        OpenedToday,

        /// <summary>Último dia aberto: "Último dia: X fecha à meia-noite".</summary>
        ClosesTomorrow,
    }
}
