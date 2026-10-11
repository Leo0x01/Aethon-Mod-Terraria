#!/usr/bin/env python3
"""
v6.50.106 — EL OJO DEL USUARIO, SIN RECORTE.

La letra del usuario: «no debes hacer recorte ya que arruinaste el recorte
que yo mismo hice, solo debes acomodar el iris para que pueda realmente
funcionar […] te volveré a dar el código del minion por si lo perdiste al
recortarlo, recuerda no debes recortar el sprite que te di ya que lo recorte
perfecto como debe ser».

LA LECCIÓN de la .105: los 3 archivos «sprite libro v3 - minion ojo X» YA
ERAN el recorte del usuario (bbox opaco (121,199)-(632,698) = 512×500 del
ojo con su armazón dorado, sus 8 remaches y sus chispas moradas) — y la
.105 le hizo OTRO recorte encima ((226,344)-(524,596): la almendra sin el
aro exterior ni los remaches) y lo molió a 44×37 por LANCZOS. Doble
destrucción. Esta vez: CERO recorte — el contenido opaco del archivo ES el
sprite, sólo se escala al tamaño de juego y se acomoda el iris.

EL IRIS («solo debes acomodar el iris»): la textura de la .84
(GrimorioHambriento_Iris) como CAPA en el centro de la ALMENDRA medida
del arte: la esclera en el recorte del usuario ocupa el cuadro local
(136,197)-(389,346) (253×149 — medición por brillo, 91,4% de relleno) →
centro (262.5, 271.5) de 512×500 → fracción (0.513, 0.543).
"""
import base64
import os
import re

import numpy as np
from PIL import Image, ImageDraw

UP = '/home/z/my-project/upload'
MOD = '/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod'
DEST = f'{MOD}/Content/Projectiles'
PREV = '/home/z/my-project/download/Aethon-Mod-Terraria/tools/preview_ojos_v650106'
os.makedirs(PREV, exist_ok=True)
SRC_W, SRC_H = 706, 967
FILES = {
    'abierto': 'sprite libro v3 - minion ojo abierto.txt',
    'medio': 'sprite libro v3 - minion ojo medio cerrado.txt',
    'cerrado': 'sprite libro v3 - minion ojo cerrado.txt',
}

# EL TAMAÑO DE JUEGO (56×54: 9,1× — el aro, los remaches y las chispas leen)
GAME_W, GAME_H = 56, 54


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


def bbox_opaco(arr):
    m = arr[..., 3] >= 8
    ys, xs = np.where(m)
    return int(xs.min()), int(ys.min()), int(xs.max()), int(ys.max())


