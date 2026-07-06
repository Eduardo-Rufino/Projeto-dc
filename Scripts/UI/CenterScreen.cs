using Godot;
using ProjetoDC.Enums;
using ProjetoDC.Scripts.Gameplay;
using ProjetoDC.Scripts.Managers;
using ProjetoDC.Scripts.Models.World;
using ProjetoDC.Scripts.Systems;

namespace ProjetoDC.Scripts.UI
{
    public partial class CenterScreen : Control
    {
        private const string PortraitFolder = "res://Assets/Sprites/Digimon/";

        private Label _nameLabel;
        private Label _levelLabel;
        private Label _expLabel;
        private Label _ageInDaysLabel;

        private Label _currentDayLabel;
        private Label _bitsLabel;
        private Label _capacityLabel;

        private Button _trainButton;
        private Button _battleButton;
        private Button _advanceDayButton;

        private OptionButton _playerOption;

        private ProgressBar _expProgressBar;

        private TextureRect _portrait;

        private GameManager Game => GameManager.Instance;

        private TrainingScreen _trainingScreen;
        private BattleScreen _battleScreen;
        private Control _centerPanel;

        public override void _Ready()
        {
            // Verificações iniciais
            if (GameManager.Instance == null)
            {
                GD.PrintErr("GameManager ainda não inicializado!");
                return;
            }

            if (DatabaseManager.Instance == null)
            {
                GD.PrintErr("DatabaseManager ainda não inicializado!");
                return;
            }

            // carregar nós da UI com segurança
            InitializeUIComponents();

            // <<< ADICIONAR AQUI >>>
            _trainingScreen = GetNodeOrNull<TrainingScreen>("TrainingScreen");
            _battleScreen = GetNodeOrNull<BattleScreen>("BattleScreen");
            _centerPanel = GetNodeOrNull<Control>("MarginContainer");

            if (_trainingScreen == null)
                GD.PrintErr("TrainingScreen não encontrado na cena!");

            _trainingScreen.BackPressed += OnTrainingBack;

            if (_battleScreen == null)
                GD.PrintErr("BattleScreen não encontrado na cena!");

            _battleScreen.BackPressed += OnBattleBack;
            // <<< FIM >>>

            // preencher opções
            var db = DatabaseManager.Instance;
            if (_playerOption != null)
            {
                RefreshDigimonList();
            }
            else
            {
                GD.PrintErr("OptionButtons não encontrados na cena. Verifique caminhos dos nós.");
            }

            if (Game.PlayerDigimon == null || Game.EnemyDigimon == null)
            {
                try
                {
                    Game.InitializeBattle();
                }
                catch
                {
                    GD.Print("Game.InitializeBattle não disponível.");
                }
            }

            RefreshUI();
        }

        private void InitializeUIComponents()
        {
            var basePath = "MarginContainer/VBoxContainer/";

            _trainingScreen = GetNodeOrNull<TrainingScreen>("TrainingScreen");
            _battleScreen = GetNodeOrNull<BattleScreen>("BattleScreen");

            if (_trainingScreen == null)
                GD.PrintErr("TrainingScreen não encontrado!");
            _centerPanel = GetNode<Control>("MarginContainer");

            if (_battleScreen == null)
                GD.PrintErr("BattleScreen não encontrado!");
            _centerPanel = GetNode<Control>("MarginContainer");

            // Usar GetNodeOrNull para evitar exceções se caminho estiver incorreto
            _portrait = GetNodeOrNull<TextureRect>($"{basePath}Portrait");

            _nameLabel = GetNodeOrNull<Label>($"{basePath}NameLabel");
            _levelLabel = GetNodeOrNull<Label>($"{basePath}LevelLabel");
            _expLabel = GetNodeOrNull<Label>($"{basePath}ExpLabel");
            _ageInDaysLabel = GetNodeOrNull<Label>($"{basePath}AgeInDaysLabel");
            _bitsLabel = GetNodeOrNull<Label>($"{basePath}BitsLabel");
            _capacityLabel = GetNodeOrNull<Label>($"{basePath}CapacityLabel");

            _expProgressBar = GetNodeOrNull<ProgressBar>($"{basePath}ExpProgressBar");

            _playerOption = GetNodeOrNull<OptionButton>($"{basePath}PlayerDigimonOption");

            // caminhos para o bloco de dia/ações (ajuste se seu nó tiver outra hierarquia)
            var dayPath = $"{basePath}HBoxContainer2/";
            _currentDayLabel = GetNodeOrNull<Label>($"{dayPath}CurrentDayLabel");
            _advanceDayButton = GetNodeOrNull<Button>($"{dayPath}AdvanceDayButton");

            var buttonPath = $"{basePath}HBoxContainer/";
            _trainButton = GetNodeOrNull<Button>($"{buttonPath}TrainButton");
            _battleButton = GetNodeOrNull<Button>($"{buttonPath}BattleButton");

            if (_advanceDayButton != null) _advanceDayButton.ButtonUp += OnAdvanceDayPressed;
        }

