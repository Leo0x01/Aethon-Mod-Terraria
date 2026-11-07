#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
altar_v6111.py — v6.111 — EL ALTAR DEL OJO QUE OBSERVA.

LA LETRA DEL USUARIO: «tambien debes cambiar las texturas del altar,
regenera la textura para crear un altar digno de una entidad cosmica,
dale nueva tematica al altar que sea diferente».

LA TEMÁTICA MUERTA (v6.110): el vórtice violeta con velas y chapitel
estelar — el portal gótico. LA TEMÁTICA NUEVA (v6.111): EL OJO — el
altar ES el ojo de una entidad cósmica QUE TE OBSERVA: la cámara se
abre en UN GRAN OJO de iris-galaxia cuya PUPILA SE MUEVE (de marco a
marco te mira a la izquierda, al centro, a la derecha…), con sus
LÁGRIMAS de polvo estelar cayendo por la piedra, LA CEJA de metal
estelar y SU ESTRELLA (el tercer ojo) latiendo arriba. Piedra
OBSIDIANA negra-violeta con el resplandor ámbar del ojo tiñéndola.

LA CURA px-max v6.111 (la lección de los iconos, AQUÍ también): el
alpha NUNCA baja — el pincel no borra el opaco del sello vecino.

REGLA PERMANENTE: transparencia NATIVA y el MISMO MAPEO (34×300,
6 marcos de 50 px, arte hasta la última fila — la silueta de la
v6.101/v6.110 INTACTA: el recorte vertical queda byte-perfecto).

Salidas:
  Content/Tiles/AltarOfrendasTile.png          (34×300)
  Content/Tiles/AltarOfrendasTile_Highlight.png (34×50)
  Content/Items/Altar/AltarOfrendasItem.png     (34×50 — el marco 0)
