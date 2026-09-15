#!/usr/bin/env python3
# gen_v630_assets.py — LOS ASSETS PROCEDURALES DE v6.30
#
# 1. QuemaduraCosmica.png (32×32): LA FORMA del debuff de quemadura (la
#    llama de OnFire) TIÑIDA DE NEGRO con corazón violeta-cósmico y motas
#    de estrellas — el fuego que arde hacia dentro (petición literal del
#    usuario: "toma la forma del debuff de quemadura y luego tiñe de negro").
#
# Estilo: pixel-art DURO de la casa, contorno exterior violeta-negro 1px,
# esquinas transparentes verificadas.

from PIL import Image
import math, os

OUT = os.path.join(os.path.dirname(__file__), '..', 'Content', 'Buffs')
os.makedirs(OUT, exist_ok=True)

def guardar(im, nombre):
    ruta = os.path.join(OUT, nombre)
    im.save(ruta)
    import numpy as np
    a = np.array(im.convert('RGBA'))
    esquinas = [int(a[0,0,3]), int(a[0,-1,3]), int(a[-1,0,3]), int(a[-1,-1,3])]
    print(f"  {nombre}: {im.size}, esquinas alfa {esquinas}")

# ----------------------------------------------------------------------
# 1. LA LLAMA NEGRA — la forma del fuego de quemadura, tiñida de negro
# ----------------------------------------------------------------------
def llama_negra():
    W = H = 32
    im = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    px = im.load()

    # LA FORMA de la llama (la silueta clásica del icono OnFire de Terraria:
    # una llama con dos lenguas laterales y la punta central alta).
    def forma_llama(x, y):
        fx = (x - 15.5) / 15.5          # -1..1
        fy = y / 31.0                   # 0..1 (0 arriba = punta)
        # el ancho crece de la punta (fy=0) a la base (fy=1) — llama
        ancho = 0.18 + 0.82 * (fy ** 0.72)
        # la ondulación de las dos lenguas laterales (la firma de la llama)
        ond = 0.10 * math.sin(fy * math.pi * 2.6 + 0.6)
        dentro = abs(fx) < (ancho + ond * (1 - fy))
        # la muesca de la base (la llama se estrecha al respirar)
        dentro = dentro and not (fy > 0.80 and abs(fx) < 0.30)
        # el estándar de la casa: la llama NUNCA toca los bordes (las
        # esquinas y la última fila quedan transparentes para el contorno)
        dentro = dentro and fy < 0.93
        dentro = dentro and abs(fx) < 0.90
        return dentro

    # LA RAMPA CÓSMICA: negro en el CORAZÓN de la llama, violeta profundo al
    # borde (la forma de la quemadura, tiñida de negro — el fuego INVERTIDO:
    # lo más caliente es lo más OSCURO, el corazón violeta del agujero).
    NEGRO   = (12, 6, 22, 255)      # el corazón negro-cósmico
    VIOLETA = (84, 34, 128, 255)    # el borde del fuego oscuro
    MORADO  = (150, 84, 210, 255)   # las brasas de las lenguas
    TINTE   = (196, 150, 240, 255)  # el rabillo casi-fuego (violeta claro)

    for y in range(H):
        for x in range(W):
            if not forma_llama(x, y):
                continue
            fx = abs(x - 15.5) / 15.5
            t = 1.0 - fx
            if t > 0.72:
                c = NEGRO
            elif t > 0.45:
                k = (t - 0.45) / 0.27
                c = tuple(int(VIOLETA[i] + (NEGRO[i] - VIOLETA[i]) * k) for i in range(4))
            elif t > 0.22:
                k = (t - 0.22) / 0.23
                c = tuple(int(MORADO[i] + (VIOLETA[i] - MORADO[i]) * k) for i in range(4))
            else:
                k = t / 0.22
                c = tuple(int(TINTE[i] + (MORADO[i] - TINTE[i]) * k) for i in range(4))
            # las lenguas ALTAS arden más violeta (la punta del fuego oscuro)
            fy = y / 31.0
            if fy < 0.22 and t > 0.5:
                c = tuple(min(255, int(v * 0.85 + 30)) if i < 3 else v for i, v in enumerate(c))
            px[x, y] = c

    # LAS MOTAS DE ESTRELLAS dentro del negro (el cosmos visible en la herida)
    semilla = 630
    def h(n):
        v = semilla * 374761393 + n * 668265263
        v = (v ^ (v >> 13)) * 1274126177
        v = (v ^ (v >> 16)) & 0xFFFFFF
        return v / 16777216.0
    for k in range(14):
        x = 4 + int(h(k * 7 + 1) * 24)
        y = 6 + int(h(k * 7 + 2) * 22)
        if forma_llama(x, y):
            brillo = h(k * 7 + 3)
            if brillo > 0.45:
                v = 140 + int(115 * brillo)
                px[x, y] = (v, v - 40, 255, 255)
                if x + 1 < W and forma_llama(x + 1, y):
                    px[x + 1, y] = (90, 60, 170, 255)

    # EL CONTORNO exterior (1px violeta-negro — el pixel-art duro de la casa)
    contorno = (30, 12, 52, 255)
    borde = []
    for y in range(H):
        for x in range(W):
            if not forma_llama(x, y):
                for dx, dy in ((1,0),(-1,0),(0,1),(0,-1)):
                    nx, ny = x + dx, y + dy
                    if 0 <= nx < W and 0 <= ny < H and forma_llama(nx, ny):
                        borde.append((x, y))
                        break
    for x, y in borde:
        px[x, y] = contorno

    return im

