#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_iconos_v650113.py — LOS TRES ICONOS DEL USUARIO (v6.50.113).

La letra: «el icono que debes usar para el buff cuando sale el minion
es este, icono de minion.txt / tambien para el icono del mod o sea lo
que se ve cuando instala el mod es este, icono del mod.txt / ahora para
el icono de la mascota de luz es este icono de mascota de luz.txt».

Los tres llegan como SPRITE_EXACTO (base64 → tuplas de 5 bytes:
n,R,G,B,A — runs de 255 máx) a 2048×2048. DESTINOS (verificados por
DECOMPILE de tML 2026.08.3.0 — ContentConverters + ModLoader.
GetModIcon + UIModItem):
  · Content/Buffs/OjoEternoBuff.png   32×32 (el buff del minion —
    rawimg en el .tmod, como TODO .png salvo icon.png)
  · Content/Buffs/AethonMenorBuff.png 32×32 (el buff de la mascota)
  · icon.png (RAÍZ DEL MOD)           80×80 EXACTOS — GetModIcon:
    «icon.png is not 80x80» si no lo es; ContentConverters lo EXIME
    de la conversión rawimg: viaja como PNG CRUDO y el menú Mods lo
    dibuja (UIModItem GetModIcon ?? PlaceholderModIcon).

Uso:
  python3 tools/gen_iconos_v650113.py            # candidatos + previews
  python3 tools/gen_iconos_v650113.py NEAREST|BOX|LANCZOS <destino>
      — instala el filtro elegido (por destino: buff-minion |
        buff-mascota | mod). P.ej.:
  python3 tools/gen_iconos_v650113.py LANCZOS buff-minion
"""
import base64
import os
import re
import sys
from PIL import Image

RAIZ = os.path.join(os.path.dirname(__file__), '..')
UPLOAD = '/home/z/my-project/upload'
OUT = os.path.join(os.path.dirname(__file__), 'tools')
FUENTES = {
    'buff-minion': ('icono de minion.txt', 'Content/Buffs/OjoEternoBuff.png', 32),
    'buff-mascota': ('icono de mascota de luz.txt', 'Content/Buffs/AethonMenorBuff.png', 32),
    'mod': ('icono del mod.txt', 'icon.png', 80),
    # v6.50.113b — EL HERMANO PEQUEÑO: el viejo icon_small.png (30×30,
    # desde la .13) seguía mostrando el ARTE VIEJO en las vistas
    # compactas — se regenera del MISMO arte para que el mod se vea
    # igual en grande y en pequeño. Empírico del decompile: AddResource
    # compara icon_small.png con la plantilla (consume el stream) →
    # ToRaw falla → viaja como PNG CRUDO «icon_small.png» — y así lo
    # necesita GetModIcon("icon_small.png", 30).
    'mod-small': ('icono del mod.txt', 'icon_small.png', 30),
}
FILTROS = {
    'NEAREST': Image.NEAREST,
    'BOX': Image.BOX,
    'LANCZOS': Image.LANCZOS,
}


def decodificar(nombre):
    s = open(os.path.join(UPLOAD, nombre), encoding='utf-8').read()
    w = int(re.search(r'w:(\d+)', s).group(1))
    h = int(re.search(r'h:(\d+)', s).group(1))
    rle = re.search(r'rle:"([^"]*)"', s).group(1)
    bin_ = base64.b64decode(rle)
    px = bytearray(w * h * 4)
    p = 0
    i = 0
    while i + 5 <= len(bin_):
        n = bin_[i]
        r, g, b, a = bin_[i + 1], bin_[i + 2], bin_[i + 3], bin_[i + 4]
        i += 5
        for _ in range(n):
            px[p] = r
            px[p + 1] = g
            px[p + 2] = b
            px[p + 3] = a
            p += 4
    assert p == w * h * 4
    return Image.frombytes('RGBA', (w, h), bytes(px))


def candidatos():
    for clave, (src, dest, tam) in FUENTES.items():
        im = decodificar(src)
        print(f'  {clave}: {src} {im.size} -> {dest} {tam}x{tam}')
        for nombre, filtro in FILTROS.items():
            chico = im.resize((tam, tam), filtro)
            chico.save(os.path.join(OUT, f'icono_v650113_{clave}_{nombre}.png'))
            prev = chico.resize((tam * 8, tam * 8), Image.NEAREST)
            prev.save(os.path.join(OUT, f'icono_v650113_{clave}_{nombre}_8x.png'))
    print('  OK — candidatos y previews en tools/tools/')


def instalar(filtro, clave):
    if filtro not in FILTROS or clave not in FUENTES:
        print(__doc__)
        sys.exit(1)
    src, dest, tam = FUENTES[clave]
    im = decodificar(src).resize((tam, tam), FILTROS[filtro])
    # RGBA y SIN alfa parcial accidental (los orígenes son opacos): normal
    im = im.convert('RGBA')
    camino = os.path.join(RAIZ, 'AethonMod', dest)
    os.makedirs(os.path.dirname(camino), exist_ok=True)
    im.save(camino)
    print(f'  {clave}: {filtro} instalado -> AethonMod/{dest} ({tam}x{tam} RGBA)')


if __name__ == '__main__':
    print('GEN ICONOS v6.50.113:')
    if len(sys.argv) == 3:
        instalar(sys.argv[1], sys.argv[2])
    else:
        candidatos()
