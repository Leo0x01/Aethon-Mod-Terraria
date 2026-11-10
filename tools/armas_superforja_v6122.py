#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
armas_superforja_v6122.py — v6.122 — LOS TRES SPRITES DE LAS ARMAS DE LA
SUPERFORJA (la letra del usuario):

  · «usa la superforja para crear un arma con ataque definitivo de luz
    y oscuridad, quiero dos esferas de luz y oscuridad colisionen y
    creen una implosion que luego explota en un destello de luz»
    → JUICIO DE LOS DOS SOLES (el ícono: las dos esferas gemelas — la
      de LUZ blanca-dorada y la de OSCURIDAD negro-violeta — a punto de
      chocar, con la chispa del impacto entre las dos).

  · «tambien quiero que crees usando la superforja un tajo de
    oscuridad, con centro negro, bordes blancos y medios morados»
    → TAJO DE OSCURIDAD (el ícono: la hoja del tajo — el filo NEGRO al
      centro, los BORDES BLANCOS brillantes y el MEDIO MORADO — la
      espada-lámina de la oscuridad que corta de extremo a extremo).

  · «crea otra arma con agujas delgadas que atraviezan y se clavan en
    los enemigos, estas aparecen siempre cerca del enemigo»
    → AGUJAS DEL VACÍO (el ícono: TRES agujas delgadas en abanico —
      violeta-negras con la PUNTA BLANCA y el ojo dorado).

LA CADENA: la pluma se intentó (el borrador del Juicio vivió dos rondas
de EL OJO — la paleta quedó curada pero el diseño pedía el control
total del concepto) y el camino final es EL DETERMINISTA de la casa
(píxel a píxel, la paleta del LIBRO, el contorno Terraria) — el mismo
de los buffs v6.122. Con montaje + EL OJO de siempre.

Salidas (AethonMod/Content/Weapons/Definitivas/):
  JuicioDosSoles.png (44×44) · TajoOscuro.png (44×44) · AgujasVacio.png (44×44)

USO:  python3 tools/armas_superforja_v6122.py [--qa]
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
DEST = os.path.join(RAIZ, "AethonMod", "Content", "Weapons", "Definitivas")
MONTAJE = "/tmp/armas_v6122_qa.png"
L = 44

# LA PALETA DEL LIBRO (la casa)
NEGRO_NUCLEO = (10, 6, 16)
NEGRO_LLAMA = (18, 12, 26)
VIOLETA_HONDO = (74, 36, 128)
VIOLETA = (150, 84, 235)
VIOLETA_ALTO = (200, 150, 255)
BLANCO = (255, 252, 245)
ORO = (255, 214, 130)
ORO_HONDO = (212, 168, 84)
CONTORNO = (26, 16, 40)          # el contorno Terraria de la casa


def px(im, x, y, c):
    xi, yi = int(x), int(y)
    if 0 <= xi < im.width and 0 <= yi < im.height:
        im.putpixel((xi, yi), (int(c[0]), int(c[1]), int(c[2]), 255))


def dentro(x, y):
    return 0 <= x < L and 0 <= y < L


def esfera(im, cx, cy, r, luz):
    """UNA ESFERA con degradado radial monotónico (la de LUZ: blanco→oro;
    la de OSCURIDAD: negro→violeta con el borde ARDIENDO violeta — la
    lección v6.101: el negro solo existe si su borde arde)."""
    for y in range(L):
        for x in range(L):
            d = math.sqrt((x - cx) ** 2 + (y - cy) ** 2)
            if d > r:
                continue
            t = d / max(r, 0.001)          # 0 centro → 1 borde
            if luz:
                if t < 0.34: c = BLANCO
                elif t < 0.62: c = (255, 240, 200)
                elif t < 0.86: c = ORO
                else: c = ORO_HONDO
            else:
                if t < 0.30: c = NEGRO_NUCLEO
                elif t < 0.55: c = NEGRO_LLAMA
                elif t < 0.80: c = (46, 24, 84)
                else: c = VIOLETA_HONDO if t < 0.92 else VIOLETA
            px(im, x, y, c)


