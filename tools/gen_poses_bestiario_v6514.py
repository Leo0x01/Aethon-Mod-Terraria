#!/usr/bin/env python3
# v6.51.4 — LAS POSE DEL BESTIARIO DE LOS JEFES (la letra: «los jefes
# no tiene sprite en el bestiario»). Los 4 jefes son 100% código (el
# arte vive en su PreDraw con Main.screenPosition del MUNDO — en el
# menú el bestiario dibuja al dummy con screenPos=Zero y el arte cae
# donde quiere + la danza de lotes corrompe la UI). LA CURA VANILLA:
# NPCBestiaryDrawOffset[Type].CustomTexturePath — una POSE pre-render-
# izada (el arte procedural de cada jefe, destilado en miniatura por
# este generador, en luz aditiva y tone-mapped como el juego lo haría).
import math
from PIL import Image

S = 112          # tamaño final
SS = 4           # supersample
N = S * SS
C = N / 2.0

class Lienzo:
    def __init__(self):
        self.r = [0.0] * (N * N)
        self.g = [0.0] * (N * N)
        self.b = [0.0] * (N * N)

    def globo(self, x, y, rad, col, falloff='gauss'):
        """Luz aditiva circular en (x,y) — col = (r,g,b) en unidades de luz."""
        cr, cg, cb = col
        x0 = max(0, int(x - rad - 2)); x1 = min(N, int(x + rad + 3))
        y0 = max(0, int(y - rad - 2)); y1 = min(N, int(y + rad + 3))
        rr2 = rad * rad + 1e-9
        for py in range(y0, y1):
            dy = py + 0.5 - y
            base = py * N
            dy2 = dy * dy
            for px in range(x0, x1):
                dx = px + 0.5 - x
                d2 = (dx * dx + dy2) / rr2
                if d2 > 1.0:
                    continue
                d = math.sqrt(d2)
                if falloff == 'gauss':
                    a = math.exp(-3.2 * d * d)
                elif falloff == 'core':
                    a = (1.0 - d) ** 1.6
                elif falloff == 'ring':
                    a = math.exp(-26.0 * (d - 0.82) ** 2)
                else:
                    a = 1.0 - d
                if a <= 0.003:
                    continue
                i = base + px
                self.r[i] += cr * a
                self.g[i] += cg * a
                self.b[i] += cb * a

    def quad(self, x, y, w, h, col, rot=0.0):
        """Cápsula/cuña suave (elipse lineal) rotada — el rayo de la casa."""
        cr, cg, cb = col
        cos, sin = math.cos(rot), math.sin(rot)
        R = max(w, h)
        x0 = max(0, int(x - R - 2)); x1 = min(N, int(x + R + 3))
        y0 = max(0, int(y - R - 2)); y1 = min(N, int(y + R + 3))
        w2 = w * w + 1e-9; h2 = h * h + 1e-9
        for py in range(y0, y1):
            dy = py + 0.5 - y
            base = py * N
            for px in range(x0, x1):
                dx = px + 0.5 - x
                ux = dx * cos + dy * sin
                uy = -dx * sin + dy * cos
                d2 = ux * ux / w2 + uy * uy / h2
                if d2 > 1.0:
                    continue
                a = 1.0 - math.sqrt(d2)
                if a <= 0.003:
                    continue
                i = base + px
                self.r[i] += cr * a
                self.g[i] += cg * a
                self.b[i] += cb * a

    def tono(self, exposure=1.0, gamma=2.2, resize=True):
        im = Image.new('RGBA', (N, N))
        px = im.load()
        for y in range(N):
            base = y * N
            for x in range(N):
                i = base + x
                r = 1.0 - math.exp(-self.r[i] * exposure)
                g = 1.0 - math.exp(-self.g[i] * exposure)
                b = 1.0 - math.exp(-self.b[i] * exposure)
                lum = max(r, g, b)
                a = min(1.0, lum * 1.18)
                if a < 0.004:
                    px[x, y] = (0, 0, 0, 0)
                    continue
                px[x, y] = (int(255 * r ** (1 / gamma) + .5),
                            int(255 * g ** (1 / gamma) + .5),
                            int(255 * b ** (1 / gamma) + .5),
                            int(255 * a + .5))
        return im.resize((S, S), Image.LANCZOS) if resize else im

