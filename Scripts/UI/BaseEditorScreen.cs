using Godot;
using ProjetoDC.Enums;
using ProjetoDC.Scripts.Managers;
using ProjetoDC.Scripts.Models.World;
using System;

namespace ProjetoDC.Scripts.UI
{
    /// <summary>
    /// Inventário de bases: lista as áreas construídas (com opção de remover, voltando pro
    /// inventário) e as áreas no inventário (com opção de recolocar em qualquer hexágono
    /// livre, sem custo - já foram pagas na compra original). Remover nunca descarta a base,
    /// só tira ela do grid - "alterar o posicionamento" é simplesmente remover e recolocar.
    /// </summary>
    public partial class BaseEditorScreen : Control
    {
        private VBoxContainer _placedContainer;
        private VBoxContainer _inventoryContainer;
        private Label _messageLabel;
        private Button _backButton;

        private Center _center;

        public event Action BackPressed;

        public override void _Ready()
        {
            // O Center pausa a árvore inteira enquanto essa tela está aberta (ver
            // HUD.SyncPauseWithBlockingScreens) - sem isso os botões parariam de responder.
            ProcessMode = ProcessModeEnum.Always;

            _placedContainer = GetNode<VBoxContainer>(
                "Window/MarginContainer/VBoxContainer/ContentScroll/ContentVBox/PlacedContainer"
            );

            _inventoryContainer = GetNode<VBoxContainer>(
                "Window/MarginContainer/VBoxContainer/ContentScroll/ContentVBox/InventoryContainer"
            );

            _messageLabel = GetNode<Label>(
                "Window/MarginContainer/VBoxContainer/MessageLabel"
            );

            _backButton = GetNode<Button>("Window/MarginContainer/VBoxContainer/BackButton");
            _backButton.Pressed += OnBackPressed;
        }

        public void Open(Center center)
        {
            _center = center;

            RefreshUI();

            Visible = true;
        }

        public void RefreshUI()
        {
            ClearContainer(_placedContainer);
            ClearContainer(_inventoryContainer);

            var placedAreas = _center.GetRemovableAreas();

            if (placedAreas.Count == 0)
            {
                _placedContainer.AddChild(BuildEmptyRow("Nenhuma base construída além da inicial."));
            }
            else
            {
                foreach (var area in placedAreas)
                    _placedContainer.AddChild(BuildPlacedRow(area));
            }

            var inventory = GameManager.Instance.Save.Center.UnplacedAreas;

            if (inventory.Count == 0)
            {
                _inventoryContainer.AddChild(BuildEmptyRow("Nenhuma base no inventário."));
            }
            else
            {
                foreach (var areaType in inventory)
                    _inventoryContainer.AddChild(BuildInventoryRow(areaType));
            }
        }

        private static void ClearContainer(VBoxContainer container)
        {
            foreach (var child in container.GetChildren())
                child.QueueFree();
        }

        private Control BuildPlacedRow(CenterArea area)
        {
            var button = new Button
            {
                Text = "Remover",
                CustomMinimumSize = new Vector2(84, 0),
            };

            button.Pressed += () => OnRemovePressed(area);

            return BuildRow(
                GetAreaEmoji(area.AreaType),
                GetAreaDisplayName(area.AreaType),
                $"Posição: ({area.GridPosition.X}, {area.GridPosition.Y})",
                button
            );
        }

        private Control BuildInventoryRow(CenterAreaType areaType)
        {
            var button = new Button
            {
                Text = "Colocar",
                CustomMinimumSize = new Vector2(84, 0),
            };

            button.Pressed += () => OnPlacePressed(areaType);

            return BuildRow(
                GetAreaEmoji(areaType),
                GetAreaDisplayName(areaType),
                "No inventário - sem custo pra recolocar",
                button
            );
        }

        private Control BuildEmptyRow(string text)
        {
            return new Label
            {
                Text = text,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                Modulate = new Color(1f, 1f, 1f, 0.6f),
            };
        }

        private static readonly Color RowBackground = new(0.1882353f, 0.21176471f, 0.23921569f, 1f);
        private static readonly Color RowBorder = new(0.33333334f, 0.35686275f, 0.3882353f, 1f);

