using ProjetoDC.Enums;
using ProjetoDC.Scripts.Data;
using ProjetoDC.Scripts.Gameplay;
using ProjetoDC.Scripts.Managers;
using ProjetoDC.Scripts.Systems.Battle;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjetoDC.Scripts.Systems.Passives
{
    /// <summary>
    /// Passivas herdadas: cada espécie pela qual o Digimon passou (DigimonInstance.EvolutionHistory)
    /// pode deixar um efeito (DigimonData.InheritedPassive), e todos se acumulam na forma atual. O save só
    /// guarda os ids de espécie - o efeito é resolvido do banco a cada luta (GetActiveInheritedPassives).
    ///
    /// Sistema separado das passivas próprias (PassiveId / PassiveSystem): os ganchos daqui são chamados
    /// ao lado dos delas em DamageCalculator.ResolveAttack, BattleUnit e BattleCombatant, sem tocar nelas.
    /// Cura de passiva herdada obedece o mesmo decaimento das próprias (PASSIVAS_SPEC.md R2). O estado de
    /// luta fica em BattleCombatant.Inherited (InheritedBattleState).
    /// </summary>
    public static class InheritedPassiveSystem
    {
        /// <summary>Atordoamento do Astamon, em segundos: o alvo para de agir e de andar.</summary>
        public const double StunDurationSeconds = 1.0;

        /// <summary>Limiar de "alvo abaixo de 50%" do Astamon.</summary>
        public const float StunLowHpThreshold = 0.5f;

        private static readonly Random _random = new();

        /// <summary>Sorteio de 0 a 1. Só os testes trocam (pra forçar ou impedir as chances).</summary>
        public static Func<double> Roll { get; set; } = () => _random.NextDouble();

        // ---------------------------------------------------------------- resolução

        /// <summary>
        /// Passivas herdadas ativas do Digimon: a de cada espécie distinta do histórico que tenha uma, na
        /// ordem em que ele passou por elas. Espécie repetida (só acontece via Jogress) conta uma vez.
        /// A forma ATUAL não entra - ela ainda não ficou pra trás.
        /// </summary>
        public static List<(DigimonData Species, InheritedPassiveData Passive)> GetActiveInheritedPassives(DigimonInstance digimon)
        {
            var result = new List<(DigimonData, InheritedPassiveData)>();

            if (digimon?.EvolutionHistory == null || DatabaseManager.Instance == null)
                return result;

            foreach (int id in digimon.EvolutionHistory.Distinct())
            {
                var species = DatabaseManager.Instance.GetDigimon(id);

                if (species?.InheritedPassive != null)
                    result.Add((species, species.InheritedPassive));
            }

            return result;
        }

        public static InheritedBattleState CreateBattleState(DigimonInstance digimon) =>
            new(GetActiveInheritedPassives(digimon));

        // ---------------------------------------------------------------- ganchos de DamageCalculator.ResolveAttack

        /// <summary>Antes de tudo: o defensor esquiva (Lamiamon)? Se sim, o golpe não acontece.</summary>
        public static bool TryDodge(BattleCombatant attacker, BattleCombatant defender)
        {
            attacker.Inherited.LastAttackDodged = false;
            attacker.Inherited.LastAttackNegated = false;
            attacker.Inherited.LastAttackCrit = false;
            attacker.Inherited.LastAttackStunned = false;

            float chance = defender.Inherited.Sum(InheritedEffectType.DodgeThenGuaranteedCrit);

            if (chance <= 0f || Roll() >= chance)
                return false;

            defender.Inherited.GuaranteedCritPending = true;
            attacker.Inherited.LastAttackDodged = true;
            return true;
        }

        /// <summary>Logo depois do dano base (ataque - defesa): Devimon ignora parte da defesa a cada N ataques.</summary>
        public static int ModifyBaseDamage(int damage, BattleCombatant attacker, BattleCombatant defender)
        {
            var pierce = attacker.Inherited.All(InheritedEffectType.PeriodicDefensePierce).FirstOrDefault();

            if (pierce == null)
                return damage;

            if (attacker.Inherited.PierceCounter < (int)pierce.Value)
            {
                attacker.Inherited.PierceCounter++;
                return damage;
            }

            attacker.Inherited.PierceCounter = 0;

            bool isPhysical = attacker.Digimon.BaseData.AttackType == AttackType.Physical;
            int attack = attacker.GetEffectiveStat(isPhysical ? StatType.PhysicalDamage : StatType.SpecialDamage);
            int defense = defender.GetEffectiveStat(isPhysical ? StatType.PhysicalDefense : StatType.SpecialDefense);
            int pierced = Math.Max(1, attack - (int)(defense * (1f - pierce.Value2)));

            return Math.Max(damage, pierced);
        }

        /// <summary>Antes do sorteio de crítico: esse ataque é crítico garantido? (Lamiamon depois de
        /// esquivar; Destromon na estaca final.)</summary>
        public static bool ConsumeGuaranteedCrit(BattleCombatant attacker)
        {
            bool guaranteed = false;

            if (attacker.Inherited.GuaranteedCritPending)
            {
                attacker.Inherited.GuaranteedCritPending = false;
                guaranteed = true;
            }

            var stacks = attacker.Inherited.All(InheritedEffectType.RangedStacksGuaranteedCrit).FirstOrDefault();

            if (stacks != null && IsRanged(attacker))
            {
                attacker.Inherited.RangedStacks++;

                if (attacker.Inherited.RangedStacks >= (int)stacks.Value)
                {
                    attacker.Inherited.RangedStacks = 0;
                    guaranteed = true;
                }
            }

            return guaranteed;
        }

        /// <summary>Bônus de dano do atacante (depois das passivas próprias e dos conjuntos).</summary>
        public static int ModifyOutgoingDamage(int damage, BattleCombatant attacker, BattleCombatant defender)
        {
            var st = attacker.Inherited;

            if (st.Passives.Count == 0)
                return damage;

            bool physical = attacker.Digimon.BaseData.AttackType == AttackType.Physical;
            bool opening = st.AttacksDealt == 0;

            if (!st.LowHpBonusActive)
                CheckLowHpBonus(attacker);

            float multiplier = 1f;

            foreach (var (_, p) in st.Passives)
            {
                switch (p.Type)
                {
                    case InheritedEffectType.PhysicalDamageDealtPercent when physical:
                        multiplier *= 1f + p.Value;
                        break;

                    case InheritedEffectType.SpeedAndBonusVsActedTarget when defender.Inherited.HasActed:
                        multiplier *= 1f + p.Value2;
                        break;

                    case InheritedEffectType.LowHpPhysicalDamageBonus when physical && st.LowHpBonusActive:
                        multiplier *= 1f + p.Value2;
                        break;

                    case InheritedEffectType.AntiTank when defender.Digimon.BaseData.Role == RoleType.Tank:
                        multiplier *= 1f + p.Value;
                        break;

                    case InheritedEffectType.PhysicalDamageWithOpeningBonus when physical:
                        multiplier *= 1f + (opening ? p.Value2 : p.Value);
                        break;

                    case InheritedEffectType.OpeningHitBonus when opening:
                        multiplier *= 1f + p.Value;
                        break;
                }
            }

            return multiplier == 1f ? damage : Math.Max(1, (int)Math.Round(damage * multiplier));
        }

        /// <summary>Redução do dano recebido (depois das passivas próprias do defensor). Pode zerar o golpe
        /// (Triceramon).</summary>
        public static int ModifyIncomingDamage(int damage, BattleCombatant attacker, BattleCombatant defender)
        {
            var st = defender.Inherited;
            bool firstHit = st.HitsTaken == 0;
            st.HitsTaken++;

            if (st.Passives.Count == 0)
                return damage;

            float negate = st.Sum(InheritedEffectType.NegateDamageChance);

            if (negate > 0f && Roll() < negate)
            {
                attacker.Inherited.LastAttackNegated = true;
                return 0;
            }

            float multiplier = 1f;

            float maxHp = st.Sum(InheritedEffectType.MaxHpPercent);
            if (maxHp > 0f)
                multiplier /= 1f + maxHp;

            if (firstHit)
            {
                foreach (var p in st.All(InheritedEffectType.FirstHitTakenReduction))
                    multiplier *= 1f - p.Value;
            }

            if (attacker.Digimon.BaseData.Role == RoleType.Tank)
            {
                foreach (var p in st.All(InheritedEffectType.AntiTank))
                    multiplier *= p.Value2;
            }

            return multiplier == 1f ? damage : Math.Max(1, (int)Math.Round(damage * multiplier));
        }

        /// <summary>Depois do dano aplicado: efeitos de crítico, atordoamento, contadores e o limiar do
        /// MetalGreymon de quem apanhou.</summary>
        public static void AfterAttack(BattleCombatant attacker, BattleCombatant defender, int damage, bool wasCrit, double elapsedSeconds)
        {
            var st = attacker.Inherited;
            st.AttacksDealt++;
            st.LastAttackCrit = wasCrit;

            if (!defender.Inherited.LowHpBonusActive)
                CheckLowHpBonus(defender);

            if (st.Passives.Count == 0)
                return;

            float healMultiplier = BattleCombatant.GetHealEffectivenessMultiplier(elapsedSeconds);

            if (wasCrit)
            {
                float critHeal = st.Sum(InheritedEffectType.CritHealMaxHp);
                if (critHeal > 0f)
                    Heal(attacker, (int)(attacker.Digimon.MaxHealthPoints * critHeal * healMultiplier));

                float lifesteal = st.Sum(InheritedEffectType.CritLifesteal);
                if (lifesteal > 0f && damage > 0)
                    Heal(attacker, (int)(damage * lifesteal * healMultiplier));

                float actions = st.Sum(InheritedEffectType.CritShortensDebuffs);
                if (actions > 0f)
                    ShortenDebuffs(attacker, actions * st.ActionIntervalSeconds);
            }

            var stun = st.All(InheritedEffectType.StunChance).FirstOrDefault();
            if (stun != null && damage > 0 && !defender.IsDead)
            {
                float chance = defender.HpPercentage < StunLowHpThreshold ? stun.Value2 : stun.Value;

                if (Roll() < chance)
                {
                    defender.Inherited.StunRemainingSeconds = Math.Max(defender.Inherited.StunRemainingSeconds, StunDurationSeconds);
                    st.LastAttackStunned = true;
                }
            }
        }

        // ---------------------------------------------------------------- ganchos de BattleUnit

        /// <summary>LadyDevimon: derrubou um inimigo com o golpe principal.</summary>
        public static void OnKill(BattleCombatant attacker)
        {
            float bonus = attacker.Inherited.Sum(InheritedEffectType.KillSpeedBonus);

            if (bonus > 0f)
                attacker.Inherited.KillSpeedBonus += bonus;
        }

        /// <summary>Garurumon: dano do golpe extra deste ataque à distância (0 = não saiu).</summary>
        public static int RollExtraStrike(BattleCombatant attacker, int damage)
        {
            if (damage <= 0 || !IsRanged(attacker))
                return 0;

            foreach (var p in attacker.Inherited.All(InheritedEffectType.RangedExtraStrike))
            {
                if (Roll() < p.Value)
                    return Math.Max(1, (int)(damage * p.Value2));
            }

            return 0;
        }

        /// <summary>Myotismon: cura o aliado vivo (fora o próprio) com menor % de HP. Devolve quem foi
        /// curado e quanto, ou (null, 0).</summary>
        public static (BattleCombatant Ally, int Amount) HealLowestAlly(BattleCombatant attacker, IEnumerable<BattleCombatant> team, int damage, double elapsedSeconds)
        {
            float percent = attacker.Inherited.Sum(InheritedEffectType.HealLowestAllyOnAttack);

            if (percent <= 0f || damage <= 0 || team == null)
                return (null, 0);

            var ally = team
                .Where(c => c != attacker && !c.IsDead && c.Digimon.CurrentHealthPoints < c.Digimon.MaxHealthPoints)
                .OrderBy(c => c.HpPercentage)
                .FirstOrDefault();

            if (ally == null)
                return (null, 0);

            int amount = (int)(damage * percent * BattleCombatant.GetHealEffectivenessMultiplier(elapsedSeconds));

            return (ally, Heal(ally, amount));
        }

        // ---------------------------------------------------------------- texto pra UI

        /// <summary>Efeito resumido, pra ficha do Digimon.</summary>
        public static string Describe(InheritedPassiveData p)
        {
            string Pct(float v) => $"{v * 100f:0.#}%";

            return p.Type switch
            {
                InheritedEffectType.MaxHpPercent => $"+{Pct(p.Value)} HP máx",
                InheritedEffectType.SpeedPercent => $"+{Pct(p.Value)} Velocidade",
                InheritedEffectType.PhysicalAttackPercent => $"+{Pct(p.Value)} PA",
                InheritedEffectType.PhysicalDefensePercent => $"+{Pct(p.Value)} PD",
                InheritedEffectType.SpecialDefensePercent => $"+{Pct(p.Value)} ED",
                InheritedEffectType.PhysicalDamageDealtPercent => $"+{Pct(p.Value)} dano físico",
                InheritedEffectType.CritShortensDebuffs => $"Crítico encurta efeitos negativos em {p.Value:0} ação",
                InheritedEffectType.PeriodicDefensePierce => $"A cada {p.Value:0} ataques, o próximo ignora {Pct(p.Value2)} da defesa",
                InheritedEffectType.SpeedAndBonusVsActedTarget => $"+{Pct(p.Value)} Velocidade; +{Pct(p.Value2)} dano em quem já agiu",
                InheritedEffectType.RangedExtraStrike => $"À distância: {Pct(p.Value)} de chance de golpe extra ({Pct(p.Value2)} do dano)",
                InheritedEffectType.FirstHitTakenReduction => $"1º golpe recebido: -{Pct(p.Value)} de dano",
                InheritedEffectType.LowHpPhysicalDamageBonus => $"Abaixo de {Pct(p.Value)} HP: +{Pct(p.Value2)} dano físico (até o fim)",
                InheritedEffectType.CritHealMaxHp => $"Crítico cura {Pct(p.Value)} do HP máx",
                InheritedEffectType.KillSpeedBonus => $"Derrotar inimigo: +{Pct(p.Value)} Velocidade (até o fim)",
                InheritedEffectType.DodgeThenGuaranteedCrit => $"+{Pct(p.Value)} esquiva; depois de esquivar, crítico garantido",
                InheritedEffectType.CritLifesteal => $"Crítico drena {Pct(p.Value)} do dano",
                InheritedEffectType.HealLowestAllyOnAttack => $"Cada ataque cura o aliado mais ferido em {Pct(p.Value)} do dano",
                InheritedEffectType.AntiTank => $"+{Pct(p.Value)} dano em Tanks; recebe {Pct(p.Value2)} do dano de Tanks",
                InheritedEffectType.NegateDamageChance => $"{Pct(p.Value)} de chance de ignorar todo o dano",
                InheritedEffectType.PhysicalDamageWithOpeningBonus => $"+{Pct(p.Value)} dano físico ({Pct(p.Value2)} no 1º golpe)",
                InheritedEffectType.OpeningHitBonus => $"+{Pct(p.Value)} dano no 1º golpe",
                InheritedEffectType.RangedStacksGuaranteedCrit => $"À distância: a cada {p.Value:0} ataques, crítico garantido",
                InheritedEffectType.StunChance => $"{Pct(p.Value)} de chance de atordoar ({Pct(p.Value2)} em alvo abaixo de 50%)",
                InheritedEffectType.DefensesUpSpeedDown => $"+{Pct(p.Value)} PD e ED, {Pct(p.Value2)} Velocidade",
                _ => p.Type.ToString(),
            };
        }

        // ---------------------------------------------------------------- apoio

        private static bool IsRanged(BattleCombatant c) => c.Digimon.BaseData.Role == RoleType.Ranged;

        private static void CheckLowHpBonus(BattleCombatant c)
        {
            foreach (var p in c.Inherited.All(InheritedEffectType.LowHpPhysicalDamageBonus))
            {
                if (!c.IsDead && c.HpPercentage < p.Value)
                {
                    c.Inherited.LowHpBonusActive = true;
                    return;
                }
            }
        }

        private static int Heal(BattleCombatant c, int amount)
        {
            if (amount <= 0 || c.IsDead)
                return 0;

            int before = c.Digimon.CurrentHealthPoints;
            c.Digimon.CurrentHealthPoints = Math.Min(c.Digimon.MaxHealthPoints, before + amount);

            return c.Digimon.CurrentHealthPoints - before;
        }

        private static void ShortenDebuffs(BattleCombatant c, double seconds)
        {
            foreach (var effect in c.Effects.Where(e => e.Percentage < 0))
                effect.RemainingSeconds -= seconds;

            c.Effects.RemoveAll(e => e.RemainingSeconds <= 0);
        }
    }
}
