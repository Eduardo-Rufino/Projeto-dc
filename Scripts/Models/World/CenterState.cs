using Godot;
using ProjetoDC.Scripts.Data;
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
        public int Bits { get; set; }
        public int CapacityLimit { get; set; }
        public int Meat { get; set; } = 100;
        public int Medicine { get; set; } = 50;
        public int CapacityUsed => Digimons.Sum(d => d.CapacityCost);

        public List<Food> Foods { get; set; } = new();
        public List<Poop> Poops { get; set; } = new();
        public List<DigimonInstance> Digimons { get; set; } = new();
        public List<EggData> Eggs { get; set; } = new();
        public List<CenterAreaData> BuiltAreas { get; set; } = new();

        public CenterState() { 
            Bits = 50000000;
            CapacityLimit = 10;
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
        }

        public void RemoveDigimon(DigimonInstance digimon)
        {
            if(!Digimons.Remove(digimon))
            {
                GD.Print("Digimon não encontrado no Center.");
            }
        }

        public void AddEgg(EggData egg)
        {
            Eggs.Add(egg);
        }

        public void RemoveEgg(EggData egg)
        {
            if (!Eggs.Remove(egg))
            {
                GD.Print("Ovo não encontrado no Center.");
            }
        }
    }
}
