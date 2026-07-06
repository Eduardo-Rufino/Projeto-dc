using Godot;
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
            var db = DatabaseManager.Instance;

            if (db == null)
            {
                GD.PrintErr("DB não inicializado");
                return;
            }

            var digimonData = db.GetDigimon(1);

            if (digimonData == null)
            {
                GD.PrintErr("Digimon ID 1 não existe no DB");
                return;
            }

            var egg = new EggData
            {
                BaseDigimonId = digimonData.Id,
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
