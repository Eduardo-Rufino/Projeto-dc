using Godot;
using ProjetoDC.Scripts.Data;
using ProjetoDC.Scripts.Managers;
using ProjetoDC.Scripts.UI;
using System.Collections.Generic;

namespace ProjetoDC.Scripts.Gameplay
{
    /// <summary>
    /// Modo isométrico da área de exploração (ExplorationMapData.IsoMapScene): o chão, as árvores
    /// e as regras de andar vêm do MapLoader; selvagens, NPCs, itens e o explorador continuam
    /// sendo os mesmos da área, só que filhos de MapLoader.Sorted (Y-sort com o cenário), com
    /// Position = posição NO CHÃO e o desenho erguido nos planaltos/rampas.
    ///
    /// Escala: o MapLoader é 2x; as entidades ficam em 0.5 dentro dele, ou seja, exatamente do
    /// tamanho que já tinham nos mapas antigos (os sprites dos Digimons já são 3x).
    /// </summary>
    public partial class ExplorationArea
    {
        private const float IsoEntityScale = 0.5f;

        // Itens com sprite do kit ficam em 1 (= px nativos do mapa, mesmo tamanho do cenário).
        private const float IsoItemScale = 1f;

        // O mapa isométrico é 2x: o passo antigo (130) ficava lento demais pra blocos desse tamanho.
        private const float IsoExplorerSpeed = 170f;

        // O sprite do Digimon é centrado no nó; no mapa isométrico a Position é o ponto no chão,
        // então o desenho sobe meia altura pra o pé encostar no bloco (px da tela).
        private const float IsoSpriteFootOffset = 20f;

        // Mesmo alcance de clique dos NPCs/selvagens (ExplorationNpcVisual.InteractionRange).
        private const float IsoInteractionRange = 140f;

        private MapLoader _isoMap;
        private Vector2 _isoExplorerSpawn;
        private ConfirmWindow _transitionConfirm;
        private ExplorationTransitionData _pendingTransition;
        private Area2D _mineClickArea;
        private Node2D _mineNamePlate;
        private ExplorationTransitionData _mineTransition;

        // Sprite do kit de cada baú/item (pra trocar pelo baú aberto) e o bloco dele.
        private readonly Dictionary<ExplorationItemVisual, Node2D> _isoItemSprites = new();

        private void InitIso(GameManager.ExplorationArrival arrival)
        {
            _boundary.Visible = false;

            _isoMap = GD.Load<PackedScene>(_map.IsoMapScene).Instantiate<MapLoader>();
            _isoMap.ShowSpotPlaceholders = false;
            _spawnsContainer.AddChild(_isoMap);

            ApplyIsoBlockers();
            ApplyIsoMapState();

            _isoExplorerSpawn = ResolveExplorerSpawn(arrival);

            _walkTest = IsWalkTestEnabled;

            SpawnExplorer();

            // DEBUG_ISO_NO_WILDS=1: sem selvagens (screenshots sem batalha no meio).
            if (!_walkTest && OS.GetEnvironment("DEBUG_ISO_NO_WILDS") != "1")
                SpawnWilds();

            SpawnNpcs();
            SpawnItems();
            SpawnMineInteraction();
            SpawnSwitchInteractions();
            SpawnWalkIntoTransitions();
            InitIsoDarkness();

            GameManager.Instance.QuestTurnedIn += OnQuestTurnedIn;
        }

