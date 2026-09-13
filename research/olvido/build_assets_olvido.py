#!/usr/bin/env python3
"""
build_assets_olvido.py — Assets FINALES del Agujero Negro del Olvido.

Genera en AethonMod/Content/Effects/Procedural/:
  · OlvidoVortex.png    (1024) — arte EXACTO extraído, ganancias horneadas,
                                 formato premultiplicado A=255 (aditivo lineal).
  · OlvidoHalo.png      (256)  — halo ambiental + bloom del anillo.
  · OlvidoSphere.png    (160)  — esfera negra + estrella + rayo púrpura.
  · OlvidoBackplate.png (256)  — placa oscura de fondo (rojo profundo).
  · OlvidoWisps.png     (512)  — velos exteriores para rotación lenta.

También mide la ELIPSE del anillo (para los overlays de fotones) y la
posición del hotspot, y las imprime como constantes para el C#.
"""
import numpy as np
from PIL import Image, ImageFilter
from scipy import ndimage
import json

SRC = 'referencia.png'
CX, CY, R = 495.0, 224.0, 34.0
RMAX = 7.8
OUT = '/home/z/my-project/AethonMod/AethonMod/Content/Effects/Procedural'

# ganancias del optimizador (130 rondas)
VORTEX_G = 0.954
RING_G = 1.042
HALO_G = 2.048
HALO_A = 1.784
BG = (35.5, 0.0, 6.2)

im = Image.open(SRC).convert('RGB')
a = np.array(im).astype(np.float32)
H, W = a.shape[:2]
r_, g_, b_ = a[..., 0], a[..., 1], a[..., 2]
lum = a.max(axis=2)
yy, xx = np.mgrid[0:H, 0:W]
rho = np.sqrt((xx-CX)**2 + (yy-CY)**2)

vortex = np.array(Image.open('draft_vortex.png')).astype(np.float32)
halo = np.array(Image.open('draft_halo.png')).astype(np.float32)
sph_tex = np.array(Image.open('draft_sphere.png')).astype(np.float32)
wisps = np.array(Image.open('draft_wisp.png')).astype(np.float32)

# ---------------- 1. VORTEX premultiplicado (A=255) ----------------
vr, va = vortex[..., :3], vortex[..., 3]/255.0
SIZE = vortex.shape[0]
yyT, xxT = np.mgrid[0:SIZE, 0:SIZE]
TR = np.hypot(xxT-SIZE/2, yyT-SIZE/2) * (2*RMAX*R/SIZE)
ring_m = ((TR > 1.0*R) & (TR < 1.75*R))
gain_map = np.where(ring_m[..., None], VORTEX_G*RING_G, VORTEX_G)
out_rgb = np.clip(vr * va[..., None] * gain_map, 0, 255)
Image.fromarray(np.dstack([out_rgb, np.full((SIZE, SIZE), 255)]).astype(np.uint8)).save(f'{OUT}/OlvidoVortex.png')
print(f"[1] OlvidoVortex.png {SIZE}px (premultiplicado, A=255)")

# ---------------- 2. HALO premultiplicado ----------------
hr, ha = halo[..., :3], halo[..., 3]/255.0
out_h = np.clip(hr * ha[..., None] * HALO_A * HALO_G, 0, 255)
Image.fromarray(np.dstack([out_h, np.full((halo.shape[0], halo.shape[0]), 255)]).astype(np.uint8)).save(f'{OUT}/OlvidoHalo.png')
print(f"[2] OlvidoHalo.png {halo.shape[0]}px")

# ---------------- 3. ESFERA con interior detallado ----------------
TS = 160
SS = 4
THR = TS*SS
sph = np.zeros((THR, THR, 4), np.float32)
pxs = (np.arange(THR) - THR/2 + 0.5) * (2*1.05*R) / THR
SX = CX + pxs[None, :]*np.ones((THR, 1))
SY = CY + pxs[:, None]*np.ones((1, THR))
SR = np.hypot(SX-CX, SY-CY)
# bilinear del interior de la referencia
IXf = np.clip(SX, 0, W-1.001); IYf = np.clip(SY, 0, H-1.001)
IX0 = IXf.astype(int); IY0 = IYf.astype(int)
FX = (IXf-IX0)[..., None]; FY = (IYf-IY0)[..., None]
sub = (a[IY0, IX0]*(1-FX)*(1-FY) + a[IY0, IX0+1]*FX*(1-FY) +
       a[IY0+1, IX0]*(1-FX)*FY + a[IY0+1, IX0+1]*FX*FY)
