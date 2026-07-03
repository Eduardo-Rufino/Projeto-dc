using Godot;
using ProjetoDC.Scripts.Gameplay;
using ProjetoDC.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjetoDC.Scripts.Systems.Battle
{
    public class BattleSystem
    {
        private bool _battleEnded = false;

        private DigimonInstance _player;
        private DigimonInstance _enemy;

        Random _random = new Random();

        public BattleSystem(DigimonInstance player, DigimonInstance enemy)
        {
            _player = player;
            _enemy = enemy;
        }

        private void Attack(DigimonInstance attacker, DigimonInstance defender)
        {
            int damage = attacker.CurrentStats.PhysicalDamage
                - defender.CurrentStats.PhysicalDefense;

            if (damage < 1)
                damage = 1;

            damage = ApplyDamageVariance(damage);
            damage = ApplyCritical(damage);

            defender.CurrentHealthPoints -= damage;

            if (defender.CurrentHealthPoints < 0)
                defender.CurrentHealthPoints = 0;

            GD.Print($"{attacker.BaseData.Name} causou {damage} de dano em {defender.BaseData.Name}");
        }

        private BattleResult CheckBattleEnd()
        {
            if (_enemy.CurrentHealthPoints <= 0)
            {
                _battleEnded = true;
                return BattleResult.PlayerWon;
            }

            if (_player.CurrentHealthPoints <= 0)
            {
                _battleEnded = true;
                return BattleResult.EnemyWon;
            }

            return BattleResult.Ongoing;
        }

        private int ApplyDamageVariance(int damage)
        {
            float variation = (_random.Next(-10, 11)) / 100f; //-10 a +10
            damage += (int)(damage * variation);

            return Math.Max(1, damage);
        }

        private int ApplyCritical(int damage)
        {
            int roll = _random.Next(1, 101); //1 a 100

            if(roll <= 10)
            {
                GD.Print("CRITICO!");
                return damage * 2;
            }

            return damage;
        }
        
        public BattleResult ExecuteTurn()
        {
            if (_battleEnded)
                return BattleResult.Ongoing;

            if (_player.CurrentStats.Speed >= _enemy.CurrentStats.Speed)
            {
                Attack(_player, _enemy);

                BattleResult result = CheckBattleEnd();

                if (result != BattleResult.Ongoing)
                    return result;

                Attack(_enemy, _player);

                result = CheckBattleEnd();

                if (result != BattleResult.Ongoing)
                    return result;
            }
            else
            {
                Attack(_enemy, _player);

                BattleResult result = CheckBattleEnd();

                if (result != BattleResult.Ongoing)
                    return result;

                Attack(_player, _enemy);

                result = CheckBattleEnd();

                if (result != BattleResult.Ongoing)
                    return result;
            }

            if(_enemy.CurrentHealthPoints <= 0)
            {
                _player.GainExperience(500);
            }
            return BattleResult.Ongoing;
        }
        
        

    }
}
