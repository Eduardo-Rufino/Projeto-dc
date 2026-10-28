# Passivas herdadas — Fase 4a: levantamento de Champion e Ultimate restantes

Data: 2026-10-20 · Branch: `passivas-herdadas` (sobre `aa92d5a`, Fase 3) · Só leitura: nenhum código nem dado foi alterado.

## Contagem

| Estágio | Total no jogo | Já têm (piloto) | Faltam |
|---|---|---|---|
| Champion | 35 | 5 | **30** |
| Ultimate | 38 | 12 | **26** |
| **Total** | 73 | 17 | **56** |

**A premissa não bateu, e o número real é 56, não 44.** As 17 do piloto estão certas.

Os estágios anteriores somam **75** espécies (20 Baby + 23 In-Training + 32 Rookie), não 73, e todas já têm passiva herdada. Mega (48 sem passiva) e MegaPlus (2 sem passiva) ficam fora deste levantamento.

## Como ler a tabela

- **Role, Ataque e Elemento** vêm do JSON da espécie.
- **Passivas próprias** é o pool `Passives` do JSON, pra não criar uma herdada que repita a identidade da própria.
- **Tema** é uma palavra-chave da franquia (golpes e aparência da espécie), escrita de memória e **não** tirada de dado do jogo. Serve como ponto de partida pra criar passivas com identidade, e vale conferir antes de usar.
- **Paildramon** é, na franquia, a fusão (DNA) de ExVeemon e Stingmon, e os dois estão neste lote. A passiva dele pode conversar com a dos dois.

## Champion (30)

| Id | Nome | Role | Ataque | Elemento | Passivas próprias | Tema |
|---|---|---|---|---|---|---|
| 15 | Kabuterimon | Tank | Especial | Elétrico | GritoDeGuerra, Espinhos, UltimaTrincheira | inseto, eletricidade |
| 16 | Angemon | Ranged | Especial | Luz | PosturaFirme, Predicao, Fixacao | anjo, luz sagrada |
| 17 | Togemon | Warrior | Físico | Terra | Investida, SedeDeBatalha, RompeGuarda | cacto, espinhos |
| 18 | Growlmon | Warrior | Físico | Fogo | RompeGuarda, GolpePesado, Teimosia | dragão, fogo, fúria |
| 19 | Gargomon | Ranged | Físico | Vento | PesLeves, ProjetilPerfurante, AlvoMarcado | metralhadoras, rajada |
| 20 | Kyubimon | Ranged | Especial | Fogo | AlvoMarcado, ProjetilPerfurante, Predicao | raposa de nove caudas, chamas |
| 21 | Wizardmon | Ranged | Especial | Trevas | PesLeves, Predicao, Fixacao | mago, feitiço |
| 23 | Gaogamon | Warrior | Físico | Vento | RompeGuarda, GolpePesado, Teimosia | lobo, garras, investida |
| 24 | Sunflowmon | Support (Debuffer) | Especial | Terra | MarcaDupla, AuraVital | girassol, sol, enfraquecer |
| 44 | Geogreymon | Warrior | Físico | Fogo | RompeGuarda, GolpePesado, Teimosia | dinossauro, chifre, investida |
| 47 | Devidramon | Assassin | Físico | Trevas | PrecisaoLetal, Execucao, PassoFantasma | dragão demoníaco, garras, olhar |
| 60 | Birdramon | Ranged | Especial | Vento | Fixacao, PosturaFirme, ProjetilPerfurante | fênix, fogo, voo |
| 66 | Ikkakumon | Tank | Físico | Água | GritoDeGuerra, Espinhos, UltimaTrincheira | morsa, chifre, mar gelado |
| 79 | Galemon | Ranged | Especial | Vento | PosturaFirme, Predicao, Fixacao | tufão, vento |
| 92 | ShoeShoemon | Support (Buffer) | Especial | Trevas | DuplaVoz, InstintoDeFuga, Ressonancia | travessura, encanto |
| 97 | Cockatrimon | Warrior | Físico | Vento | RompeGuarda, GolpePesado, Teimosia | petrificação, ave |
| 103 | Bakemon | Assassin | Especial | Trevas | FioDaLamina, GolpeSorrateiro, Infiltrador | fantasma, assombração |
| 110 | Tortamon | Tank | Físico | Terra | GritoDeGuerra, ProvocacaoInsistente, Couraca | tartaruga, carapaça |
| 117 | Snatchmon | Ranged | Especial | Trevas | PesLeves, Predicao, Fixacao | tentáculos, roubo |
| 130 | Reppamon | Ranged | Especial | Luz | PosturaFirme, Predicao, Fixacao | lâminas de vento, fera |
| 133 | Coredramon (Green) | Warrior | Físico | Terra | Investida, SedeDeBatalha, RompeGuarda | dragão terrestre, força bruta |
| 136 | Coredramon (Blue) | Ranged | Físico | Vento | AlvoMarcado, ProjetilPerfurante, Predicao | dragão do céu, sopro |
| 140 | Veedramon | Warrior | Físico | Fogo | RompeGuarda, GolpePesado, Teimosia | dragão, golpe em V |
| 146 | Gladimon | Warrior | Físico | Neutro | RompeGuarda, GolpePesado, Teimosia | gladiador, espada |
| 150 | Dorugamon | Warrior | Físico | Vento | RompeGuarda, GolpePesado, Teimosia | dragão, projéteis de ferro |
| 153 | Raptordramon | Warrior | Físico | Terra | Investida, SedeDeBatalha, RompeGuarda | raptor metálico, velocidade |
| 156 | DexDorugamon | Assassin | Físico | Trevas | PrecisaoLetal, Execucao, PassoFantasma | corrompido, decadência |
| 164 | BaoHackmon | Assassin | Físico | Neutro | PrecisaoLetal, FioDaLamina, PassoFantasma | treino, lâminas |
| 191 | ExVeemon | Warrior | Físico | Fogo | RompeGuarda, GolpePesado, Teimosia | dragão, raio em X |
| 198 | Stingmon | Assassin | Físico | Vento | PrecisaoLetal, Execucao, PassoFantasma | inseto, ferrão, ninja |

