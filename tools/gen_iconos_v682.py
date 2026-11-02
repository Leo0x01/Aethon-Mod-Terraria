#!/usr/bin/env python3
# =============================================================================
# gen_iconos_v682.py — LOS ICONOS DE LAS TRES ARMAS NUEVAS (v6.82)
#
# 1. AgujeroAbsolutoStaff.png (28×30) — el bastón del AGUJERO ABSOLUTO: el
#    cayado violeta-cian-oro (el gradiente del más allá) con la esfera
#    negra y SU CUARTA CORONA (los 4 anillos rúnicos concéntricos — la
#    firma del arma; los Supremos tienen 3).
# 2. CompasRunico.png (28×30) — EL COMPÁS RÚNICO (clicker): el compás de
#    bronce estelar ABIERTO apuntando arriba con su aguja cian arcano
#    y la runa grabada en la bisagra.
# 3. FuriaSolarClicker.png (28×30) — LA FURIA SOLAR (clicker): el
#    martillo-sol (la cabeza ES un sol pequeño: núcleo blanco-oro, la
#    corona de llamas) con el mango oscuro.
#
# El estilo de la casa: pixel-art con volumetría por rampa (4 tonos por
# material, sombreado por distancia al foco), silueta legible al 100%,
# outline oscuro Terraria.
# =============================================================================
import math
import os
from PIL import Image, ImageDraw

RAIZ = os.path.join(os.path.dirname(__file__), '..')
W, H = 28, 30


def lienzo(w=W, h=H):
    return Image.new('RGBA', (w, h), (0, 0, 0, 0))


def mezcla(c1, c2, t):
    return tuple(int(round(a + (b - a) * t)) for a, b in zip(c1, c2))


def px(img, x, y, c):
    if 0 <= x < img.width and 0 <= y < img.height:
        img.putpixel((x, y), c)


def outline(img, color=(12, 10, 18, 255)):
    """El contorno Terraria: rodea la silueta."""
    src = img.copy()
    for y in range(img.height):
        for x in range(img.width):
            if src.getpixel((x, y))[3] == 0:
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    nx, ny = x + dx, y + dy
                    if 0 <= nx < img.width and 0 <= ny < img.height \
                            and src.getpixel((nx, ny))[3] != 0:
                        px(img, x, y, color)
                        break


# =====================================================================
#  1 · EL BASTÓN DEL AGUJERO ABSOLUTO (28×30)
# =====================================================================
def gen_absoluto():
    img = lienzo()
    # LA RAMPAS DEL MÁS ALLÁ: mango violeta del vacío → cian estelar →
    # oro blanco (el gradiente del perfil Absoluto).
    mango = [(70, 40, 150), (100, 65, 210), (140, 90, 255), (190, 150, 255)]
    cian = [(20, 90, 120), (40, 150, 190), (80, 220, 255), (170, 245, 255)]
    oro = [(150, 100, 30), (200, 150, 60), (255, 220, 120), (255, 245, 200)]

    # EL CAYADO: la vara vertical con el gancho arriba (el perfil del
    # Supremo: 28×30 — el bastón clásico de la casa).
    for y in range(8, 28):
        t = (y - 8) / 20.0
        tono = mezcla(mezcla(mango[1], mango[2], t), mango[3], 0.15)
        # el grosor 2px con su veta de luz a la izquierda
        px(img, 12, y, mezcla(tono, mango[3], 0.5))
        px(img, 13, y, tono)
    # EL GANCHO: la curva superior (la C invertida que abraza la esfera)
    curva = [(14, 6), (15, 5), (16, 4), (17, 4), (18, 5), (19, 6), (19, 7),
             (18, 8), (17, 9)]
    for i, (x, y) in enumerate(curva):
        t = i / (len(curva) - 1)
        tono = mezcla(cian[2], oro[2], t)          # cian→oro: el más allá
        px(img, x, y, tono)
    px(img, 15, 6, cian[3]); px(img, 16, 5, cian[3]); px(img, 17, 5, mezcla(cian[3], oro[3], 0.5))
    px(img, 18, 6, oro[3]); px(img, 18, 7, oro[2])

    # LA ESFERA NEGRA ABSOLUTA en el ojo del gancho (el corazón del arma)
    cx, cy, r = 14, 8, 4
    for yy in range(cy - r, cy + r + 1):
        for xx in range(cx - r, cx + r + 1):
            d = math.hypot(xx - cx, yy - cy)
            if d <= r:
                # el horizonte cian en el borde superior-izquierdo
                brillo = max(0.0, 1.0 - d / r)
                if brillo < 0.25:
                    c = mezcla((10, 14, 24), (40, 130, 165), (0.25 - brillo) / 0.25)
                else:
                    c = (10, 14, 24)
                px(img, xx, yy, c)
    # EL RIM CIAN del horizonte (la luz que no puede volver)
    px(img, 11, 6, (80, 220, 255)); px(img, 12, 5, (140, 235, 255))

    # LAS CUATRO CORONAS RÚNICAS (la firma: los Supremos tienen 3):
    # arcos concéntricos alrededor de la esfera — cian, violeta, oro, blanco.
    anillos = [
        (6, cian[2]),      # interior: cian
        (8, mango[2]),     # medio: violeta
        (10, oro[2]),      # exterior: oro blanco
        (12, (235, 250, 255)),  # LA CUARTA: blanco-cian (la corona absoluta)
    ]
    d = ImageDraw.Draw(img)
    for radio, col in anillos:
        # arcos partidos (las runas NO son anillos llenos: son segmentos)
        nseg = 8
        for s in range(nseg):
            a0 = s * (360 / nseg) + 8
            a1 = a0 + 360 / nseg - 14
            d.arc([cx - radio, cy - radio, cx + radio, cy + radio],
                  a0, a1, fill=col + (255,))

    outline(img)
    img.save(os.path.join(RAIZ, 'AethonMod/Content/Weapons/Cosmic/AgujeroAbsolutoStaff.png'))
    print('OK AgujeroAbsolutoStaff.png')


