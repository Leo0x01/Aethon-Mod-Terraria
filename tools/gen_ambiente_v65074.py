#!/usr/bin/env python3
# ==================================================================
#  gen_ambiente_v65074.py — EL CIELO DE LA OLEADA (v6.50.74)
#
#  La letra del usuario: «combinas el ambiente de el eclipse solar mas
#  el ambiente de cementerio… un toque morado a la iluminación naranja
#  del eclipse… en el de noche en la luna roja, esta también debe
#  cambiar su sprite».
#
#  DOS sprites 100% procedurales (PIL puro, deterministas, cero azar
#  libre — un LCG de semilla fija para el dithering):
#
#  1. SolDeLaOleada.png (200×200) — EL SOL ECLIPSADO: disco de violeta
#     oscuro (el cuerpo oculto) rodeado de UN ANILLO DE FUEGO naranja
#     quemado (la corona del eclipse) con llamaradas moradas — el toque
#     morado SOBRE el naranja. Reemplaza a TextureAssets.Sun de día.
#
#  2. LunaDeLaOleada.png (200×1600) — LA LUNA CARMESÍ: 8 fases de 200
#     apiladas verticalmente con el MISMO layout de vanilla (fase 0
#     llena, 1-3 menguante iluminada a la IZQUIERDA, 4 nueva, 5-7
#     creciente iluminada a la DERECHA — enum MoonPhase del decompile).
#     La parte iluminada es carmesí con cráteres oscuros; la oculta es
#     un FANTASMA violeta (la luna siempre acecha) y TODO el disco
#     lleva un HALO violeta (el toque morado de la noche).
#
#  El strip usa fases cuadradas de Width×Width: el draw de vanilla
#  muestrea Rectangle(0, Width*moonPhase, Width, Width) — cualquier
#  ancho funciona, 200 = el tamaño nativo de vanilla.
# ==================================================================
import math
from PIL import Image

DEST = "AethonMod/Content/Ambientes"

# --- el LCG de la casa (determinista) -------------------------------
_semilla = 0x5EED2024
def rnd():
    global _semilla
    _semilla = (_semilla * 1664525 + 1013904223) & 0xFFFFFFFF
    return _semilla / 0x100000000

def lerp(a, b, t):
    return a + (b - a) * t

def mezcla(c1, c2, t):
    return tuple(int(round(lerp(c1[i], c2[i], t))) for i in range(len(c1)))

def clamp(v, a, b):
    return max(a, min(b, v))

