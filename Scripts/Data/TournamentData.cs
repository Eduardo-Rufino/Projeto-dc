using System.Collections.Generic;

namespace ProjetoDC.Scripts.Data
{
    /// <summary>
    /// Dado estático de um campeonato (carregado de Data/Tournaments/*.json). Diferente da
    /// batalha livre (EnemyGenerator, calibrada pelo poder do time do jogador), o time
    /// inimigo de um campeonato é sempre o mesmo, definido em Opponents - o formato (1x1 ou
    /// 3x3) é implícito pela quantidade de oponentes.
    /// </summary>
    public class TournamentData
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        /// <summary>Nível sugerido pro jogador entrar - só informativo, não bloqueia a
        /// inscrição.</summary>
        public int RecommendedLevel { get; set; }

        public List<TournamentOpponentData> Opponents { get; set; } = new();

        /// <summary>Quanto de capacidade o Center ganha ao vencer esse campeonato pela
        /// primeira vez (ver GameManager.ApplyTeamBattleReward / CenterState.ClearedTournamentIds).
        /// Nem todo campeonato precisa dar capacidade - pode ser 0 e usar BitsReward em vez disso.</summary>
        public int CapacityReward { get; set; }

        /// <summary>Bônus de Bits concedido só na primeira vitória desse campeonato, além dos
        /// Bits normais da batalha (ver GameManager.ApplyTeamBattleReward). Alternativa à
        /// capacidade pra campeonatos que não devem dar progressão de espaço.</summary>
        public int BitsReward { get; set; }
    }
}