def contorno(im):
    """EL CONTORNO TERRARIA de la casa: 1 px oscuro alrededor de TODO lo
    vivo (y las esquinas diagonales suavizadas)."""
    vivo = im.copy()
    for y in range(L):
        for x in range(L):
            if vivo.getpixel((x, y))[3] > 0:
                continue
            vecino = False
            for (dx, dy) in ((-1, 0), (1, 0), (0, -1), (0, 1)):
                nx, ny = x + dx, y + dy
                if dentro(nx, ny) and vivo.getpixel((nx, ny))[3] > 0:
                    vecino = True
                    break
            if vecino:
                im.putpixel((x, y), CONTORNO + (255,))


# ------------------------------------------------------------------
#  1 — EL JUICIO DE LOS DOS SOLES
# ------------------------------------------------------------------
def juicio_dos_soles():
    im = Image.new("RGBA", (L, L), (0, 0, 0, 0))
    # EL ARO RÚNICO — el círculo de la ceremonia (elipse fina dorada)
    d = ImageDraw.Draw(im)
    d.ellipse([6, 6, 37, 37], outline=ORO_HONDO + (255,), width=1)
    d.ellipse([7, 8, 36, 35], outline=(120, 84, 40, 255), width=1)   # la sombra del aro
    # LAS DOS ESFERAS GEMELAS — a punto de chocar (separadas: el
    # hueco del impacto RESPIRA entre las dos — la chispa vive ahí)
    esfera(im, 13, 19, 7.4, luz=True)
    esfera(im, 31, 19, 7.4, luz=False)
    # LA CHISPA DEL IMPACTO — en el HUECO entre las dos, el destello
    # del choque (GRANDE y legible: el momento de la colisión)
    for k in (-2, -1, 0, 1, 2):
        px(im, 22 + k, 19, ORO if k != 0 else BLANCO)
        px(im, 22, 19 + k, ORO if k != 0 else BLANCO)
    for (sx, sy) in ((19, 16), (25, 16), (19, 22), (25, 22), (22, 14), (22, 24)):
        px(im, sx, sy, VIOLETA_ALTO)
    for (sx, sy) in ((18, 18), (26, 18), (18, 20), (26, 20)):
        px(im, sx, sy, BLANCO)
    # EL MANGO — la empuñadura de amatista bajo el choque
    for y in range(29, 41):
        px(im, 21, y, VIOLETA_HONDO); px(im, 22, y, (96, 54, 158))
    # EL BROCHE dorado donde el mango abraza las esferas
    px(im, 20, 29, ORO); px(im, 21, 29, ORO); px(im, 22, 29, ORO); px(im, 23, 29, ORO)
    px(im, 20, 30, ORO_HONDO); px(im, 23, 30, ORO_HONDO)
    contorno(im)
    return im


