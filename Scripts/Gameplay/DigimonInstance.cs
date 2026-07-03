
using Godot;
using ProjetoDC.Enums;
using ProjetoDC.Scripts.Data;
using ProjetoDC.Scripts.Systems.Results;

namespace ProjetoDC.Scripts.Gameplay
{
    public class DigimonInstance
    {
        public DigimonData BaseData { get; private set; }

        public int Level { get; set; } = 1;
        public int MaxHealthPoints { get; set; }
        public int CurrentHealthPoints { get; set; }
        public int Experience { get; set; }
        public int ExperienceToNextLevel { get; set; }
        public int AgeInDays { get; private set; }
        public int CapacityCost { get; private set; }

        public BaseStats CurrentStats { get; private set; }
        public int Stamina { get; internal set; } = 1000;

        private static int GetCapacityCostForStage(DigimonStage stage) => stage switch
        {
            DigimonStage.Baby => 1,
            DigimonStage.InTraining => 2,
            DigimonStage.Rookie => 3,
            DigimonStage.Champion => 5,
            DigimonStage.Ultimate => 7,
            DigimonStage.Mega => 10,
            DigimonStage.MegaPlus => 12,
            DigimonStage.Special => 15,
            _ => 3,
        };

        public DigimonInstance(DigimonData baseData)
        {
            AgeInDays = 0;

            BaseData = baseData;
            CapacityCost = GetCapacityCostForStage(baseData.Stage);

            //inicialização básica
            CurrentStats = new BaseStats
            {
                HealthPoints = baseData.BaseStats.HealthPoints,
                PhysicalDamage = baseData.BaseStats.PhysicalDamage,
                PhysicalDefense = baseData.BaseStats.PhysicalDefense,
                SpecialDamage = baseData.BaseStats.SpecialDamage,
                SpecialDefense = baseData.BaseStats.SpecialDefense,
                Speed = baseData.BaseStats.Speed

            };

            MaxHealthPoints = CurrentStats.HealthPoints;
            CurrentHealthPoints = CurrentStats.HealthPoints;

            Experience = 0;
            ExperienceToNextLevel = 100;

        }

        public void TakeDamage(int amount)
        {
            CurrentHealthPoints -= amount;

            if (CurrentHealthPoints < 0)
                CurrentHealthPoints = 0;
        }

        public bool IsDead()
        {
            return CurrentHealthPoints <= 0;
        }

        public void ApplyTrainingResult(TrainingResult result)
        {
            CurrentStats.PhysicalDamage += result.PhysicDamageGained;
            CurrentStats.PhysicalDefense += result.PhysicDefenseGained;
            CurrentStats.Speed += result.SpeedGained;

            Stamina -= result.StaminaCost;
            GainExperience(result.ExpGained);
        }

        public void GainExperience(int amount)
        {
            Experience += amount;

            CheckLevelup();
        }       

        private void CheckLevelup()
        {
            while (Experience >= ExperienceToNextLevel)
            {
                Experience -= ExperienceToNextLevel;
                LevelUp();
            }
        }

        private void LevelUp()
        {
            Level++;

            CurrentStats.PhysicalDamage += 2;
            CurrentStats.PhysicalDefense += 2;
            CurrentStats.Speed += 1;
            CurrentStats.HealthPoints += 5;

            MaxHealthPoints = CurrentStats.HealthPoints;
            CurrentHealthPoints = MaxHealthPoints;

            ExperienceToNextLevel = (int)(ExperienceToNextLevel * 1.2f);

            GD.Print($"{BaseData.Name} subiu para o nível {Level}");
        }

        public void Evolve(DigimonData newForm, float multiplier)
        {
            BaseData = newForm;
            CurrentStats.PhysicalDamage = (int)(CurrentStats.PhysicalDamage * multiplier);
            CurrentStats.PhysicalDefense = (int)(CurrentStats.PhysicalDefense * multiplier);
            CurrentStats.SpecialDamage = (int)(CurrentStats.SpecialDamage * multiplier);
            CurrentStats.SpecialDefense = (int)(CurrentStats.SpecialDefense * multiplier);
            CurrentStats.Speed = (int)(CurrentStats.Speed * multiplier);
            CurrentStats.HealthPoints = (int)(CurrentStats.HealthPoints * multiplier);

            MaxHealthPoints = CurrentStats.HealthPoints;
            CurrentHealthPoints = MaxHealthPoints;

            GD.Print($"{newForm.Name} evoluiu! Stats atualizados!");
        }

        public void AdvanceDays(int days = 1)
        {
            AgeInDays += days;
        }
    }
}
