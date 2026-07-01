using Godot;
using ProjetoDC.Scripts.Data;
using ProjetoDC.Scripts.Gameplay;
using ProjetoDC.Scripts.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjetoDC.Scripts.Systems
{
    internal class EvolutionSystem
    {

        public static bool TryToEvolve(DigimonInstance digimon)
        {
            var evolutions = DatabaseManager.Instance
                .GetEvolutionsFrom(digimon.BaseData.Id);

            if(evolutions == null || evolutions.Count == 0)
            {
                return false;
            }

            var validEvolutions = new List<EvolutionData>();

            foreach (var evo in evolutions)
            {
                if (MeetsRequirements(digimon, evo))
                {
                    validEvolutions.Add(evo);
                }
            }

            if (validEvolutions.Count == 0)
            {
                return false;
            }

            var bestEvolution = validEvolutions
                .OrderByDescending(e => e.Priority)
                .First();

            var newForm = DatabaseManager.Instance.GetDigimon(bestEvolution.ToDigimonId);

            if(newForm == null)
            {
                return false;
            }

            GD.Print($"Evoluindo {digimon.BaseData.Name} -> {newForm.Name}");

            digimon.Evolve(newForm, bestEvolution.StatMultiplier);

            return true;
        }

        private static bool MeetsRequirements(
            DigimonInstance digimon,
            EvolutionData evolution)
        {
            if (digimon.Level < evolution.RequiredLevel)
                return false;

            if (digimon.AgeInDays < evolution.RequiredAgeInDays)
                return false;

            return true;
        }

    }
}
