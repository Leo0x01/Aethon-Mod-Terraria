#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
altar_v6110.py — v6.110 — EL ALTAR DE UNA ENTIDAD CÓSMICA.

LA LETRA DEL USUARIO: «tambien debes cambiar las texturas del altar,
regenera la textura para crear un altar digno de una entidad cosmica».

LA AUTOPSIA DEL ALTAR v6.101 (EL OJO VLM, 6/10): la galaxia animada
convencía pero la ESTRUCTURA era genérica — planos oscuros FLAT sin
volumen, contorno uniforme, velas placeholder, gemas sin bisel, la
galaxia «pegada» dentro del marco como un cuadro y sin brillo emisivo.

LA RECETA CÓSMICA v6.110 (las cinco curas del veredicto):
  1. EL GLOW EMISIVO — la galaxia INUNDA su cámara: halo radial
     violeta-magenta alrededor del vórtice y el BORDE INTERIOR del
     marco TEÑIDO de morado (la luz que escapa del portal).
  2. EL VOLUMEN — la piedra con sombreado direccional: luz cenital
     fría arriba-izquierda, RIM VIOLETA en las caras que miran al
     portal (la galaxia las alumbra), penumbra creciente hacia abajo.
  3. LA LUZ DE LAS VELAS — las llamas cálidas TIÑEN la piedra vecina
     (radio de 3-4 px con caída real) — dos fuentes de luz vivas.
  4. LA GALAXIA QUE ESCAPA — estrellitas deterministas DRIFTANDO fuera
     del marco del portal (el cosmos no cabe en la caja).
  5. EL OUTLINE VIVO — el contorno VARÍA: hondo abajo, teñido de
     violeta junto al portal, frío arriba — nada de borde uniforme.

Y LAS JOYAS: cada gema con SU bisel (highlight + sombra), el oro de
las bandas con su vena clara, las velas de hueso con cera modelada.

LA ANIMACIÓN (6 marcos, 7 ticks por marco — el latido ritual):
  · EL VÓRTICE girando 60°/marco (el ciclo completo de la espiral).
  · LA ESTRELLA GUÍA del chapitel pulsando.
  · LAS VELAS en contrafase (llamas gordas, núcleo caliente).
  · LAS RUNAS en persecución + los faroles alternos.
  · LA CHISPA DE CONSAGRACIÓN subiendo por la cámara.
  · LAS ESTRELLAS FUGITIVAS driftando fuera del marco.

REGLA PERMANENTE: transparencia NATIVA (lienzo alfa=0, solo el ARTE
pinta) y el MISMO MAPEO de la v6.101 (34×300, 6 marcos de 50 px,
arte hasta la última fila — el altar asentado en el suelo).

Salidas:
  Content/Tiles/AltarOfrendasTile.png          (34×300)
  Content/Tiles/AltarOfrendasTile_Highlight.png (34×50 — el contorno)
  Content/Items/Altar/AltarOfrendasItem.png     (34×50 — el marco 0)
