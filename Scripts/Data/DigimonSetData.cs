using ProjetoDC.Enums;
using System.Collections.Generic;

namespace ProjetoDC.Scripts.Data
{
    /// <summary>
    /// Dado estático de um "conjunto" de Digimon (carregado de Data/Sets/*.json pelo
    /// DatabaseManager) - um grupo temático (ex.: Lordes Demoníacos) que dá um bônus
    /// permanente ao Center quando TODAS as espécies listadas já foram descobertas pelo
    /// menos uma vez (ver CenterState.DiscoveredDigimonIds), mesmo que nenhuma esteja viva
    /// no momento. Reforça a ideia de que o Center é a progressão de verdade - o Digimon é
    /// passageiro, mas o registro na Enciclopédia não some com a morte dele.
    /// </summary>
    public class DigimonSetData
    {
        public int Id { get; set; }
        public string Name { get; set; }

        /// <summary>Texto mostrado no tooltip do nome do conjunto (EncyclopediaScreen) -
        /// descreve o efeito em prosa, não só o número.</summary>
        public string EffectDescription { get; set; }

        /// <summary>Que tipo de bônus esse conjunto dá (ver SetEffectType) - cada tipo tem seu
        /// próprio ponto de leitura (DamageCalculator, GameManager, DigimonInstance).</summary>
        public SetEffectType EffectType { get; set; }

        /// <summary>Força do bônus (0.10 = +10%, ou +10 pontos percentuais pra
        /// CritChancePercent) - ver GameManager.GetTotalSetBonus, que soma esse valor entre
        /// todo conjunto completo do mesmo EffectType.</summary>
        public float EffectValue { get; set; }

        public List<int> DigimonIds { get; set; } = new();
    }
}
