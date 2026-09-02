using Godot;

namespace ProjetoDC.Scripts.UI
{
    public enum StatusIndicatorKind
    {
        Training,
        Sleeping,
        Sick,
    }

    /// <summary>
    /// Balão flutuante com um ícone sobre a cabeça do Digimon indicando um estado
    /// (treinando, dormindo, doente). Aparece com um pequeno "pop" e some com
    /// <see cref="FadeOutAndFree"/>, chamado pelo DigimonWorld quando o estado termina.
    /// </summary>
    public partial class DigimonStatusIndicator : Node2D
    {
        private const float BalloonWidth = 34f;
        private const float BalloonHeight = 26f;
        private const float BalloonBottomOffset = 10f;
        private const float TailHeight = 7f;

        private const float BobAmplitude = 2.5f;
        private const double BobSpeed = 4.0;
        private const double PopInDuration = 0.15;
        private const double FadeOutDuration = 0.2;

        private static readonly Color BalloonColor = new(1f, 1f, 1f, 0.95f);
        private static readonly Color BorderColor = new(0.15f, 0.15f, 0.15f, 1f);
        private static readonly Color BarColor = new(0.25f, 0.25f, 0.3f, 1f);
        private static readonly Color SleepColor = new(0.3f, 0.4f, 0.8f, 1f);
        private static readonly Color SickColor = new(0.8f, 0.2f, 0.2f, 1f);

        private StatusIndicatorKind _kind = StatusIndicatorKind.Training;

        private double _elapsed;
        private double _fadeElapsed;
        private bool _fadingOut;
        private Vector2 _basePosition;

        public override void _Ready()
        {
            ZIndex = 10;
            Scale = Vector2.Zero;

            QueueRedraw();
        }

        /// <summary>
        /// <paramref name="localOffset"/> é relativo ao pai (o DigimonWorld) - assim o balão
        /// acompanha o Digimon automaticamente enquanto ele é arrastado, sem precisar
        /// recalcular posição a cada frame.
        /// </summary>
        public void Initialize(Vector2 localOffset, StatusIndicatorKind kind = StatusIndicatorKind.Training)
        {
            Position = localOffset;
            _basePosition = localOffset;
            _kind = kind;

            QueueRedraw();
        }

        public void FadeOutAndFree()
        {
            _fadingOut = true;
        }

        public override void _Process(double delta)
        {
            _elapsed += delta;

            if (_fadingOut)
            {
                _fadeElapsed += delta;

                float fadeT = (float)Mathf.Clamp(_fadeElapsed / FadeOutDuration, 0.0, 1.0);

                Modulate = new Color(1f, 1f, 1f, 1f - fadeT);
                Scale = Vector2.One * (1f - fadeT * 0.3f);

                if (fadeT >= 1f)
                    QueueFree();

                return;
            }

            float popT = (float)Mathf.Clamp(_elapsed / PopInDuration, 0.0, 1.0);
            float pop = Mathf.Sin(popT * Mathf.Pi * 0.5f);

            Scale = Vector2.One * pop;

            float bob = popT >= 1f
                ? Mathf.Sin((float)(_elapsed * BobSpeed)) * BobAmplitude
                : 0f;

            Position = _basePosition + new Vector2(0, bob);
        }

        public override void _Draw()
        {
            Rect2 rect = DrawBalloon();

            switch (_kind)
            {
                case StatusIndicatorKind.Sleeping:
                    DrawSleepIcon(rect);
                    break;

                case StatusIndicatorKind.Sick:
                    DrawSickIcon(rect);
                    break;

                default:
                    DrawDumbbellIcon(rect);
                    break;
            }
        }

        private Rect2 DrawBalloon()
        {
            var rect = new Rect2(
                -BalloonWidth / 2f,
                -BalloonHeight - BalloonBottomOffset,
                BalloonWidth,
                BalloonHeight
            );

            DrawRect(rect, BalloonColor, true);
            DrawRect(rect, BorderColor, false, 2f);

            float tailTop = rect.Position.Y + rect.Size.Y;

            var tail = new[]
            {
                new Vector2(-4f, tailTop - 1f),
                new Vector2(4f, tailTop - 1f),
                new Vector2(0f, tailTop + TailHeight),
            };

            DrawPolygon(tail, new[] { BalloonColor });
            DrawPolyline(new[] { tail[0], tail[2], tail[1] }, BorderColor, 2f);

            return rect;
        }

        private void DrawDumbbellIcon(Rect2 rect)
        {
            float barY = rect.Position.Y + rect.Size.Y / 2f;
            const float barHalfWidth = 7f;
            const float barThickness = 3f;
            const float plateWidth = 4f;
            const float plateHeight = 12f;

            DrawRect(
                new Rect2(-barHalfWidth, barY - barThickness / 2f, barHalfWidth * 2f, barThickness),
                BarColor,
                true
            );

            DrawRect(
                new Rect2(-barHalfWidth - plateWidth, barY - plateHeight / 2f, plateWidth, plateHeight),
                BarColor,
                true
            );

            DrawRect(
                new Rect2(barHalfWidth, barY - plateHeight / 2f, plateWidth, plateHeight),
                BarColor,
                true
            );
        }

        private void DrawSleepIcon(Rect2 rect)
        {
            float centerY = rect.Position.Y + rect.Size.Y / 2f;
            const float halfWidth = 6f;
            const float halfHeight = 6.5f;

            var zShape = new[]
            {
                new Vector2(-halfWidth, centerY - halfHeight),
                new Vector2(halfWidth, centerY - halfHeight),
                new Vector2(-halfWidth, centerY + halfHeight),
                new Vector2(halfWidth, centerY + halfHeight),
            };

            DrawPolyline(zShape, SleepColor, 3f, true);
        }

        private void DrawSickIcon(Rect2 rect)
        {
            float centerY = rect.Position.Y + rect.Size.Y / 2f;
            const float crossSize = 14f;
            const float crossThickness = 4f;

            DrawRect(
                new Rect2(-crossThickness / 2f, centerY - crossSize / 2f, crossThickness, crossSize),
                SickColor,
                true
            );

            DrawRect(
                new Rect2(-crossSize / 2f, centerY - crossThickness / 2f, crossSize, crossThickness),
                SickColor,
                true
            );
        }
    }
}
