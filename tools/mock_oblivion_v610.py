#!/usr/bin/env python3
"""
mock_oblivion_v610.py — v2 — EL AGUJERO NEGRO CARMESÍ v6.10 "OBLIVION"

Geometría corregida tras feedback VLM (3/10 → objetivo 9/10):
  · UNA HOJA GRUESA EN CRESCIENTE (guadaña): base MUY ancha al oeste
    (~1R de grosor), barre en sentido HORARIO por encima (W→NW→N→NE)
    y se estira hacia el E-NE hasta 6.4R terminando en PUNTA DE AGUJA.
    Hotspot blanco-amarillo a mitad de camino (due E ~3-4R).
  · BRAZO INFERIOR secundario: base SE, barre por debajo hacia el SW,
    más fino y tenue.
  · Anillo interior 360° a 1.3-2.2R (parcialmente tapado por la hoja).
  · Esfera negra compacta + gap + rayos azul-violeta dentro.
  · Trazos de pincel (filamentos brillantes DENTRO de la hoja) +
    jitter en el borde (aristas eléctricas).
  · Rotación HORARIA lenta (la dirección de flujo medida).
"""
import numpy as np
from PIL import Image
import math

W, H = 660, 540
CX, CY = 250, 255
R = 46.0
SQ = 0.88
TILT = -0.42          # inclinación global SW→NE (medida en la referencia)

def pol(rr, theta):
    """Posición de pantalla de un punto polar (rr en unidades de R),
    con el aplastado SQ y la INCLINACIÓN global aplicados."""
    x = math.cos(theta) * rr * R
    y = math.sin(theta) * rr * R * SQ
    ca, sa = math.cos(TILT), math.sin(TILT)
    return CX + x * ca - y * sa, CY + x * sa + y * ca

def smooth(points, t):
    pts = sorted(points)
    if t <= pts[0][0]: return pts[0][1]
    if t >= pts[-1][0]: return pts[-1][1]
    for i in range(len(pts) - 1):
        if pts[i][0] <= t <= pts[i + 1][0]:
            t0, v0 = pts[i]; t1, v1 = pts[i + 1]
            u = (t - t0) / (t1 - t0)
            u = u * u * (3 - 2 * u)
            return v0 + (v1 - v0) * u
    return pts[-1][1]

def lerp_color(c0, c1, u):
    return tuple(c0[k] + (c1[k] - c0[k]) * u for k in range(3))

def color_ramp(t, keys):
    for i in range(len(keys) - 1):
        if keys[i][0] <= t <= keys[i + 1][0]:
            u = (t - keys[i][0]) / (keys[i + 1][0] - keys[i][0])
            return lerp_color(keys[i][1], keys[i + 1][1], u)
    return keys[-1][1]

def hash01(a, b):
    h = (a * 374761393 + b * 668265263) & 0xFFFFFFFF
    h ^= h >> 13
    h = (h * 1274126177) & 0xFFFFFFFF
    h ^= h >> 16
    return (h & 0xFFFFFF) / 16777216.0

# ---------------- geometría de la HOJA SUPERIOR (por ARRIBA) --------------
BLADE = dict(
    th_keys=[(0.00, 175.0), (1.00, 352.0)],         # W → NW → N → NNE (horario)
    ro_keys=[(0.00, 2.30), (0.25, 3.30), (0.45, 3.00),
             (0.65, 3.30), (0.80, 3.20), (0.90, 4.20), (1.00, 5.90)],
    tk_keys=[(0.00, 1.00), (0.30, 0.80), (0.55, 0.55),
             (0.80, 0.32), (1.00, 0.07)],
    bri_keys=[(0.00, 0.60), (0.30, 0.80), (0.72, 1.00),
              (0.86, 0.78), (1.00, 0.52)],
    col_keys=[(0.00, (255, 42, 122)), (0.30, (255, 80, 160)),
              (0.55, (255, 150, 205)), (0.72, (255, 250, 155)),
              (0.84, (255, 110, 200)), (0.93, (222, 38, 98)),
              (1.00, (145, 18, 48))],
)

