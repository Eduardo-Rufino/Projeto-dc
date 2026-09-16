using Godot;
using ProjetoDC.Enums;
using ProjetoDC.Scripts.Core.Results;
using ProjetoDC.Scripts.Data;
using ProjetoDC.Scripts.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjetoDC.Scripts.UI
{
    public partial class ShopScreen : Control
    {
        private Label _bitsLabel;
        private Label _meatLabel;
        private Label _medicineLabel;
        private Label _messageLabel;

        private Button _buyMeatButton;
        private Button _buyMedicineButton;
        private Button _buyEggButton;

        private Label _staminaSnackLabel;
        private Button _buyStaminaSnackButton;

        private Control _eggTypePopup;
        private Label _randomEggPriceLabel;
        private Button _buyRandomEggButton;
        private VBoxContainer _eggTypesContainer;
        private Label _eggTypeMessageLabel;

        private readonly Dictionary<CenterAreaType, Button> _buyAreaButtons = new();
        private readonly List<Control> _eggTypeRows = new();

        // Nome em português de cada EggType, só pra exibição na tela de escolha - a ordem
        // aqui também é a ordem que as linhas aparecem no popup. O ícone de cada linha usa o
        // sprite de verdade (ver EggVisuals/BuildEggTypeRows), não um emoji.
        private static readonly (EggType Type, string Label)[] EggTypeOptions =
        {
            (EggType.Dragon, "Dragão"),
            (EggType.Beast, "Fera"),
            (EggType.Water, "Água"),
            (EggType.Forest, "Floresta"),
            (EggType.Fire, "Fogo"),
            (EggType.Dark, "Trevas"),
            (EggType.Light, "Luz"),
        };

        public event Action BackPressed;
        public event Action<EggData> EggPurchased;

        /// <summary>Disparado quando uma área é comprada com sucesso - o Center escuta isso
        /// pra fechar a loja e entrar no modo de posicionamento (ver Center.StartAreaPlacement).</summary>
        public event Action<CenterAreaType> AreaPurchased;

        private GameManager Game => GameManager.Instance;

        public override void _Ready()
        {
            // O Center pausa a árvore inteira enquanto essa tela está aberta (ver
            // HUD.SyncPauseWithBlockingScreens) - sem isso os botões parariam de responder.
            ProcessMode = ProcessModeEnum.Always;

            InitializeUIComponents();
            RefreshUI();
        }

        private static readonly (CenterAreaType Type, string RowName)[] AreaRows =
        {
            (CenterAreaType.Training, "TrainingAreaRow"),
            (CenterAreaType.TrainingHealthPoints, "TrainingHpAreaRow"),
            (CenterAreaType.TrainingAttack, "TrainingAttackAreaRow"),
            (CenterAreaType.TrainingDefense, "TrainingDefenseAreaRow"),
            (CenterAreaType.TrainingSpecialAttack, "TrainingSpecialAttackAreaRow"),
            (CenterAreaType.TrainingSpecialDefense, "TrainingSpecialDefenseAreaRow"),
            (CenterAreaType.TrainingSpeed, "TrainingSpeedAreaRow"),
            (CenterAreaType.Dormitory, "DormitoryAreaRow"),
            (CenterAreaType.Restaurant, "RestaurantAreaRow"),
            (CenterAreaType.Hospital, "HospitalAreaRow"),
        };

        private void InitializeUIComponents()
        {
            var itemsPath = "Window/MarginContainer/VBoxContainer/ItemsScroll/ItemsContainer/";

            _bitsLabel = GetNodeOrNull<Label>(
                "Window/MarginContainer/VBoxContainer/HeaderRow/BitsLabel"
            );

            _meatLabel = GetNodeOrNull<Label>(
                $"{itemsPath}MeatRow/Pad/HBoxContainer/InfoContainer/MeatLabel"
            );

            _medicineLabel = GetNodeOrNull<Label>(
                $"{itemsPath}MedicineRow/Pad/HBoxContainer/InfoContainer/MedicineLabel"
            );

            _buyMeatButton = GetNodeOrNull<Button>(
                $"{itemsPath}MeatRow/Pad/HBoxContainer/BuyMeatButton"
            );

            _buyMedicineButton = GetNodeOrNull<Button>(
                $"{itemsPath}MedicineRow/Pad/HBoxContainer/BuyMedicineButton"
            );

            _buyEggButton = GetNodeOrNull<Button>(
                $"{itemsPath}EggRow/Pad/HBoxContainer/BuyEggButton"
            );

            _messageLabel = GetNodeOrNull<Label>(
                "Window/MarginContainer/VBoxContainer/MessageLabel"
            );

            _eggTypePopup = GetNodeOrNull<Control>("EggTypePopup");

            var popupPath = "EggTypePopup/Window/MarginContainer/VBoxContainer/";

            _randomEggPriceLabel = GetNodeOrNull<Label>($"{popupPath}RandomRow/Pad/HBoxContainer/RandomPriceLabel");

            _buyRandomEggButton = GetNodeOrNull<Button>($"{popupPath}RandomRow/Pad/HBoxContainer/BuyRandomEggButton");
            _buyRandomEggButton.Pressed += OnBuyRandomEggPressed;

            _eggTypesContainer = GetNodeOrNull<VBoxContainer>($"{popupPath}TypesScroll/TypesContainer");

            _eggTypeMessageLabel = GetNodeOrNull<Label>($"{popupPath}EggTypeMessageLabel");

            var cancelEggTypeButton = GetNodeOrNull<Button>($"{popupPath}CancelEggTypeButton");
            cancelEggTypeButton.Pressed += OnCancelEggTypePressed;

            // Meat/Medicine/Egg já são ligados via [connection] no .tscn - só os botões de
            // área (dinâmicos, um por CenterAreaType) precisam de ligação por código aqui.
            foreach (var (areaType, rowName) in AreaRows)
            {
                var button = GetNodeOrNull<Button>(
                    $"{itemsPath}{rowName}/Pad/HBoxContainer/BuyAreaButton"
                );

                button.Pressed += () => OnBuyAreaPressed(areaType);

                _buyAreaButtons[areaType] = button;
            }

            BuildStaminaSnackRow(itemsPath);
        }

        /// <summary>Linha do Energético (ver GameManager.BuyStaminaSnack) - construída por
        /// código, igual às linhas do popup de tipo de ovo (BuildEggTypeRows), em vez de vir
        /// pré-montada no .tscn como MeatRow/MedicineRow - evita mexer na cena pra adicionar
        /// um item novo.</summary>
        private void BuildStaminaSnackRow(string itemsPath)
        {
            var itemsContainer = GetNodeOrNull<VBoxContainer>(itemsPath.TrimEnd('/'));

            if (itemsContainer == null)
                return;

            var row = new PanelContainer();
            row.AddThemeStyleboxOverride("panel", CreateItemStyle());

            var pad = new MarginContainer();
            pad.AddThemeConstantOverride("margin_left", 10);
            pad.AddThemeConstantOverride("margin_top", 8);
            pad.AddThemeConstantOverride("margin_right", 10);
            pad.AddThemeConstantOverride("margin_bottom", 8);
            row.AddChild(pad);

            var hbox = new HBoxContainer();
            hbox.AddThemeConstantOverride("separation", 10);
            pad.AddChild(hbox);

            hbox.AddChild(BuildStaminaSnackIcon());

            var info = new VBoxContainer();
            info.AddThemeConstantOverride("separation", 0);
            info.AddChild(new Label { Text = "Energético" });

            _staminaSnackLabel = new Label
            {
                Modulate = new Color(0.7f, 0.72f, 0.75f),
            };
            info.AddChild(_staminaSnackLabel);
            hbox.AddChild(info);

            var spacer = new Control();
            spacer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            hbox.AddChild(spacer);

            hbox.AddChild(new Label
            {
                Text = $"{GameManager.StaminaSnackPrice} Bits",
                VerticalAlignment = VerticalAlignment.Center,
            });

            _buyStaminaSnackButton = new Button
            {
                Text = "Comprar",
                CustomMinimumSize = new Vector2(84, 0),
            };
            _buyStaminaSnackButton.AddThemeStyleboxOverride("normal", CreateItemStyle());
            _buyStaminaSnackButton.Pressed += OnBuyStaminaSnackPressed;
            hbox.AddChild(_buyStaminaSnackButton);

            itemsContainer.AddChild(row);
        }

        public void RefreshUI()
        {
            _bitsLabel.Text = $"💰 {Game.Save.Center.Bits:N0}";
            _meatLabel.Text = $"Você tem: {Game.Save.Center.Meat}";
            _medicineLabel.Text = $"Você tem: {Game.Save.Center.Medicine}";

            _buyMeatButton.Disabled = Game.Save.Center.Bits < 20;
            _buyMedicineButton.Disabled = Game.Save.Center.Bits < 100;
            _buyEggButton.Disabled = Game.Save.Center.Bits < GameManager.RandomEggPrice || !Game.HasCapacityForEgg();

            if (_staminaSnackLabel != null)
                _staminaSnackLabel.Text = $"Você tem: {Game.Save.Center.StaminaSnacks}";

            if (_buyStaminaSnackButton != null)
                _buyStaminaSnackButton.Disabled = Game.Save.Center.Bits < GameManager.StaminaSnackPrice;

            foreach (var (areaType, button) in _buyAreaButtons)
                button.Disabled = Game.Save.Center.Bits < GameManager.GetAreaPrice(areaType);
        }

        /// <summary>Mostra o motivo de uma compra ter falhado (ex.: "Não há capacidade
        /// suficiente") por alguns segundos, em vez de só imprimir no log - sem isso o
        /// jogador clicava em Comprar e nada parecia acontecer.</summary>
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

        private void OnBuyMeatPressed()
        {
            var result = Game.BuyMeat();

            if (result.Success)
            {
                RefreshUI();
            }
            else
            {
                ShowMessage(result.Reason);
            }
        }

        private void OnBuyMedicinePressed()
        {
            var result = Game.BuyMedicine();

            if (result.Success)
                RefreshUI();
            else
                ShowMessage(result.Reason);
        }

        private void OnBuyStaminaSnackPressed()
        {
            var result = Game.BuyStaminaSnack();

            if (result.Success)
                RefreshUI();
            else
                ShowMessage(result.Reason);
        }

        // Abrir a loja não mexe em ovos, então o popup pode ficar sempre montado (sem
        // reconstruir do zero) - só precisa atualizar preços/disponibilidade toda vez que
        // abre, já que Bits e capacidade podem ter mudado desde a última vez.
        private void OnBuyEggPressed()
        {
            OpenEggTypePopup();
        }

        /// <summary>Só pra Center.DebugOpenScreen ("shopeggtypes") poder tirar screenshot do
        /// popup sem precisar simular clique - ver SKILL.md do run-projeto-dc.</summary>
        public void DebugOpenEggTypePopup() => OpenEggTypePopup();

        private void OpenEggTypePopup()
        {
            _randomEggPriceLabel.Text = $"{GameManager.RandomEggPrice} Bits";
            _buyRandomEggButton.Disabled = Game.Save.Center.Bits < GameManager.RandomEggPrice || !Game.HasCapacityForEgg();

            BuildEggTypeRows();

            if (_eggTypeMessageLabel != null)
                _eggTypeMessageLabel.Visible = false;

            _eggTypePopup.Visible = true;
        }

        private void CloseEggTypePopup()
        {
            _eggTypePopup.Visible = false;
        }

        /// <summary>Uma linha [ícone | nome | preço | Comprar] por EggType - desabilitada
        /// (com aviso) se não existir nenhum Digimon Baby daquele tipo ainda (ver
        /// GameManager.CountBabyDigimonsOfType), pra não deixar o jogador pagar 3x por um
        /// ovo que nunca vai chocar.</summary>
        private void BuildEggTypeRows()
        {
            foreach (var row in _eggTypeRows)
                row.QueueFree();

            _eggTypeRows.Clear();

            bool hasCapacity = Game.HasCapacityForEgg();
            int price = GameManager.GetEggPrice(EggType.Dragon); // mesmo preço pra qualquer tipo específico

            foreach (var (type, label) in EggTypeOptions)
            {
                int available = Game.CountBabyDigimonsOfType(type);

                var row = new PanelContainer();
                row.AddThemeStyleboxOverride("panel", CreateItemStyle());

                var pad = new MarginContainer();
                pad.AddThemeConstantOverride("margin_left", 10);
                pad.AddThemeConstantOverride("margin_top", 8);
                pad.AddThemeConstantOverride("margin_right", 10);
                pad.AddThemeConstantOverride("margin_bottom", 8);
                row.AddChild(pad);

                var hbox = new HBoxContainer();
                hbox.AddThemeConstantOverride("separation", 10);
                pad.AddChild(hbox);

                hbox.AddChild(BuildEggTypeIcon(type));

                // Sem CustomMinimumSize/expand/autowrap forçado - igual às linhas normais da
                // loja (MeatRow/EggRow), o texto fica numa linha só e o container cresce só
                // o que precisa, sem disputar espaço com o preço/botão.
                var info = new VBoxContainer();
                info.AddThemeConstantOverride("separation", 0);
                info.AddChild(new Label { Text = label });
                info.AddChild(new Label
                {
                    Text = available > 0 ? $"{available} espécie(s)" : "Indisponível",
                    Modulate = new Color(0.7f, 0.72f, 0.75f),
                });
                hbox.AddChild(info);

                var spacer = new Control();
                spacer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
                hbox.AddChild(spacer);

                hbox.AddChild(new Label
                {
                    Text = $"{price} Bits",
                    VerticalAlignment = VerticalAlignment.Center,
                });

                var buyButton = new Button
                {
                    Text = "Comprar",
                    CustomMinimumSize = new Vector2(84, 0),
                };
                buyButton.AddThemeStyleboxOverride("normal", CreateItemStyle());
                buyButton.Disabled = available == 0 || !hasCapacity || Game.Save.Center.Bits < price;
                buyButton.Pressed += () => OnBuySpecificEggPressed(type);
                hbox.AddChild(buyButton);

                _eggTypesContainer.AddChild(row);
                _eggTypeRows.Add(row);
            }
        }

        /// <summary>Frame 0 (mesmo "frame 1" que o Digitama usa parado - ver EggWorld.
        /// UpdateFrame) do sprite de cada EggType, como preview estático na linha - o mesmo
        /// recorte que o EggRow principal já usava antes de ganhar tipos.</summary>
        private static TextureRect BuildEggTypeIcon(EggType type)
        {
            // KeepAspectCentered (não Scale) - o recorte é um quadrado 16x16, mas a
            // HBoxContainer da linha pode espremer a largura disponível; Scale distorce pra
            // preencher o espaço que sobrar, KeepAspectCentered mantém o quadrado intacto
            // (igual ao padrão já usado em EvolutionGuideScreen.BuildSilhouette).
            var rect = new TextureRect
            {
                CustomMinimumSize = new Vector2(32, 32),
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                TextureFilter = TextureFilterEnum.Nearest,
            };

            string path = EggVisuals.GetTexturePath(type);

            if (ResourceLoader.Exists(path))
            {
                rect.Texture = new AtlasTexture
                {
                    Atlas = GD.Load<Texture2D>(path),
                    Region = new Rect2(0, 0, 16, 16),
                };
            }

            return rect;
        }

        /// <summary>Mesmo esquema de BuildEggTypeIcon acima, pro ícone do Energético na linha
        /// da loja - o sprite de verdade (ver FoodWorld.StaminaSnackTexturePath), não o emoji
        /// ⚡ que era usado antes só como placeholder.</summary>
        private static TextureRect BuildStaminaSnackIcon()
        {
            var rect = new TextureRect
            {
                CustomMinimumSize = new Vector2(32, 32),
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                TextureFilter = TextureFilterEnum.Nearest,
            };

            const string path = "res://Assets/Sprites/Food/energetico.png";

            if (ResourceLoader.Exists(path))
                rect.Texture = GD.Load<Texture2D>(path);

            return rect;
        }

        private static StyleBoxFlat CreateItemStyle()
        {
            var style = new StyleBoxFlat
            {
                BgColor = new Color(0.1882353f, 0.21176471f, 0.23921569f),
                BorderColor = new Color(0.33333334f, 0.35686275f, 0.3882353f),
            };

            style.SetBorderWidthAll(1);
            style.SetCornerRadiusAll(6);

            return style;
        }

        private void OnBuyRandomEggPressed()
        {
            FinishEggPurchase(Game.BuyEgg());
        }

        private void OnBuySpecificEggPressed(EggType type)
        {
            FinishEggPurchase(Game.BuyEgg(type));
        }

        private void FinishEggPurchase(SystemResult result)
        {
            if (result.Success)
            {
                RefreshUI();
                CloseEggTypePopup();

                EggPurchased?.Invoke(Game.Save.Center.Eggs.Last());
            }
            else if (_eggTypeMessageLabel != null)
            {
                _eggTypeMessageLabel.Text = result.Reason;
                _eggTypeMessageLabel.Visible = true;
            }
        }

        private void OnCancelEggTypePressed()
        {
            CloseEggTypePopup();
        }

        private void OnBuyAreaPressed(CenterAreaType areaType)
        {
            var result = Game.BuyArea(areaType);

            if (result.Success)
            {
                RefreshUI();

                AreaPurchased?.Invoke(areaType);
            }
            else
            {
                ShowMessage(result.Reason);
            }
        }

        private void OnBackPressed()
        {
            BackPressed?.Invoke();
        }
    }
}
