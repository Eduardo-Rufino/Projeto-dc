using Godot;
using ProjetoDC.Enums;
using ProjetoDC.Scripts.Gameplay;
using ProjetoDC.Scripts.Systems.Results;

namespace ProjetoDC.Scripts.Systems.Training
{
    public class TrainingSystem
    {
        public TrainingResult Execute(DigimonInstance digimon, TrainingType type)
        {
            var result = new TrainingResult();

            if (digimon.Stamina < 10)
            {
                result.Success = false;
                result.Reason = "NOT_ENOUGH_STAMINA";
                return result;
            }

            result.Success = true;

            switch (type)
            {
                case TrainingType.HealthPoints:
                    result.HealthPointsGained = 10;
                    break;
                case TrainingType.Attack:
                    result.PhysicDamageGained = 1;
                    break;

                case TrainingType.Defense:
                    result.PhysicDefenseGained = 1;
                    break;

                case TrainingType.Speed:
                    result.SpeedGained = 1;
                    break;
                case TrainingType.SpecialAttack:
                    result.SpecialDamageGained = 1;
                    break;
                case TrainingType.SpecialDefense:
                    result.SpecialDefenseGained = 1;
                    break;
            }

            result.StaminaCost = 10;
            result.ExpGained = 5;

            return result;
        }
    }
}