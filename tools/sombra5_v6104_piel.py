#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
sombra5_v6104_piel.py — v6.104 — LA FORJA DE LA PIEL LISA DEL QUINTO TOMO.

LA LETRA DEL USUARIO: «mejora aun mas las texturas de la sombra de la
pagina 5, recuerda que es un tentaculo LISO, con ojos y colmillos, si
lo necesitas siempre puedes crear los ojos y los colmillos con la
forja o el taller».

Este script forja LA PIEL LISA (TentaculoLiso.png): el cuerpo del
tentáculo del quinto tomo DEJA de vestir la carne del abismo de la
v6.102 (ridges musculares + DOS HILERAS DE VENTOSAS) y pasa a ser PIEL
LISA, HÚMEDA Y BRILLANTE — sin sierra dorsal, sin ventosas, sin
ridges. La cadena es LA DE LA CASA (la misma del tentaculo_v6102):

  LA PLUMA (z-ai image) — el borrador IA de la piel lisa: la columna
      de carne lisa y húmeda, el reflejo violáceo, la veta carmesí
      sutil (el LOOK; el formato lo cose la sastrería).
  EL MOLDE (la casa) — cutout real por flood-fill, la rejilla por
      celdas (128×256) y la cuantización SUAVE a la paleta lisa (más
      pasos de sombra que la del tomo: la piel lisa vive de sus
      degradados, no de sus bordes).
  LA SASTRERÍA LISA (el formato del Ribbon de VFXCore) —
      (1) LA COLUMNA CONSTANTE: cada fila se RELLENA horizontalmente
          con el color opaco más cercano (el borrador IA puede
          ondular la silueta: aquí MUERE todo bulto — LISO de verdad);
      (2) LA VENTANA SUAVE: la silueta final es UNA VENTANA U suave
          (núcleo opaco 0.34..0.66, fundido 0.14..0.34 / 0.66..0.86 —
          las MISMAS proporciones de la piel del abismo para que el
          cuerpo pese lo mismo en pantalla);
      (3) EL BRILLO HÚMEDO GARANTIZADO: una banda especular vertical
          (u≈0.40, blanco-violácea, con su ondulación húmeda a lo
          largo) + el sombreado del borde (u>0.62 se apaga hacia el
          filo) + LA VETA CARMESÍ MUY SUTIL (una sola, serpenteando);
      (4) LA MÁSCARA U de la casa (nada en 0/1, casi opaco en
          0.2..0.8 — los bordes que respiran, como HornearCarne);
      (5) EL ESPEJO PERIÓDICO 128×256 → 128×512 (la mitad baja es la
          alta INVERTIDA — costura C0 en 0, 0.5 y 1: la carne FLUYE
          continua con uvFlow sin saltos);
      (6) EL PREMULTIPLICADO (q,q,q,q) — la convención del lote alfa
          One/InverseSourceAlpha de la casa.
  LA VERIFICACIÓN — tamaño EXACTO 128×512 RGBA, premultiplicado
      (rgb ≤ a; a=0 → rgb=0), periodicidad V (fila 0 == fila 511 y
      fila 255 == fila 256 — la costura C0 del espejo) y bordes U
      (columnas 0 y 127 transparentes). Imprime OK/FAIL.
  EL MONTAJE — la hoja de contactos para EL OJO (QA VLM).

Salida: AethonMod/Content/Effects/TentaculoLiso.png (128×512,
premultiplicada, periódica en V) — la PIEL LISA del quinto tomo,
muestreada por VFXCore.Ribbon (uvFlow: la carne AVANZA raíz→punta) y
por SombrasLib.MasaLisa (que además le pasa EL BRILLO HÚMEDO aditivo
por encima — el «wet look» de la letra).

TODO con transparencia NATIVA (la regla permanente de la casa: la
transparencia vive en la GENERACIÓN — la máscara U nace aquí).

USO:
  python3 tools/sombra5_v6104_piel.py             # forja completa
  python3 tools/sombra5_v6104_piel.py --forzar    # regenera la pluma
  python3 tools/sombra5_v6104_piel.py --pil       # sin IA: 100% PIL
