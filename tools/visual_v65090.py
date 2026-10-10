#!/usr/bin/env python3
"""
v6.50.90 — PREVIEW VISUAL DEL NERVIOSO (QA).
La letra del usuario: «el tercer libro borralo, y crea otro con las
mismas caracteristicas, temblor, glisheado, y nervioso». Reproduce en
Python EXACTAMENTE la matemática del C# (Hash uint32 portado) y compone
los TRES efectos sobre el arte del paquete (rawimg decodificado):
  · EL TEMBLOR .87 (dos senos por eje, 0,2→1,3 px con el hambre)
  · EL GLITCH .88 (el volumen aprobado: 2-4 tiras, ±3,3→±6 px, regen/2f)
  · EL SOBRESALTO NUEVO (6,5 s→1,8 s con el hambre, brinco 1,2→2,2 px
    que se asienta en 7 ticks; con hambre ≥12 % el susto trae ráfaga
    corta — EL SUSTO ROMPE EL LIBRO)
SIN aura roja — la firma del Inestable no se hereda.
Paneles:
  1. NORMAL (línea de base — el libro aprobado, intacto)
  2. NERVIOSO saciado (h=0,0: temblor 0,2 px — susurro, sin glitch,
     sin susto: apenas un libro inquieto)
  3. NERVIOSO h=0,35 EN SOBRESALTO (el brinco a mitad del asentamiento
     + temblor — el iris en dardo lateral)
  4. NERVIOSO h=1,0 ráfaga frame A (2-4 tiras rotas, ojo al centro —
     la revisada ansiosa)
  5. NERVIOSO h=1,0 ráfaga frame B (2 frames después — el tearing se
     regeneró: OTRA geometría)
  6. NERVIOSO h=1,0 EL SUSTO (brinco entero + ráfaga corta a la vez —
     el momento firma: se asusta, salta Y se rompe)
"""
import math
import struct
import sys
import zlib

sys.path.insert(0, 'tools')
from parse_tmod import parse  # noqa: E402

TMOD = '/home/z/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod'
SALIDA = '/tmp/preview_v65090.png'

MASK = 0xFFFFFFFF


def u32(x):
    return x & MASK


def Hash(x):
    x = u32(x)
    x = u32(x ^ (x >> 16)); x = u32(x * 0x7FEB352D)
    x = u32(x ^ (x >> 15)); x = u32(x * 0x846CA68B)
    x = u32(x ^ (x >> 13))
    return x


# === EL LAYOUT .88 del C#, portado (GrimorioHambrientoNervioso.Layout) ===
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


# === EL TEMBLOR del C# (.87 — heredado del difunto Tembloroso) ===
TWO_PI = math.tau


def temblor(t, h):
    amp = 0.20 + 1.10 * h
    x = math.sin(t * 9.3 * TWO_PI) * 0.80 + math.sin(t * 17.3 * TWO_PI) * 0.25
    y = math.sin(t * 11.9 * TWO_PI + 1.7) * 0.80 + math.sin(t * 19.1 * TWO_PI + 0.6) * 0.25
    return (x * amp, y * amp)


# === EL SOBRESALTO del C# (.90 — lo nuevo): dir·amp·(t_rest/7) ===
DUR_SOBRESALTO = 7


