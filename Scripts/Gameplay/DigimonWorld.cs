using Godot;
using ProjetoDC.Enums;
using ProjetoDC.Scripts.Gameplay;
using ProjetoDC.Scripts.Managers;
using ProjetoDC.Scripts.Models.World;
using ProjetoDC.Scripts.UI;
using System;

namespace ProjetoDC.Scripts.World;

public partial class DigimonWorld : Node2D
{
    private DigimonSprite _sprite;
    private Center _center;
    private CenterArea _currentArea;
    private bool _lookingLeft;
    private FoodWorld _targetFood;
    private bool _isEating;
    private double _eatTimer;
    private double _trainingTimer;
    private const double TrainingInterval = 5.0;
    private bool _isTrainingAnimation;

    private bool _isDragging;
    private Vector2 _dragOffset;
    private Vector2 _positionBeforeDrag;
    private Vector2 _dragStartPosition;
    private CenterArea _dragStartArea;

    private Vector2 _targetPosition;

    private bool _isWalking;

    private float _speed = 40f;

    private double _idleTimer;

    public DigimonInstance _digimon { get; private set; }

    public override void _Ready()
    {
        _sprite = GetNode<DigimonSprite>("DigimonSprite");

        var clickArea = GetNode<Area2D>("ClickArea");

        clickArea.InputEvent += OnClickAreaInputEvent;

        _targetPosition = GlobalPosition;
    }

    public void Initialize(DigimonInstance digimon)
    {
        _digimon = digimon;

        GD.Print(
            $"VISUAL RECEBEU {_digimon.BaseData.Name} HASH: {_digimon.GetHashCode()}"
        );

        GD.Print(
            $"ANTES DO EVENTO: {_digimon.BaseData.Name} - {_digimon.Activity}"
        );

        _sprite.SetDigimon(digimon.BaseData.Code);

        _digimon.ActivityChanged += OnActivityChanged;
        _digimon.HealthStateChanged += OnHealthStateChanged;

        GD.Print(
            $"ASSINANDO EVENTO: {_digimon.BaseData.Name}"
        );

        CallDeferred(nameof(UpdateVisualState));

        GD.Print(
            $"{digimon.BaseData.Name} | Estado: {digimon.HealthState} | Atividade: {digimon.Activity}"
        );

        
    }

    private void OnActivityChanged(DigimonActivity activity)
    {
        GD.Print(
            $"EVENTO RECEBIDO: {_digimon.BaseData.Name} -> {activity}"
        );

        UpdateVisualState();

        if (!_digimon.CanMove())
        {
            _isWalking = false;
            _sprite.SetWalking(false);
        }
    }

    private void UpdateVisualState()
    {
        GD.Print(
            $"Atualizando visual: {_digimon.BaseData.Name} | {_digimon.Activity}"
        );

        if (_digimon.HealthState == HealthState.Sick)
        {
            _sprite.PlaySick();
            return;
        }

        switch (_digimon.Activity)
        {
            case DigimonActivity.Sleeping:
                _sprite.PlaySleep();
                break;

            case DigimonActivity.Training:
                _sprite.PlayTrain();
                break;

            case DigimonActivity.Eating:
                _sprite.PlayEat();
                break;

            default:
                _sprite.PlayIdle();
                break;
        }
    }

    public void SetCenter(Center center)
    {
        _center = center;
    }

    public void SetArea(CenterArea area)
    {
        _currentArea = area;

        GD.Print(
            $"{_digimon.BaseData.Name} entrou na área {_currentArea.GridPosition}"
        );

        if (_currentArea.IsTrainingArea())
        {
            _trainingTimer = TrainingInterval;
            _idleTimer = 0;

            GD.Print(
                $"{_digimon.BaseData.Name} está em uma área de treinamento."
            );
        }
    }

    public bool IsInTrainingArea() { 
        return _currentArea != null && _currentArea.IsTrainingArea();
    }

    public override void _Process(double delta)
    {
        if (_digimon == null)
            return;

        if (_isDragging)
        {
            ProcessDragging();
            return;
        }

        if (_isEating)
        {
            ProcessEating(delta);
            return;
        }

        if (_digimon.Hunger <= 30 && _targetFood == null)
        {
            if (CheckFood())
            {
                _isWalking = true;
            }
        }

        if (!_digimon.CanMove())
        {
            _isWalking = false;
            return;
        }


        if (!_isWalking)
        {
            // Training Area possui timer de treinamento,
            // mas continua usando a mesma lógica de movimentação normal.
            if (_currentArea != null &&
                _currentArea.IsTrainingArea() &&
                !_isTrainingAnimation)
            {
                _trainingTimer -= delta;

                if (_trainingTimer <= 0)
                {
                    ProcessTraining();
                }
            }

            _idleTimer -= delta;

            if (_idleTimer <= 0)
            {
                if (!CheckFood())
                {
                    ChooseNewDestination();
                }
            }

            return;
        }


        Vector2 previousPosition = GlobalPosition;

        GlobalPosition = GlobalPosition.MoveToward(
            _targetPosition,
            _speed * (float)delta
        );

        Vector2 movement = GlobalPosition - previousPosition;

        if (Mathf.Abs(movement.X) > 0.01f)
        {
            _lookingLeft = movement.X > 0;
            _sprite.SetDirection(_lookingLeft);
        }


        if (GlobalPosition.DistanceTo(_targetPosition) < 2f)
        {
            ReachDestination();
        }
    }

