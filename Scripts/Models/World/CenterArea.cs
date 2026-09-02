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

        // A arena de batalha reaproveita CenterArea pros hexágonos de combate, mas ali o
        // tipo é sempre irrelevante pro jogador (não é o Center de verdade) - desliga a
        // letra indicadora nesse caso (ver BattleArena.tscn).
        [Export]
        public bool ShowTypeLabel { get; set; } = true;

        public override void _Ready()
        {
            Position = CenterGrid.GridToWorld(GridPosition);

            _polygon = GetNode<Polygon2D>(
                "Visual/Polygon2D"
            );

            CreateDebugBorder();

            if (ShowTypeLabel)
                CreateAreaIcon();

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
            border.DefaultColor = GetAreaColor(AreaType);
            border.Closed = true;
            border.ZIndex = 10;

            border.Points = _polygon.Polygon;

            AddChild(border);
        }

        /// <summary>Cor associada a cada tipo de área - usada na borda/ícone do hexágono
        /// real e também no minimapa (ver Scripts/UI/MiniMap.cs), por isso é pública/estática
        /// em vez de só um detalhe interno dessa classe.</summary>
        public static Color GetAreaColor(CenterAreaType areaType)
        {
            return areaType switch
            {
                CenterAreaType.Neutral => Colors.White,

                // Todas as variantes de treino (genérica + específicas por stat) compartilham
                // a mesma cor de categoria - o que diferencia uma da outra é o selo no ícone
                // (ver AreaTypeIcon.DrawTrainingStatBadge), não a cor.
                CenterAreaType.Training => Colors.Blue,
                CenterAreaType.TrainingHealthPoints => Colors.Blue,
                CenterAreaType.TrainingAttack => Colors.Blue,
                CenterAreaType.TrainingDefense => Colors.Blue,
                CenterAreaType.TrainingSpecialAttack => Colors.Blue,
                CenterAreaType.TrainingSpecialDefense => Colors.Blue,
                CenterAreaType.TrainingSpeed => Colors.Blue,

                CenterAreaType.Dormitory => Colors.Purple,
                CenterAreaType.Restaurant => Colors.Green,
                CenterAreaType.Hospital => Colors.Red,

                _ => Colors.White
            };
        }

        /// <summary>
        /// Indicador visual simples e desenhado por código (sem asset externo) do tipo da
        /// área, no meio do hexágono - placeholder/teste (ver AreaTypeIcon) até ter arte de
        /// verdade.
        /// </summary>
        private void CreateAreaIcon()
        {
            var icon = new AreaTypeIcon
            {
                Name = "AreaTypeIcon",
                // Negativo: fica acima do preenchimento da área (Visual, z_index -10) mas
                // abaixo dos Digimons (z_index 0 por padrão) - o ícone não deve tampar quem
                // está em pé em cima dele.
                ZIndex = -5,
                MouseFilter = Control.MouseFilterEnum.Ignore
            };

            AddChild(icon);

            icon.Setup(AreaType, GetAreaColor(AreaType));
        }

        public void SetAreaType(CenterAreaType areaType)
        {
            AreaType = areaType;
        }

        public bool IsTrainingArea()
        {
            return AreaType == CenterAreaType.Training || ForcedTrainingType.HasValue;
        }

        /// <summary>Stat que essa área força sempre que um Digimon treina nela, ou null pra
        /// área de treino genérica (stat aleatório - ver DigimonWorld.ProcessTraining) e pra
        /// qualquer área que não seja de treino.</summary>
        public TrainingType? ForcedTrainingType => GetForcedTrainingType(AreaType);

        public static TrainingType? GetForcedTrainingType(CenterAreaType areaType) => areaType switch
        {
            CenterAreaType.TrainingHealthPoints => TrainingType.HealthPoints,
            CenterAreaType.TrainingAttack => TrainingType.Attack,
            CenterAreaType.TrainingDefense => TrainingType.Defense,
            CenterAreaType.TrainingSpecialAttack => TrainingType.SpecialAttack,
            CenterAreaType.TrainingSpecialDefense => TrainingType.SpecialDefense,
            CenterAreaType.TrainingSpeed => TrainingType.Speed,
            _ => null
        };

        public bool IsDormitory()
        {
            return AreaType == CenterAreaType.Dormitory;
        }

        public bool IsHospital()
        {
            return AreaType == CenterAreaType.Hospital;
        }

        public bool IsRestaurant()
        {
            return AreaType == CenterAreaType.Restaurant;
        }
    }
}