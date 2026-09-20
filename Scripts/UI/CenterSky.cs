using Godot;
using System.Collections.Generic;

namespace ProjetoDC.Scripts.UI
{
    /// <summary>
    /// Fundo de céu do Center: degradê que muda com a hora do relógio do jogo (dois céus sobrepostos,
    /// o de cima com alpha = fator entre os dois horários da tabela), estrelas à noite e duas camadas
    /// de nuvens andando sempre no mesmo sentido. Fica numa CanvasLayer abaixo do mundo e do HUD,
    /// então não acompanha o zoom/pan da câmera e nenhuma nuvem passa na frente das salas.
    /// Parâmetros e tabela de horários em <see cref="CenterSkyConfig"/>. Sombra e base das salas
    /// ficam em <see cref="CenterHexBackdrop"/> (elas pertencem ao mundo, não à tela).
    /// </summary>
    public partial class CenterSky : CanvasLayer
    {
        private sealed class Cloud
        {
            public Sprite2D Sprite;
            public float X;
            public float Y;
            public float Speed;
        }

        private readonly Dictionary<string, Texture2D> _textures = new();
        private readonly RandomNumberGenerator _rng = new();

        private readonly List<Cloud> _farClouds = new();
        private readonly List<Cloud> _nearClouds = new();

        private TextureRect _skyFrom;
        private TextureRect _skyTo;
        private TextureRect _stars;

        private Node2D _farLayer;
        private Node2D _nearLayer;

        private int _shownFromIndex = -1;
        private int _shownToIndex = -1;

        private CanvasItem _visibilitySource;

        public override void _Ready()
        {
            Layer = CenterSkyConfig.SkyCanvasLayer;

            // Continua andando com o jogo pausado por uma tela aberta (o HUD pausa a árvore): as
            // nuvens só enfeitam, e travar o céu atrás de uma janela pareceria bug.
            ProcessMode = ProcessModeEnum.Always;

            _rng.Randomize();

            _skyFrom = CreateSkyRect("SkyFrom", CanvasItem.TextureFilterEnum.Linear);
            _skyTo = CreateSkyRect("SkyTo", CanvasItem.TextureFilterEnum.Linear);

            _stars = CreateSkyRect("Stars", CanvasItem.TextureFilterEnum.Nearest);
            _stars.StretchMode = TextureRect.StretchModeEnum.Tile;
            _stars.Texture = LoadTexture(CenterSkyConfig.StarsTexture);

            _farLayer = CreateCloudLayer("FarClouds");
            _nearLayer = CreateCloudLayer("NearClouds");

            SpawnClouds(_farLayer, _farClouds, CenterSkyConfig.FarCloudTextures,
                _rng.RandiRange(CenterSkyConfig.FarCloudCountMin, CenterSkyConfig.FarCloudCountMax));

            SpawnClouds(_nearLayer, _nearClouds, CenterSkyConfig.NearCloudTextures,
                _rng.RandiRange(CenterSkyConfig.NearCloudCountMin, CenterSkyConfig.NearCloudCountMax));

            // O Center é escondido durante batalha/exploração e uma CanvasLayer não some junto com o
            // pai (por isso o Center desliga a própria CanvasLayer à mão) - acompanha aqui também.
            _visibilitySource = GetParent() as CanvasItem;

            if (_visibilitySource != null)
                _visibilitySource.VisibilityChanged += SyncVisibility;

            SyncVisibility();
            ApplyTimeOfDay();
        }

        public override void _ExitTree()
        {
            if (_visibilitySource != null && IsInstanceValid(_visibilitySource))
                _visibilitySource.VisibilityChanged -= SyncVisibility;
        }

        public override void _Process(double delta)
        {
            if (!Visible)
                return;

            ApplyTimeOfDay();

            Vector2 screen = GetViewport().GetVisibleRect().Size;

            MoveClouds(_farClouds, screen, (float)delta, isNear: false);
            MoveClouds(_nearClouds, screen, (float)delta, isNear: true);
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            if (!OS.IsDebugBuild() || @event is not InputEventKey { Pressed: true, Echo: false } key)
                return;

            if (key.Keycode == CenterSkyConfig.DebugHourForwardKey)
                CenterSkyClock.ShiftDebugHours(CenterSkyConfig.DebugHourStep);
            else if (key.Keycode == CenterSkyConfig.DebugHourBackKey)
                CenterSkyClock.ShiftDebugHours(-CenterSkyConfig.DebugHourStep);
            else if (key.Keycode == CenterSkyConfig.DebugHourResetKey)
                CenterSkyClock.ResetDebugHours();
        }

        private void SyncVisibility()
        {
            Visible = _visibilitySource == null || _visibilitySource.IsVisibleInTree();
        }

        // ---------------------------------------------------------------- ciclo do dia

