using Godot;

namespace ProjetoDC.Scripts.Gameplay;

public class Food
{
    public Vector2 Position { get; set; }

    public int RemainingNutrition { get; set; }

    public bool IsConsumed => RemainingNutrition <= 0;
}