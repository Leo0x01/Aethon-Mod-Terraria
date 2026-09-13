#!/usr/bin/env python3
"""
mock_blackhole_v609.py — Prototipo del NUEVO renderizador del agujero negro
carmesí (v6.09) según la referencia del usuario.

Simula EXACTAMENTE el algoritmo que se porta a C# (CrimsonBlackHoleRenderer):
las mismas capas, las mismas fórmulas de BlackHolePhysics y las MISMAS
texturas del mod (SoftGlow, Ring, GlowRay, BlackDisk) con muestreo BILINEAR
y supersampling (como el LinearClamp del SpriteBatch real).

Estructura calibrada con mediciones píxel-exactas de la referencia:
  · Esfera negra: R_sh, negro profundo
  · Anillo de fotones: 1.26·R_sh (fijo, blanco) + eco lensado a 1.75·R_sh
  · Halo de fotones recortado por la esfera (bloom pegado a la silueta)
  · Disco: borde interno CALIENTE 2.2·R_sh SOLO en el lado cercano (cruza
    por delante de la esfera a +0.76·R_sh bajo el centro); el lado lejano
    nace a 2.7·R_sh → "foso" oscuro sobre el eje mayor (como la referencia);
    cuerpo hasta el PICO a 4.6·R_sh y fade a 6.6·R_sh
  · Elipse b/a = 0.345; el arco superior atenuado; Doppler izq. +; Kepler
"""
import math
import numpy as np
from PIL import Image

BASE = "/home/z/my-project/AethonMod/AethonMod/Content/Effects"

# ==================== PARÁMETROS (idénticos al C#, en ×R_sh) ====================
R_SH = 38.0          # radio de la sombra en px (escala 1)
RATIO = 0.345        # achatado de la elipse del disco (b/a)
R_RIM = 2.20         # borde interno caliente — SOLO lado cercano (×R_sh)
R_IN = 2.55          # inicio del cuerpo del disco (×R_sh)
R_FAR_IN = 2.70      # borde interno del lado LEJANO (×R_sh) → foso en el eje
R_OUT = 6.50         # fade exterior (×R_sh)
R_PEAK = 4.60        # radio del brillo máximo (×R_sh)
RINGS = 32           # líneas de corriente del disco
CAP_W = 0.68         # grosor de cápsula (×R_sh)
SEG_LEN = 0.68       # paso angular objetivo (×R_sh de arco)
FAR_TOP = 0.58       # atenuación SOLO del arco superior (lejano)
DOPPLER = 0.30       # contraste Doppler (izq +, der −)
TIME = 2.0           # instante simulado

RING1_R, RING1_A = 1.28, 255   # anillo de fotones principal (fijo a la sombra)
RING2_R, RING2_A = 1.78, 235   # eco lensado (imagen secundaria)
RING3_R, RING3_A = 2.05, 90    # tercer susurro de eco

# ==================== TEXTURAS REALES ====================
tex_softglow = np.asarray(Image.open(f"{BASE}/Procedural/SoftGlow.png")).astype(np.float32) / 255.0
tex_ring = np.asarray(Image.open(f"{BASE}/Procedural/Ring.png")).astype(np.float32) / 255.0
tex_ray = np.asarray(Image.open(f"{BASE}/GlowRay.png")).astype(np.float32) / 255.0
tex_disk = np.asarray(Image.open(f"{BASE}/Procedural/BlackDisk.png")).astype(np.float32) / 255.0


def hash01(seed, a, b):
    h = (seed * 374761393 + a * 668265263 + b * 1911520717) & 0xFFFFFFFF
    if h >= 2**31: h -= 2**32
    h ^= (h >> 13)
    h = (h * 1274126177) & 0xFFFFFFFF
    if h >= 2**31: h -= 2**32
    h ^= (h >> 16)
    return (h & 0xFFFFFF) / 16777216.0


def smoothstep(e0, e1, x):
    t = min(1.0, max(0.0, (x - e0) / (e1 - e0)))
    return t * t * (3 - 2 * t)


def disk_brightness(u):
    """Perfil radial del disco: borde caliente → carril → pico → fade."""
    if u < R_RIM or u > R_OUT:
        return 0.0
    if u < R_IN:      # borde interno caliente
        return 0.80
    pts = [(R_IN, 0.30), (3.2, 0.55), (3.8, 0.80), (R_PEAK, 1.0),
           (5.3, 0.82), (5.5, 0.52), (5.9, 0.22), (R_OUT, 0.08)]
    for i in range(len(pts) - 1):
        if pts[i][0] <= u <= pts[i + 1][0]:
            t = (u - pts[i][0]) / (pts[i + 1][0] - pts[i][0])
            return pts[i][1] + t * (pts[i + 1][1] - pts[i][1])
    return 0.0


