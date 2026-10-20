# Passivas herdadas — Fase 2: Baby e In-Training restantes (lote 1)

Data: 2026-10-20 · Branch: `passivas-herdadas` (sobre `a8af8d9`, que adicionou o `SpecialAttackPercent`)

Só dado: o campo `InheritedPassive` foi preenchido nos JSONs de 35 espécies de `Data/Digimon/`. Nenhuma lógica de jogo mudou. O `GameAudit.cs` recebeu os testes da amostra (seção `inherited`).

## Cobertura

- **35 novas + 8 do piloto = 43** espécies Baby e In-Training, ou seja, 100% dos dois estágios (20 Baby + 23 In-Training).
- Todas as passivas são de stat percentual simples (`Value2` = 0).

## Critérios de escolha

1. **Faixa de valor:**
   - Baby de 2% a 3,5% (média das novas 2,90%).
   - In-Training de 3% a 5% (média das novas 4,05%).
   - O valor varia em degraus de 0,5%. Com valor fixo (3%/4%) e 6 stats não caberiam 15 e 20 espécies sem repetir stat+valor no estágio.
2. **Stat de ataque coerente com o tipo de ataque:** PA só pra quem ataca com dano físico e EA só pra quem ataca com dano especial. Num atacante de dano especial, PA não faria nada.
3. **Distribuição equilibrada no lote (35 novas):** EA 6, HP máx 6, PD 6, ED 6, Velocidade 6, PA 5.
4. **Sem repetir stat em sequência:** na ordem de id dentro de cada estágio, contando as espécies do piloto intercaladas, duas espécies seguidas nunca têm o mesmo stat. A única exceção é Botamon/Punimon (HP e HP), que são as duas do piloto.
5. **Sem duplicar:** nenhuma passiva nova repete stat+valor de outra do mesmo estágio, seja nova ou do piloto. Única repetição: HP 3% (Botamon/Punimon), que vem do próprio piloto e ficou como estava.

### Por que dois Baby ficaram em 2%

No Baby, o piloto já tinha usado a combinação 3% em HP, PA, Velocidade e ED. Sobram 14 combinações entre 2,5% e 3,5% pra 15 espécies novas, e uma delas é inutilizável:
- Só 3 Baby novos atacam com dano físico (os únicos que podem ter PA): Sunamon, Puttimon e Dodomon.
- Puttimon (172) e Dodomon (185) ficam colados no Chibomon (183, PA do piloto) na ordem de id. Dar PA a eles colocaria PA em sequência.
- Por isso só o Sunamon recebe PA (3,5%), e o PA 2,5% fica sem uso.
- **Nyokimon (Velocidade) e Dodomon (PD)** ficaram em 2%. Ficaram abaixo, e não em 4%, pra nenhum Baby passar de um In-Training (mínimo de 3%).

## Baby

| Id | Espécie | Role | Ataque | Stat | Valor | Origem |
|---|---|---|---|---|---|---|
| 25 | Botamon | Warrior | Físico | HP máx | 3% | piloto |
| 26 | Punimon | Warrior | Especial | HP máx | 3% | piloto |
| 27 | Yuramon | Ranged | Especial | ED | 3,5% | nova, testada |
| 28 | Keemon | Ranged | Especial | Velocidade | 3,5% | nova, testada |
| 57 | Poyomon | Ranged | Especial | EA | 3,5% | nova, testada |
| 63 | Pichimon | Warrior | Físico | ED | 3% | piloto |
| 69 | Nyaromon | Ranged | Especial | HP máx | 3,5% | nova, testada |
| 76 | Yolkmon | Ranged | Especial | EA | 2,5% | nova |
| 83 | Jyarimon | Assassin | Especial | Velocidade | 3% | piloto |
| 89 | Pafumon | Support | Especial | ED | 2,5% | nova, testada |
| 100 | Mokumon | Assassin | Especial | Velocidade | 2,5% | nova |
| 107 | Sunamon | Tank | Físico | PA | 3,5% | nova, testada |
| 114 | Kuramon | Ranged | Especial | EA | 3% | nova |
| 172 | Puttimon | Warrior | Físico | PD | 3,5% | nova |
| 183 | Chibomon | Warrior | Físico | PA | 3% | piloto |
| 185 | Dodomon | Warrior | Físico | PD | 2% | nova |
| 187 | Zerimon | Ranged | Especial | HP máx | 2,5% | nova |
| 188 | Pabumon | Ranged | Especial | PD | 2,5% | nova |
| 189 | Nyokimon | Ranged | Especial | Velocidade | 2% | nova |
| 195 | Leafmon | Ranged | Especial | PD | 3% | nova, testada |