# ---------------- geometría del BRAZO INFERIOR (cresiente grande) --------
LOWER = dict(
    th_keys=[(0.00, 20.0), (1.00, 205.0)],          # ESE → S → SSW (horario)
    ro_keys=[(0.00, 1.95), (0.25, 3.20), (0.50, 6.30),
             (0.65, 6.00), (0.80, 4.80), (1.00, 2.60)],
    tk_keys=[(0.00, 0.65), (0.35, 0.50), (0.60, 0.35),
             (0.85, 0.18), (1.00, 0.08)],
    bri_keys=[(0.00, 0.60), (0.30, 0.78), (0.50, 0.82),
              (0.75, 0.50), (1.00, 0.22)],
    col_keys=[(0.00, (255, 60, 140)), (0.30, (255, 82, 172)),
              (0.50, (250, 45, 125)), (0.75, (222, 36, 100)),
              (1.00, (140, 18, 55))],
)

def blade_center(blade, t, rot):
    """Centro de la sección de la hoja en t (con tilt global)."""
    ro = smooth(blade['ro_keys'], t)
    tk = smooth(blade['tk_keys'], t)
    rm = ro - tk * 0.5
    th = math.radians(smooth(blade['th_keys'], t)) + rot
    x, y = pol(rm, th)
    return x, y, th, rm, tk, ro

