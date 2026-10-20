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

## Champion

| Id | Espécie | Role | Ataque | Efeito | Valores no JSON | Bate com a proposta? | Teste |
|---|---|---|---|---|---|---|---|
| 15 | Kabuterimon | Tank | Especial | Chance de anular o golpe (`NegateDamageChance`) | 15% | sim | sim |
| 16 | Angemon | Ranged | Especial | 1º golpe recebido reduzido (`FirstHitTakenReduction`) | 25% | sim | sim |
| 17 | Togemon | Warrior | Físico | Perfurar defesa a cada N (`PeriodicDefensePierce`) | a cada 4 ataques, ignora 15% | sim | sim |
| 18 | Growlmon | Warrior | Físico | HP baixo: +dano físico (`LowHpPhysicalDamageBonus`) | abaixo de 50% HP: +12% | sim | sim |
| 19 | Gargomon | Ranged | Físico | Golpe extra à distância (`RangedExtraStrike`) | 12% de chance, 30% do dano | sim |  |
| 20 | Kyubimon | Ranged | Especial | Bônus no 1º golpe (`OpeningHitBonus`) | 8% | sim |  |
| 21 | Wizardmon | Ranged | Especial | Esquiva e crítico garantido (`DodgeThenGuaranteedCrit`) | 5% | sim | sim |
| 23 | Gaogamon | Warrior | Físico | Velocidade + dano em quem já agiu (`SpeedAndBonusVsActedTarget`) | +4% Vel.; +6% dano | sim |  |
| 24 | Sunflowmon | Support | Especial | Velocidade + dano em quem já agiu (`SpeedAndBonusVsActedTarget`) | +4% Vel.; +9% dano | sim; o Value é Velocidade, não ED (correção 2) | sim |
| 44 | Geogreymon | Warrior | Físico | Stat puro: PA (`PhysicalAttackPercent`) | 6% | sim | sim |
| 47 | Devidramon | Assassin | Físico | Crítico encurta debuffs (`CritShortensDebuffs`) | 1 ação | sim, com a correção | sim |
| 60 | Birdramon | Ranged | Especial | Crítico drena vida (`CritLifesteal`) | 5% | sim |  |
| 66 | Ikkakumon | Tank | Físico | Stat puro: PD (`PhysicalDefensePercent`) | 6% | sim |  |
| 79 | Galemon | Ranged | Especial | Golpe extra à distância (`RangedExtraStrike`) | 10% de chance, 30% do dano | sim |  |
| 92 | ShoeShoemon | Support | Especial | Ataque cura o aliado mais ferido (`HealLowestAllyOnAttack`) | 6% | sim |  |
| 97 | Cockatrimon | Warrior | Físico | Chance de atordoar (`StunChance`) | 8% (16% em alvo < 50%) | sim |  |
| 103 | Bakemon | Assassin | Especial | Esquiva e crítico garantido (`DodgeThenGuaranteedCrit`) | 4% | sim |  |
| 110 | Tortamon | Tank | Físico | Chance de anular o golpe (`NegateDamageChance`) | 18% | sim |  |
| 117 | Snatchmon | Ranged | Especial | Crítico drena vida (`CritLifesteal`) | 4% | sim |  |
| 130 | Reppamon | Ranged | Especial | Bônus no 1º golpe (`OpeningHitBonus`) | 7% | sim |  |
| 133 | Coredramon (Green) | Warrior | Físico | Stat puro: PA (`PhysicalAttackPercent`) | 6,5% | sim |  |
| 136 | Coredramon (Blue) | Ranged | Físico | Golpe extra à distância (`RangedExtraStrike`) | 13% de chance, 30% do dano | sim |  |
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
| 42 | Rapidmon | Ranged | Físico | Golpe extra à distância (`RangedExtraStrike`) | 18% de chance, 30% do dano | sim | sim |
| 43 | MagnaAngemon | Ranged | Especial | Perfurar defesa a cada N (`PeriodicDefensePierce`) | a cada 3 ataques, ignora 25% | sim |  |
| 49 | Taomon | Ranged | Especial | Esquiva e crítico garantido (`DodgeThenGuaranteedCrit`) | 7% | sim |  |
| 50 | Lillymon | Ranged | Especial | Crítico drena vida (`CritLifesteal`) | 7% | sim | sim |
| 51 | MegaKabuterimon | Tank | Especial | Chance de anular o golpe (`NegateDamageChance`) | 22% | sim |  |
| 61 | Garudamon | Ranged | Especial | Bônus no 1º golpe (`OpeningHitBonus`) | 10% | sim | sim |
| 67 | Zudomon | Tank | Físico | Stat puro: PD (`PhysicalDefensePercent`) | 12% | sim | sim |
| 72 | Angewomon | Ranged | Especial | Ataque cura o aliado mais ferido (`HealLowestAllyOnAttack`) | 9% | sim | sim |
| 80 | Grand Galemon | Ranged | Especial | Golpe extra à distância (`RangedExtraStrike`) | 15% de chance, 30% do dano | sim |  |
| 93 | Chaperomon | Support | Especial | Velocidade + dano em quem já agiu (`SpeedAndBonusVsActedTarget`) | +6% Vel.; +12% dano | sim; o Value é Velocidade, não ED (correção 2) | sim |
| 98 | Delumon | Warrior | Físico | HP baixo: +dano físico (`LowHpPhysicalDamageBonus`) | abaixo de 50% HP: +14% | sim |  |
| 104 | Fantomon | Assassin | Especial | Abate: +Velocidade (`KillSpeedBonus`) | 12% | sim |  |
| 131 | Chirinmon | Ranged | Especial | Chance de atordoar (`StunChance`) | 12% (22% em alvo < 50%) | sim | sim |
| 134 | Groundramon | Tank | Físico | Chance de anular o golpe (`NegateDamageChance`) | 20% | sim |  |
| 137 | Wingdramon | Ranged | Físico | Bônus no 1º golpe (`OpeningHitBonus`) | 11% | sim |  |
| 141 | AeroVeedramon | Ranged | Físico | Golpe extra à distância (`RangedExtraStrike`) | 16% de chance, 30% do dano | sim |  |
| 147 | Knightmon | Tank | Físico | Perfurar defesa a cada N (`PeriodicDefensePierce`) | a cada 3 ataques, ignora 20% | sim |  |
| 151 | Dorugreymon | Warrior | Físico | Stat puro: PA (`PhysicalAttackPercent`) | 10% | sim |  |
| 154 | Grademon | Tank | Físico | Chance de anular o golpe (`NegateDamageChance`) | 24% | sim |  |
| 157 | DexDorugreymon | Assassin | Físico | Perfurar defesa a cada N (`PeriodicDefensePierce`) | a cada 3 ataques, ignora 28% | sim | sim |
| 165 | SaviorHackmon | Assassin | Físico | Esquiva e crítico garantido (`DodgeThenGuaranteedCrit`) | 8% | sim |  |
| 170 | Mistymon | Assassin | Físico | Abate: +Velocidade (`KillSpeedBonus`) | 14% | sim |  |
| 179 | Lilamon | Support | Especial | Ataque cura o aliado mais ferido (`HealLowestAllyOnAttack`) | 11% | sim |  |
| 192 | Paildramon | Ranged | Físico | Abate: +Velocidade (`KillSpeedBonus`) | 10% | sim, com a correção | sim |
| 199 | JewelBeemon | Assassin | Físico | Perfurar defesa a cada N (`PeriodicDefensePierce`) | a cada 3 ataques, ignora 24% | sim |  |