        private void ApplyTimeOfDay()
        {
            var sample = CenterSkyConfig.Sample(CenterSkyClock.CurrentHour);

            // Só troca as texturas quando o par de horários muda; o fade é só o alpha do céu de cima.
            if (sample.FromIndex != _shownFromIndex || sample.ToIndex != _shownToIndex)
            {
                _shownFromIndex = sample.FromIndex;
                _shownToIndex = sample.ToIndex;

                _skyFrom.Texture = LoadTexture(CenterSkyConfig.Keyframes[sample.FromIndex].SkyTexture);
                _skyTo.Texture = LoadTexture(CenterSkyConfig.Keyframes[sample.ToIndex].SkyTexture);
            }

            _skyTo.Modulate = new Color(1f, 1f, 1f, sample.T);
            _stars.Modulate = new Color(1f, 1f, 1f, sample.StarsAlpha);

            _farLayer.Modulate = sample.CloudTint;
            _nearLayer.Modulate = sample.CloudTint;
        }

        // ---------------------------------------------------------------- nuvens

        private void SpawnClouds(Node2D layer, List<Cloud> clouds, string[] textureNames, int count)
        {
            Vector2 screen = GetViewport().GetVisibleRect().Size;

            for (int i = 0; i < count; i++)
            {
                var sprite = new Sprite2D
                {
                    Centered = false,
                    TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
                };

                layer.AddChild(sprite);

                var cloud = new Cloud { Sprite = sprite };
                clouds.Add(cloud);

                bool isNear = layer == _nearLayer;

                RollCloud(cloud, textureNames, screen, isNear);

                // Começa espalhada pela tela inteira (não empilhada na borda de entrada).
                cloud.X = _rng.RandfRange(-(sprite.Texture?.GetWidth() ?? 0), screen.X);
                ApplyCloudPosition(cloud);
            }
        }

        /// <summary>Sorteia sprite, velocidade e altura de uma nuvem (usado ao criar e a cada vez que
        /// ela sai da tela e reaparece do outro lado).</summary>
        private void RollCloud(Cloud cloud, string[] textureNames, Vector2 screen, bool isNear)
        {
            var texture = LoadTexture(textureNames[_rng.RandiRange(0, textureNames.Length - 1)]);

            cloud.Sprite.Texture = texture;

            cloud.Speed = isNear
                ? _rng.RandfRange(CenterSkyConfig.NearCloudSpeedMin, CenterSkyConfig.NearCloudSpeedMax)
                : _rng.RandfRange(CenterSkyConfig.FarCloudSpeedMin, CenterSkyConfig.FarCloudSpeedMax);

            float height = texture?.GetHeight() ?? 0f;
            float minY = 0f;
            float maxY = Mathf.Max(screen.Y - height, 0f);

            if (isNear && _rng.Randf() < CenterSkyConfig.NearCloudLowerHalfChance)
                minY = screen.Y * 0.5f;

            cloud.Y = _rng.RandfRange(Mathf.Min(minY, maxY), maxY);
        }

        private void MoveClouds(List<Cloud> clouds, Vector2 screen, float delta, bool isNear)
        {
            string[] textureNames = isNear
                ? CenterSkyConfig.NearCloudTextures
                : CenterSkyConfig.FarCloudTextures;

            foreach (var cloud in clouds)
            {
                cloud.X += cloud.Speed * CenterSkyConfig.CloudDirection * delta;

                float width = cloud.Sprite.Texture?.GetWidth() ?? 0f;

                bool leftScreen = CenterSkyConfig.CloudDirection > 0
                    ? cloud.X > screen.X
                    : cloud.X + width < 0f;

                if (leftScreen)
                {
                    RollCloud(cloud, textureNames, screen, isNear);

                    width = cloud.Sprite.Texture?.GetWidth() ?? 0f;

                    cloud.X = CenterSkyConfig.CloudDirection > 0 ? -width : screen.X;
                }

                ApplyCloudPosition(cloud);
            }
        }

        // Posição em pixels inteiros: a nuvem anda em float mas é desenhada arredondada, senão a
        // pixel art borra/tremula entre pixels.
        private static void ApplyCloudPosition(Cloud cloud)
        {
            cloud.Sprite.Position = new Vector2(Mathf.Round(cloud.X), Mathf.Round(cloud.Y));
        }

        // ---------------------------------------------------------------- montagem

        private TextureRect CreateSkyRect(string name, CanvasItem.TextureFilterEnum filter)
        {
            var rect = new TextureRect
            {
                Name = name,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.Scale,
                TextureFilter = filter,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };

            rect.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

            AddChild(rect);

            return rect;
        }

        private Node2D CreateCloudLayer(string name)
        {
            var layer = new Node2D { Name = name };

            AddChild(layer);

            return layer;
        }

        private Texture2D LoadTexture(string fileName)
        {
            if (_textures.TryGetValue(fileName, out var cached))
                return cached;

            string path = CenterSkyConfig.TextureFolder + fileName;

            var texture = ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;

            if (texture == null)
                GD.PrintErr($"CenterSky: textura não encontrada: {path}");

            _textures[fileName] = texture;

            return texture;
        }
    }
}
