#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
activos_v6116.py — EL TALLER DE LA v6.116 — LA MÁSCARA DEL PRISMA.

LA LETRA DEL USUARIO: «hay errores» + client.log — el mod v6.115 SE
DESACTIVABA SOLO al cargarlo en el CLIENTE:

    ReLogic.Content.AssetLoadException: Asset could not be found:
    "Content\\Tiles\\AltarPrismaTile_Highlight"
    → Terraria.ModLoader.ModTile.SetupContent() línea 145:
        if (TileID.Sets.HasOutlines[Type])
            TextureAssets.HighlightMask[Type] =
                ModContent.Request<Texture2D>(HighlightTexture);

EL PRISMA (AltarPrismaTile, v6.115) encendió `TileID.Sets.HasOutlines`
(calco del AltarDeOfrendas de la casa) pero NACIÓ SIN SU MÁSCARA — y el
cliente la EXIGE al cargar (el headless no la pide: el servidor no
carga texturas — la misma lección del guardián v6.97, ahora por la
puerta de las CONVENCIONES tML: HasOutlines ⇒ {Texture}_Highlight).

LA MÁSCARA — el patrón de la casa (AltarOfrendasTile_Highlight, v6.86):
  · UN SOLO MARCO a la escala del objeto completo (54×54 — el prisma
    es 3×3 tiles), aunque la lámina del tile anime 6 marcos;
  · el CONTORNO DE 1 PX del CUERPO del prisma — EL MOLDE de la casa:
    no se pelea con el alfa semitransparente del abanico (la
    exploración de umbrales lo demostró: a 230 el ruido de las puntas
    se funde con el cristal y las columnas de 2 px mueren; a 240 el
    cristal queda hueco) — se traza la GEOMETRÍA LIMPIA que el arte
    declara: EL CRISTAL facetado (triángulo simétrico flotando), LAS
    DOS COLUMNAS del pedimento, LOS DOS BLOQUES DE VELAS, LA LOSA y
    EL BASAMENTO que se ensancha;
  · BLANCO CÁLIDO de las páginas (255, 244, 214) sobre transparente —
    exactamente 2 colores, como el altar.

