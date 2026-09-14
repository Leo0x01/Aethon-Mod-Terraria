#!/usr/bin/env python3
# mock_bruma_v625.py — VERIFICACIÓN VISUAL DEL FIX PREMULTIPLICADO.
# Replica EXACTO el pipeline de render de BrumaFX/BrumaBrushes:
#   - hornea el puff fBm (falloff radial × ByMids(WarpedFbm)) como tML
#   - lo dibuja en un lote ADITIVO (Blend One/One: src.rgb solo) y en un
#     lote ALFA (One / InverseSourceAlpha: premultiplicado)
#   - VIEJO: RGB=255 constante + alfa variable (el bug de los rectángulos)
#   - NUEVO: RGB = blanco × alfa (premultiplicado — el fix v6.25)
import math, random
from PIL import Image, ImageDraw, ImageFilter

W, H = 440, 320

# ---------- el ruido (réplica del BrumaNoise de la casa) ----------
def _hash(x, y, seed):
    h = (seed * 374761393 + x * 668265263 + y * 1911520717) & 0xFFFFFFFF
    if h >= 0x80000000: h -= 0x100000000
    h ^= (h >> 13) & 0xFFFFFFFF
    if h < 0: h &= 0xFFFFFFFF
    h = (h * 1274126177) & 0xFFFFFFFF
    h ^= (h >> 16) & 0xFFFFFFFF
    return (h & 0xFFFFFF) / 16777216.0

def _quintic(t): return t*t*t*(t*(t*6-15)+10)

def value_noise(x, y, seed):
    xi, yi = math.floor(x), math.floor(y)
    tx, ty = _quintic(x-xi), _quintic(y-yi)
    a=_hash(xi,yi,seed); b=_hash(xi+1,yi,seed); c=_hash(xi,yi+1,seed); d=_hash(xi+1,yi+1,seed)
    ab=a+(b-a)*tx; cd=c+(d-c)*tx
    return ab+(cd-ab)*ty

def fbm(x, y, seed, octaves=4):
    s=0.0; amp=0.5; norm=0.0; f=1.0
    for i in range(octaves):
        s+=amp*value_noise(x*f,y*f,seed+i*101); norm+=amp; amp*=0.5; f*=2.0
    return s/norm

def warped_fbm(x,y,seed,warp=3.0):
    qx=fbm(x,y,seed); qy=fbm(x+5.2,y+1.3,seed)
    return fbm(x+warp*qx, y+warp*qy, seed+7)

def smoothstep(e0,e1,t):
    t=max(0.0,min(1.0,(t-e0)/(e1-e0)))
    return t*t*(3-2*t)

# ---------- el horneado del pincel (BrumaBrushes.Hornear) ----------
SIZE=128
def bake(frame=0, drift=0.0, contraste=1.6):
    """Devuelve (alfa[][]) del puff — la receta exacta de la casa."""
    cx=cy=SIZE*0.5
    alpha=[[0.0]*SIZE for _ in range(SIZE)]
    for j in range(SIZE):
        for i in range(SIZE):
            d=math.hypot(i+0.5-cx, j+0.5-cy)/(SIZE*0.5)
            falloff=1.0-smoothstep(0.55,1.0,d)
            u=i*4.0/SIZE+drift; v=j*4.0/SIZE+drift*0.7
            n=warped_fbm(u,v,977)
            n=0.5+(n-0.5)*contraste
            alpha[j][i]=max(0.0,min(1.0,falloff*n*1.35))
    return alpha

print("Horneando el pincel fBm (como BrumaBrushes)...")
A=bake(drift=0.0)

# ---------- el glow suave (SoftGlow de 64px) ----------
def softglow(size=64):
    g=[[0.0]*size for _ in range(size)]
    cx=cy=size*0.5
    for j in range(size):
        for i in range(size):
            d=math.hypot(i+0.5-cx,j+0.5-cy)/(size*0.5)
            g[j][i]=max(0.0,1.0-d)**1.6
    return g
G=softglow()

# ---------- LOS PUFFS de la escena (como el Eclipse DrawVoidSmoke) ----------
random.seed(7)
puffs=[]
for i in range(7):
    ang=random.random()*6.283
    dist=random.uniform(10,60)
    r=random.uniform(22,46)
    col=(150,110,200) if i%2 else (170,140,220)
    puffs.append((W*0.30+math.cos(ang)*dist, H*0.5+math.sin(ang)*dist*0.8, r, col))
