# Passivas herdadas — Fase 4c: raio-x da cobertura completa (leitura/simulação)

Data: 2026-10-20 · Branch: `passivas-herdadas` (sobre `83c61fc`, Fase 4b)

**Nenhum código nem dado do jogo foi alterado.** Ferramentas:
- **Tarefas 1 e 2 (estáticas):** um script Python fora do repositório que só lê `Data/Digimon`,
  `Data/Evolutions`, `Data/Jogress` e o enum.
- **Tarefas 3 e 4 (batalhas reais na `BattleArena`):** a ferramenta nova
  `passivas/ferramentas/InheritedRaioXTool.tscn`, irmã da `InheritedImpactTool` da 1c, com a mesma
  infraestrutura e o salvamento desligado. Assim como a da 1c, fica fora do git.

```
godot --headless --fixed-fps 60 --path . res://passivas/ferramentas/InheritedRaioXTool.tscn
DEBUG_4C_TASKS=3,4   DEBUG_4C_ONLY=<ids>   DEBUG_4C_ROUTES=<trecho do nome>
```

## Premissas que não bateram (só contagem; o conteúdo bate)

- **"161 espécies Baby–Ultimate":** o número real é **148** (Baby 20, In-Training 23, Rookie 32,
  Champion 35, Ultimate 38), todas com passiva. Somando o Belphemon Sleep Mode (Mega), são 149
  espécies com passiva. Não achei de onde viria 161.
- **"53 espécies novas de Champion/Ultimate":** a Fase 4 adicionou **56** (30 Champion e 26 Ultimate,
  conferido no `git diff aa92d5a 10cd0af`). A tarefa 4 mediu as 56.

Como a premissa de fundo ("100% coberto") está certa, segui com os números reais.

---

## 1. Varredura de consistência

### 1a. Cobertura

| Estágio | Com passiva | Total |
|---|---|---|
| Baby | 20 | 20 |
| In-Training | 23 | 23 |
| Rookie | 32 | 32 |
| Champion | 35 | 35 |
| Ultimate | 38 | 38 |
| **Total Baby–Ultimate** | **148** | **148** |

Fora dessa faixa, só o **Belphemon Sleep Mode** (Mega) tem passiva, do piloto. Os outros 48 Mega e os
2 MegaPlus não têm.

### 1b. Faixas de valor: 0 problemas

Cada Type foi checado contra uma faixa plausível:
- **%** em (0, 30%].
- **Chances** de esquiva e negação ≤ 25%.
- **Contadores** (ações, período, estacas) inteiros.
- **Atordoar:** `Value2 ≥ Value`.
- **Defesas/Velocidade:** `Value2` negativo.
- **Anti-Tank:** multiplicador ≤ 1.
- **Nenhum `Value2` ≠ 0** em efeito que não usa `Value2`.
- **Type** existe no enum.

Faixa real de cada efeito no jogo:

| Efeito | Espécies | Mín | Máx |
|---|---|---|---|
| MaxHpPercent | 15 | 2,5% | 6% |
| SpeedPercent | 14 | 2% | 6% |
| PhysicalAttackPercent | 15 | 3% | 10% |
| SpecialAttackPercent | 11 | 2,5% | 6% |
| PhysicalDefensePercent | 14 | 2% | 12% |
| SpecialDefensePercent | 12 | 2,5% | 6% |
| PhysicalDamageDealtPercent | 1 | 5% | 5% |
| CritShortensDebuffs | 2 | 1 ação | 1 ação |
| PeriodicDefensePierce | 6 | a cada 3, 25% | a cada 4, 18% |
| SpeedAndBonusVsActedTarget | 5 | 4% / 6% | 6% / 12% |
| RangedExtraStrike | 7 | 10% / 30% | 18% / 30% |
| FirstHitTakenReduction | 3 | 20% | 30% |
| LowHpPhysicalDamageBonus | 5 | <50%: +10% | <50%: +15% |
| CritHealMaxHp | 1 | 4% | 4% |
| KillSpeedBonus | 5 | 9% | 14% |
| DodgeThenGuaranteedCrit | 5 | 4% | 8% |
| CritLifesteal | 5 | 4% | 8% |
| HealLowestAllyOnAttack | 4 | 6% | 11% |
| AntiTank | 1 | +8% / ×0,5 | — |
| NegateDamageChance | 6 | 15% | 24% |
| PhysicalDamageWithOpeningBonus | 1 | 8% / 16% | — |
| OpeningHitBonus | 6 | 6% | 11% |
| RangedStacksGuaranteedCrit | 1 | 3 estacas | — |
| StunChance | 3 | 8% / 16% | 12% / 22% |
| DefensesUpSpeedDown | 1 | +15% / −10% | — |

