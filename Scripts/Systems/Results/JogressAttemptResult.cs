using ProjetoDC.Enums;
using ProjetoDC.Scripts.Data;
using ProjetoDC.Scripts.Gameplay;

namespace ProjetoDC.Scripts.Systems.Results
{
    /// <summary>Resultado de uma tentativa de fusão Jogress (JogressSystem.TryToFuse).
    /// Diferente de EvolutionAttemptResult (que muta um Digimon já existente),
    /// FusedDigimon carrega a instância nova já construída quando Outcome == Fused -
    /// GameManager.TryToFuse é quem efetivamente remove os dois de origem e adiciona
    /// ela no Center.</summary>
    public class JogressAttemptResult
    {
        public JogressOutcome Outcome { get; set; }

        /// <summary>Forma resultante da fusão (Fused) ou que resultaria se houvesse
        /// capacidade (BlockedByCapacity).</summary>
        public DigimonData TargetForm { get; set; }

        /// <summary>Capacidade total que a forma fundida ocupa no Center.</summary>
        public int RequiredCapacity { get; set; }

        /// <summary>Quanto falta de capacidade livre pra essa fusão acontecer
        /// (só relevante em BlockedByCapacity).</summary>
        public int CapacityDeficit { get; set; }

        /// <summary>Instância nova já pronta, só preenchida quando Outcome == Fused.</summary>
        public DigimonInstance FusedDigimon { get; set; }

        public static JogressAttemptResult NotEligible() =>
            new() { Outcome = JogressOutcome.NotEligible };

        public static JogressAttemptResult Fused(
            DigimonData targetForm,
            int requiredCapacity,
            DigimonInstance fusedDigimon) =>
            new()
            {
                Outcome = JogressOutcome.Fused,
                TargetForm = targetForm,
                RequiredCapacity = requiredCapacity,
                FusedDigimon = fusedDigimon
            };

        public static JogressAttemptResult BlockedByCapacity(
            DigimonData targetForm,
            int requiredCapacity,
            int capacityDeficit) =>
            new()
            {
                Outcome = JogressOutcome.BlockedByCapacity,
                TargetForm = targetForm,
                RequiredCapacity = requiredCapacity,
                CapacityDeficit = capacityDeficit
            };
    }
}
