#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
mock_rayos_v65022.py — v6.50.22 — EL MOCK 1:1 DE LOS RAYOS 100% CÓDIGO.

Renderiza con PIL+numpy la semántica EXACTA del motor (v6.50.9, sondeada
contra el FNA.dll): lote aditivo (SourceAlpha, One) + loader premult →

    aporte.rgb += P.rgb(x,y) · P.a(x,y) · tinte.rgb · tinte.a

con el Tinte lineal de la casa (RGB·√f, A·√f) → aporte = c.rgb·P.a·f
(el alfa del PIXEL afecta UNA vez — para MagicPixel P.a=1: aporte=c·f
EXACTO, sólido).

PANELES (el ANTES/DESPUÉS del reporte del usuario):
  1 — EL RAYO: ANTES (bandas horneadas BoltHalo/BoltCore medidas de los
      PNG v6.50.21: perfil con SUELO de alfa en los bordes) vs AHORA
      (LA PILA 6+1 del pixel del motor). Misma semilla, mismo camino.
  2 — LAS JUNTAS (zoom ×3): el fundido de 3px de la banda + el suelo de
      alfa = "líneas que se unen a otra línea" vs las colineales de la
      pila (sin costura).
  3 — EL IMPACTO: ANTES (BoltImpact v6.50.21: alfa ~29 en TODO el borde
      = EL CUADRADO) vs AHORA (perfil monotónico que MUERE a 0 exacto).

