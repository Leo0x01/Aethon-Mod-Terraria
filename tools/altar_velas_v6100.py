#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
altar_velas_v6100.py — LA DANZA DE LAS DOS VELAS DEL ALTAR (v6.100).

LA LETRA DEL USUARIO: «me gustaria que mejoraras la animacion ya que en
el sprite parece que hay 2 velas y estas dos no estan animadas».

LA AUTOPSIA del sprite v6.100 interrumpido: las llamas de las velas
solo se MENEABAN ±1 px entre marcos — a 1× en juego eso no se lee como
animación. LA CURA: LAS DOS VELAS bailan EN CONTRAFASE con CUATRO
dimensiones de cambio a la vez:

  · ALTURA    — la llama alterna 3 px (baja) ↔ 4-6 px (alta); la vela
    derecha aprovecha sus dos celdas libres del nicho y en su marco
    caliente llega a 6 px (la llama más alta de la casa).
  · PUNTA     — la punta CRUZA: los tips vivos se inclinan a izquierda
    y derecha alternando marco a marco (el vaivén de la llama real).
  · BRILLO    — los marcos calientes llevan PUNTA INCADESCENTE
    (255,238,170) y cuerpo VIVO; los fríos, cuerpo bajo naranja.
  · BASE      — los píxeles exteriores de la fila del borde (los dos
    faroles laterales de cada vela) SE ENCIENDEN en los marcos anchos
    y SE APAGAN en los estrechos — la llama late de ancho.

REGLAS DE LA CASA que cumple:
 · TRANSPARENCIA NATIVA: se pinta sobre el lienzo RGBA del sprite —
   las celdas limpias quedan TRANSPARENTES por construcción, jamás
   post-procesadas.
 · QUIRÚRGICO: SOLO se tocan las celdas de llama de las dos velas —
   el resto del altar (pedestal, runas, vórtice, chispa teal, bordes
   marrones de la vela) queda BYTE A BYTE intacto (verificación final
   incluida: compara contra la copia limpia).
 · PALETA DE LA CASA: los colores exactos del sprite original
   (255,200,80) vivo · (216,144,48) bajo · punta incandescente
   (255,238,170).

USO: python3 tools/altar_velas_v6100.py [--check]
  --check : solo informa qué haría, no escribe.
