using ProjetoDC.Scripts.Core.Results;

namespace ProjetoDC.Scripts.Systems.Results
{
    public class TrainingResult : SystemResult
    {
        public int HealthPointsGained { get; set; }

        public int PhysicDamageGained { get; set; }
        public int PhysicDefenseGained { get; set; }
        public int SpeedGained { get; set; }
        public int SpecialDamageGained { get; set; }
        public int SpecialDefenseGained { get; set; }

        public int StaminaCost { get; set; }
        public int ExpGained { get; set; }

    }
}
