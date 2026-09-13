#!/usr/bin/env python3
"""
extract_olvido_v4.py — Extracción v4 del Agujero Negro del Olvido.

Claves v4:
  · RGB muestreado BILINEAL desde la referencia (sin bloques).
  · Máscara de plasma SUAVE (gaussiana en espacio fuente) -> bordes antialias.
  · Color extendido por EDT fuera de la máscara (sin franjas oscuras al bilinear).
  · Personaje = SOLO colores equilibrados (plata/máscara/cuernos). Los rojos y
    magentas del arte (brazos, rayos, resplandor de corona) quedan incluidos.
  · Esfera supersampleada x4.
"""
import numpy as np
from PIL import Image, ImageFilter
from scipy import ndimage
import json

SRC = 'referencia.png'
CX, CY, R = 495.0, 224.0, 34.0
RMAX = 7.8
TEX_VORTEX = 1024
TEX_SPHERE = 160
TEX_HALO = 256
TEX_WISP = 512

im = Image.open(SRC).convert('RGB')
a = np.array(im).astype(np.float32)
H, W = a.shape[:2]
r_, g_, b_ = a[..., 0], a[..., 1], a[..., 2]
lum = 0.299*r_ + 0.587*g_ + 0.114*b_
sat = np.maximum(np.maximum(r_, g_), b_) - np.minimum(np.minimum(r_, g_), b_)
yy, xx = np.mgrid[0:H, 0:W]
rho = np.sqrt((xx-CX)**2 + (yy-CY)**2)

# ---------------- 1. PLASMA ----------------
magenta = (r_ > 1.85*g_) & (b_ > 0.22*r_) & (lum > 40)
redp = (r_ > 1.7*g_) & (b_ < 0.22*r_) & (lum > 50) & (rho < RMAX*R)   # rojos/naranjas del arte
white = (r_ > 195) & (g_ > 150) & (b_ > 150)
ring_zone = (rho > 1.0*R) & (rho < 2.4*R)
web = magenta & ring_zone
white_hot = white & (rho > 0.95*R) & (rho < 2.45*R) & ndimage.binary_dilation(web, iterations=3)
pink = (r_ > 1.5*g_) & (b_ > 0.35*r_) & (b_ < 0.95*r_) & (lum > 80)
pink_ring = pink & (rho > 0.95*R) & (rho < 2.6*R)

hole_core = magenta | redp | white_hot | pink_ring
lab, n = ndimage.label(hole_core)
web_labels = set(np.unique(lab[web]).tolist()) - {0}
keep = np.zeros(n + 1, bool)
for L in web_labels:
    keep[L] = True
bigd = ndimage.binary_dilation((lab > 0) & keep[lab], iterations=22)
for L in set(np.unique(lab[bigd & hole_core]).tolist()) - {0}:
    keep[L] = True
hole_mask = keep[lab]

# ---------------- 2. PERSONAJE: SOLO equilibrados ----------------
balanced = (sat < 46) & (lum > 22) & (rho > 1.06*R) & (rho < (RMAX + 1.2)*R)
charmask = ndimage.binary_dilation(balanced, iterations=1)
hole_bin = hole_mask & ~charmask
hole_bin = ndimage.binary_closing(hole_bin, structure=np.ones((3, 3)), iterations=2)
lab2, n2 = ndimage.label(hole_bin)
if n2:
    sizes2 = ndimage.sum(hole_bin, lab2, range(1, n2 + 1))
    for L in range(1, n2 + 1):
        if sizes2[L-1] < 8:
            hole_bin[lab2 == L] = False
print(f"[1-2] plasma v4: {hole_bin.sum()} px (personaje equilibrado excluido: {charmask.sum()})")

# ---------------- 3. MÁSCARA SUAVE + RGB EXTENDIDO ----------------
hole_soft = ndimage.gaussian_filter(hole_bin.astype(np.float32), 1.15)
hole_soft = np.where(ndimage.binary_dilation(hole_bin, iterations=4), hole_soft, 0.0)
# extender color: fuera de la máscara, el RGB toma el color del plasma más cercano
D, IND = ndimage.distance_transform_edt(~hole_bin, return_indices=True)
rgb_ext = a.copy()
outside = D <= 4
rgb_ext[outside] = a[IND[0][outside], IND[1][outside]]

