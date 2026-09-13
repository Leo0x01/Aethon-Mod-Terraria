#!/usr/bin/env python3
"""
gen_ai_wings_v610.py — ALAS v6.10: arte IA → spritesheet Terraria.

El pipeline (por ala):
  1. Alfa desde fondo negro (umbral suave + cierre morfológico para no
     agujerear las plumas oscuras).
  2. SIMETRÍA perfecta: se toma la mitad izquierda y se espeja.
  3. Recorte del contenido + escalado a tamaño de sprite (~136px ancho).
  4. Composición en el marco con las RAÍCES en el centro-x y ~62% de la
     altura (el origen vanilla dibuja el marco centrado en la cintura).
  5. CONTORNO oscuro estilo Terraria (1px).
  6. ANIMACIÓN de 7 frames (¡Height()/7 es como vanilla corta las alas!)
     — v6.06 usó 4 frames: por eso las alas salían "mal ubicadas".
     Frames: 0=plegadas · 1=planeo · 2=apex alzado · 3-6=ciclo.
     Cada mitad rota alrededor de la RAÍZ (aleteo real).
  7. Tira vertical W×7H → {Name}_Wings.png + icono {Name}.png.
"""
import numpy as np
from PIL import Image, ImageFilter
from scipy import ndimage
import math, os, sys

RAW = '/home/z/my-project/research/wings_v610/raw'
OUT = '/home/z/my-project/research/wings_v610/out'
OUT_MOD = '/home/z/my-project/AethonMod/AethonMod/Content/Items/Wings'
os.makedirs(OUT, exist_ok=True)

# nombre_archivo, clase C#, ancho máx marco, alto máx por frame, poses_override, suprimir_cuerpo
WINGS = [
    ('eventhorizon', 'EventHorizonWings', 140, 78, None, False),
    ('photonring',   'PhotonRingWings',   138, 76, None, False),
    ('butterfly',    'CosmicButterflyWings', 146, 88,
        [( 26, 0.72), ( 8, 0.94), (-9, 1.05), ( 2, 1.00), ( 12, 0.92), ( 18, 0.84), ( 8, 0.94)], True),
    ('fairy',        'StardustFairyWings', 132, 82,
        [( 26, 0.72), ( 8, 0.94), (-9, 1.05), ( 2, 1.00), ( 12, 0.92), ( 18, 0.84), ( 8, 0.94)], True),
    ('solar',        'SolarCoronaWings',  140, 80, None, True),
    ('nebula',       'LivingNebulaWings', 142, 80, None, False),
    ('eclipse',      'TotalEclipseWings', 138, 76, None, True),
    ('comet',        'CrimsonCometWings', 142, 78, None, False),
]

# ángulos de aleteo por frame (grados, + = puntas abajo), squash vertical
FRAME_POSE = [
    ( 14, 0.86),   # 0 reposo: plegadas
    (  4, 0.97),   # 1 planeo
    ( -7, 1.04),   # 2 apex alzado
    (  1, 1.00),   # 3 retorno
    (  6, 0.96),   # 4 ciclo
    ( 10, 0.92),   # 5 ciclo
    (  4, 0.97),   # 6 ciclo
]

def smoothstep(x, a, b):
    t = np.clip((x - a) / (b - a), 0, 1)
    return t * t * (3 - 2 * t)

def extract_alpha(rgb):
    """Alfa desde fondo negro con cierre morfológico (sin agujeros)."""
    r, g, b = rgb[:,:,0].astype(float), rgb[:,:,1].astype(float), rgb[:,:,2].astype(float)
    lum = 0.299*r + 0.587*g + 0.114*b
    a = smoothstep(lum, 7, 42)
    # cierre: rellena interiores oscuros (plumas negras) 
    solid = a > 0.35
    closed = ndimage.binary_closing(solid, structure=np.ones((7, 7)))
    filled = ndimage.binary_fill_holes(closed)
    a = np.maximum(a, filled.astype(float))
    # suavizado del alfa
    a = ndimage.gaussian_filter(a, 1.2)
    return a

