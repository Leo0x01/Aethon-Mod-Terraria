#!/usr/bin/env python3
"""GARGANTUA v6 — réplica con MEDIDAS PÍXEL-EXACTAS de la referencia.

Mediciones directas del recorte del agujero de la referencia (627×294):
  * Sombra: centro (272,145), Ø~95px → 28% del ancho de la estructura.
  * Disco: extensión total ~335px = 3.5× el diámetro de la sombra (COMPACTO).
  * Banda frontal GRUESA: 40px de alto = ±0.43 r_sh.
  * Arco superior: cima a ~61px del centro = 1.3 r_sh, BRILLANTE.
  * Ecuador del disco pasa ~0.15 r_sh POR DEBAJO del centro de la sombra.
  * Tilt del sistema: ~-12° (extremo derecho hacia arriba).
  * Doppler: lado izquierdo blanco-cálido; lado derecho carmesí tenue.
"""
import numpy as np
from PIL import Image

CW, CH = 2048, 1024
CX, CY = CW / 2, CH / 2
R_SH = 150.0
TILT = np.radians(-12.0)

DISK_IN = 1.14 * R_SH
DISK_OUT = 3.55 * R_SH          # COMPACTO como la referencia
HALF_H0 = 0.43 * R_SH           # BANDA GRUESA
DISK_DROP = 0.16 * R_SH         # ecuador por debajo del centro de la sombra

ARC_TOP_H = 1.34 * R_SH         # cima del arco (medida)
ARC_BOT_H = 0.95 * R_SH
PH_R0, PH_R1 = 0.99 * R_SH, 1.14 * R_SH

# PALETA DUAL — el lado DOPPLER (que se acerca) es CÁLIDO (coral→dorado→blanco
# como la referencia); el lado trasero cae al magenta. Interpoladas por doppler.
PAL_WARM = [
    (0.00, (  6,   0,   3)),
    (0.07, ( 34,   0,   9)),
    (0.16, ( 90,   0,  26)),
    (0.28, (150,   0,  42)),
    (0.40, (198,  20,  52)),
    (0.52, (238,  52,  76)),
    (0.62, (252,  96,  84)),    # CORAL NARANJA
    (0.74, (255, 150, 110)),    # NARANJA CLARO
    (0.86, (255, 208, 150)),    # DORADO CLARO
    (0.94, (255, 244, 220)),    # blanco cálido
    (1.00, (255, 255, 255)),
]
PAL_MAGENTA = [
    (0.00, (  6,   0,   3)),
    (0.07, ( 34,   0,   9)),
    (0.16, ( 90,   0,  32)),
    (0.28, (150,   0,  52)),
    (0.40, (205,   0,  84)),
    (0.52, (246,  12, 112)),
    (0.62, (255,  22, 172)),
    (0.74, (255,  84, 202)),
    (0.86, (255, 192, 222)),
    (0.94, (255, 246, 232)),
    (1.00, (255, 255, 255)),
]
PALETTE = PAL_MAGENTA

def _pal_eval(P, T):
    T = np.clip(T, 0.0, 1.0)
    out = np.zeros(T.shape + (3,), dtype=np.float64)
    for i in range(len(P) - 1):
        t0, c0 = P[i]
        t1, c1 = P[i + 1]
        m = (T >= t0) & (T <= t1)
        f = np.where(m, (T - t0) / max(t1 - t0, 1e-6), 0.0)
        for ch in range(3):
            out[..., ch] += np.where(m, c0[ch] + (c1[ch] - c0[ch]) * f, 0.0)
    return out

def palette(T):
    return _pal_eval(PAL_MAGENTA, T)

def palette_doppler(T, dop):
    """Gradiente de calor DUAL: cálida donde el material se acerca (dop=1),
    magenta donde se aleja (dop=0)."""
    warm = _pal_eval(PAL_WARM, T)
    mag = _pal_eval(PAL_MAGENTA, T)
    w = np.clip(dop, 0, 1)[..., None] if dop.ndim == T.ndim else dop[..., None]
    return warm * w + mag * (1.0 - w)

