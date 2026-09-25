# Fase 0 — Exploração somente leitura: tempo e campeonatos

Branch `calendario-campeonatos`, criada a partir de `development`. Nenhum código ou dado foi
alterado nesta fase — só leitura. Este arquivo é o único artefato novo.

## 1. Relógio

**Conversão tempo real → tempo de jogo**: `Scripts/Systems/Center/ClockSystem.cs:11` —
`public const double SecondsPerGameMinute = 1.0`. Ou seja, **1 minuto de jogo = 1 segundo real**
(não é uma proporção "24h de jogo = 24min reais" declarada diretamente no código — é derivada:
1h de jogo = 60s reais = 1min real, logo 24h de jogo = 24min reais. A premissa do enunciado bate,
mas por dedução, não por uma constante literal com esse valor).

`ClockSystem.Update(double delta)` (`ClockSystem.cs:26-35`) acumula `delta` em `_elapsedSeconds`
e chama `AdvanceMinute()` (`ClockSystem.cs:68-89`) uma vez por segundo real acumulado. Quem chama
`Update` é `GameManager._Process(double delta)` em `Scripts/Managers/GameManager.cs:380-383`:

```csharp
public override void _Process(double delta)
{
    ClockSystem?.Update(delta * (IsClockSpeedDoubled ? 2.0 : 1.0));
}
```

Existe um multiplicador **x2** (`IsClockSpeedDoubled`, ligado/desligado em `GameManager.cs:373-378`
via `ToggleClockSpeed`, exposto na HUD por um botão em `HUD.cs:490`) que dobra a velocidade do
relógio quando ativado pelo jogador.

**Virada de hora/dia** — tudo dentro de `AdvanceMinute()` (`ClockSystem.cs:68-89`):
- `CurrentMinute++` (linha 70); se `>= 60`, zera e `CurrentHour++` (72-75), dispara `HourPassed`
  (77).
- Se `CurrentHour >= HoursPerDay` (`HoursPerDay = 24`, `ClockSystem.cs:15`), zera a hora e
  `CurrentDay++` (79-82), dispara `DayPassed` (84).
- `MinutePassed` dispara sempre, incondicionalmente, no fim do método (88).

**O que dispara na virada de dia (`DayPassed`) — todos os handlers achados no projeto** (fora das
ferramentas de teste em `revisao_evolucao/`):

| Handler | Local | O que faz |
|---|---|---|
| `GameManager.OnDayPassed` | `Scripts/Managers/GameManager.cs:395-418` | Pra cada Digimon: `AdvanceDay()` (Idade+1, `DigimonInstance.cs:584-587`), `TryBecomeSick()`, atualiza `DaysSickInARow`, `EvaluateDailyDeathRisks` (pode matar — linha 409), **`TryToEvolve(digimon)`** (linha 412 — é aqui que a evolução é checada). Depois: `EggSystem.AdvanceDay` (incubação), `ApplyRecruitedNpcDailyBonuses` (Carne grátis de NPC), e **`SaveGame()`** (linha 417 — confirma que o jogo salva ao final de cada dia). |
| `CenterScreen.OnDayPassed` | `Scripts/UI/CenterScreen.cs:281-287` | Só UI: atualiza o label "Day: N" da tela do Center. |

**Handlers de `HourPassed`** (não pedido explicitamente, mas relevante pro calendário por
mexerem no mesmo `ClockSystem`):
- `GameManager.OnHourPassed` (`GameManager.cs:385-393`) — `digimon.AdvanceHour(...)` pra cada
  Digimon (Fome, sono, Stamina) + `EggSystem.AdvanceHour` (incubação do ovo inicial).
- `CenterScreen.OnHourPassed` (`CenterScreen.cs:289-292`) — só UI.
- `FoodWorld.OnHourPassed` (`Scripts/Models/World/FoodWorld.cs:71-79`) — estrago de comida.
- `DigimonWorld.OnDormitoryHourPassed` (`Scripts/Gameplay/DigimonWorld.cs:1027+`) — horas dormindo
  fora do Dormitório.

