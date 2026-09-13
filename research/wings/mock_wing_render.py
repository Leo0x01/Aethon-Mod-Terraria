#!/usr/bin/env python3
"""
mock_wing_render.py — v6.08 — SIMULACIÓN DE LOS RENDERIZADORES DE ALAS.

Replica la matemática EXACTA de los renderizadores C# (Butterfly, Fairy,
PhotonRing, EventHorizon, Comet) en Python y pinta su salida como cuadros
aditivos sobre fondo oscuro con una silueta de jugador — para verificar la
GEOMETRÍA antes de probar en juego: orientación, anclaje al hombro, formas
de los lóbulos, simetría y el barrido del sweep.
"""
from PIL import Image, ImageDraw
import math

CW, CH = 220, 160  # canvas de simulación


def add_glow(c, x, y, sx, sy, color, alpha):
    """Blob elíptico suave aditivo (SoftGlow aproximado)."""
    if alpha <= 0.01:
        return
    x0, y0 = x, y
    ia = max(0, int(x0 - sx)), min(CW, int(x0 + sx) + 1)
    ja = max(0, int(y0 - sy)), min(CH, int(y0 + sy) + 1)
    for j in range(ja[0], ja[1]):
        for i in range(ia[0], ia[1]):
            dx = (i - x0) / max(sx, 0.001)
            dy = (j - y0) / max(sy, 0.001)
            d = math.sqrt(dx * dx + dy * dy)
            if d >= 1.0:
                continue
            f = (1 - d) ** 2 * (3 - 2 * (1 - d))
            px = c[j][i]
            for k in range(3):
                px[k] = min(1.0, px[k] + color[k] * f * alpha)
            px[3] = min(1.0, px[3] + f * alpha)


def canvas_to_img(c):
    img = Image.new("RGBA", (CW, CH))
    px = img.load()
    for j in range(CH):
        for i in range(CW):
            r, g, b, a = c[j][i]
            px[i, j] = (int(r * 255), int(g * 255), int(b * 255), int(a * 255))
    return img


def draw_player_silhouette(img):
    """Silueta simple del jugador (torso+cabeza) para contexto."""
    d = ImageDraw.Draw(img)
    cx = CW // 2
    # cabeza
    d.ellipse([cx - 6, 52, cx + 6, 64], fill=(120, 120, 130, 255))
    # torso
    d.rounded_rectangle([cx - 8, 64, cx + 8, 104], radius=4, fill=(140, 90, 70, 255))
    # piernas
    d.rectangle([cx - 7, 104, cx - 2, 130], fill=(90, 60, 50, 255))
    d.rectangle([cx + 2, 104, cx + 7, 130], fill=(90, 60, 50, 255))


def lerp(c1, c2, t):
    return tuple(c1[k] + (c2[k] - c1[k]) * t for k in range(3))


PI = math.pi


# =====================================================================
#  MARIPOSA — réplica de ButterflyWings.Render
# =====================================================================
def upper_lobe_shape(u):
    theta = (-0.12 + 2.45 * u) * 0.5  # NOTE: el C# usa MathHelper.Lerp(0.12, 2.45) — mismo rango
    r = 1 - 0.18 * math.sin(u * PI)
    return (math.cos(theta) * r * (0.55 + 0.5 * math.sin(theta + 0.4)),
            math.sin(theta) * r * 0.82)


def lower_lobe_shape(u):
    theta = (-0.25 + 1.45 * u) * 0.62
    r = 1 - 0.10 * math.sin(u * PI)
    return (math.cos(theta) * r * 0.85,
            math.sin(theta) * r * 0.60 + 0.15)


def hash01(a, b, c=0):
    h = (a * 374761393 + b * 668265263 + c * 1911520717) & 0xFFFFFFFF
    h = h ^ (h >> 13)
    h = (h * 1274126177) & 0xFFFFFFFF
    h = h ^ (h >> 16)
    return (h & 0xFFFFFF) / 16777216.0


