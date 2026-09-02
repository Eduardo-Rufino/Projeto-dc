namespace ProjetoDC.Scripts.Data
{
    /// <summary>
    /// Dado estático de um item de inventário (carregado de Data/Itens/*.json pelo
    /// DatabaseManager). A quantidade que o jogador possui não fica aqui - fica em
    /// CenterState.Inventory (ver InventoryEntry), referenciando o Id.
    /// </summary>
    public class ItemData
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        /// <summary>Caminho res:// do ícone do item. Pode ficar vazio se ainda não existir
        /// arte pra esse item - quem desenha o inventário trata esse caso.</summary>
        public string IconPath { get; set; } = "";
    }
}
