# Export para guia externo — Projeto DC

Gerado por leitura direta dos JSONs (`Data/Digimon`, `Data/Evolutions`, `Data/Jogress`, `Data/Passives`, `Data/Evolution/evolution_web.json`). Nada foi alterado no jogo.

- Espécies: **199** · Ligações de evolução (`Data/Evolutions`): **337** · Receitas de Jogress: **3** · Passivas próprias: **31**
- Contagem por estágio: Baby 20, InTraining 23, Rookie 32, Champion 35, Ultimate 38, Mega 49, MegaPlus 2
- Notas de leitura: valores percentuais estão em fração no JSON (0.05 = 5%). O atributo aparece como está no dado (`Vacine` é a grafia do enum do jogo). Uma espécie só tem **uma** Role e, se for Support, **um** SupportType — não há espécie multi-Role.

## Índice
1. Espécies (ficha) · 2. Passivas herdadas · 3. Passivas próprias por Role · 4. Pool de passivas por espécie · 5. Árvore de evolução · 6. Receitas de Jogress

## 1. Espécies

Ataque: `Physical`/`Special` (campo `AttackType`). Ovo: campo `EggType`. Jogress: forma obtida por receita de Jogress.

### Baby (20)

| Id | Code | Nome | Role | Suporte | Elemento | Atributo | Ataque | Ovo | Jogress |
|---|---|---|---|---|---|---|---|---|---|
| 25 | botamon | Botamon | Warrior |  | Neutral | Free | Physical | Dragon |  |
| 26 | punimon | Punimon | Warrior |  | Neutral | Free | Special | Beast |  |
| 27 | yuramon | Yuramon | Ranged |  | Neutral | Free | Special | Forest |  |
| 28 | keemon | Keemon | Ranged |  | Neutral | Free | Special | Dark |  |
| 57 | poyomon | Poyomon | Ranged |  | Neutral | Free | Special | Beast |  |
| 63 | pichimon | Pichimon | Warrior |  | Neutral | Free | Physical | Water |  |
| 69 | nyaromon | Nyaromon | Ranged |  | Neutral | Free | Special | Light |  |
| 76 | yolkmon | Yolkmon | Ranged |  | Wind | Data | Special | Dragon |  |
| 83 | jyarimon | Jyarimon | Assassin |  | Dark | Virus | Special | Dragon |  |
| 89 | pafumon | Pafumon | Support | Buffer | Dark | Virus | Special | Dark |  |
| 100 | mokumon | Mokumon | Assassin |  | Dark | Virus | Special | Fire |  |
| 107 | sunamon | Sunamon | Tank |  | Earth | Virus | Physical | Beast |  |
| 114 | kuramon | Kuramon | Ranged |  | Dark | Virus | Special | Dark |  |
| 172 | puttimon | Puttimon | Warrior |  | Fire | Data | Physical | Dragon |  |
| 183 | chibomon | Chibomon | Warrior |  | Neutral | Free | Physical | Dragon |  |
| 185 | dodomon | Dodomon | Warrior |  | Neutral | Free | Physical | Dragon |  |
| 187 | zerimon | Zerimon | Ranged |  | Neutral | Free | Special | Beast |  |
| 188 | pabumon | Pabumon | Ranged |  | Neutral | Free | Special | Forest |  |
| 189 | nyokimon | Nyokimon | Ranged |  | Neutral | Free | Special | Forest |  |
| 195 | leafmon | Leafmon | Ranged |  | Neutral | Free | Special | Forest |  |

### InTraining (23)

| Id | Code | Nome | Role | Suporte | Elemento | Atributo | Ataque | Ovo | Jogress |
|---|---|---|---|---|---|---|---|---|---|
| 29 | koromon | Koromon | Warrior |  | Neutral | Free | Physical | Dragon |  |
| 30 | tsunomon | Tsunomon | Warrior |  | Neutral | Free | Physical | Dragon |  |
| 31 | tokomon | Tokomon | Ranged |  | Neutral | Free | Physical | Dragon |  |
| 32 | motimon | Motimon | Ranged |  | Neutral | Free | Physical | Dragon |  |
| 33 | gigimon | Gigimon | Ranged |  | Neutral | Free | Physical | Dragon |  |
| 34 | gummymon | Gummymon | Ranged |  | Neutral | Free | Physical | Dragon |  |
| 35 | viximon | Viximon | Warrior |  | Neutral | Free | Physical | Beast |  |
| 36 | tanemon | Tanemon | Ranged |  | Neutral | Free | Special | Forest |  |
| 37 | wanyamon | Wanyamon | Warrior |  | Neutral | Free | Physical | Forest |  |
| 38 | yaamon | Yaamon | Warrior |  | Neutral | Free | Physical | Dark |  |
| 58 | yokomon | Yokomon | Ranged |  | Neutral | Free | Physical | Beast |  |
| 64 | bukamon | Bukamon | Warrior |  | Neutral | Free | Physical | Water |  |
| 70 | salamon | Salamon | Ranged |  | Neutral | Free | Physical | Light |  |
| 77 | fluffymon | Fluffymon | Ranged |  | Wind | Data | Special | Dragon |  |
| 90 | kyaromon | Kyaromon | Support | Buffer | Dark | Virus | Special | Dark |  |
| 101 | petimeramon | PetiMeramon | Assassin |  | Dark | Virus | Special | Dark |  |
| 108 | goromon | Goromon | Tank |  | Earth | Virus | Physical | Beast |  |
| 115 | tsumemon | Tsumemon | Ranged |  | Dark | Virus | Special | Dark |  |
| 173 | babydmon | Babydmon | Warrior |  | Fire | Data | Physical | Dragon |  |
| 184 | demiveemon | DemiVeemon | Warrior |  | Neutral | Free | Physical | Dragon |  |
| 186 | dorimon | Dorimon | Warrior |  | Neutral | Free | Physical | Dragon |  |
| 190 | budmon | Budmon | Ranged |  | Neutral | Free | Special | Forest |  |
| 196 | minomon | Minomon | Ranged |  | Neutral | Free | Special | Forest |  |

### Rookie (32)

| Id | Code | Nome | Role | Suporte | Elemento | Atributo | Ataque | Ovo | Jogress |
|---|---|---|---|---|---|---|---|---|---|
| 1 | agumon | Agumon | Warrior |  | Fire | Vacine | Physical | Dragon |  |
| 2 | gabumon | Gabumon | Ranged |  | Fire | Data | Physical | Beast |  |
| 3 | tentomon | Tentomon | Tank |  | Electric | Vacine | Special | Forest |  |
| 4 | patamon | Patamon | Ranged |  | Wind | Data | Physical | Light |  |
| 5 | gaomon | Gaomon | Warrior |  | Wind | Data | Physical | Beast |  |
| 6 | palmon | Palmon | Ranged |  | Earth | Data | Physical | Forest |  |
| 7 | guilmon | Guilmon | Warrior |  | Fire | Virus | Physical | Dragon |  |
| 8 | terriermon | Terriermon | Ranged |  | Wind | Vacine | Special | Beast |  |
| 9 | renamon | Renamon | Assassin |  | Earth | Data | Special | Beast |  |
| 10 | impmon | Impmon | Ranged |  | Dark | Virus | Special | Dark |  |
| 11 | agumon_savers | Agumon (Savers) | Warrior |  | Fire | Vacine | Physical | Dragon |  |
| 12 | lalamon | Lalamon | Support | Healer | Earth | Data | Special | Forest |  |
| 59 | biyomon | Biyomon | Ranged |  | Wind | Vacine | Physical | Beast |  |
| 65 | gomamon | Gomamon | Tank |  | Water | Data | Physical | Water |  |
| 71 | gatomon | Gatomon | Assassin |  | Light | Data | Special | Light |  |
| 78 | pteromon | Pteromon | Ranged |  | Wind | Data | Special | Dragon |  |
| 84 | elizamon | Elizamon | Assassin |  | Dark | Virus | Special | Dragon |  |
| 91 | shoemon | Shoemon | Support | Debuffer | Dark | Virus | Special | Dark |  |
| 96 | muchomon | Muchomon | Warrior |  | Wind | Data | Physical | Beast |  |
| 102 | ghostmon | Ghostmon | Assassin |  | Dark | Virus | Special | Dark |  |
| 109 | sunarizamon | Sunarizamon | Tank |  | Earth | Virus | Physical | Beast |  |
| 116 | bemmon | BEMmon | Ranged |  | Dark | Virus | Special | Dark |  |
| 124 | lucemon | Lucemon | Assassin |  | Dark | Data | Special | Dark |  |
| 126 | phascomon | Phascomon | Tank |  | Dark | Data | Physical | Dark |  |
| 129 | kudamon | Kudamon | Ranged |  | Light | Vacine | Special | Beast |  |
| 132 | dracomon | Dracomon | Warrior |  | Fire | Data | Physical | Dragon |  |
| 139 | veemon | Veemon | Warrior |  | Fire | Vacine | Physical | Dragon |  |
| 145 | kotemon | Kotemon | Warrior |  | Neutral | Data | Physical | Beast |  |
| 149 | dorumon | Dorumon | Warrior |  | Neutral | Data | Physical | Dragon |  |
| 163 | hackmon | Hackmon | Assassin |  | Neutral | Data | Physical | Dark |  |
| 169 | candlemon | Candlemon | Ranged |  | Fire | Virus | Special | Fire |  |
| 197 | wormmon | Wormmon | Assassin |  | Plant | Vacine | Physical | Forest |  |

### Champion (35)

| Id | Code | Nome | Role | Suporte | Elemento | Atributo | Ataque | Ovo | Jogress |
|---|---|---|---|---|---|---|---|---|---|
| 13 | greymon | Greymon | Warrior |  | Fire | Vacine | Physical | Dragon |  |
| 14 | garurumon | Garurumon | Ranged |  | Fire | Vacine | Special | Beast |  |
| 15 | kabuterimon | Kabuterimon | Tank |  | Electric | Vacine | Special | Forest |  |
| 16 | angemon | Angemon | Ranged |  | Light | Vacine | Special | Light |  |
| 17 | togemon | Togemon | Warrior |  | Earth | Data | Physical | Forest |  |
| 18 | growlmon | Growlmon | Warrior |  | Fire | Virus | Physical | Dragon |  |
| 19 | gargomon | Gargomon | Ranged |  | Wind | Vacine | Physical | Beast |  |
| 20 | kyubimon | Kyubimon | Ranged |  | Fire | Data | Special | Beast |  |
| 21 | wizardmon | Wizardmon | Ranged |  | Dark | Virus | Special | Dark |  |
| 23 | gaogamon | Gaogamon | Warrior |  | Wind | Data | Physical | Beast |  |
| 24 | sunflowmon | Sunflowmon | Support | Debuffer | Earth | Data | Special | Forest |  |
| 44 | geogreymon | Geogreymon | Warrior |  | Fire | Vacine | Physical | Dragon |  |
| 45 | devimon | Devimon | Assassin |  | Dark | Virus | Physical | Dark |  |
| 46 | icedevimon | IceDevimon | Assassin |  | Dark | Virus | Physical | Dark |  |
| 47 | devidramon | Devidramon | Assassin |  | Dark | Virus | Physical | Dragon |  |
| 60 | birdramon | Birdramon | Ranged |  | Wind | Vacine | Special | Beast |  |
| 66 | ikkakumon | Ikkakumon | Tank |  | Water | Data | Physical | Water |  |
| 79 | galemon | Galemon | Ranged |  | Wind | Data | Special | Dragon |  |
| 85 | dimetromon | Dimetromon | Assassin |  | Dark | Virus | Special | Dragon |  |
| 92 | shoeshoemon | ShoeShoemon | Support | Buffer | Dark | Virus | Special | Dark |  |
| 97 | cockatrimon | Cockatrimon | Warrior |  | Wind | Data | Physical | Beast |  |
| 103 | bakemon | Bakemon | Assassin |  | Dark | Virus | Special | Dark |  |
| 110 | tortamon | Tortamon | Tank |  | Earth | Virus | Physical | Beast |  |
| 117 | snatchmon | Snatchmon | Ranged |  | Dark | Virus | Special | Dark |  |
| 130 | reppamon | Reppamon | Ranged |  | Light | Vacine | Special | Beast |  |
| 133 | coredramon_green | Coredramon (Green) | Warrior |  | Earth | Data | Physical | Dragon |  |
| 136 | coredramon_blue | Coredramon (Blue) | Ranged |  | Wind | Data | Physical | Dragon |  |
| 140 | veedramon | Veedramon | Warrior |  | Fire | Vacine | Physical | Dragon |  |
| 146 | gladimon | Gladimon | Warrior |  | Neutral | Data | Physical | Beast |  |
| 150 | dorugamon | Dorugamon | Warrior |  | Wind | Data | Physical | Dragon |  |
| 153 | raptordramon | Raptordramon | Warrior |  | Earth | Data | Physical | Dragon |  |
| 156 | dexdorugamon | DexDorugamon | Assassin |  | Dark | Virus | Physical | Dragon |  |
| 164 | baohackmon | BaoHackmon | Assassin |  | Neutral | Data | Physical | Dark |  |
| 191 | exveemon | ExVeemon | Warrior |  | Fire | Vacine | Physical | Dragon |  |
| 198 | stingmon | Stingmon | Assassin |  | Wind | Vacine | Physical | Forest |  |

### Ultimate (38)

| Id | Code | Nome | Role | Suporte | Elemento | Atributo | Ataque | Ovo | Jogress |
|---|---|---|---|---|---|---|---|---|---|
| 39 | metalgreymon | MetalGreymon | Warrior |  | Fire | Vacine | Physical | Dragon |  |
| 40 | weregarurumon | WereGarurumon | Warrior |  | Fire | Vacine | Physical | Beast |  |
| 41 | wargrowlmon | WarGrowlmon | Warrior |  | Fire | Virus | Physical | Dragon |  |
| 42 | rapidmon | Rapidmon | Ranged |  | Wind | Vacine | Physical | Beast |  |
| 43 | magnaangemon | MagnaAngemon | Ranged |  | Light | Vacine | Special | Light |  |
| 48 | machgaogamon | MachGaogamon | Warrior |  | Wind | Data | Physical | Beast |  |
| 49 | taomon | Taomon | Ranged |  | Fire | Data | Special | Beast |  |
| 50 | lillymon | Lillymon | Ranged |  | Plant | Data | Special | Forest |  |
| 51 | megakabuterimon | MegaKabuterimon | Tank |  | Electric | Vacine | Special | Forest |  |
| 61 | garudamon | Garudamon | Ranged |  | Wind | Vacine | Special | Beast |  |
| 67 | zudomon | Zudomon | Tank |  | Water | Data | Physical | Water |  |
| 72 | angewomon | Angewomon | Ranged |  | Light | Vacine | Special | Light |  |
| 80 | grandgalemon | Grand Galemon | Ranged |  | Wind | Data | Special | Dragon |  |
| 86 | lamiamon | Lamiamon | Assassin |  | Dark | Virus | Special | Dragon |  |
| 93 | chaperomon | Chaperomon | Support | Buffer | Dark | Virus | Special | Dark |  |
| 98 | delumon | Delumon | Warrior |  | Wind | Data | Physical | Beast |  |
| 104 | fantomon | Fantomon | Assassin |  | Dark | Virus | Special | Dark |  |
| 111 | triceramon | Triceramon | Tank |  | Earth | Virus | Physical | Beast |  |
| 118 | destromon | Destromon | Ranged |  | Dark | Virus | Special | Dark |  |
| 125 | lucemon_falldown | Lucemon Falldown Mode | Assassin |  | Dark | Virus | Special | Dark |  |
| 127 | astamon | Astamon | Ranged |  | Dark | Virus | Physical | Dark |  |
| 131 | chirinmon | Chirinmon | Ranged |  | Light | Vacine | Special | Beast |  |
| 134 | groundramon | Groundramon | Tank |  | Earth | Data | Physical | Dragon |  |
| 137 | wingdramon | Wingdramon | Ranged |  | Wind | Data | Physical | Dragon |  |
| 141 | aeroveedramon | AeroVeedramon | Ranged |  | Wind | Vacine | Physical | Dragon |  |
| 147 | knightmon | Knightmon | Tank |  | Neutral | Data | Physical | Beast |  |
| 151 | dorugreymon | Dorugreymon | Warrior |  | Fire | Data | Physical | Dragon |  |
| 154 | grademon | Grademon | Tank |  | Earth | Virus | Physical | Dragon |  |
| 157 | dexdorugreymon | DexDorugreymon | Assassin |  | Dark | Virus | Physical | Dragon |  |
| 165 | saviorhackmon | SaviorHackmon | Assassin |  | Light | Virus | Physical | Dark |  |
| 167 | rizegreymon | RizeGreymon | Warrior |  | Fire | Vacine | Physical | Dragon |  |
| 170 | mistymon | Mistymon | Assassin |  | Dark | Data | Physical | Fire |  |
| 179 | lilamon | Lilamon | Support | Healer | Plant | Data | Special | Forest |  |
| 180 | ladydevimon | LadyDevimon | Assassin |  | Dark | Virus | Special | Dark |  |
| 181 | skullsatamon | SkullSatamon | Assassin |  | Dark | Virus | Physical | Dark |  |
| 182 | myotismon | Myotismon | Assassin |  | Dark | Virus | Special | Dark |  |
| 192 | paildramon | Paildramon | Ranged |  | Fire | Vacine | Physical | Dragon | sim |
| 199 | jewelbeemon | JewelBeemon | Assassin |  | Wind | Vacine | Physical | Forest |  |

### Mega (49)

