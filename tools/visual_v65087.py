#!/usr/bin/env python3
"""
v6.50.87 — PREVIEW VISUAL del glitch y del temblor (QA).
Reproduce en Python EXACTAMENTE la matemática del C# (Hash portado con
aritmética uint32) para dibujar ráfagas REALES del Errático y offsets
REALES del Tembloroso sobre el arte del paquete (rawimg decodificado),
6× con vecino más próximo. Paneles:
  1. NORMAL (línea de base — el libro aprobado)
  2. ERRÁTICO limpio (sin ráfaga — idéntico al normal: el 96 % del tiempo)
  3. ERRÁTICO ráfaga frame A (h=1.0, 1-2 tiras)
  4. ERRÁTICO ráfaga frame B (3 frames después — el tearing se regeneró)
  5. TEMBLOROSO h=0.30 (un susurro)
  6. TEMBLOROSO h=1.00 (el temblor del apetito total)
"""
import struct
import sys
import zlib

sys.path.insert(0, 'tools')
from parse_tmod import parse  # noqa: E402

TMOD = '/home/z/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod'
SALIDA = '/tmp/preview_v65087.png'

MASK = 0xFFFFFFFF


def u32(x):
    return x & MASK


def Hash(x):
    x = u32(x)
    x = u32(x ^ (x >> 16)); x = u32(x * 0x7FEB352D)
    x = u32(x ^ (x >> 15)); x = u32(x * 0x846CA68B)
    x = u32(x ^ (x >> 13))
    return x


