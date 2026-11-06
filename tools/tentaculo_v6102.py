#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
tentaculo_v6102.py — v6.102 — LA FORJA DEL TENTÁCULO DEL ABISMO.

LA LETRA DEL USUARIO: «rediseña la Sombra de la Página V, debe ser
igual que la original, pero el tentaculo debe ser rediseñado, con
nuevas texturas, usa la forja o el taller para crear las texturas
del tentaculo».

LA CADENA DE LA FORJA/TALLER (la de siempre, vestida para el QUINTO
TOMO — la piel del tentáculo deja de ser el horneado matemático de
VFXCore.Carne y pasa a ser UNA TEXTURA DE VERDAD, forjada):

  LA PLUMA (z-ai image) — el borrador IA de la piel abisal: la
      columna de carne violeta-negra con sus ridges musculares, las
      dos hileras de ventosas que brillan, las venas carmesí.
  EL MOLDE (la casa) — cutout real por flood-fill, la rejilla por
      celdas (128×256), la cuantización a LA PALETA DEL TOMO V (no
      la del libro: esta piel es violeta-negra-carmesí, la familia
      de las sombras).
  LA SASTRERÍA (el formato del Ribbon) — LA MÁSCARA U suave (los
      bordes que respiran, como HornearCarne: nada en 0/1, casi
      opaco en 0.2..0.8), EL ESPEJO PERIÓDICO (128×256 → 128×512:
      la mitad baja es la alta INVERTIDA — la costura C0 en los dos
      extremos, el truco del espejo) y EL PREMULTIPLICADO de la
      convención (q,q,q,q) del lote alfa (rgb·m, a=255·m).
  EL OJO (z-ai vision) — el QA VLM de la casa sobre la hoja.

Salida: AethonMod/Content/Effects/TentaculoAbismo.png (128×512,
premultiplicada, periódica en V) — la piel nueva del quinto tomo,
muestreada por VFXCore.Ribbon (uvFlow: la carne AVANZA raíz→punta).

TODO con transparencia NATIVA (la regla permanente de la casa: la
transparencia vive en la GENERACIÓN — la máscara U nace aquí).
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
CARPETA = '/tmp/forja_tentaculo'

# ====================================================================
#  LA PALETA DEL TOMO V (la familia de las sombras — violeta, negro,
#  carmesí; la del LIBRO no: esta piel arde en otro color)
# ====================================================================
PALETA_TOMO_V = [
    (10, 6, 16),      # EL NÚCLEO — el vacío que se come la luz
    (18, 12, 26),     # la carne negra de la masa
    (46, 24, 78),     # el violeta hondo de las concavidades
    (74, 36, 128),    # la base de la carne vieja
    (120, 8, 12),     # la sangre apagada
    (150, 84, 235),   # EL VIOLETA VIVO — las ventosas que brillan
    (198, 18, 24),    # LAS VENAS CARMESÍ — la sangre que corre
    (200, 150, 255),  # el brillo del borde
    (238, 232, 220),  # el hueso (por si asoma)
    (248, 244, 255),  # EL DESTELLO
]


# ====================================================================
#  ETAPA 1 — LA PLUMA (el borrador IA de la piel del abismo)
# ====================================================================
def la_pluma(destino):
    """La piel del tentáculo del abismo: columna de carne frontal,
    ridges verticales, dos hileras de ventosas, venas carmesí — SIN
    contorno duro (los bordes los cose la máscara U) y SIN ojos ni
    boca (los ojos y la fauce VIVEN: los dibuja el juego).
    QA r2 (EL OJO): «cinturas estrechas rompen la continuidad» — la
    columna ahora es de ANCHO CONSTANTE, lados paralelos, cero
    pinzamientos, y las ventosas PLANAS contra la piel (que no
    abomben la silueta)."""
    prompt = (
        "Pixel art texture for a Terraria-style game. A vertical close-up "
        "segment of an abyssal tentacle: a perfectly straight vertical column "
        "of thick muscular flesh of CONSTANT UNIFORM WIDTH from top to "
        "bottom, smooth parallel sides, no pinching, no narrow waists, no "
        "bulging, facing the viewer, filling the whole frame from top to "
        "bottom. Deep violet-black skin with fine vertical muscle ridges, "
        "two staggered rows of small round glowing violet suckers FLAT "
        "against the skin, thin branching crimson veins just under the "
        "skin, faint violet bioluminescent glow inside the crevices, moist "
        "highlights on the ridges. No eyes, no mouth, no teeth, no "
        "background scenery, no outline around the subject. Limited "
        "palette: deep violet-black #120A1A flesh, dark violet #4A2480 "
        "shadows, bright violet #9654EB glowing suckers, crimson #C61218 "
        "veins, pale violet #E0CFFF highlights. Chunky clean large pixels, "
        "no anti-aliasing, flat solid pure green background (#00FF00) "
        "chroma key, subject centered filling 85% of frame width"
    )
    r = subprocess.run(['z-ai', 'image', '-p', prompt, '-o', destino, '-s', '864x1152'],
                       capture_output=True, text=True, timeout=240)
    if not os.path.exists(destino):
        print(r.stdout[-400:] if r.stdout else '', r.stderr[-400:] if r.stderr else '')
        raise SystemExit('LA PLUMA falló: la generación no devolvió imagen')
    print(f'  LA PLUMA: {destino}')


