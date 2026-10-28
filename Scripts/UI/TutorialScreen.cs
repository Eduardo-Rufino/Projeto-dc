using Godot;
using System;
using System.Collections.Generic;

namespace ProjetoDC.Scripts.UI
{
    /// <summary>
    /// Lista de tutoriais sobre as mecânicas já implementadas no jogo - puramente
    /// informativo, sem nenhuma interação com o GameManager. Os tópicos ficam fixos em
    /// código (_topics) porque são texto de ajuda, não dado de save/gameplay.
    /// </summary>
    public partial class TutorialScreen : Control
    {
        private VBoxContainer _topicsContainer;
        private RichTextLabel _contentLabel;
        private Button _backButton;

        private readonly List<Button> _topicButtons = new();
        private readonly ButtonGroup _topicGroup = new();

        public event Action BackPressed;

        // O Id é o que vai no save (CenterState.SeenTutorialIds) e o que o Center usa pra disparar o
        // tópico automaticamente (ver Center.Tutorials.cs) - não renomear um Id existente.
        private static readonly (string Id, string Title, string Body)[] Topics =
        {
            ("primeiros_passos", "👋 Primeiros Passos", PrimeirosPassosBody),
            ("vantagens_tipo", "⚔️ Vantagens de Tipo", VantagensDeTipoBody),
            ("areas_center", "🏝️ Áreas do Centro de Treinamento", AreasDoCenterBody),
            ("editor_bases", "🏗️ Editor de Bases", EditorDeBasesBody),
            ("batalha", "🥊 Batalha", BatalhaBody),
            ("passivas", "✨ Passivas", PassivasBody),
            ("campeonatos", "🏆 Campeonatos x Batalha Livre", CampeonatosBody),
            ("calendario", "📅 Calendário", CalendarioBody),
            ("exploracao", "🌎 Exploração", ExploracaoBody),
            ("npcs_recrutados", "🤝 NPCs Recrutados", NpcsRecrutadosBody),
            ("evolucao", "✨ Evolução", EvolucaoBody),
            ("jogress", "🧬 Jogress", JogressBody),
            ("morte", "💀 Morte", MorteBody),
            ("ovos", "🥚 Ovos", OvosBody),
            ("necessidades", "❤️ Necessidades do Digimon", NecessidadesBody),
            ("inventario", "🎒 Inventário", InventarioBody),
            ("enciclopedia", "📘 Enciclopédia", EnciclopediaBody),
            ("relogio", "🕐 Relógio e Sono", RelogioBody),
            ("menu_save", "💾 Menu Principal e Save", MenuESaveBody),
            ("renomear", "✏️ Renomear Digimon", RenomearBody),
        };

        public override void _Ready()
        {
            // O Center pausa a árvore inteira enquanto essa tela está aberta (ver
            // HUD.SyncPauseWithBlockingScreens) - sem isso os botões parariam de responder.
            ProcessMode = ProcessModeEnum.Always;

            _topicsContainer = GetNode<VBoxContainer>(
                "Window/MarginContainer/VBoxContainer/ContentRow/TopicsScroll/TopicsContainer"
            );

            _contentLabel = GetNode<RichTextLabel>(
                "Window/MarginContainer/VBoxContainer/ContentRow/ContentPanel/ContentScroll/ContentLabel"
            );

            _backButton = GetNode<Button>(
                "Window/MarginContainer/VBoxContainer/BackButton"
            );

            _backButton.Pressed += OnBackPressed;

            BuildTopicButtons();
        }

        private void BuildTopicButtons()
        {
            foreach (var (_, title, body) in Topics)
            {
                var button = new Button
                {
                    Text = title,
                    ToggleMode = true,
                    ButtonGroup = _topicGroup,
                    Alignment = HorizontalAlignment.Left,
                    ClipText = false,
                };

                button.AddThemeStyleboxOverride("normal", CreateTopicStyle(false));
                button.AddThemeStyleboxOverride("hover", CreateTopicStyle(false));
                button.AddThemeStyleboxOverride("pressed", CreateTopicStyle(true));

                button.Pressed += () => _contentLabel.Text = body;

                _topicsContainer.AddChild(button);
                _topicButtons.Add(button);
            }
        }

        private static StyleBoxFlat CreateTopicStyle(bool selected)
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

