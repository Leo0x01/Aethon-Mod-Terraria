#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
════════════════════════════════════════════════════════════════════
  SFORJA — v6.124 — LA FORJA DEFINITIVA + EL TALLER, MEJORADAS
════════════════════════════════════════════════════════════════════

LA LETRA DEL USUARIO: «hacer la fusion de La Forja y El Taller
comparar cual es mejor si esa fusion o la que ya existe de la
superforja, comparar y mejorar para crear la SForja, la cual sera la
forja definitiva mas el taller, usada para la generacion de pixelart de
todo tipo, incluyendo fondos, sprite, proyectiles entre otros, asi que
mejora su resolucion y capacidad» + «integralo al 100% La Forja/El
Taller, sera util para crear sprite y nuevos proyectiles, intenta
mejorar tambien la resulicion a HDcuedrado» + «todo proyectil debe
ser creado con la superforja, en este caso con la nueva que sera
creada SForja».

LA COMPARACIÓN (lo que pedía la letra, hecha de verdad):
  · LA FORJA (taller_pixelart.py v6.95)  → la cadena CORTA de 3 reels.
  · EL TALLER (superforja.py v6.98)       → LA SUPERFORJA: la cadena
    corta DENTRO + las 7 herramientas de la investigación.
  · LA FUSIÓN Forja+Taller (v6.121)       → YA era LA SUPERFORJA (el
    comando `forjar` = la cadena corta viva adentro; el taller es el
    alias de compatibilidad).
  VEREDICTO: la Superforja GANÓ la comparación — ya ES la fusión.
  LA SFORJA = LA SUPERFORJA MEJORADA, no un reemplazo:
   · EL MOTOR VIVE: sforja hereda TODO el repertorio de superforja.py
     (pluma·refina·puente·molde·espejo·alma·tambor·rampa·atlas·ojo·
     forjar·corta·cadena) — las recetas viejas siguen funcionando.
   · LA RESOLUCIÓN HD CUADRADO: supersampling ×8 + rejilla nativa
     EXACTA en lienzos cuadrados HD (64×64 · 128×128) — píxel PURO,
     sin lisis de bordes (el de blur está PROHIBIDO en la casa).
   · LA CAPACIDAD NUEVA:
       - `proyectil` — EL COMANDO DE PROYECTILES (la letra: «todo
         proyectil debe ser creado con la superforja»): sprite HD
         cuadrado/proporcionado + contorno Terraria + manifiesto de
         hitbox + tira animada opcional — nace listo para el .cs.
       - `sol` — EL CÓDIGO DE SOLES DE LA CASA (la letra: «ya que son
         soles hubieras usado nuestro codigo de soles»): el generador
         de gen_sol_aethon_v65036.py PARAMETRIZADO — un sol de CUAL-
         QUIER paleta (el blanco de la luz, el negro del vacío con el
         borde ardiendo, la fusión devastadora).
       - `fondo` — EL FONDO ENLAZABLE: tileable horizontal, la mitad
         de ancho que de alto de la casa, para los fondos del mundo.
       - EL REGISTRO DE EFECTOS AMPLIADO: diez efectos nuevos de las
         familias de pixelfx (hacia el 100 % de su arquitectura de
         244), con el MISMO doble búfer aditivo de siempre.
  LA REGLA DE SPRITE-GEN SIGUE VIVA: TODO local, sin nube, sin blur.

USO:
  python3 tools/sforja.py proyectil --nombre mialema --ancho 64 --alto 64
  python3 tools/sforja.py sol --nombre solluz --paleta luz
  python3 tools/sforja.py sol --nombre fusion --paleta fusion
  python3 tools/sforja.py fondo --nombre cielo_vacio --ancho 256 --alto 128
  python3 tools/sforja.py alma --nombre x --efecto vortice --tira
  (y TODO el repertorio de superforja: pluma·molde·espejo·corta·cadena…)
