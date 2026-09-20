using Godot;
using ProjetoDC.Enums;
using ProjetoDC.Scripts.Gameplay;
using ProjetoDC.Scripts.Managers;
using System.Threading.Tasks;

/// <summary>
/// Verificação manual (via log) da rede de segurança do arrastar de Comida/Remédio - ver
/// Center.CancelDragPlacement. Roda com a variável de ambiente DEBUG_TEST_PLACEMENT, mesmo
/// padrão dos outros DEBUG_TEST_* (ver Center._Ready). Injeta eventos de entrada reais
/// (Input.ParseInputEvent) e reproduz o bug original: o botão do mouse é solto mas o ButtonUp
/// do botão da HUD nunca chega, então a colocação nunca seria resolvida sozinha.
/// </summary>
public partial class Center
{
    private int _placementTestFailures;

    private void PlacementCheck(bool condition, string name)
    {
        if (condition)
        {
            GD.Print($"[OK] {name}");
            return;
        }

        _placementTestFailures++;
        GD.PrintErr($"[FALHOU] {name}");
    }

    private async Task PlacementWait(double seconds) =>
        await ToSignal(GetTree().CreateTimer(seconds), Timer.SignalName.Timeout);

    // Fora da janela, pra nenhum controle da HUD receber o clique injetado.
    private static readonly Vector2 OffscreenPoint = new(-500, -500);

    private static void InjectMouseLeft(bool pressed) =>
        Input.ParseInputEvent(new InputEventMouseButton
        {
            ButtonIndex = MouseButton.Left,
            Pressed = pressed,
            Position = OffscreenPoint,
            GlobalPosition = OffscreenPoint,
        });

    private static void InjectEscape() =>
        Input.ParseInputEvent(new InputEventKey { Keycode = Key.Escape, Pressed = true });

