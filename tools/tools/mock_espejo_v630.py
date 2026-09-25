#!/usr/bin/env python3
# mock_espejo_v630.py — EL MOCK 1:1 DEL ESPEJO ROTO (RiftLib v3)
#
# Replica EXACTO en Python:
#   · CaminoGrieta (el generador Lichtenberg con los mismos números)
#   · CaminoEspejoRoto (canal + ramas + sub-ramas + arcos telaraña)
#   · AnchosCamino v6.30 (taper 0.45 + suelo 25%)
#   · GrietaVacio + Grieta v6.30 (solape len+wmax, perlas en TODOS los
#     vértices, suelo 0.6px, NINGÚN segmento omitido)
# con muestreo BILINEAL (LinearClamp) y PNGs premultiplicados (tML).
#
# MIDE: los huecos a lo largo del CENTRO de cada camino del espejo —
# el brillo mínimo por posición: si el mínimo ≥ umbral, la cadena ES
# CONTINUA (cero cortes) — el veredicto numérico de v6.28 ahora para
# TODO el ramillete.

from PIL import Image
import numpy as np
import math, os

BASE = os.path.join(os.path.dirname(__file__), '..', 'Content', 'Effects', 'Procedural')

def cargar(nombre):
    a = np.array(Image.open(os.path.join(BASE, nombre + '.png')).convert('RGBA')).astype(float)
    # premultiplicar (el pipeline tML/FNA)
    a[:, :, :3] *= (a[:, :, 3:4] / 255.0)
    return a

LIP = cargar('RiftLip')
CORE = cargar('RiftCore')
VOID = cargar('BlackDisk')

def sample(tex, x, y):
    """Bilinear LinearClamp sobre la textura (uv 0..1)."""
    h, w = tex.shape[:2]
    fx, fy = min(max(x, 0.0), 1.0) * (w - 1), min(max(y, 0.0), 1.0) * (h - 1)
    x0, y0 = int(fx), int(fy)
    x1, y1 = min(x0 + 1, w - 1), min(y0 + 1, h - 1)
    tx, ty = fx - x0, fy - y0
    c00 = tex[y0, x0]; c10 = tex[y0, x1]; c01 = tex[y1, x0]; c11 = tex[y1, x1]
    return (c00 * (1 - tx) * (1 - ty) + c10 * tx * (1 - ty) +
            c01 * (1 - tx) * ty + c11 * tx * ty)

def hash01(seed, a, b):
    h = (seed * 374761393 + a * 668265263 + b * 1911520717) & 0xFFFFFFFF
    if h >= 2**31: h -= 2**32
    h ^= (h >> 13) & 0xFFFFFFFF if h >= 0 else -(((-h) >> 13))
    h = (h * 1274126177) & 0xFFFFFFFF
    if h >= 2**31: h -= 2**32
    h ^= (h >> 16)
    return (h & 0xFFFFFF) / 16777216.0

TWO_PI = math.pi * 2

# ----------------------------------------------------------------------
# EL GENERADOR (puerto exacto de RiftLib)
# ----------------------------------------------------------------------
def camino_grieta(origin, dir_, seed, points=24, paso_min=25.0, paso_max=50.0,
                  curvatura=5.0, fallas=9.0):
    points = max(4, min(40, points))
    fallas = min(12.0, max(3.0, fallas))
    pts = [np.array(origin, dtype=float)]
    bearing = math.atan2(dir_[1], dir_[0]) if (dir_[0] or dir_[1]) else 0.0
    bearing0 = bearing
    drift = 0.0
    for i in range(1, points):
        g = (hash01(seed, i, 101) - 0.5) * 2 * curvatura * (0.25 * i) * math.pi / 180
        drift = min(0.9, max(-0.9, drift + g))
        bearing = bearing0 + drift
        if hash01(seed, i, 211) < 1.0 / fallas:
            kink = (hash01(seed, i, 307) - 0.5) * 2 * 0.6
            drift = min(0.9, max(-0.9, drift + kink))
            bearing = bearing0 + drift
        paso = paso_min + (paso_max - paso_min) * hash01(seed, i, 401)
        if i == points - 1: paso *= 0.5
        pts.append(pts[-1] + np.array([math.cos(bearing), math.sin(bearing)]) * paso)
    return pts

