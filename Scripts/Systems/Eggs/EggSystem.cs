using Godot;
using ProjetoDC.Scripts.Data;
using ProjetoDC.Scripts.Gameplay;
using ProjetoDC.Scripts.Managers;
using ProjetoDC.Scripts.Systems.Center;
using System;
using System.Linq;

namespace ProjetoDC.Scripts.Systems.Eggs
{
    public class EggSystem
    {
        /// <summary>Disparado quando um ovo termina de chocar, com o ovo e o Digimon recém-nascido.</summary>
        public event Action<EggData, DigimonInstance> EggHatched;

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

            var digimonData = db.GetDigimon(25);

            if (digimonData == null)
            {
                GD.PrintErr("Digimon ID 25 não existe no DB");
                return;
            }

            var egg = new EggData
            {
                BaseDigimonId = digimonData.Id,
                IncubationTime = 6,
                IncubationProgress = 0,
                IsReady = false,
                IsStarterEgg = true
            };

            center.AddEgg(egg);

            GD.Print($"Novo ovo criado: {digimonData.Name}");
        }

        public bool CreateEgg(int digimonId, CenterService center)
        {
            var digimonData = DatabaseManager.Instance.GetDigimon(digimonId);

            if (digimonData == null)
            {
                GD.PrintErr($"Digimon {digimonId} não encontrado.");
                return false;
            }

            var egg = new EggData
            {
                BaseDigimonId = digimonData.Id,
                IncubationTime = 3,
                IncubationProgress = 0,
                IsReady = false,
                IsStarterEgg = false
            };

            center.AddEgg(egg);

            GD.Print($"Ovo comprado: {digimonData.Name}");

            return true;
        }

        public void AdvanceHour(CenterService center)
        {
            foreach (var egg in center.GetEggs().ToList())
            {
                // Apenas o ovo inicial usa horas
                if (!egg.IsStarterEgg)
                    continue;


                if (!egg.IsReady)
                {
                    egg.IncubationProgress++;

                    GD.Print(
                        $"Ovo inicial {egg.BaseDigimonId}: " +
                        $"{egg.IncubationProgress}/{egg.IncubationTime} horas"
                    );


                    if (egg.IncubationProgress >= egg.IncubationTime)
                    {
                        egg.IsReady = true;

                        GD.Print(
                            $"Ovo inicial {egg.BaseDigimonId} terminou a incubação."
                        );
                    }
                }


                if (egg.IsReady)
                {
                    TryHatchEgg(center, egg);
                }
            }
        }


        public void AdvanceDay(CenterService center)
        {
            foreach (var egg in center.GetEggs().ToList())
            {
                // Ovo inicial usa horas
                if (egg.IsStarterEgg)
                    continue;


                if (!egg.IsReady)
                {
                    egg.IncubationProgress++;

                    GD.Print(
                        $"Ovo {egg.BaseDigimonId}: " +
                        $"{egg.IncubationProgress}/{egg.IncubationTime} dias"
                    );


                    if (egg.IncubationProgress >= egg.IncubationTime)
                    {
                        egg.IsReady = true;

                        GD.Print(
                            $"Ovo {egg.BaseDigimonId} terminou a incubação."
                        );
                    }
                }


                // Tenta nascer mesmo se já estava pronto de dias anteriores
                TryHatchEgg(center, egg);
            }
        }


        private void TryHatchEgg(CenterService center, EggData egg)
        {
            var digimonData = DatabaseManager.Instance.GetDigimon(
                egg.BaseDigimonId
            );


            if (digimonData == null)
            {
                GD.PrintErr(
                    $"Digimon {egg.BaseDigimonId} não encontrado."
                );

                return;
            }


            var digimon = new DigimonInstance(digimonData);


            if (!center.CanAddDigimon(digimon))
            {
                GD.Print(
                    $"{digimon.BaseData.Name} está pronto para nascer, " +
                    "mas não há capacidade."
                );

                return;
            }


            HatchEgg(center, egg, digimon);
        }


        private void HatchEgg(
            CenterService center,
            EggData egg,
            DigimonInstance digimon)
        {
            center.RemoveEgg(egg);

            center.AddDigimon(digimon);


            if (GameManager.Instance.PlayerDigimon == null)
            {
                GameManager.Instance.SetPlayerDigimonInstance(digimon);
            }


            GD.Print($"{digimon.BaseData.Name} nasceu!");
            GD.Print($"Nasceu {digimon.BaseData.Name} - Hash: {digimon.GetHashCode()}");

            EggHatched?.Invoke(egg, digimon);

            GameManager.Instance.SaveSystem.SaveGame(
                GameManager.Instance.Save
            );
        }
    }
}