def render(time=0.0, rot=0.0, seed=7):
    acc = np.zeros((H, W, 3), dtype=np.float64)

    def add_blob(x, y, sx, sy, rot_a, color, alpha):
        if alpha <= 0.004: return
        if sx < 1 or sy < 1: return
        ca, sa = math.cos(rot_a), math.sin(rot_a)
        yy, xx = np.mgrid[0:H, 0:W]
        dx, dy = xx - x, yy - y
        rx = dx * ca + dy * sa
        ry = -dx * sa + dy * ca
        d2 = (rx / sx) ** 2 + (ry / sy) ** 2
        core = np.exp(-d2 * 3.6)
        halo = np.exp(-d2 * 1.2) * 0.40
        g = (core + halo) * alpha
        acc[:, :, 0] += color[0] * g
        acc[:, :, 1] += color[1] * g
        acc[:, :, 2] += color[2] * g

    rng = np.random.default_rng(seed)

    # ============ 1. HALO AMBIENTE (tenue, cálido, inclinado) ============
    add_blob(CX, CY, 5.6 * R, 3.7 * R * SQ, TILT, (125, 18, 55), 0.13)
    add_blob(CX, CY, 3.3 * R, 2.3 * R * SQ, TILT, (185, 36, 85), 0.12)

    # ============ 2. ANILLO INTERIOR 360° (con borde interno CALIENTE) ============
    n_ring = 48
    for i in range(n_ring):
        phi = i * 2 * math.pi / n_ring
        rr = 1.48
        x, y = pol(rr, phi)
        rot_a = math.atan2(math.cos(phi) * SQ, -math.sin(phi)) + TILT
        ang = math.degrees(phi) % 360
        # vivo bajo el paso de la hoja (W-N-E), tenue abajo
        merge = 0.5 + 0.5 * math.cos(math.radians(ang - 95 + math.degrees(rot)))
        brillo = 0.46 + 0.40 * max(0.0, merge)
        flick = 0.86 + 0.14 * math.sin(9 * phi + time * 6.0 + i * 2.3)
        col = (255, 66, 142) if math.sin(phi) < 0 else (255, 40, 108)
        add_blob(x, y, 0.52 * R, 0.26 * R, rot_a, col, brillo * flick * 0.72)
        # borde interno BLANCO-CALIENTE (el rim que abraza el gap)
        xi, yi = pol(rr - 0.22, phi)
        hot = 0.38 + 0.50 * max(0.0, merge)
        add_blob(xi, yi, 0.40 * R, 0.16 * R, rot_a, (255, 238, 198), hot * flick * 0.45)

    # ============ 3. LAS HOJAS (cuerpo + filamentos + jitter) ============
    def draw_blade(blade, n_seg, filament_boost, jitter):
        for i in range(n_seg):
            t = i / (n_seg - 1)
            # jitter determinista del borde (aristas eléctricas)
            jr = (hash01(i * 7 + 1, 13) - 0.5) * jitter
            x, y, th, rm, tk, ro = blade_center(blade, t, rot)
            rm += jr * 0.5
            x, y = pol(rm, th)
            # tangente
            eps = 1.5 / n_seg
            x2, y2, th2, rm2, tk2, _ = blade_center(blade, min(t + eps, 1), rot)
            rot_a = math.atan2(y2 - y, x2 - x)
            seg_len = max(math.hypot(x2 - x, y2 - y) * 1.55, 3.0)
            col = color_ramp(t, blade['col_keys'])
            bri = smooth(blade['bri_keys'], t)
            # trazos de pincel: ruido de brillo alargado
            stroke = 0.70 + 0.30 * math.sin(21 * t + time * 3.1 + math.sin(7.7 * t) * 2.0)
            stroke *= 0.90 + 0.10 * hash01(i * 31 + 5, 77)
            al = min(2.1, bri * stroke * 1.3)
            add_blob(x, y, seg_len, tk * R * 0.50, rot_a, col, al)

            # filamentos brillantes DENTRO de la hoja (pinceladas calientes)
            for f in range(3):
                off = (f - 1) * 0.30 * tk
                fx, fy = pol(rm + off, th)
                fcol = lerp_color(col, (255, 255, 235), 0.60 if f == 1 else 0.35)
                fal = al * filament_boost * (0.55 if f == 1 else 0.34)
                add_blob(fx, fy, seg_len * 0.92, max(1.6, tk * R * 0.14), rot_a, fcol, fal)

            # COLAS DE VELOCIDAD: mechones que se desprenden del borde
            # exterior en el sentido del giro (solo el tramo externo)
            if 0.5 < t < 0.95 and hash01(i * 13 + 3, 91) > 0.62:
                trail_r = ro + 0.18 + 0.5 * hash01(i, 55)
                trail_th = th + 0.10
                for s in range(3):
                    ur = trail_r + s * 0.55
                    uth = trail_th + s * 0.16
                    ux, uy = pol(ur, uth)
                    ucol = lerp_color(col, (140, 25, 70), 0.5 + 0.4 * s / 2)
                    add_blob(ux, uy, 0.5 * R * (1 - s * 0.25), 0.11 * R,
                             uth + TILT, ucol, 0.45 * (1 - s * 0.3) * bri)

    draw_blade(BLADE, 112, 1.0, 0.30)
    draw_blade(LOWER, 72, 0.65, 0.22)

    # HOTSPOT de la hoja (t=0.72: blanco-amarillo cegador en NNE)
    hx, hy, hth, hrm, _, _ = blade_center(BLADE, 0.72, rot)
    add_blob(hx, hy, 1.5 * R, 0.85 * R, hth, (255, 240, 168), 0.85)
    add_blob(hx, hy, 0.65 * R, 0.40 * R, hth, (255, 253, 232), 1.10)
    # nudo caliente NNE a ~3R (medido: (255,246,137) a 355°, 3R)
    kx, ky = pol(3.0, math.radians(355))
    add_blob(kx, ky, 0.55 * R, 0.34 * R, math.radians(355), (255, 246, 150), 0.55)

    # punta de la hoja: chispa de aguja + mechones lejanos hasta 6.5R
    tx, ty, tth, trm, _, _ = blade_center(BLADE, 0.97, rot)
    add_blob(tx, ty, 0.4 * R, 0.14 * R, tth, (255, 210, 170), 0.5)
    for wI in range(3):
        wth = math.radians(352 + wI * 9) + rot
        for s in range(3):
            wr = 5.9 + s * 0.35 + 0.2 * hash01(wI, s)
            wx, wy = pol(wr, wth)
            add_blob(wx, wy, 0.45 * R * (1 - s * 0.2), 0.10 * R, wth,
                     lerp_color((200, 45, 95), (110, 18, 48), s / 2), 0.50 - s * 0.12)

    # ============ 4. LA ESFERA NEGRA ============
    yy, xx = np.mgrid[0:H, 0:W]
    d2 = (xx - CX) ** 2 + (yy - CY) ** 2
    sphere = d2 <= (R * 0.985) ** 2
    acc[sphere] = 0.0

    # ============ 5. RAYOS AZUL-VIOLETA DENTRO (ramificados, NO centrados) ============
    for bi in range(3):
        if (int(time * 7) + bi * 3) % 5 >= 2:
            continue
        th0 = math.radians(-60 + bi * 130 + (int(time * 7) * 47) % 360)
        # los rayos viven en un SEMIPLANO (cuadrante), no cruzan el centro
        x0, y0 = CX + math.cos(th0) * R * 0.92, CY + math.sin(th0) * R * 0.92
        x1, y1 = CX + math.cos(th0 + 1.9) * R * 0.45, CY + math.sin(th0 + 1.9) * R * 0.45
        px, py = x0, y0
        for s in range(1, 6):
            u = s / 5
            nx = x0 + (x1 - x0) * u + (rng.random() - 0.5) * R * 0.40 * (1 - u)
            ny = y0 + (y1 - y0) * u + (rng.random() - 0.5) * R * 0.40 * (1 - u)
            mx, my = (px + nx) / 2, (py + ny) / 2
            ra = math.atan2(ny - py, nx - px)
            ln = math.hypot(nx - px, ny - py) * 0.8
            add_blob(mx, my, ln, 1.3, ra, (150, 170, 255), 0.30)
            # rama corta que se bifurca
            if s in (2, 4) and rng.random() > 0.4:
                bra = ra + (rng.random() - 0.5) * 1.6
                bx, by = mx + math.cos(bra) * R * 0.20, my + math.sin(bra) * R * 0.20
                add_blob((mx + bx) / 2, (my + by) / 2, R * 0.12, 1.3, bra,
                         (170, 185, 255), 0.32)
            px, py = nx, ny

    # ============ 6. CHISPAS ============
    spark_data = [(0.72, 3.6), (0.55, 2.7), (0.82, 4.6), (0.30, 2.3),
                  (0.62, 3.4), (0.90, 5.2), (0.12, 2.0)]
    for k, (ts, rs) in enumerate(spark_data):
        tw = 0.5 + 0.5 * math.sin(time * (5 + k * 1.7) + k * 2.9)
        if tw < 0.55: continue
        th = math.radians(smooth(BLADE['th_keys'], ts)) + rot
        x, y = pol(rs, th)
        add_blob(x, y, 2.6, 2.6, 0, (255, 252, 222), 1.0 * tw)
        add_blob(x, y, 5.5, 5.5, 0, (255, 238, 165), 0.34 * tw)

    # ============ 7. BLOOM ============
    add_blob(hx, hy, 2.8 * R, 1.8 * R, hth, (255, 195, 135), 0.20)
    add_blob(CX, CY, 3.5 * R, 2.6 * R * SQ, TILT, (255, 115, 185), 0.09)

    img = np.clip(acc, 0, 255).astype(np.uint8)
    return Image.fromarray(img)

if __name__ == "__main__":
    import sys
    t = float(sys.argv[1]) if len(sys.argv) > 1 else 0.0
    out = sys.argv[2] if len(sys.argv) > 2 else "oblivion_render.png"
    render(time=t).save(out)
    print(f"guardado {out}")
