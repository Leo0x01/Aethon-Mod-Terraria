#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_v638_assets.py — LOS ASSETS DE LA CAMADA DE LAS SIERPES (v6.38).

Pixel-art 30×30 estilo la casa (bastón diagonal + cabeza temática):
  1.  OuroborosAstralStaff   — el ANILLO de 12 cuentas con la mordida (turquesa)
  2.  CaravanaEspectralStaff — el FAROL ámbar con jaula + el rastro fantasma verde
  3.  AnguilaSolarStaff      — la CURVA EN S solar (la anguila en miniatura)
  4.  CienpiesRunicoStaff    — las PLACAS ámbar en fila + patitas frías + runa dorada
  5.  FlageloEstelarStaff    — el MANGO dorado + la cuerda carmesí (punta blanca-caliente)
  6.  ViboraGenesiacaStaff   — la DOBLE HÉLICE dorado/violeta (la escalera de mano)
  7.  BoaEclipseStaff        — la ESPIRAL blanca de 1.5 vueltas (presa + anillo ámbar)
  8.  FarolGuardianStaff     — el FAROL ámbar colgante + cadena fría + mano de 3 dedos
  9.  CintaAuroraStaff       — la CINTA verde→violeta (broche dorado)
  10. ManadaAstralStaff      — las TRES estrellitas azules en cuña (ojos dorados + corona)
  11. CriaEstelarStaff       — LA CRÍA: sierpecita blanca, ojos cian enormes, coronita

Y:
  · Los 11 sprites 76×76 de los proyectiles (glow suave del color de identidad).
  · BolsaSierpes 30×30  — el saco de la casa, firma AZUL CIELO + sierpecita en S.
  · CriaEstelarBuff 32×32 — la cabezota de la cría (icono de buff de la casa).

