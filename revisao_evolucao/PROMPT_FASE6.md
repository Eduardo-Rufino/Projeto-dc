# Fase 6 — Baby e InTraining por EA/ED, e fechamento

Regras de sempre: branch `revisao-evolucao`, **só dados**, não altere lógica do jogo, CSV só pelo
`importar_csv.py`. Merge só no passo 5 e só com as condições cumpridas.

## Premissa a conferir ANTES de tudo

Esta correção depende de **poder treinar EA e ED já nos estágios Baby e InTraining**. Confira no
código/UI do treino. Se em algum desses estágios EA ou ED não for treinável, **pare e relate** —
o CSV inteiro desta fase depende disso.

## Por que mudou

As 9 formas sem nenhuma rota alcançável são todas de Baby→InTraining e InTraining→Rookie, e a
causa é a mesma: nessas faixas o requisito era em HP/PA/PD/Vel, que **sobem sozinhos com o nível**.
Como a checagem de evolução só roda depois de batalha (que dá nível) ou à meia-noite, quando ela
finalmente roda o Digimon já cumpre os requisitos das duas irmãs, e a de Score maior sempre vence.

EA e ED **não sobem por nível** — só por treino. Então nessas duas faixas cada irmã passa a assinar
por EA, por ED, ou pelas duas (a "premium", de Score maior), e cada pai mantém uma forma padrão que
sai sem treinar nada, presa pela idade. Exemplo do Tsunomon: treinar EA → Gabumon; treinar ED →
Kotemon; não treinar nada → Elizamon aos 5 dias. As quantias são pequenas (2 a 4 treinos).

O CSV novo muda **189 ligações** em relação ao que está no branch. Sobem apenas 74 valores:
32 de EA, 20 de ED e 22 de Idade (as formas padrão). Nada de HP/PA/PD/Vel sobe.

Também entram duas correções de arrasto: os requisitos dos estágios seguintes foram rebaixados
onde a chegada ficou mais fraca, e o HP do Angewomon → Ophanimon caiu para 344, para o Lilithmon
voltar a ganhar de quem treina ED.

O `verificar.py` do pacote ganhou a checagem **H** (rival de Score maior que fica válido só
subindo de nível), que é exatamente o defeito acima. Nos dados atuais do jogo ela acusa 16.

## Passos

**1.** Importe o CSV. Esperado: **189 ligações** mudam em relação ao branch, e só sobem valores de
EA, ED e Idade.

**2.** Rode `verificar.py` sem `--csv` sobre o .md regerado, K=1 e K=7. Tem de bater com
`analise/esperado_com_csv.txt` e `esperado_com_csv_k7.txt`: **A=B=C=E=F=H=0**, D=19, G=24.
Rode GameAudit e EvolutionDebugTests.

**3. Testes pela cadeia real** — grupos representativos, um de cada formato:

| Caso | Esperado |
|---|---|
| Tsunomon, treinando só EA até 25 | Gabumon |
| Tsunomon, treinando só ED até 15 | Kotemon |
| Tsunomon, sem treinar nada, até Idade 5 | Elizamon |
| Nyaromon, treinando só EA até 15 | Tokomon |
| Nyaromon, sem treinar nada, até Idade 2 | Salamon |
| Koromon, EA até 17 / ED até 12 / nada até Idade 5 | Agumon / Veemon / Agumon (Savers) |
| Yuramon, EA até 19 / ED até 8 / EA 15 + ED 8 / nada até Idade 2 | Minomon / Yokomon / Budmon / Tanemon |
| Guilmon → Growlmon, com e sem EA (Fase 5) | MetalGreymon / WarGrowlmon |

**4. Rode a RouteMeasurementTool de novo** (a medição completa, mesma da Fase 4) e me diga a lista
de formas sem nenhuma rota alcançável. Reconfirme o **Belphemon Rage Mode** com budget maior de
batalhas/dias, já que ele ficou como provável limite da ferramenta.

**5. Merge.** Se o passo 2 bateu, os casos do passo 3 passaram e a lista do passo 4 veio vazia
(ou só com o Belphemon Rage Mode explicado pelo budget), faça o merge de `revisao-evolucao` na
`development` — não na `main` — e diga quantas ligações mudaram no total em relação à development.

Se sobrar qualquer forma inalcançável, **não faça o merge**: pare e mande a lista com o motivo.
