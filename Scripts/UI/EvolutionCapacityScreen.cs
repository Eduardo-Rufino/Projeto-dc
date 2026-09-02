using Godot;
using ProjetoDC.Scripts.Data;
using ProjetoDC.Scripts.Gameplay;
using ProjetoDC.Scripts.Managers;
using System.Collections.Generic;
using System.Linq;

namespace ProjetoDC.Scripts.UI
{
    /// <summary>
    /// Aviso mostrado quando um Digimon atende os requisitos de evolução mas o Center não
    /// tem capacidade livre pra nova forma (ver GameManager.EvolutionBlockedByCapacity). O
    /// jogador escolhe entre deletar Digimons do roster (liberando capacidade suficiente
    /// pra evoluir na hora) ou manter o Digimon como está - nesse caso ele continua sendo
    /// checado normalmente (treino, dia, batalha) e evolui sozinho assim que houver
    /// capacidade, sem precisar desse aviso de novo.
    /// </summary>
    public partial class EvolutionCapacityScreen : Control
    {
        private Label _titleLabel;
        private Label _capacityLabel;
        private GridContainer _grid;
        private Button _deleteButton;
        private Button _keepButton;

        private PackedScene _cardScene;

        private readonly List<DigimonSelectCard> _cards = new();
        private readonly List<DigimonInstance> _selected = new();

        private DigimonInstance _blockedDigimon;
        private int _deficit;

        private GameManager Game => GameManager.Instance;

        public override void _Ready()
        {
            // O Center pausa a árvore inteira enquanto essa tela está aberta (ver
            // HUD.SyncPauseWithBlockingScreens) - sem isso os botões parariam de responder.
            ProcessMode = ProcessModeEnum.Always;

            _titleLabel = GetNode<Label>("VBoxContainer/TitleLabel");
            _capacityLabel = GetNode<Label>("VBoxContainer/CapacityLabel");
            _grid = GetNode<GridContainer>("VBoxContainer/ScrollContainer/GridContainer");
            _deleteButton = GetNode<Button>("VBoxContainer/HBoxContainer/DeleteButton");
            _keepButton = GetNode<Button>("VBoxContainer/HBoxContainer/KeepButton");

            _cardScene = GD.Load<PackedScene>("res://Scenes/Battle/DigimonSelectCard.tscn");

            _deleteButton.Pressed += OnDeletePressed;
            _keepButton.Pressed += OnKeepPressed;
        }

        public void Open(DigimonInstance blockedDigimon, DigimonData targetForm, int deficit)
        {
            _blockedDigimon = blockedDigimon;
            _deficit = deficit;

            _titleLabel.Text =
                $"{blockedDigimon.BaseData.Name} está pronto para evoluir para " +
                $"{targetForm.Name}, mas o Center está sem capacidade.\n" +
                "Deseja obliterar um dos Digimons da base pra abrir espaço? Ou prefere " +
                "mantê-lo como está (ele evolui sozinho assim que houver capacidade)?";

            RefreshRoster();

            Visible = true;
        }

        private void RefreshRoster()
        {
            foreach (var card in _cards)
                card.QueueFree();

            _cards.Clear();
            _selected.Clear();

            // O próprio Digimon bloqueado não entra na lista - deletá-lo não faz sentido
            // (ele é quem ia evoluir).
            var roster = Game.CenterService.GetAllDigimons()
                .Where(d => d != _blockedDigimon)
                .ToList();

            foreach (var digimon in roster)
            {
                var card = _cardScene.Instantiate<DigimonSelectCard>();

                _grid.AddChild(card);

                card.SetDigimon(digimon);
                card.ToggledSelection += OnCardToggled;

                _cards.Add(card);
            }

            UpdateCapacityLabel();
        }

        private void OnCardToggled(DigimonSelectCard card)
        {
            if (card.ButtonPressed)
                _selected.Add(card.Digimon);
            else
                _selected.Remove(card.Digimon);

            UpdateCapacityLabel();
        }

        private void UpdateCapacityLabel()
        {
            int freed = _selected.Sum(d => d.CapacityCost);
            var center = Game.Save.Center;

            _capacityLabel.Text = _cards.Count == 0
                ? "Não há outros Digimons no Center pra deletar."
                : $"Capacidade selecionada pra liberar: {freed}/{_deficit}  " +
                  $"(Center: {center.CapacityUsed}/{center.CapacityLimit})";

            _deleteButton.Disabled = freed < _deficit;
        }

        private void OnDeletePressed()
        {
            foreach (var digimon in _selected)
                Game.DeleteDigimon(digimon);

            // Capacidade liberada - a mesma checagem de sempre agora deve passar.
            Game.TryToEvolve(_blockedDigimon);

            Game.SaveGame();

            Close();
        }

        private void OnKeepPressed()
        {
            // Não pergunta de novo enquanto a capacidade continuar insuficiente - a tentativa
            // de evolução em si não para, só o aviso (ver GameManager.TryToEvolve).
            _blockedDigimon.EvolutionCapacityWarningDismissed = true;

            Game.SaveGame();

            Close();
        }

        private void Close()
        {
            Visible = false;

            _blockedDigimon = null;
        }
    }
}
