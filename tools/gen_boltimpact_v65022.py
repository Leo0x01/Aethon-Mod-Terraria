#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_boltimpact_v65022.py — v6.50.22 — EL CUADRADO DEL IMPACTO.

El reporte del usuario: «BoltImpact.png eso se ve el cuadrado cuando
aparece, debería tener un degradado suave y difuminarse al final, hacia
los bordes quedar transparente».

LA CAUSA (medida por píxeles sobre el PNG v6.50.15): el perfil del
destello NO llegaba a 0 en el borde del lienzo 96×96 — el perímetro
entero quedaba con alfa ~29 (11%) y el falda exterior ~50-80: al
dibujarlo aditivo ×2 escalas girando, el BORDE del quad se leía como
un CUADRADO sólido. (El mismo defecto de clase que NovaBurst enterró
en v6.50.17: perfiles recortados por el lienzo.)

LA CURA (la receta NovaBurst de la casa, aplicada al impacto):
  perfil(r,θ) = f(r) · (0.74 + 0.26·star(θ))
  f(r)      = 0.40·exp(−(r/0.14)²)  [núcleo]  + 0.60·exp(−(r/0.36)^1.7) [falda]
  star(θ)   = |cos(2(θ−45°))|^4      [4 rayos sutiles en las diagonales —
                                       complementan la CRUZ de luz axial
                                       que ImpactFlash dibuja aparte con
                                       SoftGlow]
  r         = radio normalizado al SEMILADO (0 = centro, 1.0 = borde).

CONTRATO: perfil(1.0) == 0 EXACTO (borde y esquinas muertos a 0),
monótono decreciente en r (verificado por píxeles al final del script),
RGB = 255 plano y alfa = 255·√perfil — la convención premultiplicada
de la casa (aporte = perfil·color·f LINEAL en el lote aditivo).

Salida: AethonMod/Content/Effects/Procedural/BoltImpact.png (96×96).
"""
from PIL import Image
import numpy as np

S = 96
HALF = S / 2.0

yy, xx = np.mgrid[0:S, 0:S].astype(np.float64)
cy = yy - (HALF - 0.5)
cx = xx - (HALF - 0.5)
r = np.sqrt(cx * cx + cy * cy) / HALF          # 0 centro .. 1.0 borde
theta = np.arctan2(cy, cx)

# LA FALDA + EL NÚCLEO (la receta NovaBurst: núcleo estrecho + falda
# ancha de caída ^1.7 — el degradado SUAVE que el usuario pide).
nucleo = 0.40 * np.exp(-((r / 0.14) ** 2))
falda  = 0.60 * np.exp(-((r / 0.36) ** 1.7))
f = nucleo + falda

# LOS 4 RAYOS sutiles en las diagonales (la Cruz axial la dibuja
# ImpactFlash con SoftGlow aparte — estos rayos giran con el destello).
star = np.abs(np.cos(2.0 * (theta - np.pi / 4.0))) ** 4
perfil = f * (0.74 + 0.26 * star)

# LA MUERTE EXACTA EN EL BORDE (el contrato): los dos anillos exteriores
# a 0 ABSOLUTO y rampa de muerte en el 6% final — ni un píxel del
# perímetro puede brillar (el alfa ~29 del PNG viejo era EL cuadrado).
perfil *= np.clip((0.94 - r) / 0.09, 0.0, 1.0)   # rampa 0.94→0.85, muerto desde 0.94
perfil[r >= 0.90] = 0.0
perfil = np.clip(perfil, 0.0, 1.0)

# CONVENCIONES DE LA CASA: RGB=255 plano, alfa=√perfil (premult).
alpha = np.clip(np.sqrt(perfil) * 255.0 + 0.5, 0, 255).astype(np.uint8)
rgb = np.full((S, S, 3), 255, dtype=np.uint8)
out = np.dstack([rgb, alpha])

img = Image.fromarray(out, "RGBA")
img.save("AethonMod/Content/Effects/Procedural/BoltImpact.png")

# ============ VERIFICACIÓN POR PÍXELES (el control sensible) ============
chk = np.array(Image.open(
    "AethonMod/Content/Effects/Procedural/BoltImpact.png").convert("RGBA"))
a = chk[..., 3].astype(np.float64)

def m(v): return f"{v:7.2f}"

print("== BoltImpact.png 96×96 — VERIFICACIÓN DEL CONTRATO ==")
print("  alfa borde (arr/aba/izq/der):",
      m(a[0, :].mean()), m(a[-1, :].mean()), m(a[:, 0].mean()), m(a[:, -1].mean()))
print("  alfa esquinas:", [int(a[y, x]) for y, x in
      [(0, 0), (0, 95), (95, 0), (95, 95)]])
print("  alfa centro:", m(a[47:49, 47:49].mean()), " max:", int(a.max()))
assert a[0, :].max() == 0 and a[-1, :].max() == 0, "borde horizontal vivo"
assert a[:, 0].max() == 0 and a[:, -1].max() == 0, "borde vertical vivo"

# MONOTONÍA RADIAL (el degradado suave): la MEDIA ANGULAR a cada radio
# nunca sube al alejarse del centro (la anisotropía de los rayos es por
# diseño — lo que NO puede subir es el brillo medio hacia el borde).
rr = r.flatten(); aa = a.flatten()
bins = np.clip((rr * 20).astype(int), 0, 19)
means = np.array([aa[bins == b].mean() if (bins == b).any() else 0.0
                  for b in range(20)])
viol = sum(1 for i in range(1, 20) if means[i] > means[i - 1] + 1.5)
print("  perfil medio por bin de radio:", [f"{v:.0f}" for v in means])
print("  violaciones de monotonia radial (media angular):", viol)
assert viol == 0, "el brillo medio SUBE hacia fuera — perfil roto"
print("OK — el contrato se cumple: borde MUERTO a 0, degradado monotono.")
