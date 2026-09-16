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
        private Vector2[] _walkablePolygon;
        private readonly RandomNumberGenerator _rng = new();

        // Toda ilustração de área (ver GetAreaIllustrationPath) desenha paredes só nas 3
        // arestas de cima do hexágono (mesmo template em todas: hex_free, hex_treinamento,
        // hex_dormitorio etc.) - a parede de fundo (aresta horizontal do topo) e as duas
        // paredes laterais (arestas diagonais até as pontas esquerda/direita, mais finas por
        // causa da perspectiva em 3/4 do desenho). As 3 arestas de baixo não têm parede - o
        // chão vai até a borda do hexágono ali. Valores medidos direto nos pixels dos sprites
        // (ver Assets/Sprites/CenterAreas). Usados só pra restringir onde um Digimon pode
        // andar/ser solto (_walkablePolygon), nunca pro tamanho da ilustração em si (que
        // continua cobrindo o hexágono inteiro).
        private const float WallDepthTop = 75f;
        private const float WallDepthSide = 38f;

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

            _walkablePolygon = ComputeWalkablePolygon(_polygon.Polygon);

            // Ilustração de verdade (ver GetAreaIllustrationPath) substitui o ícone
            // desenhado por código pra quem já tem arte - qualquer AreaType futuro sem
            // ilustração ainda cai no ícone antigo (CreateAreaIcon) como fallback.
            if (ShowTypeLabel && !CreateAreaIllustration())
                CreateAreaIcon();
        }

        public Vector2 GetRandomPointInside()
        {
            var polygon = _walkablePolygon;

            if (polygon == null || polygon.Length == 0)
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

        /// <summary>Verdadeiro se o ponto está dentro da área andável (hexágono sem as
        /// paredes de cima - ver WallDepthTop/WallDepthSide/ComputeWalkablePolygon), não do
        /// hexágono inteiro.</summary>
        public bool IsPointInside(Vector2 globalPosition)
        {
            if (_walkablePolygon == null || _walkablePolygon.Length == 0)
                return false;

            Vector2 localPoint = ToLocal(globalPosition);

            return Geometry2D.IsPointInPolygon(
                localPoint,
                _walkablePolygon
            );
        }

        /// <summary>Encolhe as 3 arestas de cima do hexágono (parede de fundo + paredes
        /// laterais - ver WallDepthTop/WallDepthSide) pra dentro, preservando as 3 de baixo
        /// como estão - usado por GetRandomPointInside/IsPointInside pra impedir Digimons de
        /// andar ou serem soltos em cima de qualquer parte da parede desenhada na ilustração
        /// da área. GetPolygonBounds/GetGlobalHexagonBounds continuam usando o hexágono
        /// inteiro (_polygon.Polygon), só o andável é menor. Assume a ordem de vértices de
        /// CenterAreaVisual.CreateHexagon (ângulo 60*i a partir de i=0 na ponta direita): v0
        /// direita, v1 inferior-direita, v2 inferior-esquerda, v3 esquerda, v4
        /// superior-esquerda, v5 superior-direita - se essa geração mudar, os índices aqui
        /// precisam acompanhar.</summary>
        private Vector2[] ComputeWalkablePolygon(Vector2[] polygon)
        {
            if (polygon.Length != 6)
                return polygon;

            float[] edgeDepths = { 0f, 0f, 0f, WallDepthSide, WallDepthTop, WallDepthSide };

            return InsetPolygonEdges(polygon, edgeDepths);
        }

        /// <summary>Encolhe cada aresta i (de polygon[i] a polygon[i+1]) pra dentro pela
        /// distância edgeDepths[i], recalculando cada vértice como a interseção das duas
        /// arestas (deslocada ou não) que se encontram nele - arestas com profundidade 0
        /// ficam no lugar. "Pra dentro" é decidido comparando com o centroide, então funciona
        /// independente do sentido de enrolamento do polígono.</summary>
        private static Vector2[] InsetPolygonEdges(Vector2[] polygon, float[] edgeDepths)
        {
            int count = polygon.Length;

            Vector2 centroid = Vector2.Zero;

            foreach (Vector2 point in polygon)
                centroid += point;

            centroid /= count;

            var lines = new (Vector2 Point, Vector2 Direction)[count];

            for (int i = 0; i < count; i++)
            {
                Vector2 a = polygon[i];
                Vector2 b = polygon[(i + 1) % count];

                Vector2 direction = (b - a).Normalized();
                Vector2 normal = new Vector2(-direction.Y, direction.X);

                if ((centroid - (a + b) / 2f).Dot(normal) < 0f)
                    normal = -normal;

                lines[i] = (a + normal * edgeDepths[i], direction);
            }

            var result = new Vector2[count];

            for (int i = 0; i < count; i++)
            {
                int previous = (i - 1 + count) % count;

                result[i] = IntersectLines(lines[previous], lines[i]);
            }

            return result;
        }

        /// <summary>Ponto de interseção entre duas retas (ponto + direção) - usado por
        /// InsetPolygonEdges pra achar o vértice novo onde duas arestas (deslocadas ou não)
        /// se encontram. Retas paralelas (não deveria acontecer entre arestas adjacentes de
        /// um hexágono convexo) caem de volta no ponto de "a".</summary>
        private static Vector2 IntersectLines(
            (Vector2 Point, Vector2 Direction) a,
            (Vector2 Point, Vector2 Direction) b)
        {
            float denominator = a.Direction.X * b.Direction.Y - a.Direction.Y * b.Direction.X;

            if (Mathf.Abs(denominator) < 0.0001f)
                return a.Point;

            Vector2 diff = b.Point - a.Point;

            float t = (diff.X * b.Direction.Y - diff.Y * b.Direction.X) / denominator;

            return a.Point + a.Direction * t;
        }

        /// <summary>Retangulo (em espaço global) que envolve exatamente o hexágono - usado
        /// pela arena de batalha (ver BattleArena.CreateArenaBackground) pra alinhar uma
        /// ilustração única que cobre os 3 hexágonos de combate de uma vez, em vez de uma
        /// por hexágono.</summary>
        public Rect2 GetGlobalHexagonBounds()
        {
            Rect2 localBounds = GetPolygonBounds(_polygon.Polygon);

            return new Rect2(GlobalPosition + localBounds.Position, localBounds.Size);
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

        private const string IllustrationBasePath = "res://Assets/Sprites/CenterAreas/";

        /// <summary>Ilustração de verdade de cada tipo de área (ver Assets/Sprites/CenterAreas) -
        /// hexágonos pré-recortados no mesmo formato do hexágono do jogo (achatado nos lados
        /// esquerdo/direito, ver CenterAreaVisual.CreateHexagon), então basta escalar pro
        /// tamanho certo, sem crop nem UV manual.</summary>
        public static string GetAreaIllustrationPath(CenterAreaType areaType) => areaType switch
        {
            CenterAreaType.Neutral => IllustrationBasePath + "hex_free.png",
            CenterAreaType.Training => IllustrationBasePath + "hex_treinamento.png",
            CenterAreaType.Dormitory => IllustrationBasePath + "hex_dormitorio.png",
            CenterAreaType.Restaurant => IllustrationBasePath + "hex_refeitorio.png",
            CenterAreaType.Hospital => IllustrationBasePath + "hex_hospital.png",
            CenterAreaType.TrainingHealthPoints => IllustrationBasePath + "hex_hp.png",
            CenterAreaType.TrainingAttack => IllustrationBasePath + "hex_ataque_fisico.png",
            CenterAreaType.TrainingDefense => IllustrationBasePath + "hex_defesa_fisica.png",
            CenterAreaType.TrainingSpecialAttack => IllustrationBasePath + "hex_ataque_especial.png",
            CenterAreaType.TrainingSpecialDefense => IllustrationBasePath + "hex_defesa_especial.png",
            CenterAreaType.TrainingSpeed => IllustrationBasePath + "hex_velocidade.png",
            _ => null,
        };

        /// <summary>Cria o Sprite2D da ilustração de verdade, escalado pra cobrir exatamente o
        /// hexágono (calculado a partir dos vértices reais do Polygon2D, não um valor fixo -
        /// funciona mesmo se HexagonRadius mudar). Retorna false (sem criar nada) se esse
        /// AreaType ainda não tem ilustração, pra quem chamou saber que precisa cair no ícone
        /// antigo.</summary>
        private bool CreateAreaIllustration()
        {
            string path = GetAreaIllustrationPath(AreaType);

            if (string.IsNullOrEmpty(path) || !ResourceLoader.Exists(path))
                return false;

            var texture = GD.Load<Texture2D>(path);

            if (texture == null || texture.GetWidth() <= 0 || texture.GetHeight() <= 0)
                return false;

            Rect2 bounds = GetPolygonBounds(_polygon.Polygon);

            var sprite = new Sprite2D
            {
                Name = "AreaIllustration",
                Texture = texture,
                // Mesmo z_index do ícone antigo (acima do preenchimento branco do Polygon2D
                // em Visual, z_index -10; abaixo dos Digimons, z_index 0 por padrão).
                ZIndex = -5,
                TextureFilter = TextureFilterEnum.Nearest,
                Scale = new Vector2(
                    bounds.Size.X / texture.GetWidth(),
                    bounds.Size.Y / texture.GetHeight()
                ),
            };

            AddChild(sprite);

            return true;
        }

        /// <summary>
        /// Indicador visual simples e desenhado por código (sem asset externo) do tipo da
        /// área, no meio do hexágono - fallback pra qualquer AreaType sem ilustração de
        /// verdade ainda (ver CreateAreaIllustration/GetAreaIllustrationPath).
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
