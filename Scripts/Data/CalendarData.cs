using System.Collections.Generic;

namespace ProjetoDC.Scripts.Data
{
    /// <summary>
    /// Dado estático do calendário (carregado de Data/Calendar/calendar.json por CalendarSystem).
    /// A quantidade de estações é a quantidade de nomes em Seasons; um ano tem
    /// DaysPerSeason * Seasons.Count dias.
    /// </summary>
    public class CalendarData
    {
        public int DaysPerSeason { get; set; }
        public List<string> Seasons { get; set; } = new();

        public int SeasonCount => Seasons.Count;
        public int DaysPerYear => DaysPerSeason * Seasons.Count;
    }
}
