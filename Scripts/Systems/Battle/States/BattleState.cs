using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjetoDC.Scripts.Systems.Battle.States
{
    public abstract class BattleState
    {
        protected BattleDigimon Digimon;

        public BattleState(BattleDigimon digimon)
        {
            Digimon = digimon;
        }

        public virtual Task Enter()
        {
            return Task.CompletedTask;
        }

        public virtual void Exit()
        {
        }
    }
}