**Handlers de `MinutePassed`**: `CenterScreen.OnMinutePassed` (`CenterScreen.cs:276-279`, UI) e
`HUD.RefreshDateTime` (assinado em `HUD.cs:455`, corpo em `HUD.cs:477-491`, UI).

**Contador de dia total**: existe e é persistente, não é só hora/minuto do dia atual.
`Scripts/Models/World/WorldState.cs:14-16` tem os três campos: `CurrentDay` (int, começa em 1,
sempre incrementando desde o início do save), `CurrentHour`, `CurrentMinute`. Não existe conceito
de semana/mês/estação hoje — é um inteiro linear.

## 2. Pausa

O jogo usa um sistema de "motivos de pausa" empilháveis: `Scripts/Managers/GameManager.cs:103`
(`private readonly HashSet<string> _pauseReasons = new();`), com `RequestPause(reason)`
(`GameManager.cs:105-110`) e `ReleasePause(reason)` (`GameManager.cs:112-117`), ambos terminando
em `GetTree().Paused = _pauseReasons.Count > 0`. `GameManager` não tem `ProcessMode.Always`, então
**`_Process` (e portanto `ClockSystem.Update`) simplesmente não roda enquanto a árvore está
pausada** — o relógio para de verdade, não só a UI congela.

- **Batalha**: SIM, pausa. `RequestPause("battle")` em `GameManager.cs:1031`, liberado em
  `GameManager.cs:1042`. A cena da arena recebe `ProcessMode = ProcessModeEnum.Always`
  (`GameManager.cs:1027`) pra continuar animando apesar da pausa global.
- **Exploração**: SIM, pausa. `RequestPause("exploration")` em `GameManager.cs:1378`, liberado em
  `GameManager.cs:1395`. Mesmo padrão de `ProcessMode.Always` na área (`GameManager.cs:1374`).
- **Telas/menus bloqueantes** (loja, inventário, tutorial, seleção de time, lista de campeonatos,
  enciclopédia etc.): SIM, pausa. `HUD.SyncPauseWithBlockingScreens()` (`HUD.cs:420-436`) chama
  `RequestPause("screen")` sempre que `Center.IsBlockingScreenOpen()` é true; essa lista inclui a
  tela de campeonatos (`Scenes/Center/Center.cs:1536-1554`, especificamente a linha 1543 pra
  `_tournamentListScreen`).
- **Diálogos de confirmação (`ConfirmWindow`)**: NÃO pausam — comentário explícito em
  `HUD.cs:420-421` ("diálogos de confirmação não contam").
- **Menu principal**: SIM, pausa. `MainMenuScreen.cs:103` chama `GameManager.Instance.RequestPause`.

**Tempo offline**: **não existe**. `_Notification(NotificationWMCloseRequest)`
(`GameManager.cs:1928-1934`) só chama `SaveGame()` — nenhum timestamp real (`DateTime.Now` /
`DateTime.UtcNow`) é salvo junto do relógio. A única ocorrência de `DateTime.Now` no projeto é
`Scripts/Systems/Save/SaveSystem.cs:128`, usada só pra nomear o arquivo de backup de um save
corrompido (`.corrupt-{timestamp}`), sem relação com o relógio do jogo. Ao reabrir, o
`WorldState` volta exatamente de onde o save deixou — nenhum tempo é avançado pelo tempo em que
o jogo ficou fechado.

## 3. Save/Load

`WorldState` é a única classe guardando o estado do relógio — não há timestamp real nem estrutura
paralela. A serialização é reflexiva (o objeto `SaveData` inteiro), não campo a campo:

- **Salvar**: `SaveSystem.SaveGame(SaveData save)` (`Scripts/Systems/Save/SaveSystem.cs:15-55`),
  `JsonSerializer.Serialize` na linha 29 — serializa `save` inteiro, o que inclui `save.World`
  automaticamente (`SaveData.World` declarado em `Scripts/Save/SaveData.cs:13`).
