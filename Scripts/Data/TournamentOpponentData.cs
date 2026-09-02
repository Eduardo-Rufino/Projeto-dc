namespace ProjetoDC.Scripts.Data
{
    /// <summary>Um oponente fixo de um campeonato (TournamentData.Opponents) - espécie e
    /// nível são sempre os mesmos, não dependem do time do jogador.</summary>
    public class TournamentOpponentData
    {
        public int DigimonId { get; set; }
        public int Level { get; set; }
    }
}
