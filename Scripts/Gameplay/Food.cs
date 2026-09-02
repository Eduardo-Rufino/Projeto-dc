using Godot;

namespace ProjetoDC.Scripts.Gameplay;

public class Food
{
    public string Name { get; set; }

    public int RemainingNutrition { get; set; }

    public int MaxNutrition { get; set; }

    public float PositionX { get; set; }
    public float PositionY { get; set; }

    /// <summary>Se foi colocada dentro de uma área de Restaurante - estraga bem mais devagar
    /// (ver FoodWorld.OnHourPassed). Decidido uma vez, na colocação (Center.PlaceFood) -
    /// comida não se move depois de colocada, então não precisa recalcular.</summary>
    public bool PlacedInRestaurant { get; set; }

    /// <summary>Horas de jogo desde que foi colocada - incrementado a cada HourPassed
    /// enquanto ainda não estragou (ver FoodWorld.OnHourPassed).</summary>
    public int HoursSincePlaced { get; set; }

    /// <summary>Uma vez estragada, fica estragada até ser toda comida ou removida - trocar
    /// o sprite (FoodWorld) e os efeitos de quem comer (DigimonWorld.ProcessEating) dependem
    /// disso.</summary>
    public bool IsSpoiled { get; set; }

    public Food() { }

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