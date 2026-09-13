#!/usr/bin/env python3
"""
gen_ai_wings_v611.py — ALAS v6.11: arte IA → spritesheet Terraria CORRECTO.

EL BUG v6.10 (decompilado DrawPlayer_09_Wings de tModLoader 2026.07.3.0):
las alas moddeadas caen en el camino POR DEFECTO, que corta la tira con
num13 = 4 → **Height()/4**, NO Height()/7 (ese era el caso especial de las
alas 22/43/44). El origen es (Width/2, Height/8) = CENTRO del frame, y el
anclaje del jugador es el torso medio + (-9, +2). Las tiras de 7 frames
cortadas en cuartos mostraban TRES BANDAS de alas con huecos (exactamente
la captura del usuario).

La animación vanilla para alas normales (decompilada, Player.cs):
  · reposo/suelo:        frame 0
  · volando (salto):     ciclo 0 → 1 → 2 → 0 (cada 5 ticks)
  · planeo/caída:        frame 2 fijo
  · frame 3:             NUNCA se usa en alas normales (copia del 1)

EL PIPELINE v6.11 (por ala):
  1. Alfa desde fondo negro (umbral suave + cierre morfológico).
  2. SIMETRÍA perfecta (mitad izquierda espejada sobre el centro de masa).
  3. Recorte + escalado al marco con MARGEN DE ALETEO (sin clipping).
  4. Composición con la RAÍZ en el CENTRO del frame (W/2, FH/2) — el
     origen exacto con el que vanilla dibuja.
  5. CONTORNO oscuro estilo Terraria + limpieza + alpha dura (fondo 100%
     transparente: cualquier alpha < 0.03 → 0).
  6. 4 POSES: 0=plegada · 1=media · 2=apertura total (la firma, se ve al
     planear) · 3=copia de la 1. Cada mitad rota sobre la costura en el
     centro; el squash se aplica al CONTENIDO (no al marco) → la raíz
     jamás se desplaza y nada se recorta.
  7. Tira vertical W×4·FH → {Name}_Wings.png + icono de par completo.
"""
import numpy as np
from PIL import Image, ImageFilter
from scipy import ndimage
import math, os

RAW = '/home/z/my-project/research/wings_v610/raw'
OUT = '/home/z/my-project/research/wings_v611'
OUT_MOD = '/home/z/my-project/AethonMod/AethonMod/Content/Items/Wings'
os.makedirs(OUT, exist_ok=True)

# 4 poses: (ángulo°, squash) — + = puntas abajo
POSE_NORMAL = [
    (12, 0.88),   # 0 reposo: plegadas
    (2, 0.97),    # 1 media
    (-9, 1.06),   # 2 apertura total (firma / planeo)
    (2, 0.97),    # 3 copia de la 1 (vanilla no la usa)
]
POSE_BUTTERFLY = [
    (20, 0.76),   # 0 plegadas fuerte
    (6, 0.95),    # 1 media
    (-10, 1.08),  # 2 apertura total
    (6, 0.95),    # 3 copia
]

# nombre, clase, ancho marco, alto MÁX de frame, poses, suprimir cuerpo
WINGS = [
    ('eventhorizon', 'EventHorizonWings',    140, 104, POSE_NORMAL,     False),
    ('photonring',   'PhotonRingWings',      138, 104, POSE_NORMAL,     False),
    ('butterfly',    'CosmicButterflyWings', 146, 128, POSE_BUTTERFLY,  True),
    ('fairy',        'StardustFairyWings',   132, 120, POSE_BUTTERFLY,  True),
    ('solar',        'SolarCoronaWings',     140, 104, POSE_NORMAL,     True),
    ('nebula',       'LivingNebulaWings',    142, 104, POSE_NORMAL,     False),
    ('eclipse',      'TotalEclipseWings',    138, 104, POSE_NORMAL,     True),
    ('comet',        'CrimsonCometWings',    142, 104, POSE_NORMAL,     False),
]


def smoothstep(x, a, b):
    t = np.clip((x - a) / (b - a), 0, 1)
    return t * t * (3 - 2 * t)