| Id | Code | Nome | Role | Suporte | Elemento | Atributo | Ataque | Ovo | Jogress |
|---|---|---|---|---|---|---|---|---|---|
| 52 | wargreymon | WarGreymon | Warrior |  | Fire | Vacine | Physical | Dragon |  |
| 53 | metalgarurumon | MetalGarurumon | Warrior |  | Fire | Vacine | Physical | Beast |  |
| 54 | gallantmon | Gallantmon | Warrior |  | Fire | Virus | Physical | Dragon |  |
| 55 | megagargomon | MegaGargomon | Tank |  | Wind | Vacine | Physical | Beast |  |
| 56 | seraphimon | Seraphimon | Ranged |  | Light | Vacine | Special | Light |  |
| 62 | hououmon | Hououmon | Ranged |  | Light | Vacine | Special | Beast |  |
| 68 | vikemon | Vikemon | Tank |  | Water | Data | Physical | Water |  |
| 73 | magnadramon | Magnadramon | Ranged |  | Light | Vacine | Special | Light |  |
| 74 | herculeskabuterimon | HerculesKabuterimon | Tank |  | Electric | Vacine | Special | Forest |  |
| 75 | rosemon | Rosemon | Ranged |  | Plant | Data | Special | Forest |  |
| 81 | zephagamon | Zephagamon | Ranged |  | Wind | Data | Special | Dragon |  |
| 82 | pinochimon | Pinochimon | Ranged |  | Wind | Data | Special | Dragon |  |
| 87 | medusamon | Medusamon | Assassin |  | Dark | Virus | Special | Dragon |  |
| 88 | leviamon | Leviamon | Tank |  | Dark | Data | Physical | Dragon |  |
| 94 | cendrillmon | Cendrillmon | Support | Buffer | Dark | Virus | Special | Dark |  |
| 95 | lilithmon | Lilithmon | Support | Healer | Dark | Virus | Special | Dark |  |
| 99 | medievaldukemon | Medieval Dukemon | Warrior |  | Wind | Data | Physical | Beast |  |
| 105 | demon | Demon | Assassin |  | Dark | Data | Special | Dark |  |
| 106 | beelzebumon | Beelzebumon | Assassin |  | Dark | Virus | Special | Dark |  |
| 112 | ancientsphinxmon | AncientSphinxmon | Tank |  | Earth | Virus | Physical | Beast |  |
| 113 | boltmon | Boltmon | Tank |  | Earth | Virus | Physical | Beast |  |
| 119 | ragnamon | Ragnamon | Ranged |  | Dark | Virus | Special | Dark |  |
| 120 | dinomon | Dinomon | Warrior |  | Fire | Vacine | Physical | Dragon |  |
| 121 | lucemon_satan | Lucemon Satan Mode | Assassin |  | Dark | Unknown | Special | Dark |  |
| 122 | barbamon | Barbamon | Ranged |  | Dark | Virus | Special | Dark |  |
| 123 | belphemon_rage | Belphemon Rage Mode | Warrior |  | Dark | Virus | Physical | Dark |  |
| 128 | belphemon | Belphemon Sleep Mode | Tank |  | Dark | Data | Physical | Dark |  |
| 135 | breakdramon | Breakdramon | Warrior |  | Earth | Virus | Physical | Dragon |  |
| 138 | slayerdramon | SlayerDramon | Assassin |  | Wind | Virus | Physical | Dragon |  |
| 142 | ulforceveedramon | UlforceVeedramon | Assassin |  | Light | Free | Physical | Dragon |  |
| 143 | magnamon | Magnamon | Warrior |  | Light | Vacine | Physical | Dragon |  |
| 144 | kentaurusmon | Kentaurusmon | Ranged |  | Light | Vacine | Special | Beast |  |
| 148 | crusadermon | Crusadermon | Warrior |  | Neutral | Virus | Physical | Beast |  |
| 152 | alphamon | Alphamon | Assassin |  | Light | Unknown | Physical | Dragon |  |
| 155 | dorugoramon | Dorugoramon | Tank |  | Earth | Virus | Physical | Dragon |  |
| 158 | dexdorugoramon | DexDorugoramon | Assassin |  | Dark | Virus | Physical | Dragon |  |
| 159 | craniummon | Craniummon | Tank |  | Neutral | Vacine | Physical | Beast |  |
| 160 | dynasmon | Dynasmon | Warrior |  | Wind | Vacine | Physical | Dragon |  |
| 161 | duftmon | Duftmon | Assassin |  | Neutral | Data | Physical | Beast |  |
| 162 | gankoomon | Gankoomon | Warrior |  | Fire | Vacine | Physical | Beast |  |
| 166 | jesmon | Jesmon | Assassin |  | Light | Unknown | Physical | Dark |  |
| 168 | shinegreymon | ShineGreymon | Warrior |  | Fire | Vacine | Physical | Dragon |  |
| 175 | sakuyamon | Sakuyamon | Ranged |  | Light | Data | Special | Beast |  |
| 176 | miragegaogamon | MirageGaogamon | Warrior |  | Wind | Data | Physical | Beast |  |
| 177 | sleipmon | Sleipmon | Warrior |  | Light | Vacine | Physical | Beast |  |
| 178 | ophanimon | Ophanimon | Ranged |  | Light | Vacine | Special | Light |  |
| 193 | imperialdramon | Imperialdramon | Warrior |  | Fire | Free | Physical | Dragon |  |
| 194 | piedmon | Piedmon | Assassin |  | Dark | Virus | Special | Dark |  |
| 200 | grandiskuwagamon | GrandisKuwagamon | Warrior |  | Dark | Virus | Physical | Forest |  |

### MegaPlus (2)

| Id | Code | Nome | Role | Suporte | Elemento | Atributo | Ataque | Ovo | Jogress |
|---|---|---|---|---|---|---|---|---|---|
| 171 | omegamon | Omegamon | Warrior |  | Light | Vacine | Physical | Light | sim |
| 174 | examon | Examon | Warrior |  | Fire | Virus | Physical | Dragon | sim |

## 2. Passivas herdadas (`InheritedPassive`)

Cada espécie deixa no máximo uma passiva herdada; o Digimon acumula a de toda espécie pela qual já passou (histórico de evolução). Texto da ficha in-game = `InheritedPassiveSystem.Describe`.

### 2.1 Tipos de efeito

| Tipo | Significado (Value / Value2) | Espécies |
|---|---|---|
| MaxHpPercent | Value = +% HP máx | 15 |
| SpeedPercent | Value = +% Velocidade | 14 |
| PhysicalAttackPercent | Value = +% PA | 15 |
| SpecialAttackPercent | Value = +% EA | 11 |
| PhysicalDefensePercent | Value = +% PD | 14 |
| SpecialDefensePercent | Value = +% ED | 12 |
| PhysicalDamageDealtPercent | Value = +% dano físico final | 1 |
| CritShortensDebuffs | Value = ações removidas dos debuffs ao critar | 2 |
| PeriodicDefensePierce | Value = a cada N ataques; Value2 = % de defesa ignorada | 6 |
| SpeedAndBonusVsActedTarget | Value = +% Velocidade; Value2 = +% dano em alvo que já agiu | 7 |
| RangedExtraStrike | Value = chance; Value2 = % do dano do golpe extra | 2 |
| FirstHitTakenReduction | Value = % de redução do 1º golpe recebido | 3 |
| LowHpPhysicalDamageBonus | Value = limiar de HP; Value2 = +% dano físico | 5 |
| CritHealMaxHp | Value = % do HP máx curado no crítico | 1 |
| KillSpeedBonus | Value = +% Velocidade ao derrotar | 5 |
| DodgeThenGuaranteedCrit | Value = chance de esquiva | 5 |
| CritLifesteal | Value = % do dano drenado no crítico | 5 |
| HealLowestAllyOnAttack | Value = % do dano curado no aliado mais ferido | 4 |
| AntiTank | Value = +% dano em Tank; Value2 = multiplicador do dano recebido de Tank | 1 |
| NegateDamageChance | Value = chance de anular o golpe | 6 |
| PhysicalDamageWithOpeningBonus | Value = +% dano físico; Value2 = +% no 1º golpe | 3 |
| OpeningHitBonus | Value = +% no 1º golpe | 7 |
| RangedStacksGuaranteedCrit | Value = acúmulos para crítico garantido | 1 |
| StunChance | Value = chance; Value2 = chance em alvo <50% HP | 3 |
| DefensesUpSpeedDown | Value = +% PD e ED; Value2 = % de Velocidade (negativo) | 1 |

### 2.2 Por espécie

