using Godot;
using ProjetoDC.Enums;
using ProjetoDC.Scripts.Gameplay;
using ProjetoDC.Scripts.Systems.Results;

namespace ProjetoDC.Scripts.Systems.Training
{
    public class TrainingSystem
    {
        // HP ganha um múltiplo dos outros stats (a vida sempre teve um valor absoluto bem
        // maior que ATK/DEF/etc, então 1:1 deixaria HP andando de menos) - era 10x, jogadores
        // treinando só HP acumulavam vida desproporcional ao resto do time. Reduzido, mas
        // ainda maior que os outros stats de propósito.
        private const int HealthPointsGainMultiplier = 6;

        // Bônus das áreas de treino específicas por stat (ver CenterArea.GetForcedTrainingType)
        // sobre o ganho da área de treino genérica - "levemente mais status", não um upgrade
        // que torne a área inicial obsoleta.
        public const float SpecificAreaBonusMultiplier = 1.15f;

        private readonly RandomNumberGenerator _rng = new();

        public TrainingResult Execute(
            DigimonInstance digimon,
            TrainingType type,
            float gainMultiplier = 1f)
        {
            var result = new TrainingResult();

            if (digimon.Stamina < 10)
            {
                result.Success = false;
                result.Reason = "NOT_ENOUGH_STAMINA";
                return result;
            }

            result.Success = true;

            // Resultado do treinamento:
            // 1 = resultado ruim
            // 2 = resultado normal (mais comum)
            // 3 = resultado excelente
            int trainingResult = RollTrainingResult();

            switch (type)
            {
                case TrainingType.HealthPoints:
                    result.HealthPointsGained = ApplyGainMultiplier(HealthPointsGainMultiplier * trainingResult, gainMultiplier);
                    break;

                case TrainingType.Attack:
                    result.PhysicDamageGained = ApplyGainMultiplier(trainingResult, gainMultiplier);
                    break;

                case TrainingType.Defense:
                    result.PhysicDefenseGained = ApplyGainMultiplier(trainingResult, gainMultiplier);
                    break;

                case TrainingType.Speed:
                    result.SpeedGained = ApplyGainMultiplier(trainingResult, gainMultiplier);
                    break;

                case TrainingType.SpecialAttack:
                    result.SpecialDamageGained = ApplyGainMultiplier(trainingResult, gainMultiplier);
                    break;

                case TrainingType.SpecialDefense:
                    result.SpecialDefenseGained = ApplyGainMultiplier(trainingResult, gainMultiplier);
                    break;
            }

            result.StaminaCost = 10;
            result.ExpGained = 5;

            GD.Print(
                $"Treino: {digimon.BaseData.Name} | " +
                $"Tipo: {type} | " +
                $"Resultado: {trainingResult}"
            );

            return result;
        }

        private static int ApplyGainMultiplier(int baseGain, float multiplier)
        {
            return Mathf.RoundToInt(baseGain * multiplier);
        }

        private int RollTrainingResult()
        {
            int roll = _rng.RandiRange(1, 100);

            if (roll <= 25)
                return 1;

            if (roll <= 75)
                return 2;

            return 3;
        }
    }
}