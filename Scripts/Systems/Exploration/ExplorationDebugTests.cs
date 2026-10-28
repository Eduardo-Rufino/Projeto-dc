using Godot;
using ProjetoDC.Enums;
using ProjetoDC.Scripts.Data;
using ProjetoDC.Scripts.Gameplay;
using ProjetoDC.Scripts.Managers;
using System.Collections.Generic;
using System.Linq;

namespace ProjetoDC.Scripts.Systems.Exploration
{
    /// <summary>
    /// Verificação manual (via log) da integridade dos dados de exploração - temas, mapas, NPCs,
    /// quests, itens e drops. O projeto não tem framework de teste (ver AGENTS.md), então roda
    /// sob demanda com a variável de ambiente DEBUG_TEST_EXPLORATION, mesmo padrão de
    /// DEBUG_TEST_PASSIVES (ver Center._Ready). Só lê dados: nada toca no save.
    ///
    /// Rode depois de mexer em Data/Maps, Data/NPCs, Data/Quests, Data/Itens ou
    /// Data/AreaThemes - a maior parte dos erros nesses arquivos (id que não existe, sprite
    /// faltando, quest que ninguém consegue completar) não dá erro nenhum no log do jogo, só
    /// aparece como um NPC ou área "estranha" jogando.
    /// </summary>
    public static class ExplorationDebugTests
    {
        private static int _failures;

        // Mesmo valor de ExplorationArea.MaxGuardDistance / o polígono de Boundary da cena.
        private const float MaxGuardDistance = 260f;
        // Espelha WildEncounterSpawn.WanderRadius (privado): até onde um selvagem vagueia do spawn.
        private const float WildWanderRadius = 220f;
        private const float MapWidth = 3400f;
        private const float MapHeight = 2200f;

        // Pontos de interesse mais perto que isso ficam um em cima do outro.
        private const float MinPointSpacing = 60f;

        private static DatabaseManager Db => DatabaseManager.Instance;

        public static void Run()
        {
            _failures = 0;

            GD.Print("===== ExplorationDebugTests: início =====");

            TestThemes();
            TestMaps();
            TestNpcsAndQuests();
            TestProgression();
            TestIsoMaps();

            GD.Print(_failures == 0
                ? "===== ExplorationDebugTests: TODOS OS TESTES PASSARAM ====="
                : $"===== ExplorationDebugTests: {_failures} FALHA(S) =====");
        }

        private static void Check(bool condition, string message)
        {
            if (condition)
                return;

            _failures++;
            GD.PrintErr($"[FALHOU] {message}");
        }

        private static void Ok(string message) => GD.Print($"[OK] {message}");

        // ---------------------------------------------------------------- temas

