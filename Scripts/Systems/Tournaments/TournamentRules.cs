using ProjetoDC.Enums;
using ProjetoDC.Scripts.Data;
using ProjetoDC.Scripts.Gameplay;
using System.Collections.Generic;
using System.Linq;

namespace ProjetoDC.Scripts.Systems.Tournaments
{
    /// <summary>Resultado de TournamentRules.ValidateTeam: IsValid e, pra cada Digimon fora da regra,
    /// o motivo (ex.: "Estágio Champion não permitido").</summary>
    public sealed class TeamValidation
    {
        public List<(DigimonInstance Digimon, string Reason)> Invalid { get; } = new();

        public bool IsValid => Invalid.Count == 0;
    }

    /// <summary>
    /// Regras de inscrição dos campeonatos (TournamentData.AllowedStages/AllowedAttributes): quem pode
    /// entrar. Lista vazia = sem restrição naquele critério. Funções puras - quem aplica é
    /// GameManager.StartTournamentBattle e a TeamSelectionScreen.
    /// </summary>
    public static class TournamentRules
    {
        public static bool HasRules(TournamentData tournament) =>
            tournament.AllowedStages.Count > 0 || tournament.AllowedAttributes.Count > 0;

        /// <summary>Motivo de a espécie não poder entrar no campeonato, ou null se pode.</summary>
        public static string GetIneligibilityReason(TournamentData tournament, DigimonData species)
        {
            if (tournament.AllowedStages.Count > 0 && !tournament.AllowedStages.Any(s => StageMatches(s, species.Stage)))
                return $"Estágio {species.Stage} não permitido";

            if (tournament.AllowedAttributes.Count > 0 && !tournament.AllowedAttributes.Contains(species.Attribute))
                return $"Atributo {AttributeName(species.Attribute)} não permitido";

            return null;
        }

        // "Só Mega" também aceita MegaPlus (Omegamon, Examon - resultados de Jogress): chegar mais longe
        // não pode tirar o Digimon dos campeonatos mais difíceis.
        private static bool StageMatches(DigimonStage allowed, DigimonStage stage) =>
            allowed == stage || (allowed == DigimonStage.Mega && stage == DigimonStage.MegaPlus);

        public static bool IsEligible(TournamentData tournament, DigimonData species) =>
            GetIneligibilityReason(tournament, species) == null;

        public static TeamValidation ValidateTeam(TournamentData tournament, IEnumerable<DigimonInstance> team)
        {
            var result = new TeamValidation();

            foreach (var digimon in team)
            {
                string reason = GetIneligibilityReason(tournament, digimon.BaseData);

                if (reason != null)
                    result.Invalid.Add((digimon, reason));
            }

            return result;
        }

        /// <summary>Texto curto das regras pro card do campeonato ("Só Rookie", "Só Vírus"), ou
        /// "Sem restrição".</summary>
        public static string Describe(TournamentData tournament)
        {
            if (!HasRules(tournament))
                return "Sem restrição";

            var parts = new List<string>();

            if (tournament.AllowedStages.Count > 0)
                parts.Add(string.Join("/", tournament.AllowedStages));

            if (tournament.AllowedAttributes.Count > 0)
                parts.Add(string.Join("/", tournament.AllowedAttributes.Select(AttributeName)));

            return "Só " + string.Join(" ", parts);
        }

        public static string AttributeName(DigimonAttribute attribute) => attribute switch
        {
            DigimonAttribute.Vacine => "Vacina",
            DigimonAttribute.Virus => "Vírus",
            DigimonAttribute.Data => "Data",
            DigimonAttribute.Free => "Livre",
            _ => "Desconhecido",
        };
    }
}
