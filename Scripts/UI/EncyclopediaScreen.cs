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

        private Button _speciesTabButton;
        private Button _setsTabButton;
        private Control _speciesPanel;
        private Control _setsPanel;
        private VBoxContainer _setsContainer;

        private List<DigimonData> _allDigimon;

        public event Action BackPressed;

        public override void _Ready()
        {
            // O Center pausa a árvore inteira enquanto essa tela está aberta (ver
            // HUD.SyncPauseWithBlockingScreens) - sem isso a busca/scroll parariam de responder.
            ProcessMode = ProcessModeEnum.Always;

            var vboxPath = "Window/MarginContainer/VBoxContainer";

            _speciesTabButton = GetNode<Button>($"{vboxPath}/TabRow/SpeciesTabButton");
            _setsTabButton = GetNode<Button>($"{vboxPath}/TabRow/SetsTabButton");

            _speciesPanel = GetNode<Control>($"{vboxPath}/SpeciesPanel");
            _sections = GetNode<VBoxContainer>($"{vboxPath}/SpeciesPanel/GridScroll/Sections");
            _searchEdit = GetNode<LineEdit>($"{vboxPath}/SpeciesPanel/SearchEdit");

            _setsPanel = GetNode<Control>($"{vboxPath}/SetsPanel");
            _setsContainer = GetNode<VBoxContainer>($"{vboxPath}/SetsPanel/SetsScroll/SetsContainer");

            _backButton = GetNode<Button>($"{vboxPath}/BackButton");

            _speciesTabButton.Pressed += () => ShowTab(species: true);
            _setsTabButton.Pressed += () => ShowTab(species: false);

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

            _speciesTabButton.ButtonPressed = true;
            ShowTab(species: true);

            Visible = true;
        }

        /// <summary>Alterna entre a grade de espécies e a aba de Conjuntos - só reconstrói a
        /// lista de conjuntos ao entrar nela, já que ela depende de DiscoveredDigimonIds
        /// (pode ter mudado desde a última vez, ex.: um Digimon novo descoberto).</summary>
        /// <summary>Só pra Center.DebugOpenScreen ("encyclopediasets") poder tirar
        /// screenshot da aba sem precisar simular clique - ver SKILL.md do
        /// run-projeto-dc.</summary>
        public void DebugShowSetsTab() => ShowTab(species: false);

        private void ShowTab(bool species)
        {
            _speciesPanel.Visible = species;
            _setsPanel.Visible = !species;

            if (!species)
                RefreshSets();
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

        // --- Aba de Conjuntos ---

        private static readonly Color SetCompleteColor = new(0.24705882f, 0.38431373f, 0.28627452f, 1f);
        private static readonly Color SetIncompleteColor = new(0.1882353f, 0.21176471f, 0.23921569f, 1f);

        private void RefreshSets()
        {
            foreach (var child in _setsContainer.GetChildren())
                child.QueueFree();

            var discovered = GameManager.Instance.Save.Center.DiscoveredDigimonIds;

            foreach (var set in DatabaseManager.Instance.GetAllSets())
                _setsContainer.AddChild(BuildSetEntry(set, discovered));
        }

        /// <summary>Um cartão por conjunto: cabeçalho com o checkbox de status (ligado só
        /// quando toda a lista já foi descoberta - ver GameManager.IsSetComplete), o nome
        /// (com o efeito no tooltip, ao passar o mouse) e a contagem, seguido de um retrato
        /// por membro - colorido se já descoberto, silhueta (mesmo efeito da grade de
        /// espécies) se não.</summary>
        private Control BuildSetEntry(DigimonSetData set, List<int> discovered)
        {
            int discoveredCount = set.DigimonIds.Count(discovered.Contains);
            bool complete = discoveredCount == set.DigimonIds.Count;

            var outer = new PanelContainer();
            outer.AddThemeStyleboxOverride("panel", CreateSetCardStyle(complete));

            var pad = new MarginContainer();
            pad.AddThemeConstantOverride("margin_left", 14);
            pad.AddThemeConstantOverride("margin_top", 10);
            pad.AddThemeConstantOverride("margin_right", 14);
            pad.AddThemeConstantOverride("margin_bottom", 10);
            outer.AddChild(pad);

            var vbox = new VBoxContainer();
            vbox.AddThemeConstantOverride("separation", 8);
            pad.AddChild(vbox);

            var headerRow = new HBoxContainer();
            headerRow.AddThemeConstantOverride("separation", 10);
            vbox.AddChild(headerRow);

            // "Checkbox estilizado": um badge colorido (verde quando completo, cinza quando
            // não) em volta de um CheckBox nativo desabilitado - ele só REFLETE o estado
            // (completo/incompleto), não é um toggle manual do jogador.
            var statusBadge = new PanelContainer();
            statusBadge.AddThemeStyleboxOverride("panel", CreateSetCardStyle(complete));

            var statusCheck = new CheckBox
            {
                ButtonPressed = complete,
                Disabled = true,
                FocusMode = FocusModeEnum.None,
                Text = "",
            };
            statusBadge.AddChild(statusCheck);
            headerRow.AddChild(statusBadge);

            var nameLabel = new Label
            {
                Text = set.Name,
                TooltipText = set.EffectDescription,
                VerticalAlignment = VerticalAlignment.Center,
                // Label ignora mouse por padrão (MouseFilterEnum.Ignore) - sem isso o tooltip
                // nunca dispara em NENHUM estado, completo ou não, porque o hover nunca chega
                // a ser detectado no Label.
                MouseFilter = MouseFilterEnum.Stop,
            };
            nameLabel.AddThemeFontSizeOverride("font_size", 15);
            headerRow.AddChild(nameLabel);

            var countLabel = new Label
            {
                Text = $"({discoveredCount}/{set.DigimonIds.Count})",
                Modulate = new Color(0.7f, 0.72f, 0.75f),
                VerticalAlignment = VerticalAlignment.Center,
            };
            headerRow.AddChild(countLabel);

            var membersRow = new HBoxContainer();
            membersRow.AddThemeConstantOverride("separation", 8);
            vbox.AddChild(membersRow);

            foreach (var id in set.DigimonIds)
            {
                var data = DatabaseManager.Instance.GetDigimon(id);

                if (data != null)
                    membersRow.AddChild(BuildSetMemberIcon(data, discovered.Contains(id)));
            }

            return outer;
        }

        /// <summary>Retrato de um membro do conjunto - mesmo padrão visual (silhueta preta se
        /// não descoberto) usado na grade principal de espécies (ver BuildEntry).</summary>
        private static Control BuildSetMemberIcon(DigimonData data, bool discovered)
        {
            var box = new VBoxContainer { CustomMinimumSize = new Vector2(68, 0) };
            box.AddThemeConstantOverride("separation", 2);

            var textureCenter = new CenterContainer();

            var texture = new TextureRect
            {
                CustomMinimumSize = new Vector2(48, 48),
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
            nameLabel.AddThemeFontSizeOverride("font_size", 10);

            box.AddChild(nameLabel);

            return box;
        }

        private static StyleBoxFlat CreateSetCardStyle(bool complete)
        {
            var style = new StyleBoxFlat
            {
                BgColor = complete ? SetCompleteColor : SetIncompleteColor,
                BorderColor = complete
                    ? new Color(0.40392157f, 0.7019608f, 0.44313726f)
                    : new Color(0.33333334f, 0.35686275f, 0.3882353f),
            };

            style.SetBorderWidthAll(1);
            style.SetCornerRadiusAll(6);

            return style;
        }

        private void OnBackPressed()
        {
            Visible = false;

            BackPressed?.Invoke();
        }
    }
}