def sobresalto(t_rest, dirx, diry, amp):
    if t_rest <= 0:
        return (0.0, 0.0)
    k = amp * (t_rest / DUR_SOBRESALTO)
    return (dirx * k, diry * k)


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

    ESC = 6
    W, H = 36 * ESC, 49 * ESC               # el libro a 6×
    # margen para el brinco (2,2 px) + desfase de tira (6 px) + temblor
    MARGEN = 10 * ESC
    PW, PH = W + 2 * MARGEN, H + 2 * MARGEN

    def iris_en(dx_px, dy_px):
        """el iris compuesto en una posición de mirada (px de textura)."""
        img = base.copy()
        i = iris.resize((max(1, round(32 * 0.2773 * ESC)),) * 2, Image.NEAREST)
        cx = (19.09 + dx_px) * ESC
        cy = (22.45 + dy_px) * ESC
        img = img.resize((W, H), Image.NEAREST)
        img.paste(i, (round(cx - i.width / 2), round(cy - i.height / 2)), i)
        return img

    GAP, LBL = 14, 18
    cols, rows = 3, 2
    lienzo = Image.new('RGBA', (cols * PW + GAP * (cols + 1),
                                rows * PH + GAP * (rows + 1)), (36, 34, 40, 255))
    draw = ImageDraw.Draw(lienzo)

    def panel(h, tick, con_glitch, susto=None, mirada=(0.0, 0.0), semilla=987654321):
        """susto = (t_rest, dirx, diry) — el brinco del C# en decaimiento."""
        img = Image.new('RGBA', (PW, PH), (0, 0, 0, 0))
        t = tick / 60.0
        dx, dy = temblor(t, h)
        if susto is not None:
            t_rest, dirx, diry = susto
            amp = 1.2 + (2.2 - 1.2) * h
            sx, sy = sobresalto(t_rest, dirx, diry, amp)
            dx, dy = dx + sx, dy + sy
        ox = MARGEN + round(dx * ESC)
        oy = MARGEN + round(dy * ESC)

        # EL LIBRO — temblando (y brincando); en ráfaga se rompe en tiras
        libro = iris_en(*mirada)
        if con_glitch:
            tiras, off_ojo = layout88(semilla, tick, h)
            for (a0, b0, o) in tiras:
                franja = libro.crop((0, a0 * ESC, W, b0 * ESC))
                libro.paste(franja, (round(o * ESC), a0 * ESC))
            # el iris viaja con su tira
            ddx = round(off_ojo * ESC)
            if ddx:
                cx = (19.09 + mirada[0]) * ESC
                cy = (22.45 + mirada[1]) * ESC
                caja = (round(cx - 20), round(cy - 20),
                        round(cx + 20), round(cy + 20))
                parche = libro.crop(caja)
                libro.paste(parche, (caja[0] + ddx, caja[1]))
        img.paste(libro, (ox, oy), libro)
        return img

    def pegar(img, col, row, titulo):
        x = GAP + col * (PW + GAP)
        y = GAP + row * (PH + GAP)
        lienzo.paste(img, (x, y + LBL), img)
        draw.text((x + 4, y + 2), titulo, fill=(235, 230, 220, 255))

    # 1. NORMAL — el libro aprobado (mirada serena al frente, INTACTO)
    pegar(iris_en(0.0, 0.0), 0, 0, '1 NORMAL (base — sin cambios)')

    # 2. NERVIOSO saciado: h=0 — temblor 0,2 px (susurro), sin glitch
    #    (gate 12 %), sin susto: un libro quieto con la mirada inquieta
    pegar(panel(0.0, 240, False, mirada=(1.8, 0.6)), 1, 0,
          '2 NERVIOSO saciado (inquieto, nada roto)')

    # 3. NERVIOSO h=0,35 EN SOBRESALTO — el brinco a mitad (t_rest=4 de 7,
    #    diagonal arriba-derecha) + temblor; el iris en DARDO LATERAL
    pegar(panel(0.35, 3600, False, susto=(4, 0.707, -0.707), mirada=(2.6, 0.3)),
          2, 0, '3 NERVIOSO h=0.35 SOBRESALTO (brinco+temblor)')

    # 4-5. NERVIOSO h=1,0 ráfaga frames A/B — el tearing se REGENERA y el
    #      iris revisa el CENTRO (la revisada ansiosa de la casa)
    pegar(panel(1.0, 1200, True, mirada=(0.0, 0.0)), 0, 1,
          '4 NERVIOSO h=1.0 rafaga A (tiras, ojo al centro)')
    pegar(panel(1.0, 1202, True, mirada=(0.0, 0.0)), 1, 1,
          '5 NERVIOSO h=1.0 rafaga B (regenerada)')

    # 6. NERVIOSO h=1,0 EL SUSTO — brinco entero (t_rest=7: amplitud plena,
    #    diagonal) + ráfaga corta A LA VEZ: el momento firma de la copia
    pegar(panel(1.0, 4400, True, susto=(7, 0.577, -0.577), mirada=(2.2, -0.5),
                semilla=1357924680), 2, 1,
          '6 NERVIOSO h=1.0 EL SUSTO (brinco+rafaga juntas)')

    lienzo.save(SALIDA)
    print('OK', SALIDA, lienzo.size)
    # métricas del sobresalto para el reporte
    for hh in (0.0, 0.35, 1.0):
        amp = 1.2 + 1.0 * hh
        print(f'  sobresalto h={hh}: brinco {amp:.2f}px, se asienta en '
              f'{DUR_SOBRESALTO} ticks; cada {6.5 + (1.8 - 6.5) * hh:.1f}s')


if __name__ == '__main__':
    main()
