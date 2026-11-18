#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
pilar_v6152.py — v6.152 — EL PILAR DEL ETERNO, NUEVO DE RAÍZ (la letra
del usuario: «el pilar no ha sido cambiado, creo que lo mejor es
borrarlo y crearlo de nuevo con otra forma, que no tenga ningun ojo y
asegurate de que no este recortado mal, usa la sforja para eso»).

LA NUEVA FORMA — EL OBELISCO DEL UMBRAL (nada que ver con el prisma
viejo): una AGUJA TRIPLICADA — tres cuerpos verticales fundidos (el
central alto y grave, dos guardianes inclinados a sus lados), cada uno
con su BANDA DE RUNAS descendiendo en persecución, y en la corona UN
ANILO DE CONJURO abierto (el aro que era el faro del prisma — ahora es
la PUERTA: el aro vacío por donde se sale del mundo interior).

  · SIN OJO — ninguno. El ojo-minion (PilarOjoMinion) MUERE con el
    prisma viejo: la nueva forma no necesita vigilante.
  · SIN RECORTE — el marco 3×5 (54×86) nace COMPLETO: las tres
    CoordinateHeights (16,16,16,16,22) pintan el obelisco de UNA pieza
    vertical continua (columna sólida de ancho completo en el último
    tile — el corte superior era la fila 22: aquí TODO el marco tiene
    masa hasta arriba).
  · LA ANIMACIÓN (6 marcos): LAS RUNAS EN DESCENSO — la banda de
    glifos baja por el cuerpo central marco a marco (la luz que cae),
    EL ARO que RESPIRA (el anillo de la corona late en grosor), y LAS
    CHISPAS que caen del aro a los guardianes (en contrafase).

REGLA DE LA CASA: transparencia NATIVA (el lienzo nace alfa=0).

Salidas:
  Content/Tiles/PilarPrismaTile.png  (54×516 — 6 marcos de 86 px)
