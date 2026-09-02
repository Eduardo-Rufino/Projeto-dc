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

        // Piso do reescalonamento, pra nunca gerar um time absurdamente fraco de uma vez só
        // (evita spikes de dificuldade). Sem teto pra cima: o time inimigo precisa
        // acompanhar o poder do time do jogador mesmo depois de muito treino/evolução, senão
        // as batalhas ficam triviais no fim de jogo (era o comportamento antigo, com teto de
        // 1.6x).
        private const double MinScaleRatio = 0.6;

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

                    // Nunca acima do nível do jogador - não faz sentido o time inimigo
                    // começar com vantagem de nível que o jogador não teve chance de
                    // alcançar; a variedade/desafio já vem do estágio aleatório e da
                    // margem pra baixo.
                    int minLevel = Math.Max(1, player.Level - 2);
                    int maxLevel = player.Level;

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
            CapEnemyDefense(playerTeam, enemies);

            return enemies;
        }

        /// <summary>
        /// Reescala os stats do time inimigo (todo mundo pela mesma proporção) pra aproximar
        /// o poder de combate total do time do jogador, respeitando o piso de MinScaleRatio.
        /// Só o Digimon inimigo (recém-criado, descartável) é alterado - o time do jogador
        /// nunca é tocado.
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

            ratio = Math.Max(ratio, MinScaleRatio);

            foreach (var enemy in enemyTeam)
            {
                ScaleStats(enemy.CurrentStats, ratio);

                enemy.MaxHealthPoints = enemy.CurrentStats.HealthPoints;
                enemy.CurrentHealthPoints = enemy.MaxHealthPoints;
            }
        }

        // A batalha calcula dano como ATK - DEF (mín. 1) - ver DamageCalculator. Isso é bem
        // diferente de "poder total": duas equipes com a mesma soma de stats podem ser
        // completamente injustas se a defesa do inimigo chegar perto do ataque do jogador,
        // porque aí o jogador passa a bater sempre (ou quase sempre) no mínimo de 1, não
        // importa quanto treinou. BalanceTeamPower (soma linear de stats) não enxerga essa
        // não-linearidade. Esse teto garante que a defesa do inimigo nunca deixe o jogador
        // preso nesse piso.
        private const double MaxDefenseToAttackRatio = 0.85;

        private void CapEnemyDefense(List<DigimonInstance> playerTeam, List<DigimonInstance> enemyTeam)
        {
            if (playerTeam.Count == 0 || enemyTeam.Count == 0)
                return;

            double avgPhysicalAttack = playerTeam.Average(d => d.CurrentStats.PhysicalDamage);
            double avgSpecialAttack = playerTeam.Average(d => d.CurrentStats.SpecialDamage);

            int maxPhysicalDefense = Math.Max(1, (int)(avgPhysicalAttack * MaxDefenseToAttackRatio));
            int maxSpecialDefense = Math.Max(1, (int)(avgSpecialAttack * MaxDefenseToAttackRatio));

            foreach (var enemy in enemyTeam)
            {
                enemy.CurrentStats.PhysicalDefense = Math.Min(
                    enemy.CurrentStats.PhysicalDefense, maxPhysicalDefense
                );

                enemy.CurrentStats.SpecialDefense = Math.Min(
                    enemy.CurrentStats.SpecialDefense, maxSpecialDefense
                );
            }
        }

        // Diferente da batalha livre (que lê o poder real do time do jogador), o time de um
        // campeonato é fixo por design (TournamentData.Opponents) - não recalibra pelo time
        // que aparece pra batalhar. Sem esse bônus, um campeonato calibrado pro nível
        // recomendado vira um passeio pra um time que chegou lá bem treinado, já que os
        // oponentes só ganham os incrementos fixos de level up (sem treino nenhum). Esse
        // valor estima quanto de treino um time do jogador teria acumulado até alcançar
        // aquele nível e aplica como bônus percentual nos stats do oponente, pra o
        // campeonato continuar sendo um desafio real pro nível a que se propõe. Ajustar aqui
        // pra tunar a dificuldade geral dos campeonatos.
        private const double TournamentTrainingBonusPerLevel = 0.025;
        private const double MaxTournamentTrainingBonus = 1.0;

        /// <summary>
        /// Aplica o bônus estimado de treino (ver TournamentTrainingBonusPerLevel) a um
        /// oponente fixo de campeonato, escalando todos os stats. Deve ser chamado depois do
        /// enemy já estar no nível final (level up) e com HP restaurado.
        /// </summary>
        public void ApplyTournamentTrainingBonus(DigimonInstance enemy)
        {
            double bonus = Math.Min(MaxTournamentTrainingBonus, enemy.Level * TournamentTrainingBonusPerLevel);

            ScaleStats(enemy.CurrentStats, 1.0 + bonus);

            enemy.MaxHealthPoints = enemy.CurrentStats.HealthPoints;
            enemy.CurrentHealthPoints = enemy.MaxHealthPoints;
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