        /// <summary>Mesma linha [ícone | nome/detalhe | ação] usada nas listas de compra da
        /// loja (ver ShopScreen.tscn), montada por código aqui porque a quantidade de linhas
        /// varia (uma por base construída/no inventário) em vez de ser fixa.</summary>
        private Control BuildRow(string emoji, string title, string subtitle, Button actionButton)
        {
            var style = new StyleBoxFlat
            {
                BgColor = RowBackground,
                BorderColor = RowBorder,
                BorderWidthLeft = 1,
                BorderWidthTop = 1,
                BorderWidthRight = 1,
                BorderWidthBottom = 1,
                CornerRadiusTopLeft = 6,
                CornerRadiusTopRight = 6,
                CornerRadiusBottomRight = 6,
                CornerRadiusBottomLeft = 6,
                ContentMarginLeft = 10,
                ContentMarginTop = 8,
                ContentMarginRight = 10,
                ContentMarginBottom = 8,
            };

            var panel = new PanelContainer();
            panel.AddThemeStyleboxOverride("panel", style);

            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 10);
            panel.AddChild(row);

            var iconLabel = new Label
            {
                Text = emoji,
                VerticalAlignment = VerticalAlignment.Center,
            };
            iconLabel.AddThemeFontSizeOverride("font_size", 22);
            row.AddChild(iconLabel);

            var info = new VBoxContainer();
            info.AddThemeConstantOverride("separation", 0);
            info.SizeFlagsVertical = SizeFlags.ShrinkCenter;

            var titleLabel = new Label { Text = title };
            titleLabel.AddThemeFontSizeOverride("font_size", 13);
            info.AddChild(titleLabel);

            var subtitleLabel = new Label
            {
                Text = subtitle,
                Modulate = new Color(0.7f, 0.72f, 0.75f, 1f),
            };
            subtitleLabel.AddThemeFontSizeOverride("font_size", 10);
            info.AddChild(subtitleLabel);

            row.AddChild(info);

            row.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });

            row.AddChild(actionButton);

            return panel;
        }

        private void OnRemovePressed(CenterArea area)
        {
            var result = _center.RemoveArea(area);

            if (result.Success)
            {
                RefreshUI();
            }
            else
            {
                ShowMessage(result.Reason);
            }
        }

        private void OnPlacePressed(CenterAreaType areaType)
        {
            Visible = false;

            _center.StartAreaPlacementFromInventory(areaType);
        }

        /// <summary>Mostra o motivo de uma remoção ter falhado (ex.: Digimon em cima da área)
        /// por alguns segundos - mesmo padrão do ShopScreen.ShowMessage.</summary>
        private void ShowMessage(string text)
        {
            if (_messageLabel == null)
                return;

            _messageLabel.Text = text;
            _messageLabel.Visible = true;

            GetTree().CreateTimer(3.0).Timeout += () =>
            {
                if (IsInstanceValid(_messageLabel))
                    _messageLabel.Visible = false;
            };
        }

        private static string GetAreaEmoji(CenterAreaType areaType) => areaType switch
        {
            CenterAreaType.Training => "🏋",
            CenterAreaType.TrainingHealthPoints => "❤️",
            CenterAreaType.TrainingAttack => "⚔️",
            CenterAreaType.TrainingDefense => "🛡️",
            CenterAreaType.TrainingSpecialAttack => "✨",
            CenterAreaType.TrainingSpecialDefense => "🔷",
            CenterAreaType.TrainingSpeed => "⚡",
            CenterAreaType.Dormitory => "🛏",
            CenterAreaType.Restaurant => "🍴",
            CenterAreaType.Hospital => "⚕",
            _ => "⬡",
        };

        private static string GetAreaDisplayName(CenterAreaType areaType) => areaType switch
        {
            CenterAreaType.Training => "Área de Treino",
            CenterAreaType.TrainingHealthPoints => "Treino de HP",
            CenterAreaType.TrainingAttack => "Treino de Ataque",
            CenterAreaType.TrainingDefense => "Treino de Defesa",
            CenterAreaType.TrainingSpecialAttack => "Treino de Ataque Especial",
            CenterAreaType.TrainingSpecialDefense => "Treino de Defesa Especial",
            CenterAreaType.TrainingSpeed => "Treino de Velocidade",
            CenterAreaType.Dormitory => "Dormitório",
            CenterAreaType.Restaurant => "Restaurante",
            CenterAreaType.Hospital => "Hospital",
            _ => "Área",
        };

        private void OnBackPressed()
        {
            Visible = false;

            BackPressed?.Invoke();
        }
    }
}
