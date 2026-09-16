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
            ("🆕 Versão 0.8", Version08Body),
            ("📦 Versão 0.7", Version07Body),
            ("📦 Versão 0.6", Version06Body),
            ("📦 Versão 0.5", Version05Body),
            ("📦 Versão 0.4", Version04Body),
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

        private const string Version08Body =
            "Em desenvolvimento.";

        private const string Version07Body =
            "[b]Ilustrações do Center[/b]\n" +
            "Todos os hexágonos do Center ganharam arte de verdade no lugar dos ícones " +
            "desenhados por código: Treino (genérico), Dormitório, Refeitório, Hospital, área " +
            "Neutra e as 6 áreas de treino específicas (HP, Ataque, Defesa, Ataque Especial, " +
            "Defesa Especial, Velocidade) - cada uma com ilustração própria preenchendo o " +
            "hexágono inteiro. A borda colorida que contornava os hexágonos (só existia pra " +
            "identificar o tipo da área) foi removida - a ilustração já deixa isso claro " +
            "sozinha.\n\n" +
            "[b]Colisão nas Paredes do Center[/b]\n" +
            "As ilustrações acima desenham parede no topo de cada hexágono (fundo + as duas " +
            "laterais) - Digimons não andam mais em cima dela nem são soltos ali; a área " +
            "andável agora respeita o formato de cada parede.\n\n" +
            "[b]Menu de Pausa na Batalha[/b]\n" +
            "Botão \"Pausa\" (ou Esc) na arena de batalha, com Continuar ou Desistir da " +
            "batalha - útil quando dois Digimons muito tanques ficam trocando dano " +
            "indefinidamente sem nenhum morrer. Desistir conta como derrota normal.";

        private const string Version06Body =
            "[b]Malha Evolutiva Mais Interligada[/b]\n" +
            "13 novas evoluções conectando Digimons que antes não tinham nenhum jeito de " +
            "serem obtidos (Veemon, Motimon, Tokomon e outros 8), todas reaproveitando " +
            "espécies e evoluções que já existiam. Corrigida também uma evolução antiga sem " +
            "sentido (Yuramon virando Punimon - Baby virando Baby, removida).\n\n" +
            "[b]Ajuste na Idade Máxima[/b]\n" +
            "Baby (2→3 dias) e Baby II/In-Training (4→5 dias) ganharam um pouco mais de " +
            "folga antes de morrer de velhice - o teto era apertado demais pra quem ainda tá " +
            "aprendendo a treinar logo no início. Os estágios seguintes continuam como " +
            "estavam.\n\n" +
            "[b]Novo Item: Energético[/b]\n" +
            "Funciona como a Carne - coloca no Center e os Digimons comem sozinhos -, mas " +
            "recupera Stamina em vez de Fome. Sozinho, um Digimon recupera a Stamina " +
            "inteira; se mais de um comer junto, a Stamina é dividida entre eles, igual já " +
            "acontecia com a Carne. Custa 2.000 Bits na Loja. Segurar o botão de Carne (🍖) " +
            "agora abre uma mini janela com as duas opções antes de arrastar pro Center.\n\n" +
            "[b]Correção[/b]\n" +
            "O NPC guia da Floresta Inicial estava cadastrado por engano como um segundo " +
            "\"Wizardmon\", duplicando o nome do Wizardmon de verdade (o recrutável, que " +
            "desbloqueia bloqueio de evolução) no mesmo mapa. Virou Gabumon.\n\n" +
            "[b]Linha do Digiovo de Fogo[/b]\n" +
            "Mokumon passa a ser Fogo (era Trevas) e vira a porta de entrada de uma linha " +
            "própria: Mokumon → PetiMeramon → Candlemon (novo) → Wizardmon → Mistymon (novo) " +
            "→ Dynasmon/Craniummon. PetiMeramon e Wizardmon ganharam uma segunda opção de " +
            "evolução além das linhas de Trevas/Destromon que já existiam (nenhuma das duas " +
            "foi alterada); Mistymon pode virar Dynasmon (exige 10 batalhas) ou Craniummon " +
            "(exige Disciplina alta) - três bifurcações usando quase só espécies que já " +
            "existiam no jogo.\n\n" +
            "[b]Conjuntos[/b]\n" +
            "Novo sistema: grupos temáticos de Digimon que dão um bônus permanente ao Center " +
            "quando todas as espécies da lista já foram descobertas na Enciclopédia pelo menos " +
            "uma vez - vale mesmo que nenhuma esteja viva agora, já que o progresso é do " +
            "Center, não do Digimon. Primeiro conjunto: Lordes Demoníacos (Lucemon Satan Mode, " +
            "Barbamon, Belphemon Rage Mode, Beelzebumon, Leviamon, Lilithmon, Demon), que dá " +
            "+10% de dano final em batalha pra qualquer Digimon do time quando completo. Nova " +
            "aba \"Conjuntos\" na Enciclopédia (📖) mostra os conjuntos existentes, quantos " +
            "membros faltam, e o efeito de cada um no tooltip do nome. Barbamon, que não tinha " +
            "nenhum caminho de evolução até agora, ganhou um: Destromon pode virar Ragnamon " +
            "(como antes) ou Barbamon (exige Disciplina baixa).\n\n" +
            "Mais quatro conjuntos, cada um com um tipo de bônus diferente (todos empilham " +
            "entre si, e com o dos Lordes):\n" +
            "- [b]Parceiros do Adventure[/b] (Agumon, Gabumon, Biyomon, Tentomon, Palmon, " +
            "Gomamon, Patamon, Gatomon): +10% em toda experiência ganha, treino ou batalha.\n" +
            "- [b]Parceiros do Tamers[/b] (Guilmon, Terriermon, Renamon, Impmon): Fome cai 15% " +
            "mais devagar.\n" +
            "- [b]Parceiros do Data Squad[/b] (Agumon Savers, Gaomon, Lalamon): +10% nos Bits " +
            "ganhos em batalha.\n" +
            "- [b]Adventure - Formas de Batalha[/b] (Greymon, Garurumon, Birdramon, " +
            "Kabuterimon, Togemon, Ikkakumon, Angemon, Angewomon): +8 pontos percentuais de " +
            "chance de crítico em batalha.";

        private const string Version05Body =
            "[b]Bloqueio de Evoluções[/b]\n" +
            "Novo NPC recrutável, Wizardmon (escondido na Floresta Inicial) - diferente dos " +
            "outros NPCs recrutáveis, não dá nenhum bônus automático, mas desbloqueia a opção " +
            "de bloquear evoluções específicas no Guia de Evolução (✨): um botão " +
            "\"Bloquear\" em cada evolução possível de um Digimon. Uma evolução bloqueada " +
            "nunca acontece, mesmo que os requisitos sejam atendidos, o que dá pra usar pra " +
            "recusar de propósito a evolução mais fácil de um Digimon e esperar juntar os " +
            "stats de uma alternativa mais forte. Dá pra marcar e desmarcar quando quiser, " +
            "sem custo.\n\n" +
            "[b]Novos requisitos de evolução[/b]\n" +
            "Evoluções agora também podem exigir número mínimo de batalhas e/ou taxa de " +
            "vitória (evita evoluir só de XP de consolação perdendo de propósito), e " +
            "Disciplina/Felicidade mínima ou máxima (formas mais \"malignas\" exigem " +
            "Disciplina baixa; formas mais \"queridas\" pela fanbase exigem Felicidade alta, e " +
            "algumas mais rejeitadas exigem o contrário). Vários pares de evolução alternativa " +
            "que tinham requisitos quase idênticos entre si foram rebalanceados: Patamon, " +
            "Guilmon, Impmon, MetalGreymon, WarGrowlmon, Veemon, Chaperomon, Fantomon, " +
            "Lamiamon, Astamon, GrandGalemon e Triceramon.\n\n" +
            "[b]Sistema de Morte[/b]\n" +
            "Digimons agora podem morrer. De velhice, se passarem da idade máxima do estágio " +
            "atual (a idade nunca reseta ao evoluir - evoluir a tempo é o que evita isso). Ou " +
            "por maus-tratos, com chance crescente por doença prolongada sem tratamento, " +
            "derrotas seguidas em batalha, nocautes acumulados ao longo da vida, ser escalado " +
            "fraco demais pra batalhar repetidas vezes, ou ambiente sujo por tempo demais. Um " +
            "aviso explica o motivo quando acontece. Saves de antes dessa atualização com " +
            "Digimon já acima do novo teto de idade ganham 1 dia de folga na primeira carga, " +
            "em vez de morrer de surpresa. Novo tópico no Tutorial (💀 Morte) com todos os " +
            "detalhes; o tópico ✨ Evolução também foi atualizado.\n\n" +
            "[b]Treino Autônomo por Digimon[/b]\n" +
            "A tela de Detalhes do Digimon (botão ℹ) ganhou a seção \"Treino Autônomo\": um " +
            "\"Permitir\" por stat (HP, Ataque, Defesa, Ataque Especial, Defesa Especial, " +
            "Velocidade) - desmarcar impede aquele Digimon específico de ir sozinho treinar " +
            "esse stat quando um NPC recrutado libera o bônus da área, sem afetar treino " +
            "manual. Resolve Digimons treinando sozinhos num stat que puxa pra uma evolução " +
            "indesejada.\n\n" +
            "[b]Tipos de Digiovo[/b]\n" +
            "Cada espécie Baby agora choca com o sprite do seu EggType de verdade (Dragão, " +
            "Fera, Água, Floresta, Fogo, Trevas ou Luz), em vez do Digitama genérico antigo. " +
            "Comprar um ovo na Loja abre uma tela pra escolher entre \"Aleatório\" (qualquer " +
            "tipo, preço normal) ou um tipo específico (garante o EggType, mas custa 3x mais - " +
            "o Baby exato dentro do tipo continua sendo sorteado). Tópico 🥚 Ovos do Tutorial " +
            "atualizado.";

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
            "[b]Sistema de Passivas[/b]\n" +
            "Novo sistema: cada Digimon pode ter 1 passiva ativa por vez, sorteada entre 31 " +
            "passivas diferentes (divididas por Role - Tank, Warrior, Assassin, Ranged e as " +
            "três variações de Suporte). O sorteio acontece ao nascer de um ovo ou ao evoluir, " +
            "e a passiva fica fixa até a próxima evolução. Sunflowmon e Shoemon viraram " +
            "Debuffer (eram Buffer), pra existir Suporte representando as três variações. " +
            "Digimons de saves salvos antes dessa atualização recebem a passiva automaticamente " +
            "na primeira vez que o save é carregado.\n\n" +
            "[b]Detalhes do Digimon[/b]\n" +
            "Novo botão ℹ na barra de status do Digimon selecionado, abrindo uma janela com " +
            "informações que não cabiam na tela principal: Role, Atributo, Elemento, Tipo de " +
            "Ovo, Tipo de Ataque (e Suporte, se for o caso), Idade, Capacidade e a passiva " +
            "ativa.\n\n" +
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
            "Quatro tópicos novos: Exploração, NPCs Recrutados, Enciclopédia, Passivas.\n\n" +
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
