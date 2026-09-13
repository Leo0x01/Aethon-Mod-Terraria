#!/usr/bin/env python3
"""v5.99 — Texturas de LOS TRES NUEVOS CONTENIDOS CÓSMICOS.

1. EL COMETA ESTELAR (minion invocador, inspirado en la imagen de referencia
   del usuario — estrella de 8 puntas con núcleo cálido y cuerpo frío):
   - CometHead.png (192): núcleo de plasma blanco-oro con granulación +
     corona cálida que se enfría hacia el borde violeta.
   - CometCrown.png (256): 8 puntas lanceoladas (los brazos de la estrella
     de la referencia) en degradado cian→violeta, con ligera asimetría
     orgánica. ROTA lentamente alrededor del núcleo en el juego.
   - Iconos: arma 30×30, buff 32×32.

2. EL PÚLSAR VIVO (minion invocador — estrella de neutrones girando con dos
   haces de faro):
   - PulsarCore.png (128): núcleo blanco-azul EXTREMO con bloom y arcos
     magnéticos tenues. Los haces se dibujan en código.
   - Iconos: arma 30×30, buff 32×32.

3. LA LANZA DEL QUÁSAR (arma mágica — chorro relativista):
   - Icono del arma 30×30 (lanza con cabeza de chorro brillante).

Todo a 4x supersampling + LANCZOS. Semilla fija.
"""
import math
import random
from PIL import Image, ImageDraw, ImageFilter

random.seed(20260914)

ROOT = "/home/z/my-project/AethonMod/AethonMod"


def lerp(a, b, t):
    return a + (b - a) * t


def lerp3(c1, c2, t):
    return tuple(int(round(lerp(c1[i], c2[i], t))) for i in range(3))


def clamp(x, lo, hi):
    return max(lo, min(hi, x))


def smoothstep(e0, e1, x):
    t = clamp((x - e0) / (e1 - e0), 0.0, 1.0)
    return t * t * (3.0 - 2.0 * t)


# ================================================================
#  1. COMETA — núcleo
# ================================================================
def gen_comet_head():
    S = 192
    SS = 4
    W = S * SS
    cx = cy = W / 2
    R = W * 0.47
    img = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    px = img.load()

    # paleta: núcleo blanco-oro → corona dorada → halo frío violeta
    c_core = (255, 252, 240)
    c_gold = (255, 216, 130)
    c_warm = (255, 168, 92)
    c_cool = (150, 120, 220)

    for y in range(W):
        for x in range(W):
            dx, dy = x - cx, y - cy
            d = math.sqrt(dx * dx + dy * dy)
            if d > R:
                continue
            rn = d / R
            # núcleo brillante compacto + corona que se enfría
            if rn < 0.22:
                base = lerp3(c_core, c_gold, rn / 0.22)
                a = 255
            elif rn < 0.55:
                base = lerp3(c_gold, c_warm, (rn - 0.22) / 0.33)
                a = int(lerp(255, 190, (rn - 0.22) / 0.33))
            else:
                base = lerp3(c_warm, c_cool, (rn - 0.55) / 0.45)
                a = int(lerp(190, 0, smoothstep(0.55, 1.0, rn)))
            # granulación del plasma (celdas de convección sutiles)
            gran = math.sin(x * 0.055) * math.sin(y * 0.061) * \
                math.sin((x + y) * 0.033)
            g = 14 * gran * (1.0 - rn)
            r = int(clamp(base[0] + g, 0, 255))
            gg = int(clamp(base[1] + g * 0.8, 0, 255))
            b = int(clamp(base[2] + g * 0.5, 0, 255))
            px[x, y] = (r, gg, b, a)

    # hotspot off-center (el punto más caliente del plasma)
    hot = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    hd = ImageDraw.Draw(hot)
    hx, hy = cx - R * 0.14, cy - R * 0.1
    for k, (rr, aa) in enumerate([(R * 0.30, 120), (R * 0.16, 160)]):
        hd.ellipse([hx - rr, hy - rr, hx + rr, hy + rr],
                   fill=(255, 255, 250, aa))
    img = Image.alpha_composite(img, hot.filter(ImageFilter.GaussianBlur(SS * 3)))

    return img.resize((S, S), Image.LANCZOS)


