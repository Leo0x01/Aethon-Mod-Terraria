#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""v6.181 — EL RETRATO DEL SELLADO (la cura del arranque del cliente).

LA LECCIÓN (la v6.180 no cargó): ModNPC.AutoStaticDefaults() pide la
textura PRINCIPAL del NPC en SU RUTA DEFAULT (Content/NPCs/AethonSellado)
y el gigante murió con SU png — el Sellado nuevo nació sin retrato y el
mod entero se desactivó en la máquina del usuario
(«AssetLoadException: Content\\NPCs\\AethonSellado»).

EL RETRATO se compone EXACTAMENTE como el NPC se dibuja en juego
(AethonSellado.PreDraw): el CUERPO es el sprite del arma
(GrimorioHambriento, 36×49) a ESCALA 1.30 y SU OJO es el iris que MIRA
(GrimorioHambriento_Iris, 32×32) a la misma escala, centrado en SU CASA
(OJO = 19.09, 22.45 de 36×49 — el código es la fuente de la geometría).

También nace el ICONO DE CABEZA (AethonSellado_Head_Boss, 32×32): el
Sellado es jefe (NPC.boss = true, BarraAethonSellado es su barra) y la
convención de la casa (AethonBoss_Head_Boss) es «{textura}_Head_Boss».

SALIDA:
  AethonMod/Content/NPCs/AethonSellado.png          (50×66, el retrato)
  AethonMod/Content/NPCs/AethonSellado_Head_Boss.png (32×32, el icono)
"""
import os

from PIL import Image

AQUI = os.path.dirname(os.path.abspath(__file__))
RAIZ = os.path.join(AQUI, '..', 'AethonMod')
W = 'Content/Weapons'
N = 'Content/NPCs'

# === LA GEOMETRÍA DEL CÓDIGO (AethonSellado.cs — la fuente de la verdad) ===
ESCALA = 1.30            # private const float ESCALA
CUERPO_W, CUERPO_H = 36, 49      # GrimorioHambriento.Size()
IRIS_W = 32                     # GrimorioHambriento_Iris.Size()
OJO_X, OJO_Y = 19.09, 22.45     # LA CASA DEL OJO (coords del sprite 36×49)
# casaOjo = destino + (OJO - cuerpo*0.5) * ESCALA  → el iris centrado ahí

SS = 4  # super-muestreo para bordes limpios


def cargar(nombre):
    return Image.open(os.path.join(RAIZ, W, nombre)).convert('RGBA')


def en(canvas_ss, im, cx_ss, cy_ss, escala):
    """Pega im escalado (escala*SS) centrado en (cx_ss, cy_ss) del canvas SS."""
    w, h = im.size
    nuevo = im.resize((round(w * escala * SS), round(h * escala * SS)), Image.LANCZOS)
    x = round(cx_ss - nuevo.width / 2)
    y = round(cy_ss - nuevo.height / 2)
    canvas_ss.alpha_composite(nuevo, (x, y))


# ══ 1) EL RETRATO: 50×66 (el cuerpo 46.8×63.7 + margen) ══
PW, PH = 50, 66
retrato = Image.new('RGBA', (PW * SS, PH * SS), (0, 0, 0, 0))
cx, cy = PW / 2, PH / 2
en(retrato, cargar('GrimorioHambriento.png'), cx * SS, cy * SS, ESCALA)
# el centro del ojo: destino + (OJO - centro_del_cuerpo) * ESCALA
ox = cx + (OJO_X - CUERPO_W / 2) * ESCALA
oy = cy + (OJO_Y - CUERPO_H / 2) * ESCALA
en(retrato, cargar('GrimorioHambriento_Iris.png'), ox * SS, oy * SS, ESCALA)
retrato = retrato.resize((PW, PH), Image.LANCZOS)

# ══ 2) EL ICONO DE CABEZA: 32×32 (la convención AethonBoss_Head_Boss) ══
cabeza = Image.new('RGBA', (32, 32), (0, 0, 0, 0))
# el retrato entero (libro + ojo) a 30 px de alto, centrado
mini = retrato.resize((round(PW * 30 / PH), 30), Image.LANCZOS)
cabeza.alpha_composite(mini, ((32 - mini.width) // 2, (32 - 30) // 2))

# ══ LA VERIFICACIÓN (nada nace sin prueba) ══
for nombre, im in (('AethonSellado.png', retrato), ('AethonSellado_Head_Boss.png', cabeza)):
    ruta = os.path.join(RAIZ, N, nombre)
    im.save(ruta)
    opacos = sum(1 for p in im.getdata() if p[3] > 0)
    print(f'{nombre}: {im.size[0]}x{im.size[1]} · {opacos} px opacos · '
          f'bbox {im.getbbox()} · {os.path.getsize(ruta)} B')
    assert im.size in [(PW, PH), (32, 32)]
    assert opacos > 200, '¡el sprite nació vacío!'

print('EL SELLADO YA TIENE CARA (retrato 50x66 + cabeza 32x32)')
