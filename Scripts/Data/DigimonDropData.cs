namespace ProjetoDC.Scripts.Data
{
    /// <summary>
    /// Um item que uma espécie de Digimon pode soltar ao ser derrotada num encontro selvagem
    /// de exploração (ver DigimonData.Drops / GameManager.ApplyWildEncounterDrops) - não tem
    /// nada a ver com a recompensa normal de batalha (Bits/XP), é só pra alimentar quests de
    /// coleta tipo "colete 3 chifres de Gabumon". Cada espécie pode ter vários drops
    /// independentes, cada um com sua própria chance.
    /// </summary>
    public class DigimonDropData
    {
        public int ItemId { get; set; }
        public int Quantity { get; set; } = 1;

        /// <summary>Chance de soltar esse item, em porcentagem (0-100) - avaliada
        /// independentemente pra cada drop da lista, então uma espécie pode ter mais de um
        /// item caindo na mesma vitória.</summary>
        public float ChancePercent { get; set; } = 100f;
    }
}