# ==================================================================
#  1 — EL SOL ECLIPSADO (200×200)
# ==================================================================
def gen_sol():
    W = H = 200
    cx = cy = 100.0
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    px = im.load()

    # paleta
    NUCLEO_IN  = (24, 8, 42)      # violeta casi negro (el cuerpo oculto)
    NUCLEO_OUT = (64, 24, 96)     # violeta profundo (el borde del disco)
    FUEGO_IN   = (255, 158, 64)   # naranja vivo (la corona interior)
    FUEGO_OUT  = (196, 52, 16)    # naranja quemado (el borde de fuego)
    LLAMA      = (168, 74, 214)   # morado (las llamaradas)
    LLAMA_OSC  = (108, 40, 158)

    R_DISCO = 60.0   # el disco oculto
    R_FUEGO = 76.0   # el anillo de fuego
    R_MAX   = 98.0   # el alcance de las llamaradas

    # suavizado de los bordes duros (2 px a cada lado)
    SUAVE = 2.0

    def suaviza(r, borde):
        return clamp((r - (borde - SUAVE)) / (2.0 * SUAVE), 0.0, 1.0)

    # los picos de las llamaradas: 12 rayos alternando largo/corto
    rayos = []
    for i in range(12):
        ang = i * (math.pi * 2 / 12) + 0.13
        largo = 88.0 if i % 2 == 0 else 74.0
        rayos.append((ang, largo))

    for y in range(H):
        for x in range(W):
            dx, dy = x - cx, y - cy
            r = math.hypot(dx, dy)
            if r > R_MAX:
                continue
            ang = math.atan2(dy, dx)

            # la fuerza de la llamarada en este ángulo (los 12 picos)
            llama = 0.0
            for ra, rlargo in rayos:
                da = abs(math.atan2(math.sin(ang - ra), math.cos(ang - ra)))
                if da < 0.30:                      # dentro del rayo
                    t = 1.0 - da / 0.30            # 1 en el pico
                    rad = 1.0 - (r - R_FUEGO) / (rlargo - R_FUEGO)
                    if rad > 0.0:
                        llama = max(llama, t * rad)

            if r <= R_DISCO + SUAVE:
                # EL DISCO OCULTO: violeta oscuro con vetas sutiles
                t = clamp(r / R_DISCO, 0.0, 1.0)
                veta = 0.5 + 0.5 * math.sin(ang * 7.0 + t * 9.0)
                col = mezcla(NUCLEO_IN, NUCLEO_OUT, t * 0.85 + veta * 0.15)
                a = 255
                # el borde del disco SE FUNDE con el fuego (sin escalones)
                if r > R_DISCO - SUAVE:
                    t2 = (r - R_DISCO + SUAVE) / (2.0 * SUAVE)
                    brillo = 0.35
                    fuego = mezcla(FUEGO_OUT, FUEGO_IN, brillo)
                    col = mezcla(col, fuego, t2)
            elif r <= R_FUEGO + SUAVE:
                # EL ANILLO DE FUEGO: lo más vivo pegado al disco
                t = clamp((r - R_DISCO) / (R_FUEGO - R_DISCO), 0.0, 1.0)
                brillo = (1.0 - t) ** 0.7
                col = mezcla(FUEGO_OUT, FUEGO_IN, brillo)
                # el fuego se MORDISQUEA con morado hacia afuera
                if t > 0.55 and rnd() < (t - 0.55) * 1.6:
                    col = mezcla(col, LLAMA_OSC, 0.55)
                a = int(255 * clamp(1.35 - t * 0.5, 0.0, 1.0))
            else:
                # LA CORONA: el fuego se apaga y nacen las llamaradas moradas
                t = (r - R_FUEGO) / (R_MAX - R_FUEGO)
                base = max(0.0, 1.0 - t * 2.6)          # el fuego muere rápido
                col = mezcla(FUEGO_OUT, LLAMA_OSC, clamp(t * 1.7, 0.0, 1.0))
                if llama > 0.02:
                    col = mezcla(col, LLAMA, clamp(llama * 1.15, 0.0, 1.0))
                a = int(255 * clamp(max(base * 0.55, llama) , 0.0, 1.0))
                if a < 6:
                    continue
            px[x, y] = (col[0], col[1], col[2], a)
    return im

# ==================================================================
#  2 — LA LUNA CARMESÍ (200×1600, 8 fases de 200)
# ==================================================================
# El orden de fases de vanilla (enum MoonPhase del decompile):
#   0 Full · 1 ThreeQuartersAtLeft · 2 HalfAtLeft · 3 QuarterAtLeft
#   4 Empty · 5 QuarterAtRight · 6 HalfAtRight · 7 ThreeQuartersAtRight
# "AtLeft/AtRight" = el LADO ILUMINADO (menguante a la izquierda,
# creciente a la derecha).
FASES_F = [1.0, 0.75, 0.5, 0.25, 0.0, 0.25, 0.5, 0.75]
FASES_S = [0, -1, -1, -1, 0, +1, +1, +1]

# los cráteres (relativos al centro, en px) — los mismos en TODAS las fases
CRATERES = [
    (-26, -20, 13), (18, -34, 9), (30, 8, 15), (-8, 26, 11),
    (-38, 14, 7), (6, -8, 7), (40, -22, 6), (-16, -42, 6), (14, 44, 8),
]

