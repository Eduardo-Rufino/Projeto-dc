using Godot;
using ProjetoDC.Scripts.Data;
using System;

namespace ProjetoDC.Scripts.Gameplay
{
    /// <summary>
    /// Elemento decorativo de uma área de exploração (ver ExplorationArea.SpawnDecorations) -
    /// sprite estático sorteado entre as variações do tipo pedido, definidas pelo tema da área
    /// (ver AreaThemeData / Data/AreaThemes/). Só as variações que declaram uma caixa sólida
    /// (SolidBaseData) bloqueiam passagem de verdade: um StaticBody2D pequeno plantado na base
    /// do tronco/pedra/estalagmite, não no sprite inteiro - a copa da árvore não é sólida.
    /// O nó fica ancorado pela base (a Position que ExplorationArea atribui é o "pé" - mesma
    /// convenção dos sprites de Digimon), e é filho direto do SpawnsContainer com Y-sort
    /// ligado: comparar essa base com a posição (também na base) do Digimon é o que resolve
    /// sozinho ele desenhar atrás do objeto perto do topo e na frente perto da base.
    /// </summary>
    public partial class AreaDecoration : Node2D
    {
        /// <summary>Sorteia uma variação do <paramref name="kind"/> no tema e monta o sprite (e a
        /// caixa sólida, se a variação tiver uma).</summary>
        public void SetKind(AreaThemeData theme, string kind, Random random)
        {
            if (!theme.Variants.TryGetValue(kind, out var variants) || variants.Count == 0)
            {
                GD.PrintErr($"Tema '{theme.Id}' não tem variações para o tipo '{kind}'.");
                return;
            }

            var variant = variants[random.Next(variants.Count)];

            var texture = GD.Load<Texture2D>(theme.Folder + variant.File);

            if (texture == null)
            {
                GD.PrintErr($"Sprite de cenário não encontrado: {theme.Folder}{variant.File}");
                return;
            }

            var sprite = new Sprite2D
            {
                Texture = texture,
                TextureFilter = TextureFilterEnum.Nearest,
                Offset = new Vector2(0, -texture.GetHeight() / 2f),
            };

            AddChild(sprite);

            if (variant.Solid != null)
                AddSolidBase(variant.Solid);

            foreach (var extra in variant.ExtraSolids)
                AddSolidBase(extra);
        }

        private void AddSolidBase(SolidBaseData solid)
        {
            // Camada 1 ("obstáculo") - ver ExplorationDigimon/WildEncounterSpawn, que ficam
            // na máscara 1 especificamente pra colidir com isso.
            var body = new StaticBody2D
            {
                CollisionLayer = 1,
                CollisionMask = 0,
            };

            AddChild(body);

            var shape = new CollisionShape2D
            {
                Shape = new RectangleShape2D { Size = new Vector2(solid.Width, solid.Height) },
                Position = new Vector2(solid.CenterX, solid.CenterY),
            };

            body.AddChild(shape);
        }
    }
}
