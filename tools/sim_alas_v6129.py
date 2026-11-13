#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
sim_alas_v6129.py — EL SIM 1:1 DEL ALA LISA DEL VACÍO (la letra de la
sesión 10: «las alas siguen con picos, diente de sierra, supongo que esos
dientes de sierra son supuestas plumas, esas plumas las puedes eliminar»).

El puerto 1:1 del dibujado de AlasAuroraLib para el estilo Runas del
Vacío (el catálogo: membrana 1, redonda 1, runas 2 — y AHORA lisas true):

  · ConstruirPluma — el eje de 9 puntos y los semianchos con el Perfil
    (aguja/plectro) EXACTOS, el fundido y el barrido de siempre.
  · EmitirCapa ×3 — LAS TIRAS (núcleo 0.78·0.3 · cuerpo 0.42·0.62 ·
    halo 0.15·1.0) con la muerte de la aguja v6.127 (el perfil muere
    DENTRO en el último tramo) — SOLO para el modo CON plumas.
  · EmitirMembrana — LA VELA (los quads entre ejes vecinos, la vela
    v6.129: 0.80→1.30 ×CuerpoInten para la lisa · 0.55→1.17 la de
    siempre) + EL BORDE MAESTRO (la Catmull-Rom C1 por las cinco
    puntas con su banda y su labio — brBorde tope 1.05).
  · El blend soft-add de la casa: dst += rgb·(1−dst) — la aditiva
    real (el orden de las capas es irrelevante, como en el juego).
  · Rasterización baricéntrica de los quads con interpolación del
    color de los vértices.

LAS MEDIDAS (las dos caras de la sierra):
  A · EL CONTORNO PERCIBIDO — para cada ángulo alrededor del cuerpo,
      el radio exterior donde el brillo cruza el umbral del ojo: el
      ZIGZAG (máx−mín sobre el arco del ala) y el NÚMERO DE PICOS
      locales del contorno (dientes = picos).
  B · LOS RAYOS INTERIORES — la luminancia a lo largo de un arco a
      media distancia del ala: el NÚMERO DE MÁXIMOS LOCALES con
      prominencia (las 5 plumas = 5 rayos radiales brillantes).
  C · LA SIMETRÍA DEL PAR — Δ entre el ala izquierda y la derecha
      (el espejo de la v6.56.4: debe ser ~0 px).

