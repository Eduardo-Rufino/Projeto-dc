using Godot;
using ProjetoDC.Enums;
using ProjetoDC.Scripts.Gameplay;
using ProjetoDC.Scripts.Managers;
using ProjetoDC.Scripts.Models.World;
using ProjetoDC.Scripts.Systems;
using System.Linq;
using System.Threading.Tasks;

namespace ProjetoDC.Scripts.UI
{
    public partial class CenterScreen : Control
    {
        private const string PortraitFolder = "res://Assets/Sprites/Digimon/";

        private Label _nameLabel;
        private Label _healthLabel;
        private Label _levelLabel;
        private Label _hungryLabel;
        private Label _staminaLabel;
        private Label _expLabel;
        private Label _ageInDaysLabel;

        private Label _currentDayLabel;
        private Label _clockLabel;
        private Label _bitsLabel;
        private Label _capacityLabel;
        private Label _meatLabel;
        private Label _medicineLabel;

        private Button _trainButton;
        private Button _battleButton;
        private Button _advanceDayButton;
        private Button _feedButton;
        private Button _useMedicineButton;
        private Button _addDebugDigimonButton;

        private OptionButton _playerOption;

        private ProgressBar _expProgressBar;

        private TextureRect _portrait;

        private GameManager Game => GameManager.Instance;

        private TrainingScreen _trainingScreen;
        private TeamSelectionScreen _teamSelectionScreen;
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
            _teamSelectionScreen = GetNodeOrNull<TeamSelectionScreen>("TeamSelectionScreen");
            _shopScreen = GetNodeOrNull<ShopScreen>("ShopScreen");
            _centerPanel = GetNodeOrNull<Control>("MarginContainer");
            _digimonSprite = GetNodeOrNull<DigimonSprite>("MarginContainer/VBoxContainer/HBoxContainer3/DigimonDisplay/DigimonSprite");

            if (_digimonSprite == null)
            {
                GD.PrintErr("DigimonSprite não encontrado");
                return;
            }

            Game.ClockSystem.MinutePassed += OnMinutePassed;
            Game.ClockSystem.HourPassed += OnHourPassed;
            GameManager.Instance.PlayerDigimonChanged += RefreshUI;
            Game.GameLoaded += OnGameLoaded;
            Game.TeamBattleFinished += OnTeamBattleFinished;

            if (_trainingScreen == null)
                GD.PrintErr("TrainingScreen não encontrado na cena!");

            _trainingScreen.BackPressed += OnTrainingBack;

            if (_teamSelectionScreen == null)
                GD.PrintErr("TeamSelectionScreen não encontrado na cena!");

            _teamSelectionScreen.BackPressed += OnTeamSelectionBack;

            if (_shopScreen == null)
                GD.PrintErr("ShopScreen não encontrado na cena!");

            _shopScreen.BackPressed += OnShopBack;

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

            RefreshUI();
        }