    private void ProcessDragging()
    {
        Vector2 mousePosition = GetGlobalMousePosition();

        GlobalPosition = mousePosition + _dragOffset;
    }

    private void ChooseNewDestination()
    {
        const float minimumDistance = 80f;

        CenterArea currentArea = _center.GetAreaAtPosition(GlobalPosition);

        if (currentArea == null)
        {
            GD.PrintErr(
                $"{_digimon.BaseData.Name} não está dentro de nenhuma CenterArea."
            );

            return;
        }

        Vector2 destination = GlobalPosition;

        for (int i = 0; i < 10; i++)
        {
            destination = currentArea.GetRandomPointInside();

            if (GlobalPosition.DistanceTo(destination) >= minimumDistance)
                break;
        }

        _targetPosition = destination;

        _isWalking = true;
        _sprite.SetWalking(true);

        GD.Print(
            $"{_digimon.BaseData.Name} indo para {_targetPosition} " +
            $"dentro da área {currentArea.GridPosition}"
        );
    }

    private bool CheckFood()
    {
        if (_targetFood != null)
            return true;

        if (_center == null)
            return false;

        if (!_digimon.WantsFood())
            return false;

        var food = _center.GetNearestFood(
            GlobalPosition,
            _currentArea
        );

        if (food == null)
            return false;

        _targetFood = food;

        _targetPosition = _targetFood.GlobalPosition;
        _isWalking = true;

        GD.Print(
            $"{_digimon.BaseData.Name} encontrou comida em {food.GlobalPosition}"
        );

        return true;
    }

    private void ProcessEating(double delta)
    {
        if (_targetFood == null || !GodotObject.IsInstanceValid(_targetFood))
        {
            _isEating = false;
            _targetFood = null;
            _idleTimer = 1.0;
            return;
        }

        _eatTimer -= delta;

        if (_eatTimer > 0)
            return;

        Food food = _targetFood.GetFood();

        int eaten = food.Consume(5);

        if (eaten == 0)
        {
            _isEating = false;
            _targetFood = null;
            _idleTimer = 1.0;
            return;
        }

        _digimon.Feed(eaten);

        GD.Print($"{_digimon.BaseData.Name} comeu {eaten}.");
        GD.Print($"Nutrição restante: {food.RemainingNutrition}");

        if (food.IsEmpty())
        {
            GD.Print("A comida acabou.");

            if (GodotObject.IsInstanceValid(_targetFood))
                _center.RemoveFood(_targetFood);
        }

        if(!food.IsEmpty() && !_digimon.IsFull())
        {
            _eatTimer = 1.0;
            return;
        }

        _digimon.StopEating();

        _isEating = false;
        _targetFood = null;
        _idleTimer = 1.0;
    }

    private void ReachDestination()
    {
        _isWalking = false;

        _sprite.SetWalking(false);

        if (_targetFood != null)
        {
            GD.Print($"{_digimon.BaseData.Name} chegou na comida.");

            StartEating();

            return;
        }

        _idleTimer = GD.RandRange(2.0, 5.0);

        GD.Print($"{_digimon.BaseData.Name} chegou ao destino.");
    }

    private void StartEating()
    {
        _isEating = true;

        _eatTimer = 1.0;

        _digimon.StartEating();

        GD.Print(
            $"{_digimon.BaseData.Name} começou a comer."
        );
    }

    private void OnClickAreaInputEvent(
    Node viewport,
    InputEvent @event,
    long shapeIdx)
    {
        if (@event is InputEventMouseButton mouseEvent)
        {
            if (mouseEvent.ButtonIndex == MouseButton.Left &&
                mouseEvent.Pressed)
            {
                _center.InspectDigimon(_digimon);

                StartDragging(mouseEvent.Position);
            }
        }
    }

