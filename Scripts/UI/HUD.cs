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
        private Label _bitsLabel;
        private Label _capacityLabel;
        private Label _digimonCountLabel;
        private Label _selectedDigimonNameLabel;
        private Label _selectedDigimonLevelLabel;
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

        public override void _Ready()
        {
            _dateLabel = GetNode<Label>(
                "TopBar/DateTime/Panel/VBoxContainer/DateLabel"
            );

            _calendarLabel = GetNode<Label>(
                "TopBar/DateTime/Panel/VBoxContainer/CalendarLabel"
            );

            _clockLabel = GetNode<Label>(
                "TopBar/DateTime/Panel/VBoxContainer/ClockLabel"
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
                "TopBar/SelectedDigimon/Panel/HBoxContainer/InfoContainer/NameLabel"
            );

            _selectedDigimonLevelLabel = GetNode<Label>(
                "TopBar/SelectedDigimon/Panel/HBoxContainer/InfoContainer/LevelLabel"
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

            _cleanButton.ButtonDown += OnCleanButtonDown;
            _cleanButton.ButtonUp += OnCleanButtonUp;

            _shopButton = GetNode<Button>(
                "ControlBar/ShopButton"
            );

            _shopButton.Pressed += OnShopButtonPressed;

            // O GameManager pode ainda estar terminando sua inicialização.
            CallDeferred(nameof(ConnectToGameManager));
        }

        public override void _Process(double delta)
        {
            if (_inspectedDigimon == null)
                return;

            _infoRefreshTimer -= delta;

            if (_infoRefreshTimer > 0)
                return;

            _infoRefreshTimer = InfoRefreshInterval;

            RefreshInspectedDigimonInfo();
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

            _center.DigimonInspected += RefreshSelectedDigimon;

            RefreshDateTime();
            RefreshCenterStatus();
        }

        private void RefreshDateTime()
        {
            var world = GameManager.Instance.Save.World;

            _dateLabel.Text = $"Dia {world.CurrentDay}";
            _calendarLabel.Text = "Calendário";
            _clockLabel.Text =
                $"{world.CurrentHour:00}:{world.CurrentMinute:00}";
        }

        public override void _ExitTree()
        {
            if (GameManager.Instance?.ClockSystem != null)
            {
                GameManager.Instance.ClockSystem.MinutePassed -= RefreshDateTime;
            }

            if (_center != null)
            {
                _center.DigimonInspected -= RefreshSelectedDigimon;
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
        }

        private void RefreshSelectedDigimon(DigimonInstance digimon)
        {
            _inspectedDigimon = digimon;

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

        private void RefreshInspectedDigimonInfo()
        {
            if (_inspectedDigimon == null)
            {
                _selectedDigimonNameLabel.Text = "Nenhum Digimon";
                _selectedDigimonLevelLabel.Text = "";

                _hpLabel.Text = "";
                _staminaLabel.Text = "";
                _hungerLabel.Text = "";
                _statusLabel.Text = "";
                _activityLabel.Text = "";
                _happinessLabel.Text = "";
                _disciplineLabel.Text = "";

                return;
            }

            _selectedDigimonNameLabel.Text =
                _inspectedDigimon.BaseData.Name;

            _selectedDigimonLevelLabel.Text =
                $"Lv. {_inspectedDigimon.Level}";

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

        private void OnCleanButtonDown()
        {
            _center.StartBroomPlacement();
        }

        private void OnCleanButtonUp()
        {
            _center.UseBroom();
        }

        private void OnShopButtonPressed()
        {
            _center.OpenShop();
        }
    }
}