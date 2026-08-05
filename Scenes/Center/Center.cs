using Godot;
using ProjetoDC.Scripts.Gameplay;
using ProjetoDC.Scripts.Managers;
using ProjetoDC.Scripts.Models.World;
using ProjetoDC.Scripts.World;
using System.Collections.Generic;
using System.Linq;

public partial class Center : Node2D
{
    private const int MaxFoods = 4;

    private PackedScene _digimonScene;
    private PackedScene _foodScene;
    private PackedScene _centerAreaScene;

    private List<Marker2D> _spawnPoints = new();

    private List<Marker2D> _walkPoints = new();

    private readonly Vector2 _areaGridOrigin = new Vector2(647, 366);

    private readonly List<FoodWorld> _foods = new();

    private readonly List<DigimonWorld> _digimonWorlds = new();

    private readonly Dictionary<Vector2I, CenterArea> _centerAreas = new();

    private List<CenterExpansionSlot> _expansionSlots = new();

    private Node2D _digimonsContainer;
    private Node2D _expansionSlotsVisual;

    private readonly RandomNumberGenerator _rng = new();

    private PackedScene _expansionSlotScene =
    GD.Load<PackedScene>("res://Scenes/Center/CenterExpansionSlotVisual.tscn");

    public override void _Ready()
    {
        PrintTree();

        _digimonScene = GD.Load<PackedScene>(
            "res://Scenes/Center/DigimonWorld.tscn"
        );

        _foodScene = GD.Load<PackedScene>(
            "res://Scenes/Center/FoodWorld.tscn"
        );

        _centerAreaScene = GD.Load<PackedScene>(
            "res://Scenes/Center/Areas/CenterArea.tscn"
        );

        

        if (_digimonScene == null)
        {
            GD.PrintErr("Não foi possível carregar DigimonWorld.tscn");
            return;
        }

        var spawnRoot = GetNode<Node2D>("Digimons/SpawnPoints");

        foreach (Node child in spawnRoot.GetChildren())
        {
            if (child is Marker2D marker)
                _spawnPoints.Add(marker);
        }

        var walkRoot = GetNode<Node2D>("Digimons/WalkPoints");

        foreach (Node child in walkRoot.GetChildren())
        {
            if (child is Marker2D marker)
            {
                _walkPoints.Add(marker);
                GD.Print($"WalkPoint adicionado: {marker.Name} - {marker.GlobalPosition}");
            }

        }

        _digimonsContainer = GetNode<Node2D>("Digimons");

        SpawnDigimons();

        RegisterCenterAreas();
        RefreshExpansionSlots();
        CreateExpansionSlotVisuals();

    }

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("spawn_food"))
        {
            SpawnTestFood();
        }


        if (Input.IsActionJustPressed("test_food"))
        {
            var food = GetNearestFood(new Vector2(570, 367));

            if (food != null)
            {
                GD.Print($"Comida encontrada em: {food.GlobalPosition}");
            }
            else
            {
                GD.Print("Nenhuma comida encontrada.");
            }
        }
    }

    private void SpawnDigimons()
    {
        var digimons = GameManager.Instance.CenterService.GetAllDigimons();

        for (int i = 0; i < digimons.Count; i++)
        {
            if (i >= _spawnPoints.Count)
            {
                GD.PrintErr("Não há SpawnPoints suficientes.");
                break;
            }

            SpawnDigimon(digimons[i], _spawnPoints[i]);
        }
    }

    private DigimonWorld SpawnDigimon(
        DigimonInstance digimon,
        Marker2D spawnPoint)
    {
        GD.Print($"Criando DigimonWorld: {digimon.BaseData.Name}");

        var instance = _digimonScene.Instantiate<DigimonWorld>();

        _digimonsContainer.AddChild(instance);

        instance.SetCenter(this);

        instance.GlobalPosition = spawnPoint.GlobalPosition;

        instance.Initialize(digimon);

        GD.Print($"Spawn em {instance.GlobalPosition}");

        _digimonWorlds.Add(instance);

        return instance;
    }

    private void SpawnFood()
    {
        GD.Print("Criando comida!");

        var food = _foodScene.Instantiate<Node2D>();

        AddChild(food);

        food.GlobalPosition = new Vector2(500, 300);
    }

    private void SpawnTestFood()
    {
        if (_foods.Count >= MaxFoods)
        {
            GD.Print("Limite de comidas atingido.");
            return;
        }

        var food = new Food("Carne", 20);

        var world = _foodScene.Instantiate<FoodWorld>();

        _digimonsContainer.AddChild(world);

        world.GlobalPosition = new Vector2(
            GD.RandRange(550, 850),
            GD.RandRange(200, 400)
        );

        world.Initialize(food);

        _foods.Add(world);

        GD.Print($"Comida criada em: {world.GlobalPosition}");
    }

    public void RemoveFood(FoodWorld food)
    {
        if (food == null)
            return;

        if (!GodotObject.IsInstanceValid(food))
            return;

        if (!_foods.Remove(food))
            return;

        food.QueueFree();

        GD.Print($"Comidas restantes: {_foods.Count}");
    }

    public FoodWorld GetAvailableFood()
    {
        foreach (var food in _foods)
        {
            if (food.HasFood())
                return food;
        }

        return null;
    }

    public bool HasFood()
    {
        foreach (var food in _foods)
        {
            if (food.HasFood())
                return true;
        }

        return false;
    }

    public FoodWorld GetNearestFood(Vector2 position)
    {
        FoodWorld nearestFood = null;
        float nearestdistance = float.MaxValue;

        foreach (var food in _foods)
        {
            if (!food.HasFood())
                continue;

            float currentDistance = position.DistanceTo(food.GlobalPosition);

            if (currentDistance < nearestdistance)
            {
                nearestdistance = currentDistance;
                nearestFood = food;
            }
        }

        return nearestFood;
    }

    public Vector2 GetRandomWalkPoint()
    {
        if (_walkPoints.Count == 0)
            return Vector2.Zero;

        int index = _rng.RandiRange(0, _walkPoints.Count - 1);

        return _walkPoints[index].GlobalPosition;
    }

    private void CreateInitialArea()
    {
        CenterArea area = _centerAreaScene.Instantiate<CenterArea>();

        area.GridPosition = Vector2I.Zero;

        AddChild(area);

        area.Position = new Vector2(647, 366);

        GD.Print(
            $"CenterArea instanciada: {area.Name} | Grid: {area.GridPosition} | Posição: {area.Position}"
        );
    }

    public bool AddArea(Vector2I gridPosition)
    {
        if (IsAreaOccupied(gridPosition))
        {
            GD.Print($"Não é possível adicionar área em {gridPosition}: posição ocupada.");
            return false;
        }

        var availablePositions = GetAllAvailablePositions();

        if (!availablePositions.Contains(gridPosition))
        {
            GD.Print($"Não é possível adicionar área em {gridPosition}: posição não está conectada ao centro.");
            return false;
        }

        CreateCenterArea(gridPosition);

        return true;
    }

    private void RegisterCenterAreas()
    {
        _centerAreas.Clear();

        foreach (Node child in GetNode("CenterAreas").GetChildren())
        {
            if (child is CenterArea area)
            {
                _centerAreas[area.GridPosition] = area;

                GD.Print(
                    $"Área registrada: {area.Name} | Grid: {area.GridPosition}"
                );
            }
        }
    }

    private bool IsAreaOccupied(Vector2I gridPosition)
    {
        return _centerAreas.ContainsKey(gridPosition);
    }

    public List<Vector2I> GetAvailableNeighbors(Vector2I gridPosition)
    {
        var available = new List<Vector2I>();

        foreach (var neighbor in CenterGrid.GetNeighbors(gridPosition))
        {
            if (!IsAreaOccupied(neighbor))
            {
                available.Add(neighbor);
            }
        }

        return available;
    }

    private List<Vector2I> GetAllAvailablePositions()
    {
        var available = new List<Vector2I>();

        foreach (var area in _centerAreas.Values)
        {
            var neighbors = GetAvailableNeighbors(area.GridPosition);

            foreach (var neighbor in neighbors)
            {
                if (!available.Contains(neighbor))
                {
                    available.Add(neighbor);
                }
            }
        }

        return available;
    }

    private void CreateCenterArea(Vector2I gridPosition)
    {
        var scene = GD.Load<PackedScene>(
            "res://Scenes/Center/Areas/CenterArea.tscn"
        );

        if (scene == null)
        {
            GD.PrintErr("Não foi possível carregar CenterArea.tscn.");
            return;
        }

        var area = scene.Instantiate<CenterArea>();

        area.GridPosition = gridPosition;

        GetNode("CenterAreas").AddChild(area);

        area.Position = GridToWorldPosition(gridPosition);

        _centerAreas[gridPosition] = area;

        GD.Print(
            $"Nova área criada: {area.Name} | Grid: {gridPosition} | Position: {area.Position}"
        );
    }

    private void RefreshExpansionSlots()
    {
        _expansionSlots.Clear();

        var availablePositions = GetAllAvailablePositions();

        foreach (var position in availablePositions)
        {
            _expansionSlots.Add(
                new CenterExpansionSlot(position)
            );
        }

        GD.Print("=== SLOTS DE EXPANSÃO ===");

        foreach (var slot in _expansionSlots)
        {
            GD.Print($"Slot disponível: {slot.GridPosition}");
        }
    }

    private void RefreshExpansionSlotVisuals()
    {
        if (_expansionSlotsVisual != null)
        {
            _expansionSlotsVisual.QueueFree();
            _expansionSlotsVisual = null;
        }

        RefreshExpansionSlots();
        CreateExpansionSlotVisuals();
    }

    private void CreateExpansionSlotVisuals()
    {
        _expansionSlotsVisual = new Node2D
        {
            Name = "ExpansionSlotsVisual"
        };

        AddChild(_expansionSlotsVisual);

        foreach (var slot in _expansionSlots)
        {
            var visual = _expansionSlotScene.Instantiate<CenterExpansionSlotVisual>();

            visual.Initialize(slot.GridPosition);

            visual.Position = GridToWorldPosition(slot.GridPosition);

            visual.SlotClicked += OnExpansionSlotClicked;

            _expansionSlotsVisual.AddChild(visual);

            GD.Print(
                $"Visual do slot criado: {slot.GridPosition} -> {visual.Position}"
            );
        }
    }

    private Vector2 GridToWorldPosition(Vector2I gridPosition)
    {
        const float horizontalSpacing = 375f;
        const float verticalSpacing = 216.50635f;

        return new Vector2(
            _areaGridOrigin.X + gridPosition.X * horizontalSpacing,
            _areaGridOrigin.Y + gridPosition.X * verticalSpacing
                + gridPosition.Y * verticalSpacing * 2f
        );
    }

    private void OnExpansionSlotClicked(Vector2I gridPosition)
    {
        GD.Print($"Tentando expandir para: {gridPosition}");

        if (AddArea(gridPosition))
        {
            RefreshExpansionSlotVisuals();
        }
    }

    private void TestAreaOccupancy()
    {
        GD.Print($"(0, 0) ocupado? {IsAreaOccupied(new Vector2I(0, 0))}");
        GD.Print($"(1, 0) ocupado? {IsAreaOccupied(new Vector2I(1, 0))}");
        GD.Print($"(1, -1) ocupado? {IsAreaOccupied(new Vector2I(1, -1))}");
    }

    private void TestAvailableNeighbors()
    {
        var available = GetAvailableNeighbors(Vector2I.Zero);

        foreach (var position in available)
        {
            GD.Print($"Espaço disponível ao redor do centro: {position}");
        }
    }

    private void TestCreateArea()
    {
        var available = GetAvailableNeighbors(Vector2I.Zero);

        if (available.Count == 0)
        {
            GD.Print("Não existem espaços disponíveis.");
            return;
        }

        CreateCenterArea(available[0]);

        available = GetAvailableNeighbors(Vector2I.Zero);

        if (available.Count == 0)
        {
            GD.Print("Não existem mais espaços disponíveis.");
            return;
        }

        CreateCenterArea(available[0]);
    }

    private void TestAllAvailablePositions()
    {
        var available = GetAllAvailablePositions();

        GD.Print("=== POSIÇÕES DISPONÍVEIS ===");

        foreach (var position in available)
        {
            GD.Print($"Disponível: {position}");
        }
    }

    private void TestAddArea()
    {
        AddArea(new Vector2I(1, 0));
        AddArea(new Vector2I(2, 0));
    }
}