def render_butterfly(c, back, open_, flap_phase, flap_amp, time):
    stroke = math.sin(flap_phase)
    flap = (stroke ** 0.75 if stroke > 0 else stroke * 1.25) * flap_amp
    o = open_
    for side in (-1, 1):
        root = (back[0] + side * 4, back[1] - 2)
        for lobe in range(2):
            upper = lobe == 0
            flap_angle = (flap if upper else flap * 0.72) * 0.55
            fold_angle = 1.25 + (0.12 - 1.25) * o
            plane = fold_angle + flap_angle
            pc, ps = math.cos(plane), math.sin(plane)
            W = (34 if upper else 20) * (0.42 + 0.58 * o)
            H = (30 if upper else 19) * (0.42 + 0.58 * o)
            lobe_root = (root[0] + (side * 2 if upper else side * 5),
                         root[1] + (-1 if upper else 4))
            cells = 26 if upper else 12
            for cN in range(cells):
                cu = (hash01(side * 7 + lobe, cN, 1) + cN / cells) % 1.0
                cv = hash01(side * 7 + lobe, cN, 2)
                edge = upper_lobe_shape(cu) if upper else lower_lobe_shape(cu)
                inner = (edge[0] * (0.25 + 0.75 * cv), edge[1] * (0.25 + 0.75 * cv))
                local = (inner[0] * W, -abs(inner[1]) * H * 0.9 - (0 if upper else 4))
                spun = (local[0] * pc - local[1] * ps, local[0] * ps + local[1] * pc)
                depth = 1 - cv * 0.7
                mem = lerp((0.44, 0, 0.23), (0.99, 0, 0.59), 0.35 + 0.4 * cv)
                add_glow(c, lobe_root[0] + spun[0] * side, lobe_root[1] + spun[1],
                         W * 0.34, H * 0.34, mem, 0.085 * depth * (0.6 + 0.4 * o))
            # venas
            for v in range(5 if upper else 3):
                vAng = -0.15 + 1.35 * v / max(5 - 1, 1) if upper else -0.15 + 1.35 * v / 2
                for s in range(8):
                    t = s / 7
                    bend = vAng * t + (1 - t) * 0.35
                    local = (math.sin(bend) * t * W * 0.92, -math.cos(bend) * t * H * 0.88)
                    spun = (local[0] * pc - local[1] * ps, local[0] * ps + local[1] * pc)
                    vein = lerp((1.0, 0.6, 0.82), (1.0, 0.24, 0.75), t * 0.5)
                    add_glow(c, lobe_root[0] + spun[0] * side, lobe_root[1] + spun[1],
                             2.2, 2.2, vein, 0.38 * (0.5 + 0.5 * o))
            # borde dorado
            rim_steps = 22 if upper else 13
            for r in range(rim_steps + 1):
                u = r / rim_steps
                edge = upper_lobe_shape(u) if upper else lower_lobe_shape(u)
                local = (edge[0] * W, -abs(edge[1]) * H * 0.9 - (0 if upper else 4))
                spun = (local[0] * pc - local[1] * ps, local[0] * ps + local[1] * pc)
                rim = lerp((1.0, 0.80, 0.38), (1.0, 0.92, 0.67), u * 0.6)
                add_glow(c, lobe_root[0] + spun[0] * side, lobe_root[1] + spun[1],
                         1.9, 1.9, rim, 0.5 * (0.55 + 0.45 * o))
            # ojo del lóbulo superior
            if upper and o > 0.3:
                eye_local = (W * 0.62, -H * 0.48)
                eye_spun = (eye_local[0] * pc - eye_local[1] * ps,
                            eye_local[0] * ps + eye_local[1] * pc)
                eye = (lobe_root[0] + eye_spun[0] * side, lobe_root[1] + eye_spun[1])
                add_glow(c, eye[0], eye[1], 4.6, 4.6, (1.0, 0.6, 0.82), 0.5)
                add_glow(c, eye[0], eye[1], 3.4, 3.4, (0, 0, 0), 0.4)
                add_glow(c, eye[0] - 1, eye[1] - 1, 1.8, 1.8, (1, 1, 1), 0.4)
        add_glow(c, root[0], root[1], 7.5, 9.5, (1.0, 0.67, 0.31), 0.5)