def rayos(lienzo, cx, cy, n, desde, largo, col, paso=0.16, par=True):
    """La rueda de luz: n cuñas radiales (largas/cortas alternadas)."""
    for i in range(n):
        ang = i * math.tau / n + paso
        L = largo if (not par or i % 2 == 0) else largo * 0.75
        for k in range(9):
            f = (k + 0.5) / 9.0
            rr = desde + f * L
            w = (3.3 - 1.9 * f)
            cr, cg, cb = col
            fcol = (cr * (1 - 0.55 * f), cg * (1 - 0.55 * f), cb * (1 - 0.55 * f))
            lienzo.quad(cx + math.cos(ang) * rr, cy + math.sin(ang) * rr,
                        w, w * 2.3, fcol, rot=ang)

# ======================================================================
#  1 — AETHON, EL DIOS DE LUZ (Oro 255,240,190 · Violeta 196,150,255 ·
#      Núcleo 255,252,240)
# ======================================================================
def pose_aethon():
    L = Lienzo()
    L.globo(C, C, 48 * SS, (0.055, 0.038, 0.09))                     # el velo violeta
    L.globo(C, C, 32 * SS, (0.52, 0.44, 0.24), 'ring')               # el halo dorado (aro)
    L.globo(C, C, 28 * SS, (0.30, 0.24, 0.13))                       # el resplandor interior
    rayos(L, C, C, 14, 12 * SS, 36 * SS, (0.46, 0.40, 0.20))         # la rueda de luz
    L.globo(C, C, 13 * SS, (1.50, 1.35, 0.95), 'core')               # el núcleo
    L.globo(C, C, 8 * SS, (1.90, 1.75, 1.30), 'core')
    L.globo(C, C, 3.4 * SS, (2.20, 2.05, 1.55), 'core')              # la chispa
    return L.tono(0.9)

# ======================================================================
#  2 — EL FUSIONADO: el sol dorado TAPADO por el disco de la
#      oscuridad — UN ECLIPSE VIVO (rim violeta en el borde)
# ======================================================================
def pose_fusionado():
    L = Lienzo()
    L.globo(C, C, 52 * SS, (0.05, 0.036, 0.085))
    L.globo(C, C, 34 * SS, (0.50, 0.42, 0.22), 'ring')
    L.globo(C, C, 28 * SS, (0.28, 0.22, 0.12))
    rayos(L, C, C, 12, 13 * SS, 40 * SS, (0.44, 0.38, 0.19))
    L.globo(C, C, 12 * SS, (1.50, 1.32, 0.90), 'core')
    im = L.tono(0.92, resize=False)
    # EL DISCO DE LA OSCURIDAD (opaca el sol — el eclipse vivo)
    dx, dy, rr = -8 * SS, -6 * SS, 26 * SS
    p = im.load()
    for y in range(N):
        for x in range(N):
            d2 = (x - (C + dx)) ** 2 + (y - (C + dy)) ** 2
            if d2 <= rr * rr:
                d = math.sqrt(d2) / rr
                rim = math.exp(-30.0 * (d - 0.93) ** 2)
                r, g, b, a = p[x, y]
                p[x, y] = (int(30 + 60 * rim), int(10 + 26 * rim), int(52 + 110 * rim),
                           max(a, int(235 - 20 * (1 - rim))))
    return im.resize((S, S), Image.LANCZOS)