        public void RefreshUI()
        {
            // Atualizar World / Center visuais
            if (Game.Save.World != null)
            {
                if (_currentDayLabel != null)
                    _currentDayLabel.Text = $"Day: {Game.Save.World.CurrentDay}";
            }
            else
            {
                GD.Print("WorldState nulo em RefreshUI()");
            }




            if (_bitsLabel != null)
                _bitsLabel.Text = $"Bits: {Game.Save.Center.Bits}";

            if (_capacityLabel != null)
                _capacityLabel.Text = $"Capacidade: {Game.Save.Center.CapacityUsed}/{Game.Save.Center.CapacityLimit}";

            // Atualizar player/enemy se existirem
            var player = Game.PlayerDigimon;
            var enemy = Game.EnemyDigimon;

            if (player != null)
            {
                if (_nameLabel != null) _nameLabel.Text = $"Nome: {player.BaseData.Name}";
                if (_levelLabel != null) _levelLabel.Text = $"Level: {player.Level}";
                if (_expLabel != null)
                {
                    _expLabel.Text = $"EXP: {player.Experience}/{player.ExperienceToNextLevel}";
                    if (_expProgressBar != null)
                    {
                        _expProgressBar.MaxValue = player.ExperienceToNextLevel;
                        _expProgressBar.Value = player.Experience;
                    }
                }

                if (_ageInDaysLabel != null)
                    _ageInDaysLabel.Text = $"Age: {player.AgeInDays} days";

                // portrait
                if (_portrait != null)
                {
                    string path = $"{PortraitFolder}{player.BaseData.Code}.png";
                    if (ResourceLoader.Exists(path)) _portrait.Texture = GD.Load<Texture2D>(path);
                }
            }

            GD.Print($"Center Player Hash: {Game.PlayerDigimon.GetHashCode()}");
            GD.Print($"Center XP: {Game.PlayerDigimon.Experience}");
        }

        public void RefreshDigimonList()
        {
            // Atualizar lista de digimons do player
            _playerOption.Clear();
            foreach (var digimon in Game.CenterService.GetAllDigimons())
            {
                GD.Print("------------------------------------" + digimon.BaseData.Name);
                _playerOption.AddItem(digimon.BaseData.Name, digimon.BaseData.Id);
            }
        }

        private void OnAdvanceDayPressed()
        {
            Game.AdvanceDay();

            RefreshUI();
        }

        /*
        private void OnBattleButtonPressed()
        {
            if (Game.BattleSystem == null)
            {
                GD.PrintErr("BattleSystem nulo. Certifique-se de que Player e Enemy estão definidos.");
                return;
            }

            var result = Game.BattleSystem.ExecuteTurn();

            switch (result)
            {
                case BattleResult.PlayerWon:
                    GD.Print("Você venceu!");
                    if (Game.PlayerDigimon != null) Game.PlayerDigimon.GainExperience(540);                    
                    Game.TryToEvolve(Game.PlayerDigimon);
                    break;
                case BattleResult.EnemyWon:
                    GD.Print("Você perdeu!");
                    break;
            }

            RefreshUI();
        }
        */

        private void OnPlayerDigimonOptionItemSelected(long index)
        {
            int id = _playerOption?.GetItemId((int)index) ?? -1;
            if (id >= 0)
            {
                Game.SetPlayerDigimon(id);
                RefreshUI();
            }
        }

        private void OnTrainingBack()
        {
            _trainingScreen.Visible = false;
            _centerPanel.Visible = true;

            RefreshUI();
        }

        private void OnBattleBack()
        {
            _battleScreen.Visible = false;
            _centerPanel.Visible = true;

            RefreshUI();
        }

        private void OnTrainButtonPressed()
        {
            _centerPanel.Visible = false;
            _trainingScreen.Visible = true;

            _trainingScreen.RefreshUI();
        }

        private void OnBattleButtonPressed()
        {
            Game.InitializeBattle();

            _centerPanel.Visible = false;
            _battleScreen.Visible = true;

            _battleScreen.Init(Game.BattleSystem);
        }
    }
}