**Nota:** **30 pares** de espécies têm Type+Value+Value2 idênticos. Exemplos: Botamon, Budmon e Punimon
com HP 3%; Garurumon e Grand Galemon com o golpe extra 15%/30%; Triceramon e Groundramon com negação
de 20%; LadyDevimon e Paildramon com abate 10%. Não é erro, porque nenhuma regra proíbe; registro só
como informação.

### 1c. Colisões de efeitos que só contam a 1ª ocorrência: 0

Foram varridas **todas as 1.962 cadeias até a forma final**:
- **1.418 normais**, do Baby ao fim do grafo.
- **544 de Jogress** (Omegamon, Examon e Paildramon), com o histórico real do fundido: caminho até A
  + A + caminho até B + B, e depois o que vem depois do fundido.

| Efeito | Cadeias em que aparece | Cadeias com 2+ ocorrências |
|---|---|---|
| PeriodicDefensePierce | 436 | **0** |
| StunChance | 98 | **0** |
| RangedStacksGuaranteedCrit | 57 | **0** |

Não sobrou nenhuma colisão, nem nas Jogress.

### 1d. Maiores somas de um único stat (efeitos que somam)

Conta todo efeito que soma no stat: os % puros, a Velocidade de `SpeedAndBonusVsActedTarget` e a
PD/ED/Velocidade do Belphemon Sleep Mode. Cada linha é uma combinação distinta de espécies.

| # | Stat | Soma | Espécies somadas | Exemplo de cadeia |
|---|---|---|---|---|
| 1 | **PD** | **+23,5%** | Gomamon 5,5% + Ikkakumon 6% + Zudomon 12% (3) | Pichimon > Bukamon > Gomamon > Ikkakumon > Zudomon > **Vikemon** |
| 2 | **PA** | **+22,5%** | Chibomon 3% + Agumon (Savers) 4,5% + Geogreymon 6% + Wanyamon 3,5% + Gaomon 5,5% (5) | Jogress **Omegamon** (linha do Geogreymon + linha do Gaomon) |
| 3 | **ED** | **+22%** | Pafumon 2,5% + Kyaromon 4,5% + Belphemon Sleep Mode 15% (3) | Pafumon > Kyaromon > Phascomon > IceDevimon > Destromon > Belphemon SM > **Belphemon Rage Mode** |
| 4 | **PA** | **+20%** | Tokomon 4,5% + Babydmon 5% + Dracomon 4% + Coredramon (Green) 6,5% (4) | Jogress **Examon** (SlayerDramon via Galemon + Breakdramon) |
| 5 | **HP** | **+19%** | Botamon 3% + Koromon 4% + Veemon 5% + Punimon 3% + Elizamon 4% (5) | Jogress **Examon** |

**Fora de Jogress**, os maiores PA e Velocidade são:
- **PA +15,5%:** Babydmon + Dracomon + Coredramon (Green), no Breakdramon. Empata com Babydmon +
  Dorugreymon 10%, no Alphamon (+15%).
- **Velocidade +13,5%:** Jyarimon + Dorumon + Dorugamon.
- **EA +9,5%:** o máximo, bem abaixo dos outros stats.

**Leitura:**
- **O maior risco silencioso é a linha Gomamon > Ikkakumon > Zudomon > Vikemon, com +23,5% de PD.** O
  Vikemon é Tank e o controle da 1c. Dos 23,5%, 12% vêm só do Zudomon, o maior % puro do jogo.
- **A linha que chega ao Belphemon Rage Mode** soma até +22% de ED e +19% de PD, porque o Belphemon
  SM dá 15% em cada um.
- **As Jogress somam as duas linhas.** Por isso chegam a +20% de PA e +19% de HP: são o teto natural
  do sistema. Mesmo assim, nenhum stat passa de +25%.

---

## 2. Carga total por cadeia

**Categorias usadas:**
- **Dano:** % de PA e EA, dano físico, perfurar, bônus em quem agiu, golpe extra, abaixo de 50%,
  Anti-Tank, 1º golpe e estacas.
- **Sobrevivência:** % de HP, PD e ED, 1º golpe recebido, curas no crítico, negação, esquiva,
  defesas do Belphemon e cura de aliado.
- **Utilidade:** Velocidade, abate, encurtar debuff e atordoar.

