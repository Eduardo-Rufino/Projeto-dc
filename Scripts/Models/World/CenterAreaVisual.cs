using Godot;

namespace ProjetoDC.Scripts.World;

public partial class CenterAreaVisual : Node2D
{
    private Polygon2D _polygon;

    private const float HexagonRadius = 250f;

    public override void _Ready()
    {
        _polygon = GetNode<Polygon2D>("Polygon2D");

        CreateHexagon();
    }

    private void CreateHexagon()
    {
        Vector2[] points = new Vector2[6];

        for (int i = 0; i < 6; i++)
        {
            float angle = Mathf.DegToRad(60f * i);

            points[i] = new Vector2(
                Mathf.Cos(angle),
                Mathf.Sin(angle)
            ) * HexagonRadius;
        }

        _polygon.Polygon = points;
    }
}
