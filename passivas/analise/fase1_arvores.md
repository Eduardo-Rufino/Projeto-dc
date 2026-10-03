# Passivas herdadas — Fase 1: árvores das linhas-piloto (Agumon e Gabumon)

Gerado direto de `Data/Digimon/*.json`, `Data/Evolutions/*.json` e `Data/Jogress/*.json` (estado pós-revisão de evolução, commit `50c1d15` em diante), na branch `passivas-herdadas`. Total no jogo: 199 espécies, 337 ligações de evolução, 3 receitas de Jogress.

Leitura: "Alcançável" = existe um caminho de evolução a partir da espécie-raiz. A árvore repete uma espécie só uma vez; quando ela aparece de novo por outro pai, vem marcada "(já listado acima)".

## Linha do Agumon (id 1)

### De onde o Agumon vem (pré-evoluções)

| id | Espécie | Estágio | Role | Evolui para (dentro desse trecho) |
|---|---|---|---|---|
| 25 | Botamon | Baby | Warrior | Koromon (29) |
| 83 | Jyarimon | Baby | Assassin | Koromon (29) |
| 183 | Chibomon | Baby | Warrior | Koromon (29) |
| 29 | Koromon | In-Training | Warrior | Agumon (1) |

### Árvore a partir do Agumon

```
Agumon (1) · Rookie · Warrior
├─ Greymon (13) · Champion · Warrior
│  ├─ MetalGreymon (39) · Ultimate · Warrior
│  │  ├─ WarGreymon (52) · Mega · Warrior
│  │  └─ ShineGreymon (168) · Mega · Warrior
│  └─ RizeGreymon (167) · Ultimate · Warrior
│     ├─ WarGreymon (52) · Mega · Warrior  (já listado acima)
│     └─ ShineGreymon (168) · Mega · Warrior  (já listado acima)
├─ Devimon (45) · Champion · Assassin
│  ├─ Lucemon Falldown Mode (125) · Ultimate · Assassin
│  │  ├─ Demon (105) · Mega · Assassin
│  │  └─ Lucemon Satan Mode (121) · Mega · Assassin
│  ├─ LadyDevimon (180) · Ultimate · Assassin
│  │  ├─ Medusamon (87) · Mega · Assassin
│  │  └─ Lilithmon (95) · Mega · Support
│  ├─ SkullSatamon (181) · Ultimate · Assassin
│  │  ├─ Demon (105) · Mega · Assassin  (já listado acima)
│  │  └─ Piedmon (194) · Mega · Assassin
│  └─ Myotismon (182) · Ultimate · Assassin
│     ├─ Beelzebumon (106) · Mega · Assassin
│     └─ Piedmon (194) · Mega · Assassin  (já listado acima)
└─ Dimetromon (85) · Champion · Assassin
   ├─ Lamiamon (86) · Ultimate · Assassin
   │  ├─ Medusamon (87) · Mega · Assassin  (já listado acima)
   │  └─ Leviamon (88) · Mega · Tank
   └─ Triceramon (111) · Ultimate · Tank
      ├─ AncientSphinxmon (112) · Mega · Tank
      ├─ Dinomon (120) · Mega · Warrior
      └─ GrandisKuwagamon (200) · Mega · Warrior
```

### Todas as espécies alcançáveis a partir do Agumon (23, fora ele)

Pais = todas as formas que evoluem para ela no jogo inteiro. As que não são alcançáveis a partir do Agumon vão marcadas com *(fora da linha)*. Requisitos = os da ligação pai -> filho.

