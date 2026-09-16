using Godot;
using ProjetoDC.Enums;
using ProjetoDC.Scripts.Gameplay;
using ProjetoDC.Scripts.Managers;
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

        private void OnBackPressed()
        {
            Visible = false;

            BackPressed?.Invoke();
        }
    }
}
