#!/usr/bin/env python3
"""
gen_oblivion_blob.py — v6.11 — LA TEXTURA BLOB GAUSSIANA DEL VÓRTICE.

BUG v6.10: el renderer C# dibujaba SoftGlow con (len, wid) como TAMAÑO
TOTAL del quad, pero esos números eran las SIGMAS gaussianas del
prototipo Python (add_blob: core=exp(-3.6·d²), halo=0.4·exp(-1.2·d²),
visible hasta ~1.5×sigma). SoftGlow además concentra su brillo en un
núcleo diminuto (alpha 134 a r=6/32) → cada cápsula brillaba en ~2-3px
→ EL VÓRTICE ENTERO ERA MICROSCÓPICO (14 píxeles magenta en la captura
del usuario).

FIX: textura NUEVA con el perfil EXACTO del prototipo horneado en el RGB
(alpha=255 en toda la textura para que el premultiply de tML no la toque
y el blending aditivo (SourceAlpha, One) quede LINEAL en el perfil):

    g(d) = (exp(-3.6·d²) + 0.4·exp(-1.2·d²)) · (1-d²)² / 1.4

con d = radio normalizado (1 = borde). El renderer dibuja el quad con
TAMAÑO TOTAL = (2·sigma, 2·sigma) → d = distancia/sigma → el perfil
reproduce add_blob píxel a píxel.
"""
import numpy as np
from PIL import Image

SIZE = 128

yy, xx = np.mgrid[0:SIZE, 0:SIZE]
cx = cy = (SIZE - 1) / 2.0
d = np.sqrt(((xx - cx) / (SIZE / 2.0)) ** 2 + ((yy - cy) / (SIZE / 2.0)) ** 2)
d = np.clip(d, 0.0, 1.0)

core = np.exp(-3.6 * d * d)
halo = 0.40 * np.exp(-1.2 * d * d)
window = (1.0 - d * d) ** 2          # llega a 0 en el borde (sin costuras)
g = (core + halo) * window / 1.4     # pico normalizado a 1.0

rgb = (np.clip(g, 0, 1) * 255).astype(np.uint8)
img = np.zeros((SIZE, SIZE, 4), dtype=np.uint8)
img[..., 0] = rgb
img[..., 1] = rgb
img[..., 2] = rgb
img[..., 3] = 255                    # alpha OPACO: el decaimiento vive en RGB

out = '/home/z/my-project/AethonMod/AethonMod/Content/Effects/Procedural/OblivionBlob.png'
Image.fromarray(img, 'RGBA').save(out)
print(f'guardado {out} ({SIZE}x{SIZE}, pico={img[SIZE//2, SIZE//2, 0]}, borde={img[0, SIZE//2, 0]}')