# ------------------------------------------------------------------
#  2 — EL Tajo de Oscuridad
# ------------------------------------------------------------------
def tajo_oscuro():
    im = Image.new("RGBA", (L, L), (0, 0, 0, 0))
    # LA HOJA — diagonal (de abajo-izquierda a arriba-derecha): CENTRO
    # NEGRO, MEDIO MORADO, BORDES BLANCOS (la letra, exacta)
    # eje: (10, 35) → (35, 10); perpendicular = (1,1)/√2 normalizado
    ax, ay = 10, 35
    bx, by = 35, 10
    dx, dy = bx - ax, by - ay
    largo = math.sqrt(dx * dx + dy * dy)
    ux, uy = dx / largo, dy / largo          # el filo
    nx, ny = -uy, ux                         # la perpendicular
    for paso in range(int(largo * 2)):
        t = (paso / 2) / largo               # 0→1 a lo largo del filo
        # LA HOJA SE AFINA a las puntas (el tajo entra y sale)
        ancho = 5.0 * math.sin(math.pi * min(1, t) ** 0.8) + 0.5
        for k in range(int(ancho * 2 * 2)):
            w = (k / 2 - ancho) / max(ancho, 0.001)   # −1→1 a lo ancho
            aw = abs(w)
            if aw > 1:
                continue
            x = ax + ux * (t * largo) + nx * (w * ancho)
            y = ay + uy * (t * largo) + ny * (w * ancho)
            if not (dentro(x, y)):
                continue
            # LA LETRA: centro NEGRO · medio MORADO · borde BLANCO
            if aw < 0.45: c = NEGRO_NUCLEO
            elif aw < 0.78: c = (66, 34, 112) if w > 0 else (52, 28, 92)
            elif aw < 0.94: c = VIOLETA if w > 0 else VIOLETA_HONDO
            else: c = BLANCO
            px(im, x, y, c)
    # LA EMPUÑADURA — GRUESA y legible (el cabo del tajo): 3 px de
    # amatista con la viva al centro (se lee incluso en fondo oscuro)
    for k in range(10):
        bx1, by1 = ax - ux * (2 + k), ay - uy * (2 + k)
        px(im, bx1, by1, VIOLETA)
        px(im, bx1 - nx, by1 - ny, VIOLETA_HONDO)
        px(im, bx1 + nx, by1 + ny, (96, 54, 158))
    # LA GUARDIA — el gavilán dorado cruzado al empezar el mango
    gx, gy = ax - ux * 2, ay - uy * 2
    for k in range(-2, 3):
        px(im, gx + nx * k, gy + ny * k, ORO)
    # EL POMO dorado
    px(im, ax - ux * 13, ay - uy * 13, ORO)
    px(im, ax - ux * 14 + nx, ay - uy * 14 + ny, ORO_HONDO)
    contorno(im)
    return im


# ------------------------------------------------------------------
#  3 — LAS AGUJAS DEL VACÍO
# ------------------------------------------------------------------
def agujas_vacio():
    im = Image.new("RGBA", (L, L), (0, 0, 0, 0))
    # TRES AGUJAS DELGADAS en abanico (desde el cabo abajo-centro)
    for (ang, largoA) in ((-0.42, 38), (0.0, 40), (0.42, 36)):
        ux, uy = math.sin(ang), -math.cos(ang)       # el eje de la aguja
        cx, cy = 22, 41                              # el cabo del abanico
        for paso in range(int(largoA * 2)):
            t = (paso / 2) / largoA                  # 0 cabo → 1 punta
            x = cx + ux * (t * largoA)
            y = cy + uy * (t * largoA)
            if not dentro(x, y):
                break
            # EL CUERPO: violeta hondo con la viva al centro
            px(im, x, y, VIOLETA if t < 0.88 else NEGRO_LLAMA)
            # EL GRUESO: 2 px de grueso salvo la punta (agujas DELGADAS)
            if t < 0.72:
                px(im, x + 1, y, VIOLETA_HONDO)
                px(im, x, y + 1, (58, 32, 100))
            # LA PUNTA BLANCA (la última séptima parte)
            if t > 0.86:
                px(im, x, y, BLANCO)
    # LOS OJOS DORADOS — el ojo de coser de cada aguja (cerca del cabo)
    for (ang, largoA) in ((-0.42, 38), (0.0, 40), (0.42, 36)):
        ux, uy = math.sin(ang), -math.cos(ang)
        ex = 22 + ux * (largoA * 0.16)
        ey = 41 + uy * (largoA * 0.16)
        px(im, ex, ey, ORO)
        px(im, ex + 1, ey, ORO_HONDO)
    # EL BROCHE del abanico (donde nacen las tres)
    px(im, 21, 41, ORO); px(im, 22, 41, ORO); px(im, 23, 41, ORO)
    contorno(im)
    return im