        private static void TestThemes()
        {
            var themeIds = Db.GetAllExplorationMaps().Select(m => m.Theme ?? "forest").Distinct().ToList();

            foreach (var id in themeIds)
            {
                var theme = Db.GetAreaTheme(id);

                Check(theme != null && theme.Id.ToLowerInvariant() == id.ToLowerInvariant(),
                    $"tema '{id}' (usado por um mapa) não foi carregado");

                if (theme == null)
                    continue;

                Check(theme.Folder.EndsWith("/"), $"tema '{id}': Folder deveria terminar com '/'");

                if (!string.IsNullOrEmpty(theme.GroundTexture))
                    Check(ResourceLoader.Exists(theme.Folder + theme.GroundTexture),
                        $"tema '{id}': textura de chão '{theme.Folder}{theme.GroundTexture}' não existe");
                Check(theme.DecorationCount > 0, $"tema '{id}': DecorationCount deveria ser > 0");

                if (theme.HasShoreline)
                {
                    Check(ResourceLoader.Exists(theme.Folder + theme.ShoreTexture),
                        $"tema '{id}': textura da orla '{theme.Folder}{theme.ShoreTexture}' não existe");
                    Check(ResourceLoader.Exists(theme.Folder + theme.SeaTexture),
                        $"tema '{id}': textura do mar '{theme.Folder}{theme.SeaTexture}' não existe");
                    Check(theme.ShoreLine > 0.3f && theme.ShoreLine < 0.9f,
                        $"tema '{id}': ShoreLine {theme.ShoreLine} fora da faixa razoável (0.3 a 0.9)");

                    // Sem terra suficiente ou sem mar visível a praia perde o sentido.
                    Check(theme.GetWalkLimitY(0, MapHeight) is float limit && limit > 600 && limit < MapHeight - 400,
                        $"tema '{id}': o limite de caminhada não deixa terra e mar suficientes no mapa");

                    foreach (var kind in theme.SeaKinds)
                        Check(theme.Variants.ContainsKey(kind), $"tema '{id}': o tipo de mar '{kind}' não tem variações");
                }
                Check(theme.ScaleMin > 0 && theme.ScaleMin <= theme.ScaleMax, $"tema '{id}': faixa de escala inválida");

                foreach (var (kind, variants) in theme.Variants)
                {
                    Check(variants.Count > 0, $"tema '{id}': o tipo '{kind}' não tem nenhuma variação");

                    foreach (var variant in variants)
                    {
                        Check(ResourceLoader.Exists(theme.Folder + variant.File),
                            $"tema '{id}': sprite '{theme.Folder}{variant.File}' não existe");

                        if (variant.Solid != null)
                            Check(variant.Solid.Width > 0 && variant.Solid.Height > 0,
                                $"tema '{id}': caixa sólida de '{variant.File}' com tamanho inválido");

                        foreach (var extra in variant.ExtraSolids)
                            Check(extra.Width > 0 && extra.Height > 0,
                                $"tema '{id}': caixa sólida extra de '{variant.File}' com tamanho inválido");
                    }
                }

                var usedKinds = theme.DefaultKinds
                    .Concat(theme.ConcealmentKinds)
                    .Concat(theme.Zones.SelectMany(z => z.Kinds))
                    .Distinct();

                foreach (var kind in usedKinds)
                    Check(theme.Variants.ContainsKey(kind), $"tema '{id}': o tipo '{kind}' é usado mas não tem variações");

                Check(theme.DefaultKinds.Count > 0, $"tema '{id}': DefaultKinds vazio");

                // O cerco dos NPCs escondidos só bloqueia de verdade com pelo menos um tipo sólido.
                bool concealmentHasSolid = theme.ConcealmentKinds.Any(k =>
                    theme.Variants.TryGetValue(k, out var v) && v.Any(x => x.Solid != null));

                Check(theme.ConcealmentKinds.Count == 0 || concealmentHasSolid,
                    $"tema '{id}': nenhum tipo do cerco (ConcealmentKinds) é sólido - o cerco não bloquearia");

                Ok($"tema '{id}': {theme.Variants.Count} tipos, {theme.Zones.Count} zonas");
            }
        }

        // ---------------------------------------------------------------- mapas

