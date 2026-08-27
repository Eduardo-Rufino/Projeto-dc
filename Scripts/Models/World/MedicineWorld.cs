using Godot;

namespace ProjetoDC.Scripts.World;

public partial class MedicineWorld : Node2D
{
    private bool _placementMode;

    public override void _Ready()
    {
        QueueRedraw();
    }

    public void SetPlacementMode(bool placementMode)
    {
        _placementMode = placementMode;
        QueueRedraw();
    }

    public override void _Draw()
    {
        float alpha = _placementMode ? 0.5f : 1.0f;

        // Corpo da cápsula
        DrawCircle(
            new Vector2(-7, 0),
            7,
            new Color(0.9f, 0.9f, 0.9f, alpha)
        );

        DrawCircle(
            new Vector2(7, 0),
            7,
            new Color(0.8f, 0.15f, 0.15f, alpha)
        );

        DrawRect(
            new Rect2(-7, -7, 14, 14),
            new Color(0.85f, 0.85f, 0.85f, alpha)
        );

        // Linha central
        DrawLine(
            new Vector2(0, -7),
            new Vector2(0, 7),
            new Color(0.2f, 0.2f, 0.2f, alpha),
            1.5f
        );
    }
}