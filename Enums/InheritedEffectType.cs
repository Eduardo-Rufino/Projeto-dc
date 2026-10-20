namespace ProjetoDC.Enums
{
    /// <summary>
    /// Efeito de uma passiva herdada (ver InheritedPassiveData / InheritedPassiveSystem). Cada espécie
    /// pode ter no máximo uma; o Digimon acumula a de toda espécie pela qual já passou
    /// (DigimonInstance.EvolutionHistory). Os números ficam no JSON da espécie (Value/Value2), não aqui.
    /// Só acrescentar membros no FIM: o save grava enums como número.
    /// </summary>
    public enum InheritedEffectType
    {
        /// <summary>+Value de HP máximo (na luta: o dano recebido é dividido por 1+Value).</summary>
        MaxHpPercent,

        /// <summary>+Value de Velocidade.</summary>
        SpeedPercent,

        /// <summary>+Value de PA (Ataque Físico).</summary>
        PhysicalAttackPercent,

        /// <summary>+Value de PD (Defesa Física).</summary>
        PhysicalDefensePercent,

        /// <summary>+Value de ED (Defesa Especial).</summary>
        SpecialDefensePercent,

        /// <summary>+Value no dano final de ataques físicos.</summary>
        PhysicalDamageDealtPercent,

        /// <summary>Ao acertar crítico, cada efeito negativo ativo no Digimon perde Value ações de duração.</summary>
        CritShortensDebuffs,

        /// <summary>A cada Value ataques, o próximo ignora Value2 da defesa do alvo.</summary>
        PeriodicDefensePierce,

        /// <summary>+Value de Velocidade; +Value2 de dano contra alvo que já agiu na luta.</summary>
        SpeedAndBonusVsActedTarget,

        /// <summary>Ataque à distância: Value de chance de um golpe extra imediato com Value2 do dano.</summary>
        RangedExtraStrike,

        /// <summary>O 1º golpe recebido na luta causa Value a menos.</summary>
        FirstHitTakenReduction,

        /// <summary>Uma vez abaixo de Value de HP: +Value2 de dano físico pelo resto da luta.</summary>
        LowHpPhysicalDamageBonus,

        /// <summary>Ao acertar crítico, cura Value do HP máximo.</summary>
        CritHealMaxHp,

        /// <summary>Ao derrotar um inimigo com o golpe principal: +Value de Velocidade pelo resto da luta.</summary>
        KillSpeedBonus,

        /// <summary>Value de chance de esquivar; depois de esquivar, o próximo ataque é crítico garantido.</summary>
        DodgeThenGuaranteedCrit,

        /// <summary>Ao acertar crítico, cura Value do dano causado.</summary>
        CritLifesteal,

        /// <summary>A cada ataque, cura o aliado com menor % de HP em Value do dano causado.</summary>
        HealLowestAllyOnAttack,

        /// <summary>+Value de dano contra Tanks; dano recebido de Tanks multiplicado por Value2.</summary>
        AntiTank,

        /// <summary>Value de chance de ignorar todo o dano de um golpe recebido.</summary>
        NegateDamageChance,

        /// <summary>+Value de dano físico (+Value2 no 1º golpe desferido da luta).</summary>
        PhysicalDamageWithOpeningBonus,

        /// <summary>+Value de dano no 1º golpe desferido da luta.</summary>
        OpeningHitBonus,

        /// <summary>Cada ataque à distância soma 1 estaca; ao chegar em Value, esse ataque é crítico garantido.</summary>
        RangedStacksGuaranteedCrit,

        /// <summary>Value de chance de atordoar o alvo (Value2 se o alvo estiver abaixo de 50% de HP).</summary>
        StunChance,

        /// <summary>+Value de PD e ED, Value2 (negativo) de Velocidade.</summary>
        DefensesUpSpeedDown,

        /// <summary>+Value de EA (Ataque Especial).</summary>
        SpecialAttackPercent,
    }
}