## Ultimate (26)

| Id | Nome | Role | Ataque | Elemento | Passivas próprias | Tema |
|---|---|---|---|---|---|---|
| 41 | WarGrowlmon | Warrior | Físico | Fogo | Investida, GolpePesado, RompeGuarda | ciborgue, canhões |
| 42 | Rapidmon | Ranged | Físico | Vento | PesLeves, ProjetilPerfurante, AlvoMarcado | mísseis teleguiados, rapidez |
| 43 | MagnaAngemon | Ranged | Especial | Luz | PesLeves, ProjetilPerfurante, AlvoMarcado | anjo, banimento (Portão) |
| 49 | Taomon | Ranged | Especial | Fogo | PesLeves, ProjetilPerfurante, AlvoMarcado | talismãs, proteção |
| 50 | Lillymon | Ranged | Especial | Planta | AlvoMarcado, PosturaFirme, Predicao | fada, flor, canhão |
| 51 | MegaKabuterimon | Tank | Especial | Elétrico | GritoDeGuerra, Espinhos, UltimaTrincheira | besouro gigante, trovão |
| 61 | Garudamon | Ranged | Especial | Vento | Fixacao, PosturaFirme, ProjetilPerfurante | ave guerreira, asas de fogo |
| 67 | Zudomon | Tank | Físico | Água | Couraca, Folego, ProvocacaoInsistente | martelo, carapaça, mar |
| 72 | Angewomon | Ranged | Especial | Luz | PesLeves, ProjetilPerfurante, AlvoMarcado | anjo, flecha sagrada, cura |
| 80 | Grand Galemon | Ranged | Especial | Vento | PesLeves, Predicao, Fixacao | furacão |
| 93 | Chaperomon | Support (Buffer) | Especial | Trevas | Ressonancia, DuplaVoz, AuraVital | disfarce, conto de fadas |
| 98 | Delumon | Warrior | Físico | Vento | RompeGuarda, GolpePesado, Teimosia | ave, ovos explosivos |
| 104 | Fantomon | Assassin | Especial | Trevas | GolpeSorrateiro, PassoFantasma, Infiltrador | ceifador, foice |
| 131 | Chirinmon | Ranged | Especial | Luz | PesLeves, ProjetilPerfurante, AlvoMarcado | qilin, luz, raios |
| 134 | Groundramon | Tank | Físico | Terra | Couraca, Folego, ProvocacaoInsistente | dragão terrestre, tremor |
| 137 | Wingdramon | Ranged | Físico | Vento | PesLeves, ProjetilPerfurante, AlvoMarcado | dragão celeste, supersônico |
| 141 | AeroVeedramon | Ranged | Físico | Vento | PesLeves, ProjetilPerfurante, AlvoMarcado | dragão alado, onda em V |
| 147 | Knightmon | Tank | Físico | Neutro | Couraca, Folego, ProvocacaoInsistente | cavaleiro, armadura |
| 151 | Dorugreymon | Warrior | Físico | Fogo | SedeDeBatalha, FuriaCrescente, Teimosia | dragão, metal, asas |
| 154 | Grademon | Tank | Físico | Terra | Couraca, Espinhos, UltimaTrincheira | espadachim, lâminas cruzadas |
| 157 | DexDorugreymon | Assassin | Físico | Trevas | Execucao, PrecisaoLetal, GolpeSorrateiro | corrompido, devora dados |
| 165 | SaviorHackmon | Assassin | Físico | Luz | Infiltrador, Execucao, FioDaLamina | cavaleiro sagrado, lança e escudo |
| 170 | Mistymon | Assassin | Físico | Trevas | PassoFantasma, PrecisaoLetal, FioDaLamina | névoa, ilusão |
| 179 | Lilamon | Support (Healer) | Especial | Planta | MaosFirmes, InstintoDeFuga, AuraVital | fada, flor, cura |
| 192 | Paildramon | Ranged | Físico | Fogo | Fixacao, PosturaFirme, ProjetilPerfurante | fusão (DNA), metralhadoras |
| 199 | JewelBeemon | Assassin | Físico | Vento | Execucao, PrecisaoLetal, GolpeSorrateiro | abelha, lança, perfuração |

