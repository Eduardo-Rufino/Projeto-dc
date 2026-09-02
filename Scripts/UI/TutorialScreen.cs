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

        private static readonly (string Title, string Body)[] Topics =
        {
            ("⚔️ Vantagens de Tipo", VantagensDeTipoBody),
            ("🏝️ Áreas do Center", AreasDoCenterBody),
            ("🏗️ Editor de Bases", EditorDeBasesBody),
            ("🥊 Batalha", BatalhaBody),
            ("🏆 Campeonatos x Batalha Livre", CampeonatosBody),
            ("✨ Evolução", EvolucaoBody),
            ("🥚 Ovos", OvosBody),
            ("❤️ Necessidades do Digimon", NecessidadesBody),
            ("🎒 Inventário", InventarioBody),
            ("🕐 Relógio e Sono", RelogioBody),
            ("✏️ Renomear Digimon", RenomearBody),
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
            foreach (var (title, body) in Topics)
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

        private void OnBackPressed()
        {
            Visible = false;
            BackPressed?.Invoke();
        }

        private const string VantagensDeTipoBody =
            "[b]Atributo[/b] (triângulo de vantagem, como pedra-papel-tesoura):\n" +
            "Vacina > Vírus > Data > Vacina.\n" +
            "Unknown tem vantagem contra todos, exceto Free. Free perde para todos, exceto Unknown.\n\n" +
            "[b]Elemento[/b] (teia de vantagens):\n" +
            "Fogo > Planta > Água > Fogo\n" +
            "Elétrico > Vento > Terra > Elétrico\n" +
            "Luz ↔ Trevas (vantagem mútua)\n" +
            "Neutro não tem vantagem nem desvantagem contra ninguém.\n\n" +
            "[b]Multiplicador de dano[/b]: cada sistema (Atributo e Elemento) conta um ponto de vantagem ou desvantagem. " +
            "1 vantagem = x1.3, 2 vantagens (Atributo e Elemento ao mesmo tempo) = x2.0. " +
            "Do outro lado, 1 desvantagem = x0.7, 2 desvantagens = x0.5. " +
            "Se tiver uma vantagem e uma desvantagem ao mesmo tempo, elas se cancelam (x1.0).\n\n" +
            "O multiplicador aparece do lado do número de dano na batalha quando é diferente de x1.";

        private const string AreasDoCenterBody =
            "O Center é dividido em áreas hexagonais, cada uma com uma função. Compre novas áreas na Loja e posicione " +
            "nos hexágonos livres.\n\n" +
            "[b]Neutra[/b]: área padrão, sem bônus especial - os Digimons ficam ali quando não há necessidade de ir a outro lugar.\n\n" +
            "[b]Treino[/b]: Digimons dentro dessa área ganham experiência treinando, num status " +
            "aleatório a cada treino.\n\n" +
            "[b]Treino Especializado[/b]: também existem seis áreas de treino específicas de um " +
            "status só (HP, Ataque, Defesa, Ataque Especial, Defesa Especial ou Velocidade), " +
            "vendidas na Loja por um preço mais alto que a área de treino comum. Um Digimon " +
            "treinando numa delas sempre foca naquele status, com um ganho um pouco melhor que o " +
            "treino genérico. Cada área especializada só aceita 1 Digimon treinando por vez - " +
            "tentar colocar um segundo mostra um aviso na tela.\n\n" +
            "[b]Dormitório[/b]: dormir aqui recupera stamina extra e aumenta a felicidade do Digimon a cada hora de sono. " +
            "Dormir fora do Dormitório por mais de 1 hora começa a custar felicidade, cada vez mais (mas em escala menor a cada hora extra).\n\n" +
            "[b]Refeitório[/b]: coloque carne aqui para os Digimons comerem. Carne dentro do Refeitório demora muito mais " +
            "pra estragar (~1 dia) e ainda dá 10% a mais de recuperação de fome quando fresca. Carne fora do Refeitório estraga rápido " +
            "e, se um Digimon comer carne estragada, perde felicidade, disciplina e corre mais risco de ficar doente.\n\n" +
            "[b]Hospital[/b]: Digimons dentro dessa área recuperam HP muito mais rápido (regeneração passiva bem mais forte " +
            "que em qualquer outro lugar do Center). Não tem relação com remédio - remédio cura doença e pode ser usado em " +
            "qualquer lugar, não precisa estar no Hospital.";

        private const string EditorDeBasesBody =
            "Clique no botão 🏗️ na barra de controle pra abrir o Editor de Bases: mostra todas as " +
            "áreas que você já construiu no Center, além de um inventário com as que foram " +
            "removidas.\n\n" +
            "[b]Remover[/b] uma base tira ela do hexágono, mas não perde ela - a base vai pro " +
            "inventário e fica guardada. Não dá pra remover a área inicial do Center, nem uma área " +
            "que tenha um Digimon nela no momento.\n\n" +
            "[b]Colocar[/b] uma base do inventário funciona como comprar uma nova na Loja: escolha " +
            "um hexágono livre - só que sem custar Bits, já que ela já foi paga antes.\n\n" +
            "Pra mudar uma base de lugar, é só remover e colocar de novo em outro hexágono.";

        private const string BatalhaBody =
            "As batalhas são em tempo real, por turnos individuais: a Velocidade de cada Digimon decide a ordem de ação.\n\n" +
            "[b]Dano[/b] = Ataque do atacante − Defesa do alvo (mínimo 1), com ±10% de variação aleatória. " +
            "10% de chance de crítico, que dobra o dano. O multiplicador de vantagem de tipo (veja o tópico de Vantagens de Tipo) " +
            "é aplicado em cima disso.\n\n" +
            "Times podem ter até 3 Digimons (batalha 3x3) ou 1 (1x1), dependendo do tipo de combate escolhido.\n\n" +
            "[b]Selecionar um Digimon[/b]: durante a luta, clique em qualquer Digimon (seu ou " +
            "inimigo) pra ver o HP dele. Do seu time mostra o HP exato (atual/máximo); do time " +
            "inimigo mostra só uma porcentagem aproximada de vida restante.\n\n" +
            "Ao vencer, o time ganha experiência e Bits. Mesmo perdendo, cada Digimon ganha uma experiência de consolação " +
            "(cerca de 1/5 do normal), pra não travar o progresso do jogador.";

        private const string CampeonatosBody =
            "Ao clicar no botão de troféu, você escolhe entre dois tipos de combate:\n\n" +
            "[b]Batalha Livre[/b]: o jogo gera um time inimigo balanceado de acordo com o nível e poder do seu time atual.\n\n" +
            "[b]Campeonato[/b]: uma lista de combates fixos e pré-determinados (1x1 ou 3x3), com oponentes e nível recomendado " +
            "sempre iguais, independente do quão forte seu time está - e um pouco mais fortes do que o nível sugere, pra " +
            "continuar sendo um desafio mesmo pra um time já bem treinado. Campeonatos não dão experiência (nem na vitória, " +
            "nem a de consolação numa derrota) - só Bits e as recompensas de progressão abaixo. Vencer um campeonato dá uma " +
            "recompensa única de aumento permanente na Capacidade do Center (a forma principal de progressão do jogo por " +
            "enquanto), recebida apenas na primeira vez que aquele campeonato é vencido.";

        private const string EvolucaoBody =
            "Cada Digimon pode evoluir quando atinge o nível mínimo, a idade mínima (em dias) e os stats mínimos exigidos " +
            "pela próxima forma. Quando isso acontece, uma animação centraliza o Digimon na tela e pausa o resto do jogo " +
            "(evoluções simultâneas de vários Digimons entram numa fila, uma de cada vez).\n\n" +
            "Evoluir também custa Capacidade do Center (Digimons de estágio maior ocupam mais espaço). Se não houver " +
            "capacidade suficiente na hora, o jogo avisa e deixa você escolher entre liberar espaço (removendo outros Digimons) " +
            "ou manter esse Digimon sem evoluir por enquanto - ele evolui automaticamente assim que houver espaço.";

        private const string OvosBody =
            "Ovos ocupam Capacidade do Center do mesmo jeito que o Baby que vai nascer deles, então não dá pra acumular " +
            "ovos infinitamente sem espaço. Compre ovos na Loja ou receba o ovo inicial no começo do jogo. Depois de um " +
            "tempo de incubação, o ovo choca sozinho e o Baby aparece no Center.";

        private const string NecessidadesBody =
            "[b]Fome[/b]: cai com o tempo; alimente os Digimons colocando carne no Refeitório (ou em qualquer lugar, " +
            "mas aí ela estraga mais rápido).\n\n" +
            "[b]Stamina[/b]: gasta ao treinar e se recupera descansando/dormindo (mais rápido dentro do Dormitório).\n\n" +
            "[b]Felicidade[/b] e [b]Disciplina[/b]: afetadas por como o Digimon é cuidado (onde dorme, o que come, etc).\n\n" +
            "[b]Doença[/b]: Digimons podem ficar doentes aleatoriamente (chance maior comendo comida estragada); " +
            "use remédio nele (em qualquer lugar do Center, não precisa ser no Hospital) pra curar. Já o Hospital em si " +
            "serve pra recuperar HP mais rápido, não pra curar doença.\n\n" +
            "Enquanto todos os Digimons do Center estão dormindo, o botão de pular sono (na área do relógio) avança o " +
            "tempo direto até alguém acordar.";

        private const string InventarioBody =
            "O botão de mochila abre o Inventário: mostra todo item conhecido pelo jogo (comida, remédio, e outros " +
            "itens futuros) com ícone e quantidade atual. Passe o mouse sobre um item pra ver o nome completo e a descrição.";

        private const string RelogioBody =
            "O relógio na barra superior mostra a hora atual (incluindo um relógio analógico) e o dia/calendário do jogo, " +
            "num ciclo de 24 horas. Enquanto todos os Digimons estão dormindo (não há nada pra fazer nesse período ainda, " +
            "já que não existem Digimons diurnos/noturnos no jogo por enquanto), aparece um botão pra pular direto até " +
            "alguém acordar, sem quebrar a passagem de dias nem nenhum sistema que dependa do relógio.";

        private const string RenomearBody =
            "Clique no nome do Digimon selecionado, ao lado do sprite na barra superior, pra dar um apelido a ele. " +
            "Isso só muda o nome mostrado nas telas - não afeta a espécie, evolução ou qualquer outra lógica interna do jogo. " +
            "Deixar o campo vazio (ou digitar o nome original da espécie) remove o apelido.";
    }
}