    private void StartDragging(Vector2 mousePosition)
    {
        if (_digimon == null)
            return;

        if (!_digimon.CanMove())
            return;

        _dragStartPosition = GlobalPosition;
        _dragStartArea = _center.GetAreaAtPosition(GlobalPosition);

        _isDragging = true;

        _positionBeforeDrag = GlobalPosition;

        _dragOffset = GlobalPosition - mousePosition;

        _isWalking = false;
        _sprite.SetWalking(false);

        GD.Print(
            $"{_digimon.BaseData.Name} começou a ser arrastado."
        );
    }

    private void StopDragging()
    {
        if (!_isDragging)
            return;

        _isDragging = false;

        CenterArea targetArea = _center.GetAreaAtPosition(GlobalPosition);

        if (targetArea == null)
        {
            GD.Print(
                $"{_digimon.BaseData.Name} foi solto fora de uma CenterArea. " +
                $"Voltando para a posição anterior."
            );

            GlobalPosition = _dragStartPosition;
            _currentArea = _dragStartArea;

            return;
        }

        _currentArea = targetArea;

        GD.Print(
            $"{_digimon.BaseData.Name} parou de ser arrastado em {GlobalPosition}"
        );

        GD.Print(
            $"{_digimon.BaseData.Name} agora está na área {_currentArea.GridPosition}"
        );

        GD.Print(
            $"{_digimon.BaseData.Name} está em área de treino? {IsInTrainingArea()}"
        );
    }

    public override void _Input(InputEvent @event)
    {
        if (!_isDragging)
            return;

        if (@event is InputEventMouseButton mouseEvent)
        {
            if (mouseEvent.ButtonIndex == MouseButton.Left &&
                !mouseEvent.Pressed)
            {
                StopDragging();
            }
        }
    }

    private async void ProcessTraining()
    {
        if (_digimon == null)
            return;

        if (_isTrainingAnimation)
            return;

        if (_digimon.HealthState == HealthState.Sick)
            return;

        GD.Print(
            $"[TREINO] {_digimon.BaseData.Name} | " +
            $"Stamina: {_digimon.Stamina}/{_digimon.MaxStamina} | " +
            $"Health: {_digimon.HealthState} | " +
            $"Activity: {_digimon.Activity}"
        );

        GD.Print(
            $"[TREINO] {_digimon.BaseData.Name} | " +
            $"Área: {_currentArea?.GridPosition} | " +
            $"É treino: {_currentArea?.IsTrainingArea()} | " +
            $"Stamina: {_digimon.Stamina}/{_digimon.MaxStamina}"
        );

        if (_digimon.Stamina < 10)
        {
            _trainingTimer = TrainingInterval;
            return;
        }

        var trainingSystem = GameManager.Instance.TrainingSystem;

        if (trainingSystem == null)
        {
            GD.PrintErr("TrainingSystem não inicializado.");
            return;
        }

        TrainingType[] trainingTypes =
            Enum.GetValues<TrainingType>();

        int randomIndex = GD.RandRange(
            0,
            trainingTypes.Length - 1
        );

        TrainingType type = trainingTypes[randomIndex];

        GD.Print(
            $"{_digimon.BaseData.Name} iniciou treinamento de {type}."
        );

        var result = trainingSystem.Execute(
            _digimon,
            type
        );

        if (!result.Success)
        {
            GD.Print(
                $"{_digimon.BaseData.Name} não conseguiu treinar. " +
                $"Motivo: {result.Reason}"
            );

            _trainingTimer = TrainingInterval;
            return;
        }

        // ==========================================
        // INÍCIO DO TREINAMENTO
        // ==========================================

        _isTrainingAnimation = true;
        _isWalking = false;

        _sprite.SetWalking(false);

        // Agora o estado lógico também passa a ser Training.
        _digimon.StartTraining();

        // ==========================================
        // ANIMAÇÃO
        // ==========================================

        int trainingLoops = GD.RandRange(1, 2);

        await _sprite.PlayTrainingSequence(trainingLoops);

        // ==========================================
        // APLICA O RESULTADO
        // ==========================================

        _digimon.ApplyTrainingResult(result);

        GD.Print(
            $"{_digimon.BaseData.Name} terminou o treinamento de {type}."
        );

        // ==========================================
        // FIM DO TREINAMENTO
        // ==========================================

        _digimon.StopTraining();

        _isTrainingAnimation = false;

        _trainingTimer = TrainingInterval;

        // Volta a andar normalmente pela área.
        ChooseNewDestination();
    }

    public bool IsPointInside(Vector2 position)
    {
        return GlobalPosition.DistanceTo(position) <= 32f;
    }

    private void OnHealthStateChanged(HealthState state)
    {
        UpdateVisualState();

        if (!_digimon.CanMove())
        {
            _isWalking = false;
            _sprite.SetWalking(false);
        }
    }
}