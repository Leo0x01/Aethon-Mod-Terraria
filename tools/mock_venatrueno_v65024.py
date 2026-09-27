#!/usr/bin/env python3
# mock_venatrueno_v65024.py — v6.50.24 — EL MOCK 1:1 DEL TRÍO VENA TRUENO.
# Porta a Python la matemática EXACTA de RayoLib.cs (el LCG32 de vanilla +
# GenerarBolt + la ola + el taper) y compone EL TRÍO DE CORALITE: TRES
# canales con semillas distintas (1 NARANJA 219,114,22 + 2 AMARILLOS
# 255,202,101 — los colores EXACTOS de ThunderveinDragon.cs), cada uno
# con LA PILA 6+1 del pixel del motor (pasadas sólidas de ancho
# telescópico cuya SUMA es el degradado transversal + la VENA blanca) y
# el tinte aditivo lineal de la casa. TRES FASES del ThunderFalling:
# NACIENDO (el frente desciende, ancho fino), PLENO (la caída entera,
# ancho ×2) y MURIENDO (la desintegración: el sobre del meandro ABIERTO
# ×3.5, el ancho colapsando, la retirada de la ola).
# Salida: research/venatrueno_v65024/MOCK_VENATRUENO_v65024.png

import math, os, random
import numpy as np
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, '..', 'research', 'venatrueno_v65024')
os.makedirs(OUT, exist_ok=True)

FONDO = (16, 15, 24)   # la noche de Terraria

# ---------------- LA MATEMÁTICA DE RayoLib.cs (puerto exacto) ----------------
class Lcg32:
    def __init__(self, seed): self.state = seed & 0xFFFFFFFF
    def next_double(self):
        self.state = (self.state * 2438992949 + 1) & 0xFFFFFFFF
        return self.state / 4294967296.0
    def next_float(self): return float(self.next_double())

def remap(v, a, b, c, d):
    if b - a < 1e-4: return c if v < a else d
    t = max(0.0, min(1.0, (v - a) / (b - a)))
    return c + (d - c) * t

def clamp(v, a, b): return max(a, min(b, v))
def wrap_angle(a):
    while a > math.pi: a -= 2 * math.pi
    while a < -math.pi: a += 2 * math.pi
    return a
def rot_by(v, ang):
    c, s = math.cos(ang), math.sin(ang)
    return (v[0] * c - v[1] * s, v[0] * s + v[1] * c)

# EL PRESET Tormenta DE RayoLib (el rayo del clima de vanilla)
TORMENTA = dict(limite_origen=0.34906587, longitud=1000, fuerza_rot=0.9,
                paso=8, capas=4, factor_capas=1.5, desvio=5.0, azar_tras=0.8,
                umbral=0.65, reflejo=0.4, rot_h=0.9, paso_h=0.8, largo_h=0.8,
                max_horquillas=2, max_prof=2, hr_min=0.3, hr_max=0.8,
                colisiona=True)

class Bolt:
    def __init__(self, puntos, rango, profundidad):
        self.puntos = puntos; self.rango = rango; self.profundidad = profundidad
        self.rotaciones = None

def elegir_capa(r, chance, capas):
    if chance <= 0: return False, 0
    capa = 0
    while capa < capas:
        if r >= 1.0 - chance: return True, capa
        r /= chance; capa += 1
    return False, 0

