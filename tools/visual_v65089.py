#!/usr/bin/env python3
"""
v6.50.89 — PREVIEW VISUAL DEL INESTABLE (QA).
La letra del usuario: «crea un cuarto grimorio donde el efecto glish y
el temblor este unidos, ademas ponle un aura pequeña roja». Reproduce
en Python EXACTAMENTE la matemática del C# (Hash uint32 portado) y
compone los TRES efectos sobre el arte del paquete (rawimg decodificado):
  · EL GLITCH .88 (el volumen aprobado: 2-4 tiras, ±3,3→±6 px, regen/2f)
  · EL TEMBLOR .87 (dos senos por eje, 0,2→1,3 px con el hambre)
  · EL AURA ROJA NUEVA: SoftGlow del paquete teñido (255,64,48),
    1,55× el ancho del libro, alfa 0,30→0,42 con respiración ±12 %.
Paneles:
  1. NORMAL (línea de base — el libro aprobado, sin aura)
  2. INESTABLE saciado (h=0,0: aura mínima, temblor 0,2 px, sin glitch)
  3. INESTABLE h=0,35 EN RÁFAGA (glitch moderado + aura + temblor)
  4. INESTABLE h=1,0 ráfaga frame A (2-4 tiras rotas)
  5. INESTABLE h=1,0 ráfaga frame B (2 frames después — el tearing se
     regeneró: OTRA geometría)
  6. INESTABLE h=1,0 LIMPIO (sin ráfaga: el cuerpo entero tiembla
     ardiendo — el temblor en su pico)
"""
import math
import struct
import sys
import zlib

sys.path.insert(0, 'tools')
from parse_tmod import parse  # noqa: E402

TMOD = '/home/z/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod'
SALIDA = '/tmp/preview_v65089.png'

MASK = 0xFFFFFFFF


def u32(x):
    return x & MASK


def Hash(x):
    x = u32(x)
    x = u32(x ^ (x >> 16)); x = u32(x * 0x7FEB352D)
    x = u32(x ^ (x >> 15)); x = u32(x * 0x846CA68B)
    x = u32(x ^ (x >> 13))
    return x


# === EL LAYOUT .88 del C#, portado (GrimorioHambrientoInestable.Layout) ===
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


# === EL TEMBLOR del C# (.87 — el del Tembloroso, portado) ===
TWO_PI = math.tau


def temblor(t, h):
    amp = 0.20 + 1.10 * h
    x = math.sin(t * 9.3 * TWO_PI) * 0.80 + math.sin(t * 17.3 * TWO_PI) * 0.25
    y = math.sin(t * 11.9 * TWO_PI + 1.7) * 0.80 + math.sin(t * 19.1 * TWO_PI + 0.6) * 0.25
    return (x * amp, y * amp)


# === EL AURA del C# (nueva — SoftGlow teñido de rojo) ===
AURA_ANCHO = 1.55
AURA_ROJO = (255, 64, 48)
AURA_ALFA_MIN, AURA_ALFA_MAX = 0.30, 0.42


def pulso(t):
    return 0.88 + 0.12 * math.sin(t * 0.45 * TWO_PI)


