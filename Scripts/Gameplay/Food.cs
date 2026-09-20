using Godot;
using ProjetoDC.Enums;

namespace ProjetoDC.Scripts.Gameplay;

public class Food
{
    public string Name { get; set; }

    /// <summary>O que essa comida restaura em quem come (Fome ou Stamina) - ver
    /// DigimonWorld.ProcessEating. Meat por padrão (0) pra saves antigos, sem esse campo no
    /// JSON, desserializarem como Carne normalmente.</summary>
    public FoodType Type { get; set; } = FoodType.Meat;

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

    /// <summary>Carne Assada (ver GameManager.CookMeat): continua sendo FoodType.Meat pra tudo
    /// que já existe (procurar, comer, estragar) funcionar sem mudança, mas tem mais nutrição
    /// (<see cref="CookedMeatNutrition"/>) e estraga mais devagar (ver
    /// FoodWorld.GetSpoilHours). False por padrão - saves antigos, sem esse campo, carregam
    /// como Carne crua normalmente.</summary>
    public bool IsCooked { get; set; }

    public const int RawMeatNutrition = 20;
    public const int CookedMeatNutrition = 30;

    public static Food CreateMeat() => new("Carne", RawMeatNutrition);

    public static Food CreateCookedMeat() => new("Carne Assada", CookedMeatNutrition) { IsCooked = true };

    public Food() { }

    public Food(string name, int nutrition, FoodType type = FoodType.Meat)
    {
        Name = name;
        Type = type;

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

        return consumed;
    }

    public bool IsEmpty()
    {
        return RemainingNutrition <= 0;
    }
}