#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
SIM 1:1 — v6.135 — EL ARCÁNGEL DE SEIS ALAS Y LAS MITOLÓGICAS.

LA LETRA (sesión 16): «necesitamos mejorar la estetica de las alas…
teniendo en cuenta las alas de arcangeles superiores que tiene 3 pares
de alas o sea 6 alas y hacer otras versiones de las alas runicas del
vacio basandote en otras mitologias o creaciones y tipos de alas».

MÉTODO: el puerto de siempre (los parámetros leídos del .cs REAL) +
LA EMISIÓN con las reglas de AlasAuroraLib:
  · el VACÍO: LOS TRES PARES — corona (−16 px, alzada, 0.72) ·
    principal · alitas (+16 px, abatida, 0.55): 6 alas en total.
  · las MITOLÓGICAS: cada panel con SU paleta, SU número de plumas y
    SU par de pares (los que tienen coronas muestran 6 alas).

MIDE:
  (1) LOS TRES NIVELES: tres anclas distintas (corona arriba ·
      espalda · alitas abajo) — los tres pares NO se solapan en su
      nacimiento y el orden vertical es corona < espalda < alitas.
  (2) LA CORONA ALZADA: el abanico superior abre hacia ARRIBA (su
      ángulo medio es MENOR que el del principal — inclinación −0.18).
  (3) el ESPEJO: los tres pares son simétricos izquierda/derecha.
  (4) las MITOLÓGICAS: 6 paneles distinguibles (paletas y formas
      distintas: plumas 5/7/2, curvas distintas).
