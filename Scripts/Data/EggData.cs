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

        public float PositionX { get; set; }

        public float PositionY { get; set; }
    }
}
