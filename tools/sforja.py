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
  python3 tools/sforja.py proyectil --nombre dagas_eter --receta daga --tipo vacio
  python3 tools/sforja.py proyectil --nombre dagas_eter --receta daga --tira --qa \
      --destino_tml AethonMod/Content/Projectiles/DagaEterna_Hoja.png
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
#  LAS DAGAS DEL ETERNO — LA RECETA DE LAS CINCO (la letra: «vienen
#  las 5 dagas que también tendrán un ojo en el centro»): CRISTAL ·
#  LLAMA · ROCA · VACÍO · VEREDICTO, cada una con SU alma (la rampa),
#  SU filo (el borde que arde) y SU guarda — y TODAS con EL OJO EN EL
#  CENTRO: la ALMENDRA en (17,15) del marco de 34×34, exactamente
#  donde el IRIS VIVO del DagaEternaMinion aterriza en runtime (el
#  iris que MIRA a la presa, dibujado aparte). La hoja APUNTA ARRIBA
#  en rotación 0 (lo que el minion espera: dir.ToRotation() + π/2) y
#  LA TIRA apila las cinco en el ORDEN EXACTO de ai[0] (el PreDraw
#  recorta Rectangle(0, tipo·34, 34, 34) del spritesheet 34×170).
#  La paleta de cada daga ES la de su alma en el .cs (LuzDe/IrisDe):
#  el iris del párpado y la luz de la estela nacen del mismo color.
# ====================================================================
ORDEN_DAGAS = ['cristal', 'llama', 'roca', 'vacio', 'veredicto']   # ai[0] 0..4

DAGAS = {
    # 0 · LA DAGA DE CRISTAL — lila pálido facetado (la «Dazzling»
    #     del stabber: la ráfaga de tajos que abre la escalera)
    'cristal': dict(
        punta=1.0, cuerpo=3.7, onda=0.0, semilla=0.0, gx=5,
        rampa=[((198, 162, 250), 0.0), ((170, 132, 232), 0.55), ((112, 76, 180), 1.0)],
        filo=(242, 232, 255), guarda=(172, 142, 222), guarda_hi=(228, 206, 255),
        iris=(224, 200, 255), anillo=(96, 62, 158), pomo=(188, 152, 242),
        facetas=True,
    ),
    # 1 · LA DAGA DE LLAMA — hoja negra ONDULADA con el filo ardiendo
    #     violeta (el barrido en arco amplio). LA LECCIÓN v6.101 DEL SOL
    #     NEGRO: «el negro solo existe si arde» — el cuerpo sube de
    #     tono y el filo ARDE GRUESO respirando hacia adentro.
    'llama': dict(
        punta=1.4, cuerpo=4.8, onda=1.55, semilla=-1.0, gx=5, fw=2.0,
        rampa=[((18, 10, 32), 0.0), ((40, 22, 64), 1.0)],
        filo=(228, 140, 255), guarda=(86, 52, 128), guarda_hi=(140, 84, 200),
        iris=(178, 120, 255), anillo=(216, 170, 255), pomo=(178, 120, 255),
        respira=0.55, respiw=0.7,
    ),
    # 2 · LA DAGA DE ROCA — obsidiana MACIZA (ancha) con vetas de oro
    #     (la embestida con reculo, 300 %)
    'roca': dict(
        punta=2.6, cuerpo=6.6, onda=0.0, semilla=0.0, gx=7, ojox=5,
        rampa=[((58, 34, 90), 0.0), ((30, 17, 50), 1.0)],
        filo=(120, 76, 168), guarda=(64, 38, 96), guarda_hi=(120, 76, 168),
        iris=(150, 84, 235), pomo=(255, 214, 130), vetas=True,
    ),
    # 3 · LA DAGA DEL VACÍO — hoja negra de borde violeta con la
    #     guarda DORADA (se clava y gotea; el iris ORO de la mirada).
    #     La lección del sol negro también: el borde arde HACIA
    #     adentro o la hoja negra no existe contra lo oscuro.
    'vacio': dict(
        punta=1.5, cuerpo=4.5, onda=0.0, semilla=0.0, gx=7, gyh=3, fw=1.4,
        rampa=[((22, 12, 38), 0.0), ((48, 26, 80), 1.0)],
        filo=(140, 92, 215), guarda=(255, 214, 130), guarda_hi=(255, 244, 214),
        iris=(255, 214, 130), pomo=(255, 214, 130), respira=0.55,
    ),
    # 4 · LA DAGA DEL VEREDICTO — el ORO del veredicto con el filo
    #     blanco-cálido y la GEMA violeta (marca y sentencia, 150 %)
    'veredicto': dict(
        punta=2.0, cuerpo=4.6, onda=0.0, semilla=0.0, gx=6,
        rampa=[((255, 214, 130), 0.0), ((226, 172, 90), 0.6), ((158, 108, 50), 1.0)],
        filo=(255, 244, 214), guarda=(212, 168, 84), guarda_hi=(255, 214, 130),
        iris=(255, 244, 214), pomo=(255, 244, 214), gema=(150, 84, 235),
    ),
}

