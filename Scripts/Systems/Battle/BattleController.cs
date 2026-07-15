using Godot;
using ProjetoDC.Enums;
using ProjetoDC.Scripts.Gameplay;
using System;
using System.Threading.Tasks;

namespace ProjetoDC.Scripts.Systems.Battle
{
    public partial class BattleController : Node
    {
        private BattleSystem _battle;

        private bool _running;

        public BattleSystem Battle => _battle;

        public event Action<BattleResult> BattleFinished;
        public event Action<DigimonInstance> AttackStarted;
        public event Action<DigimonInstance> DamageReceived;

        public void Init(BattleSystem battle)
        {
            _battle = battle;
        }

        /*
        public async Task ExecuteBattle()
        {
            if (_battle == null)
                return;

            _running = true;

            while (_running)
            {
                var result = _battle.ExecuteTurn();

                await Wait(800);

                if (!_running)
                    return;

                if (result != BattleResult.Ongoing)
                {
                    BattleFinished?.Invoke(result);
                    _running = false;
                    return;
                }

                await Wait(500);
            }
        }
        */

        public void StartBattle()
        {
            _running = true;
            ExecuteNextTurn();
        }

        public void StopBattle()
        {
            _running = false;
            _battle?.StopBattle();
        }

        private async Task Wait(int ms)
        {
            await ToSignal(
                GetTree().CreateTimer(ms / 1000.0),
                SceneTreeTimer.SignalName.Timeout);
        }

        private async Task PlayerTurn()
        {
            AttackStarted?.Invoke(_battle.Player);

            await Wait(500);

            _battle.PlayerAttack();

            DamageReceived?.Invoke(_battle.Enemy);

            await Wait(500);
        }

        private async Task EnemyTurn()
        {
            AttackStarted?.Invoke(_battle.Enemy);

            await Wait(500);

            _battle.EnemyAttack();

            DamageReceived?.Invoke(_battle.Player);

            await Wait(500);
        }

        private void EndBattle()
        {
            _running = false;

            var result = _battle.CheckBattleStatus();

            BattleFinished?.Invoke(result);
        }

        private async Task ExecuteNextTurn()
        {
            if (!_running)
                return;

            if (_battle.PlayerHasTurn())
            {
                await PlayerTurn();

                if (_battle.CheckBattleStatus() != BattleResult.Ongoing)
                {
                    EndBattle();
                    return;
                }

                await EnemyTurn();
            }
            else
            {
                await EnemyTurn();

                if (_battle.CheckBattleStatus() != BattleResult.Ongoing)
                {
                    EndBattle();
                    return;
                }

                await PlayerTurn();
            }

            ExecuteNextTurn();
        }
    }
}