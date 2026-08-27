using Godot;
using ProjetoDC.Enums;
using ProjetoDC.Scripts.Gameplay;
using ProjetoDC.Scripts.Systems.Results;

namespace ProjetoDC.Scripts.Systems.Training
{
    public class TrainingSystem
    {
        private readonly RandomNumberGenerator _rng = new();

        public TrainingResult Execute(
            DigimonInstance digimon,
            TrainingType type)
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
                    result.HealthPointsGained = 10 * trainingResult;
                    break;

                case TrainingType.Attack:
                    result.PhysicDamageGained = trainingResult;
                    break;

                case TrainingType.Defense:
                    result.PhysicDefenseGained = trainingResult;
                    break;

                case TrainingType.Speed:
                    result.SpeedGained = trainingResult;
                    break;

                case TrainingType.SpecialAttack:
                    result.SpecialDamageGained = trainingResult;
                    break;

                case TrainingType.SpecialDefense:
                    result.SpecialDefenseGained = trainingResult;
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