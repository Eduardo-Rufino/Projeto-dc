# NPCs da Floresta Inicial — relatório e formulário de revisão

Gerado em 2026-10-27 a partir de `Data/Maps/floresta_inicial.json`, `Data/NPCs/`, `Data/Quests/`, `Data/Itens/`, `Data/Digimon/` (drops), do mapa `Assets/Areas/Forest_new/modelo_floresta/mapa/mapa_floresta.json` e do código (`GameManager`, `ExplorationArea`, `WildEncounterSpawn`, `BattleRewardCalculator`, `DigimonWorld`, `TrainingSystem`).

## 1. Resumo

A Floresta Inicial (mapa 1) tem 12 NPCs: 9 dão quest, 4 podem ser recrutados pro Center e 3 não dão quest nenhuma.

- **Linha principal (0.12, mapa isométrico):** Reppamon → Terriermon, e Elizamon em paralelo. Cada quest abre um bloqueio do mapa: toras ao sul da praça, rio (jangada pro Pântano) e deslizamento (mina pra Caverna).
- **Quests antigas (de antes do mapa isométrico):** Gabumon, Gaogamon, Togemon, Impmon, Palmon e Wizardmon. Cinco delas são "derrote N selvagens" e só uma (Impmon) é de coleta. Nenhuma delas libera área nem bloqueio.
- **Sem quest:** Patamon (guia da praça), Gomamon (barqueiro pro Pântano) e Tortamon (só conversa).
- **Total que a floresta paga em quests:** 3.150 Bits, +5 de capacidade (o Center começa com 10), 1 Tábuas (consumida na quest seguinte) e 4 recrutamentos.

O jogo não tem um campo de personalidade nos dados. A personalidade descrita nas fichas foi deduzida das falas de cada NPC.

| NPC | Função | Quest | Objetivo | Recompensa | Libera | Recrutável |
|---|---|---|---|---|---|---|
| Patamon | Guia da praça | — | — | — | — | Não |
| Reppamon | Lenhador | 27 Toras no Caminho | Conversar | 200 Bits + 1 Tábuas | Clareira dos lenhadores | Não |
| Terriermon | Construtor da jangada | 28 A Jangada | Entregar 1 Tábuas | 300 Bits + 1 capacidade | Rio, barqueiro, Pântano Sombrio | Não |
| Elizamon | Escavador | 29 O Túnel Soterrado | Entregar 1 Chip de Escavação | 300 Bits + 1 capacidade | Mina, Caverna Cristalina | Não |
| Gomamon | Barqueiro | — | — | — | Viagem pro Pântano | Não |
| Gabumon | Boas-vindas | 1 Limpeza da Floresta | Derrotar 3 selvagens | 500 Bits + 2 capacidade | — | Não |
| Gaogamon | Mestre de Ataque | 2 Prove seu Valor (Ataque) | Derrotar 2 selvagens | 400 Bits | Recrutamento | Sim |
| Togemon | Mestre de Defesa | 3 Prove seu Valor (Defesa) | Derrotar 2 selvagens | 400 Bits | Recrutamento | Sim |
| Impmon | Negociante | 4 Folhas pro Impmon | Coletar 2 Folhas da Floresta | 300 Bits | — | Não |
| Palmon | Coletora escondida | 5 A Trilha da Palmon | Derrotar 2 selvagens | 350 Bits + 1 capacidade | Recrutamento | Sim |
| Wizardmon | Vidente da evolução | 6 O Selo do Destino | Derrotar 2 selvagens | 400 Bits | Recrutamento | Sim |
| Tortamon | Ermitão das pedras | — | — | — | — | Não |

## 2. A área

| Dado | Valor |
|---|---|
| Nível recomendado | 6 |
| Selvagens | Agumon, Gabumon, Renamon (todos Rookie), sorteados a cada ponto |
| Nível dos selvagens | 5 a 8 (sorteado) |
| Pontos de selvagem | 10 (`inimigo_0` a `inimigo_9`) |
| Início do explorador | bloco [58,35], ao lado da praça |
| Tamanho do mapa | 96 x 60 blocos |
| Requisito pra entrar | nenhum (é a primeira área) |
| Áreas que saem daqui | Caverna Cristalina (nível rec. 14, quest 29) e Pântano Sombrio (nível rec. 20, quest 28) |

