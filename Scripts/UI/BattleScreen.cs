using Godot;
using ProjetoDC.Enums;
using ProjetoDC.Scripts.Gameplay;
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

        private DigimonSprite _playerSprite;
        private DigimonSprite _enemySprite;
        private BattleResultScreen _battleResultScreen;

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

            _controller.AttackStarted += OnAttackStarted;
            _controller.DamageReceived += OnDamageReceived;

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
                    _playerSprite.SetDigimon(player.BaseData.Code);
                    _playerSprite.SetFlip(true);
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
                    _enemySprite.SetDigimon(enemy.BaseData.Code);
                    _enemySprite.SetFlip(false);
                }
            }
        }

        private void InitializeUIComponents()
        {
            var basePath = "MarginContainer/VBoxContainer/";

            // Nó de sprites
            _playerSprite = GetNodeOrNull<DigimonSprite>($"{basePath}HBoxContainer/VBoxContainer/PlayerDigimonDisplay/DigimonDisplay/DigimonSprite");
            _enemySprite = GetNodeOrNull<DigimonSprite>($"{basePath}HBoxContainer/VBoxContainer2/EnemyDigimonDisplay/DigimonDisplay/DigimonSprite");

            // Labels principais
            _playerNameLabel = GetNodeOrNull<Label>($"{basePath}HBoxContainer/VBoxContainer/PlayerNameLabel");
            _enemyNameLabel = GetNodeOrNull<Label>($"{basePath}HBoxContainer/VBoxContainer2/EnemyNameLabel");
            _playerHpLabel = GetNodeOrNull<Label>($"{basePath}HBoxContainer/VBoxContainer/PlayerHpLabel");
            _enemyHpLabel = GetNodeOrNull<Label>($"{basePath}HBoxContainer/VBoxContainer2/EnemyHpLabel");

            // ProgressBars
            _playerHpProgressBar = GetNodeOrNull<ProgressBar>($"{basePath}HBoxContainer/VBoxContainer/PlayerHpProgressBar");
            _enemyHpProgressBar = GetNodeOrNull<ProgressBar>($"{basePath}HBoxContainer/VBoxContainer2/EnemyHpProgressBar");

            _battleResultScreen = GetNodeOrNull<BattleResultScreen>("BattleResultScreen");
            GD.Print($"BattleScreen Size: {Size}");
            GD.Print($"BattleResult Size: {_battleResultScreen.Size}");
            if (_battleResultScreen != null)
            {
                _battleResultScreen.OkPressed += OnBattleResultOkPressed;
            }
            else
            {
                GD.PrintErr("BattleResultScreen não encontrado!");
            }
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

        private void OnAttackStarted(DigimonInstance digimon)
        {
            if (digimon == _battle.Player)
            {
                _playerSprite.PlayAttack();
            }
            else if (digimon == _battle.Enemy)
            {
                _enemySprite.PlayAttack();
            }
        }

        private void OnDamageReceived(DigimonInstance digimon)
        {
            if (digimon == _battle.Player)
            {
                _playerSprite.PlayHit();
            }
            else if (digimon == _battle.Enemy)
            {
                _enemySprite.PlayHit();
            }
        }

        private async void OnBattleFinished(BattleResult result)
        {
            switch (result)
            {
                case BattleResult.PlayerWon:

                    _enemySprite.PlayDeath();
                    _playerSprite.PlayVictory();

                    GameManager.Instance.ApplyBattleReward(result);
                    break;

                case BattleResult.EnemyWon:

                    _playerSprite.PlayDeath();
                    _enemySprite.PlayVictory();

                    GameManager.Instance.ApplyBattleReward(result);
                    break;
            }

            await ToSignal(
                GetTree().CreateTimer(1.5f),
                SceneTreeTimer.SignalName.Timeout);

            _battleResultScreen.ShowResult(result);
        }

        public void StopBattle()
        {
            _controller.StopBattle();
        }

        private void OnBattleResultOkPressed()
        {
            BackPressed?.Invoke();
        }
    }
}
