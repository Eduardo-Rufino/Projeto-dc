using Godot;
using ProjetoDC.Enums;
using ProjetoDC.Scripts.Gameplay;
using ProjetoDC.Scripts.Managers;
using ProjetoDC.Scripts.Models.World;
using ProjetoDC.Scripts.Systems;
using System.Threading.Tasks;

namespace ProjetoDC.Scripts.UI
{
    public partial class CenterScreen : Control
    {
        private const string PortraitFolder = "res://Assets/Sprites/Digimon/";

        private Label _nameLabel;
        private Label _levelLabel;
        private Label _hungryLabel;
        private Label _expLabel;
        private Label _ageInDaysLabel;

        private Label _currentDayLabel;
        private Label _bitsLabel;
        private Label _capacityLabel;
        private Label _meatLabel;
        private Label _medicineLabel;

        private Button _trainButton;
        private Button _battleButton;
        private Button _advanceDayButton;
        private Button _feedButton;

        private OptionButton _playerOption;

        private ProgressBar _expProgressBar;

        private TextureRect _portrait;

        private GameManager Game => GameManager.Instance;

        private TrainingScreen _trainingScreen;
        private EnemySelectionScreen _enemySelectionScreen;
        private BattleScreen _battleScreen;
        private ShopScreen _shopScreen;
        private Control _centerPanel;
        private DigimonSprite _digimonSprite;

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
            _enemySelectionScreen = GetNodeOrNull<EnemySelectionScreen>("EnemySelectionScreen");
            _battleScreen = GetNodeOrNull<BattleScreen>("BattleScreen");
            _shopScreen = GetNodeOrNull<ShopScreen>("ShopScreen");
            _centerPanel = GetNodeOrNull<Control>("MarginContainer");
            _digimonSprite = GetNodeOrNull<DigimonSprite>("MarginContainer/VBoxContainer/HBoxContainer3/DigimonDisplay/DigimonSprite");

            if (_digimonSprite != null)
            {
                _digimonSprite.SetDigimon("agumon");
            }
            else
            {
                GD.PrintErr("DigimonSprite não encontrado");
            }

            if (_trainingScreen == null)
                GD.PrintErr("TrainingScreen não encontrado na cena!");

            _trainingScreen.BackPressed += OnTrainingBack;

            if (_battleScreen == null)
                GD.PrintErr("BattleScreen não encontrado na cena!");

            _battleScreen.BackPressed += OnBattleBack;

            if (_enemySelectionScreen == null)
                GD.PrintErr("EnemySelectionScreen não encontrado na cena!");

            _enemySelectionScreen.BackPressed += OnEnemySelectionBack;

            if (_shopScreen == null)
                GD.PrintErr("ShopScreen não encontrado na cena!");

            _shopScreen.BackPressed += OnShopBack;
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

            /*
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
            */

            RefreshUI();
        }