# === EL LAYOUT del C#, portado (GrimorioHambrientoErratico.Layout) ===
def layout(semilla, tick, h):
    fase = (tick // 3)
    sd = Hash(u32(semilla) ^ u32(fase * 0x9E3779B9))
    n = 1 + Hash(sd) % 2
    tiras = []
    y = 3 + Hash(sd ^ 0xA5A5) % 34
    hechos = 0
    while hechos < n and y <= 41:
        alto = 4 + Hash(sd ^ (hechos * 97 + 1)) % 8
        off = (Hash(sd ^ (hechos * 131 + 3)) % 7 - 3) * (0.45 + 0.55 * h)
        y1 = min(46, y + alto)
        tiras.append((y, y1, off))
        hechos += 1
        y = y1 + 2 + Hash(sd ^ (hechos * 17 + 5)) % 6
    off_ojo = 0.0
    for (a, b, o) in tiras:
        if a <= 22.45 < b:
            off_ojo = o
            break
    return tiras, off_ojo


# === EL TEMBLOR del C#, portado (GrimorioHambrientoTembloroso.Temblor) ===
import math
TWO_PI = math.tau


def temblor(t, h):
    amp = 0.20 + 1.10 * h
    x = math.sin(t * 9.3 * TWO_PI) * 0.80 + math.sin(t * 17.3 * TWO_PI) * 0.25
    y = math.sin(t * 11.9 * TWO_PI + 1.7) * 0.80 + math.sin(t * 19.1 * TWO_PI + 0.6) * 0.25
    return (x * amp, y * amp)


def buscar_pico(h, abajo=False):
    # el tick (0-600, 10 s) con el desplazamiento MÁS grande (o más quieto)
    mejor, tmejor = -1.0, 0
    for t in range(0, 600):
        dx, dy = temblor(t / 60.0, h)
        m = math.hypot(dx, dy)
        if (m > mejor) if not abajo else (m < 1e9 and m < mejor if abajo else m > mejor):
            if abajo and m > 0.05:
                continue
            mejor, tmejor = m, t
    return tmejor


def main():
    from PIL import Image, ImageDraw

    d = open(TMOD, 'rb').read()
    _, _, _, entries, tend, _ = parse(TMOD)
    blobs = {}
    off = tend
    for p, raw, comp in entries:
        data = d[off:off + comp]
        off += comp
        blobs[p] = data if raw == comp else zlib.decompress(data, -15)

    def rgba(sufijo):
        for p, b in blobs.items():
            if p.endswith(sufijo):
                v, w, h = struct.unpack('<III', b[:12])
                import numpy as np
                a = np.frombuffer(b[12:12 + w * h * 4], dtype=np.uint8).reshape(h, w, 4)
                return Image.fromarray(a, 'RGBA')
        raise KeyError(sufijo)

    base = rgba('GrimorioHambriento.rawimg')
    iris = rgba('GrimorioHambriento_Iris.rawimg')

    # === el compuesto ABIERTO: base + iris a media mirada (desp (2.0, 1.5)) ===
    comp = base.copy()
    # el iris es 32×32 dibujado a escala IRIS_ESC 0.2773 → 8.87 px; su centro
    # va a OJO+desp = (21.09, 23.95); a 6× el disco mide 8.87*6 ≈ 53 px
    ESC = 6
    iris6 = iris.resize((max(1, round(32 * 0.2773 * ESC)),) * 2, Image.NEAREST)
    cx, cy = (19.09 + 2.0) * ESC, (22.45 + 1.5) * ESC
    comp6 = base.resize((36 * ESC, 49 * ESC), Image.NEAREST)
    comp6.paste(iris6, (round(cx - iris6.width / 2), round(cy - iris6.height / 2)), iris6)

    W, H = 36 * ESC, 49 * ESC
    GAP = 14
    LBL = 18
    panel_w, panel_h = W, H + LBL
    cols, rows = 3, 2
    lienzo = Image.new('RGBA', (cols * panel_w + GAP * (cols + 1),
                                rows * panel_h + GAP * (rows + 1)), (36, 34, 40, 255))
    draw = ImageDraw.Draw(lienzo)

    def pegar(img, col, row, titulo):
        x = GAP + col * (panel_w + GAP)
        y = GAP + row * (panel_h + GAP)
        lienzo.paste(img, (x, y + LBL))
        draw.text((x + 4, y + 2), titulo, fill=(235, 230, 220, 255))

    # 1. NORMAL (línea de base)
    pegar(comp6, 0, 0, '1 NORMAL (base .87)')

    # 2. ERRÁTICO limpio (sin ráfaga — idéntico al normal)
    pegar(comp6, 1, 0, '2 ERRATICO limpio (sin rafaga)')

    # 3-4. ERRÁTICO en ráfaga: layouts REALES (h=1.0, semilla de demo)
    semilla = 987654321
    for i, tick in enumerate((1200, 1209)):   # 3 frames de diferencia: fase distinta
        tiras, off_ojo = layout(semilla, tick, 1.0)
        img = comp6.copy()
        for (a, b, o) in tiras:
            caja = (0, a * ESC, W, b * ESC)
            franja = img.crop((0, a * ESC, W, b * ESC))
            img.paste(franja, (round(o * ESC), a * ESC))
        # el iris viaja con su tira
        dx = round(off_ojo * ESC)
        if dx:
            iris_caja = (round(cx - iris6.width / 2) - 6, round(cy - iris6.height / 2) - 6,
                         round(cx + iris6.width / 2) + 6, round(cy + iris6.height / 2) + 6)
            parche = img.crop(iris_caja)
            img.paste(parche, (iris_caja[0] + dx, iris_caja[1]))
        pegar(img, 2 + i, 0, f'{3 + i} ERRATICO rafaga {"AB"[i]} (h=1.0, {len(tiras)} tiras)')

    # 5-6. TEMBLOROSO: offsets REALES
    for i, h in enumerate((0.30, 1.00)):
        # pico de desplazamiento en 10 s de simulación
        mejor_t, mejor_m = 0, -1.0
        for t in range(600):
            dx, dy = temblor(t / 60.0, h)
            m = math.hypot(dx, dy)
            if m > mejor_m:
                mejor_m, mejor_t = m, t
        dx, dy = temblor(mejor_t / 60.0, h)
        img = comp6.copy()
        caja = (0, 0, W, H)
        lienzo2 = Image.new('RGBA', (W + 40, H + 40), (0, 0, 0, 0))
        parche = img.crop(caja)
        lienzo2.paste(parche, (20 + round(dx * ESC), 20 + round(dy * ESC)))
        # recortar al tamaño del panel (el libro se salió ±: se ve el borde)
        img = lienzo2.crop((20, 20, 20 + W, 20 + H))
        pegar(img, i, 1, f'{5 + i} TEMBLOROSO h={h:.2f} (Δ {dx:.2f},{dy:.2f} px)')

    lienzo.save(SALIDA)
    print(f'preview: {SALIDA} {lienzo.size}')
    # métricas de suavidad
    for h in (0.3, 1.0):
        mags = [math.hypot(*temblor(t / 60.0, h)) for t in range(600)]
        print(f'  temblor h={h}: pico {max(mags):.2f} px · media {sum(mags)/600:.2f} px')
    offs = []
    for s in range(50):
        t, _ = layout(1000 + s * 7919, 3000 + s * 31, 1.0)
        offs += [abs(o) for (_, _, o) in t]
    print(f'  glitch h=1.0: |off| tiras max {max(offs):.2f} px · media {sum(offs)/len(offs):.2f} px')


if __name__ == '__main__':
    main()
