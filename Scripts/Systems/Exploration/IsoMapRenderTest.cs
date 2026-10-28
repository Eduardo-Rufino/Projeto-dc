using Godot;
using ProjetoDC.Scripts.Gameplay;
using System.Collections.Generic;
using System.Text.Json;

namespace ProjetoDC.Scripts.Systems.Exploration
{
    /// <summary>
    /// Ferramenta de conferência do mapa isométrico (cena Scenes/Audit/IsoMapRender.tscn): monta
    /// o mapa parado no quadro da prévia (MapLoader.PreviewMode), renderiza numa SubViewport e
    /// salva o PNG em user://, no mesmo enquadramento da ferramenta montar_previa.py do pacote.
    /// Depois é só comparar as duas imagens. Fecha sozinha.
    ///
    /// Variáveis:
    /// - DEBUG_ISO_MAP: cena do mapa (padrão: a floresta).
    /// - DEBUG_ISO_STATE: blocked/cleared - "cleared" libera todos os bloqueios e, nos mapas com
    ///   quests de objetos (caverna), conclui as quests e acende/liga tudo (= "depois" da prévia).
    /// - sem mais nada: mapa inteiro em 1x, sem o escuro -> user://iso_render_{estado}.png
    ///   (recortado até o conteúdo).
    /// - DEBUG_ISO_ZOOM=gx,gy: recorte 2x de 1920x1080 em volta do bloco, sem o escuro, igual a
    ///   "montar_previa.py antes|depois zoom GX GY" -> user://iso_zoom_{estado}_{gx}_{gy}.png
    /// - DEBUG_ISO_DARK=gx,gy,estado_visão: o mesmo recorte COM o escuro e a luz do digimon no
    ///   bloco, igual a "montar_previa.py antes|depois escuro GX GY ESTADO"
    ///   -> user://iso_dark_{estado}_{gx}_{gy}_{visão}.png
    /// </summary>
    public partial class IsoMapRenderTest : Node
    {
        // Recortes da prévia: 960x540 px nativos ampliados 2x.
        private const int CropWidth = 960;
        private const int CropHeight = 540;

        // Fundo da prévia (fora do mapa), escurecido junto no modo escuro.
        private static readonly Color PreviewBackground = Color.Color8(12, 10, 18);

        private SubViewport _viewport;
        private string _state;
        private string _output;
        private bool _fullMap;
        private int _framesWaited;

        public override void _Ready()
        {
            string scenePath = OS.GetEnvironment("DEBUG_ISO_MAP");
            if (string.IsNullOrEmpty(scenePath))
                scenePath = "res://Assets/Areas/Forest_new/Floresta.tscn";

            _state = OS.GetEnvironment("DEBUG_ISO_STATE");
            if (string.IsNullOrEmpty(_state))
                _state = "blocked";

            string zoom = OS.GetEnvironment("DEBUG_ISO_ZOOM");
            string dark = OS.GetEnvironment("DEBUG_ISO_DARK");
            _fullMap = string.IsNullOrEmpty(zoom) && string.IsNullOrEmpty(dark);

            var map = GD.Load<PackedScene>(scenePath).Instantiate<MapLoader>();
            map.PreviewMode = true;
            map.DarknessEnabled = !string.IsNullOrEmpty(dark);

            var (imageMin, imageMax) = PreviewImageBounds(map.MapJson);

            _viewport = new SubViewport
            {
                Size = _fullMap ? (Vector2I)(imageMax - imageMin).Ceil() : new Vector2I(CropWidth * 2, CropHeight * 2),
                TransparentBg = _fullMap,
                RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
                CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest,
            };
            AddChild(_viewport);

            if (!_fullMap)
            {
                // Fundo da prévia atrás do mapa, no mesmo canvas (o escuro também o escurece).
                _viewport.AddChild(new Polygon2D
                {
                    Color = PreviewBackground,
                    Polygon = new[] { new Vector2(-1, -1), new Vector2(CropWidth * 2 + 1, -1), new Vector2(CropWidth * 2 + 1, CropHeight * 2 + 1), new Vector2(-1, CropHeight * 2 + 1) },
                });
            }

            _viewport.AddChild(map);

            if (_state == "cleared")
                ClearEverything(map);

            if (_fullMap)
            {
                // A cena vem em 2x (mundo do jogo); a prévia é em 1x, com a mesma folga da
                // montar_previa.py em volta do conteúdo (depois recortado até ele).
                map.Scale = Vector2.One;
                map.Position = -imageMin;
                _output = $"user://iso_render_{_state}.png";
                return;
            }

            var parts = (string.IsNullOrEmpty(dark) ? zoom : dark).Split(',');
            var tile = new Vector2I(int.Parse(parts[0]), int.Parse(parts[1]));
            var ground = MapLoader.TileToGround(tile);
            Vector2 topLeft;

            if (string.IsNullOrEmpty(dark))
            {
                // montar_previa.py zoom: X0 = px - 480, Y0 = py - 290 (sem limitar à borda).
                topLeft = ground - new Vector2(CropWidth / 2f, CropHeight / 2f + 20);
                _output = $"user://iso_zoom_{_state}_{tile.X}_{tile.Y}.png";
            }
            else
            {
                // montar_previa.py escuro: o recorte fica dentro da imagem do mapa inteiro.
                float x0 = Mathf.Clamp(ground.X - CropWidth / 2f, imageMin.X, imageMax.X - CropWidth);
                float y0 = Mathf.Clamp(ground.Y - 20 - CropHeight / 2f, imageMin.Y, imageMax.Y - CropHeight);
                topLeft = new Vector2(Mathf.Floor(x0), Mathf.Floor(y0));

                string vision = parts.Length > 2 ? parts[2] : "sem_bonus";
                map.PlayerLightGroundOverride = ground;
                map.SetVisionState(vision, animate: false);
                _output = $"user://iso_dark_{_state}_{tile.X}_{tile.Y}_{vision}.png";
            }

            map.Scale = new Vector2(2, 2);
            map.Position = -topLeft * 2f;
        }

