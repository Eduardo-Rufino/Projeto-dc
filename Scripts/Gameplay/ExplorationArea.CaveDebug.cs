using Godot;
using ProjetoDC.Scripts.Managers;
using ProjetoDC.Scripts.Save;
using System.Linq;
using System.Text.Json;

namespace ProjetoDC.Scripts.Gameplay
{
    /// <summary>
    /// Testes da caverna (mapa isométrico escuro), com DEBUG_OPEN_SCREEN=explorationarea
    /// DEBUG_EXPLORATION_MAP=2. Mexem no save EM MEMÓRIA - backup antes, e não deixe o jogo salvar.
    ///
    /// - DEBUG_ISO_SHOT=nome: depois de DEBUG_ISO_SHOT_FRAMES quadros (padrão 90) salva a tela em
    ///   user://iso_shot_{nome}.png e fecha (prints; combine com DEBUG_EXPLORATION_TILE,
    ///   DEBUG_ISO_VISION, DEBUG_ISO_CLEAR_ALL, DEBUG_ISO_NO_WILDS e DEBUG_ISO_SHOT_BATTLE=1, que
    ///   abre uma batalha contra um selvagem da área antes do print).
    /// - DEBUG_ISO_CAVETEST=1 (num save sem as quests da caverna, explorando com um Digimon que NÃO
    ///   é de fogo - ex.: DEBUG_EXPLORER_CODE=tentomon): faz as 3 quests pelo jogo de verdade
    ///   (lanterna sem fogo recusa, item da lanterna, caixas de junção com a quest terminando
    ///   sozinha, lanternas, barreira), confere visual/luzes/passagens/estado de visão, a saída
    ///   norte "em breve", o save (ida e volta do JSON), sai pela boca da caverna pra floresta e
    ///   volta pelo botão, conferindo que tudo continua aberto/aceso.
    /// </summary>
    public partial class ExplorationArea
    {
        private static int _caveStage;
        private static int _caveFailures;

        private void CaveCheck(bool condition, string message)
        {
            if (!condition)
                _caveFailures++;

            GD.Print($"[ISO-CAVETEST] {(condition ? "ok" : "FALHOU")}: {message}");
        }

        private async void RunIsoShot()
        {
            string name = OS.GetEnvironment("DEBUG_ISO_SHOT");
            int frames = int.TryParse(OS.GetEnvironment("DEBUG_ISO_SHOT_FRAMES"), out int f) ? f : 90;

            for (int i = 0; i < frames; i++)
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            // DEBUG_ISO_SHOT_BATTLE=1: o print é da batalha contra um selvagem da área (confere
            // que a arena não herda o escuro do mapa), depois de mais uns quadros.
            if (OS.GetEnvironment("DEBUG_ISO_SHOT_BATTLE") == "1" && _map.WildPool.Count > 0)
            {
                GameManager.Instance.StartWildEncounter(_explorer, _map.WildPool[0], _map.WildLevelMin);

                for (int i = 0; i < frames; i++)
                    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }

            string path = ProjectSettings.GlobalizePath($"user://iso_shot_{name}.png");
            GetViewport().GetTexture().GetImage().SavePng(path);
            GD.Print($"[ISO-SHOT] salvo {path}");
            GetTree().Quit();
        }

