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
            ("🆕 Versão 0.12", Version12Body),
            ("📦 Versão 0.11", Version11Body),
            ("📦 Versão 0.10", Version10Body),
            ("📦 Versão 0.9", Version09Body),
            ("📦 Versão 0.8", Version08Body),
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

        private const string Version12Body =
            "Em desenvolvimento - o que já entrou:\n" +
            "\n" +
            "[b]Caverna Cristalina nova: o escuro[/b]\n" +
            "A Caverna Cristalina foi refeita no estilo isométrico da Floresta Inicial, e o escuro é a mecânica principal: fora de uma luz não se vê nada - nem os selvagens, nem os baús, nem os NPCs. O Digimon que explora leva uma luz em volta dele, que fica maior se ele for do elemento fogo, maior ainda com a lanterna e máxima com as duas coisas. Lanternas, cristais, cogumelos, telas e a ponte de dados brilham no escuro.\n" +
            "\n" +
            "Entra-se pela mina da floresta, e dá pra voltar andando pela boca da caverna, a oeste. Três quests novas: [b]A Lanterna Perdida[/b] (o Groundramon, minerador do acampamento: siga os trilhos no escuro até o vagonete descarrilado - a recompensa é a lanterna e a passagem pra galeria funda), [b]Energia nas Ruínas[/b] (o Tentomon: religue as 3 caixas de junção espalhadas pela caverna pra acender as ruínas digitais e ligar a ponte de dados sobre o abismo) e a opcional [b]A Galeria Acesa[/b] (o PetiMeramon: acenda as 4 lanternas apagadas - precisa de fogo - pra quebrar a barreira de cristal e abrir uma gruta secreta). Candlemon, Stingmon, Sunarizamon e Phascomon continuam na caverna, com as mesmas quests. A saída norte, depois da ponte, ainda está bloqueada.\n" +
            "\n" +
            "[b]Correções[/b]\n" +
            "- Nas batalhas contra selvagens da exploração, o fundo da área (verde na floresta) tampava o céu da arena e o botão 🚪 Sair aparecia por baixo do Pausa.";

        private const string Version11Body =
            "[b]Passivas herdadas[/b]\n" +
            "Cada forma por onde o Digimon passa deixa uma passiva herdada, que ele carrega pelo resto da vida: um Greymon que foi Koromon e Agumon leva as passivas dos dois pra batalha. Todas as espécies de Baby a Ultimate têm a sua (bônus de ataque ou defesa, perfurar defesa, golpe extra, cura, esquiva, atordoamento...). Passar duas vezes pela mesma espécie conta uma vez só. Os Digimons que já existiam no save começam com o histórico vazio e passam a acumular a partir da próxima evolução.\n" +
            "\n" +
            "A ficha do Digimon mostra a lista \"🧬 Passivas herdadas\", com rolagem, e os dados curtos da ficha foram pra duas colunas pra abrir espaço.\n" +
            "\n" +
            "[b]Jogress: linha principal[/b]\n" +
            "Na montagem da Jogress, o botão \"Linha principal\" escolhe de qual dos dois Digimons vem primeiro o histórico do fundido. O resultado herda a linha evolutiva dos dois.\n" +
            "\n" +
            "[b]Barra de botões[/b]\n" +
            "O botão de sair (🚪) passou a ser o último da barra, depois de Notas de Atualização e Jogress.\n" +
            "\n" +
            "[b]Correções[/b]\n" +
            "- Na exploração, os Digimons selvagens continuavam andando durante a conversa com um NPC e podiam começar uma batalha no meio do diálogo. Agora o mapa fica parado enquanto o diálogo está aberto.";

        private const string Version10Body =
            "[b]Calendário e temporadas[/b]\n" +
            "O jogo agora tem um calendário: o ano (a temporada) tem 60 dias, divididos em 4 estações de 15 dias - Primavera, Verão, Outono e Inverno. O ano vira no dia 61 do relógio, e tudo que é \"da temporada\" (medalhas, Grande Evento) recomeça do zero.\n" +
            "\n" +
            "[b]Campeonatos com data[/b]\n" +
            "Os campeonatos deixaram de estar sempre disponíveis: cada um abre em datas próprias do calendário. Alguns voltam de tempos em tempos (a Copa Novato abre 3 dias a cada 6, o Torneio Rookie 4 a cada 10, o Trio Guardião 02 5 a cada 20), outros abrem numa janela (Desafio Champion dos dias 10 a 25, Trio Tamers de 20 a 35) e os demais em dias fixos do ano (ex.: Copa Ultimate no dia 30, Copa Mega no dia 55). Cada abertura é uma \"edição\": vencer fecha o campeonato até a próxima edição, perder não gasta nada - dá pra tentar de novo enquanto estiver aberto. A recompensa de Capacidade/Bits continua sendo só na primeira vitória de todas.\n" +
            "\n" +
            "Alguns campeonatos têm trava: o Trio Guardião 02 só abre depois de vencer o Desafio Champion, e a Copa das Lendas e a Copa Mega: Revanche depois de vencer a Copa Mega. A lista de campeonatos mostra o motivo de cada um estar indisponível (\"Abre em N dias\", \"Aberto por mais N dias\", \"Concluído nesta edição\", \"Bloqueado: vença ...\").\n" +
            "\n" +
            "[b]Regras de inscrição[/b]\n" +
            "Vários campeonatos agora limitam quem pode entrar, por estágio ou atributo: Só Rookie (Copa Novato, Torneio Rookie), Só Champion (Desafio Champion), Só Ultimate (Copa Ultimate, Trio Guardião 02), Só Mega (Copa Mega, Revanche - Mega+ como Omegamon e Examon também vale), Só Vírus (Trio das Trevas) e Só Vacina (Trio Celestial). A regra aparece no card do campeonato, e na montagem do time os Digimon que não cumprem ficam bloqueados com o motivo escrito.\n" +
            "\n" +
            "[b]Avisos de campeonato[/b]\n" +
            "Na virada do dia aparece um aviso quando um campeonato vai abrir (5 dias antes; o Grande Evento avisa 9 dias antes), no dia em que abre e no último dia (\"Último dia: ... fecha à meia-noite\"). Se o Center não tiver Digimon elegíveis suficientes, o aviso diz quantos faltam. A Copa Novato e o Torneio Rookie, que abrem com muita frequência, não avisam.\n" +
            "\n" +
            "[b]Medalhas da temporada[/b]\n" +
            "Vencer um campeonato dá uma medalha na temporada, de 1 a 3 pontos conforme a dificuldade (20 no total; a Copa Novato e o Torneio Rookie não dão). Cada campeonato dá a medalha uma vez por temporada, e elas zeram na virada do ano. A tela 🏅 Medalhas, no menu do troféu, mostra quais você já conquistou, quais ainda dá pra conquistar e quais estão bloqueadas ou encerradas.\n" +
            "\n" +
            "[b]Grande Evento[/b]\n" +
            "O clímax da temporada: um campeonato 3x3 sem restrição que abre só no último dia do ano (dia 60) e exige 12 medalhas da temporada pra se inscrever, contra Examon, Jesmon e Sakuyamon no nível 65. A vitória dá +25 de Capacidade, 80.000 Bits e o título de \"Campeão do Ano\". Se não der mais pra chegar a 12 medalhas no ano, ele aparece como \"Inalcançável nesta temporada\".\n" +
            "\n" +
            "[b]Barra superior: relógio e minicalendário[/b]\n" +
            "O canto esquerdo da barra superior foi reorganizado: o relógio (analógico e digital) fica com os botões ⏩ x2 e 💤 Pular sono logo embaixo, e ao lado um minicalendário mostra a estação e o ano, os 15 dias da estação com o dia de hoje destacado, as medalhas da temporada e o próximo campeonato. A data deixou de aparecer duas vezes.\n" +
            "\n" +
            "[b]Tutoriais automáticos[/b]\n" +
            "Num jogo novo, o básico aparece logo de cara (primeiros passos, necessidades do Digimon, relógio e ovos), e cada tela ou função mostra o tutorial dela na primeira vez que é usada (Loja, Inventário, Campeonatos, Exploração, Jogress, Guia de Evolução...). Cada tutorial aparece uma vez só, e todos continuam no botão 📖 pra consultar. Saves de antes começam com isso desligado - dá pra ligar em ⚙ Configurações.\n" +
            "\n" +
            "[b]Ovos arrastáveis[/b]\n" +
            "Os ovos agora podem ser arrastados pelo Center, igual aos Digimons. A área onde ele fica não muda nada nele - é só pra organizar. Soltar fora do Center devolve o ovo pra onde estava.\n" +
            "\n" +
            "[b]Menu de comida fica aberto[/b]\n" +
            "Ao alimentar, o menu de comidas continua aberto depois de soltar a comida no Center, pra colocar a próxima sem reabrir. Ele fecha clicando de novo no botão 🍖 ou em qualquer outro lugar.\n" +
            "\n" +
            "[b]Revisão dos requisitos de evolução[/b]\n" +
            "Os requisitos das ligações de evolução foram revistos (308 das 337 mudaram, nenhuma foi criada ou removida) pra resolver dois problemas: uma forma \"irmã\" mais forte aparecendo no lugar da que você estava mirando, e formas padrão que às vezes não dava pra alcançar. Nove espécies que não tinham nenhum caminho possível agora têm: Gabumon, Salamon, DemiVeemon, Dracomon, Dorimon, Bukamon, Hackmon, Minomon e Agumon (Savers). Nos estágios Baby e In-Training, cada forma agora tem uma assinatura em Ataque Especial e/ou Defesa Especial - treinar esses stats decide pra qual delas o Digimon vai. Growlmon vira WarGrowlmon por padrão e MetalGreymon treinando Ataque Especial. Confira os requisitos no Guia de Evolução (✨).\n" +
            "\n" +
            "[b]Correções[/b]\n" +
            "- 21 Digimons (entre eles Veemon, DemiVeemon, ExVeemon, Paildramon, Myotismon, Lilamon, Piedmon, Sakuyamon e Ophanimon) usavam o quadro de dormir como animação de ataque e treino. Os quadros foram reordenados.\n" +
            "- O texto da Enciclopédia no tutorial indicava o botão errado (📖 em vez de 📘).";

        private const string Version09Body =
            "[b]Mais tempo pra evoluir (todos os estágios)[/b]\n" +
            "Os tetos de idade em dias subiram: Baby 4, In-Training 7, Rookie 15, Champion 26, Ultimate 52 e Mega/Mega+/Special 68 (antes: 3, 5, 9, 15, 22 e 30). A ideia é que o Digimon só morra sem evoluir se você cuidar e treinar mal: com um esforço moderado (uns 4 combates por dia) ele chega pelo menos a uma forma mais fraca de cada estágio. As formas mais fortes pedem mais - Energético, mais combates ou um Center mais evoluído (áreas de treino específicas, NPCs recrutados, conjuntos completos).\n" +
            "\n" +
            "Chegar ao Mega deixou de ser praticamente impossível. Além dos tetos maiores, o nível exigido nas faixas mais fortes caiu um pouco: Rookie -> Champion de nível 20 pra 18, Champion -> Ultimate de 30 pra 28, e Ultimate -> Mega de 30 pra 28, de 40 pra 36 e de 42 pra 37.\n" +
            "\n" +
            "" +
            "[b]Morte súbita nas batalhas[/b]\n" +
            "Depois de 2 minutos de luta começa a morte súbita: todo mundo perde 3% da vida máxima por segundo, ignorando defesa, escudo e cura - então nenhuma luta dura pra sempre (antes, um Suporte de cura contra um Tank de pouco dano podia travar). Como a perda é proporcional à vida máxima, ela não favorece quem tem pouca vida nem pune quem tem muita: quem estiver com a menor porcentagem de vida cai primeiro, ou seja, vence quem causou mais dano relativo à vida máxima do alvo. Um aviso vermelho no topo faz a contagem nos últimos 15 segundos.\n" +
            "\n" +
            "[b]Novo fundo do Center: céu e nuvens[/b]\n" +
            "O fundo cinza do Center virou um céu que muda com a hora do relógio do jogo (madrugada, amanhecer, dia, pôr do sol e noite estrelada), com nuvens andando devagar, como se o Center fosse uma ilha flutuante. Cada sala ganhou sombra e uma base de pedra, e à noite as salas escurecem de leve. A arena de batalha também ganhou o mesmo céu, com a sombra e a base de pedra nos hexágonos.\n" +
            "\n" +
            "[b]Novo fundo nas janelas[/b]\n" +
            "As janelas do jogo (inventário, loja, enciclopédia, tutorial, batalha, exploração...), a barra superior com as informações e a barra de botões ganharam o fundo de hexágonos azuis do menu inicial. As caixas de confirmação (fechar o jogo, deletar Digimon) também foram refeitas nesse estilo, com o botão Cancelar já selecionado.\n" +
            "\n" +
            "[b]Tela de evolução sem capacidade[/b]\n" +
            "Ganhou o mesmo visual das outras telas. Continua igual: selecione Digimons pra obliterar até liberar a capacidade que falta, ou mantenha a evolução bloqueada até haver espaço.\n" +
            "\n" +
            "[b]Correções[/b]\n" +
            "- Pular o sono agora acorda o Center às 6h de qualquer horário entre 22h e 5h (parava 4 horas depois de começar).\n" +
            "- Digimons doentes agora continuam doentes depois de salvar e carregar (antes voltavam saudáveis, o que também zerava o risco de morte por doença).\n" +
            "- As funções dos NPCs recrutados (Carne diária da Palmon, consultor do Wizardmon, cozinha do Meramon) agora vêm dos dados de cada NPC.\n" +
            "\n" +
            "" +
            "[b]Cozinha do Meramon: Carne Assada[/b]\n" +
            "Depois de recrutar o Meramon (quest Brasas pro Meramon, no Vulcão Ardente), o Inventário ganha uma linha de cozinha: ele assa as Carnes cruas do seu estoque (\"Assar 1\" ou \"Assar todas\", 1 Carne crua vira 1 Carne Assada). Cada Carne custa 100 Bits e a encomenda inteira leva 20 minutos do relógio do jogo (acelera com o x2 e o Pular sono); só cabe uma encomenda por vez e um aviso aparece quando ela fica pronta. A Carne Assada é colocada no Center como a crua - pelo botão de comida, na terceira opção - mas sacia mais (30 de fome, contra 20) e demora o dobro do tempo pra estragar: 12 horas fora do Refeitório e 48 dentro dele (a crua dura 6 e 24). Dentro do Refeitório ela continua durando mais e ainda dá o bônus de recuperação de fome.\n" +
            "\n" +
            "" +
            "[b]Nova área: Vulcão Ardente[/b]\n" +
            "Um segundo caminho alternativo, que sai do Deserto Escaldante em vez do Cânion: um vulcão de rocha escura, fumarolas e colunas de basalto, com um rio de lava ao sul. Abre ao concluir a quest Ventos do Deserto (a mesma do Cânion) e tem nível recomendado 30, com selvagens de nível 28 a 32 (Growlmon, GeoGreymon, Kyubimon, Garurumon, Dracomon e Candlemon). Só dá pra andar até a beira da brasa - a lava não é caminho.\n" +
            "\n" +
            "Quem mora lá: Agumon (quest Trilhas de Cinzas), Renamon (caçada de Kyubimon), Tentomon (coleta de Pedras de Enxofre) e um Meramon escondido atrás das colunas. Entregar a quest do Meramon (coletar 4 Brasas Vulcânicas) recruta ele pro Center: ele vai trabalhar na cozinha do Center, assando as suas Carnes (veja Cozinha do Meramon, acima).\n" +
            "\n" +
            "Itens novos: Brasa Vulcânica (dropada por Growlmon, GeoGreymon e Candlemon) e Pedra de Enxofre (dropada por Garurumon, Dracomon e Kyubimon).\n" +
            "\n" +
            "" +
            "[b]Mais quests nas áreas novas[/b]\n" +
            "Cada uma das quatro áreas novas (Pântano, Praia, Deserto e Cânion) ganhou duas quests além da do guia:\n" +
            "- Uma quest de coleta, com o NPC escondido: junte 4 itens que só os selvagens da própria área soltam - Cogumelo do Pântano, Concha Perolada, Escama do Deserto e Fragmento de Fóssil - e entregue pra ele.\n" +
            "- Uma caçada, com um NPC novo à vista: derrote 2 selvagens de uma espécie específica (ShoeShoemon, Ikkakumon, Gladimon e BaoHackmon). O progresso vale entre visitas.\n" +
            "\n" +
            "As recompensas são Bits, Capacidade e itens (Energético e Remédio) - nenhum desses NPCs entra no Center.\n" +
            "\n" +
            "" +
            "[b]Nova área: Praia Dourada[/b]\n" +
            "Continuação do caminho alternativo da Floresta: uma praia de areia clara com o mar ao sul, depois do Pântano Sombrio. Abre ao concluir a quest Trilhas do Pântano e é um pouco mais difícil que o pântano (nível recomendado 24, selvagens de nível 22 a 26 - Gomamon, Elizamon, Bakemon, Dimetromon e Ikkakumon). Só dá pra caminhar até a beira da água: onde a areia vira onda e depois mar, o Digimon não passa. Quem mora lá: Muchomon (quest Maré Baixa) e um Lucemon escondido atrás das palmeiras e pedras. Tem palmeiras, guarda-sóis, castelos de areia, conchas, estrelas-do-mar e coral no mar, além de 4 itens espalhados pela areia.\n" +
            "\n" +
            "" +
            "[b]Nova área: Cânion Carmesim[/b]\n" +
            "A última etapa por enquanto: um cânion de paredões vermelhos, arcos de pedra, colunas e fósseis, além do Deserto Escaldante. Abre ao concluir a quest Ventos do Deserto e é a área mais difícil até agora (nível recomendado 32, selvagens de nível 30 a 34 - Veedramon, Reppamon, BaoHackmon, ExVeemon, Stingmon, Galemon, Coredramon Azul e Dorugamon). Quem mora lá: Dorumon (quest Rastro no Cânion) e um Gladimon escondido atrás das colunas e paredões. Tem 4 itens espalhados pelo chão.\n" +
            "\n" +
            "" +
            "[b]Novas áreas: Pântano Sombrio e Deserto Escaldante[/b]\n" +
            "Duas áreas novas de exploração, cada uma com chão e cenário próprios.\n" +
            "\n" +
            "O Pântano Sombrio é um caminho alternativo que sai da Floresta Inicial: abre logo ao concluir a quest Limpeza da Floresta, mas é mais difícil que a Caverna Cristalina (nível recomendado 20, selvagens de nível 18 a 22 - Shoemon, ShoeShoemon, Sunflowmon, Snatchmon, Palmon e Lalamon). Dá pra entrar antes da caverna, só que sem treino suficiente os selvagens de lá não perdoam. Quem mora lá: Gomamon (quest Trilhas do Pântano) e um Bakemon escondido entre os troncos.\n" +
            "\n" +
            "O Deserto Escaldante continua a caverna: abre ao concluir a quest Luz na Escuridão, do Candlemon, e é a área mais difícil até agora (nível recomendado 26, selvagens de nível 24 a 28 - Greymon, Kabuterimon, Devidramon, Gladimon, Raptordramon, Coredramon Verde, Togemon e Cockatrimon). Quem mora lá: Pteromon (quest Ventos do Deserto) e um Kotemon escondido atrás das rochas e ruínas.\n" +
            "\n" +
            "Cada área tem 4 itens espalhados pelo chão pra coletar.\n" +
            "\n" +
            "" +
            "[b]Nova área: Caverna Cristalina[/b]\n" +
            "A próxima etapa depois da Floresta Inicial: uma caverna escura de cristais, estalagmites e colunas de pedra, com nível recomendado 14 e selvagens bem mais fortes que os da floresta (Sunarizamon, Phascomon, Ghostmon, BEMmon, Wormmon e Tortamon, níveis 12 a 16). Ela só abre depois de concluir a quest Limpeza da Floresta - o cartão na lista de áreas (🌎) mostra o que falta enquanto estiver bloqueada.\n" +
            "\n" +
            "Quatro NPCs novos: o Candlemon, guia visível na entrada, e três escondidos atrás das formações de pedra - Stingmon, Sunarizamon e Phascomon -, cada um com uma quest. Recrutar os quatro completa, junto com os da Floresta, o bônus das seis áreas de treino específico: Ataque Especial (Candlemon), Velocidade (Stingmon), HP (Sunarizamon) e Defesa Especial (Phascomon).\n" +
            "\n" +
            "Novo item: Cristal de Caverna, solto por Sunarizamon e Phascomon selvagens (50% de chance) e usado na quest do Sunarizamon.\n" +
            "\n" +
            "[b]Áreas liberadas pela progressão[/b]\n" +
            "Uma área agora pode exigir a conclusão de uma quest pra abrir. Enquanto estiver bloqueada, o botão vira \"Bloqueada\" e o cartão explica qual quest concluir.\n" +
            "\n" +
            "[b]Visual novo: Floresta e Caverna[/b]\n" +
            "Árvores, arbustos, pedras, flores, cogumelos e gramas da Floresta Inicial foram redesenhados com mais detalhe. As colisões de árvores e pedras foram reajustadas ao novo desenho (antes ficavam abaixo do tronco e mais estreitas que ele). O chão da Floresta Inicial e o da Caverna Cristalina agora são texturas ladrilhadas, no lugar de uma cor lisa.\n" +
            "\n" +
            "[b]Passivas para todos os Digimons[/b]\n" +
            "Todas as 199 espécies agora têm uma pool de 2 ou 3 passivas possíveis, da mesma Role dela (e, no Suporte, do mesmo tipo: Curador, Buffer ou Debuffer). Antes só 4 espécies tinham pool - o resto nascia sem passiva nenhuma. Ao nascer ou evoluir, o Digimon sorteia 1 delas, como sempre foi a regra. Digimons de saves antigos que estavam sem passiva recebem uma na primeira vez que o save é carregado; quem já tinha uma continua com a mesma.\n" +
            "\n" +
            "Consequência: Digimons selvagens e times inimigos de Batalha Livre também sorteiam passiva, então essas lutas ficam um pouco mais difíceis. Times de Campeonato não mudam." +
            "\n" +
            "\n" +
            "[b]Correção: comida e remédio travados[/b]\n" +
            "Segurar e arrastar Carne, Energético ou remédio podia travar a colocação pra sempre se o soltar do botão não fosse percebido pelo jogo (por exemplo, trocando de janela com Alt-Tab no meio do arrastar): dali em diante nada mais podia ser colocado até reabrir o jogo. Agora o arrastar é cancelado sozinho se a janela perder o foco ou se o botão do mouse for solto sem a colocação terminar, e dá pra cancelar de propósito apertando Esc. Cancelar não gasta o item.";

        private const string Version08Body =
            "[b]Menu Principal[/b]\n" +
            "O jogo agora abre num menu com Novo Jogo, Carregar Jogo e Opções (volume da música). Novo Jogo pede confirmação antes de apagar o save existente - o aviso mostra o dia, os Digimons, os ovos e os Bits que serão perdidos - e, sem nenhum save, começa direto. Carregar Jogo fica desativado quando não há save. O relógio do jogo fica parado enquanto o menu está aberto. O fundo tem uma grade de hexágonos e Digimons aleatórios comemorando pela tela, diferentes a cada vez que o jogo abre.\n" +
            "\n" +
            "[b]Teia Evolutiva Nova[/b]\n" +
            "A malha de evoluções foi refeita a partir de um mapa completo: 337 evoluções entre os 199 Digimons, e nenhum Digimon de meio de linha fica mais com uma única origem e uma única evolução - todos têm pelo menos duas entradas ou duas saídas. Várias evoluções antigas saíram ou mudaram de lugar (por exemplo, Tsunomon deixou de virar Renamon, Gigimon deixou de virar Agumon e Gatomon deixou de virar Angewomon direto). Toda evolução nova ganhou requisitos de nível, idade e stats; onde dois caminhos saem do mesmo Digimon, eles se diferenciam por stats, Felicidade, Disciplina ou batalhas, pra nenhum ficar escondido atrás do outro. As formas mais raras (Sakuyamon, MirageGaogamon, Sleipmon, Ophanimon, Lilamon e Imperialdramon) exigem Felicidade 40 ou mais; sem isso o Digimon segue pela alternativa que já existia.\n" +
            "\n" +
            "Também nova: troca de modo. Belphemon Sleep Mode pode virar Rage Mode (exige 15 batalhas, Felicidade e Disciplina baixas).\n" +
            "\n" +
            "[b]Jogress[/b]\n" +
            "Novo botão 🧬 na barra de controle: funde dois Digimons do Center num Digimon novo. Não exige nível nem stats - só os dois Digimons certos e Capacidade livre. O resultado é um Digimon novo, com a média dos stats dos dois (com bônus) puxada pra espécie resultante. Receitas: Omegamon (WarGreymon + MetalGarurumon), Examon (Slayerdramon + Breakdramon) e Paildramon (ExVeemon + Stingmon). Essas três formas só saem por Jogress - nenhuma evolução comum chega nelas.\n" +
            "\n" +
            "[b]Digimons novos (26)[/b]\n" +
            "- [b]Megas[/b]: Sakuyamon, MirageGaogamon, Sleipmon, Ophanimon, Piedmon, Imperialdramon (Dragon Mode) e GrandisKuwagamon.\n" +
            "- [b]Ultimates[/b]: Lilamon, LadyDevimon, SkullSatamon, Myotismon, Paildramon (só por Jogress) e JewelBeemon.\n" +
            "- [b]Champions[/b]: ExVeemon e Stingmon.\n" +
            "- [b]Rookie[/b]: Wormmon.\n" +
            "- [b]In-Training[/b]: DemiVeemon, Dorimon, Budmon e Minomon.\n" +
            "- [b]Babies[/b]: Chibomon, Dodomon, Zerimon, Pabumon, Nyokimon e Leafmon.\n" +
            "\n" +
            "A linha do Wormmon é nova por inteira: Leafmon → Minomon → Wormmon → Stingmon → JewelBeemon → GrandisKuwagamon, com ramificações em todos os estágios (por exemplo, o Wormmon também vira Kabuterimon ou Togemon, e o JewelBeemon também vira HerculesKabuterimon ou Imperialdramon).\n" +
            "\n" +
            "[b]Correção[/b]\n" +
            "Onze Digimons (Candlemon, Devidramon, Devimon, Geogreymon, IceDevimon, MachGaogamon, Mistymon, RizeGreymon, Sunflowmon, Taomon e Wizardmon) tinham o arquivo de evoluções fora da lista do projeto - funcionava no editor, mas uma versão exportada ficaria sem as evoluções deles.";

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
