using Godot;
using ProjetoDC.Scripts.Gameplay;
using ProjetoDC.Scripts.Managers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjetoDC.Scripts.Systems.Battle
{
    public class EnemyGenerator
    {
        private readonly Random _random = new();

        public List<DigimonInstance> GenerateEnemies(DigimonInstance player, int amount)
        {
            var enemies = new List<DigimonInstance>();

            try
            {
                var candidates = DatabaseManager.Instance
                    .GetAllDigimons()
                    .Where(x => x.Stage == player.BaseData.Stage)
                    .OrderBy(x => _random.Next())
                    .Take(amount);

                foreach (var data in candidates)
                {
                    var enemy = new DigimonInstance(data);

                    int minLevel = Math.Max(1, player.Level - 2);
                    int maxLevel = player.Level + 2;

                    int targetLevel = _random.Next(minLevel, maxLevel + 1);

                    while (enemy.Level < targetLevel)
                    {
                        enemy.GainExperience(enemy.ExperienceToNextLevel);
                    }

                    enemy.RestoreHealth();

                    enemies.Add(enemy);
                }

            }
            catch(Exception ex)
            {
                GD.PrintErr(ex.Message);
            }

            return enemies;
        }
    }
}
