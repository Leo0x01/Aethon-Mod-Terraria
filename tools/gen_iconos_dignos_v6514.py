#!/usr/bin/env python3
# v6.51.4 — LOS ICONOS DE LOS SEIS CANDIDATOS DIGNOS + SU BOLSA.
#
# Petición del usuario: «crea un arma nueva por cada una de estas ideas
# que diste y crea una nueva bolsa para darsela al jugador». Las armas
# viven en DignosWeapons.cs con SetDefaults 28×30 — iconos pixel art
# de la casa (contorno negro, highlight blanco cálido, la paleta oro/
# violeta de la familia DignosGrimorio).
#
#   1 · DignosEquinoccioBinario — la estrella doble: disco ORO y disco
#       VIOLETA orbitándose con el eje tenue entre ambos y el destello
#       de 4 puntas en el cruce.
#   2 · DignosComasQueMuerden — la coma gorda DORADA con la cola que se
#       afina (punta violeta) + dos comitas suspendidas de compañía.
#   3 · DignosPaginaArrancada — la hoja INCLINADA de dos caras (oro
#       arriba · violeta abajo), filo blanco y dos renglones de glifos.
#   4 · DignosOjoQueCaza — la mirada del escuadrón: la almendra con el
#       iris dorado DESLIZADO hacia su presa (a la derecha).
#   5 · DignosLineaDeLectura — la fila de glifos dorados con CABEZA
#       violeta (la frase escrita y su punto final).
#   6 · DignosMarcapaginas — el listón vertical dorado con la punta
#       violeta (el marcador colgando de su página).
#   7 · BolsaProyectilesDignos — el saco de cuero de la casa (el patrón
#       de la BolsaDeLaCria: contorno K, cuero M, cordel R) con LA
#       BINARIA estampada en el frente (el símbolo de la familia).
from PIL import Image
import math, os

W = H = 30
# === LA PALETA DE LA CASA ===
K = (18, 10, 28, 255)          # el negro-violeta del contorno
W_ = (255, 250, 232, 255)      # el blanco cálido del highlight
ORO = (255, 214, 120, 255)     # OroBinaria
ORO_CLARO = (255, 236, 170, 255)  # OroGlifo
ORO_OSCURO = (196, 140, 52, 255)
VIO = (178, 108, 240, 255)     # VioletaBinaria
VIO_CLARO = (216, 168, 252, 255)
VIO_OSCURO = (108, 56, 176, 255)
M = (150, 96, 52, 255)         # el cuero del saco
M_CLARO = (188, 130, 76, 255)
M_OSCURO = (104, 62, 34, 255)
R = (196, 60, 60, 255)         # el cordel rojo

def nuevo():
    return Image.new('RGBA', (W, H), (0, 0, 0, 0))

def px(img, x, y, c):
    if 0 <= int(x) < W and 0 <= int(y) < H:
        img.putpixel((int(x), int(y)), c)

def disco(img, cx, cy, r, relleno, borde=K, highlight=None, hl_off=(-2, -2)):
    """Disco pixel-art con contorno y highlight (sin antialias)."""
    rr = r * r
    rb = (r + 1) * (r + 1)
    for y in range(int(cy - r - 2), int(cy + r + 3)):
        for x in range(int(cx - r - 2), int(cx + r + 3)):
            d2 = (x - cx) ** 2 + (y - cy) ** 2
            if d2 <= rr:
                px(img, x, y, relleno)
            elif d2 <= rb:
                px(img, x, y, borde)
    if highlight:
        px(img, cx + hl_off[0], cy + hl_off[1], highlight)
        px(img, cx + hl_off[0] + 1, cy + hl_off[1], highlight)

def linea_grosor(img, x0, y0, x1, y1, grosor, c):
    """Línea recta de grosor fijo (para listones y ejes)."""
    n = int(max(abs(x1 - x0), abs(y1 - y0))) * 2 + 1
    for i in range(n):
        f = i / (n - 1)
        x = x0 + (x1 - x0) * f
        y = y0 + (y1 - y0) * f
        g = grosor / 2
        for dy in range(-int(g), int(g + 0.5)):
            for dx in range(-int(g), int(g + 0.5)):
                px(img, x + dx, y + dy, c)

def destello4(img, cx, cy, brazo, c=W_, nucleo=None):
    """El destello de 4 puntas de la casa: cruz de cápsulas."""
    for i in range(1, brazo + 1):
        f = 1 - i / (brazo + 1)
        px(img, cx + i, cy, tuple(int(v * (0.55 + 0.45 * f)) + (255,) for v in c[:3]) if len(c) == 4 else c)
        px(img, cx - i, cy, c)
        px(img, cx, cy + i, c)
        px(img, cx, cy - i, c)
    if nucleo:
        px(img, cx, cy, nucleo)