        private static void TestMaps()
        {
            var maps = Db.GetAllExplorationMaps().ToList();
            var usedPickupIds = new HashSet<int>();

            foreach (var map in maps)
            {
                string name = $"mapa {map.Id} '{map.Name}'";

                Check(map.WildPool.Count > 0, $"{name}: WildPool vazio");
                Check(map.WildLevelMin >= 1 && map.WildLevelMin <= map.WildLevelMax, $"{name}: faixa de nível dos selvagens inválida");

                foreach (var id in map.WildPool)
                {
                    var species = Db.GetDigimon(id);

                    Check(species != null, $"{name}: selvagem {id} não existe em Data/Digimon");

                    if (species != null)
                        Check(ResourceLoader.Exists($"res://Assets/Sprites/Digimon/{species.Code}/0.png"),
                            $"{name}: selvagem {species.Code} não tem sprite");
                }

                var points = new List<(string Label, Vector2 Position)>();

                // Mapa isométrico: as posições são spots/blocos do mapa, conferidas em TestIsoMaps.
                bool iso = !string.IsNullOrEmpty(map.IsoMapScene);

                foreach (var p in iso ? new List<WildSpawnPointData>() : map.WildSpawnPoints)
                    points.Add(("selvagem", new Vector2(p.X, p.Y)));

                foreach (var n in iso ? new List<NpcSpawnData>() : map.Npcs)
                    points.Add(($"NPC {n.NpcId}", new Vector2(n.X, n.Y)));

                foreach (var p in iso ? new List<ItemPickupData>() : map.ItemPickups)
                    points.Add(($"item {p.UniqueId}", new Vector2(p.X, p.Y)));

                // Tema com mar: nada de ponto de interesse na água. Selvagens vagueiam até
                // WildWanderRadius do spawn sem checar o limite, então precisam de folga.
                float? walkLimit = Db.GetAreaTheme(map.Theme ?? "forest")?.GetWalkLimitY(0, MapHeight);

                if (walkLimit.HasValue && !iso)
                {
                    foreach (var p in map.WildSpawnPoints)
                        Check(p.Y + WildWanderRadius <= walkLimit.Value,
                            $"{name}: selvagem em ({p.X},{p.Y}) pode vaguear pra dentro do mar (limite {walkLimit.Value})");

                    foreach (var (label, pos) in points.Where(x => x.Label != "selvagem"))
                        Check(pos.Y <= walkLimit.Value - 40f,
                            $"{name}: {label} em ({pos.X},{pos.Y}) fica na água (limite {walkLimit.Value})");
                }

                foreach (var (label, pos) in points)
                    Check(pos.X >= 0 && pos.X <= MapWidth && pos.Y >= 0 && pos.Y <= MapHeight,
                        $"{name}: {label} em ({pos.X},{pos.Y}) fica fora do mapa ({MapWidth}x{MapHeight})");

                for (int i = 0; i < points.Count; i++)
                {
                    for (int j = i + 1; j < points.Count; j++)
                    {
                        Check(points[i].Position.DistanceTo(points[j].Position) >= MinPointSpacing,
                            $"{name}: {points[i].Label} e {points[j].Label} estão a menos de {MinPointSpacing}px um do outro");
                    }
                }

                foreach (var n in map.Npcs)
                {
                    Check(Db.GetNpc(n.NpcId) != null, $"{name}: NPC {n.NpcId} não existe em Data/NPCs");

                    if (!n.Hidden)
                        continue;

                    // NPC escondido sem selvagem por perto ("guarda" da entrada do cerco) é permitido -
                    // a abertura do cerco cai de volta pro ponto de partida (ver
                    // ExplorationArea.SpawnConcealmentRings) -, só vale avisar: normalmente o
                    // recanto tem um guarda.
                    var center = new Vector2(n.X, n.Y);
                    bool hasGuard = map.WildSpawnPoints.Any(w => new Vector2(w.X, w.Y).DistanceTo(center) < MaxGuardDistance);

                    if (!hasGuard)
                        GD.Print($"[AVISO] {name}: NPC escondido {n.NpcId} sem selvagem a menos de {MaxGuardDistance}px (a abertura do cerco aponta pro ponto de partida)");
                }

                foreach (var pickup in map.ItemPickups)
                {
                    Check(Db.GetItem(pickup.ItemId) != null, $"{name}: item {pickup.ItemId} do pickup {pickup.UniqueId} não existe");

                    // UniqueId é lembrado numa lista GLOBAL de coletados: repetir entre mapas faria
                    // coletar um item em uma área "consumir" o de outra.
                    Check(usedPickupIds.Add(pickup.UniqueId),
                        $"{name}: UniqueId {pickup.UniqueId} de pickup já é usado (em outro pickup/mapa)");
                }

                Ok($"{name}: {map.WildSpawnPoints.Count} selvagens, {map.Npcs.Count} NPCs, {map.ItemPickups.Count} itens");
            }
        }

        // ---------------------------------------------------------------- NPCs e quests

