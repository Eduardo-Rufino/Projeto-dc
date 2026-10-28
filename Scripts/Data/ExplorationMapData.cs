using System.Collections.Generic;

namespace ProjetoDC.Scripts.Data
{
    /// <summary>Posição fixa (só posição - o selvagem que aparece ali é sorteado de
    /// ExplorationMapData.WildPool toda vez que a área é aberta, ver ExplorationArea).</summary>
    public class WildSpawnPointData
    {
        public float X { get; set; }
        public float Y { get; set; }

        /// <summary>Mapa isométrico (ExplorationMapData.IsoMapScene): id do spot do mapa onde o
        /// selvagem aparece - no lugar de X/Y. Ver ExplorationArea.ResolveIsoPosition.</summary>
        public string Spot { get; set; }

        /// <summary>Mapa isométrico: bloco [gx, gy] - alternativa ao Spot.</summary>
        public List<int> Tile { get; set; }
    }

    public class NpcSpawnData
    {
        public int NpcId { get; set; }
        public float X { get; set; }
        public float Y { get; set; }

        /// <summary>Se true, ExplorationArea planta um cerco fixo de decoração (árvore/arbusto)
        /// ao redor desse NPC ao invés de só evitar decoração perto dele - ver
        /// ExplorationArea.SpawnConcealmentRings. A abertura do cerco aponta pro
        /// WildSpawnPoint mais próximo (o "guarda" bloqueando o único jeito de entrar), ou pro
        /// spawn do explorador se não houver nenhum perto.</summary>
        public bool Hidden { get; set; }

        /// <summary>Mapa isométrico: spot ou bloco [gx, gy] do NPC (ver WildSpawnPointData.Spot).
        /// Hidden não vale lá - o próprio mapa já esconde os cantos.</summary>
        public string Spot { get; set; }
        public List<int> Tile { get; set; }
    }

    public class ItemPickupData
    {
        /// <summary>Único dentro do mapa - usado em CenterState.CollectedItemPickupIds pra
        /// lembrar que esse pickup específico já foi coletado (não respawna).</summary>
        public int UniqueId { get; set; }

        public int ItemId { get; set; }
        public int Quantity { get; set; } = 1;
        public float X { get; set; }
        public float Y { get; set; }

        /// <summary>Mapa isométrico: spot ou bloco [gx, gy] (ver WildSpawnPointData.Spot).</summary>
        public string Spot { get; set; }
        public List<int> Tile { get; set; }

        /// <summary>Mapa isométrico: sprite do kit do mapa no lugar do ícone do item (ex.:
        /// "chest" pra baú, "item_orb" pra item solto). Vazio = ícone do item, como nos
        /// mapas antigos.</summary>
        public string KitSprite { get; set; }
    }

    /// <summary>
    /// Passagem de um mapa isométrico pra outra área de exploração (a mina leva à caverna, o
    /// barqueiro ao pântano). Interagir com a origem abre a área de destino com o mesmo
    /// Digimon; de lá, o botão de voltar traz o jogador de novo pra ReturnTile, na frente da
    /// passagem. Ver ExplorationArea / GameManager.TravelExploration.
    /// </summary>
    public class ExplorationTransitionData
    {
        public string Id { get; set; }

        /// <summary>Origem: a mina do mapa (MapLoader.MineNode, aparece com o bloqueio dela).</summary>
        public bool Mine { get; set; }

        /// <summary>Origem: um NPC do mapa - interagir com ele pergunta se quer ir, em vez do diálogo.</summary>
        public int? NpcId { get; set; }

        /// <summary>Origem: um spot do mapa isométrico onde o jogador entra andando ("walk_into"
        /// do JSON - as bocas da caverna). Pisar no bloco do spot pergunta se quer ir; clicar no
        /// objeto de mesmo id do mapa leva o explorador até lá.</summary>
        public string Spot { get; set; }

        /// <summary>Área de destino (Data/Maps). 0 = destino ainda não existe: a passagem só
        /// mostra ComingSoonText.</summary>
        public int TargetMapId { get; set; }

        /// <summary>Mensagem da passagem sem destino (TargetMapId 0), ex.: "em breve".</summary>
        public string ComingSoonText { get; set; } = "";

        /// <summary>Onde o jogador chega no destino, se o destino for isométrico (spot ou
        /// bloco [gx, gy]); vazio = o spawn normal do destino.</summary>
        public string ArrivalSpot { get; set; }
        public List<int> ArrivalTile { get; set; }

        /// <summary>Quest que libera a passagem (a mesma que libera a área de destino na lista).</summary>
        public int? RequiredQuestId { get; set; }

