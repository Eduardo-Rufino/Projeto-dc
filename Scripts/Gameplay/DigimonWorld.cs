using Godot;
using ProjetoDC.Enums;
using ProjetoDC.Scripts.Data;
using ProjetoDC.Scripts.Gameplay;
using ProjetoDC.Scripts.Managers;
using ProjetoDC.Scripts.Models.World;
using ProjetoDC.Scripts.Systems.Battle;
using ProjetoDC.Scripts.Systems.Results;
using ProjetoDC.Scripts.Systems.Training;
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
    private double _poopTimer;
    private const double PoopIntervalSeconds = 120.0;

    private double _dirtyAreaTimer;
    private const double DirtyAreaCheckInterval = 10.0;
    private const int DirtyAreaHappinessPenalty = 2;
    private const int DirtyAreaDisciplinePenalty = 2;

    private double _hpRegenTimer;
    private const double HpRegenInterval = 5.0;
    private const float NormalHpRegenPercentage = 0.01f;
    private const float HospitalHpRegenPercentage = 0.08f;
    private const int DormitorySleepDisciplineBonus = 3;
    private const int WokenWhileSleepingHappinessPenalty = 4;
    private const int FailedTrainingDisciplinePenalty = 1;

    // Dormir no Dormitório: além do bônus de Disciplina que já existia (uma vez, ao começar
    // a dormir), agora cada HORA dormida lá dá Felicidade e um extra de recuperação de
    // Stamina por cima da recuperação normal do sono (DigimonInstance.AdvanceHour).
    private const int DormitorySleepHourlyHappinessBonus = 2;
    private const int DormitoryExtraStaminaRecovery = 3;

    // Dormir fora do Dormitório: a primeira hora é de graça (sem penalidade) - só a partir
    // da segunda hora seguida fora é que começa a perder Felicidade, crescendo a cada hora
    // extra mas numa escala cada vez menor (raiz quadrada, não linear).
    private const int OutsideDormitoryPenaltyScale = 2;

    // Comer comida colocada no Refeitório rende um pouco mais de saciedade por mordida;
    // comer comida estragada (ver Food.IsSpoiled/FoodWorld) machuca Felicidade/Disciplina e
    // arrisca deixar doente - por mordida, então comer bastante acumula.
    private const float RestaurantFeedBonusMultiplier = 1.1f;
    private const int SpoiledFoodHappinessPenalty = 3;
    private const int SpoiledFoodDisciplinePenalty = 2;

    private static readonly Color TrainingGainColor = new(1f, 0.85f, 0.2f);
    private double _trainingTimer;
    private const double TrainingInterval = 5.0;
    private bool _isTrainingAnimation;
    private DigimonStatusIndicator _trainingIndicator;
    private DigimonStatusIndicator _sleepIndicator;
    private DigimonStatusIndicator _sickIndicator;

    // Compartilhado entre todas as instâncias: garante que só um Digimon seja arrastado
    // por vez, mesmo se dois estiverem próximos o bastante pra um clique atingir a
    // ClickArea dos dois (o Godot chama InputEvent em toda Area2D sobreposta sob o
    // cursor, não só na mais "de cima").
    private static DigimonWorld _draggingInstance;

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

    /// <summary>Área que esse Digimon ocupa agora - usada por Center.
    /// IsSpecificTrainingAreaOccupied pra impedir dois Digimons na mesma área de treino
    /// específica de um stat (ver StopDragging).</summary>
    public CenterArea CurrentArea => _currentArea;

    public override void _ExitTree()
    {
        // Segurança: se esse Digimon for removido da árvore no meio de um arraste (troca
        // de cena, etc.), libera o "lock" pra não travar o arraste de todo mundo pra sempre.
        if (_draggingInstance == this)
            _draggingInstance = null;

        if (GameManager.Instance?.ClockSystem != null)
            GameManager.Instance.ClockSystem.HourPassed -= OnDormitoryHourPassed;
    }

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

        _poopTimer = PoopIntervalSeconds;

        GD.Print(
            $"VISUAL RECEBEU {_digimon.BaseData.Name} HASH: {_digimon.GetHashCode()}"
        );

        GD.Print(
            $"ANTES DO EVENTO: {_digimon.BaseData.Name} - {_digimon.Activity}"
        );

        _sprite.SetDigimon(digimon.BaseData.Code);

        _digimon.ActivityChanged += OnActivityChanged;
        _digimon.HealthStateChanged += OnHealthStateChanged;
        _digimon.Evolved += OnEvolved;

        GameManager.Instance.ClockSystem.HourPassed -= OnDormitoryHourPassed;
        GameManager.Instance.ClockSystem.HourPassed += OnDormitoryHourPassed;

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

        if (activity == DigimonActivity.Sleeping &&
            _currentArea != null &&
            _currentArea.IsDormitory())
        {
            _digimon.ChangeDiscipline(DormitorySleepDisciplineBonus);
        }

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

        SyncStatusIndicators();

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

        // Mantém a posição salva sempre em dia (não só num "momento de assentar" específico)
        // - o jogo pode ser fechado a qualquer momento (inclusive no meio de um arrasto ou
        // andando), e sem isso o Digimon sempre reaparecia na área inicial ao recarregar o
        // save, em vez de onde o jogador tinha deixado.
        _digimon.PositionX = GlobalPosition.X;
        _digimon.PositionY = GlobalPosition.Y;

        if (_isDragging)
        {
            ProcessDragging();
            return;
        }

        if (_digimon.Activity != DigimonActivity.Sleeping)
        {
            _poopTimer -= delta;

            if (_poopTimer <= 0)
            {
                Poop();
            }
        }

        _dirtyAreaTimer -= delta;

        if (_dirtyAreaTimer <= 0)
        {
            _dirtyAreaTimer = DirtyAreaCheckInterval;

            CheckDirtyArea();
        }

        _hpRegenTimer -= delta;

        if (_hpRegenTimer <= 0)
        {
            _hpRegenTimer = HpRegenInterval;

            RegenerateHealth();
        }

        if (_isEating)
        {
            ProcessEating(delta);
            return;
        }

        if (_digimon.Hunger <= 30 && _targetFood == null && !_isTrainingAnimation)
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

            if (!_isTrainingAnimation)
            {
                _idleTimer -= delta;

                if (_idleTimer <= 0)
                {
                    if (!CheckFood())
                    {
                        ChooseNewDestination();
                    }
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

        if (food.IsSpoiled)
        {
            _digimon.Feed(eaten);

            _digimon.ChangeHappiness(-SpoiledFoodHappinessPenalty);
            _digimon.ChangeDiscipline(-SpoiledFoodDisciplinePenalty);

            _digimon.TryBecomeSickFromSpoiledFood();

            GD.Print($"{_digimon.BaseData.Name} comeu comida estragada!");
        }
        else
        {
            int feedAmount = food.PlacedInRestaurant
                ? Mathf.RoundToInt(eaten * RestaurantFeedBonusMultiplier)
                : eaten;

            _digimon.Feed(feedAmount);
        }

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

    private void Poop()
    {
        _poopTimer = PoopIntervalSeconds;

        _center.SpawnPoop(GlobalPosition);

        GD.Print(
            $"{_digimon.BaseData.Name} fez coco em {GlobalPosition}."
        );
    }

    private void CheckDirtyArea()
    {
        if (_currentArea == null)
            return;

        if (!_center.AreaHasPoop(_currentArea))
            return;

        _digimon.ChangeHappiness(-DirtyAreaHappinessPenalty);
        _digimon.ChangeDiscipline(-DirtyAreaDisciplinePenalty);

        GD.Print(
            $"{_digimon.BaseData.Name} está num ambiente sujo. " +
            $"Felicidade: {_digimon.Happiness} | Disciplina: {_digimon.Discipline}"
        );
    }

    private void RegenerateHealth()
    {
        bool inHospital = _currentArea != null && _currentArea.IsHospital();

        float percentage = inHospital
            ? HospitalHpRegenPercentage
            : NormalHpRegenPercentage;

        _digimon.RegenerateHealth(percentage);
    }

    /// <summary>
    /// Aplica de uma vez os efeitos que normalmente tickam em tempo real via _Process
    /// (regeneração de HP a cada HpRegenInterval, penalidade de área suja a cada
    /// DirtyAreaCheckInterval) - chamado quando GameManager.SkipSleep avança o relógio sem
    /// tempo real de verdade passar, senão o Digimon perderia toda a regeneração/penalidade
    /// que teria acumulado dormindo em tempo real (o relógio de jogo avança, mas esses dois
    /// timers, sendo baseados no delta de _Process, ficariam parados - GD.Print segue o
    /// mesmo padrão de log de RegenerateHealth/CheckDirtyArea normais).
    ///
    /// Não mexe em coco/comida/treino/movimento: coco já fica parado dormindo (_poopTimer só
    /// conta com Activity != Sleeping) e o resto não se aplica a um Digimon dormindo, então
    /// não precisam de "catch-up" nenhum - só esses dois timers rodam incondicionalmente,
    /// mesmo dormindo.
    /// </summary>
    public void CatchUpPassiveTime(double seconds)
    {
        if (_digimon == null)
            return;

        _hpRegenTimer -= seconds;

        while (_hpRegenTimer <= 0)
        {
            RegenerateHealth();

            _hpRegenTimer += HpRegenInterval;
        }

        _dirtyAreaTimer -= seconds;

        while (_dirtyAreaTimer <= 0)
        {
            CheckDirtyArea();

            _dirtyAreaTimer += DirtyAreaCheckInterval;
        }
    }

    /// <summary>Mostra um indicador flutuante (igual ao de dano em batalha) pra cada stat
    /// que o treino aumentou - normalmente só um por treino, já que cada tipo de treino
    /// afeta um stat só.</summary>
    private void ShowTrainingGainIndicators(TrainingResult result)
    {
        if (result.PhysicDamageGained != 0)
            ShowStatGainIndicator($"+{result.PhysicDamageGained} ATK");

        if (result.SpecialDamageGained != 0)
            ShowStatGainIndicator($"+{result.SpecialDamageGained} ATK");

        if (result.PhysicDefenseGained != 0)
            ShowStatGainIndicator($"+{result.PhysicDefenseGained} DEF");

        if (result.SpecialDefenseGained != 0)
            ShowStatGainIndicator($"+{result.SpecialDefenseGained} DEF");

        if (result.SpeedGained != 0)
            ShowStatGainIndicator($"+{result.SpeedGained} VEL");

        if (result.HealthPointsGained != 0)
            ShowStatGainIndicator($"+{result.HealthPointsGained} HP");
    }

    /// <summary>Garante que os balões de dormindo/doente estejam mostrando ou não de
    /// acordo com o estado atual do Digimon (chamado sempre que o visual é atualizado,
    /// cobrindo tanto mudanças em tempo real quanto o estado já carregado do save).
    /// Doente tem prioridade visual sobre dormindo, assim como o sprite já prioriza.</summary>
    private void SyncStatusIndicators()
    {
        bool shouldShowSick = _digimon.HealthState == HealthState.Sick;
        bool shouldShowSleep = !shouldShowSick && _digimon.Activity == DigimonActivity.Sleeping;

        SetIndicatorVisible(ref _sickIndicator, shouldShowSick, StatusIndicatorKind.Sick);
        SetIndicatorVisible(ref _sleepIndicator, shouldShowSleep, StatusIndicatorKind.Sleeping);
    }

    private void SetIndicatorVisible(ref DigimonStatusIndicator indicator, bool shouldShow, StatusIndicatorKind kind)
    {
        if (shouldShow && indicator == null)
        {
            indicator = ShowStatusIndicator(kind);
        }
        else if (!shouldShow && indicator != null)
        {
            indicator.FadeOutAndFree();
            indicator = null;
        }
    }

    /// <summary>Mostra o balão com o ícone correspondente ao estado sobre a cabeça do
    /// Digimon. É removido (com FadeOutAndFree) quando o estado termina.
    /// Filho do próprio DigimonWorld (não do pai) pra acompanhar o Digimon automaticamente
    /// caso ele seja arrastado enquanto dormindo/doente.</summary>
    private DigimonStatusIndicator ShowStatusIndicator(StatusIndicatorKind kind)
    {
        var scene = GD.Load<PackedScene>("res://Scenes/Center/StatusIndicator.tscn");
        var indicator = scene.Instantiate<DigimonStatusIndicator>();

        AddChild(indicator);

        indicator.Initialize(new Vector2(0, -38f), kind);

        return indicator;
    }

    private void ShowStatGainIndicator(string text)
    {
        var scene = GD.Load<PackedScene>("res://Scenes/Battle/DamageIndicator.tscn");
        var indicator = scene.Instantiate<DamageIndicator>();

        GetParent().AddChild(indicator);

        indicator.Initialize(GlobalPosition + new Vector2(0, -30f), text, TrainingGainColor);
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
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
        {
            if (mouseEvent.ButtonIndex == MouseButton.Left)
            {
                StartDragging();
            }
            else if (mouseEvent.ButtonIndex == MouseButton.Right)
            {
                _center.InspectDigimon(_digimon);
            }
        }
    }

    private void StartDragging()
    {
        if (_digimon == null)
            return;

        if (!_digimon.CanBeDragged())
            return;

        // Já tem outro Digimon sendo arrastado (mesmo clique pegando duas ClickArea
        // sobrepostas) - ignora, só o primeiro a pedir continua.
        if (_draggingInstance != null)
            return;

        if (_digimon.Activity == DigimonActivity.Sleeping)
        {
            _digimon.ChangeHappiness(-WokenWhileSleepingHappinessPenalty);
        }

        _dragStartPosition = GlobalPosition;
        _dragStartArea = _center.GetAreaAtPosition(GlobalPosition);

        _isDragging = true;
        _draggingInstance = this;

        // Fica por cima dos outros Digimons (todos em z_index 0) enquanto é arrastado -
        // senão a ordem de desenho seguia só a ordem em que cada um entrou na árvore,
        // deixando quem estava sendo arrastado por baixo de quem nasceu depois dele.
        ZIndex = 1;

        _positionBeforeDrag = GlobalPosition;

        _dragOffset = GlobalPosition - GetGlobalMousePosition();

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

        ZIndex = 0;

        if (_draggingInstance == this)
            _draggingInstance = null;

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

        // Área de treino específica de um stat só admite 1 Digimon treinando por vez (ver
        // Center.IsSpecificTrainingAreaOccupied) - recusa o segundo, devolvendo ele pra
        // posição de onde começou a ser arrastado, igual ao caso de soltar fora de uma área.
        if (_center.IsSpecificTrainingAreaOccupied(targetArea, this))
        {
            GD.Print(
                $"{_digimon.BaseData.Name} não pode entrar em {targetArea.GridPosition}: " +
                "área de treino específica já ocupada."
            );

            _center.ShowWarning("Essa área de treino específica já está sendo usada por outro Digimon.");

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

        // Área de treino específica de um stat (ver CenterAreaType) força sempre esse stat,
        // com um bônus leve sobre o resultado - a área genérica continua sorteando entre
        // todos os stats, sem bônus.
        TrainingType? forcedType = _currentArea?.ForcedTrainingType;

        TrainingType type;
        float gainMultiplier;

        if (forcedType.HasValue)
        {
            type = forcedType.Value;
            gainMultiplier = TrainingSystem.SpecificAreaBonusMultiplier;
        }
        else
        {
            TrainingType[] trainingTypes = Enum.GetValues<TrainingType>();

            int randomIndex = GD.RandRange(0, trainingTypes.Length - 1);

            type = trainingTypes[randomIndex];
            gainMultiplier = 1f;
        }

        GD.Print(
            $"{_digimon.BaseData.Name} iniciou treinamento de {type}."
        );

        var result = trainingSystem.Execute(
            _digimon,
            type,
            gainMultiplier
        );

        if (!result.Success)
        {
            GD.Print(
                $"{_digimon.BaseData.Name} não conseguiu treinar. " +
                $"Motivo: {result.Reason}"
            );

            _digimon.ChangeDiscipline(-FailedTrainingDisciplinePenalty);

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

        _trainingIndicator = ShowStatusIndicator(StatusIndicatorKind.Training);

        // ==========================================
        // ANIMAÇÃO
        // ==========================================

        int trainingLoops = GD.RandRange(1, 2);

        bool finishedNormally = await _sprite.PlayTrainingSequence(trainingLoops);

        // ==========================================
        // APLICA O RESULTADO
        // ==========================================

        _trainingIndicator?.FadeOutAndFree();
        _trainingIndicator = null;

        _digimon.ApplyTrainingResult(result);

        ShowTrainingGainIndicators(result);

        GD.Print(
            $"{_digimon.BaseData.Name} terminou o treinamento de {type}."
        );

        // ==========================================
        // FIM DO TREINAMENTO
        // ==========================================

        _digimon.StopTraining();

        _isTrainingAnimation = false;

        _trainingTimer = TrainingInterval;

        // Se a sequência foi interrompida (ex.: o Digimon dormiu no meio do treino, trocando
        // o sprite por baixo), não força ele a voltar a andar por cima do que interrompeu -
        // UpdateVisualState()/ChooseNewDestination() já vão rodar de novo naturalmente quando
        // essa outra atividade terminar (ex.: acordar).
        if (finishedNormally && _digimon.CanMove())
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

    /// <summary>
    /// A cada hora de jogo, se o Digimon estiver dormindo: dormir no Dormitório dá
    /// Felicidade e um extra de recuperação de Stamina (por cima da recuperação normal do
    /// sono, que já acontece em DigimonInstance.AdvanceHour antes desse evento, chamado por
    /// GameManager); dormir fora do Dormitório é de graça na primeira hora, mas a partir da
    /// segunda hora seguida começa a perder Felicidade, crescendo a cada hora extra numa
    /// escala cada vez menor. Não sendo hora de sono (acordado), zera a contagem de horas
    /// fora - a próxima vez que dormir fora do Dormitório começa a contagem do zero.
    /// </summary>
    private void OnDormitoryHourPassed()
    {
        if (_digimon == null)
            return;

        if (_digimon.Activity != DigimonActivity.Sleeping)
        {
            _digimon.HoursSleptOutsideDormitory = 0;
            return;
        }

        bool inDormitory = _currentArea != null && _currentArea.IsDormitory();

        if (inDormitory)
        {
            _digimon.HoursSleptOutsideDormitory = 0;

            _digimon.ChangeHappiness(DormitorySleepHourlyHappinessBonus);
            _digimon.RecoverStamina(DormitoryExtraStaminaRecovery);

            return;
        }

        _digimon.HoursSleptOutsideDormitory++;

        // Primeira hora fora é de graça - só penaliza a partir da segunda.
        if (_digimon.HoursSleptOutsideDormitory <= 1)
            return;

        int extraHours = _digimon.HoursSleptOutsideDormitory - 1;

        int penalty = Mathf.CeilToInt(
            OutsideDormitoryPenaltyScale * Mathf.Sqrt(extraHours)
        );

        _digimon.ChangeHappiness(-penalty);
    }

    // A animação em si (centralizada na tela, com o resto do jogo pausado) roda no
    // EvolutionOverlay - aqui só para de andar e entra na fila do Center (que garante só
    // uma animação de evolução por vez, mesmo se vários Digimons evoluírem juntos, ex.:
    // depois de uma batalha em time). O sprite/estado local só troca quando a animação
    // dessa evolução específica terminar (ver ApplyEvolvedSprite).
    private void OnEvolved(DigimonData oldForm, DigimonData newForm)
    {
        _isWalking = false;
        _sprite.SetWalking(false);

        _center.EnqueueEvolution(oldForm, newForm, this);
    }

    /// <summary>Chamado pelo Center quando a animação de evolução (EvolutionOverlay) dessa
    /// instância termina - só então o sprite/visual local troca pra forma nova.</summary>
    public void ApplyEvolvedSprite(DigimonData newForm)
    {
        _sprite.SetDigimon(newForm.Code);

        UpdateVisualState();
    }
}