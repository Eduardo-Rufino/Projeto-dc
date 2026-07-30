using Godot;
using ProjetoDC.Enums;
using ProjetoDC.Scripts.Core.Results;
using ProjetoDC.Scripts.Data;
using ProjetoDC.Scripts.Managers;
using ProjetoDC.Scripts.Models.World;
using ProjetoDC.Scripts.Save;
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
        public DigimonData BaseData { get; set; }

        public int Level { get; set; } = 1;
        public int MaxHealthPoints { get; set; }
        public int CurrentHealthPoints { get; set; }
        public int Experience { get; set; }
        public int ExperienceToNextLevel { get; set; }
        public int AgeInDays { get; set; }
        public int CapacityCost { get; set; }

        public int Hunger { get; set; }

        public int MaxHunger => 100;

        public BaseStats CurrentStats { get; set; }
        public HealthState HealthState { get; set; } = HealthState.Healthy;
        public DigimonActivity Activity { get; set; } = DigimonActivity.Idle;
        public int MaxStamina { get; set; } = 100;
        public int Stamina { get; set; } = 100;

        private static readonly Random _random = new();

        public event Action<DigimonActivity>? ActivityChanged;


        public DigimonInstance()
        {
        }

        /// <summary>
        /// Construtor que inicializa a instância com os valores base do <see cref="DigimonData"/>.
        /// Define stats atuais igual aos base e prepara HP, experiência e custo de capacidade.
        /// </summary>
        public DigimonInstance(DigimonData baseData)
        {
            AgeInDays = 0;

            BaseData = CloneBaseData(baseData);
            CapacityCost = GetCapacityCostForStage(baseData.Stage);

            Hunger = MaxHunger;

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

        private DigimonData CloneBaseData(DigimonData data)
        {
            return new DigimonData
            {
                Id = data.Id,
                Code = data.Code,
                Name = data.Name,
                Stage = data.Stage,
                Role = data.Role,
                Attribute = data.Attribute,
                Element = data.Element,
                EggType = data.EggType,
                AttackType = data.AttackType,

                BaseStats = new BaseStats
                {
                    HealthPoints = data.BaseStats.HealthPoints,
                    PhysicalDamage = data.BaseStats.PhysicalDamage,
                    PhysicalDefense = data.BaseStats.PhysicalDefense,
                    SpecialDamage = data.BaseStats.SpecialDamage,
                    SpecialDefense = data.BaseStats.SpecialDefense,
                    Speed = data.BaseStats.Speed
                }
            };
        }
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

        public void ConsumeStamina(int amount)
        {
            Stamina -= amount;

            if (Stamina < 0)
                Stamina = 0;
        }

        public void RecoverStamina(int amount)
        {
            Stamina += amount;

            if (Stamina > MaxStamina)
                Stamina = MaxStamina;
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

            ConsumeStamina(result.StaminaCost);
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

        private void SetActivity(DigimonActivity activity)
        {
            GD.Print($"{BaseData.Name}: {Activity} -> {activity}");

            if (Activity == activity)
                return;

            Activity = activity;

            ActivityChanged?.Invoke(activity);
        }

        public void AdvanceHour(WorldState world)
        {
            ConsumeHunger();

            GD.Print(
                $"HASH GAME {BaseData.Name}: {GetHashCode()}"
            );

            if (world.CurrentHour >= 1 && world.CurrentHour < 2)
            {
                SetActivity(DigimonActivity.Sleeping);
                RecoverStamina(5);
            }
            else
            {
                if (Activity == DigimonActivity.Sleeping)
                    SetActivity(DigimonActivity.Idle);

                RecoverStamina(2);
            }


            GD.Print(
                $"{BaseData.Name} passou uma hora. " +
                $"Fome: {Hunger} | " +
                $"Stamina: {Stamina}/{MaxStamina}" +
                $"Atividade: {Activity}"
            );
        }

        public void AdvanceDay()
        {
            AgeInDays++;

            GD.Print($"{BaseData.Name} envelheceu. Idade: {AgeInDays} dias");
        }

        private void ConsumeHunger()
        {
            Hunger -= 1;

            if (Hunger < 0)
                Hunger = 0;
        }

        public bool IsHungry()
        {
            return Hunger <= MaxHunger;
        }

        public void Feed(int amount)
        {
            Hunger = Math.Min(MaxHunger, Hunger + amount);

            if (Hunger > MaxHunger)
                Hunger = MaxHunger;
        }

        public void RestoreHealth()
        {
            CurrentHealthPoints = MaxHealthPoints;
        }

        public void SetHealthState(HealthState state)
        {
            HealthState = state;
        }

        public void BecomeSick()
        {
            if (HealthState != HealthState.Healthy)
                return;

            HealthState = HealthState.Sick;
        }

        public void Heal()
        {
            HealthState = HealthState.Healthy;
        }

        public SystemResult UseMedicine(DigimonInstance digimon)
        {
            if (GameManager.Instance.Save.Center.Medicine <= 0)
            {
                return SystemResult.Fail("Você não possui remédios.");
            }

            if (digimon.HealthState != HealthState.Sick)
            {
                return SystemResult.Fail("O Digimon não está doente.");
            }

            GameManager.Instance.Save.Center.Medicine--;

            digimon.HealthState = HealthState.Healthy;

            return SystemResult.Ok("O Digimon foi curado.");
        }

        public void TryBecomeSick()
        {
            if (HealthState == HealthState.Sick)
                return;

            int chance = CalculateSicknessRisk();

            int roll = _random.Next(1, 101);

            if (roll <= chance)
            {
                SetHealthState(HealthState.Sick);

                GD.Print($"{BaseData.Name} ficou doente.");
            }

            GD.Print(
                $"Idade={AgeInDays} | " +
                $"Fome={Hunger} | " +
                $"Chance={chance} | " +
                $"Rolagem={roll}"
);
        }

        public int CalculateSicknessRisk()
        {
            int risk = 0;

            if (Hunger <= 70)
                risk += 5;

            if (Hunger <= 50)
                risk += 10;

            if (Hunger <= 30)
                risk += 15;

            if (Hunger <= 10)
                risk += 20;

            if (AgeInDays > 20)
                risk += 5;

            if (AgeInDays > 40)
                risk += 5;

            return risk;
        }

        public bool CanMove()
        {
            if (HealthState == HealthState.Sick)
                return false;

            if (Activity == DigimonActivity.Sleeping)
                return false;

            return true;
        }
    }
}
