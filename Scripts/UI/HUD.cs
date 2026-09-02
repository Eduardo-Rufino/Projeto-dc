using Godot;
using ProjetoDC.Enums;
using ProjetoDC.Scripts.Gameplay;
using ProjetoDC.Scripts.Managers;

namespace ProjetoDC.Scripts.UI
{
    public partial class HUD : Control
    {
        private Label _dateLabel;
        private Label _calendarLabel;
        private Label _clockLabel;
        private Button _skipSleepButton;
        private Button _speedButton;
        private AnalogClock _analogClock;
        private MiniMap _miniMap;
        private Label _bitsLabel;
        private Label _capacityLabel;
        private Label _digimonCountLabel;
        private Label _selectedDigimonNameLabel;
        private LineEdit _selectedDigimonNameEdit;
        private Button _deleteDigimonButton;
        private ConfirmationDialog _deleteDigimonConfirmDialog;
        private AcceptDialog _deleteDigimonBlockedDialog;
        private DigimonInstance _pendingDeleteDigimon;
        private Label _selectedDigimonLevelLabel;
        private Label _selectedDigimonXpLabel;
        private ProgressBar _selectedDigimonXpBar;
        private TextureRect _attributeIcon;
        private TextureRect _elementIcon;
        private Label _hpLabel;
        private Label _staminaLabel;
        private Label _hungerLabel;
        private Label _statusLabel;
        private Label _activityLabel;
        private Label _happinessLabel;
        private Label _disciplineLabel;
        private Label _attackLabel;
        private Label _defenseLabel;
        private Label _specialAttackLabel;
        private Label _specialDefenseLabel;
        private Label _speedLabel;

        private Button _feedButton;
        private Button _medicineButton;
        private Button _cleanButton;
        private Button _shopButton;
        private Button _trophyButton;
        private Button _exitButton;
        private Button _inventoryButton;
        private Button _tutorialButton;
        private Button _evolutionGuideButton;
        private Button _baseEditorButton;
        private Button _settingsButton;
        private ConfirmationDialog _exitConfirmDialog;

        private Control _digimonStatusPage1;
        private Control _digimonStatusPage2;
        private Button _digimonStatusPageButton;
        private bool _digimonStatusOnPage2;

        private ProgressBar _hpBar;
        private ProgressBar _staminaBar;
        private ProgressBar _hungerBar;
        private ProgressBar _happinessBar;
        private ProgressBar _disciplineBar;

        private double _infoRefreshTimer;
        private const double InfoRefreshInterval = 0.2;

        private Center _center;
        private DigimonInstance _inspectedDigimon;
        private DigimonSprite _selectedDigimonSprite;

        private Panel _warningToast;
        private Label _warningLabel;

