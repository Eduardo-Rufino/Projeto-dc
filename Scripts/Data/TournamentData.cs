using ProjetoDC.Enums;
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

        // ---- Agendamento no calendário (ver CalendarSystem.IsOpen/DaysUntilOpen). Todos opcionais: sem
        // eles o campeonato é Always, como antes do calendário. Dias do ano vão de 1 a
        // CalendarData.DaysPerYear. Por enquanto só dado - nenhum fluxo do jogo consulta isso ainda.

        public TournamentScheduleType ScheduleType { get; set; } = TournamentScheduleType.Always;

        /// <summary>Window: primeiro dia da janela. Fixed: dia em que abre.</summary>
        public int StartDayOfYear { get; set; }

        /// <summary>Window: último dia da janela (inclusive).</summary>
        public int EndDayOfYear { get; set; }

        /// <summary>Recurring: de quantos em quantos dias corridos abre.</summary>
        public int IntervalDays { get; set; }

        /// <summary>Recurring: deslocamento em dias corridos da primeira abertura (0 = abre no 1º dia do jogo).</summary>
        public int OffsetDays { get; set; }

        /// <summary>Recurring/Fixed: quantos dias fica aberto a cada abertura (mínimo 1).</summary>
        public int DurationDays { get; set; }

        /// <summary>Id do campeonato que precisa ter sido vencido antes (0 = sem trava). Vale com
        /// qualquer ScheduleType - ver CalendarSystem.IsUnlocked.</summary>
        public int UnlockAfterTournamentId { get; set; }

        /// <summary>Quantos dias antes da abertura o campeonato é anunciado.</summary>
        public int AnnounceDaysBefore { get; set; }

        // ---- Regras de inscrição (ver TournamentRules). Lista vazia = sem restrição naquele critério;
        // sem os campos no JSON o campeonato aceita qualquer Digimon, como antes.

        /// <summary>Estágios que podem entrar (ex.: só Rookie).</summary>
        public List<DigimonStage> AllowedStages { get; set; } = new();

        /// <summary>Atributos que podem entrar (ex.: só Virus).</summary>
        public List<DigimonAttribute> AllowedAttributes { get; set; } = new();
    }
}