# ================================================================
#  2. COMETA — corona de 8 puntas (la estrella de la referencia)
# ================================================================
def gen_comet_crown():
    S = 256
    SS = 4
    W = S * SS
    cx = cy = W / 2
    img = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    innerR = W * 0.185          # arranca fuera del núcleo
    # ligera asimetría orgánica por punta (la referencia no es uniforme)
    asym = [random.uniform(0.88, 1.12) for _ in range(8)]

    for i in range(8):
        ang = i * math.tau / 8 - math.pi / 2   # punta arriba primero
        L = W * 0.46 * asym[i]                 # largo de la punta
        wBase = W * 0.052                      # ancho en la base
        # vértices de la punta lanceolada (rombo alargado con lados cóncavos)
        tip = (cx + math.cos(ang) * (innerR + L),
               cy + math.sin(ang) * (innerR + L))
        perp = ang + math.pi / 2
        b1 = (cx + math.cos(ang) * innerR + math.cos(perp) * wBase,
              cy + math.sin(ang) * innerR + math.sin(perp) * wBase)
        b2 = (cx + math.cos(ang) * innerR - math.cos(perp) * wBase,
              cy + math.sin(ang) * innerR - math.sin(perp) * wBase)
        mid = (cx + math.cos(ang) * (innerR + L * 0.55),
               cy + math.sin(ang) * (innerR + L * 0.55))
        # polígono punta (lanceolada: dos triángulos suavizados)
        d.polygon([b1, tip, b2, mid], fill=(255, 255, 255, 255))

    # colorear por distancia radial: cian en la base → violeta en la punta
    px = img.load()
    out = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    opx = out.load()
    c_base = (150, 245, 255)   # cian brillante (cerca del núcleo)
    c_mid = (120, 180, 255)    # azul eléctrico
    c_tip = (170, 120, 255)    # violeta (la punta fría)
    for y in range(W):
        for x in range(W):
            r, g, b, a = px[x, y]
            if a == 0:
                continue
            dx, dy = x - cx, y - cy
            dist = math.sqrt(dx * dx + dy * dy)
            t = clamp((dist - innerR) / (W * 0.46 - innerR), 0.0, 1.0)
            if t < 0.5:
                col = lerp3(c_base, c_mid, t / 0.5)
            else:
                col = lerp3(c_mid, c_tip, (t - 0.5) / 0.5)
            # el filo central de cada punta brilla (canal de energía)
            ang = math.atan2(dy, dx) + math.pi / 2
            # brillo del eje: cerca del ángulo del brazo correspondiente
            ang8 = (math.atan2(dy, dx) + math.pi / 2) % (math.tau / 8)
            axis = 1.0 - min(ang8, math.tau / 8 - ang8) / (math.tau / 16)
            boost = axis * 0.5 * (1.0 - t)
            r = int(clamp(col[0] + boost * 105, 0, 255))
            g = int(clamp(col[1] + boost * 105, 0, 255))
            b = int(clamp(col[2] + boost * 90, 0, 255))
            # alpha: nítida en la base, se disuelve hacia la punta
            a = int(lerp(235, 25, t ** 1.3))
            opx[x, y] = (r, g, b, a)

    # glow tenue del conjunto (la corona EMITE)
    glow = out.filter(ImageFilter.GaussianBlur(SS * 2.5))
    from PIL import ImageChops
    final = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    final = Image.alpha_composite(final, glow)
    final = Image.alpha_composite(final, out)

    return final.resize((S, S), Image.LANCZOS)