"""
from PIL import Image, ImageDraw
import math, os

os.chdir("/home/z/my-project/mod/AethonMod")

W, H, MARCOS = 54, 86, 6
ALTO = H * MARCOS

# LA PALETA DEL PILAR (la violeta de la casa + el hueso cálido del umbral)
VIOLETA_HONDO = (74, 42, 128)
VIOLETA_CUERPO = (104, 62, 168)
VIOLETA_ALTO = (138, 92, 208)
VIOLETA_LUZ = (186, 140, 250)
ORO_RUNA = (255, 208, 120)
ORO_LUZ = (255, 236, 180)
HUESO = (216, 200, 180)
HUESO_SOMBRA = (168, 150, 134)

lienzo = Image.new("RGBA", (W, ALTO), (0, 0, 0, 0))

def glifo(d, x, y, tam, color, fase):
    """UN GLIFO RUNICO: 3-5 trazos deterministas (la runa solar mínima)."""
    t = max(2, tam)
    d.point((x, y), fill=color)
    d.line([(x - t, y), (x + t, y)], fill=color)
    d.line([(x, y - t), (x, y + t)], fill=color)
    if fase % 2 == 0:
        d.line([(x - t, y - t), (x + t, y + t)], fill=color)
    else:
        d.line([(x - t, y + t), (x + t, y - t)], fill=color)

for m in range(MARCOS):
    y0 = m * H
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)

    # ================================================================
    # 1. LOS DOS GUARDIANES (los cuerpos inclinados a los lados —
    #    nacen en la base y SE INCLINAN hacia el central)
    # ================================================================
    def guardian(cx_base, alto, inclinacion, ancho):
        for y in range(alto):
            u = y / max(1, alto - 1)          # 0 base → 1 punta
            x = cx_base + int(inclinacion * u * u)
            w = max(1, int(ancho * (1.0 - 0.35 * u)))   # se afila
            col = tuple(int(VIOLETA_CUERPO[i] + (VIOLETA_HONDO[i] - VIOLETA_CUERPO[i]) * (1 - u))
                       for i in range(3))
            for dx in range(-w, w + 1):
                a = 255 if abs(dx) < w else 200
                px = im.getpixel((x + dx, H - 6 - y))
                if px[3] == 0:
                    im.putpixel((x + dx, H - 6 - y), col + (a,))
    guardian(12, 52, -6, 4)     # el izquierdo
    guardian(W - 13, 52, 6, 4)  # el derecho

    # ================================================================
    # 2. EL CUERPO CENTRAL — la aguja (gruesa abajo, afilada arriba,
    #    de UNA pieza hasta la coronación)
    # ================================================================
    for y in range(H - 4):
        u = y / (H - 5)                     # 0 abajo → 1 arriba
        w = max(3, int(9 - 4.5 * u))        # 9 → 4 px de medio-ancho
        cx = W // 2
        col = tuple(int(VIOLETA_CUERPO[i] + (VIOLETA_ALTO[i] - VIOLETA_CUERPO[i]) * u)
                   for i in range(3))
        for dx in range(-w, w + 1):
            xx = cx + dx
            # el filo lateral (la arista iluminada del lado de la luz)
            borde = abs(dx) >= w - 1
            c2 = VIOLETA_LUZ if (borde and dx < 0) else col
            im.putpixel((xx, H - 4 - y), c2 + (255,))

    # LA BASE PEDESTAL (los tres pies fundidos — el último tile de 22
    # px pinta la zanca COMPLETA del marco: nada queda cortado)
    for y in range(6):
        w = 24 - y * 2
        for dx in range(-w, w + 1):
            c = HUESO_SOMBRA if abs(dx) > w - 3 else HUESO
            im.putpixel((W // 2 + dx, H - 1 - y), c + (255,))
    # las gradas del pedestal (el sello de hueso con runas)
    for gx in range(-18, 19, 6):
        glifo(d, W // 2 + gx, H - 3, 1, ORO_RUNA, m)

    # ================================================================
    # 3. LAS BANDAS DE RUNAS EN DESCENSO (la animación principal: la
    #    luz que CAE por el cuerpo central — la banda baja un glifo
    #    por marco, en persecución)
    # ================================================================
    for banda in range(3):
        yb = 12 + banda * 22 + ((m * 4) % 22)   # el descenso
        if yb > H - 10: continue
        brillo = 1.0 - 0.25 * banda
        col_r = tuple(int(ORO_RUNA[i] * brillo) for i in range(3))
        # los glifos de la banda: 3 en el cuerpo central
        for gx in (-4, 0, 4):
            glifo(d, W // 2 + gx, yb, 1, col_r + (255,), m + gx)

    # LOS GLIFOS DE LOS GUARDIANES (fijos, prenden en contrafase)
    if (m + 1) % 2 == 0:
        for gy in (20, 34, 48):
            glifo(d, 9, gy, 1, ORO_RUNA + (255,), m)
            glifo(d, W - 10, gy + 6, 1, ORO_RUNA + (255,), m)

    # ================================================================
    # 4. LA CORONA — EL ARO DE CONJURO ABIERTO (la puerta del umbral:
    #    el anillo VACÍO que respira — sin ojo, sin prisma: el aro)
    # ================================================================
    cy_aro, r_aro = 9, 8
    latido = 1 + 0.14 * math.sin(m * math.pi / 3)
    rr = int(r_aro * latido)
    for k in range(64):
        ang = k * 2 * math.pi / 64
        x = W // 2 + int(math.cos(ang) * rr * 1.0)
        y = cy_aro + int(math.sin(ang) * rr * 0.62)
        brillo = 0.55 + 0.45 * math.sin(ang * 3 + m * 0.9)
        c = tuple(int(VIOLETA_LUZ[i] * (0.5 + 0.5 * brillo)) for i in range(3))
        im.putpixel((x, y), c + (255,))
    # el núcleo del aro (el vacío iluminado que atraviesa)
    d.ellipse([W // 2 - 3, cy_aro - 2, W // 2 + 3, cy_aro + 2],
              fill=ORO_LUZ + (150,))

    # ================================================================
    # 5. LAS CHISPAS QUE CAEN del aro a los guardianes (contrafase)
    # ================================================================
    for s in range(4):
        fase = (m + s * 2) % 6
        y_s = 14 + fase * 6
        x_s = W // 2 + (10 if s % 2 == 0 else -10)
        if y_s < H - 8:
            brillo = 1.0 - fase / 7
            col_s = tuple(int(ORO_LUZ[i] * brillo) for i in range(3))
            im.putpixel((x_s, y_s), col_s + (255,))
            im.putpixel((x_s + 1, y_s), col_s + (160,))

    # pegar el marco
    lienzo.alpha_composite(im, (0, y0))

# LA VERIFICACIÓN DE LA CASA: el marco es COMPLETO — la fila 22 (la
# última del tile) tiene masa en TODO el ancho del pedestal; la fila 0
# (la coronación) tiene el aro
for m in range(MARCOS):
    y0 = m * H
    fila_0 = [lienzo.getpixel((x, y0 + 2)) for x in range(W)]
    fila_85 = [lienzo.getpixel((x, y0 + H - 2)) for x in range(W)]
    assert any(p[3] > 0 for p in fila_0), f"marco {m}: la coronación está vacía"
    assert sum(1 for p in fila_85 if p[3] > 0) > 30, f"marco {m}: la base está cortada"

lienzo.save("Content/Tiles/PilarPrismaTile.png")
print(f"  ✔ PilarPrismaTile.png NUEVO — {W}×{ALTO} (6 marcos de {H}), "
      f"el obelisco del umbral SIN ojo y SIN cortes")

# y el icono del ítem (el marco 0)
icono = lienzo.crop((0, 0, W, H))
icono.save("Content/Tiles/PilarPrismaTile_Icono.png")
print("  ✔ PilarPrismaTile_Icono.png (el marco 0 — para el ítem)")
