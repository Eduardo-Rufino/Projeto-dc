"""verificar.py — checagem independente dos requisitos de evolução.

Uso:
  python3 verificar.py REQUISITOS_EVOLUCAO.md                 # mede os dados como estão
  python3 verificar.py REQUISITOS_EVOLUCAO.md --csv revisado.csv   # mede o CSV proposto
  python3 verificar.py REQUISITOS_EVOLUCAO.md --json saida.json    # também grava o relatório
  python3 verificar.py REQUISITOS_EVOLUCAO.md --k 7                # 7 treinos acumulados entre checagens

--k modela o jogo real: o treino (autônomo, na sala) não checa evolução; a checagem só ocorre
após batalha (fora de torneio) e à meia-noite. K = quantos treinos acumulam entre duas checagens.
Subida de nível sempre vem de batalha, então há checagem a cada nível.

Só lê arquivos. Não altera nada no projeto.

O que mede (modelo simplificado das regras da seção 2 do REQUISITOS_EVOLUCAO.md):
  A. Alcançabilidade: para cada evolução T de um pai, um jogador que MIRA em T treina só o
     que falta para T, na ordem mais favorável, e a checagem automática (a cada K treinos e a cada nível)
     aplica a irmã válida de maior score (maior score; empate = sorteio). T é "inalcançável" se em
     nenhuma ordem ela é a vencedora.
  B. Forma padrão: irmã com Idade >= teto do estágio - 2 é tratada como rede de segurança;
     verifica que nenhuma outra irmã fica válida só de subir de nível (sem treinar).
  C. Empates de score entre irmãs.
  D. Pais sem nenhuma rota para quem não se especializa (sem forma padrão nem irmã barata).
Premissas do modelo: ver PREMISSAS no PROMPT. Se o código real divergir delas, os números
mudam — é exatamente isso que precisa ser conferido antes de aplicar.
"""
import argparse, csv, itertools, json, math, re, sys
from collections import defaultdict

STATS = ['HP', 'PA', 'PD', 'EA', 'ED', 'Vel']
TRAIN = ['PA', 'PD', 'EA', 'ED', 'Vel']
GROWTH = dict(HP=5, PA=2, PD=2, EA=0, ED=0, Vel=1)          # por nível (seção 2.4)
TETO = dict(Baby=4, InTraining=7, Rookie=15, Champion=26, Ultimate=52, Mega=68)  # seção 2.2
SAFE = {k: v - 2 for k, v in TETO.items()}


# ------------------------------------------------------------------ leitura
def parse_md(path):
    lines = open(path, encoding='utf-8').read().split('\n')
    hdr = re.compile(r'^#### (.+?) \(#(\d+)\) - (.+)$')
    base = re.compile(r'^- Stats base: HP (\d+) / PA (\d+) / PD (\d+) / EA (\d+) / ED (\d+) / Vel (\d+)')
    D, E, cur = {}, [], None
    for ln in lines:
        m = hdr.match(ln)
        if m:
            parts = [p.strip() for p in m.group(3).split('|')]
            cur = dict(name=m.group(1), stage=parts[0], role=parts[1].replace('Role ', ''),
                       atk=parts[2] if len(parts) > 2 else '', base=None)
            D[cur['name']] = cur
            continue
        if cur is None:
            continue
        mb = base.match(ln)
        if mb:
            cur['base'] = dict(zip(STATS, map(int, mb.groups())))
            continue
        if ln.startswith('| ') and not ln.startswith('| Para') and not ln.startswith('|---'):
            c = [x.strip() for x in ln.strip().strip('|').split('|')]
            if len(c) != 15:
                continue
            mt = re.match(r'^(.+?) \(#\d+\)$', c[0])
            if not mt:
                continue
            n = lambda v: 0 if v == '-' else int(v)
            e = dict(src=cur['name'], src_stage=cur['stage'], dst=mt.group(1), dst_stage=c[1],
                     lv=n(c[2]), age=n(c[3]), **{s: n(c[4 + i]) for i, s in enumerate(STATS)},
                     mult=float(c[11]), tipo=c[14], batalhas=0, vitorias=0.0,
                     disc_min=0, disc_max=100, fel_min=0, fel_max=100)
            for tok in re.split(r'[,;]\s*', '' if c[10] == '-' else c[10]):
                tok = tok.strip()
                for pat, key, conv in ((r'^Batalhas≥(\d+)$', 'batalhas', int),
                                       (r'^Vitórias≥(\d+(?:\.\d+)?)%?$', 'vitorias', float),
                                       (r'^Disc≥(\d+)$', 'disc_min', int), (r'^Disc≤(\d+)$', 'disc_max', int),
                                       (r'^Felic≥(\d+)$', 'fel_min', int), (r'^Felic≤(\d+)$', 'fel_max', int)):
                    mm = re.match(pat, tok)
                    if mm:
                        e[key] = conv(mm.group(1))
            E.append(e)
    return D, E


