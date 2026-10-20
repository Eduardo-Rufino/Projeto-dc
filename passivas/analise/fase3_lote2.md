# Passivas herdadas — Fase 3: Rookie restantes (lote 2)

Data: 2026-10-20 · Branch: `passivas-herdadas` (sobre `1df576a`, Fase 2)

Só dado: o campo `InheritedPassive` foi preenchido nos JSONs de 30 espécies Rookie. Nenhuma lógica de jogo mudou. O `GameAudit.cs` só ganhou os testes da amostra (seção `inherited`).

## Cobertura

- **30 novas + 2 do piloto (Agumon, Gabumon) = 32**, ou seja, 100% dos Rookie do jogo.
- Das 30 novas, 16 atacam com dano físico e 14 com dano especial. Todas são de stat percentual simples (`Value2` = 0).

## Critérios de escolha

1. **Faixa de valor:** de 4% a 6%, em degraus de 0,5% (média das novas 4,95%), mais uma espécie em 3,5% (ver abaixo).
2. **Stat de ataque coerente com o tipo de ataque:** PA só pra quem ataca com dano físico e EA só pra quem ataca com dano especial, como no lote 1.
3. **Distribuição no lote:** HP máx 6, EA 5, PD 5, ED 5, Velocidade 5, PA 4. O PA tem uma a menos porque o PA 5% já é do Gabumon.
4. **Sem repetir stat em sequência:** na ordem de id, contando Agumon e Gabumon, duas espécies seguidas nunca têm o mesmo stat.
5. **Sem duplicar:** nenhuma passiva nova repete stat+valor de outra Rookie, nova ou do piloto.
6. **Afinidade com a Role, quando dava:**
   - Tanks ficaram com PD ou HP, e Supports com ED.
   - Assassins ficaram com Velocidade, EA ou HP.
   - Warriors ficaram com PA, HP ou PD.

### Por que uma espécie ficou em 3,5%

- O Agumon herda "+5% dano físico", que não é um stat, então não ocupa nenhuma combinação stat+valor. O Gabumon ocupa PA 5%.
- Entre 4% e 6%, com degraus de 0,5%, há 6 x 5 - 1 = 29 combinações livres pra 30 espécies.
- A 30ª, **Hackmon (HP máx)**, ficou em 3,5%. É o mesmo critério do lote 1: errar pra baixo, e não criar uma passiva acima do patamar, já que o objetivo desta expansão é não desbalancear.
- Foi pro HP porque, entre os stats, é o que tem o efeito mais suave (redução do dano recebido).

## Rookie

| Id | Espécie | Role | Ataque | Stat | Valor | Origem |
|---|---|---|---|---|---|---|
| 1 | Agumon | Warrior | Físico | dano físico | 5% | piloto |
| 2 | Gabumon | Ranged | Físico | PA | 5% | piloto |
| 3 | Tentomon | Tank | Especial | PD | 5% | nova, testada |
| 4 | Patamon | Ranged | Físico | Velocidade | 5% | nova |
| 5 | Gaomon | Warrior | Físico | PA | 5,5% | nova, testada |
| 6 | Palmon | Ranged | Físico | ED | 4,5% | nova |
| 7 | Guilmon | Warrior | Físico | HP máx | 5,5% | nova, testada |
| 8 | Terriermon | Ranged | Especial | EA | 5% | nova, testada |
| 9 | Renamon | Assassin | Especial | Velocidade | 6% | nova, testada |
| 10 | Impmon | Ranged | Especial | EA | 4,5% | nova |
| 11 | Agumon (Savers) | Warrior | Físico | PA | 4,5% | nova |
| 12 | Lalamon | Support | Especial | ED | 5,5% | nova, testada |
| 59 | Biyomon | Ranged | Físico | Velocidade | 4% | nova, testada |
| 65 | Gomamon | Tank | Físico | PD | 5,5% | nova |
| 71 | Gatomon | Assassin | Especial | EA | 5,5% | nova |
| 78 | Pteromon | Ranged | Especial | Velocidade | 5,5% | nova |
| 84 | Elizamon | Assassin | Especial | HP máx | 4% | nova |
| 91 | Shoemon | Support | Especial | ED | 6% | nova |
| 96 | Muchomon | Warrior | Físico | PA | 6% | nova |
| 102 | Ghostmon | Assassin | Especial | EA | 4% | nova |
| 109 | Sunarizamon | Tank | Físico | PD | 6% | nova |
| 116 | BEMmon | Ranged | Especial | HP máx | 4,5% | nova |
| 124 | Lucemon | Assassin | Especial | EA | 6% | nova, testada |
| 126 | Phascomon | Tank | Físico | HP máx | 6% | nova |
| 129 | Kudamon | Ranged | Especial | ED | 4% | nova |
| 132 | Dracomon | Warrior | Físico | PA | 4% | nova, testada |
| 139 | Veemon | Warrior | Físico | HP máx | 5% | nova |
| 145 | Kotemon | Warrior | Físico | PD | 4,5% | nova, testada |
| 149 | Dorumon | Warrior | Físico | Velocidade | 4,5% | nova |
| 163 | Hackmon | Assassin | Físico | HP máx | 3,5% | nova, testada |
| 169 | Candlemon | Ranged | Especial | ED | 5% | nova, testada |
| 197 | Wormmon | Assassin | Físico | PD | 4% | nova |

## Testes (GameAudit, seção `inherited`)

Mesmo padrão dos lotes anteriores (`StatTest` e `HpTest`): a passiva dispara com a espécie no histórico e não dispara sendo a forma atual, sem histórico.

Amostra de 12 espécies novas, cobrindo os 6 stats (2 por stat):
- **EA:** Terriermon, Lucemon.
- **PA:** Gaomon, Dracomon.
- **PD:** Tentomon, Kotemon.
- **ED:** Lalamon, Candlemon.
- **Velocidade:** Renamon, Biyomon.
- **HP máx:** Guilmon, Hackmon.

### Resultado

- **Build:** 0 erros. Os 9 avisos de compilação são os de antes.
- **GameAudit completo:** 48327 checagens, 0 falhas, 7 avisos. São os 7 avisos antigos: 6 campeonatos fortes demais em `flows` e 3 avisos num dia em `grandevent`.
- **Seção `inherited`:** 0 falhas e 51 linhas de passiva ok. São as 39 que já existiam (piloto e lote 1) mais as 12 da amostra.
- **Save real:** o SHA-256 de `save.json` e de `save.json.bak` é o mesmo antes e depois da rodada.
