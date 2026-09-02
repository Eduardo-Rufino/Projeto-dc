using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjetoDC.Scripts.Data
{
    public class EggData
    {
        public int BaseDigimonId { get; set; }

        public int IncubationTime { get; set; }

        public int IncubationProgress { get; set; }

        public bool IsReady { get; set; }

        public bool IsStarterEgg { get; set; }

        /// <summary>Capacidade reservada no Center enquanto o ovo não choca - igual ao custo
        /// do Digimon Baby que vai nascer dele (ver DigimonInstance.GetCapacityCostForStage),
        /// calculado uma vez na criação (EggSystem) pra não depender do DB de novo depois.</summary>
        public int CapacityCost { get; set; }

        public float PositionX { get; set; }

        public float PositionY { get; set; }
    }
}
