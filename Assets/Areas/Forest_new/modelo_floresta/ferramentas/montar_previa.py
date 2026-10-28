"""Monta uma previa do mapa usando SO o mapa_floresta.json e os arquivos do kit (o mesmo que o Godot fara).
Uso: python montar_previa.py [blocked|cleared]  -> gera ferramentas/previa_<estado>.png
Serve para conferir o JSON e comparar com um print do jogo. Requer Python 3 + Pillow."""
import json, sys
from PIL import Image, ImageChops

import os
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__))) + "/"
SP = ROOT + "ferramentas/"
mapa = json.load(open(ROOT + "mapa/mapa_floresta.json"))
man = json.load(open(ROOT + "kit/manifest.json"))
STATE = sys.argv[1] if len(sys.argv) > 1 else "blocked"
_img = {}
def load(path):
    if path not in _img: _img[path] = Image.open(ROOT + "kit/" + path).convert("RGBA")
    return _img[path]
def frame_img(entry, f):
    if isinstance(entry, str): return load(entry)
    if "sheet" in entry:
        sh = load(entry["sheet"]); n = entry.get("frames", 1); w = sh.width // n
        return sh.crop(((f % n) * w, 0, (f % n) * w + w, sh.height))
    return load(entry["file"])
def find(name):
    for sec in ("tiles", "terrain", "props", "overlays"):
        if name in man.get(sec, {}): return sec, man[sec][name]
    raise KeyError(name)

W, H = 5200, 2900; OX, OY = 2000, 200
out = Image.new("RGBA", (W, H), (0, 0, 0, 0))
def paste(im, x, y): out.alpha_composite(im, (int(x), int(y)))
# 1) ground layer in the listed order
sorted_items = []
for t in mapa["tiles"]:
    sec, e = find(t["sprite"]); cx, cy = e["tile_center"]
    im = frame_img(e, 0)
    if t["layer"] == "ground":
        paste(im, OX + t["pos"][0] - cx, OY + t["pos"][1] - cy)
        for o in t["overlays"]:
            _, oe = find(o); oim = frame_img(oe, 0)
            dy = oe.get("offset_y", 0) if isinstance(oe, dict) else 0
            paste(oim, OX + t["pos"][0] - cx, OY + t["pos"][1] - cy + dy)
    else:
        sorted_items.append((t["sort_y"], 0, len(sorted_items), "tile", t, e))
# 2) depth-sorted layer: raised tiles, ramps and objects
objs = list(mapa["objects"])
if STATE == "blocked":
    for b in mapa["blockers"]: objs += b["objects_while_blocked"]
else:
    for b in mapa["blockers"]: objs += b["objects_after_clear"]
    objs += mapa["mine"]["objects_after_clear"]
for o in objs: sorted_items.append((o["sort_y"], 1, len(sorted_items), "obj", o, None))
sorted_items.sort(key=lambda s: (s[0], s[1], s[2]))
for (_, _, _, kind, it, e) in sorted_items:
    if kind == "tile":
        cx, cy = e["tile_center"]; im = frame_img(e, 0)
        base = Image.new("RGBA", (im.width, im.height + 8), (0, 0, 0, 0)); base.alpha_composite(im, (0, 8))
        for o in it["overlays"]:
            _, oe = find(o); dy = oe.get("offset_y", 0) if isinstance(oe, dict) else 0
            base.alpha_composite(frame_img(oe, 0), (0, 8 + dy))
        paste(base, OX + it["pos"][0] - cx, OY + it["pos"][1] - cy - 8)
    else:
        sec, e = find(it["sprite"])
        if it.get("shadow"):
            _, se = find(it["shadow"]); sim = frame_img(se, 0)
            paste(sim, OX + it["pos"][0] - sim.width // 2 + it["shadow_offset"][0], OY + it["pos"][1] - sim.height // 2 - 2)
        gx, gy = it["tile"]
        f = ((gx + gy) // 4) if it.get("frame_mode") == "wind" else 0
        im = frame_img(e, f)
        ax, ay = it["anchor"]
        paste(im, OX + it["pos"][0] - ax, OY + it["pos"][1] - ay)
bg = Image.new("RGBA", out.size, (24, 40, 34, 255)); bg.alpha_composite(out)
bg = bg.crop(out.getbbox())
bg.save(SP + f"previa_{STATE}.png")
print("ok:", SP + f"previa_{STATE}.png", bg.size)
