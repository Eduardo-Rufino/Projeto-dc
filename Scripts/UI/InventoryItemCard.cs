using Godot;
using ProjetoDC.Scripts.Data;

namespace ProjetoDC.Scripts.UI
{
    /// <summary>Um slot do grid de inventário: só ícone e quantidade por padrão - nome e
    /// descrição aparecem no tooltip nativo do Godot ao passar o mouse por cima (ver
    /// Control.TooltipText), pra não poluir o grid com texto o tempo todo.</summary>
    public partial class InventoryItemCard : PanelContainer
    {
        private TextureRect _icon;
        private Label _quantityLabel;

        public override void _Ready()
        {
            _icon = GetNode<TextureRect>("Pad/VBoxContainer/Icon");
            _quantityLabel = GetNode<Label>("Pad/VBoxContainer/QuantityLabel");
        }

        public void SetItem(ItemData item, int quantity)
        {
            _quantityLabel.Text = $"x{quantity}";

            TooltipText = string.IsNullOrEmpty(item.Description)
                ? item.Name
                : $"{item.Name}\n{item.Description}";

            // Nem todo item tem arte ainda (ex.: Remédio) - fica sem ícone em vez de quebrar.
            bool hasIcon = !string.IsNullOrEmpty(item.IconPath) && ResourceLoader.Exists(item.IconPath);

            if (!hasIcon && !string.IsNullOrEmpty(item.IconPath))
            {
                GD.PrintErr($"Ícone do item '{item.Name}' não encontrado: {item.IconPath}");
            }

            _icon.Texture = hasIcon ? GD.Load<Texture2D>(item.IconPath) : null;
        }
    }
}
