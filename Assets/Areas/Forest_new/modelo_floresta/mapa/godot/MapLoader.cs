// MapLoader.cs — REFERÊNCIA para o Claude Code (Godot 4, C#). Escrito fora do Godot: NÃO foi compilado nem testado.
// Monta o mapa a partir de mapa_floresta.json + kit/manifest.json e expõe a regra de andar (blocos, rampas, bloqueios).
// Adapte nomes de pastas, o sistema de quests e o controle do player ao que já existe no projeto.
using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

public partial class MapLoader : Node2D
{
    [Export(PropertyHint.File, "*.json")] public string MapJson = "res://maps/floresta/mapa/mapa_floresta.json";
    [Export(PropertyHint.Dir)] public string KitDir = "res://maps/floresta/kit";
    [Export] public Godot.Collections.Array<string> ClearedBlockers = new();   // ids de bloqueios já liberados (vem do save)
    [Export] public bool ShowSpotPlaceholders = true;
    [Export] public NodePath PlayerPath;                                          // opcional: para a transparência das árvores

    public const int ElevationStepPx = 18;

    Node2D _ground, _shadows, _sorted, _spots;
    JsonElement _man;
    string[] _walk = Array.Empty<string>();
    string[] _elev = Array.Empty<string>();
    int _w, _h;
    readonly Dictionary<string, Texture2D> _tex = new();
    readonly Dictionary<string, SpriteFrames> _frames = new();
    readonly Dictionary<Vector2I, string> _ramps = new();
    readonly Dictionary<Vector2I, string> _questTiles = new();                   // tile -> id do bloqueio
    readonly Dictionary<string, List<Node2D>> _blockedNodes = new(), _clearedNodes = new();
    readonly List<(Node2D node, Vector2 ground)> _trees = new();
    Node2D _player;

    public override void _Ready()
    {
        TextureFilter = TextureFilterEnum.Nearest;
        Scale = new Vector2(2, 2);                       // mundo em 2x (digimons em 3x ficam no próprio sprite deles)
        _ground = AddLayer("Ground", false);             // blocos no chão, na ordem do JSON (já vem ordenado)
        _shadows = AddLayer("Shadows", false);           // sombras sob árvores/props
        _sorted = AddLayer("Sorted", true);              // Y-sort: blocos altos, rampas, objetos, player, NPCs
        _spots = AddLayer("Spots", false);

        _man = JsonDocument.Parse(FileAccess.GetFileAsString(KitDir + "/manifest.json")).RootElement.Clone();
        var map = JsonDocument.Parse(FileAccess.GetFileAsString(MapJson)).RootElement.Clone();

        var size = map.GetProperty("size_tiles");
        _w = size[0].GetInt32(); _h = size[1].GetInt32();
        _walk = ReadRows(map.GetProperty("walk"));
        _elev = ReadRows(map.GetProperty("elevation"));
        foreach (var r in map.GetProperty("ramps").EnumerateArray())
            _ramps[V2I(r.GetProperty("tile"))] = r.GetProperty("rises_toward").GetString();

        foreach (var t in map.GetProperty("tiles").EnumerateArray()) PlaceTile(t);
        foreach (var o in map.GetProperty("objects").EnumerateArray()) PlaceObject(o);

        foreach (var b in map.GetProperty("blockers").EnumerateArray())
        {
            string id = b.GetProperty("id").GetString();
            foreach (var c in b.GetProperty("tiles").EnumerateArray()) _questTiles[V2I(c)] = id;
            _blockedNodes[id] = new(); _clearedNodes[id] = new();
            foreach (var o in b.GetProperty("objects_while_blocked").EnumerateArray()) _blockedNodes[id].Add(PlaceObject(o));
            foreach (var o in b.GetProperty("objects_after_clear").EnumerateArray()) _clearedNodes[id].Add(PlaceObject(o));
        }
        // a mina aparece quando o deslizamento é liberado (q_escavador)
        foreach (var o in map.GetProperty("mine").GetProperty("objects_after_clear").EnumerateArray())
            _clearedNodes["bloqueio_deslizamento"].Add(PlaceObject(o));

        foreach (var s in map.GetProperty("spots").EnumerateArray()) PlaceSpot(s);
        foreach (var id in _blockedNodes.Keys) SetBlockerCleared(id, ClearedBlockers.Contains(id));
        if (PlayerPath != null && !PlayerPath.IsEmpty) _player = GetNode<Node2D>(PlayerPath);
    }

    // ---------------------------------------------------------------- construção
    Node2D AddLayer(string name, bool ysort)
    {
        var n = new Node2D { Name = name, YSortEnabled = ysort };
        AddChild(n); return n;
    }

