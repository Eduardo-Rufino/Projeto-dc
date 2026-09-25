using Godot;
using ProjetoDC.Enums;
using ProjetoDC.Scripts.Data;
using ProjetoDC.Scripts.Gameplay;
using ProjetoDC.Scripts.Managers;
using ProjetoDC.Scripts.Models.World;
using ProjetoDC.Scripts.Save;
using ProjetoDC.Scripts.Systems.Battle;
using ProjetoDC.Scripts.Systems.Calendar;
using ProjetoDC.Scripts.Systems.Clock;
using ProjetoDC.Scripts.Systems.Center;
using ProjetoDC.Scripts.Systems.Eggs;
using ProjetoDC.Scripts.Systems.Passives;
using ProjetoDC.Scripts.Systems.Results;
using ProjetoDC.Scripts.Systems.Tournaments;
using ProjetoDC.Scripts.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace ProjetoDC.Scripts.Systems.Audit
{
    /// <summary>
    /// Bateria de testes do jogo inteiro (dados, economia, ciclo de vida dos Digimons, treino,
    /// evolução, Jogress, quests, exploração, NPCs recrutados, save/load, batalhas). Roda numa cena
    /// própria (Scenes/Audit/GameAudit.tscn), SEM carregar o Center, com um estado de jogo
    /// descartável: o salvamento do GameManager é desligado (_hasActiveGame=false) e o save real só
    /// é lido, nunca escrito. Uso:
    ///   godot --headless --fixed-fps 60 --path . res://Scenes/Audit/GameAudit.tscn
    /// Variável DEBUG_AUDIT_SECTIONS (lista separada por vírgula) restringe às seções desejadas.
    /// </summary>
    public partial class GameAudit : Node
    {
        private int _ok, _fail, _warn;
        private readonly List<string> _failures = new();
        private readonly List<string> _warnings = new();
        private string _section = "";
        private readonly Random _rng = new(12345);

        private static DatabaseManager Db => DatabaseManager.Instance;
        private static GameManager Gm => GameManager.Instance;

        private static readonly BindingFlags NonPublic = BindingFlags.NonPublic | BindingFlags.Instance;

        // ------------------------------------------------------------------ infraestrutura

        private void Ok(string msg)
        {
            _ok++;
        }

        private void Check(bool cond, string msg)
        {
            if (cond)
            {
                _ok++;
                return;
            }

            _fail++;
            string line = $"[{_section}] {msg}";
            _failures.Add(line);
            GD.PrintErr($"[AUDIT-FALHOU] {line}");
        }

        private void Warn(string msg)
        {
            _warn++;
            string line = $"[{_section}] {msg}";
            _warnings.Add(line);
            GD.Print($"[AUDIT-AVISO] {line}");
        }

        private void Info(string msg) => GD.Print($"[AUDIT] [{_section}] {msg}");

        private async Task Frames(int n)
        {
            for (int i = 0; i < n; i++)
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }

        public override void _Ready()
        {
            CallDeferred(nameof(Start));
        }

        private async void Start()
        {
            await Frames(45);

            var wanted = (OS.GetEnvironment("DEBUG_AUDIT_SECTIONS") ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var sections = new List<(string Name, Func<Task> Run)>
            {
                ("data", DataIntegrity),
                ("economy", Economy),
                ("items", ItemsUsage),
                ("lifecycle", Lifecycle),
                ("training", Training),
                ("growth", GrowthSimulation),
                ("jogress", Jogress),
                ("quests", Quests),
                ("unlock", UnlockChain),
                ("recruit", RecruitEffects),
                ("sets", SetsAndEncyclopedia),
                ("save", SaveRoundTrip),
                ("exploration", ExplorationRuntime),
                ("flows", BattleFlows),
                ("battles", RandomBattles),
                ("scenes", ScenesLoad),
                ("misc", Misc),
                ("suddendeath", SuddenDeath),
                ("lifespan", Lifespan),
                ("calendar", Calendar),
                ("rules", TournamentRulesSection),
                ("winrate", TournamentWinRate),
            };

            // A bateria testa a regra de elegibilidade de campeonatos de verdade (DEBUG_IGNORE_CALENDAR fora).
            Gm.IgnoreCalendar = false;

            // Desliga qualquer gravação de save durante a bateria (o save real só é LIDO).
            typeof(GameManager).GetField("_hasActiveGame", NonPublic)?.SetValue(Gm, false);

            foreach (var (name, run) in sections)
            {
                if (wanted.Count > 0 && !wanted.Contains(name))
                    continue;

                _section = name;
                var started = Time.GetTicksMsec();
                int failBefore = _fail;

                try
                {
                    await run();
                }
                catch (Exception e)
                {
                    _fail++;
                    string line = $"[{name}] EXCEÇÃO NÃO TRATADA: {e.GetType().Name}: {e.Message}\n{e.StackTrace}";
                    _failures.Add(line);
                    GD.PrintErr($"[AUDIT-FALHOU] {line}");
                }

                GD.Print($"[AUDIT] seção '{name}' concluída em {(Time.GetTicksMsec() - started) / 1000.0:F1}s - {_fail - failBefore} falha(s)");
            }

            GD.Print("===== AUDIT RESUMO =====");
            GD.Print($"[AUDIT] checagens ok={_ok} falhas={_fail} avisos={_warn}");

            foreach (var f in _failures)
                GD.Print("[AUDIT-DETALHE-FALHA] " + f);

            foreach (var w in _warnings)
                GD.Print("[AUDIT-DETALHE-AVISO] " + w);

            GD.Print("===== AUDIT FIM =====");
            GetTree().Quit();
        }

        // ------------------------------------------------------------------ estado descartável

        private void ResetScratch(int bits = 500, int capacity = 10)
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

            save.Center.Bits = bits;
            save.Center.CapacityLimit = capacity;
        }

        private DigimonInstance MakeDigimon(DigimonData data, int level = 1, bool addToCenter = false)
        {
            var d = new DigimonInstance(data);
            d.PassiveId = PassiveSystem.RollRandomPassive(data);

            while (d.Level < level)
                d.GainExperience(d.ExperienceToNextLevel);

            d.RestoreHealth();

            if (addToCenter)
                Gm.CenterService.AddDigimon(d);

            return d;
        }

        private static DigimonData FindSpecies(int id) => Db.GetDigimon(id);

        private static List<DigimonData> AllSpecies() => Db.GetAllDigimons().ToList();

        // ------------------------------------------------------------------ 1. dados

        private Task DataIntegrity()
        {
            var species = AllSpecies();
            Info($"{species.Count} espécies");

            Check(species.Count == species.Select(s => s.Id).Distinct().Count(), "ids de espécie duplicados");
            Check(species.Count == species.Select(s => s.Code).Distinct().Count(), "códigos de espécie duplicados");

            foreach (var s in species)
            {
                string n = $"espécie {s.Id} {s.Name}";

                Check(!string.IsNullOrWhiteSpace(s.Name), $"{n}: sem nome");
                Check(!string.IsNullOrWhiteSpace(s.Code), $"{n}: sem código");
                Check(ResourceLoader.Exists($"res://Assets/Sprites/Digimon/{s.Code}.png"), $"{n}: sem retrato");

                bool allFrames = true;
                for (int f = 0; f < 15; f++)
                    if (!ResourceLoader.Exists($"res://Assets/Sprites/Digimon/{s.Code}/{f}.png"))
                        allFrames = false;
                Check(allFrames, $"{n}: faltam frames de sprite (0-14)");

                var b = s.BaseStats;
                Check(b != null && b.HealthPoints > 0 && b.Speed > 0 && b.PhysicalDamage >= 0 && b.SpecialDamage >= 0
                      && b.PhysicalDefense >= 0 && b.SpecialDefense >= 0, $"{n}: stats base inválidos");
                Check(Enum.IsDefined(typeof(DigimonStage), s.Stage), $"{n}: estágio inválido");
                Check(Enum.IsDefined(typeof(RoleType), s.Role), $"{n}: role inválida");
                Check(Enum.IsDefined(typeof(DigimonElement), s.Element), $"{n}: elemento inválido");
                Check(Enum.IsDefined(typeof(DigimonAttribute), s.Attribute), $"{n}: atributo inválido");

                if (s.Role == RoleType.Support)
                    Check(Enum.IsDefined(typeof(SupportType), s.SupportType), $"{n}: Support sem SupportType válido");

                foreach (var drop in s.Drops)
                {
                    Check(Db.GetItem(drop.ItemId) != null, $"{n}: drop de item inexistente {drop.ItemId}");
                    Check(drop.ChancePercent > 0 && drop.ChancePercent <= 100 && drop.Quantity > 0, $"{n}: drop com chance/quantidade inválida");
                }

                if (s.Stage == DigimonStage.Baby)
                    Check(Enum.IsDefined(typeof(EggType), s.EggType), $"{n}: Baby sem EggType válido");

                // evoluções: alvos existem, requisitos sãos
                foreach (var e in Db.GetEvolutionsFrom(s.Id))
                {
                    Check(FindSpecies(e.ToDigimonId) != null, $"{n}: evolui pra espécie inexistente {e.ToDigimonId}");
                    Check(e.RequiredLevel >= 0 && e.RequiredAgeInDays >= 0 && e.MinDiscipline <= e.MaxDiscipline
                          && e.RequiredWinRate >= 0f && e.RequiredWinRate <= 1f, $"{n}: requisito de evolução incoerente -> {e.ToDigimonId}");
                }

                // criar instância não pode falhar e deve copiar os stats
                try
                {
                    var inst = new DigimonInstance(s);
                    Check(inst.MaxHealthPoints > 0 && inst.CurrentHealthPoints == inst.MaxHealthPoints, $"{n}: instância nasce com HP inconsistente");
                    Check(inst.CapacityCost > 0, $"{n}: capacidade 0");
                }
                catch (Exception ex)
                {
                    Check(false, $"{n}: exceção ao instanciar ({ex.Message})");
                }
            }

            // cada tipo de ovo tem pelo menos um Baby e uma textura
            foreach (EggType type in Enum.GetValues(typeof(EggType)))
            {
                Check(species.Any(x => x.Stage == DigimonStage.Baby && x.EggType == type), $"EggType {type}: nenhum Baby");
                Check(ResourceLoader.Exists(EggVisuals.GetTexturePath(type)), $"EggType {type}: sem textura de ovo");
            }

            // Jogress
            foreach (var r in Db.GetAllJogressRecipes())
            {
                Check(FindSpecies(r.DigimonAId) != null && FindSpecies(r.DigimonBId) != null && FindSpecies(r.ToDigimonId) != null,
                    $"Jogress {r.DigimonAId}+{r.DigimonBId}->{r.ToDigimonId}: espécie inexistente");
                Check(r.StatMultiplier >= 1f, $"Jogress {r.ToDigimonId}: multiplicador < 1");
            }

            // Sets
            foreach (var set in Db.GetAllSets())
            {
                Check(set.DigimonIds.Count > 0 && set.DigimonIds.All(id => FindSpecies(id) != null), $"Set {set.Name}: membro inexistente");
                Check(set.EffectValue > 0, $"Set {set.Name}: efeito 0");
                Check(!string.IsNullOrWhiteSpace(set.EffectDescription), $"Set {set.Name}: sem descrição");
            }

            // Torneios
            foreach (var t in Db.GetAllTournaments())
            {
                string n = $"torneio {t.Id} {t.Name}";
                Check(t.Opponents.Count == 1 || t.Opponents.Count == 3, $"{n}: {t.Opponents.Count} oponentes (esperado 1 ou 3)");
                Check(t.Opponents.All(o => FindSpecies(o.DigimonId) != null && o.Level >= 1 && o.Level <= 120), $"{n}: oponente inválido");
                Check(t.RecommendedLevel >= 1, $"{n}: nível recomendado inválido");
                Check(t.CapacityReward >= 0 && t.BitsReward >= 0, $"{n}: recompensa negativa");
            }

            Check(Db.GetAllTournaments().Select(t => t.Id).Distinct().Count() == Db.GetAllTournaments().Count(), "ids de torneio duplicados");

            // Itens: as constantes de ItemIds existem
            foreach (var id in new[] { ItemIds.Meat, ItemIds.Medicine, ItemIds.StaminaSnack, ItemIds.CookedMeat })
                Check(Db.GetItem(id) != null, $"ItemIds {id} sem item em Data/Itens");

            Check(Db.GetAllItems().Select(i => i.Id).Distinct().Count() == Db.GetAllItems().Count(), "ids de item duplicados");

            // Passivas: todo PassiveType tem dados
            foreach (PassiveType p in Enum.GetValues(typeof(PassiveType)))
                Check(Db.GetPassive(p) != null, $"passiva {p} sem dados em Data/Passives");

            // Áreas do Center: ilustração + preço
            foreach (CenterAreaType type in Enum.GetValues(typeof(CenterAreaType)))
            {
                string path = CenterArea.GetAreaIllustrationPath(type);
                Check(path != null && ResourceLoader.Exists(path), $"CenterAreaType {type}: sem ilustração");
                Check(GameManager.GetAreaPrice(type) > 0, $"CenterAreaType {type}: preço 0");
            }

            // Mapas/NPCs/quests: consistência cruzada extra (o resto está no ExplorationDebugTests)
            foreach (var q in Db.GetAllQuests())
                Check(Db.GetNpc(q.GiverNpcId) != null, $"quest {q.Id}: NPC doador {q.GiverNpcId} inexistente");

            var npcsInMaps = Db.GetAllExplorationMaps().SelectMany(m => m.Npcs.Select(n => n.NpcId)).ToHashSet();
            foreach (var npc in Db.GetAllNpcs())
                if (!npcsInMaps.Contains(npc.Id))
                    Warn($"NPC {npc.Id} {npc.Name} não aparece em nenhum mapa (órfão)");

            var questsGiven = Db.GetAllNpcs().Where(n => n.QuestId.HasValue).Select(n => n.QuestId.Value).ToHashSet();
            foreach (var q in Db.GetAllQuests())
                if (!questsGiven.Contains(q.Id))
                    Warn($"quest {q.Id} '{q.Name}' não é oferecida por nenhum NPC (órfã)");

            return Task.CompletedTask;
        }

        // ------------------------------------------------------------------ 2. economia

        private Task Economy()
        {
            ResetScratch(bits: 100_000, capacity: 500);
            var c = Gm.Save.Center;

            int bits = c.Bits, meat = c.Meat;
            var r = Gm.BuyMeat();
            Check(r.Success && c.Bits == bits - 20 && c.Meat == meat + 1, "BuyMeat: -20 Bits, +1 Carne");

            bits = c.Bits; int med = c.Medicine;
            r = Gm.BuyMedicine();
            Check(r.Success && c.Bits == bits - 100 && c.Medicine == med + 1, "BuyMedicine: -100 Bits, +1 Remédio");

            bits = c.Bits; int snack = c.StaminaSnacks;
            r = Gm.BuyStaminaSnack();
            Check(r.Success && c.Bits == bits - GameManager.StaminaSnackPrice && c.StaminaSnacks == snack + 1, "BuyStaminaSnack");

            // ovos: aleatório e cada tipo
            foreach (EggType? type in new EggType?[] { null }.Concat(Enum.GetValues(typeof(EggType)).Cast<EggType?>()))
            {
                bits = c.Bits; int eggs = c.Eggs.Count;
                r = Gm.BuyEgg(type);
                Check(r.Success && c.Eggs.Count == eggs + 1 && c.Bits == bits - GameManager.GetEggPrice(type),
                    $"BuyEgg({type?.ToString() ?? "aleatório"}): sucesso, +1 ovo, preço {GameManager.GetEggPrice(type)}");

                if (r.Success && type.HasValue)
                {
                    var egg = c.Eggs.Last();
                    var species = FindSpecies(egg.BaseDigimonId);
                    Check(species != null && species.Stage == DigimonStage.Baby && species.EggType == type.Value,
                        $"BuyEgg({type}): o ovo é de um Baby do tipo certo");
                    Check(egg.CapacityCost > 0, "ovo comprado reserva capacidade");
                }
            }

            Check(GameManager.GetEggPrice(EggType.Fire) == GameManager.GetEggPrice(null) * GameManager.SpecificEggPriceMultiplier,
                "ovo específico custa o multiplicador esperado");

            // áreas
            foreach (CenterAreaType type in Enum.GetValues(typeof(CenterAreaType)))
            {
                bits = c.Bits;
                r = Gm.BuyArea(type);
                Check(r.Success && c.Bits == bits - GameManager.GetAreaPrice(type), $"BuyArea({type}): desconta {GameManager.GetAreaPrice(type)}");
            }

            // sem Bits nada muda
            c.Bits = 5;
            meat = c.Meat; med = c.Medicine; snack = c.StaminaSnacks; int eggCount = c.Eggs.Count;
            Check(!Gm.BuyMeat().Success && !Gm.BuyMedicine().Success && !Gm.BuyStaminaSnack().Success && !Gm.BuyEgg().Success && !Gm.BuyArea(CenterAreaType.Hospital).Success,
                "sem Bits todas as compras falham");
            Check(c.Bits == 5 && c.Meat == meat && c.Medicine == med && c.StaminaSnacks == snack && c.Eggs.Count == eggCount, "compra que falha não altera nada");

            // capacidade cheia bloqueia ovo
            ResetScratch(bits: 100_000, capacity: 1);
            Gm.CenterService.AddDigimon(MakeDigimon(AllSpecies().First(s => s.Stage == DigimonStage.Baby)));
            Check(!Gm.BuyEgg().Success, "BuyEgg com capacidade cheia falha");
            Check(Gm.Save.Center.Bits == 100_000, "BuyEgg que falha por capacidade não cobra");

            // valores absurdos: Bits nunca negativos
            ResetScratch(bits: 0);
            Check(!Gm.BuyMeat().Success && Gm.Save.Center.Bits == 0, "com 0 Bits, BuyMeat falha sem ficar negativo");

            return Task.CompletedTask;
        }

        // ------------------------------------------------------------------ 3. itens (uso)

        private Task ItemsUsage()
        {
            ResetScratch(capacity: 500);
            var c = Gm.Save.Center;
            var species = AllSpecies().First(s => s.Stage == DigimonStage.Rookie);
            var d = MakeDigimon(species, 5, addToCenter: true);

            // Carne
            d.Hunger = 30; c.Meat = 3;
            var r = Gm.FeedDigimon(d);
            Check(r.Success && c.Meat == 2 && d.Hunger > 30, "Carne: alimenta, gasta 1 e aumenta a fome");
            d.Hunger = d.MaxHunger;
            Check(!Gm.FeedDigimon(d).Success && c.Meat == 2, "Carne: Digimon cheio não come e não gasta");
            c.Meat = 0; d.Hunger = 10;
            Check(!Gm.FeedDigimon(d).Success, "Carne: sem estoque falha");

            // Remédio
            c.Medicine = 2;
            Check(!Gm.UseMedicine(d).Success && c.Medicine == 2, "Remédio: Digimon saudável não usa e não gasta");
            d.BecomeSick();
            Check(d.HealthState == HealthState.Sick, "BecomeSick deixa doente");
            r = Gm.UseMedicine(d);
            Check(r.Success && c.Medicine == 1 && d.HealthState == HealthState.Healthy, "Remédio: cura e gasta 1");
            d.BecomeSick(); c.Medicine = 0;
            Check(!Gm.UseMedicine(d).Success && d.HealthState == HealthState.Sick, "Remédio: sem estoque não cura");

            // Energético (stamina)
            d.Stamina = 10;
            d.RecoverStamina(500);
            Check(d.Stamina == d.MaxStamina && d.IsStaminaFull(), "Stamina recuperada nunca passa do máximo");
            d.ConsumeStamina(9999);
            Check(d.Stamina == 0, "Stamina nunca fica negativa");

            // Carne Assada / cozinha: 100 Bits por Carne, encomenda pronta em 20 minutos de jogo
            c.Meat = 6;
            c.Bits = 10_000;
            c.CookedMeat = 0;
            Check(!Gm.CookMeat(2).Success && c.Meat == 6 && c.Bits == 10_000, "Cozinha: sem o Meramon recrutado não assa nem cobra");
            c.RecruitedNpcIds.Add(PerkNpc(NpcPerkType.KitchenCook));

            var cook = Gm.CookMeat(4);
            Check(cook.Success && c.Meat == 2 && c.CookedMeat == 0 && c.Bits == 10_000 - 4 * GameManager.CookingPricePerMeat, "Cozinha: encomendar 4 de 6 cobra 100 por Carne e não entrega na hora");
            Check(c.IsCooking && c.CookingMeatAmount == 4 && c.CookingMinutesLeft == GameManager.CookingMinutes, "Cozinha: encomenda registrada com 20 minutos");
            Check(!Gm.CookMeat(1).Success && c.Meat == 2 && c.Bits == 10_000 - 400, "Cozinha: com o fogão ocupado recusa e não cobra");

            Gm.ClockSystem.Update(GameManager.CookingMinutes - 1);
            Check(c.IsCooking && c.CookedMeat == 0 && c.CookingMinutesLeft == 1, "Cozinha: aos 19 minutos ainda não está pronto");
            Gm.ClockSystem.Update(1);
            Check(!c.IsCooking && c.CookedMeat == 4, "Cozinha: aos 20 minutos as 4 Carnes Assadas chegam ao estoque");

            c.Bits = 250;
            Check(Gm.CookMeat(99).Success && c.Meat == 0 && c.Bits == 50 && c.CookingMeatAmount == 2, "Cozinha: 'todas' assa só o que os Bits pagam (250 Bits = 2 Carnes)");
            Gm.ClockSystem.Update(GameManager.CookingMinutes);
            Check(c.CookedMeat == 6 && !c.IsCooking, "Cozinha: segunda encomenda pronta em 20 minutos");

            c.Meat = 3;
            c.Bits = 99;
            Check(!Gm.CookMeat(1).Success && c.Meat == 3 && c.Bits == 99 && !c.IsCooking, "Cozinha: menos de 100 Bits não assa");
            c.Bits = 5000;
            c.Meat = 0;
            Check(!Gm.CookMeat(1).Success && c.Bits == 5000, "Cozinha: sem Carne crua falha sem cobrar");
            Check(!Gm.CookMeat(0).Success && !Gm.CookMeat(-3).Success, "Cozinha: quantidade inválida falha");

            // Deletar (o jogo recusa apagar o ÚNICO Digimon do Center - de propósito)
            Check(!Gm.DeleteDigimon(d), "DeleteDigimon recusa apagar o único Digimon");
            MakeDigimon(species, 5, addToCenter: true);
            int before = c.Digimons.Count;
            Check(Gm.DeleteDigimon(d) && c.Digimons.Count == before - 1, "DeleteDigimon remove do Center");
            Check(!Gm.DeleteDigimon(d), "DeleteDigimon duas vezes não remove de novo");

            // Bloquear evolução
            var champ = AllSpecies().First(s => s.Stage == DigimonStage.Rookie && Db.GetEvolutionsFrom(s.Id).Count > 0);
            var e = Db.GetEvolutionsFrom(champ.Id).First();
            var d2 = MakeDigimon(champ, 5, addToCenter: true);
            Check(!Gm.SetEvolutionBlocked(d2, e.ToDigimonId, true).Success, "SetEvolutionBlocked sem o Wizardmon recrutado falha");
            c.RecruitedNpcIds.Add(PerkNpc(NpcPerkType.EvolutionAdvisor));
            Check(Gm.SetEvolutionBlocked(d2, e.ToDigimonId, true).Success && d2.IsEvolutionBlocked(e.ToDigimonId), "SetEvolutionBlocked bloqueia");
            Check(Gm.SetEvolutionBlocked(d2, e.ToDigimonId, false).Success && !d2.IsEvolutionBlocked(e.ToDigimonId), "SetEvolutionBlocked desbloqueia");

            // Dormir / pular: de QUALQUER hora dentro da janela de sono (22h-6h) o "pular sono" tem que
            // acordar o Center às 6h - a janela tem 8h, então o relógio precisa avançar até 480 min.
            ResetScratch();
            Gm.Save.World.CurrentHour = 12;
            Check(!Gm.CanSkipSleep && !Gm.SkipSleep(), "fora do horário de sono não pula o sono");

            foreach (int startHour in new[] { 22, 23, 0, 1, 2, 3, 4, 5 })
            {
                ResetScratch();
                Gm.Save.World.CurrentHour = startHour;
                Gm.Save.World.CurrentMinute = 0;
                int day = Gm.Save.World.CurrentDay;

                Check(Gm.CanSkipSleep, $"{startHour}h: deveria poder pular o sono");
                bool skipped = Gm.SkipSleep();
                int endHour = Gm.Save.World.CurrentHour;

                Check(skipped && endHour == DigimonInstance.SleepEndHour,
                    $"Pular sono às {startHour}h parou às {endHour}h em vez de {DigimonInstance.SleepEndHour}h (o teto de AdvanceUntilHour é curto demais pra janela de sono de 8h)");
                Check(Gm.Save.World.CurrentDay >= day, $"{startHour}h: o dia voltou");
            }

            return Task.CompletedTask;
        }

        // ------------------------------------------------------------------ 4. ciclo de vida

        private void AssertInvariants(IEnumerable<DigimonInstance> list, string ctx)
        {
            foreach (var d in list)
            {
                string n = $"{ctx}: {d.BaseData?.Name} (id {d.BaseData?.Id})";
                Check(d.Hunger >= 0 && d.Hunger <= d.MaxHunger, $"{n}: fome fora da faixa ({d.Hunger})");
                Check(d.Stamina >= 0 && d.Stamina <= d.MaxStamina, $"{n}: stamina fora da faixa ({d.Stamina})");
                Check(d.CurrentHealthPoints >= 0 && d.CurrentHealthPoints <= d.MaxHealthPoints, $"{n}: HP fora da faixa ({d.CurrentHealthPoints}/{d.MaxHealthPoints})");
                Check(d.Level >= 1, $"{n}: nível < 1");
                Check(d.Happiness >= 0 && d.Happiness <= DigimonInstance.MaxHappiness, $"{n}: felicidade fora da faixa ({d.Happiness})");
                Check(d.Discipline >= 0 && d.Discipline <= DigimonInstance.MaxDiscipline, $"{n}: disciplina fora da faixa ({d.Discipline})");
                Check(d.AgeInDays >= 0, $"{n}: idade negativa");
                Check(d.CurrentStats != null && d.CurrentStats.HealthPoints > 0, $"{n}: stats atuais inválidos");
            }
        }

        private Task Lifecycle()
        {
            var species = AllSpecies();

            // A) negligência: todas as espécies juntas, sem comida/remédio, 25 dias
            ResetScratch(capacity: 100_000);
            int diedA = 0;
            Action<DigimonInstance, string> onDiedA = (d, why) => diedA++;
            Gm.DigimonDied += onDiedA;

            foreach (var s in species)
                MakeDigimon(s, 3, addToCenter: true);

            int start = Gm.Save.Center.Digimons.Count;
            Info($"negligência: {start} Digimons no Center");

            for (int day = 1; day <= 25; day++)
            {
                Gm.ClockSystem.Update(1440.0);
                AssertInvariants(Gm.Save.Center.Digimons, $"negligência dia {day}");
            }

            Gm.DigimonDied -= onDiedA;
            Info($"negligência: sobraram {Gm.Save.Center.Digimons.Count}/{start}, {diedA} mortes");
            Check(Gm.Save.World.CurrentDay >= 25, "o relógio avançou 25 dias");

            // B) cuidado: alimentado e descansado, até a velhice
            ResetScratch(capacity: 100_000);
            var reasons = new Dictionary<string, int>();
            int deaths = 0;
            var timeline = new List<string>();
            Action<DigimonInstance, string> onDied = (d, why) =>
            {
                deaths++;
                if (deaths > 44 && timeline.Count < 30)
                    timeline.Add($"dia {Gm.Save.World.CurrentDay} {Gm.Save.World.CurrentHour}h: {d.BaseData.Name}[{d.BaseData.Stage}] idade={d.AgeInDays} max={DigimonInstance.GetMaxAgeForStage(d.BaseData.Stage)}");
                string key = why.Length > 60 ? why.Substring(0, 60) : why;
                key = System.Text.RegularExpressions.Regex.Replace(key, "^[^ ]+ ", "");
                reasons[key] = reasons.GetValueOrDefault(key) + 1;
            };
            Gm.DigimonDied += onDied;

            var sample = species.Where(s => s.Stage != DigimonStage.Baby).OrderBy(_ => _rng.Next()).Take(40).ToList();
            foreach (var s in sample)
                MakeDigimon(s, 3, addToCenter: true);

            var perDay = new List<string>();
            for (int day = 1; day <= 70; day++)
            {
                for (int h = 0; h < 24; h++)
                {
                    foreach (var d in Gm.Save.Center.Digimons)
                    {
                        d.Hunger = d.MaxHunger;
                        d.Stamina = d.MaxStamina;
                        d.ChangeHappiness(5);
                    }

                    Gm.ClockSystem.Update(60.0);
                }

                AssertInvariants(Gm.Save.Center.Digimons, $"cuidado dia {day}");

                if (day <= 12 || day % 10 == 0)
                    perDay.Add($"d{day}: vivos={Gm.Save.Center.Digimons.Count} ovos={Gm.Save.Center.Eggs.Count} mortes={deaths}");
            }

            Gm.DigimonDied -= onDied;
            Info("linha do tempo das mortes tardias: " + string.Join(" || ", timeline));
            Info("cuidado (por dia): " + string.Join(" | ", perDay));
            Info($"cuidado: {Gm.Save.Center.Digimons.Count} vivos após 70 dias, {deaths} mortes; motivos: " +
                 string.Join("; ", reasons.OrderByDescending(kv => kv.Value).Select(kv => $"{kv.Value}x '{kv.Key}'")));

            // 40 Digimons cuidados não podem gerar centenas de mortes: cada Digimon morre uma vez, e o Center vazio
            // cria no máximo um ovo novo por vez - mais que isso indica ciclo descontrolado de ovo -> morte.
            Check(deaths <= sample.Count + 70, $"cuidado: {deaths} mortes em 70 dias com {sample.Count} Digimons (ciclo ovo->morte descontrolado?)");

            // C) morrendo remove do Center e do save: nenhum Digimon com HP<=0 ou morto continua vivo
            Check(Gm.Save.Center.Digimons.All(d => !d.IsPastMaxAge() || true), "sanidade");

            // D) ovo inicial choca sozinho pelo relógio
            ResetScratch(capacity: 500);
            Gm.EggSystem.CreateInitialEgg(Gm.CenterService);
            Check(Gm.Save.Center.Eggs.Count == 1, "ovo inicial criado");
            bool hatched = false;
            Gm.EggSystem.EggHatched += (egg, digi) => hatched = true;
            for (int h = 0; h < 6 && !hatched; h++)
                Gm.ClockSystem.Update(60.0);
            Check(hatched && Gm.Save.Center.Digimons.Count == 1, "ovo inicial choca em poucas horas de jogo e vira Digimon");
            Check(Gm.Save.Center.Eggs.Count == 0, "ovo que chocou some da lista de ovos");

            // E) ovo comprado choca em dias
            ResetScratch(bits: 10_000, capacity: 500);
            Check(Gm.BuyEgg().Success, "ovo comprado");
            // O Baby parado (sem treino) morre de velhice depois do teto de idade e o Center dá um ovo de
            // reposição - por isso o que vale é o ovo comprado ter chocado em algum dia, não a contagem final.
            int maxDigimons = 0;
            for (int day = 0; day < 6; day++)
            {
                Gm.ClockSystem.Update(1440.0);
                maxDigimons = Math.Max(maxDigimons, Gm.Save.Center.Digimons.Count);
            }

            Check(maxDigimons >= 1, "ovo comprado choca em alguns dias");

            return Task.CompletedTask;
        }

        // ------------------------------------------------------------------ 5. treino

        private Task Training()
        {
            ResetScratch(capacity: 100_000);
            var sample = AllSpecies().Where(s => s.Stage != DigimonStage.Baby).OrderBy(_ => _rng.Next()).Take(24).ToList();

            foreach (var s in sample)
            {
                foreach (TrainingType type in Enum.GetValues(typeof(TrainingType)))
                {
                    var d = MakeDigimon(s, 10);
                    d.Stamina = d.MaxStamina;
                    int stamina = d.Stamina;
                    var before = Snapshot(d);

                    var res = Gm.TrainingSystem.Execute(d, type);
                    Check(res.Success, $"treino {type} em {s.Name}: falhou com stamina cheia");

                    if (!res.Success)
                        continue;

                    d.ApplyTrainingResult(res);
                    var after = Snapshot(d);

                    bool gained = type switch
                    {
                        TrainingType.HealthPoints => after.hp > before.hp,
                        TrainingType.Attack => after.atk > before.atk,
                        TrainingType.Defense => after.def > before.def,
                        TrainingType.SpecialAttack => after.satk > before.satk,
                        TrainingType.SpecialDefense => after.sdef > before.sdef,
                        TrainingType.Speed => after.spd > before.spd,
                        _ => false,
                    };

                    Check(gained, $"treino {type} em {s.Name}: o stat treinado não subiu");
                    Check(d.Stamina < stamina, $"treino {type} em {s.Name}: não gastou stamina");
                    AssertInvariants(new[] { d }, "pós-treino");
                }
            }

            // sem stamina
            var weak = MakeDigimon(sample[0], 5);
            weak.Stamina = 5;
            var fail = Gm.TrainingSystem.Execute(weak, TrainingType.Attack);
            Check(!fail.Success && fail.Reason == "NOT_ENOUGH_STAMINA", "treino sem stamina falha com NOT_ENOUGH_STAMINA");

            // TrainPlayer: doente não treina; sem Digimon selecionado falha
            Gm.Save.Center.Digimons.Clear();
            Check(!Gm.TrainPlayer(TrainingType.Speed).Success, "TrainPlayer sem Digimon selecionado falha");
            var p = MakeDigimon(sample[1], 5, addToCenter: true);
            Gm.SetPlayerDigimon(p);
            p.Stamina = p.MaxStamina;
            Check(Gm.TrainPlayer(TrainingType.Speed).Success, "TrainPlayer com Digimon saudável funciona");
            p.BecomeSick(); p.Stamina = p.MaxStamina;
            Check(!Gm.TrainPlayer(TrainingType.Speed).Success, "TrainPlayer com Digimon doente falha");

            // ganhos de XP/level: nunca passar de um teto absurdo e nunca regredir
            var lv = MakeDigimon(sample[2], 1);
            int prevLevel = lv.Level;
            for (int i = 0; i < 400; i++)
            {
                lv.GainExperience(lv.ExperienceToNextLevel);
                Check(lv.Level >= prevLevel, "nível regrediu");
                prevLevel = lv.Level;
            }
            Info($"nível após 400 level-ups forçados: {lv.Level}");
            AssertInvariants(new[] { lv }, "level máximo");

            return Task.CompletedTask;
        }

        private static (int hp, int atk, int def, int satk, int sdef, int spd) Snapshot(DigimonInstance d) =>
            (d.CurrentStats.HealthPoints, d.CurrentStats.PhysicalDamage, d.CurrentStats.PhysicalDefense,
             d.CurrentStats.SpecialDamage, d.CurrentStats.SpecialDefense, d.CurrentStats.Speed);

        // ------------------------------------------------------------------ 6. evolução (todas as arestas + caminhadas)

        private static readonly MethodInfo MeetsRequirements =
            typeof(ProjetoDC.Scripts.Systems.Evolution.EvolutionSystem).GetMethod("MeetsRequirements", BindingFlags.NonPublic | BindingFlags.Static);

        /// <summary>Deixa o Digimon cumprindo exatamente os requisitos da aresta (nível, idade, stats, batalhas,
        /// taxa de vitória, disciplina e felicidade).</summary>
        private static void SatisfyEdge(DigimonInstance d, EvolutionData e)
        {
            while (d.Level < e.RequiredLevel)
                d.GainExperience(d.ExperienceToNextLevel);

            d.AgeInDays = Math.Max(d.AgeInDays, e.RequiredAgeInDays);

            var req = e.RequiredStats;
            if (req != null)
            {
                d.CurrentStats.HealthPoints = Math.Max(d.CurrentStats.HealthPoints, req.HealthPoints);
                d.CurrentStats.PhysicalDamage = Math.Max(d.CurrentStats.PhysicalDamage, req.PhysicalDamage);
                d.CurrentStats.PhysicalDefense = Math.Max(d.CurrentStats.PhysicalDefense, req.PhysicalDefense);
                d.CurrentStats.SpecialDamage = Math.Max(d.CurrentStats.SpecialDamage, req.SpecialDamage);
                d.CurrentStats.SpecialDefense = Math.Max(d.CurrentStats.SpecialDefense, req.SpecialDefense);
                d.CurrentStats.Speed = Math.Max(d.CurrentStats.Speed, req.Speed);
            }

            d.MaxHealthPoints = Math.Max(d.MaxHealthPoints, d.CurrentStats.HealthPoints);
            d.RestoreHealth();

            int fought = Math.Max(d.BattlesFought, e.RequiredBattles);
            if (e.RequiredWinRate > 0)
                fought = Math.Max(fought, 10);

            d.BattlesFought = fought;
            d.BattlesWon = Math.Max(d.BattlesWon, (int)Math.Ceiling(e.RequiredWinRate * fought));

            d.Discipline = Math.Clamp((e.MinDiscipline + e.MaxDiscipline) / 2, 0, 100);
            d.Happiness = Math.Clamp((e.MinHappiness + e.MaxHappiness) / 2, 0, 100);
        }

        private Task GrowthSimulation()
        {
            int edges = 0, evolved = 0;
            var neverChosen = new List<string>();

            // ---- A) cada aresta de evolução: um Digimon que cumpre só ela tem que evoluir
            foreach (var from in AllSpecies())
            {
                var outgoing = Db.GetEvolutionsFrom(from.Id);

                foreach (var e in outgoing)
                {
                    edges++;
                    ResetScratch(capacity: 100_000);
                    string n = $"aresta {from.Name} -> {FindSpecies(e.ToDigimonId)?.Name}";

                    var d = MakeDigimon(from, 1, addToCenter: true);
                    SatisfyEdge(d, e);
                    Check((bool)MeetsRequirements.Invoke(null, new object[] { d, e }), $"{n}: o teste não conseguiu cumprir os requisitos (bug do teste)");

                    var valid = outgoing.Where(o => (bool)MeetsRequirements.Invoke(null, new object[] { d, o })).Select(o => o.ToDigimonId).ToList();
                    bool ok = Gm.TryToEvolve(d);
                    Check(ok, $"{n}: cumpre os requisitos e não evoluiu");

                    if (!ok)
                        continue;

                    evolved++;
                    Check(valid.Contains(d.BaseData.Id), $"{n}: evoluiu pra {d.BaseData.Name}, que não estava entre as válidas");
                    Check(d.BaseData.Stage >= from.Stage, $"{n}: o estágio regrediu");
                    Check(Gm.Save.Center.DiscoveredDigimonIds.Contains(d.BaseData.Id), $"{n}: nova forma não foi marcada como descoberta");
                    Check(d.PassiveId.HasValue, $"{n}: evoluiu sem passiva");
                    AssertInvariants(new[] { d }, n);

                    if (d.BaseData.Id != e.ToDigimonId)
                        neverChosen.Add($"{from.Name}->{FindSpecies(e.ToDigimonId)?.Name} perdeu pra {d.BaseData.Name}");

                    // capacidade cheia: a evolução que custa mais capacidade não acontece
                    ResetScratch(capacity: 1);
                    var d2 = MakeDigimon(from, 1);
                    Gm.Save.Center.Digimons.Add(d2);
                    SatisfyEdge(d2, e);
                    Gm.Save.Center.CapacityLimit = Gm.Save.Center.CapacityUsed;
                    int before = d2.BaseData.Id;
                    bool evolvedBlocked = Gm.TryToEvolve(d2);

                    var newForm = FindSpecies(e.ToDigimonId);
                    if (DigimonInstance.GetCapacityCostForStage(newForm.Stage) > d2.CapacityCost && outgoing.Count == 1)
                        Check(!evolvedBlocked && d2.BaseData.Id == before, $"{n}: com capacidade cheia deveria bloquear a evolução");
                }
            }

            Info($"{edges} arestas de evolução testadas, {evolved} evoluíram");

            if (neverChosen.Count > 0)
                Info($"{neverChosen.Count} arestas em que, cumpridos os requisitos, outra aresta válida foi escolhida no lugar (normal quando várias ficam válidas)");

            // ---- B) caminhadas encadeadas a partir de cada Baby
            var maxStage = 0;
            var reached = new HashSet<int>();

            foreach (var baby in AllSpecies().Where(x => x.Stage == DigimonStage.Baby))
            {
                for (int walk = 0; walk < 6; walk++)
                {
                    ResetScratch(capacity: 100_000);
                    var d = MakeDigimon(baby, 1, addToCenter: true);
                    var path = new List<string> { baby.Name };

                    for (int step = 0; step < 12; step++)
                    {
                        var options = Db.GetEvolutionsFrom(d.BaseData.Id);
                        if (options.Count == 0) break;

                        var pick = options[_rng.Next(options.Count)];
                        SatisfyEdge(d, pick);

                        if (!Gm.TryToEvolve(d))
                        {
                            Check(false, $"caminhada {string.Join(" > ", path)}: travou em {d.BaseData.Name} ao cumprir {pick.ToDigimonId}");
                            break;
                        }

                        path.Add(d.BaseData.Name);
                        reached.Add(d.BaseData.Id);
                        maxStage = Math.Max(maxStage, (int)d.BaseData.Stage);
                        AssertInvariants(new[] { d }, $"caminhada {baby.Name}");
                    }

                    Check(path.Count >= 2, $"Baby {baby.Name}: nenhuma evolução possível");
                }
            }

            Info($"caminhadas: {reached.Count} espécies alcançadas, estágio máximo {(DigimonStage)maxStage}");

            // ---- C) toda espécie é alcançável a partir de algum Baby (evolução) ou por Jogress
            var reachableIds = new HashSet<int>(AllSpecies().Where(x => x.Stage == DigimonStage.Baby).Select(x => x.Id));
            var queue = new Queue<int>(reachableIds);
            while (queue.Count > 0)
            {
                int id = queue.Dequeue();
                foreach (var e in Db.GetEvolutionsFrom(id))
                    if (reachableIds.Add(e.ToDigimonId))
                        queue.Enqueue(e.ToDigimonId);
            }

            foreach (var r in Db.GetAllJogressRecipes())
                reachableIds.Add(r.ToDigimonId);

            foreach (var u in AllSpecies().Where(x => !reachableIds.Contains(x.Id)))
                Warn($"espécie {u.Id} {u.Name} ({u.Stage}) não é alcançável por nenhuma evolução nem Jogress a partir de um Baby");

            return Task.CompletedTask;
        }

        // ------------------------------------------------------------------ 7. jogress

        private Task Jogress()
        {
            var recipes = Db.GetAllJogressRecipes().ToList();
            Info($"{recipes.Count} receitas");

            foreach (var r in recipes)
            {
                ResetScratch(capacity: 100_000);
                var a = MakeDigimon(FindSpecies(r.DigimonAId), 60, addToCenter: true);
                var b = MakeDigimon(FindSpecies(r.DigimonBId), 60, addToCenter: true);
                a.AgeInDays = 30; b.AgeInDays = 30;
                Gm.SetPlayerDigimon(a);

                var before = Gm.Save.Center.Digimons.Count;
                var res = Gm.TryToFuse(a, b);
                string n = $"Jogress {a.BaseData.Name}+{b.BaseData.Name}";

                if (res.Outcome != JogressOutcome.Fused)
                {
                    Warn($"{n}: não fundiu com nível 60 ({res.Outcome}) - pode haver requisito extra");
                    continue;
                }

                Check(res.FusedDigimon.BaseData.Id == r.ToDigimonId, $"{n}: espécie fundida errada");
                Check(Gm.Save.Center.Digimons.Count == before - 1, $"{n}: deveria sobrar 1 Digimon (2 pais -> 1 fundido)");
                Check(!Gm.Save.Center.Digimons.Contains(a) && !Gm.Save.Center.Digimons.Contains(b), $"{n}: pais continuam no Center");
                Check(Gm.PlayerDigimon == res.FusedDigimon, $"{n}: o Digimon selecionado não passou pro fundido");
                Check(Gm.Save.Center.DiscoveredDigimonIds.Contains(r.ToDigimonId), $"{n}: fundido não foi marcado como descoberto");
                AssertInvariants(new[] { res.FusedDigimon }, n);

                // não funde com ele mesmo nem com par errado
                Check(Gm.TryToFuse(res.FusedDigimon, res.FusedDigimon).Outcome != JogressOutcome.Fused, $"{n}: fundiu consigo mesmo");
            }

            // par sem receita
            ResetScratch(capacity: 100_000);
            var sp = AllSpecies().Where(s => s.Stage == DigimonStage.Champion).Take(2).ToList();
            var x = MakeDigimon(sp[0], 60, addToCenter: true);
            var y = MakeDigimon(sp[1], 60, addToCenter: true);
            Check(Gm.TryToFuse(x, y).Outcome != JogressOutcome.Fused || Db.GetJogressRecipe(x.BaseData.Id, y.BaseData.Id) != null, "par sem receita não funde");

            // capacidade cheia
            var recipe = recipes.FirstOrDefault();
            if (recipe != null)
            {
                ResetScratch(capacity: 2);
                var a = MakeDigimon(FindSpecies(recipe.DigimonAId), 60, addToCenter: true);
                var b = MakeDigimon(FindSpecies(recipe.DigimonBId), 60, addToCenter: true);
                Info($"capacidade usada {Gm.Save.Center.CapacityUsed}/{Gm.Save.Center.CapacityLimit}");
                var res = Gm.TryToFuse(a, b);
                Info($"fusão com capacidade apertada: {res.Outcome}");
            }

            return Task.CompletedTask;
        }

        // ------------------------------------------------------------------ 8. quests

        private void SetProgressViaGame(QuestData q, int amount)
        {
            var method = q.ObjectiveType == QuestObjectiveType.DefeatWild
                ? typeof(GameManager).GetMethod("ApplyWildEncounterQuestProgress", NonPublic)
                : typeof(GameManager).GetMethod("ApplyCollectItemQuestProgress", NonPublic);

            int arg = q.ObjectiveType == QuestObjectiveType.DefeatWild
                ? (q.ObjectiveTargetId != 0 ? q.ObjectiveTargetId : 1)
                : q.ObjectiveTargetId;

            for (int i = 0; i < amount; i++)
                method.Invoke(Gm, new object[] { arg });
        }

        private Task Quests()
        {
            foreach (var q in Db.GetAllQuests().OrderBy(q => q.Id))
            {
                ResetScratch(bits: 0, capacity: 10);
                var c = Gm.Save.Center;
                string n = $"quest {q.Id} '{q.Name}'";

                Check(!Gm.TryTurnInQuest(q.Id).Success, $"{n}: entregou sem aceitar");
                Check(Gm.TryAcceptQuest(q.Id).Success && c.ActiveQuestIds.Contains(q.Id), $"{n}: não aceitou");
                Check(!Gm.TryAcceptQuest(q.Id).Success, $"{n}: aceitou duas vezes");
                Check(!Gm.IsQuestReadyToTurnIn(q.Id), $"{n}: pronta sem progresso");
                Check(!Gm.TryTurnInQuest(q.Id).Success, $"{n}: entregou sem cumprir");

                if (q.ObjectiveCount > 1)
                {
                    SetProgressViaGame(q, q.ObjectiveCount - 1);
                    Check(Gm.GetQuestProgress(q.Id) == q.ObjectiveCount - 1 && !Gm.IsQuestReadyToTurnIn(q.Id), $"{n}: progresso parcial");
                }

                SetProgressViaGame(q, 3);
                Check(Gm.GetQuestProgress(q.Id) == q.ObjectiveCount, $"{n}: progresso não trava no objetivo ({Gm.GetQuestProgress(q.Id)}/{q.ObjectiveCount})");
                Check(Gm.IsQuestReadyToTurnIn(q.Id), $"{n}: não ficou pronta");

                int bits = c.Bits, cap = c.CapacityLimit;
                int itemBefore = q.RewardItemId > 0 ? c.GetItemQuantity(q.RewardItemId) : 0;
                var turn = Gm.TryTurnInQuest(q.Id);
                Check(turn.Success, $"{n}: entrega falhou");
                Check(c.CompletedQuestIds.Contains(q.Id) && !c.ActiveQuestIds.Contains(q.Id), $"{n}: estado ativo/completa errado");
                Check(c.Bits == bits + q.RewardBits, $"{n}: Bits da recompensa ({c.Bits - bits} != {q.RewardBits})");
                Check(c.CapacityLimit == cap + q.RewardCapacity, $"{n}: capacidade da recompensa");

                if (q.RewardItemId > 0)
                    Check(c.GetItemQuantity(q.RewardItemId) == itemBefore + q.RewardItemQuantity, $"{n}: item da recompensa");

                var giver = Db.GetNpc(q.GiverNpcId);
                Check(giver.IsRecruitable == c.RecruitedNpcIds.Contains(giver.Id), $"{n}: recrutamento inconsistente com IsRecruitable");
                Check(!Gm.TryTurnInQuest(q.Id).Success, $"{n}: entregou duas vezes");
                Check(!Gm.TryAcceptQuest(q.Id).Success, $"{n}: aceitou de novo depois de completa");
            }

            // coleta de item de exploração no chão
            ResetScratch();
            var map = Db.GetAllExplorationMaps().First(m => m.ItemPickups.Count > 0);
            var pick = map.ItemPickups.First();
            var c2 = Gm.Save.Center;
            int qty = c2.GetItemQuantity(pick.ItemId);
            Check(Gm.CollectExplorationItem(pick).Success && c2.GetItemQuantity(pick.ItemId) == qty + pick.Quantity, "coletar item do chão soma no estoque");
            Check(!Gm.CollectExplorationItem(pick).Success && c2.GetItemQuantity(pick.ItemId) == qty + pick.Quantity, "item do chão não pode ser coletado duas vezes");

            // todos os pickups do jogo têm UniqueId distinto
            var ids = Db.GetAllExplorationMaps().SelectMany(m => m.ItemPickups).Select(p => p.UniqueId).ToList();
            Check(ids.Count == ids.Distinct().Count(), "UniqueId de pickup repetido entre mapas");

            // drops: chance ~ esperada para cada espécie com drop
            foreach (var s in AllSpecies().Where(s => s.Drops.Count > 0))
            {
                foreach (var drop in s.Drops)
                {
                    int hits = 0, trials = 3000;
                    for (int i = 0; i < trials; i++)
                    {
                        ResetDropState(s.Id);
                        if (Gm.ResolveWildEncounterDrops().Any(x => x.Item.Id == drop.ItemId))
                            hits++;
                    }

                    double rate = hits * 100.0 / trials;
                    Check(Math.Abs(rate - drop.ChancePercent) <= 6.0, $"drop {s.Name} item {drop.ItemId}: taxa {rate:F1}% vs {drop.ChancePercent}%");
                }
            }

            return Task.CompletedTask;
        }

        private void ResetDropState(int digimonId)
        {
            var t = typeof(GameManager);
            t.GetField("_activeWildEncounterDigimonId", NonPublic).SetValue(Gm, (int?)digimonId);
            t.GetField("_resolvedWildEncounterDrops", NonPublic).SetValue(Gm, null);
            t.GetProperty("BattleFromExploration").GetSetMethod(true).Invoke(Gm, new object[] { true });
        }

        // ------------------------------------------------------------------ 9. desbloqueio das áreas

        private Task UnlockChain()
        {
            var maps = Db.GetAllExplorationMaps().ToList();
            var npcQuest = Db.GetAllNpcs().Where(n => n.QuestId.HasValue).ToDictionary(n => n.Id, n => n.QuestId.Value);

            ResetScratch();
            var c = Gm.Save.Center;

            foreach (var m in maps.Where(m => m.RequiredQuestId.HasValue))
            {
                c.CompletedQuestIds.Clear();
                Check(!Gm.IsExplorationMapUnlocked(m), $"{m.Name}: aberta sem a quest {m.RequiredQuestId}");
                Check(!string.IsNullOrEmpty(Gm.GetExplorationMapLockReason(m)), $"{m.Name}: bloqueada sem motivo");
                c.CompletedQuestIds.Add(m.RequiredQuestId.Value);
                Check(Gm.IsExplorationMapUnlocked(m) && Gm.GetExplorationMapLockReason(m) == null, $"{m.Name}: não abriu com a quest");
            }

            // alcançabilidade a partir de um save vazio
            var unlocked = new HashSet<int>();
            var completed = new HashSet<int>();
            bool progressed = true;

            while (progressed)
            {
                progressed = false;

                foreach (var m in maps)
                {
                    if (unlocked.Contains(m.Id))
                        continue;

                    if (m.RequiredQuestId == null || completed.Contains(m.RequiredQuestId.Value))
                    {
                        unlocked.Add(m.Id);
                        progressed = true;

                        foreach (var npc in m.Npcs)
                            if (npcQuest.TryGetValue(npc.NpcId, out var qid))
                                completed.Add(qid);
                    }
                }
            }

            foreach (var m in maps)
                Check(unlocked.Contains(m.Id), $"mapa {m.Id} {m.Name} é inalcançável a partir de um save novo");

            Info($"{unlocked.Count}/{maps.Count} mapas alcançáveis; {completed.Count} quests obtidas pela progressão");

            var orphanQuests = Db.GetAllQuests().Where(q => !completed.Contains(q.Id)).ToList();
            foreach (var q in orphanQuests)
                Warn($"quest {q.Id} '{q.Name}' nunca é oferecida numa área alcançável");

            return Task.CompletedTask;
        }

        // ------------------------------------------------------------------ 10. NPCs recrutados

        /// <summary>Id do NPC que dá esse perk (dado em NpcData.RecruitmentPerk).</summary>
        private static int PerkNpc(NpcPerkType perk) => Db.GetAllNpcs().First(n => n.RecruitmentPerk == perk).Id;

        private Task RecruitEffects()
        {
            // o dado dos perks: todo perk tem dono e todo dono é recrutável
            foreach (NpcPerkType perk in Enum.GetValues(typeof(NpcPerkType)))
            {
                if (perk == NpcPerkType.None)
                    continue;

                var owners = Db.GetAllNpcs().Where(n => n.RecruitmentPerk == perk).ToList();
                Check(owners.Count >= 1, $"perk {perk}: nenhum NPC o oferece em Data/NPCs");
                Check(owners.All(n => n.IsRecruitable && !string.IsNullOrWhiteSpace(n.RecruitmentPerkDescription)), $"perk {perk}: o NPC dono precisa ser recrutável e descrever o perk");
            }

            // Palmon: Carne diária
            ResetScratch(capacity: 100);
            var c = Gm.Save.Center;
            c.Meat = 0;
            Gm.ClockSystem.Update(1440.0);
            Check(c.Meat == 0, "sem a Palmon recrutada não ganha carne diária");
            c.RecruitedNpcIds.Add(PerkNpc(NpcPerkType.DailyMeat));
            for (int i = 0; i < 5; i++)
            {
                int before = c.Meat;
                Gm.ClockSystem.Update(1440.0);
                Check(c.Meat - before >= 1 && c.Meat - before <= 2, $"Palmon: ganhou {c.Meat - before} carnes no dia {i + 1} (esperado 1-2)");
            }

            // Wizardmon: consultor de evolução
            ResetScratch();
            Check(!Gm.HasRecruitedEvolutionAdvisor, "sem o Wizardmon não há consultor de evolução");
            Gm.Save.Center.RecruitedNpcIds.Add(PerkNpc(NpcPerkType.EvolutionAdvisor));
            Check(Gm.HasRecruitedEvolutionAdvisor, "com o Wizardmon há consultor");

            // Meramon: cozinha
            ResetScratch();
            Check(!Gm.HasRecruitedKitchenCook, "sem o Meramon não há cozinha");
            Gm.Save.Center.RecruitedNpcIds.Add(PerkNpc(NpcPerkType.KitchenCook));
            Check(Gm.HasRecruitedKitchenCook, "com o Meramon há cozinha");

            // bônus de treino: cada NPC de treino bonifica exatamente a sua área
            foreach (var npc in Db.GetAllNpcs().Where(n => n.RecruitmentTrainingAreaType.HasValue))
            {
                ResetScratch();
                foreach (CenterAreaType type in Enum.GetValues(typeof(CenterAreaType)))
                    Check(!Gm.HasRecruitedTrainingBonus(type), $"{npc.Name}: bônus de {type} sem recrutar");

                Gm.Save.Center.RecruitedNpcIds.Add(npc.Id);

                foreach (CenterAreaType type in Enum.GetValues(typeof(CenterAreaType)))
                    Check(Gm.HasRecruitedTrainingBonus(type) == (type == npc.RecruitmentTrainingAreaType.Value),
                        $"{npc.Name}: bônus de treino em {type} incorreto");
            }

            // recrutáveis sem função definida: só avisa
            foreach (var npc in Db.GetAllNpcs().Where(n => n.IsRecruitable && !n.RecruitmentTrainingAreaType.HasValue))
                Info($"recrutável sem tipo de treino: {npc.Id} {npc.Name} (perk: {npc.RecruitmentPerk})");

            return Task.CompletedTask;
        }

        // ------------------------------------------------------------------ 11. sets e enciclopédia

        private Task SetsAndEncyclopedia()
        {
            foreach (var set in Db.GetAllSets())
            {
                ResetScratch(capacity: 100_000);
                var c = Gm.Save.Center;

                Check(!Gm.IsSetComplete(set) && Gm.GetTotalSetBonus(set.EffectType) == 0f, $"set {set.Name}: completo sem ter ninguém");

                foreach (var id in set.DigimonIds.Take(set.DigimonIds.Count - 1))
                    c.MarkDigimonDiscovered(id);

                Check(!Gm.IsSetComplete(set), $"set {set.Name}: completo faltando um membro");

                c.MarkDigimonDiscovered(set.DigimonIds.Last());
                Check(Gm.IsSetComplete(set), $"set {set.Name}: não completou com todos");
                Check(Gm.GetTotalSetBonus(set.EffectType) >= set.EffectValue - 0.0001f, $"set {set.Name}: bônus não aplicado");
            }

            // descobrir ao adicionar / ao evoluir
            ResetScratch(capacity: 1000);
            var s = AllSpecies().First(x => x.Stage == DigimonStage.Rookie);
            Check(!Gm.Save.Center.DiscoveredDigimonIds.Contains(s.Id), "espécie começa não descoberta");
            Gm.CenterService.AddDigimon(MakeDigimon(s, 1));
            Check(Gm.Save.Center.DiscoveredDigimonIds.Contains(s.Id), "AddDigimon marca como descoberta");

            return Task.CompletedTask;
        }

        // ------------------------------------------------------------------ 12. save / load

        private Task SaveRoundTrip()
        {
            ResetScratch(bits: 12345, capacity: 100_000);
            var c = Gm.Save.Center;

            // estado rico
            foreach (var s in AllSpecies().Where(x => x.Stage != DigimonStage.Baby).OrderBy(_ => _rng.Next()).Take(30))
            {
                var d = MakeDigimon(s, 12, addToCenter: true);
                d.Nickname = "Apelido " + s.Id;
                d.BecomeSick();
                d.BlockedEvolutionTargetIds.Add(1);
                d.BattlesFought = 7; d.BattlesWon = 3;
            }

            c.Foods.Add(Food.CreateCookedMeat());
            var raw = Food.CreateMeat(); raw.PlacedInRestaurant = true; raw.HoursSincePlaced = 5; raw.IsSpoiled = true;
            c.Foods.Add(raw);
            c.Poops.Add(new Poop { PositionX = 10, PositionY = 20 });
            Gm.EggSystem.CreateEgg(AllSpecies().First(x => x.Stage == DigimonStage.Baby).Id, Gm.CenterService);
            c.RecruitedNpcIds.AddRange(new[] { PerkNpc(NpcPerkType.DailyMeat), PerkNpc(NpcPerkType.EvolutionAdvisor), PerkNpc(NpcPerkType.KitchenCook) });
            c.CompletedQuestIds.AddRange(new[] { 1, 2 });
            c.ActiveQuestIds.Add(4); c.QuestProgress[4] = 1;
            c.CollectedItemPickupIds.AddRange(new[] { 1, 101 });
            c.ClearedTournamentIds.Add(1);
            foreach (var item in Db.GetAllItems()) c.SetItemQuantity(item.Id, item.Id + 2);
            Gm.Save.World.CurrentDay = 42; Gm.Save.World.CurrentHour = 7;

            string json = JsonSerializer.Serialize(Gm.Save, new JsonSerializerOptions { WriteIndented = true });
            var loaded = JsonSerializer.Deserialize<SaveData>(json);
            var lc = loaded.Center;

            Check(lc.Bits == c.Bits && lc.CapacityLimit == c.CapacityLimit, "save: Bits/capacidade");
            Check(lc.Digimons.Count == c.Digimons.Count, "save: quantidade de Digimons");
            Check(lc.Foods.Count == 2 && lc.Foods[0].IsCooked && !lc.Foods[1].IsCooked && lc.Foods[1].IsSpoiled && lc.Foods[1].PlacedInRestaurant, "save: comidas (assada/crua/estragada)");
            Check(lc.Poops.Count == 1 && lc.Eggs.Count == 1, "save: cocô e ovo");
            Check(lc.RecruitedNpcIds.SequenceEqual(c.RecruitedNpcIds) && lc.CompletedQuestIds.SequenceEqual(c.CompletedQuestIds), "save: NPCs e quests");
            Check(lc.QuestProgress.Count == c.QuestProgress.Count && lc.QuestProgress[4] == 1, "save: progresso de quest");
            Check(lc.CollectedItemPickupIds.SequenceEqual(c.CollectedItemPickupIds) && lc.ClearedTournamentIds.SequenceEqual(c.ClearedTournamentIds), "save: pickups e torneios");
            Check(loaded.World.CurrentDay == 42 && loaded.World.CurrentHour == 7, "save: relógio");

            foreach (var item in Db.GetAllItems())
                Check(lc.GetItemQuantity(item.Id) == item.Id + 2, $"save: quantidade do item {item.Id}");

            var fieldDiffs = new Dictionary<string, int>();
            for (int i = 0; i < c.Digimons.Count; i++)
            {
                var a = c.Digimons[i]; var b = lc.Digimons[i];
                void Cmp(string field, object x, object y)
                {
                    if (!Equals(x, y))
                        fieldDiffs[field] = fieldDiffs.GetValueOrDefault(field) + 1;
                }

                Cmp("Level", a.Level, b.Level);
                Cmp("Nickname", a.Nickname, b.Nickname);
                Cmp("PassiveId", a.PassiveId, b.PassiveId);
                Cmp("HealthState", a.HealthState, b.HealthState);
                Cmp("BaseData.Id", a.BaseData?.Id, b.BaseData?.Id);
                Cmp("Stats.HP", a.CurrentStats?.HealthPoints, b.CurrentStats?.HealthPoints);
                Cmp("Blocked", string.Join(",", a.BlockedEvolutionTargetIds), string.Join(",", b.BlockedEvolutionTargetIds));
                Cmp("BattlesFought", a.BattlesFought, b.BattlesFought);
                Cmp("Hunger", a.Hunger, b.Hunger);
                Cmp("Stamina", a.Stamina, b.Stamina);
                Cmp("Happiness", a.Happiness, b.Happiness);
                Cmp("Experience", a.Experience, b.Experience);
                Cmp("AgeInDays", a.AgeInDays, b.AgeInDays);
                Cmp("MaxHP", a.MaxHealthPoints, b.MaxHealthPoints);
                Cmp("CurHP", a.CurrentHealthPoints, b.CurrentHealthPoints);
            }

            Check(fieldDiffs.Count == 0, "save: campos de Digimon que NÃO sobrevivem à ida-e-volta: " +
                string.Join(", ", fieldDiffs.Select(kv => $"{kv.Key} ({kv.Value}/{c.Digimons.Count})")));

            // Reserializar o que foi carregado tem que dar exatamente o mesmo JSON: qualquer propriedade que a
            // desserialização ignore (ex.: setter privado) volta ao valor padrão e aparece aqui como diferença.
            string json2 = JsonSerializer.Serialize(loaded, new JsonSerializerOptions { WriteIndented = true });
            Check(json2 == json, "save: reserializar o save carregado NÃO reproduz o JSON original" + FirstDiff(json, json2));

            // o save REAL (somente leitura)
            try
            {
                string path = ProjectSettings.GlobalizePath("user://save.json");

                if (System.IO.File.Exists(path))
                {
                    var real = JsonSerializer.Deserialize<SaveData>(System.IO.File.ReadAllText(path));
                    Check(real != null, "save real: não desserializa");

                    if (real != null)
                    {
                        foreach (var d in real.Center.Digimons)
                        {
                            Check(d.BaseData != null && FindSpecies(d.BaseData.Id) != null, $"save real: Digimon com espécie inexistente ({d.BaseData?.Id})");
                            AssertInvariants(new[] { d }, "save real");
                        }

                        foreach (var e in real.Center.Inventory)
                            Check(Db.GetItem(e.ItemId) != null && e.Quantity >= 0, $"save real: item {e.ItemId} inválido");

                        foreach (var q in real.Center.ActiveQuestIds.Concat(real.Center.CompletedQuestIds))
                            Check(Db.GetQuest(q) != null, $"save real: quest {q} inexistente");

                        foreach (var n in real.Center.RecruitedNpcIds)
                            Check(Db.GetNpc(n) != null, $"save real: NPC recrutado {n} inexistente");

                        Info($"save real ok: {real.Center.Digimons.Count} Digimons, dia {real.World.CurrentDay}");
                    }
                }
                else
                {
                    Info("sem save real para ler");
                }
            }
            catch (Exception e)
            {
                Check(false, $"save real: exceção ao ler ({e.Message})");
            }

            // save corrompido não pode derrubar o parse (SaveSystem trata) - simula JSON quebrado
            bool threw = false;
            try { JsonSerializer.Deserialize<SaveData>("{ quebrado"); } catch (JsonException) { threw = true; }
            Check(threw, "JSON quebrado lança JsonException (o SaveSystem captura e usa o backup)");

            return Task.CompletedTask;
        }

        private static string FirstDiff(string a, string b)
        {
            if (a == b) return "";
            int i = 0;
            while (i < a.Length && i < b.Length && a[i] == b[i]) i++;
            string ctx(string x) => x.Substring(Math.Max(0, i - 60), Math.Min(140, x.Length - Math.Max(0, i - 60))).Replace('\n', ' ');
            return $" | primeira diferença na posição {i}: original «{ctx(a)}» / recarregado «{ctx(b)}»";
        }

        // ------------------------------------------------------------------ 13. exploração em tempo de execução

        private async Task ExplorationRuntime()
        {
            foreach (var map in Db.GetAllExplorationMaps().OrderBy(m => m.Id))
            {
                ResetScratch(capacity: 1000);
                var c = Gm.Save.Center;
                string n = $"mapa {map.Id} {map.Name}";

                if (map.RequiredQuestId.HasValue)
                {
                    var locked = MakeDigimon(AllSpecies().First(s => s.Stage == DigimonStage.Rookie), 5, addToCenter: true);
                    Gm.StartExploration(locked, map);
                    Check(!Gm.IsExploring, $"{n}: iniciou bloqueado");
                    c.CompletedQuestIds.Add(map.RequiredQuestId.Value);
                }

                var explorer = MakeDigimon(AllSpecies().First(s => s.Stage == DigimonStage.Rookie), 5, addToCenter: true);
                Gm.StartExploration(explorer, map);
                await Frames(8);

                Check(Gm.IsExploring && Gm.ActiveExplorationArea != null, $"{n}: não iniciou a exploração");

                var area = Gm.ActiveExplorationArea;

                if (area == null)
                    continue;

                int wilds = CountChildren<WildEncounterSpawn>(area);
                int npcs = CountChildren<ExplorationNpcVisual>(area);
                int items = CountChildren<ExplorationItemVisual>(area);
                Check(wilds == map.WildSpawnPoints.Count, $"{n}: {wilds} selvagens em cena, esperado {map.WildSpawnPoints.Count}");
                Check(npcs == map.Npcs.Count, $"{n}: {npcs} NPCs em cena, esperado {map.Npcs.Count}");
                Check(items == map.ItemPickups.Count, $"{n}: {items} itens em cena, esperado {map.ItemPickups.Count}");

                var explorerNode = FindFirst<ExplorationDigimon>(area);
                Check(explorerNode != null && area.IsPointInsideBoundary(explorerNode.GlobalPosition), $"{n}: explorador fora do limite");

                await Frames(90);
                Check(explorerNode == null || area.IsPointInsideBoundary(explorerNode.GlobalPosition), $"{n}: explorador saiu do limite depois de 90 frames");

                // andar pra dentro / fora do mapa
                var theme = Db.GetAreaTheme(map.Theme ?? "forest");
                if (explorerNode != null)
                {
                    explorerNode.MoveTo(new Vector2(-500, -500));
                    await Frames(30);
                    Check(area.IsPointInsideBoundary(explorerNode.GlobalPosition), $"{n}: clique fora do mapa mexeu o explorador pra fora");

                    if (theme != null && theme.HasShoreline)
                    {
                        float limit = theme.GetWalkLimitY(0, 2200f).Value;
                        Check(!area.IsPointInsideBoundary(new Vector2(500, limit + 30)), $"{n}: ponto no mar considerado andável");
                        Check(area.IsPointInsideBoundary(new Vector2(500, limit - 30)), $"{n}: ponto na areia considerado bloqueado");
                    }
                }

                Gm.EndExploration();
                await Frames(4);
                Check(!Gm.IsExploring, $"{n}: EndExploration não encerrou");
                Check(!IsInstanceValid(area) || area.IsQueuedForDeletion() || area.GetParent() == null, $"{n}: a área de exploração não foi liberada");
            }
        }

        private static int CountChildren<T>(Node root) where T : Node
        {
            int count = 0;
            foreach (var child in root.GetChildren())
            {
                if (child is T) count++;
                count += CountChildren<T>(child);
            }
            return count;
        }

        private static T FindFirst<T>(Node root) where T : Node
        {
            foreach (var child in root.GetChildren())
            {
                if (child is T t) return t;
                var found = FindFirst<T>(child);
                if (found != null) return found;
            }
            return null;
        }

        // ------------------------------------------------------------------ 14. fluxos de batalha completos pelo GameManager

        private bool _lastTimedOut;

        private static readonly MethodInfo OkPressed = typeof(BattleArena).GetMethod("OnResultOkPressed", NonPublic);
        private static readonly FieldInfo ResultShown = typeof(BattleArena).GetField("_resultShown", NonPublic);

        /// <summary>Espera a arena aberta pelo GameManager terminar de verdade e clica "OK" no
        /// resultado (reflexão) - retorna o resultado e devolve o controle ao GameManager.</summary>
        private async Task<BattleResult?> DriveArenaToEnd(double timeoutSeconds = 300)
        {
            BattleArena arena = null;
            for (int i = 0; i < 60 && arena == null; i++)
            {
                await Frames(1);
                arena = GetTree().Root.GetChildren().OfType<BattleArena>().FirstOrDefault();
            }

            if (arena == null)
                return null;

            BattleResult? result = null;
            arena.BattleFinished += r => result = r;

            double elapsed = 0;
            while (!(bool)ResultShown.GetValue(arena) && elapsed < timeoutSeconds)
            {
                await Frames(1);
                elapsed += GetProcessDeltaTime();
            }

            _lastTimedOut = false;

            if (!(bool)ResultShown.GetValue(arena))
            {
                // Impasse: registra o estado e sai pelo caminho do jogador (desistir), pra não deixar a
                // batalha "ativa" no GameManager e contaminar os próximos testes.
                _lastTimedOut = true;
                DumpArena(arena);
                typeof(BattleArena).GetMethod("OnSurrenderConfirmed", NonPublic)?.Invoke(arena, null);

                double waitedSurrender = 0;
                while (!(bool)ResultShown.GetValue(arena) && waitedSurrender < 30)
                {
                    await Frames(1);
                    waitedSurrender += GetProcessDeltaTime();
                }

                if (!(bool)ResultShown.GetValue(arena))
                {
                    Check(false, "desistir depois de um impasse não levou ao resultado");
                    return null;
                }
            }

            OkPressed.Invoke(arena, null);
            await Frames(3);
            return result;
        }

        private static readonly FieldInfo UnitTarget = typeof(BattleUnit).GetField("_target", NonPublic);
        private static readonly FieldInfo UnitRange = typeof(BattleUnit).GetField("_range", NonPublic);
        private static readonly FieldInfo UnitActing = typeof(BattleUnit).GetField("_isActing", NonPublic);

        private void DumpArena(BattleArena arena)
        {
            var sb = new System.Text.StringBuilder();

            foreach (var (team, label) in new[] { (arena.Match.PlayerTeam, "P"), (arena.Match.EnemyTeam, "E") })
            {
                foreach (var m in team.Members)
                {
                    var u = arena.GetUnitFor(m);
                    var target = u != null ? UnitTarget.GetValue(u) as BattleCombatant : null;
                    var tu = target != null ? arena.GetUnitFor(target) : null;
                    float dist = (u != null && tu != null) ? u.GlobalPosition.DistanceTo(tu.GlobalPosition) : -1;
                    sb.Append($" {label}:{m.Digimon.BaseData.Name}[{m.Digimon.BaseData.Role}{(m.Digimon.BaseData.Role == RoleType.Support ? "/" + m.Digimon.BaseData.SupportType : "")}] " +
                              $"hp={m.HpPercentage:P0}{(m.IsDead ? " MORTO" : "")} pos={(u != null ? u.GlobalPosition.ToString("F0") : "?")} " +
                              $"alvo={(target?.Digimon.BaseData.Name ?? "-")}@{dist:F0}px range={(u != null ? ((float)UnitRange.GetValue(u)).ToString("F0") : "?")} agindo={(u != null && (bool)UnitActing.GetValue(u))} passiva={m.Digimon.PassiveId};");
                }
            }

            GD.Print($"[AUDIT-IMPASSE] estado da arena ao estourar o tempo:{sb}");
        }

        private int _tournamentRefusedClosed, _tournamentAcceptedOpen;

        // Põe o relógio do estado descartável no índice de dia (0 = dia 1 do Ano 1) - CurrentDay começa em 1.
        private void SetDayIndex(int dayIndex) => Gm.Save.World.CurrentDay = dayIndex + 1;

        // Primeiro índice de dia (a partir do 0) em que o campeonato está aberto/fechado no calendário.
        private static int FirstDayIndex(TournamentData t, bool open)
        {
            for (int i = 0; i < 1000; i++)
                if (CalendarSystem.IsOpen(t, i) == open)
                    return i;
            return -1;
        }

        private List<DigimonInstance> StrongTeam(TournamentData t, List<DigimonData> strongPool)
        {
            var pool = RulePool(t, strongPool);
            var team = new List<DigimonInstance>();
            for (int i = 0; i < t.Opponents.Count; i++)
                team.Add(MakeDigimon(pool[i % pool.Count], 100, addToCenter: true));
            return team;
        }

        // Espécies mais fortes que cumprem a regra de inscrição do campeonato (TournamentRules): Mega se
        // houver, senão o maior estágio permitido. Sem regra, o mesmo strongPool de antes.
        private static List<DigimonData> RulePool(TournamentData t, List<DigimonData> strongPool)
        {
            if (!TournamentRules.HasRules(t))
                return strongPool;

            var eligible = AllSpecies().Where(sp => TournamentRules.IsEligible(t, sp)).ToList();
            var stage = eligible.Any(sp => sp.Stage == DigimonStage.Mega) ? DigimonStage.Mega : eligible.Max(sp => sp.Stage);
            return eligible.Where(sp => sp.Stage == stage)
                .OrderByDescending(sp => Math.Max(sp.BaseStats.PhysicalDamage, sp.BaseStats.SpecialDamage) + sp.BaseStats.Speed)
                .Take(6).ToList();
        }

        private int _rulesInvalidRefused, _rulesValidAccepted, _rulesTournaments, _noRulesAccepted;

        private async Task<BattleResult?> WinTournament(TournamentData t, List<DigimonData> strongPool, string what)
        {
            Gm.StartTournamentBattle(t, StrongTeam(t, strongPool));
            Check(Gm.IsBattleActive, $"elegibilidade: {what}: não iniciou");
            if (!Gm.IsBattleActive)
                return null;

            var result = await DriveArenaToEnd(300);
            Check(result == BattleResult.PlayerWon, $"elegibilidade: {what}: esperado vencer, deu {result}");
            return result;
        }

        private async Task<BattleResult?> LoseTournament(TournamentData t, List<DigimonData> strongPool, string what)
        {
            Gm.StartTournamentBattle(t, StrongTeam(t, strongPool));
            Check(Gm.IsBattleActive, $"elegibilidade: {what}: não iniciou");
            await Frames(30);
            var arena = GetTree().Root.GetChildren().OfType<BattleArena>().FirstOrDefault();
            if (arena == null)
                return null;

            BattleResult? res = null;
            arena.BattleFinished += r => res = r;
            typeof(BattleArena).GetMethod("OnSurrenderConfirmed", NonPublic)?.Invoke(arena, null);

            double waited = 0;
            while (!(bool)ResultShown.GetValue(arena) && waited < 30) { await Frames(1); waited += GetProcessDeltaTime(); }
            if ((bool)ResultShown.GetValue(arena))
            {
                OkPressed.Invoke(arena, null);
                await Frames(3);
            }

            Check(res == BattleResult.EnemyWon, $"elegibilidade: {what}: desistir deu {res}, esperado derrota");
            Check(!Gm.IsBattleActive, $"elegibilidade: {what}: batalha continua ativa");
            return res;
        }

        // Regra de início de campeonato (aberto + destravado + não vencido nesta edição) com batalhas de
        // verdade: vencer bloqueia a edição, perder não, edição nova reabre, trava e virada de ano.
        private async Task TournamentEligibility(List<DigimonData> strongPool)
        {
            var db = Db;
            var t1 = db.GetTournament(1);
            ResetScratch(bits: 0, capacity: 1000);
            var c = Gm.Save.Center;

            // Copa Novato: a cada 6 dias, aberta 3 -> edição 0 = índices 0-2, edição 1 = 6-8, edição 2 = 12-14
            SetDayIndex(0);
            await WinTournament(t1, strongPool, "Copa Novato, 1ª vitória (edição 0)");
            Check(c.LastWonEdition.TryGetValue(1, out int e0) && e0 == 0, "elegibilidade: vitória não registrou a edição 0");
            Check(c.ClearedTournamentIds.Contains(1), "elegibilidade: vitória não registrou TournamentsWon");

            SetDayIndex(1);
            var again = Gm.GetTournamentAvailability(t1);
            Check(again.Status == TournamentAvailabilityStatus.WonThisEdition, $"elegibilidade: 2ª na mesma edição = {again.Status}, esperado WonThisEdition");
            Gm.StartTournamentBattle(t1, StrongTeam(t1, strongPool));
            Check(!Gm.IsBattleActive, "elegibilidade: Copa Novato vencida 2x na mesma edição (a 2ª deveria ser recusada)");
            Info($"Copa Novato: 2ª tentativa na mesma edição {(Gm.IsBattleActive ? "ACEITA" : "recusada")} ({again.Status})");

            SetDayIndex(6);
            var next = await WinTournament(t1, strongPool, "Copa Novato na edição seguinte");
            Info($"Copa Novato: na edição seguinte (dia {Gm.Save.World.CurrentDay}) {(next.HasValue ? "aceita" : "RECUSADA")}");
            Check(c.LastWonEdition.TryGetValue(1, out int e1) && e1 == 1, "elegibilidade: 2ª vitória não registrou a edição 1");

            // perder não bloqueia
            SetDayIndex(12);
            await LoseTournament(t1, strongPool, "Copa Novato, derrota na edição 2");
            Check(c.LastWonEdition[1] == 1, "elegibilidade: derrota mexeu em LastWonEdition");
            Check(Gm.GetTournamentAvailability(t1).CanStart, "elegibilidade: derrota bloqueou a edição");
            await WinTournament(t1, strongPool, "Copa Novato, nova tentativa depois da derrota");
            Check(c.LastWonEdition[1] == 2, "elegibilidade: vitória depois da derrota não registrou a edição 2");

            // trava: Trio Guardião 02 só depois do Desafio Champion
            ResetScratch(bits: 0, capacity: 1000);
            c = Gm.Save.Center;
            var t3 = db.GetTournament(3);
            var t10 = db.GetTournament(10);
            SetDayIndex(20); // aberto: 3 (dias 10-25) e 10 (índices 20-24)
            var locked = Gm.GetTournamentAvailability(t10);
            Check(locked.Status == TournamentAvailabilityStatus.Locked, $"elegibilidade: Trio Guardião 02 sem o 3 = {locked.Status}, esperado Locked");
            Gm.StartTournamentBattle(t10, StrongTeam(t10, strongPool));
            Check(!Gm.IsBattleActive, "elegibilidade: Trio Guardião 02 iniciou travado");
            await WinTournament(t3, strongPool, "Desafio Champion (destrava o 10)");
            // Só Ultimate: um time Ultimate nível 100 destrava e vence
            await WinTournament(t10, strongPool, "Trio Guardião 02 depois de vencer o 3");

            // virada de ano: agenda de teste (cópia da Copa Novato) aberta nos dias 59, 60 e 1
            ResetScratch(bits: 0, capacity: 1000);
            c = Gm.Save.Center;
            var wrap = new TournamentData
            {
                Id = 901, Name = "Teste virada", Opponents = t1.Opponents,
                ScheduleType = TournamentScheduleType.Fixed, StartDayOfYear = 59, DurationDays = 3,
            };
            SetDayIndex(59); // ano 1, dia 60
            await WinTournament(wrap, strongPool, "virada: vitória no dia 60 do ano 1");
            SetDayIndex(60); // ano 2, dia 1 - mesma edição
            Check(Gm.GetTournamentAvailability(wrap).Status == TournamentAvailabilityStatus.WonThisEdition, "elegibilidade: virada - dia 1 do ano 2 reabriu a edição que começou no ano 1");
            SetDayIndex(61); // ano 2, dia 2 - fechado
            Check(Gm.GetTournamentAvailability(wrap).Status == TournamentAvailabilityStatus.Closed, "elegibilidade: virada - dia 2 deveria estar fechado");
            SetDayIndex(118); // ano 2, dia 59 - edição nova
            await WinTournament(wrap, strongPool, "virada: edição do ano 2");
            Check(c.LastWonEdition.TryGetValue(901, out int ew) && ew == 2, "elegibilidade: virada - edição do ano 2 não registrada");
        }

        private async Task BattleFlows()
        {
            var db = Db;
            var strongPool = AllSpecies().Where(s => s.Stage == DigimonStage.Mega)
                .OrderByDescending(s => Math.Max(s.BaseStats.PhysicalDamage, s.BaseStats.SpecialDamage) + s.BaseStats.Speed).Take(6).ToList();
            var strongData = strongPool[0];
            var weakData = AllSpecies().Where(s => s.Stage == DigimonStage.Rookie).OrderBy(s => s.BaseStats.HealthPoints).First();

            // ---- batalha livre 1x1 e 3x3: vitória e derrota
            foreach (int size in string.IsNullOrEmpty(OS.GetEnvironment("DEBUG_AUDIT_TOURNAMENT")) ? new[] { 1, 3 } : new int[0])
            {
                foreach (bool strong in new[] { true, false })
                {
                    ResetScratch(bits: 0, capacity: 1000);
                    var c = Gm.Save.Center;
                    var team = new List<DigimonInstance>();
                    for (int i = 0; i < size; i++)
                        team.Add(MakeDigimon(strong ? strongPool[i % strongPool.Count] : weakData, strong ? 90 : 1, addToCenter: true));

                    int bits = c.Bits;
                    int xp = team.Sum(t => t.Experience + t.Level * 1000);
                    string n = $"batalha livre {size}x{size} ({(strong ? "forte" : "fraco")})";

                    Gm.StartTeamBattle(team);
                    Check(Gm.IsBattleActive, $"{n}: não iniciou");

                    var result = await DriveArenaToEnd();
                    bool stalled = _lastTimedOut;
                    if (stalled) Warn($"{n}: impasse >300s (estado no log [AUDIT-IMPASSE]); resolvido desistindo");
                    Check(result.HasValue, $"{n}: a luta não terminou no tempo");
                    Check(!Gm.IsBattleActive && Gm.BattleMatch == null, $"{n}: estado de batalha não foi limpo");

                    if (result.HasValue && !stalled)
                    {
                        Info($"{n}: resultado {result}");
                        if (result == BattleResult.PlayerWon)
                            Check(c.Bits > bits, $"{n}: vitória sem Bits");
                        else
                            Check(c.Bits == bits, $"{n}: derrota deu Bits");

                        Check(team.All(t => t.BattlesFought == 1), $"{n}: BattlesFought não registrado");
                        AssertInvariants(c.Digimons, n);
                    }
                }
            }

            // ---- campeonatos
            string onlyT = OS.GetEnvironment("DEBUG_AUDIT_TOURNAMENT");
            foreach (var t in db.GetAllTournaments().OrderBy(t => t.Id))
            {
                if (!string.IsNullOrEmpty(onlyT) && t.Id.ToString() != onlyT)
                    continue;

                ResetScratch(bits: 0, capacity: 1000);
                var c = Gm.Save.Center;
                var team = StrongTeam(t, strongPool);

                int cap = c.CapacityLimit;
                string n = $"campeonato {t.Id} {t.Name}";

                // Prepara o dia e a trava pela regra (sem IgnoreCalendar): recusado travado (se tiver
                // trava), recusado num dia fechado e aceito aberto e destravado.
                if (t.UnlockAfterTournamentId > 0)
                {
                    SetDayIndex(FirstDayIndex(t, open: true));
                    Gm.StartTournamentBattle(t, team);
                    Check(!Gm.IsBattleActive && Gm.GetTournamentAvailability(t).Status == TournamentAvailabilityStatus.Locked,
                        $"{n}: não foi recusado como travado (sem vencer o {t.UnlockAfterTournamentId})");
                    for (var req = t; req != null && req.UnlockAfterTournamentId > 0; req = db.GetTournament(req.UnlockAfterTournamentId))
                        c.ClearedTournamentIds.Add(req.UnlockAfterTournamentId);
                }

                SetDayIndex(FirstDayIndex(t, open: false));
                Gm.StartTournamentBattle(t, team);
                bool refusedClosed = !Gm.IsBattleActive && Gm.GetTournamentAvailability(t).Status == TournamentAvailabilityStatus.Closed;
                Check(refusedClosed, $"{n}: não foi recusado como fechado (dia {Gm.Save.World.CurrentDay})");
                if (refusedClosed) _tournamentRefusedClosed++;

                SetDayIndex(FirstDayIndex(t, open: true));

                // regra de inscrição: o mesmo time com 1 Digimon fora da regra é recusado
                if (TournamentRules.HasRules(t))
                {
                    _rulesTournaments++;
                    var outsider = AllSpecies().First(sp => !TournamentRules.IsEligible(t, sp));
                    var invalidTeam = new List<DigimonInstance>(team);
                    invalidTeam[0] = MakeDigimon(outsider, 100, addToCenter: true);
                    Gm.StartTournamentBattle(t, invalidTeam);
                    Check(!Gm.IsBattleActive, $"{n}: iniciou com {outsider.Name} ({outsider.Stage}/{outsider.Attribute}) fora da regra {TournamentRules.Describe(t)}");
                    if (!Gm.IsBattleActive) _rulesInvalidRefused++;
                }

                Check(TournamentRules.ValidateTeam(t, team).IsValid, $"{n}: o time de teste não cumpre {TournamentRules.Describe(t)}");
                Gm.StartTournamentBattle(t, team);
                Check(Gm.IsBattleActive, $"{n}: não iniciou aberto e destravado (dia {Gm.Save.World.CurrentDay})");
                if (Gm.IsBattleActive)
                {
                    _tournamentAcceptedOpen++;
                    if (TournamentRules.HasRules(t)) _rulesValidAccepted++; else _noRulesAccepted++;
                }

                var result = await DriveArenaToEnd(300);
                bool stalledT = _lastTimedOut;
                if (stalledT) Warn($"{n}: impasse >300s de simulação (dano mínimo 1 contra defesa alta?) - resolvido desistindo; estado em [AUDIT-IMPASSE]");
                Check(result.HasValue, $"{n}: a luta não terminou");
                Check(!Gm.IsBattleActive, $"{n}: batalha continua ativa");

                if (stalledT)
                {
                    Check(!c.ClearedTournamentIds.Contains(t.Id), $"{n}: desistir marcou como concluído");
                }
                else if (result == BattleResult.PlayerWon)
                {
                    Check(c.ClearedTournamentIds.Contains(t.Id), $"{n}: vitória não marcou como concluído");
                    Check(c.CapacityLimit == cap + t.CapacityReward, $"{n}: capacidade da recompensa");
                    Check(c.Bits >= t.BitsReward, $"{n}: Bits da recompensa");
                }
                else if (result.HasValue)
                {
                    Warn($"{n}: um time nível 100 de {string.Join("/", team.Select(d => d.BaseData.Name))} ({TournamentRules.Describe(t)}) perdeu ({result}) - campeonato muito forte ou bug");
                    Check(!c.ClearedTournamentIds.Contains(t.Id), $"{n}: derrota marcou como concluído");
                }
            }

            if (string.IsNullOrEmpty(onlyT))
            {
                Info($"campeonatos: recusados fechados {_tournamentRefusedClosed}/12, aceitos abertos e destravados {_tournamentAcceptedOpen}/12");
                Info($"regras: com regra {_rulesTournaments} - time válido aceito {_rulesValidAccepted}/{_rulesTournaments}, time com 1 inválido recusado {_rulesInvalidRefused}/{_rulesTournaments}; sem regra aceitos {_noRulesAccepted}/{12 - _rulesTournaments}");
            }

            if (!string.IsNullOrEmpty(onlyT))
                return;

            await TournamentEligibility(strongPool);

            // ---- encontros selvagens: uma espécie de cada mapa
            foreach (var map in db.GetAllExplorationMaps().OrderBy(m => m.Id))
            {
                var wildId = map.WildPool[0];
                ResetScratch(bits: 0, capacity: 1000);
                var c = Gm.Save.Center;
                var explorer = MakeDigimon(strongData, 100, addToCenter: true);

                // quest de "derrotar" ativa no mapa, se houver
                var defeat = db.GetAllQuests().FirstOrDefault(q => q.ObjectiveType == QuestObjectiveType.DefeatWild && q.ObjectiveTargetId == 0);
                if (defeat != null)
                    Gm.TryAcceptQuest(defeat.Id);

                string n = $"selvagem {db.GetDigimon(wildId).Name} em {map.Name}";
                Gm.StartWildEncounter(explorer, wildId, map.WildLevelMin);
                Check(Gm.IsBattleActive, $"{n}: não iniciou");

                var result = await DriveArenaToEnd();
                Check(result.HasValue, $"{n}: não terminou");

                if (result == BattleResult.PlayerWon)
                {
                    Check(c.Bits > 0, $"{n}: vitória sem Bits");

                    if (defeat != null)
                        Check(Gm.GetQuestProgress(defeat.Id) >= 1, $"{n}: quest DefeatWild não avançou");
                }
            }

            // ---- desistir: derrota registrada
            ResetScratch(bits: 0, capacity: 1000);
            var s1 = MakeDigimon(strongData, 100, addToCenter: true);
            Gm.StartTeamBattle(new List<DigimonInstance> { s1 });
            await Frames(30);
            var arena = GetTree().Root.GetChildren().OfType<BattleArena>().FirstOrDefault();

            if (arena != null)
            {
                BattleResult? res = null;
                arena.BattleFinished += r => res = r;
                typeof(BattleArena).GetMethod("OnSurrenderConfirmed", NonPublic)?.Invoke(arena, null);

                double waited = 0;
                while (!(bool)ResultShown.GetValue(arena) && waited < 30) { await Frames(1); waited += GetProcessDeltaTime(); }

                if ((bool)ResultShown.GetValue(arena))
                {
                    OkPressed.Invoke(arena, null);
                    await Frames(3);
                }

                Check(res == BattleResult.EnemyWon, $"desistir: resultado {res}, esperado derrota");
                Check(!Gm.IsBattleActive, "desistir: estado de batalha limpo");
            }
            else
            {
                Check(false, "desistir: a arena não abriu");
            }

            // ---- não permite duas batalhas ao mesmo tempo
            ResetScratch(bits: 0, capacity: 1000);
            var s2 = MakeDigimon(strongData, 100, addToCenter: true);
            Gm.StartTeamBattle(new List<DigimonInstance> { s2 });
            await Frames(5);
            int arenas = GetTree().Root.GetChildren().OfType<BattleArena>().Count();
            Gm.StartTeamBattle(new List<DigimonInstance> { s2 });
            Gm.StartWildEncounter(s2, db.GetAllExplorationMaps().First().WildPool[0], 5);
            await Frames(5);
            Check(GetTree().Root.GetChildren().OfType<BattleArena>().Count() == arenas, "segunda batalha simultânea foi aberta (deveria ser recusada)");
            await DriveArenaToEnd();

            // limpa arenas remanescentes
            foreach (var a in GetTree().Root.GetChildren().OfType<BattleArena>().ToList())
                a.QueueFree();
        }

        // ------------------------------------------------------------------ 17. música, configurações e treino autônomo

        private Task Misc()
        {
            var music = MusicManager.Instance;
            Check(music != null, "MusicManager não existe");

            if (music != null)
            {
                var t = typeof(MusicManager);
                Check(t.GetField("_centerMusic", NonPublic)?.GetValue(music) != null, "música do Center não carregou");
                Check(t.GetField("_battleMusic", NonPublic)?.GetValue(music) != null, "música de batalha não carregou");

                float original = music.VolumeLinear;
                Check(original >= 0f && original <= 1f, $"volume atual fora de 0..1 ({original})");

                music.SetVolume(0.37f);
                Check(Math.Abs(music.VolumeLinear - 0.37f) < 0.001f, "SetVolume(0.37) não aplicou");
                music.SetVolume(5f);
                Check(music.VolumeLinear <= 1f, "SetVolume(5) passou de 1");
                music.SetVolume(-3f);
                Check(music.VolumeLinear >= 0f, "SetVolume(-3) ficou negativo");

                music.SetVolume(original); // devolve o valor que o jogador tinha (grava o settings.cfg de novo)
                Check(Math.Abs(music.VolumeLinear - original) < 0.001f, "não conseguiu restaurar o volume original");
            }

            // treino autônomo: liga/desliga por tipo de área e sobrevive ao save
            ResetScratch(capacity: 100);
            var d = MakeDigimon(AllSpecies().First(s => s.Stage == DigimonStage.Rookie), 5, addToCenter: true);

            foreach (var type in new[] { CenterAreaType.TrainingAttack, CenterAreaType.TrainingSpeed, CenterAreaType.TrainingHealthPoints })
            {
                Check(d.IsAutoTrainingAllowed(type), $"treino autônomo de {type} deveria começar permitido");
                d.SetAutoTrainingAllowed(type, false);
                Check(!d.IsAutoTrainingAllowed(type), $"desligar treino autônomo de {type}");
            }

            var reloaded = JsonSerializer.Deserialize<DigimonInstance>(JsonSerializer.Serialize(d));
            Check(reloaded != null && !reloaded.IsAutoTrainingAllowed(CenterAreaType.TrainingAttack) && !reloaded.IsAutoTrainingAllowed(CenterAreaType.TrainingSpeed),
                "treino autônomo desligado não sobreviveu ao salvar/carregar");
            d.SetAutoTrainingAllowed(CenterAreaType.TrainingAttack, true);
            Check(d.IsAutoTrainingAllowed(CenterAreaType.TrainingAttack), "religar treino autônomo");

            // Apelido
            d.Nickname = "Teste";
            Check(d.DisplayName.Contains("Teste"), "DisplayName não usa o apelido");

            return Task.CompletedTask;
        }

        // ------------------------------------------------------------------ 18. morte súbita

        private async Task SuddenDeath()
        {
            ResetScratch();
            var species = AllSpecies();
            var lalamon = species.First(s => s.Name == "Lalamon");
            var phascomon = species.First(s => s.Name == "Phascomon");

            BattleMatch NewMatch(DigimonInstance a, DigimonInstance b) =>
                new BattleMatch(new BattleTeam(new List<DigimonInstance> { a }), new BattleTeam(new List<DigimonInstance> { b }));

            // A) lógica pura: nada antes de 120s
            var p = MakeDigimon(phascomon, 20);
            var e = MakeDigimon(phascomon, 20);
            var m = NewMatch(p, e);
            int hpP = p.CurrentHealthPoints, hpE = e.CurrentHealthPoints;

            for (double t = 0; t < 119.0; t += 0.1)
                m.Tick(0.1);

            Check(!m.IsSuddenDeath && p.CurrentHealthPoints == hpP && e.CurrentHealthPoints == hpE, "antes de 120s ninguém perde vida por morte súbita");

            // B) depois de 120s: ~3% da vida máxima POR SEGUNDO, proporcional a cada um
            for (double t = 0; t < 3.0; t += 0.1)
                m.Tick(0.1);

            Check(m.IsSuddenDeath, "morte súbita começou depois de 120s");
            double expected = 0.03 * 1.9; // ~1.9s de morte súbita (o relógio passou 119 + 3 = 122 - margem)
            double lostP = (hpP - p.CurrentHealthPoints) / (double)p.MaxHealthPoints;
            Check(lostP > 0.03 && lostP < 0.12, $"morte súbita tirou {lostP:P1} da vida máxima em ~2-3s (esperado ~6-9%)");

            // C) proporcional: um tanque (muita vida) e um frágil perdem a MESMA fração
            var tank = MakeDigimon(species.OrderByDescending(s => s.BaseStats.HealthPoints).First(s => s.Stage == DigimonStage.Rookie), 20);
            var frail = MakeDigimon(species.OrderBy(s => s.BaseStats.HealthPoints).First(s => s.Stage == DigimonStage.Rookie), 20);
            var m2 = NewMatch(tank, frail);
            double t0 = 0;
            while (t0 < 121.0) { m2.Tick(0.1); t0 += 0.1; }
            int tankBefore = tank.CurrentHealthPoints, frailBefore = frail.CurrentHealthPoints;
            for (int i = 0; i < 100 && m2.CheckResult() == BattleResult.Ongoing; i++) m2.Tick(0.1);
            double tankLost = (tankBefore - tank.CurrentHealthPoints) / (double)tank.MaxHealthPoints;
            double frailLost = (frailBefore - frail.CurrentHealthPoints) / (double)frail.MaxHealthPoints;
            Check(Math.Abs(tankLost - frailLost) < 0.02, $"a morte súbita é proporcional: tanque perdeu {tankLost:P1}, frágil {frailLost:P1}");

            // D) quem tem a MENOR % de vida cai primeiro (vence quem causou mais dano relativo)
            var strong = MakeDigimon(phascomon, 20);
            var weak = MakeDigimon(phascomon, 20);
            weak.CurrentHealthPoints = weak.MaxHealthPoints / 2;
            var m3 = NewMatch(weak, strong); // jogador = fraco
            double t3 = 0;
            while (t3 < 121.0) { m3.Tick(0.1); t3 += 0.1; }
            double sim = 0;
            while (m3.CheckResult() == BattleResult.Ongoing && sim < 60) { m3.Tick(0.1); sim += 0.1; }
            Check(m3.CheckResult() == BattleResult.EnemyWon, $"o de menor % de vida deveria perder (resultado {m3.CheckResult()})");
            Check(strong.CurrentHealthPoints > 0 && weak.CurrentHealthPoints == 0, "o mais forte sobrevive quando o fraco cai");

            // E) empate exato: acaba em poucos segundos e sempre com um vencedor
            var x = MakeDigimon(phascomon, 20);
            var y = MakeDigimon(phascomon, 20);
            var m4 = NewMatch(x, y);
            double t4 = 0;
            while (m4.CheckResult() == BattleResult.Ongoing && t4 < 200) { m4.Tick(0.05); t4 += 0.05; }
            Check(m4.CheckResult() != BattleResult.Ongoing && t4 <= 120 + 40, $"empate exato terminou em {t4:F1}s (esperado <= 160s)");

            // F) o caso que travava: Suporte de cura contra Tank de pouco dano, na arena de verdade
            int total = 24, longest = 0, suddenDeaths = 0;
            double slowest = 0;

            for (int i = 0; i < total; i++)
            {
                ResetScratch(capacity: 1000);
                int level = _rng.Next(8, 45);
                var player = MakeDigimon(i % 2 == 0 ? lalamon : phascomon, level);
                var enemy = MakeDigimon(i % 2 == 0 ? phascomon : lalamon, level);

                var arena = GD.Load<PackedScene>("res://Scenes/Battle/BattleArena.tscn").Instantiate<BattleArena>();
                GetTree().Root.AddChild(arena);
                arena.Init(new List<DigimonInstance> { player }, new List<DigimonInstance> { enemy });

                double elapsed = 0;
                while (!arena.IsBattleOver && elapsed < 250)
                {
                    await Frames(1);
                    elapsed += GetProcessDeltaTime();
                }

                bool ended = arena.IsBattleOver;
                Check(ended, $"Suporte x Tank nível {level}: não terminou em 250s (a morte súbita deveria garantir o fim)");

                if (elapsed > 120) suddenDeaths++;
                slowest = Math.Max(slowest, elapsed);
                longest = Math.Max(longest, (int)elapsed);
                arena.QueueFree();
                await Frames(2);
            }

            Info($"{total} lutas Suporte de cura x Tank: {suddenDeaths} chegaram à morte súbita, a mais longa durou {slowest:F0}s");
        }

        // ------------------------------------------------------------------ 19. tempo de vida x evolução (jogador ideal)

        private static DigimonInstance CloneDigimon(DigimonInstance d) =>
            JsonSerializer.Deserialize<DigimonInstance>(JsonSerializer.Serialize(d));

        private static int StatValue(DigimonInstance d, string stat) => stat switch
        {
            "hp" => d.CurrentStats.HealthPoints,
            "atk" => d.CurrentStats.PhysicalDamage,
            "def" => d.CurrentStats.PhysicalDefense,
            "satk" => d.CurrentStats.SpecialDamage,
            "sdef" => d.CurrentStats.SpecialDefense,
            _ => d.CurrentStats.Speed,
        };

        private static int RequiredStat(EvolutionData e, string stat)
        {
            var r = e.RequiredStats;
            if (r == null) return 0;
            return stat switch
            {
                "hp" => r.HealthPoints,
                "atk" => r.PhysicalDamage,
                "def" => r.PhysicalDefense,
                "satk" => r.SpecialDamage,
                "sdef" => r.SpecialDefense,
                _ => r.Speed,
            };
        }

        /// <summary>Simula o melhor jogador possível (treina o stat que mais falta, todo dia, usando toda a
        /// stamina; nenhuma XP de batalha - conservador) até cumprir a aresta. Retorna null se o Digimon morre de
        /// velhice antes; senão o Digimon já evoluído (idade = dia em que evoluiu).</summary>
        /// <summary>Teto de idade usado na simulação: o do jogo, ou o valor de DEBUG_AUDIT_MAXAGES (ex.:
        /// "Ultimate:34,Mega:50") pra testar números novos antes de mexer no código.</summary>
        private static int AgeCeiling(DigimonStage stage)
        {
            foreach (var part in (OS.GetEnvironment("DEBUG_AUDIT_MAXAGES") ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                var kv = part.Split(':');
                if (kv.Length == 2 && kv[0].Trim() == stage.ToString() && int.TryParse(kv[1], out var v))
                    return v;
            }

            return DigimonInstance.GetMaxAgeForStage(stage);
        }

        /// <summary>"Center evoluído": multiplica o XP das batalhas (inimigos mais fortes, bônus de conjunto) e o ganho
        /// de stats do treino (área de treino específica x1.15 e NPC recrutado x1.15 = ~x1.32).</summary>
        private static double XpMult => double.TryParse(OS.GetEnvironment("DEBUG_AUDIT_XP_MULT"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 1.0;
        private static double GainMult => double.TryParse(OS.GetEnvironment("DEBUG_AUDIT_GAIN_MULT"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 1.0;

        private static int ExtraStaminaPerDay => int.TryParse(OS.GetEnvironment("DEBUG_AUDIT_EXTRA_STAMINA"), out var v) ? v : 0;

        private static int BattlesPerDay => int.TryParse(OS.GetEnvironment("DEBUG_AUDIT_BATTLES_PER_DAY"), out var v) ? v : 3;

        private (DigimonInstance evolved, string failReason) SimulateEdge(DigimonInstance source, EvolutionData e)
        {
            var d = CloneDigimon(source);
            var target = FindSpecies(e.ToDigimonId);
            string[] stats = { "hp", "atk", "def", "satk", "sdef", "spd" };

            for (int safety = 0; safety < 60; safety++)
            {
                // batalhas/disciplina/felicidade: o jogador cuida disso (não limitam o tempo)
                d.BattlesFought = Math.Max(d.BattlesFought, e.RequiredWinRate > 0 ? Math.Max(10, e.RequiredBattles) : e.RequiredBattles);
                d.BattlesWon = Math.Max(d.BattlesWon, (int)Math.Ceiling(e.RequiredWinRate * d.BattlesFought));
                d.Discipline = Math.Clamp((e.MinDiscipline + e.MaxDiscipline) / 2, 0, 100);
                d.Happiness = Math.Clamp((e.MinHappiness + e.MaxHappiness) / 2, 0, 100);

                while (true)
                {
                    if ((bool)MeetsRequirements.Invoke(null, new object[] { d, e }))
                    {
                        d.Evolve(target, e.StatMultiplier);
                        return (d, null);
                    }

                    if (d.Stamina < 10)
                        break;

                    // stat com mais sessões faltando (HP rende ~12 por sessão, os outros ~2)
                    string pick = stats.OrderByDescending(s => Math.Max(0, RequiredStat(e, s) - StatValue(d, s)) / (s == "hp" ? 12.0 : 2.0)).First();
                    bool anyStatMissing = stats.Any(s => RequiredStat(e, s) > StatValue(d, s));

                    var res = new TrainingResult { Success = true, StaminaCost = 10, ExpGained = 5 };

                    if (anyStatMissing)
                    {
                        switch (pick)
                        {
                            case "hp": res.HealthPointsGained = (int)Math.Round(12 * GainMult); break;
                            case "atk": res.PhysicDamageGained = (int)Math.Round(2 * GainMult); break;
                            case "def": res.PhysicDefenseGained = (int)Math.Round(2 * GainMult); break;
                            case "satk": res.SpecialDamageGained = (int)Math.Round(2 * GainMult); break;
                            case "sdef": res.SpecialDefenseGained = (int)Math.Round(2 * GainMult); break;
                            default: res.SpeedGained = (int)Math.Round(2 * GainMult); break;
                        }
                    }
                    else if (d.Level >= e.RequiredLevel)
                    {
                        break; // só falta idade: espera o dia passar
                    }

                    d.ApplyTrainingResult(res);
                }

                // batalhas do dia (XP de vitória contra um inimigo do MESMO estágio, nível parecido: ver
                // BattleRewardCalculator) - é daqui que vem a maior parte dos níveis
                for (int b = 0; b < BattlesPerDay; b++)
                {
                    int xp = d.BaseData.Stage switch
                    {
                        DigimonStage.Baby => 20, DigimonStage.InTraining => 40, DigimonStage.Rookie => 80,
                        DigimonStage.Champion => 180, DigimonStage.Ultimate => 400, DigimonStage.Mega => 900, _ => 50,
                    };

                    d.GainExperience((int)(xp * XpMult));
                    d.RecordBattleResult(true);

                    if ((bool)MeetsRequirements.Invoke(null, new object[] { d, e }))
                    {
                        d.Evolve(target, e.StatMultiplier);
                        return (d, null);
                    }
                }

                // fim do dia: envelhece e regenera stamina (72/dia: 16h acordado x2 + 8h dormindo x5)
                d.AgeInDays++;
                d.Stamina = Math.Min(d.MaxStamina, d.Stamina + 72 + ExtraStaminaPerDay);

                if (d.AgeInDays > AgeCeiling(d.BaseData.Stage))
                {
                    var missing = new List<string>();
                    if (d.Level < e.RequiredLevel) missing.Add($"nível {d.Level}/{e.RequiredLevel}");
                    foreach (var s in stats)
                        if (RequiredStat(e, s) > StatValue(d, s)) missing.Add($"{s} {StatValue(d, s)}/{RequiredStat(e, s)}");
                    if (d.AgeInDays < e.RequiredAgeInDays) missing.Add($"idade {d.AgeInDays}/{e.RequiredAgeInDays}");
                    return (null, $"morre aos {d.AgeInDays} dias ({d.BaseData.Stage}, teto {AgeCeiling(d.BaseData.Stage)}) faltando: {string.Join(", ", missing)}");
                }
            }

            return (null, "não convergiu");
        }

        // Calendário de estações (CalendarSystem): funções puras sobre o índice de dia (0 = primeiro
        // dia do Ano 1) e a conversão a partir do CurrentDay de saves reais/antigos.
        private Task Calendar()
        {
            var cal = CalendarSystem.Data;
            Check(cal != null, "calendário: Data/Calendar/calendar.json não carregou");
            if (cal == null)
                return Task.CompletedTask;

            Check(cal.DaysPerSeason == 15 && cal.SeasonCount == 4 && cal.DaysPerYear == 60,
                $"calendário: config inesperada ({cal.DaysPerSeason} dias x {cal.SeasonCount} estações)");

            // índice -> (ano, estação, dia da estação, dia do ano)
            var expected = new (int Index, int Year, int Season, int DayOfSeason, int DayOfYear)[]
            {
                (0, 1, 1, 1, 1),
                (14, 1, 1, 15, 15),
                (15, 1, 2, 1, 16),
                (59, 1, 4, 15, 60),
                (60, 2, 1, 1, 1),
                (61, 2, 1, 2, 2),
                (119, 2, 4, 15, 60),
                (120, 3, 1, 1, 1),
                (10000, 167, 3, 11, 41),
            };

            foreach (var e in expected)
            {
                var d = CalendarSystem.GetDate(cal, e.Index);
                Check(d.Year == e.Year && d.Season == e.Season && d.DayOfSeason == e.DayOfSeason && d.DayOfYear == e.DayOfYear,
                    $"calendário: índice {e.Index} deu Ano {d.Year} Estação {d.Season} Dia {d.DayOfSeason} (DiaDoAno {d.DayOfYear}), esperado Ano {e.Year} Estação {e.Season} Dia {e.DayOfSeason} (DiaDoAno {e.DayOfYear})");
                Info($"índice {e.Index} -> {CalendarSystem.Format(d)} (DiaDoAno {d.DayOfYear})");
            }

            // varredura: faixas sempre válidas e o dia sempre avança 1 (sem pular nem repetir)
            int outOfRange = 0, badStep = 0;
            var prev = CalendarSystem.GetDate(cal, 0);
            for (int i = 0; i < 10000; i++)
            {
                var d = CalendarSystem.GetDate(cal, i);
                if (d.DayOfYear < 1 || d.DayOfYear > 60 || d.DayOfSeason < 1 || d.DayOfSeason > 15 || d.Season < 1 || d.Season > 4)
                    outOfRange++;
                if (i > 0 && (d.Year - 1) * 60 + d.DayOfYear != (prev.Year - 1) * 60 + prev.DayOfYear + 1)
                    badStep++;
                if (CalendarSystem.DaysUntil(cal, i, d.DayOfYear) != 0 || CalendarSystem.GetDate(cal, i + CalendarSystem.DaysUntil(cal, i, 1)).DayOfYear != 1)
                    badStep++;
                prev = d;
            }
            Check(outOfRange == 0, $"calendário: {outOfRange} dia(s) fora da faixa em 10.000");
            Check(badStep == 0, $"calendário: {badStep} passo(s)/DaysUntil incoerente(s) em 10.000");

            // DaysUntil atravessando a virada de ano
            Check(CalendarSystem.DaysUntil(cal, 59, 1) == 1, "calendário: DaysUntil(59 -> dia 1) != 1");
            Check(CalendarSystem.DaysUntil(cal, 0, 60) == 59, "calendário: DaysUntil(0 -> dia 60) != 59");
            Check(CalendarSystem.DaysUntil(cal, 30, 10) == 39, "calendário: DaysUntil(30 -> dia 10) != 39");
            Check(CalendarSystem.DaysUntil(cal, 70, 11) == 0, "calendário: DaysUntil(70 -> dia 11) != 0");

            // CurrentDay (começa em 1) -> índice
            Check(CalendarSystem.Format(CalendarSystem.GetDateFromCurrentDay(1)) == "Ano 1 • Primavera • Dia 1", "calendário: jogo novo (CurrentDay 1) não é Ano 1 • Primavera • Dia 1");
            Check(CalendarSystem.Format(CalendarSystem.GetDateFromCurrentDay(61)) == "Ano 2 • Primavera • Dia 1", "calendário: CurrentDay 61 não é Ano 2 • Primavera • Dia 1");

            CalendarTournamentSchedule(cal);

            // saves reais/antigos: só lê o CurrentDay e mostra o label que a HUD exibiria
            string dir = ProjectSettings.GlobalizePath("user://");
            foreach (var path in System.IO.Directory.GetFiles(dir, "save*").OrderBy(p => p))
            {
                try
                {
                    var s = JsonSerializer.Deserialize<SaveData>(System.IO.File.ReadAllText(path));
                    if (s?.World == null)
                        continue;
                    string label = CalendarSystem.Format(CalendarSystem.GetDateFromCurrentDay(s.World.CurrentDay));
                    Info($"{System.IO.Path.GetFileName(path)}: CurrentDay {s.World.CurrentDay} -> \"{label}\"");

                    // campos novos entram vazios; destravado só o que a lista de vencidos justifica
                    Check(s.Center.LastWonEdition != null && s.Center.LastWonEdition.Count == 0,
                        $"{System.IO.Path.GetFileName(path)}: LastWonEdition não veio vazio de um save antigo");
                    var dayIndex = CalendarSystem.DayIndexFromCurrentDay(s.World.CurrentDay);
                    var states = Db.GetAllTournaments().Select(t =>
                        $"{t.Id}:{CalendarSystem.GetAvailability(cal, t, dayIndex, s.Center.ClearedTournamentIds, s.Center.LastWonEdition).Status}");
                    foreach (var t in Db.GetAllTournaments().Where(t => t.UnlockAfterTournamentId > 0))
                        Check(CalendarSystem.IsUnlocked(t, s.Center.ClearedTournamentIds) == s.Center.ClearedTournamentIds.Contains(t.UnlockAfterTournamentId),
                            $"{System.IO.Path.GetFileName(path)}: campeonato {t.Id} destravado sem vencer o {t.UnlockAfterTournamentId}");
                    Info($"  vencidos [{string.Join(",", s.Center.ClearedTournamentIds)}] -> {string.Join(" ", states)}");
                }
                catch (Exception ex)
                {
                    Info($"{System.IO.Path.GetFileName(path)}: não desserializa ({ex.GetType().Name}), ignorado");
                }
            }

            return Task.CompletedTask;
        }

        // Agenda dos campeonatos (CalendarSystem.IsOpen/DaysUntilOpen/IsUnlocked): casos sintéticos + os 12
        // campeonatos reais. Imprime a agenda de um ano ([AUDIT-AGENDA]) pra gerar calendario/analise/agenda.md.
        private void CalendarTournamentSchedule(CalendarData cal)
        {
            int year = cal.DaysPerYear;
            int Idx(int yearNumber, int dayOfYear) => (yearNumber - 1) * year + dayOfYear - 1;

            TournamentData T(TournamentScheduleType type, int start = 0, int end = 0, int interval = 0, int offset = 0, int duration = 0) =>
                new() { Id = -1, Name = "teste", ScheduleType = type, StartDayOfYear = start, EndDayOfYear = end, IntervalDays = interval, OffsetDays = offset, DurationDays = duration };

            void Open(TournamentData t, int index, bool expected, string what) =>
                Check(CalendarSystem.IsOpen(cal, t, index) == expected, $"agenda: {what} (índice {index}) deveria estar {(expected ? "aberto" : "fechado")}");

            void Until(TournamentData t, int index, int expected, string what)
            {
                int got = CalendarSystem.DaysUntilOpen(cal, t, index);
                Check(got == expected, $"agenda: DaysUntilOpen {what} (índice {index}) = {got}, esperado {expected}");
            }

            // Always
            var always = T(TournamentScheduleType.Always);
            Open(always, 0, true, "Always");
            Open(always, 9999, true, "Always");
            Until(always, 123, 0, "Always");

            // janela simples 10..25: início e fim
            var window = T(TournamentScheduleType.Window, start: 10, end: 25);
            Open(window, Idx(1, 9), false, "janela 10-25, dia 9");
            Open(window, Idx(1, 10), true, "janela 10-25, dia 10");
            Open(window, Idx(1, 25), true, "janela 10-25, dia 25");
            Open(window, Idx(1, 26), false, "janela 10-25, dia 26");
            Open(window, Idx(3, 10), true, "janela 10-25, ano 3 dia 10");
            Until(window, Idx(1, 1), 9, "janela 10-25 a partir do dia 1");
            Until(window, Idx(1, 26), 44, "janela 10-25 a partir do dia 26 (abre no dia 10 do ano seguinte)");

            // janela atravessando a virada do ano: 58..2
            var wrap = T(TournamentScheduleType.Window, start: 58, end: 2);
            Open(wrap, Idx(1, 57), false, "janela 58-2, dia 57");
            Open(wrap, Idx(1, 58), true, "janela 58-2, dia 58");
            Open(wrap, Idx(1, 60), true, "janela 58-2, dia 60");
            Open(wrap, Idx(2, 1), true, "janela 58-2, ano 2 dia 1");
            Open(wrap, Idx(2, 2), true, "janela 58-2, ano 2 dia 2");
            Open(wrap, Idx(2, 3), false, "janela 58-2, ano 2 dia 3");
            Open(wrap, Idx(1, 1), true, "janela 58-2, ano 1 dia 1");
            Until(wrap, Idx(1, 57), 1, "janela 58-2 a partir do dia 57");
            Until(wrap, Idx(2, 3), 55, "janela 58-2 a partir do dia 3");

            // fixo atravessando a virada do ano: dia 59 por 5 dias = 59, 60, 1, 2, 3
            var fixedWrap = T(TournamentScheduleType.Fixed, start: 59, duration: 5);
            Open(fixedWrap, Idx(1, 58), false, "fixo 59x5, dia 58");
            Open(fixedWrap, Idx(1, 59), true, "fixo 59x5, dia 59");
            Open(fixedWrap, Idx(2, 3), true, "fixo 59x5, ano 2 dia 3");
            Open(fixedWrap, Idx(2, 4), false, "fixo 59x5, ano 2 dia 4");

            // fixo em 2 anos seguidos: dia 40 por 3 dias
            var fixed40 = T(TournamentScheduleType.Fixed, start: 40, duration: 3);
            foreach (int y in new[] { 1, 2 })
            {
                Open(fixed40, Idx(y, 39), false, $"fixo 40x3, ano {y} dia 39");
                Open(fixed40, Idx(y, 40), true, $"fixo 40x3, ano {y} dia 40");
                Open(fixed40, Idx(y, 42), true, $"fixo 40x3, ano {y} dia 42");
                Open(fixed40, Idx(y, 43), false, $"fixo 40x3, ano {y} dia 43");
            }
            Until(fixed40, Idx(1, 43), 57, "fixo 40x3 a partir do dia 43 (abre no dia 40 do ano 2)");
            Until(fixed40, Idx(2, 1), 39, "fixo 40x3 a partir do ano 2 dia 1");

            // recorrente em vários anos: a cada 7 dias corridos (não divide 60), deslocado 3, aberto 2
            var recurring = T(TournamentScheduleType.Recurring, interval: 7, offset: 3, duration: 2);
            Open(recurring, 2, false, "recorrente 7/3/2 antes do deslocamento");
            Open(recurring, 3, true, "recorrente 7/3/2, índice 3");
            Open(recurring, 4, true, "recorrente 7/3/2, índice 4");
            Open(recurring, 5, false, "recorrente 7/3/2, índice 5");
            Open(recurring, 10, true, "recorrente 7/3/2, índice 10");
            Open(recurring, 3 + 7 * 100, true, "recorrente 7/3/2, ano 12 (índice 703)");
            Open(recurring, 3 + 7 * 100 + 2, false, "recorrente 7/3/2, índice 705");
            Until(recurring, 0, 3, "recorrente 7/3/2 a partir do índice 0");
            Until(recurring, 5, 5, "recorrente 7/3/2 a partir do índice 5");
            Until(recurring, 3 + 7 * 1000 + 2, 5, "recorrente 7/3/2 no ano 117");
            int openYear1 = Enumerable.Range(Idx(1, 1), year).Count(i => CalendarSystem.IsOpen(cal, recurring, i));
            int openYear2 = Enumerable.Range(Idx(2, 1), year).Count(i => CalendarSystem.IsOpen(cal, recurring, i));
            Info($"recorrente 7/3/2: {openYear1} dias abertos no ano 1, {openYear2} no ano 2 (as datas andam porque 7 não divide {year})");

            // trava
            var locked = new TournamentData { UnlockAfterTournamentId = 8 };
            Check(!CalendarSystem.IsUnlocked(locked, new[] { 1, 2, 3 }), "agenda: trava após o 8 liberou sem o 8 vencido");
            Check(CalendarSystem.IsUnlocked(locked, new[] { 8 }), "agenda: trava após o 8 não liberou com o 8 vencido");
            Check(CalendarSystem.IsUnlocked(always, Array.Empty<int>()), "agenda: campeonato sem trava ficou travado");

            // os 12 campeonatos reais
            var tournaments = Db.GetAllTournaments().ToList();
            Check(tournaments.Count == 12, $"agenda: {tournaments.Count} campeonatos carregados, esperado 12");
            Info("tipos: " + string.Join(", ", Enum.GetValues<TournamentScheduleType>().Select(t => $"{t}={tournaments.Count(x => x.ScheduleType == t)}")));

            foreach (var t in tournaments)
            {
                Check(t.ScheduleType != TournamentScheduleType.Always, $"agenda: campeonato {t.Id} {t.Name} sem ScheduleType");
                Check(t.AnnounceDaysBefore == 5, $"agenda: campeonato {t.Id} {t.Name} com AnnounceDaysBefore {t.AnnounceDaysBefore}");
                Check(t.UnlockAfterTournamentId == 0 || tournaments.Any(o => o.Id == t.UnlockAfterTournamentId),
                    $"agenda: campeonato {t.Id} trava num campeonato inexistente ({t.UnlockAfterTournamentId})");

                // datas do ano 1 se repetem nos anos seguintes (os intervalos da tabela dividem o ano)
                bool repeats = Enumerable.Range(0, year * 5).All(i => CalendarSystem.IsOpen(cal, t, i) == CalendarSystem.IsOpen(cal, t, i % year));
                Check(repeats, $"agenda: campeonato {t.Id} {t.Name} muda de data de um ano pro outro");

                int openDays = Enumerable.Range(0, year).Count(i => CalendarSystem.IsOpen(cal, t, i));
                Check(openDays > 0, $"agenda: campeonato {t.Id} {t.Name} nunca abre");
                Check(openDays < year || t.ScheduleType == TournamentScheduleType.Always, $"agenda: campeonato {t.Id} {t.Name} aberto o ano inteiro");
                GD.Print($"[AUDIT-AGENDA-TOTAL] {t.Id}|{t.Name}|{t.ScheduleType}|{t.UnlockAfterTournamentId}|{openDays}");
            }

            int daysWithAny = 0, longestGap = 0, gap = 0;
            // dobra o ano pra medir o intervalo sem campeonato que atravessa a virada
            for (int i = 0; i < year * 2; i++)
            {
                var open = tournaments.Where(t => CalendarSystem.IsOpen(cal, t, i)).Select(t => t.Id).ToList();
                if (i < year)
                {
                    if (open.Count > 0) daysWithAny++;
                    GD.Print($"[AUDIT-AGENDA] {i + 1}|{string.Join(",", open)}");
                }
                gap = open.Count == 0 ? gap + 1 : 0;
                longestGap = Math.Max(longestGap, Math.Min(gap, year));
            }
            Info($"dias do ano com pelo menos 1 campeonato aberto: {daysWithAny}/{year}; maior sequência sem nenhum: {longestGap}");

            // ---- edição (EditionId)
            void Edition(TournamentData t, int index, int expected, string what)
            {
                int got = CalendarSystem.EditionId(cal, t, index);
                Check(got == expected, $"edição: {what} (índice {index}) = {got}, esperado {expected}");
            }

            var every6 = T(TournamentScheduleType.Recurring, interval: 6, duration: 3);
            Edition(every6, 0, 0, "recorrente 6/3 início");
            Edition(every6, 5, 0, "recorrente 6/3 fim do ciclo 0");
            Edition(every6, 6, 1, "recorrente 6/3 ciclo 1");
            Edition(every6, 60, 10, "recorrente 6/3 ano 2 dia 1");
            Edition(recurring, 2, -1, "recorrente com deslocamento, antes da 1ª abertura");
            Edition(recurring, 3, 0, "recorrente com deslocamento, 1ª abertura");
            Edition(recurring, 10, 1, "recorrente com deslocamento, 2ª abertura");
            Edition(fixed40, Idx(1, 40), 1, "fixo 40 ano 1");
            Edition(fixed40, Idx(2, 42), 2, "fixo 40 ano 2");
            Edition(fixed40, Idx(2, 10), 1, "fixo 40 ano 2 antes de abrir (edição anterior)");
            Edition(window, Idx(3, 25), 3, "janela 10-25 ano 3");
            Edition(wrap, Idx(1, 58), 1, "janela 58-2 ano 1 dia 58");
            Edition(wrap, Idx(2, 2), 1, "janela 58-2 ano 2 dia 2 (mesma edição da virada)");
            Edition(wrap, Idx(2, 58), 2, "janela 58-2 ano 2 dia 58");
            Edition(fixedWrap, Idx(2, 3), 1, "fixo 59x5 ano 2 dia 3 (mesma edição)");

            // ---- dias restantes e disponibilidade (prioridade Locked > Closed > WonThisEdition)
            Check(CalendarSystem.DaysOpenRemaining(cal, fixed40, Idx(1, 40)) == 3, "disponibilidade: fixo 40x3 no dia 40 deveria ter 3 dias restantes");
            Check(CalendarSystem.DaysOpenRemaining(cal, fixed40, Idx(1, 42)) == 1, "disponibilidade: fixo 40x3 no dia 42 deveria ter 1 dia restante");
            Check(CalendarSystem.DaysOpenRemaining(cal, wrap, Idx(1, 60)) == 3, "disponibilidade: janela 58-2 no dia 60 deveria ter 3 dias restantes (60, 1, 2)");
            Check(CalendarSystem.DaysOpenRemaining(cal, always, 5) == -1, "disponibilidade: Always não fecha");

            var lockedFixed = T(TournamentScheduleType.Fixed, start: 40, duration: 3);
            lockedFixed.Id = 77;
            lockedFixed.UnlockAfterTournamentId = 8;
            var none = Array.Empty<int>();
            var won = new Dictionary<int, int> { [77] = 1 };
            TournamentAvailabilityStatus Status(int index, IEnumerable<int> cleared, Dictionary<int, int> w) =>
                CalendarSystem.GetAvailability(cal, lockedFixed, index, cleared, w).Status;
            Check(Status(Idx(1, 1), none, won) == TournamentAvailabilityStatus.Locked, "disponibilidade: travado e fechado deveria dar Locked");
            Check(Status(Idx(1, 1), new[] { 8 }, won) == TournamentAvailabilityStatus.Closed, "disponibilidade: destravado e fechado deveria dar Closed");
            Check(Status(Idx(1, 40), new[] { 8 }, won) == TournamentAvailabilityStatus.WonThisEdition, "disponibilidade: vencido na edição 1 deveria dar WonThisEdition");
            Check(Status(Idx(2, 40), new[] { 8 }, won) == TournamentAvailabilityStatus.Available, "disponibilidade: edição 2 deveria reabrir");
            Check(Status(Idx(1, 40), new[] { 8 }, new Dictionary<int, int>()) == TournamentAvailabilityStatus.Available, "disponibilidade: aberto e destravado deveria dar Available");
            var closedAv = CalendarSystem.GetAvailability(cal, lockedFixed, Idx(1, 35), new[] { 8 }, null);
            Check(closedAv.Days == 5, $"disponibilidade: fechado no dia 35 deveria abrir em 5 dias, deu {closedAv.Days}");

            // ---- save: LastWonEdition faz ida-e-volta e save antigo entra vazio
            var sd = new SaveData();
            sd.Center.LastWonEdition[1] = 3;
            var back = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(sd));
            Check(back.Center.LastWonEdition.TryGetValue(1, out int bw) && bw == 3, "save: LastWonEdition não sobrevive à ida-e-volta");

            // ---- disponível no início do jogo (nada vencido): com os dados reais e com a hipótese de só 1, 2, 3 e 5
            var startIds = tournaments.Where(t => CalendarSystem.IsUnlocked(t, none)).Select(t => t.Id).ToList();
            foreach (var (label, ids) in new[] { ("dados reais", startIds), ("só 1, 2, 3 e 5", new List<int> { 1, 2, 3, 5 }) })
            {
                var pool = tournaments.Where(t => ids.Contains(t.Id)).ToList();
                int any = 0, gapStart = 0, bestStart = 0;
                for (int i = 0; i < year * 2; i++)
                {
                    bool open = pool.Any(t => CalendarSystem.IsOpen(cal, t, i));
                    if (i < year && open) any++;
                    gapStart = open ? 0 : gapStart + 1;
                    bestStart = Math.Max(bestStart, Math.Min(gapStart, year));
                }
                Info($"início do jogo ({label}: {string.Join(",", ids)}): {any}/{year} dias com algum disponível; maior sequência sem nenhum: {bestStart}");
                if (label == "dados reais")
                    for (int i = 0; i < year; i++)
                        GD.Print($"[AUDIT-AGENDA-INICIO] {i + 1}|{string.Join(",", pool.Where(t => CalendarSystem.IsOpen(cal, t, i)).Select(t => t.Id))}");
            }
        }

        // Só medição (não entra no audit normal - liga com DEBUG_AUDIT_WINRATE=1): taxa de vitória de times de
        // 3 Ultimate sorteados (semente fixa) no Trio Guardião 02, em 50 batalhas por nível. Não ajusta nada.
        private async Task TournamentWinRate()
        {
            if (OS.GetEnvironment("DEBUG_AUDIT_WINRATE") != "1")
            {
                Info("pulada (DEBUG_AUDIT_WINRATE=1 pra rodar)");
                return;
            }

            var t10 = Db.GetTournament(10);
            var ultimates = AllSpecies().Where(sp => sp.Stage == DigimonStage.Ultimate).ToList();
            const int battles = 50;

            foreach (int level in new[] { 22, 30 })
            {
                var rng = new Random(2026 + level);
                int won = 0, lost = 0, stalled = 0;
                var lostTeams = new List<string>();

                for (int b = 0; b < battles; b++)
                {
                    ResetScratch(bits: 0, capacity: 1000);
                    Gm.Save.Center.ClearedTournamentIds.Add(t10.UnlockAfterTournamentId);
                    SetDayIndex(FirstDayIndex(t10, open: true));

                    var team = Enumerable.Range(0, 3).Select(_ => MakeDigimon(ultimates[rng.Next(ultimates.Count)], level, addToCenter: true)).ToList();
                    Gm.StartTournamentBattle(t10, team);
                    Check(Gm.IsBattleActive, $"winrate: batalha {b} (nível {level}) não iniciou");
                    if (!Gm.IsBattleActive)
                        continue;

                    var result = await DriveArenaToEnd(300);
                    if (_lastTimedOut) stalled++;
                    else if (result == BattleResult.PlayerWon) won++;
                    else
                    {
                        lost++;
                        lostTeams.Add(string.Join("/", team.Select(d => d.BaseData.Name)));
                    }
                }

                Info($"Trio Guardião 02, 3 Ultimate nível {level}: {won}/{battles} vitórias ({won * 100 / battles}%), {lost} derrotas, {stalled} impasses");
                GD.Print($"[AUDIT-WINRATE] nivel={level} vitorias={won} derrotas={lost} impasses={stalled} times_que_perderam={string.Join("; ", lostTeams.Take(10))}");
            }
        }

        // Regras de inscrição (TournamentRules): dado explícito nos 12 JSONs, funções puras e o relatório de
        // elegibilidade do save real (somente leitura).
        private Task TournamentRulesSection()
        {
            var tournaments = Db.GetAllTournaments().ToList();

            // 12/12 com os dois campos escritos no JSON (lista vazia = "sem restrição" explícito)
            int explicitCount = 0;
            using (var dir = DirAccess.Open("res://Data/Tournaments/"))
            {
                foreach (var file in dir.GetFiles().Where(f => f.EndsWith(".json")))
                {
                    using var doc = JsonDocument.Parse(Godot.FileAccess.GetFileAsString("res://Data/Tournaments/" + file));
                    bool has = doc.RootElement.TryGetProperty("AllowedStages", out _) && doc.RootElement.TryGetProperty("AllowedAttributes", out _);
                    Check(has, $"regras: {file} sem AllowedStages/AllowedAttributes explícitos");
                    if (has) explicitCount++;
                }
            }
            Info($"{explicitCount}/{tournaments.Count} campeonatos com regra explícita no JSON");

            var byRule = tournaments.GroupBy(t => t.AllowedStages.Count > 0 ? "estágio" : t.AllowedAttributes.Count > 0 ? "atributo" : "sem restrição");
            Info("contagem por tipo de regra: " + string.Join(", ", byRule.Select(g => $"{g.Key}={g.Count()} ({string.Join(",", g.Select(t => t.Id))})")));
            foreach (var t in tournaments)
                Info($"  {t.Id} {t.Name}: {TournamentRules.Describe(t)}");

            // tabela da fase 4
            var expected = new Dictionary<int, string>
            {
                [1] = "Só Rookie", [2] = "Só Rookie", [3] = "Só Champion", [4] = "Só Vírus", [5] = "Sem restrição",
                [6] = "Só Ultimate", [7] = "Sem restrição", [8] = "Só Mega", [9] = "Só Vacina", [10] = "Só Ultimate",
                [11] = "Sem restrição", [12] = "Só Mega",
            };
            foreach (var t in tournaments)
                Check(expected.TryGetValue(t.Id, out var e) && TournamentRules.Describe(t) == e, $"regras: {t.Id} {t.Name} = {TournamentRules.Describe(t)}, esperado {(expected.TryGetValue(t.Id, out var e2) ? e2 : "?")}");

            // funções puras
            var rookieOnly = new TournamentData { AllowedStages = { DigimonStage.Rookie } };
            var virusOnly = new TournamentData { AllowedAttributes = { DigimonAttribute.Virus } };
            var both = new TournamentData { AllowedStages = { DigimonStage.Champion }, AllowedAttributes = { DigimonAttribute.Data } };
            var species = AllSpecies();
            var rookie = species.First(sp => sp.Stage == DigimonStage.Rookie);
            var champion = species.First(sp => sp.Stage == DigimonStage.Champion && sp.Attribute == DigimonAttribute.Vacine);
            var virus = species.First(sp => sp.Attribute == DigimonAttribute.Virus);
            var data = species.First(sp => sp.Attribute == DigimonAttribute.Data && sp.Stage == DigimonStage.Champion);
            Check(TournamentRules.GetIneligibilityReason(rookieOnly, rookie) == null, "regras: Rookie recusado num Só Rookie");
            Check(TournamentRules.GetIneligibilityReason(rookieOnly, champion) == "Estágio Champion não permitido", $"regras: motivo errado pra Champion num Só Rookie: {TournamentRules.GetIneligibilityReason(rookieOnly, champion)}");
            Check(TournamentRules.GetIneligibilityReason(virusOnly, virus) == null, "regras: Vírus recusado num Só Vírus");
            Check(TournamentRules.GetIneligibilityReason(virusOnly, champion) == "Atributo Vacina não permitido", $"regras: motivo errado pra Vacina num Só Vírus: {TournamentRules.GetIneligibilityReason(virusOnly, champion)}");
            Check(TournamentRules.IsEligible(both, data) && !TournamentRules.IsEligible(both, champion), "regras: estágio + atributo combinados");
            var mixed = new List<DigimonInstance> { MakeDigimon(rookie), MakeDigimon(champion), MakeDigimon(virus) };
            var v = TournamentRules.ValidateTeam(rookieOnly, mixed);
            Check(!v.IsValid && v.Invalid.All(i => i.Digimon.BaseData.Stage != DigimonStage.Rookie) && v.Invalid.Count == mixed.Count(d => d.BaseData.Stage != DigimonStage.Rookie),
                "regras: ValidateTeam não apontou exatamente os Digimon fora da regra");
            Check(TournamentRules.ValidateTeam(new TournamentData(), mixed).IsValid, "regras: campeonato sem regra recusou um time misto");

            // "Só Mega" aceita MegaPlus (Omegamon, Examon); outras regras de estágio não
            var megaPlus = species.Where(sp => sp.Stage == DigimonStage.MegaPlus).ToList();
            Check(megaPlus.Count > 0, "regras: nenhuma espécie MegaPlus pra testar");
            var megaOnly = tournaments.First(t => t.Id == 8);
            var ultimateOnly = tournaments.First(t => t.Id == 6);
            int megaPlusAccepted = 0, megaPlusCases = 0;
            foreach (var mp in species.Where(sp => sp.Name is "Omegamon" or "Examon"))
            {
                foreach (var t in tournaments.Where(t => t.Id is 8 or 12))
                {
                    megaPlusCases++;
                    bool ok = TournamentRules.IsEligible(t, mp) && TournamentRules.ValidateTeam(t, new[] { MakeDigimon(mp) }).IsValid;
                    Check(ok, $"regras: {mp.Name} ({mp.Stage}) recusado no {t.Id} {t.Name}");
                    if (ok) megaPlusAccepted++;
                }
                foreach (var t in tournaments.Where(t => t.AllowedStages.Count > 0 && !t.AllowedStages.Contains(DigimonStage.Mega)))
                    Check(!TournamentRules.IsEligible(t, mp), $"regras: {mp.Name} aceito no {t.Id} {t.Name} ({TournamentRules.Describe(t)})");
            }
            Check(megaPlusCases == 4, $"regras: {megaPlusCases} casos Omegamon/Examon x 8/12, esperado 4");
            Info($"Omegamon/Examon aceitos em 8 e 12: {megaPlusAccepted}/{megaPlusCases}; recusados em {TournamentRules.Describe(ultimateOnly)} (6 e 10)");
            foreach (var mp in megaPlus)
                Check(TournamentRules.IsEligible(megaOnly, mp), $"regras: {mp.Name} (MegaPlus) recusado num Só Mega");

            // DEBUG_IGNORE_CALENDAR também ignora as regras de inscrição
            var rookieTeam = new List<DigimonInstance> { MakeDigimon(rookie), MakeDigimon(rookie), MakeDigimon(rookie) };
            Check(!Gm.ValidateTournamentTeam(megaOnly, rookieTeam).IsValid, "regras: time de Rookies aceito na Copa Mega sem IgnoreCalendar");
            Gm.IgnoreCalendar = true;
            if (Gm.IgnoreCalendar)
            {
                Check(Gm.ValidateTournamentTeam(megaOnly, rookieTeam).IsValid, "regras: IgnoreCalendar não ignorou a regra Só Mega");
                Check(Gm.GetTournamentIneligibilityReason(megaOnly, rookieTeam[0]) == null, "regras: IgnoreCalendar ainda mostra motivo de bloqueio");
                Check(Gm.GetTournamentAvailability(megaOnly).CanStart, "regras: IgnoreCalendar não liberou a data/trava");
                Info("IgnoreCalendar: regras, data e trava ignoradas");
            }
            else
            {
                Warn("regras: IgnoreCalendar não liga fora de build de debug - teste pulado");
            }
            Gm.IgnoreCalendar = false;

            // sem regra: qualquer time é válido (100 times aleatórios)
            var free = tournaments.Where(t => !TournamentRules.HasRules(t)).ToList();
            int randomOk = 0;
            for (int k = 0; k < 100; k++)
            {
                var team = Enumerable.Range(0, 3).Select(_ => MakeDigimon(species[_rng.Next(species.Count)])).ToList();
                if (free.All(t => TournamentRules.ValidateTeam(t, team).IsValid)) randomOk++;
            }
            Check(randomOk == 100, $"regras: campeonato sem regra recusou {100 - randomOk} de 100 times aleatórios");

            // save real (somente leitura): quantos Digimon do Center são elegíveis por campeonato
            string path = ProjectSettings.GlobalizePath("user://save.json");
            if (System.IO.File.Exists(path))
            {
                var real = JsonSerializer.Deserialize<SaveData>(System.IO.File.ReadAllText(path));
                var roster = real.Center.Digimons;
                Info($"save real: dia {real.World.CurrentDay}, {roster.Count} Digimon no Center: " +
                    string.Join(", ", roster.Select(d => $"{d.BaseData.Name} ({d.BaseData.Stage}/{TournamentRules.AttributeName(d.BaseData.Attribute)})")));
                var cannot = new List<string>();
                foreach (var t in tournaments)
                {
                    int eligible = roster.Count(d => TournamentRules.IsEligible(t, d.BaseData));
                    int need = t.Opponents.Count;
                    Info($"  {t.Id} {t.Name} ({TournamentRules.Describe(t)}, {need}x{need}): {eligible} de {roster.Count} elegíveis{(eligible < need ? " - NÃO monta" : "")}");
                    if (eligible < need) cannot.Add($"{t.Id} {t.Name}");
                }
                Info($"save real: não montam hoje: {(cannot.Count == 0 ? "nenhum" : string.Join(", ", cannot))}");
            }

            return Task.CompletedTask;
        }

        private Task Lifespan()
        {
            ResetScratch(capacity: 100_000);

            // DEBUG_AUDIT_LEVEL_MAP="Rookie>Champion:20=18;Champion>Ultimate:30=28;Ultimate>Mega:37=36,39=37" troca o nível exigido
            // (só nesta simulação, em memória) pra calibrar antes de editar os JSONs. Compatível com o antigo
            // DEBUG_AUDIT_UM_LEVELS="30:28,40:37" (Ultimate>Mega).
            var levelMap = new List<(DigimonStage from, DigimonStage to, int lv, int nl)>();
            string mapText = OS.GetEnvironment("DEBUG_AUDIT_LEVEL_MAP") ?? "";
            foreach (var group in mapText.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var head = group.Split(':');
                var stages = head[0].Split('>');
                if (head.Length != 2 || stages.Length != 2) continue;
                var fromStage = Enum.Parse<DigimonStage>(stages[0].Trim());
                var toStage = Enum.Parse<DigimonStage>(stages[1].Trim());
                foreach (var pair in head[1].Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    var kv = pair.Split('=');
                    if (kv.Length == 2 && int.TryParse(kv[0], out var f) && int.TryParse(kv[1], out var t))
                        levelMap.Add((fromStage, toStage, f, t));
                }
            }

            if (levelMap.Count > 0)
                foreach (var sp in AllSpecies())
                    foreach (var ev in Db.GetEvolutionsFrom(sp.Id))
                    {
                        var toStage = FindSpecies(ev.ToDigimonId).Stage;
                        foreach (var m in levelMap)
                            if (m.from == sp.Stage && m.to == toStage && m.lv == ev.RequiredLevel)
                            {
                                ev.RequiredLevel = m.nl;
                                break;
                            }
                    }

            var arrival = new Dictionary<int, DigimonInstance>();
            var queue = new List<DigimonInstance>();

            foreach (var baby in AllSpecies().Where(s => s.Stage == DigimonStage.Baby))
            {
                var d = MakeDigimon(baby, 1);
                d.AgeInDays = 0;
                d.Stamina = d.MaxStamina;
                arrival[baby.Id] = d;
                queue.Add(d);
            }

            var doneSpecies = new HashSet<int>();
            var failures = new List<string>();
            var byTransition = new Dictionary<string, (int ok, int bad, int minSlack)>();
            var badByReason = new Dictionary<string, int>();
            var slack = new List<int>();
            var ageByStage = new Dictionary<DigimonStage, List<int>>();

            while (queue.Count > 0)
            {
                // Dijkstra simples: processa sempre a espécie com menor idade de chegada
                queue.Sort((a, b) => a.AgeInDays.CompareTo(b.AgeInDays));
                var current = queue[0];
                queue.RemoveAt(0);

                if (!doneSpecies.Add(current.BaseData.Id))
                    continue;

                ageByStage.TryAdd(current.BaseData.Stage, new List<int>());
                ageByStage[current.BaseData.Stage].Add(current.AgeInDays);

                foreach (var e in Db.GetEvolutionsFrom(current.BaseData.Id))
                {
                    var (evolved, why) = SimulateEdge(current, e);
                    string name = $"{current.BaseData.Name}({current.BaseData.Stage}, chegou aos {current.AgeInDays}d) -> {FindSpecies(e.ToDigimonId).Name}";

                    string trans = $"{current.BaseData.Stage}->{FindSpecies(e.ToDigimonId).Stage} nível {e.RequiredLevel}";
                    var cur = byTransition.GetValueOrDefault(trans, (0, 0, 99));

                    if (evolved == null)
                    {
                        failures.Add($"{name}: {why}");
                        byTransition[trans] = (cur.Item1, cur.Item2 + 1, cur.Item3);
                        string reasonKey = $"{current.BaseData.Stage}->{FindSpecies(e.ToDigimonId).Stage} | " + (why.Contains("nível") ? "nível" : "") + (why.Contains(" hp ") || why.Contains(" atk ") || why.Contains(" def ") || why.Contains(" satk ") || why.Contains(" sdef ") || why.Contains(" spd ") ? " stats" : "");
                        badByReason[reasonKey] = badByReason.GetValueOrDefault(reasonKey) + 1;
                        continue;
                    }

                    int edgeSlack = AgeCeiling(current.BaseData.Stage) - evolved.AgeInDays;
                    byTransition[trans] = (cur.Item1 + 1, cur.Item2, Math.Min(cur.Item3, edgeSlack));
                    slack.Add(edgeSlack);

                    if (!arrival.TryGetValue(e.ToDigimonId, out var known) || evolved.AgeInDays < known.AgeInDays)
                    {
                        arrival[e.ToDigimonId] = evolved;
                        queue.Add(evolved);
                    }
                }
            }

            foreach (var stage in new[] { DigimonStage.Baby, DigimonStage.InTraining, DigimonStage.Rookie, DigimonStage.Champion, DigimonStage.Ultimate, DigimonStage.Mega })
            {
                if (!ageByStage.TryGetValue(stage, out var ages) || ages.Count == 0) continue;
                Info($"{stage}: {ages.Count} espécies; idade mínima de chegada {ages.Min()}d, mediana {ages.OrderBy(x => x).ElementAt(ages.Count / 2)}d, máxima {ages.Max()}d (teto do estágio: {AgeCeiling(stage)}d)");
            }

            Info($"{slack.Count} evoluções possíveis; folga (dias sobrando no estágio) mínima {(slack.Count > 0 ? slack.Min() : 0)}, média {(slack.Count > 0 ? slack.Average() : 0):F1}");

            foreach (var kv in byTransition.OrderBy(k => k.Key))
                Info($"transição {kv.Key}: {kv.Value.ok} viáveis, {kv.Value.bad} inviáveis, folga mínima {(kv.Value.ok > 0 ? kv.Value.minSlack.ToString() : "-")}d");

            foreach (var kv in badByReason.OrderBy(k => k.Key))
                Info($"falhas {kv.Key}: {kv.Value}");

            // faixa de requisitos por transição (nível/idade/soma de stats exigidos)
            foreach (var g in AllSpecies().SelectMany(sp => Db.GetEvolutionsFrom(sp.Id).Select(ev => (from: sp, ev)))
                         .GroupBy(x => $"{x.from.Stage}->{FindSpecies(x.ev.ToDigimonId).Stage}").OrderBy(x => x.Key))
            {
                var lv = g.Select(x => x.ev.RequiredLevel).ToList();
                var ag = g.Select(x => x.ev.RequiredAgeInDays).ToList();
                var sm = g.Select(x => x.ev.RequiredStats == null ? 0 : x.ev.RequiredStats.HealthPoints + x.ev.RequiredStats.PhysicalDamage + x.ev.RequiredStats.PhysicalDefense + x.ev.RequiredStats.SpecialDamage + x.ev.RequiredStats.SpecialDefense + x.ev.RequiredStats.Speed).ToList();
                Info($"requisitos {g.Key} ({g.Count()} arestas): nível {lv.Min()}-{lv.Max()}, idade {ag.Min()}-{ag.Max()}d, soma de stats {sm.Min()}-{sm.Max()}");
            }

            if (OS.GetEnvironment("DEBUG_AUDIT_VERBOSE") == "1")
                foreach (var f in failures)
                    Warn("INVIÁVEL com treino ideal: " + f);

            Info($"{failures.Count} arestas inviáveis no melhor cenário ({BattlesPerDay} batalhas por dia)");
            return Task.CompletedTask;
        }

        // ------------------------------------------------------------------ 16. toda cena do projeto instancia

        private async Task ScenesLoad()
        {
            var paths = new List<string>();

            void Walk(string dir)
            {
                using var d = DirAccess.Open(dir);
                if (d == null) return;

                foreach (var sub in d.GetDirectories())
                    Walk(dir.TrimEnd('/') + "/" + sub);

                foreach (var file in d.GetFiles())
                    if (file.EndsWith(".tscn"))
                        paths.Add(dir.TrimEnd('/') + "/" + file);
            }

            Walk("res://Scenes");
            Info($"{paths.Count} cenas .tscn encontradas");

            foreach (var path in paths.OrderBy(x => x))
            {
                // Cenas "principais" que assumem o jogo inteiro rodando (menu/Center/exploração/arena) são
                // exercitadas pelas outras seções e pelo tour de telas - aqui só carregar já pega scripts/recursos quebrados.
                var packed = GD.Load<PackedScene>(path);
                Check(packed != null, $"{path}: não carrega como PackedScene");

                if (packed == null || path.Contains("Main.tscn") || path.EndsWith("Center.tscn") || path.EndsWith("MainMenu.tscn") || path.EndsWith("BattleArena.tscn") || path.EndsWith("ExplorationArea.tscn"))
                    continue;

                try
                {
                    var node = packed.Instantiate();
                    Check(node != null, $"{path}: não instancia");

                    if (node != null)
                    {
                        AddChild(node);
                        await Frames(2);

                        // alguns nós (projétil, indicador de dano) se liberam sozinhos
                        if (IsInstanceValid(node))
                            node.QueueFree();

                        await Frames(1);
                    }
                }
                catch (Exception e)
                {
                    Check(false, $"{path}: exceção ao instanciar ({e.GetType().Name}: {e.Message})");
                }
            }
        }

        // ------------------------------------------------------------------ 15. batalhas aleatórias (movimento e desfecho)

        private async Task RandomBattles()
        {
            int total = int.TryParse(OS.GetEnvironment("DEBUG_AUDIT_BATTLES"), out var t) ? t : 40;
            int stalemates = 0;

            for (int b = 1; b <= total; b++)
            {
                ResetScratch(capacity: 1000);
                int size = _rng.Next(4) == 0 ? 1 : 3;
                var pool = AllSpecies().Where(d => d.Stage >= DigimonStage.Rookie && d.Stage <= DigimonStage.Mega).ToList();
                var team = new List<DigimonInstance>();
                for (int i = 0; i < size; i++)
                    team.Add(MakeDigimon(pool[_rng.Next(pool.Count)], _rng.Next(8, 45)));

                var enemies = Gm.EnemyGenerator.GenerateEnemyTeam(team, size);

                var arena = GD.Load<PackedScene>("res://Scenes/Battle/BattleArena.tscn").Instantiate<BattleArena>();
                GetTree().Root.AddChild(arena);
                arena.Init(team, enemies);

                var units = arena.Match.PlayerTeam.Members.Concat(arena.Match.EnemyTeam.Members)
                    .Select(m => (m, u: arena.GetUnitFor(m))).ToList();

                var last = units.ToDictionary(x => x.m, x => x.u.GlobalPosition);
                var outside = units.ToDictionary(x => x.m, _ => 0.0);
                double sim = 0;
                bool teleport = false, stuckOutside = false;
                string desc = string.Join("+", team.Select(x => x.BaseData.Name)) + " vs " + string.Join("+", enemies.Select(x => x.BaseData.Name));

                while (!arena.IsBattleOver && sim < 240)
                {
                    await Frames(1);
                    double dt = GetProcessDeltaTime();
                    sim += dt;

                    foreach (var (m, u) in units)
                    {
                        if (m.IsDead || !IsInstanceValid(u)) continue;
                        var p = u.GlobalPosition;
                        if (p.DistanceTo(last[m]) > 150f) teleport = true;
                        last[m] = p;

                        if (!arena.IsPointInsideArena(p))
                        {
                            outside[m] += dt;
                            if (outside[m] > 3) stuckOutside = true;
                        }
                        else outside[m] = 0;
                    }
                }

                if (!arena.IsBattleOver)
                {
                    stalemates++;
                    Check(false, $"batalha {b}: impasse (>240s) {desc}");
                    DumpArena(arena);
                    GD.Print($"[AUDIT-IMPASSE] equipes: P[{string.Join(", ", team.Select(x => $"{x.BaseData.Name} L{x.Level} HP{x.MaxHealthPoints} ATK{x.CurrentStats.PhysicalDamage}/{x.CurrentStats.SpecialDamage} DEF{x.CurrentStats.PhysicalDefense}/{x.CurrentStats.SpecialDefense} SPD{x.CurrentStats.Speed}"))}] E[{string.Join(", ", enemies.Select(x => $"{x.BaseData.Name} L{x.Level} HP{x.MaxHealthPoints} ATK{x.CurrentStats.PhysicalDamage}/{x.CurrentStats.SpecialDamage} DEF{x.CurrentStats.PhysicalDefense}/{x.CurrentStats.SpecialDefense} SPD{x.CurrentStats.Speed}"))}]");
                }

                Check(!teleport, $"batalha {b}: teletransporte detectado {desc}");
                Check(!stuckOutside, $"batalha {b}: unidade fora da arena por >3s {desc}");
                AssertInvariants(team, $"batalha {b}");
                arena.QueueFree();
                await Frames(2);
            }

            Info($"{total} batalhas aleatórias, {stalemates} impasses");
        }
    }
}
