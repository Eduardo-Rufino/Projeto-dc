import glob
import json
import statistics
from collections import Counter, defaultdict

import os
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..")) + "/"
OUT = ROOT + "REQUISITOS_EVOLUCAO.md"

STAGE_ORDER = ["Baby", "InTraining", "Rookie", "Champion", "Ultimate", "Mega", "MegaPlus", "Special"]
MAX_AGE = {"Baby": 4, "InTraining": 7, "Rookie": 15, "Champion": 26, "Ultimate": 52, "Mega": 68, "MegaPlus": 68, "Special": 68}
CAPACITY = {"Baby": 1, "InTraining": 2, "Rookie": 3, "Champion": 5, "Ultimate": 7, "Mega": 10, "MegaPlus": 12, "Special": 15}


def load(path):
    return json.load(open(path, encoding="utf-8-sig"))


def lower_keys(d):
    return {k.lower(): v for k, v in d.items()}


# ---------------------------------------------------------------- dados
digimon = {}
for f in glob.glob(ROOT + "Data/Digimon/*.json"):
    d = lower_keys(load(f))
    d["basestats"] = lower_keys(d.get("basestats", {}))
    digimon[d["id"]] = d
by_code = {d["code"]: d for d in digimon.values()}

edges = []
for f in sorted(glob.glob(ROOT + "Data/Evolutions/*.json")):
    for e in load(f):
        e = lower_keys(e)
        e["requiredstats"] = lower_keys(e.get("requiredstats") or {})
        edges.append(e)

web = load(ROOT + "Data/Evolution/evolution_web.json")
web_meta = {(e["from"], e["to"]): (e["type"], e["source"]) for e in web["edges"]}
web_dna = web.get("dna", [])

jogress = []
for f in sorted(glob.glob(ROOT + "Data/Jogress/*.json")):
    jogress.append(lower_keys(load(f)))

out_edges = defaultdict(list)
in_edges = defaultdict(list)
for e in edges:
    out_edges[e["fromdigimonid"]].append(e)
    in_edges[e["todigimonid"]].append(e)

jog_in = defaultdict(list)
for j in jogress:
    jog_in[j["todigimonid"]].append(j)


def name(i):
    return digimon[i]["name"]


def stage_rank(i):
    s = digimon[i]["stage"]
    return STAGE_ORDER.index(s) if s in STAGE_ORDER else 99


def score(e):
    s = e["requiredstats"]
    total = e["requiredlevel"] + e["requiredageindays"]
    total += sum(s.get(k, 0) for k in ("healthpoints", "physicaldamage", "physicaldefense", "specialdamage", "specialdefense", "speed"))
    total += e.get("requiredbattles", 0)
    total += int(e.get("requiredwinrate", 0) * 100)
    total += e.get("mindiscipline", 0)
    total += 100 - e.get("maxdiscipline", 100)
    total += e.get("minhappiness", 0)
    total += 100 - e.get("maxhappiness", 100)
    return total


def dash(v):
    return "-" if not v else str(v)


def others(e):
    parts = []
    if e.get("requiredbattles", 0):
        parts.append(f"Batalhas≥{e['requiredbattles']}")
    if e.get("requiredwinrate", 0):
        parts.append(f"Vitórias≥{round(e['requiredwinrate'] * 100)}%")
    if e.get("mindiscipline", 0):
        parts.append(f"Disc≥{e['mindiscipline']}")
    if e.get("maxdiscipline", 100) < 100:
        parts.append(f"Disc≤{e['maxdiscipline']}")
    if e.get("minhappiness", 0):
        parts.append(f"Felic≥{e['minhappiness']}")
    if e.get("maxhappiness", 100) < 100:
        parts.append(f"Felic≤{e['maxhappiness']}")
    return "; ".join(parts) if parts else "-"


def stat_line(bs):
    return f"HP {bs['healthpoints']} / PA {bs['physicaldamage']} / PD {bs['physicaldefense']} / EA {bs['specialdamage']} / ED {bs['specialdefense']} / Vel {bs['speed']}"


# ---------------------------------------------------------------- cabeçalho / contexto
L = []
w = L.append

