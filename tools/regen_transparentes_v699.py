#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
regen_transparentes_v699.py — LA LEY DE LA TRANSPARENCIA, PARTE 2.

LA LETRA DEL USUARIO (v6.99): «posiblemente hiciste mal la transparencia
ya que arruino el sprite del altar, lo que tienes que hacer es regenerar
los sprites […] pero con transparencia en el fondo, NO aplicar la
transparencia ya que te comiste varios colores dejando el altar
arruinado».

LA AUTOPSIA: el CORTE de la v6.98 (cmd #13 de la superforja) cortó por
COLOR (keying global con correa): en los sprites cuyo ARTE usa azules
noche parecidos al fondo se COMIÓ el cuerpo (Campana −215 px de arte ·
Pupila −89 · Juicio −32 · Letania −13), y en los que no, dejó restos.
Esta herramienta es LA CURA DETERMINISTA:

  · LA INUNDACIÓN DE BORDE (la de siempre, la del molde): solo se va
    el fondo ALCANZABLE DESDE EL BORDE del lienzo caminando por la
    familia del fondo — UNA VEJIGA conectada al marco. Lo que está
    ENCERRADO por el arte (las sombras internas, los cuerpos oscuros)
    NUNCA se toca: no existe tolerancia que los coma porque el flood
    no puede saltar el contorno del dibujo.
  · CERO re-tinte: el RGBA de cada píxel superviviente es el del
    original, byte a byte. Solo el alfa del fondo pasa a 0. Ningún
    des-fringe, ningún peine: los colores NO se tocan (la lección).

LA DECISIÓN POR SPRITE (QA VLM de la sesión, montaje a 6×):
  · 10 sprites → LA INUNDACIÓN (REGEN): el arte queda completo y el
    fondo se va entero (Campana · Cetro · Espinas… NO — ver abajo ·
    Geometría · Hambre · Juicio · Letania · Pupila · Semilla ·
    Susurro · Cadenas).
  · EspinasNocturnas → SE QUEDA LA v6.98 (el CORTE acertó aquí): su
    cuerpo de espinas USA el color del fondo y la inundación lo
    vacía (agujeros por el cuerpo — la autopsia visual); el corte de
    color de la v6.98 en ESTE sprite conservó masa y disco de sombra.
    La ambigüedad color-arte=color-fondo es irresoluble por máscara
    automática: gana la versión viva ya en el mod.
  · CoroDeLosProfundos + LamparaDelPeregrino → RESTAURADAS de la
    v6.97 (ya eran transparentes y perfectas; el CORTE las mutiló
    sin motivo: Coro 1152→669, Lámpara 1111→367).

Y EL ALTAR (la razón de la sesión): se REGENERARÁ con
tools/altar_coro_v698.py desde el Coro restaurado — la composición
nativa sobre lienzo transparente (el paste con alfa), que es
«regenerar con transparencia», no «aplicar transparencia».

TODO local, sin nube (la regla de la casa).
"""
import io
import os
import subprocess
import sys
from collections import Counter

from PIL import Image, ImageDraw

AQUI = os.path.dirname(os.path.abspath(__file__))
RAIZ = os.path.join(AQUI, '..')
CARPETA = os.path.join(RAIZ, 'AethonMod', 'Content', 'Weapons', 'TresCasas')

# la fuente de la verdad: la v6.97 (el último commit con el arte íntegro)
FUENTE_COMMIT = 'e061523'

# los que se regeneran con LA INUNDACIÓN de borde
REGEN_FLOOD = [
    'CadenasDelOlvido', 'CampanaDelSantuario', 'CetroDelAlba',
    'GeometriaImposible', 'HambrePrimigenia', 'JuicioDelCielo',
    'LetaniaNebular', 'PupilaDelAbismo', 'SemillaDeEstrellas',
    'SusurroDelVacio',
]
# los que YA están bien en el disco (no se tocan — documentado arriba)
SE_QUEDAN = ['EspinasNocturnas']

TOLERANCIA = 12  # la familia del fondo (estable en 4..16 — probado)


def _dist(a, b):
    return ((a[0] - b[0]) ** 2 + (a[1] - b[1]) ** 2 +
            (a[2] - b[2]) ** 2) ** 0.5


def original_v697(nombre):
    """El sprite ÍNTEGRO de la v6.97, sacado de git (la fuente de la
    verdad — ni una versión intermedia)."""
    ruta = f'AethonMod/Content/Weapons/TresCasas/{nombre}.png'
    r = subprocess.run(['git', 'show', f'{FUENTE_COMMIT}:{ruta}'],
                       cwd=RAIZ, capture_output=True)
    if r.returncode != 0:
        raise RuntimeError(f'git no encontró {ruta}@{FUENTE_COMMIT}')
    return Image.open(io.BytesIO(r.stdout)).convert('RGBA')


def inundacion_de_borde(im, tol=TOLERANCIA):
    """LA INUNDACIÓN: el fondo alcanzable DESDE EL BORDE caminando por
    la familia del fondo (la lista de colores dominantes del anillo del
    marco). Lo encerrado por el arte NUNCA se toca."""
    W, H = im.size
    px = im.load()

    # 1 — LA FAMILIA DEL FONDO: los colores dominantes del anillo (los
    #     primeros 2 píxeles del marco de toda la vida)
    anillo = Counter()
    for x in range(W):
        for y in (0, 1, H - 2, H - 1):
            anillo[px[x, y][:3]] += 1
    for y in range(H):
        for x in (0, 1, W - 2, W - 1):
            anillo[px[x, y][:3]] += 1
    familia = [c for c, n in anillo.most_common(6) if n > 20]
    if not familia:
        raise RuntimeError('no hay familia de fondo en el anillo')

    # 2 — EL FLOOD desde TODO el marco (una vejiga conectada)
    fondo = set()
    pila = []
    for x in range(W):
        pila += [(x, 0), (x, H - 1)]
    for y in range(H):
        pila += [(0, y), (W - 1, y)]
    while pila:
        x, y = pila.pop()
        if (x, y) in fondo or not (0 <= x < W and 0 <= y < H):
            continue
        c = px[x, y][:3]
        if all(_dist(c, b) > tol for b in familia):
            continue
        fondo.add((x, y))
        pila += [(x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)]

    # 3 — LA RECOMPOSICIÓN NATIVA: lienzo transparente + SOLO el arte
    #     (el RGBA original byte a byte — cero re-tinte, cero fringe)
    out = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    po = out.load()
    for y in range(H):
        for x in range(W):
            if (x, y) not in fondo:
                po[x, y] = px[x, y]
    return out


def damero(im, s=6):
    """El fondo de damero de la casa (el QA se VE sobre transparencia)."""
    big = im.resize((im.width * s, im.height * s), Image.NEAREST)
    bg = Image.new('RGBA', big.size, (255, 255, 255, 255))
    p = bg.load()
    c = s
    for y in range(big.height):
        for x in range(big.width):
            if ((x // c) + (y // c)) % 2 == 0:
                p[x, y] = (204, 204, 204, 255)
    bg.paste(big, (0, 0), big)
    return bg


def main():
    print('LA REGENERACIÓN NATIVA (v6.99) — la inundación de borde:')
    print('  la fuente: la v6.97 (git) · la cura: flood conectado al marco')
    print()
    filas = []
    for nombre in REGEN_FLOOD:
        ruta = os.path.join(CARPETA, nombre + '.png')
        antes = Image.open(ruta).convert('RGBA')
        vivos_antes = sum(1 for p in antes.getdata() if p[3] > 0)
        original = original_v697(nombre)
        nuevo = inundacion_de_borde(original)
        vivos = sum(1 for p in nuevo.getdata() if p[3] > 0)
        nuevo.save(ruta)
        print(f'  {nombre:22} {vivos_antes:4} → {vivos:4} vivos '
              f'(el original v6.97 tenía {original.width * original.height})')
        filas.append((nombre, original, nuevo))
    print()
    for nombre in SE_QUEDAN:
        print(f'  {nombre:22} SE QUEDA la viva (la decisión de la sesión, '
              f'documentada arriba)')
    print(f'  {"CoroDeLosProfundos":22} restaurada de la v6.97 (aparte)')
    print(f'  {"LamparaDelPeregrino":22} restaurada de la v6.97 (aparte)')

    # EL MONTAJE QA (para el ojo de la casa)
    S = 6
    cw = max(f[1].width for f in filas) * S
    ch = max(f[1].height for f in filas) * S
    m = Image.new('RGBA', (cw * 2 + 50, len(filas) * (ch + 18) + 18),
                  (250, 250, 250, 255))
    d = ImageDraw.Draw(m)
    for i, (nombre, original, nuevo) in enumerate(filas):
        y = 18 + i * (ch + 18)
        m.paste(damero(original), (0, y))
        m.paste(damero(nuevo), (cw + 50, y))
        d.text((2, y - 12), nombre, fill=(0, 0, 0, 255))
        d.text((cw + 52, y - 12), 'REGENERADO', fill=(0, 110, 0, 255))
    destino = os.path.join('/tmp', 'montaje_regen_v699.png')
    m.save(destino)
    print(f'\n  el montaje QA → {destino}')


if __name__ == '__main__':
    main()
