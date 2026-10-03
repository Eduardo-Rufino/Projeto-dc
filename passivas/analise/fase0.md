# Passivas herdadas — Fase 0 (exploração somente leitura)

Data: 2026-10-02 · Branch: `passivas-herdadas`

## Branch-base

A `calendario-campeonatos` **não** está mergeada na `development` (8 commits à frente). Pela regra
do prompt a base seria ela, mas a `hud-calendario` já estava à frente dela (commit do HUD
`f0d7699`) e tinha trabalho não commitado. Com aprovação do desenvolvedor:

1. As pendências foram commitadas na `hud-calendario`: `f46f8f4` (sprites de 21 Digimon) e
   `d338c37` (tutoriais automáticos, ovos arrastáveis, menu de comida que fica aberto).
2. A `passivas-herdadas` foi criada a partir da `hud-calendario`, que contém toda a
   `calendario-campeonatos` mais o HUD.

Nenhum código ou dado foi alterado nesta fase.

---

## 1. Passivas hoje

**Definição**
- Enum `PassiveType` com as 31 passivas, agrupadas por Role: `Enums/PassiveType.cs:11-53`.
- Metadados (nome, descrição, Role, SupportTypes) em `PassiveData`: `Scripts/Data/PassiveData.cs:13-30`.
  São 31 JSONs em `Data/Passives/` (ex.: `Data/Passives/passive_couraca.json`), carregados por
  `DatabaseManager.LoadPassives` (`Scripts/Managers/DatabaseManager.cs:801`) e lidos com
  `GetPassive` (`DatabaseManager.cs:869`).
- Os **números** de cada efeito não ficam no JSON. Eles são `const` em C#, perto de cada hook
  (`Enums/PassiveType.cs:4-9`).
- Especificação: `PASSIVAS_SPEC.md` na raiz.

**Pool por espécie e sorteio**
- O pool fica em `DigimonData.Passives`: `Scripts/Data/DigimonData.cs:81`, uma lista de 2-3 ids.
- O sorteio fica em `PassiveSystem.RollRandomPassive`
  (`Scripts/Systems/Passives/PassiveSystem.cs:29-49`): filtra por Role e SupportType
  (`IsCompatible`, `PassiveSystem.cs:54-72`) e devolve **uma** passiva.

**O que o indivíduo carrega**
- Um campo só, `DigimonInstance.PassiveId` (`PassiveType?`): `Scripts/Gameplay/DigimonInstance.cs:28`.
- Ele é sorteado em quatro momentos:
  - ao nascer: `Scripts/Systems/Eggs/EggSystem.cs:190`;
  - ao evoluir, **re-sorteado do zero**: `DigimonInstance.cs:472`;
  - no Jogress: `Scripts/Systems/Jogress/JogressSystem.cs:62`;
  - retroativamente, em saves antigos: `GameManager.ApplyRetroactivePassives`, `Scripts/Managers/GameManager.cs:350`.

**Aplicação em batalha**
- Não há um ponto central. São **42 checagens de igualdade** `Digimon.PassiveId == PassiveType.X`
  (algumas com `!=`) espalhadas em três arquivos:
  - `Scripts/Systems/Battle/DamageCalculator.cs` (17), por exemplo:
    - `:147` Sede de Batalha;
    - `:190` Fúria Crescente;
    - `:313` Couraça;
    - `:366` Precisão Letal;
    - `:376` Fio da Lâmina.
  - `Scripts/Systems/Battle/BattleUnit.cs` (24), por exemplo:
    - `:314` e `:1089` Fixação;
    - `:357` Aura Vital;
    - `:730`, `:853` e `:1203` Vigília (checada nos **aliados**);
    - `:1215` Dupla Voz;
    - `:1464` Mãos Firmes.
  - `Scripts/Systems/Battle/BattleCombatant.cs:168` (Fôlego).
