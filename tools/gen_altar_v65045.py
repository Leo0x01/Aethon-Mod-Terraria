#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_altar_v65045.py — EL ALTAR ANTIGUO, REESCRITO (v6.50.45).

La petición (SEGUNDA ronda sobre el mismo sprite — la .44 no bastó):
«mejorar el sprite de Altar Antiguo». El diagnóstico de la .44 (54×36):
cristal pequeño y ruidoso (10 px ahogados entre W/G/V sin orden), losas
PLANAS de relleno sin textura ni volumen, una RANURA de 1 px por runa y
un disco de oro plano donde debía vivir el medallón.

LA REESCRITA (54×36, supermuestreo ×8 → LANCZOS):
  · EL CRISTAL GEMO (el héroe): 13×14 px — silueta de gema alargada con
    FILO DE ORO de 1 px (como las gemas de Terraria), facetas con LUZ
    coherente (blanco arriba, oro a la izquierda, violetas abajo — la
    luz viene de arriba), NÚCLEO blanco vertical, TRES glints, DOS
    satélites orbitando y un GLOBO dorado suave detrás (el cristal se
    lee PRIMERO).
  · SU SOMBRA sobre la cara superior de la plataforma (la luz que da,
    la sombra que cobra).
  · LOS PILARES TALLADOS: capitel + plinto (más anchos que el fuste),
    DOS RUNAS por fuste con formas distintas (una «F», un «∠») en oro
    con marco hundido oscuro — talladas, no ranuradas; el CANTO INTERNO
    (el que mira al cristal) más CLARO (+20%) y el externo más oscuro
    (−18%): el cristal LOS alumbra; y dos DESPORTILLADURAS por pilar.
  · LA PLATAFORMA en dos niveles: cara superior clara (recibe la luz),
    frente medio con RUIDO por píxel (±6%) y EL MEDALLÓN DEL SOL: anillo
    de oro 1 px + disco radial oro→ámbar + EL SIGILO de Aethon (la raya
    vertical con dos brazos) en violeta hundido + 4 destellos blancos.
  · LAS VETAS KINTSUGI: dos hilos de oro de 1 px bajando del medallón
    por el frente (la fractura reparada con oro — el altar SANO).
  · LA BASE: canto superior iluminado, frente oscuro con ruido y
    esquinas DESPORTILLADAS irregulares (talla a mano, no rectángulo).
El ÍTEM (24×24): la misma jerarquía en miniatura — el cristal arriba,
    el pedestal de dos escalones con su medallón, la base con muescas.

Determinista (hash propio, CERO random), solo PIL. Salidas:
  AethonMod/Content/Tiles/AncientAltar.png          (54×36 RGBA)
  AethonMod/Content/Items/Placeables/AncientAltarItem.png (24×24 RGBA)
