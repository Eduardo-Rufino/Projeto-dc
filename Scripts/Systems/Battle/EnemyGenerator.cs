using Godot;
using ProjetoDC.Scripts.Data;
using ProjetoDC.Scripts.Gameplay;
using ProjetoDC.Scripts.Managers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjetoDC.Scripts.Systems.Battle
{
    public class EnemyGenerator
    {
        // Diferença de poder tolerada antes de reescalar o time inimigo (evita ajustes
        // desnecessários quando o time gerado já está razoavelmente parelho).
        private const double PowerToleranceRatio = 0.15;

        // Limites do reescalonamento, pra nunca gerar um time absurdamente fraco/forte
        // de uma vez só (evita spikes de dificuldade).
        private const double MinScaleRatio = 0.6;
        private const double MaxScaleRatio = 1.6;

        private readonly Random _random = new();

        public List<DigimonInstance> GenerateEnemies(DigimonInstance player, int amount)
        {
            var enemies = new List<DigimonInstance>();

            try
            {
                var candidates = DatabaseManager.Instance
                    .GetAllDigimons()
                    .Where(x => x.Stage == player.BaseData.Stage)
                    .OrderBy(x => _random.Next())
                    .Take(amount);

                foreach (var data in candidates)
                {
                    var enemy = new DigimonInstance(data);

                    int minLevel = Math.Max(1, player.Level - 2);
                    int maxLevel = player.Level + 2;

                    int targetLevel = _random.Next(minLevel, maxLevel + 1);

                    while (enemy.Level < targetLevel)
                    {
                        enemy.GainExperience(enemy.ExperienceToNextLevel);
                    }

                    enemy.RestoreHealth();

                    enemies.Add(enemy);
                }

            }
            catch(Exception ex)
            {
                GD.PrintErr(ex.Message);
            }

            return enemies;
        }

        /// <summary>
        /// Gera um time inimigo calibrado por um time de jogador (batalha 3x3): cada inimigo
        /// nasce a partir do Stage/Level do membro correspondente do time do jogador
        /// (playerTeam[i % playerTeam.Count]), pra um time misto gerar oponentes igualmente mistos,
        /// e depois o time inteiro é reescalado pra ficar com poder de combate parecido com o
        /// time do jogador (dois Digimons do mesmo nível podem ter stats bem diferentes por
        /// causa de treino acumulado, então só bater nível/estágio não é suficiente).
        /// </summary>
        public List<DigimonInstance> GenerateEnemyTeam(List<DigimonInstance> playerTeam, int amount = 3)
        {
            var enemies = new List<DigimonInstance>();

            if (playerTeam == null || playerTeam.Count == 0)
                return enemies;

            for (int i = 0; i < amount; i++)
            {
                var reference = playerTeam[i % playerTeam.Count];

                var generated = GenerateEnemies(reference, 1);

                if (generated.Count > 0)
                    enemies.Add(generated[0]);
            }

            BalanceTeamPower(playerTeam, enemies);

            return enemies;
        }

        /// <summary>
        /// Reescala os stats do time inimigo (todo mundo pela mesma proporção) pra aproximar
        /// o poder de combate total do time do jogador, dentro dos limites de MinScaleRatio/
        /// MaxScaleRatio. Só o Digimon inimigo (recém-criado, descartável) é alterado - o
        /// time do jogador nunca é tocado.
        /// </summary>
        private void BalanceTeamPower(List<DigimonInstance> playerTeam, List<DigimonInstance> enemyTeam)
        {
            if (enemyTeam.Count == 0)
                return;

            double playerPower = CalculateTeamPower(playerTeam);
            double enemyPower = CalculateTeamPower(enemyTeam);

            if (enemyPower <= 0)
                return;

            double ratio = playerPower / enemyPower;

            if (Math.Abs(1.0 - ratio) < PowerToleranceRatio)
                return;

            ratio = Math.Clamp(ratio, MinScaleRatio, MaxScaleRatio);

            foreach (var enemy in enemyTeam)
            {
                ScaleStats(enemy.CurrentStats, ratio);

                enemy.MaxHealthPoints = enemy.CurrentStats.HealthPoints;
                enemy.CurrentHealthPoints = enemy.MaxHealthPoints;
            }
        }

        private double CalculateTeamPower(List<DigimonInstance> team)
        {
            return team.Sum(CalculatePower);
        }

        private double CalculatePower(DigimonInstance digimon)
        {
            var stats = digimon.CurrentStats;

            return stats.HealthPoints +
                stats.PhysicalDamage +
                stats.PhysicalDefense +
                stats.SpecialDamage +
                stats.SpecialDefense +
                stats.Speed;
        }

        private void ScaleStats(BaseStats stats, double ratio)
        {
            stats.HealthPoints = Math.Max(1, (int)(stats.HealthPoints * ratio));
            stats.PhysicalDamage = Math.Max(1, (int)(stats.PhysicalDamage * ratio));
            stats.PhysicalDefense = Math.Max(1, (int)(stats.PhysicalDefense * ratio));
            stats.SpecialDamage = Math.Max(1, (int)(stats.SpecialDamage * ratio));
            stats.SpecialDefense = Math.Max(1, (int)(stats.SpecialDefense * ratio));
            stats.Speed = Math.Max(1, (int)(stats.Speed * ratio));
        }
    }
}