O que dá pra alcançar a pé com cada bloqueio fechado (conferido na grade de passos do mapa):

| Situação | Fica de fora |
|---|---|
| Nada liberado | Gomamon (barqueiro), baú da clareira dos lenhadores, baú da margem do pântano |
| Depois da quest 27 | Gomamon e baú da margem do pântano |
| Depois das quests 27 e 28 | Nada |

O Chip de Escavação e o Elizamon estão acessíveis desde o começo. A quest 29 não depende da 27 nem da 28.

## 3. Regras das quests (como o código funciona hoje)

**Aceitar**
- Não tem limite de quests ativas ao mesmo tempo, nem pré-requisito entre quests: todas as 9 podem ser aceitas logo na primeira visita.
- Toda quest é única (não repete depois de entregue). Uma quest = um objetivo + uma recompensa, sem etapas.

**Progresso por tipo de objetivo**
- **Derrotar selvagens (`DefeatWild`):** cada vitória contra selvagem avança TODAS as quests desse tipo que estiverem ativas ao mesmo tempo. Alvo 0 = qualquer espécie conta.
  - A vitória conta em qualquer área de exploração, não só na floresta. O texto "nessa área" das quests 1, 2, 3, 5 e 6 não é verdade no código.
  - Derrota não conta.
  - Com as 5 quests aceitas juntas, 3 vitórias completam todas (11 vitórias "no papel").
- **Coletar item (`CollectItem`):** só conta o item que entra no inventário DEPOIS de aceitar (drop ou baú). Folhas que você já tinha não contam. A entrega NÃO consome os itens.
- **Conversar (`Talk`):** fica pronta no momento em que é aceita.
- **Entregar item (`DeliverItem`):** conta o que está no inventário agora, não importa quando foi pego (pegar o chip antes de falar com o Elizamon vale). A entrega consome os itens.

**Entregar**
- É sempre pro mesmo NPC que deu a quest.
- Aplica Bits, capacidade e item de recompensa. Se o NPC for recrutável, ele é recrutado na entrega (não existe recrutar sem fazer a quest).
- Abre na hora o bloqueio ligado à quest (`BlockerQuests`) e as passagens que pedem a quest (`Transitions`).

## 4. Batalhas com selvagens

- **Formato:** 1 contra 1, o Digimon explorador contra um selvagem.
- **Como começa:** o selvagem vagueia até 220 px do ponto dele. Ele te persegue quando você chega a 320 px (30% mais rápido que o passo normal) e a luta começa ao encostar (28 px) ou ao clicar nele.
- **Depois da luta:** o selvagem some, ganhando ou perdendo, e só volta quando você sai e entra de novo na área. São no máximo 10 lutas por visita.
- **Recompensa por vitória (selvagem Rookie):** 80 de XP e 200 Bits, vezes o multiplicador de nível (de 0,5 com o selvagem 5 níveis abaixo até 1,5 com ele 5 acima). Ganha +8 de felicidade.
  - Uma única vitória já paga mais ou menos o mesmo que uma quest (200 a 500 Bits).
  - Pra comparar: 1 Carne custa 20 Bits na loja.
- **Derrota:** -6 de felicidade e XP de consolação.

## 5. Itens envolvidos nas quests

| Item | Id | Como conseguir | Usado em |
|---|---|---|---|
| Tábuas | 13 | Recompensa da quest 27 (1 unidade); não tem outra fonte | Entregue na quest 28 (consome 1) |
| Chip de Escavação | 14 | Orbe no chão do spot `item_chip`, bloco [43,55], beco ao sul; só existe 1 no jogo | Entregue na quest 29 (consome 1) |
| Folha da Floresta | 3 | Drop dos 3 selvagens da floresta (40% cada vitória, 1 unidade) e baú `bau_2` (2 unidades) | Coletada na quest 4 (não consome) |

### Chance de drop da Folha da Floresta

Agumon, Gabumon e Renamon têm o mesmo drop: Folha da Floresta x1 com 40% de chance por vitória (`Drops` no JSON de cada espécie). São os únicos selvagens da floresta, então qualquer vitória ali tem 40% de chance.

| Vitórias depois de aceitar a quest 4 | Chance de já ter as 2 Folhas |
|---|---|
| 2 | 16% |
| 3 | 35% |
| 4 | 52% |
| 5 | 66% |
| 7 | 84% |
| 10 (uma visita inteira) | 95% |