def _h1(seed, x):
    h = (seed * 374761393 + x * 668265263) & 0xFFFFFFFF
    h ^= h >> 13; h = (h * 1274126177) & 0xFFFFFFFF; h ^= h >> 16
    return (h & 0xFFFFFF) / 16777216.0

def vnoise2(seed, X, Y):
    xi, yi = np.floor(X).astype(np.int64), np.floor(Y).astype(np.int64)
    xf, yf = X - xi, Y - yi
    ux = xf * xf * (3 - 2 * xf)
    uy = yf * yf * (3 - 2 * yf)
    U64 = np.uint64
    def hh(xi, yi):
        h = (U64(seed) * U64(374761393) + (xi & 0xFFFF).astype(U64) * U64(668265263)
             + (yi & 0xFFFF).astype(U64) * U64(1911520717))
        h = h ^ (h >> U64(13))
        h = (h * U64(1274126177)) & U64(0xFFFFFFFF)
        h = h ^ (h >> U64(16))
        return (h & U64(0xFFFFFF)) / 16777216.0
    a = hh(xi, yi); b = hh(xi + 1, yi); c = hh(xi, yi + 1); d = hh(xi + 1, yi + 1)
    return (a * (1 - ux) + b * ux) * (1 - uy) + (c * (1 - ux) + d * ux) * uy

def fbm2(seed, X, Y, octaves=4, base=5.0):
    total = np.zeros_like(X); amp, norm = 1.0, 0.0
    freq = base
    for o in range(octaves):
        total += amp * vnoise2(seed + o * 131, X * freq, Y * freq)
        norm += amp; amp *= 0.52; freq *= 2.05
    return total / norm

def vnoise1(seed, x, period):
    i0 = np.floor(x).astype(np.int64)
    f = x - i0
    u = f * f * (3 - 2 * f)
    v0 = np.array([_h1(seed, int(i) % period) for i in i0.ravel()]).reshape(i0.shape)
    v1 = np.array([_h1(seed, int(i) % period) for i in (i0 + 1).ravel()]).reshape(i0.shape)
    return v0 * (1 - u) + v1 * u

def fbm_ang(seed, ang01, octaves=3, base_period=12):
    total = np.zeros_like(ang01); amp, norm = 1.0, 0.0
    period = base_period
    for o in range(octaves):
        total += amp * vnoise1(seed + o * 977, ang01 * period, period)
        norm += amp; amp *= 0.55; period *= 2
    return total / norm

# ----------------------------------------------------------------------
#  MALLA
# ----------------------------------------------------------------------
xs = np.arange(CW); ys = np.arange(CH)
X, Y = np.meshgrid(xs, ys)
Xc, Yc = X - CX, Y - CY
ct, st = np.cos(-TILT), np.sin(-TILT)
Xr0 = Xc * ct - Yc * st
Yr = Xc * st + Yc * ct - DISK_DROP     # ecuador BAJO el centro de la sombra

k_skew = 0.80
Xs = np.sign(Xr0) * np.abs(Xr0 / 200.0) ** k_skew * 200.0
RHO = np.sqrt(Xs ** 2 + Yr ** 2)
RHO_raw = np.sqrt(Xr0 ** 2 + (Yr + DISK_DROP) ** 2)   # respecto al centro de la SOMBRRA
ANG = np.arctan2(Yr, Xs)
ang01 = (ANG / (2 * np.pi) + 1.0) % 1.0
logR = np.log(np.maximum(RHO, 4.0) / R_SH)

# turbulencia con domain warping — ANISOTROFÍA EXTREMA (vetas orbitales
# MUY estiradas, como plasma fluyendo a velocidad orbital)
u = ang01 * 14.0
v = logR * 2.6
wx = fbm2(41, u / 6.0, v / 4.0, 3, 3.0) - 0.5
wy = fbm2(97, u / 6.0, v / 4.0, 3, 3.0) - 0.5
turbA = fbm2(808, u + wx * 4.6, v + wy * 1.1, 4, 5.0)
turbB = fbm2(313, u * 2.4 + wx * 2.6, v * 2.6 + wy * 0.9, 3, 6.0)
turb = 0.62 * turbA + 0.38 * turbB
turb_sharp = np.clip((turb - 0.30) / 0.42, 0, 1) ** 1.5
# filamentos BLANCOS finos (vetas de energía de la referencia)
filament = np.clip((turbA - 0.55) * 3.6, 0, 1) * np.clip((turbB - 0.32) * 2.4, 0, 1)
filament = filament ** 0.7
# VETAS OSCURAS entre filamentos (el contraste duro de la referencia)
dark_lanes = np.clip((turb - 0.36) / 0.30, 0, 1)