        private void InitializeUIComponents()
        {
            var basePath = "MarginContainer/VBoxContainer/";

            _trainingScreen = GetNodeOrNull<TrainingScreen>("TrainingScreen");
            _teamSelectionScreen = GetNodeOrNull<TeamSelectionScreen>("TeamSelectionScreen");
            _shopScreen = GetNodeOrNull<ShopScreen>("ShopScreen");

            if (_trainingScreen == null)
                GD.PrintErr("TrainingScreen não encontrado!");
            _centerPanel = GetNode<Control>("MarginContainer");

            if (_teamSelectionScreen == null)
                GD.PrintErr("TeamSelectionScreen não encontrado!");
            _centerPanel = GetNode<Control>("MarginContainer");

            if (_shopScreen == null)
                GD.PrintErr("ShopScreen não encontrado!");
            _centerPanel = GetNode<Control>("MarginContainer");

            // Usar GetNodeOrNull para evitar exceções se caminho estiver incorreto
            _portrait = GetNodeOrNull<TextureRect>($"{basePath}Portrait");

            _nameLabel = GetNodeOrNull<Label>($"{basePath}NameLabel");
            _healthLabel = GetNodeOrNull<Label>($"{basePath}HealthLabel");
            _levelLabel = GetNodeOrNull<Label>($"{basePath}LevelLabel");
            _hungryLabel = GetNodeOrNull<Label>($"{basePath}HBoxContainer4/HungryLabel");
            _staminaLabel = GetNodeOrNull<Label>($"{basePath}StaminaLabel");
            _expLabel = GetNodeOrNull<Label>($"{basePath}ExpLabel");
            _ageInDaysLabel = GetNodeOrNull<Label>($"{basePath}AgeInDaysLabel");
            _bitsLabel = GetNodeOrNull<Label>($"{basePath}BitsLabel");
            _capacityLabel = GetNodeOrNull<Label>($"{basePath}CapacityLabel");
            _meatLabel = GetNodeOrNull<Label>($"{basePath}MeatLabel");
            _medicineLabel = GetNodeOrNull<Label>($"{basePath}MedicineLabel");
            _addDebugDigimonButton = GetNodeOrNull<Button>($"{basePath}AddDebugDigimonButton");

            _expProgressBar = GetNodeOrNull<ProgressBar>($"{basePath}ExpProgressBar");

            _playerOption = GetNodeOrNull<OptionButton>($"{basePath}PlayerDigimonOption");

            // caminhos para o bloco de dia/ações (ajuste se seu nó tiver outra hierarquia)
            var dayPath = $"{basePath}HBoxContainer2/";
            _currentDayLabel = GetNodeOrNull<Label>($"{dayPath}CurrentDayLabel");
            _clockLabel = GetNodeOrNull<Label>($"{dayPath}ClockLabel");
            if (_clockLabel == null)
                GD.PrintErr("ClockLabel não encontrado!");
            else
                GD.Print("ClockLabel encontrado.");
            _advanceDayButton = GetNodeOrNull<Button>($"{dayPath}AdvanceDayButton");

            var buttonPath = $"{basePath}HBoxContainer/";
            _trainButton = GetNodeOrNull<Button>($"{buttonPath}TrainButton");
            _battleButton = GetNodeOrNull<Button>($"{buttonPath}BattleButton");
            _feedButton = GetNodeOrNull<Button>($"{basePath}HBoxContainer4/FeedButton");
            _useMedicineButton = GetNodeOrNull<Button>($"{basePath}UseMedicineButton");

            if (Game.PlayerDigimon != null) Game.PlayerDigimon.ActivityChanged += OnPlayerActivityChanged;
            if (_advanceDayButton != null) _advanceDayButton.ButtonUp += OnAdvanceDayPressed;
            if (_feedButton != null) _feedButton.ButtonUp += OnFeedButtonPressed;
            if (_useMedicineButton != null) _useMedicineButton.ButtonUp += OnUseMedicineButtonPressed;
            if (Game.ClockSystem != null) Game.ClockSystem.MinutePassed += OnMinutePassed;
            if(Game.ClockSystem != null) Game.ClockSystem.DayPassed += OnDayPassed;

            if (_addDebugDigimonButton != null)
                _addDebugDigimonButton.ButtonUp += OnAddDebugDigimonPressed;
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

            OnMinutePassed();

            if (_bitsLabel != null)
                _bitsLabel.Text = $"Bits: {Game.Save.Center.Bits}";

            if (_capacityLabel != null)
                _capacityLabel.Text = $"Capacidade: {Game.Save.Center.CapacityUsed}/{Game.Save.Center.CapacityLimit}";

            if (_meatLabel != null)
                _meatLabel.Text = $"Comida: {Game.Save.Center.Meat}";

            if (_medicineLabel != null)
                _medicineLabel.Text = $"Remédio: {Game.Save.Center.Medicine}";

            // Atualizar player se existir
            var player = Game.PlayerDigimon;

            if (player != null)
            {
                if (_nameLabel != null) _nameLabel.Text = $"Nome: {player.BaseData.Name}";
                if (_healthLabel != null) _healthLabel.Text = $"Health: {player.HealthState}";
                if (_levelLabel != null) _levelLabel.Text = $"Level: {player.Level}";
                if (_hungryLabel != null) _hungryLabel.Text = $"Hunger: {player.Hunger}/{player.MaxHunger}";
                if (_staminaLabel != null) _staminaLabel.Text = $"Stamina: {player.Stamina}/{player.MaxStamina}";
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
                    _digimonSprite.RefreshState(player);
                }

                // portrait
                if (_portrait != null)
                {
                    string path = $"{PortraitFolder}{player.BaseData.Code}.png";
                    if (ResourceLoader.Exists(path)) _portrait.Texture = GD.Load<Texture2D>(path);
                }

                if (player.HealthState == HealthState.Sick)
                {
                    _digimonSprite.PlaySick();
                }
                else
                {
                    _digimonSprite.PlayIdle();
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

        private void OnGameLoaded()
        {
            RefreshDigimonList();
            RefreshUI();
        }

        private void OnMinutePassed()
        {
            UpdateClock();
        }

        private void OnDayPassed()
        {
            if (_currentDayLabel == null)
                return;

            _currentDayLabel.Text = $"Day: {Game.Save.World.CurrentDay}";
        }

        private void OnHourPassed()
        {
            UpdateDigimonStatus();
        }

        private void UpdateClock()
        {
            if (_clockLabel != null)
            {
                _clockLabel.Text =
                    $"{Game.Save.World.CurrentHour:00}:{Game.Save.World.CurrentMinute:00}";
            }
        }

        private void UpdateDigimonStatus()
        {
            var player = Game.PlayerDigimon;

            if (player == null)
                return;

            _hungryLabel.Text = $"Hunger: {player.Hunger}/{player.MaxHunger}";
            _staminaLabel.Text = $"Stamina: {player.Stamina}";
        }

        public void RefreshDigimonList()
        {
            int selectedIndex = 0;

            _playerOption.Clear();

            int index = 0;

            foreach (var digimon in Game.CenterService.GetAllDigimons())
            {
                _playerOption.AddItem(
                    digimon.BaseData.Name,
                    index
                );

                if (digimon == Game.PlayerDigimon)
                {
                    selectedIndex = index;
                }

                index++;
            }


            if (_playerOption.ItemCount > 0)
            {
                _playerOption.Select(selectedIndex);
            }
        }

        private void OnPlayerActivityChanged(DigimonActivity activity)
        {
            if (Game.PlayerDigimon == null || _digimonSprite == null)
                return;

            _digimonSprite.RefreshState(Game.PlayerDigimon);
        }

        private void OnAdvanceDayPressed()
        {
            Game.SkipDay();

            RefreshUI();
        }

        private async void OnFeedButtonPressed()
        {
            var result = Game.FeedPlayer();

            if (result.Success)
            {
                _digimonSprite.PlayEat();
            }
            else
            {
                await _digimonSprite.PlayRefuse();
            }

            await Wait(1.5f);

            RefreshUI();
        }

        private void OnUseMedicineButtonPressed()
        {
            var result = Game.UseMedicinePlayer();

            if (result.Success)
            {
                RefreshUI();
            }
            else
            {
                GD.Print(result.Reason);
            }
        }

        private void OnPlayerDigimonOptionItemSelected(long index)
        {
            int listIndex = (int)index;

            var digimonList = Game.CenterService.GetAllDigimons();

            if (listIndex >= 0 && listIndex < digimonList.Count)
            {
                var digimon = digimonList[listIndex];

                Game.SetPlayerDigimon(digimon);

                RefreshUI();
            }
        }

        private void OnTrainingBack()
        {
            _trainingScreen.Visible = false;
            _centerPanel.Visible = true;

            RefreshUI();
        }

        private void OnTeamSelectionBack()
        {
            _teamSelectionScreen.Visible = false;
            _centerPanel.Visible = true;

            RefreshUI();
        }

        private void OnTeamBattleFinished(BattleResult result)
        {
            _teamSelectionScreen.Visible = false;
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
            _centerPanel.Visible = false;
            _teamSelectionScreen.Visible = true;

            _teamSelectionScreen.RefreshUI();
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

        private void OnAddDebugDigimonPressed()
        {
            Game.AddDebugDigimon(2); // ID do Agumon (confirme se é realmente 2 no seu banco)
            Game.AddDebugDigimon(3);

            RefreshDigimonList();
        }
    }
}
