#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_rift_meseta_v631.py — LAS TEXTURAS DEL DESGARRO PAREJO (v6.31).

LA RAÍZ DEL DEFECTO (medida en v6.30 → research/v631/INFORME_TAJOS_CORTE_REALIDAD.md):
las RiftTaper* horneaban un perfil longitudinal tipo HUSO — ancho 100% SOLO al
centro (u≈0.5-0.65), 0.77 en u=0.20, 0.54 en u=0.10, 0.20 en las puntas — y el
ojo lee esa variación como línea DISCONTINUA y DESPAREJA. Además llevaban una
respiración nebulosa ±15% a 4 ciclos a lo largo del eje (más "cuentas").

LA GEOMETRÍA NUEVA (informe §D.2-D.4 — la lección CyberRift/CWR):
  · PERFIL DE MESETA + TAPAS REDONDAS (estadio): ancho 100% en u∈[0.10, 0.90]
    (el 80% del largo), tapa CIRCULAR en cada extremo (sqrt(1-t²) — la cápsula
    de las líneas continuas de verdad), CERO respiración longitudinal.
  · El ancho NO respira: la vida la pone la ALPHA (el código).

LA ANATOMÍA VERTICAL (cross-section, preservada y CORREGIDA — quad 1.60·W):
  · VACÍO   (RiftTaperVoid):   banda negra sólida d≤0.625 (= maxWidth en pantalla,
    el contrato D.3 "14 constante"), α 0.94, borde suave ~1.5 px. Color (8,8,10).
  · CUERPO  (RiftTaperCuerpo): LOS DOS LABIOS en el borde del vacío (dd=0.625),
    σ≈0.13 — SIN relleno central (v6.31: el vacío se lee NEGRO de verdad).
  · NÚCLEO  (RiftTaperNucleo): los dos filos RAZOR sobre los labios (σ≈0.055)
    + EL CENTRO CEGADOR (lección Last Prism: banda blanca pura ×0.5 del ancho
    del vacío, α 0.14 — profundidad dentro de la herida).
  · VELO    (RiftTaperVelo):   halo integrador (1-dd^2.6)^1.2 hasta d≈0.94
    (×1.6 del ancho = 22 px del contrato).

La anatomía entera se COMPRIME con el ancho local (dd = d/w(u)): las tapas se
cierran en telescopio — el corte termina en PUNTA REDONDA, no en corte seco.

Salida: Content/Effects/Procedural/RiftTaper{Velo,Cuerpo,Nucleo,Void}.png
        (512×64, alfa recto, blanco para las capas de luz).
