using System.Collections.Generic;

namespace ProjetoDC.Scripts.Data
{
    /// <summary>
    /// Formato bruto de Data/Evolution/evolution_web.json (a "teia evolutiva"). POCOs mutáveis
    /// só para desserialização, como o resto de Scripts/Data - Type e Source ficam como string
    /// aqui e são convertidos para enum por EvolutionDatabase, que reporta valor desconhecido
    /// com uma mensagem clara em vez de estourar na desserialização.
    /// </summary>
    public class EvolutionWebFile
    {
        public int Version { get; set; }

        public string Notes { get; set; }

        public List<string> Stages { get; set; } = new();

        /// <summary>Uma entrada por espécie - só usada para conferir consistência, a fonte da
        /// verdade é <see cref="Edges"/>.</summary>
        public List<EvolutionWebDigimon> Digimon { get; set; } = new();

        public List<EvolutionWebDnaRecipe> Dna { get; set; } = new();

        public List<EvolutionWebEdge> Edges { get; set; } = new();
    }

    public class EvolutionWebDigimon
    {
        public string Code { get; set; }

        public string Name { get; set; }

        public string Stage { get; set; }

        public List<string> EvolvesFrom { get; set; } = new();

        public List<EvolutionWebTarget> EvolvesTo { get; set; } = new();
    }

    public class EvolutionWebTarget
    {
        public string To { get; set; }

        public string Type { get; set; }

        public string Source { get; set; }
    }

    public class EvolutionWebEdge
    {
        public string From { get; set; }

        public string To { get; set; }

        public string Type { get; set; }

        public string Source { get; set; }
    }

    public class EvolutionWebDnaRecipe
    {
        public string Result { get; set; }

        public List<string> Partners { get; set; } = new();
    }
}
