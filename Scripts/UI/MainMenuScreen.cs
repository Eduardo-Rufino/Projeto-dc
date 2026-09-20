using Godot;
using ProjetoDC.Scripts.Managers;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ProjetoDC.Scripts.UI
{
    /// <summary>
    /// Menu principal, a primeira tela do jogo (cena principal do projeto, ver
    /// project.godot): Novo Jogo, Carregar Jogo e
    /// Opções. O GameManager já carregou o save (se existir) no boot - "Carregar Jogo" só
    /// continua a partir dele; "Novo Jogo" pede confirmação antes de apagar um save existente
    /// (GameManager.StartNewGame). Enquanto o menu está aberto o relógio do jogo fica pausado,
    /// senão o tempo andaria (fome, doença, dia) com o jogador ainda nem dentro do Center.
    /// </summary>
    public partial class MainMenuScreen : Control
    {
        private const string PauseReason = "main_menu";
        private const string CenterScenePath = "res://Scenes/Center/Center.tscn";

        private const string DigimonSpriteScenePath = "res://Scenes/UI/DigimonSprite.tscn";

        // Quantos Digimons de enfeite aparecem espalhados pela tela, e o quanto cada sprite
        // (48x48 nativo) é ampliado - pixel art, então sempre um inteiro.
        private const int ScatterCount = 16;
        private const float ScatterScale = 2f;

        // Distância mínima entre dois enfeites e margem até a borda da janela, em pixels.
        private const float ScatterMinSpacing = 120f;
        private const float ScatterEdgeMargin = 64f;

        // Faixa central (em fração da tela) reservada pro título e os botões - nenhum Digimon
        // é sorteado aí dentro, senão ele ficaria em cima do texto.
        private static readonly Rect2 ScatterKeepOut = new(0.28f, 0.16f, 0.44f, 0.76f);

        private Button _newGameButton;
        private Button _loadGameButton;
        private Button _optionsButton;
        private Label _saveInfoLabel;
        private Control _scatter;
        private ColorRect _background;

        // Cada enfeite guarda a posição normalizada (0 a 1) na janela, pra acompanhar quando
        // ela é redimensionada.
        private readonly List<(Node2D Node, Vector2 Normalized)> _scattered = new();

        private SettingsScreen _settingsScreen;

        private Control _confirmOverlay;
        private Label _confirmMessageLabel;
        private Button _confirmCancelButton;
        private Button _confirmAcceptButton;

        private static readonly CultureInfo PtBr = new("pt-BR");

        private bool _leaving;

        private GameManager Game => GameManager.Instance;

        public override void _Ready()
        {
            // Os hooks de teste (ver Center._Ready) abrem uma tela específica do Center assim
            // que ele carrega - sem automação de clique não dá pra passar pelo menu antes, então
            // eles (e DEBUG_SKIP_MENU, pra quem só quer cair direto no Center) pulam o menu.
            if (ShouldSkipMenu())
            {
                _leaving = true;

                GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, CenterScenePath);

                return;
            }

            // O menu pausa a árvore (ver PauseReason) - sem isso os próprios botões parariam.
            ProcessMode = ProcessModeEnum.Always;

            _newGameButton = GetNode<Button>("Content/Column/Buttons/NewGameButton");
            _loadGameButton = GetNode<Button>("Content/Column/Buttons/LoadGameButton");
            _optionsButton = GetNode<Button>("Content/Column/Buttons/OptionsButton");
            _saveInfoLabel = GetNode<Label>("Content/Column/SaveInfoLabel");
            _scatter = GetNode<Control>("Scatter");
            _background = GetNode<ColorRect>("Background");

            _settingsScreen = GetNode<SettingsScreen>("SettingsScreen");

            _confirmOverlay = GetNode<Control>("ConfirmOverlay");
            _confirmMessageLabel = GetNode<Label>("ConfirmOverlay/Center/Panel/Margin/VBox/MessageLabel");
            _confirmCancelButton = GetNode<Button>("ConfirmOverlay/Center/Panel/Margin/VBox/ButtonsRow/CancelButton");
            _confirmAcceptButton = GetNode<Button>("ConfirmOverlay/Center/Panel/Margin/VBox/ButtonsRow/AcceptButton");

            _newGameButton.Pressed += OnNewGamePressed;
            _loadGameButton.Pressed += OnLoadGamePressed;
            _optionsButton.Pressed += OnOptionsPressed;

            _confirmCancelButton.Pressed += CloseConfirm;
            _confirmAcceptButton.Pressed += OnConfirmNewGame;

            _settingsScreen.BackPressed += OnSettingsBack;
            _settingsScreen.Visible = false;
            _confirmOverlay.Visible = false;

            Game.RequestPause(PauseReason);

            // Mesma música do Center: já toca aqui (o slider de volume das Opções fica
            // audível) e segue sem reiniciar quando o Center abrir.
            MusicManager.Instance?.PlayCenterMusic();

            BuildScatter();
            OnViewportSizeChanged();
            GetViewport().SizeChanged += OnViewportSizeChanged;

            RefreshSaveState();

            // Foco inicial no que o jogador provavelmente quer: continuar, se der (teclado/
            // controle). Adiado um frame - pedir foco dentro do _Ready pode não pegar.
            (_loadGameButton.Disabled ? _newGameButton : _loadGameButton).CallDeferred(Control.MethodName.GrabFocus);
        }

        private static bool ShouldSkipMenu()
        {
            foreach (var variable in new[]
                     { "DEBUG_SKIP_MENU", "DEBUG_OPEN_SCREEN", "DEBUG_TEST_PASSIVES", "DEBUG_TEST_BATTLE",
                       "DEBUG_TEST_EVOLUTION", "DEBUG_TEST_PLACEMENT",
                       "DEBUG_TEST_EXPLORATION", "DEBUG_TEST_COOKING", "DEBUG_SOAK", "DEBUG_TEST_FOOD" })
            {
                if (!string.IsNullOrEmpty(OS.GetEnvironment(variable)))
                    return true;
            }

            return false;
        }

        public override void _ExitTree()
        {
            // Segurança: qualquer caminho que tire o menu da árvore não pode deixar o jogo
            // pausado. Liberar duas vezes é inofensivo (é um conjunto de motivos).
            Game?.ReleasePause(PauseReason);
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            if (@event.IsActionPressed("ui_cancel") && _confirmOverlay != null && _confirmOverlay.Visible)
            {
                CloseConfirm();
                GetViewport().SetInputAsHandled();
            }
        }

        private void RefreshSaveState()
        {
            bool canContinue = Game.CanContinueGame;

            _loadGameButton.Disabled = !canContinue;
            _loadGameButton.FocusMode = canContinue ? FocusModeEnum.All : FocusModeEnum.None;
            _loadGameButton.TooltipText = canContinue ? "" : "Nenhum jogo salvo para carregar.";

            _saveInfoLabel.Text = DescribeSave();
        }

        /// <summary>Resumo do save carregado (dia, Digimons, ovos), ou o motivo de não haver
        /// um - também usado na mensagem de confirmação do Novo Jogo, pro jogador ver
        /// exatamente o que vai perder.</summary>
        private string DescribeSave()
        {
            if (Game.CanContinueGame)
            {
                var save = Game.Save;

                int digimons = save.Center.Digimons.Count;
                int eggs = save.Center.Eggs.Count;

                string digimonText = digimons == 1 ? "1 Digimon" : $"{digimons} Digimons";
                string eggText = eggs == 1 ? "1 ovo" : $"{eggs} ovos";

                return $"Dia {save.World.CurrentDay}  |  {digimonText}  |  {eggText}  |  {save.Center.Bits.ToString("N0", PtBr)} Bits";
            }

            return Game.SaveSystem.HasSave()
                ? "O save existente não pôde ser lido."
                : "Nenhum jogo salvo.";
        }

        /// <summary>Espalha Digimons aleatórios (uma espécie diferente cada) pela tela, todos
        /// tocando a animação "Happy" em loop - reaproveita o DigimonSprite do jogo. Só
        /// espécies que têm sprite entram; muda a cada vez que o menu abre.</summary>
        private void BuildScatter()
        {
            var spriteScene = GD.Load<PackedScene>(DigimonSpriteScenePath);

            if (spriteScene == null || DatabaseManager.Instance == null)
                return;

            var rng = new RandomNumberGenerator();
            rng.Randomize();

            var species = DatabaseManager.Instance.GetAllDigimons()
                .Where(d => ResourceLoader.Exists($"res://Assets/Sprites/Digimon/{d.Code}/2.png"))
                .OrderBy(_ => rng.Randi())
                .ToList();

            var size = GetViewportRect().Size;
            var placed = new List<Vector2>();

            foreach (var digimon in species)
            {
                if (placed.Count >= ScatterCount)
                    break;

                if (!TryPickScatterSpot(rng, size, placed, out var normalized))
                    continue;

                placed.Add(normalized);

                var node = spriteScene.Instantiate<DigimonSprite>();

                // Entrar na árvore antes de configurar: é o _Ready que resolve o AnimatedSprite2D.
                _scatter.AddChild(node);

                node.SetDigimon(digimon.Code);
                node.Scale = new Vector2(ScatterScale, ScatterScale);
                node.SetDirection(rng.Randf() < 0.5f);

                var sprite = node._sprite;
                sprite.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;

                // "Happy" não tem loop (ver DigimonSprite.AddAnimation) - recomeça sozinha a cada
                // fim. A velocidade e o frame inicial variam um pouco pros enfeites não
                // ficarem todos batendo palma no mesmo compasso.
                sprite.AnimationFinished += () => PlayHappy(sprite);
                sprite.SpeedScale = rng.RandfRange(0.8f, 1.25f);

                PlayHappy(sprite);
                sprite.Frame = rng.RandiRange(0, 1);

                _scattered.Add((node, normalized));
            }

            RepositionScatter();
        }

        private static void PlayHappy(AnimatedSprite2D sprite)
        {
            sprite.Stop();
            sprite.Frame = 0;
            sprite.Play("Happy");
        }

        /// <summary>Sorteia uma posição (normalizada) fora da faixa central e longe dos
        /// enfeites já colocados. Devolve false depois de muitas tentativas sem achar lugar.</summary>
        private static bool TryPickScatterSpot(
            RandomNumberGenerator rng, Vector2 size, List<Vector2> placed, out Vector2 spot)
        {
            float marginX = ScatterEdgeMargin / size.X;
            float marginY = ScatterEdgeMargin / size.Y;

            for (int attempt = 0; attempt < 200; attempt++)
            {
                var candidate = new Vector2(
                    rng.RandfRange(marginX, 1f - marginX),
                    rng.RandfRange(marginY, 1f - marginY));

                if (ScatterKeepOut.HasPoint(candidate))
                    continue;

                bool tooClose = placed.Any(other =>
                    ((candidate - other) * size).Length() < ScatterMinSpacing);

                if (tooClose)
                    continue;

                spot = candidate;

                return true;
            }

            spot = Vector2.Zero;

            return false;
        }

        private void OnViewportSizeChanged()
        {
            RepositionScatter();

            // O shader do fundo desenha a grade de hexágonos em pixels - precisa saber o tamanho
            // da janela pra manter os hexágonos regulares (não esticados).
            (_background.Material as ShaderMaterial)?.SetShaderParameter("screen_size", GetViewportRect().Size);
        }

        private void RepositionScatter()
        {
            var size = GetViewportRect().Size;

            foreach (var (node, normalized) in _scattered)
                node.Position = normalized * size;
        }

        private void OnNewGamePressed()
        {
            // Sem save nenhum não há o que apagar - começa direto, sem pergunta à toa. Um save
            // ilegível também conta (HasSave): o arquivo ainda existe e vai ser apagado.
            if (!Game.SaveSystem.HasSave())
            {
                StartNewGame();
                return;
            }

            _confirmMessageLabel.Text =
                "Já existe um jogo salvo:\n" +
                $"{DescribeSave()}\n\n" +
                "Começar um novo jogo vai APAGAR esse save permanentemente. " +
                "Isso não pode ser desfeito.\n\n" +
                "Tem certeza?";

            _confirmOverlay.Visible = true;
            _confirmCancelButton.GrabFocus();
        }

        private void CloseConfirm()
        {
            _confirmOverlay.Visible = false;
            _newGameButton.GrabFocus();
        }

        private void OnConfirmNewGame()
        {
            _confirmOverlay.Visible = false;

            StartNewGame();
        }

        private void StartNewGame()
        {
            if (_leaving)
                return;

            Game.StartNewGame();

            EnterCenter();
        }

        private void OnLoadGamePressed()
        {
            if (_leaving || !Game.CanContinueGame)
                return;

            // O save já foi carregado no boot (GameManager._Ready) - é só entrar no Center.
            EnterCenter();
        }

        private void OnOptionsPressed()
        {
            _settingsScreen.Open();
        }

        private void OnSettingsBack()
        {
            _optionsButton.GrabFocus();
        }

        private void EnterCenter()
        {
            _leaving = true;

            _newGameButton.Disabled = true;
            _loadGameButton.Disabled = true;
            _optionsButton.Disabled = true;

            Game.ReleasePause(PauseReason);

            GetTree().ChangeSceneToFile(CenterScenePath);
        }
    }
}
