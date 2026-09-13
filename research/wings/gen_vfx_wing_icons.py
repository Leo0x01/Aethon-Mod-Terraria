#!/usr/bin/env python3
"""
gen_vfx_wing_icons.py — v6.08 — ICONOS PROCEDURALES DE LAS 8 ALAS DE LUZ.

Cada icono es una mini-viñeta del estilo de ala correspondiente (la misma
imagen mental que el renderizador dibuja en el mundo): 30×24 px finales con
supersampling 4× (canvas de trabajo 120×96) y composición ADITIVA sobre
fondo transparente — como se ven los brillos del juego.

La forma de cada icono replica la SILUETA del ala para que el inventario
cuente la misma historia que el mundo.
"""
from PIL import Image
import math
import os
import random

OUT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..",
                    "AethonMod", "Content", "Items", "Wings"))
SS = 4            # supersampling
W, H = 30, 24     # tamaño final
CW, CH = W * SS, H * SS


def new_canvas():
    return [[[0.0, 0.0, 0.0, 0.0] for _ in range(CW)] for _ in range(CH)]


def add_glow(c, x, y, radius, color, alpha=1.0, squash=1.0, stretch=1.0):
    """Blob radial suave aditivo (soft glow estilo Terraria)."""
    r = radius * SS
    x0, y0 = x * SS, y * SS
    rx = r * stretch
    ry = r * squash
    if rx < 0.1 or ry < 0.1:
        return
    ia = max(0, int(x0 - rx) - 1), min(CW, int(x0 + rx) + 2)
    ja = max(0, int(y0 - ry) - 1), min(CH, int(y0 + ry) + 2)
    for j in range(ja[0], ja[1]):
        for i in range(ia[0], ia[1]):
            dx = (i - x0) / max(rx, 0.001)
            dy = (j - y0) / max(ry, 0.001)
            d = math.sqrt(dx * dx + dy * dy)
            if d >= 1.0:
                continue
            # Falloff suave (smoothstep invertido al cuadrado).
            f = (1.0 - d)
            f = f * f * (3 - 2 * f)
            a = f * alpha
            px = c[j][i]
            px[0] = min(1.0, px[0] + color[0] * a)
            px[1] = min(1.0, px[1] + color[1] * a)
            px[2] = min(1.0, px[2] + color[2] * a)
            px[3] = min(1.0, px[3] + a)


def add_streak(c, pts, thick, color, alpha=1.0, taper=True):
    """Cinta de puntos a lo largo de una polilínea (grosores variables)."""
    n = len(pts)
    for k, (x, y) in enumerate(pts):
        t = k / max(n - 1, 1)
        th = thick * (1.0 - 0.65 * t) if taper else thick
        a = alpha * (1.0 - 0.25 * t)
        add_glow(c, x, y, th, color, a)


def canvas_to_png(c, path):
    img = Image.new("RGBA", (CW, CH))
    px = img.load()
    for j in range(CH):
        for i in range(CW):
            r, g, b, a = c[j][i]
            px[i, j] = (int(r * 255), int(g * 255), int(b * 255), int(a * 255))
    img = img.resize((W, H), Image.LANCZOS)
    img.save(path)
    print("icono:", os.path.basename(path))


def lerp_color(c1, c2, t):
    return tuple(c1[k] + (c2[k] - c1[k]) * t for k in range(3))


