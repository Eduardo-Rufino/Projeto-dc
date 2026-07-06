using Godot;
using ProjetoDC.Enums;
using System;
using System.Threading.Tasks;

namespace ProjetoDC.Scripts.Systems.Battle
{
    public partial class BattleController : Node
    {
        private BattleSystem _battle;

        public BattleSystem Battle => _battle;

        public event Action<BattleResult> BattleFinished;

        public void Init(BattleSystem battle)
        {
            _battle = battle;
        }

        public async Task ExecuteBattle()
        {
            if (_battle == null)
            {
                GD.PrintErr("BattleSystem nulo no controller");
                return;
            }

            while (true)
            {
                var result = _battle.ExecuteTurn();

                await Wait(800);

                if (result != BattleResult.Ongoing)
                {
                    GD.Print($"Batalha terminou: {result}");

                    BattleFinished?.Invoke(result);

                    return;
                }

                await Wait(500);
            }
        }

        private async Task Wait(int ms)
        {
            await ToSignal(
                GetTree().CreateTimer(ms / 1000.0),
                SceneTreeTimer.SignalName.Timeout);
        }
    }
}