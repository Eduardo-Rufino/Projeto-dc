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
    private PatchNotesScreen _patchNotesScreen;
    private DigimonDeathScreen _digimonDeathScreen;
    private EvolutionGuideScreen _evolutionGuideScreen;
    private BaseEditorScreen _baseEditorScreen;
    private SettingsScreen _settingsScreen;
    private ExplorationListScreen _explorationListScreen;
    private ExplorationDigimonPickScreen _explorationPickScreen;
    private EncyclopediaScreen _encyclopediaScreen;
    private DigimonDetailScreen _digimonDetailScreen;
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

        _patchNotesScreen = GetNode<PatchNotesScreen>("CanvasLayer/PatchNotesScreen");
        _patchNotesScreen.BackPressed += OnPatchNotesBackPressed;

        _digimonDeathScreen = GetNode<DigimonDeathScreen>("CanvasLayer/DigimonDeathScreen");

        _evolutionGuideScreen = GetNode<EvolutionGuideScreen>("CanvasLayer/EvolutionGuideScreen");
        _evolutionGuideScreen.BackPressed += OnEvolutionGuideBackPressed;

        _baseEditorScreen = GetNode<BaseEditorScreen>("CanvasLayer/BaseEditorScreen");
        _baseEditorScreen.BackPressed += OnBaseEditorBackPressed;

        _settingsScreen = GetNode<SettingsScreen>("CanvasLayer/SettingsScreen");
        _settingsScreen.BackPressed += OnSettingsBackPressed;

        _explorationListScreen = GetNode<ExplorationListScreen>("CanvasLayer/ExplorationListScreen");
        _explorationListScreen.BackPressed += OnExplorationListBackPressed;
        _explorationListScreen.MapSelected += OnExplorationMapSelected;

        _explorationPickScreen = GetNode<ExplorationDigimonPickScreen>("CanvasLayer/ExplorationDigimonPickScreen");
        _explorationPickScreen.BackPressed += OnExplorationPickBackPressed;

        _encyclopediaScreen = GetNode<EncyclopediaScreen>("CanvasLayer/EncyclopediaScreen");
        _encyclopediaScreen.BackPressed += OnEncyclopediaBackPressed;

        _digimonDetailScreen = GetNode<DigimonDetailScreen>("CanvasLayer/DigimonDetailScreen");
        _digimonDetailScreen.BackPressed += OnDigimonDetailBackPressed;

        GameManager.Instance.EvolutionBlockedByCapacity -= OnEvolutionBlockedByCapacity;
        GameManager.Instance.EvolutionBlockedByCapacity += OnEvolutionBlockedByCapacity;

        GameManager.Instance.DigimonDeleted -= OnDigimonDeleted;
        GameManager.Instance.DigimonDeleted += OnDigimonDeleted;

        GameManager.Instance.DigimonDied -= OnDigimonDied;
        GameManager.Instance.DigimonDied += OnDigimonDied;

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

        GameManager.Instance.ExplorationStarted -= OnExplorationStarted;
        GameManager.Instance.ExplorationStarted += OnExplorationStarted;

        GameManager.Instance.ExplorationFinished -= OnExplorationFinished;
        GameManager.Instance.ExplorationFinished += OnExplorationFinished;

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

        // Hook de teste: abre uma tela específica assim que o Center termina de carregar,
        // via variável de ambiente DEBUG_OPEN_SCREEN - não tem efeito nenhum se ela não
        // estiver setada (não usado no jogo normal). Existe porque não há automação de
        // clique disponível pra essa engine (ver .claude/skills/run-projeto-dc/SKILL.md),
        // então é assim que uma sessão de agente consegue abrir uma tela específica pra
        // tirar screenshot sem interação manual.
        string debugScreen = OS.GetEnvironment("DEBUG_OPEN_SCREEN");
        if (!string.IsNullOrEmpty(debugScreen))
            CallDeferred(nameof(DebugOpenScreen), debugScreen);

        // Mesmo esquema acima, mas pra rodar a bateria de verificação manual do Sistema de
        // Passivas (ver PassiveSystemDebugTests) via log em vez de abrir uma tela - variável
        // de ambiente separada porque não tem UI nenhuma envolvida.
        if (!string.IsNullOrEmpty(OS.GetEnvironment("DEBUG_TEST_PASSIVES")))
            ProjetoDC.Scripts.Systems.Passives.PassiveSystemDebugTests.Run();

        // Mesmo esquema acima: reproduz ao vivo o impasse relatado de Suporte vs Suporte
        // (ver GameManager.DebugStartSupportDeadlockBattle/BattleUnit._supportFallbackAttack) -
        // um duelo 1x1 entre dois Suportes do mesmo SupportType não tinha como terminar antes
        // desse fix, já que nenhum dos dois causava dano.
        string debugBattle = OS.GetEnvironment("DEBUG_TEST_BATTLE");

        if (!string.IsNullOrEmpty(debugBattle) && debugBattle.StartsWith("support_deadlock_"))
        {
            _debugSupportDeadlockType = debugBattle.Substring("support_deadlock_".Length);
            CallDeferred(nameof(DebugStartSupportDeadlockBattle));
        }

    }

    private string _debugSupportDeadlockType;

    private void DebugStartSupportDeadlockBattle()
    {
        var supportType = _debugSupportDeadlockType switch
        {
            "healer" => ProjetoDC.Enums.SupportType.Healer,
            "buffer" => ProjetoDC.Enums.SupportType.Buffer,
            "debuffer" => ProjetoDC.Enums.SupportType.Debuffer,
            _ => ProjetoDC.Enums.SupportType.Buffer,
        };

        GameManager.Instance.DebugStartSupportDeadlockBattle(supportType);
    }

    /// <summary>Ver o comentário sobre DEBUG_OPEN_SCREEN em _Ready() acima. Cada case usa
    /// dados de exemplo (primeiro Digimon do roster, primeiro mapa cadastrado, etc.) só pra
    /// ter algo pra mostrar - não representa nenhum estado real de jogo.</summary>
    private void DebugOpenScreen(string name)
    {
        switch (name)
        {
            case "encyclopedia":
                OpenEncyclopedia();
                break;
            case "inventory":
                OpenInventory();
                break;
            case "team":
                _teamSelectionScreen.Open();
                _teamSelectionScreen.Visible = true;
                break;
            case "evolutioncapacity":
                var digimon = GameManager.Instance.Save.Center.Digimons.FirstOrDefault();
                var targetForm = DatabaseManager.Instance.GetAllDigimons().FirstOrDefault();
                if (digimon != null && targetForm != null)
                    _evolutionCapacityScreen.Open(digimon, targetForm, 5);
                break;
            case "evolutionguide":
                _evolutionGuideScreen.Open();
                break;
            case "encyclopediasets":
                OpenEncyclopedia();
                _encyclopediaScreen.DebugShowSetsTab();
                break;
            case "shop":
                OpenShop();
                break;
            case "shopeggtypes":
                _shopScreen.Visible = true;
                _shopScreen.DebugOpenEggTypePopup();
                break;
            case "digimondeath":
                _digimonDeathScreen.Notify(
                    "Agumon morreu de velhice, sem ter evoluído a tempo. (mensagem de teste)"
                );
                break;
            case "digimondetail":
                var detailTarget = GameManager.Instance.Save.Center.Digimons.FirstOrDefault();
                if (detailTarget != null)
                    OpenDigimonDetail(detailTarget);
                break;
            case "explorationpick":
                var map = DatabaseManager.Instance.GetAllExplorationMaps().FirstOrDefault();
                if (map != null)
                {
                    _explorationPickScreen.Open(map);
                    _explorationPickScreen.Visible = true;
                }
                break;
            case "explorationarea":
                var explorationMap = DatabaseManager.Instance.GetAllExplorationMaps().FirstOrDefault();
                var explorer = GameManager.Instance.Save.Center.Digimons.FirstOrDefault();
                if (explorationMap != null && explorer != null)
                    GameManager.Instance.StartExploration(explorer, explorationMap);
                break;
            case "tutorial":
                _tutorialScreen.Open();
                _tutorialScreen.Visible = true;
                break;
            case "patchnotes":
                _patchNotesScreen.Open();
                _patchNotesScreen.Visible = true;
                break;
            case "battle":
                // Duelo/3x3 livre com o time real do save (não um cenário fabricado como os
                // outros cases) - existe pra exercitar o pipeline de dano/crítico/passivas/
                // bônus de conjunto (ver DamageCalculator) de ponta a ponta num teste
                // automatizado, sem precisar simular clique. A recompensa (XP/Bits) só é
                // aplicada quando o jogador clica "OK" na tela de resultado (ver
                // BattleArena.OnResultOkPressed) - fechar o jogo antes disso não persiste
                // XP/Bits (o dano recebido em combate, esse sim, já é aplicado ao vivo).
                var battleTeam = GameManager.Instance.Save.Center.Digimons.Take(3).ToList();
                if (battleTeam.Count > 0)
                    GameManager.Instance.StartTeamBattle(battleTeam);
                break;
        }
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
            GameManager.Instance.ExplorationStarted -= OnExplorationStarted;
            GameManager.Instance.ExplorationFinished -= OnExplorationFinished;
            GameManager.Instance.EvolutionBlockedByCapacity -= OnEvolutionBlockedByCapacity;
            GameManager.Instance.DigimonDeleted -= OnDigimonDeleted;
            GameManager.Instance.DigimonDied -= OnDigimonDied;
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

        _digimonWorlds.Add(instance);

        return instance;
    }

    private void SpawnFood()
    {
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

    public FoodWorld GetNearestFood(Vector2 position, CenterArea area, FoodType type = FoodType.Meat)
    {
        FoodWorld nearestFood = null;
        float nearestDistance = float.MaxValue;

        if (area == null)
            return null;

        foreach (var food in _foods)
        {
            if (!food.HasFood())
                continue;

            if (food.GetFood().Type != type)
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

    /// <summary>Primeira área construída desse tipo, ou null se não houver nenhuma - usado
    /// por DigimonWorld.TryStartAutoTraining pra achar a área de treino específica que um
    /// NPC recrutado bonifica (ver GameManager.HasRecruitedTrainingBonus). Se o jogador
    /// construiu mais de uma área do mesmo tipo, só a primeira encontrada vale de destino
    /// autônomo - não distribui entre elas.</summary>
    public CenterArea GetAreaOfType(CenterAreaType areaType)
    {
        foreach (var area in _centerAreas.Values)
        {
            if (area.AreaType == areaType)
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

    /// <summary>Ponto de entrada do botão de globo (🌎, ver HUD.OnExplorationButtonPressed) -
    /// lista as áreas de exploração cadastradas.</summary>
    public void OpenExplorationList()
    {
        _explorationListScreen.RefreshUI();
        _explorationListScreen.Visible = true;
    }

    private void OnExplorationListBackPressed()
    {
        _explorationListScreen.Visible = false;
    }

    private void OnExplorationMapSelected(ExplorationMapData map)
    {
        _explorationListScreen.Visible = false;

        _explorationPickScreen.Open(map);
        _explorationPickScreen.Visible = true;
    }

    private void OnExplorationPickBackPressed()
    {
        _explorationPickScreen.Visible = false;

        _explorationListScreen.Visible = true;
    }

    private void OnExplorationStarted()
    {
        // Mesma lógica de esconder o Center durante uma batalha (ver OnTeamBattleStarted) -
        // a área de exploração é uma cena separada por cima, então o Center continua
        // existindo/simulando (pausado, ver GameManager.StartExploration), só não deve
        // aparecer nem ser clicável.
        _explorationPickScreen.Visible = false;

        Visible = false;
        _canvasLayer.Visible = false;
    }

    private void OnExplorationFinished()
    {
        Visible = true;
        _canvasLayer.Visible = true;

        _camera.MakeCurrent();
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

    public void OpenPatchNotes()
    {
        _patchNotesScreen.Open();
    }

    private void OnPatchNotesBackPressed()
    {
        _patchNotesScreen.Visible = false;
    }

    public void OpenEvolutionGuide()
    {
        _evolutionGuideScreen.Open();
    }

    private void OnEvolutionGuideBackPressed()
    {
        _evolutionGuideScreen.Visible = false;
    }

    public void OpenEncyclopedia()
    {
        _encyclopediaScreen.Open();
    }

    private void OnEncyclopediaBackPressed()
    {
        _encyclopediaScreen.Visible = false;
    }

    /// <summary>Ponto de entrada do botão "ℹ" na barra de status do Digimon (ver
    /// HUD.OnDigimonInfoButtonPressed) - detalhes que não cabem na barra principal (Role,
    /// Atributo, Elemento, etc.) e a passiva (ver PASSIVAS_SPEC.md).</summary>
    public void OpenDigimonDetail(DigimonInstance digimon)
    {
        _digimonDetailScreen.Open(digimon);
    }

    private void OnDigimonDetailBackPressed()
    {
        _digimonDetailScreen.Visible = false;
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
        // Selvagem de exploração: o Center já está escondido desde que a área de exploração
        // abriu (ver OnExplorationMapSelected/GameManager.StartExploration) - quem cuida de
        // esconder/mostrar em volta dessa batalha é a própria ExplorationArea, não o Center.
        if (GameManager.Instance.BattleFromExploration)
            return;

        // Esconde o Center inteiro (mundo + HUD) durante o combate, igual à tela de
        // seleção de time - a arena de batalha é uma cena separada por cima, então o
        // Center continua existindo/simulando, só não deve aparecer nem ser clicável.
        Visible = false;
        _canvasLayer.Visible = false;
    }

    private void OnTeamBattleFinished(BattleResult result)
    {
        if (GameManager.Instance.BattleFromExploration)
            return;

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
            || (_patchNotesScreen?.Visible ?? false)
            || (_evolutionGuideScreen?.Visible ?? false)
            || (_baseEditorScreen?.Visible ?? false)
            || (_settingsScreen?.Visible ?? false)
            || (_explorationListScreen?.Visible ?? false)
            || (_explorationPickScreen?.Visible ?? false)
            || (_encyclopediaScreen?.Visible ?? false)
            || (_digimonDetailScreen?.Visible ?? false)
            || (_digimonDeathScreen?.Visible ?? false);
    }

    public void InspectDigimon(DigimonInstance digimon)
    {
        if (digimon == null)
            return;

        _hud.InspectDigimon(digimon);
    }

    private bool CanStartPlacingFood()
    {
        if (_isPlacingFood)
            return false;

        if (_foods.Count >= MaxFoods)
        {
            GD.Print("Limite de comidas atingido.");
            return false;
        }

        return true;
    }

    public void StartFoodPlacement()
    {
        if (!CanStartPlacingFood())
            return;

        // A Carne colocada aqui vem do inventário (Save.Center.Meat) - sem essa checagem,
        // dava pra colocar comida ilimitada mesmo com o estoque zerado, já que o consumo em
        // si só acontece em PlaceFood (na confirmação, igual StartMedicinePlacement).
        if (GameManager.Instance.Save.Center.Meat <= 0)
        {
            GD.Print("Você não possui Carne.");
            return;
        }

        BeginPlacingFood(new Food("Carne", 20, FoodType.Meat));
    }

    /// <summary>Mesmo esquema de StartFoodPlacement acima, só que pro Energético (ver
    /// GameManager.BuyStaminaSnack) - MaxNutrition igual a MaxStamina (100) de propósito, pra
    /// um Digimon sozinho comendo tudo recuperar a Stamina inteira (ver DigimonWorld.
    /// ProcessEating); se mais de um comer junto, a mesma mordida compartilhada de sempre
    /// (Food.Consume) naturalmente divide entre eles.</summary>
    public void StartStaminaSnackPlacement()
    {
        if (!CanStartPlacingFood())
            return;

        if (GameManager.Instance.Save.Center.StaminaSnacks <= 0)
        {
            GD.Print("Você não possui Energético.");
            return;
        }

        BeginPlacingFood(new Food("Energético", 100, FoodType.StaminaSnack));
    }

    private void BeginPlacingFood(Food food)
    {
        _placingFood = _foodScene.Instantiate<FoodWorld>();

        _digimonsContainer.AddChild(_placingFood);

        _placingFood.Initialize(food);

        _placingFood.SetPlacementMode(true);

        _isPlacingFood = true;
    }

    /// <summary>Confirma a colocação do que estiver em andamento (Carne ou Energético - ver
    /// StartFoodPlacement/StartStaminaSnackPlacement) - qual estoque desconta depende só do
    /// Food.Type já guardado no objeto, sem precisar saber qual dos dois Start disparou.</summary>
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
        // FoodWorld.OnHourPassed) - comida não se move depois de colocada. Sem efeito pro
        // Energético, que não estraga.
        food.PlacedInRestaurant = area.IsRestaurant();

        _foods.Add(_placingFood);

        GameManager.Instance.Save.Center.Foods.Add(food);

        // Só consome o item do inventário aqui, na confirmação bem-sucedida - se o jogador
        // cancelar soltando fora de uma área (acima), nada é gasto.
        if (food.Type == FoodType.StaminaSnack)
            GameManager.Instance.Save.Center.StaminaSnacks--;
        else
            GameManager.Instance.Save.Center.Meat--;

        _placingFood = null;
        _isPlacingFood = false;
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
    }

    private void StopBroomMode()
    {
        if (_placingBroom != null)
        {
            _placingBroom.QueueFree();
            _placingBroom = null;
        }

        _isPlacingBroom = false;
    }

    private void CleanAtPosition(Vector2 position)
    {
        PoopWorld poop = GetPoopAtPosition(position);

        if (poop != null)
        {
            RemovePoop(poop);

            return;
        }

        FoodWorld food = GetFoodAtPosition(position);

        if (food != null)
        {
            RemoveFood(food);
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

    // Igual a OnDigimonDeleted (o mesmo DigimonDeleted já dispara junto - ver
    // GameManager.KillDigimon) só que aqui é sobre avisar o jogador do motivo, não sobre
    // limpar o visual - a tela enfileira sozinha se mais de uma morte acontecer perto uma da
    // outra.
    private void OnDigimonDied(DigimonInstance digimon, string cause)
    {
        _digimonDeathScreen.Notify(cause);
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