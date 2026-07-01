using Godot;
using ProjetoDC.Enums;
using ProjetoDC.Scripts.Gameplay;
using ProjetoDC.Scripts.Managers;
using ProjetoDC.Scripts.Models.World;
using ProjetoDC.Scripts.Systems;

namespace ProjetoDC.Scripts.UI
{
    public partial class TrainingCenter : Control
    {
        private const string PortraitFolder = "res://Assets/Sprites/Digimon/";

        private Label _nameLabel;
        private Label _levelLabel;
        private Label _hpLabel;
        private Label _attackLabel;
        private Label _defenseLabel;
        private Label _speedLabel;
        private Label _enemyHpLabel;
        private Label _expLabel;

        private Label _currentDayLabel;
        private Label _bitsLabel;
        private Label _capacityLabel;

        private Button _trainAttackButton;
        private Button _trainDefenseButton;
        private Button _trainSpeedButton;
        private Button _battleButton;
        private Button _advanceDayButton;

        private OptionButton _playerOption;
        private OptionButton _enemyOption;

        private ProgressBar _hpProgressBar;
        private ProgressBar _expProgressBar;

        private TextureRect _portrait;

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

            // preencher opções
            var db = DatabaseManager.Instance;
            if (_playerOption != null && _enemyOption != null)
            {
                foreach (var digimon in db.GetAllDigimons())
                {
                    _playerOption.AddItem(digimon.Name, digimon.Id);
                    _enemyOption.AddItem(digimon.Name, digimon.Id);
                }
            }
            else
            {
                GD.PrintErr("OptionButtons não encontrados na cena. Verifique caminhos dos nós.");
            }

            // garantir digimons iniciais (comportamento original)
            if (Game.PlayerDigimon == null || Game.EnemyDigimon == null)
            {
                // StartBattle pode estar ausente em algumas versões do GameManager;
                // chamamos apenas se existir
                try
                {
                    Game.StartBattle(1, 2);
                }
                catch
                {
                    GD.Print("Game.StartBattle não disponível—verifique GameManager.");
                }
            }

            RefreshUI();
            GD.Print("TrainingCenter iniciado.");
        }

        private void InitializeUIComponents()
        {
            var basePath = "MarginContainer/VBoxContainer/";

            // Usar GetNodeOrNull para evitar exceções se caminho estiver incorreto
            _portrait = GetNodeOrNull<TextureRect>($"{basePath}Portrait");

            _nameLabel = GetNodeOrNull<Label>($"{basePath}NameLabel");
            _levelLabel = GetNodeOrNull<Label>($"{basePath}LevelLabel");
            _hpLabel = GetNodeOrNull<Label>($"{basePath}HpLabel");
            _attackLabel = GetNodeOrNull<Label>($"{basePath}AttackLabel");
            _defenseLabel = GetNodeOrNull<Label>($"{basePath}DefenseLabel");
            _speedLabel = GetNodeOrNull<Label>($"{basePath}SpeedLabel");
            _expLabel = GetNodeOrNull<Label>($"{basePath}ExpLabel");
            _enemyHpLabel = GetNodeOrNull<Label>($"{basePath}EnemyHpLabel");

            _hpProgressBar = GetNodeOrNull<ProgressBar>($"{basePath}HpProgressBar");
            _expProgressBar = GetNodeOrNull<ProgressBar>($"{basePath}ExpProgressBar");

            _playerOption = GetNodeOrNull<OptionButton>($"{basePath}PlayerDigimonOption");
            _enemyOption = GetNodeOrNull<OptionButton>($"{basePath}EnemyDigimonOption");

            // caminhos para o bloco de dia/ações (ajuste se seu nó tiver outra hierarquia)
            var dayPath = $"{basePath}HBoxContainer2/";
            _currentDayLabel = GetNodeOrNull<Label>($"{dayPath}CurrentDayLabel");
            _bitsLabel = GetNodeOrNull<Label>($"{dayPath}BitsLabel");
            _capacityLabel = GetNodeOrNull<Label>($"{dayPath}CapacityLabel");
            _advanceDayButton = GetNodeOrNull<Button>($"{dayPath}AdvanceDayButton");

            var buttonPath = $"{basePath}HBoxContainer/";
            _trainAttackButton = GetNodeOrNull<Button>($"{buttonPath}TrainAttackButton");
            _trainDefenseButton = GetNodeOrNull<Button>($"{buttonPath}TrainDefenseButton");
            _trainSpeedButton = GetNodeOrNull<Button>($"{buttonPath}TrainSpeedButton");
            _battleButton = GetNodeOrNull<Button>($"{buttonPath}BattleButton");

            // Conexão segura de sinais (somente se nós foram encontrados)
            if (_trainAttackButton != null) _trainAttackButton.Pressed += OnTrainAttackButtonPressed;
            if (_trainDefenseButton != null) _trainDefenseButton.Pressed += OnTrainDefenseButtonPressed;
            if (_trainSpeedButton != null) _trainSpeedButton.Pressed += OnTrainSpeedButtonPressed;
            if (_battleButton != null) _battleButton.Pressed += OnBattleButtonPressed;
            if (_advanceDayButton != null) _advanceDayButton.Pressed += OnAdvanceDayPressed;

            if (_playerOption != null) _playerOption.ItemSelected += OnPlayerDigimonOptionItemSelected;
            if (_enemyOption != null) _enemyOption.ItemSelected += OnEnemyDigimonOptionItemSelected;
        }