        private void InitializeUIComponents()
        {
            var basePath = "MarginContainer/VBoxContainer/";

            _trainingScreen = GetNodeOrNull<TrainingScreen>("TrainingScreen");
            _battleScreen = GetNodeOrNull<BattleScreen>("BattleScreen");
            _shopScreen = GetNodeOrNull<ShopScreen>("ShopScreen");

            if (_trainingScreen == null)
                GD.PrintErr("TrainingScreen não encontrado!");
            _centerPanel = GetNode<Control>("MarginContainer");

            if (_battleScreen == null)
                GD.PrintErr("BattleScreen não encontrado!");
            _centerPanel = GetNode<Control>("MarginContainer");

            if (_shopScreen == null)
                GD.PrintErr("ShopScreen não encontrado!");
            _centerPanel = GetNode<Control>("MarginContainer");

            // Usar GetNodeOrNull para evitar exceções se caminho estiver incorreto
            _portrait = GetNodeOrNull<TextureRect>($"{basePath}Portrait");

            _nameLabel = GetNodeOrNull<Label>($"{basePath}NameLabel");
            _levelLabel = GetNodeOrNull<Label>($"{basePath}LevelLabel");
            _hungryLabel = GetNodeOrNull<Label>($"{basePath}HBoxContainer4/HungryLabel");
            _expLabel = GetNodeOrNull<Label>($"{basePath}ExpLabel");
            _ageInDaysLabel = GetNodeOrNull<Label>($"{basePath}AgeInDaysLabel");
            _bitsLabel = GetNodeOrNull<Label>($"{basePath}BitsLabel");
            _capacityLabel = GetNodeOrNull<Label>($"{basePath}CapacityLabel");
            _meatLabel = GetNodeOrNull<Label>($"{basePath}MeatLabel");
            _medicineLabel = GetNodeOrNull<Label>($"{basePath}MedicineLabel");


            _expProgressBar = GetNodeOrNull<ProgressBar>($"{basePath}ExpProgressBar");

            _playerOption = GetNodeOrNull<OptionButton>($"{basePath}PlayerDigimonOption");

            // caminhos para o bloco de dia/ações (ajuste se seu nó tiver outra hierarquia)
            var dayPath = $"{basePath}HBoxContainer2/";
            _currentDayLabel = GetNodeOrNull<Label>($"{dayPath}CurrentDayLabel");
            _advanceDayButton = GetNodeOrNull<Button>($"{dayPath}AdvanceDayButton");

            var buttonPath = $"{basePath}HBoxContainer/";
            _trainButton = GetNodeOrNull<Button>($"{buttonPath}TrainButton");
            _battleButton = GetNodeOrNull<Button>($"{buttonPath}BattleButton");
            _feedButton = GetNodeOrNull<Button>($"{basePath}HBoxContainer4/FeedButton");

            if (_advanceDayButton != null) _advanceDayButton.ButtonUp += OnAdvanceDayPressed;
            if (_feedButton != null) _feedButton.ButtonUp += OnFeedButtonPressed;
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

            if (_meatLabel != null)
                _meatLabel.Text = $"Comida: {Game.Save.Center.Meat}";

            if (_medicineLabel != null)
                _medicineLabel.Text = $"Remédio: {Game.Save.Center.Medicine}";

            // Atualizar player/enemy se existirem
            var player = Game.PlayerDigimon;
            var enemy = Game.EnemyDigimon;

            if (player != null)
            {
                if (_nameLabel != null) _nameLabel.Text = $"Nome: {player.BaseData.Name}";
                if (_levelLabel != null) _levelLabel.Text = $"Level: {player.Level}";
                if (_hungryLabel != null) _hungryLabel.Text = $"Hunger: {player.Hunger}/{player.MaxHunger}";
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

                if (_digimonSprite != null)
                {
                    _digimonSprite.SetDigimon(player.BaseData.Code);
                }

                // portrait
                if (_portrait != null)
                {
                    string path = $"{PortraitFolder}{player.BaseData.Code}.png";
                    if (ResourceLoader.Exists(path)) _portrait.Texture = GD.Load<Texture2D>(path);
                }
            }

            if (Game.PlayerDigimon != null)
            {
                GD.Print($"Center Player Hash: {Game.PlayerDigimon.GetHashCode()}");
                GD.Print($"Center XP: {Game.PlayerDigimon.Experience}");
                //TODO: Atualizar a lista apenas quando um digimon evoluir, for adicionado ou removido, para evitar refresh desnecessário
                RefreshDigimonList();
            }
        }

        private async Task Wait(float seconds)
        {
            await ToSignal(
                GetTree().CreateTimer(seconds),
                SceneTreeTimer.SignalName.Timeout);
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

        private async void OnFeedButtonPressed()
        {
            var result = Game.FeedPlayer();

            if (result.Success)
            {
                await _digimonSprite.PlayEat();
            }
            else
            {
                await _digimonSprite.PlayRefuse();
            }

            await Wait(1.5f);

            RefreshUI();
        }
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
            _battleScreen.StopBattle();

            _battleScreen.Visible = false;
            _centerPanel.Visible = true;

            RefreshUI();
        }

        private void OnEnemySelectionBack()
        {
            _enemySelectionScreen.Visible = false;
            _centerPanel.Visible = true;

            RefreshUI();
        }

        private void OnShopBack()
        {
            _shopScreen.Visible = false;
            _centerPanel.Visible = true;

            RefreshUI();
        }

        private void OnTrainButtonPressed()
        {
            _centerPanel.Visible = false;
            _trainingScreen.Visible = true;

            _trainingScreen.RefreshUI();
        }

        private void OnShopButtonPressed()
        {
            _centerPanel.Visible = false;
            _shopScreen.Visible = true;

            _shopScreen.RefreshUI();
        }

        private void OnBattleButtonPressed()
        {
            Game.GenerateEnemyCandidates();

            _centerPanel.Visible = false;
            _enemySelectionScreen.Visible = true;

            _enemySelectionScreen.RefreshUI();
        }

        private void OnAttackPressed()
        {
            _digimonSprite.PlayAttack();
        }

        private void OnHappyPressed()
        {
            _digimonSprite.PlayHappy();
        }

        private void OnEatPressed()
        {
            _digimonSprite.PlayEat();
        }

        private void OnSleepPressed()
        {
            _digimonSprite.PlaySleep();
        }

        private void OnIdlePressed()
        {
            _digimonSprite.PlayIdle();
        }
        private void OnAngryPressed()
        {
            _digimonSprite.PlayAngry();
        }
        private void OnTrainPressed()
        {
            _digimonSprite.PlayTrain();
        }
        private void OnRefusePressed()
        {
            _digimonSprite.PlayRefuse();
        }
    }
}
