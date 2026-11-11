#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
superforja_v6124.py — v6.124 — LA SUPERFORJA DE LA SESIÓN (la letra del
usuario):

  · «entonces crea un nuevo tajo y para eso debes usar un pequeño haz
    usando nuestros hazes, el inicio y final del tajo tipo haz sera muy
    fino en el centro se vuelve mas grueso, y como dije antes los
    colores son negro en el centro, morado en los bordes»
    → EL TAJO DEL VACÍO (el ícono: el HUSO — la lágrima de haz fina en
      los extremos y LLena al centro, NEGRO al corazón, MORADO el filo,
      el arma de HazLib).

  · «investiga y copia al 100% todo sobre el arma Arbitrator II en
    CALAMITY ENTROPY, esto sera usado como investigacion»
    → EL ARBITRADOR II (el ícono: LA JABALINA — la lanza violeta de la
      Entropía que atraviesa muros y estalla; su punta con la MARCA de
      la X del sigiloso).

  · «copia el arma the beginning and the end de CALAMITY ENTROPY y crea
    una version con su codigo, sprite y textura al 100% como
    investigacion, luego daselas al jugador»
    → EL PRINCIPIO Y EL FIN (el ícono: LAS DOS DAGAS CRUZADAS — la de
      ORO «El Principio» y la AZUL «El Fin», espalda con espalda con el
      destello del canje de almas entre las dos).

  · LOS ICONOS DE LOS BUFFS DE LAS INVESTIGACIONES (opacos, estilo
    Terraria — la regla de la casa): LAS LLAMAS SAGRADAS (la llama
    blanca-dorada que disuelve), LA MARCA DEL PRINCIPIO (laruna dorada
    que arde), LA MIRADA DEL FIN (el ojo azul del abismo), LA SUPRESIÓN
    VITAL (la calavera de la muerte vista) y LA SERVIDUMBRE (el ala
    dorada sirviente).

LA CADENA: EL DETERMINISTA de la casa (píxel a píxel, la paleta del
LIBRO, el contorno Terraria) — el mismo de las armas v6.122 y los buffs
v6.122. Con montaje + EL OJO de siempre.

Salidas:
  Weapons/Definitivas/TajoDelVacio.png (44×44)
  Weapons/Investigaciones/ArbitradorII.png (44×44)
  Weapons/Investigaciones/ElPrincipioYElFin.png (44×44)
  Buffs/Investigaciones/LlamasSagradas.png (32×32)
  Buffs/Investigaciones/MarcaDelPrincipio.png (32×32)
  Buffs/Investigaciones/MiradaDelFin.png (32×32)
  Buffs/Investigaciones/SupresionVital.png (32×32)
  Buffs/Investigaciones/Servidumbre.png (32×32)