        public void RefreshUI()
        {
            // Atualizar World / Center visuais
            if (Game.World != null)
            {
                if (_currentDayLabel != null)
                    _currentDayLabel.Text = $"Day: {Game.World.CurrentDay}";
            }
            else
            {
                GD.Print("WorldState nulo em RefreshUI()");
            }




            /*
            // Mostrar bits e capacidade usando Game.Center (se existir)
            var centerProp = Game.GetType().GetProperty("Center");
            if (centerProp != null)
            {
                var center = centerProp.GetValue(Game.Center);
                if (center != null)
                {
                    // usar reflexão leve para acessar propriedades sem depender de referência direta
                    var bitsProp = center.GetType().GetProperty("Bits");
                    var capUsedProp = center.GetType().GetProperty("CapacityUsed");
                    var capLimitProp = center.GetType().GetProperty("CapacityLimit");

                    int bits = bitsProp != null ? (int)bitsProp.GetValue(center) : 0;
                    int used = capUsedProp != null ? (int)capUsedProp.GetValue(center) : 0;
                    int limit = capLimitProp != null ? (int)capLimitProp.GetValue(center) : 0;

                    if (_bitsLabel != null) _bitsLabel.Text = $"Bits: {bits}";
                    if (_capacityLabel != null) _capacityLabel.Text = $"Capacidade: {used}/{limit}";
                }
                else
                {
                    if (_bitsLabel != null) _bitsLabel.Text = "Bits: N/A";
                    if (_capacityLabel != null) _capacityLabel.Text = "Capacidade: N/A";
                }
            }
            else
            {
                // Game não tem propriedade Center (versão do GameManager antiga)
                if (_bitsLabel != null) _bitsLabel.Text = "Bits: N/D";
                if (_capacityLabel != null) _capacityLabel.Text = "Capacidade: N/D";
            }
            */

            // Atualizar player/enemy se existirem
            var player = Game.PlayerDigimon;
            var enemy = Game.EnemyDigimon;

            if (player != null)
            {
                if (_nameLabel != null) _nameLabel.Text = $"Nome: {player.BaseData.Name}";
                if (_levelLabel != null) _levelLabel.Text = $"Level: {player.Level}";
                if (_hpLabel != null) _hpLabel.Text = $"HP: {player.CurrentHealthPoints}/{player.MaxHealthPoints}";
                if (_hpProgressBar != null)
                {
                    _hpProgressBar.MaxValue = player.MaxHealthPoints;
                    _hpProgressBar.Value = player.CurrentHealthPoints;
                }
                if (_attackLabel != null) _attackLabel.Text = $"ATK: {player.CurrentStats.PhysicalDamage}";
                if (_defenseLabel != null) _defenseLabel.Text = $"DEF: {player.CurrentStats.PhysicalDefense}";
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

            if (enemy != null && _enemyHpLabel != null)
            {
                _enemyHpLabel.Text = $"Enemy HP: {enemy.CurrentHealthPoints}";
            }
        }

        private void OnAdvanceDayPressed()
        {
            // Verificar existência e executar
            if (Game.World != null)
            {
                Game.World.AdvanceDay();
            }
            else
            {
                GD.PrintErr("Não foi possível avançar o dia: WorldState nulo.");
            }

            // Avançar dias no PlayerDigimon (se existir)
            if (Game.PlayerDigimon != null)
            {
                Game.PlayerDigimon.AdvanceDays();
            }

            RefreshUI();
        }

        private void OnTrainAttackButtonPressed()
        {
            if (Game.PlayerDigimon == null)
            {
                GD.PrintErr("PlayerDigimon nulo ao treinar ATK.");
                return;
            }

            Game.PlayerDigimon.TrainAttack(1);
            RefreshUI();
        }

        private void OnTrainDefenseButtonPressed()
        {
            if (Game.PlayerDigimon == null)
            {
                GD.PrintErr("PlayerDigimon nulo ao treinar DEF.");
                return;
            }

            Game.PlayerDigimon.TrainDefense(1);
            RefreshUI();
        }

        private void OnTrainSpeedButtonPressed()
        {
            if (Game.PlayerDigimon == null)
            {
                GD.PrintErr("PlayerDigimon nulo ao treinar SPD.");
                return;
            }

            Game.PlayerDigimon.TrainSpeed(1);
            RefreshUI();
        }

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
                    if (Game.PlayerDigimon != null) Game.PlayerDigimon.GainExperience(540);
                    GD.Print("Você venceu!");
                    break;
                case BattleResult.EnemyWon:
                    GD.Print("Você perdeu!");
                    break;
            }

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

        private void OnEnemyDigimonOptionItemSelected(long index)
        {
            int id = _enemyOption?.GetItemId((int)index) ?? -1;
            if (id >= 0)
            {
                Game.SetEnemyDigimon(id);
                RefreshUI();
            }
        }
    }
}
