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

                foreach (var p in map.WildSpawnPoints)
                    points.Add(("selvagem", new Vector2(p.X, p.Y)));

                foreach (var n in map.Npcs)
                    points.Add(($"NPC {n.NpcId}", new Vector2(n.X, n.Y)));

                foreach (var p in map.ItemPickups)
                    points.Add(($"item {p.UniqueId}", new Vector2(p.X, p.Y)));

                // Tema com mar: nada de ponto de interesse na água. Selvagens vagueiam até
                // WildWanderRadius do spawn sem checar o limite, então precisam de folga.
                float? walkLimit = Db.GetAreaTheme(map.Theme ?? "forest")?.GetWalkLimitY(0, MapHeight);

                if (walkLimit.HasValue)
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
                    Check(quest.ObjectiveCount > 0, $"quest {quest.Id}: ObjectiveCount deveria ser > 0");

                    if (quest.ObjectiveType == QuestObjectiveType.CollectItem)
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
                    else if (quest.ObjectiveTargetId != 0)
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
    }
}
