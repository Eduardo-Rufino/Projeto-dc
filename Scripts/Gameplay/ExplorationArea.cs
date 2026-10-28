using Godot;
using ProjetoDC.Enums;
using ProjetoDC.Scripts.Data;
using ProjetoDC.Scripts.Managers;
using ProjetoDC.Scripts.UI;
using System;
using System.Collections.Generic;

namespace ProjetoDC.Scripts.Gameplay
{
    /// <summary>
    /// Área de exploração em tempo real: o Digimon escolhido (ver GameManager.StartExploration)
    /// anda livremente (clique-pra-andar, ver ExplorationDigimon) por um mapa com selvagens
    /// pra batalhar, NPCs pra conversar (com quests simples) e itens pra coletar. Os selvagens
    /// não são fixos - são sorteados de ExplorationMapData.WildPool toda vez que a área é
    /// aberta (ver SpawnWilds), então saem e voltam a cada visita.
    /// </summary>
    public partial class ExplorationArea : Node2D
    {
        private static readonly Random _random = new();

        // Suavização/velocidade de acompanhamento da câmera (ver AttachCameraToExplorer) -
        // valores baixos deixam a câmera "atrasada" de propósito, mais orgânico que grudar
        // exatamente na posição do Digimon a cada frame.
        private const float CameraSmoothingSpeed = 6f;

        // Mapas antigos: quão perto (px da tela) o explorador precisa estar pra pegar um item.
        // No isométrico é o bloco vizinho (ver IsIsoExplorerNextTo).
        private const float ItemPickupRange = 90f;

        private ConfirmWindow _itemFoundWindow;

        private Node2D _spawnsContainer;
        private Polygon2D _boundary;
        private ColorRect _background;
        private AreaThemeData _theme;
        private Marker2D _explorerSpawnPoint;
        private Camera2D _camera;

        private PackedScene _explorerScene;
        private PackedScene _wildScene;
        private PackedScene _npcScene;
        private PackedScene _itemScene;

        private DialogueScreen _dialogueScreen;
        private Label _mapNameLabel;

        private ExplorationMapData _map;

        // Y local do Boundary além do qual é mar (só existe em tema com orla - ver
        // AreaThemeData.ShoreTexture); IsPointInsideBoundary trata como fora do mapa.
        private float? _walkLimitY;
        private DigimonInstance _explorer;
        private ExplorationDigimon _explorerVisual;

        /// <summary>Digimon que está explorando (ver GameManager.TravelExploration, que leva o
        /// mesmo Digimon pra outra área).</summary>
        public DigimonInstance Explorer => _explorer;

        public override void _Ready()
        {
            _camera = GetNode<Camera2D>("Camera2D");

            _boundary = GetNode<Polygon2D>("Boundary");
            _background = GetNode<ColorRect>("BackgroundLayer/Background");
            _spawnsContainer = GetNode<Node2D>("SpawnsContainer");
            _explorerSpawnPoint = GetNode<Marker2D>("ExplorerSpawnPoint");

            _explorerScene = GD.Load<PackedScene>("res://Scenes/Exploration/ExplorationDigimon.tscn");
            _wildScene = GD.Load<PackedScene>("res://Scenes/Exploration/WildEncounterSpawn.tscn");
            _npcScene = GD.Load<PackedScene>("res://Scenes/Exploration/ExplorationNpcVisual.tscn");
            _itemScene = GD.Load<PackedScene>("res://Scenes/Exploration/ExplorationItemVisual.tscn");

            _dialogueScreen = GetNode<DialogueScreen>("CanvasLayer/DialogueScreen");
            _mapNameLabel = GetNode<Label>("CanvasLayer/MapNamePanel/MapNameLabel");

            var exitButton = GetNode<Button>("CanvasLayer/ExitButton");
            exitButton.Pressed += OnExitPressed;

            GameManager.Instance.TeamBattleStarted += OnTeamBattleStarted;
            GameManager.Instance.TeamBattleFinished += OnTeamBattleFinished;
        }

        public override void _ExitTree()
        {
            if (GameManager.Instance == null)
                return;

            GameManager.Instance.TeamBattleStarted -= OnTeamBattleStarted;
            GameManager.Instance.TeamBattleFinished -= OnTeamBattleFinished;
            GameManager.Instance.QuestTurnedIn -= OnQuestTurnedIn;
        }

