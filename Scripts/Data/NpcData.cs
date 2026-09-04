using ProjetoDC.Enums;
using System.Collections.Generic;

namespace ProjetoDC.Scripts.Data
{
    /// <summary>
    /// Dado estático de um NPC (carregado de Data/NPCs/*.json pelo DatabaseManager) -
    /// aparece em áreas de exploração (ver ExplorationMapData.Npcs).
    /// </summary>
    public class NpcData
    {
        public int Id { get; set; }
        public string Name { get; set; }

        /// <summary>Código usado pro retrato do NPC (res://Assets/Sprites/Digimon/{Code}.png) -
        /// reaproveita o mesmo asset de espécie quando o NPC "é" um Digimon parceiro.</summary>
        public string Code { get; set; } = "";

        public List<string> DialogueLines { get; set; } = new();

        /// <summary>Quest oferecida por esse NPC, ou null se ele só tem diálogo de flavor sem
        /// nenhuma quest associada.</summary>
        public int? QuestId { get; set; }

        /// <summary>Se true, entregar QuestId recruta esse NPC pro Center (ver
        /// GameManager.TryTurnInQuest, que adiciona o Id em CenterState.RecruitedNpcIds).
        /// Só marca a intenção - a lógica de efeito do NPC recrutado dentro do Center ainda
        /// não existe, fica pra uma versão futura.</summary>
        public bool IsRecruitable { get; set; } = false;

        /// <summary>Texto de flavor descrevendo o que esse NPC faria uma vez recrutado (ex.:
        /// "Passa a treinar Ataque nos Digimon sozinho"). Só exibido em diálogo - não aciona
        /// nenhum efeito real ainda.</summary>
        public string RecruitmentPerkDescription { get; set; } = "";

        /// <summary>Quando o perk desse NPC recrutado é "assistir" uma área de treino
        /// específica (ver CenterAreaType.TrainingAttack/TrainingDefense/etc.), qual área é
        /// essa - null se o perk não é desse tipo. Só metadado por enquanto.</summary>
        public CenterAreaType? RecruitmentTrainingAreaType { get; set; }
    }
}