        public override void _Ready()
        {
            // Precisa continuar rodando com a árvore pausada - é a própria HUD quem decide
            // pausar (ver _Process/_center.IsBlockingScreenOpen), então ela não pode travar
            // junto, senão nunca detectaria a tela secundária fechando pra despausar de volta.
            ProcessMode = ProcessModeEnum.Always;

            _dateLabel = GetNode<Label>(
                "TopBar/DateTime/Panel/HBoxContainer/VBoxContainer/DateLabel"
            );

            _calendarLabel = GetNode<Label>(
                "TopBar/DateTime/Panel/HBoxContainer/VBoxContainer/CalendarLabel"
            );

            _clockLabel = GetNode<Label>(
                "TopBar/DateTime/Panel/HBoxContainer/VBoxContainer/ClockLabel"
            );

            _skipSleepButton = GetNode<Button>(
                "TopBar/DateTime/Panel/HBoxContainer/SkipSleepButton"
            );

            _skipSleepButton.Pressed += OnSkipSleepButtonPressed;

            _speedButton = GetNode<Button>(
                "TopBar/DateTime/Panel/HBoxContainer/SpeedButton"
            );

            _speedButton.Pressed += OnSpeedButtonPressed;

            _analogClock = GetNode<AnalogClock>(
                "TopBar/DateTime/Panel/HBoxContainer/AnalogClock"
            );

            _miniMap = GetNode<MiniMap>(
                "TopBar/CenterMap/Panel/MiniMap"
            );

            _bitsLabel = GetNode<Label>(
                "TopBar/CenterStatus/Panel/VBoxContainer/BitsLabel"
            );

            _capacityLabel = GetNode<Label>(
                "TopBar/CenterStatus/Panel/VBoxContainer/CapacityLabel"
            );

            _digimonCountLabel = GetNode<Label>(
                "TopBar/CenterStatus/Panel/VBoxContainer/DigimonCountLabel"
            );

            _selectedDigimonNameLabel = GetNode<Label>(
                "TopBar/SelectedDigimon/Panel/HBoxContainer/LeftPad/InfoContainer/NameRow/NameSlot/NameLabel"
            );

            _selectedDigimonNameEdit = GetNode<LineEdit>(
                "TopBar/SelectedDigimon/Panel/HBoxContainer/LeftPad/InfoContainer/NameRow/NameSlot/NameLineEdit"
            );

            _selectedDigimonNameLabel.GuiInput += OnSelectedDigimonNameLabelGuiInput;
            _selectedDigimonNameEdit.TextSubmitted += OnSelectedDigimonNameSubmitted;
            _selectedDigimonNameEdit.FocusExited += OnSelectedDigimonNameEditFocusExited;

            _deleteDigimonButton = GetNode<Button>(
                "TopBar/SelectedDigimon/Panel/HBoxContainer/LeftPad/InfoContainer/NameRow/DeleteButton"
            );

            _deleteDigimonButton.Pressed += OnDeleteDigimonButtonPressed;

            _selectedDigimonLevelLabel = GetNode<Label>(
                "TopBar/SelectedDigimon/Panel/HBoxContainer/LeftPad/InfoContainer/LevelRow/LevelLabel"
            );

            _selectedDigimonXpLabel = GetNode<Label>(
                "TopBar/SelectedDigimon/Panel/HBoxContainer/LeftPad/InfoContainer/XPContainer/XPLabel"
            );

            _selectedDigimonXpBar = GetNode<ProgressBar>(
                "TopBar/SelectedDigimon/Panel/HBoxContainer/LeftPad/InfoContainer/XPContainer/XPBar"
            );

            _attributeIcon = GetNode<TextureRect>(
                "TopBar/SelectedDigimon/Panel/HBoxContainer/LeftPad/InfoContainer/LevelRow/TypeIconsRow/AttributeIcon"
            );

            _elementIcon = GetNode<TextureRect>(
                "TopBar/SelectedDigimon/Panel/HBoxContainer/LeftPad/InfoContainer/LevelRow/TypeIconsRow/ElementIcon"
            );

            _selectedDigimonSprite = GetNode<DigimonSprite>(
                "TopBar/SelectedDigimon/Panel/HBoxContainer/SpritePanel/DigimonSprite"
            );
            _hpLabel = GetNode<Label>(
                "TopBar/DigimonStatus/Panel/HBoxContainer/Page1/ColA/HPContainer/HPLabel"
            );

            _staminaLabel = GetNode<Label>(
                "TopBar/DigimonStatus/Panel/HBoxContainer/Page1/ColA/StaminaContainer/StaminaLabel"
            );

            _hungerLabel = GetNode<Label>(
                "TopBar/DigimonStatus/Panel/HBoxContainer/Page1/ColA/HungerContainer/HungerLabel"
            );
            _hpBar = GetNode<ProgressBar>(
                "TopBar/DigimonStatus/Panel/HBoxContainer/Page1/ColA/HPContainer/HPBar"
            );

            _staminaBar = GetNode<ProgressBar>(
                "TopBar/DigimonStatus/Panel/HBoxContainer/Page1/ColA/StaminaContainer/StaminaBar"
            );

            _hungerBar = GetNode<ProgressBar>(
                "TopBar/DigimonStatus/Panel/HBoxContainer/Page1/ColA/HungerContainer/HungerBar"
            );
            _statusLabel = GetNode<Label>(
                "TopBar/DigimonStatus/Panel/HBoxContainer/Page1/ColB/StatusLabel"
            );

            _activityLabel = GetNode<Label>(
                "TopBar/DigimonStatus/Panel/HBoxContainer/Page1/ColB/ActivityLabel"
            );

            _happinessLabel = GetNode<Label>(
                "TopBar/DigimonStatus/Panel/HBoxContainer/Page1/ColB/HappinessContainer/HappinessLabel"
            );

            _happinessBar = GetNode<ProgressBar>(
                "TopBar/DigimonStatus/Panel/HBoxContainer/Page1/ColB/HappinessContainer/HappinessBar"
            );

            _disciplineLabel = GetNode<Label>(
                "TopBar/DigimonStatus/Panel/HBoxContainer/Page1/ColB/DisciplineContainer/DisciplineLabel"
            );

            _disciplineBar = GetNode<ProgressBar>(
                "TopBar/DigimonStatus/Panel/HBoxContainer/Page1/ColB/DisciplineContainer/DisciplineBar"
            );

            _attackLabel = GetNode<Label>(
                "TopBar/DigimonStatus/Panel/HBoxContainer/Page2/AttackLabel"
            );

            _defenseLabel = GetNode<Label>(
                "TopBar/DigimonStatus/Panel/HBoxContainer/Page2/DefenseLabel"
            );

            _specialAttackLabel = GetNode<Label>(
                "TopBar/DigimonStatus/Panel/HBoxContainer/Page2/SpecialAttackLabel"
            );

            _specialDefenseLabel = GetNode<Label>(
                "TopBar/DigimonStatus/Panel/HBoxContainer/Page2/SpecialDefenceLabel"
            );

            _speedLabel = GetNode<Label>(
                "TopBar/DigimonStatus/Panel/HBoxContainer/Page2/SpeedLabel"
            );

            _digimonStatusPage1 = GetNode<Control>(
                "TopBar/DigimonStatus/Panel/HBoxContainer/Page1"
            );

            _digimonStatusPage2 = GetNode<Control>(
                "TopBar/DigimonStatus/Panel/HBoxContainer/Page2"
            );

            _digimonStatusPageButton = GetNode<Button>(
                "TopBar/DigimonStatus/Panel/HBoxContainer/PageButton"
            );

            _digimonStatusPageButton.Pressed += OnDigimonStatusPageButtonPressed;

            _warningToast = GetNode<Panel>("WarningToast");
            _warningLabel = GetNode<Label>("WarningToast/Label");

            _feedButton = GetNode<Button>(
                "ControlBar/FeedButton"
            );

            _feedButton.ButtonDown += OnFeedButtonDown;
            _feedButton.ButtonUp += OnFeedButtonUp;

            _medicineButton = GetNode<Button>(
                "ControlBar/MedicineButton"
            );

            _medicineButton.ButtonDown += OnMedicineButtonDown;
            _medicineButton.ButtonUp += OnMedicineButtonUp;

            _cleanButton = GetNode<Button>(
                "ControlBar/CleanButton"
            );

            _cleanButton.Pressed += OnCleanButtonPressed;

            _shopButton = GetNode<Button>(
                "ControlBar/ShopButton"
            );

            _shopButton.Pressed += OnShopButtonPressed;

            _trophyButton = GetNode<Button>(
                "ControlBar/TrophyButton"
            );

            _trophyButton.Pressed += OnTrophyButtonPressed;

            _settingsButton = GetNode<Button>(
                "ControlBar/SettingsButton"
            );

            _settingsButton.Pressed += OnSettingsButtonPressed;

            _exitButton = GetNode<Button>(
                "ControlBar/ExitButton"
            );

            _exitButton.Pressed += OnExitButtonPressed;

            _inventoryButton = GetNode<Button>(
                "ControlBar/InventoryButton"
            );

            _inventoryButton.Pressed += OnInventoryButtonPressed;

            _tutorialButton = GetNode<Button>(
                "ControlBar/ActionButton"
            );

            _tutorialButton.Pressed += OnTutorialButtonPressed;

            _evolutionGuideButton = GetNode<Button>(
                "ControlBar/ActionButton2"
            );

            _evolutionGuideButton.Pressed += OnEvolutionGuideButtonPressed;

            _baseEditorButton = GetNode<Button>(
                "ControlBar/ActionButton3"
            );

            _baseEditorButton.Pressed += OnBaseEditorButtonPressed;

            // O GameManager pode ainda estar terminando sua inicialização.
            CallDeferred(nameof(ConnectToGameManager));
        }

