using Godot;
using ProjetoDC.Enums;
using ProjetoDC.Scripts.Data;
using ProjetoDC.Scripts.Managers;
using ProjetoDC.Scripts.UI;
using System.Collections.Generic;
using System.Linq;

namespace ProjetoDC.Scripts.Gameplay
{
    /// <summary>
    /// O que os mapas isométricos da caverna acrescentam à área de exploração:
    /// - o escuro: o MapLoader monta o CanvasModulate e as luzes; aqui só se decide o raio da luz
    ///   do Digimon (sem bônus / Digimon de fogo / lanterna / os dois - ver UpdateIsoVision);
    /// - as quests do mapa ("q_energia" no JSON <-> Id em Data/Quests, ExplorationMapData.MapQuestIds):
    ///   luzes "after:" e objetos "quest:" acendem quando a quest está concluída;
    /// - objetos que o jogador acende/liga (ExplorationMapData.Switches: lanternas apagadas, caixas
    ///   de junção) - estado em CenterState.ExplorationSwitchesOn, contado pelas quests SwitchOn;
    /// - passagens em que se entra andando (ExplorationTransitionData.Spot: as bocas da caverna).
    /// Nada disso existe na floresta (MapQuestIds/Switches vazios, sem "vision"), que fica igual.
    /// </summary>
    public partial class ExplorationArea
    {
        // Fundo atrás do mapa (fora dele): o da prévia, já escurecido pelo ambiente.
        private static readonly Color DarkPreviewBackground = Color.Color8(12, 10, 18);

        private readonly Dictionary<string, ExplorationSwitchData> _switchData = new();
        private ConfirmWindow _infoWindow;
        private Vector2I? _lastWalkIntoTile;

        private bool IsExplorerFire => _explorer?.BaseData != null && _explorer.BaseData.Element == DigimonElement.Fire;

        /// <summary>Dá pra acender lanterna: tem a lanterna ou explora com um Digimon de fogo.</summary>
        public bool HasFireOrLantern => IsExplorerFire || GameManager.Instance.HasLantern;

        // ---------------------------------------------------------------- estado do mapa

        /// <summary>Quests do mapa concluídas (luzes "after:", terminais "quest:") e objetos
        /// já acesos/ligados, lidos do save. DEBUG_ISO_CLEAR_ALL=1 mostra tudo pronto.</summary>
        private void ApplyIsoMapState()
        {
            bool debugClearAll = OS.GetEnvironment("DEBUG_ISO_CLEAR_ALL") == "1";
            var completed = GameManager.Instance.Save.Center.CompletedQuestIds;

            foreach (var (mapQuestId, questId) in _map.MapQuestIds)
                _isoMap.SetMapQuestDone(mapQuestId, debugClearAll || completed.Contains(questId));

            foreach (var sw in _map.Switches)
            {
                if (!_isoMap.HasSwitchable(sw.Id))
                {
                    GD.PrintErr($"Mapa '{_map.Name}': objeto '{sw.Id}' (Switches) não existe no mapa isométrico.");
                    continue;
                }

                _switchData[sw.Id] = sw;
                _isoMap.SetSwitchedOn(sw.Id, debugClearAll || GameManager.Instance.IsExplorationSwitchOn(_map.Id, sw.Id));
            }
        }

        private void OnIsoMapQuestTurnedIn(int questId)
        {
            foreach (var (mapQuestId, id) in _map.MapQuestIds)
            {
                if (id == questId)
                    _isoMap.SetMapQuestDone(mapQuestId, true);
            }

            // Quest SwitchOn entregue (inclusive pelo diálogo): os objetos dela ficam ligados.
            foreach (var sw in _switchData.Values)
            {
                if (GameManager.Instance.IsExplorationSwitchOn(_map.Id, sw.Id))
                    _isoMap.SetSwitchedOn(sw.Id, true);
            }

            UpdateIsoVision();
        }

        // ---------------------------------------------------------------- o escuro

        private void InitIsoDarkness()
        {
            if (!_isoMap.HasDarkness)
                return;

            var ambient = _isoMap.AmbientColor ?? Colors.Black;
            _background.Color = new Color(
                DarkPreviewBackground.R * ambient.R,
                DarkPreviewBackground.G * ambient.G,
                DarkPreviewBackground.B * ambient.B);

            UpdateIsoVision(animate: false);
        }