Salida: tools/sim_alas_v6129_con_plumas.png · tools/sim_alas_v6129_lisa.png
"""
import math
import numpy as np

# =====================================================================
#  EL ESTILO — Runas del Vacío (el catálogo, BYTE a BYTE)
# =====================================================================
ESTILO = dict(
    plumas=5, largo_int=90.0, largo_ext=52.0, raiz=3.5,
    punta_base=20.0, punta_abierta=14.0,
    plegado_min=0.5, plegado_max=0.92, abierto_min=0.22, abierto_max=1.54,
    inclinacion=0.12, barrido=0.14, curva=0.18, anclaje_sep=5.0,
    vientre=0.45, afilada=0.5, filosa=0.0,
    hue_entre=0.085, hue_largo=0.16,
    nucleo_inten=0.78, nucleo_frac=0.30,
    cuerpo_inten=0.42, cuerpo_frac=0.62,
    halo_inten=0.15,
    paleta=[(150, 84, 235), (122, 78, 188), (26, 14, 44)],
    membrana=1.0, redonda=1.0,
)

SS = 2                     # supersampling
CX, CY = 150.0, 150.0      # el cuerpo (MountedCenter)
ANCHO, ALTO = 300, 300     # el lienzo del sim
UMBRAL = 0.10              # el umbral del ojo (brillo percibido)


def clamp(x, a, b):
    return a if x < a else (b if x > b else x)


def lerp(a, b, t):
    return a + (b - a) * t


def suave(e0, e1, x):
    t = clamp((x - e0) / max(e1 - e0, 1e-4), 0.0, 1.0)
    return t * t * (3.0 - 2.0 * t)


# =====================================================================
#  EL TONO — TonoPluma/DePaleta (el viaje de hue + el blanco)
# =====================================================================
def de_paleta(h):
    paleta = ESTILO["paleta"]
    h -= math.floor(h)
    num = h * len(paleta)
    i0 = int(num) % len(paleta)
    i1 = (i0 + 1) % len(paleta)
    t = num - int(num)
    c0, c1 = paleta[i0], paleta[i1]
    return tuple(lerp(c0[k], c1[k], t) / 255.0 for k in range(3))


def tono_pluma(hue, hacia_blanco):
    c = de_paleta(hue)
    return tuple(lerp(c[k], 1.0, hacia_blanco) for k in range(3))


# =====================================================================
#  ConstruirPluma — el puerto EXACTO (spread 1, flap 0, noche)
# =====================================================================
def perfil(q, raiz, vientre, tope, afilada, redonda):
    if q < vientre:
        t = q / vientre
        return lerp(raiz, tope, math.sin(t * math.pi / 2) ** 0.8)
    t = (q - vientre) / max(1.0 - vientre, 0.001)
    aguja = (1.0 - t) ** afilada
    plectro = max(0.0, math.cos(t * math.pi * 0.5)) ** 0.62
    return tope * lerp(aguja, plectro, redonda)


def construir_pluma(ancla, i, lado, fundido):
    n = ESTILO["plumas"]
    num2 = i / (n - 1)
    num3 = lerp(ESTILO["plegado_min"], ESTILO["plegado_max"], num2)
    num4 = lerp(ESTILO["abierto_min"], ESTILO["abierto_max"], num2)
    num5 = lerp(num3, num4, 1.0)          # spread 1
    num5 += math.sin(0.0 - i * 0.45) * (0.05 + 0.11)  # flap 0
    num7 = -math.pi / 2 + lado * (ESTILO["inclinacion"] + num5)
    vx, vy = math.cos(num7), math.sin(num7)
    px, py = -vy, vx                       # la perpendicular
    largo = lerp(ESTILO["largo_int"], ESTILO["largo_ext"], num2) * 1.0
    vientre = clamp(ESTILO["vientre"], 0.15, 0.7)
    afilada = clamp(ESTILO["afilada"], 0.35, 2.5)
    filosa = clamp(ESTILO["filosa"], 0.0, 1.0)
    redonda = clamp(ESTILO["redonda"], 0.0, 1.0)
    num11 = ESTILO["punta_base"] + ESTILO["punta_abierta"] * 1.0
    vientre2 = vientre * (1.0 - 0.45 * filosa)
    ancho = num11 * (1.0 + 0.18 * filosa)
    ancho2 = num11 * (1.0 - 0.3 * filosa)
    eje, semi_mas, semi_menos = [], [], []
    for j in range(9):
        q = j / 8.0
        x = ancla[0] + vx * (largo * q) + px * (ESTILO["curva"] * lado * largo * q * q)
        y = ancla[1] + vy * (largo * q) + py * (ESTILO["curva"] * lado * largo * q * q)
        n14 = perfil(q, ESTILO["raiz"], vientre, ancho, afilada, redonda)
        n15 = perfil(q, ESTILO["raiz"], vientre2, ancho2, afilada, redonda)
        if q > 0.7:
            n16 = (q - 0.7) / 0.3
            n17 = lerp(1.0, 0.25 + 0.75 * fundido, n16 * n16)
            n14 *= n17
            n15 *= n17
        eje.append((x, y))
        if lado < 0:
            semi_mas.append(max(n15, 0.3))
            semi_menos.append(max(n14, 0.3))
        else:
            semi_mas.append(max(n14, 0.3))
            semi_menos.append(max(n15, 0.3))
    return eje, semi_mas, semi_menos, largo


# =====================================================================
#  LA GEOMETRÍA DEL PAR (Dibujar: anclas, lados, espejo direction=1)
# =====================================================================
def construir_par():
    val = (CX, CY)
    val2 = (CX, CY - 4.0)
    por_lado = ESTILO["plumas"]
    plumas = [None] * (por_lado * 2)
    hues = [0.0] * (por_lado * 2)
    fundidos = [1.0] * (por_lado * 2)
    for lado in (-1, 1):
        ancla_lado = (val2[0] + lado * ESTILO["anclaje_sep"], val2[1])
        b = 0 if lado < 0 else por_lado
        for j in range(por_lado):
            eje, sm, sn, _ = construir_pluma(ancla_lado, j, lado, 1.0)
            plumas[b + j] = dict(eje=eje, semi_mas=sm, semi_menos=sn)
            hues[b + j] = (j * ESTILO["hue_entre"]) % 1.0
    return plumas, hues, fundidos, val2, por_lado


# =====================================================================
#  LA VELA ESTIRADA (EstirarVelaLisa v6.129 — el puerto EXACTO)
#
#  EL BORDE PARAMETRIZADO POR EL CAMINO u ∈ [-U_NACE, 1+U_NACE]:
#    u=0 → LA CÚSPIDE (la pluma 0: la más larga) · u=1 → LA TENDIDA
#    (la pluma 4) · u=±U_NACE → LOS NACIMIENTOS junto al cuerpo.
#    R(u): la función SUAVE por tramos (smoothstep — SIN Catmull: los
#    puntos desiguales hacían overshoot y la curva ONDULABA = sierra).
#    θ(u): el ángulo interp lineal en [0,1] y el cierre hacia el cuerpo
#    en los nacimientos. TODO por CONSTRUCCIÓN (el orden garantizado,
#    sin ambigüedades de atan2 ni envolturas por ±π).
# =====================================================================
PAD, RNACE, DTH = 12.0, 16.0, 0.35
U_NACE = 0.35
CENTRO = None  # el _geoAnclaCentral (se fija en main)


def camino_borde(plumas, b, por_lado):
    """El camino del borde de UN lado: devuelve (P, u_de_pluma)."""
    def th_de(p):
        return math.atan2(p[1] - CENTRO[1], p[0] - CENTRO[0])
    def r_de(p):
        return math.hypot(p[0] - CENTRO[0], p[1] - CENTRO[1])
    p0 = plumas[b]["eje"][8]              # la cúspide (pluma 0)
    p4 = plumas[b + por_lado - 1]["eje"][8]  # la tendida (pluma 4)
    th0 = th_de(p0)
    th4 = th_de(p4)
    # el des-envoltura: th4 al entorno corto de th0 (el arco del abanico < π)
    while th4 > th0 + math.pi:
        th4 -= 2 * math.pi
    while th4 <= th0 - math.pi:
        th4 += 2 * math.pi
    r0 = r_de(p0) + PAD
    r4 = r_de(p4) + PAD
    signo = 1.0 if th4 >= th0 else -1.0
    th_nace_sup = th0 - signo * DTH
    th_nace_inf = th4 + signo * DTH

    def theta(u):
        if u < 0.0:
            return lerp(th0, th_nace_sup, -u / U_NACE)
        if u > 1.0:
            return lerp(th4, th_nace_inf, (u - 1.0) / U_NACE)
        return lerp(th0, th4, u)

    def radio(u):
        if u < 0.0:
            return lerp(RNACE, r0, suave(0.0, 1.0, (u + U_NACE) / U_NACE))
        if u > 1.0:
            return lerp(r4, RNACE, suave(0.0, 1.0, (u - 1.0) / U_NACE))
        return lerp(r0, r4, suave(0.0, 1.0, u))

    def P(u):
        th = theta(u)
        r = radio(u)
        return (CENTRO[0] + math.cos(th) * r, CENTRO[1] + math.sin(th) * r)

    # la fracción u de cada pluma (su ángulo des-envuelto al camino)
    us = []
    for k in range(por_lado):
        thk = th_de(plumas[b + k]["eje"][8])
        while thk > th0 + math.pi:
            thk -= 2 * math.pi
        while thk <= th0 - math.pi:
            thk += 2 * math.pi
        us.append(min(1.0, max(0.0, (thk - th0) / (th4 - th0) if abs(th4 - th0) > 1e-4 else 0.0)))
    return P, us


def estirar_vela_lisa(plumas, hues, fundidos, por_lado, val2):
    nuevo = por_lado + 2
    out = [None] * (nuevo * 2)
    for lado in (-1, 1):
        b_viejo = 0 if lado < 0 else por_lado
        b_nuevo = 0 if lado < 0 else nuevo
        ancla = (val2[0] + lado * ESTILO["anclaje_sep"], val2[1])
        P, us = camino_borde(plumas, b_viejo, por_lado)

        ejes = []
        for j in range(9):
            q = j / 8.0
            fila = []
            # EL NACIMIENTO SUPERIOR: del ancla al punto P(-U_NACE)
            n_sup = P(-U_NACE)
            fila.append((lerp(ancla[0], n_sup[0], q), lerp(ancla[1], n_sup[1], q)))
            # LAS PLUMAS ESTIRADAS: el tramo final corre a SU punto del borde
            for k in range(por_lado):
                e = plumas[b_viejo + k]["eje"][j]
                punta = plumas[b_viejo + k]["eje"][8]
                dest = P(us[k])
                d = (dest[0] - punta[0], dest[1] - punta[1])
                dk = math.hypot(d[0], d[1])
                dirk = plumas[b_viejo + k]["eje"][8][0] - ancla[0], plumas[b_viejo + k]["eje"][8][1] - ancla[1]
                ld = math.hypot(*dirk)
                dirk = (dirk[0] / ld, dirk[1] / ld) if ld > 0.001 else (0.0, -1.0)
                s = (d[0] * dirk[0] + d[1] * dirk[1]) * suave(0.45, 1.0, q)
                fila.append((e[0] + dirk[0] * s, e[1] + dirk[1] * s))
            # EL NACIMIENTO INFERIOR: del ancla al punto P(1+U_NACE)
            n_inf = P(1.0 + U_NACE)
            fila.append((lerp(ancla[0], n_inf[0], q), lerp(ancla[1], n_inf[1], q)))
            ejes.append(fila)
        for k in range(nuevo):
            eje = [ejes[j][k] for j in range(9)]
            vecino = min(max(k - 1, 0), por_lado - 1)
            out[b_nuevo + k] = dict(eje=eje,
                                     hue=hues[b_viejo + vecino],
                                     fundido=fundidos[b_viejo + vecino])
    return out, nuevo * 2


# =====================================================================
#  LA VELA + EL BORDE MAESTRO (EmitirMembrana v6.129)
# =====================================================================
def capa_vela(plumas, hues, fundidos, por_lado, lisa):
    quads = []
    fuerza = ESTILO["membrana"]
    cuerpo = ESTILO["cuerpo_inten"]
    base_v = 0.80 if lisa else 0.55
    tope_v = 1.30 if lisa else 0.62
    for lado in (0, 1):
        b = lado * por_lado
        for i in range(por_lado - 1):
            pa, pb = plumas[b + i], plumas[b + i + 1]
            fundido = (fundidos[b + i] + fundidos[b + i + 1]) * 0.5
            hue_medio = (hues[b + i] + hues[b + i + 1]) * 0.5
            for j in range(8):
                q0, q1 = j / 8.0, (j + 1) / 8.0
                s0, s1 = suave(0.35, 0.95, q0), suave(0.35, 0.95, q1)
                i0 = (base_v + tope_v * s0) * cuerpo * fuerza * fundido
                i1 = (base_v + tope_v * s1) * cuerpo * fuerza * fundido
                hb0, hb1 = 0.05 + 0.38 * s0, 0.05 + 0.38 * s1
                if lisa:
                    # v6.129 — LA RAMPA RADIAL (sin franjas): vivo (hue 0) → hondo PURO (hue 1/3)
                    hue0l = (1.0 / 3.0) * suave(0.0, 0.85, q0)
                    hue1l = (1.0 / 3.0) * suave(0.0, 0.85, q1)
                else:
                    hue0l = hue_medio + ESTILO["hue_largo"] * q0
                    hue1l = hue_medio + ESTILO["hue_largo"] * q1
                c0 = (i0,) + tono_pluma(hue0l, hb0)
                c1 = (i1,) + tono_pluma(hue1l, hb1)
                quads.append((pa["eje"][j], pb["eje"][j], pb["eje"][j + 1], pa["eje"][j + 1], c0, c0, c1, c1))
    return quads


def catmull(p0, p1, p2, p3, t):
    t2, t3 = t * t, t * t * t
    return tuple(0.5 * (2 * p1[k] + (p2[k] - p0[k]) * t +
                        (2 * p0[k] - 5 * p1[k] + 4 * p2[k] - p3[k]) * t2 +
                        (3 * p1[k] - p0[k] - 3 * p2[k] + p3[k]) * t3)
                 for k in range(2))


def capa_borde(plumas, hues, fundidos, por_lado, ancla_central, lisa_borde=False):
    quads = []
    fuerza = ESTILO["membrana"]
    br_borde = clamp(ESTILO["cuerpo_inten"] * 1.55 * 1.5, 0.30, 1.05)
    grosor, labio = 12.0 + 4.0 * fuerza, 2.5 * fuerza
    rim = 0.20
    respiracion = 1.0
    for lado in (0, 1):
        b = lado * por_lado
        m = por_lado
        muestras = (m - 1) * 6
        # v6.129 — EL BORDE MUESTREADO (el ala lisa): la FUNCIÓN del
        # camino u ∈ [-U_NACE, 1+U_NACE] (nacimiento→cúspide→tendida→
        # nacimiento) evaluada en u EQUIESPACIADO — SIN Catmull (con los
        # puntos desiguales la Catmull hacía OVERSHOOT: la sierra ±25 px).
        P_l = None
        if lisa_borde:
            P_l, _ = camino_borde(plumas, b, m)
        for s in range(muestras):
            if lisa_borde:
                u0 = -U_NACE + (1.0 + 2.0 * U_NACE) * s / muestras
                u1 = -U_NACE + (1.0 + 2.0 * U_NACE) * (s + 1) / muestras
                p0 = P_l(u0)
                p1 = P_l(u1)
                seg = min(m - 2, int(u0 / (1.0 + 2.0 * U_NACE) * (m - 1)))
            else:
                seg = s // 6
                k0, k1 = (s % 6) / 6.0, (s % 6 + 1) / 6.0
                q0p = plumas[b + seg - 1]["eje"][8] if seg > 0 else plumas[b]["eje"][8]
                q1p = plumas[b + seg]["eje"][8]
                q2p = plumas[b + seg + 1]["eje"][8]
                q3p = plumas[b + seg + 2]["eje"][8] if seg + 2 < m else plumas[b + m - 1]["eje"][8]
                p0 = catmull(q0p, q1p, q2p, q3p, k0)
                p1 = catmull(q0p, q1p, q2p, q3p, k1)
            f0 = s / muestras if lisa_borde else (seg + (s % 6) / 6.0) / (m - 1)
            f1 = (s + 1) / muestras if lisa_borde else (seg + (s % 6 + 1) / 6.0) / (m - 1)
            ia0 = min(m - 1, int(f0 * (m - 1) + 0.5))
            ia1 = min(m - 1, int(f1 * (m - 1) + 0.5))
            hue0, hue1 = hues[b + ia0], hues[b + ia1]
            if lisa_borde:
                hue0 = hue1 = 1.0 / 3.0   # v6.129 — el borde liso: UN tono (el hondo puro)
            fund = (fundidos[b + ia0] + fundidos[b + ia1]) * 0.5

            def radial(p):
                rx, ry = p[0] - ancla_central[0], p[1] - ancla_central[1]
                l = math.hypot(rx, ry)
                if l < 1e-3:
                    return (0.0, -1.0)
                return (rx / l, ry / l)

            r0, r1 = radial(p0), radial(p1)
            in0 = (p0[0] - r0[0] * grosor, p0[1] - r0[1] * grosor)
            in1 = (p1[0] - r1[0] * grosor, p1[1] - r1[1] * grosor)
            out0 = (p0[0] + r0[0] * labio, p0[1] + r0[1] * labio)
            out1 = (p1[0] + r1[0] * labio, p1[1] + r1[1] * labio)
            # LA BANDA
            bi0 = (br_borde * 0.55 * fuerza * fund,) + tono_pluma(hue0, 0.18 + rim * 0.6)
            bi1 = (br_borde * 0.55 * fuerza * fund,) + tono_pluma(hue1, 0.18 + rim * 0.6)
            bo0 = (br_borde * 0.95 * fuerza * fund * respiracion,) + tono_pluma(hue0, 0.28 + rim)
            bo1 = (br_borde * 0.95 * fuerza * fund * respiracion,) + tono_pluma(hue1, 0.28 + rim)
            quads.append((in0, in1, p1, p0, bi0, bi1, bo1, bo0))
            # EL LABIO
            lo0 = (br_borde * 1.05 * fuerza * fund,) + tono_pluma(hue0, 0.42 + rim)
            lo1 = (br_borde * 1.05 * fuerza * fund,) + tono_pluma(hue1, 0.42 + rim)
            lx = (0.0,) + tono_pluma(hue0, 0.42 + rim)
            quads.append((p0, p1, out1, out0, lo0, lo1, lx, lx))
    return quads


# =====================================================================
#  LAS TIRAS (EmitirCapa ×3 — SOLO para el modo CON plumas)
# =====================================================================
def capas_tiras(plumas, hues, fundidos, n_total):
    quads = []
    capas = [
        (ESTILO["nucleo_frac"], ESTILO["nucleo_inten"], 0.72),
        (ESTILO["cuerpo_frac"], ESTILO["cuerpo_inten"], 0.30),
        (1.0, ESTILO["halo_inten"], 0.12),
    ]
    for frac, inten, blanco in capas:
        for i in range(n_total):
            pluma = plumas[i]
            eje, sm, sn = pluma["eje"], pluma["semi_mas"], pluma["semi_menos"]
            for j in range(8):
                q0, q1 = j / 8.0, (j + 1) / 8.0
                dx = eje[j + 1][0] - eje[j][0]
                dy = eje[j + 1][1] - eje[j][1]
                px, py = -dy, dx
                l = math.hypot(px, py) or 1.0
                px, py = px / l, py / l
                a = eje[j][0] + px * sm[j] * frac, eje[j][1] + py * sm[j] * frac
                b = eje[j][0] - px * sn[j] * frac, eje[j][1] - py * sn[j] * frac
                c = eje[j + 1][0] + px * sm[j + 1] * frac, eje[j + 1][1] + py * sm[j + 1] * frac
                d = eje[j + 1][0] - px * sn[j + 1] * frac, eje[j + 1][1] - py * sn[j + 1] * frac
                b10 = lerp(0.22, fundidos[i], q0 * q0) * (1.0 - suave(0.60, 1.0, q0))
                b11 = lerp(0.22, fundidos[i], q1 * q1) * (1.0 - suave(0.60, 1.0, q1))
                c0 = (inten * b10,) + tono_pluma(hues[i] + ESTILO["hue_largo"] * q0, blanco * (1.0 - 0.3 * q0))
                c1 = (inten * b11,) + tono_pluma(hues[i] + ESTILO["hue_largo"] * q1, blanco * (1.0 - 0.3 * q1))
                quads.append((a, b, c, d, c0, c0, c1, c1))
    return quads


# =====================================================================
#  EL RASTERIZADOR (baricéntrico, blend soft-add de la casa)
# =====================================================================
def rasterizar(quads, w=ANCHO * SS, h=ALTO * SS):
    img = np.zeros((h, w, 3), dtype=np.float64)
    ox, oy = SS * 0.5, SS * 0.5
    for a, b, c, d, ca, cb, cc, cd in quads:
        for (p0, p1, p2, c0v, c1v, c2v) in (
            (a, b, c, ca, cb, cc),
            (a, c, d, ca, cc, cd),
        ):
            x0, y0 = p0[0] * SS + ox, p0[1] * SS + oy
            x1, y1 = p1[0] * SS + ox, p1[1] * SS + oy
            x2, y2 = p2[0] * SS + ox, p2[1] * SS + oy
            minX = max(int(math.floor(min(x0, x1, x2))), 0)
            maxX = min(int(math.ceil(max(x0, x1, x2))), w - 1)
            minY = max(int(math.floor(min(y0, y1, y2))), 0)
            maxY = min(int(math.ceil(max(y0, y1, y2))), h - 1)
            if minX > maxX or minY > maxY:
                continue
            det = (y1 - y2) * (x0 - x2) + (x2 - x1) * (y0 - y2)
            if abs(det) < 1e-9:
                continue
            xs = np.arange(minX, maxX + 1) - x0
            ys = np.arange(minY, maxY + 1) - y0
            gx, gy = np.meshgrid(xs, ys)
            # LAS BARICÉNTRICAS RELATIVAS AL VÉRTICE 0 (Cramer): el punto
            # P = V0+(gx,gy) = λ1·e1 + λ2·e2 con e1=V1−V0, e2=V2−V0 — la
            # forma absoluta de antes mezclaba coordenadas y NO pintaba.
            e1x, e1y = x1 - x0, y1 - y0
            e2x, e2y = x2 - x0, y2 - y0
            detR = e1x * e2y - e1y * e2x
            if abs(detR) < 1e-9:
                continue
            l1 = (gx * e2y - gy * e2x) / detR
            l2 = (gy * e1x - gx * e1y) / detR
            l0 = 1.0 - l1 - l2
            m = (l0 >= -0.001) & (l1 >= -0.001) & (l2 >= -0.001)
            if not m.any():
                continue
            # el color del vértice: (intensidad, r, g, b) → rgb·intensidad
            rgb = (np.array(c0v[1:]) * c0v[0] * l0[:, :, None] +
                   np.array(c1v[1:]) * c1v[0] * l1[:, :, None] +
                   np.array(c2v[1:]) * c2v[0] * l2[:, :, None])
            sub = img[minY:maxY + 1, minX:maxX + 1]
            aporte = rgb * (1.0 - sub) * m[:, :, None]
            sub += aporte
    return np.clip(img, 0.0, 1.0)


def guardar_png(img, path):
    from PIL import Image
    alto, ancho = img.shape[:2]
    img8 = (img.reshape(alto // SS, SS, ancho // SS, SS, 3)
               .mean(axis=(1, 3)) * 255).astype(np.uint8)
    Image.fromarray(img8).save(path)


# =====================================================================
#  LAS MEDIDAS
# =====================================================================
def medir(img, etiqueta):
    alto, ancho = img.shape[:2]
    lum = img.mean(axis=2)
    cy, cx = CY * SS, CX * SS
    # --- A · EL CONTORNO PERCIBIDO (por ángulo, el radio del umbral) ---
    nAng = 1440
    radios = np.zeros(nAng)
    thetas = np.linspace(-math.pi, math.pi, nAng, endpoint=False)
    for k, th in enumerate(thetas):
        rmax = 0.0
        for r in np.arange(4.0, 140.0, 0.5):
            x = int(cx + math.cos(th) * r * SS)
            y = int(cy + math.sin(th) * r * SS)
            if 0 <= x < ancho and 0 <= y < alto and lum[y, x] > UMBRAL:
                rmax = r
        radios[k] = rmax
    # el arco del ala izquierda y derecha (el abanico vive arriba)
    dentro = []
    for k, th in enumerate(thetas):
        if radios[k] > 8.0:
            dentro.append(k)
    arco = np.array([radios[k] for k in dentro])
    zigzag = arco.max() - arco.min() if len(arco) else 0.0
    # los PICOS del contorno (dientes): máximos locales del radio suavizado
    if len(arco) > 12:
        ker = np.ones(9) / 9.0
        su = np.convolve(arco, ker, mode="same")
        su[:6] = su[6:12][::-1]
        su[-6:] = su[-12:-6][::-1]
        picos = 0
        for i in range(8, len(su) - 8):
            if su[i] >= su[i - 1] and su[i] > su[i + 1]:
                ventana = su[max(0, i - 40):i + 40]
                if su[i] - ventana.min() > 3.0:
                    picos += 1
    else:
        picos = 0
    # --- B · LOS RAYOS INTERIORES (arco a media distancia) ---
    r_rayo = 46.0
    nR = 1440
    rayo = np.zeros(nR)
    for k in range(nR):
        th = -math.pi + 2 * math.pi * k / nR
        x = int(cx + math.cos(th) * r_rayo * SS)
        y = int(cy + math.sin(th) * r_rayo * SS)
        rayo[k] = lum[y, x] if (0 <= x < ancho and 0 <= y < alto) else 0.0
    ker = np.ones(7) / 7.0
    rayo_su = np.convolve(rayo, ker, mode="same")
    rayo_su[:3] = rayo_su[3:6][::-1]
    rayo_su[-3:] = rayo_su[-6:-3][::-1]
    rayos = 0
    for i in range(6, nR - 6):
        if rayo_su[i] > rayo_su[i - 1] and rayo_su[i] >= rayo_su[i + 1]:
            ventana = rayo_su[max(0, i - 60):i + 60]
            if rayo_su[i] - ventana.min() > 0.045 and rayo_su[i] > 0.12:
                rayos += 1
    # --- C · LA SIMETRÍA DEL PAR (izq vs der, el radio del contorno) ---
    dif = 0.0
    cnt = 0
    for k, th in enumerate(thetas):
        th2 = math.pi - th
        k2 = int(round((th2 + math.pi) / (2 * math.pi) * nAng)) % nAng
        if radios[k] > 8.0 and radios[k2] > 8.0:
            dif += abs(radios[k] - radios[k2])
            cnt += 1
    simetria = dif / cnt if cnt else 0.0
    print(f"  [{etiqueta}] contorno: zigzag {zigzag:5.1f} px · picos {picos} · "
          f"rayos interiores {rayos} · simetría Δ {simetria:5.2f} px")
    return dict(zigzag=zigzag, picos=picos, rayos=rayos, simetria=simetria)


# =====================================================================
#  EL SIM
# =====================================================================
def main():
    print("SIM ALAS v6.129 — el puerto 1:1 del estilo Runas del Vacío")
    global CENTRO
    plumas, hues, fundidos, val2, por_lado = construir_par()
    CENTRO = val2
    n_total = por_lado * 2

    for lisa, nombre, ruta in (
        (False, "CON PLUMAS (las tiras de siempre)", "/home/z/my-project/mod/tools/sim_alas_v6129_con_plumas.png"),
        (True, "LISA (la vela estirada al borde envolvente — la v6.129)", "/home/z/my-project/mod/tools/sim_alas_v6129_lisa.png"),
    ):
        if lisa:
            # v6.129 — LA VELA ESTIRADA: los ejes corren al borde envolvente
            plumas_l, n_l = estirar_vela_lisa(plumas, hues, fundidos, por_lado, val2)
            fundidos_l = [p["fundido"] for p in plumas_l]
            hues_ll = [p["hue"] for p in plumas_l]
            por_lado_l = n_l // 2
            quads = capa_vela(plumas_l, hues_ll, fundidos_l, por_lado_l, True)
            quads += capa_borde(plumas_l, hues_ll, fundidos_l, por_lado_l, val2, True)
        else:
            quads = capa_vela(plumas, hues, fundidos, por_lado, False)
            quads += capa_borde(plumas, hues, fundidos, por_lado, val2, False)
            quads += capas_tiras(plumas, hues, fundidos, n_total)
        img = rasterizar(quads)
        guardar_png(img, ruta)
        print(f"  {nombre}: {ruta}")
        medir(img, nombre)
    print("El sim termina.")


if __name__ == "__main__":
    main()