def process(name, cls, maxW, maxH, poses=None, suppress_body=False):
    img = Image.open(f'{RAW}/{name}.png').convert('RGB')
    a = np.array(img)
    alpha = extract_alpha(a)
    H, W = alpha.shape

    # ---- simetría: mitad izquierda espejada ----
    # (el arte IA suele ser casi simétrico; forzamos simetría perfecta)
    cx = W // 2
    left_a = alpha[:, :cx]
    rgb_left = a[:, :cx]
    # buscar el centro REAL del contenido en la mitad
    ys, xs = np.where(left_a > 0.4)
    if len(xs) == 0:
        print(f"  {name}: SIN contenido, saltando"); return
    # centro de masa x del contenido total → alineamos el espejo ahí
    ysF, xsF = np.where(alpha > 0.4)
    true_cx = int((xsF.min() + xsF.max()) / 2)
    if true_cx >= W - 8: true_cx = cx
    half_w = min(true_cx, W - true_cx)
    left_a = alpha[:, true_cx - half_w:true_cx]
    rgb_left = a[:, true_cx - half_w:true_cx]
    # espejo
    full_rgb = np.concatenate([rgb_left, rgb_left[:, ::-1]], axis=1)
    full_a = np.concatenate([left_a, left_a[:, ::-1]], axis=1)

    # ---- recorte del contenido ----
    ys, xs = np.where(full_a > 0.30)
    x0, x1 = max(0, xs.min() - 4), min(full_a.shape[1], xs.max() + 5)
    y0, y1 = max(0, ys.min() - 4), min(full_a.shape[0], ys.max() + 5)
    full_rgb = full_rgb[y0:y1, x0:x1]
    full_a = full_a[y0:y1, x0:x1]
    cw, ch = full_rgb.shape[1], full_rgb.shape[0]

    # ---- escalado al marco ----
    frameW = maxW
    scale = min(frameW / cw, maxH / ch, 1.0)
    newW, newH = max(8, int(cw * scale)), max(8, int(ch * scale))
    im = Image.fromarray(np.dstack([full_rgb, (full_a * 255).astype(np.uint8)]), 'RGBA')
    im = im.resize((newW, newH), Image.LANCZOS)
    # nitidez leve
    im = im.filter(ImageFilter.UnsharpMask(radius=1.2, percent=90, threshold=2))

    # ---- composición en el marco: raíces en (W/2, 62%H) ----
    FH = maxH if newH > maxH * 0.86 else int(math.ceil(newH * 1.06 / 4) * 4)
    FH = max(FH, 40)
    frame = Image.new('RGBA', (frameW, FH), (0, 0, 0, 0))
    root_y = int(FH * 0.62)
    py = root_y - newH // 2
    px = (frameW - newW) // 2
    frame.paste(im, (px, py), im)

    # ---- contorno oscuro estilo Terraria + limpieza de píxeles sueltos ----
    fa = np.array(frame).astype(float)
    al = fa[:,:,3] / 255.0
    # quitar componentes sueltos menores de 14px (artefactos del resize)
    solid = al > 0.30
    lbl, n = ndimage.label(solid)
    if n > 1:
        sizes = ndimage.sum(solid, lbl, range(1, n + 1))
        main = (np.argmax(sizes) + 1)
        keep = (lbl == main) | (sizes[lbl - 1] >= 14)
        al = al * keep
    # supresión del cuerpo central del insecto/aura (el jugador lo tapa:
    # dejamos solo un 25% para que no asome por encima/debajo del cuerpo)
    if suppress_body:
        yy, xx = np.mgrid[0:FH, 0:frameW]
        d2 = ((xx - frameW / 2) / 32.0) ** 2 + ((yy - FH * 0.60) / 26.0) ** 2
        central = np.exp(-d2)
        al = al * (1 - 0.85 * central)
    fa[:,:,3] = al * 255
    dil = ndimage.binary_dilation(al > 0.30, iterations=1)
    ring = dil & (al <= 0.30)
    # color del contorno: versión oscura del color local
    r_, g_, b_ = fa[:,:,0], fa[:,:,1], fa[:,:,2]
    orr = ndimage.grey_dilation(r_, size=(3,3)) * 0.30
    ogg = ndimage.grey_dilation(g_, size=(3,3)) * 0.30
    obb = ndimage.grey_dilation(b_, size=(3,3)) * 0.30
    fa[:,:,0] = np.where(ring, orr, r_)
    fa[:,:,1] = np.where(ring, ogg, g_)
    fa[:,:,2] = np.where(ring, obb, b_)
    fa[:,:,3] = np.where(ring, 235.0, fa[:,:,3])
    frame = Image.fromarray(fa.astype(np.uint8), 'RGBA')

    # ---- generar los 7 frames (rotación de cada mitad sobre la raíz) ----
    frames = []
    rootY = int(FH * 0.62)
    pose_list = poses if poses else FRAME_POSE
    for fi, (ang, sq) in enumerate(pose_list):
        f = Image.new('RGBA', (frameW, FH), (0, 0, 0, 0))
        # squash vertical del ala completa (aleteo)
        body = frame.resize((frameW, max(4, int(FH * sq))), Image.LANCZOS)
        # re-anclar: mantener la raíz en su sitio
        sqH = body.height
        offY = int(rootY * (1 - sq))
        # mitad izquierda rota +ang alrededor de su borde DERECHO (la costura),
        # mitad derecha rota -ang alrededor de su borde IZQUIERDO (la costura)
        halfL = body.crop((0, 0, frameW // 2, sqH))
        rotL = halfL.rotate(ang, resample=Image.BICUBIC,
                            center=(halfL.width, rootY), expand=False)
        f.paste(rotL, (0, offY), rotL)
        halfR = body.crop((frameW // 2, 0, frameW, sqH))
        rotR = halfR.rotate(-ang, resample=Image.BICUBIC,
                            center=(0, rootY), expand=False)
        f.paste(rotR, (frameW // 2, offY), rotR)
        frames.append(f)

    # ---- tira 7 frames ----
    sheet = Image.new('RGBA', (frameW, FH * 7), (0, 0, 0, 0))
    for i, f in enumerate(frames):
        sheet.paste(f, (0, i * FH))
    sheet.save(f'{OUT}/{cls}_Wings.png')

    # ---- icono del ítem (frame 2, el más extendido) ----
    icon = frames[2].crop((frameW // 2 - 34, 0, frameW // 2 + 34, FH))
    icon = icon.resize((28, int(FH * 28 / 68)), Image.LANCZOS)
    icon.save(f'{OUT}/{cls}.png')

    print(f"  {name} → {cls}: marco {frameW}x{FH}, tira {frameW}x{FH*7}")

if __name__ == '__main__':
    print("Procesando las 8 alas IA → formato Terraria:")
    for name, cls, mw, mh, poses, supp in WINGS:
        process(name, cls, mw, mh, poses, supp)
    print("LISTO")