        public override void _Process(double delta)
        {
            SyncPauseWithBlockingScreens();

            _infoRefreshTimer -= delta;

            if (_infoRefreshTimer > 0)
                return;

            _infoRefreshTimer = InfoRefreshInterval;

            // Capacidade/Bits/contagem de Digimons podem mudar por várias fontes (comprar,
            // ovo chocar, Digimon evoluir de estágio, etc.) sem um evento dedicado pra cada
            // uma - poll simples resolve sem precisar instrumentar cada ponto de mutação.
            RefreshCenterStatus();

            // Não chama enquanto o campo de apelido está aberto: RefreshInspectedDigimonInfo
            // cancela a edição em andamento (CancelNameEdit), então esse poll periódico
            // fechava a caixa de edição sozinho a cada 0.2s, antes do jogador conseguir
            // digitar qualquer coisa.
            if (_inspectedDigimon != null && !_selectedDigimonNameEdit.Visible)
                RefreshInspectedDigimonInfo();
        }

        /// <summary>Pausa o resto do jogo (câmera, Digimons, relógio, etc.) sempre que
        /// qualquer tela secundária está aberta por cima do Center (loja, inventário,
        /// tutorial, seleção de time, etc.) - diálogos de confirmação não contam (ver
        /// Center.IsBlockingScreenOpen). Cada uma dessas telas precisa de ProcessMode.Always
        /// pra continuar clicável mesmo com a árvore pausada.
        /// Usa GameManager.Request/ReleasePause (motivo "screen") em vez de mexer direto em
        /// GetTree().Paused - assim não atropela outros motivos de pausa que já existem
        /// (batalha, animação de evolução), que ficam ativos independente disso.</summary>
        private void SyncPauseWithBlockingScreens()
        {
            if (_center == null || GameManager.Instance == null)
                return;

            if (_center.IsBlockingScreenOpen())
                GameManager.Instance.RequestPause("screen");
            else
                GameManager.Instance.ReleasePause("screen");
        }