        /// <summary>"Depois" da prévia: todos os bloqueios liberados, todas as quests do mapa
        /// concluídas (luzes "after:") e todos os objetos acesos/ligados.</summary>
        private static void ClearEverything(MapLoader map)
        {
            foreach (var id in map.BlockerIds)
                map.SetBlockerCleared(id, true);

            foreach (var questId in MapQuestIds(map.MapJson))
                map.SetMapQuestDone(questId, true);

            foreach (var id in new List<string>(map.SwitchableIds))
                map.SetSwitchedOn(id, true);
        }

        private static IEnumerable<string> MapQuestIds(string mapJson)
        {
            var root = JsonDocument.Parse(FileAccess.GetFileAsString(mapJson)).RootElement;

            if (!root.TryGetProperty("quests", out var quests) || quests.ValueKind != JsonValueKind.Array)
                yield break;

            foreach (var q in quests.EnumerateArray())
            {
                if (q.TryGetProperty("id", out var id))
                    yield return id.GetString();
            }
        }

        /// <summary>Limites (px nativos, coordenadas do mapa) da imagem do mapa inteiro que o
        /// montar_previa.py monta: o conteúdo com 120 px de folga dos lados, 200 em cima e 160
        /// embaixo - é dentro dela que o recorte do escuro fica.</summary>
        private static (Vector2 Min, Vector2 Max) PreviewImageBounds(string mapJson)
        {
            var root = JsonDocument.Parse(FileAccess.GetFileAsString(mapJson)).RootElement;
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;

            foreach (var t in root.GetProperty("tiles").EnumerateArray())
            {
                var pos = t.GetProperty("pos");
                float x = pos[0].GetSingle(), y = pos[1].GetSingle();
                minX = Mathf.Min(minX, x);
                minY = Mathf.Min(minY, y);
                maxX = Mathf.Max(maxX, x);
                maxY = Mathf.Max(maxY, y);
            }

            return (new Vector2(minX - 120, minY - 200), new Vector2(maxX + 120, maxY + 160));
        }

        public override void _Process(double delta)
        {
            if (++_framesWaited < 5)
                return;

            var image = _viewport.GetTexture().GetImage();
            string path = ProjectSettings.GlobalizePath(_output);

            if (_fullMap)
            {
                var used = image.GetUsedRect();
                var cropped = image.GetRegion(used);

                var background = Image.CreateEmpty(cropped.GetWidth(), cropped.GetHeight(), false, Image.Format.Rgba8);
                background.Fill(Color.Color8(24, 40, 34));
                background.BlendRect(cropped, new Rect2I(Vector2I.Zero, cropped.GetSize()), Vector2I.Zero);
                background.SavePng(path);
                GD.Print($"IsoMapRenderTest: salvo {path} ({cropped.GetWidth()}x{cropped.GetHeight()})");
            }
            else
            {
                image.SavePng(path);
                GD.Print($"IsoMapRenderTest: salvo {path} ({image.GetWidth()}x{image.GetHeight()})");
            }

            GetTree().Quit();
        }
    }
}
