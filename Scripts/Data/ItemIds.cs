namespace ProjetoDC.Scripts.Data
{
    /// <summary>
    /// IDs fixos dos itens que já existiam como campos próprios antes do inventário
    /// (CenterState.Meat/Medicine) - viraram passthrough pro Inventory usando esses IDs,
    /// pra não precisar mexer em quem já lia/escrevia Save.Center.Meat/Medicine direto
    /// (GameManager, ShopScreen, DigimonInstance, etc.). Precisam bater com os "Id" nos
    /// JSONs correspondentes em Data/Itens/.
    /// </summary>
    public static class ItemIds
    {
        public const int Meat = 1;
        public const int Medicine = 2;
    }
}
