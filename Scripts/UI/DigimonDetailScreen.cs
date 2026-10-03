using Godot;
using ProjetoDC.Enums;
using ProjetoDC.Scripts.Gameplay;
using ProjetoDC.Scripts.Managers;
using ProjetoDC.Scripts.Systems.Passives;
using System;
using System.Collections.Generic;

namespace ProjetoDC.Scripts.UI
{
    /// <summary>
    /// Janela de detalhes do Digimon inspecionado - mostra dados que a barra de status
    /// principal (HUD.DigimonStatus/SelectedDigimon) não tem espaço pra exibir: Role,
    /// Atributo, Elemento, Tipo de Ovo, Tipo de Ataque (e Suporte, se for Support), Idade,
    /// Capacidade, e a passiva (ver PASSIVAS_SPEC.md na raiz do projeto). Sprite fixo
    /// (frame 0 da animação), mesmo padrão de retrato usado em EncyclopediaScreen.BuildEntry.
    /// </summary>
    public partial class DigimonDetailScreen : Control
    {
        private TextureRect _sprite;
        private Label _nameLabel;
        private Label _stageLabel;
        private Label _roleLabel;
        private Label _attributeLabel;
        private Label _elementLabel;
        private Label _eggTypeLabel;
        private Label _attackTypeLabel;
        private Label _ageLabel;
        private Label _capacityLabel;
        private Label _passiveNameLabel;
        private Label _passiveDescriptionLabel;

        // Passivas herdadas (InheritedPassiveSystem): título + lista com rolagem, criados em código
        // dentro do PassiveBox, logo abaixo da passiva própria.
        private Label _inheritedTitleLabel;
        private ScrollContainer _inheritedScroll;
        private VBoxContainer _inheritedList;
        private GridContainer _autoTrainingGrid;
        private Button _backButton;

        private readonly List<CheckButton> _autoTrainingToggles = new();

        // Só os 6 stats têm área de treino específica (ver CenterAreaType) - a área Neutra/
        // Treino genérico não força nenhum stat, então não faz sentido ter toggle pra ela.
        private static readonly (CenterAreaType Type, string Label)[] TrainableStats =
        {
            (CenterAreaType.TrainingHealthPoints, "HP"),
            (CenterAreaType.TrainingAttack, "Ataque"),
            (CenterAreaType.TrainingDefense, "Defesa"),
            (CenterAreaType.TrainingSpecialAttack, "Ataque Especial"),
            (CenterAreaType.TrainingSpecialDefense, "Defesa Especial"),
            (CenterAreaType.TrainingSpeed, "Velocidade"),
        };

        public event Action BackPressed;

        public override void _Ready()
        {
            // O Center pausa a árvore inteira enquanto essa tela está aberta (ver
            // HUD.SyncPauseWithBlockingScreens) - sem isso o botão Voltar não responderia.
            ProcessMode = ProcessModeEnum.Always;

            _sprite = GetNode<TextureRect>("Window/MarginContainer/VBoxContainer/HeaderRow/Sprite");

            _nameLabel = GetNode<Label>(
                "Window/MarginContainer/VBoxContainer/HeaderRow/InfoColumn/NameLabel"
            );

            _stageLabel = GetNode<Label>(
                "Window/MarginContainer/VBoxContainer/HeaderRow/InfoColumn/StageLabel"
            );

            _roleLabel = GetNode<Label>("Window/MarginContainer/VBoxContainer/DetailsGrid/RoleLabel");

            _attributeLabel = GetNode<Label>(
                "Window/MarginContainer/VBoxContainer/DetailsGrid/AttributeLabel"
            );

            _elementLabel = GetNode<Label>("Window/MarginContainer/VBoxContainer/DetailsGrid/ElementLabel");

            _eggTypeLabel = GetNode<Label>("Window/MarginContainer/VBoxContainer/DetailsGrid/EggTypeLabel");

            _attackTypeLabel = GetNode<Label>(
                "Window/MarginContainer/VBoxContainer/DetailsGrid/AttackTypeLabel"
            );

            _ageLabel = GetNode<Label>("Window/MarginContainer/VBoxContainer/DetailsGrid/AgeLabel");

            _capacityLabel = GetNode<Label>("Window/MarginContainer/VBoxContainer/DetailsGrid/CapacityLabel");

            _passiveNameLabel = GetNode<Label>(
                "Window/MarginContainer/VBoxContainer/PassiveBox/PassiveNameLabel"
            );

            _passiveDescriptionLabel = GetNode<Label>(
                "Window/MarginContainer/VBoxContainer/PassiveBox/PassiveDescriptionLabel"
            );

            CompactDetailsGrid();
            BuildInheritedSection(GetNode<VBoxContainer>("Window/MarginContainer/VBoxContainer/PassiveBox"));

            _autoTrainingGrid = GetNode<GridContainer>(
                "Window/MarginContainer/VBoxContainer/AutoTrainingBox/AutoTrainingGrid"
            );

            _backButton = GetNode<Button>("Window/MarginContainer/VBoxContainer/BackButton");

            _backButton.Pressed += OnBackPressed;
        }

