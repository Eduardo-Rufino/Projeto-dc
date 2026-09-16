namespace ProjetoDC.Enums
{
    /// <summary>Tipo de bônus que um DigimonSetData completo concede ao Center (ver
    /// GameManager.GetTotalSetBonus) - cada um soma entre todos os conjuntos completos desse
    /// tipo, então dois conjuntos com o mesmo EffectType empilham.</summary>
    public enum SetEffectType
    {
        /// <summary>+X% no dano final de qualquer Digimon do time em batalha (ver
        /// DamageCalculator.ApplySetBonuses).</summary>
        FinalDamagePercent,

        /// <summary>+X pontos percentuais na chance de crítico em batalha (ver
        /// DamageCalculator.ApplyCritical).</summary>
        CritChancePercent,

        /// <summary>+X% nos Bits ganhos ao vencer uma batalha (ver GameManager.
        /// ApplyTeamBattleReward).</summary>
        BitsGainedPercent,

        /// <summary>-X% na velocidade com que a Fome cai por hora (ver DigimonInstance.
        /// ConsumeHunger).</summary>
        HungerDecayReductionPercent,

        /// <summary>+X% em toda experiência ganha (treino ou batalha - ver DigimonInstance.
        /// GainExperience, o único ponto de entrada de XP).</summary>
        ExperienceGainedPercent,
    }
}