def extract_alpha(rgb):
    """Alfa con REMOCIÓN DE FONDO POR CONECTIVIDAD (v6.11).

    El v6.10 usaba solo un umbral de LUMINANCIA (7→42): los artes IA con
    fondo GRIS oscuro (photonring (25,24,29), fairy, eclipse, comet)
    quedaban por encima del umbral → una CAJA RECTANGULAR semitransparente
    cubría todo el frame (el "fondo no transparente" del usuario).

    Método nuevo:
      1. Color de fondo estimado por la mediana del borde.
      2. d = distancia de color al fondo.
      3. REGIÓN DE FONDO = flood-fill desde los 4 bordes a través de los
         píxeles con d < 58 (mata viñetas y gradientes graduales aunque
         deriven del color medido).
      4. Fuera de esa región: alfa = smoothstep(d, 22, 58).
      5. Cierre morfológico + relleno de huecos (plumas oscuras intactas).
    """
    r, g, b = rgb[:, :, 0].astype(float), rgb[:, :, 1].astype(float), rgb[:, :, 2].astype(float)
    H, W = r.shape
    border = np.concatenate([
        rgb[:12].reshape(-1, 3), rgb[-12:].reshape(-1, 3),
        rgb[:, :12].reshape(-1, 3), rgb[:, -12:].reshape(-1, 3)])
    bg = np.median(border, axis=0)
    d = np.sqrt(((rgb - bg) ** 2).sum(axis=2))

    near = d < 58.0
    lbl, n = ndimage.label(near)
    border_labels = set(np.unique(np.concatenate([
        lbl[0, :], lbl[-1, :], lbl[:, 0], lbl[:, -1]])))
    border_labels.discard(0)
    bg_region = np.isin(lbl, list(border_labels)) if border_labels else np.zeros_like(near)

    a = smoothstep(d, 22.0, 58.0)
    a[bg_region] = 0.0

    # cierre: rellena interiores oscuros (plumas negras) que NO son fondo
    solid = a > 0.35
    closed = ndimage.binary_closing(solid, structure=np.ones((7, 7)))
    filled = ndimage.binary_fill_holes(closed)
    a = np.maximum(a, filled.astype(float) * 0.999)
    a = ndimage.gaussian_filter(a, 1.2)
    return a