        public override void _Process(double delta)
        {
            UpdateMapFreeze();
            UpdateIsoRaft();
            UpdateWalkIntoTransitions();
        }

        public void Init(DigimonInstance explorer, ExplorationMapData map, GameManager.ExplorationArrival arrival = null)
        {
            _explorer = explorer;
            _map = map;

            _mapNameLabel.Text = map.Name;

            AddReturnButton();
            AddQuestTracker();

            CallDeferred(MethodName.RunFlowTestStep);

            if (!string.IsNullOrEmpty(map.IsoMapScene))
            {
                InitIso(arrival);
                return;
            }

            _theme = DatabaseManager.Instance.GetAreaTheme(map.Theme);
            ApplyThemeGround();
            ApplyShoreline();

            // Decorações primeiro: são adicionadas antes de tudo em SpawnsContainer, então
            // desenham por baixo do explorador/selvagens/NPCs/itens (mesma irmã mais velha
            // desenha atrás - sem precisar mexer em z_index pra isso).
            SpawnDecorations();
            SpawnSeaDecorations();
            SpawnConcealmentRings();

            SpawnExplorer();
            SpawnWilds();
            SpawnNpcs();
            SpawnItems();
        }

        public bool IsPointInsideBoundary(Vector2 globalPosition)
        {
            if (_isoMap != null)
                return IsIsoPointWalkable(globalPosition);

            if (_boundary == null)
                return true;

            Vector2 local = _boundary.ToLocal(globalPosition);

            if (_walkLimitY.HasValue && local.Y > _walkLimitY.Value)
                return false;

            return Geometry2D.IsPointInPolygon(local, _boundary.Polygon);
        }

        private void SpawnExplorer()
        {
            _explorerVisual = _explorerScene.Instantiate<ExplorationDigimon>();

            if (_isoMap != null)
            {
                PlaceIsoEntity(_explorerVisual, _isoExplorerSpawn);
                _explorerVisual.Speed = IsoExplorerSpeed;
                _isoMap.Player = _explorerVisual;
            }
            else
            {
                _spawnsContainer.AddChild(_explorerVisual);
                _explorerVisual.GlobalPosition = _explorerSpawnPoint.GlobalPosition;
            }

            _explorerVisual.Initialize(_explorer, this);

            AttachCameraToExplorer(_explorerVisual);
        }

        /// <summary>
        /// A câmera passa a ser filha do próprio Digimon explorador, pra seguir ele andando
        /// pelo mapa (mapas de exploração são bem maiores que a tela - só dá pra ver o que já
        /// foi andado até ali, o resto só aparece explorando de verdade). LimitLeft/Top/Right/
        /// Bottom (calculados a partir do polígono de Boundary) impedem a câmera de mostrar
        /// além da borda do mapa.
        /// </summary>
        private void AttachCameraToExplorer(Node2D explorerVisual)
        {
            _camera.GetParent()?.RemoveChild(_camera);

            explorerVisual.AddChild(_camera);

            _camera.Position = Vector2.Zero;
            _camera.PositionSmoothingEnabled = true;
            _camera.PositionSmoothingSpeed = CameraSmoothingSpeed;

            ConfigureCameraLimits();

            _camera.MakeCurrent();
        }

        private void ConfigureCameraLimits()
        {
            Rect2? bounds = _isoMap != null ? GetIsoCameraBounds() : GetBoundaryBounds();

            if (bounds == null)
                return;

            _camera.LimitLeft = (int)bounds.Value.Position.X;
            _camera.LimitTop = (int)bounds.Value.Position.Y;
            _camera.LimitRight = (int)bounds.Value.End.X;
            _camera.LimitBottom = (int)bounds.Value.End.Y;
        }

        private Rect2? GetBoundaryBounds()
        {
            if (_boundary == null || _boundary.Polygon.Length == 0)
                return null;

            Vector2 min = _boundary.Polygon[0];
            Vector2 max = _boundary.Polygon[0];

            foreach (var point in _boundary.Polygon)
            {
                min = new Vector2(Mathf.Min(min.X, point.X), Mathf.Min(min.Y, point.Y));
                max = new Vector2(Mathf.Max(max.X, point.X), Mathf.Max(max.Y, point.Y));
            }

            return new Rect2(min, max - min);
        }

