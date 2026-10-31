#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
v6.57 — SIM 1:1 DE LA PILA v2 (SIETE CAPAS + AGUJA + ESTRANGULACIÓN).
La regla de la casa: «siempre analisa lo que haces para ver si crea
picos o no».

Replica EXACTO el RayosPruebaProjectile v6.57:
  · StripWidth(p) = p^0.8 · cbrt(lerp) · AGUJA(p) · CIERRE(t) · 3500 ·
                    (1.1 + cos·(0.08−p·0.06))
      AGUJA   = Lerp(1, 0.06, GetLerpValue(0.84, 1, p)^1.35)   (la punta)
      CIERRE  = Lerp(1, 0.07, cierre01²)  (la muerte: colapso del haz)
  · LA PILA (7 capas, misma polilínea): bruma → glow → (glow²) →
    CUERPO → ALMA (la tela blanca a todo el ancho, NUEVA) → núcleo →
    soles conducto (SIN corazón dorado — la ley del corazón).
Renderiza:
  · las 3 recetas EN VIDA (cierre = 1)
  · la receta C MURIENDO (cierre01 = 0.44 → cierre ≈ 0.83) y AGONIZANDO
    (cierre01 = 0.89 → cierre ≈ 0.27)
Y ANALIZA:
  · PICOS del borde exterior por columna (saltos > 4 px) — en vida Y en
    muerte (la estrangulación NO puede generar picos: todas las capas
    comparten el mismo ancho).
  · MONOTONÍA de la aguja: el perfil de anchos por nodo debe ser suave
    (2ª diferencia sin saltos) — cero esquinas.
