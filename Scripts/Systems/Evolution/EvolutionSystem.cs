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
            var evolutions = DatabaseManager.Instance
                .GetEvolutionsFrom(digimon.BaseData.Id);

            if (evolutions == null || evolutions.Count == 0)
            {
                return EvolutionAttemptResult.NotEligible();
            }

            var validEvolutions = new List<EvolutionData>();

            foreach (var evo in evolutions)
            {
                if (digimon.IsEvolutionBlocked(evo.ToDigimonId))
                    continue;

                if (MeetsRequirements(digimon, evo))
                {
                    validEvolutions.Add(evo);
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
                return EvolutionAttemptResult.BlockedByCapacity(
                    newForm,
                    requiredCapacity,
                    projectedUsed - capacityLimit
                );
            }

            digimon.Evolve(newForm, bestEvolution.StatMultiplier);

            return EvolutionAttemptResult.Evolved(newForm, requiredCapacity);
        }

        private static bool MeetsRequirements(DigimonInstance digimon, EvolutionData evolution)
        {
            var req = evolution.RequiredStats;

            if (digimon.Level < evolution.RequiredLevel)
                return false;

            if (digimon.AgeInDays < evolution.RequiredAgeInDays)
                return false;

            if (req == null)
            {
                GD.PrintErr($"Evolução sem requerimentos: {evolution.ToDigimonId}");
                return false;
            }

            if (digimon.CurrentStats.HealthPoints < req.HealthPoints)
                return false;

            if (digimon.CurrentStats.PhysicalDamage < req.PhysicalDamage)
                return false;

            if (digimon.CurrentStats.PhysicalDefense < req.PhysicalDefense)
                return false;

            if (digimon.CurrentStats.SpecialDamage < req.SpecialDamage)
                return false;

            if (digimon.CurrentStats.SpecialDefense < req.SpecialDefense)
                return false;

            if (digimon.CurrentStats.Speed < req.Speed)
                return false;

            if (digimon.BattlesFought < evolution.RequiredBattles)
                return false;

            if (evolution.RequiredWinRate > 0 && digimon.WinRate < evolution.RequiredWinRate)
                return false;

            if (digimon.Discipline < evolution.MinDiscipline)
                return false;

            if (digimon.Discipline > evolution.MaxDiscipline)
                return false;

            if (digimon.Happiness < evolution.MinHappiness)
                return false;

            if (digimon.Happiness > evolution.MaxHappiness)
                return false;

            return true;
        }

        public static int CalculateEvolutionScore(EvolutionData evolution)
        {
            var stats = evolution.RequiredStats;

            int score = evolution.RequiredLevel + evolution.RequiredAgeInDays;

            if (stats != null)
            {
                score +=
                    stats.HealthPoints +
                    stats.PhysicalDamage +
                    stats.PhysicalDefense +
                    stats.SpecialDamage +
                    stats.SpecialDefense +
                    stats.Speed;
            }

            // Requisitos de batalhas/win-rate/disciplina/felicidade também contam pro placar -
            // entre duas evoluções simultaneamente válidas (ex.: uma delas era bloqueada até
            // agora), a mais exigente continua vencendo o desempate em SelectBestEvolution.
            score += evolution.RequiredBattles;
            score += (int)(evolution.RequiredWinRate * 100);
            score += evolution.MinDiscipline;
            score += 100 - evolution.MaxDiscipline;
            score += evolution.MinHappiness;
            score += 100 - evolution.MaxHappiness;

            return score;
        }

        public static EvolutionData SelectBestEvolution(List<EvolutionData> evolutions, DigimonInstance digimon)
        {
            var scored = new List<(EvolutionData evo, int score)>();

            foreach(var evo in evolutions)
            {
                int score = CalculateEvolutionScore(evo);
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