        // Não nasce nenhuma decoração mais perto que isso de um ponto de interesse (spawn do
        // explorador, selvagem, NPC ou item) - evita tampar visualmente o que importa.
        private const float DecorationMinDistanceFromPoints = 70f;

        /// <summary>Aplica o chão do tema: a textura ladrilhada em todo o Boundary (ver
        /// AreaThemeData.GroundTexture) ou, sem ela, a cor sólida - vazio no tema = mantém a
        /// cor padrão da cena, que é a da floresta. O fundo (fora do mapa) só aparece se a
        /// câmera sair dos limites, então continua sendo uma cor.</summary>
        private void ApplyThemeGround()
        {
            if (!string.IsNullOrEmpty(_theme.BackgroundColor))
                _background.Color = Color.FromHtml(_theme.BackgroundColor);

            if (!string.IsNullOrEmpty(_theme.GroundTexture))
            {
                var texture = GD.Load<Texture2D>(_theme.Folder + _theme.GroundTexture);

                if (texture != null)
                {
                    _boundary.Texture = texture;

                    // Polygon2D lê a textura pelo UV, em pixels dela - usar as próprias
                    // coordenadas do polígono como UV faz 1 pixel da imagem = 1 unidade do
                    // mundo, ladrilhando a partir da origem do mapa.
                    _boundary.UV = (Vector2[])_boundary.Polygon.Clone();
                    _boundary.TextureRepeat = CanvasItem.TextureRepeatEnum.Enabled;
                    _boundary.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;

                    // A cor do Polygon2D multiplica a textura - branco = sem tingir.
                    _boundary.Color = Colors.White;

                    return;
                }

                GD.PrintErr($"Textura de chão não encontrada: {_theme.Folder}{_theme.GroundTexture}");
            }

            if (!string.IsNullOrEmpty(_theme.GroundColor))
                _boundary.Color = Color.FromHtml(_theme.GroundColor);
        }

        /// <summary>Monta a praia por cima do chão de areia (o Boundary continua sendo o
        /// retângulo todo, que a câmera usa como limite): a faixa da orla e, abaixo dela, o mar
        /// até o fim do mapa. Ver AreaThemeData.ShoreTexture.</summary>
        private void ApplyShoreline()
        {
            _walkLimitY = null;

            if (!_theme.HasShoreline)
                return;

            Rect2? bounds = GetBoundaryBounds();
            var shore = GD.Load<Texture2D>(_theme.Folder + _theme.ShoreTexture);
            var sea = GD.Load<Texture2D>(_theme.Folder + _theme.SeaTexture);

            if (bounds == null || shore == null || sea == null)
            {
                GD.PrintErr($"Orla do tema '{_theme.Id}' incompleta: sem texturas/limites, o mar não foi montado.");
                return;
            }

            float left = bounds.Value.Position.X;
            float right = bounds.Value.End.X;
            float shoreTop = bounds.Value.Position.Y + _theme.ShoreLine * bounds.Value.Size.Y;
            float seaTop = shoreTop + shore.GetHeight();

            _walkLimitY = _theme.GetWalkLimitY(bounds.Value.Position.Y, bounds.Value.Size.Y);

            // Logo depois do Boundary na árvore: desenha por cima da areia e por baixo de tudo
            // que vai em SpawnsContainer.
            int index = _boundary.GetIndex() + 1;

            var shoreStrip = CreateGroundStrip(shore, left, right, shoreTop, seaTop);
            AddChild(shoreStrip);
            MoveChild(shoreStrip, index);

            var seaStrip = CreateGroundStrip(sea, left, right, seaTop, bounds.Value.End.Y);
            AddChild(seaStrip);
            MoveChild(seaStrip, index + 1);
        }

        /// <summary>Faixa retangular ladrilhada: 1 pixel da imagem = 1 unidade do mundo, com o
        /// canto de cima-esquerda da faixa como origem da textura (assim a faixa da orla começa
        /// exatamente no topo da imagem, sem cortar a onda).</summary>
        private static Polygon2D CreateGroundStrip(Texture2D texture, float left, float right, float top, float bottom)
        {
            var points = new[]
            {
                new Vector2(left, top), new Vector2(right, top),
                new Vector2(right, bottom), new Vector2(left, bottom),
            };

            var uv = new Vector2[points.Length];

            for (int i = 0; i < points.Length; i++)
                uv[i] = points[i] - new Vector2(left, top);

            return new Polygon2D
            {
                Polygon = points,
                UV = uv,
                Texture = texture,
                TextureRepeat = CanvasItem.TextureRepeatEnum.Enabled,
                TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
                Color = Colors.White,
            };
        }

