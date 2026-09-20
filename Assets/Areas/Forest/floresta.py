"""
floresta.py — props do bioma de floresta (áreas exploráveis).

Estilo igual ao dos Digimon e dos props antigos: desenho em resolução nativa baixa,
contorno escuro de 1px nativo, ampliado 3x (bloco de 3x3 por pixel).
Luz de cima à esquerda. Sombra de contato elíptica semitransparente embutida.

Ancoragem de todos: centro da sombra de contato (base do objeto), informado em ANCHOR.
Requer: numpy, pillow, scipy.
"""
import math
import numpy as np
from PIL import Image

SCALE = 3
OUT = (30, 28, 24)
SHADOW = (14, 20, 12, 92)
LX, LY, LZ = -0.55, -0.70, 0.45           # direção da luz (y cresce para baixo)
_n = math.sqrt(LX * LX + LY * LY + LZ * LZ)
LX, LY, LZ = LX / _n, LY / _n, LZ / _n

# ---------------------------------------------------------------- paletas
LEAF = [(18, 50, 36), (30, 82, 50), (48, 118, 60), (84, 156, 70), (150, 198, 92)]
PINE = [(12, 40, 38), (20, 64, 52), (32, 92, 64), (58, 128, 78), (110, 168, 98)]
PALMF = [(24, 70, 40), (44, 108, 52), (76, 150, 62), (124, 188, 78), (180, 220, 110)]
BARK = [(44, 28, 22), (72, 46, 32), (104, 70, 46), (138, 98, 64), (170, 128, 86)]
PALMB = [(70, 50, 34), (106, 78, 50), (144, 110, 72), (180, 146, 100), (208, 180, 130)]
WOOD = [(96, 62, 38), (136, 94, 58), (176, 132, 84), (210, 170, 116), (232, 204, 150)]
GRASS = [(26, 72, 40), (40, 106, 50), (66, 144, 60), (112, 182, 76), (166, 214, 104)]
ROCK = [(66, 70, 82), (98, 102, 116), (132, 136, 150), (168, 172, 184), (206, 210, 220)]
MOSS = [(52, 96, 44), (78, 132, 56), (116, 168, 72)]
PETAL = {
    'blue':  [(40, 58, 140), (62, 96, 196), (104, 144, 236), (170, 200, 255)],
    'red':   [(128, 30, 44), (190, 54, 64), (236, 100, 100), (255, 170, 160)],
    'white': [(150, 150, 168), (204, 204, 214), (238, 238, 240), (255, 255, 255)],
}
CORE = [(168, 104, 20), (232, 172, 40), (255, 226, 110)]
CAP = [(110, 24, 26), (168, 40, 40), (216, 70, 60), (246, 128, 108)]
STEM = [(150, 134, 116), (200, 186, 166), (236, 226, 208), (255, 250, 238)]
BERRY = [(96, 20, 48), (170, 40, 72), (240, 110, 130)]


def h01(x, y, s=0):
    """Ruído determinístico em [0,1) por pixel — textura estável entre execuções."""
    v = (x * 73856093) ^ (y * 19349663) ^ (s * 83492791)
    v = (v ^ (v >> 13)) * 1274126177
    return ((v ^ (v >> 16)) & 0xFFFF) / 65536.0


# ---------------------------------------------------------------- canvas nativo
class Spr:
    def __init__(self, w, h):
        self.w, self.h = w, h
        self.c = np.zeros((h, w, 3), np.uint8)
        self.m = np.zeros((h, w), bool)
        self.own = -np.ones((h, w), int)       # qual "peça" é dona do pixel
        self.tone = -np.ones((h, w), int)

    def px(self, x, y, col, own=-2, tone=-1):
        x, y = int(x), int(y)
        if 0 <= x < self.w and 0 <= y < self.h:
            self.c[y, x] = col
            self.m[y, x] = True
            if own != -2:
                self.own[y, x] = own
            self.tone[y, x] = tone

    def finish(self, anchor, shadow_r, name):
        """Contorno externo de 1px, sombra de contato, ampliação 3x."""
        m = self.m
        d = np.zeros_like(m)
        d[1:, :] |= m[:-1, :]; d[:-1, :] |= m[1:, :]
        d[:, 1:] |= m[:, :-1]; d[:, :-1] |= m[:, 1:]
        edge = d & ~m
        rgba = np.zeros((self.h, self.w, 4), np.uint8)
        # sombra primeiro, só onde não há sprite
        ax, ay = anchor
        rx, ry = shadow_r
        yy, xx = np.mgrid[0:self.h, 0:self.w]
        t = ((xx + 0.5 - ax) / rx) ** 2 + ((yy + 0.5 - ay) / ry) ** 2
        rgba[t <= 1.0] = SHADOW
        rgba[m, :3] = self.c[m]; rgba[m, 3] = 255
        rgba[edge] = (*OUT, 255)
        img = Image.fromarray(rgba, 'RGBA')
        img = img.resize((self.w * SCALE, self.h * SCALE), Image.NEAREST)
        ANCHOR[name] = (int(ax * SCALE), int(ay * SCALE))
        return img


