#!/usr/bin/env python3
"""
v6.50.88 — PREVIEW VISUAL del glitch INTENSIFICADO (QA).
El usuario: «el efecto glish es muy suave, aumente el efecto glish al
menos un 70 %». Reproduce en Python EXACTAMENTE la matemática del C#
(Hash portado con aritmética uint32) para dibujar ráfagas REALES del
Errático .88 sobre el arte del paquete (rawimg decodificado), 6× con
vecino más próximo, y MIDE el aumento contra la matemática de la .87:
  desfase:  ±1,3 → ±3 px  →  ±3,3 → ±6 px   (media |off| a h=1: +88 %)
  tiras:    1-2           →  2-4            (+100 %)
  ráfaga:   7-14 ticks    →  12-24          (+71 %)
  regen:    cada 3 frames →  cada 2         (+50 %)
Paneles:
  1. NORMAL (línea de base — el libro aprobado)
  2. ERRÁTICO limpio (sin ráfaga — idéntico al normal)
  3. ERRÁTICO ráfaga frame A (h=1.0)
  4. ERRÁTICO ráfaga frame B (2 frames después — el tearing se regeneró)
  5. ERRÁTICO ráfaga h=0.35 (el glitch ya se nota con hambre moderada)
  6. TEMBLOROSO h=1.00 (SIN CAMBIOS — referencia de la .87)
"""
import math
import struct
import sys
import zlib

sys.path.insert(0, 'tools')
from parse_tmod import parse  # noqa: E402

TMOD = '/home/z/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod'
SALIDA = '/tmp/preview_v65088.png'

MASK = 0xFFFFFFFF


def u32(x):
    return x & MASK


def Hash(x):
    x = u32(x)
    x = u32(x ^ (x >> 16)); x = u32(x * 0x7FEB352D)
    x = u32(x ^ (x >> 15)); x = u32(x * 0x846CA68B)
    x = u32(x ^ (x >> 13))
    return x