**"Inerte"** quer dizer que a Role ou o tipo de ataque da forma final desligam a passiva. Exemplos:
- golpe extra ou estacas, se a forma não é Ranged;
- PA ou dano físico, se a forma ataca com Especial (e EA, se ataca com Físico);
- abate, se a forma é Support.

### 2a. Amostra de 20 cadeias até Mega

18 cadeias normais e 2 Jogress, sorteadas com semente fixa, sem Agumon ou Gabumon no caminho. Regras:
Megas todos diferentes, no máximo 1 cadeia por Champion e 2 por Baby.

| # | Cadeia | Mega (Role, ataque) | Herdadas | Dano / Sobrev. / Util. | Inertes na forma final | "Ignora dano" |
|---|---|---|---|---|---|---|
| 1 | Mokumon > Kyaromon > Phascomon > IceDevimon > Destromon > **Demon** | Assassin, Esp. | 5 | 1 / 3 / 1 | Destromon (não Ranged) | IceDevimon |
| 2 | Botamon > Tsunomon > Elizamon > Tortamon > Triceramon > **Dinomon** | Warrior, Fís. | 5 | **0 / 5 / 0** | — | **Tortamon + Triceramon** |
| 3 | Pafumon > Kyaromon > Lucemon > Angemon > Angewomon > **Ophanimon** | Ranged, Esp. | 5 | 1 / 4 / 0 | — | Angemon |
| 4 | Yuramon > Minomon > Tentomon > Togemon > Lilamon > **Cendrillmon** | Support, Esp. | 5 | 1 / 4 / 0 | — | — |
| 5 | Yuramon > Yokomon > Wormmon > Stingmon > MegaKabuterimon > **Craniummon** | Tank, Fís. | 5 | 0 / 4 / 1 | — | MegaKabuterimon |
| 6 | Puttimon > Babydmon > Dorumon > Dorugamon > Dorugreymon > **Alphamon** | Assassin, Fís. | 5 | 2 / 1 / 2 | — | — |
| 7 | Keemon > Tsumemon > Phascomon > Snatchmon > Destromon > Belphemon SM > **Belphemon RM** | Warrior, Fís. | 6 | 2 / 3 / 1 | Tsumemon (EA), Destromon (não Ranged) | — |
| 8 | Pichimon > Tsunomon > Kotemon > Gaogamon > WereGarurumon > **MetalGarurumon** | Warrior, Fís. | 5 | 2 / 3 / 0 | — | — |
| 9 | Pabumon > Tanemon > Palmon > Sunflowmon > JewelBeemon > **GrandisKuwagamon** | Warrior, Fís. | 5 | 3 / 2 / 0 | Tanemon (EA) | — |
| 10 | Puttimon > Fluffymon > Pteromon > Galemon > Grand Galemon > **Dynasmon** | Warrior, Fís. | 5 | 3 / 1 / 1 | **Fluffymon (EA), Galemon e Grand Galemon (não Ranged)** | — |
| 11 | Dodomon > Dorimon > Hackmon > BaoHackmon > SaviorHackmon > **Jesmon** | Assassin, Fís. | 5 | 1 / 4 / 0 | — | SaviorHackmon |
| 12 | Nyokimon > Yokomon > Muchomon > Birdramon > Garudamon > **Zephagamon** | Ranged, Esp. | 5 | 2 / 2 / 1 | Muchomon (PA) | — |
| 13 | Jyarimon > Gigimon > Dorumon > DexDorugamon > Dorugreymon > **Dorugoramon** | Tank, Fís. | 5 | 3 / **0** / 2 | — | — |
| 14 | Keemon > Yaamon > Shoemon > Wizardmon > Fantomon > **Piedmon** | Assassin, Esp. | 5 | 0 / 3 / 2 | — | Wizardmon |
| 15 | Chibomon > Koromon > Veemon > Veedramon > AeroVeedramon > **Magnamon** | Warrior, Fís. | 5 | 3 / 2 / 0 | AeroVeedramon (não Ranged) | — |
| 16 | Nyokimon > Yokomon > Wormmon > Kabuterimon > JewelBeemon > **Imperialdramon** | Warrior, Fís. | 5 | 1 / 3 / 1 | — | Kabuterimon |
| 17 | Poyomon > Tokomon > Kudamon > Reppamon > Taomon > **Sakuyamon** | Ranged, Esp. | 5 | 3 / 2 / 0 | Tokomon (PA) | Taomon |
| 18 | Poyomon > Yokomon > Muchomon > Cockatrimon > Delumon > **AncientSphinxmon** | Tank, Fís. | 5 | 3 / 1 / 1 | Poyomon (EA) | — |
| 19 | Jogress ExVeemon (Jyarimon > Koromon > Veemon) + Stingmon (Poyomon > Yokomon > Wormmon) => Paildramon > **Imperialdramon** | Warrior, Fís. | 9 | 2 / 4 / 3 | Poyomon (EA) | — |
| 20 | Jogress WarGreymon (Chibomon > Koromon > Agumon (Savers) > Greymon > RizeGreymon) + MetalGarurumon (Zerimon > Gummymon > Gaomon > Gaogamon > WereGarurumon) => **Omegamon** | Warrior, Fís. | 10 | 5 / 4 / 1 | — | — |

