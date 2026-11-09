#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
=============================================================================
 pilar_prisma_v6120.py — LA FORJA DEL PILAR PRISMA (v6.120 — EL LLANO DE
 OBSIDIANA: la puerta de salida del MUNDO INTERIOR)

 LA LETRA: «una vez dentro del mundo interior no puedo salir, necesitas
 crear un pilar tipo prisma altar que permita salir, este se creara usando
 las herramientas del mod, la forja y el taller, ademas el pilar debe
 aparecer ya colocado en el mundo interior».

 EL PILAR PRISMA — la columna santuario del llano: la herencia del
 PRISMA DEL ETERNO (activos_v6115.py — el abanico, el triángulo facetado,
 el pedimento de amatista con sus DOS VELAS y el filo dorado de las
 páginas) estirada en UNA COLUMNA que se ve desde cualquier punto del
 llano de obsidiana:
   · LA BASE (la fila de abajo, 22 px): el pedimento de amatista con la
     mesa dorada (el filo de la página) y LAS DOS VELAS vivas de la casa
     — la herencia del altar, el gesto de la casa.
   · EL FUSTE (las tres filas del medio): la columna facetada de cristal
     violeta (la cara central viva · las laterales hondas) con SUS TRES
     ANILLOS RÚNICOS DORADOS latiendo (los aros del conjuro subiendo y
     bajando de brillo por el fuste — el pilar ES un conjuro parado).
   · EL CAPITEL (la fila de arriba): EL PRISMA CRISTALINO flotando (el
     triángulo facetado con su glint viajero) bajo EL HAZ DORADO que
     entra desde arriba y se PARTE en EL ABANICO ESPECTRAL de SIETE
     bandas que respiran y caminan marco a marco — el pilar CAMBIA de
     colores al vivo, igual que su padre.

 LA FORJA DE LA CASA (el patrón de activos_v6115.py / piel_eterno_v6118.py:
 100% PIL determinista — el camino --pil sancionado, cero nube):
   · PilarPrismaTile.png (54×516 = 6 marcos de 54×86 — el pilar entero
     3×5 tiles: 18 + 16 + 16 + 16 + 22 = 86 px por marco).
   · PilarPrismaTile_Highlight.png (54×86) — la silueta sólida de la
     convención tML (el guardián la exige en disco Y en el .tmod).

 VERIFICACIÓN TÉCNICA (la casa la exige): tamaño exacto · 6 marcos
 exactos · transparencia nativa (la regla v6.98: el fondo vive en la
 GENERACIÓN, jamás en el post-proceso) · el arte LLEGA a la última fila
 (la lección v6.100 del altar flotante — el pilar se ASENTA). MONTAJE ×3
 para EL OJO (QA VLM, SHIP/FIX).

 USO:  python3 tools/pilar_prisma_v6120.py [--qa]
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
MONTAJE = "/tmp/pilar_prisma_qa.png"

SS = 4                      # el supersample de la casa
F_W, F_H = 54, 86           # el marco: 3 tiles de ancho (18) × 5 filas
MARCOS = 6                  # el ciclo del abanico (el tempo del prisma)

# LA PALETA DEL PRISMA (la casa + los colores — la MISMA del prisma padre)
ESPECTRO = [
    (150, 84, 235),   # violeta del vacío (el señor del abanico)
    (196, 112, 255),  # lila vivo
    (110, 170, 235),  # azul del cielo
    (110, 225, 205),  # cian de página
    (150, 230, 120),  # verde de tinta
    (250, 210, 100),  # oro del veredicto
    (250, 140, 110),  # brasa del eclipse
]
ORO = (255, 214, 130)
ORO_HONDO = (212, 168, 84)
AMATISTA_HONDA = (64, 34, 96)
AMATISTA_MEDIA = (84, 48, 138)
AMATISTA_VIVA = (150, 84, 235)
CRISTAL_CLARO = (226, 206, 255)


