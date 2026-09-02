using Godot;
using ProjetoDC.Enums;

namespace ProjetoDC.Scripts.Models.World
{
    /// <summary>
    /// Ícone simples desenhado por código (sem depender de nenhum asset externo) pra
    /// diferenciar visualmente cada CenterAreaType dentro do hexágono, no lugar da letra
    /// inicial usada antes - placeholder/teste até ter arte de verdade (ver CenterArea.
    /// CreateAreaIcon). Estilo flat/minimalista de traço único, inspirado nos ícones
    /// convencionais de academia (halter), cama, talheres e cruz médica.
    /// </summary>
    public partial class AreaTypeIcon : Control
    {
        [Export]
        public CenterAreaType AreaType { get; set; } = CenterAreaType.Neutral;

        [Export]
        public Color IconColor { get; set; } = Colors.White;

        [Export]
        public float IconSize { get; set; } = 46f;

        [Export]
        public float LineWidth { get; set; } = 5f;

        public void Setup(CenterAreaType areaType, Color color)
        {
            AreaType = areaType;
            IconColor = color;

            QueueRedraw();
        }

        public override void _Draw()
        {
            switch (AreaType)
            {
                case CenterAreaType.Training:
                case CenterAreaType.TrainingHealthPoints:
                case CenterAreaType.TrainingAttack:
                case CenterAreaType.TrainingDefense:
                case CenterAreaType.TrainingSpecialAttack:
                case CenterAreaType.TrainingSpecialDefense:
                case CenterAreaType.TrainingSpeed:
                    DrawDumbbell();
                    break;

                case CenterAreaType.Dormitory:
                    DrawBed();
                    break;

                case CenterAreaType.Restaurant:
                    DrawForkAndKnife();
                    break;

                case CenterAreaType.Hospital:
                    DrawCross();
                    break;

                default:
                    DrawNeutralMark();
                    break;
            }
        }

        /// <summary>Treino: halter (barra + dois discos). Nas áreas específicas de um stat
        /// (ver CenterAreaType), soma um selo pequeno acima da barra identificando qual.</summary>
        private void DrawDumbbell()
        {
            float half = IconSize / 2f;

            Vector2 left = new(-half, 0);
            Vector2 right = new(half, 0);

            DrawLine(left, right, IconColor, LineWidth * 0.7f, true);

            float plateRadius = IconSize * 0.22f;

            DrawCircle(left, plateRadius, IconColor);
            DrawCircle(right, plateRadius, IconColor);

            var statType = CenterArea.GetForcedTrainingType(AreaType);

            if (statType.HasValue)
                DrawTrainingStatBadge(statType.Value);
        }