# ---------------------------------------------------------------------------
#  1 — HORIZONTE DE SUCESOS: núcleo negro + anillo rosa + rastro carmesí
# ---------------------------------------------------------------------------
def icon_event_horizon():
    c = new_canvas()
    cx, cy = 11, 12
    # Neblina púrpura.
    add_glow(c, cx + 4, cy, 11, (0.10, 0.04, 0.18), 0.8)
    # El rastro de acreción: cinta que sube y se estira a la derecha.
    streak = []
    for k in range(9):
        t = k / 8
        x = cx + 1 + t * 15
        y = cy + 3 - math.sin(t * math.pi * 0.9) * 7 + t * t * 2
        streak.append((x, y))
    for k, p in enumerate(streak):
        t = k / 8
        col = lerp_color((1.0, 0.35, 0.72), (0.45, 0.05, 0.25), t)
        add_glow(c, p[0], p[1], 2.6 - 1.4 * t, col, 0.9 - 0.25 * t)
    # El mini horizonte: anillo de fotones + disco negro + chispa.
    add_glow(c, cx, cy, 5.2, (1.0, 0.45, 0.75), 1.0)     # glow rosa NEÓN
    add_glow(c, cx, cy, 3.8, (1.0, 0.7, 0.9), 0.9)      # halo interno brillante
    # El "negro" del icono: se logra con alpha alto y rgb 0 — se dibuja
    # DESPUÉS del glow para taparlo (usamos un blob oscuro sólido).
    add_glow(c, cx, cy, 2.6, (0.02, 0.0, 0.05), 0.97)
    add_glow(c, cx - 1.5, cy - 1.8, 0.9, (1, 1, 1), 1.0)  # chispa del limbo
    # Eco del anillo de Einstein en la punta.
    add_glow(c, streak[-1][0], streak[-1][1], 1.6, (1.0, 0.6, 0.82), 0.8)
    return c


# ---------------------------------------------------------------------------
#  2 — ANILLO DE FOTONES: aros dorados en abanico con fotones
# ---------------------------------------------------------------------------
def icon_photon_ring():
    c = new_canvas()
    cx, cy = 10, 14
    add_glow(c, cx + 5, cy - 3, 10, (0.12, 0.02, 0.10), 0.7)
    for h in range(3):
        r = 5.5 + 4.5 * h
        tilt = -0.35 + 0.18 * h
        col = lerp_color((1.0, 0.72, 0.86), (0.95, 0.2, 0.6), h / 2)
        # Aro: 26 puntos en la elipse girada (mitad superior).
        for k in range(27):
            a = math.pi * k / 26
            ex, ey = math.cos(a) * r * 1.5, -math.sin(a) * r
            ct, st = math.cos(tilt), math.sin(tilt)
            x = cx + ex * ct - ey * st
            y = cy + ex * st + ey * ct
            add_glow(c, x, y, 1.1, col, 0.75)
        # Fotón brillante en cada aro.
        a = 1.9 + h * 0.5
        ex, ey = math.cos(a) * r * 1.5, -math.sin(a) * r
        ct, st = math.cos(tilt), math.sin(tilt)
        add_glow(c, cx + ex * ct - ey * st, cy + ex * st + ey * ct,
                 1.6, (1.0, 0.95, 1.0), 1.0)
    # Núcleo blanco.
    add_glow(c, cx, cy, 2.4, (1.0, 0.3, 0.7), 1.0)
    add_glow(c, cx, cy, 1.2, (1, 1, 1), 1.0)
    return c


# ---------------------------------------------------------------------------
#  3 — MARIPOSA CÓSMICA: la silueta de mariposa completa
# ---------------------------------------------------------------------------
def icon_butterfly():
    c = new_canvas()
    cx, cy = 15, 13
    rng = random.Random(7)

    def lobe(cxl, cyl, W, H, upper):
        # Membrana: retícula jitter.
        for k in range(16):
            u = rng.random()
            v = rng.random()
            theta = (0.15 + 2.2 * u) * 0.5
            r = 1 - 0.15 * math.sin(u * math.pi)
            ex = math.cos(theta) * r * (0.55 + 0.5 * math.sin(theta + 0.4)) * W
            ey = math.sin(theta) * r * 0.82 * H
            ix, iy = ex * (0.25 + 0.75 * v), ey * (0.25 + 0.75 * v)
            mem = lerp_color((0.30, 0.0, 0.16), (0.85, 0.1, 0.5), v)
            add_glow(c, cxl + ix, cyl - abs(iy), W * 0.30, mem, 0.55)
        # Venas.
        for v in range(4):
            vang = -0.1 + 1.3 * v / 3
            for s in range(5):
                t = s / 4
                bend = vang * t + (1 - t) * 0.35
                vx = math.sin(bend) * t * W * 0.92
                vy = math.cos(bend) * t * H * 0.88
                add_glow(c, cxl + vx, cyl - vy, 0.8,
                         (1.0, 0.6, 0.82), 0.55 - 0.25 * t)
        # Borde dorado.
        for k in range(18):
            u = k / 17
            theta = (0.15 + 2.2 * u) * 0.5
            r = 1 - 0.15 * math.sin(u * math.pi)
            ex = math.cos(theta) * r * (0.55 + 0.5 * math.sin(theta + 0.4)) * W
            ey = math.sin(theta) * r * 0.82 * H
            rim = lerp_color((1.0, 0.8, 0.38), (1.0, 0.92, 0.67), u * 0.6)
            add_glow(c, cxl + ex, cyl - abs(ey), 0.85, rim, 0.8)

    for side in (-1, 1):
        lobe(cx + side * 1.5, cy - 1.5, 9.5, 7.5, True)          # superior
        lobe(cx + side * 2.8, cy + 3.5, 6.0, 4.5, False)          # inferior
        # Ojo del lóbulo superior.
        add_glow(c, cx + side * 6.2, cy - 4.2, 1.5, (1.0, 0.6, 0.82), 0.9)
        add_glow(c, cx + side * 6.2, cy - 4.2, 0.8, (0.1, 0.0, 0.05), 0.95)
    # Tórax dorado.
    add_glow(c, cx, cy, 1.8, (1.0, 0.66, 0.31), 1.0)
    return c