# ---------------- 4. INPAINTING del personaje (polar, solo huecos ocultos) ----------------
NR, NA = 220, 1080
rr = np.linspace(0.9*R, RMAX*R, NR)
aa = np.radians(np.linspace(0, 360, NA, endpoint=False))
AX = CX + np.cos(aa)[None, :]*rr[:, None]
AY = CY - np.sin(aa)[None, :]*rr[:, None]
IX0 = np.clip(AX.astype(int), 0, W-1); IY0 = np.clip(AY.astype(int), 0, H-1)
pol_hole = hole_bin[IY0, IX0]
pol_char = charmask[IY0, IX0]
pol_rgb = a[IY0, IX0]
recon_rgb = pol_rgb.copy()
recon_hole = pol_hole.copy()
k = np.hanning(25); k /= k.sum()
for i in range(NR):
    if rr[i] < 1.05*R:
        continue
    clean = pol_hole[i]
    occluded = pol_char[i] & ~clean
    if not clean.any() or not occluded.any():
        continue
    pres = clean.astype(float)
    pres_s = np.convolve(np.concatenate([pres[-12:], pres, pres[:12]]), k, 'same')[12:-12]
    need = occluded & (pres_s > 0.30)
    for j in np.where(need)[0]:
        bl = br = None
        for d in range(1, 140):
            if bl is None and clean[(j-d) % NA]:
                bl = pol_rgb[i, (j-d) % NA]
            if br is None and clean[(j+d) % NA]:
                br = pol_rgb[i, (j+d) % NA]
            if bl is not None and br is not None:
                break
        if bl is not None and br is not None:
            recon_rgb[i, j] = 0.5*(bl+br)
        elif bl is not None:
            recon_rgb[i, j] = bl
        elif br is not None:
            recon_rgb[i, j] = br
        else:
            continue
        recon_hole[i, j] = True
print(f"[3] inpainting: {int((recon_hole & ~pol_hole).sum())} celdas")

# campo reconstruido suave para texels ocultos
recon_soft = ndimage.gaussian_filter(recon_hole.astype(np.float32), (1.5, 2.0))

def polar_bil(RR, TH_deg):
    ti = np.clip((RR - rr[0])/(rr[-1]-rr[0])*(NR-1), 0, NR-1.001)
    tj = (TH_deg % 360)/360*NA
    i0 = ti.astype(int); j0 = tj.astype(int) % NA
    fi = ti-i0; fj = tj-j0
    i1 = np.minimum(i0+1, NR-1); j1 = (j0+1) % NA
    def bil(F):
        v00 = F[i0, j0]; v01 = F[i0, j1]; v10 = F[i1, j0]; v11 = F[i1, j1]
        if F.ndim == 3:
            fi_ = fi[..., None]; fj_ = fj[..., None]
        else:
            fi_ = fi; fj_ = fj
        return v00*(1-fi_)*(1-fj_) + v01*(1-fi_)*fj_ + v10*fi_*(1-fj_) + v11*fi_*fj_
    return bil(recon_rgb), bil(recon_soft)

# ---------------- 5. TEXTURA VORTEX ----------------
size_v = TEX_VORTEX
win = RMAX*R
pxt = (np.arange(size_v) - size_v/2 + 0.5) * (2*win) / size_v
TX = CX + pxt[None, :]*np.ones((size_v, 1))
TY = CY + pxt[:, None]*np.ones((1, size_v))
TR = np.hypot(TX-CX, TY-CY)
TA = np.degrees(np.arctan2(-(TY-CY), TX-CX)) % 360
# bilinear del rgb extendido y la máscara suave
def bilinear_img(field):
    from scipy.ndimage import map_coordinates
    return map_coordinates(field, [TY.ravel(), TX.ravel()], order=1, mode='nearest').reshape(size_v, size_v)
tex_rgb = np.stack([bilinear_img(rgb_ext[..., c]) for c in range(3)], -1)
tex_a = bilinear_img(hole_soft)
pr_rgb, pr_hole = polar_bil(TR, TA)
sel_in = (TR >= 1.02*R) & (TR <= RMAX*R)
direct = bilinear_img(hole_bin.astype(np.float32))
use_recon = sel_in & (direct < 0.35) & (pr_hole > 0.45)
tex_rgb[use_recon] = pr_rgb[use_recon]
tex_a = np.where(use_recon, np.maximum(tex_a, pr_hole), tex_a)
tex_a *= np.clip((RMAX*R - TR)/(0.5*R), 0, 1)
tex_a *= sel_in | (TR < 1.02*R)*0
print(f"[5] vortex: {int((tex_a>0.05).sum())} texels activos, recon={int(use_recon.sum())}")

# ---------------- 6. ESFERA ----------------
SS = 4
THR = TEX_SPHERE*SS
sph_hr = np.zeros((THR, THR, 4), np.float32)
pxs = (np.arange(THR) - THR/2 + 0.5) * (2*1.05*R) / THR
SX = CX + pxs[None, :]*np.ones((THR, 1))
SY = CY + pxs[:, None]*np.ones((1, THR))
SR = np.hypot(SX-CX, SY-CY)
sph_hr[..., 0] = 2; sph_hr[..., 1] = 1; sph_hr[..., 2] = 4
sph_hr[..., 3] = np.clip((R*1.008 - SR)/(0.016*R), 0, 1)
sub_fil = (b_ > r_*0.75) & (b_ > 55)
IXS = np.clip(np.round(SX).astype(int), 0, W-1)
IYS = np.clip(np.round(SY).astype(int), 0, H-1)
fm = sub_fil[IYS, IXS] & (SR < 0.96*R)
if fm.sum() > 10:
    fcol = a[IYS, IXS] * fm[..., None]
    sph_hr[..., :3] = np.where(fm[..., None], fcol*0.45 + np.array([10, 2, 20]), sph_hr[..., :3])
    sph_hr[..., 3] = np.maximum(sph_hr[..., 3], fm*0.55)