doppler = 0.5 - 0.5 * np.clip(Xs / (DISK_OUT * 0.60), -1, 1)

# ----------------------------------------------------------------------
#  FRONT — LA BANDA GRUESA QUE CRUZA (compacta, como la referencia)
# ----------------------------------------------------------------------
rho_n = np.clip((RHO - DISK_IN) / (DISK_OUT - DISK_IN), 0, 1)
# GROSOR 3D (torus): más grueso en el FRENTE (Yr>0, cerca del observador),
# fino hacia atrás y en los extremos — profundidad de perspectiva
frontness = np.clip(Yr / (HALF_H0 * 2.2), -1, 1) * 0.5 + 0.5       # 0 atrás → 1 frente
half_h = HALF_H0 * (1.0 - rho_n) ** 0.55 * (0.62 + 0.55 * frontness) + 0.030 * R_SH
jitter_amp = 0.26 * (1.0 - rho_n ** 1.4)
h_jitter = 1.0 + jitter_amp * (turb - 0.5)
y_rel = Yr / np.maximum(half_h * h_jitter, 1e-3)
in_band = (np.abs(Yr) <= half_h * h_jitter) & (RHO >= DISK_IN * 0.86) & (RHO <= DISK_OUT)

# RIM INTERIOR ARDIENDO: el gas más caliente vive en el borde interno del
# torus (pegado a la sombra) — franja blanca-amarilla como la referencia
inner_rim = np.exp(-((RHO - DISK_IN * 1.06) / (R_SH * 0.38)) ** 2) * in_band

core = np.exp(-y_rel ** 2 * 1.4)
cross = np.exp(-(Xs / (R_SH * 1.25)) ** 2)

T_rad = 1.0 - rho_n * 0.46
T_front = np.clip(
    T_rad * (0.46 + 0.54 * doppler ** 2.2)
    + 0.50 * cross
    + 0.42 * inner_rim * (0.45 + 0.55 * doppler)   # rim interior BLANCO-AMARILLO
    + 0.24 * filament * doppler                     # filamentos blancos
    + 0.22 * turb_sharp * doppler
    + 0.10 * (turb - 0.5),
    0.02, 1.0)

I_front = (0.72 + 1.05 * doppler ** 1.8) * (1.0 - rho_n * 0.30)   # lado derecho VISIBLE
I_front *= (0.74 + 0.55 * turb_sharp + 0.30 * turb + 0.55 * filament)
I_front *= (0.38 + 0.62 * dark_lanes)                     # VETAS OSCURAS: contraste duro
I_front *= (1.0 + 1.05 * cross + 0.85 * inner_rim * (0.4 + 0.6 * doppler))
alpha_front = np.where(in_band, np.clip(I_front * (0.52 + 0.66 * core), 0, 1.0) ** 1.12, 0.0)

front_rgb = palette_doppler(T_front, doppler)   # ← gradiente CÁLIDO dual
white_mask = (T_front > 0.90) & in_band & (alpha_front > 0.55)
front_rgb[white_mask] = [255, 255, 250]
# toque DORADO extra en el rim interior cálido
warm_zone = in_band & (inner_rim > 0.25) & (doppler > 0.55) & (T_front > 0.70)
front_rgb[warm_zone] = np.clip(
    front_rgb[warm_zone] * np.array([1.0, 0.95, 0.62]), 0, 255).astype(np.float64)

# ----------------------------------------------------------------------
#  BACK
# ----------------------------------------------------------------------
back_rgb = np.zeros((CH, CW, 3))
back_a = np.zeros((CH, CW))

