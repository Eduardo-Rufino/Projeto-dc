namespace ProjetoDC.Scripts.Data
{
    /// <summary>Quanto o jogador possui de um item (ItemData.Id) - parte do save,
    /// em CenterState.Inventory.</summary>
    public class InventoryEntry
    {
        public int ItemId { get; set; }
        public int Quantity { get; set; }
    }
}
