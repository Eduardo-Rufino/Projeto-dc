# Passivas herdadas — Fase 4: Champion e Ultimate restantes (lote 3)

Data: 2026-10-20 · Branch: `passivas-herdadas` (sobre `aa92d5a`, Fase 3)

Só dado: o campo `InheritedPassive` foi preenchido nos JSONs de 56 espécies. Nenhum `EffectType` novo foi criado e nenhuma lógica de jogo mudou. O `GameAudit.cs` só ganhou testes.

## Cobertura

- **Champion: 35/35** (30 novas + 5 do piloto).
- **Ultimate: 38/38** (26 novas + 12 do piloto).
- Com Baby, In-Training e Rookie, são 148 espécies cobertas. Ficam faltando as Mega (48) e as MegaPlus (2).

## Correções aplicadas sobre a proposta

1. **Paildramon (192):** ficou só com o `KillSpeedBonus` 0.10. O formato de dado aceita um efeito por espécie, e você escolheu a opção (c), sem código novo.
2. **Sunflowmon (24) e Chaperomon (93):** o `SpeedAndBonusVsActedTarget` dá Velocidade, não ED, e você aceitou a Velocidade. Os valores ficaram como na tabela: 0.04/0.09 e 0.06/0.12.
   - Na mensagem das correções o Sunflowmon aparecia como 0.04/0.06.
   - Com 0.04/0.06 ele ficaria idêntico ao Gaogamon, então tratei como erro de digitação e mantive o 0.09 da tabela.
   - Se o certo for 0.06, é só trocar o `Value2` no JSON dele e o valor esperado no teste.
3. **Devidramon (47):** saiu do `KillSpeedBonus` 0.08 e ficou com `CritShortensDebuffs` 1, igual ao Greymon. Agora o Stingmon é o único `KillSpeedBonus` de Champion.

## Fase 4b: colisão de perfurar defesa corrigida

O `PeriodicDefensePierce` só usa a primeira espécie daquele tipo no histórico (`FirstOrDefault`). Em duas linhas, a segunda espécie com perfurar defesa ficava sem efeito pra quem evoluía além dela. Troquei a passiva da Ultimate de cada par:

| Id | Espécie | Antes | Depois | Por quê |
|---|---|---|---|---|
| 147 | Knightmon | Perfurar defesa: a cada 3 ataques, ignora 20% | 1º golpe recebido -30% (`FirstHitTakenReduction`) | Tema cavaleiro/armadura. Nenhuma cadeia do Knightmon tem esse efeito. Valor acima dos Champion (IceDevimon 20%, Angemon 25%). |
| 157 | DexDorugreymon | Perfurar defesa: a cada 3 ataques, ignora 28% | Crítico drena 8% do dano (`CritLifesteal`) | Tema "devora dados". Nenhuma cadeia dele tem esse efeito. Valor acima dos outros (4% a 7%). |

Os dois valores novos não repetem Type+valores de nenhuma outra espécie do jogo.

**Cadeias conferidas.** Em todos os caminhos de evolução até as Megas, nenhuma das duas linhas tem mais o mesmo efeito duas vezes:
- **Knightmon:** 50 caminhos. Ele vem de Gladimon, BaoHackmon ou Kabuterimon, e vai pra Crusadermon ou Craniummon.
- **DexDorugreymon:** 20 caminhos. Ele vem de DexDorugamon ou Dorugamon, e vai pra Dorugoramon ou DexDorugoramon.

Exemplos:
- Botamon > Wanyamon > Kotemon > Gladimon [perfurar defesa] > Knightmon [1º golpe -30%] > Crusadermon
- Dodomon > Gigimon > Dorumon > DexDorugamon [perfurar defesa] > DexDorugreymon [crítico drena 8%] > Dorugoramon

**No jogo todo.** Varri todas as cadeias, Jogress incluídos, atrás dos três efeitos que só usam a primeira ocorrência (`PeriodicDefensePierce`, `StunChance` e `RangedStacksGuaranteedCrit`). Não sobrou nenhuma colisão.

