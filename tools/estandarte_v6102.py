#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
estandarte_v6102.py — v6.102 — EL VESTIDO NUEVO DEL ESTANDARTE.

LA LETRA DEL USUARIO: «el efecto sobre el enemigo quedo bien en el
banner pero los vfx no, crea otro banner con algo que me dio una IA
[…] adapta el codigo y lo que dijo» — la receta del calentamiento
aerodinámico (las 4 capas), adaptada a la energía MORADA Y NEGRA del
vacío exterior.

LAS DOS TEXTURAS QUE LA RECETA PIDE (adaptadas a la paleta del
estandarte — BLANCAS: el tinte morado se aplica al dibujar, como
GlowCircleWhite con el que la casa ya tiñe):

  Content/Effects/CampoVacio.png (256×256) — LA CAPA 1, EL AURA BASE
      (el «campo térmico» de la receta: la textura circular
      semitransparente que sigue al jugador — el escudo visible).
      Anular: centro TRANSPARENTE (la zona del jugador queda limpia),
      banda brillante cuyo borde INTERNO (el que mira al portador)
      arde más, y la frontera externa ONDULANDO (la turbulencia de la
      compresión) hasta morir en transparente.

  Content/Effects/FrenteVacio.png (256×32) — LA CAPA 2, EL FRENTE DE
      COMPRESIÓN (el «cono de aire caliente» entre el jugador y cada
      enemigo): el rectángulo largo con degradado horizontal DENSO a
      la izquierda (el origen, en el portador) → DISPERSO a la
      derecha (hacia la presa), con las VETAS de turbulencia a lo
      largo del flujo y la sección lente (fina arriba y abajo).