def camino_espejo_roto(origin, dir_, seed, length=620.0):
    dir_ = dir_ / (np.linalg.norm(dir_) + 1e-9)
    pts_n = max(14, min(26, int(length / 30)))
    principal = camino_grieta(origin, dir_, seed, pts_n, 24.0, 42.0, 4.5, 6.0)
    ramas, anchos_r = [], []
    n = len(principal)
    lado = 1 if seed % 2 == 0 else -1
    for i in range(2, n - 2, 2 + seed % 2):
        if hash01(seed, i, 1201) < 0.35: continue
        a, b = principal[i - 1], principal[i]
        tan = b - a
        if np.dot(tan, tan) < 0.01: continue
        tan = tan / np.linalg.norm(tan)
        restante = 1 - i / max(n - 1, 1)
        largo = length * (0.22 + 0.20 * hash01(seed, i, 1211)) * (0.45 + 0.55 * restante)
        if largo < 55: continue
        ang = (25 + 30 * hash01(seed, i, 1221)) * math.pi / 180 * lado
        c, s = math.cos(ang), math.sin(ang)
        dir_r = np.array([tan[0] * c - tan[1] * s, tan[0] * s + tan[1] * c])
        pts_r = max(4, min(9, int(largo / 26)))
        ramas.append(camino_grieta(b, dir_r, seed + i * 17, pts_r, 13.0, 24.0, 5.0, 7.0))
        anchos_r.append(0.60)
        lado = -lado
    # sub-ramas de las 3 más largas
    orden = sorted(range(len(ramas)), key=lambda k: -longitud(ramas[k]))
    for s_i in range(min(3, len(orden))):
        idx = orden[s_i]
        madre = ramas[idx]
        m = len(madre)
        for j in range(2, m - 1, 3):
            if hash01(seed, 3301 + idx, j) < 0.55: continue
            a, b = madre[j - 1], madre[j]
            tan = b - a
            if np.dot(tan, tan) < 0.01: continue
            tan = tan / np.linalg.norm(tan)
            largo_sub = longitud(madre) * 0.45 * (0.6 + 0.4 * hash01(seed, 3311 + idx, j))
            if largo_sub < 40: continue
            ang_s = (30 + 25 * hash01(seed, 3321 + idx, j)) * math.pi / 180 * \
                    (1 if hash01(seed, 3331 + idx, j) > 0.5 else -1)
            c, s = math.cos(ang_s), math.sin(ang_s)
            dir_s = np.array([tan[0] * c - tan[1] * s, tan[0] * s + tan[1] * c])
            pts_s = max(3, min(6, int(largo_sub / 22)))
            ramas.append(camino_grieta(b, dir_s, seed + 3401 + idx * 13 + j, pts_s, 11.0, 20.0, 5.0, 7.0))
            anchos_r.append(0.45)
    # los arcos telaraña
    arcos, anchos_a = [], []
    for anillo in range(2):
        radio = length * (0.22 + 0.20 * anillo)
        segs = 3 + seed % 3
        hueco = 0.12 + 0.06 * hash01(seed, anillo, 1301)
        for s_i in range(segs):
            a0 = s_i / segs * TWO_PI + hash01(seed, anillo * 31 + s_i, 1311) * 0.6
            a1 = (s_i + 1 - hueco) / segs * TWO_PI
            arco = []
            for p in range(7):
                t = p / 6
                ang = a0 + (a1 - a0) * t
                rr = radio * (0.92 + 0.16 * hash01(seed, anillo * 7 + s_i, 1321 + p))
                arco.append(origin + np.array([math.cos(ang), math.sin(ang)]) * rr)
            arcos.append(arco)
            anchos_a.append(0.40)
    return principal, ramas, anchos_r, arcos, anchos_a

def longitud(camino):
    return sum(np.linalg.norm(camino[i] - camino[i - 1]) for i in range(1, len(camino)))

def anchos_camino(camino, max_w):
    total = longitud(camino)
    if total < 1: return [max_w] * len(camino)
    suelo = max_w * 0.25
    ws = [max_w]
    arc = 0.0
    for i in range(1, len(camino)):
        arc += np.linalg.norm(camino[i] - camino[i - 1])
        t = arc / total
        ws.append(max(max_w * (1 - t) ** 0.45, suelo))
    return ws