"""
from PIL import Image, ImageDraw

SS = 8          # supermuestreo
TW, TH = 54, 36  # tile
IW, IH = 24, 24  # ítem


def h01(a, b, c):
    """Hash determinista → [0,1). La casa: FNV-1a mezclado."""
    h = (a * 374761393 + b * 668265263 + c * 2147483647) & 0xFFFFFFFF
    h ^= h >> 13
    h = (h * 1274126177) & 0xFFFFFFFF
    h ^= h >> 16
    return (h & 0xFFFFFF) / 0xFFFFFF


def mix(c1, c2, f):
    return tuple(int(c1[i] + (c2[i] - c1[i]) * f) for i in range(3))


class Lienzo:
    def __init__(self, w, h):
        self.w, self.h = w, h
        self.im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
        self.d = ImageDraw.Draw(self.im)

    def px(self, x, y, col):
        if 0 <= x < self.w and 0 <= y < self.h:
            self.im.putpixel((int(x), int(y)), col + (255,) if len(col) == 3 else col)

    def rect(self, x0, y0, x1, y1, col):
        self.d.rectangle([x0, y0, x1 - 1, y1 - 1], fill=col + (255,) if len(col) == 3 else col)

    def poly(self, pts, col):
        self.d.polygon(pts, fill=col + (255,) if len(col) == 3 else col)

    def ellipse_ring(self, cx, cy, rx, ry, col, wpx):
        """Anillo elíptico de grosor wpx."""
        for i in range(int(ry * 2)):
            pass  # (se usa la versión de arco de abajo)

    def arc_ellipse(self, cx, cy, rx, ry, col, wpx):
        self.d.ellipse([cx - rx, cy - ry, cx + rx, cy + ry],
                       outline=col + (255,) if len(col) == 3 else col, width=max(1, int(wpx)))

    def glow(self, cx, cy, r, col, amax):
        """Globo radial aditivo-look (alpha hacia 0 en el borde)."""
        r = int(r)
        for dy in range(-r, r + 1):
            for dx in range(-r, r + 1):
                d2 = (dx * dx + dy * dy) / (r * r)
                if d2 >= 1.0:
                    continue
                a = int(amax * (1.0 - d2) ** 2)
                if a <= 2:
                    continue
                x, y = int(cx + dx), int(cy + dy)
                if 0 <= x < self.w and 0 <= y < self.h:
                    base = self.im.getpixel((x, y))
                    if len(base) < 4 or base[3] == 0:
                        self.im.putpixel((x, y), col + (a,))
                    else:
                        # suma (el glow se ACUMULA sobre lo dibujado)
                        na = min(255, base[3] + a)
                        nr = min(255, base[0] + col[0] * a // 255)
                        ng = min(255, base[1] + col[1] * a // 255)
                        nb = min(255, base[2] + col[2] * a // 255)
                        self.im.putpixel((x, y), (nr, ng, nb, na))

    def ruido(self, x0, y0, x1, y1, base, amp, salt):
        """Ruido por píxel (±amp fracción) sobre una zona ya dibujada."""
        for y in range(int(y0), int(y1)):
            for x in range(int(x0), int(x1)):
                p = self.im.getpixel((x, y))
                if len(p) < 4 or p[3] == 0:
                    continue
                m = 1.0 + (h01(x, y, salt) - 0.5) * 2.0 * amp
                self.im.putpixel((x, y), (
                    min(255, int(p[0] * m)), min(255, int(p[1] * m)),
                    min(255, int(p[2] * m)), p[3]))

    def borde(self, x0, y0, x1, y1, lado, col, wpx=1):
        """Pinta un canto del rectángulo."""
        if lado == "t":
            self.rect(x0, y0, x1, y0 + wpx, col)
        elif lado == "b":
            self.rect(x0, y1 - wpx, x1, y1, col)
        elif lado == "l":
            self.rect(x0, y0, x0 + wpx, y1, col)
        elif lado == "r":
            self.rect(x1 - wpx, y0, x1, y1, col)


def s(v):
    """tile→supersample."""
    return v * SS


# ======================================================================
#  EL TILE (432×288 ss → 54×36)
# ======================================================================
def build_tile():
    L = Lienzo(s(TW), s(TH))

    # --- 0. EL GLOBO DEL CRISTAL (primero: debajo de todo lo demás) ---
    #     El cristal se lee PRIMERO: su luz dorada lo separa del fondo.
    L.glow(s(27), s(8), s(10.5), (255, 232, 170), 78)

    # --- 1. LA PLATAFORMA (se dibuja ANTES del cristal: el cristal flota
    #        DELANTE de su cara superior) ---
    # tier superior: y20-29
    L.rect(s(0), s(20), s(54), s(23), (205, 186, 242))       # cara superior (clara)
    L.rect(s(0), s(23), s(54), s(29), (152, 127, 202))       # frente medio
    L.borde(0, s(20), s(54), s(29), "t", (232, 218, 255))    # canto superior iluminado
    L.rect(s(0), s(23), s(54), s(23) + SS, (188, 164, 238))  # la arista que da la luz
    L.rect(s(0), s(28), s(54), s(29), (96, 76, 152))         # la sombra del pie

    # EL MEDALLÓN DEL SOL (en el frente, centrado): anillo + disco +
    # sigilo + destellos — lo que la .44 dibujaba como disco plano.
    mx, my, mrx, mry = s(27), s(25.7), s(7.2), s(2.9)
    for dy in range(-int(mry), int(mry) + 1):
        for dx in range(-int(mrx), int(mrx) + 1):
            d = ((dx / mrx) ** 2 + (dy / mry) ** 2) ** 0.5
            if d <= 1.0:
                # disco radial: centro claro → borde ámbar
                c = mix((255, 240, 192), (212, 156, 84), d)
                L.px(int(mx + dx), int(my + dy), c)
    L.arc_ellipse(mx, my, mrx, mry, (255, 220, 132), max(1, SS // 3))  # anillo de oro
    # EL SIGILO de Aethon: raya vertical + dos brazos (hundido, violeta).
    sig = (74, 54, 132)
    L.rect(mx - SS // 2, my - s(1.6), mx + SS // 2, my + s(1.6), sig)
    L.rect(mx - s(2.2), my - SS, mx + s(2.2), my, sig)
    L.rect(mx - s(2.2), my, mx + s(2.2), my + SS, sig)
    # 4 destellos blancos sobre el anillo (las cuatro diagonales).
    for k in range(4):
        sx = 1 if k in (0, 1) else -1
        sy = 1 if k in (1, 2) else -1
        L.px(int(mx + mrx * 0.97 * sx), int(my + mry * 0.97 * sy), (255, 255, 250))

    # tier base: y29-36, x2-52
    L.rect(s(2), s(29), s(52), s(30), (188, 164, 238))       # canto superior
    L.rect(s(2), s(30), s(52), s(36), (108, 88, 166))        # frente oscuro
    L.rect(s(2), s(35), s(52), s(36), (72, 56, 122))         # la última sombra

    # --- 2. LOS PILARES TALLADOS (izquierdo y derecho) ---
    for lado in (-1, 1):
        if lado < 0:
            x_cap0, x_cap1 = s(0), s(12)       # capitel/plinto más anchos
            x_f0, x_f1 = s(1), s(11)           # fuste
        else:
            x_cap0, x_cap1 = s(42), s(54)
            x_f0, x_f1 = s(43), s(53)
        # capitel (y6-10): cara superior clara + frente
        L.rect(x_cap0, s(6), x_cap1, s(8), (196, 176, 236))
        L.rect(x_cap0, s(8), x_cap1, s(10), (166, 142, 216))
        L.borde(x_cap0, s(6), x_cap1, s(10), "t", (226, 210, 255))
        # fuste (y10-22): frente medio
        L.rect(x_f0, s(10), x_f1, s(22), (136, 111, 186))
        # el canto que MIRA AL CRISTAL se aclara (+): la luz lo baña.
        inner = "r" if lado < 0 else "l"
        outer = "l" if lado < 0 else "r"
        L.borde(x_f0, s(10), x_f1, s(22), inner, (176, 152, 226))
        L.borde(x_f0, s(10), x_f1, s(22), outer, (104, 84, 150))
        # DOS RUNAS talladas por fuste (y11-15 y y15-19 — ambas EN el
        # fuste visible, ANTES de la plataforma), oro con marco hundido:
        # una «F» y un «∠» — formas DISTINTAS, no ranuras.
        for rj, (ry0, ry1) in enumerate(((11, 15), (15, 19))):
            rx = s(5) if lado < 0 else s(47)
            rw = s(3) - SS // 3              # ~2.6 tile px de alcance
            # marco hundido (1 tile px alrededor)
            L.rect(rx - SS, s(ry0) - SS, rx + rw + SS, s(ry1) + SS, (58, 44, 104))
            oro = (255, 224, 148)
            if rj == 0:
                # «F»: palo vertical (2 px) + dos brazos
                L.rect(rx, s(ry0), rx + 2 * SS, s(ry1), oro)
                L.rect(rx, s(ry0) + SS, rx + rw, s(ry0) + 2 * SS + SS // 2, oro)
                L.rect(rx, s(ry0) + s(1.7), rx + rw - SS, s(ry0) + s(2.8), oro)
            else:
                # «∠»: el brazo alzado — 4 pasos de TILE, no ss
                for k in range(4):
                    off = int(k * 0.62 * SS)
                    y0 = s(ry1) - SS - k * SS
                    L.rect(rx + off, y0, rx + 2 * SS + off, y0 + SS, oro)
                L.rect(rx, s(ry1) - SS, rx + rw, s(ry1), oro)
        # plinto (y22-26): más ancho, oscuro
        L.rect(x_cap0, s(22), x_cap1, s(26), (116, 95, 172))
        L.rect(x_cap0, s(24), x_cap1, s(26), (96, 78, 152))
        # DESPORTILLADURAS del canto externo (talla a mano)
        for k in range(3):
            yy = s(10.5) + int(s(4.5) * h01(lado, k, 17))
            hh = SS + int(SS * 0.9 * h01(lado, k, 19))
            if lado < 0:
                L.rect(x_f0, yy, x_f0 + SS, yy + hh, (0, 0, 0, 0))
            else:
                L.rect(x_f1 - SS, yy, x_f1, yy + hh, (0, 0, 0, 0))

    # --- 3. EL CRISTAL GEMO (el héroe — se dibuja al final: flota
    #        DELANTE de la cara superior de la plataforma) ---
    cx, cy = s(27), s(8.3)
    hw, hh = s(6.4), s(6.9)   # semiancho, semialto
    top = (cx, cy - hh)
    ul = (cx - hw, cy - s(1.7))
    ur = (cx + hw, cy - s(1.7))
    ll = (cx - s(5.0), cy + hh)
    lr = (cx + s(5.0), cy + hh)
    bot = (cx, cy + s(6.2))
    # base de la gema (violeta medio)
    L.poly([top, ul, ll, bot, lr, ur], (150, 122, 214))
    # faceta superior-izquierda: ORO (la luz cae de arriba-izquierda)
    L.poly([top, ul, (cx, cy)], (255, 238, 196))
    # faceta superior-derecha: violeta CLARO
    L.poly([top, ur, (cx, cy)], (208, 188, 255))
    # faceta inferior-izquierda: violeta medio
    L.poly([ul, ll, (cx, cy)], (176, 150, 238))
    # faceta inferior-derecha: violeta PROFUNDO
    L.poly([ur, lr, (cx, cy)], (124, 100, 194))
    # EL NÚCLEO: cometa vertical blanco (la vena caliente de la gema)
    L.poly([(cx - s(0.9), cy - s(4.2)), (cx + s(0.9), cy - s(4.2)),
            (cx + s(0.55), cy + s(5.2)), (cx, cy + s(5.9)),
            (cx - s(0.55), cy + s(5.2))], (255, 252, 242))
    # EL FILO DE ORO (el contorno de las gemas de Terraria)
    L.d.line([top, ul, ll, bot, lr, ur, top], fill=(255, 218, 132) + (255,), width=max(1, SS // 3))
    # TRES GLINTS blancos (las chispas de la talla)
    for gx, gy in ((cx - s(3.1), cy - s(3.4)), (cx + s(2.6), cy - s(4.1)), (cx - s(0.6), cy + s(3.8))):
        L.rect(gx - SS // 2, gy - SS // 2, gx + SS // 2, gy + SS // 2, (255, 255, 252))
    # LOS DOS SATÉLITES (las esquirlas orbitando — la una y la otra)
    for sx, sy in ((cx - s(9.6), cy - s(0.4)), (cx + s(9.4), cy + s(1.8))):
        L.poly([(sx, sy - s(1.7)), (sx + s(1.2), sy), (sx, sy + s(1.7)),
                (sx - s(1.2), sy)], (255, 232, 178))
        L.poly([(sx, sy - s(0.9)), (sx + s(0.6), sy), (sx, sy + s(0.9)),
                (sx - s(0.6), sy)], (255, 252, 242))
    # LA SOMBRA DEL CRISTAL sobre la cara superior (la luz que da, la
    # sombra que cobra): elipse oscura suave, alpha 32%.
    shx, shy = s(27), s(21.4)
    for dy in range(-SS, SS + 1):
        for dx in range(-s(6), s(6) + 1):
            d = ((dx / s(6)) ** 2 + (dy / SS) ** 2) ** 0.5
            if d <= 1.0:
                p = L.im.getpixel((int(shx + dx), int(shy + dy)))
                if len(p) >= 4 and p[3] > 0:
                    L.im.putpixel((int(shx + dx), int(shy + dy)),
                                  (int(p[0] * 0.62), int(p[1] * 0.62), int(p[2] * 0.68), p[3]))

    # --- 4. LAS VETAS KINTSUGI (el oro que repara la fractura) ---
    for lado in (-1, 1):
        pts = []
        x = s(27) + lado * s(2.6)
        y = s(28.4)
        for k in range(9):
            x += lado * s(1.05)
            y += s(0.86)
            wob = (h01(lado, k, 23) - 0.5) * s(1.4)
            pts.append((x + wob, y))
        L.d.line(pts, fill=(255, 216, 130) + (245,), width=SS)  # 1 tile px de hilo de oro

    # --- 5. EL RUIDO DE LA PIEDRA (textura por píxel en TODO el frente) ---
    L.ruido(0, s(6), s(TW), s(TH), None, 0.055, 65045)
    L.ruido(s(2), s(30), s(52), s(36), None, 0.075, 65046)   # la base, más viva

    # --- 6. LAS ESQUINAS DESPORTILLADAS de la base (talla irregular) ---
    for lado in (-1, 1):
        bx = s(2) if lado < 0 else s(52)
        step = 1 if lado < 0 else -1
        for k in range(3):
            n = 2 + int(2.6 * h01(lado, k, 29))
            for j in range(n):
                xx = bx + step * j * SS
                yy = s(30) + int(s(2.2) * h01(lado, k * 7 + j, 31))
                hh = SS + int(s(1.8) * h01(lado, k * 5 + j, 33))
                L.rect(xx, yy, xx + SS, yy + hh, (0, 0, 0, 0))
    # la esquina inferior del pie, mordida
    for lado in (-1, 1):
        for j in range(3):
            if lado < 0:
                xa, xb = s(2) + j * SS, s(2) + (j + 1) * SS
            else:
                xa, xb = s(52) - (j + 1) * SS, s(52) - j * SS
            L.rect(xa, s(34) + j * (SS // 2), xb, s(36), (0, 0, 0, 0))

    return L.im.resize((TW, TH), Image.LANCZOS)


# ======================================================================
#  EL ÍTEM (192×192 ss → 24×24): la misma jerarquía en miniatura.
# ======================================================================
def build_item():
    L = Lienzo(s(IW), s(IH))
    cx = s(12)

    # EL GLOBO
    L.glow(cx, s(6), s(7), (255, 232, 170), 88)

    # LA BASE (dos escalones, de abajo hacia arriba)
    L.rect(s(2), s(19), s(22), s(20), (188, 164, 238))
    L.rect(s(2), s(20), s(22), s(24), (112, 92, 170))
    L.rect(s(4), s(14), s(20), s(15), (188, 164, 238))
    L.rect(s(4), s(15), s(20), s(19), (144, 120, 196))
    L.rect(s(4), s(18), s(20), s(19), (92, 74, 148))

    # EL MEDALLÓN mini (en el frente del escalón superior)
    mx, my, mrx, mry = cx, s(16.6), s(4.6), s(1.9)
    for dy in range(-int(mry), int(mry) + 1):
        for dx in range(-int(mrx), int(mrx) + 1):
            d = ((dx / mrx) ** 2 + (dy / mry) ** 2) ** 0.5
            if d <= 1.0:
                L.px(int(mx + dx), int(my + dy), mix((255, 240, 192), (212, 156, 84), d))
    L.arc_ellipse(mx, my, mrx, mry, (255, 220, 132), max(1, SS // 3))
    L.rect(cx - SS // 3, my - s(1.1), cx + SS // 3, my + s(1.1), (74, 54, 132))
    L.rect(cx - s(1.5), my - SS, cx + s(1.5), my, (74, 54, 132))
    L.rect(cx - s(1.5), my, cx + s(1.5), my + SS, (74, 54, 132))

    # LOS PILARES mini (a los lados del escalón superior)
    for lado in (-1, 1):
        x0 = s(3) if lado < 0 else s(19)
        L.rect(x0, s(9), x0 + s(2), s(14), (136, 111, 186))
        inner = "r" if lado < 0 else "l"
        outer = "l" if lado < 0 else "r"
        L.borde(x0, s(9), x0 + s(2), s(14), inner, (176, 152, 226))
        L.borde(x0, s(9), x0 + s(2), s(14), outer, (104, 84, 150))
        # UNA runa por pilar (la «F»)
        rx = x0 + (SS // 2 if lado < 0 else SS // 2)
        L.rect(rx, s(10), rx + SS, s(13), (255, 222, 142))
        L.rect(rx, s(10) + SS, rx + SS + s(1.2), s(10) + 2 * SS, (255, 222, 142))
        L.rect(rx, s(10) + s(1.6), rx + SS + s(0.8), s(10) + s(2.6), (255, 222, 142))

    # EL CRISTAL (el héroe, y2-12)
    cy = s(6.6)
    hw, hh = s(4.4), s(4.7)
    top = (cx, cy - hh)
    ul = (cx - hw, cy - s(1.2))
    ur = (cx + hw, cy - s(1.2))
    ll = (cx - s(3.4), cy + hh)
    lr = (cx + s(3.4), cy + hh)
    bot = (cx, cy + s(4.2))
    L.poly([top, ul, ll, bot, lr, ur], (150, 122, 214))
    L.poly([top, ul, (cx, cy)], (255, 238, 196))
    L.poly([top, ur, (cx, cy)], (208, 188, 255))
    L.poly([ul, ll, (cx, cy)], (176, 150, 238))
    L.poly([ur, lr, (cx, cy)], (124, 100, 194))
    L.poly([(cx - s(0.7), cy - s(2.9)), (cx + s(0.7), cy - s(2.9)),
            (cx + s(0.4), cy + s(3.5)), (cx, cy + s(4.0)),
            (cx - s(0.4), cy + s(3.5))], (255, 252, 242))
    L.d.line([top, ul, ll, bot, lr, ur, top], fill=(255, 218, 132) + (255,), width=max(1, SS // 3))
    for gx, gy in ((cx - s(2.2), cy - s(2.3)), (cx + s(1.8), cy - s(2.8))):
        L.rect(gx - SS // 2, gy - SS // 2, gx + SS // 2, gy + SS // 2, (255, 255, 252))
    # los DOS satélites
    for sx, sy in ((cx - s(6.6), cy - s(0.3)), (cx + s(6.4), cy + s(1.2))):
        L.poly([(sx, sy - s(1.1)), (sx + s(0.8), sy), (sx, sy + s(1.1)),
                (sx - s(0.8), sy)], (255, 232, 178))
        L.poly([(sx, sy - s(0.55)), (sx + s(0.4), sy), (sx, sy + s(0.55)),
                (sx - s(0.4), sy)], (255, 252, 242))
    # LA SOMBRA del cristal sobre el escalón
    shx, shy = cx, s(14.4)
    for dy in range(-SS // 2, SS // 2 + 1):
        for dx in range(-s(4), s(4) + 1):
            d = ((dx / s(4)) ** 2 + (dy / (SS / 2)) ** 2) ** 0.5
            if d <= 1.0:
                p = L.im.getpixel((int(shx + dx), int(shy + dy)))
                if len(p) >= 4 and p[3] > 0:
                    L.im.putpixel((int(shx + dx), int(shy + dy)),
                                  (int(p[0] * 0.62), int(p[1] * 0.62), int(p[2] * 0.68), p[3]))

    # LAS VETAS kintsugi mini
    for lado in (-1, 1):
        pts = []
        x = cx + lado * s(2)
        y = s(19.2)
        for k in range(4):
            x += lado * s(1.4)
            y += s(1.1)
            wob = (h01(lado, k, 43) - 0.5) * s(1)
            pts.append((x + wob, y))
        L.d.line(pts, fill=(255, 216, 130) + (245,), width=SS)

    # EL RUIDO
    L.ruido(0, 0, s(IW), s(IH), None, 0.055, 65047)

    return L.im.resize((IW, IH), Image.LANCZOS)


if __name__ == "__main__":
    t = build_tile()
    t.save("AethonMod/Content/Tiles/AncientAltar.png")
    i = build_item()
    i.save("AethonMod/Content/Items/Placeables/AncientAltarItem.png")
    print(" AncientAltar.png", t.size, "· AncientAltarItem.png", i.size)