Determinista: random.Random(semilla fija), cero azar del sistema.
"""
import math
import os
import random

from PIL import Image, ImageDraw

BASE = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
W = os.path.join(BASE, "Content", "Weapons", "Cosmic")
P = os.path.join(BASE, "Content", "Projectiles", "Cosmic")
I = os.path.join(BASE, "Content", "Items", "Bolsas")
F = os.path.join(BASE, "Content", "Buffs")


# ----------------------------------------------------------------------
def baston_base(draw, madero=(94, 62, 38)):
    """El bastón diagonal clásico (abajo-izq → arriba-der) + engarce."""
    for i in range(17):
        x, y = 6 + i, 23 - i
        c = madero if i % 5 < 3 else (118, 80, 48)
        draw.point((x, y), fill=c)
        draw.point((x + 1, y), fill=(66, 44, 28))
    # El engarce dorado bajo la cabeza.
    for dx, dy in ((21, 7), (22, 6), (22, 8), (23, 7)):
        draw.point((dx, dy), fill=(255, 214, 106))


def lerp(c1, c2, t):
    return tuple(int(c1[i] + (c2[i] - c1[i]) * t) for i in range(3)) + (255,)


# ----------------------------------------------------------------------
# 1. OUROBOROS ASTRAL — el anillo de 12 cuentas y la mordida.
# ----------------------------------------------------------------------
def ouroboros(path):
    im = Image.new("RGBA", (30, 30), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    baston_base(d)
    cx, cy, R = 20, 8, 5.4
    N = 12
    a0 = -1.2                       # aquí muerde la cabeza
    # EL HALO interior (el aliento del anillo).
    d.ellipse((cx - 2, cy - 2, cx + 2, cy + 2), fill=(255, 208, 120, 55))
    d.point((cx, cy), fill=(255, 240, 200, 150))
    for k in range(N):
        a = a0 + k * math.tau / N
        x, y = cx + math.cos(a) * R, cy + math.sin(a) * R
        rx, ry = round(x), round(y)
        if k == 1:
            continue                # LA BOCA abierta (el hueco de la mordida)
        if k == 0:                  # LA CABEZA que muerde (la cuenta más brillante)
            d.ellipse((rx - 1, ry - 1, rx + 1, ry + 1), fill=(255, 250, 230, 255))
            d.point((rx, ry - 1), fill=(255, 255, 250, 255))
            d.point((rx + 1, ry), fill=(60, 40, 20, 255))       # el ojo del anillo
        elif k == N - 1:            # LA COLA turquesa entrando en la boca
            d.ellipse((rx, ry, rx + 1, ry + 1), fill=(90, 225, 230, 255))
            d.point((rx, ry), fill=(190, 250, 252, 255))
        else:                       # LAS CUENTAS doradas del cuerpo
            d.ellipse((rx, ry, rx + 1, ry + 1), fill=(255, 208, 120, 255))
            d.point((rx, ry), fill=(255, 236, 180, 255))
    # LA CHISPA del anillo (una, limpia).
    d.point((cx + 6, cy - 5), fill=(255, 240, 190, 200))
    im.save(path)


# ----------------------------------------------------------------------
# 2. CARAVANA ESPECTRAL — el farol y sus fantasmas en fila.
# ----------------------------------------------------------------------
def caravana(path):
    im = Image.new("RGBA", (30, 30), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    baston_base(d)
    # EL GANCHO frío que cuelga del engarce.
    d.line((22, 7, 23, 9), fill=(168, 188, 205, 255), width=1)
    # EL FAROL: marco oscuro + vidrio ámbar + llama verde-espectral.
    d.rectangle((21, 9, 26, 15), outline=(40, 32, 20, 255), width=1)
    d.rectangle((22, 10, 25, 14), fill=(255, 196, 92, 245))
    d.rectangle((23, 11, 24, 13), fill=(140, 255, 220, 255))
    d.point((23, 12), fill=(235, 255, 248, 255))
    # LA JAULA: la barra fría cruzando el vidrio.
    d.line((22, 10, 25, 14), fill=(150, 170, 185, 210), width=1)
    # EL RASTRO FANTASMA: 4 cuentas verdes cayendo en diagonal detrás
    #     (la caravana de espectros siguiendo al farol).
    for bx, by, al in ((18, 8, 255), (15, 10, 235),
                       (12, 12, 205), (9, 14, 175)):
        d.ellipse((bx, by, bx + 1, by + 1), fill=(140, 255, 220, al))
        d.point((bx + 1, by), fill=(220, 255, 245, al))
        d.point((bx, by + 1), fill=(90, 210, 180, al))
    # LA ESTELA tenue entre las cuentas (el hilo de la caravana).
    d.point((17, 9), fill=(140, 255, 220, 90))
    d.point((14, 11), fill=(140, 255, 220, 80))
    d.point((11, 13), fill=(140, 255, 220, 70))
    im.save(path)


# ----------------------------------------------------------------------
# 3. ANGUILA SOLAR — la curva en S del cuerpo eléctrico.
# ----------------------------------------------------------------------
def anguila(path):
    im = Image.new("RGBA", (30, 30), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    baston_base(d)
    # EL CUERPO ondulado (la S de la anguila, de la cola a la cabeza).
    for st in range(26):
        t = st / 25
        x = 16.0 + 10.0 * t
        y = 7.5 - 3.0 * t + 2.8 * math.sin(t * 6.5)
        rx, ry = round(x), round(y)
        d.point((rx, ry), fill=(255, 236, 120, 255))
        d.point((rx, ry + 1), fill=(240, 196, 70, 235))
        if st % 2 == 0:             # LA CRESTA dorsal, lo más brillante
            d.point((rx, ry - 1), fill=(255, 252, 200, 255))
        if st % 6 == 3:             # la aleta baja
            d.point((rx, ry + 2), fill=(255, 226, 140, 190))
    # LA CABEZA: el morro con el ojo oscuro.
    d.ellipse((25, 4, 27, 6), fill=(255, 240, 150, 255))
    d.point((26, 4), fill=(70, 44, 12, 255))
    d.point((27, 5), fill=(255, 252, 210, 220))
    # EL DESTELLO solar (dos puntos, limpios).
    d.point((24, 1), fill=(255, 250, 200, 220))
    d.point((17, 12), fill=(255, 226, 130, 190))
    im.save(path)


# ----------------------------------------------------------------------
# 4. CIEMPIÉS RÚNICO — placas ámbar, patitas frías y la runa al frente.
# ----------------------------------------------------------------------
def cienpies(path):
    im = Image.new("RGBA", (30, 30), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    baston_base(d)
    # EL LOMO: 7 placas ámbar con juntas oscuras (la fila segmentada).
    for i in range(7):
        x0 = 12 + i * 2                     # columnas pares: placa brillante
        d.point((x0, 7), fill=(255, 232, 165, 255))
        d.point((x0, 8), fill=(255, 176, 96, 255))
        d.point((x0, 9), fill=(216, 140, 74, 255))
        d.point((x0, 10), fill=(188, 116, 60, 255))
        if i < 6:                           # LA JUNTA oscura entre placas
            d.point((x0 + 1, 8), fill=(146, 92, 48, 255))
            d.point((x0 + 1, 9), fill=(120, 74, 38, 255))
            d.point((x0 + 1, 10), fill=(100, 62, 32, 255))
    # LA CABEZA al frente (placa mayor, ojo oscuro y antenas).
    d.rectangle((25, 6, 26, 10), fill=(255, 200, 120, 255))
    d.point((25, 6), fill=(255, 240, 190, 255))
    d.point((26, 7), fill=(70, 40, 14, 255))
    d.point((27, 5), fill=(255, 208, 128, 235))
    # LAS PATITAS frías (4 pares en V — la ola metacronal).
    for k, lx in enumerate((14, 17, 20, 23)):
        d.line((lx, 11, lx - 2, 13), fill=(150, 210, 255, 255), width=1)
        d.line((lx, 11, lx + 2, 13), fill=(150, 210, 255, 255), width=1)
        d.point((lx, 11), fill=(215, 240, 255, 255))
    # LA RUNA DORADA plantada al frente (ᛉ — el estandarte del ciempiés).
    d.line((27, 1, 27, 5), fill=(255, 232, 150, 255), width=1)
    d.line((28, 1, 28, 5), fill=(255, 214, 106, 255), width=1)
    d.line((28, 2, 26, 1), fill=(255, 240, 180, 255), width=1)
    d.line((28, 3, 26, 2), fill=(255, 240, 180, 255), width=1)
    d.line((28, 3, 29, 2), fill=(255, 240, 180, 255), width=1)
    d.point((28, 1), fill=(255, 252, 220, 255))
    d.point((28, 6), fill=(255, 232, 150, 235))
    im.save(path)


# ----------------------------------------------------------------------
# 5. FLAGELO ESTELAR — el mango dorado y la cuerda carmesí.
# ----------------------------------------------------------------------
def flagelo(path):
    im = Image.new("RGBA", (30, 30), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    baston_base(d)
    # EL NUDO del mango (donde agarra la cuerda).
    d.ellipse((21, 9, 22, 10), fill=(255, 214, 106, 255))
    # LA CUERDA carmesí ondulándose (grosor que muere en la punta).
    for st in range(24):
        t = st / 23
        x = 22.0 + 6.3 * t
        y = 9.0 + 9.5 * t + 2.8 * math.sin(t * 6.8 + 2.4)
        rx, ry = round(x), round(y)
        if t < 0.82:
            d.point((rx, ry), fill=(255, 96, 64, 255))
            if st % 2 == 0:
                d.point((rx, ry + 1), fill=(200, 50, 40, 235))
                d.point((rx - 1, ry), fill=(255, 150, 110, 235))
        else:
            d.point((rx, ry), fill=(255, 130, 100, 245))
    # LA PUNTA BLANCA-CALIENTE (el rehilete del látigo).
    tx, ty = round(22.0 + 6.3), round(18.5 + 2.8 * math.sin(9.2))
    d.point((tx, ty), fill=(255, 255, 245, 255))
    d.point((tx, ty - 1), fill=(255, 236, 200, 255))
    for dx, dy in ((-1, 0), (0, 1), (1, 0)):
        d.point((tx + dx, ty + dy), fill=(255, 220, 180, 140))
    d.point((tx + 2, ty - 3), fill=(255, 240, 210, 200))
    im.save(path)


# ----------------------------------------------------------------------
# 6. VíBORA GENESIACA — la doble hélice dorado/violeta.
# ----------------------------------------------------------------------
def vibora(path):
    im = Image.new("RGBA", (30, 30), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    baston_base(d)
    cx = 22.0
    # LA ESPINA central (la columna de la escalera).
    for k in range(0, 11, 2):
        d.point((cx, 4 + k), fill=(120, 96, 150, 120))
    # LA DOBLE HÉLICE: dos hileras de cuentas girando alrededor.
    for st in range(13):
        t = st / 12
        y = 14.0 - 10.0 * t
        ph = t * math.tau * 1.25 + 1.1
        xa = cx + 3.0 * math.cos(ph)
        xb = cx - 3.0 * math.cos(ph)
        # LOS PELDAÑOS (donde las hebras se separan al máximo).
        if abs(math.cos(ph)) > 0.86:
            d.line((round(xa), round(y), round(xb), round(y)),
                   fill=(225, 190, 255, 130), width=1)
        # LAS CUENTAS: hebra A dorada, hebra B violeta.
        d.ellipse((round(xa), round(y), round(xa) + 1, round(y) + 1),
                  fill=(255, 214, 106, 255))
        d.point((round(xa), round(y)), fill=(255, 240, 180, 255))
        d.ellipse((round(xb), round(y), round(xb) + 1, round(y) + 1),
                  fill=(190, 120, 255, 255))
        d.point((round(xb), round(y)), fill=(228, 178, 255, 255))
    # LA CABEZA arriba (la cuenta gema de la génesis).
    d.ellipse((cx - 1, 2, cx + 1, 4), fill=(255, 250, 235, 255))
    d.point((cx, 2), fill=(190, 120, 255, 255))
    im.save(path)


# ----------------------------------------------------------------------
# 7. BOA DEL ECLIPSE — la espiral que aprieta a su presa.
# ----------------------------------------------------------------------
def boa(path):
    im = Image.new("RGBA", (30, 30), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    baston_base(d)
    cx, cy = 21, 9
    # LA PRESA (el punto ámbar en el centro, con su halo de pánico).
    d.ellipse((cx - 2, cy - 2, cx + 2, cy + 2), fill=(255, 196, 92, 90))
    d.ellipse((cx - 1, cy - 1, cx, cy), fill=(255, 176, 96, 255))
    d.point((cx - 1, cy - 1), fill=(255, 244, 214, 255))
    # LA BOA ENROLLADA: el cuerpo es UN ARO BLANCO GRUESO (la vuelta
    #     completa) con la boca arriba a la derecha y la cola metida
    #     hacia dentro — el coil de la constrictora, legible).
    caja = (cx - 6, cy - 6, cx + 6, cy + 6)
    d.arc(caja, 0, 300, fill=(248, 248, 240, 255), width=2)
    d.arc(caja, 340, 360, fill=(248, 248, 240, 255), width=2)
    # EL BRILLO del lomo (el filete superior de la vuelta).
    d.arc(caja, 130, 230, fill=(255, 255, 252, 235), width=1)
    # LA COLA metiéndose hacia el centro (el extremo fino del coil).
    for k, (tx, ty) in enumerate(((24, 4), (23, 5), (23, 6))):
        d.point((tx, ty), fill=(248, 248, 240, 255 - k * 45))
    # EL ANILLO ÁMBAR DE APRIETE (el aro del eclipse apretando el nudo).
    d.ellipse((cx - 3, cy - 3, cx + 3, cy + 3),
              outline=(255, 176, 96, 240), width=1)
    d.point((cx + 3, cy - 1), fill=(255, 240, 200, 255))
    d.point((cx - 3, cy + 1), fill=(255, 240, 200, 255))
    # LA CABEZA fuera del aro (el morro con ojo y boca abierta).
    d.ellipse((26, 6, 28, 8), fill=(255, 252, 246, 255))
    d.point((27, 6), fill=(70, 60, 50, 255))
    d.point((28, 5), fill=(255, 252, 246, 235))
    im.save(path)


# ----------------------------------------------------------------------
# 8. FAROL GUARDIÁN — el farol colgante y la mano de luz.
# ----------------------------------------------------------------------
def farol(path):
    im = Image.new("RGBA", (30, 30), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    baston_base(d)
    # EL GANCHO frío colgado del engarce.
    d.line((22, 6, 23, 9), fill=(168, 188, 205, 255), width=1)
    # EL FAROL ámbar: marco + vidrio + corazón.
    d.rectangle((21, 9, 26, 15), outline=(56, 48, 34, 255), width=1)
    d.rectangle((22, 10, 25, 14), fill=(255, 196, 92, 235))
    d.rectangle((23, 11, 24, 13), fill=(255, 240, 200, 255))
    d.point((23, 12), fill=(255, 255, 240, 255))
    # LA JAULA fría (la barra del guardián).
    d.line((22, 10, 25, 14), fill=(150, 170, 185, 190), width=1)
    # LA CADENA fría descendiendo (3 eslabones en zigzag, compacta).
    for k in range(3):
        lx = 24 if k % 2 == 0 else 23
        ly = 16 + k
        d.point((lx, ly), fill=(200, 215, 230, 255))
        d.point((lx + 1, ly), fill=(110, 125, 140, 235))
    # LA MANO DE 3 DEDOS DE LUZ (la zarpa simétrica del guardián).
    d.ellipse((22, 19, 25, 21), fill=(255, 236, 180, 235))
    d.point((23, 20), fill=(255, 255, 245, 255))
    d.point((24, 20), fill=(255, 255, 245, 255))
    for fx, fy, mx, my in ((22, 20, 20, 23), (24, 21, 24, 25), (26, 20, 28, 23)):
        d.line((fx, fy, mx, my), fill=(255, 240, 170, 255), width=1)
        d.point((mx, my), fill=(255, 255, 250, 255))
        d.point((mx, my - 1), fill=(255, 226, 140, 140))
    im.save(path)


# ----------------------------------------------------------------------
# 9. CINTA AURORA — la banderola verde→violeta con broche.
# ----------------------------------------------------------------------
def cinta(path):
    im = Image.new("RGBA", (30, 30), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    baston_base(d)
    VERDE = (120, 255, 190)
    VIOLETA = (190, 120, 255)
    # LA CINTA ondeante: BANDA GRUESA de 3px en tres colores planos de
    #     aurora (menta → cian → violeta) sobre una curva Bézier que
    #     cae y remonta (la banderola volando hacia la punta).
    P0, P1, P2 = (20.0, 12.0), (26.0, 14.0), (28.0, 3.0)
    for st in range(15):
        t = st / 14
        x = (1 - t) ** 2 * P0[0] + 2 * (1 - t) * t * P1[0] + t ** 2 * P2[0]
        y = (1 - t) ** 2 * P0[1] + 2 * (1 - t) * t * P1[1] + t ** 2 * P2[1]
        rx, ry = round(x), round(y)
        if t < 0.45:
            c, c_osc, c_cl = (120, 255, 190), (70, 175, 130), (190, 255, 225)
        elif t < 0.75:
            c, c_osc, c_cl = (150, 225, 255), (85, 160, 205), (215, 240, 255)
        else:
            c, c_osc, c_cl = (190, 120, 255), (130, 70, 205), (225, 170, 255)
        d.point((rx, ry), fill=c + (255,))
        d.point((rx, ry + 1), fill=c_osc + (255,))
        d.point((rx, ry - 1), fill=c_cl + (235,))
    # LA PUNTA de la cinta (el pico final, violeta claro).
    d.point((28, 2), fill=(225, 170, 255, 255))
    # EL BROCHE dorado que sujeta la cinta al engarce.
    d.ellipse((19, 11, 21, 13), fill=(255, 214, 106, 255))
    d.point((20, 12), fill=(255, 240, 180, 255))
    # LOS POLVOS de aurora (dos, limpios).
    d.point((24, 4), fill=(150, 225, 255, 190))
    d.point((18, 15), fill=(120, 255, 190, 170))
    im.save(path)


# ----------------------------------------------------------------------
# 10. MANADA ASTRAL — tres estrellitas en cuña, la líder coronada.
# ----------------------------------------------------------------------
def manada(path):
    im = Image.new("RGBA", (30, 30), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    baston_base(d)
    AZUL = (140, 200, 255)
    OJO = (255, 214, 106)

    def estrella(scx, scy, grande=False):
        d.point((scx, scy), fill=(235, 248, 255, 255))
        for dx, dy in ((0, -1), (0, 1), (-1, 0), (1, 0)):
            d.point((scx + dx, scy + dy), fill=AZUL)
        if grande:
            for dx, dy in ((-1, -1), (1, -1), (-1, 1), (1, 1)):
                d.point((scx + dx, scy + dy), fill=(105, 168, 238, 255))

    # LA CUÑA: la líder al frente, las dos a sus espaldas.
    estrella(25, 6, grande=True)
    estrella(19, 4)
    estrella(20, 11)
    # LOS OJOS DORADOS de la manada (mirando al frente).
    d.point((26, 6), fill=OJO)
    d.point((20, 4), fill=OJO)
    d.point((21, 11), fill=OJO)
    # LA CORONA sobre la líder.
    for bx in (23, 24, 25, 26, 27):
        d.point((bx, 3), fill=(255, 214, 106, 255))
    for sx in (23, 25, 27):
        d.point((sx, 2), fill=(255, 240, 180, 255))
    # EL RASTRO estelar de la manada (dos, limpios).
    d.point((16, 8), fill=(140, 200, 255, 150))
    d.point((15, 13), fill=(140, 200, 255, 120))
    im.save(path)


# ----------------------------------------------------------------------
# 11. CRÍA ESTELAR — la sierpecita de ojos enormes y coronita.
# ----------------------------------------------------------------------
def cria(path):
    im = Image.new("RGBA", (30, 30), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    baston_base(d)
    # LA CABEZOTA redonda blanca-estelar (grande para su cuerpecito).
    d.ellipse((21, 3, 26, 8), fill=(250, 252, 255, 255))
    d.point((21, 6), fill=(226, 238, 250, 255))
    d.point((26, 5), fill=(226, 238, 250, 255))
    # LOS OJOS ENORMES cian (con su brillo).
    d.ellipse((22, 5, 23, 6), fill=(80, 215, 255, 255))
    d.ellipse((25, 5, 26, 6), fill=(80, 215, 255, 255))
    d.point((22, 5), fill=(235, 255, 255, 255))
    d.point((25, 5), fill=(235, 255, 255, 255))
    # LAS MEJILLAS rositas.
    d.point((21, 7), fill=(255, 205, 205, 235))
    d.point((26, 7), fill=(255, 205, 205, 235))
    # LA CORONITA dorada sobre la cabeza.
    for bx in (21, 22, 23, 24, 25):
        d.point((bx, 2), fill=(255, 214, 106, 255))
    for sx in (21, 23, 25):
        d.point((sx, 1), fill=(255, 240, 180, 255))
    # EL CUERPECITO en S (la colita fina hacia abajo).
    for st in range(7):
        t = st / 6
        x = 24.0 - 2.4 * t + 1.7 * math.sin(t * 5.2)
        y = 9.0 + 4.6 * t
        c = (250, 252, 255, 255) if t < 0.6 else (208, 232, 250, 200)
        d.point((round(x), round(y)), fill=c)
    im.save(path)


# ----------------------------------------------------------------------
# LA BOLSA DE LAS SIERPES — el saco de la casa, firma azul cielo.
# ----------------------------------------------------------------------
def bolsa_sierpes(path):
    im = Image.new("RGBA", (30, 30), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    rng = random.Random(63838)
    AZUL = (170, 240, 255)
    # EL SACO: el cuerpo redondeado oscuro con el brillo de la camada.
    d.ellipse((6, 9, 24, 27), fill=(30, 36, 50, 255), outline=(64, 80, 104, 255))
    d.ellipse((9, 12, 21, 24), fill=(22, 27, 38, 255))
    # EL CUELLO del saco atado.
    d.rectangle((12, 5, 18, 10), fill=(42, 50, 68, 255), outline=(74, 90, 116, 255))
    d.point((13, 5), fill=AZUL)
    d.point((17, 5), fill=AZUL)
    # LA SIERPECITA EN S bordada al frente (la firma de la camada).
    for st in range(9):
        t = st / 8
        x = 15 + 2.6 * math.sin(t * 5.6 - 1.2)
        y = 14 + 6.0 * t
        rx, ry = round(x), round(y)
        d.point((rx, ry), fill=AZUL)
        if st < 6:
            d.point((rx + 1, ry), fill=(120, 220, 245, 235))
    # LA CABECITA bordada (la cuenta mayor + el ojo).
    d.ellipse((11, 13, 12, 14), fill=(205, 248, 255, 255))
    d.point((11, 13), fill=(25, 60, 80, 255))
    # LAS CHISPAS de la camada escapando del saco.
    for k in range(6):
        a = rng.random() * math.tau
        r = 10 + rng.random() * 3
        x = 15 + math.cos(a) * r
        y = 18 + math.sin(a) * r * 0.9
        d.point((round(x), round(y)), fill=AZUL[:3] + (170,))
    im.save(path)


# ----------------------------------------------------------------------
# EL BUFF DE LA CRÍA (32×32) — la cabezota feliz de la camada.
# ----------------------------------------------------------------------
def cria_buff(path):
    im = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    rng = random.Random(63839)
    # LA CABEZOTA redonda blanca-estelar (llena el icono — es la cría).
    d.ellipse((8, 10, 24, 26), fill=(246, 250, 255, 255),
              outline=(160, 180, 210, 255), width=1)
    # el brillo de arriba-izquierda (el volumen de la casa).
    d.arc((8, 10, 24, 26), 190, 300, fill=(255, 255, 255, 255), width=1)
    # LOS OJOS ENORMES cian (el sello de la cría).
    d.ellipse((10, 15, 14, 21), fill=(80, 215, 255, 255),
              outline=(30, 130, 200, 255), width=1)
    d.ellipse((18, 15, 22, 21), fill=(80, 215, 255, 255),
              outline=(30, 130, 200, 255), width=1)
    # los brillos de los ojos.
    d.rectangle((11, 16, 12, 17), fill=(240, 252, 255, 255))
    d.rectangle((19, 16, 20, 17), fill=(240, 252, 255, 255))
    # LA BOQUITA sonriente.
    d.arc((13, 19, 19, 24), 20, 160, fill=(110, 130, 160, 255), width=1)
    # LAS MEJILLAS rositas.
    d.point((9, 19), fill=(255, 205, 205, 235))
    d.point((23, 19), fill=(255, 205, 205, 235))
    # LA CORONITA dorada (5 puntas — la hermana pequeña de las coronas).
    d.rectangle((9, 8, 23, 9), fill=(255, 214, 106, 255))
    d.point((9, 9), fill=(218, 165, 60, 255))
    for sx in (10, 13, 16, 19, 22):
        d.point((sx, 6), fill=(255, 240, 180, 255))
        d.point((sx - 1, 7), fill=(255, 224, 140, 255))
        d.point((sx + 1, 7), fill=(255, 224, 140, 255))
    d.point((16, 5), fill=(255, 252, 230, 255))
    # LOS POLVOS estrellares alrededor (deterministas).
    for k in range(5):
        a = rng.random() * math.tau
        r = 12 + rng.random() * 2
        x, y = 16 + math.cos(a) * r, 18 + math.sin(a) * r
        rx, ry = round(x), round(y)
        d.point((rx, ry), fill=(200, 240, 255, 200))
        d.point((rx, ry - 1), fill=(200, 240, 255, 120))
    im.save(path)


# ----------------------------------------------------------------------
# EL GLOW DE LOS PROYECTILES (76×76 — la librería SierpesLib dibuja encima).
# ----------------------------------------------------------------------
def glow_proyectil(path, rgb):
    im = Image.new("RGBA", (76, 76), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    cx = cy = 38
    for r in range(34, 0, -1):
        f = (1 - r / 34) ** 1.6
        d.ellipse((cx - r, cy - r, cx + r, cy + r),
                  fill=(int(rgb[0] * f), int(rgb[1] * f), int(rgb[2] * f), int(150 * f)))
    im.save(path)


# ----------------------------------------------------------------------
# LA HOJA DE CONTACTO 4× (para la verificación VLM — no es asset del mod).
# ----------------------------------------------------------------------
def hoja_contacto(salida, rutas):
    from PIL import ImageFont
    celda = 120
    cols, filas = 4, 3
    hoja = Image.new("RGBA", (cols * celda, filas * celda), (36, 40, 48, 255))
    d = ImageDraw.Draw(hoja)
    try:
        font = ImageFont.load_default(30)
    except TypeError:
        font = ImageFont.load_default()
    for idx, ruta in enumerate(rutas):
        fx, fy = (idx % cols) * celda, (idx // cols) * celda
        icono = Image.open(ruta).convert("RGBA").resize((celda, celda), Image.NEAREST)
        hoja.paste(icono, (fx, fy), icono)
        d.rectangle((fx + 2, fy + 2, fx + 34, fy + 34), fill=(180, 30, 30, 220))
        d.text((fx + 8, fy + 2), str(idx + 1), fill=(255, 255, 255, 255), font=font)
        d.rectangle((fx, fy, fx + celda - 1, fy + celda - 1),
                    outline=(80, 86, 96, 255), width=1)
    hoja.save(salida)


# ----------------------------------------------------------------------
if __name__ == "__main__":
    staffs = [
        (ouroboros, "OuroborosAstralStaff.png"),
        (caravana, "CaravanaEspectralStaff.png"),
        (anguila, "AnguilaSolarStaff.png"),
        (cienpies, "CienpiesRunicoStaff.png"),
        (flagelo, "FlageloEstelarStaff.png"),
        (vibora, "ViboraGenesiacaStaff.png"),
        (boa, "BoaEclipseStaff.png"),
        (farol, "FarolGuardianStaff.png"),
        (cinta, "CintaAuroraStaff.png"),
        (manada, "ManadaAstralStaff.png"),
        (cria, "CriaEstelarStaff.png"),
    ]
    for fun, nombre in staffs:
        fun(os.path.join(W, nombre))

    proyectiles = [
        ("OuroborosAstralProjectile.png", (255, 208, 120)),
        ("CaravanaEspectralProjectile.png", (140, 255, 220)),
        ("AnguilaSolarProjectile.png", (255, 236, 120)),
        ("CienpiesRunicoProjectile.png", (255, 176, 96)),
        ("FlageloEstelarProjectile.png", (255, 96, 64)),
        ("ViboraGenesiacaProjectile.png", (190, 120, 255)),
        ("BoaEclipseProjectile.png", (240, 240, 230)),
        ("FarolGuardianProjectile.png", (255, 196, 92)),
        ("CintaAuroraProjectile.png", (120, 255, 190)),
        ("ManadaAstralProjectile.png", (140, 200, 255)),
        ("CriaEstelarMinion.png", (200, 240, 255)),
    ]
    for nombre, rgb in proyectiles:
        glow_proyectil(os.path.join(P, nombre), rgb)

    bolsa_sierpes(os.path.join(I, "BolsaSierpes.png"))
    cria_buff(os.path.join(F, "CriaEstelarBuff.png"))

    print("24 PNGs generados")

    # La hoja de contacto para el VLM (11 bastones + la bolsa).
    rutas = [os.path.join(W, n) for _, n in staffs] + [os.path.join(I, "BolsaSierpes.png")]
    hoja_contacto(os.path.join(BASE, "tools", "v638_hoja_contacto.png"), rutas)
    print("Hoja de contacto: tools/v638_hoja_contacto.png")