## Efeitos usados no lote

| Efeito | Espécies |
|---|---|
| Perfurar defesa a cada N (`PeriodicDefensePierce`) | 7 |
| Golpe extra à distância (`RangedExtraStrike`) | 6 |
| Chance de anular o golpe (`NegateDamageChance`) | 5 |
| Bônus no 1º golpe (`OpeningHitBonus`) | 5 |
| HP baixo: +dano físico (`LowHpPhysicalDamageBonus`) | 4 |
| Esquiva e crítico garantido (`DodgeThenGuaranteedCrit`) | 4 |
| Velocidade + dano em quem já agiu (`SpeedAndBonusVsActedTarget`) | 4 |
| Stat puro: PA (`PhysicalAttackPercent`) | 4 |
| Abate: +Velocidade (`KillSpeedBonus`) | 4 |
| Crítico drena vida (`CritLifesteal`) | 3 |
| Ataque cura o aliado mais ferido (`HealLowestAllyOnAttack`) | 3 |
| Stat puro: PD (`PhysicalDefensePercent`) | 2 |
| Chance de atordoar (`StunChance`) | 2 |
| 1º golpe recebido reduzido (`FirstHitTakenReduction`) | 1 |
| Crítico encurta debuffs (`CritShortensDebuffs`) | 1 |
| Stat puro: Velocidade (`SpeedPercent`) | 1 |