# ----------------------------------------------------------------------
# EL RENDER (puerto exacto de Grieta v6.30 — el pase de LUZ)
# ----------------------------------------------------------------------
def render_grieta(canvas, camino, max_w, paleta, intensity=1.0, vida=1.0, time_=0.0, seed=0):
    """Pinta la cadena v6.30 en el canvas aditivo (RGB float)."""
    if len(camino) < 2 or longitud(camino) < 8: return
    ws = anchos_camino(camino, max_w)
    respira = 1 + 0.08 * math.sin(time_ * 2.2 + seed * 0.13)

    def quad(tex, pos, size, rot, tint):
        """Un quad estirado (como SpriteBatch) muestreado al canvas."""
        w_px, h_px = size
        if w_px < 0.1 or h_px < 0.1: return
        cos_r, sin_r = math.cos(rot), math.sin(rot)
        # recorrer el AABB CORRECTO del quad rotado (v6.30-fix: el AABB de
        # un rectángulo w×h rotado θ es x±(|cos|·w+|sin|·h)/2, y±(|sin|·w+|cos|·h)/2)
        ax = abs(cos_r) * w_px + abs(sin_r) * h_px
        ay = abs(sin_r) * w_px + abs(cos_r) * h_px
        x0 = int(max(0, math.floor(pos[0] - ax * 0.5 - 1)))
        x1 = int(min(canvas.shape[1] - 1, math.ceil(pos[0] + ax * 0.5 + 1)))
        y0 = int(max(0, math.floor(pos[1] - ay * 0.5 - 1)))
        y1 = int(min(canvas.shape[0] - 1, math.ceil(pos[1] + ay * 0.5 + 1)))
        if x1 <= x0 or y1 <= y0: return
        for py in range(y0, y1 + 1):
            for pxx in range(x0, x1 + 1):
                d = np.array([pxx + 0.5, py + 0.5]) - pos
                lu = d[0] * cos_r + d[1] * sin_r      / 1.0
                lv = -d[0] * sin_r + d[1] * cos_r
                if abs(lu) > w_px * 0.5 or abs(lv) > h_px * 0.5: continue
                u = (lu / w_px) + 0.5
                v = (lv / h_px) + 0.5
                c = sample(tex, u, v)
                # aditivo con el tinte (c.R*t.R/255, alpha c.A*t.A/255)
                r = c[0] * tint[0] / 255.0
                g = c[1] * tint[1] / 255.0
                b = c[2] * tint[2] / 255.0
                a = c[3] * tint[3] / 255.0
                canvas[py, pxx, 0] += r
                canvas[py, pxx, 1] += g
                canvas[py, pxx, 2] += b

    velo = paleta[0]; cuerpo = paleta[1]; nucleo = paleta[-1]
    k = 0
    for i in range(len(camino) - 1):
        a, b = camino[i], camino[i + 1]
        delta = b - a
        length_seg = np.linalg.norm(delta)
        if length_seg < 0.30: continue
        wa = max(ws[i] * respira, 0.6)
        wb = max(ws[i + 1] * respira, 0.6)
        wseg = (wa + wb) * 0.5
        wmax = max(wa, wb)
        mid = (a + b) * 0.5
        rot = math.atan2(delta[1], delta[0])
        beat = 0.88 + 0.12 * math.sin(time_ * 6.1 + k * 1.7 + seed)

        # VELO + CUERPO + NÚCLEO (solape len + wmax — v6.30)
        quad(LIP, mid, (length_seg + wmax, wseg * 1.6), rot, (velo[0], velo[1], velo[2], 255 * 0.30 * intensity * vida * beat))
        quad(LIP, mid, (length_seg + wmax, wseg), rot, (cuerpo[0], cuerpo[1], cuerpo[2], 255 * 0.60 * intensity * vida))
        quad(CORE, mid, (length_seg + wmax, wseg * 0.8), rot, (nucleo[0], nucleo[1], nucleo[2], 255 * 0.90 * intensity * vida))
        # LA PERLA del vértice (TODOS)
        quad(LIP, a, (wmax * 1.25, wseg * 1.6), rot, (velo[0], velo[1], velo[2], 255 * 0.30 * intensity * vida * beat))
        quad(LIP, a, (wmax * 1.25, wseg), rot, (cuerpo[0], cuerpo[1], cuerpo[2], 255 * 0.60 * intensity * vida))
        quad(CORE, a, (wmax * 1.25, wseg * 0.8), rot, (nucleo[0], nucleo[1], nucleo[2], 255 * 0.90 * intensity * vida))
        k += 1
    # LA PERLA DE LA PUNTA
    fin = len(camino) - 1
    if fin > 0:
        a, b = camino[fin - 1], camino[fin]
        rot = math.atan2(b[1] - a[0] if False else (b - a)[1], (b - a)[0])
        wfin = max(ws[fin] * respira, 0.6)
        quad(LIP, b, (wfin * 1.25, wfin * 1.6), rot, (velo[0], velo[1], velo[2], 255 * 0.30 * intensity * vida))
        quad(LIP, b, (wfin * 1.25, wfin), rot, (cuerpo[0], cuerpo[1], cuerpo[2], 255 * 0.60 * intensity * vida))
        quad(CORE, b, (wfin * 1.25, wfin * 0.8), rot, (nucleo[0], nucleo[1], nucleo[2], 255 * 0.90 * intensity * vida))

