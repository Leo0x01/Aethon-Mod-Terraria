#!/usr/bin/env python3
# v6.51.5 — LOS ICONOS DE LAS CUATRO ARMAS NUEVAS DE LA SEGUNDA AUDICIÓN
# (la letra: «crea otras armas en consecuencia… recuerda meterle tantos
# efectos chulos como puedas»). Pixel art limpio 30×30 como los dignos
# de la .51.4, con la firma de la casa: lectura legible en 16 px,
# paleta de la familia (oro binario · violeta bruma · hueso) y glow
# aditivo tone-mapped. Iconos:
#   1 · DignosIndiceGrimorio  — el dedo de hueso + la línea de glifos + el rayo
#   2 · DignosLluviaVersos    — la grieta de bruma + la costura dorada + los versos
#   3 · DignosOjoQueJuzga     — el ojo colosal + el iris + el halo del veredicto
#   4 · DignosCorazonMagnetar — la estrella violeta + las líneas de campo
import math, os
from PIL import Image, ImageDraw

S = 30
OUT = os.path.join(os.path.dirname(__file__), "..", "AethonMod", "Content", "Weapons", "Cosmic")

# === LA PALETA DE LA FAMILIA ===
ORO      = (255, 214, 120)
ORO_CLARO= (255, 240, 190)
ORO_PROFO= (196, 140, 48)
VIOLETA  = (178, 108, 240)
VIO_CLARO= (216, 176, 255)
VIO_PROFO= (96, 48, 160)
HUESO    = (235, 226, 205)
SOMBRA   = (24, 14, 38)
TINTA    = (64, 34, 98)
BLANCO   = (255, 252, 240)

def hx(c):
    return "#{:02X}{:02X}{:02X}".format(*c[:3])

def add(base, glow, mask):
    """Composición aditiva tone-mapped (base RGB + glow*mask)."""
    out = Image.new("RGBA", base.size)
    pb, pg = base.load(), out.load()
    pm = mask.load()
    w, h = base.size
    for y in range(h):
        for x in range(w):
            br, bg, bb, ba = pb[x, y]
            m = pm[x, y]
            if m <= 0.0:
                pg[x, y] = (br, bg, bb, ba)
                continue
            r = br + glow[0] * m
            g = bg + glow[1] * m
            b = bb + glow[2] * m
            # tone map suave (reinhard con piso)
            l = max(r, g, b)
            if l > 252.0:
                s = 252.0 / l
                r, g, b = r * s, g * s, b * s
            pg[x, y] = (int(r), int(g), int(b), ba)
    return out

def mascaras(size):
    w, h = size
    m1 = Image.new("F", size, 0.0)
    m2 = Image.new("F", size, 0.0)
    return [m.load() for m in (m1, m2)], (m1, m2)

def guardar(img, nombre):
    # recorte final: fondo transparente fuera del disco
    img.save(os.path.join(OUT, nombre))
    print("OK", nombre)

# ======================================================================
#  1 · EL ÍNDICE DEL GRIMORIO — el dedo de hueso diagonal con su rayo
# ======================================================================
def icono_indice():
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    # EL RAYO DE PERLIN (el fondo del icono: la valla que ya cayó)
    pts = [(21, 2), (18, 9), (21, 10), (14, 18), (16, 19), (9, 28)]
    d.line(pts, fill=hx(ORO_PROFO), width=3)
    d.line(pts, fill=hx(ORO), width=1)

    # EL DEDO DE HUESO (la aguja diagonal, punta abajo-izquierda)
    dedo = [(8, 4), (12, 6), (20, 22), (16, 25), (7, 8)]
    d.polygon(dedo, fill=hx(HUESO))
    # la sombra del dedo
    d.line([(10, 5), (19, 23)], fill="#B9AE93", width=1)

    # LOS GLIFOS de la cita (tres puntitos dorados en arco)
    for gx, gy in [(4, 16), (6, 21), (10, 25)]:
        d.rectangle([gx, gy, gx + 1, gy + 2], fill=hx(ORO_CLARO))

    # EL DESTELLO de la punta (donde clava)
    d.ellipse([13, 22, 19, 28], outline=hx(ORO), width=1)

    # EL GLOW (el lote aditivo de la casa: el rayo arde)
    mask = Image.new("F", (S, S), 0.0)
    dm = mask.load()
    for y in range(S):
        for x in range(S):
            # el rayo brilla
            for px, py in pts:
                dd = math.hypot(x - px, y - py)
                if dd < 4.5:
                    dm[x, y] = max(dm[x, y], (1.0 - dd / 4.5) * 0.55)
            # la punta arde más
            dd2 = math.hypot(x - 16, y - 25)
            if dd2 < 5.5:
                dm[x, y] = max(dm[x, y], (1.0 - dd2 / 5.5) * 0.75)
    img = add(img, (255, 190, 80), mask)
    guardar(img, "DignosIndiceGrimorio.png")