            if (_topicButtons.Count > 0)
                _topicButtons[0].ButtonPressed = true;

            _contentLabel.Text = Topics[0].Body;
        }

        /// <summary>Título e texto de um tópico pelo Id (ver Topics) - usado pelos tutoriais
        /// automáticos (TutorialPopup). False se o Id não existe.</summary>
        public static bool TryGetTopic(string id, out string title, out string body)
        {
            foreach (var topic in Topics)
            {
                if (topic.Id == id)
                {
                    title = topic.Title;
                    body = topic.Body;
                    return true;
                }
            }

            title = null;
            body = null;
            return false;
        }

        private void OnBackPressed()
        {
            Visible = false;
            BackPressed?.Invoke();
        }

        private const string PrimeirosPassosBody =
            "Bem-vindo ao centro de treinamento! Aqui você cria Digimons, cuida deles e os leva a campeonatos. " +
            "Os Digimons vivem um tempo limitado, mas cada vitória deixa o centro de treinamento maior e mais forte.\n\n" +
            "[b]Cuidar[/b]: clique num Digimon pra ver o estado dele. Arraste a Carne (🍖) pra alimentar, o Remédio (💊) " +
            "pra curar doença e use 🧹 pra limpar o cocô.\n\n" +
            "[b]Crescer[/b]: coloque o Digimon na área de Treino e leve-o pra batalhar (🏆). Ele evolui sozinho ao " +
            "cumprir os requisitos - e precisa evoluir antes de ficar velho demais.\n\n" +
            "[b]Expandir[/b]: compre áreas e ovos na Loja (🏪) com os Bits das batalhas.\n\n" +
            "Todos os tutoriais ficam no botão 📖.";

        private const string VantagensDeTipoBody =
            "[b]Atributo[/b]: Vacina > Vírus > Data > Vacina. Unknown vence todos, menos Free; Free perde pra todos, menos Unknown.\n\n" +
            "[b]Elemento[/b]:\n" +
            "Fogo > Planta > Água > Fogo\n" +
            "Elétrico > Vento > Terra > Elétrico\n" +
            "Luz ↔ Trevas (um vence o outro)\n" +
            "Neutro não tem vantagem nem desvantagem.\n\n" +
            "Vantagem aumenta o dano e desvantagem diminui. Ter vantagem nos dois ao mesmo tempo dobra o dano. " +
            "O multiplicador aparece ao lado do número de dano.";

        private const string AreasDoCenterBody =
            "O centro de treinamento é dividido em áreas hexagonais. Compre novas na Loja e coloque nos hexágonos livres.\n\n" +
            "[b]Neutra[/b]: onde os Digimons ficam quando não têm nada pra fazer.\n\n" +
            "[b]Treino[/b]: o Digimon treina um status aleatório. As áreas de treino especializado (HP, Ataque, Defesa, " +
            "Ataque Especial, Defesa Especial ou Velocidade) treinam sempre o mesmo status, rendem um pouco mais e aceitam " +
            "1 Digimon por vez.\n\n" +
            "[b]Dormitório[/b]: dormir aqui recupera mais stamina e deixa o Digimon mais feliz. Dormir fora dele deixa o Digimon infeliz.\n\n" +
            "[b]Refeitório[/b]: a carne colocada aqui demora muito mais pra estragar e alimenta um pouco mais. Comer carne " +
            "estragada deixa o Digimon infeliz e pode deixá-lo doente.\n\n" +
            "[b]Carne Assada[/b]: com o Meramon recrutado, dá pra assar Carne pelo Inventário. Ela alimenta mais e demora o dobro pra estragar.\n\n" +
            "[b]Hospital[/b]: recupera HP bem mais rápido. Doença se cura com remédio, em qualquer lugar.";

        private const string EditorDeBasesBody =
            "O botão 🏗️ mostra as áreas construídas no centro de treinamento.\n\n" +
            "[b]Remover[/b] guarda a área no inventário, sem perdê-la. Não dá pra remover a área inicial nem uma área com Digimon dentro.\n\n" +
            "[b]Colocar[/b] uma área guardada num hexágono livre não custa Bits.\n\n" +
            "Pra mudar uma área de lugar, remova e coloque de novo.";

