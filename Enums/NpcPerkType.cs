namespace ProjetoDC.Enums
{
    /// <summary>Função que um NPC recrutado passa a ter no Center (NpcData.RecruitmentPerk). É dado no
    /// JSON do NPC, então trocar quem faz o quê (ou mais de um NPC com a mesma função) não pede mudar
    /// código. Os perks de treino específico são outro campo (RecruitmentTrainingAreaType).</summary>
    public enum NpcPerkType
    {
        None,

        /// <summary>Todo dia deixa 1 ou 2 Carnes de graça no Center.</summary>
        DailyMeat,

        /// <summary>Permite bloquear evoluções indesejadas por Digimon (Guia de Evolução).</summary>
        EvolutionAdvisor,

        /// <summary>Trabalha na cozinha: assa a Carne do estoque (Carne Assada).</summary>
        KitchenCook,
    }
}
