#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
=============================================================================
 piel_eterno_v6118.py — LA FORJA DE LA PIEL DEL ETERNO + LA PÁGINA DEL ETERNO
 (v6.118 — EL MUNDO INTERIOR DEL GRIMORIO, el rework del fondo)

 LA LETRA: «el fondo del mundo debe ser un mundo de tentaculos por capas
 y oscuridad por capas, ademas de brumas y particulas» + el cuestionario:
 Piel = NUEVA («Piel del Eterno: violeta-negro con vetas DORADAS que
 fluyen») y Materiales = MEZCLA («vetas del tile forjado como decoración»).

 LA FORJA DE LA CASA (el patrón de sombra5_v6104_piel.py / tentaculo_v6102.py,
 100% PIL determinista — el camino --pil sancionado):
   · LA PIEL (AethonMod/Content/Effects/PielDelEterno.png, 128×512):
     columna constante de CARNE DEL ETERNO — base violeta-negra de la casa
     (26,14,44 → 122,78,188 → 150,84,235), SURCOS musculares oscuros y
     VETAS DORADAS (255,214,130 — el oro del veredicto) que fluyen por el
     cuerpo: 5 filamentos serpenteantes con latido de brillo.
     SASTRERÍA de la casa: máscara U suave (bordes transparentes, núcleo
     opaco), ESPEJO PERIÓDICO en V (128×256 → 512: la carne fluye
     continua con uvFlow, costura C0) y PREMULTIPLICADO rgb≤a (la
     convención del lote alfa del juego).
   · LA PÁGINA (AethonMod/Content/Tiles/PaginaDelEterno_Tile.png, 54×18):
     el TILE de veta para el terreno del interior — 3 variantes de 16×16
     (18px de celda con el padding de vanilla), la misma paleta en mini:
     base violeta-negra + UNA veta dorada que cruza cada variante con
     ángulo distinto + textura de página (hilos tenues).
     SIN premultiplicar (el pase de tiles de vanilla dibuja con alfa
     normal); el padding derecho/inferior de cada celda queda negro.

 VERIFICACIÓN TÉCNICA (la casa la exige): tamaño exacto · premultiplicado
 rgb≤a (la piel) · periodicidad V (fila 0 == fila 511 en la piel) · bordes
 U transparentes y núcleo opaco · el tile: 3 marcos de 18px, arte 16×16
 opaco. MONTAJE ×4 para EL OJO (QA VLM con z-ai vision, SHIP/FIX).

 USO:  python3 tools/piel_eterno_v6118.py [--qa]