        /// <summary>Área onde dá pra pisar: o Boundary, cortado no limite de caminhada quando
        /// há mar. Decorações de terra e as zonas (frações) usam esse retângulo.</summary>
        private Rect2? GetLandBounds()
        {
            Rect2? bounds = GetBoundaryBounds();

            if (bounds == null || !_walkLimitY.HasValue)
                return bounds;

            var b = bounds.Value;
            return new Rect2(b.Position, new Vector2(b.Size.X, _walkLimitY.Value - b.Position.Y));
        }

        /// <summary>Decoração só visual dentro do mar (coral, barco...), longe da faixa da onda.</summary>
        private void SpawnSeaDecorations()
        {
            Rect2? bounds = GetBoundaryBounds();

            if (bounds == null || !_theme.HasShoreline || _theme.SeaKinds.Count == 0)
                return;

            var shore = GD.Load<Texture2D>(_theme.Folder + _theme.ShoreTexture);
            float top = bounds.Value.Position.Y + _theme.ShoreLine * bounds.Value.Size.Y + shore.GetHeight() + 90f;
            float bottom = bounds.Value.End.Y - 10f;

            if (bottom <= top)
                return;

            for (int i = 0; i < _theme.SeaDecorationCount; i++)
            {
                var position = new Vector2(
                    bounds.Value.Position.X + 80f + (float)_random.NextDouble() * (bounds.Value.Size.X - 160f),
                    top + (float)_random.NextDouble() * (bottom - top));

                var decoration = new AreaDecoration { Position = position };
                decoration.SetKind(_theme, _theme.SeaKinds[_random.Next(_theme.SeaKinds.Count)], _random);
                _spawnsContainer.AddChild(decoration);
            }
        }

        private void SpawnDecorations()
        {
            Rect2? bounds = GetLandBounds();

            if (bounds == null)
                return;

            var pointsToAvoid = new List<Vector2> { _explorerSpawnPoint.GlobalPosition };

            foreach (var point in _map.WildSpawnPoints)
                pointsToAvoid.Add(new Vector2(point.X, point.Y));

            foreach (var spawn in _map.Npcs)
                pointsToAvoid.Add(new Vector2(spawn.X, spawn.Y));

            foreach (var pickup in _map.ItemPickups)
                pointsToAvoid.Add(new Vector2(pickup.X, pickup.Y));

            for (int i = 0; i < _theme.DecorationCount; i++)
            {
                Vector2? position = FindDecorationPosition(bounds.Value, pointsToAvoid);

                if (position == null)
                    continue;

                var kinds = PickDecorationKindsFor(position.Value, bounds.Value);

                if (kinds.Count == 0)
                    continue;

                var decoration = new AreaDecoration { Position = position.Value };

                decoration.SetKind(_theme, kinds[_random.Next(kinds.Count)], _random);

                float scale = _theme.ScaleMin + (float)_random.NextDouble() * (_theme.ScaleMax - _theme.ScaleMin);
                decoration.Scale = new Vector2(scale, scale);

                _spawnsContainer.AddChild(decoration);
            }
        }

        /// <summary>Mix de tipos da zona (bioma) que contém a posição, ou o mix padrão do tema
        /// (trilha/transição entre biomas) se ela cair fora de todas.</summary>
        private List<string> PickDecorationKindsFor(Vector2 position, Rect2 bounds)
        {
            foreach (var zone in _theme.Zones)
            {
                Vector2 center = bounds.Position + new Vector2(zone.CenterX, zone.CenterY) * bounds.Size;

                if (position.DistanceTo(center) <= zone.Radius)
                    return zone.Kinds;
            }

            return _theme.DefaultKinds;
        }

        // Quantos elementos formam o cerco ao redor de um NPC "Hidden" (ver
        // NpcSpawnData.Hidden), e a que distância dele - maior que
        // DecorationMinDistanceFromPoints de propósito, pra preencher exatamente o anel que a
        // decoração comum deixaria vazio ao redor do ponto.
        private const int ConcealmentRingPoints = 9;
        private const float ConcealmentRingRadius = 95f;