| Id | Nome | Estágio | Tipo | Value | Value2 | Efeito |
|---|---|---|---|---|---|---|
| 25 | Botamon | Baby | MaxHpPercent | 0.03 | 0 | +3% de HP máximo (na luta, o dano recebido é dividido por 1.03). |
| 26 | Punimon | Baby | MaxHpPercent | 0.03 | 0 | +3% de HP máximo (na luta, o dano recebido é dividido por 1.03). |
| 27 | Yuramon | Baby | SpecialDefensePercent | 0.035 | 0 | +3.5% de Defesa Especial (ED). |
| 28 | Keemon | Baby | SpeedPercent | 0.035 | 0 | +3.5% de Velocidade. |
| 57 | Poyomon | Baby | SpecialAttackPercent | 0.035 | 0 | +3.5% de Ataque Especial (EA). |
| 63 | Pichimon | Baby | SpecialDefensePercent | 0.03 | 0 | +3% de Defesa Especial (ED). |
| 69 | Nyaromon | Baby | MaxHpPercent | 0.035 | 0 | +3.5% de HP máximo (na luta, o dano recebido é dividido por 1.035). |
| 76 | Yolkmon | Baby | SpecialAttackPercent | 0.025 | 0 | +2.5% de Ataque Especial (EA). |
| 83 | Jyarimon | Baby | SpeedPercent | 0.03 | 0 | +3% de Velocidade. |
| 89 | Pafumon | Baby | SpecialDefensePercent | 0.025 | 0 | +2.5% de Defesa Especial (ED). |
| 100 | Mokumon | Baby | SpeedPercent | 0.025 | 0 | +2.5% de Velocidade. |
| 107 | Sunamon | Baby | PhysicalAttackPercent | 0.035 | 0 | +3.5% de Ataque Físico (PA). |
| 114 | Kuramon | Baby | SpecialAttackPercent | 0.03 | 0 | +3% de Ataque Especial (EA). |
| 172 | Puttimon | Baby | PhysicalDefensePercent | 0.035 | 0 | +3.5% de Defesa Física (PD). |
| 183 | Chibomon | Baby | PhysicalAttackPercent | 0.03 | 0 | +3% de Ataque Físico (PA). |
| 185 | Dodomon | Baby | PhysicalDefensePercent | 0.02 | 0 | +2% de Defesa Física (PD). |
| 187 | Zerimon | Baby | MaxHpPercent | 0.025 | 0 | +2.5% de HP máximo (na luta, o dano recebido é dividido por 1.025). |
| 188 | Pabumon | Baby | PhysicalDefensePercent | 0.025 | 0 | +2.5% de Defesa Física (PD). |
| 189 | Nyokimon | Baby | SpeedPercent | 0.02 | 0 | +2% de Velocidade. |
| 195 | Leafmon | Baby | PhysicalDefensePercent | 0.03 | 0 | +3% de Defesa Física (PD). |
| 29 | Koromon | InTraining | MaxHpPercent | 0.04 | 0 | +4% de HP máximo (na luta, o dano recebido é dividido por 1.04). |
| 30 | Tsunomon | InTraining | PhysicalDefensePercent | 0.04 | 0 | +4% de Defesa Física (PD). |
| 31 | Tokomon | InTraining | PhysicalAttackPercent | 0.045 | 0 | +4.5% de Ataque Físico (PA). |
| 32 | Motimon | InTraining | SpeedPercent | 0.035 | 0 | +3.5% de Velocidade. |
| 33 | Gigimon | InTraining | PhysicalAttackPercent | 0.04 | 0 | +4% de Ataque Físico (PA). |
| 34 | Gummymon | InTraining | MaxHpPercent | 0.045 | 0 | +4.5% de HP máximo (na luta, o dano recebido é dividido por 1.045). |
| 35 | Viximon | InTraining | SpeedPercent | 0.045 | 0 | +4.5% de Velocidade. |
| 36 | Tanemon | InTraining | SpecialAttackPercent | 0.045 | 0 | +4.5% de Ataque Especial (EA). |
| 37 | Wanyamon | InTraining | PhysicalAttackPercent | 0.035 | 0 | +3.5% de Ataque Físico (PA). |
| 38 | Yaamon | InTraining | MaxHpPercent | 0.035 | 0 | +3.5% de HP máximo (na luta, o dano recebido é dividido por 1.035). |
| 58 | Yokomon | InTraining | PhysicalDefensePercent | 0.045 | 0 | +4.5% de Defesa Física (PD). |
| 64 | Bukamon | InTraining | SpeedPercent | 0.04 | 0 | +4% de Velocidade. |
| 70 | Salamon | InTraining | SpecialDefensePercent | 0.04 | 0 | +4% de Defesa Especial (ED). |
| 77 | Fluffymon | InTraining | SpecialAttackPercent | 0.035 | 0 | +3.5% de Ataque Especial (EA). |
| 90 | Kyaromon | InTraining | SpecialDefensePercent | 0.045 | 0 | +4.5% de Defesa Especial (ED). |
| 101 | PetiMeramon | InTraining | SpeedPercent | 0.05 | 0 | +5% de Velocidade. |
| 108 | Goromon | InTraining | PhysicalDefensePercent | 0.035 | 0 | +3.5% de Defesa Física (PD). |
| 115 | Tsumemon | InTraining | SpecialAttackPercent | 0.04 | 0 | +4% de Ataque Especial (EA). |
| 173 | Babydmon | InTraining | PhysicalAttackPercent | 0.05 | 0 | +5% de Ataque Físico (PA). |
| 184 | DemiVeemon | InTraining | MaxHpPercent | 0.05 | 0 | +5% de HP máximo (na luta, o dano recebido é dividido por 1.05). |
| 186 | Dorimon | InTraining | SpecialDefensePercent | 0.035 | 0 | +3.5% de Defesa Especial (ED). |
| 190 | Budmon | InTraining | MaxHpPercent | 0.03 | 0 | +3% de HP máximo (na luta, o dano recebido é dividido por 1.03). |
| 196 | Minomon | InTraining | SpecialDefensePercent | 0.03 | 0 | +3% de Defesa Especial (ED). |
| 1 | Agumon | Rookie | PhysicalDamageDealtPercent | 0.05 | 0 | +5% no dano final de ataques físicos. |
| 2 | Gabumon | Rookie | PhysicalAttackPercent | 0.05 | 0 | +5% de Ataque Físico (PA). |
| 3 | Tentomon | Rookie | PhysicalDefensePercent | 0.05 | 0 | +5% de Defesa Física (PD). |
| 4 | Patamon | Rookie | SpeedPercent | 0.05 | 0 | +5% de Velocidade. |
| 5 | Gaomon | Rookie | PhysicalAttackPercent | 0.055 | 0 | +5.5% de Ataque Físico (PA). |
| 6 | Palmon | Rookie | SpecialDefensePercent | 0.045 | 0 | +4.5% de Defesa Especial (ED). |
| 7 | Guilmon | Rookie | MaxHpPercent | 0.055 | 0 | +5.5% de HP máximo (na luta, o dano recebido é dividido por 1.055). |
| 8 | Terriermon | Rookie | SpecialAttackPercent | 0.05 | 0 | +5% de Ataque Especial (EA). |
| 9 | Renamon | Rookie | SpeedPercent | 0.06 | 0 | +6% de Velocidade. |
| 10 | Impmon | Rookie | SpecialAttackPercent | 0.045 | 0 | +4.5% de Ataque Especial (EA). |
| 11 | Agumon (Savers) | Rookie | PhysicalAttackPercent | 0.045 | 0 | +4.5% de Ataque Físico (PA). |
| 12 | Lalamon | Rookie | SpecialDefensePercent | 0.055 | 0 | +5.5% de Defesa Especial (ED). |
| 59 | Biyomon | Rookie | SpeedPercent | 0.04 | 0 | +4% de Velocidade. |
| 65 | Gomamon | Rookie | PhysicalDefensePercent | 0.055 | 0 | +5.5% de Defesa Física (PD). |
| 71 | Gatomon | Rookie | SpecialAttackPercent | 0.055 | 0 | +5.5% de Ataque Especial (EA). |
| 78 | Pteromon | Rookie | SpeedPercent | 0.055 | 0 | +5.5% de Velocidade. |
| 84 | Elizamon | Rookie | MaxHpPercent | 0.04 | 0 | +4% de HP máximo (na luta, o dano recebido é dividido por 1.04). |
| 91 | Shoemon | Rookie | SpecialDefensePercent | 0.06 | 0 | +6% de Defesa Especial (ED). |
| 96 | Muchomon | Rookie | PhysicalAttackPercent | 0.06 | 0 | +6% de Ataque Físico (PA). |
| 102 | Ghostmon | Rookie | SpecialAttackPercent | 0.04 | 0 | +4% de Ataque Especial (EA). |
| 109 | Sunarizamon | Rookie | PhysicalDefensePercent | 0.06 | 0 | +6% de Defesa Física (PD). |
| 116 | BEMmon | Rookie | MaxHpPercent | 0.045 | 0 | +4.5% de HP máximo (na luta, o dano recebido é dividido por 1.045). |
| 124 | Lucemon | Rookie | SpecialAttackPercent | 0.06 | 0 | +6% de Ataque Especial (EA). |
| 126 | Phascomon | Rookie | MaxHpPercent | 0.06 | 0 | +6% de HP máximo (na luta, o dano recebido é dividido por 1.06). |
| 129 | Kudamon | Rookie | SpecialDefensePercent | 0.04 | 0 | +4% de Defesa Especial (ED). |
| 132 | Dracomon | Rookie | PhysicalAttackPercent | 0.04 | 0 | +4% de Ataque Físico (PA). |
| 139 | Veemon | Rookie | MaxHpPercent | 0.05 | 0 | +5% de HP máximo (na luta, o dano recebido é dividido por 1.05). |
| 145 | Kotemon | Rookie | PhysicalDefensePercent | 0.045 | 0 | +4.5% de Defesa Física (PD). |
| 149 | Dorumon | Rookie | SpeedPercent | 0.045 | 0 | +4.5% de Velocidade. |
| 163 | Hackmon | Rookie | MaxHpPercent | 0.035 | 0 | +3.5% de HP máximo (na luta, o dano recebido é dividido por 1.035). |
| 169 | Candlemon | Rookie | SpecialDefensePercent | 0.05 | 0 | +5% de Defesa Especial (ED). |
| 197 | Wormmon | Rookie | PhysicalDefensePercent | 0.04 | 0 | +4% de Defesa Física (PD). |
| 13 | Greymon | Champion | CritShortensDebuffs | 1 | 0 | Ao acertar um crítico, cada efeito negativo ativo no Digimon perde 1 ação(ões) de duração. |
| 14 | Garurumon | Champion | RangedExtraStrike | 0.15 | 0.3 | Em ataques à distância, 15% de chance de um golpe extra imediato com 30% do dano. |
| 15 | Kabuterimon | Champion | NegateDamageChance | 0.15 | 0 | 15% de chance de ignorar todo o dano de um golpe recebido. |
| 16 | Angemon | Champion | FirstHitTakenReduction | 0.25 | 0 | O primeiro golpe recebido na luta causa 25% a menos de dano. |
| 17 | Togemon | Champion | PeriodicDefensePierce | 4 | 0.15 | A cada 4 ataques, o próximo ignora 15% da defesa do alvo. |
| 18 | Growlmon | Champion | LowHpPhysicalDamageBonus | 0.5 | 0.12 | Ao cair abaixo de 50% de HP, ganha +12% de dano físico pelo resto da luta. |
| 19 | Gargomon | Champion | PhysicalDamageWithOpeningBonus | 0.06 | 0.12 | +6% de dano físico (+12% no primeiro golpe desferido na luta). |
| 20 | Kyubimon | Champion | OpeningHitBonus | 0.08 | 0 | +8% de dano no primeiro golpe desferido na luta. |
| 21 | Wizardmon | Champion | DodgeThenGuaranteedCrit | 0.05 | 0 | 5% de chance de esquivar; depois de esquivar, o próximo ataque é crítico garantido. |
| 23 | Gaogamon | Champion | SpeedAndBonusVsActedTarget | 0.04 | 0.06 | +4% de Velocidade e +6% de dano contra alvos que já agiram na luta. |
| 24 | Sunflowmon | Champion | SpeedAndBonusVsActedTarget | 0.04 | 0.09 | +4% de Velocidade e +9% de dano contra alvos que já agiram na luta. |
| 44 | Geogreymon | Champion | PhysicalAttackPercent | 0.06 | 0 | +6% de Ataque Físico (PA). |
| 45 | Devimon | Champion | PeriodicDefensePierce | 3 | 0.2 | A cada 3 ataques, o próximo ignora 20% da defesa do alvo. |
| 46 | IceDevimon | Champion | FirstHitTakenReduction | 0.2 | 0 | O primeiro golpe recebido na luta causa 20% a menos de dano. |
| 47 | Devidramon | Champion | CritShortensDebuffs | 1 | 0 | Ao acertar um crítico, cada efeito negativo ativo no Digimon perde 1 ação(ões) de duração. |
| 60 | Birdramon | Champion | CritLifesteal | 0.05 | 0 | Ao acertar um crítico, cura 5% do dano causado. |
| 66 | Ikkakumon | Champion | PhysicalDefensePercent | 0.06 | 0 | +6% de Defesa Física (PD). |
| 79 | Galemon | Champion | SpeedAndBonusVsActedTarget | 0.05 | 0.09 | +5% de Velocidade e +9% de dano contra alvos que já agiram na luta. |
| 85 | Dimetromon | Champion | SpeedAndBonusVsActedTarget | 0.05 | 0.08 | +5% de Velocidade e +8% de dano contra alvos que já agiram na luta. |
| 92 | ShoeShoemon | Champion | HealLowestAllyOnAttack | 0.06 | 0 | A cada ataque, cura o aliado (outro que não ele) com menor % de HP em 6% do dano causado. |
| 97 | Cockatrimon | Champion | StunChance | 0.08 | 0.16 | 8% de chance de atordoar o alvo (16% se o alvo estiver abaixo de 50% de HP). |
| 103 | Bakemon | Champion | DodgeThenGuaranteedCrit | 0.04 | 0 | 4% de chance de esquivar; depois de esquivar, o próximo ataque é crítico garantido. |
| 110 | Tortamon | Champion | NegateDamageChance | 0.18 | 0 | 18% de chance de ignorar todo o dano de um golpe recebido. |
| 117 | Snatchmon | Champion | CritLifesteal | 0.04 | 0 | Ao acertar um crítico, cura 4% do dano causado. |
| 130 | Reppamon | Champion | OpeningHitBonus | 0.07 | 0 | +7% de dano no primeiro golpe desferido na luta. |
| 133 | Coredramon (Green) | Champion | PhysicalAttackPercent | 0.065 | 0 | +6.5% de Ataque Físico (PA). |
| 136 | Coredramon (Blue) | Champion | PhysicalDamageWithOpeningBonus | 0.07 | 0.14 | +7% de dano físico (+14% no primeiro golpe desferido na luta). |
| 140 | Veedramon | Champion | LowHpPhysicalDamageBonus | 0.5 | 0.1 | Ao cair abaixo de 50% de HP, ganha +10% de dano físico pelo resto da luta. |
| 146 | Gladimon | Champion | PeriodicDefensePierce | 4 | 0.18 | A cada 4 ataques, o próximo ignora 18% da defesa do alvo. |
| 150 | Dorugamon | Champion | SpeedPercent | 0.06 | 0 | +6% de Velocidade. |
| 153 | Raptordramon | Champion | SpeedAndBonusVsActedTarget | 0.05 | 0.07 | +5% de Velocidade e +7% de dano contra alvos que já agiram na luta. |
| 156 | DexDorugamon | Champion | PeriodicDefensePierce | 3 | 0.22 | A cada 3 ataques, o próximo ignora 22% da defesa do alvo. |
| 164 | BaoHackmon | Champion | OpeningHitBonus | 0.09 | 0 | +9% de dano no primeiro golpe desferido na luta. |
| 191 | ExVeemon | Champion | PhysicalAttackPercent | 0.07 | 0 | +7% de Ataque Físico (PA). |
| 198 | Stingmon | Champion | KillSpeedBonus | 0.09 | 0 | Ao derrotar um inimigo com o golpe principal, ganha +9% de Velocidade pelo resto da luta. |
| 39 | MetalGreymon | Ultimate | LowHpPhysicalDamageBonus | 0.5 | 0.15 | Ao cair abaixo de 50% de HP, ganha +15% de dano físico pelo resto da luta. |
| 40 | WereGarurumon | Ultimate | PhysicalDamageWithOpeningBonus | 0.08 | 0.16 | +8% de dano físico (+16% no primeiro golpe desferido na luta). |
| 41 | WarGrowlmon | Ultimate | LowHpPhysicalDamageBonus | 0.5 | 0.16 | Ao cair abaixo de 50% de HP, ganha +16% de dano físico pelo resto da luta. |
| 42 | Rapidmon | Ultimate | SpeedAndBonusVsActedTarget | 0.06 | 0.1 | +6% de Velocidade e +10% de dano contra alvos que já agiram na luta. |
| 43 | MagnaAngemon | Ultimate | PeriodicDefensePierce | 3 | 0.25 | A cada 3 ataques, o próximo ignora 25% da defesa do alvo. |
| 48 | MachGaogamon | Ultimate | OpeningHitBonus | 0.06 | 0 | +6% de dano no primeiro golpe desferido na luta. |
| 49 | Taomon | Ultimate | DodgeThenGuaranteedCrit | 0.07 | 0 | 7% de chance de esquivar; depois de esquivar, o próximo ataque é crítico garantido. |
| 50 | Lillymon | Ultimate | CritLifesteal | 0.07 | 0 | Ao acertar um crítico, cura 7% do dano causado. |
| 51 | MegaKabuterimon | Ultimate | NegateDamageChance | 0.22 | 0 | 22% de chance de ignorar todo o dano de um golpe recebido. |
| 61 | Garudamon | Ultimate | OpeningHitBonus | 0.1 | 0 | +10% de dano no primeiro golpe desferido na luta. |
| 67 | Zudomon | Ultimate | PhysicalDefensePercent | 0.12 | 0 | +12% de Defesa Física (PD). |
| 72 | Angewomon | Ultimate | HealLowestAllyOnAttack | 0.09 | 0 | A cada ataque, cura o aliado (outro que não ele) com menor % de HP em 9% do dano causado. |
| 80 | Grand Galemon | Ultimate | RangedExtraStrike | 0.15 | 0.3 | Em ataques à distância, 15% de chance de um golpe extra imediato com 30% do dano. |
| 86 | Lamiamon | Ultimate | DodgeThenGuaranteedCrit | 0.06 | 0 | 6% de chance de esquivar; depois de esquivar, o próximo ataque é crítico garantido. |
| 93 | Chaperomon | Ultimate | SpeedAndBonusVsActedTarget | 0.06 | 0.12 | +6% de Velocidade e +12% de dano contra alvos que já agiram na luta. |
| 98 | Delumon | Ultimate | LowHpPhysicalDamageBonus | 0.5 | 0.14 | Ao cair abaixo de 50% de HP, ganha +14% de dano físico pelo resto da luta. |
| 104 | Fantomon | Ultimate | KillSpeedBonus | 0.12 | 0 | Ao derrotar um inimigo com o golpe principal, ganha +12% de Velocidade pelo resto da luta. |
| 111 | Triceramon | Ultimate | NegateDamageChance | 0.2 | 0 | 20% de chance de ignorar todo o dano de um golpe recebido. |
| 118 | Destromon | Ultimate | RangedStacksGuaranteedCrit | 3 | 0 | Cada ataque à distância soma 1 acúmulo; ao chegar em 3, esse ataque é crítico garantido. |
| 125 | Lucemon Falldown Mode | Ultimate | CritLifesteal | 0.06 | 0 | Ao acertar um crítico, cura 6% do dano causado. |
| 127 | Astamon | Ultimate | StunChance | 0.1 | 0.2 | 10% de chance de atordoar o alvo (20% se o alvo estiver abaixo de 50% de HP). |
| 131 | Chirinmon | Ultimate | StunChance | 0.12 | 0.22 | 12% de chance de atordoar o alvo (22% se o alvo estiver abaixo de 50% de HP). |
| 134 | Groundramon | Ultimate | NegateDamageChance | 0.2 | 0 | 20% de chance de ignorar todo o dano de um golpe recebido. |
| 137 | Wingdramon | Ultimate | OpeningHitBonus | 0.11 | 0 | +11% de dano no primeiro golpe desferido na luta. |
| 141 | AeroVeedramon | Ultimate | OpeningHitBonus | 0.12 | 0 | +12% de dano no primeiro golpe desferido na luta. |
| 147 | Knightmon | Ultimate | FirstHitTakenReduction | 0.3 | 0 | O primeiro golpe recebido na luta causa 30% a menos de dano. |
| 151 | Dorugreymon | Ultimate | PhysicalAttackPercent | 0.1 | 0 | +10% de Ataque Físico (PA). |
| 154 | Grademon | Ultimate | NegateDamageChance | 0.24 | 0 | 24% de chance de ignorar todo o dano de um golpe recebido. |
| 157 | DexDorugreymon | Ultimate | CritLifesteal | 0.08 | 0 | Ao acertar um crítico, cura 8% do dano causado. |
| 165 | SaviorHackmon | Ultimate | DodgeThenGuaranteedCrit | 0.08 | 0 | 8% de chance de esquivar; depois de esquivar, o próximo ataque é crítico garantido. |
| 167 | RizeGreymon | Ultimate | CritHealMaxHp | 0.04 | 0 | Ao acertar um crítico, cura 4% do HP máximo. |
| 170 | Mistymon | Ultimate | KillSpeedBonus | 0.14 | 0 | Ao derrotar um inimigo com o golpe principal, ganha +14% de Velocidade pelo resto da luta. |
| 179 | Lilamon | Ultimate | HealLowestAllyOnAttack | 0.11 | 0 | A cada ataque, cura o aliado (outro que não ele) com menor % de HP em 11% do dano causado. |
| 180 | LadyDevimon | Ultimate | KillSpeedBonus | 0.1 | 0 | Ao derrotar um inimigo com o golpe principal, ganha +10% de Velocidade pelo resto da luta. |
| 181 | SkullSatamon | Ultimate | AntiTank | 0.08 | 0.5 | +8% de dano contra Tanks; o dano recebido de Tanks é multiplicado por 0.5 (50%). |
| 182 | Myotismon | Ultimate | HealLowestAllyOnAttack | 0.1 | 0 | A cada ataque, cura o aliado (outro que não ele) com menor % de HP em 10% do dano causado. |
| 192 | Paildramon | Ultimate | KillSpeedBonus | 0.1 | 0 | Ao derrotar um inimigo com o golpe principal, ganha +10% de Velocidade pelo resto da luta. |
| 199 | JewelBeemon | Ultimate | PeriodicDefensePierce | 3 | 0.24 | A cada 3 ataques, o próximo ignora 24% da defesa do alvo. |
| 128 | Belphemon Sleep Mode | Mega | DefensesUpSpeedDown | 0.15 | -0.1 | +15% de PD e ED, -10% de Velocidade. |

Espécies sem passiva herdada (50): WarGreymon (Mega), MetalGarurumon (Mega), Gallantmon (Mega), MegaGargomon (Mega), Seraphimon (Mega), Hououmon (Mega), Vikemon (Mega), Magnadramon (Mega), HerculesKabuterimon (Mega), Rosemon (Mega), Zephagamon (Mega), Pinochimon (Mega), Medusamon (Mega), Leviamon (Mega), Cendrillmon (Mega), Lilithmon (Mega), Medieval Dukemon (Mega), Demon (Mega), Beelzebumon (Mega), AncientSphinxmon (Mega), Boltmon (Mega), Ragnamon (Mega), Dinomon (Mega), Lucemon Satan Mode (Mega), Barbamon (Mega), Belphemon Rage Mode (Mega), Breakdramon (Mega), SlayerDramon (Mega), UlforceVeedramon (Mega), Magnamon (Mega), Kentaurusmon (Mega), Crusadermon (Mega), Alphamon (Mega), Dorugoramon (Mega), DexDorugoramon (Mega), Craniummon (Mega), Dynasmon (Mega), Duftmon (Mega), Gankoomon (Mega), Jesmon (Mega), ShineGreymon (Mega), Sakuyamon (Mega), MirageGaogamon (Mega), Sleipmon (Mega), Ophanimon (Mega), Imperialdramon (Mega), Piedmon (Mega), GrandisKuwagamon (Mega), Omegamon (MegaPlus), Examon (MegaPlus).

## 3. Passivas próprias por Role

Cada espécie tem um pool de 2–3 passivas (campo `Passives`); ao nascer ou evoluir, o Digimon sorteia **1** do pool da espécie atual, que fica fixa até a próxima evolução. As passivas de Support podem ser restritas a SupportTypes específicos (`supportTypes` vazio = qualquer Support).

### Tank

| Id | Nome | Suporte | Descrição |
|---|---|---|---|
| Couraca | Couraça | — | -18% no dano final recebido. |
| Espinhos | Espinhos | — | Devolve 15% do dano final recebido em melee. |
| Folego | Fôlego | — | Regenera 0.6% do HP máximo por segundo, com decaimento em lutas longas. |
| GritoDeGuerra | Grito de Guerra | — | Inimigos sob Taunt causam -30% de dano durante os 4s. |
| ProvocacaoInsistente | Provocação Insistente | — | Taunt a cada 6.5s em vez de 8s. |
| UltimaTrincheira | Última Trincheira | — | Abaixo de 40% de HP, -50% no dano final por 5s, uma vez por luta. |

### Warrior

| Id | Nome | Suporte | Descrição |
|---|---|---|---|
| FuriaCrescente | Fúria Crescente | — | +5% de dano final por golpe acertado, até 5 pilhas, decai após 3s sem atacar. |
| GolpePesado | Golpe Pesado | — | A cada 4º ataque, +50% de dano final em área pequena ao redor do alvo. |
| Investida | Investida | — | Cooldown do dash 6s -> 4s e o primeiro golpe após um dash causa +60% de dano final. |
| RompeGuarda | Rompe-Guarda | — | Ignora 25% da DEF do alvo, com o ganho limitado a +40% do dano base. |
| SedeDeBatalha | Sede de Batalha | — | Cura 20% do dano final causado, com decaimento em lutas longas. |
| Teimosia | Teimosia | — | Redução de dano final escala com o HP perdido, até -30% no HP mínimo. |

### Assassin

| Id | Nome | Suporte | Descrição |
|---|---|---|---|
| Execucao | Execução | — | +25% de dano final contra alvos abaixo de 40% de HP. |
| FioDaLamina | Fio da Lâmina | — | Crítico causa x3.2 em vez de x2. |
| GolpeSorrateiro | Golpe Sorrateiro | — | +80% de dano final no primeiro golpe contra cada alvo, uma vez por alvo por luta. |
| Infiltrador | Infiltrador | — | Penalidade de Role na pontuação de alvo passa de x70 para x130. |
| PassoFantasma | Passo Fantasma | — | Ganha o dash de melee (cooldown 4s, não 6s). |
| PrecisaoLetal | Precisão Letal | — | Crítico 10% -> 22%. |

### Ranged

| Id | Nome | Suporte | Descrição |
|---|---|---|---|
| AlvoMarcado | Alvo Marcado | — | A cada 4º acerto, -12% de Speed no alvo por 5s. |
| Fixacao | Fixação | — | Não reavalia alvo (troca só quando o alvo morre) e cada acerto consecutivo reduz 0.06s do cooldown, até -0.24s. |
| PesLeves | Pés Leves | — | Raio de fuga 130 -> 190 e +8% de velocidade de movimento ao fugir. |
| PosturaFirme | Postura Firme | — | Desliga a órbita e a fuga; +25% de dano final. |
| Predicao | Predição | — | O projétil mira a posição prevista do alvo, reduzindo pela metade a margem de esquiva geométrica. |
| ProjetilPerfurante | Projétil Perfurante | — | Atravessa e atinge um segundo inimigo por 50% do dano. |

### Support

| Id | Nome | Suporte | Descrição |
|---|---|---|---|
| AuraVital | Aura Vital | qualquer | Regeneração passiva num raio, com decaimento em lutas longas. |
| DuplaVoz | Dupla Voz | Buffer | Buffa os dois maiores atacantes aliados com +12% cada, em vez de um com +20%. |
| InstintoDeFuga | Instinto de Fuga | Healer, Buffer | Ganha o kiting que hoje só o Debuffer tem. |
| MaosFirmes | Mãos Firmes | Healer | Cura 15% -> 19% do HP máximo, curva de decaimento original inalterada. |
| MarcaDupla | Marca Dupla | Debuffer | Debuffa os dois maiores atacantes inimigos com -14% cada. |
| Ressonancia | Ressonância | Buffer | Empilha até 3x, mas a 3ª pilha vale +10% em vez de +20%. |
| Vigilia | Vigília | Healer | Sem ninguém ferido, aplica escudo de 8% do HP máximo no aliado sem escudo. Um escudo por aliado, não renova até ser consumido. |

### 3.1 Multi-Role / combinações

Nenhuma espécie tem mais de uma Role nem mais de um SupportType (são campos únicos em `DigimonData`). As "combinações" possíveis vêm de duas fontes:
- **Pool da espécie**: 2–3 passivas próprias sorteáveis (seção 4).
- **Linha evolutiva**: a Role muda ao evoluir quando a espécie-filha tem outra Role, e as passivas herdadas se acumulam pela linha. Ligações que trocam de Role estão marcadas na seção 5 (coluna "Role").

Ligações por par de Role (pai → filho):

| Pai | Filho | Ligações |
|---|---|---|
| Ranged | Ranged | 70 |
| Warrior | Warrior | 57 |
| Assassin | Assassin | 32 |
| Ranged | Assassin | 23 |
| Ranged | Warrior | 21 |
| Tank | Tank | 19 |
| Warrior | Ranged | 18 |
| Assassin | Ranged | 12 |
| Warrior | Assassin | 11 |
| Assassin | Tank | 9 |
| Tank | Warrior | 9 |
| Warrior | Tank | 8 |
| Assassin | Warrior | 8 |
| Support | Support | 7 |
| Ranged | Support | 6 |
| Ranged | Tank | 6 |
| Support | Ranged | 5 |
| Tank | Assassin | 4 |
| Support | Assassin | 4 |
| Assassin | Support | 2 |
| Support | Warrior | 2 |
| Warrior | Support | 2 |
| Support | Tank | 1 |
| Tank | Ranged | 1 |

