using Godot;
using ProjetoDC.Enums;
using ProjetoDC.Scripts.World;

namespace ProjetoDC.Scripts.Models.World
{
    public partial class CenterArea : Node2D
    {
        [Export]
        public CenterAreaType AreaType { get; private set; } = CenterAreaType.Neutral;

        [Export]
        public Vector2I GridPosition { get; set; } = Vector2I.Zero;

        private Polygon2D _polygon;
        private readonly RandomNumberGenerator _rng = new();

        [Export]
        public float DebugBorderWidth { get; set; } = 3f;

        public override void _Ready()
        {
            Position = CenterGrid.GridToWorld(GridPosition);

            _polygon = GetNode<Polygon2D>(
                "Visual/Polygon2D"
            );

            CreateDebugBorder();

            GD.Print(
                $"CenterArea criada: {Name} | Tipo {AreaType} | Grid: {GridPosition} | Position: {Position}"
            );

            var neighbors = CenterGrid.GetNeighbors(GridPosition);

            foreach (var neighbor in neighbors)
            {
                GD.Print($"Vizinhos possíveis: {neighbor}");
            }

            if (GridPosition == Vector2I.Zero)
            {
                TestRandomPoints();
            }
        }

        public Vector2 GetRandomPointInside()
        {
            var polygon = _polygon.Polygon;

            if (polygon.Length == 0)
            {
                GD.PrintErr(
                    $"CenterArea {Name} não possui Polygon2D configurado."
                );

                return GlobalPosition;
            }

            Rect2 bounds = GetPolygonBounds(polygon);

            for (int i = 0; i < 100; i++)
            {
                float x = _rng.RandfRange(
                    bounds.Position.X,
                    bounds.End.X
                );

                float y = _rng.RandfRange(
                    bounds.Position.Y,
                    bounds.End.Y
                );

                Vector2 point = new Vector2(x, y);

                if (Geometry2D.IsPointInPolygon(point, polygon))
                {
                    return ToGlobal(point);
                }
            }

            GD.PrintErr(
                $"Não foi possível encontrar ponto dentro da área {Name}."
            );

            return GlobalPosition;
        }

        public bool IsPointInside(Vector2 globalPosition)
        {
            if (_polygon == null)
                return false;

            Vector2 localPoint = ToLocal(globalPosition);

            return Geometry2D.IsPointInPolygon(
                localPoint,
                _polygon.Polygon
            );
        }

        private Rect2 GetPolygonBounds(Vector2[] polygon)
        {
            Vector2 min = polygon[0];
            Vector2 max = polygon[0];

            foreach (Vector2 point in polygon)
            {
                min.X = Mathf.Min(min.X, point.X);
                min.Y = Mathf.Min(min.Y, point.Y);

                max.X = Mathf.Max(max.X, point.X);
                max.Y = Mathf.Max(max.Y, point.Y);
            }

            return new Rect2(
                min,
                max - min
            );
        }

        public void TestRandomPoints()
        {
            for (int i = 0; i < 5; i++)
            {
                Vector2 point = GetRandomPointInside();

                GD.Print(
                    $"Ponto aleatório dentro da área {GridPosition}: {point}"
                );
            }
        }

        private void CreateDebugBorder()
        {
            var border = new Line2D();

            border.Name = "DebugBorder";
            border.Width = DebugBorderWidth;
            border.DefaultColor = GetAreaDebugColor();
            border.Closed = true;
            border.ZIndex = 10;

            border.Points = _polygon.Polygon;

            AddChild(border);
        }

        private Color GetAreaDebugColor()
        {
            return AreaType switch
            {
                CenterAreaType.Neutral => Colors.White,
                CenterAreaType.Training => Colors.Blue,
                CenterAreaType.Dormitory => Colors.Purple,
                CenterAreaType.Restaurant => Colors.Green,
                CenterAreaType.Hospital => Colors.Red,

                _ => Colors.White
            };
        }

        public void SetAreaType(CenterAreaType areaType)
        {
            AreaType = areaType;
        }

        public bool IsTrainingArea()
        {
            return AreaType == CenterAreaType.Training;
        }
    }
}