**O que continua repetindo (e não é colisão).** Muitas cadeias têm o mesmo stat percentual duas vezes. Por exemplo, Tsunomon (PD 4%) e Kotemon (PD 4,5%), ou Dorumon (Velocidade 4,5%) e Dorugamon (Velocidade 6%). Esses efeitos **somam** (`Sum`), então nenhum fica sem efeito. O mesmo vale pros outros efeitos de soma, como o abate do Stingmon com o do Paildramon.

## Fase 4d: golpe extra à distância inerte em 5 espécies

O `RangedExtraStrike` só dispara se a forma ATUAL for Ranged. A Fase 4c mostrou que estas 5 espécies são Ranged, mas nenhum (ou quase nenhum) Mega descendente delas é: a passiva aparecia na ficha e nunca disparava. Troquei por gatilhos já existentes, sem lógica nova. Garurumon e Destromon (piloto) ficam como estão, por decisão sua.

| Id | Espécie | Antes | Depois | Tema |
|---|---|---|---|---|
| 19 | Gargomon | Golpe extra 12% (30% do dano) | +6% dano físico, 12% no 1º golpe (`PhysicalDamageWithOpeningBonus`) | rajada de metralhadora que abre a luta |
| 79 | Galemon | Golpe extra 10% (30% do dano) | +5% Velocidade; +9% dano em quem já agiu (`SpeedAndBonusVsActedTarget`) | tufão: velocidade |
| 136 | Coredramon (Blue) | Golpe extra 13% (30% do dano) | +7% dano físico, 14% no 1º golpe (`PhysicalDamageWithOpeningBonus`) | sopro do dragão do céu |
| 42 | Rapidmon | Golpe extra 18% (30% do dano) | +6% Velocidade; +10% dano em quem já agiu (`SpeedAndBonusVsActedTarget`) | mísseis teleguiados / rapidez |
| 141 | AeroVeedramon | Golpe extra 16% (30% do dano) | +12% dano no 1º golpe (`OpeningHitBonus`) | mergulho com a onda em V |

**Por que esses gatilhos.** Todos os Megas descendentes dessas 5 atacam com Físico, exceto o Zephagamon (Especial), que é descendente do Galemon. Então:
- **O dano físico com bônus de abertura** não fica inerte no Gargomon nem no Coredramon (Blue).
- **O Galemon ficou com Velocidade + bônus**, que não depende de Role nem de tipo de ataque.
- **O `OpeningHitBonus` só serviu no AeroVeedramon.** Ele já existe na linha do Gargomon (MachGaogamon), na do Galemon e na do Coredramon (Blue) (Wingdramon), e repetir somaria o mesmo gatilho duas vezes. Na linha do AeroVeedramon ele não aparece.
- **Gargomon e Rapidmon** estão na mesma linha e ficaram com tipos diferentes; o mesmo vale pra Coredramon (Blue) e AeroVeedramon.

**Valores.** Nenhum repete Type+Value+Value2 de outra espécie do jogo. Os que já existiam:
- `PhysicalDamageWithOpeningBonus`: WereGarurumon 8%/16%.
- `SpeedAndBonusVsActedTarget`: de 4%/6% a 6%/12%.
- `OpeningHitBonus`: de 6% a 11%. A Ultimate ficou com 12%, acima do Wingdramon.

**Inércia (cadeias reais até Mega em que a passiva fica sem efeito pela Role ou pelo tipo de ataque da forma final):**

| Espécie | Cadeias até Mega | Antes | Depois |
|---|---|---|---|
| Gargomon | 8 | 8 (100%) | **0 (0%)** |
| Galemon | 126 | 117 (93%) | **0 (0%)** |
| Coredramon (Blue) | 70 | 70 (100%) | **0 (0%)** |
| Rapidmon | 4 | 4 (100%) | **0 (0%)** |
| AeroVeedramon | 42 | 42 (100%) | **0 (0%)** |

