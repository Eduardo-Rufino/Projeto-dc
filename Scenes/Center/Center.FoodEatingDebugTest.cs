using Godot;
using ProjetoDC.Enums;
using ProjetoDC.Scripts.Gameplay;
using ProjetoDC.Scripts.Managers;
using ProjetoDC.Scripts.World;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// Verificação manual (via log) da comida no Center de verdade (DigimonWorld comendo o que está no
/// chão): Carne crua, Carne Assada e Energético. Roda com DEBUG_TEST_FOOD (mesmo padrão dos outros
/// DEBUG_TEST_*, ver Center._Ready). Confere que um Digimon com fome vai até a comida e come, que a
/// Carne Assada sacia mais que a crua, que o Energético recupera Stamina, e que a crua estraga antes da
/// assada com o passar das horas de jogo (FoodWorld.OnHourPassed). Mexe no estado do jogo em
/// execução - rode com um save de teste (o driver de teste restaura o backup depois).
/// </summary>
public partial class Center
{
    private int _foodTestFailures;

    private void FoodCheck(bool condition, string name)
    {
        if (condition)
        {
            GD.Print($"[OK] {name}");
            return;
        }

        _foodTestFailures++;
        GD.PrintErr($"[FALHOU] {name}");
    }

    private async void DebugTestFoodEating()
    {
        _foodTestFailures = 0;
        GD.Print("===== CenterFoodEatingDebugTest: início =====");

        await PlacementWait(2.0);

        var gm = GameManager.Instance;
        var save = gm.Save.Center;

        foreach (var f in _foods.ToList())
            RemoveFood(f);

        var world = _digimonWorlds.FirstOrDefault(d => GodotObject.IsInstanceValid(d));
        FoodCheck(world != null, "há pelo menos um Digimon no Center pra testar");

        if (world == null)
            return;

        var digimon = world._digimon;
        save.Meat = 5;
        save.CookedMeat = 5;
        save.StaminaSnacks = 5;

        int rawGain = -1, cookedGain = -1;

        foreach (var kind in new[] { "crua", "assada", "energetico" })
        {
            digimon.SetHealthState(HealthState.Healthy);
            digimon.RestoreHealth();
            digimon.Hunger = 10;
            digimon.Stamina = 20;

            int stockBefore = kind == "crua" ? save.Meat : kind == "assada" ? save.CookedMeat : save.StaminaSnacks;

            if (kind == "crua") StartFoodPlacement();
            else if (kind == "assada") StartCookedMeatPlacement();
            else StartStaminaSnackPlacement();

            FoodCheck(_isPlacingFood && _placingFood != null, $"{kind}: começou a colocar");

            if (_placingFood == null)
                continue;

            _placingFood.GlobalPosition = world.GlobalPosition + new Vector2(25, 0);
            var placedFood = _placingFood.GetFood();
            PlaceFood();

            int stockAfter = kind == "crua" ? save.Meat : kind == "assada" ? save.CookedMeat : save.StaminaSnacks;
            FoodCheck(stockAfter == stockBefore - 1, $"{kind}: colocar desconta 1 do estoque ({stockBefore} -> {stockAfter})");

            int hungerBefore = digimon.Hunger, staminaBefore = digimon.Stamina;
            await PlacementWait(25.0);

            if (kind == "energetico")
            {
                FoodCheck(digimon.Stamina > staminaBefore, $"energetico: o Digimon recuperou Stamina ({staminaBefore} -> {digimon.Stamina})");
                GD.Print($"[INFO] energetico: sobrou {placedFood.RemainingNutrition}/{placedFood.MaxNutrition} de nutrição após 25s; atividade={digimon.Activity}");
                await PlacementWait(40.0);
                GD.Print($"[INFO] energetico: após +40s sobrou {placedFood.RemainingNutrition}/{placedFood.MaxNutrition}; stamina {digimon.Stamina}/{digimon.MaxStamina}; atividade={digimon.Activity}");
            }
            else
            {
                int gain = digimon.Hunger - hungerBefore;
                FoodCheck(gain > 0, $"{kind}: o Digimon comeu (fome {hungerBefore} -> {digimon.Hunger}, +{gain})");

                if (kind == "crua") rawGain = gain;
                else cookedGain = gain;
            }

            foreach (var f in _foods.ToList())
                RemoveFood(f);
        }

        if (rawGain > 0 && cookedGain > 0)
            FoodCheck(cookedGain > rawGain, $"a Carne Assada sacia mais que a crua (+{cookedGain} x +{rawGain})");

        // estrago: crua estraga antes da assada (fora do Refeitório), longe do Digimon (ele não come)
        digimon.Hunger = 100;

        Food raw = Food.CreateMeat(), cooked = Food.CreateCookedMeat();
        var area = GetAreaAtPosition(world.GlobalPosition);
        FoodCheck(area != null, "o Digimon está dentro de uma área do Center");

        if (area != null && !area.IsRestaurant())
        {
            save.Meat = 5; save.CookedMeat = 5;
            StartFoodPlacement();
            var rawWorld = _placingFood;
            rawWorld.GlobalPosition = world.GlobalPosition + new Vector2(60, 40);
            PlaceFood();

            StartCookedMeatPlacement();
            var cookedWorld = _placingFood;
            cookedWorld.GlobalPosition = world.GlobalPosition + new Vector2(80, 40);
            PlaceFood();

            var rawFood = rawWorld.GetFood();
            var cookedFood = cookedWorld.GetFood();

            for (int hour = 1; hour <= 14; hour++)
            {
                digimon.Hunger = 100;
                digimon.Stamina = digimon.MaxStamina;
                gm.ClockSystem.Update(60.0); // 1 minuto de jogo por segundo: 60s = 1 hora

                if (hour == 5)
                    FoodCheck(!rawFood.IsSpoiled && !cookedFood.IsSpoiled, "após 5h nenhuma das duas estragou");

                if (hour == 7)
                    FoodCheck(rawFood.IsSpoiled && !cookedFood.IsSpoiled, "após 7h a crua estragou (6h) e a assada não");

                if (hour == 11)
                    FoodCheck(!cookedFood.IsSpoiled, "após 11h a assada ainda está boa");

                if (hour == 13)
                    FoodCheck(cookedFood.IsSpoiled, "após 13h a assada estragou (12h)");
            }

            foreach (var f in _foods.ToList())
                RemoveFood(f);
        }
        else
        {
            GD.Print("[AVISO] o Digimon está no Refeitório - o teste de estrago fora do Refeitório foi pulado");
        }

        GD.Print(_foodTestFailures == 0
            ? "===== CenterFoodEatingDebugTest: TODOS OS TESTES PASSARAM ====="
            : $"===== CenterFoodEatingDebugTest: {_foodTestFailures} TESTE(S) FALHARAM =====");
    }
}