w("# Requisitos de evolução - Projeto DC (Digimon Training Center)")
w("")
w("Arquivo gerado automaticamente a partir dos dados do jogo (`Data/Digimon`, `Data/Evolutions`, `Data/Evolution/evolution_web.json`, `Data/Jogress`). "
  "Serve de entrada pra revisar e melhorar os requisitos de evolução. **Os números em cada tabela são os valores reais em uso hoje.**")
w("")
w(f"- Digimons no jogo: **{len(digimon)}** | evoluções normais (com requisitos): **{len(edges)}** | receitas Jogress: **{len(jogress)}**")
w("- Estágios (ordem): Baby -> InTraining -> Rookie -> Champion -> Ultimate -> Mega -> MegaPlus (existe também Special, sem linha própria de evolução)")
w("")

w("## 1. Objetivo da revisão (o que peço ao revisor)")
w("")
w("Verifique e proponha melhorias nos requisitos de evolução, respondendo com **listas de alterações concretas** (Digimon de origem -> destino: campo: valor atual -> valor novo, e o motivo). Pontos a avaliar:")
w("")
w("1. **Coerência de dificuldade por estágio**: níveis, idades e stats exigidos crescem de forma sensata de Rookie -> Champion -> Ultimate -> Mega? Há saltos estranhos ou requisitos fora do padrão da própria faixa?")
w("2. **Viabilidade dentro do tempo de vida** (seção 3): todo Digimon morre de velhice se passar do teto de idade do estágio atual. Cada evolução deve ser alcançável no tempo disponível por um jogador que treina/luta com esforço moderado, e as formas 'premium' devem exigir esforço alto ou um Centro mais evoluído.")
w("3. **Afinidade com a Role/atributos**: os stats exigidos combinam com a Role e o `AttackType` de quem evolui e de quem nasce? (Ex.: uma evolução de Tank deveria pedir PD/ED/HP; um Ranged/Mage, EA.) O jogador escolhe o caminho treinando certos stats: os caminhos irmãos precisam ser **distinguíveis** e alcançáveis por treino diferente.")
w("4. **Empates e ambiguidade**: quando várias evoluções ficam válidas ao mesmo tempo, vence a de maior 'score' (seção 2.3); empate = escolha aleatória. Empates entre irmãos devem ser evitados, exceto de propósito.")
w("5. **Stats especiais**: o nível NÃO aumenta EA/ED, só o treino e o multiplicador de evolução (seção 2.4). Requisitos altos de EA/ED em Digimons que não treinam isso são difíceis de justificar.")
w("6. **Caminhos 'sombrios'/'queridos'**: uso de Disciplina/Felicidade mínima e máxima, batalhas e taxa de vitória. Há evoluções onde esses requisitos deveriam existir e não existem (ou o contrário)?")
w("7. **Progressão de multiplicador** (`statMultiplier`): a recompensa de cada evolução é proporcional à dificuldade?")
w("8. Qualquer **beco sem saída** ou Digimon cujas evoluções sejam sempre tudo-ou-nada (uma única opção sem alternativa fraca).")
w("")
w("Restrições: não crie/renomeie Digimons; só ajuste requisitos e (se realmente necessário) sugira novas ligações de evolução, marcando-as como sugestão. Preserve o espírito canônico onde a fonte é `canon` (seção 2.5).")
w("")

w("## 2. Como as evoluções funcionam hoje")
w("")
w("### 2.1 Requisitos (todos precisam ser atendidos ao mesmo tempo)")
w("")
w("| Campo | Significado |")
w("|---|---|")
w("| `Lv` (requiredLevel) | Nível mínimo do Digimon |")
w("| `Idade` (requiredAgeInDays) | Idade mínima em dias de jogo. **A idade é acumulada desde o nascimento e NÃO reseta ao evoluir.** |")
w("| `HP`, `PA`, `PD`, `EA`, `ED`, `Vel` (requiredStats) | Valor **atual** mínimo de cada stat: HP, Ataque Físico, Defesa Física, Ataque Especial, Defesa Especial, Velocidade. `-` = 0 = sem exigência |")
w("| `Batalhas` | Nº mínimo de batalhas lutadas (vitória ou derrota) |")
w("| `Vitórias%` | Taxa mínima de vitórias (só faz sentido com Batalhas>0) |")
w("| `Disc≥/≤` | Disciplina mínima/máxima (0-100) |")
w("| `Felic≥/≤` | Felicidade mínima/máxima (0-100) |")
w("| `xMult` (statMultiplier) | Multiplicador aplicado aos stats atuais ao evoluir (seção 2.4) |")
w("| `Score` | Soma usada pra desempate entre evoluções válidas (2.3) |")
w("")
w("A evolução é verificada automaticamente no fim de cada dia, depois de cada treino e depois de cada batalha (o jogador não escolhe o momento; a evolução com maior score entre as válidas é aplicada). "
  "Ela só é aplicada se houver **capacidade** no Centro (a forma nova ocupa mais capacidade); senão fica bloqueada até haver espaço. "
  "O jogador também pode, com um NPC recrutado (Wizardmon), **bloquear** evoluções específicas pra escolher o caminho.")