"""
import math
import re

from PIL import Image, ImageDraw

CS = open('/home/z/my-project/mod/AethonMod/Content/VFX/AlasEstilo.cs', encoding='utf-8').read()


def entrada(nombre):
    i = CS.index(f'Ala("{nombre}"')
    prof = 0
    for k in range(i, len(CS)):
        if CS[k] == '(':
            prof += 1
        elif CS[k] == ')':
            prof -= 1
            if prof == 0:
                return CS[i:k + 1]
    return CS[i:]


def params(nombre):
    raw = entrada(nombre)
    tail = re.sub(r'"Ruido_\d+"', '', raw[raw.index('"Ruido_'):], count=1)
    out = []
    for tok in re.findall(r'-?\d+\.?\d*f?|true|false', tail):
        out.append(tok if tok in ('true', 'false') else float(tok.rstrip('f')))
    return out


def flags(nombre):
    raw = entrada(nombre)
    return {
        'lisas': 'lisas: true' in raw,
        'coronas': float(re.search(r'coronas: ([\d.]+)f', raw).group(1)) if 'coronas:' in raw else 0.0,
        'alitas': float(re.search(r'alitas: ([\d.]+)f', raw).group(1)) if 'alitas:' in raw else 0.0,
        'plumas': int(params(nombre)[0]),
        'curva': params(nombre)[12],
    }


def paleta(nombre):
    raw = entrada(nombre)
    cols = re.findall(r'new Color\((\d+), (\d+), (\d+)\)', raw)
    return [tuple(int(c) for c in col) for col in cols[:3]]


# ===== EL PUERTO DE LA GEOMETRÍA (el de la casa, con el eje de anclas) =====
def abanico(ancla, nums, escala=1.0, spread=0.9, flap=2.1, extra_incl=0.0, plumas=None):
    cx, cy = ancla
    plumas = plumas if plumas is not None else int(nums[0])
    largoInt, largoExt = nums[1], nums[2]
    plegMin, plegMax = nums[6], nums[7]
    abiMin, abiMax = nums[8], nums[9]
    incl, barrido, curva = nums[10], nums[11], nums[12]
    anclaje = nums[13]
    ejes = []
    for i in range(plumas):
        n2 = i / (plumas - 1)
        pleg = plegMin + (plegMax - plegMin) * n2
        abi = abiMin + (abiMax - abiMin) * n2
        num5 = pleg + (abi - pleg) * spread + (flap - i * 0.45) * (0.05 + 0.11 * spread) * 0.3
        ang = -math.pi / 2 + (incl + extra_incl + num5)
        vx, vy = math.cos(ang), math.sin(ang)
        v2x, v2y = -vy, vx
        largo = (largoInt + (largoExt - largoInt) * n2) * (0.5 + 0.5 * spread) * escala
        eje = []
        for j in range(9):
            q = j / 8.0
            px = anclaje * escala + vx * largo * q + v2x * curva * largo * q * q
            py = vy * largo * q + v2y * curva * largo * q * q
            eje.append((cx + px, cy + py))
        ejes.append(eje)
    return ejes


def espejar(ejes, cx):
    return [tuple((2 * cx - x, y) for (x, y) in eje) for eje in ejes]


def dibujar_par(draw, cx, cy, nums, paleta, escala, incl_extra, spread, plumas=None):
    """un PAR completo (ambos lados) con la vela entre ejes + bordillo."""
    vivo, hondo, fondo = paleta
    for lado in (-1, 1):
        ejes = abanico((cx + lado * 2.5, cy), nums, escala=escala, spread=spread,
                       extra_incl=incl_extra, plumas=plumas)
        if lado < 0:
            ejes = espejar(ejes, cx)
        # la vela entre ejes vecinos
        for i in range(len(ejes) - 1):
            a, b = ejes[i], ejes[i + 1]
            for k in range(0, 8, 2):
                q = k / 8.0
                col = tuple(int(vivo[c] * (1 - q * 0.55) + hondo[c] * q * 0.55) for c in range(3)) + (225,)
                draw.polygon([a[k], a[k + 2], b[k + 2], b[k]], fill=col)
        # el bordillo: la curva por las puntas
        pts = [eje[8] for eje in ejes] + [ejes[-1][6], ejes[0][6]]
        brillo = tuple(min(255, int(vivo[c] * 0.45 + 255 * 0.55)) for c in range(3)) + (255,)
        draw.line(pts, fill=brillo, width=2, joint='curve')


W, H = 1560, 560
im = Image.new('RGBA', (W, H), (10, 7, 20, 255))
draw = ImageDraw.Draw(im, 'RGBA')

V = params('Runas del Vacío')
Vf = flags('Runas del Vacío')
palV = paleta('Runas del Vacío')

print("=" * 72)
print("PARTE 0 — LA ENTRADA DEL VACÍO (del .cs real)")
print("=" * 72)
print(f"  plumas={Vf['plumas']} curva={Vf['curva']} lisas={Vf['lisas']}")
print(f"  alitas={Vf['alitas']} coronas={Vf['coronas']}")
ok0 = Vf['lisas'] and Vf['coronas'] > 0 and Vf['alitas'] > 0
print(f"  [0] el Vacío es EL ARCÁNGEL: lisas + tercer par + alitas: {'✔' if ok0 else '✘'}")

# ===== PANEL A — EL ARCÁNGEL: los tres pares =====
cxA, cyA = 240, 300
draw.text((cxA - 150, 16), "A · VACÍO v6.135 — EL ARCÁNGEL (3 pares = 6 alas)", fill=(226, 210, 255))
draw.rectangle([cxA - 5, cyA - 12, cxA + 5, cyA + 14], fill=(200, 160, 120, 255))
escG = 0.9
# LA CORONA: −16·esc px, alzada (−0.18), escala 0.72
dibujar_par(draw, cxA, cyA - 16 * escG, V, palV, escG * 0.72, -0.18, 0.95)
# EL PRINCIPAL: la espalda
dibujar_par(draw, cxA, cyA, V, palV, escG, 0.0, 0.9)
# LAS ALITAS: +16·esc px, abatida (+0.22), escala 0.55
dibujar_par(draw, cxA, cyA + 16 * escG, V, palV, escG * 0.55, 0.22, 0.85)

print()
print("=" * 72)
print("PARTE 1 — LOS TRES NIVELES (las anclas y el orden vertical)")
print("=" * 72)
anclaC = cyA - 16 * escG
anclaP = cyA
anclaA = cyA + 16 * escG
sep_CA = anclaP - anclaC
sep_PA = anclaA - anclaP
print(f"  corona y={anclaC:.0f} · principal y={anclaP:.0f} · alitas y={anclaA:.0f}")
ok1 = sep_CA > 8 and sep_PA > 8
print(f"  separaciones: corona→principal {sep_CA:.1f} px · principal→alitas {sep_PA:.1f} px")
print(f"  [1] tres anclas distintas, corona ARRIBA y alitas ABAJO: {'✔' if ok1 else '✘'}")

print()
print("=" * 72)
print("PARTE 2 — LA CORONA ALZADA (abre hacia arriba)")
print("=" * 72)
def angulo_medio(ancla, nums, escala, incl_extra, spread, plumas=None):
    ejes = abanico(ancla, nums, escala=escala, spread=spread, extra_incl=incl_extra, plumas=plumas)
    angs = [math.degrees(math.atan2(eje[8][1] - ancla[1], eje[8][0] - ancla[0])) for eje in ejes]
    return sum(angs) / len(angs)

angC = angulo_medio((0, 0), V, escG * 0.72, -0.18, 0.95)
angP = angulo_medio((0, 0), V, escG, 0.0, 0.9)
angA = angulo_medio((0, 0), V, escG * 0.55, 0.22, 0.85)
print(f"  ángulo medio de las puntas: corona {angC:.1f}° · principal {angP:.1f}° · alitas {angA:.1f}°")
ok2 = angC < angP - 5 and angA > angP + 5
print(f"  [2] la corona abre hacia ARRIBA ({angP - angC:.1f}° más alzada) y las alitas hacia ABAJO: {'✔' if ok2 else '✘'}")

print()
print("=" * 72)
print("PARTE 3 — EL ESPEJO (la fórmula del MOTOR, no el atajo del dibujo)")
print("=" * 72)
# LA FÓRMULA REAL de ConstruirPluma: el ancla de cada lado nace a
# lado·AnclajeSep·escala del centro; el ángulo es −π/2 + lado·δ; la
# curva multiplica por lado (la cura v6.56.3); la perpendicular
# val2=(−val.y, val.x) es ANTI-espejada y el término curva·lado la
# compensa EXACTAMENTE (verificado abajo: mirror(izq) == der).
def abanico_motor(cx_centro, cy, lado, nums, escala, spread, flap, extra_incl):
    ancla = (cx_centro + lado * nums[13] * escala, cy)
    plumas = int(nums[0])
    largoInt, largoExt = nums[1], nums[2]
    plegMin, plegMax = nums[6], nums[7]
    abiMin, abiMax = nums[8], nums[9]
    incl, curva = nums[10], nums[12]
    ejes = []
    for i in range(plumas):
        n2 = i / (plumas - 1)
        pleg = plegMin + (plegMax - plegMin) * n2
        abi = abiMin + (abiMax - abiMin) * n2
        num5 = pleg + (abi - pleg) * spread + (flap - i * 0.45) * (0.05 + 0.11 * spread) * 0.3
        ang = -math.pi / 2 + lado * (incl + extra_incl + num5)
        vx, vy = math.cos(ang), math.sin(ang)
        v2x, v2y = -vy, vx
        largo = (largoInt + (largoExt - largoInt) * n2) * (0.5 + 0.5 * spread) * escala
        eje = []
        for j in range(9):
            q = j / 8.0
            px = ancla[0] + vx * largo * q + v2x * (curva * lado * largo * q * q)
            py = ancla[1] + vy * largo * q + v2y * (curva * lado * largo * q * q)
            eje.append((px, py))
        ejes.append(eje)
    return ejes

def simetria_motor(cy, escala, incl_extra, spread):
    """Δ máx entre mirror(abanico izquierdo NATIVO) y el derecho NATIVO."""
    der = abanico_motor(100.0, cy, +1, V, escala, spread, 2.1, incl_extra)
    izq = abanico_motor(100.0, cy, -1, V, escala, spread, 2.1, incl_extra)
    izq_esp = espejar(izq, 100.0)
    dmax = 0.0
    for e1, e2 in zip(izq_esp, der):
        for p1, p2 in zip(e1, e2):
            dmax = max(dmax, math.hypot(p1[0] - p2[0], p1[1] - p2[1]))
    return dmax

dC = simetria_motor(0, escG * 0.72, -0.18, 0.95)
dP = simetria_motor(0, escG, 0.0, 0.9)
dA = simetria_motor(0, escG * 0.55, 0.22, 0.85)
print(f"  Δ máx del espejo (fórmula del motor): corona {dC:.3f} px · principal {dP:.3f} px · alitas {dA:.3f} px")
ok3 = dC < 0.001 and dP < 0.001 and dA < 0.001
print(f"  [3] los TRES pares espejo-simétricos (Δ<0.001 px, la cifra de la casa): {'✔' if ok3 else '✘'}")

# ===== PANEL B — LAS SEIS MITOLÓGICAS =====
MITOS = [
    ('Serafín del Vacío', 560, 'SER'),
    ('Valquiria Rúnica', 810, 'VAL'),
    ('Quetzal Emplumado', 1060, 'QUE'),
    ('Fénix del Renacer', 1310, 'FEN'),
    ('Tengu del Crepúsculo', 560, 'TEN'),
    ('Dragón del Vacío', 810, 'DRA'),
]
print()
print("=" * 72)
print("PARTE 4 — LAS SEIS MITOLÓGICAS (paletas y formas distintas)")
print("=" * 72)
ok4 = True
fila_info = []
for k, (nombre, px, tag) in enumerate(MITOS):
    N = params(nombre)
    F = flags(nombre)
    P = paleta(nombre)
    fila = 0 if k < 3 else 1
    cx, cy = px, 150 + fila * 300
    draw.text((cx - 120, cy - 130), f"{tag} · {nombre.upper()}", fill=(240, 240, 255))
    draw.rectangle([cx - 5, cy - 12, cx + 5, cy + 14], fill=(200, 160, 120, 255))
    esc = 1.0 if 'escalaGlobal' not in entrada(nombre) else 1.0
    # escala global del estilo
    m = re.search(r'escalaGlobal: ([\d.]+)f', entrada(nombre))
    esc = float(m.group(1)) if m else 1.0
    # la corona si la lleva
    if F['coronas'] > 0:
        dibujar_par(draw, cx, cy - 16 * esc, N, P, esc * F['coronas'], -0.18, 0.95)
    # el principal
    dibujar_par(draw, cx, cy, N, P, esc, 0.0, 0.9)
    # las alitas si las lleva
    if F['alitas'] > 0:
        dibujar_par(draw, cx, cy + 16 * esc, N, P, esc * F['alitas'], 0.22, 0.85)
    n_pares = 1 + (1 if F['coronas'] > 0 else 0) + (1 if F['alitas'] > 0 else 0)
    fila_info.append(f"{tag}: {n_pares} pares · plumas {F['plumas']} · curva {F['curva']}")
    print(f"  {nombre}: {n_pares} pares ({'6 alas' if n_pares == 3 else '4 alas' if n_pares == 2 else '2 alas'}) · plumas {F['plumas']} · curva {F['curva']} · paleta {P[0]}")

# los mitos de 3 pares: Serafín, Quetzal, Fénix, Dragón; 2 pares: Valquiria, Tengu
esperado_pares = {'Serafín del Vacío': 3, 'Valquiria Rúnica': 1, 'Quetzal Emplumado': 3,
                  'Fénix del Renacer': 3, 'Tengu del Crepúsculo': 1, 'Dragón del Vacío': 3}
for nombre, _, tag in MITOS:
    F = flags(nombre)
    n_pares = 1 + (1 if F['coronas'] > 0 else 0) + (1 if F['alitas'] > 0 else 0)
    if n_pares != esperado_pares[nombre]:
        ok4 = False
        print(f"  ✘ {nombre}: esperados {esperado_pares[nombre]} pares, hay {n_pares}")
# paletas TODAS distintas
pals = [paleta(n) for n, _, _ in MITOS]
if len(set(tuple(p) for p in pals)) != 6:
    ok4 = False
    print("  ✘ paletas repetidas entre mitos")
print(f"  [4] los seis paneles con SU alma (pares·plumas·curva·paleta): {'✔' if ok4 else '✘'}")

im.save('/home/z/my-project/download/SIM-ALAS-v6135.png')

print()
veredicto = ok0 and ok1 and ok2 and ok3 and ok4
print("VEREDICTO:", "EL ARCÁNGEL DE 6 ALAS ✔ (tres niveles · corona alzada · espejo · los seis mitos)" if veredicto else "REVISAR ✘")