Support por SupportType (10 espécies): Buffer: 5; Healer: 3; Debuffer: 2.

## 4. Pool de passivas por espécie

| Id | Nome | Estágio | Role | Suporte | Pool (sorteia 1) |
|---|---|---|---|---|---|
| 25 | Botamon | Baby | Warrior |  | Fúria Crescente / Investida / Golpe Pesado |
| 26 | Punimon | Baby | Warrior |  | Rompe-Guarda / Golpe Pesado / Teimosia |
| 27 | Yuramon | Baby | Ranged |  | Alvo Marcado / Postura Firme / Predição |
| 28 | Keemon | Baby | Ranged |  | Alvo Marcado / Postura Firme / Predição |
| 57 | Poyomon | Baby | Ranged |  | Alvo Marcado / Postura Firme / Predição |
| 63 | Pichimon | Baby | Warrior |  | Fúria Crescente / Investida / Golpe Pesado |
| 69 | Nyaromon | Baby | Ranged |  | Alvo Marcado / Postura Firme / Predição |
| 76 | Yolkmon | Baby | Ranged |  | Alvo Marcado / Projétil Perfurante / Predição |
| 83 | Jyarimon | Baby | Assassin |  | Precisão Letal / Fio da Lâmina / Passo Fantasma |
| 89 | Pafumon | Baby | Support | Buffer | Ressonância / Instinto de Fuga / Aura Vital |
| 100 | Mokumon | Baby | Assassin |  | Precisão Letal / Fio da Lâmina / Passo Fantasma |
| 107 | Sunamon | Baby | Tank |  | Couraça / Espinhos / Última Trincheira |
| 114 | Kuramon | Baby | Ranged |  | Pés Leves / Predição / Fixação |
| 172 | Puttimon | Baby | Warrior |  | Investida / Sede de Batalha / Rompe-Guarda |
| 183 | Chibomon | Baby | Warrior |  | Fúria Crescente / Investida / Golpe Pesado |
| 185 | Dodomon | Baby | Warrior |  | Fúria Crescente / Investida / Golpe Pesado |
| 187 | Zerimon | Baby | Ranged |  | Alvo Marcado / Postura Firme / Predição |
| 188 | Pabumon | Baby | Ranged |  | Alvo Marcado / Postura Firme / Predição |
| 189 | Nyokimon | Baby | Ranged |  | Alvo Marcado / Postura Firme / Predição |
| 195 | Leafmon | Baby | Ranged |  | Alvo Marcado / Postura Firme / Predição |
| 29 | Koromon | InTraining | Warrior |  | Fúria Crescente / Investida / Golpe Pesado |
| 30 | Tsunomon | InTraining | Warrior |  | Fúria Crescente / Investida / Golpe Pesado |
| 31 | Tokomon | InTraining | Ranged |  | Fixação / Postura Firme / Projétil Perfurante |
| 32 | Motimon | InTraining | Ranged |  | Fixação / Postura Firme / Projétil Perfurante |
| 33 | Gigimon | InTraining | Ranged |  | Fixação / Postura Firme / Projétil Perfurante |
| 34 | Gummymon | InTraining | Ranged |  | Fixação / Postura Firme / Projétil Perfurante |
| 35 | Viximon | InTraining | Warrior |  | Fúria Crescente / Investida / Golpe Pesado |
| 36 | Tanemon | InTraining | Ranged |  | Alvo Marcado / Postura Firme / Predição |
| 37 | Wanyamon | InTraining | Warrior |  | Fúria Crescente / Investida / Golpe Pesado |
| 38 | Yaamon | InTraining | Warrior |  | Fúria Crescente / Investida / Golpe Pesado |
| 58 | Yokomon | InTraining | Ranged |  | Fixação / Postura Firme / Projétil Perfurante |
| 64 | Bukamon | InTraining | Warrior |  | Fúria Crescente / Investida / Golpe Pesado |
| 70 | Salamon | InTraining | Ranged |  | Fixação / Postura Firme / Projétil Perfurante |
| 77 | Fluffymon | InTraining | Ranged |  | Alvo Marcado / Projétil Perfurante / Predição |
| 90 | Kyaromon | InTraining | Support | Buffer | Ressonância / Instinto de Fuga / Aura Vital |
| 101 | PetiMeramon | InTraining | Assassin |  | Precisão Letal / Fio da Lâmina / Passo Fantasma |
| 108 | Goromon | InTraining | Tank |  | Couraça / Espinhos / Última Trincheira |
| 115 | Tsumemon | InTraining | Ranged |  | Pés Leves / Predição / Fixação |
| 173 | Babydmon | InTraining | Warrior |  | Investida / Sede de Batalha / Rompe-Guarda |
| 184 | DemiVeemon | InTraining | Warrior |  | Fúria Crescente / Investida / Golpe Pesado |
| 186 | Dorimon | InTraining | Warrior |  | Fúria Crescente / Investida / Golpe Pesado |
| 190 | Budmon | InTraining | Ranged |  | Alvo Marcado / Postura Firme / Predição |
| 196 | Minomon | InTraining | Ranged |  | Alvo Marcado / Postura Firme / Predição |
| 1 | Agumon | Rookie | Warrior |  | Investida / Sede de Batalha / Rompe-Guarda |
| 2 | Gabumon | Rookie | Ranged |  | Alvo Marcado / Projétil Perfurante / Predição |
| 3 | Tentomon | Rookie | Tank |  | Grito de Guerra / Provocação Insistente / Couraça |
| 4 | Patamon | Rookie | Ranged |  | Fixação / Postura Firme / Projétil Perfurante |
| 5 | Gaomon | Rookie | Warrior |  | Fúria Crescente / Teimosia / Golpe Pesado |
| 6 | Palmon | Rookie | Ranged |  | Fixação / Postura Firme / Projétil Perfurante |
| 7 | Guilmon | Rookie | Warrior |  | Fúria Crescente / Teimosia / Golpe Pesado |
| 8 | Terriermon | Rookie | Ranged |  | Pés Leves / Projétil Perfurante / Alvo Marcado |
| 9 | Renamon | Rookie | Assassin |  | Precisão Letal / Execução / Passo Fantasma |
| 10 | Impmon | Rookie | Ranged |  | Pés Leves / Predição / Fixação |
| 11 | Agumon (Savers) | Rookie | Warrior |  | Investida / Sede de Batalha / Rompe-Guarda |
| 12 | Lalamon | Rookie | Support | Healer | Vigília / Mãos Firmes / Aura Vital |
| 59 | Biyomon | Rookie | Ranged |  | Fixação / Postura Firme / Projétil Perfurante |
| 65 | Gomamon | Rookie | Tank |  | Couraça / Fôlego / Provocação Insistente |
| 71 | Gatomon | Rookie | Assassin |  | Precisão Letal / Fio da Lâmina / Passo Fantasma |
| 78 | Pteromon | Rookie | Ranged |  | Alvo Marcado / Projétil Perfurante / Predição |
| 84 | Elizamon | Rookie | Assassin |  | Precisão Letal / Fio da Lâmina / Passo Fantasma |
| 91 | Shoemon | Rookie | Support | Debuffer | Marca Dupla / Aura Vital |
| 96 | Muchomon | Rookie | Warrior |  | Fúria Crescente / Teimosia / Golpe Pesado |
| 102 | Ghostmon | Rookie | Assassin |  | Precisão Letal / Fio da Lâmina / Passo Fantasma |
| 109 | Sunarizamon | Rookie | Tank |  | Grito de Guerra / Espinhos / Última Trincheira |
| 116 | BEMmon | Rookie | Ranged |  | Pés Leves / Predição / Fixação |
| 124 | Lucemon | Rookie | Assassin |  | Precisão Letal / Execução / Passo Fantasma |
| 126 | Phascomon | Rookie | Tank |  | Última Trincheira / Fôlego / Grito de Guerra |
| 129 | Kudamon | Rookie | Ranged |  | Pés Leves / Projétil Perfurante / Alvo Marcado |
| 132 | Dracomon | Rookie | Warrior |  | Investida / Sede de Batalha / Rompe-Guarda |
| 139 | Veemon | Rookie | Warrior |  | Investida / Sede de Batalha / Rompe-Guarda |
| 145 | Kotemon | Rookie | Warrior |  | Sede de Batalha / Fúria Crescente / Teimosia |
| 149 | Dorumon | Rookie | Warrior |  | Sede de Batalha / Fúria Crescente / Teimosia |
| 163 | Hackmon | Rookie | Assassin |  | Precisão Letal / Fio da Lâmina / Passo Fantasma |
| 169 | Candlemon | Rookie | Ranged |  | Alvo Marcado / Projétil Perfurante / Predição |
| 197 | Wormmon | Rookie | Assassin |  | Precisão Letal / Execução / Passo Fantasma |
| 13 | Greymon | Champion | Warrior |  | Rompe-Guarda / Golpe Pesado / Teimosia |
| 14 | Garurumon | Champion | Ranged |  | Pés Leves / Predição / Fixação |
| 15 | Kabuterimon | Champion | Tank |  | Grito de Guerra / Espinhos / Última Trincheira |
| 16 | Angemon | Champion | Ranged |  | Postura Firme / Predição / Fixação |
| 17 | Togemon | Champion | Warrior |  | Investida / Sede de Batalha / Rompe-Guarda |
| 18 | Growlmon | Champion | Warrior |  | Rompe-Guarda / Golpe Pesado / Teimosia |
| 19 | Gargomon | Champion | Ranged |  | Pés Leves / Projétil Perfurante / Alvo Marcado |
| 20 | Kyubimon | Champion | Ranged |  | Alvo Marcado / Projétil Perfurante / Predição |
| 21 | Wizardmon | Champion | Ranged |  | Pés Leves / Predição / Fixação |
| 23 | Gaogamon | Champion | Warrior |  | Rompe-Guarda / Golpe Pesado / Teimosia |
| 24 | Sunflowmon | Champion | Support | Debuffer | Marca Dupla / Aura Vital |
| 44 | Geogreymon | Champion | Warrior |  | Rompe-Guarda / Golpe Pesado / Teimosia |
| 45 | Devimon | Champion | Assassin |  | Precisão Letal / Execução / Passo Fantasma |
| 46 | IceDevimon | Champion | Assassin |  | Precisão Letal / Execução / Passo Fantasma |
| 47 | Devidramon | Champion | Assassin |  | Precisão Letal / Execução / Passo Fantasma |
| 60 | Birdramon | Champion | Ranged |  | Fixação / Postura Firme / Projétil Perfurante |
| 66 | Ikkakumon | Champion | Tank |  | Grito de Guerra / Espinhos / Última Trincheira |
| 79 | Galemon | Champion | Ranged |  | Postura Firme / Predição / Fixação |
| 85 | Dimetromon | Champion | Assassin |  | Fio da Lâmina / Golpe Sorrateiro / Infiltrador |
| 92 | ShoeShoemon | Champion | Support | Buffer | Dupla Voz / Instinto de Fuga / Ressonância |
| 97 | Cockatrimon | Champion | Warrior |  | Rompe-Guarda / Golpe Pesado / Teimosia |
| 103 | Bakemon | Champion | Assassin |  | Fio da Lâmina / Golpe Sorrateiro / Infiltrador |
| 110 | Tortamon | Champion | Tank |  | Grito de Guerra / Provocação Insistente / Couraça |
| 117 | Snatchmon | Champion | Ranged |  | Pés Leves / Predição / Fixação |
| 130 | Reppamon | Champion | Ranged |  | Postura Firme / Predição / Fixação |
| 133 | Coredramon (Green) | Champion | Warrior |  | Investida / Sede de Batalha / Rompe-Guarda |
| 136 | Coredramon (Blue) | Champion | Ranged |  | Alvo Marcado / Projétil Perfurante / Predição |
| 140 | Veedramon | Champion | Warrior |  | Rompe-Guarda / Golpe Pesado / Teimosia |
| 146 | Gladimon | Champion | Warrior |  | Rompe-Guarda / Golpe Pesado / Teimosia |
| 150 | Dorugamon | Champion | Warrior |  | Rompe-Guarda / Golpe Pesado / Teimosia |
| 153 | Raptordramon | Champion | Warrior |  | Investida / Sede de Batalha / Rompe-Guarda |
| 156 | DexDorugamon | Champion | Assassin |  | Precisão Letal / Execução / Passo Fantasma |
| 164 | BaoHackmon | Champion | Assassin |  | Precisão Letal / Fio da Lâmina / Passo Fantasma |
| 191 | ExVeemon | Champion | Warrior |  | Rompe-Guarda / Golpe Pesado / Teimosia |
| 198 | Stingmon | Champion | Assassin |  | Precisão Letal / Execução / Passo Fantasma |
| 39 | MetalGreymon | Ultimate | Warrior |  | Fúria Crescente / Investida / Golpe Pesado |
| 40 | WereGarurumon | Ultimate | Warrior |  | Fúria Crescente / Investida / Golpe Pesado |
| 41 | WarGrowlmon | Ultimate | Warrior |  | Investida / Golpe Pesado / Rompe-Guarda |
| 42 | Rapidmon | Ultimate | Ranged |  | Pés Leves / Projétil Perfurante / Alvo Marcado |
| 43 | MagnaAngemon | Ultimate | Ranged |  | Pés Leves / Projétil Perfurante / Alvo Marcado |
| 48 | MachGaogamon | Ultimate | Warrior |  | Rompe-Guarda / Golpe Pesado / Teimosia |
| 49 | Taomon | Ultimate | Ranged |  | Pés Leves / Projétil Perfurante / Alvo Marcado |
| 50 | Lillymon | Ultimate | Ranged |  | Alvo Marcado / Postura Firme / Predição |
| 51 | MegaKabuterimon | Ultimate | Tank |  | Grito de Guerra / Espinhos / Última Trincheira |
| 61 | Garudamon | Ultimate | Ranged |  | Fixação / Postura Firme / Projétil Perfurante |
| 67 | Zudomon | Ultimate | Tank |  | Couraça / Fôlego / Provocação Insistente |
| 72 | Angewomon | Ultimate | Ranged |  | Pés Leves / Projétil Perfurante / Alvo Marcado |
| 80 | Grand Galemon | Ultimate | Ranged |  | Pés Leves / Predição / Fixação |
| 86 | Lamiamon | Ultimate | Assassin |  | Golpe Sorrateiro / Passo Fantasma / Infiltrador |
| 93 | Chaperomon | Ultimate | Support | Buffer | Ressonância / Dupla Voz / Aura Vital |
| 98 | Delumon | Ultimate | Warrior |  | Rompe-Guarda / Golpe Pesado / Teimosia |
| 104 | Fantomon | Ultimate | Assassin |  | Golpe Sorrateiro / Passo Fantasma / Infiltrador |
| 111 | Triceramon | Ultimate | Tank |  | Couraça / Espinhos / Última Trincheira |
| 118 | Destromon | Ultimate | Ranged |  | Pés Leves / Predição / Fixação |
| 125 | Lucemon Falldown Mode | Ultimate | Assassin |  | Golpe Sorrateiro / Passo Fantasma / Infiltrador |
| 127 | Astamon | Ultimate | Ranged |  | Fixação / Postura Firme / Projétil Perfurante |
| 131 | Chirinmon | Ultimate | Ranged |  | Pés Leves / Projétil Perfurante / Alvo Marcado |
| 134 | Groundramon | Ultimate | Tank |  | Couraça / Fôlego / Provocação Insistente |
| 137 | Wingdramon | Ultimate | Ranged |  | Pés Leves / Projétil Perfurante / Alvo Marcado |
| 141 | AeroVeedramon | Ultimate | Ranged |  | Pés Leves / Projétil Perfurante / Alvo Marcado |
| 147 | Knightmon | Ultimate | Tank |  | Couraça / Fôlego / Provocação Insistente |
| 151 | Dorugreymon | Ultimate | Warrior |  | Sede de Batalha / Fúria Crescente / Teimosia |
| 154 | Grademon | Ultimate | Tank |  | Couraça / Espinhos / Última Trincheira |
| 157 | DexDorugreymon | Ultimate | Assassin |  | Execução / Precisão Letal / Golpe Sorrateiro |
| 165 | SaviorHackmon | Ultimate | Assassin |  | Infiltrador / Execução / Fio da Lâmina |
| 167 | RizeGreymon | Ultimate | Warrior |  | Rompe-Guarda / Golpe Pesado / Investida |
| 170 | Mistymon | Ultimate | Assassin |  | Passo Fantasma / Precisão Letal / Fio da Lâmina |
| 179 | Lilamon | Ultimate | Support | Healer | Mãos Firmes / Instinto de Fuga / Aura Vital |
| 180 | LadyDevimon | Ultimate | Assassin |  | Golpe Sorrateiro / Passo Fantasma / Infiltrador |
| 181 | SkullSatamon | Ultimate | Assassin |  | Execução / Precisão Letal / Golpe Sorrateiro |
| 182 | Myotismon | Ultimate | Assassin |  | Golpe Sorrateiro / Passo Fantasma / Infiltrador |
| 192 | Paildramon | Ultimate | Ranged |  | Fixação / Postura Firme / Projétil Perfurante |
| 199 | JewelBeemon | Ultimate | Assassin |  | Execução / Precisão Letal / Golpe Sorrateiro |
| 52 | WarGreymon | Mega | Warrior |  | Fúria Crescente / Investida / Golpe Pesado |
| 53 | MetalGarurumon | Mega | Warrior |  | Fúria Crescente / Investida / Golpe Pesado |
| 54 | Gallantmon | Mega | Warrior |  | Investida / Golpe Pesado / Rompe-Guarda |
| 55 | MegaGargomon | Mega | Tank |  | Couraça / Espinhos / Última Trincheira |
| 56 | Seraphimon | Mega | Ranged |  | Pés Leves / Projétil Perfurante / Alvo Marcado |
| 62 | Hououmon | Mega | Ranged |  | Pés Leves / Projétil Perfurante / Alvo Marcado |
| 68 | Vikemon | Mega | Tank |  | Couraça / Fôlego / Provocação Insistente |
| 73 | Magnadramon | Mega | Ranged |  | Pés Leves / Projétil Perfurante / Alvo Marcado |
| 74 | HerculesKabuterimon | Mega | Tank |  | Grito de Guerra / Espinhos / Última Trincheira |
| 75 | Rosemon | Mega | Ranged |  | Alvo Marcado / Postura Firme / Predição |
| 81 | Zephagamon | Mega | Ranged |  | Pés Leves / Predição / Fixação |
| 82 | Pinochimon | Mega | Ranged |  | Pés Leves / Predição / Fixação |
| 87 | Medusamon | Mega | Assassin |  | Golpe Sorrateiro / Passo Fantasma / Infiltrador |
| 88 | Leviamon | Mega | Tank |  | Couraça / Fôlego / Provocação Insistente |
| 94 | Cendrillmon | Mega | Support | Buffer | Ressonância / Dupla Voz / Aura Vital |
| 95 | Lilithmon | Mega | Support | Healer | Vigília / Mãos Firmes / Aura Vital |
| 99 | Medieval Dukemon | Mega | Warrior |  | Rompe-Guarda / Golpe Pesado / Teimosia |
| 105 | Demon | Mega | Assassin |  | Golpe Sorrateiro / Passo Fantasma / Infiltrador |
| 106 | Beelzebumon | Mega | Assassin |  | Golpe Sorrateiro / Passo Fantasma / Infiltrador |
| 112 | AncientSphinxmon | Mega | Tank |  | Couraça / Espinhos / Última Trincheira |
| 113 | Boltmon | Mega | Tank |  | Couraça / Espinhos / Última Trincheira |
| 119 | Ragnamon | Mega | Ranged |  | Pés Leves / Predição / Fixação |
| 120 | Dinomon | Mega | Warrior |  | Fúria Crescente / Investida / Golpe Pesado |
| 121 | Lucemon Satan Mode | Mega | Assassin |  | Execução / Precisão Letal / Golpe Sorrateiro |
| 122 | Barbamon | Mega | Ranged |  | Pés Leves / Predição / Fixação |
| 123 | Belphemon Rage Mode | Mega | Warrior |  | Investida / Golpe Pesado / Rompe-Guarda |
| 128 | Belphemon Sleep Mode | Mega | Tank |  | Couraça / Fôlego / Provocação Insistente |
| 135 | Breakdramon | Mega | Warrior |  | Investida / Golpe Pesado / Rompe-Guarda |
| 138 | SlayerDramon | Mega | Assassin |  | Golpe Sorrateiro / Passo Fantasma / Infiltrador |
| 142 | UlforceVeedramon | Mega | Assassin |  | Golpe Sorrateiro / Passo Fantasma / Infiltrador |
| 143 | Magnamon | Mega | Warrior |  | Investida / Golpe Pesado / Rompe-Guarda |
| 144 | Kentaurusmon | Mega | Ranged |  | Pés Leves / Projétil Perfurante / Alvo Marcado |
| 148 | Crusadermon | Mega | Warrior |  | Fúria Crescente / Teimosia / Golpe Pesado |
| 152 | Alphamon | Mega | Assassin |  | Golpe Sorrateiro / Passo Fantasma / Infiltrador |
| 155 | Dorugoramon | Mega | Tank |  | Couraça / Espinhos / Última Trincheira |
| 158 | DexDorugoramon | Mega | Assassin |  | Execução / Precisão Letal / Golpe Sorrateiro |
| 159 | Craniummon | Mega | Tank |  | Grito de Guerra / Provocação Insistente / Couraça |
| 160 | Dynasmon | Mega | Warrior |  | Sede de Batalha / Fúria Crescente / Teimosia |
| 161 | Duftmon | Mega | Assassin |  | Golpe Sorrateiro / Passo Fantasma / Infiltrador |
| 162 | Gankoomon | Mega | Warrior |  | Fúria Crescente / Investida / Golpe Pesado |
| 166 | Jesmon | Mega | Assassin |  | Golpe Sorrateiro / Passo Fantasma / Infiltrador |
| 168 | ShineGreymon | Mega | Warrior |  | Fúria Crescente / Investida / Golpe Pesado |
| 175 | Sakuyamon | Mega | Ranged |  | Alvo Marcado / Postura Firme / Predição |
| 176 | MirageGaogamon | Mega | Warrior |  | Rompe-Guarda / Golpe Pesado / Teimosia |
| 177 | Sleipmon | Mega | Warrior |  | Investida / Golpe Pesado / Rompe-Guarda |
| 178 | Ophanimon | Mega | Ranged |  | Pés Leves / Projétil Perfurante / Alvo Marcado |
| 193 | Imperialdramon | Mega | Warrior |  | Fúria Crescente / Teimosia / Golpe Pesado |
| 194 | Piedmon | Mega | Assassin |  | Golpe Sorrateiro / Passo Fantasma / Infiltrador |
| 200 | GrandisKuwagamon | Mega | Warrior |  | Investida / Golpe Pesado / Rompe-Guarda |
| 171 | Omegamon | MegaPlus | Warrior |  | Investida / Golpe Pesado / Rompe-Guarda |
| 174 | Examon | MegaPlus | Warrior |  | Investida / Golpe Pesado / Rompe-Guarda |