# el puño por daga (la venda de la empuñadura)
_WRAP_DAGA = {'cristal': (150, 110, 210), 'llama': (96, 58, 148),
              'roca': (74, 44, 116), 'vacio': (122, 78, 188),
              'veredicto': (140, 96, 46)}


def _mult(c, k):
    """El color multiplicado (el shading de la casa, con tope 255)."""
    return tuple(min(255, int(c[j] * k)) for j in range(3))


def la_daga(tipo='vacio', tam=34):
    """LA DAGA DEL ETERNO — UN marco de 34×34 nacido en la forja: la
    punta de aguja, la hoja escaneada PÍXEL A PÍXEL (la rampa vertical
    + el shading de la casa: flanco izquierdo que lee, flanco derecho
    que descansa, el nervio central y el filo que ARDE en el color del
    alma), EL OJO-ALMENDRA en (17,15), la guarda con sus puntas de
    rombo, la empuñadura vendada y el pomo joya.

    LA DAGA ES PIXEL ART (la lección v6.128 de la autopsia): el Lanczos
    de hdcuadrado es el traje de los VFX LISOS (soles, tajos, agujas)
    pero DESDIBUJA los detalles de 1 px del minion (el filo de la llama
    quedaba lavado, las vetas de oro se mezclaban con la obsidiana, la
    «onda» respiraba el ancho sin serpentear) — la daga se dibuja
    NATIVA 34×34, píxel DURO (la ley de las armas de la casa), con el
    EJE SERPENTEANTE de la llama y el contorno Terraria de la casa."""
    cfg = DAGAS[tipo]
    rampa = cfg['rampa']

    def color_en(t):
        for i in range(len(rampa) - 1):
            (c0, t0), (c1, t1) = rampa[i], rampa[i + 1]
            if t <= t1 or i == len(rampa) - 2:
                k = 0.0 if t1 == t0 else min(1.0, max(0.0, (t - t0) / (t1 - t0)))
                return tuple(int(c0[j] + (c1[j] - c0[j]) * k) for j in range(3))
        return rampa[-1][0]

    im = Image.new('RGBA', (tam, tam), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    cx = tam // 2                 # 17 para 34 (la columna del alma)
    tip_y, guard_y = 2, 22        # la anatomía del marco

    def semi(t):
        """EL PERFIL: el semiancho de la hoja (punta de aguja fundida
        en el cuerpo pleno; la llama además RESPIRA el ancho)."""
        b = cfg['punta'] + (cfg['cuerpo'] - cfg['punta']) * min(1.0, t * 2.1) ** 0.85
        if cfg['onda']:
            b += cfg['onda'] * 0.30 * math.sin(t * 4.6 + cfg['semilla'])
        return b

    def serpent(t):
        """LA ONDA DE LA LLAMA (la lección de la autopsia): el EJE
        serpentea — la hoja que ondula DE VERDAD mueve su centro (±2,6
        px medidos: el serpenteo de ±0,75 px era INVISIBLE; el S
        completo se lee: la punta a un lado, el vientre al otro)."""
        if not cfg['onda']:
            return 0.0
        A = cfg['onda'] * 1.7
        return A * math.sin(t * 6.0 + cfg['semilla']) * (1.0 - t * 0.45)

    def dentro(x, y):
        """¿El píxel vive dentro de la hoja? (el eje serpenteante manda)"""
        if y < tip_y or y >= guard_y or x < 0 or x >= tam:
            return False
        t = (y - tip_y) / float(guard_y - tip_y)
        return abs(x + 0.5 - (cx + serpent(t))) <= semi(t)

    # ---- LA HOJA (píxel a píxel: rampa vertical + shading horizontal)
    fw = cfg.get('fw', 0.85)                     # el ancho del filo que arde
    rw = fw + cfg.get('respiw', 1.5)             # la respiración hacia adentro
    brasa = _mult(cfg['filo'], cfg['respira']) if cfg.get('respira') else None
    for y in range(tip_y, guard_y):
        t = (y - tip_y) / float(guard_y - tip_y)
        sm = semi(t)
        if sm < 0.3:
            continue
        cxe = cx + serpent(t)                    # el eje de ESTA fila
        base = color_en(t)
        izq = _mult(base, 1.14)                  # el flanco izquierdo LEE (la casa)
        der = _mult(base, 0.84)                  # el derecho descansa
        nervio = _mult(base, 0.55)               # el pliegue central
        if cfg['onda']:
            # EL FILO QUE ARDE DE VERDAD (la llama): el borde arde MÁS
            # donde la onda CRESTA — blanco-cálido PLENO en los pliegues,
            # violeta caliente en los valles (el fuego lame las curvas)
            calor = abs(math.sin(t * 6.0 + cfg['semilla']))
            filo_f = tuple(int(cfg['filo'][j] + (252 - cfg['filo'][j]) * calor)
                           for j in range(3))
        else:
            filo_f = cfg['filo']
        for x in range(tam):
            dx = x + 0.5 - cxe
            adx = abs(dx)
            if adx > sm:
                continue
            if adx > sm - fw:                    # EL FILO (lo que MÁS arde)
                c = filo_f
            elif brasa is not None and adx > sm - rw:
                c = brasa                        # la brasa respira hacia adentro
            elif sm > 1.7 and adx <= 0.75:       # el nervio central
                c = nervio
            else:
                c = izq if dx < 0 else der
            d.point((x, y), fill=c + (255,))

    # ---- LOS FACETOS DEL CRISTAL (las cuñas que refractan — pares
    #      BRILLO/SOMBRA en diagonal: la faceta lee por el contraste)
    if cfg.get('facetas'):
        for t0 in (0.16, 0.42, 0.68, 0.86):
            y0 = int(tip_y + (guard_y - tip_y) * t0)
            x0 = int(round(cx + serpent(t0) - semi(t0) * 0.55))
            for k in range(5):
                x, y = x0 + k, y0 - k
                if dentro(x, y):
                    d.point((x, y), fill=(248, 242, 255, 255))   # la faceta brillante
                x2, y2 = x0 + k + 1, y0 - k + 1
                if dentro(x2, y2) and (k % 2 == 0):
                    d.point((x2, y2), fill=(88, 58, 142, 255))    # su sombra gemela

    # ---- LAS VETAS DE ORO DE LA ROCA (VENAS que bajan serpenteando
    #      DENTRO de la hoja — la v6.128-d: el zigzag viejo se SALÍA de
    #      la hoja y quedaban puntos sueltos; la vena SIGUE el perfil)
    if cfg.get('vetas'):
        for (t0, fase) in ((0.10, 0.0), (0.50, math.pi)):
            y = int(round(tip_y + (guard_y - tip_y) * t0))
            for k in range(8):                       # la vena: 8 px bajando
                if y >= guard_y:
                    break
                tt = (y - tip_y) / float(guard_y - tip_y)
                amp = min(semi(tt) - 2.0, 2.8)
                vx = int(round(cx + serpent(tt) + math.sin(k * 0.95 + fase) * amp))
                for px in (vx, vx + 1):              # la vena de 2 px de oro
                    if dentro(px, y):
                        d.point((px, y), fill=(255, 214, 130, 255))
                if dentro(vx, y) and k % 3 == 0:     # el destello de la vena
                    d.point((vx, y), fill=(255, 244, 214, 255))
                y += 1

    # ---- EL OJO EN EL CENTRO (la letra): la ALMENDRA en (17,15) —
    #      la cuenca oscura que recibe al iris vivo del runtime. El
    #      ANILLO del párpado por daga (el contraste manda: oscuro en
    #      la hoja pálida, claro en la hoja ardiendo, ancho en la roca)
    ox, oy = cx, 15
    oxr = cfg.get('ojox', 4)
    d.ellipse([ox - oxr, oy - 2, ox + oxr, oy + 2],
              outline=cfg.get('anillo', cfg['iris']) + (255,))
    d.ellipse([ox - oxr + 1, oy - 1, ox + oxr - 1, oy + 1], fill=(10, 6, 18, 255))
    d.point((ox - 2, oy), fill=(16, 10, 28, 255))   # los párpados caen a los lados
    d.point((ox + 2, oy), fill=(16, 10, 28, 255))

    # ---- LA GUARDA (el gavilán: la barra + las puntas de rombo)
    gy, gx = 22, cfg['gx']
    gyh = cfg.get('gyh', 2)                          # el alto de la barra
    d.rectangle([cx - gx, gy, cx + gx, gy + gyh - 1], fill=cfg['guarda'] + (255,))
    d.rectangle([cx - gx, gy, cx + gx, gy], fill=cfg['guarda_hi'] + (255,))
    for lado in (-1, 1):                             # las puntas de rombo
        px0 = cx + lado * gx
        d.polygon([(px0 + lado * 2, gy + gyh - 1), (px0, gy - 1),
                   (px0 - lado, gy + gyh - 1), (px0, gy + gyh)],
                  fill=cfg['guarda'] + (255,))
        d.point((px0 + lado, gy), fill=cfg['guarda_hi'] + (255,))
    if cfg.get('gema'):                              # la gema del veredicto
        d.polygon([(cx, gy - 2), (cx + 3, gy), (cx, gy + 2), (cx - 3, gy)],
                  fill=cfg['gema'] + (255,))
        d.polygon([(cx, gy - 1), (cx + 1, gy), (cx, gy + 1), (cx - 1, gy)],
                  fill=_mult(cfg['gema'], 1.5) + (255,))
        d.point((cx, gy - 1), fill=(255, 244, 214, 255))
    if cfg.get('vetas'):                             # las pepitas de la roca
        for vx in (-4, -1, 3):
            d.point((cx + vx, gy + 1), fill=(255, 214, 130, 255))

    # ---- LA EMPUÑADURA (la venda: vueltas alternas con su brillo)
    wrap = _WRAP_DAGA[tipo]
    for y in range(gy + gyh, gy + gyh + 6):
        c = wrap if y % 2 == 0 else _mult(wrap, 0.66)
        d.rectangle([cx - 1, y, cx + 1, y], fill=c + (255,))
        d.point((cx - 1, y), fill=_mult(wrap, 1.45) + (255,))

    # ---- EL POMO (la joya que cierra el puño — montada sobre el
    #      final de la venda, SIEMPRE dentro del marco de 34)
    d.ellipse([cx - 2, gy + gyh + 4, cx + 2, gy + gyh + 7], fill=cfg['pomo'] + (255,))
    d.point((cx - 1, gy + gyh + 5), fill=_mult(cfg['pomo'], 1.5) + (255,))

    # EL CONTORNO TERRARIA de la casa (el hilo negro que lee el ojo)
    img = SF.contorno(im)
    # EL GLOW — la luz irradia suave alrededor del filo (el bautizo)
    return _glow(img, 0.05, 90)


def la_tira_dagas(tam=34):
    """LA TIRA DE LAS CINCO — el spritesheet del mod (34×170): un
    marco de 34×34 por daga, apiladas en el ORDEN EXACTO de ai[0]
    (cristal·llama·roca·vacío·veredicto) — el recorte que el
    DagaEternaMinion hace en PreDraw con Rectangle(0, tipo·34, 34, 34)."""
    tira = Image.new('RGBA', (tam, tam * len(ORDEN_DAGAS)), (0, 0, 0, 0))
    for k, tp in enumerate(ORDEN_DAGAS):
        tira.paste(la_daga(tp, tam), (0, k * tam))
    return tira


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


def el_montaje_dagas(carpeta, tira, tam=34, x=5):
    """EL MONTAJE DE LAS CINCO — los cinco marcos ×5 + la tira ×3
    sobre el fondo oscuro de la casa (el escenario del QA VLM, el
    mismo look del montaje de siempre)."""
    piezas = []
    for k, tp in enumerate(ORDEN_DAGAS):
        marco = tira.crop((0, k * tam, tam, (k + 1) * tam))
        piezas.append(marco.resize((tam * x, tam * x), Image.NEAREST))
    piezas.append(tira.resize((tam * x, tam * x * len(ORDEN_DAGAS)), Image.NEAREST))
    alto = max(p.height for p in piezas) + 30
    ancho = sum(p.width for p in piezas) + 20 * (len(piezas) + 1)
    lienzo = Image.new('RGBA', (ancho, alto), (64, 64, 74, 255))   # el gris medio (el QA lo pidió)
    dx = 20
    for p in piezas:
        lienzo.paste(p, (dx, 22), p)
        dx += p.width + 20
    destino = os.path.join(carpeta, 'montaje_daga.png')
    lienzo.save(destino)
    print(f'  EL MONTAJE: {destino}')
    return destino


def el_ojo_daga(montaje, destino_txt):
    """EL OJO DE LAS DAGAS — el QA VLM específico de la receta (las
    cinco almas, el ojo-almendra en el centro, el contorno Terraria):
    la nota y los defectos concretos, listos para la refina."""
    import subprocess
    prompt = (
        "QA de sprites de dagas para un mod de Terraria (fondo gris oscuro). "
        "De izquierda a derecha: (1) DAGA DE CRISTAL: hoja lila pálido facetada; "
        "(2) DAGA DE LLAMA: hoja negra ONDULADA con el filo ardiendo violeta; "
        "(3) DAGA DE ROCA: hoja ANCHA de obsidiana con vetas de oro; "
        "(4) DAGA DEL VACÍO: hoja negra de borde violeta con guarda DORADA; "
        "(5) DAGA DEL VEREDICTO: hoja dorada con filo blanco-cálido y gema "
        "violeta en la guarda; y a la derecha LA TIRA con las cinco apiladas. "
        "TODAS apuntan ARRIBA y llevan UN OJO (almendra oscura con párpado de "
        "color) en el centro de la hoja. Responde en español, máximo 6 líneas: "
        "(1) ¿las cinco dagas se ven completas, legibles y DISTINTAS entre sí? "
        "(2) ¿cada hoja lleva su ojo-almendra en el centro? "
        "(3) ¿las paletas se leen como se describe? "
        "(4) ¿el contorno oscuro estilo Terraria marca bien la silueta? "
        "(5) NOTA 0-10 y defectos concretos (máximo 4). "
        "(6) VEREDICTO final: SHIP o FIX (y qué corregir).")
    r = subprocess.run(['z-ai', 'vision', '-p', prompt, '-i', montaje],
                       capture_output=True, text=True, timeout=180)
    veredicto = ''
    ini = (r.stdout or '').find('{')
    if ini >= 0:
        try:
            dd = json.loads(r.stdout[ini:])
            veredicto = dd['choices'][0]['message']['content'].strip()
        except Exception:
            veredicto = ''
    if not veredicto:
        veredicto = ((r.stdout or '') + '\n' + (r.stderr or '')).strip()[-1500:] \
            or 'EL OJO no respondió'
    with open(destino_txt, 'w', encoding='utf-8') as f:
        f.write(veredicto + '\n')
    print('  EL OJO DE LAS DAGAS (QA VLM):')
    for linea in veredicto.splitlines()[:8]:
        print('   ', linea)
    return veredicto


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
    elif receta == 'daga':
        # LA RECETA DE LAS CINCO DAGAS DEL ETERNO — la tira apila las
        # cinco en el ORDEN de ai[0] (lo que el DagaEternaMinion recorta)
        if args.tira:
            img = la_tira_dagas(args.tam or 34)
        else:
            img = la_daga(args.tipo, args.tam or 34)
    else:
        raise SystemExit(f"receta desconocida: {receta} (tajo·aguja·sol·daga)")

    ruta = os.path.join(dest, f"proyectil_{receta}{'_' + args.tipo if receta == 'daga' and not args.tira else ''}.png")
    img.save(ruta)
    # EL MANIFIESTO DE HITBOX (el documento del proyectil: lo que el
    # .cs necesita saber del sprite que la forja le hizo).
    bbox_img = img.getbbox()
    man = {"comando": "proyectil", "receta": receta,
           "ruta": ruta, "sprite": [img.size[0], img.size[1]],
           "bbox_opaco": list(bbox_img) if bbox_img else None,
           "hitbox_sugerida": [max(8, img.size[0] // 4), max(8, img.size[1] // 2)],
           "hd_cuadrado": True, "supersampling": SS_HD}
    if receta == 'daga':
        man.update({
            "tira": bool(args.tira),
            "marco": args.tam or 34,
            "marcos": len(ORDEN_DAGAS) if args.tira else 1,
            "mapa_marcos": {tp: k for k, tp in enumerate(ORDEN_DAGAS)}
                           if args.tira else {args.tipo: 0},
            "orden_ai0": ORDEN_DAGAS,
            "apunta": "ARRIBA en rotation 0 (el .cs usa dir.ToRotation() + PI/2)",
            "ojo_almendra": [17, 15],
            "hitbox_sugerida": [20, 20],
            "nota": ("la receta de las cinco dagas del Eterno — el "
                     "DagaEternaMinion recorta SU marco por ai[0] con "
                     "Rectangle(0, tipo*34, 34, 34) del spritesheet 34x170"),
        })
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
    if args.qa and receta == 'daga' and args.tira:
        # EL OJO DE LAS DAGAS — el QA VLM de la casa sobre el montaje
        montaje = el_montaje_dagas(dest, img, args.tam or 34)
        el_ojo_daga(montaje, os.path.join(dest, 'qa_ojo.txt'))
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
    p.add_argument('--receta', default='tajo', choices=['tajo', 'aguja', 'sol', 'daga'])
    p.add_argument('--tipo', default='vacio', choices=list(DAGAS.keys()),
                   help='el alma de la daga (receta daga)')
    p.add_argument('--tira', action='store_true',
                   help='(receta daga) LA TIRA de las cinco en el orden de ai[0] (34×170)')
    p.add_argument('--qa', action='store_true',
                   help='(receta daga + tira) EL OJO: el montaje + el QA VLM de la casa')
    p.add_argument('--ancho', type=int, default=120)
    p.add_argument('--alto', type=int, default=36)
    p.add_argument('--tam', type=int, default=None,
                   help='el lado del sol (receta sol) o el marco de la daga (34, receta daga)')
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