**Cadeias.** Varri de novo todas as cadeias, normais e de Jogress (1.962 até a forma final):
- **Colisões de 1ª ocorrência** (`PeriodicDefensePierce`, `StunChance`, `RangedStacksGuaranteedCrit`): continuam 0.
- **Nenhuma cadeia que passa por uma das 5** tem o novo Type duas vezes.
- **O golpe extra repetido na mesma cadeia** caiu de 32 cadeias (ex.: Coredramon (Blue) > AeroVeedramon) para 0.
- **No jogo, o `RangedExtraStrike` ficou só no Garurumon (piloto) e no Grand Galemon**, que funciona quando vira Zephagamon (Ranged).

## Champion

| Id | Espécie | Role | Ataque | Efeito | Valores no JSON | Bate com a proposta? | Teste |
|---|---|---|---|---|---|---|---|
| 15 | Kabuterimon | Tank | Especial | Chance de anular o golpe (`NegateDamageChance`) | 15% | sim | sim |
| 16 | Angemon | Ranged | Especial | 1º golpe recebido reduzido (`FirstHitTakenReduction`) | 25% | sim | sim |
| 17 | Togemon | Warrior | Físico | Perfurar defesa a cada N (`PeriodicDefensePierce`) | a cada 4 ataques, ignora 15% | sim | sim |
| 18 | Growlmon | Warrior | Físico | HP baixo: +dano físico (`LowHpPhysicalDamageBonus`) | abaixo de 50% HP: +12% | sim | sim |
| 19 | Gargomon | Ranged | Físico | Dano físico + bônus no 1º golpe (`PhysicalDamageWithOpeningBonus`) | +6% dano físico (12% no 1º golpe) | trocado na Fase 4d | sim |
| 20 | Kyubimon | Ranged | Especial | Bônus no 1º golpe (`OpeningHitBonus`) | 8% | sim |  |
| 21 | Wizardmon | Ranged | Especial | Esquiva e crítico garantido (`DodgeThenGuaranteedCrit`) | 5% | sim | sim |
| 23 | Gaogamon | Warrior | Físico | Velocidade + dano em quem já agiu (`SpeedAndBonusVsActedTarget`) | +4% Vel.; +6% dano | sim |  |
| 24 | Sunflowmon | Support | Especial | Velocidade + dano em quem já agiu (`SpeedAndBonusVsActedTarget`) | +4% Vel.; +9% dano | sim; o Value é Velocidade, não ED (correção 2) | sim |
| 44 | Geogreymon | Warrior | Físico | Stat puro: PA (`PhysicalAttackPercent`) | 6% | sim | sim |
| 47 | Devidramon | Assassin | Físico | Crítico encurta debuffs (`CritShortensDebuffs`) | 1 ação | sim, com a correção | sim |
| 60 | Birdramon | Ranged | Especial | Crítico drena vida (`CritLifesteal`) | 5% | sim |  |
| 66 | Ikkakumon | Tank | Físico | Stat puro: PD (`PhysicalDefensePercent`) | 6% | sim |  |
| 79 | Galemon | Ranged | Especial | Velocidade + dano em quem já agiu (`SpeedAndBonusVsActedTarget`) | +5% Vel.; +9% dano | trocado na Fase 4d | sim |
| 92 | ShoeShoemon | Support | Especial | Ataque cura o aliado mais ferido (`HealLowestAllyOnAttack`) | 6% | sim |  |
| 97 | Cockatrimon | Warrior | Físico | Chance de atordoar (`StunChance`) | 8% (16% em alvo < 50%) | sim |  |
| 103 | Bakemon | Assassin | Especial | Esquiva e crítico garantido (`DodgeThenGuaranteedCrit`) | 4% | sim |  |
| 110 | Tortamon | Tank | Físico | Chance de anular o golpe (`NegateDamageChance`) | 18% | sim |  |
| 117 | Snatchmon | Ranged | Especial | Crítico drena vida (`CritLifesteal`) | 4% | sim |  |
| 130 | Reppamon | Ranged | Especial | Bônus no 1º golpe (`OpeningHitBonus`) | 7% | sim |  |
| 133 | Coredramon (Green) | Warrior | Físico | Stat puro: PA (`PhysicalAttackPercent`) | 6,5% | sim |  |
| 136 | Coredramon (Blue) | Ranged | Físico | Dano físico + bônus no 1º golpe (`PhysicalDamageWithOpeningBonus`) | +7% dano físico (14% no 1º golpe) | trocado na Fase 4d | sim |
| 140 | Veedramon | Warrior | Físico | HP baixo: +dano físico (`LowHpPhysicalDamageBonus`) | abaixo de 50% HP: +10% | sim |  |
| 146 | Gladimon | Warrior | Físico | Perfurar defesa a cada N (`PeriodicDefensePierce`) | a cada 4 ataques, ignora 18% | sim |  |
| 150 | Dorugamon | Warrior | Físico | Stat puro: Velocidade (`SpeedPercent`) | 6% | sim | sim |
| 153 | Raptordramon | Warrior | Físico | Velocidade + dano em quem já agiu (`SpeedAndBonusVsActedTarget`) | +5% Vel.; +7% dano | sim |  |
| 156 | DexDorugamon | Assassin | Físico | Perfurar defesa a cada N (`PeriodicDefensePierce`) | a cada 3 ataques, ignora 22% | sim |  |
| 164 | BaoHackmon | Assassin | Físico | Bônus no 1º golpe (`OpeningHitBonus`) | 9% | sim |  |
| 191 | ExVeemon | Warrior | Físico | Stat puro: PA (`PhysicalAttackPercent`) | 7% | sim |  |
| 198 | Stingmon | Assassin | Físico | Abate: +Velocidade (`KillSpeedBonus`) | 9% | sim | sim |