w("")
w("### 2.2 Custo de capacidade e tetos de idade por estágio")
w("")
w("| Estágio | Capacidade que ocupa | Teto de idade (morre se `Idade` > teto sem evoluir) |")
w("|---|---|---|")
for s in STAGE_ORDER:
    if s in MAX_AGE:
        w(f"| {s} | {CAPACITY[s]} | {MAX_AGE[s]} dias |")
w("")
w("O teto vale para o estágio **atual** e a idade é acumulada. Ex.: um Digimon que ainda é Rookie no dia 16 morre; um que evoluiu pra Champion no dia 14 ganha o teto de Champion (26).")
w("")
w("### 2.3 Como a evolução é escolhida")
w("")
w("Entre as evoluções cujos requisitos estão TODOS atendidos, vence a de **maior Score**; empate no Score = sorteio aleatório.")
w("")
w("`Score = Lv + Idade + HP + PA + PD + EA + ED + Vel + Batalhas + Vitórias%(x100) + Disc≥ + (100 - Disc≤) + Felic≥ + (100 - Felic≤)`")
w("")
w("### 2.4 Crescimento dos stats (base pra calibrar requisitos)")
w("")
w("- **Stats iniciais** = `BaseStats` da espécie (aparecem no cabeçalho de cada Digimon, seção 7).")
w("- **A cada nível**: +2 PA, +2 PD, +1 Vel, +5 HP. **EA e ED não crescem por nível.**")
w("- **Treino** (custa 10 de stamina, dá 5 XP): o jogador escolhe 1 stat. Resultado sorteado 1/2/3 com chance 25%/50%/25% (média 2). Ganho = resultado no stat escolhido (PA, PD, EA, ED ou Vel) e **6x o resultado no HP** (média 12). "
  "Áreas de treino específicas dão x1,15 e NPC recrutado da especialidade x1,15 (empilham). Cada treino também dá +10 Felicidade e +5 Disciplina.")
w("- **Stamina**: regenera 72 por dia (cerca de 7 treinos/dia sem Energético; o Energético recupera stamina e custa Bits).")
w("- **Ao evoluir**: cada stat atual vira `max(stat x xMult, lerp(stat x xMult, stat_da_ficha_da_nova_forma, 0.4))` - multiplica o treino acumulado e puxa um pouco em direção à ficha da nova espécie, sem nunca reduzir.")
w("- **XP**: nível N -> N+1 custa `100 x 1,2^(N-1)`. XP total acumulado pra chegar ao nível N (partindo do 1):")
w("")
xp = 100.0
cum = 0.0
rows = []
for lv in range(2, 46):
    cum += xp
    rows.append((lv, int(cum)))
    xp *= 1.2
w("| Nível | 5 | 10 | 12 | 15 | 18 | 20 | 24 | 28 | 30 | 36 | 37 | 40 | 45 |")
w("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|")
pick = [5, 10, 12, 15, 18, 20, 24, 28, 30, 36, 37, 40, 45]
cmap = dict(rows)
w("| XP total | " + " | ".join(f"{cmap[p]:,}".replace(",", ".") for p in pick) + " |")
w("")
w("- **Fontes de XP**: treino (5), batalhas (XP base por estágio do inimigo: 80/180/400/900 para Rookie/Champion/Ultimate/Mega, x um multiplicador 0,5-1,5 conforme a diferença de nível), exploração, e conjuntos completos (+8% XP). Derrota dá só um XP de consolação.")
w("- **Felicidade/Disciplina**: começam em 50. Felicidade sobe ao comer (+5), treinar (+10), vencer (+8), dormir no Dormitório; cai com fome, doença (-8), derrota (-6), comida estragada, sujeira e ser acordado. "
  "Disciplina sobe treinando (+5), curando com remédio (+5) e dormindo no Dormitório (+3); cai com comida estragada, sujeira e treino falho.")