# ---------------------------------------------------------------------------
#  4 — HADA: 4 lóbulos dorados puntiagudos + destellos
# ---------------------------------------------------------------------------
def icon_fairy():
    c = new_canvas()
    cx, cy = 11, 15
    lobes = [(-1.28, 8.5, 3.4), (-0.85, 10.5, 3.8), (-0.30, 7.0, 3.2), (0.12, 5.5, 2.6)]
    rng = random.Random(3)
    for ang, L, wid in lobes:
        dx, dy = math.cos(ang), math.sin(ang)
        px, py = -dy, dx
        for s in range(6):
            t = (s + 0.5) / 6
            profile = math.sin(t * math.pi) * (1 - t * 0.35)
            w = wid * (0.45 + profile)
            x = cx + dx * L * t
            y = cy + dy * L * t
            mem = lerp_color((1.0, 0.85, 0.55), (1.0, 0.72, 0.42), t)
            add_glow(c, x, y, w * 1.6, mem, 0.5)
        for s in range(8):
            t = s / 7
            profile = math.sin(t * math.pi)
            w = wid * (0.45 + profile)
            x = cx + dx * L * t
            y = cy + dy * L * t
            rim = lerp_color((1.0, 0.92, 0.67), (1.0, 0.7, 0.35), t * 0.7)
            add_glow(c, x + px * w, y + py * w, 0.85, rim, 0.85)
            add_glow(c, x - px * w, y - py * w, 0.85, rim, 0.85)
        # Punta brillante.
        add_glow(c, cx + dx * L, cy + dy * L, 1.4, (1.0, 0.94, 0.75), 1.0)
    # Destellos de polvo estelar.
    for k in range(6):
        u, v = rng.random(), rng.random()
        x = cx + 2 + u * 12
        y = cy - 8 + v * 12
        col = (1.0, 0.96, 0.82) if v > 0.5 else (1.0, 0.78, 0.92)
        add_glow(c, x, y, 1.0, col, 1.0)
    # Corazón del hada.
    add_glow(c, cx, cy, 2.6, (1.0, 0.84, 0.59), 1.0)
    add_glow(c, cx, cy, 1.2, (1, 1, 1), 1.0)
    return c


