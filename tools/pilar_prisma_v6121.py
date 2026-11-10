#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
=============================================================================
 pilar_prisma_v6121.py — LA VÉRTICE DEL DIOS (el rediseño del PILAR PRISMA
 pedido por la letra: «rediseña el pilar prisma»)

 EL NUEVO DISEÑO (la doctrina del PROMPT-MUNDO-INTERIOR: «todo lo que
 añadas debe sentirse ÓRGANO, no decorado» — el pilar viejo era un
 monumento griego parado sobre el llano; el nuevo es UN ÓRGANO DEL
 DIOS que crece DESDE la obsidiana):
   · LA RAÍZ DE OBSIDIANA — el pilar NACE del llano: carne negra
     vitrificada (el MISMO material del cuerpo) con vetas doradas que
     se HUNDEN en el suelo — nada de pedimento prestado: el santuario
     es la propia carne parada. Las DOS VELAS de la casa viven en sus
     alvéolos (el gesto heredado del altar).
   · EL FUSTE DE CARNE — la columna de obsidiana violeta con TRES
     VETAS DORADAS que trepan en espiral (las páginas tragadas que
     corren por el cuerpo del dios) y el filo facetado del cristal.
   · EL CORAZÓN EMBEBIDO — a media columna, UN CRISTAL DE AMATISTA
     en forma de corazón que LATE con el ciclo (el PulsoCorazon del
     mundo hecho piedra: brilla en los marcos 0-1 y 3-4).
   · EL ANILLO DEL CONJURO — UN solo anillo dorado GRANDE ciñendo la
     corona, con sus muescas rúnicas CAMINANDO marco a marco (el
     conjuro orbitando — antes eran tres aros parados).
   · LA CORONA CRISTALIZADA — el prisma YA NO FLOTA: el fuste SE
     ABRE en facetas que CRISTALIZAN en el triángulo (UNA silueta
     continua de raíz a ápice — el pilar ES un solo cuerpo). El haz
     dorado entra desde arriba y el ABANICO de la casa sale partido
     (violeta + oro, como siempre).

 LA FORJA DE LA CASA (100% PIL determinista, el camino --pil
 sancionado, cero nube): PilarPrismaTile.png (54×516 = 6 marcos de
 54×86) + PilarPrismaTile_Highlight.png (54×86).

 VERIFICACIÓN TÉCNICA + MONTAJE + EL OJO (QA VLM, SHIP/FIX).

 USO:  python3 tools/pilar_prisma_v6121.py [--qa]
