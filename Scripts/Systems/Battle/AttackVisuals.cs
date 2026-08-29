using Godot;
using ProjetoDC.Enums;
using System;
using System.Collections.Generic;

namespace ProjetoDC.Scripts.Systems.Battle
{
    /// <summary>
    /// Escolhe o sprite de projétil arremessado num ataque à distância, de acordo com o
    /// Element do Digimon atacante. Cada elemento tem algumas opções (quando existem sprites
    /// parecidos na pasta), escolhidas aleatoriamente pra dar variedade.
    /// </summary>
    public static class AttackVisuals
    {
        private const string Folder = "res://Assets/Sprites/attacks/";

        private static readonly Random _random = new();

        private static readonly Dictionary<DigimonElement, string[]> ProjectilesByElement = new()
        {
            { DigimonElement.Fire, new[] { "1_fogo.png", "5_fogo_vermelho.png" } },
            { DigimonElement.Water, new[] { "4_agua_azul.png", "9_agua_azul2.png", "8_respingo_azul.png", "9_ondas_ciano.png", "2_gelo_raio_1.png", "6_gelo_ciano.png" } },
            { DigimonElement.Plant, new[] { "7_pedras.png" } },
            { DigimonElement.Electric, new[] { "3_raio_verde.png", "7_raio_verde2.png" } },
            { DigimonElement.Wind, new[] { "8_vento_dourado.png" } },
            { DigimonElement.Earth, new[] { "7_pedras.png" } },
            { DigimonElement.Light, new[] { "3_chama_amarela.png", "5_lanca_amarela.png" } },
            { DigimonElement.Dark, new[] { "4_chama_roxa.png", "6_onda_roxa.png" } },
            { DigimonElement.Neutral, new[] { "10_nave_projetil.png" } },
        };

        private const string FallbackFile = "10_nave_projetil.png";

        public static Texture2D GetProjectileTexture(DigimonElement element)
        {
            string fileName = FallbackFile;

            if (ProjectilesByElement.TryGetValue(element, out var options) && options.Length > 0)
            {
                fileName = options[_random.Next(options.Length)];
            }

            return GD.Load<Texture2D>(Folder + fileName);
        }
    }
}