def generar_bolt(P, bolts, seed, prof, inicio, blanco, fuerza_rot, paso, rango):
    rng = Lcg32(seed)
    rot = 0.0
    capas = [0.0] * P["capas"]
    pos = inicio
    cuerda = (blanco[0] - inicio[0], blanco[1] - inicio[1])
    lc = math.hypot(*cuerda)
    if lc < 1e-3: lc = 1e-3
    eje = (cuerda[0] / lc, cuerda[1] / lc)
    perp = (eje[1], -eje[0])
    capacidad = int(max(lc * 2 / paso, 1))
    puntos = []
    horquillas = 0
    bolt = Bolt(puntos, rango, prof)
    for _ in range(capacidad):
        puntos.append(pos)
        al = (blanco[0] - pos[0], blanco[1] - pos[1])
        adelante = al[0] * eje[0] + al[1] * eje[1]
        if adelante < paso: break
        progreso = clamp(1 - adelante / lc, 0, 1)
        rumbo = (al[0] / math.hypot(*al), al[1] / math.hypot(*al))
        timon = clamp(-(rumbo[0] * perp[0] + rumbo[1] * perp[1]) /
                      max(0.01, min(progreso, 1 - progreso) * P["desvio"] * 2), -1, 1)
        ok, capa = elegir_capa(rng.next_double(), 0.5, P["capas"])
        if ok:
            fuerza = fuerza_rot
            for k in range(P["capas"] - 1, capa, -1): fuerza /= P["factor_capas"]
            azar = rng.next_double() * 2 - 1
            objetivo = (azar + (timon - azar * abs(timon)) / 2) * fuerza
            delta = objetivo - capas[capa]
            rot += delta; capas[capa] = objetivo
            if capa == P["capas"] - 1 and bolts is not None:
                dado = rng.next_float()
                permiso = remap(horquillas, 0, P["max_horquillas"], 1, 0)
                reflejo = rot - delta * (1 + P["reflejo"])
                if (abs(delta) >= fuerza_rot * P["umbral"] and
                        P["hr_min"] <= progreso <= P["hr_max"] and
                        prof < P["max_prof"] and dado < permiso and abs(reflejo) < 1.3962634):
                    horquillas += 1
                    largo_h = (1 - progreso) * P["largo_h"]
                    dir_h = rot_by(rumbo, reflejo)
                    blanco_h = (pos[0] + dir_h[0] * lc * largo_h, pos[1] + dir_h[1] * lc * largo_h)
                    generar_bolt(P, bolts, (rng.state + 1) & 0xFFFFFFFF, prof + 1,
                                 pos, blanco_h, fuerza_rot * P["rot_h"], paso * P["paso_h"],
                                 (rango[0] + (rango[1] - rango[0]) * progreso,
                                  rango[0] + (rango[1] - rango[0]) * clamp(progreso + largo_h, 0, 1)))
        chance = remap(progreso, P["azar_tras"], 1, 0, 1) + remap(abs(timon), 0.5, 1, 0, 1)
        ok2, capa_alta = elegir_capa(rng.next_double(), chance, P["capas"])
        if ok2:
            rot -= capas[P["capas"] - 1 - capa_alta]
            capas[P["capas"] - 1 - capa_alta] = 0.0
        pv = rot_by(rumbo, rot)
        pos = (pos[0] + pv[0] * paso, pos[1] + pv[1] * paso)
    if bolts is not None and (prof == 0 or len(puntos) > 2):
        bolts.append(bolt)
    return bolt

def generar(P, bolts, semilla, blanco, direccion=None):
    dirv = direccion if direccion is not None else (0.0, 1.0)
    dirv = rot_by(dirv, (Lcg32(semilla).next_double() * 2 - 1) * P["limite_origen"])
    dirv = (dirv[0] * P["longitud"], dirv[1] * P["longitud"])
    return generar_bolt(P, bolts, semilla, 0, (blanco[0] - dirv[0], blanco[1] - dirv[1]), blanco,
                        P["fuerza_rot"], P["paso"], (0.0, 1.0))

def calc_rotaciones(puntos):
    n = len(puntos)
    rots = [0.0] * n
    if n < 2: return rots
    rots[0] = math.atan2(puntos[0][1] - puntos[1][1], puntos[0][0] - puntos[1][0])
    prev = rots[0]
    for i in range(1, n - 1):
        a = math.atan2(puntos[i][1] - puntos[i + 1][1], puntos[i][0] - puntos[i + 1][0])
        rots[i] = prev + wrap_angle(a - prev) / 2
        prev = a
    rots[n - 1] = prev
    antes = rots[0]
    for i in range(1, n - 1):
        a, b = rots[i], rots[i + 1]
        rots[i] = a + (wrap_angle(antes - a) + wrap_angle(b - a)) / 2
        antes = a
    return rots

# ---------------- LA OLA (VenaTrueno) ----------------
def ola_transicion(p, w, longitud, desde, hasta):
    return remap(p, (-longitud) + (1 + longitud) * w, (1 + longitud) * w, hasta, desde)

