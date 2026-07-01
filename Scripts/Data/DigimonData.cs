using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProjetoDC.Enums;

namespace ProjetoDC.Scripts.Data
{
    public class DigimonData
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public DigimonStage Stage { get; set; }
        public RoleType Role { get; set; }
        public DigimonAttribute Attribute { get; set; }
        public DigimonElement Element { get; set; }
        public EggType EggType { get; set; }
        public BaseStats BaseStats { get; set; } = new();
    }
}
