using Godot;
using ProjetoDC.Scripts.Gameplay;
using ProjetoDC.Scripts.Managers;
using ProjetoDC.Scripts.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjetoDC.Scripts.Models.World
{
    public partial class CenterState
    {
        public int Bits { get; private set; }
        public int CapacityLimit { get; private set; }
        public int CapacityUsed { get; private set; }

        public List<DigimonInstance> Digimons { get; } = new();

        public CenterState() { 
            Bits = 0;
            CapacityLimit = 10;
            CapacityUsed = 0;
        }

        public void AddBits(int amount)
        {
            Bits += amount;
        }

        public bool SpendBits(int amount)
        {
            if(Bits < amount)
            {
                return false;
            }

            Bits -= amount;
            return true;
        }

        public void AddCapacity(int amount)
        {
            CapacityLimit += amount;
        }

        public bool SpendCapacity(int amount)
        {
            if(CapacityLimit - CapacityUsed < amount)
            {
                return false;
            }

            CapacityUsed += amount;
            return true;
        }

        public void AddDigimon(DigimonInstance digimon)
        {
            if(CapacityUsed + digimon.CapacityCost > CapacityLimit)
            {
                GD.Print("Não há capacidade suficiente para adicionar o Digimon.");
                return;
            }
            Digimons.Add(digimon);
            CapacityUsed += digimon.CapacityCost;
        }

        public void RemoveDigimon(DigimonInstance digimon)
        {
            if(Digimons.Remove(digimon))
            {
                CapacityUsed -= digimon.CapacityCost;
            }
            else
            {
                GD.Print("Digimon não encontrado no Center.");
            }
        }
    }
}