# ================================================================
#  3. PÚLSAR — núcleo
# ================================================================
def gen_pulsar_core():
    S = 128
    SS = 4
    W = S * SS
    cx = cy = W / 2
    R = W * 0.30
    img = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    px = img.load()

    # núcleo blanco-azul EXTREMO (una estrella de neutrones es pura densidad)
    for y in range(W):
        for x in range(W):
            dx, dy = x - cx, y - cy
            d = math.sqrt(dx * dx + dy * dy)
            if d > W * 0.5:
                continue
            rn = d / R if d < R else 1e9
            if d < R:
                if rn < 0.4:
                    col, a = (255, 255, 255), 255
                elif rn < 0.75:
                    t = (rn - 0.4) / 0.35
                    col, a = lerp3((255, 255, 255), (150, 220, 255), t), int(lerp(255, 170, t))
                else:
                    t = (rn - 0.75) / 0.25
                    col, a = lerp3((150, 220, 255), (80, 140, 255), t), int(lerp(170, 0, t))
            else:
                # halo lejano tenue
                hn = d / (W * 0.5)
                col, a = (90, 150, 255), int(60 * (1.0 - hn) ** 2)
            px[x, y] = (col[0], col[1], col[2], a)

    # halo lejano más contenido (menos "mancha", más foco)
    # arcos magnéticos NÍTIDOS: trazos oscuros debajo + núcleo brillante
    # encima + nodos brillantes donde cruzan (definición de silueta)
    arcs = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    ad = ImageDraw.Draw(arcs)
    for rot in (0.5, -0.5):
        ex, ey = R * 2.0, R * 0.85
        pts = []
        for s in range(129):
            a = s / 128 * math.tau
            xx = math.cos(a) * ex
            yy = math.sin(a) * ey
            rx = xx * math.cos(rot) - yy * math.sin(rot)
            ry = xx * math.sin(rot) + yy * math.cos(rot)
            pts.append((cx + rx, cy + ry))
        # pasada 1: núcleo brillante fino
        ad.line(pts, fill=(200, 235, 255, 235), width=SS * 2, joint="curve")
        # pasada 2: halo suave alrededor
        ad.line(pts, fill=(90, 150, 230, 120), width=SS * 5, joint="curve")
    img = Image.alpha_composite(img, arcs.filter(ImageFilter.GaussianBlur(SS * 0.9)))

    # nodos brillantes en los 4 extremos de los arcos (los POLOS magnéticos)
    nodes = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    nd = ImageDraw.Draw(nodes)
    for rot in (0.5, -0.5):
        for a in (0, math.pi):
            ex, ey = R * 2.0, R * 0.85
            xx = math.cos(a) * ex
            yy = math.sin(a) * ey
            rx = xx * math.cos(rot) - yy * math.sin(rot)
            ry = xx * math.sin(rot) + yy * math.cos(rot)
            nx, ny = cx + rx, cy + ry
            for rr, aa in ((SS * 6, 110), (SS * 3, 210)):
                nd.ellipse([nx - rr, ny - rr, nx + rr, ny + rr],
                           fill=(220, 240, 255, aa))
    img = Image.alpha_composite(img, nodes.filter(ImageFilter.GaussianBlur(SS * 1.2)))

    # bandas de giro en el ecuador (la estrella GIRA — achatada)
    bands = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    bd = ImageDraw.Draw(bands)
    for k in range(3):
        rr = R * (1.18 + k * 0.10)
        sq = 0.42 + k * 0.05
        bd.ellipse([cx - rr, cy - rr * sq, cx + rr, cy + rr * sq],
                   outline=(140, 200, 255, 150 - k * 35), width=SS * 2)
    img = Image.alpha_composite(img, bands.filter(ImageFilter.GaussianBlur(SS * 0.8)))

    return img.resize((S, S), Image.LANCZOS)


# ================================================================
#  ICONOS
# ================================================================
def icon_comet_staff():
    W = 120
    img = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    cx, cy = W / 2, W / 2
    # corona de 8 puntas
    for i in range(8):
        ang = i * math.tau / 8 - math.pi / 2
        L = W * 0.42
        x0, y0 = cx + math.cos(ang) * W * 0.14, cy + math.sin(ang) * W * 0.14
        x1, y1 = cx + math.cos(ang) * L, cy + math.sin(ang) * L
        col = lerp3((150, 245, 255), (170, 120, 255), i / 7)
        d.line([(x0, y0), (x1, y1)], fill=col + (230,), width=5)
    # núcleo
    d.ellipse([cx - W * 0.15, cy - W * 0.15, cx + W * 0.15, cy + W * 0.15],
              fill=(255, 250, 225, 255))
    d.ellipse([cx - W * 0.07, cy - W * 0.07, cx + W * 0.07, cy + W * 0.07],
              fill=(255, 255, 255, 255))
    # cola corta abajo-izquierda
    tail = [(cx - W * 0.10, cy + W * 0.10), (cx - W * 0.30, cy + W * 0.30)]
    d.line(tail, fill=(255, 200, 120, 140), width=6)
    return img.resize((30, 30), Image.LANCZOS)


def icon_comet_buff():
    W = 128
    img = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    cx, cy = W / 2, W / 2
    for i in range(8):
        ang = i * math.tau / 8 - math.pi / 2
        x1 = cx + math.cos(ang) * W * 0.44
        y1 = cy + math.sin(ang) * W * 0.44
        d.line([(cx, cy), (x1, y1)],
               fill=lerp3((150, 245, 255), (170, 120, 255), i / 7) + (220,), width=6)
    d.ellipse([cx - W * 0.16, cy - W * 0.16, cx + W * 0.16, cy + W * 0.16],
              fill=(255, 250, 225, 255))
    return img.resize((32, 32), Image.LANCZOS)


