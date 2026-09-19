using ProjetoDC.Enums;
using ProjetoDC.Scripts.Data;
using ProjetoDC.Scripts.Gameplay;
using ProjetoDC.Scripts.Managers;
using ProjetoDC.Scripts.Systems.Passives;
using ProjetoDC.Scripts.Systems.Results;

namespace ProjetoDC.Scripts.Systems.Jogress
{
    /// <summary>
    /// Fusão de dois Digimons existentes num Digimon novo (Jogress) - sistema separado
    /// de EvolutionSystem de propósito (mecânica diferente: consome dois indivíduos em
    /// vez de mutar um só). Sem nenhum requisito de nível/stats/batalhas/disciplina/
    /// felicidade - só precisa dos dois Digimons certos (ver JogressData.MatchesPair).
    /// </summary>
    public static class JogressSystem
    {
        /// <summary>
        /// Tenta fundir a/b. Não muta nenhum dos dois Digimons nem o Center - quem chamou
        /// (GameManager.TryToFuse) é responsável por remover a/b e adicionar
        /// FusedDigimon só quando o resultado for Fused. A checagem de capacidade
        /// acontece antes de qualquer coisa ser construída, pra uma tentativa bloqueada
        /// nunca arriscar perder os dois Digimons de origem.
        /// </summary>
        public static JogressAttemptResult TryToFuse(
            DigimonInstance a,
            DigimonInstance b,
            int capacityUsed,
            int capacityLimit)
        {
            if (a == null || b == null || a == b)
                return JogressAttemptResult.NotEligible();

            var recipe = DatabaseManager.Instance.GetJogressRecipe(a.BaseData.Id, b.BaseData.Id);

            if (recipe == null)
                return JogressAttemptResult.NotEligible();

            var resultForm = DatabaseManager.Instance.GetDigimon(recipe.ToDigimonId);

            if (resultForm == null)
                return JogressAttemptResult.NotEligible();

            int requiredCapacity = DigimonInstance.GetCapacityCostForStage(resultForm.Stage);
            int capacityDelta = requiredCapacity - (a.CapacityCost + b.CapacityCost);
            int projectedUsed = capacityUsed + capacityDelta;

            if (projectedUsed > capacityLimit)
            {
                return JogressAttemptResult.BlockedByCapacity(
                    resultForm,
                    requiredCapacity,
                    projectedUsed - capacityLimit
                );
            }

            // Instância nova, não DigimonInstance.Evolve num dos dois - um Digimon
            // fundido não é "um dos dois indivíduos subindo de forma", é um terceiro
            // Digimon (mesmo padrão de EggSystem.HatchEgg pra quem acabou de nascer).
            var fused = new DigimonInstance(resultForm)
            {
                PassiveId = PassiveSystem.RollRandomPassive(resultForm)
            };

            BlendStatsFromParents(fused, a, b, resultForm.BaseStats, recipe.StatMultiplier);

            return JogressAttemptResult.Fused(resultForm, requiredCapacity, fused);
        }

        /// <summary>
        /// Recompensa o treino acumulado dos dois pais em vez de descartá-lo: pega a MÉDIA
        /// do CurrentStats de a/b, multiplica pelo StatMultiplier da receita, e usa o mesmo
        /// DigimonInstance.BlendTowardSpecies que a evolução normal usa (nunca reduz o que
        /// já foi multiplicado, só puxa em direção à ficha da espécie resultante quem ficou
        /// pra trás). Full-heal no final, mesmo padrão de Evolve.
        /// </summary>
        private static void BlendStatsFromParents(
            DigimonInstance fused,
            DigimonInstance a,
            DigimonInstance b,
            BaseStats species,
            float multiplier)
        {
            fused.CurrentStats.HealthPoints = BlendAverage(
                a.CurrentStats.HealthPoints, b.CurrentStats.HealthPoints, multiplier, species.HealthPoints);

            fused.CurrentStats.PhysicalDamage = BlendAverage(
                a.CurrentStats.PhysicalDamage, b.CurrentStats.PhysicalDamage, multiplier, species.PhysicalDamage);

            fused.CurrentStats.PhysicalDefense = BlendAverage(
                a.CurrentStats.PhysicalDefense, b.CurrentStats.PhysicalDefense, multiplier, species.PhysicalDefense);

            fused.CurrentStats.SpecialDamage = BlendAverage(
                a.CurrentStats.SpecialDamage, b.CurrentStats.SpecialDamage, multiplier, species.SpecialDamage);

            fused.CurrentStats.SpecialDefense = BlendAverage(
                a.CurrentStats.SpecialDefense, b.CurrentStats.SpecialDefense, multiplier, species.SpecialDefense);

            fused.CurrentStats.Speed = BlendAverage(
                a.CurrentStats.Speed, b.CurrentStats.Speed, multiplier, species.Speed);

            fused.MaxHealthPoints = fused.CurrentStats.HealthPoints;
            fused.CurrentHealthPoints = fused.MaxHealthPoints;
        }

        private static int BlendAverage(int valueA, int valueB, float multiplier, int speciesValue)
        {
            int average = (valueA + valueB) / 2;

            return DigimonInstance.BlendTowardSpecies(average, multiplier, speciesValue);
        }
    }
}
