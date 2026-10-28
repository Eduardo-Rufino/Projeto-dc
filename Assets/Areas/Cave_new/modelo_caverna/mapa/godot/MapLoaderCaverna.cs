// MapLoaderCaverna.cs — REFERÊNCIA para o Claude Code (Godot 4, C#). Escrito fora do Godot: NÃO foi compilado nem testado.
// É o MapLoader da floresta com o que a caverna acrescenta: camada de decalques, sobreposições com âncora própria,
// objetos que trocam de sprite (apagado -> aceso), luzes fixas, a luz do digimon (raio de visão) e o brilho (glow).
// Se o projeto já tem um MapLoader genérico vindo da floresta, o melhor é juntar estas partes nele.
using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

public partial class MapLoaderCaverna : Node2D
{
    [Export(PropertyHint.File, "*.json")] public string MapJson = "res://maps/caverna/mapa/mapa_caverna.json";
    [Export(PropertyHint.Dir)] public string KitDir = "res://maps/caverna/kit";
    [Export] public Godot.Collections.Array<string> ClearedBlockers = new();   // bloqueios liberados (save)
    [Export] public Godot.Collections.Array<string> DoneQuests = new();        // quests concluídas (save)
    [Export] public Godot.Collections.Array<string> SwitchedOn = new();        // lanternas acesas / caixas ligadas (save)
    [Export] public bool ShowSpotPlaceholders = true;
    [Export] public NodePath PlayerPath;

    public const int ElevationStepPx = 18;
    const int GlowLayerBit = 2;                       // light_mask dos sprites de brilho (bit 2)

    Node2D _ground, _decals, _sorted, _spots, _lightsRoot;
    JsonElement _man, _vision;
    string[] _walk = Array.Empty<string>(), _elev = Array.Empty<string>();
    int _w, _h;
    readonly Dictionary<string, Texture2D> _tex = new();
    readonly Dictionary<string, SpriteFrames> _frames = new();
    readonly Dictionary<Vector2I, string> _ramps = new();
    readonly Dictionary<Vector2I, string> _questTiles = new();
    readonly Dictionary<string, List<Node2D>> _blockedNodes = new(), _clearedNodes = new();
    readonly Dictionary<string, string> _blockerQuest = new();                   // id do bloqueio -> quest que libera
    readonly Dictionary<string, (Node2D node, string after, string rule, JsonElement o)> _switch = new();
    readonly List<(PointLight2D light, string rule, bool flicker, float energy)> _lights = new();
    PointLight2D _playerLight;
    Node2D _player;
    float _flickT;

    public override void _Ready()
    {
        TextureFilter = TextureFilterEnum.Nearest;
        Scale = new Vector2(2, 2);
        _ground = AddLayer("Ground", false);           // blocos no nível do chão + sobreposições (trilhos, espuma)
        _decals = AddLayer("Decals", false);           // poças, cabos, pedrinhas, ponte de dados (planos no chão)
        _sorted = AddLayer("Sorted", true);            // Y-sort: paredes, patamares, escadas, objetos, player, NPCs
        _spots = AddLayer("Spots", false);
        _lightsRoot = AddLayer("Lights", false);

        _man = JsonDocument.Parse(FileAccess.GetFileAsString(KitDir + "/manifest.json")).RootElement.Clone();
        var map = JsonDocument.Parse(FileAccess.GetFileAsString(MapJson)).RootElement.Clone();
        _vision = map.GetProperty("vision");

        var size = map.GetProperty("size_tiles");
        _w = size[0].GetInt32(); _h = size[1].GetInt32();
        _walk = ReadRows(map.GetProperty("walk"));
        _elev = ReadRows(map.GetProperty("elevation"));
        foreach (var r in map.GetProperty("ramps").EnumerateArray()) _ramps[V2I(r.GetProperty("tile"))] = r.GetProperty("rises_toward").GetString();

        foreach (var t in map.GetProperty("tiles").EnumerateArray()) PlaceTile(t);
        foreach (var o in map.GetProperty("objects").EnumerateArray()) PlaceObject(o);
        foreach (var b in map.GetProperty("blockers").EnumerateArray())
        {
            string id = b.GetProperty("id").GetString();
            foreach (var c in b.GetProperty("tiles").EnumerateArray()) _questTiles[V2I(c)] = id;
            _blockedNodes[id] = new(); _clearedNodes[id] = new();
            _blockerQuest[id] = b.GetProperty("quest").GetString();
            foreach (var o in b.GetProperty("objects_while_blocked").EnumerateArray()) _blockedNodes[id].Add(PlaceObject(o));
            foreach (var o in b.GetProperty("objects_after_clear").EnumerateArray()) _clearedNodes[id].Add(PlaceObject(o));
        }
        foreach (var s in map.GetProperty("spots").EnumerateArray()) PlaceSpot(s);
        SetupDarkness(map);

        foreach (var id in _blockedNodes.Keys) SetBlockerCleared(id, ClearedBlockers.Contains(id));
        foreach (var id in SwitchedOn) SwitchOn(id);
        foreach (var q in DoneQuests) OnQuestCompleted(q);
        if (PlayerPath != null && !PlayerPath.IsEmpty) _player = GetNode<Node2D>(PlayerPath);
        SetVisionState("sem_bonus");
    }