- **Carregar**: `SaveSystem.LoadGame()` (`SaveSystem.cs:57-94`), `JsonSerializer.Deserialize<SaveData>`
  na linha 71 — `World` volta populado do mesmo jeito. Chamado no boot em
  `GameManager.cs:147` (`Save = SaveSystem.LoadGame();`).

## 4. Campeonatos

**Classe de dado**: `Scripts/Data/TournamentData.cs:11-32`. Campos: `Id` (13), `Name` (14),
`Description` (15), `RecommendedLevel` (19 — comentário nas linhas 17-18 confirma: "só
informativo, não bloqueia a inscrição"), `Opponents: List<TournamentOpponentData>` (21 — o
formato 1x1/3x3 é implícito pela contagem de oponentes, não um campo explícito), `CapacityReward`
(26), `BitsReward` (31).

**Premissa não bate**: nenhum campo de restrição de inscrição (estágio máximo, idade, "sem
itens", equipe específica) existe hoje em `TournamentData`. O `game_idea.txt` menciona essas
restrições como visão futura do "Sistema de Torneios", mas o código atual só tem os 6 campos
acima — nenhum de elegibilidade.

**Loader**: `Scripts/Managers/DatabaseManager.cs`, `LoadTournaments()` (linhas 384-449, chamada
no boot em `DatabaseManager.cs:27`) lê `Data/Tournaments/*.json` e popula
`Dictionary<int, TournamentData> tournaments` (campo declarado na linha 56). Acesso via
`GetTournament(int id)` (452-459) e `GetAllTournaments()` (462-465, ordenado por Id).

**Início pela UI**: `Scripts/UI/TournamentListScreen.cs`, `RefreshUI()` (52-76) chama
`GetAllTournaments()` (linha 59) e cria um `TournamentCard` por campeonato **sem nenhum filtro**
— os 12 aparecem sempre, só marcados como "já vencido" cosmeticamente
(`ClearedTournamentIds.Contains`, linha 69). Clicar dispara o evento `TournamentSelected`
(78-81) → `Scenes/Center/Center.cs:180` assina esse evento → `OnTournamentSelected`
(`Center.cs:1417-1426`) abre `TeamSelectionScreen.Open(tournament)` → dali chega em
`GameManager.StartTournamentBattle` (`GameManager.cs:880-909`).

**Elegibilidade — não existe nenhuma checagem real.** `StartTournamentBattle`
(`GameManager.cs:880-909`) só valida: se já existe uma batalha em andamento (882-886) e se
`playerTeam.Count == tournament.Opponents.Count` (891-895). Nenhuma checagem de nível,
capacidade, idade, ou "Digimon já usado em outro campeonato". Qualquer campeonato pode ser
tentado a qualquer momento, quantas vezes o jogador quiser — perder não consome nada (comentário
em `GameManager.cs:1123-1124` confirma que a tentativa não é "gasta").

**Campeonato "já vencido"**: `Scripts/Models/World/CenterState.cs:133` —
`List<int> ClearedTournamentIds`. É campo direto de `CenterState`, que faz parte do `SaveData`,
então persiste normalmente no save.

**Aplicação da recompensa**: `GameManager.cs:1281-1301`,
`ApplyTournamentRewardIfNeeded()`, chamado em `GameManager.cs:1158` dentro de
`ApplyTeamBattleReward(BattleResult result)`, só no caminho de vitória. Guarda contra repetição
(`ClearedTournamentIds.Contains`, linhas 1286-1287) antes de somar `CapacityReward` via
`Save.Center.AddCapacity(...)` (linha 1292) e `BitsReward` via `Save.Center.AddBits(...)` (linha
1295). Campeonato nunca dá XP, nem na vitória nem na derrota (comentário em `GameManager.cs:1084-1087`).

## 5. Capacidade

`Scripts/Models/World/CenterState.cs`:
- `CapacityLimit` (linha 18) — propriedade simples, inicializada em `10` no construtor (linha 190).
- `CapacityUsed` (linha 115) — **computada, não armazenada**:
  `Digimons.Sum(d => d.CapacityCost) + Eggs.Sum(e => e.CapacityCost)`, recalculada toda vez que é
  lida, sem cache.
- `AddCapacity(int amount)` (linhas 242-245) — só `CapacityLimit += amount`. É o mesmo método
  chamado pela recompensa de campeonato (`GameManager.cs:1292`, ver item 4).
- **Sem teto máximo**: `CapacityLimit` é um `int` sem clamp superior em `AddCapacity` nem em
  nenhum outro ponto encontrado — cresce indefinidamente.

## 6. Idade

`Scripts/Gameplay/DigimonInstance.cs`:
- **Incremento**: `AdvanceDay()` (linhas 584-587, `AgeInDays++`), chamado por
  `GameManager.OnDayPassed` (`GameManager.cs:402`, ver item 1). Existe também `AdvanceDays(int
  days = 1)` (`DigimonInstance.cs:505-508`) — **achado, não pedido**: esse método plural não é
  chamado em lugar nenhum do código atual (parece reservado/morto).
- **Leituras de `AgeInDays`, por finalidade**:
  - (a) Requisito de evolução: `Scripts/Systems/Evolution/EvolutionSystem.cs:94`
    (`digimon.AgeInDays < evolution.RequiredAgeInDays` → bloqueia).
  - (b) Score de evolução (desempate entre irmãs): `EvolutionSystem.cs:146`
    (`RequiredLevel + RequiredAgeInDays`).
  - (c) Teto por estágio / morte por velhice: `GetMaxAgeForStage` (`DigimonInstance.cs:305-313`)
    e `IsPastMaxAge()` (`DigimonInstance.cs:315`); consumidos em `GameManager.cs:1772-1783`
    (migração de save antigo) e `GameManager.cs:1823` (checagem de morte diária).
  - (d) Outros: risco de doença sobe com a idade (`CalculateSicknessRisk`,
    `DigimonInstance.cs:763-767`, +2 se `AgeInDays>20`, +3 se `>40`); exibição em
    `Scripts/UI/CenterScreen.cs:147,231`, `Scripts/UI/DigimonDetailScreen.cs:126` e
    `Scripts/UI/EvolutionGuideScreen.cs:152,164`.
- **Morte por velhice existe e é imediata (não é chance)**: `GameManager.cs:1821-1831`
  (`EvaluateDailyDeathRisks`) — se `IsPastMaxAge()` é true, mata na hora via `KillDigimon`
  (`GameManager.cs:1795-1812`), sem rolagem de dado. (A morte por doença prolongada, no mesmo
  método, linhas 1833-1843, essa sim usa `RollDeathChance`.)
- **Tetos atuais por estágio** (`DigimonInstance.cs:305-313`): Baby=4, InTraining=7, Rookie=15,
  Champion=26, Ultimate=52, Mega/MegaPlus/Special=68.

## 7. UI

**Não existe nenhuma tela de calendário, mural ou NPC de anúncios hoje.** Busca por
`Calendar|Mural|Bulletin|Notice|Aviso|Anuncio` no código não encontrou nada desse tipo — só um
`_warningLabel` genérico (`Scripts/UI/HUD.cs:592`) pra mensagens temporárias tipo "área de treino
ocupada", sem relação com calendário.

**Achado relevante pro projeto futuro**: a HUD já tem um **placeholder de calendário não
funcional**. `Scripts/UI/HUD.cs:11` declara `Label _calendarLabel` (path
`TopBar/DateTime/Panel/HBoxContainer/VBoxContainer/CalendarLabel`, resolvido em `HUD.cs:98-100`),
mas dentro de `RefreshDateTime()` (`HUD.cs:477-491`) ele só recebe o texto fixo
`"Calendário"` (linha 482) — nenhum dado dinâmico por trás. É o gancho mais óbvio pra plugar uma
estação/data real.

A tela mais próxima de "listar objetivos futuros" hoje é `TournamentListScreen.cs:14-49`, que
lista todos os 12 campeonatos de uma vez, sem noção de data/disponibilidade (ver item 4).

A HUD já mostra dia/hora atuais: `HUD.cs:481` (`_dateLabel.Text = $"Dia {world.CurrentDay}"`) e
`HUD.cs:483-484` (`_clockLabel.Text = "{hora}:{minuto}"`). O mesmo padrão aparece duplicado em
`Scripts/UI/CenterScreen.cs:189,286,299` e `Scripts/UI/TrainingScreen.cs:288,318`.

## 8. Pontos de extensão prováveis

**(a) Datar campeonatos** (fazer cada campeonato ter uma data/estação/recorrência em vez de
estar sempre disponível):
- `Scripts/Data/TournamentData.cs:11-32` — precisa ganhar campo(s) de data/estação/recorrência.
- `Scripts/Managers/DatabaseManager.cs:384` (`LoadTournaments`) / `:462` (`GetAllTournaments`) —
  ponto natural pra filtrar por data em vez de sempre devolver a lista inteira.
- `Scripts/UI/TournamentListScreen.cs:14-49` — hoje lista tudo sem filtro; passaria a consumir só
  os campeonatos "ativos hoje" (ou mostrar os futuros como bloqueados/datados).
- `Scripts/Models/World/WorldState.cs:14-16` — é o lugar mais natural pra guardar a noção de
  "estação atual"/calendário (hoje só tem `CurrentDay`/`CurrentHour`/`CurrentMinute`, sem estação).
- `Scripts/UI/HUD.cs:482` — troca do texto fixo `"Calendário"` pela estação/data real, já
  calculada a partir de `WorldState`.
- Atenção: qualquer cálculo de "quantos dias faltam pro próximo campeonato" precisa considerar
  que o relógio pode rodar em velocidade dobrada (`GameManager.IsClockSpeedDoubled`, lido em
  `HUD.cs:490`) e que `AdvanceUntilHour` (`ClockSystem.cs:55-66`, usado por "Pular sono") também
  dispara `DayPassed` várias vezes de uma vez se atravessar mais de um dia.

**(b) Filtrar por regras de inscrição** (estágio máximo, nível máximo, idade, "sem itens" etc. —
hoje **não existe nenhuma**, ver item 4):
- `Scripts/Data/TournamentData.cs` — precisaria de novos campos de restrição.
- `Scripts/Managers/GameManager.cs:880-909` (`StartTournamentBattle`) — hoje só valida contagem
  de time; é onde a checagem de elegibilidade de verdade precisaria entrar (bloquear a batalha).
- `Scripts/UI/TournamentListScreen.cs` / `Scripts/UI/TournamentCard.cs` — onde os cards seriam
  desabilitados/marcados conforme o time atual do jogador não cumprir os requisitos, antes mesmo
  de tentar iniciar.

## Premissas que não bateram

Nenhuma. Todas as premissas do enunciado (relógio, virada de dia, `WorldState`, formato dos
campeonatos, `CapacityReward`/`BitsReward`) bateram com o código real. Duas ressalvas, sem impacto
no design a seguir:
- A proporção "24h de jogo = 24min reais" não é uma constante literal no código — é derivada de
  `SecondsPerGameMinute = 1.0` (1 minuto de jogo = 1 segundo real).
- `TournamentData` hoje **não tem nenhum campo de restrição de inscrição** (estágio, idade, "sem
  itens") — isso é intenção de design do `game_idea.txt`, ainda não implementada. Vale confirmar
  com o usuário se a Fase 1 deve introduzir esses campos junto com a data, ou só a data por
  enquanto.