Em média são 5 vitórias. Quem aceita a quest 4 e depois abre o baú `bau_2` (bloco [12,47]) completa a quest na hora com as 2 Folhas dele, sem lutar.

## 6. Baús e itens no chão da floresta

| Spot | Bloco | Conteúdo | Acesso |
|---|---|---|---|
| bau_0 | [15,19] | Carne x3 | Livre |
| bau_1 | [49,25] | Remédio x1 | Livre |
| bau_2 | [12,47] | Folha da Floresta x2 | Livre |
| bau_3 | [36,4] | Carne x2 | Livre (fecha um beco sem saída atrás dele) |
| bau_4 | [66,13] | Energético x1 | Livre |
| bau_5 | [44,41] | Remédio x1 | Livre |
| bau_6 | [24,38] | Carne x3 | Livre |
| bau_lenhadores | [62,54] | Energético x2 | Depois da quest 27 (clareira dos lenhadores) |
| bau_pantano | [91,11] | Remédio x2 | Depois da quest 28 (margem do pântano) |
| item_chip | [43,55] | Chip de Escavação x1 | Livre (item da quest 29) |

Cada baú só pode ser aberto uma vez no jogo inteiro. Depois disso ele fica aberto.

## 7. Bônus dos NPCs recrutáveis

| NPC | Bônus no Center | Números |
|---|---|---|
| Gaogamon | Treino de Ataque mais efetivo; Digimon vão treinar Ataque sozinhos | x1,15 no treino da área de Ataque; 8% de chance, quando o Digimon fica à toa, de ir sozinho pra essa área |
| Togemon | Treino de Defesa mais efetivo; Digimon vão treinar Defesa sozinhos | x1,15 no treino da área de Defesa; mesma chance de 8% |
| Palmon | Carne grátis | 1 ou 2 Carnes por dia (sorteado) |
| Wizardmon | Bloquear evoluções | Permite bloquear, Digimon por Digimon, evoluções específicas (Guia de Evolução) |

## 8. Fichas dos NPCs

### Linha principal

**Patamon** (NPC 28, spot `npc_guia`, bloco [55,30], na praça)

- Função: guia. Explica as três frentes da floresta e dá a dica de onde está o chip do Elizamon.
- Quest: nenhuma.
- Personalidade: anfitrião simpático, informativo e meio fofoqueiro ("Dizem que ele perdeu um chip num beco ao sul...").
- Falas:
  - "Bem-vindo à praça da Floresta Inicial!"
  - "Ao sul, o Reppamon está cortando as toras que fecham a clareira dos lenhadores."
  - "Ao nordeste fica o cais do rio - o Terriermon quer montar uma jangada pra atravessar até o pântano."
  - "E lá no noroeste, depois do planalto, o Elizamon tenta abrir um túnel pra caverna. Dizem que ele perdeu um chip num beco ao sul..."

**Reppamon** (NPC 29, spot `npc_lenhador`, bloco [57,44], sul da praça)

- Função: lenhador. Corta a pilha de toras (`bloqueio_arvores`, blocos [58,47] e [59,47]) e libera a clareira dos lenhadores (com o baú de Energético x2). As toras viram tocos.
- Quest 27 — Toras no Caminho
  - Descrição no jogo: "Converse com o Reppamon: ele corta a pilha de toras que fecha o caminho ao sul da praça e te dá as tábuas."
  - Objetivo: conversar (pronta assim que é aceita).
  - Recompensa: 200 Bits + 1 Tábuas.
  - Pré-requisito: nenhum.
  - Libera: a clareira e as Tábuas da quest 28.
- Personalidade: prestativo e confiante ("Deixa comigo"), resolve sozinho e já pensa em quem precisa das tábuas.
- Falas:
  - "Essas toras caídas fecharam a trilha da clareira."
  - "Deixa comigo: umas rajadas de vento e viram tábuas. Pode ficar com elas - alguém lá no cais estava precisando."

**Terriermon** (NPC 30, spot `npc_construtor`, bloco [69,11], cais do rio a nordeste)

