using Godot;
using ProjetoDC.Enums;
using ProjetoDC.Scripts.Data;
using ProjetoDC.Scripts.Gameplay;
using ProjetoDC.Scripts.Managers;
using ProjetoDC.Scripts.Save;
using ProjetoDC.Scripts.Systems.Battle;
using ProjetoDC.Scripts.Systems.Center;
using ProjetoDC.Scripts.Systems.Clock;
using ProjetoDC.Scripts.Systems.Eggs;
using ProjetoDC.Scripts.Systems.Passives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace ProjetoDC.Tools
{
    /// <summary>
    /// Fase 1c das passivas herdadas: MEDIÇÃO, não teste. Roda batalhas reais (BattleArena) e simula um
    /// Center por dias, sem mudar nada do jogo, e imprime os números com a marca [1C] no log. Igual ao
    /// GameAudit, desliga o salvamento (o save real só é lido no boot). Uso:
    ///   godot --headless --fixed-fps 60 --path . res://passivas/ferramentas/InheritedImpactTool.tscn
    /// DEBUG_1C_TASKS=1,2,3,4,5 restringe às tarefas.
    /// </summary>
    public partial class InheritedImpactTool : Node
    {
        private const int Task1Level = 30;
        private const int MegaLevel = 45;

        private static readonly BindingFlags NonPublic = BindingFlags.NonPublic | BindingFlags.Instance;
        private static DatabaseManager Db => DatabaseManager.Instance;
        private static GameManager Gm => GameManager.Instance;

        // Times de controle fixos (espécie, passiva própria fixa).
        private static readonly (int Id, PassiveType Passive)[] ControlUltimate =
        {
            (67, PassiveType.Couraca),      // Zudomon - Tank
            (42, PassiveType.AlvoMarcado),  // Rapidmon - Ranged (aplica debuff de Velocidade)
            (98, PassiveType.GolpePesado),  // Delumon - Warrior
        };

        private static readonly (int Id, PassiveType Passive)[] ControlMega =
        {
            (68, PassiveType.Couraca),      // Vikemon - Tank
            (144, PassiveType.AlvoMarcado), // Kentaurusmon - Ranged
            (143, PassiveType.GolpePesado), // Magnamon - Warrior
        };

        private static readonly (int Id, PassiveType Passive)[] AlliesMega =
        {
            (159, PassiveType.Couraca),      // Craniummon - Tank
            (175, PassiveType.PosturaFirme), // Sakuyamon - Ranged
        };

        public override void _Ready() => CallDeferred(nameof(Start));

        private async void Start()
        {
            await Frames(45);

            typeof(GameManager).GetField("_hasActiveGame", NonPublic)?.SetValue(Gm, false);
            Gm.IgnoreCalendar = false;

            var wanted = (OS.GetEnvironment("DEBUG_1C_TASKS") ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet();
            bool Run(string t) => wanted.Count == 0 || wanted.Contains(t);

            try
            {
                if (Run("1")) await Task1();
                if (Run("2")) await Task2();
                if (Run("3")) await Task3();
                if (Run("4")) await Task4();
                if (Run("5")) Task5();
                if (Run("6")) await Task1Calibrated();
                if (Run("7")) await Diagnose();
                if (Run("8")) DevimonCapSweep();
            }
            catch (Exception e)
            {
                GD.PrintErr($"[1C] EXCEÇÃO: {e}");
            }

            Out("FIM");
            GetTree().Quit();
        }

        // ================================================================ infraestrutura

        private static void Out(string s) => GD.Print("[1C] " + s);

        private async Task Frames(int n)
        {
            for (int i = 0; i < n; i++)
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }

        private static DigimonData Sp(int id) => Db.GetDigimon(id);

        private static void FixPassive(DigimonInstance d)
        {
            var pool = Sp(d.BaseData.Id)?.Passives;
            d.PassiveId = pool != null && pool.Count > 0 ? pool[0] : null;
        }

        private static DigimonInstance Make(int id, int level, PassiveType? passive = null)
        {
            var d = new DigimonInstance(Sp(id));
            while (d.Level < level)
                d.GainExperience(d.ExperienceToNextLevel);
            if (passive.HasValue) d.PassiveId = passive; else FixPassive(d);
            d.RestoreHealth();
            return d;
        }

        /// <summary>Requisitos mínimos de uma aresta (mesma ideia do GameAudit.SatisfyEdge).</summary>
        private static void Satisfy(DigimonInstance d, EvolutionData e, bool touchAge = true)
        {
            while (d.Level < e.RequiredLevel)
                d.GainExperience(d.ExperienceToNextLevel);
            if (touchAge)
                d.AgeInDays = Math.Max(d.AgeInDays, e.RequiredAgeInDays);
            var r = e.RequiredStats;
            if (r != null)
            {
                var s = d.CurrentStats;
                s.HealthPoints = Math.Max(s.HealthPoints, r.HealthPoints);
                s.PhysicalDamage = Math.Max(s.PhysicalDamage, r.PhysicalDamage);
                s.PhysicalDefense = Math.Max(s.PhysicalDefense, r.PhysicalDefense);
                s.SpecialDamage = Math.Max(s.SpecialDamage, r.SpecialDamage);
                s.SpecialDefense = Math.Max(s.SpecialDefense, r.SpecialDefense);
                s.Speed = Math.Max(s.Speed, r.Speed);
            }
            d.MaxHealthPoints = Math.Max(d.MaxHealthPoints, d.CurrentStats.HealthPoints);
            int fought = Math.Max(d.BattlesFought, e.RequiredBattles);
            if (e.RequiredWinRate > 0) fought = Math.Max(fought, 10);
            d.BattlesFought = fought;
            d.BattlesWon = Math.Max(d.BattlesWon, (int)Math.Ceiling(e.RequiredWinRate * fought));
            d.Discipline = Math.Clamp((e.MinDiscipline + e.MaxDiscipline) / 2, 0, 100);
            d.Happiness = Math.Clamp((e.MinHappiness + e.MaxHappiness) / 2, 0, 100);
        }

        /// <summary>Sobe a cadeia de verdade (Evolve), da primeira à última espécie, e leva ao nível.</summary>
        private static DigimonInstance BuildRoute(int[] chain, int level)
        {
            var d = Make(chain[0], 1);
            for (int i = 1; i < chain.Length; i++)
            {
                var edge = Db.GetEvolutionsFrom(d.BaseData.Id).First(e => e.ToDigimonId == chain[i]);
                Satisfy(d, edge);
                d.Evolve(Sp(chain[i]), edge.StatMultiplier);
            }
            while (d.Level < level)
                d.GainExperience(d.ExperienceToNextLevel);
            FixPassive(d);
            d.RestoreHealth();
            return d;
        }

        private static DigimonInstance Clone(DigimonInstance d, bool keepHistory = true)
        {
            var c = JsonSerializer.Deserialize<DigimonInstance>(JsonSerializer.Serialize(d));
            if (!keepHistory) c.EvolutionHistory.Clear();
            c.RestoreHealth();
            return c;
        }

        private static List<DigimonInstance> Team((int Id, PassiveType P)[] spec, int level) =>
            spec.Select(s => Make(s.Id, level, s.P)).ToList();

        private static string Names(IEnumerable<int> ids) => string.Join(" > ", ids.Select(i => Sp(i)?.Name ?? i.ToString()));

        private sealed class BattleOutcome
        {
            public bool Won;
            public double Seconds;
            public bool Finished;
        }

        /// <summary>Uma batalha real; <paramref name="observe"/> roda a cada quadro.</summary>
        private async Task<BattleOutcome> Fight(List<DigimonInstance> player, List<DigimonInstance> enemy, Action<BattleMatch> observe = null)
        {
            var arena = GD.Load<PackedScene>("res://Scenes/Battle/BattleArena.tscn").Instantiate<BattleArena>();
            GetTree().Root.AddChild(arena);
            arena.Init(player, enemy);

            double sim = 0;
            while (!arena.IsBattleOver && sim < 240)
            {
                await Frames(1);
                sim += GetProcessDeltaTime();
                observe?.Invoke(arena.Match);
            }

            var outcome = new BattleOutcome
            {
                Finished = arena.IsBattleOver,
                Won = arena.Match.CheckResult() == BattleResult.PlayerWon,
                Seconds = arena.Match.ElapsedSeconds,
            };

            arena.QueueFree();
            await Frames(2);
            return outcome;
        }

        private async Task<(int Wins, int Total, double AvgSeconds, int Unfinished)> Series(
            Func<List<DigimonInstance>> player, Func<List<DigimonInstance>> enemy, int n)
        {
            int wins = 0, unfinished = 0;
            double seconds = 0;
            for (int i = 0; i < n; i++)
            {
                var r = await Fight(player(), enemy());
                if (r.Won) wins++;
                if (!r.Finished) unfinished++;
                seconds += r.Seconds;
            }
            return (wins, n, seconds / n, unfinished);
        }

        private static string Pct(int a, int b) => b == 0 ? "-" : $"{100.0 * a / b:0}%";

        // ================================================================ tarefa 1 - impacto por rota

        private static readonly (string Name, int[] Chain)[] Task1Routes =
        {
            ("Agumon A1 (Greymon > MetalGreymon)", new[] { 25, 29, 1, 13, 39 }),
            ("Agumon A2 (Devimon > LadyDevimon)", new[] { 25, 29, 1, 45, 180 }),
            ("Agumon A3 (Dimetromon > Triceramon)", new[] { 25, 29, 1, 85, 111 }),
            ("Gabumon G1 (Garurumon > WereGarurumon)", new[] { 26, 30, 2, 14, 40 }),
            ("Gabumon G2 (Garurumon > MachGaogamon)", new[] { 26, 30, 2, 14, 48 }),
            ("Gabumon G3 (IceDevimon > Destromon)", new[] { 63, 64, 2, 46, 118 }),
        };

        private async Task Task1()
        {
            const int N = 30;
            Out("=== TAREFA 1: impacto por rota (3 clones da rota vs controle Ultimate fixo, nível " + Task1Level + ") ===");
            string only1 = OS.GetEnvironment("DEBUG_1C_ROUTES") ?? "";
            foreach (var (name, chain) in Task1Routes)
            {
                if (only1.Length > 0 && !only1.Split(',').Any(r => name.Contains(r)))
                    continue;
                var proto = BuildRoute(chain, Task1Level);
                var actives = InheritedPassiveSystem.GetActiveInheritedPassives(proto).Select(p => p.Species.Name);
                var with = await Series(() => Enumerable.Range(0, 3).Select(_ => Clone(proto)).ToList(), () => Team(ControlUltimate, Task1Level), N);
                var without = await Series(() => Enumerable.Range(0, 3).Select(_ => Clone(proto, false)).ToList(), () => Team(ControlUltimate, Task1Level), N);
                var mirror = await Series(() => Enumerable.Range(0, 3).Select(_ => Clone(proto)).ToList(),
                                          () => Enumerable.Range(0, 3).Select(_ => Clone(proto, false)).ToList(), N);
                var s = proto.CurrentStats;
                Out($"T1 | {name} | forma {proto.BaseData.Name} ({proto.BaseData.Role}, {proto.BaseData.AttackType}) | herdadas: {string.Join(", ", actives)} | " +
                    $"stats HP{s.HealthPoints} PA{s.PhysicalDamage} PD{s.PhysicalDefense} EA{s.SpecialDamage} ED{s.SpecialDefense} Vel{s.Speed} | " +
                    $"COM {with.Wins}/{N} ({Pct(with.Wins, N)}) {with.AvgSeconds:0.0}s | SEM {without.Wins}/{N} ({Pct(without.Wins, N)}) {without.AvgSeconds:0.0}s | " +
                    $"diferença {100.0 * (with.Wins - without.Wins) / N:+0;-0;0} p.p. | ESPELHO com-vs-sem {mirror.Wins}/{N} ({Pct(mirror.Wins, N)}) {mirror.AvgSeconds:0.0}s" +
                    (with.Unfinished + without.Unfinished + mirror.Unfinished > 0 ? $" | sem fim: {with.Unfinished}/{without.Unfinished}/{mirror.Unfinished}" : ""));
            }
        }

        /// <summary>Tarefa 1 calibrada: mesma composição de controle, mas com o NÍVEL do controle ajustado
        /// (busca binária, 10 lutas por passo) até a versão SEM passiva ficar perto de 50% - só assim um
        /// controle fixo consegue mostrar diferença. Depois mede 30 com e 30 sem nesse nível.</summary>
        private async Task Task1Calibrated()
        {
            const int N = 30;
            Out("=== TAREFA 1 (calibrada): nível do controle Ultimate ajustado por rota até SEM ~ 50% ===");
            string only = OS.GetEnvironment("DEBUG_1C_ROUTES") ?? "";
            foreach (var (name, chain) in Task1Routes)
            {
                if (only.Length > 0 && !only.Split(',').Any(r => name.Contains(r)))
                    continue;

                var proto = BuildRoute(chain, Task1Level);

                // Nível da virada: o mais alto em que o SEM ainda vence pelo menos metade (a virada é
                // brusca - de 10/10 pra 0/10 em poucos níveis -, então "o mais perto de 50%" não serve).
                int lo = 1, hi = 90, best = 1;
                while (lo <= hi)
                {
                    int mid = (lo + hi) / 2;
                    int lvl = mid;
                    var probe = await Series(() => Enumerable.Range(0, 3).Select(_ => Clone(proto, false)).ToList(), () => Team(ControlUltimate, lvl), 10);
                    if (probe.Wins >= 5) { best = mid; lo = mid + 1; } else hi = mid - 1;
                }

                int L = best;
                var with = await Series(() => Enumerable.Range(0, 3).Select(_ => Clone(proto)).ToList(), () => Team(ControlUltimate, L), N);
                var without = await Series(() => Enumerable.Range(0, 3).Select(_ => Clone(proto, false)).ToList(), () => Team(ControlUltimate, L), N);
                Out($"T1C | {name} | controle no nível {L} | COM {with.Wins}/{N} ({Pct(with.Wins, N)}) {with.AvgSeconds:0.0}s | SEM {without.Wins}/{N} ({Pct(without.Wins, N)}) {without.AvgSeconds:0.0}s | diferença {100.0 * (with.Wins - without.Wins) / N:+0;-0;0} p.p.");
            }
        }

        /// <summary>Diagnóstico: rotas que perdem até pro controle em nível muito baixo.</summary>
        private async Task Diagnose()
        {
            foreach (var (name, chain) in Task1Routes.Where(r => r.Name.Contains("G1") || r.Name.Contains("G2") || r.Name.Contains("G3")))
            {
                var proto = BuildRoute(chain, Task1Level);
                foreach (int lvl in new[] { 1, 5, 10 })
                {
                    int wins = 0; double secs = 0; string last = "";
                    for (int i = 0; i < 5; i++)
                    {
                        var team = Enumerable.Range(0, 3).Select(_ => Clone(proto, false)).ToList();
                        var enemies = Team(ControlUltimate, lvl);
                        var r = await Fight(team, enemies);
                        if (r.Won) wins++;
                        secs += r.Seconds;
                        last = $"jogador HP [{string.Join(",", team.Select(t => $"{t.CurrentHealthPoints}/{t.MaxHealthPoints}"))}] controle HP [{string.Join(",", enemies.Select(t => $"{t.CurrentHealthPoints}/{t.MaxHealthPoints}"))}]";
                    }
                    var e0 = Team(ControlUltimate, lvl);
                    Out($"DIAG | {name} | controle nível {lvl} (Zudomon HP{e0[0].MaxHealthPoints} PD{e0[0].CurrentStats.PhysicalDefense} ED{e0[0].CurrentStats.SpecialDefense}) | vitórias {wins}/5, {secs / 5:0.0}s | última luta: {last}");
                }
            }
        }

        /// <summary>Fase 1d: o ganho do 4º golpe do Devimon nunca passa do teto do Rompe-Guarda, numa grade de
        /// ataque x defesa; e a constante nova tem o mesmo valor da privada do Rompe-Guarda.</summary>
        private void DevimonCapSweep()
        {
            var rgField = typeof(DamageCalculator).GetField("RompeGuardaMaxGainRatio", BindingFlags.NonPublic | BindingFlags.Static);
            float rg = rgField != null ? (float)rgField.GetValue(null) : float.NaN;
            Out($"T8 | constante | Rompe-Guarda {rg} | Devimon {InheritedPassiveSystem.DefensePierceMaxGainRatio} | iguais: {rg == InheritedPassiveSystem.DefensePierceMaxGainRatio}");

            int cases = 0, violations = 0, capped = 0, maxGainSeen = 0;
            string worst = "";
            foreach (int atk in new[] { 10, 50, 100, 150, 200, 300, 500, 646, 800, 1000, 2000 })
            foreach (int def in new[] { 1, 10, 50, 100, 150, 200, 290, 400, 500, 650, 800, 1000, 1500, 2500 })
            {
                var attacker = new BattleCombatant(new DigimonInstance(Sp(105)) { EvolutionHistory = { 45 } });
                attacker.Digimon.BaseData.AttackType = AttackType.Physical;
                attacker.Digimon.CurrentStats.PhysicalDamage = atk;
                var defender = new BattleCombatant(new DigimonInstance(Sp(68)));
                defender.Digimon.CurrentStats.PhysicalDefense = def;

                int baseDamage = Math.Max(1, atk - def);
                int fourth = 0;
                for (int i = 0; i < 4; i++)
                    fourth = InheritedPassiveSystem.ModifyBaseDamage(baseDamage, attacker, defender);

                int gain = fourth - baseDamage;
                int cap = (int)(baseDamage * InheritedPassiveSystem.DefensePierceMaxGainRatio);
                int uncapped = Math.Max(1, atk - (int)(def * 0.8f)) - baseDamage;
                cases++;
                if (gain > cap) { violations++; worst = $"ataque {atk} defesa {def}: ganho {gain} > teto {cap}"; }
                if (uncapped > cap) capped++;
                maxGainSeen = Math.Max(maxGainSeen, gain);
            }
            Out($"T8 | varredura | {cases} combinações ataque x defesa | ganho acima do teto: {violations}{(violations > 0 ? " (" + worst + ")" : "")} | casos em que o teto cortou o ganho: {capped}");

            foreach (var (atk, def) in new[] { (300, 290), (646, 600), (500, 400), (1000, 500), (1000, 100) })
            {
                var attacker = new BattleCombatant(new DigimonInstance(Sp(105)) { EvolutionHistory = { 45 } });
                attacker.Digimon.BaseData.AttackType = AttackType.Physical;
                attacker.Digimon.CurrentStats.PhysicalDamage = atk;
                var defender = new BattleCombatant(new DigimonInstance(Sp(68)));
                defender.Digimon.CurrentStats.PhysicalDefense = def;
                int baseDamage = Math.Max(1, atk - def);
                int fourth = 0;
                for (int i = 0; i < 4; i++)
                    fourth = InheritedPassiveSystem.ModifyBaseDamage(baseDamage, attacker, defender);
                int old = Math.Max(1, atk - (int)(def * 0.8f));
                Out($"T8 | exemplo | ataque {atk} x defesa {def} | dano base {baseDamage} | 4º golpe sem teto (1b) {old} ({(double)old / baseDamage:0.##}x) | com teto (1d) {fourth} ({(double)fourth / baseDamage:0.##}x)");
            }
        }

        // ================================================================ tarefa 2 - quanto cada passiva dispara

        /// <summary>Espécie da passiva -> Mega descendente usado como portador (a forma "natural" de quem
        /// carrega aquela passiva no fim da linha).</summary>
        private static readonly (int Species, int Carrier)[] Task2Carriers =
        {
            (25, 52), (26, 53), (83, 52), (63, 119), (183, 52), (29, 52), (30, 53), (64, 53),
            (1, 52), (2, 53), (13, 52), (45, 106), (85, 88), (14, 53), (46, 119), (39, 52),
            (167, 168), (180, 95), (86, 87), (125, 121), (182, 106), (181, 194), (111, 120),
            (40, 53), (48, 176), (118, 113), (127, 119), (128, 123),
        };

        private sealed class Watch
        {
            public BattleCombatant Bearer;
            public int LastAttacks;
            public bool HadDebuff;
            public bool AllyHurt;
            public int LowHpAt = -1;
            public int LastStacks;
            public int ForcedCrits;
            public bool Fired;
        }

        private static bool IsPhysical(BattleCombatant c) => c.Digimon.BaseData.AttackType == AttackType.Physical;

        /// <summary>Checagem de "disparou" por espécie, a cada quadro, só olhando o estado público da luta.</summary>
        private static void Observe(int species, Watch w, BattleMatch m, int garurumonHits)
        {
            var b = w.Bearer;
            var st = b.Inherited;
            var enemies = m.EnemyTeam.Members;
            var allies = m.PlayerTeam.Members.Where(c => c != b).ToList();
            bool attacked = st.AttacksDealt > w.LastAttacks;

            switch (species)
            {
                case 83: case 64: case 128: w.Fired |= st.HasActed; break;                                   // Velocidade / Belphemon
                case 63: w.Fired |= st.HitsTaken > 0 && enemies.Any(e => !e.IsDead && !IsPhysical(e)); break; // ED
                case 30: w.Fired |= st.HitsTaken > 0 && enemies.Any(e => !e.IsDead && IsPhysical(e)); break;  // PD
                case 183: case 2: case 1: w.Fired |= IsPhysical(b) && st.AttacksDealt > 0; break;             // PA / dano físico
                case 25: case 26: case 29: case 46: w.Fired |= st.HitsTaken > 0; break;                       // HP máx / IceDevimon
                case 13: w.Fired |= attacked && st.LastAttackCrit && w.HadDebuff; break;                       // Greymon
                case 45: w.Fired |= st.AttacksDealt >= 4; break;                                               // Devimon
                case 85: w.Fired |= attacked && enemies.Any(e => e.Inherited.HasActed); break;                 // Dimetromon
                case 14: w.Fired |= garurumonHits > 0; break;                                                  // Garurumon
                case 39:                                                                                         // MetalGreymon
                    if (st.LowHpBonusActive && w.LowHpAt < 0) w.LowHpAt = st.AttacksDealt;
                    w.Fired |= w.LowHpAt >= 0 && st.AttacksDealt > w.LowHpAt && IsPhysical(b);
                    break;
                case 167: case 125: w.Fired |= attacked && st.LastAttackCrit; break;                           // crítico cura / drena
                case 180: w.Fired |= st.KillSpeedBonus > 0; break;                                             // LadyDevimon
                case 86: w.Fired |= st.GuaranteedCritPending || enemies.Any(e => e.Inherited.LastAttackDodged); break; // Lamiamon
                case 182: w.Fired |= attacked && w.AllyHurt; break;                                            // Myotismon
                case 181: w.Fired |= (attacked || st.HitsTaken > 0) && enemies.Any(e => !e.IsDead && e.Digimon.BaseData.Role == RoleType.Tank); break; // SkullSatamon
                case 111: w.Fired |= enemies.Any(e => e.Inherited.LastAttackNegated); break;                   // Triceramon
                case 40: w.Fired |= IsPhysical(b) && st.AttacksDealt >= 1; break;                              // WereGarurumon
                case 48: w.Fired |= st.AttacksDealt >= 1; break;                                               // MachGaogamon
                case 118:                                                                                        // Destromon
                    if (w.LastStacks > 0 && st.RangedStacks == 0) w.ForcedCrits++;
                    w.Fired |= w.ForcedCrits > 0;
                    break;
                case 127: w.Fired |= enemies.Any(e => e.Inherited.StunRemainingSeconds > 0); break;            // Astamon
            }

            w.LastAttacks = st.AttacksDealt;
            w.LastStacks = st.RangedStacks;
            w.HadDebuff = b.Effects.Any(e => e.Percentage < 0);
            w.AllyHurt = allies.Any(a => !a.IsDead && a.Digimon.CurrentHealthPoints < a.Digimon.MaxHealthPoints);
        }

        /// <summary>Envolve o sorteio das herdadas pra contar quantos golpes extras do Garurumon saíram (o
        /// único efeito sem rastro no estado). Não muda o resultado do sorteio.</summary>
        private static int _garurumonHits;
        private static Func<double> _realRoll;

        private static void HookRoll()
        {
            _realRoll ??= InheritedPassiveSystem.Roll;
            InheritedPassiveSystem.Roll = () =>
            {
                double v = _realRoll();
                if (v < 0.15 && new System.Diagnostics.StackTrace().GetFrames().Any(f => f.GetMethod()?.Name == "RollExtraStrike"))
                    _garurumonHits++;
                return v;
            };
        }

        private static void UnhookRoll()
        {
            if (_realRoll != null)
                InheritedPassiveSystem.Roll = _realRoll;
        }

        private async Task<(int FiredBattles, int Wins, double AvgSeconds, int ForcedCrits, int GarurumonHits, string CarrierInfo)> CarrierSeries(int species, int carrierId, int[] history, int n)
        {
            int fired = 0, wins = 0, forced = 0, garu = 0;
            double secs = 0;
            string info = "";
            HookRoll();
            try
            {
                for (int i = 0; i < n; i++)
                {
                    var bearer = Make(carrierId, MegaLevel);
                    bearer.EvolutionHistory.AddRange(history);
                    info = $"{bearer.BaseData.Name} ({bearer.BaseData.Role}, {bearer.BaseData.AttackType})";
                    var team = new List<DigimonInstance> { bearer };
                    team.AddRange(Team(AlliesMega, MegaLevel));

                    _garurumonHits = 0;
                    Watch w = null;
                    var r = await Fight(team, Team(ControlMega, MegaLevel), m =>
                    {
                        w ??= new Watch { Bearer = m.PlayerTeam.Members[0] };
                        Observe(species, w, m, _garurumonHits);
                    });

                    if (w?.Fired == true) fired++;
                    if (r.Won) wins++;
                    secs += r.Seconds;
                    forced += w?.ForcedCrits ?? 0;
                    garu += _garurumonHits;
                }
            }
            finally
            {
                UnhookRoll();
            }
            return (fired, wins, secs / n, forced, garu, info);
        }

        private async Task Task2()
        {
            const int N = 10;
            Out($"=== TAREFA 2: em quantas de {N} batalhas reais cada passiva disparou (portador Mega nível {MegaLevel} só com aquela espécie no histórico + Craniummon + Sakuyamon vs Vikemon/Kentaurusmon/Magnamon) ===");
            foreach (var (species, carrier) in Task2Carriers)
            {
                var r = await CarrierSeries(species, carrier, new[] { species }, N);
                var p = Sp(species).InheritedPassive;
                Out($"T2 | {Sp(species).Name} ({species}) | {InheritedPassiveSystem.Describe(p)} | portador {r.CarrierInfo} | disparou em {r.FiredBattles}/{N}" +
                    (species == 118 ? $" | críticos forçados {r.ForcedCrits}" : "") + (species == 14 ? $" | golpes extras {r.GarurumonHits}" : ""));
            }
        }

        // ================================================================ tarefa 3 - rotas "mortas"

        private async Task Task3()
        {
            const int N = 20;
            Out($"=== TAREFA 3: bônus de Ranged do Garurumon/Destromon nas rotas reais ({N} batalhas cada, vs controle Mega/Ultimate) ===");

            var cases = new (string Name, int Carrier, int[] History, int Species, bool Ultimate)[]
            {
                ("Garurumon > WereGarurumon (Ultimate)", 40, new[] { 26, 30, 2, 14 }, 14, true),
                ("Garurumon > MachGaogamon (Ultimate)", 48, new[] { 26, 30, 2, 14 }, 14, true),
                ("... > WereGarurumon > MetalGarurumon (Mega)", 53, new[] { 26, 30, 2, 14, 40 }, 14, false),
                ("... > WereGarurumon > Gankoomon (Mega)", 162, new[] { 26, 30, 2, 14, 40 }, 14, false),
                ("... > MachGaogamon > MirageGaogamon (Mega)", 176, new[] { 26, 30, 2, 14, 48 }, 14, false),
                ("... > Destromon > Demon (Mega)", 105, new[] { 63, 64, 2, 46, 118 }, 118, false),
                ("... > Destromon > Boltmon (Mega)", 113, new[] { 63, 64, 2, 46, 118 }, 118, false),
                ("... > Destromon > Belphemon Sleep (Mega)", 128, new[] { 63, 64, 2, 46, 118 }, 118, false),
            };

            foreach (var c in cases)
            {
                int fired = 0, forced = 0, garu = 0, rangedAttacks = 0;
                HookRoll();
                try
                {
                    for (int i = 0; i < N; i++)
                    {
                        int level = c.Ultimate ? Task1Level : MegaLevel;
                        var bearer = Make(c.Carrier, level);
                        bearer.EvolutionHistory.AddRange(c.History);
                        var team = new List<DigimonInstance> { bearer };
                        team.AddRange(c.Ultimate
                            ? new[] { Make(147, level, PassiveType.Couraca), Make(50, level, PassiveType.PosturaFirme) }
                            : Team(AlliesMega, level).ToArray());

                        _garurumonHits = 0;
                        Watch w = null;
                        await Fight(team, c.Ultimate ? Team(ControlUltimate, level) : Team(ControlMega, level), m =>
                        {
                            w ??= new Watch { Bearer = m.PlayerTeam.Members[0] };
                            Observe(c.Species, w, m, _garurumonHits);
                        });

                        if (w?.Fired == true) fired++;
                        forced += w?.ForcedCrits ?? 0;
                        garu += _garurumonHits;
                        if (w != null && w.Bearer.Digimon.BaseData.Role == RoleType.Ranged)
                            rangedAttacks += w.Bearer.Inherited.AttacksDealt;
                    }
                }
                finally
                {
                    UnhookRoll();
                }

                var carrier = Sp(c.Carrier);
                Out($"T3 | {c.Name} | forma final {carrier.Name} ({carrier.Role}) | passiva de {Sp(c.Species).Name} | " +
                    $"disparou em {fired}/{N} batalhas | ataques à distância do portador: {rangedAttacks} | " +
                    (c.Species == 14 ? $"golpes extras: {garu}" : $"críticos forçados: {forced}"));
            }

            // Referência: o mesmo efeito com um portador Ranged, pra mostrar que a passiva funciona quando a forma é Ranged.
            foreach (var (name, carrier, history, species) in new[]
            {
                ("referência: Garurumon no histórico de um Ranged (Seraphimon)", 56, new[] { 14 }, 14),
                ("referência: Destromon no histórico de um Ranged (Seraphimon)", 56, new[] { 118 }, 118),
            })
            {
                var r = await CarrierSeries(species, carrier, history, N);
                Out($"T3 | {name} | disparou em {r.FiredBattles}/{N} | " + (species == 14 ? $"golpes extras {r.GarurumonHits}" : $"críticos forçados {r.ForcedCrits}"));
            }
        }

        // ================================================================ tarefa 4 - irmãs, mesmo estágio final

        private async Task Task4()
        {
            const int N = 30;
            Out($"=== TAREFA 4: mesma forma final (Mega nível {MegaLevel}), históricos diferentes - {N} batalhas cada, 3 clones vs controle Mega ===");

            var pairs = new (string Final, int[] RouteA, string NameA, int[] RouteB, string NameB)[]
            {
                ("WarGreymon", new[] { 25, 29, 1, 13, 39, 52 }, "via MetalGreymon", new[] { 25, 29, 1, 13, 167, 52 }, "via RizeGreymon"),
                ("Gankoomon", new[] { 26, 30, 2, 14, 40, 162 }, "via WereGarurumon", new[] { 26, 30, 2, 14, 48, 162 }, "via MachGaogamon"),
                ("Demon", new[] { 25, 29, 1, 45, 181, 105 }, "via SkullSatamon", new[] { 25, 29, 1, 45, 125, 105 }, "via Lucemon FM"),
            };

            // Nota: duas Ultimate irmãs do mesmo Champion carregam o MESMO histórico (a passiva delas só entra na próxima evolução).
            var mgU = BuildRoute(new[] { 25, 29, 1, 13, 39 }, Task1Level);
            var rgU = BuildRoute(new[] { 25, 29, 1, 13, 167 }, Task1Level);
            Out($"T4 | nota | MetalGreymon e RizeGreymon (Ultimate, irmãs de Greymon): herdadas [{string.Join(", ", InheritedPassiveSystem.GetActiveInheritedPassives(mgU).Select(p => p.Species.Name))}] vs [{string.Join(", ", InheritedPassiveSystem.GetActiveInheritedPassives(rgU).Select(p => p.Species.Name))}]");

            string onlyPair = OS.GetEnvironment("DEBUG_1C_PAIRS") ?? "";
            foreach (var (final, routeA, nameA, routeB, nameB) in pairs)
            {
                if (onlyPair.Length > 0 && !onlyPair.Split(',').Contains(final))
                    continue;
                var realA = BuildRoute(routeA, MegaLevel);
                var realB = BuildRoute(routeB, MegaLevel);

                // (a) só a passiva muda: os mesmos stats (os de A), troca só o histórico.
                var sameStatsB = Clone(realA, false);
                sameStatsB.EvolutionHistory.AddRange(realB.EvolutionHistory);

                var a1 = await Series(() => Enumerable.Range(0, 3).Select(_ => Clone(realA)).ToList(), () => Team(ControlMega, MegaLevel), N);
                var b1 = await Series(() => Enumerable.Range(0, 3).Select(_ => Clone(sameStatsB)).ToList(), () => Team(ControlMega, MegaLevel), N);
                var none = await Series(() => Enumerable.Range(0, 3).Select(_ => Clone(realA, false)).ToList(), () => Team(ControlMega, MegaLevel), N);
                // (b) rota real: stats de cada rota + passivas de cada rota.
                var b2 = await Series(() => Enumerable.Range(0, 3).Select(_ => Clone(realB)).ToList(), () => Team(ControlMega, MegaLevel), N);
                // (c) espelho direto A vs B (mesmos stats)
                var duel = await Series(() => Enumerable.Range(0, 3).Select(_ => Clone(realA)).ToList(),
                                        () => Enumerable.Range(0, 3).Select(_ => Clone(sameStatsB)).ToList(), N);

                string Stats(DigimonInstance d) => $"HP{d.CurrentStats.HealthPoints} PA{d.CurrentStats.PhysicalDamage} PD{d.CurrentStats.PhysicalDefense} EA{d.CurrentStats.SpecialDamage} ED{d.CurrentStats.SpecialDefense} Vel{d.CurrentStats.Speed}";
                Out($"T4 | {final} | {nameA}: {a1.Wins}/{N} ({Pct(a1.Wins, N)}) {a1.AvgSeconds:0.0}s | {nameB} (mesmos stats): {b1.Wins}/{N} ({Pct(b1.Wins, N)}) {b1.AvgSeconds:0.0}s | " +
                    $"sem herdadas: {none.Wins}/{N} ({Pct(none.Wins, N)}) | {nameB} (stats da própria rota): {b2.Wins}/{N} ({Pct(b2.Wins, N)}) | " +
                    $"duelo {nameA} vs {nameB}: {duel.Wins}/{N} ({Pct(duel.Wins, N)}) | stats A {Stats(realA)} | stats B {Stats(realB)}");
            }
        }

        // ================================================================ tarefa 5 - Center simulado por dias

        private static void ResetScratch()
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
            save.Center.Bits = 100_000;
            save.Center.CapacityLimit = 100;
        }

        private void Task5()
        {
            Out("=== TAREFA 5: Center simulado - Digimons nascem e evoluem pela virada de dia real (GameManager.OnDayPassed -> TryToEvolve) ===");
            ResetScratch();

            // O "jogador" treina os stats da próxima forma da rota e usa o bloqueio de evolução (Wizardmon) nas
            // irmãs; a idade corre de verdade (nada força AgeInDays) e a evolução só acontece na virada do dia.
            var plans = new List<(DigimonInstance D, int[] Route)>
            {
                (Make(25, 1), new[] { 25, 29, 1, 13, 39, 52 }),     // Botamon -> WarGreymon
                (Make(26, 1), new[] { 26, 30, 2, 14, 40, 53 }),     // Punimon -> MetalGarurumon
                (Make(63, 1), new[] { 63, 64, 2, 46, 127, 119 }),   // Pichimon -> Ragnamon
            };
            foreach (var (d, _) in plans)
                Gm.CenterService.AddDigimon(d);

            for (int day = 1; day <= 90; day++)
            {
                foreach (var (d, route) in plans)
                {
                    if (!Gm.Save.Center.Digimons.Contains(d))
                        continue;
                    int idx = Array.IndexOf(route, d.BaseData.Id);
                    if (idx < 0 || idx == route.Length - 1)
                        continue;

                    var edges = Db.GetEvolutionsFrom(d.BaseData.Id);
                    d.BlockedEvolutionTargetIds.Clear();
                    d.BlockedEvolutionTargetIds.AddRange(edges.Where(e => e.ToDigimonId != route[idx + 1]).Select(e => e.ToDigimonId));
                    Satisfy(d, edges.First(e => e.ToDigimonId == route[idx + 1]), touchAge: false);
                }

                foreach (var d in Gm.Save.Center.Digimons)
                {
                    d.Hunger = d.MaxHunger;
                    d.Stamina = d.MaxStamina;
                    d.RestoreHealth();
                    if (d.HealthState == HealthState.Sick)
                        d.Heal(); // remédio, como o jogador faria
                }

                Gm.ClockSystem.Update(1440.0);

                if (plans.All(p => !Gm.Save.Center.Digimons.Contains(p.D) || p.D.BaseData.Id == p.Route[^1]))
                {
                    Out($"T5 | todas as rotas chegaram ao fim no dia {day}");
                    break;
                }
            }

            foreach (var (d, route) in plans)
            {
                bool alive = Gm.Save.Center.Digimons.Contains(d);
                var passives = InheritedPassiveSystem.GetActiveInheritedPassives(d);
                Out($"T5 | {Names(route)} | {(alive ? "vivo" : "MORREU")} | forma atual {d.BaseData.Name} ({d.BaseData.Stage}), idade {d.AgeInDays}d, nível {d.Level} | " +
                    $"EvolutionHistory {Names(d.EvolutionHistory)} | ficha: \"Passivas herdadas ({passives.Count})\" -> " +
                    string.Join(" / ", passives.Select(p => $"{p.Species.Name}: {InheritedPassiveSystem.Describe(p.Passive)}")));
            }

            // Bônus: Jogress das duas linhas (Omegamon), com a linha do Agumon como principal.
            var war = plans[0].D; var metal = plans[1].D;
            if (Gm.Save.Center.Digimons.Contains(war) && Gm.Save.Center.Digimons.Contains(metal) && war.BaseData.Id == 52 && metal.BaseData.Id == 53)
            {
                var res = Gm.TryToFuse(war, metal);
                if (res.FusedDigimon != null)
                {
                    var p = InheritedPassiveSystem.GetActiveInheritedPassives(res.FusedDigimon);
                    Out($"T5 | Jogress Omegamon (Agumon principal) | EvolutionHistory {Names(res.FusedDigimon.EvolutionHistory)} | ficha: \"Passivas herdadas ({p.Count})\" -> " +
                        string.Join(" / ", p.Select(x => $"{x.Species.Name}: {InheritedPassiveSystem.Describe(x.Passive)}")));
                }
            }
        }
    }
}
