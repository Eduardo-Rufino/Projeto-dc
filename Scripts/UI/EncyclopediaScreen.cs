using Godot;
using ProjetoDC.Enums;
using ProjetoDC.Scripts.Data;
using ProjetoDC.Scripts.Managers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjetoDC.Scripts.UI
{
    /// <summary>
    /// Enciclopédia com todas as espécies de Digimon do jogo, agrupadas por estágio evolutivo
    /// (ver DigimonStage - Baby, InTraining, Rookie, Champion, Ultimate, Mega, MegaPlus,
    /// Special, nessa ordem) com um cabeçalho por grupo. Espécies que o jogador já adquiriu
    /// no Center pelo menos uma vez (ver CenterState.DiscoveredDigimonIds, marcado em
    /// CenterState.AddDigimon e GameManager.TryToEvolve) mostram sprite/nome/role normalmente;
    /// as que nunca foram adquiridas aparecem só como silhueta (mesmo efeito visual do Guia de
    /// Evolução), sem nome nem role. Puramente informativo, sem stats detalhados.
    /// </summary>
    public partial class EncyclopediaScreen : Control
    {
        private static readonly Color SilhouetteColor = new(0f, 0f, 0f);
        private static readonly Vector2 EntrySpriteSize = new(56, 56);

        // Mesmos números da grade única antiga (ver ColumnFlowLayout removido) - 8 colunas
        // cabem na largura fixa da janela (920px) sem cortar a última nem precisar de
        // rolagem horizontal.
        private const int Columns = 8;

        private VBoxContainer _sections;
        private LineEdit _searchEdit;
        private Button _backButton;

        private List<DigimonData> _allDigimon;

        public event Action BackPressed;

        public override void _Ready()
        {
            // O Center pausa a árvore inteira enquanto essa tela está aberta (ver
            // HUD.SyncPauseWithBlockingScreens) - sem isso a busca/scroll parariam de responder.
            ProcessMode = ProcessModeEnum.Always;

            _sections = GetNode<VBoxContainer>("Window/MarginContainer/VBoxContainer/GridScroll/Sections");
            _searchEdit = GetNode<LineEdit>("Window/MarginContainer/VBoxContainer/SearchEdit");
            _backButton = GetNode<Button>("Window/MarginContainer/VBoxContainer/BackButton");

            _searchEdit.TextChanged += OnSearchTextChanged;
            _backButton.Pressed += OnBackPressed;
        }

        public void Open()
        {
            _allDigimon = DatabaseManager.Instance.GetAllDigimons()
                .OrderBy(d => (int)d.Stage)
                .ThenBy(d => d.Id)
                .ToList();

            _searchEdit.Text = "";

            RefreshGrid("");

            Visible = true;
        }

        private void OnSearchTextChanged(string text)
        {
            RefreshGrid(text);
        }

        private void RefreshGrid(string filter)
        {
            foreach (var child in _sections.GetChildren())
                child.QueueFree();

            var discovered = GameManager.Instance.Save.Center.DiscoveredDigimonIds;
            string needle = filter.Trim();

            // _allDigimon já vem ordenado por Stage (ver Open) - agrupar preservando essa
            // ordem então já sai na sequência evolutiva certa (Baby -> ... -> Special), sem
            // precisar reordenar os grupos depois.
            foreach (var group in _allDigimon.GroupBy(d => d.Stage))
            {
                var entries = new List<Control>();

                foreach (var data in group)
                {
                    bool isDiscovered = discovered.Contains(data.Id);

                    // Espécie nunca adquirida não tem nome pra comparar contra a busca - só
                    // entra no filtro por texto se já foi descoberta, senão o jogador
                    // "adivinharia" nomes de espécies que nunca viu por tentativa e erro.
                    if (needle.Length > 0)
                    {
                        if (!isDiscovered)
                            continue;

                        if (data.Name.IndexOf(needle, StringComparison.OrdinalIgnoreCase) < 0)
                            continue;
                    }

                    entries.Add(BuildEntry(data, isDiscovered));
                }

                // Estágio inteiro filtrado pela busca - não mostra o cabeçalho vazio.
                if (entries.Count == 0)
                    continue;

                _sections.AddChild(BuildSectionHeader(group.Key));

                var grid = new GridContainer { Columns = Columns };
                grid.AddThemeConstantOverride("h_separation", 10);
                grid.AddThemeConstantOverride("v_separation", 12);

                foreach (var entry in entries)
                    grid.AddChild(entry);

                _sections.AddChild(grid);
            }
        }

        private static Control BuildSectionHeader(DigimonStage stage)
        {
            var label = new Label
            {
                Text = stage.ToString(),
                Modulate = new Color(1f, 0.85882354f, 0.30980393f),
            };

            label.AddThemeFontSizeOverride("font_size", 15);

            return label;
        }

        private Control BuildEntry(DigimonData data, bool discovered)
        {
            var box = new VBoxContainer
            {
                CustomMinimumSize = new Vector2(92, 0),
            };

            box.AddThemeConstantOverride("separation", 2);

            var textureCenter = new CenterContainer();

            var texture = new TextureRect
            {
                CustomMinimumSize = EntrySpriteSize,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            };

            string path = $"res://Assets/Sprites/Digimon/{data.Code}/0.png";

            if (ResourceLoader.Exists(path))
                texture.Texture = GD.Load<Texture2D>(path);

            if (!discovered)
                texture.Modulate = SilhouetteColor;

            textureCenter.AddChild(texture);
            box.AddChild(textureCenter);

            var nameLabel = new Label
            {
                Text = discovered ? data.Name : "???",
                HorizontalAlignment = HorizontalAlignment.Center,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            };

            box.AddChild(nameLabel);

            var roleLabel = new Label
            {
                Text = discovered ? data.Role.ToString() : "",
                HorizontalAlignment = HorizontalAlignment.Center,
                Modulate = new Color(0.7f, 0.75f, 0.8f),
            };

            roleLabel.AddThemeFontSizeOverride("font_size", 11);

            box.AddChild(roleLabel);

            return box;
        }

        private void OnBackPressed()
        {
            Visible = false;

            BackPressed?.Invoke();
        }
    }
}
