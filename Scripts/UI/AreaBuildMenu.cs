using Godot;
using ProjetoDC.Enums;
using System;

namespace ProjetoDC.Scripts.UI;

public partial class AreaBuildMenu : Control
{
    public event Action<CenterAreaType> AreaTypeSelected;
    public event Action Cancelled;

    private Button _trainingButton;
    private Button _dormitoryButton;
    private Button _restaurantButton;
    private Button _hospitalButton;
    private Button _cancelButton;

    public override void _Ready()
    {
        _trainingButton = GetNode<Button>(
            "Panel/VBoxContainer/TrainingButton"
        );

        _dormitoryButton = GetNode<Button>(
            "Panel/VBoxContainer/DormitoryButton"
        );

        _restaurantButton = GetNode<Button>(
            "Panel/VBoxContainer/RestaurantButton"
        );

        _hospitalButton = GetNode<Button>(
            "Panel/VBoxContainer/HospitalButton"
        );

        _cancelButton = GetNode<Button>(
            "Panel/VBoxContainer/CancelButton"
        );

        _trainingButton.Pressed += () =>
            SelectAreaType(CenterAreaType.Training);

        _dormitoryButton.Pressed += () =>
            SelectAreaType(CenterAreaType.Dormitory);

        _restaurantButton.Pressed += () =>
            SelectAreaType(CenterAreaType.Restaurant);

        _hospitalButton.Pressed += () =>
            SelectAreaType(CenterAreaType.Hospital);

        _cancelButton.Pressed += OnCancelPressed;

        Hide();
    }

    public void Open()
    {
        Show();
    }

    private void SelectAreaType(CenterAreaType areaType)
    {
        GD.Print($"Tipo de área escolhido: {areaType}");

        Hide();

        AreaTypeSelected?.Invoke(areaType);
    }

    private void OnCancelPressed()
    {
        GD.Print("Construção cancelada.");

        Hide();

        Cancelled?.Invoke();
    }
}