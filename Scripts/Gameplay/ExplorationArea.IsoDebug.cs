using Godot;
using System;
using System.Collections.Generic;

namespace ProjetoDC.Scripts.Gameplay
{
    /// <summary>
    /// Teste de colisão do mapa isométrico com a física de verdade (DEBUG_ISO_WALKTEST=1, junto
    /// com DEBUG_OPEN_SCREEN=explorationarea): "dirige" o explorador como se fosse o teclado, em
    /// direções aleatórias, começando perto de bordas de floresta/água, penhascos, rampas e
    /// bloqueios de quest, e confere a cada passo de física que ele só passou de um bloco pro
    /// outro por onde MapLoader.CanStep deixa. Sem selvagens (não interrompe com batalha). Ao
    /// final escreve o resumo no log e fecha o jogo. DEBUG_ISO_CLEAR_ALL=1 testa com tudo liberado.
    /// </summary>
    public partial class ExplorationArea
    {
        private const double WalkTestSegmentSeconds = 4.0;
        private const double WalkTestTurnSeconds = 0.35;

        private static readonly Vector2I[] WalkTestStarts =
        {
            new(58, 35), new(58, 46), new(59, 46), new(7, 8), new(8, 8), new(73, 8), new(73, 9),
            new(19, 21), new(17, 21), new(65, 17), new(65, 15), new(44, 45), new(44, 43),
            new(15, 19), new(12, 17), new(64, 12), new(42, 42), new(49, 23), new(70, 33),
            new(43, 55), new(24, 12), new(36, 4), new(69, 11), new(10, 12), new(24, 38),
        };

        private bool _walkTest;
        private int _walkTestIndex = -1;
        private double _walkTestSegmentTime;
        private double _walkTestTurnTime;
        private Vector2I _walkTestLastTile;
        private int _walkTestSteps;
        private int _walkTestViolations;
        private readonly Random _walkTestRandom = new(12345);
        private readonly List<string> _walkTestReport = new();
        private double _fpsSum;
        private int _fpsSamples;
        private double _fpsMin = double.MaxValue;

        private bool IsWalkTestEnabled => OS.GetEnvironment("DEBUG_ISO_WALKTEST") == "1" || IsPathTestEnabled;

        // DEBUG_ISO_PATHTEST=1: em vez de andar a esmo, clica (MoveTo) em destinos fixos a partir
        // do spawn e confere que o explorador chega - rampas, corredores estreitos e, com
        // DEBUG_ISO_CLEAR_ALL=1, as áreas liberadas por quest (balsa, clareira, mina). Roda em
        // velocidade 4x (com 4x mais passos de física, pra não atravessar parede).
        private bool IsPathTestEnabled => OS.GetEnvironment("DEBUG_ISO_PATHTEST") == "1";

        private static readonly Vector2I[] PathTestTargets =
        {
            new(15, 19), new(13, 22), new(64, 12), new(66, 13), new(42, 42), new(44, 41), new(43, 55),
            new(36, 4), new(24, 12), new(12, 47), new(10, 12), new(69, 11), new(62, 54), new(91, 11), new(7, 5),
        };

        private int _pathTestIndex = -1;
        private double _pathTestTime;
        private int _pathTestFailures;

