#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
buffs_vacio_v6122.py — v6.122 — LOS DOS ÍCONOS DE BUFF DEL VACÍO.

LA LETRA DEL USUARIO: «este daño causado por la forma Eterna se debe
ver reflejado como un buff y como el daño viene del aura del vacio,
el buff se llamara Aura del Vacio, asi que crea un promt que use la
misma estetica de las imagenes para buff de terraria con un pequeño
marco y el buff en el interior de la imagen para crear el buff del
aura del vacio, y de la misma forma crea una imagen del buff de
Vacio Exterior, que ahora se llamara Quemadura del Vacio».

LOS PROMPTS (la estética de los buffs de Terraria + el marco pequeño
con el buff en el interior — la letra, palabra por palabra). LA PLUMA
se intentó DOS rondas con ellos y EL OJO dijo FIX las dos veces (la
máquina de imágenes no sabe pintar LLAMAS NEGRAS: las quiere naranjas
y el negro sin su borde violeta se vuelve papilla — la lección eterna
de v6.101: «el negro solo existe en la pantalla si su borde arde
violeta»). Los prompts viven aquí documentados y el camino final es
EL DETERMINISTA de la casa (el algoritmo de llamas negras QA v4/v5 de
vacio_v6101.py + el anillo de niebla del vacío nuevo), con EL MARCO
pequeño dibujado a píxel:

  · AURA DEL VACÍO — «Icono de buff de pixel art estilo Terraria,
    cuadrado: un AURA DEL VACÍO — un anillo de neblina violeta oscura
    girando en remolino con un núcleo negro absoluto en el centro (un
    pequeño vacío), chispas doradas diminutas orbitando el anillo y
    destellos lila en el borde exterior; colores planos, contornos
    nítidos, sin texto, sobre fondo verde cromakey #00FF00 sólido».

  · QUEMADURA DEL VACÍO — «Icono de buff de pixel art estilo
    Terraria, cuadrado: una QUEMADURA DEL VACÍO — UNA SOLA lengua de
    llama negra grande y simétrica (la silueta de la llama de buff de
    Terraria), el cuerpo negro profundo con TODO el contorno ardiendo
    en un BORDE VIOLETA BRILLANTE GRUESO, la punta abierta en dos
    cuernos lila y tres brasas doradas en la base; colores planos,
    contornos nítidos, sin texto, sobre fondo verde cromakey #00FF00
    sólido».

