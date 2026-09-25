# Fase 5 — último ajuste e fechamento

Regras de sempre: branch `revisao-evolucao`, **só dados**, não altere lógica do jogo, CSV só pelo
`importar_csv.py`. Se algum número não bater, pare e relate sem ajustar nada por conta própria.
O merge está autorizado **só** no passo 5 e **só** se as condições estiverem cumpridas.

## 1. Conserto do par Growlmon → {MetalGreymon, WarGrowlmon}

Uma linha muda no CSV (`requisitos_evolucao_revisados.csv` = v5):

- **Growlmon → MetalGreymon: EA 0 → 60** (Score 688 → 748), papel = assinatura EA.
- Growlmon → WarGrowlmon: nada muda; passa a ser a forma natural da linhagem.

Motivo: pela rota do Guilmon o Digimon chega no Lv 20 com HP675/PA395/PD163, o que já cumpria as
duas, e o MetalGreymon vencia por Score. O EA é o único stat que essa cadeia não traz — nada no
caminho do Guilmon pede EA e o nível não dá EA. Com isso, quem não treina EA vira WarGrowlmon
(a evolução natural da linhagem do starter) e quem treina EA até 60 vira MetalGreymon. O
MetalGreymon continua alcançável normalmente por Greymon e Geogreymon.

Importe o CSV. Esperado: **1 ligação muda** em relação ao branch.

## 2. Verificador

Rode `verificar.py` sem `--csv` sobre o .md regerado, K=1 e K=7. Tem de bater com
`analise/esperado_com_csv.txt` e `esperado_com_csv_k7.txt`: A=B=C=E=F=0, D=41, G=27.
Rode GameAudit e EvolutionDebugTests.

## 3. Testes pela cadeia real

Na `EvolutionCheckpointTestFase3.cs`, três casos, todos pela cadeia real:

| Caso | Esperado |
|---|---|
| Guilmon → Growlmon, sem treinar EA, até Lv 20 | WarGrowlmon |
| Guilmon → Growlmon, treinando só EA até 60, Lv 20 | MetalGreymon |
| Greymon → MetalGreymon (rota existente) | continua funcionando |

Mantenha os casos que já passavam.

## 4. Consulta no `rotas_reais.csv` (não muda nada, só responde)

Das 134 rotas não alcançáveis, me diga:
1. quantas são por outra irmã vencer antes e quantas por limite de batalhas;
2. **a lista de formas que ficaram sem NENHUMA rota alcançável** (Digimon que o jogador não
   consegue obter por caminho algum — ignore os que só existem por Jogress).

## 5. Merge

Se, e somente se: passo 2 bateu, os três casos do passo 3 passaram, e a lista do item 4.2 veio
**vazia** — faça o merge de `revisao-evolucao` na `development` (não na `main`), com mensagem
descritiva, e me diga quantas ligações mudaram no total.

Se a lista do item 4.2 **não** vier vazia, **não faça o merge**: pare e me mande a lista com o
motivo de cada uma. Nesse caso eu decido caso a caso antes de fechar.

No relatório final: números dos passos 1–3, as respostas do passo 4 e o que foi feito no passo 5.
