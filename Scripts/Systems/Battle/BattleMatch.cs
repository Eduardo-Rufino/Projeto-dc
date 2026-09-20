using ProjetoDC.Enums;
using System.Linq;

namespace ProjetoDC.Scripts.Systems.Battle
{
    /// <summary>
    /// Estado puro de uma batalha 3x3 (sem Node). Substitui o antigo BattleSystem 1x1.
    /// </summary>
    public class BattleMatch
    {
        public BattleTeam PlayerTeam { get; }

        public BattleTeam EnemyTeam { get; }

        /// <summary>Tempo real de luta desde o início (usado pra ir enfraquecendo cura ao
        /// longo do combate - ver BattleUnit.GetHealEffectivenessMultiplier).</summary>
        public double ElapsedSeconds { get; private set; }

        /// <summary>Depois de tanto tempo de luta começa a morte súbita: todo mundo perde uma % fixa da
        /// própria vida máxima por segundo. Existe porque a luta não tinha fim garantido - um Suporte
        /// curando contra um Tank de pouco dano podia durar pra sempre (a cura já decai com o tempo,
        /// ver BattleCombatant.GetHealEffectivenessMultiplier, mas nunca chega a zero: o piso é 20%).
        /// 3% por segundo é maior que qualquer cura/regeneração no piso, então a luta sempre acaba, no
        /// máximo ~33s depois de começar. Quem estiver com a MENOR % de vida cai primeiro, então em
        /// teoria vence quem causou mais dano relativo à vida máxima do alvo antes disso.</summary>
        public const double SuddenDeathStartSeconds = 120.0;

        public const float SuddenDeathPercentOfMaxHpPerSecond = 0.03f;

        public bool IsSuddenDeath => ElapsedSeconds >= SuddenDeathStartSeconds;

        public BattleMatch(BattleTeam playerTeam, BattleTeam enemyTeam)
        {
            PlayerTeam = playerTeam;
            EnemyTeam = enemyTeam;
        }

        public int ResolveAttack(BattleCombatant attacker, BattleCombatant defender)
        {
            // Sede de Batalha (ver PASSIVAS_SPEC.md R2) precisa do tempo real de luta pra
            // saber o quanto a cura já decaiu.
            return DamageCalculator.ResolveAttack(attacker, defender, ElapsedSeconds);
        }

        public void Tick(double delta)
        {
            ElapsedSeconds += delta;

            PlayerTeam.Tick(delta, ElapsedSeconds);
            EnemyTeam.Tick(delta, ElapsedSeconds);

            ApplySuddenDeath(delta);
        }

        private void ApplySuddenDeath(double delta)
        {
            if (!IsSuddenDeath)
                return;

            // Quem tem a menor % de vida recebe primeiro: se o dano deste quadro zerar o último de um
            // time, a luta já está decidida e ninguém mais é tocado (empate exato = perde o mais fraco).
            foreach (var combatant in PlayerTeam.AliveMembers
                         .Concat(EnemyTeam.AliveMembers)
                         .OrderBy(c => c.HpPercentage)
                         .ToList())
            {
                combatant.ApplySuddenDeathDamage(SuddenDeathPercentOfMaxHpPerSecond, delta);

                if (CheckResult() != BattleResult.Ongoing)
                    break;
            }
        }

        public BattleResult CheckResult()
        {
            if (EnemyTeam.IsDefeated)
                return BattleResult.PlayerWon;

            if (PlayerTeam.IsDefeated)
                return BattleResult.EnemyWon;

            return BattleResult.Ongoing;
        }
    }
}
