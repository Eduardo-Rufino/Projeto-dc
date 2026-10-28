# Passivas herdadas — Fase 1c: impacto em batalha (medição)

Data: 2026-10-03 · Branch: `passivas-herdadas` (sobre o commit `a20d4f8`, Fase 1b)

Raio-x antes do teste manual. **Nenhuma lógica nem dado do jogo foi alterado.** Tudo saiu de batalhas
reais (`BattleArena`, a mesma cena do jogo) e de um Center simulado pela virada de dia real, rodados
pela ferramenta `passivas/ferramentas/InheritedImpactTool.tscn` (fora da lógica do jogo, salvamento
desligado, igual ao GameAudit). Comando:

```
godot --headless --fixed-fps 60 --path . res://passivas/ferramentas/InheritedImpactTool.tscn
DEBUG_1C_TASKS=1,2,3,4,5,6   (6 = tarefa 1 calibrada)
```

## Como foi medido

- **Digimons das rotas:** construídos pela cadeia real de evolução (`Evolve`) desde o Baby, cumprindo o
  mínimo de cada aresta. Por isso o `EvolutionHistory` e os stats são os que uma rota real produz. O
  caso **"sem passiva"** é uma cópia idêntica (mesmo nível, mesmos stats, mesma passiva própria) com o
  histórico zerado.
- **Passiva própria** fixa (a primeira do pool) em todo mundo, para tirar o ruído do sorteio.
- **Times de controle fixos:**
  - **Ultimate:** Zudomon (Tank, Couraça), Rapidmon (Ranged, Alvo Marcado) e Delumon (Warrior, Golpe
    Pesado).
  - **Mega:** Vikemon (Tank), Kentaurusmon (Ranged, Alvo Marcado) e Magnamon (Warrior).
  - O Alvo Marcado é a única fonte de efeito negativo, porque não há Debuffer fora do piloto nesses
    estágios.
- **Disparo de cada passiva:** lido do estado público da luta a cada quadro (contadores, flags,
  atordoamento etc.). O golpe extra do Garurumon não deixa rastro, então foi contado envolvendo o
  sorteio `InheritedPassiveSystem.Roll`, sem mudar o resultado.

---

## 1. Impacto em batalha (6 rotas: 3 por linha, até Ultimate, nível 30)

Time = 3 cópias da rota. Na Ultimate, o histórico vai do Baby ao Champion (a passiva da própria Ultimate
só entra quando ela evoluir de novo).

### 1a. Controle fixo no nível 30 (como pedido)

| Rota | Forma | Herdadas ativas | COM | SEM | Diferença | Tempo COM / SEM |
|---|---|---|---|---|---|---|
| A1 Greymon > MetalGreymon | MetalGreymon (Warrior) | Botamon, Koromon, Agumon, Greymon | 30/30 (100%) | 30/30 (100%) | 0 p.p. | 13,3s / 13,8s |
| A2 Devimon > LadyDevimon | LadyDevimon (Assassin, Especial) | Botamon, Koromon, Agumon, Devimon | 30/30 (100%) | 16/30 (53%) | **+47 p.p.** | 24,5s / 30,8s |
| A3 Dimetromon > Triceramon | Triceramon (Tank) | Botamon, Koromon, Agumon, Dimetromon | 28/30 (93%) | 11/30 (37%) | **+57 p.p.** | 28,9s / 32,5s |
| G1 Garurumon > WereGarurumon | WereGarurumon (Warrior) | Punimon, Tsunomon, Gabumon, Garurumon | 0/30 (0%) | 0/30 (0%) | 0 p.p. | 17,7s / 16,0s |
| G2 Garurumon > MachGaogamon | MachGaogamon (Warrior) | Punimon, Tsunomon, Gabumon, Garurumon | 0/30 (0%) | 0/30 (0%) | 0 p.p. | 44,3s / 35,8s |
| G3 IceDevimon > Destromon | Destromon (Ranged, Especial) | Pichimon, Bukamon, Gabumon, IceDevimon | 0/30 (0%) | 0/30 (0%) | 0 p.p. | 32,0s / 30,4s |

**Leitura:** o controle fixo no nível 30 está longe do ponto de equilíbrio de 4 das 6 rotas. Elas
ganham ou perdem tudo **com e sem** passiva, e aí qualquer bônus some. O "0 p.p." dessas 4 **não**
quer dizer que a passiva é irrelevante; quer dizer que a luta já estava decidida pelos stats. Por isso
fiz duas medições complementares.