# ======================================================================
#  2 · LA LLUVIA DE VERSOS — la grieta vertical + la costura + versos
# ======================================================================
def icono_lluvia():
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    # LA GRIETA (la lágrima vertical de tinta)
    grieta = [(15, 2), (20, 9), (21, 15), (18, 24), (15, 28), (12, 24), (9, 15), (10, 9)]
    d.polygon(grieta, fill=hx(TINTA))
    d.line([(15, 3), (19, 10), (20, 15), (17, 23), (15, 27)], fill=hx(SOMBRA), width=1)

    # LA COSTURA DORADA (la línea que late dentro)
    d.line([(15, 4), (15, 26)], fill=hx(ORO), width=2)
    d.line([(15, 6), (15, 24)], fill=hx(ORO_CLARO), width=1)

    # LOS VERSOS CAYENDO (cuatro glifos a los lados)
    versos = [(7, 12), (23, 14), (8, 19), (22, 21), (12, 30 - 2)]
    for vx, vy in versos:
        d.rectangle([vx, vy, vx + 1, vy + 2], fill=hx(ORO_CLARO))

    # LAS TAPAS asomando (los bordes del capítulo: arriba y abajo)
    d.rectangle([4, 1, 25, 2], fill=hx(SOMBRA))
    d.rectangle([5, 27, 24, 28], fill=hx(SOMBRA))
    d.line([(4, 1), (25, 1)], fill=hx(ORO_PROFO))
    d.line([(5, 28), (24, 28)], fill=hx(ORO_PROFO))

    mask = Image.new("F", (S, S), 0.0)
    dm = mask.load()
    for y in range(S):
        for x in range(S):
            dd = abs(x - 15)
            if dd < 8.0 and 3 < y < 27:
                dm[x, y] = max(dm[x, y], (1.0 - dd / 8.0) * 0.45 * (0.6 + 0.4 * math.sin(y * 0.5)))
            for vx, vy in versos:
                dv = math.hypot(x - vx, y - vy)
                if dv < 3.0:
                    dm[x, y] = max(dm[x, y], (1.0 - dv / 3.0) * 0.6)
    img = add(img, (255, 200, 100), mask)
    guardar(img, "DignosLluviaVersos.png")

