#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_v686_assets.py — LOS PNG DE LA v6.86 (todo procedural, PIL):
  1. Content/Items/Altar/LibroCorrupto.png        (22×30 — el libro negro de la mazmorra)
  2. Content/Items/Altar/AltarOfrendasItem.png     (30×44 — el icono del altar)
  3. Content/Tiles/AltarOfrendasTile.png           (36×54 — la hoja del tile 2×3)
  4. Content/Items/Investigaciones/CodiceHabilidades.png (28×28 — el códice con el árbol)
  5. Content/UI/ArbolHabilidades_Cadena.png        (16×16 — el eslabón de PoT/Umbra)
  6. Content/UI/ArbolHabilidades_Nodo.png          (48×48 — el disco del nodo, TINTABLE)
Y una hoja de contacto 4x para el VLM de la casa.
"""
from PIL import Image, ImageDraw

RAIZ = '/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod'

# LA PALETA (la de la casa)
NEGRO_BORDE = (10, 7, 20, 255)
TINTA_LIBRO = (20, 16, 29, 255)
TINTA_LIBRO_ALTA = (58, 44, 85, 255)
HOJA_VIEJA = (216, 208, 196, 255)
PIEDRA = (107, 127, 132, 255)
PIEDRA_CLARA = (139, 161, 166, 255)
PIEDRA_OScura = (62, 78, 84, 255)
TEAL = (90, 220, 180, 255)
TEAL_CLARO = (150, 245, 215, 255)
TEAL_OSCURO = (30, 110, 92, 255)
DORADO = (255, 215, 120, 255)
DORADO_OSCURO = (170, 130, 60, 255)
CODICE = (42, 27, 74, 255)
CODICE_ALTO = (74, 52, 122, 255)
CADENA_CLARA = (200, 200, 208, 255)
CADENA_MEDIA = (138, 138, 150, 255)
CADENA_OSCURA = (85, 85, 95, 255)
NODO_BLANCO = (255, 255, 255, 255)
NODO_ANILLO = (207, 207, 216, 255)
NODO_RIM = (106, 106, 120, 255)


def px(d, x, y, c):
    d.point((x, y), fill=c)


def rect(d, x0, y0, x1, y1, c):
    d.rectangle([x0, y0, x1, y1], fill=c)


def guardar(img, ruta_relativa):
    ruta = f'{RAIZ}/{ruta_relativa}'
    img.save(ruta)
    print(f'  OK {ruta_relativa} {img.size}')


# ================================================================
#  1) EL LIBRO CORRUPTO (22×30) — mismo cuerpo que los libros de la
#     mazmorra, pero NEGRO: tapa tinta con brillo violeta, bloque de
#     hojas pálido a la derecha, runa corrupta en la tapa.
# ================================================================
def gen_libro():
    img = Image.new('RGBA', (22, 30), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    # EL CUERPO de la tapa (con el lomo a la izquierda)
    rect(d, 2, 3, 19, 26, NEGRO_BORDE)
    rect(d, 3, 4, 18, 25, TINTA_LIBRO)
    # EL LOMO con su brillo violeta
    rect(d, 3, 4, 5, 25, TINTA_LIBRO_ALTA)
    for y in range(6, 25, 3):
        px(d, 4, y, (90, 70, 130, 255))
    # EL BLOQUE DE HOJAS (a la derecha, asomando)
    rect(d, 17, 5, 18, 24, HOJA_VIEJA)
    for y in range(6, 24, 3):
        px(d, 17, y, (150, 142, 130, 255))
    # LA RUNA CORRUPTA en la tapa (una V invertida con punto — el sello)
    rect(d, 9, 10, 13, 10, (150, 110, 235, 255))
    px(d, 8, 11, (150, 110, 235, 255)); px(d, 14, 11, (150, 110, 235, 255))
    px(d, 9, 12, (110, 80, 190, 255)); px(d, 13, 12, (110, 80, 190, 255))
    px(d, 10, 13, (110, 80, 190, 255)); px(d, 12, 13, (110, 80, 190, 255))
    rect(d, 10, 14, 12, 14, (90, 60, 170, 255))
    # EL PUNTO de la runa
    rect(d, 10, 17, 12, 19, DORADO)
    # LA BASE (la peana del libro)
    rect(d, 2, 27, 19, 27, NEGRO_BORDE)
    return img


# ================================================================
#  2) EL ICONO DEL ALTAR (30×44) — mesa de piedra de dos cuerpos con
#     el ojo teal flotando arriba y su bowl de ofrenda.
# ================================================================
def gen_altar_item():
    img = Image.new('RGBA', (30, 44), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    # EL OJO FLOTANTE (arriba): halo + iris teal
    rect(d, 11, 2, 18, 9, TEAL_OSCURO)            # marco
    rect(d, 12, 3, 17, 8, (18, 44, 40, 255))      # fondo del ojo
    rect(d, 13, 4, 16, 7, TEAL)                   # iris
    rect(d, 14, 5, 15, 6, TEAL_CLARO)             # brillo
    # LA MESA — cuerpo superior (más angosto)
    rect(d, 4, 12, 25, 20, NEGRO_BORDE)
    rect(d, 5, 13, 24, 19, PIEDRA)
    rect(d, 5, 13, 24, 14, PIEDRA_CLARA)          # la luz de arriba
    for x in range(7, 24, 4):                     # las runas grabadas
        px(d, x, 16, TEAL_OSCURO); px(d, x, 17, (50, 70, 74, 255))
    # EL BOWL DE OFRENDA (el cuenco en la mesa)
    rect(d, 12, 15, 17, 18, (24, 40, 44, 255))
    rect(d, 13, 16, 16, 17, TEAL * 2 if False else (60, 180, 150, 255))
    # EL CUERPO INFERIOR (más ancho — la peana)
    rect(d, 2, 22, 27, 32, NEGRO_BORDE)
    rect(d, 3, 23, 26, 31, PIEDRA_OScura)
    rect(d, 3, 23, 26, 24, PIEDRA)                # su luz
    rect(d, 6, 26, 10, 28, PIEDRA)                # el hueco sombra
    rect(d, 19, 26, 23, 28, PIEDRA)
    rect(d, 12, 26, 16, 28, (40, 56, 60, 255))    # el nicho central
    px(d, 14, 27, TEAL)                           # la chispa del nicho
    # LA BASE final
    rect(d, 1, 34, 28, 36, NEGRO_BORDE)
    rect(d, 2, 35, 27, 35, PIEDRA_OScura)
    # LOS PIES
    rect(d, 3, 37, 7, 40, NEGRO_BORDE); rect(d, 4, 38, 6, 40, PIEDRA_OScura)
    rect(d, 22, 37, 26, 40, NEGRO_BORDE); rect(d, 23, 38, 25, 40, PIEDRA_OScura)
    # LAS ESTRELLAS (los cuatro tornillos dorados)
    for x, y in ((6, 14), (23, 14), (6, 30), (23, 30)):
        px(d, x, y, DORADO)
    return img


# ================================================================
#  3) LA HOJA DEL TILE (36×54) — Style2xX 2×3 con alturas 16/16/18
#     (pitch 18px): la misma mesa del icono, en cuerpo grande.
# ================================================================
def gen_altar_tile():
    img = Image.new('RGBA', (36, 54), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    # EL OJO arriba (fila 1: 0..16)
    rect(d, 13, 2, 22, 10, TEAL_OSCURO)
    rect(d, 14, 3, 21, 9, (18, 44, 40, 255))
    rect(d, 15, 4, 20, 8, TEAL)
    rect(d, 16, 5, 19, 7, TEAL_CLARO)
    # EL CUERPO SUPERIOR (fila 1-2: la mesa angosta)
    rect(d, 5, 13, 30, 20, NEGRO_BORDE)
    rect(d, 6, 14, 29, 19, PIEDRA)
    rect(d, 6, 14, 29, 15, PIEDRA_CLARA)
    for x in range(9, 29, 5):
        px(d, x, 17, TEAL_OSCURO)
    # EL BOWL central de la mesa
    rect(d, 15, 16, 20, 18, (24, 40, 44, 255))
    rect(d, 16, 17, 19, 18, (60, 180, 150, 255))
    # EL CUERPO INFERIOR (fila 2: la peana ancha)
    rect(d, 2, 22, 33, 36, NEGRO_BORDE)
    rect(d, 3, 23, 32, 35, PIEDRA_OScura)
    rect(d, 3, 23, 32, 24, PIEDRA)
    rect(d, 7, 27, 12, 31, PIEDRA)
    rect(d, 23, 27, 28, 31, PIEDRA)
    rect(d, 14, 27, 21, 31, (34, 50, 54, 255))    # el nicho
    rect(d, 16, 29, 19, 30, TEAL)                 # la chispa del nicho
    # LA BASE (fila 3: 16/16/18 — la base usa hasta 54)
    rect(d, 1, 38, 34, 44, NEGRO_BORDE)
    rect(d, 2, 39, 33, 43, PIEDRA_OScura)
    rect(d, 2, 39, 33, 40, PIEDRA)
    for x in range(6, 31, 6):                     # los tornillos de la base
        px(d, x, 41, DORADO_OSCURO)
    # LOS PIES hasta el suelo (54)
    rect(d, 4, 46, 12, 52, NEGRO_BORDE); rect(d, 5, 47, 11, 52, PIEDRA_OScura)
    rect(d, 23, 46, 31, 52, NEGRO_BORDE); rect(d, 24, 47, 30, 52, PIEDRA_OScura)
    rect(d, 14, 46, 21, 52, NEGRO_BORDE); rect(d, 15, 47, 20, 52, (24, 40, 44, 255))
    px(d, 17, 49, TEAL)
    return img


# ================================================================
#  4) EL CÓDICE DE HABILIDADES (28×28) — libro violeta con el ÁRBOL
#     dorado en la tapa (tronco + 3 ramas + 3 nodos).
# ================================================================
def gen_codice():
    img = Image.new('RGBA', (28, 28), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    # EL CUERPO
    rect(d, 3, 2, 24, 25, NEGRO_BORDE)
    rect(d, 4, 3, 23, 24, CODICE)
    rect(d, 4, 3, 6, 24, CODICE_ALTO)            # el lomo
    # EL ÁRBOL en la tapa: tronco
    rect(d, 13, 9, 14, 19, DORADO_OSCURO)
    # LAS RAMAS
    rect(d, 9, 12, 12, 12, DORADO_OSCURO); rect(d, 8, 11, 9, 11, DORADO_OSCURO)
    rect(d, 15, 12, 18, 12, DORADO_OSCURO); rect(d, 19, 11, 20, 11, DORADO_OSCURO)
    rect(d, 13, 8, 14, 8, DORADO_OSCURO)
    # LOS NODOS (las bolitas de las habilidades)
    for x, y in ((8, 9), (14, 6), (20, 9)):
        rect(d, x - 1, y - 1, x + 1, y + 1, DORADO)
        px(d, x, y, (255, 245, 210, 255))
    # LA RAÍZ (la base del tronco)
    rect(d, 11, 20, 16, 20, DORADO_OSCURO)
    # EL BROCHE
    rect(d, 22, 12, 23, 15, (170, 130, 60, 255))
    # LAS PÁGINAS a la derecha
    rect(d, 22, 4, 23, 23, HOJA_VIEJA)
    return img


# ================================================================
#  5) LA CADENA (16×16) — el eslabón de PoT/Umbra: dos óvalos
#     engarzados en horizontal (la UI la rota por la pendiente).
# ================================================================
def gen_cadena():
    img = Image.new('RGBA', (16, 16), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    # Eslabón 1 (izquierda- centro): óvalo horizontal 10×6
    d.ellipse([0, 5, 9, 10], fill=CADENA_MEDIA)
    d.ellipse([1, 6, 8, 9], fill=CADENA_CLARA)
    d.ellipse([3, 7, 6, 8], fill=CADENA_OSCURA)
    # Eslabón 2 (derecha-centro), pasando POR DENTRO del 1
    d.ellipse([6, 5, 15, 10], fill=CADENA_MEDIA)
    d.ellipse([7, 6, 14, 9], fill=CADENA_CLARA)
    d.ellipse([9, 7, 12, 8], fill=CADENA_OSCURA)
    # El cruce (el 2 tapa al 1 en su mitad derecha — el engarce)
    d.ellipse([6, 5, 15, 10], outline=CADENA_OSCURA)
    d.ellipse([0, 5, 9, 10], outline=CADENA_OSCURA)
    return img


# ================================================================
#  6) EL NODO (48×48) — disco TINTABLE: casi blanco (el Draw lo
#     multiplica por el color de estado — la lección de los tintes).
# ================================================================
def gen_nodo():
    img = Image.new('RGBA', (48, 48), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    # EL RIM exterior (oscuro)
    d.ellipse([2, 2, 45, 45], fill=NODO_RIM)
    # EL ANILLO
    d.ellipse([4, 4, 43, 43], fill=NODO_ANILLO)
    # EL DISCO central (blanco puro — tinte total)
    d.ellipse([10, 10, 37, 37], fill=NODO_BLANCO)
    # EL CORAZÓN (leve sombreado interior)
    d.ellipse([13, 13, 34, 34], fill=(250, 250, 255, 255))
    d.ellipse([16, 16, 31, 31], fill=NODO_BLANCO)
    # LAS 4 MUESCAS del anillo (norte/sur/este/oeste — el símbolo del nodo)
    for x, y in ((23, 4), (23, 41), (4, 23), (41, 23)):
        rect(d, x - 1, y - 1, x + 1, y + 1, NODO_RIM)
    return img


# ================================================================
#  LA HOJA DE CONTACTO (4x sobre gris, para el VLM)
# ================================================================
def hoja_contacto():
    piezas = [
        ('Content/Items/Altar/LibroCorrupto.png', 'libro'),
        ('Content/Items/Altar/AltarOfrendasItem.png', 'icono altar'),
        ('Content/Tiles/AltarOfrendasTile.png', 'tile altar'),
        ('Content/Items/Investigaciones/CodiceHabilidades.png', 'codice'),
        ('Content/UI/ArbolHabilidades_Cadena.png', 'cadena'),
        ('Content/UI/ArbolHabilidades_Nodo.png', 'nodo'),
    ]
    escala = 4
    alto_max = max(Image.open(f'{RAIZ}/{p}').height for p, _ in piezas) * escala
    ancho_total = sum(Image.open(f'{RAIZ}/{p}').width for p, _ in piezas) * escala + 30 * len(piezas)
    hoja = Image.new('RGBA', (ancho_total, alto_max + 40), (120, 120, 128, 255))
    x = 15
    for p, _ in piezas:
        im = Image.open(f'{RAIZ}/{p}').convert('RGBA')
        im = im.resize((im.width * escala, im.height * escala), Image.NEAREST)
        hoja.paste(im, (x, 20), im)
        x += im.width + 30
    hoja.save('/tmp/v686_hoja.png')
    print('  OK /tmp/v686_hoja.png (hoja de contacto 4x)')


if __name__ == '__main__':
    print('GEN v6.86 ASSETS:')
    guardar(gen_libro(), 'Content/Items/Altar/LibroCorrupto.png')
    guardar(gen_altar_item(), 'Content/Items/Altar/AltarOfrendasItem.png')
    guardar(gen_altar_tile(), 'Content/Tiles/AltarOfrendasTile.png')
    guardar(gen_codice(), 'Content/Items/Investigaciones/CodiceHabilidades.png')
    guardar(gen_cadena(), 'Content/UI/ArbolHabilidades_Cadena.png')
    guardar(gen_nodo(), 'Content/UI/ArbolHabilidades_Nodo.png')
    hoja_contacto()
    print('LISTO')