        private SignalAwaiter NextFrame() => ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        private async void RunCaveTest()
        {
            var game = GameManager.Instance;
            var center = game.Save.Center;

            switch (_caveStage)
            {
                case 0:
                {
                    _caveStage = 1;
                    CaveCheck(_map.Id == 2 && _isoMap.HasDarkness, $"caverna escura aberta (área {_map.Id})");
                    CaveCheck(!IsExplorerFire, $"explorando com um Digimon que não é de fogo ({_explorer.BaseData.Name}, {_explorer.BaseData.Element})");
                    CaveCheck(_isoMap.VisionState == "sem_bonus", $"luz do Digimon começa sem bônus ({_isoMap.VisionState})");

                    foreach (var id in _isoMap.BlockerIds)
                        CaveCheck(!_isoMap.IsBlockerCleared(id), $"começa com '{id}' fechado");

                    foreach (var id in _isoMap.SwitchableIds)
                        CaveCheck(!_isoMap.IsSwitchedOn(id), $"'{id}' começa apagado/desligado");

                    CaveCheck(_isoMap.LightRules.Where(l => l.Rule != "always").All(l => !_isoMap.IsLightActive(l.Id)),
                        "só as luzes 'always' começam acesas");
                    CaveCheck(_isoMap.IsLightActive("luz_0"), "lanterna do acampamento (luz_0) acesa");
                    CaveCheck(!_isoMap.IsWalkable(new Vector2I(103, 30)), "ponte sobre o abismo ainda não é andável");

                    // Lanterna sem fogo: recusa e avisa.
                    CaveCheck(!TryUseSwitch("lanterna_apagada_1"), "sem lanterna nem Digimon de fogo, a lanterna apagada não acende");
                    CaveCheck(_infoWindow != null && _infoWindow.IsOpen, "aparece o aviso de que falta fogo");
                    _infoWindow?.Close();

                    // q_lanterna: pega a lanterna velha e entrega ao minerador.
                    CaveCheck(game.TryAcceptQuest(32).Success, "aceita A Lanterna Perdida");
                    CaveCheck(!game.IsQuestReadyToTurnIn(32), "sem a lanterna velha não dá pra entregar");
                    var lanternPickup = _map.ItemPickups.Find(pk => pk.ItemId == 17);
                    CaveCheck(lanternPickup != null && game.CollectExplorationItem(lanternPickup).Success, "pega a Lanterna Velha");
                    CaveCheck(game.IsQuestReadyToTurnIn(32) && game.TryTurnInQuest(32).Success, "entrega A Lanterna Perdida");
                    CaveCheck(center.GetItemQuantity(17) == 0, "a lanterna velha foi entregue (consumida)");
                    CaveCheck(game.HasLantern, "o jogador tem a lanterna");
                    CaveCheck(_isoMap.IsBlockerCleared("bloqueio_desmoronamento"), "desmoronamento vira escombros (bloqueio aberto)");
                    CaveCheck(_isoMap.VisionState == "lanterna", $"luz do Digimon passa pra lanterna ({_isoMap.VisionState})");

                    // q_energia: aceita e liga as 3 caixas - a última termina a quest sozinha.
                    CaveCheck(game.TryAcceptQuest(33).Success, "aceita Energia nas Ruínas");
                    CaveCheck(TryUseSwitch("juncao_1") && _isoMap.IsSwitchedOn("juncao_1"), "liga a caixa de junção 1 (troca o sprite)");
                    CaveCheck(_isoMap.IsLightActive("luz_juncao_1"), "a luz da caixa 1 acende");
                    _infoWindow?.Close();
                    CaveCheck(game.GetQuestProgress(33) == 1 && !center.CompletedQuestIds.Contains(33), "Energia nas Ruínas 1/3");
                    CaveCheck(!TryUseSwitch("juncao_1"), "a mesma caixa não liga duas vezes");
                    TryUseSwitch("juncao_2");
                    _infoWindow?.Close();
                    CaveCheck(game.GetQuestProgress(33) == 2, "Energia nas Ruínas 2/3");
                    CaveCheck(!_isoMap.IsBlockerCleared("bloqueio_abismo"), "com 2 caixas a ponte ainda não liga");
                    TryUseSwitch("juncao_3");
                    CaveCheck(center.CompletedQuestIds.Contains(33), "a terceira caixa termina Energia nas Ruínas sozinha");
                    CaveCheck(_infoWindow != null && _infoWindow.IsOpen, "aparece a mensagem da quest concluída");
                    _infoWindow?.Close();
                    CaveCheck(_isoMap.IsBlockerCleared("bloqueio_abismo"), "a ponte de dados liga (bloqueio_abismo aberto)");
                    CaveCheck(_isoMap.IsWalkable(new Vector2I(103, 30)) && _isoMap.IsWalkable(new Vector2I(104, 32)), "os blocos da ponte viram andáveis");
                    CaveCheck(new[] { "terminal_27", "terminal_28", "terminal_29", "terminal_30" }.All(_isoMap.IsSwitchedOn), "os terminais trocam pra versão ligada");
                    CaveCheck(_isoMap.LightRules.Where(l => l.Rule == "after:q_energia").All(l => _isoMap.IsLightActive(l.Id)), "acendem as luzes 'after:q_energia' (ruínas e ponte)");

                    // q_lanternas: agora com a lanterna; acende as 4 antes de aceitar e entrega no NPC.
                    for (int i = 1; i <= 4; i++)
                    {
                        CaveCheck(TryUseSwitch($"lanterna_apagada_{i}"), $"acende a lanterna {i} (com a lanterna)");
                        CaveCheck(_isoMap.IsLightActive($"luz_lanterna_apagada_{i}"), $"a luz da lanterna {i} acende");
                        _infoWindow?.Close();
                    }

                    CaveCheck(!center.CompletedQuestIds.Contains(34) && !_isoMap.IsBlockerCleared("bloqueio_cristal"), "sem aceitar, a barreira ainda não quebra");
                    CaveCheck(game.TryAcceptQuest(34).Success && game.IsQuestReadyToTurnIn(34), "aceita A Galeria Acesa já pronta (as 4 contam)");
                    CaveCheck(game.TryTurnInQuest(34).Success, "entrega A Galeria Acesa");
                    CaveCheck(_isoMap.IsBlockerCleared("bloqueio_cristal"), "a barreira de cristal quebra (gruta aberta)");

                    // Saída norte: ainda sem destino.
                    _explorerVisual.Position = MapLoader.TileToGround(new Vector2I(102, 9));
                    await NextFrame();
                    _explorerVisual.Position = MapLoader.TileToGround(new Vector2I(102, 8));
                    await NextFrame();
                    await NextFrame();
                    CaveCheck(_infoWindow != null && _infoWindow.IsOpen, "pisar na saída norte mostra o aviso de caminho bloqueado / em breve");
                    _infoWindow?.Close();

                    // Save: ida e volta do JSON (mesmo serializador do SaveSystem).
                    var loaded = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(game.Save, new JsonSerializerOptions { WriteIndented = true }));
                    CaveCheck(new[] { 32, 33, 34 }.All(loaded.Center.CompletedQuestIds.Contains), "o save guarda as 3 quests concluídas");
                    CaveCheck(new[] { "juncao_1", "juncao_2", "juncao_3", "lanterna_apagada_1", "lanterna_apagada_4" }
                        .All(id => loaded.Center.IsExplorationSwitchOn(2, id)), "o save guarda as caixas ligadas e as lanternas acesas");

                    // Volta pra floresta pela boca da caverna (andando).
                    _explorerVisual.Position = MapLoader.TileToGround(new Vector2I(3, 62));
                    await NextFrame();
                    _explorerVisual.Position = MapLoader.TileToGround(new Vector2I(2, 62));
                    await NextFrame();
                    await NextFrame();
                    CaveCheck(_transitionConfirm != null && _transitionConfirm.IsOpen, "pisar na boca da caverna pergunta se quer voltar pra floresta");
                    OnTransitionConfirmed();
                    break;
                }
                case 1:
                {
                    _caveStage = 2;
                    CaveCheck(_map.Id == 1, $"chegou na Floresta (área {_map.Id})");
                    CaveCheck(MapLoader.GroundToTile(_explorerVisual.Position) == new Vector2I(7, 5), $"chegou ao lado da mina ({MapLoader.GroundToTile(_explorerVisual.Position)})");
                    CaveCheck(!_isoMap.HasDarkness, "a floresta não tem escuro");

                    Button returnButton = null;

                    foreach (var child in GetNode("CanvasLayer").GetChildren())
                    {
                        if (child is Button b && b.Text.StartsWith("↩"))
                            returnButton = b;
                    }

                    CaveCheck(returnButton != null, "a floresta tem o botão de voltar pra caverna");
                    returnButton?.EmitSignal(BaseButton.SignalName.Pressed);
                    break;
                }
                case 2:
                {
                    _caveStage = 3;

                    for (int i = 0; i < 5; i++)
                        await NextFrame();

                    CaveCheck(_map.Id == 2, $"voltou pra Caverna (área {_map.Id})");
                    CaveCheck(MapLoader.GroundToTile(_explorerVisual.Position) == new Vector2I(6, 62), $"voltou na frente da boca da caverna ({MapLoader.GroundToTile(_explorerVisual.Position)})");

                    foreach (var id in _isoMap.BlockerIds)
                        CaveCheck(_isoMap.IsBlockerCleared(id), $"'{id}' continua aberto (lido das quests concluídas)");

                    foreach (var id in _isoMap.SwitchableIds)
                        CaveCheck(_isoMap.IsSwitchedOn(id), $"'{id}' continua aceso/ligado");

                    CaveCheck(_isoMap.LightRules.All(l => _isoMap.IsLightActive(l.Id)), "todas as 49 luzes fixas ativas");
                    CaveCheck(_isoMap.VisionState == "lanterna", $"a luz do Digimon continua a da lanterna ({_isoMap.VisionState})");

                    GD.Print($"[ISO-CAVETEST] {(_caveFailures == 0 ? "OK" : "FALHOU")}: {_caveFailures} falha(s)");
                    GetTree().Quit();
                    break;
                }
            }
        }
    }
}