# =====================================================================
#  HADA — réplica de FairyWings.Render
# =====================================================================
def render_fairy(c, back, open_, flap_phase, flap_amp, time):
    flutter = math.sin(flap_phase) * (flap_amp * 0.75 + 0.25)
    o = open_
    for side in (-1, 1):
        root = (back[0] + side * 4, back[1] - 3)
        for lobe in range(4):
            base_ang = {-1.28: 0, -0.88: 1, -0.30: 2, 0.10: 3}
            base_ang = {0: -1.28, 1: -0.88, 2: -0.30, 3: 0.10}[lobe]
            len_ = {0: 21, 1: 25, 2: 17, 3: 13}[lobe]
            wid = {0: 7.5, 1: 8.5, 2: 7.0, 3: 5.5}[lobe]
            lag = lobe * 0.55
            ang = base_ang + flutter * (0.30 + 0.06 * lobe) * math.sin(flap_phase - lag) * 1.6
            fold = 0.45 + (1 - 0.45) * o
            ang = -(0.35 * (1 - fold) + (-ang) * fold)  # -lerp(-0.35, -ang, fold)
            dirX, dirY = math.cos(ang), math.sin(ang)
            for cN in range(6):
                t = (cN + 0.5) / 6
                jw = (hash01(side * 5 + lobe, cN, 1) - 0.5) * 0.4
                profile = math.sin(t * PI) * (1 - t * 0.35)
                w = wid * (0.45 + profile + jw * 0.3) * (0.5 + 0.5 * o)
                pos = (root[0] + dirX * side * len_ * t * fold,
                       root[1] + dirY * len_ * t * fold + flutter * 1.2 * t)
                mem = lerp((1.0, 0.85, 0.55), (1.0, 0.75, 0.47), t)
                add_glow(c, pos[0], pos[1], w * 2.1, len_ / 6 * 1.5, mem,
                         0.09 * (0.5 + 0.5 * o))
            for s in range(10):
                t = s / 9
                profile = math.sin(t * PI)
                w = wid * (0.45 + profile) * (0.5 + 0.5 * o)
                axis = (dirX * side, dirY)
                perp = (-axis[1], axis[0])
                for b in (-1, 1):
                    pos = (root[0] + axis[0] * len_ * t * fold + perp[0] * w * b,
                           root[1] + axis[1] * len_ * t * fold + perp[1] * w * b + flutter * 1.2 * t)
                    rim = lerp((1.0, 0.92, 0.67), (1.0, 0.71, 0.35), t * 0.7)
                    add_glow(c, pos[0], pos[1], 1.6, 1.6, rim, 0.42 * (0.55 + 0.45 * o))
            tip = (root[0] + dirX * side * len_ * fold,
                   root[1] + dirY * len_ * fold + flutter * 1.2)
            add_glow(c, tip[0], tip[1], 3.4, 3.4, (1.0, 0.94, 0.75), 0.7)
        # destellos
        for k in range(7):
            u = hash01(side, k, 11)
            v = hash01(side, k, 12)
            lobeK = k % 4
            base_ang = {0: -1.28, 1: -0.88, 2: -0.30, 3: 0.10}[lobeK]
            lenK = {0: 21, 1: 25, 2: 17, 3: 13}[lobeK]
            fold = 0.45 + (1 - 0.45) * o
            axis = (math.cos(base_ang) * side, math.sin(base_ang))
            perp = (-axis[1], axis[0])
            pos = (root[0] + axis[0] * lenK * u * fold + perp[0] * (v - 0.5) * 14,
                   root[1] + axis[1] * lenK * u * fold + perp[1] * (v - 0.5) * 14)
            phase = time * (2.2 + 2.8 * v) + u * 37
            tw = (0.5 + 0.5 * math.sin(phase)) ** 3
            if tw > 0.05:
                col = (1.0, 0.96, 0.82) if v > 0.5 else (1.0, 0.78, 0.92)
                add_glow(c, pos[0], pos[1], 3.0, 3.0, col, tw * 0.85)
        add_glow(c, root[0], root[1], 8.5, 8.5, (1.0, 0.84, 0.59), 0.5)
        add_glow(c, root[0], root[1], 3.6, 3.6, (1, 1, 1), 0.55)


