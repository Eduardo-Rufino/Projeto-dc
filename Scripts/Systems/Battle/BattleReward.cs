using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjetoDC.Scripts.Systems.Battle
{
    public class BattleReward
    {
        public int Experience { get; set; }
        public int Bits { get; set; }

        /// <summary>Capacidade extra do Center - só maior que 0 na prévia de vitória de um
        /// campeonato ainda não vencido antes (ver BattleArena e GameManager.ActiveTournament).</summary>
        public int CapacityGained { get; set; }

        /// <summary>Bits de bônus de campeonato (TournamentData.BitsReward), à parte dos Bits
        /// normais da batalha em Bits - mesma regra do CapacityGained (só na primeira vitória).</summary>
        public int BonusBitsGained { get; set; }
    }
}
