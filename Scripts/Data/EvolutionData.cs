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
        public int Priority { get; set; }
        public float StatMultiplier { get; set; } = 1.2f;
    }
}