# ----------------------------------------------------------------------
# EL TEST DE CONTINUIDAD — el brillo a lo largo del CENTRO de cada camino
# ----------------------------------------------------------------------
def medir_continuidad(camino, canvas):
    """Muestrea el brillo del canvas a lo largo del centro del camino.
    Devuelve (min, media, tramos_bajos) — tramos_bajos = posiciones con
    brillo < 45/255 (un 'corte' visible)."""
    brillos = []
    for i in range(len(camino) - 1):
        a, b = camino[i], camino[i + 1]
        seg = np.linalg.norm(b - a)
        n = max(2, int(seg / 2))
        for k in range(n):
            p = a + (b - a) * (k / n)
            x, y = int(round(p[0])), int(round(p[1]))
            if 0 <= y < canvas.shape[0] and 0 <= x < canvas.shape[1]:
                brillos.append(canvas[y, x].max())
    if not brillos: return 0, 0, 999
    arr = np.array(brillos)
    cortes = int((arr < 45).sum())
    return float(arr.min()), float(arr.mean()), cortes

def main():
    W, H = 1400, 700
    canvas = np.zeros((H, W, 3), dtype=float)
    paleta = [(150, 80, 255), (220, 60, 200), (255, 255, 255)]
    seed = 7331
    origin = np.array([180.0, 350.0])
    dir_ = np.array([1.0, 0.0])
    time_ = 3.7

    principal, ramas, anchos_r, arcos, anchos_a = camino_espejo_roto(origin, dir_, seed, 620.0)

    print("== EL ESPEJO ROTO (v6.30) ==")
    print(f"   canal madre: {len(principal)} pts, {longitud(principal):.0f} px")
    print(f"   ramas: {len(ramas)} (largos {[f'{longitud(r):.0f}' for r in ramas[:8]]}...)")
    print(f"   arcos: {len(arcos)}")

    # render: canal a ancho completo, ramas ×escala, arcos ×escala
    render_grieta(canvas, principal, 14.0, paleta, 1.0, 1.0, time_, seed)
    for r, esc in zip(ramas, anchos_r):
        render_grieta(canvas, r, 14.0 * esc, paleta, 0.88, 1.0, time_, seed + 51)
    for arc, esc in zip(arcos, anchos_a):
        render_grieta(canvas, arc, 14.0 * esc, paleta, 0.75, 1.0, time_, seed + 97)

    # LA MEDICIÓN
    print("\n== LA MEDICIÓN DE CONTINUIDAD (brillo del centro, corte < 45/255) ==")
    todo_min, todo_cortes = 999.0, 0
    for nombre, camino in [("CANAL MADRE", principal)] + \
            [(f"rama {i}", r) for i, r in enumerate(ramas)] + \
            [(f"arco {i}", a) for i, a in enumerate(arcos)]:
        mn, mean, cortes = medir_continuidad(camino, canvas)
        todo_min = min(todo_min, mn)
        todo_cortes += cortes
        estado = "CONTINUA" if cortes == 0 and mn >= 45 else "¡CORTE!"
        print(f"   {nombre:<12} min={mn:6.1f} media={mean:6.1f} cortes={cortes:3d}  {estado}")

    print(f"\n   VEREDICTO: mínimo global={todo_min:.1f}/255, cortes totales={todo_cortes}")

    # guardar la visualización (gamma para verla)
    vis = np.clip(canvas, 0, 255).astype(np.uint8)
    Image.fromarray(vis).save('/tmp/espejo_v630.png')
    print("   visual: /tmp/espejo_v630.png")

    # comparar con el ALGORITMO v6.29 (solape 0.9w, sin perlas en raíz/punta,
    # taper 0.9 sin suelo, skip <0.4)
    print("\n== EL COMPARATIVO v6.29 (lo que el usuario veía con cortes) ==")
    canvas_v29 = np.zeros((H, W, 3), dtype=float)
    def render_v29(camino, max_w):
        if len(camino) < 2 or longitud(camino) < 8: return
        ws = [max_w * (1 - (sum(np.linalg.norm(camino[j]-camino[j-1]) for j in range(1, i+1)) / longitud(camino))) ** 0.9
              for i in range(len(camino))]
        for i in range(len(camino) - 1):
            a, b = camino[i], camino[i+1]
            delta = b - a
            ls = np.linalg.norm(delta)
            if ls < 0.35: continue
            wseg = (ws[i] + ws[i+1]) * 0.5
            if wseg < 0.4: continue          # ← el SKIP que hacía huecos
            mid = (a+b)*0.5
            rot = math.atan2(delta[1], delta[0])
            def q(tex, pos, size, rot, tint):
                w_px, h_px = size
                if w_px < 0.1 or h_px < 0.1: return
                cos_r, sin_r = math.cos(rot), math.sin(rot)
                ax = abs(cos_r)*w_px + abs(sin_r)*h_px
                ay = abs(sin_r)*w_px + abs(cos_r)*h_px
                for py in range(int(pos[1]-ay*0.5-1), int(pos[1]+ay*0.5+1)+1):
                    for pxx in range(int(pos[0]-ax*0.5-1), int(pos[0]+ax*0.5+1)+1):
                        if not (0<=py<H and 0<=pxx<W): continue
                        d = np.array([pxx+0.5, py+0.5]) - pos
                        lu = d[0]*cos_r + d[1]*sin_r
                        lv = -d[0]*sin_r + d[1]*cos_r
                        if abs(lu) > w_px*0.5 or abs(lv) > h_px*0.5: continue
                        c = sample(tex, lu/w_px+0.5, lv/h_px+0.5)
                        canvas_v29[py,pxx,0] += c[0]*tint[0]/255
                        canvas_v29[py,pxx,1] += c[1]*tint[1]/255
                        canvas_v29[py,pxx,2] += c[2]*tint[2]/255
            velo, cuerpo, nucleo = paleta
            q(LIP, mid, (ls + wseg*0.9, wseg*1.6), rot, (velo[0], velo[1], velo[2], 255*0.30))
            q(LIP, mid, (ls + wseg*0.9, wseg), rot, (cuerpo[0], cuerpo[1], cuerpo[2], 255*0.60))
            q(CORE, mid, (ls + wseg*0.9, wseg*0.8), rot, (nucleo[0], nucleo[1], nucleo[2], 255*0.90))
            if i > 0:
                q(LIP, a, (wseg*1.15, wseg*1.6), rot, (velo[0], velo[1], velo[2], 255*0.30))
                q(LIP, a, (wseg*1.15, wseg), rot, (cuerpo[0], cuerpo[1], cuerpo[2], 255*0.60))
                q(CORE, a, (wseg*1.15, wseg*0.8), rot, (nucleo[0], nucleo[1], nucleo[2], 255*0.90))
    render_v29(principal, 14.0)
    for r, esc in zip(ramas, anchos_r):
        render_v29(r, 14.0 * esc)
    mn29, _, c29 = medir_continuidad(principal, canvas_v29)
    mn29b, _, c29b = medir_continuidad(ramas[0] if ramas else principal, canvas_v29)
    print(f"   v6.29 canal: min={mn29:.1f} cortes={c29} | rama 0: min={mn29b:.1f} cortes={c29b}")

if __name__ == '__main__':
    main()