# =====================================================================
#  ANILLO DE FOTONES — réplica de BlackHoleWings.RenderPhotonRing
# =====================================================================
def render_photon_ring(c, back, open_, flap_phase, flap_amp, time):
    flap = math.sin(flap_phase) * flap_amp
    o = open_
    for side in (-1, 1):
        root = (back[0] + side * 6, back[1] + flap * 2.0)
        add_glow(c, root[0], root[1], 11, 11, (1.0, 0.24, 0.75), 0.55)
        add_glow(c, root[0], root[1], 5.5, 5.5, (1, 1, 1), 0.65)
        for h in range(3):
            h01 = h / 2
            rx = (16 + 17 * h01) * (0.50 + 0.50 * o)
            ry = rx * (0.62 - 0.10 * h01)
            center = (root[0] + side * (6 + 8 * h01) * (0.5 + 0.5 * o),
                      root[1] - (4 + 7 * h01) * (0.5 + 0.5 * o))
            tilt = (-0.42 + 0.21 * h01) * side
            hoop = lerp((1.0, 0.6, 0.82), (0.99, 0, 0.59), 0.25 + 0.45 * h01)
            # el aro: 30 puntos de la elipse girada
            ct, st = math.cos(tilt), math.sin(tilt)
            for k in range(30):
                a = 2 * PI * k / 30
                lx, ly = math.cos(a) * rx, math.sin(a) * ry
                x = center[0] + lx * ct - ly * st
                y = center[1] + lx * st + ly * ct
                add_glow(c, x, y, 1.6, 1.6, hoop, 0.34 + 0.30 * (1 - h01 * 0.5))
            # fotones
            speed = 3.0 + 2.2 * h01 + flap_amp * 2.0
            for i in range(2):
                ang = time * speed + i * PI + h * 1.9 + side * 0.8
                lx, ly = math.cos(ang) * rx, math.sin(ang) * ry
                x = center[0] + lx * ct - ly * st
                y = center[1] + lx * st + ly * ct
                add_glow(c, x, y, 3.6, 3.6, (1, 1, 1), 0.8)
                add_glow(c, x, y, 7.5, 7.5, (1.0, 0.24, 0.75), 0.45)
                for kk in (1, 2):
                    aT = ang - kk * 0.22
                    lx, ly = math.cos(aT) * rx, math.sin(aT) * ry
                    xt = center[0] + lx * ct - ly * st
                    yt = center[1] + lx * st + ly * ct
                    add_glow(c, xt, yt, 2.8 / kk, 2.8 / kk, (1.0, 0.6, 0.82), 0.30 / kk)


