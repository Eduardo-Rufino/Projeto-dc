using Godot;
using ProjetoDC.Scripts.Data;
using ProjetoDC.Scripts.Managers;

namespace ProjetoDC.Scripts.UI
{
    /// <summary>
    /// Diálogo de um NPC de exploração - texto de flavor (NpcData.DialogueLines) + estado
    /// contextual da quest que ele oferece (NpcData.QuestId): oferecer, em andamento, pronta
    /// pra entregar, ou já completa. Só single-step em v1 - sem múltiplos objetivos/etapas.
    /// </summary>
    public partial class DialogueScreen : Control
    {
        private TextureRect _portrait;
        private Label _nameLabel;
        private RichTextLabel _dialogueLabel;
        private Control _questPanel;
        private Label _questLabel;
        private Button _actionButton;
        private Button _closeButton;

        private NpcData _npc;

        /// <summary>Qual ação ActionButton dispara na próxima vez que for clicado - decidido
        /// por RefreshQuestState. ActionButton.Pressed é conectado uma única vez (ver _Ready);
        /// evita ficar conectando/desconectando OnAcceptPressed/OnTurnInPressed a cada
        /// RefreshQuestState, que gerava "Attempt to disconnect a nonexistent connection"
        /// sempre que o handler que ia ser removido nunca tinha sido conectado (ex.: primeira
        /// vez que qualquer diálogo abre na sessão).</summary>
        private enum PendingQuestAction { None, Accept, TurnIn }

        private PendingQuestAction _pendingAction;

        public override void _Ready()
        {
            // A área de exploração pausa o resto do jogo enquanto uma batalha selvagem está
            // rolando, mas o diálogo em si não pausa nada - ainda assim segue o mesmo padrão
            // de ProcessMode.Always das outras telas modais, por segurança/consistência.
            ProcessMode = ProcessModeEnum.Always;

            _portrait = GetNode<TextureRect>("Window/MarginContainer/VBoxContainer/HeaderRow/Portrait");
            _nameLabel = GetNode<Label>("Window/MarginContainer/VBoxContainer/HeaderRow/NameLabel");

            _dialogueLabel = GetNode<RichTextLabel>(
                "Window/MarginContainer/VBoxContainer/DialogueScroll/DialogueLabel"
            );

            _questPanel = GetNode<Control>("Window/MarginContainer/VBoxContainer/QuestPanel");
            _questLabel = GetNode<Label>("Window/MarginContainer/VBoxContainer/QuestPanel/QuestLabel");
            _actionButton = GetNode<Button>("Window/MarginContainer/VBoxContainer/FooterRow/ActionButton");
            _closeButton = GetNode<Button>("Window/MarginContainer/VBoxContainer/FooterRow/CloseButton");

            _closeButton.Pressed += () => Visible = false;
            _actionButton.Pressed += OnActionButtonPressed;
        }

        public void Open(NpcData npc)
        {
            _npc = npc;

            _nameLabel.Text = npc.Name;
            _dialogueLabel.Text = string.Join("\n\n", npc.DialogueLines);

            string portraitPath = $"res://Assets/Sprites/Digimon/{npc.Code}.png";

            _portrait.Texture = ResourceLoader.Exists(portraitPath)
                ? GD.Load<Texture2D>(portraitPath)
                : null;

            RefreshQuestState();

            Visible = true;
        }

        private void RefreshQuestState()
        {
            _pendingAction = PendingQuestAction.None;
            _actionButton.Visible = false;

            if (_npc.QuestId == null)
            {
                _questPanel.Visible = false;
                return;
            }

            int questId = _npc.QuestId.Value;
            var quest = DatabaseManager.Instance.GetQuest(questId);

            if (quest == null)
            {
                _questPanel.Visible = false;
                return;
            }

            var game = GameManager.Instance;

            _questPanel.Visible = true;

            if (game.Save.Center.CompletedQuestIds.Contains(questId))
            {
                _questLabel.Text = _npc.IsRecruitable
                    ? $"{quest.Name} - já completa. {_npc.Name} foi recrutado para o Center!"
                    : $"{quest.Name} - já completa.";
                return;
            }

            if (game.Save.Center.ActiveQuestIds.Contains(questId))
            {
                if (game.IsQuestReadyToTurnIn(questId))
                {
                    _questLabel.Text = $"{quest.Name} - pronta pra entregar!";
                    _actionButton.Text = "Entregar";
                    _actionButton.Visible = true;
                    _pendingAction = PendingQuestAction.TurnIn;
                }
                else
                {
                    int progress = game.GetQuestProgress(questId);
                    _questLabel.Text = $"{quest.Name} - {progress}/{quest.ObjectiveCount}";
                }

                return;
            }

            string description = quest.Description;

            if (_npc.IsRecruitable && !string.IsNullOrEmpty(_npc.RecruitmentPerkDescription))
                description += $"\n\nSe recrutado: {_npc.RecruitmentPerkDescription}";

            _questLabel.Text = $"{quest.Name}\n{description}";
            _actionButton.Text = "Aceitar";
            _actionButton.Visible = true;
            _pendingAction = PendingQuestAction.Accept;
        }

        private void OnActionButtonPressed()
        {
            switch (_pendingAction)
            {
                case PendingQuestAction.Accept:
                    GameManager.Instance.TryAcceptQuest(_npc.QuestId!.Value);
                    break;
                case PendingQuestAction.TurnIn:
                    GameManager.Instance.TryTurnInQuest(_npc.QuestId!.Value);
                    break;
            }

            RefreshQuestState();
        }
    }
}