### 1b. Espelho: o mesmo time com passivas contra ele mesmo sem passivas

| Rota | COM vence | Tempo |
|---|---|---|
| A1 MetalGreymon | 28/30 (93%) | 15,1s |
| A2 LadyDevimon | 30/30 (100%) | 18,3s |
| A3 Triceramon | 30/30 (100%) | 38,2s |
| G1 WereGarurumon | 30/30 (100%) | 25,0s |
| G2 MachGaogamon | 30/30 (100%) | 22,1s |
| G3 Destromon | 26/30 (87%) | 41,4s |

### 1c. Controle calibrado: mesma composição, nível ajustado ao ponto de virada de cada rota

O nível do controle é o mais alto em que a versão SEM ainda vence pelo menos metade das lutas de teste.

| Rota | Nível do controle | COM | SEM | Diferença |
|---|---|---|---|---|
| A1 MetalGreymon | 44 | 22/30 (73%) | 14/30 (47%) | **+27 p.p.** |
| A2 LadyDevimon | 33 | 24/30 (80%) | 14/30 (47%) | **+33 p.p.** |
| A3 Triceramon | 30 | 29/30 (97%) | 11/30 (37%) | **+60 p.p.** |
| G1 WereGarurumon | 14 | 27/30 (90%) | 13/30 (43%) | **+47 p.p.** |
| G2 MachGaogamon | 20 | 30/30 (100%) | 23/30 (77%) | **+23 p.p.** |
| G3 Destromon | 2 | 13/30 (43%) | 6/30 (20%) | **+23 p.p.** |

**Conclusão da tarefa 1:** em luta equilibrada, as 4 passivas herdadas de uma Ultimate mudam o
resultado em **+23 a +60 p.p.** em todas as 6 rotas, muito acima do limiar de 10 p.p. Elas não são
irrelevantes; se algo, são **fortes**. Por referência, a soma típica dessas 4 passivas fica entre +7% e
+12% de HP equivalente, +5% de dano ou de stat e um efeito de condição, e isso já basta para virar
lutas parelhas.

**Destaque: Devimon (perfurar defesa).** A rota A2 dá +47 p.p. contra o controle fixo, e o Demon da
tarefa 4 vai de 0/30 para 30/30. A causa provável: o dano base é `ataque − defesa` (mínimo 1). Quando o
ataque está perto da defesa do alvo, ignorar 20% da defesa multiplica o golpe várias vezes. A passiva
própria Rompe-Guarda, que faz algo parecido, tem um teto de ganho (`RompeGuardaMaxGainRatio`); a
herdada do Devimon não tem teto.

**Observação lateral (dado da evolução, não das passivas):** LadyDevimon e Destromon atacam com Ataque
Especial, mas a rota que leva até elas pede PA. Elas chegam com PA 252 / EA 109 e PA 317 / EA 83. O
Destromon dessa rota é muito fraco (perde para o controle no nível 5) e as lutas dele vão para a morte
súbita (cerca de 125s).

---

## 2. Quanto cada passiva dispara em batalha real (28 passivas, 10 batalhas cada)

Portador = o Mega descendente "natural" daquela espécie, nível 45, com **só aquela espécie** no
histórico. Ele luta com Craniummon (Tank) e Sakuyamon (Ranged) contra o controle Mega. "Disparou" =
o efeito aconteceu pelo menos uma vez na luta.