        /// <summary>
        /// Selo pequeno acima da barra do haltere, uma forma simples e distinta por stat
        /// (sem depender de texto/asset - mesmo espírito minimalista do resto do ícone):
        /// HP = ponto sólido, Ataque = triângulo, Defesa = losango, Ataque Especial = fagulha
        /// de 4 pontas, Defesa Especial = hexágono, Velocidade = seta.
        /// </summary>
        private void DrawTrainingStatBadge(TrainingType type)
        {
            Vector2 center = new(0, -IconSize * 0.44f);
            float size = IconSize * 0.16f;
            float thin = LineWidth * 0.55f;

            switch (type)
            {
                case TrainingType.HealthPoints:
                    DrawCircle(center, size * 0.8f, IconColor);
                    break;

                case TrainingType.Attack:
                    DrawPolyline(new[]
                    {
                        center + new Vector2(0, -size),
                        center + new Vector2(size * 0.85f, size * 0.7f),
                        center + new Vector2(-size * 0.85f, size * 0.7f),
                        center + new Vector2(0, -size),
                    }, IconColor, thin, true);
                    break;

                case TrainingType.Defense:
                    DrawPolyline(new[]
                    {
                        center + new Vector2(0, -size),
                        center + new Vector2(size, 0),
                        center + new Vector2(0, size),
                        center + new Vector2(-size, 0),
                        center + new Vector2(0, -size),
                    }, IconColor, thin, true);
                    break;

                case TrainingType.SpecialAttack:
                    DrawLine(center + new Vector2(-size, 0), center + new Vector2(size, 0), IconColor, thin, true);
                    DrawLine(center + new Vector2(0, -size), center + new Vector2(0, size), IconColor, thin, true);
                    DrawLine(center + new Vector2(-size * 0.7f, -size * 0.7f), center + new Vector2(size * 0.7f, size * 0.7f), IconColor, thin, true);
                    DrawLine(center + new Vector2(-size * 0.7f, size * 0.7f), center + new Vector2(size * 0.7f, -size * 0.7f), IconColor, thin, true);
                    break;

                case TrainingType.SpecialDefense:
                    var hex = new Vector2[7];

                    for (int i = 0; i <= 6; i++)
                    {
                        float angle = Mathf.Tau * i / 6f - Mathf.Pi / 2f;
                        hex[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * size;
                    }

                    DrawPolyline(hex, IconColor, thin, true);
                    break;

                case TrainingType.Speed:
                    DrawPolyline(new[]
                    {
                        center + new Vector2(-size * 0.6f, -size),
                        center + new Vector2(size * 0.6f, 0),
                        center + new Vector2(-size * 0.6f, size),
                    }, IconColor, thin, true);
                    break;
            }
        }

        /// <summary>Dormitório: cama simples (colchão + cabeceira) com um "z" de sono.</summary>
        private void DrawBed()
        {
            float w = IconSize;
            float h = IconSize * 0.5f;

            var mattress = new Rect2(new Vector2(-w / 2f, 0f), new Vector2(w, h));
            DrawRect(mattress, IconColor, false, LineWidth * 0.7f);

            var headboard = new Rect2(
                new Vector2(-w / 2f, -h * 0.8f),
                new Vector2(w * 0.22f, h * 0.8f)
            );
            DrawRect(headboard, IconColor, true);

            Vector2 zCenter = new(w * 0.18f, -h * 1.05f);
            float zSize = IconSize * 0.16f;

            var zPoints = new[]
            {
                zCenter + new Vector2(-zSize, -zSize * 0.6f),
                zCenter + new Vector2(zSize, -zSize * 0.6f),
                zCenter + new Vector2(-zSize, zSize * 0.6f),
                zCenter + new Vector2(zSize, zSize * 0.6f),
            };

            DrawPolyline(new[] { zPoints[0], zPoints[1], zPoints[2], zPoints[3] }, IconColor, LineWidth * 0.5f, true);
        }

        /// <summary>Refeitório: garfo e faca cruzados.</summary>
        private void DrawForkAndKnife()
        {
            float half = IconSize / 2f;
            float thin = LineWidth * 0.55f;

            float forkX = -half * 0.45f;

            DrawLine(new Vector2(forkX, -half * 0.1f), new Vector2(forkX, half), IconColor, thin, true);

            for (int i = -1; i <= 1; i++)
            {
                float tineX = forkX + i * thin * 2.2f;

                DrawLine(
                    new Vector2(tineX, -half * 0.1f),
                    new Vector2(tineX, -half * 0.75f),
                    IconColor,
                    thin * 0.7f,
                    true
                );
            }

            DrawLine(
                new Vector2(forkX - thin * 2.2f, -half * 0.1f),
                new Vector2(forkX + thin * 2.2f, -half * 0.1f),
                IconColor,
                thin * 0.7f,
                true
            );

            float knifeX = half * 0.45f;

            DrawLine(new Vector2(knifeX, -half * 0.05f), new Vector2(knifeX, half), IconColor, thin, true);

            var blade = new[]
            {
                new Vector2(knifeX - thin, -half * 0.05f),
                new Vector2(knifeX + thin, -half * 0.05f),
                new Vector2(knifeX, -half * 0.85f),
                new Vector2(knifeX - thin, -half * 0.05f),
            };

            DrawPolyline(blade, IconColor, thin * 0.6f, true);
        }

        /// <summary>Hospital: cruz médica.</summary>
        private void DrawCross()
        {
            float half = IconSize / 2f;
            float arm = IconSize * 0.32f;

            var vertical = new Rect2(new Vector2(-arm / 2f, -half), new Vector2(arm, IconSize));
            var horizontal = new Rect2(new Vector2(-half, -arm / 2f), new Vector2(IconSize, arm));

            DrawRect(vertical, IconColor, true);
            DrawRect(horizontal, IconColor, true);
        }

        /// <summary>Neutra: sem função especial - só um marcador discreto.</summary>
        private void DrawNeutralMark()
        {
            DrawCircle(Vector2.Zero, IconSize * 0.07f, IconColor);
        }
    }
}
