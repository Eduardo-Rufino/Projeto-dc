using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjetoDC.Scripts.Data
{
    public partial class EvolutionData
    {
        public int FromDigimonId { get; set; }
        public int ToDigimonId { get; set; }
        public int RequiredLevel { get; set; }
        public int RequiredAgeInDays { get; set; }
        public EvolutionRequirements RequiredStats { get; set; }
        public float StatMultiplier { get; set; } = 1.2f;

        /// <summary>Quantas batalhas (vitórias ou derrotas, qualquer uma conta - ver
        /// DigimonInstance.BattlesFought) esse Digimon precisa ter lutado pra evoluir por
        /// esse caminho. 0 = sem exigência. Existe pra evitar que o jogador vá só atrás do
        /// XP de consolação de derrotas propositais (GameManager.LossExperienceDivisor) sem
        /// nunca realmente batalhar de verdade.</summary>
        public int RequiredBattles { get; set; }

        /// <summary>Taxa de vitórias mínima (0 a 1 - ver DigimonInstance.WinRate) exigida
        /// pra essa evolução. 0 = sem exigência. Só faz sentido combinado com
        /// RequiredBattles > 0 (senão qualquer Digimon com 0 batalhas passaria, já que
        /// WinRate é 0 nesse caso e a checagem vira "0 >= 0").</summary>
        public float RequiredWinRate { get; set; }

        /// <summary>Disciplina mínima exigida (0-100, ver DigimonInstance.Discipline). 0 =
        /// sem exigência.</summary>
        public int MinDiscipline { get; set; }

        /// <summary>Disciplina máxima permitida (0-100). 100 = sem exigência (Disciplina
        /// nunca passa de 100) - use um valor baixo pra modelar evoluções "desobedientes"/
        /// malignas, que só acontecem com Disciplina baixa.</summary>
        public int MaxDiscipline { get; set; } = 100;

        /// <summary>Felicidade mínima exigida (0-100, ver DigimonInstance.Happiness). 0 =
        /// sem exigência - use um valor alto pra evoluções "queridas", que só acontecem com
        /// o Digimon feliz.</summary>
        public int MinHappiness { get; set; }

        /// <summary>Felicidade máxima permitida (0-100). 100 = sem exigência - use um valor
        /// baixo pra evoluções "indesejadas"/sombrias, que só acontecem com o Digimon
        /// infeliz.</summary>
        public int MaxHappiness { get; set; } = 100;
    }
}