        private Vector2 ResolveExplorerSpawn(GameManager.ExplorationArrival arrival)
        {
            // DEBUG_EXPLORATION_TILE=gx,gy[,dx,dy]: começa nesse bloco, deslocado dx,dy px nativos
            // do centro (testes manuais/screenshots).
            string debugTile = OS.GetEnvironment("DEBUG_EXPLORATION_TILE");

            if (!string.IsNullOrEmpty(debugTile))
            {
                var parts = debugTile.Split(',');

                if (parts.Length >= 2 && int.TryParse(parts[0], out int gx) && int.TryParse(parts[1], out int gy))
                {
                    var nudge = parts.Length == 4 && int.TryParse(parts[2], out int dx) && int.TryParse(parts[3], out int dy)
                        ? new Vector2(dx, dy)
                        : Vector2.Zero;

                    return MapLoader.TileToGround(new Vector2I(gx, gy)) + nudge;
                }
            }

            if (arrival != null && TryResolveIsoPosition(arrival.Spot, arrival.Tile, out var arrivalGround))
                return arrivalGround;

            if (TryResolveIsoPosition(_map.ExplorerSpawnSpot, null, out var spawnGround))
                return spawnGround;

            return Vector2.Zero;
        }

        /// <summary>Posição no chão (px nativos do mapa) de um spot ou bloco [gx, gy].</summary>
        private bool TryResolveIsoPosition(string spot, List<int> tile, out Vector2 ground)
        {
            ground = Vector2.Zero;

            if (!string.IsNullOrEmpty(spot))
            {
                if (_isoMap.TryGetSpot(spot, out var info))
                {
                    ground = MapLoader.TileToGround(info.Tile);
                    return true;
                }

                GD.PrintErr($"Mapa '{_map.Name}': spot '{spot}' não existe no mapa isométrico.");
                return false;
            }

            if (tile != null && tile.Count == 2)
            {
                ground = MapLoader.TileToGround(new Vector2I(tile[0], tile[1]));
                return true;
            }

            GD.PrintErr($"Mapa '{_map.Name}': ponto sem Spot nem Tile no mapa isométrico.");
            return false;
        }

        private void PlaceIsoEntity(Node2D node, Vector2 ground, float scale = IsoEntityScale)
        {
            _isoMap.Sorted.AddChild(node);
            node.Position = ground;
            node.Scale = new Vector2(scale, scale);
        }

        /// <summary>Sobe o desenho de uma entidade parada (NPC) no planalto, deixando a Position
        /// no chão pro Y-sort.</summary>
        private void LiftIsoChildren(Node2D node, Vector2 ground)
        {
            float lift = _isoMap.VisualLift(ground) / node.Scale.Y;

            if (lift != 0)
            {
                foreach (var child in node.GetChildren())
                {
                    if (child is Node2D child2D)
                        child2D.Position += new Vector2(0, -lift);
                    else if (child is Control control)
                        control.Position += new Vector2(0, -lift);
                }
            }

            PivotIsoSort(node, ground);
        }

        /// <summary>Entidade parada no planalto/rampa: põe o desenho num pivô ordenado mais
        /// pra frente (ver MapLoader.SortLift), sem mexer no lugar de nada na tela.</summary>
        private void PivotIsoSort(Node2D node, Vector2 ground)
        {
            float sortLift = _isoMap.SortLift(ground) / node.Scale.Y;

            if (sortLift == 0)
                return;

            var pivot = new Node2D { Name = "SortPivot", Position = new Vector2(0, sortLift) };
            node.AddChild(pivot);
            node.YSortEnabled = true;

            foreach (var child in node.GetChildren())
            {
                if (child != pivot && child is CanvasItem item)
                    item.Reparent(pivot, true);
            }
        }

        private void OpenIsoChest(ExplorationItemVisual visual)
        {
            visual.SetClickable(false);

            if (!_isoItemSprites.TryGetValue(visual, out var closed))
                return;

            var open = _isoMap.MakeKitSprite(visual.Pickup.KitSprite + "_open");
            open.Position = closed.Position;
            closed.GetParent().AddChild(open);

            // Esconde em vez de apagar: o baú fechado é animado e está no relógio de animação
            // do MapLoader, que continuaria trocando o quadro de um sprite já liberado.
            closed.Visible = false;
            _isoItemSprites[visual] = open;
        }