def process(name, cls, frameW, FH, poses, suppress_body):
    img = Image.open(f'{RAW}/{name}.png').convert('RGB')
    a = np.array(img)
    alpha = extract_alpha(a)

    # ---- simetría: mitad izquierda espejada ----
    H, W = alpha.shape
    ysF, xsF = np.where(alpha > 0.4)
    if len(xsF) == 0:
        print(f"  {name}: SIN contenido, saltando"); return
    true_cx = int((xsF.min() + xsF.max()) / 2)
    if true_cx >= W - 8:
        true_cx = W // 2
    half_w = min(true_cx, W - true_cx)
    left_a = alpha[:, true_cx - half_w:true_cx]
    rgb_left = a[:, true_cx - half_w:true_cx]
    full_rgb = np.concatenate([rgb_left, rgb_left[:, ::-1]], axis=1)
    full_a = np.concatenate([left_a, left_a[:, ::-1]], axis=1)

    # ---- recorte del contenido ----
    ys, xs = np.where(full_a > 0.30)
    x0, x1 = max(0, xs.min() - 4), min(full_a.shape[1], xs.max() + 5)
    y0, y1 = max(0, ys.min() - 4), min(full_a.shape[0], ys.max() + 5)
    full_rgb = full_rgb[y0:y1, x0:x1]
    full_a = full_a[y0:y1, x0:x1]
    cw, ch = full_rgb.shape[1], full_rgb.shape[0]

    # ---- escalado con MARGEN DE ALETEO EXACTO (por píxel) ----
    # El giro alrededor de la costura lleva cada píxel a
    # reach = |dy|·sq·cos(ang) + |dx|·sin(ang) del centro (la raíz).
    # El alcance máximo sobre las poses reales decide la escala: el arte
    # puede ser GRANDE (las alas se estrechan hacia las puntas — el
    # alcance real es mucho menor que el del bounding box).
    def max_reach(a_mask, halfW, halfH):
        ys, xs = np.where(a_mask > 0.3)
        if len(xs) == 0:
            return 0.0
        dx = np.abs(xs - (a_mask.shape[1] / 2))
        dy = np.abs(ys - (a_mask.shape[0] / 2))
        r = 0.0
        for ang, sq in poses:
            ca, sa = math.cos(math.radians(ang)), abs(math.sin(math.radians(ang)))
            r = max(r, float((dy * sq * ca + dx * sa).max()))
        return r

    scale = min(frameW / cw, FH * 0.98 / ch, 1.0)
    for _ in range(4):
        newW, newH = max(8, int(cw * scale)), max(8, int(ch * scale))
        im = Image.fromarray(np.dstack([full_rgb, (full_a * 255).astype(np.uint8)]))
        im = im.resize((newW, newH), Image.LANCZOS)
        a_small = np.array(im)[:, :, 3] / 255.0
        reach = max_reach(a_small, newW / 2, newH / 2) * 1.03 + 2
        if 2 * reach <= FH or scale < 0.05:
            break
        scale *= FH / (2 * reach)   # el alcance escala linealmente con la escala
    im = im.filter(ImageFilter.UnsharpMask(radius=1.2, percent=90, threshold=2))

    # ---- composición: contenido centrado en (W/2, FH/2) = LA RAÍZ ----
    frame = Image.new('RGBA', (frameW, FH), (0, 0, 0, 0))
    frame.paste(im, ((frameW - newW) // 2, FH // 2 - newH // 2), im)

    # ---- contorno oscuro + limpieza + alpha dura ----
    fa = np.array(frame).astype(float)
    al = fa[:, :, 3] / 255.0
    solid = al > 0.30
    lbl, n = ndimage.label(solid)
    if n > 1:
        sizes = ndimage.sum(solid, lbl, range(1, n + 1))
        main = (np.argmax(sizes) + 1)
        keep = (lbl == main) | (sizes[lbl - 1] >= 14)
        al = al * keep
    if suppress_body:
        yy, xx = np.mgrid[0:FH, 0:frameW]
        d2 = ((xx - frameW / 2) / 32.0) ** 2 + ((yy - FH / 2) / 26.0) ** 2
        central = np.exp(-d2)
        al = al * (1 - 0.85 * central)
    # ALPHA DURA: el fondo queda 100% transparente (petición del usuario)
    al = np.where(al < 0.03, 0.0, al)
    fa[:, :, 3] = al * 255
    dil = ndimage.binary_dilation(al > 0.30, iterations=1)
    ring = dil & (al <= 0.30)
    r_, g_, b_ = fa[:, :, 0], fa[:, :, 1], fa[:, :, 2]
    orr = ndimage.grey_dilation(r_, size=(3, 3)) * 0.30
    ogg = ndimage.grey_dilation(g_, size=(3, 3)) * 0.30
    obb = ndimage.grey_dilation(b_, size=(3, 3)) * 0.30
    fa[:, :, 0] = np.where(ring, orr, r_)
    fa[:, :, 1] = np.where(ring, ogg, g_)
    fa[:, :, 2] = np.where(ring, obb, b_)
    fa[:, :, 3] = np.where(ring, 235.0, fa[:, :, 3])
    base = Image.fromarray(fa.astype(np.uint8))

    # ---- las 4 poses: squash al CONTENIDO y rotación sobre la raíz ----
    # Método SIN recortes de canvas: se rota el frame COMPLETO alrededor
    # del pivote (costura, raíz) +ang y -ang, y se compone cada mitad con
    # una máscara dura en la costura. Cerca de la raíz el desplazamiento
    # es ~0 (se gira alrededor de ella) → la unión es perfecta; hacia las
    # puntas las mitades divergen = el aleteo.
    rootY = FH // 2
    seamX = frameW // 2
    # máscara izquierda (x ≤ seam) y derecha (x ≥ seam)
    maskL = np.zeros((FH, frameW), dtype=np.uint8)
    maskL[:, :seamX + 1] = 255
    maskR = np.zeros((FH, frameW), dtype=np.uint8)
    maskR[:, seamX:] = 255
    frames = []
    for ang, sq in poses:
        # squash vertical del contenido (raíz anclada al centro)
        body = base.resize((frameW, max(4, int(round(FH * sq)))), Image.LANCZOS)
        sqH = body.height
        # tras el squash la raíz vive en rootY·sq (espacio del cuerpo): el
        # PIVOTE de rotación debe ser ESE punto para que las puntas giren
        # alrededor de la raíz de verdad (el v6.10 rotaba alrededor de
        # rootY sin squash — desplazaba el pivote rootY·(1-sq) px).
        pivotY = int(round(rootY * sq))
        offY = rootY - pivotY   # re-anclar la raíz al centro del frame
        rotL = body.rotate(ang, resample=Image.BICUBIC,
                           center=(seamX, pivotY), expand=False)
        rotR = body.rotate(-ang, resample=Image.BICUBIC,
                           center=(seamX, pivotY), expand=False)
        arrL = np.array(rotL).astype(np.float64)
        arrR = np.array(rotR).astype(np.float64)
        # región válida tras el desplazamiento vertical offY
        ys0 = max(0, offY); ys1 = min(FH, offY + sqH)
        f = np.zeros((FH, frameW, 4), dtype=np.uint8)
        if ys1 > ys0:
            srcL = arrL[ys0 - offY:ys1 - offY]
            srcR = arrR[ys0 - offY:ys1 - offY]
            mL = maskL[ys0:ys1]; mR = maskR[ys0:ys1]
            aL = (srcL[:, :, 3] / 255.0) * (mL / 255.0)
            aR = (srcR[:, :, 3] / 255.0) * (mR / 255.0)
            a_sum = aL + aR
            alpha = np.minimum(a_sum, 1.0)
            safe = np.maximum(a_sum, 1e-6)
            # color de alfa RECTO: media ponderada de las mitades (en la
            # costura se funden — sin hueco; fuera es el color original)
            rgb = (srcL[:, :, :3] * aL[:, :, None] +
                   srcR[:, :, :3] * aR[:, :, None]) / safe[:, :, None]
            f[ys0:ys1, :, :3] = np.round(rgb).astype(np.uint8)
            f[ys0:ys1, :, 3] = np.round(alpha * 255).astype(np.uint8)
        frames.append(Image.fromarray(f))
    # fin de las poses

    # ---- tira de 4 frames ----
    sheet = Image.new('RGBA', (frameW, FH * 4), (0, 0, 0, 0))
    for i, f in enumerate(frames):
        sheet.paste(f, (0, i * FH))
    sheet.save(f'{OUT}/{cls}_Wings.png')
    sheet.save(f'{OUT_MOD}/{cls}_Wings.png')

    # ---- icono del ítem: el PAR COMPLETO del frame 2 ----
    icon = frames[2]
    iw, ih = icon.size
    k = min(30 / iw, 26 / ih)
    icon = icon.resize((max(1, int(iw * k)), max(1, int(ih * k))), Image.LANCZOS)
    icon.save(f'{OUT}/{cls}.png')
    icon.save(f'{OUT_MOD}/{cls}.png')

    # ---- verificación de transparencia y clipping ----
    arr = np.array(sheet)
    corners = [arr[0, 0, 3], arr[0, -1, 3], arr[-1, 0, 3], arr[-1, -1, 3]]
    # el contenido no debe tocar los bordes verticales de cada frame
    touch = []
    for i in range(4):
        fa_ = arr[i * FH:(i + 1) * FH, :, 3]
        if fa_[0, :].max() > 30 or fa_[-1, :].max() > 30:
            touch.append(i)
    print(f"  {name} → {cls}: marco {frameW}x{FH}, tira {frameW}x{FH*4}, "
          f"esquinas={[int(c) for c in corners]}, bordes_tocados={touch or 'NINGUNO'}")


if __name__ == '__main__':
    print("Procesando las 8 alas IA → formato Terraria v6.11 (4 frames):")
    for name, cls, mw, fh, poses, supp in WINGS:
        process(name, cls, mw, fh, poses, supp)
    print("LISTO")
