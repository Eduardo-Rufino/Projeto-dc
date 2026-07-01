using Godot;
using ProjetoDC.Enums;
using ProjetoDC.Scripts.Gameplay;
using ProjetoDC.Scripts.Managers;
using ProjetoDC.Scripts.Models.World;
using ProjetoDC.Scripts.Systems;
using ProjetoDC.Scripts.Systems.Center;
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

        public WorldState World { get; private set; }
        public CenterState Center { get; private set; }
        public CenterService CenterService { get; private set; }
        public TrainingSystem TrainingSystem { get; private set; }

        public override void _Ready()
        {
            Instance = this;

            World = new WorldState();
            Center = new CenterState();
            CenterService = new CenterService(Center);
            TrainingSystem = new TrainingSystem();

            // Espera DatabaseManager terminar de carregar
            CallDeferred(nameof(InitializeStarterDigimons));

            GD.Print("GameManager inicializado!");
        }

        private void InitializeStarterDigimons()
        {
            var db = DatabaseManager.Instance;

            if (db == null)
            {
                GD.PrintErr("DatabaseManager ainda não inicializado!");
                return;
            }

            AddStarterDigimon(1);
            AddStarterDigimon(2);
            AddStarterDigimon(3);

            GD.Print($"Centro criado com {Center.Digimons.Count} Digimons.");
        }

        private void AddStarterDigimon(int id)
        {
            var data = DatabaseManager.Instance.GetDigimon(id);

            if (data == null)
            {
                GD.PrintErr($"Digimon {id} não encontrado.");
                return;
            }

            CenterService.AddDigimon(new DigimonInstance(data));
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
            World.AdvanceDay(days);

            foreach (var digimon in Center.Digimons)
            {
                digimon.AdvanceDays(days);
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

            return result;
        }
    }
}