def icon_pulsar_staff():
    W = 120
    img = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    cx, cy = W / 2, W / 2
    # núcleo brillante
    d.ellipse([cx - W * 0.10, cy - W * 0.10, cx + W * 0.10, cy + W * 0.10],
              fill=(235, 245, 255, 255))
    d.ellipse([cx - W * 0.05, cy - W * 0.05, cx + W * 0.05, cy + W * 0.05],
              fill=(255, 255, 255, 255))
    # dos haces de faro cruzados
    for ang in (math.radians(-30), math.radians(150)):
        x1 = cx + math.cos(ang) * W * 0.48
        y1 = cy + math.sin(ang) * W * 0.48
        x0 = cx + math.cos(ang) * W * 0.12
        y0 = cy + math.sin(ang) * W * 0.12
        # haz: línea gruesa que afina
        for k, wdt in enumerate((10, 6, 3)):
            t0, t1 = 0.12 + k * 0.30, 0.12 + (k + 1) * 0.30
            d.line([(cx + math.cos(ang) * W * t0, cy + math.sin(ang) * W * t0),
                    (cx + math.cos(ang) * W * t1, cy + math.sin(ang) * W * t1)],
                   fill=(150, 220, 255, 200 - k * 40), width=wdt)
    # anillo ecuatorial
    d.ellipse([cx - W * 0.2, cy - W * 0.08, cx + W * 0.2, cy + W * 0.08],
              outline=(120, 180, 255, 180), width=3)
    return img.resize((30, 30), Image.LANCZOS)


def icon_pulsar_buff():
    W = 128
    img = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    cx, cy = W / 2, W / 2
    d.ellipse([cx - W * 0.11, cy - W * 0.11, cx + W * 0.11, cy + W * 0.11],
              fill=(235, 245, 255, 255))
    for ang in (math.radians(-30), math.radians(150)):
        d.line([(cx + math.cos(ang) * W * 0.13, cy + math.sin(ang) * W * 0.13),
                (cx + math.cos(ang) * W * 0.46, cy + math.sin(ang) * W * 0.46)],
               fill=(150, 220, 255, 210), width=8)
    return img.resize((32, 32), Image.LANCZOS)


def icon_quasar_lance():
    W = 120
    img = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    cx, cy = W / 2, W / 2
    # la lanza diagonal: mango violeta oscuro
    ang = math.radians(-45)
    x0 = cx - math.cos(ang) * W * 0.42
    y0 = cy - math.sin(ang) * W * 0.42
    x1 = cx + math.cos(ang) * W * 0.30
    y1 = cy + math.sin(ang) * W * 0.30
    d.line([(x0, y0), (x1, y1)], fill=(90, 70, 160, 255), width=7)
    d.line([(x0, y0), (x1, y1)], fill=(150, 120, 220, 140), width=3)
    # la cabeza: núcleo brillante + chorro hacia arriba-derecha
    hx = cx + math.cos(ang) * W * 0.32
    hy = cy + math.sin(ang) * W * 0.32
    d.ellipse([hx - W * 0.09, hy - W * 0.09, hx + W * 0.09, hy + W * 0.09],
              fill=(255, 255, 250, 255))
    # el chorro: 3 nudos que se apagan
    for k in range(3):
        t = 0.38 + k * 0.14
        kx = cx + math.cos(ang) * W * t
        ky = cy + math.sin(ang) * W * t
        r = W * (0.055 - k * 0.012)
        d.ellipse([kx - r, ky - r, kx + r, ky + r],
                  fill=(150, 220, 255, 220 - k * 55))
    # halo
    d.ellipse([hx - W * 0.16, hy - W * 0.16, hx + W * 0.16, hy + W * 0.16],
              outline=(120, 200, 255, 90), width=4)
    return img.resize((30, 30), Image.LANCZOS)


def placeholder(path):
    Image.new("RGBA", (1, 1), (0, 0, 0, 0)).save(path, optimize=True)


if __name__ == "__main__":
    proc = f"{ROOT}/Content/Effects/Procedural"
    wpn = f"{ROOT}/Content/Weapons/Cosmic"
    buffs = f"{ROOT}/Content/Buffs"
    proj = f"{ROOT}/Content/Projectiles/Cosmic"

    gen_comet_head().save(f"{proc}/CometHead.png")
    gen_comet_crown().save(f"{proc}/CometCrown.png")
    gen_pulsar_core().save(f"{proc}/PulsarCore.png")

    icon_comet_staff().save(f"{wpn}/LivingCometStaff.png")
    icon_comet_buff().save(f"{buffs}/StellarCometBuff.png")
    icon_pulsar_staff().save(f"{wpn}/LivingPulsarStaff.png")
    icon_pulsar_buff().save(f"{buffs}/LivingPulsarBuff.png")
    icon_quasar_lance().save(f"{wpn}/QuasarLance.png")

    placeholder(f"{proj}/StellarCometMinion.png")
    placeholder(f"{proj}/LivingPulsarMinion.png")
    placeholder(f"{proj}/QuasarJetProjectile.png")
    print("OK — texturas de los 3 contenidos nuevos generadas")