        // Largura (radianos) da abertura deixada no cerco - o único jeito de passar até o
        // NPC escondido. Mirada no WildSpawnPoint mais próximo (ver SpawnConcealmentRings),
        // então o selvagem que vagueia/persegue por ali faz o papel de guarda bloqueando essa
        // entrada.
        private const float ConcealmentGapWidth = Mathf.Pi / 2.2f;

        // Distância máxima pra um WildSpawnPoint contar como "guarda" de um recanto escondido
        // - além disso é só um selvagem solto em outra parte do mapa, não o bloqueio
        // pretendido pra esse NPC.
        private const float MaxGuardDistance = 260f;

        /// <summary>
        /// Planta um cerco fixo de árvore/arbusto ao redor de cada NPC marcado Hidden -
        /// diferente da decoração comum (SpawnDecorations), que só evita nascer perto de
        /// pontos de interesse por sorte, esse cerco é garantido: não depende de RNG pra
        /// esconder o NPC de verdade atrás de árvores. A abertura mira no guarda mais próximo
        /// (ou de volta pro spawn do explorador, se não houver nenhum perto o bastante).
        /// </summary>
        private void SpawnConcealmentRings()
        {
            if (_theme.ConcealmentKinds.Count == 0)
                return;

            foreach (var npcSpawn in _map.Npcs)
            {
                if (!npcSpawn.Hidden)
                    continue;

                var center = new Vector2(npcSpawn.X, npcSpawn.Y);
                Vector2 gapTarget = FindNearestGuardPoint(center) ?? _explorerSpawnPoint.GlobalPosition;
                float gapAngle = (gapTarget - center).Angle();

                for (int i = 0; i < ConcealmentRingPoints; i++)
                {
                    float angle = i * Mathf.Tau / ConcealmentRingPoints;

                    if (Mathf.Abs(Mathf.AngleDifference(angle, gapAngle)) < ConcealmentGapWidth / 2f)
                        continue;

                    var position = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * ConcealmentRingRadius;

                    var decoration = new AreaDecoration { Position = position };

                    decoration.SetKind(_theme, _theme.ConcealmentKinds[_random.Next(_theme.ConcealmentKinds.Count)], _random);

                    float scale = 0.9f + (float)_random.NextDouble() * 0.3f;
                    decoration.Scale = new Vector2(scale, scale);

                    _spawnsContainer.AddChild(decoration);
                }
            }
        }

        private Vector2? FindNearestGuardPoint(Vector2 center)
        {
            Vector2? nearest = null;
            float nearestDistance = MaxGuardDistance;

            foreach (var point in _map.WildSpawnPoints)
            {
                var position = new Vector2(point.X, point.Y);
                float distance = position.DistanceTo(center);

                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = position;
                }
            }

            return nearest;
        }