        /// <summary>Explorador num dos 8 blocos em volta do item, no mesmo nível.</summary>
        private bool IsIsoExplorerNextTo(ExplorationItemVisual visual) =>
            IsIsoExplorerNextToTile(MapLoader.GroundToTile(visual.Position));

        /// <summary>Leva o explorador até o bloco livre mais perto (em passos) em volta do item -
        /// o do baú é sólido. Não coleta: é preciso clicar de novo lá do lado.</summary>
        private void WalkExplorerNextTo(ExplorationItemVisual visual) =>
            WalkExplorerNextToTile(MapLoader.GroundToTile(visual.Position));

        /// <summary>Põe o sprite de um Digimon que anda (explorador/selvagem): pé no chão,
        /// erguido no planalto/rampa e, nos mapas isométricos, ordenado por cima do chão alto em
        /// que pisa (o corpo vira Y-sort e o DigimonSprite desce até o ponto de ordenação, com o
        /// desenho subindo de volta). Nos mapas antigos fica como era.</summary>
        public void PlaceWalkerSprite(Node2D body, DigimonSprite sprite)
        {
            float offsetY = GetSpriteOffsetY(body.GlobalPosition);

            if (_isoMap == null)
            {
                sprite.Position = new Vector2(0, offsetY);
                return;
            }

            if (!body.YSortEnabled)
                body.YSortEnabled = true;

            float sortY = _isoMap.SortLift(_isoMap.ToLocal(body.GlobalPosition)) * _isoMap.Scale.Y;
            sprite.Position = new Vector2(0, sortY);
            sprite._sprite.Position = new Vector2(0, offsetY - sortY);
        }

        /// <summary>Item com sprite do kit que tem a versão "_open" (o baú): fica sólido e,
        /// depois de coletado, continua no mapa aberto em vez de sumir.</summary>
        private bool IsIsoChest(string kitSprite)
        {
            return _isoMap != null && !string.IsNullOrEmpty(kitSprite) && _isoMap.HasKitSprite(kitSprite + "_open");
        }

        /// <summary>Baú/item solto com a arte do kit no lugar do ícone do item ('opened': o baú
        /// já coletado, desenhado aberto).</summary>
        private void DressIsoItem(ExplorationItemVisual visual, string kitSprite, bool opened)
        {
            if (string.IsNullOrEmpty(kitSprite))
            {
                LiftIsoChildren(visual, visual.Position);
                return;
            }

            var icon = visual.GetNodeOrNull<Control>("Icon");

            if (icon != null)
                icon.Visible = false;

            bool chest = IsIsoChest(kitSprite);
            float lift = _isoMap.VisualLift(visual.Position);
            var sprite = _isoMap.MakeKitSprite(chest && opened ? kitSprite + "_open" : kitSprite);
            sprite.Position = new Vector2(0, -lift);
            visual.AddChild(sprite);
            _isoItemSprites[visual] = sprite;

            if (chest)
                _isoMap.AddSolidTile(MapLoader.GroundToTile(visual.Position));

            var clickArea = visual.GetNodeOrNull<Area2D>("ClickArea");

            if (clickArea != null)
                clickArea.Position = new Vector2(0, -14 - lift);

            PivotIsoSort(visual, visual.Position);
        }

        /// <summary>Deslocamento vertical do sprite de um Digimon que anda (explorador/selvagem),
        /// em px da tela: põe o pé no chão e sobe no planalto/rampa. 0 nos mapas antigos.</summary>
        public float GetSpriteOffsetY(Vector2 globalPosition)
        {
            if (_isoMap == null)
                return 0f;

            float lift = _isoMap.VisualLift(_isoMap.ToLocal(globalPosition));

            return -(IsoSpriteFootOffset + lift * _isoMap.Scale.Y);
        }

        /// <summary>Dois pontos no mesmo nível (planalto x chão)? Selvagem não persegue nem
        /// encosta em quem está do outro lado de um penhasco. Sempre true nos mapas antigos.</summary>
        public bool AreOnSameLevel(Vector2 globalA, Vector2 globalB)
        {
            if (_isoMap == null)
                return true;

            return _isoMap.AreOnSameLevel(_isoMap.ToLocal(globalA), _isoMap.ToLocal(globalB));
        }

