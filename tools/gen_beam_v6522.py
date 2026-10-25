#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_beam_v6522.py — v6.52.2 — LA TEXTURA DEL HAZ DE VERDAD.

La investigación del ciclo (Hunt of the Old God / FusionRay + Calamity
Overhaul / AriaGammaRay, leídas de la fuente): la textura canónica de un
haz es un RUIDO DE NUBES con la CAÍDA GAUSSIANA HORIZONTAL HORNEADA en el
eje V (0 en los bordes → 255 al centro) y ruido ELONGADO a lo largo de U,
MUESTREADA CON WRAP. La RuidoTinta de la .52.0 fallaba en dos cosas:
(1) su borde V no moría en 0 (56/255 — un borde duro fantasma), y
(2) su ruido NO era tileable en U (costura cada 256 px — «cuadrados
discontinuos», la letra del usuario).

ESTA V2: cero en los bordes V, fBm TILEABLE en U (períodos enteros por
octava), blanco-cálido para que el TINTE llegue limpio del vértice.
"""
import math
import struct
import zlib

W, H = 256, 64

def _hash(xi, yi, semilla):
    """El hash determinista de la casa (el mismo espíritu de VFXCore.Hash01)."""
    h = (xi * 374761393 + yi * 668265263 + semilla * 2246822519) & 0xFFFFFFFF
    h = (h ^ (h >> 13)) * 1274126177 & 0xFFFFFFFF
    h = h ^ (h >> 16)
    return (h & 0xFFFFFF) / 0xFFFFFF

def _vnoise_tileable(x, y, periodo, semilla):
    """Value-noise con retícula que ENVUELVE en X (períodos ENTEROS = tileable)."""
    x0 = math.floor(x); fx = x - x0
    y0 = math.floor(y); fy = y - y0
    fx = fx * fx * (3 - 2 * fx)
    fy = fy * fy * (3 - 2 * fy)
    xa, xb = x0 % periodo, (x0 + 1) % periodo
    ya, yb = y0, y0 + 1
    a = _hash(xa, ya, semilla); b = _hash(xb, ya, semilla)
    c = _hash(xa, yb, semilla); d = _hash(xb, yb, semilla)
    return a + (b - a) * fx + (c - a) * fy + (a - b - c + d) * fx * fy

def generar():
    img = []
    # LAS OCTAVAS: períodos ENTEROS en U (3, 6, 13, 27) → tileable exacto;
    # la V vive libre (sus bordes mueren a cero por la gaussiana).
    octavas = [(3, 2.2, 11), (6, 1.9, 131), (13, 1.6, 257), (27, 1.3, 397)]
    for y in range(H):
        v = y / (H - 1)
        # LA CAÍDA GAUSSIANA del eje V — la que muere en CERO en los bordes.
        gauss = math.exp(-((v - 0.5) ** 2) / (2 * 0.165 ** 2))
        nucleo = math.exp(-((v - 0.5) ** 2) / (2 * 0.052 ** 2))
        fila = []
        for x in range(W):
            u = x / W
            # EL fBm ELONGADO (x comprimido ×2.6 — franjas largas de nube).
            f = 0.0; total = 0.0
            for periodo, amp, semilla in octavas:
                f += amp * _vnoise_tileable(u * periodo * 2.6 / 2.6, v * 7.0, periodo, semilla)
                total += amp
            f /= total
            # LA CARNE: gauss·(0.35+0.65·f) — a los bordes V la gauss MUERE →
            # cero exacto; + el NÚCLEO vivo (la vena central de plasma).
            brillo = gauss * (0.35 + 0.65 * f) + 0.55 * nucleo * f
            brillo = max(0.0, min(1.0, brillo))
            # BLANCO-CÁLIDO (el tinte lo pone el color del vértice).
            r = int(255 * brillo)
            g = int(250 * brillo)
            b = int(244 * brillo)
            fila.append((r, g, b, 255))
        img.append(fila)
    return img

def png(path, img):
    raw = b""
    for fila in img:
        raw += b"\x00" + b"".join(struct.pack("4B", *px) for px in fila)
    def chunk(tipo, datos):
        c = tipo + datos
        return struct.pack(">I", len(datos)) + c + struct.pack(">I", zlib.crc32(c) & 0xFFFFFFFF)
    header = struct.pack(">8B", 137, 80, 78, 71, 13, 10, 26, 10)
    ihdr = struct.pack(">IIBBBBB", W, H, 8, 6, 0, 0, 0)
    data = header + chunk(b"IHDR", ihdr) + chunk(b"IDAT", zlib.compress(raw, 9)) + chunk(b"IEND", b"")
    with open(path, "wb") as f:
        f.write(data)
    print("OK", path, f"({W}x{H}, {len(data)} B)")

if __name__ == "__main__":
    import os
    raiz = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    png(os.path.join(raiz, "AethonMod/Content/Effects/Procedural/RuidoTinta.png"), generar())
    print("RuidoTinta V2 GENERADA — borde cero + tileable en U")
