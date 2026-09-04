using Godot;
using ProjetoDC.Scripts.Data;
using System;

namespace ProjetoDC.Scripts.Gameplay
{
    /// <summary>
    /// NPC numa área de exploração - clicar avisa quem estiver escutando (ExplorationArea, que
    /// abre a DialogueScreen). Não decide sozinho o que mostrar - só sinaliza o clique.
    /// </summary>
    public partial class ExplorationNpcVisual : Node2D
    {
        // Distância máxima do explorador pra um clique valer como "conversar" - clicar num
        // NPC do outro lado da tela não deve abrir diálogo à distância; o clique só "vazar"
        // pra ExplorationDigimon._UnhandledInput (que anda até o ponto clicado) até o jogador
        // chegar perto o bastante (ver OnClickAreaInputEvent).
        private const float InteractionRange = 140f;

        private TextureRect _portrait;
        private Label _nameLabel;
        private NpcData _npc;
        private ExplorationDigimon _explorerVisual;

        public event Action<NpcData> Clicked;

        public override void _Ready()
        {
            _portrait = GetNodeOrNull<TextureRect>("Portrait");
            _nameLabel = GetNodeOrNull<Label>("NamePanel/NameLabel");

            var clickArea = GetNode<Area2D>("ClickArea");
            clickArea.InputEvent += OnClickAreaInputEvent;
        }

        public void Initialize(NpcData npc, ExplorationDigimon explorerVisual)
        {
            _npc = npc;
            _explorerVisual = explorerVisual;

            if (_nameLabel != null)
                _nameLabel.Text = npc.Name;

            string portraitPath = $"res://Assets/Sprites/Digimon/{npc.Code}.png";

            if (_portrait != null && ResourceLoader.Exists(portraitPath))
                _portrait.Texture = GD.Load<Texture2D>(portraitPath);
        }

        private void OnClickAreaInputEvent(Node viewport, InputEvent @event, long shapeIdx)
        {
            if (@event is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
                return;

            if (_explorerVisual == null ||
                GlobalPosition.DistanceTo(_explorerVisual.GlobalPosition) > InteractionRange)
                return;

            GetViewport().SetInputAsHandled();

            Clicked?.Invoke(_npc);
        }
    }
}
