"""Monta a caverna usando SÓ o mapa_caverna.json e os arquivos do kit (as mesmas regras que o Godot vai seguir).

Uso:
  python montar_previa.py antes|depois                       -> ferramentas/previa_<estado>.png (mapa inteiro, sem escuro)
  python montar_previa.py antes|depois zoom GX GY            -> recorte em 2x ao redor do bloco (GX, GY), sem o escuro
  python montar_previa.py antes|depois escuro GX GY ESTADO   -> previa com o escuro, o digimon no bloco (GX, GY);
                                                                 ESTADO = sem_bonus | fogo | lanterna | fogo_lanterna
Serve para conferir o JSON e comparar com um print do jogo. Requer Python 3 + Pillow + numpy.
'antes' = nenhuma quest feita; 'depois' = todas as quests feitas, lanternas acesas e caixas ligadas.
"""
import json, os, sys
import numpy as np
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__))) + "/"
mapa = json.load(open(ROOT + "mapa/mapa_caverna.json", encoding="utf-8"))
man = json.load(open(ROOT + "kit/manifest.json", encoding="utf-8"))
STATE = sys.argv[1] if len(sys.argv) > 1 else "antes"
DARK = len(sys.argv) > 2 and sys.argv[2] == "escuro"
ZOOM = len(sys.argv) > 2 and sys.argv[2] == "zoom"
DONE = STATE == "depois"
_img = {}
def load(path):
    if path not in _img: _img[path] = Image.open(ROOT + "kit/" + path).convert("RGBA")
    return _img[path]
def find(name):
    for sec in ("tiles", "props", "overlays"):
        if name in man[sec]: return man[sec][name]
    raise KeyError(name)
def frame(e, f=0, key="sheet"):
    if key not in e: return None
    sh = load(e[key]); n = e.get("frames", 1); w = sh.width // n
    return sh.crop(((f % n) * w, 0, (f % n) * w + w, sh.height))
def img_of(e, f=0):
    if "sheet" in e: return frame(e, f)
    return load(e["file"])

xs = [t["pos"][0] for t in mapa["tiles"]]; ys = [t["pos"][1] for t in mapa["tiles"]]
OX, OY = -min(xs) + 120, -min(ys) + 200
W, H = max(xs) + OX + 120, max(ys) + OY + 160
alb = Image.new("RGBA", (W, H), (0, 0, 0, 0))
glw = Image.new("RGBA", (W, H), (0, 0, 0, 0))            # glow layer: glowing pixels in colour, everything else opaque black
def put(e, x, y, f=0):
    im = img_of(e, f); alb.alpha_composite(im, (int(round(x)), int(round(y))))
    if DARK:
        sil = Image.new("RGBA", im.size, (0, 0, 0, 255)); sil.putalpha(im.getchannel("A"))
        g = frame(e, f, "glow") if "glow" in e else None
        if g is not None: sil.alpha_composite(g)
        glw.alpha_composite(sil, (int(round(x)), int(round(y))))

objs = list(mapa["objects"])
for b in mapa["blockers"]: objs += b["objects_after_clear" if DONE else "objects_while_blocked"]
def sprite_of(o): return o.get("sprite_after", o["sprite"]) if (DONE and o.get("kind") == "switchable") else o["sprite"]
# 1) chão: blocos no nível 0, na ordem do JSON, com as sobreposições (âncora própria)
sorted_items = []
for t in mapa["tiles"]:
    e = find(t["sprite"]); cx, cy = e["tile_center"]
    if t["layer"] == "ground":
        put(e, OX + t["pos"][0] - cx, OY + t["pos"][1] - cy)
        for o in t["overlays"]:
            oe = find(o); ax, ay = oe["anchor"]; put(oe, OX + t["pos"][0] - ax, OY + t["pos"][1] - ay)
    else:
        sorted_items.append((t["sort_y"], 0, len(sorted_items), t))
# 2) decalques (poças, cabos, pedrinhas, ponte de dados) por cima do chão
for o in objs:
    if o["layer"] == "decal":
        e = find(sprite_of(o)); ax, ay = e["anchor"]; put(e, OX + o["pos"][0] - ax, OY + o["pos"][1] - ay)
# 3) camada ordenada por sort_y: blocos altos, escadas e objetos (empate: bloco antes, depois a ordem do JSON)
for o in objs:
    if o["layer"] != "decal": sorted_items.append((o["sort_y"], 1, len(sorted_items), o))
