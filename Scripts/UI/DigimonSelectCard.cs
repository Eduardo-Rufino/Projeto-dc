using Godot;
using ProjetoDC.Scripts.Gameplay;
using System;

namespace ProjetoDC.Scripts.UI
{
    /// <summary>
    /// Card selecionável de um Digimon do roster, usado na TeamSelectionScreen pra montar
    /// o time de batalha 3x3. Generaliza os blocos fixos que existiam na antiga EnemySelectionScreen.
    /// </summary>
    public partial class DigimonSelectCard : Button
    {
        private const string PortraitFolder = "res://Assets/Sprites/Digimon/";

        private TextureRect _portrait;
        private Label _nameLabel;
        private Label _levelLabel;
        private Label _roleLabel;

        public DigimonInstance Digimon { get; private set; }

        public event Action<DigimonSelectCard> ToggledSelection;

        public override void _Ready()
        {
            _portrait = GetNode<TextureRect>("VBoxContainer/Portrait");
            _nameLabel = GetNode<Label>("VBoxContainer/NameLabel");
            _levelLabel = GetNode<Label>("VBoxContainer/LevelLabel");
            _roleLabel = GetNode<Label>("VBoxContainer/RoleLabel");

            Toggled += _ => ToggledSelection?.Invoke(this);
        }

        public void SetDigimon(DigimonInstance digimon)
        {
            Digimon = digimon;

            _nameLabel.Text = digimon.BaseData.Name;
            _levelLabel.Text = $"Lv {digimon.Level}";
            _roleLabel.Text = digimon.BaseData.Role.ToString();

            string path = $"{PortraitFolder}{digimon.BaseData.Code}.png";

            if (ResourceLoader.Exists(path))
                _portrait.Texture = GD.Load<Texture2D>(path);
        }
    }
}
