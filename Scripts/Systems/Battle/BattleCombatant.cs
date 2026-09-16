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

        // Estado de combate por passiva (ver PASSIVAS_SPEC.md seções 3/4) - efêmero, só faz
        // sentido durante ESSA luta, nunca persiste (diferente de Digimon.PassiveId, que é
        // fixo até a próxima evolução). Fica aqui, não em DigimonInstance, pelo mesmo motivo
        // de Effects: BattleCombatant "embrulha" o Digimon só enquanto dura o combate.

        /// <summary>Fúria Crescente (Warrior) - pilhas atuais e quanto falta pra decair (ver
        /// DamageCalculator).</summary>
        public int FuriaCrescenteStacks { get; set; }
        public double FuriaCrescenteDecayRemainingSeconds { get; set; }

        /// <summary>Golpe Pesado (Warrior) - contador até o 4º golpe.</summary>
        public int GolpePesadoHitCounter { get; set; }

        /// <summary>Alvo Marcado (Ranged) - contador até o 4º acerto.</summary>
        public int AlvoMarcadoHitCounter { get; set; }

        /// <summary>Golpe Sorrateiro (Assassin) - alvos que já sofreram o bônus de primeiro
        /// golpe nessa luta.</summary>
        public HashSet<BattleCombatant> GolpeSorrateiroHitTargets { get; } = new();

        /// <summary>Última Trincheira (Tank) - já disparou nessa luta (gatilho de uma vez só)
        /// e quanto falta da janela de -50% de dano ativa.</summary>
        public bool UltimaTrincheiraTriggered { get; set; }
        public double UltimaTrincheiraRemainingSeconds { get; set; }

        /// <summary>Investida (Warrior) - true entre o momento em que um dash termina e o
        /// próximo golpe acertado (que recebe o bônus e consome a flag - ver
        /// DamageCalculator.ApplyAttackerPassives/BattleUnit).</summary>
        public bool InvestidaPendingBonus { get; set; }

        /// <summary>Fixação (Ranged) - acertos consecutivos sem esquiva no meio, usado por
        /// BattleUnit.ComputeEffectiveCooldown pra reduzir o cooldown. Zera só numa esquiva
        /// (ver BattleUnit.PerformAction) - matar o alvo e trocar não conta como quebra.</summary>
        public int FixacaoConsecutiveHits { get; set; }

        /// <summary>Quem está provocando esse combatente agora (Taunt ativo) - null se não
        /// estiver sob Taunt. Espelha BattleUnit._forcedTarget/_forcedTargetRemaining, mas
        /// fica aqui porque DamageCalculator só enxerga BattleCombatant, não BattleUnit.
        /// Escrito por BattleUnit (ApplyTaunt e a cada frame em _Process), lido pelo hook
        /// on_damage_final_dealt (Grito de Guerra, ver DamageCalculator.
        /// ApplyDefenderPassives).</summary>
        public BattleCombatant TauntedBy { get; set; }

        /// <summary>Vigília (Healer, ver PASSIVAS_SPEC.md) - escudo que absorve dano ANTES do
        /// HP (ver DamageCalculator.ResolveAttack). "Um escudo por aliado, não renova até ser
        /// consumido" já sai de graça: BattleUnit.SelectTarget só escolhe aliados com
        /// ShieldAmount == 0 pra receber um novo.</summary>
        public int ShieldAmount { get; set; }

        // R2 da spec: decaimento de cura/regeneração/escudo de passiva - 100% de eficácia
        // nos primeiros HealDecayGraceSeconds de luta, depois -HealDecayPerStep a cada
        // HealDecayInterval segundos, nunca abaixo de MinHealEffectiveness. Compartilhado
        // entre BattleUnit (cura do Healer, escudo de Vigília) e aqui mesmo (Fôlego, Aura
        // Vital) - um só lugar pra essa curva, pra nunca divergir entre os dois.
        private const double HealDecayGraceSeconds = 30.0;
        private const double HealDecayInterval = 5.0;
        private const float HealDecayPerStep = 0.15f;
        private const float MinHealEffectiveness = 0.2f;

        public static float GetHealEffectivenessMultiplier(double elapsedSeconds)
        {
            if (elapsedSeconds <= HealDecayGraceSeconds)
                return 1f;

            double secondsPastGrace = elapsedSeconds - HealDecayGraceSeconds;
            int decaySteps = (int)(secondsPastGrace / HealDecayInterval) + 1;

            float multiplier = 1f - decaySteps * HealDecayPerStep;

            return Math.Max(MinHealEffectiveness, multiplier);
        }

        // Fôlego (Tank, ver PASSIVAS_SPEC.md): regen contínua por segundo.
        private const float FolegoRegenPercentPerSecond = 0.006f;

        // Acumulador de HP fracionário pra regen contínua (Fôlego, e Aura Vital via
        // BattleUnit.ApplyAuraVitalRegen) - DigimonInstance.RegenerateHealth tem um piso de
        // 1 HP por chamada (pensado pra chamadas esporádicas, ex.: 1x por hora no Center),
        // então usá-lo direto todo frame a 60fps explodiria uma % pequena pra ~60 HP/s só
        // pelo piso. Aqui só aplica HP de verdade quando a fração acumulada cruza 1.
        private float _pendingRegenHp;

        /// <summary>Aplica regeneração contínua (Fôlego/Aura Vital), respeitando o
        /// decaimento R2. Chamado a cada frame com o delta desse frame.</summary>
        public void ApplyContinuousRegen(float percentageOfMaxPerSecond, double delta, double elapsedSeconds)
        {
            if (Digimon.CurrentHealthPoints >= Digimon.MaxHealthPoints)
                return;

            float multiplier = GetHealEffectivenessMultiplier(elapsedSeconds);

            _pendingRegenHp += Digimon.MaxHealthPoints * percentageOfMaxPerSecond * (float)delta * multiplier;

            int wholeHp = (int)_pendingRegenHp;

            if (wholeHp <= 0)
                return;

            _pendingRegenHp -= wholeHp;

            Digimon.CurrentHealthPoints = Math.Min(Digimon.MaxHealthPoints, Digimon.CurrentHealthPoints + wholeHp);
        }

        public void Tick(double delta, double elapsedSeconds)
        {
            foreach (var effect in Effects)
            {
                effect.RemainingSeconds -= delta;
            }

            Effects.RemoveAll(e => e.RemainingSeconds <= 0);

            if (FuriaCrescenteDecayRemainingSeconds > 0)
                FuriaCrescenteDecayRemainingSeconds -= delta;

            if (UltimaTrincheiraRemainingSeconds > 0)
                UltimaTrincheiraRemainingSeconds -= delta;

            if (Digimon.PassiveId == PassiveType.Folego)
                ApplyContinuousRegen(FolegoRegenPercentPerSecond, delta, elapsedSeconds);
        }

        /// <summary>
        /// Adiciona um novo status effect. Se já houver o mesmo Stat com o mesmo sinal
        /// (buff ou debuff) e o número de stacks já atingiu o teto, apenas reinicia a
        /// duração do mais antigo em vez de empilhar mais um.
        /// </summary>
        /// <param name="maxStacksOverride">Teto de pilhas diferente do padrão
        /// (MaxStacksPerStat) - usado por Ressonância (ver PASSIVAS_SPEC.md), que permite
        /// uma 3ª pilha. Quem decide o VALOR de cada pilha continua sendo quem chama
        /// AddEffect (ver BattleUnit.ApplyAction), não este método.</param>
        public void AddEffect(StatType stat, float percentage, double durationSeconds, int? maxStacksOverride = null)
        {
            bool isBuff = percentage > 0;

            var sameKind = Effects
                .Where(e => e.Stat == stat && (e.Percentage > 0) == isBuff)
                .ToList();

            int maxStacks = maxStacksOverride ?? MaxStacksPerStat;

            if (sameKind.Count >= maxStacks)
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