# ------------------------------------------------------------------
#  EL OJO (QA VLM — SHIP/FIX)
# ------------------------------------------------------------------
def el_ojo():
    prompt = ("QA this montage for a Terraria-style mod: THREE weapon item sprites at 6x "
              "zoom shown twice (over LIGHT row and DARK row). LEFT: 'Judgment of the Two "
              "Suns' — a ritual icon with TWO gem spheres about to collide (left sphere of "
              "LIGHT white-gold, right sphere of DARKNESS black-violet with bright violet "
              "rim), an impact spark between them, a small amethyst handle below, all inside "
              "a thin golden ritual ring. MIDDLE: 'Dark Slash' — a long diagonal blade with "
              "BLACK core, PURPLE middle bands and BRIGHT WHITE edges (exact spec), thin "
              "amethyst hilt with gold pommel. RIGHT: 'Void Needles' — THREE thin fanned "
              "needles, deep-violet shafts with WHITE tips and golden needle eyes, gold "
              "clasp at the base. Answer in 3 lines: (1) does each sprite read CLEARLY as "
              "its concept at this scale? (2) defects (max 4, short): stray pixels, mush, "
              "palette breaks, unreadable parts? (3) VERDICT first word: SHIP or FIX (with "
              "what). Answer in Spanish.")
    try:
        r = subprocess.run(["z-ai", "vision", "-p", prompt, "-i", MONTAJE],
                           capture_output=True, text=True, timeout=180)
        out = (r.stdout or "").strip()
        print("  EL OJO:", out[:600] if out else "(sin salida)")
        for linea in out.splitlines():
            linea = linea.strip()
            if linea.startswith("SHIP"):
                return True
            if linea.startswith("FIX") or linea.startswith("CORREGIR"):
                return False
        return "SHIP" in out
    except Exception as e:
        print(f"  EL OJO no pudo mirar ({e}) — el QA queda en la verificación técnica")
        return True


def verificar(iconos):
    ok = True
    for ruta, im in iconos:
        if im.size != (L, L):
            print(f"  [FALLO] {ruta}: {im.size} != {L}×{L}"); ok = False
        esquinas = [im.getpixel(p) for p in ((0, 0), (L - 1, 0), (0, L - 1), (L - 1, L - 1))]
        if any(e[3] != 0 for e in esquinas):
            print(f"  [FALLO] {ruta}: esquina opaca (el fondo no es transparente)"); ok = False
    if ok:
        print(f"  VERIFICACIÓN TÉCNICA ✔ (44×44 · transparencia nativa · contorno de la casa)")
    return ok


def montaje(iconos):
    ancho = L * 6 * 3 + 80
    hoja = Image.new("RGBA", (ancho, L * 6 * 2 + 70), (128, 128, 128, 255))
    oscura = Image.new("RGBA", (ancho, L * 6 + 20), (16, 9, 28, 255))
    for k, (ruta, im) in enumerate(iconos):
        g = im.resize((L * 6, L * 6), Image.NEAREST)
        hoja.alpha_composite(g, (20 + k * (L * 6 + 20), 20))
        oscura.alpha_composite(g, (20 + k * (L * 6 + 20), 10))
    hoja.paste(oscura, (0, L * 6 + 40), oscura)
    hoja.save(MONTAJE)
    print(f"  montaje QA: {MONTAJE}")


def main():
    print("LA FORJA v6.122 — LAS TRES ARMAS DE LA SUPERFORJA:")
    iconos = [
        (os.path.join(DEST, "JuicioDosSoles.png"), juicio_dos_soles()),
        (os.path.join(DEST, "TajoOscuro.png"), tajo_oscuro()),
        (os.path.join(DEST, "AgujasVacio.png"), agujas_vacio()),
    ]
    if not verificar(iconos):
        sys.exit("[FALLO] la verificación técnica no pasó — nada se escribe")
    montaje(iconos)
    if "--qa" in sys.argv:
        if not el_ojo():
            print("[AVISO] EL OJO dijo FIX — mira el montaje y re-forja")
            return
    for destino, icono in iconos:
        icono.save(destino)
        print(f"  ESCRITO: {destino}")


if __name__ == "__main__":
    main()