def alfa_aura(t, h):
    return (AURA_ALFA_MIN + (AURA_ALFA_MAX - AURA_ALFA_MIN) * h) * pulso(t)


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
    glow = rgba('SoftGlow.rawimg')          # 64×64 blanco con caída de alfa

    ESC = 6
    W, H = 36 * ESC, 49 * ESC               # el libro a 6×
    # el panel deja sitio al halo: 1,55× el ancho → (56−36)/2 = 10 px por lado
    MARGEN = 10 * ESC
    PW, PH = W + 2 * MARGEN, H + 2 * MARGEN

    iris6 = iris.resize((max(1, round(32 * 0.2773 * ESC)),) * 2, Image.NEAREST)
    cx, cy = (19.09 + 2.0) * ESC, (22.45 + 1.5) * ESC      # media mirada
    comp6 = base.resize((W, H), Image.NEAREST)
    comp6.paste(iris6, (round(cx - iris6.width / 2), round(cy - iris6.height / 2)), iris6)

    GAP, LBL = 14, 18
    cols, rows = 3, 2
    lienzo = Image.new('RGBA', (cols * PW + GAP * (cols + 1),
                                rows * PH + GAP * (rows + 1)), (36, 34, 40, 255))
    draw = ImageDraw.Draw(lienzo)

    # el glow teñido: SoftGlow × rojo, al alfa del C# — tamaño 1,55× el ancho
    ancho_libro_px = 36                                   # px de textura
    glow_esc = (ancho_libro_px * AURA_ANCHO / glow.width) * ESC

    def aura_img(h, t):
        a = alfa_aura(t, h)
        g = glow.resize((max(1, round(glow.width * glow_esc)),) * 2, Image.BILINEAR)
        # teñir: cada píxel → (255,64,48, α_softglow × a)
        import numpy as np
        arr = np.array(g).astype(float)
        alfa = arr[:, :, 3] * (a / 255.0)
        out = np.zeros((arr.shape[0], arr.shape[1], 4), dtype=np.uint8)
        out[:, :, 0], out[:, :, 1], out[:, :, 2] = AURA_ROJO
        out[:, :, 3] = alfa.astype(np.uint8)
        return Image.fromarray(out, 'RGBA')

    def panel(h, tick, con_glitch, con_aura, semilla=987654321):
        img = Image.new('RGBA', (PW, PH), (0, 0, 0, 0))
        t = tick / 60.0
        dx, dy = temblor(t, h)
        ox = MARGEN + round(dx * ESC)
        oy = MARGEN + round(dy * ESC)

        # 1) EL AURA — detrás de todo, respirando en el centro del libro
        if con_aura:
            a = aura_img(h, t)
            centro_x = ox + W // 2
            centro_y = oy + H // 2
            img.paste(a, (centro_x - a.width // 2, centro_y - a.height // 2), a)

        # 2) EL LIBRO — temblando; en ráfaga se rompe en tiras (el mismo
        #    layout que el C#: cada franja a su lugar + su desfase)
        libro = comp6.copy()
        if con_glitch:
            tiras, off_ojo = layout88(semilla, tick, h)
            for (a0, b0, o) in tiras:
                franja = libro.crop((0, a0 * ESC, W, b0 * ESC))
                libro.paste(franja, (round(o * ESC), a0 * ESC))
            # el iris viaja con su tira
            ddx = round(off_ojo * ESC)
            if ddx:
                caja = (round(cx - iris6.width / 2) - 6, round(cy - iris6.height / 2) - 6,
                        round(cx + iris6.width / 2) + 6, round(cy + iris6.height / 2) + 6)
                parche = libro.crop(caja)
                libro.paste(parche, (caja[0] + ddx, caja[1]))
        img.paste(libro, (ox, oy), libro)
        return img

    def pegar(img, col, row, titulo):
        x = GAP + col * (PW + GAP)
        y = GAP + row * (PH + GAP)
        lienzo.paste(img, (x, y + LBL), img)
        draw.text((x + 4, y + 2), titulo, fill=(235, 230, 220, 255))

    # 1. NORMAL — el libro aprobado (SIN aura: el original no cambia)
    pegar(comp6, 0, 0, '1 NORMAL (base — sin cambios)')

    # 2. INESTABLE saciado: h=0 — aura mínima respirando, temblor 0,2 px
    #    (invisible — como debe ser), SIN glitch (gate 12 %)
    pegar(panel(0.0, 240, False, True), 1, 0, '2 INESTABLE saciado (aura min, sin glitch)')

    # 3. INESTABLE h=0,35 EN RÁFAGA — glitch moderado + aura + temblor
    pegar(panel(0.35, 3600, True, True), 2, 0, '3 INESTABLE h=0.35 rafaga (todo junto)')

    # 4-5. INESTABLE h=1,0 ráfaga frames A/B — el tearing se REGENERA
    pegar(panel(1.0, 1200, True, True), 0, 1, '4 INESTABLE h=1.0 rafaga A (tirer rotas)')
    pegar(panel(1.0, 1202, True, True), 1, 1, '5 INESTABLE h=1.0 rafaga B (regenerada)')

    # 6. INESTABLE h=1,0 LIMPIO — sin ráfaga: el cuerpo entero tiembla
    #    ardiendo (el tick del temblor MÁXIMO en 10 s)
    mejor_t, mejor_m = 0, -1.0
    for t in range(600):
        dx, dy = temblor(t / 60.0, 1.0)
        m = math.hypot(dx, dy)
        if m > mejor_m:
            mejor_m, mejor_t = m, t
    pegar(panel(1.0, mejor_t, False, True), 2, 1,
          f'6 INESTABLE h=1.0 limpio (temblor pico {mejor_m:.2f}px + aura)')

    lienzo.save(SALIDA)
    print('OK', SALIDA, lienzo.size)
    # métricas del aura para el reporte
    for hh in (0.0, 0.35, 1.0):
        a_max = alfa_aura(1.0 / (0.45 * 4), hh)   # cerca del pico de la respiración
        a_min = alfa_aura(1.0 / (0.45 * 2), hh)
        print(f'  aura h={hh}: alfa {a_min:.3f}…{a_max:.3f} · ancho '
              f'{36 * AURA_ANCHO:.0f}px de textura (halo ~{36 * (AURA_ANCHO - 1) / 2:.0f}px/lado)')


if __name__ == '__main__':
    main()