# ====================================================================
#  ETAPA 2 — EL MOLDE (cutout → rejilla → la paleta del tomo)
# ====================================================================
def cuantizar_tomo(im):
    """La paleta del QUINTO TOMO: cada píxel vuela a SU color más
    cercano (umbral 100 de distancia: más lejos, se queda con su tono
    real — la humedad del borrador NO se aplasta)."""
    px = im.load()
    w, h = im.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if a == 0:
                continue
            mejor, dmin = None, 101
            for c in PALETA_TOMO_V:
                d = abs(r - c[0]) + abs(g - c[1]) + abs(b - c[2])
                if d < dmin:
                    mejor, dmin = c, d
            if mejor is not None:
                px[x, y] = (mejor[0], mejor[1], mejor[2], a)
    return im


def el_molde(borrador):
    """EL SPRITE de la casa: cutout real, la rejilla por celdas y la
    cuantización a la paleta del tomo (128×256 — el módulo de la piel)."""
    im = Image.open(borrador).convert('RGBA')
    recorte = bbox(cutout(im))
    sprite = celdas(recorte, 128, 256)
    sprite = cuantizar_tomo(sprite)
    print(f'  EL MOLDE: piel 128×256 (bbox del borrador: {recorte.size})')
    return sprite


# ====================================================================
#  ETAPA 3 — LA SASTRERÍA (el formato del Ribbon de VFXCore)
# ====================================================================
def suave(u, a, b):
    """El smoothstep de la casa (la máscara de HornearCarne)."""
    if u <= a:
        return 0.0
    if u >= b:
        return 1.0
    t = (u - a) / (b - a)
    return t * t * (3.0 - 2.0 * t)


def la_sastreria(sprite):
    """LA PIEL DEL RIBBON — tres costuras:
    (1) LA MÁSCARA U: nada en 0/1, casi opaco en 0.20..0.80 — el
        borde de la cinta RESPIRA, no corta a cuchillo;
    (2) EL ESPEJO PERIÓDICO: 128×256 → 128×512 (la mitad baja es la
        alta INVERTIDA) — la costura es C0 en 0, 0.5 y 1: la carne
        FLUYE continua con uvFlow sin saltos;
    (3) EL PREMULTIPLICADO: rgb·m con a=255·m — la convención
        (q,q,q,q) del lote alfa One/InverseSourceAlpha de la casa.
    """
    w, h = sprite.size
    px = sprite.load()

    # (1) LA MÁSCARA U — los bordes que respiran
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if a == 0:
                continue
            u = x / (w - 1)
            m = suave(u, 0.02, 0.20) * (1.0 - suave(u, 0.80, 0.98))
            if m <= 0.003:
                px[x, y] = (0, 0, 0, 0)
                continue
            # la opacidad del cuerpo: densa en el corazón (0.2..0.8 llena)
            cuerpo = 0.35 + 0.65 * m
            px[x, y] = (
                int(r * cuerpo), int(g * cuerpo), int(b * cuerpo),
                int(255 * min(1.0, m * 1.25)))

    # (2) EL ESPEJO PERIÓDICO — 512 con las dos costuras C0
    tela = Image.new('RGBA', (128, 512), (0, 0, 0, 0))
    tela.paste(sprite, (0, 0))
    tela.paste(sprite.transpose(Image.FLIP_TOP_BOTTOM), (0, 256))

    # (3) EL PREMULTIPLICADO — (q,q,q,q) sobre el resultado
    tp = tela.load()
    for y in range(512):
        for x in range(128):
            r, g, b, a = tp[x, y]
            if a == 0:
                continue
            k = a / 255.0
            tp[x, y] = (int(r * k), int(g * k), int(b * k), a)
    return tela


# ====================================================================
#  EL MONTAJE — la hoja de contacto para EL OJO
# ====================================================================
def el_montaje(tela):
    """La piel ×1 al lado de TRES BANDAS del Ribbon (lo que el juego
    ve de verdad: franjas de 64 px muestreadas por VFXCore.Ribbon) y
    la COLUMNA SIMULADA (4 bandas consecutivas apiladas: la carne
    fluye continua de raíz a punta)."""
    piezas = []
    piezas.append(('la piel 128×512', tela))
    for k in range(3):
        y0 = 40 + k * 150
        banda = tela.crop((0, y0, 128, y0 + 64)).resize((256, 128), Image.NEAREST)
        piezas.append((f'banda Ribbon {k}', banda))
    # LA COLUMNA VIVA: bandas de 64 SOLAPADAS al 25% (stride 48 — la
    # convención del Ribbon: cada segmento se extiende sobre el siguiente
    # y la carne fluye CONTINUA, sin huecos ni costuras).
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
    hoja.save('/tmp/tentaculo_qa.png')
    print('  EL MONTAJE: /tmp/tentaculo_qa.png')
    return hoja


def main():
    os.makedirs(CARPETA, exist_ok=True)
    borrador = os.path.join(CARPETA, 'borrador.png')
    if not os.path.exists(borrador):
        print('LA FORJA — etapa 1: LA PLUMA (la piel del abismo)')
        la_pluma(borrador)
    else:
        print('LA FORJA — etapa 1: LA PLUMA (borrador existente)')

    print('LA FORJA — etapa 2: EL MOLDE (cutout → rejilla → paleta del tomo)')
    sprite = el_molde(borrador)
    sprite.save(os.path.join(CARPETA, 'molde.png'))

    print('LA FORJA — etapa 3: LA SASTRERÍA (máscara U + espejo + premultiplicado)')
    tela = la_sastreria(sprite)
    destino = os.path.join(BASE, 'Content', 'Effects', 'TentaculoAbismo.png')
    tela.save(destino)
    print(f'  guardada: {destino} {tela.size}')

    print('LA FORJA — el montaje para EL OJO')
    el_montaje(tela)
    print('\nEL OJO de la casa:  node tools/vlm_tentaculo_v6102.mjs')


if __name__ == '__main__':
    main()