"""
import math
import os
import subprocess
import sys

from PIL import Image

AQUI = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, AQUI)
from gen_altar_v695 import cutout, bbox, celdas  # noqa: E402

BASE = os.path.join(AQUI, '..', 'AethonMod')
CARPETA = '/tmp/forja_tentaculo_liso'
DESTINO = os.path.join(BASE, 'Content', 'Effects', 'TentaculoLiso.png')

FORZAR = '--forzar' in sys.argv
SOLO_PIL = '--pil' in sys.argv

# ====================================================================
#  LA PALETA LISA (la piel lisa vive de sus DEGRADADOS: más pasos de
#  sombra que la del tomo — violeta-negro húmedo con su brillo)
# ====================================================================
PALETA_LISA = [
    (5, 3, 9),       # EL BORDE — donde la piel se apaga en la sombra
    (10, 6, 17),     # el cuerpo hondo
    (16, 10, 26),    # la carne negra de la masa
    (24, 14, 38),    # el violeta sombra
    (34, 20, 56),    # el violeta hondo
    (48, 28, 80),    # la carne violeta
    (70, 42, 114),   # el reflejo violáceo apagado
    (100, 64, 152),  # el reflejo violáceo
    (138, 98, 196),  # EL BRILLO HÚMEDO — la banda que resbala
    (184, 148, 232), # el destello mojado
    (108, 14, 20),   # la veta carmesí apagada
    (156, 26, 30),   # LA VETA CARMESÍ — sutil, una sola
]

# EL TINTE del brillo garantizado (blanco-violáceo, el «wet look»)
BRILLO_RGB = (205, 178, 250)
# LA VETA (carmesí muy sutil — la sangre que corre por dentro)
VETA_RGB = (140, 22, 26)


# ====================================================================
#  ETAPA 1 — LA PLUMA (el borrador IA de la piel lisa y húmeda)
# ====================================================================
def la_pluma(destino):
    """LA PIEL LISA: MUESTRA DE TEXTURA — macro extremo de la
    superficie de piel lisa y húmeda. La lección del QA r1 (EL OJO:
    «se ven OJOS y tentaculos con rizos/ventosas»): pedir «un
    tentáculo» invita a la pluma a pintar LA CRIATURA ENTERA con sus
    ojos y sus rizos. Aquí NO se pide criatura: se pide la MUESTRA de
    superficie que LLENA EL ENCUADRE de borde a borde (la piel del
    quinto tomo es LISA — la letra: «recuerda que es un tentaculo
    LISO»), SIN ventosas, SIN ridges, SIN ojos, SIN dientes."""
    prompt = (
        "Pixel art texture swatch for a Terraria-style game. An extreme "
        "macro close-up of a SMOOTH WET monster skin SURFACE ONLY: the "
        "texture fills the ENTIRE frame edge to edge, wall to wall, no "
        "background visible anywhere, no subject, no creature, no silhouette "
        "- just the flat surface itself. Completely smooth hairless glossy "
        "wet flesh like polished wet leather or slick rubber, deep "
        "violet-black skin with soft violet bioluminescent sheen, one broad "
        "soft vertical specular highlight stripe on the left side, subtle "
        "moisture gradient. STRICTLY FORBIDDEN: suckers, suction cups, "
        "ridges, muscles, veins bumps, spines, barbs, eyes, mouths, teeth, "
        "tentacle shapes, curls, coils, any creature, any object, any "
        "outline. Limited palette: deep violet-black #100A1A base, dark "
        "violet #30204E shadows, soft violet #6440A0 reflections, pale "
        "violet #B894E8 wet highlight stripe. Chunky clean large pixels, no "
        "anti-aliasing, subject fills 100% of the frame"
    )
    r = subprocess.run(['z-ai', 'image', '-p', prompt, '-o', destino, '-s', '864x1152'],
                       capture_output=True, text=True, timeout=240)
    if not os.path.exists(destino):
        print(r.stdout[-400:] if r.stdout else '', r.stderr[-400:] if r.stderr else '')
        raise SystemExit('LA PLUMA falló: la generación no devolvió imagen')
    print(f'  LA PLUMA: {destino}')


# ====================================================================
#  ETAPA 2 — EL MOLDE (cutout → rejilla → la paleta lisa)
# ====================================================================
def cuantizar_lisa(im, umbral=90):
    """La cuantización SUAVE de la piel lisa: cada píxel vuela a SU
    color más cercano de la paleta SI está cerca (umbral 90 — más
    lejos se queda con su tono real: la humedad del borrador NO se
    aplasta, los degradados sobreviven)."""
    px = im.load()
    w, h = im.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if a == 0:
                continue
            mejor, dmin = None, umbral + 1
            for c in PALETA_LISA:
                d = abs(r - c[0]) + abs(g - c[1]) + abs(b - c[2])
                if d < dmin:
                    mejor, dmin = c, d
            if mejor is not None:
                px[x, y] = (mejor[0], mejor[1], mejor[2], a)
    return im


def quitar_verde_interior(im):
    """LA LECCIÓN DEL QA r1 (EL OJO: «hay fondo VERDE visible»): el
    chroma verde puede colarse DENTRO del sujeto (entre rizos del
    borrador) donde el flood-fill del cutout no llega. Todo píxel
    verde-dominante se marca transparente — la COLUMNA CONSTANTE lo
    rellena después con el color opaco más cercano de su fila."""
    px = im.load()
    w, h = im.size
    muertos = 0
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if a > 0 and g > 90 and g > r + 28 and g > b + 28:
                px[x, y] = (0, 0, 0, 0)
                muertos += 1
    if muertos:
        print(f'  EL VERDE INTERIOR: {muertos} px verdes fuera')
    return im


def la_compresion(sprite):
    """LA COMPRESIÓN DE LUMINANCIA (la lección del QA r3: «nodos
    ovales claros» — las manchas brillantes del borrador IA que
    sobreviven al alisado). La piel LISA no lleva NODOS: cada píxel se
    comprime hacia la LUMINANCIA DE LA RAMPA de su columna (la rampa
    vertical u→violeta de la paleta) con un margen corto — las manchas
    claras mueren, los degradados sobreviven. Determinista: sin IA, la
    última pasada de la sastrería."""
    w, h = sprite.size
    px = sprite.load()
    # la luminancia objetivo por columna: la rampa de la piel (más
    # clara hacia la izquierda, donde vive el brillo garantizado)
    def objetivo(u):
        return 26.0 + (1.0 - u) * 66.0
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if a == 0:
                continue
            u = x / (w - 1)
            lum = 0.30 * r + 0.55 * g + 0.15 * b
            tope = objetivo(u) + 26.0        # margen corto: nada de nodos
            if lum > tope:
                k = tope / lum if lum > 0 else 1.0
                px[x, y] = (int(r * k), int(g * k), int(b * k), a)
    return sprite


def el_molde(borrador):
    """EL SPRITE de la casa: cutout real, EL VERDE INTERIOR (la
    lección del QA r1), la rejilla por celdas, EL ALISADO (la lección
    del QA r2: «moteado/ruido granular» — un desenfoque suave y la
    re-cuantización dejan la piel LISA de verdad: bandas limpias, no
    grano), LA COMPRESIÓN de luminancia (la lección del QA r3: fuera
    los nodos ovales claros) y la cuantización suave (128×256 — el
    módulo de la piel)."""
    im = Image.open(borrador).convert('RGBA')
    recorte = bbox(cutout(im))
    recorte = quitar_verde_interior(recorte)
    sprite = celdas(recorte, 128, 256)
    sprite = cuantizar_lisa(sprite)
    # EL ALISADO (QA r2): fuera el grano — la piel LISA vive de sus
    # degradados. Desenfoque suave + re-cuantización = bandas limpias.
    from PIL import ImageFilter
    sprite = sprite.filter(ImageFilter.GaussianBlur(2.2))
    # LA COMPRESIÓN (QA r3): fuera los nodos claros del borrador.
    sprite = la_compresion(sprite)
    sprite = cuantizar_lisa(sprite, umbral=70)
    print(f'  EL MOLDE: piel lisa 128×256 (bbox del borrador: {recorte.size})')
    return sprite


def el_molde_pil():
    """EL MOLDE SIN IA (100% PIL — si la pluma no clava el look): el
    moteado orgánico de la piel húmeda nace de un fBm barato (senos
    enrevesados), la paleta la misma de arriba. Es el BORRADOR, no la
    tela: la sastrería cose igual los dos."""
    W, H = 128, 256
    sprite = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    px = sprite.load()
    for y in range(H):
        for x in range(W):
            u, v = x / (W - 1), y / (H - 1)
            # el moteado húmedo: dos octavas de ruido de senos
            n = (0.5 * math.sin(u * 41.0 + v * 97.0 + math.sin(v * 23.0) * 2.0)
                 + 0.5 * math.sin(u * 89.0 - v * 53.0 + math.sin(u * 31.0) * 3.0))
            m = 0.5 + 0.5 * n
            # la rampa de la piel: hondo → violeta (más claro hacia la
            # izquierda, donde caerá el brillo garantizado)
            t = max(0.0, min(1.0, (1.0 - u) * 0.75 + m * 0.35 - 0.18))
            if t < 0.18:
                c = (10, 6, 17)
            elif t < 0.36:
                c = (16, 10, 26)
            elif t < 0.52:
                c = (24, 14, 38)
            elif t < 0.66:
                c = (34, 20, 56)
            elif t < 0.80:
                c = (48, 28, 80)
            else:
                c = (70, 42, 114)
            px[x, y] = (c[0], c[1], c[2], 255)
    print('  EL MOLDE (PIL): moteado húmedo 128×256')
    return sprite


# ====================================================================
#  ETAPA 3 — LA SASTRERÍA LISA (el formato del Ribbon de VFXCore)
# ====================================================================
def suave(u, a, b):
    """El smoothstep de la casa (la máscara de HornearCarne)."""
    if u <= a:
        return 0.0
    if u >= b:
        return 1.0
    t = (u - a) / (b - a)
    return t * t * (3.0 - 2.0 * t)


def la_columna_constante(sprite):
    """(1) LA COLUMNA CONSTANTE: cada fila del sprite se RELLENA
    horizontalmente — todo píxel transparente toma el color opaco más
    cercano de SU fila (izquierda o derecha, dos pasadas lineales).
    El borrador IA puede ondular y apretar la silueta: aquí MUERE todo
    bulto y toda cintura. La piel queda LISA de verdad: una columna
    perfecta de lados paralelos (la silueta final la pone la VENTANA
    suave de la sastrería, idéntica en todas las filas)."""
    w, h = sprite.size
    px = sprite.load()
    for y in range(h):
        # el opaco más cercano por la izquierda y por la derecha
        izq = [None] * w
        ultimo = None
        for x in range(w):
            izq[x] = ultimo
            if px[x, y][3] > 0:
                ultimo = (x, px[x, y])
        der = [None] * w
        ultimo = None
        for x in range(w - 1, -1, -1):
            der[x] = ultimo
            if px[x, y][3] > 0:
                ultimo = (x, px[x, y])
        # el relleno: el color del vecino opaco MÁS CERCANO
        for x in range(w):
            if px[x, y][3] > 0:
                continue
            i, d = izq[x], der[x]
            if i is None and d is None:
                continue          # fila entera vacía: la llena la sastrería
            if i is None:
                c = d[1]
            elif d is None:
                c = i[1]
            else:
                c = i[1] if (x - i[0]) <= (d[0] - x) else d[1]
            px[x, y] = (c[0], c[1], c[2], 255)
    return sprite


def el_brillo_y_la_veta(tela_base):
    """(3) EL BRILLO HÚMEDO GARANTIZADO + LA VETA CARMESÍ SUTIL sobre
    la tela ya encolumnada (128×256, ANTES del espejo — el espejo
    preserva la costura C0 solito). Trabaja sobre RGB puro (aún sin
    premultiplicar):
      · LA BANDA ESPECULAR en u≈0.40 (ancho ~0.085), blanco-violácea,
        con su ONDULACIÓN húmeda a lo largo (la luz resbala);
      · EL BORDE OSCURO: u>0.62 se apaga hacia el filo (el volumen);
      · LA VETA CARMESÍ: UNA sola, serpenteando sutilísima por
        u≈0.52 (la sangre que corre por dentro — «MUY sutil»)."""
    w, h = tela_base.size
    px = tela_base.load()
    for y in range(h):
        v = y / (h - 1)
        vv = v * math.pi * 2.0        # un período EXACTO del alto
        # LA SIMETRÍA DEL ESPEJO (la lección del QA r2: «el escalón de
        # la veta en la costura espejada»): TODO lo que ondule con V
        # usa cos(2πv) SIN fase — cos es SIMÉTRICO respecto a v=0.5 y
        # v=0, así la mitad espejada reproduce el patrón EXACTO y la
        # costura ni se nota (sin kink, sin escalón).
        cosv = math.cos(vv)
        for x in range(w):
            r, g, b, a = px[x, y]
            if a == 0:
                continue
            u = x / (w - 1)

            # LA BANDA ESPECULAR — el «wet look»
            du = u - 0.40
            banda = math.exp(-du * du / (2.0 * 0.090 * 0.090))
            # la ondulación húmeda: el brillo RESBALA a lo largo (el
            # vaivén en U es libre; el vaivén en V es el cos simétrico)
            mojado = 0.74 + 0.26 * cosv * (0.6 + 0.4 * math.sin(u * 6.283))
            k = banda * mojado
            r += (BRILLO_RGB[0] - r) * 0.62 * k
            g += (BRILLO_RGB[1] - g) * 0.62 * k
            b += (BRILLO_RGB[2] - b) * 0.62 * k

            # EL BORDE OSCURO — el volumen hacia el filo
            sombra = 1.0 - 0.42 * suave(u, 0.60, 0.88)
            r, g, b = r * sombra, g * sombra, b * sombra

            # LA VETA CARMESÍ — una sola, serpenteando, MUY sutil
            # (cos simétrico: la costura del espejo la reproduce exacta)
            uu = 0.52 + 0.030 * cosv
            dv = u - uu
            veta = math.exp(-dv * dv / (2.0 * 0.016 * 0.016))
            veta *= 0.55 + 0.45 * cosv
            veta *= 0.42          # MUY sutil: apenas asoma bajo la piel
            r += (VETA_RGB[0] - r) * veta
            g += (VETA_RGB[1] - g) * veta
            b += (VETA_RGB[2] - b) * veta

            px[x, y] = (int(max(0, min(255, r))),
                        int(max(0, min(255, g))),
                        int(max(0, min(255, b))), a)
    return tela_base


def la_sastreria(sprite):
    """LA PIEL LISA DEL RIBBON — las costuras de la casa y las nuevas
    de la letra:
    (1) la columna constante (todo bulto muere);
    (2) la VENTANA suave (el núcleo opaco 0.34..0.66 — las mismas
        proporciones de la piel del abismo, el cuerpo pesa lo mismo);
    (3) el brillo húmedo garantizado + el borde oscuro + la veta;
    (4) LA MÁSCARA U (nada en 0/1, casi opaco en 0.2..0.8);
    (5) EL ESPEJO PERIÓDICO 128×256 → 128×512 (costura C0 en 0, 0.5
        y 1 — la carne fluye continua con uvFlow);
    (6) EL PREMULTIPLICADO (q,q,q,q) del lote alfa.
    """
    # (1) LA COLUMNA CONSTANTE
    tela = la_columna_constante(sprite.copy())

    # (3) EL BRILLO Y LA VETA (sobre RGB puro, antes de las máscaras)
    tela = el_brillo_y_la_veta(tela)

    # (2)+(4) LA VENTANA SUAVE × LA MÁSCARA U — la silueta LISA que
    #     respira (los bordes NO cortan a cuchillo)
    w, h = tela.size
    px = tela.load()
    for y in range(h):
        # el relleno de emergencia (filas vacías del borrador): el
        # color base de la piel, para que la columna NUNCA se rompa
        fila_vacia = all(px[x, y][3] == 0 for x in range(0, w, 4))
        for x in range(w):
            u = x / (w - 1)
            ventana = suave(u, 0.14, 0.34) * (1.0 - suave(u, 0.66, 0.86))
            mascara = suave(u, 0.02, 0.20) * (1.0 - suave(u, 0.80, 0.98))
            m = ventana * mascara
            if m <= 0.003:
                px[x, y] = (0, 0, 0, 0)
                continue
            if fila_vacia:
                # fila entera vacía (borrador roto): la RAMPA de la
                # piel húmeda — la columna NUNCA se rompe
                t = max(0.0, min(1.0, (1.0 - u) * 0.9))
                rampa = [(5, 3, 9), (10, 6, 17), (16, 10, 26), (24, 14, 38),
                         (34, 20, 56), (48, 28, 80), (70, 42, 114)]
                base = rampa[min(len(rampa) - 1, int(t * len(rampa)))]
                px[x, y] = (base[0], base[1], base[2], 255)
            r, g, b, a = px[x, y]
            cuerpo = 0.35 + 0.65 * m
            px[x, y] = (int(r * cuerpo), int(g * cuerpo), int(b * cuerpo),
                        int(255 * min(1.0, m * 1.25)))

    # (5) EL ESPEJO PERIÓDICO — 512 con las dos costuras C0
    tela512 = Image.new('RGBA', (128, 512), (0, 0, 0, 0))
    tela512.paste(tela, (0, 0))
    tela512.paste(tela.transpose(Image.FLIP_TOP_BOTTOM), (0, 256))

    # (6) EL PREMULTIPLICADO — (q,q,q,q) sobre el resultado
    tp = tela512.load()
    for y in range(512):
        for x in range(128):
            r, g, b, a = tp[x, y]
            if a == 0:
                tp[x, y] = (0, 0, 0, 0)
                continue
            k = a / 255.0
            tp[x, y] = (int(r * k), int(g * k), int(b * k), a)
    return tela512


# ====================================================================
#  ETAPA 4 — LA VERIFICACIÓN (el chequeo técnico de la casa)
# ====================================================================
def verificar(tela):
    ok = True

    def chequeo(nombre, condicion):
        nonlocal ok
        print(f'  [{"OK" if condicion else "FAIL"}] {nombre}')
        if not condicion:
            ok = False

    chequeo('tamaño EXACTO 128×512 RGBA',
            tela.size == (128, 512) and tela.mode == 'RGBA')
    px = tela.load()
    w, h = tela.size

    # PREMULTIPLICADO — (q,q,q,q): rgb ≤ a y a=0 → rgb=0
    mal = 0
    for y in range(0, h, 3):
        for x in range(0, w, 3):
            r, g, b, a = px[x, y]
            if a == 0:
                if (r, g, b) != (0, 0, 0):
                    mal += 1
            elif r > a or g > a or b > a:
                mal += 1
    chequeo(f'premultiplicado rgb≤a (violaciones: {mal})', mal == 0)

    # PERIODICIDAD V — la costura C0 del espejo: fila 0 == fila 511
    # (el muestreo del Ribbon envuelve V al fluir) y fila 255 ==
    # fila 256 (la costura del medio)
    d_borde = sum(abs(px[x, 0][i] - px[x, 511][i]) for x in range(w) for i in range(4))
    d_medio = sum(abs(px[x, 255][i] - px[x, 256][i]) for x in range(w) for i in range(4))
    chequeo(f'periodicidad V: fila0==fila511 (diff {d_borde})', d_borde == 0)
    chequeo(f'periodicidad V: fila255==fila256 (diff {d_medio})', d_medio == 0)

    # BORDES U — las columnas extremas transparentes (la máscara U)
    bordes = sum(px[x, y][3] for x in (0, 1, 126, 127) for y in range(0, h, 2))
    chequeo(f'bordes U transparentes (alfa acumulada {bordes})', bordes == 0)

    # NÚCLEO — el corazón de la piel opaco en el centro
    nucleo = max(px[x, 128][3] for x in range(40, 88))
    chequeo(f'núcleo opaco en el centro (alfa máx {nucleo})', nucleo >= 250)

    # LISURA — sin dientes en el perfil: la anchura visible por fila
    # no puede brincar (una sierra/ridge dejaría saltos)
    anchos = []
    for y in range(0, h, 8):
        xs = [x for x in range(w) if px[x, y][3] > 24]
        anchos.append(max(xs) - min(xs) if xs else 0)
    brincos = sum(1 for i in range(1, len(anchos))
                  if abs(anchos[i] - anchos[i - 1]) > 6)
    chequeo(f'lisura del perfil (saltos >6px: {brincos})', brincos == 0)

    return ok


# ====================================================================
#  ETAPA 5 — EL MONTAJE (la hoja de contactos para EL OJO)
# ====================================================================
def el_montaje(tela):
    """La piel ×1 al lado de TRES BANDAS del Ribbon (lo que el juego
    ve de verdad: franjas de 64 px muestreadas por VFXCore.Ribbon) y
    la COLUMNA SIMULADA (bandas consecutivas apiladas: la piel fluye
    continua de raíz a punta)."""
    piezas = [('la piel 128×512', tela)]
    for k in range(3):
        y0 = 40 + k * 150
        banda = tela.crop((0, y0, 128, y0 + 64)).resize((256, 128), Image.NEAREST)
        piezas.append((f'banda Ribbon {k}', banda))
    col = Image.new('RGBA', (128, 3 * 48 + 64), (0, 0, 0, 0))
    for s in range(4):
        col.paste(tela.crop((0, s * 48, 128, s * 48 + 64)), (0, s * 48))
    piezas.append(('la columna viva', col.resize((256, 704), Image.NEAREST)))

    ancho = sum(p.width for _, p in piezas) + 20 * (len(piezas) + 1)
    alto = max(p.height for _, p in piezas) + 30
    hoja = Image.new('RGBA', (ancho, alto), (24, 24, 34, 255))
    dx = 20
    for _, p in piezas:
        hoja.paste(p, (dx, 22), p)
        dx += p.width + 20
    hoja.save('/tmp/sombra5_piel_qa.png')
    print('  EL MONTAJE: /tmp/sombra5_piel_qa.png')
    return hoja


def el_ojo_vlm():
    """EL OJO de la casa (el QA VLM) sobre el montaje — veredicto
    SHIP/FIX en español (máx 3 iteraciones las maneja quien llama)."""
    r = subprocess.run(['z-ai', 'vision', '-p',
                        'Textura de un mod de Terraria. De izquierda a derecha: '
                        '(1) LA PIEL LISA de un tentáculo del abismo (128×512: una '
                        'tira vertical de carne violeta-negra LISA y húmeda; la MITAD '
                        'INFERIOR de la tira es la superior ESPEJADA — truco '
                        'deliberado de enlosado), (2-4) TRES BANDAS de 64 px (lo que '
                        'el motor muestrea), (5) LA COLUMNA VIVA (bandas solapadas: '
                        'el cuerpo continuo). Responde en espanol, max 5 lineas: '
                        '(1) ¿la piel se lee LISA y HÚMEDA (brillo especular en una '
                        'banda, sin ventosas, sin ridges, sin dientes)? (2) ¿los '
                        'bordes izquierdo y derecho se desvanecen suaves hacia el '
                        'transparente? (3) ¿se ve UNA veta carmesí sutil? (4) ¿la '
                        'columna solapada se lee como UN cuerpo continuo y liso? '
                        '(5) VEREDICTO: SHIP o FIX (que).',
                        '-i', '/tmp/sombra5_piel_qa.png'],
                       capture_output=True, text=True, timeout=180)
    crudo = (r.stdout or '') + '\n' + (r.stderr or '')
    veredicto = ''
    ini = (r.stdout or '').find('{')
    if ini >= 0:
        import json
        try:
            d = json.loads(r.stdout[ini:])
            veredicto = d['choices'][0]['message']['content'].strip()
        except Exception:
            veredicto = ''
    if not veredicto:
        veredicto = crudo.strip()[-1200:] or 'EL OJO no respondió'
    print('  EL OJO (QA VLM):')
    for linea in veredicto.splitlines()[:6]:
        print('   ', linea)
    return veredicto


def main():
    os.makedirs(CARPETA, exist_ok=True)
    borrador = os.path.join(CARPETA, 'borrador.png')

    print('LA FORJA v6.104 — LA PIEL LISA DEL QUINTO TOMO')
    print('  (la letra: «recuerda que es un tentaculo LISO»)')

    if SOLO_PIL:
        print('ETAPA 1: EL MOLDE PIL (sin IA — modo --pil)')
        sprite = el_molde_pil()
    else:
        if FORZAR and os.path.exists(borrador):
            os.remove(borrador)
        if not os.path.exists(borrador):
            print('ETAPA 1: LA PLUMA (la piel lisa y húmeda)')
            try:
                la_pluma(borrador)
            except SystemExit as e:
                print(f'  {e} — cayendo al modo PIL')
                sprite = el_molde_pil()
                borrador = None
        else:
            print('ETAPA 1: LA PLUMA (borrador existente)')
        if borrador:
            print('ETAPA 2: EL MOLDE (cutout → rejilla → paleta lisa)')
            sprite = el_molde(borrador)
            sprite.save(os.path.join(CARPETA, 'molde.png'))

    print('ETAPA 3: LA SASTRERÍA LISA (columna + brillo + máscara + espejo + premult)')
    tela = la_sastreria(sprite)
    tela.save(DESTINO)
    print(f'  guardada: {DESTINO} {tela.size}')

    print('ETAPA 4: LA VERIFICACIÓN')
    ok = verificar(tela)

    print('ETAPA 5: EL MONTAJE para EL OJO')
    el_montaje(tela)

    if '--qa' in sys.argv:
        print('ETAPA 6: EL OJO (QA VLM)')
        el_ojo_vlm()

    print('\nRESULTADO:', 'OK — la piel lisa está forjada' if ok
          else 'FAIL — revisa los chequeos de arriba')
    sys.exit(0 if ok else 1)


if __name__ == '__main__':
    main()
