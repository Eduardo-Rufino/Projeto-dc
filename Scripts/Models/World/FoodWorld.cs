using Godot;
using ProjetoDC.Scripts.Gameplay;
using ProjetoDC.Scripts.Managers;

namespace ProjetoDC.Scripts.World;

public partial class FoodWorld : Node2D
{
    // Fora do Refeitório, estraga rápido; dentro, demora bem mais (~1 dia de jogo) - ver
    // Center.PlaceFood, que decide qual das duas vale pra essa comida na hora de colocar.
    private const int SpoilHoursOutsideRestaurant = 6;
    private const int SpoilHoursInRestaurant = 24;

    private const string FreshTexturePath = "res://Assets/Sprites/Food/beef on bone raw.png";
    private const string SpoiledTexturePath = "res://Assets/Sprites/Food/beef_on_bone_spoiled_heavy.png";

    private Food _food;
    private Sprite2D _sprite;

    public override void _Ready()
    {
        _sprite = GetNode<Sprite2D>("Sprite2D");
    }

    public void Initialize(Food food)
    {
        _food = food;

        UpdateSpriteForSpoilState();

        GameManager.Instance.ClockSystem.HourPassed -= OnHourPassed;
        GameManager.Instance.ClockSystem.HourPassed += OnHourPassed;
    }

    public override void _ExitTree()
    {
        if (GameManager.Instance?.ClockSystem != null)
            GameManager.Instance.ClockSystem.HourPassed -= OnHourPassed;
    }

    public bool HasFood()
    {
        return _food != null && !_food.IsEmpty();
    }

    public Food GetFood()
    {
        return _food;
    }

    public void SetPlacementMode(bool placementMode)
    {
        Modulate = placementMode
            ? new Color(1f, 1f, 1f, 0.5f)
            : new Color(1f, 1f, 1f, 1f);
    }

    /// <summary>A cada hora de jogo, envelhece a comida até ela estragar (o limite depende
    /// de ter sido colocada no Refeitório ou não - ver as consts no topo) e troca o sprite
    /// pro de carne estragada. Comida já vazia/estragada não precisa mais contar.</summary>
    private void OnHourPassed()
    {
        if (_food == null || _food.IsEmpty() || _food.IsSpoiled)
            return;

        _food.HoursSincePlaced++;

        int spoilThreshold = _food.PlacedInRestaurant
            ? SpoilHoursInRestaurant
            : SpoilHoursOutsideRestaurant;

        if (_food.HoursSincePlaced < spoilThreshold)
            return;

        _food.IsSpoiled = true;

        UpdateSpriteForSpoilState();

        GD.Print($"Comida em {GlobalPosition} estragou.");
    }

    private void UpdateSpriteForSpoilState()
    {
        if (_sprite == null || _food == null)
            return;

        string path = _food.IsSpoiled ? SpoiledTexturePath : FreshTexturePath;

        if (ResourceLoader.Exists(path))
            _sprite.Texture = GD.Load<Texture2D>(path);
    }
}
