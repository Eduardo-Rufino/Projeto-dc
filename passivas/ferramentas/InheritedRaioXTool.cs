using Godot;
using ProjetoDC.Enums;
using ProjetoDC.Scripts.Data;
using ProjetoDC.Scripts.Gameplay;
using ProjetoDC.Scripts.Managers;
using ProjetoDC.Scripts.Systems.Battle;
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
    /// Fase 4c das passivas herdadas: MEDIÇÃO (mesma infraestrutura da Fase 1c, InheritedImpactTool), sem
    /// mudar nada do jogo. Imprime [4C] no log. Salvamento desligado, igual ao GameAudit. Uso:
    ///   godot --headless --fixed-fps 60 --path . res://passivas/ferramentas/InheritedRaioXTool.tscn
    /// DEBUG_4C_TASKS=3,4 restringe às tarefas; DEBUG_4C_ONLY=ids restringe a tarefa 4 a essas espécies.
    /// </summary>
    public partial class InheritedRaioXTool : Node
    {
        private const int UltimateLevel = 30;
        private const int MegaLevel = 45;

        private static readonly BindingFlags NonPublic = BindingFlags.NonPublic | BindingFlags.Instance;
        private static DatabaseManager Db => DatabaseManager.Instance;
        private static GameManager Gm => GameManager.Instance;

        private static readonly (int Id, PassiveType Passive)[] ControlUltimate =
        {
            (67, PassiveType.Couraca), (42, PassiveType.AlvoMarcado), (98, PassiveType.GolpePesado),
        };

        private static readonly (int Id, PassiveType Passive)[] ControlMega =
        {
            (68, PassiveType.Couraca), (144, PassiveType.AlvoMarcado), (143, PassiveType.GolpePesado),
        };

        private static readonly (int Id, PassiveType Passive)[] AlliesMega =
        {
            (159, PassiveType.Couraca), (175, PassiveType.PosturaFirme),
        };

        public override void _Ready() => CallDeferred(nameof(Start));

        private async void Start()
        {
            await Frames(45);
            typeof(GameManager).GetField("_hasActiveGame", NonPublic)?.SetValue(Gm, false);
            Gm.IgnoreCalendar = false;

            var wanted = (OS.GetEnvironment("DEBUG_4C_TASKS") ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet();
            bool Run(string t) => wanted.Count == 0 || wanted.Contains(t);

            try
            {
                if (Run("3")) await Task3();
                if (Run("4")) await Task4();
            }
            catch (Exception e)
            {
                GD.PrintErr($"[4C] EXCEÇÃO: {e}");
            }

            Out("FIM");
            GetTree().Quit();
        }

        // ================================================================ infraestrutura (igual à 1c)

        private static void Out(string s) => GD.Print("[4C] " + s);

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

        private static void Satisfy(DigimonInstance d, EvolutionData e)
        {
            while (d.Level < e.RequiredLevel)
                d.GainExperience(d.ExperienceToNextLevel);
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

        // ================================================================ tarefa 3 - impacto, 6 rotas novas

        private static readonly (string Name, int[] Chain)[] Routes =
        {
            ("Tank: Knightmon", new[] { 185, 186, 163, 164, 147 }),          // Dodomon > Dorimon > Hackmon > BaoHackmon
            ("Warrior: WarGrowlmon", new[] { 185, 33, 84, 47, 41 }),         // Dodomon > Gigimon > Elizamon > Devidramon
            ("Ranged especial: Lillymon", new[] { 189, 190, 12, 24, 50 }),   // Nyokimon > Budmon > Lalamon > Sunflowmon
            ("Ranged físico: Wingdramon", new[] { 172, 173, 132, 136, 137 }),// Puttimon > Babydmon > Dracomon > Coredramon (Blue)
            ("Assassin: Fantomon", new[] { 28, 90, 116, 103, 104 }),         // Keemon > Kyaromon > BEMmon > Bakemon
            ("Support: Chaperomon", new[] { 28, 38, 91, 92, 93 }),           // Keemon > Yaamon > Shoemon > ShoeShoemon
        };

        private async Task Task3()
        {
            const int N = 30;
            Out($"=== TAREFA 3: 6 rotas novas até Ultimate (nível {UltimateLevel}), 3 clones vs controle Ultimate calibrado ===");
            string only = OS.GetEnvironment("DEBUG_4C_ROUTES") ?? "";
            foreach (var (name, chain) in Routes)
            {
                if (only.Length > 0 && !only.Split(',').Any(r => name.Contains(r)))
                    continue;

                var proto = BuildRoute(chain, UltimateLevel);
                var actives = InheritedPassiveSystem.GetActiveInheritedPassives(proto);

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
                var s = proto.CurrentStats;
                Out($"T3 | {name} | {Names(chain)} | forma {proto.BaseData.Name} ({proto.BaseData.Role}, {proto.BaseData.AttackType}) | " +
                    $"herdadas: {string.Join(" / ", actives.Select(p => $"{p.Species.Name}: {InheritedPassiveSystem.Describe(p.Passive)}"))} | " +
                    $"stats HP{s.HealthPoints} PA{s.PhysicalDamage} PD{s.PhysicalDefense} EA{s.SpecialDamage} ED{s.SpecialDefense} Vel{s.Speed} | " +
                    $"controle nível {L} | COM {with.Wins}/{N} ({Pct(with.Wins, N)}) {with.AvgSeconds:0.0}s | SEM {without.Wins}/{N} ({Pct(without.Wins, N)}) {without.AvgSeconds:0.0}s | " +
                    $"diferença {100.0 * (with.Wins - without.Wins) / N:+0;-0;0} p.p." +
                    (with.Unfinished + without.Unfinished > 0 ? $" | sem fim: {with.Unfinished}/{without.Unfinished}" : ""));
            }
        }

        // ================================================================ tarefa 4 - disparo das 56 da Fase 4

        /// <summary>Espécie -> Mega descendente mais comum nas cadeias reais (empate: menor id).</summary>
        private static readonly (int Species, int Carrier)[] Carriers =
        {
            (15, 74), (16, 73), (17, 75), (18, 52), (19, 55), (20, 122), (21, 194), (23, 162), (24, 75), (44, 52),
            (47, 54), (60, 62), (66, 68), (79, 160), (92, 82), (97, 62), (103, 194), (110, 112), (117, 105), (130, 144),
            (133, 135), (136, 138), (140, 138), (146, 148), (150, 155), (153, 152), (156, 155), (164, 148), (191, 142), (198, 142),
            (41, 54), (42, 55), (43, 56), (49, 161), (50, 75), (51, 74), (61, 62), (67, 68), (72, 73), (80, 81),
            (93, 82), (98, 62), (104, 113), (131, 144), (134, 135), (137, 138), (141, 142), (147, 148), (151, 152), (154, 152),
            (157, 155), (165, 162), (170, 122), (179, 75), (192, 142), (199, 74),
        };

        private sealed class Watch
        {
            public BattleCombatant Bearer;
            public int LastAttacks;
            public bool HadDebuff;
            public bool AllyHurt;
            public int LowHpAt = -1;
            public int LastStacks;
            public bool WasStunned;
            public int Events;
            public bool Fired;
        }

        private static bool IsPhysical(BattleCombatant c) => c.Digimon.BaseData.AttackType == AttackType.Physical;

        /// <summary>"Disparou" por TIPO de efeito, lido do estado público da luta a cada quadro (mesmos
        /// critérios da tarefa 2 da 1c). Events conta ocorrências quando dá pra contar.</summary>
        private static void Observe(InheritedPassiveData p, Watch w, BattleMatch m, int extraHits)
        {
            var b = w.Bearer;
            var st = b.Inherited;
            var enemies = m.EnemyTeam.Members;
            bool attacked = st.AttacksDealt > w.LastAttacks;

            switch (p.Type)
            {
                case InheritedEffectType.SpeedPercent:
                    w.Fired |= st.HasActed; break;
                case InheritedEffectType.MaxHpPercent:
                case InheritedEffectType.FirstHitTakenReduction:
                case InheritedEffectType.DefensesUpSpeedDown:
                    w.Fired |= st.HitsTaken > 0; break;
                case InheritedEffectType.PhysicalDefensePercent:
                    w.Fired |= st.HitsTaken > 0 && enemies.Any(e => !e.IsDead && IsPhysical(e)); break;
                case InheritedEffectType.SpecialDefensePercent:
                    w.Fired |= st.HitsTaken > 0 && enemies.Any(e => !e.IsDead && !IsPhysical(e)); break;
                case InheritedEffectType.PhysicalAttackPercent:
                case InheritedEffectType.PhysicalDamageDealtPercent:
                case InheritedEffectType.PhysicalDamageWithOpeningBonus:
                    w.Fired |= IsPhysical(b) && st.AttacksDealt > 0; break;
                case InheritedEffectType.SpecialAttackPercent:
                    w.Fired |= !IsPhysical(b) && st.AttacksDealt > 0; break;
                case InheritedEffectType.OpeningHitBonus:
                    w.Fired |= st.AttacksDealt >= 1; break;
                case InheritedEffectType.CritShortensDebuffs:
                    if (attacked && st.LastAttackCrit && w.HadDebuff) { w.Fired = true; w.Events++; }
                    break;
                case InheritedEffectType.PeriodicDefensePierce:
                    w.Fired |= st.AttacksDealt >= (int)p.Value + 1;
                    w.Events = st.AttacksDealt / ((int)p.Value + 1);
                    break;
                case InheritedEffectType.SpeedAndBonusVsActedTarget:
                    w.Fired |= attacked && enemies.Any(e => e.Inherited.HasActed); break;
                case InheritedEffectType.RangedExtraStrike:
                    w.Events = extraHits; w.Fired |= extraHits > 0; break;
                case InheritedEffectType.LowHpPhysicalDamageBonus:
                    if (st.LowHpBonusActive && w.LowHpAt < 0) w.LowHpAt = st.AttacksDealt;
                    w.Fired |= w.LowHpAt >= 0 && st.AttacksDealt > w.LowHpAt && IsPhysical(b);
                    break;
                case InheritedEffectType.CritHealMaxHp:
                case InheritedEffectType.CritLifesteal:
                    if (attacked && st.LastAttackCrit) { w.Fired = true; w.Events++; }
                    break;
                case InheritedEffectType.KillSpeedBonus:
                    w.Fired |= st.KillSpeedBonus > 0;
                    w.Events = (int)Math.Round(st.KillSpeedBonus / p.Value);
                    break;
                case InheritedEffectType.DodgeThenGuaranteedCrit:
                    w.Fired |= st.GuaranteedCritPending || enemies.Any(e => e.Inherited.LastAttackDodged); break;
                case InheritedEffectType.HealLowestAllyOnAttack:
                    if (attacked && w.AllyHurt) { w.Fired = true; w.Events++; }
                    break;
                case InheritedEffectType.AntiTank:
                    w.Fired |= (attacked || st.HitsTaken > 0) && enemies.Any(e => !e.IsDead && e.Digimon.BaseData.Role == RoleType.Tank); break;
                case InheritedEffectType.NegateDamageChance:
                    w.Fired |= enemies.Any(e => e.Inherited.LastAttackNegated); break;
                case InheritedEffectType.RangedStacksGuaranteedCrit:
                    if (w.LastStacks > 0 && st.RangedStacks == 0) w.Events++;
                    w.Fired |= w.Events > 0;
                    break;
                case InheritedEffectType.StunChance:
                    bool stunned = enemies.Any(e => e.Inherited.StunRemainingSeconds > 0);
                    if (stunned && !w.WasStunned) w.Events++;
                    w.WasStunned = stunned;
                    w.Fired |= stunned;
                    break;
            }

            w.LastAttacks = st.AttacksDealt;
            w.LastStacks = st.RangedStacks;
            w.HadDebuff = b.Effects.Any(e => e.Percentage < 0);
            w.AllyHurt = m.PlayerTeam.Members.Any(a => a != b && !a.IsDead && a.Digimon.CurrentHealthPoints < a.Digimon.MaxHealthPoints);
        }

        // Conta os golpes extras (RangedExtraStrike) envolvendo o sorteio, sem mudar o resultado. Só o
        // portador tem herdadas na luta, então todo sorteio de dentro de RollExtraStrike é dele.
        private static int _extraHits;
        private static float _extraChance;
        private static Func<double> _realRoll;

        private static void HookRoll(float extraChance)
        {
            _realRoll ??= InheritedPassiveSystem.Roll;
            _extraChance = extraChance;
            InheritedPassiveSystem.Roll = () =>
            {
                double v = _realRoll();
                if (_extraChance > 0 && v < _extraChance && new System.Diagnostics.StackTrace().GetFrames().Any(f => f.GetMethod()?.Name == "RollExtraStrike"))
                    _extraHits++;
                return v;
            };
        }

        private static void UnhookRoll()
        {
            if (_realRoll != null)
                InheritedPassiveSystem.Roll = _realRoll;
        }

        private async Task Task4()
        {
            const int N = 10;
            Out($"=== TAREFA 4: em quantas de {N} batalhas cada passiva da Fase 4 disparou (portador = Mega descendente mais comum, nível {MegaLevel}, só aquela espécie no histórico, + Craniummon + Sakuyamon vs Vikemon/Kentaurusmon/Magnamon) ===");
            var only = (OS.GetEnvironment("DEBUG_4C_ONLY") ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToHashSet();

            foreach (var (species, carrierId) in Carriers)
            {
                if (only.Count > 0 && !only.Contains(species))
                    continue;

                var p = Sp(species).InheritedPassive;
                int fired = 0, wins = 0, events = 0, attacks = 0, hits = 0;
                double secs = 0;
                HookRoll(p.Type == InheritedEffectType.RangedExtraStrike ? p.Value : 0f);
                try
                {
                    for (int i = 0; i < N; i++)
                    {
                        var bearer = Make(carrierId, MegaLevel);
                        bearer.EvolutionHistory.Add(species);
                        var team = new List<DigimonInstance> { bearer };
                        team.AddRange(Team(AlliesMega, MegaLevel));

                        _extraHits = 0;
                        Watch w = null;
                        var r = await Fight(team, Team(ControlMega, MegaLevel), m =>
                        {
                            w ??= new Watch { Bearer = m.PlayerTeam.Members[0] };
                            Observe(p, w, m, _extraHits);
                        });

                        if (w?.Fired == true) fired++;
                        if (r.Won) wins++;
                        secs += r.Seconds;
                        events += w?.Events ?? 0;
                        attacks += w?.Bearer.Inherited.AttacksDealt ?? 0;
                        hits += w?.Bearer.Inherited.HitsTaken ?? 0;
                    }
                }
                finally
                {
                    UnhookRoll();
                }

                var c = Sp(carrierId);
                Out($"T4 | {species} | {Sp(species).Name} | {Sp(species).Stage} | {p.Type} | {InheritedPassiveSystem.Describe(p)} | portador {c.Name} ({c.Role}, {c.AttackType}) | " +
                    $"disparou {fired}/{N} | eventos {events} | ataques {attacks} | golpes recebidos {hits} | vitórias {wins}/{N} | {secs / N:0.0}s");
            }
        }
    }
}
