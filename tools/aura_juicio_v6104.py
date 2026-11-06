#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
aura_juicio_v6104.py — EL AURA DE JUICIO: LAS TEXTURAS DEL PUERTO FIEL
DEL «WARBANNER OF THE RIGHTEOUS» (mecánica y técnica VFX idénticas,
ARTE 100% de la casa — la ley de la limpieza v6.93: cero píxel ajeno).

LA LETRA DEL USUARIO: «copia exactamente el codigo de Warbanner of the
Righteous y sus textura, efectos vfx tecnica todo, en un nuevo item
llamado Aura de Juicio».

QUÉ GENERA CADA FUNCIÓN:
  · gen_circulo_bloom()   → Content/Effects/AuraJuicio/CirculoBloom.png
      200×200 — el BloomCircle del original: gradiente radial
      blanco-centro → transparente-borde con curva pow suave (las
      CINCO CAPAS concéntricas del aura lo apilan).
  · gen_estallido()       → Content/Effects/AuraJuicio/EstallidoFragmentado.png
      512×512 — el ShatteredExplosion: destello radial con ~26
      fracturas angulares finas desde el centro (los DOS GIROS del
      núcleo del aura lo dibujan a escala mínima).
  · gen_bloom_chico()     → Content/Effects/AuraJuicio/BloomChico.png
      200×200 — el SmallBloom: bloom más CONCENTRADO y duro que
      CirculoBloom (el halo de las chispas/luciérnagas).
  · gen_polvo()           → Content/Dusts/PolvoLuzLuciernaga.png
      4×4 — el sprite declarado del ModDust (el dibujo real es manual
      en 3 capas; mismo tamaño que PolvoVacioInvertido.png).
  · gen_metro()           → Content/Systems/AuraJuicioMetro.png
      14×18 — el mini estandarte del medidor «Fuerza del Estandarte».
  · gen_estandarte()      → Content/Items/Accessories/AuraDeJuicioItem.png
      32×390 (5 marcos de 32×78 a 6 ticks) — EL ESTANDARTE DE GUERRA
      dorado-naranja: asta de bronce oscuro, travesaño, PENNANT DE
      GOLONDRA (bifurcado) con PLIEGUES y RUNAS, el SOL RADIANTE DEL
      JUICIO pulsando al centro y las ASCUAS que suben — el respaldo
      procedural de la casa (si la forja IA da mejor arte, ese gana).
  · gen_balloon()         → Content/Items/Accessories/AuraDeJuicioItem_Balloon.png
      30×80 — el estandarte visto SOBRE EL JUGADOR (EquipType.Balloon:
      la tela cargada a la espalda del portador).

REGLA PERMANENTE DE LA CASA: TRANSPARENCIA NATIVA — el lienzo RGBA
nace transparente y solo se pintan los píxeles del objeto; jamás se
post-procesa un fondo.

