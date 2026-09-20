using System.Collections.Generic;

namespace ProjetoDC.Scripts.Data
{
    /// <summary>Caixa sólida de uma decoração (ver AreaDecoration): tamanho e centro em pixels
    /// locais do nó da decoração - origem = base-centro da imagem, x pra direita, y pra baixo
    /// (logo CenterY é negativo: a caixa fica acima da borda inferior da imagem, onde está o
    /// pé do objeto). O nó ainda é escalado ao ser plantado, e leva a caixa junto.</summary>
    public class SolidBaseData
    {
        public float Width { get; set; }
        public float Height { get; set; }
        public float CenterX { get; set; }
        public float CenterY { get; set; }
    }

    /// <summary>Uma variação visual de uma decoração: o arquivo (dentro de
    /// AreaThemeData.Folder) e, se bloqueia passagem, sua caixa sólida. Sem Solid = puramente
    /// decorativo (grama, flor, poça...).</summary>
    public class DecorationVariantData
    {
        public string File { get; set; }
        public SolidBaseData Solid { get; set; }

        /// <summary>Caixas sólidas adicionais, pra objetos com mais de um pé separado por um
        /// vão passável (ex.: o arco do cânion, com uma perna de cada lado).</summary>
        public List<SolidBaseData> ExtraSolids { get; set; } = new();
    }

    /// <summary>Bioma dentro de uma área: centro em fração (0..1) do retângulo de Boundary (não
    /// pixel fixo, pra funcionar em mapas de tamanhos diferentes) e o mix de tipos de
    /// decoração sorteado ali - tipo repetido = mais chance.</summary>
    public class DecorationZoneData
    {
        public float CenterX { get; set; }
        public float CenterY { get; set; }
        public float Radius { get; set; }
        public List<string> Kinds { get; set; } = new();
    }

    /// <summary>
    /// "Tema" visual de uma área de exploração (Data/AreaThemes/*.json): que sprites de cenário
    /// usa, em que proporção e onde (zonas). Um mapa escolhe o tema pelo campo
    /// ExplorationMapData.Theme - assim uma área nova (caverna, deserto...) é só dado, sem mexer
    /// no código de ExplorationArea.
    ///
    /// Os "tipos" (Kinds) são nomes livres definidos por cada tema (ex.: "Tree" na floresta,
    /// "Stalagmite" na caverna) - só precisam existir em Variants.
    /// </summary>
    public class AreaThemeData
    {
        /// <summary>Identificador do tema (minúsculo, ex.: "forest"), usado por
        /// ExplorationMapData.Theme.</summary>
        public string Id { get; set; }

        /// <summary>Pasta dos sprites, com barra no final (ex.: res://Assets/Areas/Forest/).</summary>
        public string Folder { get; set; }

        /// <summary>Textura do chão (arquivo dentro de <see cref="Folder"/>, ex.:
        /// ground_forest.png) - ladrilhada (repetida, sem suavização) por todo o polígono do
        /// Boundary, no tamanho original da imagem (os sprites de cenário já são 3x, então a
        /// textura também deve ser). Tem prioridade sobre <see cref="GroundColor"/>, que só vale
        /// se ela estiver vazia ou o arquivo não existir.</summary>
        public string GroundTexture { get; set; } = "";

        /// <summary>Cor de fundo (fora do mapa) e do chão (polígono do Boundary), em HTML
        /// (#rrggbb). Vazio = mantém a cor padrão da cena ExplorationArea.tscn (a da floresta).
        /// A cor do chão é só o plano B de <see cref="GroundTexture"/>.</summary>
        public string BackgroundColor { get; set; } = "";
        public string GroundColor { get; set; } = "";

        /// <summary>Quantos elementos de cenário espalhados pelo mapa (fora o cerco dos NPCs
        /// escondidos).</summary>
        public int DecorationCount { get; set; } = 210;

        /// <summary>Faixa do escalonamento aleatório de cada decoração.</summary>
        public float ScaleMin { get; set; } = 0.85f;
        public float ScaleMax { get; set; } = 1.25f;

        /// <summary>Orla (praia): uma linha reta de costa que separa a terra (chão =
        /// <see cref="GroundTexture"/>, por cima do Boundary inteiro) do mar. Em <see cref="ShoreLine"/>
        /// (fração 0..1 da altura do Boundary) começa uma faixa de <see cref="ShoreTexture"/> -
        /// ladrilhada só na horizontal, com a areia em cima, a espuma da onda no meio e a água
        /// embaixo - e dali até o fim do mapa vem <see cref="SeaTexture"/>. As três texturas
        /// emendam entre si, então a água nunca aparece sem uma transição ligando-a à areia.
        /// Vazio = tema sem mar (floresta, caverna...).</summary>
        public string ShoreTexture { get; set; } = "";
        public string SeaTexture { get; set; } = "";
        public float ShoreLine { get; set; } = 0f;

        /// <summary>Quantos pixels abaixo do início da faixa da orla fica o limite de caminhada
        /// (passou disso já é onda/água: o explorador não anda, e não nascem decorações de
        /// terra, selvagens, NPCs nem itens).</summary>
        public float WalkLimitOffset { get; set; } = 56f;

        /// <summary>Tipos plantados dentro do mar (só visual: coral, barco...) e quantos.</summary>
        public List<string> SeaKinds { get; set; } = new();
        public int SeaDecorationCount { get; set; } = 0;

        public bool HasShoreline => !string.IsNullOrEmpty(ShoreTexture) && !string.IsNullOrEmpty(SeaTexture);

        /// <summary>Y (mundo) além do qual não se anda, ou null se o tema não tem mar.</summary>
        public float? GetWalkLimitY(float boundaryTop, float boundaryHeight)
            => HasShoreline ? boundaryTop + ShoreLine * boundaryHeight + WalkLimitOffset : null;

        /// <summary>Tipo -> variações possíveis (uma é sorteada por decoração).</summary>
        public Dictionary<string, List<DecorationVariantData>> Variants { get; set; } = new();

        /// <summary>Mix padrão (fora de toda zona) - trilha/transição entre os biomas.</summary>
        public List<string> DefaultKinds { get; set; } = new();

        public List<DecorationZoneData> Zones { get; set; } = new();

        /// <summary>Tipos que formam o cerco ao redor de um NPC marcado Hidden (ver
        /// ExplorationArea.SpawnConcealmentRings) - pelo menos um deles deveria ser sólido pra
        /// o cerco bloquear de verdade.</summary>
        public List<string> ConcealmentKinds { get; set; } = new();
    }
}
