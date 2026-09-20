using Godot;
using ProjetoDC.Scripts.Managers;

namespace ProjetoDC.Scripts.UI
{
    /// <summary>
    /// Hora (fracionária, 0 a 24) que o fundo do Center mostra. Vem do relógio do jogo
    /// (GameManager.Save.World - o mesmo que o HUD mostra, já acelerado pelo botão x2), a não ser
    /// que um atalho de debug mude isso:
    /// <list type="bullet">
    /// <item><c>DEBUG_SKY_HOUR=18.5</c> - trava o céu numa hora fixa.</item>
    /// <item><c>DEBUG_SKY_CYCLE=60</c> - passa as 24h do céu em 60 segundos reais (ciclo contínuo).</item>
    /// <item>F9 / F10 / F11 (ver CenterSkyConfig) - volta/avança 1h e reseta, só em build de debug.</item>
    /// </list>
    /// Nada disso mexe no relógio de verdade (fome, idade, ovos...), só no que o céu desenha.
    /// </summary>
    public static class CenterSkyClock
    {
        private static readonly float? FixedHour = ParseEnv("DEBUG_SKY_HOUR");
        private static readonly float? CycleSeconds = ParseEnv("DEBUG_SKY_CYCLE");

        private static float _debugOffsetHours;

        public static float CurrentHour
        {
            get
            {
                if (FixedHour.HasValue)
                    return FixedHour.Value + _debugOffsetHours;

                if (CycleSeconds is > 0f)
                    return (float)(Time.GetTicksMsec() / 1000.0 / CycleSeconds.Value * 24.0) + _debugOffsetHours;

                var world = GameManager.Instance?.Save?.World;

                if (world == null)
                    return 12f;

                return world.CurrentHour + world.CurrentMinute / 60f + _debugOffsetHours;
            }
        }

        public static void ShiftDebugHours(float hours)
        {
            _debugOffsetHours += hours;

            GD.Print($"[Céu] hora exibida: {Mathf.PosMod(CurrentHour, 24f):0.00}h (deslocamento de debug: {_debugOffsetHours:+0.##;-0.##;0}h)");
        }

        public static void ResetDebugHours()
        {
            _debugOffsetHours = 0f;

            GD.Print($"[Céu] hora exibida: {Mathf.PosMod(CurrentHour, 24f):0.00}h (hora real)");
        }

        private static float? ParseEnv(string name)
        {
            string value = System.Environment.GetEnvironmentVariable(name);

            if (string.IsNullOrWhiteSpace(value))
                return null;

            return float.TryParse(value, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out float parsed)
                ? parsed
                : null;
        }
    }
}