def save(img, ruta):
    img.save(ruta)
    print('OK', ruta, img.size)

BASE = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
W_COSMIC = os.path.join(BASE, 'AethonMod', 'Content', 'Weapons', 'Cosmic')
W_BOLSAS = os.path.join(BASE, 'AethonMod', 'Content', 'Items', 'Bolsas')

# ======================================================================
#  1 · EL EQUINOCCIO BINARIO — la estrella doble orbitándose
# ======================================================================
img = nuevo()
# EL EJE ORBITAL (la estela tenue que une la par — píxeles alternados).
for i in range(9):
    x = 9 + i
    y = 9 + i
    c = ORO_OSCURO if i % 2 == 0 else VIO_OSCURO
    px(img, x, y, c)
# EL DISCO ORO (arriba-izquierda, el del día).
disco(img, 9, 9, 6, ORO, highlight=W_, hl_off=(-2, -2))
# EL DISCO VIOLETA (abajo-derecha, el de la noche).
disco(img, 21, 21, 6, VIO, highlight=W_, hl_off=(-2, -2))
# EL DESTELLO DE 4 PUNTAS en el cruce de la órbita.
for i in range(1, 4):
    v = 0.55 + 0.45 * (1 - i / 4)
    cw = (int(255 * v), int(250 * v), int(232 * v), 255)
    px(img, 15 + i, 15, cw); px(img, 15 - i, 15, cw)
    px(img, 15, 15 + i, cw); px(img, 15, 15 - i, cw)
px(img, 15, 15, W_)
save(img, os.path.join(W_COSMIC, 'DignosEquinoccioBinario.png'))

# ======================================================================
#  2 · LAS COMAS QUE MUERDEN — la coma suspendida y sus hermanas
# ======================================================================
img = nuevo()
# LA COMA MAYOR: cabeza gorda dorada + la cola que se afina (punta violeta).
disco(img, 19, 10, 5, ORO_CLARO, highlight=W_, hl_off=(-2, -2))
cola = [(15, 13, 3), (13, 16, 3), (12, 19, 2), (11, 22, 2), (11, 25, 1)]
for (x, y, g) in cola:
    for dy in range(g):
        px(img, x, y + dy, ORO)
# la punta VIOLETA de la cola (la tinta del final).
px(img, 11, 26, VIO); px(img, 12, 26, VIO); px(img, 12, 27, VIO)
# contorno manual de la cola (K a la izquierda).
for (x, y, g) in cola:
    px(img, x - 1, y + g - 1, K)
# LAS DOS COMITAS SUSPENDIDAS (las que esperan el latigazo).
disco(img, 6, 6, 2, ORO_CLARO)
px(img, 4, 8, ORO); px(img, 4, 9, VIO)
disco(img, 26, 19, 2, ORO_CLARO)
px(img, 25, 22, ORO); px(img, 25, 23, VIO)
# EL HALO PULSANTE (el borde de la espera): 4 puntitos claros alrededor.
px(img, 13, 4, ORO_OSCURO); px(img, 25, 6, ORO_OSCURO)
px(img, 8, 24, ORO_OSCURO); px(img, 21, 28, ORO_OSCURO)
save(img, os.path.join(W_COSMIC, 'DignosComasQueMuerden.png'))

# ======================================================================
#  3 · LA PÁGINA ARRANCADA — la hoja de dos caras, inclinada
# ======================================================================
img = nuevo()
# LA HOJA: paralelogramo inclinado (dx=+5 por cada 6 de alto).
# borde superior (el filo blanco), cuerpo arriba ORO / abajo VIOLETA.
for fila in range(16):           # 16 renglones de hoja
    y = 7 + fila
    dx = (fila * 5) // 16       # el corrimiento de la inclinación
    for col in range(13):       # 13 columnas de ancho
        x = 6 + col + dx
        if fila == 0:
            px(img, x, y, W_ if 2 <= col <= 10 else K)
        elif fila <= 7:          # LA CARA ORO
            c = ORO if 1 <= col <= 11 else K
            px(img, x, y, c)
        else:                    # LA CARA VIOLETA
            c = VIO if 1 <= col <= 11 else K
            px(img, x, y, c)
    px(img, 6 + dx, y, K)        # el canto izquierdo
    px(img, 6 + 12 + dx, y, K)   # el canto derecho
