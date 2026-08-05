using Godot;
using ProjetoDC.Enums;
using ProjetoDC.Scripts.Gameplay;
using ProjetoDC.Scripts.UI;

namespace ProjetoDC.Scripts.World;

public partial class DigimonWorld : Node2D
{
    private DigimonSprite _sprite;
    private Center _center;
    private bool _lookingLeft;
    private FoodWorld _targetFood;
    private bool _isEating;
    private double _eatTimer;

    private Vector2 _targetPosition;

    private bool _isWalking;

    private float _speed = 40f;

    private double _idleTimer;

    public DigimonInstance _digimon { get; private set; }

    public override void _Ready()
    {
        _sprite = GetNode<DigimonSprite>("DigimonSprite");

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

    public override void _Process(double delta)
    {
        if (_digimon == null)
            return;

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

    private void ChooseNewDestination()
    {
        const float minimumDistance = 80f;

        Vector2 destination = GlobalPosition;

        for (int i = 0; i < 10; i++)
        {
            destination = _center.GetRandomWalkPoint();

            if (GlobalPosition.DistanceTo(destination) >= minimumDistance)
                break;
        }

        _targetPosition = destination;

        _isWalking = true;
        _sprite.SetWalking(true);

        GD.Print($"{_digimon.BaseData.Name} indo para {_targetPosition}");
    }

    private bool CheckFood()
    {
        if (_targetFood != null)
            return true;

        if (_center == null)
            return false;

        if (!_digimon.WantsFood())
            return false;

        var food = _center.GetNearestFood(GlobalPosition);

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
}