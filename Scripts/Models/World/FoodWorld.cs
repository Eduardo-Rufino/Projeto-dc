using Godot;
using ProjetoDC.Scripts.Gameplay;

namespace ProjetoDC.Scripts.World;

public partial class FoodWorld : Node2D
{
    public Food Food { get; private set; }

    public void Initialize(Food food)
    {
        Food = food;

        GlobalPosition = food.Position;
    }
}