- Função: construtor da jangada. Abre o rio (`bloqueio_rio`, 12 blocos de [74,8] a [79,9]); a jangada aparece no lugar.
- Quest 28 — A Jangada
  - Descrição no jogo: "Entregue as Tábuas ao Terriermon pra ele montar a jangada e você atravessar o rio até a margem do pântano."
  - Objetivo: entregar 1 Tábuas (consome o item).
  - Recompensa: 300 Bits + 1 de capacidade.
  - Pré-requisito: na prática, a quest 27 (é a única fonte de Tábuas).
  - Libera: a margem do pântano (baú de Remédio x2), o barqueiro Gomamon e a entrada do Pântano Sombrio (nível rec. 20).
- Personalidade: tranquilão e otimista, com o bordão "Momentai!".
- Falas:
  - "Do outro lado do rio fica a margem do pântano, mas não tem ponte nenhuma."
  - "Se eu tivesse umas tábuas, montava uma jangada rapidinho. Momentai!"

**Elizamon** (NPC 31, spot `npc_escavador`, bloco [10,12], noroeste, depois do planalto)

- Função: escavador. Remove o deslizamento (`bloqueio_deslizamento`, blocos [7,7] e [8,7]); no lugar aparece a mina (clicável, com plaquinha "Mina").
- Quest 29 — O Túnel Soterrado
  - Descrição no jogo: "Encontre o Chip de Escavação (perdido num beco ao sul) e entregue ao Elizamon pra ele abrir o túnel até a caverna."
  - Objetivo: entregar 1 Chip de Escavação (consome o item).
  - Recompensa: 300 Bits + 1 de capacidade.
  - Pré-requisito: nenhum (o chip está acessível desde o início, em [43,55]).
  - Libera: a mina e a Caverna Cristalina (nível rec. 14). Na volta da caverna, o explorador aparece no bloco [7,5].
- Personalidade: confiante no próprio serviço ("cavo isso num instante"), mas distraído (perdeu o chip).
- Falas:
  - "Um deslizamento fechou a entrada da mina que leva à caverna."
  - "Eu cavo isso num instante... mas perdi o meu Chip de Escavação. Acho que caiu num beco lá no sul da floresta."

**Gomamon** (NPC 32, spot `npc_pantano`, bloco [89,6], margem do outro lado do rio)

- Função: barqueiro. Clicar nele pergunta se você quer ir pro Pântano Sombrio; na volta, o explorador aparece no bloco [88,7].
- Quest: nenhuma. Só fica alcançável depois da quest 28.
- Personalidade: prático, de poucas palavras.
- Falas:
  - "Daqui pra frente é só lama e trilha de tábua. Quer que eu te leve até o Pântano Sombrio?"

### Quests antigas

**Gabumon** (NPC 1, bloco [60,33], perto do início)

- Função: boas-vindas e primeira caçada.
- Quest 1 — Limpeza da Floresta
  - Descrição no jogo: "Derrote 3 Digimon selvagens nessa área."
  - Objetivo: derrotar 3 selvagens quaisquer.
  - Recompensa: 500 Bits + 2 de capacidade (a maior da floresta).
- Personalidade: veterano cordial que dá conselho ("cuidado, eles aparecem em bando").
- Observações: Gabumon também é um dos selvagens da área. A fala diz "eles aparecem em bando", mas as lutas são sempre 1 contra 1.
- Falas:
  - "Bem-vindo à Floresta Inicial, treinador."
  - "Esses selvagens por aqui não são páreo pra quem já treinou de verdade - mas cuidado, eles aparecem em bando."

**Gaogamon** (NPC 2, bloco [12,17], noroeste)

- Função: mestre de Ataque.
- Quest 2 — Prove seu Valor (Ataque)
  - Descrição no jogo: "Derrote 2 Digimon selvagens nessa área pra mostrar que sabe batalhar."
  - Objetivo: derrotar 2 selvagens quaisquer.
  - Recompensa: 400 Bits + recrutamento.
- Personalidade: lobo solitário e orgulhoso, testa o jogador antes de ajudar.
- Falas:
  - "Sinto o cheiro de um treinador de verdade. Poucos chegam até esse canto da floresta."
  - "Passei anos treinando Ataque sozinho por aqui. Se você provar que manja de batalha, posso pensar em ajudar no seu Center."

**Togemon** (NPC 3, bloco [42,42], sudoeste da praça)

- Função: mestre de Defesa.
- Quest 3 — Prove seu Valor (Defesa)
  - Descrição no jogo: "Derrote 2 Digimon selvagens nessa área pra mostrar que sabe batalhar." (igual à da quest 2)
  - Objetivo: derrotar 2 selvagens quaisquer.
  - Recompensa: 400 Bits + recrutamento.