# ---------------------------------------------------------------------------
#  5 — CORONA SOLAR: lazos de prominencia naranjas
# ---------------------------------------------------------------------------
def icon_solar_corona():
    c = new_canvas()
    cx, cy = 8, 15
    add_glow(c, cx + 6, cy - 5, 11, (0.5, 0.15, 0.03), 0.9)
    for l in range(4):
        base = 2 + 3.2 * l
        h = 7 + 4.2 * l
        w = 4 + 2.4 * l
        prev = (cx + base, cy)
        for s in range(13):
            t = s / 12
            ax = -w * 0.35 + (w + w * 0.35) * t
            ay = math.sin(t * math.pi) * h
            x, y = cx + base + ax, cy - ay
            cooling = math.sin(t * math.pi)
            if cooling < 0.35:
                col = lerp_color((1.0, 0.98, 0.92), (1.0, 0.82, 0.43), cooling / 0.35)
            elif cooling < 0.75:
                col = lerp_color((1.0, 0.82, 0.43), (1.0, 0.51, 0.16), (cooling - 0.35) / 0.4)
            else:
                col = lerp_color((1.0, 0.51, 0.16), (0.78, 0.24, 0.08), (cooling - 0.75) / 0.25)
            th = 1.6 - 0.9 * cooling
            add_glow(c, x, y, th, col, 0.9)
            prev = (x, y)
    # La mancha solar (núcleo brillante).
    add_glow(c, cx, cy, 3.2, (1.0, 0.96, 0.88), 1.0)
    add_glow(c, cx, cy, 5.5, (1.0, 0.78, 0.43), 0.7)
    add_glow(c, cx, cy, 8, (1.0, 0.51, 0.16), 0.4)
    return c


# ---------------------------------------------------------------------------
#  6 — NEBULOSA VIVA: blobs magenta + estrellas
# ---------------------------------------------------------------------------
def icon_nebula():
    c = new_canvas()
    cx, cy = 10, 14
    rng = random.Random(11)
    for b in range(6):
        t = b / 5
        size = 3.2 + 3.4 * t
        x = cx + 2 + 13 * t
        y = cy - 1 - 5 * t + rng.uniform(-1.5, 1.5)
        # Núcleo DENSO (v6.08 fix VLM: el icono se veía borroso — sube alpha).
        add_glow(c, x, y, size * 1.5, (0.38, 0.10, 0.44), 1.0)
        add_glow(c, x, y, size, (0.78, 0.22, 0.84), 1.0)
        add_glow(c, x, y, size * 0.55, (1.0, 0.55, 1.0), 0.85)
    # Filamentos MÁS BRILLANTES (la forma del ala legible).
    for k in range(14):
        t = k / 13
        x = cx + 1 + 19 * t
        y = cy - 2 - math.sin(t * math.pi * 0.9) * 5 + math.sin(t * 6 + 1) * 1.2
        add_glow(c, x, y, 1.1, (1.0, 0.62, 1.0), 0.95)
    # Estrellas GRANDES y brillantes (puntos de ancla).
    for k in range(5):
        u, v = rng.random(), rng.random()
        x = cx + 3 + u * 14
        y = cy - 7 + v * 11
        col = (0.82, 0.98, 1.0) if v > 0.55 else (1.0, 0.96, 0.88)
        add_glow(c, x, y, 1.25, col, 1.0)
        if v > 0.55:
            add_glow(c, x, y, 3.6, col, 0.35, squash=0.25)   # cruz de difracción
            add_glow(c, x, y, 3.6, col, 0.35, squash=0.25, stretch=0.25)
    # Corazón del cúmulo más denso.
    add_glow(c, cx, cy, 3.4, (1.0, 0.60, 1.0), 1.0)
    add_glow(c, cx, cy, 1.5, (1.0, 0.9, 1.0), 1.0)
    return c


# ---------------------------------------------------------------------------
#  7 — ECLIPSE TOTAL: disco negro + corona de rayos
# ---------------------------------------------------------------------------
def icon_eclipse():
    c = new_canvas()
    cx, cy = 13, 12
    R = 6.5
    # Rayos de corona desiguales.
    rng = random.Random(5)
    for r in range(6):
        ang = math.pi * (0.1 + 0.8 * r / 5)
        ln = R * (0.9 + 1.1 * rng.random())
        dx, dy = math.cos(ang), -math.sin(ang)
        x = cx + dx * (R + ln * 0.5)
        y = cy + dy * (R + ln * 0.5)
        col = lerp_color((0.92, 0.94, 1.0), (1.0, 0.88, 0.92), rng.random())
        add_glow(c, x, y, ln * 0.5, col, 0.65, squash=0.30,
                 stretch=1.0) if False else None
        # Rayo como cinta alargada (rotada): 3 blobs a lo largo.
        for k in range(4):
            t = k / 3
            px = cx + dx * (R + ln * t)
            py = cy + dy * (R + ln * t)
            th = 1.1 - 0.5 * t
            add_glow(c, px, py, th, col, 0.8 - 0.3 * t)
    # Halo cromosférico.
    add_glow(c, cx, cy, R + 1.3, (1.0, 0.84, 0.89), 0.85)
    add_glow(c, cx, cy, R + 0.4, (1.0, 0.94, 0.97), 1.0)
    # El disco negro (pinta encima, oscuro sólido).
    add_glow(c, cx, cy, R * 0.92, (0.02, 0.01, 0.04), 1.0)
    # Prominente rosa en el limbo.
    add_glow(c, cx + R * 0.55, cy - R * 0.8, 1.3, (1.0, 0.55, 0.67), 0.95)
    return c