px(img, 6 + 12 + ((15 * 5) // 16), 22, K)  # esquina
# LA LÍNEA DEL PLIEGUE (donde el oro se vuelve violeta).
dx7 = (7 * 5) // 16
for col in range(1, 12):
    px(img, 6 + col + dx7 + 1, 14, ORO_OSCURO if col < 6 else VIO_OSCURO)
# LOS RENGLONES (los glifos escritos: K sobre oro, W sobre violeta).
for col in (3, 5, 7, 9):
    px(img, 6 + col + 1, 4, K); px(img, 6 + col + 2, 4, K)
for col in (3, 5, 7, 9):
    px(img, 6 + col + 3, 10, ORO_OSCURO)
for col in (3, 6, 9):
    px(img, 6 + col + 4, 18, W_); px(img, 6 + col + 5, 18, W_)
# LA ESQUINA DOBLADA (el arranque de la hoja).
for i in range(3):
    px(img, 6 + 11 - i, 7 + i, ORO_OSCURO)
save(img, os.path.join(W_COSMIC, 'DignosPaginaArrancada.png'))

# ======================================================================
#  4 · EL OJO QUE CAZA — la almendra con el iris deslizado
# ======================================================================
img = nuevo()
# LA ALMENDRA: 22 de ancho × 12 de alto, contorno K, esclerótica blanca.
for fila in range(11):
    y = 9 + fila
    # el ancho de la almendra por fila (óvalo apuntado).
    anchos = [7, 10, 12, 13, 13, 13, 13, 12, 10, 7, 4]
    a = anchos[fila]
    x0 = 15 - a
    x1 = 15 + a
    for x in range(x0, x1 + 1):
        borde = (fila == 0 or fila == 10 or x <= x0 + 1 or x >= x1 - 1 or
                 abs(x - 15) > anchos[fila] - 1)
        if fila in (0, 10) or x == x0 or x == x1:
            px(img, x, y, K)
        else:
            px(img, x, y, W_)
# LA PUPILA DESLIZADA a la derecha (la mirada cazando) — iris ORO.
cx, cy = 19, 14
for dy in (-3, 3):
    for dx in (-3, 3):
        d2 = dx * dx + dy * dy
        if d2 <= 9:
            px(img, cx + dx, cy + dy, ORO if d2 <= 4 else VIO_OSCURO)
px(img, cx, cy, K); px(img, cx + 1, cy, K)         # la pupila
px(img, cx - 1, cy - 1, W_)                          # el brillo
# LAS PESTAÑAS (tres rayitas K en el párpado superior).
for i, (x, y) in enumerate([(9, 8), (15, 7), (21, 8)]):
    px(img, x, y - 1, K); px(img, x, y, K)
# EL DESTELLO DE LA MIRA (la puntita del antejo).
px(img, 24, 11, W_); px(img, 25, 10, W_)
save(img, os.path.join(W_COSMIC, 'DignosOjoQueCaza.png'))

# ======================================================================
#  5 · LA LÍNEA DE LECTURA — la fila de glifos y su cabeza
# ======================================================================
img = nuevo()
# LA LÍNEA tenue que une la frase (los renglones del libro).
for x in range(4, 25):
    if x % 2 == 0:
        px(img, x, 15, ORO_OSCURO)
# LOS GLIFOS (cuadraditos dorados, cada uno su pose — la escritura).
glifos = [(4, 13), (8, 15), (12, 13), (16, 15), (20, 13)]
for i, (gx, gy) in enumerate(glifos):
    c = ORO_CLARO
    if i % 2 == 0:      # el glifo "de pie"
        px(img, gx, gy, c); px(img, gx + 1, gy, c)
        px(img, gx, gy + 1, c); px(img, gx + 1, gy + 1, c)
        px(img, gx + 1, gy - 1, ORO_OSCURO)
    else:               # el glifo "rotado" (la variedad de la escritura)
        px(img, gx, gy, c); px(img, gx + 1, gy + 1, c)
        px(img, gx + 1, gy, ORO); px(img, gx, gy + 1, ORO)
# LA CABEZA VIOLETA (el punto y coma que cierra la frase).
for dy in (-2, 2):
    for dx in (-2, 2):
        if dx * dx + dy * dy <= 4:
            px(img, 24 + dx, 14 + dy, VIO_CLARO if dx * dx + dy * dy <= 2 else VIO)
px(img, 24, 14, W_)     # el núcleo brillante
# EL LIBRO ABIERTO debajo (la fuente de la frase): dos páginas M.
for x in range(8, 22):
    px(img, x, 23, M_CLARO if x != 14 and x != 15 else K)
    px(img, x, 24, M if x != 14 and x != 15 else K)
    px(img, x, 25, M_OSCURO if x != 14 and x != 15 else K)
px(img, 8, 22, K); px(img, 21, 22, K)   # las esquinas del libro
px(img, 14, 26, K); px(img, 15, 26, K)  # el lomo
save(img, os.path.join(W_COSMIC, 'DignosLineaDeLectura.png'))

# ======================================================================
#  6 · EL MARCAPÁGINAS — el listón colgando con su brillo
# ======================================================================
img = nuevo()
# EL LISTÓN VERTICAL (ancho 4, largo 17) con contorno K y brillo W.
for y in range(5, 22):
    px(img, 12, y, K); px(img, 13, y, ORO_CLARO if y < 12 else ORO)
    px(img, 14, y, ORO); px(img, 15, y, ORO_OSCURO)
    px(img, 16, y, K)
# el reflejo del brillo (la cinta satinada).
px(img, 13, 7, W_); px(img, 13, 8, W_)
# LA PUNTA TRIANGULAR VIOLETA (el final del listón).
for i in range(4):
    y = 22 + i
    for x in range(13 - i, 16 + i):
        px(img, x, y, VIO if x < 16 else VIO_OSCURO)
px(img, 14, 25, VIO_CLARO); px(img, 15, 25, VIO_OSCURO)
# EL ARO DORADO arriba (por donde cuelga).
for dy in (-2, 2):
    for dx in (-2, 2):
        d2 = dx * dx + dy * dy
        if 2 <= d2 <= 5:
            px(img, 14 + dx, 4 + dy, ORO_OSCURO)
# LA PÁGINA detrás (de donde cuelga): rectangulito claro inclinado.
for y in range(6, 24):
    dx = 5 - (y - 6) // 4
    for x in range(6, 10):
        px(img, x + dx, y, (216, 200, 168, 255) if x < 9 else (170, 150, 120, 255))
# contorno de la página.
for y in range(6, 24):
    dx = 5 - (y - 6) // 4
    px(img, 5 + dx, y, K)
px(img, 10 + 5, 6, K)
for x in range(6, 10):
    px(img, x + 5, 5, K); px(img, x + 4, 23, K)
# LOS RENGLONES de la página.
for y in (9, 12, 15, 18):
    dx = 5 - (y - 6) // 4
    for x in range(7, 9):
        px(img, x + dx - 1, y, M_OSCURO)
save(img, os.path.join(W_COSMIC, 'DignosMarcapaginas.png'))

# ======================================================================
#  7 · LA BOLSA DE LOS PROYECTILES DIGNOS — el saco con la binaria
# ======================================================================
img = nuevo()
# EL SACO (el patrón de la BolsaDeLaCria): cuero M con contorno K.
# cuerpo del saco (redondeado).
for y in range(8, 29):
    for x in range(3, 27):
        dentro = False
        if 10 <= y <= 26 and 4 <= x <= 25:
            dentro = True
        elif 8 <= y <= 9 and 7 <= x <= 22:
            dentro = True
        elif 27 <= y <= 28 and 7 <= x <= 22:
            dentro = True
        elif y in (26, 27) and 5 <= x <= 24:
            dentro = True
        if dentro:
            borde = (x <= 4 or x >= 25 or y <= 8 or y >= 28 or
                     (y <= 10 and (x <= 7 or x >= 22)) or
                     (y >= 26 and (x <= 7 or x >= 22)))
            if borde:
                px(img, x, y, K)
            else:
                # el sombreado del cuero (izquierda clara, derecha oscura).
                c = M_CLARO if x <= 9 else (M if x <= 19 else M_OSCURO)
                px(img, x, y, c)
# EL CUELLO DEL SACO (la garga estrechada + el cordel R).
for y in range(3, 8):
    for x in range(11, 19):
        if y == 3 or y == 7 or x == 11 or x == 18:
            px(img, x, y, K)
        else:
            px(img, x, y, M_CLARO if x < 15 else M)
# EL CORDEL (rojo, anudado).
for x in range(10, 20):
    if x not in (10, 19):
        px(img, x, 6, R if x not in (14, 15) else K)
px(img, 14, 6, R); px(img, 15, 6, R)
px(img, 12, 7, R); px(img, 17, 7, R)   # las puntas del lazo
# LAS OREJAS DEL SACO (los picos del cuero a los lados del cuello).
for (x, y) in [(9, 9), (10, 8), (20, 8), (21, 9)]:
    px(img, x, y, M_OSCURO)
# LA BINARIA ESTAMPADA (el símbolo de la familia en el frente).
disco(img, 12, 16, 3, ORO, highlight=W_, hl_off=(-1, -1))
disco(img, 18, 20, 3, VIO, highlight=W_, hl_off=(-1, -1))
# el eje entre ambos.
px(img, 15, 17, ORO_OSCURO); px(img, 16, 18, VIO_OSCURO)
# LOS TRES GLIFOS bajo la binaria (los seis candidatos).
for gx in (11, 15, 19):
    px(img, gx, 25, ORO_CLARO); px(img, gx + 1, 25, ORO)
save(img, os.path.join(W_BOLSAS, 'BolsaProyectilesDignos.png'))

print('LISTO — 7 iconos.')
