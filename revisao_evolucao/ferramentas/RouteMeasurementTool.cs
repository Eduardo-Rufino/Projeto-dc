using Godot;
using ProjetoDC.Enums;
using ProjetoDC.Scripts.Data;
using ProjetoDC.Scripts.Gameplay;
using ProjetoDC.Scripts.Managers;
using ProjetoDC.Scripts.Models.World;
using ProjetoDC.Scripts.Save;
using ProjetoDC.Scripts.Systems.Center;
using ProjetoDC.Scripts.Systems.Clock;
using ProjetoDC.Scripts.Systems.Eggs;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ProjetoDC.Revisao
{
    /// <summary>
    /// Fase 4 da revisão de requisitos de evolução: MEDIÇÃO, não correção. Pra cada Digimon e
    /// cada rota de entrada possível desde o ovo, sobe pela cadeia real (EvolutionSystem/
    /// DigimonInstance de verdade, os mesmos 2 pontos de checagem - batalha e virada de dia -
    /// já provados na Fase 2) treinando SOMENTE o que o requisito de cada aresta exige (lê
    /// EvolutionData.RequiredStats direto, nada de "assinatura" escolhida à mão) e registra a
    /// chegada de cada forma, no nível mínimo dela, em revisao_evolucao/analise/rotas_reais.csv.
    /// Quando uma aresta não bate (outra irmã dispara antes, ou não evolui dentro do limite),
    /// a rota entra como "NAO_ALCANCAVEL" com o que aconteceu de verdade em vez de inventar
    /// número - não ajusta nenhum requisito.
    ///
    /// Uso: godot --headless --fixed-fps 60 --path . res://revisao_evolucao/ferramentas/RouteMeasurementTool.tscn
    /// Env DEBUG_ROUTES_ROOTS="tentomon,gaomon,kotemon" restringe a exploração a esses códigos de
    /// espécie como raiz (pra validar rápido antes de rodar o grafo inteiro, sem esse env roda tudo).
    /// </summary>
    public partial class RouteMeasurementTool : Node
    {
        private static readonly BindingFlags NonPublic = BindingFlags.NonPublic | BindingFlags.Instance;
        private static GameManager Gm => GameManager.Instance;
        private static DatabaseManager Db => DatabaseManager.Instance;

        private readonly List<string[]> _rows = new();
        private readonly HashSet<int> _rootsSeen = new();
        private int _reachableCount, _unreachableCount, _maxDepthHits;
        private const int MaxDepth = 8; // maior estágio possível (Baby..Special) tem folga de sobra

        public override void _Ready() => CallDeferred(nameof(Start));

        private async void Start()
        {
            await Frames(30);

            GD.Print("===== RouteMeasurementTool: início =====");

            var allEdgesByFrom = Db.GetAllDigimons()
                .ToDictionary(d => d.Id, d => Db.GetEvolutionsFrom(d.Id));

            var hasIncoming = new HashSet<int>();
            foreach (var edges in allEdgesByFrom.Values)
                foreach (var e in edges)
                    hasIncoming.Add(e.ToDigimonId);

            var roots = Db.GetAllDigimons().Where(d => !hasIncoming.Contains(d.Id)).ToList();

            string filter = OS.GetEnvironment("DEBUG_ROUTES_ROOTS");
            if (!string.IsNullOrWhiteSpace(filter))
            {
                var wanted = filter.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(s => s.ToLowerInvariant()).ToHashSet();
                roots = roots.Where(d => wanted.Contains(d.Code.ToLowerInvariant())).ToList();
                GD.Print($"  filtro DEBUG_ROUTES_ROOTS ativo: {roots.Count} raiz(es) - {string.Join(", ", roots.Select(r => r.Name))}");
            }

            GD.Print($"  {roots.Count} raízes (sem aresta normal de entrada) de {Db.GetAllDigimons().Count()} espécies, {allEdgesByFrom.Values.Sum(l => l.Count)} arestas normais");

            _rows.Add(new[] { "rota", "forma", "estagio", "nivel", "hp", "pa", "pd", "ea", "ed", "vel", "alcancavel", "obs" });

            foreach (var root in roots.OrderBy(r => r.Name))
            {
                ResetScratch();
                var d = SpawnNatural(root.Id);
                ExploreNode(d, root.Name, allEdgesByFrom, depth: 0);
            }

            WriteCsv();

            GD.Print($"===== RouteMeasurementTool: {_reachableCount} chegadas medidas, {_unreachableCount} rotas não alcançáveis, {_maxDepthHits} corte(s) por profundidade máxima =====");

            GetTree().Quit(0);
        }

        private async Task Frames(int n)
        {
            for (int i = 0; i < n; i++)
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }

        // ------------------------------------------------------------------ infraestrutura (mesma receita da Fase 3 v2/v3)

        private void ResetScratch(int bits = 100_000, int capacity = 5000)
        {
            var gm = Gm;
            var t = typeof(GameManager);

            var onHour = (Action)Delegate.CreateDelegate(typeof(Action), gm, t.GetMethod("OnHourPassed", NonPublic));
            var onDay = (Action)Delegate.CreateDelegate(typeof(Action), gm, t.GetMethod("OnDayPassed", NonPublic));
            var onMinute = (Action)Delegate.CreateDelegate(typeof(Action), gm, t.GetMethod("OnMinutePassed", NonPublic));

            if (gm.ClockSystem != null)
            {
                gm.ClockSystem.HourPassed -= onHour;
                gm.ClockSystem.DayPassed -= onDay;
                gm.ClockSystem.MinutePassed -= onMinute;
            }

            var save = new SaveData();

            t.GetProperty("Save").GetSetMethod(true).Invoke(gm, new object[] { save });
            t.GetProperty("CenterService").GetSetMethod(true).Invoke(gm, new object[] { new CenterService(save.Center) });
            var clock = new ClockSystem(save.World);
            t.GetProperty("ClockSystem").GetSetMethod(true).Invoke(gm, new object[] { clock });
            clock.HourPassed += onHour;
            clock.DayPassed += onDay;
            clock.MinutePassed += onMinute;
            gm.EggSystem = new EggSystem();
            t.GetProperty("PlayerDigimon").GetSetMethod(true).Invoke(gm, new object[] { null });
            t.GetField("_hasActiveGame", NonPublic)?.SetValue(gm, false);
            t.GetProperty("ActiveTournament").GetSetMethod(true).Invoke(gm, new object[] { null });
            t.GetProperty("BattleFromExploration").GetSetMethod(true)?.Invoke(gm, new object[] { false });

            save.Center.Bits = bits;
            save.Center.CapacityLimit = capacity;
        }

        private static DigimonInstance SpawnNatural(int speciesId, int level = 1, int age = 0, int happiness = 50, int discipline = 50)
        {
            var species = Db.GetDigimon(speciesId);
            var d = new DigimonInstance(species);

            while (d.Level < level)
                d.GainExperience(d.ExperienceToNextLevel);

            d.AgeInDays = age;
            d.Happiness = happiness;
            d.Discipline = discipline;
            d.RestoreHealth();

            return d;
        }

        /// <summary>Clona uma instância (espécie + Level/Idade/XP/Felicidade/Disciplina/Stamina/
        /// contagem de batalhas/stats atuais) pra explorar um ramo sem afetar os outros - cada
        /// aresta de saída de um nó precisa da SUA PRÓPRIA cópia, senão treinar pra uma irmã
        /// vazaria stat pra tentativa da outra.</summary>
        private static DigimonInstance CloneInstance(DigimonInstance src)
        {
            var clone = new DigimonInstance(Db.GetDigimon(src.BaseData.Id))
            {
                Level = src.Level,
                Experience = src.Experience,
                ExperienceToNextLevel = src.ExperienceToNextLevel,
                AgeInDays = src.AgeInDays,
                Happiness = src.Happiness,
                Discipline = src.Discipline,
                Stamina = src.Stamina,
                MaxStamina = src.MaxStamina,
                BattlesFought = src.BattlesFought,
                BattlesWon = src.BattlesWon,
                CurrentStats = new BaseStats
                {
                    HealthPoints = src.CurrentStats.HealthPoints,
                    PhysicalDamage = src.CurrentStats.PhysicalDamage,
                    PhysicalDefense = src.CurrentStats.PhysicalDefense,
                    SpecialDamage = src.CurrentStats.SpecialDamage,
                    SpecialDefense = src.CurrentStats.SpecialDefense,
                    Speed = src.CurrentStats.Speed,
                },
            };

            clone.MaxHealthPoints = src.MaxHealthPoints;
            clone.CurrentHealthPoints = src.CurrentHealthPoints;
            clone.RestoreHealth();

            return clone;
        }

        private static Func<BaseStats, int> StatReader(TrainingType type) => type switch
        {
            TrainingType.HealthPoints => s => s.HealthPoints,
            TrainingType.Attack => s => s.PhysicalDamage,
            TrainingType.Defense => s => s.PhysicalDefense,
            TrainingType.SpecialAttack => s => s.SpecialDamage,
            TrainingType.SpecialDefense => s => s.SpecialDefense,
            TrainingType.Speed => s => s.Speed,
            _ => throw new ArgumentOutOfRangeException(nameof(type)),
        };

        private void TrainOnce(DigimonInstance d, TrainingType type)
        {
            if (d.Stamina < 10)
                d.Stamina = 100;

            var result = Gm.TrainingSystem.Execute(d, type);

            if (result.Success)
                d.ApplyTrainingResult(result);
        }

        private void TrainUntilStat(DigimonInstance d, TrainingType type, Func<BaseStats, int> read, int floor, int maxAttempts = 200)
        {
            for (int i = 0; i < maxAttempts && read(d.CurrentStats) < floor; i++)
                TrainOnce(d, type);
        }

        private void BattleCheckpointWithLevelUp(DigimonInstance member)
        {
            var t = typeof(GameManager);

            var enemySpecies = Db.GetAllDigimons().First(s => s.Stage == DigimonStage.Mega);
            var enemy = new DigimonInstance(enemySpecies) { Level = member.Level + 10 };

            t.GetProperty("PlayerBattleTeam").GetSetMethod(true)
                .Invoke(Gm, new object[] { new List<DigimonInstance> { member } });
            t.GetProperty("EnemyBattleTeam").GetSetMethod(true)
                .Invoke(Gm, new object[] { new List<DigimonInstance> { enemy } });

            Gm.ApplyTeamBattleReward(BattleResult.PlayerWon);
        }

        private void DayPassedCheckpoint(DigimonInstance member)
        {
            if (!Gm.Save.Center.Digimons.Contains(member))
                Gm.CenterService.AddDigimon(member);

            member.Feed(member.MaxHunger);
            Gm.ClockSystem.Update(1440.0);

            if (member.HealthState == HealthState.Sick)
                member.Heal();
        }

        // ------------------------------------------------------------------ o núcleo: sobe UMA aresta treinando só o que ela exige

        /// <summary>Treina cada stat de <see cref="EvolutionData.RequiredStats"/> que for > 0
        /// (nada além disso - nem "assinatura" escolhida à mão, nem stats que a aresta não
        /// pede), ajusta Felicidade/Disciplina só o suficiente pra caber na janela [Min,Max] da
        /// aresta (não é treino - é o estado prévio necessário pra essa aresta em particular
        /// poder dar certo, testado numa cópia isolada), sobe de nível por batalha até o
        /// exigido e avança dias até a idade exigida - devolve se o resultado bateu com o alvo
        /// pretendido (edge.ToDigimonId) ou se outra coisa disparou primeiro / nada evoluiu.</summary>
        private bool GrowToEdge(DigimonInstance d, EvolutionData edge, int maxBattles = 260, int maxDays = 60)
        {
            int startId = d.BaseData.Id;
            var req = edge.RequiredStats;

            if (req.HealthPoints > 0) TrainUntilStat(d, TrainingType.HealthPoints, StatReader(TrainingType.HealthPoints), req.HealthPoints);
            if (req.PhysicalDamage > 0) TrainUntilStat(d, TrainingType.Attack, StatReader(TrainingType.Attack), req.PhysicalDamage);
            if (req.PhysicalDefense > 0) TrainUntilStat(d, TrainingType.Defense, StatReader(TrainingType.Defense), req.PhysicalDefense);
            if (req.SpecialDamage > 0) TrainUntilStat(d, TrainingType.SpecialAttack, StatReader(TrainingType.SpecialAttack), req.SpecialDamage);
            if (req.SpecialDefense > 0) TrainUntilStat(d, TrainingType.SpecialDefense, StatReader(TrainingType.SpecialDefense), req.SpecialDefense);
            if (req.Speed > 0) TrainUntilStat(d, TrainingType.Speed, StatReader(TrainingType.Speed), req.Speed);

            // Disciplina/Felicidade são ajustadas SÓ DEPOIS do treino (não é treino em si, é o
            // estado prévio necessário pra essa aresta em particular) - TrainOnce/
            // ApplyTrainingResult sobe Felicidade +10 e Disciplina +5 a cada sucesso, então
            // ajustar ANTES do treino é desfeito pelo próprio treino (bug real encontrado: toda
            // aresta com maxDiscipline/maxHappiness baixo - Devimon, Wizardmon, Bakemon,
            // Myotismon etc. - nunca disparava).
            d.Discipline = Math.Clamp(d.Discipline, edge.MinDiscipline, edge.MaxDiscipline);
            d.Happiness = Math.Clamp(d.Happiness, edge.MinHappiness, edge.MaxHappiness);

            // Garante pelo menos 1 checkpoint de batalha depois do treino, mesmo se a cadeia já
            // chegou aqui com Nível/Idade de sobra pra ESTA aresta em particular (herdado de um
            // trecho anterior mais exigente) - treino sozinho nunca dispara TryToEvolve (só
            // batalha/virada de dia, ver Fase 2), então sem isso a checagem nunca rodaria.
            if (d.BaseData.Id == startId)
                BattleCheckpointWithLevelUp(d);

            // Continua batalhando não só até o Nível, mas também até RequiredBattles/
            // RequiredWinRate baterem (poucas arestas pedem isso, mas o Nível baixo costuma
            // ser satisfeito num único salto de XP, deixando a exigência de batalhas pra trás).
            for (int i = 0; i < maxBattles && d.BaseData.Id == startId &&
                 (d.Level < edge.RequiredLevel || d.BattlesFought < edge.RequiredBattles ||
                  (edge.RequiredWinRate > 0 && d.WinRate < edge.RequiredWinRate)); i++)
                BattleCheckpointWithLevelUp(d);

            for (int i = 0; i < maxDays && d.AgeInDays < edge.RequiredAgeInDays && d.BaseData.Id == startId; i++)
                DayPassedCheckpoint(d);

            if (OS.GetEnvironment("DEBUG_ROUTES_VERBOSE") == "1")
                GD.Print($"  [debug] {edge.FromDigimonId}->{edge.ToDigimonId} exigeLv{edge.RequiredLevel}/Age{edge.RequiredAgeInDays}/HP{req.HealthPoints}/PA{req.PhysicalDamage}/PD{req.PhysicalDefense}/EA{req.SpecialDamage}/ED{req.SpecialDefense}/Vel{req.Speed}/Bat{edge.RequiredBattles} " +
                    $"-> ficou Lv{d.Level} Age{d.AgeInDays} HP{d.CurrentStats.HealthPoints} PA{d.CurrentStats.PhysicalDamage} PD{d.CurrentStats.PhysicalDefense} EA{d.CurrentStats.SpecialDamage} ED{d.CurrentStats.SpecialDefense} Vel{d.CurrentStats.Speed} Bat{d.BattlesFought} nome={d.BaseData.Name}");

            return d.BaseData.Id == edge.ToDigimonId;
        }

        private void ExploreNode(DigimonInstance d, string route, Dictionary<int, List<EvolutionData>> edgesByFrom, int depth)
        {
            RecordArrival(route, d);

            if (depth >= MaxDepth)
            {
                _maxDepthHits++;
                GD.PrintErr($"  [corte] profundidade máxima em: {route}");
                return;
            }

            if (!edgesByFrom.TryGetValue(d.BaseData.Id, out var outgoing) || outgoing.Count == 0)
                return;

            foreach (var edge in outgoing)
            {
                var branch = CloneInstance(d);
                var targetName = Db.GetDigimon(edge.ToDigimonId)?.Name ?? $"id{edge.ToDigimonId}";

                // Isola cada ramo no seu próprio Center vazio - sem isso, Save.Center.Digimons
                // acumula TODO ramo já visitado (nunca é removido), e o OnDayPassed de um ramo
                // qualquer passa a rodar TryBecomeSick/TryToEvolve em cima de ramos antigos
                // também (foi isso que matou um Digimon de outro ramo no meio da exploração).
                ResetScratch();
                bool ok = GrowToEdge(branch, edge);

                if (ok)
                {
                    ExploreNode(branch, $"{route} > {branch.BaseData.Name}", edgesByFrom, depth + 1);
                }
                else
                {
                    RecordUnreachable(route, targetName, edge, branch);
                }
            }
        }

        private void RecordArrival(string route, DigimonInstance d)
        {
            _reachableCount++;
            _rows.Add(new[]
            {
                route,
                d.BaseData.Name,
                d.BaseData.Stage.ToString(),
                d.Level.ToString(CultureInfo.InvariantCulture),
                d.CurrentStats.HealthPoints.ToString(CultureInfo.InvariantCulture),
                d.CurrentStats.PhysicalDamage.ToString(CultureInfo.InvariantCulture),
                d.CurrentStats.PhysicalDefense.ToString(CultureInfo.InvariantCulture),
                d.CurrentStats.SpecialDamage.ToString(CultureInfo.InvariantCulture),
                d.CurrentStats.SpecialDefense.ToString(CultureInfo.InvariantCulture),
                d.CurrentStats.Speed.ToString(CultureInfo.InvariantCulture),
                "sim",
                "",
            });
        }

        private void RecordUnreachable(string route, string targetName, EvolutionData edge, DigimonInstance branch)
        {
            _unreachableCount++;

            string obs = branch.BaseData.Id == edge.FromDigimonId
                ? "nao evoluiu dentro do limite de batalhas/dias"
                : $"virou {branch.BaseData.Name} em vez disso (Lv{branch.Level} Age{branch.AgeInDays})";

            _rows.Add(new[]
            {
                $"{route} > {targetName}",
                targetName,
                (Db.GetDigimon(edge.ToDigimonId)?.Stage.ToString()) ?? "",
                edge.RequiredLevel.ToString(CultureInfo.InvariantCulture),
                "", "", "", "", "", "",
                "nao",
                obs,
            });
        }

        private void WriteCsv()
        {
            string path = ProjectSettings.GlobalizePath("res://revisao_evolucao/analise/rotas_reais.csv");

            var sb = new StringBuilder();
            foreach (var row in _rows)
                sb.AppendLine(string.Join(",", row.Select(CsvField)));

            System.IO.File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));

            GD.Print($"  CSV escrito em {path} ({_rows.Count - 1} linhas de dado)");
        }

        private static string CsvField(string s)
        {
            if (s == null) return "";
            return s.Contains(',') || s.Contains('"') || s.Contains('\n')
                ? "\"" + s.Replace("\"", "\"\"") + "\""
                : s;
        }
    }
}