    Node2D AddLayer(string name, bool ysort) { var n = new Node2D { Name = name, YSortEnabled = ysort }; AddChild(n); return n; }

    // ---------------------------------------------------------------- blocos
    void PlaceTile(JsonElement t)
    {
        var tile = V2I(t.GetProperty("tile"));
        var pos = V2(t.GetProperty("pos"));
        var entry = Entry(t.GetProperty("sprite").GetString());
        var center = V2(entry.GetProperty("tile_center"));
        bool sorted = t.GetProperty("layer").GetString() == "sorted";
        int start = (tile.X + tile.Y) % 4;
        var holder = new Node2D();
        float lift = 0;
        if (sorted)
        {
            float sortY = t.GetProperty("sort_y").GetSingle();
            holder.Position = new Vector2(pos.X, sortY); lift = pos.Y - sortY;
            _sorted.AddChild(holder);
        }
        else { holder.Position = pos; _ground.AddChild(holder); }
        holder.AddChild(MakeVisual(entry, -center + new Vector2(0, lift), start, 5f));
        foreach (var o in t.GetProperty("overlays").EnumerateArray())
        {
            var oe = Entry(o.GetString());
            var anchor = V2(oe.GetProperty("anchor"));                     // sobreposição da caverna: âncora própria
            AddWithGlow(holder, oe, -anchor + new Vector2(0, lift), start, 4f);
        }
    }

    // ---------------------------------------------------------------- objetos
    Node2D PlaceObject(JsonElement o)
    {
        var pos = V2(o.GetProperty("pos"));
        float sortY = o.GetProperty("sort_y").GetSingle();
        var anchor = V2(o.GetProperty("anchor"));
        var tile = V2I(o.GetProperty("tile"));
        string sprite = o.GetProperty("sprite").GetString();
        bool decal = o.TryGetProperty("layer", out var ly) && ly.GetString() == "decal";
        int start = (tile.X + tile.Y) % 4;
        var node = new Node2D { Name = sprite + "_" + tile.X + "_" + tile.Y };
        if (decal) { node.Position = pos; _decals.AddChild(node); AddWithGlow(node, Entry(sprite), -anchor, start, 4f); }
        else { node.Position = new Vector2(pos.X, sortY); _sorted.AddChild(node); AddWithGlow(node, Entry(sprite), -anchor + new Vector2(0, pos.Y - sortY), start, 4f); }

        string kind = o.GetProperty("kind").GetString();
        if (kind == "switchable")
        {
            string id = o.GetProperty("id").GetString();
            _switch[id] = (node, o.GetProperty("sprite_after").GetString(), o.GetProperty("switch_on").GetString(), o);
        }
        return node;
    }

