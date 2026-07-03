using Godot;
using ProjetoDC.Enums;
using ProjetoDC.Scripts.Gameplay;
using ProjetoDC.Scripts.Managers;
using ProjetoDC.Scripts.Models.World;
using ProjetoDC.Scripts.Save;
using ProjetoDC.Scripts.Systems.Battle;
using ProjetoDC.Scripts.Systems.Center;
using ProjetoDC.Scripts.Systems.Eggs;
using ProjetoDC.Scripts.Systems.Evolution;
using ProjetoDC.Scripts.Systems.Results;
using ProjetoDC.Scripts.Systems.Training;
using System.Linq;

namespace ProjetoDC.Scripts.Managers
{
    public partial class GameManager : Node
    {
        public static GameManager Instance { get; private set; }

        public DigimonInstance PlayerDigimon { get; private set; }
        public DigimonInstance EnemyDigimon { get; private set; }

        public BattleSystem BattleSystem { get; private set; }

        public SaveData Save { get; private set; }
        public CenterService CenterService { get; private set; }
        public TrainingSystem TrainingSystem { get; private set; }
        public EggSystem EggSystem { get; set; }

        public WorldState World => Save.World;
        public CenterState Center => Save.Center;

        public override void _Ready()
        {
            Instance = this;

            Save = new SaveData();
            CenterService = new CenterService(Save.Center);
            TrainingSystem = new TrainingSystem();
            EggSystem = new EggSystem();

            GD.Print("GameManager inicializado!");

            // esperar outros autoloads
            GetTree().ProcessFrame += OnFirstFrame;
        }

        private void OnFirstFrame()
        {
            GetTree().ProcessFrame -= OnFirstFrame;

            InitializeNewGame();
        }

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

        private void RebuildBattle()
        {
            if (PlayerDigimon == null || EnemyDigimon == null)
                return;

            BattleSystem = new BattleSystem(PlayerDigimon, EnemyDigimon);

            GD.Print("BattleSystem reconstruído.");
        }

        public void StartBattle(int playerId, int enemyId)
        {
            SetPlayerDigimon(playerId);
            SetEnemyDigimon(enemyId);
        }

        public void AdvanceDay(int days = 1)
        {
            Save.World.AdvanceDay(days);

            foreach (var digimon in Save.Center.Digimons)
            {
                digimon.AdvanceDays(days);
                TryToEvolve(PlayerDigimon);
            }            
        }

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

        public bool TryToEvolve(DigimonInstance digimon)
        {
            if (digimon == null)
                return false;

            return EvolutionSystem.TryToEvolve(digimon);
        }

        public void InitializeNewGame()
        {
            EggSystem.CreateInitialEgg(CenterService);
        }

        public void InitializeBattle()
        {
            var player = CenterService.GetAllDigimons().FirstOrDefault();

            if (player == null)
                return;

            PlayerDigimon = player;

            SetEnemyDigimon(2); // por enquanto pode continuar sendo fixo

            RebuildBattle();
        }
    }
}