"""
from PIL import Image
import math
import os

W, H = 34, 50
MARCOS = 6
CX = 16.5

# ---------------------------------------------------------------- PALETA
# LA PIEDRA DEL VACÍO (trío hondo con caída vertical).
NEGRO_FONDO = (7, 5, 18)
PIEDRA_HONDA = (11, 9, 30)
PIEDRA = (14, 12, 38)
PIEDRA_MEDIA = (19, 17, 50)
PIEDRA_ALTA = (27, 26, 72)
PIEDRA_CENITAL = (38, 40, 96)

# EL ORO CÓSMICO (el marco de la entidad).
ORO = (255, 202, 92)
ORO_CLARO = (255, 232, 160)
ORO_HONDO = (188, 128, 38)
ORO_SOMBRA = (128, 84, 26)

# EL VÓRTICE (la galaxia interior).
MORADO = (150, 62, 235)
MORADO_HONDO = (168, 44, 150)
MAGENTA = (255, 120, 224)
MAGENTA_CALIENTE = (255, 180, 240)
MORADO_OSCURO = (52, 22, 96)
RIM_VIOLETA = (124, 72, 190)

# LAS VELAS (las dos llamas cálidas).
LLAMA_PUNTA = (255, 240, 180)
LLAMA = (255, 204, 88)
LLAMA_MEDIA = (216, 144, 48)
PORTALLAMAS = (150, 96, 30)
VELA_CUERPO = (228, 222, 202)
VELA_LUZ = (246, 242, 228)
VELA_SOMBRA = (150, 140, 122)

# LAS RUNAS Y FAROLES (la casa: teal vivo).
TEAL_RUNA = (72, 216, 168)
TEAL_LINTERNA = (90, 220, 180)
CIAN_CHISPA = (150, 240, 250)
TEAL_TRABAJO = (24, 48, 110)


def px(im, x, y, c):
    x, y = int(x), int(y)
    if 0 <= x < W and 0 <= y < H:
        im.putpixel((x, y), c)


def mezcla(a, b, k):
    return (int(a[0] + (b[0] - a[0]) * k),
            int(a[1] + (b[1] - a[1]) * k),
            int(a[2] + (b[2] - a[2]) * k))


def dentro(x, y):
    """LA SILUETA (el mapeo probado de la v6.101 — el recorte vertical
    queda byte-perfecto: arte hasta la ÚLTIMA fila)."""
    if y >= 41:                       # EL PEDESTAL (ancho completo)
        return 0 <= x < W
    if y >= 32:                       # LA REPISA DE LAS VELAS
        return 1 <= x < W - 1
    if y >= 10:                       # LA CÁMARA (pilares a los lados)
        return 5 <= x <= 29
    if y >= 5:                        # LOS HOMBROS DEL ARCO
        return 7 + (10 - y) // 2 <= x <= 26 - (10 - y) // 2
    half = 5 + y                      # LA CÚPULA (filas 0-4)
    return CX - half <= x <= CX + half


# ------------------------------------------------------------ EL CUERPO
def cuerpo_piedra(im):
    """LA PIEDRA CON VOLUMEN DE VERDAD (la cura #2 y #5): luz cenital
    fría desde arriba-izquierda, penumbra creciente hacia abajo y el
    OUTLINE VIVO (hondo abajo, nada uniforme)."""
    for y in range(H):
        for x in range(W):
            if not dentro(x, y):
                continue
            # LA CAÍDA VERTICAL (la base más honda — la gravedad).
            k = y / float(H)
            c = mezcla(PIEDRA_MEDIA, PIEDRA_HONDA, k * 0.9)
            # LA CARA CENITAL (arriba-izquierda — el cielo frío).
            if not dentro(x, y - 1) or not dentro(x - 1, y):
                c = mezcla(PIEDRA_CENITAL, PIEDRA_ALTA, k) if y < 26 else PIEDRA_ALTA
            # LA PENUMBRA INFERIOR de cada masa.
            elif not dentro(x + 1, y) or not dentro(x, y + 1):
                c = mezcla(PIEDRA_HONDA, NEGRO_FONDO, k * 0.7)
            # EL MOTEADO sutil de la piedra (vetas deterministas).
            v = ((x * 7 + y * 13) % 11) / 11.0
            if v < 0.14 and dentro(x, y + 1) and dentro(x, y - 1):
                c = mezcla(c, PIEDRA_HONDA, 0.5)
            # EL OUTLINE VIVO (la cura #5): el borde EXTERIOR varía —
            # hondo abajo, frío arriba, NUNCA uniforme.
            if (not dentro(x - 1, y) or not dentro(x + 1, y) or
                    not dentro(x, y - 1) or not dentro(x, y + 1)):
                if y >= 34:
                    c = mezcla(NEGRO_FONDO, PIEDRA_HONDA, 0.3)
                elif y >= 12:
                    c = mezcla(PIEDRA_HONDA, RIM_VIOLETA, 0.18)
                else:
                    c = mezcla(PIEDRA_HONDA, PIEDRA_MEDIA, 0.4)
            px(im, x, y, c)


def trim_oro(im):
    """EL ORO CÓSMICO (las líneas maestras): la CORONA del arco, las
    molduras de la cámara y las bandas del pedestal — cada línea con
    SU vena clara y SU sombra (el bisel del oro)."""
    for y in range(H):
        for x in range(W):
            if not dentro(x, y):
                continue
            if y in (9, 10) and dentro(x, y + 1) and dentro(x, y - 1):
                # LA CORONA DEL ARCO (la moldura maestra).
                c = ORO if (x % 2 == 0) else ORO_HONDO
                if y == 9:
                    c = ORO_CLARO if x % 2 == 0 else ORO
                px(im, x, y, c)
            elif y in (31, 32) and dentro(x, y + 1) and dentro(x, y - 1):
                # LA MOLDURA DE LA REPISA.
                c = ORO if (x % 2 == 0) else ORO_HONDO
                if y == 31:
                    c = ORO_CLARO if x % 2 == 0 else ORO
                px(im, x, y, c)


def chapitel(im, f):
    """EL CHAPITEL ESTELAR (la cima: la piedra angular con SU ESTRELLA
    GUÍA pulsando — el faro de la entidad, blanco-violeta latiendo)."""
    latido = 0.5 + 0.5 * math.sin(f / MARCOS * 2 * math.pi)
    # LA PIEDRA ANGULAR (el bisel de la cima).
    for y in range(0, 5):
        for x in range(W):
            if dentro(x, y) and not dentro(x, y - 1):
                px(im, x, y, PIEDRA_CENITAL)
    # LA ESTRELLA GUÍA (el corazón del chapitel) — EL PULSO FUERTE
    # (r2 QA: «muy estática» — el latido de la estrella guía AHORA
    # SE VE: nace tenue y ARDE blanca en su ápice).
    brillo = mezcla(mezcla(MORADO, MAGENTA, 0.5), MAGENTA_CALIENTE, latido)
    nucleo = mezcla(MAGENTA_CALIENTE, (255, 255, 252), latido * 0.8)
    # EL CONTORNO OSCURO (r3 QA: la estrella se funde con la nebulosa
    # — 1 px de separación la devuelve a su sitio de faro).
    for dx, dy in ((-2, 2), (2, 2), (-2, 0), (2, 0), (0, 4)):
        if dentro(16 + dx, 2 + dy):
            r, g, b, _a = im.getpixel((16 + dx, 2 + dy))
            if r < 80:
                px(im, 16 + dx, 2 + dy, mezcla((r, g, b), NEGRO_FONDO, 0.45))
    px(im, 16, 2, nucleo)
    px(im, 15, 2, brillo)
    px(im, 17, 2, brillo)
    px(im, 16, 1, brillo)
    px(im, 16, 3, brillo)
    px(im, 15, 1, mezcla(brillo, MORADO, 0.4))
    px(im, 17, 3, mezcla(brillo, MORADO, 0.4))
    # EL HALO de la estrella (el aire encendido — MÁS ANCHO y latiendo
    # con la estrella: el faro de la entidad).
    for dx, dy, k in ((-2, 0, .45), (2, 0, .45), (0, -2, .40), (0, 2, .40),
                      (-1, -1, .30), (1, 1, .30), (-1, 1, .30), (1, -1, .30),
                      (-3, 0, .18), (3, 0, .18), (0, -3, .15), (0, 3, .15)):
        hx, hy = 16 + dx, 2 + dy
        if dentro(hx, hy):
            r, g, b, _a = im.getpixel((hx, hy))
            if r < 80:
                px(im, hx, hy, mezcla((r, g, b), brillo, k * (0.55 + 0.45 * latido)))


def vortex(im, f):
    """EL VÓRTICE CÓSMICO — la galaxia interior girando 60°/marco (el
    ciclo completo) CON SU GLOW EMISIVO (la cura #1): el halo radial
    que INUNDA la cámara y tiñe el marco interior de morado."""
    cx, cy = CX, 20.5
    rx, ry = 7.6, 10.4
    giro = (f / MARCOS) * 2 * math.pi
    latido = 0.5 + 0.5 * math.sin(f / MARCOS * 2 * math.pi)

    # FASE 1 — EL GLOW EMISIVO (el halo que INUNDA la cámara y MUERE
    # en el marco: el borde interior TEÑIDO de morado).
    for y in range(10, 32):
        for x in range(5, 30):
            if not dentro(x, y):
                continue
            dx = (x - cx) / (rx * 1.35)
            dy = (y - cy) / (ry * 1.30)
            d = math.sqrt(dx * dx + dy * dy)
            if d > 1.35:
                continue
            # la caída del halo (muere suave hacia el marco).
            k = max(0.0, 1.0 - d / 1.35)
            halo = mezcla(MORADO_OSCURO, MORADO, 0.35 + 0.25 * latido)
            r, g, b, _a = im.getpixel((x, y))
            # SOLO sobre la piedra (las capas de arriba viven).
            if r < 60 and g < 60 and b < 130:
                px(im, x, y, mezcla((r, g, b), halo, k * 0.55))

    # FASE 2 — LA GALAXIA (los tres brazos espirales + el corazón).
    for y in range(10, 31):
        for x in range(8, 26):
            if not dentro(x, y):
                continue
            dx = (x - cx) / rx
            dy = (y - cy) / ry
            d = math.sqrt(dx * dx + dy * dy)
            if d > 1.18:
                continue
            ang = math.atan2(dy, dx)
            espiral = (ang * 3 + d * 7.5 - giro * 3) % (2 * math.pi)
            brazo = math.cos(espiral)
            if d < 0.26:
                # EL CORAZÓN (magenta incandescente latiendo).
                k = 1 - d / 0.26
                col = mezcla(MAGENTA, MAGENTA_CALIENTE, k * (0.4 + 0.6 * latido))
                px(im, x, y, col)
            elif brazo > 0.30:
                # EL BRAZO (la banda ANCHA con su gradiente).
                k = min(1, (brazo - 0.30) / 0.70)
                if d > 0.88:
                    col = MORADO_HONDO
                else:
                    col = mezcla(MORADO_HONDO, MORADO, k)
                    # la cresta del brazo se enciende cerca del corazón.
                    if d < 0.55:
                        col = mezcla(col, MAGENTA, (0.55 - d) * 0.8)
                px(im, x, y, col)
            elif brazo > 0.05 and d < 1.0:
                # EL FILO del brazo.
                px(im, x, y, MORADO if d > 0.45 else MORADO_HONDO)
            elif d > 0.38:
                # EL VALLE (la oscuridad entre brazos — con eco violeta).
                k = (d - 0.38) / 0.8
                col = mezcla(MORADO_OSCURO, (40, 18, 76), k * 0.5)
                px(im, x, y, col)
            else:
                px(im, x, y, MORADO_OSCURO)

    # FASE 3 — EL RIM DEL PORTAL (el borde interior del marco TEÑIDO
    # por la galaxia: las caras que MIRAN al vórtice se encienden).
    for y in range(10, 31):
        for x in range(5, 30):
            if not dentro(x, y):
                continue
            # el pixel de piedra JUNTO a la cámara (los pilares).
            junto = (not dentro(x + 2, y) or not dentro(x - 2, y) or
                     (y in (10, 11, 29, 30) and dentro(x, y - 2)))
            r, g, b, _a = im.getpixel((x, y))
            if junto and r < 70 and b < 140 and not (r > 150 and g > 120):
                dx = abs(x - cx) / rx
                dy = abs(y - cy) / ry
                d = math.sqrt(dx * dx + dy * dy)
                if 1.05 < d < 1.9:
                    k = max(0.0, 1.0 - (d - 1.05) / 0.85) * 0.5
                    px(im, x, y, mezcla((r, g, b), RIM_VIOLETA, k))


def estrellas_fugitivas(im, f):
    """LAS ESTRELLAS FUGITIVAS (la cura #4): el cosmos NO CABE en la
    caja — estrellitas deterministas DRIFTANDO fuera del marco del
    portal (sobre los pilares y el arco), titilando marco a marco."""
    semillas = (
        (7, 14, 0), (26, 17, 1), (6, 24, 2), (27, 25, 3),
        (12, 8, 4), (22, 9, 5), (16, 7, 2), (9, 28, 3),
    )
    for x0, y0, fase in semillas:
        # el drift: cada estrella flota 1 px en su órbita pequeña.
        t = (f + fase) / MARCOS
        x = x0 + int(round(math.sin(t * 2 * math.pi)))
        y = y0 + int(round(math.cos(t * 2 * math.pi) * 0.5))
        if not dentro(x, y):
            continue
        r, g, b, _a = im.getpixel((x, y))
        # SOLO sobre la piedra (nunca sobre el vórtice ni el oro).
        if r < 70 and g < 70:
            titila = 0.5 + 0.5 * math.sin((f + fase * 2) / MARCOS * 2 * math.pi)
            col = mezcla((170, 150, 220), (240, 230, 255), titila)
            px(im, x, y, col)


def chispa(im, f):
    """LA CHISPA DE CONSAGRACIÓN — el destello que SUBE por la cámara
    (CONTENIDA: nace al fondo y asciende hasta la cumbre, con su
    estela tenue que NO tapa los brazos del vórtice)."""
    t = f / (MARCOS - 1)
    y = int(29 - t * 16)
    x = int(CX + math.sin(t * 6.28) * 3)
    x = max(10, min(23, x))
    if dentro(x, y):
        px(im, x, y, (255, 255, 242) if t < 0.6 else (210, 240, 238))
    # LA CRUZ solo al nacer.
    if t < 0.6:
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            if dentro(x + dx, y + dy):
                px(im, x + dx, y + dy, CIAN_CHISPA)
    # LA ESTELA tenue (solo sobre los valles).
    if f > 0:
        y_prev = int(29 - (f - 1) / (MARCOS - 1) * 16)
        for yy in range(y + 1, min(y_prev + 1, 30)):
            ex = int(CX + math.sin((29 - yy) / 16 * 6.28) * 3 * 0.7)
            ex = max(10, min(23, ex))
            r, g, b, _a = im.getpixel((ex, yy))
            if r < 90 and b < 200:
                px(im, ex, yy, (44, 130, 108))


def velas(im, f):
    """LAS DOS VELAS DE HUESO — en CONTRAFASE, llamas GORDAS con
    núcleo caliente y (la cura #3) SU LUZ TIÑENDO la piedra vecina:
    cada llama un FOCO CÁLIDO real con radio de 3-4 px y caída."""
    # LA CERA (estática): columnas modeladas con highlight y sombra.
    for vx in (6, 27):
        for y in range(36, 41):
            px(im, vx, y, VELA_CUERPO)
            px(im, vx + 1, y, VELA_LUZ if y < 38 else VELA_CUERPO)
            if vx == 6:
                px(im, vx - 1, y, VELA_SOMBRA)
            else:
                px(im, vx + 2, y, VELA_SOMBRA)
        px(im, vx, 36, VELA_SOMBRA)
        # EL PORTALLAMAS (el aro de oro envejecido que sostiene).
        for dx in (-1, 2):
            px(im, vx + dx, 36, PORTALLAMAS)
            px(im, vx + dx, 37, PORTALLAMAS)

    # LAS LLAMAS (en contrafase — la izquierda en f, la derecha +3).
    fases = (f, (f + 3) % MARCOS)
    for vx, fase in zip((6, 27), fases):
        alta = (3, 4, 5, 4, 3, 4)[fase]
        lado = -1 if vx == 6 else 1
        cruza = (0, 1, 0, 1, 1, 2)[fase]
        for i in range(alta):
            y = 35 - i
            if i == 0:
                col, col2 = LLAMA_MEDIA, LLAMA_MEDIA
            elif i < alta - 1:
                col, col2 = LLAMA, LLAMA_MEDIA if i == 1 else LLAMA
            else:
                col, col2 = LLAMA_PUNTA, LLAMA
            px(im, vx, y, col)
            if i < alta - 1:
                px(im, vx + 1, y, col2)
                if i == 0:
                    px(im, vx - 1, y, LLAMA_MEDIA)
        if cruza > 0:
            tipY = 35 - alta + 1
            for c in range(cruza):
                px(im, vx + 1 + lado * (c + 1), tipY + c,
                   LLAMA_PUNTA if c == cruza - 1 else LLAMA)
        if fase in (3, 5):
            px(im, vx, 33, LLAMA_PUNTA)
            px(im, vx, 34, (255, 226, 120))
            px(im, vx + 1, 34, LLAMA_PUNTA)

    # === LA LUZ DE LAS VELAS (la cura #3, r2 QA: «la llama no emite
    # luz sobre su entorno» — AHORA SÍ): cada llama TIÑE la piedra
    # vecina con caída REAL y FUERTE, el rim cálido del pilar interior
    # y el parpadeo sincronizado con la llama) ===
    for vx, fase in zip((6, 27), fases):
        alta = (3, 4, 5, 4, 3, 4)[fase]
        fuerza = alta / 5.0
        # el foco: la llama vive en (vx+0.5, 35 - alta*0.5).
        fy = 35 - alta * 0.5
        for y in range(27, 41):
            for x in range(vx - 6, vx + 8):
                if not dentro(x, y):
                    continue
                d = math.hypot(x - (vx + 0.5), (y - fy) * 1.2)
                if d > 6.0:
                    continue
                r, g, b, _a = im.getpixel((x, y))
                # SOLO la piedra desnuda (ni llama, ni cera, ni oro).
                if r > 150 or (r > 90 and g > 60):
                    continue
                k = max(0.0, 1.0 - d / 6.0) * fuerza * 0.85
                if k < 0.06:
                    continue
                px(im, x, y, mezcla((r, g, b), (150, 96, 34), k))
        # EL RIM CÁLIDO DEL PILAR (el borde interior que MIRA a la
        # llama se enciende de ámbar — el pilar recibe la luz).
        rim_x = 8 if vx == 6 else 25
        for y in range(29, 36):
            r, g, b, _a = im.getpixel((rim_x, y))
            if r < 80:
                caida = 1.0 - (y - 29) / 7.0
                px(im, rim_x, y, mezcla((r, g, b), (170, 116, 44),
                                        0.5 * fuerza * caida))


def pedestal(im, f):
    """EL PEDESTAL — las runas en persecución, los faroles alternos y
    (nuevo v6.110) LAS JOYAS BISELADAS: cada gema con SU highlight y
    SU sombra — joyas de verdad, no píxeles sueltos."""
    izq_on = f % 2 == 0
    pulso = 0.5 + 0.5 * math.sin(f / MARCOS * 2 * math.pi)

    # LA BANDA INFERIOR de piedra (con su caída).
    for y in range(41, 50):
        for x in range(W):
            if not dentro(x, y):
                continue
            k = (y - 41) / 8.0
            c = mezcla(PIEDRA, PIEDRA_HONDA, k)
            if y == 41:
                c = mezcla(PIEDRA_ALTA, PIEDRA_MEDIA, 0.3)
            px(im, x, y, c)
    # EL OUTLINE INFERIOR (lo más hondo del altar).
    for x in range(W):
        if dentro(x, 49):
            px(im, x, 49, NEGRO_FONDO)

    # LA BANDA DE LAS RUNAS (pulsando + el glifo que corre) — con EL
    # REBOTE CÁLIDO bajo las velas (r3 QA: la base no puede quedar
    # muerta: la fila 46 recibe el eco tenue de las dos llamas).
    for x in range(W):
        fase = (x / W) * 2 * math.pi
        k = 0.35 + 0.65 * (0.5 + 0.5 * math.sin(fase + f / MARCOS * 2 * math.pi))
        if (x - f * 2) % 17 == 0:
            px(im, x, 47, CIAN_CHISPA)
        else:
            px(im, x, 47, mezcla((20, 60, 66), TEAL_RUNA, k))
        # EL REBOTE: cerca de cada vela la piedra se entibia.
        ecoV = max(math.exp(-((x - 7) ** 2) / 10.0),
                   math.exp(-((x - 28) ** 2) / 10.0))
        if ecoV > 0.25:
            r, g, b, _a = im.getpixel((x, 46))
            if r < 90:
                px(im, x, 46, mezcla((r, g, b), (96, 62, 26), ecoV * 0.5))

    # LAS JOYAS BISELADAS (la fila 43: gemas cada 5 px — cada una con
    # highlight arriba-izquierda y sombra abajo-derecha + SU latido).
    for j, jx in enumerate((4, 9, 14, 19, 24, 29)):
        lat = 0.5 + 0.5 * math.sin(f / MARCOS * 2 * math.pi + j * 1.1)
        base = mezcla(TEAL_LINTERNA, TEAL_RUNA, j % 2 * 0.5)
        gema = mezcla(base, (200, 255, 240), lat * 0.4)
        # EL CUERPO de la gema (2×2 con bisel).
        px(im, jx, 43, mezcla(gema, (255, 255, 255), 0.35))
        px(im, jx + 1, 43, gema)
        px(im, jx, 44, gema)
        px(im, jx + 1, 44, mezcla(gema, NEGRO_FONDO, 0.45))
        # EL ENCASTRE (el aro de oro que la sostiene).
        px(im, jx - 1, 44, ORO_HONDO if j % 2 else ORO)

    # LOS FAROLES ALTERNOS (las esquinas prenden por turnos).
    for x in range(1, 4):
        px(im, x, 42, TEAL_LINTERNA if izq_on else mezcla(TEAL_TRABAJO, PIEDRA, 0.5))
    for x in range(W - 3, W - 1):
        px(im, x, 42, TEAL_LINTERNA if not izq_on else mezcla(TEAL_TRABAJO, PIEDRA, 0.5))

    # EL FILO DE ORO de la moldura (la línea maestra del pedestal).
    for x in range(W):
        if dentro(x, 45):
            px(im, x, 45, ORO if x % 2 == 0 else ORO_HONDO)
        if dentro(x, 48):
            k = (x + f) % 4
            if k == 0:
                px(im, x, 48, mezcla(TEAL_TRABAJO, (40, 90, 150), 0.5))
            elif k == 2:
                px(im, x, 48, mezcla(TEAL_TRABAJO, TEAL_LINTERNA, 0.25))


def highlight():
    """EL CONTORNO DEL RATÓN (el _Highlight): la silueta del altar en
    blanco cálido — el contorno que tML dibuja al pasar el cursor."""
    im = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    for y in range(H):
        for x in range(W):
            if dentro(x, y) and (
                    not dentro(x - 1, y) or not dentro(x + 1, y) or
                    not dentro(x, y - 1) or not dentro(x, y + 1)):
                im.putpixel((x, y), (255, 244, 214, 255))
    return im


def generar():
    base = os.path.join(os.path.dirname(__file__), '..', 'AethonMod')
    hoja = Image.new('RGBA', (W, H * MARCOS), (0, 0, 0, 0))
    for f in range(MARCOS):
        marco = Image.new('RGBA', (W, H), (0, 0, 0, 0))
        cuerpo_piedra(marco)      # 1. la piedra CON VOLUMEN
        trim_oro(marco)           # 2. el oro cósmico (las molduras)
        chapitel(marco, f)        # 3. el chapitel estelar
        vortex(marco, f)          # 4. el vórtice + EL GLOW EMISIVO + rim
        velas(marco, f)           # 5. las velas + SU LUZ CÁLIDA
        chispa(marco, f)          # 6. la chispa de consagración
        estrellas_fugitivas(marco, f)  # 7. las estrellas fugitivas
        pedestal(marco, f)        # 8. el pedestal + las joyas biseladas
        hoja.paste(marco, (0, f * H))
    hoja.save(os.path.join(base, 'Content', 'Tiles', 'AltarOfrendasTile.png'))
    # EL ÍTEM: el marco 0.
    marco0 = hoja.crop((0, 0, W, H))
    marco0.save(os.path.join(base, 'Content', 'Items', 'Altar', 'AltarOfrendasItem.png'))
    # EL CONTORNO del ratón.
    highlight().save(os.path.join(base, 'Content', 'Tiles',
                                  'AltarOfrendasTile_Highlight.png'))

    # --- LA VERIFICACIÓN DE LA CASA ---
    im = Image.open(os.path.join(base, 'Content', 'Tiles', 'AltarOfrendasTile.png')).convert('RGBA')
    assert im.size == (W, H * MARCOS), f'tamaño roto: {im.size}'
    pxs = im.load()
    for f in range(MARCOS):
        ultima = -1
        for y in range(H):
            for x in range(W):
                if pxs[x, y + f * H][3] > 10:
                    ultima = y
        assert ultima == H - 1, f'¡el marco {f} flota (fila {ultima})!'
        print(f'  marco {f}: arte hasta la ÚLTIMA fila ({ultima})')
    print(f'EL ALTAR CÓSMICO: {im.size[0]}x{im.size[1]} · 6 marcos · transparencia nativa')

    # LA HOJA DE CONTACTO para EL OJO.
    frames = [hoja.crop((0, i * H, W, (i + 1) * H)) for i in range(MARCOS)]
    zoom = 8
    hoja_qa = Image.new('RGBA', (W * zoom * 6 + 5 * 10, H * zoom), (18, 16, 26, 255))
    for i, fr in enumerate(frames):
        hoja_qa.paste(fr.resize((W * zoom, H * zoom), Image.NEAREST),
                      (i * (W * zoom + 10), 0))
    hoja_qa.save('/tmp/altar_v6110_qa.png')
    print('hoja de contacto: /tmp/altar_v6110_qa.png')


if __name__ == '__main__':
    generar()