USO:  python3 tools/superforja_v6124.py [--qa]
"""
import math
import os
import subprocess
import sys

try:
    from PIL import Image, ImageDraw
except ImportError:
    sys.exit("Falta Pillow: pip install Pillow")

RAIZ = os.path.join(os.path.dirname(__file__), "..")
DEST_DEF = os.path.join(RAIZ, "AethonMod", "Content", "Weapons", "Definitivas")
DEST_INV = os.path.join(RAIZ, "AethonMod", "Content", "Weapons", "Investigaciones")
DEST_BUFF = os.path.join(RAIZ, "AethonMod", "Content", "Buffs", "Investigaciones")
MONTAJE = "/tmp/superforja_v6124_qa.png"

L = 44          # los items de la casa
B = 32          # los iconos de buff de la casa

# LA PALETA DEL LIBRO (la casa)
NEGRO_NUCLEO = (8, 5, 14)
NEGRO_LLAMA = (18, 12, 26)
VIOLETA_HONDO = (74, 36, 128)
VIOLETA = (150, 84, 235)
VIOLETA_ALTO = (200, 150, 255)
MORADO_MEDIA = (108, 62, 182)
BLANCO = (255, 252, 245)
ORO = (255, 214, 130)
ORO_HONDO = (212, 168, 84)
AZUL = (110, 170, 255)
AZUL_HONDO = (52, 96, 178)
AZUL_ALTO = (190, 224, 255)
CONTORNO = (26, 16, 40)          # el contorno Terraria de la casa


def px(im, x, y, c):
    xi, yi = int(x), int(y)
    if 0 <= xi < im.width and 0 <= yi < im.height:
        im.putpixel((xi, yi), (int(c[0]), int(c[1]), int(c[2]), 255))


def contorno(im):
    """EL CONTORNO TERRARIA de la casa alrededor de TODO lo vivo."""
    vivo = im.copy()
    for y in range(im.height):
        for x in range(im.width):
            if vivo.getpixel((x, y))[3] > 0:
                continue
            vecino = False
            for (dx, dy) in ((-1, 0), (1, 0), (0, -1), (0, 1)):
                nx, ny = x + dx, y + dy
                if 0 <= nx < im.width and 0 <= ny < im.height and \
                        vivo.getpixel((nx, ny))[3] > 0:
                    vecino = True
                    break
            if vecino:
                im.putpixel((x, y), CONTORNO + (255,))


# ------------------------------------------------------------------
#  1 — EL TAJO DEL VACÍO (el HUSO: fino en los extremos, lleno al
#      centro; NEGRO al corazón, MORADO el filo — la letra, EXACTA)
# ------------------------------------------------------------------
def tajo_del_vacio():
    im = Image.new("RGBA", (L, L), (0, 0, 0, 0))
    ax, ay = 8, 36            # el rabo (abajo-izquierda)
    bx, by = 37, 7            # la punta (arriba-derecha)
    dx, dy = bx - ax, by - ay
    largo = math.sqrt(dx * dx + dy * dy)
    ux, uy = dx / largo, dy / largo          # el eje del haz
    nx, ny = -uy, ux                         # la perpendicular
    for paso in range(int(largo * 3)):
        t = (paso / 3) / largo               # 0→1 a lo largo del eje
        # LA LEY DEL HUSO (la letra): MUY FINO en el inicio y el final,
        # GRUESO en el centro — seno^0.75 (la misma ley del arma).
        ancho = 6.4 * math.pow(max(math.sin(math.pi * t), 0.0), 0.75) + 0.4
        for k in range(int(ancho * 2 * 3)):
            w = (k / 3 - ancho) / max(ancho, 0.001)   # −1→1 a lo ancho
            aw = abs(w)
            if aw > 1:
                continue
            x = ax + ux * (t * largo) + nx * (w * ancho)
            y = ay + uy * (t * largo) + ny * (w * ancho)
            # LA LETRA DE COLOR: NEGRO en el CENTRO, MORADO en los BORDES
            if aw < 0.44: c = NEGRO_NUCLEO
            elif aw < 0.72: c = (46, 24, 86)
            elif aw < 0.9: c = MORADO_MEDIA if w > 0 else VIOLETA_HONDO
            else: c = VIOLETA if w > 0 else VIOLETA_ALTO
            px(im, x, y, c)
    # EL DESTELLO DE LA PUNTA — la aguja que se clava (la chispa blanca
    # del extremo fino + las motas del clavado)
    for k in ((0, 0), (1, 0), (-1, 0), (0, 1), (0, -1)):
        px(im, bx + k[0], by + k[1], BLANCO if k == (0, 0) else VIOLETA_ALTO)
    # LAS MOTAS DEL VACÍO alrededor del huso (la firma del goteo)
    for (sx, sy) in ((14, 30), (20, 25), (26, 19), (31, 14), (17, 33), (29, 12)):
        px(im, sx, sy, VIOLETA)
    # LA EMPUÑADURA — el mango corto de amatista al rabo
    for k in range(5):
        hx, hy = ax - ux * (1 + k), ay - uy * (1 + k)
        px(im, hx, hy, VIOLETA_HONDO)
        px(im, hx + nx, hy + ny, (96, 54, 158))
    contorno(im)
    return im


# ------------------------------------------------------------------
#  2 — EL ARBITRADOR II (la JABALINA de la Entropía que atraviesa
#      muros; su punta carga la X del sigiloso)
# ------------------------------------------------------------------
def arbitrador_ii():
    im = Image.new("RGBA", (L, L), (0, 0, 0, 0))
    ax, ay = 6, 38            # el rabo de la lanza
    bx, by = 36, 9            # la punta
    dx, dy = bx - ax, by - ay
    largo = math.sqrt(dx * dx + dy * dy)
    ux, uy = dx / largo, dy / largo
    nx, ny = -uy, ux
    # EL ASTA — la caña violeta (fina, pareja, con la viva al borde)
    for paso in range(int(largo * 3)):
        t = (paso / 3) / largo
        if t > 0.88:
            break                       # la punta vive aparte
        x = ax + ux * (t * largo)
        y = ay + uy * (t * largo)
        px(im, x, y, VIOLETA_HONDO)
        px(im, x + nx, y + ny, MORADO_MEDIA)
        px(im, x - nx, y - ny, NEGRO_LLAMA)
    # EL REGATÓN — el contrapeso del rabo
    for k in ((0, 0), (1, 0), (0, 1)):
        px(im, ax - ux - k[0], ay - uy - k[1], ORO_HONDO)
    # LA PUNTA — la hoja de la jabalina: GRUESA al abrirse y fina al
    # filo (la misma ley del huso, corta — la cabeza que ESTALLA)
    for paso in range(24):
        t = paso / 24
        x = ax + ux * (largo * (0.80 + 0.20 * t))
        y = ay + uy * (largo * (0.80 + 0.20 * t))
        ancho = 3.6 * math.pow(max(math.sin(math.pi * t), 0.0), 0.6) + 0.4
        for k in range(int(ancho * 2 * 3)):
            w = (k / 3 - ancho) / max(ancho, 0.001)
            aw = abs(w)
            if aw > 1:
                continue
            px(im, x + nx * (w * ancho), y + ny * (w * ancho),
               BLANCO if aw > 0.8 else (VIOLETA_ALTO if aw > 0.5 else VIOLETA_HONDO))
    # LA MARCA DE LA X — la herida del sigiloso: la X pequeña que arde
    # junto a la punta (la explosión en X de 1 segundo)
    cx, cy = 31, 20
    for k in range(4):
        px(im, cx - k, cy - k, ORO)
        px(im, cx + k, cy - k, ORO)
        px(im, cx - k, cy + k, ORO)
        px(im, cx + k, cy + k, ORO)
    px(im, cx, cy, BLANCO)
    # LAS MOTAS DE LA EXPLOSIÓN (el estallido que promete)
    for (sx, sy) in ((26, 16), (35, 24), (28, 26)):
        px(im, sx, sy, VIOLETA_ALTO)
    contorno(im)
    return im


# ------------------------------------------------------------------
#  3 — EL PRINCIPIO Y EL FIN (las dos dagas cruzadas: la ORO y la
#      AZUL, con el destello del canje entre las dos)
# ------------------------------------------------------------------
def _daga(im, cx, cy, ang, cuerpo, filo, brillo, tinte):
    """UNA DAGA diagonal (45°): hoja corta + guarda + mango."""
    ux, uy = math.cos(ang), math.sin(ang)
    nx, ny = -uy, ux
    # LA HOJA — la ley del huso corta (fina al filo, llena al centro)
    for paso in range(40):
        t = paso / 40
        x = cx + ux * t * 17
        y = cy + uy * t * 17
        ancho = 2.8 * math.pow(max(math.sin(math.pi * t), 0.0), 0.5) + 0.35
        for k in range(int(ancho * 2 * 3)):
            w = (k / 3 - ancho) / max(ancho, 0.001)
            aw = abs(w)
            if aw > 1:
                continue
            c = brillo if aw > 0.72 else (filo if aw > 0.4 else cuerpo)
            px(im, x + nx * (w * ancho), y + ny * (w * ancho), c)
    # LA PUNTA — el destello del filo
    px(im, cx + ux * 17.5, cy + uy * 17.5, brillo)
    # LA GUARDA — el gavilán cruzado
    for k in range(-2, 3):
        px(im, cx - ux * 1 + nx * k, cy - uy * 1 + ny * k, tinte)
    # EL MANGO — la empuñadura corta
    for k in range(2, 8):
        px(im, cx - ux * k, cy - uy * k, cuerpo)
    # EL POMO
    px(im, cx - ux * 8, cy - uy * 8, tinte)


def el_principio_y_el_fin():
    im = Image.new("RGBA", (L, L), (0, 0, 0, 0))
    # LA DAGA DE ORO — «El Principio» (arriba-izquierda → abajo-derecha)
    _daga(im, 20, 20, math.pi / 4, ORO_HONDO, ORO, BLANCO, ORO)
    # LA DAGA AZUL — «El Fin» (arriba-derecha → abajo-izquierda)
    _daga(im, 24, 20, math.pi * 3 / 4, AZUL_HONDO, AZUL, AZUL_ALTO, AZUL)
    # EL DESTELLO DEL CANJE — la chispa donde las hojas se CRUZAN (el
    # momento en que una marca libera sus almas)
    for k in ((0, 0), (1, 0), (-1, 0), (0, 1), (0, -1), (2, 0), (-2, 0), (0, 2), (0, -2)):
        px(im, 22 + k[0], 19 + k[1], BLANCO if k == (0, 0) else ORO)
    # LAS ALMAS — dos motas (la de ECO azul y la de LUZ INFERIOR dorada)
    px(im, 14, 12, AZUL_ALTO); px(im, 15, 12, AZUL)
    px(im, 30, 12, ORO); px(im, 29, 12, ORO_HONDO)
    px(im, 13, 26, AZUL); px(im, 30, 27, VIOLETA_ALTO)
    contorno(im)
    return im


# ------------------------------------------------------------------
#  LOS ICONOS DE LOS BUFFS (opacos, 32×32, estilo Terraria)
# ------------------------------------------------------------------
def icono_llamas_sagradas():
    """LAS LLAMAS SAGRADAS: la llama blanca-dorada que DISUELVE hacia
    arriba (la copia local del Holy Flames de Calamity)."""
    im = Image.new("RGBA", (B, B), (0, 0, 0, 0))
    cx = 16
    # EL CUERPO DE LA LLAMA — la silueta de OnFire (tres lenguas)
    for y in range(28, 3, -1):
        t = (28 - y) / 24.0               # 0 abajo → 1 arriba
        # la anchura de la llama (la cintura y las lenguas)
        ancho = 9 * math.sin(math.pi * min(1, t * 1.15)) * (1 - 0.35 * t) + \
            3 * math.sin(t * math.pi * 3 + 1.2) + 1.2
        for k in range(int(ancho * 2 * 2)):
            w = (k / 2 - ancho) / max(ancho, 0.001)
            aw = abs(w)
            if aw > 1:
                continue
            x = cx + w * ancho + math.sin(t * 5.5) * 1.8
            # LA PALETA SAGRADA: el CORAZÓN blanco-dorado que disuelve
            if aw < 0.4 and t < 0.5: c = BLANCO
            elif aw < 0.62: c = (255, 244, 205)
            elif aw < 0.85: c = ORO
            else: c = ORO_HONDO
            px(im, x, y, c)
    # EL NÚCLEO — el corazón blanco que arde al centro
    for (sx, sy) in ((15, 20), (16, 20), (15, 21), (16, 21), (15, 22), (16, 22), (17, 21)):
        px(im, sx, sy, BLANCO)
    contorno(im)
    return im


def icono_marca_principio():
    """LA MARCA DEL PRINCIPIO: la runa dorada que ARDE (la marca de la
    daga de oro — las 8 capas)."""
    im = Image.new("RGBA", (B, B), (0, 0, 0, 0))
    # EL ROMBO RUNA — la marca grabada (el sello de la casa)
    for y in range(B):
        for x in range(B):
            d = abs(x - 16) + abs(y - 16)
            if d <= 10:
                if d >= 8: c = ORO_HONDO
                elif d >= 5: c = ORO
                else: c = (255, 244, 205)
                px(im, x, y, c)
    # EL GLIFO — la runa de la casa (el trazo del principio: | y los
    # dos brazos ascendentes — la puerta que se abre)
    for y in range(9, 24):
        px(im, 16, y, ORO_HONDO)
        px(im, 15, y, (200, 158, 90))
    for k in range(5):
        px(im, 16 - k, 10 + k, ORO_HONDO)
        px(im, 16 + k, 10 + k, ORO_HONDO)
    # EL ARDOR — las motas de la marca que arde
    for (sx, sy) in ((8, 8), (24, 8), (7, 20), (25, 20), (11, 26), (21, 26)):
        px(im, sx, sy, BLANCO)
    contorno(im)
    return im


def icono_mirada_fin():
    """LA MIRADA DEL FIN: el OJO AZUL del abismo (la mirada de la daga
    azul — el ojo que te ve morir)."""
    im = Image.new("RGBA", (B, B), (0, 0, 0, 0))
    # EL OJO — la almendra (el ojo de la casa: la familia del Ojo Eterno)
    for y in range(B):
        for x in range(B):
            dxn = (x - 16) / 12.0
            dyn = (y - 16) / 8.5
            d = math.sqrt(dxn * dxn + dyn * dyn)
            if d <= 1.0:
                if d > 0.88: c = AZUL_HONDO
                elif d > 0.6: c = AZUL
                else: c = NEGRO_LLAMA
                px(im, x, y, c)
    # EL IRIS — el anillo azul hondo
    for y in range(B):
        for x in range(B):
            d = math.sqrt((x - 16) ** 2 + (y - 16) ** 2)
            if 3.4 <= d <= 5.6:
                px(im, x, y, AZUL_HONDO if d > 4.6 else AZUL)
            elif d < 3.4:
                px(im, x, y, (16, 26, 52))
    # LA PUPILA — el punto negro del abismo (te ve)
    px(im, 15, 15, NEGRO_NUCLEO); px(im, 16, 15, NEGRO_NUCLEO)
    px(im, 15, 16, NEGRO_NUCLEO); px(im, 16, 16, NEGRO_NUCLEO)
    # EL DESTELLO — la luz fría del abismo
    px(im, 14, 14, AZUL_ALTO)
    contorno(im)
    return im


def icono_supresion_vital():
    """LA SUPRESIÓN VITAL: la calavera de la MUERTE VISTA («has visto
    tu muerte» — la ficha del original)."""
    im = Image.new("RGBA", (B, B), (0, 0, 0, 0))
    # LA CALAVERA — el cráneo violeta-negro de la casa
    for y in range(B):
        for x in range(B):
            dxn = (x - 16) / 9.5
            dyn = (y - 15) / 8.5 if y < 18 else (y - 15) / 10.5
            d = dxn * dxn + dyn * dyn
            if d <= 1.0:
                c = VIOLETA_HONDO if d > 0.62 else NEGRO_LLAMA
                px(im, x, y, c)
    # LAS CUENCAS — los ojos vacíos que ya vieron
    for (ox, oy) in ((12, 14), (20, 14)):
        for k in range(3):
            px(im, ox - 1 + k, oy, NEGRO_NUCLEO)
            px(im, ox - 1 + k, oy + 1, NEGRO_NUCLEO)
        px(im, ox, oy - 1, NEGRO_NUCLEO)
    # LA NARIZ — la fosa invertida
    px(im, 16, 18, NEGRO_NUCLEO)
    # LA MANDÍBULA — los dientes del silencio
    for k in range(-3, 4):
        px(im, 16 + k * 2, 22, VIOLETA_HONDO)
        px(im, 16 + k * 2, 23, NEGRO_LLAMA)
    # EL VACÍO QUE BAJA — las motas de la vida que se rinde
    for (sx, sy) in ((8, 6), (24, 7), (6, 24), (26, 23), (16, 3)):
        px(im, sx, sy, VIOLETA)
    contorno(im)
    return im


def icono_servidumbre():
    """LA SERVIDUMBRE: el ALA DORADA sirviente (el alma de luz inferior
    que sirve al portador — +daño, +velocidad, +crítico)."""
    im = Image.new("RGBA", (B, B), (0, 0, 0, 0))
    # EL ALA — las tres plumas doradas que se pliegan al servicio
    for y in range(B):
        for x in range(B):
            dxn = (x - 12) / 13.0
            dyn = (y - 20) / 13.0
            d = math.sqrt(dxn * dxn + dyn * dyn)
            if d <= 1.0 and y <= 20 + (x - 12) * 0.35:
                # las bandas de las plumas
                banda = (d * 3.2) % 1.0
                if d > 0.9: c = ORO_HONDO
                elif banda > 0.72: c = ORO_HONDO
                elif banda > 0.45: c = ORO
                else: c = (255, 240, 195)
                px(im, x, y, c)
    # EL OJO DEL ALMA — la chispa que mira al portador
    px(im, 12, 16, BLANCO); px(im, 13, 16, ORO_HONDO)
    # LAS MOTAS DEL REGALO — las chispas del servicio
    for (sx, sy) in ((20, 8), (25, 13), (8, 8), (23, 20)):
        px(im, sx, sy, BLANCO)
    contorno(im)
    return im


# ------------------------------------------------------------------
#  LA VERIFICACIÓN + EL MONTAJE (EL OJO de la casa)
# ------------------------------------------------------------------
def verificar(iconos):
    ok = True
    for ruta, im in iconos:
        nombre = os.path.basename(ruta)
        if im.width == 0 or im.height == 0:
            print(f"  ✗ {nombre}: VACÍO")
            ok = False
            continue
        alfa = sum(1 for p in im.getdata() if p[3] > 0)
        cubre = alfa / (im.width * im.height)
        # LA REGLA DE LOS ICONOS DE BUFF: OPACOS (la letra v6.122 — el
        # interior LLENO, nada de transparencias sucias)
        print(f"  · {nombre}: {im.width}×{im.height}, cobertura {cubre:.0%}")
    return ok


def el_ojo(montaje):
    """EL OJO de la casa (el VLM de siempre — el control de calidad)."""
    try:
        r = subprocess.run(
            ["node", "/home/z/my-project/mod/tools/vlm_emperatriz_v65099.mjs",
             montaje],
            capture_output=True, text=True, timeout=120)
        print("EL OJO dice:")
        print(r.stdout[-1200:] if r.stdout else r.stderr[-600:])
    except Exception as e:
        print(f"(EL OJO no disponible: {e})")


def main():
    qa = "--qa" in sys.argv
    trabajos = [
        (os.path.join(DEST_DEF, "TajoDelVacio.png"), tajo_del_vacio()),
        (os.path.join(DEST_INV, "ArbitradorII.png"), arbitrador_ii()),
        (os.path.join(DEST_INV, "ElPrincipioYElFin.png"), el_principio_y_el_fin()),
        (os.path.join(DEST_BUFF, "LlamasSagradas.png"), icono_llamas_sagradas()),
        (os.path.join(DEST_BUFF, "MarcaDelPrincipio.png"), icono_marca_principio()),
        (os.path.join(DEST_BUFF, "MiradaDelFin.png"), icono_mirada_fin()),
        (os.path.join(DEST_BUFF, "SupresionVital.png"), icono_supresion_vital()),
        (os.path.join(DEST_BUFF, "Servidumbre.png"), icono_servidumbre()),
    ]

    print("LA SUPERFORJA v6.124 — las armas y los iconos de la sesión:")
    if not verificar(trabajos):
        sys.exit("HAY ICONOS VACÍOS — la forja no entrega")

    for ruta, im in trabajos:
        im.save(ruta)
        print(f"  ✓ {os.path.relpath(ruta, RAIZ)}")

    # EL MONTAJE para EL OJO (una tira con todo)
    ancho = sum(im.width for _, im in trabajos) + 10 * (len(trabajos) + 1)
    alto = max(im.height for _, im in trabajos) + 20
    m = Image.new("RGBA", (ancho, alto), (34, 24, 52, 255))
    x = 10
    for _, im in trabajos:
        m.paste(im, (x, 10), im)
        x += im.width + 10
    m.save(MONTAJE)
    print(f"  ✓ montaje: {MONTAJE}")

    if qa:
        el_ojo(MONTAJE)
    print("LA FORGA ENTREGA COMPLETA.")


if __name__ == "__main__":
    main()