"""
import math
import os

from PIL import Image

W, H = 34, 50
MARCOOS = 6
CX = 17          # el eje del altar

# ---------------------------------------------------------------- paletas
OBSIDIANA = (26, 20, 44)        # la piedra negra-violeta
OBSIDIANA_SOMBRA = (14, 10, 26)
OBSIDIANA_LUZ = (44, 36, 74)
ORO = (255, 208, 96)            # el metal estelar
ORO_HONDO = (188, 138, 40)
AMBAR = (255, 224, 150)         # la luz del ojo
BLANCO = (255, 255, 250)        # la pupila
IRIS_ORO = (255, 216, 110)
IRIS_CORAL = (255, 150, 96)
LAGRIMA = (190, 240, 255)       # el polvo estelar
RUNA = (150, 84, 235)           # la runa violeta de la casa


# ------------------------------------------------------------ helpers
def px(im, x, y, c, a=255):
    """ESCRIBE SIEMPRE (el orden lo dan LAS CAPAS: cada componente pinta
    su capa y generar() las compone — la lección de la v6.111: el
    px-max de los iconos AQUÍ mataba el pintado (la piedra ya estaba a
    255 y el ojo jamás escribía encima — EL ALTAR SEQUÍA en piedra))."""
    x, y = int(round(x)), int(round(y))
    if 0 <= x < W and 0 <= y < H:
        im.putpixel((x, y), (c[0], c[1], c[2], a))


def dentro(x, y):
    """LA SILUETA (el mapeo probado — INTACTO byte-perfecto)."""
    if y >= 41:
        return 0 <= x < W
    if y >= 32:
        return 1 <= x < W - 1
    if y >= 10:
        return 5 <= x <= 29
    if y >= 5:
        return 7 + (10 - y) // 2 <= x <= 26 - (10 - y) // 2
    half = 5 + y
    return CX - half <= x <= CX + half


def mezcla(a, b, k):
    return tuple(int(round(a[i] + (b[i] - a[i]) * k)) for i in range(3))


# ------------------------------------------------------------ LA PIEDRA
def cuerpo_piedra(im, f=0):
    """LA OBSIDIANA CON VOLUMEN: la luz cenital fría arriba-izquierda,
    la penumbra creciente hacia abajo… y EL OJO LA TIÑE: las caras de
    la piedra que rodean la cámara se encienden de ÁMBAR (el resplandor
    del ojo que las mira desde dentro)."""
    for y in range(H):
        for x in range(W):
            if not dentro(x, y):
                continue
            # EL VOLUMEN: k=0 arriba-izquierda -> 1 abajo-derecha.
            k = (x / W) * 0.35 + (y / H) * 0.65
            c = mezcla(OBSIDIANA_LUZ, OBSIDIANA_SOMBRA, k)
            # EL RESPLANDOR DEL OJO: la piedra vecina a la cámara (y 8-32)
            # se enciende de ámbar — la luz que ESCAPA del ojo.
            d_ojo = math.hypot(x - CX, y - 21)
            if d_ojo < 15.0:
                c = mezcla(c, (120, 84, 60), (1.0 - d_ojo / 15.0) * 0.55)
            px(im, x, y, c)
            # LAS FACETAS (el brillo de la obsidiana: dos vetas verticales
            # de luz que bajan por los pilares — la piedra tallada).
            if y < 41 and 9 <= y <= 31:
                for vx in (8, 26):
                    if abs(x - vx) <= 0 and (y % 7) < 5:
                        px(im, x, y, mezcla(c, (95, 82, 140), 0.5))
            # EL OUTLINE VIVO: el borde de la silueta, más hondo abajo.
            if not dentro(x - 1, y) or not dentro(x + 1, y) or \
               not dentro(x, y - 1) or not dentro(x, y + 1):
                borde = mezcla(OBSIDIANA, (10, 8, 20), y / H)
                px(im, x, y, borde)


# ------------------------------------------------------------- EL OJO
def el_ojo_f(im, f):
    """EL GRAN OJO — la cámara entera se abre en un ojo de almendra:
    la esclerótica de vacío violeta, EL IRIS de galaxia (la espiral
    girando 60°/marco) y LA PUPILA que TE MIRA (se mueve de marco a
    marco: izquierda -> centro -> derecha -> centro…)."""
    ecx, ecy = CX, 21.0     # el centro del ojo
    rx, ry = 11.2, 7.6      # la almendra

    # LA MIRADA — el RECORRIDO CIRCULAR completo (ningún marco repite
    # la posición: la entidad nunca parpadea dos veces igual).
    mirada = [-4, -2, 0, 4, 2, 0][f % 6]
    # EL GIRO del iris (60°/marco).
    giro = math.radians(60 * f)

    for y in range(9, 33):
        for x in range(4, 30):
            # LA ALMENDRA (vesica): dentro si |dx/rx| + |dy/ry| <= 1
            dx, dy = x - ecx, y - ecy
            alm = abs(dx) / rx + abs(dy) / ry
            if alm > 1.0:
                continue
            # LA ESCLERÓTICA: el vacío violeta hondo (más hondo abajo).
            c = mezcla((40, 30, 66), (16, 12, 30), (y - 9) / 24.0)
            px(im, x, y, c)
            # EL IRIS: el disco de la galaxia (r 6.2) centrado en la
            # MIRADA (la pupila viaja DENTRO del ojo).
            icx, icy = ecx + mirada, ecy
            d = math.hypot(x - icx, y - icy)
            if d < 6.4:
                # EL NÚCLEO blanco-oro (menos saturado: más blanco).
                c = mezcla(mezcla(IRIS_ORO, BLANCO, 0.35), BLANCO,
                           max(0.0, 1.0 - d / 3.0))
                px(im, x, y, c)
                # EL ANILLO CÓRNEO (el aro oscuro que separa el iris del
                # párpado — la córnea de la entidad).
                if d > 5.2:
                    px(im, x, y, (52, 38, 84))
                # LA ESPIRAL: dos brazos cortos que giran con el marco.
                th = math.atan2(y - icy, x - icx) + giro
                r = d / 6.4
                if (math.cos(th * 2.0 + r * 5.0) > 0.55 and d > 2.6):
                    px(im, x, y, IRIS_CORAL if r > 0.72 else ORO)
                # LA PUPILA (el punto que TE MIRA).
                if d < 2.0:
                    px(im, x, y, BLANCO)
            elif d < 7.4:
                px(im, x, y, mezcla((40, 30, 66), (90, 70, 130), 0.6))

    # LOS PÁRPADOS: el filete de ORO que cierra la almendra (arriba
    # GORDO, abajo fino — el ojo entrecerrado de la entidad).
    for x in range(4, 30):
        dx = x - ecx
        if abs(dx) > rx:
            continue
        # el párpado superior: y = ecy - ry*(1-|dx|/rx)
        ysup = ecy - ry * (1 - abs(dx) / rx)
        for dy in range(-1, 2):
            px(im, x, ysup + dy, ORO if dy < 1 else ORO_HONDO)
        yinf = ecy + ry * (1 - abs(dx) / rx)
        px(im, x, yinf, ORO_HONDO)
    # LOS LAGRIMALES: los destellos de las comisuras.
    px(im, ecx - rx + 1, ecy - 1, BLANCO)
    px(im, ecx + rx - 1, ecy - 1, AMBAR)


def las_lagrimas_f(im, f):
    """LAS LÁGRIMAS DE POLVO ESTELAR — dos hilos de luz que caen desde
    las comisuras por la piedra obsidiana hasta el pedestal (el polvo
    que la entity derrama por habitar la piedra)."""
    brillo = [0.55, 0.75, 1.0, 0.8, 0.6, 0.9][f % 6]
    for lado in (-1, 1):
        x0 = CX + lado * 9
        for y in range(30, 45):
            # el hilo ondulado (la lágrima resbala).
            x = x0 + int(round(math.sin(y * 0.55 + lado) * 1.2))
            a = int(255 * brillo * (0.5 + 0.5 * math.sin(y * 0.7 + f)))
            px(im, x, y, LAGRIMA, max(90, a))
            px(im, x + lado, y, LAGRIMA, max(50, a // 2))
        # EL CHARCO (la lágrima muere en el pedestal con su destello).
        px(im, x0, 44, LAGRIMA, 230)
        px(im, x0 - 1, 44, LAGRIMA, 140)
        px(im, x0 + 1, 44, LAGRIMA, 140)


def la_ceja_f(im, f):
    """LA CEJA DE METAL ESTELAR — el arco de oro sobre la cámara (los
    hombros del arco) y SU ESTRELLA: el TERCER OJO latiendo arriba
    (la cúpula: la estrella guía que apunta al cielo)."""
    # EL ARCO (las filas 5-9, siguiendo la silueta de los hombros).
    for y in range(5, 10):
        for x in range(W):
            if not dentro(x, y):
                continue
            # LA CEJA GORDA (el filete de la ceja: DOS filas de oro).
            if y <= 8 and dentro(x, y - 1) and not dentro(x, y - 2):
                px(im, x, y, ORO)
                if dentro(x, y + 1):
                    px(im, x, y + 1, ORO)
                if dentro(x, y + 2):
                    px(im, x, y + 2, ORO_HONDO)
    # LA ESTRELLA (el tercer ojo): el latido de 6 fases.
    late = 0.5 + 0.5 * math.sin(f / 6.0 * 2 * math.pi)
    sx, sy = CX, 3
    for dy in range(-2, 3):
        for dx in range(-2, 3):
            d = math.hypot(dx, dy)
            if d < 1.6:
                px(im, sx + dx, sy + dy, BLANCO if d < 0.7 else AMBAR,
                   int(180 + 75 * late))
            elif d < 2.4:
                px(im, sx + dx, sy + dy, AMBAR, int(90 + 90 * late))


def el_pedestal_f(im, f):
    """EL PEDESTAL — las dos gradas de obsidiana con SU trim de oro y
    EL CÍRCULO DE RUNAS (12 marcas que persiguen: la runa viva salta
    una posición por marco)."""
    # LAS GRADAS.
    for y in range(41, 50):
        for x in range(W):
            if not dentro(x, y):
                continue
            k = (y - 41) / 9.0
            c = mezcla(OBSIDIANA, OBSIDIANA_SOMBRA, k)
            px(im, x, y, c)
    # EL TRIM DE ORO (los filetes de las gradas) + SU SOMBRA (la penumbra
    # que el filete arroja sobre la grada — el volumen del oro).
    for x in range(W):
        if dentro(x, 41):
            px(im, x, 41, ORO)
            px(im, x, 42, mezcla(ORO_HONDO, ORO, 0.5))
            px(im, x, 43, (12, 9, 22, 235))     # la sombra bajo el trim
        if dentro(x, 45):
            px(im, x, 45, ORO_HONDO)
            px(im, x, 46, (12, 9, 22, 200))
    # EL CÍRCULO DE RUNAS (12 marcas — la persecución: corren 1/marko).
    offset = f % 12
    for k in range(12):
        th = math.tau * ((k + offset) % 12) / 12.0
        x = CX + int(round(math.cos(th) * 13))
        y = 46 + int(round(math.sin(th) * 2.2))
        # la runa viva: la que LE TOCA en este marco ARDE.
        es_viva = (k == 0)
        c = AMBAR if es_viva else (198, 160, 255)
        a = 255 if es_viva else 235
        px(im, x, y, c, a)
        if dentro(x - 1, y) and dentro(x + 1, y):
            px(im, x - 1, y, c, a)
            px(im, x + 1, y, c, a)


def highlight():
    """EL CONTORNO DEL RATÓN: la silueta en blanco cálido."""
    im = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    for y in range(H):
        for x in range(W):
            if dentro(x, y) and (
                    not dentro(x - 1, y) or not dentro(x + 1, y) or
                    not dentro(x, y - 1) or not dentro(x, y + 1)):
                im.putpixel((x, y), (255, 244, 214, 255))
    return im


def generar():
    base = os.path.join(os.path.dirname(__file__), '..', 'AethonMod')
    hoja = Image.new('RGBA', (W, H * MARCOOS), (0, 0, 0, 0))
    for f in range(MARCOOS):
        # LAS CAPAS, en orden de la casa (cada una pinta LIBRE y se
        # compone encima: la piedra < EL OJO < las lágrimas < la ceja
        # < el pedestal — NADA se come a NADIE).
        marco = Image.new('RGBA', (W, H), (0, 0, 0, 0))
        for fn in (cuerpo_piedra, el_ojo_f, las_lagrimas_f,
                   la_ceja_f, el_pedestal_f):
            capa = Image.new('RGBA', (W, H), (0, 0, 0, 0))
            fn(capa, f)
            marco = Image.alpha_composite(marco, capa)
        hoja.paste(marco, (0, f * H))
    hoja.save(os.path.join(base, 'Content', 'Tiles', 'AltarOfrendasTile.png'))
    marco0 = hoja.crop((0, 0, W, H))
    marco0.save(os.path.join(base, 'Content', 'Items', 'Altar', 'AltarOfrendasItem.png'))
    highlight().save(os.path.join(base, 'Content', 'Tiles',
                                  'AltarOfrendasTile_Highlight.png'))

    # --- LA VERIFICACIÓN DE LA CASA ---
    im = Image.open(os.path.join(base, 'Content', 'Tiles', 'AltarOfrendasTile.png')).convert('RGBA')
    assert im.size == (W, H * MARCOOS), f'tamaño roto: {im.size}'
    pxs = im.load()
    for f in range(MARCOOS):
        ultima = -1
        for y in range(H):
            for x in range(W):
                if pxs[x, y + f * H][3] > 10:
                    ultima = y
        assert ultima == H - 1, f'¡el marco {f} flota (fila {ultima})!'
    print(f'EL ALTAR DEL OJO: {im.size[0]}x{im.size[1]} · 6 marcos · arte hasta la última fila')

    # LA HOJA DE CONTACTO para EL OJO (QA VLM).
    zoom = 6
    hoja_qa = Image.new('RGBA', (W * zoom * 3 + 20, H * zoom * 2 + 10), (18, 16, 26, 255))
    for i in range(6):
        fr = im.crop((0, i * H, W, (i + 1) * H))
        fr = fr.resize((W * zoom, H * zoom), Image.NEAREST)
        hoja_qa.paste(fr, ((i % 3) * (W * zoom + 10), (i // 3) * (H * zoom + 5)))
    hoja_qa.save('/tmp/altar_v6111_qa.png')
    print('hoja de contacto: /tmp/altar_v6111_qa.png')


if __name__ == '__main__':
    generar()
