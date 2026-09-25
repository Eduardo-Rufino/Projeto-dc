# Requisitos de evolução - Projeto DC (Digimon Training Center)

Arquivo gerado automaticamente a partir dos dados do jogo (`Data/Digimon`, `Data/Evolutions`, `Data/Evolution/evolution_web.json`, `Data/Jogress`). Serve de entrada pra revisar e melhorar os requisitos de evolução. **Os números em cada tabela são os valores reais em uso hoje.**

- Digimons no jogo: **199** | evoluções normais (com requisitos): **337** | receitas Jogress: **3**
- Estágios (ordem): Baby -> InTraining -> Rookie -> Champion -> Ultimate -> Mega -> MegaPlus (existe também Special, sem linha própria de evolução)

## 1. Objetivo da revisão (o que peço ao revisor)

Verifique e proponha melhorias nos requisitos de evolução, respondendo com **listas de alterações concretas** (Digimon de origem -> destino: campo: valor atual -> valor novo, e o motivo). Pontos a avaliar:

1. **Coerência de dificuldade por estágio**: níveis, idades e stats exigidos crescem de forma sensata de Rookie -> Champion -> Ultimate -> Mega? Há saltos estranhos ou requisitos fora do padrão da própria faixa?
2. **Viabilidade dentro do tempo de vida** (seção 3): todo Digimon morre de velhice se passar do teto de idade do estágio atual. Cada evolução deve ser alcançável no tempo disponível por um jogador que treina/luta com esforço moderado, e as formas 'premium' devem exigir esforço alto ou um Centro mais evoluído.
3. **Afinidade com a Role/atributos**: os stats exigidos combinam com a Role e o `AttackType` de quem evolui e de quem nasce? (Ex.: uma evolução de Tank deveria pedir PD/ED/HP; um Ranged/Mage, EA.) O jogador escolhe o caminho treinando certos stats: os caminhos irmãos precisam ser **distinguíveis** e alcançáveis por treino diferente.
4. **Empates e ambiguidade**: quando várias evoluções ficam válidas ao mesmo tempo, vence a de maior 'score' (seção 2.3); empate = escolha aleatória. Empates entre irmãos devem ser evitados, exceto de propósito.
5. **Stats especiais**: o nível NÃO aumenta EA/ED, só o treino e o multiplicador de evolução (seção 2.4). Requisitos altos de EA/ED em Digimons que não treinam isso são difíceis de justificar.
6. **Caminhos 'sombrios'/'queridos'**: uso de Disciplina/Felicidade mínima e máxima, batalhas e taxa de vitória. Há evoluções onde esses requisitos deveriam existir e não existem (ou o contrário)?
7. **Progressão de multiplicador** (`statMultiplier`): a recompensa de cada evolução é proporcional à dificuldade?
8. Qualquer **beco sem saída** ou Digimon cujas evoluções sejam sempre tudo-ou-nada (uma única opção sem alternativa fraca).

Restrições: não crie/renomeie Digimons; só ajuste requisitos e (se realmente necessário) sugira novas ligações de evolução, marcando-as como sugestão. Preserve o espírito canônico onde a fonte é `canon` (seção 2.5).

## 2. Como as evoluções funcionam hoje

### 2.1 Requisitos (todos precisam ser atendidos ao mesmo tempo)

| Campo | Significado |
|---|---|
| `Lv` (requiredLevel) | Nível mínimo do Digimon |
| `Idade` (requiredAgeInDays) | Idade mínima em dias de jogo. **A idade é acumulada desde o nascimento e NÃO reseta ao evoluir.** |
| `HP`, `PA`, `PD`, `EA`, `ED`, `Vel` (requiredStats) | Valor **atual** mínimo de cada stat: HP, Ataque Físico, Defesa Física, Ataque Especial, Defesa Especial, Velocidade. `-` = 0 = sem exigência |
| `Batalhas` | Nº mínimo de batalhas lutadas (vitória ou derrota) |
| `Vitórias%` | Taxa mínima de vitórias (só faz sentido com Batalhas>0) |
| `Disc≥/≤` | Disciplina mínima/máxima (0-100) |
| `Felic≥/≤` | Felicidade mínima/máxima (0-100) |
| `xMult` (statMultiplier) | Multiplicador aplicado aos stats atuais ao evoluir (seção 2.4) |
| `Score` | Soma usada pra desempate entre evoluções válidas (2.3) |

A evolução é verificada automaticamente no fim de cada dia, depois de cada treino e depois de cada batalha (o jogador não escolhe o momento; a evolução com maior score entre as válidas é aplicada). Ela só é aplicada se houver **capacidade** no Centro (a forma nova ocupa mais capacidade); senão fica bloqueada até haver espaço. O jogador também pode, com um NPC recrutado (Wizardmon), **bloquear** evoluções específicas pra escolher o caminho.

### 2.2 Custo de capacidade e tetos de idade por estágio

| Estágio | Capacidade que ocupa | Teto de idade (morre se `Idade` > teto sem evoluir) |
|---|---|---|
| Baby | 1 | 4 dias |
| InTraining | 2 | 7 dias |
| Rookie | 3 | 15 dias |
| Champion | 5 | 26 dias |
| Ultimate | 7 | 52 dias |
| Mega | 10 | 68 dias |
| MegaPlus | 12 | 68 dias |
| Special | 15 | 68 dias |

O teto vale para o estágio **atual** e a idade é acumulada. Ex.: um Digimon que ainda é Rookie no dia 16 morre; um que evoluiu pra Champion no dia 14 ganha o teto de Champion (26).

### 2.3 Como a evolução é escolhida

Entre as evoluções cujos requisitos estão TODOS atendidos, vence a de **maior Score**; empate no Score = sorteio aleatório.

`Score = Lv + Idade + HP + PA + PD + EA + ED + Vel + Batalhas + Vitórias%(x100) + Disc≥ + (100 - Disc≤) + Felic≥ + (100 - Felic≤)`

### 2.4 Crescimento dos stats (base pra calibrar requisitos)

- **Stats iniciais** = `BaseStats` da espécie (aparecem no cabeçalho de cada Digimon, seção 7).
- **A cada nível**: +2 PA, +2 PD, +1 Vel, +5 HP. **EA e ED não crescem por nível.**
- **Treino** (custa 10 de stamina, dá 5 XP): o jogador escolhe 1 stat. Resultado sorteado 1/2/3 com chance 25%/50%/25% (média 2). Ganho = resultado no stat escolhido (PA, PD, EA, ED ou Vel) e **6x o resultado no HP** (média 12). Áreas de treino específicas dão x1,15 e NPC recrutado da especialidade x1,15 (empilham). Cada treino também dá +10 Felicidade e +5 Disciplina.
- **Stamina**: regenera 72 por dia (cerca de 7 treinos/dia sem Energético; o Energético recupera stamina e custa Bits).
- **Ao evoluir**: cada stat atual vira `max(stat x xMult, lerp(stat x xMult, stat_da_ficha_da_nova_forma, 0.4))` - multiplica o treino acumulado e puxa um pouco em direção à ficha da nova espécie, sem nunca reduzir.
- **XP**: nível N -> N+1 custa `100 x 1,2^(N-1)`. XP total acumulado pra chegar ao nível N (partindo do 1):

| Nível | 5 | 10 | 12 | 15 | 18 | 20 | 24 | 28 | 30 | 36 | 37 | 40 | 45 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| XP total | 536 | 2.079 | 3.215 | 5.919 | 10.593 | 15.473 | 32.623 | 68.185 | 98.406 | 294.834 | 353.900 | 611.904 | 1.523.359 |

- **Fontes de XP**: treino (5), batalhas (XP base por estágio do inimigo: 80/180/400/900 para Rookie/Champion/Ultimate/Mega, x um multiplicador 0,5-1,5 conforme a diferença de nível), exploração, e conjuntos completos (+8% XP). Derrota dá só um XP de consolação.
- **Felicidade/Disciplina**: começam em 50. Felicidade sobe ao comer (+5), treinar (+10), vencer (+8), dormir no Dormitório; cai com fome, doença (-8), derrota (-6), comida estragada, sujeira e ser acordado. Disciplina sobe treinando (+5), curando com remédio (+5) e dormindo no Dormitório (+3); cai com comida estragada, sujeira e treino falho.

### 2.5 Fonte das ligações (evolution_web)

- `canon`: baseado em v-pet/anime/jogos; `design`: escolha de game design sem linha oficial. Nas tabelas, a coluna `Fonte` traz isso por ligação.
- Ligações no grafo: 336 normais (127 canon + 209 design), 6 DNA/Jogress e 1 troca de modo.

## 3. Viabilidade medida (simulação de jogador ideal)

Uma simulação com o código real (treino + batalhas por dia, stamina, XP, tetos de idade acima) mediu se cada faixa de nível exigido é alcançável antes de o Digimon morrer, com um jogador que treina só o que a evolução pede. "Folga" = dias que sobram antes do teto de idade. Resultado com os requisitos atuais:

| Faixa (nível exigido pela evolução) | Esforço moderado (~4 batalhas/dia, sem Energético) | Esforço alto e/ou Centro evoluído |
|---|---|---|
| Rookie -> Champion, nível 12 (base) | todas viáveis (folga ~2 dias) | - |
| Rookie -> Champion, nível 18 (premium) | inviável | só com esforço alto + Energético e/ou Centro evoluído; com Centro evoluído a folga é ~8 dias |
| Champion -> Ultimate, nível 20 (base) | todas viáveis (folga ~1 dia) | - |
| Champion -> Ultimate, nível 28 (premium) | inviável | só com esforço alto + Energético e/ou Centro evoluído; com Centro evoluído a folga é ~3 dias |
| Ultimate -> Mega, nível 28 (base) | precisa de >= 5 batalhas/dia (folga ~6 dias) | viável |
| Ultimate -> Mega, níveis 36/37 (premium) | inviável | só com esforço alto + Energético e/ou Centro evoluído; com Centro evoluído a folga é ~1 a 3 dias |

"Centro evoluído" na simulação = 12 batalhas/dia, XP x1,6 e ganho de treino x1,32 (áreas de treino específicas, NPCs recrutados e conjuntos completos empilhados). Só as folgas do Centro evoluído e as linhas de esforço moderado foram medidas diretamente; "esforço alto" sem Centro evoluído não foi tabelado por faixa.

A intenção de design: com esforço moderado o Digimon chega a uma **forma mais fraca/menos desejada** de cada estágio; as formas mais fortes (níveis exigidos maiores) exigem **mais esforço ou um Centro mais evoluído**. Só morre sem evoluir quem cuida/treina mal. Ao mudar níveis/idades/stats exigidos, preserve esse gradiente.

## 4. Visão geral por transição de estágio

| Transição | Nº de evoluções | Nível (mín / mediana / máx) | Idade (mín / mediana / máx) | Soma dos stats exigidos (mín / mediana / máx) | Com Batalhas/Vitórias/Disc/Felic | xMult (mín-máx) |
|---|---|---|---|---|---|---|
| Baby -> InTraining | 49 | 2 / 2 / 2 | 0 / 0 / 2 | 37 / 53 / 70 | 6 | 1.15-1.2 |
| InTraining -> Rookie | 59 | 2 / 2 / 3 | 0 / 0 / 5 | 20 / 97 / 152 | 6 | 1.25-1.3 |
| Rookie -> Champion | 72 | 12 / 12 / 18 | 2 / 2 / 13 | 145 / 242 / 417 | 10 | 1.8-1.9 |
| Champion -> Ultimate | 75 | 20 / 20 / 28 | 5 / 8 / 24 | 316 / 472 / 813 | 8 | 1.6-1.7 |
| Ultimate -> Mega | 81 | 28 / 28 / 36 | 8 / 12 / 50 | 557 / 781 / 1394 | 20 | 1.7-1.8 |
| Mega -> Mega | 1 | 45 / 45 / 45 | 14 / 14 / 14 | 1208 / 1208 / 1208 | 1 | 1.1-1.1 |

## 5. Pontos de atenção detectados automaticamente

Fatos objetivos extraídos dos dados (não são julgamentos de design).

### 5.1 Evoluções cuja idade mínima excede o teto de idade do estágio de origem: 0
- nenhuma

### 5.2 Empates de score entre evoluções irmãs (mesma origem) - escolha aleatória se ambas ficarem válidas
Total de grupos empatados: **0**
- nenhum

### 5.3 Evoluções sem nenhum requisito de stat (só nível/idade/outros)
Total: **0**

### 5.4 Digimons que não evoluem (sem saída) fora do último estágio
- nenhum

### 5.5 Evoluções que exigem EA/ED sem o Digimon de origem ter Role/AttackType especial
Origem com `AttackType` Physical exigindo EA/ED > 0: **36** evoluções (EA/ED só sobem por treino). Exemplos: Agumon->Dimetromon (EA 58, ED 0); Belphemon Sleep Mode->Belphemon Rage Mode (EA 0, ED 141); Biyomon->Birdramon (EA 17, ED 0); Devimon->Myotismon (EA 100, ED 0); Gabumon->Garurumon (EA 17, ED 0); Gaogamon->WereGarurumon (EA 47, ED 0); Gigimon->Elizamon (EA 34, ED 0); Gomamon->Tortamon (EA 0, ED 43); Goromon->Kotemon (EA 0, ED 18); Greymon->MetalGreymon (EA 30, ED 0); Greymon->RizeGreymon (EA 30, ED 0); Groundramon->Dorugoramon (EA 0, ED 56)

## 6. Receitas Jogress (fusão de dois Digimons)

Sem requisito de nível/stats: só precisa dos dois Digimons certos e capacidade. O resultado é um Digimon **novo** com stats = mistura da média dos pais (multiplicador abaixo).

| Resultado | Digimon A | Digimon B | xMult |
|---|---|---|---|
| Examon (MegaPlus) | SlayerDramon (Mega) | Breakdramon (Mega) | 1.3 |
| Omegamon (MegaPlus) | WarGreymon (Mega) | MetalGarurumon (Mega) | 1.3 |
| Paildramon (Ultimate) | ExVeemon (Champion) | Stingmon (Champion) | 1.3 |

## 7. Todos os Digimons e suas evoluções

Legenda das colunas: `Lv`, `Idade`, stats (`HP PA PD EA ED Vel`), `Outros` (Batalhas/Vitórias/Disciplina/Felicidade), `xMult`, `Score`, `Fonte` (canon/design), `Tipo` (normal/dna/mode). `PA`=Ataque Físico, `PD`=Defesa Física, `EA`=Ataque Especial, `ED`=Defesa Especial.

### Estágio Baby (20 Digimons)

#### Botamon (#25) - Baby | Role Warrior | Physical | Free / Neutral | Ovo: Dragon