    /// sprite + (se o kit tiver) o sprite de brilho por cima, com o mesmo offset e quadro
    void AddWithGlow(Node2D parent, JsonElement entry, Vector2 offset, int start, float fps)
    {
        parent.AddChild(MakeVisual(entry, offset, start, fps));
        if (entry.ValueKind == JsonValueKind.Object && entry.TryGetProperty("glow", out var g))
        {
            var glow = MakeVisual(entry, offset, start, fps, g.GetString());
            glow.Name = "Glow";
            glow.LightMask = 1 << (GlowLayerBit - 1);                  // só a GlowLight (abaixo) acende estes pixels
            parent.AddChild(glow);
        }
    }

    void PlaceSpot(JsonElement s)
    {
        var m = new Marker2D { Name = s.GetProperty("id").GetString(), Position = V2(s.GetProperty("pos")) };
        m.SetMeta("type", s.GetProperty("type").GetString());
        m.SetMeta("note", s.GetProperty("note").GetString());
        m.SetMeta("tile", V2I(s.GetProperty("tile")));
        _spots.AddChild(m);
        if (!ShowSpotPlaceholders || s.GetProperty("placeholder_sprite").ValueKind != JsonValueKind.String) return;
        var e = Entry(s.GetProperty("placeholder_sprite").GetString());
        var anchor = V2(e.GetProperty("anchor"));
        var holder = new Node2D { Position = new Vector2(m.Position.X, s.GetProperty("sort_y").GetSingle()) };
        AddWithGlow(holder, e, -anchor + new Vector2(0, m.Position.Y - holder.Position.Y), 0, 4f);
        _sorted.AddChild(holder);
    }

    // ---------------------------------------------------------------- o escuro
    void SetupDarkness(JsonElement map)
    {
        var amb = _vision.GetProperty("ambient_color");
        // CanvasModulate escurece o canvas inteiro desta cena (só pode haver um por canvas)
        AddChild(new CanvasModulate { Name = "Escuro", Color = new Color(amb[0].GetSingle(), amb[1].GetSingle(), amb[2].GetSingle()) });
        // GlowLight: acende SÓ os sprites de brilho (light_mask bit 2), deixando-os com a cor cheia no escuro
        AddChild(new DirectionalLight2D { Name = "GlowLight", Energy = 1f, Color = Colors.White, RangeItemCullMask = 1 << (GlowLayerBit - 1), BlendMode = Light2D.BlendModeEnum.Add });

        foreach (var L in map.GetProperty("lights").EnumerateArray())
        {
            var col = L.GetProperty("color");
            var light = new PointLight2D
            {
                Name = L.GetProperty("id").GetString(),
                Texture = Tex("luz/" + L.GetProperty("texture").GetString() + ".png"),
                TextureScale = 1f,                                   // a textura já tem o tamanho certo em px do mundo
                Color = new Color(col[0].GetSingle(), col[1].GetSingle(), col[2].GetSingle()),
                Energy = L.GetProperty("energy").GetSingle(),
                Position = V2(L.GetProperty("pos")),
                RangeItemCullMask = 1,                               // não precisa acender o brilho (ele já é cheio)
                ShadowEnabled = false,
            };
            _lightsRoot.AddChild(light);
            string rule = L.GetProperty("enabled").GetString();
            light.Enabled = rule == "always";
            _lights.Add((light, rule, L.GetProperty("flicker").GetBoolean(), light.Energy));
        }
        _playerLight = new PointLight2D { Name = "LuzDoDigimon", RangeItemCullMask = 1, ShadowEnabled = false };
        _lightsRoot.AddChild(_playerLight);
    }