        public override void _PhysicsProcess(double delta)
        {
            if (!_walkTest || _isoMap == null || _explorerVisual == null)
                return;

            if (IsPathTestEnabled)
            {
                ProcessPathTest(delta);
                return;
            }

            double fps = Engine.GetFramesPerSecond();

            if (_walkTestIndex >= 0 && fps > 0)
            {
                _fpsSum += fps;
                _fpsSamples++;
                _fpsMin = Math.Min(_fpsMin, fps);
            }

            if (_walkTestIndex < 0 || (_walkTestSegmentTime -= delta) <= 0)
            {
                _walkTestIndex++;

                if (_walkTestIndex >= WalkTestStarts.Length)
                {
                    FinishWalkTest();
                    return;
                }

                var start = WalkTestStarts[_walkTestIndex];

                _explorerVisual.Position = MapLoader.TileToGround(start);
                _explorerVisual.DebugDirection = Vector2.Zero;
                _walkTestLastTile = start;
                _walkTestSegmentTime = WalkTestSegmentSeconds;
                _walkTestTurnTime = 0;

                if (!_isoMap.IsWalkable(start))
                    _walkTestReport.Add($"início {start} não é andável (ignorado)");

                return;
            }

            if ((_walkTestTurnTime -= delta) <= 0)
            {
                _walkTestTurnTime = WalkTestTurnSeconds;
                float angle = (float)(_walkTestRandom.NextDouble() * Mathf.Tau);
                _explorerVisual.DebugDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            }

            var tile = MapLoader.GroundToTile(_explorerVisual.Position);

            if (tile == _walkTestLastTile)
                return;

            _walkTestSteps++;

            if (!IsLegalWalkTestStep(_walkTestLastTile, tile))
            {
                _walkTestViolations++;

                if (_walkTestReport.Count < 40)
                    _walkTestReport.Add($"passo ilegal {_walkTestLastTile} -> {tile} (elev {_isoMap.Elevation(_walkTestLastTile)}->{_isoMap.Elevation(tile)}, andável={_isoMap.IsWalkable(tile)})");
            }

            _walkTestLastTile = tile;
        }

        // Um passo de física pode cruzar a quina entre dois blocos e pular na diagonal: vale se
        // algum dos dois caminhos de 4-vizinhos pela quina é permitido.
        private bool IsLegalWalkTestStep(Vector2I a, Vector2I b)
        {
            var d = b - a;

            if (Math.Abs(d.X) + Math.Abs(d.Y) == 1)
                return _isoMap.CanStep(a, b);

            if (Math.Abs(d.X) == 1 && Math.Abs(d.Y) == 1)
            {
                var viaX = new Vector2I(b.X, a.Y);
                var viaY = new Vector2I(a.X, b.Y);

                return (_isoMap.CanStep(a, viaX) && _isoMap.CanStep(viaX, b)) ||
                       (_isoMap.CanStep(a, viaY) && _isoMap.CanStep(viaY, b));
            }

            return false;
        }

        // Destino que virou sólido (baú, pedra...) troca pelo primeiro vizinho livre do mesmo nível.
        private Vector2I PathTestTarget(int index)
        {
            var target = PathTestTargets[index];

            if (_isoMap.IsWalkable(target))
                return target;

            foreach (var d in new[] { new Vector2I(1, 0), new Vector2I(0, 1), new Vector2I(-1, 0), new Vector2I(0, -1) })
            {
                var near = target + d;

                if (_isoMap.IsWalkable(near) && !_isoMap.IsRamp(near) && _isoMap.Elevation(near) == _isoMap.Elevation(target))
                    return near;
            }

            return target;
        }

