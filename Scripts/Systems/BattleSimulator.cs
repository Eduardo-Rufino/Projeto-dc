using Godot;
using ProjetoDC.Scripts.Gameplay;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjetoDC.Scripts.Systems
{
    internal class BattleSimulator
    {
        public static void Simulate(DigimonInstance a, DigimonInstance b)
        {
            GD.Print("Batalha iniciada!");
            GD.Print(a.BaseData.Name + " VS " + b.BaseData.Name);

            int turn = 1;

            while (!a.IsDead() && !b.IsDead())
            {
                GD.Print("------ Turno " + turn + " ------");

                //A ataca B
                int damageA = CalculateDamage(a, b);
                b.TakeDamage(damageA);
                GD.Print(a.BaseData.Name + " causa " + damageA + " de dano. HP inimigo: " + b.CurrentHealthPoints);

                if (b.IsDead())
                    break;

                //B ataca A
                int damageB = CalculateDamage(b, a);
                a.TakeDamage(damageB);
                GD.Print(b.BaseData.Name + " causa " + damageB + " de danmo. HP inimigo: " + a.CurrentHealthPoints);

                turn++;
            }

            GD.Print("==== FIM DA BATALHA ====");

            if (a.IsDead() && b.IsDead())
                GD.Print("Empate!");
            else if (a.IsDead())
                GD.Print(b.BaseData.Name + " venceu");
            else
                GD.Print(a.BaseData.Name + " venceu!");
        }

        private static int CalculateDamage(DigimonInstance attacker, DigimonInstance defender)
        {
            //versão simples
            int atk = attacker.BaseData.BaseStats.PhysicalDamage;
            int def = defender.BaseData.BaseStats.PhysicalDefense;

            int damage = atk - def / 2;

            if (damage < 1)
                damage = 1;

            return damage;

        }
    }
}