def gen_luna_fase(p):
    W = H = 200
    cx = cy = 100.0
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    px = im.load()

    R = 72.0        # el disco
    R_HALO = 94.0   # el halo violeta
    AA = 1.5        # el suavizado del terminador (submuestreo 2×2)

    f = FASES_F[p]
    s = FASES_S[p]
    k = 2.0 * f - 1.0   # el terminador: lit ⇔ s·x + k·w ≥ 0

    # paleta
    LIT_IN   = (240, 96, 108)   # carmesí claro (el brillo del centro)
    LIT_MED  = (196, 34, 58)    # carmesí pleno
    LIT_OUT  = (142, 16, 38)    # carmesí profundo (el borde)
    CRATER   = (104, 10, 28)    # los cráteres
    FANTASMA = (52, 26, 84)     # la cara oculta (violeta oscuro)
    HALO     = (172, 84, 224)   # el halo morado

    def color_lit(dx, dy, r, t):
        col = mezcla(LIT_MED, LIT_IN, (1.0 - t) ** 1.6 * 0.8)
        col = mezcla(col, LIT_OUT, clamp(t * t * 1.15, 0.0, 1.0))
        for cdx, cdy, cr in CRATERES:
            d = math.hypot(dx - cdx, dy - cdy)
            if d < cr:
                prof = 1.0 - d / cr
                col = mezcla(col, CRATER, clamp(prof * 1.25, 0.0, 1.0) * 0.85)
        return col

    def es_lit(dx, dy, w_f=None):
        if s == 0:
            return f >= 1.0
        w = w_f if w_f is not None else math.sqrt(max(0.0, R * R - dy * dy))
        return (s * dx + k * w) >= 0.0

    for y in range(H):
        for x in range(W):
            dx, dy = x - cx, y - cy
            r = math.hypot(dx, dy)
            if r > R_HALO:
                continue

            if r <= R + AA:
                # EL SUBMUESTREO 2×2: el terminador y el borde del disco
                # llevan ANTI-ALIAS (la crítica de la primera hoja: los
                # escalones del cuarto y los crecientes)
                lit = 0.0
                for ox in (-0.5, 0.5):
                    for oy in (-0.5, 0.5):
                        sdx, sdy = dx + ox, dy + oy
                        sr = math.hypot(sdx, sdy)
                        if sr <= R and es_lit(sdx, sdy):
                            lit += 0.25
                t = clamp(r / R, 0.0, 1.0)
                if lit >= 1.0:
                    col = color_lit(dx, dy, r, t)
                    a = 255
                elif lit <= 0.0:
                    # LA CARA OCULTA: un fantasma violeta (la luna SIEMPRE
                    # acecha — la fase nueva no desaparece, vela)
                    rim = clamp((t - 0.82) / 0.18, 0.0, 1.0)
                    col = FANTASMA
                    a = int(58 + rim * 44)
                else:
                    col_l = color_lit(dx, dy, r, t)
                    col = mezcla(FANTASMA, col_l, lit)
                    a = int(lerp(58, 255, lit))
                # el borde del disco: un hilo de luz violeta
                if t > 0.94:
                    col = mezcla(col, HALO, 0.35)
                    a = max(a, 120)
                # el último píxel se disuelve en el halo
                if r > R - AA:
                    a = int(a * clamp((R + AA - r) / (2.0 * AA), 0.0, 1.0) * 0.9 + 10)
                px[x, y] = (col[0], col[1], col[2], a)
            else:
                # EL HALO MORADO: anillo que se disuelve (presente en TODAS
                # las fases — el toque morado de la noche)
                t = (r - R) / (R_HALO - R)
                a = int(80 * (1.0 - t) ** 1.5)
                if a > 4:
                    px[x, y] = (HALO[0], HALO[1], HALO[2], a)
    return im

