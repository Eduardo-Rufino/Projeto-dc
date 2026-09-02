using Godot;

namespace ProjetoDC.Scripts.UI
{
    /// <summary>
    /// Relógio analógico simples (mostrador + ponteiro de hora e de minuto), desenhado
    /// direto via _Draw - sem sprite/textura. É um relógio comum de 12h (o ponteiro de hora
    /// dá duas voltas por dia de 24h), só pra dar uma leitura visual rápida do horário atual
    /// junto do texto digital que já existia.
    /// </summary>
    public partial class AnalogClock : Control
    {
        [Export] public float Radius { get; set; } = 32f;
        [Export] public Color FaceColor { get; set; } = new(0.08f, 0.09f, 0.11f, 1f);
        [Export] public Color RimColor { get; set; } = new(0.4f, 0.43f, 0.46f, 1f);
        [Export] public Color HourHandColor { get; set; } = Colors.White;
        [Export] public Color MinuteHandColor { get; set; } = new(0.7f, 0.72f, 0.75f, 1f);

        private int _hour;
        private int _minute;

        public override void _Ready()
        {
            CustomMinimumSize = new Vector2(Radius * 2f, Radius * 2f);
        }

        public void SetTime(int hour, int minute)
        {
            _hour = hour;
            _minute = minute;

            QueueRedraw();
        }

        public override void _Draw()
        {
            Vector2 center = Size / 2f;

            DrawCircle(center, Radius, FaceColor);
            DrawArc(center, Radius, 0f, Mathf.Tau, 48, RimColor, 2f, true);

            // Marcadores nas 12 posições (só traços, sem números - mantém simples).
            for (int i = 0; i < 12; i++)
            {
                float markAngle = Mathf.Tau * i / 12f - Mathf.Pi / 2f;
                Vector2 direction = new(Mathf.Cos(markAngle), Mathf.Sin(markAngle));

                DrawLine(
                    center + direction * (Radius - 5f),
                    center + direction * Radius,
                    RimColor,
                    1.5f
                );
            }

            // Ponteiro de hora: meia-volta a cada 6h (relógio comum de 12h, duas voltas
            // por dia de 24h) - soma a fração de minuto pra não "pular" de hora em hora.
            float hour12 = _hour % 12 + _minute / 60f;
            float hourAngle = Mathf.Tau * hour12 / 12f - Mathf.Pi / 2f;

            DrawLine(
                center,
                center + new Vector2(Mathf.Cos(hourAngle), Mathf.Sin(hourAngle)) * (Radius * 0.5f),
                HourHandColor,
                3f,
                true
            );

            float minuteAngle = Mathf.Tau * _minute / 60f - Mathf.Pi / 2f;

            DrawLine(
                center,
                center + new Vector2(Mathf.Cos(minuteAngle), Mathf.Sin(minuteAngle)) * (Radius * 0.8f),
                MinuteHandColor,
                2f,
                true
            );

            DrawCircle(center, 2.5f, HourHandColor);
        }
    }
}
