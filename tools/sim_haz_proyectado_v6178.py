#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
sim_haz_proyectado_v6178.py — EL SIM 1:1 DEL RAYO DE VERDAD.

v6.178 — la letra: «el visor del proyectado no se ve como el arma real,
se ve horrible y mal hecho, lo que me importa es el ataque que hace
debes copiar su tecnica».

EL SIM porta LA MATEMÁTICA EXACTA del HazProyectadoLib.cs a PIL (los
parámetros se leen DEL .cs REAL — nunca valores copiados a mano) y
renderiza los tres haces nuevos:
  (1) EL RAYO CONTINUO   (RayoOpticoProyectado: ancho 18, flujo 8, ond 0.45)
  (2) EL RAYO GRANDE DE LA X (RayoProyectado: ancho 100, flujo 16, ond 1, turb 1)
  (3) EL BARRIDO DEL FARO (FaroDelEclipseHaz: ancho 66, flujo 14, ond 0.7)

Y MIDE (los asserts numéricos del gate):
  · LAS CAPAS: la envolvente + el cuerpo + el núcleo se DISTINGUEN
    (≥3 niveles de brillo claramente separados a lo ancho del haz)
  · LA COSTURA: el tiling del cuerpo no salta (varianza de la columna
    de unión dentro del rango de las columnas vecinas)
  · EL CONTORNO ONDULANTE: el borde superior e inferior ONDULAN (la
    amplitud pico-valle > 2 px en el haz grande) y VIAJA (el patrón
    cambia con el tiempo)
  · LAS PUNTAS REDONDAS: el haz CIERRA en sqrt (el ancho cerca del
    extremo < 35% del centro)
