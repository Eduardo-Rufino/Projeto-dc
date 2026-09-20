using Godot;
using ProjetoDC.Scripts.Data;
using ProjetoDC.Scripts.Gameplay;
using ProjetoDC.Scripts.Managers;
using ProjetoDC.Scripts.Models.World;
using ProjetoDC.Scripts.World;
using System.Text.Json;

namespace ProjetoDC.Scripts.Systems.Kitchen
{
    /// <summary>
    /// Verificação manual (via log) da cozinha do Meramon: item da Carne Assada, conversão de
    /// estoque, nutrição e tempo de estrago (crua x assada, Refeitório x fora) e compatibilidade
    /// de save. Roda com DEBUG_TEST_COOKING (mesmo padrão de DEBUG_TEST_EXPLORATION). Só usa
    /// objetos novos em memória: nada toca no save real.
    /// </summary>
    public static class CookingDebugTests
    {
        private static int _failures;

        public static void Run()
        {
            _failures = 0;

            GD.Print("===== CookingDebugTests: início =====");

            TestItems();
            TestNutritionAndSpoil();
            TestStockConversion();
            TestSaveCompatibility();
            TestMeramon();

            GD.Print(_failures == 0
                ? "===== CookingDebugTests: TODOS OS TESTES PASSARAM ====="
                : $"===== CookingDebugTests: {_failures} FALHA(S) =====");
        }

        private static void Check(bool ok, string message)
        {
            if (ok)
                return;

            _failures++;
            GD.PrintErr($"[FALHOU] {message}");
        }

        private static void TestItems()
        {
            var db = DatabaseManager.Instance;
            var cooked = db.GetItem(ItemIds.CookedMeat);
            var raw = db.GetItem(ItemIds.Meat);

            Check(cooked != null, "item Carne Assada (ItemIds.CookedMeat) não existe em Data/Itens");
            Check(raw != null, "item Carne não existe");

            if (cooked == null || raw == null)
                return;

            Check(ResourceLoader.Exists(cooked.IconPath), $"ícone da Carne Assada não existe: {cooked.IconPath}");
            Check(ResourceLoader.Exists(raw.IconPath), $"ícone da Carne não existe: {raw.IconPath}");
            Check(cooked.IconPath != raw.IconPath, "Carne e Carne Assada não deveriam ter o mesmo ícone");
            GD.Print("[OK] itens Carne / Carne Assada com ícones distintos");
        }

        private static void TestNutritionAndSpoil()
        {
            var raw = Food.CreateMeat();
            var cooked = Food.CreateCookedMeat();

            Check(!raw.IsCooked && cooked.IsCooked, "IsCooked errado nas fábricas");
            Check(cooked.MaxNutrition > raw.MaxNutrition, "Carne Assada deveria saciar mais que a crua");

            foreach (bool restaurant in new[] { false, true })
            {
                raw.PlacedInRestaurant = restaurant;
                cooked.PlacedInRestaurant = restaurant;

                Check(FoodWorld.GetSpoilHours(cooked) > FoodWorld.GetSpoilHours(raw),
                    $"assada deveria durar mais que crua (Refeitório={restaurant})");
            }

            foreach (var food in new[] { raw, cooked })
            {
                food.PlacedInRestaurant = false;
                int outside = FoodWorld.GetSpoilHours(food);
                food.PlacedInRestaurant = true;
                int inside = FoodWorld.GetSpoilHours(food);

                Check(inside > outside, $"no Refeitório deveria durar mais ({(food.IsCooked ? "assada" : "crua")}: {inside}h x {outside}h)");
            }

            Check(cooked.Type == Enums.FoodType.Meat, "Carne Assada precisa continuar FoodType.Meat pro Digimon achar e comer");
            GD.Print($"[OK] nutrição {raw.MaxNutrition} -> {cooked.MaxNutrition}; estrago crua {Spoil(false, false)}h/{Spoil(false, true)}h, assada {Spoil(true, false)}h/{Spoil(true, true)}h (fora/Refeitório)");
        }

        private static int Spoil(bool cooked, bool restaurant)
        {
            var f = cooked ? Food.CreateCookedMeat() : Food.CreateMeat();
            f.PlacedInRestaurant = restaurant;
            return FoodWorld.GetSpoilHours(f);
        }

