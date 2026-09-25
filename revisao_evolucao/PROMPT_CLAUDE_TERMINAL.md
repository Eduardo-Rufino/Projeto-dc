# Tarefa: validar e aplicar a revisão dos requisitos de evolução

Os requisitos de evolução já foram revisados várias vezes sem resolver o problema. Por isso esta
tarefa **começa por provar o diagnóstico no código real** e só depois altera dados. Trabalhe em
fases e **pare no fim de cada fase marcada com PARE** para eu aprovar.

## Contexto em 5 linhas

- A evolução é checada automaticamente (após treino, batalha, fim do dia) e vence a de maior score
  entre as válidas. Por isso, o treino para mirar numa forma muitas vezes cumpre os requisitos de
  uma irmã antes, e ela dispara primeiro.
- Uma análise externa (feita sobre o `REQUISITOS_EVOLUCAO.md` gerado pelo projeto) encontrou 28
  formas impossíveis de alcançar treinando e 3 empates de score entre irmãs.
- A proposta é uma regra de "assinatura de treino": em cada pai, uma **forma padrão** que pede só o
  que o nível dá mas só libera perto do teto de idade (rede de segurança), e cada outra irmã pede
  **um stat exclusivo**.
- Os valores finais das 337 ligações estão em `requisitos_evolucao_revisados.csv`.
- `analise/verificar.py` refaz a medição a partir do `.md` (com ou sem o CSV). Os resultados
  esperados estão em `analise/esperado_*.txt`.

## Regras

- Crie um branch (`revisao-evolucao`) antes de qualquer edição. Nada de commit na main, nada de merge.
- **Só dados.** Não altere lógica do jogo. Se alguma premissa abaixo estiver errada no código,
  **não conserte o código nem ajuste o CSV por conta própria**: relate e pare.
- Aplique o CSV com um **script de importação**, nunca editando valores à mão.
- Se achar um valor no CSV que pareça errado, liste-o no relatório; não corrija.

---

## Fase 1 — Conferir as premissas no código (somente leitura) · PARE

A análise supõe o que está abaixo. Para cada item, responda **Confirmado / Diferente / Não
encontrado**, com `arquivo:linha` e, se for diferente, o comportamento real.

| # | Premissa | Por que importa |
|---|---|---|
| 1 | A evolução é checada depois de **cada** treino, **cada** batalha e no fim do dia, e aplica imediatamente a primeira válida. | É o que faz uma irmã "atropelar" a outra. Se a checagem for só no fim do dia, o diagnóstico muda. |
| 2 | Entre as válidas vence o maior `Score`; empate = aleatório. O `Score` é exatamente a fórmula da seção 2.3 (inclui `Idade`, `100 - Disc≤`, `100 - Felic≤`). | Toda a separação entre irmãs depende disso. |
| 3 | Os requisitos de stat comparam com o stat **máximo/base atual**, não com o HP corrente (ferido). | Se comparar HP corrente, perder HP em batalha "desliga" evoluções. |
| 4 | O nível **não reseta** ao evoluir. | A projeção por nível usa isso. |
| 5 | A idade acumula desde o nascimento e não reseta. Qual roda primeiro no fim do dia: a morte por idade (`Idade > teto`) ou a checagem de evolução? O requisito de idade é `>=`? | A forma padrão usa `Idade ≥ teto − 2`. |
| 6 | Por nível: +2 PA, +2 PD, +1 Vel, +5 HP; EA/ED não sobem. | Projeção de stats. |
| 7 | Treino: +1/2/3 (25/50/25%) no stat escolhido, 6× isso no HP, +10 Felicidade, +5 Disciplina; ambos limitados a 0–100. | `Felic≥` fica automático para quem treina; `Disc≤` exige descuido. |
| 8 | Ao evoluir: `max(stat × xMult, lerp(stat × xMult, ficha_nova, 0.4))`. | Stats de entrada de cada forma. |
| 9 | Como o bloqueio pelo Wizardmon funciona e o que acontece com uma evolução válida bloqueada por **capacidade** (fica pendente e é reavaliada? outra irmã pode passar na frente?). | Pode mudar quem dispara primeiro. |
| 10 | **Qual arquivo o jogo lê de verdade em tempo de execução** para os requisitos: `Data/Evolutions`, `evolution_web.json`, outro? Existe duplicação, cache, `.import`/`.tres` gerado ou export que precise ser refeito? O gerador do `REQUISITOS_EVOLUCAO.md` lê a mesma fonte que o jogo? | Se as revisões anteriores foram feitas numa fonte que o jogo não lê (ou que é sobrescrita), isso explicaria "revisei e nada mudou". |
| 11 | Formato de `Vitórias` nos dados (55 ou 0.55) e nomes dos campos de Disc/Felic mínimo e máximo. | Para o script de importação converter certo. |