TODO con transparencia NATIVA (el lienzo nace alfa=0 — la regla
permanente de la casa: la transparencia vive en la GENERACIÓN).
"""
import math
import os

from PIL import Image

AQUI = os.path.dirname(os.path.abspath(__file__))
BASE = os.path.join(AQUI, '..', 'AethonMod')


def ss(t, a, b):
    """El smoothstep de la casa."""
    if t <= a:
        return 0.0
    if t >= b:
        return 1.0
    k = (t - a) / (b - a)
    return k * k * (3.0 - 2.0 * k)


def campo():
    """EL CAMPO TÉRMICO (256×256, blanco tintable): el anillo del
    vacío comprimiéndose. Centro transparente (r<0.30 — el jugador
    respira), cresta que sube (0.30→0.52), la meseta (0.52→0.72) y la
    caída (0.72→1) con la FRONTERA ONDULADA — el borde externo se
    adelanta y se atrasa con la ondulación de la compresión (dos
    senos inconmensurables en θ: la frontera NUNCA se repite igual)."""
    W = H = 256
    im = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    px = im.load()
    for y in range(H):
        for x in range(W):
            dx, dy = x - 127.5, y - 127.5
            r = math.hypot(dx, dy) / 128.0
            if r >= 1.0:
                continue
            th = math.atan2(dy, dx)

            # LA FRONTERA EXTERNA ONDULADA — la turbulencia de la
            # compresión: la caída empieza más lejos o más cerca según
            # el ángulo (nunca se repite: 7 y 13 son inconmensurables)
            caida = 0.72 + 0.045 * math.sin(th * 7.0 + 1.7) \
                    + 0.030 * math.sin(th * 13.0 - 0.6)

            # EL PERFIL: nada en el centro, cresta, meseta, caída
            if r < 0.30:
                continue
            subida = ss(r, 0.30, 0.52)
            meseta = 1.0 - ss(r, caida, 1.0)
            a = subida * meseta
            if a <= 0.01:
                continue

            # EL BRILLO — el borde INTERNO arde más (la cara que mira
            # al portador: ahí nace la compresión) y la meseta lleva
            # UNA VETA FINA (la cresta del campo — el eco de la frontera)
            caliente = 1.0 - ss(r, 0.34, 0.50)          # 1 en el borde interno
            brillo = int(168 + 87 * caliente)
            veta = math.exp(-((r - 0.585) / 0.045) ** 2) * 46
            brillo = min(255, brillo + int(veta))

            px[x, y] = (brillo, brillo, brillo, int(255 * a))
    im.save(os.path.join(BASE, 'Content', 'Effects', 'CampoVacio.png'))
    return im


def _hash01(x, y):
    """El azar determinista de la casa (el de los .cs, en Python)."""
    v = math.sin(x * 12.9898 + y * 78.233) * 43758.547
    return v - math.floor(v)


def _ruido2d(u, v, escala=0.055):
    """EL RUIDO DE VALOR 2D (la turbulencia de verdad): dos octavas
    del hash determinista interpoladas — el moteado que sobrevive el
    estirado (una sinusoide sola se aplana a LÍNEA pura; el ruido
    2D sigue siendo MASA a cualquier escala)."""
    def celda(cu, cv):
        return _hash01(math.floor(cu), math.floor(cv))
    su = u / escala
    sv = v / (escala * 8.0)          # el lienzo es 8× más largo que alto
    x0, y0 = math.floor(su), math.floor(sv)
    fx = ss(su - x0, 0.0, 1.0) if (su - x0) > 0 else 0.0
    fy = ss(sv - y0, 0.0, 1.0) if (sv - y0) > 0 else 0.0
    n00 = celda(x0, y0)
    n10 = celda(x0 + 1, y0)
    n01 = celda(x0, y0 + 1)
    n11 = celda(x0 + 1, y0 + 1)
    a1 = n00 + (n10 - n00) * fx
    a2 = n01 + (n11 - n01) * fx
    grueso = a1 + (a2 - a1) * fy
    fino = _hash01(round(u * 380), round(v * 47))
    return grueso * 0.72 + fino * 0.28


def frente():
    """EL FRENTE DE COMPRESIÓN (256×32, blanco tintable): la estela
    DENSA en el origen (u=0, el portador) que se dispersa hacia la
    presa (u=1). r4 — LA TURBULENCIA DE VERDAD (EL OJO: «falta
    turbulencia, ruido de vetas curvas»): las vetas ya no son senos
    paralelos sino FILAMENTOS EN S (curvados por el ruido) sobre el
    moteado 2D, con CANALES que se abren entre ellos (donde el ruido
    baja, la estela se PARTE en hebras) y el jaspeado de brillo a lo
    largo de la traza."""
    W, H = 256, 32
    im = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    px = im.load()
    for y in range(H):
        v = y / (H - 1)
        # LA SECCIÓN LENTE — r6 (EL OJO: «engrosar ~2×»): exponente 1.2
        # — la lente ANCHA (el -6 dB vive en |v-0.5|<0.44: casi toda la
        # altura del lienzo es cuerpo; el resto lo engorda el motor con
        # su doble pasada y su escala Y por cercanía)
        taper = 1.0 - (abs(v - 0.5) * 2.0) ** 1.2
        for x in range(W):
            u = x / (W - 1)
            # EL DEGRADADO — denso a la izquierda (el origen arde);
            # r5 (EL OJO: «reforzar el tercio derecho»): la cola no
            # muere en cero — un 10% de piso mantiene las vetas vivas
            # hasta la presa (el destello de la presa lo tapa al final)
            base = 0.10 + 0.84 * (1.0 - u) ** 1.4
            # EL RUIDO 2D — el moteado que sobrevive el estirado
            n2 = _ruido2d(u, v)
            # LAS VETAS EN S — curvadas por el ruido (no senos
            # paralelos: filamentos que se retuercen a lo largo)
            freq = 2.6 + 1.6 * u
            fase = v * math.pi * 2 * freq + u * 9.0 + 1.3 * n2
            # r6 — LAS VETAS GORDAS y la OPACIDAD PLENA (EL OJO: «más
            # opacas, más contraste»): piso 0.50, canales 0.62 y el
            # multiplicador general ×1.15 — en el origen la estela
            # arde CASI sólida y se abre en hebras hacia la presa
            vetas = 0.50 + 0.50 * math.sin(fase)
            vetas *= 0.70 + 0.30 * math.sin(v * math.pi * 2 * (freq * 2.7) - u * 5.0 + 0.9 * n2)
            canal = 0.62 + 0.38 * n2
            a = base * taper * max(0.0, vetas) * canal * 1.15
            a = min(1.0, a)
            if a <= 0.012:
                continue
            # EL BRILLO JASPEADO — moteado a lo largo de la traza (el
            # origen casi blanco; la presa la pinta su propia capa)
            brillo = int(255 - 74 * u + 36 * (n2 - 0.5))
            brillo = max(140, min(255, brillo))
            px[x, y] = (brillo, brillo, brillo, int(255 * a))
    im.save(os.path.join(BASE, 'Content', 'Effects', 'FrenteVacio.png'))
    return im


def el_montaje(a, b):
    """La hoja para EL OJO: ambas texturas ×2 sobre el fondo del juego
    (24,24,34), el campo con su tinte morado de verdad (una vista
    tintada como se ve en juego) y el frente estirado como lo estira
    el motor (una tirada de 300 px de larga)."""
    from PIL import Image as I
    tint = I.new('RGBA', a.size, (0, 0, 0, 0))
    ta = tint.load()
    pa = a.load()
    for y in range(a.height):
        for x in range(a.width):
            r, g, bb, al = pa[x, y]
            if al:
                # EL TINTE MORADO del juego: violeta (185,110,255)
                ta[x, y] = (int(r * 185 / 255), int(g * 110 / 255),
                            int(bb * 255 / 255), al)
    piezas = [
        ('campo ×2', a.resize((512, 512), I.NEAREST)),
        ('campo TINTADO ×2 (como en juego)', tint.resize((512, 512), I.NEAREST)),
        ('frente ×2', b.resize((512, 64), I.NEAREST)),
        ('frente ESTIRADO (300 px, como el motor)', b.resize((600, 56), I.BILINEAR)),
    ]
    ancho = sum(p.width for _, p in piezas) + 20 * (len(piezas) + 1)
    alto = max(p.height for _, p in piezas) + 30
    hoja = I.new('RGBA', (ancho, alto), (24, 24, 34, 255))
    dx = 20
    for _, p in piezas:
        hoja.paste(p, (dx, 22), p)
        dx += p.width + 20
    hoja.save('/tmp/estandarte_qa.png')
    print('  EL MONTAJE: /tmp/estandarte_qa.png')


if __name__ == '__main__':
    a = campo()
    b = frente()
    print(f'CampoVacio {a.size} · FrenteVacio {b.size}')
    el_montaje(a, b)
    print('EL OJO de la casa:  node tools/vlm_estandarte_v6102.mjs')
