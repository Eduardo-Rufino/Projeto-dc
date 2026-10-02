using Godot;
using ProjetoDC.Scripts.Managers;
using System;
using System.Collections.Generic;

namespace ProjetoDC.Scripts.UI
{
    /// <summary>
    /// Tutoriais automáticos: mostra um tópico da TutorialScreen (pelo Id) na primeira vez que o
    /// gatilho dele acontece - boas-vindas no jogo novo, uma tela aberta pela primeira vez, etc. Os
    /// gatilhos são registrados por quem conhece as telas (ver Center.Tutorials.cs) e checados aqui a
    /// cada frame; esta janela roda com o jogo pausado (as telas do Center pausam a árvore), por isso a
    /// checagem mora nela e não no Center. Tópico mostrado vai pra CenterState.SeenTutorialIds e não
    /// aparece de novo; a lista completa continua no botão 📖.
    ///
    /// Desligado quando CenterState.AutoTutorialsEnabled é false (save de antes desse sistema, ou o
    /// jogador desligou em ⚙ Configurações) e em qualquer execução com variável DEBUG_* (testes,
    /// screenshots, soak), a menos que DEBUG_TUTORIALS=1.
    /// Montada em código, no mesmo estilo da ConfirmWindow (fundo de hexágonos).
    /// </summary>
    public partial class TutorialPopup : CanvasLayer
    {
        private const float WindowWidth = 680f;
        private const float BodyHeight = 380f;

        private sealed class Rule
        {
            public string[] TopicIds;
            public Func<bool> Trigger;
        }

        private readonly List<Rule> _rules = new();
        private readonly List<string> _queue = new();

        // Páginas da janela aberta agora (um gatilho com vários tópicos vira várias páginas).
        private readonly List<string> _pages = new();
        private int _pageIndex;

        private Control _root;
        private Label _titleLabel;
        private RichTextLabel _bodyLabel;
        private ScrollContainer _bodyScroll;
        private Label _pageLabel;
        private Button _previousButton;
        private Button _nextButton;

        private bool _debugDisabled;

        /// <summary>Só mostra a janela quando isso devolve true (ex.: o Center está visível, não há
        /// animação de evolução nem item sendo arrastado). Os gatilhos continuam sendo anotados.</summary>
        public Func<bool> CanShow { get; set; } = () => true;

        public bool IsOpen => _root != null && _root.Visible;

        private static GameManager Gm => GameManager.Instance;

        private static bool Enabled =>
            Gm?.Save?.Center != null && Gm.HasActiveGame && Gm.Save.Center.AutoTutorialsEnabled;

        public override void _Ready()
        {
            Layer = 90;
            ProcessMode = ProcessModeEnum.Always;

            _debugDisabled = OS.GetEnvironment("DEBUG_TUTORIALS") != "1" && HasDebugEnvironment();

            BuildWindow();
        }

        /// <summary>Quando <paramref name="trigger"/> ficar true pela primeira vez, mostra os tópicos
        /// ainda não vistos (cada um vira uma página, na ordem dada).</summary>
        public void AddRule(Func<bool> trigger, params string[] topicIds)
        {
            _rules.Add(new Rule { TopicIds = topicIds, Trigger = trigger });
        }

        public override void _Process(double delta)
        {
            if (_debugDisabled || !Enabled)
            {
                _queue.Clear();
                return;
            }

            var seen = Gm.Save.Center.SeenTutorialIds;

            foreach (var rule in _rules)
            {
                if (Array.TrueForAll(rule.TopicIds, id => seen.Contains(id) || _queue.Contains(id) || _pages.Contains(id)))
                    continue;

                if (!rule.Trigger())
                    continue;

                foreach (var id in rule.TopicIds)
                {
                    if (!seen.Contains(id) && !_queue.Contains(id) && !_pages.Contains(id))
                        _queue.Add(id);
                }
            }

            if (!IsOpen && _queue.Count > 0 && CanShow())
                ShowQueued();
        }

        private void ShowQueued()
        {
            _pages.Clear();

            foreach (var id in _queue)
            {
                if (TutorialScreen.TryGetTopic(id, out _, out _))
                    _pages.Add(id);
                else
                    GD.PrintErr($"TutorialPopup: tópico '{id}' não existe na TutorialScreen.");
            }

            _queue.Clear();

            if (_pages.Count == 0)
                return;

            _pageIndex = 0;
            ShowPage();
            _root.Visible = true;
            _nextButton.GrabFocus();
        }

        private void ShowPage()
        {
            string id = _pages[_pageIndex];
            TutorialScreen.TryGetTopic(id, out string title, out string body);

            _titleLabel.Text = title;
            _bodyLabel.Text = body;
            _bodyScroll.ScrollVertical = 0;

            // Marca na hora em que aparece: fechar o jogo no meio não faz a página repetir.
            var seen = Gm.Save.Center.SeenTutorialIds;
            if (!seen.Contains(id))
                seen.Add(id);

            bool multi = _pages.Count > 1;
            _pageLabel.Visible = multi;
            _pageLabel.Text = $"{_pageIndex + 1} / {_pages.Count}";
            _previousButton.Visible = multi;
            _previousButton.Disabled = _pageIndex == 0;
            _nextButton.Text = _pageIndex < _pages.Count - 1 ? "Próximo ▶" : "Entendi";
        }

        private void OnNextPressed()
        {
            if (_pageIndex < _pages.Count - 1)
            {
                _pageIndex++;
                ShowPage();
                return;
            }

            Close();
        }

        private void OnPreviousPressed()
        {
            if (_pageIndex == 0)
                return;

            _pageIndex--;
            ShowPage();
        }

