#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""v6.182 — EL ICONO DEL MUNDO INTERIOR (la letra: «el icono es simple debe
representar un paisaje oscuro y brumoso, el paisaje seria completamente
plano sin nada a la distancia solo bruma y tentaculos»).

EL BUFF DE LA PUERTA VIVA: el botón «IR AL MUNDO INTERIOR» del altar murió
(ocupaba el papel) — la puerta ahora es un ICONO DE BUFF fijo, con clic
derecho para viajar. El icono es EL PAISAJE DEL INTERIOR:

  · el fondo: el violeta profundo del eclipse permanente (nada más lejos:
    NO hay montañas, NO hay nada en la distancia — el llano plano).
  · el suelo: la línea del llano, lisa, con su filo apenas más claro.
  · la bruma: TRES bandas horizontales suaves (la niebla del cuerpo del
    dios, alpha bajo, difuminadas — las mismas masas de la casa).
  · los tentáculos: TRES que suben del suelo, encorvados, silueta oscura
    con su filo iluminado (los del velo del interior).

SALIDA: AethonMod/Content/Buffs/MundoInteriorBuff.png (32×32 — la convención
de TODOS los buffs de la casa: VacioExterior, AuraVacio, OjoEterno…).
"""
import os

from PIL import Image, ImageDraw, ImageFilter

AQUI = os.path.dirname(os.path.abspath(__file__))
RAIZ = os.path.join(AQUI, '..', 'AethonMod')
SALIDA = os.path.join(RAIZ, 'Content', 'Buffs', 'MundoInteriorBuff.png')

W = H = 32          # la convención de los buffs de la casa
SS = 4              # super-muestreo para bordes limpios
N = W * SS          # canvas de trabajo 128×128

# ── LA PALETA DEL INTERIOR (el eclipse y su velo — las de CieloInteriorEterno) ──
FONDO_ARRIBA = (30, 16, 46)
FONDO_ABAJO = (10, 5, 20)
SUELO = (16, 8, 28)
FILO_SUELO = (74, 44, 116)
TENTA_CUERPO = (34, 16, 54)
TENTA_FILO = (104, 62, 158)
BRUMA = (150, 116, 186)

img = Image.new('RGBA', (N, N), (0, 0, 0, 0))
d = ImageDraw.Draw(img)

# ══ 1) EL CIELO DEL ECLIPSE: el degradado violeta profundo ══
for y in range(N):
    t = y / (N - 1)
    c = tuple(int(FONDO_ABAJO[i] + (FONDO_ARRIBA[i] - FONDO_ABAJO[i]) * t) for i in range(3))
    d.line([(0, y), (N, y), ], fill=c + (255,))

# ══ 2) EL LLANO PLANO: la línea del suelo — NADA a la distancia ══
y_suelo = int(N * 0.66)
d.rectangle([0, y_suelo, N, N], fill=SUELO + (255,))
d.line([(0, y_suelo), (N, y_suelo)], fill=FILO_SUELO + (255,), width=SS)

# ══ 3) LA BRUMA: TRES bandas horizontales suaves sobre el paisaje (las
#     tentáculos suben DESPUÉS — siluetas oscuras CONTRA la niebla, la
#     letra: «solo bruma y tentaculos» — los dos se leen) ══
bruma = Image.new('RGBA', (N, N), (0, 0, 0, 0))
db = ImageDraw.Draw(bruma)
bandas = [
    (int(N * 0.38), int(N * 0.115), 66),   # la alta: la más tenue
    (int(N * 0.52), int(N * 0.140), 84),   # la del medio
    (int(N * 0.63), int(N * 0.165), 96),   # la del suelo: la más densa
]
for cy, ry, a in bandas:
    db.ellipse([-N // 3, cy - ry, N + N // 3, cy + ry], fill=BRUMA + (a,))
bruma = bruma.filter(ImageFilter.GaussianBlur(radius=int(3.2 * SS)))
img = Image.alpha_composite(img, bruma)

# ══ 4) LOS TRES TENTÁCULOS: siluetas ENCIMA de la bruma — suben del
#     suelo encorvados, oscuros contra la niebla, con su filo iluminado
#     (OJO: la bruma compone un canvas NUEVO — el lápiz se RE-CREA aquí,
#     si no dibujaría sobre el canvas muerto de antes) ══
d = ImageDraw.Draw(img)


def tentaculo(x_base, alto_px, curva, grosor0):
    """Dibuja un tentáculo cónico que sube del suelo con una curva lateral
    y una punta fina (tiras verticales de anchura decreciente)."""
    pasos = 40
    puntos = []
    for k in range(pasos + 1):
        t = k / pasos
        y = y_suelo + SS - t * alto_px
        x = x_base + curva * (t * t) * SS
        w = grosor0 * (1 - 0.82 * t) * SS * 0.5
        puntos.append((x, y, w))
    # el cuerpo: cuadriláteros encadenados
    for k in range(pasos):
        x1, y1, w1 = puntos[k]
        x2, y2, w2 = puntos[k + 1]
        d.polygon([(x1 - w1, y1), (x1 + w1, y1), (x2 + w2, y2), (x2 - w2, y2)],
                  fill=TENTA_CUERPO + (255,))
        # el filo iluminado: solo el lado que mira al eclipse
        d.line([(x1 - w1, y1), (x2 - w2, y2)], fill=TENTA_FILO + (230,), width=max(1, SS // 2))

tentaculo(N * 0.22, N * 0.52, 1.35, 4.6)    # el alto, doblado a la derecha
tentaculo(N * 0.52, N * 0.34, -1.05, 3.4)   # el mediano, a la izquierda
tentaculo(N * 0.80, N * 0.42, 0.75, 3.0)    # el del filo, casi recto

# una última veladura MUY suave en la BASE (los tentáculos NAcen de la
# niebla — el velo que lame el suelo)
base = Image.new('RGBA', (N, N), (0, 0, 0, 0))
db2 = ImageDraw.Draw(base)
db2.ellipse([-N // 3, int(N * 0.56), N + N // 3, int(N * 0.84)], fill=BRUMA + (58,))
base = base.filter(ImageFilter.GaussianBlur(radius=int(4.5 * SS)))
img = Image.alpha_composite(img, base)

# ══ 5) EL NACIMIENTO: 128×128 → 32×32 (LANCZOS, como el retrato del Sellado) ══
icono = img.resize((W, H), Image.LANCZOS)
icono.save(SALIDA)

# ══ LA VERIFICACIÓN (nada nace sin prueba) ══
opacos = sum(1 for p in icono.getdata() if p[3] > 0)
print(f'MundoInteriorBuff.png: {icono.size[0]}x{icono.size[1]} · {opacos} px opacos · '
      f'{os.path.getsize(SALIDA)} B')
assert icono.size == (32, 32), '¡la convención de los buffs es 32×32!'
assert opacos >= 900, '¡el icono nació roto!'


def lum(c):
    return sum(c[:3])


p = icono.load()
# EL PAISAJE DE LA LETRA, comprobado píxel a píxel:
# (1) el cielo del eclipse: oscuro y violeta
assert p[16, 3][2] > p[16, 3][1] and lum(p[16, 3]) < 130, '¡el cielo no es el eclipse violeta!'
# (2) el llano PLANO: nada a la distancia — la fila del horizonte más
#     clara que el suelo y que el cielo puro (el filo del suelo vivo)
assert lum(p[16, 21]) > lum(p[16, 28]) + 30, '¡el filo del suelo no se lee!'
assert lum(p[16, 28]) < lum(p[16, 13]), '¡el suelo no es el más oscuro abajo!'
# (3) LA BRUMA: la banda del medio más clara que el cielo puro
assert lum(p[16, 13]) > lum(p[16, 3]) + 60, '¡la bruma no se lee!'
# (4) LOS TENTÁCULOS: siluetas oscuras CONTRA la niebla — en su cuerpo
#     sólido, el píxel del tentáculo es mucho más OSCURO que el cielo
#     a su lado (las puntas finas se mezclan con el filo — se mide el cuerpo)
assert lum(p[8, 14]) < lum(p[3, 14]) - 30, '¡el tentáculo alto no se lee contra la niebla!'
assert lum(p[16, 15]) < lum(p[22, 15]) - 30, '¡el tentáculo mediano no se lee!'
print('EL MUNDO INTERIOR YA TIENE ICONO (el paisaje plano, la bruma y los tentáculos)')