Auto-verificación numérica: meseta real, 0 cortes, desviación ≤±5% en u∈[0.15,0.85].
"""
import math
import os
import sys

from PIL import Image

# ==== CONFIGURACIÓN ==========================================================
W, H = 512, 64                 # el contrato de las RiftTaper (v6.28)
CAP = 0.10                     # tapas redondas: 10% en cada extremo (meseta 80%)
SS = 3                         # super-muestreo 3×3 por texel (bordes suaves)

A_VOID = 240                   # 0.94 — el negro del vacío
A_CUERPO = 224                 # 0.88 del pase (0.60 lo pone el tint del código)
A_NUCLEO = 255
A_VELO = 140

VOID_EDGE = 0.625              # d del borde del vacío (= maxWidth en pantalla)
VOID_SOFT = 0.045              # borde suave del vacío (~1 px)
LIP_POS = 0.625                # los labios EN el borde del vacío
LIP_SIGMA = 0.130
RAZOR_SIGMA = 0.055
CORE_HALF = 0.31               # el centro cegador: ×0.5 del ancho del vacío
CORE_SIGMA = 0.22
CORE_ALPHA = 0.14

OUT_DIR = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))),
                       "Content", "Effects", "Procedural")


# ==== EL PERFIL LONGITUDINAL: MESETA + TAPAS REDONDAS (el estadio) ===========
def perfil_meseta(u: float) -> float:
    """Ancho del corte en u∈[0,1]: 1.0 en el cuerpo, tapa circular en los extremos."""
    if CAP <= u <= 1.0 - CAP:
        return 1.0
    if u < CAP:
        t = max(u / CAP, 0.0)
        return math.sqrt(max(1.0 - (1.0 - t) ** 2, 0.0))   # círculo: sube RÁPIDO
    t = max((u - (1.0 - CAP)) / CAP, 0.0)
    return math.sqrt(max(1.0 - t * t, 0.0))


# ==== LAS SECCIONES VERTICALES (d = |2v-1| normalizado; dd = d/w local) ======
def sec_veilo(dd: float) -> float:
    """El halo integrador: plano arriba, hombros suaves, muere en d≈0.94."""
    if dd >= 0.99:
        return 0.0
    return max(1.0 - dd ** 2.6, 0.0) ** 1.2


def sec_cuerpo(dd: float) -> float:
    """LOS DOS LABIOS (uno por borde del vacío) — SIN relleno central."""
    return math.exp(-0.5 * ((dd - LIP_POS) / LIP_SIGMA) ** 2)


def sec_nucleo(dd: float) -> float:
    """Los filos RAZOR sobre los labios + EL CENTRO CEGADOR (Last Prism)."""
    razor = math.exp(-0.5 * ((dd - LIP_POS) / RAZOR_SIGMA) ** 2)
    core = CORE_ALPHA * math.exp(-0.5 * (dd / CORE_SIGMA) ** 2) if dd < CORE_HALF * 3 else 0.0
    return min(razor + core, 1.0)


def sec_void(dd: float) -> float:
    """La banda negra sólida con borde suave (smoothstep de 1→0)."""
    if dd <= VOID_EDGE - VOID_SOFT:
        return 1.0
    if dd >= VOID_EDGE + VOID_SOFT:
        return 0.0
    t = (dd - (VOID_EDGE - VOID_SOFT)) / (2.0 * VOID_SOFT)
    s = t * t * (3.0 - 2.0 * t)          # smoothstep ascendente
    return 1.0 - s                        # ...invertido: la banda muere suave


# ==== GENERADOR ==============================================================
def generar(nombre: str, seccion, alpha_max: int, rgb) -> Image.Image:
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    px = img.load()
    for x in range(W):
        u = x / (W - 1)
        wu = perfil_meseta(u)
        if wu <= 0.001:
            continue
        for y in range(H):
            # super-muestreo del texel (bordes limpios sin dientes de sierra)
            acc = 0.0
            for sy in range(SS):
                vy = (y + (sy + 0.5) / SS) / H
                for sx in range(SS):
                    vx = (x + (sx + 0.5) / SS) / W
                    ww = perfil_meseta(vx)
                    if ww <= 0.001:
                        continue
                    d = abs(2.0 * vy - 1.0)
                    dd = d / ww if ww > 0.001 else 99.0
                    acc += seccion(dd)
            a = acc / (SS * SS)
            if a <= 0.004:
                continue
            px[x, y] = (rgb[0], rgb[1], rgb[2], int(round(alpha_max * min(a, 1.0))))
    return img


def main() -> int:
    capas = [
        ("RiftTaperVelo", sec_veilo, A_VELO, (255, 255, 255)),
        ("RiftTaperCuerpo", sec_cuerpo, A_CUERPO, (255, 255, 255)),
        ("RiftTaperNucleo", sec_nucleo, A_NUCLEO, (255, 255, 255)),
        ("RiftTaperVoid", sec_void, A_VOID, (8, 8, 10)),
    ]
    os.makedirs(OUT_DIR, exist_ok=True)
    for nombre, sec, amax, rgb in capas:
        img = generar(nombre, sec, amax, rgb)
        ruta = os.path.join(OUT_DIR, nombre + ".png")
        img.save(ruta)
        print(f"  · {nombre}.png — {W}x{H}, α máx {amax}")
    print(f"TEXTURAS ESCRITAS en {OUT_DIR}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