        public void Open(DigimonInstance digimon)
        {
            if (digimon == null)
                return;

            var data = digimon.BaseData;

            string spritePath = $"res://Assets/Sprites/Digimon/{data.Code}/0.png";

            _sprite.Texture = ResourceLoader.Exists(spritePath)
                ? GD.Load<Texture2D>(spritePath)
                : null;

            _nameLabel.Text = digimon.DisplayName;
            _stageLabel.Text = data.Stage.ToString();

            _roleLabel.Text = $"Role: {data.Role}";
            _attributeLabel.Text = $"Atributo: {data.Attribute}";
            _elementLabel.Text = $"Elemento: {data.Element}";
            _eggTypeLabel.Text = $"Tipo de Ovo: {data.EggType}";

            _attackTypeLabel.Text = data.Role == RoleType.Support
                ? $"Tipo de Ataque: {data.AttackType} | Suporte: {data.SupportType}"
                : $"Tipo de Ataque: {data.AttackType}";

            _ageLabel.Text = $"Idade: {digimon.AgeInDays} dia(s)";
            _capacityLabel.Text = $"Capacidade: {digimon.CapacityCost}";

            RefreshPassiveInfo(digimon);
            RefreshInheritedPassives(digimon);
            BuildAutoTrainingToggles(digimon);

            Visible = true;
        }

        /// <summary>Um "Permitir" por stat treinável (ver DigimonInstance.
        /// AutoTrainingDisabledStats) - desmarcar impede esse Digimon específico de andar
        /// sozinho até a área de treino daquele stat quando um NPC recrutado libera o bônus
        /// (DigimonWorld.TryStartAutoTraining), sem afetar treino manual (arrastar pra lá na
        /// mão continua funcionando normalmente).</summary>
        private void BuildAutoTrainingToggles(DigimonInstance digimon)
        {
            foreach (var toggle in _autoTrainingToggles)
                toggle.QueueFree();

            _autoTrainingToggles.Clear();

            foreach (var (type, label) in TrainableStats)
            {
                var toggle = new CheckButton
                {
                    Text = label,
                    ButtonPressed = digimon.IsAutoTrainingAllowed(type),
                };

                toggle.Toggled += allowed => digimon.SetAutoTrainingAllowed(type, allowed);

                _autoTrainingGrid.AddChild(toggle);
                _autoTrainingToggles.Add(toggle);
            }
        }