        private static void TestNpcsAndQuests()
        {
            // Ícone declarado que não existe faz o inventário registrar erro e mostrar o item em branco.
            foreach (var item in Db.GetAllItems())
            {
                if (string.IsNullOrEmpty(item.IconPath))
                    GD.Print($"[AVISO] item {item.Id} '{item.Name}' sem ícone (IconPath vazio)");
                else
                    Check(ResourceLoader.Exists(item.IconPath), $"item {item.Id} '{item.Name}': ícone '{item.IconPath}' não existe");
            }

            var dropItemsInPools = new HashSet<int>();

            foreach (var map in Db.GetAllExplorationMaps())
            {
                foreach (var id in map.WildPool)
                {
                    var species = Db.GetDigimon(id);

                    if (species == null)
                        continue;

                    foreach (var drop in species.Drops)
                        dropItemsInPools.Add(drop.ItemId);
                }
            }

            var recruitedAreaTypes = new HashSet<CenterAreaType>();

            foreach (var map in Db.GetAllExplorationMaps())
            {
                foreach (var spawn in map.Npcs)
                {
                    var npc = Db.GetNpc(spawn.NpcId);

                    if (npc == null)
                        continue;

                    string name = $"NPC {npc.Id} {npc.Name}";

                    Check(ResourceLoader.Exists($"res://Assets/Sprites/Digimon/{npc.Code}.png"), $"{name}: sem retrato ({npc.Code}.png)");
                    Check(npc.DialogueLines.Count > 0, $"{name}: sem falas");

                    if (npc.RecruitmentTrainingAreaType.HasValue)
                        Check(recruitedAreaTypes.Add(npc.RecruitmentTrainingAreaType.Value),
                            $"{name}: outro NPC já dá o bônus de {npc.RecruitmentTrainingAreaType.Value}");

                    if (!npc.QuestId.HasValue)
                        continue;

                    var quest = Db.GetQuest(npc.QuestId.Value);

                    Check(quest != null, $"{name}: quest {npc.QuestId} não existe");

                    if (quest == null)
                        continue;

                    Check(quest.GiverNpcId == npc.Id, $"quest {quest.Id} '{quest.Name}': GiverNpcId {quest.GiverNpcId} != NPC dono {npc.Id}");

                    if (quest.ObjectiveType == QuestObjectiveType.Talk)
                    {
                        Check(quest.ObjectiveCount == 0, $"quest {quest.Id}: Talk deveria ter ObjectiveCount 0");
                    }
                    else
                    {
                        Check(quest.ObjectiveCount > 0, $"quest {quest.Id}: ObjectiveCount deveria ser > 0");
                    }

                    if (quest.ObjectiveType == QuestObjectiveType.DeliverItem)
                    {
                        // O item a entregar tem que vir de algum lugar: um pickup, a recompensa de outra
                        // quest ou o drop de um selvagem (ex.: o Fragmento do Destino do Renamon).
                        bool obtainable = Db.GetAllExplorationMaps().Any(m => m.ItemPickups.Any(pk => pk.ItemId == quest.ObjectiveTargetId)) ||
                                          Db.GetAllQuests().Any(q => q.Id != quest.Id && q.RewardItemId == quest.ObjectiveTargetId && q.RewardItemQuantity > 0) ||
                                          dropItemsInPools.Contains(quest.ObjectiveTargetId);

                        Check(Db.GetItem(quest.ObjectiveTargetId) != null, $"quest {quest.Id}: item {quest.ObjectiveTargetId} não existe");
                        Check(obtainable, $"quest {quest.Id} '{quest.Name}': o item {quest.ObjectiveTargetId} não vem de nenhum pickup nem recompensa");
                    }
                    else if (quest.ObjectiveType == QuestObjectiveType.CollectItem)
                    {
                        Check(Db.GetItem(quest.ObjectiveTargetId) != null, $"quest {quest.Id}: item {quest.ObjectiveTargetId} não existe");

                        // Sem nenhum selvagem que solte o item, a quest de coleta seria impossível.
                        Check(dropItemsInPools.Contains(quest.ObjectiveTargetId),
                            $"quest {quest.Id} '{quest.Name}': nenhum selvagem de nenhum mapa solta o item {quest.ObjectiveTargetId} - impossível de completar");

                        // ...e o item precisa cair de alguém do pool do MESMO mapa do NPC, senão o jogador
                        // teria que ir farmar em outra área sem nenhuma pista disso.
                        Check(map.WildPool.Any(id => Db.GetDigimon(id)?.Drops.Any(d => d.ItemId == quest.ObjectiveTargetId) == true),
                            $"quest {quest.Id} '{quest.Name}': ninguém do pool de '{map.Name}' solta o item {quest.ObjectiveTargetId}");
                    }
                    else if (quest.ObjectiveType == QuestObjectiveType.SwitchOn)
                    {
                        // Objetos a acender/ligar: todos cadastrados como Switches do mapa do objetivo
                        // (a existência deles no JSON do mapa é conferida em TestIsoMaps).
                        var targetMap = Db.GetExplorationMap(quest.ObjectiveMapId);

                        Check(targetMap != null && !string.IsNullOrEmpty(targetMap.IsoMapScene),
                            $"quest {quest.Id} '{quest.Name}': ObjectiveMapId {quest.ObjectiveMapId} não é um mapa isométrico");
                        Check(quest.ObjectiveSwitchIds.Count == quest.ObjectiveCount,
                            $"quest {quest.Id} '{quest.Name}': ObjectiveCount {quest.ObjectiveCount} != {quest.ObjectiveSwitchIds.Count} objetos");

                        foreach (var id in quest.ObjectiveSwitchIds)
                            Check(targetMap != null && targetMap.Switches.Any(sw => sw.Id == id),
                                $"quest {quest.Id} '{quest.Name}': o objeto '{id}' não está em Switches do mapa {quest.ObjectiveMapId}");
                    }
                    else if (quest.ObjectiveType == QuestObjectiveType.DefeatWild && quest.ObjectiveTargetId != 0)
                    {
                        // Caçada por espécie: o alvo precisa aparecer no mapa onde a quest é dada.
                        Check(map.WildPool.Contains(quest.ObjectiveTargetId),
                            $"quest {quest.Id} '{quest.Name}': a espécie {quest.ObjectiveTargetId} não está no pool de '{map.Name}'");
                    }

                    if (quest.RewardItemId > 0)
                        Check(Db.GetItem(quest.RewardItemId) != null && quest.RewardItemQuantity > 0,
                            $"quest {quest.Id} '{quest.Name}': item de recompensa {quest.RewardItemId} inválido");
                }
            }

            Ok($"NPCs/quests: {recruitedAreaTypes.Count} tipos de treino com um NPC recrutável ({string.Join(", ", recruitedAreaTypes)})");
        }

