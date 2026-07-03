using ProjetoDC.Scripts.Data;
using ProjetoDC.Scripts.Gameplay;
using ProjetoDC.Scripts.Managers;
using ProjetoDC.Scripts.Systems.Center;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjetoDC.Scripts.Systems.Eggs
{
    public class EggSystem
    {
        public EggSystem() { }


        public void SelectEgg()
        {

        }

        public void CreateInitialEgg(CenterService center)
        {
            var egg = new EggData
            {
                BaseDigimonId = 1,
                IsReady = true,
            };

            HatchEgg(egg, center);
        }

        public void HatchEgg(EggData egg, CenterService center)
        {
            var data = DatabaseManager.Instance.GetDigimon(egg.BaseDigimonId);

            var digimon = new DigimonInstance(data);

            GameManager.Instance.CenterService.AddDigimon(digimon);
        }
    }
}