    void PlaceTile(JsonElement t)
    {
        var tile = V2I(t.GetProperty("tile"));
        var pos = V2(t.GetProperty("pos"));
        string sprite = t.GetProperty("sprite").GetString();
        bool sorted = t.GetProperty("layer").GetString() == "sorted";
        var entry = Entry(sprite);
        var center = V2(entry.GetProperty("tile_center"));
        int start = (tile.X + tile.Y) % 4;

        var holder = new Node2D();
        float lift = 0;
        if (sorted)
        {
            float sortY = t.GetProperty("sort_y").GetSingle();
            holder.Position = new Vector2(pos.X, sortY);
            lift = pos.Y - sortY;                        // desenha mais alto, mas ordena pelo chão
            _sorted.AddChild(holder);
        }
        else { holder.Position = pos; _ground.AddChild(holder); }

        holder.AddChild(MakeVisual(entry, -center + new Vector2(0, lift), start, 5f));
        foreach (var o in t.GetProperty("overlays").EnumerateArray())
        {
            var oe = Entry(o.GetString());
            float dy = oe.ValueKind == JsonValueKind.Object && oe.TryGetProperty("offset_y", out var oy) ? oy.GetSingle() : 0f;
            holder.AddChild(MakeVisual(oe, -center + new Vector2(0, lift + dy), start, 5f));
        }
    }

    Node2D PlaceObject(JsonElement o)
    {
        var pos = V2(o.GetProperty("pos"));
        float sortY = o.GetProperty("sort_y").GetSingle();
        var anchor = V2(o.GetProperty("anchor"));
        var tile = V2I(o.GetProperty("tile"));
        string sprite = o.GetProperty("sprite").GetString();
        bool wind = o.TryGetProperty("frame_mode", out var fm) && fm.GetString() == "wind";
        int start = wind ? ((tile.X + tile.Y) / 4) % 4 : 0;

        var node = new Node2D { Position = new Vector2(pos.X, sortY), Name = sprite + "_" + tile.X + "_" + tile.Y };
        node.AddChild(MakeVisual(Entry(sprite), -anchor + new Vector2(0, pos.Y - sortY), start, wind ? 3.5f : 4f));
        _sorted.AddChild(node);

        if (o.TryGetProperty("shadow", out var sh) && sh.ValueKind == JsonValueKind.String)
        {
            var off = V2(o.GetProperty("shadow_offset"));
            var s = MakeVisual(Entry(sh.GetString()), Vector2.Zero, 0, 0f);
            if (s is Sprite2D sp) sp.Centered = true;
            s.Position = new Vector2(pos.X + off.X, pos.Y - 2);
            _shadows.AddChild(s);
        }
        if (o.GetProperty("kind").GetString() == "tree") _trees.Add((node, new Vector2(pos.X, sortY)));
        return node;
    }

    void PlaceSpot(JsonElement s)
    {
        var m = new Marker2D { Name = s.GetProperty("id").GetString(), Position = V2(s.GetProperty("pos")) };
        m.SetMeta("type", s.GetProperty("type").GetString());
        m.SetMeta("note", s.GetProperty("note").GetString());
        m.SetMeta("tile", V2I(s.GetProperty("tile")));
        _spots.AddChild(m);
        // O jogo troca o placeholder pelo NPC/inimigo/baú de verdade. Para testar, mostramos o ícone.
        if (ShowSpotPlaceholders)
        {
            string ph = s.GetProperty("placeholder_sprite").GetString();
            var e = Entry(ph);
            var anchor = e.TryGetProperty("anchor", out var a) ? V2(a) : Vector2.Zero;
            var holder = new Node2D { Position = new Vector2(m.Position.X, s.GetProperty("sort_y").GetSingle()) };
            holder.AddChild(MakeVisual(e, -anchor + new Vector2(0, m.Position.Y - holder.Position.Y), 0, 4f));
            _sorted.AddChild(holder);
        }
    }

    Node2D MakeVisual(JsonElement entry, Vector2 offset, int startFrame, float fps)
    {
        string path; int frames = 1;
        if (entry.ValueKind == JsonValueKind.String) path = entry.GetString();
        else
        {
            path = entry.TryGetProperty("sheet", out var sh) ? sh.GetString() : entry.GetProperty("file").GetString();
            if (entry.TryGetProperty("frames", out var f)) frames = f.GetInt32();
        }
        var tex = Tex(path);
        if (frames <= 1) return new Sprite2D { Texture = tex, Centered = false, Offset = offset };
        string key = path + "|" + fps;
        if (!_frames.TryGetValue(key, out var sf))
        {
            sf = new SpriteFrames();
            sf.SetAnimationSpeed("default", fps);
            sf.SetAnimationLoop("default", true);
            int fw = tex.GetWidth() / frames;
            for (int i = 0; i < frames; i++)
                sf.AddFrame("default", new AtlasTexture { Atlas = tex, Region = new Rect2(i * fw, 0, fw, tex.GetHeight()) });
            _frames[key] = sf;
        }
        var a = new AnimatedSprite2D { SpriteFrames = sf, Centered = false, Offset = offset };
        a.Play("default");
        a.Frame = startFrame % frames;
        return a;
    }