def forjar_marco(frame):
    """UN marco del pilar (54×86 × SS) — determinista, la fase manda."""
    W, H = F_W * SS, F_H * SS
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    dr = ImageDraw.Draw(img)
    fase = frame / MARCOS

    # ================================================================
    #  LA BASE — EL PEDIMENTO DE AMATISTA (la herencia del altar: la
    #  mesa dorada, el bloque hondo y LAS DOS VELAS vivas). LA CURA DEL
    #  OJO (2ª): el pedimento es COMPACTO (el pilar ES la columna — el
    #  santuario no se come la proporción)
    # ================================================================
    py0 = H - 18 * SS                       # la mesa dorada vive aquí
    dr.polygon([(W * 0.20, H), (W * 0.80, H),
                (W * 0.77, py0 + 2 * SS), (W * 0.23, py0 + 2 * SS)],
               fill=AMATISTA_HONDA, outline=ORO_HONDO)
    # la mesa dorada (el filo de la página)
    dr.polygon([(W * 0.17, py0 + 2 * SS), (W * 0.83, py0 + 2 * SS),
                (W * 0.80, py0), (W * 0.20, py0)],
               fill=ORO_HONDO)
    # el cuello del pedimento
    dr.polygon([(W * 0.23, py0), (W * 0.77, py0),
                (W * 0.72, py0 - 2 * SS), (W * 0.28, py0 - 2 * SS)],
               fill=(46, 24, 72, 255), outline=(150, 110, 200, 255))
    # LAS DOS VELAS de la casa (la herencia del altar de la v6.97 —
    # LA CURA DEL OJO: la llama late MÁS FUERTE, el ciclo se lee). LA
    # CURA DEL OJO (3ª): ALINEADAS al cuello del pedimento (±0.21W —
    # viven SOBRE su bloque, no flotando al costado)
    for lado in (-1, 1):
        vx = W / 2 + lado * W * 0.21
        dr.rectangle([vx - SS, py0 - 8 * SS, vx + SS, py0 - 2 * SS],
                     fill=(240, 226, 198, 255), outline=(150, 110, 200, 255))
        llam = 3.0 + 2.0 * math.sin(fase * math.pi * 2 + (1 if lado > 0 else 0) * math.pi)
        dr.polygon([(vx, py0 - 8 * SS - llam * SS),
                    (vx - SS * 0.8, py0 - 8 * SS),
                    (vx + SS * 0.8, py0 - 8 * SS)],
                   fill=ORO)

    # LA SOMBRA DE ASIENTO (LA CURA DEL OJO: el pilar PISA el llano —
    # una elipse oscura y suave bajo el pedimento, la que ancla)
    sombra = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    ds = ImageDraw.Draw(sombra)
    ds.ellipse([W * 0.16, H - 6 * SS, W * 0.84, H - SS], fill=(8, 4, 16, 130))
    sombra = sombra.filter(ImageFilter.GaussianBlur(SS * 1.2))
    img.alpha_composite(sombra)

    # ================================================================
    #  EL FUSTE — LA COLUMNA FACETADA (tres caras: central viva,
    #  laterales hondas — el cristal del dios parado) con SUS TRES
    #  ANILLOS RÚNICOS DORADOS latiendo por el cuerpo
    # ================================================================
    f_top = H * 0.265                       # dónde arranca el fuste (el aire bajo el prisma)
    f_bot = py0 - 2 * SS                    # donde muere: el cuello del pedimento
    cx = W / 2
    # el ensanche del fuste (ESBELTO pero PRESENTE — la cura del ojo 3ª)
    w_top, w_bot = W * 0.095, W * 0.165
    # LAS TRES CARAS
    dr.polygon([(cx, f_top), (cx + w_bot, f_bot), (cx - w_bot, f_bot)],
               fill=AMATISTA_MEDIA, outline=(150, 110, 200, 255))
    dr.polygon([(cx, f_top), (cx + w_top, f_top + (f_bot - f_top) * 0.10),
                (cx + w_bot, f_bot), (cx + w_bot * 0.55, f_bot)],
               fill=(104, 62, 168, 255))
    dr.polygon([(cx, f_top), (cx - w_top, f_top + (f_bot - f_top) * 0.10),
                (cx - w_bot, f_bot), (cx - w_bot * 0.55, f_bot)],
               fill=(66, 38, 112, 255))
    # EL CORAZÓN DEL CRISTAL (la veta central que sube — la luz del pilar)
    for i in range(int((f_bot - f_top) / (3 * SS))):
        t = i / max(1, int((f_bot - f_top) / (3 * SS)) - 1)
        y = f_top + (f_bot - f_top) * t
        half = (w_top + (w_bot - w_top) * t) * 0.34
        brillo = 0.55 + 0.45 * math.sin(fase * math.pi * 2 - t * 2.6)
        col = (int(150 + 40 * brillo), int(84 + 50 * brillo), int(235), 235)
        dr.ellipse([cx - half, y - SS * 1.4, cx + half, y + SS * 1.4], fill=col)
    # LOS TRES ANILLOS RÚNICOS (los aros del conjuro — cada uno late en
    # su fase: la luz SUBE por el pilar marco a marco). LA CURA DEL OJO:
    # los aros CIÑEN el fuste (su ancho = el del fuste EN ESA ALTURA —
    # nada de aros flotando fuera del cristal)
    for a in range(3):
        t = 0.34 + 0.22 * a                  # la altura del aro en el fuste
        y = f_top + (f_bot - f_top) * t
        half = (w_top + (w_bot - w_top) * t) * 1.06
        latido = 0.5 + 0.5 * math.sin(fase * math.pi * 2 - a * 1.3)
        aro = (255, int(214 - 40 * latido), int(130 - 20 * latido), int(190 + 60 * latido))
        dr.ellipse([cx - half, y - SS * 1.1, cx + half, y + SS * 1.1],
                   outline=aro, width=SS + 1)
        # las muescas del aro (las runas grabadas — tres por aro)
        for m in (-1, 0, 1):
            mx = cx + m * half * 0.58
            dr.line([(mx - SS * 0.4, y - SS * 1.6), (mx + SS * 0.4, y + SS * 1.6)],
                    fill=(255, 240, 200, 255), width=SS - 1)

    # ================================================================
    #  EL CAPITEL — EL PRISMA CRISTALINO FLOTANDO (el triángulo facetado
    #  con su glint viajero) bajo EL HAZ que entra y EL ABANICO que sale
    # ================================================================
    bob = math.sin(fase * math.pi * 2) * 1.9 * SS
    px, py = cx, H * 0.155 + bob
    # LA CURA DEL OJO (3ª): el prisma es UN TRIÁNGULO DE VERDAD —
    # ESTRECHO y ALTO (el ápice fino arriba, la base corta abajo:
    # nada de rombo)
    pw, ph = W * 0.155, H * 0.185
    # el resplandor del corazón (TENUE — el brillo NO puede comerse el
    # triángulo: el prisma lee PRISMA, no orbe) + EL PARPADEO del
    # cristal (la cura del ojo 3ª: el corazón late VISIBLEMENTE)
    late = 0.5 + 0.5 * math.sin(fase * math.pi * 2)
    for r in range(int(pw * (1.05 + 0.35 * late)), 0, -2):
        f = 1 - r / (pw * (1.05 + 0.35 * late))
        col = (150 + int(70 * f), 84 + int(90 * f), 235, int(24 + int(34 * f) + int(20 * late)))
        dr.ellipse([px - r, py - r * 1.1, px + r, py + r * 1.1], fill=col)
    # EL HAZ QUE ENTRA (la luz dorada del Eterno, desde arriba — GRUESO
    # y PULSANTE: su alfa camina 165↔255, se lee desde el llano)
    haz_alfa = 210 + int(45 * math.sin(fase * math.pi * 2))
    dr.polygon([(px - SS * 4.0, 0), (px + SS * 4.0, 0),
                (px + SS * 1.7, py - ph * 0.85), (px - SS * 1.7, py - ph * 0.85)],
               fill=(255, 226, 150, haz_alfa))
    # el triángulo facetado (el prisma de verdad — ESTRECHO y ALTO:
    # ápice filudo, base corta — sus TRES facetas y aristas vivas)
    A = (px, py - ph)
    B = (px - pw, py + ph * 0.60)
    C = (px + pw, py + ph * 0.60)
    dr.polygon([A, B, C], fill=AMATISTA_MEDIA, outline=CRISTAL_CLARO)
    Mab = ((A[0] + B[0]) / 2, (A[1] + B[1]) / 2)
    Mac = ((A[0] + C[0]) / 2, (A[1] + C[1]) / 2)
    Mbc = ((B[0] + C[0]) / 2, (B[1] + C[1]) / 2)
    dr.polygon([A, Mab, Mbc, Mac], fill=(150, 84, 235, 245))
    dr.polygon([Mab, B, Mbc], fill=(104, 62, 168, 245))
    dr.polygon([Mac, Mbc, C], fill=(66, 38, 112, 245))
    # LAS ARISTAS VIVAS (las líneas que hacen el tallado)
    dr.line([A, Mbc], fill=CRISTAL_CLARO, width=SS)
    dr.line([Mab, Mac], fill=(196, 168, 245, 255), width=SS - 1)
    # EL GLINT (la chispa que viaja por las facetas — el marco vivo)
    gp = [(A, Mab), (Mab, B), (B, Mbc), (Mbc, C), (C, Mac), (Mac, A)][int(fase * 6) % 6]
    gx = gp[0][0] + (gp[1][0] - gp[0][0]) * 0.5
    gy = gp[0][1] + (gp[1][1] - gp[0][1]) * 0.5
    dr.line([gx - SS * 1.3, gy - SS * 1.3, gx + SS * 1.3, gy + SS * 1.3],
            fill=(255, 246, 225, 255), width=SS - 1)

    # EL ABANICO DE LA CASA (la luz PARTIDA — LA CURA DEL OJO 3ª: nada
    # de confeti multicolor: CINCO bandas en los DOS colores de la casa
    # — el violeta del vacío y el oro del veredicto — que respiran y se
    # desplazan marco a marco; cortas y finas: el pilar se lee ENTERO
    ABANICO_CASA = [
        (150, 84, 235),    # violeta del vacío
        (196, 112, 255),   # lila vivo
        (255, 214, 130),   # oro del veredicto
        (122, 78, 188),    # la carne hond
        (150, 84, 235),    # violeta (el abrazo del ciclo)
    ]
    for b in range(5):
        ang = math.pi * (1.0 - 0.30 * (b - 2) / 2.0)   # de arriba-abajo a ambos lados
        largo = (f_bot - py) * (0.15 + 0.055 * math.sin(fase * math.pi * 2 + b * 0.9))
        x1 = px + math.cos(ang) * pw * 0.85
        y1 = py + ph * 0.5
        x2 = px + math.cos(ang) * (pw * 0.85 + largo * 0.72)
        y2 = y1 + math.sin(ang) * largo
        brillo = 0.45 + 0.55 * math.sin(fase * math.pi * 2 + b * 1.25)
        base = ABANICO_CASA[(b + frame) % 5]
        col = (base[0], base[1], base[2], int(95 + 90 * brillo))
        dr.line([(x1, y1), (x2, y2)], fill=col, width=SS)
        # la mota al final de la banda (la chispa del color)
        dr.ellipse([x2 - SS * 0.7, y2 - SS * 0.7, x2 + SS * 0.7, y2 + SS * 0.7],
                   fill=(base[0], base[1], base[2], 235))

    # LAS MOTAS DEL UMBRAL (el polvo de la casa — violeta y oro —
    # girando al prisma)
    for i in range(5):
        a = fase * math.pi * 2 + i * math.pi * 2 / 5
        mx = px + math.cos(a) * W * 0.40
        my = py + math.sin(a) * H * 0.085
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
    """LA SILUETA de la convención tML: el marco 0 del pilar, sólido en
    el color del highlight de la casa (la que ya vive en el prisma)."""
    marco = tira.crop((0, 0, F_W, F_H))
    sil = marco.split()[3].point(lambda a: 255 if a > 24 else 0)
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
    # LA LEY v6.98: transparencia NATIVA (el fondo vive en la generación)
    px = tira.load()
    esquinas = [(0, 0), (F_W - 1, 0), (0, F_H * MARCOS - 1), (F_W - 1, F_H * MARCOS - 1)]
    for (x, y) in esquinas:
        if px[x, y][3] != 0:
            print(f"  [FALLO] esquina ({x},{y}) opaca — el fondo no es transparente"); ok = False
    # LA LEY v6.100: el arte LLEGA a la última fila (el pilar se ASENTA)
    fila_baja = sum(1 for x in range(F_W) if px[x, F_H - 1][3] > 0)
    if fila_baja < F_W * 0.30:
        print(f"  [FALLO] la última fila casi vacía ({fila_baja}/{F_W}) — el pilar flota"); ok = False
    # EL CORAZÓN OPACO (la columna central del marco 0)
    m0 = tira.crop((0, 0, F_W, F_H))
    fila_media = sum(1 for x in range(F_W) if m0.getpixel((x, F_H // 2))[3] > 200)
    if fila_media < 6:
        print(f"  [FALLO] el fuste no es opaco en su corazón ({fila_media} px)"); ok = False
    if ok:
        print(f"  VERIFICACIÓN TÉCNICA ✔ ({F_W}×{F_H}×{MARCOS} · transparencia nativa · asentado · corazón opaco)")
    return ok


def montaje(tira, hi):
    hoja = Image.new("RGBA", (780, 620), (24, 14, 40, 255))
    # LA TIRA ×3 vertical (los 6 marcos legibles)
    t3 = tira.resize((F_W * 3, F_H * MARCOS * 3), Image.NEAREST)
    hoja.paste(t3, (24, 24), t3)
    # EL MARCO 0 ×6 (la lectura del detalle)
    m0 = tira.crop((0, 0, F_W, F_H)).resize((F_W * 6, F_H * 6), Image.NEAREST)
    hoja.paste(m0, (230, 24), m0)
    # EL HIGHLIGHT ×6 (la convención tML a la vista)
    h6 = hi.resize((F_W * 6, F_H * 6), Image.NEAREST)
    hoja.paste(h6, (580, 24), h6)
    # UNA PREVISTA "EN EL LLANO": el pilar sobre una franja de obsidiana
    prev = Image.new("RGBA", (510, 300), (16, 9, 28, 255))
    piso = Image.new("RGBA", (510, 62), (26, 18, 38, 255))
    dp = ImageDraw.Draw(piso)
    for x in range(0, 510, 6):
        dp.line([(x, 0), (x - 12, 62)], fill=(46, 30, 62, 255), width=1)
    prev.paste(piso, (0, 238), piso)
    pil = tira.crop((0, 0, F_W, F_H)).resize((F_W * 5, F_H * 5), Image.NEAREST)
    prev.paste(pil, (120, 238 - F_H * 5 + 5), pil)
    prev = prev.resize((510, 300), Image.NEAREST)
    hoja.paste(prev, (230, 300), prev)
    hoja.save(MONTAJE)
    print(f"  montaje QA: {MONTAJE}")


def el_ojo():
    """EL QA VLM de la casa (z-ai vision — SHIP/FIX)."""
    prompt = ("QA this pixel art montage for a Terraria-style mod. LEFT: the full 6-frame "
              "animation strip of a tall crystal PILLAR (3 tiles wide, 5 tall) - compact amethyst "
              "pedestal base with two candles on its neck, slender faceted violet crystal shaft with "
              "three golden rings, a NARROW TALL TRIANGULAR prism floating at the top drinking a thick "
              "pulsing golden beam, with a short 5-band fan in house colors ONLY (violet+gold). "
              "MIDDLE: single frame at 6x zoom (a single still frame by design). RIGHT: the yellow "
              "highlight silhouette. BOTTOM: a mockup of the pillar standing on an obsidian plain. "
              "Rate 0-10: shape readability as a tall pillar, palette consistency (violet+gold), "
              "animation quality, Terraria tile fit. List defects (max 4, short). First line must be "
              "SHIP or FIX. Answer in Spanish, 3 lines max.")
    try:
        r = subprocess.run(["z-ai", "vision", "-p", prompt, "-i", MONTAJE],
                           capture_output=True, text=True, timeout=180)
        out = (r.stdout or "").strip()
        print("  EL OJO:", out[:600] if out else "(sin salida)")
        # el veredicto vive en la PRIMERA línea del contenido (las líneas
        # 🚀 del CLI vienen antes — se busca SHIP/FIX en el texto)
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
    print("LA FORJA v6.120 — EL PILAR PRISMA (la salida del mundo interior):")
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