# ======================================================================
#  3 — EL OSCURO, EL SOL MORIBUNDO (halo 90,20,140 · velo 24,10,38 ·
#      brasa 150,90,220)
# ======================================================================
def pose_oscuro():
    L = Lienzo()
    L.globo(C, C, 54 * SS, (0.035, 0.016, 0.062))                    # el velo negro-morado
    L.globo(C, C, 34 * SS, (0.30, 0.06, 0.46), 'ring')               # el halo moribundo
    L.globo(C, C, 29 * SS, (0.16, 0.04, 0.27))
    rayos(L, C, C, 10, 13 * SS, 32 * SS, (0.26, 0.07, 0.42), paso=0.31)
    L.globo(C, C, 12 * SS, (0.52, 0.26, 0.82), 'core')               # la brasa violeta
    L.globo(C, C, 7 * SS, (0.62, 0.30, 0.95), 'core')
    L.globo(C, C, 3.2 * SS, (0.70, 0.34, 1.05), 'core')
    return L.tono(1.0)

# ======================================================================
#  4 — EL CAZADOR ASTRAL, EL LOBO-ESTRELA (cuerpo 140,200,255 · ojo
#      255,214,106 · cola de 3 cuentas + la corona del líder)
# ======================================================================
def pose_cazador():
    L = Lienzo()
    ang = -0.5                                     # el rumbo de caza (derecha-arriba)
    cos, sin = math.cos(ang), math.sin(ang)
    def hacia(d, perp=0.0):
        return (C + cos * d - sin * perp, C + sin * d + cos * perp)
    x0, y0 = hacia(-2 * SS)
    L.globo(x0, y0, 30 * SS, (0.10, 0.17, 0.28))                     # el aura del cuerpo
    for k in range(3):                                               # la cola (3 cuentas)
        xk, yk = hacia(-(16 + k * 11) * SS, (3 if k % 2 == 0 else -3) * SS)
        L.globo(xk, yk, (6.5 - k * 1.3) * SS, (0.14, 0.22, 0.34), 'core')
    for e in (-1, 1):                                                # las orejas (varillas)
        angE = ang + math.pi + e * 0.52
        ex = C + math.cos(angE) * 12 * SS
        ey = C + math.sin(angE) * 12 * SS
        L.quad((C + ex) / 2, (C + ey) / 2, 2.2 * SS, 6.0 * SS, (0.24, 0.38, 0.58), rot=angE)
        L.globo(ex, ey, 2.4 * SS, (0.30, 0.46, 0.68), 'core')
    L.globo(C, C, 13.5 * SS, (0.34, 0.52, 0.80), 'core')             # el cuerpo
    L.globo(C, C, 8.5 * SS, (0.55, 0.80, 1.15), 'core')
    hx, hy = hacia(11 * SS, -3 * SS)                                 # el hocico
    L.globo(hx, hy, 6 * SS, (0.48, 0.70, 1.02), 'core')
    ox, oy = hacia(4 * SS, -4.5 * SS)                                # el ojo dorado
    L.globo(ox, oy, 3.8 * SS, (1.15, 0.86, 0.32), 'core')
    sx, sy = hacia(-2 * SS, -17 * SS)                                # la corona del líder
    for a2 in range(4):
        aa = a2 * math.pi / 2 + 0.4
        L.quad(sx + math.cos(aa) * 5 * SS, sy + math.sin(aa) * 5 * SS,
               1.8 * SS, 5.5 * SS, (1.05, 0.78, 0.28), rot=aa)
    L.globo(sx, sy, 3 * SS, (1.20, 0.92, 0.38), 'core')
    return L.tono(1.0)

if __name__ == '__main__':
    import os
    base = '/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod/Content/NPCs'
    pose_aethon().save(os.path.join(base, 'AethonBoss_PoseBestiario.png'))
    pose_fusionado().save(os.path.join(base, 'AethonBossFusionado_PoseBestiario.png'))
    pose_oscuro().save(os.path.join(base, 'AethonBossOscuro_PoseBestiario.png'))
    pose_cazador().save(os.path.join(base, 'CazadorAstral_PoseBestiario.png'))
    print('poses generadas')