def add_layer(rgb, a, lrgb, la):
    rgb[..., 0] += lrgb[..., 0] * la
    rgb[..., 1] += lrgb[..., 1] * la
    rgb[..., 2] += lrgb[..., 2] * la
    a += la

# --- 1. NEBLINA ROJA ---
clouds = fbm2(505, X / 850.0, Y / 850.0, 4, 3.4)
clouds_c = np.clip((clouds - 0.34) / 0.66, 0, 1) ** 1.4
halo_r = RHO_raw / (DISK_OUT * 1.30)
I_halo = np.clip(1.0 - halo_r, 0, 1) ** 2.0 * 0.50 * (0.4 + 0.9 * clouds_c)
halo_rgb = np.zeros((CH, CW, 3))
halo_rgb[..., 0] = 130; halo_rgb[..., 1] = 10; halo_rgb[..., 2] = 46
add_layer(back_rgb, back_a, halo_rgb, I_halo)

# --- 2. ARCO SUPERIOR (cima 1.34 R, brillante, continuo) ---
A_arc = DISK_OUT * 0.98
B_arc = ARC_TOP_H
arc_mod = 1.0 + 0.10 * (fbm_ang(421, ang01, 3, 12) - 0.5) * 2.0
E_arc = np.sqrt((Xs / (A_arc * arc_mod)) ** 2 + (Yr / (B_arc * arc_mod)) ** 2)
theta_arc = np.arctan2(Yr / B_arc, Xs / A_arc)
d_arc = (E_arc - 1.0) * np.maximum(A_arc, B_arc)
arc_w = R_SH * (0.11 + 0.30 * np.abs(np.sin(theta_arc)) ** 0.5) * (0.82 + 0.36 * turb)
in_arc = (Yr < 0.10 * R_SH) & (np.abs(d_arc) <= arc_w) & (E_arc > 0.5) & (RHO_raw > R_SH * 1.0)

near_sh = np.clip(1.0 - (RHO_raw - R_SH * 1.15) / (R_SH * 1.10), 0, 1)
T_arc = 0.58 + 0.26 * doppler ** 1.2 + 0.32 * near_sh ** 1.5 + 0.10 * (turb_sharp - 0.3)
I_arc = (0.55 + 0.70 * doppler ** 1.2) * (1.0 - np.abs(d_arc) / np.maximum(arc_w, 1e-3) * 0.34)
I_arc *= (0.68 + 0.60 * turb_sharp + 0.28 * turb + 0.50 * filament)
I_arc *= (0.42 + 0.58 * dark_lanes)                       # vetas oscuras también
I_arc *= (1.0 + 1.45 * near_sh ** 2.0)
alpha_arc = np.where(in_arc, np.clip(I_arc, 0, 1), 0.0)
add_layer(back_rgb, back_a, palette_doppler(np.clip(T_arc, 0.02, 1.0), doppler), alpha_arc)

# --- 3. ARCO INFERIOR ---
B_arc2 = ARC_BOT_H
E_arc2 = np.sqrt((Xs / A_arc) ** 2 + (Yr / B_arc2) ** 2)
d_arc2 = (E_arc2 - 1.0) * np.maximum(A_arc, B_arc2)
arc_w2 = R_SH * (0.10 + 0.22 * np.abs(np.sin(np.arctan2(Yr / B_arc2, Xs / A_arc))) ** 0.5)
in_arc2 = (Yr > 0) & (np.abs(d_arc2) <= arc_w2) & (E_arc2 > 0.5) & (RHO_raw > R_SH * 1.0)
T_arc2 = 0.40 + 0.22 * doppler ** 1.2 + 0.16 * near_sh ** 1.6 + 0.08 * (turb - 0.5)
I_arc2 = (0.28 + 0.42 * doppler ** 1.3) * (1.0 - np.abs(d_arc2) / np.maximum(arc_w2, 1e-3) * 0.36)
I_arc2 *= (0.62 + 0.55 * turb_sharp)
I_arc2 *= (1.0 + 0.8 * near_sh ** 2.0)
alpha_arc2 = np.where(in_arc2, np.clip(I_arc2, 0, 1), 0.0)
add_layer(back_rgb, back_a, palette(np.clip(T_arc2, 0.02, 1.0)), alpha_arc2)