| id | Espécie | Estágio | Role | Pais (requisitos da ligação) |
|---|---|---|---|---|
| 45 | Devimon | Champion | Assassin | Agumon (1): Nv12, Idade 2, HP 151, PA 70, PD 37, Vel 45, Disc<=45<br>Impmon (10) *(fora da linha)*: Nv12, Idade 2, HP 121, EA 72, Vel 63, Disc<=40<br>Ghostmon (102) *(fora da linha)*: Nv12, Idade 2, HP 100, EA 95, Vel 87, Disc<=40<br>Lucemon (124) *(fora da linha)*: Nv12, Idade 2, HP 97, PA 88, EA 29, Vel 33 |
| 85 | Dimetromon | Champion | Assassin | Agumon (1): Nv12, Idade 2, HP 152, PA 59, PD 38, EA 58, Vel 40<br>Guilmon (7) *(fora da linha)*: Nv12, Idade 13, HP 113, PA 45, PD 34<br>Elizamon (84) *(fora da linha)*: Nv12, Idade 13, HP 87, EA 24, Vel 34<br>Sunarizamon (109) *(fora da linha)*: Nv12, Idade 2, HP 152, PA 81, PD 62, ED 40 |
| 13 | Greymon | Champion | Warrior | Agumon (1): Nv12, Idade 2, HP 150, PA 61, PD 55, Vel 24<br>Agumon (Savers) (11) *(fora da linha)*: Nv12, Idade 2, HP 112, PA 85, PD 35 |
| 180 | LadyDevimon | Ultimate | Assassin | Devimon (45): Nv20, Idade 24, HP 213, PA 95, Vel 78<br>ShoeShoemon (92) *(fora da linha)*: Nv20, Idade 5, HP 223, PD 71, EA 121, ED 37 |
| 86 | Lamiamon | Ultimate | Assassin | Devidramon (47) *(fora da linha)*: Nv20, Idade 24, HP 196, PA 106, Vel 138<br>Dimetromon (85): Nv20, Idade 24, HP 204, EA 48, Vel 62 |
| 125 | Lucemon Falldown Mode | Ultimate | Assassin | Angemon (16) *(fora da linha)*: Nv20, Idade 5, HP 298, EA 167, Vel 124, Disc<=35<br>Devimon (45): Nv20, Idade 5, HP 213, PA 163, Vel 78 |
| 39 | MetalGreymon | Ultimate | Warrior | Greymon (13): Nv20, Idade 5, HP 248, PA 191, EA 30, Vel 36<br>Growlmon (18) *(fora da linha)*: Nv20, Idade 5, HP 258, PA 177, PD 77, EA 60<br>Geogreymon (44) *(fora da linha)*: Nv20, Idade 5, HP 256, PA 149, PD 70 |
| 182 | Myotismon | Ultimate | Assassin | Devimon (45): Nv20, Idade 5, HP 230, EA 100, Vel 150, Disc<=50<br>Bakemon (103) *(fora da linha)*: Nv20, Idade 5, HP 209, EA 148, Vel 67 |
| 167 | RizeGreymon | Ultimate | Warrior | Greymon (13): Nv20, Idade 24, HP 248, PA 121, PD 74, EA 30<br>Geogreymon (44) *(fora da linha)*: Nv20, Idade 24, HP 256, PA 102, Vel 42 |
| 181 | SkullSatamon | Ultimate | Assassin | Devimon (45): Nv20, Idade 5, HP 235, PA 215, Vel 140, Disc<=40 |
| 111 | Triceramon | Ultimate | Tank | Ikkakumon (66) *(fora da linha)*: Nv20, Idade 5, HP 445, PA 156, PD 137, ED 75<br>Dimetromon (85): Nv20, Idade 5, HP 204, PD 94, EA 48, Vel 62<br>Tortamon (110) *(fora da linha)*: Nv20, Idade 24, HP 233, PA 85, Vel 42 |
| 112 | AncientSphinxmon | Mega | Tank | Delumon (98) *(fora da linha)*: Nv28, Idade 8, HP 400, PA 239, Vel 116<br>Triceramon (111): Nv28, Idade 8, HP 660, PA 225, Vel 87 |
| 106 | Beelzebumon | Mega | Assassin | Astamon (127) *(fora da linha)*: Nv28, Idade 8, HP 555, PA 212, Vel 175, Disc<=40<br>Myotismon (182): Nv28, Idade 8, HP 584, EA 239, Vel 141, Disc<=40 |
| 105 | Demon | Mega | Assassin | Destromon (118) *(fora da linha)*: Nv28, Idade 8, HP 548, EA 285, Vel 185, Disc<=35<br>Lucemon Falldown Mode (125): Nv28, Idade 50, HP 387, PA 130, EA 89, Vel 133<br>SkullSatamon (181): Nv28, Idade 8, HP 601, PA 200, Vel 135, Fel<=40 |
| 120 | Dinomon | Mega | Warrior | Triceramon (111): Nv28, Idade 8, HP 678, PA 267, PD 226, ED 146 |
| 200 | GrandisKuwagamon | Mega | Warrior | Triceramon (111): Nv28, Idade 8, HP 678, PA 267, PD 229, ED 100, Vel 120, Disc<=40<br>JewelBeemon (199) *(fora da linha)*: Nv28, Idade 8, HP 393, PA 224, Vel 104 |
| 88 | Leviamon | Mega | Tank | Zudomon (67) *(fora da linha)*: Nv28, Idade 8, HP 768, PA 267, PD 228, ED 129<br>Lamiamon (86): Nv28, Idade 8, HP 510, PA 120, Vel 140, Disc<=30 |
| 95 | Lilithmon | Mega | Support | Angewomon (72) *(fora da linha)*: Nv28, Idade 8, HP 378, EA 112, ED 108, Vel 129<br>LadyDevimon (180): Nv28, Idade 8, HP 530, EA 250, Vel 190, Disc<=30, Fel<=40 |
| 121 | Lucemon Satan Mode | Mega | Assassin | Lucemon Falldown Mode (125): Nv36, Idade 12, HP 425, PD 96, EA 243, Vel 140 |
| 87 | Medusamon | Mega | Assassin | Lamiamon (86): Nv28, Idade 50, HP 370, EA 73, Vel 112<br>LadyDevimon (180): Nv28, Idade 50, HP 380, EA 85, Vel 106 |
| 194 | Piedmon | Mega | Assassin | Fantomon (104) *(fora da linha)*: Nv28, Idade 8, HP 372, EA 126, Vel 189, Disc>=60<br>Mistymon (170) *(fora da linha)*: Nv28, Idade 8, HP 577, PA 190, Vel 196, Disc<=40<br>SkullSatamon (181): Nv28, Idade 8, HP 602, PA 216, Vel 136, Disc<=40<br>Myotismon (182): Nv28, Idade 50, HP 372, EA 152, Vel 118 |
| 168 | ShineGreymon | Mega | Warrior | MetalGreymon (39): Nv28, Idade 50, HP 436, PA 221, PD 121, EA 56<br>RizeGreymon (167): Nv28, Idade 8, HP 451, PA 256, Vel 74 |
| 52 | WarGreymon | Mega | Warrior | MetalGreymon (39): Nv28, Idade 8, HP 436, PA 278, EA 56, Vel 57, 12 batalhas, vitórias 55%, Fel>=55<br>RizeGreymon (167): Nv28, Idade 50, HP 451, PA 178, PD 111 |