    Texture2D Tex(string rel)
    {
        if (!_tex.TryGetValue(rel, out var t)) { t = GD.Load<Texture2D>(KitDir + "/" + rel); _tex[rel] = t; }
        return t;
    }

    JsonElement Entry(string name)
    {
        foreach (var sec in new[] { "tiles", "terrain", "props", "overlays" })
            if (_man.TryGetProperty(sec, out var s) && s.TryGetProperty(name, out var e)) return e;
        throw new Exception("sprite não está no kit: " + name);
    }

    // ---------------------------------------------------------------- quests
    public void SetBlockerCleared(string id, bool cleared)
    {
        if (cleared && !ClearedBlockers.Contains(id)) ClearedBlockers.Add(id);
        if (!cleared) ClearedBlockers.Remove(id);
        if (_blockedNodes.TryGetValue(id, out var bn)) foreach (var n in bn) n.Visible = !cleared;
        if (_clearedNodes.TryGetValue(id, out var cn)) foreach (var n in cn) n.Visible = cleared;
    }

    // ---------------------------------------------------------------- andar (coordenadas locais deste nó, sem a escala)
    public static Vector2I GroundToTile(Vector2 p) =>
        new((int)MathF.Round((p.X / 32f + p.Y / 16f) / 2f), (int)MathF.Round((p.Y / 16f - p.X / 32f) / 2f));
    public static Vector2 TileToGround(Vector2I t) => new((t.X - t.Y) * 32, (t.X + t.Y) * 16);
    public int Elevation(Vector2I t) => Inside(t) ? _elev[t.Y][t.X] - '0' : 0;
    bool Inside(Vector2I t) => t.X >= 0 && t.Y >= 0 && t.X < _w && t.Y < _h;

    /// Pode ir do bloco 'a' para o vizinho 'b'? (mesmo nível; rampas ligam só o lado baixo ao alto; bloqueios só depois da quest)
    public bool CanStep(Vector2I a, Vector2I b)
    {
        if (!Inside(b)) return false;
        char c = _walk[b.Y][b.X];
        if (c == '#' || c == 'O') return false;
        if (c == 'Q' && !(_questTiles.TryGetValue(b, out var id) && ClearedBlockers.Contains(id))) return false;
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

    /// Altura visual (px nativos) de um ponto: use para desenhar o digimon mais alto sobre os planaltos/rampas.
    public float VisualLift(Vector2 ground)
    {
        var t = GroundToTile(ground);
        if (_ramps.TryGetValue(t, out var d))
        {
            var c = TileToGround(t); var rel = ground - c;
            float p = rel.X / 64f + rel.Y / 32f + .5f, q = -rel.X / 64f + rel.Y / 32f + .5f;   // 0..1 dentro do losango
            float s = d == "-gx" ? 1 - p : 1 - q;
            return Mathf.Clamp(s, 0, 1) * ElevationStepPx;
        }
        return Elevation(t) * ElevationStepPx;
    }

    // ---------------------------------------------------------------- árvores ficam translúcidas quando um digimon passa atrás
    public override void _Process(double delta)
    {
        if (_player == null) return;
        var p = _player.Position;    // o player deve ser filho de Sorted, com Position = posição no chão
        float step = (float)delta * 6f;
        foreach (var (node, g) in _trees)
        {
            bool behind = p.Y < g.Y && g.Y - p.Y < 82 && Mathf.Abs(p.X - g.X) < 36;
            float target = behind ? .45f : 1f;
            var m = node.Modulate;
            if (!Mathf.IsEqualApprox(m.A, target)) { m.A = Mathf.MoveToward(m.A, target, step); node.Modulate = m; }
        }
    }

    // ---------------------------------------------------------------- json helpers
    static string[] ReadRows(JsonElement arr)
    {
        var rows = new List<string>();
        foreach (var r in arr.EnumerateArray()) rows.Add(r.GetString());
        return rows.ToArray();
    }
    static Vector2 V2(JsonElement a) => new(a[0].GetSingle(), a[1].GetSingle());
    static Vector2I V2I(JsonElement a) => new(a[0].GetInt32(), a[1].GetInt32());
}
