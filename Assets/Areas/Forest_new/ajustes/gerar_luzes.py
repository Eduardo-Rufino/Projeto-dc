"""Gera as folhas do poste e da antena com a luz piscando SEM a estrutura se mexer.

No kit original os quadros 3-4 (luz apagada) são outro desenho, mais curto e deslocado, e a
estrutura parecia pular. Aqui os quadros ficam: 1 e 2 do kit (luz acesa; no poste os fios
balançam entre eles) + os mesmos 1 e 2 com a luz apagada (brilho semitransparente removido e
lâmpada escurecida). O MapLoader usa um arquivo desta pasta no lugar do de mesmo caminho do kit.

Rodar da raiz do projeto:  python Assets/Areas/Forest_new/ajustes/gerar_luzes.py
"""
from pathlib import Path
from PIL import Image

KIT = Path("Assets/Areas/Forest_new/modelo_floresta/kit/prop_sheets")
OUT = Path(__file__).resolve().parent / "prop_sheets"

# folha, largura do quadro, cores das luzes que apagam (as outras ficam acesas sempre)
SHEETS = [
    ("power_pole.png", 53, [(100, 236, 250)]),
    ("radio_antenna.png", 77, [(255, 80, 90)]),
]
# só procura a luz no alto da arte
LIGHT_ROWS = 24


def near(c, light, tol=12):
    return all(abs(a - b) <= tol for a, b in zip(c, light))


def is_core(c, light):
    # pixel opaco da lâmpada: a cor da luz ou quase branco puxado pra ela
    if near(c, light, 40):
        return True
    r, g, b = c
    return min(c) >= 150 and max(c) >= 230 and (light[0] > light[2]) == (r > b)


def lights_off(frame, lights):
    off = frame.copy()
    px = off.load()
    for y in range(min(LIGHT_ROWS, off.height)):
        for x in range(off.width):
            r, g, b, a = px[x, y]
            if a == 0:
                continue
            for light in lights:
                if a < 255 and near((r, g, b), light):
                    px[x, y] = (0, 0, 0, 0)
                    break
                if a == 255 and is_core((r, g, b), light):
                    px[x, y] = (int(r * 0.35), int(g * 0.35), int(b * 0.35), 255)
                    break
    return off


OUT.mkdir(parents=True, exist_ok=True)
for name, fw, lights in SHEETS:
    sheet = Image.open(KIT / name).convert("RGBA")
    on = [sheet.crop((i * fw, 0, (i + 1) * fw, sheet.height)) for i in (0, 1)]
    frames = on + [lights_off(f, lights) for f in on]
    result = Image.new("RGBA", sheet.size, (0, 0, 0, 0))
    for i, f in enumerate(frames):
        result.paste(f, (i * fw, 0))
    result.save(OUT / name)
    print("gerado", OUT / name)