    private async void DebugTestPlacementSafety()
    {
        _placementTestFailures = 0;

        GD.Print("===== CenterPlacementDebugTest: início =====");

        var save = GameManager.Instance.Save.Center;
        int originalMedicine = save.Medicine;
        save.Medicine = System.Math.Max(1, originalMedicine);

        // 1. Esc cancela e não deixa o jogo preso
        BeginPlacingFood(new Food("Carne", 20, FoodType.Meat));
        PlacementCheck(IsDragPlacing, "Esc: a comida entrou em modo de posicionamento");
        InjectEscape();
        await PlacementWait(0.15);
        PlacementCheck(!IsDragPlacing && !_isPlacingFood && _placingFood == null,
            "Esc: cancelou a comida em andamento e limpou o estado");
        PlacementCheck(CanStartPlacingFood(), "Esc: dá pra começar outra colocação logo depois");

        // 2. Esc no remédio
        StartMedicinePlacement();
        PlacementCheck(_isPlacingMedicine && _placingMedicine != null, "Esc: o remédio entrou em modo de posicionamento");
        InjectEscape();
        await PlacementWait(0.15);
        PlacementCheck(!_isPlacingMedicine && _placingMedicine == null, "Esc: cancelou o remédio em andamento");

        // 3. Perda de foco da janela
        BeginPlacingFood(new Food("Carne", 20, FoodType.Meat));
        Notification((int)NotificationApplicationFocusOut);
        await PlacementWait(0.15);
        PlacementCheck(!IsDragPlacing, "Foco: perder o foco da janela cancelou a comida");

        StartMedicinePlacement();
        Notification((int)NotificationWMWindowFocusOut);
        await PlacementWait(0.15);
        PlacementCheck(!_isPlacingMedicine, "Foco: perder o foco da janela cancelou o remédio");

        // 4. O BUG ORIGINAL: botão do mouse pressionado no início, depois solto, mas o ButtonUp
        //    do botão da HUD nunca chega (aqui ninguém chama PlaceFood).
        InjectMouseLeft(true);
        await PlacementWait(0.05);
        BeginPlacingFood(new Food("Carne", 20, FoodType.Meat));
        await PlacementWait(0.6); // arrastar lento com o botão segurado NÃO pode cancelar
        PlacementCheck(_isPlacingFood && _placingFood != null,
            "ButtonUp perdido: arrastar lento (0.6s) com o botão segurado não é cancelado");

        InjectMouseLeft(false);
        await PlacementWait(0.1);
        PlacementCheck(_isPlacingFood, "ButtonUp perdido: dentro da folga (0.1s) ainda não cancelou");
        await PlacementWait(0.4);
        PlacementCheck(!_isPlacingFood && _placingFood == null,
            "ButtonUp perdido: cancelou sozinho depois da folga - não trava mais");
        PlacementCheck(CanStartPlacingFood(), "ButtonUp perdido: dá pra colocar comida de novo depois");

        // 5. Mesmo cenário com o remédio
        InjectMouseLeft(true);
        await PlacementWait(0.05);
        StartMedicinePlacement();
        InjectMouseLeft(false);
        await PlacementWait(0.5);
        PlacementCheck(!_isPlacingMedicine && _placingMedicine == null,
            "ButtonUp perdido (remédio): cancelou sozinho depois da folga");

        // 6. Fluxo normal: o ButtonUp chega no mesmo quadro e resolve antes da folga
        InjectMouseLeft(true);
        await PlacementWait(0.05);
        BeginPlacingFood(new Food("Carne", 20, FoodType.Meat));
        InjectMouseLeft(false);
        PlaceFood(); // é o que OnFoodOptionButtonUp faz - longe de qualquer área, cai no cancelamento normal
        await PlacementWait(0.5);
        PlacementCheck(!IsDragPlacing, "Fluxo normal: PlaceFood resolveu a colocação sem depender da rede de segurança");

        // 7. Quem começou SEM o mouse pressionado (teclado) não é cancelado pela rede do mouse
        BeginPlacingFood(new Food("Carne", 20, FoodType.Meat));
        await PlacementWait(0.6);
        PlacementCheck(_isPlacingFood, "Teclado: colocação sem mouse pressionado não é cancelada pela rede do mouse");
        InjectEscape();
        await PlacementWait(0.15);
        PlacementCheck(!IsDragPlacing, "Teclado: Esc ainda cancela");

        // 8. Um ButtonUp tardio depois de cancelar é inofensivo
        PlaceFood();
        PlaceMedicine();
        PlacementCheck(!IsDragPlacing, "ButtonUp tardio: PlaceFood/PlaceMedicine sem nada pendente não fazem nada");

        // 9. Carne Assada: sem estoque nem começa; com estoque, uma colocação bem-sucedida desconta
        //    só do estoque de assadas e a comida colocada fica marcada como assada.
        int originalCooked = save.CookedMeat;
        int originalMeat = save.Meat;

        save.CookedMeat = 0;
        StartCookedMeatPlacement();
        PlacementCheck(!IsDragPlacing, "Assada: sem estoque não começa a colocação");

        save.CookedMeat = 2;
        StartCookedMeatPlacement();
        PlacementCheck(_isPlacingFood && _placingFood != null && _placingFood.GetFood().IsCooked,
            "Assada: com estoque, começa a colocação de uma comida assada");

        var anyArea = System.Linq.Enumerable.FirstOrDefault(_centerAreas.Values);

        if (_placingFood != null && anyArea != null)
        {
            _placingFood.GlobalPosition = anyArea.GetRandomPointInside();
            var placedCooked = _placingFood;

            PlaceFood();

            PlacementCheck(save.CookedMeat == 1 && save.Meat == originalMeat,
                "Assada: colocar desconta 1 do estoque de assadas e nenhuma Carne crua");
            PlacementCheck(placedCooked.GetFood().IsCooked && placedCooked.GetFood().MaxNutrition == Food.CookedMeatNutrition,
                "Assada: a comida colocada é assada e com a nutrição maior");

            RemoveFood(placedCooked);
        }
        else
        {
            CancelDragPlacement("teste da Carne Assada sem área válida");
            PlacementCheck(false, "Assada: não foi possível montar o cenário de colocação");
        }

        save.CookedMeat = originalCooked;
        save.Meat = originalMeat;

        // nada de item foi consumido nos cancelamentos
        save.Medicine = originalMedicine;

        GD.Print(_placementTestFailures == 0
            ? "===== CenterPlacementDebugTest: TODOS OS TESTES PASSARAM ====="
            : $"===== CenterPlacementDebugTest: {_placementTestFailures} TESTE(S) FALHARAM =====");
    }
}