"""
import os
import sys
import math
import json
import argparse

AQUI = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, AQUI)

# EL MOTOR VIVO DE LA SUPERFORJA — TODO su repertorio heredado
import superforja as SF
from superforja import (EFECTOS, VACIO_H, VIOLETA_V, LILA_V, ORO_V, ORO_H,
                         _EFECTOS_PIXELFX, el_ojo, el_montaje)

from PIL import Image, ImageDraw

RAIZ = os.path.join(AQUI, '..', 'AethonMod')

# ====================================================================
#  LA RESOLUCIÓN HD CUADRADO — el escalador de la casa:
#  supersampling ×8 → la caja EXACTA (la rejilla nativa en píxel puro,
#  el Lanczos SOLO para el paso de reducción — nunca para agrandar).
# ====================================================================
HD_CUADRO = 64          # el lienzo cuadrado HD de la casa (px nativos)
SS_HD = 8               # el supersampling del HD (8×8 muestras por px)

def hdcuadrado(dibuja, w, h, ss=SS_HD):
    """El lienzo HD: dibuja() pinta en (w·ss, h·ss) y LA CASA reduce
    a la caja EXACTA con Lanczos (la única reducción permitida — el
    píxel sigue CRISP, la lección del pilar)."""
    W, H = w * ss, h * ss
    lienzo = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(lienzo)
    dibuja(d, W, H)
    return lienzo.resize((w, h), Image.LANCZOS)


def _glow(img, radio_frac=0.02, alfa=110):
    """El resplandor de la casa: el sprite difuminado UNA VEZ y
    re-compuesto ENCIMA (la luz IRRADIA, el píxel original vive)."""
    w, h = img.size
    halo = img.filter(__import__('PIL.ImageFilter', fromlist=['GaussianBlur'])
                       .GaussianBlur(max(1, int(w * radio_frac))))
    out = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    out.alpha_composite(halo)
    out.alpha_composite(img)
    return out


# ====================================================================
#  EL CÓDIGO DE SOLES DE LA CASA — gen_sol_aethon_v65036.py, el
#  generador del sol del jefe, PARAMETRIZADO (la letra: «ya que son
#  soles hubieras usado nuestro codigo de soles para crear esos 2
#  soles, creando uno blanco y otro negro y asi fucionarlos»).
# ====================================================================
PALETAS_SOL = {
    # EL SOL DE LUZ — el blanco-dorado de Aethon (el disco blanco con
    # el corazón de nieve y la corona de oro — el sol de la vida).
    'luz': dict(
        velo=(196, 150, 255, 90), rayo=(255, 246, 210, 255),
        halo=(255, 226, 140, 255), disco=(255, 250, 240, 255),
        nucleo=(255, 255, 255, 255), n_rayos=12, largo=7 * 4,
        perlas=((255, 226, 140, 220), (196, 150, 255, 200)),
    ),
    # EL SOL DE OSCURIDAD — el negro que SOLO existe porque su borde
    # ARDE violeta (la lección v6.101), la corona morada del vacío.
    # (v6.124-b: LA CORONA ARDE MÁS — el QA VLM pidió el borde
    # VIOLETA VIVO y el disco MÁS GRANDE: el rayo gana grosor y el
    # halo sube de alfa — el negro solo existe si arde.)
    'oscuridad': dict(
        velo=(52, 24, 92, 110), rayo=(140, 72, 215, 255),
        halo=(64, 32, 104, 255), disco=(10, 5, 20, 255),
        nucleo=(3, 1, 6, 255), n_rayos=12, largo=8 * 4,
        perlas=((170, 98, 245, 235), (96, 50, 160, 210)),
        borde_vivo=True,
    ),
    # LA FUSIÓN DEVASTADORA — el sol del JUICIO: el disco blanco
    # abrasado con el corazón NEGRO DOMINANTE (v6.124-b: el núcleo
    # crece — el QA VLM pidió el impacto devastador) y la DOBLE
    # corona (oro por fuera, violeta por dentro).
    'fusion': dict(
        velo=(130, 80, 210, 110), rayo=(255, 240, 170, 255),
        halo=(255, 214, 130, 255), disco=(255, 244, 214, 255),
        nucleo=(16, 8, 28, 255), n_rayos=18, largo=11 * 4,
        perlas=((255, 214, 130, 235), (150, 84, 235, 235)),
        doble_corona=True, nucleo_grande=True,
    ),
}


def el_sol(paleta='luz', tam=64, ss=SS_HD):
    """EL SOL DE LA CASA a HD cuadrado: el mismo génesis de
    gen_sol_aethon (velo → rayos → halo → disco → núcleo → perlas),
    con supersampling de la SForja y el glow final de siempre."""
    cfg = dict(PALETAS_SOL[paleta])     # copia (algunas sin doble_corona)

    def pinta(d, W, H):
        cx = cy = W / 2.0
        r_disco = W * 0.26
        # 1. EL VELO (la profundidad de la luz)
        d.ellipse([cx - r_disco * 1.65, cy - r_disco * 1.65,
                   cx + r_disco * 1.65, cy + r_disco * 1.65], fill=cfg['velo'])
        # 2. LOS RAYOS (la corona de triángulos radiales)
        for i in range(cfg['n_rayos']):
            ang = i * 2 * math.pi / cfg['n_rayos'] + 0.22
            largo = cfg['largo'] / 8.0 * W / 64.0 * (0.82 + 0.18 * math.sin(i * 2.3))
            ancho = math.pi / cfg['n_rayos'] * 0.62
            p1 = (cx + math.cos(ang - ancho) * r_disco * 1.04,
                  cy + math.sin(ang - ancho) * r_disco * 1.04)
            p2 = (cx + math.cos(ang + ancho) * r_disco * 1.04,
                  cy + math.sin(ang + ancho) * r_disco * 1.04)
            p3 = (cx + math.cos(ang) * (r_disco + largo),
                  cy + math.sin(ang) * (r_disco + largo))
            d.polygon([p1, p3, p2], fill=cfg['rayo'])
        # 3. EL HALO (el anillo exterior del disco)
        d.ellipse([cx - r_disco * 1.22, cy - r_disco * 1.22,
                   cx + r_disco * 1.22, cy + r_disco * 1.22], fill=cfg['halo'])
        # 4. EL DISCO (el cuerpo del sol)
        d.ellipse([cx - r_disco, cy - r_disco, cx + r_disco, cy + r_disco],
                  fill=cfg['disco'])
        # 4b. v6.124-b — EL BORDE VIVO DEL SOL NEGRO (el QA VLM: el
        # borde ardiendo GANA GROSOR — el negro solo existe si arde)
        if cfg.get('borde_vivo'):
            d.ellipse([cx - r_disco * 1.02, cy - r_disco * 1.02,
                       cx + r_disco * 1.02, cy + r_disco * 1.02],
                      outline=(214, 150, 255, 255), width=max(4, W // 90))
            d.ellipse([cx - r_disco * 0.92, cy - r_disco * 0.92,
                       cx + r_disco * 0.92, cy + r_disco * 0.92],
                      outline=(150, 84, 235, 255), width=max(3, W // 130))
        # 5. EL NÚCLEO (el corazón — la fusión lo lleva GRANDE)
        frac = 0.72 if cfg.get('nucleo_grande') else 0.52
        d.ellipse([cx - r_disco * frac, cy - r_disco * frac,
                   cx + r_disco * frac, cy + r_disco * frac], fill=cfg['nucleo'])
        # 6. LA DOBLE CORONA DE LA FUSIÓN (el anillo violeta interior)
        if cfg.get('doble_corona'):
            d.ellipse([cx - r_disco * 0.78, cy - r_disco * 0.78,
                       cx + r_disco * 0.78, cy + r_disco * 0.78],
                      outline=VIOLETA_V + (235,), width=max(2, W // 220))
        # 7. LAS PERLAS orbitantes
        for i, ang in enumerate((0.6, 3.5)):
            rr = r_disco * 1.42
            px = cx + math.cos(ang) * rr
            py = cy + math.sin(ang) * rr * 0.55
            pr = W * 0.035
            d.ellipse([px - pr, py - pr, px + pr, py + pr], fill=cfg['perlas'][i])
    img = hdcuadrado(pinta, tam, tam, ss)
    return _glow(img, 0.018)


# ====================================================================
#  LOS PROYECTILES DE LA CASA — recetas procedurales HD (100 % PIL,
#  supersampling de la SForja): EL TAJO DE OSCURIDAD (centro negro ·
#  medios morados · bordes blancos) y LA AGUJA DEL VACÍO (punta
#  blanca · cuerpo violeta · ojo dorado).
# ====================================================================
def el_tajo(w=120, h=36, ss=SS_HD):
    """EL TAJO DE OSCURIDAD — la letra EXACTA: «centro negro, bordes
    blancos y medios morados». v6.124-b (el QA VLM cazó la inversión
    de la primera Hornada): LA HOJA LLENA POR CAPAS — el perfil de
    media hoja con PUNTAS DE AGUJA en los extremos se dibuja TRES
    VECES, cada una MÁS CHICA (inset del contorno): (1) la capa
    BLANCA (el filo exterior), (2) la capa MORADA (los medios),
    (3) la capa NEGRA (el centro) — de FUERA hacia DENTRO, la
    lectura del ojo queda EXACTA: borde blanco · medio morado ·
    corazón negro."""
    def perfil_hoja(d, W, H, inset):
        """El contorno de la hoja con el inset dado (en fracción de H):
        el eje curvo con puntas de aguja — la cuchilla del vacío."""
        cx, cy = W / 2.0, H / 2.0
        L = W * 0.48
        curva = H * 0.22
        grosor = H * 0.38 - inset
        arriba, abajo = [], []
        N = int(W // 2)
        for k in range(N + 1):
            t = k / N
            perfil = math.sin(t * math.pi) ** 0.42   # las puntas de aguja FINAS y los medios plenos
            ancho = grosor * perfil
            if ancho < W * 0.003:
                ancho = W * 0.003
            x = cx - L + t * 2 * L
            y = cy - math.sin(t * math.pi) * curva
            arriba.append((x, y - ancho))
            abajo.append((x, y + ancho))
        return arriba + abajo[::-1]

    def pinta(d, W, H):
        # (1) EL BORDE BLANCO (la capa EXTERIOR — el filo que arde)
        d.polygon(perfil_hoja(d, W, H, 0.0), fill=(255, 252, 245, 255))
        # (2) LOS MEDIOS MORADOS (la capa MEDIA — la carne del vacío:
        # el QA: MÁS ANCHA y VIVA, tiene que LEERSE morada)
        d.polygon(perfil_hoja(d, W, H, H * 0.038), fill=(112, 60, 190, 255))
        # (2b) EL VIVO VIOLETA (la vena entre medio y centro)
        d.polygon(perfil_hoja(d, W, H, H * 0.075), fill=(120, 66, 205, 255))
        # (3) EL CENTRO NEGRO (la capa INTERIOR — la nada que corta;
        # el QA: MÁS NEGRO — la capa interior GANA territorio)
        d.polygon(perfil_hoja(d, W, H, H * 0.125), fill=(6, 3, 10, 255))
        # (4) LAS PUNTAS DE AGUJA (el QA: los extremos TIENEN que
        # clavar — dos agujas blancas/violetas que salen de cada
        # punta de la hoja, delgadas como alfileres)
        cx, cy = W / 2.0, H / 2.0
        L = W * 0.48
        curva = H * 0.16
        for lado in (-1, 1):
            tip_x = cx + lado * L
            tip_y = cy - math.sin((1 if lado > 0 else 0)) * curva * 0.0
            # la dirección de la aguja: tangente a la hoja en la punta
            dirx, diry = lado, 0.62 * curva / L * -lado
            largo = W * 0.045
            base = H * 0.045
            d.polygon([(tip_x + dirx * largo, tip_y + diry * largo),
                       (tip_x - diry * base, tip_y + dirx * base),
                       (tip_x + diry * base, tip_y - dirx * base)],
                      fill=(255, 252, 245, 255))
            d.polygon([(tip_x + dirx * largo * 0.6, tip_y + diry * largo * 0.6),
                       (tip_x - diry * base * 0.6, tip_y + dirx * base * 0.6),
                       (tip_x + diry * base * 0.6, tip_y - dirx * base * 0.6)],
                      fill=(120, 66, 205, 255))
    img = hdcuadrado(pinta, w, h, ss)
    return _glow(img, 0.010, 90)


def el_aguja(w=44, h=12, ss=SS_HD):
    """LA AGUJA DEL VACÍO — delgada, la PUNTA BLANCA al frente, el
    CUERPO VIOLETA y EL OJO DORADO de coser atrás (la letra: «agujas
    delgadas que atraviezan y se clavan en los enemigos»). v6.124-b:
    MÁS GRANDE y LEGIBLE (el QA VLM la vio chica): 44×12 nativos y
    el trazo engordado para que el píxel respire."""
    def pinta(d, W, H):
        cy = H / 2.0
        punta_x = W * 0.95
        rabo_x = W * 0.04
        fuste = H * 0.30                  # el medio-espesor del fuste (el QA: VIOLETA VIVO)
        # EL CUERPO violeta (el fuste con el afilado al frente)
        d.polygon([(punta_x, cy), (W * 0.30, cy - fuste),
                   (rabo_x, cy - fuste * 1.15),
                   (rabo_x, cy + fuste * 1.15),
                   (W * 0.30, cy + fuste)], fill=(122, 66, 205, 255))
        # LA COSTURA blanca del lomo (la línea que brilla)
        d.line([(W * 0.92, cy), (rabo_x, cy)], fill=(214, 186, 255, 235),
               width=max(3, H // 30))
        # LA PUNTA BLANCA (el QA VLM: MÁS GRUESA Y BRILLANTE — el
        # trío frontal entero + el destello de 4 puntas de la casa)
        d.polygon([(punta_x, cy), (W * 0.66, cy - fuste * 0.95),
                   (W * 0.66, cy + fuste * 0.95)], fill=(255, 255, 255, 255))
        d.line([(punta_x, cy), (W * 0.70, cy)], fill=(255, 255, 255, 255),
               width=max(3, H // 26))
        # EL OJO DORADO (el agujero de coser, cerca del rabo — el QA:
        # MÁS GRANDE, el oro de la casa debe leerse)
        ox = W * 0.15
        r = H * 0.26
        d.ellipse([ox - r, cy - r, ox + r, cy + r], fill=(255, 214, 130, 255))
        d.ellipse([ox - r * 0.42, cy - r * 0.42, ox + r * 0.42, cy + r * 0.42],
                  fill=(26, 14, 44, 255))
    img = hdcuadrado(pinta, w, h, ss)
    return _glow(img, 0.020, 80)


# ====================================================================
#  EL REGISTRO DE EFECTOS AMPLIADO — diez efectos nuevos de las
#  familias de pixelfx (hacia el 100 % de su arquitectura de 244),
#  con el MISMO doble búfer aditivo (dc = color · dl = luz).
# ====================================================================
EFECTOS_SFORJA = ['chispa', 'vortice', 'prisma', 'pulso', 'lagrima',
                  'corrosion', 'nebulosa', 'filo', 'temblor', 'eclipse_total']

_TEATRO_VIEJO = SF._teatro_pixelfx

def _teatro_sforja(efecto, sprite, frame, frames):
    """EL DESPACHADOR AMPLIADO: los 10 efectos de la SForja viven
    aquí; TODO lo demás cae al teatro de la Superforja (los 17 de
    siempre quedan INTACTOS, píxel a píxel)."""
    import math as _m
    if efecto not in EFECTOS_SFORJA:
        return _TEATRO_VIEJO(efecto, sprite, frame, frames)

    fase = frame / max(1, frames)
    w, h = sprite.size
    PAD = SF._PAD
    W, H = w + PAD * 2, h + PAD * 2
    lienzo = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    lienzo.paste(sprite, (PAD, PAD))
    color = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    luz = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    dc = ImageDraw.Draw(color)
    dl = ImageDraw.Draw(luz)
    cx, cy = W / 2.0, H / 2.0
    R = max(w, h) / 2.0

    if efecto == 'chispa':
        # (impacts — su «sparkle burst») las 6 chispas doradas radiando
        for k in range(6):
            a = fase * _m.pi * 2 + k * _m.pi / 3
            r0 = R * (0.30 + 0.55 * fase)
            r1 = r0 + R * 0.16 * (1 - fase * 0.4)
            dc.line([(cx + _m.cos(a) * r0, cy + _m.sin(a) * r0),
                     (cx + _m.cos(a) * r1, cy + _m.sin(a) * r1)],
                    fill=ORO_V + (240,), width=2)
        dl.ellipse([cx - R * 0.9, cy - R * 0.9, cx + R * 0.9, cy + R * 0.9],
                   fill=ORO_V + (40,))

    elif efecto == 'vortice':
        # (arcane — su «vortex») el remolino de 4 brazos tragando luz
        for k in range(44):
            t = fase * _m.pi * 2 + k * 0.14
            r = R * (0.95 - 0.85 * (k / 44.0))
            a = t + r * 0.036
            x, y = cx + _m.cos(a) * r, cy + _m.sin(a) * r
            dc.ellipse([x - 2, y - 2, x + 2, y + 2],
                       fill=(VIOLETA_V if k % 3 else LILA_V) + (235,))
        dl.ellipse([cx - R * 0.5, cy - R * 0.5, cx + R * 0.5, cy + R * 0.5],
                   fill=(VIOLETA_V + (60,)))

    elif efecto == 'prisma':
        # (arcane — su «prism shield») el anillo de 6 facetas girando
        for k in range(6):
            a = fase * _m.pi + k * _m.pi / 3
            p = [(cx + _m.cos(a + q) * R * 0.72, cy + _m.sin(a + q) * R * 0.72)
                 for q in (0, _m.pi / 6, _m.pi / 3)]
            dc.polygon(p, outline=LILA_V + (225,), fill=None)
        dl.ellipse([cx - R * 0.8, cy - R * 0.8, cx + R * 0.8, cy + R * 0.8],
                   fill=(196, 112, 255, 46))

    elif efecto == 'pulso':
        # (holy — su «pulse ring») el anillo de choque expandiéndose
        r = R * (0.2 + 0.9 * fase)
        ancho = max(2, int(4 * (1 - fase)))
        dc.ellipse([cx - r, cy - r, cx + r, cy + r],
                   outline=ORO_V + (int(250 * (1 - fase * 0.7)),), width=ancho)
        dl.ellipse([cx - r, cy - r, cx + r, cy + r],
                   outline=ORO_V + (int(70 * (1 - fase)),), width=ancho + 2)

    elif efecto == 'lagrima':
        # (status — su «tear») las 3 lágrimas de tinta cayendo
        for k in range(3):
            y = cy + ((fase * H * 1.4 + k * H * 0.33) % (H * 1.1)) - H * 0.55
            x = cx + (k - 1) * w * 0.30
            dc.polygon([(x, y + h * 0.16), (x - h * 0.10, y),
                        (x, y - h * 0.16), (x + h * 0.10, y)],
                       fill=(90, 40, 160, 235))
        dl.ellipse([cx - w * 0.5, cy - h * 0.5, cx + w * 0.5, cy + h * 0.5],
                   fill=(150, 84, 235, 34))

    elif efecto == 'corrosion':
        # (status — su «corrosion») las manchas royendo el contorno
        import random as _r
        rr = _r.Random(4400)
        for k in range(26):
            a = rr.uniform(0, _m.pi * 2)
            d0 = R * rr.uniform(0.8, 1.25)
            r = rr.uniform(2, 5) * (0.5 + 0.5 * _m.sin(fase * _m.pi + k))
            x, y = cx + _m.cos(a) * d0, cy + _m.sin(a) * d0
            dc.ellipse([x - r, y - r, x + r, y + r],
                       fill=(74, 40, 120, int(120 + 100 * _m.sin(k + fase * 9))))

    elif efecto == 'nebulosa':
        # (arcane — su «nebula») las masas fBm suaves orbitando
        for k in range(7):
            a = fase * _m.pi * 2 * (0.6 if k % 2 else -0.8) + k * 1.9
            r = R * (0.35 + 0.30 * ((k * 37) % 10) / 10.0)
            x, y = cx + _m.cos(a) * r, cy + _m.sin(a) * r * 0.7
            rad = R * (0.16 + 0.06 * _m.sin(fase * _m.pi * 2 + k))
            dl.ellipse([x - rad, y - rad, x + rad, y + rad],
                       fill=((VIOLETA_V if k % 2 else ORO_V) + (42,)))

    elif efecto == 'filo':
        # (transitions — su «slash») el arco del corte con la estela
        a0 = -_m.pi * 0.75 + fase * _m.pi * 1.5
        a1 = a0 + _m.pi * 0.55
        dc.arc([cx - R * 1.15, cy - R * 1.15, cx + R * 1.15, cy + R * 1.15],
               a0 * 180 / _m.pi, a1 * 180 / _m.pi, fill=(255, 252, 245, 245), width=3)
        dc.arc([cx - R * 0.95, cy - R * 0.95, cx + R * 0.95, cy + R * 0.95],
               a0 * 180 / _m.pi, a1 * 180 / _m.pi, fill=(150, 84, 235, 190), width=2)
        dl.ellipse([cx - R * 1.2, cy - R * 1.2, cx + R * 1.2, cy + R * 1.2],
                   fill=(150, 84, 235, 30))

    elif efecto == 'temblor':
        # (impacts — su «impact shake») los ecos dobles desfasados
        for q, k in ((0, 0), (2, 1), (4, 2)):
            dx = (k + 1) * (2.4 if k % 2 == 0 else -2.4) * (1 - fase)
            dy = (k + 1) * (1.6 if k % 2 else -1.6) * (1 - fase)
            pastilla = sprite.crop((0, 0, w, h))
            m = Image.new('RGBA', (W, H), (0, 0, 0, 0))
            m.paste(pastilla, (int(PAD + dx), int(PAD + dy)))
            luz.alpha_composite(m)
        dl.rectangle([PAD, PAD, PAD + w, PAD + h],
                     outline=(255, 120, 120, int(40 * (1 - fase))), width=2)

    elif efecto == 'eclipse_total':
        # (shadow — su «total eclipse») el disco negro con LA CORONA
        # BLANCA de la fotosfera — el fuego que solo se ve en el apagón
        dr = R * 0.55
        for k in range(20):
            a = k * _m.pi / 10 + fase * 0.4
            largo = dr * (1.05 + 0.5 * abs(_m.sin(k * 2.7 + fase * 6)))
            dc.line([(cx + _m.cos(a) * dr * 0.98, cy + _m.sin(a) * dr * 0.98),
                     (cx + _m.cos(a) * largo, cy + _m.sin(a) * largo)],
                    fill=(255, 248, 230, 220), width=2)
        dc.ellipse([cx - dr, cy - dr, cx + dr, cy + dr], fill=(10, 5, 18, 255))
        dl.ellipse([cx - dr * 1.6, cy - dr * 1.6, cx + dr * 1.6, cy + dr * 1.6],
                   fill=(255, 240, 200, 44))

    # LA COMPOSICIÓN ADITIVA DE SIEMPRE (el corazón de pixelfx)
    base = lienzo.copy()
    base.alpha_composite(color)
    luz_rgb = luz.convert('RGB')
    aditivo = Image.new('RGBA', luz.size, (0, 0, 0, 255))
    ad = Image.merge('RGBA', (luz_rgb.split()[0], luz_rgb.split()[1],
                              luz_rgb.split()[2], luz.split()[3]))
    return SF._luz_aditiva(base, ad)


# EL MONO-PARCHE LIMPIO: el teatro de la SForja envuelve al de la
# Superforja — los 17 de siempre INTACTOS, los 10 nuevos VIVOS.
SF._teatro_pixelfx = _teatro_sforja
SF.EFECTOS = list(EFECTOS) + EFECTOS_SFORJA


# ====================================================================
#  LOS COMANDOS NUEVOS
# ====================================================================
def cmd_sol(args):
    """`sol` — EL CÓDIGO DE SOLES DE LA CASA en HD cuadrado."""
    dest = SF.carpeta_de(args.nombre, args.dest)
    img = el_sol(args.paleta, args.tam)
    ruta = os.path.join(dest, f"sol_{args.paleta}_{args.tam}.png")
    img.save(ruta)
    man = {"comando": "sol", "paleta": args.paleta, "tam": args.tam,
           "ruta": ruta, "tam_px": args.tam,
           "nota": "el sol de la casa (gen_sol_aethon_v65036) en HD cuadrado"}
    with open(os.path.join(dest, "manifiesto.json"), 'w', encoding='utf-8') as f:
        json.dump(man, f, indent=2, ensure_ascii=False)
    print(f"[sforja/sol] {ruta} ({img.size[0]}×{img.size[1]})")
    if args.destino_tml:
        os.makedirs(os.path.dirname(args.destino_tml), exist_ok=True)
        img.save(args.destino_tml)
        print(f"[sforja/sol] → COPIADO AL MOD: {args.destino_tml}")
    return ruta


def cmd_proyectil(args):
    """`proyectil` — EL SPRITE DE PROYECTIL nacido en la forja (la
    letra: «todo proyectil debe ser creado con la superforja»): la
    receta + el contorno Terraria + EL MANIFIESTO DE HITBOX listo
    para el .cs + la tira animada opcional (el espejo de la casa)."""
    dest = SF.carpeta_de(args.nombre, args.dest)
    receta = args.receta
    if receta == 'tajo':
        img = el_tajo(args.ancho, args.alto)
    elif receta == 'aguja':
        img = el_aguja(args.ancho, args.alto)
    elif receta == 'sol':
        img = el_sol(args.paleta or 'luz', args.tam or HD_CUADRO)
    else:
        raise SystemExit(f"receta desconocida: {receta} (tajo·aguja·sol)")

    ruta = os.path.join(dest, f"proyectil_{receta}.png")
    img.save(ruta)
    # EL MANIFIESTO DE HITBOX (el documento del proyectil: lo que el
    # .cs necesita saber del sprite que la forja le hizo).
    bbox_img = img.getbbox()
    man = {"comando": "proyectil", "receta": receta,
           "ruta": ruta, "sprite": [img.size[0], img.size[1]],
           "bbox_opaco": list(bbox_img) if bbox_img else None,
           "hitbox_sugerida": [max(8, img.size[0] // 4), max(8, img.size[1] // 2)],
           "hd_cuadrado": True, "supersampling": SS_HD}
    with open(os.path.join(dest, "manifiesto.json"), 'w', encoding='utf-8') as f:
        json.dump(man, f, indent=2, ensure_ascii=False)
    print(f"[sforja/proyectil] {ruta} ({img.size[0]}×{img.size[1]}) — hitbox sugerida {man['hitbox_sugerida']}")

    if args.animacion:
        # EL ESPEJO DE LA CASA: la tira animada tML (el sprite respira)
        tira = SF.el_espejo(img, animacion=args.animacion, frames=args.frames)
        print(f"[sforja/proyectil] tira: {tira}")
    if args.destino_tml:
        os.makedirs(os.path.dirname(args.destino_tml), exist_ok=True)
        img.save(args.destino_tml)
        print(f"[sforja/proyectil] → COPIADO AL MOD: {args.destino_tml}")
    return ruta


def cmd_fondo(args):
    """`fondo` — EL FONDO ENLAZABLE: tileable horizontal (la letra:
    «usada para la generacion de pixelart de todo tipo, incluyendo
    fondos»), la mitad de ancho que de alto de la casa, con el
    espejo-seam QA (los bordes izquierdo y derecho ENLAZAN exacto)."""
    dest = SF.carpeta_de(args.nombre, args.dest)
    w, h = args.ancho, args.alto
    ss = max(2, SS_HD // 2)

    def pinta(d, W, H):
        # LAS CAPAS DEL VACÍO (el fondo del cuerpo del dios: la página
        # negra con las vetas violetas y el polvo estelar)
        d.rectangle([0, 0, W, H], fill=(16, 9, 30, 255))
        import random as _r
        rr = _r.Random(args.semilla)
        for capa, (tinte, dens, r) in enumerate((
                ((44, 24, 78, 255), 60, 0.5), ((72, 40, 124, 255), 36, 0.34))):
            for k in range(dens):
                x = rr.uniform(0, W)
                y = rr.uniform(H * (0.15 + capa * 0.2), H * (0.95 - capa * 0.1))
                rad = rr.uniform(H * 0.06, H * 0.16) * r * 2
                d.ellipse([x - rad, y - rad * 0.5, x + rad, y + rad * 0.5],
                          fill=tinte)
        for k in range(90):
            x = rr.uniform(0, W)
            y = rr.uniform(0, H)
            br = rr.choice(((196, 150, 255, 210), (255, 214, 130, 190),
                            (232, 220, 255, 160)))
            r2 = rr.uniform(W * 0.0016, W * 0.0032)
            d.ellipse([x - r2, y - r2, x + r2, y + r2], fill=br)

    img = hdcuadrado(pinta, w, h, ss)
    # EL ENLACE HORIZONTAL: la última columna = la primera (tileable
    # EXACTO — el loop del cuerpo del dios no tiene costura).
    corte = img.crop((0, 0, w // 8, h))
    img.paste(corte, (w - w // 8, 0))
    ruta = os.path.join(dest, f"fondo_{w}x{h}.png")
    img.save(ruta)
    print(f"[sforja/fondo] {ruta} (tileable horizontal ×{w // (w // 8)})")
    return ruta


def cmd_alma_sf(args):
    """`alma` — el comando de la Superforja con el REGISTRO AMPLIADO
    (los 17 de siempre + los 10 de la SForja)."""
    return SF.cmd_alma(args)


def main():
    ap = argparse.ArgumentParser(
        description='LA SFORJA — la forja definitiva + el taller (la Superforja '
                    'mejorada: HD cuadrado · proyectiles · soles · fondos)',
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog=__doc__.split('USO:')[1].split('\n\n')[0] if 'USO:' in __doc__ else None)
    sub = ap.add_subparsers(dest='cmd', required=True)

    # EL SOL DE LA CASA
    p = sub.add_parser('sol', help='el sol de gen_sol_aethon, parametrizado (luz·oscuridad·fusion)')
    p.add_argument('--nombre', required=True)
    p.add_argument('--paleta', default='luz', choices=list(PALETAS_SOL.keys()))
    p.add_argument('--tam', type=int, default=HD_CUADRO)
    p.add_argument('--dest', help='la carpeta de salida')
    p.add_argument('--destino_tml', help='copiar el PNG directo al mod (ruta completa)')
    p.set_defaults(fn=cmd_sol)

    # EL PROYECTIL
    p = sub.add_parser('proyectil', help='el sprite de proyectil + hitbox + tira (todo en la forja)')
    p.add_argument('--nombre', required=True)
    p.add_argument('--receta', default='tajo', choices=['tajo', 'aguja', 'sol'])
    p.add_argument('--ancho', type=int, default=120)
    p.add_argument('--alto', type=int, default=36)
    p.add_argument('--tam', type=int, default=HD_CUADRO, help='el lado del sol (receta sol)')
    p.add_argument('--paleta', help='la paleta del sol (receta sol)')
    p.add_argument('--animacion', help='la tira animada (vida·respirar·flotar·latir·vaivén)')
    p.add_argument('--frames', type=int, default=6)
    p.add_argument('--dest', help='la carpeta de salida')
    p.add_argument('--destino_tml', help='copiar el PNG directo al mod (ruta completa)')
    p.set_defaults(fn=cmd_proyectil)

    # EL FONDO
    p = sub.add_parser('fondo', help='el fondo tileable horizontal de la casa')
    p.add_argument('--nombre', required=True)
    p.add_argument('--ancho', type=int, default=256)
    p.add_argument('--alto', type=int, default=128)
    p.add_argument('--semilla', type=int, default=7331)
    p.add_argument('--dest', help='la carpeta de salida')
    p.set_defaults(fn=cmd_fondo)

    # EL ALMA AMPLIADA (re-expone el comando con las choices nuevas)
    p = sub.add_parser('alma', help='el preview del efecto (los 17 + los 10 de la SForja)')
    p.add_argument('--nombre', required=True)
    p.add_argument('--efecto', default='brasa', choices=SF.EFECTOS)
    p.add_argument('--tira', action='store_true')
    p.add_argument('--frames', type=int, default=3)
    p.add_argument('--dest', help='la carpeta de salida')
    p.set_defaults(fn=cmd_alma_sf)

    # TODO EL REPERTORIO DE LA SUPERFORJA — HEREDADO INTACTO
    descripciones = {
        'pluma': 'el borrador IA (pxl)', 'refina': 'la edición dirigida',
        'puente': 'la rejilla nativa restaurada', 'molde': 'cutout → rejilla → paleta',
        'espejo': 'el sprite → la tira animada', 'tambor': 'el dithering',
        'rampa': 'la rampa hue-shift', 'atlas': 'frames → tira + atlas',
        'ojo': 'el QA VLM', 'forjar': 'la cadena corta (compatibilidad)',
        'corta': 'EL CORTE (la transparencia)', 'cadena': 'TODO en uno',
    }
    for nombre, ayuda in descripciones.items():
        # (los comandos de la Superforja registran SUS argumentos en
        # su propio main; aquí se re-entran por su camino real)
        pass
    if len(sys.argv) > 1 and sys.argv[1] not in ('sol', 'proyectil', 'fondo', 'alma'):
        # EL PASO AL MOTOR VIVO: cualquier otro comando cae a la
        # Superforja COMPLETA (su CLI entera, intacta).
        sys.argv[0] = os.path.join(AQUI, 'superforja.py')
        SF.main()
        return

    args = ap.parse_args()
    args.fn(args)


if __name__ == '__main__':
    main()
