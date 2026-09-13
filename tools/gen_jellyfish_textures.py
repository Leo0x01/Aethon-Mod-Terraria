#!/usr/bin/env python3
"""v5.97 — Generación de texturas para LA MEDUSA NEBULAR (minion invocador).

Todas se generan a 4x y se reducen con LANCZOS para anti-aliasing de calidad.
Paleta: campana translúcida verde-azulada (teal/esmeralda) con margen
bioluminiscente violeta-rosa; galaxia interior blanco-dorado; cuentas de
tentáculo tintables (blancas); iconos de ítem y buff.

Texturas generadas:
  - Content/Effects/Procedural/JellyfishBell.png    (256)  campana
  - Content/Effects/Procedural/JellyfishGalaxy.png  (128)  galaxia interior
  - Content/Effects/Procedural/JellyfishBead.png    (64)   cuenta de tentáculo
  - Content/Weapons/Cosmic/MedusaNebularStaff.png   (28x30) icono del arma
  - Content/Buffs/NebulaJellyfishBuff.png           (32x32) icono del buff
"""
import math
import random
from PIL import Image, ImageDraw, ImageFilter

random.seed(20260717)  # semilla fija: determinista entre regeneraciones

ROOT = "/home/z/my-project/AethonMod/AethonMod"


def lerp(a, b, t):
    return a + (b - a) * t


def lerp3(c1, c2, t):
    return tuple(int(round(lerp(c1[i], c2[i], t))) for i in range(3))


def smoothstep(e0, e1, x):
    t = max(0.0, min(1.0, (x - e0) / (e1 - e0)))
    return t * t * (3 - 2 * t)


def clamp(x, lo, hi):
    return max(lo, min(hi, x))


# ================================================================
#  1. JELLYFISH BELL — la campana translúcida
# ================================================================
def gen_bell():
    S = 256
    SS = 4
    W = S * SS
    cx = cy = W / 2
    R = W * 0.485
    img = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    px = img.load()

    # Paleta radial de la campana:
    c_core = (168, 255, 232)   # aqua pálido translúcido (el corazón)
    c_mid = (52, 214, 170)     # esmeralda-teal
    c_margin = (150, 84, 216)  # violeta profundo
    c_rim = (255, 156, 216)    # rosa bioluminiscente del borde

    for y in range(W):
        for x in range(W):
            dx, dy = x - cx, y - cy
            r = math.hypot(dx, dy) / R
            if r > 1.02:
                continue
            # Color: aqua en el centro → esmeralda → violeta hacia el margen
            if r < 0.55:
                t = smoothstep(0.0, 0.55, r)
                c = lerp3(c_core, c_mid, t)
            else:
                t = smoothstep(0.55, 0.92, r)
                c = lerp3(c_mid, c_margin, t)

            # Alpha: centro translúcido (la luz lo atraviesa) → más opaco
            # hacia el margen (la materia de la campana se engrosa) → borde
            # bioluminiscente → corte suave.
            a = lerp(120, 205, smoothstep(0.1, 0.85, r))
            # El anillo del margen SE ENCIENDE (bioluminiscencia).
            glow = smoothstep(0.78, 0.94, r) * (1.0 - smoothstep(0.96, 1.0, r))
            a = clamp(a + glow * 70, 0, 255)
            if glow > 0.05:
                c = lerp3(c, c_rim, glow * 0.85)
            # corte suave del borde exterior
            if r > 0.94:
                a *= (1.0 - smoothstep(0.94, 1.0, r))
            px[x, y] = (*c, int(a))

    draw = ImageDraw.Draw(img, "RGBA")

    # --- COSTILLAS RADIALES (canales de la campana): 16 costillas con ---
    # --- curvatura orgánica (espiral suave), más brillantes hacia fuera ---
    ribs = 16
    for i in range(ribs):
        base_a = (math.tau / ribs) * i
        wobble = math.sin(i * 2.7) * 0.06
        pts = []
        for s in range(41):
            t = s / 40.0
            rr = 0.18 + t * 0.80          # desde el ápice hasta el margen
            # espiral suave: el canal se retuerce un poco al alejarse
            ang = base_a + wobble * t + 0.14 * (t * t)
            x = cx + math.cos(ang) * rr * R
            y = cy + math.sin(ang) * rr * R
            pts.append((x, y))
        for s in range(len(pts) - 1):
            t = s / (len(pts) - 2)
            # ancho creciente hacia el margen (2px→7px en espacio SS)
            wd = lerp(2.5, 8.0, t) * SS / 4
            # brillo creciente hacia el margen
            br = int(lerp(30, 105, t))
            col = (190, 255, 236, br)
            draw.line([pts[s], pts[s + 1]], fill=col, width=int(wd))
    # Suavizar las costillas
    img = img.filter(ImageFilter.GaussianBlur(2.2 * SS / 4))
    draw = ImageDraw.Draw(img, "RGBA")

    # --- ANILLOS DE CRECIMIENTO (arcos concéntricos tenues) ---
    for k, rr in enumerate((0.42, 0.58, 0.72)):
        ring_a = 16 + k * 5
        bbox = [cx - rr * R, cy - rr * R, cx + rr * R, cy + rr * R]
        draw.ellipse(bbox, outline=(120, 240, 214, ring_a), width=int(1.5 * SS / 4))

    # --- CLÚSTER ESTELAR INTERIOR: semillitas dentro de la campana ---
    for _ in range(26):
        ang = random.uniform(0, math.tau)
        rr = random.uniform(0.05, 0.62)
        x = cx + math.cos(ang) * rr * R
        y = cy + math.sin(ang) * rr * R
        b = random.randint(150, 255)
        sz = random.uniform(1.2, 3.0) * SS / 4
        col = (255, 250, 235, b) if random.random() < 0.6 else (200, 240, 255, b)
        draw.ellipse([x - sz, y - sz, x + sz, y + sz], fill=col)

    img = img.filter(ImageFilter.GaussianBlur(1.0 * SS / 4))
    img = img.resize((S, S), Image.LANCZOS)
    img.save(f"{ROOT}/Content/Effects/Procedural/JellyfishBell.png")
    print("JellyfishBell.png OK", img.size)