### 2b. Visão das 1.962 cadeias

- **Herdadas no Mega:** 5 em 1.399 cadeias (o caso normal, Baby a Ultimate). As linhas do Belphemon
  Rage Mode têm 6, e as Jogress têm de 7 a 10 (Omegamon, Examon e Paildramon > Mega).
- **Inertes no Mega:**

  | Inertes | Cadeias |
  |---|---|
  | 0 | 1.234 (63%) |
  | 1 | 531 |
  | 2 | 144 |
  | 3 | 50 |
  | 4 | 3 |

  As 3 cadeias com 4 inertes ficam com **1 herdada útil de 5**:
  - Yolkmon > Fluffymon > Pteromon > Galemon > Grand Galemon > **Dynasmon**
  - Yolkmon > Fluffymon > Pteromon > Coredramon (Blue) > AeroVeedramon > **UlforceVeedramon**
  - Yolkmon > Fluffymon > Pteromon > Coredramon (Blue) > AeroVeedramon > **Magnamon**

  Nas três, os dois EA (Yolkmon e Fluffymon) caem num Mega físico, e os dois golpes extras caem num
  Mega que não é Ranged.
- **Sem nenhuma herdada de dano:** 169 cadeias. As mais comuns passam por MegaKabuterimon (34), Fantomon (27), Knightmon (20), Astamon (18) e Triceramon (15).
- **Sem nenhuma de sobrevivência:** 44 cadeias, a maioria via Dorugreymon (12) e Wingdramon (8).

### 2c. Redundâncias e exageros

- **"Ignora dano" empilhado:** negação, esquiva e 1º golpe recebido. Nenhuma cadeia tem 3 ou mais.
  205 cadeias têm 2:
  - **Tortamon + Groundramon**, as duas com negação, que somam: **18% + 20% = 38%** de ignorar o golpe
    inteiro, em 140 cadeias (Breakdramon, Dorugoramon, Examon).
  - **Tortamon + Triceramon** dá 38% (21 cadeias).
  - **Kabuterimon + MegaKabuterimon** e **Kabuterimon + Knightmon**: 22 cadeias cada.
  - **38% de negação é o pico de "ignora dano" do jogo**, e é o candidato mais forte a ficar forte
    demais. Nenhuma das 6 rotas da tarefa 3 passa por ele; se quiser, dá pra medir em seguida.
- **Perfurar defesa:** nenhuma cadeia tem 2 (tarefa 1c).
- **Críticos:** no máximo 2 efeitos de crítico na mesma cadeia (Greymon + RizeGreymon). Sem excesso.
- **"Quase nada de relevante":**
  - as 3 cadeias do Yolkmon com 4 inertes;
  - as 50 com 3 inertes, todas com EA/PA no estágio baixo e golpe extra ou estacas num Mega que não
    é Ranged;
  - a linha **Tortamon > Triceramon > Dinomon** (amostra nº 2): é 100% defensiva, 5 de 5 em
    sobrevivência, sem nada pra causar dano.

### 2d. Passivas que ficam inertes na maioria dos descendentes (estático)

% das cadeias até Mega que passam pela espécie e em que a forma final desliga a passiva:

| Espécie | Efeito | Inerte em | Motivo |
|---|---|---|---|
| **Garurumon** (piloto) | golpe extra à distância | **168/168 (100%)** | nenhum Mega descendente é Ranged |
| **Destromon** (piloto) | estacas → crítico | **57/57 (100%)** | idem |
| **Coredramon (Blue)** (Fase 4) | golpe extra à distância | **70/70 (100%)** | idem |
| **AeroVeedramon** (Fase 4) | golpe extra à distância | **42/42 (100%)** | idem |
| **Gargomon** (Fase 4) | golpe extra à distância | **8/8 (100%)** | idem |
| **Rapidmon** (Fase 4) | golpe extra à distância | **4/4 (100%)** | idem |
| **Galemon** (Fase 4) | golpe extra à distância | 117/126 (93%) | só o Zephagamon é Ranged |
| Terriermon (Fase 3) | +EA | 34/36 (94%) | os Megas da linha são físicos |
| Yolkmon (Fase 2) | +EA | 77/93 (83%) | idem |
| Muchomon (Fase 3) | +PA | 30/40 (75%) | os Megas da linha são especiais |
| Fluffymon (Fase 2) | +EA | 64/96 (67%) | |
| Tsumemon / Tokomon | +EA / +PA | 54% / 53% | |
| LadyDevimon (piloto) | abate | 19/38 (50%) | Lilithmon é Support |
| Grand Galemon (Fase 4) | golpe extra à distância | 9/18 (50%) | Dynasmon é Warrior |
| Delumon (Fase 4) | abaixo de 50%: +dano físico | 22/44 (50%) | Hououmon ataca com Especial |

**Achado principal da Fase 4:** o problema do Garurumon da 1c **se repetiu em 4 espécies novas**
(Coredramon (Blue), AeroVeedramon, Gargomon e Rapidmon), que ficam 100% inertes nas rotas reais, e
quase também no Galemon. A tabela da Fase 4 deu `RangedExtraStrike` a Champions e Ultimates Ranged
cujos Megas não são Ranged. A tarefa 4 confirma em batalha.

---

## 3. Impacto em batalha fora do piloto (6 rotas novas até Ultimate)

O método é o mesmo da 1c calibrada (tarefa 1c dela):
- **Time:** 3 cópias da rota, construídas pela cadeia real (`Evolve`) e levadas ao nível 30.
- **Controle:** Ultimate fixo (Zudomon, Rapidmon e Delumon). O **nível** dele é ajustado por busca
  binária, com 10 lutas por passo, até o mais alto em que a versão SEM ainda vence pelo menos metade.
- **Medição:** depois, 30 lutas COM e 30 SEM nesse nível.
- **SEM** é a mesma cópia (mesmos stats e passiva própria) com o histórico zerado.

Nenhuma das 6 rotas passa por Agumon ou Gabumon, e cada uma termina numa Role diferente. As 4
herdadas de uma Ultimate vão do Baby ao Champion; a passiva da própria Ultimate não conta.

| Rota (forma final) | Herdadas ativas | Controle | COM | SEM | Diferença | Tempo COM / SEM |
|---|---|---|---|---|---|---|
| Dodomon > Dorimon > Hackmon > BaoHackmon > **Knightmon** (Tank, Fís.) | PD 2%, ED 3,5%, HP 3,5%, 1º golpe +9% | nível 58 | 22/30 (73%) | 12/30 (40%) | **+33 p.p.** | 19,4s / 18,4s |
| Dodomon > Gigimon > Elizamon > Devidramon > **WarGrowlmon** (Warrior, Fís.) | PD 2%, PA 4%, HP 4%, crítico encurta debuff | nível 68 | 26/30 (87%) | 9/30 (30%) | **+57 p.p.** | 17,4s / 20,4s |
| Nyokimon > Budmon > Lalamon > Sunflowmon > **Lillymon** (Ranged, Esp.) | Vel 2%, HP 3%, ED 5,5%, Vel 4% + 9% em quem agiu | nível 8 | 29/30 (97%) | 18/30 (60%) | **+37 p.p.** | 41,6s / 48,7s |
| Puttimon > Babydmon > Dracomon > Coredramon (Blue) > **Wingdramon** (Ranged, Fís.) | PD 3,5%, PA 5%, PA 4%, golpe extra 13% | nível 28 | 30/30 (100%) | 24/30 (80%) | **+20 p.p.** | **26,3s / 79,9s** |
| Keemon > Kyaromon > BEMmon > Bakemon > **Fantomon** (Assassin, Esp.) | Vel 3,5%, ED 4,5%, HP 4,5%, esquiva 4% | nível 67 | 21/30 (70%) | 6/30 (20%) | **+50 p.p.** | 12,5s / 13,7s |
| Keemon > Yaamon > Shoemon > ShoeShoemon > **Chaperomon** (Support, Esp.) | Vel 3,5%, HP 3,5%, ED 6%, cura aliado 6% | nível 13 | 19/30 (63%) | 9/30 (30%) | **+33 p.p.** | 43,9s / 43,1s |

