# Agenda provisória dos campeonatos (Fase 2)

Gerado a partir da saída de `CalendarSystem.IsOpen` na seção `calendar` do GameAudit (linhas `[AUDIT-AGENDA]`), ou seja, é o que o código calcula de fato, não uma conta à mão. **Nada disso tem efeito no jogo ainda**: todo campeonato continua iniciável a qualquer dia.

Ano de 60 dias = 4 estações de 15. A tabela mostra só o calendário; a trava (`UnlockAfterTournamentId`) é à parte: 10 destrava após vencer o 3, e 11 e 12 após vencer o 8. Todos têm `AnnounceDaysBefore = 5`. Os intervalos recorrentes (6, 10 e 20) dividem 60, então todo ano é igual a este.

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
| 12 | Copa Mega: Revanche | Fixed | dia 60, aberta 3 (60, 1, 2) | após o 8 | 3 |

Por tipo: Recurring 3, Window 2, Fixed 7, Unlockable 0, Always 0.

## Cobertura

- Dias com pelo menos 1 campeonato aberto: **56/60**.
- Dias sem nenhum: 6, 36, 58, 59.
- Maior sequência sem nenhum aberto (contando a virada do ano): **2 dias**.

## Ano completo

`●` = aberto. Coluna "Qtd" = quantos abertos no dia.