# === EL LAYOUT .88 del C#, portado (GrimorioHambrientoErratico.Layout) ===
def layout88(semilla, tick, h):
    fase = (tick // 2)                                   # regen cada 2 frames
    sd = Hash(u32(semilla) ^ u32(fase * 0x9E3779B9))
    n = 2 + Hash(sd) % 3                                 # 2-4 tiras
    tiras = []
    y = 3 + Hash(sd ^ 0xA5A5) % 22                       # nace en y=3..24
    hechos = 0
    while hechos < n and y <= 41:
        alto = 4 + Hash(sd ^ (hechos * 97 + 1)) % 9      # 4-12 px
        off = (Hash(sd ^ (hechos * 131 + 3)) % 13 - 6) * (0.55 + 0.45 * h)
        y1 = min(46, y + alto)
        tiras.append((y, y1, off))
        hechos += 1
        y = y1 + 1 + Hash(sd ^ (hechos * 17 + 5)) % 5
    off_ojo = 0.0
    for (a, b, o) in tiras:
        if a <= 22.45 < b:
            off_ojo = o
            break
    return tiras, off_ojo


# === EL LAYOUT .87 (para la comparación de métricas) ===
def layout87(semilla, tick, h):
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


# === EL TEMBLOR del C# (.87 intacto — referencia) ===
TWO_PI = math.tau


def temblor(t, h):
    amp = 0.20 + 1.10 * h
    x = math.sin(t * 9.3 * TWO_PI) * 0.80 + math.sin(t * 17.3 * TWO_PI) * 0.25
    y = math.sin(t * 11.9 * TWO_PI + 1.7) * 0.80 + math.sin(t * 19.1 * TWO_PI + 0.6) * 0.25
    return (x * amp, y * amp)


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
    pegar(comp6, 0, 0, '1 NORMAL (base — sin cambios)')

    # 2. ERRÁTICO limpio (sin ráfaga — idéntico al normal)
    pegar(comp6, 1, 0, '2 ERRATICO limpio (sin rafaga)')

    # 3-4. ERRÁTICO en ráfaga h=1.0: layouts REALES (2 frames de diferencia)
    semilla = 987654321
    for i, tick in enumerate((1200, 1202)):   # regen cada 2 frames: fase distinta
        tiras, off_ojo = layout88(semilla, tick, 1.0)
        img = comp6.copy()
        for (a, b, o) in tiras:
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

    # 5. ERRÁTICO ráfaga con hambre MODERADA (h=0.35)
    tiras, off_ojo = layout88(semilla, 3600, 0.35)
    img = comp6.copy()
    for (a, b, o) in tiras:
        franja = img.crop((0, a * ESC, W, b * ESC))
        img.paste(franja, (round(o * ESC), a * ESC))
    dx = round(off_ojo * ESC)
    if dx:
        iris_caja = (round(cx - iris6.width / 2) - 6, round(cy - iris6.height / 2) - 6,
                     round(cx + iris6.width / 2) + 6, round(cy + iris6.height / 2) + 6)
        parche = img.crop(iris_caja)
        img.paste(parche, (iris_caja[0] + dx, iris_caja[1]))
    pegar(img, 0, 1, f'5 ERRATICO rafaga (h=0.35, {len(tiras)} tiras)')

    # 6. TEMBLOROSO h=1.00 (SIN CAMBIOS — referencia .87)
    mejor_t, mejor_m = 0, -1.0
    for t in range(600):
        dx, dy = temblor(t / 60.0, 1.0)
        m = math.hypot(dx, dy)
        if m > mejor_m:
            mejor_m, mejor_t = m, t
    dx, dy = temblor(mejor_t / 60.0, 1.0)
    img = comp6.copy()
    lienzo2 = Image.new('RGBA', (W + 40, H + 40), (0, 0, 0, 0))
    lienzo2.paste(img.crop((0, 0, W, H)), (20 + round(dx * ESC), 20 + round(dy * ESC)))
    img = lienzo2.crop((20, 20, 20 + W, 20 + H))
    pegar(img, 1, 1, f'6 TEMBLOROSO h=1.00 (SIN CAMBIOS)')

    # panel 7 (hueco libre): la FIRMA del +70 % — .87 vs .88 lado a lado
    t87, _ = layout87(semilla, 1200, 1.0)
    img87 = comp6.copy()
    for (a, b, o) in t87:
        franja = img87.crop((0, a * ESC, W, b * ESC))
        img87.paste(franja, (round(o * ESC), a * ESC))
    pegar(img87, 2, 1, '7 GLITCH .87 (antes — para comparar)')

    lienzo.save(SALIDA)
    print(f'preview: {SALIDA} {lienzo.size}')

    # === MÉTRICAS: .87 vs .88 — la prueba del +70 % ===
    print('=== EL GLITCH: .87 vs .88 (misma semilla, 500 ráfagas) ===')
    for h in (0.12, 0.35, 0.5, 1.0):
        for nombre, fn in (('  .87', layout87), ('  .88', layout88)):
            offs, ntiras = [], []
            for s in range(500):
                t, _ = fn(1000 + s * 7919, 3000 + s * 31, h)
                offs += [abs(o) for (_, _, o) in t]
                ntiras.append(len(t))
            print(f'  h={h:.2f} {nombre}: |off| max {max(offs):.2f} px · '
                  f'media {sum(offs)/len(offs):.2f} px · tiras media '
                  f'{sum(ntiras)/len(ntiras):.2f}')
    # el VEREDICTO
    o87, o88, t87c, t88c = [], [], [], []
    for s in range(500):
        a, _ = layout87(1000 + s * 7919, 3000 + s * 31, 1.0)
        b, _ = layout88(1000 + s * 7919, 3000 + s * 31, 1.0)
        o87 += [abs(o) for (_, _, o) in a]
        o88 += [abs(o) for (_, _, o) in b]
        t87c.append(len(a))
        t88c.append(len(b))
    m87, m88 = sum(o87) / len(o87), sum(o88) / len(o88)
    print(f'=== VEREDICTO h=1.0: media |off| {m87:.2f} → {m88:.2f} px '
          f'(+{(m88/m87-1)*100:.0f} %) · máx {max(o87):.2f} → {max(o88):.2f} px '
          f'(+{(max(o88)/max(o87)-1)*100:.0f} %) · tiras '
          f'{sum(t87c)/len(t87c):.2f} → {sum(t88c)/len(t88c):.2f} '
          f'(+{(sum(t88c)/len(t88c))/(sum(t87c)/len(t87c))*100-100:.0f} %) ===')
    # presencia: ráfaga 12-24 (media 18) cada Lerp(15,2.1,h) s ±25 %
    for h in (0.35, 1.0):
        p87 = 10.5 / (60 * (22 + (3.2 - 22) * h) * 1.0 + 10.5)
        p88 = 18.0 / (60 * (15 + (2.1 - 15) * h) * 1.0 + 18.0)
        print(f'  presencia h={h:.2f}: {p87*100:.1f} % → {p88*100:.1f} % '
              f'(×{p88/p87:.2f})')


if __name__ == '__main__':
    main()