## Ultimate

| Id | Espécie | Role | Ataque | Efeito | Valores no JSON | Bate com a proposta? | Teste |
|---|---|---|---|---|---|---|---|
| 41 | WarGrowlmon | Warrior | Físico | HP baixo: +dano físico (`LowHpPhysicalDamageBonus`) | abaixo de 50% HP: +16% | sim |  |
| 42 | Rapidmon | Ranged | Físico | Velocidade + dano em quem já agiu (`SpeedAndBonusVsActedTarget`) | +6% Vel.; +10% dano | trocado na Fase 4d | sim |
| 43 | MagnaAngemon | Ranged | Especial | Perfurar defesa a cada N (`PeriodicDefensePierce`) | a cada 3 ataques, ignora 25% | sim |  |
| 49 | Taomon | Ranged | Especial | Esquiva e crítico garantido (`DodgeThenGuaranteedCrit`) | 7% | sim |  |
| 50 | Lillymon | Ranged | Especial | Crítico drena vida (`CritLifesteal`) | 7% | sim | sim |
| 51 | MegaKabuterimon | Tank | Especial | Chance de anular o golpe (`NegateDamageChance`) | 22% | sim |  |
| 61 | Garudamon | Ranged | Especial | Bônus no 1º golpe (`OpeningHitBonus`) | 10% | sim | sim |
| 67 | Zudomon | Tank | Físico | Stat puro: PD (`PhysicalDefensePercent`) | 12% | sim | sim |
| 72 | Angewomon | Ranged | Especial | Ataque cura o aliado mais ferido (`HealLowestAllyOnAttack`) | 9% | sim | sim |
| 80 | Grand Galemon | Ranged | Especial | Golpe extra à distância (`RangedExtraStrike`) | 15% de chance, 30% do dano | sim | sim (Fase 4d) |
| 93 | Chaperomon | Support | Especial | Velocidade + dano em quem já agiu (`SpeedAndBonusVsActedTarget`) | +6% Vel.; +12% dano | sim; o Value é Velocidade, não ED (correção 2) | sim |
| 98 | Delumon | Warrior | Físico | HP baixo: +dano físico (`LowHpPhysicalDamageBonus`) | abaixo de 50% HP: +14% | sim |  |
| 104 | Fantomon | Assassin | Especial | Abate: +Velocidade (`KillSpeedBonus`) | 12% | sim |  |
| 131 | Chirinmon | Ranged | Especial | Chance de atordoar (`StunChance`) | 12% (22% em alvo < 50%) | sim | sim |
| 134 | Groundramon | Tank | Físico | Chance de anular o golpe (`NegateDamageChance`) | 20% | sim |  |
| 137 | Wingdramon | Ranged | Físico | Bônus no 1º golpe (`OpeningHitBonus`) | 11% | sim |  |
| 141 | AeroVeedramon | Ranged | Físico | Bônus no 1º golpe (`OpeningHitBonus`) | 12% | trocado na Fase 4d | sim |
| 147 | Knightmon | Tank | Físico | 1º golpe recebido reduzido (`FirstHitTakenReduction`) | 30% | trocado na Fase 4b | sim |
| 151 | Dorugreymon | Warrior | Físico | Stat puro: PA (`PhysicalAttackPercent`) | 10% | sim |  |
| 154 | Grademon | Tank | Físico | Chance de anular o golpe (`NegateDamageChance`) | 24% | sim |  |
| 157 | DexDorugreymon | Assassin | Físico | Crítico drena vida (`CritLifesteal`) | 8% | trocado na Fase 4b | sim |
| 165 | SaviorHackmon | Assassin | Físico | Esquiva e crítico garantido (`DodgeThenGuaranteedCrit`) | 8% | sim |  |
| 170 | Mistymon | Assassin | Físico | Abate: +Velocidade (`KillSpeedBonus`) | 14% | sim |  |
| 179 | Lilamon | Support | Especial | Ataque cura o aliado mais ferido (`HealLowestAllyOnAttack`) | 11% | sim |  |
| 192 | Paildramon | Ranged | Físico | Abate: +Velocidade (`KillSpeedBonus`) | 10% | sim, com a correção | sim |
| 199 | JewelBeemon | Assassin | Físico | Perfurar defesa a cada N (`PeriodicDefensePierce`) | a cada 3 ataques, ignora 24% | sim |  |