**O padrão do piloto se mantém.** O piloto deu de +23 a +60 p.p.; as rotas novas dão de **+20 a +57
p.p.** O WarGrowlmon (+57) fica no topo, mesmo com 3 das 4 herdadas sendo stats pequenos: a luta dele
é parelha e qualquer +4% vira o resultado.

Não há outlier fora da faixa. Duas observações:
- **Wingdramon (+20 p.p., o menor):** a calibragem parou num ponto em que o SEM já vencia 80%, porque
  a virada é brusca e o teto de 30/30 limita a diferença. O sinal está no **tempo**: as lutas SEM levam
  80s em média, com morte súbita aos 120s, contra 26s COM. Com +9% de PA e o golpe extra (13%, que
  aqui funciona porque o Wingdramon é Ranged), o time passa a matar antes da morte súbita. É a rota
  em que a herdada mais muda o *jeito* da luta.
- **Os níveis de controle variam muito:** de 8 (Lillymon) a 68 (WarGrowlmon). Isso vem dos stats
  que cada rota produz, não das passivas: rotas Ranged/Especial chegam fracas no nível 30, como o
  Destromon da 1c.

---

## 4. Disparo das 56 passivas da Fase 4 (10 batalhas cada)

O método é o mesmo da tarefa 2 da 1c:
- **Portador:** o **Mega descendente mais comum** nas cadeias reais daquela espécie (empate: menor
  id), nível 45, com **só aquela espécie** no histórico.
- **Time:** o portador + Craniummon (Tank) + Sakuyamon (Ranged), contra Vikemon, Kentaurusmon e
  Magnamon.
- **"Disparou":** o efeito aconteceu pelo menos uma vez na luta, lido do estado público a cada quadro.
- **Golpe extra:** contado envolvendo o sorteio, sem mudar o resultado. Só o portador tem herdadas,
  então todo sorteio de `RollExtraStrike` é dele.
- **Eventos:** quantas vezes aconteceu nas 10 lutas, quando dá pra contar.

O controle Mega perdeu quase todas as lutas (o time do portador venceu de 8/10 a 10/10). Por isso as
lutas são curtas, e os efeitos raros têm menos chance de aparecer, como na 1c.