# =====================================================================
#  2 · EL COMPÁS RÚNICO (28×30)
# =====================================================================
def gen_compas():
    img = lienzo()
    # LAS RAMPAS: bronce estelar (las patas) + acero cian (la aguja).
    bronce = [(110, 70, 30), (170, 110, 45), (215, 160, 75), (250, 215, 140)]
    acero = [(30, 60, 90), (50, 110, 150), (90, 180, 225), (160, 235, 255)]
    runa = (120, 220, 255)

    # LA BISAGRA arriba (el círculo del compás, con SU RUNA grabada)
    cx, cy = 13, 6
    for yy in range(cy - 3, cy + 4):
        for xx in range(cx - 3, cx + 4):
            d = math.hypot(xx - cx, yy - cy)
            if d <= 3:
                c = bronce[3] if d < 1.6 else (bronce[2] if d < 2.6 else bronce[1])
                px(img, xx, yy, c)
    # la runa de la bisagra (el glifo del contrato: la I con su perla)
    px(img, cx, cy - 2, runa); px(img, cx, cy - 1, runa)
    px(img, cx, cy + 1, runa); px(img, cx, cy + 2, runa)
    px(img, cx - 1, cy, runa); px(img, cx + 1, cy, runa)

    # LAS DOS PATAS DEL COMPÁS abriéndose hacia abajo (la V del compás)
    for i in range(16):
        t = i / 15.0
        # pata izquierda
        x1 = int(12 - t * 8); y1 = 10 + int(t * 16)
        # pata derecha
        x2 = int(15 + t * 8); y2 = 10 + int(t * 16)
        tono = mezcla(bronce[1], bronce[0], t)
        px(img, x1, y1, mezcla(bronce[2], bronce[3], 0.35 - t * 0.3))
        px(img, x1 + 1, y1, tono)
        px(img, x2, y1, mezcla(bronce[2], bronce[3], 0.35 - t * 0.3))
        px(img, x2 - 1, y1, tono)
    # LAS PUNTAS (las agujas de acero cian — donde el compás CLICA)
    px(img, 4, 26, acero[2]); px(img, 4, 27, acero[3]); px(img, 3, 27, acero[2])
    px(img, 23, 26, acero[2]); px(img, 23, 27, acero[3]); px(img, 24, 27, acero[2])

    # LA AGUJA CENTRAL: el fiel cian apuntando ARRIBA (la que lee el
    # contrato — el eje del clic)
    for i in range(9):
        t = i / 8.0
        x = 13 + (1 if t < 0.5 else 0); y = 14 - int(t * 7)
        px(img, x, y, mezcla(acero[1], acero[3], t))
    px(img, 13, 5, acero[3])

    outline(img)
    img.save(os.path.join(RAIZ, 'AethonMod/Content/Weapons/Clicker/CompasRunico.png'))
    print('OK CompasRunico.png')


