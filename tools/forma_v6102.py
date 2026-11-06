#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
forma_v6102.py — v6.102 — LA FORMA ASCENDIDA 5: EL VIENTO DEL ESPÍRITU.

LA LETRA DEL USUARIO: «crear una nueva La Forma Ascendida, recuerda
que esta forma ascendida, a esta nueva forma debes darle aura, ya
sabes como crear aura y a esa aura añade una capa de turbulencia
suave que solo afecte al aura».

LA QUINTA LUZ — y su SELLO (r3, EL OJO sobre la FAMILIA: «no hay
figura humana; son puros símbolos — forma circular radiante con
núcleo luminoso central y anillo perimetral, estilo sol/sello
sagrado»): EL SELLO DEL VIENTO — el núcleo de plata del aliento, el
ANILLO PERIMETRAL que en esta luz NO es un aro cerrado sino EL
VIENTO: tres abrazos de turbulencia con huecos y colas que se
disuelven (LA FIRMA de la quinta luz — la misma capa que vive en el
aura) y LOS RAYOS DEL ALIENTO (las ráfagas cortas del exterior, como
las de las dos primeras luces).

LA PALETA: plata blanca (el núcleo — el aliento), teal → violeta
(EL VIENTO: el cuerpo blanco-plateado al centro, el teal pálido al
medio y el violeta al borde — igual que el perfil de AuraLib).

