using Godot;
using ProjetoDC.Enums;
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

        /// <summary>Disparado quando um novo ovo é criado (inicial ou comprado), pra quem
        /// desenha o Center poder posicionar o visual (EggWorld) na hora - sem isso, o ovo
        /// só aparece depois que a cena recarrega e reconstrói a partir do save.</summary>
        public event Action<EggData> EggCreated;

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

            // Qualquer Digimon Baby (estágio inicial) pode nascer do ovo inicial - mesma
            // lista/critério usado por GameManager.BuyEgg pros ovos comprados na loja.
            var babyDigimons = db.GetAllDigimons()
                .Where(d => d.Stage == DigimonStage.Baby)
                .ToList();

            if (babyDigimons.Count == 0)
            {
                GD.PrintErr("Nenhum Digimon Baby disponível no DB");
                return;
            }

            var digimonData = babyDigimons[GD.RandRange(0, babyDigimons.Count - 1)];

            var egg = new EggData
            {
                BaseDigimonId = digimonData.Id,
                // Bem mais rápido que os 3 dias de um ovo comprado (EggSystem.CreateEgg) -
                // é o primeiro Digimon do jogador, ele deve poder começar a jogar logo.
                IncubationTime = 1,
                IncubationProgress = 0,
                IsReady = false,
                IsStarterEgg = true,
                CapacityCost = DigimonInstance.GetCapacityCostForStage(digimonData.Stage)
            };

            center.AddEgg(egg);

            EggCreated?.Invoke(egg);

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
                IsStarterEgg = false,
                CapacityCost = DigimonInstance.GetCapacityCostForStage(digimonData.Stage)
            };

            center.AddEgg(egg);

            // (Não dispara EggCreated aqui: o fluxo de compra já tem seu próprio evento -
            // ShopScreen.EggPurchased, escutado por Center.OnEggPurchased - disparar os dois
            // pro mesmo ovo duplicaria o EggWorld visual.)

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


            // O próprio ovo já reserva CapacityCost (ver EggData.CapacityCost) - sem
            // descontar isso, chocar exigiria capacidade em dobro (a do ovo que já existe
            // + a do Digimon novo) pra uma troca que, na prática, não deveria pedir nada
            // além do que o ovo já estava ocupando.
            if (!center.CanAddDigimon(digimon, freeingEgg: egg))
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