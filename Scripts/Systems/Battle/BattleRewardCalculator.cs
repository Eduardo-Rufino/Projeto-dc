using ProjetoDC.Enums;
using ProjetoDC.Scripts.Gameplay;

namespace ProjetoDC.Scripts.Systems.Battle
{
    public static class BattleRewardCalculator
    {
        public static BattleReward Calculate(DigimonInstance player, DigimonInstance enemy)
        {
            int baseXp = GetBaseExperience(enemy.BaseData.Stage);
            int baseBits = GetBaseBits(enemy.BaseData.Stage);

            double multiplier = GetLevelMultiplier(player.Level, enemy.Level);

            return new BattleReward
            {
                Experience = (int)(baseXp * multiplier),
                Bits = (int)(baseBits * multiplier)
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