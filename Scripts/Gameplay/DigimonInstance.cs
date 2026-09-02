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

        /// <summary>Apelido opcional definido pelo jogador na HUD (clicando no nome do Digimon
        /// selecionado). Só afeta exibição - toda lógica interna (evolução, save, banco de
        /// dados) continua usando BaseData/Id, nunca este campo. Use DisplayName pra mostrar
        /// na UI.</summary>
        public string Nickname { get; set; }

        /// <summary>Nome mostrado pra o jogador: o apelido, se definido, senão o nome da espécie.</summary>
        public string DisplayName =>
            string.IsNullOrWhiteSpace(Nickname) ? BaseData.Name : Nickname;

        public int Level { get; set; } = 1;
        public int MaxHealthPoints { get; set; }
        public int CurrentHealthPoints { get; set; }
        public int Experience { get; set; }
        public int ExperienceToNextLevel { get; set; }
        public int AgeInDays { get; set; }
        public int CapacityCost { get; set; }

        /// <summary>Última posição conhecida no mundo do Center (atualizada continuamente
        /// por DigimonWorld._Process) - restaurada em Center.SpawnDigimons ao carregar o
        /// save, em vez de sempre nascer nos SpawnPoints fixos. (0,0) (o default) significa
        /// "ainda não tem posição salva" - mesma convenção já usada em EggData.</summary>
        public float PositionX { get; set; }
        public float PositionY { get; set; }

        public int Hunger { get; set; }

        public int MaxHunger => 100;

        public int Happiness { get; set; } = 50;
        public int Discipline { get; set; } = 50;

        public const int MaxHappiness = 100;
        public const int MaxDiscipline = 100;

        public void ChangeHappiness(int amount)
        {
            Happiness = Math.Clamp(Happiness + amount, 0, MaxHappiness);
        }

        public void ChangeDiscipline(int amount)
        {
            Discipline = Math.Clamp(Discipline + amount, 0, MaxDiscipline);
        }

        public BaseStats CurrentStats { get; set; }
        private HealthState _healthState = HealthState.Healthy;
        public DigimonActivity Activity { get; set; } = DigimonActivity.Idle;
        public int MaxStamina { get; set; } = 100;
        public int Stamina { get; set; } = 100;

        /// <summary>True depois que o jogador escolhe "manter sem evoluir" no aviso de
        /// evolução bloqueada por falta de capacidade (ver GameManager.EvolutionBlockedByCapacity)
        /// - evita reabrir o mesmo aviso a cada tentativa (treino, dia, batalha) enquanto a
        /// capacidade continuar insuficiente. A tentativa de evolução em si não para: quando
        /// a capacidade for suficiente, evolui normalmente e essa flag é zerada de novo.</summary>
        public bool EvolutionCapacityWarningDismissed { get; set; }

        /// <summary>Quantas horas seguidas (na sessão de sono atual) o Digimon já dormiu
        /// fora do Dormitório - zera ao acordar ou ao passar uma hora dormindo dentro do
        /// Dormitório. Usado por DigimonWorld.OnDormitoryHourPassed pra aplicar a penalidade
        /// crescente de Felicidade.</summary>
        public int HoursSleptOutsideDormitory { get; set; }

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
                SupportType = data.SupportType,

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
        /// <summary>Público pra EggSystem poder calcular o custo de capacidade de um ovo
        /// (o mesmo custo do Digimon Baby que vai nascer dele) sem duplicar a tabela.</summary>
        public static int GetCapacityCostForStage(DigimonStage stage) => stage switch
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
        /// Recupera uma porcentagem do HP máximo (regeneração passiva no Center).
        /// </summary>
        public void RegenerateHealth(float percentage)
        {
            if (CurrentHealthPoints >= MaxHealthPoints)
                return;

            int amount = Math.Max(1, (int)(MaxHealthPoints * percentage));

            CurrentHealthPoints = Math.Min(MaxHealthPoints, CurrentHealthPoints + amount);
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

            ChangeHappiness(10);
            ChangeDiscipline(5);
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

        // Fração de quanto um stat negligenciado é puxado em direção à ficha da nova espécie
        // ao evoluir - ex.: um Digimon Special que evoluiu treinando quase só físico não
        // fica travado pra sempre com um SpecialDamage residual da forma anterior. Nunca
        // reduz um stat que o jogador já treinou acima do valor da espécie, só complementa
        // o que ficou pra trás (ver BlendTowardSpecies).
        private const float EvolutionSpeciesBlendWeight = 0.4f;

        /// <summary>
        /// Atualiza a forma base (evolução) do Digimon e multiplica os stats existentes
        /// pelo multiplicador informado, atualizando HP para o máximo.
        /// </summary>
        public void Evolve(DigimonData newForm, float multiplier)
        {
            var oldForm = BaseData;

            BaseData = newForm;
            CapacityCost = GetCapacityCostForStage(newForm.Stage);

            var species = newForm.BaseStats;

            CurrentStats.PhysicalDamage = BlendTowardSpecies(CurrentStats.PhysicalDamage, multiplier, species.PhysicalDamage);
            CurrentStats.PhysicalDefense = BlendTowardSpecies(CurrentStats.PhysicalDefense, multiplier, species.PhysicalDefense);
            CurrentStats.SpecialDamage = BlendTowardSpecies(CurrentStats.SpecialDamage, multiplier, species.SpecialDamage);
            CurrentStats.SpecialDefense = BlendTowardSpecies(CurrentStats.SpecialDefense, multiplier, species.SpecialDefense);
            CurrentStats.Speed = BlendTowardSpecies(CurrentStats.Speed, multiplier, species.Speed);
            CurrentStats.HealthPoints = BlendTowardSpecies(CurrentStats.HealthPoints, multiplier, species.HealthPoints);

            MaxHealthPoints = CurrentStats.HealthPoints;
            CurrentHealthPoints = MaxHealthPoints;

            GD.Print($"{newForm.Name} evoluiu! Stats atualizados!");

            Evolved?.Invoke(oldForm, newForm);
        }

        /// <summary>
        /// Multiplica o stat atual pelo multiplicador da evolução (recompensa o treino
        /// acumulado) e, se isso ainda ficar abaixo do valor "de ficha" da nova espécie,
        /// puxa uma fração dele em direção a esse valor - nunca reduz o que já foi
        /// multiplicado, só corrige quem ficou pra trás (nunca pune quem já treinou acima
        /// da média da espécie).
        /// </summary>
        private static int BlendTowardSpecies(int currentValue, float multiplier, int speciesValue)
        {
            int multiplied = (int)(currentValue * multiplier);

            float blended = Mathf.Lerp(multiplied, speciesValue, EvolutionSpeciesBlendWeight);

            return Math.Max(multiplied, (int)blended);
        }

        /// <summary>Disparado quando o Digimon evolui, com a forma antiga e a nova. Usado
        /// pelo visual (DigimonWorld) pra tocar a animação de evolução (alterna entre os
        /// sprites das duas formas antes de assentar na nova).</summary>
        public event Action<DigimonData, DigimonData> Evolved;

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

        public void StartSleeping()
        {
            SetActivity(DigimonActivity.Sleeping);
        }

        public void WakeUp()
        {
            if (Activity == DigimonActivity.Sleeping)
                SetActivity(DigimonActivity.Idle);
        }

        public void StartTraining()
        {
            SetActivity(DigimonActivity.Training);
        }

        public void StopTraining()
        {
            if (Activity == DigimonActivity.Training)
                SetActivity(DigimonActivity.Idle);
        }

        public void StartEating()
        {
            if (Activity == DigimonActivity.Sleeping)
                return;

            SetActivity(DigimonActivity.Eating);
        }

        public void StopEating()
        {
            if (Activity == DigimonActivity.Eating)
                SetActivity(DigimonActivity.Idle);
        }

        // Janela de sono, hoje sincronizada pro Center inteiro (sem Digimons diurnos/
        // noturnos ainda - cada um vai ter sua própria janela quando esse sistema existir de
        // verdade). Passa da meia-noite (22h de um dia até 6h do dia seguinte), por isso o
        // teste em IsSleepHour usa "ou" em vez de um intervalo comum início<fim.
        public const int SleepStartHour = 22;
        public const int SleepEndHour = 6;

        public static bool IsSleepHour(int hour) =>
            hour >= SleepStartHour || hour < SleepEndHour;

        public void AdvanceHour(WorldState world)
        {
            ConsumeHunger();

            GD.Print(
                $"HASH GAME {BaseData.Name}: {GetHashCode()}"
            );

            if (IsSleepHour(world.CurrentHour))
            {
                StartSleeping();
                RecoverStamina(5);
            }
            else
            {
                if (Activity == DigimonActivity.Sleeping)
                    WakeUp();

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

            if (Hunger <= 20)
                ChangeHappiness(-1);
        }

        public bool IsHungry()
        {
            return WantsFood();
        }

        public void Feed(int amount)
        {
            Hunger = Math.Min(MaxHunger, Hunger + amount);

            if (Hunger > MaxHunger)
                Hunger = MaxHunger;

            ChangeHappiness(5);
        }

        public bool WantsFood()
        {
            return Hunger <= 80;
        }

        public bool IsFull()
        {
            return Hunger >= 100;
        }

        public bool IsStarving()
        {
            return Hunger <= 30;
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

            ChangeDiscipline(5);
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

            digimon.Heal();

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

        // Chance de ficar doente a cada mordida de comida estragada (ver
        // DigimonWorld.ProcessEating) - separado da chance diária normal
        // (CalculateSicknessRisk/TryBecomeSick), que é sobre fome/idade, não sobre ter
        // comido algo ruim agora mesmo.
        private const int SpoiledFoodSicknessChance = 15;

        /// <summary>Rolagem de chance extra de ficar doente por comer comida estragada -
        /// chamada uma vez por mordida (Food.Consume), então comer bastante de uma comida
        /// estragada acumula várias chances, não só uma.</summary>
        public void TryBecomeSickFromSpoiledFood()
        {
            if (HealthState == HealthState.Sick)
                return;

            int roll = _random.Next(1, 101);

            if (roll <= SpoiledFoodSicknessChance)
            {
                SetHealthState(HealthState.Sick);

                GD.Print($"{BaseData.Name} ficou doente por comer comida estragada.");
            }
        }

        public int CalculateSicknessRisk()
        {
            int risk = 0;

            if (Hunger <= 80)
                risk += 2;

            if (Hunger <= 60)
                risk += 3;

            if (Hunger <= 40)
                risk += 5;

            if (Hunger <= 20)
                risk += 10;

            if (Hunger <= 5)
                risk += 20;

            if (AgeInDays > 20)
                risk += 2;

            if (AgeInDays > 40)
                risk += 3;

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

        /// <summary>
        /// Indica se o jogador pode arrastar o Digimon manualmente.
        /// Diferente de <see cref="CanMove"/>: dormindo ainda pode ser arrastado, doente não.
        /// </summary>
        public bool CanBeDragged()
        {
            return HealthState != HealthState.Sick;
        }

        public HealthState HealthState
        {
            get => _healthState;
            private set
            {
                if (_healthState == value)
                    return;

                _healthState = value;

                if (value == HealthState.Sick)
                    ChangeHappiness(-8);

                HealthStateChanged?.Invoke(value);
            }
        }

        public event Action<HealthState>? HealthStateChanged;
    }
}
