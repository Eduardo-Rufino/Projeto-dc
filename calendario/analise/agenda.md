# Agenda dos campeonatos

Gerado a partir da saída de `CalendarSystem.IsOpen` na seção `calendar` do GameAudit (linhas `[AUDIT-AGENDA]` e `[AUDIT-AGENDA-INICIO]`): é o que o código calcula, não conta feita à mão. Desde a Fase 3 a agenda vale no jogo: um campeonato só inicia se estiver aberto, destravado e não tiver sido vencido na edição atual.

Ano de 60 dias = 4 estações de 15. Travas (`UnlockAfterTournamentId`): 10 destrava após vencer o 3, e 11 e 12 após vencer o 8. Todos têm `AnnounceDaysBefore = 5`. Os intervalos recorrentes (6, 10 e 20) dividem 60, então todo ano é igual a este.

## Dias abertos por campeonato

| Id | Campeonato | Tipo | Agenda | Trava | Dias abertos/ano |
|---:|---|---|---|---|---:|
| 1 | Copa Novato | Recurring | a cada 6 dias, aberta 3 | — | 30 |
| 2 | Torneio Rookie | Recurring | a cada 10 dias, aberta 4 | — | 24 |
| 3 | Desafio Champion | Window | dias 10 a 25 | — | 16 |
| 4 | Trio das Trevas | Fixed | dia 40, aberta 3 | — | 3 |
| 5 | Trio Tamers | Window | dias 20 a 35 | — | 16 |
| 6 | Copa Ultimate | Fixed | dia 30, aberta 3 | — | 3 |
| 7 | Reis do Mar | Fixed | dia 50, aberta 3 | — | 3 |
| 8 | Copa Mega | Fixed | dia 55, aberta 3 | — | 3 |
| 9 | Trio Celestial | Fixed | dia 15, aberta 3 | — | 3 |
| 10 | Trio Guardião 02 | Recurring | a cada 20 dias, aberta 5 | após o 3 | 15 |
| 11 | Copa das Lendas | Fixed | dia 45, aberta 5 | após o 8 | 5 |
| 12 | Copa Mega: Revanche | Fixed | dia 58, aberta 3 (58 a 60) | após o 8 | 3 |

Por tipo: Recurring 3, Window 2, Fixed 7, Unlockable 0, Always 0.

## Cobertura

- **Todos os campeonatos** (sem considerar a trava): 58/60 dias com pelo menos 1 aberto; dias sem nenhum: 6, 36; maior sequência sem nenhum (contando a virada do ano): 1.
- **Início do jogo** (nada vencido, então só os sem trava, de 1 a 9): 51/60 dias com pelo menos 1 disponível; dias sem nenhum: 5, 6, 36, 46, 47, 48, 58, 59, 60; maior sequência sem nenhum: 3.

## Ano completo

`●` = aberto. "Qtd" = quantos abertos no dia; "Início" = quantos disponíveis no começo do jogo (sem nada vencido).