# masa central
puffs.append((W*0.30, H*0.52, 58, (120,88,168)))

def render(mode, premult, tint_rgb_scale=True):
    """mode: 'additive' (One,One) o 'alpha' (One,InvSrcAlpha)."""
    img=Image.new("RGB",(W,H),(16,14,22))     # fondo nocturno del juego
    px=img.load()
    # acumulamos en float
    acc=[[ [0.0,0.0,0.0] for _ in range(W)] for _ in range(H)]
    dst=[[ [16.0,14.0,22.0] for _ in range(W)] for _ in range(H)]
    for (cx,cy,r,col) in puffs:
        ca=0.35  # alpha del puff (Tint)
        # el TINTE premultiplicado v6.25: RGB×f además de alfa×f
        f = ca if tint_rgb_scale else 1.0
        for j in range(max(0,int(cy-r)),min(H,int(cy+r))):
            for i in range(max(0,int(cx-r)),min(W,int(cx+r))):
                # el frame del pincel: mapa de alfa
                u=int((i-cx+r)/ (2*r) * SIZE); v=int((j-cy+r)/(2*r)*SIZE)
                if not (0<=u<SIZE and 0<=v<SIZE): continue
                a=A[v][u]
                # sub-blob suave (SoftGlow) al 40% del radio
                bu=(i-cx)/(r*0.8)+0.5; bv=(j-cy)/(r*0.8)+0.5
                if 0<=bu<1 and 0<=bv<1:
                    gs=G[int(bv*63)][int(bu*63)]
                    a=max(a, gs*0.45)
                if a<=0.003: continue
                if premult:
                    srcR=col[0]/255*a*f; srcG=col[1]/255*a*f; srcB=col[2]/255*a*f
                else:
                    srcR=col[0]/255*f; srcG=col[1]/255*f; srcB=col[2]/255*f
                if mode=='additive':
                    acc[j][i][0]+=srcR*255; acc[j][i][1]+=srcG*255; acc[j][i][2]+=srcB*255
                else:
                    sa=a*ca if premult else a*ca  # src.a = tex.a × tint.a
                    # premultiplicado CORRECTO: src.rgb YA lleva el alfa
                    if premult:
                        srca=a*f
                        d=dst[j][i]
                        dst[j][i]=[col[0]*srca + d[0]*(1-srca),
                                   col[1]*srca + d[1]*(1-srca),
                                   col[2]*srca + d[2]*(1-srca)]
                    else:
                        # VIEJO: rgb LLENO + solo el alfa protege al dst →
                        # sobrerresplandor de borde duro
                        d=dst[j][i]
                        dst[j][i]=[col[0]*f + d[0]*(1-a*ca),
                                   col[1]*f + d[1]*(1-a*ca),
                                   col[2]*f + d[2]*(1-a*ca)]
    for j in range(H):
        for i in range(W):
            if mode=='additive':
                d=dst[j][i]; a=acc[j][i]
                px[i,j]=(int(min(255,d[0]+a[0])),int(min(255,d[1]+a[1])),int(min(255,d[2]+a[2])))
            else:
                d=dst[j][i]
                px[i,j]=(int(min(255,d[0])),int(min(255,d[1])),int(min(255,d[2])))
    return img

print("Renderizando VIEJO (el bug)...")
old_add=render('additive', premult=False, tint_rgb_scale=False)
old_alp=render('alpha', premult=False, tint_rgb_scale=False)
print("Renderizando NUEVO (el fix v6.25)...")
new_add=render('additive', premult=True)
new_alp=render('alpha', premult=True)

# ---------- composición final 2×2 ----------
canvas=Image.new("RGB",(W*2+30,H*2+30),(8,8,12))
dr=ImageDraw.Draw(canvas)
canvas.paste(old_add,(10,10)); canvas.paste(new_add,(W+20,10))
canvas.paste(old_alp,(10,H+20)); canvas.paste(new_alp,(W+20,H+20))
dr.text((14,6),"VIEJO — aditivo (rectangulos)",fill=(255,90,90))
dr.text((W+24,6),"NUEVO v6.25 — aditivo (premult)",fill=(90,255,140))
dr.text((14,H+16),"VIEJO — alfa (bordes duros)",fill=(255,90,90))
dr.text((W+24,H+16),"NUEVO v6.25 — alfa (suave)",fill=(90,255,140))
canvas.save("/tmp/mock_bruma_v625.png")
print("Listo: /tmp/mock_bruma_v625.png")