def apply_csv(E, path):
    idx = {(e['src'], e['dst']): e for e in E}
    miss = 0
    with open(path, encoding='utf-8') as f:
        for r in csv.DictReader(f):
            e = idx.get((r['origem'], r['destino']))
            if not e:
                miss += 1
                continue
            e.update(lv=int(r['Lv']), age=int(r['Idade']), mult=float(r['xMult']),
                     batalhas=int(r['Batalhas']), vitorias=float(r['Vitorias%']),
                     disc_min=int(r['Disc_min']), disc_max=int(r['Disc_max']),
                     fel_min=int(r['Felic_min']), fel_max=int(r['Felic_max']),
                     **{s: int(r[s]) for s in STATS})
    return miss


def score(e):
    return (e['lv'] + e['age'] + sum(e[s] for s in STATS) + e['batalhas'] + int(e['vitorias'])
            + e['disc_min'] + (100 - e['disc_max']) + e['fel_min'] + (100 - e['fel_max']))


# ------------------------------------------------------------------ modelo
def entry(D, inc, name):
    st = dict(D[name]['base'])
    ins = inc[name]
    if not ins:
        return st, 1
    best = min(ins, key=lambda x: (x['lv'], sum(x[s] for s in STATS)))
    for s in STATS:
        st[s] = max(st[s], int(round(best[s] * best['mult'])))
    return st, best['lv']


def valid(e, st, lv, beh):
    if lv < e['lv'] or e['age'] >= SAFE.get(e['src_stage'], 999):
        return False
    if any(st[s] < e[s] for s in STATS):
        return False
    return (e['disc_min'] <= beh['disc'] <= e['disc_max'] and e['fel_min'] <= beh['fel'] <= e['fel_max']
            and e['batalhas'] <= beh['bat'] and e['vitorias'] <= beh['vit'])


def behaviour(T, n):
    disc, fel = min(100, 50 + 5 * n), min(100, 50 + 10 * n)
    if T['disc_max'] < 100: disc = T['disc_max']
    if T['fel_max'] < 100: fel = T['fel_max']
    return dict(disc=max(disc, T['disc_min']), fel=max(fel, T['fel_min']),
                bat=max(15, T['batalhas']), vit=max(60, T['vitorias']))


K = 1   # treinos acumulados entre duas checagens (batalha ou meia-noite)


def run(T, sibs, st0, lv0, need, pre, post, filler, extra):
    st, lv, n = dict(st0), lv0, 0

    def check():
        v = [o for o in sibs if valid(o, st, lv, behaviour(T, n))]
        if not v:
            return None
        top = max(o['score'] for o in v)
        win = [o for o in v if o['score'] == top]
        if len(win) == 1 and win[0] is T:
            return True, None
        if T in win:
            return False, 'sorteio com ' + '/'.join(o['dst'] for o in win if o is not T)
        return False, win[0]['dst']

    def train(s):
        nonlocal n
        st[s] += 2; st['HP'] += 12; n += 1

    for s in pre:
        for _ in range(math.ceil(need[s] / 2)):
            train(s)
    if pre:
        for _ in range(extra):
            train(filler)
        extra = 0
    while lv < T['lv']:
        lv += 1
        for s in STATS:
            st[s] += GROWTH[s]
        r = check()
        if r:
            return r
    for s in list(post) + [filler] * extra:
        reps = math.ceil(need[s] / 2) if s in post else 1
        for _ in range(reps):
            train(s)
            if n % K == 0:
                r = check()
                if r:
                    return r
    return check() or (False, 'nada válido')