| Gatilho | Espécie (estágio) | Efeito | Portador (Role, ataque) | Disparou | Eventos |
|---|---|---|---|---|---|
| **Stat puro** | Geogreymon (C) | +6% PA | WarGreymon (Warrior, Fís.) | 10/10 | |
| | Coredramon Green (C) | +6,5% PA | Breakdramon (Warrior, Fís.) | 10/10 | |
| | ExVeemon (C) | +7% PA | UlforceVeedramon (Assassin, Fís.) | 10/10 | |
| | Dorugreymon (U) | +10% PA | Alphamon (Assassin, Fís.) | 10/10 | |
| | Ikkakumon (C) | +6% PD | Vikemon (Tank, Fís.) | 9/10 | |
| | Zudomon (U) | +12% PD | Vikemon (Tank, Fís.) | 10/10 | |
| | Dorugamon (C) | +6% Vel | Dorugoramon (Tank, Fís.) | 10/10 | |
| **Ignorar dano** | Kabuterimon (C) | 15% | HerculesKabuterimon (Tank) | 10/10 | |
| | Tortamon (C) | 18% | AncientSphinxmon (Tank) | 10/10 | |
| | Groundramon (U) | 20% | Breakdramon (Warrior) | 9/10 | |
| | MegaKabuterimon (U) | 22% | HerculesKabuterimon (Tank) | 9/10 | |
| | Grademon (U) | 24% | Alphamon (Assassin) | 7/10 | |
| **1º golpe recebido** | Angemon (C) | −25% | Magnadramon (**Ranged**) | **5/10** | apanhou 9 vezes em 10 lutas |
| | Knightmon (U) | −30% | Crusadermon (Warrior) | 10/10 | |
| **Perfurar defesa** | Togemon (C) | a cada 4, 15% | Rosemon (Ranged, Esp.) | 10/10 | 13 |
| | Gladimon (C) | a cada 4, 18% | Crusadermon (Warrior) | 10/10 | 25 |
| | DexDorugamon (C) | a cada 3, 22% | Dorugoramon (Tank) | 10/10 | 22 |
| | MagnaAngemon (U) | a cada 3, 25% | Seraphimon (Ranged, Esp.) | 10/10 | 20 |
| | JewelBeemon (U) | a cada 3, 24% | HerculesKabuterimon (Tank, Esp.) | 10/10 | 34 |
| **Abaixo de 50%: +dano físico** | Growlmon (C) | +12% | WarGreymon (Warrior, Fís.) | 8/10 | |
| | Veedramon (C) | +10% | SlayerDramon (Assassin, Fís.) | 8/10 | |
| | WarGrowlmon (U) | +16% | Gallantmon (Warrior, Fís.) | **1/10** | apanhou 17 vezes em 10 lutas |
| | Delumon (U) | +14% | Hououmon (Ranged, **Esp.**) | **0/10** | **inerte: forma especial** |
| **Golpe extra à distância** | Gargomon (C) | 12% | MegaGargomon (**Tank**) | **0/10** | **0, inerte** |
| | Galemon (C) | 10% | Dynasmon (**Warrior**) | **0/10** | **0, inerte** |
| | Coredramon Blue (C) | 13% | SlayerDramon (**Assassin**) | **0/10** | **0, inerte** |
| | Rapidmon (U) | 18% | MegaGargomon (**Tank**) | **0/10** | **0, inerte** |
| | AeroVeedramon (U) | 16% | UlforceVeedramon (**Assassin**) | **0/10** | **0, inerte** |
| | Grand Galemon (U) | 15% | Zephagamon (Ranged) | 6/10 | 10 golpes extras |
| **1º golpe desferido** | Kyubimon (C) | +8% | Barbamon | 10/10 | |
| | Reppamon (C) | +7% | Kentaurusmon | 10/10 | |
| | BaoHackmon (C) | +9% | Crusadermon | 10/10 | |
| | Garudamon (U) | +10% | Hououmon | 10/10 | |
| | Wingdramon (U) | +11% | SlayerDramon | 10/10 | |
| **Vel + bônus em quem agiu** | Gaogamon (C) | 4% / 6% | Gankoomon | 10/10 | |
| | Sunflowmon (C) | 4% / 9% | Rosemon | 10/10 | |
| | Raptordramon (C) | 5% / 7% | Alphamon | 10/10 | |
| | Chaperomon (U) | 6% / 12% | Pinochimon | 10/10 | |
| **Esquiva → crítico** | Wizardmon (C) | 5% | Piedmon (Assassin) | **2/10** | |
| | Bakemon (C) | 4% | Piedmon (Assassin) | **1/10** | |
| | Taomon (U) | 7% | Duftmon (Assassin) | **3/10** | |
| | SaviorHackmon (U) | 8% | Gankoomon (Warrior) | **3/10** | |
| **Crítico drena** | Birdramon (C) | 5% | Hououmon | 8/10 | 11 críticos |
| | Snatchmon (C) | 4% | Demon (Assassin, Esp.) | **1/10** | **1 crítico em 96 ataques** (ver nota) |
| | Lillymon (U) | 7% | Rosemon | 6/10 | 10 |
| | DexDorugreymon (U) | 8% | Dorugoramon | 7/10 | 12 |
| **Crítico encurta debuff** | Devidramon (C) | 1 ação | Gallantmon (Warrior) | **0/10** | |
| **Cura aliado** | ShoeShoemon (C) | 6% | Pinochimon | 10/10 | 94 curas |
| | Angewomon (U) | 9% | Magnadramon | 10/10 | 94 |
| | Lilamon (U) | 11% | Rosemon | 10/10 | 92 |
| **Abate: +Vel** | Stingmon (C) | 9% | UlforceVeedramon | 4/10 | 4 abates |
| | Paildramon (U) | 10% | UlforceVeedramon | 3/10 | 4 |
| | Fantomon (U) | 12% | Boltmon (Tank) | 4/10 | 6 |
| | Mistymon (U) | 14% | Barbamon (Ranged) | 5/10 | 7 |
| **Atordoar** | Cockatrimon (C) | 8% / 16% | Hououmon | 7/10 | 9 atordoamentos |
| | Chirinmon (U) | 12% / 22% | Kentaurusmon | 10/10 | 16 |

### Muito abaixo das outras do mesmo gatilho

1. **Golpe extra à distância: 5 de 6 espécies novas em 0/10.** Gargomon, Galemon, Coredramon (Blue),
   Rapidmon e AeroVeedramon caem num Mega que não é Ranged. Pela tarefa 2d, isso vale para **100%**
   dos descendentes deles (Galemon 93%). Só o Grand Galemon funciona (6/10), quando vira Zephagamon.
   **É exatamente o problema do Garurumon da 1c, agora em mais 5 espécies.** Somando o Garurumon,
   são 6 das 7 espécies com `RangedExtraStrike` no jogo. Esse é o principal ajuste a considerar
   antes de jogar.