        public bool IsIsoMap => _isoMap != null;

        /// <summary>Destino de vagueio válido pra um selvagem: bloco andável, no mesmo nível do
        /// ponto de spawn dele e fora de rampa.</summary>
        public bool IsValidWanderTarget(Vector2 originGlobal, Vector2 targetGlobal)
        {
            if (_isoMap == null)
                return true;

            var origin = MapLoader.GroundToTile(_isoMap.ToLocal(originGlobal));
            var target = MapLoader.GroundToTile(_isoMap.ToLocal(targetGlobal));

            return _isoMap.IsWalkable(target) && !_isoMap.IsRamp(target) &&
                   _isoMap.Elevation(target) == _isoMap.Elevation(origin);
        }

        /// <summary>Bloco que o jogador quis dizer ao clicar num ponto da tela: o desenho do
        /// planalto fica 18 px acima do chão, então um clique em cima dele aponta pro bloco alto.</summary>
        private Vector2I? IsoTileAtClick(Vector2 globalPosition)
        {
            var local = _isoMap.ToLocal(globalPosition);
            var raised = MapLoader.GroundToTile(local + new Vector2(0, MapLoader.ElevationStepPx));

            if (_isoMap.Elevation(raised) == 1 && _isoMap.IsWalkable(raised))
                return raised;

            var flat = MapLoader.GroundToTile(local);

            return _isoMap.IsWalkable(flat) ? flat : null;
        }

        private bool IsIsoPointWalkable(Vector2 globalPosition)
        {
            return IsoTileAtClick(globalPosition) != null;
        }

        /// <summary>Caminho (pontos globais, centro de cada bloco) do explorador até o bloco
        /// clicado, desviando de floresta/água/penhasco. Null nos mapas antigos (lá ele anda em
        /// linha reta) ou se não há caminho.</summary>
        public List<Vector2> FindWalkPath(Vector2 fromGlobal, Vector2 toGlobal)
        {
            if (_isoMap == null)
                return null;

            var target = IsoTileAtClick(toGlobal);

            if (target == null)
                return null;

            var from = MapLoader.GroundToTile(_isoMap.ToLocal(fromGlobal));
            var tiles = _isoMap.FindPath(from, target.Value);

            if (tiles == null)
                return null;

            var points = new List<Vector2>();

            foreach (var tile in tiles)
                points.Add(_isoMap.ToGlobal(MapLoader.TileToGround(tile)));

            return points;
        }

        private Rect2 GetIsoCameraBounds()
        {
            var bounds = _isoMap.GetGroundBounds();
            var topLeft = _isoMap.ToGlobal(bounds.Position);
            var bottomRight = _isoMap.ToGlobal(bounds.End);

            return new Rect2(topLeft, bottomRight - topLeft);
        }

        /// <summary>Travessia do rio: enquanto o explorador está nas casas do rio liberado, a
        /// balsa vai com ele (um pouco atrás no Y-sort, pra ele ficar em cima dela) e fica onde
        /// ele desembarcar.</summary>
        private void UpdateIsoRaft()
        {
            if (_isoMap == null || _explorerVisual == null)
                return;

            var raft = _isoMap.RaftAt(MapLoader.GroundToTile(_explorerVisual.Position));

            if (raft != null)
                raft.Position = _explorerVisual.Position + new Vector2(0, -1);
        }

        // ---------------------------------------------------------------- bloqueios de quest