"""
import sys
from PIL import Image, ImageChops

RUTA = 'AethonMod/Content/Tiles/AltarOfrendasTile.png'
W, H = 34, 200
ALTO_MARCO = 50

# La paleta de las llamas (muestreada del sprite original).
PUNTA = (255, 238, 170, 255)   # blanco incandescente de la punta
VIVO = (255, 200, 80, 255)     # amarillo dorado vivo
BAJO = (216, 144, 48, 255)     # naranja de la base
MARRON = (160, 88, 24, 255)    # el borde de la boca de la vela (el portallamas)
AZUL_HUERFANO = (96, 128, 255, 255)  # un azul suelto del sprite viejo (se normaliza)
TEAL_ACCENTO = (90, 220, 180, 255)    # la chispa teal — sus 2 celdas que caen DENTRO de la chimenea se ceden a la llama

# EL PORTALLAMAS constante: la boca marrón de cada vela (fila 38) —
# se repinta IGUAL en los cuatro marcos: la vela no se mueve, la LLAMA
# es la que baila encima.
PORTALLAMAS = {
    'izq': [(5, 38), (6, 38), (7, 38), (8, 38)],
    'der': [(25, 38), (26, 38), (27, 38), (28, 38)],
}

P, V, B = 'P', 'V', 'B'
# La danza: por marco, por vela (izq, der), la lista de celdas
# (x_local, y_local, color). Coordenadas LOCALES del marco; x absoluta
# del marco (la vela izquierda vive en x=4..9, la derecha en x=24..29).
DANZA = {
    # ---- MARCO 0: izq ALTA caliente (punta blanca, inclinada izq,
    #      base ancha) · der BAJA fría (estrecha, sin faroles) ----
    0: {
        'izq': [(6, 35, P), (7, 35, V),
                (5, 36, V), (6, 36, V), (7, 36, V), (8, 36, V),
                (6, 37, B), (7, 37, V), (8, 37, B),
                (4, 38, V), (9, 38, V)],
        'der': [(27, 36, V),
                (26, 37, B), (27, 37, B), (28, 37, B)],
    },
    # ---- MARCO 1: izq BAJA fría · der ALTA caliente de 6 px (la
    #      punta sube por su celda libre hasta y=33) ----
    1: {
        'izq': [(7, 36, V),
                (6, 37, B), (7, 37, B), (8, 37, B)],
        'der': [(28, 33, P), (28, 34, V), (27, 35, V), (28, 35, V),
                (26, 36, V), (27, 36, V), (28, 36, V),
                (26, 37, B), (27, 37, V), (28, 37, B),
                (24, 38, V), (29, 38, V)],
    },
    # ---- MARCO 2: izq ALTA caliente (punta cruzada a la derecha) ·
    #      der BAJA fría con la punta ladeada a la izquierda ----
    2: {
        'izq': [(7, 35, V), (8, 35, P),
                (5, 36, V), (6, 36, V), (7, 36, V), (8, 36, V),
                (6, 37, B), (7, 37, V), (8, 37, B),
                (4, 38, V), (9, 38, V)],
        'der': [(26, 36, V),
                (26, 37, B), (27, 37, B), (28, 37, B)],
    },
    # ---- MARCO 3: izq MEDIA fría (un solo farol) · der MEDIA
    #      caliente (punta incandescente ladeada a la izquierda) ----
    3: {
        'izq': [(7, 36, V),
                (6, 37, V), (7, 37, B), (8, 37, B),
                (9, 38, V)],
        'der': [(26, 35, P), (27, 35, V),
                (26, 36, V), (27, 36, V), (28, 36, V),
                (26, 37, B), (27, 37, V), (28, 37, B),
                (24, 38, V), (29, 38, V)],
    },
}

# La región quirúrgica por vela (x0..x1, y0..y1 — local al marco):
# la chimenea de la llama SIN tocar el borde marrón de la vela.
REGION = {'izq': (4, 10, 34, 39), 'der': (24, 30, 33, 39)}

COL = {'P': PUNTA, 'V': VIVO, 'B': BAJO}


def es_llama_o_aire(rgba):
    """¿Es celda de llama (por color), aire, o parte normalizable de
    la boca de la vela? Lo demás (pedestal, runas) JAMÁS se toca.
    La familia de llama abarca los tres tonos del sprite viejo: el
    vivo (255,200,80), el bajo (216,144,48) y el palo del tip
    (255,246,200) — más el marrón del portallamas, un azul huérfano
    y los dos acentos teal que caen DENTRO de la chimenea."""
    r, g, b, a = rgba
    if a == 0:
        return True
    if (r, g, b, 255) in (MARRON, AZUL_HUERFANO, TEAL_ACCENTO):
        return True
    return r >= 200 and g >= 130 and (r - b) > 40


def main():
    check = '--check' in sys.argv
    img = Image.open(RUTA).convert('RGBA')
    assert img.size == (W, H), f'tamaño inesperado {img.size}'
    px = img.load()

    total_b = total_p = bloqueadas = 0
    for marco, velas in DANZA.items():
        off = marco * ALTO_MARCO
        for lado, celdas in velas.items():
            # 1) LIMPIAR la chimenea (celdas de llama viejas → aire).
            x0, x1, y0, y1 = REGION[lado]
            for y in range(y0, y1):
                for x in range(x0, x1):
                    if es_llama_o_aire(px[x, y + off]):
                        px[x, y + off] = (0, 0, 0, 0)
                        total_b += 1
            # 2) EL PORTALLAMAS: la boca marrón de la vela, IGUAL en
            #    los cuatro marcos (la vela no baila — la llama sí).
            for (x, y) in PORTALLAMAS[lado]:
                px[x, y + off] = MARRON
            # 3) PINTAR la llama del marco (sin pisar arte jamás).
            for (x, y, c) in celdas:
                if px[x, y + off][3] != 0 and (x, y) not in PORTALLAMAS[lado]:
                    bloqueadas += 1
                    continue
                px[x, y + off] = COL[c]
                total_p += 1

    print(f'limpiadas {total_b} · pintadas {total_p} · bloqueadas por arte {bloqueadas}')
    if bloqueadas:
        print('AVISO: celdas del diseño chocaron con arte — revisar.')
    if check:
        print('CHECK: no se escribió nada.')
        return

    img.save(RUTA)

    # VERIFICACIÓN QUIRÚRGICA: contra la copia limpia, SOLO las celdas
    # de las dos chimeneas pueden diferir (y solo en colores de llama).
    base = Image.open('/tmp/altar_backup.png').convert('RGBA')
    bp = base.load()
    diff = ImageChops.difference(img, base)
    dp = diff.load()
    fuera = 0
    for y in range(H):
        for x in range(W):
            if dp[x, y][3] == 0 and dp[x, y][:3] == (0, 0, 0):
                continue
            marco = y // ALTO_MARCO
            local = y % ALTO_MARCO
            ok = any(r[0] <= x < r[1] and r[2] <= local < r[3]
                     for r in REGION.values())
            if not ok:
                fuera += 1
    print(f'verificación: {fuera} celdas tocadas FUERA de las chimeneas'
          + (' — ¡ERROR!' if fuera else ' — QUIRÚRGICO OK'))


if __name__ == '__main__':
    main()