Entregue a tabela preenchida e pare. **Se os itens 1, 2, 3, 5 ou 10 forem "Diferente", não siga
para a Fase 2** — eu refaço a análise com o comportamento real.

---

## Fase 2 — Reproduzir o problema nos dados atuais · PARE

1. Gere o `REQUISITOS_EVOLUCAO.md` com o gerador do projeto, a partir dos dados atuais.
2. Rode `python3 analise/verificar.py REQUISITOS_EVOLUCAO.md` e compare com
   `analise/esperado_dados_atuais.txt` (esperado: **A = 28, B = 0, C = 3**). Explique qualquer
   diferença.
3. Prova no código real: escreva um teste headless (GDScript, rodando com `godot --headless`)
   que usa a **função real** de escolha de evolução, sem reimplementá-la. Para cada caso, monte
   o Digimon com os stats indicados, aplique os treinos um a um chamando a checagem real depois
   de cada um, e registre qual forma sai:

   | Caso | Montagem | Resultado esperado hoje |
   |---|---|---|
   | Guilmon mirando Growlmon | Guilmon Lv 12 com HP 154, PA 64, PD 44, EA 9, ED 15, Vel 42 (ficha + 10 níveis); treinar PA um por um até 84 | Em PA 81 sai **Dimetromon**, antes de Growlmon (PA 84) |
   | Gaomon mirando Gladimon (Lv 18) | Gaomon Lv 12 com HP 154, PA 68, PD 42, EA 9, ED 15, Vel 32; treinar PA até 92 e ir subindo de nível até 18 | Em PA 80 sai **Gaogamon**, e Gladimon nunca é alcançado |
   | Angewomon mirando Magnadramon | Cumprir os stats de Magnadramon treinando (Felicidade sobe com o treino) | Sai **Ophanimon** |
   | MachGaogamon mirando Gankoomon | Idem | Sai **MirageGaogamon** |
   | Gigimon com stats que cumprem Guilmon e Elizamon | Checar 20 vezes | Resultado **varia** (empate 125 × 125) |

   Os requisitos exatos de cada ligação estão no `.md`. Se o teste real contradisser a análise
   em algum caso, isso é mais importante que o resto: relate com detalhe.

Pare e mostre: saída do `verificar.py`, resultado de cada caso do teste e o caminho do teste.

---

## Fase 3 — Aplicar no branch e provar que resolveu · PARE

1. Escreva um script de importação que lê `requisitos_evolucao_revisados.csv` e grava nos
   arquivos que o jogo lê (item 10 da Fase 1), convertendo formatos (item 11). O CSV tem uma
   linha por ligação (`origem`, `destino`) com os valores **finais**; a coluna `alterada` diz se
   algo mudou em relação aos dados atuais. Ligações que não existirem nos dados = erro, não criar.
2. Rode o script e mostre o resumo do diff: nº de arquivos e de ligações alteradas (esperado:
   249 ligações com alguma mudança, 88 sem mudança).
3. Regenere o `REQUISITOS_EVOLUCAO.md` a partir dos dados **já alterados** e rode
   `python3 analise/verificar.py REQUISITOS_EVOLUCAO.md` (sem `--csv`). O resultado tem de bater
   com `analise/esperado_com_csv.txt`: **A = 0, B = 0, C = 0**. Se bater, prova que a importação
   foi fiel; se não, algo não foi gravado onde o jogo lê.
4. Rode de novo o teste headless da Fase 2, agora mirando em cada forma pelo stat-assinatura dela
   (coluna `papel` do CSV):
   - Guilmon: treinar só PA → **Growlmon**. Sem treinar nada até a idade 13 → **Dimetromon** (forma padrão).
   - Gaomon: treinar só Vel até Lv 18 → **Gladimon**; só PA → **Gaogamon**; nada → **Garurumon** aos 13 dias.
   - Angewomon: só EA → **Ophanimon**; só ED → **Lilithmon**; nada → **Magnadramon** aos 50 dias.
   - Gigimon: sem sorteio; o mesmo estado dá sempre a mesma forma.
   - Um Rookie qualquer que não treina nada: evolui para a forma padrão antes de morrer (Idade 13, teto 15).

Pare e mostre o relatório final.

## Relatório final (formato)

1. Tabela de premissas (Fase 1).
2. Antes: saída do `verificar.py` + casos do teste real.
3. Depois: saída do `verificar.py` + casos do teste real.
4. Diff: arquivos tocados, nº de ligações, script de importação usado.
5. Divergências, valores suspeitos do CSV e qualquer coisa que você não conseguiu verificar.

Não faça merge. Eu reviso e decido.