- O estado de combate das passivas (pilhas, contadores) é efêmero e fica em `BattleCombatant`
  (`BattleCombatant.cs:29-32`), não no `DigimonInstance`.

**Restrição de desenho já documentada**
- `PASSIVAS_SPEC.md` seção 6 ("Se um dia forem 2 passivas ativas simultâneas") diz que todo o
  balanceamento assume **1 passiva ativa**.
- A mesma seção lista combinações a bloquear se o limite mudar:
  - Infiltrador + Passo Fantasma;
  - Postura Firme + Fixação;
  - Couraça + Fôlego;
  - qualquer par com duas fontes de cura ou regeneração.

## 2. Passivas dos oponentes

- **É o mesmo sistema** (`PassiveType` no mesmo campo `DigimonInstance.PassiveId`). Só muda a origem:
  - **Batalha Livre**: sorteio do pool da espécie, em `Scripts/Systems/Battle/EnemyGenerator.cs:58`.
  - **Selvagem de exploração**: sorteio do pool da espécie, em `GameManager.cs:1131`.
  - **Campeonato**: passiva fixa por design, `TournamentOpponentData.PassiveId`
    (`Scripts/Data/TournamentOpponentData.cs:15`), copiada em `GameManager.BuildTournamentEnemyTeam`
    (`GameManager.cs:1148`, atribuição em `:1167`). O comentário ali diz que, sem passiva
    definida, o oponente fica sem passiva, porque não há sorteio de fallback.
- **Premissa que não bateu:** **nenhum** dos 13 JSONs de `Data/Tournaments/` define `PassiveId`.
  Conferi com `grep -i passiv Data/Tournaments`, que voltou vazio. Exemplo:
  `Data/Tournaments/grande_evento.json` tem só `DigimonId`/`Level` em `Opponents`.
  - Na prática, **todos os oponentes de campeonato lutam sem passiva hoje**.
  - O texto do tutorial ("Times de Campeonato usam uma passiva fixa",
    `Scripts/UI/TutorialScreen.cs`, tópico Passivas) descreve uma intenção que nunca foi preenchida
    nos dados.

## 3. Histórico evolutivo por indivíduo

**Confirmado: não existe.**
- Não há nenhum campo de histórico, linhagem, formas anteriores ou espécie de origem em
  `DigimonInstance`. A busca por `history|lineage|ancestr|previousform|EvolvedFrom|OriginSpecies`
  em `Scripts/`, `Scenes/` e `Enums/` voltou vazia.
- Os campos persistidos do indivíduo estão em `DigimonInstance.cs:22-186`.

**O que existe de parecido, mas não serve**
- `CenterState.DiscoveredDigimonIds` (`Scripts/Models/World/CenterState.cs:189`) registra as
  espécies que já passaram pelo Center. É **global do Center**, não por indivíduo. É alimentado na
  evolução por `GameManager.cs:737`.
- `DigimonInstance.BlockedEvolutionTargetIds` (`DigimonInstance.cs:95`) é por indivíduo, mas
  guarda evoluções **bloqueadas** (o futuro), não o passado.
- O evento `DigimonInstance.Evolved(oldForm, newForm)` (`DigimonInstance.cs:476`) carrega a
  transição, mas ninguém a grava. Hoje só o visual escuta esse evento, para tocar a animação.

## 4. Evolução: onde gravar "passou pela forma X"

- **Fluxo:**
  1. `GameManager.TryToEvolve` (`GameManager.cs:722`);
  2. `EvolutionSystem.TryToEvolve` (`Scripts/Systems/Evolution/EvolutionSystem.cs:29`);
  3. aplica em `digimon.Evolve(newForm, multiplier)` (`EvolutionSystem.cs:82`).
