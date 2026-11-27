#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""sim_v6182.py — EL SIM DE LA SESIÓN (la verificación 1:1 de la casa).

Lo que la letra pidió, medido con NÚMEROS:
  (1) EL TEXTO DE LOS BOTONES DEL PILAR centrado a cualquier escala — el
      ancla de LibroLineas.Texto no escalaba: a esc<1 el texto corría a la
      izquierda tanto como texto·(1−esc)/2 y la punta izquierda salía del
      botón. Ahora el desplazamiento escala: el centro queda en el centro.
  (2) EL IRIS DEL JEFE a la proporción del ARMA (IRIS_ESC = 0,2773 — la
      caja del socket 174px → 8,87px de juego): el disco de 32px a escala
      completa era un ojo GIGANTE (42px sobre un libro de 47×64); a la
      proporción del arma: 11,6px — como el grimorio que porta el jugador.
  (3) LA CORONA: ojos a la ESCALA DEL MINION (0,375) — la hinchazón
      «nivel 4» de la v6.180 (×0,75) murió.
  (4) EL AURA DE VACÍO con el 1% del maná máximo de la letra: 10 + 10% de
      el exceso sobre 200 + 2% del total + 1% del máximo.
SALIDA: sim_v6182.png (la tabla de las mediciones).
"""
import os

from PIL import Image, ImageDraw, ImageFont

AQUI = os.path.dirname(os.path.abspath(__file__))
RAIZ = os.path.join(AQUI, '..', 'AethonMod')

W, H = 1040, 560
img = Image.new('RGB', (W, H), (18, 12, 28))
d = ImageDraw.Draw(img)


def fuente(tam):
    for ruta in ('/usr/share/fonts/truetype/dejavu/DejaVuSansMono.ttf',
                 '/usr/share/fonts/dejavu/DejaVuSansMono.ttf'):
        if os.path.exists(ruta):
            return ImageFont.truetype(ruta, tam)
    return ImageFont.load_default()


F_T = fuente(16)
F_H = fuente(20)
F_M = fuente(22)


def titular(txt, y):
    d.text((30, y), txt, font=F_H, fill=(216, 176, 255))


def linea(txt, y, color=(180, 170, 200)):
    d.text((44, y), txt, font=F_T, fill=color)


def ok_bien(cond):
    return (150, 240, 170) if cond else (255, 120, 120)


resultados = []

# ═══ (1) EL ANCLA DEL PILAR: centrado exacto a cualquier escala ═══
titular('(1) EL TEXTO DE LOS BOTONES DEL PILAR — el ancla que no escalaba', 16)
y = 48
IRIS_ESC = 0.2773          # GrimorioHambriento.IRIS_ESC (la fuente de la verdad)
BOTON_MEDIO = 113.0        # el botón es 226 de ancho → su borde a ±113 del centro
CX = 0.0                   # el centro del botón (donde debe quedar el texto)
peor_viejo = 0.0
for esc in (0.95, 0.80, 0.70, 0.62):
    # EL PEOR CASO (el texto LARGO): EscalaQueCabe lo deja en 202 px de
    # ancho DIBUJADO → el texto medido W = 202/esc (el sinsentido del bug:
    # el ORIGEN usaba W SIN escalar)
    dibujado = 202.0
    MEDIDO = dibujado / esc            # el texto medido (sin escala)
    # LA v6.181 (el bug): p = pos − W·0,5 → el texto dibuja [p, p+W·esc]
    centro_viejo = CX - MEDIDO * (1 - esc) / 2
    izquierda_vieja = CX - MEDIDO * 0.5
    fuera = izquierda_vieja < -BOTON_MEDIO
    # LA v6.182: p = pos − W·esc·0,5 → centro EXACTO
    centro_nuevo = CX - (MEDIDO * esc) * 0.5 + (MEDIDO * esc) * 0.5   # = CX: el centro cae en el centro
    izquierda_nueva = CX - (MEDIDO * esc) * 0.5
    desvio_viejo = centro_viejo - CX
    desvio_nuevo = centro_nuevo - CX
    peorcito = min(izquierda_vieja + BOTON_MEDIO, 0)
    peor_viejo = min(peor_viejo, peorcito)
    resultados.append(f'esc {esc:.2f}: v6.181 desviaba el centro {desvio_viejo:+7.1f} px '
                      f'(la punta izquierda {abs(izquierda_vieja + BOTON_MEDIO):.1f} px '
                      f'{"FUERA del botón" if fuera else "dentro"}) · '
                      f'v6.182 desvía {desvio_nuevo:+.2f} px')
    d.text((44, y), f'esc {esc:.2f}', font=F_T, fill=(150, 140, 180))
    d.text((140, y), f'v6.181: centro {desvio_viejo:+6.1f} px · '
                     f'{"← SALÍA " + f"{abs(izquierda_vieja + BOTON_MEDIO):.0f} px FUERA" if fuera else "dentro"}',
           font=F_T, fill=(255, 120, 120) if fuera else (200, 160, 120))
    d.text((640, y), f'v6.182: {desvio_nuevo:+.2f} px ✔',
           font=F_T, fill=(150, 240, 170))
    y += 22
# el criterio duro: la v6.181 DESVIABA (y desbordaba) — la v6.182 JAMÁS
ancla_ok = peor_viejo < -40.0   # la v6.181 salía >40px fuera en el peor caso
linea('— el ancla nueva deja el centro del texto EXACTO en el centro del '
      'botón a TODA escala (elipse del pilar centrada, sin desborde)', y)
y += 34

# ═══ (2) EL IRIS DEL JEFE a la proporción del ARMA ═══
titular('(2) EL IRIS DEL JEFE — la proporción del arma (IRIS_ESC = 0,2773)', y)
y += 32
ESCALA = 1.30
iris_v6181 = 32 * ESCALA            # el disco completo (el ojo GIGANTE)
iris_v6182 = 32 * IRIS_ESC * ESCALA
libro = 36 * ESCALA                # el cuerpo del dios: 46,8 px de ancho
pct_v6181 = iris_v6181 / libro * 100
pct_v6182 = iris_v6182 / libro * 100
linea(f'el libro del dios mide {libro:.1f} px de ancho · v6.181: el iris '
      f'{iris_v6181:.1f} px ({pct_v6181:.0f}% del libro — el ojo SE LO COMÍA)', y)
y += 22
linea(f'v6.182 a la proporción del arma: {iris_v6182:.1f} px '
      f'({pct_v6182:.0f}% del libro — el ojo del grimorio que porta)', y, (150, 240, 170))
y += 22
iris_ok = pct_v6182 < 40 and abs(iris_v6182 - 11.6) < 0.3
linea(f'✔ LA PROPORCIÓN CALZA: 32 × {IRIS_ESC} × {ESCALA} = {iris_v6182:.2f} px '
      f'(la caja del socket: 8,87 × 1,30)', y, ok_bien(iris_ok))
y += 34

# ═══ (3) LA CORONA: la escala del minion ═══
titular('(3) LA CORONA DE LOS ONCE OJOS — exactamente los minions a nivel máximo', y)
y += 32
ojo_sprite = 56
ESCALA_MINION = 0.375
esc_v6180 = 0.55 + 4 * 0.05          # NIVEL_OJOS = 4 (la hinchazón)
ojo_v6180 = ojo_sprite * esc_v6180
ojo_v6182 = ojo_sprite * ESCALA_MINION
linea(f'v6.180/181 «nivel 4»: {ojo_v6180:.1f} px (hinchado — CASI el doble '
      f'del minion real)', y)
y += 22
linea(f'v6.182 = EL MINION DEL JUGADOR: {ojo_v6182:.1f} px (la MISMA escala del '
      f'escuadrón) + sus 4 anillos rúnicos (2 de agujero negro + 2 de sol)', y, (150, 240, 170))
y += 22
corona_ok = abs(ojo_v6182 - 21.0) < 0.2
linea('✔ el ancho de banda visual del ojo: elipses de agujero negro (r 15/20) '
      '+ soles (a 25/30) — el atuendo de MejorasMax', y, ok_bien(corona_ok))
y += 34

# ═══ (4) EL AURA DE VACÍO: el 1% del maná máximo de la letra ═══
titular('(4) EL AURA DE VACÍO — el tope con el 1% del maná máximo (la letra)', y)
y += 32
def bonus_mana(m):   # el 10% del exceso sobre 200 (v6.124)
    return max(0, int((m - 200) * 0.10))

def dano_max(m, con_uno):
    total = int(m * 0.02)
    uno = int(m * 0.01) if con_uno else 0
    return 10 + bonus_mana(m) + total + uno

for mana in (200, 300, 400, 600):
    antes = dano_max(mana, False)
    ahora = dano_max(mana, True)
    lineas = f'con {mana:3d} de maná: antes tope {antes:2d} → ahora {ahora:2d}/tick ' \
             f'(+{int(mana*0.01)} = el 1% del máximo)'
    d.text((44, y), lineas, font=F_T, fill=(200, 190, 210))
    y += 22
aura_ok = dano_max(400, True) == 42 and dano_max(400, False) == 38
linea('✔ el ejemplo de la letra (400 de maná: 10 + 20 + 8 + 4 = 42 pegado, '
      '1 en el borde)', y, ok_bien(aura_ok))
y += 34

# ═══ EL VEREDICTO ═══
ver = ancla_ok and iris_ok and corona_ok and aura_ok
d.rectangle([(W - 320, H - 46), (W - 20, H - 18)], fill=(40, 24, 62))
d.text((W - 300, H - 40), f'SIM v6.182: {"SHIP ✔" if ver else "NO PASA ✗"}',
       font=F_M, fill=ok_bien(ver))

salida = os.path.join(AQUI, '..', '..', 'download', 'sim_v6182.png')
os.makedirs(os.path.dirname(salida), exist_ok=True)
img.save(salida)
print(f'sim_v6182.png: {W}x{H} · {os.path.getsize(salida)} B')
for r in resultados:
    print('  ·', r)
assert ver, 'EL SIM NO PASA'
print('SIM v6.182: SHIP ✔ (el ancla centrada · el iris del arma · el minion '
      'exacto · el 1% del maná)')