| Dia | Estação | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 | 12 | Qtd | Início |
|---:|---|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|---:|---:|
| 1 | Primavera 1 | ● | ● |  |  |  |  |  |  |  | ● |  |  | 3 | 2 |
| 2 | Primavera 2 | ● | ● |  |  |  |  |  |  |  | ● |  |  | 3 | 2 |
| 3 | Primavera 3 | ● | ● |  |  |  |  |  |  |  | ● |  |  | 3 | 2 |
| 4 | Primavera 4 |  | ● |  |  |  |  |  |  |  | ● |  |  | 2 | 1 |
| 5 | Primavera 5 |  |  |  |  |  |  |  |  |  | ● |  |  | 1 | 0 |
| 6 | Primavera 6 |  |  |  |  |  |  |  |  |  |  |  |  | 0 | 0 |
| 7 | Primavera 7 | ● |  |  |  |  |  |  |  |  |  |  |  | 1 | 1 |
| 8 | Primavera 8 | ● |  |  |  |  |  |  |  |  |  |  |  | 1 | 1 |
| 9 | Primavera 9 | ● |  |  |  |  |  |  |  |  |  |  |  | 1 | 1 |
| 10 | Primavera 10 |  |  | ● |  |  |  |  |  |  |  |  |  | 1 | 1 |
| 11 | Primavera 11 |  | ● | ● |  |  |  |  |  |  |  |  |  | 2 | 2 |
| 12 | Primavera 12 |  | ● | ● |  |  |  |  |  |  |  |  |  | 2 | 2 |
| 13 | Primavera 13 | ● | ● | ● |  |  |  |  |  |  |  |  |  | 3 | 3 |
| 14 | Primavera 14 | ● | ● | ● |  |  |  |  |  |  |  |  |  | 3 | 3 |
| 15 | Primavera 15 | ● |  | ● |  |  |  |  |  | ● |  |  |  | 3 | 3 |
| 16 | Verão 1 |  |  | ● |  |  |  |  |  | ● |  |  |  | 2 | 2 |
| 17 | Verão 2 |  |  | ● |  |  |  |  |  | ● |  |  |  | 2 | 2 |
| 18 | Verão 3 |  |  | ● |  |  |  |  |  |  |  |  |  | 1 | 1 |
| 19 | Verão 4 | ● |  | ● |  |  |  |  |  |  |  |  |  | 2 | 2 |
| 20 | Verão 5 | ● |  | ● |  | ● |  |  |  |  |  |  |  | 3 | 3 |
| 21 | Verão 6 | ● | ● | ● |  | ● |  |  |  |  | ● |  |  | 5 | 4 |
| 22 | Verão 7 |  | ● | ● |  | ● |  |  |  |  | ● |  |  | 4 | 3 |
| 23 | Verão 8 |  | ● | ● |  | ● |  |  |  |  | ● |  |  | 4 | 3 |
| 24 | Verão 9 |  | ● | ● |  | ● |  |  |  |  | ● |  |  | 4 | 3 |
| 25 | Verão 10 | ● |  | ● |  | ● |  |  |  |  | ● |  |  | 4 | 3 |
| 26 | Verão 11 | ● |  |  |  | ● |  |  |  |  |  |  |  | 2 | 2 |
| 27 | Verão 12 | ● |  |  |  | ● |  |  |  |  |  |  |  | 2 | 2 |
| 28 | Verão 13 |  |  |  |  | ● |  |  |  |  |  |  |  | 1 | 1 |
| 29 | Verão 14 |  |  |  |  | ● |  |  |  |  |  |  |  | 1 | 1 |
| 30 | Verão 15 |  |  |  |  | ● | ● |  |  |  |  |  |  | 2 | 2 |
| 31 | Outono 1 | ● | ● |  |  | ● | ● |  |  |  |  |  |  | 4 | 4 |
| 32 | Outono 2 | ● | ● |  |  | ● | ● |  |  |  |  |  |  | 4 | 4 |
| 33 | Outono 3 | ● | ● |  |  | ● |  |  |  |  |  |  |  | 3 | 3 |
| 34 | Outono 4 |  | ● |  |  | ● |  |  |  |  |  |  |  | 2 | 2 |
| 35 | Outono 5 |  |  |  |  | ● |  |  |  |  |  |  |  | 1 | 1 |
| 36 | Outono 6 |  |  |  |  |  |  |  |  |  |  |  |  | 0 | 0 |
| 37 | Outono 7 | ● |  |  |  |  |  |  |  |  |  |  |  | 1 | 1 |
| 38 | Outono 8 | ● |  |  |  |  |  |  |  |  |  |  |  | 1 | 1 |
| 39 | Outono 9 | ● |  |  |  |  |  |  |  |  |  |  |  | 1 | 1 |
| 40 | Outono 10 |  |  |  | ● |  |  |  |  |  |  |  |  | 1 | 1 |
| 41 | Outono 11 |  | ● |  | ● |  |  |  |  |  | ● |  |  | 3 | 2 |
| 42 | Outono 12 |  | ● |  | ● |  |  |  |  |  | ● |  |  | 3 | 2 |
| 43 | Outono 13 | ● | ● |  |  |  |  |  |  |  | ● |  |  | 3 | 2 |
| 44 | Outono 14 | ● | ● |  |  |  |  |  |  |  | ● |  |  | 3 | 2 |
| 45 | Outono 15 | ● |  |  |  |  |  |  |  |  | ● | ● |  | 3 | 1 |
| 46 | Inverno 1 |  |  |  |  |  |  |  |  |  |  | ● |  | 1 | 0 |
| 47 | Inverno 2 |  |  |  |  |  |  |  |  |  |  | ● |  | 1 | 0 |
| 48 | Inverno 3 |  |  |  |  |  |  |  |  |  |  | ● |  | 1 | 0 |
| 49 | Inverno 4 | ● |  |  |  |  |  |  |  |  |  | ● |  | 2 | 1 |
| 50 | Inverno 5 | ● |  |  |  |  |  | ● |  |  |  |  |  | 2 | 2 |
| 51 | Inverno 6 | ● | ● |  |  |  |  | ● |  |  |  |  |  | 3 | 3 |
| 52 | Inverno 7 |  | ● |  |  |  |  | ● |  |  |  |  |  | 2 | 2 |
| 53 | Inverno 8 |  | ● |  |  |  |  |  |  |  |  |  |  | 1 | 1 |
| 54 | Inverno 9 |  | ● |  |  |  |  |  |  |  |  |  |  | 1 | 1 |
| 55 | Inverno 10 | ● |  |  |  |  |  |  | ● |  |  |  |  | 2 | 2 |
| 56 | Inverno 11 | ● |  |  |  |  |  |  | ● |  |  |  |  | 2 | 2 |
| 57 | Inverno 12 | ● |  |  |  |  |  |  | ● |  |  |  |  | 2 | 2 |
| 58 | Inverno 13 |  |  |  |  |  |  |  |  |  |  |  | ● | 1 | 0 |
| 59 | Inverno 14 |  |  |  |  |  |  |  |  |  |  |  | ● | 1 | 0 |
| 60 | Inverno 15 |  |  |  |  |  |  |  |  |  |  |  | ● | 1 | 0 |

Legenda: 1 = Copa Novato; 2 = Torneio Rookie; 3 = Desafio Champion; 4 = Trio das Trevas; 5 = Trio Tamers; 6 = Copa Ultimate; 7 = Reis do Mar; 8 = Copa Mega; 9 = Trio Celestial; 10 = Trio Guardião 02; 11 = Copa das Lendas; 12 = Copa Mega: Revanche.
