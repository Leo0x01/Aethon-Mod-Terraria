#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
GEN_CODICE_STRIP_ITEM — v6.50.71 — EL SPRITE DE ITEM ANIMADO DEL CÓDICE VIVO.

El usuario pidió el arma "completamente a parte": su sprite nace de CÓDIGO
(codigo.txt — la matriz de píxeles) y por eso SE ANIMA. Este script toma
los 8 frames ya animados por gen_codice_vivo_v65071.py (pulso de energía +
parpadeo del ojo, 313×313 en /tmp/codice/) y compone EL STRIP VERTICAL DEL
ITEM: 8 frames de 48×48 → 48×384 (formato tModLoader: se registra con
Main.RegisterItemAnimation + ItemID.Sets.AnimatesAsSoul → el icono ANIMA
en el inventario, y el objeto tirado en el mundo también).

Downscale: BOX (área) + alfa binarizado + SNAP a la paleta original de 26
colores (pixel-art limpio). Determinista: sin random.
"""

import re
import sys
import hashlib
from pathlib import Path
from PIL import Image

REPO = Path(__file__).resolve().parent.parent
CODIGO = Path("/home/z/my-project/upload/codigo.txt")
FRAMES_DIR = Path("/tmp/codice")
DESTINO = REPO / "AethonMod" / "Content" / "Weapons" / "CodiceVivo" / "CodiceVivo.png"

LADO = 48          # lado de cada frame del item
N_FRAMES = 8


def parsear_paleta(texto: str) -> list:
    pares = re.findall(r"([a-z]):\s*'(#[0-9a-fA-F]{6})'", texto)
    return [(k, tuple(int(h[i:i + 2], 16) for i in (1, 3, 5))) for k, h in pares]


def snap(px, paleta_rgb):
    r, g, b = px[:3]
    mejor, d2 = None, None
    for c in paleta_rgb:
        dd = (r - c[0]) ** 2 + (g - c[1]) ** 2 + (b - c[2]) ** 2
        if d2 is None or dd < d2:
            mejor, d2 = c, dd
    return mejor


def reducir(im: Image.Image, paleta_rgb) -> Image.Image:
    im = im.resize((LADO, LADO), Image.BOX).convert("RGBA")
    px = im.load()
    for y in range(LADO):
        for x in range(LADO):
            r, g, b, a = px[x, y]
            if a < 128:
                px[x, y] = (0, 0, 0, 0)
            else:
                c = snap((r, g, b), paleta_rgb)
                px[x, y] = (c[0], c[1], c[2], 255)
    return im


def main() -> int:
    if not CODIGO.exists():
        print(f"FALTA {CODIGO}")
        return 1
    paleta = parsear_paleta(CODIGO.read_text(encoding="utf-8"))
    paleta_rgb = [c for _, c in paleta]
    print(f"paleta: {len(paleta_rgb)} colores")

    strip = Image.new("RGBA", (LADO, LADO * N_FRAMES), (0, 0, 0, 0))
    hashes = []
    for k in range(N_FRAMES):
        ruta = FRAMES_DIR / f"frame_{k}.png"
        if not ruta.exists():
            print(f"FALTA {ruta} — corre antes tools/gen_codice_vivo_v65071.py")
            return 1
        frame = reducir(Image.open(ruta).convert("RGBA"), paleta_rgb)
        strip.paste(frame, (0, k * LADO))
        hashes.append(hashlib.md5(frame.tobytes()).hexdigest())

    assert len(set(hashes)) == N_FRAMES, "frames idénticos entre sí — animación rota"
    # el parpadeo: frame 7 debe diferir de frame 0 (ojo cerrado vs abierto)
    assert hashes[0] != hashes[7], "no hay parpadeo (f0 == f7)"

    DESTINO.parent.mkdir(parents=True, exist_ok=True)
    strip.save(DESTINO)
    print(f"strip item: {DESTINO} {strip.size[0]}x{strip.size[1]} · {N_FRAMES} frames × {LADO}px · {len(set(hashes))} distintos")
    return 0


if __name__ == "__main__":
    sys.exit(main())