        // ---------------------------------------------------------------- progressão

        private static void TestProgression()
        {
            foreach (var map in Db.GetAllExplorationMaps())
            {
                if (!map.RequiredQuestId.HasValue)
                    continue;

                string name = $"mapa {map.Id} '{map.Name}'";
                var quest = Db.GetQuest(map.RequiredQuestId.Value);

                Check(quest != null, $"{name}: RequiredQuestId {map.RequiredQuestId} não existe");

                if (quest == null)
                    continue;

                // A quest que libera a área precisa ser oferecida numa área que o jogador
                // consegue alcançar ANTES - senão a área nunca abriria (ciclo/impossível).
                // (Dentro de um mapa isométrico, TestIsoMaps confere que o NPC é alcançável.)
                var giverMap = Db.GetAllExplorationMaps()
                    .FirstOrDefault(m => m.Npcs.Any(n => n.NpcId == quest.GiverNpcId));

                Check(giverMap != null, $"{name}: ninguém oferece a quest {quest.Id} que a libera");

                if (giverMap == null)
                    continue;

                Check(giverMap.Id != map.Id, $"{name}: a quest que libera a área só é oferecida dentro dela mesma");
                Check(giverMap.RequiredQuestId == null || giverMap.RequiredQuestId != map.RequiredQuestId,
                    $"{name}: a quest que libera a área depende dela mesma");
                Check(giverMap.RecommendedLevel <= map.RecommendedLevel,
                    $"{name}: a área que libera esta (nível {giverMap.RecommendedLevel}) é mais difícil que ela (nível {map.RecommendedLevel})");

                Ok($"{name}: liberada por '{quest.Name}' (dada em '{giverMap.Name}')");
            }
        }

        // ---------------------------------------------------------------- mapas isométricos

        /// <summary>
        /// Mapas isométricos (ExplorationMapData.IsoMapScene): todo spot/bloco usado existe e é
        /// andável, cada bloqueio tem a sua quest, e a ordem das quests fecha sem travar - simula
        /// o jogador: começa sem nenhum bloqueio aberto, conclui toda quest de bloqueio cujo NPC
        /// já alcança (e cujo item já consegue pegar) e repete até abrir tudo. Também confere
        /// que cada passagem (mina, barqueiro) só é alcançável depois da quest dela.
        /// </summary>
        private static void TestIsoMaps()
        {
            foreach (var map in Db.GetAllExplorationMaps().Where(m => !string.IsNullOrEmpty(m.IsoMapScene)))
            {
                string name = $"mapa isométrico {map.Id} '{map.Name}'";

                var scene = GD.Load<PackedScene>(map.IsoMapScene);
                Check(scene != null, $"{name}: cena {map.IsoMapScene} não existe");

                if (scene == null)
                    continue;

                var loader = scene.Instantiate<MapLoader>();
                loader.Build();

                TestIsoMap(map, loader, name);

                loader.Free();
            }
        }