        private void ProcessPathTest(double delta)
        {
            if (_pathTestIndex < 0)
            {
                Engine.PhysicsTicksPerSecond = 240;
                Engine.TimeScale = 4;
                Engine.MaxPhysicsStepsPerFrame = 32;
            }

            var tile = MapLoader.GroundToTile(_explorerVisual.Position);
            bool arrived = _pathTestIndex >= 0 && tile == PathTestTarget(_pathTestIndex);

            if (_pathTestIndex >= 0 && !arrived && (_pathTestTime += delta) < 90)
                return;

            if (_pathTestIndex >= 0)
            {
                var target = PathTestTarget(_pathTestIndex);
                bool reachableNow = _isoMap.FindPath(MapLoader.GroundToTile(MapLoader.TileToGround(new Vector2I(58, 35))), target) != null;

                if (arrived)
                    GD.Print($"[ISO-PATHTEST] chegou em {target} em {_pathTestTime:0.0}s");
                else if (!reachableNow)
                    GD.Print($"[ISO-PATHTEST] {target} sem caminho (bloqueado por quest, esperado sem DEBUG_ISO_CLEAR_ALL)");
                else
                {
                    _pathTestFailures++;
                    GD.Print($"[ISO-PATHTEST] FALHOU: não chegou em {target}, parou em {tile}");
                }
            }

            _pathTestIndex++;
            _pathTestTime = 0;

            if (_pathTestIndex >= PathTestTargets.Length)
            {
                GD.Print($"[ISO-PATHTEST] {(_pathTestFailures == 0 ? "OK" : "FALHOU")}: {_pathTestFailures} falha(s)");
                GetTree().Quit();
                return;
            }

            _explorerVisual.Position = MapLoader.TileToGround(new Vector2I(58, 35));
            // Clica onde o bloco aparece na tela (no planalto, 18 px acima do chão), como o jogador.
            var ground = MapLoader.TileToGround(PathTestTarget(_pathTestIndex));
            _explorerVisual.MoveTo(_isoMap.ToGlobal(ground - new Vector2(0, _isoMap.VisualLift(ground))));
        }

        // ---------------------------------------------------------------- fluxo de quests

        // DEBUG_ISO_FLOWTEST=1 (com DEBUG_OPEN_SCREEN=explorationarea DEBUG_EXPLORATION_MAP=1, num
        // save sem as quests da floresta): faz as 3 quests pelo GameManager de verdade (aceitar,
        // entregar, pegar o chip), confere que cada bloqueio abre e os itens entram/saem, entra
        // na mina, volta pelo botão de voltar (área nova lê o estado das quests de novo e põe o
        // explorador na frente da mina) e pega o barqueiro até o pântano. Mexe no save EM
        // MEMÓRIA - faça backup e não deixe o jogo salvar depois. Etapa estática porque cada
        // troca de área cria outra ExplorationArea.
        private static int _flowStage;
        private static int _flowFailures;

        private static bool IsFlowTestEnabled => OS.GetEnvironment("DEBUG_ISO_FLOWTEST") == "1";

        private void FlowCheck(bool condition, string message)
        {
            if (!condition)
                _flowFailures++;

            GD.Print($"[ISO-FLOWTEST] {(condition ? "ok" : "FALHOU")}: {message}");
        }