# =====================================================================
#  HORIZONTE DE SUCESOS — réplica de RenderEventHorizon (solo la cinta)
# =====================================================================
def render_event_horizon(c, back, open_, flap_phase, flap_amp, time, speed_x=0):
    flap = math.sin(flap_phase) * flap_amp
    o = open_
    sweep = min(abs(speed_x) / 9, 1) * 0.55
    for side in (-1, 1):
        root = (back[0] + side * 7, back[1] + flap * 2.2)
        span = (46 + 14 * o) * (0.55 + 0.45 * o)
        lift = (34 + 12 * o) * (0.50 + 0.50 * o) * (1 + 0.10 * flap)
        stretch = 1 + sweep * 0.5 + 0.06 * flap
        prev = root
        for s in range(17):
            t = s / 16
            arcX = 1 - (1 - t) ** 1.7
            arcY = math.sin(t * PI * 0.88) * lift - t * t * lift * 0.22
            pos = (root[0] + side * arcX * span * stretch,
                   root[1] - arcY)
            th = (4.2 - 3.3 * t) * (0.65 + 0.35 * o) + 0.9
            col = lerp((1.0, 0.24, 0.75), (0.44, 0, 0.23), t * 0.85)
            dx, dy = pos[0] - prev[0], pos[1] - prev[1]
            rot = math.atan2(dy, dx) if (dx * dx + dy * dy) > 1e-4 else 0
            # cinta: elipse alargada rotada (aprox: dos blobs por segmento)
            for q in range(2):
                f = (q - 0.5) * 0.5
                px = pos[0] + math.cos(rot) * f * th * 2.2
                py = pos[1] + math.sin(rot) * f * th * 2.2
                add_glow(c, px, py, th * 1.8, th * 0.8, col,
                         0.50 * (0.5 + 0.5 * (1 - t * 0.6)))
            prev = pos
        # mini horizonte
        add_glow(c, root[0], root[1], 10, 10, (1.0, 0.6, 0.82), 0.6)
        add_glow(c, root[0], root[1], 6.5, 6.5, (0, 0, 0), 0.95)
        add_glow(c, root[0] - 3.5, root[1] - 4.5, 3.4, 3.4, (1, 1, 1), 0.85)


# =====================================================================
#  COMETA — réplica de CometWings.Render (la cola)
# =====================================================================
def render_comet(c, back, open_, flap_phase, flap_amp, time, speed_x=0, direction=1):
    flap = math.sin(flap_phase) * flap_amp
    o = open_
    speed01 = min(abs(speed_x) / 9, 1)
    sweep = speed01 * 0.9
    for side in (-1, 1):
        head = (back[0] + side * 7, back[1] - 3 + flap * 1.8)
        add_glow(c, head[0], head[1], 30, 30, (1.0, 0.59, 0.27), 0.30)
        add_glow(c, head[0], head[1], 16, 16, (1.0, 0.80, 0.51), 0.40)
        add_glow(c, head[0], head[1], 7.5, 7.5, (1.0, 0.98, 0.94), 0.95)
        prev = head
        for s in range(19):
            t = s / 18
            L = (38 + 22 * o) * (0.55 + 0.45 * o) * (1 + sweep * 0.65 + 0.06 * flap)
            waveP = time * 3.2 - t * 4.5
            wave = math.sin(waveP) * (2.0 + 7.5 * t) * (0.35 + 0.65 * flap_amp)
            curveX = t * L * (0.85 if side == direction else 1.0)
            curveY = math.sin(t * PI * 0.75) * (26 * (0.45 + 0.55 * o)) * (1 - sweep * 0.55) \
                     - t * t * 10 * (1 - sweep * 0.4)
            curveX = side * max(abs(curveX), 6 + 30 * t)
            pos = (head[0] + curveX * (1 - sweep * 0.25) + wave * 0.35 * side,
                   head[1] - curveY + wave + flap * 1.8 * (1 - t * 0.5))
            th = (5.2 - 4.4 * t) * (0.55 + 0.45 * o) + 0.8
            if t < 0.18:
                col = lerp((1.0, 0.98, 0.94), (1.0, 0.84, 0.47), t / 0.18)
            elif t < 0.55:
                col = lerp((1.0, 0.84, 0.47), (1.0, 0.47, 0.24), (t - 0.18) / 0.37)
            else:
                col = lerp((1.0, 0.47, 0.24), (0.59, 0.12, 0.10), (t - 0.55) / 0.45)
            dx, dy = pos[0] - prev[0], pos[1] - prev[1]
            rot = math.atan2(dy, dx) if (dx * dx + dy * dy) > 1e-4 else 0
            add_glow(c, pos[0], pos[1], th * 2.4, th * 1.1, col,
                     0.50 * (0.45 + 0.55 * o) * (1 - t * 0.25))
            prev = pos