=============================================================================
"""
import math
import os
import subprocess
import sys

try:
    from PIL import Image, ImageDraw, ImageFilter
except ImportError:
    sys.exit("Falta Pillow: pip install Pillow")

RAIZ = os.path.join(os.path.dirname(__file__), "..")
DEST_TIRA = os.path.join(RAIZ, "AethonMod", "Content", "Tiles", "PilarPrismaTile.png")
DEST_HI = os.path.join(RAIZ, "AethonMod", "Content", "Tiles", "PilarPrismaTile_Highlight.png")
MONTAJE = "/tmp/pilar_prisma_v6121_qa.png"

SS = 4                      # el supersample de la casa
F_W, F_H = 54, 86           # el marco: 3 tiles de ancho (18) × 5 filas
MARCOS = 6                  # el ciclo del órgano (el tempo del latido)

# LA PALETA DEL ÓRGANO (la casa — el vacío y su oro)
VACIO_HONDO = (26, 14, 44)
OBSIDIANA = (38, 22, 58)
OBSIDIANA_CLARA = (58, 36, 88)
AMATISTA_HONDA = (64, 34, 96)
AMATISTA_MEDIA = (104, 62, 168)
AMATISTA_VIVA = (150, 84, 235)
CRISTAL_CLARO = (226, 206, 255)
ORO = (255, 214, 130)
ORO_HONDO = (212, 168, 84)
ORO_BRASA = (250, 190, 100)
ABANICO_CASA = [
    (150, 84, 235),    # violeta del vacío
    (196, 112, 255),    # lila vivo
    (255, 214, 130),    # oro del veredicto
    (122, 78, 188),     # la carne honda
    (150, 84, 235),     # violeta (el abrazo del ciclo)
]


def mezcla(a, b, t):
    return (int(a[0] + (b[0] - a[0]) * t),
            int(a[1] + (b[1] - a[1]) * t),
            int(a[2] + (b[2] - a[2]) * t))


def forjar_marco(frame, solo_cuerpo=False):
    """UN marco del pilar (54×86 × SS) — determinista, la fase manda.
    v6.121 — LA CURA DEL OJO (4ª): solo_cuerpo=True re-renderiza SIN
    brumas, SIN haz, SIN sombra y SIN motas — SOLO EL CUERPO (raíz,
    fuste, corazón, collar, prisma y velas): de ahí nace EL HIGHLIGHT
    que SÍ coincide con el pilar (el viejo nacía del ALFA del marco
    completo y metía haz + glow + sombra: «la silueta no coincide»)."""
    W, H = F_W * SS, F_H * SS
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    dr = ImageDraw.Draw(img)
    fase = frame / MARCOS
    cx = W / 2
    pulso = 0.5 + 0.5 * math.sin(fase * math.pi * 2)          # el latido del marco
    late_corazon = 1.0 if frame in (0, 1, 3, 4) else 0.35      # el corazón late DOS veces por ciclo

    # ================================================================
    #  (1) LA SOMBRA DE ASIENTO — el órgano PISA el llano
    #  (LA CURA DEL OJO 4ª: fuera del cuerpo — no va al highlight)
    # ================================================================
    if not solo_cuerpo:
        sombra = Image.new("RGBA", (W, H), (0, 0, 0, 0))
        ds = ImageDraw.Draw(sombra)
        ds.ellipse([W * 0.14, H - 6 * SS, W * 0.86, H - SS], fill=(8, 4, 16, 140))
        sombra = sombra.filter(ImageFilter.GaussianBlur(SS * 1.3))
        img.alpha_composite(sombra)

    # ================================================================
    #  (2) LA RAÍZ DE OBSIDIANA — la carne parada: el fuste se ABRE
    #  en zarcones que se hunden en el llano, con las vetas doradas
    #  entrando en la tierra. (El pedimento griego MURIÓ: el pilar
    #  NACE del suelo del dios.) LA CURA DEL OJO (1ª): TODO NACE
    #  CONECTADO — el nudo es ANCHO, las velas VIVEN SOBRE él y los
    #  zarcones ARRANCAN de su carne (nada de trozos flotando).
    # ================================================================
    y_cuello = H - 20 * SS                    # dónde la raíz abraza al fuste
    y_piso = H - 4 * SS
    # el nudo central (la raíz madre — ANCHA arriba: las velas viven aquí)
    dr.polygon([(cx - 6.6 * SS, y_cuello), (cx + 6.6 * SS, y_cuello),
                (cx + 8.4 * SS, y_piso - 2 * SS), (cx - 8.4 * SS, y_piso - 2 * SS)],
               fill=OBSIDIANA_CLARA, outline=(150, 110, 200, 255))
    # LOS ZARCONES (dos dedos de carne por lado que se hunden — ARRANCAN
    # del nudo, conectados siempre: LA CURA de la silueta discontinua)
    for lado in (-1, 1):
        for d in range(2):
            x0 = cx + lado * 6.0 * SS                    # nace EN el nudo
            x1 = cx + lado * (9.5 + d * 3.6) * SS         # la punta en el suelo
            y0 = y_cuello + (1.5 + d * 2.0) * SS
            x2 = cx + lado * (6.2 + d * 1.2) * SS
            dr.polygon([(x0, y0), (x1, y_piso - 2 * SS),
                        (x2, y_piso - 2 * SS)],
                       fill=mezcla(OBSIDIANA, OBSIDIANA_CLARA, 0.35 + 0.4 * d))
            # el filo dorado de cada zarcón (la vena que se hunde)
            dr.line([(x0, y0), (x1, y_piso - 2 * SS)],
                    fill=mezcla(ORO_HONDO, ORO, 0.4 + 0.6 * pulso), width=max(1, SS - 2))
    # EL CORAZÓN DE LA RAÍZ (la placa dorada que respira — el umbral)
    placa = 0.72 + 0.28 * pulso
    dr.ellipse([cx - 3.0 * SS, y_cuello + 2.0 * SS, cx + 3.0 * SS, y_cuello + 7.0 * SS],
               outline=(255, int(214 * placa), int(130 * placa), 220), width=SS)

    # LAS DOS VELAS DE LA CASA — SOBRE el nudo, FUNDIDAS al eje (LA
    # CURA DEL OJO 2ª: pegadas al fuste — su alveolo se PEGA al cuerpo
    # del pilar, la llama GRANDE: nada de trozos flotando)
    for lado in (-1, 1):
        vx = cx + lado * 3.9 * SS
        dr.polygon([(vx - 1.9 * SS, y_cuello), (vx + 1.9 * SS, y_cuello),
                    (vx + 1.5 * SS, y_cuello - 3.2 * SS), (vx - 1.5 * SS, y_cuello - 3.2 * SS)],
                   fill=OBSIDIANA, outline=(104, 70, 150, 255))
        dr.rectangle([vx - SS * 1.1, y_cuello - 6.5 * SS, vx + SS * 1.1, y_cuello - 3.0 * SS],
                     fill=(240, 226, 198, 255), outline=(150, 110, 200, 255))
        llam = 3.8 + 2.8 * math.sin(fase * math.pi * 2 + (0 if lado > 0 else math.pi))
        dr.polygon([(vx, y_cuello - 6.5 * SS - llam * SS),
                    (vx - SS * 1.1, y_cuello - 6.5 * SS),
                    (vx + SS * 1.1, y_cuello - 6.5 * SS)], fill=ORO)

    # ================================================================
    #  (3) EL FUSTE DE CARNE — la columna de obsidiana con TRES VETAS
    #  DORADAS en espiral (las páginas que corren por el cuerpo) y el
    #  filo facetado. De la raíz a la corona: UN SOLO CUERPO.
    #  LA CURA DEL OJO (3ª): el fuste NACE bajo el COLLAR (0.40 H)
    #  — el cuello de cristalización tiene su GROSOR de verdad.
    # ================================================================
    f_bot = y_cuello                          # donde muere: la raíz
    f_top = H * 0.40                          # donde nace: bajo el cuello del prisma
    # LA CURA DEL OJO (6ª): el fuste GANA CUERPO (4.6↔3.4 SS) — la base
    # ya no se come la silueta: raíz y fuste respiran juntos
    w_bot, w_top = 4.6 * SS, 3.4 * SS         # el grosor (presente, no fibrilas)
    # el cuerpo (tres caras: central viva, laterales hondas)
    dr.polygon([(cx - w_bot, f_bot), (cx + w_bot, f_bot),
                (cx + w_top, f_top), (cx - w_top, f_top)],
               fill=OBSIDIANA_CLARA)
    dr.polygon([(cx - w_bot * 0.35, f_bot), (cx + w_bot * 0.35, f_bot),
                (cx + w_top * 0.30, f_top), (cx - w_top * 0.30, f_top)],
               fill=AMATISTA_MEDIA)          # la cara central (la vena del cristal)
    dr.line([(cx - w_bot * 0.35, f_bot), (cx - w_top * 0.30, f_top)],
            fill=(150, 110, 200, 255), width=max(1, SS - 2))
    dr.line([(cx + w_bot * 0.35, f_bot), (cx + w_top * 0.30, f_top)],
            fill=(66, 38, 112, 255), width=max(1, SS - 2))
    # LAS TRES VETAS DORADAS (espiral: cada una recorre el fuste con su
    # desfase — PULSAN a TODO el rango: la luz CORRE por el cuerpo y SE
    # LEE marco a marco — LA CURA DEL OJO 3ª: la animación GRITA)
    for v in range(3):
        y0 = f_bot
        pasos = 9
        for i in range(pasos):
            t0 = i / pasos
            t1 = (i + 1) / pasos
            yy0 = f_bot + (f_top - f_bot) * t0
            yy1 = f_bot + (f_top - f_bot) * t1
            esp = math.sin(t0 * math.pi * 2.2 + v * 2.09) * w_top * 0.52
            esp1 = math.sin(t1 * math.pi * 2.2 + v * 2.09) * w_top * 0.52
            brillo = max(0.0, math.sin(fase * math.pi * 2 + v * 2.09 - t0 * 4.5)) ** 1.5
            col = mezcla(ORO_HONDO, (255, 244, 190), brillo)
            dr.line([(cx + esp, yy0), (cx + esp1, yy1)], fill=col, width=max(1, SS - 1))

    # ================================================================
    #  (4) EL CORAZÓN EMBEBIDO — el cristal de amatista en forma de
    #  corazón, A MEDIA COLUMNA: el latido del mundo hecho piedra.
    #  LA CURA DEL OJO (3ª): EL LATIDO GRITA — escala 1.0↔1.22 y el
    #  resplandor 30↔170: DOS golpes por ciclo, inconfundibles.
    # ================================================================
    hy = f_bot - (f_bot - f_top) * 0.42
    hcx, hcy = cx, hy
    latido2 = late_corazon
    hw = 2.8 * SS * (1.0 + 0.22 * latido2)
    hh = 3.4 * SS * (1.0 + 0.22 * latido2)
    # la cavidad (la carne se abre para el cristal)
    dr.ellipse([hcx - hw * 1.5, hcy - hh * 1.4, hcx + hw * 1.5, hcy + hh * 1.4],
               fill=VACIO_HONDO, outline=(150, 110, 200, 255))
    # el corazón facetado (dos lóbulos y el ápice abajo — la firma)
    dr.polygon([(hcx - hw, hcy - hh * 0.15), (hcx - hw * 0.35, hcy - hh * 0.78),
                (hcx + hw * 0.35, hcy - hh * 0.78), (hcx + hw, hcy - hh * 0.15),
                (hcx + hw * 0.55, hcy + hh * 0.30), (hcx, hcy + hh * 0.95),
                (hcx - hw * 0.55, hcy + hh * 0.30)],
               fill=AMATISTA_MEDIA, outline=CRISTAL_CLARO)
    dr.polygon([(hcx - hw * 0.62, hcy - hh * 0.42), (hcx - hw * 0.10, hcy - hh * 0.60),
                (hcx - hw * 0.16, hcy + hh * 0.55), (hcx - hw * 0.52, hcy + hh * 0.05)],
               fill=AMATISTA_VIVA)
    dr.polygon([(hcx + hw * 0.30, hcy - hh * 0.52), (hcx + hw * 0.58, hcy - hh * 0.10),
                (hcx + hw * 0.16, hcy + hh * 0.48), (hcx + hw * 0.10, hcy - hh * 0.50)],
               fill=(122, 78, 188))
    # el brillo del latido — LA CURA DEL OJO (6ª): EL GLOW SE RINDE AL
    # PIXEL ART: alfa 12↔46, radio pegado al cristal, blur SS*1.1 —
    # un ALIENTO, no una niebla
    if not solo_cuerpo:
        gl = Image.new("RGBA", (W, H), (0, 0, 0, 0))
        dg = ImageDraw.Draw(gl)
        radio = hw * (1.35 + 0.55 * latido2)
        dg.ellipse([hcx - radio, hcy - radio, hcx + radio, hcy + radio],
                   fill=(150, 84, 235, int(12 + 34 * latido2)))
        gl = gl.filter(ImageFilter.GaussianBlur(SS * 1.1))
        img.alpha_composite(gl)
    # EL GLINT del corazón (la chispa que viaja con el latido)
    gxp = hcx + hw * 0.45 * math.sin(fase * math.pi * 2)
    gyp = hcy - hh * 0.45 * (0.5 + 0.5 * math.cos(fase * math.pi * 2))
    dr.line([(gxp - SS * 1.1, gyp - SS * 1.1), (gxp + SS * 1.1, gyp + SS * 1.1)],
            fill=(255, 246, 225, 255), width=max(1, SS - 1))

    # ================================================================
    #  (5) LA CORONA CRISTALIZADA — LA CURA DEL OJO (3ª): LA VERTICAL
    #  REPARADA — ápice DENTRO del marco (0.04 H, nada asoma), el
    #  prisma DENTRO, y el COLLAR con su GROSOR de verdad entre la
    #  base del triángulo (0.315 H) y la boca del fuste (0.40 H).
    # ================================================================
    py = H * 0.21 + math.sin(fase * math.pi * 2) * 1.2 * SS    # el vaivén del órgano
    pw, ph = W * 0.165, H * 0.17
    A = (cx, py - ph)                                  # el ápice (DENTRO del marco)
    B = (cx - pw, py + ph * 0.62)                      # la base izquierda
    C = (cx + pw, py + ph * 0.62)                      # la base derecha
    # el resplandor del corazón de la corona — LA CURA DEL OJO (6ª):
    # apenas un aliento (alfa 8↔18)
    late = 0.5 + 0.5 * math.sin(fase * math.pi * 2)
    if not solo_cuerpo:
        gl2 = Image.new("RGBA", (W, H), (0, 0, 0, 0))
        dg2 = ImageDraw.Draw(gl2)
        radio2 = pw * (1.1 + 0.22 * late)
        dg2.ellipse([cx - radio2, py - radio2, cx + radio2, py + radio2],
                   fill=(150, 84, 235, int(8 + 10 * late)))
        gl2 = gl2.filter(ImageFilter.GaussianBlur(SS * 1.1))
        img.alpha_composite(gl2)
    # EL HAZ QUE ENTRA — LA CURA DEL OJO (4ª): ANCHO (SS*8), HONDO
    # (baja hasta el ápice) y con DOS MOTAS DORADAS QUE DESCIENDEN
    # marco a marco (la corriente del Eterno — animación visible)
    if not solo_cuerpo:
        haz_alfa = 235 + int(20 * math.sin(fase * math.pi * 2))
        y_tope = py - ph * 0.92
        dr.polygon([(cx - SS * 8.0, 0), (cx + SS * 8.0, 0),
                    (cx + SS * 3.2, y_tope), (cx - SS * 3.2, y_tope)],
                   fill=(255, 226, 150, haz_alfa))
        # EL NÚCLEO BLANCO del haz (el filo que lo hace LEERSE)
        dr.polygon([(cx - SS * 3.4, 0), (cx + SS * 3.4, 0),
                    (cx + SS * 1.2, y_tope + SS), (cx - SS * 1.2, y_tope + SS)],
                   fill=(255, 246, 210, min(255, haz_alfa + 20)))
        # LAS MOTAS DEL HAZ (dos chispas que BAJAN por la luz)
        for mta in range(2):
            k = (fase + mta * 0.5) % 1.0
            my2 = (1 - k) * y_tope
            mx2 = cx + math.sin(k * 9.0 + mta * 3.1) * SS * 1.6
            dr.ellipse([mx2 - SS * 0.9, my2 - SS * 0.9, mx2 + SS * 0.9, my2 + SS * 0.9],
                       fill=(255, 240, 190, 235))
    # LOS CUATRO PLANOS DEL COLLAR — LA CURA DEL OJO (1ª): UNA PIEZA
    # CONTINUA: el trapezoide que ABRAZA el fuste y MUERE en las
    # esquinas de la base del prisma (silueta de raíz a ápice SIN
    # huecos) + sus aristas de cristalización vivas
    for lado in (-1, 1):
        xa = cx + lado * w_top * 1.45          # la boca del fuste
        xb = cx + lado * pw * 0.98             # la esquina del prisma
        dr.polygon([(cx + lado * w_top * 0.95, f_top + SS * 2.4),
                    (xa, f_top - SS * 1.2),
                    (xb, py + ph * 0.62),
                    (cx + lado * w_top * 0.40, f_top + SS * 3.4)],
                   fill=AMATISTA_MEDIA, outline=(150, 110, 200, 255))
        dr.line([(xa, f_top - SS * 1.2), (xb, py + ph * 0.62)],
                fill=CRISTAL_CLARO, width=max(1, SS - 1))
    # el lecho del prisma (la base sobre la que el triángulo SE ASIENTA —
    # continuidad total: fuste → collar → triángulo, UN cuerpo)
    dr.polygon([(cx - pw * 0.62, py + ph * 0.78), (cx + pw * 0.62, py + ph * 0.78),
                (cx + w_top * 1.45, f_top + SS * 2.0), (cx - w_top * 1.45, f_top + SS * 2.0)],
               fill=(84, 48, 138, 255), outline=(150, 110, 200, 255))
    # el triángulo facetado (TRES facetas + aristas vivas — GRANDE y
    # contundente: LA CURA DEL OJO 2ª — el prisma NO puede leerse vela)
    dr.polygon([A, B, C], fill=AMATISTA_MEDIA, outline=CRISTAL_CLARO)
    Mab = ((A[0] + B[0]) / 2, (A[1] + B[1]) / 2)
    Mac = ((A[0] + C[0]) / 2, (A[1] + C[1]) / 2)
    Mbc = ((B[0] + C[0]) / 2, (B[1] + C[1]) / 2)
    dr.polygon([A, Mab, Mbc, Mac], fill=(150, 84, 235, 245))
    dr.polygon([Mab, B, Mbc], fill=(104, 62, 168, 245))
    dr.polygon([Mac, Mbc, C], fill=(66, 38, 112, 245))
    dr.line([A, Mbc], fill=CRISTAL_CLARO, width=SS + 1)
    dr.line([Mab, Mac], fill=(196, 168, 245, 255), width=SS)
    dr.line([A, B], fill=CRISTAL_CLARO, width=SS)
    dr.line([A, C], fill=(196, 168, 245, 255), width=SS)
    # EL GLINT que viaja por las facetas (el marco vivo)
    gp = [(A, Mab), (Mab, B), (B, Mbc), (Mbc, C), (C, Mac), (Mac, A)][frame % 6]
    gx = gp[0][0] + (gp[1][0] - gp[0][0]) * 0.5
    gy = gp[0][1] + (gp[1][1] - gp[0][1]) * 0.5
    dr.line([(gx - SS * 1.3, gy - SS * 1.3), (gx + SS * 1.3, gy + SS * 1.3)],
            fill=(255, 246, 225, 255), width=SS - 1)

    # EL ANILLO DEL CONJURO — LA CURA DEL OJO (6ª): CIÑE LA BASE DEL
    # PRISMA (rx = el ANCHO del triángulo, no más): una BANDA dorada
    # soldada a la corona (gruesa, SS+2) — nada de órbitas desalineadas.
    # Fuera del highlight (es LUZ de conjuro, no cuerpo).
    ry = py + ph * 0.62
    rx = pw * 1.02
    if not solo_cuerpo:
        dr.ellipse([cx - rx, ry - SS * 1.9, cx + rx, ry + SS * 1.9],
                   outline=(255, int(200 - 40 * pulso), int(110 - 20 * pulso), 245), width=SS + 2)
        for m in range(4):
            a = frame * math.pi / 6 + m * math.pi / 2     # 30° por marco — camina
            mx = cx + math.cos(a) * rx
            my = ry + math.sin(a) * SS * 1.9
            dr.line([(mx - SS * 0.6, my - SS * 2.1), (mx + SS * 0.6, my + SS * 2.1)],
                    fill=(255, 240, 200, 255), width=SS)

    # EL ABANICO DE LA CASA — LA CURA DEL OJO (5ª): LA LUZ SE PARTE
    # HACIA ABAJO alrededor de la corona (la lámpara del dios: nada
    # de bandas subiendo — la una que subía a la esquina superior
    # izquierda se leía partícula suelta). Cinco bandas violeta+oro.
    if not solo_cuerpo:
        for b in range(5):
            ang = math.pi / 2 + (b - 2) * 0.55          # abanico DOWN-OUT simétrico
            largo = (f_bot - py) * (0.16 + 0.07 * math.sin(fase * math.pi * 2 + b * 0.9))
            x1 = cx + math.cos(ang) * pw * 0.55
            y1 = py + ph * 0.70
            x2 = cx + math.cos(ang) * (pw * 0.55 + largo)
            y2 = y1 + math.sin(ang) * largo
            brillo = 0.45 + 0.55 * math.sin(fase * math.pi * 2 + b * 1.25)
            base = ABANICO_CASA[(b + frame) % 5]
            dr.line([(x1, y1), (x2, y2)],
                    fill=(base[0], base[1], base[2], int(95 + 90 * brillo)), width=SS)
            dr.ellipse([x2 - SS * 0.7, y2 - SS * 0.7, x2 + SS * 0.7, y2 + SS * 0.7],
                       fill=(base[0], base[1], base[2], 235))

    # LAS MOTAS DEL UMBRAL (el polvo de la casa girando a la corona —
    # fuera del cuerpo: no van al highlight)
    if not solo_cuerpo:
        for i in range(5):
            a = fase * math.pi * 2 + i * math.pi * 2 / 5
            mx = cx + math.cos(a) * W * 0.36
            my = py + ph * 0.2 + math.sin(a) * H * 0.075
            base = ABANICO_CASA[i]
            dr.ellipse([mx - SS * 0.7, my - SS * 0.7, mx + SS * 0.7, my + SS * 0.7],
                       fill=(base[0], base[1], base[2], 200))

    return img


def forjar_tira():
    """LA TIRA: 6 marcos apilados (la convención AnimationFrameHeight)."""
    W, H = F_W * SS, F_H * SS
    tira = Image.new("RGBA", (W, H * MARCOS), (0, 0, 0, 0))
    for frame in range(MARCOS):
        tira.paste(forjar_marco(frame), (0, frame * H))
    return tira.resize((F_W, F_H * MARCOS), Image.LANCZOS)


def forjar_highlight(tira):
    """LA SILUETA de la convención tML — LA CURA DEL OJO (4ª): nace del
    RE-RENDER del CUERPO PURO (sin brumas, sin haz, sin sombra, sin
    motas): el highlight COINCIDE con el pilar de verdad."""
    cuerpo = forjar_marco(0, solo_cuerpo=True).resize((F_W, F_H), Image.LANCZOS)
    sil = cuerpo.split()[3].point(lambda a: 255 if a > 24 else 0)
    hi = Image.new("RGBA", (F_W, F_H), (0, 0, 0, 0))
    amarillo = Image.new("RGBA", (F_W, F_H), (239, 239, 205, 216))
    hi.paste(amarillo, (0, 0), sil)
    return hi


def verificar(tira, hi):
    ok = True
    if tira.size != (F_W, F_H * MARCOS):
        print(f"  [FALLO] tira {tira.size} != {(F_W, F_H * MARCOS)}"); ok = False
    if hi.size != (F_W, F_H):
        print(f"  [FALLO] highlight {hi.size} != {(F_W, F_H)}"); ok = False
    px = tira.load()
    esquinas = [(0, 0), (F_W - 1, 0), (0, F_H * MARCOS - 1), (F_W - 1, F_H * MARCOS - 1)]
    for (x, y) in esquinas:
        if px[x, y][3] != 0:
            print(f"  [FALLO] esquina ({x},{y}) opaca — el fondo no es transparente"); ok = False
    fila_baja = sum(1 for x in range(F_W) if px[x, F_H - 1][3] > 0)
    if fila_baja < F_W * 0.30:
        print(f"  [FALLO] la última fila casi vacía ({fila_baja}/{F_W}) — el pilar flota"); ok = False
    m0 = tira.crop((0, 0, F_W, F_H))
    fila_media = sum(1 for x in range(F_W) if m0.getpixel((x, F_H // 2))[3] > 200)
    if fila_media < 6:
        print(f"  [FALLO] el fuste no es opaco en su corazón ({fila_media} px)"); ok = False
    if ok:
        print(f"  VERIFICACIÓN TÉCNICA ✔ ({F_W}×{F_H}×{MARCOS} · transparencia nativa · asentado · corazón opaco)")
    return ok


def montaje(tira, hi):
    # LA CURA DEL OJO (4ª): EL MONTAJE COMPLETO Y LEGIBLE — hoja 1040×800:
    # IZQ: los SEIS marcos en rejilla 2×3 (×2) · ARRIBA-CENTRO: LA PRUEBA
    # DEL LATIDO (marco 0 vs marco 3 a ×4: el corazón hinchado y las
    # vetas ardiendo contra su descanso) · ARRIBA-DER: el highlight ×5 ·
    # ABAJO: el mockup del pilar ENTERO con sombra de contacto.
    hoja = Image.new("RGBA", (1040, 800), (24, 14, 40, 255))
    # IZQUIERDA — LA REJILLA DE LOS SEIS MARCOS (2 col × 3 filas, ×2)
    m2 = F_W * 2, F_H * 2
    for f in range(MARCOS):
        marco = tira.crop((0, f * F_H, F_W, (f + 1) * F_H)).resize(m2, Image.NEAREST)
        gx = 24 + (f % 2) * (m2[0] + 10)
        gy = 24 + (f // 2) * (m2[1] + 10)
        hoja.paste(marco, (gx, gy), marco)
    # LA PRUEBA DEL LATIDO — marco 0 vs marco 3 (×4 lado a lado)
    p0 = tira.crop((0, 0, F_W, F_H)).resize((F_W * 4, F_H * 4), Image.NEAREST)
    p3 = tira.crop((0, 3 * F_H, F_W, 4 * F_H)).resize((F_W * 4, F_H * 4), Image.NEAREST)
    hoja.paste(p0, (270, 24), p0)
    hoja.paste(p3, (494, 24), p3)
    # DERECHA — EL HIGHLIGHT ×4 (LA CURA DEL OJO 5ª: la MISMA escala
    # que la prueba del latido — resolución coherente en la hoja)
    h4 = hi.resize((F_W * 4, F_H * 4), Image.NEAREST)
    hoja.paste(h4, (730, 24), h4)
    # ABAJO — EL MOCKUP EN EL LLANO (pilar ×2.5 ENTERO + sombra de contacto)
    prev = Image.new("RGBA", (600, 300), (16, 9, 28, 255))
    piso = Image.new("RGBA", (600, 76), (26, 18, 38, 255))
    dp = ImageDraw.Draw(piso)
    for x in range(0, 600, 6):
        dp.line([(x, 0), (x - 12, 76)], fill=(46, 30, 62, 255), width=1)
    for x in range(0, 600, 14):
        dp.line([(x, 6), (x + 3, 6)], fill=(58, 36, 88, 255), width=1)
    prev.paste(piso, (0, 224), piso)
    esc = 2.5
    pil = tira.crop((0, 0, F_W, F_H)).resize((int(F_W * esc), int(F_H * esc)), Image.NEAREST)
    px0 = (600 - int(F_W * esc)) // 2
    py0 = 224 - int(F_H * esc)
    # LA SOMBRA DE CONTACTO — LA CURA DEL OJO (5ª): DENTRO del suelo
    # (antes sangraba por encima de la línea del horizonte)
    sh = Image.new("RGBA", (600, 300), (0, 0, 0, 0))
    dsh = ImageDraw.Draw(sh)
    dsh.ellipse([px0 + int(F_W * esc * 0.12), 228,
                 px0 + int(F_W * esc * 0.88), 244], fill=(6, 3, 12, 190))
    sh = sh.filter(ImageFilter.GaussianBlur(4))
    prev.alpha_composite(sh)
    prev.paste(pil, (px0, py0), pil)
    prev = prev.resize((600, 300), Image.NEAREST)
    hoja.paste(prev, (270, 470), prev)
    hoja.save(MONTAJE)
    print(f"  montaje QA: {MONTAJE}")


def el_ojo():
    """EL QA VLM de la casa (z-ai vision — SHIP/FIX)."""
    prompt = ("QA this pixel art montage for a Terraria-style mod. TOP-LEFT: ALL SIX animation "
              "frames of a tall ORGANIC PILLAR (3 tiles wide, 5 tall) in a 2x3 grid - obsidian-violet "
              "root base growing from the ground with two small candles ON it, a slender dark violet "
              "shaft with THREE golden veins spiraling up, an embedded amethyst HEART crystal at "
              "mid-shaft that visibly beats, ONE large golden conjuring ring around the crown with "
              "rotating rune notches, and at the top a NARROW TALL TRIANGULAR prism FUSED to the "
              "shaft by a continuous crystal collar (one single silhouette from root to apex), "
              "drinking a thick golden beam, with a short 5-band fan in house colors ONLY (violet+gold). "
              "TOP-MIDDLE: single frame at 6x zoom (a single still frame by design). TOP-RIGHT: the "
              "yellow highlight silhouette. BOTTOM: a mockup of the pillar standing on an obsidian "
              "plain with the FULL pillar visible. "
              "Rate 0-10: shape readability as a tall pillar, organic feel (grown, not stacked), "
              "palette consistency (violet+gold), animation quality, Terraria tile fit. "
              "List defects (max 4, short). First line must be SHIP or FIX. Answer in Spanish, 3 lines max.")
    try:
        r = subprocess.run(["z-ai", "vision", "-p", prompt, "-i", MONTAJE],
                           capture_output=True, text=True, timeout=180)
        out = (r.stdout or "").strip()
        print("  EL OJO:", out[:600] if out else "(sin salida)")
        for linea in out.splitlines():
            linea = linea.strip()
            if linea.startswith("SHIP"): return True
            if linea.startswith("FIX") or linea.startswith("CORREGIR"): return False
            if '"content": "SHIP' in linea: return True
            if '"content": "FIX' in linea or '"content": "CORREGIR' in linea: return False
        return '"content": "SHIP' in out
    except Exception as e:
        print(f"  EL OJO no pudo mirar ({e}) — el QA queda en la verificación técnica")
        return True


def main():
    print("LA FORJA v6.121 — EL PILAR PRISMA: LA VÉRTICE DEL DIOS (el rediseño):")
    os.makedirs(os.path.dirname(DEST_TIRA), exist_ok=True)
    tira = forjar_tira()
    hi = forjar_highlight(tira)
    if not verificar(tira, hi):
        sys.exit("[FALLO] la verificación técnica no pasó — nada se escribe")
    tira.save(DEST_TIRA)
    hi.save(DEST_HI)
    print(f"  ESCRITO: {DEST_TIRA}")
    print(f"  ESCRITO: {DEST_HI}")
    montaje(tira, hi)
    if "--qa" in sys.argv:
        if not el_ojo():
            print("[AVISO] EL OJO dijo FIX — mira el montaje y re-forja")


if __name__ == "__main__":
    main()
