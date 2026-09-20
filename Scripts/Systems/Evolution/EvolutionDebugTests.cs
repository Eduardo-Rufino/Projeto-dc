using Godot;
using ProjetoDC.Enums;
using ProjetoDC.Scripts.Data;
using ProjetoDC.Scripts.Gameplay;
using ProjetoDC.Scripts.Managers;
using ProjetoDC.Scripts.Systems.Jogress;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjetoDC.Scripts.Systems.Evolution
{
    /// <summary>
    /// Verificação manual da malha evolutiva via log (o projeto não tem framework de teste, ver
    /// AGENTS.md) - roda sob demanda com a variável de ambiente DEBUG_TEST_EVOLUTION, mesmo
    /// padrão de DEBUG_TEST_PASSIVES (ver Center._Ready), sem afetar o jogo normal. Usa o
    /// EvolutionSystem/JogressSystem reais com instâncias novas (nada toca no save):
    ///  1. a teia (evolution_web.json) e as evoluções com requisitos (Data/Evolutions) contam a
    ///     mesma história;
    ///  2. toda evolução consegue acontecer de fato (nenhuma fica "escondida" atrás de uma
    ///     irmã mais exigente que sempre vence);
    ///  3. os Digimons de Jogress só saem por Jogress, e todas as receitas funcionam.
    /// </summary>
    public static class EvolutionDebugTests
    {
        private static int _failures;

        public static void Run()
        {
            _failures = 0;

            GD.Print("===== EvolutionDebugTests: início =====");

            TestWebMatchesRuntimeData();
            TestEveryEvolutionIsReachable();
            TestJogressOnlyForms();
            TestJogressRecipes();

            GD.Print(_failures == 0
                ? "===== EvolutionDebugTests: tudo OK ====="
                : $"===== EvolutionDebugTests: {_failures} FALHA(S) =====");
        }

        private static void Check(bool condition, string message)
        {
            if (condition)
                return;

            _failures++;
            GD.PrintErr($"[EvolutionDebugTests] FALHA: {message}");
        }

        private static DatabaseManager Db => DatabaseManager.Instance;

        // ---------------------------------------------------------------- 1. teia x dados

        private static void TestWebMatchesRuntimeData()
        {
            GD.Print("--- 1. teia x Data/Evolutions");

            Check(Db.EvolutionWeb.ValidationErrors.Count == 0,
                $"a teia tem {Db.EvolutionWeb.ValidationErrors.Count} erro(s) de validação");

            var runtime = new HashSet<(string, string)>();

            foreach (var digimon in Db.GetAllDigimons())
            {
                foreach (var evo in Db.GetEvolutionsFrom(digimon.Id))
                    runtime.Add((digimon.Code, Db.GetDigimon(evo.ToDigimonId).Code));
            }

            var web = Db.EvolutionWeb.Links
                .Where(l => l.Type != EvolutionLinkType.Dna)
                .Select(l => (l.From, l.To))
                .ToHashSet();

            foreach (var missing in web.Except(runtime))
                Check(false, $"{missing.Item1} → {missing.Item2} está na teia mas não tem requisitos em Data/Evolutions");

            foreach (var extra in runtime.Except(web))
                Check(false, $"{extra.Item1} → {extra.Item2} existe em Data/Evolutions mas não está na teia");

            GD.Print($"    teia: {web.Count} ligações | Data/Evolutions: {runtime.Count}");
        }

        // ---------------------------------------------------------- 2. alcançabilidade

        // MeetsRequirements é privado no EvolutionSystem - reflexão pra usar o critério REAL sem
        // evoluir de verdade (Evolve loga e muda o Digimon, o que tornaria o teste lento e
        // barulhento).
        private static readonly System.Reflection.MethodInfo MeetsRequirements =
            typeof(EvolutionSystem).GetMethod(
                "MeetsRequirements",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        private static void TestEveryEvolutionIsReachable()
        {
            GD.Print("--- 2. toda evolução consegue acontecer");

            if (MeetsRequirements == null)
            {
                Check(false, "EvolutionSystem.MeetsRequirements não foi encontrado (renomeado?)");
                return;
            }

            int total = 0;
            int tieOnly = 0;
            var hidden = new List<string>();

            foreach (var source in Db.GetAllDigimons())
            {
                var evolutions = Db.GetEvolutionsFrom(source.Id);

                if (evolutions.Count == 0)
                    continue;

                var (unique, tied) = FindWinners(source, evolutions);

                foreach (var evo in evolutions)
                {
                    total++;

                    string code = Db.GetDigimon(evo.ToDigimonId).Code;

                    if (unique.Contains(evo.ToDigimonId))
                        continue;

                    if (tied.Contains(evo.ToDigimonId))
                    {
                        tieOnly++;
                        GD.Print($"    (só por sorteio de empate) {source.Code} → {code}");
                        continue;
                    }

                    var winners = string.Join(", ", unique.Union(tied).Select(id => Db.GetDigimon(id).Code));

                    hidden.Add($"{source.Code} → {code} nunca é escolhida (o que ganha no lugar: {winners})");
                }
            }

            foreach (var line in hidden)
                Check(false, line);

            GD.Print($"    {total} evoluções testadas | {total - hidden.Count} alcançáveis | " +
                     $"{hidden.Count} escondidas | {tieOnly} só por sorteio de empate");
        }

        /// <summary>Simula o EvolutionSystem em vários estados do Digimon e junta quais destinos
        /// ele chega a escolher: <c>unique</c> = venceu sozinho (maior placar), <c>tied</c> =
        /// só apareceu num empate de placar (sorteio). Os estados partem dos requisitos mínimos
        /// de cada evolução do <paramref name="source"/> e variam Felicidade/Disciplina (nos
        /// cortes que os próprios dados usam) e as batalhas.</summary>
        private static (HashSet<int> Unique, HashSet<int> Tied) FindWinners(
            DigimonData source, List<EvolutionData> evolutions)
        {
            var unique = new HashSet<int>();
            var tied = new HashSet<int>();

            // Só os valores de Felicidade/Disciplina que mudam algum resultado: os extremos e
            // cada corte usado nos dados (e o vizinho de cada um).
            var moods = new SortedSet<int> { 0, 100 };

            foreach (var evo in evolutions)
            {
                foreach (int v in new[] { evo.MinHappiness, evo.MaxHappiness, evo.MinDiscipline, evo.MaxDiscipline })
                {
                    moods.Add(Math.Clamp(v - 1, 0, 100));
                    moods.Add(Math.Clamp(v, 0, 100));
                    moods.Add(Math.Clamp(v + 1, 0, 100));
                }
            }

            foreach (var basis in evolutions)
            {
                int neededBattles = Math.Max(basis.RequiredBattles, basis.RequiredWinRate > 0 ? 20 : 0);

                foreach (int happiness in moods)
                {
                    foreach (int discipline in moods)
                    {
                        foreach (int battles in new[] { neededBattles, 60 })
                        {
                            var digimon = new DigimonInstance(source)
                            {
                                Level = basis.RequiredLevel,
                                AgeInDays = basis.RequiredAgeInDays,
                                Happiness = happiness,
                                Discipline = discipline,
                                BattlesFought = battles,
                                BattlesWon = battles,
                            };

                            var req = basis.RequiredStats;
                            var stats = digimon.CurrentStats;

                            stats.HealthPoints = Math.Max(stats.HealthPoints, req.HealthPoints);
                            stats.PhysicalDamage = Math.Max(stats.PhysicalDamage, req.PhysicalDamage);
                            stats.PhysicalDefense = Math.Max(stats.PhysicalDefense, req.PhysicalDefense);
                            stats.SpecialDamage = Math.Max(stats.SpecialDamage, req.SpecialDamage);
                            stats.SpecialDefense = Math.Max(stats.SpecialDefense, req.SpecialDefense);
                            stats.Speed = Math.Max(stats.Speed, req.Speed);

                            var valid = evolutions
                                .Where(e => (bool)MeetsRequirements.Invoke(null, new object[] { digimon, e }))
                                .ToList();

                            if (valid.Count == 0)
                                continue;

                            int best = valid.Max(EvolutionSystem.CalculateEvolutionScore);

                            var top = valid.Where(e => EvolutionSystem.CalculateEvolutionScore(e) == best).ToList();

                            foreach (var winner in top)
                            {
                                if (top.Count == 1)
                                    unique.Add(winner.ToDigimonId);
                                else
                                    tied.Add(winner.ToDigimonId);
                            }
                        }
                    }
                }
            }

            return (unique, tied);
        }

        // ---------------------------------------------------------------- 3. Jogress

        private static void TestJogressOnlyForms()
        {
            GD.Print("--- 3. formas de Jogress só saem por Jogress");

            var jogressResults = Db.GetAllJogressRecipes().Select(r => r.ToDigimonId).ToHashSet();

            foreach (var source in Db.GetAllDigimons())
            {
                foreach (var evo in Db.GetEvolutionsFrom(source.Id))
                {
                    if (jogressResults.Contains(evo.ToDigimonId))
                        Check(false,
                            $"{source.Code} evolui normalmente para {Db.GetDigimon(evo.ToDigimonId).Code}, que só deveria sair por Jogress");
                }
            }

            // A teia também não pode ter ligação "normal" chegando nelas.
            foreach (var link in Db.EvolutionWeb.Links.Where(l => l.Type == EvolutionLinkType.Normal))
            {
                var target = Db.GetAllDigimons().First(d => d.Code == link.To);

                Check(!jogressResults.Contains(target.Id),
                    $"a teia tem uma ligação normal {link.From} → {link.To}, que só deveria sair por Jogress");
            }

            GD.Print($"    {jogressResults.Count} Digimons só por Jogress: " +
                     string.Join(", ", jogressResults.Select(id => Db.GetDigimon(id).Code)));
        }

        private static void TestJogressRecipes()
        {
            GD.Print("--- 4. receitas de Jogress");

            foreach (var recipe in Db.GetAllJogressRecipes())
            {
                var a = Db.GetDigimon(recipe.DigimonAId);
                var b = Db.GetDigimon(recipe.DigimonBId);
                var result = Db.GetDigimon(recipe.ToDigimonId);

                // Os dois parceiros também precisam bater com a teia (DNA).
                var partners = Db.EvolutionWeb.GetDnaPartners(result.Code);

                Check(partners.Count == 2 && partners.Contains(a.Code) && partners.Contains(b.Code),
                    $"receita {a.Code} + {b.Code} → {result.Code} não bate com dna[] da teia");

                // Nos dois sentidos (a ordem dos parceiros não pode importar).
                foreach (var (first, second) in new[] { (a, b), (b, a) })
                {
                    var fused = JogressSystem.TryToFuse(
                        new DigimonInstance(first), new DigimonInstance(second), 0, 100000);

                    Check(fused.Outcome == JogressOutcome.Fused &&
                          fused.FusedDigimon?.BaseData.Id == result.Id,
                        $"{first.Code} + {second.Code} não gerou {result.Code}");
                }

                // Sem capacidade sobrando o Jogress é recusado sem consumir ninguém.
                var blocked = JogressSystem.TryToFuse(
                    new DigimonInstance(a), new DigimonInstance(b), 100000, 1);

                Check(blocked.Outcome == JogressOutcome.BlockedByCapacity,
                    $"{a.Code} + {b.Code} deveria ser bloqueado sem capacidade");

                // Um parceiro trocado por outro não pode formar o resultado.
                var wrong = Db.GetAllDigimons().First(d => d.Id != a.Id && d.Id != b.Id &&
                                                            Db.GetJogressRecipe(a.Id, d.Id) == null);

                var invalid = JogressSystem.TryToFuse(
                    new DigimonInstance(a), new DigimonInstance(wrong), 0, 100000);

                Check(invalid.Outcome == JogressOutcome.NotEligible,
                    $"{a.Code} + {wrong.Code} não deveria formar nada");

                GD.Print($"    {a.Code} + {b.Code} → {result.Code}: ok");
            }
        }
    }
}
