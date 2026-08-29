using Godot;
using ProjetoDC.Enums;
using ProjetoDC.Scripts.Core.Results;
using ProjetoDC.Scripts.Data;
using ProjetoDC.Scripts.Gameplay;
using ProjetoDC.Scripts.Managers;
using ProjetoDC.Scripts.Models.World;
using ProjetoDC.Scripts.UI;
using ProjetoDC.Scripts.World;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Center : Node2D
{
    private const int MaxFoods = 4;

    private PackedScene _digimonScene;
    private PackedScene _foodScene;
    private PackedScene _poopScene;
    private PackedScene _eggScene;
    private PackedScene _centerAreaScene;

    private List<Marker2D> _spawnPoints = new();

    private readonly Vector2 _areaGridOrigin = new Vector2(647, 366);

    private readonly List<FoodWorld> _foods = new();

    private readonly List<PoopWorld> _poops = new();

    private readonly List<EggWorld> _eggs = new();

    private readonly List<DigimonWorld> _digimonWorlds = new();

    private readonly Dictionary<Vector2I, CenterArea> _centerAreas = new();

    private Vector2I _pendingAreaPosition;

    private List<CenterExpansionSlot> _expansionSlots = new();

    private Node2D _digimonsContainer;
    private Node2D _expansionSlotsVisual;

    private AreaBuildMenu _areaBuildMenu;
    private HUD _hud;
    private ShopScreen _shopScreen;
    private TeamSelectionScreen _teamSelectionScreen;
    private CanvasLayer _canvasLayer;

    private FoodWorld _placingFood;
    private bool _isPlacingFood;

    private PackedScene _medicineScene;

    private MedicineWorld _placingMedicine;
    private bool _isPlacingMedicine;

    private PackedScene _broomScene;

    private BroomWorld _placingBroom;
    private bool _isPlacingBroom;

    private const float CleanRadius = 28f;
    private const int PoopFreshnessHours = 3;
    private const int CleanQuicklyHappinessBonus = 3;

    private readonly RandomNumberGenerator _rng = new();

    public event Action<DigimonInstance> DigimonInspected;

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

        _poopScene = GD.Load<PackedScene>(
            "res://Scenes/Center/PoopWorld.tscn"
        );

        _eggScene = GD.Load<PackedScene>(
            "res://Scenes/Center/EggWorld.tscn"
        );

        _centerAreaScene = GD.Load<PackedScene>(
            "res://Scenes/Center/Areas/CenterArea.tscn"
        );

        _medicineScene = GD.Load<PackedScene>(
            "res://Scenes/Center/MedicineWorld.tscn"
         );

        _broomScene = GD.Load<PackedScene>(
            "res://Scenes/Center/BroomWorld.tscn"
        );

        _areaBuildMenu = GetNode<AreaBuildMenu>(
            "CanvasLayer/AreaBuildMenu"
        );

        _hud = GetNode<HUD>("CanvasLayer/HUD");

        _shopScreen = GetNode<ShopScreen>("CanvasLayer/ShopScreen");

        _shopScreen.BackPressed += OnShopBackPressed;
        _shopScreen.EggPurchased += OnEggPurchased;

        _teamSelectionScreen = GetNode<TeamSelectionScreen>("CanvasLayer/TeamSelectionScreen");

        _teamSelectionScreen.BackPressed += OnTeamSelectionBackPressed;

        _areaBuildMenu.AreaTypeSelected += OnAreaTypeSelected;

        GameManager.Instance.EggSystem.EggHatched -= OnEggHatched;
        GameManager.Instance.EggSystem.EggHatched += OnEggHatched;

        GameManager.Instance.TeamBattleFinished -= OnTeamBattleFinished;
        GameManager.Instance.TeamBattleFinished += OnTeamBattleFinished;

        GameManager.Instance.TeamBattleStarted -= OnTeamBattleStarted;
        GameManager.Instance.TeamBattleStarted += OnTeamBattleStarted;

        _canvasLayer = GetNode<CanvasLayer>("CanvasLayer");

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

        _digimonsContainer = GetNode<Node2D>("Digimons");
        
        RegisterCenterAreas();
        RestoreBuiltAreas();
        SpawnDigimons();
        RestoreFoods();
        RestorePoops();
        RestoreEggs();
        RefreshExpansionSlots();
        CreateExpansionSlotVisuals();

    }

    public override void _ExitTree()
    {
        if (GameManager.Instance?.EggSystem != null)
        {
            GameManager.Instance.EggSystem.EggHatched -= OnEggHatched;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.TeamBattleFinished -= OnTeamBattleFinished;
            GameManager.Instance.TeamBattleStarted -= OnTeamBattleStarted;
        }
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("spawn_food"))
        {
            SpawnTestFood();
        }

        if (Input.IsActionJustPressed("test_food"))
        {
            Vector2 testPosition = new Vector2(570, 367);

            CenterArea testArea = GetAreaAtPosition(testPosition);

            var food = GetNearestFood(
                testPosition,
                testArea
            );

            if (food != null)
            {
                GD.Print(
                    $"Comida encontrada em: {food.GlobalPosition}"
                );
            }
            else
            {
                GD.Print("Nenhuma comida encontrada.");
            }
        }

        if (_isPlacingFood && _placingFood != null)
        {
            _placingFood.GlobalPosition = GetGlobalMousePosition();
        }

        if (_isPlacingMedicine && _placingMedicine != null)
        {
            _placingMedicine.GlobalPosition =
                GetGlobalMousePosition();
        }

        if (_isPlacingBroom && _placingBroom != null)
        {
            _placingBroom.GlobalPosition =
                GetGlobalMousePosition();
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
        return SpawnDigimon(digimon, spawnPoint.GlobalPosition);
    }

    private DigimonWorld SpawnDigimon(
        DigimonInstance digimon,
        Vector2 position)
    {
        GD.Print($"Criando DigimonWorld: {digimon.BaseData.Name}");

        var instance = _digimonScene.Instantiate<DigimonWorld>();

        _digimonsContainer.AddChild(instance);

        instance.SetCenter(this);

        instance.GlobalPosition = position;

        instance.Initialize(digimon);

         CenterArea initialArea = GetAreaAtPosition(instance.GlobalPosition);

        if (initialArea != null) {
            instance.SetArea(initialArea);
        }
        else{
            GD.PrintErr($"{digimon.BaseData.Name} nasceu fora de uma CenterArea");
        }

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

        food.PositionX = world.GlobalPosition.X;
        food.PositionY = world.GlobalPosition.Y;

        world.Initialize(food);

        _foods.Add(world);

        GameManager.Instance.Save.Center.Foods.Add(food);

        GD.Print($"Comida criada em: {world.GlobalPosition}");
    }

    private void RestoreFoods()
    {
        foreach (var food in GameManager.Instance.Save.Center.Foods.ToList())
        {
            if (food.IsEmpty())
            {
                GameManager.Instance.Save.Center.Foods.Remove(food);
                continue;
            }

            var world = _foodScene.Instantiate<FoodWorld>();

            _digimonsContainer.AddChild(world);

            world.GlobalPosition = new Vector2(food.PositionX, food.PositionY);

            world.Initialize(food);

            _foods.Add(world);
        }

        GD.Print($"Comidas restauradas: {_foods.Count}");
    }

    public void RemoveFood(FoodWorld food)
    {
        if (food == null)
            return;

        if (!GodotObject.IsInstanceValid(food))
            return;

        if (!_foods.Remove(food))
            return;

        var foodData = food.GetFood();

        if (foodData != null)
            GameManager.Instance.Save.Center.Foods.Remove(foodData);

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

    public FoodWorld GetNearestFood(Vector2 position, CenterArea area)
    {
        FoodWorld nearestFood = null;
        float nearestDistance = float.MaxValue;

        if (area == null)
            return null;

        foreach (var food in _foods)
        {
            if (!food.HasFood())
                continue;

            CenterArea foodArea = GetAreaAtPosition(food.GlobalPosition);

            if (foodArea != area)
                continue;

            float currentDistance = position.DistanceTo(food.GlobalPosition);

            if (currentDistance < nearestDistance)
            {
                nearestDistance = currentDistance;
                nearestFood = food;
            }
        }

        return nearestFood;
    }

    public PoopWorld SpawnPoop(Vector2 position)
    {
        if (_poopScene == null)
        {
            GD.PrintErr("PoopWorld.tscn não foi carregado.");
            return null;
        }

        var poop = _poopScene.Instantiate<PoopWorld>();

        _digimonsContainer.AddChild(poop);

        poop.GlobalPosition = position;

        var world = GameManager.Instance.Save.World;

        var poopData = new Poop
        {
            PositionX = position.X,
            PositionY = position.Y,
            CreatedOnDay = world.CurrentDay,
            CreatedOnHour = world.CurrentHour
        };

        poop.Initialize(poopData);

        GameManager.Instance.Save.Center.Poops.Add(poopData);

        _poops.Add(poop);

        GD.Print($"Coco criado em {position}.");

        return poop;
    }

    private void RestorePoops()
    {
        foreach (var poopData in GameManager.Instance.Save.Center.Poops)
        {
            var poop = _poopScene.Instantiate<PoopWorld>();

            _digimonsContainer.AddChild(poop);

            poop.GlobalPosition = new Vector2(poopData.PositionX, poopData.PositionY);

            poop.Initialize(poopData);

            _poops.Add(poop);
        }

        GD.Print($"Cocos restaurados: {_poops.Count}");
    }

    public void RemovePoop(PoopWorld poop)
    {
        if (poop == null)
            return;

        if (!GodotObject.IsInstanceValid(poop))
            return;

        if (!_poops.Remove(poop))
            return;

        var poopData = poop.GetPoop();

        if (poopData != null)
        {
            GameManager.Instance.Save.Center.Poops.Remove(poopData);

            RewardQuickCleaning(poopData, poop.GlobalPosition);
        }

        poop.QueueFree();

        GD.Print($"Cocos restantes: {_poops.Count}");
    }

    private void RewardQuickCleaning(Poop poopData, Vector2 position)
    {
        var world = GameManager.Instance.Save.World;

        bool isFresh = world.CurrentDay == poopData.CreatedOnDay &&
            (world.CurrentHour - poopData.CreatedOnHour) <= PoopFreshnessHours;

        if (!isFresh)
            return;

        CenterArea area = GetAreaAtPosition(position);

        if (area == null)
            return;

        foreach (var digimonWorld in _digimonWorlds)
        {
            if (!GodotObject.IsInstanceValid(digimonWorld))
                continue;

            if (GetAreaAtPosition(digimonWorld.GlobalPosition) != area)
                continue;

            digimonWorld._digimon?.ChangeHappiness(CleanQuicklyHappinessBonus);
        }
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

    public bool AddArea(Vector2I gridPosition, CenterAreaType areaType)
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

        CreateCenterArea(gridPosition, areaType);

        // Persiste só a área nova construída pelo jogador - RestoreBuiltAreas() no _Ready()
        // não passa por aqui de novo, então não duplica no save a cada load.
        GameManager.Instance.Save.Center.BuiltAreas.Add(new CenterAreaData
        {
            GridX = gridPosition.X,
            GridY = gridPosition.Y,
            AreaType = areaType
        });

        return true;
    }

    private void RestoreBuiltAreas()
    {
        foreach (var areaData in GameManager.Instance.Save.Center.BuiltAreas)
        {
            var gridPosition = new Vector2I(areaData.GridX, areaData.GridY);

            if (IsAreaOccupied(gridPosition))
                continue;

            CreateCenterArea(gridPosition, areaData.AreaType);
        }

        GD.Print($"Áreas construídas restauradas: {GameManager.Instance.Save.Center.BuiltAreas.Count}");
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

    private void CreateCenterArea(Vector2I gridPosition, CenterAreaType areaType)
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

        area.SetAreaType(areaType);

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
            _expansionSlotsVisual.Free();
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
        GD.Print($"SLOT CLICADO! Grid: {gridPosition}");

        _pendingAreaPosition = gridPosition;

        GD.Print(
            $"Escolhendo tipo de área para {_pendingAreaPosition}"
        );

        _areaBuildMenu.Open();
    }

    public CenterArea GetAreaAtPosition(Vector2 position)
    {
        foreach (var area in _centerAreas.Values)
        {
            if (area.IsPointInside(position))
                return area;
        }

        return null;
    }

    private void OnAreaTypeSelected(CenterAreaType areaType)
    {
        GD.Print(
            $"Construindo área {_pendingAreaPosition} " +
            $"do tipo {areaType}"
        );

        if (AddArea(_pendingAreaPosition, areaType))
        {
            RefreshExpansionSlotVisuals();
        }
    }

    public void OpenShop()
    {
        _shopScreen.RefreshUI();

        _shopScreen.Visible = true;
    }

    private void OnShopBackPressed()
    {
        _shopScreen.Visible = false;
    }

    public void OpenTeamSelection()
    {
        _teamSelectionScreen.RefreshUI();

        _teamSelectionScreen.Visible = true;
    }

    private void OnTeamSelectionBackPressed()
    {
        _teamSelectionScreen.Visible = false;
    }

    private void OnTeamBattleStarted()
    {
        // Esconde o Center inteiro (mundo + HUD) durante o combate, igual à tela de
        // seleção de time - a arena de batalha é uma cena separada por cima, então o
        // Center continua existindo/simulando, só não deve aparecer nem ser clicável.
        Visible = false;
        _canvasLayer.Visible = false;
    }

    private void OnTeamBattleFinished(BattleResult result)
    {
        Visible = true;
        _canvasLayer.Visible = true;

        _teamSelectionScreen.Visible = false;

        // A arena de batalha usa sua própria Camera2D (precisa pra enquadrar a luta
        // certo, independente de onde a câmera do Center estava olhando); ao encerrar
        // a arena, garantimos que a câmera do Center volte a ser a atual.
        GetNode<Camera2D>("Camera2D").MakeCurrent();
    }

    public void InspectDigimon(DigimonInstance digimon)
    {
        if (digimon == null)
            return;

        _hud.InspectDigimon(digimon);
    }

    public void StartFoodPlacement()
    {
        if (_isPlacingFood)
            return;

        if (_foods.Count >= MaxFoods)
        {
            GD.Print("Limite de comidas atingido.");
            return;
        }

        _placingFood = _foodScene.Instantiate<FoodWorld>();

        _digimonsContainer.AddChild(_placingFood);

        _placingFood.Initialize(
            new Food("Carne", 20)
        );

        _placingFood.SetPlacementMode(true);

        _isPlacingFood = true;
    }

    public void PlaceFood()
    {
        if (_placingFood == null)
            return;

        CenterArea area = GetAreaAtPosition(
            _placingFood.GlobalPosition
        );

        if (area == null)
        {
            GD.Print("Não é possível colocar comida fora de uma área do Center.");

            _placingFood.QueueFree();
            _placingFood = null;
            _isPlacingFood = false;

            return;
        }

        _placingFood.SetPlacementMode(false);

        Food food = _placingFood.GetFood();

        food.PositionX = _placingFood.GlobalPosition.X;
        food.PositionY = _placingFood.GlobalPosition.Y;

        _foods.Add(_placingFood);

        GameManager.Instance.Save.Center.Foods.Add(food);

        GD.Print(
            $"Carne colocada em {_placingFood.GlobalPosition}"
        );

        _placingFood = null;
        _isPlacingFood = false;
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

    private DigimonWorld GetDigimonAtPosition(Vector2 position)
    {
        foreach (var digimon in _digimonWorlds)
        {
            if (!GodotObject.IsInstanceValid(digimon))
                continue;

            if (digimon.IsPointInside(position))
                return digimon;
        }

        return null;
    }

    public void StartMedicinePlacement()
    {
        if (_isPlacingMedicine)
            return;

        if (GameManager.Instance.Save.Center.Medicine <= 0)
        {
            GD.Print("Você não possui remédios.");
            return;
        }

        if (_medicineScene == null)
        {
            GD.PrintErr(
                "MedicineWorld.tscn não foi carregado."
            );

            return;
        }



        _placingMedicine =
            _medicineScene.Instantiate<MedicineWorld>();

        AddChild(_placingMedicine);

        _placingMedicine.SetPlacementMode(true);

        _isPlacingMedicine = true;

        GD.Print("Iniciando posicionamento de medicamento.");
    }

    public void PlaceMedicine()
    {
        if (!_isPlacingMedicine ||
            _placingMedicine == null)
            return;

        Vector2 position =
            _placingMedicine.GlobalPosition;

        DigimonWorld digimonWorld =
            GetDigimonAtPosition(position);

        if (digimonWorld == null)
        {
            GD.Print(
                "Medicamento não foi colocado sobre nenhum Digimon."
            );

            _placingMedicine.QueueFree();

            _placingMedicine = null;
            _isPlacingMedicine = false;

            return;
        }

        DigimonInstance digimon =
            digimonWorld._digimon;

        if (digimon == null)
        {
            GD.PrintErr(
                "DigimonWorld encontrado, mas sem DigimonInstance."
            );

            _placingMedicine.QueueFree();

            _placingMedicine = null;
            _isPlacingMedicine = false;

            return;
        }

        SystemResult result =
            digimon.UseMedicine(digimon);

        GD.Print(
            $"Resultado do medicamento: {result.Reason}"
        );

        _placingMedicine.QueueFree();

        _placingMedicine = null;
        _isPlacingMedicine = false;
    }

    public void StartBroomPlacement()
    {
        if (_isPlacingBroom)
            return;

        if (_broomScene == null)
        {
            GD.PrintErr("BroomWorld.tscn não foi carregado.");
            return;
        }

        _placingBroom = _broomScene.Instantiate<BroomWorld>();

        AddChild(_placingBroom);

        _placingBroom.SetPlacementMode(true);

        _isPlacingBroom = true;

        GD.Print("Iniciando limpeza.");
    }

    public void UseBroom()
    {
        if (!_isPlacingBroom || _placingBroom == null)
            return;

        Vector2 position = _placingBroom.GlobalPosition;

        PoopWorld poop = GetPoopAtPosition(position);

        if (poop != null)
        {
            RemovePoop(poop);

            GD.Print("Coco limpo.");
        }
        else
        {
            FoodWorld food = GetFoodAtPosition(position);

            if (food != null)
            {
                RemoveFood(food);

                GD.Print("Carne removida com a vassoura.");
            }
        }

        _placingBroom.QueueFree();

        _placingBroom = null;
        _isPlacingBroom = false;
    }

    private PoopWorld GetPoopAtPosition(Vector2 position)
    {
        foreach (var poop in _poops)
        {
            if (!GodotObject.IsInstanceValid(poop))
                continue;

            if (position.DistanceTo(poop.GlobalPosition) <= CleanRadius)
                return poop;
        }

        return null;
    }

    private FoodWorld GetFoodAtPosition(Vector2 position)
    {
        foreach (var food in _foods)
        {
            if (!GodotObject.IsInstanceValid(food))
                continue;

            if (position.DistanceTo(food.GlobalPosition) <= CleanRadius)
                return food;
        }

        return null;
    }

    public bool AreaHasPoop(CenterArea area)
    {
        if (area == null)
            return false;

        foreach (var poop in _poops)
        {
            if (!GodotObject.IsInstanceValid(poop))
                continue;

            if (GetAreaAtPosition(poop.GlobalPosition) == area)
                return true;
        }

        return false;
    }

    private Vector2 GetRandomAreaPosition()
    {
        var area = _centerAreas.Values.FirstOrDefault();

        if (area != null)
            return area.GetRandomPointInside();

        return _areaGridOrigin;
    }

    public EggWorld SpawnEgg(EggData egg)
    {
        if (_eggScene == null)
        {
            GD.PrintErr("EggWorld.tscn não foi carregado.");
            return null;
        }

        var world = _eggScene.Instantiate<EggWorld>();

        _digimonsContainer.AddChild(world);

        world.GlobalPosition = new Vector2(egg.PositionX, egg.PositionY);

        world.Initialize(egg);

        _eggs.Add(world);

        GD.Print($"Ovo posicionado em {world.GlobalPosition}.");

        return world;
    }

    private void RestoreEggs()
    {
        foreach (var egg in GameManager.Instance.Save.Center.Eggs)
        {
            if (egg.PositionX == 0 && egg.PositionY == 0)
            {
                Vector2 position = GetRandomAreaPosition();

                egg.PositionX = position.X;
                egg.PositionY = position.Y;
            }

            SpawnEgg(egg);
        }

        GD.Print($"Ovos restaurados: {_eggs.Count}");
    }

    private void OnEggPurchased(EggData egg)
    {
        Vector2 position = GetRandomAreaPosition();

        egg.PositionX = position.X;
        egg.PositionY = position.Y;

        SpawnEgg(egg);
    }

    private void OnEggHatched(EggData egg, DigimonInstance digimon)
    {
        var eggWorld = _eggs.FirstOrDefault(e => e.GetEgg() == egg);

        Vector2 position = GetRandomAreaPosition();

        if (eggWorld != null)
        {
            position = eggWorld.GlobalPosition;

            _eggs.Remove(eggWorld);

            eggWorld.QueueFree();
        }

        SpawnDigimon(digimon, position);
    }

}