ANCHOR = {}


def lambert(nx, ny):
    nz = math.sqrt(max(0.0, 1 - nx * nx - ny * ny))
    return LX * nx + LY * ny + LZ * nz


# ---------------------------------------------------------------- folhagem em tufos
def foliage(s, blobs, pal, box, tex=0.16, seed=1, k_glob=0.32, bias=0.0):
    """Copa feita de tufos (círculos) sobrepostos, do fundo para a frente.
    Cada tufo tem sombreamento esférico próprio + um termo global da copa inteira;
    a borda inferior de cada tufo projeta 1px de sombra no tufo de trás.
    """
    x0, y0, x1, y1 = box
    gcx, gcy = (x0 + x1) / 2, (y0 + y1) / 2
    grx, gry = (x1 - x0) / 2, (y1 - y0) / 2
    for i, (cx, cy, r) in enumerate(blobs):
        for y in range(int(cy - r) - 1, int(cy + r) + 2):
            for x in range(int(cx - r) - 1, int(cx + r) + 2):
                dx, dy = (x + 0.5 - cx) / r, (y + 0.5 - cy) / r
                # tufos levemente irregulares
                wob = 1.0 + 0.10 * math.sin(math.atan2(dy, dx) * 5 + i * 1.7)
                if dx * dx + dy * dy > wob * wob:
                    continue
                if not (0 <= x < s.w and 0 <= y < s.h):
                    continue
                l = lambert(dx / wob * 0.95, dy / wob * 0.95)
                g = -(0.55 * (x - gcx) / grx + 0.85 * (y - gcy) / gry)
                v = (1 - k_glob) * l + k_glob * g + bias
                t = 0 if v < -0.05 else 1 if v < 0.22 else 2 if v < 0.50 else 3
                s.own[y, x] = i
                s.tone[y, x] = t
                s.m[y, x] = True
    # sombra do tufo da frente sobre o de trás (1px abaixo/direita da borda)
    own = s.own.copy()
    tone = s.tone.copy()
    for y in range(s.h):
        for x in range(s.w):
            o = own[y, x]
            if o < 0:
                continue
            for ddx, ddy in ((0, -1), (-1, -1)):
                xx, yy = x + ddx, y + ddy
                if 0 <= xx < s.w and 0 <= yy < s.h and own[yy, xx] > o:
                    tone[y, x] = max(0, tone[y, x] - 1)
                    break
    # textura de folhas: pontinhos claros nas áreas iluminadas, escuros nas sombras
    for y in range(s.h):
        for x in range(s.w):
            t = tone[y, x]
            if own[y, x] < 0 or t < 0:
                continue
            r = h01(x, y, seed)
            if t == 3 and r < tex:
                t = 4
            elif t in (1, 2) and r < tex * 0.7:
                t -= 1
            elif t == 2 and r > 1 - tex * 0.5:
                t = 3
            tone[y, x] = t
            s.c[y, x] = pal[t]
    s.tone = tone