        private static void TestIsoMap(ExplorationMapData map, MapLoader loader, string name)
        {
            Vector2I? Resolve(string spot, List<int> tile, string label)
            {
                if (!string.IsNullOrEmpty(spot))
                {
                    bool found = loader.TryGetSpot(spot, out var info);
                    Check(found, $"{name}: {label}: spot '{spot}' não existe no mapa");
                    return found ? info.Tile : null;
                }

                Check(tile != null && tile.Count == 2, $"{name}: {label}: sem Spot nem Tile");
                return tile != null && tile.Count == 2 ? new Vector2I(tile[0], tile[1]) : null;
            }

            foreach (var id in loader.BlockerIds)
                Check(map.BlockerQuests.ContainsKey(id), $"{name}: o bloqueio '{id}' do mapa não tem quest em BlockerQuests");

            foreach (var (id, questId) in map.BlockerQuests)
            {
                Check(loader.HasBlocker(id), $"{name}: BlockerQuests cita '{id}', que não existe no mapa");
                Check(Db.GetQuest(questId) != null, $"{name}: bloqueio '{id}': quest {questId} não existe");
            }

            // Tudo que precisa ser alcançado, com o rótulo pro log.
            var points = new List<(string Label, Vector2I Tile, int? NpcId, ItemPickupData Pickup)>();
            var spawn = Resolve(map.ExplorerSpawnSpot, null, "spawn do explorador");

            foreach (var w in map.WildSpawnPoints)
                if (Resolve(w.Spot, w.Tile, "selvagem") is Vector2I t) points.Add(($"selvagem {w.Spot}", t, null, null));

            foreach (var n in map.Npcs)
            {
                if (Resolve(n.Spot, n.Tile, $"NPC {n.NpcId}") is not Vector2I t)
                    continue;

                // NPC escondido dentro de um arbusto/pedra (ex.: o Impmon): vale o bloco do lado
                // de onde o explorador conversa com ele.
                string label = $"NPC {n.NpcId} ({Db.GetNpc(n.NpcId)?.Name})";

                if (loader.IsSolidProp(t))
                {
                    var side = SideTile(loader, t);
                    Check(side.HasValue, $"{name}: {label} está num objeto sólido em {t} sem nenhum bloco andável do lado");

                    if (!side.HasValue)
                        continue;

                    label += $" (ao lado de {t})";
                    t = side.Value;
                }

                points.Add((label, t, n.NpcId, null));
            }

            foreach (var p in map.ItemPickups)
                if (Resolve(p.Spot, p.Tile, $"item {p.UniqueId}") is Vector2I t) points.Add(($"item {p.UniqueId} ({p.Spot})", t, null, p));

            foreach (var tr in map.Transitions)
            {
                // Destino 0 = passagem ainda sem destino (só o aviso de "em breve").
                Check(Db.GetExplorationMap(tr.TargetMapId) != null || (tr.TargetMapId == 0 && !string.IsNullOrEmpty(tr.ComingSoonText)),
                    $"{name}: passagem '{tr.Id}': área {tr.TargetMapId} não existe");
                Check(tr.Mine || tr.NpcId.HasValue || !string.IsNullOrEmpty(tr.Spot), $"{name}: passagem '{tr.Id}' sem origem (Mine/NpcId/Spot)");

                // Passagem em que se entra andando: o spot dela precisa ser alcançável (e, se tem
                // quest, só depois dela - ver abaixo).
                if (!string.IsNullOrEmpty(tr.Spot) && Resolve(tr.Spot, null, $"passagem '{tr.Id}'") is Vector2I spotTile)
                    points.Add(($"passagem '{tr.Id}'", spotTile, null, null));

                if (tr.NpcId.HasValue)
                    Check(map.Npcs.Any(n => n.NpcId == tr.NpcId), $"{name}: passagem '{tr.Id}': NPC {tr.NpcId} não está no mapa");

                if (tr.Mine)
                    Check(loader.MineNode != null, $"{name}: passagem '{tr.Id}' é a mina, mas o mapa não tem mina");

                // A passagem em que se entra andando pode não ter volta (destino ainda não existe).
                if ((tr.ReturnTile != null || string.IsNullOrEmpty(tr.Spot)) &&
                    Resolve(null, tr.ReturnTile, $"passagem '{tr.Id}' (ReturnTile)") is Vector2I t)
                    points.Add(($"frente da passagem '{tr.Id}'", t, null, null));
            }

            // Quests do mapa (ids do JSON) e objetos que acendem/ligam.
            foreach (var (mapQuestId, questId) in map.MapQuestIds)
                Check(Db.GetQuest(questId) != null, $"{name}: MapQuestIds '{mapQuestId}': quest {questId} não existe");

            foreach (var (lightId, rule) in loader.LightRules)
            {
                if (rule.StartsWith("after:"))
                    Check(map.MapQuestIds.ContainsKey(rule.Substring(6)), $"{name}: luz '{lightId}' ({rule}) cita uma quest fora de MapQuestIds");
                else if (rule.StartsWith("when_on:"))
                    Check(loader.HasSwitchable(rule.Substring(8)), $"{name}: luz '{lightId}' ({rule}) cita um objeto que não existe");
                else
                    Check(rule == "always", $"{name}: luz '{lightId}' com regra desconhecida '{rule}'");
            }

            foreach (var id in loader.SwitchableIds)
            {
                string rule = loader.SwitchRule(id) ?? "";

                if (rule.StartsWith("quest:"))
                    Check(map.MapQuestIds.ContainsKey(rule.Substring(6)), $"{name}: objeto '{id}' ({rule}) cita uma quest fora de MapQuestIds");
                else
                    Check(map.Switches.Any(sw => sw.Id == id), $"{name}: objeto '{id}' ({rule}) não está em Switches - ninguém consegue ligar");
            }

            var switchSides = new Dictionary<string, Vector2I>();

            foreach (var sw in map.Switches)
            {
                Check(loader.HasSwitchable(sw.Id), $"{name}: Switches cita '{sw.Id}', que não existe no mapa");

                if (!loader.HasSwitchable(sw.Id))
                    continue;

                // O objeto é sólido: vale o bloco do lado de onde o explorador liga.
                var side = SideTile(loader, loader.SwitchTile(sw.Id));
                Check(side.HasValue, $"{name}: objeto '{sw.Id}' sem nenhum bloco andável do lado");

                if (side.HasValue)
                {
                    switchSides[sw.Id] = side.Value;
                    points.Add(($"objeto '{sw.Id}' (ao lado de {loader.SwitchTile(sw.Id)})", side.Value, null, null));
                }
            }

            if (spawn == null)
                return;

            // Tudo liberado: todo ponto precisa ser andável e alcançável.
            foreach (var id in loader.BlockerIds)
                loader.SetBlockerCleared(id, true);

            var reachableAll = Reachable(loader, spawn.Value);

            foreach (var (label, tile, _, _) in points)
            {
                Check(loader.IsWalkable(tile), $"{name}: {label} no bloco {tile} não é andável");
                Check(reachableAll.Contains(tile), $"{name}: {label} no bloco {tile} não é alcançável nem com tudo liberado");
            }

            // De que bloqueios cada ponto depende (log, pra conferir com o desenho do mapa).
            var requirements = new Dictionary<string, List<string>>();

            foreach (var blocker in loader.BlockerIds.ToList())
            {
                foreach (var id in loader.BlockerIds)
                    loader.SetBlockerCleared(id, id != blocker);

                var reachable = Reachable(loader, spawn.Value);

                foreach (var (label, tile, _, _) in points)
                {
                    if (reachable.Contains(tile))
                        continue;

                    if (!requirements.TryGetValue(label, out var list))
                        requirements[label] = list = new List<string>();

                    list.Add(blocker);
                }
            }

            foreach (var (label, list) in requirements)
                GD.Print($"[INFO] {name}: {label} só depois de {string.Join(" + ", list)}");

            // Cada passagem só depois da quest dela.
            foreach (var tr in map.Transitions.Where(t => t.RequiredQuestId.HasValue && (t.ReturnTile?.Count == 2 || !string.IsNullOrEmpty(t.Spot))))
            {
                string label = !string.IsNullOrEmpty(tr.Spot) ? $"passagem '{tr.Id}'" : $"frente da passagem '{tr.Id}'";
                var needed = map.BlockerQuests.Where(b => b.Value == tr.RequiredQuestId.Value).Select(b => b.Key).ToList();

                Check(needed.Count > 0 && requirements.TryGetValue(label, out var req) && needed.All(req.Contains),
                    $"{name}: passagem '{tr.Id}' é alcançável sem a quest {tr.RequiredQuestId}");
            }

            // Simula a progressão: nada liberado, conclui o que der, repete.
            var cleared = new HashSet<string>();
            var completedQuests = new HashSet<int>();
            var order = new List<string>();
            bool progressed = true;

            while (progressed)
            {
                progressed = false;

                foreach (var id in loader.BlockerIds)
                    loader.SetBlockerCleared(id, cleared.Contains(id));

                var reachable = Reachable(loader, spawn.Value);

                foreach (var (blocker, questId) in map.BlockerQuests)
                {
                    if (cleared.Contains(blocker))
                        continue;

                    var quest = Db.GetQuest(questId);

                    if (quest == null)
                        continue;

                    var giver = points.FirstOrDefault(p => p.NpcId == quest.GiverNpcId);

                    if (giver.Label == null || !reachable.Contains(giver.Tile))
                        continue;

                    if (quest.ObjectiveType == QuestObjectiveType.DeliverItem)
                    {
                        bool fromPickup = points.Any(p => p.Pickup != null && p.Pickup.ItemId == quest.ObjectiveTargetId && reachable.Contains(p.Tile));
                        bool fromReward = completedQuests.Any(q => Db.GetQuest(q)?.RewardItemId == quest.ObjectiveTargetId);

                        if (!fromPickup && !fromReward)
                            continue;
                    }
                    else if (quest.ObjectiveType == QuestObjectiveType.SwitchOn)
                    {
                        // Todos os objetos alcançáveis; os que pedem fogo, só com a lanterna (o
                        // caminho que não depende do Digimon escolhido).
                        bool hasLantern = completedQuests.Any(q => Db.GetQuest(q)?.GrantsLantern == true);
                        bool allOn = quest.ObjectiveSwitchIds.All(id =>
                            switchSides.TryGetValue(id, out var side) && reachable.Contains(side) &&
                            (hasLantern || map.Switches.FirstOrDefault(sw => sw.Id == id)?.NeedsFire != true));

                        if (!allOn)
                            continue;
                    }

                    cleared.Add(blocker);
                    completedQuests.Add(questId);
                    order.Add($"{quest.Name} -> {blocker}");
                    progressed = true;
                }
            }

            foreach (var id in loader.BlockerIds)
                Check(cleared.Contains(id), $"{name}: o bloqueio '{id}' nunca abre (quest inalcançável ou item que não dá pra pegar antes)");

            Ok($"{name}: {points.Count} pontos alcançáveis; ordem das quests: {string.Join(", ", order)}");
        }

        /// <summary>Um bloco andável vizinho (8 direções, mesmo nível) de um objeto sólido.</summary>
        private static Vector2I? SideTile(MapLoader loader, Vector2I solid)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    var side = solid + new Vector2I(dx, dy);

                    if (side != solid && loader.IsWalkable(side) && loader.Elevation(side) == loader.Elevation(solid))
                        return side;
                }
            }

            return null;
        }

        private static HashSet<Vector2I> Reachable(MapLoader loader, Vector2I start)
        {
            var seen = new HashSet<Vector2I> { start };
            var queue = new Queue<Vector2I>();
            queue.Enqueue(start);

            var dirs = new[] { new Vector2I(1, 0), new Vector2I(-1, 0), new Vector2I(0, 1), new Vector2I(0, -1) };

            while (queue.Count > 0)
            {
                var a = queue.Dequeue();

                foreach (var d in dirs)
                {
                    var b = a + d;

                    if (!seen.Contains(b) && loader.CanStep(a, b))
                    {
                        seen.Add(b);
                        queue.Enqueue(b);
                    }
                }
            }

            return seen;
        }
    }
}
