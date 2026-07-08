using Godot;
using ProjetoDC.Enums;
using ProjetoDC.Scripts.Data;
using ProjetoDC.Scripts.Systems.Results;
using System;

namespace ProjetoDC.Scripts.Gameplay
{
    /// <summary>
    /// Representa uma instância de Digimon em jogo com seus stats atuais, HP, nível e experiência.
    /// Contém lógica de ganho de experiência, evolução, aplicar resultados de treino e avanço de dias.
    /// </summary>
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
        public int Stamina { get; internal set; } = 10000;

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

        /// <summary>
        /// Construtor que inicializa a instância com os valores base do <see cref="DigimonData"/>.
        /// Define stats atuais igual aos base e prepara HP, experiência e custo de capacidade.
        /// </summary>
        public DigimonInstance(DigimonData baseData)
        {
            AgeInDays = 0;

            BaseData = baseData;
            CapacityCost = GetCapacityCostForStage(baseData.Stage);

            if (baseData == null)
                throw new Exception("DigimonData não encontrado no Construtor");

            // inicialização básica dos stats
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

            if (baseData == null)
                throw new Exception("DigimonData não encontrado no DB");
        }

        /// <summary>
        /// Subtrai HP do Digimon e garante que não fique abaixo de zero.
        /// </summary>
        public void TakeDamage(int amount)
        {
            CurrentHealthPoints -= amount;

            if (CurrentHealthPoints < 0)
                CurrentHealthPoints = 0;
        }

        /// <summary>
        /// Retorna true se o Digimon estiver sem HP (morto).
        /// </summary>
        public bool IsDead()
        {
            return CurrentHealthPoints <= 0;
        }

        /// <summary>
        /// Aplica os ganhos resultantes de um treino:
        /// atualiza stats, HP, reduz stamina e aplica experiência.
        /// </summary>
        public void ApplyTrainingResult(TrainingResult result)
        {
            CurrentStats.PhysicalDamage += result.PhysicDamageGained;
            CurrentStats.PhysicalDefense += result.PhysicDefenseGained;
            CurrentStats.Speed += result.SpeedGained;
            CurrentStats.SpecialDamage += result.SpecialDamageGained;
            CurrentStats.SpecialDefense += result.SpecialDefenseGained;

            if (result.HealthPointsGained != 0)
            {
                CurrentStats.HealthPoints += result.HealthPointsGained;
                MaxHealthPoints = CurrentStats.HealthPoints;
                CurrentHealthPoints = Math.Min(CurrentHealthPoints + result.HealthPointsGained, MaxHealthPoints);
            }

            Stamina -= result.StaminaCost;
            GainExperience(result.ExpGained);
        }

        /// <summary>
        /// Adiciona experiência e verifica se houve level up.
        /// </summary>
        public void GainExperience(int amount)
        {
            Experience += amount;

            CheckLevelup();
        }       

        /// <summary>
        /// Loop que realiza level up enquanto houver experiência suficiente.
        /// </summary>
        private void CheckLevelup()
        {
            while (Experience >= ExperienceToNextLevel)
            {
                Experience -= ExperienceToNextLevel;
                LevelUp();
            }
        }

        /// <summary>
        /// Incrementa o nível e aplica ganhos fixos de stats, ajustando HP e próxima meta de XP.
        /// </summary>
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

        /// <summary>
        /// Atualiza a forma base (evolução) do Digimon e multiplica os stats existentes
        /// pelo multiplicador informado, atualizando HP para o máximo.
        /// </summary>
        public void Evolve(DigimonData newForm, float multiplier)
        {
            BaseData = newForm;
            CapacityCost = GetCapacityCostForStage(newForm.Stage);
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

        /// <summary>
        /// Avança o contador de idade em dias para o Digimon.
        /// </summary>
        public void AdvanceDays(int days = 1)
        {
            AgeInDays += days;
        }

        public void RestoreHealth()
        {
            CurrentHealthPoints = MaxHealthPoints;
        }
    }
}