        /// <summary>Raio da luz do Digimon: sem bônus, Digimon de fogo explorando, lanterna (quest
        /// do minerador) ou os dois. Recalculado ao entregar quest e depois de cada batalha (o
        /// explorador pode ter evoluído pra outro elemento). DEBUG_ISO_VISION força um estado.</summary>
        private void UpdateIsoVision(bool animate = true)
        {
            if (_isoMap == null || !_isoMap.HasDarkness)
                return;

            string forced = OS.GetEnvironment("DEBUG_ISO_VISION");
            string state = !string.IsNullOrEmpty(forced)
                ? forced
                : MapLoader.VisionStateFor(IsExplorerFire, GameManager.Instance.HasLantern);

            _isoMap.SetVisionState(state, animate);
        }

        // ---------------------------------------------------------------- objetos que acendem/ligam

        private void SpawnSwitchInteractions()
        {
            foreach (var sw in _switchData.Values)
            {
                var node = _isoMap.SwitchNode(sw.Id);

                if (node == null)
                    continue;

                float lift = _isoMap.VisualLift(MapLoader.TileToGround(_isoMap.SwitchTile(sw.Id)));

                // Camada 1 como a ClickArea dos NPCs: o clique do mouse ignora áreas sem camada.
                var area = new Area2D { Name = "SwitchClickArea", CollisionLayer = 1, CollisionMask = 0 };
                area.AddChild(new CollisionShape2D
                {
                    Position = new Vector2(0, -22 - lift),
                    Shape = new RectangleShape2D { Size = new Vector2(34, 52) },
                });
                node.AddChild(area);

                var data = sw;
                area.InputEvent += (_, @event, _) => OnSwitchInput(@event, data);
            }
        }

