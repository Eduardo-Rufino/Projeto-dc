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

            // Ilustração de verdade (ver GetAreaIllustrationPath) substitui o ícone
            // desenhado por código pra quem já tem arte - qualquer AreaType futuro sem
            // ilustração ainda cai no ícone antigo (CreateAreaIcon) como fallback.
            if (ShowTypeLabel && !CreateAreaIllustration())
                CreateAreaIcon();
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