Por estágio: Champion 3, Ultimate 8, Mega 12.  
Por Role: Assassin 12, Warrior 7, Tank 3, Support 1.

### Jogress que envolvem a linha

- WarGreymon (52) + MetalGarurumon (53) -> Omegamon (171) · Mega+ · Warrior (o outro pai é de fora da linha)

## Linha do Gabumon (id 2)

### De onde o Gabumon vem (pré-evoluções)

| id | Espécie | Estágio | Role | Evolui para (dentro desse trecho) |
|---|---|---|---|---|
| 25 | Botamon | Baby | Warrior | Tsunomon (30) |
| 26 | Punimon | Baby | Warrior | Tsunomon (30) |
| 63 | Pichimon | Baby | Warrior | Tsunomon (30), Bukamon (64) |
| 30 | Tsunomon | In-Training | Warrior | Gabumon (2) |
| 64 | Bukamon | In-Training | Warrior | Gabumon (2) |

### Árvore a partir do Gabumon

```
Gabumon (2) · Rookie · Ranged
├─ Garurumon (14) · Champion · Ranged
│  ├─ WereGarurumon (40) · Ultimate · Warrior
│  │  ├─ MetalGarurumon (53) · Mega · Warrior
│  │  └─ Gankoomon (162) · Mega · Warrior
│  └─ MachGaogamon (48) · Ultimate · Warrior
│     ├─ Gankoomon (162) · Mega · Warrior  (já listado acima)
│     └─ MirageGaogamon (176) · Mega · Warrior
└─ IceDevimon (46) · Champion · Assassin
   ├─ Destromon (118) · Ultimate · Ranged
   │  ├─ Demon (105) · Mega · Assassin
   │  ├─ Boltmon (113) · Mega · Tank
   │  └─ Belphemon Sleep Mode (128) · Mega · Tank
   │     └─ Belphemon Rage Mode (123) · Mega · Warrior
   └─ Astamon (127) · Ultimate · Ranged
      ├─ Beelzebumon (106) · Mega · Assassin
      └─ Ragnamon (119) · Mega · Ranged
```

### Todas as espécies alcançáveis a partir do Gabumon (15, fora ele)

Pais = todas as formas que evoluem para ela no jogo inteiro. As que não são alcançáveis a partir do Gabumon vão marcadas com *(fora da linha)*. Requisitos = os da ligação pai -> filho.