        private Vector2? FindDecorationPosition(Rect2 bounds, List<Vector2> pointsToAvoid)
        {
            for (int attempt = 0; attempt < 10; attempt++)
            {
                var candidate = new Vector2(
                    bounds.Position.X + (float)_random.NextDouble() * bounds.Size.X,
                    bounds.Position.Y + (float)_random.NextDouble() * bounds.Size.Y
                );

                bool tooClose = false;

                foreach (var point in pointsToAvoid)
                {
                    if (candidate.DistanceTo(point) < DecorationMinDistanceFromPoints)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (!tooClose)
                    return candidate;
            }

            return null;
        }

        private void SpawnWilds()
        {
            if (_map.WildPool.Count == 0)
                return;

            foreach (var point in _map.WildSpawnPoints)
            {
                int digimonId = _map.WildPool[_random.Next(_map.WildPool.Count)];
                var data = DatabaseManager.Instance.GetDigimon(digimonId);

                if (data == null)
                    continue;

                int level = _map.WildLevelMin >= _map.WildLevelMax
                    ? _map.WildLevelMin
                    : _random.Next(_map.WildLevelMin, _map.WildLevelMax + 1);

                var wild = _wildScene.Instantiate<WildEncounterSpawn>();

                if (_isoMap != null)
                {
                    if (!TryResolveIsoPosition(point.Spot, point.Tile, out var ground))
                    {
                        wild.QueueFree();
                        continue;
                    }

                    PlaceIsoEntity(wild, ground);
                    wild.Area = this;
                }
                else
                {
                    _spawnsContainer.AddChild(wild);
                    wild.GlobalPosition = new Vector2(point.X, point.Y);
                }

                wild.Initialize(digimonId, level, data, _explorer, _explorerVisual);
            }
        }

        private void SpawnNpcs()
        {
            foreach (var spawn in _map.Npcs)
            {
                var data = DatabaseManager.Instance.GetNpc(spawn.NpcId);

                if (data == null)
                    continue;

                var npc = _npcScene.Instantiate<ExplorationNpcVisual>();

                if (_isoMap != null)
                {
                    if (!TryResolveIsoPosition(spawn.Spot, spawn.Tile, out var ground))
                    {
                        npc.QueueFree();
                        continue;
                    }

                    PlaceIsoEntity(npc, ground);
                    LiftIsoChildren(npc, ground);
                }
                else
                {
                    _spawnsContainer.AddChild(npc);
                    npc.GlobalPosition = new Vector2(spawn.X, spawn.Y);
                }

                npc.Initialize(data, _explorerVisual);
                npc.Clicked += OnNpcClicked;
            }
        }

        private void SpawnItems()
        {
            foreach (var pickup in _map.ItemPickups)
            {
                // Baú do mapa isométrico fica aberto no lugar depois de coletado; o resto some.
                bool collected = GameManager.Instance.Save.Center.CollectedItemPickupIds.Contains(pickup.UniqueId);

                if (collected && !IsIsoChest(pickup.KitSprite))
                    continue;

                var itemData = DatabaseManager.Instance.GetItem(pickup.ItemId);

                Texture2D icon = null;

                if (itemData != null && !string.IsNullOrEmpty(itemData.IconPath) && ResourceLoader.Exists(itemData.IconPath))
                    icon = GD.Load<Texture2D>(itemData.IconPath);

                var visual = _itemScene.Instantiate<ExplorationItemVisual>();

                if (_isoMap != null)
                {
                    if (!TryResolveIsoPosition(pickup.Spot, pickup.Tile, out var ground))
                    {
                        visual.QueueFree();
                        continue;
                    }

                    PlaceIsoEntity(visual, ground, IsoItemScale);
                }
                else
                {
                    _spawnsContainer.AddChild(visual);
                    visual.GlobalPosition = new Vector2(pickup.X, pickup.Y);
                }

                visual.Initialize(pickup, icon);

                if (_isoMap != null)
                    DressIsoItem(visual, pickup.KitSprite, collected);

                if (collected)
                    visual.SetClickable(false);
                else
                    visual.Clicked += OnItemClicked;
            }
        }

        /// <summary>Clique num item: longe, o explorador só vai até ele (no mapa isométrico, até
        /// o bloco do lado); perto, coleta e mostra o que veio.</summary>
        private void OnItemClicked(ExplorationItemVisual visual)
        {
            if (_explorerVisual == null)
                return;

            if (!IsExplorerNextTo(visual))
            {
                if (_isoMap != null)
                    WalkExplorerNextTo(visual);
                else
                    _explorerVisual.MoveTo(visual.GlobalPosition);

                return;
            }

            var pickup = visual.Pickup;

            if (!GameManager.Instance.CollectExplorationItem(pickup).Success)
                return;

            if (_isoMap != null && IsIsoChest(pickup.KitSprite))
                OpenIsoChest(visual);
            else
                visual.QueueFree();

            ShowItemFound(pickup);
        }

        private bool IsExplorerNextTo(ExplorationItemVisual visual)
        {
            if (_isoMap != null)
                return IsIsoExplorerNextTo(visual);

            return _explorerVisual.GlobalPosition.DistanceTo(visual.GlobalPosition) <= ItemPickupRange;
        }

        private void ShowItemFound(ItemPickupData pickup)
        {
            var item = DatabaseManager.Instance.GetItem(pickup.ItemId);
            string name = item?.Name ?? "Item";
            Texture2D icon = item != null && !string.IsNullOrEmpty(item.IconPath) && ResourceLoader.Exists(item.IconPath)
                ? GD.Load<Texture2D>(item.IconPath)
                : null;

            if (_itemFoundWindow == null)
            {
                _itemFoundWindow = new ConfirmWindow(hasCancel: false);
                AddChild(_itemFoundWindow);
            }

            string message = $"Você encontrou {pickup.Quantity}x {name}.";

            if (!string.IsNullOrEmpty(item?.Description))
                message += "\n\n" + item.Description;

            _itemFoundWindow.Open("🎁 Item encontrado!", message, "OK", icon: icon);
        }

        private void OnNpcClicked(NpcData npc)
        {
            if (TryOpenNpcTransition(npc))
                return;

            _dialogueScreen.Open(npc);
        }

        // Congela o mapa (explorador, selvagens, NPCs, itens - e o mapa isométrico, que mora
        // dentro do SpawnsContainer) enquanto o diálogo ou uma confirmação de passagem está
        // aberta - senão os selvagens continuavam vagando/perseguindo e encostavam no
        // explorador, disparando batalha no meio da conversa. Só o SpawnsContainer: a própria
        // área (e as janelas, filhas dela) segue processando normalmente.
        /// <summary>Botão/lista das quests ativas, por baixo do diálogo (que fica por cima).</summary>
        private void AddQuestTracker()
        {
            var layer = GetNode("CanvasLayer");
            var tracker = new ExplorationQuestTracker { Name = "QuestTracker" };

            layer.AddChild(tracker);
            layer.MoveChild(tracker, _dialogueScreen.GetIndex());
        }

        private void UpdateMapFreeze()
        {
            bool frozen = _dialogueScreen.Visible ||
                          (_transitionConfirm != null && _transitionConfirm.IsOpen) ||
                          (_itemFoundWindow != null && _itemFoundWindow.IsOpen) ||
                          (_infoWindow != null && _infoWindow.IsOpen);
            var mode = frozen ? ProcessModeEnum.Disabled : ProcessModeEnum.Inherit;

            if (_spawnsContainer.ProcessMode != mode)
                _spawnsContainer.ProcessMode = mode;
        }

        // Um selvagem só é enfrentado com o Digimon que está explorando (BattleFromExploration
        // garante isso - ver GameManager.StartWildEncounter) - a área só reage às batalhas que
        // ela mesma originou, nunca às batalhas normais do Center.
        private void OnTeamBattleStarted()
        {
            if (!GameManager.Instance.BattleFromExploration)
                return;

            Visible = false;

            // As CanvasLayer da área (fundo e HUD) não somem com o Visible do Node2D: sem isso o
            // fundo da área (verde na floresta, preto na caverna escura) tampava o céu da arena
            // e o botão 🚪 Sair ficava por baixo do Pausa da batalha.
            SetAreaLayersVisible(false);

            // Não basta esconder: ProcessMode.Always (setado em GameManager.StartExploration
            // pra continuar rodando durante a própria pausa) também fazia essa área continuar
            // processando clique mesmo escondida atrás da arena de batalha - um clique perdido
            // conseguia acertar outro selvagem por baixo e disparar uma SEGUNDA batalha em cima
            // da primeira, corrompendo BattleFromExploration/o time (campos únicos, não por
            // batalha) e deixando o jogo preso mostrando pedaços da exploração e do Center
            // juntos. Disabled para tudo (incluindo input) até a batalha acabar.
            ProcessMode = ProcessModeEnum.Disabled;
        }

        private void OnTeamBattleFinished(BattleResult result)
        {
            if (!GameManager.Instance.BattleFromExploration)
                return;

            ProcessMode = ProcessModeEnum.Always;

            Visible = true;
            SetAreaLayersVisible(true);

            // A BattleArena usa sua própria Camera2D (precisa ficar "current" durante a luta) -
            // ao voltar, precisa reivindicar a câmera de volta, senão a viewport fica sem
            // nenhuma câmera ativa (a da arena já foi liberada junto com ela).
            _camera.MakeCurrent();

            // O explorador pode ter evoluído na batalha (e trocado de elemento): a luz dele no
            // escuro depende disso.
            UpdateIsoVision();
        }

        private void SetAreaLayersVisible(bool visible)
        {
            GetNode<CanvasLayer>("BackgroundLayer").Visible = visible;
            GetNode<CanvasLayer>("CanvasLayer").Visible = visible;
        }

        private void OnExitPressed()
        {
            GameManager.Instance.EndExploration();
        }
    }
}