- **Ponto exato:** `DigimonInstance.Evolve`, `Scripts/Gameplay/DigimonInstance.cs:449-477`.
  - A forma antiga está em `oldForm` (`:451`).
  - A troca acontece em `BaseData = newForm` (`:453`).
  - A passiva é re-sorteada em `:472`.
  - A transição é anunciada em `Evolved?.Invoke(oldForm, newForm)` (`:476`).
  - Gravar ali cobre **todo** caminho de evolução: o normal, a troca de modo do Belphemon e os
    dois usos no GameAudit (`GameAudit.cs:2126`, `:2174`).
- **A troca de modo do Belphemon passa pelo mesmo caminho:** Sleep Mode (id 128) → Rage Mode
  (id 123) é uma aresta comum em `Data/Evolutions/belphemon_evolutions.json`. Pela regra "uma por
  espécie percorrida", o Rage Mode contaria como uma espécie a mais, mesmo sendo o mesmo estágio.
  É uma decisão de desenho.
- **Também é preciso gravar no nascimento** (a forma Baby), que **não** passa por `Evolve`:
  `EggSystem.cs:185-190`. O mesmo vale para `GameManager.AddDebugDigimon`
  (`GameManager.cs:1900-1904`).
- **Observação lateral (não é bug desta fase):**
  - `Evolve` faz `BaseData = newForm` com o objeto **do banco**, sem clonar. O construtor clona
    (`DigimonInstance.cs:205`, `CloneBaseData` em `:234`).
  - Por isso, um Digimon evoluído grava o pool `Passives` da espécie no save, e um recém-nascido
    grava `Passives: []`. Visto no save real: linhas ~155 e ~222 do `save.json`.
  - Sem efeito hoje, mas o histórico não deve depender de `BaseData.Passives`: o pool deve ser
    lido sempre do banco (`DatabaseManager.GetDigimon`), como o código já recomenda em
    `DigimonData.cs:74-80`.

## 5. Jogress

- **Onde acontece:**
  - `GameManager.TryToFuse`: `Scripts/Managers/GameManager.cs:764` (o prompt citava `:780`; as
    linhas andaram).
  - A fusão em si: `JogressSystem.TryToFuse`, `Scripts/Systems/Jogress/JogressSystem.cs:25-68`.
  - Depois disso, o `GameManager`:
    - remove os dois pais (`GameManager.cs:779-780`);
    - adiciona o fundido (`:784`);
    - dispara `JogressFused` (`:796`).
- **Hoje o resultado é um indivíduo novo** (`new DigimonInstance(resultForm)`,
  `JogressSystem.cs:60`):
  - passiva sorteada do zero (`:62`);
  - stats pela **média** dos dois pais (`BlendStatsFromParents`, `:77-104`);
  - nada mais dos pais é herdado: nível, idade, batalhas e histórico voltam ao zero.