        private void ConnectToGameManager()
        {
            if (GameManager.Instance == null)
            {
                GD.PrintErr("[HUD] GameManager não encontrado.");
                return;
            }

            _center = GetTree().CurrentScene as Center;

            if (_center == null)
            {
                GD.PrintErr("[HUD] Center não encontrado.");
                return;
            }

            GameManager.Instance.ClockSystem.MinutePassed += RefreshDateTime;

            // Sem isso, o painel de status (nome/nível/HP/etc.) só era atualizado quando o
            // jogador clicava no próprio Digimon no mundo (ver DigimonWorld -> Center.
            // InspectDigimon -> HUD.InspectDigimon) - início de jogo novo, save carregado e
            // troca de PlayerDigimon (ex.: deletar o selecionado) deixavam o painel preso no
            // texto de placeholder do .tscn ("Botamon Lv. 1") até isso acontecer.
            GameManager.Instance.PlayerDigimonChanged += OnPlayerDigimonChanged;

            if (GameManager.Instance.PlayerDigimon != null)
                InspectDigimon(GameManager.Instance.PlayerDigimon);

            RefreshDateTime();
            RefreshCenterStatus();
        }

        private void OnPlayerDigimonChanged()
        {
            InspectDigimon(GameManager.Instance.PlayerDigimon);
        }

        private void RefreshDateTime()
        {
            var world = GameManager.Instance.Save.World;

            _dateLabel.Text = $"Dia {world.CurrentDay}";
            _calendarLabel.Text = "Calendário";
            _clockLabel.Text =
                $"{world.CurrentHour:00}:{world.CurrentMinute:00}";

            _analogClock.SetTime(world.CurrentHour, world.CurrentMinute);

            _skipSleepButton.Disabled = !GameManager.Instance.CanSkipSleep;

            _speedButton.ButtonPressed = GameManager.Instance.IsClockSpeedDoubled;
        }

        private void OnSkipSleepButtonPressed()
        {
            GameManager.Instance.SkipSleep();
        }

        private void OnSpeedButtonPressed()
        {
            GameManager.Instance.ToggleClockSpeedDoubled();
        }

        public override void _ExitTree()
        {
            if (GameManager.Instance?.ClockSystem != null)
            {
                GameManager.Instance.ClockSystem.MinutePassed -= RefreshDateTime;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.PlayerDigimonChanged -= OnPlayerDigimonChanged;
            }

            if (_inspectedDigimon != null)
            {
                _inspectedDigimon.ActivityChanged -= OnInspectedDigimonActivityChanged;
                _inspectedDigimon.HealthStateChanged -= OnInspectedDigimonHealthStateChanged;
            }
        }