        private const string BatalhaBody =
            "As batalhas acontecem sozinhas, em tempo real. Quanto mais Velocidade, mais cedo o Digimon age; Ataque e Defesa " +
            "decidem o dano, e às vezes sai um golpe crítico.\n\n" +
            "Clique em qualquer Digimon durante a luta pra ver a vida dele.\n\n" +
            "[b]Morte súbita[/b]: as curas enfraquecem com o tempo, e depois de 2 minutos todos começam a perder vida - " +
            "quem estiver mais machucado cai primeiro.\n\n" +
            "Vencer dá experiência e Bits. Perder ainda dá um pouco de experiência.";

        private const string PassivasBody =
            "Cada Digimon tem uma passiva ativa, um efeito extra na batalha (dano extra, cura, escudo, crítico...). " +
            "Ela é sorteada ao nascer e sorteada de novo a cada evolução.\n\n" +
            "[b]Passivas herdadas[/b]: cada forma por onde o Digimon passou deixa uma passiva a mais, que ele carrega pra sempre. " +
            "Quanto mais formas na história dele, mais passivas.\n\n" +
            "Pra ver as passivas de um Digimon, selecione ele e clique no botão ℹ.";

        private const string CampeonatosBody =
            "O botão 🏆 oferece dois tipos de combate:\n\n" +
            "[b]Batalha Livre[/b]: um time inimigo do mesmo nível que o seu. Dá experiência e Bits.\n\n" +
            "[b]Campeonato[/b]: lutas fixas e mais difíceis. Não dão experiência, mas a primeira vitória em cada um " +
            "aumenta a Capacidade do centro de treinamento - é o principal jeito de crescer.\n\n" +
            "Cada campeonato abre só em certas datas (veja 📅 Calendário), e alguns só aceitam certos estágios ou atributos " +
            "(\"Só Rookie\", \"Só Vírus\"...). Perder não custa nada: dá pra tentar de novo enquanto estiver aberto.";

        private const string CalendarioBody =
            "O ano tem 60 dias, divididos em 4 estações. O minicalendário na barra superior mostra o dia de hoje e o próximo campeonato.\n\n" +
            "[b]Campeonatos[/b]: cada um abre em datas próprias. Um aviso aparece quando um campeonato vai abrir e no último dia dele.\n\n" +
            "[b]Medalhas[/b]: vencer campeonatos dá medalhas, que zeram na virada do ano. Veja as suas em 🏅 Medalhas, no menu do troféu.\n\n" +
            "[b]Grande Evento[/b]: no último dia do ano, quem tiver 12 medalhas pode disputar o título de Campeão do Ano.";

        private const string ExploracaoBody =
            "No botão 🌎, escolha uma área e 1 Digimon pra explorar. Ande com WASD, setas ou clicando no chão.\n\n" +
            "[b]Selvagens[/b]: perseguem você quando chega perto, e encostar neles começa uma batalha. Vencer dá experiência e pode render itens.\n\n" +
            "[b]NPCs[/b]: chegue perto e clique pra conversar. Alguns oferecem quests - cumpra o objetivo e volte pra receber a recompensa. " +
            "Alguns ficam escondidos, então explore todo canto.\n\n" +
            "[b]Itens[/b]: clique de perto pra coletar.\n\n" +
            "[b]Áreas novas[/b]: as áreas bloqueadas (🔒) abrem ao concluir certas quests, e o cartão mostra qual falta.";

        private const string NpcsRecrutadosBody =
            "Alguns NPCs da exploração passam a ajudar o centro de treinamento depois que você conclui a quest deles. " +
            "O diálogo diz o que cada um faz.\n\n" +
            "Uns reforçam uma área de treino e levam os Digimons parados a treinar lá sozinhos. Outros deixam recursos todo dia, " +
            "e o Wizardmon permite bloquear evoluções no Guia de Evolução.\n\n" +
            "[b]Treino Autônomo[/b]: se um Digimon estiver treinando sozinho um status que você não quer, desmarque esse status " +
            "na tela de detalhes dele (botão ℹ).";

        private const string EvolucaoBody =
            "O Digimon evolui sozinho quando cumpre os requisitos da próxima forma: nível, idade, status e, em algumas formas, " +
            "batalhas vencidas, Felicidade ou Disciplina. Evoluir também ocupa mais Capacidade do centro de treinamento.\n\n" +
            "[b]Guia de Evolução[/b] (✨): mostra todos os caminhos de um Digimon e o que falta pra cada um. Treine os status " +
            "certos pra chegar na forma que você quer.\n\n" +
            "Quando vários caminhos estão liberados, o Digimon vai pro mais difícil. Com o Wizardmon recrutado, dá pra " +
            "bloquear um caminho no Guia pra esperar uma forma melhor.";

