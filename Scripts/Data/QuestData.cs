using ProjetoDC.Enums;

namespace ProjetoDC.Scripts.Data
{
    /// <summary>
    /// Dado estático de uma quest (carregado de Data/Quests/*.json pelo DatabaseManager) -
    /// sempre single-step em v1 (sem cadeia/ramificação): um objetivo, uma recompensa.
    /// Oferecida por um NpcData (ver NpcData.QuestId), entregue de volta pro mesmo NPC.
    /// </summary>
    public class QuestData
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public int GiverNpcId { get; set; }

        public QuestObjectiveType ObjectiveType { get; set; }

        /// <summary>Alvo do objetivo: Id de Digimon em DefeatWild (0 = qualquer selvagem conta)
        /// ou Id de item em CollectItem.</summary>
        public int ObjectiveTargetId { get; set; }

        public int ObjectiveCount { get; set; } = 1;

        public int RewardBits { get; set; }
        public int RewardCapacity { get; set; }
        public int RewardItemId { get; set; }
        public int RewardItemQuantity { get; set; }
    }
}