sub_lum = sub.max(axis=2)
inside = SR <= R
# negro profundo + detalles internos (estrella, rayo) donde brillan
detail = (sub_lum > 48) & (SR < 0.985*R)
sph[..., :3] = np.where(detail[..., None], sub, np.array([2, 1, 4]))
# suavizar detalles (pinceladas, no pixeles)
det_rgb = np.array(Image.fromarray(np.clip(sph[..., :3], 0, 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(2*SS*0.55)))
sph[..., :3] = np.where(detail[..., None], det_rgb, np.array([2, 1, 4], np.float32))
sph[..., 3] = np.clip((R*1.006 - SR)/(0.012*R), 0, 1)
sph_img = Image.fromarray(np.clip(sph, 0, 255).astype(np.uint8)).resize((TS, TS), Image.LANCZOS)
sph_img.save(f'{OUT}/OlvidoSphere.png')
print(f"[3] OlvidoSphere.png {TS}px (estrella + rayo horneados)")

# ---------------- 4. BACKPLATE radial ----------------
TB = 256
bp = np.zeros((TB, TB, 4), np.float32)
pxb = (np.arange(TB) - TB/2 + 0.5) / (TB/2)      # -1..1
BX = pxb[None, :]*np.ones((TB, 1))
BY = pxb[:, None]*np.ones((1, TB))
BR = np.hypot(BX, BY)
prof = np.clip(1 - BR, 0, 1)**1.5
bp[..., 0] = BG[0]; bp[..., 1] = BG[1]; bp[..., 2] = BG[2]
bp[..., 3] = np.clip(prof*0.94, 0, 1)
Image.fromarray(np.clip(bp, 0, 255).astype(np.uint8)).save(f'{OUT}/OlvidoBackplate.png')
print(f"[4] OlvidoBackplate.png {TB}px")

# ---------------- 5. WISPS premultiplicado ----------------
wr, wa = wisps[..., :3], wisps[..., 3]/255.0
out_w = np.clip(wr * wa[..., None] * 0.9, 0, 255)
Image.fromarray(np.dstack([out_w, np.full((wisps.shape[0], wisps.shape[0]), 255)]).astype(np.uint8)).save(f'{OUT}/OlvidoWisps.png')
print(f"[5] OlvidoWisps.png {wisps.shape[0]}px")

# ---------------- 6. MEDICIONES para los overlays C# ----------------
# elipse del anillo (brillo>120 en 1.2-1.9R)
ring_px = (rho > 1.2*R) & (rho < 1.9*R) & (lum > 120) & (r_ > g_*1.4)
ys, xs = np.where(ring_px)
pts = np.stack([xs-CX, ys-CY], 1).astype(np.float64)
P = np.stack([pts[:, 0]**2, pts[:, 0]*pts[:, 1], pts[:, 1]**2], 1)
coef, *_ = np.linalg.lstsq(P, np.ones(len(pts)), rcond=None)
A_, B_, C_ = coef
M = np.array([[A_, B_/2], [B_/2, C_]])
evals, evecs = np.linalg.eigh(M)
axes = 1/np.sqrt(evals)
big = int(np.argmax(axes))
a_may, b_men = float(axes[big]), float(axes[1-big])
ang = float(np.degrees(np.arctan2(evecs[1, big], evecs[0, big])) % 180)
print(f"[6] ELIPSE ANILLO: a={a_may/R:.3f}R b={b_men/R:.3f}R squash={b_men/a_may:.3f} tilt={ang:.1f}°")

# hotspot: sector más brillante del anillo
best_sec, best_v = 0, 0
thd = np.degrees(np.arctan2(-(ys-CY), xs-CX)) % 360
for s0 in range(0, 360, 10):
    m = (thd >= s0) & (thd < s0+10)
    if m.sum() > 5:
        v = lum[ys[m], xs[m]].mean()
        if v > best_v:
            best_v, best_sec = v, s0+5
print(f"    HOTSPOT: {best_sec}° brillo={best_v:.0f}")

# estrella interior: centro de masa de lum>170 dentro de la esfera
ins = (rho < 0.99*R)
st = ins & (lum > 170)
if st.sum():
    ys2, xs2 = np.where(st)
    print(f"    ESTRELLA: ({xs2.mean()-CX:+.1f},{ys2.mean()-CY:+.1f})px rel = ({(xs2.mean()-CX)/R:+.2f}R,{(ys2.mean()-CY)/R:+.2f}R)")

json.dump({'vortex_g': VORTEX_G, 'ring_g': RING_G, 'halo_g': HALO_G, 'halo_a': HALO_A,
           'bg': BG, 'ring_ellipse': {'a_R': a_may/R, 'b_R': b_men/R, 'squash': b_men/a_may, 'tilt_deg': ang},
           'hotspot_deg': best_sec}, open('final_params.json', 'w'), indent=1)
print("[7] final_params.json guardado")

# ---------------- 8. PREVIEW FINAL (simula el juego exacto) ----------------
bgc = np.array(BG)
comp = np.broadcast_to(bgc, (SIZE, SIZE, 3)).astype(np.float32).copy()
halo_up = np.array(Image.fromarray(np.dstack([out_h, np.full((halo.shape[0], halo.shape[0]), 255)]).astype(np.uint8)).resize((SIZE, SIZE), Image.BILINEAR)).astype(np.float32)
comp += halo_up[..., :3]  # A=255 -> aditivo directo
comp += out_rgb
sph_up = np.array(sph_img.resize((int(SIZE*1.05*R/(RMAX*R)),)*2, Image.BILINEAR)).astype(np.float32)
s0 = (SIZE - sph_up.shape[0])//2
reg = comp[s0:s0+sph_up.shape[0], s0:s0+sph_up.shape[0]]
al = sph_up[..., 3:]/255
comp[s0:s0+sph_up.shape[0], s0:s0+sph_up.shape[0]] = reg*(1-al) + sph_up[..., :3]*al
prev = Image.fromarray(np.clip(comp, 0, 255).astype(np.uint8))
prev.save('preview_final_olvido.png')
S = int(round(2*RMAX*R))
x0, y0 = int(round(CX-RMAX*R)), int(round(CY-RMAX*R))
refc = im.crop((x0, y0, x0+S, y0+S))
side = Image.new('RGB', (S*2+16, S), (25, 25, 25))
side.paste(refc, (0, 0))
side.paste(prev.resize((S, S), Image.LANCZOS), (S+16, 0))
side.save('compare_final.png')
d = np.abs(np.array(refc).astype(np.float32) - np.array(prev.resize((S, S), Image.LANCZOS)).astype(np.float32))
print(f"[8] EMA final vs referencia: {d.mean():.1f}/255 (total ventana)")
