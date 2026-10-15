#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_grimorio_menor_v650124.py — v6.50.124 — EL ICONO QUE PARPADEA.

LA LETRA DEL USUARIO: «ademas el icono de su item no tiene animacion
ni movimiento en los ojos».

LA CAUSA: el icono del mini grimorio (GrimorioMenorItem.png, la .122)
es el resize LANCZOS 24×33 del libro del Nervioso — y el Nervioso
lleva la esclerótica BLANCA VACÍA (el iris del arma/mascota se dibuja
ENCIMA cada frame: GrimorioHambriento_Iris en OJO + mirada). El icono
quedó con el ojo CIEGO: ni pupila ni vida.

LA CURA (dos piezas):
 1. LA PUPILA VIVA: GrimorioMenorItem dibuja el iris ENCIMA del icono
    cada frame (PostDrawInInventory + PreDrawInWorld): MIRA de lado a
    lado (dos senos superpuestos, deterministas — el reloj del mundo)
    y la pupila RESPIRA (±6%).
 2. EL PARPADEO: GrimorioMenorItem_Cerrado.png — el resize LANCZOS
    24×33 de GrimorioHambriento_Cerrado (el libro con el párpado
    cerrado): como el icono base nació del MISMO resize del libro
    abierto, el overlay alinea AL PÍXEL — el libro CIERRA el ojo un
    instante cada ~4,6 s (el compás del flotante).

Este generador produce (DETERMINISTA — byte a byte reproducible):
  · Content/Items/Llamados/GrimorioMenorItem.png          (verificación: == resize del Nervioso)
  · Content/Items/Llamados/GrimorioMenorItem_Cerrado.png  (NUEVO — el párpado)
  · tools/tools/v650124_icono_montaje.png                 (hoja de contacto para el VLM)
"""
import os

from PIL import Image, ImageDraw

RAIZ = os.path.join(os.path.dirname(__file__), '..', 'AethonMod')
LIBRO = os.path.join(RAIZ, 'Content', 'Weapons', 'GrimorioHambrientoNervioso.png')
LIBRO_CERRADO = os.path.join(RAIZ, 'Content', 'Weapons', 'GrimorioHambriento_Cerrado.png')
IRIS = os.path.join(RAIZ, 'Content', 'Weapons', 'GrimorioHambriento_Iris.png')
ICONO = os.path.join(RAIZ, 'Content', 'Items', 'Llamados', 'GrimorioMenorItem.png')
ICONO_CERRADO = os.path.join(RAIZ, 'Content', 'Items', 'Llamados', 'GrimorioMenorItem_Cerrado.png')
MONTAJE = os.path.join(os.path.dirname(__file__), 'tools', 'v650124_icono_montaje.png')

# === LA GEOMETRÍA DEL OJO (medida del icono real — esclerótica blanca) ===
OJO = (12.0, 15.0)          # centro de la esclerótica en texels del icono 24×33
ESCALA_IRIS = 0.2773 * 24.0 / 36.0   # IRIS_ESC del libro × la escala del icono ≈ 0.185 (pupila ≈ 5.9 px)


def main():
    os.makedirs(os.path.dirname(MONTAJE), exist_ok=True)

    # === 1. EL ICONO CERRADO (el overlay del parpadeo) ===
    cerrado = Image.open(LIBRO_CERRADO).convert('RGBA').resize((24, 33), Image.LANCZOS)
    cerrado.save(ICONO_CERRADO)
    print(f'OK  {os.path.relpath(ICONO_CERRADO)} {cerrado.size}')

    # === 2. VERIFICACIÓN DEL ICONO BASE (debe ser el resize del libro) ===
    libro = Image.open(LIBRO).convert('RGBA')
    esperado = libro.resize((24, 33), Image.LANCZOS)
    actual = Image.open(ICONO).convert('RGBA')
    igual = list(esperado.getdata()) == list(actual.getdata())
    print(f'{"OK " if igual else "MAL"} icono base == resize LANCZOS 24×33 del Nervioso: {igual}')
    assert igual, 'el icono base cambió de origen — revisar la geometría del ojo'

    # === 3. LA HOJA DE CONTACTO (el mock para el VLM) ===
    # 8 celdas ×5 (ampliación ×5): mirada izquierda/centro/derecha/arriba/abajo,
    # pupila respirando, el destello, el parpadeo cerrado y el icono tal cual.
    iris = Image.open(IRIS).convert('RGBA')
    d_iris = max(2, round(32 * ESCALA_IRIS))

    def celda(mirada=(0, 0), cerrado_ojo=False, destello=True):
        img = actual.copy() if not cerrado_ojo else cerrado.copy()
        if not cerrado_ojo:
            px = round(OJO[0] + mirada[0] - d_iris / 2)
            py = round(OJO[1] + mirada[1] - d_iris / 2)
            pupila = iris.resize((d_iris, d_iris), Image.LANCZOS)
            img.alpha_composite(pupila, (px, py))
            if destello:
                dd = ImageDraw.Draw(img)
                sx = round(OJO[0] + mirada[0] - 1.4)
                sy = round(OJO[1] + mirada[1] - 1.6)
                dd.rectangle([sx, sy, sx, sy], fill=(255, 250, 240, 235))
        return img.resize((24 * 5, 33 * 5), Image.NEAREST)

    miradas = {
        'mira izq': (-0.95, 0.0), 'mira centro': (0.0, 0.0), 'mira der': (0.95, 0.0),
        'mira arriba': (0.0, -0.6), 'mira abajo': (0.0, 0.6),
        'parpadeo (cerrado)': (0.0, 0.0),
    }
    celdas = [celda(m) for m in miradas.values()] + [celda((0.0, 0.0), cerrado_ojo=True)]
    ancho = 24 * 5
    alto = 33 * 5
    hoja = Image.new('RGBA', (ancho * len(celdas) + 8 * (len(celdas) + 1), alto + 16),
                     (34, 30, 44, 255))
    x = 8
    for c in celdas:
        hoja.alpha_composite(c, (x, 8))
        x += ancho + 8
    hoja.save(MONTAJE)
    print(f'OK  {os.path.relpath(MONTAJE)} — {len(celdas)} celdas')


if __name__ == '__main__':
    main()
