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
                ChooseNewDestination();
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
            _isWalking = false;

            _sprite.SetWalking(false);

            _idleTimer = GD.RandRange(2.0, 5.0);

            GD.Print(
                $"{_digimon.BaseData.Name} chegou ao destino."
            );
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
}