        private const string JogressBody =
            "O botão 🧬 funde 2 Digimons num Digimon novo, se a dupla for uma receita conhecida. Não precisa de nível nem de status, só da dupla certa e de Capacidade livre.\n\n" +
            "Os dois somem e o novo Digimon nasce mais forte quanto mais treinados eles estavam. O botão \"Linha principal\" escolhe de qual dos dois vem primeiro o histórico do novo Digimon.\n\n" +
            "[b]Receitas[/b]:\n" +
            "- Omegamon = WarGreymon + MetalGarurumon\n" +
            "- Examon = Slayerdramon + Breakdramon\n" +
            "- Paildramon = ExVeemon + Stingmon\n\n" +
            "O Guia de Evolução (✨) mostra com quem cada Digimon pode se fundir.";

        private const string MorteBody =
            "Digimons não vivem pra sempre.\n\n" +
            "[b]Velhice[/b]: cada estágio tem uma idade máxima. Se o Digimon passar dela sem evoluir, ele morre. Evoluir a tempo é o que o mantém vivo.\n\n" +
            "[b]Maus-tratos[/b]: descuido pode matar - doença sem tratamento por dias, muitas derrotas seguidas, batalhar " +
            "muito machucado ou ficar no meio do cocô por muito tempo.\n\n" +
            "Quando um Digimon morre, um aviso explica o motivo.";

        private const string OvosBody =
            "Ovos vêm da Loja e chocam sozinhos depois de um tempo. Cada ovo ocupa Capacidade do centro de treinamento.\n\n" +
            "Dá pra comprar um ovo aleatório ou escolher o tipo (Dragão, Fera, Água, Floresta, Fogo, Trevas ou Luz), que custa mais. " +
            "O desenho do ovo mostra o tipo.\n\n" +
            "Dá pra arrastar os ovos pelo centro de treinamento pra organizar.";

        private const string NecessidadesBody =
            "[b]Fome[/b]: coloque carne pra alimentar - no Refeitório ela dura mais.\n\n" +
            "[b]Stamina[/b]: gasta treinando e volta descansando, mais rápido no Dormitório.\n\n" +
            "[b]Felicidade e Disciplina[/b]: dependem de como o Digimon é cuidado e mudam as evoluções possíveis.\n\n" +
            "[b]Doença[/b]: use Remédio em qualquer lugar pra curar.\n\n" +
            "[b]Colocar itens[/b]: segure o botão do item, arraste até o lugar e solte. Esc cancela.";

        private const string InventarioBody =
            "O botão 🎒 mostra seus itens e quantidades. Passe o mouse sobre um item pra ver o que ele faz.";

        private const string EnciclopediaBody =
            "O botão 📘 mostra todas as espécies do jogo. As que você já teve aparecem completas, e as outras ficam como silhueta.\n\n" +
            "[b]Conjuntos[/b]: grupos temáticos de Digimon. Descobrir todos os membros de um conjunto dá um bônus permanente " +
            "pro centro de treinamento, mesmo que esses Digimons não estejam mais com você. Passe o mouse sobre o nome pra ver o bônus.";

        private const string RelogioBody =
            "O relógio na barra superior mostra a hora do dia. Embaixo dele:\n" +
            "- [b]⏩ x2[/b]: o tempo passa duas vezes mais rápido.\n" +
            "- [b]💤 Pular sono[/b]: quando todos estão dormindo, avança até eles acordarem.";

        private const string MenuESaveBody =
            "[b]Menu Principal[/b]: Carregar Jogo continua de onde você parou. Novo Jogo começa do zero e [b]apaga o save atual[/b] - o jogo pergunta antes.\n\n" +
            "[b]Salvamento[/b]: o jogo salva sozinho no fim de cada dia, em momentos importantes e ao sair.";

        private const string RenomearBody =
            "Clique no nome do Digimon selecionado, na barra superior, pra dar um apelido. Deixe vazio pra voltar ao nome da espécie.";
    }
}