    /// estado = "sem_bonus" | "fogo" | "lanterna" | "fogo_lanterna". O jogo decide pelo time (digimon de fogo) e pela q_lanterna.
    public void SetVisionState(string state)
    {
        var st = _vision.GetProperty("player_light").GetProperty("states").GetProperty(state);
        var col = st.GetProperty("color");
        var newTex = Tex("luz/" + st.GetProperty("texture").GetString() + ".png");
        float oldW = _playerLight.Texture != null ? _playerLight.Texture.GetWidth() * _playerLight.TextureScale : newTex.GetWidth();
        _playerLight.Texture = newTex;
        _playerLight.Color = new Color(col[0].GetSingle(), col[1].GetSingle(), col[2].GetSingle());
        _playerLight.Energy = st.GetProperty("energy").GetSingle();
        _playerLight.TextureScale = oldW / newTex.GetWidth();          // começa do tamanho antigo e cresce
        CreateTween().TweenProperty(_playerLight, "texture_scale", 1f, 0.6f);
    }
    public static string VisionStateFor(bool hasFireDigimon, bool hasLantern) =>
        hasFireDigimon && hasLantern ? "fogo_lanterna" : hasLantern ? "lanterna" : hasFireDigimon ? "fogo" : "sem_bonus";

    // ---------------------------------------------------------------- quests, interagíveis e bloqueios
    public void OnQuestCompleted(string questId)
    {
        if (!DoneQuests.Contains(questId)) DoneQuests.Add(questId);
        foreach (var kv in _blockerQuest) if (kv.Value == questId) SetBlockerCleared(kv.Key, true);
        foreach (var (light, rule, _, _) in _lights) if (rule == "after:" + questId) light.Enabled = true;
        foreach (var kv in _switch) if (kv.Value.rule == "quest:" + questId) SwitchOn(kv.Key);
    }

    /// acender uma lanterna apagada / ligar uma caixa de junção (o jogo chama ao interagir com o spot de mesmo id)
    public void SwitchOn(string id)
    {
        if (!_switch.TryGetValue(id, out var s)) return;
        if (!SwitchedOn.Contains(id)) SwitchedOn.Add(id);
        foreach (var c in s.node.GetChildren()) ((Node)c).QueueFree();
        var anchor = V2(Entry(s.after).GetProperty("anchor"));
        var pos = V2(s.o.GetProperty("pos")); float sortY = s.o.GetProperty("sort_y").GetSingle();
        var tile = V2I(s.o.GetProperty("tile"));
        AddWithGlow(s.node, Entry(s.after), -anchor + new Vector2(0, pos.Y - sortY), (tile.X + tile.Y) % 4, 4f);
        foreach (var (light, rule, _, _) in _lights) if (rule == "when_on:" + id) light.Enabled = true;
    }

    public void SetBlockerCleared(string id, bool cleared)
    {
        if (cleared && !ClearedBlockers.Contains(id)) ClearedBlockers.Add(id);
        if (!cleared) ClearedBlockers.Remove(id);
        if (_blockedNodes.TryGetValue(id, out var bn)) foreach (var n in bn) n.Visible = !cleared;
        if (_clearedNodes.TryGetValue(id, out var cn)) foreach (var n in cn) n.Visible = cleared;
    }

    // ---------------------------------------------------------------- andar
    public static Vector2I GroundToTile(Vector2 p) =>
        new((int)MathF.Round((p.X / 32f + p.Y / 16f) / 2f), (int)MathF.Round((p.Y / 16f - p.X / 32f) / 2f));
    public static Vector2 TileToGround(Vector2I t) => new((t.X - t.Y) * 32, (t.X + t.Y) * 16);
    public int Elevation(Vector2I t) => Inside(t) ? _elev[t.Y][t.X] - '0' : 0;
    bool Inside(Vector2I t) => t.X >= 0 && t.Y >= 0 && t.X < _w && t.Y < _h;