        private void RefreshCenterStatus()
        {
            var center = GameManager.Instance.Save.Center;

            _bitsLabel.Text = $"Bits: {center.Bits:N0}";
            _capacityLabel.Text =
                $"Capacidade: {center.CapacityUsed} / {center.CapacityLimit}";
            _digimonCountLabel.Text =
                $"Digimons: {center.Digimons.Count}";

            if (_center != null)
                _miniMap.RefreshAreas(_center.GetAreas());
        }

        private void OnInspectedDigimonActivityChanged(DigimonActivity activity)
        {
            if (_inspectedDigimon == null)
                return;

            _selectedDigimonSprite.RefreshState(
                _inspectedDigimon
            );
        }

        private void OnInspectedDigimonHealthStateChanged(HealthState state)
        {
            if (_inspectedDigimon == null)
                return;

            _selectedDigimonSprite.RefreshState(
                _inspectedDigimon
            );
        }

        public void InspectDigimon(DigimonInstance digimon)
        {
            if (_inspectedDigimon != null)
            {
                _inspectedDigimon.ActivityChanged -= OnInspectedDigimonActivityChanged;
                _inspectedDigimon.HealthStateChanged -= OnInspectedDigimonHealthStateChanged;
            }

            _inspectedDigimon = digimon;

            if (_inspectedDigimon != null)
            {
                _inspectedDigimon.ActivityChanged += OnInspectedDigimonActivityChanged;
                _inspectedDigimon.HealthStateChanged += OnInspectedDigimonHealthStateChanged;
            }

            RefreshInspectedDigimonInfo();

            if (_inspectedDigimon == null)
                return;

            _selectedDigimonSprite.SetDigimon(
                _inspectedDigimon.BaseData.Code
            );

            _selectedDigimonSprite.RefreshState(
                _inspectedDigimon
            );
        }

        /// <summary>Aviso temporário no topo da tela (ex.: Center.IsSpecificTrainingAreaOccupied
        /// recusando um segundo Digimon numa área de treino específica) - some sozinho depois
        /// de alguns segundos, mesmo padrão do ShopScreen.ShowMessage.</summary>
        public void ShowWarning(string message)
        {
            if (_warningToast == null || _warningLabel == null)
                return;

            _warningLabel.Text = message;
            _warningToast.Visible = true;

            GetTree().CreateTimer(3.0).Timeout += () =>
            {
                if (IsInstanceValid(_warningToast))
                    _warningToast.Visible = false;
            };
        }

        private void OnSelectedDigimonNameLabelGuiInput(InputEvent @event)
        {
            if (_inspectedDigimon == null)
                return;

            if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
            {
                AcceptEvent();
                StartNameEdit();
            }
        }

        private void StartNameEdit()
        {
            _selectedDigimonNameEdit.Text = _inspectedDigimon.DisplayName;

            _selectedDigimonNameLabel.Visible = false;
            _selectedDigimonNameEdit.Visible = true;

            // GrabFocus() direto aqui, no meio do próprio evento de clique que trocou a
            // visibilidade Label->LineEdit, perde a disputa com o input ainda em processamento
            // (o LineEdit nunca fica de fato focado, e o clique seguinte já dispara
            // FocusExited/CommitNameEdit no mesmo instante). Adiar pra depois do input atual
            // terminar de processar resolve.
            CallDeferred(nameof(FocusNameEdit));
        }

        private void FocusNameEdit()
        {
            _selectedDigimonNameEdit.GrabFocus();
            _selectedDigimonNameEdit.SelectAll();
        }

        private void OnSelectedDigimonNameSubmitted(string newText)
        {
            CommitNameEdit();
        }

        private void OnSelectedDigimonNameEditFocusExited()
        {
            CommitNameEdit();
        }

        /// <summary>Aplica o texto digitado como Nickname (só exibição - ver DigimonInstance.
        /// DisplayName) e volta a mostrar o Label. Texto vazio ou igual ao nome da espécie
        /// limpa o apelido, voltando a mostrar o nome padrão.</summary>
        private void CommitNameEdit()
        {
            if (!_selectedDigimonNameEdit.Visible)
                return;

            if (_inspectedDigimon != null)
            {
                string newName = _selectedDigimonNameEdit.Text.Trim();

                _inspectedDigimon.Nickname =
                    newName.Length == 0 || newName == _inspectedDigimon.BaseData.Name
                        ? null
                        : newName;

                _selectedDigimonNameLabel.Text = _inspectedDigimon.DisplayName;
            }

            _selectedDigimonNameEdit.Visible = false;
            _selectedDigimonNameLabel.Visible = true;
        }

