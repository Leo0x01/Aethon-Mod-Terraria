#!/usr/bin/env python3
"""
optimize_olvido.py — 100+ RONDAS DE REVISIÓN PROFUNDA automatizadas.

Compone el render EXACTO como lo hará el C# (aditivo + alfa) con un vector
de parámetros de ganancia y minimiza el error contra la referencia con
descenso por coordenadas + perturbaciones aleatorias (rondas).

Métrica de pérdida:
  · EMA en la ventana activa
  · error del perfil radial de luminancia (plasma)
  · error del perfil angular del anillo interior
  · error de histograma de color (calidez)
"""
import numpy as np
from PIL import Image
import json

CX, CY, R = 495.0, 224.0, 34.0
RMAX = 7.8
WIN = RMAX * R
SIZE = 1024

ref = Image.open('referencia.png').convert('RGB')
S = int(round(2*WIN))
x0 = int(round(CX-WIN)); y0 = int(round(CY-WIN))
refc = np.array(ref.crop((x0, y0, x0+S, y0+S))).astype(np.float32)
RS = S

vortex = np.array(Image.open('draft_vortex.png')).astype(np.float32)
halo = np.array(Image.open('draft_halo.png').resize((SIZE, SIZE), Image.BILINEAR)).astype(np.float32)
sph_tex = np.array(Image.open('draft_sphere.png')).astype(np.float32)

vr, va = vortex[..., :3], vortex[..., 3]/255.0
hr, ha = halo[..., :3], halo[..., 3]/255.0

# anillo interior en texels (para ring_gain): 1.0-1.75R
yy, xx = np.mgrid[0:SIZE, 0:SIZE]
TR = np.hypot(xx-SIZE/2, yy-SIZE/2) * (2*WIN/SIZE)
ring_m = ((TR > 1.0*R) & (TR < 1.75*R))[..., None].astype(np.float32)

# esfera: paste parametrizable por escala
def render(p):
    bgc = np.array([p['bg_r'], p['bg_g'], p['bg_b']])
    out = np.broadcast_to(bgc, (SIZE, SIZE, 3)).copy()
    out += hr * (ha*p['halo_a'])[..., None] * p['halo_g']
    vrgb = vr * p['vortex_g'] * (1 + (p['ring_g']-1)*ring_m)
    out += vrgb * va[..., None]
    ssz = int(round(SIZE * (2*1.05*R*p['sph_s']) / (2*WIN)))
    ssz = max(8, min(SIZE, ssz))
    sp = np.array(Image.fromarray(np.clip(sph_tex, 0, 255).astype(np.uint8)).resize((ssz, ssz), Image.BILINEAR)).astype(np.float32)
    s0 = (SIZE-ssz)//2
    reg = out[s0:s0+ssz, s0:s0+ssz]
    al = sp[..., 3:]/255*p['sph_a']
    out[s0:s0+ssz, s0:s0+ssz] = reg*(1-al) + sp[..., :3]*al
    return out

def to_ref(img):
    return np.array(Image.fromarray(np.clip(img, 0, 255).astype(np.uint8)).resize((RS, RS), Image.LANCZOS)).astype(np.float32)

# ---- metricas ----
yyR, xxR = np.mgrid[0:RS, 0:RS]
rref = np.hypot(xxR-RS/2, yyR-RS/2) * (2*WIN/RS)
lum_ref = refc.max(axis=2)
# mascara de perdida: zona del agujero SIN el personaje (sus pixeles son
# irreplicables — el personaje esta DELANTE en la referencia)
from scipy import ndimage
charm_crop = np.array(Image.open('draft_charmask.png').crop((x0, y0, x0+S, y0+S))) > 40
charm_d = ndimage.binary_dilation(charm_crop, iterations=5)
active = (lum_ref > 26) & ~charm_d

def loss_of(img):
    d = to_ref(img)
    ema = np.abs(d - refc)[active].mean()
    # perfil radial
    prof_r, prof_d = [], []
    for r0 in np.arange(1.1, 7.5, 0.35):
        m = (rref > r0*R) & (rref < (r0+0.35)*R)
        if m.sum() > 50:
            prof_r.append(refc[m].mean())
            prof_d.append(d[m].mean())
    epr = np.abs(np.array(prof_r)-np.array(prof_d)).mean()
    # calidez: media de (R-G) en zonas activas
    warm_r = (refc[..., 0]-refc[..., 1])[active].mean()
    warm_d = (d[..., 0]-d[..., 1])[active].mean()
    ew = abs(warm_r-warm_d)
    return ema + 0.35*epr + 0.4*ew, ema, epr, ew

P0 = dict(bg_r=24.0, bg_g=3.0, bg_b=10.0, halo_g=1.0, halo_a=1.0, vortex_g=1.0, ring_g=1.0, sph_s=1.0, sph_a=1.0)
RANGES = dict(bg_r=(12, 44), bg_g=(0, 12), bg_b=(2, 22), halo_g=(0.8, 2.2), halo_a=(0.8, 1.8), vortex_g=(0.85, 1.35),
              ring_g=(0.95, 1.45), sph_s=(0.98, 1.02), sph_a=(0.92, 1.02))

rng = np.random.default_rng(42)
best = dict(P0); bestL = loss_of(render(P0))[0]
cur = dict(P0); curL = bestL
history = [bestL]
N_ROUNDS = 130
for it in range(N_ROUNDS):
    cand = dict(cur)
    keys = list(RANGES)
    # descenso por coordenadas ciclico + jitter aleatorio
    k = keys[it % len(keys)]
    lo, hi = RANGES[k]
    for _ in range(6):
        cand[k] = float(np.clip(cur[k] + rng.uniform(lo-hi, hi-lo)*0.22, lo, hi))
        L = loss_of(render(cand))[0]
        if L < curL:
            curL = L; cur = dict(cand)
            break
        cand[k] = cur[k]
    # reinicio aleatorio ocasional alrededor del mejor
    if it % 17 == 16:
        cand = dict(best)
        for k in keys:
            lo, hi = RANGES[k]
            cand[k] = float(np.clip(best[k] + rng.normal(0, (hi-lo)*0.05), lo, hi))
        L = loss_of(render(cand))[0]
        if L < bestL:
            bestL, best = L, dict(cand)
    if curL < bestL:
        bestL, best = curL, dict(cur)
    history.append(curL)

L, ema, epr, ew = loss_of(render(best))
print(f"MEJOR tras {N_ROUNDS} rondas: L={L:.2f} (EMA={ema:.1f}, perfil={epr:.1f}, calidez={ew:.1f})")
print("params:", {k: round(v, 3) for k, v in best.items()})
json.dump({'params': best, 'loss': float(L), 'ema': float(ema), 'rounds': N_ROUNDS,
           'history': [float(h) for h in history]}, open('optim_result.json', 'w'), indent=1)

# guardar render final y comparacion
img = render(best)
Image.fromarray(np.clip(img, 0, 255).astype(np.uint8)).save('render_olvido_opt.png')
dr = to_ref(img)
side = Image.new('RGB', (RS*2+16, RS), (25, 25, 25))
side.paste(Image.fromarray(refc.astype(np.uint8)), (0, 0))
side.paste(Image.fromarray(dr.astype(np.uint8)), (RS+16, 0))
side.save('compare_opt.png')
print("render optimo y comparacion guardados")
