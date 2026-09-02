using Godot;
using ProjetoDC.Enums;
using System;

namespace ProjetoDC.Scripts.Systems.Battle
{
    /// <summary>
    /// Fórmula de dano da batalha (extraída do antigo BattleSystem 1x1, sem alteração de
    /// comportamento): ATK - DEF do alvo (mínimo 1), multiplicador de vantagem de tipo
    /// (ver <see cref="TypeAdvantageCalculator"/>), variação aleatória ±10%, crítico com
    /// 10% de chance dobra o dano. Lê os stats via <see cref="BattleCombatant.GetEffectiveStat"/>,
    /// então buffs/debuffs ativos já entram na conta.
    /// </summary>
    public static class DamageCalculator
    {
        private static readonly Random _random = new();

        public static int ResolveAttack(BattleCombatant attacker, BattleCombatant defender)
        {
            int damage = CalculateBaseDamage(attacker, defender);

            damage = ApplyTypeAdvantage(damage, attacker, defender);
            damage = ApplyDamageVariance(damage);
            damage = ApplyCritical(damage);

            defender.Digimon.TakeDamage(damage);

            GD.Print($"{attacker.Digimon.BaseData.Name} causou {damage} de dano em {defender.Digimon.BaseData.Name}");

            return damage;
        }

        private static int CalculateBaseDamage(BattleCombatant attacker, BattleCombatant defender)
        {
            bool isPhysical = attacker.Digimon.BaseData.AttackType == AttackType.Physical;

            int attack = attacker.GetEffectiveStat(
                isPhysical ? StatType.PhysicalDamage : StatType.SpecialDamage
            );

            int defense = defender.GetEffectiveStat(
                isPhysical ? StatType.PhysicalDefense : StatType.SpecialDefense
            );

            return Math.Max(1, attack - defense);
        }

        private static int ApplyTypeAdvantage(int damage, BattleCombatant attacker, BattleCombatant defender)
        {
            float multiplier = TypeAdvantageCalculator.GetDamageMultiplier(
                attacker.Digimon.BaseData,
                defender.Digimon.BaseData
            );

            if (multiplier != 1.0f)
            {
                GD.Print(
                    multiplier > 1.0f
                        ? $"Vantagem de tipo! x{multiplier}"
                        : $"Desvantagem de tipo! x{multiplier}"
                );
            }

            return Math.Max(1, (int)(damage * multiplier));
        }

        private static int ApplyDamageVariance(int damage)
        {
            float variation = _random.Next(-10, 11) / 100f;

            damage += (int)(damage * variation);

            return Math.Max(1, damage);
        }

        private static int ApplyCritical(int damage)
        {
            int roll = _random.Next(1, 101);

            if (roll <= 10)
            {
                GD.Print("CRITICO!");
                return damage * 2;
            }

            return damage;
        }
    }
}