## Resumo do lote

- **Champion:** Warrior 11, Ranged 9, Assassin 5, Tank 3, Support 2 · Físico 19, Especial 11
- **Ultimate:** Ranged 11, Tank 5, Assassin 5, Warrior 3, Support 2 · Físico 15, Especial 11

## Passivas que o piloto já usa nesses estágios

Pra não repetir efeito ao criar as novas:

### Champion

- **Devimon:** `PeriodicDefensePierce` (3 / 0.2)
- **Dimetromon:** `SpeedAndBonusVsActedTarget` (0.05 / 0.08)
- **Garurumon:** `RangedExtraStrike` (0.15 / 0.3)
- **Greymon:** `CritShortensDebuffs` (1 / 0)
- **IceDevimon:** `FirstHitTakenReduction` (0.2 / 0)

### Ultimate

- **Astamon:** `StunChance` (0.1 / 0.2)
- **Destromon:** `RangedStacksGuaranteedCrit` (3 / 0)
- **LadyDevimon:** `KillSpeedBonus` (0.1 / 0)
- **Lamiamon:** `DodgeThenGuaranteedCrit` (0.06 / 0)
- **Lucemon Falldown Mode:** `CritLifesteal` (0.06 / 0)
- **MachGaogamon:** `OpeningHitBonus` (0.06 / 0)
- **MetalGreymon:** `LowHpPhysicalDamageBonus` (0.5 / 0.15)
- **Myotismon:** `HealLowestAllyOnAttack` (0.1 / 0)
- **RizeGreymon:** `CritHealMaxHp` (0.04 / 0)
- **SkullSatamon:** `AntiTank` (0.08 / 0.5)
- **Triceramon:** `NegateDamageChance` (0.2 / 0)
- **WereGarurumon:** `PhysicalDamageWithOpeningBonus` (0.08 / 0.16)