        private void OnSwitchInput(InputEvent @event, ExplorationSwitchData sw)
        {
            if (@event is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
                return;

            GetViewport().SetInputAsHandled();

            if (_explorerVisual == null || _isoMap.IsSwitchedOn(sw.Id))
                return;

            var tile = _isoMap.SwitchTile(sw.Id);

            // Longe: só leva o explorador pro lado (o bloco do objeto é sólido), como no baú.
            if (!IsIsoExplorerNextToTile(tile))
            {
                WalkExplorerNextToTile(tile);
                return;
            }

            TryUseSwitch(sw.Id);
        }

        /// <summary>Acende/liga o objeto (o explorador já está do lado): confere o fogo, troca o
        /// sprite, acende a luz, guarda no save e, se uma quest SwitchOn aceita ficou pronta,
        /// entrega na hora. False se não deu (já ligado, falta fogo, id desconhecido).</summary>
        public bool TryUseSwitch(string switchId)
        {
            if (!_switchData.TryGetValue(switchId, out var sw) || _isoMap.IsSwitchedOn(switchId))
                return false;

            if (sw.NeedsFire && !HasFireOrLantern)
            {
                ShowIsoInfo("🕯 Escuro demais", string.IsNullOrEmpty(sw.NoFireText)
                    ? "Está escuro demais... falta fogo pra acender isto."
                    : sw.NoFireText);
                return false;
            }

            var game = GameManager.Instance;
            game.SetExplorationSwitchOn(_map.Id, switchId);
            _isoMap.SetSwitchedOn(switchId, true);

            string title = string.IsNullOrEmpty(sw.Name) ? "Pronto!" : sw.Name;
            var lines = new List<string>();

            if (!string.IsNullOrEmpty(sw.OnText))
                lines.Add(sw.OnText);

            foreach (var quest in ActiveSwitchQuestsFor(switchId))
            {
                if (game.IsQuestReadyToTurnIn(quest.Id) && game.TryTurnInQuest(quest.Id).Success)
                {
                    title = $"✅ {quest.Name}";
                    lines.Add(string.IsNullOrEmpty(quest.CompletionText) ? "Quest concluída!" : quest.CompletionText);
                }
                else
                {
                    lines.Add($"{quest.Name}: {game.GetQuestProgress(quest.Id)}/{quest.ObjectiveCount}");
                }
            }

            ShowIsoInfo(title, string.Join("\n\n", lines));
            return true;
        }

        private IEnumerable<QuestData> ActiveSwitchQuestsFor(string switchId)
        {
            var center = GameManager.Instance.Save.Center;

            return center.ActiveQuestIds
                .ToList()
                .Select(id => DatabaseManager.Instance.GetQuest(id))
                .Where(q => q != null && q.ObjectiveType == QuestObjectiveType.SwitchOn &&
                            q.ObjectiveMapId == _map.Id && q.ObjectiveSwitchIds.Contains(switchId));
        }

        private void ShowIsoInfo(string title, string message)
        {
            if (_infoWindow == null)
            {
                _infoWindow = new ConfirmWindow(hasCancel: false);
                AddChild(_infoWindow);
            }

            _infoWindow.Open(title, message, "OK");
        }

        // ---------------------------------------------------------------- passagens andando

        private void SpawnWalkIntoTransitions()
        {
            foreach (var transition in _map.Transitions.Where(t => !string.IsNullOrEmpty(t.Spot)))
            {
                if (!_isoMap.TryGetSpot(transition.Spot, out var spot))
                {
                    GD.PrintErr($"Mapa '{_map.Name}': passagem '{transition.Id}': spot '{transition.Spot}' não existe.");
                    continue;
                }

                // Clicar na boca (objeto de mesmo id no mapa) leva o explorador até o spot, onde
                // a passagem acontece ao pisar.
                var node = _isoMap.GetObjectNode(transition.Spot);

                if (node == null)
                    continue;

                var area = new Area2D { Name = "TransitionClickArea", CollisionLayer = 1, CollisionMask = 0 };
                area.AddChild(new CollisionShape2D
                {
                    Position = new Vector2(0, -30),
                    Shape = new RectangleShape2D { Size = new Vector2(64, 60) },
                });
                node.AddChild(area);

                var target = spot.Tile;
                area.InputEvent += (_, @event, _) =>
                {
                    if (@event is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
                        return;

                    GetViewport().SetInputAsHandled();
                    WalkExplorerTo(target);
                };
            }

            // Quem chega já em cima de uma passagem não dispara ela até sair do bloco.
            if (_explorerVisual != null)
                _lastWalkIntoTile = MapLoader.GroundToTile(_explorerVisual.Position);
        }

        private void UpdateWalkIntoTransitions()
        {
            if (_isoMap == null || _explorerVisual == null || _map.Transitions.Count == 0)
                return;

            var tile = MapLoader.GroundToTile(_explorerVisual.Position);

            if (tile == _lastWalkIntoTile)
                return;

            _lastWalkIntoTile = tile;

            foreach (var transition in _map.Transitions)
            {
                if (!string.IsNullOrEmpty(transition.Spot) && _isoMap.TryGetSpot(transition.Spot, out var spot) && spot.Tile == tile)
                {
                    OnWalkIntoTransition(transition);
                    return;
                }
            }
        }

        private void OnWalkIntoTransition(ExplorationTransitionData transition)
        {
            bool unlocked = transition.RequiredQuestId == null ||
                            GameManager.Instance.Save.Center.CompletedQuestIds.Contains(transition.RequiredQuestId.Value);

            if (!unlocked)
                return;

            // Sem destino ainda (saída norte da caverna): só o aviso.
            if (transition.TargetMapId == 0)
            {
                ShowIsoInfo("🚧 Caminho bloqueado", string.IsNullOrEmpty(transition.ComingSoonText)
                    ? "O caminho adiante ainda está bloqueado. Em breve!"
                    : transition.ComingSoonText);
                return;
            }

            AskTransition(transition);
        }

        // ---------------------------------------------------------------- andar até um bloco

        /// <summary>Explorador num dos 8 blocos em volta, no mesmo nível.</summary>
        private bool IsIsoExplorerNextToTile(Vector2I target)
        {
            var explorer = MapLoader.GroundToTile(_explorerVisual.Position);
            var d = explorer - target;

            return Mathf.Abs(d.X) <= 1 && Mathf.Abs(d.Y) <= 1 &&
                   _isoMap.AreOnSameLevel(_explorerVisual.Position, MapLoader.TileToGround(target));
        }

        /// <summary>Leva o explorador até o bloco livre mais perto (em passos) em volta de um bloco
        /// sólido (baú, lanterna, caixa de junção).</summary>
        private void WalkExplorerNextToTile(Vector2I target)
        {
            var from = MapLoader.GroundToTile(_explorerVisual.Position);
            var targetGround = MapLoader.TileToGround(target);
            List<Vector2I> best = null;

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    var tile = target + new Vector2I(dx, dy);

                    if ((dx == 0 && dy == 0) || !_isoMap.IsWalkable(tile) ||
                        !_isoMap.AreOnSameLevel(MapLoader.TileToGround(tile), targetGround))
                        continue;

                    var path = tile == from ? new List<Vector2I>() : _isoMap.FindPath(from, tile);

                    if (path != null && (best == null || path.Count < best.Count))
                        best = path;
                }
            }

            FollowTiles(best);
        }

        /// <summary>Leva o explorador até um bloco andável (caminho por blocos).</summary>
        private void WalkExplorerTo(Vector2I target)
        {
            FollowTiles(_isoMap.FindPath(MapLoader.GroundToTile(_explorerVisual.Position), target));
        }

        private void FollowTiles(List<Vector2I> tiles)
        {
            if (tiles == null || tiles.Count == 0)
                return;

            var points = new List<Vector2>();

            foreach (var tile in tiles)
                points.Add(_isoMap.ToGlobal(MapLoader.TileToGround(tile)));

            _explorerVisual.FollowPath(points);
        }
    }
}
