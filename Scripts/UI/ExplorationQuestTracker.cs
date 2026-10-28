using Godot;
using ProjetoDC.Scripts.Managers;
using System.Text;

namespace ProjetoDC.Scripts.UI
{
    /// <summary>
    /// Botão "📜 Quests" do modo exploração (embaixo do nome da área) que abre a lista das quests
    /// ativas: quem deu, o progresso e se já dá pra entregar. A lista acompanha o save sozinha
    /// (confere a cada fração de segundo e só remonta se algo mudou) - quest entregue some dela,
    /// quest aceita aparece, progresso de coleta/batalha atualiza.
    /// </summary>
    public partial class ExplorationQuestTracker : Control
    {
        private const float PanelWidth = 330f;
        private const double RefreshInterval = 0.25;

        private Button _toggleButton;
        private PanelContainer _panel;
        private VBoxContainer _list;
        private string _signature;
        private double _refreshTimer;

        public override void _Ready()
        {
            MouseFilter = MouseFilterEnum.Ignore;
            SetAnchorsPreset(LayoutPreset.FullRect);

            _toggleButton = new Button
            {
                Position = new Vector2(16, 56),
                Size = new Vector2(200, 32),
                FocusMode = FocusModeEnum.None,
            };
            _toggleButton.AddThemeStyleboxOverride("normal", MakeBox(new Color(0.075f, 0.133f, 0.212f, 0.92f), 6, 1, new Color(0.227f, 0.4f, 0.541f)));
            _toggleButton.AddThemeStyleboxOverride("hover", MakeBox(new Color(0.118f, 0.231f, 0.345f, 0.96f), 6, 2, new Color(0.439f, 0.843f, 1f)));
            _toggleButton.AddThemeStyleboxOverride("pressed", MakeBox(new Color(0.055f, 0.102f, 0.165f), 6, 2, new Color(0.314f, 0.682f, 0.902f)));
            _toggleButton.Pressed += () => _panel.Visible = !_panel.Visible;
            AddChild(_toggleButton);

            _panel = new PanelContainer
            {
                Visible = false,
                Position = new Vector2(16, 96),
                CustomMinimumSize = new Vector2(PanelWidth, 0),
            };
            _panel.AddThemeStyleboxOverride("panel", MakeBox(new Color(0.05f, 0.086f, 0.141f, 0.96f), 8, 1, new Color(0.227f, 0.4f, 0.541f)));
            AddChild(_panel);

            _panel.AddChild(new HexWindowBackground { MouseFilter = MouseFilterEnum.Ignore });

            var margin = new MarginContainer();
            margin.AddThemeConstantOverride("margin_left", 14);
            margin.AddThemeConstantOverride("margin_right", 14);
            margin.AddThemeConstantOverride("margin_top", 12);
            margin.AddThemeConstantOverride("margin_bottom", 12);
            _panel.AddChild(margin);

            var box = new VBoxContainer();
            box.AddThemeConstantOverride("separation", 8);
            margin.AddChild(box);

            var title = new Label { Text = "📜 Quests ativas" };
            title.AddThemeFontSizeOverride("font_size", 18);
            box.AddChild(title);
            box.AddChild(new HSeparator());

            _list = new VBoxContainer();
            _list.AddThemeConstantOverride("separation", 10);
            box.AddChild(_list);

            Refresh(force: true);
        }

        public override void _Process(double delta)
        {
            _refreshTimer += delta;

            if (_refreshTimer < RefreshInterval)
                return;

            _refreshTimer = 0;
            Refresh(force: false);
        }

        private void Refresh(bool force)
        {
            var game = GameManager.Instance;
            var active = game.Save.Center.ActiveQuestIds;

            var signature = new StringBuilder();

            foreach (var questId in active)
                signature.Append(questId).Append(':').Append(game.GetQuestProgress(questId)).Append(game.IsQuestReadyToTurnIn(questId) ? "!" : "").Append(';');

            if (!force && signature.ToString() == _signature)
                return;

            _signature = signature.ToString();
            _toggleButton.Text = $"📜 Quests ({active.Count})";

            foreach (var child in _list.GetChildren())
                child.QueueFree();

            if (active.Count == 0)
            {
                _list.AddChild(MakeText("Nenhuma quest ativa. Converse com os NPCs (💬) pra pegar uma.", 14, new Color(0.7f, 0.78f, 0.86f)));
                return;
            }

            foreach (var questId in active)
            {
                var quest = DatabaseManager.Instance.GetQuest(questId);

                if (quest == null)
                    continue;

                string giver = DatabaseManager.Instance.GetNpc(quest.GiverNpcId)?.Name ?? "?";
                var entry = new VBoxContainer();
                entry.AddThemeConstantOverride("separation", 2);

                entry.AddChild(MakeText(quest.Name, 16, new Color(0.94f, 0.97f, 1f)));

                if (!string.IsNullOrEmpty(quest.Description))
                    entry.AddChild(MakeText(quest.Description, 13, new Color(0.7f, 0.78f, 0.86f)));

                entry.AddChild(game.IsQuestReadyToTurnIn(questId)
                    ? MakeText($"✔ Pronta! Entregue para {giver}.", 14, new Color(0.5f, 0.9f, 0.55f))
                    : MakeText($"{giver} · {game.GetQuestProgress(questId)}/{quest.ObjectiveCount}", 14, new Color(0.44f, 0.84f, 1f)));

                _list.AddChild(entry);
            }
        }

        private static Label MakeText(string text, int size, Color color)
        {
            var label = new Label
            {
                Text = text,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                CustomMinimumSize = new Vector2(PanelWidth - 30, 0),
            };
            label.AddThemeFontSizeOverride("font_size", size);
            label.AddThemeColorOverride("font_color", color);
            return label;
        }

        private static StyleBoxFlat MakeBox(Color background, int radius, int border, Color borderColor)
        {
            var box = new StyleBoxFlat { BgColor = background, BorderColor = borderColor };
            box.SetBorderWidthAll(border);
            box.SetCornerRadiusAll(radius);
            return box;
        }
    }
}