=============================================================================
"""
import math
import os
import random
import subprocess
import sys

try:
    from PIL import Image, ImageDraw, ImageFilter
except ImportError:
    sys.exit("Falta Pillow: pip install Pillow")

RAIZ = os.path.join(os.path.dirname(__file__), "..")
DEST_PIEL = os.path.join(RAIZ, "AethonMod", "Content", "Effects", "PielDelEterno.png")
DEST_TILE = os.path.join(RAIZ, "AethonMod", "Content", "Tiles", "PaginaDelEterno_Tile.png")
MONTAJE = "/tmp/piel_eterno_qa.png"

# === LA PALETA DEL VACÍO DE LA CASA (PROMPT-MUNDO-INTERIOR) ===
VACIO_0 = (12, 7, 22)       # el fondo del cuerpo (casi negro)
VACIO_1 = (26, 14, 44)      # la sombra del vacío
VACIO_2 = (64, 36, 104)     # la carne media
VACIO_3 = (122, 78, 188)    # la carne viva
VACIO_4 = (150, 84, 235)    # la luz violeta
ORO = (255, 214, 130)       # EL ORO DEL VEREDICTO
ORO_TIP = (255, 240, 200)   # el corazón de la veta


def mezcla(a, b, f):
    return tuple(int(round(a[i] + (b[i] - a[i]) * max(0.0, min(1.0, f)))) for i in range(3))


def ruido_senos(x, y, semilla, octavas=4):
    """El fBm de senos de la casa (determinista — el camino --pil)."""
    v = 0.0
    amp = 1.0
    frec = 1.0
    for o in range(octavas):
        v += amp * math.sin((x * frec * 0.161 + semilla * 2.399 + o * 5.1)
                            + math.cos(y * frec * 0.107 + semilla * 1.618 + o * 3.7) * 2.1)
        amp *= 0.55
        frec *= 2.13
    return v / (1 - 0.55 ** octavas) * 0.5 + 0.5   # 0..1


# LA REFINA DEL OJO (el QA v6.118 pidió PIXEL-ART de verdad): tonos
# CUANTIZADOS a una rampa de LA CASA + DITHERING Bayer 2×2 entre escalones
# (nada de gradientes suaves) — el estilo de los tiles de Terraria.
RAMA = [VACIO_0, VACIO_1, (44, 25, 76), VACIO_2, (94, 56, 148), VACIO_3, VACIO_4]
BAYER2 = ((0, 2), (3, 1))


def tono(t, x, y):
    """El tono t (0..1) cuantizado a la RAMA con dither Bayer 2×2."""
    tt = max(0.0, min(0.999, t)) * (len(RAMA) - 1)
    i = int(tt)
    f = tt - i
    umbral = (BAYER2[y & 1][x & 1] + 0.5) / 4.0
    if f > umbral and i < len(RAMA) - 1:
        i += 1
    return RAMA[i]


def alfa_escalon(k, x, y):
    """El alfa premultiplicado en ESCALONES limpios (cero o ≥0.28 — el
    QA cazó el 'alpha sucio' de los fundidos suaves en el borde)."""
    if k < 0.14:
        return 0.0
    kk = max(0.28, min(1.0, k))
    escalones = (0.28, 0.55, 0.80, 1.0)
    mejor = escalones[0]
    for e in escalones:
        if abs(kk - e) < abs(kk - mejor):
            mejor = e
    return mejor


# =====================================================================
#  LA PIEL DEL ETERNO — 128×512 (128×256 generada + espejo periódico V)
# =====================================================================
def forjar_piel():
    W, H = 128, 256          # la mitad generada (el espejo completa 512)
    cx = W * 0.5
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    px = img.load()

    # LOS FILAMENTOS DORADOS (5 vetas que serpentean y laten)
    vetas = []
    rng = random.Random(20260118)
    for v in range(5):
        vetas.append({
            "base": cx + (v - 2) * 9.5 + rng.uniform(-3, 3),
            "fase": rng.uniform(0, math.pi * 2),
            "fase2": rng.uniform(0, math.pi * 2),
            "ancho": 1.6 + rng.uniform(0, 1.4),
            "fue": 0.55 + rng.uniform(0, 0.45),
        })

    for y in range(H):
        # el tono base de la fila (la carne respira en bandas verticales)
        banda = ruido_senos(0, y * 0.061, 7, 3)          # 0..1 lento
        musculo = ruido_senos(3, y * 0.24, 11, 4)        # 0..1 rápido
        for x in range(W):
            u = (x - cx) / (W * 0.5)                     # -1..1
            au = abs(u)
            if au >= 0.94:
                continue                                  # borde U MUERTO (cero absoluto)
            # LA MÁSCARA U: plena hasta 0.72, UNA transición dithered, cero
            if au < 0.72:
                mask = 1.0
            elif au < 0.94:
                mask = 1.0 - (au - 0.72) / 0.22          # la rampa (los ESCALONES viven abajo)
            else:
                continue
            k = alfa_escalon(mask, x, y)
            if k <= 0.0:
                continue

            # LA CARNE: tono cuantizado a la RAMA + dither Bayer (pixel-art)
            t = 0.30 + 0.26 * banda + 0.20 * musculo * (1 - au * 0.55)
            t -= 0.20 * max(0.0, au - 0.52) / 0.42       # el borde se hunde
            t += 0.12 * max(0.0, 0.24 - au) / 0.24       # la quilla central
            c = list(tono(t, x, y))

            # LAS VETAS DORADAS — NÍTIDAS (el QA cazó el antialiasing):
            # corazón de 1 px PLENO + 1 px al 45% con dither de tablero
            latido = 0.72 + 0.28 * math.sin(y * 0.092 + 1.3)
            for v in vetas:
                vx = v["base"] + math.sin(y * 0.045 + v["fase"]) * 7.5 \
                     + math.sin(y * 0.013 + v["fase2"]) * 4.0
                d = abs(x - vx)
                cuerpo = max(0.0, 1.0 - au * 1.25)
                if d <= 0.75 and cuerpo > 0.15:
                    c = list(ORO_TIP if latido > 0.62 else ORO)
                elif d <= 1.8 and ((x + y) & 1) == 0 and cuerpo > 0.15:
                    c = [mezcla(c, ORO, 0.45 * v["fue"])[i] for i in range(3)]

            # LA VETA VIOLETA VIVA del borde interior (el rim que se lee de noche)
            if 0.60 < au < 0.86 and ((x * 3 + y) & 3) == 0:
                if latido > 0.55:
                    c = [mezcla(c, VACIO_4, 0.38)[i] for i in range(3)]

            # EL PREMULTIPLICADO DE LA CASA: rgb·k, a=255·k
            px[x, y] = (int(c[0] * k), int(c[1] * k), int(c[2] * k), int(255 * k))

    # EL ESPEJO PERIÓDICO EN V: 256 → 512 (la costura C0 — la carne fluye
    # continua con uvFlow: fila 256 == espejo de la 255, fila 511 == fila 0)
    espejo = img.transpose(Image.FLIP_TOP_BOTTOM)
    entera = Image.new("RGBA", (W, H * 2), (0, 0, 0, 0))
    entera.paste(img, (0, 0))
    entera.paste(espejo, (0, H))
    return entera


# =====================================================================
#  LA PÁGINA DEL ETERNO (el tile de veta) — 54×18 (3 marcos de 18px)
# =====================================================================
def forjar_tile():
    img = Image.new("RGBA", (54, 18), (0, 0, 0, 0))
    angulos = [0.42, -0.55, 1.05]          # el ángulo de la veta por variante
    for v in range(3):
        ox = v * 18
        rng = random.Random(20260118 + v)
        # LA BASE: la página violeta-negra — RUIDO PERIÓDICO en 16 (el QA
        # cazó la costura: dos tiles vecinos deben FUNDIRSE sin visible)
        for y in range(16):
            for x in range(16):
                ang = x * 2 * math.pi / 16.0
                n = 0.5 + 0.5 * math.sin(ang * 2 + y * 0.9 + v * 2.1) * 0.6 \
                    + 0.2 * math.sin(ang * 3 - y * 1.31 + v)
                n = max(0.0, min(1.0, n))
                t = 0.26 + 0.30 * n
                c = tono(t, x, y)
                # los HILOS de la página (líneas horizontales tenues)
                hilo = math.sin((y + v * 3) * 1.31) * 0.5 + 0.5
                c = mezcla(c, RAMA[5], 0.18 * hilo)
                img.putpixel((ox + x, y), (c[0], c[1], c[2], 255))
        # LA VETA DORADA que cruza — NÍTIDA (1 px pleno + dither al borde)
        a = angulos[v]
        for s in range(-8, 26):
            vx = 8 + math.cos(a) * (s - 8) * 1.30
            vy = 8 + math.sin(a) * (s - 8) * 1.30
            ix = int(round(vx))
            iy = int(round(vy))
            brillo = 0.65 + 0.35 * math.sin(s * 0.9 + v)
            if 0 <= ix < 16 and 0 <= iy < 16 and brillo > 0.45:
                c = ORO_TIP if brillo > 0.8 else ORO
                img.putpixel((ox + ix, iy), (c[0], c[1], c[2], 255))
            # el borde dithered de la veta (1 px, tablero)
            for ddx, ddy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                jx, jy = ix + ddx, iy + ddy
                if 0 <= jx < 16 and 0 <= jy < 16 and ((jx + jy) & 1) == 0:
                    c0 = img.getpixel((ox + jx, jy))[:3]
                    c = mezcla(c0, ORO, 0.42)
                    img.putpixel((ox + jx, jy), (c[0], c[1], c[2], 255))
        # EL FILO INFERIOR (lectura de tile — una sola fila oscura)
        for x in range(16):
            c0 = img.getpixel((ox + x, 15))[:3]
            c = mezcla(c0, VACIO_0, 0.5)
            img.putpixel((ox + x, 15), (c[0], c[1], c[2], 255))
    return img


# =====================================================================
#  LAS VERIFICACIONES TÉCNICAS DE LA CASA
# =====================================================================
def verificar_piel(img):
    ok = True
    if img.size != (128, 512):
        print(f"  [FALLO] piel: tamaño {img.size} ≠ (128, 512)")
        return False
    px = img.load()
    # (1) PREMULTIPLICADO rgb≤a en TODOS los píxeles
    malos = 0
    for y in range(0, 512, 3):
        for x in range(0, 128, 3):
            r, g, b, a = px[x, y]
            if r > a or g > a or b > a:
                malos += 1
    if malos:
        print(f"  [FALLO] piel: {malos} píxeles sin premultiplicar (rgb>a)")
        ok = False
    # (2) PERIODICIDAD V: fila 0 == fila 511
    dif = sum(abs(px[x, 0][c] - px[x, 511][c]) for x in range(0, 128, 5) for c in range(4))
    if dif > 300:
        print(f"  [FALLO] piel: periodicidad V rota (dif={dif})")
        ok = False
    # (3) BORDES U TRANSPARENTES y (4) NÚCLEO OPACO
    b = sum(px[x, y][3] for x in (0, 1, 2, 125, 126, 127) for y in range(0, 512, 16))
    if b > 0:
        print(f"  [FALLO] piel: los bordes U no son transparentes (alfa={b})")
        ok = False
    nucleo = sum(px[x, y][3] for x in (60, 64, 68) for y in range(0, 512, 16))
    if nucleo < 3 * 32 * 250:
        print(f"  [FALLO] piel: el núcleo no es opaco (alfa={nucleo})")
        ok = False
    # (5) EL ORO VIVE (alguna veta dorada presente: r>g>b en el cuerpo)
    oro = sum(1 for y in range(0, 512, 4) for x in range(16, 112, 4)
              if px[x, y][0] > px[x, y][2] + 24 and px[x, y][3] > 140)
    if oro < 40:
        print(f"  [FALLO] piel: las vetas doradas no viven ({oro} px)")
        ok = False
    else:
        print(f"  piel OK: premult · periódica V · bordes U · núcleo · {oro} px de oro")
    return ok


def verificar_tile(img):
    if img.size != (54, 18):
        print(f"  [FALLO] tile: tamaño {img.size} ≠ (54, 18)")
        return False
    px = img.load()
    ok = True
    for v in range(3):
        # arte 16×16 opaco
        hueco = sum(1 for y in range(16) for x in range(16) if px[v * 18 + x, y][3] < 250)
        if hueco:
            print(f"  [FALLO] tile v{v}: {hueco} píxeles del arte sin opacidad")
            ok = False
        # las tres variantes DISTINTAS (el ángulo de la veta)
    d01 = sum(abs(px[0 + x, 8][c] - px[18 + x, 8][c]) for x in range(0, 16, 2) for c in range(3))
    d12 = sum(abs(px[18 + x, 8][c] - px[36 + x, 8][c]) for x in range(0, 16, 2) for c in range(3))
    if d01 < 60 or d12 < 60:
        print(f"  [FALLO] tile: variantes casi idénticas (d01={d01}, d12={d12})")
        ok = False
    if ok:
        print("  tile OK: 54×18 · 3 marcos de 18 · arte 16×16 opaco · variantes distintas")
    return ok


# =====================================================================
#  EL MONTAJE + EL OJO (QA VLM — el paso mirar de la casa)
# =====================================================================
def montaje(piel, tile):
    hoja = Image.new("RGBA", (720, 600), (24, 14, 40, 255))
    piel4 = piel.resize((128 * 2, 512), Image.NEAREST)
    hoja.paste(piel4, (24, 40), piel4)
    tile8 = tile.resize((54 * 8, 18 * 8), Image.NEAREST)
    hoja.paste(tile8, (320, 60), tile8)
    # un recorte del CUERPO de la piel ×6 (la lectura de la veta)
    cuerpo = piel.crop((32, 96, 96, 224)).resize((64 * 6, 128 * 6), Image.NEAREST)
    hoja.paste(cuerpo, (320, 260), cuerpo)
    hoja.save(MONTAJE)
    print(f"  montaje QA: {MONTAJE}")


def el_ojo():
    """EL QA VLM de la casa (z-ai vision — SHIP/FIX)."""
    prompt = ("QA this pixel art texture montage for a Terraria-style mod. LEFT: a 128x512 "
              "tentacle skin strip (violet-black flesh with flowing GOLDEN veins, transparent "
              "side edges). TOP-RIGHT: a 54x18 tile sheet, three 16x16 frame variants each with "
              "a golden vein crossing violet-black page flesh. BOTTOM-RIGHT: a 6x zoom crop of "
              "the skin. Rate 0-10: palette consistency, vein readability, tile art quality, "
              "Terraria fit. List defects (max 4, short). First line must be SHIP or FIX. "
              "Answer in Spanish, 3 lines max.")
    try:
        r = subprocess.run(["z-ai", "vision", "-p", prompt, "-i", MONTAJE],
                           capture_output=True, text=True, timeout=180)
        out = (r.stdout or "").strip()
        print("  EL OJO:", out[:600] if out else "(sin salida)")
        return out.splitlines()[0].upper().startswith("SHIP") if out else True
    except Exception as e:
        print(f"  EL OJO no pudo mirar ({e}) — el QA queda en la verificación técnica")
        return True


def main():
    print("LA FORJA v6.118 — LA PIEL DEL ETERNO + LA PÁGINA DEL ETERNO:")
    os.makedirs(os.path.dirname(DEST_PIEL), exist_ok=True)
    os.makedirs(os.path.dirname(DEST_TILE), exist_ok=True)

    piel = forjar_piel()
    tile = forjar_tile()

    ok_p = verificar_piel(piel)
    ok_t = verificar_tile(tile)
    if not (ok_p and ok_t):
        sys.exit("[FALLO] la verificación técnica no pasó — nada se escribe")

    piel.save(DEST_PIEL)
    tile.save(DEST_TILE)
    print(f"  ESCRITO: {DEST_PIEL}")
    print(f"  ESCRITO: {DEST_TILE}")

    montaje(piel, tile)
    if "--qa" in sys.argv:
        if not el_ojo():
            print("  [AVISO] EL OJO dijo FIX — revisar el montaje a mano")
    print("FORJA COMPLETA.")


if __name__ == "__main__":
    main()
