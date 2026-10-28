using Godot;
using ProjetoDC.Scripts.Gameplay;

namespace ProjetoDC.Scripts.Systems.Exploration
{
    /// <summary>
    /// Ferramenta de conferência do mapa isométrico (cena Scenes/Audit/IsoMapRender.tscn): monta
    /// o mapa em 1x, parado no quadro da prévia (MapLoader.PreviewMode), renderiza numa
    /// SubViewport do tamanho do mapa inteiro e salva o PNG em user://iso_render_{estado}.png,
    /// no mesmo enquadramento da ferramenta montar_previa.py do pacote (origem em 2000,200 e
    /// recortado até o conteúdo). Depois é só comparar as duas imagens. Fecha sozinha.
    /// Variáveis: DEBUG_ISO_MAP (cena do mapa) e DEBUG_ISO_STATE (blocked/cleared).
    /// </summary>
    public partial class IsoMapRenderTest : Node
    {
        private const int ViewportWidth = 5200;
        private const int ViewportHeight = 2900;
        private static readonly Vector2 Origin = new(2000, 200);

        private SubViewport _viewport;
        private string _state;
        private int _framesWaited;

        public override void _Ready()
        {
            string scenePath = OS.GetEnvironment("DEBUG_ISO_MAP");
            if (string.IsNullOrEmpty(scenePath))
                scenePath = "res://Assets/Areas/Forest_new/Floresta.tscn";

            _state = OS.GetEnvironment("DEBUG_ISO_STATE");
            if (string.IsNullOrEmpty(_state))
                _state = "blocked";

            _viewport = new SubViewport
            {
                Size = new Vector2I(ViewportWidth, ViewportHeight),
                TransparentBg = true,
                RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
                CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest,
            };
            AddChild(_viewport);

            var map = GD.Load<PackedScene>(scenePath).Instantiate<MapLoader>();
            map.PreviewMode = true;
            _viewport.AddChild(map);

            // A cena vem em 2x (mundo do jogo); a prévia é em 1x.
            map.Scale = Vector2.One;
            map.Position = Origin;

            if (_state == "cleared")
            {
                foreach (var id in map.BlockerIds)
                    map.SetBlockerCleared(id, true);
            }
        }

        public override void _Process(double delta)
        {
            if (++_framesWaited < 5)
                return;

            var image = _viewport.GetTexture().GetImage();
            var used = image.GetUsedRect();
            var cropped = image.GetRegion(used);

            var background = Image.CreateEmpty(cropped.GetWidth(), cropped.GetHeight(), false, Image.Format.Rgba8);
            background.Fill(Color.Color8(24, 40, 34));
            background.BlendRect(cropped, new Rect2I(Vector2I.Zero, cropped.GetSize()), Vector2I.Zero);

            string path = ProjectSettings.GlobalizePath($"user://iso_render_{_state}.png");
            background.SavePng(path);
            GD.Print($"IsoMapRenderTest: salvo {path} ({cropped.GetWidth()}x{cropped.GetHeight()})");

            GetTree().Quit();
        }
    }
}
