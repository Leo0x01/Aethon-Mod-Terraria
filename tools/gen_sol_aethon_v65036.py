#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
v6.50.36 — EL SOL DE AETHON (los iconos del jefe-luz).
Aethon, la Luz Primordial, dejó de ser una sierpe: ES UNA LUZ BRILLANTE.
Este generador pinta los DOS iconos del jefe (100% código, el patrón de
la casa — nada de arte a mano):

  · AethonBoss_Head_Boss.png (32×32) — el icono de la BARRA DE JEFE:
    el disco dorado + el núcleo blanco + los rayos cortos.
  · AethonBoss.png (48×48) — el retrato del bestiario: el sol completo
    con la corona de rayos largos y las dos perlas orbitantes.

La paleta canónica de la luz: oro #FFF0BE / #FFE28C / #F5C451, el
núcleo #FFFFF4 y el velo violeta #C496FF.
"""
from PIL import Image, ImageDraw, ImageFilter
import math, os

ORO_CLARO = (255, 246, 210, 255)
ORO = (255, 226, 140, 255)
ORO_HONDO = (245, 196, 81, 255)
NUCLEO = (255, 255, 244, 255)
VIOLETA = (196, 150, 255, 90)

BASE = os.path.join(os.path.dirname(__file__), "..", "AethonMod", "Content", "NPCs")


def sol(tam, n_rayos, largo_rayo, con_perlas):
    """El sol de Aethon a resolución nativa + supersampling ×4."""
    S = 4
    W = tam * S
    img = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    cx = cy = W / 2.0
    r_disco = W * 0.26

    # 1. EL VELO violeta (la profundidad de la luz).
    d.ellipse([cx - r_disco * 1.65, cy - r_disco * 1.65,
               cx + r_disco * 1.65, cy + r_disco * 1.65], fill=VIOLETA)

    # 2. LOS RAYOS (triángulos radiales — la corona del sol).
    for i in range(n_rayos):
        ang = i * 2 * math.pi / n_rayos + 0.22
        largo = largo_rayo * (0.82 + 0.18 * math.sin(i * 2.3))
        ancho = math.pi / n_rayos * 0.62
        p1 = (cx + math.cos(ang - ancho) * r_disco * 1.04,
              cy + math.sin(ang - ancho) * r_disco * 1.04)
        p2 = (cx + math.cos(ang + ancho) * r_disco * 1.04,
              cy + math.sin(ang + ancho) * r_disco * 1.04)
        p3 = (cx + math.cos(ang) * (r_disco + largo),
              cy + math.sin(ang) * (r_disco + largo))
        d.polygon([p1, p3, p2], fill=ORO_HONDO)

    # 3. EL HALO dorado (el anillo exterior del disco).
    d.ellipse([cx - r_disco * 1.22, cy - r_disco * 1.22,
               cx + r_disco * 1.22, cy + r_disco * 1.22], fill=ORO)
    # 4. EL DISCO (el cuerpo de la luz).
    d.ellipse([cx - r_disco, cy - r_disco, cx + r_disco, cy + r_disco], fill=ORO_CLARO)
    # 5. EL NÚCLEO blanco (el corazón de Aethon).
    d.ellipse([cx - r_disco * 0.52, cy - r_disco * 0.52,
               cx + r_disco * 0.52, cy + r_disco * 0.52], fill=NUCLEO)

    # 6. LAS PERLAS orbitantes (las coronas de Saturno en miniatura).
    if con_perlas:
        for i, ang in enumerate((0.6, 3.5)):
            rr = r_disco * 1.42
            px = cx + math.cos(ang) * rr
            py = cy + math.sin(ang) * rr * 0.55
            pr = W * 0.035
            d.ellipse([px - pr, py - pr, px + pr, py + pr],
                      fill=ORO if i else VIOLETA[:3] + (200,))

    # El glow final: un difuminado suave y re-multiplicado (la luz IRRADIA).
    glow = img.filter(ImageFilter.GaussianBlur(W * 0.020))
    out = Image.alpha_composite(glow, img)
    return out.resize((tam, tam), Image.LANCZOS)


if __name__ == "__main__":
    # LA BARRA (32×32): rayos cortos, sin perlas — legible a 24 px.
    sol(32, 10, 4 * 4, False).save(os.path.join(BASE, "AethonBoss_Head_Boss.png"))
    # EL BESTIARIO (48×48): la corona completa con las perlas.
    sol(48, 12, 7 * 4, True).save(os.path.join(BASE, "AethonBoss.png"))
    print("AethonBoss_Head_Boss.png (32) + AethonBoss.png (48) — EL SOL generado")