def disk_color(u):
    """Rampa cromática radial NEÓN: borde blanco-rosado → carmesí vivo →
    fucsia → MAGENTA (pico) → fucsia → carmesí apagado."""
    pts = [
        (R_RIM, (255, 214, 240)),
        (2.55, (255, 42, 122)),
        (3.20, (255, 66, 168)),
        (3.80, (255, 88, 232)),
        (R_PEAK, (255, 105, 255)),
        (5.30, (255, 78, 212)),
        (5.90, (224, 32, 84)),
        (R_OUT, (142, 22, 54)),
    ]
    for i in range(len(pts) - 1):
        if pts[i][0] <= u <= pts[i + 1][0]:
            t = (u - pts[i][0]) / (pts[i + 1][0] - pts[i][0])
            c0, c1 = pts[i][1], pts[i + 1][1]
            return tuple(c0[k] + t * (c1[k] - c0[k]) for k in range(3))
    return pts[-1][1] if u > R_OUT else pts[0][1]


class Canvas:
    """Lienzo float con blending aditivo/alfa y muestreo BILINEAR."""

    def __init__(self, w, h, bg=(16, 20, 49)):
        self.w, self.h = w, h
        self.buf = np.zeros((h, w, 3), dtype=np.float32)
        self.buf[:] = bg

    def _sample(self, tex, cx, cy, size_x, size_y, rot):
        th, tw = tex.shape[:2]
        sx, sy = size_x / tw, size_y / th
        cosr, sinr = math.cos(rot), math.sin(rot)
        ex, ey = abs(size_x * cosr) + abs(size_y * sinr), abs(size_x * sinr) + abs(size_y * cosr)
        x0, x1 = int(cx - ex / 2 - 1), int(cx + ex / 2 + 2)
        y0, y1 = int(cy - ey / 2 - 1), int(cy + ey / 2 + 2)
        x0, y0 = max(x0, 0), max(y0, 0)
        x1, y1 = min(x1, self.w), min(y1, self.h)
        if x0 >= x1 or y0 >= y1:
            return None
        ys, xs = np.mgrid[y0:y1, x0:x1]
        dx, dy = xs - cx, ys - cy
        lx = (dx * cosr + dy * sinr) / sx + tw / 2 - 0.5
        ly = (-dx * sinr + dy * cosr) / sy + th / 2 - 0.5
        # bilinear
        fx, fy = np.floor(lx), np.floor(ly)
        tx, ty = lx - fx, ly - fy
        fx, fy = fx.astype(np.int32), fy.astype(np.int32)
        fx1, fy1 = np.clip(fx + 1, 0, tw - 1), np.clip(fy + 1, 0, th - 1)
        fx, fy = np.clip(fx, 0, tw - 1), np.clip(fy, 0, th - 1)
        a00 = tex[fy, fx, 3]; a10 = tex[fy, fx1, 3]
        a01 = tex[fy1, fx, 3]; a11 = tex[fy1, fx1, 3]
        a = (a00 * (1 - tx) * (1 - ty) + a10 * tx * (1 - ty) +
             a01 * (1 - tx) * ty + a11 * tx * ty)
        inb = (lx >= -0.5) & (lx < tw - 0.5) & (ly >= -0.5) & (ly < th - 0.5)
        return (y0, y1, x0, x1, a, inb)

    def add_quad(self, tex, cx, cy, size_x, size_y, rot, color, alpha):
        s = self._sample(tex, cx, cy, size_x, size_y, rot)
        if s is None:
            return
        y0, y1, x0, x1, ta, inb = s
        a = ta * alpha
        for k in range(3):
            self.buf[y0:y1, x0:x1, k] += a * color[k]

    def alpha_quad(self, tex, cx, cy, size_x, size_y, rot, color, alpha):
        s = self._sample(tex, cx, cy, size_x, size_y, rot)
        if s is None:
            return
        y0, y1, x0, x1, ta, inb = s
        a = np.where(inb, ta * alpha, 0.0)
        dst = self.buf[y0:y1, x0:x1]
        for k in range(3):
            dst[:, :, k] = dst[:, :, k] * (1 - a) + color[k] * a

    def to_image(self):
        return Image.fromarray(np.clip(self.buf, 0, 255).astype(np.uint8))


