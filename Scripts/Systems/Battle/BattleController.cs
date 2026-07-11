using Godot;
using ProjetoDC.Enums;
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

        public async void ExecuteNextTurn()
        {
            if (!_running)
                return;

            var result = _battle.ExecuteTurn();

            if(result != BattleResult.Ongoing)
            {
                _running = false;
                BattleFinished?.Invoke(result);
                return;
            }

            await Wait(800);

            ExecuteNextTurn();
        }
    }
}