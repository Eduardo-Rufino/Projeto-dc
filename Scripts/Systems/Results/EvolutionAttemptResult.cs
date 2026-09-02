using ProjetoDC.Enums;
using ProjetoDC.Scripts.Data;

namespace ProjetoDC.Scripts.Systems.Results
{
    /// <summary>Resultado de uma tentativa de evolução (EvolutionSystem.TryToEvolve).
    /// TargetForm/RequiredCapacity/CapacityDeficit só são preenchidos quando havia uma
    /// evolução válida encontrada (Evolved ou BlockedByCapacity) - em NotEligible ficam
    /// nos valores padrão.</summary>
    public class EvolutionAttemptResult
    {
        public EvolutionOutcome Outcome { get; set; }

        /// <summary>Forma pra qual o Digimon evoluiu (Evolved) ou evoluiria se houvesse
        /// capacidade (BlockedByCapacity).</summary>
        public DigimonData TargetForm { get; set; }

        /// <summary>Capacidade total que a nova forma ocupa no Center.</summary>
        public int RequiredCapacity { get; set; }

        /// <summary>Quanto falta de capacidade livre pra essa evolução acontecer
        /// (só relevante em BlockedByCapacity).</summary>
        public int CapacityDeficit { get; set; }

        public static EvolutionAttemptResult NotEligible() =>
            new() { Outcome = EvolutionOutcome.NotEligible };

        public static EvolutionAttemptResult Evolved(DigimonData targetForm, int requiredCapacity) =>
            new()
            {
                Outcome = EvolutionOutcome.Evolved,
                TargetForm = targetForm,
                RequiredCapacity = requiredCapacity
            };

        public static EvolutionAttemptResult BlockedByCapacity(
            DigimonData targetForm,
            int requiredCapacity,
            int capacityDeficit) =>
            new()
            {
                Outcome = EvolutionOutcome.BlockedByCapacity,
                TargetForm = targetForm,
                RequiredCapacity = requiredCapacity,
                CapacityDeficit = capacityDeficit
            };
    }
}
