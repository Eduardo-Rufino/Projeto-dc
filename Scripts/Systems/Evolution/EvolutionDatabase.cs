using Godot;
using ProjetoDC.Enums;
using ProjetoDC.Scripts.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace ProjetoDC.Scripts.Systems.Evolution
{
    /// <summary>Uma ligação da teia evolutiva, identificada por code de espécie (o mesmo
    /// campo <c>code</c> de Data/Digimon/*.json).</summary>
    public sealed class EvolutionLink
    {
        public string From { get; }
        public string To { get; }
        public EvolutionLinkType Type { get; }
        public EvolutionLinkSource Source { get; }

        public EvolutionLink(string from, string to, EvolutionLinkType type, EvolutionLinkSource source)
        {
            From = from;
            To = to;
            Type = type;
            Source = source;
        }
    }

    /// <summary>
    /// Registro da teia evolutiva (Data/Evolution/evolution_web.json): responde "para o que
    /// esse code pode evoluir", "de onde ele vem" e "quais os parceiros de DNA de um
    /// resultado". Montado a partir de <c>edges[]</c> (fonte da verdade); <c>digimon[]</c> só é
    /// usado para conferir consistência.
    ///
    /// É só consulta: NÃO decide destino nem checa requisitos - quem evolui de fato continua
    /// sendo o <c>EvolutionSystem</c> (requisitos por ligação em Data/Evolutions/) e o
    /// <c>JogressSystem</c>. Ligações do tipo <see cref="EvolutionLinkType.Dna"/> aparecem em
    /// <see cref="GetEvolutionsFrom"/> de cada parceiro - quem consulta precisa checar o tipo e
    /// exigir os dois parceiros (<see cref="GetDnaPartners"/>).
    ///
    /// Erros de validação vão para <c>GD.PushError</c> e ficam em <see cref="ValidationErrors"/>;
    /// a ligação problemática é ignorada, o resto da teia continua carregado.
    /// </summary>
    public sealed class EvolutionDatabase
    {
        // Ordem dos estágios "de linha" - "próximo estágio" é o seguinte nesta lista.
        private static readonly DigimonStage[] StageOrder =
        {
            DigimonStage.Baby,
            DigimonStage.InTraining,
            DigimonStage.Rookie,
            DigimonStage.Champion,
            DigimonStage.Ultimate,
            DigimonStage.Mega,
            DigimonStage.MegaPlus,
        };

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private static readonly IReadOnlyList<EvolutionLink> NoLinks = Array.Empty<EvolutionLink>();
        private static readonly IReadOnlyList<string> NoPartners = Array.Empty<string>();

        private readonly Dictionary<string, List<EvolutionLink>> _from = new();
        private readonly Dictionary<string, List<EvolutionLink>> _to = new();
        private readonly Dictionary<string, List<string>> _dnaPartners = new();
        private readonly List<EvolutionLink> _links = new();
        private readonly List<string> _errors = new();

        private EvolutionDatabase() { }

        /// <summary>Todas as ligações carregadas (já sem as que falharam na validação).</summary>
        public IReadOnlyList<EvolutionLink> Links => _links;

        /// <summary>Mensagens dos erros de validação encontrados no carregamento (vazio = ok).</summary>
        public IReadOnlyList<string> ValidationErrors => _errors;

        /// <summary>Ligações que saem de <paramref name="code"/> (todos os tipos).</summary>
        public IReadOnlyList<EvolutionLink> GetEvolutionsFrom(string code) =>
            code != null && _from.TryGetValue(code, out var list) ? list : NoLinks;

        /// <summary>Ligações que chegam em <paramref name="code"/> (todos os tipos).</summary>
        public IReadOnlyList<EvolutionLink> GetPreEvolutions(string code) =>
            code != null && _to.TryGetValue(code, out var list) ? list : NoLinks;

        /// <summary>Os dois Digimons que precisam estar juntos para formar
        /// <paramref name="resultCode"/> via DNA Digivolution, ou lista vazia se o code não é
        /// resultado de DNA.</summary>
        public IReadOnlyList<string> GetDnaPartners(string resultCode) =>
            resultCode != null && _dnaPartners.TryGetValue(resultCode, out var list) ? list : NoPartners;

        /// <summary>Busca a ligação exata <paramref name="from"/> → <paramref name="to"/>.</summary>
        public bool TryGetLink(string from, string to, out EvolutionLink link)
        {
            link = GetEvolutionsFrom(from).FirstOrDefault(l => l.To == to);
            return link != null;
        }

        /// <summary>
        /// Lê o JSON da teia e valida contra os Digimons carregados (<paramref name="digimonsByCode"/>,
        /// chaveado pelo campo <c>code</c>). Sempre devolve uma instância utilizável - em caso de
        /// JSON ilegível ela vem vazia e com o erro em <see cref="ValidationErrors"/>.
        /// </summary>
        public static EvolutionDatabase Load(string json, IReadOnlyDictionary<string, DigimonData> digimonsByCode)
        {
            var db = new EvolutionDatabase();

            EvolutionWebFile file;

            try
            {
                file = JsonSerializer.Deserialize<EvolutionWebFile>(json, JsonOptions);
            }
            catch (JsonException ex)
            {
                db.Report($"evolution_web.json ilegível: {ex.Message}");
                return db;
            }

            if (file == null)
            {
                db.Report("evolution_web.json vazio.");
                return db;
            }

            db.BuildLinks(file, digimonsByCode);
            db.BuildDnaRecipes(file, digimonsByCode);
            db.ValidateStages(digimonsByCode);
            db.ValidateAgainstDigimonList(file, digimonsByCode);
            db.ValidateNoDeadEnds(digimonsByCode);

            return db;
        }

        private void Report(string message)
        {
            _errors.Add(message);
            GD.PushError($"[Teia evolutiva] {message}");
        }

        private void BuildLinks(EvolutionWebFile file, IReadOnlyDictionary<string, DigimonData> digimons)
        {
            var unknownCodes = new HashSet<string>();

            bool CodeExists(string code)
            {
                if (!string.IsNullOrEmpty(code) && digimons.ContainsKey(code))
                    return true;

                // Um code desconhecido costuma aparecer em várias ligações - avisa uma vez só.
                if (unknownCodes.Add(code ?? "<vazio>"))
                    Report($"code '{code}' do arquivo não existe em Data/Digimon.");

                return false;
            }

            foreach (var edge in file.Edges)
            {
                bool fromOk = CodeExists(edge.From);
                bool toOk = CodeExists(edge.To);

                if (!fromOk || !toOk)
                    continue;

                if (!TryParseEnum(edge.Type, out EvolutionLinkType type))
                {
                    Report($"ligação {edge.From} → {edge.To} tem type inválido '{edge.Type}' (esperado: normal, dna ou mode).");
                    continue;
                }

                if (!TryParseEnum(edge.Source, out EvolutionLinkSource source))
                {
                    Report($"ligação {edge.From} → {edge.To} tem source inválido '{edge.Source}' (esperado: canon ou design).");
                    continue;
                }

                if (edge.From == edge.To)
                {
                    Report($"ligação {edge.From} → {edge.To} aponta para si mesma.");
                    continue;
                }

                if (TryGetLink(edge.From, edge.To, out _))
                {
                    Report($"ligação {edge.From} → {edge.To} duplicada.");
                    continue;
                }

                var link = new EvolutionLink(edge.From, edge.To, type, source);

                _links.Add(link);
                GetOrCreate(_from, edge.From).Add(link);
                GetOrCreate(_to, edge.To).Add(link);
            }
        }

        private void BuildDnaRecipes(EvolutionWebFile file, IReadOnlyDictionary<string, DigimonData> digimons)
        {
            foreach (var recipe in file.Dna)
            {
                if (!digimons.ContainsKey(recipe.Result ?? ""))
                {
                    Report($"dna: resultado '{recipe.Result}' não existe em Data/Digimon.");
                    continue;
                }

                if (recipe.Partners == null || recipe.Partners.Count != 2)
                {
                    Report($"dna: '{recipe.Result}' precisa de exatamente 2 parceiros (tem {recipe.Partners?.Count ?? 0}).");
                    continue;
                }

                var missing = recipe.Partners.Where(p => !digimons.ContainsKey(p ?? "")).ToList();

                if (missing.Count > 0)
                {
                    Report($"dna: parceiro(s) '{string.Join(", ", missing)}' de '{recipe.Result}' não existem em Data/Digimon.");
                    continue;
                }

                if (_dnaPartners.ContainsKey(recipe.Result))
                {
                    Report($"dna: receita duplicada para '{recipe.Result}'.");
                    continue;
                }

                _dnaPartners[recipe.Result] = recipe.Partners.ToList();
            }

            // As ligações dna dos edges e as receitas de dna[] precisam contar a mesma história.
            foreach (var link in _links.Where(l => l.Type == EvolutionLinkType.Dna))
            {
                if (!_dnaPartners.TryGetValue(link.To, out var partners))
                    Report($"ligação dna {link.From} → {link.To}: '{link.To}' não tem receita em dna[].");
                else if (!partners.Contains(link.From))
                    Report($"ligação dna {link.From} → {link.To}: '{link.From}' não é parceiro na receita de '{link.To}'.");
            }

            foreach (var (result, partners) in _dnaPartners)
            {
                foreach (var partner in partners)
                {
                    if (!TryGetLink(partner, result, out var link) || link.Type != EvolutionLinkType.Dna)
                        Report($"dna: receita de '{result}' lista '{partner}', mas falta a ligação dna {partner} → {result} em edges[].");
                }
            }
        }

        private void ValidateStages(IReadOnlyDictionary<string, DigimonData> digimons)
        {
            foreach (var link in _links)
            {
                var fromStage = digimons[link.From].Stage;
                var toStage = digimons[link.To].Stage;

                switch (link.Type)
                {
                    case EvolutionLinkType.Normal:
                        int fromIndex = Array.IndexOf(StageOrder, fromStage);
                        int toIndex = Array.IndexOf(StageOrder, toStage);

                        if (fromIndex < 0 || toIndex < 0 || toIndex != fromIndex + 1)
                            Report($"ligação normal {link.From} ({fromStage}) → {link.To} ({toStage}) não vai para o estágio seguinte.");
                        break;

                    case EvolutionLinkType.Mode:
                        if (fromStage != toStage)
                            Report($"ligação mode {link.From} ({fromStage}) → {link.To} ({toStage}) deveria ficar no mesmo estágio.");
                        break;
                }
            }
        }

        private void ValidateAgainstDigimonList(EvolutionWebFile file, IReadOnlyDictionary<string, DigimonData> digimons)
        {
            // digimon[] é redundante com edges[] - se divergirem, alguém editou só um dos dois.
            foreach (var entry in file.Digimon)
            {
                if (!digimons.ContainsKey(entry.Code ?? ""))
                {
                    Report($"digimon[]: code '{entry.Code}' não existe em Data/Digimon.");
                    continue;
                }

                var declaredTo = entry.EvolvesTo.Select(t => $"{t.To}:{t.Type}").OrderBy(s => s).ToList();
                var actualTo = GetEvolutionsFrom(entry.Code).Select(l => $"{l.To}:{l.Type.ToString().ToLowerInvariant()}").OrderBy(s => s).ToList();

                if (!declaredTo.SequenceEqual(actualTo))
                    Report($"digimon[] '{entry.Code}': evolvesTo diverge de edges[].");

                var declaredFrom = entry.EvolvesFrom.OrderBy(s => s).ToList();
                // Convenção do arquivo: evolvesFrom lista só as pré-evoluções "normal" (as de dna/mode
                // aparecem apenas em evolvesTo do parceiro e em dna[]).
                var actualFrom = GetPreEvolutions(entry.Code)
                    .Where(l => l.Type == EvolutionLinkType.Normal)
                    .Select(l => l.From)
                    .OrderBy(s => s)
                    .ToList();

                if (!declaredFrom.SequenceEqual(actualFrom))
                    Report($"digimon[] '{entry.Code}': evolvesFrom diverge de edges[].");
            }

            var listed = new HashSet<string>(file.Digimon.Select(d => d.Code ?? ""));

            foreach (var code in digimons.Keys.Where(c => !listed.Contains(c)))
                Report($"Digimon '{code}' existe em Data/Digimon mas falta em digimon[] da teia.");
        }

        private void ValidateNoDeadEnds(IReadOnlyDictionary<string, DigimonData> digimons)
        {
            int megaIndex = Array.IndexOf(StageOrder, DigimonStage.Mega);

            foreach (var (code, data) in digimons.OrderBy(kv => kv.Value.Id))
            {
                int index = Array.IndexOf(StageOrder, data.Stage);

                if (index >= 0 && index < megaIndex && GetEvolutionsFrom(code).Count == 0)
                    Report($"'{code}' ({data.Stage}) não tem nenhuma evolução.");
            }
        }

        private static bool TryParseEnum<T>(string value, out T result) where T : struct, Enum =>
            Enum.TryParse(value, ignoreCase: true, out result) && Enum.IsDefined(typeof(T), result);

        private static List<T> GetOrCreate<T>(Dictionary<string, List<T>> map, string key)
        {
            if (!map.TryGetValue(key, out var list))
            {
                list = new List<T>();
                map[key] = list;
            }

            return list;
        }
    }
}