# ================================================================
#  2. JELLYFISH GALAXY — el corazón: una minigalaxia espiral
# ================================================================
def gen_galaxy():
    S = 128
    SS = 4
    W = S * SS
    cx = cy = W / 2
    img = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    px = img.load()

    # Núcleo: resplandor blanco-dorado
    core = (255, 244, 214)
    halo = (255, 210, 160)
    out = (120, 200, 255)
    R = W * 0.48
    for y in range(W):
        for x in range(W):
            dx, dy = x - cx, y - cy
            r = math.hypot(dx, dy) / R
            if r > 1.0:
                continue
            if r < 0.3:
                t = smoothstep(0.0, 0.3, r)
                c = lerp3(core, halo, t)
                a = lerp(255, 120, t)
            else:
                t = smoothstep(0.3, 1.0, r)
                c = lerp3(halo, out, t)
                a = lerp(120, 0, t)
            px[x, y] = (*c, int(a))

    draw = ImageDraw.Draw(img, "RGBA")

    # --- DOS BRAZOS ESPIRALES (logarítmicos) sembrados de estrellas ---
    def arm(phase, n=70):
        a_spiral = 0.30
        b_spiral = 0.24
        for i in range(n):
            t = i / (n - 1)
            th = phase + t * 2.6          # ~150° de barrido por brazo
            r = (a_spiral * math.exp(b_spiral * th * 1.15)) * R
            if r > R * 0.96:
                continue
            # dispersión alrededor del brazo (debajo y encima)
            for _ in range(random.randint(1, 3)):
                jitter_r = r + random.gauss(0, W * 0.012)
                jitter_th = th + random.gauss(0, 0.10)
                x = cx + math.cos(jitter_th) * jitter_r
                y = cy + math.sin(jitter_th) * jitter_r
                if 0 <= x < W and 0 <= y < W:
                    fade = 1.0 - t * 0.55
                    warm = random.random() < 0.45
                    b = int(random.uniform(120, 255) * fade)
                    col = (255, 245, 225, b) if warm else (185, 225, 255, b)
                    sz = random.uniform(0.9, 2.2) * SS / 4 * (1.0 - t * 0.4)
                    draw.ellipse([x - sz, y - sz, x + sz, y + sz], fill=col)

    arm(0.0)
    arm(math.pi)
    # estrellitas dispersas del halo
    for _ in range(24):
        ang = random.uniform(0, math.tau)
        rr = random.uniform(0.25, 0.55) * R
        x = cx + math.cos(ang) * rr
        y = cy + math.sin(ang) * rr
        b = random.randint(70, 160)
        sz = random.uniform(0.7, 1.6) * SS / 4
        draw.ellipse([x - sz, y - sz, x + sz, y + sz], fill=(230, 240, 255, b))

    img = img.filter(ImageFilter.GaussianBlur(0.8 * SS / 4))
    img = img.resize((S, S), Image.LANCZOS)
    img.save(f"{ROOT}/Content/Effects/Procedural/JellyfishGalaxy.png")
    print("JellyfishGalaxy.png OK", img.size)