## Efeitos usados no lote

| Efeito | Espécies |
|---|---|
| Bônus no 1º golpe (`OpeningHitBonus`) | 6 |
| Velocidade + dano em quem já agiu (`SpeedAndBonusVsActedTarget`) | 6 |
| Chance de anular o golpe (`NegateDamageChance`) | 5 |
| Perfurar defesa a cada N (`PeriodicDefensePierce`) | 5 |
| HP baixo: +dano físico (`LowHpPhysicalDamageBonus`) | 4 |
| Esquiva e crítico garantido (`DodgeThenGuaranteedCrit`) | 4 |
| Dano físico + bônus no 1º golpe (`PhysicalDamageWithOpeningBonus`) | 2 |
| Golpe extra à distância (`RangedExtraStrike`) | 1 |
| Stat puro: PA (`PhysicalAttackPercent`) | 4 |
| Crítico drena vida (`CritLifesteal`) | 4 |
| Abate: +Velocidade (`KillSpeedBonus`) | 4 |
| Ataque cura o aliado mais ferido (`HealLowestAllyOnAttack`) | 3 |
| 1º golpe recebido reduzido (`FirstHitTakenReduction`) | 2 |
| Stat puro: PD (`PhysicalDefensePercent`) | 2 |
| Chance de atordoar (`StunChance`) | 2 |
| Crítico encurta debuffs (`CritShortensDebuffs`) | 1 |
| Stat puro: Velocidade (`SpeedPercent`) | 1 |

## Observações (comportamento do código que já existia, não mudou)

- **O teto de +40% do perfurar defesa (Fase 1d) vale pra todos.** Com defesa alta, o ganho dos valores maiores (MagnaAngemon 25%, JewelBeemon 24%) é cortado pelo teto.
- **Os demais efeitos somam** (`Sum`), inclusive o `KillSpeedBonus`. Um Digimon que vem do Jogress ExVeemon + Stingmon → Paildramon:
  - Velocidade por abate: 9% + 10% = 19%.
  - PA: os 7% do ExVeemon.
  - Isso está testado.