# ---------------------------------------------------------------------------
#  8 — COMETA CARMESÍ: cabeza brillante + cola ondeante
# ---------------------------------------------------------------------------
def icon_comet():
    c = new_canvas()
    hx, hy = 9, 7
    # La cola: cinta con onda y gradiente blanco → dorado → carmesí → braza.
    prev = (hx, hy)
    for s in range(16):
        t = s / 15
        L = 17
        wave = math.sin(t * 4.5 - 0.8) * (1.2 + 3.2 * t)
        x = hx + 1 + t * L
        y = hy + 2 + math.sin(t * math.pi * 0.8) * 7 + wave * 0.6 + t * t * 3
        if t < 0.18:
            col = lerp_color((1.0, 0.98, 0.94), (1.0, 0.84, 0.47), t / 0.18)
        elif t < 0.55:
            col = lerp_color((1.0, 0.84, 0.47), (1.0, 0.47, 0.24), (t - 0.18) / 0.37)
        else:
            col = lerp_color((1.0, 0.47, 0.24), (0.59, 0.12, 0.10), (t - 0.55) / 0.45)
        th = 2.4 - 1.8 * t
        add_glow(c, x, y, th, col, 0.85 - 0.3 * t)
        prev = (x, y)
    # Motas de polvo en la punta.
    rng = random.Random(9)
    for k in range(4):
        x = 24 + rng.uniform(0, 4)
        y = 16 + rng.uniform(-3, 3)
        add_glow(c, x, y, 0.8, (1.0, 0.78, 0.59), 0.9)
    # El núcleo (cabeza) del cometa.
    add_glow(c, hx, hy, 6.0, (1.0, 0.59, 0.27), 0.5)
    add_glow(c, hx, hy, 3.4, (1.0, 0.80, 0.51), 0.8)
    add_glow(c, hx, hy, 1.8, (1.0, 0.98, 0.94), 1.0)
    return c


if __name__ == "__main__":
    canvas_to_png(icon_event_horizon(), os.path.join(OUT, "EventHorizonWings.png"))
    canvas_to_png(icon_photon_ring(), os.path.join(OUT, "PhotonRingWings.png"))
    canvas_to_png(icon_butterfly(), os.path.join(OUT, "CosmicButterflyWings.png"))
    canvas_to_png(icon_fairy(), os.path.join(OUT, "StardustFairyWings.png"))
    canvas_to_png(icon_solar_corona(), os.path.join(OUT, "SolarCoronaWings.png"))
    canvas_to_png(icon_nebula(), os.path.join(OUT, "LivingNebulaWings.png"))
    canvas_to_png(icon_eclipse(), os.path.join(OUT, "TotalEclipseWings.png"))
    canvas_to_png(icon_comet(), os.path.join(OUT, "CrimsonCometWings.png"))
    # Hoja de contactos para validación VLM.
    sheet = Image.new("RGBA", (W * 4 + 30, H * 2 + 20), (24, 24, 28, 255))
    names = ["EventHorizonWings", "PhotonRingWings", "CosmicButterflyWings", "StardustFairyWings",
             "SolarCoronaWings", "LivingNebulaWings", "TotalEclipseWings", "CrimsonCometWings"]
    for k, n in enumerate(names):
        img = Image.open(os.path.join(OUT, n + ".png"))
        sheet.alpha_composite(img, (10 + (k % 4) * (W + 6), 10 + (k // 4) * (H + 6)))
    sheet.save(os.path.join(os.path.dirname(__file__), "icons_contact_sheet.png"))
    print("hoja de contactos: icons_contact_sheet.png")
