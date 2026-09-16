using ProjetoDC.Enums;

namespace ProjetoDC.Scripts.Data
{
    /// <summary>Um oponente fixo de um campeonato (TournamentData.Opponents) - espécie e
    /// nível são sempre os mesmos, não dependem do time do jogador.</summary>
    public class TournamentOpponentData
    {
        public int DigimonId { get; set; }
        public int Level { get; set; }

        /// <summary>Passiva fixa desse oponente, definida por design (ver PASSIVAS_SPEC.md
        /// R5: "Torneios: as passivas ficam fixas no TournamentData") - diferente de Batalha
        /// Livre/Selvagem, NÃO é sorteada. Null = sem passiva.</summary>
        public PassiveType? PassiveId { get; set; }
    }
}
