using Godot;
using ProjetoDC.Scripts.Data;
using System;

namespace ProjetoDC.Scripts.UI
{
    /// <summary>Uma linha da lista de áreas de exploração: nome, nível recomendado e
    /// descrição - usado pela ExplorationListScreen.</summary>
    public partial class ExplorationMapCard : PanelContainer
    {
        private Label _nameLabel;
        private Label _levelLabel;
        private Label _descriptionLabel;
        private Button _enterButton;

        private ExplorationMapData _map;

        public event Action<ExplorationMapData> Entered;

        public override void _Ready()
        {
            _nameLabel = GetNode<Label>("Pad/VBoxContainer/HeaderRow/NameLabel");
            _levelLabel = GetNode<Label>("Pad/VBoxContainer/LevelLabel");
            _descriptionLabel = GetNode<Label>("Pad/VBoxContainer/DescriptionLabel");
            _enterButton = GetNode<Button>("Pad/VBoxContainer/FooterRow/EnterButton");

            _enterButton.Pressed += () => Entered?.Invoke(_map);
        }

        public void SetMap(ExplorationMapData map)
        {
            _map = map;

            _nameLabel.Text = map.Name;
            _levelLabel.Text = $"Nível recomendado: {map.RecommendedLevel}";
            _descriptionLabel.Text = map.Description;
        }
    }
}
