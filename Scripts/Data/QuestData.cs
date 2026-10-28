using ProjetoDC.Enums;
using System.Collections.Generic;

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

        /// <summary>SwitchOn: ids (do JSON do mapa isométrico ObjectiveMapId) dos objetos a
        /// acender/ligar - as lanternas apagadas, as caixas de junção. ObjectiveCount = quantos.</summary>
        public List<string> ObjectiveSwitchIds { get; set; } = new();

        /// <summary>SwitchOn: área (Data/Maps) onde ficam os objetos de ObjectiveSwitchIds.</summary>
        public int ObjectiveMapId { get; set; }

        /// <summary>Entregar dá a lanterna: nos mapas escuros o raio de visão do Digimon
        /// aumenta pra sempre (ver GameManager.HasLantern). Vale pela quest concluída, sem item.</summary>
        public bool GrantsLantern { get; set; }

        public int RewardBits { get; set; }
        public int RewardCapacity { get; set; }
        public int RewardItemId { get; set; }
        public int RewardItemQuantity { get; set; }

        /// <summary>Fala opcional do NPC depois da entrega (ex.: o lore do Tortamon) - aparece
        /// no diálogo quando a quest já está completa. Vazio = só o "já completa".</summary>
        public string CompletionText { get; set; } = "";
    }
}