# ======================================================================
#  3 · EL OJO QUE JUZGA — el ojo colosal con su iris clavado
# ======================================================================
def icono_ojo():
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    # EL VELO (las dos cortinas de cine a los lados)
    d.polygon([(0, 4), (5, 8), (4, 22), (0, 26)], fill=hx(TINTA))
    d.polygon([(29, 4), (24, 8), (25, 22), (29, 26)], fill=hx(TINTA))

    # EL OJO (la almendra colosal)
    ojo = [(6, 15), (10, 8), (20, 8), (24, 15), (20, 22), (10, 22)]
    d.polygon(ojo, fill=hx(SOMBRA))
    # el párpado (el borde violeta)
    d.line([(6, 15), (10, 8), (20, 8), (24, 15), (20, 22), (10, 22), (6, 15)],
           fill=hx(VIOLETA), width=1)

    # EL IRIS (clavado hacia abajo-izquierda — juzgando)
    d.ellipse([12, 11, 19, 19], fill=hx(VIO_PROFO))
    d.ellipse([14, 13, 17, 17], fill=hx(VIOLETA))
    d.ellipse([15, 14, 16, 16], fill=hx(BLANCO))

    # EL RAYO DEL VEREDICTO (la mirada saliendo)
    d.line([(13, 17), (5, 26)], fill=hx(VIO_CLARO), width=1)
    d.ellipse([3, 24, 7, 28], outline=hx(VIOLETA), width=1)

    mask = Image.new("F", (S, S), 0.0)
    dm = mask.load()
    for y in range(S):
        for x in range(S):
            dd = math.hypot(x - 15, y - 15)
            if dd < 11.0:
                dm[x, y] = max(dm[x, y], (1.0 - dd / 11.0) * 0.35)
            di = math.hypot(x - 15.5, y - 15)
            if di < 4.0:
                dm[x, y] = max(dm[x, y], (1.0 - di / 4.0) * 0.7)
            dr = math.hypot(x - 5, y - 26)
            if dr < 4.0:
                dm[x, y] = max(dm[x, y], (1.0 - dr / 4.0) * 0.6)
    img = add(img, (190, 130, 255), mask)
    guardar(img, "DignosOjoQueJuzga.png")

# ======================================================================
#  4 · EL CORAZÓN DEL MAGNETAR — la estrella + las líneas de campo
# ======================================================================
def icono_corazon():
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    # LAS LÍNEAS DE CAMPO (los arcos retorcidos del magnetar)
    d.arc([2, 8, 28, 22], 90, 270, fill=hx(VIO_PROFO), width=1)
    d.arc([4, 11, 26, 19], 90, 270, fill=hx(VIOLETA), width=1)
    d.arc([2, 8, 28, 22], 270, 450, fill=hx(VIO_PROFO), width=1)
    d.arc([4, 11, 26, 19], 270, 450, fill=hx(VIOLETA), width=1)

    # EL CORAZÓN (la estrella violeta de 4 puntas — el faro)
    cx, cy = 15, 15
    cora = []
    for i in range(8):
        ang = i * math.pi / 4 - math.pi / 2
        r = 10.0 if i % 2 == 0 else 3.6
        cora.append((cx + r * math.cos(ang), cy + r * math.sin(ang)))
    d.polygon(cora, fill=hx(VIOLETA))
    # el núcleo blanco-violeta
    cora2 = []
    for i in range(8):
        ang = i * math.pi / 4 - math.pi / 2
        r = 5.4 if i % 2 == 0 else 2.2
        cora2.append((cx + r * math.cos(ang), cy + r * math.sin(ang)))
    d.polygon(cora2, fill=hx(VIO_CLARO))
    d.ellipse([13, 13, 17, 17], fill=hx(BLANCO))

    # EL TETHER (el hilo que lo ata a su dueño)
    d.line([(24, 24), (28, 28)], fill=hx(VIOLETA), width=1)

    # LAS CHISPAS (las descargas de las cadenas)
    for sx, sy in [(5, 5), (25, 6), (4, 24)]:
        d.rectangle([sx, sy, sx, sy], fill=hx(VIO_CLARO))

    mask = Image.new("F", (S, S), 0.0)
    dm = mask.load()
    for y in range(S):
        for x in range(S):
            dd = math.hypot(x - 15, y - 15)
            if dd < 12.0:
                dm[x, y] = max(dm[x, y], (1.0 - dd / 12.0) * 0.5)
            dn = math.hypot(x - 15, y - 15)
            if dn < 4.0:
                dm[x, y] = max(dm[x, y], (1.0 - dn / 4.0) * 0.85)
            for sx, sy in [(5, 5), (25, 6), (4, 24)]:
                ds = math.hypot(x - sx, y - sy)
                if ds < 3.0:
                    dm[x, y] = max(dm[x, y], (1.0 - ds / 3.0) * 0.6)
    img = add(img, (200, 150, 255), mask)
    guardar(img, "DignosCorazonMagnetar.png")

if __name__ == "__main__":
    icono_indice()
    icono_lluvia()
    icono_ojo()
    icono_corazon()
    print("4/4 iconos de la segunda audición")