        private void ApplyIsoBlockers()
        {
            // DEBUG_ISO_CLEAR_ALL=1: mostra tudo liberado sem mexer no save (screenshots).
            bool debugClearAll = OS.GetEnvironment("DEBUG_ISO_CLEAR_ALL") == "1";
            var completed = GameManager.Instance.Save.Center.CompletedQuestIds;

            foreach (var (blockerId, questId) in _map.BlockerQuests)
            {
                if (!_isoMap.HasBlocker(blockerId))
                {
                    GD.PrintErr($"Mapa '{_map.Name}': bloqueio '{blockerId}' não existe no mapa isométrico.");
                    continue;
                }

                _isoMap.SetBlockerCleared(blockerId, debugClearAll || completed.Contains(questId));
            }
        }

        private void OnQuestTurnedIn(int questId)
        {
            if (_isoMap == null)
                return;

            foreach (var (blockerId, blockerQuest) in _map.BlockerQuests)
            {
                if (blockerQuest == questId && _isoMap.HasBlocker(blockerId))
                    _isoMap.SetBlockerCleared(blockerId, true);
            }

            UpdateMineInteraction();
            OnIsoMapQuestTurnedIn(questId);
        }

        // ---------------------------------------------------------------- passagens

        private ExplorationTransitionData FindTransition(System.Func<ExplorationTransitionData, bool> match)
        {
            foreach (var transition in _map.Transitions)
            {
                if (match(transition))
                    return transition;
            }

            return null;
        }

        private void SpawnMineInteraction()
        {
            var mineTransition = FindTransition(t => t.Mine);

            if (mineTransition == null || _isoMap.MineNode == null)
                return;

            // Área de clique por cima do desenho da mina (o túnel tem ~100x50 px nativos e o
            // ponto de âncora é a base dele).
            // Camada 1 como a ClickArea dos NPCs: o clique do mouse (picking) ignora áreas sem
            // camada nenhuma.
            _mineClickArea = new Area2D { Name = "MineClickArea", CollisionLayer = 1, CollisionMask = 0 };
            _mineClickArea.AddChild(new CollisionShape2D
            {
                Position = new Vector2(0, -20),
                Shape = new RectangleShape2D { Size = new Vector2(90, 50) },
            });

            _mineClickArea.Position = _isoMap.MineNode.Position;
            _isoMap.Sorted.AddChild(_mineClickArea);

            _mineClickArea.InputEvent += (_, @event, _) => OnMineInput(@event, mineTransition);
            _mineTransition = mineTransition;

            // A mina se apresenta como um NPC: a mesma plaquinha de nome, por cima do morro
            // (embaixo ela cobriria o Digimon parado na frente da entrada).
            if (!string.IsNullOrEmpty(mineTransition.Name))
            {
                var template = _npcScene.Instantiate<Node2D>();
                var plate = template.GetNode<Control>("NamePanel");
                template.RemoveChild(plate);
                template.Free();

                plate.GetNode<Label>("NameLabel").Text = mineTransition.Name;

                _mineNamePlate = new Node2D { Name = "MineNamePlate", Scale = new Vector2(IsoEntityScale, IsoEntityScale), ZIndex = 1 };
                _mineNamePlate.Position = _isoMap.MineNode.Position + new Vector2(0, -58);
                _mineNamePlate.AddChild(plate);
                _isoMap.Sorted.AddChild(_mineNamePlate);
            }

            UpdateMineInteraction();
        }

        private void UpdateMineInteraction()
        {
            if (_mineClickArea == null)
                return;

            bool open = _isoMap.MineBlockerId != null && _isoMap.IsBlockerCleared(_isoMap.MineBlockerId);
            _mineClickArea.InputPickable = open;

            if (_mineNamePlate != null)
                _mineNamePlate.Visible = open;

            // Morro da mina sólido (só existe depois do deslizamento; AddSolidTile ignora
            // bloco que já é sólido).
            if (open)
            {
                foreach (var tile in _mineTransition.SolidTiles)
                {
                    if (tile.Count == 2)
                        _isoMap.AddSolidTile(new Vector2I(tile[0], tile[1]));
                }
            }
        }