        private void CancelNameEdit()
        {
            _selectedDigimonNameEdit.Visible = false;
            _selectedDigimonNameLabel.Visible = true;
        }

        private void RefreshInspectedDigimonInfo()
        {
            CancelNameEdit();

            if (_inspectedDigimon == null)
            {
                _selectedDigimonNameLabel.Text = "Nenhum Digimon";
                _selectedDigimonLevelLabel.Text = "";
                _selectedDigimonXpLabel.Text = "";
                _selectedDigimonXpBar.Value = 0;

                _attributeIcon.Texture = null;
                _elementIcon.Texture = null;

                _hpLabel.Text = "";
                _staminaLabel.Text = "";
                _hungerLabel.Text = "";
                _statusLabel.Text = "";
                _activityLabel.Text = "";
                _happinessLabel.Text = "";
                _disciplineLabel.Text = "";

                _selectedDigimonSprite.Visible = false;
                _deleteDigimonButton.Disabled = true;

                return;
            }

            _selectedDigimonSprite.Visible = true;
            _deleteDigimonButton.Disabled = false;

            _selectedDigimonNameLabel.Text =
                _inspectedDigimon.DisplayName;

            _selectedDigimonLevelLabel.Text =
                $"Lv. {_inspectedDigimon.Level}";

            _selectedDigimonXpBar.MaxValue = _inspectedDigimon.ExperienceToNextLevel;
            _selectedDigimonXpBar.Value = _inspectedDigimon.Experience;

            _selectedDigimonXpLabel.Text =
                $"EXP: {_inspectedDigimon.Experience}/{_inspectedDigimon.ExperienceToNextLevel}";

            _attributeIcon.Texture = GD.Load<Texture2D>(
                TypeIcons.GetPath(_inspectedDigimon.BaseData.Attribute)
            );

            _elementIcon.Texture = GD.Load<Texture2D>(
                TypeIcons.GetPath(_inspectedDigimon.BaseData.Element)
            );

            _hpBar.MaxValue = _inspectedDigimon.MaxHealthPoints;
            _hpBar.Value = _inspectedDigimon.CurrentHealthPoints;

            _hpLabel.Text =
                $"HP: {_inspectedDigimon.CurrentHealthPoints} / {_inspectedDigimon.MaxHealthPoints}";


            _staminaBar.MaxValue = _inspectedDigimon.MaxStamina;
            _staminaBar.Value = _inspectedDigimon.Stamina;

            _staminaLabel.Text =
                $"Stamina: {_inspectedDigimon.Stamina} / {_inspectedDigimon.MaxStamina}";


            _hungerBar.MaxValue = _inspectedDigimon.MaxHunger;
            _hungerBar.Value = _inspectedDigimon.Hunger;

            _hungerLabel.Text =
                $"Fome: {_inspectedDigimon.Hunger} / {_inspectedDigimon.MaxHunger}";

            _statusLabel.Text =
                $"Saúde: {_inspectedDigimon.HealthState}";

            _activityLabel.Text =
                $"Atividade: {_inspectedDigimon.Activity}";

            _happinessBar.MaxValue = DigimonInstance.MaxHappiness;
            _happinessBar.Value = _inspectedDigimon.Happiness;

            _happinessLabel.Text =
                $"Felicidade: {_inspectedDigimon.Happiness} / {DigimonInstance.MaxHappiness}";

            _disciplineBar.MaxValue = DigimonInstance.MaxDiscipline;
            _disciplineBar.Value = _inspectedDigimon.Discipline;

            _disciplineLabel.Text =
                $"Disciplina: {_inspectedDigimon.Discipline} / {DigimonInstance.MaxDiscipline}";

            _attackLabel.Text =
                $"ATQ Físico: {_inspectedDigimon.CurrentStats.PhysicalDamage}";

            _defenseLabel.Text =
                $"DEF Física: {_inspectedDigimon.CurrentStats.PhysicalDefense}";

            _specialAttackLabel.Text =
                $"ATQ Especial: {_inspectedDigimon.CurrentStats.SpecialDamage}";

            _specialDefenseLabel.Text =
                $"DEF Especial: {_inspectedDigimon.CurrentStats.SpecialDefense}";

            _speedLabel.Text =
                $"Velocidade: {_inspectedDigimon.CurrentStats.Speed}";
        }