# ================================================================
#  3. JELLYFISH BEAD — cuenta de tentáculo (tintable)
# ================================================================
def gen_bead():
    S = 64
    SS = 4
    W = S * SS
    cx = cy = W / 2
    R = W * 0.46
    img = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    px = img.load()

    for y in range(W):
        for x in range(W):
            dx, dy = x - cx, y - cy
            r = math.hypot(dx, dy) / R
            if r > 1.0:
                continue
            # núcleo blanco puro (tintable por Color en el draw) → caída suave
            if r < 0.35:
                t = smoothstep(0.0, 0.35, r)
                c = lerp3((255, 255, 255), (235, 245, 250), t)
                a = 255
            else:
                t = smoothstep(0.35, 1.0, r)
                c = lerp3((235, 245, 250), (200, 225, 240), t)
                a = lerp(235, 0, t * t)
            px[x, y] = (*c, int(a))

    draw = ImageDraw.Draw(img, "RGBA")
    # destello de 4 puntas (cruz fina)
    arm_len = R * 0.92
    wid = int(1.2 * SS / 4)
    draw.line([(cx - arm_len, cy), (cx + arm_len, cy)], fill=(255, 255, 255, 120), width=wid)
    draw.line([(cx, cy - arm_len), (cx, cy + arm_len)], fill=(255, 255, 255, 120), width=wid)

    img = img.filter(ImageFilter.GaussianBlur(0.6 * SS / 4))
    img = img.resize((S, S), Image.LANCZOS)
    img.save(f"{ROOT}/Content/Effects/Procedural/JellyfishBead.png")
    print("JellyfishBead.png OK", img.size)


