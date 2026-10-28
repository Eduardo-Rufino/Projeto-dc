namespace ProjetoDC.Enums
{
    public enum QuestObjectiveType
    {
        DefeatWild,
        CollectItem,

        /// <summary>Só conversar: pronta pra entregar assim que aceita (ObjectiveCount 0).</summary>
        Talk,

        /// <summary>Entregar ObjectiveCount do item ObjectiveTargetId que já estiver no
        /// inventário (não importa quando foi obtido) - a entrega consome os itens.</summary>
        DeliverItem,

        /// <summary>Acender/ligar os objetos QuestData.ObjectiveSwitchIds do mapa isométrico
        /// QuestData.ObjectiveMapId (lanternas, caixas de junção). Conta os que já estão
        /// ligados (não importa quando); com a quest aceita, ligar o último entrega na hora
        /// (ver ExplorationArea.Switches).</summary>
        SwitchOn,
    }
}
