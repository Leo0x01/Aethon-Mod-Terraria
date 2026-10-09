#!/usr/bin/env python3
"""Visualización del arte 706×967 + máscaras de estrellas/venas para QA."""
import numpy as np
from PIL import Image, ImageDraw

base = np.load('/tmp/v65086_base.npy')
con_iris = np.load('/tmp/v65086_coniris.npy')
estrellas = np.load('/tmp/v65086_estrellas.npy')
venas = np.load('/tmp/v65086_venas.npy')

# el arte con las máscaras coloreadas encima
art = con_iris.copy()
vis = art.copy()
vis[venas] = (255, 60, 60, 255)      # venas en rojo
vis[estrellas] = (60, 255, 60, 255)  # estrellas en verde
comp = Image.new('RGBA', (art.shape[1] * 2 + 24, art.shape[0] + 8), (18, 16, 26, 255))
comp.paste(Image.fromarray(art, 'RGBA'), (8, 4))
comp.paste(Image.fromarray(vis, 'RGBA'), (art.shape[1] + 16, 4))
comp = comp.resize((comp.width // 2, comp.height // 2), Image.LANCZOS)
comp.save('/tmp/v65086_arte_mascaras.png')
print('arte+marcas → /tmp/v65086_arte_mascaras.png', comp.size)

# zoom de las esquinas con estrellas (la tapa)
for nombre, caja in {
    'sup_der': (540, 0, 706, 160),
    'inf_der': (600, 690, 706, 830),
    'ojo': (280, 350, 470, 540),
}.items():
    x0, y0, x1, y1 = caja
    rec = vis[y0:y1, x0:x1]
    im = Image.fromarray(rec, 'RGBA').resize(((x1 - x0) * 2, (y1 - y0) * 2), Image.LANCZOS)
    im.save(f'/tmp/v65086_zoom_{nombre}.png')
    print(f'zoom {nombre} → /tmp/v65086_zoom_{nombre}.png')