def ola_color(p, progreso, rango, anim):
    p_local = rango[0] + (rango[1] - rango[0]) * clamp(p, 0, 1)
    w_in = remap(progreso, 0, anim["entrada"], 0, 1)
    c_in = ola_transicion(p_local, w_in, anim["longitud"], 0, 1)
    w_out = remap(progreso, 1 - anim["salida"], 1, 0, 1)
    c_out = ola_transicion(p_local, w_out, anim["longitud"], 1, 0)
    return clamp(c_in * c_out, 0, 1)

def ola_ancho(p, progreso, rango, ancho, es_tronco):
    p_local = rango[0] + (rango[1] - rango[0]) * clamp(p, 0, 1)
    w = ancho * remap(p_local, 0.5, 1, 1, 0.5) * remap(progreso, 0.5, 1, 1, 0.5)
    if rango[1] < 1:
        w *= remap(rango[1] - p_local, 0.1, 0, 1, 0.5 if es_tronco else 0)
    return w

ANIM_VENA = dict(oi=0.6, fi=0.04, fm=0.55, ff=1.0, of=0.0,
                 longitud=0.4, entrada=0.5, salida=0.35)

# LA PILA 6+1 DEL PIXEL DEL MOTOR (StormLib.PilaW/PilaF)
PILA_W = [5.2, 3.6, 2.5, 1.7, 1.15, 0.72]
PILA_F = [0.06, 0.09, 0.13, 0.19, 0.28, 0.42]

# LA PALETA DEL DRAGÓN (ThunderveinDragon.cs de Coralite)
AMARILLO = (255, 202, 101)
NARANJA = (219, 114, 22)
NUCLEO = (255, 246, 215)


class Lienzo:
    """Buffer RGB float con blending ADITIVO (la semántica del lote del
    RayoSistema: Additive → aporte = tinte.rgb; el tinte lineal de la
    casa (RGB·√f, A·√f) hace el aporte = color·f en el canal)."""
    def __init__(self, w, h):
        self.w, self.h = w, h
        self.buf = np.zeros((h, w, 3), dtype=np.float64)
        self.buf[:, :] = FONDO

    def quad(self, cx, cy, largo, ancho, rot, color, f):
        """Un quad centrado/rotado con el PIXEL (aporte color·f)."""
        if f <= 0.003 or ancho < 0.05 or largo < 0.35: return
        c, s = math.cos(rot), math.sin(rot)
        ex, ey = c * largo * 0.5, s * largo * 0.5
        px, py = -s * ancho * 0.5, c * ancho * 0.5
        pts = [(cx - ex - px, cy - ey - py), (cx + ex - px, cy + ey - py),
               (cx + ex + px, cy + ey + py), (cx - ex + px, cy - ey + py)]
        capa = Image.new("L", (self.w, self.h), 0)
        dd = ImageDraw.Draw(capa)
        dd.polygon([(p[0], p[1]) for p in pts], fill=255)
        m = np.asarray(capa, dtype=np.float64) / 255.0
        r, g, b = color
        self.buf[:, :, 0] += m * r * f
        self.buf[:, :, 1] += m * g * f
        self.buf[:, :, 2] += m * b * f

    def png(self, escala=1.0):
        arr = np.clip(self.buf * escala, 0, 255).astype(np.uint8)
        img = Image.fromarray(arr)
        # v2 — EL FILTRADO LINEAL DEL MOTOR: el lote del RayoSistema
        # muestrea con SamplerState.LinearClamp (el AA bilinear de la
        # GPU) — el mock v1 rasterizaba los polígonos SIN AA y el VLM
        # leía los escalones del rasterizador como «bloques apilados».
        # El gaussian de 0.7 px EMULA ese filtrado bilinear.
        from PIL import ImageFilter
        return img.filter(ImageFilter.GaussianBlur(0.7))