        private void OnDigimonStatusPageButtonPressed()
        {
            _digimonStatusOnPage2 = !_digimonStatusOnPage2;

            _digimonStatusPage1.Visible = !_digimonStatusOnPage2;
            _digimonStatusPage2.Visible = _digimonStatusOnPage2;

            _digimonStatusPageButton.Text = _digimonStatusOnPage2 ? "◀" : "▶";
        }

        private void OnFeedButtonDown()
        {
            _center.StartFoodPlacement();
        }

        private void OnFeedButtonUp()
        {
            _center.PlaceFood();
        }

        private void OnMedicineButtonDown()
        {
            _center.StartMedicinePlacement();
        }

        private void OnMedicineButtonUp()
        {
            _center.PlaceMedicine();
        }

        private void OnCleanButtonPressed()
        {
            _center.ToggleBroomMode();
        }

        private void OnShopButtonPressed()
        {
            _center.OpenShop();
        }

        private void OnTrophyButtonPressed()
        {
            _center.OpenBattleTypeMenu();
        }

        private void OnInventoryButtonPressed()
        {
            _center.OpenInventory();
        }

        private void OnTutorialButtonPressed()
        {
            _center.OpenTutorial();
        }

        private void OnEvolutionGuideButtonPressed()
        {
            _center.OpenEvolutionGuide();
        }

        private void OnBaseEditorButtonPressed()
        {
            _center.OpenBaseEditor();
        }

        private void OnSettingsButtonPressed()
        {
            _center.OpenSettings();
        }

        private void OnDeleteDigimonButtonPressed()
        {
            if (_inspectedDigimon == null)
                return;

            if (GameManager.Instance.CenterService.GetAllDigimons().Count <= 1)
            {
                if (_deleteDigimonBlockedDialog == null)
                {
                    _deleteDigimonBlockedDialog = new AcceptDialog
                    {
                        Title = "Deletar Digimon"
                    };

                    AddChild(_deleteDigimonBlockedDialog);
                }

                _deleteDigimonBlockedDialog.DialogText =
                    "Você não pode deletar seu único Digimon.";

                _deleteDigimonBlockedDialog.PopupCentered();

                return;
            }

            _pendingDeleteDigimon = _inspectedDigimon;

            if (_deleteDigimonConfirmDialog == null)
            {
                _deleteDigimonConfirmDialog = new ConfirmationDialog
                {
                    Title = "Deletar Digimon"
                };

                AddChild(_deleteDigimonConfirmDialog);

                _deleteDigimonConfirmDialog.Confirmed += OnDeleteDigimonConfirmed;
            }

            _deleteDigimonConfirmDialog.DialogText =
                $"Tem certeza que quer deletar {_pendingDeleteDigimon.DisplayName}?\n" +
                "Essa ação é PERMANENTE: o Digimon será apagado e não pode ser recuperado.";

            _deleteDigimonConfirmDialog.PopupCentered();
        }

        private void OnDeleteDigimonConfirmed()
        {
            if (_pendingDeleteDigimon == null)
                return;

            GameManager.Instance.DeleteDigimon(_pendingDeleteDigimon);

            if (_inspectedDigimon == _pendingDeleteDigimon)
            {
                _inspectedDigimon = null;
                RefreshInspectedDigimonInfo();
            }

            _pendingDeleteDigimon = null;
        }

        private void OnExitButtonPressed()
        {
            if (_exitConfirmDialog == null)
            {
                _exitConfirmDialog = new ConfirmationDialog
                {
                    Title = "Fechar o jogo",
                    DialogText = "Tem certeza que quer fechar o jogo?\n" +
                        "As alterações serão salvas automaticamente ao fechar."
                };

                AddChild(_exitConfirmDialog);

                _exitConfirmDialog.Confirmed += OnExitConfirmed;
            }

            _exitConfirmDialog.PopupCentered();
        }

        private void OnExitConfirmed()
        {
            // O save-ao-fechar normal (GameManager._Notification/NotificationWMCloseRequest)
            // é disparado pelo SO fechando a janela, não necessariamente por GetTree().Quit()
            // chamado por código - salva explicitamente aqui pra garantir, já que a própria
            // mensagem de confirmação promete isso ao jogador.
            GameManager.Instance?.SaveGame();

            GetTree().Quit();
        }
    }
}