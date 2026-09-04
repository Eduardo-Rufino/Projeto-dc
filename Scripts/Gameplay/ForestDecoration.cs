using Godot;
using System;
using System.Collections.Generic;

namespace ProjetoDC.Scripts.Gameplay
{
    public enum ForestDecorationKind
    {
        Tree,
        Grass,
        Flower,
        Mushroom,
        Rock,
        Bush,
    }

    /// <summary>
    /// Elemento decorativo de uma área de exploração (ver ExplorationArea.SpawnDecorations) -
    /// sprite estático sorteado entre as variações de Assets/Areas/Forest/ pra cada Kind.
    /// Árvore/pedra bloqueiam passagem de verdade (StaticBody2D pequeno plantado na base do
    /// tronco/pedra, não no sprite inteiro - a copa da árvore não é sólida); grama/flor/
    /// cogumelo/arbusto não têm colisão nenhuma. O próprio nó fica ancorado pela base (a
    /// Position que ExplorationArea atribui é o "pé" - mesma convenção dos sprites de
    /// Digimon), e é filho direto do SpawnsContainer com Y-sort ligado: comparar essa base
    /// com a posição (também na base) do Digimon é o que resolve sozinho ele desenhar atrás
    /// da árvore perto da copa e na frente perto do tronco.
    /// </summary>
    public partial class ForestDecoration : Node2D
    {
        private const string FolderPath = "res://Assets/Areas/Forest/";

        private static readonly Dictionary<ForestDecorationKind, string[]> Variants = new()
        {
            [ForestDecorationKind.Tree] = new[]
            {
                "forest_tree_oak.png", "forest_tree_palm.png", "forest_tree_pine.png",
            },
            [ForestDecorationKind.Grass] = new[] { "grass_small.png", "grass_big.png" },
            [ForestDecorationKind.Flower] = new[]
            {
                "flower_white.png", "flower_red.png", "flower_blue.png",
            },
            [ForestDecorationKind.Mushroom] = new[] { "forest_mushroom.png" },
            [ForestDecorationKind.Rock] = new[] { "forest_rock.png" },
            [ForestDecorationKind.Bush] = new[] { "forest_bush.png" },
        };

        // Só árvore/pedra têm corpo sólido - tamanho pequeno cobrindo só a base (tronco/
        // pedra em si), não o sprite inteiro, senão a copa da árvore "esbarraria" o Digimon
        // de longe, o que não faz sentido.
        private static readonly Dictionary<ForestDecorationKind, Vector2> CollisionSize = new()
        {
            [ForestDecorationKind.Tree] = new Vector2(16, 14),
            [ForestDecorationKind.Rock] = new Vector2(30, 14),
        };

        public void SetKind(ForestDecorationKind kind, Random random)
        {
            var variants = Variants[kind];
            string file = variants[random.Next(variants.Length)];

            var texture = GD.Load<Texture2D>(FolderPath + file);

            var sprite = new Sprite2D
            {
                Texture = texture,
                TextureFilter = TextureFilterEnum.Nearest,
                Offset = new Vector2(0, -texture.GetHeight() / 2f),
            };

            AddChild(sprite);

            if (CollisionSize.TryGetValue(kind, out var size))
                AddSolidBase(size);
        }

        private void AddSolidBase(Vector2 size)
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
                Shape = new RectangleShape2D { Size = size },
                Position = new Vector2(0, -size.Y / 2f),
            };

            body.AddChild(shape);
        }
    }
}
