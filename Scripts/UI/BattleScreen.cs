using Godot;
using ProjetoDC.Enums;
using ProjetoDC.Scripts.Managers;
using ProjetoDC.Scripts.Systems.Battle;
using System;
using System.Threading.Tasks;

namespace ProjetoDC.Scripts.UI
{
    public partial class BattleScreen : Control
    {
        private const string SpriteFolder = "res://Assets/Sprites/Digimon/";

        private Label _playerNameLabel;
        private Label _enemyNameLabel;
        private Label _playerHpLabel;
        private Label _enemyHpLabel;

        private ProgressBar _playerHpProgressBar;
        private ProgressBar _enemyHpProgressBar;

        private TextureRect _playerSprite;
        private TextureRect _enemySprite;

        public event Action BackPressed;

        private GameManager Game => GameManager.Instance;
        private BattleSystem _battle;
        private BattleController _controller;

        public override void _Ready()
        {
            InitializeUIComponents();
            GD.Print("BattleScreen iniciado.");
        }

        public void Init(BattleSystem battle)
        {
            _battle = battle;

            _battle.OnStateChanged += RefreshUI;

            _controller = new BattleController();
            AddChild(_controller);
            _controller.Init(_battle);

            _controller.BattleFinished += OnBattleFinished;

            RefreshUI();

            _ = StartBattle();
        }

        public override void _ExitTree()
        {
            if (_battle != null)
                _battle.OnStateChanged -= RefreshUI;

            if(_controller != null)
            {
                _controller.BattleFinished -= OnBattleFinished;
            }
        }

        public void RefreshUI()
        {
            if (_battle == null)
                return;

            // Atualizar player
            var player = _battle.Player;
            var enemy = _battle.Enemy;

            if (player != null)
            {
                if (_playerNameLabel != null) _playerNameLabel.Text = $"Nome: {player.BaseData.Name}";
                if (_playerHpLabel != null)
                {
                    _playerHpLabel.Text = $"HP: {player.CurrentHealthPoints}/{player.MaxHealthPoints}";
                    if (_playerHpProgressBar != null)
                    {
                        _playerHpProgressBar.MaxValue = player.MaxHealthPoints;
                        _playerHpProgressBar.Value = player.CurrentHealthPoints;
                    }
                }

                // portrait
                if (_playerSprite != null)
                {
                    string path = $"{SpriteFolder}{player.BaseData.Code}.png";
                    if (ResourceLoader.Exists(path))
                        _playerSprite.Texture = GD.Load<Texture2D>(path);
                    else
                        _playerSprite.Texture = null;
                }
            }
            if (enemy != null)
            {
                if (_enemyNameLabel != null) _enemyNameLabel.Text = $"Nome: {enemy.BaseData.Name}";
                if (_enemyHpLabel != null)
                {
                    _enemyHpLabel.Text = $"HP: {enemy.CurrentHealthPoints}/{enemy.MaxHealthPoints}";
                    if (_enemyHpProgressBar != null)
                    {
                        _enemyHpProgressBar.MaxValue = enemy.MaxHealthPoints;
                        _enemyHpProgressBar.Value = enemy.CurrentHealthPoints;
                    }
                }

                // portrait
                if (_enemySprite != null)
                {
                    string path = $"{SpriteFolder}{enemy.BaseData.Code}.png";

                    if (ResourceLoader.Exists(path))
                        _enemySprite.Texture = GD.Load<Texture2D>(path);
                    else
                        _enemySprite.Texture = null;
                }
            }
        }

        private void InitializeUIComponents()
        {
            var basePath = "MarginContainer/VBoxContainer/";

            // Nó de sprites
            _playerSprite = GetNodeOrNull<TextureRect>($"{basePath}PlayerSprite");
            _enemySprite = GetNodeOrNull<TextureRect>($"{basePath}EnemySprite");

            // Labels principais
            _playerNameLabel = GetNodeOrNull<Label>($"{basePath}PlayerNameLabel");
            _enemyNameLabel = GetNodeOrNull<Label>($"{basePath}EnemyNameLabel");
            _playerHpLabel = GetNodeOrNull<Label>($"{basePath}PlayerHpLabel");
            _enemyHpLabel = GetNodeOrNull<Label>($"{basePath}EnemyHpLabel");


            // ProgressBars
            _playerHpProgressBar = GetNodeOrNull<ProgressBar>($"{basePath}PlayerHpProgressBar");
            _enemyHpProgressBar = GetNodeOrNull<ProgressBar>($"{basePath}EnemyHpProgressBar");
        }

        private void OnBackPressed()
        {
            BackPressed?.Invoke();
        }

        public async Task StartBattle()
        {
            RefreshUI();

            _controller.StartBattle();
        }

        private void OnBattleFinished(BattleResult result)
        {
            switch (result)
            {
                case BattleResult.PlayerWon:
                    GD.Print("Vitória!");

                    GameManager.Instance.ApplyBattleReward(result);
                    break;

                case BattleResult.EnemyWon:
                    GD.Print("Derrota!");

                    GameManager.Instance.ApplyBattleReward(result);
                    break;
            }
        }

        public void StopBattle()
        {
            _controller.StopBattle();
        }
    }
}