        /// <summary>Ver PASSIVAS_SPEC.md - digimon.PassiveId é sorteado ao nascer/evoluir e
        /// fica fixo até a próxima evolução; null significa que a espécie não tinha (ou
        /// ainda não tem) pool preenchido.</summary>
        private void RefreshPassiveInfo(DigimonInstance digimon)
        {
            if (!digimon.PassiveId.HasValue)
            {
                _passiveNameLabel.Text = "✨ Nenhuma passiva";
                _passiveDescriptionLabel.Text = "Esse Digimon ainda não tem uma passiva definida.";
                return;
            }

            var passive = DatabaseManager.Instance.GetPassive(digimon.PassiveId.Value);

            if (passive == null)
            {
                _passiveNameLabel.Text = "✨ Passiva desconhecida";
                _passiveDescriptionLabel.Text = "";
                return;
            }

            _passiveNameLabel.Text = $"✨ {passive.Name}";
            _passiveDescriptionLabel.Text = passive.Description;
        }

        /// <summary>Abre espaço pra lista de passivas herdadas sem diminuir fonte: os dados curtos vão pra
        /// duas colunas (Tipo de Ataque, que é longo, continua na linha inteira) e a janela fica mais larga
        /// (540, menos quebra de linha) e um pouco mais alta (690, numa tela de 720).</summary>
        private void CompactDetailsGrid()
        {
            var details = GetNode<VBoxContainer>("Window/MarginContainer/VBoxContainer/DetailsGrid");

            var grid = new GridContainer { Columns = 2 };
            grid.AddThemeConstantOverride("h_separation", 16);
            grid.AddThemeConstantOverride("v_separation", 6);
            details.AddChild(grid);
            details.MoveChild(grid, 0);

            foreach (var label in new[] { _roleLabel, _attributeLabel, _elementLabel, _eggTypeLabel, _ageLabel, _capacityLabel })
            {
                label.Reparent(grid);
                label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            }

            var window = GetNode<Control>("Window");
            window.OffsetLeft = -270;
            window.OffsetRight = 270;
            window.OffsetTop = -345;
            window.OffsetBottom = 345;
        }

        /// <summary>O PassiveBox já expande pra ocupar o espaço livre da janela; a lista rola dentro dele
        /// (até 6 linhas numa linha evolutiva completa) em vez de diminuir a fonte.</summary>
        private void BuildInheritedSection(VBoxContainer passiveBox)
        {
            _inheritedTitleLabel = new Label();
            _inheritedTitleLabel.AddThemeFontSizeOverride("font_size", 15);
            _inheritedTitleLabel.AddThemeColorOverride("font_color", new Color(0.55f, 0.85f, 1f));
            passiveBox.AddChild(_inheritedTitleLabel);

            _inheritedScroll = new ScrollContainer
            {
                SizeFlagsVertical = SizeFlags.ExpandFill,
                CustomMinimumSize = new Vector2(0, 56),
                HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            };
            passiveBox.AddChild(_inheritedScroll);

            _inheritedList = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            _inheritedList.AddThemeConstantOverride("separation", 2);
            _inheritedScroll.AddChild(_inheritedList);
        }

        private void RefreshInheritedPassives(DigimonInstance digimon)
        {
            foreach (var child in _inheritedList.GetChildren())
                child.QueueFree();

            var passives = InheritedPassiveSystem.GetActiveInheritedPassives(digimon);

            _inheritedTitleLabel.Text = $"🧬 Passivas herdadas ({passives.Count})";
            Callable.From(() => _inheritedScroll.ScrollVertical = 0).CallDeferred();

            if (passives.Count == 0)
            {
                _inheritedList.AddChild(MakeInheritedLine("Nenhuma ainda - cada forma que ficar pra trás pode deixar uma."));
                return;
            }

            foreach (var (species, passive) in passives)
                _inheritedList.AddChild(MakeInheritedLine($"{species.Name}: {InheritedPassiveSystem.Describe(passive)}"));
        }

        private static Label MakeInheritedLine(string text)
        {
            var label = new Label
            {
                Text = text,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            label.AddThemeFontSizeOverride("font_size", 12);
            label.AddThemeColorOverride("font_color", new Color(0.7f, 0.75f, 0.8f));
            return label;
        }

        private void OnBackPressed()
        {
            Visible = false;

            BackPressed?.Invoke();
        }
    }
}
