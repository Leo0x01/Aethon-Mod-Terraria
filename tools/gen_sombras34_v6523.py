#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_sombras34_v6523.py — v6.52.3 — LAS TEXTURAS QUE FALTABAN (v2 PIXEL-ART DURO).

La letra del usuario: «hay nuevos errores» + client.log nuevo → LA CULPA:
v6.52.2 NO CARGÓ — SombraDeLaPagina3/4 sin su .png → MissingResourceException
→ tML DESACTIVÓ el mod entero (lección v6.50.99 reincidida: ni el -build ni
el headless validan texturas de cliente — SOLO el audit_texturas.py las caza).

v2 tras la crítica del VLM (III: 6/10 «needs redraw», IV: 7.5/10 «minor polish»):
  · PIXEL-ART DURO: cada píxel entero, paleta cuantizada de 4 tonos por zona,
    SIN anti-aliasing, SIN degradados suaves, SIN resize (se dibuja 1:1 a 38×38).
  · III: los 7 iris se leen POR SEPARADO (3 grandes + 4 chicos, abanico con
    hueco), pupila negra 2-3px, especular 1px, zarcillo con brillos 1px
    blancos (la firma de Sombra1) y punta roja saturada.
  · IV: bandas nítidas de luz (4 tonos), contorno 1px oscuro, fibras radiales
    1px, pupila dilatada con reborde, nervios ópticos de 2px→1px duros con
    goterones cuadrados y especular secundario 1px.

La paleta ES la de la familia (muestreada de GrimorioHambriento_Iris*.png):
  dorado: #a7562f / #d0954e / #917243 — rojo: #a73448 / #d05164 / #91414e
  pupila #120a13 — tinta #101018 — luz #1c1c26
