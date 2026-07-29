using Godot;
using ProjetoDC.Enums;
using ProjetoDC.Scripts.Core.Results;
using ProjetoDC.Scripts.Gameplay;
using ProjetoDC.Scripts.Save;
using ProjetoDC.Scripts.Systems.Battle;
using ProjetoDC.Scripts.Systems.Center;
using ProjetoDC.Scripts.Systems.Clock;
using ProjetoDC.Scripts.Systems.Eggs;
using ProjetoDC.Scripts.Systems.Evolution;
using ProjetoDC.Scripts.Systems.Save;
using ProjetoDC.Scripts.Systems.Training;
using ProjetoDC.Scripts.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjetoDC.Scripts.Managers
{
    /// <summary>
    /// Gerencia o estado geral do jogo: salvar, inicializar sistemas (centro, treinos, ovos),
    /// selecionar digimons para batalha e controlar fluxos relacionados ao tempo.
    /// </summary>
    public partial class GameManager : Node
    {
        public static GameManager Instance { get; private set; }

        /// <summary>Digimon do jogador atualmente selecionado.</summary>
        public DigimonInstance PlayerDigimon { get; private set; }

        /// <summary>Digimon inimigo atual para batalhas.</summary>
        public DigimonInstance EnemyDigimon { get; private set; }
        public List<DigimonInstance> GeneratedEnemies { get; private set; } = new();

        public event Action DigimonListChanged;
        public event Action PlayerDigimonChanged;
        public event Action GameLoaded;

        /// <summary>Sistema de batalha ativo quando iniciado.</summary>
        public BattleSystem BattleSystem { get; private set; }

        public SaveData Save { get; private set; }
        public CenterService CenterService { get; private set; }
        public TrainingSystem TrainingSystem { get; private set; }
        public EggSystem EggSystem { get; set; }
        public EnemyGenerator EnemyGenerator { get; set; }
        public ClockSystem ClockSystem { get; private set; }
        public SaveSystem SaveSystem { get; private set; }

        public override void _Ready()
        {
            Instance = this;

            SaveSystem = new SaveSystem();

            if (SaveSystem.HasSave())
            {
                Save = SaveSystem.LoadGame();

                GD.Print($"Digimons carregados: {Save.Center.Digimons.Count}");

                foreach (var d in Save.Center.Digimons)
                {
                    GD.Print($"{d.BaseData?.Name} - Lv {d.Level}");
                }

                if (Save == null)
                {
                    GD.PrintErr("Falha ao carregar save.");
                    Save = new SaveData();
                }
            }
            else
            {
                Save = new SaveData();
            }

            CenterService = new CenterService(Save.Center);
            TrainingSystem = new TrainingSystem();
            EggSystem = new EggSystem();
            EnemyGenerator = new EnemyGenerator();
            ClockSystem = new ClockSystem(Save.World);

            ClockSystem.HourPassed += OnHourPassed;
            ClockSystem.DayPassed += OnDayPassed;

            GD.Print("GameManager inicializado!");

            ClockSystem.MinutePassed += OnMinutePassed;

            CallDeferred(nameof(InitializeGame));
        }

        private void OnFirstFrame()
        {
            GetTree().ProcessFrame -= OnFirstFrame;

            InitializeNewGame();
        }

        private void InitializeGame()
        {
            if (SaveSystem.HasSave())
            {
                LoadExistingGame();
            }
            else
            {
                InitializeNewGame();
            }
        }

        public void SaveGame()
        {
            SaveSystem.SaveGame(Save);
        }

        private void LoadExistingGame()
        {
            Save = SaveSystem.LoadGame();

            CenterService = new CenterService(Save.Center);

            ClockSystem = new ClockSystem(Save.World);

            ClockSystem.HourPassed += OnHourPassed;
            ClockSystem.DayPassed += OnDayPassed;
            ClockSystem.MinutePassed += OnMinutePassed;

            PlayerDigimon = CenterService.GetAllDigimons().FirstOrDefault();

            if (PlayerDigimon != null)
            {
                SetPlayerDigimon(PlayerDigimon);
            }

            GD.Print("Save carregado com sucesso!");
            GameLoaded?.Invoke();
        }

        public override void _Process(double delta)
        {
            ClockSystem?.Update(delta);
        }

        private void OnMinutePassed()
        {
            GD.Print($"{Save.World.CurrentDay} - {Save.World.CurrentHour:00}:{Save.World.CurrentMinute:00}");
        }

        private void OnHourPassed()
        {
            foreach (var digimon in Save.Center.Digimons)
            {
                digimon.AdvanceHour(Save.World);
            }

            EggSystem.AdvanceHour(CenterService);
        }

        private void OnDayPassed()
        {
            foreach (var digimon in Save.Center.Digimons)
            {
                digimon.AdvanceDay();
                digimon.TryBecomeSick();

                TryToEvolve(digimon);
            }

            EggSystem.AdvanceDay(CenterService);
            SaveGame();
        }

        /// <summary>
        /// Inicialização e log relacionado aos digimons iniciais no centro.
        /// </summary>
        private void InitializeStarterDigimons()
        {
            var db = DatabaseManager.Instance;

            if (db == null)
            {
                GD.PrintErr("DatabaseManager ainda não inicializado!");
                return;
            }

            GD.Print($"Centro criado com {Save.Center.Digimons.Count} Digimons.");
        }

        /// <summary>
        /// Seleciona o digimon do jogador a partir do Center pelo ID e reconstrói o sistema de batalha.
        /// </summary>
        public void SetPlayerDigimon(DigimonInstance digimon)
        {
            if (digimon == null)
            {
                GD.PrintErr("Digimon inválido.");
                return;
            }

            PlayerDigimon = digimon;

            GD.Print(
                $"PlayerDigimon definido: {PlayerDigimon.BaseData.Name} Hash: {PlayerDigimon.GetHashCode()}"
            );

            PlayerDigimonChanged?.Invoke();

            RebuildBattle();
        }

        /// <summary>
        /// Cria uma instância inimiga a partir do DB e reconstrói o sistema de batalha.
        /// </summary>
        public void SetEnemyDigimon(int id)
        {
            var data = DatabaseManager.Instance.GetDigimon(id);

            if (data == null)
            {
                GD.PrintErr($"Digimon {id} não encontrado.");
                return;
            }

            EnemyDigimon = new DigimonInstance(data);

            GD.Print($"EnemyDigimon: {EnemyDigimon.BaseData.Name}");

            RebuildBattle();
        }

        /// <summary>
        /// Reconstrói o objeto <see cref="BattleSystem"/> quando ambos players estiverem definidos.
        /// </summary>
        private void RebuildBattle()
        {
            if (PlayerDigimon == null || EnemyDigimon == null)
                return;

            BattleSystem = new BattleSystem(PlayerDigimon, EnemyDigimon);

            GD.Print("BattleSystem reconstruído.");
        }

        /// <summary>
        /// Dispara a tela de batalha e inicializa com o BattleSystem atual.
        /// </summary>
        public async void StartBattle()
        {
            if (BattleSystem == null)
            {
                GD.PrintErr("BattleSystem não inicializado!");
                return;
            }

            var battleScene = GD.Load<PackedScene>("res://Scenes/BattleScreen.tscn");
            var battleScreen = battleScene.Instantiate<BattleScreen>();

            GetTree().Root.AddChild(battleScreen);

            var controller = battleScreen.GetNode<BattleController>("BattleController");
            controller.Init(BattleSystem);
        }

        /// <summary>
        /// Avança o tempo global do jogo (dias) e aplica avanço nos digimons do Center.
        /// Também tenta evoluir o digimon do jogador quando apropriado.
        /// </summary>
        public void SkipDay()
        {
            Save.World.CurrentHour = 1;
            Save.World.CurrentMinute = 59;
        }

        public SystemResult FeedPlayer()
        {
            return FeedDigimon(PlayerDigimon);
        }

        public SystemResult FeedDigimon(DigimonInstance digimon)
        {
            if (Save.Center.Meat <= 0)
            {
                return SystemResult.Fail("Você não possui Comida.");
            }

            if (digimon.Hunger >= digimon.MaxHunger)
                return SystemResult.Fail("O Digimon não está com fome.");

            Save.Center.Meat--;

            digimon.Feed(10);

            return SystemResult.Ok("O Digimon foi alimentado.");
        }

        public void SetPlayerDigimonInstance(DigimonInstance digimon)
        {
            if (digimon == null)
            {
                GD.PrintErr("Tentativa de selecionar Digimon nulo.");
                return;
            }

            PlayerDigimon = digimon;

            GD.Print(
                $"PlayerDigimon definido: {PlayerDigimon.BaseData.Name} " +
                $"Hash: {PlayerDigimon.GetHashCode()}"
            );

            PlayerDigimonChanged?.Invoke();

            RebuildBattle();
        }

        /// <summary>
        /// Executa treino no <see cref="PlayerDigimon"/> usando o <see cref="TrainingSystem"/>.
        /// Em caso de sucesso aplica o resultado e tenta evolução.
        /// </summary>
        public SystemResult TrainPlayer(TrainingType type)
        {
            if (PlayerDigimon == null)
            {
                return SystemResult.Fail("Nenhum Digimon selecionado.");
            }

            if (PlayerDigimon.HealthState == HealthState.Sick)
            {
                return SystemResult.Fail("O Digimon está doente.");
            }

            var result = TrainingSystem.Execute(PlayerDigimon, type);

            if (!result.Success)
            {
                return SystemResult.Fail(result.Reason);
            }

            PlayerDigimon.ApplyTrainingResult(result);

            TryToEvolve(PlayerDigimon);

            return SystemResult.Ok();
        }

        /// <summary>
        /// Tenta evoluir o digimon passado utilizando a lógica do <see cref="EvolutionSystem"/>.
        /// </summary>
        public bool TryToEvolve(DigimonInstance digimon)
        {
            if (digimon == null)
                return false;

            return EvolutionSystem.TryToEvolve(digimon);
        }

        /// <summary>
        /// Inicialização de novo jogo: cria ovo inicial via <see cref="EggSystem"/>.
        /// </summary>
        public void InitializeNewGame()
        {
            GD.Print($"[DEBUG] Digimons no Center ANTES egg: {Save.Center.Digimons.Count}");

            EggSystem.CreateInitialEgg(CenterService);

            GD.Print($"[DEBUG] Digimons no Center DEPOIS egg: {Save.Center.Digimons.Count}");

            PlayerDigimon = CenterService.GetAllDigimons().FirstOrDefault();

            if (PlayerDigimon != null)
            {
                SetPlayerDigimon(PlayerDigimon);
                GD.Print($"Player inicial: {PlayerDigimon.BaseData.Name}");
            }

            SaveSystem.SaveGame(Save);
        }

        /// <summary>
        /// Inicializa batalha selecionando um jogador do Center e um inimigo fixo (temporário).
        /// </summary>
        public void InitializeBattle(DigimonInstance enemy)
        {
            EnemyDigimon = enemy;

            EnemyDigimon.RestoreHealth();

            RebuildBattle();
        }

        public void GenerateEnemyCandidates()
        {
            if (PlayerDigimon == null)
                return;

            PlayerDigimon.RestoreHealth();

            GeneratedEnemies = EnemyGenerator.GenerateEnemies(PlayerDigimon, 4);
        }

        public void ApplyBattleReward(BattleResult result)
        {
            if (PlayerDigimon == null && EnemyDigimon == null)
                return;

            if (result == BattleResult.EnemyWon)
            {
                GD.Print("Derrota! Sem recompensa");
                return;
            }

            var reward = BattleRewardCalculator.Calculate(PlayerDigimon, EnemyDigimon);

            GD.Print($"Vitória! +{reward.Experience} XP | +{reward.Bits} Bits");

            PlayerDigimon.GainExperience(reward.Experience);

            Save.Center.AddBits(reward.Bits);

            TryToEvolve(PlayerDigimon);
            
        }

        public SystemResult BuyMeat()
        {
            if (Save.Center.Bits < 20) {
                return SystemResult.Fail("Bits insuficientes.");
                GD.Print("sem dinheiro");
            }

            Save.Center.Bits -= 20;
            Save.Center.Meat++;

            return SystemResult.Ok();
        }

        public SystemResult BuyMedicine()
        {
            if (Save.Center.Bits < 100)
                return SystemResult.Fail("Bits insuficientes.");

            Save.Center.Bits -= 100;
            Save.Center.Medicine++;

            return SystemResult.Ok();
        }

        public SystemResult BuyEgg(int digimonId)
        {
            const int eggPrice = 500;

            if (Save.Center.Bits < eggPrice)
            {
                return SystemResult.Fail("Bits insuficientes.");
            }

            bool created = EggSystem.CreateEgg(
                digimonId,
                CenterService
            );

            if (!created)
            {
                return SystemResult.Fail("Não foi possível criar o ovo.");
            }

            Save.Center.Bits -= eggPrice;

            return SystemResult.Ok();
        }

        public SystemResult UseMedicinePlayer()
        {
            return UseMedicine(PlayerDigimon);
        }

        public SystemResult UseMedicine(DigimonInstance digimon)
        {
            if (Save.Center.Medicine <= 0)
            {
                return SystemResult.Fail("Você não possui remédios.");
            }

            if (digimon.HealthState != HealthState.Sick)
            {
                return SystemResult.Fail("O Digimon não está doente.");
            }

            Save.Center.Medicine--;

            digimon.Heal(); // ou digimon.HealthState = HealthState.Healthy;

            return SystemResult.Ok("O Digimon foi curado.");
        }

        public void AddDebugDigimon(int id)
        {
            var data = DatabaseManager.Instance.GetDigimon(id);

            if (data == null)
            {
                GD.PrintErr($"Digimon {id} não encontrado.");
                return;
            }

            var digimon = new DigimonInstance(data);

            CenterService.AddDigimon(digimon);

            GD.Print($"{data.Name} adicionado ao Center.");
        }

        public void EnsureCenterCanContinue()
        {
            if (Save.Center.Digimons.Count == 0 &&
                Save.Center.Eggs.Count == 0)
            {
                GD.Print("Centro vazio. Criando ovo inicial.");

                EggSystem.CreateInitialEgg(CenterService);

                SaveGame();
            }
        }

        public override void _Notification(int what)
        {
            if (what == NotificationWMCloseRequest)
            {
                SaveGame();
            }
        }
    }
}