## Observações (comportamento do código que já existia, não mudou)

- **`PeriodicDefensePierce` e `StunChance` não somam:** o código usa só a primeira espécie daquele tipo no histórico (`FirstOrDefault`). Duas linhas do jogo passam por duas espécies com perfurar defesa:
  - **Gladimon (4 / 18%) → Knightmon (3 / 20%):** quem evoluir além do Knightmon herda os dois, mas só vale o do Gladimon.
  - **DexDorugamon (3 / 22%) → DexDorugreymon (3 / 28%):** quem evoluir além do DexDorugreymon herda os dois, mas só vale o do DexDorugamon, que é o mais fraco.
  - Nenhuma linha passa por duas espécies com `StunChance`. Contei pelas evoluções em `Data/Evolutions` e pelos Jogress.
- **O teto de +40% do perfurar defesa (Fase 1d) vale pra todos.** Com defesa alta, o ganho dos valores maiores (DexDorugreymon 28%, MagnaAngemon 25%, JewelBeemon 24%) é cortado pelo teto.
- **Os demais efeitos somam** (`Sum`), inclusive o `KillSpeedBonus`. Um Digimon que vem do Jogress ExVeemon + Stingmon → Paildramon:
  - Velocidade por abate: 9% + 10% = 19%.
  - PA: os 7% do ExVeemon.
  - Isso está testado.

## Testes (GameAudit, seção `inherited`)

Cada teste confirma que o efeito dispara com a espécie no histórico e não dispara fora da condição. A forma atual é uma Mega sem herdada (id 95), pra só a espécie testada contar.

Amostra de 19 espécies:

**Os 13 gatilhos:**
- `NegateDamageChance`: Kabuterimon.
- `FirstHitTakenReduction`: Angemon.
- `PeriodicDefensePierce`: Togemon (a cada 4) e DexDorugreymon (a cada 3, 28%).
- `LowHpPhysicalDamageBonus`: Growlmon.
- `RangedExtraStrike`: Rapidmon.
- `OpeningHitBonus`: Garudamon.
- `DodgeThenGuaranteedCrit`: Wizardmon.
- `SpeedAndBonusVsActedTarget`: Sunflowmon e Chaperomon, checando também que a ED não muda.
- `CritShortensDebuffs`: Devidramon.
- `KillSpeedBonus`: Stingmon.
- `CritLifesteal`: Lillymon.
- `HealLowestAllyOnAttack`: Angewomon.
- `StunChance`: Chirinmon.

**Stat puro:** Geogreymon (PA), Zudomon (PD), Dorugamon (Velocidade).

**Paildramon isolado:** o abate dá +10% de Velocidade, e sozinho ele não mexe no PA. À parte, o histórico ExVeemon + Stingmon + Paildramon dá Velocidade 1190 depois de um abate e PA 1070.

### Resultado

- **Build:** 0 erros. Os 9 avisos de compilação são os de antes.
- **GameAudit completo:** 48146 checagens, 0 falhas, 7 avisos. São os 7 avisos antigos: 6 campeonatos fortes demais em `flows` e 3 avisos num dia em `grandevent`.
- **Seção `inherited`:** 0 falhas e 70 linhas de passiva ok. São as 51 que já existiam mais as 19 deste lote, e o Paildramon com o histórico do Jogress também passou.
- **Save real:** o SHA-256 de `save.json` e de `save.json.bak` é o mesmo antes e depois da rodada.
