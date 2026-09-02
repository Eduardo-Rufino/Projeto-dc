using Godot;
using ProjetoDC.Enums;
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

        /// <summary>Todos os itens que o jogador possui (comida, remédio, e qualquer item
        /// futuro), por Id de ItemData - ver DatabaseManager.GetItem/GetAllItems.</summary>
        public List<InventoryEntry> Inventory { get; set; } = new();

        // Meat/Medicine viraram passthrough pro Inventory (ver ItemIds) - mantidos como
        // propriedades pra não precisar mexer em quem já lia/escrevia Save.Center.Meat/
        // Medicine direto (GameManager, ShopScreen, DigimonInstance, etc.). Saves antigos
        // (sem "Inventory" no JSON, só "Meat"/"Medicine" like antes) migram sozinhos: o
        // deserializador chama esses setters normalmente, que gravam no Inventory.
        public int Meat
        {
            get => GetItemQuantity(ItemIds.Meat);
            set => SetItemQuantity(ItemIds.Meat, value);
        }

        public int Medicine
        {
            get => GetItemQuantity(ItemIds.Medicine);
            set => SetItemQuantity(ItemIds.Medicine, value);
        }

        // Ovos reservam capacidade igual ao Digimon Baby que vão gerar (EggData.CapacityCost) -
        // senão o jogador podia comprar ovos sem limite e lotar o Center de uma vez só quando
        // todos chocassem.
        public int CapacityUsed => Digimons.Sum(d => d.CapacityCost) + Eggs.Sum(e => e.CapacityCost);

        public List<Food> Foods { get; set; } = new();
        public List<Poop> Poops { get; set; } = new();
        public List<DigimonInstance> Digimons { get; set; } = new();
        public List<EggData> Eggs { get; set; } = new();
        public List<CenterAreaData> BuiltAreas { get; set; } = new();

        /// <summary>Bases que o jogador já possui mas removeu do grid de volta pro inventário
        /// (ver Center.RemoveArea/Center.StartAreaPlacementFromInventory) - continuam
        /// existindo, só não ocupam nenhum hexágono agora. Cada entrada é uma base física
        /// independente (a mesma CenterAreaType pode aparecer mais de uma vez); colocar uma de
        /// volta no grid não custa Bits, já foi paga na compra original.</summary>
        public List<CenterAreaType> UnplacedAreas { get; set; } = new();

        /// <summary>IDs de TournamentData já vencidos pelo menos uma vez - a recompensa de
        /// capacidade só é concedida na primeira vitória (ver
        /// GameManager.ApplyTournamentCapacityRewardIfNeeded).</summary>
        public List<int> ClearedTournamentIds { get; set; } = new();

        public CenterState() {
            // Um save novo começa com um colchão mínimo de recursos - o suficiente pra não
            // deixar o jogador sem Bits/Carne/Remédio caso o ovo inicial choque um Digimon
            // fraco (ver EggSystem.CreateInitialEgg, que sorteia entre todos os Baby). Não é
            // mais o placeholder de debug que existia antes (50_000_000 Bits, 100 Carne, 50
            // Remédio) - valores bem menores, só pra cobrir o começo. Não mexe em
            // CapacityLimit aqui (não foi pedido).
            Bits = 500;
            CapacityLimit = 10;
            Meat = 10;
            Medicine = 2;

            // Base inicial: além da área Neutra fixa em (0,0) (já vem em Center.tscn, não
            // entra em BuiltAreas - ver CenterAreaData), o save novo já nasce com Hospital e
            // Treinamento construídos nos hexágonos vizinhos (ver Center.RestoreBuiltAreas).
            // Só se aplica a um save realmente novo: ao carregar um save existente o
            // deserializador substitui BuiltAreas inteiro pelo valor gravado no JSON.
            BuiltAreas.Add(new CenterAreaData { GridX = 1, GridY = 0, AreaType = CenterAreaType.Hospital });
            BuiltAreas.Add(new CenterAreaData { GridX = -1, GridY = 0, AreaType = CenterAreaType.Training });
        }

        public int GetItemQuantity(int itemId)
        {
            return Inventory.FirstOrDefault(e => e.ItemId == itemId)?.Quantity ?? 0;
        }

        public void SetItemQuantity(int itemId, int quantity)
        {
            var entry = Inventory.FirstOrDefault(e => e.ItemId == itemId);

            if (entry == null)
            {
                Inventory.Add(new InventoryEntry { ItemId = itemId, Quantity = quantity });
                return;
            }

            entry.Quantity = quantity;
        }

        public void AddItemQuantity(int itemId, int amount)
        {
            SetItemQuantity(itemId, GetItemQuantity(itemId) + amount);
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