def main():
    estados = {n: decodificar(f'{UP}/{f}') for n, f in FILES.items()}

    # === EL RECORTE DEL USUARIO = el bbox opaco COMÚN (cero criterio propio) ===
    X0, Y0, X1, Y1 = bbox_opaco(estados['abierto'])
    for n, arr in estados.items():
        b = bbox_opaco(arr)
        assert (b[0], b[1], b[2], b[3]) == (X0, Y0, X1, Y1), f'bbox distinto en {n}: {b}'
    W, H = X1 - X0 + 1, Y1 - Y0 + 1
    print(f'recorte del usuario: ({X0},{Y0})-({X1},{Y1}) = {W}×{H} — SIN tocar')

    # === LA ALMENDRA (medida por brillo dentro del recorte — la del canon) ===
    ab = estados['abierto'][Y0:Y1 + 1, X0:X1 + 1]
    r, g, b, a = (ab[..., 0].astype(int), ab[..., 1].astype(int),
                  ab[..., 2].astype(int), ab[..., 3].astype(int))
    claro = ((r + g + b) / 3 > 140) & (a > 150)
    # la esclera: el bloque claro CONTIGUO al centro (la caja del canon local)
    caja = np.zeros_like(claro)
    caja[197:347, 136:390] = claro[197:347, 136:390]
    ys, xs = np.where(caja)
    ax0, ay0, ax1, ay1 = xs.min(), ys.min(), xs.max(), ys.max()
    CX, CY = (ax0 + ax1) / 2, (ay0 + ay1) / 2
    print(f'almendra: ({ax0},{ay0})-({ax1},{ay1}) · centro ({CX:.1f},{CY:.1f}) '
          f'· fracción ({CX / W:.3f},{CY / H:.3f})')

    # === EL ESCALADO (CERO recorte adicional: el contenido entero) ===
    iris = Image.open(f'{MOD}/Content/Weapons/GrimorioHambriento_Iris.png').convert('RGBA')
    salida = {}
    for nombre, arr in estados.items():
        contenido = arr[Y0:Y1 + 1, X0:X1 + 1]
        im = Image.fromarray(contenido).resize((GAME_W, GAME_H), Image.LANCZOS)
        # LA NORMALIZACIÓN DE tML: el empaquetador pone a CERO el RGB de
        # los píxeles de alpha 0 (el LANCZOS deja ringing 1-2 ahí); sin
        # esto el audit ve DIFF paquete≠árbol en ~19 px invisibles
        a = np.array(im)
        transp = a[..., 3] == 0
        a[transp, 0] = 0; a[transp, 1] = 0; a[transp, 2] = 0
        im = Image.fromarray(a)
        path = f'{DEST}/OjoEterno_{nombre.capitalize()}.png'
        im.save(path, optimize=True)
        salida[nombre] = im
        print(f'OjoEterno_{nombre.capitalize()}.png  {GAME_W}×{GAME_H}  {os.path.getsize(path):4d} B')

    # === EL IRIS ACOMODADO: reposo en el centro de la almendra, escala 0.41
    #     (caja 13,1 px → disco ~11 px dentro de la almendra 27,7×16,1) ===
    ESC_IRIS = 0.41
    iris_juego = iris.resize((max(1, int(32 * ESC_IRIS + 0.5)),) * 2, Image.LANCZOS)
    REST = (CX / W * GAME_W, CY / H * GAME_H)
    print(f'reposo del iris: ({REST[0]:.1f},{REST[1]:.1f}) de {GAME_W}×{GAME_H}')
    # el viaje de la mirada: ±7,5 X (borde del iris 13,05 < media almendra 13,85)
    # · ±2,5 Y (borde 8,05 ≈ media altura 8,05) — la geometría de la .105
    # recalibrada a la almendra del usuario
    MIRADAS = {'centro': (0, 0), 'izq': (-7.5, 0), 'der': (7.5, 0),
               'arriba': (0, -2.5), 'abajo': (0, 2.5)}

    celdas = []
    for tag, (mx, my) in MIRADAS.items():
        base = salida['abierto'].copy()
        px, py = REST[0] + mx, REST[1] + my
        base.alpha_composite(iris_juego, (int(px - iris_juego.width / 2),
                                          int(py - iris_juego.height / 2)))
        celdas.append((f'abierto {tag}', base))
    celdas.append(('medio', salida['medio']))
    celdas.append(('cerrado', salida['cerrado']))

    # === LA HOJA DE VALIDACIÓN (×4) ===
    Z = 4
    cw, ch = GAME_W * Z, GAME_H * Z
    sheet = Image.new('RGBA', (cw * len(celdas) + 12 * (len(celdas) - 1), ch + 28),
                      (24, 20, 32, 255))
    dr = ImageDraw.Draw(sheet)
    for i, (tag, im) in enumerate(celdas):
        big = im.resize((cw, ch), Image.NEAREST)
        sheet.paste(big, (i * (cw + 12), 24), big)
        dr.text((i * (cw + 12) + 4, 2), tag, fill=(255, 255, 255))
    sheet.save(f'{PREV}/hoja_minion_v650106.png')
    print('hoja:', f'{PREV}/hoja_minion_v650106.png', sheet.size)

    # === comparativa con la .105 arruinada (para el registro) ===
    v105 = Image.open(f'{DEST}/OjoEterno_Abierto.png')  # ya reemplazado — noops
    print('OK')


if __name__ == '__main__':
    main()