def trunk(s, cx, ytop, ybot, wtop, wbot, pal, flare=2, seed=3):
    """Tronco cilíndrico com raízes, fendas verticais de casca, luz à esquerda."""
    for y in range(ytop, ybot + 1):
        f = (y - ytop) / max(1, ybot - ytop)
        w = wtop + (wbot - wtop) * f
        if y >= ybot - 2:
            w += flare * (1 - (ybot - y) / 3)          # raízes abrindo
        hw = w / 2
        for x in range(int(math.floor(cx - hw)), int(math.ceil(cx + hw))):
            u = (x + 0.5 - cx) / hw
            if abs(u) > 1:
                continue
            t = 3 if u < -0.45 else 2 if u < 0.05 else 1 if u < 0.6 else 0
            # fendas da casca: colunas escuras em traços verticais
            if -0.5 < u < 0.7 and h01(x, y // 3, seed) < 0.22:
                t = max(0, t - 1)
            s.px(x, y, pal[t], tone=t)
    # raízes separadas no chão
    for x in (int(cx - wbot / 2 - flare), int(cx + wbot / 2 + flare - 1)):
        s.px(x, ybot, pal[1 if x > cx else 2])


def shade_band(s, x0, x1, y, depth, amount=2):
    """Escurece a faixa logo abaixo da copa (sombra da copa sobre o tronco)."""
    for x in range(x0, x1 + 1):
        for j in range(depth):
            yy = y + j
            if 0 <= yy < s.h and s.m[yy, x] and s.own[yy, x] < 0:
                s.c[yy, x] = (s.c[yy, x] * (0.55 + 0.15 * j)).astype(np.uint8)


# ---------------------------------------------------------------- árvores
def oak():
    s = Spr(40, 50)
    trunk(s, 20, 30, 45, 6, 8, BARK, flare=3)
    shade_band(s, 14, 26, 30, 3)
    blobs = [(12, 20, 8), (28, 20, 8), (20, 13, 9), (8, 27, 6.5), (32, 27, 6.5),
             (14, 28, 7.5), (26, 28, 7.5), (20, 22, 8.5), (13, 12, 6), (27, 12, 6),
             (20, 6, 6), (20, 30, 6)]
    foliage(s, blobs, LEAF, (2, 0, 38, 36), seed=11)
    # galho aparecendo entre os tufos
    for (x, y) in ((16, 33), (17, 32), (24, 33), (23, 32)):
        s.px(x, y, BARK[1])
    return s.finish((20.5, 46), (11, 3), 'forest_tree_oak')


def pine():
    s = Spr(38, 56)
    cx = 19
    trunk(s, cx, 42, 51, 4, 6, BARK, flare=2, seed=5)
    # camadas em trapézio: cada uma nasce estreita sob a de cima e abre até a ponta
    # dos galhos, criando o contorno em zigue-zague
    tiers = [(29, 44, 4.0, 17.5), (20, 35, 3.5, 14.5), (12, 26, 2.5, 11.0),
             (5, 17, 1.5, 7.5), (0, 9, 0.5, 4.5)]
    for i, (yt, yb, wt, wb) in enumerate(tiers):
        for y in range(max(0, yt), yb + 3):
            f = min(1.0, max(0.0, (y + 0.5 - yt) / (yb - yt)))
            half = wt + (wb - wt) * f ** 1.35
            for x in range(s.w):
                dx = x + 0.5 - cx
                a = abs(dx)
                if a > half:
                    continue
                # pontas de galho caindo na borda de baixo, mais fundas nas laterais
                tip = 1.0 + 1.6 * (a / wb) if (int(a) % 3 == 1) else 0.0
                if y > yb + tip - (1 if a < wb * 0.35 else 0):
                    continue
                u = dx / max(half, 1)
                v = -0.8 * u + 0.45 * (1 - f) - 0.15
                t = 0 if v < -0.5 else 1 if v < -0.1 else 2 if v < 0.3 else 3
                s.own[y, x] = i
                s.tone[y, x] = t
                s.m[y, x] = True
    own = s.own.copy()
    for y in range(s.h):
        for x in range(s.w):
            o = own[y, x]
            if o < 0:
                continue
            t = s.tone[y, x]
            for k in (1, 2, 3):
                if y - k >= 0 and own[y - k, x] > o:
                    t = max(0, t - (2 if k == 1 else 1))
                    break
            r = h01(x, y, 21)
            if t == 3 and r < 0.2:
                t = 4
            elif t in (1, 2) and r < 0.12:
                t -= 1
            s.c[y, x] = PINE[t]
    shade_band(s, 15, 23, 45, 2)
    return s.finish((19.5, 52), (9, 3), 'forest_tree_pine')

def palm():
    s = Spr(46, 54)
    # tronco curvo em anéis, afinando para cima
    pts = []
    N = 30
    for k in range(N):
        f = k / (N - 1)
        pts.append((22 + 3.5 * math.sin(f * 2.0 + 0.2) - 1, 49 - k * 1.0))
    for k, (cx, cy) in enumerate(pts):
        hw = 2.7 - 0.9 * (k / N)
        for x in range(int(cx - hw) - 1, int(cx + hw) + 2):
            u = (x + 0.5 - cx) / hw
            if abs(u) > 1:
                continue
            t = 3 if u < -0.35 else 2 if u < 0.35 else 1
            if k % 3 == 0:
                t = max(0, t - 1)          # anel
            if k % 3 == 1 and u < -0.3:
                t = 4                      # brilho logo acima do anel
            s.px(x, int(cy), PALMB[t], tone=t)
    tx, ty = pts[-1]
    ty -= 2
    # folhas (de trás para frente): ângulo, comprimento, queda
    fronds = [(-150, 15, 0.9, 1), (-30, 15, 0.9, 1), (-100, 10, 0.3, 1),
              (180, 18, 1.0, 0), (0, 18, 1.0, 0), (-160, 16, 0.7, 0), (-20, 16, 0.7, 0),
              (150, 13, 1.1, 0), (30, 13, 1.1, 0)]
    for fi, (ang, L, droop, back) in enumerate(fronds):
        a = math.radians(ang)
        ca, sa = math.cos(a), math.sin(a) * 0.62
        steps = int(L * 4)
        for k in range(steps):
            f = k / steps
            d = L * f
            x = tx + ca * d
            y = ty + sa * d + droop * (d * d) / L * 0.55
            w = 2.8 * math.sin(math.pi * min(1, f * 1.15 + 0.08)) + 0.4
            for j in range(-int(w * 2) - 1, int(w * 2) + 2):
                off = j * 0.5
                if abs(off) > w:
                    continue
                side = off / max(w, 0.5)             # -1 face de cima, +1 de baixo
                # folíolos: dentes nas duas bordas, mais fundos na de baixo
                if abs(side) > 0.45 and ((k // 2) % 2 == 1):
                    continue
                t = 3 if side < -0.25 else 2 if side < 0.3 else 1
                if abs(side) < 0.12:
                    t = 1                             # nervura central
                if back:
                    t = max(0, t - 1)
                if t == 3 and f > 0.2 and h01(int(x), int(y + off), 40 + fi) < 0.3:
                    t = 4
                s.px(x, y + off, PALMF[t], own=fi, tone=t)
    # cocos sob a copa
    for (dx, dy) in ((-2.2, 1.5), (1.4, 1.5), (-0.3, 3.2)):
        cx, cy = tx + dx, ty + dy
        for y in range(int(cy) - 2, int(cy) + 2):
            for x in range(int(cx) - 2, int(cx) + 2):
                u, v = x + 0.5 - cx, y + 0.5 - cy
                if u * u + v * v <= 2.4:
                    t = 3 if (u + v) < -0.9 else 2 if (u + v) < 0.6 else 1
                    s.px(x, y, BARK[t])
    return s.finish((21, 50), (8, 2.5), 'forest_tree_palm')

# ---------------------------------------------------------------- vegetação baixa
def bush(berries=False):
    s = Spr(20, 15)
    blobs = [(6, 8, 4.5), (14, 8, 4.5), (10, 5, 5), (5, 10, 3.8), (15, 10, 3.8), (10, 9, 4.5)]
    foliage(s, blobs, LEAF, (1, 0, 19, 14), tex=0.18, seed=7, k_glob=0.4)
    if berries:
        for (x, y) in ((6, 6), (13, 5), (9, 10), (15, 9), (4, 10), (11, 7)):
            s.px(x, y, BERRY[1]); s.px(x, y + 1, BERRY[0]); s.px(x - 1, y, BERRY[1])
            s.px(x - 1, y - 0, BERRY[2]) if False else s.px(x - 1, y, BERRY[2])
    name = 'forest_bush_berry' if berries else 'forest_bush'
    return s.finish((10, 13), (8, 2), name)


def grass(big=True):
    s = Spr(20 if big else 15, 16 if big else 13)
    base = s.h - 3
    blades = ([(2, 7, -1.5), (4, 11, -0.8), (6, 8, 0.2), (8, 13, -0.3), (10, 9, 1.0),
               (11, 12, 0.4), (13, 10, 1.3), (15, 8, 0.9), (17, 6, 2.0), (6, 12, -1.6)]
              if big else
              [(2, 6, -1.2), (4, 9, -0.5), (6, 7, 0.3), (7, 10, 0.1), (9, 8, 0.9), (11, 6, 1.4)])
    for bi, (bx, L, lean) in enumerate(blades):
        for k in range(L):
            f = k / L
            x = bx + lean * f * f * 2.2
            y = base - k
            w = 1.6 * (1 - f) + 0.5
            for dx in range(-1, 2):
                if abs(dx) > w - 0.3:
                    continue
                t = 3 if dx < 0 else 2
                t = min(3, t + (1 if f > 0.6 else 0)) if dx <= 0 else t
                if f < 0.25:
                    t = max(0, t - 2)          # base fica na sombra do tufo
                elif f < 0.5:
                    t = max(0, t - 1)
                if f > 0.8 and dx <= 0 and h01(bi, k, 3) < 0.5:
                    t = 4
                s.px(x + dx, y, GRASS[t])
    name = 'grass_big' if big else 'grass_small'
    return s.finish((s.w / 2, base + 1), (s.w / 2 - 2, 1.8), name)


def flower(kind):
    P = PETAL[kind]
    s = Spr(14, 18)
    cx, cy = 7, 5.5
    # caule e folhinha
    for y in range(8, 16):
        x = 7 + (0 if y > 11 else (1 if y < 10 else 0))
        s.px(x - 1, y, GRASS[2]); s.px(x, y, GRASS[1])
    for (x, y, t) in ((8, 12, 3), (9, 12, 2), (10, 11, 2), (9, 13, 1), (10, 12, 1), (5, 14, 2), (4, 13, 3), (5, 13, 1)):
        s.px(x, y, GRASS[t])
    # cinco pétalas arredondadas
    for pi in range(5):
        a = math.radians(-90 + pi * 72)
        px_, py_ = cx + math.cos(a) * 3.1, cy + math.sin(a) * 2.8
        for y in range(int(py_) - 3, int(py_) + 3):
            for x in range(int(px_) - 3, int(px_) + 3):
                u, v = x + 0.5 - px_, y + 0.5 - py_
                if u * u + v * v > 5.0:
                    continue
                l = -(u * 0.55 + v * 0.8) / 2.2
                g = -((x + 0.5 - cx) * 0.4 + (y + 0.5 - cy) * 0.6) / 4
                w_ = l * 0.5 + g * 0.5
                t = 0 if w_ < -0.35 else 1 if w_ < 0.0 else 2 if w_ < 0.35 else 3
                s.px(x, y, P[t])
    # nervura: linha escura do centro para fora nas pétalas de baixo
    for pi in (2, 3):
        a = math.radians(-90 + pi * 72)
        s.px(cx + math.cos(a) * 2.3 - 0.5, cy + math.sin(a) * 2.1 - 0.5, P[0])
    # miolo
    for (x, y, t) in ((6, 5, 2), (7, 5, 1), (6, 6, 1), (7, 6, 0), (6, 4, 1), (5, 5, 1)):
        s.px(x, y, CORE[t])
    s.px(6, 5, CORE[2])
    return s.finish((7, 16), (3.5, 1.3), f'flower_{kind}')


def mushroom():
    s = Spr(18, 16)

    def one(cx, cyc, rx, ry, sh, seed):
        # caule
        for y in range(int(cyc), int(cyc + sh)):
            for x in range(int(cx - 2), int(cx + 2)):
                u = (x + 0.5 - cx) / 2
                t = 3 if u < -0.4 else 2 if u < 0.3 else 1
                if y < cyc + 2:
                    t = 0                     # sombra do chapéu
                s.px(x, y, STEM[t])
        # lamelas (parte de baixo do chapéu)
        for x in range(int(cx - rx + 1), int(cx + rx)):
            s.px(x, cyc, STEM[1] if (x % 2) else STEM[0])
        # chapéu em domo
        for y in range(int(cyc - ry * 2) - 1, int(cyc)):
            for x in range(int(cx - rx) - 1, int(cx + rx) + 1):
                u = (x + 0.5 - cx) / rx
                v = (y + 0.5 - cyc) / (ry * 2)
                if u * u + v * v > 1 or v > 0:
                    continue
                l = lambert(u * 0.9, (v + 0.5) * 0.9)
                t = 0 if l < 0.05 else 1 if l < 0.4 else 2 if l < 0.7 else 3
                s.px(x, y, CAP[t])
        # pintas
        for (dx, dy) in ((-2.5, -2.5), (1.5, -3.8), (2.8, -1.4), (-0.5, -1.2)):
            x, y = int(cx + dx * rx / 5), int(cyc + dy * ry / 2.5)
            if s.m[y, x]:
                s.px(x, y, STEM[2]); s.px(x + 1, y, STEM[1])
        s.px(int(cx - rx * 0.45), int(cyc - ry * 1.5), (255, 200, 180))

    one(6, 7, 5, 2.6, 7, 1)
    one(13.5, 10, 3.2, 1.7, 4, 2)
    return s.finish((9, 14), (7, 1.8), 'forest_mushroom')


def rock(mossy=True):
    s = Spr(20, 14)
    # pedra facetada: topo (encurtado) claro, faces laterais mais escuras
    for y in range(s.h):
        for x in range(s.w):
            u, v = (x + 0.5 - 10) / 8.6, (y + 0.5 - 8) / 5.2
            r = u * u + v * v
            wob = 1 + 0.08 * math.sin(math.atan2(v, u) * 3 + 0.5)
            if r > wob * wob or y > 12:
                continue
            # facetas planas: quantiza a normal em poucos planos
            if v < -0.25 and abs(u) < 0.75:
                t = 3                             # topo
            elif u < -0.35:
                t = 2 if v < 0.3 else 1           # face esquerda
            elif u > 0.4:
                t = 1 if v < 0.2 else 0           # face direita
            else:
                t = 2 if v < 0.35 else 1          # face frontal
            if t >= 2 and h01(x, y, 9) < 0.1:
                t -= 1                            # poros
            s.px(x, y, ROCK[t], tone=t)
    # aresta viva entre topo e frente
    for x in range(5, 16):
        y = 6
        if s.m[y, x]:
            s.px(x, y, ROCK[4] if x < 11 else ROCK[3])
    # rachadura
    for (x, y) in ((12, 8), (12, 9), (13, 10), (13, 11)):
        s.px(x, y, ROCK[0])
    if mossy:
        for y in range(s.h):
            for x in range(s.w):
                if s.m[y, x] and y <= 6 and 3 <= x <= 12 and h01(x, y, 5) < 0.75 \
                        and (x - 7) ** 2 / 20 + (y - 4) ** 2 / 4 < 1:
                    s.px(x, y, MOSS[2] if y < 4 else MOSS[1])
        for (x, y) in ((3, 8), (4, 7), (2, 9)):
            s.px(x, y, MOSS[0])
    return s.finish((10, 12), (9, 2), 'forest_rock')


def stump():
    s = Spr(18, 15)
    cx = 9
    # corpo
    for y in range(5, 13):
        hw = 5.5 + (1.5 if y >= 11 else 0)
        for x in range(int(cx - hw), int(cx + hw) + 1):
            u = (x + 0.5 - cx) / hw
            if abs(u) > 1:
                continue
            t = 3 if u < -0.45 else 2 if u < 0.1 else 1 if u < 0.6 else 0
            if h01(x, y // 2, 4) < 0.2:
                t = max(0, t - 1)
            s.px(x, y, BARK[t])
    # topo cortado com anéis (elipse encurtada)
    for y in range(1, 8):
        for x in range(cx - 6, cx + 7):
            u, v = (x + 0.5 - cx) / 5.6, (y + 0.5 - 4.5) / 3.4
            r = math.sqrt(u * u + v * v)
            if r > 1:
                continue
            if r > 0.82:
                t = 1 if (u + v) > 0 else 2
                s.px(x, y, BARK[t + 1] if (u + v) < 0 else BARK[1])
                continue
            ringi = int(r * 5)
            t = 3 if ringi % 2 == 0 else 2
            if (u + v * 1.4) > 0.5:
                t -= 1
            s.px(x, y, WOOD[t])
    s.px(cx - 1, 4, WOOD[4]); s.px(cx - 2, 3, WOOD[4])
    # musgo na base e um cogumelinho
    for x in range(3, 9):
        if h01(x, 12, 2) < 0.7:
            s.px(x, 12, MOSS[1]); s.px(x, 11, MOSS[2]) if h01(x, 11, 3) < 0.4 else None
    for (x, y, c) in ((14, 11, CAP[2]), (15, 11, CAP[1]), (14, 12, STEM[2])):
        s.px(x, y, c)
    return s.finish((9, 13), (8, 2), 'forest_stump')


def log():
    s = Spr(30, 13)
    # tronco caído, eixo horizontal: topo claro, base escura
    for y in range(2, 11):
        for x in range(3, 26):
            v = (y + 0.5 - 6.5) / 4.5
            if abs(v) > 1:
                continue
            t = 3 if v < -0.45 else 2 if v < 0.1 else 1 if v < 0.6 else 0
            if h01(x // 3, y, 6) < 0.25:
                t = max(0, t - 1)
            s.px(x, y, BARK[t])
    # ponta cortada (elipse de frente, à direita)
    for y in range(2, 11):
        for x in range(23, 30):
            u, v = (x + 0.5 - 26) / 2.8, (y + 0.5 - 6.5) / 4.5
            r = math.sqrt(u * u + v * v)
            if r > 1:
                continue
            t = 3 if int(r * 4) % 2 == 0 else 2
            if r > 0.8:
                t = 1
            if (u + v) > 0.7:
                t = max(0, t - 1)
            s.px(x, y, WOOD[t])
    s.px(25, 5, WOOD[4])
    # galhinho quebrado e musgo por cima
    for (x, y) in ((9, 1), (9, 0), (10, 0)):
        s.px(x, y, BARK[2])
    for x in range(5, 18):
        if h01(x, 2, 8) < 0.6:
            s.px(x, 2, MOSS[2]); s.px(x, 3, MOSS[1]) if h01(x, 3, 8) < 0.5 else None
    return s.finish((15, 11), (13, 2), 'forest_log')


def fern():
    s = Spr(28, 20)
    bx, by = 14, 18
    fronds = [(-160, 12, 1), (-20, 12, 1), (-122, 13, 0), (-58, 13, 0), (-90, 11, 1)]
    for fi, (ang, L, back) in enumerate(fronds):
        a = math.radians(ang)
        steps = int(L * 3)
        pts = []
        for k in range(steps):
            f = k / steps
            d = L * f
            pts.append((bx + math.cos(a) * d, by + math.sin(a) * d + d * d * 0.028 * abs(math.cos(a)) + d * d * 0.012, f))
        for (x, y, f) in pts:
            s.px(x, y, GRASS[0 if back else 1])
        # folíolos alternados, espaçados para deixar vãos entre eles
        for k in range(4, steps, 4):
            x, y, f = pts[k]
            wl = 2.0 * (1 - f) + 0.8
            for side in (-1, 1):
                nx_, ny_ = -math.sin(a) * side, math.cos(a) * side * 0.85
                for q in range(1, int(round(wl)) + 1):
                    xx, yy = x + nx_ * q, y + ny_ * q - q * 0.45
                    up = (ny_ - 0.45) < 0
                    tt = 3 if up else 2
                    if back:
                        tt -= 1
                    if tt == 3 and q == 1 and h01(int(xx), int(yy), fi) < 0.4:
                        tt = 4
                    s.px(xx, yy, GRASS[max(0, tt)])
    return s.finish((14, 18.5), (8, 1.8), 'forest_fern')

# ---------------------------------------------------------------- execução
if __name__ == '__main__':
    import os, sys
    out = sys.argv[1] if len(sys.argv) > 1 else '.'
    os.makedirs(out, exist_ok=True)
    sprites = [
        oak(), pine(), palm(), bush(), bush(True), grass(True), grass(False),
        flower('blue'), flower('red'), flower('white'), mushroom(), rock(),
        stump(), log(), fern(),
    ]
    names = list(ANCHOR.keys())
    for n, im in zip(names, sprites):
        im.save(os.path.join(out, n + '.png'))
    for n, im in zip(names, sprites):
        print(f'{n:22s} {im.width:4d}x{im.height:<4d} ancora {ANCHOR[n]}')
