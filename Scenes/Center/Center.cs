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
using System.Threading.Tasks;

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

    private CenterAreaType? _pendingPurchasedAreaType;

    // True quando o posicionamento em andamento veio do inventário de bases (BaseEditorScreen)
    // em vez de uma compra nova na loja - ver StartAreaPlacementFromInventory/EndAreaPlacement.
    private bool _pendingPlacementFromInventory;

    private List<CenterExpansionSlot> _expansionSlots = new();

    private Node2D _digimonsContainer;
    private Node2D _expansionSlotsVisual;

    private Camera2D _camera;
    private bool _isPanningCamera;

    private const float MinCameraZoom = 0.35f;
    private const float MaxCameraZoom = 1.5f;
    private const float CameraZoomStep = 0.1f;

    private HUD _hud;
    private ShopScreen _shopScreen;
    private TeamSelectionScreen _teamSelectionScreen;
    private BattleTypeScreen _battleTypeScreen;
    private TournamentListScreen _tournamentListScreen;

    // Pra qual tela o botão Voltar da TeamSelectionScreen deve retornar - varia conforme
    // ela foi aberta pela Batalha Livre (BattleTypeScreen) ou por um campeonato específico
    // (TournamentListScreen).
    private Control _teamSelectionReturnScreen;
    private EvolutionCapacityScreen _evolutionCapacityScreen;
    private EvolutionOverlay _evolutionOverlay;
    private InventoryScreen _inventoryScreen;
    private TutorialScreen _tutorialScreen;
    private EvolutionGuideScreen _evolutionGuideScreen;
    private BaseEditorScreen _baseEditorScreen;
    private SettingsScreen _settingsScreen;
    private CanvasLayer _canvasLayer;

    // Garante só uma animação de evolução por vez: se vários Digimons evoluírem juntos
    // (ex.: todo o time depois de uma vitória em batalha), cada um entra nessa fila e joga
    // sua animação em sequência, não ao mesmo tempo.
    private readonly Queue<(DigimonData oldForm, DigimonData newForm, DigimonWorld world)> _evolutionQueue = new();
    private bool _isProcessingEvolutionQueue;

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

    private PackedScene _expansionSlotScene =
    GD.Load<PackedScene>("res://Scenes/Center/CenterExpansionSlotVisual.tscn");

    public override void _Ready()
    {
        PrintTree();

        MusicManager.Instance?.PlayCenterMusic();

        _camera = GetNode<Camera2D>("Camera2D");

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

        _hud = GetNode<HUD>("CanvasLayer/HUD");

        _shopScreen = GetNode<ShopScreen>("CanvasLayer/ShopScreen");

        _shopScreen.BackPressed += OnShopBackPressed;
        _shopScreen.EggPurchased += OnEggPurchased;
        _shopScreen.AreaPurchased += OnAreaPurchased;

        _teamSelectionScreen = GetNode<TeamSelectionScreen>("CanvasLayer/TeamSelectionScreen");

        _teamSelectionScreen.BackPressed += OnTeamSelectionBackPressed;

        _battleTypeScreen = GetNode<BattleTypeScreen>("CanvasLayer/BattleTypeScreen");

        _battleTypeScreen.FreeBattleSelected += OnFreeBattleSelected;
        _battleTypeScreen.TournamentSelected += OnTournamentMenuSelected;
        _battleTypeScreen.BackPressed += OnBattleTypeBackPressed;

        _tournamentListScreen = GetNode<TournamentListScreen>("CanvasLayer/TournamentListScreen");

        _tournamentListScreen.BackPressed += OnTournamentListBackPressed;
        _tournamentListScreen.TournamentSelected += OnTournamentSelected;

        _evolutionCapacityScreen = GetNode<EvolutionCapacityScreen>("CanvasLayer/EvolutionCapacityScreen");
        _evolutionOverlay = GetNode<EvolutionOverlay>("CanvasLayer/EvolutionOverlay");

        _inventoryScreen = GetNode<InventoryScreen>("CanvasLayer/InventoryScreen");
        _inventoryScreen.BackPressed += OnInventoryBackPressed;

        _tutorialScreen = GetNode<TutorialScreen>("CanvasLayer/TutorialScreen");
        _tutorialScreen.BackPressed += OnTutorialBackPressed;

        _evolutionGuideScreen = GetNode<EvolutionGuideScreen>("CanvasLayer/EvolutionGuideScreen");
        _evolutionGuideScreen.BackPressed += OnEvolutionGuideBackPressed;

        _baseEditorScreen = GetNode<BaseEditorScreen>("CanvasLayer/BaseEditorScreen");
        _baseEditorScreen.BackPressed += OnBaseEditorBackPressed;

        _settingsScreen = GetNode<SettingsScreen>("CanvasLayer/SettingsScreen");
        _settingsScreen.BackPressed += OnSettingsBackPressed;

        GameManager.Instance.EvolutionBlockedByCapacity -= OnEvolutionBlockedByCapacity;
        GameManager.Instance.EvolutionBlockedByCapacity += OnEvolutionBlockedByCapacity;

        GameManager.Instance.DigimonDeleted -= OnDigimonDeleted;
        GameManager.Instance.DigimonDeleted += OnDigimonDeleted;

        GameManager.Instance.SleepSkipped -= OnSleepSkipped;
        GameManager.Instance.SleepSkipped += OnSleepSkipped;

        GameManager.Instance.EggSystem.EggHatched -= OnEggHatched;
        GameManager.Instance.EggSystem.EggHatched += OnEggHatched;

        GameManager.Instance.EggSystem.EggCreated -= OnEggCreated;
        GameManager.Instance.EggSystem.EggCreated += OnEggCreated;

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
    }

    public override void _ExitTree()
    {
        if (GameManager.Instance?.EggSystem != null)
        {
            GameManager.Instance.EggSystem.EggHatched -= OnEggHatched;
            GameManager.Instance.EggSystem.EggCreated -= OnEggCreated;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.TeamBattleFinished -= OnTeamBattleFinished;
            GameManager.Instance.TeamBattleStarted -= OnTeamBattleStarted;
            GameManager.Instance.EvolutionBlockedByCapacity -= OnEvolutionBlockedByCapacity;
            GameManager.Instance.DigimonDeleted -= OnDigimonDeleted;
            GameManager.Instance.SleepSkipped -= OnSleepSkipped;
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

        int nextSpawnPointIndex = 0;

        foreach (var digimon in digimons)
        {
            // (0,0) é o default de um DigimonInstance que nunca teve a posição salva ainda
            // (ex.: acabou de nascer de um ovo antes de DigimonWorld._Process rodar uma vez
            // - mesma convenção já usada em EggData.PositionX/Y) - só nesse caso usa um
            // SpawnPoint fixo; do contrário, volta pra onde o jogador deixou.
            bool hasSavedPosition = digimon.PositionX != 0f || digimon.PositionY != 0f;

            if (hasSavedPosition)
            {
                SpawnDigimon(digimon, new Vector2(digimon.PositionX, digimon.PositionY));
                continue;
            }

            if (nextSpawnPointIndex >= _spawnPoints.Count)
            {
                GD.PrintErr("Não há SpawnPoints suficientes.");
                break;
            }

            SpawnDigimon(digimon, _spawnPoints[nextSpawnPointIndex]);
            nextSpawnPointIndex++;
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

    /// <summary>Todas as áreas do Center atualmente registradas (posição no grid + tipo) -
    /// usado pelo minimapa (ver Scripts/UI/MiniMap.cs) pra desenhar um esquema simplificado
    /// sem precisar de uma segunda câmera renderizando o mundo de verdade.</summary>
    public IEnumerable<CenterArea> GetAreas() => _centerAreas.Values;

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

    // Só é não-nulo durante o modo de posicionamento de área (ver StartAreaPlacement) - os
    // hexágonos de expansão só existem/são clicáveis nesse modo, então um clique de slot só
    // chega aqui quando já sabemos pra qual tipo de área ele é.
    private void OnExpansionSlotClicked(Vector2I gridPosition)
    {
        if (_pendingPurchasedAreaType == null)
            return;

        GD.Print($"SLOT CLICADO! Grid: {gridPosition} | Tipo: {_pendingPurchasedAreaType}");

        if (AddArea(gridPosition, _pendingPurchasedAreaType.Value))
        {
            // Colocada a partir do inventário (não uma compra nova) - remove só 1 unidade
            // desse tipo do inventário, já que AddArea acabou de recriar ela no grid.
            if (_pendingPlacementFromInventory)
                GameManager.Instance.Save.Center.UnplacedAreas.Remove(_pendingPurchasedAreaType.Value);

            EndAreaPlacement();
        }
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

    /// <summary>Áreas de treino específicas de um stat (ver CenterArea.ForcedTrainingType) só
    /// admitem 1 Digimon treinando por vez - diferente da área de treino genérica, que aceita
    /// quantos couberem. Usado por DigimonWorld.StopDragging pra recusar um segundo Digimon
    /// largado na mesma área específica.</summary>
    public bool IsSpecificTrainingAreaOccupied(CenterArea area, DigimonWorld excluding)
    {
        if (area == null || !area.ForcedTrainingType.HasValue)
            return false;

        return _digimonWorlds.Any(w => w != excluding && w.CurrentArea == area);
    }

    /// <summary>Aviso temporário no topo da tela (ver HUD.ShowWarning) - usado quando uma ação
    /// do jogador no mundo do Center (ex.: arrastar um Digimon) precisa ser recusada.</summary>
    public void ShowWarning(string message)
    {
        _hud?.ShowWarning(message);
    }

    private void OnAreaPurchased(CenterAreaType areaType)
    {
        _shopScreen.Visible = false;

        StartAreaPlacement(areaType, fromInventory: false);
    }

    /// <summary>Bases (áreas dinamicamente construídas, ver Save.Center.BuiltAreas) atualmente
    /// no grid - exclui a área Neutra fixa em (0,0) de Center.tscn, que nunca entra em
    /// BuiltAreas e não pode ser removida. Usado por BaseEditorScreen pra listar o que dá pra
    /// remover.</summary>
    public List<CenterArea> GetRemovableAreas()
    {
        var built = GameManager.Instance.Save.Center.BuiltAreas;

        return _centerAreas.Values
            .Where(a => built.Any(b => b.GridX == a.GridPosition.X && b.GridY == a.GridPosition.Y))
            .OrderBy(a => a.GridPosition.X)
            .ThenBy(a => a.GridPosition.Y)
            .ToList();
    }

    /// <summary>
    /// Remove uma base do grid de volta pro inventário (Save.Center.UnplacedAreas) - o tipo
    /// não é perdido, só deixa de ocupar um hexágono, e pode ser colocado de novo (ver
    /// StartAreaPlacementFromInventory) sem custo, já que já foi pago na compra original.
    /// Recusa a área Neutra fixa (nunca está em BuiltAreas) e qualquer área com um Digimon
    /// em cima agora (removeria o node debaixo dele, deixando uma referência inválida em
    /// DigimonWorld._currentArea).
    /// </summary>
    public SystemResult RemoveArea(CenterArea area)
    {
        if (area == null)
            return SystemResult.Fail("Área inválida.");

        var savedData = GameManager.Instance.Save.Center.BuiltAreas
            .FirstOrDefault(a => a.GridX == area.GridPosition.X && a.GridY == area.GridPosition.Y);

        if (savedData == null)
            return SystemResult.Fail("Essa área não pode ser removida.");

        if (_digimonWorlds.Any(w => area.IsPointInside(w.GlobalPosition)))
            return SystemResult.Fail("Não é possível remover uma área com um Digimon nela.");

        GameManager.Instance.Save.Center.BuiltAreas.Remove(savedData);
        GameManager.Instance.Save.Center.UnplacedAreas.Add(area.AreaType);

        _centerAreas.Remove(area.GridPosition);
        area.QueueFree();

        return SystemResult.Ok("Base removida - agora está no inventário.");
    }

    /// <summary>Recoloca uma base do inventário (ver Save.Center.UnplacedAreas) no grid - mesmo
    /// modo de posicionamento da compra na loja, só que sem cobrar Bits (a diferença é resolvida
    /// em EndAreaPlacement, que também reabre o BaseEditorScreen ao concluir).</summary>
    public void StartAreaPlacementFromInventory(CenterAreaType areaType)
    {
        StartAreaPlacement(areaType, fromInventory: true);
    }

    /// <summary>
    /// Modo de posicionamento de uma área (recém-comprada na loja ou recolocada do
    /// inventário de bases): some com Digimons/comida/coco/ovos (só as áreas ficam visíveis)
    /// e destaca em quais hexágonos livres ao redor da base atual dá pra colocar a área nova -
    /// clicar em um deles conclui (ver OnExpansionSlotClicked). Substitui o antigo fluxo
    /// (clicar num hexágono sempre visível e escolher o tipo ali na hora, de graça).
    /// </summary>
    private void StartAreaPlacement(CenterAreaType areaType, bool fromInventory)
    {
        _pendingPurchasedAreaType = areaType;
        _pendingPlacementFromInventory = fromInventory;

        SetWorldElementsVisible(false);

        RefreshExpansionSlotVisuals();
    }

    private void EndAreaPlacement()
    {
        _pendingPurchasedAreaType = null;

        if (_expansionSlotsVisual != null)
        {
            _expansionSlotsVisual.Free();
            _expansionSlotsVisual = null;
        }

        SetWorldElementsVisible(true);

        // Colocar do inventário sempre parte do BaseEditorScreen (ver OnPlacePressed, que o
        // esconde antes de entrar no modo de posicionamento) - reabre ele em seguida, já
        // atualizado, pra continuar gerenciando outras bases sem precisar reabrir manualmente.
        if (_pendingPlacementFromInventory)
        {
            _pendingPlacementFromInventory = false;
            OpenBaseEditor();
        }
    }

    public void OpenBaseEditor()
    {
        _baseEditorScreen.Open(this);
    }

    private void OnBaseEditorBackPressed()
    {
        _baseEditorScreen.Visible = false;
    }

    public void OpenSettings()
    {
        _settingsScreen.Open();
    }

    private void OnSettingsBackPressed()
    {
        _settingsScreen.Visible = false;
    }

    private void SetWorldElementsVisible(bool visible)
    {
        foreach (var digimon in _digimonWorlds)
            digimon.Visible = visible;

        foreach (var food in _foods)
            food.Visible = visible;

        foreach (var poop in _poops)
            poop.Visible = visible;

        foreach (var egg in _eggs)
            egg.Visible = visible;
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

    public void OpenInventory()
    {
        _inventoryScreen.RefreshUI();

        _inventoryScreen.Visible = true;
    }

    private void OnInventoryBackPressed()
    {
        _inventoryScreen.Visible = false;
    }

    public void OpenTutorial()
    {
        _tutorialScreen.Open();
    }

    private void OnTutorialBackPressed()
    {
        _tutorialScreen.Visible = false;
    }

    public void OpenEvolutionGuide()
    {
        _evolutionGuideScreen.Open();
    }

    private void OnEvolutionGuideBackPressed()
    {
        _evolutionGuideScreen.Visible = false;
    }

    /// <summary>Ponto de entrada do botão de troféu - abre a escolha entre Campeonato e
    /// Batalha Livre, em vez de ir direto pra montagem de time.</summary>
    public void OpenBattleTypeMenu()
    {
        _battleTypeScreen.Visible = true;
    }

    private void OnBattleTypeBackPressed()
    {
        _battleTypeScreen.Visible = false;
    }

    private void OnFreeBattleSelected()
    {
        _battleTypeScreen.Visible = false;

        _teamSelectionReturnScreen = _battleTypeScreen;

        _teamSelectionScreen.Open();

        _teamSelectionScreen.Visible = true;
    }

    private void OnTournamentMenuSelected()
    {
        _battleTypeScreen.Visible = false;

        _tournamentListScreen.RefreshUI();

        _tournamentListScreen.Visible = true;
    }

    private void OnTournamentListBackPressed()
    {
        _tournamentListScreen.Visible = false;

        _battleTypeScreen.Visible = true;
    }

    private void OnTournamentSelected(TournamentData tournament)
    {
        _tournamentListScreen.Visible = false;

        _teamSelectionReturnScreen = _tournamentListScreen;

        _teamSelectionScreen.Open(tournament);

        _teamSelectionScreen.Visible = true;
    }

    private void OnTeamSelectionBackPressed()
    {
        _teamSelectionScreen.Visible = false;

        if (_teamSelectionReturnScreen != null)
        {
            _teamSelectionReturnScreen.Visible = true;
            _teamSelectionReturnScreen = null;
        }
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
        _teamSelectionReturnScreen = null;

        // A arena de batalha usa sua própria Camera2D (precisa pra enquadrar a luta
        // certo, independente de onde a câmera do Center estava olhando); ao encerrar
        // a arena, garantimos que a câmera do Center volte a ser a atual.
        _camera.MakeCurrent();
    }

    /// <summary>
    /// Zoom (scroll do mouse) e pan (arrastar com o botão do meio) livres da câmera do
    /// Center - o botão do meio não é usado em mais nada no jogo, então não conflita com
    /// arrastar Digimon (botão esquerdo) ou inspecionar (botão direito). Serve principalmente
    /// pra dar acesso visual a partes do Center que ficam atrás de UI fixa na tela (como a
    /// barra superior), que não acompanha a câmera.
    /// </summary>
    public override void _UnhandledInput(InputEvent @event)
    {
        if (IsBlockingScreenOpen())
            return;

        if (@event is InputEventMouseButton mouseButton)
        {
            switch (mouseButton.ButtonIndex)
            {
                case MouseButton.Middle:
                    _isPanningCamera = mouseButton.Pressed;
                    break;

                case MouseButton.WheelUp when mouseButton.Pressed:
                    ZoomCamera(CameraZoomStep);
                    break;

                case MouseButton.WheelDown when mouseButton.Pressed:
                    ZoomCamera(-CameraZoomStep);
                    break;

                // Modo vassoura ligado (ver ToggleBroomMode): cada clique esquerdo no Center
                // limpa cocô/comida ali, sem desligar o modo - dá pra limpar vários seguidos.
                case MouseButton.Left when mouseButton.Pressed && _isPlacingBroom:
                    CleanAtPosition(GetGlobalMousePosition());
                    break;
            }

            return;
        }

        if (@event is InputEventMouseMotion mouseMotion && _isPanningCamera)
        {
            _camera.Position -= mouseMotion.Relative / _camera.Zoom;
        }
    }

    private void ZoomCamera(float delta)
    {
        float newZoom = Mathf.Clamp(_camera.Zoom.X + delta, MinCameraZoom, MaxCameraZoom);

        _camera.Zoom = new Vector2(newZoom, newZoom);
    }

    /// <summary>Verdadeiro enquanto qualquer tela secundária (loja, seleção de time,
    /// inventário, tutorial, etc.) está aberta por cima do Center - usado tanto pra bloquear
    /// pan/zoom da câmera quanto pra pausar o resto do jogo (ver HUD._Process, que sincroniza
    /// GetTree().Paused com isso a cada frame). Diálogos de confirmação (ConfirmationDialog)
    /// não entram aqui de propósito - só telas cheias pausam o jogo atrás.</summary>
    public bool IsBlockingScreenOpen()
    {
        return (_shopScreen?.Visible ?? false)
            || (_teamSelectionScreen?.Visible ?? false)
            || (_evolutionCapacityScreen?.Visible ?? false)
            || (_inventoryScreen?.Visible ?? false)
            || (_battleTypeScreen?.Visible ?? false)
            || (_tournamentListScreen?.Visible ?? false)
            || (_tutorialScreen?.Visible ?? false)
            || (_evolutionGuideScreen?.Visible ?? false)
            || (_baseEditorScreen?.Visible ?? false)
            || (_settingsScreen?.Visible ?? false);
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

        // A Carne colocada aqui vem do inventário (Save.Center.Meat) - sem essa checagem,
        // dava pra colocar comida ilimitada mesmo com o estoque zerado, já que o consumo em
        // si só acontece em PlaceFood (na confirmação, igual StartMedicinePlacement).
        if (GameManager.Instance.Save.Center.Meat <= 0)
        {
            GD.Print("Você não possui Carne.");
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

        // Decide uma vez, aqui, se ela vai estragar rápido ou devagar (ver
        // FoodWorld.OnHourPassed) - comida não se move depois de colocada.
        food.PlacedInRestaurant = area.IsRestaurant();

        _foods.Add(_placingFood);

        GameManager.Instance.Save.Center.Foods.Add(food);

        // Só consome a Carne do inventário aqui, na confirmação bem-sucedida - se o
        // jogador cancelar soltando fora de uma área (acima), nada é gasto.
        GameManager.Instance.Save.Center.Meat--;

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

    /// <summary>
    /// Botão da vassoura funciona como um modo (liga/desliga), não como segurar-e-soltar:
    /// clicar liga o modo (a vassoura passa a seguir o mouse), clicar em qualquer lugar do
    /// Center limpa cocô/comida ali (sem desligar o modo, pra dar pra limpar vários seguidos),
    /// e clicar no botão de novo desliga.
    /// </summary>
    public void ToggleBroomMode()
    {
        if (_isPlacingBroom)
            StopBroomMode();
        else
            StartBroomMode();
    }

    private void StartBroomMode()
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

        GD.Print("Modo de limpeza ativado.");
    }

    private void StopBroomMode()
    {
        if (_placingBroom != null)
        {
            _placingBroom.QueueFree();
            _placingBroom = null;
        }

        _isPlacingBroom = false;

        GD.Print("Modo de limpeza desativado.");
    }

    private void CleanAtPosition(Vector2 position)
    {
        PoopWorld poop = GetPoopAtPosition(position);

        if (poop != null)
        {
            RemovePoop(poop);

            GD.Print("Coco limpo.");

            return;
        }

        FoodWorld food = GetFoodAtPosition(position);

        if (food != null)
        {
            RemoveFood(food);

            GD.Print("Carne removida com a vassoura.");
        }
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

    // O ovo inicial (EggSystem.CreateInitialEgg) não passa pelo fluxo de compra da loja -
    // sem isso, ele só ganhava um EggWorld visual quando a cena recarregava e RestoreEggs()
    // reconstruía a partir do save, ficando invisível até o jogo ser fechado e reaberto.
    private void OnEggCreated(EggData egg)
    {
        if (_eggs.Any(e => e.GetEgg() == egg))
            return;

        Vector2 position = GetRandomAreaPosition();

        egg.PositionX = position.X;
        egg.PositionY = position.Y;

        SpawnEgg(egg);
    }

    // Se já tem um aviso aberto (pra esse Digimon ou outro), ignora - a checagem de
    // evolução roda de novo em breve (treino/dia/batalha) e reabre o aviso pra quem
    // ainda estiver bloqueado nessa hora, sem empilhar telas por cima uma da outra.
    private void OnEvolutionBlockedByCapacity(DigimonInstance digimon, DigimonData targetForm, int deficit)
    {
        if (_evolutionCapacityScreen.Visible)
            return;

        _evolutionCapacityScreen.Open(digimon, targetForm, deficit);
    }

    /// <summary>
    /// Enfileira a animação de evolução de um Digimon (chamado por DigimonWorld.OnEvolved).
    /// Só uma toca por vez - se vários Digimons evoluírem no mesmo instante (ex.: o time
    /// inteiro depois de uma vitória em batalha), cada um espera sua vez em vez de tocar
    /// junto. O resto do jogo fica pausado enquanto a fila não esvaziar.
    /// </summary>
    public void EnqueueEvolution(DigimonData oldForm, DigimonData newForm, DigimonWorld world)
    {
        _evolutionQueue.Enqueue((oldForm, newForm, world));

        if (!_isProcessingEvolutionQueue)
            _ = ProcessEvolutionQueue();
    }

    private async Task ProcessEvolutionQueue()
    {
        _isProcessingEvolutionQueue = true;

        GameManager.Instance.RequestPause("evolution");

        while (_evolutionQueue.Count > 0)
        {
            var (oldForm, newForm, world) = _evolutionQueue.Dequeue();

            await _evolutionOverlay.Play(oldForm, newForm);

            // O Digimon (ou até a própria cena) pode ter deixado de existir enquanto a
            // animação tocava - ex.: deletado na tela de capacidade de outra evolução que
            // furou a fila entre uma animação e outra.
            if (GodotObject.IsInstanceValid(world))
                world.ApplyEvolvedSprite(newForm);
        }

        GameManager.Instance.ReleasePause("evolution");

        _isProcessingEvolutionQueue = false;
    }

    // O modelo (CenterService.RemoveDigimon, disparado via GameManager.DeleteDigimon) já
    // tirou o Digimon do save - sem isso, o DigimonWorld visual continuaria existindo e
    // andando por aí, referenciando um Digimon que não existe mais no Center.
    private void OnDigimonDeleted(DigimonInstance digimon)
    {
        var digimonWorld = _digimonWorlds.FirstOrDefault(w => w._digimon == digimon);

        if (digimonWorld == null)
            return;

        _digimonWorlds.Remove(digimonWorld);

        digimonWorld.QueueFree();
    }

    // GameManager.SkipSleep avança o relógio sem tempo real de verdade passar - os timers
    // de _Process de cada DigimonWorld (regeneração de HP, área suja) não tickam sozinhos
    // nesse meio tempo, então precisam desse empurrão explícito pra não "perder" o que
    // teriam acumulado esperando de verdade.
    private void OnSleepSkipped(double secondsSkipped)
    {
        foreach (var digimonWorld in _digimonWorlds)
            digimonWorld.CatchUpPassiveTime(secondsSkipped);
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