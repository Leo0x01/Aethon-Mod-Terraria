#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_icono_ojo_v650111.py — EL ICONO DEL BUFF DE LOS OJOS (32×32).

La letra del usuario (v6.50.111): «el icono que muestra el buff del
minion es demasiado grande» — OjoEternoBuff apuntaba la textura AL
SPRITE DEL PROYECTIL (56×54: el mismo del minion) y el HUD de buffs
dibuja la textura TAL CUAL: el icono salía GIGANTE, montado sobre los
buffs vecinos (el estándar de la casa es 32×32 — los otros 10 buffs
del mod ya lo son).

EL DISEÑO: lo que el buff REPRESENTA — el OJO DEL ETERNO tal como lo
dibuja el juego: la base OjoEterno_Abierto (56×54) CON EL IRIS en su
posición de reposo (OjoEternoMinion: iris 13×13 — IRIS_ESCALA 0,41 de
la caja 32×32 — centrado en IRIS_REST (0,7 · 2,3)), reducido al
lienzo 32×32. El iris se compone AQUÍ porque en runtime el juego lo
dibuja aparte — el sprite base está SIN pupila.

SALIDA: AethonMod/Content/Buffs/OjoEternoBuff.png (32×32 RGBA) — la
ruta por CONVENCIÓN (el override Texture muere en la .111) + tres
candidatos a 8× en tools/tools/ para el ojo humano/VLM.
"""
import os

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
RAIZ = os.path.join(HERE, "..", "AethonMod")
OJO = os.path.join(RAIZ, "Content", "Projectiles", "OjoEterno_Abierto.png")
IRIS = os.path.join(RAIZ, "Content", "Weapons", "GrimorioHambriento_Iris.png")
OUT = os.path.join(RAIZ, "Content", "Buffs", "OjoEternoBuff.png")

# --- la geometría del runtime (OjoEternoMinion.cs, ESCALA 1: arte crudo) ---
IRIS_ESCALA = 0.41          # caja 32 → disco ~13 px
IRIS_REST_X = 0.7           # el reposo del iris (px, desde el centro)
IRIS_REST_Y = 2.3

def componer():
    """El ojo COMO LO DIBUJA EL JUEGO: base + iris en reposo."""
    ojo = Image.open(OJO).convert("RGBA")
    iris = Image.open(IRIS).convert("RGBA")

    caja = max(1, int(round(32 * IRIS_ESCALA)))          # 13
    iris_peq = iris.resize((caja, caja), Image.NEAREST)  # pixel art

    lienzo = ojo.copy()
    cx, cy = ojo.width / 2.0, ojo.height / 2.0           # 28 · 27
    px = int(round(cx + IRIS_REST_X - caja / 2.0))       # 22
    py = int(round(cy + IRIS_REST_Y - caja / 2.0))       # 23
    lienzo.alpha_composite(iris_peq, (px, py))
    return lienzo

def main():
    compuesto = componer()
    print(f"compuesto: {compuesto.size[0]}×{compuesto.size[1]} (base + iris en reposo)")

    candidatos = {}

    # A) NEAREST — recorta píxeles: el más «pixel», puede perder filas
    candidatos["NEAREST"] = compuesto.resize((32, 32), Image.NEAREST)

    # B) BOX — promedia el área: suave pero fiel al color total
    box = compuesto.resize((32, 32), Image.BOX)
    candidatos["BOX"] = box

    # C) BOX + FILO — BOX y luego re-endurecer el alfa (los bordes a
    #    3 niveles: sólido / medio / nada) para que NO parezca mancha
    filo = box.copy()
    a = filo.getchannel("A")
    a = a.point(lambda v: 255 if v > 190 else (128 if v > 70 else 0))
    filo.putalpha(a)
    candidatos["FILO"] = filo

    os.makedirs(os.path.join(HERE, "tools"), exist_ok=True)
    for nombre, im in candidatos.items():
        im.save(OUT if nombre == "FILO" else
                os.path.join(HERE, "tools", f"icono_ojo_v650111_{nombre}.png"))
        # el panel de inspección (8×, nearest: como el zoom del HUD)
        im.resize((256, 256), Image.NEAREST).save(
            os.path.join(HERE, "tools", f"icono_ojo_v650111_{nombre}_8x.png"))
        print(f"  candidato {nombre}: {im.size[0]}×{im.size[1]}")

    print(f"ESCOGIDO (FILO) → {OUT}")

if __name__ == "__main__":
    main()
