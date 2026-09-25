using Godot;
using ProjetoDC.Scripts.Data;
using ProjetoDC.Scripts.Gameplay;
using ProjetoDC.Scripts.Managers;
using ProjetoDC.Scripts.Systems.Tournaments;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjetoDC.Scripts.UI
{
    /// <summary>
    /// Tela de montagem do time de batalha: o jogador escolhe livremente do roster do
    /// Center, sem restrição de Role. Serve pros dois modos de batalha:
    /// - Livre (<see cref="_pendingTournament"/> nulo): 1 Digimon pra um duelo 1x1 (caso
    ///   não tenha, ou não queira usar, 3 no momento), ou 3 pro time completo 3x3.
    /// - Campeonato (<see cref="_pendingTournament"/> setado via Open): o tamanho do time
    ///   é fixo, igual ao número de oponentes do campeonato escolhido.
    /// </summary>
    public partial class TeamSelectionScreen : Control
    {
        private const int SoloTeamSize = 1;
        private const int FullTeamSize = 3;

        private GridContainer _grid;
        private Label _countLabel;
        private Label _warningLabel;
        private Button _battleButton;
        private Button _backButton;

        private PackedScene _cardScene;

        private readonly List<DigimonSelectCard> _cards = new();
        private readonly List<DigimonInstance> _selected = new();

        private TournamentData _pendingTournament;

        public event Action BackPressed;

        private GameManager Game => GameManager.Instance;

        private int RequiredTeamSize => _pendingTournament?.Opponents.Count ?? FullTeamSize;

        public override void _Ready()
        {
            // O Center pausa a árvore inteira enquanto essa tela está aberta (ver
            // HUD.SyncPauseWithBlockingScreens) - sem isso os botões parariam de responder.
            ProcessMode = ProcessModeEnum.Always;

            _grid = GetNode<GridContainer>("Window/MarginContainer/VBoxContainer/ScrollContainer/GridContainer");
            _countLabel = GetNode<Label>("Window/MarginContainer/VBoxContainer/HeaderRow/CountLabel");
            _warningLabel = GetNode<Label>("Window/MarginContainer/VBoxContainer/WarningLabel");
            _battleButton = GetNode<Button>("Window/MarginContainer/VBoxContainer/HBoxContainer/BattleButton");
            _backButton = GetNode<Button>("Window/MarginContainer/VBoxContainer/HBoxContainer/BackButton");

            _cardScene = GD.Load<PackedScene>("res://Scenes/Battle/DigimonSelectCard.tscn");

            _battleButton.Pressed += OnBattlePressed;
            _backButton.Pressed += OnBackPressed;
        }

        /// <summary>Abre pra batalha livre (tamanho de time flexível: 1 ou 3).</summary>
        public void Open()
        {
            _pendingTournament = null;

            RefreshUI();
        }

        /// <summary>Abre pra um campeonato específico - tamanho de time fixo, igual ao
        /// número de oponentes do campeonato.</summary>
        public void Open(TournamentData tournament)
        {
            _pendingTournament = tournament;

            RefreshUI();
        }

        private void RefreshUI()
        {
            foreach (var card in _cards)
                card.QueueFree();

            _cards.Clear();
            _selected.Clear();

            var roster = Game.CenterService.GetAllDigimons();

            if (roster.Count == 0)
            {
                _warningLabel.Visible = true;
                _warningLabel.Text = "Você não tem nenhum Digimon no Center pra batalhar.";
            }
            else if (roster.Count < RequiredTeamSize)
            {
                _warningLabel.Visible = true;
                _warningLabel.Text = _pendingTournament != null
                    ? $"Você precisa de {RequiredTeamSize} Digimons pra esse campeonato (tem {roster.Count})."
                    : $"Você tem menos de {FullTeamSize} Digimons - só é possível batalhar 1x1.";
            }
            else
            {
                _warningLabel.Visible = false;
            }

            // Campeonato com regra de inscrição (TournamentRules): quem não cumpre fica desabilitado.
            if (_pendingTournament != null && TournamentRules.HasRules(_pendingTournament) && roster.Count >= RequiredTeamSize)
            {
                int eligible = roster.Count(d => Game.GetTournamentIneligibilityReason(_pendingTournament, d) == null);

                if (eligible < RequiredTeamSize)
                {
                    _warningLabel.Visible = true;
                    _warningLabel.Text = $"{TournamentRules.Describe(_pendingTournament)}: você tem {eligible} Digimon(s) elegível(is), precisa de {RequiredTeamSize}.";
                }
            }

            foreach (var digimon in roster)
            {
                var card = _cardScene.Instantiate<DigimonSelectCard>();

                _grid.AddChild(card);

                card.SetDigimon(digimon);
                card.ToggledSelection += OnCardToggled;

                string reason = _pendingTournament != null
                    ? Game.GetTournamentIneligibilityReason(_pendingTournament, digimon)
                    : null;

                if (reason != null)
                    card.SetBlockedReason(reason);

                _cards.Add(card);
            }

            UpdateSelectionState();
        }

        private void OnCardToggled(DigimonSelectCard card)
        {
            if (card.ButtonPressed)
            {
                int maxSelectable = _pendingTournament != null ? RequiredTeamSize : FullTeamSize;

                if (_selected.Count >= maxSelectable)
                {
                    card.SetPressedNoSignal(false);
                    return;
                }

                _selected.Add(card.Digimon);
            }
            else
            {
                _selected.Remove(card.Digimon);
            }

            UpdateSelectionState();
        }

        // Os cards fora da regra já vêm desabilitados, mas confere de novo antes de liberar o botão.
        private bool IsValidTeam() => IsValidTeamSize(_selected.Count) &&
            (_pendingTournament == null || Game.ValidateTournamentTeam(_pendingTournament, _selected).IsValid);

        private bool IsValidTeamSize(int count) => _pendingTournament != null
            ? count == RequiredTeamSize
            : count == SoloTeamSize || count == FullTeamSize;

        private void UpdateSelectionState()
        {
            if (_pendingTournament != null)
            {
                _countLabel.Text = $"{_selected.Count}/{RequiredTeamSize} selecionados " +
                    $"(campeonato: {_pendingTournament.Name})";
            }
            else
            {
                _countLabel.Text = _selected.Count switch
                {
                    SoloTeamSize => $"{_selected.Count}/{FullTeamSize} selecionados (batalha 1x1)",
                    FullTeamSize => $"{_selected.Count}/{FullTeamSize} selecionados (batalha 3x3)",
                    _ => $"{_selected.Count}/{FullTeamSize} selecionados (escolha 1 pra batalha 1x1, ou 3 pro time completo)"
                };
            }

            _battleButton.Disabled = !IsValidTeam();
        }

        private void OnBattlePressed()
        {
            if (!IsValidTeam())
                return;

            if (_pendingTournament != null)
                Game.StartTournamentBattle(_pendingTournament, new List<DigimonInstance>(_selected));
            else
                Game.StartTeamBattle(new List<DigimonInstance>(_selected));

            Visible = false;
        }

        private void OnBackPressed()
        {
            BackPressed?.Invoke();
        }
    }
}
