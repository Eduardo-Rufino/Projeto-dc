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

        /// <summary>Digimon do jogador atualmente selecionado (fora de batalha: treino, alimentação, etc.).</summary>
        public DigimonInstance PlayerDigimon { get; private set; }

        /// <summary>Times da batalha 3x3 em andamento.</summary>
        public List<DigimonInstance> PlayerBattleTeam { get; private set; } = new();
        public List<DigimonInstance> EnemyBattleTeam { get; private set; } = new();

        public event Action DigimonListChanged;
        public event Action PlayerDigimonChanged;
        public event Action GameLoaded;
        public event Action<BattleResult> TeamBattleFinished;
        public event Action TeamBattleStarted;

        /// <summary>Estado puro da batalha 3x3 ativa (nulo fora de batalha).</summary>
        public BattleMatch BattleMatch { get; private set; }

        public SaveData Save { get; private set; }
        public CenterService CenterService { get; private set; }
        public TrainingSystem TrainingSystem { get; private set; }
        public EggSystem EggSystem { get; set; }
        public EnemyGenerator EnemyGenerator { get; set; }
        public ClockSystem ClockSystem { get; private set; }
        public SaveSystem SaveSystem { get; private set; }

        private bool _loadedExistingSave;

        public override void _Ready()
        {
            Instance = this;

            SaveSystem = new SaveSystem();

            if (SaveSystem.HasSave())
            {
                Save = SaveSystem.LoadGame();

                if (Save == null)
                {
                    GD.PrintErr("Falha ao carregar save.");
                    Save = new SaveData();
                }
                else
                {
                    _loadedExistingSave = true;

                    GD.Print($"Digimons carregados: {Save.Center.Digimons.Count}");

                    foreach (var d in Save.Center.Digimons)
                    {
                        GD.Print($"{d.BaseData?.Name} - Lv {d.Level}");
                    }
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
            ClockSystem.MinutePassed += OnMinutePassed;


            GD.Print("GameManager inicializado!");

            CallDeferred(nameof(InitializeGame));
        }

        private void OnFirstFrame()
        {
            GetTree().ProcessFrame -= OnFirstFrame;

            InitializeNewGame();
        }

        private void InitializeGame()
        {
            if (_loadedExistingSave)
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
            CenterService = new CenterService(Save.Center);

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
        /// Inicia uma batalha 3x3 em tempo real com o time escolhido pelo jogador (exatamente
        /// 3 Digimons, sem restrição de Role). Gera o time inimigo calibrado por esse time e
        /// abre a arena; a recompensa é aplicada quando a arena avisa que a batalha terminou.
        /// </summary>
        public void StartTeamBattle(List<DigimonInstance> playerTeam)
        {
            if (playerTeam == null || playerTeam.Count != 3)
            {
                GD.PrintErr("StartTeamBattle exige exatamente 3 Digimons.");
                return;
            }

            PlayerBattleTeam = playerTeam;

            EnemyBattleTeam = EnemyGenerator.GenerateEnemyTeam(PlayerBattleTeam, 3);

            if (EnemyBattleTeam.Count == 0)
            {
                GD.PrintErr("Não foi possível gerar o time inimigo.");
                return;
            }

            TeamBattleStarted?.Invoke();

            var arenaScene = GD.Load<PackedScene>("res://Scenes/Battle/BattleArena.tscn");
            var arena = arenaScene.Instantiate<BattleArena>();

            GetTree().Root.AddChild(arena);

            arena.Init(PlayerBattleTeam, EnemyBattleTeam);

            BattleMatch = arena.Match;

            arena.BattleFinished += result => OnTeamBattleFinished(result, arena);
        }

        private void OnTeamBattleFinished(BattleResult result, BattleArena arena)
        {
            ApplyTeamBattleReward(result);

            arena.QueueFree();

            BattleMatch = null;

            TeamBattleFinished?.Invoke(result);
        }

        /// <summary>
        /// Aplica o resultado de uma batalha 3x3: na derrota, todo o time perde Felicidade;
        /// na vitória, cada membro (sobrevivente ou não) recebe sua fração de XP e o bônus
        /// de Felicidade, e tenta evoluir; os Bits vão pro Center uma única vez.
        /// </summary>
        public void ApplyTeamBattleReward(BattleResult result)
        {
            if (PlayerBattleTeam.Count == 0)
                return;

            if (result == BattleResult.EnemyWon)
            {
                GD.Print("Derrota! Sem recompensa");

                foreach (var member in PlayerBattleTeam)
                    member.ChangeHappiness(-6);

                return;
            }

            var reward = BattleRewardCalculator.CalculateForTeam(PlayerBattleTeam, EnemyBattleTeam);

            GD.Print($"Vitória! +{reward.Experience} XP por membro | +{reward.Bits} Bits");

            foreach (var member in PlayerBattleTeam)
            {
                member.GainExperience(reward.Experience);
                member.ChangeHappiness(8);

                TryToEvolve(member);
            }

            Save.Center.AddBits(reward.Bits);
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

        public SystemResult BuyEgg()
        {
            const int eggPrice = 500;

            if (Save.Center.Bits < eggPrice)
            {
                return SystemResult.Fail("Bits insuficientes.");
            }

            var babyDigimons = DatabaseManager.Instance.GetAllDigimons()
                .Where(d => d.Stage == DigimonStage.Baby)
                .ToList();

            if (babyDigimons.Count == 0)
            {
                return SystemResult.Fail("Nenhum Digimon Baby disponível.");
            }

            var chosen = babyDigimons[GD.RandRange(0, babyDigimons.Count - 1)];

            // Confere a capacidade com uma instância descartável, só para checar o custo pelo estágio.
            var previewDigimon = new DigimonInstance(chosen);

            if (!CenterService.CanAddDigimon(previewDigimon))
            {
                return SystemResult.Fail("Não há capacidade suficiente no Center.");
            }

            bool created = EggSystem.CreateEgg(
                chosen.Id,
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