        private static void TestStockConversion()
        {
            var center = new CenterState();
            center.Meat = 5;
            center.CookedMeat = 0;

            // A encomenda tira as cruas do estoque na hora, mas as assadas só chegam ao terminar.
            Check(center.StartCooking(3, 20) == 3 && center.Meat == 2 && center.CookedMeat == 0 && center.IsCooking, "encomendar 3 de 5 deveria tirar 3 cruas e não dar assadas ainda");
            Check(center.StartCooking(1, 20) == 0 && center.Meat == 2, "com o fogão ocupado nada deveria ser encomendado");

            int finishedEarly = 0;

            for (int minute = 1; minute < 20; minute++)
                finishedEarly += center.AdvanceCookingMinute();

            Check(finishedEarly == 0 && center.CookedMeat == 0 && center.CookingMinutesLeft == 1, "faltando 1 minuto nada deveria estar pronto");
            Check(center.AdvanceCookingMinute() == 3 && center.CookedMeat == 3 && !center.IsCooking, "no minuto 20 as 3 assadas deveriam chegar ao estoque");
            Check(center.AdvanceCookingMinute() == 0 && center.CookedMeat == 3, "fogão vazio não deveria produzir nada");

            Check(center.StartCooking(10, 20) == 2 && center.Meat == 0, "encomendar mais que o estoque deveria pegar só o que há");
            center.CookingMeatAmount = 0;
            center.CookingMinutesLeft = 0;
            Check(center.StartCooking(1, 20) == 0, "sem carne crua nada deveria ser encomendado");

            center.Meat = 4;
            Check(center.StartCooking(0, 20) == 0 && center.StartCooking(-3, 20) == 0 && center.Meat == 4, "quantidade zero/negativa não deveria mexer no estoque");
            GD.Print("[OK] encomenda da cozinha (parcial, ocupado, tempo, excedente, vazio, inválido)");
        }

        private static void TestSaveCompatibility()
        {
            // Save antigo: Food sem o campo IsCooked precisa carregar como Carne crua.
            var old = JsonSerializer.Deserialize<Food>("{\"Name\":\"Carne\",\"RemainingNutrition\":20,\"MaxNutrition\":20}");
            Check(old != null && !old.IsCooked, "Food antigo (sem IsCooked) deveria carregar como crua");

            var roundTrip = JsonSerializer.Deserialize<Food>(JsonSerializer.Serialize(Food.CreateCookedMeat()));
            Check(roundTrip != null && roundTrip.IsCooked && roundTrip.MaxNutrition == Food.CookedMeatNutrition, "IsCooked não sobreviveu ao salvar/carregar");

            var cooking = new CenterState { CookingMeatAmount = 4, CookingMinutesLeft = 12 };
            var cookingBack = JsonSerializer.Deserialize<CenterState>(JsonSerializer.Serialize(cooking));
            Check(cookingBack != null && cookingBack.CookingMeatAmount == 4 && cookingBack.CookingMinutesLeft == 12, "encomenda em andamento não sobreviveu ao salvar/carregar");
            GD.Print("[OK] compatibilidade de save (Food antigo e ida-e-volta da assada)");
        }

        private static void TestMeramon()
        {
            var npc = System.Linq.Enumerable.FirstOrDefault(DatabaseManager.Instance.GetAllNpcs(),
                n => n.RecruitmentPerk == Enums.NpcPerkType.KitchenCook);

            Check(npc != null && npc.Name == "Meramon", "o NPC com o perk KitchenCook deveria ser o Meramon");

            if (npc == null)
                return;

            Check(npc.IsRecruitable, "Meramon precisa ser recrutável");
            Check(!npc.RecruitmentTrainingAreaType.HasValue, "Meramon não deveria ocupar nenhum tipo de treino");
            Check(npc.RecruitmentPerkDescription.ToLowerInvariant().Contains("assa"), "a descrição do perk deveria explicar que ele assa carnes");
            GD.Print("[OK] Meramon: recrutável, sem tipo de treino, perk descrito");
        }
    }
}
