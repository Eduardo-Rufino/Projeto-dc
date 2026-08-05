using Godot;
using System;

public partial class CenterExpansionSlotVisual : Node2D
{
    private Area2D _area;

    public Vector2I GridPosition { get; private set; }

    public event Action<Vector2I> SlotClicked;

    public void Initialize(Vector2I gridPosition)
    {
        GridPosition = gridPosition;
    }

    public override void _Ready()
    {
        _area = GetNode<Area2D>("Area2D");

        _area.InputEvent += OnAreaInputEvent;
    }

    private void OnAreaInputEvent(
        Node viewport,
        InputEvent @event,
        long shapeIdx)
    {
        if (@event is InputEventMouseButton mouseEvent)
        {
            if (mouseEvent.ButtonIndex == MouseButton.Left &&
                mouseEvent.Pressed)
            {
                GD.Print(
                    $"SLOT CLICADO! Grid: {GridPosition}"
                );

                SlotClicked?.Invoke(GridPosition);
            }
        }
    }
}