| Espécie | Efeito | Portador | Disparou |
|---|---|---|---|
| Botamon | +3% HP máx | WarGreymon (Warrior) | 10/10 |
| Punimon | +3% HP máx | MetalGarurumon (Warrior) | 10/10 |
| Jyarimon | +3% Velocidade | WarGreymon (Warrior) | 10/10 |
| Pichimon | +3% ED | Ragnamon (Ranged) | **4/10** |
| Chibomon | +3% PA | WarGreymon (Warrior) | 10/10 |
| Koromon | +4% HP máx | WarGreymon (Warrior) | 10/10 |
| Tsunomon | +4% PD | MetalGarurumon (Warrior) | 9/10 |
| Bukamon | +4% Velocidade | MetalGarurumon (Warrior) | 10/10 |
| Agumon | +5% dano físico | WarGreymon (Warrior) | 10/10 |
| Gabumon | +5% PA | MetalGarurumon (Warrior) | 10/10 |
| Greymon | Crítico encurta efeitos negativos | WarGreymon (Warrior) | **0/10** |
| Devimon | O 4º golpe ignora 20% da defesa | Beelzebumon (Assassin) | 10/10 |
| Dimetromon | +5% Vel; +8% em quem já agiu | Leviamon (Tank) | 10/10 |
| Garurumon | À distância: golpe extra | MetalGarurumon (Warrior) | **0/10** (0 golpes extras) |
| IceDevimon | 1º golpe recebido −20% | Ragnamon (Ranged) | 7/10 |
| MetalGreymon | Abaixo de 50%: +15% físico | WarGreymon (Warrior) | 8/10 |
| RizeGreymon | Crítico cura 4% | ShineGreymon (Warrior) | 5/10 |
| LadyDevimon | Abate: +10% Velocidade | Lilithmon (**Support**) | **0/10** |
| Lamiamon | 6% esquiva → crítico garantido | Medusamon (Assassin) | **1/10** |
| Lucemon Falldown Mode | Crítico drena 6% | Lucemon Satan Mode (Assassin) | 6/10 |
| Myotismon | Cura o aliado mais ferido | Beelzebumon (Assassin) | 10/10 |
| SkullSatamon | +8% em Tanks; ½ do dano de Tanks | Piedmon (Assassin) | 10/10* |
| Triceramon | 20% de ignorar o dano | Dinomon (Warrior) | 6/10 |
| WereGarurumon | +8% físico (16% no 1º golpe) | MetalGarurumon (Warrior) | 10/10 |
| MachGaogamon | +6% no 1º golpe | MirageGaogamon (Warrior) | 10/10** |
| Destromon | À distância: 3ª estaca = crítico | Boltmon (**Tank**) | **0/10** (0 críticos forçados) |
| Astamon | 10% de atordoar (20% abaixo de 50%) | Ragnamon (Ranged) | 8/10 |
| Belphemon Sleep Mode | +15% PD/ED, −10% Vel | Belphemon Rage Mode (Warrior) | 10/10 |

\* O controle tem um Tank (Vikemon). Contra times sem Tank, essa passiva não faz nada.
\*\* Dispara sempre, mas vale só para **um golpe** por luta.

**Muito abaixo das outras:**
- **Greymon, 0/10:** precisa acertar um crítico (10%) **enquanto** tem um efeito negativo ativo. Fora
  de times com Debuffer, a condição quase nunca se junta.
- **Garurumon, 0/10, e Destromon, 0/10:** o descendente natural não ataca à distância. Ver a tarefa 3.
- **LadyDevimon, 0/10:** o descendente natural (Lilithmon) é **Support**, que cura e aplica efeitos e
  quase nunca dá o golpe final. É o mesmo problema do Garurumon: a Role da forma seguinte desliga a
  passiva. (A outra filha da LadyDevimon, Medusamon, é Assassin e daria o abate.)
- **Lamiamon, 1/10:** 6% por golpe recebido, numa luta curta, quase nunca aparece. O crítico garantido
  depois da esquiva quase nunca chega a ser usado.
- **Pichimon, 4/10, e IceDevimon, 7/10:** dependem de o portador apanhar, e o portador natural é Ranged,
  que fica atrás. Não é defeito, mas o efeito é menor do que parece no papel.

**Ressalva de método:** "disparou" foi lido do estado da luta. Para as passivas de stat (PA, PD, ED, HP)
conta como disparo o stat ter sido usado de fato: atacou, ou apanhou de quem usa aquele tipo de ataque.
Para o Dimetromon e o SkullSatamon, o alvo exato do golpe não fica registrado, então conta se havia um
alvo que cumpria a condição.

---

## 3. Rotas "mortas": Garurumon e Destromon

20 batalhas por caso, com o descendente real carregando o histórico real da rota.

