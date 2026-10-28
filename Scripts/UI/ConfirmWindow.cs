using Godot;

namespace ProjetoDC.Scripts.UI
{
    /// <summary>
    /// Janela de confirmação/aviso no mesmo estilo das outras janelas do jogo (fundo de hexágonos
    /// azuis, ver <see cref="HexWindowBackground"/>), montada em código: substitui os
    /// ConfirmationDialog/AcceptDialog nativos do Godot, que vinham com a barra de título e o tema
    /// padrão do engine (e botões em inglês). É uma CanvasLayer por cima de tudo, com um Dim que
    /// bloqueia o mouse enquanto a janela está aberta. Esc cancela.
    /// </summary>
    public partial class ConfirmWindow : CanvasLayer
    {
        private const float WindowWidth = 500f;

        public event System.Action Confirmed;

        private readonly bool _hasCancel;
        private readonly bool _danger;

        private Control _root;
        private Label _titleLabel;
        private Label _messageLabel;
        private TextureRect _icon;
        private Button _confirmButton;
        private Button _cancelButton;

        /// <param name="hasCancel">true: pergunta com Cancelar + Confirmar; false: só um aviso com um botão.</param>
        /// <param name="danger">Confirmar em vermelho (ação destrutiva).</param>
        public ConfirmWindow(bool hasCancel = true, bool danger = false)
        {
            _hasCancel = hasCancel;
            _danger = danger;
        }

        public override void _Ready()
        {
            Layer = 100;
            ProcessMode = ProcessModeEnum.Always;

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
                new Color(0.05f, 0.086f, 0.141f, 1f), 10, 1,
                _danger ? new Color(0.72f, 0.35f, 0.35f) : new Color(0.227f, 0.4f, 0.541f), shadow: true));
            center.AddChild(window);

            window.AddChild(new HexWindowBackground { MouseFilter = Control.MouseFilterEnum.Ignore });

            var margin = new MarginContainer();
            margin.AddThemeConstantOverride("margin_left", 24);
            margin.AddThemeConstantOverride("margin_right", 24);
            margin.AddThemeConstantOverride("margin_top", 22);
            margin.AddThemeConstantOverride("margin_bottom", 22);
            window.AddChild(margin);

            var box = new VBoxContainer();
            box.AddThemeConstantOverride("separation", 16);
            margin.AddChild(box);

            _titleLabel = new Label();
            _titleLabel.AddThemeFontSizeOverride("font_size", 22);
            _titleLabel.AddThemeColorOverride("font_color",
                _danger ? new Color(0.949f, 0.557f, 0.557f) : new Color(0.94f, 0.97f, 1f));
            box.AddChild(_titleLabel);

            box.AddChild(new HSeparator());

            _icon = new TextureRect
            {
                Visible = false,
                CustomMinimumSize = new Vector2(64, 64),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
                SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
            };
            box.AddChild(_icon);

            _messageLabel = new Label
            {
                CustomMinimumSize = new Vector2(WindowWidth - 50f, 0),
                AutowrapMode = TextServer.AutowrapMode.WordSmart
            };
            _messageLabel.AddThemeFontSizeOverride("font_size", 16);
            box.AddChild(_messageLabel);

            var buttons = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
            buttons.AddThemeConstantOverride("separation", 12);
            box.AddChild(buttons);

            if (_hasCancel)
            {
                _cancelButton = MakeButton("Cancelar", 150, danger: false);
                _cancelButton.Pressed += Close;
                buttons.AddChild(_cancelButton);
            }

            _confirmButton = MakeButton("Confirmar", 200, _danger);
            _confirmButton.Pressed += OnConfirmPressed;
            buttons.AddChild(_confirmButton);
        }

        public bool IsOpen => _root != null && _root.Visible;

        public void Open(string title, string message, string confirmText = null, string cancelText = null, Texture2D icon = null)
        {
            _titleLabel.Text = title;
            _messageLabel.Text = message;
            _icon.Texture = icon;
            _icon.Visible = icon != null;

            _confirmButton.Text = confirmText ?? (_hasCancel ? "Confirmar" : "Entendido");

            if (_cancelButton != null)
                _cancelButton.Text = cancelText ?? "Cancelar";

            _root.Visible = true;

            // Numa pergunta destrutiva o foco inicial fica no Cancelar (Enter sem querer não apaga nada).
            (_cancelButton ?? _confirmButton).GrabFocus();
        }

        public void Close() => _root.Visible = false;

        private void OnConfirmPressed()
        {
            Close();
            Confirmed?.Invoke();
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            if (!IsOpen || !@event.IsActionPressed("ui_cancel"))
                return;

            Close();
            GetViewport().SetInputAsHandled();
        }

        private static Button MakeButton(string text, float width, bool danger)
        {
            var button = new Button { Text = text, CustomMinimumSize = new Vector2(width, 44) };
            button.AddThemeFontSizeOverride("font_size", 18);

            if (danger)
            {
                button.AddThemeStyleboxOverride("normal", MakeBox(new Color(0.451f, 0.165f, 0.165f), 6, 1, new Color(0.718f, 0.353f, 0.353f)));
                button.AddThemeStyleboxOverride("hover", MakeBox(new Color(0.569f, 0.208f, 0.208f), 6, 1, new Color(0.859f, 0.482f, 0.482f)));
                button.AddThemeStyleboxOverride("pressed", MakeBox(new Color(0.451f, 0.165f, 0.165f), 6, 1, new Color(0.718f, 0.353f, 0.353f)));
            }
            else
            {
                button.AddThemeStyleboxOverride("normal", MakeBox(new Color(0.075f, 0.133f, 0.212f, 0.92f), 6, 1, new Color(0.227f, 0.4f, 0.541f)));
                button.AddThemeStyleboxOverride("hover", MakeBox(new Color(0.118f, 0.231f, 0.345f, 0.96f), 6, 2, new Color(0.439f, 0.843f, 1f)));
                button.AddThemeStyleboxOverride("pressed", MakeBox(new Color(0.055f, 0.102f, 0.165f), 6, 2, new Color(0.314f, 0.682f, 0.902f)));
            }

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