| id | Espécie | Estágio | Role | Pais (requisitos da ligação) |
|---|---|---|---|---|
| 14 | Garurumon | Champion | Ranged | Gabumon (2): Nv12, Idade 2, HP 120, EA 17, Vel 60<br>Gaomon (5) *(fora da linha)*: Nv12, Idade 13, HP 113, PA 41, PD 31, Vel 21 |
| 46 | IceDevimon | Champion | Assassin | Gabumon (2): Nv12, Idade 2, HP 120, PA 92, Vel 67<br>Phascomon (126) *(fora da linha)*: Nv12, Idade 2, HP 134, PA 61, PD 40, Vel 70, Disc<=40 |
| 127 | Astamon | Ultimate | Ranged | IceDevimon (46): Nv20, Idade 24, HP 173, PA 132, Vel 126<br>Snatchmon (117) *(fora da linha)*: Nv20, Idade 5, HP 202, EA 80, Vel 132 |
| 118 | Destromon | Ultimate | Ranged | IceDevimon (46): Nv20, Idade 5, HP 173, PA 132, Vel 154<br>Snatchmon (117) *(fora da linha)*: Nv20, Idade 24, HP 202, EA 80, Vel 61 |
| 48 | MachGaogamon | Ultimate | Warrior | Garurumon (14): Nv20, Idade 24, HP 247, EA 38, Vel 59<br>Gargomon (19) *(fora da linha)*: Nv20, Idade 5, HP 248, PA 143, Vel 113<br>Gaogamon (23) *(fora da linha)*: Nv20, Idade 24, HP 258, PA 146, Vel 49 |
| 40 | WereGarurumon | Ultimate | Warrior | Garurumon (14): Nv20, Idade 5, HP 247, PA 34, EA 127, Vel 59<br>Gaogamon (23) *(fora da linha)*: Nv20, Idade 5, HP 258, PA 167, PD 73, EA 47, Vel 49 |
| 106 | Beelzebumon | Mega | Assassin | Astamon (127): Nv28, Idade 8, HP 555, PA 212, Vel 175, Disc<=40<br>Myotismon (182) *(fora da linha)*: Nv28, Idade 8, HP 584, EA 239, Vel 141, Disc<=40 |
| 123 | Belphemon Rage Mode | Mega | Warrior | Belphemon Sleep Mode (128): Nv45, Idade 14, HP 752, PA 135, PD 180, ED 141, 15 batalhas, Disc<=40, Fel<=40 |
| 128 | Belphemon Sleep Mode | Mega | Tank | Destromon (118): Nv36, Idade 13, HP 411, PD 89, ED 136, Vel 90 |
| 113 | Boltmon | Mega | Tank | Fantomon (104) *(fora da linha)*: Nv28, Idade 50, HP 372, EA 126, Vel 116<br>Destromon (118): Nv28, Idade 50, HP 373, EA 74, Vel 110 |
| 105 | Demon | Mega | Assassin | Destromon (118): Nv28, Idade 8, HP 548, EA 285, Vel 185, Disc<=35<br>Lucemon Falldown Mode (125) *(fora da linha)*: Nv28, Idade 50, HP 387, PA 130, EA 89, Vel 133<br>SkullSatamon (181) *(fora da linha)*: Nv28, Idade 8, HP 601, PA 200, Vel 135, Fel<=40 |
| 162 | Gankoomon | Mega | Warrior | WereGarurumon (40): Nv28, Idade 50, HP 434, PA 132, PD 90, EA 89<br>MachGaogamon (48): Nv28, Idade 50, HP 443, PA 164, Vel 86<br>SaviorHackmon (165) *(fora da linha)*: Nv28, Idade 50, HP 388, PA 150, PD 130, Vel 128 |
| 53 | MetalGarurumon | Mega | Warrior | WereGarurumon (40): Nv28, Idade 8, HP 434, PA 236, EA 89, Vel 85, 12 batalhas, vitórias 55%, Fel>=55 |
| 176 | MirageGaogamon | Mega | Warrior | MachGaogamon (48): Nv28, Idade 8, HP 443, PA 226, Vel 86, Fel>=40 |
| 119 | Ragnamon | Mega | Ranged | Fantomon (104) *(fora da linha)*: Nv28, Idade 8, HP 372, EA 218, Vel 116<br>Astamon (127): Nv28, Idade 50, HP 375, PA 129, Vel 176 |

Por estágio: Champion 2, Ultimate 4, Mega 9.  
Por Role: Warrior 6, Ranged 4, Assassin 3, Tank 2.

### Jogress que envolvem a linha

- WarGreymon (52) + MetalGarurumon (53) -> Omegamon (171) · Mega+ · Warrior (o outro pai é de fora da linha)

## Cruzamentos entre as duas linhas

Espécies alcançáveis pelas duas linhas: 2: Beelzebumon (106), Demon (105).

Isso importa pro histórico: o mesmo indivíduo de uma espécie compartilhada pode ter vindo de qualquer uma das duas linhas, e só o `EvolutionHistory` dele diz qual.