USO:  python3 tools/activos_v6116.py
"""
import os

from PIL import Image

AQUI = os.path.dirname(os.path.abspath(__file__))
RAIZ = os.path.join(AQUI, '..', 'AethonMod')
DESTINO = os.path.join(RAIZ, 'Content', 'Tiles', 'AltarPrismaTile_Highlight.png')

LADO = 54                      # el prisma es 3×3 tiles (16+16+22)
BLANCO_CASA = (255, 244, 214, 255)   # el blanco cálido de las páginas

# =====================================================================
#  EL MOLDE — la geometría del cuerpo del prisma (leída del arte de
#  AltarPrismaTile.png: la unión sólida de sus 6 marcos declara el
#  cristal triangular de filas 9-30, las columnas de 2 px en filas
#  36-41, los bloques de velas en 42-43 y el pedimento 44-53 — aquí
#  vestida SIMÉTRICA sobre el eje 26.5, como manda la casa).
# =====================================================================

# EL CRISTAL: triángulo simétrico — ápice (27, 9), base (16..38, 30).
CRISTAL_APIX_X = 27
CRISTAL_APIX_Y = 9
CRISTAL_BASE_Y = 30
CRISTAL_BASE_MEDIO = 11        # 27 ± 11 → base 16..38 (23 px)

# LAS COLUMNAS del pedimento (2 px cada una, filas 36-41).
COLUMNA_IZQ = (11, 12)
COLUMNA_DER = (41, 42)
COLUMNA_Y = (36, 41)

# LOS BLOQUES DE VELAS (filas 42-43) sobre la losa.
VELA_IZQ = (11, 20)
VELA_DER = (33, 42)
VELA_Y = (42, 43)

# LA LOSA (filas 44-45) y EL BASAMENTO que se ensancha (filas 46-53).
LOSA_X = (8, 45)
LOSA_Y = (44, 45)
BASAMENTO = {                  # fila → (x0, x1)
    46: (11, 42), 47: (11, 42),
    48: (10, 43), 49: (10, 43),
    50: (9, 44), 51: (9, 44),
    52: (8, 45), 53: (8, 45),
}


def construir_cuerpo():
    """El cuerpo sólido del prisma: cristal + columnas + velas + losa + basamento."""
    m = [[False] * LADO for _ in range(LADO)]

    # EL CRISTAL (triángulo relleno).
    for y in range(CRISTAL_APIX_Y, CRISTAL_BASE_Y + 1):
        medio = CRISTAL_BASE_MEDIO * (y - CRISTAL_APIX_Y) / (CRISTAL_BASE_Y - CRISTAL_APIX_Y)
        izq = round(CRISTAL_APIX_X - medio)
        der = round(CRISTAL_APIX_X + medio)
        for x in range(izq, der + 1):
            m[y][x] = True

    # LAS COLUMNAS.
    for x0, x1 in (COLUMNA_IZQ, COLUMNA_DER):
        for y in range(COLUMNA_Y[0], COLUMNA_Y[1] + 1):
            for x in range(x0, x1 + 1):
                m[y][x] = True

    # LOS BLOQUES DE VELAS.
    for x0, x1 in (VELA_IZQ, VELA_DER):
        for y in range(VELA_Y[0], VELA_Y[1] + 1):
            for x in range(x0, x1 + 1):
                m[y][x] = True

    # LA LOSA.
    for y in range(LOSA_Y[0], LOSA_Y[1] + 1):
        for x in range(LOSA_X[0], LOSA_X[1] + 1):
            m[y][x] = True

    # EL BASAMENTO.
    for y, (x0, x1) in BASAMENTO.items():
        for x in range(x0, x1 + 1):
            m[y][x] = True

    return m


def dentro(x, y):
    return 0 <= x < LADO and 0 <= y < LADO


def main():
    cuerpo = construir_cuerpo()

    # EL CONTORNO INTERIOR de 1 px: sólido con algún vecino-4 fuera
    # (el borde de la forma — el trazo del altar de la casa).
    contorno = [[False] * LADO for _ in range(LADO)]
    for y in range(LADO):
        for x in range(LADO):
            if cuerpo[y][x]:
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    if not dentro(x + dx, y + dy) or not cuerpo[y + dy][x + dx]:
                        contorno[y][x] = True
                        break

    # LA PINTURA: el blanco cálido de las páginas sobre transparente.
    mascara = Image.new('RGBA', (LADO, LADO), (0, 0, 0, 0))
    px = mascara.load()
    for y in range(LADO):
        for x in range(LADO):
            if contorno[y][x]:
                px[x, y] = BLANCO_CASA
    mascara.save(DESTINO)

    # LA VERIFICACIÓN de la casa.
    ver = Image.open(DESTINO).convert('RGBA')
    colores = {ver.getpixel((x, y)) for y in range(ver.size[1]) for x in range(ver.size[0])}
    pintados = sum(1 for y in range(LADO) for x in range(LADO) if contorno[y][x])
    print(f'máscara: {ver.size[0]}x{ver.size[1]} — {pintados} px de contorno — '
          f'cristal + columnas + velas + losa + basamento')
    print(f'colores exactos: {sorted(colores)}')
    assert ver.size == (LADO, LADO), 'la máscara debe ser 54x54'
    assert colores <= {BLANCO_CASA, (0, 0, 0, 0)}, 'la máscara debe tener SOLO 2 colores'
    assert pintados > 100, 'el contorno no puede ser tan flaco'
    print('\nEL MAPA (el ojo de la casa):')
    for y in range(LADO):
        print(f'{y:3d} ' + ''.join('#' if contorno[y][x] else '·' for x in range(LADO)))
    print(f'\nOK — {os.path.relpath(DESTINO, RAIZ)}')


if __name__ == '__main__':
    main()
