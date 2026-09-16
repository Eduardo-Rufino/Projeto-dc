using ProjetoDC.Enums;

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