def simulate(T, sibs, st0, lv0):
    at = {s: st0[s] + GROWTH[s] * (T['lv'] - lv0) for s in STATS}
    need = {s: max(0, T[s] - at[s]) for s in TRAIN}
    req = [s for s in TRAIN if need[s] > 0]
    n_stat = sum(math.ceil(need[s] / 2) for s in req)
    extra = max(0, math.ceil(max(0, T['HP'] - at['HP']) / 12) - n_stat)
    free = [s for s in TRAIN if all(o[s] == 0 for o in sibs if o is not T)]
    filler = free[0] if free else (max(req, key=lambda s: need[s]) if req else 'Vel')
    blk = defaultdict(int)
    for k in range(len(req) + 1):
        for pre in itertools.combinations(req, k):
            for order in itertools.permutations([s for s in req if s not in pre]):
                ok, who = run(T, sibs, st0, lv0, need, pre, order, filler, extra)
                if ok:
                    return True, None, n_stat + extra
                blk[who] += 1
    return False, max(blk, key=blk.get), n_stat + extra


# ------------------------------------------------------------------ relatório
def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('md')
    ap.add_argument('--csv')
    ap.add_argument('--json')
    ap.add_argument('--k', type=int, default=1,
                    help='treinos acumulados entre checagens (1 = checa após cada treino)')
    a = ap.parse_args()
    global K
    K = a.k
    D, E = parse_md(a.md)
    if a.csv:
        miss = apply_csv(E, a.csv)
        if miss:
            print(f'AVISO: {miss} linhas do CSV sem ligação correspondente no .md')
    for e in E:
        e['score'] = score(e)
    inc, out = defaultdict(list), defaultdict(list)
    for e in E:
        if e['tipo'] == 'normal':
            inc[e['dst']].append(e); out[e['src']].append(e)
    bloqueadas, padrao_ruim, empates, sem_rede = [], [], [], []
    n_alvo = 0
    for src, sibs in sorted(out.items()):
        if len(sibs) < 2:
            continue
        st0, lv0 = entry(D, inc, src)
        stg = D[src]['stage']
        for T in sibs:
            n_alvo += 1
            if T['age'] >= SAFE.get(stg, 999):
                beh = dict(disc=50, fel=50, bat=15, vit=60)
                for L in range(lv0, max(o['lv'] for o in sibs) + 1):
                    stL = {s: st0[s] + GROWTH[s] * (L - lv0) for s in STATS}
                    v = [o for o in sibs if o is not T and valid(o, stL, L, beh)]
                    if v:
                        padrao_ruim.append((src, T['dst'], v[0]['dst'])); break
                continue
            ok, who, tr = simulate(T, sibs, st0, lv0)
            if not ok:
                bloqueadas.append((T['src_stage'] + '->' + T['dst_stage'], src, T['dst'], T['lv'], who))
        sc = defaultdict(list)
        for e in sibs:
            sc[e['score']].append(e['dst'])
        empates += [(src, s, g) for s, g in sc.items() if len(g) > 1]
        if stg in SAFE and not any(e['age'] >= SAFE[stg] for e in sibs):
            sem_rede.append(src)
    print(f"Arquivo: {a.md}" + (f"  +  CSV: {a.csv}" if a.csv else '') + f"  |  treinos entre checagens: {K}")
    print(f"Ligações: {len(E)} | pais com 2+ saídas: {sum(1 for s in out.values() if len(s) > 1)} "
          f"| formas-alvo testadas: {n_alvo}")
    print(f"\nA. Inalcançáveis mirando nelas: {len(bloqueadas)}")
    for b in bloqueadas:
        print(f"   [{b[0]}] {b[1]} -> {b[2]} (Lv {b[3]}) — dispara antes: {b[4]}")
    print(f"\nB. Formas padrão atropeladas por irmã que sai só de subir de nível: {len(padrao_ruim)}")
    for p in padrao_ruim:
        print(f"   {p[0]} -> {p[1]}: {p[2]} fica válida sem treino")
    print(f"\nC. Empates de score entre irmãs: {len(empates)}")
    for p in empates:
        print(f"   {p[0]}: score {p[1]} -> {', '.join(p[2])}")
    print(f"\nD. Pais sem forma padrão (Idade >= teto-2): {len(sem_rede)}  (informativo: nesses, quem"
          f" não treina a assinatura de nenhuma irmã depende de cumprir uma delas)")
    if a.json:
        json.dump(dict(bloqueadas=bloqueadas, padrao_ruim=padrao_ruim, empates=empates, sem_rede=sem_rede),
                  open(a.json, 'w'), ensure_ascii=False, indent=1)


if __name__ == '__main__':
    main()
