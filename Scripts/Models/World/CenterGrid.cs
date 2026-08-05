using Godot;
using System.Collections.Generic;

namespace ProjetoDC.Scripts.World;

public static class CenterGrid
{
    public const float HexagonRadius = 250f;

    public static readonly Vector2 Origin = new(647, 366);

    public static readonly Vector2I[] Directions =
    {
        new Vector2I(1, 0),
        new Vector2I(1, -1),
        new Vector2I(0, -1),
        new Vector2I(-1, 0),
        new Vector2I(-1, 1),
        new Vector2I(0, 1)
    };

    public static List<Vector2I> GetNeighbors(Vector2I position)
    {
        var neighbors = new List<Vector2I>();

        foreach (var direction in Directions)
        {
            neighbors.Add(position + direction);
        }

        return neighbors;
    }

    public static Vector2 GridToWorld(Vector2I gridPosition)
    {
        float horizontalSpacing = HexagonRadius * 1.5f;
        float verticalSpacing = HexagonRadius * Mathf.Sqrt(3f);

        float x = Origin.X + gridPosition.X * horizontalSpacing;

        float y = Origin.Y
            + (gridPosition.Y + gridPosition.X * 0.5f) * verticalSpacing;

        return new Vector2(x, y);
    }
}