print("== v6.30: los assets ==")
guardar(llama_negra(), 'QuemaduraCosmica.png')
print("OK")

# ----------------------------------------------------------------------
# 2. LA CABEZA DE LA EMINENCIA — el icono medido: masa rojo-oscura
#    (32,0,0)+(64,0,0) ~50% con detalles TAN (128,96,64) ~20% y los
#    OJOS rojo-naranja ardiendo (la "Cabeza de Dismas" de Darkest Dungeon).
# ----------------------------------------------------------------------
def cabeza_eminencia():
    W = H = 30
    im = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    px = im.load()

    # LA SILUETA de la cabeza encapuchada (redondeada arriba, la barbilla
    # angosta abajo, con la capuja cayendo a los lados).
    def dentro(x, y):
        fx = (x - 14.5) / 14.5
        fy = y / 29.0
        # la cúpula de la cabeza (arriba redonda)
        if fy < 0.55:
            r = 0.92 * (1 - fy * 0.25)
            return fx * fx / (r * r) + (fy / 0.72) ** 2 < 1.0
        # la mandíbula angostándose
        ancho = 0.92 * (1 - (fy - 0.55) * 0.85)
        return abs(fx) < ancho and fy < 0.94

    ROJO_OSCURO = (44, 4, 8)
    ROJO_MEDIO  = (78, 10, 14)
    ROJO_ALTO   = (108, 20, 22)
    TAN         = (128, 96, 64)
    TAN_CLARO   = (150, 118, 84)

    for y in range(H):
        for x in range(W):
            if not dentro(x, y):
                continue
            fx = abs(x - 14.5) / 14.5
            fy = y / 29.0
            # la carne rojo-oscura con sombreado (más oscuro a los bordes/abajo)
            base = ROJO_OSCURO if fx > 0.62 or fy > 0.78 else (
                ROJO_MEDIO if fx > 0.35 else ROJO_ALTO)
            # la iluminación de la coronilla
            if fy < 0.22 and fx < 0.45:
                base = tuple(min(255, int(c * 1.25)) for c in base)
            px[x, y] = base + (255,)

    # EL PLIEGUE DE LA CAPUJA (línea tan cruzando la frente)
    for x in range(5, 26):
        y = 8 + (1 if (x // 3) % 2 == 0 else 0)
        if dentro(x, y):
            px[x, y] = TAN + (255,)
            if dentro(x, y + 1):
                px[x, y + 1] = TAN_CLARO + (255,) if (x % 4) < 2 else (60, 8, 10, 255)

    # LAS COSTURAS (los hilvos tan de la cabeza momificada)
    for k in range(4):
        x = 6 + k * 5
        for y in range(14, 19):
            if dentro(x, y):
                px[x, y] = TAN + (255,)

    # LA BOCA COSIDA (la línea horizontal tan con las puntadas)
    for x in range(10, 21):
        y = 23
        if dentro(x, y):
            px[x, y] = (30, 4, 6, 255)
    for k in range(3):
        x = 11 + k * 4
        if dentro(x, 23):
            px[x, 23] = TAN + (255,)
            if dentro(x - 1, 23): px[x - 1, 23] = TAN_CLARO + (255,)
            if dentro(x + 1, 23): px[x + 1, 23] = TAN_CLARO + (255,)

    # LOS OJOS QUE ARDEN (rojo-naranja — la firma medida 253,74,60)
    for ex in (10, 19):
        # el zócalo negro
        for dy in (-1, 0, 1):
            for dx in (-2, -1, 0, 1, 2):
                xx, yy = ex + dx, 16 + dy
                if 0 <= xx < W and 0 <= yy < H and dentro(xx, yy):
                    px[xx, yy] = (16, 2, 4, 255)
        # el ojo ardiendo
        px[ex, 16] = (253, 74, 60, 255)
        if dentro(ex + 1, 16): px[ex + 1, 16] = (255, 130, 70, 255)
        if dentro(ex - 1, 16): px[ex - 1, 16] = (180, 40, 34, 255)

    # EL CONTORNO (1px casi negro)
    contorno = (18, 3, 5, 255)
    for y in range(H):
        for x in range(W):
            if not dentro(x, y):
                for dx, dy in ((1,0),(-1,0),(0,1),(0,-1)):
                    nx, ny = x + dx, y + dy
                    if 0 <= nx < W and 0 <= ny < H and dentro(nx, ny):
                        px[x, y] = contorno
                        break
    return im

OUTW = os.path.normpath(os.path.join(OUT, '..', 'Weapons', 'Cosmic'))
im2 = cabeza_eminencia()
ruta2 = os.path.join(OUTW, 'EminenciaAtrozStaff.png')
im2.save(ruta2)
import numpy as np
a2 = np.array(im2.convert('RGBA'))
print('  EminenciaAtrozStaff.png: %s, esquinas %s' % (im2.size, [int(a2[0,0,3]), int(a2[0,-1,3]), int(a2[-1,0,3]), int(a2[-1,-1,3])]))
print("OK icono eminencia")