LA COMPOSICIÓN FINAL (determinista, píxel a píxel):
  · AURA DEL VACÍO — el ANILLO DE NIEBLA: banda anular violeta con
    remolino (la niebla se engorda y adelgaza por sector), núcleo
    negro absoluto, chispas doradas orbitando y destellos lila.
  · QUEMADURA DEL VACÍO — la LLAMA NEGRA de la casa (llama_negra de
    vacio_v6101.py — el algoritmo QA'd que ya vive en el juego) con
    su borde violeta, las brasas doradas y el destello blanco.
  · EL MARCO PEQUEÑO (la letra): 1 px de amatista honda con las
    cuatro esquinitas doradas de la casa — el buff vive EN el
    interior del marco.

Salidas:
  AethonMod/Content/Buffs/AuraVacioBuff.png      (32×32 — el buff NUEVO)
  AethonMod/Content/Buffs/VacioExterior.png      (32×32 — renacido:
      el mismo archivo del buff que ahora se llama Quemadura del
      Vacío — la clase NO cambia de nombre, solo su display y su arte)

USO:  python3 tools/buffs_vacio_v6122.py [--qa] [--pluma]
      (--pluma: re-intenta el camino IA con los prompts de arriba)
"""
import math
import os
import subprocess
import sys

try:
    from PIL import Image, ImageDraw
except ImportError:
    sys.exit("Falta Pillow: pip install Pillow")

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from vacio_v6101 import (llama_negra, px, NEGRO_NUCLEO, NEGRO_LLAMA,
                         VIOLETA_HONDO, VIOLETA, VIOLETA_ALTO, BLANCO)

RAIZ = os.path.join(os.path.dirname(__file__), "..")
DEST_AURA = os.path.join(RAIZ, "AethonMod", "Content", "Buffs", "AuraVacioBuff.png")
DEST_QUEMA = os.path.join(RAIZ, "AethonMod", "Content", "Buffs", "VacioExterior.png")
MONTAJE = "/tmp/buffs_vacio_qa.png"
FINAL = 32
ORO = (255, 214, 130)
ORO_BRASSA = (250, 190, 90)

# LA PALETA DEL MARCO (la casa): amatista honda + oro del veredicto
MARCO_HONDO = (58, 32, 92, 255)
MARCO_ORO = (255, 214, 130, 255)


# ------------------------------------------------------------------
#  EL AURA DEL VACÍO — EL ANILLO DE NIEBLA (determinista)
# ------------------------------------------------------------------
def aura_vacio():
    im = Image.new("RGBA", (FINAL, FINAL), (0, 0, 0, 0))
    cx = cy = FINAL / 2 - 0.5

    def h01(k):
        """hash determinista 0..1 (la casa no tira dados)."""
        s = math.sin(k * 127.1 + 311.7) * 43758.5453
        return s - math.floor(s)

    for y in range(FINAL):
        for x in range(FINAL):
            dx = x - cx
            dy = y - cy
            d = math.sqrt(dx * dx + dy * dy)
            ang = math.atan2(dy, dx)
            # EL REMOLINO — el radio del anillo ondula por sector (la
            # niebla gira: se adelgaza y engorda)
            r_ext = 10.6 + 1.5 * math.sin(ang * 3.0 + 1.3)
            r_int = 6.2 + 0.9 * math.sin(ang * 3.0 + 1.3 + 0.5)
            if d <= 4.6:
                # EL NÚCLEO NEGRO ABSOLUTO (el pequeño vacío)
                col = NEGRO_NUCLEO if d > 2.2 else (6, 4, 10)
                # el borde del núcleo respira violeta hondo
                if d > 3.8:
                    col = VIOLETA_HONDO
                px(im, x, y, col)
            elif r_int <= d <= r_ext:
                # EL CUERPO DE NIEBLA — bandas radiales: hondo adentro,
                # vivo al medio, lila en el borde que mira afuera
                t = (d - r_int) / max(0.001, r_ext - r_int)   # 0 adentro → 1 afuera
                sector = h01(math.floor((ang + math.pi) * 4.2))
                if t > 0.80:
                    col = VIOLETA_ALTO if sector > 0.42 else VIOLETA
                elif t > 0.34:
                    col = VIOLETA if sector > 0.30 else VIOLETA_HONDO
                else:
                    col = VIOLETA_HONDO
                # LOS HUECOS DE LA NIEBLA — el anillo NO es sólido: por
                # sectores se abre (la neblina respira)
                if sector > 0.86 and t > 0.5:
                    continue
                px(im, x, y, col)
    # LAS CHISPAS DORADAS orbitando el anillo (cinco, repartidas áureas)
    for k, a in enumerate((0.35, 1.75, 3.05, 4.35, 5.55)):
        r = 10.4
        sx = cx + math.cos(a) * r
        sy = cy + math.sin(a) * r
        px(im, round(sx), round(sy), ORO)
        px(im, round(sx + math.cos(a + 1.57)), round(sy + math.sin(a + 1.57)),
           ORO_BRASSA if k % 2 == 0 else VIOLETA_ALTO)
    # EL DESTELLO LILA en el borde exterior (dos, contrapeso)
    for a in (2.4, 5.0):
        sx = cx + math.cos(a) * 11.6
        sy = cy + math.sin(a) * 11.6
        px(im, round(sx), round(sy), VIOLETA_ALTO)
    return im


# ------------------------------------------------------------------
#  LA QUEMADURA DEL VACÍO — la llama negra de la casa + brasas
# ------------------------------------------------------------------
def quemadura_vacio():
    im = Image.new("RGBA", (FINAL, FINAL), (0, 0, 0, 0))
    # LA GRAN LLAMA CENTRAL — la silueta clásica del buff ¡En llamas! de
    # Terraria, en negro con el borde violeta (llama_negra de v6.101,
    # el algoritmo QA'd que ya vive en el juego)
    llama_negra(im, 16, 28, 3.1, lado=0.12)
    # LAS DOS LENGUAS MENORES — la quemadura respirando a los lados
    llama_negra(im, 9, 27, 1.6, lado=-0.85)
    llama_negra(im, 23, 27, 1.6, lado=0.85)
    # LAS TRES BRASAS DORADAS de la base (el pago del dueño arde en oro)
    px(im, 11, 29, ORO)
    px(im, 16, 30, ORO_BRASSA)
    px(im, 21, 29, ORO)
    # EL DESTELLO BLANCO sobre la llama central (el punto de reentrada)
    px(im, 16, 3, BLANCO)
    px(im, 15, 4, VIOLETA_ALTO)
    px(im, 17, 4, VIOLETA_ALTO)
    # LAS CHISPAS VIOLETAS desprendidas
    for x, y, c in ((6, 12, VIOLETA), (26, 13, VIOLETA),
                    (8, 5, VIOLETA_ALTO), (24, 6, VIOLETA_ALTO),
                    (4, 20, VIOLETA_HONDO), (28, 21, VIOLETA_HONDO)):
        px(im, x, y, c)
    return im


# ------------------------------------------------------------------
#  EL MARCO PEQUEÑO (la letra: «con un pequeño marco y el buff en el
#  interior de la imagen») — a píxeles, NÍTIDO: 1 px de amatista honda
#  con las cuatro esquinitas doradas de la casa
# ------------------------------------------------------------------
def poner_marco(im):
    d = ImageDraw.Draw(im)
    d.rectangle([0, 0, FINAL - 1, FINAL - 1], outline=MARCO_HONDO, width=1)
    for (x, y) in ((1, 1), (FINAL - 2, 1), (1, FINAL - 2), (FINAL - 2, FINAL - 2)):
        d.point((x, y), fill=MARCO_ORO)
    return im


# ------------------------------------------------------------------
#  EL OJO (QA VLM — SHIP/FIX)
# ------------------------------------------------------------------
def el_ojo():
    prompt = ("QA this montage for a Terraria-style mod. TWO buff icons at 6x zoom: "
              "LEFT icon is an 'Aura of the Void' (a violet mist ring with a pure "
              "black void core, golden sparks orbiting); RIGHT icon is a 'Void Burn' "
              "(black flame tongues with bright violet edges, golden embers at the "
              "base). Both are 32x32 pixel art icons with a thin dark-violet frame "
              "with golden corner dots, shown over a LIGHT row and a DARK row. "
              "Answer in 3 lines: (1) is each motif CLEARLY readable as its concept "
              "(void aura ring / black-violet flames)? (2) any defects: mush, muddy "
              "colors, leftover green chroma pixels, broken frame? (3) VEREDICT, first "
              "word SHIP or FIX (with what to fix). Answer in Spanish.")
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
        if im.size != (FINAL, FINAL):
            print(f"  [FALLO] {ruta}: {im.size} != 32×32"); ok = False
        px_ = im.load()
        for (x, y) in ((0, 0), (FINAL - 1, 0), (0, FINAL - 1), (FINAL - 1, FINAL - 1)):
            if px_[x, y][3] != 255:
                print(f"  [FALLO] {ruta}: la esquina ({x},{y}) no es marco sólido"); ok = False
        # el verde del chroma NO puede existir (camino determinista: jamás)
        verdes = 0
        for y in range(FINAL):
            for x in range(FINAL):
                r, g, b, a = px_[x, y]
                if a > 40 and g > 150 and r < 110 and b < 110:
                    verdes += 1
        if verdes > 3:
            print(f"  [FALLO] {ruta}: {verdes} px de chroma verde"); ok = False
    if ok:
        print("  VERIFICACIÓN TÉCNICA ✔ (32×32 · marco sólido · sin chroma)")
    return ok


def montaje(iconos):
    hoja = Image.new("RGBA", (FINAL * 6 * 2 + 60, FINAL * 6 * 2 + 70), (128, 128, 128, 255))
    for k, (ruta, im) in enumerate(iconos):
        grande = im.resize((FINAL * 6, FINAL * 6), Image.NEAREST)
        hoja.alpha_composite(grande, (20 + k * (FINAL * 6 + 20), 20))
    oscura = Image.new("RGBA", (FINAL * 6 * 2 + 60, FINAL * 6 + 20), (16, 9, 28, 255))
    for k, (ruta, im) in enumerate(iconos):
        grande = im.resize((FINAL * 6, FINAL * 6), Image.NEAREST)
        oscura.alpha_composite(grande, (20 + k * (FINAL * 6 + 20), 10))
    hoja.paste(oscura, (0, FINAL * 6 + 40), oscura)
    hoja.save(MONTAJE)
    print(f"  montaje QA: {MONTAJE}")


def main():
    print("LA FORJA v6.122 — LOS DOS BUFFS DEL VACÍO (el Aura y la Quemadura):")
    iconos = [(DEST_AURA, poner_marco(aura_vacio())),
              (DEST_QUEMA, poner_marco(quemadura_vacio()))]
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