        /// <summary>Bloco [gx, gy] deste mapa onde o jogador reaparece ao voltar do destino.</summary>
        public List<int> ReturnTile { get; set; }

        /// <summary>Blocos [gx, gy] que ficam sólidos enquanto a passagem está aberta (o morro
        /// da mina: o desenho dela cobre blocos que o mapa deixa andar).</summary>
        public List<List<int>> SolidTiles { get; set; } = new();

        /// <summary>Plaquinha de nome sobre a origem (a mina), igual à dos NPCs.</summary>
        public string Name { get; set; } = "";

        public string ConfirmText { get; set; } = "";
    }

    /// <summary>
    /// Objeto de um mapa isométrico que o jogador acende/liga (switchable do JSON com
    /// "switch_on": "interact:..." - lanternas apagadas, caixas de junção). O estado fica em
    /// CenterState.ExplorationSwitchesOn; as quests SwitchOn contam esses ids.
    /// </summary>
    public class ExplorationSwitchData
    {
        /// <summary>Id do objeto no JSON do mapa (ex.: "juncao_1").</summary>
        public string Id { get; set; }

        /// <summary>Nome mostrado nas mensagens (ex.: "Caixa de junção").</summary>
        public string Name { get; set; } = "";

        /// <summary>Só acende com a lanterna (GameManager.HasLantern) ou explorando com um
        /// Digimon do elemento fogo.</summary>
        public bool NeedsFire { get; set; }

        /// <summary>Mensagem ao acender/ligar.</summary>
        public string OnText { get; set; } = "";

        /// <summary>Mensagem quando falta fogo (NeedsFire).</summary>
        public string NoFireText { get; set; } = "";
    }

    /// <summary>
    /// Dado estático de uma área de exploração (carregado de Data/Maps/*.json pelo
    /// DatabaseManager). Os selvagens não são fixos: WildSpawnPoints só marca onde eles
    /// aparecem - o Id sorteado vem de WildPool toda vez que a área é aberta (ver
    /// ExplorationArea._Ready).
    /// </summary>
    public class ExplorationMapData
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int RecommendedLevel { get; set; }

        /// <summary>Tema visual da área (ver AreaThemeData / Data/AreaThemes/) - "forest" por
        /// padrão, o que mantém os mapas antigos sem esse campo exatamente como eram.</summary>
        public string Theme { get; set; } = "forest";

        /// <summary>Se preenchido, a área só abre depois que o jogador concluir essa quest (id em
        /// Data/Quests) - é como uma área nova é liberada pela progressão (ver
        /// GameManager.IsExplorationMapUnlocked). Null = sempre disponível.</summary>
        public int? RequiredQuestId { get; set; }

        public List<int> WildPool { get; set; } = new();
        public int WildLevelMin { get; set; } = 1;
        public int WildLevelMax { get; set; } = 1;

        /// <summary>Cena de mapa isométrico (MapLoader, ver Assets/Areas/Forest_new/Floresta.tscn).
        /// Preenchido, a área usa esse mapa no lugar do chão/decoração do tema, e os pontos de
        /// selvagem/NPC/item passam a ser spots ou blocos do mapa (campos Spot/Tile).</summary>
        public string IsoMapScene { get; set; }

        /// <summary>Mapa isométrico: spot onde o explorador começa.</summary>
        public string ExplorerSpawnSpot { get; set; } = "spawn";

        /// <summary>Mapa isométrico: id do bloqueio do mapa -> quest (Data/Quests) que o libera.
        /// O estado vem de CenterState.CompletedQuestIds, então já fica salvo junto com a quest.</summary>
        public Dictionary<string, int> BlockerQuests { get; set; } = new();

        public List<ExplorationTransitionData> Transitions { get; set; } = new();

        /// <summary>Mapa isométrico: id das quests no JSON do mapa ("q_energia") -> Id em
        /// Data/Quests. É o que liga as regras do mapa ("after:q_energia" das luzes,
        /// "quest:q_energia" dos terminais) às quests do jogo.</summary>
        public Dictionary<string, int> MapQuestIds { get; set; } = new();

        /// <summary>Mapa isométrico: objetos que o jogador acende/liga (ver ExplorationSwitchData).</summary>
        public List<ExplorationSwitchData> Switches { get; set; } = new();

        public List<WildSpawnPointData> WildSpawnPoints { get; set; } = new();
        public List<NpcSpawnData> Npcs { get; set; } = new();
        public List<ItemPickupData> ItemPickups { get; set; } = new();
    }
}
