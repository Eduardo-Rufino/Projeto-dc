using ProjetoDC.Enums;
using ProjetoDC.Scripts.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjetoDC.Scripts.Systems.Battle
{
    /// <summary>
    /// Estado das passivas herdadas de UM combatente durante UMA luta (ver InheritedPassiveSystem).
    /// Vive em BattleCombatant.Inherited, criado junto com ele e descartado no fim - nada daqui vai pro
    /// save. Também guarda os fatos de luta que as passivas dos OUTROS consultam (ex.: HasActed, pro
    /// Dimetromon de quem ataca este combatente), por isso existe mesmo pra quem não herdou nada.
    /// </summary>
    public class InheritedBattleState
    {
        /// <summary>Espécie de origem + efeito, na ordem do histórico, sem espécies repetidas.</summary>
        public IReadOnlyList<(DigimonData Species, InheritedPassiveData Passive)> Passives { get; }

        public InheritedBattleState(IReadOnlyList<(DigimonData Species, InheritedPassiveData Passive)> passives)
        {
            Passives = passives ?? Array.Empty<(DigimonData, InheritedPassiveData)>();
        }

        public bool Has(InheritedEffectType type) => Passives.Any(p => p.Passive.Type == type);

        public IEnumerable<InheritedPassiveData> All(InheritedEffectType type) =>
            Passives.Where(p => p.Passive.Type == type).Select(p => p.Passive);

        /// <summary>Soma de Value de todas as passivas desse tipo (duas espécies com o mesmo efeito somam).</summary>
        public float Sum(InheritedEffectType type) => All(type).Sum(p => p.Value);

        // ---------------------------------------------------------------- fatos de luta

        /// <summary>Esse combatente já executou alguma ação nesta luta (ver BattleUnit.ApplyAction).</summary>
        public bool HasActed { get; set; }

        /// <summary>Duração de uma ação dele em segundos (o cooldown efetivo da última ação) - é o "turno"
        /// do Greymon numa luta em tempo real. 1.5s até a primeira ação.</summary>
        public double ActionIntervalSeconds { get; set; } = 1.5;

        /// <summary>Ataques que conectaram (não esquivados) desferidos nesta luta.</summary>
        public int AttacksDealt { get; set; }

        /// <summary>Golpes recebidos (não esquivados) nesta luta.</summary>
        public int HitsTaken { get; set; }

        // ---------------------------------------------------------------- contadores das passivas

        /// <summary>Devimon: ataques desde o último golpe que ignorou defesa.</summary>
        public int PierceCounter { get; set; }

        /// <summary>Destromon: estacas acumuladas.</summary>
        public int RangedStacks { get; set; }

        /// <summary>MetalGreymon: já caiu abaixo do limiar nesta luta (o bônus fica até o fim).</summary>
        public bool LowHpBonusActive { get; set; }

        /// <summary>Lamiamon: esquivou e o próximo ataque é crítico garantido.</summary>
        public bool GuaranteedCritPending { get; set; }

        /// <summary>LadyDevimon: Velocidade ganha derrotando inimigos.</summary>
        public float KillSpeedBonus { get; set; }

        /// <summary>Astamon (no alvo): segundos de atordoamento restantes.</summary>
        public double StunRemainingSeconds { get; set; }

        public bool IsStunned => StunRemainingSeconds > 0;

        // ---------------------------------------------------------------- resultado do último golpe (pra UI)

        public bool LastAttackDodged { get; set; }
        public bool LastAttackNegated { get; set; }
        public bool LastAttackCrit { get; set; }
        public bool LastAttackStunned { get; set; }

        public void Tick(double delta)
        {
            if (StunRemainingSeconds > 0)
                StunRemainingSeconds -= delta;
        }

        /// <summary>% somado ao stat em BattleCombatant.GetEffectiveStat.</summary>
        public float GetStatPercent(StatType stat)
        {
            float total = 0f;

            foreach (var (_, p) in Passives)
            {
                switch (p.Type)
                {
                    case InheritedEffectType.SpeedPercent when stat == StatType.Speed:
                    case InheritedEffectType.SpeedAndBonusVsActedTarget when stat == StatType.Speed:
                    case InheritedEffectType.PhysicalAttackPercent when stat == StatType.PhysicalDamage:
                    case InheritedEffectType.PhysicalDefensePercent when stat == StatType.PhysicalDefense:
                    case InheritedEffectType.SpecialDefensePercent when stat == StatType.SpecialDefense:
                        total += p.Value;
                        break;

                    case InheritedEffectType.DefensesUpSpeedDown:
                        if (stat == StatType.PhysicalDefense || stat == StatType.SpecialDefense)
                            total += p.Value;
                        else if (stat == StatType.Speed)
                            total += p.Value2;
                        break;
                }
            }

            if (stat == StatType.Speed)
                total += KillSpeedBonus;

            return total;
        }
    }
}
