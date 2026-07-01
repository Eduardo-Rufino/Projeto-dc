using ProjetoDC.Scripts.Gameplay;
using ProjetoDC.Scripts.Models.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjetoDC.Scripts.Systems.Center
{
    public partial class CenterService
    {
        private CenterState _center;

        public CenterService(CenterState center) {
            _center = center;
        }

        public IEnumerable<DigimonInstance> GetDigimons()
            => _center.Digimons;

        public DigimonInstance GetDigimonById(int id)
            => _center.Digimons.FirstOrDefault(d => d.BaseData.Id == id);

        public List<DigimonInstance> GetAllDigimons()
        {
            return _center.Digimons;
        }

        public void AddDigimon(DigimonInstance digimon)
        {
            _center.AddDigimon(digimon);
        }

        public void RemoveDigimon(DigimonInstance digimon)
        {
            _center.RemoveDigimon(digimon);
        }

    }
}