def side_factor(sphi, u):
    """El arco superior (lejano) atenúa SOLO su región interna (u<5):
    el PICO del arco lejano brilla sobre la esfera como en la referencia;
    los extremos horizontales y el lado cercano quedan a pleno brillo."""
    if sphi >= 0:
        return 1.0
    far_dim = 0.55 + 0.45 * smoothstep(2.7, 5.0, u)
    topness = smoothstep(0.18, 0.85, -sphi)
    return 1.0 - (1.0 - far_dim) * topness


def render(time=TIME, r_sh=R_SH, seed=7, show_rays=True, stars=True):
    W, H = int(24.7 * r_sh), int(12.35 * r_sh)
    cv = Canvas(W, H)
    cx, cy = W / 2, H / 2
    cap_w = CAP_W * r_sh

    # ---------- 1. FONDO: halo ambiental + rayos cian ----------
    cv.add_quad(tex_softglow, cx, cy, 7.1 * r_sh, 7.1 * r_sh * RATIO * 1.6, 0,
                (110, 15, 48), 0.16)
    cv.add_quad(tex_softglow, cx, cy, 3.6 * r_sh, 3.6 * r_sh, 0, (145, 28, 64), 0.18)
    if show_rays:
        for i in range(18):
            ang = hash01(seed, i, 3) * math.tau + 0.10 * math.sin(time * 0.31 + i * 2.1)
            r0 = 2.1 * r_sh                                  # nacen fuera de la esfera
            r1 = (8.5 + 3.5 * hash01(seed, i, 9)) * r_sh     # cruzan el fondo entero
            mid = (r0 + r1) * 0.5
            ln = (r1 - r0) * 2.0        # el centro brillante cae a media longitud
            cv.add_quad(tex_ray, cx + math.cos(ang) * mid, cy + math.sin(ang) * mid,
                        ln, 0.18 * r_sh, ang, (58, 170, 255), 0.50)

    # ---------- DISCO — cápsulas por línea de corriente ----------
    def disk_pass(near_side):
        for ri in range(RINGS):
            u = R_RIM + (R_OUT - R_RIM) * ri / (RINGS - 1)
            r = u * r_sh
            b = disk_brightness(u)
            if b <= 0.01:
                continue
            # el lado LEJANO nace más lejos: el foso del eje mayor (referencia)
            if not near_side and u < R_FAR_IN:
                continue
            col = disk_color(u)
            omega = 1.5 * (R_PEAK / u) ** 1.5       # Kepler: ω ∝ r^-3/2
            n_seg = max(48, min(120, int(math.tau * r / (SEG_LEN * r_sh))))
            step = math.tau / n_seg
            grid_shift = hash01(seed, ri, 77) * step   # desalinea costuras
            cap_len = step * r * 2.2               # solapado → banda DENSA
            cap_w_side = cap_w * (0.72 if not near_side else 1.0)  # arco lejano COMPRIMIDO
            for si in range(n_seg):
                phi = si * step + grid_shift
                sphi = math.sin(phi)
                if near_side and sphi <= 0:
                    continue
                if (not near_side) and sphi > 0:
                    continue
                px = cx + r * math.cos(phi)
                py = cy + r * sphi * RATIO
                tx, ty = -math.sin(phi), RATIO * math.cos(phi)
                rot = math.atan2(ty, tx)
                # grano fino del plasma
                flick = 0.92 + 0.08 * math.sin(
                    7.0 * phi - omega * time * 2.5 + 2.3 * hash01(seed, ri, si))
                dop = 1.0 + DOPPLER * (-math.cos(phi))
                mult = b * flick * dop * side_factor(sphi, u)
                if not near_side:
                    mult *= 0.92
                # el borde interno caliente ARDE en el lado cercano
                if u < R_IN:
                    mult *= 0.35 + 0.65 * max(0.0, sphi)
                a = min(1.0, 1.02 * mult)
                c = list(col)
                if near_side and sphi > 0:
                    # núcleo del arco cercano más CLARO (rosa-blanco medido)
                    w = 0.22 * sphi
                    for k in range(3):
                        tgt = (255, 196, 255)[k]
                        c[k] = c[k] + (tgt - c[k]) * w
                elif not near_side and u < 3.1:
                    # borde del arco lejano más BLANCO (medido arriba)
                    w = 0.45 * (3.1 - u) / (3.1 - R_FAR_IN)
                    for k in range(3):
                        c[k] = c[k] + (255 - c[k]) * w
                cv.add_quad(tex_softglow, px, py, cap_len, cap_w_side, rot,
                            tuple(int(v) for v in c), a)

    disk_pass(near_side=False)  # 2. lado lejano (detrás de la esfera)

    # ---------- 3. COMPLEJO DEL ANILLO DE FOTONES (la esfera lo recorta) ----------
    cv.add_quad(tex_softglow, cx, cy, 4.2 * r_sh, 4.2 * r_sh, 0, (255, 196, 228), 0.26)
    cv.add_quad(tex_softglow, cx, cy, 3.3 * r_sh, 3.3 * r_sh, 0, (255, 214, 238), 0.42)
    cv.add_quad(tex_softglow, cx, cy, 2.7 * r_sh, 2.7 * r_sh, 0, (255, 224, 244), 0.62)

    # ---------- 4. ESFERA NEGRA (opaca, negro profundo) ----------
    cv.alpha_quad(tex_disk, cx, cy, 2 * r_sh, 2 * r_sh, 0, (0, 0, 0), 1.0)

    # ---------- 5. ANILLOS DE FOTONES (banda sólida + eco, como la ref) ----------
    ring_pulse = 0.90 + 0.10 * math.sin(time * 2.3)
    # banda principal sólida 1.16-1.49 (rungs solapados)
    for rr, aa, col in [(1.16, 150, (255, 248, 253)), (1.22, 245, (255, 250, 254)),
                        (1.28, 255, (255, 253, 255)), (1.34, 255, (255, 253, 255)),
                        (1.40, 225, (255, 247, 252)), (1.46, 175, (255, 240, 250)),
                        (1.56, 100, (255, 236, 248)), (1.62, 70, (255, 234, 247))]:
        s = 2.174 * rr * r_sh
        cv.add_quad(tex_ring, cx, cy, s, s, 0, col, (aa / 255) * ring_pulse)
    # eco lensado 1.68-1.95
    for rr, aa, col in [(1.68, 220, (255, 238, 249)), (1.75, 205, (255, 236, 248)),
                        (1.82, 165, (255, 232, 247)), (1.89, 120, (255, 228, 246)),
                        (1.96, 80, (255, 226, 245))]:
        s = 2.174 * rr * r_sh
        cv.add_quad(tex_ring, cx, cy, s, s, 0, col, aa / 255)
    # el eco ARQUEA por encima de la esfera (imagen lensada superior)
    cv.add_quad(tex_softglow, cx, cy - 1.62 * r_sh, 3.6 * r_sh, 1.5 * r_sh, 0,
                (255, 235, 248), 0.16)

    # ---------- 6. LADO CERCANO (cruza POR DELANTE de la esfera) ----------
    disk_pass(near_side=True)

    # ---------- 7. BLOOM ----------
    cv.add_quad(tex_softglow, cx, cy + R_PEAK * r_sh * RATIO * 0.72,
                6.8 * r_sh, 3.1 * r_sh * RATIO * 1.9, 0, (255, 150, 205), 0.30)
    cv.add_quad(tex_softglow, cx - 3.5 * r_sh, cy + 0.10 * r_sh,
                3.3 * r_sh, 1.5 * r_sh, -0.05, (255, 130, 190), 0.18)
    cv.add_quad(tex_softglow, cx, cy, 6.9 * r_sh, 6.9 * r_sh * RATIO * 1.55, 0,
                (255, 90, 170), 0.12)

    # estrellas de fondo (solo para comparar con la referencia)
    if stars:
        rng = np.random.default_rng(seed)
        for _ in range(38):
            x, y = int(rng.integers(0, W)), int(rng.integers(0, H))
            b = rng.uniform(120, 255)
            cv.buf[y, x] = [b, b, b]
    return cv.to_image()


if __name__ == "__main__":
    import sys
    out = sys.argv[1] if len(sys.argv) > 1 else \
        "/home/z/my-project/AethonMod/research/blackhole/mock_v609.png"
    # supersampling 2×: render grande + reducción (antialias como la GPU)
    img = render(r_sh=R_SH * 2, stars=False).resize(
        (940, 470), Image.LANCZOS)
    img2 = render(r_sh=R_SH * 2, stars=True).resize((940, 470), Image.LANCZOS)
    img2.save(out)
    print("guardado:", out, img2.size)

    ref = Image.open("/home/z/my-project/upload/pasted_image_1789282941428.png")
    comp = Image.new("RGB", (940 + ref.width + 20, 470), (8, 8, 12))
    comp.paste(img2, (0, 0))
    comp.paste(ref.convert("RGB"), (960, 470 // 2 - ref.height // 2))
    comp.save("/home/z/my-project/AethonMod/research/blackhole/mock_vs_ref.png")
    print("comparación:", comp.size)
