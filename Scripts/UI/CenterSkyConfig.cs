using Godot;

namespace ProjetoDC.Scripts.UI
{
    /// <summary>
    /// Todos os parâmetros do fundo do Center (céu que muda com a hora, estrelas, nuvens, sombras e
    /// bases das salas) num lugar só - ver CenterSky (céu/estrelas/nuvens) e CenterHexBackdrop
    /// (sombras, bases e tom noturno das salas). Arte e convenções em Assets/Background/LEIA-ME_ceu.md.
    /// </summary>
    public static class CenterSkyConfig
    {
        public const string TextureFolder = "res://Assets/Background/";

        // Abaixo do mundo (layer 0) e do HUD (layer 1): o céu é o fundo de tudo.
        public const int SkyCanvasLayer = -100;

        // ---------------------------------------------------------------- ciclo do dia

        /// <summary>Um horário da tabela: o céu (degradê), o tom das nuvens, o alpha das estrelas e o
        /// tom das salas nesse horário. Entre dois horários tudo é interpolado pelo relógio do jogo.</summary>
        public readonly record struct Keyframe(
            float Hour,
            string SkyTexture,
            Color CloudTint,
            float StarsAlpha,
            Color RoomTint);

        private static readonly Color White = Colors.White;

        /// <summary>Tabela de horários, em ordem crescente e começando em 0h. Depois do último
        /// horário o próximo é o primeiro, no dia seguinte (19:30 -> 00:00).</summary>
        public static readonly Keyframe[] Keyframes =
        {
            new(0.0f,  "sky_00_noite.png",       Color.Color8(110, 120, 170), 1.0f, Color.Color8(200, 205, 235)),
            new(5.0f,  "sky_05_madrugada.png",   Color.Color8(150, 146, 190), 0.6f, Color.Color8(215, 218, 240)),
            new(6.5f,  "sky_06_amanhecer.png",   Color.Color8(255, 214, 206), 0.0f, White),
            new(9.0f,  "sky_09_manha.png",       Color.Color8(255, 255, 255), 0.0f, White),
            new(12.0f, "sky_12_meio_dia.png",    Color.Color8(255, 255, 255), 0.0f, White),
            new(16.0f, "sky_16_tarde.png",       Color.Color8(255, 244, 228), 0.0f, White),
            new(18.0f, "sky_18_por_do_sol.png",  Color.Color8(255, 188, 160), 0.0f, White),
            new(19.5f, "sky_19_anoitecer.png",   Color.Color8(170, 150, 196), 0.4f, Color.Color8(222, 222, 242)),
        };

        // ---------------------------------------------------------------- estrelas

        public const string StarsTexture = "sky_stars.png";

        // ---------------------------------------------------------------- nuvens

        public static readonly string[] FarCloudTextures = { "cloud_far_1.png", "cloud_far_2.png", "cloud_far_3.png" };
        public static readonly string[] NearCloudTextures = { "cloud_near_1.png", "cloud_near_2.png", "cloud_near_3.png" };

        /// <summary>Sentido de todas as nuvens: +1 = esquerda para a direita, -1 = o contrário.</summary>
        public const int CloudDirection = 1;

        // Quantas nuvens de cada camada existem ao mesmo tempo (sorteado uma vez, no início).
        public const int FarCloudCountMin = 5;
        public const int FarCloudCountMax = 7;
        public const int NearCloudCountMin = 3;
        public const int NearCloudCountMax = 4;

        // Velocidades em pixels por segundo (sorteadas por nuvem, a cada vez que ela reaparece).
        public const float FarCloudSpeedMin = 4f;
        public const float FarCloudSpeedMax = 8f;
        public const float NearCloudSpeedMin = 12f;
        public const float NearCloudSpeedMax = 20f;

        /// <summary>Chance de uma nuvem próxima aparecer na metade de baixo da tela (o resto
        /// aparece em qualquer altura) - as próximas passam "por baixo" do Center.</summary>
        public const float NearCloudLowerHalfChance = 0.7f;

        // ---------------------------------------------------------------- salas (sombra e base)

        // Tamanho do sprite das salas em pixels (arte). O sprite é esticado até a caixa do
        // hexágono (CenterArea.CreateAreaIllustration), então a escala em cima das salas é
        // (2 * raio) / 252 no X e (raio * raiz de 3) / 218 no Y - sombra e base usam a mesma.
        public static readonly Vector2 RoomArtSize = new(252f, 218f);

        // A sombra tem 8px de margem em volta e fica 26px abaixo da sala (em pixels de arte).
        public static readonly Vector2 ShadowOffsetArt = new(-8f, -8f + 26f);
        public const float ShadowAlpha = 0.3f;

        public const string ShadowTexture = "hex_shadow.png";
        public const string BaseTexture = "hex_base.png";

        // As salas desenham o preenchimento em z -10 e a ilustração em z -5: a sombra vai abaixo de
        // tudo isso e a base entre os dois (escondida sob a sala, só a borda de baixo aparece).
        public const int ShadowZIndex = -11;
        public const int BaseZIndex = -6;

        /// <summary>Escurece levemente as salas e as bases à noite (RoomTint da tabela).</summary>
        public static bool RoomNightTintEnabled = true;

        // ---------------------------------------------------------------- debug

        /// <summary>Teclas de debug (só em build de debug): avançam/voltam a hora mostrada pelo
        /// céu, sem mexer no relógio do jogo. F11 volta pra hora real.</summary>
        public const Key DebugHourBackKey = Key.F9;
        public const Key DebugHourForwardKey = Key.F10;
        public const Key DebugHourResetKey = Key.F11;
        public const float DebugHourStep = 1f;

        // ---------------------------------------------------------------- amostragem

        public readonly record struct SkySample(
            int FromIndex,
            int ToIndex,
            float T,
            Color CloudTint,
            float StarsAlpha,
            Color RoomTint);

        /// <summary>Acha os dois horários da tabela que cercam <paramref name="hour"/> (0 a 24) e
        /// interpola tudo entre eles. T vai de 0 (no primeiro horário) a 1 (no próximo).</summary>
        public static SkySample Sample(float hour)
        {
            hour = Mathf.PosMod(hour, 24f);

            int from = 0;

            for (int i = 0; i < Keyframes.Length; i++)
            {
                if (Keyframes[i].Hour <= hour)
                    from = i;
            }

            int to = (from + 1) % Keyframes.Length;

            float fromHour = Keyframes[from].Hour;
            float toHour = Keyframes[to].Hour + (to <= from ? 24f : 0f);

            float t = Mathf.Clamp((hour - fromHour) / (toHour - fromHour), 0f, 1f);

            var a = Keyframes[from];
            var b = Keyframes[to];

            return new SkySample(
                from,
                to,
                t,
                a.CloudTint.Lerp(b.CloudTint, t),
                Mathf.Lerp(a.StarsAlpha, b.StarsAlpha, t),
                a.RoomTint.Lerp(b.RoomTint, t));
        }
    }
}