def scene(renderer, open_, phase, amp, time, speed=0, direction=1, label=""):
    c = [[[0.0, 0.0, 0.0, 0.0] for _ in range(CW)] for _ in range(CH)]
    # fondo estrellado tenue
    for k in range(40):
        x = (hash01(k, 1, 3) * CW) % CW
        y = (hash01(k, 2, 3) * CH) % CH
        add_glow(c, x, y, 0.8, 0.8, (0.5, 0.5, 0.6), 0.5)
    back = (CW / 2, 82 - 6)  # hombros del jugador (Center - 6px)
    renderer(c, back, open_, phase, amp, time, speed, direction) \
        if renderer.__code__.co_argcount >= 7 else renderer(c, back, open_, phase, amp, time)
    img = canvas_to_img(c)
    draw_player_silhouette(img)
    d = ImageDraw.Draw(img)
    d.text((4, 2), label, fill=(255, 255, 255, 255))
    return img


if __name__ == "__main__":
    # HOJA: cada estilo en 3 estados (volando medio, reposo, barrido)
    rows = []
    specs = [
        ("MARIPOSA volando", lambda c, b, o, p, a, t: render_butterfly(c, b, 1.0, p, a, t), 1.0, 1.2, 1.0, 1.3),
        ("MARIPOSA reposo", lambda c, b, o, p, a, t: render_butterfly(c, b, 0.3, 0, 0, t), 0.3, 0, 0, 1.3),
        ("MARIPOSA bajada", lambda c, b, o, p, a, t: render_butterfly(c, b, 1.0, 1.9, 1.0, t), 1.0, 1.9, 1.0, 1.3),
        ("HADA volando", lambda c, b, o, p, a, t: render_fairy(c, b, 1.0, p, a, t), 1.0, 0.8, 1.0, 2.0),
        ("HADA reposo", lambda c, b, o, p, a, t: render_fairy(c, b, 0.42, 1.0, 0.22, t), 0.42, 1.0, 0.22, 2.0),
        ("FOTONES volando", lambda c, b, o, p, a, t: render_photon_ring(c, b, 1.0, p, a, t), 1.0, 0.5, 1.0, 1.0),
        ("FOTONES reposo", lambda c, b, o, p, a, t: render_photon_ring(c, b, 0.22, 0, 0, t), 0.22, 0, 0, 1.0),
        ("HORIZONTE volando", lambda c, b, o, p, a, t: render_event_horizon(c, b, 1.0, p, a, t, 3), 1.0, 0.5, 1.0, 1.3),
        ("HORIZONTE reposo", lambda c, b, o, p, a, t: render_event_horizon(c, b, 0.18, 0, 0, t), 0.18, 0, 0, 1.3),
        ("COMETA veloz", lambda c, b, o, p, a, t: render_comet(c, b, 1.0, p, a, t, 8, 1), 1.0, 0.5, 1.0, 1.5),
        ("COMETA reposo", lambda c, b, o, p, a, t: render_comet(c, b, 0.25, 0.4, 0.1, t, 0, 1), 0.25, 0.4, 0.1, 1.5),
    ]
    for label, fn, o, p, a, t in specs:
        c = [[[0.0, 0.0, 0.0, 0.0] for _ in range(CW)] for _ in range(CH)]
        for k in range(40):
            x = (hash01(k, 1, 3) * CW) % CW
            y = (hash01(k, 2, 3) * CH) % CH
            add_glow(c, x, y, 0.8, 0.8, (0.5, 0.5, 0.6), 0.5)
        back = (CW / 2, 82 - 6)
        fn(c, back, o, p, a, t)
        img = canvas_to_img(c)
        draw_player_silhouette(img)
        d = ImageDraw.Draw(img)
        d.text((4, 2), label, fill=(255, 255, 255, 255))
        rows.append(img)

    # composición 3 columnas
    cols = 3
    sheet = Image.new("RGBA", (CW * cols + 8 * (cols + 1), (CH + 10) * ((len(rows) + 2) // 3) + 8), (12, 12, 16, 255))
    for k, im in enumerate(rows):
        sheet.alpha_composite(im, (8 + (k % 3) * (CW + 8), 8 + (k // 3) * (CH + 10)))
    sheet.save("mock_wings_preview.png")
    print("mock_wings_preview.png —", len(rows), "escenas")