    public bool CanStep(Vector2I a, Vector2I b)
    {
        if (!Inside(b)) return false;
        char c = _walk[b.Y][b.X];
        if (c == '#' || c == 'O') return false;
        if (c == 'Q' && !(_questTiles.TryGetValue(b, out var id) && ClearedBlockers.Contains(id))) return false;   // inclui a ponte sobre o abismo
        bool ra = _ramps.ContainsKey(a), rb = _ramps.ContainsKey(b);
        if (ra || rb)
        {
            var r = ra ? a : b; var o = ra ? b : a; string d = _ramps[r];
            var hi = d == "-gx" ? new Vector2I(r.X - 1, r.Y) : new Vector2I(r.X, r.Y - 1);
            var lo = d == "-gx" ? new Vector2I(r.X + 1, r.Y) : new Vector2I(r.X, r.Y + 1);
            return (o == hi && Elevation(o) == 1) || (o == lo && Elevation(o) == 0);
        }
        return Elevation(a) == Elevation(b);
    }

    public float VisualLift(Vector2 ground)
    {
        var t = GroundToTile(ground);
        if (_ramps.TryGetValue(t, out var d))
        {
            var c = TileToGround(t); var rel = ground - c;
            float p = rel.X / 64f + rel.Y / 32f + .5f, q = -rel.X / 64f + rel.Y / 32f + .5f;
            return Mathf.Clamp(d == "-gx" ? 1 - p : 1 - q, 0, 1) * ElevationStepPx;
        }
        return Elevation(t) * ElevationStepPx;
    }

    public override void _Process(double delta)
    {
        if (_player != null) _playerLight.Position = _player.Position + new Vector2(0, -8 - VisualLift(_player.Position));
        _flickT += (float)delta;
        int f = (int)(_flickT * 6) % 4;
        float k = f == 1 ? .92f : f == 3 ? .86f : 1f;                 // tremulação das lanternas, igual à prévia
        foreach (var (light, _, flicker, energy) in _lights) if (flicker) light.Energy = energy * k;
    }

    // ---------------------------------------------------------------- utilitários
    Node2D MakeVisual(JsonElement entry, Vector2 offset, int startFrame, float fps, string overridePath = null)
    {
        string path; int frames = 1;
        if (entry.ValueKind == JsonValueKind.String) path = entry.GetString();
        else
        {
            path = entry.TryGetProperty("sheet", out var sh) ? sh.GetString() : entry.GetProperty("file").GetString();
            if (entry.TryGetProperty("frames", out var fr)) frames = fr.GetInt32();
        }
        if (overridePath != null) path = overridePath;
        var tex = Tex(path);
        if (frames <= 1) return new Sprite2D { Texture = tex, Centered = false, Offset = offset };
        string key = path + "|" + fps;
        if (!_frames.TryGetValue(key, out var sf))
        {
            sf = new SpriteFrames(); sf.SetAnimationSpeed("default", fps); sf.SetAnimationLoop("default", true);
            int fw = tex.GetWidth() / frames;
            for (int i = 0; i < frames; i++) sf.AddFrame("default", new AtlasTexture { Atlas = tex, Region = new Rect2(i * fw, 0, fw, tex.GetHeight()) });
            _frames[key] = sf;
        }
        var a = new AnimatedSprite2D { SpriteFrames = sf, Centered = false, Offset = offset };
        a.Play("default"); a.Frame = startFrame % frames;
        return a;
    }
    Texture2D Tex(string rel) { if (!_tex.TryGetValue(rel, out var t)) { t = GD.Load<Texture2D>(KitDir + "/" + rel); _tex[rel] = t; } return t; }
    JsonElement Entry(string name)
    {
        foreach (var sec in new[] { "tiles", "props", "overlays" })
            if (_man.TryGetProperty(sec, out var s) && s.TryGetProperty(name, out var e)) return e;
        throw new Exception("sprite não está no kit: " + name);
    }
    static string[] ReadRows(JsonElement arr) { var rows = new List<string>(); foreach (var r in arr.EnumerateArray()) rows.Add(r.GetString()); return rows.ToArray(); }
    static Vector2 V2(JsonElement a) => new(a[0].GetSingle(), a[1].GetSingle());
    static Vector2I V2I(JsonElement a) => new(a[0].GetInt32(), a[1].GetInt32());
}
