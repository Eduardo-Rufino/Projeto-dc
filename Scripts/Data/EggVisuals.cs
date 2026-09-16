using ProjetoDC.Enums;
using System.Collections.Generic;

namespace ProjetoDC.Scripts.Data
{
    /// <summary>
    /// Caminho do sprite de Digitama pra cada EggType - cada arquivo é uma spritesheet de 3
    /// frames (48x16, 16px cada), igual ao Digitama genérico que existia antes: frames 0/1
    /// alternam no "chacoalhar" enquanto o ovo incuba, frame 2 é a rachadura mostrada quando
    /// pronto pra chocar (ver EggWorld.UpdateFrame). Usado tanto pelo visual do ovo no Center
    /// (EggWorld) quanto pelos ícones da tela de escolha de tipo na Loja (ShopScreen).
    /// </summary>
    public static class EggVisuals
    {
        public static readonly Dictionary<EggType, string> TexturePaths = new()
        {
            { EggType.Dragon, "res://Assets/Sprites/DigiTamas/DragonDigitama.png" },
            { EggType.Beast, "res://Assets/Sprites/DigiTamas/BeastDigitama.png" },
            { EggType.Water, "res://Assets/Sprites/DigiTamas/WaterDigitama.png" },
            { EggType.Forest, "res://Assets/Sprites/DigiTamas/ForestDigitama.png" },
            { EggType.Fire, "res://Assets/Sprites/DigiTamas/FireDigitama.png" },
            { EggType.Dark, "res://Assets/Sprites/DigiTamas/PicoDevi_Digitama.png" },
            { EggType.Light, "res://Assets/Sprites/DigiTamas/LightDigitama.png" },
        };

        public static string GetTexturePath(EggType type) =>
            TexturePaths.TryGetValue(type, out var path) ? path : TexturePaths[EggType.Dragon];
    }
}