w("")
w("### 2.5 Fonte das ligações (evolution_web)")
w("")
c = Counter(web_meta.values())
w("- `canon`: baseado em v-pet/anime/jogos; `design`: escolha de game design sem linha oficial. Nas tabelas, a coluna `Fonte` traz isso por ligação.")
w(f"- Ligações no grafo: {sum(1 for k, v in c.items() if k[0] == 'normal' for _ in range(v))} normais "
  f"({c[('normal', 'canon')]} canon + {c[('normal', 'design')]} design), {sum(v for k, v in c.items() if k[0] == 'dna')} DNA/Jogress e {sum(v for k, v in c.items() if k[0] == 'mode')} troca de modo.")
w("")

# ---------------------------------------------------------------- resultados de viabilidade
w("## 3. Viabilidade medida (simulação de jogador ideal)")
w("")
w("Uma simulação com o código real (treino + batalhas por dia, stamina, XP, tetos de idade acima) mediu se cada faixa de nível exigido é alcançável antes de o Digimon morrer, "
  "com um jogador que treina só o que a evolução pede. \"Folga\" = dias que sobram antes do teto de idade. Resultado com os requisitos atuais:")
w("")
w("| Faixa (nível exigido pela evolução) | Esforço moderado (~4 batalhas/dia, sem Energético) | Esforço alto e/ou Centro evoluído |")
w("|---|---|---|")
w("| Rookie -> Champion, nível 12 (base) | todas viáveis (folga ~2 dias) | - |")
w("| Rookie -> Champion, nível 18 (premium) | inviável | só com esforço alto + Energético e/ou Centro evoluído; com Centro evoluído a folga é ~8 dias |")
w("| Champion -> Ultimate, nível 20 (base) | todas viáveis (folga ~1 dia) | - |")
w("| Champion -> Ultimate, nível 28 (premium) | inviável | só com esforço alto + Energético e/ou Centro evoluído; com Centro evoluído a folga é ~3 dias |")
w("| Ultimate -> Mega, nível 28 (base) | precisa de >= 5 batalhas/dia (folga ~6 dias) | viável |")
w("| Ultimate -> Mega, níveis 36/37 (premium) | inviável | só com esforço alto + Energético e/ou Centro evoluído; com Centro evoluído a folga é ~1 a 3 dias |")
w("")
w("\"Centro evoluído\" na simulação = 12 batalhas/dia, XP x1,6 e ganho de treino x1,32 (áreas de treino específicas, NPCs recrutados e conjuntos completos empilhados). "
  "Só as folgas do Centro evoluído e as linhas de esforço moderado foram medidas diretamente; \"esforço alto\" sem Centro evoluído não foi tabelado por faixa.")
w("")
w("A intenção de design: com esforço moderado o Digimon chega a uma **forma mais fraca/menos desejada** de cada estágio; as formas mais fortes (níveis exigidos maiores) exigem **mais esforço ou um Centro mais evoluído**. "
  "Só morre sem evoluir quem cuida/treina mal. Ao mudar níveis/idades/stats exigidos, preserve esse gradiente.")
w("")

# ---------------------------------------------------------------- visão geral por transição
w("## 4. Visão geral por transição de estágio")
w("")
w("| Transição | Nº de evoluções | Nível (mín / mediana / máx) | Idade (mín / mediana / máx) | Soma dos stats exigidos (mín / mediana / máx) | Com Batalhas/Vitórias/Disc/Felic | xMult (mín-máx) |")
w("|---|---|---|---|---|---|---|")
groups = defaultdict(list)
for e in edges:
    a = digimon[e["fromdigimonid"]]["stage"]
    b = digimon[e["todigimonid"]]["stage"]
    groups[(a, b)].append(e)


def stat_sum(e):
    s = e["requiredstats"]
    return sum(s.get(k, 0) for k in ("healthpoints", "physicaldamage", "physicaldefense", "specialdamage", "specialdefense", "speed"))


def has_extra(e):
    return others(e) != "-"


