using Godot;
using ProjetoDC.Scripts.Gameplay;
using ProjetoDC.Scripts.Managers;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProjetoDC.Scripts.UI
{
    /// <summary>
    /// Guia de evolução: lista os Digimons do roster do jogador e, ao clicar em um deles,
    /// mostra todas as evoluções possíveis (pode ter mais de uma - ver EvolutionSystem) com
    /// os requisitos de cada uma e se o Digimon já os atende ou não. Puramente informativo,
    /// não aplica nenhuma evolução por aqui (isso continua automático via GameManager.
    /// TryToEvolve, chamado durante o dia/treino/batalha).
    /// </summary>
    public partial class EvolutionGuideScreen : Control
    {
        private VBoxContainer _rosterContainer;
        private Label _emptyLabel;
        private RichTextLabel _headerLabel;
        private VBoxContainer _entriesContainer;
        private Button _backButton;

        private readonly List<Button> _rosterButtons = new();
        private readonly ButtonGroup _rosterGroup = new();

        // Tamanho fixo da silhueta ao lado de cada evolução - grande o bastante pra dar pra
        // reconhecer a forma, sem dominar a linha de requisitos.
        private static readonly Vector2 SilhouetteSize = new(64, 64);

        // Modula a sprite (idle, frame 0) pra preto sólido preservando o alfa da textura -
        // dá o efeito de silhueta sem precisar de um asset separado por Digimon.
        private static readonly Color SilhouetteColor = new(0f, 0f, 0f);

        public event System.Action BackPressed;

        private GameManager Game => GameManager.Instance;

        public override void _Ready()
        {
            // O Center pausa a árvore inteira enquanto essa tela está aberta (ver
            // HUD.SyncPauseWithBlockingScreens) - sem isso os botões parariam de responder.
            ProcessMode = ProcessModeEnum.Always;

            _rosterContainer = GetNode<VBoxContainer>(
                "Window/MarginContainer/VBoxContainer/ContentRow/RosterScroll/RosterContainer"
            );

            _emptyLabel = GetNode<Label>("Window/MarginContainer/VBoxContainer/EmptyLabel");

            _headerLabel = GetNode<RichTextLabel>(
                "Window/MarginContainer/VBoxContainer/ContentRow/ContentPanel/ContentScroll/ContentVBox/HeaderLabel"
            );

            _entriesContainer = GetNode<VBoxContainer>(
                "Window/MarginContainer/VBoxContainer/ContentRow/ContentPanel/ContentScroll/ContentVBox/EntriesContainer"
            );

            _backButton = GetNode<Button>("Window/MarginContainer/VBoxContainer/BackButton");

            _backButton.Pressed += OnBackPressed;
        }

        public void Open()
        {
            RefreshRoster();

            Visible = true;
        }

        private void RefreshRoster()
        {
            foreach (var button in _rosterButtons)
                button.QueueFree();

            _rosterButtons.Clear();

            var roster = Game.CenterService.GetAllDigimons().ToList();

            _emptyLabel.Visible = roster.Count == 0;

            foreach (var digimon in roster)
            {
                var button = new Button
                {
                    Text = $"{digimon.DisplayName} (Nv. {digimon.Level})",
                    ToggleMode = true,
                    ButtonGroup = _rosterGroup,
                    Alignment = HorizontalAlignment.Left,
                };

                button.Pressed += () => ShowEvolutionInfo(digimon);

                _rosterContainer.AddChild(button);
                _rosterButtons.Add(button);
            }

            if (_rosterButtons.Count > 0)
            {
                _rosterButtons[0].ButtonPressed = true;
                ShowEvolutionInfo(roster[0]);
            }
            else
            {
                _headerLabel.Text = "Nenhum Digimon no Center ainda.";
                ClearEntries();
            }
        }

        private void ClearEntries()
        {
            foreach (var child in _entriesContainer.GetChildren())
                child.QueueFree();
        }

        private void ShowEvolutionInfo(DigimonInstance digimon)
        {
            var evolutions = DatabaseManager.Instance.GetEvolutionsFrom(digimon.BaseData.Id);

            _headerLabel.Text =
                $"[b]{digimon.DisplayName}[/b] ({digimon.BaseData.Name}) - " +
                $"Nv. {digimon.Level} - {digimon.BaseData.Stage}";

            ClearEntries();

            if (evolutions == null || evolutions.Count == 0)
            {
                var label = new Label
                {
                    Text = "Esse Digimon ainda não tem nenhuma evolução conhecida (forma final, por enquanto).",
                    AutowrapMode = TextServer.AutowrapMode.WordSmart,
                };

                _entriesContainer.AddChild(label);

                return;
            }

            var center = Game.Save.Center;

            foreach (var evo in evolutions)
            {
                var target = DatabaseManager.Instance.GetDigimon(evo.ToDigimonId);

                if (target == null)
                    continue;

                bool levelOk = digimon.Level >= evo.RequiredLevel;
                bool ageOk = digimon.AgeInDays >= evo.RequiredAgeInDays;

                int requiredCapacity = DigimonInstance.GetCapacityCostForStage(target.Stage);
                int capacityDelta = requiredCapacity - digimon.CapacityCost;
                bool capacityOk = center.CapacityUsed + capacityDelta <= center.CapacityLimit;

                bool statsOk = true;

                var sb = new StringBuilder();

                sb.AppendLine($"[b]➜ {target.Name}[/b] ({target.Stage})");
                sb.AppendLine($"{Check(levelOk)} Nível: {digimon.Level}/{evo.RequiredLevel}");
                sb.AppendLine($"{Check(ageOk)} Idade: {digimon.AgeInDays}/{evo.RequiredAgeInDays} dia(s)");
                sb.AppendLine(
                    $"{Check(capacityOk)} Capacidade extra: {capacityDelta} " +
                    $"(Center: {center.CapacityUsed}/{center.CapacityLimit})"
                );

                var req = evo.RequiredStats;

                if (req != null)
                {
                    AppendStatRequirement(sb, "HP", digimon.CurrentStats.HealthPoints, req.HealthPoints, ref statsOk);
                    AppendStatRequirement(sb, "Ataque", digimon.CurrentStats.PhysicalDamage, req.PhysicalDamage, ref statsOk);
                    AppendStatRequirement(sb, "Defesa", digimon.CurrentStats.PhysicalDefense, req.PhysicalDefense, ref statsOk);
                    AppendStatRequirement(sb, "Ataque Especial", digimon.CurrentStats.SpecialDamage, req.SpecialDamage, ref statsOk);
                    AppendStatRequirement(sb, "Defesa Especial", digimon.CurrentStats.SpecialDefense, req.SpecialDefense, ref statsOk);
                    AppendStatRequirement(sb, "Velocidade", digimon.CurrentStats.Speed, req.Speed, ref statsOk);
                }

                bool allOk = levelOk && ageOk && capacityOk && statsOk;

                sb.AppendLine(
                    allOk
                        ? "[color=#8fd98f]Pronto pra evoluir![/color]"
                        : "[color=#d98f8f]Ainda faltam requisitos.[/color]"
                );

                _entriesContainer.AddChild(BuildEvolutionEntry(target.Code, sb.ToString()));
            }
        }

        /// <summary>Uma linha [silhueta | requisitos] por evolução possível.</summary>
        private Control BuildEvolutionEntry(string targetCode, string requirementsText)
        {
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 12);

            row.AddChild(BuildSilhouette(targetCode));

            var label = new RichTextLabel
            {
                BbcodeEnabled = true,
                FitContent = true,
                ScrollActive = false,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                Text = requirementsText,
            };

            row.AddChild(label);

            return row;
        }

        private TextureRect BuildSilhouette(string code)
        {
            var texture = new TextureRect
            {
                CustomMinimumSize = SilhouetteSize,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                Modulate = SilhouetteColor,
            };

            string path = $"res://Assets/Sprites/Digimon/{code}/0.png";

            if (ResourceLoader.Exists(path))
                texture.Texture = GD.Load<Texture2D>(path);

            return texture;
        }

        /// <summary>Requisitos com valor 0 significam "sem exigência nesse stat" (ver
        /// EvolutionSystem.MeetsRequirements) - não mostra a linha nesse caso, pra não poluir
        /// com "0/0" em todo stat irrelevante pra essa evolução.</summary>
        private static void AppendStatRequirement(StringBuilder sb, string label, int current, int required, ref bool allOk)
        {
            if (required <= 0)
                return;

            bool ok = current >= required;

            allOk &= ok;

            sb.AppendLine($"{Check(ok)} {label}: {current}/{required}");
        }

        private static string Check(bool ok) => ok ? "✅" : "❌";

        private void OnBackPressed()
        {
            Visible = false;

            BackPressed?.Invoke();
        }
    }
}
