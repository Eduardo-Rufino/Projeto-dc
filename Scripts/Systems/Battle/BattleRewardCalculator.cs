using ProjetoDC.Enums;
using ProjetoDC.Scripts.Gameplay;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjetoDC.Scripts.Systems.Battle
{
    public static class BattleRewardCalculator
    {
        /// <summary>
        /// Recompensa de uma batalha 3x3: soma XP/Bits de cada inimigo derrotado do time
        /// (calibrado pelo nível médio do time do jogador). O XP retornado já é a fração
        /// por membro (dividido pelo tamanho do time); os Bits são o total, pra somar uma
        /// única vez no Center.
        /// </summary>
        public static BattleReward CalculateForTeam(List<DigimonInstance> playerTeam, List<DigimonInstance> enemyTeam)
        {
            if (playerTeam == null || playerTeam.Count == 0 || enemyTeam == null || enemyTeam.Count == 0)
                return new BattleReward { Experience = 0, Bits = 0 };

            int referenceLevel = (int)Math.Round(playerTeam.Average(p => p.Level));

            int totalXp = 0;
            int totalBits = 0;

            foreach (var enemy in enemyTeam)
            {
                int baseXp = GetBaseExperience(enemy.BaseData.Stage);
                int baseBits = GetBaseBits(enemy.BaseData.Stage);

                double multiplier = GetLevelMultiplier(referenceLevel, enemy.Level);

                totalXp += (int)(baseXp * multiplier);
                totalBits += (int)(baseBits * multiplier);
            }

            return new BattleReward
            {
                Experience = totalXp / playerTeam.Count,
                Bits = totalBits
            };
        }

        private static int GetBaseExperience(DigimonStage stage)
        {
            return stage switch
            {
                DigimonStage.Baby => 20,
                DigimonStage.InTraining => 40,
                DigimonStage.Rookie => 80,
                DigimonStage.Champion => 180,
                DigimonStage.Ultimate => 400,
                DigimonStage.Mega => 900,
                _ => 50
            };
        }

        private static int GetBaseBits(DigimonStage stage)
        {
            return stage switch
            {
                DigimonStage.Baby => 50,
                DigimonStage.InTraining => 80,
                DigimonStage.Rookie => 200,
                DigimonStage.Champion => 500,
                DigimonStage.Ultimate => 1200,
                DigimonStage.Mega => 2500,
                _ => 100
            };
        }

        private static double GetLevelMultiplier(int playerLevel, int enemyLevel)
        {
            int difference = enemyLevel - playerLevel;

            return difference switch
            {
                <= -5 => 0.5,
                -4 => 0.6,
                -3 => 0.7,
                -2 => 0.8,
                -1 => 0.9,
                0 => 1.0,
                1 => 1.1,
                2 => 1.2,
                3 => 1.3,
                4 => 1.4,
                >= 5 => 1.5
            };
        }
    }
}