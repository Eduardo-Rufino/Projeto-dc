using Godot;
using System;
using System.Collections.Generic;

namespace ProjetoDC.Scripts.UI
{
    /// <summary>
    /// Lista de notas de atualização (changelog), separadas por versão - puramente
    /// informativo, sem nenhuma interação com o GameManager. As versões ficam fixas em
    /// código (_versions) porque são texto de ajuda, não dado de save/gameplay. Uma versão só
    /// é adicionada aqui quando o desenvolvedor considera ela "fechada" - o que ainda está em
    /// teste fica dentro da versão em aberto até esse momento.
    /// </summary>
    public partial class PatchNotesScreen : Control
    {
        private VBoxContainer _versionsContainer;
        private RichTextLabel _contentLabel;
        private Button _backButton;

        private readonly List<Button> _versionButtons = new();
        private readonly ButtonGroup _versionGroup = new();

        public event Action BackPressed;

        // Mais recente primeiro - é a que abre selecionada por padrão.
        private static readonly (string Title, string Body)[] Versions =
        {
            ("🆕 Versão 0.4", Version04Body),
            ("📦 Anterior à 0.4", BeforeVersion04Body),
        };

        public override void _Ready()
        {
            // O Center pausa a árvore inteira enquanto essa tela está aberta (ver
            // HUD.SyncPauseWithBlockingScreens) - sem isso os botões parariam de responder.
            ProcessMode = ProcessModeEnum.Always;

            _versionsContainer = GetNode<VBoxContainer>(
                "Window/MarginContainer/VBoxContainer/ContentRow/TopicsScroll/TopicsContainer"
            );

            _contentLabel = GetNode<RichTextLabel>(
                "Window/MarginContainer/VBoxContainer/ContentRow/ContentPanel/ContentScroll/ContentLabel"
            );

            _backButton = GetNode<Button>(
                "Window/MarginContainer/VBoxContainer/BackButton"
            );

            _backButton.Pressed += OnBackPressed;

            BuildVersionButtons();
        }

        private void BuildVersionButtons()
        {
            foreach (var (title, body) in Versions)
            {
                var button = new Button
                {
                    Text = title,
                    ToggleMode = true,
                    ButtonGroup = _versionGroup,
                    Alignment = HorizontalAlignment.Left,
                    ClipText = false,
                };

                button.AddThemeStyleboxOverride("normal", CreateVersionStyle(false));
                button.AddThemeStyleboxOverride("hover", CreateVersionStyle(false));
                button.AddThemeStyleboxOverride("pressed", CreateVersionStyle(true));

                button.Pressed += () => _contentLabel.Text = body;

                _versionsContainer.AddChild(button);
                _versionButtons.Add(button);
            }
        }

        private static StyleBoxFlat CreateVersionStyle(bool selected)
        {
            var style = new StyleBoxFlat
            {
                BgColor = selected
                    ? new Color(0.24705882f, 0.38431373f, 0.28627452f)
                    : new Color(0.1882353f, 0.21176471f, 0.23921569f),
                BorderColor = selected
                    ? new Color(0.40392157f, 0.7019608f, 0.44313726f)
                    : new Color(0.33333334f, 0.35686275f, 0.3882353f),
                ContentMarginLeft = 10,
                ContentMarginTop = 6,
                ContentMarginRight = 10,
                ContentMarginBottom = 6,
            };

            style.SetBorderWidthAll(1);
            style.SetCornerRadiusAll(6);

            return style;
        }

        public void Open()
        {
            Visible = true;

            if (_versionButtons.Count > 0)
                _versionButtons[0].ButtonPressed = true;

            _contentLabel.Text = Versions[0].Body;
        }

        private void OnBackPressed()
        {
            Visible = false;
            BackPressed?.Invoke();
        }

        private const string Version04Body =
            "[b]Área de Exploração[/b]\n" +
            "Reformulação completa do mapa da Floresta Inicial: biomas por região (clareira " +
            "aberta na entrada, bosque denso, moita espinhenta, afloramento de pedra), NPCs " +
            "escondidos de propósito atrás de árvores com um selvagem guardando o único " +
            "acesso, duas NPCs novas (Palmon e Tortamon), selvagens e itens redistribuídos com " +
            "lógica de lugar.\n\n" +
            "Clicar num selvagem ou NPC de longe não inicia mais a interação à distância - só " +
            "manda o Digimon andar até lá; a batalha/diálogo só acontece de perto.\n\n" +
            "[b]NPCs Recrutados[/b]\n" +
            "Novo sistema: entregar a quest de um NPC recrutável passa a dar um bônus passivo " +
            "de verdade pro Center. Gaogamon e Togemon bonificam ainda mais a área de treino " +
            "específica deles e fazem Digimons ociosos irem treinar lá sozinhos de vez em " +
            "quando; Palmon deixa 1-2 Carnes de graça todo dia.\n\n" +
            "[b]Correções[/b]\n" +
            "- Diálogo de NPC disparava erro toda vez que abria (sinal do botão de ação sendo " +
            "conectado/desconectado errado).\n" +
            "- Seleção de Digimon pra explorar podia travar o botão Explorar por causa da " +
            "ordem dos sinais do grupo de botões.\n" +
            "- Treino autônomo tinha uma corrida que podia colocar dois Digimons na mesma área " +
            "de treino específica ao mesmo tempo.\n" +
            "- Enciclopédia mostrava \"???\" pra Digimon de saves antigos, mesmo já tendo um no " +
            "Center.\n\n" +
            "[b]Tutorial[/b]\n" +
            "Três tópicos novos: Exploração, NPCs Recrutados, Enciclopédia.\n\n" +
            "[b]Digimons novos[/b]\n" +
            "Os 3 Lordes Demônios que faltavam (Lucemon, Barbamon, Belphemon - Sleep Mode e " +
            "Rage Mode como formas separadas), e uma leva grande dos Cavaleiros Reais: " +
            "Kentaurusmon, as duas linhas do Dracomon (verde e azul), UlforceVeedramon, " +
            "Magnamon, Crusadermon, Alphamon (com as duas linhas alternativas do Dorumon), " +
            "Jesmon, além de Craniummon, Dynasmon, Duftmon e Gankoomon soltos.";

        private const string BeforeVersion04Body =
            "[b]Sistemas de base[/b]\n" +
            "Center com grid hexagonal de áreas, save/load, relógio de jogo, fome/doença, " +
            "evolução, loja.\n\n" +
            "[b]Batalha[/b]\n" +
            "Sistema de batalha 3x3 em tempo real, ovos, e recompensas.\n\n" +
            "[b]Exploração (primeira versão)[/b]\n" +
            "Áreas de exploração com Digimon selvagem, NPCs com diálogo e quests simples, " +
            "coleta de itens, e a Enciclopédia (estrutura inicial).\n\n" +
            "[b]Conteúdo[/b]\n" +
            "Vários Digimons e evoluções adicionados ao longo do desenvolvimento.";
    }
}