- Stats base: HP 22 / PA 12 / PD 7 / EA 2 / ED 4 / Vel 5
- Evolui de: nasce de ovo do tipo Dragon

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Koromon (#29) | InTraining | 2 | 2 | 25 | 13 | - | - | - | 5 | - | 1.15 | 47 | canon | normal |
| Tsunomon (#30) | InTraining | 2 | - | 25 | 22 | 8 | - | - | - | Batalhas≥8 | 1.2 | 65 | design | normal |
| Wanyamon (#37) | InTraining | 2 | - | 25 | 13 | 14 | - | - | - | - | 1.15 | 54 | design | normal |

#### Punimon (#26) - Baby | Role Warrior | Special | Free / Neutral | Ovo: Beast

- Stats base: HP 22 / PA 2 / PD 7 / EA 12 / ED 4 / Vel 5
- Evolui de: nasce de ovo do tipo Beast

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Tsunomon (#30) | InTraining | 2 | - | 25 | 12 | 4 | - | - | 5 | - | 1.2 | 48 | canon | normal |
| Gummymon (#34) | InTraining | 2 | 2 | 24 | - | 8 | 11 | - | - | - | 1.15 | 47 | design | normal |
| Viximon (#35) | InTraining | 2 | - | 25 | - | 14 | 6 | - | 5 | - | 1.15 | 52 | design | normal |

#### Yuramon (#27) - Baby | Role Ranged | Special | Free / Neutral | Ovo: Forest

- Stats base: HP 22 / PA 2 / PD 4 / EA 11 / ED 4 / Vel 9
- Evolui de: nasce de ovo do tipo Forest

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Tanemon (#36) | InTraining | 2 | 2 | 25 | - | - | 10 | - | 9 | - | 1.15 | 48 | canon | normal |
| Yokomon (#58) | InTraining | 2 | - | 25 | 12 | - | 10 | - | 9 | - | 1.15 | 58 | design | normal |
| Budmon (#190) | InTraining | 2 | - | 25 | - | - | 10 | - | 18 | Batalhas≥8 | 1.15 | 63 | design | normal |
| Minomon (#196) | InTraining | 2 | - | 25 | - | - | 19 | - | 9 | - | 1.15 | 55 | design | normal |

#### Keemon (#28) - Baby | Role Ranged | Special | Free / Neutral | Ovo: Dark

- Stats base: HP 22 / PA 2 / PD 4 / EA 11 / ED 4 / Vel 9
- Evolui de: nasce de ovo do tipo Dark

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Yaamon (#38) | InTraining | 2 | 2 | 25 | 3 | - | - | - | 9 | - | 1.15 | 41 | design | normal |
| Kyaromon (#90) | InTraining | 2 | - | 25 | - | - | 19 | - | 9 | - | 1.2 | 55 | design | normal |
| Tsumemon (#115) | InTraining | 2 | - | 31 | - | - | 17 | - | 14 | Disc≤40 | 1.2 | 124 | design | normal |

#### Poyomon (#57) - Baby | Role Ranged | Special | Free / Neutral | Ovo: Beast

- Stats base: HP 22 / PA 2 / PD 4 / EA 11 / ED 4 / Vel 9
- Evolui de: nasce de ovo do tipo Beast

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Tokomon (#31) | InTraining | 2 | 2 | 25 | - | - | 10 | - | 9 | - | 1.2 | 48 | canon | normal |
| Yokomon (#58) | InTraining | 2 | - | 25 | 12 | - | 10 | - | 9 | Batalhas≥8 | 1.15 | 66 | design | normal |
| Salamon (#70) | InTraining | 2 | - | 25 | - | - | 10 | - | 18 | Batalhas≥8 | 1.15 | 63 | design | normal |

#### Pichimon (#63) - Baby | Role Warrior | Physical | Free / Neutral | Ovo: Water

- Stats base: HP 22 / PA 12 / PD 7 / EA 2 / ED 4 / Vel 5
- Evolui de: nasce de ovo do tipo Water

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Tsunomon (#30) | InTraining | 2 | - | 25 | 22 | 8 | - | - | - | - | 1.2 | 57 | design | normal |
| Bukamon (#64) | InTraining | 2 | 2 | 25 | 13 | - | - | - | 5 | - | 1.15 | 47 | canon | normal |

#### Nyaromon (#69) - Baby | Role Ranged | Special | Free / Neutral | Ovo: Light

- Stats base: HP 22 / PA 2 / PD 4 / EA 11 / ED 4 / Vel 9
- Evolui de: nasce de ovo do tipo Light

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Tokomon (#31) | InTraining | 2 | - | 31 | - | - | 14 | - | 14 | - | 1.2 | 61 | design | normal |
| Salamon (#70) | InTraining | 2 | - | 30 | - | - | 15 | - | 11 | - | 1.15 | 58 | canon | normal |

#### Yolkmon (#76) - Baby | Role Ranged | Special | Data / Wind | Ovo: Dragon

- Stats base: HP 22 / PA 4 / PD 6 / EA 11 / ED 6 / Vel 9
- Evolui de: nasce de ovo do tipo Dragon

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Fluffymon (#77) | InTraining | 2 | - | 25 | - | - | 19 | - | 9 | - | 1.2 | 55 | design | normal |
| Babydmon (#173) | InTraining | 2 | 2 | 24 | - | 7 | 10 | - | 9 | - | 1.15 | 54 | design | normal |

#### Jyarimon (#83) - Baby | Role Assassin | Special | Virus / Dark | Ovo: Dragon

- Stats base: HP 24 / PA 9 / PD 9 / EA 6 / ED 6 / Vel 7
- Evolui de: nasce de ovo do tipo Dragon

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Koromon (#29) | InTraining | 2 | - | 27 | 19 | 10 | - | - | 7 | - | 1.15 | 65 | design | normal |
| Gigimon (#33) | InTraining | 2 | 2 | 27 | 10 | - | - | - | 7 | - | 1.2 | 48 | canon | normal |

#### Pafumon (#89) - Baby | Role Support | Special | Virus / Dark | Ovo: Dark | Suporte: Buffer

- Stats base: HP 20 / PA 3 / PD 9 / EA 9 / ED 9 / Vel 6
- Evolui de: nasce de ovo do tipo Dark

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Yaamon (#38) | InTraining | 2 | - | 23 | 13 | 10 | 8 | 8 | - | - | 1.15 | 64 | design | normal |
| Kyaromon (#90) | InTraining | 2 | 2 | 23 | - | - | 8 | - | 6 | - | 1.2 | 41 | design | normal |
| PetiMeramon (#101) | InTraining | 2 | - | 23 | - | 10 | 17 | 8 | - | - | 1.2 | 60 | design | normal |

#### Mokumon (#100) - Baby | Role Assassin | Special | Virus / Dark | Ovo: Fire

- Stats base: HP 20 / PA 6 / PD 7 / EA 9 / ED 7 / Vel 6
- Evolui de: nasce de ovo do tipo Fire

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Kyaromon (#90) | InTraining | 2 | - | 28 | - | 10 | 14 | 10 | - | - | 1.2 | 64 | design | normal |
| PetiMeramon (#101) | InTraining | 2 | - | 28 | - | - | 13 | - | 8 | - | 1.2 | 51 | canon | normal |

#### Sunamon (#107) - Baby | Role Tank | Physical | Virus / Earth | Ovo: Beast

- Stats base: HP 24 / PA 9 / PD 10 / EA 4 / ED 6 / Vel 5
- Evolui de: nasce de ovo do tipo Beast

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Viximon (#35) | InTraining | 2 | - | 34 | 14 | 14 | - | 8 | - | - | 1.15 | 72 | design | normal |
| Goromon (#108) | InTraining | 2 | - | 34 | 13 | - | - | - | 7 | - | 1.2 | 56 | canon | normal |

#### Kuramon (#114) - Baby | Role Ranged | Special | Virus / Dark | Ovo: Dark

- Stats base: HP 20 / PA 5 / PD 6 / EA 10 / ED 6 / Vel 8
- Evolui de: nasce de ovo do tipo Dark

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Yaamon (#38) | InTraining | 2 | 2 | 23 | - | 7 | 9 | - | 8 | - | 1.15 | 51 | design | normal |
| Tsumemon (#115) | InTraining | 2 | - | 22 | - | - | 18 | - | 8 | - | 1.2 | 50 | canon | normal |

#### Puttimon (#172) - Baby | Role Warrior | Physical | Data / Fire | Ovo: Dragon

- Stats base: HP 24 / PA 6 / PD 4 / EA 2 / ED 3 / Vel 10
- Evolui de: nasce de ovo do tipo Dragon

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Tokomon (#31) | InTraining | 2 | - | 27 | 16 | - | - | - | 10 | - | 1.2 | 55 | design | normal |
| Fluffymon (#77) | InTraining | 2 | - | 27 | 7 | - | - | - | 19 | Batalhas≥8 | 1.2 | 63 | design | normal |
| Babydmon (#173) | InTraining | 2 | 2 | 27 | 7 | - | - | - | 10 | - | 1.15 | 48 | canon | normal |

#### Chibomon (#183) - Baby | Role Warrior | Physical | Free / Neutral | Ovo: Dragon

- Stats base: HP 22 / PA 10 / PD 8 / EA 2 / ED 4 / Vel 6
- Evolui de: nasce de ovo do tipo Dragon

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Koromon (#29) | InTraining | 2 | - | 25 | 20 | 9 | - | - | 6 | - | 1.15 | 62 | design | normal |
| DemiVeemon (#184) | InTraining | 2 | 2 | 25 | 11 | - | - | - | 6 | - | 1.15 | 46 | canon | normal |

#### Dodomon (#185) - Baby | Role Warrior | Physical | Free / Neutral | Ovo: Dragon

- Stats base: HP 22 / PA 11 / PD 7 / EA 2 / ED 4 / Vel 6
- Evolui de: nasce de ovo do tipo Dragon

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Gigimon (#33) | InTraining | 2 | - | 31 | 17 | 9 | - | - | 9 | - | 1.2 | 68 | design | normal |
| Dorimon (#186) | InTraining | 2 | - | 30 | 15 | - | - | - | 7 | - | 1.15 | 54 | canon | normal |

#### Zerimon (#187) - Baby | Role Ranged | Special | Free / Neutral | Ovo: Beast

- Stats base: HP 22 / PA 2 / PD 4 / EA 11 / ED 4 / Vel 9
- Evolui de: nasce de ovo do tipo Beast

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Gummymon (#34) | InTraining | 2 | - | 33 | 3 | - | - | - | 14 | - | 1.15 | 52 | canon | normal |
| Viximon (#35) | InTraining | 2 | - | 31 | - | - | 13 | - | 12 | - | 1.15 | 58 | design | normal |

#### Pabumon (#188) - Baby | Role Ranged | Special | Free / Neutral | Ovo: Forest

- Stats base: HP 22 / PA 2 / PD 4 / EA 11 / ED 5 / Vel 9
- Evolui de: nasce de ovo do tipo Forest

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Motimon (#32) | InTraining | 2 | - | 34 | - | - | 12 | - | 10 | - | 1.15 | 58 | canon | normal |
| Tanemon (#36) | InTraining | 2 | - | 31 | - | - | 17 | - | 14 | - | 1.15 | 64 | design | normal |

#### Nyokimon (#189) - Baby | Role Ranged | Special | Free / Neutral | Ovo: Forest

- Stats base: HP 22 / PA 2 / PD 4 / EA 11 / ED 4 / Vel 9
- Evolui de: nasce de ovo do tipo Forest

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Tanemon (#36) | InTraining | 2 | - | 25 | - | - | 19 | - | 9 | - | 1.15 | 55 | design | normal |
| Yokomon (#58) | InTraining | 2 | 2 | 25 | - | - | 10 | - | 9 | - | 1.15 | 48 | canon | normal |
| Budmon (#190) | InTraining | 2 | - | 24 | - | - | 10 | - | 18 | - | 1.15 | 54 | design | normal |

#### Leafmon (#195) - Baby | Role Ranged | Special | Free / Neutral | Ovo: Forest

- Stats base: HP 22 / PA 2 / PD 4 / EA 11 / ED 4 / Vel 9
- Evolui de: nasce de ovo do tipo Forest

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Tanemon (#36) | InTraining | 2 | - | 31 | - | - | 14 | - | 18 | - | 1.15 | 65 | design | normal |
| Minomon (#196) | InTraining | 2 | - | 31 | - | - | 17 | - | 14 | - | 1.15 | 64 | canon | normal |

### Estágio InTraining (23 Digimons)

#### Koromon (#29) - InTraining | Role Warrior | Physical | Free / Neutral | Ovo: Dragon

- Stats base: HP 48 / PA 22 / PD 12 / EA 4 / ED 8 / Vel 9
- Evolui de: Botamon (#25, Lv 2); Chibomon (#183, Lv 2); Jyarimon (#83, Lv 2)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Agumon (#1) | Rookie | 2 | - | 35 | 38 | - | 1 | - | 6 | - | 1.25 | 82 | canon | normal |
| Agumon (Savers) (#11) | Rookie | 2 | 5 | 35 | 17 | - | - | - | - | - | 1.25 | 59 | canon | normal |
| Veemon (#139) | Rookie | 2 | - | 35 | 17 | 9 | - | - | 25 | - | 1.25 | 88 | design | normal |

#### Tsunomon (#30) - InTraining | Role Warrior | Physical | Free / Neutral | Ovo: Dragon

- Stats base: HP 48 / PA 22 / PD 12 / EA 4 / ED 8 / Vel 9
- Evolui de: Botamon (#25, Lv 2); Pichimon (#63, Lv 2); Punimon (#26, Lv 2)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Gabumon (#2) | Rookie | 2 | - | 36 | 16 | - | 1 | - | 25 | - | 1.3 | 80 | canon | normal |
| Elizamon (#84) | Rookie | 2 | 5 | 36 | 16 | 9 | - | - | - | - | 1.25 | 68 | design | normal |
| Kotemon (#145) | Rookie | 2 | - | 36 | 38 | 9 | - | - | - | - | 1.25 | 85 | design | normal |

#### Tokomon (#31) - InTraining | Role Ranged | Physical | Free / Neutral | Ovo: Dragon

- Stats base: HP 48 / PA 20 / PD 7 / EA 4 / ED 8 / Vel 17
- Evolui de: Nyaromon (#69, Lv 2); Poyomon (#57, Lv 2); Puttimon (#172, Lv 2)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Patamon (#4) | Rookie | 2 | - | 67 | 30 | - | - | - | 26 | - | 1.25 | 125 | canon | normal |
| Kudamon (#129) | Rookie | 3 | 1 | 67 | 29 | - | - | - | 29 | - | 1.25 | 129 | design | normal |

#### Motimon (#32) - InTraining | Role Ranged | Physical | Free / Neutral | Ovo: Dragon

- Stats base: HP 48 / PA 20 / PD 7 / EA 4 / ED 8 / Vel 17
- Evolui de: Pabumon (#188, Lv 2)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Tentomon (#3) | Rookie | 2 | - | 67 | - | 11 | - | 12 | - | - | 1.25 | 92 | canon | normal |
| Wormmon (#197) | Rookie | 2 | - | 67 | 31 | - | - | - | 29 | - | 1.25 | 129 | canon | normal |
| Palmon (#6) | Rookie | 3 | 1 | 67 | 34 | - | - | - | 28 | - | 1.25 | 133 | design | normal |

#### Gigimon (#33) - InTraining | Role Ranged | Physical | Free / Neutral | Ovo: Dragon

- Stats base: HP 48 / PA 20 / PD 7 / EA 4 / ED 8 / Vel 17
- Evolui de: Dodomon (#185, Lv 2); Jyarimon (#83, Lv 2)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Guilmon (#7) | Rookie | 2 | 5 | 37 | 14 | - | - | - | 11 | - | 1.25 | 69 | canon | normal |
| Elizamon (#84) | Rookie | 2 | - | 67 | - | - | 34 | - | 26 | Disc≤45 | 1.25 | 184 | design | normal |
| Dorumon (#149) | Rookie | 2 | - | 37 | 14 | - | - | - | 33 | Batalhas≥8 | 1.25 | 94 | design | normal |

#### Gummymon (#34) - InTraining | Role Ranged | Physical | Free / Neutral | Ovo: Dragon

- Stats base: HP 48 / PA 20 / PD 7 / EA 4 / ED 8 / Vel 17
- Evolui de: Punimon (#26, Lv 2); Zerimon (#187, Lv 2)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Gaomon (#5) | Rookie | 2 | - | 67 | 34 | - | - | - | 26 | - | 1.25 | 129 | design | normal |
| Terriermon (#8) | Rookie | 3 | 1 | 67 | - | - | 6 | - | 26 | - | 1.25 | 103 | canon | normal |

#### Viximon (#35) - InTraining | Role Warrior | Physical | Free / Neutral | Ovo: Beast

- Stats base: HP 48 / PA 22 / PD 12 / EA 4 / ED 8 / Vel 9
- Evolui de: Punimon (#26, Lv 2); Sunamon (#107, Lv 2); Zerimon (#187, Lv 2)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Renamon (#9) | Rookie | 3 | 1 | - | - | - | 6 | - | 14 | - | 1.25 | 24 | canon | normal |
| Kudamon (#129) | Rookie | 3 | 1 | 67 | 32 | 17 | - | - | - | - | 1.25 | 120 | design | normal |

#### Tanemon (#36) - InTraining | Role Ranged | Special | Free / Neutral | Ovo: Forest

- Stats base: HP 48 / PA 4 / PD 7 / EA 20 / ED 8 / Vel 17
- Evolui de: Leafmon (#195, Lv 2); Nyokimon (#189, Lv 2); Pabumon (#188, Lv 2); Yuramon (#27, Lv 2)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Lalamon (#12) | Rookie | 2 | - | 67 | - | - | 28 | - | 29 | - | 1.25 | 126 | design | normal |
| Palmon (#6) | Rookie | 3 | 1 | 67 | 6 | - | - | - | 26 | - | 1.25 | 103 | canon | normal |

#### Wanyamon (#37) - InTraining | Role Warrior | Physical | Free / Neutral | Ovo: Forest

- Stats base: HP 48 / PA 22 / PD 12 / EA 4 / ED 8 / Vel 9
- Evolui de: Botamon (#25, Lv 2)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Gaomon (#5) | Rookie | 2 | - | 35 | 38 | 15 | - | - | - | Batalhas≥8 | 1.25 | 98 | design | normal |
| Kotemon (#145) | Rookie | 2 | 5 | 35 | 17 | 15 | - | - | - | - | 1.25 | 74 | design | normal |
| Kudamon (#129) | Rookie | 3 | 1 | 39 | 19 | 17 | - | - | 26 | - | 1.25 | 105 | design | normal |

#### Yaamon (#38) - InTraining | Role Warrior | Physical | Free / Neutral | Ovo: Dark

- Stats base: HP 48 / PA 22 / PD 12 / EA 4 / ED 8 / Vel 9
- Evolui de: Keemon (#28, Lv 2); Kuramon (#114, Lv 2); Pafumon (#89, Lv 2)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Shoemon (#91) | Rookie | 2 | - | 34 | 10 | 28 | - | - | - | - | 1.25 | 74 | design | normal |
| Ghostmon (#102) | Rookie | 2 | 5 | 34 | 10 | 7 | - | - | - | - | 1.25 | 58 | design | normal |
| Impmon (#10) | Rookie | 3 | 1 | 38 | - | - | 3 | - | 27 | - | 1.25 | 72 | canon | normal |

#### Yokomon (#58) - InTraining | Role Ranged | Physical | Free / Neutral | Ovo: Beast

- Stats base: HP 48 / PA 20 / PD 7 / EA 4 / ED 8 / Vel 17
- Evolui de: Nyokimon (#189, Lv 2); Poyomon (#57, Lv 2); Yuramon (#27, Lv 2)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Biyomon (#59) | Rookie | 2 | 5 | 35 | 9 | - | - | - | 12 | - | 1.25 | 63 | canon | normal |
| Muchomon (#96) | Rookie | 2 | - | 35 | 36 | - | - | - | - | - | 1.25 | 73 | design | normal |
| Wormmon (#197) | Rookie | 2 | - | 35 | 9 | - | - | - | 33 | - | 1.25 | 79 | canon | normal |

#### Bukamon (#64) - InTraining | Role Warrior | Physical | Free / Neutral | Ovo: Water

- Stats base: HP 48 / PA 22 / PD 12 / EA 4 / ED 8 / Vel 9
- Evolui de: Pichimon (#63, Lv 2)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Gabumon (#2) | Rookie | 2 | - | 67 | 38 | 17 | - | - | - | - | 1.3 | 124 | design | normal |
| Gomamon (#65) | Rookie | 2 | - | 67 | 33 | - | - | - | 14 | - | 1.25 | 116 | canon | normal |

#### Salamon (#70) - InTraining | Role Ranged | Physical | Free / Neutral | Ovo: Light

- Stats base: HP 48 / PA 20 / PD 7 / EA 4 / ED 8 / Vel 17
- Evolui de: Nyaromon (#69, Lv 2); Poyomon (#57, Lv 2)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Patamon (#4) | Rookie | 2 | - | 67 | 34 | - | - | - | 28 | Batalhas≥8 | 1.25 | 139 | design | normal |
| Gatomon (#71) | Rookie | 2 | - | 67 | - | - | 28 | - | 26 | - | 1.25 | 123 | canon | normal |
| Lucemon (#124) | Rookie | 3 | 1 | 67 | - | - | 30 | - | 28 | Disc≤40 | 1.25 | 189 | design | normal |

#### Fluffymon (#77) - InTraining | Role Ranged | Special | Data / Wind | Ovo: Dragon

- Stats base: HP 46 / PA 8 / PD 11 / EA 21 / ED 11 / Vel 19
- Evolui de: Puttimon (#172, Lv 2); Yolkmon (#76, Lv 2)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Biyomon (#59) | Rookie | 2 | - | 35 | - | 7 | 8 | - | 35 | - | 1.25 | 87 | design | normal |
| Pteromon (#78) | Rookie | 2 | 5 | 35 | - | - | 8 | - | 13 | - | 1.25 | 63 | design | normal |
| Muchomon (#96) | Rookie | 2 | - | 35 | 24 | 7 | 8 | - | 13 | Batalhas≥8 | 1.25 | 97 | design | normal |

#### Kyaromon (#90) - InTraining | Role Support | Special | Virus / Dark | Ovo: Dark | Suporte: Buffer

- Stats base: HP 44 / PA 8 / PD 15 / EA 18 / ED 15 / Vel 12
- Evolui de: Keemon (#28, Lv 2); Mokumon (#100, Lv 2); Pafumon (#89, Lv 2)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| BEMmon (#116) | Rookie | 2 | - | 61 | - | 21 | 31 | 21 | - | - | 1.25 | 136 | design | normal |
| Lucemon (#124) | Rookie | 3 | 1 | - | - | - | 44 | - | 38 | - | 1.25 | 86 | design | normal |
| Phascomon (#126) | Rookie | 3 | 1 | 62 | - | 26 | 28 | 25 | - | - | 1.25 | 145 | design | normal |

#### PetiMeramon (#101) - InTraining | Role Assassin | Special | Virus / Dark | Ovo: Dark

- Stats base: HP 44 / PA 11 / PD 13 / EA 19 / ED 13 / Vel 13
- Evolui de: Mokumon (#100, Lv 2); Pafumon (#89, Lv 2)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Ghostmon (#102) | Rookie | 2 | - | 61 | - | 18 | 33 | 18 | - | - | 1.25 | 132 | design | normal |
| Candlemon (#169) | Rookie | 2 | - | 55 | - | - | 42 | - | 30 | - | 1.25 | 129 | canon | normal |

#### Goromon (#108) - InTraining | Role Tank | Physical | Virus / Earth | Ovo: Beast

- Stats base: HP 50 / PA 19 / PD 20 / EA 8 / ED 12 / Vel 10
- Evolui de: Sunamon (#107, Lv 2)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Sunarizamon (#109) | Rookie | 2 | - | 70 | 27 | - | - | - | 14 | - | 1.25 | 113 | canon | normal |
| Kotemon (#145) | Rookie | 2 | - | 70 | 32 | 32 | - | 18 | - | - | 1.25 | 154 | design | normal |

#### Tsumemon (#115) - InTraining | Role Ranged | Special | Virus / Dark | Ovo: Dark

- Stats base: HP 42 / PA 10 / PD 11 / EA 20 / ED 12 / Vel 17
- Evolui de: Keemon (#28, Lv 2); Kuramon (#114, Lv 2)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| BEMmon (#116) | Rookie | 2 | - | 59 | - | - | 28 | - | 24 | - | 1.25 | 113 | design | normal |
| Phascomon (#126) | Rookie | 3 | 1 | 68 | - | 28 | - | - | - | - | 1.25 | 100 | design | normal |
| Hackmon (#163) | Rookie | 3 | 1 | 59 | - | - | 30 | 18 | 29 | - | 1.25 | 140 | design | normal |

#### Babydmon (#173) - InTraining | Role Warrior | Physical | Data / Fire | Ovo: Dragon

- Stats base: HP 46 / PA 20 / PD 10 / EA 4 / ED 7 / Vel 14
- Evolui de: Puttimon (#172, Lv 2); Yolkmon (#76, Lv 2)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Dracomon (#132) | Rookie | 2 | - | - | 28 | - | - | - | 30 | - | 1.25 | 60 | canon | normal |
| Dorumon (#149) | Rookie | 2 | - | 64 | 32 | 15 | - | - | 24 | - | 1.25 | 137 | design | normal |

#### DemiVeemon (#184) - InTraining | Role Warrior | Physical | Free / Neutral | Ovo: Dragon

- Stats base: HP 48 / PA 21 / PD 11 / EA 4 / ED 8 / Vel 10
- Evolui de: Chibomon (#183, Lv 2)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Dracomon (#132) | Rookie | 2 | - | 67 | 36 | 17 | - | - | - | - | 1.25 | 122 | design | normal |
| Veemon (#139) | Rookie | 2 | - | - | 29 | - | - | - | 35 | - | 1.25 | 66 | canon | normal |

#### Dorimon (#186) - InTraining | Role Warrior | Physical | Free / Neutral | Ovo: Dragon

- Stats base: HP 48 / PA 22 / PD 12 / EA 4 / ED 8 / Vel 9
- Evolui de: Dodomon (#185, Lv 2)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Dorumon (#149) | Rookie | 2 | - | - | 27 | - | - | - | 37 | - | 1.25 | 66 | canon | normal |
| Hackmon (#163) | Rookie | 3 | 1 | 67 | 34 | 18 | - | - | - | - | 1.25 | 123 | design | normal |

#### Budmon (#190) - InTraining | Role Ranged | Special | Free / Neutral | Ovo: Forest

- Stats base: HP 48 / PA 4 / PD 8 / EA 19 / ED 9 / Vel 15
- Evolui de: Nyokimon (#189, Lv 2); Yuramon (#27, Lv 2)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Lalamon (#12) | Rookie | 2 | - | 75 | - | - | - | - | 22 | - | 1.25 | 99 | canon | normal |
| Wormmon (#197) | Rookie | 2 | - | 67 | - | - | 26 | - | 26 | - | 1.25 | 121 | canon | normal |
| Palmon (#6) | Rookie | 3 | 1 | 67 | - | - | 26 | - | 25 | - | 1.25 | 122 | design | normal |

#### Minomon (#196) - InTraining | Role Ranged | Special | Free / Neutral | Ovo: Forest

- Stats base: HP 48 / PA 4 / PD 9 / EA 18 / ED 9 / Vel 14
- Evolui de: Leafmon (#195, Lv 2); Yuramon (#27, Lv 2)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Tentomon (#3) | Rookie | 2 | - | 35 | - | 25 | 16 | - | 11 | - | 1.25 | 89 | canon | normal |
| Palmon (#6) | Rookie | 2 | - | 35 | - | 6 | 18 | - | 30 | - | 1.25 | 91 | design | normal |
| Wormmon (#197) | Rookie | 2 | 5 | 35 | - | 6 | 18 | - | 11 | - | 1.25 | 77 | canon | normal |

### Estágio Rookie (32 Digimons)

#### Agumon (#1) - Rookie | Role Warrior | Physical | Vacine / Fire | Ovo: Dragon

- Stats base: HP 95 / PA 32 / PD 21 / EA 32 / ED 16 / Vel 14
- Evolui de: Koromon (#29, Lv 2)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Greymon (#13) | Champion | 12 | 2 | 150 | 61 | 55 | - | - | 24 | - | 1.85 | 304 | canon | normal |
| Devimon (#45) | Champion | 12 | 2 | 151 | 70 | 37 | - | - | 45 | Disc≤45 | 1.85 | 372 | canon | normal |
| Dimetromon (#85) | Champion | 12 | 2 | 152 | 59 | 38 | 58 | - | 40 | - | 1.8 | 361 | design | normal |

#### Gabumon (#2) - Rookie | Role Ranged | Physical | Data / Fire | Ovo: Beast

- Stats base: HP 76 / PA 42 / PD 14 / EA 9 / ED 16 / Vel 35
- Evolui de: Bukamon (#64, Lv 2); Tsunomon (#30, Lv 2)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Garurumon (#14) | Champion | 12 | 2 | 120 | - | - | 17 | - | 60 | - | 1.85 | 211 | canon | normal |
| IceDevimon (#46) | Champion | 12 | 2 | 120 | 92 | - | - | - | 67 | - | 1.85 | 293 | design | normal |

#### Tentomon (#3) - Rookie | Role Tank | Special | Vacine / Electric | Ovo: Forest

- Stats base: HP 135 / PA 9 / PD 32 / EA 9 / ED 32 / Vel 11
- Evolui de: Minomon (#196, Lv 2); Motimon (#32, Lv 2)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Kabuterimon (#15) | Champion | 12 | 13 | 124 | - | 38 | - | 16 | - | - | 1.85 | 203 | canon | normal |
| Togemon (#17) | Champion | 12 | 2 | 124 | 69 | 38 | - | 16 | - | - | 1.85 | 261 | design | normal |

#### Patamon (#4) - Rookie | Role Ranged | Physical | Data / Wind | Ovo: Light

- Stats base: HP 76 / PA 42 / PD 14 / EA 9 / ED 16 / Vel 35
- Evolui de: Salamon (#70, Lv 2); Tokomon (#31, Lv 2)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Angemon (#16) | Champion | 12 | 2 | 120 | - | - | 17 | - | 60 | Felic≥55 | 1.85 | 266 | canon | normal |
| Galemon (#79) | Champion | 12 | 2 | 123 | 73 | - | - | - | 64 | - | 1.8 | 274 | design | normal |

#### Gaomon (#5) - Rookie | Role Warrior | Physical | Data / Wind | Ovo: Beast

- Stats base: HP 104 / PA 42 / PD 22 / EA 9 / ED 15 / Vel 22
- Evolui de: Gummymon (#34, Lv 2); Wanyamon (#37, Lv 2)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Garurumon (#14) | Champion | 12 | 13 | 113 | 58 | 31 | - | - | 21 | - | 1.85 | 248 | design | normal |
| Gaogamon (#23) | Champion | 12 | 2 | 113 | 108 | - | - | - | 21 | - | 1.85 | 256 | canon | normal |
| Gladimon (#146) | Champion | 18 | 5 | 141 | 70 | 42 | - | - | 82 | - | 1.9 | 358 | design | normal |

#### Palmon (#6) - Rookie | Role Ranged | Physical | Data / Earth | Ovo: Forest

- Stats base: HP 85 / PA 35 / PD 23 / EA 9 / ED 19 / Vel 30
- Evolui de: Budmon (#190, Lv 3); Minomon (#196, Lv 2); Motimon (#32, Lv 3); Tanemon (#36, Lv 3)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Togemon (#17) | Champion | 12 | 2 | 136 | 67 | - | - | - | 51 | - | 1.85 | 268 | canon | normal |
| Sunflowmon (#24) | Champion | 12 | 2 | 137 | 60 | 48 | - | - | 59 | - | 1.85 | 318 | design | normal |

#### Guilmon (#7) - Rookie | Role Warrior | Physical | Virus / Fire | Ovo: Dragon

- Stats base: HP 104 / PA 44 / PD 24 / EA 9 / ED 15 / Vel 18
- Evolui de: Gigimon (#33, Lv 2)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Growlmon (#18) | Champion | 12 | 2 | 114 | 104 | - | - | - | 24 | - | 1.85 | 256 | canon | normal |
| Devidramon (#47) | Champion | 12 | 2 | 70 | 115 | - | - | - | 75 | Disc≤30 | 1.85 | 344 | design | normal |
| Dimetromon (#85) | Champion | 12 | 13 | 114 | 45 | 34 | - | - | - | - | 1.8 | 218 | design | normal |

#### Terriermon (#8) - Rookie | Role Ranged | Special | Vacine / Wind | Ovo: Beast

- Stats base: HP 76 / PA 9 / PD 14 / EA 42 / ED 16 / Vel 35
- Evolui de: Gummymon (#34, Lv 3)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Gargomon (#19) | Champion | 12 | 2 | 120 | 17 | - | - | - | 60 | - | 1.85 | 211 | canon | normal |
| Galemon (#79) | Champion | 12 | 2 | 123 | - | - | 92 | - | 64 | - | 1.8 | 293 | design | normal |

#### Renamon (#9) - Rookie | Role Assassin | Special | Data / Earth | Ovo: Beast

- Stats base: HP 50 / PA 9 / PD 12 / EA 68 / ED 12 / Vel 47
- Evolui de: Viximon (#35, Lv 3)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Kyubimon (#20) | Champion | 12 | 13 | 80 | - | - | 29 | - | 36 | - | 1.85 | 170 | canon | normal |
| Reppamon (#130) | Champion | 18 | 5 | 82 | - | - | 114 | - | 41 | - | 1.9 | 260 | design | normal |

#### Impmon (#10) - Rookie | Role Ranged | Special | Virus / Dark | Ovo: Dark

- Stats base: HP 76 / PA 9 / PD 14 / EA 42 / ED 16 / Vel 35
- Evolui de: Yaamon (#38, Lv 3)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Wizardmon (#21) | Champion | 12 | 2 | 100 | - | - | 82 | - | 39 | Felic≥55 | 1.85 | 290 | design | normal |
| Devimon (#45) | Champion | 12 | 2 | 121 | - | - | 72 | - | 63 | Disc≤40 | 1.85 | 330 | design | normal |
| Bakemon (#103) | Champion | 12 | 13 | 100 | - | - | 22 | - | 39 | - | 1.8 | 186 | design | normal |

#### Agumon (Savers) (#11) - Rookie | Role Warrior | Physical | Vacine / Fire | Ovo: Dragon

- Stats base: HP 103 / PA 44 / PD 26 / EA 9 / ED 18 / Vel 13
- Evolui de: Koromon (#29, Lv 2)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Greymon (#13) | Champion | 12 | 2 | 112 | 85 | 35 | - | - | - | - | 1.85 | 246 | design | normal |
| Geogreymon (#44) | Champion | 12 | 13 | 112 | 47 | - | - | - | 19 | - | 1.85 | 203 | canon | normal |

#### Lalamon (#12) - Rookie | Role Support | Special | Data / Earth | Ovo: Forest | Suporte: Healer

- Stats base: HP 94 / PA 5 / PD 23 / EA 9 / ED 23 / Vel 28
- Evolui de: Budmon (#190, Lv 2); Tanemon (#36, Lv 2)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Togemon (#17) | Champion | 12 | 2 | 130 | 65 | 31 | - | 11 | 35 | - | 1.85 | 286 | design | normal |
| Sunflowmon (#24) | Champion | 12 | 13 | 130 | - | 31 | - | 11 | 35 | - | 1.85 | 232 | canon | normal |

#### Biyomon (#59) - Rookie | Role Ranged | Physical | Vacine / Wind | Ovo: Beast

- Stats base: HP 76 / PA 42 / PD 14 / EA 9 / ED 16 / Vel 35
- Evolui de: Fluffymon (#77, Lv 2); Yokomon (#58, Lv 2)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Birdramon (#60) | Champion | 12 | 2 | 120 | - | - | 17 | - | 60 | - | 1.85 | 211 | canon | normal |
| Cockatrimon (#97) | Champion | 12 | 2 | 123 | 92 | - | - | - | 62 | - | 1.8 | 291 | design | normal |

#### Gomamon (#65) - Rookie | Role Tank | Physical | Data / Water | Ovo: Water

- Stats base: HP 120 / PA 35 / PD 30 / EA 9 / ED 22 / Vel 16
- Evolui de: Bukamon (#64, Lv 2)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Ikkakumon (#66) | Champion | 12 | 2 | 190 | 67 | 57 | - | - | - | - | 1.85 | 328 | canon | normal |
| Tortamon (#110) | Champion | 12 | 2 | 192 | 76 | 64 | - | 43 | - | - | 1.8 | 389 | design | normal |

#### Gatomon (#71) - Rookie | Role Assassin | Special | Data / Light | Ovo: Light

- Stats base: HP 55 / PA 15 / PD 14 / EA 62 / ED 14 / Vel 50
- Evolui de: Salamon (#70, Lv 2)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Angemon (#16) | Champion | 12 | 13 | 90 | - | - | 42 | - | 46 | - | 1.85 | 203 | design | normal |
| Wizardmon (#21) | Champion | 12 | 2 | 90 | - | - | 100 | - | 80 | Disc≤40 | 1.85 | 344 | design | normal |

#### Pteromon (#78) - Rookie | Role Ranged | Special | Data / Wind | Ovo: Dragon

- Stats base: HP 68 / PA 12 / PD 17 / EA 58 / ED 19 / Vel 50
- Evolui de: Fluffymon (#77, Lv 2)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Birdramon (#60) | Champion | 12 | 2 | 98 | - | - | 98 | - | 38 | - | 1.85 | 248 | design | normal |
| Galemon (#79) | Champion | 12 | 13 | 98 | - | - | 27 | - | 38 | - | 1.8 | 188 | design | normal |
| Coredramon (Blue) (#136) | Champion | 18 | 5 | 110 | - | - | 27 | - | 108 | Batalhas≥8 | 1.9 | 276 | design | normal |

#### Elizamon (#84) - Rookie | Role Assassin | Special | Virus / Dark | Ovo: Dragon

- Stats base: HP 58 / PA 17 / PD 11 / EA 62 / ED 15 / Vel 54
- Evolui de: Gigimon (#33, Lv 2); Tsunomon (#30, Lv 2)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Devidramon (#47) | Champion | 12 | 2 | 93 | - | - | 24 | - | 104 | - | 1.85 | 235 | design | normal |
| Dimetromon (#85) | Champion | 12 | 13 | 87 | - | - | 24 | - | 34 | - | 1.8 | 170 | design | normal |
| Tortamon (#110) | Champion | 12 | 2 | 95 | - | 74 | 24 | - | 34 | - | 1.8 | 241 | design | normal |

#### Shoemon (#91) - Rookie | Role Support | Special | Virus / Dark | Ovo: Dark | Suporte: Debuffer

- Stats base: HP 72 / PA 12 / PD 23 / EA 40 / ED 25 / Vel 28
- Evolui de: Yaamon (#38, Lv 2)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Wizardmon (#21) | Champion | 12 | 2 | 99 | - | - | 80 | 12 | 25 | - | 1.85 | 230 | design | normal |
| ShoeShoemon (#92) | Champion | 12 | 13 | 99 | - | - | 21 | - | 25 | - | 1.8 | 170 | canon | normal |

#### Muchomon (#96) - Rookie | Role Warrior | Physical | Data / Wind | Ovo: Beast

- Stats base: HP 90 / PA 50 / PD 20 / EA 10 / ED 14 / Vel 38
- Evolui de: Fluffymon (#77, Lv 2); Yokomon (#58, Lv 2)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Birdramon (#60) | Champion | 12 | 2 | 107 | 55 | - | - | - | 88 | - | 1.85 | 264 | design | normal |
| Cockatrimon (#97) | Champion | 12 | 13 | 107 | 55 | - | - | - | 32 | - | 1.8 | 219 | design | normal |

#### Ghostmon (#102) - Rookie | Role Assassin | Special | Virus / Dark | Ovo: Dark

- Stats base: HP 62 / PA 14 / PD 14 / EA 55 / ED 16 / Vel 48
- Evolui de: PetiMeramon (#101, Lv 2); Yaamon (#38, Lv 2)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Devimon (#45) | Champion | 12 | 2 | 100 | - | - | 95 | - | 87 | Disc≤40 | 1.85 | 356 | design | normal |
| Bakemon (#103) | Champion | 12 | 13 | 93 | - | - | 27 | - | 33 | - | 1.8 | 178 | design | normal |

#### Sunarizamon (#109) - Rookie | Role Tank | Physical | Virus / Earth | Ovo: Beast

- Stats base: HP 95 / PA 44 / PD 34 / EA 10 / ED 22 / Vel 22
- Evolui de: Goromon (#108, Lv 2)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Dimetromon (#85) | Champion | 12 | 2 | 152 | 81 | 62 | - | 40 | - | - | 1.8 | 349 | design | normal |
| Tortamon (#110) | Champion | 12 | 2 | 143 | 66 | - | - | - | 33 | - | 1.8 | 256 | design | normal |

#### BEMmon (#116) - Rookie | Role Ranged | Special | Virus / Dark | Ovo: Dark

- Stats base: HP 64 / PA 14 / PD 16 / EA 52 / ED 18 / Vel 46
- Evolui de: Kyaromon (#90, Lv 2); Tsumemon (#115, Lv 2)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Bakemon (#103) | Champion | 12 | 2 | 103 | - | - | 92 | - | 33 | - | 1.8 | 242 | design | normal |
| Snatchmon (#117) | Champion | 12 | 13 | 96 | - | - | 38 | - | 33 | - | 1.8 | 192 | design | normal |

#### Lucemon (#124) - Rookie | Role Assassin | Special | Data / Dark | Ovo: Dark

- Stats base: HP 70 / PA 30 / PD 25 / EA 55 / ED 30 / Vel 48
- Evolui de: Kyaromon (#90, Lv 3); Salamon (#70, Lv 3)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Angemon (#16) | Champion | 12 | 13 | 97 | - | - | 41 | - | 46 | - | 1.85 | 209 | design | normal |
| Devimon (#45) | Champion | 12 | 2 | 97 | 88 | - | 41 | - | 46 | - | 1.85 | 286 | design | normal |

#### Phascomon (#126) - Rookie | Role Tank | Physical | Data / Dark | Ovo: Dark

- Stats base: HP 85 / PA 28 / PD 35 / EA 20 / ED 30 / Vel 20
- Evolui de: Kyaromon (#90, Lv 3); Tsumemon (#115, Lv 3)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| IceDevimon (#46) | Champion | 12 | 2 | 134 | 61 | 40 | - | - | 70 | Disc≤40 | 1.85 | 379 | design | normal |
| Snatchmon (#117) | Champion | 12 | 2 | 136 | - | 64 | 48 | 55 | - | Disc≤40 | 1.8 | 377 | design | normal |

#### Kudamon (#129) - Rookie | Role Ranged | Special | Vacine / Light | Ovo: Beast

- Stats base: HP 75 / PA 20 / PD 22 / EA 45 / ED 25 / Vel 55
- Evolui de: Tokomon (#31, Lv 3); Viximon (#35, Lv 3); Wanyamon (#37, Lv 3)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Angemon (#16) | Champion | 12 | 2 | 150 | - | - | 98 | - | 80 | - | 1.85 | 342 | design | normal |
| Reppamon (#130) | Champion | 18 | 5 | 180 | - | - | 100 | 60 | 77 | - | 1.9 | 440 | canon | normal |

#### Dracomon (#132) - Rookie | Role Warrior | Physical | Data / Fire | Ovo: Dragon

- Stats base: HP 80 / PA 38 / PD 28 / EA 18 / ED 22 / Vel 40
- Evolui de: Babydmon (#173, Lv 2); DemiVeemon (#184, Lv 2)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Coredramon (Green) (#133) | Champion | 12 | 13 | 103 | 53 | - | - | - | 30 | - | 1.8 | 211 | canon | normal |
| Coredramon (Blue) (#136) | Champion | 18 | 5 | 132 | 64 | - | - | - | 108 | - | 1.9 | 327 | canon | normal |

#### Veemon (#139) - Rookie | Role Warrior | Physical | Vacine / Fire | Ovo: Dragon

- Stats base: HP 78 / PA 36 / PD 26 / EA 16 / ED 20 / Vel 44
- Evolui de: DemiVeemon (#184, Lv 2); Koromon (#29, Lv 2)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Veedramon (#140) | Champion | 12 | 13 | 102 | 44 | - | - | - | 43 | - | 1.8 | 214 | canon | normal |
| ExVeemon (#191) | Champion | 18 | 5 | 131 | 110 | - | - | - | 49 | - | 1.9 | 313 | canon | normal |

#### Kotemon (#145) - Rookie | Role Warrior | Physical | Data / Neutral | Ovo: Beast

- Stats base: HP 82 / PA 40 / PD 30 / EA 14 / ED 18 / Vel 42
- Evolui de: Goromon (#108, Lv 2); Tsunomon (#30, Lv 2); Wanyamon (#37, Lv 2)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Gaogamon (#23) | Champion | 12 | 2 | 170 | 90 | 50 | - | - | 50 | - | 1.85 | 374 | design | normal |
| Gladimon (#146) | Champion | 18 | 5 | 195 | 90 | 70 | - | - | 61 | - | 1.9 | 439 | design | normal |

#### Dorumon (#149) - Rookie | Role Warrior | Physical | Data / Neutral | Ovo: Dragon

- Stats base: HP 76 / PA 34 / PD 24 / EA 20 / ED 22 / Vel 46
- Evolui de: Babydmon (#173, Lv 2); Dorimon (#186, Lv 2); Gigimon (#33, Lv 2)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Dorugamon (#150) | Champion | 12 | 13 | 103 | 41 | - | - | - | 43 | - | 1.8 | 212 | canon | normal |
| Raptordramon (#153) | Champion | 18 | 5 | 132 | 120 | - | - | - | 49 | - | 1.9 | 324 | canon | normal |
| DexDorugamon (#156) | Champion | 18 | 5 | 132 | 53 | - | - | - | 106 | - | 1.9 | 314 | canon | normal |

#### Hackmon (#163) - Rookie | Role Assassin | Physical | Data / Neutral | Ovo: Dark

- Stats base: HP 74 / PA 32 / PD 24 / EA 26 / ED 24 / Vel 50
- Evolui de: Dorimon (#186, Lv 3); Tsumemon (#115, Lv 3)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| BaoHackmon (#164) | Champion | 12 | 13 | 112 | 34 | - | - | - | 33 | - | 1.8 | 204 | canon | normal |
| Gladimon (#146) | Champion | 18 | 5 | 140 | 120 | 43 | - | - | 38 | - | 1.9 | 364 | design | normal |

#### Candlemon (#169) - Rookie | Role Ranged | Special | Virus / Fire | Ovo: Fire

- Stats base: HP 74 / PA 10 / PD 15 / EA 50 / ED 18 / Vel 34
- Evolui de: PetiMeramon (#101, Lv 2)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Wizardmon (#21) | Champion | 12 | 13 | 110 | - | - | 49 | - | 44 | - | 1.85 | 228 | canon | normal |
| Bakemon (#103) | Champion | 12 | 2 | 114 | - | - | 92 | - | 44 | - | 1.8 | 264 | design | normal |

#### Wormmon (#197) - Rookie | Role Assassin | Physical | Vacine / Plant | Ovo: Forest

- Stats base: HP 70 / PA 30 / PD 20 / EA 12 / ED 18 / Vel 48
- Evolui de: Budmon (#190, Lv 2); Minomon (#196, Lv 2); Motimon (#32, Lv 2); Yokomon (#58, Lv 2)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Kabuterimon (#15) | Champion | 12 | 13 | 99 | 33 | - | - | - | 36 | - | 1.85 | 193 | canon | normal |
| Togemon (#17) | Champion | 12 | 2 | 99 | 90 | - | - | - | 36 | - | 1.85 | 239 | design | normal |
| Stingmon (#198) | Champion | 12 | 2 | 99 | 33 | - | - | - | 98 | - | 1.85 | 244 | canon | normal |

### Estágio Champion (35 Digimons)

#### Greymon (#13) - Champion | Role Warrior | Physical | Vacine / Fire | Ovo: Dragon

- Stats base: HP 230 / PA 71 / PD 45 / EA 71 / ED 35 / Vel 30
- Evolui de: Agumon (#1, Lv 12); Agumon (Savers) (#11, Lv 12)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| MetalGreymon (#39) | Ultimate | 20 | 5 | 248 | 191 | - | 30 | - | 36 | - | 1.6 | 530 | canon | normal |
| RizeGreymon (#167) | Ultimate | 20 | 24 | 248 | 131 | 74 | 30 | - | - | - | 1.6 | 527 | design | normal |

#### Garurumon (#14) - Champion | Role Ranged | Special | Vacine / Fire | Ovo: Beast

- Stats base: HP 186 / PA 20 / PD 30 / EA 91 / ED 35 / Vel 76
- Evolui de: Gabumon (#2, Lv 12); Gaomon (#5, Lv 12)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| WereGarurumon (#40) | Ultimate | 20 | 5 | 247 | 34 | - | 127 | - | 59 | - | 1.6 | 492 | canon | normal |
| MachGaogamon (#48) | Ultimate | 20 | 24 | 247 | - | - | 38 | - | 59 | - | 1.6 | 388 | design | normal |

#### Kabuterimon (#15) - Champion | Role Tank | Special | Vacine / Electric | Ovo: Forest

- Stats base: HP 330 / PA 20 / PD 69 / EA 20 / ED 69 / Vel 25
- Evolui de: Tentomon (#3, Lv 12); Wormmon (#197, Lv 12)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| MegaKabuterimon (#51) | Ultimate | 20 | 5 | 273 | - | 75 | 19 | 105 | - | - | 1.6 | 497 | canon | normal |
| JewelBeemon (#199) | Ultimate | 20 | 24 | 273 | - | 75 | - | 36 | - | - | 1.6 | 428 | canon | normal |
| Knightmon (#147) | Ultimate | 28 | 8 | 311 | - | 144 | - | 36 | - | - | 1.7 | 527 | design | normal |

#### Angemon (#16) - Champion | Role Ranged | Special | Vacine / Light | Ovo: Light

- Stats base: HP 186 / PA 20 / PD 30 / EA 91 / ED 35 / Vel 76
- Evolui de: Gatomon (#71, Lv 12); Kudamon (#129, Lv 12); Lucemon (#124, Lv 12); Patamon (#4, Lv 12)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| MagnaAngemon (#43) | Ultimate | 20 | 5 | 218 | - | - | 127 | - | 93 | Felic≥55 | 1.6 | 518 | canon | normal |
| Angewomon (#72) | Ultimate | 20 | 24 | 218 | - | - | 52 | - | 93 | - | 1.6 | 407 | design | normal |
| Lucemon Falldown Mode (#125) | Ultimate | 20 | 5 | 298 | - | - | 167 | - | 124 | Disc≤35 | 1.6 | 679 | design | normal |

#### Togemon (#17) - Champion | Role Warrior | Physical | Data / Earth | Ovo: Forest

- Stats base: HP 250 / PA 82 / PD 67 / EA 19 / ED 38 / Vel 34
- Evolui de: Lalamon (#12, Lv 12); Palmon (#6, Lv 12); Tentomon (#3, Lv 12); Wormmon (#197, Lv 12)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Lillymon (#50) | Ultimate | 20 | 24 | 243 | 129 | - | - | - | 51 | - | 1.6 | 467 | canon | normal |
| Lilamon (#179) | Ultimate | 20 | 5 | 243 | 119 | 74 | - | 62 | - | - | 1.6 | 523 | design | normal |

#### Growlmon (#18) - Champion | Role Warrior | Physical | Virus / Fire | Ovo: Dragon

- Stats base: HP 250 / PA 96 / PD 53 / EA 19 / ED 34 / Vel 38
- Evolui de: Guilmon (#7, Lv 12)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| MetalGreymon (#39) | Ultimate | 20 | 5 | 398 | 177 | 88 | 60 | - | - | - | 1.6 | 748 | design | normal |
| WarGrowlmon (#41) | Ultimate | 20 | 5 | 375 | 163 | - | - | - | 61 | - | 1.6 | 624 | canon | normal |

#### Gargomon (#19) - Champion | Role Ranged | Physical | Vacine / Wind | Ovo: Beast

- Stats base: HP 186 / PA 91 / PD 30 / EA 20 / ED 35 / Vel 76
- Evolui de: Terriermon (#8, Lv 12)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Rapidmon (#42) | Ultimate | 20 | 24 | 248 | 84 | - | - | - | 113 | - | 1.6 | 489 | canon | normal |
| MachGaogamon (#48) | Ultimate | 20 | 5 | 248 | 143 | - | - | - | 113 | - | 1.6 | 529 | design | normal |

#### Kyubimon (#20) - Champion | Role Ranged | Special | Data / Fire | Ovo: Beast

- Stats base: HP 186 / PA 20 / PD 30 / EA 91 / ED 35 / Vel 76
- Evolui de: Renamon (#9, Lv 12)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Taomon (#49) | Ultimate | 20 | 24 | 209 | - | - | 66 | - | 76 | - | 1.6 | 395 | canon | normal |
| Mistymon (#170) | Ultimate | 20 | 5 | 209 | - | - | 66 | - | 142 | - | 1.6 | 442 | design | normal |

#### Wizardmon (#21) - Champion | Role Ranged | Special | Virus / Dark | Ovo: Dark

- Stats base: HP 186 / PA 20 / PD 30 / EA 91 / ED 35 / Vel 76
- Evolui de: Candlemon (#169, Lv 12); Gatomon (#71, Lv 12); Impmon (#10, Lv 12); Shoemon (#91, Lv 12)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Fantomon (#104) | Ultimate | 20 | 5 | 297 | - | - | 167 | - | 130 | Disc≤45 | 1.6 | 674 | design | normal |
| Mistymon (#170) | Ultimate | 20 | 5 | 280 | 95 | - | 120 | - | 140 | - | 1.6 | 660 | design | normal |

#### Gaogamon (#23) - Champion | Role Warrior | Physical | Data / Wind | Ovo: Beast

- Stats base: HP 250 / PA 91 / PD 48 / EA 19 / ED 34 / Vel 48
- Evolui de: Gaomon (#5, Lv 12); Kotemon (#145, Lv 12)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| WereGarurumon (#40) | Ultimate | 20 | 5 | 258 | 167 | 73 | 47 | - | 49 | - | 1.6 | 619 | design | normal |
| MachGaogamon (#48) | Ultimate | 20 | 24 | 258 | 146 | - | - | - | 49 | - | 1.6 | 497 | canon | normal |

#### Sunflowmon (#24) - Champion | Role Support | Special | Data / Earth | Ovo: Forest | Suporte: Debuffer

- Stats base: HP 230 / PA 12 / PD 50 / EA 19 / ED 50 / Vel 61
- Evolui de: Lalamon (#12, Lv 12); Palmon (#6, Lv 12)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Lillymon (#50) | Ultimate | 20 | 24 | 277 | - | 73 | - | 30 | 71 | - | 1.6 | 495 | design | normal |
| Lilamon (#179) | Ultimate | 20 | 5 | 277 | - | 73 | - | 86 | 71 | Felic≥40 | 1.6 | 572 | canon | normal |
| JewelBeemon (#199) | Ultimate | 20 | 5 | 277 | - | 73 | - | 30 | 111 | Felic≥40 | 1.6 | 556 | canon | normal |

#### Geogreymon (#44) - Champion | Role Warrior | Physical | Vacine / Fire | Ovo: Dragon

- Stats base: HP 250 / PA 96 / PD 58 / EA 19 / ED 38 / Vel 29
- Evolui de: Agumon (Savers) (#11, Lv 12)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| MetalGreymon (#39) | Ultimate | 20 | 5 | 256 | 149 | 70 | - | - | - | - | 1.6 | 500 | design | normal |
| RizeGreymon (#167) | Ultimate | 20 | 24 | 256 | 103 | - | - | - | 42 | - | 1.6 | 445 | canon | normal |

#### Devimon (#45) - Champion | Role Assassin | Physical | Virus / Dark | Ovo: Dark

- Stats base: HP 143 / PA 129 / PD 31 / EA 25 / ED 31 / Vel 92
- Evolui de: Agumon (#1, Lv 12); Ghostmon (#102, Lv 12); Impmon (#10, Lv 12); Lucemon (#124, Lv 12)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Lucemon Falldown Mode (#125) | Ultimate | 20 | 5 | 214 | 163 | - | - | - | 89 | - | 1.6 | 491 | design | normal |
| LadyDevimon (#180) | Ultimate | 20 | 24 | 214 | 95 | - | - | - | 89 | - | 1.6 | 442 | design | normal |
| SkullSatamon (#181) | Ultimate | 20 | 5 | 235 | 215 | - | - | - | 140 | Disc≤40 | 1.6 | 675 | canon | normal |
| Myotismon (#182) | Ultimate | 20 | 5 | 230 | - | - | 100 | - | 150 | Disc≤50 | 1.6 | 555 | canon | normal |

#### IceDevimon (#46) - Champion | Role Assassin | Physical | Virus / Dark | Ovo: Dark

- Stats base: HP 106 / PA 141 / PD 20 / EA 27 / ED 20 / Vel 128
- Evolui de: Gabumon (#2, Lv 12); Phascomon (#126, Lv 12)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Destromon (#118) | Ultimate | 20 | 5 | 173 | 132 | - | - | - | 154 | - | 1.6 | 484 | design | normal |
| Astamon (#127) | Ultimate | 20 | 24 | 173 | 132 | - | - | - | 126 | - | 1.6 | 475 | design | normal |

#### Devidramon (#47) - Champion | Role Assassin | Physical | Virus / Dark | Ovo: Dragon

- Stats base: HP 121 / PA 149 / PD 26 / EA 19 / ED 26 / Vel 104
- Evolui de: Elizamon (#84, Lv 12); Guilmon (#7, Lv 12)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| WarGrowlmon (#41) | Ultimate | 20 | 5 | 199 | 201 | - | - | - | 138 | - | 1.6 | 563 | design | normal |
| Lamiamon (#86) | Ultimate | 20 | 24 | 196 | 111 | - | - | - | 138 | - | 1.6 | 489 | design | normal |

#### Birdramon (#60) - Champion | Role Ranged | Special | Vacine / Wind | Ovo: Beast

- Stats base: HP 186 / PA 20 / PD 30 / EA 91 / ED 35 / Vel 76
- Evolui de: Biyomon (#59, Lv 12); Muchomon (#96, Lv 12); Pteromon (#78, Lv 12)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Garudamon (#61) | Ultimate | 20 | 5 | 220 | - | - | 127 | - | 77 | - | 1.6 | 449 | canon | normal |
| Delumon (#98) | Ultimate | 20 | 24 | 220 | - | - | 45 | - | 77 | - | 1.6 | 386 | design | normal |

#### Ikkakumon (#66) - Champion | Role Tank | Physical | Data / Water | Ovo: Water

- Stats base: HP 280 / PA 85 / PD 75 / EA 15 / ED 45 / Vel 28
- Evolui de: Gomamon (#65, Lv 12)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Zudomon (#67) | Ultimate | 20 | 5 | 448 | 136 | 120 | - | - | - | - | 1.6 | 729 | canon | normal |
| Triceramon (#111) | Ultimate | 20 | 5 | 445 | 156 | 137 | - | 75 | - | - | 1.6 | 838 | design | normal |

#### Galemon (#79) - Champion | Role Ranged | Special | Data / Wind | Ovo: Dragon

- Stats base: HP 195 / PA 26 / PD 40 / EA 120 / ED 46 / Vel 88
- Evolui de: Patamon (#4, Lv 12); Pteromon (#78, Lv 12); Terriermon (#8, Lv 12)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Grand Galemon (#80) | Ultimate | 20 | 24 | 218 | - | - | 49 | - | 81 | - | 1.6 | 392 | canon | normal |
| Wingdramon (#137) | Ultimate | 28 | 8 | 256 | - | - | 49 | - | 168 | - | 1.7 | 509 | design | normal |

#### Dimetromon (#85) - Champion | Role Assassin | Special | Virus / Dark | Ovo: Dragon

- Stats base: HP 175 / PA 46 / PD 42 / EA 118 / ED 40 / Vel 82
- Evolui de: Agumon (#1, Lv 12); Elizamon (#84, Lv 12); Guilmon (#7, Lv 12); Sunarizamon (#109, Lv 12)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Lamiamon (#86) | Ultimate | 20 | 24 | 204 | - | - | 48 | - | 64 | - | 1.6 | 360 | design | normal |
| Triceramon (#111) | Ultimate | 20 | 5 | 204 | - | 94 | 48 | - | 64 | - | 1.6 | 435 | design | normal |

#### ShoeShoemon (#92) - Champion | Role Support | Special | Virus / Dark | Ovo: Dark | Suporte: Buffer

- Stats base: HP 205 / PA 25 / PD 59 / EA 85 / ED 63 / Vel 48
- Evolui de: Shoemon (#91, Lv 12)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Chaperomon (#93) | Ultimate | 20 | 24 | 223 | - | - | 55 | - | 53 | - | 1.6 | 375 | design | normal |
| LadyDevimon (#180) | Ultimate | 20 | 5 | 223 | - | 71 | 121 | 37 | - | - | 1.6 | 477 | design | normal |

#### Cockatrimon (#97) - Champion | Role Warrior | Physical | Data / Wind | Ovo: Beast

- Stats base: HP 210 / PA 110 / PD 42 / EA 20 / ED 38 / Vel 70
- Evolui de: Biyomon (#59, Lv 12); Muchomon (#96, Lv 12)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Garudamon (#61) | Ultimate | 20 | 5 | 232 | 115 | - | - | - | 125 | - | 1.6 | 497 | design | normal |
| Delumon (#98) | Ultimate | 20 | 24 | 232 | 115 | - | - | - | 68 | - | 1.6 | 459 | design | normal |

#### Bakemon (#103) - Champion | Role Assassin | Special | Virus / Dark | Ovo: Dark

- Stats base: HP 165 / PA 28 / PD 34 / EA 112 / ED 36 / Vel 80
- Evolui de: BEMmon (#116, Lv 12); Candlemon (#169, Lv 12); Ghostmon (#102, Lv 12); Impmon (#10, Lv 12)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Fantomon (#104) | Ultimate | 20 | 24 | 209 | - | - | 66 | - | 73 | - | 1.6 | 392 | canon | normal |
| Myotismon (#182) | Ultimate | 20 | 5 | 209 | - | - | 148 | - | 73 | - | 1.6 | 455 | design | normal |

#### Tortamon (#110) - Champion | Role Tank | Physical | Virus / Earth | Ovo: Beast

- Stats base: HP 245 / PA 95 / PD 88 / EA 20 / ED 55 / Vel 38
- Evolui de: Elizamon (#84, Lv 12); Gomamon (#65, Lv 12); Sunarizamon (#109, Lv 12)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Triceramon (#111) | Ultimate | 20 | 24 | 233 | 89 | - | - | - | 53 | - | 1.6 | 419 | design | normal |
| Groundramon (#134) | Ultimate | 28 | 8 | 271 | 104 | 168 | - | 29 | - | - | 1.7 | 608 | design | normal |

#### Snatchmon (#117) - Champion | Role Ranged | Special | Virus / Dark | Ovo: Dark

- Stats base: HP 170 / PA 30 / PD 38 / EA 105 / ED 42 / Vel 78
- Evolui de: BEMmon (#116, Lv 12); Phascomon (#126, Lv 12)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Destromon (#118) | Ultimate | 20 | 24 | 202 | - | - | 80 | - | 61 | - | 1.6 | 387 | design | normal |
| Astamon (#127) | Ultimate | 20 | 5 | 202 | - | - | 80 | - | 132 | - | 1.6 | 439 | design | normal |

#### Reppamon (#130) - Champion | Role Ranged | Special | Vacine / Light | Ovo: Beast

- Stats base: HP 190 / PA 45 / PD 50 / EA 95 / ED 60 / Vel 85
- Evolui de: Kudamon (#129, Lv 18); Renamon (#9, Lv 18)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Taomon (#49) | Ultimate | 20 | 24 | 190 | - | - | 158 | 31 | 80 | - | 1.6 | 503 | design | normal |
| Chirinmon (#131) | Ultimate | 28 | 8 | 228 | - | - | 243 | 31 | 88 | - | 1.7 | 626 | canon | normal |

#### Coredramon (Green) (#133) - Champion | Role Warrior | Physical | Data / Earth | Ovo: Dragon

- Stats base: HP 200 / PA 85 / PD 70 / EA 30 / ED 45 / Vel 60
- Evolui de: Dracomon (#132, Lv 12)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Groundramon (#134) | Ultimate | 20 | 24 | 225 | 109 | - | - | - | 45 | - | 1.6 | 423 | canon | normal |
| Grademon (#154) | Ultimate | 28 | 8 | 263 | 183 | 93 | - | - | 70 | - | 1.7 | 645 | design | normal |

#### Coredramon (Blue) (#136) - Champion | Role Ranged | Physical | Data / Wind | Ovo: Dragon

- Stats base: HP 180 / PA 75 / PD 55 / EA 50 / ED 50 / Vel 90
- Evolui de: Dracomon (#132, Lv 18); Pteromon (#78, Lv 18)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Wingdramon (#137) | Ultimate | 20 | 24 | 208 | 75 | - | - | - | 126 | - | 1.6 | 453 | canon | normal |
| AeroVeedramon (#141) | Ultimate | 28 | 8 | 246 | 147 | 71 | - | - | 171 | - | 1.7 | 671 | design | normal |

#### Veedramon (#140) - Champion | Role Warrior | Physical | Vacine / Fire | Ovo: Dragon

- Stats base: HP 210 / PA 95 / PD 65 / EA 35 / ED 50 / Vel 70
- Evolui de: Veemon (#139, Lv 12)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| AeroVeedramon (#141) | Ultimate | 20 | 24 | 228 | 98 | - | - | - | 85 | - | 1.6 | 455 | canon | normal |
| Wingdramon (#137) | Ultimate | 28 | 8 | 266 | 114 | 92 | - | - | 160 | - | 1.7 | 668 | design | normal |

#### Gladimon (#146) - Champion | Role Warrior | Physical | Data / Neutral | Ovo: Beast

- Stats base: HP 205 / PA 100 / PD 70 / EA 30 / ED 42 / Vel 68
- Evolui de: Gaomon (#5, Lv 18); Hackmon (#163, Lv 18); Kotemon (#145, Lv 18)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Knightmon (#147) | Ultimate | 20 | 24 | 276 | 136 | - | - | - | 75 | - | 1.6 | 531 | design | normal |
| Grademon (#154) | Ultimate | 28 | 8 | 314 | 210 | 99 | - | - | 82 | - | 1.7 | 741 | design | normal |

#### Dorugamon (#150) - Champion | Role Warrior | Physical | Data / Wind | Ovo: Dragon

- Stats base: HP 195 / PA 88 / PD 60 / EA 35 / ED 45 / Vel 80
- Evolui de: Dorumon (#149, Lv 12)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Dorugreymon (#151) | Ultimate | 20 | 24 | 224 | 93 | - | - | - | 85 | - | 1.6 | 446 | canon | normal |
| DexDorugreymon (#157) | Ultimate | 28 | 8 | 262 | 176 | 87 | - | - | 93 | - | 1.7 | 654 | design | normal |

#### Raptordramon (#153) - Champion | Role Warrior | Physical | Data / Earth | Ovo: Dragon

- Stats base: HP 210 / PA 100 / PD 65 / EA 20 / ED 35 / Vel 65
- Evolui de: Dorumon (#149, Lv 18)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Grademon (#154) | Ultimate | 20 | 24 | 260 | 158 | - | - | - | 68 | - | 1.6 | 530 | canon | normal |
| Dorugreymon (#151) | Ultimate | 28 | 8 | 298 | 274 | 80 | - | - | 102 | - | 1.7 | 790 | design | normal |

#### DexDorugamon (#156) - Champion | Role Assassin | Physical | Virus / Dark | Ovo: Dragon

- Stats base: HP 185 / PA 92 / PD 55 / EA 38 / ED 40 / Vel 88
- Evolui de: Dorumon (#149, Lv 18)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| DexDorugreymon (#157) | Ultimate | 20 | 24 | 260 | 104 | - | - | - | 108 | - | 1.6 | 516 | canon | normal |
| Dorugreymon (#151) | Ultimate | 28 | 8 | 296 | 190 | 71 | - | - | 154 | - | 1.7 | 747 | design | normal |

#### BaoHackmon (#164) - Champion | Role Assassin | Physical | Data / Neutral | Ovo: Dark

- Stats base: HP 195 / PA 85 / PD 60 / EA 65 / ED 55 / Vel 85
- Evolui de: Hackmon (#163, Lv 12)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| SaviorHackmon (#165) | Ultimate | 20 | 24 | 239 | 83 | - | - | - | 75 | - | 1.6 | 441 | canon | normal |
| Knightmon (#147) | Ultimate | 28 | 8 | 277 | 187 | 87 | - | - | 82 | - | 1.7 | 669 | design | normal |

#### ExVeemon (#191) - Champion | Role Warrior | Physical | Vacine / Fire | Ovo: Dragon

- Stats base: HP 205 / PA 92 / PD 60 / EA 40 / ED 50 / Vel 80
- Evolui de: Veemon (#139, Lv 18)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| AeroVeedramon (#141) | Ultimate | 20 | 5 | 327 | 153 | 88 | - | - | 152 | - | 1.6 | 745 | design | normal |

#### Stingmon (#198) - Champion | Role Assassin | Physical | Vacine / Wind | Ovo: Forest

- Stats base: HP 170 / PA 120 / PD 45 / EA 30 / ED 45 / Vel 115
- Evolui de: Wormmon (#197, Lv 12)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| MegaKabuterimon (#51) | Ultimate | 20 | 24 | 222 | 96 | - | - | - | 176 | - | 1.6 | 538 | design | normal |
| AeroVeedramon (#141) | Ultimate | 20 | 5 | 222 | 96 | - | - | - | 207 | Felic≥40 | 1.6 | 590 | design | normal |
| JewelBeemon (#199) | Ultimate | 20 | 5 | 222 | 172 | - | - | - | 179 | - | 1.6 | 598 | canon | normal |

### Estágio Ultimate (38 Digimons)

#### MetalGreymon (#39) - Ultimate | Role Warrior | Physical | Vacine / Fire | Ovo: Dragon

- Stats base: HP 420 / PA 124 / PD 79 / EA 124 / ED 62 / Vel 53
- Evolui de: Geogreymon (#44, Lv 20); Greymon (#13, Lv 20); Growlmon (#18, Lv 20)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| WarGreymon (#52) | Mega | 28 | 8 | 436 | 278 | - | 56 | - | 57 | Batalhas≥12; Vitórias≥55%; Felic≥55 | 1.7 | 985 | canon | normal |
| ShineGreymon (#168) | Mega | 28 | 50 | 436 | 221 | 121 | 56 | - | - | - | 1.7 | 912 | design | normal |

#### WereGarurumon (#40) - Ultimate | Role Warrior | Physical | Vacine / Fire | Ovo: Beast

- Stats base: HP 420 / PA 123 / PD 79 / EA 123 / ED 62 / Vel 53
- Evolui de: Gaogamon (#23, Lv 20); Garurumon (#14, Lv 20)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| MetalGarurumon (#53) | Mega | 28 | 8 | 434 | 236 | - | 89 | - | 85 | Batalhas≥12; Vitórias≥55%; Felic≥55 | 1.7 | 1002 | canon | normal |
| Gankoomon (#162) | Mega | 28 | 50 | 434 | 132 | 90 | 89 | - | - | - | 1.7 | 823 | design | normal |

#### WarGrowlmon (#41) - Ultimate | Role Warrior | Physical | Virus / Fire | Ovo: Dragon

- Stats base: HP 460 / PA 168 / PD 92 / EA 34 / ED 59 / Vel 67
- Evolui de: Devidramon (#47, Lv 20); Growlmon (#18, Lv 20)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Gallantmon (#54) | Mega | 28 | 50 | 418 | 269 | - | - | - | 99 | - | 1.7 | 864 | canon | normal |
| Medieval Dukemon (#99) | Mega | 28 | 8 | 418 | 299 | 180 | - | - | - | - | 1.7 | 933 | design | normal |

#### Rapidmon (#42) - Ultimate | Role Ranged | Physical | Vacine / Wind | Ovo: Beast

- Stats base: HP 340 / PA 159 / PD 53 / EA 35 / ED 62 / Vel 132
- Evolui de: Gargomon (#19, Lv 20)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| MegaGargomon (#55) | Mega | 28 | 8 | 544 | 254 | - | - | - | 211 | Batalhas≥12; Vitórias≥55% | 1.7 | 1112 | canon | normal |
| Magnamon (#143) | Mega | 36 | 12 | 600 | 283 | 150 | - | - | 200 | - | 1.8 | 1281 | design | normal |

#### MagnaAngemon (#43) - Ultimate | Role Ranged | Special | Vacine / Light | Ovo: Light

- Stats base: HP 340 / PA 35 / PD 53 / EA 159 / ED 62 / Vel 132
- Evolui de: Angemon (#16, Lv 20)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Seraphimon (#56) | Mega | 28 | 8 | 387 | - | - | 251 | - | 155 | Batalhas≥12; Felic≥55 | 1.7 | 896 | canon | normal |
| Magnadramon (#73) | Mega | 28 | 50 | 387 | - | - | 192 | - | 155 | - | 1.7 | 812 | design | normal |

#### MachGaogamon (#48) - Ultimate | Role Warrior | Physical | Data / Wind | Ovo: Beast

- Stats base: HP 445 / PA 162 / PD 85 / EA 34 / ED 61 / Vel 85
- Evolui de: Gaogamon (#23, Lv 20); Gargomon (#19, Lv 20); Garurumon (#14, Lv 20)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Gankoomon (#162) | Mega | 28 | 50 | 443 | 169 | - | - | - | 86 | - | 1.7 | 776 | design | normal |
| MirageGaogamon (#176) | Mega | 28 | 8 | 443 | 226 | - | - | - | 86 | Felic≥40 | 1.7 | 831 | canon | normal |

#### Taomon (#49) - Ultimate | Role Ranged | Special | Data / Fire | Ovo: Beast

- Stats base: HP 331 / PA 36 / PD 53 / EA 162 / ED 62 / Vel 135
- Evolui de: Kyubimon (#20, Lv 20); Reppamon (#130, Lv 20)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Duftmon (#161) | Mega | 28 | 50 | 362 | - | - | 125 | - | 131 | - | 1.7 | 696 | design | normal |
| Sakuyamon (#175) | Mega | 28 | 8 | 362 | - | - | 227 | - | 131 | Batalhas≥12; Felic≥55 | 1.7 | 823 | canon | normal |

#### Lillymon (#50) - Ultimate | Role Ranged | Special | Data / Plant | Ovo: Forest

- Stats base: HP 340 / PA 35 / PD 53 / EA 159 / ED 62 / Vel 110
- Evolui de: Sunflowmon (#24, Lv 20); Togemon (#17, Lv 20)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Rosemon (#75) | Mega | 28 | 50 | 426 | - | - | 84 | - | 97 | - | 1.7 | 685 | canon | normal |
| Cendrillmon (#94) | Mega | 28 | 8 | 426 | - | - | 207 | - | 97 | - | 1.7 | 766 | design | normal |

#### MegaKabuterimon (#51) - Ultimate | Role Tank | Special | Vacine / Electric | Ovo: Forest

- Stats base: HP 585 / PA 35 / PD 122 / EA 35 / ED 122 / Vel 44
- Evolui de: Kabuterimon (#15, Lv 20); Stingmon (#198, Lv 20)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| HerculesKabuterimon (#74) | Mega | 28 | 50 | 473 | - | 129 | - | 71 | - | - | 1.7 | 751 | canon | normal |
| Craniummon (#159) | Mega | 28 | 8 | 473 | - | 186 | - | 71 | - | - | 1.7 | 766 | design | normal |

#### Garudamon (#61) - Ultimate | Role Ranged | Special | Vacine / Wind | Ovo: Beast

- Stats base: HP 340 / PA 35 / PD 53 / EA 159 / ED 62 / Vel 132
- Evolui de: Birdramon (#60, Lv 20); Cockatrimon (#97, Lv 20)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Hououmon (#62) | Mega | 28 | 50 | 390 | - | - | 76 | - | 132 | - | 1.7 | 676 | canon | normal |
| Zephagamon (#81) | Mega | 28 | 8 | 390 | - | - | 251 | - | 132 | - | 1.7 | 809 | design | normal |

#### Zudomon (#67) - Ultimate | Role Tank | Physical | Data / Water | Ovo: Water

- Stats base: HP 500 / PA 150 / PD 135 / EA 25 / ED 80 / Vel 50
- Evolui de: Ikkakumon (#66, Lv 20)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Vikemon (#68) | Mega | 28 | 8 | 800 | 240 | 216 | - | - | - | - | 1.7 | 1292 | canon | normal |
| Leviamon (#88) | Mega | 28 | 8 | 768 | 267 | 228 | - | 129 | - | - | 1.7 | 1428 | design | normal |

#### Angewomon (#72) - Ultimate | Role Ranged | Special | Vacine / Light | Ovo: Light

- Stats base: HP 320 / PA 30 / PD 50 / EA 165 / ED 60 / Vel 140
- Evolui de: Angemon (#16, Lv 20)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Magnadramon (#73) | Mega | 28 | 50 | 387 | - | - | 112 | - | 155 | - | 1.7 | 732 | canon | normal |
| Lilithmon (#95) | Mega | 28 | 8 | 387 | - | - | 112 | 108 | 155 | - | 1.7 | 798 | design | normal |
| Ophanimon (#178) | Mega | 28 | 8 | 350 | - | - | 213 | - | 155 | Felic≥40 | 1.7 | 794 | canon | normal |

#### Grand Galemon (#80) - Ultimate | Role Ranged | Special | Data / Wind | Ovo: Dragon

- Stats base: HP 360 / PA 40 / PD 60 / EA 175 / ED 70 / Vel 128
- Evolui de: Galemon (#79, Lv 20)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Zephagamon (#81) | Mega | 28 | 8 | 380 | 53 | - | 230 | - | 137 | - | 1.7 | 836 | design | normal |
| Dynasmon (#160) | Mega | 28 | 50 | 380 | - | - | 113 | - | 137 | - | 1.7 | 708 | design | normal |

#### Lamiamon (#86) - Ultimate | Role Assassin | Special | Virus / Dark | Ovo: Dragon

- Stats base: HP 350 / PA 56 / PD 62 / EA 168 / ED 66 / Vel 118
- Evolui de: Devidramon (#47, Lv 20); Dimetromon (#85, Lv 20)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Medusamon (#87) | Mega | 28 | 50 | 370 | - | - | 73 | - | 114 | - | 1.7 | 635 | canon | normal |
| Leviamon (#88) | Mega | 28 | 8 | 510 | 120 | - | - | - | 140 | Disc≤30 | 1.7 | 876 | design | normal |

#### Chaperomon (#93) - Ultimate | Role Support | Special | Virus / Dark | Ovo: Dark | Suporte: Buffer

- Stats base: HP 380 / PA 38 / PD 79 / EA 148 / ED 89 / Vel 70
- Evolui de: ShoeShoemon (#92, Lv 20)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Pinochimon (#82) | Mega | 28 | 8 | 586 | - | 118 | 264 | 134 | - | - | 1.7 | 1138 | design | normal |
| Cendrillmon (#94) | Mega | 28 | 8 | 570 | - | - | 222 | - | 140 | Felic≥40 | 1.7 | 1008 | design | normal |

#### Delumon (#98) - Ultimate | Role Warrior | Physical | Data / Wind | Ovo: Beast

- Stats base: HP 400 / PA 175 / PD 70 / EA 40 / ED 65 / Vel 95
- Evolui de: Birdramon (#60, Lv 20); Cockatrimon (#97, Lv 20)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Hououmon (#62) | Mega | 28 | 50 | 400 | 144 | - | - | - | 116 | - | 1.7 | 738 | design | normal |
| AncientSphinxmon (#112) | Mega | 28 | 8 | 400 | 239 | - | - | - | 116 | - | 1.7 | 791 | design | normal |

#### Fantomon (#104) - Ultimate | Role Assassin | Special | Virus / Dark | Ovo: Dark

- Stats base: HP 345 / PA 42 / PD 56 / EA 165 / ED 62 / Vel 120
- Evolui de: Bakemon (#103, Lv 20); Wizardmon (#21, Lv 20)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Boltmon (#113) | Mega | 28 | 50 | 373 | - | - | 126 | - | 124 | - | 1.7 | 701 | design | normal |
| Ragnamon (#119) | Mega | 28 | 8 | 373 | - | - | 218 | - | 124 | - | 1.7 | 751 | design | normal |
| Piedmon (#194) | Mega | 28 | 8 | 373 | - | - | 126 | - | 189 | Disc≥60 | 1.7 | 784 | design | normal |

#### Triceramon (#111) - Ultimate | Role Tank | Physical | Virus / Earth | Ovo: Beast

- Stats base: HP 440 / PA 150 / PD 145 / EA 35 / ED 98 / Vel 58
- Evolui de: Dimetromon (#85, Lv 20); Ikkakumon (#66, Lv 20); Tortamon (#110, Lv 20)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| AncientSphinxmon (#112) | Mega | 28 | 8 | 660 | 225 | - | - | - | 87 | - | 1.7 | 1008 | design | normal |
| Dinomon (#120) | Mega | 28 | 8 | 678 | 267 | 226 | - | 146 | - | - | 1.7 | 1353 | design | normal |
| GrandisKuwagamon (#200) | Mega | 28 | 8 | 678 | 267 | 229 | - | 100 | 120 | Disc≤40 | 1.7 | 1490 | canon | normal |

#### Destromon (#118) - Ultimate | Role Ranged | Special | Virus / Dark | Ovo: Dark

- Stats base: HP 355 / PA 48 / PD 62 / EA 160 / ED 68 / Vel 115
- Evolui de: IceDevimon (#46, Lv 20); Snatchmon (#117, Lv 20)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Demon (#105) | Mega | 28 | 8 | 548 | - | - | 285 | - | 185 | Disc≤35 | 1.7 | 1119 | design | normal |
| Boltmon (#113) | Mega | 28 | 50 | 375 | - | - | 74 | - | 110 | - | 1.7 | 637 | design | normal |
| Belphemon Sleep Mode (#128) | Mega | 36 | 13 | 413 | - | 89 | - | 136 | 90 | - | 1.8 | 777 | design | normal |

#### Lucemon Falldown Mode (#125) - Ultimate | Role Assassin | Special | Virus / Dark | Ovo: Dark

- Stats base: HP 380 / PA 90 / PD 70 / EA 175 / ED 85 / Vel 100
- Evolui de: Angemon (#16, Lv 20); Devimon (#45, Lv 20)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Demon (#105) | Mega | 28 | 50 | 387 | 130 | - | 89 | - | 150 | - | 1.7 | 834 | design | normal |
| Lucemon Satan Mode (#121) | Mega | 36 | 12 | 425 | - | 96 | 243 | - | 157 | - | 1.8 | 969 | canon | normal |

#### Astamon (#127) - Ultimate | Role Ranged | Physical | Virus / Dark | Ovo: Dark

- Stats base: HP 360 / PA 140 / PD 60 / EA 60 / ED 55 / Vel 110
- Evolui de: IceDevimon (#46, Lv 20); Snatchmon (#117, Lv 20)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Beelzebumon (#106) | Mega | 28 | 8 | 555 | 212 | - | - | - | 175 | Disc≤40 | 1.7 | 1038 | design | normal |
| Ragnamon (#119) | Mega | 28 | 50 | 380 | 129 | - | - | - | 176 | - | 1.7 | 763 | design | normal |

#### Chirinmon (#131) - Ultimate | Role Ranged | Special | Vacine / Light | Ovo: Beast

- Stats base: HP 360 / PA 70 / PD 85 / EA 165 / ED 100 / Vel 115
- Evolui de: Reppamon (#130, Lv 28)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Kentaurusmon (#144) | Mega | 28 | 50 | 368 | - | - | 198 | - | 117 | - | 1.7 | 761 | design | normal |
| Sleipmon (#177) | Mega | 36 | 12 | 406 | 265 | 95 | - | - | 150 | Felic≥40 | 1.8 | 1004 | canon | normal |

#### Groundramon (#134) - Ultimate | Role Tank | Physical | Data / Earth | Ovo: Dragon

- Stats base: HP 400 / PA 150 / PD 140 / EA 40 / ED 80 / Vel 50
- Evolui de: Coredramon (Green) (#133, Lv 20); Tortamon (#110, Lv 28)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Breakdramon (#135) | Mega | 28 | 50 | 405 | 177 | - | - | - | 76 | - | 1.7 | 736 | canon | normal |
| Dorugoramon (#155) | Mega | 36 | 12 | 443 | 192 | 210 | - | 56 | - | - | 1.8 | 949 | design | normal |

#### Wingdramon (#137) - Ultimate | Role Ranged | Physical | Data / Wind | Ovo: Dragon

- Stats base: HP 360 / PA 130 / PD 85 / EA 80 / ED 85 / Vel 140
- Evolui de: Coredramon (Blue) (#136, Lv 20); Galemon (#79, Lv 28); Veedramon (#140, Lv 28)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Dynasmon (#160) | Mega | 28 | 50 | 436 | 134 | 117 | - | - | 160 | - | 1.7 | 925 | design | normal |
| SlayerDramon (#138) | Mega | 36 | 12 | 474 | 235 | 105 | - | - | 180 | - | 1.8 | 1042 | canon | normal |

#### AeroVeedramon (#141) - Ultimate | Role Ranged | Physical | Vacine / Wind | Ovo: Dragon

- Stats base: HP 370 / PA 150 / PD 80 / EA 55 / ED 75 / Vel 135
- Evolui de: Coredramon (Blue) (#136, Lv 28); ExVeemon (#191, Lv 20); Stingmon (#198, Lv 20); Veedramon (#140, Lv 20)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Magnamon (#143) | Mega | 28 | 50 | 393 | 170 | 91 | - | - | 144 | - | 1.7 | 876 | design | normal |
| UlforceVeedramon (#142) | Mega | 36 | 12 | 431 | 268 | 106 | - | - | 152 | - | 1.8 | 1005 | canon | normal |

#### Knightmon (#147) - Ultimate | Role Tank | Physical | Data / Neutral | Ovo: Beast

- Stats base: HP 380 / PA 160 / PD 120 / EA 45 / ED 80 / Vel 85
- Evolui de: BaoHackmon (#164, Lv 28); Gladimon (#146, Lv 20); Kabuterimon (#15, Lv 28)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Craniummon (#159) | Mega | 28 | 50 | 471 | 137 | 150 | - | - | 89 | - | 1.7 | 925 | design | normal |
| Crusadermon (#148) | Mega | 36 | 12 | 509 | 152 | 150 | - | - | 147 | - | 1.8 | 1006 | design | normal |

#### Dorugreymon (#151) - Ultimate | Role Warrior | Physical | Data / Fire | Ovo: Dragon

- Stats base: HP 390 / PA 165 / PD 95 / EA 55 / ED 70 / Vel 100
- Evolui de: DexDorugamon (#156, Lv 28); Dorugamon (#150, Lv 20); Raptordramon (#153, Lv 28)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Dorugoramon (#155) | Mega | 28 | 50 | 400 | 166 | 105 | - | - | 106 | - | 1.7 | 855 | canon | normal |
| Alphamon (#152) | Mega | 36 | 12 | 438 | 265 | 120 | - | - | 152 | - | 1.8 | 1023 | canon | normal |

#### Grademon (#154) - Ultimate | Role Tank | Physical | Virus / Earth | Ovo: Dragon

- Stats base: HP 410 / PA 175 / PD 120 / EA 35 / ED 70 / Vel 75
- Evolui de: Coredramon (Green) (#133, Lv 28); Gladimon (#146, Lv 28); Raptordramon (#153, Lv 20)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Dynasmon (#160) | Mega | 28 | 50 | 446 | 262 | 157 | - | - | - | - | 1.7 | 943 | design | normal |
| Alphamon (#152) | Mega | 36 | 12 | 484 | 262 | 160 | - | - | 192 | - | 1.8 | 1146 | design | normal |

#### DexDorugreymon (#157) - Ultimate | Role Assassin | Physical | Virus / Dark | Ovo: Dragon

- Stats base: HP 365 / PA 155 / PD 85 / EA 70 / ED 70 / Vel 120
- Evolui de: DexDorugamon (#156, Lv 20); Dorugamon (#150, Lv 28)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| DexDorugoramon (#158) | Mega | 28 | 50 | 445 | 182 | - | - | - | 135 | - | 1.7 | 840 | canon | normal |
| Dorugoramon (#155) | Mega | 36 | 12 | 483 | 284 | 111 | - | - | 128 | - | 1.8 | 1054 | design | normal |

#### SaviorHackmon (#165) - Ultimate | Role Assassin | Physical | Virus / Light | Ovo: Dark

- Stats base: HP 375 / PA 145 / PD 90 / EA 115 / ED 85 / Vel 130
- Evolui de: BaoHackmon (#164, Lv 20)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Gankoomon (#162) | Mega | 28 | 50 | 420 | 150 | 130 | - | - | 128 | - | 1.7 | 906 | design | normal |
| Jesmon (#166) | Mega | 36 | 12 | 458 | 165 | 130 | - | - | 185 | - | 1.8 | 986 | canon | normal |

#### RizeGreymon (#167) - Ultimate | Role Warrior | Physical | Vacine / Fire | Ovo: Dragon

- Stats base: HP 460 / PA 168 / PD 101 / EA 34 / ED 66 / Vel 51
- Evolui de: Geogreymon (#44, Lv 20); Greymon (#13, Lv 20)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| WarGreymon (#52) | Mega | 28 | 50 | 451 | 180 | 111 | - | - | - | - | 1.7 | 820 | design | normal |
| ShineGreymon (#168) | Mega | 28 | 8 | 451 | 256 | - | - | - | 74 | - | 1.7 | 817 | canon | normal |

#### Mistymon (#170) - Ultimate | Role Assassin | Physical | Data / Dark | Ovo: Fire

- Stats base: HP 340 / PA 145 / PD 80 / EA 40 / ED 70 / Vel 170
- Evolui de: Kyubimon (#20, Lv 20); Wizardmon (#21, Lv 20)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Barbamon (#122) | Mega | 28 | 50 | 362 | 140 | - | - | - | 182 | - | 1.7 | 762 | design | normal |
| Piedmon (#194) | Mega | 28 | 8 | 577 | 190 | - | - | - | 196 | Disc≤40 | 1.7 | 1059 | design | normal |

#### Lilamon (#179) - Ultimate | Role Support | Special | Data / Plant | Ovo: Forest | Suporte: Healer

- Stats base: HP 360 / PA 30 / PD 70 / EA 150 / ED 85 / Vel 100
- Evolui de: Sunflowmon (#24, Lv 20); Togemon (#17, Lv 20)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Rosemon (#75) | Mega | 28 | 8 | 426 | - | - | 198 | - | 94 | - | 1.7 | 754 | canon | normal |
| Cendrillmon (#94) | Mega | 28 | 50 | 426 | - | - | 80 | 94 | 94 | - | 1.7 | 772 | design | normal |

#### LadyDevimon (#180) - Ultimate | Role Assassin | Special | Virus / Dark | Ovo: Dark

- Stats base: HP 340 / PA 45 / PD 55 / EA 165 / ED 65 / Vel 128
- Evolui de: Devimon (#45, Lv 20); ShoeShoemon (#92, Lv 20)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Medusamon (#87) | Mega | 28 | 50 | 380 | - | - | 85 | - | 106 | - | 1.7 | 649 | design | normal |
| Lilithmon (#95) | Mega | 28 | 8 | 530 | - | - | 250 | - | 190 | Disc≤30; Felic≤40 | 1.7 | 1136 | canon | normal |

#### SkullSatamon (#181) - Ultimate | Role Assassin | Physical | Virus / Dark | Ovo: Dark

- Stats base: HP 355 / PA 165 / PD 62 / EA 55 / ED 60 / Vel 118
- Evolui de: Devimon (#45, Lv 20)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Demon (#105) | Mega | 28 | 8 | 601 | 200 | - | - | - | 135 | Felic≤40 | 1.7 | 1032 | design | normal |
| Piedmon (#194) | Mega | 28 | 8 | 602 | 216 | - | - | - | 136 | Disc≤40 | 1.7 | 1050 | design | normal |

#### Myotismon (#182) - Ultimate | Role Assassin | Special | Virus / Dark | Ovo: Dark

- Stats base: HP 345 / PA 60 / PD 58 / EA 160 / ED 72 / Vel 125
- Evolui de: Bakemon (#103, Lv 20); Devimon (#45, Lv 20)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Beelzebumon (#106) | Mega | 28 | 8 | 584 | - | - | 239 | - | 141 | Disc≤40 | 1.7 | 1060 | design | normal |
| Piedmon (#194) | Mega | 28 | 50 | 373 | - | - | 152 | - | 124 | - | 1.7 | 727 | design | normal |

#### Paildramon (#192) - Ultimate | Role Ranged | Physical | Vacine / Fire | Ovo: Dragon

- Stats base: HP 375 / PA 155 / PD 82 / EA 60 / ED 75 / Vel 140
- Evolui de: Jogress: ExVeemon + Stingmon

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Imperialdramon (#193) | Mega | 28 | 50 | 484 | 198 | - | - | - | 152 | - | 1.7 | 912 | canon | normal |
| UlforceVeedramon (#142) | Mega | 36 | 12 | 522 | 293 | 106 | - | - | 164 | - | 1.8 | 1133 | design | normal |

#### JewelBeemon (#199) - Ultimate | Role Assassin | Physical | Vacine / Wind | Ovo: Forest

- Stats base: HP 340 / PA 160 / PD 62 / EA 60 / ED 62 / Vel 128
- Evolui de: Kabuterimon (#15, Lv 20); Stingmon (#198, Lv 20); Sunflowmon (#24, Lv 20)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| HerculesKabuterimon (#74) | Mega | 28 | 50 | 393 | 132 | - | - | - | 104 | - | 1.7 | 707 | canon | normal |
| Imperialdramon (#193) | Mega | 28 | 8 | 393 | 132 | - | - | 99 | 104 | Felic≥40 | 1.7 | 804 | canon | normal |
| GrandisKuwagamon (#200) | Mega | 28 | 8 | 393 | 224 | - | - | - | 104 | - | 1.7 | 757 | canon | normal |

### Estágio Mega (49 Digimons)

#### WarGreymon (#52) - Mega | Role Warrior | Physical | Vacine / Fire | Ovo: Dragon

- Stats base: HP 745 / PA 220 / PD 140 / EA 220 / ED 110 / Vel 94
- Evolui de: MetalGreymon (#39, Lv 28); RizeGreymon (#167, Lv 28)
- Evolui para: (nenhum)

#### MetalGarurumon (#53) - Mega | Role Warrior | Physical | Vacine / Fire | Ovo: Beast

- Stats base: HP 745 / PA 219 / PD 140 / EA 219 / ED 110 / Vel 94
- Evolui de: WereGarurumon (#40, Lv 28)
- Evolui para: (nenhum)

#### Gallantmon (#54) - Mega | Role Warrior | Physical | Virus / Fire | Ovo: Dragon

- Stats base: HP 818 / PA 298 / PD 164 / EA 60 / ED 105 / Vel 119
- Evolui de: WarGrowlmon (#41, Lv 28)
- Evolui para: (nenhum)

#### MegaGargomon (#55) - Mega | Role Tank | Physical | Vacine / Wind | Ovo: Beast

- Stats base: HP 640 / PA 260 / PD 140 / EA 40 / ED 100 / Vel 90
- Evolui de: Rapidmon (#42, Lv 28)
- Evolui para: (nenhum)

#### Seraphimon (#56) - Mega | Role Ranged | Special | Vacine / Light | Ovo: Light

- Stats base: HP 600 / PA 60 / PD 100 / EA 280 / ED 115 / Vel 130
- Evolui de: MagnaAngemon (#43, Lv 28)
- Evolui para: (nenhum)

#### Hououmon (#62) - Mega | Role Ranged | Special | Vacine / Light | Ovo: Beast

- Stats base: HP 600 / PA 60 / PD 100 / EA 280 / ED 115 / Vel 130
- Evolui de: Delumon (#98, Lv 28); Garudamon (#61, Lv 28)
- Evolui para: (nenhum)

#### Vikemon (#68) - Mega | Role Tank | Physical | Data / Water | Ovo: Water

- Stats base: HP 850 / PA 255 / PD 230 / EA 40 / ED 135 / Vel 85
- Evolui de: Zudomon (#67, Lv 28)
- Evolui para: (nenhum)

#### Magnadramon (#73) - Mega | Role Ranged | Special | Vacine / Light | Ovo: Light

- Stats base: HP 600 / PA 55 / PD 95 / EA 290 / ED 110 / Vel 150
- Evolui de: Angewomon (#72, Lv 28); MagnaAngemon (#43, Lv 28)
- Evolui para: (nenhum)

#### HerculesKabuterimon (#74) - Mega | Role Tank | Special | Vacine / Electric | Ovo: Forest

- Stats base: HP 995 / PA 60 / PD 207 / EA 60 / ED 207 / Vel 75
- Evolui de: JewelBeemon (#199, Lv 28); MegaKabuterimon (#51, Lv 28)
- Evolui para: (nenhum)

#### Rosemon (#75) - Mega | Role Ranged | Special | Data / Plant | Ovo: Forest

- Stats base: HP 580 / PA 60 / PD 90 / EA 270 / ED 105 / Vel 185
- Evolui de: Lilamon (#179, Lv 28); Lillymon (#50, Lv 28)
- Evolui para: (nenhum)

#### Zephagamon (#81) - Mega | Role Ranged | Special | Data / Wind | Ovo: Dragon

- Stats base: HP 640 / PA 150 / PD 120 / EA 230 / ED 118 / Vel 150
- Evolui de: Garudamon (#61, Lv 28); Grand Galemon (#80, Lv 28)
- Evolui para: (nenhum)

#### Pinochimon (#82) - Mega | Role Ranged | Special | Data / Wind | Ovo: Dragon

- Stats base: HP 610 / PA 90 / PD 100 / EA 265 / ED 110 / Vel 190
- Evolui de: Chaperomon (#93, Lv 28)
- Evolui para: (nenhum)

#### Medusamon (#87) - Mega | Role Assassin | Special | Virus / Dark | Ovo: Dragon

- Stats base: HP 600 / PA 70 / PD 96 / EA 255 / ED 100 / Vel 145
- Evolui de: LadyDevimon (#180, Lv 28); Lamiamon (#86, Lv 28)
- Evolui para: (nenhum)

#### Leviamon (#88) - Mega | Role Tank | Physical | Data / Dark | Ovo: Dragon

- Stats base: HP 700 / PA 190 / PD 152 / EA 90 / ED 122 / Vel 95
- Evolui de: Lamiamon (#86, Lv 28); Zudomon (#67, Lv 28)
- Evolui para: (nenhum)

#### Cendrillmon (#94) - Mega | Role Support | Special | Virus / Dark | Ovo: Dark | Suporte: Buffer

- Stats base: HP 590 / PA 55 / PD 95 / EA 220 / ED 115 / Vel 165
- Evolui de: Chaperomon (#93, Lv 28); Lilamon (#179, Lv 28); Lillymon (#50, Lv 28)
- Evolui para: (nenhum)

#### Lilithmon (#95) - Mega | Role Support | Special | Virus / Dark | Ovo: Dark | Suporte: Healer

- Stats base: HP 560 / PA 48 / PD 110 / EA 205 / ED 135 / Vel 120
- Evolui de: Angewomon (#72, Lv 28); LadyDevimon (#180, Lv 28)
- Evolui para: (nenhum)

#### Medieval Dukemon (#99) - Mega | Role Warrior | Physical | Data / Wind | Ovo: Beast

- Stats base: HP 780 / PA 270 / PD 150 / EA 50 / ED 120 / Vel 110
- Evolui de: WarGrowlmon (#41, Lv 28)
- Evolui para: (nenhum)

#### Demon (#105) - Mega | Role Assassin | Special | Data / Dark | Ovo: Dark

- Stats base: HP 620 / PA 65 / PD 98 / EA 240 / ED 105 / Vel 150
- Evolui de: Destromon (#118, Lv 28); Lucemon Falldown Mode (#125, Lv 28); SkullSatamon (#181, Lv 28)
- Evolui para: (nenhum)

#### Beelzebumon (#106) - Mega | Role Assassin | Special | Virus / Dark | Ovo: Dark

- Stats base: HP 600 / PA 100 / PD 92 / EA 235 / ED 98 / Vel 140
- Evolui de: Astamon (#127, Lv 28); Myotismon (#182, Lv 28)
- Evolui para: (nenhum)

#### AncientSphinxmon (#112) - Mega | Role Tank | Physical | Virus / Earth | Ovo: Beast

- Stats base: HP 760 / PA 230 / PD 220 / EA 55 / ED 160 / Vel 75
- Evolui de: Delumon (#98, Lv 28); Triceramon (#111, Lv 28)
- Evolui para: (nenhum)

#### Boltmon (#113) - Mega | Role Tank | Physical | Virus / Earth | Ovo: Beast

- Stats base: HP 720 / PA 260 / PD 190 / EA 70 / ED 150 / Vel 100
- Evolui de: Destromon (#118, Lv 28); Fantomon (#104, Lv 28)
- Evolui para: (nenhum)

#### Ragnamon (#119) - Mega | Role Ranged | Special | Virus / Dark | Ovo: Dark

- Stats base: HP 630 / PA 80 / PD 100 / EA 245 / ED 108 / Vel 148
- Evolui de: Astamon (#127, Lv 28); Fantomon (#104, Lv 28)
- Evolui para: (nenhum)

#### Dinomon (#120) - Mega | Role Warrior | Physical | Vacine / Fire | Ovo: Dragon

- Stats base: HP 720 / PA 250 / PD 130 / EA 80 / ED 95 / Vel 100
- Evolui de: Triceramon (#111, Lv 28)
- Evolui para: (nenhum)

#### Lucemon Satan Mode (#121) - Mega | Role Assassin | Special | Unknown / Dark | Ovo: Dark

- Stats base: HP 650 / PA 70 / PD 100 / EA 250 / ED 130 / Vel 155
- Evolui de: Lucemon Falldown Mode (#125, Lv 36)
- Evolui para: (nenhum)

#### Barbamon (#122) - Mega | Role Ranged | Special | Virus / Dark | Ovo: Dark

- Stats base: HP 640 / PA 55 / PD 130 / EA 220 / ED 150 / Vel 90
- Evolui de: Mistymon (#170, Lv 28)
- Evolui para: (nenhum)

#### Belphemon Rage Mode (#123) - Mega | Role Warrior | Physical | Virus / Dark | Ovo: Dark

- Stats base: HP 750 / PA 210 / PD 140 / EA 60 / ED 90 / Vel 75
- Evolui de: Belphemon Sleep Mode (#128, Lv 45)
- Evolui para: (nenhum)

#### Belphemon Sleep Mode (#128) - Mega | Role Tank | Physical | Data / Dark | Ovo: Dark

- Stats base: HP 850 / PA 130 / PD 190 / EA 40 / ED 160 / Vel 50
- Evolui de: Destromon (#118, Lv 36)

| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Belphemon Rage Mode (#123) | Mega | 45 | 14 | 752 | 135 | 180 | - | 141 | - | Batalhas≥15; Disc≤40; Felic≤40 | 1.1 | 1402 | canon | mode |

#### Breakdramon (#135) - Mega | Role Warrior | Physical | Virus / Earth | Ovo: Dragon

- Stats base: HP 700 / PA 230 / PD 170 / EA 60 / ED 110 / Vel 90
- Evolui de: Groundramon (#134, Lv 28)
- Evolui para: (nenhum)

#### SlayerDramon (#138) - Mega | Role Assassin | Physical | Virus / Wind | Ovo: Dragon

- Stats base: HP 620 / PA 220 / PD 110 / EA 90 / ED 100 / Vel 165
- Evolui de: Wingdramon (#137, Lv 36)
- Evolui para: (nenhum)

#### UlforceVeedramon (#142) - Mega | Role Assassin | Physical | Free / Light | Ovo: Dragon

- Stats base: HP 680 / PA 245 / PD 120 / EA 80 / ED 115 / Vel 175
- Evolui de: AeroVeedramon (#141, Lv 36); Paildramon (#192, Lv 36)
- Evolui para: (nenhum)

#### Magnamon (#143) - Mega | Role Warrior | Physical | Vacine / Light | Ovo: Dragon

- Stats base: HP 720 / PA 235 / PD 175 / EA 70 / ED 150 / Vel 100
- Evolui de: AeroVeedramon (#141, Lv 28); Rapidmon (#42, Lv 36)
- Evolui para: (nenhum)

#### Kentaurusmon (#144) - Mega | Role Ranged | Special | Vacine / Light | Ovo: Beast

- Stats base: HP 720 / PA 120 / PD 150 / EA 220 / ED 170 / Vel 130
- Evolui de: Chirinmon (#131, Lv 28)
- Evolui para: (nenhum)

#### Crusadermon (#148) - Mega | Role Warrior | Physical | Virus / Neutral | Ovo: Beast

- Stats base: HP 660 / PA 225 / PD 160 / EA 60 / ED 110 / Vel 120
- Evolui de: Knightmon (#147, Lv 36)
- Evolui para: (nenhum)

#### Alphamon (#152) - Mega | Role Assassin | Physical | Unknown / Light | Ovo: Dragon

- Stats base: HP 700 / PA 240 / PD 140 / EA 90 / ED 130 / Vel 160
- Evolui de: Dorugreymon (#151, Lv 36); Grademon (#154, Lv 36)
- Evolui para: (nenhum)

#### Dorugoramon (#155) - Mega | Role Tank | Physical | Virus / Earth | Ovo: Dragon

- Stats base: HP 750 / PA 245 / PD 175 / EA 50 / ED 100 / Vel 95
- Evolui de: DexDorugreymon (#157, Lv 36); Dorugreymon (#151, Lv 28); Groundramon (#134, Lv 36)
- Evolui para: (nenhum)

#### DexDorugoramon (#158) - Mega | Role Assassin | Physical | Virus / Dark | Ovo: Dragon

- Stats base: HP 640 / PA 215 / PD 125 / EA 105 / ED 95 / Vel 150
- Evolui de: DexDorugreymon (#157, Lv 28)
- Evolui para: (nenhum)

#### Craniummon (#159) - Mega | Role Tank | Physical | Vacine / Neutral | Ovo: Beast

- Stats base: HP 800 / PA 200 / PD 210 / EA 50 / ED 150 / Vel 70
- Evolui de: Knightmon (#147, Lv 28); MegaKabuterimon (#51, Lv 28)
- Evolui para: (nenhum)

#### Dynasmon (#160) - Mega | Role Warrior | Physical | Vacine / Wind | Ovo: Dragon

- Stats base: HP 670 / PA 220 / PD 150 / EA 70 / ED 110 / Vel 140
- Evolui de: Grademon (#154, Lv 28); Grand Galemon (#80, Lv 28); Wingdramon (#137, Lv 28)
- Evolui para: (nenhum)

#### Duftmon (#161) - Mega | Role Assassin | Physical | Data / Neutral | Ovo: Beast

- Stats base: HP 630 / PA 215 / PD 130 / EA 95 / ED 110 / Vel 155
- Evolui de: Taomon (#49, Lv 28)
- Evolui para: (nenhum)

#### Gankoomon (#162) - Mega | Role Warrior | Physical | Vacine / Fire | Ovo: Beast

- Stats base: HP 710 / PA 250 / PD 145 / EA 55 / ED 100 / Vel 105
- Evolui de: MachGaogamon (#48, Lv 28); SaviorHackmon (#165, Lv 28); WereGarurumon (#40, Lv 28)
- Evolui para: (nenhum)

#### Jesmon (#166) - Mega | Role Assassin | Physical | Unknown / Light | Ovo: Dark

- Stats base: HP 710 / PA 250 / PD 135 / EA 160 / ED 125 / Vel 180
- Evolui de: SaviorHackmon (#165, Lv 36)
- Evolui para: (nenhum)

#### ShineGreymon (#168) - Mega | Role Warrior | Physical | Vacine / Fire | Ovo: Dragon

- Stats base: HP 819 / PA 299 / PD 180 / EA 61 / ED 117 / Vel 91
- Evolui de: MetalGreymon (#39, Lv 28); RizeGreymon (#167, Lv 28)
- Evolui para: (nenhum)

#### Sakuyamon (#175) - Mega | Role Ranged | Special | Data / Light | Ovo: Beast

- Stats base: HP 600 / PA 55 / PD 100 / EA 285 / ED 125 / Vel 160
- Evolui de: Taomon (#49, Lv 28)
- Evolui para: (nenhum)

#### MirageGaogamon (#176) - Mega | Role Warrior | Physical | Data / Wind | Ovo: Beast

- Stats base: HP 700 / PA 245 / PD 135 / EA 60 / ED 105 / Vel 165
- Evolui de: MachGaogamon (#48, Lv 28)
- Evolui para: (nenhum)

#### Sleipmon (#177) - Mega | Role Warrior | Physical | Vacine / Light | Ovo: Beast

- Stats base: HP 700 / PA 230 / PD 140 / EA 100 / ED 130 / Vel 165
- Evolui de: Chirinmon (#131, Lv 36)
- Evolui para: (nenhum)

#### Ophanimon (#178) - Mega | Role Ranged | Special | Vacine / Light | Ovo: Light

- Stats base: HP 590 / PA 50 / PD 100 / EA 285 / ED 140 / Vel 145
- Evolui de: Angewomon (#72, Lv 28)
- Evolui para: (nenhum)

#### Imperialdramon (#193) - Mega | Role Warrior | Physical | Free / Fire | Ovo: Dragon

- Stats base: HP 700 / PA 245 / PD 145 / EA 150 / ED 125 / Vel 150
- Evolui de: JewelBeemon (#199, Lv 28); Paildramon (#192, Lv 28)
- Evolui para: (nenhum)

#### Piedmon (#194) - Mega | Role Assassin | Special | Virus / Dark | Ovo: Dark

- Stats base: HP 640 / PA 130 / PD 100 / EA 250 / ED 125 / Vel 165
- Evolui de: Fantomon (#104, Lv 28); Mistymon (#170, Lv 28); Myotismon (#182, Lv 28); SkullSatamon (#181, Lv 28)
- Evolui para: (nenhum)

#### GrandisKuwagamon (#200) - Mega | Role Warrior | Physical | Virus / Dark | Ovo: Forest

- Stats base: HP 690 / PA 250 / PD 140 / EA 60 / ED 105 / Vel 140
- Evolui de: JewelBeemon (#199, Lv 28); Triceramon (#111, Lv 28)
- Evolui para: (nenhum)

### Estágio MegaPlus (2 Digimons)

#### Omegamon (#171) - MegaPlus | Role Warrior | Physical | Vacine / Light | Ovo: Light

- Stats base: HP 900 / PA 260 / PD 170 / EA 260 / ED 140 / Vel 110
- Evolui de: Jogress: WarGreymon + MetalGarurumon
- Evolui para: (nenhum)

#### Examon (#174) - MegaPlus | Role Warrior | Physical | Virus / Fire | Ovo: Dragon

- Stats base: HP 860 / PA 290 / PD 180 / EA 70 / ED 130 / Vel 160
- Evolui de: Jogress: SlayerDramon + Breakdramon
- Evolui para: (nenhum)