## Testes (GameAudit, seção `inherited`)

Cada teste confirma que o efeito dispara com a espécie no histórico e não dispara fora da condição. A forma atual é uma Mega sem herdada (id 95), pra só a espécie testada contar.

Amostra de 25 espécies: 19 da Fase 4, o Knightmon da Fase 4b e, na Fase 4d, as 5 trocadas mais o Grand Galemon (que passou a cobrir o golpe extra). O Rapidmon deixou de testar o golpe extra e passou a testar o efeito novo.

**Os 13 gatilhos:**
- `NegateDamageChance`: Kabuterimon.
- `FirstHitTakenReduction`: Angemon e Knightmon (Fase 4b).
- `PeriodicDefensePierce`: Togemon (a cada 4).
- `LowHpPhysicalDamageBonus`: Growlmon.
- `RangedExtraStrike`: Grand Galemon (era o Rapidmon até a Fase 4d).
- `OpeningHitBonus`: Garudamon e AeroVeedramon (Fase 4d).
- `DodgeThenGuaranteedCrit`: Wizardmon.
- `SpeedAndBonusVsActedTarget`: Sunflowmon, Chaperomon, Galemon e Rapidmon (Fase 4d), checando também que a ED não muda.
- `PhysicalDamageWithOpeningBonus`: Gargomon e Coredramon (Blue) (Fase 4d), checando que ataque especial não ganha nada.
- `CritShortensDebuffs`: Devidramon.
- `KillSpeedBonus`: Stingmon.
- `CritLifesteal`: Lillymon e DexDorugreymon (Fase 4b).
- `HealLowestAllyOnAttack`: Angewomon.
- `StunChance`: Chirinmon.

**Stat puro:** Geogreymon (PA), Zudomon (PD), Dorugamon (Velocidade).

**Paildramon isolado:** o abate dá +10% de Velocidade, e sozinho ele não mexe no PA. À parte, o histórico ExVeemon + Stingmon + Paildramon dá Velocidade 1190 depois de um abate e PA 1070.

Também checa que nas duas linhas corrigidas (Gladimon + Knightmon e DexDorugamon + DexDorugreymon) só resta um perfurar defesa, e (Fase 4d) que Gargomon > Rapidmon e Coredramon (Blue) > AeroVeedramon não herdam mais nenhum golpe extra, e Galemon > Grand Galemon só o do Grand Galemon.

### Resultado (depois da Fase 4b)

- **Build:** 0 erros. Os 9 avisos de compilação são os de antes.
- **GameAudit completo:** 49111 checagens, 0 falhas, 7 avisos. São os 7 avisos antigos: 6 campeonatos fortes demais em `flows` e 3 avisos num dia em `grandevent`.
- **Seção `inherited`:** 0 falhas e 71 linhas de passiva ok.
  - O teste antigo do DexDorugreymon virou o de roubo de vida.
  - Entrou o do Knightmon.
  - A checagem de um único perfurar defesa nas duas linhas corrigidas passou.
- **Save real:** o SHA-256 de `save.json` e de `save.json.bak` é o mesmo do backup feito antes da Fase 4.

### Resultado (depois da Fase 4d)

- **Build:** 0 erros. Os 9 avisos de compilação são os de antes.
- **GameAudit completo:** 48889 checagens, 0 falhas e os mesmos 7 avisos antigos. O total muda de uma execução pra outra por causa das batalhas aleatórias: nas 7 execuções desta branch foi de 48146 a 49111.
- **Seção `inherited`:** 0 falhas e 76 linhas de passiva ok (eram 71).
  - Entraram Gargomon, Galemon, Coredramon (Blue) e AeroVeedramon, e o Grand Galemon assumiu o teste do golpe extra.
  - O Rapidmon passou a testar a Velocidade + bônus.
  - A checagem de que as linhas Gargomon > Rapidmon e Coredramon (Blue) > AeroVeedramon não herdam golpe extra passou.
- **Save real:** o SHA-256 de `save.json` e de `save.json.bak` é igual ao do backup feito antes.
