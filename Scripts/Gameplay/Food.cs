using Godot;

namespace ProjetoDC.Scripts.Gameplay;

public class Food
{
    public string Name { get; set; }

    public int RemainingNutrition { get; private set; }

    public int MaxNutrition { get; private set; }

    public Food(string name, int nutrition)
    {
        Name = name;

        MaxNutrition = nutrition;
        RemainingNutrition = nutrition;
    }

    public int Consume(int amount)
    {
        if (amount <= 0)
            return 0;

        if (RemainingNutrition <= 0)
            return 0;

        int consumed = Mathf.Min(amount, RemainingNutrition);

        RemainingNutrition -= consumed;

        GD.Print($"{Name}: consumiu {consumed}. Restante: {RemainingNutrition}");

        return consumed;
    }

    public bool IsEmpty()
    {
        return RemainingNutrition <= 0;
    }
}