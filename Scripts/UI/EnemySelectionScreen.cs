using Godot;
using ProjetoDC.Scripts.Gameplay;
using ProjetoDC.Scripts.Managers;
using ProjetoDC.Scripts.Systems.Battle;
using System;

namespace ProjetoDC.Scripts.UI
{
    public partial class EnemySelectionScreen : Control
    {
        private const string PortraitFolder = "res://Assets/Sprites/Digimon/";

        private Label _nameLabel1;
        private Label _levelLabel1;
        private Label _roleLabel1;
        private Label _nameLabel2;
        private Label _levelLabel2;
        private Label _roleLabel2;
        private Label _nameLabel3;
        private Label _levelLabel3;
        private Label _roleLabel3;
        private Label _nameLabel4;
        private Label _levelLabel4;
        private Label _roleLabel4;

        private Button _battleButton1;
        private Button _battleButton2;
        private Button _battleButton3;
        private Button _battleButton4;

        private TextureRect _portrait1;
        private TextureRect _portrait2;
        private TextureRect _portrait3;
        private TextureRect _portrait4;

        public event Action BattlePressed;

        private BattleScreen _battleScreen;
        private Control _enemySelectionPanel;

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

            _battleScreen = GetNodeOrNull<BattleScreen>("../BattleScreen");
            _enemySelectionPanel = this;

            RefreshUI();
            GD.Print("EnemySelection iniciado.");
        }

        private void InitializeUIComponents()
        {
            var basePath = "MarginContainer/VBoxContainer/HBoxContainer/VBoxContainer1/";

            // Nó de portrait
            _portrait1 = GetNode<TextureRect>($"{basePath}Portrait1");

            // Labels principais
            _nameLabel1 = GetNodeOrNull<Label>($"{basePath}NameLabel1");
            _levelLabel1 = GetNodeOrNull<Label>($"{basePath}LevelLabel1");
            _roleLabel1 = GetNodeOrNull<Label>($"{basePath}RoleLabel1");
            _battleButton1 = GetNodeOrNull<Button>($"{basePath}BattleButton1");

            basePath = "MarginContainer/VBoxContainer/HBoxContainer/VBoxContainer2/";

            _portrait2 = GetNode<TextureRect>($"{basePath}Portrait2");
            _nameLabel2 = GetNodeOrNull<Label>($"{basePath}NameLabel2");
            _levelLabel2 = GetNodeOrNull<Label>($"{basePath}LevelLabel2");
            _roleLabel2 = GetNodeOrNull<Label>($"{basePath}RoleLabel2");
            _battleButton2 = GetNodeOrNull<Button>($"{basePath}BattleButton2");

            basePath = "MarginContainer/VBoxContainer/HBoxContainer2/VBoxContainer3/";

            _portrait3 = GetNode<TextureRect>($"{basePath}Portrait3");
            _nameLabel3 = GetNodeOrNull<Label>($"{basePath}NameLabel3");
            _levelLabel3 = GetNodeOrNull<Label>($"{basePath}LevelLabel3");
            _roleLabel3 = GetNodeOrNull<Label>($"{basePath}RoleLabel3");
            _battleButton3 = GetNodeOrNull<Button>($"{basePath}BattleButton3");

            basePath = "MarginContainer/VBoxContainer/HBoxContainer2/VBoxContainer4/";

            _portrait4 = GetNode<TextureRect>($"{basePath}Portrait4");
            _nameLabel4 = GetNodeOrNull<Label>($"{basePath}NameLabel4");
            _levelLabel4 = GetNodeOrNull<Label>($"{basePath}LevelLabel4");
            _roleLabel4 = GetNodeOrNull<Label>($"{basePath}RoleLabel4");
            _battleButton4 = GetNodeOrNull<Button>($"{basePath}BattleButton4");

            _battleScreen = GetNodeOrNull<BattleScreen>("BattleScreen");

            if (_battleScreen == null)
            {
                GD.PrintErr("BattleScreen não encontrado!");
            }

            _enemySelectionPanel = this;
        }

        public void RefreshUI()
        {
            // Atualizar player
            var player = Game.PlayerDigimon;

            var enemies = Game.GeneratedEnemies;

            if (enemies != null && enemies.Count > 0)
            {
                if (_nameLabel1 != null) _nameLabel1.Text = $"Nome: {enemies[0].BaseData.Name}";
                if (_levelLabel1 != null) _levelLabel1.Text = $"Level: {enemies[0].Level}";
                if (_roleLabel1 != null) _roleLabel1.Text = $"Role: {enemies[0].BaseData.Role}";
                
                // portrait
                if (_portrait1   != null)
                {
                    string path = $"{PortraitFolder}{enemies[0].BaseData.Code}.png";
                    if (ResourceLoader.Exists(path)) _portrait1.Texture = GD.Load<Texture2D>(path);
                }

                if (_nameLabel2 != null) _nameLabel2.Text = $"Nome: {enemies[1].BaseData.Name}";
                if (_levelLabel2 != null) _levelLabel2.Text = $"Level: {enemies[1].Level}";
                if (_roleLabel2 != null) _roleLabel2.Text = $"Role: {enemies[1].BaseData.Role}";

                // portrait
                if (_portrait2 != null)
                {
                    string path = $"{PortraitFolder}{enemies[1].BaseData.Code}.png";
                    if (ResourceLoader.Exists(path)) _portrait2.Texture = GD.Load<Texture2D>(path);
                }

                if (_nameLabel3 != null) _nameLabel3.Text = $"Nome: {enemies[2].BaseData.Name}";
                if (_levelLabel3 != null) _levelLabel3.Text = $"Level: {enemies[2].Level}";
                if (_roleLabel3 != null) _roleLabel3.Text = $"Role: {enemies[2].BaseData.Role}";

                // portrait
                if (_portrait3 != null)
                {
                    string path = $"{PortraitFolder}{enemies[2].BaseData.Code}.png";
                    if (ResourceLoader.Exists(path)) _portrait3.Texture = GD.Load<Texture2D>(path);
                }

                if (_nameLabel4 != null) _nameLabel4.Text = $"Nome: {enemies[3].BaseData.Name}";
                if (_levelLabel4 != null) _levelLabel4.Text = $"Level: {enemies[3].Level}";
                if (_roleLabel4 != null) _roleLabel4.Text = $"Role: {enemies[3].BaseData.Role}";

                // portrait
                if (_portrait4 != null)
                {
                    string path = $"{PortraitFolder}{enemies[3].BaseData.Code}.png";
                    if (ResourceLoader.Exists(path)) _portrait4.Texture = GD.Load<Texture2D>(path);
                }
            }
        }

        private void StartBattleWithEnemy(int index)
        {
            var enemy = Game.GeneratedEnemies[index];

            Game.InitializeBattle(enemy);

            Visible = false;

            _battleScreen.Visible = true;
            _battleScreen.Init(Game.BattleSystem);
        }

        private void OnBattleButton1Pressed()
        {
            StartBattleWithEnemy(0);
        }

        private void OnBattleButton2Pressed()
        {
            StartBattleWithEnemy(1);
        }

        private void OnBattleButton3Pressed()
        {
            StartBattleWithEnemy(2);
        }

        private void OnBattleButton4Pressed()
        {
            StartBattleWithEnemy(3);
        }

        private void OnBackPressed()
        {
            BackPressed?.Invoke();
        }
    }
}