- Personalidade: durão e territorial.
- Observação: a fala usa "do que você é feita", no feminino, para o jogador.
- Falas:
  - "Ninguém atravessa essa parte da floresta sem esbarrar nos meus espinhos."
  - "Defesa é o que me mantém de pé até hoje. Enfrente alguns selvagens pra eu ver do que você é feita, e talvez eu vá pro seu Center ensinar isso."

**Impmon** (NPC 4, bloco [48,22], escondido dentro de um arbusto na beira do lago, ao norte da praça)

- Função: negociante.
- Quest 4 — Folhas pro Impmon
  - Descrição no jogo: "Colete 2 Folhas da Floresta (dropadas pelos Digimon selvagens da floresta derrotados) e entregue ao Impmon."
  - Objetivo: coletar 2 Folhas da Floresta depois de aceitar (drop 40% ou baú `bau_2`).
  - Recompensa: 300 Bits.
  - Observação: a descrição diz "entregue", mas a quest não consome as Folhas, que ficam no inventário sem outro uso.
- Personalidade: malandro e misterioso, quer negociar e não explica nada ("Não pergunta por quê").
- Falas:
  - "Ei, você aí. Não, não quero briga, quero negociar."
  - "Ando colecionando folhas da floresta. Não pergunta por quê. Traga umas pra mim e eu pago bem."

**Palmon** (NPC 5, bloco [70,33], leste)

- Função: coletora escondida.
- Quest 5 — A Trilha da Palmon
  - Descrição no jogo: "Derrote 2 Digimon selvagens nessa área pra afastá-los da trilha escondida da Palmon."
  - Objetivo: derrotar 2 selvagens quaisquer.
  - Recompensa: 350 Bits + 1 de capacidade + recrutamento.
- Personalidade: tímida e desconfiada, se esconde de propósito.
- Falas:
  - "Ah! Não... não esperava visita aqui atrás."
  - "Eu me escondo desse jeito por um motivo - selvagem nenhum incomoda quem ninguém encontra."
  - "Se você conseguiu chegar até aqui sem se perder, já mostrou que sabe se virar na floresta. Ajude a afastar alguns selvagens da minha trilha e eu vejo se confio em vir pro seu Center."

**Wizardmon** (NPC 7, bloco [16,44], sudoeste)

- Função: vidente da evolução.
- Quest 6 — O Selo do Destino
  - Descrição no jogo: "Derrote 2 Digimon selvagens nessa área pra mostrar ao Wizardmon que você sabe proteger seus Digimon."
  - Objetivo: derrotar 2 selvagens quaisquer.
  - Recompensa: 400 Bits + recrutamento.
- Personalidade: místico e enigmático, fala de destino e de caminhos.
- Falas:
  - "Sinto o destino de vários Digimon passando por perto de você. Alguns caminhos que eles seguirão... nem você escolheu."
  - "Eu vejo as linhas de evolução antes delas acontecerem. Prove que sabe proteger quem já é seu, e eu te ensino a fechar os caminhos que você não quer."
  - "Afaste alguns selvagens daqui e considere isso feito."

**Tortamon** (NPC 6, bloco [64,12], nas pedras a nordeste)

- Função: só conversa.
- Quest: nenhuma.
- Personalidade: ermitão calado e melancólico, quer companhia mas também sossego.
- Falas:
  - "..."
  - "Fica quieto aí um instante. Já faz tempo que ninguém sobe até essas pedras."
  - "Não tenho nada pra pedir de você, treinador. Só queria companhia de vez em quando - e talvez um pouco de sossego no resto do tempo."

## 9. Pontos de atenção encontrados