Salida: research/rayos_v65022/MOCK_RAYOS_v65022.png
"""
import os
import math
import subprocess
import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
PROJ = os.path.join(HERE, '..', 'AethonMod')
EFF = os.path.join(PROJ, 'Content', 'Effects', 'Procedural')
OUT = os.path.join(HERE, '..', 'research', 'rayos_v65022')
os.makedirs(OUT, exist_ok=True)

FONDO = (10, 9, 18)


# =====================================================================
#  EL NÚCLEO DE ADITIVO (la semántica del motor REAL, v6.50.9)
# =====================================================================

class Lienzo:
    """Buffer con blending aditivo (SourceAlpha, One) + premult del
    loader: aporte = P.rgb · P.a · tint.rgb · tint.a."""

    def __init__(self, w, h, fondo=FONDO):
        self.w, self.h = w, h
        self.buf = np.zeros((h, w, 3), dtype=np.float64)
        self.buf[:, :] = fondo

    def quad(self, perfil, center, size, rot_rad, color, f):
        """Quad centrado/rotado con perfil (HxW o escalar 1.0) 0..1,
        color (r,g,b) 0..255, TINTE LINEAL de la casa (RGB·√f, A·√f) →
        aporte = perfil·perfil·c·f si perfil es el ALFA premult... NO:
        aquí `perfil` ES P.a y P.rgb=1 (todas las texturas blancas):
        aporte = 1 · P.a · (c·√f) · (√f) = c·f·P.a."""
        if size[0] < 0.4 or size[1] < 0.4 or f <= 0.001:
            return
        r, g, b = color
        sf = math.sqrt(f)
        cr, cg, cb = r * sf, g * sf, b * sf
        ca = sf
        # la textura (perfil = alfa 0..1, blanco premult por el loader:
        # P.rgb = 1·P.a tras premult → aporte = P.a²·cr·ca... cuidado:
        # P.rgb premult = P.a. aporte = P.a · cr · ca = P.a·c·f
        # → el perfil entra UNA vez (P.a), como en el motor.
        tex8 = (np.clip(perfil, 0, 1) * 255).astype(np.uint8)
        nw = max(1, int(round(size[0])))
        nh = max(1, int(round(size[1])))
        img = Image.fromarray(tex8).resize((nw, nh), Image.BILINEAR)
        deg = math.degrees(rot_rad)
        if abs(deg) > 0.01:
            img = img.rotate(deg, resample=Image.BILINEAR, expand=True)
        patch = np.asarray(img, dtype=np.float64) / 255.0  # P.a
        ph, pw = patch.shape
        x0 = int(round(center[0] - pw / 2))
        y0 = int(round(center[1] - ph / 2))
        xa, ya = max(0, x0), max(0, y0)
        xb, yb = min(self.w, x0 + pw), min(self.h, y0 + ph)
        if xa >= xb or ya >= yb:
            return
        sub = patch[ya - y0:yb - y0, xa - x0:xb - x0]
        self.buf[ya:yb, xa:xb, 0] += sub * (cr * ca)
        self.buf[ya:yb, xa:xb, 1] += sub * (cg * ca)
        self.buf[ya:yb, xa:xb, 2] += sub * (cb * ca)

    def png(self):
        arr = np.clip(self.buf, 0, 255).astype(np.uint8)
        return Image.fromarray(arr, 'RGB')


# =====================================================================
#  LOS PERFILES MEDIDOS (los PNG reales del ANTES y el pixel del AHORA)
# =====================================================================

def perfil_de(ruta):
    im = Image.open(ruta).convert('RGBA')
    return np.asarray(im, dtype=np.float64)[..., 3] / 255.0

# ANTES: las bandas v6.50.21 (HEAD de git)
def git_show(ruta_rel, destino):
    subprocess.run(['git', 'show', f'HEAD:AethonMod/{ruta_rel}'],
                   cwd=PROJ, stdout=open(destino, 'wb'), check=True)

os.makedirs('/tmp/mock65022', exist_ok=True)
git_show('Content/Effects/Procedural/BoltHalo.png', '/tmp/mock65022/BoltHalo_old.png')
git_show('Content/Effects/Procedural/BoltCore.png', '/tmp/mock65022/BoltCore_old.png')
git_show('Content/Effects/Procedural/BoltImpact.png', '/tmp/mock65022/BoltImpact_old.png')

HALO_OLD = perfil_de('/tmp/mock65022/BoltHalo_old.png')     # 64x256 (HxW)
CORE_OLD = perfil_de('/tmp/mock65022/Core_old.png') if os.path.exists('/tmp/mock65022/Core_old.png') else perfil_de('/tmp/mock65022/BoltCore_old.png')
IMPACT_OLD = perfil_de('/tmp/mock65022/BoltImpact_old.png') # 96x96

PIXEL = np.ones((1, 1))                                      # MagicPixel 1×1
IMPACT_NEW = perfil_de(os.path.join(EFF, 'BoltImpact.png'))  # 96×96 nuevo


# =====================================================================
#  EL CAMINO (FractalPath simplificado — midpoint displacement)
# =====================================================================

def hash01(a, b, c):
    h = (a * 374761393 + b * 668265263 + c * 2147483647) & 0xFFFFFFFF
    h = (h ^ (h >> 13)) * 1274126177 & 0xFFFFFFFF
    return ((h ^ (h >> 16)) & 0xFFFFFFFF) / 0xFFFFFFFF

def fractal_path(start, end, seed, flick, gens=4, chaos=0.16):
    pts = [np.array(start, float), np.array(end, float)]
    for g in range(gens):
        nxt = [pts[0]]
        for i in range(len(pts) - 1):
            a, b = pts[i], pts[i + 1]
            m = (a + b) * 0.5
            d = b - a
            n = np.array([-d[1], d[0]])
            ln = np.linalg.norm(n)
            if ln > 1e-6:
                n = n / ln
            off = (hash01(seed, flick, g * 131 + i * 17) - 0.5) * 2
            off *= np.linalg.norm(d) * chaos
            nxt.append(m + n * off)
            nxt.append(b)
        pts = nxt
    return pts

def chaikin(pts, pasadas=1):
    for _ in range(pasadas):
        if len(pts) < 3:
            break
        out = [pts[0]]
        for i in range(len(pts) - 1):
            a, b = pts[i], pts[i + 1]
            out.append(a * 0.75 + b * 0.25)
            out.append(a * 0.25 + b * 0.75)
        out.append(pts[-1])
        pts = out
    return pts


# =====================================================================
#  EL TAPER / CRACKLE / EXT (los del código)
# =====================================================================

def taper_center(t):
    return max(0.35, math.sin(t * math.pi) ** 0.65)

def crackle(seed, flick, i):
    return 0.66 + 0.34 * hash01(seed, flick, i * 41 + 17)

def ext_junta_vieja(w, giro):
    e = w * 0.5 * math.tan(giro * 0.5) + 1.2
    return min(max(e, 1.2), w * 0.5 + 1.2)

def ext_junta_nueva(w, giro):
    e = w * 0.5 * math.tan(giro * 0.5)
    return min(max(e, 0.35), max(w * 0.5, 0.35))

def angulo(d0, d1):
    n0, n1 = np.linalg.norm(d0), np.linalg.norm(d1)
    if n0 < 1e-4 or n1 < 1e-4:
        return 0.0
    c = np.clip(np.dot(d0 / n0, d1 / n1), -1, 1)
    return math.acos(c)


# =====================================================================
#  LOS DOS MOTORES (el ANTES de bandas vs el AHORA de la pila)
# =====================================================================

def rayo_bandas(lienzo, pts, seed, flick, w, halo, core, alpha=1.0):
    """v6.50.21 — StrandImpl con BoltHalo/BoltCore (ANTES)."""
    total = sum(np.linalg.norm(pts[i+1] - pts[i]) for i in range(len(pts)-1))
    if total < 1:
        return
    arc = 0.0
    for i in range(len(pts) - 1):
        a, b = pts[i], pts[i + 1]
        seg = b - a
        ln = np.linalg.norm(seg)
        if ln < 0.35:
            arc += ln; continue
        prevv = a - pts[i - 1] if i > 0 else seg
        nextt = pts[i + 2] - b if i < len(pts) - 2 else seg
        avg = prevv / np.linalg.norm(prevv) + nextt / np.linalg.norm(nextt)
        if np.linalg.norm(avg) < 0.03:
            avg = seg
        avg = avg / np.linalg.norm(avg)
        rot = math.atan2(avg[1], avg[0])
        mid = (a + b) * 0.5
        tMid = (arc + ln * 0.5) / total
        wseg = w * taper_center(tMid)
        cr = (crackle(seed, flick, i) + crackle(seed, flick, i + 1)) * 0.5
        largo = float(np.dot(seg, avg))
        if largo < 0.35:
            arc += ln; continue
        dp, dn = angulo(prevv, seg), angulo(seg, nextt)
        # 4 capas v6.50.15: bloom 4.6 · halo 2.2 · cuerpo 1 · vena ¼ (min 2)
        for wK, fK, tex in ((wseg*4.6, 0.16, HALO_OLD), (wseg*2.2, 0.30, HALO_OLD),
                            (wseg, 0.80, CORE_OLD), (max(wseg*0.26, 2.0), 1.0, CORE_OLD)):
            ext = ext_junta_vieja(wK, dp) + ext_junta_vieja(wK, dn)
            lienzo.quad(tex, mid, (largo + ext, wK), rot, halo if wK > wseg else core,
                        (fK * alpha * cr) if wK > wseg*0.3 else (fK * alpha))
        arc += ln


PILA_W = [5.2, 3.6, 2.5, 1.7, 1.15, 0.72]
PILA_F = [0.06, 0.09, 0.13, 0.19, 0.28, 0.42]

def rayo_pila(lienzo, pts, seed, flick, w, halo, core, alpha=1.0):
    """v6.50.22 — StrandImpl 100% CÓDIGO (LA PILA del pixel del motor)."""
    total = sum(np.linalg.norm(pts[i+1] - pts[i]) for i in range(len(pts)-1))
    if total < 1:
        return
    arc = 0.0
    for i in range(len(pts) - 1):
        a, b = pts[i], pts[i + 1]
        seg = b - a
        ln = np.linalg.norm(seg)
        if ln < 0.35:
            arc += ln; continue
        prevv = a - pts[i - 1] if i > 0 else seg
        nextt = pts[i + 2] - b if i < len(pts) - 2 else seg
        avg = prevv / np.linalg.norm(prevv) + nextt / np.linalg.norm(nextt)
        if np.linalg.norm(avg) < 0.03:
            avg = seg
        avg = avg / np.linalg.norm(avg)
        rot = math.atan2(avg[1], avg[0])
        mid = (a + b) * 0.5
        tMid = (arc + ln * 0.5) / total
        wseg = w * taper_center(tMid)
        cr = (crackle(seed, flick, i) + crackle(seed, flick, i + 1)) * 0.5
        largo = float(np.dot(seg, avg))
        if largo < 0.35:
            arc += ln; continue
        dp, dn = angulo(prevv, seg), angulo(seg, nextt)
        # LA PILA: 6 pasadas sólidas + vena
        for k in range(6):
            wk = wseg * PILA_W[k]
            ext = ext_junta_nueva(wk, dp) + ext_junta_nueva(wk, dn)
            lienzo.quad(PIXEL, mid, (largo + ext, wk), rot, halo,
                        PILA_F[k] * alpha * cr)
        wv = max(wseg * 0.34, 1.5)
        extV = ext_junta_nueva(wv, dp) + ext_junta_nueva(wv, dn)
        lienzo.quad(PIXEL, mid, (largo + extV, wv), rot, core, alpha)
        arc += ln


# =====================================================================
#  LOS PANELES
# =====================================================================

W, H = 1100, 1500
sheet = Image.new('RGB', (W, H), FONDO)
from PIL import ImageDraw
draw = ImageDraw.Draw(sheet)

def titulo(x, y, txt, grande=False):
    draw.text((x, y), txt, fill=(200, 200, 210))

CYAN = (110, 220, 255)
WHITE = (255, 252, 240)

# ---------- PANEL 1: EL RAYO (antes / ahora) ----------
titulo(20, 12, "PANEL 1 - EL RAYO  (misma semilla, mismo camino fractal)")
L, R = 20, 560
for col, motor, tag in ((L, rayo_bandas, "ANTES v6.50.21 - bandas horneadas (BoltHalo/BoltCore)"),
                        (R, rayo_pila, "AHORA v6.50.22 - 100% CODIGO (pila 6+1 del pixel del motor)")):
    titulo(col, 34, tag)
    lienzo = Lienzo(520, 300)
    y0 = 60
    # 3 rayos: largo, medio, corto (con ramitas)
    for j, (x0, x1, wdt, seed) in enumerate((
            (30, 490, 6.0, 101), (60, 460, 4.0, 202), (90, 430, 2.6, 303))):
        pts = chaikin(fractal_path((x0, y0 + j * 80), (x1, y0 + j * 80 + 18), seed, 7), 1)
        motor(lienzo, pts, seed, 7, wdt, CYAN, WHITE)
        # una ramita
        mid = pts[len(pts)//2]
        rama = chaikin(fractal_path(mid, mid + np.array([55, -34]), seed + 9, 7, 3, 0.13), 0)
        motor(lienzo, rama, seed + 9, 7, wdt * 0.55, CYAN, WHITE, alpha=0.6)
    sheet.paste(lienzo.png(), (col, 56))

# ---------- PANEL 2: LAS JUNTAS (zoom x3) ----------
titulo(20, 380, "PANEL 2 - LAS JUNTAS EN ZOOM x3  ('lineas que se unen a otra linea'?)")
titulo(L, 402, "ANTES: fundido de 3px por junta + suelo de alfa 50 en los bordes de la banda")
titulo(R, 402, "AHORA: cortes colineales del pixel solido - sin costura, sin cuentas")
for col, motor in ((L, rayo_bandas), (R, rayo_pila)):
    lienzo = Lienzo(520, 220)
    pts = chaikin(fractal_path((40, 60), (480, 120), 4711, 3, 4, 0.14), 1)
    motor(lienzo, pts, 4711, 3, 7.0, CYAN, WHITE)
    sheet.paste(lienzo.png(), (col, 424))

# ---------- PANEL 3: EL IMPACTO (antes / ahora) ----------
titulo(20, 660, "PANEL 3 - EL IMPACTO  ('se ve el cuadrado cuando aparece')")
titulo(L, 682, "ANTES: alfa ~29 en todo el borde del lienzo = EL CUADRADO")
titulo(R, 682, "AHORA: perfil monotonic 236->0 que MUERE a 0 en el borde")
for col, tex in ((L, IMPACT_OLD), (R, IMPACT_NEW)):
    lienzo = Lienzo(520, 300)
    # el destello radial girando (2 escalas apiladas, como ImpactFlash)
    lienzo.quad(tex, (260, 150), (240, 240), 0.35, CYAN, 0.50)
    lienzo.quad(tex, (260, 150), (150, 150), -0.25, CYAN, 0.70)
    # la cruz de luz (SoftGlow en el juego — aqui dos tiras del pixel)
    for sx, sy in ((0.62, 2.35), (2.35, 0.62), (0.40, 1.55), (1.55, 0.40)):
        pass  # la cruz la dibuja SoftGlow (radial a 0) — no es el reporte
    # el punto cegador
    lienzo.quad(PIXEL, (260, 150), (52, 52), 0, CYAN, 0.80)
    lienzo.quad(PIXEL, (260, 150), (26, 26), 0, WHITE, 0.95)
    sheet.paste(lienzo.png(), (col, 704))

# ---------- PANEL 4: PERFIL TRANSVERSAL (la suma de la pila) ----------
titulo(20, 1020, "PANEL 4 - EL PERFIL TRANSVERSAL MEDIDO (la suma de las pasadas = el degradado)")
# medir el perfil real de cada motor: rayo horizontal corto, muestrear la columna central
def medir_perfil(motor, halo, core):
    lienzo = Lienzo(360, 120)
    pts = [np.array((10.0, 60.0)), np.array((350.0, 60.0))]
    motor(lienzo, pts, 77, 1, 6.0, halo, core)
    col = lienzo.buf[:, 180, :]
    lum = col.mean(axis=1)
    return lum

for k, (motor, tag, colx) in enumerate((
        (rayo_bandas, "ANTES (bandas)", L), (rayo_pila, "AHORA (pila)", R))):
    lum = medir_perfil(motor, CYAN, WHITE)
    # graficar el perfil (eje y invertido)
    for y in range(120):
        v = lum[y]
        vpx = min(int(v / 3.0), 200)
        draw.rectangle([colx + 20, 1044 + y, colx + 20 + vpx, 1044 + y], fill=(90, 190, 255))
    titulo(colx + 20, 1044 + 130, f"{tag}: suma en el centro "
           f"{lum.sum()/255:.2f}·color | meseta central "
           f"{lum[50:70].min()/255:.2f}-{lum[50:70].max()/255:.2f}")

# ---------- PANEL 5: firma ----------
titulo(20, 1330, "v6.50.22 - RAYOS 100% CODIGO: el pincel es TextureAssets.MagicPixel (el pixel 1x1")
titulo(20, 1352, "blanco del MOTOR) — la receta del LightningArc 466 de vanilla (0.6/0.4/0.2)")
titulo(20, 1374, "extendida a 6 pasadas + vena. CERO textura de banda, cero sprite de rayo.")
titulo(20, 1396, "BoltImpact.png re-horneado: perfil monotonic que muere a 0 EXACTO en el borde.")

path_png = os.path.join(OUT, 'MOCK_RAYOS_v65022.png')
sheet.save(path_png)
print("OK ->", path_png)

# ============ VERIFICACIÓN AUTOMÁTICA (el control sensible) ============
arr = np.asarray(sheet, dtype=np.float64)
# 1) el impacto AHORA no debe tener ningun píxel de borde de quad:
pan = arr[704:1004, 560:1080]
# el cuadrado del ANTES: buscar líneas verticales/horizontales de brillo
# (gradiente fuerte alineado a ejes) — el clásico borde de quad
gx = np.abs(np.diff(pan.mean(axis=2), axis=1)).max()
gy = np.abs(np.diff(pan.mean(axis=2), axis=0)).max()
panA = arr[704:1004, 20:540]
gxA = np.abs(np.diff(panA.mean(axis=2), axis=1)).max()
gyA = np.abs(np.diff(panA.mean(axis=2), axis=0)).max()
print(f"  gradiente max impactos ANTES (gx={gxA:.1f}, gy={gyA:.1f}) vs AHORA (gx={gx:.1f}, gy={gy:.1f})")

# 2) LAS JUNTAS: el brillo a LO LARGO del eje del rayo debe ser CONTINUO
#    (el ANTES tenia el fundido de 3px por junta = valles periodicos).
def perfil_longitudinal(motor):
    lienzo = Lienzo(520, 220)
    pts = chaikin(fractal_path((40, 110), (480, 110), 8123, 5, 4, 0.10), 1)
    motor(lienzo, pts, 8123, 5, 7.0, CYAN, WHITE)
    # muestrear la intensidad a lo largo del eje Y central del rayo
    line = lienzo.buf[:, :, :].mean(axis=2)
    # para cada x, el brillo maximo en la columna (el eje del rayo)
    return np.array([line[:, x].max() for x in range(40, 470)])

lumA = perfil_longitudinal(rayo_bandas)
lumN = perfil_longitudinal(rayo_pila)
# detectar VALLES (caidas locales fuertes = juntas oscuras)
dA = np.abs(np.diff(lumA)); dN = np.abs(np.diff(lumN))
vallesA = (dA > 18).sum(); vallesN = (dN > 18).sum()
print(f"  saltos de brillo a lo largo del rayo (>18/255 por px): "
      f"ANTES={vallesA}  AHORA={vallesN}")
print(f"  brillo medio del eje: ANTES={lumA.mean():.1f}  AHORA={lumN.mean():.1f}")
print("OK -> el control automatico corrio (ver cifras arriba y el VLM del panel)")
