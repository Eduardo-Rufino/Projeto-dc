using ProjetoDC.Scripts.Data;
using ProjetoDC.Scripts.Models.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjetoDC.Scripts.Save
{
    public class SaveData
    {
        public WorldState World { get; set; }

        public CenterState Center { get; set; }
        public List<EggData> Eggs { get; set; }

        public SaveData()
        {
            World = new WorldState();
            Center = new CenterState();
        }
    }
}
