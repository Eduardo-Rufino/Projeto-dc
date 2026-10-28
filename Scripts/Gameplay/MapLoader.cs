using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace ProjetoDC.Scripts.Gameplay
{
    /// <summary>
    /// Mapa isométrico de exploração montado a partir de um JSON (formato "digimon-iso-map" do
    /// pacote modelo_floresta - ver Assets/Areas/Forest_new/modelo_floresta/GUIA_CLAUDE_CODE.md)
    /// + o manifest do kit de sprites. Só monta o visual e responde as regras de andar (blocos,
    /// rampas, bloqueios de quest): selvagens, NPCs, itens e o explorador continuam sendo os da
    /// ExplorationArea, que os coloca como filhos de <see cref="Sorted"/> com Position = posição
    /// NO CHÃO (coordenadas nativas deste nó, sem a escala 2).
    ///
    /// Camadas: Ground (blocos no chão, na ordem do JSON), Shadows (sombras dos objetos no
    /// chão) e Sorted (Y-sort: blocos altos, rampas, objetos, bloqueios e as entidades). Cada
    /// item de Sorted fica em (pos.x, sort_y) e o sprite tem offset = -anchor + (0, pos.y - sort_y)
    /// - desenha na altura certa, mas ordena pelo chão.
    ///
    /// Animação: um relógio único (ver _Process) troca o quadro de todos os sprites animados
    /// de uma vez, em vez de milhares de AnimatedSprite2D rodando cada um o seu.
    /// </summary>
    public partial class MapLoader : Node2D
    {
        public const int ElevationStepPx = 18;

        // Quanto o digimon "sobe" no planalto/rampa já está em VisualLift; isso aqui é só a
        // regra de transparência das árvores (px nativos): o digimon está atrás da árvore se
        // está acima da base dela, até essa altura, e perto o bastante na horizontal.
        private const float TreeFadeHeight = 82f;
        private const float TreeFadeHalfWidth = 36f;
        private const float TreeFadeAlpha = 0.45f;
        private const float TreeFadeSpeed = 6f;

        [Export(PropertyHint.File, "*.json")] public string MapJson { get; set; } = "res://Assets/Areas/Forest_new/modelo_floresta/mapa/mapa_floresta.json";
        [Export(PropertyHint.Dir)] public string KitDir { get; set; } = "res://Assets/Areas/Forest_new/modelo_floresta/kit";

        /// <summary>Mostra os ícones provisórios (marker_*) dos spots - só pra abrir a cena do
        /// mapa sozinha no editor. A ExplorationArea desliga e põe as coisas de verdade.</summary>
        [Export] public bool ShowSpotPlaceholders { get; set; }

        /// <summary>Monta parado no mesmo quadro que a ferramenta montar_previa.py do pacote usa
        /// (blocos no quadro 0, sem animar) - só pra comparar o resultado com a prévia (ver
        /// IsoMapRenderTest).</summary>
        [Export] public bool PreviewMode { get; set; }

        /// <summary>Objetos que o mapa deixa atravessar (collides: false) mas que aqui viram
        /// sólidos: o bloco deles vira 'O' na grade 'walk', então ganham parede de colisão e o
        /// caminho por clique/vagueio desvia. Começo do nome do sprite no kit. Grama, flores,
        /// samambaias e juncos continuam atravessáveis.</summary>
        [Export] public string[] SolidProps { get; set; } = { "stones", "bush", "fallen_log", "signpost", "mushrooms" };

        /// <summary>Pasta de ajustes do kit: um arquivo aqui com o mesmo caminho relativo de um
        /// do kit é usado no lugar dele (ex.: poste/antena com a luz piscando sem a estrutura
        /// pular - ver gerar_luzes.py nessa pasta). O pacote do kit fica intacto.</summary>
        [Export(PropertyHint.Dir)] public string KitOverrideDir { get; set; } = "res://Assets/Areas/Forest_new/ajustes";

        /// <summary>Quem deixa as árvores translúcidas ao passar atrás delas - filho de
        /// <see cref="Sorted"/>, com Position no chão.</summary>
        public Node2D Player { get; set; }

        public Node2D Sorted { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        private Node2D _ground;
        private Node2D _shadows;
        private Node2D _walls;
        private JsonElement _manifest;
        private string[] _walk = Array.Empty<string>();
        private string[] _elevation = Array.Empty<string>();
        private bool _built;

        private readonly Dictionary<string, Texture2D> _textures = new();
        private readonly Dictionary<Vector2I, string> _ramps = new();
        private readonly Dictionary<Vector2I, float> _blockSortY = new();
        private readonly Dictionary<Vector2I, string> _questTiles = new();
        private readonly HashSet<string> _clearedBlockers = new();
        private readonly Dictionary<string, List<Node2D>> _blockedNodes = new();
        private readonly Dictionary<string, List<Node2D>> _clearedNodes = new();
        private readonly Dictionary<string, StaticBody2D> _blockerBodies = new();
        private readonly Dictionary<string, Node2D> _rafts = new();
        private readonly Dictionary<string, SpotInfo> _spots = new();
        private readonly List<(Node2D Node, Vector2 Ground)> _trees = new();

        // Relógio único de animação: cada grupo (fps) troca o quadro de todos os sprites dele
        // quando o quadro global muda.
        private readonly Dictionary<float, List<(Sprite2D Sprite, int Start)>> _animated = new();
        private readonly Dictionary<float, int> _animatedLastFrame = new();
        private double _animationTime;

        public readonly struct SpotInfo
        {
            public SpotInfo(string id, string type, Vector2I tile) { Id = id; Type = type; Tile = tile; }
            public string Id { get; }
            public string Type { get; }
            public Vector2I Tile { get; }
        }

        /// <summary>O objeto da mina (aparece quando o deslizamento é liberado) - a
        /// ExplorationArea põe a interação de entrar nela.</summary>
        public Node2D MineNode { get; private set; }
        public string MineBlockerId { get; private set; }

        public override void _Ready()
        {
            Build();
        }

        public void Build()
        {
            if (_built)
                return;

            _built = true;

            TextureFilter = TextureFilterEnum.Nearest;
            Scale = new Vector2(2, 2);

            _ground = AddLayer("Ground", false);
            _shadows = AddLayer("Shadows", false);
            Sorted = AddLayer("Sorted", true);
            _walls = AddLayer("Walls", false);

            _manifest = JsonDocument.Parse(FileAccess.GetFileAsString(KitDir + "/manifest.json")).RootElement.Clone();
            var map = JsonDocument.Parse(FileAccess.GetFileAsString(MapJson)).RootElement.Clone();

            var size = map.GetProperty("size_tiles");
            Width = size[0].GetInt32();
            Height = size[1].GetInt32();
            _walk = ReadRows(map.GetProperty("walk"));
            _elevation = ReadRows(map.GetProperty("elevation"));

            foreach (var ramp in map.GetProperty("ramps").EnumerateArray())
                _ramps[V2I(ramp.GetProperty("tile"))] = ramp.GetProperty("rises_toward").GetString();

            foreach (var tile in map.GetProperty("tiles").EnumerateArray())
                PlaceTile(tile);

            foreach (var obj in map.GetProperty("objects").EnumerateArray())
            {
                PlaceObject(obj);
                MarkSolidProp(obj);
            }

            foreach (var blocker in map.GetProperty("blockers").EnumerateArray())
            {
                string id = blocker.GetProperty("id").GetString();

                foreach (var t in blocker.GetProperty("tiles").EnumerateArray())
                    _questTiles[V2I(t)] = id;

                _blockedNodes[id] = new List<Node2D>();
                _clearedNodes[id] = new List<Node2D>();

                foreach (var obj in blocker.GetProperty("objects_while_blocked").EnumerateArray())
                    _blockedNodes[id].Add(PlaceObject(obj));

                foreach (var obj in blocker.GetProperty("objects_after_clear").EnumerateArray())
                    _clearedNodes[id].Add(PlaceObject(obj));

                // Bloqueio de rio: o que aparece depois da quest é a balsa (ver RaftAt).
                if (blocker.TryGetProperty("kind", out var kind) && kind.GetString() == "river" && _clearedNodes[id].Count > 0)
                    _rafts[id] = _clearedNodes[id][0];

                // A mina mora no "bolso" de um bloqueio (o deslizamento) e aparece junto com ele.
                if (blocker.TryGetProperty("pocket", out var pocket) && pocket.ValueKind == JsonValueKind.String &&
                    map.TryGetProperty("mine", out var mine) && mine.GetProperty("id").GetString() == pocket.GetString())
                {
                    MineBlockerId = id;

                    foreach (var obj in mine.GetProperty("objects_after_clear").EnumerateArray())
                    {
                        MineNode = PlaceObject(obj);
                        _clearedNodes[id].Add(MineNode);
                    }
                }
            }

            foreach (var spot in map.GetProperty("spots").EnumerateArray())
                PlaceSpot(spot);

            BuildWalls();

            foreach (var id in _blockedNodes.Keys)
                SetBlockerCleared(id, false);
        }

        // ---------------------------------------------------------------- construção

        private Node2D AddLayer(string name, bool ySort)
        {
            var layer = new Node2D { Name = name, YSortEnabled = ySort };
            AddChild(layer);
            return layer;
        }

        private void PlaceTile(JsonElement t)
        {
            var tile = V2I(t.GetProperty("tile"));
            var pos = V2(t.GetProperty("pos"));
            var entry = Entry(t.GetProperty("sprite").GetString());
            var center = V2(entry.GetProperty("tile_center"));
            bool sorted = t.GetProperty("layer").GetString() == "sorted";
            int start = PreviewMode ? 0 : (tile.X + tile.Y) % 4;

            var holder = new Node2D();
            float lift = 0;

            if (sorted)
            {
                float sortY = t.GetProperty("sort_y").GetSingle();
                holder.Position = new Vector2(pos.X, sortY);
                lift = pos.Y - sortY;
                _blockSortY[tile] = sortY;
                Sorted.AddChild(holder);
            }
            else
            {
                holder.Position = pos;
                _ground.AddChild(holder);
            }

            holder.AddChild(MakeVisual(entry, -center + new Vector2(0, lift), start, 5f));

            foreach (var overlay in t.GetProperty("overlays").EnumerateArray())
            {
                var overlayEntry = Entry(overlay.GetString());
                float dy = overlayEntry.ValueKind == JsonValueKind.Object &&
                           overlayEntry.TryGetProperty("offset_y", out var oy) ? oy.GetSingle() : 0f;

                holder.AddChild(MakeVisual(overlayEntry, -center + new Vector2(0, lift + dy), start, 5f));
            }
        }

        private Node2D PlaceObject(JsonElement o)
        {
            var pos = V2(o.GetProperty("pos"));
            float sortY = o.GetProperty("sort_y").GetSingle();
            var anchor = V2(o.GetProperty("anchor"));
            var tile = V2I(o.GetProperty("tile"));
            string sprite = o.GetProperty("sprite").GetString();
            bool wind = o.TryGetProperty("frame_mode", out var fm) && fm.GetString() == "wind";
            int start = wind ? ((tile.X + tile.Y) / 4) % 4 : 0;

            var node = new Node2D { Position = new Vector2(pos.X, sortY), Name = $"{sprite}_{tile.X}_{tile.Y}" };

            if (o.TryGetProperty("shadow", out var shadowName) && shadowName.ValueKind == JsonValueKind.String)
            {
                var offset = V2(o.GetProperty("shadow_offset"));
                var shadow = MakeVisual(Entry(shadowName.GetString()), Vector2.Zero, 0, 0f);

                if (shadow is Sprite2D shadowSprite)
                    shadowSprite.Centered = true;

                // Objeto no planalto: a sombra iria pra camada Shadows, que fica ABAIXO do bloco
                // alto (Sorted) e sumiria embaixo dele - então ela vai junto do objeto, desenhada
                // logo antes dele (é o que a prévia do pacote faz pra todo objeto).
                if (pos.Y - sortY < -ElevationStepPx / 2f)
                {
                    shadow.Position = new Vector2(offset.X, pos.Y - sortY - 2);
                    node.AddChild(shadow);
                }
                else
                {
                    shadow.Position = new Vector2(pos.X + offset.X, pos.Y - 2);
                    _shadows.AddChild(shadow);
                }
            }

            node.AddChild(MakeVisual(Entry(sprite), -anchor + new Vector2(0, pos.Y - sortY), start, wind ? 3.5f : 4f));
            Sorted.AddChild(node);

            if (o.GetProperty("kind").GetString() == "tree")
                _trees.Add((node, new Vector2(pos.X, sortY)));

            return node;
        }

        private void MarkSolidProp(JsonElement o)
        {
            if (o.TryGetProperty("collides", out var collides) && collides.GetBoolean())
                return;

            string sprite = o.GetProperty("sprite").GetString();

            if (!Array.Exists(SolidProps, prefix => sprite.StartsWith(prefix, StringComparison.Ordinal)))
                return;

            var t = V2I(o.GetProperty("tile"));

            // Só bloco livre comum: rampa, bloqueio de quest e o que já é parede ficam como estão.
            if (!Inside(t) || _walk[t.Y][t.X] != '.')
                return;

            var row = _walk[t.Y].ToCharArray();
            row[t.X] = 'O';
            _walk[t.Y] = new string(row);
        }

        private void PlaceSpot(JsonElement s)
        {
            string id = s.GetProperty("id").GetString();
            _spots[id] = new SpotInfo(id, s.GetProperty("type").GetString(), V2I(s.GetProperty("tile")));

            if (!ShowSpotPlaceholders)
                return;

            var entry = Entry(s.GetProperty("placeholder_sprite").GetString());
            var anchor = entry.TryGetProperty("anchor", out var a) ? V2(a) : Vector2.Zero;
            var pos = V2(s.GetProperty("pos"));
            float sortY = s.GetProperty("sort_y").GetSingle();

            var holder = new Node2D { Position = new Vector2(pos.X, sortY), Name = "spot_" + id };
            holder.AddChild(MakeVisual(entry, -anchor + new Vector2(0, pos.Y - sortY), 0, 4f));
            Sorted.AddChild(holder);
        }

        /// <summary>Sprite de um nome do kit (ex.: "chest", "item_orb"), com o ponto de âncora
        /// na origem - pra a ExplorationArea vestir baús/itens com a arte do kit.</summary>
        public bool HasKitSprite(string name)
        {
            foreach (var section in new[] { "tiles", "terrain", "props", "overlays" })
            {
                if (_manifest.TryGetProperty(section, out var s) && s.TryGetProperty(name, out _))
                    return true;
            }

            return false;
        }

        /// <summary>Deixa um bloco livre sólido depois de montado (baú posto pela
        /// ExplorationArea): vira 'O' na grade e ganha uma parede em volta do losango.</summary>
        public void AddSolidTile(Vector2I t)
        {
            if (!Inside(t) || _walk[t.Y][t.X] != '.')
                return;

            var row = _walk[t.Y].ToCharArray();
            row[t.X] = 'O';
            _walk[t.Y] = new string(row);

            var c = TileToGround(t);
            _walls.AddChild(MakeWallBody($"Solid_{t.X}_{t.Y}", new List<Vector2>
            {
                c + DiamondTop, c + DiamondRight,
                c + DiamondRight, c + DiamondBottom,
                c + DiamondBottom, c + DiamondLeft,
                c + DiamondLeft, c + DiamondTop,
            }));
        }

        public Node2D MakeKitSprite(string name)
        {
            var entry = Entry(name);
            var anchor = entry.ValueKind == JsonValueKind.Object && entry.TryGetProperty("anchor", out var a)
                ? V2(a)
                : Vector2.Zero;

            return MakeVisual(entry, -anchor, 0, 4f);
        }

        private Node2D MakeVisual(JsonElement entry, Vector2 offset, int startFrame, float fps)
        {
            string path;
            int frames = 1;

            if (entry.ValueKind == JsonValueKind.String)
            {
                path = entry.GetString();
            }
            else
            {
                path = entry.TryGetProperty("sheet", out var sheet) ? sheet.GetString() : entry.GetProperty("file").GetString();

                if (entry.TryGetProperty("frames", out var f))
                    frames = f.GetInt32();
            }

            var sprite = new Sprite2D
            {
                Texture = Texture(path),
                Centered = false,
                Offset = offset,
                Hframes = Math.Max(frames, 1),
            };

            if (frames > 1)
            {
                sprite.Frame = startFrame % frames;

                if (fps > 0)
                {
                    if (!_animated.TryGetValue(fps, out var list))
                    {
                        list = new List<(Sprite2D, int)>();
                        _animated[fps] = list;
                        _animatedLastFrame[fps] = 0;
                    }

                    list.Add((sprite, startFrame));
                }
            }

            return sprite;
        }

        private Texture2D Texture(string relativePath)
        {
            if (!_textures.TryGetValue(relativePath, out var texture))
            {
                string overridePath = string.IsNullOrEmpty(KitOverrideDir) ? null : KitOverrideDir + "/" + relativePath;
                string path = overridePath != null && ResourceLoader.Exists(overridePath) ? overridePath : KitDir + "/" + relativePath;
                texture = GD.Load<Texture2D>(path);

                if (texture == null)
                    GD.PrintErr($"MapLoader: textura do kit não encontrada: {path}");

                _textures[relativePath] = texture;
            }

            return texture;
        }

        private JsonElement Entry(string name)
        {
            foreach (var section in new[] { "tiles", "terrain", "props", "overlays" })
            {
                if (_manifest.TryGetProperty(section, out var s) && s.TryGetProperty(name, out var e))
                    return e;
            }

            throw new Exception("MapLoader: sprite não está no kit: " + name);
        }

        // ---------------------------------------------------------------- colisão

        // Paredes geradas da grade: um segmento em cada aresta entre um bloco onde se anda e um
        // vizinho onde não dá pra passar (floresta, água, objeto, borda do mapa, ou outro nível
        // sem rampa - inclui as laterais das rampas). As arestas dos bloqueios de quest ficam
        // num corpo próprio por bloqueio, desligado quando a quest é concluída.
        private void BuildWalls()
        {
            var staticSegments = new List<Vector2>();
            var blockerSegments = new Dictionary<string, List<Vector2>>();

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    var a = new Vector2I(x, y);

                    if (!IsOpenTile(a, treatQuestAsOpen: true))
                        continue;

                    foreach (var dir in NeighbourDirs)
                    {
                        var b = a + dir;
                        bool aIsQuest = _questTiles.TryGetValue(a, out var aBlocker);
                        bool bIsQuest = Inside(b) && _questTiles.TryGetValue(b, out _);

                        bool wall = !Inside(b) || !IsOpenTile(b, treatQuestAsOpen: true) || !StepRule(a, b) || !StepRule(b, a);

                        if (wall)
                        {
                            AddEdge(staticSegments, a, dir);
                            continue;
                        }

                        // Livre dos dois lados: só vira parede (de quest) na fronteira entre um
                        // bloco de bloqueio e um bloco comum - fica do lado do bloqueio.
                        if (aIsQuest && !bIsQuest)
                        {
                            if (!blockerSegments.TryGetValue(aBlocker, out var list))
                            {
                                list = new List<Vector2>();
                                blockerSegments[aBlocker] = list;
                            }

                            AddEdge(list, a, dir);
                        }
                    }
                }
            }

            _walls.AddChild(MakeWallBody("StaticWalls", staticSegments));

            foreach (var (id, segments) in blockerSegments)
            {
                var body = MakeWallBody("Blocker_" + id, segments);
                _walls.AddChild(body);
                _blockerBodies[id] = body;
            }
        }

        private static readonly Vector2I[] NeighbourDirs =
        {
            new(1, 0), new(-1, 0), new(0, 1), new(0, -1),
        };

        // Vértices do losango (centro no chão): topo, direita, baixo, esquerda.
        private static readonly Vector2 DiamondTop = new(0, -16);
        private static readonly Vector2 DiamondRight = new(32, 0);
        private static readonly Vector2 DiamondBottom = new(0, 16);
        private static readonly Vector2 DiamondLeft = new(-32, 0);

        private static void AddEdge(List<Vector2> segments, Vector2I tile, Vector2I dir)
        {
            var c = TileToGround(tile);

            (Vector2 p, Vector2 q) = dir switch
            {
                { X: 1 } => (DiamondRight, DiamondBottom),
                { X: -1 } => (DiamondLeft, DiamondTop),
                { Y: 1 } => (DiamondBottom, DiamondLeft),
                _ => (DiamondTop, DiamondRight),
            };

            segments.Add(c + p);
            segments.Add(c + q);
        }

        private static StaticBody2D MakeWallBody(string name, List<Vector2> segments)
        {
            var body = new StaticBody2D { Name = name, CollisionLayer = 1, CollisionMask = 0 };

            if (segments.Count > 0)
            {
                body.AddChild(new CollisionShape2D
                {
                    Shape = new ConcavePolygonShape2D { Segments = segments.ToArray() },
                });
            }

            return body;
        }

        // ---------------------------------------------------------------- quests

        public bool HasBlocker(string id) => _blockedNodes.ContainsKey(id);

        public IEnumerable<string> BlockerIds => _blockedNodes.Keys;

        public bool IsBlockerCleared(string id) => _clearedBlockers.Contains(id);

        /// <summary>Troca o visual do bloqueio (antes/depois da quest) e abre/fecha a passagem.
        /// Não salva nada: o estado vem das quests concluídas no save (ver ExplorationArea).</summary>
        public void SetBlockerCleared(string id, bool cleared)
        {
            if (cleared)
                _clearedBlockers.Add(id);
            else
                _clearedBlockers.Remove(id);

            if (_blockedNodes.TryGetValue(id, out var blocked))
                foreach (var n in blocked) n.Visible = !cleared;

            if (_clearedNodes.TryGetValue(id, out var clearedNodes))
                foreach (var n in clearedNodes) n.Visible = cleared;

            if (_blockerBodies.TryGetValue(id, out var body))
            {
                body.CollisionLayer = cleared ? 0u : 1u;

                foreach (var child in body.GetChildren())
                {
                    if (child is CollisionShape2D shape)
                        shape.SetDeferred(CollisionShape2D.PropertyName.Disabled, cleared);
                }
            }
        }

        /// <summary>A balsa de um rio liberado, se o bloco é uma das casas do rio que ela cruza
        /// - a ExplorationArea leva a balsa junto com o Digimon durante a travessia.</summary>
        public Node2D RaftAt(Vector2I tile)
        {
            if (_questTiles.TryGetValue(tile, out var id) && _clearedBlockers.Contains(id) && _rafts.TryGetValue(id, out var raft))
                return raft;

            return null;
        }

        // ---------------------------------------------------------------- spots

        public bool TryGetSpot(string id, out SpotInfo spot) => _spots.TryGetValue(id, out spot);

        // ---------------------------------------------------------------- andar
        // (coordenadas locais deste nó, sem a escala; Position das entidades em Sorted)

        public static Vector2I GroundToTile(Vector2 p) =>
            new((int)MathF.Round((p.X / 32f + p.Y / 16f) / 2f), (int)MathF.Round((p.Y / 16f - p.X / 32f) / 2f));

        public static Vector2 TileToGround(Vector2I t) => new((t.X - t.Y) * 32, (t.X + t.Y) * 16);

        public bool Inside(Vector2I t) => t.X >= 0 && t.Y >= 0 && t.X < Width && t.Y < Height;

        public int Elevation(Vector2I t) => Inside(t) ? _elevation[t.Y][t.X] - '0' : 0;

        public bool IsRamp(Vector2I t) => _ramps.ContainsKey(t);

        /// <summary>Dá pra ficar em pé nesse bloco agora (considerando os bloqueios de quest)?</summary>
        public bool IsWalkable(Vector2I t) => IsOpenTile(t, treatQuestAsOpen: false);

        /// <summary>Bloco ocupado por um objeto sólido (arbusto, pedra... ver SolidProps)?</summary>
        public bool IsSolidProp(Vector2I t) => Inside(t) && _walk[t.Y][t.X] == 'O';

        private bool IsOpenTile(Vector2I t, bool treatQuestAsOpen)
        {
            if (!Inside(t))
                return false;

            char c = _walk[t.Y][t.X];

            if (c == '#' || c == 'O')
                return false;

            if (c == 'Q' && !treatQuestAsOpen)
                return _questTiles.TryGetValue(t, out var id) && _clearedBlockers.Contains(id);

            return true;
        }

        /// <summary>Pode ir do bloco 'a' para o vizinho 'b'? Mesmo nível; rampa liga só o lado
        /// baixo (frente) ao alto; bloqueio de quest só depois da quest.</summary>
        public bool CanStep(Vector2I a, Vector2I b) => IsWalkable(b) && StepRule(a, b);

        private bool StepRule(Vector2I a, Vector2I b)
        {
            bool aRamp = _ramps.ContainsKey(a);
            bool bRamp = _ramps.ContainsKey(b);

            if (aRamp || bRamp)
            {
                var r = aRamp ? a : b;
                var other = aRamp ? b : a;
                string dir = _ramps[r];
                var high = dir == "-gx" ? new Vector2I(r.X - 1, r.Y) : new Vector2I(r.X, r.Y - 1);
                var low = dir == "-gx" ? new Vector2I(r.X + 1, r.Y) : new Vector2I(r.X, r.Y + 1);

                return (other == high && Elevation(other) == 1) || (other == low && Elevation(other) == 0);
            }

            return Elevation(a) == Elevation(b);
        }

        /// <summary>"Nível" de um ponto pra saber se dois digimons se enxergam/encostam: rampa
        /// conta como os dois níveis.</summary>
        public bool AreOnSameLevel(Vector2 groundA, Vector2 groundB)
        {
            var a = GroundToTile(groundA);
            var b = GroundToTile(groundB);

            if (IsRamp(a) || IsRamp(b))
                return true;

            return Elevation(a) == Elevation(b);
        }

        /// <summary>Altura visual (px nativos) de um ponto: sobe o desenho do digimon nos
        /// planaltos (18 px) e na rampa (gradual), com a Position continuando no chão.</summary>
        public float VisualLift(Vector2 ground)
        {
            var t = GroundToTile(ground);

            if (_ramps.TryGetValue(t, out var dir))
            {
                var rel = ground - TileToGround(t);

                // Coordenadas dentro do losango: p cresce pra +gx, q pra +gy (0..1).
                float p = rel.X / 64f + rel.Y / 32f + 0.5f;
                float q = -rel.X / 64f + rel.Y / 32f + 0.5f;
                float s = dir == "-gx" ? 1 - p : 1 - q;

                return Mathf.Clamp(s, 0, 1) * ElevationStepPx;
            }

            return Elevation(t) * ElevationStepPx;
        }

        /// <summary>Quanto (px nativos, pra baixo) o desenho de uma entidade em 'ground' precisa
        /// ser ordenado depois da Position pra ficar por cima do chão alto em que pisa. Os blocos
        /// altos e rampas ordenam pelo centro do losango, então quem está na metade de trás do
        /// bloco (ou perto dos vizinhos da frente, no mesmo planalto) ficava atrás do próprio
        /// chão. 0 no chão baixo. Não vai além do necessário: árvores dos blocos da frente
        /// continuam na frente.</summary>
        public float SortLift(Vector2 ground)
        {
            var t = GroundToTile(ground);

            if (!_blockSortY.TryGetValue(t, out float key))
                return 0f;

            // Na rampa, só o próprio bloco: as laterais são paredes mais altas que ela e devem
            // continuar na frente.
            if (Elevation(t) == 1 && !IsRamp(t))
            {
                foreach (var n in new[] { t + new Vector2I(1, 0), t + new Vector2I(0, 1) })
                {
                    if (_blockSortY.TryGetValue(n, out float front) && (Elevation(n) == 1 || IsRamp(n)))
                        key = Mathf.Max(key, front);
                }
            }

            return Mathf.Max(0f, key + 1 - ground.Y);
        }

        /// <summary>Caminho de blocos (4-vizinhos, regras de CanStep) de 'from' até 'to', sem
        /// incluir 'from'. Null se não há caminho.</summary>
        public List<Vector2I> FindPath(Vector2I from, Vector2I to)
        {
            if (!Inside(from) || !IsWalkable(to))
                return null;

            if (from == to)
                return new List<Vector2I>();

            var cameFrom = new Dictionary<Vector2I, Vector2I> { [from] = from };
            var frontier = new PriorityQueue<Vector2I, int>();
            var cost = new Dictionary<Vector2I, int> { [from] = 0 };
            frontier.Enqueue(from, 0);

            while (frontier.Count > 0)
            {
                var current = frontier.Dequeue();

                if (current == to)
                    break;

                foreach (var dir in NeighbourDirs)
                {
                    var next = current + dir;

                    if (!CanStep(current, next))
                        continue;

                    int newCost = cost[current] + 1;

                    if (cost.TryGetValue(next, out int old) && old <= newCost)
                        continue;

                    cost[next] = newCost;
                    cameFrom[next] = current;
                    frontier.Enqueue(next, newCost + Math.Abs(to.X - next.X) + Math.Abs(to.Y - next.Y));
                }
            }

            if (!cameFrom.ContainsKey(to))
                return null;

            var path = new List<Vector2I>();

            for (var t = to; t != from; t = cameFrom[t])
                path.Add(t);

            path.Reverse();
            return path;
        }

        /// <summary>Retângulo (px nativos, no chão) que cobre o mapa inteiro, com folga pra
        /// altura dos blocos - usado nos limites da câmera.</summary>
        public Rect2 GetGroundBounds()
        {
            float left = -(Height - 1) * 32 - 32;
            float right = (Width - 1) * 32 + 32;
            float top = -16 - ElevationStepPx;
            float bottom = (Width - 1 + Height - 1) * 16 + 16;

            return new Rect2(left, top, right - left, bottom - top);
        }

        // ---------------------------------------------------------------- por frame

        public override void _Process(double delta)
        {
            if (PreviewMode)
                return;

            _animationTime += delta;

            foreach (var (fps, list) in _animated)
            {
                int frame = (int)(_animationTime * fps);

                if (frame == _animatedLastFrame[fps])
                    continue;

                _animatedLastFrame[fps] = frame;

                foreach (var (sprite, start) in list)
                    sprite.Frame = (start + frame) % sprite.Hframes;
            }

            UpdateTreeFade((float)delta);
        }

        private void UpdateTreeFade(float delta)
        {
            if (Player == null || !IsInstanceValid(Player))
                return;

            var p = Player.Position;
            float step = delta * TreeFadeSpeed;

            foreach (var (node, ground) in _trees)
            {
                bool behind = p.Y < ground.Y && ground.Y - p.Y < TreeFadeHeight && Mathf.Abs(p.X - ground.X) < TreeFadeHalfWidth;
                float target = behind ? TreeFadeAlpha : 1f;
                var modulate = node.Modulate;

                if (Mathf.IsEqualApprox(modulate.A, target))
                    continue;

                modulate.A = Mathf.MoveToward(modulate.A, target, step);
                node.Modulate = modulate;
            }
        }

        // ---------------------------------------------------------------- json

        private static string[] ReadRows(JsonElement array)
        {
            var rows = new List<string>();

            foreach (var row in array.EnumerateArray())
                rows.Add(row.GetString());

            return rows.ToArray();
        }

        private static Vector2 V2(JsonElement a) => new(a[0].GetSingle(), a[1].GetSingle());

        private static Vector2I V2I(JsonElement a) => new(a[0].GetInt32(), a[1].GetInt32());
    }
}