def componer_vena(lienzo, bolts, color, nucleo, ancho, progreso, anim):
    """EL RENDER DEL CANAL con LA PILA 6+1 + LA VENA (RayoLib.Dibujar)."""
    for bolt in bolts:
        n = len(bolt.puntos)
        if bolt.rotaciones is None:
            bolt.rotaciones = calc_rotaciones(bolt.puntos)
        alfa_global = (remap(progreso, anim["fi"], anim["fm"], anim["oi"], 1) *
                       remap(progreso, anim["fm"], anim["ff"], 1, anim["of"]))
        if bolt.profundidad > 0:
            alfa_global *= 0.72 * (0.8 ** (bolt.profundidad - 1))
        if alfa_global <= 0.01: continue
        for i in range(n - 1):
            t = i / (n - 1)
            ola = ola_color(t, progreso, bolt.rango, anim)
            if ola <= 0.004: continue
            w = ola_ancho(t, progreso, bolt.rango, ancho, bolt.profundidad == 0)
            if w <= 0.05: continue
            a = bolt.puntos[i]; b = bolt.puntos[i + 1]
            rot = bolt.rotaciones[i]
            mid = ((a[0] + b[0]) * 0.5, (a[1] + b[1]) * 0.5)
            largo = math.hypot(b[0] - a[0], b[1] - a[1])
            if largo < 0.35: continue
            f = alfa_global * ola
            # LA PILA: 6 pasadas sólidas cuya SUMA es el degradado
            for k in range(len(PILA_W)):
                lienzo.quad(mid[0], mid[1], largo, w * PILA_W[k], rot, color,
                            PILA_F[k] * f)
            # LA VENA BLANCA (el núcleo razor, suelo 1.5 px)
            wv = max(w * 0.34, 1.5)
            lienzo.quad(mid[0], mid[1], largo, wv, rot, nucleo, f)


def escena(fase, semillas, colores, anchos_base, spread=1.0, progreso=None):
    """UNA ESCENA del trío: los TRES canales con semillas distintas."""
    W, H = 420, 560
    lienzo = Lienzo(W, H)
    rng = random.Random(1234)
    blanco = (W * 0.5, H - 60)
    for i in range(3):
        P = dict(TORMENTA)
        P["desvio"] = TORMENTA["desvio"] * spread
        bolts = []
        generar(P, bolts, semillas[i], blanco)
        # la fase manda el progreso de la ola y el ancho
        if progreso is None: progreso = fase["p"]
        ancho = fase["ancho"] * (anchos_base[i] / 46.0)
        componer_vena(lienzo, bolts, colores[i], NUCLEO, ancho, progreso, ANIM_VENA)
    # EL SUELO (la línea del mundo, para contexto)
    d = ImageDraw.Draw(lienzo.png())
    return lienzo


def main():
    rng = random.Random(77)
    semillas = [rng.randint(1, 2**30) for _ in range(3)]
    colores = [NARANJA, AMARILLO, AMARILLO]

    # LAS TRES FASES del ThunderFalling:
    #  NACIENDO: progreso 0.42 de la vida (el frente desciende ~mitad),
    #            el ancho crecido a ~70% del máximo.
    #  PLENO: progreso 0.62 (la caída COMPLETA), ancho máximo.
    #  MURIENDO: progreso 0.78 (media agonía — el X2Ease 1−f² deja el
    #            alfa ~0.5) con la RECADA de sobre ABIERTO ×3.5 (el
    #            zigzag se desparrama) y el ancho al 35%.
    fases = [
        ("NACIENDO", dict(p=0.42, ancho=38), 1.0),
        ("PLENO",    dict(p=0.62, ancho=92), 1.0),
        ("MURIENDO", dict(p=0.78, ancho=32), 3.5),
    ]

    escenas = []
    for nombre, fase, spread in fases:
        rng2 = random.Random(hash(nombre) & 0xFFFF)
        sems = [rng2.randint(1, 2**30) for _ in range(3)]
        lienzo = escena(fase, sems, colores, [46, 44, 42], spread)
        img = lienzo.png()
        # etiqueta
        from PIL import ImageDraw
        d = ImageDraw.Draw(img)
        d.rectangle([0, 0, 420, 22], fill=(8, 8, 14))
        d.text((10, 5), nombre, fill=(255, 255, 255))
        escenas.append(img)

    # LA TIRA: las tres fases lado a lado
    tira = Image.new("RGB", (420 * 3 + 40, 560), (24, 24, 32))
    for i, im in enumerate(escenas):
        tira.paste(im, (i * (420 + 20), 0))
    ruta = os.path.join(OUT, "MOCK_VENATRUENO_v65024.png")
    tira.save(ruta)
    print("  ✓", ruta)


if __name__ == "__main__":
    main()