USO: python3 tools/aura_juicio_v6104.py
"""
import math
from PIL import Image

R = 'AethonMod'

# ============================================================
#  EL HASH DETERMINISTA DE LA CASA (murmur-3 finalizer)
# ============================================================
def h01(semilla, t=0):
    h = (int(semilla) * 0x27D4EB2D + int(t) * 0x165667B1) & 0xFFFFFFFF
    h ^= h >> 15; h = (h * 0x85EBCA6B) & 0xFFFFFFFF
    h ^= h >> 13; h = (h * 0xC2B2AE35) & 0xFFFFFFFF
    h ^= h >> 16
    return h / 0xFFFFFFFF


# ============================================================
#  LA PALETA DEL JUICIO — el oro y el fuego del original
#  (Goldenrod ↔ Orange: el ciclo del aura)
# ============================================================
ORO_VIVO = (255, 200, 80, 255)
ORO_ALTO = (255, 228, 145, 255)
ORO_SOMBRA = (198, 134, 42, 255)
ORO_HONDO = (150, 95, 30, 255)
ORO_BRASA = (255, 165, 45, 255)      # la brasa entre el oro y el fuego
SOL_CENTRO = (255, 246, 200, 255)    # el corazón blanco-cálido
SOL_RAYO = (255, 175, 60, 255)       # los rayos del sol radiante
RUNA = (255, 235, 170, 255)          # las runas que laten
ASTA = (72, 56, 40, 255)             # el asta — bronce nocturno
ASTA_LUZ = (118, 94, 62, 255)        # el filo del asta
REMATE = (255, 214, 110, 255)        # el regatón dorado
ASCUA = (255, 240, 180, 255)         # las ascuas que suben


# ============================================================
#  1) EL CIRCULO DE BLOOM — 200×200, la torre de halos
# ============================================================
def gen_circulo_bloom():
    N = 200
    im = Image.new('RGBA', (N, N), (0, 0, 0, 0))
    px = im.load()
    c = (N - 1) / 2.0
    for y in range(N):
        for x in range(N):
            r = math.hypot(x - c, y - c) / c
            if r >= 1.0:
                continue
            # LA CURVA DEL ORIGINAL: pow suave de larga caída (el
            # apilado de 5 capas necesita colas largas que se fundan)
            a = (1.0 - r) ** 2.15
            # el corazón un pelo más caliente (blanco puro al centro)
            t = min(1.0, a * 1.35)
            rb = int(255 * (0.92 + 0.08 * t))
            gb = int(255 * (0.94 + 0.06 * t))
            px[x, y] = (rb, gb, 255, int(255 * a))
    im.save(f'{R}/Content/Effects/AuraJuicio/CirculoBloom.png')
    return im


# ============================================================
#  2) EL ESTALLIDO FRACTURADO — 512×512, el destello roto
# ============================================================
def gen_estallido():
    N = 512
    im = Image.new('RGBA', (N, N), (0, 0, 0, 0))
    px = im.load()
    c = (N - 1) / 2.0

    # LAS FRACTURAS — 26 picos angulares: cada uno su ángulo, su
    # largo y su grosor (deterministas: la semilla de la casa).
    picos = []
    for i in range(26):
        ang = (i / 26.0) * math.tau + (h01(11 + i) - 0.5) * 0.22
        largo = 0.35 + 0.60 * h01(31 + i)          # 0.35..0.95 del radio
        grosor = 0.010 + 0.030 * h01(71 + i)       # rad (fino)
        picos.append((ang, largo, grosor))

    for y in range(N):
        for x in range(N):
            dx, dy = x - c, y - c
            r = math.hypot(dx, dy) / c
            if r >= 1.0:
                continue
            ang = math.atan2(dy, dx)

            # EL NÚCLEO — el destello radial (más caliente y compacto
            # que el bloom: pow 2.6)
            a = (1.0 - r) ** 2.6

            # LAS FRACTURAS — cada pico aporta si el punto cae en su
            # cuña (grosor que se AFILA al largo: triángulos finos)
            for (pang, plargo, pgrosor) in picos:
                if r > plargo:
                    continue
                d = abs((ang - pang + math.pi) % math.tau - math.pi)
                g = pgrosor * (1.0 - r / plargo) + 0.004
                if d < g:
                    a += (1.0 - r / plargo) ** 1.1 * (1.0 - d / g) * 0.85

            a = min(1.0, a)
            # BLANCO al centro, ámbar en las puntas (las fracturas
            # arden): el degradado del lens-flare roto
            calor = min(1.0, a * 1.6)
            rb = int(255)
            gb = int(200 + 55 * calor)
            bb = int(140 + 115 * calor)
            px[x, y] = (rb, gb, bb, int(255 * a))
    im.save(f'{R}/Content/Effects/AuraJuicio/EstallidoFragmentado.png')
    return im


# ============================================================
#  3) EL BLOOM CHICO — 200×200, el halo duro de las chispas
# ============================================================
def gen_bloom_chico():
    N = 200
    im = Image.new('RGBA', (N, N), (0, 0, 0, 0))
    px = im.load()
    c = (N - 1) / 2.0
    for y in range(N):
        for x in range(N):
            r = math.hypot(x - c, y - c) / c
            if r >= 1.0:
                continue
            # LA CURVA DURA — el SmallBloom del original: núcleo
            # compacto y caída rápida (pow 3.4 + el pequeño refuerzo
            # del corazón): la chispa es un PUNTO, no una nube
            a = (1.0 - r) ** 3.4
            if r < 0.22:
                a = min(1.0, a + (0.22 - r) * 2.2)
            px[x, y] = (255, 250, 235, int(255 * min(1.0, a)))
    im.save(f'{R}/Content/Effects/AuraJuicio/BloomChico.png')
    return im


# ============================================================
#  4) EL POLVO — 4×4 (el sprite declarado; el dibujo es manual)
# ============================================================
def gen_polvo():
    im = Image.new('RGBA', (4, 4), (0, 0, 0, 0))
    px = im.load()
    # la perla: núcleo blanco-cálido con borde dorado tenue
    px[1, 1] = (255, 246, 210, 255)
    px[2, 1] = (255, 250, 225, 255)
    px[1, 2] = (255, 250, 225, 255)
    px[2, 2] = (255, 240, 190, 255)
    px[0, 1] = (255, 210, 110, 90)
    px[1, 0] = (255, 210, 110, 90)
    px[2, 2] = (255, 240, 190, 255)
    px[3, 2] = (255, 210, 110, 90)
    px[2, 3] = (255, 210, 110, 90)
    im.save(f'{R}/Content/Dusts/PolvoLuzLuciernaga.png')
    return im


# ============================================================
#  5) EL MINI ESTANDARTE DEL MEDIDOR — 14×18
# ============================================================
def gen_metro():
    W, H = 14, 18
    L = [[(0, 0, 0, 0)] * W for _ in range(H)]

    def pon(x, y, c):
        if 0 <= x < W and 0 <= y < H:
            L[y][x] = c

    # EL ASTA (con su regatón)
    for y in range(0, 17):
        pon(3, y, ASTA_LUZ)
        pon(4, y, ASTA)
    pon(3, 0, REMATE); pon(4, 0, REMATE)
    # LA TELA — el pennant corto
    for y in range(2, 15):
        prog = (y - 2) / 12.0
        ancho = max(1, round(8 * (1.0 - prog) ** 1.1))
        for x in range(5, 5 + ancho):
            u = (x - 5) / max(1, ancho - 1)
            pliegue = math.sin(u * math.pi * 2.2 + prog * 3.0)
            if u < 0.12 or u > 0.88:
                c = ORO_HONDO
            elif pliegue > 0.45:
                c = ORO_ALTO
            elif pliegue < -0.45:
                c = ORO_SOMBRA
            else:
                c = ORO_VIVO
            pon(x, y, c)
    # EL SOL pequeño (3×3 + rayo)
    for dy in (-2, -1, 0, 1, 2):
        for dx in (-2, -1, 0, 1, 2):
            d = math.hypot(dx, dy)
            if d <= 2.4:
                pon(8 + dx, 6 + dy, SOL_CENTRO if d <= 1.2 else SOL_RAYO)
    pon(8, 3, SOL_RAYO); pon(8, 11, SOL_RAYO)      # el rayo vertical
    pon(5, 7, SOL_RAYO); pon(11, 7, SOL_RAYO)      # el rayo horizontal
    im = Image.new('RGBA', (W, H))
    im.putdata([c for fila in L for c in fila])
    im.save(f'{R}/Content/Systems/AuraJuicioMetro.png')
    return im


# ============================================================
#  6) EL ESTANDARTE-ÍTEM — 32×390 (5 marcos de 32×78)
#     EL ARTE PROPIA DE LA CASA: pennant DE GOLONDRA (bifurcado),
#     runas latiendo, el SOL RADIANTE de 8 rayos, las ascuas.
# ============================================================
W_ITEM, H_ITEM, MARCOS = 32, 78, 5


class Lienzo:
    def __init__(self, w, h):
        self.w, self.h = w, h
        self.px = [[(0, 0, 0, 0)] * w for _ in range(h)]

    def pon(self, x, y, c):
        if 0 <= x < self.w and 0 <= y < self.h:
            self.px[y][x] = c

    def imagen(self):
        img = Image.new('RGBA', (self.w, self.h))
        img.putdata([c for fila in self.px for c in fila])
        return img


def forjar_marco_estandarte(marco):
    L = Lienzo(W_ITEM, H_ITEM)
    t = marco / MARCOS * math.tau
    mecer = math.sin(t) * 2.2            # la tela se mece (viva)
    late = 0.5 + 0.5 * math.sin(t * 2.0)  # el sol late

    # --- 1) EL ASTA — bronce nocturno, y=1..77 ---
    for y in range(1, 78):
        L.pon(3, y, ASTA_LUZ)
        L.pon(4, y, ASTA)
        L.pon(5, y, ASTA)
    L.pon(4, 0, REMATE); L.pon(5, 0, REMATE)
    L.pon(3, 1, REMATE); L.pon(6, 1, ASTA)

    # --- 2) EL TRAVESAÑO — la barra de la que cuelga la tela ---
    for x in range(4, 27):
        L.pon(x, 8, ASTA_LUZ if x % 4 == 0 else ASTA)
        L.pon(x, 9, ASTA)
    L.pon(26, 7, ASTA); L.pon(26, 10, ASTA)   # el tope del gancho

    # --- 3) LA TELA — EL PENNANT DE GOLONDRA: recta arriba (y=11),
    #        abajo se parte en DOS COLAS con la MUESCA al centro
    #        (el borde inferior baja = 52 + 14·|2u−1|^0.8: los extremos
    #        cuelgan hasta 66, el centro sube a 52 — la silueta
    #        bifurcada del estandarte de guerra del original, con la
    #        muesca ANCHA y plana para que se lea a simple vista) ---
    X0, X1 = 6, 25
    for y in range(11, 67):
        for x in range(X0, X1 + 1):
            u = (x - X0) / float(X1 - X0)
            fondo = 50 + 16 * abs(2 * u - 1) ** 0.7   # la golondra (muesca PROFUNDA)
            if y > fondo:
                continue
            prog = (y - 11) / 55.0
            swing = round(mecer * prog + math.sin(t * 2.0 + prog * 5.0) * 0.8 * prog)
            if swing != 0:
                x -= swing                        # la tela pivota (SUAVE: sin cortes)
            if not (X0 <= x <= X1):
                continue
            # LOS PLIEGUES — bandas anchas y tranquilas (poco ruido:
            # la modulación temporal es SUAVE y los bordes anchos)
            pliegue = math.sin(u * math.pi * 2.4 + t * 0.8 + prog * 3.5)
            if u < 0.10 or u > 0.90:
                c = ORO_HONDO
            elif pliegue > 0.55:
                c = ORO_ALTO
            elif pliegue < -0.55:
                c = ORO_SOMBRA
            else:
                c = ORO_VIVO
            L.pon(x, y, c)

    # --- 4) EL SOL RADIANTE DEL JUICIO — disco + 8 RAYOS EXPLÍCITOS
    #        (agujas de 1 px que SALEN del disco: se leen en CADA marco;
    #        el conjunto LATA suave con el marco) ---
    cx, cy = 15 + round(mecer * 0.4), 32
    radio = 7.2 * (0.92 + 0.08 * late)
    for dy in range(-9, 10):
        for dx in range(-9, 10):
            d = math.hypot(dx, dy)
            ang = math.atan2(dy, dx)
            # LOS RAYOS: el radio crece en 8 direcciones (fijas: sin giro)
            rayo = 0.66 + 0.34 * max(0.0, math.cos(ang * 8.0))
            r_rayo = radio * rayo
            if d <= r_rayo:
                if d <= 2.6 + 1.2 * late:
                    c = SOL_CENTRO
                elif d <= r_rayo * 0.62:
                    c = ORO_BRASA
                else:
                    c = SOL_RAYO
                L.pon(cx + dx, cy + dy, c)
    # LAS 8 AGUJAS — 1 px de ancho, del borde del disco hacia afuera
    for k in range(8):
        a = k * math.tau / 8.0
        largo = 9 + round(1.5 * math.sin(t + k))
        for paso in range(6, largo + 1):
            ax = cx + round(math.cos(a) * paso)
            ay = cy + round(math.sin(a) * paso)
            c = SOL_RAYO if paso < largo - 1 else ASCUA
            L.pon(ax, ay, c)

    # --- 5) LAS RUNAS — 3 glifos GRANDES (2×4) que laten bajando por
    #        la tela, con su pixel BLANCO de núcleo: se leen a escala ---
    for k in range(3):
        ry = 44 + k * 7
        brillo = 0.62 + 0.38 * math.sin(t * 2.0 + k * 2.1)
        c = tuple(int(v * brillo) if i < 3 else v for i, v in enumerate(RUNA))
        forma = [(0, 0), (1, 0), (1, 1), (1, 2), (1, 3), (0, 3)] if k % 2 == 0 else \
                [(0, 0), (0, 1), (0, 2), (0, 3), (1, 0), (1, 2), (2, 0), (2, 3)]
        for (rx, ry2) in forma:
            L.pon(13 + rx + round(mecer * 0.2), ry + ry2, c)
        # el núcleo blanco del glifo (el latido legible)
        L.pon(13 + (0 if k % 2 == 0 else 0) + round(mecer * 0.2), ry + 1, SOL_CENTRO)

    # --- 6) LAS ASCUAS — 4 puntos blanco-cálido PEGADOS a la tela
    #        (junto a su borde, subiendo: se leen como SU fuego, no
    #        como píxeles perdidos) ---
    for i in range(4):
        sx = 8 + int(h01(50 + i, 0) * 15) + round(mecer * 0.5)
        sy = 38 - int((marco / MARCOS + h01(51 + i, 3)) * 24) % 26
        L.pon(sx, max(12, sy), ASCUA)
        L.pon(sx, max(12, sy) - 1, RUNA if h01(52 + i, marco) > 0.5 else ASCUA)

    # --- 7) EL FILO DE FUEGO — la tela ARDE por el borde inferior:
    #        lenguas ANCHAS (2 px) y ALTAS (3-6) subiendo por las dos
    #        colas y la muesca, de brasa abajo a punta blanco-cálido ---
    for u_x in range(X0, X1, 2):
        u = (u_x - X0) / float(X1 - X0)
        fondo = 50 + 16 * abs(2 * u - 1) ** 0.7
        f = h01(31 + u_x, marco * 7)
        alto = 3 + round(f * 3)
        for k in range(alto):
            c = ASCUA if k >= alto - 2 else ORO_BRASA
            y_llama = int(fondo) - k
            L.pon(u_x + round(mecer * 0.5), y_llama, c)
            L.pon(u_x + 1 + round(mecer * 0.5), y_llama, c if k < alto - 1 else ORO_BRASA)
    return L


def gen_estandarte():
    tira = Image.new('RGBA', (W_ITEM, H_ITEM * MARCOS), (0, 0, 0, 0))
    for m in range(MARCOS):
        tira.paste(forjar_marco_estandarte(m).imagen(), (0, m * H_ITEM))
    tira.save(f'{R}/Content/Items/Accessories/AuraDeJuicioItem.png')
    return tira


# ============================================================
#  7) EL BALLOON — 30×80: la tela cargada A LA ESPALDA del
#     portador (EquipType.Balloon — el original SE VE sobre el
#     jugador). Mismo arte adaptado: asta corta, tela dorada de
#     golondra, el sol y las runas — MARCO ÚNICO.
# ============================================================
def gen_balloon():
    W, H = 30, 80
    L = Lienzo(W, H)
    t = 1.1                      # pose fija viva (el mecer medio)
    late = 0.8

    # EL ASTA CORTA (el portador la empuña por el medio)
    for y in range(2, 76):
        L.pon(4, y, ASTA_LUZ)
        L.pon(5, y, ASTA)
        L.pon(6, y, ASTA)
    L.pon(5, 1, REMATE); L.pon(6, 1, REMATE); L.pon(4, 2, REMATE)

    # EL TRAVESAÑO
    for x in range(5, 26):
        L.pon(x, 9, ASTA_LUZ if x % 4 == 0 else ASTA)
        L.pon(x, 10, ASTA)

    # LA TELA DE GOLONDRA (más larga: y=12..70 — a la espalda cuelga)
    X0, X1 = 7, 24
    for y in range(12, 71):
        for x in range(X0, X1 + 1):
            u = (x - X0) / float(X1 - X0)
            fondo = 56 + 14 * abs(2 * u - 1)
            if y > fondo:
                continue
            prog = (y - 12) / 58.0
            pliegue = math.sin(u * math.pi * 2.6 + t * 1.6 + prog * 4.5)
            if u < 0.07 or u > 0.93:
                c = ORO_HONDO
            elif pliegue > 0.45:
                c = ORO_ALTO
            elif pliegue < -0.45:
                c = ORO_SOMBRA
            else:
                c = ORO_VIVO
            L.pon(x, y, c)

    # EL SOL RADIANTE (disco + las 8 agujas, como el ítem)
    cx, cy = 15, 30
    radio = 7.0 * late
    for dy in range(-9, 10):
        for dx in range(-9, 10):
            d = math.hypot(dx, dy)
            ang = math.atan2(dy, dx)
            rayo = 0.66 + 0.34 * max(0.0, math.cos(ang * 8.0))
            r_rayo = radio * rayo
            if d <= r_rayo:
                if d <= 2.6 + 1.0:
                    c = SOL_CENTRO
                elif d <= r_rayo * 0.62:
                    c = ORO_BRASA
                else:
                    c = SOL_RAYO
                L.pon(cx + dx, cy + dy, c)
    for k in range(8):
        a = k * math.tau / 8.0
        for paso in range(6, 10):
            L.pon(cx + round(math.cos(a) * paso), cy + round(math.sin(a) * paso),
                  SOL_RAYO if paso < 9 else ASCUA)

    # LAS RUNAS
    for k in range(3):
        ry = 44 + k * 6
        forma = [(0, 0), (1, 0), (1, 1), (1, 2), (0, 2)] if k % 2 == 0 else \
                [(0, 0), (0, 1), (0, 2), (1, 1), (2, 0), (2, 2)]
        for (rx, ry2) in forma:
            L.pon(14 + rx, ry + ry2, RUNA)

    # EL FILO DE FUEGO de las colas (lenguas anchas de 2 px)
    for u_x in range(X0, X1, 2):
        u = (u_x - X0) / float(X1 - X0)
        fondo = 56 + 14 * abs(2 * u - 1)
        f = h01(31 + u_x, 3)
        alto = 3 + round(f * 2)
        for k in range(alto):
            L.pon(u_x, int(fondo) - k, ASCUA if k >= alto - 1 else ORO_BRASA)
            L.pon(u_x + 1, int(fondo) - k, ORO_BRASA if k < alto - 1 else ASCUA)

    im = L.imagen()
    im.save(f'{R}/Content/Items/Accessories/AuraDeJuicioItem_Balloon.png')
    return im


# ============================================================
#  LA HOJA DE CONTACTOS — para el QA visual del ojo
# ============================================================
def hoja_de_contactos(tira, nombre='/tmp/aura_juicio_qa.png', escala=2):
    """Los 5 marcos lado a lado (y el balloon) sobre el fondo de la casa."""
    marcos = [tira.crop((0, m * H_ITEM, W_ITEM, (m + 1) * H_ITEM)) for m in range(MARCOS)]
    balloon = Image.open(f'{R}/Content/Items/Accessories/AuraDeJuicioItem_Balloon.png')
    piezas = [m.resize((m.width * escala, m.height * escala), Image.NEAREST) for m in marcos]
    piezas.append(balloon.resize((balloon.width * escala, balloon.height * escala), Image.NEAREST))
    alto = max(p.height for p in piezas) + 24
    ancho = sum(p.width for p in piezas) + 12 * (len(piezas) + 1)
    lienzo = Image.new('RGBA', (ancho, alto), (24, 24, 34, 255))
    dx = 12
    for p in piezas:
        lienzo.paste(p, (dx, 18), p)
        dx += p.width + 12
    lienzo.save(nombre)
    return nombre


def main():
    gen_circulo_bloom()
    gen_estallido()
    gen_bloom_chico()
    gen_polvo()
    gen_metro()
    tira = gen_estandarte()
    gen_balloon()
    hoja = hoja_de_contactos(tira)
    print('OK — Aura de Juicio: texturas procedurales completas')
    print('   · CirculoBloom 200×200 · EstallidoFragmentado 512×512 · BloomChico 200×200')
    print('   · PolvoLuzLuciernaga 4×4 · AuraJuicioMetro 14×18')
    print('   · AuraDeJuicioItem 32×390 (5 marcos) · _Balloon 30×80')
    print(f'   · hoja de contactos para el ojo: {hoja}')


if __name__ == '__main__':
    main()