1. **Quests repetidas:** as quests 1, 2, 3, 5 e 6 pedem a mesma coisa (2 ou 3 selvagens quaisquer). As quests 2 e 3 têm a mesma descrição palavra por palavra.
2. **Feitas todas juntas:** como uma vitória conta pra todas as quests de selvagem ativas, 3 vitórias completam as 5 e recrutam 4 NPCs. Isso dá 2.050 Bits e +3 de capacidade.
3. **"Nessa área" não é verdade:** vitórias em qualquer área de exploração contam pras quests da floresta.
4. **A linha principal não tem batalha:** as quests 27, 28 e 29 se completam sem lutar nenhuma vez.
5. **Salto de dificuldade:** o Pântano (nível rec. 20) abre com duas quests sem luta numa área de nível 6. A Caverna (nível rec. 14) também abre sem luta.
6. **Folhas sem uso:** a quest 4 não consome as Folhas e elas não servem pra mais nada hoje.
7. **Atalho do baú:** o `bau_2` dá exatamente as 2 Folhas da quest 4.
8. **Recrutamento atrelado:** só dá pra recrutar entregando a quest. Mudar a quest de um recrutável muda quando o bônus chega no Center.
9. **Recompensa x batalha:** uma vitória contra um selvagem (cerca de 200 Bits) paga quase o mesmo que uma quest.
10. **Falas desatualizadas:** as falas antigas não citam a praça, o rio nem a mina do mapa novo. O Gabumon fala de "bando", mas as lutas são 1 contra 1. O Togemon trata o jogador no feminino.
11. **Sem quest:** Patamon, Gomamon e Tortamon.

## 10. Formulário de análise

Preencha uma linha por NPC. Os tipos de objetivo que o jogo já suporta são: derrotar selvagens (qualquer um ou de uma espécie), coletar item, conversar e entregar item. Um tipo novo, ou uma quest com várias etapas ou pré-requisito, exige programação.

- **Decisão:** Manter / Ajustar / Reescrever / Remover quest / Criar quest
- **Prioridade:** Alta / Média / Baixa

| NPC | Quest atual | Decisão | Prioridade | Novo objetivo (tipo + alvo + qtd.) | História / motivo da quest | Recompensa nova | Pré-requisito / libera | Personalidade (manter / mudar) | Notas |
|---|---|---|---|---|---|---|---|---|---|
| Patamon | — | | | | | | | | |
| Reppamon | 27 Toras no Caminho (conversar) | | | | | | Libera clareira + Tábuas | | |
| Terriermon | 28 A Jangada (entregar 1 Tábuas) | | | | | | Depende da 27; libera Pântano | | |
| Elizamon | 29 O Túnel Soterrado (entregar 1 Chip) | | | | | | Libera Caverna | | |
| Gomamon | — (barqueiro) | | | | | | Depende da 28 | | |
| Gabumon | 1 Limpeza da Floresta (3 selvagens) | | | | | | | | |
| Gaogamon | 2 Prove seu Valor – Ataque (2 selvagens) | | | | | | Recrutamento (Ataque x1,15) | | |
| Togemon | 3 Prove seu Valor – Defesa (2 selvagens) | | | | | | Recrutamento (Defesa x1,15) | | |
| Impmon | 4 Folhas pro Impmon (2 Folhas, 40% drop) | | | | | | | | |
| Palmon | 5 A Trilha da Palmon (2 selvagens) | | | | | | Recrutamento (1-2 Carnes/dia) | | |
| Wizardmon | 6 O Selo do Destino (2 selvagens) | | | | | | Recrutamento (bloquear evoluções) | | |
| Tortamon | — | | | | | | | | |

### Perguntas gerais

- [ ] Qual deve ser a ordem ideal das quests na floresta (o que o jogador faz primeiro)?
- [ ] As quests devem ter pré-requisito entre si (ex.: só falar com o Terriermon depois do Reppamon)?
- [ ] A linha principal deve ter alguma batalha obrigatória antes de abrir o Pântano/Caverna?
- [ ] O progresso de "derrote selvagens" deve contar só na área da quest?
- [ ] Uma vitória deve continuar contando pra várias quests ao mesmo tempo?
- [ ] Os 4 recrutáveis devem pedir algo ligado ao que ensinam (Ataque, Defesa, comida, evolução)?
- [ ] A quest do Impmon deve consumir as Folhas? As Folhas devem ter outro uso?
- [ ] O `bau_2` deve continuar dando Folhas?
- [ ] Algum NPC antigo deve sair da floresta ou mudar de mapa?
- [ ] Personalidades: manter as atuais ou redefinir alguma?
- [ ] Precisa de algum tipo de objetivo novo (ex.: vencer com um Digimon de certo atributo, entregar comida, escoltar)?
- [ ] As recompensas (200 a 500 Bits, 0 a 2 de capacidade) estão equilibradas contra o que uma batalha paga (~200 Bits)?
- [ ] Quanto da capacidade total (+5 na floresta) deve vir de quests?