## 5. Árvore de evolução

Fonte: `Data/Evolutions/*.json` (é o que o jogo usa). Requisitos: nível, idade (dias), stats mínimos (só os >0), e quando houver: batalhas, taxa de vitória, disciplina e felicidade. Mult = `statMultiplier` aplicado ao evoluir. Tipo/Fonte vêm do `evolution_web.json` (normal / mode; canon / design).

| De | Estágio | → Para | Estágio | Role | Nív | Idade | Stats mín. | Outros | Mult | Tipo/Fonte |
|---|---|---|---|---|---|---|---|---|---|---|
| Botamon (#25) | Baby | Koromon (#29) | InTraining | Warrior | 2 | 2 | HP 25 PA 13 Vel 5 |  | 1.15 | normal/canon |
| Botamon (#25) | Baby | Tsunomon (#30) | InTraining | Warrior | 2 | 0 | HP 25 PA 13 PD 8 ED 8 | 8 batalhas | 1.2 | normal/design |
| Botamon (#25) | Baby | Wanyamon (#37) | InTraining | Warrior | 2 | 0 | HP 25 PA 13 PD 8 EA 6 |  | 1.15 | normal/design |
| Punimon (#26) | Baby | Tsunomon (#30) | InTraining | Warrior | 2 | 0 | HP 25 PA 3 PD 4 EA 16 Vel 5 |  | 1.2 | normal/canon |
| Punimon (#26) | Baby | Gummymon (#34) | InTraining | Warrior→Ranged | 2 | 2 | HP 24 PD 8 EA 11 |  | 1.15 | normal/design |
| Punimon (#26) | Baby | Viximon (#35) | InTraining | Warrior | 2 | 0 | HP 25 PD 8 EA 6 ED 8 Vel 5 |  | 1.15 | normal/design |
| Yuramon (#27) | Baby | Tanemon (#36) | InTraining | Ranged | 2 | 2 | HP 25 EA 10 Vel 9 |  | 1.15 | normal/canon |
| Yuramon (#27) | Baby | Yokomon (#58) | InTraining | Ranged | 2 | 0 | HP 25 PA 3 EA 10 ED 8 Vel 9 |  | 1.15 | normal/design |
| Yuramon (#27) | Baby | Budmon (#190) | InTraining | Ranged | 2 | 0 | HP 25 EA 15 ED 8 Vel 9 | 8 batalhas | 1.15 | normal/design |
| Yuramon (#27) | Baby | Minomon (#196) | InTraining | Ranged | 2 | 0 | HP 25 EA 19 Vel 9 |  | 1.15 | normal/design |
| Keemon (#28) | Baby | Yaamon (#38) | InTraining | Ranged→Warrior | 2 | 2 | HP 25 PA 3 Vel 9 |  | 1.15 | normal/design |
| Keemon (#28) | Baby | Kyaromon (#90) | InTraining | Ranged→Support | 2 | 0 | HP 25 EA 19 Vel 9 |  | 1.2 | normal/design |
| Keemon (#28) | Baby | Tsumemon (#115) | InTraining | Ranged | 2 | 0 | HP 25 EA 10 ED 8 Vel 9 | Disc ≤40 | 1.2 | normal/design |
| Poyomon (#57) | Baby | Tokomon (#31) | InTraining | Ranged | 2 | 2 | HP 25 EA 10 Vel 9 |  | 1.2 | normal/canon |
| Poyomon (#57) | Baby | Yokomon (#58) | InTraining | Ranged | 2 | 0 | HP 25 PA 3 EA 10 ED 8 Vel 9 | 8 batalhas | 1.15 | normal/design |
| Poyomon (#57) | Baby | Salamon (#70) | InTraining | Ranged | 2 | 0 | HP 25 EA 15 Vel 9 | 8 batalhas | 1.15 | normal/design |
| Pichimon (#63) | Baby | Tsunomon (#30) | InTraining | Warrior | 2 | 0 | HP 25 PA 13 PD 8 EA 6 |  | 1.2 | normal/design |
| Pichimon (#63) | Baby | Bukamon (#64) | InTraining | Warrior | 2 | 2 | HP 25 PA 13 Vel 5 |  | 1.15 | normal/canon |
| Nyaromon (#69) | Baby | Tokomon (#31) | InTraining | Ranged | 2 | 0 | HP 25 EA 15 Vel 9 |  | 1.2 | normal/design |
| Nyaromon (#69) | Baby | Salamon (#70) | InTraining | Ranged | 2 | 2 | HP 25 EA 10 Vel 9 |  | 1.15 | normal/canon |
| Yolkmon (#76) | Baby | Fluffymon (#77) | InTraining | Ranged | 2 | 0 | HP 25 EA 19 Vel 9 |  | 1.2 | normal/design |
| Yolkmon (#76) | Baby | Babydmon (#173) | InTraining | Ranged→Warrior | 2 | 2 | HP 24 PD 7 EA 10 Vel 9 |  | 1.15 | normal/design |
| Jyarimon (#83) | Baby | Koromon (#29) | InTraining | Assassin→Warrior | 2 | 0 | HP 27 PA 10 PD 10 EA 10 Vel 7 |  | 1.15 | normal/design |
| Jyarimon (#83) | Baby | Gigimon (#33) | InTraining | Assassin→Ranged | 2 | 2 | HP 27 PA 10 Vel 7 |  | 1.2 | normal/canon |
| Pafumon (#89) | Baby | Yaamon (#38) | InTraining | Support→Warrior | 2 | 0 | HP 23 PA 4 PD 10 EA 8 ED 13 |  | 1.15 | normal/design |
| Pafumon (#89) | Baby | Kyaromon (#90) | InTraining | Support | 2 | 2 | HP 23 EA 8 Vel 6 |  | 1.2 | normal/design |
| Pafumon (#89) | Baby | PetiMeramon (#101) | InTraining | Support→Assassin | 2 | 0 | HP 22 PD 10 EA 17 ED 8 |  | 1.2 | normal/design |
| Mokumon (#100) | Baby | Kyaromon (#90) | InTraining | Assassin→Support | 2 | 0 | HP 23 PD 8 EA 14 ED 6 |  | 1.2 | normal/design |
| Mokumon (#100) | Baby | PetiMeramon (#101) | InTraining | Assassin | 2 | 2 | HP 23 EA 8 Vel 6 |  | 1.2 | normal/canon |
| Sunamon (#107) | Baby | Viximon (#35) | InTraining | Tank→Warrior | 2 | 0 | HP 27 PA 10 PD 11 EA 8 ED 5 |  | 1.15 | normal/design |
| Sunamon (#107) | Baby | Goromon (#108) | InTraining | Tank | 2 | 2 | HP 27 PA 10 Vel 5 |  | 1.2 | normal/canon |
| Kuramon (#114) | Baby | Yaamon (#38) | InTraining | Ranged→Warrior | 2 | 2 | HP 21 PD 7 EA 9 Vel 8 |  | 1.15 | normal/design |
| Kuramon (#114) | Baby | Tsumemon (#115) | InTraining | Ranged | 2 | 0 | HP 22 EA 18 Vel 8 |  | 1.2 | normal/canon |
| Puttimon (#172) | Baby | Tokomon (#31) | InTraining | Warrior→Ranged | 2 | 0 | HP 27 PA 7 EA 6 Vel 10 |  | 1.2 | normal/design |
| Puttimon (#172) | Baby | Fluffymon (#77) | InTraining | Warrior→Ranged | 2 | 0 | HP 27 PA 7 ED 7 Vel 10 | 8 batalhas | 1.2 | normal/design |
| Puttimon (#172) | Baby | Babydmon (#173) | InTraining | Warrior | 2 | 2 | HP 27 PA 7 Vel 10 |  | 1.15 | normal/canon |
| Chibomon (#183) | Baby | Koromon (#29) | InTraining | Warrior | 2 | 0 | HP 25 PA 11 PD 9 EA 6 Vel 6 |  | 1.15 | normal/design |
| Chibomon (#183) | Baby | DemiVeemon (#184) | InTraining | Warrior | 2 | 2 | HP 25 PA 11 Vel 6 |  | 1.15 | normal/canon |
| Dodomon (#185) | Baby | Gigimon (#33) | InTraining | Warrior→Ranged | 2 | 0 | HP 25 PA 12 PD 8 EA 6 Vel 6 |  | 1.2 | normal/design |
| Dodomon (#185) | Baby | Dorimon (#186) | InTraining | Warrior | 2 | 2 | HP 25 PA 12 Vel 6 |  | 1.15 | normal/canon |
| Zerimon (#187) | Baby | Gummymon (#34) | InTraining | Ranged | 2 | 2 | HP 25 PA 3 Vel 9 |  | 1.15 | normal/canon |
| Zerimon (#187) | Baby | Viximon (#35) | InTraining | Ranged→Warrior | 2 | 0 | HP 25 EA 15 Vel 9 |  | 1.15 | normal/design |
| Pabumon (#188) | Baby | Motimon (#32) | InTraining | Ranged | 2 | 2 | HP 25 EA 10 Vel 9 |  | 1.15 | normal/canon |
| Pabumon (#188) | Baby | Tanemon (#36) | InTraining | Ranged | 2 | 0 | HP 25 EA 17 Vel 9 |  | 1.15 | normal/design |
| Nyokimon (#189) | Baby | Tanemon (#36) | InTraining | Ranged | 2 | 0 | HP 25 EA 10 ED 8 Vel 9 |  | 1.15 | normal/design |
| Nyokimon (#189) | Baby | Yokomon (#58) | InTraining | Ranged | 2 | 2 | HP 25 EA 10 Vel 9 |  | 1.15 | normal/canon |
| Nyokimon (#189) | Baby | Budmon (#190) | InTraining | Ranged | 2 | 0 | HP 24 EA 15 Vel 9 |  | 1.15 | normal/design |
| Leafmon (#195) | Baby | Tanemon (#36) | InTraining | Ranged | 2 | 0 | HP 25 EA 15 Vel 9 |  | 1.15 | normal/design |
| Leafmon (#195) | Baby | Minomon (#196) | InTraining | Ranged | 2 | 2 | HP 25 EA 10 Vel 9 |  | 1.15 | normal/canon |
| Koromon (#29) | InTraining | Agumon (#1) | Rookie | Warrior | 2 | 0 | HP 35 PA 15 EA 17 Vel 6 |  | 1.25 | normal/canon |
| Koromon (#29) | InTraining | Agumon (Savers) (#11) | Rookie | Warrior | 2 | 5 | HP 35 PA 15 |  | 1.25 | normal/canon |
| Koromon (#29) | InTraining | Veemon (#139) | Rookie | Warrior | 2 | 0 | HP 35 PA 15 PD 9 ED 12 Vel 6 |  | 1.25 | normal/design |
| Tsunomon (#30) | InTraining | Gabumon (#2) | Rookie | Warrior→Ranged | 2 | 0 | HP 36 PA 10 EA 25 Vel 6 |  | 1.3 | normal/canon |
| Tsunomon (#30) | InTraining | Elizamon (#84) | Rookie | Warrior→Assassin | 2 | 5 | HP 36 PA 10 PD 9 |  | 1.25 | normal/design |
| Tsunomon (#30) | InTraining | Kotemon (#145) | Rookie | Warrior | 2 | 0 | HP 36 PA 10 PD 9 ED 15 |  | 1.25 | normal/design |
| Tokomon (#31) | InTraining | Patamon (#4) | Rookie | Ranged | 2 | 5 | HP 36 PA 9 Vel 13 |  | 1.25 | normal/canon |
| Tokomon (#31) | InTraining | Kudamon (#129) | Rookie | Ranged | 3 | 0 | HP 40 PA 11 EA 24 Vel 14 |  | 1.25 | normal/design |
| Motimon (#32) | InTraining | Tentomon (#3) | Rookie | Ranged→Tank | 2 | 5 | HP 35 PD 5 ED 5 |  | 1.25 | normal/canon |
| Motimon (#32) | InTraining | Palmon (#6) | Rookie | Ranged | 3 | 0 | HP 39 PA 11 ED 12 Vel 13 |  | 1.25 | normal/design |
| Motimon (#32) | InTraining | Wormmon (#197) | Rookie | Ranged→Assassin | 2 | 0 | HP 35 PA 9 EA 18 Vel 12 |  | 1.25 | normal/canon |
| Gigimon (#33) | InTraining | Guilmon (#7) | Rookie | Ranged→Warrior | 2 | 5 | HP 36 PA 14 Vel 10 |  | 1.25 | normal/canon |
| Gigimon (#33) | InTraining | Elizamon (#84) | Rookie | Ranged→Assassin | 2 | 0 | HP 36 EA 6 ED 13 Vel 10 | Disc ≤45 | 1.25 | normal/design |
| Gigimon (#33) | InTraining | Dorumon (#149) | Rookie | Ranged→Warrior | 2 | 0 | HP 36 PA 14 EA 13 Vel 10 | 8 batalhas | 1.25 | normal/design |
| Gummymon (#34) | InTraining | Gaomon (#5) | Rookie | Ranged→Warrior | 2 | 0 | HP 35 PA 9 EA 19 Vel 9 |  | 1.25 | normal/design |
| Gummymon (#34) | InTraining | Terriermon (#8) | Rookie | Ranged | 3 | 5 | HP 39 EA 6 Vel 10 |  | 1.25 | normal/canon |
| Viximon (#35) | InTraining | Renamon (#9) | Rookie | Warrior→Assassin | 3 | 5 | EA 6 Vel 7 |  | 1.25 | normal/canon |
| Viximon (#35) | InTraining | Kudamon (#129) | Rookie | Warrior→Ranged | 3 | 0 | HP 39 PA 12 PD 9 EA 23 |  | 1.25 | normal/design |
| Tanemon (#36) | InTraining | Palmon (#6) | Rookie | Ranged | 3 | 5 | HP 39 PA 5 Vel 13 |  | 1.25 | normal/canon |
| Tanemon (#36) | InTraining | Lalamon (#12) | Rookie | Ranged→Support | 2 | 0 | HP 35 EA 28 Vel 12 |  | 1.25 | normal/design |
| Wanyamon (#37) | InTraining | Gaomon (#5) | Rookie | Warrior | 2 | 0 | HP 35 PA 17 PD 9 EA 12 | 8 batalhas | 1.25 | normal/design |
| Wanyamon (#37) | InTraining | Kudamon (#129) | Rookie | Warrior→Ranged | 3 | 0 | HP 39 PA 19 PD 11 ED 11 Vel 7 |  | 1.25 | normal/design |
| Wanyamon (#37) | InTraining | Kotemon (#145) | Rookie | Warrior | 2 | 5 | HP 35 PA 17 PD 9 |  | 1.25 | normal/design |
| Yaamon (#38) | InTraining | Impmon (#10) | Rookie | Warrior→Ranged | 3 | 0 | HP 38 EA 18 Vel 8 |  | 1.25 | normal/canon |
| Yaamon (#38) | InTraining | Shoemon (#91) | Rookie | Warrior→Support | 2 | 0 | HP 34 PA 10 PD 7 ED 20 |  | 1.25 | normal/design |
| Yaamon (#38) | InTraining | Ghostmon (#102) | Rookie | Warrior→Assassin | 2 | 5 | HP 34 PA 10 PD 7 |  | 1.25 | normal/design |
| Yokomon (#58) | InTraining | Biyomon (#59) | Rookie | Ranged | 2 | 5 | HP 35 PA 9 Vel 12 |  | 1.25 | normal/canon |
| Yokomon (#58) | InTraining | Muchomon (#96) | Rookie | Ranged→Warrior | 2 | 0 | HP 35 PA 9 EA 18 |  | 1.25 | normal/design |
| Yokomon (#58) | InTraining | Wormmon (#197) | Rookie | Ranged→Assassin | 2 | 0 | HP 35 PA 9 ED 15 Vel 12 |  | 1.25 | normal/canon |
| Bukamon (#64) | InTraining | Gabumon (#2) | Rookie | Warrior→Ranged | 2 | 0 | HP 35 PA 17 PD 9 EA 8 |  | 1.3 | normal/design |
| Bukamon (#64) | InTraining | Gomamon (#65) | Rookie | Warrior→Tank | 2 | 5 | HP 35 PA 17 Vel 6 |  | 1.25 | normal/canon |
| Salamon (#70) | InTraining | Patamon (#4) | Rookie | Ranged | 2 | 0 | HP 35 PA 9 EA 23 Vel 12 | 8 batalhas | 1.25 | normal/design |
| Salamon (#70) | InTraining | Gatomon (#71) | Rookie | Ranged→Assassin | 2 | 5 | HP 35 EA 11 Vel 12 |  | 1.25 | normal/canon |
| Salamon (#70) | InTraining | Lucemon (#124) | Rookie | Ranged→Assassin | 3 | 0 | HP 39 EA 11 ED 11 Vel 13 | Disc ≤40 | 1.25 | normal/design |
| Fluffymon (#77) | InTraining | Biyomon (#59) | Rookie | Ranged | 2 | 0 | HP 35 PD 7 EA 28 Vel 13 |  | 1.25 | normal/design |
| Fluffymon (#77) | InTraining | Pteromon (#78) | Rookie | Ranged | 2 | 5 | HP 35 EA 8 Vel 13 |  | 1.25 | normal/design |
| Fluffymon (#77) | InTraining | Muchomon (#96) | Rookie | Ranged→Warrior | 2 | 0 | HP 35 PA 6 PD 7 EA 8 ED 15 Vel 13 | 8 batalhas | 1.25 | normal/design |
| Kyaromon (#90) | InTraining | BEMmon (#116) | Rookie | Support→Ranged | 2 | 0 | HP 33 PD 9 EA 31 ED 7 |  | 1.25 | normal/design |
| Kyaromon (#90) | InTraining | Lucemon (#124) | Rookie | Support→Assassin | 3 | 5 | EA 12 Vel 9 |  | 1.25 | normal/design |
| Kyaromon (#90) | InTraining | Phascomon (#126) | Rookie | Support→Tank | 3 | 0 | HP 38 PD 11 EA 12 ED 25 |  | 1.25 | normal/design |
| PetiMeramon (#101) | InTraining | Ghostmon (#102) | Rookie | Assassin | 2 | 0 | HP 33 PD 10 EA 33 ED 9 |  | 1.25 | normal/design |
| PetiMeramon (#101) | InTraining | Candlemon (#169) | Rookie | Assassin→Ranged | 2 | 5 | HP 33 EA 12 Vel 9 |  | 1.25 | normal/canon |
| Goromon (#108) | InTraining | Sunarizamon (#109) | Rookie | Tank | 2 | 5 | HP 38 PA 14 Vel 7 |  | 1.25 | normal/canon |
| Goromon (#108) | InTraining | Kotemon (#145) | Rookie | Tank→Warrior | 2 | 0 | HP 38 PA 14 PD 15 EA 11 ED 8 |  | 1.25 | normal/design |
| Tsumemon (#115) | InTraining | BEMmon (#116) | Rookie | Ranged | 2 | 0 | HP 32 EA 28 Vel 11 |  | 1.25 | normal/design |
| Tsumemon (#115) | InTraining | Phascomon (#126) | Rookie | Ranged→Tank | 3 | 5 | HP 37 PD 9 |  | 1.25 | normal/design |
| Tsumemon (#115) | InTraining | Hackmon (#163) | Rookie | Ranged→Assassin | 3 | 0 | HP 37 EA 14 ED 18 Vel 12 |  | 1.25 | normal/design |
| Babydmon (#173) | InTraining | Dracomon (#132) | Rookie | Warrior | 2 | 5 | PA 10 Vel 11 |  | 1.25 | normal/canon |
| Babydmon (#173) | InTraining | Dorumon (#149) | Rookie | Warrior | 2 | 0 | HP 35 PA 10 PD 6 EA 18 Vel 11 |  | 1.25 | normal/design |
| DemiVeemon (#184) | InTraining | Dracomon (#132) | Rookie | Warrior | 2 | 0 | HP 35 PA 15 PD 10 EA 8 |  | 1.25 | normal/design |
| DemiVeemon (#184) | InTraining | Veemon (#139) | Rookie | Warrior | 2 | 5 | PA 15 Vel 7 |  | 1.25 | normal/canon |
| Dorimon (#186) | InTraining | Dorumon (#149) | Rookie | Warrior | 2 | 5 | PA 16 Vel 7 |  | 1.25 | normal/canon |
| Dorimon (#186) | InTraining | Hackmon (#163) | Rookie | Warrior→Assassin | 3 | 0 | HP 39 PA 18 PD 11 EA 8 |  | 1.25 | normal/design |
| Budmon (#190) | InTraining | Palmon (#6) | Rookie | Ranged | 3 | 0 | HP 39 EA 16 ED 15 Vel 12 |  | 1.25 | normal/design |
| Budmon (#190) | InTraining | Lalamon (#12) | Rookie | Ranged→Support | 2 | 5 | HP 35 Vel 11 |  | 1.25 | normal/canon |
| Budmon (#190) | InTraining | Wormmon (#197) | Rookie | Ranged→Assassin | 2 | 0 | HP 35 EA 26 Vel 11 |  | 1.25 | normal/canon |
| Minomon (#196) | InTraining | Tentomon (#3) | Rookie | Ranged→Tank | 2 | 0 | HP 35 PD 6 EA 27 Vel 11 |  | 1.25 | normal/canon |
| Minomon (#196) | InTraining | Palmon (#6) | Rookie | Ranged | 2 | 0 | HP 35 PD 6 EA 13 ED 12 Vel 11 |  | 1.25 | normal/design |
| Minomon (#196) | InTraining | Wormmon (#197) | Rookie | Ranged→Assassin | 2 | 5 | HP 35 PD 6 EA 13 Vel 11 |  | 1.25 | normal/canon |
| Agumon (#1) | Rookie | Greymon (#13) | Champion | Warrior | 12 | 2 | HP 150 PA 61 PD 55 Vel 24 |  | 1.85 | normal/canon |
| Agumon (#1) | Rookie | Devimon (#45) | Champion | Warrior→Assassin | 12 | 2 | HP 151 PA 70 PD 37 Vel 45 | Disc ≤45 | 1.85 | normal/canon |
| Agumon (#1) | Rookie | Dimetromon (#85) | Champion | Warrior→Assassin | 12 | 2 | HP 152 PA 59 PD 38 EA 58 Vel 40 |  | 1.8 | normal/design |
| Gabumon (#2) | Rookie | Garurumon (#14) | Champion | Ranged | 12 | 2 | HP 120 EA 17 Vel 60 |  | 1.85 | normal/canon |
| Gabumon (#2) | Rookie | IceDevimon (#46) | Champion | Ranged→Assassin | 12 | 2 | HP 120 PA 92 Vel 67 |  | 1.85 | normal/design |
| Tentomon (#3) | Rookie | Kabuterimon (#15) | Champion | Tank | 12 | 13 | HP 124 PD 35 ED 16 |  | 1.85 | normal/canon |
| Tentomon (#3) | Rookie | Togemon (#17) | Champion | Tank→Warrior | 12 | 2 | HP 124 PA 69 PD 35 ED 16 |  | 1.85 | normal/design |
| Patamon (#4) | Rookie | Angemon (#16) | Champion | Ranged | 12 | 2 | HP 120 EA 17 Vel 60 | Felic ≥55 | 1.85 | normal/canon |
| Patamon (#4) | Rookie | Galemon (#79) | Champion | Ranged | 12 | 2 | HP 123 PA 73 Vel 64 |  | 1.8 | normal/design |
| Gaomon (#5) | Rookie | Garurumon (#14) | Champion | Warrior→Ranged | 12 | 13 | HP 113 PA 41 PD 31 Vel 21 |  | 1.85 | normal/design |
| Gaomon (#5) | Rookie | Gaogamon (#23) | Champion | Warrior | 12 | 2 | HP 113 PA 108 Vel 21 |  | 1.85 | normal/canon |
| Gaomon (#5) | Rookie | Gladimon (#146) | Champion | Warrior | 18 | 5 | HP 141 PA 53 PD 42 Vel 82 |  | 1.9 | normal/design |
| Palmon (#6) | Rookie | Togemon (#17) | Champion | Ranged→Warrior | 12 | 2 | HP 136 PA 67 Vel 51 |  | 1.85 | normal/canon |
| Palmon (#6) | Rookie | Sunflowmon (#24) | Champion | Ranged→Support | 12 | 2 | HP 137 PA 60 PD 48 Vel 59 |  | 1.85 | normal/design |
| Guilmon (#7) | Rookie | Growlmon (#18) | Champion | Warrior | 12 | 2 | HP 113 PA 104 Vel 23 |  | 1.85 | normal/canon |
| Guilmon (#7) | Rookie | Devidramon (#47) | Champion | Warrior→Assassin | 12 | 2 | HP 70 PA 115 Vel 75 | Disc ≤30 | 1.85 | normal/design |
| Guilmon (#7) | Rookie | Dimetromon (#85) | Champion | Warrior→Assassin | 12 | 13 | HP 113 PA 45 PD 34 |  | 1.8 | normal/design |
| Terriermon (#8) | Rookie | Gargomon (#19) | Champion | Ranged | 12 | 2 | HP 120 PA 17 Vel 60 |  | 1.85 | normal/canon |
| Terriermon (#8) | Rookie | Galemon (#79) | Champion | Ranged | 12 | 2 | HP 123 EA 92 Vel 64 |  | 1.8 | normal/design |
| Renamon (#9) | Rookie | Kyubimon (#20) | Champion | Assassin→Ranged | 12 | 13 | HP 80 EA 29 Vel 31 |  | 1.85 | normal/canon |
| Renamon (#9) | Rookie | Reppamon (#130) | Champion | Assassin→Ranged | 18 | 5 | HP 82 EA 114 Vel 37 |  | 1.9 | normal/design |
| Impmon (#10) | Rookie | Wizardmon (#21) | Champion | Ranged | 12 | 2 | HP 100 EA 82 Vel 27 | Felic ≥55 | 1.85 | normal/design |
| Impmon (#10) | Rookie | Devimon (#45) | Champion | Ranged→Assassin | 12 | 2 | HP 121 EA 72 Vel 63 | Disc ≤40 | 1.85 | normal/design |
| Impmon (#10) | Rookie | Bakemon (#103) | Champion | Ranged→Assassin | 12 | 13 | HP 100 EA 22 Vel 27 |  | 1.8 | normal/design |
| Agumon (Savers) (#11) | Rookie | Greymon (#13) | Champion | Warrior | 12 | 2 | HP 112 PA 85 PD 35 |  | 1.85 | normal/design |
| Agumon (Savers) (#11) | Rookie | Geogreymon (#44) | Champion | Warrior | 12 | 13 | HP 112 PA 46 Vel 19 |  | 1.85 | normal/canon |
| Lalamon (#12) | Rookie | Togemon (#17) | Champion | Support→Warrior | 12 | 2 | HP 109 PA 65 PD 31 ED 11 Vel 28 |  | 1.85 | normal/design |
| Lalamon (#12) | Rookie | Sunflowmon (#24) | Champion | Support | 12 | 13 | HP 109 PD 31 ED 11 Vel 28 |  | 1.85 | normal/canon |
| Biyomon (#59) | Rookie | Birdramon (#60) | Champion | Ranged | 12 | 2 | HP 120 EA 17 Vel 60 |  | 1.85 | normal/canon |
| Biyomon (#59) | Rookie | Cockatrimon (#97) | Champion | Ranged→Warrior | 12 | 2 | HP 123 PA 92 Vel 62 |  | 1.8 | normal/design |
| Gomamon (#65) | Rookie | Ikkakumon (#66) | Champion | Tank | 12 | 2 | HP 190 PA 67 PD 57 |  | 1.85 | normal/canon |
| Gomamon (#65) | Rookie | Tortamon (#110) | Champion | Tank | 12 | 2 | HP 192 PA 76 PD 64 ED 43 |  | 1.8 | normal/design |
| Gatomon (#71) | Rookie | Angemon (#16) | Champion | Assassin→Ranged | 12 | 13 | HP 90 EA 31 Vel 37 |  | 1.85 | normal/design |
| Gatomon (#71) | Rookie | Wizardmon (#21) | Champion | Assassin→Ranged | 12 | 2 | HP 90 EA 100 Vel 80 | Disc ≤40 | 1.85 | normal/design |
| Pteromon (#78) | Rookie | Birdramon (#60) | Champion | Ranged | 12 | 2 | HP 98 EA 98 Vel 38 |  | 1.85 | normal/design |
| Pteromon (#78) | Rookie | Galemon (#79) | Champion | Ranged | 12 | 13 | HP 98 EA 27 Vel 38 |  | 1.8 | normal/design |
| Pteromon (#78) | Rookie | Coredramon (Blue) (#136) | Champion | Ranged | 18 | 5 | HP 110 EA 27 Vel 108 | 8 batalhas | 1.9 | normal/design |
| Elizamon (#84) | Rookie | Devidramon (#47) | Champion | Assassin | 12 | 2 | HP 93 EA 24 Vel 104 |  | 1.85 | normal/design |
| Elizamon (#84) | Rookie | Dimetromon (#85) | Champion | Assassin | 12 | 13 | HP 87 EA 24 Vel 34 |  | 1.8 | normal/design |
| Elizamon (#84) | Rookie | Tortamon (#110) | Champion | Assassin→Tank | 12 | 2 | HP 95 PD 74 EA 24 Vel 34 |  | 1.8 | normal/design |
| Shoemon (#91) | Rookie | Wizardmon (#21) | Champion | Support→Ranged | 12 | 2 | HP 99 EA 80 ED 12 Vel 25 |  | 1.85 | normal/design |
| Shoemon (#91) | Rookie | ShoeShoemon (#92) | Champion | Support | 12 | 13 | HP 99 EA 21 Vel 25 |  | 1.8 | normal/canon |
| Muchomon (#96) | Rookie | Birdramon (#60) | Champion | Warrior→Ranged | 12 | 2 | HP 107 PA 41 Vel 88 |  | 1.85 | normal/design |
| Muchomon (#96) | Rookie | Cockatrimon (#97) | Champion | Warrior | 12 | 13 | HP 107 PA 41 Vel 32 |  | 1.8 | normal/design |
| Ghostmon (#102) | Rookie | Devimon (#45) | Champion | Assassin | 12 | 2 | HP 100 EA 95 Vel 87 | Disc ≤40 | 1.85 | normal/design |
| Ghostmon (#102) | Rookie | Bakemon (#103) | Champion | Assassin | 12 | 13 | HP 93 EA 27 Vel 33 |  | 1.8 | normal/design |
| Sunarizamon (#109) | Rookie | Dimetromon (#85) | Champion | Tank→Assassin | 12 | 2 | HP 152 PA 81 PD 62 ED 40 |  | 1.8 | normal/design |
| Sunarizamon (#109) | Rookie | Tortamon (#110) | Champion | Tank | 12 | 2 | HP 143 PA 66 Vel 33 |  | 1.8 | normal/design |
| BEMmon (#116) | Rookie | Bakemon (#103) | Champion | Ranged→Assassin | 12 | 2 | HP 95 EA 92 Vel 33 |  | 1.8 | normal/design |
| BEMmon (#116) | Rookie | Snatchmon (#117) | Champion | Ranged | 12 | 13 | HP 95 EA 38 Vel 33 |  | 1.8 | normal/design |
| Lucemon (#124) | Rookie | Angemon (#16) | Champion | Assassin→Ranged | 12 | 13 | HP 97 EA 29 Vel 33 |  | 1.85 | normal/design |
| Lucemon (#124) | Rookie | Devimon (#45) | Champion | Assassin | 12 | 2 | HP 97 PA 88 EA 29 Vel 33 |  | 1.85 | normal/design |
| Phascomon (#126) | Rookie | IceDevimon (#46) | Champion | Tank→Assassin | 12 | 2 | HP 134 PA 61 PD 40 Vel 70 | Disc ≤40 | 1.85 | normal/design |
| Phascomon (#126) | Rookie | Snatchmon (#117) | Champion | Tank→Ranged | 12 | 2 | HP 136 PD 64 EA 48 ED 55 | Disc ≤40 | 1.8 | normal/design |
| Kudamon (#129) | Rookie | Angemon (#16) | Champion | Ranged | 12 | 2 | HP 150 EA 98 Vel 80 |  | 1.85 | normal/design |
| Kudamon (#129) | Rookie | Reppamon (#130) | Champion | Ranged | 18 | 5 | HP 180 EA 100 ED 60 Vel 77 |  | 1.9 | normal/canon |
| Dracomon (#132) | Rookie | Coredramon (Green) (#133) | Champion | Warrior | 12 | 13 | HP 103 PA 40 Vel 30 |  | 1.8 | normal/canon |
| Dracomon (#132) | Rookie | Coredramon (Blue) (#136) | Champion | Warrior→Ranged | 18 | 5 | HP 132 PA 52 Vel 108 |  | 1.9 | normal/canon |
| Veemon (#139) | Rookie | Veedramon (#140) | Champion | Warrior | 12 | 13 | HP 102 PA 43 Vel 30 |  | 1.8 | normal/canon |
| Veemon (#139) | Rookie | ExVeemon (#191) | Champion | Warrior | 18 | 5 | HP 131 PA 110 Vel 36 |  | 1.9 | normal/canon |
| Kotemon (#145) | Rookie | Gaogamon (#23) | Champion | Warrior | 12 | 2 | HP 170 PA 90 PD 50 Vel 50 |  | 1.85 | normal/design |
| Kotemon (#145) | Rookie | Gladimon (#146) | Champion | Warrior | 18 | 5 | HP 195 PA 90 PD 70 Vel 61 |  | 1.9 | normal/design |
| Dorumon (#149) | Rookie | Dorugamon (#150) | Champion | Warrior | 12 | 13 | HP 102 PA 38 Vel 32 |  | 1.8 | normal/canon |
| Dorumon (#149) | Rookie | Raptordramon (#153) | Champion | Warrior | 18 | 5 | HP 131 PA 120 Vel 38 |  | 1.9 | normal/canon |
| Dorumon (#149) | Rookie | DexDorugamon (#156) | Champion | Warrior→Assassin | 18 | 5 | HP 131 PA 50 Vel 106 |  | 1.9 | normal/canon |
| Hackmon (#163) | Rookie | Gladimon (#146) | Champion | Assassin→Warrior | 18 | 5 | HP 126 PA 120 PD 43 Vel 38 |  | 1.9 | normal/design |
| Hackmon (#163) | Rookie | BaoHackmon (#164) | Champion | Assassin | 12 | 13 | HP 97 PA 34 Vel 33 |  | 1.8 | normal/canon |
| Candlemon (#169) | Rookie | Wizardmon (#21) | Champion | Ranged | 12 | 13 | HP 99 EA 27 Vel 28 |  | 1.85 | normal/canon |
| Candlemon (#169) | Rookie | Bakemon (#103) | Champion | Ranged→Assassin | 12 | 2 | HP 99 EA 92 Vel 28 |  | 1.8 | normal/design |
| Wormmon (#197) | Rookie | Kabuterimon (#15) | Champion | Assassin→Tank | 12 | 13 | HP 99 PA 33 Vel 36 |  | 1.85 | normal/canon |
| Wormmon (#197) | Rookie | Togemon (#17) | Champion | Assassin→Warrior | 12 | 2 | HP 99 PA 90 Vel 36 |  | 1.85 | normal/design |
| Wormmon (#197) | Rookie | Stingmon (#198) | Champion | Assassin | 12 | 2 | HP 99 PA 33 Vel 98 |  | 1.85 | normal/canon |
| Greymon (#13) | Champion | MetalGreymon (#39) | Ultimate | Warrior | 20 | 5 | HP 248 PA 191 EA 30 Vel 36 |  | 1.6 | normal/canon |
| Greymon (#13) | Champion | RizeGreymon (#167) | Ultimate | Warrior | 20 | 24 | HP 248 PA 121 PD 74 EA 30 |  | 1.6 | normal/design |
| Garurumon (#14) | Champion | WereGarurumon (#40) | Ultimate | Ranged→Warrior | 20 | 5 | HP 247 PA 34 EA 127 Vel 59 |  | 1.6 | normal/canon |
| Garurumon (#14) | Champion | MachGaogamon (#48) | Ultimate | Ranged→Warrior | 20 | 24 | HP 247 EA 38 Vel 59 |  | 1.6 | normal/design |
| Kabuterimon (#15) | Champion | MegaKabuterimon (#51) | Ultimate | Tank | 20 | 5 | HP 273 PD 75 EA 19 ED 105 |  | 1.6 | normal/canon |
| Kabuterimon (#15) | Champion | Knightmon (#147) | Ultimate | Tank | 28 | 8 | HP 311 PD 144 ED 36 |  | 1.7 | normal/design |
| Kabuterimon (#15) | Champion | JewelBeemon (#199) | Ultimate | Tank→Assassin | 20 | 24 | HP 273 PD 75 ED 36 |  | 1.6 | normal/canon |
| Angemon (#16) | Champion | MagnaAngemon (#43) | Ultimate | Ranged | 20 | 5 | HP 212 EA 127 Vel 72 | Felic ≥55 | 1.6 | normal/canon |
| Angemon (#16) | Champion | Angewomon (#72) | Ultimate | Ranged | 20 | 24 | HP 212 EA 52 Vel 72 |  | 1.6 | normal/design |
| Angemon (#16) | Champion | Lucemon Falldown Mode (#125) | Ultimate | Ranged→Assassin | 20 | 5 | HP 298 EA 167 Vel 124 | Disc ≤35 | 1.6 | normal/design |
| Togemon (#17) | Champion | Lillymon (#50) | Ultimate | Warrior→Ranged | 20 | 24 | HP 243 PA 129 Vel 51 |  | 1.6 | normal/canon |
| Togemon (#17) | Champion | Lilamon (#179) | Ultimate | Warrior→Support | 20 | 5 | HP 243 PA 119 PD 74 ED 62 |  | 1.6 | normal/design |
| Growlmon (#18) | Champion | MetalGreymon (#39) | Ultimate | Warrior | 20 | 5 | HP 258 PA 177 PD 77 EA 60 |  | 1.6 | normal/design |
| Growlmon (#18) | Champion | WarGrowlmon (#41) | Ultimate | Warrior | 20 | 5 | HP 258 PA 163 Vel 51 |  | 1.6 | normal/canon |
| Gargomon (#19) | Champion | Rapidmon (#42) | Ultimate | Ranged | 20 | 24 | HP 248 PA 84 Vel 113 |  | 1.6 | normal/canon |
| Gargomon (#19) | Champion | MachGaogamon (#48) | Ultimate | Ranged→Warrior | 20 | 5 | HP 248 PA 143 Vel 113 |  | 1.6 | normal/design |
| Kyubimon (#20) | Champion | Taomon (#49) | Ultimate | Ranged | 20 | 24 | HP 209 EA 66 Vel 71 |  | 1.6 | normal/canon |
| Kyubimon (#20) | Champion | Mistymon (#170) | Ultimate | Ranged→Assassin | 20 | 5 | HP 209 EA 66 Vel 142 |  | 1.6 | normal/design |
| Wizardmon (#21) | Champion | Fantomon (#104) | Ultimate | Ranged→Assassin | 20 | 5 | HP 297 EA 167 Vel 130 | Disc ≤45 | 1.6 | normal/design |
| Wizardmon (#21) | Champion | Mistymon (#170) | Ultimate | Ranged→Assassin | 20 | 5 | HP 280 PA 95 EA 120 Vel 140 |  | 1.6 | normal/design |
| Gaogamon (#23) | Champion | WereGarurumon (#40) | Ultimate | Warrior | 20 | 5 | HP 258 PA 167 PD 73 EA 47 Vel 49 |  | 1.6 | normal/design |
| Gaogamon (#23) | Champion | MachGaogamon (#48) | Ultimate | Warrior | 20 | 24 | HP 258 PA 146 Vel 49 |  | 1.6 | normal/canon |
| Sunflowmon (#24) | Champion | Lillymon (#50) | Ultimate | Support→Ranged | 20 | 24 | HP 246 PD 73 ED 30 Vel 61 |  | 1.6 | normal/design |
| Sunflowmon (#24) | Champion | Lilamon (#179) | Ultimate | Support | 20 | 5 | HP 246 PD 73 ED 86 Vel 61 | Felic ≥40 | 1.6 | normal/canon |
| Sunflowmon (#24) | Champion | JewelBeemon (#199) | Ultimate | Support→Assassin | 20 | 5 | HP 246 PD 73 ED 30 Vel 111 | Felic ≥40 | 1.6 | normal/canon |
| Geogreymon (#44) | Champion | MetalGreymon (#39) | Ultimate | Warrior | 20 | 5 | HP 256 PA 149 PD 70 |  | 1.6 | normal/design |
| Geogreymon (#44) | Champion | RizeGreymon (#167) | Ultimate | Warrior | 20 | 24 | HP 256 PA 102 Vel 42 |  | 1.6 | normal/canon |
| Devimon (#45) | Champion | Lucemon Falldown Mode (#125) | Ultimate | Assassin | 20 | 5 | HP 213 PA 163 Vel 78 |  | 1.6 | normal/design |
| Devimon (#45) | Champion | LadyDevimon (#180) | Ultimate | Assassin | 20 | 24 | HP 213 PA 95 Vel 78 |  | 1.6 | normal/design |
| Devimon (#45) | Champion | SkullSatamon (#181) | Ultimate | Assassin | 20 | 5 | HP 235 PA 215 Vel 140 | Disc ≤40 | 1.6 | normal/canon |
| Devimon (#45) | Champion | Myotismon (#182) | Ultimate | Assassin | 20 | 5 | HP 230 EA 100 Vel 150 | Disc ≤50 | 1.6 | normal/canon |
| IceDevimon (#46) | Champion | Destromon (#118) | Ultimate | Assassin→Ranged | 20 | 5 | HP 173 PA 132 Vel 154 |  | 1.6 | normal/design |
| IceDevimon (#46) | Champion | Astamon (#127) | Ultimate | Assassin→Ranged | 20 | 24 | HP 173 PA 132 Vel 126 |  | 1.6 | normal/design |
| Devidramon (#47) | Champion | WarGrowlmon (#41) | Ultimate | Assassin→Warrior | 20 | 5 | HP 199 PA 201 Vel 138 |  | 1.6 | normal/design |
| Devidramon (#47) | Champion | Lamiamon (#86) | Ultimate | Assassin | 20 | 24 | HP 196 PA 106 Vel 138 |  | 1.6 | normal/design |
| Birdramon (#60) | Champion | Garudamon (#61) | Ultimate | Ranged | 20 | 5 | HP 220 EA 127 Vel 77 |  | 1.6 | normal/canon |
| Birdramon (#60) | Champion | Delumon (#98) | Ultimate | Ranged→Warrior | 20 | 24 | HP 220 EA 45 Vel 77 |  | 1.6 | normal/design |
| Ikkakumon (#66) | Champion | Zudomon (#67) | Ultimate | Tank | 20 | 5 | HP 448 PA 136 PD 120 |  | 1.6 | normal/canon |
| Ikkakumon (#66) | Champion | Triceramon (#111) | Ultimate | Tank | 20 | 5 | HP 445 PA 156 PD 137 ED 75 |  | 1.6 | normal/design |
| Galemon (#79) | Champion | Grand Galemon (#80) | Ultimate | Ranged | 20 | 24 | HP 218 EA 49 Vel 81 |  | 1.6 | normal/canon |
| Galemon (#79) | Champion | Wingdramon (#137) | Ultimate | Ranged | 28 | 8 | HP 256 EA 49 Vel 168 |  | 1.7 | normal/design |
| Dimetromon (#85) | Champion | Lamiamon (#86) | Ultimate | Assassin | 20 | 24 | HP 204 EA 48 Vel 62 |  | 1.6 | normal/design |
| Dimetromon (#85) | Champion | Triceramon (#111) | Ultimate | Assassin→Tank | 20 | 5 | HP 204 PD 94 EA 48 Vel 62 |  | 1.6 | normal/design |
| ShoeShoemon (#92) | Champion | Chaperomon (#93) | Ultimate | Support | 20 | 24 | HP 223 EA 55 Vel 53 |  | 1.6 | normal/design |
| ShoeShoemon (#92) | Champion | LadyDevimon (#180) | Ultimate | Support→Assassin | 20 | 5 | HP 223 PD 71 EA 121 ED 37 |  | 1.6 | normal/design |
| Cockatrimon (#97) | Champion | Garudamon (#61) | Ultimate | Warrior→Ranged | 20 | 5 | HP 232 PA 101 Vel 125 |  | 1.6 | normal/design |
| Cockatrimon (#97) | Champion | Delumon (#98) | Ultimate | Warrior | 20 | 24 | HP 232 PA 101 Vel 68 |  | 1.6 | normal/design |
| Bakemon (#103) | Champion | Fantomon (#104) | Ultimate | Assassin | 20 | 24 | HP 209 EA 66 Vel 67 |  | 1.6 | normal/canon |
| Bakemon (#103) | Champion | Myotismon (#182) | Ultimate | Assassin | 20 | 5 | HP 209 EA 148 Vel 67 |  | 1.6 | normal/design |
| Tortamon (#110) | Champion | Triceramon (#111) | Ultimate | Tank | 20 | 24 | HP 233 PA 85 Vel 42 |  | 1.6 | normal/design |
| Tortamon (#110) | Champion | Groundramon (#134) | Ultimate | Tank | 28 | 8 | HP 271 PA 100 PD 168 ED 29 |  | 1.7 | normal/design |
| Snatchmon (#117) | Champion | Destromon (#118) | Ultimate | Ranged | 20 | 24 | HP 202 EA 80 Vel 61 |  | 1.6 | normal/design |
| Snatchmon (#117) | Champion | Astamon (#127) | Ultimate | Ranged | 20 | 5 | HP 202 EA 80 Vel 132 |  | 1.6 | normal/design |
| Reppamon (#130) | Champion | Taomon (#49) | Ultimate | Ranged | 20 | 24 | HP 190 EA 158 ED 31 Vel 76 |  | 1.6 | normal/design |
| Reppamon (#130) | Champion | Chirinmon (#131) | Ultimate | Ranged | 28 | 8 | HP 228 EA 243 ED 31 Vel 83 |  | 1.7 | normal/canon |
| Coredramon (Green) (#133) | Champion | Groundramon (#134) | Ultimate | Warrior→Tank | 20 | 24 | HP 225 PA 91 Vel 45 |  | 1.6 | normal/canon |
| Coredramon (Green) (#133) | Champion | Grademon (#154) | Ultimate | Warrior→Tank | 28 | 8 | HP 263 PA 183 PD 93 Vel 70 |  | 1.7 | normal/design |
| Coredramon (Blue) (#136) | Champion | Wingdramon (#137) | Ultimate | Ranged | 20 | 24 | HP 208 PA 75 Vel 126 |  | 1.6 | normal/canon |
| Coredramon (Blue) (#136) | Champion | AeroVeedramon (#141) | Ultimate | Ranged | 28 | 8 | HP 246 PA 147 PD 71 Vel 171 |  | 1.7 | normal/design |
| Veedramon (#140) | Champion | Wingdramon (#137) | Ultimate | Warrior→Ranged | 28 | 8 | HP 266 PA 113 PD 92 Vel 160 |  | 1.7 | normal/design |
| Veedramon (#140) | Champion | AeroVeedramon (#141) | Ultimate | Warrior→Ranged | 20 | 24 | HP 228 PA 97 Vel 66 |  | 1.6 | normal/canon |
| Gladimon (#146) | Champion | Knightmon (#147) | Ultimate | Warrior→Tank | 20 | 24 | HP 248 PA 104 Vel 75 |  | 1.6 | normal/design |
| Gladimon (#146) | Champion | Grademon (#154) | Ultimate | Warrior→Tank | 28 | 8 | HP 286 PA 210 PD 99 Vel 82 |  | 1.7 | normal/design |
| Dorugamon (#150) | Champion | Dorugreymon (#151) | Ultimate | Warrior | 20 | 24 | HP 222 PA 90 Vel 72 |  | 1.6 | normal/canon |
| Dorugamon (#150) | Champion | DexDorugreymon (#157) | Ultimate | Warrior→Assassin | 28 | 8 | HP 260 PA 176 PD 87 Vel 79 |  | 1.7 | normal/design |
| Raptordramon (#153) | Champion | Dorugreymon (#151) | Ultimate | Warrior | 28 | 8 | HP 296 PA 274 PD 80 Vel 81 |  | 1.7 | normal/design |
| Raptordramon (#153) | Champion | Grademon (#154) | Ultimate | Warrior→Tank | 20 | 24 | HP 258 PA 158 Vel 68 |  | 1.6 | normal/canon |
| DexDorugamon (#156) | Champion | Dorugreymon (#151) | Ultimate | Assassin→Warrior | 28 | 8 | HP 296 PA 190 PD 71 Vel 154 |  | 1.7 | normal/design |
| DexDorugamon (#156) | Champion | DexDorugreymon (#157) | Ultimate | Assassin | 20 | 24 | HP 258 PA 98 Vel 108 |  | 1.6 | normal/canon |
| BaoHackmon (#164) | Champion | Knightmon (#147) | Ultimate | Assassin→Tank | 28 | 8 | HP 255 PA 187 PD 87 Vel 82 |  | 1.7 | normal/design |
| BaoHackmon (#164) | Champion | SaviorHackmon (#165) | Ultimate | Assassin | 20 | 24 | HP 217 PA 83 Vel 75 |  | 1.6 | normal/canon |
| ExVeemon (#191) | Champion | AeroVeedramon (#141) | Ultimate | Warrior→Ranged | 20 | 5 | HP 327 PA 153 PD 88 Vel 152 |  | 1.6 | normal/design |
| Stingmon (#198) | Champion | MegaKabuterimon (#51) | Ultimate | Assassin→Tank | 20 | 24 | HP 222 PA 96 Vel 176 |  | 1.6 | normal/design |
| Stingmon (#198) | Champion | AeroVeedramon (#141) | Ultimate | Assassin→Ranged | 20 | 5 | HP 222 PA 96 Vel 207 | Felic ≥40 | 1.6 | normal/design |
| Stingmon (#198) | Champion | JewelBeemon (#199) | Ultimate | Assassin | 20 | 5 | HP 222 PA 172 Vel 179 |  | 1.6 | normal/canon |
| MetalGreymon (#39) | Ultimate | WarGreymon (#52) | Mega | Warrior | 28 | 8 | HP 436 PA 278 EA 56 Vel 57 | 12 batalhas, vitórias ≥55%, Felic ≥55 | 1.7 | normal/canon |
| MetalGreymon (#39) | Ultimate | ShineGreymon (#168) | Mega | Warrior | 28 | 50 | HP 436 PA 221 PD 121 EA 56 |  | 1.7 | normal/design |
| WereGarurumon (#40) | Ultimate | MetalGarurumon (#53) | Mega | Warrior | 28 | 8 | HP 434 PA 236 EA 89 Vel 85 | 12 batalhas, vitórias ≥55%, Felic ≥55 | 1.7 | normal/canon |
| WereGarurumon (#40) | Ultimate | Gankoomon (#162) | Mega | Warrior | 28 | 50 | HP 434 PA 132 PD 90 EA 89 |  | 1.7 | normal/design |
| WarGrowlmon (#41) | Ultimate | Gallantmon (#54) | Mega | Warrior | 28 | 50 | HP 418 PA 269 Vel 89 |  | 1.7 | normal/canon |
| WarGrowlmon (#41) | Ultimate | Medieval Dukemon (#99) | Mega | Warrior | 28 | 8 | HP 418 PA 299 PD 180 |  | 1.7 | normal/design |
| Rapidmon (#42) | Ultimate | MegaGargomon (#55) | Mega | Ranged→Tank | 28 | 8 | HP 544 PA 254 Vel 211 | 12 batalhas, vitórias ≥55% | 1.7 | normal/canon |
| Rapidmon (#42) | Ultimate | Magnamon (#143) | Mega | Ranged→Warrior | 36 | 12 | HP 600 PA 283 PD 150 Vel 200 |  | 1.8 | normal/design |
| MagnaAngemon (#43) | Ultimate | Seraphimon (#56) | Mega | Ranged | 28 | 8 | HP 378 EA 251 Vel 126 | 12 batalhas, Felic ≥55 | 1.7 | normal/canon |
| MagnaAngemon (#43) | Ultimate | Magnadramon (#73) | Mega | Ranged | 28 | 50 | HP 378 EA 192 Vel 126 |  | 1.7 | normal/design |
| MachGaogamon (#48) | Ultimate | Gankoomon (#162) | Mega | Warrior | 28 | 50 | HP 443 PA 164 Vel 86 |  | 1.7 | normal/design |
| MachGaogamon (#48) | Ultimate | MirageGaogamon (#176) | Mega | Warrior | 28 | 8 | HP 443 PA 226 Vel 86 | Felic ≥40 | 1.7 | normal/canon |
| Taomon (#49) | Ultimate | Duftmon (#161) | Mega | Ranged→Assassin | 28 | 50 | HP 362 EA 125 Vel 127 |  | 1.7 | normal/design |
| Taomon (#49) | Ultimate | Sakuyamon (#175) | Mega | Ranged | 28 | 8 | HP 362 EA 227 Vel 127 | 12 batalhas, Felic ≥55 | 1.7 | normal/canon |
| Lillymon (#50) | Ultimate | Rosemon (#75) | Mega | Ranged | 28 | 50 | HP 426 EA 84 Vel 97 |  | 1.7 | normal/canon |
| Lillymon (#50) | Ultimate | Cendrillmon (#94) | Mega | Ranged→Support | 28 | 8 | HP 426 EA 207 Vel 97 |  | 1.7 | normal/design |
| MegaKabuterimon (#51) | Ultimate | HerculesKabuterimon (#74) | Mega | Tank | 28 | 50 | HP 473 PD 129 ED 71 |  | 1.7 | normal/canon |
| MegaKabuterimon (#51) | Ultimate | Craniummon (#159) | Mega | Tank | 28 | 8 | HP 473 PD 186 ED 71 |  | 1.7 | normal/design |
| Garudamon (#61) | Ultimate | Hououmon (#62) | Mega | Ranged | 28 | 50 | HP 390 EA 76 Vel 132 |  | 1.7 | normal/canon |
| Garudamon (#61) | Ultimate | Zephagamon (#81) | Mega | Ranged | 28 | 8 | HP 390 EA 251 Vel 132 |  | 1.7 | normal/design |
| Zudomon (#67) | Ultimate | Vikemon (#68) | Mega | Tank | 28 | 8 | HP 800 PA 240 PD 216 |  | 1.7 | normal/canon |
| Zudomon (#67) | Ultimate | Leviamon (#88) | Mega | Tank | 28 | 8 | HP 768 PA 267 PD 228 ED 129 |  | 1.7 | normal/design |
| Angewomon (#72) | Ultimate | Magnadramon (#73) | Mega | Ranged | 28 | 50 | HP 378 EA 112 Vel 129 |  | 1.7 | normal/canon |
| Angewomon (#72) | Ultimate | Lilithmon (#95) | Mega | Ranged→Support | 28 | 8 | HP 378 EA 112 ED 108 Vel 129 |  | 1.7 | normal/design |
| Angewomon (#72) | Ultimate | Ophanimon (#178) | Mega | Ranged | 28 | 8 | HP 344 EA 213 Vel 129 | Felic ≥40 | 1.7 | normal/canon |
| Grand Galemon (#80) | Ultimate | Zephagamon (#81) | Mega | Ranged | 28 | 8 | HP 380 PA 53 EA 230 Vel 137 |  | 1.7 | normal/design |
| Grand Galemon (#80) | Ultimate | Dynasmon (#160) | Mega | Ranged→Warrior | 28 | 50 | HP 380 EA 113 Vel 137 |  | 1.7 | normal/design |
| Lamiamon (#86) | Ultimate | Medusamon (#87) | Mega | Assassin | 28 | 50 | HP 370 EA 73 Vel 112 |  | 1.7 | normal/canon |
| Lamiamon (#86) | Ultimate | Leviamon (#88) | Mega | Assassin→Tank | 28 | 8 | HP 510 PA 120 Vel 140 | Disc ≤30 | 1.7 | normal/design |
| Chaperomon (#93) | Ultimate | Pinochimon (#82) | Mega | Support→Ranged | 28 | 8 | HP 586 PD 118 EA 264 ED 134 |  | 1.7 | normal/design |
| Chaperomon (#93) | Ultimate | Cendrillmon (#94) | Mega | Support | 28 | 8 | HP 570 EA 222 Vel 140 | Felic ≥40 | 1.7 | normal/design |
| Delumon (#98) | Ultimate | Hououmon (#62) | Mega | Warrior→Ranged | 28 | 50 | HP 400 PA 144 Vel 116 |  | 1.7 | normal/design |
| Delumon (#98) | Ultimate | AncientSphinxmon (#112) | Mega | Warrior→Tank | 28 | 8 | HP 400 PA 239 Vel 116 |  | 1.7 | normal/design |
| Fantomon (#104) | Ultimate | Boltmon (#113) | Mega | Assassin→Tank | 28 | 50 | HP 372 EA 126 Vel 116 |  | 1.7 | normal/design |
| Fantomon (#104) | Ultimate | Ragnamon (#119) | Mega | Assassin→Ranged | 28 | 8 | HP 372 EA 218 Vel 116 |  | 1.7 | normal/design |
| Fantomon (#104) | Ultimate | Piedmon (#194) | Mega | Assassin | 28 | 8 | HP 372 EA 126 Vel 189 | Disc ≥60 | 1.7 | normal/design |
| Triceramon (#111) | Ultimate | AncientSphinxmon (#112) | Mega | Tank | 28 | 8 | HP 660 PA 225 Vel 87 |  | 1.7 | normal/design |
| Triceramon (#111) | Ultimate | Dinomon (#120) | Mega | Tank→Warrior | 28 | 8 | HP 678 PA 267 PD 226 ED 146 |  | 1.7 | normal/design |
| Triceramon (#111) | Ultimate | GrandisKuwagamon (#200) | Mega | Tank→Warrior | 28 | 8 | HP 678 PA 267 PD 229 ED 100 Vel 120 | Disc ≤40 | 1.7 | normal/canon |
| Destromon (#118) | Ultimate | Demon (#105) | Mega | Ranged→Assassin | 28 | 8 | HP 548 EA 285 Vel 185 | Disc ≤35 | 1.7 | normal/design |
| Destromon (#118) | Ultimate | Boltmon (#113) | Mega | Ranged→Tank | 28 | 50 | HP 373 EA 74 Vel 110 |  | 1.7 | normal/design |
| Destromon (#118) | Ultimate | Belphemon Sleep Mode (#128) | Mega | Ranged→Tank | 36 | 13 | HP 411 PD 89 ED 136 Vel 90 |  | 1.8 | normal/design |
| Lucemon Falldown Mode (#125) | Ultimate | Demon (#105) | Mega | Assassin | 28 | 50 | HP 387 PA 130 EA 89 Vel 133 |  | 1.7 | normal/design |
| Lucemon Falldown Mode (#125) | Ultimate | Lucemon Satan Mode (#121) | Mega | Assassin | 36 | 12 | HP 425 PD 96 EA 243 Vel 140 |  | 1.8 | normal/canon |
| Astamon (#127) | Ultimate | Beelzebumon (#106) | Mega | Ranged→Assassin | 28 | 8 | HP 555 PA 212 Vel 175 | Disc ≤40 | 1.7 | normal/design |
| Astamon (#127) | Ultimate | Ragnamon (#119) | Mega | Ranged | 28 | 50 | HP 375 PA 129 Vel 176 |  | 1.7 | normal/design |
| Chirinmon (#131) | Ultimate | Kentaurusmon (#144) | Mega | Ranged | 28 | 50 | HP 368 EA 198 Vel 117 |  | 1.7 | normal/design |
| Chirinmon (#131) | Ultimate | Sleipmon (#177) | Mega | Ranged→Warrior | 36 | 12 | HP 406 PA 265 PD 95 Vel 149 | Felic ≥40 | 1.8 | normal/canon |
| Groundramon (#134) | Ultimate | Breakdramon (#135) | Mega | Tank→Warrior | 28 | 50 | HP 405 PA 160 Vel 76 |  | 1.7 | normal/canon |
| Groundramon (#134) | Ultimate | Dorugoramon (#155) | Mega | Tank | 36 | 12 | HP 443 PA 175 PD 210 ED 56 |  | 1.8 | normal/design |
| Wingdramon (#137) | Ultimate | SlayerDramon (#138) | Mega | Ranged→Assassin | 36 | 12 | HP 474 PA 235 PD 105 Vel 180 |  | 1.8 | normal/canon |
| Wingdramon (#137) | Ultimate | Dynasmon (#160) | Mega | Ranged→Warrior | 28 | 50 | HP 436 PA 134 PD 117 Vel 160 |  | 1.7 | normal/design |
| AeroVeedramon (#141) | Ultimate | UlforceVeedramon (#142) | Mega | Ranged→Assassin | 36 | 12 | HP 431 PA 268 PD 106 Vel 130 |  | 1.8 | normal/canon |
| AeroVeedramon (#141) | Ultimate | Magnamon (#143) | Mega | Ranged→Warrior | 28 | 50 | HP 393 PA 170 PD 91 Vel 122 |  | 1.7 | normal/design |
| Knightmon (#147) | Ultimate | Crusadermon (#148) | Mega | Tank→Warrior | 36 | 12 | HP 472 PA 152 PD 150 Vel 147 |  | 1.8 | normal/design |
| Knightmon (#147) | Ultimate | Craniummon (#159) | Mega | Tank | 28 | 50 | HP 434 PA 137 PD 150 Vel 89 |  | 1.7 | normal/design |
| Dorugreymon (#151) | Ultimate | Alphamon (#152) | Mega | Warrior→Assassin | 36 | 12 | HP 437 PA 265 PD 120 Vel 130 |  | 1.8 | normal/canon |
| Dorugreymon (#151) | Ultimate | Dorugoramon (#155) | Mega | Warrior→Tank | 28 | 50 | HP 399 PA 164 PD 105 Vel 106 |  | 1.7 | normal/canon |
| Grademon (#154) | Ultimate | Alphamon (#152) | Mega | Tank→Assassin | 36 | 12 | HP 484 PA 262 PD 160 Vel 192 |  | 1.8 | normal/design |
| Grademon (#154) | Ultimate | Dynasmon (#160) | Mega | Tank→Warrior | 28 | 50 | HP 446 PA 262 PD 152 |  | 1.7 | normal/design |
| DexDorugreymon (#157) | Ultimate | Dorugoramon (#155) | Mega | Assassin→Tank | 36 | 12 | HP 479 PA 284 PD 111 Vel 128 |  | 1.8 | normal/design |
| DexDorugreymon (#157) | Ultimate | DexDorugoramon (#158) | Mega | Assassin | 28 | 50 | HP 441 PA 172 Vel 134 |  | 1.7 | normal/canon |
| SaviorHackmon (#165) | Ultimate | Gankoomon (#162) | Mega | Assassin→Warrior | 28 | 50 | HP 388 PA 150 PD 130 Vel 128 |  | 1.7 | normal/design |
| SaviorHackmon (#165) | Ultimate | Jesmon (#166) | Mega | Assassin | 36 | 12 | HP 426 PA 165 PD 130 Vel 185 |  | 1.8 | normal/canon |
| RizeGreymon (#167) | Ultimate | WarGreymon (#52) | Mega | Warrior | 28 | 50 | HP 451 PA 178 PD 111 |  | 1.7 | normal/design |
| RizeGreymon (#167) | Ultimate | ShineGreymon (#168) | Mega | Warrior | 28 | 8 | HP 451 PA 256 Vel 74 |  | 1.7 | normal/canon |
| Mistymon (#170) | Ultimate | Barbamon (#122) | Mega | Assassin→Ranged | 28 | 50 | HP 362 PA 140 Vel 182 |  | 1.7 | normal/design |
| Mistymon (#170) | Ultimate | Piedmon (#194) | Mega | Assassin | 28 | 8 | HP 577 PA 190 Vel 196 | Disc ≤40 | 1.7 | normal/design |
| Lilamon (#179) | Ultimate | Rosemon (#75) | Mega | Support→Ranged | 28 | 8 | HP 426 EA 198 Vel 94 |  | 1.7 | normal/canon |
| Lilamon (#179) | Ultimate | Cendrillmon (#94) | Mega | Support | 28 | 50 | HP 426 EA 80 ED 94 Vel 94 |  | 1.7 | normal/design |
| LadyDevimon (#180) | Ultimate | Medusamon (#87) | Mega | Assassin | 28 | 50 | HP 380 EA 85 Vel 106 |  | 1.7 | normal/design |
| LadyDevimon (#180) | Ultimate | Lilithmon (#95) | Mega | Assassin→Support | 28 | 8 | HP 530 EA 250 Vel 190 | Disc ≤30, Felic ≤40 | 1.7 | normal/canon |
| SkullSatamon (#181) | Ultimate | Demon (#105) | Mega | Assassin | 28 | 8 | HP 601 PA 200 Vel 135 | Felic ≤40 | 1.7 | normal/design |
| SkullSatamon (#181) | Ultimate | Piedmon (#194) | Mega | Assassin | 28 | 8 | HP 602 PA 216 Vel 136 | Disc ≤40 | 1.7 | normal/design |
| Myotismon (#182) | Ultimate | Beelzebumon (#106) | Mega | Assassin | 28 | 8 | HP 584 EA 239 Vel 141 | Disc ≤40 | 1.7 | normal/design |
| Myotismon (#182) | Ultimate | Piedmon (#194) | Mega | Assassin | 28 | 50 | HP 372 EA 152 Vel 118 |  | 1.7 | normal/design |
| Paildramon (#192) | Ultimate | UlforceVeedramon (#142) | Mega | Ranged→Assassin | 36 | 12 | HP 522 PA 293 PD 106 Vel 164 |  | 1.8 | normal/design |
| Paildramon (#192) | Ultimate | Imperialdramon (#193) | Mega | Ranged→Warrior | 28 | 50 | HP 484 PA 198 Vel 152 |  | 1.7 | normal/canon |
| JewelBeemon (#199) | Ultimate | HerculesKabuterimon (#74) | Mega | Assassin→Tank | 28 | 50 | HP 393 PA 132 Vel 104 |  | 1.7 | normal/canon |
| JewelBeemon (#199) | Ultimate | Imperialdramon (#193) | Mega | Assassin→Warrior | 28 | 8 | HP 393 PA 132 ED 99 Vel 104 | Felic ≥40 | 1.7 | normal/canon |
| JewelBeemon (#199) | Ultimate | GrandisKuwagamon (#200) | Mega | Assassin→Warrior | 28 | 8 | HP 393 PA 224 Vel 104 |  | 1.7 | normal/canon |
| Belphemon Sleep Mode (#128) | Mega | Belphemon Rage Mode (#123) | Mega | Tank→Warrior | 45 | 14 | HP 752 PA 135 PD 180 ED 141 | 15 batalhas, Disc ≤40, Felic ≤40 | 1.1 | mode/canon |

Pontos de partida (ninguém evolui para eles): Botamon (Baby), Punimon (Baby), Yuramon (Baby), Keemon (Baby), Poyomon (Baby), Pichimon (Baby), Nyaromon (Baby), Yolkmon (Baby), Jyarimon (Baby), Pafumon (Baby), Mokumon (Baby), Sunamon (Baby), Kuramon (Baby), Puttimon (Baby), Chibomon (Baby), Dodomon (Baby), Zerimon (Baby), Pabumon (Baby), Nyokimon (Baby), Leafmon (Baby).

Formas finais (não evoluem por `Data/Evolutions`): WarGreymon (Mega), MetalGarurumon (Mega), Gallantmon (Mega), MegaGargomon (Mega), Seraphimon (Mega), Hououmon (Mega), Vikemon (Mega), Magnadramon (Mega), HerculesKabuterimon (Mega), Rosemon (Mega), Zephagamon (Mega), Pinochimon (Mega), Medusamon (Mega), Leviamon (Mega), Cendrillmon (Mega), Lilithmon (Mega), Medieval Dukemon (Mega), Demon (Mega), Beelzebumon (Mega), AncientSphinxmon (Mega), Boltmon (Mega), Ragnamon (Mega), Dinomon (Mega), Lucemon Satan Mode (Mega), Barbamon (Mega), Belphemon Rage Mode (Mega), Breakdramon (Mega), SlayerDramon (Mega), UlforceVeedramon (Mega), Magnamon (Mega), Kentaurusmon (Mega), Crusadermon (Mega), Alphamon (Mega), Dorugoramon (Mega), DexDorugoramon (Mega), Craniummon (Mega), Dynasmon (Mega), Duftmon (Mega), Gankoomon (Mega), Jesmon (Mega), ShineGreymon (Mega), Sakuyamon (Mega), MirageGaogamon (Mega), Sleipmon (Mega), Ophanimon (Mega), Imperialdramon (Mega), Piedmon (Mega), GrandisKuwagamon (Mega), Omegamon (MegaPlus), Examon (MegaPlus).

Ligações presentes só no `evolution_web.json` (não existem em `Data/Evolutions`; DNA = Jogress, ver seção 6):

- breakdramon → examon (dna/canon)
- exveemon → paildramon (dna/canon)
- metalgarurumon → omegamon (dna/canon)
- slayerdramon → examon (dna/canon)
- stingmon → paildramon (dna/canon)
- wargreymon → omegamon (dna/canon)

## 6. Receitas de Jogress

| Pai A | Pai B | Resultado | Estágio | Mult |
|---|---|---|---|---|
| WarGreymon (#52) [Mega] | MetalGarurumon (#53) [Mega] | Omegamon (#171) | MegaPlus | 1.3 |
| SlayerDramon (#138) [Mega] | Breakdramon (#135) [Mega] | Examon (#174) | MegaPlus | 1.3 |
| ExVeemon (#191) [Champion] | Stingmon (#198) [Champion] | Paildramon (#192) | Ultimate | 1.3 |
