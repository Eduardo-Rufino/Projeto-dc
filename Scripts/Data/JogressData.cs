namespace ProjetoDC.Scripts.Data
{
    /// <summary>Receita de Jogress: fusão de dois Digimons existentes (DigimonAId/
    /// DigimonBId, em qualquer ordem) num Digimon novo (ToDigimonId) - diferente de
    /// EvolutionData, não tem nenhum requisito de nível/stats/batalhas/disciplina/
    /// felicidade, de propósito (ver JogressSystem.TryToFuse): só precisa ter os dois
    /// Digimons certos no roster.</summary>
    public class JogressData
    {
        public int DigimonAId { get; set; }

        public int DigimonBId { get; set; }

        public int ToDigimonId { get; set; }

        /// <summary>Multiplicador aplicado à MÉDIA dos CurrentStats dos dois Digimons de
        /// origem antes de puxar em direção à ficha da espécie resultante (ver
        /// JogressSystem.TryToFuse/DigimonInstance.BlendTowardSpecies) - mesmo espírito do
        /// StatMultiplier de EvolutionData, recompensando o treino acumulado dos dois pais
        /// em vez de descartá-lo.</summary>
        public float StatMultiplier { get; set; } = 1.3f;

        /// <summary>Par de origem é desordenado - A+B é a mesma receita que B+A.</summary>
        public bool MatchesPair(int idA, int idB) =>
            (DigimonAId == idA && DigimonBId == idB) ||
            (DigimonAId == idB && DigimonBId == idA);
    }
}