# --- 4. ANILLO DE FOTONES — LA PARTE MÁS BRILLANTE (naranja-blanco) ---
pr = (RHO_raw - PH_R0) / (PH_R1 - PH_R0)
in_ph = (pr >= 0) & (pr <= 1)
prof_ph = np.sin(np.clip(pr, 0, 1) * np.pi) ** 0.5
bolts = fbm_ang(777, ang01, 3, 48)
bolt_spikes = np.clip((bolts - 0.62) * 4.0, 0, 1) * np.clip(1.0 - np.abs(pr - 0.45) * 2.4, 0, 1)
T_ph = 0.74 + 0.18 * doppler + bolt_spikes * 0.16     # NARANJA-BLANCO
I_ph = prof_ph * (1.05 + 0.85 * doppler ** 1.1) + bolt_spikes * 0.60 * (0.6 + 0.4 * doppler)
alpha_ph = np.where(in_ph, np.clip(I_ph, 0, 1), 0.0)
ph_rgb = palette_doppler(np.clip(T_ph, 0, 1.0), 0.65 + 0.35 * doppler)
add_layer(back_rgb, back_a, ph_rgb, alpha_ph)

# --- 4b. TENDRILAS DE GAS (wisps caóticas alrededor del disco) ---
wisp_field = fbm2(222, ang01 * 10.0, logR * 4.2, 4, 5.0)
wisps = np.clip((wisp_field - 0.62) * 3.0, 0, 1) ** 1.4
wisp_zone = (RHO_raw > R_SH * 1.25) & (RHO_raw < DISK_OUT * 1.18)
wisp_col = palette_doppler(np.full_like(RHO_raw, 0.34), doppler)
alpha_wisp = np.where(wisp_zone, wisps * 0.30 * np.clip(1.0 - RHO_raw / (DISK_OUT * 1.18), 0, 1), 0.0)
add_layer(back_rgb, back_a, wisp_col, alpha_wisp)

# --- 5. PENUMBRA (cálida — rojo intenso pegado a la sombra) ---
pen = np.clip(1.0 - (RHO_raw - R_SH) / (R_SH * 0.55), 0, 1)
I_pen = (pen ** 2.6) * 0.55 * (0.6 + 0.5 * doppler)
add_layer(back_rgb, back_a, palette_doppler(np.full_like(RHO_raw, 0.38), doppler), I_pen)

inside_shadow = RHO_raw < R_SH * 0.985
back_a = np.where(inside_shadow, 0.0, back_a)

# ----------------------------------------------------------------------
#  EXPORTAR
# ----------------------------------------------------------------------
def to_png(rgb, a, path):
    r = np.clip(rgb, 0, 255).astype(np.uint8)
    alpha = np.clip(a * 255, 0, 255).astype(np.uint8)
    Image.fromarray(np.dstack([r, alpha])).save(path)
    print(f"  {path.split('/')[-1]}: activos={(alpha > 8).sum()}")

OUT = "/home/z/my-project/AethonMod/AethonMod/Content/Effects/Procedural/"
print("GARGANTUA v6 (medidas exactas):")
to_png(back_rgb, back_a, OUT + "GargantuaBack.png")
to_png(front_rgb, alpha_front, OUT + "GargantuaFront.png")

# ----------------------------------------------------------------------
#  PREVIEW
# ----------------------------------------------------------------------
comp = np.zeros((CH, CW, 3))
comp += back_rgb * np.clip(back_a, 0, 1)[..., None]
comp = np.where(inside_shadow[..., None], np.array([1, 0, 1]), comp)
edge = np.clip((RHO_raw - R_SH * 0.985) / (R_SH * 0.024), 0, 1)
comp = comp * edge[..., None] + np.array([1, 0, 1]) * (1 - edge[..., None])
comp += front_rgb * np.clip(alpha_front, 0, 1)[..., None]
Image.fromarray(np.clip(comp, 0, 255).astype(np.uint8)).save(
    "/home/z/my-project/AethonMod/research/gargantua/gargantua_preview.png")
print("  preview listo.")