def trio(values):
    return f"{min(values)} / {int(statistics.median(values))} / {max(values)}"


for key in sorted(groups, key=lambda k: (STAGE_ORDER.index(k[0]) if k[0] in STAGE_ORDER else 99, STAGE_ORDER.index(k[1]) if k[1] in STAGE_ORDER else 99)):
    g = groups[key]
    mults = [e["statmultiplier"] for e in g]
    w(f"| {key[0]} -> {key[1]} | {len(g)} | {trio([e['requiredlevel'] for e in g])} | {trio([e['requiredageindays'] for e in g])} | "
      f"{trio([stat_sum(e) for e in g])} | {sum(1 for e in g if has_extra(e))} | {min(mults)}-{max(mults)} |")
w("")

# ---------------------------------------------------------------- alertas automáticos
w("## 5. Pontos de atenção detectados automaticamente")
w("")
w("Fatos objetivos extraídos dos dados (não são julgamentos de design).")
w("")

# 5.1 idade impossível
imp = [e for e in edges if e["requiredageindays"] > MAX_AGE.get(digimon[e["fromdigimonid"]]["stage"], 999)]
w(f"### 5.1 Evoluções cuja idade mínima excede o teto de idade do estágio de origem: {len(imp)}")
for e in imp:
    w(f"- {name(e['fromdigimonid'])} -> {name(e['todigimonid'])}: idade {e['requiredageindays']} > teto {MAX_AGE[digimon[e['fromdigimonid']]['stage']]}")
if not imp:
    w("- nenhuma")
w("")

# 5.2 empates
w("### 5.2 Empates de score entre evoluções irmãs (mesma origem) - escolha aleatória se ambas ficarem válidas")
ties = []
for src, lst in out_edges.items():
    byscore = defaultdict(list)
    for e in lst:
        byscore[score(e)].append(e)
    for sc, group in byscore.items():
        if len(group) > 1:
            ties.append((src, sc, group))
ties.sort(key=lambda t: (stage_rank(t[0]), t[0]))
w(f"Total de grupos empatados: **{len(ties)}**")
for src, sc, group in ties:
    w(f"- {name(src)} ({digimon[src]['stage']}), score {sc}: " + ", ".join(name(e["todigimonid"]) for e in group))
if not ties:
    w("- nenhum")
w("")

# 5.3 só nível/idade
w("### 5.3 Evoluções sem nenhum requisito de stat (só nível/idade/outros)")
flat = [e for e in edges if stat_sum(e) == 0]
w(f"Total: **{len(flat)}**")
for e in flat[:60]:
    w(f"- {name(e['fromdigimonid'])} -> {name(e['todigimonid'])} (Lv {e['requiredlevel']}, idade {e['requiredageindays']}, {others(e)})")
w("")

# 5.4 becos sem saída
w("### 5.4 Digimons que não evoluem (sem saída) fora do último estágio")
dead = [d for d in digimon.values() if d["id"] not in out_edges and d["stage"] in STAGE_ORDER[:-3]]
for d in sorted(dead, key=lambda d: (STAGE_ORDER.index(d["stage"]), d["id"])):
    w(f"- {d['name']} ({d['stage']})")
if not dead:
    w("- nenhum")
w("")

# 5.5 stats altos de EA/ED
w("### 5.5 Evoluções que exigem EA/ED sem o Digimon de origem ter Role/AttackType especial")
mism = []
for e in edges:
    s = e["requiredstats"]
    src = digimon[e["fromdigimonid"]]
    if (s.get("specialdamage", 0) or s.get("specialdefense", 0)) and src.get("attacktype") == "Physical":
        mism.append(e)
w(f"Origem com `AttackType` Physical exigindo EA/ED > 0: **{len(mism)}** evoluções (EA/ED só sobem por treino). Exemplos: "
  + "; ".join(f"{name(e['fromdigimonid'])}->{name(e['todigimonid'])} (EA {e['requiredstats'].get('specialdamage', 0)}, ED {e['requiredstats'].get('specialdefense', 0)})" for e in mism[:12]))
w("")