def main():
    import os
    os.makedirs(DEST, exist_ok=True)

    sol = gen_sol()
    ruta_sol = f"{DEST}/SolDeLaOleada.png"
    sol.save(ruta_sol)
    print(f"[ok] {ruta_sol} {sol.size}")

    strip = Image.new("RGBA", (200, 1600), (0, 0, 0, 0))
    for p in range(8):
        fase = gen_luna_fase(p)
        strip.paste(fase, (0, p * 200))  # SIN máscara: copia RGBA cruda (la máscara MEZCLA y mata los halos tenues)
    ruta_luna = f"{DEST}/LunaDeLaOleada.png"
    strip.save(ruta_luna)
    print(f"[ok] {ruta_luna} {strip.size}")

    # ---- autoverificación ----------------------------------------
    # 1) tamaños exactos
    assert sol.size == (200, 200), "el sol debe ser 200×200"
    assert strip.size == (200, 1600), "la luna debe ser 200×1600 (8×200)"

    # 2) las 8 fases son DISTINTAS y el orden de iluminación es correcto
    pxs = strip.load()
    def cuenta_lit(p):
        n = 0
        for y in range(p * 200, (p + 1) * 200, 4):
            for x in range(0, 200, 4):
                r, g, b, a = pxs[x, y]
                if a > 200 and r > 120:   # píxel carmesí pleno
                    n += 1
        return n
    lits = [cuenta_lit(p) for p in range(8)]
    print("    iluminación por fase:", lits)
    assert lits[0] > lits[1] > lits[2] > lits[3], "menguante: llena → 3/4 → 1/2 → 1/4"
    assert lits[4] < lits[3], "la nueva es la más oscura"
    assert lits[5] < lits[6] < lits[7] < lits[0], "creciente: 1/4 → 1/2 → 3/4"
    # el lado iluminado: menguante a la IZQUIERDA, creciente a la DERECHA
    def lado_lit(p):
        izq = der = 0
        for y in range(p * 200, (p + 1) * 200, 4):
            for x in range(0, 100, 4):
                r, g, b, a = pxs[x, y]
                if a > 200 and r > 120: izq += 1
            for x in range(100, 200, 4):
                r, g, b, a = pxs[x, y]
                if a > 200 and r > 120: der += 1
        return izq, der
    for p in (1, 2, 3):
        izq, der = lado_lit(p)
        assert izq > der, f"fase {p}: la menguante ilumina la IZQUIERDA ({izq} vs {der})"
    for p in (5, 6, 7):
        izq, der = lado_lit(p)
        assert der > izq, f"fase {p}: la creciente ilumina la DERECHA ({izq} vs {der})"

    # 3) el sol: el anillo de fuego existe y el disco es oscuro
    pxs2 = sol.load()
    centro = pxs2[100, 100]
    assert centro[2] > centro[0], "el centro del sol es VIOLETA oscuro (el cuerpo oculto)"
    assert centro[0] < 90, "el centro no es brillante"
    anillo = pxs2[100, 34]   # a 66px del centro: dentro del anillo de fuego (60..76)
    assert anillo[0] > 180 and anillo[1] > 80, f"el anillo de fuego arde: {anillo}"

    # 4) el halo morado existe alrededor de la luna llena
    h = pxs[100, 12]  # por encima del disco (y=12 < 100-72)
    assert h[3] > 10 and h[2] > h[1], f"halo violeta sobre el disco: {h}"

    print("[ok] autoverificación completa: fases correctas, sol eclipsado, halo morado")

    # hoja de contacto para el ojo (VLM)
    hoja = Image.new("RGBA", (200 * 10 + 110, 240), (18, 10, 26, 255))
    hoja.paste(sol, (10, 20), sol)
    for p in range(8):
        fase = strip.crop((0, p * 200, 200, (p + 1) * 200))
        hoja.paste(fase, (220 + p * 200, 20), fase)
    hoja.convert("RGB").save("/tmp/ambiente74_hoja.png")
    print("[ok] hoja de contacto: /tmp/ambiente74_hoja.png")

if __name__ == "__main__":
    main()
