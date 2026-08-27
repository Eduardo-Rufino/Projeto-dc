using Godot;
using ProjetoDC.Scripts.Gameplay;

namespace ProjetoDC.Scripts.World;

public partial class FoodWorld : Node2D
{
    private Food _food;


    public void Initialize(Food food)
    {
        _food = food;
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
}