| Dia | Estação | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 | 12 | Qtd |
|---:|---|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|---:|
| 1 | Primavera 1 | ● | ● |  |  |  |  |  |  |  | ● |  | ● | 4 |
| 2 | Primavera 2 | ● | ● |  |  |  |  |  |  |  | ● |  | ● | 4 |
| 3 | Primavera 3 | ● | ● |  |  |  |  |  |  |  | ● |  |  | 3 |
| 4 | Primavera 4 |  | ● |  |  |  |  |  |  |  | ● |  |  | 2 |
| 5 | Primavera 5 |  |  |  |  |  |  |  |  |  | ● |  |  | 1 |
| 6 | Primavera 6 |  |  |  |  |  |  |  |  |  |  |  |  | 0 |
| 7 | Primavera 7 | ● |  |  |  |  |  |  |  |  |  |  |  | 1 |
| 8 | Primavera 8 | ● |  |  |  |  |  |  |  |  |  |  |  | 1 |
| 9 | Primavera 9 | ● |  |  |  |  |  |  |  |  |  |  |  | 1 |
| 10 | Primavera 10 |  |  | ● |  |  |  |  |  |  |  |  |  | 1 |
| 11 | Primavera 11 |  | ● | ● |  |  |  |  |  |  |  |  |  | 2 |
| 12 | Primavera 12 |  | ● | ● |  |  |  |  |  |  |  |  |  | 2 |
| 13 | Primavera 13 | ● | ● | ● |  |  |  |  |  |  |  |  |  | 3 |
| 14 | Primavera 14 | ● | ● | ● |  |  |  |  |  |  |  |  |  | 3 |
| 15 | Primavera 15 | ● |  | ● |  |  |  |  |  | ● |  |  |  | 3 |
| 16 | Verão 1 |  |  | ● |  |  |  |  |  | ● |  |  |  | 2 |
| 17 | Verão 2 |  |  | ● |  |  |  |  |  | ● |  |  |  | 2 |
| 18 | Verão 3 |  |  | ● |  |  |  |  |  |  |  |  |  | 1 |
| 19 | Verão 4 | ● |  | ● |  |  |  |  |  |  |  |  |  | 2 |
| 20 | Verão 5 | ● |  | ● |  | ● |  |  |  |  |  |  |  | 3 |
| 21 | Verão 6 | ● | ● | ● |  | ● |  |  |  |  | ● |  |  | 5 |
| 22 | Verão 7 |  | ● | ● |  | ● |  |  |  |  | ● |  |  | 4 |
| 23 | Verão 8 |  | ● | ● |  | ● |  |  |  |  | ● |  |  | 4 |
| 24 | Verão 9 |  | ● | ● |  | ● |  |  |  |  | ● |  |  | 4 |
| 25 | Verão 10 | ● |  | ● |  | ● |  |  |  |  | ● |  |  | 4 |
| 26 | Verão 11 | ● |  |  |  | ● |  |  |  |  |  |  |  | 2 |
| 27 | Verão 12 | ● |  |  |  | ● |  |  |  |  |  |  |  | 2 |
| 28 | Verão 13 |  |  |  |  | ● |  |  |  |  |  |  |  | 1 |
| 29 | Verão 14 |  |  |  |  | ● |  |  |  |  |  |  |  | 1 |
| 30 | Verão 15 |  |  |  |  | ● | ● |  |  |  |  |  |  | 2 |
| 31 | Outono 1 | ● | ● |  |  | ● | ● |  |  |  |  |  |  | 4 |
| 32 | Outono 2 | ● | ● |  |  | ● | ● |  |  |  |  |  |  | 4 |
| 33 | Outono 3 | ● | ● |  |  | ● |  |  |  |  |  |  |  | 3 |
| 34 | Outono 4 |  | ● |  |  | ● |  |  |  |  |  |  |  | 2 |
| 35 | Outono 5 |  |  |  |  | ● |  |  |  |  |  |  |  | 1 |
| 36 | Outono 6 |  |  |  |  |  |  |  |  |  |  |  |  | 0 |
| 37 | Outono 7 | ● |  |  |  |  |  |  |  |  |  |  |  | 1 |
| 38 | Outono 8 | ● |  |  |  |  |  |  |  |  |  |  |  | 1 |
| 39 | Outono 9 | ● |  |  |  |  |  |  |  |  |  |  |  | 1 |
| 40 | Outono 10 |  |  |  | ● |  |  |  |  |  |  |  |  | 1 |
| 41 | Outono 11 |  | ● |  | ● |  |  |  |  |  | ● |  |  | 3 |
| 42 | Outono 12 |  | ● |  | ● |  |  |  |  |  | ● |  |  | 3 |
| 43 | Outono 13 | ● | ● |  |  |  |  |  |  |  | ● |  |  | 3 |
| 44 | Outono 14 | ● | ● |  |  |  |  |  |  |  | ● |  |  | 3 |
| 45 | Outono 15 | ● |  |  |  |  |  |  |  |  | ● | ● |  | 3 |
| 46 | Inverno 1 |  |  |  |  |  |  |  |  |  |  | ● |  | 1 |
| 47 | Inverno 2 |  |  |  |  |  |  |  |  |  |  | ● |  | 1 |
| 48 | Inverno 3 |  |  |  |  |  |  |  |  |  |  | ● |  | 1 |
| 49 | Inverno 4 | ● |  |  |  |  |  |  |  |  |  | ● |  | 2 |
| 50 | Inverno 5 | ● |  |  |  |  |  | ● |  |  |  |  |  | 2 |
| 51 | Inverno 6 | ● | ● |  |  |  |  | ● |  |  |  |  |  | 3 |
| 52 | Inverno 7 |  | ● |  |  |  |  | ● |  |  |  |  |  | 2 |
| 53 | Inverno 8 |  | ● |  |  |  |  |  |  |  |  |  |  | 1 |
| 54 | Inverno 9 |  | ● |  |  |  |  |  |  |  |  |  |  | 1 |
| 55 | Inverno 10 | ● |  |  |  |  |  |  | ● |  |  |  |  | 2 |
| 56 | Inverno 11 | ● |  |  |  |  |  |  | ● |  |  |  |  | 2 |
| 57 | Inverno 12 | ● |  |  |  |  |  |  | ● |  |  |  |  | 2 |
| 58 | Inverno 13 |  |  |  |  |  |  |  |  |  |  |  |  | 0 |
| 59 | Inverno 14 |  |  |  |  |  |  |  |  |  |  |  |  | 0 |
| 60 | Inverno 15 |  |  |  |  |  |  |  |  |  |  |  | ● | 1 |

Legenda: 1 = Copa Novato; 2 = Torneio Rookie; 3 = Desafio Champion; 4 = Trio das Trevas; 5 = Trio Tamers; 6 = Copa Ultimate; 7 = Reis do Mar; 8 = Copa Mega; 9 = Trio Celestial; 10 = Trio Guardião 02; 11 = Copa das Lendas; 12 = Copa Mega: Revanche.
