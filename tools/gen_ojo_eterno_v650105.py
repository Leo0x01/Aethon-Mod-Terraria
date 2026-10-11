#!/usr/bin/env python3
"""
v6.50.105 — EL OJO DEL ETERNO (los minions son los ojos del libro).

La letra del usuario: «también debemos cambiar los minions, los minion que
use el grimorio será su propio ojo […] yo te daré los sprite del ojo usando
código es lo mejor para que pudieses editarlo en caso de que convenga, ya
sabes como animar los ojos».

El usuario entregó 3 sprites EXACTOS en código (RLE base64 de tuplas
[count,R,G,B,A], lienzo 706×967 — el formato de la casa): el LIBRO con el
ojo en 3 estados. La medición (analiza_ojos_v650105.py):
  · la ESCLERA (almendra blanca): x∈[257,510] y∈[396,545] — 253×150;
  · el ARMAZÓN del ojo (la piel lila del párpado + el rim): x∈[230,522];
  · el PÁRPADO INFERIOR DORADO: y∈[546,590] (la banda con hebillas);
  · el iris dorado (capa aparte, .84): disco r=84 en (374.3, 443.1).

EL RECORTE del minion: la caja (226,344)-(524,596) — 298×252 fuente —
corta el OJO COMPLETO con su armazón (el álamo del párpado arriba, el
cuero oscuro a los lados dando el rim de silueta, la banda dorada del
párpado inferior abajo) SIN los adornos del libro. Los 3 estados usan LA
MISMA CAJA (parpadeo con marco consistente).

Salida (44×37 juego — el ojo del libro DESPRENDIDO, más grande que en el
libro porque ya es un minion, no una ceja):
  Content/Projectiles/OjoEterno_Abierto.png    (la esclera vacía — el iris va CAPA aparte)
  Content/Projectiles/OjoEterno_Medio.png      (el párpado a medio bajar)
  Content/Projectiles/OjoEterno_Cerrado.png    (la banda dorada con hebillas)

EL IRIS: se REUTILIZA GrimorioHambriento_Iris.png (la .84 — «ya que tú
tienes el iris»): dibujado a escala 0.52 (disco ≈14 px de 44×37) para que
la MIRADA lea dentro de la almendra (el ojo artístico trae el iris
sobredimensionado respecto de la esclera: a escala proporcional taparía
la vista entera y la mirada no se leería).
"""
import base64
import os
import re

import numpy as np
from PIL import Image

UP = '/home/z/my-project/upload'
DEST = '/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod/Content/Projectiles'
PREV = '/home/z/my-project/download/Aethon-Mod-Terraria/tools/preview_ojos_v650105'
os.makedirs(PREV, exist_ok=True)

SRC_W, SRC_H = 706, 967
FILES = {
    'abierto': 'sprite libro v3 - minion ojo abierto.txt',
    'medio': 'sprite libro v3 - minion ojo medio cerrado.txt',
    'cerrado': 'sprite libro v3 - minion ojo cerrado.txt',
}

# LA CAJA del ojo (medida en analiza_ojos_v650105.py)
X0, Y0, X1, Y1 = 226, 344, 524, 596          # 298×252
GAME_W, GAME_H = 44, 37                       # el tamaño del minion en juego


def decodificar(path):
    data = open(path, encoding='utf-8').read()
    rle = base64.b64decode(re.search(r'rle:"([^"]+)"', data).group(1))
    assert len(rle) % 5 == 0
    px = bytearray(SRC_W * SRC_H * 4)
    p = i = 0
    while i + 5 <= len(rle):
        n, r, g, b, a = rle[i:i + 5]
        i += 5
        for _ in range(n):
            px[p] = r; px[p + 1] = g; px[p + 2] = b; px[p + 3] = a
            p += 4
    assert p == SRC_W * SRC_H * 4
    return np.frombuffer(bytes(px), dtype=np.uint8).reshape(SRC_H, SRC_W, 4)


def main():
    estados = {}
    for nombre, fname in FILES.items():
        estados[nombre] = decodificar(f'{UP}/{fname}')

    # === EL RECORTE COMÚN + EL ESCALADO (LANCZOS, la casa) ===
    salida = {}
    for nombre, arr in estados.items():
        caja = arr[Y0:Y1, X0:X1]
        im = Image.fromarray(caja).resize((GAME_W, GAME_H), Image.LANCZOS)
        path = f'{DEST}/OjoEterno_{nombre.capitalize()}.png'
        im.save(path, optimize=True)
        salida[nombre] = im
        print(f'OjoEterno_{nombre.capitalize()}.png  {GAME_W}×{GAME_H}  '
              f'{os.path.getsize(path):4d} B')

    # === LA VALIDACIÓN: hoja ×4 con el iris compuesto en 5 miradas ===
    iris = Image.open('/home/z/my-project/download/Aethon-Mod-Terraria/'
                       'AethonMod/Content/Weapons/GrimorioHambriento_Iris.png').convert('RGBA')
    # escala del iris: disco ≈14 px (la textura trae el disco a ~27/32)
    ESC_IRIS = 0.52
    iris_juego = iris.resize((max(1, int(32 * ESC_IRIS + 0.5)),) * 2, Image.LANCZOS)
    # posición de reposo: el centro de la almendra — esclera y∈[396,545] → centro 470.5
    # en caja: (374-226, 470.5-344)·(44/298, 37/252) = (21.9, 18.6)
    REST = (21.9, 18.6)
    MIRADAS = {'centro': (0, 0), 'izq': (-7.5, 0), 'der': (7.5, 0),
               'arriba': (0, -3.5), 'abajo': (0, 3.5)}

    celdas = []
    for tag, (mx, my) in MIRADAS.items():
        base = salida['abierto'].copy()
        px, py = REST[0] + mx, REST[1] + my
        base.alpha_composite(iris_juego, (int(px - iris_juego.width / 2),
                                          int(py - iris_juego.height / 2)))
        celdas.append((f'abierto {tag}', base))
    celdas.append(('medio', salida['medio']))
    celdas.append(('cerrado', salida['cerrado']))

    Z = 4
    cw, ch = GAME_W * Z, GAME_H * Z
    sheet = Image.new('RGBA', (cw * len(celdas) + 12 * (len(celdas) - 1), ch + 28),
                      (24, 20, 32, 255))
    from PIL import ImageDraw
    dr = ImageDraw.Draw(sheet)
    for i, (tag, im) in enumerate(celdas):
        big = im.resize((cw, ch), Image.NEAREST)
        sheet.paste(big, (i * (cw + 12), 24), big)
        dr.text((i * (cw + 12) + 4, 2), tag, fill=(255, 255, 255))
    sheet.save(f'{PREV}/hoja_minion_validacion.png')
    print('hoja de validación:', f'{PREV}/hoja_minion_validacion.png', sheet.size)


if __name__ == '__main__':
    main()
