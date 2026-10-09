#!/usr/bin/env python3
"""v6.50.89 — EL ICONO DEL GRIMORIO INESTABLE.

DECISIÓN: el icono es una COPIA BYTE A BYTE del sprite base del Grimorio
Hambriento (36×49) — la convención de la familia (Errático y Tembloroso
comparten el mismo PNG, md5 15dd6ab7…). Se probó primero hornearle un halo
rojo a la silueta, pero el sprite base es un RECTÁNGULO LLENO (solo 2 px
transparentes de 1.764): no hay margen donde viva el halo.

El AURA ROJA PEQUEÑA es 100 % CÓDIGO VIVO: el SoftGlow de la librería de
efectos teñido de rojo, respirando detrás del libro en los TRES estados
(inventario, mundo y mano) — así el aura se ve hasta en la cuadrícula del
inventario, latiendo, que es mejor que un halo muerto horneado.
"""
import hashlib
import shutil

BASE = 'AethonMod/Content/Weapons/GrimorioHambriento.png'
OUT = 'AethonMod/Content/Weapons/GrimorioHambrientoInestable.png'

shutil.copyfile(BASE, OUT)
md5 = hashlib.md5(open(OUT, 'rb').read()).hexdigest()
assert md5 == hashlib.md5(open(BASE, 'rb').read()).hexdigest()
print('OK', OUT, '== base (md5', md5[:8] + ') — copia byte a byte, como las otras dos copias')