        private void OnMineInput(InputEvent @event, ExplorationTransitionData transition)
        {
            if (@event is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
                return;

            GetViewport().SetInputAsHandled();

            if (_mineClickArea.GlobalPosition.DistanceTo(_explorerVisual.GlobalPosition) > IsoInteractionRange)
            {
                // Longe: anda até a frente da mina em vez de entrar à distância.
                if (transition.ReturnTile != null && transition.ReturnTile.Count == 2)
                {
                    var front = MapLoader.TileToGround(new Vector2I(transition.ReturnTile[0], transition.ReturnTile[1]));
                    _explorerVisual.MoveTo(_isoMap.ToGlobal(front));
                }

                return;
            }

            AskTransition(transition);
        }

        /// <summary>NPC que é uma passagem (o barqueiro): pergunta se quer ir em vez de abrir o
        /// diálogo. False se esse NPC não é passagem neste mapa.</summary>
        private bool TryOpenNpcTransition(NpcData npc)
        {
            if (_map.Transitions == null)
                return false;

            var transition = FindTransition(t => t.NpcId == npc.Id);

            if (transition == null)
                return false;

            AskTransition(transition);
            return true;
        }

        private void AskTransition(ExplorationTransitionData transition)
        {
            var target = DatabaseManager.Instance.GetExplorationMap(transition.TargetMapId);

            if (target == null)
            {
                GD.PrintErr($"Passagem '{transition.Id}': área de destino {transition.TargetMapId} não existe.");
                return;
            }

            bool unlocked = transition.RequiredQuestId == null ||
                            GameManager.Instance.Save.Center.CompletedQuestIds.Contains(transition.RequiredQuestId.Value);

            if (!unlocked || !GameManager.Instance.IsExplorationMapUnlocked(target))
                return;

            if (_transitionConfirm == null)
            {
                _transitionConfirm = new ConfirmWindow();
                AddChild(_transitionConfirm);
                _transitionConfirm.Confirmed += OnTransitionConfirmed;
            }

            _pendingTransition = transition;

            string message = string.IsNullOrEmpty(transition.ConfirmText)
                ? $"Ir para {target.Name}?"
                : transition.ConfirmText;

            _transitionConfirm.Open(target.Name, message, "Ir");
        }

        private void OnTransitionConfirmed()
        {
            var transition = _pendingTransition;
            _pendingTransition = null;

            if (transition == null)
                return;

            var target = DatabaseManager.Instance.GetExplorationMap(transition.TargetMapId);

            var arrival = new GameManager.ExplorationArrival
            {
                Spot = transition.ArrivalSpot,
                Tile = transition.ArrivalTile,
            };

            var returnPoint = new GameManager.ExplorationReturnPoint
            {
                MapId = _map.Id,
                Tile = transition.ReturnTile,
            };

            GameManager.Instance.TravelExploration(target, arrival, returnPoint);
        }

        /// <summary>Área aberta por uma passagem: botão pra voltar à área de origem, na frente
        /// da passagem (o 🚪 Sair continua levando pro centro de treinamento).</summary>
        private void AddReturnButton()
        {
            var returnPoint = GameManager.Instance.ExplorationReturn;

            if (returnPoint == null)
                return;

            var origin = DatabaseManager.Instance.GetExplorationMap(returnPoint.MapId);

            if (origin == null)
                return;

            var exitButton = GetNode<Button>("CanvasLayer/ExitButton");

            var button = new Button
            {
                Text = $"↩ {origin.Name}",
                OffsetTop = exitButton.OffsetTop,
                OffsetBottom = exitButton.OffsetBottom,
                OffsetRight = exitButton.OffsetLeft - 8,
                OffsetLeft = exitButton.OffsetLeft - 8 - 200,
            };

            button.AddThemeStyleboxOverride("normal", exitButton.GetThemeStylebox("normal"));
            exitButton.GetParent().AddChild(button);

            button.Pressed += () => GameManager.Instance.TravelExploration(
                origin,
                new GameManager.ExplorationArrival { Tile = returnPoint.Tile },
                null);
        }
    }
}