- **Não há "linha principal" nem "secundária"; os pais são tratados de forma simétrica:**
  - receita sem ordem: `JogressData.MatchesPair`, `Scripts/Data/JogressData.cs:24-26` ("A+B é a
    mesma receita que B+A");
  - busca em `DatabaseManager.GetJogressRecipe` (`DatabaseManager.cs:957`);
  - stats por média simétrica.
- O único lugar com ordem é a tela: `JogressPickScreen` chama `TryToFuse(_selected[0], _selected[1])`
  (`Scripts/UI/JogressPickScreen.cs:144`) na ordem do clique. Ninguém usa essa ordem.
- **Receitas** (3): Omegamon (52+53→171), Examon (138+135→174), Paildramon (191+198→192), em `Data/Jogress/`.

## 6. Espécies

- **199 espécies distintas**, um JSON por espécie em `Data/Digimon/`, carregadas por
  `DatabaseManager.LoadDigimons` (`DatabaseManager.cs:91`). Todas têm id único.
- **Por estágio:**

  | Baby | InTraining | Rookie | Champion | Ultimate | Mega | MegaPlus | Special |
  |---|---|---|---|---|---|---|---|
  | 20 | 23 | 32 | 35 | 38 | 49 | 2 | 0 |

- **Por Role:**

  | Ranged | Warrior | Assassin | Tank | Support |
  |---|---|---|---|---|
  | 65 | 61 | 39 | 24 | 10 |

- **Tabela central:** sim, é a `DigimonData` (`Scripts/Data/DigimonData.cs:14-81`). Já existe
  `Passives` (`:81`), mas ela é um **pool**: 197 espécies com 3 e 2 com 2. **Não existe "uma
  passiva por espécie".** Se a passiva herdada for "a passiva da espécie", falta definir qual
  delas: um campo novo (ex.: `InheritedPassive`) ou a que o indivíduo sorteou quando estava
  naquela forma.
- **Dimensão das linhas evolutivas** (calculado sobre `Data/Evolutions/`, 337 arestas únicas):
  - Um caminho completo Baby→fim tem **6 formas** (1.399 caminhos) ou **7** (19 caminhos, com
    MegaPlus ou troca de modo). Acumulando uma por espécie, o Mega teria **6 passivas**.
  - **152 de 337 arestas (45%) mudam de Role.** Só 102 de 1.418 caminhos ficam numa Role só. Os
    demais passam por 2 (510), 3 (539), 4 (257) ou 5 (10) Roles distintas.
  - Hoje toda passiva é presa a uma Role (`PassiveData.Role`, `PassiveSystem.IsCompatible`). Por
    isso, herdar a passiva da forma anterior significa, em quase toda linha, um Digimon carregando
    passiva de **outra** Role. Por exemplo, Vigília é de Support e é checada nos aliados.

## 7. Onde as passivas aparecem na UI

- **Ficha do Digimon:** é o único lugar que mostra a passiva.
  - Tela: `Scripts/UI/DigimonDetailScreen.cs`.
  - Nós: `PassiveNameLabel`/`PassiveDescriptionLabel` dentro de `PassiveBox` (`:84-89`;
    na cena, `Scenes/Center/DigimonDetailScreen.tscn:173-188`).
  - Preenchimento: `RefreshPassiveInfo` (`:165-185`), que mostra **uma** passiva ("✨ Nome" + descrição).
  - A lista de herdadas entraria logo abaixo, no mesmo `PassiveBox`, antes de `HSeparator4`
    (`.tscn:191`).
  - Atenção ao espaço: a janela tem altura fixa (`offset_top -320` / `offset_bottom 320`,
    `.tscn:60-62`, 640px) e já tem a seção "Treino Autônomo" embaixo. Seis passivas pedem rolagem
    ou outro arranjo.
- **Batalha:** **não mostra passiva nenhuma.**
  - O painel do Digimon selecionado (`Scripts/Systems/Battle/BattleArena.cs:57-61`, nós em
    `:99-102`; cena `Scenes/Battle/BattleArena.tscn:100`) tem retrato, nome (`:350`) e HP
    (`:360`/`:368`).
  - Se a lista for mostrada na luta, esse é o ponto natural.
- **Outros pontos sem passiva:** HUD, Enciclopédia, montagem de time e Guia de Evolução. Só os
  textos de tutorial e as notas de versão falam de passiva.

## 8. Save: como estender sem quebrar

- **Como é gravado:** `SaveSystem` serializa `SaveData` inteiro com
  `JsonSerializer.Serialize(..., new JsonSerializerOptions { WriteIndented = true })`
  (`Scripts/Systems/Save/SaveSystem.cs:31-34`). Lê com `JsonSerializer.Deserialize<SaveData>(json)`
  **sem opções** (`SaveSystem.cs:71`, `:107`).
  - `CenterState.Digimons` (`CenterState.cs:119`) grava cada `DigimonInstance` com todas as
    propriedades públicas, inclusive o `BaseData` inteiro.
- **Ponto de extensão:** uma propriedade nova em `DigimonInstance` com inicializador
  (`public List<int> X { get; set; } = new();`). Num save antigo, sem a chave, ela fica com o valor
  inicial (vazia). É o mesmo padrão de `BlockedEvolutionTargetIds` (`DigimonInstance.cs:95`) e
  `AutoTrainingDisabledStats` (`:153`). Nenhuma migração de formato é necessária.
- **Atenção 1: enums viram número no save.**
  - Não há `JsonStringEnumConverter` no `SaveSystem`. No save real aparecem `"PassiveId": 30` e
    `"Stage": 2`.
  - Se o histórico gravar `PassiveType`, **reordenar ou inserir membros no meio do enum** troca a
    passiva de todo mundo em saves existentes.
  - Mais seguro: gravar **ids de espécie** (`int`, estáveis) e/ou só acrescentar membros novos no
    fim do enum.
  - O `JsonStringEnumConverter` do AGENTS.md vale para os dados de `Data/`
    (`DatabaseManager`), não para o save.
- **Atenção 2: saves antigos não têm como reconstruir o histórico.**
  - Um Digimon já evoluído chega só com a forma atual. A teia de evolução é ambígua, então não dá
    para saber por qual pré-evolução ele passou de verdade.
  - Opções:
    - começar o histórico só com a forma atual;
    - inferir pela teia, como palpite;
    - deixar vazio.
  - Se precisar rodar uma vez só, o padrão de migração existente é o flag
    `CenterState.AgeCapMigrationApplied` (`CenterState.cs:182`; usado em `GameManager.cs:180-184`)
    ou o idempotente `ApplyRetroactivePassives` (`GameManager.cs:292`/`:350`).
- **Atenção 3: o boot regrava o save.**
  - Hoje `ApplyRetroactivePassives` já regrava o save no boot quando sorteia alguém.
  - Uma migração de histórico teria o mesmo efeito: faça backup do `save.json` antes de testar.

---

## Premissas que não bateram

1. **Branch-base:** a `calendario-campeonatos` não está mergeada na `development`. Usei a
   `hud-calendario` (que contém a `calendario-campeonatos` mais o HUD), depois de commitar as
   pendências nela, com aprovação.
2. **Passivas de campeonato:** os JSONs de campeonato **não usam `PassiveId`** (nenhum dos 13). O
   campo existe (`TournamentOpponentData.cs:15`) e o código o lê (`GameManager.cs:1167`), mas todo
   oponente de campeonato luta sem passiva. O tutorial diz o contrário.
3. **Linha da fusão:** `GameManager.cs:780` hoje é a remoção do 2º pai. `TryToFuse` está em
   `GameManager.cs:764`, e a fusão de fato em `JogressSystem.cs:25-68`.
4. **"Uma passiva por espécie":** não existe hoje. Cada espécie tem um **pool** de 2-3 passivas, e
   o indivíduo sorteia uma (re-sorteada a cada evolução). Antes de desenhar, falta decidir de onde
   vem a passiva herdada de cada espécie.

## Riscos de desenho para a Fase 1 (não são premissas, só para decidir)

- `PASSIVAS_SPEC.md` seção 6 assume 1 passiva ativa e lista combinações proibidas. Acumular até 6
  passivas reabre todo o balanceamento e o `EnemyGenerator`, cujo rebalanceamento por soma de
  stats não enxerga passiva (spec R5).
- 45% das arestas mudam de Role, e as passivas hoje são presas a Role.
- As 42 checagens `PassiveId == X` precisariam virar algo como `HasPassive(X)`. É preciso definir
  também o que acontece com a **mesma** passiva herdada duas vezes: acumula ou não.
- O Jogress é simétrico: decidir se o fundido herda as linhas dos dois pais, de nenhum, ou de um
  "principal" que hoje não existe.
- Oponentes (Batalha Livre, selvagens, campeonato) não têm histórico. Decidir se ganham passivas
  herdadas simuladas para manter o equilíbrio.
