using ProjetoDC.Scripts.Gameplay;
using System.Collections.Generic;
using System.Linq;

namespace ProjetoDC.Scripts.Systems.Battle
{
    /// <summary>
    /// Um dos dois lados de uma <see cref="BattleMatch"/> — até 3 <see cref="BattleCombatant"/>.
    /// </summary>
    public class BattleTeam
    {
        public List<BattleCombatant> Members { get; }

        public BattleTeam(IEnumerable<DigimonInstance> digimons)
        {
            Members = digimons.Select(d => new BattleCombatant(d)).ToList();
        }

        public List<BattleCombatant> AliveMembers =>
            Members.Where(m => !m.IsDead).ToList();

        public bool IsDefeated =>
            Members.All(m => m.IsDead);

        public void Tick(double delta)
        {
            foreach (var member in Members)
            {
                member.Tick(delta);
            }
        }
    }
}