2. **Delumon, 0/10:** "+dano físico abaixo de 50%" num descendente que ataca com Especial (Hououmon).
   O outro descendente, AncientSphinxmon, é físico; 50% das cadeias são inertes (tarefa 2d).
3. **WarGrowlmon, 1/10:** o gatilho funciona (Growlmon e Veedramon dão 8/10), mas o Gallantmon apanha
   pouco nessas lutas e quase nunca cai abaixo de 50%. É o mesmo comportamento do MetalGreymon no
   piloto, só que menos frequente. Não é defeito de Role.
4. **Devidramon, 0/10:** o mesmo caso do Greymon do piloto (0/10). Precisa de um crítico enquanto há
   um debuff ativo, e só o Alvo Marcado do controle aplica debuff.
5. **Esquiva → crítico (1 a 3 de 10):** o mesmo caso da Lamiamon do piloto (1/10). De 4% a 8% de
   esquiva por golpe recebido, em lutas curtas, num portador que apanha pouco. Funciona, mas quase
   não se sente.
6. **Angemon, 5/10:** o portador natural (Magnadramon) é Ranged e fica atrás. Apanhou só 9 vezes em
   10 lutas, como o IceDevimon da 1c (7/10).
7. **Snatchmon, 1/10:** o crítico drena num Demon que registrou só **1 crítico em 96 ataques**,
   contra cerca de 10 por 90 ataques nos outros portadores. É provável que seja limitação do método,
   não da passiva: se o golpe do Demon acerta vários alvos na mesma ação, o `LastAttackCrit` é
   reescrito a cada alvo e a leitura por quadro perde os críticos. Não investiguei mais, porque seria
   preciso olhar a lógica do Demon. Fica como ponto a conferir.

**Os gatilhos de abate (Stingmon, Paildramon, Fantomon e Mistymon) dão de 3 a 5 de 10.** Não é
outlier: com dois aliados fortes, o golpe final costuma ser de outro. Mesmo assim, é melhor que a
LadyDevimon da 1c, que ficou em 0/10 por ter filha Support.

---

## Resumo para decidir antes de jogar

1. **Consistência dos dados está limpa:**
   - 148/148 espécies Baby–Ultimate com passiva;
   - 0 valores fora de faixa;
   - **0 colisões de efeitos de 1ª ocorrência** em 1.962 cadeias, Jogress incluídos.
2. **Força em batalha confirmada fora do piloto.** As 6 rotas novas dão de +20 a +57 p.p., na mesma
   faixa do piloto (+23 a +60). As herdadas são fortes, mas sem outlier.
3. **O problema mais claro é o golpe extra à distância (`RangedExtraStrike`).** 6 das 7 espécies que
   o têm ficam 100% inertes nas rotas reais, porque nenhum Mega descendente é Ranged: Garurumon,
   Gargomon, Galemon, Coredramon (Blue), Rapidmon e AeroVeedramon. Na ficha aparece, mas nunca
   dispara.
4. **Também inertes por tipo de ataque:**
   - **Delumon:** 50% dos descendentes.
   - **EA ou PA nos estágios baixos que caem num Mega do tipo oposto:** Terriermon 94%, Yolkmon 83%,
     Muchomon 75% e Fluffymon 67%.
   - **Pior caso:** cadeias do Yolkmon com **4 de 5** herdadas inertes.
5. **Candidatos a forte demais:**
   - **Tortamon + Groundramon/Triceramon:** 38% de chance de ignorar o golpe inteiro, em 161 cadeias.
   - **Gomamon > Ikkakumon > Zudomon > Vikemon:** +23,5% de PD.
   - **Belphemon Rage Mode:** até +22% de ED.

   Nada passa de +25% num stat.
6. **Raras demais pra se sentir:** as mesmas do piloto, agora com mais espécies. Crítico encurta
   debuff (Devidramon 0/10), esquiva → crítico (1 a 3 de 10) e WarGrowlmon (1/10).

## Observações de execução

- **Save real intacto:** o SHA-256 de `save.json` (`cf3d41c8…`) e do `.bak` (`327ba440…`) é idêntico
  ao do backup antes e depois.
- **Log:** nenhuma exceção ou erro nas cerca de 1.400 batalhas.
- **Repositório:** nenhum arquivo de código ou dado mudou (`git status` sem `M`). Novos e fora do
  git: este relatório e a ferramenta `passivas/ferramentas/InheritedRaioXTool.{cs,tscn}`, que é
  compilada junto com o projeto como a da 1c, mas não é usada por nenhuma cena do jogo.
