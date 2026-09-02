using Godot;
using ProjetoDC.Enums;
using ProjetoDC.Scripts.Data;
using ProjetoDC.Scripts.Gameplay;
using ProjetoDC.Scripts.Managers;
using ProjetoDC.Scripts.Systems.Results;
using ProjetoDC.Scripts.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace ProjetoDC.Scripts.Systems.Evolution
{
    internal class EvolutionSystem
    {
        private static readonly Random _random = new Random();


        /// <summary>
        /// Tenta evoluir o Digimon. <paramref name="capacityUsed"/>/<paramref name="capacityLimit"/>
        /// vêm do Center (CenterState.CapacityUsed/CapacityLimit) - uma evolução só é aplicada
        /// se a capacidade extra que a nova forma ocupa (em relação ao estágio atual) couber;
        /// senão retorna BlockedByCapacity sem alterar o Digimon, pra quem chamou poder avisar
        /// o jogador (ver GameManager.TryToEvolve/EvolutionBlockedByCapacity).
        /// </summary>
        public static EvolutionAttemptResult TryToEvolve(
            DigimonInstance digimon,
            int capacityUsed,
            int capacityLimit)
        {
            GD.Print($"Tentando evoluir {digimon.BaseData.Name}");
            var evolutions = DatabaseManager.Instance
                .GetEvolutionsFrom(digimon.BaseData.Id);

            GD.Print($"Evoluções encontradas: {evolutions?.Count ?? 0}");

            if (evolutions == null || evolutions.Count == 0)
            {
                return EvolutionAttemptResult.NotEligible();
            }

            var validEvolutions = new List<EvolutionData>();

            foreach (var evo in evolutions)
            {
                GD.Print($"Testando evolução para {evo.ToDigimonId}");

                if (MeetsRequirements(digimon, evo))
                {
                    GD.Print("Requisitos atendidos.");
                    validEvolutions.Add(evo);
                }
                else
                {
                    GD.Print("Requisitos NÃO atendidos.");
                }
            }

            if (validEvolutions.Count == 0)
            {
                return EvolutionAttemptResult.NotEligible();
            }

            var bestEvolution = SelectBestEvolution(validEvolutions, digimon);

            var newForm = DatabaseManager.Instance.GetDigimon(bestEvolution.ToDigimonId);

            if(newForm == null)
            {
                return EvolutionAttemptResult.NotEligible();
            }

            int requiredCapacity = DigimonInstance.GetCapacityCostForStage(newForm.Stage);
            int capacityDelta = requiredCapacity - digimon.CapacityCost;
            int projectedUsed = capacityUsed + capacityDelta;

            if (projectedUsed > capacityLimit)
            {
                GD.Print(
                    $"{digimon.BaseData.Name} pronto pra evoluir pra {newForm.Name}, " +
                    $"mas falta capacidade ({projectedUsed}/{capacityLimit})."
                );

                return EvolutionAttemptResult.BlockedByCapacity(
                    newForm,
                    requiredCapacity,
                    projectedUsed - capacityLimit
                );
            }

            GD.Print($"Evoluindo {digimon.BaseData.Name} -> {newForm.Name}");

            digimon.Evolve(newForm, bestEvolution.StatMultiplier);

            return EvolutionAttemptResult.Evolved(newForm, requiredCapacity);
        }

        private static bool MeetsRequirements(DigimonInstance digimon, EvolutionData evolution)
        {
            var req = evolution.RequiredStats;

            GD.Print(evolution.RequiredStats == null);

            if (digimon.Level < evolution.RequiredLevel)
            {
                GD.Print($"Level insuficiente: {digimon.Level}/{evolution.RequiredLevel}");
                return false;
            }

            if (digimon.AgeInDays < evolution.RequiredAgeInDays)
            {
                GD.Print($"Idade insuficiente: {digimon.AgeInDays}/{evolution.RequiredAgeInDays}");
                return false;
            }

            if (req == null)
            {
                GD.PrintErr($"Evolução sem requerimentos: {evolution.ToDigimonId}");
                return false;
            }
                

            if (digimon.CurrentStats.HealthPoints < req.HealthPoints)
            {
                GD.Print($"HP insuficiente: {digimon.MaxHealthPoints}/{evolution.RequiredStats.HealthPoints}");
                return false;
            }

            if (digimon.CurrentStats.PhysicalDamage < req.PhysicalDamage)
            {
                GD.Print($"Ataque fisico insuficiente: {digimon.CurrentStats.PhysicalDamage}/{evolution.RequiredStats.PhysicalDamage}");
                return false;
            }

            if (digimon.CurrentStats.PhysicalDefense < req.PhysicalDefense)
            {
                GD.Print($"Defesa fisica insuficiente: {digimon.CurrentStats.PhysicalDefense}/{evolution.RequiredStats.PhysicalDefense}");
                return false;
            }

            if (digimon.CurrentStats.SpecialDamage < req.SpecialDamage)
                return false;

            if (digimon.CurrentStats.SpecialDefense < req.SpecialDefense)
                return false;

            if (digimon.CurrentStats.Speed < req.Speed)
            {
                GD.Print($"Velocidade insuficiente: {digimon.CurrentStats.Speed}/{evolution.RequiredStats.Speed}");
                return false;
            }

            return true;
        }

        public static int CalculateEvolutionScore(EvolutionData evolution)
        {
            var stats = evolution.RequiredStats;

            if (stats == null)
                return evolution.RequiredLevel + evolution.RequiredAgeInDays;

            return 
                evolution.RequiredLevel +
                evolution.RequiredAgeInDays +
                stats.HealthPoints +
                stats.PhysicalDamage +
                stats.PhysicalDefense +
                stats.SpecialDamage +
                stats.SpecialDefense +
                stats.Speed;
        }

        public static EvolutionData SelectBestEvolution(List<EvolutionData> evolutions, DigimonInstance digimon)
        {
            var scored = new List<(EvolutionData evo, int score)>();

            foreach(var evo in evolutions)
            {
                int score = CalculateEvolutionScore(evo);
                GD.Print($"Evo {evo.ToDigimonId} score = {score}");
                scored.Add((evo, score));
            }

            int maxScore = scored.Max(x => x.score);

            var best = scored.Where(x => x.score == maxScore)
                .Select(x => x.evo)
                .ToList();

            if (best.Count == 1)
                return best[0];

            //empate -> escolha aleatoria entre os empatados

            return best[_random.Next(best.Count)];
        }
    }
}