sorted_items.sort(key=lambda s: (s[0], s[1], s[2]))
for (_, kind, _, it) in sorted_items:
    if kind == 0:
        e = find(it["sprite"]); cx, cy = e["tile_center"]
        put(e, OX + it["pos"][0] - cx, OY + it["pos"][1] - cy)
        for o in it["overlays"]:
            oe = find(o); ax, ay = oe["anchor"]; put(oe, OX + it["pos"][0] - ax, OY + it["pos"][1] - ay)
    else:
        e = find(sprite_of(it)); ax, ay = e["anchor"]; put(e, OX + it["pos"][0] - ax, OY + it["pos"][1] - ay)

out = Image.new("RGBA", (W, H), (12, 10, 18, 255)); out.alpha_composite(alb)
if ZOOM:                                                     # python montar_previa.py antes zoom GX GY -> recorte 2x sem o escuro
    gx, gy = float(sys.argv[3]), float(sys.argv[4]); px_, py_ = (gx - gy) * 32 + OX, (gx + gy) * 16 + OY
    X0, Y0 = int(px_ - 480), int(py_ - 290)
    out.crop((X0, Y0, X0 + 960, Y0 + 540)).resize((1920, 1080), Image.NEAREST).save(ROOT + f"ferramentas/zoom_{STATE}_{int(gx)}_{int(gy)}.png"); print("ok zoom"); sys.exit()
if not DARK:
    out = out.crop(alb.getbbox()); out.save(ROOT + f"ferramentas/previa_{STATE}.png"); print("ok", out.size); sys.exit()

# ------------------------------------------------------------------ o escuro (CanvasModulate + PointLight2D) e o brilho
gx, gy, vstate = float(sys.argv[3]), float(sys.argv[4]), sys.argv[5]
V = mapa["vision"]
TILE_R = 45.25
def add_light(acc, X0, Y0, lx, ly, r, col, en):
    rx = r * TILE_R; ry = rx / 2; h, w = acc.shape[:2]
    x0, x1 = max(0, int(lx - rx * 1.12 - X0)), min(w, int(lx + rx * 1.12 - X0) + 1)
    y0, y1 = max(0, int(ly - ry * 1.12 - Y0)), min(h, int(ly + ry * 1.12 - Y0) + 1)
    if x0 >= x1 or y0 >= y1: return
    yy, xx = np.mgrid[y0:y1, x0:x1]
    d = np.sqrt(((xx + X0 + .5 - lx) / rx) ** 2 + ((yy + Y0 + .5 - ly) / ry) ** 2); chk = ((xx + X0 + yy + Y0) % 2 == 0)
    v = np.where(d < .55, 1.0, np.where(d < .78, .7, np.where(d < .92, .42, np.where(d < 1.0, np.where(chk, .42, .2), np.where(d < 1.1, np.where(chk, .2, 0.0), 0.0)))))
    acc[y0:y1, x0:x1] += v[..., None] * (np.array(col, np.float32) * en)
px_, py_ = (gx - gy) * 32 + OX, (gx + gy) * 16 + OY
VW, VH = 960, 540
X0 = int(max(0, min(W - VW, px_ - VW / 2))); Y0 = int(max(0, min(H - VH, py_ - 20 - VH / 2)))
acc = np.zeros((VH, VW, 3), np.float32); acc[:] = V["ambient_color"]
for L in mapa["lights"]:
    en = L["enabled"]
    if en != "always" and not DONE: continue
    add_light(acc, X0, Y0, L["pos"][0] + OX, L["pos"][1] + OY, L["radius_tiles"], L["color"], L["energy"])
pl = V["player_light"]["states"][vstate]
add_light(acc, X0, Y0, px_, py_ - 8, pl["radius_tiles"], pl["color"], pl["energy"])
acc = np.minimum(acc, 1.3)
a = np.asarray(out.crop((X0, Y0, X0 + VW, Y0 + VH)).convert("RGB")).astype(np.float32)
gl = np.asarray(glw.crop((X0, Y0, X0 + VW, Y0 + VH))).astype(np.float32)
em = (gl[..., 3] > 0) & (gl[..., :3].sum(-1) > 0)
lit = np.where(em[..., None], np.maximum(acc, 1.0), acc)
res = Image.fromarray(np.clip(a * lit, 0, 255).astype(np.uint8)).resize((VW * 2, VH * 2), Image.NEAREST)
res.save(ROOT + f"ferramentas/previa_{STATE}_escuro_{int(gx)}_{int(gy)}_{vstate}.png"); print("ok escuro", res.size)