"""
from PIL import Image
import math, os

W, H = 960, 540
OUT = "/tmp/sim657/"
os.makedirs(OUT, exist_ok=True)

_perfil = [0,0,0,0,1,6,24,61,121,187,233,254,255,236,168,84,26,5,0,0,0,0]
def rampa(v):
    v = max(0.0, min(1.0, v))
    x = v * 20
    i = int(x)
    if i >= 20: return _perfil[20] / 255.0
    return (_perfil[i] + (x - i) * (_perfil[i+1] - _perfil[i])) / 255.0

def hash01(seed, a, b):
    n = (seed * 374761393 + a * 668265263 + b * 2246822519) & 0xFFFFFFFF
    n = ((n ^ (n >> 13)) * 1274126177) & 0xFFFFFFFF
    return ((n ^ (n >> 16)) & 0xFFFFFFFF) / 4294967295.0

PALETA = [(150,62,235),(104,34,182),(208,46,88),(178,52,214)]
def conjuro(reloj):
    t = reloj % 48.0
    seg = t / 12.0
    i = int(seg) % 4
    j = (i + 1) % 4
    f = seg - int(seg)
    a, b = PALETA[i], PALETA[j]
    return tuple(a[k] + (b[k]-a[k]) * f for k in range(3))

def lerp1(a, b, t): return a + (b - a) * t
def get_lerp_value(frm, to, v):
    x = (v - frm) / (to - frm)
    return max(0.0, min(1.0, x))

# ============ EL ANCHO v6.57 (AGUJA + CIERRE) ============
def strip_width(p, time, real=1.0, cierre01=0.0):
    start = p ** 0.8
    cap = max(0.0, get_lerp_value(1.01, 0.7, p)) ** (1/3)
    # LA AGUJA (v6.57): el último 16 % se afila a ×0.06 — punta de aguja.
    aguja = lerp1(1.0, 0.06, get_lerp_value(0.84, 1.0, p) ** 1.35)
    # LA ESTRANGULACIÓN (v6.57): la muerte colapsa TODO el conducto.
    cierre = lerp1(1.0, 0.07, cierre01 * cierre01)
    return start * cap * aguja * cierre * real * 3500.0 * (1.1 + math.cos(time) * (0.08 - p * 0.06))

RECETAS = {
    "A": dict(kb=2.60, ab=0.09, kg=1.75, ag=0.20, k2=0,    a2=0,    kn=0.34, an=0.80, ka=0.88, aa=0.20, soles=5),
    "B": dict(kb=3.10, ab=0.14, kg=2.10, ag=0.28, k2=0,    a2=0,    kn=0.42, an=0.95, ka=0.92, aa=0.26, soles=8),
    "C": dict(kb=3.60, ab=0.20, kg=2.50, ag=0.24, k2=1.90, a2=0.12, kn=0.28, an=0.65, ka=0.96, aa=0.30, soles=6),
}

def render(receta, time=90.0, reloj=120.0, cierre01=0.0, cuerpo_shader=True, ruta=None,
          soles=True, ruido_cuerpo=True):
    """soles=False y ruido_cuerpo=False → MODO GEOMETRÍA: solo las capas
    de rampa suaves (el SOBRE del haz — donde vivirían los picos reales).
    Los soles son destellos flotantes APARTE (por diseño) y el ruido
    entero del cuerpo es un artefacto del sim (el shader real muestrea
    fBm con filtrado bilineal — suave por construcción)."""
    R = RECETAS[receta]
    img = Image.new("RGB", (W, H), (10, 8, 16))
    px = img.load()
    boca = (90, 300)
    ang = -0.16
    ux, uy = math.cos(ang), math.sin(ang)
    px_, py_ = -uy, ux
    largo = 3500 * (0.5 + 1.0 * 0.5)

    for y in range(H):
        for x in range(W):
            rx, ry = x - boca[0], y - boca[1]
            along = rx * ux + ry * uy
            perp = rx * px_ + ry * py_
            if along < -20 or along > largo: continue
            p = max(0.0, min(1.0, along / largo))
            ap = abs(perp)
            semi = strip_width(p, time, cierre01=cierre01)
            if semi < 0.001 or ap > semi * R["kb"]: continue

            rr = gg = bb = 0.0

            # (1) BRUMA
            c = conjuro(reloj - p * 48.0)
            f = rampa(0.5 + (perp / (semi * R["kb"])) * 0.5)
            sombra = (c[0]*0.42, c[1]*0.34, c[2]*0.52)
            rr += sombra[0] * R["ab"] * f
            gg += sombra[1] * R["ab"] * f
            bb += sombra[2] * R["ab"] * f

            # (2) GLOW
            if ap < semi * R["kg"]:
                c = conjuro(reloj - p * 48.0)
                f = rampa(0.5 + (perp / (semi * R["kg"])) * 0.5)
                rr += c[0] * R["ag"] * f
                gg += c[1] * R["ag"] * f
                bb += c[2] * R["ag"] * f

            # (3) GLOW²
            if R["a2"] > 0 and ap < semi * R["k2"]:
                c = conjuro(reloj - p * 48.0)
                cal = tuple(lerp1(c[k], (255,246,255)[k], 0.28) for k in range(3))
                f = rampa(0.5 + (perp / (semi * R["k2"])) * 0.5)
                rr += cal[0] * R["a2"] * f
                gg += cal[1] * R["a2"] * f
                bb += cal[2] * R["a2"] * f

            # (4) CUERPO (aprox del shader)
            if cuerpo_shader and ap < semi:
                c = conjuro(reloj - p * 48.0)
                v = 0.5 + (perp / semi) * 0.5
                if ruido_cuerpo:
                    n = (hash01(int((p*90+v*40+time*0.8)*3), int(v*70), 7) * 0.55
                         + hash01(int((p*50)*5), int((v*90+time*0.6)*3), 11) * 0.45)
                else:
                    n = 0.5  # MODO GEOMETRÍA: envolvente suave sin ruido
                env = rampa(v)
                cuerpo = n * env * (1.0 - 0.75 * max(0.0, (p - 0.5) / 0.5))
                tono = (c[0]*0.62/255, c[1]*0.5/255, c[2]*0.95/255)
                rr += 255 * cuerpo * tono[0] * 1.35
                gg += 255 * cuerpo * tono[1] * 1.35
                bb += 255 * cuerpo * tono[2] * 1.35
                lum = cuerpo
                nucleo = max(0.0, (lum - 0.42) / 0.58) * rampa(v)
                rr += 255 * nucleo * 0.9; gg += 250 * nucleo * 0.9; bb += 255 * nucleo * 0.9

            # (5) EL ALMA (v6.57) — LA TELA BLANCA A TODO EL ANCHO:
            #     la MISMA rampa del núcleo abierta hasta la silueta.
            if ap < semi * R["ka"]:
                alma = tuple(lerp1((255,250,255)[k], (222,196,255)[k], 0.42) for k in range(3))
                f = rampa(0.5 + (perp / (semi * R["ka"])) * 0.5)
                rr += alma[0] * R["aa"] * f
                gg += alma[1] * R["aa"] * f
                bb += alma[2] * R["aa"] * f

            # (6) NÚCLEO (la vena — arde ENCIMA del alma)
            if ap < max(1.0, semi * R["kn"]):
                c = (255, 246, 255)
                f = rampa(0.5 + (perp / max(1.0, semi * R["kn"])) * 0.5)
                rr += c[0] * R["an"] * f * 0.55
                gg += c[1] * R["an"] * f * 0.55
                bb += c[2] * R["an"] * f * 0.55

            px[x, y] = (min(255, int(rr)), min(255, int(gg)), min(255, int(bb)))

    # (7) SOLES CONDUCTO — SIN CORAZÓN DORADO (v6.57, la ley del corazón):
    #     solo la estrella blanca de 4 puntas. (En MODO GEOMETRÍA no se
    #     dibujan: son destellos flotantes APARTE del sobre.)
    for i in range(R["soles"] if soles else 0):
        ritmo = 0.045 + 0.05 * hash01(97, 610 + i, 7)
        p = (hash01(97, 600 + i, 5) * 0.88 + 2.0 * ritmo) % 1.0
        semi = strip_width(p, time, cierre01=cierre01)
        if semi < 4.0: continue
        lado = 1 if i % 2 == 0 else -1
        margen = (0.62 + 0.30 * hash01(97, 620 + i, 11)) * semi * lado
        cx = boca[0] + ux * (p * largo) + px_ * margen
        cy = boca[1] + uy * (p * largo) + py_ * margen
        size = max(9.0, semi * (0.085 + 0.045 * hash01(97, 630 + i, 13))) * 0.55
        arde = 0.42 + 0.58 * math.sin(2.0 * 6.3 + i * 2.39)
        if arde <= 0.05: continue
        for t in range(-int(size*2.2), int(size*2.2)+1):
            for l1 in (-1, 1):
                yy = int(cy + l1 * max(1.0, size*0.55 - abs(t)*0.18))
                xx = int(cx + t)
                if 0 <= xx < W and 0 <= yy < H:
                    r0,g0,b0 = px[xx,yy]
                    a = 200 * arde
                    px[xx,yy] = (min(255,int(r0+a)), min(255,int(g0+a*0.97)), min(255,int(b0+a*0.88)))
        for t in range(-int(size), int(size)+1):
            for l1 in (-1, 1):
                xx = int(cx + l1 * max(1.0, size*0.55 - abs(t)*0.18))
                yy = int(cy + t)
                if 0 <= xx < W and 0 <= yy < H:
                    r0,g0,b0 = px[xx,yy]
                    a = 200 * arde
                    px[xx,yy] = (min(255,int(r0+a)), min(255,int(g0+a*0.97)), min(255,int(b0+a*0.88)))
        # (EL CORAZÓN DORADO MURIÓ AQUÍ — v6.57: a escala de margen se
        #  leía como círculo amarillo. Ya no existe en el haz.)

    if ruta:
        img.save(ruta)

    # ============ EL ANÁLISIS DE PICOS (el peine de la casa) ============
    picos = 0
    borde_prev = None
    saltos = []
    for x in range(80, W - 10):
        alto = None
        for y in range(0, H):
            r, g, b = px[x, y]
            if b > 28 and r > 10 and not (r > 230 and g > 230):
                alto = y; break
        if alto is None: continue
        if borde_prev is not None:
            salto = alto - borde_prev
            if abs(salto) > 4:
                picos += 1
                saltos.append((x, salto))
        borde_prev = alto
    return picos, saltos

def perfil_anchos(cierre01):
    """EL CHEQUEO NUMÉRICO DEL ANCHO: 1500 nodos como el juego; la 2ª
    diferencia del semiancho no puede saltar (cero esquinas => cero
    picos por construcción). Se omite el NACIMIENTO de la boca (los 3
    primeros nodos: p^0.8 nace empinado POR DISEÑO desde 607df36 — el
    afinado de la boca del arma, curva suave, no esquina)."""
    time = 90.0
    anchos = [strip_width(i / 1499.0, time, cierre01=cierre01) for i in range(1500)]
    peores = []
    for i in range(4, 1499):
        d2 = anchos[i] - 2 * anchos[i-1] + anchos[i-2]
        # una esquina POLIGONAL salta decenas de px entre nodos; la
        # curvatura suave de la aguja/cierre es monótona y pequeña.
        if abs(d2) > max(4.0, anchos[i] * 0.06):
            peores.append((i, round(d2, 2), round(anchos[i], 1)))
    # LA AGUJA DEBE SER MONÓTONA: del p=0.84 a la punta el ancho solo
    # BAJA (cero rebotes — un rebote sería un pico de silhouette).
    zona = anchos[int(0.84 * 1499):-1]
    rebotes = sum(1 for a, b in zip(zona, zona[1:]) if b > a + 0.5)
    return peores, anchos, rebotes

if __name__ == "__main__":
    print("=" * 68)
    print("SIM 1:1 v6.57 — LA PILA DE SIETE CAPAS + AGUJA + ESTRANGULACIÓN")
    print("=" * 68)

    # --- 1. EL SOBRE (GEOMETRÍA PURA): donde vivirían los picos ---
    #     Soles y ruido entero FUERA: el sobre son SOLO las rampas suaves.
    ok = True
    for r in "ABC":
        picos, saltos = render(r, soles=False, ruido_cuerpo=False,
                               ruta=OUT + f"v657_{r}_sobre.png")
        print(f"SOBRE RECETA {r} EN VIDA:        picos de borde = {picos}  {saltos[:4]}")
        ok &= picos == 0
    for nombre, c01 in (("muriendo", 0.44), ("agonizando", 0.89)):
        picos, saltos = render("C", cierre01=c01, soles=False, ruido_cuerpo=False,
                               ruta=OUT + f"v657_C_sobre_{nombre}.png")
        c = lerp1(1.0, 0.07, c01 * c01)
        print(f"SOBRE RECETA C {nombre.upper()} (×{c:.3f}):  picos de borde = {picos}  {saltos[:4]}")
        ok &= picos == 0

    # --- 2. LOS RENDERES COMPLETOS (visual + los soles flotando) ---
    for r in "ABC":
        render(r, ruta=OUT + f"v657_{r}_vida.png")
    render("C", cierre01=0.44, ruta=OUT + "v657_C_muriendo.png")
    render("C", cierre01=0.89, ruta=OUT + "v657_C_agonizando.png")

    # --- 3. EL CHEQUEO NUMÉRICO DEL PERFIL (aguja + cierre) ---
    for nombre, c01 in (("vida", 0.0), ("agonizando", 0.89)):
        peores, anchos, rebotes = perfil_anchos(c01)
        print(f"PERFIL DE ANCHOS ({nombre}): esquinas = {len(peores)} {peores[:3]} · "
              f"rebotes de la aguja = {rebotes}")
        w84, w1 = anchos[int(0.84 * 1499)], anchos[-1]
        print(f"  · AGUJA: semiancho p=0.84 → {w84:7.1f}px ; p=1.0 → {w1:7.1f}px "
              f"(×{w1 / max(w84, 0.001):.3f} — debe ser ≈ ≤0.10)")
        ok &= len(peores) == 0 and rebotes == 0

    print()
    print("VEREDICTO:", "0 PICOS EN EL SOBRE — LA LEY SE CUMPLE" if ok
          else "¡HAY PICOS EN EL SOBRE — PARAR Y CURAR!")