# ================================================================
#  4. MEDUSA NEBULAR STAFF — icono del arma (28x30)
# ================================================================
def gen_staff_icon():
    W, H = 28 * 4, 30 * 4  # supersampleado directo
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img, "RGBA")

    # Vara diagonal: carbón-teal con brillo
    shaft = [(7 * 4, 27 * 4), (9 * 4, 22 * 4), (17 * 4, 8 * 4), (19 * 4, 9 * 4), (11 * 4, 24 * 4), (9 * 4, 28 * 4)]
    draw.polygon(shaft, fill=(38, 62, 66, 255))
    draw.line([(9 * 4, 25 * 4), (18 * 4, 8.5 * 4)], fill=(94, 160, 150, 200), width=3)

    # Medusa en la cima: campana
    jx, jy = 18 * 4, 6 * 4
    # halo
    for rr, a in ((13, 40), (10, 70), (7, 110)):
        draw.ellipse([jx - rr, jy - rr * 0.8, jx + rr, jy + rr * 0.8], fill=(60, 200, 170, a))
    # campana (domo)
    draw.pieslice([jx - 9, jy - 9, jx + 9, jy + 9], 180, 360, fill=(94, 235, 200, 235))
    draw.pieslice([jx - 6.4, jy - 7, jx + 6.4, jy + 7], 180, 360, fill=(190, 255, 238, 255))
    # margen bioluminiscente rosa
    draw.arc([jx - 9, jy - 9, jx + 9, jy + 9], 180, 360, fill=(255, 130, 205, 255), width=2)
    # núcleo galaxia
    draw.ellipse([jx - 2.4, jy - 4.6, jx + 2.4, jy + 0.2], fill=(255, 240, 190, 255))
    # tentáculos colgantes
    for k, (tx, wob) in enumerate(((-5.5, 2.5), (-2, 4), (2.2, 3.5), (5.5, 2))):
        pts = []
        for s in range(6):
            t = s / 5
            pts.append((jx + tx + math.sin(t * 3 + k) * wob * 0.5, jy + 1 + t * 8))
        col = (110, 230, 255, 235) if k % 2 == 0 else (255, 150, 220, 220)
        draw.line(pts, fill=col, width=2)
    # estrellitas
    for (sx, sy, b) in ((4, 5, 220), (24, 12, 200), (6, 16, 170), (23, 3, 160)):
        draw.ellipse([sx * 4 - 2, sy * 4 - 2, sx * 4 + 2, sy * 4 + 2], fill=(255, 255, 240, b))

    img = img.filter(ImageFilter.GaussianBlur(0.5))
    img = img.resize((28, 30), Image.LANCZOS)
    img.save(f"{ROOT}/Content/Weapons/Cosmic/MedusaNebularStaff.png")
    print("MedusaNebularStaff.png OK", img.size)


# ================================================================
#  5. NEBULA JELLYFISH BUFF — icono del buff (32x32)
# ================================================================
def gen_buff_icon():
    S = 32
    SS = 4
    W = S * SS
    img = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img, "RGBA")
    cx, cy = W / 2, W * 0.42

    # halo
    for rr, a in ((W * 0.36, 36), (W * 0.28, 60), (W * 0.2, 90)):
        draw.ellipse([cx - rr, cy - rr * 0.85, cx + rr, cy + rr * 0.85], fill=(60, 200, 170, a))
    # campana
    bellR = W * 0.27
    draw.pieslice([cx - bellR, cy - bellR, cx + bellR, cy + bellR], 180, 360, fill=(94, 235, 200, 230))
    draw.pieslice([cx - bellR * 0.7, cy - bellR * 0.78, cx + bellR * 0.7, cy + bellR * 0.78], 180, 360, fill=(190, 255, 238, 250))
    draw.arc([cx - bellR, cy - bellR, cx + bellR, cy + bellR], 180, 360, fill=(255, 130, 205, 255), width=int(1.6 * SS / 4))
    # núcleo
    draw.ellipse([cx - W * 0.07, cy - W * 0.13, cx + W * 0.07, cy + W * 0.01], fill=(255, 240, 190, 255))
    # tentáculos
    for k, tx in enumerate((-0.16, -0.06, 0.06, 0.16)):
        pts = []
        for s in range(6):
            t = s / 5
            pts.append((cx + tx * W + math.sin(t * 3 + k) * W * 0.03, cy + t * W * 0.26))
        col = (110, 230, 255, 235) if k % 2 == 0 else (255, 150, 220, 225)
        draw.line(pts, fill=col, width=int(1.8 * SS / 4))
    # estrellitas
    for (fx, fy, b) in ((0.16, 0.2, 210), (0.85, 0.28, 190), (0.3, 0.62, 160)):
        draw.ellipse([fx * W - 2.5, fy * W - 2.5, fx * W + 2.5, fy * W + 2.5], fill=(255, 255, 240, b))

    img = img.filter(ImageFilter.GaussianBlur(0.6))
    img = img.resize((S, S), Image.LANCZOS)
    img.save(f"{ROOT}/Content/Buffs/NebulaJellyfishBuff.png")
    print("NebulaJellyfishBuff.png OK", img.size)


if __name__ == "__main__":
    gen_bell()
    gen_galaxy()
    gen_bead()
    gen_staff_icon()
    gen_buff_icon()
    print("Listo: texturas de LA MEDUSA NEBULAR generadas.")
