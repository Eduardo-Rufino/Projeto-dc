using System.Collections.Generic;

namespace ProjetoDC.Scripts.Data
{
    /// <summary>Posição fixa (só posição - o selvagem que aparece ali é sorteado de
    /// ExplorationMapData.WildPool toda vez que a área é aberta, ver ExplorationArea).</summary>
    public class WildSpawnPointData
    {
        public float X { get; set; }
        public float Y { get; set; }
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

        public List<int> WildPool { get; set; } = new();
        public int WildLevelMin { get; set; } = 1;
        public int WildLevelMax { get; set; } = 1;

        public List<WildSpawnPointData> WildSpawnPoints { get; set; } = new();
        public List<NpcSpawnData> Npcs { get; set; } = new();
        public List<ItemPickupData> ItemPickups { get; set; } = new();
    }
}