## In-Training

| Id | Espécie | Role | Ataque | Stat | Valor | Origem |
|---|---|---|---|---|---|---|
| 29 | Koromon | Warrior | Físico | HP máx | 4% | piloto |
| 30 | Tsunomon | Warrior | Físico | PD | 4% | piloto |
| 31 | Tokomon | Ranged | Físico | PA | 4,5% | nova |
| 32 | Motimon | Ranged | Físico | Velocidade | 3,5% | nova |
| 33 | Gigimon | Ranged | Físico | PA | 4% | nova |
| 34 | Gummymon | Ranged | Físico | HP máx | 4,5% | nova |
| 35 | Viximon | Warrior | Físico | Velocidade | 4,5% | nova |
| 36 | Tanemon | Ranged | Especial | EA | 4,5% | nova |
| 37 | Wanyamon | Warrior | Físico | PA | 3,5% | nova |
| 38 | Yaamon | Warrior | Físico | HP máx | 3,5% | nova |
| 58 | Yokomon | Ranged | Físico | PD | 4,5% | nova |
| 64 | Bukamon | Warrior | Físico | Velocidade | 4% | piloto |
| 70 | Salamon | Ranged | Físico | ED | 4% | nova |
| 77 | Fluffymon | Ranged | Especial | EA | 3,5% | nova |
| 90 | Kyaromon | Support | Especial | ED | 4,5% | nova |
| 101 | PetiMeramon | Assassin | Especial | Velocidade | 5% | nova |
| 108 | Goromon | Tank | Físico | PD | 3,5% | nova |
| 115 | Tsumemon | Ranged | Especial | EA | 4% | nova, testada |
| 173 | Babydmon | Warrior | Físico | PA | 5% | nova, testada |
| 184 | DemiVeemon | Warrior | Físico | HP máx | 5% | nova, testada |
| 186 | Dorimon | Warrior | Físico | ED | 3,5% | nova, testada |
| 190 | Budmon | Ranged | Especial | HP máx | 3% | nova |
| 196 | Minomon | Ranged | Especial | ED | 3% | nova |

## Testes (GameAudit, seção `inherited`)

Mesmo padrão do piloto. **`StatTest`**: com a espécie no histórico (forma atual Agumon), o stat sai de 1000 pra 1000 x (1 + %). Sendo ela a forma atual, sem histórico, fica em 1000. **`HpTest`**: com a espécie no histórico, o dano recebido é dividido por (1 + %). Sem ela, não muda.

Amostra de 11 espécies novas, cobrindo os 6 stats e os dois estágios:
- **EA:** Poyomon (Baby), Tsumemon (In-Training).
- **Velocidade:** Keemon (Baby).
- **PA:** Sunamon (Baby), Babydmon (In-Training).
- **PD:** Leafmon (Baby).
- **ED:** Pafumon e Yuramon (Baby), Dorimon (In-Training).
- **HP máx:** Nyaromon (Baby), DemiVeemon (In-Training).

Também checa que o EA herdado (Poyomon) não mexe no PA.

O teste de EA do commit anterior (`a8af8d9`) emprestava o Dodomon, que na época não tinha herdada, só durante o teste. Agora o Dodomon tem herdada própria (PD), e o teste passou a usar o Poyomon, que tem EA de verdade.

### Resultado

- **Build:** 0 erros. Os 9 avisos de compilação são os de antes e nenhum vem de código desta fase.
- **GameAudit completo:** 48547 checagens, 0 falhas, 7 avisos. São os 7 avisos antigos: 6 campeonatos fortes demais em `flows` e 3 avisos num dia em `grandevent`.
- **Seção `inherited`:** 0 falhas. As 11 espécies da amostra dispararam com a espécie no histórico e não dispararam fora dele, e as 28 linhas do piloto continuam ok.
- O `save.json` real ficou idêntico ao backup feito antes da rodada.