sph_hr[..., :3] = np.array(Image.fromarray(np.clip(sph_hr[..., :3], 0, 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(3)))
sph_hr[..., 3] = ndimage.gaussian_filter(sph_hr[..., 3], 2.4)
sph = np.array(Image.fromarray(np.clip(sph_hr, 0, 255).astype(np.uint8)).resize((TEX_SPHERE, TEX_SPHERE), Image.LANCZOS)).astype(np.float32)
print(f"[6] esfera lista ({TEX_SPHERE}px)")

# ---------------- 7. HALO / WISPS ----------------
halo_w = hole_bin | ((lum > 26) & (rho < 6.2*R) & (r_ > 1.4*g_) & ~charmask)
halo_blur = ndimage.gaussian_filter(halo_w.astype(np.float32), 22)
halo_rgb = a*halo_blur[..., None]*0.55 + np.array([105, 8, 34])[None, None, :]*halo_blur[..., None]*0.45
halo_a = np.clip(halo_blur*2.3, 0, 0.85)
wisp_mask = hole_bin & (rho > 4.4*R)

def sample_grid(tex_size, radius_px, rgb_field, alpha_field):
    pxt = (np.arange(tex_size) - tex_size/2 + 0.5) * (2*radius_px) / tex_size
    GX = CX + pxt[None, :]*np.ones((tex_size, 1))
    GY = CY + pxt[:, None]*np.ones((1, tex_size))
    from scipy.ndimage import map_coordinates
    rg = np.stack([map_coordinates(rgb_field[..., c], [GY.ravel(), GX.ravel()], order=1, mode='nearest').reshape(tex_size, tex_size) for c in range(3)], -1)
    al = map_coordinates(alpha_field, [GY.ravel(), GX.ravel()], order=1, mode='nearest').reshape(tex_size, tex_size)
    return np.dstack([rg, al])

halo_tex = sample_grid(TEX_HALO, RMAX*R*0.92, halo_rgb, halo_a)
wisp_tex = sample_grid(TEX_WISP, RMAX*R, a*wisp_mask[..., None], wisp_mask.astype(np.float32))

# ---------------- 8. GUARDAR + PREVIEW ----------------
def save_rgba(arr, path):
    Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8)).save(path)

save_rgba(np.dstack([tex_rgb, tex_a*255]), 'draft_vortex.png')
save_rgba(sph, 'draft_sphere.png')
save_rgba(halo_tex, 'draft_halo.png')
save_rgba(wisp_tex, 'draft_wisp.png')

bg = np.full((size_v, size_v, 3), 10.0)
halo_up = np.array(Image.fromarray(np.clip(halo_tex, 0, 255).astype(np.uint8)).resize((size_v, size_v), Image.BILINEAR)).astype(np.float32)
comp = bg + halo_up[..., :3]*(halo_up[..., 3:]/255)
comp = comp + tex_rgb*tex_a[..., None]
sphsz = int(round(size_v*1.05*R/win))
sp_up = np.array(Image.fromarray(np.clip(sph, 0, 255).astype(np.uint8)).resize((sphsz, sphsz), Image.BILINEAR)).astype(np.float32)
s0 = (size_v - sphsz)//2
reg = comp[s0:s0+sphsz, s0:s0+sphsz]
al = (sp_up[..., 3:]/255)
comp[s0:s0+sphsz, s0:s0+sphsz] = reg*(1-al) + sp_up[..., :3]*al
prev = Image.fromarray(np.clip(comp, 0, 255).astype(np.uint8))
prev.save('preview_olvido_dark.png')

ref = Image.open(SRC).convert('RGB')
refc = ref.crop((int(CX-win), int(CY-win), int(CX+win), int(CY+win)))
side = Image.new('RGB', (refc.width*2+16, refc.height), (25, 25, 25))
side.paste(refc, (0, 0))
side.paste(prev.resize(refc.size, Image.LANCZOS), (refc.width+16, 0))
side.save('compare_r6.png')

refa = np.array(refc).astype(np.float32)
compd = np.array(prev.resize(refc.size, Image.LANCZOS)).astype(np.float32)
diff = np.abs(refa - compd).mean()
mask_any = (refa.max(axis=2) > 30) | (compd.max(axis=2) > 30)
diff_m = np.abs(refa - compd)[mask_any].mean() if mask_any.any() else 0
print(f"[8] EMA total={diff:.1f}/255  EMA activas={diff_m:.1f}/255")
json.dump({'cx': CX, 'cy': CY, 'R': R, 'RMAX': RMAX, 'EMA': float(diff)}, open('geom.json', 'w'), indent=1)