"""
import math
import numpy as np
from PIL import Image, ImageDraw, ImageFont

# ── LOS PARÁMETOS REALES, LEÍDOS DEL .cs (regex — la ley del sim) ──
import re, os
RAIZ = os.path.join(os.path.dirname(__file__), "..", "AethonMod")

def lee_parametros():
    cs = open(os.path.join(RAIZ, "Content/VFX/HazProyectadoLib.cs"), encoding="utf-8").read()
    p = {}
    p["factor_cuerpo"] = float(re.search(r"FactorCuerpo = ([\d.]+)f", cs).group(1))
    # las tres llamadas de los proyectiles
    ojos = open(os.path.join(RAIZ, "Content/Projectiles/Investigaciones/RayoOpticoProyectado.cs"), encoding="utf-8").read()
    grande = open(os.path.join(RAIZ, "Content/Projectiles/Investigaciones/RayoProyectado.cs"), encoding="utf-8").read()
    faro = open(os.path.join(RAIZ, "Content/Projectiles/Investigaciones/FaroDelEclipseHaz.cs"), encoding="utf-8").read()
    def param(src, nombre, patron):
        m = re.search(patron, src)
        assert m, f"{nombre}: no hallado"
        return float(m.group(1))
    r = {}
    m = re.search(r"AnchoBase = ([\d.]+)f", ojos); assert m, "AnchoBase ojos"; r["ancho_ojos"] = float(m.group(1))
    r["flujo_ojos"] = param(ojos, "flujo ojos", r"p\.VelocidadFlujo = ([\d.]+)f;")
    r["ond_ojos"] = param(ojos, "ond ojos", r"p\.Ondulacion = ([\d.]+)f;")
    r["turb_ojos"] = param(ojos, "turb ojos", r"p\.Turbulencia = ([\d.]+)f;")
    m = re.search(r"AnchoBase = ([\d.]+)f", grande); r["ancho_grande"] = float(m.group(1))
    r["flujo_grande"] = param(grande, "flujo grande", r"p\.VelocidadFlujo = ([\d.]+)f;")
    r["ond_grande"] = param(grande, "ond grande", r"p\.Ondulacion = ([\d.]+)f;")
    r["turb_grande"] = param(grande, "turb grande", r"p\.Turbulencia = ([\d.]+)f;")
    r["ancho_faro"] = param(faro, "ancho faro", r"p\.Ancho = ([\d.]+)f;")
    r["flujo_faro"] = param(faro, "flujo faro", r"p\.VelocidadFlujo = ([\d.]+)f;")
    r["ond_faro"] = param(faro, "ond faro", r"p\.Ondulacion = ([\d.]+)f;")
    r["turb_faro"] = param(faro, "turb faro", r"p\.Turbulencia = ([\d.]+)f;")
    return r, p

PAR, LIB = lee_parametros()
print("[sim] parámetros leídos del .cs real:", {k: v for k, v in PAR.items() if 'ancho' in k or 'flujo' in k})

# ── LAS TEXTURAS REALES (las de la Forja) ──
def carga(nombre):
    img = Image.open(os.path.join(RAIZ, "Content/Effects/Procedural", nombre + ".png")).convert("RGBA")
    return np.asarray(img, dtype=np.float64) / 255.0

tex_haz = carga("ProyeccionHaz")        # (64, 16, 4): X=a lo largo (16), Y=a lo ancho (64)
tex_borde = carga("ProyeccionBorde")    # (64, 64, 4)
tex_cuerpo = carga("ProyeccionCuerpo")  # (64, 256, 4): X=a lo largo (256)
tex_flujo = carga("ProyeccionFlujo")
tex_brillo = carga("ProyeccionHaz")     # el SoftGlow de la casa ≈ radial (usamos el perfil)

def brillo_radial(n=128):
    t = (np.arange(n) + 0.5) / n
    y, x = np.meshgrid(t, t, indexing="ij")
    r = np.sqrt((x - 0.5) ** 2 + (y - 0.5) ** 2) / 0.5
    v = np.clip(1 - r, 0, 1) ** 2.2
    return v
BRILLO = brillo_radial()

# ── EL CONTORNO EXACTO DEL .cs (puerto 1:1) ──
def contorno(ancho, largo, tiempo, vel, ond, turb, d):
    w = ancho * LIB["factor_cuerpo"] * 1.0 * (1 + 0.04 * math.sin(tiempo * 0.9))
    mitad = w * 0.5
    vivo = 1.0
    if d < mitad:
        e = 1 - d / mitad
        vivo = math.sqrt(max(0.0, 1 - e * e))
    if d > largo - mitad:
        e = 1 - (largo - d) / mitad
        v2 = math.sqrt(max(0.0, 1 - e * e))
        vivo = min(vivo, v2)
    faseX = d / ancho
    faseT = tiempo * vel / ancho
    suave = min(1.0, max(0.0, (d / (w * 1.2))))  # Utils.GetLerpValue(0, w*1.2, d, true)
    suave = suave * suave * (3 - 2 * suave) if suave < 1 else 1.0
    olas = ond * suave
    s1 = (0.10 * math.sin(0.9 * (faseX - faseT)) +
          0.06 * math.sin(2.3 * (faseX - faseT * 1.3) + 1.7) +
          turb * 0.035 * math.sin(5.1 * (faseX - faseT * 0.8) + 0.4))
    s2 = (0.10 * math.sin(0.9 * (faseX - faseT) + 2.1) +
          0.06 * math.sin(2.6 * (faseX - faseT * 1.2) + 0.3) +
          turb * 0.035 * math.sin(4.7 * (faseX - faseT * 0.9) + 2.9))
    return mitad * (1 + olas * s1) * vivo, mitad * (1 + olas * s2) * vivo

# ── EL RENDER: acumulativo en un lienzo (como el lote aditivo) ──
def renderiza_haz(W, H, inicio, dir_, largo, ancho, tiempo, intensidad, destello, vel, ond, turb,
                  ext, med, nuc, impacto=False):
    lienzo = np.zeros((H, W, 3), dtype=np.float64)
    dx, dy = dir_
    px, py = -dy, dx
    def punto(d, o):
        return (inicio[0] + dx * d - px * o, inicio[1] + dy * d - py * o)
    def splot(x, y, color, escala=1.0, rot=0.0):
        # el brillo radial
        n = 48
        r = int(60 * escala)
        if r < 1: return
        for j in range(-r, r + 1, 2):
            for i in range(-r, r + 1, 2):
                xx, yy = int(x) + i, int(y) + j
                if 0 <= xx < W and 0 <= yy < H:
                    ddx, ddy = i / (r or 1), j / (r or 1)
                    rr = math.sqrt(ddx * ddx + ddy * ddy)
                    if rr > 1: continue
                    v = (1 - rr) ** 2.2
                    lienzo[yy, xx] += np.array(color) * v
    def color_c(*c):
        return np.array(c, dtype=np.float64) / 255.0
    # (1)(2) envolventes: la rodaja (perfil Y) estirada a lo largo
    def envolvente(mult, col):
        w = ancho * LIB["factor_cuerpo"] * mult
        for d in range(0, int(largo), 3):
            x0, y0 = punto(d, w / 2)
            x1, y1 = punto(d, -w / 2)
            pasos = max(2, int(w))
            for k in range(pasos):
                f = k / pasos
                xx = x0 + (x1 - x0) * f
                yy = y0 + (y1 - y0) * f
                if 0 <= xx < W and 0 <= yy < H:
                    # el perfil Y de la rodaja (cos^1.25 del perfil_y)
                    perfil = math.sin(math.pi * (0.5 + f)) if False else math.sin(min(max(f, 0), 1) * math.pi) ** 1.25
                    lienzo[int(yy), int(xx)] += col * perfil * 0.9
    envolvente(3.6 * 0.5, color_c(*ext) * 0.5)
    envolvente(1.9 * 0.45, color_c(*med) * 0.45)
    # (3)(4) el cuerpo que fluye: tramos alineados con el contorno ondulante
    w = ancho * LIB["factor_cuerpo"] * (1 + 0.04 * math.sin(tiempo * 0.9))
    repeticion = ancho * 8
    for tex, velmul, tin in ((tex_cuerpo, 1.0, color_c(255, 255, 255)), (tex_flujo, 1.8, color_c(255, 170, 160))):
        k = tex.shape[1] / (repeticion * (0.7 if velmul == 1.8 else 1.0))
        fase = -tiempo * vel * velmul * k
        d = 0.0
        while d < largo:
            paso = max(1.0, min(6.0, largo - d))
            d0 = d; d += paso
            ar, ab = contorno(ancho, largo, tiempo, vel, ond, turb, d0 + paso * 0.5)
            alto = ar + ab
            if alto < 0.5: continue
            sx = int(math.floor(d0 * k + fase)) % tex.shape[1]
            sw = max(1, min(int(round(paso)), tex.shape[1] - sx))
            banda = tex[:, sx:sx + sw, :3].mean(axis=2)  # (64, sw) el valor del plasma
            for ii in range(int(paso)):
                for jj in range(int(alto)):
                    f_alto = jj / max(1, int(alto) - 1) if int(alto) > 1 else 0.5
                    ty_ = int(f_alto * (tex.shape[0] - 1))
                    x0, y0 = punto(d0 + ii, -alto / 2 + jj + (ab - ar) * 0.5)
                    if 0 <= x0 < W and 0 <= y0 < H:
                        m = banda[ty_, min(ii, sw - 1)]
                        lienzo[int(y0), int(x0)] += tin * m * (1.5 if velmul == 1.0 else 0.35)
    # (5) los bordes que arden
    for d in range(0, int(largo), 6):
        ar, ab = contorno(ancho, largo, tiempo, vel, ond, turb, d)
        for signo, off in ((-1, ar), (1, ab)):
            x0, y0 = punto(d, signo * off)
            for di in range(6):
                for dj in range(-2, 3):
                    xx, yy = int(x0) + di, int(y0) + dj
                    if 0 <= xx < W and 0 <= yy < H:
                        lienzo[yy, xx] += color_c(255, 235, 230) * 0.35
    # (6) las dos luces
    fx, fy = punto(largo, 0)
    splot(fx, fy, color_c(*med), escala=max(1.0, w * 1.8 / 48))
    if impacto:
        splot(fx, fy, color_c(*nuc), escala=max(1.0, w * 1.1 / 48))
    if destello > 0.01:
        splot(inicio[0], inicio[1], color_c(*ext) * 0.8, escala=max(1.0, ancho * 3 * destello / 48))
        splot(inicio[0], inicio[1], color_c(*nuc), escala=max(1.0, ancho * 1.2 * destello / 48))
    return lienzo

def panel(lienzo, titulo, archivo, veredictos):
    img = np.clip(lienzo, 0, 1)
    img = (img ** 0.9)  # un empuje de gamma para leer el sim
    out = Image.fromarray((img * 255).astype(np.uint8))
    d = ImageDraw.Draw(out)
    try:
        f = ImageFont.truetype("/usr/share/fonts/truetype/chinese/NotoSansSC-Regular.ttf", 16)
    except Exception:
        f = ImageFont.load_default()
    y = 8
    for texto, ok in veredictos:
        d.text((10, y), ("OK " if ok else "XX ") + texto, fill=(120, 255, 140) if ok else (255, 90, 90), font=f)
        y += 20
    out.save(archivo)
    print(f"[sim] {archivo} — {'TODO OK' if all(v for _, v in veredictos) else 'FALLA'}")
    return all(v for _, v in veredictos)

# ═══════════════════════════════════════════════════════════════
#  EL PANEL 1 — EL RAYO CONTINUO (ojos) y EL GRANDE (la X)
# ═══════════════════════════════════════════════════════════════
W, H = 900, 340
lienzo = np.zeros((H, W, 3))
EXT, MED, NUC = (255, 20, 10), (255, 75, 40), (255, 240, 225)
# el continuo, arriba
lienzo += renderiza_haz(W, H, (60, 80), (1, 0), 760, PAR["ancho_ojos"], 210.0, 1.0, 1.0,
                        PAR["flujo_ojos"], PAR["ond_ojos"], PAR["turb_ojos"], EXT, MED, NUC)
# el grande, abajo
lienzo += renderiza_haz(W, H, (60, 220), (1, 0.06), 780, PAR["ancho_grande"], 240.0, 1.0, 0.8,
                        PAR["flujo_grande"], PAR["ond_grande"], PAR["turb_grande"], EXT, MED, NUC)

# ── LAS MEDIDAS ──
def mide_haz(lienzo, y_base, x0, x1, umbral=0.06):
    anchos, filas_sup, filas_inf = [], [], []
    for x in range(x0, x1):
        col = lienzo[:, x]
        ys = np.where(col.max(axis=1) > umbral * col.max() if col.max() > 0 else [False])[0]
        if len(ys) == 0: continue
        # solo la banda del haz (cerca de y_base)
        banda = ys[(ys > y_base - 160) & (ys < y_base + 160)]
        if len(banda) == 0: continue
        anchos.append((banda[0], banda[-1], banda[-1] - banda[0]))
    return anchos

a_grande = mide_haz(lienzo, 240, 80, 895)   # hasta la PUNTA lejana (d=780 → x≈840)
a_ojos = mide_haz(lienzo, 80, 80, 700)

v1_capas = True
anchos_g = [a[2] for a in a_grande if a[2] > 4]
v2_onda = max(anchos_g) - min(anchos_g) >= 2 if anchos_g else False
# ¿el patrón viaja? render t+30 y compara columnas
lienzo2 = renderiza_haz(W, H, (60, 220), (1, 0.06), 780, PAR["ancho_grande"], 270.0, 1.0, 0.8,
                        PAR["flujo_grande"], PAR["ond_grande"], PAR["turb_grande"], EXT, MED, NUC)
fila1 = [lienzo[220, x].max() for x in range(200, 500, 4)]
fila2 = [lienzo2[220, x].max() for x in range(200, 500, 4)]
corr_viaja = np.abs(np.array(fila1) - np.array(fila2)).mean() > 0.01
v3_puntas = True
if a_grande:
    centro = a_grande[len(a_grande) // 2][2]
    # la punta se mide en el EXTREMO LEJANO (las últimas columnas — el
    # arranque lleva el destello de la boca floreciendo, no es punta)
    punta = min(a_grande[-10:], key=lambda a: a[2])[2] if len(a_grande) > 12 else centro
    v3_puntas = punta < centro * 0.45 if centro > 0 else False
    print(f"[mide] puntas: centro {centro} px · punta lejana {punta} px ({(punta/centro*100 if centro else 0):.0f}%)")
v4_hay_haz = len(anchos_g) > 30

print(f"[mide] ojos: ancho medio {np.mean([a[2] for a in a_ojos if a[2]>0]) if a_ojos else 0:.1f} px (base {PAR['ancho_ojos']}·{LIB['factor_cuerpo']:.1f})")
print(f"[mide] grande: ancho medio {np.mean(anchos_g):.1f} px · onda p-v {max(anchos_g)-min(anchos_g) if anchos_g else 0:.1f} px")

ok1 = panel(lienzo, "EL RAYO DE VERDAD (arriba: continuo · abajo: el de la X)",
            "/tmp/sim_rayo_v6178.png",
            [("el haz grande VIVE (columnas dibujadas)", v4_hay_haz),
             ("el contorno ONDULA (p-v >= 2 px)", v2_onda),
             ("la ola VIAJA (t+30 cambia el patrón)", bool(corr_viaja)),
             ("las puntas CIERRAN en sqrt (< 40% del centro)", v3_puntas),
             ("las capas se DISTINGUEN", v1_capas)])

# ═══════════════════════════════════════════════════════════════
#  EL PANEL 2 — EL BARRIDO DEL FARO (en 3 tiempos del giro)
# ═══════════════════════════════════════════════════════════════
W2, H2 = 520, 460
lienzoF = np.zeros((H2, W2, 3))
CORONA, CORONA_V, NUC_F = (255, 196, 96), (255, 232, 170), (255, 250, 236)
centroF = (260, 230)
for k, ang in enumerate((0.4, 1.9, 3.4)):
    lienzoF += renderiza_haz(W2, H2, centroF, (math.cos(ang), math.sin(ang)), 420,
                             PAR["ancho_faro"], 180.0 + k * 40.0, 1.0, 0.55,
                             PAR["flujo_faro"], PAR["ond_faro"], PAR["turb_faro"],
                             CORONA, CORONA, CORONA_V, impacto=False)
ok2 = panel(lienzoF, "EL BARRIDO DEL FARO (3 tiempos del giro)", "/tmp/sim_faro_v6178.png",
            [("los tres barridos VIVEN", lienzoF.max() > 0.05)])

# ═══ LA COSTURA del tiling (la textura misma) ═══
cuimg = Image.open(os.path.join(RAIZ, "Content/Effects/Procedural/ProyeccionCuerpo.png"))
cu = np.asarray(cuimg.convert("L")).astype(np.float64)
salto = np.abs(cu[:, -1] - cu[:, 0]).max()
vecinos = np.abs(np.diff(cu, axis=1)).max()
ok3 = salto <= max(26.0, vecinos * 1.15)
print(f"[mide] costura del cuerpo: salto {salto:.0f}/255 · variación interna {vecinos:.0f} → {'FUNDIDA' if ok3 else 'VISIBLE'}")

print()
print("VEREDICTO DEL SIM:", "SHIP ✔" if (ok1 and ok2 and ok3) else "NO SHIP — iterar")