| Rota | Forma final | Ataques à distância do portador | Bônus disparado |
|---|---|---|---|
| Garurumon > WereGarurumon (Ultimate) | WereGarurumon (Warrior) | 0 | **0 golpes extras** em 20 lutas |
| Garurumon > MachGaogamon (Ultimate) | MachGaogamon (Warrior) | 0 | **0** |
| … > WereGarurumon > MetalGarurumon | MetalGarurumon (Warrior) | 0 | **0** |
| … > WereGarurumon > Gankoomon | Gankoomon (Warrior) | 0 | **0** |
| … > MachGaogamon > MirageGaogamon | MirageGaogamon (Warrior) | 0 | **0** |
| … > Destromon > Demon | Demon (Assassin) | 0 | **0 críticos forçados** |
| … > Destromon > Boltmon | Boltmon (Tank) | 0 | **0** |
| … > Destromon > Belphemon Sleep | Belphemon Sleep Mode (Tank) | 0 | **0** |
| *referência:* Garurumon no histórico de um Ranged (Seraphimon) | Seraphimon (Ranged) | — | 18 golpes extras, em 11/20 lutas |
| *referência:* Destromon no histórico de um Ranged (Seraphimon) | Seraphimon (Ranged) | — | 52 críticos forçados, em 20/20 lutas |

**Confirmado:** nas rotas reais as duas passivas ficam **100% inertes**, com 0 disparos em 160 lutas.
**Todas** as formas depois de Garurumon e de Destromon perdem a Role Ranged. Com um portador Ranged,
elas funcionam bem (referência), então o problema é só a combinação passiva × rota. O mesmo padrão
apareceu na tarefa 2 para a **LadyDevimon** (filha Support).

---

## 4. Duas rotas, mesmo estágio

**Primeiro, uma constatação estrutural:** duas Ultimate irmãs do mesmo Champion carregam **exatamente
as mesmas** passivas herdadas. MetalGreymon e RizeGreymon herdam [Botamon, Koromon, Agumon, Greymon].
A passiva de cada Ultimate só passa a valer quando ela evolui para Mega. Por isso a comparação foi feita
no **Mega com a mesma espécie final** e históricos diferentes.

Controle Mega nível 45, 30 batalhas por caso, 3 cópias por time.

| Forma final | Rota A | Rota B (mesmos stats de A) | Sem herdadas | Rota B (stats da própria rota) | Duelo A vs B (mesmos stats) |
|---|---|---|---|---|---|
| WarGreymon | via MetalGreymon: 30/30 | via RizeGreymon: 30/30 | 30/30 | 30/30 | **A vence 25/30 (83%)** |
| Gankoomon | via WereGarurumon: 3/30 (10%) | via MachGaogamon: 1/30 (3%) | 0/30 | **8/30 (27%)** | **A vence 28/30 (93%)** |
| Demon | via SkullSatamon: 30/30 | via Lucemon FM: 30/30 | **0/30** | 30/30 | **A vence 25/30 (83%)** |

Stats de cada rota no Mega 45:
- **WarGreymon:** A tem PA 579, B tem PA 408.
- **Gankoomon:** A tem EA 345, B tem EA 192.
- **Demon:** A tem PA 646 e Vel 411; B tem PA 503 e Vel 285.

**Leitura:**
- **Contra um adversário fixo, a diferença entre as rotas quase desaparece.** O resultado fica
  saturado (100% ou perto de 0%) porque os stats e a Role dominam. Exceção: o Demon, onde **ter**
  herdadas (as duas rotas têm Devimon) decide tudo, de 0/30 para 30/30.
- **No duelo direto, com os mesmos stats**, a rota com a passiva melhor vence de 83% a 93%. A
  diferença existe e é visível quando todo o resto é igual.
- **Com os stats que cada rota realmente produz**, a diferença de stats engole a de passiva. No
  Gankoomon, a rota "pior" de passiva (MachGaogamon) se sai melhor contra o controle (27% contra 10%).
  Na prática, a escolha de rota hoje é decidida pelos stats; a passiva herdada é um desempate forte,
  mas secundário.

---

## 5. Center simulado: ficha de um Digimon com 5+ passivas

Center novo (`ResetScratch`). Três Digimons nascem no Baby e evoluem **pela virada de dia real**
(`ClockSystem` → `GameManager.OnDayPassed` → `TryToEvolve`). A idade corre de verdade, sem forçar
`AgeInDays`. O "jogador" simulado faz o seguinte:
- treina os stats da próxima forma da rota;
- usa o bloqueio de evolução nas irmãs;
- alimenta, cura doença e deixa descansar.

