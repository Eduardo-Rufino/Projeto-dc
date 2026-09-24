# Fase 3 v3 — correção do achado Gaogamon → MachGaogamon (e dos 5 casos iguais)

Mesmas regras de antes: branch `revisao-evolucao`, sem merge, sem commit na main. **Só dados.**
Não altere lógica do jogo. Aplique o CSV só pelo `importar_csv.py`, nunca à mão. Se algum número
abaixo não bater, **pare e relate** — não ajuste nada por conta própria.

## O que mudou no pacote

Seu achado estava certo e é uma classe de problema que o verificador não enxergava: ele só olhava
a rota de entrada mais **fraca**. O Gaogamon que vem pela rota **forte** já chega com PD/PA altos
(carry-over), então a irmã de score maior fica válida junto e sempre vence.

- `analise/verificar.py` ganhou duas checagens, calculadas **rota por rota** desde o ovo (sem
  misturar stats de rotas diferentes):
  - **F** — forma inalcançável mirando nela por ALGUMA rota de entrada (é o seu achado);
  - **G** — informativo: forma padrão que alguma rota forte pula porque uma irmã sai de graça.
    Isso é o carry-over já conhecido (Digimon forte ganha a irmã premium sem treinar). Não é bug.
- `requisitos_evolucao_revisados.csv` = v4. Muda só 18 ligações em relação ao v3 que você importou.
  6 pais que tinham o mesmo defeito do Gaogamon:

| Pai | Forma padrão (sem treino, só idade) | Irmã(s) com assinatura |
|---|---|---|
| Gaogamon | MachGaogamon (Idade 24) | WereGarurumon: EA ≥ 47 |
| Togemon | Lillymon (Idade 24) | Lilamon: ED ≥ 62 |
| WarGrowlmon | Gallantmon (Idade 50; perde Felic≥55/Batalhas/Vitórias) | Medieval Dukemon: PD ≥ 180 |
| Knightmon | Craniummon (Idade 50) | Crusadermon: Vel ≥ 147 (Lv 36) |
| JewelBeemon | HerculesKabuterimon (já era) | Imperialdramon: assinatura muda de Vel para **ED ≥ 99**; GrandisKuwagamon: PA (igual) |
| Angewomon | Magnadramon (já era) | Ophanimon: HP 387 → 350 (só baixa o score para 794, abaixo do Lilithmon 798) |

Regra usada: a forma canônica vira a padrão; a outra assina num stat que **nenhuma** rota de
entrada já traz alto. Os outros ajustes nas 18 linhas são cascata (stats "do nível" recalculados
para o que chega nos filhos desses pais).

## Passos

**1. Verificador novo contra o que já está importado (v3).** Rode `verificar.py` sem `--csv`
sobre o .md gerado do branch atual, K=1. Esperado: A=0, B=0, C=0, E=0 e **F=6**, exatamente:
Angewomon→Lilithmon, Gaogamon→MachGaogamon, JewelBeemon→GrandisKuwagamon,
Knightmon→Crusadermon, Togemon→Lillymon, WarGrowlmon→Gallantmon. Se não bater, pare.

**2. Importe o CSV v4** com `importar_csv.py`. Esperado: **18 ligações** mudam em relação ao
branch. Valores que **sobem** (e só estes 12):
- Idade: Togemon→Lillymon 5→24, Gaogamon→MachGaogamon 5→24, WarGrowlmon→Gallantmon 8→50, Knightmon→Craniummon 8→50
- Togemon→Lilamon ED 0→62; Gaogamon→WereGarurumon EA 0→47; WarGrowlmon→Medieval Dukemon PD 145→180;
  Knightmon→Crusadermon Vel 120→147; JewelBeemon→Imperialdramon ED 0→99
- WereGarurumon→MetalGarurumon EA 56→89; WereGarurumon→Gankoomon EA 56→89; Lilamon→Cendrillmon ED 55→94
  (cascata: WereGarurumon/Lilamon agora chegam com EA/ED altos, então o valor "do nível" volta a caber)

**3. Regere o .md e rode o verificador sem `--csv`, K=1 e K=7.** Tem de bater com
`analise/esperado_com_csv.txt` e `esperado_com_csv_k7.txt`: A=B=C=E=F=0, D=41, G=27.
Rode GameAudit e EvolutionDebugTests.

**4. Testes pela cadeia real** (estenda `EvolutionCheckpointTestFase3.cs`, mesma técnica: nasce
antes e sobe por `TryToEvolve` aresta por aresta; alimente a cada virada de dia). Em cada caso,
"treinar a assinatura" = treinar só aquele stat até o valor, sem mexer no resto.

| Pai / rota de entrada | Ação | Esperado |
|---|---|---|
| Gaogamon via Gaomon **e** via Kotemon | EA até 47, Lv 20 | WereGarurumon |
| Gaogamon via Gaomon **e** via Kotemon | sem treino, até Idade 24 | MachGaogamon (nada antes) |
| Togemon via Palmon | ED até 62, Lv 20 | Lilamon |
| Togemon via Palmon | sem treino, até Idade 24 | Lillymon |
| WarGrowlmon via Growlmon **e** via Devidramon | PD até 180, Lv 28 | Medieval Dukemon |
| WarGrowlmon via Growlmon | sem treino, até Idade 50 | Gallantmon |
| JewelBeemon via **Stingmon** | ED até 99, Felic ≥ 40, Lv 28 | Imperialdramon (não GrandisKuwagamon) |
| JewelBeemon via Kabuterimon | PA até 224, Lv 28 | GrandisKuwagamon |
| Angewomon via **Kudamon → Angemon** | ED até 108, Lv 28 | Lilithmon (não Ophanimon) |
| Knightmon via Tentomon → Kabuterimon | sem treino, até Idade 50 | Craniummon |
| Knightmon via Tentomon → Kabuterimon | Vel até 147, Lv 36 antes da Idade 50 | Crusadermon |

Esperado e **não** é falha (checagem G, carry-over): via Stingmon, GrandisKuwagamon sai sem
treinar; via Kudamon, Ophanimon sai sem treinar; via Gladimon/BaoHackmon/Wormmon→Kabuterimon,
Crusadermon sai sem treinar (no Lv 36). Se quiser, registre como informação.

Mantenha os 38 casos que já passavam. Commit no branch com os números. **Sem merge.** Pare e relate:
passos 1–3 com os números, tabela do passo 4 (passou/falhou, Score e stats na hora da evolução
quando falhar), e qualquer divergência.