# ---------------------------------------------------------------- Jogress
w("## 6. Receitas Jogress (fusão de dois Digimons)")
w("")
w("Sem requisito de nível/stats: só precisa dos dois Digimons certos e capacidade. O resultado é um Digimon **novo** com stats = mistura da média dos pais (multiplicador abaixo).")
w("")
w("| Resultado | Digimon A | Digimon B | xMult |")
w("|---|---|---|---|")
for j in jogress:
    w(f"| {name(j['todigimonid'])} ({digimon[j['todigimonid']]['stage']}) | {name(j['digimonaid'])} ({digimon[j['digimonaid']]['stage']}) | {name(j['digimonbid'])} ({digimon[j['digimonbid']]['stage']}) | {j['statmultiplier']} |")
w("")

# ---------------------------------------------------------------- por digimon
w("## 7. Todos os Digimons e suas evoluções")
w("")
w("Legenda das colunas: `Lv`, `Idade`, stats (`HP PA PD EA ED Vel`), `Outros` (Batalhas/Vitórias/Disciplina/Felicidade), `xMult`, `Score`, `Fonte` (canon/design), `Tipo` (normal/dna/mode). "
  "`PA`=Ataque Físico, `PD`=Defesa Física, `EA`=Ataque Especial, `ED`=Defesa Especial.")
w("")

by_stage = defaultdict(list)
for d in digimon.values():
    by_stage[d["stage"]].append(d)

for stage in STAGE_ORDER:
    if stage not in by_stage:
        continue
    w(f"### Estágio {stage} ({len(by_stage[stage])} Digimons)")
    w("")
    for d in sorted(by_stage[stage], key=lambda x: x["id"]):
        i = d["id"]
        extras = []
        if d.get("supporttype") and d.get("role") == "Support":
            extras.append(f"Suporte: {d['supporttype']}")
        head = (f"#### {d['name']} (#{i}) - {d['stage']} | Role {d.get('role', '?')} | {d.get('attacktype', '?')} | "
                f"{d.get('attribute', '?')} / {d.get('element', '?')} | Ovo: {d.get('eggtype', '?')}")
        if extras:
            head += " | " + ", ".join(extras)
        w(head)
        w("")
        w(f"- Stats base: {stat_line(d['basestats'])}")
        srcs = [f"{name(e['fromdigimonid'])} (#{e['fromdigimonid']}, Lv {e['requiredlevel']})" for e in in_edges.get(i, [])]
        srcs += [f"Jogress: {name(j['digimonaid'])} + {name(j['digimonbid'])}" for j in jog_in.get(i, [])]
        if srcs:
            w("- Evolui de: " + "; ".join(srcs))
        elif d["stage"] == "Baby":
            w("- Evolui de: nasce de ovo do tipo " + str(d.get("eggtype", "?")))
        else:
            w("- Evolui de: (ninguém - sem ligação de entrada com requisitos)")
        outs = sorted(out_edges.get(i, []), key=lambda e: (stage_rank(e["todigimonid"]), e["requiredlevel"], e["todigimonid"]))
        if not outs:
            w("- Evolui para: (nenhum)")
            w("")
            continue
        w("")
        w("| Para | Estágio | Lv | Idade | HP | PA | PD | EA | ED | Vel | Outros | xMult | Score | Fonte | Tipo |")
        w("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|")
        for e in outs:
            t = e["todigimonid"]
            s = e["requiredstats"]
            meta = web_meta.get((d["code"], digimon[t]["code"]), ("?", "?"))
            w(f"| {name(t)} (#{t}) | {digimon[t]['stage']} | {e['requiredlevel']} | {dash(e['requiredageindays'])} | "
              f"{dash(s.get('healthpoints', 0))} | {dash(s.get('physicaldamage', 0))} | {dash(s.get('physicaldefense', 0))} | "
              f"{dash(s.get('specialdamage', 0))} | {dash(s.get('specialdefense', 0))} | {dash(s.get('speed', 0))} | "
              f"{others(e)} | {e['statmultiplier']} | {score(e)} | {meta[1]} | {meta[0]} |")
        w("")

with open(OUT, "w", encoding="utf-8", newline="\n") as fh:
    fh.write("\n".join(L) + "\n")

print("linhas:", len(L), "bytes:", len(open(OUT, "rb").read()))
print("digimons:", len(digimon), "edges:", len(edges), "ties:", len(ties), "flat:", len(flat), "imp:", len(imp), "dead:", len(dead), "mism:", len(mism))