        private void RunFlowTestStep()
        {
            if (OS.GetEnvironment("DEBUG_ISO_CHESTTEST") == "1" && _isoMap != null)
                RunChestTest();

            if (!IsFlowTestEnabled)
                return;

            var game = Managers.GameManager.Instance;
            var center = game.Save.Center;

            switch (_flowStage)
            {
                case 0:
                {
                    _flowStage = 1;

                    foreach (var id in _isoMap.BlockerIds)
                        FlowCheck(!_isoMap.IsBlockerCleared(id), $"começa com '{id}' fechado");

                    FlowCheck(!_mineClickArea.InputPickable, "mina ainda não é clicável");

                    // Lenhador (derrotar 1 Agumon): a vitória é simulada direto no progresso.
                    FlowCheck(game.TryAcceptQuest(27).Success, "aceita Toras no Caminho");
                    FlowCheck(!game.IsQuestReadyToTurnIn(27), "sem vencer um Agumon, Toras no Caminho não pode ser entregue");
                    center.QuestProgress[27] = 1;
                    FlowCheck(game.IsQuestReadyToTurnIn(27), "Toras no Caminho pronta depois de vencer um Agumon");
                    FlowCheck(game.TryTurnInQuest(27).Success, "entrega Toras no Caminho");
                    FlowCheck(_isoMap.IsBlockerCleared("bloqueio_arvores"), "toras viram tocos (bloqueio_arvores aberto)");
                    FlowCheck(center.GetItemQuantity(13) == 1, "ganhou 1 Tábuas");

                    // Escavador antes do chip: não pode entregar.
                    FlowCheck(game.TryAcceptQuest(29).Success, "aceita O Túnel Soterrado");
                    FlowCheck(!game.IsQuestReadyToTurnIn(29), "sem o chip, o túnel não pode ser entregue");

                    // Jangada: as tábuas que já estavam no inventário contam.
                    FlowCheck(game.TryAcceptQuest(28).Success, "aceita A Jangada");
                    FlowCheck(game.IsQuestReadyToTurnIn(28), "A Jangada pronta (tábuas no inventário)");
                    FlowCheck(game.TryTurnInQuest(28).Success, "entrega A Jangada");
                    FlowCheck(center.GetItemQuantity(13) == 0, "as tábuas foram consumidas");
                    FlowCheck(_isoMap.IsBlockerCleared("bloqueio_rio"), "balsa aparece (bloqueio_rio aberto)");

                    var chip = _map.ItemPickups.Find(pk => pk.ItemId == 14);
                    FlowCheck(chip != null && game.CollectExplorationItem(chip).Success, "pega o Chip de Escavação");
                    FlowCheck(game.IsQuestReadyToTurnIn(29), "com o chip, O Túnel Soterrado pronto");
                    FlowCheck(game.TryTurnInQuest(29).Success, "entrega O Túnel Soterrado");
                    FlowCheck(center.GetItemQuantity(14) == 0, "o chip foi consumido");
                    FlowCheck(_isoMap.IsBlockerCleared("bloqueio_deslizamento"), "deslizamento some (bloqueio_deslizamento aberto)");
                    FlowCheck(_isoMap.MineNode.Visible && _mineClickArea.InputPickable, "a mina aparece e é clicável");

                    var cave = Managers.DatabaseManager.Instance.GetExplorationMap(2);
                    var swamp = Managers.DatabaseManager.Instance.GetExplorationMap(3);
                    FlowCheck(game.IsExplorationMapUnlocked(cave) && game.IsExplorationMapUnlocked(swamp), "Caverna e Pântano liberados na lista");

                    // Entra na mina (o mesmo caminho do clique: pergunta, confirma).
                    AskTransition(FindTransition(t => t.Mine));
                    FlowCheck(_transitionConfirm != null && _transitionConfirm.IsOpen, "a mina pergunta se quer entrar");
                    OnTransitionConfirmed();
                    break;
                }
                case 1:
                {
                    _flowStage = 2;
                    FlowCheck(_map.Id == 2, $"chegou na Caverna (área {_map.Id})");

                    var back = GetNode("CanvasLayer").GetChildren();
                    Button returnButton = null;

                    foreach (var child in back)
                    {
                        if (child is Button b && b.Text.StartsWith("↩"))
                            returnButton = b;
                    }

                    FlowCheck(returnButton != null, "a caverna tem o botão de voltar pra floresta");
                    returnButton?.EmitSignal(BaseButton.SignalName.Pressed);
                    break;
                }
                case 2:
                {
                    _flowStage = 3;
                    FlowCheck(_map.Id == 1 && _isoMap != null, "voltou pra Floresta");

                    foreach (var id in _isoMap.BlockerIds)
                        FlowCheck(_isoMap.IsBlockerCleared(id), $"'{id}' continua aberto (lido das quests concluídas)");

                    FlowCheck(MapLoader.GroundToTile(_explorerVisual.Position) == new Vector2I(7, 5), $"voltou na frente da mina ({MapLoader.GroundToTile(_explorerVisual.Position)})");

                    var boatman = Managers.DatabaseManager.Instance.GetNpc(32);
                    FlowCheck(TryOpenNpcTransition(boatman), "o barqueiro pergunta se quer ir pro pântano");
                    OnTransitionConfirmed();
                    break;
                }
                case 3:
                {
                    _flowStage = 4;
                    FlowCheck(_map.Id == 3, $"chegou no Pântano (área {_map.Id})");
                    GD.Print($"[ISO-FLOWTEST] {(_flowFailures == 0 ? "OK" : "FALHOU")}: {_flowFailures} falha(s)");
                    GetTree().Quit();
                    break;
                }
            }
        }

