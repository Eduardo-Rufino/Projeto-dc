using Godot;
using ProjetoDC.Enums;
using ProjetoDC.Scripts.Gameplay;
using ProjetoDC.Scripts.Save;
using ProjetoDC.Scripts.Systems.Battle;
using ProjetoDC.Scripts.Systems.Center;
using ProjetoDC.Scripts.Systems.Eggs;
using ProjetoDC.Scripts.Systems.Evolution;
using ProjetoDC.Scripts.Systems.Results;
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

        /// <summary>Sistema de batalha ativo quando iniciado.</summary>
        public BattleSystem BattleSystem { get; private set; }

        public SaveData Save { get; private set; }
        public CenterService CenterService { get; private set; }
        public TrainingSystem TrainingSystem { get; private set; }
        public EggSystem EggSystem { get; set; }
        public EnemyGenerator EnemyGenerator { get; set; }

        public override void _Ready()
        {
            Instance = this;

            Save = new SaveData();
            CenterService = new CenterService(Save.Center);
            TrainingSystem = new TrainingSystem();
            EggSystem = new EggSystem();
            EnemyGenerator = new EnemyGenerator();

            GD.Print("GameManager inicializado!");

            // esperar outros autoloads
            CallDeferred(nameof(InitializeNewGame));
        }

        private void OnFirstFrame()
        {
            GetTree().ProcessFrame -= OnFirstFrame;

            InitializeNewGame();
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
        public void SetPlayerDigimon(int id)
        {
            var digimon = CenterService.GetDigimonById(id);

            if (digimon == null)
            {
                GD.PrintErr($"Digimon {id} não encontrado no Center.");
                return;
            }

            PlayerDigimon = digimon;

            GD.Print($"PlayerDigimon: {PlayerDigimon.BaseData.Name}");

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
        public void AdvanceDay(int days = 1)
        {
            Save.World.AdvanceDay(days);

            foreach (var digimon in Save.Center.Digimons)
            {
                digimon.AdvanceDays(days);
                TryToEvolve(PlayerDigimon);
            }            
        }

        /// <summary>
        /// Executa treino no <see cref="PlayerDigimon"/> usando o <see cref="TrainingSystem"/>.
        /// Em caso de sucesso aplica o resultado e tenta evolução.
        /// </summary>
        public TrainingResult TrainPlayer(TrainingType type)
        {
            if (PlayerDigimon == null)
            {
                return new TrainingResult
                {
                    Success = false,
                    Reason = "PLAYER_DIGIMON_NULL"
                };
            }

            var result = TrainingSystem.Execute(PlayerDigimon, type);

            if (!result.Success)
                return result;

            PlayerDigimon.ApplyTrainingResult(result);

            TryToEvolve(PlayerDigimon);

            return result;
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
                SetPlayerDigimon(PlayerDigimon.BaseData.Id);
                GD.Print($"Player inicial: {PlayerDigimon.BaseData.Name}");
            }
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
    }
}