"""
import math
import os

import numpy as np
from PIL import Image, ImageDraw

DEST = os.path.join(os.path.dirname(os.path.abspath(__file__)),
                    '..', 'AethonMod', 'Content', 'Weapons', 'Sombras')
W = H = 38

# --- LA PALETA DE LA FAMILIA ---
GOLD_RING = (0xa7, 0x56, 0x2f, 255)
GOLD_BODY = (0xd0, 0x95, 0x4e, 255)
GOLD_SHADE = (0x91, 0x72, 0x43, 255)
GOLD_HI = (0xe8, 0xb4, 0x6e, 255)
RED_RING = (0xa7, 0x34, 0x48, 255)
RED_BODY = (0xd0, 0x51, 0x64, 255)
RED_HI = (0xe0, 0x6a, 0x80, 255)
RED_SHADE = (0x9141, 0x41, 0x4e, 255) if False else (0x91, 0x41, 0x4e, 255)
RED_DEEP = (0x4a, 0x18, 0x22, 255)
PUPILA = (0x12, 0x0a, 0x13, 255)
PUPILA_RIM = (0x2a, 0x10, 0x18, 255)
TINTA = (0x10, 0x10, 0x18, 255)
TINTA_LUZ = (0x2a, 0x2a, 0x36, 255)
BLANCO = (255, 255, 255, 255)


class Lienzo:
    """Pixel-art duro: entero o nada, sin AA."""

    def __init__(self):
        self.px = np.zeros((H, W, 4), dtype=np.uint8)

    def poner(self, x, y, col):
        if 0 <= int(x) < W and 0 <= int(y) < H:
            self.px[int(y), int(x)] = col

    def disco(self, cx, cy, r, col, borde=None):
        """Disco duro por distancia (corte limpio, sin pluma)."""
        x0, x1 = int(cx - r) - 1, int(cx + r) + 2
        y0, y1 = int(cy - r) - 1, int(cy + r) + 2
        for y in range(max(0, y0), min(H, y1)):
            for x in range(max(0, x0), min(W, x1)):
                d = math.hypot(x - cx, y - cy)
                if borde is not None and d <= r:
                    self.px[y, x] = borde if d > r - 1.0 else col
                elif d <= r:
                    self.px[y, x] = col

    def anillo_banda(self, cx, cy, r, col, desde):
        """Banda anular dura: pinta donde desde < d <= r."""
        x0, x1 = int(cx - r) - 1, int(cx + r) + 2
        y0, y1 = int(cy - r) - 1, int(cy + r) + 2
        for y in range(max(0, y0), min(H, y1)):
            for x in range(max(0, x0), min(W, x1)):
                d = math.hypot(x - cx, y - cy)
                if desde < d <= r:
                    self.px[y, x] = col

    def linea_gorda(self, a, b, grosor, col):
        """Línea de píxeles enteros (grosor en px, sin AA)."""
        n = max(2, int(math.hypot(b[0] - a[0], b[1] - a[1])) * 2)
        for i in range(n + 1):
            t = i / n
            x = a[0] + (b[0] - a[0]) * t
            y = a[1] + (b[1] - a[1]) * t
            if grosor <= 1:
                self.poner(round(x), round(y), col)
            else:
                r = (grosor - 1) / 2
                for dy in range(-int(math.ceil(r)), int(math.ceil(r)) + 1):
                    for dx in range(-int(math.ceil(r)), int(math.ceil(r)) + 1):
                        if dx * dx + dy * dy <= r * r + 0.1:
                            self.poner(round(x) + dx, round(y) + dy, col)

    def guardar(self, path):
        Image.fromarray(self.px, 'RGBA').save(path)
        return path


def bezier(p0, p1, p2, p3, n):
    pts = []
    for i in range(n + 1):
        t = i / n
        x = ((1 - t) ** 3 * p0[0] + 3 * (1 - t) ** 2 * t * p1[0]
             + 3 * (1 - t) * t ** 2 * p2[0] + t ** 3 * p3[0])
        y = ((1 - t) ** 3 * p0[1] + 3 * (1 - t) ** 2 * t * p1[1]
             + 3 * (1 - t) * t ** 2 * p2[1] + t ** 3 * p3[1])
        pts.append((x, y))
    return pts


def gen_sombra3():
    """EL CORO DE IRISES — zarcillo + 7 iris dorados (v3: ojos TODOS sobre
    el zarcillo, jerarquía 3 grandes + 4 chicos, dorado de ALTO contraste)."""
    L = Lienzo()

    # --- EL ZARCILLO (la firma de Sombra1): más grueso, protagonista ---
    p0, p1, p2, p3 = (3.0, 35.0), (13.0, 31.0), (27.0, 19.0), (35.5, 5.0)
    pts = bezier(p0, p1, p2, p3, 60)
    for i in range(len(pts) - 1):
        t = i / (len(pts) - 1)
        grosor = 4 if t < 0.25 else 3 if t < 0.7 else 2
        L.linea_gorda(pts[i], pts[i + 1], grosor, TINTA)
    # NÓDULOS/articulaciones entre racimos de ojos (el fix del VLM: movimiento orgánico)
    for t in (0.21, 0.545, 0.84):
        x, y = pts[int(t * (len(pts) - 1))]
        L.disco(x, y, 1.7, TINTA)
    # brillos 1px blancos sobre el lomo (los 3 de Sombra1)
    for t in (0.20, 0.45, 0.68):
        x, y = pts[int(t * (len(pts) - 1))]
        L.poner(round(x), round(y) - 2, BLANCO)
        L.poner(round(x) + 1, round(y) - 2, BLANCO)
    # LA PUNTA ROJA saturada (3px, pop contra el dorado)
    tx, ty = pts[-1]
    L.disco(tx - 0.5, ty - 0.5, 1.8, (0xd0, 0x30, 0x50, 255))
    L.poner(round(tx) - 1, round(ty) - 1, (0xff, 0x50, 0x70, 255))

    # --- EL CORO: SIETE iris TODOS ARRIBA del zarcillo, arco limpio ---
    # 3 GRANDES (r=4.2 → 8-9px, se leen solos) + 4 chicos (r=2.6)
    coro = [  # (t sobre curva, elevación sobre el lomo, radio)
        (0.06, 4.6, 2.6), (0.15, 5.6, 2.6),        # chicos de apertura
        (0.28, 6.2, 4.2), (0.46, 5.4, 4.4),        # GRANDES (el centro, el mayor)
        (0.63, 6.2, 4.2),
        (0.77, 5.4, 2.6), (0.91, 4.4, 2.6),        # chicos de cierre
    ]
    for t, ele, r in coro:
        i = int(t * (len(pts) - 1))
        a, b = pts[max(0, i - 1)], pts[min(len(pts) - 1, i + 1)]
        dx, dy = b[0] - a[0], b[1] - a[1]
        Ln = math.hypot(dx, dy) or 1.0
        nx, ny = -dy / Ln, dx / Ln
        if ny > 0: nx, ny = -nx, -ny                # normal SIEMPRE hacia arriba
        cx = pts[i][0] + nx * ele
        cy = pts[i][1] + ny * ele
        # OJO DURO de alto contraste: anillo → cuerpo BRILLANTE → sombra → pupila NEGRA → especular BLANCO
        L.disco(cx, cy, r, GOLD_RING)
        L.disco(cx, cy, r - 1.0, GOLD_HI)
        # media luna inferior con el tono medio + sombra (2 bandas duras)
        rr = r - 1.0
        for y in range(int(cy - rr) - 1, int(cy + rr) + 2):
            for x in range(int(cx - rr) - 1, int(cx + rr) + 2):
                d = math.hypot(x - cx, y - cy)
                dy45 = (x - cx) + (y - cy)          # diagonal abajo-derecha
                if d <= rr and dy45 > rr * 0.9:
                    L.poner(x, y, GOLD_SHADE)
                elif d <= rr and dy45 > rr * 0.35:
                    L.poner(x, y, GOLD_BODY)
        # pupila NEGRO PURO contundente (≥2px en los grandes)
        rp = max(1.2, r * 0.45)
        L.disco(cx, cy, rp, (0x00, 0x00, 0x00, 255))
        # especular 1px BLANCO PURO a 0.6r — SIEMPRE sobre el oro, SIEMPRE arriba-izq
        # (el fix del VLM: alineación consistente + que no caiga en la pupila)
        L.poner(round(cx - r * 0.60), round(cy - r * 0.60), BLANCO)

    return L.guardar(os.path.join(DEST, 'SombraDeLaPagina3.png'))


def gen_sombra4():
    """EL IRIS COLOSAL — v3: LA PUPILA YA NO SE COME EL IRIS (r 37%),
    especulares SOBRE la banda roja, nervios 3px→2px con goterones 3px."""
    L = Lienzo()
    cx, cy, R = 19.0, 12.5, 12.2

    # --- LOS DOS NERVIOS ÓPTICOS (3px→2px, goterones 3px) ---
    def nervio(q0, q1, q2):
        n = 22
        pts = []
        for i in range(n + 1):
            t = i / n
            x = (1 - t) ** 2 * q0[0] + 2 * (1 - t) * t * q1[0] + t ** 2 * q2[0]
            y = (1 - t) ** 2 * q0[1] + 2 * (1 - t) * t * q1[1] + t ** 2 * q2[1]
            pts.append((x, y))
        for i in range(len(pts) - 1):
            t = i / (len(pts) - 1)
            grosor = 3 if t < 0.4 else 2
            L.linea_gorda(pts[i], pts[i + 1], grosor, TINTA)
            if i in (4, 10):  # nódulos del nervio
                x, y = pts[i]
                L.disco(x, y, 1.7, TINTA)
        # goterón 3px + gota desprendida (el fix del VLM: 1px más larga + secundaria)
        x, y = pts[-1]
        L.disco(x, y + 0.5, 1.6, TINTA)
        L.poner(round(x), round(y) + 3, TINTA)
        L.poner(round(x), round(y) + 4, TINTA)
        L.poner(round(x), round(y) + 5, TINTA)   # la gota estirada
        L.poner(round(x) + 1, round(y) + 6, TINTA)  # la secundaria, desplazada

    nervio((15.5, 23.5), (12.0, 29.5), (9.0, 34.0))
    nervio((22.5, 23.5), (26.5, 29.5), (29.5, 34.0))

    # --- EL IRIS COLOSAL: BANDAS NÍTIDAS (luz arriba-izquierda) ---
    lx, ly = -0.58, -0.58
    for y in range(H):
        for x in range(W):
            dx, dy = x - cx, y - cy
            d = math.hypot(dx, dy)
            if d > R:
                continue
            if d > R - 1.0:      # contorno 1px profundo
                L.poner(x, y, RED_DEEP)
                continue
            k = ((dx / d) * lx + (dy / d) * ly) if d > 0.35 else 0.5
            k = k * 0.62 + (1 - d / R) * 0.38
            col = RED_SHADE if k < 0.18 else RED_BODY if k < 0.52 else RED_HI
            L.poner(x, y, col)

    # FIBRAS radiales 1px (12 estrías duras) — SOLO en la banda externa
    for kk in range(12):
        ang = kk * math.pi / 6 + 0.26
        r0, r1 = R * 0.58, R * 0.84
        for i in range(int(r1 - r0) + 1):
            rr = r0 + i
            L.poner(round(cx + math.cos(ang) * rr), round(cy + math.sin(ang) * rr), RED_RING)
    # anillo de la familia (2px nítidos)
    L.anillo_banda(cx, cy, R - 1.0, RED_RING, R - 2.6)

    # LA PUPILA DILATADA PERO NO CIEGA: 37% de R (el iris ES la boca,
    # pero el ANILLO ROJO debe LEERSE — la lección del dump ASCII)
    rp = R * 0.37
    L.disco(cx, cy, rp, (0x0a, 0x05, 0x0c, 255), borde=PUPILA_RIM)
    # MEDIA LUNA rojo-tinta dentro de la pupila, borde inferior-derecho
    # (el fix del VLM: profundidad — que no sea un vacío plano)
    for y in range(H):
        for x in range(W):
            dx, dy = x - cx, y - cy
            d = math.hypot(dx, dy)
            if d < rp - 0.6 and (dx + dy) > rp * 0.55:
                L.poner(x, y, (0x1e, 0x0c, 0x14, 255))

    # ESPECULARES SOBRE LA BANDA ROJA (a 0.66R — ya no dentro de la pupila)
    ang = math.radians(135)
    ex, ey = cx + math.cos(ang) * R * 0.66, cy + math.sin(ang) * R * 0.66
    L.poner(round(ex), round(ey), BLANCO)
    L.poner(round(ex) + 1, round(ey), BLANCO)
    L.poner(round(ex), round(ey) + 1, (255, 244, 248, 255))
    # secundario abajo-derecha (el brillo húmedo)
    L.poner(round(cx + R * 0.52), round(cy + R * 0.50), (255, 235, 240, 255))

    # ANCLAJES (AL FINAL, sobre el globo ya pintado): carne rojo-oscura donde
    # los nervios nacen — el fix del VLM: ENRAIZADOS, no pegados
    for ax, ay in ((15.5, 23.8), (22.5, 23.8)):
        L.disco(ax, ay, 2.0, RED_DEEP)
        L.disco(ax, ay + 0.8, 1.2, TINTA)

    return L.guardar(os.path.join(DEST, 'SombraDeLaPagina4.png'))


def main():
    os.makedirs(DEST, exist_ok=True)
    p3 = gen_sombra3()
    p4 = gen_sombra4()
    print(f'SombraDeLaPagina3.png  {os.path.getsize(p3)} B')
    print(f'SombraDeLaPagina4.png  {os.path.getsize(p4)} B')

    # --- preview: 4x (lo que el jugador resuelve) + 8x (detalle) — NEAREST ---
    fam = [
        ('I', 'AethonMod/Content/Weapons/Sombras/SombraDeLaPagina.png'),
        ('III', p3), ('IV', p4),
        ('IrisDorado', 'AethonMod/Content/Weapons/GrimorioHambriento_Iris.png'),
        ('IrisRojo', 'AethonMod/Content/Weapons/GrimorioHambriento_IrisRojo.png'),
    ]
    S = 8
    tile = 44 * S
    prev = Image.new('RGB', (tile * len(fam), tile * 2), (255, 255, 255))
    dd = ImageDraw.Draw(prev)
    for i, (n, p) in enumerate(fam):
        im = Image.open(p).convert('RGBA')
        # fila 1: 4x sobre fondo de inventario oscuro (la resolución del jugador)
        dd.rectangle([i * tile, 0, (i + 1) * tile - 1, tile - 1], fill=(48, 40, 56))
        for yy in range(0, tile, 44):
            for xx in range(0, tile, 44):
                if (xx // 44 + yy // 44) % 2 == 0:
                    dd.rectangle([i * tile + xx, yy, i * tile + xx + 43, yy + 43],
                                 fill=(40, 33, 47))
        med = im.resize((im.width * 4, im.height * 4), Image.NEAREST)
        prev.paste(med, (i * tile + (tile - med.width) // 2,
                         (tile - med.height) // 2), med)
        # fila 2: 8x sobre ajedrez (detalle)
        big = im.resize((im.width * S, im.height * S), Image.NEAREST)
        for yy in range(tile, tile * 2, 16):
            for xx in range(0, tile, 16):
                if (xx // 16 + yy // 16) % 2 == 0:
                    dd.rectangle([i * tile + xx, yy, i * tile + xx + 15, yy + 15],
                                 fill=(205, 205, 205))
        prev.paste(big, (i * tile + (tile - big.width) // 2,
                        tile + (tile - big.height) // 2), big)
    pp = os.path.join(os.path.dirname(os.path.abspath(__file__)),
                      'tools', 'preview_sombras34_v6523.png')
    prev.save(pp)
    print(f'preview -> {pp}')


if __name__ == '__main__':
    main()