# =====================================================================
#  3 · LA FURIA SOLAR (28×30)
# =====================================================================
def gen_furia():
    img = lienzo()
    # LAS RAMPAS: el sol (la cabeza del martillo) + el mango oscuro.
    sol = [(170, 60, 20), (230, 120, 30), (255, 190, 70), (255, 245, 210)]
    mango = [(40, 30, 22), (70, 52, 36), (105, 80, 55), (140, 110, 75)]

    # EL MANGO: la vara vertical oscura (abajo)
    for y in range(16, 28):
        t = (y - 16) / 11.0
        px(img, 13, y, mezcla(mango[2], mango[3], 0.3))
        px(img, 14, y, mezcla(mango[1], mango[0], t))
    # LA EMPUÑADURA (el pomo de abajo)
    for dx in range(3):
        px(img, 12 + dx, 27, mango[3] if dx == 1 else mango[2])

    # LA CABEZA: UN SOL PEQUEÑO (núcleo blanco-oro + corona de llamas)
    cx, cy = 13, 9
    r = 5
    for yy in range(cy - r, cy + r + 1):
        for xx in range(cx - r, cx + r + 1):
            d = math.hypot(xx - cx, yy - cy)
            if d <= r:
                if d < 2.0:
                    c = sol[3]
                elif d < 3.4:
                    c = sol[2]
                elif d < 4.4:
                    c = sol[1]
                else:
                    c = sol[0]
                px(img, xx, yy, c)

    # LAS LLAMAS DE LA CORONA: las lenguas radiales del sol (8 puntas)
    for i in range(8):
        ang = i * math.pi / 4 + 0.39
        for k in range(2):
            r2 = r + 1 + k
            x = int(round(cx + math.cos(ang) * r2))
            y = int(round(cy + math.sin(ang) * r2))
            c = sol[2] if k == 0 else sol[1]
            px(img, x, y, c)
            px(img, x + (1 if math.cos(ang) > 0.3 else -1 if math.cos(ang) < -0.3 else 0),
               y, mezcla(c, sol[1], 0.5))

    # EL DESTELLO: la cruz de luz del sol (la chispa del clic)
    px(img, cx, cy - 7, (255, 250, 225)); px(img, cx, cy + 7 - 1, (255, 250, 225))
    px(img, cx - 7, cy, (255, 250, 225)); px(img, cx + 7, cy, (255, 250, 225))

    # EL VÍNCULO cabeza-mango (la abrazadera de bronce)
    for dx in range(-2, 3):
        px(img, cx + dx, 15, (170, 110, 45) if abs(dx) < 2 else (110, 70, 30))
        px(img, cx + dx, 16, (110, 70, 30))

    outline(img)
    img.save(os.path.join(RAIZ, 'AethonMod/Content/Weapons/Clicker/FuriaSolarClicker.png'))
    print('OK FuriaSolarClicker.png')


if __name__ == '__main__':
    gen_absoluto()
    gen_compas()
    gen_furia()
    print('LOS TRES ICONOS LISTOS')

# =====================================================================
#  4 · EL REQUIEM DEL PÚLSAR (28×30)
# =====================================================================
def gen_pulsar():
    img = lienzo()
    # LAS RAMPAS del púlsar: cian estelar ×1.4 overbright + violeta del
    # vacío + el blanco platino del núcleo.
    cian = [(20, 90, 120), (40, 150, 190), (110, 215, 255), (200, 245, 255)]
    viol = [(60, 30, 120), (90, 50, 180), (150, 100, 255), (210, 180, 255)]

    # EL CAYADO: la vara violeta del vacío (el báculo que lanza estrellas)
    for y in range(12, 28):
        t = (y - 12) / 15.0
        tono = mezcla(viol[1], viol[0], t)
        px(img, 13, y, mezcla(viol[2], viol[3], 0.25))
        px(img, 14, y, tono)
    # EL POMO (la perla violeta abajo)
    px(img, 13, 28, viol[3]); px(img, 14, 28, viol[2])

    # LA ESTRELLA DEL PÚLSAR (arriba del cayado — el corazón)
    cx, cy = 13, 8
    r = 4
    for yy in range(cy - r, cy + r + 1):
        for xx in range(cx - r, cx + r + 1):
            d = math.hypot(xx - cx, yy - cy)
            if d <= r:
                if d < 1.6:
                    c = cian[3]       # el blanco platino del núcleo
                elif d < 2.8:
                    c = cian[2]       # el cian overbright
                else:
                    c = cian[1]       # el cian profundo
                px(img, xx, yy, c)

    # LOS DOS FAROS: los haces opuestos en DIAGONAL (el lighthouse):
    # NO-SE y SE (la barra de luz que barre)
    for (dx, dy) in ((1, -1), (-1, 1)):
        for k in range(7):
            x = cx + dx * (2 + k)
            y = cy + dy * (2 + k)
            c = mezcla(cian[2], cian[1], k / 7.0)
            px(img, x, y, c)
            if k < 4:  # el grosor del haz cerca de la estrella
                px(img, x + (1 if dy < 0 else 0), y + (1 if dx > 0 else 0), mezcla(c, cian[3], 0.5))
    # LAS PERLAS DE LAS PUNTAS (donde el faro termina)
    px(img, cx + 9, cy - 9, cian[3]); px(img, cx - 9, cy + 9, cian[3])

    # LOS PULSOS: dos arcos concéntricos cian alrededor de la estrella
    d = ImageDraw.Draw(img)
    d.arc([cx - 7, cy - 7, cx + 7, cy + 7], 100, 170, fill=cian[2] + (255,))
    d.arc([cx - 7, cy - 7, cx + 7, cy + 7], 280, 350, fill=cian[2] + (255,))
    d.arc([cx - 10, cy - 10, cx + 10, cy + 10], 120, 150, fill=cian[1] + (255,))
    d.arc([cx - 10, cy - 10, cx + 10, cy + 10], 300, 330, fill=cian[1] + (255,))

    outline(img)
    img.save(os.path.join(RAIZ, 'AethonMod/Content/Weapons/Cosmic/RequiemDelPulsarStaff.png'))
    print('OK RequiemDelPulsarStaff.png')