Todas as rotas chegaram ao Mega até o **dia 50**; o limite é a Astamon, que exige idade 24.

**WarGreymon** (Botamon > Koromon > Agumon > Greymon > MetalGreymon > WarGreymon), vivo, idade 50, nível
28. A ficha mostraria:

> 🧬 **Passivas herdadas (5)**
> - Botamon: +3% HP máx
> - Koromon: +4% HP máx
> - Agumon: +5% dano físico
> - Greymon: Crítico encurta efeitos negativos em 1 ação
> - MetalGreymon: Abaixo de 50% HP: +15% dano físico (até o fim)

**MetalGarurumon** (Punimon > Tsunomon > Gabumon > Garurumon > WereGarurumon > MetalGarurumon): **(5)**
- Punimon: +3% HP máx
- Tsunomon: +4% PD
- Gabumon: +5% PA
- Garurumon: À distância: 15% de chance de golpe extra (30% do dano). *Aparece na ficha, mas nunca
  dispara, porque MetalGarurumon é Warrior (tarefa 3).*
- WereGarurumon: +8% dano físico (16% no 1º golpe)

**Ragnamon** (Pichimon > Bukamon > Gabumon > IceDevimon > Astamon > Ragnamon): **(5)**
- Pichimon: +3% ED
- Bukamon: +4% Velocidade
- Gabumon: +5% PA
- IceDevimon: 1º golpe recebido: −20% de dano
- Astamon: 10% de chance de atordoar (20% em alvo abaixo de 50%)

**Omegamon** (Jogress de WarGreymon + MetalGarurumon, com a linha do Agumon como principal): **(10)**.
São as 5 do WarGreymon seguidas das 5 do MetalGarurumon.

**Como fica na tela** (layout da Fase 1b): o título "🧬 Passivas herdadas (5)" e cerca de 3 linhas
visíveis; as outras 2 aparecem rolando dentro do quadro, sem estourar a janela. No Omegamon, com 10
linhas, a área de rolagem continua do mesmo tamanho e só fica mais longa. Duas coisas valem nota:
1. A ficha mostra passivas que **nunca vão disparar** naquela forma (Garurumon num Warrior), sem
   nenhum aviso.
2. Como espécie repetida conta uma vez, um Omegamon de duas linhas que compartilham o Gabumon (como
   Ragnamon + MetalGarurumon) mostraria 9, não 10.

---

## Resumo para decidir antes de jogar

1. **As passivas herdadas mudam lutas, e com força.** Em luta equilibrada dão +23 a +60 p.p. por
   Ultimate, e no espelho ganham de 87% a 100%. Vale olhar se o total, que chega a 5 no Mega e a 10 no
   Omegamon, não fica forte demais perto da passiva própria única.
2. **Devimon** (perfurar 20% da defesa) é a mais desproporcional quando ataque ≈ defesa: o Demon vai de
   0/30 para 30/30 contra o controle. Candidata a um teto de ganho, como o do Rompe-Guarda.
3. **Inertes por Role da forma seguinte:** Garurumon (0/160 nas rotas reais), Destromon (0 críticos
   forçados) e LadyDevimon (filha Support não abate).
4. **Raras demais para serem sentidas:** Greymon (0/10) e Lamiamon (1/10).
5. **Irmãs do mesmo Champion têm o mesmo histórico na Ultimate.** A escolha entre elas só pesa na
   passiva a partir do Mega, e mesmo lá os stats da rota pesam mais do que a passiva herdada.

## Observações de execução

- O save real não foi tocado: o hash do `save.json` e do `.bak` é igual ao do backup antes e depois.
- Numa das execuções, o log mostrou um `InvalidOperationException: Handle is not initialized` vindo de
  dentro da ponte C# do Godot (`ScriptManagerBridge.SwapGCHandleForType`), ao liberar objetos entre
  batalhas. Não veio da lógica de batalha nem da ferramenta, e a execução continuou normalmente.
- A primeira versão da calibragem (1c) escolhia mal o nível nas rotas G2 e G3, porque a virada de
  vitória para derrota é brusca. Corrigi a busca para mirar o nível de virada e refiz essas duas rotas.
  O diagnóstico confirmou que o MachGaogamon vence bem em nível baixo; o Destromon é que é fraco.