        private void Close()
        {
            // Esc ou "Entendi" no meio de várias páginas: as que faltavam também contam como vistas
            // (continuam no botão 📖), pra não reaparecerem do nada depois.
            var seen = Gm.Save.Center.SeenTutorialIds;
            foreach (var id in _pages)
            {
                if (!seen.Contains(id))
                    seen.Add(id);
            }

            _pages.Clear();
            _root.Visible = false;
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            if (!IsOpen || !@event.IsActionPressed("ui_cancel"))
                return;

            Close();
            GetViewport().SetInputAsHandled();
        }

        private static bool HasDebugEnvironment()
        {
            foreach (var key in System.Environment.GetEnvironmentVariables().Keys)
            {
                if (key is string name && name.StartsWith("DEBUG_", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private void BuildWindow()
        {
            _root = new Control { Visible = false };
            _root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            AddChild(_root);

            var dim = new Panel();
            dim.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            dim.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(0.03f, 0.035f, 0.043f, 0.6f) });
            _root.AddChild(dim);

            var center = new CenterContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
            center.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            _root.AddChild(center);

            var window = new PanelContainer { CustomMinimumSize = new Vector2(WindowWidth, 0) };
            window.AddThemeStyleboxOverride("panel", MakeBox(
                new Color(0.05f, 0.086f, 0.141f, 1f), 10, 1, new Color(0.227f, 0.4f, 0.541f), shadow: true));
            center.AddChild(window);

            window.AddChild(new HexWindowBackground { MouseFilter = Control.MouseFilterEnum.Ignore });

            var margin = new MarginContainer();
            margin.AddThemeConstantOverride("margin_left", 24);
            margin.AddThemeConstantOverride("margin_right", 24);
            margin.AddThemeConstantOverride("margin_top", 20);
            margin.AddThemeConstantOverride("margin_bottom", 20);
            window.AddChild(margin);

            var box = new VBoxContainer();
            box.AddThemeConstantOverride("separation", 12);
            margin.AddChild(box);

            var header = new HBoxContainer();
            box.AddChild(header);

            var tag = new Label { Text = "Tutorial", VerticalAlignment = VerticalAlignment.Center };
            tag.AddThemeFontSizeOverride("font_size", 14);
            tag.AddThemeColorOverride("font_color", new Color(0.439f, 0.843f, 1f));
            header.AddChild(tag);

            _titleLabel = new Label { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, HorizontalAlignment = HorizontalAlignment.Right };
            _titleLabel.AddThemeFontSizeOverride("font_size", 22);
            _titleLabel.AddThemeColorOverride("font_color", new Color(0.94f, 0.97f, 1f));
            header.AddChild(_titleLabel);

            box.AddChild(new HSeparator());

            _bodyScroll = new ScrollContainer
            {
                CustomMinimumSize = new Vector2(WindowWidth - 50f, BodyHeight),
                HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            };
            box.AddChild(_bodyScroll);

            _bodyLabel = new RichTextLabel
            {
                BbcodeEnabled = true,
                FitContent = true,
                ScrollActive = false,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            };
            _bodyLabel.AddThemeFontSizeOverride("normal_font_size", 16);
            _bodyLabel.AddThemeFontSizeOverride("bold_font_size", 16);
            _bodyScroll.AddChild(_bodyLabel);

            var hint = new Label
            {
                Text = "Todos os tutoriais ficam no botão 📖 da barra de controle.",
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            hint.AddThemeFontSizeOverride("font_size", 13);
            hint.AddThemeColorOverride("font_color", new Color(0.62f, 0.7f, 0.78f));
            box.AddChild(hint);

            var buttons = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
            buttons.AddThemeConstantOverride("separation", 12);
            box.AddChild(buttons);

            _previousButton = MakeButton("◀ Anterior", 150);
            _previousButton.Pressed += OnPreviousPressed;
            buttons.AddChild(_previousButton);

            _pageLabel = new Label { CustomMinimumSize = new Vector2(70, 0), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            _pageLabel.AddThemeFontSizeOverride("font_size", 16);
            buttons.AddChild(_pageLabel);

            _nextButton = MakeButton("Entendi", 180);
            _nextButton.Pressed += OnNextPressed;
            buttons.AddChild(_nextButton);
        }

        private static Button MakeButton(string text, float width)
        {
            var button = new Button { Text = text, CustomMinimumSize = new Vector2(width, 44) };
            button.AddThemeFontSizeOverride("font_size", 18);
            button.AddThemeStyleboxOverride("normal", MakeBox(new Color(0.075f, 0.133f, 0.212f, 0.92f), 6, 1, new Color(0.227f, 0.4f, 0.541f)));
            button.AddThemeStyleboxOverride("hover", MakeBox(new Color(0.118f, 0.231f, 0.345f, 0.96f), 6, 2, new Color(0.439f, 0.843f, 1f)));
            button.AddThemeStyleboxOverride("pressed", MakeBox(new Color(0.055f, 0.102f, 0.165f), 6, 2, new Color(0.314f, 0.682f, 0.902f)));
            button.AddThemeStyleboxOverride("disabled", MakeBox(new Color(0.06f, 0.09f, 0.13f, 0.7f), 6, 1, new Color(0.18f, 0.25f, 0.32f)));
            return button;
        }

        private static StyleBoxFlat MakeBox(Color background, int radius, int border, Color borderColor, bool shadow = false)
        {
            var box = new StyleBoxFlat { BgColor = background, BorderColor = borderColor };

            box.SetBorderWidthAll(border);
            box.SetCornerRadiusAll(radius);

            if (shadow)
            {
                box.ShadowColor = new Color(0, 0, 0, 0.35f);
                box.ShadowSize = 10;
            }

            return box;
        }
    }
}
