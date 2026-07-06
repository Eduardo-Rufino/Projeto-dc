using Godot;
using ProjetoDC.Enums;
using ProjetoDC.Scripts.Managers;
using System;

namespace ProjetoDC.Scripts.UI
{
    public partial class TrainingScreen : Control
    {
        private const string PortraitFolder = "res://Assets/Sprites/Digimon/";

        private Label _nameLabel;
        private Label _levelLabel;
        private Label _maxHpLabel;
        private Label _attackLabel;
        private Label _defenseLabel;
        private Label _specialAttackLabel;
        private Label _specialDefenseLabel;
        private Label _speedLabel;
        private Label _expLabel;

        private Label _currentDayLabel;
        private Label _bitsLabel;

        private Button _trainHealthPointsButton;
        private Button _trainAttackButton;
        private Button _trainDefenseButton;
        private Button _trainSpecialDefenseButton;
        private Button _trainSpecialAttackButton;
        private Button _trainSpeedButton;
        private Button _advanceDayButton;

        private ProgressBar _expProgressBar;

        private TextureRect _portrait;

        public event Action BackPressed;

        private GameManager Game => GameManager.Instance;

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
            
            RefreshUI();
            GD.Print("TrainingCenter iniciado.");
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
            

            // Atualizar player
            var player = Game.PlayerDigimon;

            if (player != null)
            {
                if (_nameLabel != null) _nameLabel.Text = $"Nome: {player.BaseData.Name}";
                if (_levelLabel != null) _levelLabel.Text = $"Level: {player.Level}";
                if (_maxHpLabel != null) _maxHpLabel.Text = $"HP: {player.MaxHealthPoints}";

                if (_attackLabel != null) _attackLabel.Text = $"ATK: {player.CurrentStats.PhysicalDamage}";
                if (_defenseLabel != null) _defenseLabel.Text = $"DEF: {player.CurrentStats.PhysicalDefense}";
                if (_specialAttackLabel != null) _specialAttackLabel.Text = $"SP. ATK: {player.CurrentStats.SpecialDamage}";
                if (_specialDefenseLabel != null) _specialDefenseLabel.Text = $"SP. DEF: {player.CurrentStats.SpecialDefense}";
                if (_speedLabel != null) _speedLabel.Text = $"SPD: {player.CurrentStats.Speed}";
                if (_expLabel != null)
                {
                    _expLabel.Text = $"EXP: {player.Experience}/{player.ExperienceToNextLevel}";
                    if (_expProgressBar != null)
                    {
                        _expProgressBar.MaxValue = player.ExperienceToNextLevel;
                        _expProgressBar.Value = player.Experience;
                    }
                }


                // portrait
                if (_portrait != null)
                {
                    string path = $"{PortraitFolder}{player.BaseData.Code}.png";
                    if (ResourceLoader.Exists(path)) _portrait.Texture = GD.Load<Texture2D>(path);
                }
            }
        }

        private void InitializeUIComponents()
        {
            var basePath = "MarginContainer/VBoxContainer/";

            // Nó de portrait
            _portrait = GetNodeOrNull<TextureRect>($"{basePath}Portrait");

            // Labels principais
            _nameLabel = GetNodeOrNull<Label>($"{basePath}NameLabel");
            _levelLabel = GetNodeOrNull<Label>($"{basePath}LevelLabel");
            _maxHpLabel = GetNodeOrNull<Label>($"{basePath}MaxHpLabel");
            _attackLabel = GetNodeOrNull<Label>($"{basePath}AttackLabel");
            _defenseLabel = GetNodeOrNull<Label>($"{basePath}DefenseLabel");
            _specialAttackLabel = GetNodeOrNull<Label>($"{basePath}SpecialAttackLabel");
            _specialDefenseLabel = GetNodeOrNull<Label>($"{basePath}SpecialDefenseLabel");
            _speedLabel = GetNodeOrNull<Label>($"{basePath}SpeedLabel");
            _expLabel = GetNodeOrNull<Label>($"{basePath}ExpLabel");
            _bitsLabel = GetNodeOrNull<Label>($"{basePath}BitsLabel");


            // ProgressBars
            _expProgressBar = GetNodeOrNull<ProgressBar>($"{basePath}ExpProgressBar");

            // Day / Bits
            var dayPath = $"{basePath}HBoxContainer4/";
            _currentDayLabel = GetNodeOrNull<Label>($"{dayPath}CurrentDayLabel");
            //_bitsLabel = GetNodeOrNull<Label>($"{basePath}BitsLabel");
            _advanceDayButton = GetNodeOrNull<Button>($"{dayPath}AdvanceDayButton");

            // Botões de treino
            var buttonPath = $"{basePath}HBoxContainer/";
            _trainHealthPointsButton = GetNodeOrNull<Button>($"{buttonPath}TrainHealthPointsButton");
            _trainAttackButton = GetNodeOrNull<Button>($"{buttonPath}TrainAttackButton");
            _trainDefenseButton = GetNodeOrNull<Button>($"{buttonPath}TrainDefenseButton");
            _trainSpecialAttackButton = GetNodeOrNull<Button>($"{buttonPath}TrainSpecialAttackButton");
            _trainSpecialDefenseButton = GetNodeOrNull<Button>($"{buttonPath}TrainSpecialDefenseButton");
            _trainSpeedButton = GetNodeOrNull<Button>($"{buttonPath}TrainSpeedButton");

            // Conexões seguras (apenas se existirem nós)
            if (_trainHealthPointsButton != null)
            {
                _trainHealthPointsButton.Pressed += OnTrainHealthPointsButtonPressed;
            }

            if (_trainAttackButton != null)
            {
                _trainAttackButton.Pressed += OnTrainAttackButtonPressed;
            }

            if (_trainDefenseButton != null)
            {
                _trainDefenseButton.Pressed += OnTrainDefenseButtonPressed;
            }

            if (_trainSpecialAttackButton != null)
            {
                _trainSpecialAttackButton.Pressed += OnTrainSpecialAttackButtonPressed;
            }

            if (_trainSpecialDefenseButton != null)
            {
                _trainSpecialDefenseButton.Pressed += OnTrainSpecialDefenseButtonPressed;
            }

            if (_trainSpeedButton != null)
            {
                _trainSpeedButton.Pressed += OnTrainSpeedButtonPressed;
            }

            if (_advanceDayButton != null)
            {
                _advanceDayButton.Pressed += OnAdvanceDayPressed;
            }
        }

        private void OnAdvanceDayPressed()
        {
            Game.AdvanceDay();

            RefreshUI();
        }

        private void OnTrainHealthPointsButtonPressed()
        {
            Game.TrainPlayer(TrainingType.HealthPoints);

            RefreshUI();
        }

        private void OnTrainAttackButtonPressed()
        {
            Game.TrainPlayer(TrainingType.Attack);

            RefreshUI();
        }

        private void OnTrainDefenseButtonPressed()
        {
            Game.TrainPlayer(TrainingType.Defense);

            RefreshUI();
        }

        private void OnTrainSpecialAttackButtonPressed()
        {
            Game.TrainPlayer(TrainingType.SpecialAttack);

            RefreshUI();
        }

        private void OnTrainSpecialDefenseButtonPressed()
        {
            Game.TrainPlayer(TrainingType.SpecialDefense);

            RefreshUI();
        }

        private void OnTrainSpeedButtonPressed()
        {
            Game.TrainPlayer(TrainingType.Speed);

            RefreshUI();
        }

        private void OnBackPressed()
        {
            BackPressed?.Invoke();
        }
    }
}