Salida: Content/Items/Cosmetics/FormaAscendidaCincoItem.png (30×30,
transparencia NATIVA — el lienzo nace alfa=0, la regla de la casa).
"""
import math
import os

from PIL import Image

AQUI = os.path.dirname(os.path.abspath(__file__))
BASE = os.path.join(AQUI, '..', 'AethonMod')

# LA PALETA DE LA QUINTA LUZ
PLATA = (235, 240, 255)          # el núcleo
PLATA_ALTA = (248, 246, 255)     # el corazón del núcleo
TEAL = (90, 220, 180)            # EL VIENTO — el medio del aura
TEAL_CLARO = (150, 240, 210)     # la cresta del viento
VIOLETA = (150, 84, 235)         # EL BORDE — el filo del aura
VIOLETA_ALTO = (200, 150, 255)   # la cresta violeta
BLANCO = (248, 244, 255)         # el destello


def px(im, x, y, c, a=255):
    if 0 <= int(x) < im.width and 0 <= int(y) < im.height:
        im.putpixel((int(x), int(y)), (c[0], c[1], c[2], a))


def nucleo(im):
    """EL NÚCLEO DEL ALIENTO — r4 (EL OJO: «1 píxel más de densidad
    para que rivalice con la Forma 4»): el corazón de plata r≈5.2 con
    su centro blanco de r≈2.6 — la calma exacta del centro del viento
    (el ojo del espíritu, lo único quieto del sello)."""
    cx, cy = 15.0, 15.0
    for y in range(30):
        for x in range(30):
            d = math.hypot(x - cx, y - cy)
            if d < 5.2:
                c = PLATA_ALTA if d < 2.6 else PLATA
                px(im, x, y, c)
    # LA CHISPA del centro (la perla del aliento)
    px(im, 15, 14, BLANCO)
    px(im, 16, 15, BLANCO)


def viento(im):
    """EL ANILLO PERIMETRAL — en la quinta luz el aro NO cierra: son
    TRES ABRAZOS de turbulencia (radios y horas distintas) con HUECOS
    al nacer y COLAS LARGAS que se disuelven subiendo — el abrazo
    mayor envuelve desde la izquierda por abajo y muere a la derecha;
    la contra-curva se leva por el costado; y el acento corto de la
    izquierda. teal en la cresta → violeta en el filo (la paleta del
    aura de la quinta luz: plata → teal → violeta, de dentro afuera)."""
    cx, cy = 15.0, 15.0
    abrazos = [
        # (radio, θ0, θ1, hueco al nacer) — θ en radianes (0 = derecha,
        # π/2 = abajo en imagen): EL ABRAZO MAYOR — de la izquierda
        # (168°) baja por delante (90°) y sube muriendo a la derecha
        # (-38°): media vuelta INCOMPLETA, la cola se disuelve
        (12.2, math.radians(168), math.radians(-38), 0.14),
        # LA CONTRA-CURVA — nace al costado (45°) y se leva muriendo
        # arriba (-70°): la segunda corriente que cruza la primera
        (10.8, math.radians(45), math.radians(-70), 0.10),
        # EL ACENTO CORTO de la izquierda — un gesto alto y breve
        (12.8, math.radians(205), math.radians(255), 0.18),
    ]
    for radio, a0, a1, hueco in abrazos:
        pasos = 46
        for s in range(pasos):
            t = s / (pasos - 1)
            if t < hueco:
                continue                     # EL HUECO al nacer (no es aro)
            th = a0 + (a1 - a0) * t
            x = cx + math.cos(th) * radio
            y = cy + math.sin(th) * radio * 0.95
            # LA COLA LARGA: nace fuerte (90%) y se disuelve hasta 25%
            alfa = 255 * (0.90 - 0.65 * t)
            if alfa < 45:
                continue
            # EL COLOR: teal en la cresta → violeta en el filo (de
            # dentro afuera la paleta del aura: plata→teal→violeta)
            c = TEAL if t < 0.42 else (TEAL_CLARO if t < 0.66 else
                                        (VIOLETA if t < 0.85 else VIOLETA_ALTO))
            px(im, x, y, c, min(255, int(alfa)))
            # EL GROSOR: el pincel de 2 px con su eco más tenue
            px(im, x, y + 1, c, min(255, int(alfa * 0.50)))


def rayos(im):
    """LOS RAYOS DEL ALIENTO — r5 (EL OJO: «reforzar los rayos»): el
    MISMO PINCEL de 2 px de los abrazos (trazo + eco) — antes eran
    líneas de 1 px que a 8× se perdían frente a los arcos gruesos."""
    for th_deg, r0, r1, c in (
            (160, 15.0, 19.0, TEAL),      # la ráfaga alta-izquierda
            (-18, 15.0, 19.5, VIOLETA),   # la ráfaga de la derecha
            (108, 14.5, 18.0, TEAL_CLARO),  # la ráfaga baja
            (55, 14.0, 16.5, VIOLETA_ALTO),  # la cuarta, corta
            (-42, 15.0, 17.5, TEAL)):     # la quinta, alta-derecha
        th = math.radians(th_deg)
        r = r0
        while r <= r1:
            x = 15 + math.cos(th) * r
            y = 15 + math.sin(th) * r * 0.95
            px(im, x, y, c)
            px(im, x, y + 1, c, 130)     # el eco del pincel (2 px)
            r += 0.5


def chispas(im):
    """LAS CHISPAS — el viento lleva motas (el aura respira). r5: una
    más arriba del todo (la lectura radiante de la familia)."""
    for x, y, c in ((5, 9, TEAL), (25, 8, TEAL), (4, 22, VIOLETA),
                    (26, 23, VIOLETA), (15, 28, TEAL_CLARO),
                    (2, 15, VIOLETA_ALTO), (28, 15, VIOLETA_ALTO),
                    (10, 3, TEAL_CLARO)):
        px(im, x, y, c)


def main():
    im = Image.new('RGBA', (30, 30), (0, 0, 0, 0))
    viento(im)      # EL ANILLO primero; el núcleo pisa encima
    nucleo(im)
    rayos(im)
    chispas(im)
    destino = os.path.join(BASE, 'Content', 'Items', 'Cosmetics',
                           'FormaAscendidaCincoItem.png')
    im.save(destino)
    print(f'FormaAscendidaCincoItem {im.size} -> {destino}')

    # LA HOJA DE CONTACTO para EL OJO (con la familia al lado)
    familia = Image.open(os.path.join(
        BASE, 'Content', 'Items', 'Cosmetics', 'FormaAscendidaCuatroItem.png')
    ).convert('RGBA').resize((30 * 8, 30 * 8), Image.NEAREST)
    grande = im.resize((30 * 8, 30 * 8), Image.NEAREST)
    hoja = Image.new('RGBA', (30 * 8 * 2 + 60, 30 * 8 + 20), (24, 24, 34, 255))
    hoja.paste(familia, (20, 10), familia)
    hoja.paste(grande, (30 * 8 + 40, 10), grande)
    hoja.save('/tmp/forma5_qa.png')
    print('  hoja: /tmp/forma5_qa.png')


if __name__ == '__main__':
    main()