        // ---------------------------------------------------------------- baú

        // DEBUG_ISO_CHESTTEST=1 (com DEBUG_OPEN_SCREEN=explorationarea DEBUG_EXPLORATION_MAP=1,
        // num save sem o baú 1 coletado): clique de longe só leva o explorador pro lado do baú,
        // clique do lado coleta, troca pelo baú aberto e mostra a janela; o bloco é sólido.
        // Mexe no save EM MEMÓRIA - backup antes.
        private async void RunChestTest()
        {
            int failures = 0;

            void Check(bool condition, string message)
            {
                if (!condition)
                    failures++;

                GD.Print($"[ISO-CHESTTEST] {(condition ? "ok" : "FALHOU")}: {message}");
            }

            ExplorationItemVisual chest = null;

            foreach (var visual in _isoItemSprites.Keys)
            {
                if (visual.Pickup.UniqueId == 1)
                    chest = visual;
            }

            Check(chest != null, "baú 1 está no mapa");

            if (chest == null)
            {
                GetTree().Quit();
                return;
            }

            var chestTile = MapLoader.GroundToTile(chest.Position);
            Check(!_isoMap.IsWalkable(chestTile), $"bloco do baú {chestTile} é sólido");

            var center = Managers.GameManager.Instance.Save.Center;
            int before = center.GetItemQuantity(chest.Pickup.ItemId);

            Check(!IsIsoExplorerNextTo(chest), "explorador começa longe do baú");
            OnItemClicked(chest);
            Check(!center.CollectedItemPickupIds.Contains(1), "clique de longe NÃO coleta");

            for (int i = 0; i < 600 && !IsIsoExplorerNextTo(chest); i++)
                await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

            Check(IsIsoExplorerNextTo(chest), $"clique de longe levou o explorador pro lado do baú ({MapLoader.GroundToTile(_explorerVisual.Position)})");

            for (int i = 0; i < 60; i++)
                await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

            OnItemClicked(chest);
            Check(center.CollectedItemPickupIds.Contains(1), "clique do lado coleta");
            Check(center.GetItemQuantity(chest.Pickup.ItemId) == before + chest.Pickup.Quantity, "item entrou no inventário");
            Check(IsInstanceValid(chest) && _isoItemSprites[chest] != null, "baú continua no mapa");
            Check(_itemFoundWindow != null && _itemFoundWindow.IsOpen, "janela do item aberta");

            for (int i = 0; i < 30; i++)
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            if (OS.GetEnvironment("DEBUG_ISO_CHESTTEST_SHOT") == "1")
            {
                GetViewport().GetTexture().GetImage().SavePng("user://iso_chest_window.png");
                _itemFoundWindow.Close();

                for (int i = 0; i < 10; i++)
                    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

                GetViewport().GetTexture().GetImage().SavePng("user://iso_chest_open.png");
            }

            GD.Print($"[ISO-CHESTTEST] {(failures == 0 ? "OK" : "FALHOU")}: {failures} falha(s)");
            GetTree().Quit();
        }

        private void FinishWalkTest()
        {
            _walkTest = false;

            foreach (var line in _walkTestReport)
                GD.Print($"[ISO-WALKTEST] {line}");

            GD.Print($"[ISO-WALKTEST] {(_walkTestViolations == 0 ? "OK" : "FALHOU")}: {_walkTestSteps} trocas de bloco, " +
                     $"{_walkTestViolations} ilegais, {WalkTestStarts.Length} pontos de partida. " +
                     $"FPS médio {(_fpsSamples > 0 ? _fpsSum / _fpsSamples : 0):0.0}, mínimo {(_fpsMin == double.MaxValue ? 0 : _fpsMin):0.0}");

            GetTree().Quit();
        }
    }
}
