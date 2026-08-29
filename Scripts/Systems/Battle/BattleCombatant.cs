using ProjetoDC.Enums;
using ProjetoDC.Scripts.Gameplay;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjetoDC.Scripts.Systems.Battle
{
    /// <summary>
    /// Embrulha um <see cref="DigimonInstance"/> durante uma batalha, guardando os
    /// status effects (buffs/debuffs) temporários que afetam seus stats efetivos.
    /// Os stats "crus" do Digimon (CurrentStats) nunca são alterados por isso.
    /// </summary>
    public class BattleCombatant
    {
        private const int MaxStacksPerStat = 2;

        public DigimonInstance Digimon { get; }

        public List<StatusEffect> Effects { get; } = new();

        public BattleCombatant(DigimonInstance digimon)
        {
            Digimon = digimon;
        }

        public bool IsDead => Digimon.IsDead();

        public void Tick(double delta)
        {
            foreach (var effect in Effects)
            {
                effect.RemainingSeconds -= delta;
            }

            Effects.RemoveAll(e => e.RemainingSeconds <= 0);
        }

        /// <summary>
        /// Adiciona um novo status effect. Se já houver o mesmo Stat com o mesmo sinal
        /// (buff ou debuff) e o número de stacks já atingiu o teto, apenas reinicia a
        /// duração do mais antigo em vez de empilhar mais um.
        /// </summary>
        public void AddEffect(StatType stat, float percentage, double durationSeconds)
        {
            bool isBuff = percentage > 0;

            var sameKind = Effects
                .Where(e => e.Stat == stat && (e.Percentage > 0) == isBuff)
                .ToList();

            if (sameKind.Count >= MaxStacksPerStat)
            {
                sameKind[0].RemainingSeconds = durationSeconds;
                return;
            }

            Effects.Add(new StatusEffect
            {
                Stat = stat,
                Percentage = percentage,
                RemainingSeconds = durationSeconds
            });
        }

        public int GetEffectiveStat(StatType stat)
        {
            int baseValue = stat switch
            {
                StatType.PhysicalDamage => Digimon.CurrentStats.PhysicalDamage,
                StatType.PhysicalDefense => Digimon.CurrentStats.PhysicalDefense,
                StatType.SpecialDamage => Digimon.CurrentStats.SpecialDamage,
                StatType.SpecialDefense => Digimon.CurrentStats.SpecialDefense,
                StatType.Speed => Digimon.CurrentStats.Speed,
                _ => 0
            };

            float totalPercentage = Effects
                .Where(e => e.Stat == stat)
                .Sum(e => e.Percentage);

            int effective = (int)Math.Round(baseValue * (1f + totalPercentage));

            return Math.Max(1, effective);
        }

        public StatType AttackStat =>
            Digimon.BaseData.AttackType == AttackType.Physical
                ? StatType.PhysicalDamage
                : StatType.SpecialDamage;

        public float HpPercentage =>
            Digimon.MaxHealthPoints <= 0
                ? 0f
                : (float)Digimon.CurrentHealthPoints / Digimon.MaxHealthPoints;
    }
}
