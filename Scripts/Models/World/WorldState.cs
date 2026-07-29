using Godot;
using ProjetoDC.Scripts.Managers;
using ProjetoDC.Scripts.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjetoDC.Scripts.Models.World
{
    public partial class WorldState
    {
        public int CurrentDay { get; set; } = 1;
        public int CurrentHour { get; set; }
        public int CurrentMinute { get; set; }

        public WorldState() { 
            
        }

        public void AdvanceDay(int days = 1)
        {
            CurrentDay += days;

            GD.Print($"Dia atual: {CurrentDay}");
        }
    }
}
