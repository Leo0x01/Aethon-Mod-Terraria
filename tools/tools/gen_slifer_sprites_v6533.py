#!/usr/bin/env python3
# =============================================================================
#  gen_slifer_sprites_v6533.py — LA RECONSTRUCCIÓN DEL DRAGÓN DEL CIELO
#
#  La petición del usuario (v6.50.33): «mejor rediseña al jefe completo,
#  podrias crear sprite segmentados basados en slifer, por ejemplo puedes
#  usar Devourer of Gods de Calamity sprites por segmento y rediseñarlos
#  cambiando el color y el arte». El enfoque de código puro de la v6.50.32
#  no convenció («no se parece en nada») — los SPRITES POR SEGMENTO son
#  ahora la vía (el patrón del DoG: cada segmento del gusano lleva su
#  propio sprite).
#
#  Este script genera TODO el set con un motor de pintura procedural
#  (PIL + supersampling ×3 + AO por blur + rim light + contorno):
#
#    AethonSierpeCabeza.png    — el cráneo de Slifer (240×150, mira a la
#                                DERECHA): máscara plateada del hocico,
#                                colmillos sable, corona de 5 llamas, ojo
#                                dorado, gema azul, franja de garganta.
#    AethonSierpeMandibula.png — la mandíbula inferior (168×132): el
#                                TRIÁNGULO INTERIOR oscuro (la boca que se
#                                abre girando sobre la bisagra), la SEGUNDA
#                                BOCA plateada y los dientes inferiores.
#    AethonSierpeVertebra.png  — el segmento del cuerpo (124×128): anillo
#                                escarlata, aleta dorsal en teja, 3 bandas
#                                de pizarra del vientre, filo frontal claro
#                                (la lectura de anillos encadenados).
#    AethonSierpeCola.png      — la cola (192×120): la pala espatulada con
#                                muescas del final, aletas menguantes.
#    AethonSierpeAla.png       — el ala-brazo de murciélago (264×212):
#                                hueso escarlata, 3 dedos ganchudos, garra
#                                de marfil, paño granate festoneado.
#    AethonBoss.png            — el retrato del bestiario (compuesto).
#    AethonBoss_Head_Boss.png  — el icono 32×32 de la barra de jefe.
#
#  La paleta es el canon VLM de las referencias del usuario:
#    rojo lomo #A61C1C · sombra #4A0E0E · vientre #2C2D35 · máscara
#    plateada #8A9299 · colmillos marfil #F0EAD6 · ojo oro #FFD700 ·
#    gema #35C5FF · corona->brasa #FF5A2A · paño ala #6B1515 ·
#    hueso ala #D42B2B.
# =============================================================================

import math
import os
from PIL import Image, ImageDraw, ImageFilter, ImageChops

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, "..", ".."))
NPCS = os.path.join(REPO, "AethonMod", "Content", "NPCs")
OUT_PREVIEW = os.path.join(HERE, "v6533_hoja_contacto.png")
OUT_MONTAGE = os.path.join(HERE, "v6533_montaje.png")

SS = 3  # supersampling

PAL = {
    "red":       (166, 28, 28),
    "red_mid":   (139, 20, 20),
    "red_dark":  (110, 14, 14),
    "red_shadow": (74, 12, 12),
    "red_deep":  (52, 8, 8),
    "red_light": (201, 58, 42),
    "red_hi":    (232, 97, 74),
    "ember":     (255, 90, 42),
    "ember_hi":  (255, 158, 74),
    "grey":      (138, 146, 153),
    "grey_dark": (92, 99, 105),
    "grey_deep": (69, 75, 82),
    "grey_hi":   (184, 192, 199),
    "slate":     (44, 45, 53),
    "slate_hi":  (72, 75, 88),
    "slate_dark": (27, 28, 34),
    "ivory":     (240, 234, 214),
    "ivory_sh":  (201, 194, 168),
    "ivory_dk":  (158, 151, 126),
    "gold":      (255, 215, 0),
    "gold_deep": (218, 165, 32),
    "gem":       (53, 197, 255),
    "gem_deep":  (24, 120, 200),
    "mouth":     (58, 13, 13),
    "mouth_deep": (30, 6, 6),
    "flesh":     (126, 36, 48),
    "bone":      (212, 43, 43),
    "bone_dk":   (142, 26, 26),
    "membrane":  (107, 21, 21),
    "membrane_d": (74, 14, 14),
    "outline":   (24, 8, 8),
}


def hx(c):
    return "#%02X%02X%02X" % c[:3]


# ----------------------------------------------------------------------------
#  EL MOTOR — lienzo supersampleado con capas y máscaras de material
# ----------------------------------------------------------------------------
class Lienzo:
    def __init__(self, w, h):
        self.w, self.h = w, h
        self.img = Image.new("RGBA", (w * SS, h * SS), (0, 0, 0, 0))
        self.dr = ImageDraw.Draw(self.img)
        # máscaras de material (modo L, a resolución SS)
        self.mask_all = Image.new("L", (w * SS, h * SS), 0)

    # -- primitivas -----------------------------------------------------
    def poly(self, pts, color, mask=None):
        p = [(x * SS, y * SS) for (x, y) in pts]
        self.dr.polygon(p, fill=color)
        self._stamp(p, mask)

    def ellipse(self, box, color, mask=None):
        b = [v * SS for v in box]
        self.dr.ellipse(b, fill=color)
        if mask is not None:
            m = Image.new("L", self.img.size, 0)
            ImageDraw.Draw(m).ellipse(b, fill=255)
            mask.paste(ImageChops.lighter(mask, m), (0, 0), ImageChops.lighter(mask, m))

    def line(self, pts, color, width, mask=None):
        p = [(x * SS, y * SS) for (x, y) in pts]
        self.dr.line(p, fill=color, width=int(width * SS), joint="curve")

    def _stamp(self, p, mask):
        if mask is not None:
            m = Image.new("L", self.img.size, 0)
            ImageDraw.Draw(m).polygon(p, fill=255)
            mask.paste(ImageChops.lighter(mask, m), (0, 0), ImageChops.lighter(mask, m))
            self.mask_all.paste(ImageChops.lighter(self.mask_all, m), (0, 0),
                                ImageChops.lighter(self.mask_all, m))
        else:
            m = Image.new("L", self.img.size, 0)
            ImageDraw.Draw(m).polygon(p, fill=255)
            self.mask_all.paste(ImageChops.lighter(self.mask_all, m), (0, 0),
                                ImageChops.lighter(self.mask_all, m))

    # -- sombreado ------------------------------------------------------
    def _shade(self, mask, blur, color, alpha, mode, bandas=True):
        """Difumina la máscara y la compone (multiply/screen) recortada.
        Con `bandas`: la máscara suave se POSTERIZA en 3 escalones duros
        (cel-shading — el veredicto VLM: cero degradés embarrados)."""
        if mask is None:
            mask = self.mask_all
        soft = mask.filter(ImageFilter.GaussianBlur(blur * SS))
        if bandas:
            soft = posterizar(soft)
        if mode == "mul":
            shade = Image.new("RGB", self.img.size, color)
            base_rgb = self.img.convert("RGB")
            out = ImageChops.multiply(base_rgb, shade)
        else:
            shade = Image.new("RGB", self.img.size, color)
            base_rgb = self.img.convert("RGB")
            out = ImageChops.screen(base_rgb, shade)
        out = out.convert("RGBA")
        out.putalpha(self.img.split()[3])
        self.img = Image.composite(out, self.img, soft)

    def ao(self, shapes, blur, strength=0.55):
        """Sombras de contacto: polígonos oscuros difuminados, recortados
        a la silueta total."""
        m = Image.new("L", self.img.size, 0)
        d = ImageDraw.Draw(m)
        for pts in shapes:
            d.polygon([(x * SS, y * SS) for (x, y) in pts], fill=255)
        soft = m.filter(ImageFilter.GaussianBlur(blur * SS))
        dark = Image.new("RGBA", self.img.size,
                         (PAL["red_deep"][0], PAL["red_deep"][1], PAL["red_deep"][2], 0))
        dark = Image.new("RGB", self.img.size, (int(255 * (1 - strength)),) * 3)
        base_rgb = self.img.convert("RGB")
        out = ImageChops.multiply(base_rgb, dark).convert("RGBA")
        out.putalpha(self.img.split()[3])
        self.img = Image.composite(out, self.img, soft)

    def glow(self, shapes, blur, color, alpha=90):
        """Añade brillo aditivo suave (screen) recortado a la silueta."""
        m = Image.new("L", self.img.size, 0)
        d = ImageDraw.Draw(m)
        for pts in shapes:
            if isinstance(pts, tuple) and len(pts) == 5:  # ellipse
                d.ellipse([v * SS for v in pts[:4]], fill=255)
            else:
                d.polygon([(x * SS, y * SS) for (x, y) in pts], fill=255)
        soft = m.filter(ImageFilter.GaussianBlur(blur * SS))
        light = Image.new("RGB", self.img.size, color)
        base_rgb = self.img.convert("RGB")
        out = ImageChops.screen(base_rgb, light).convert("RGBA")
        out.putalpha(self.img.split()[3])
        self.img = Image.composite(out, self.img, soft)

    def tint_region(self, shapes, color, alpha, blur=1.2):
        """Tinte plano suave recortado a la silueta (para acentos)."""
        m = Image.new("L", self.img.size, 0)
        d = ImageDraw.Draw(m)
        for pts in shapes:
            d.polygon([(x * SS, y * SS) for (x, y) in pts], fill=int(alpha * 2.55))
        soft = m.filter(ImageFilter.GaussianBlur(blur * SS))
        veil = Image.new("RGBA", self.img.size, color + (255,))
        veil.putalpha(ImageChops.multiply(soft, veil.split()[3]))
        self.img = Image.alpha_composite(self.img, veil)

    # -- acabado --------------------------------------------------------
    def outline(self, width=2.0, color=None):
        """Contorno oscuro exterior (la firma de Terraria): dilata la
        silueta y pinta el borde."""
        color = color or PAL["outline"]
        a = self.img.split()[3]
        dil = a.filter(ImageFilter.MaxFilter(int(width * SS) | 1))
        edge = ImageChops.subtract(dil, a)
        edge = edge.filter(ImageFilter.GaussianBlur(0.6 * SS))
        ring = Image.new("RGBA", self.img.size, color + (255,))
        ring.putalpha(edge)
        self.img = Image.alpha_composite(ring, self.img)

    def final(self):
        return self.img.resize((self.w, self.h), Image.LANCZOS)


def arco(dr, cx, cy, r, a0, a1, color, width, ss=SS):
    dr.arc([ (cx - r) * ss, (cy - r) * ss, (cx + r) * ss, (cy + r) * ss ],
           math.degrees(a0), math.degrees(a1), fill=color, width=int(width * ss))


def posterizar(mask, niveles=(0.34, 0.62, 0.85)):
    """Convierte una máscara suave en BANDAS DURAS (el cel-shading de la
    casa: 3 escalones nítidos en vez de degradés embarrados)."""
    lut = []
    for v in range(256):
        t = v / 255.0
        if t >= niveles[2]:
            lut.append(255)
        elif t >= niveles[1]:
            lut.append(178)
        elif t >= niveles[0]:
            lut.append(108)
        else:
            lut.append(0)
    return mask.point(lut)


# ----------------------------------------------------------------------------
#  1. LA CABEZA — el cráneo de Slifer (mira a la DERECHA)
# ----------------------------------------------------------------------------
def gen_cabeza():
    W, H = 240, 152
    L = Lienzo(W, H)
    m_red = Image.new("L", (W * SS, H * SS), 0)
    m_grey = Image.new("L", (W * SS, H * SS), 0)
    m_throat = Image.new("L", (W * SS, H * SS), 0)

    # ---- la línea de la boca (la referencia: sonrisa ligera en la punta)
    def boca(x):
        # curva suave del labio: comisura (100,92) -> (206,97) -> punta (232,88)
        if x < 100:
            return 92 - (100 - x) * 0.06
        if x < 206:
            t = (x - 100) / 106.0
            return 92 + 5 * math.sin(t * math.pi * 0.5)
        t = (x - 206) / 26.0
        return 97 - 9 * t * t

    # ---- LA SILUETA SUPERIOR (cráneo + hocico), rojo base — v2: la
    #      nuca angular, la ceja en GANCHO y el hocico plano (el veredicto
    #      VLM: nada de forma de globo; el perfil de Slifer es un PUÑAL)
    # la nuca angular — v3: la MORDIDA DE AJUSTE (el cuello se ESTRECHA
    # hacia atrás en punta redondeada: la primera vértebra lo solapa y la
    # unión lee «cuello», no «corte de sierra»)
    sil = []
    sil += [(2, 52), (5, 46), (14, 39), (30, 35), (48, 33)]
    # cráneo hasta la ceja (el pico de la ceja)
    sil += [(70, 29), (94, 29), (112, 33)]
    # EL GANCHO de la ceja: cae y vuelve a subir (el perfil cobra)
    sil += [(124, 40), (132, 43), (140, 42), (148, 43)]
    # lomo del hocico PLANO hasta la punta
    sil += [(168, 45), (192, 49), (212, 55), (226, 63)]
    # la punta ganchuda
    sil += [(235, 75), (233, 84), (226, 88)]
    # el labio (de punta a comisura)
    xs = [226, 216, 200, 184, 168, 152, 136, 120, 108, 100, 92, 80, 66, 50, 34, 18, 4]
    for x in reversed(xs):
        sil.append((x, boca(x)))
    # mejilla / garganta cerrada — el vientre del cuello también se estrecha
    sil += [(92, 104), (70, 106), (46, 102), (22, 92), (8, 78), (2, 62)]
    L.poly(sil, PAL["red"], mask=m_red)
    # el degradado del cuello hacia la nuca (se hunde en la sombra)
    L.poly([(2, 52), (30, 36), (48, 34), (44, 48), (14, 60), (6, 78), (2, 62)],
           PAL["red_mid"], mask=m_red)

    # BANDA DURA de luz en el lomo (cel-shading: el escalón de la frente)
    L.poly([(18, 36), (70, 31), (112, 35), (124, 41), (70, 40), (24, 44)],
           PAL["red_light"], mask=m_red)
    # BANDA DURA de sombra bajo el lomo del hocico
    L.poly([(148, 46), (192, 51), (212, 57), (224, 64), (216, 68),
            (176, 60), (150, 53)], PAL["red_mid"], mask=m_red)

    # ---- LA MÁSCARA PLATEADA (la referencia: cubre la punta del hocico
    #      superior) — v2: TRES TONOS DUROS, borde limpio, cero degradé
    mg = [(160, 44), (168, 45), (192, 49), (212, 55), (226, 63),
          (235, 75), (233, 84), (226, 88)]
    for x in reversed(xs):
        if x <= 216:
            mg.append((x, boca(x) - 1))
    mg += [(150, 62), (154, 52), (160, 44)]
    L.poly(mg, PAL["grey"], mask=m_grey)
    # banda superior clara de la máscara (el lentejuelo del acero)
    L.poly([(160, 45), (192, 50), (212, 56), (224, 64), (216, 66),
            (188, 58), (162, 52)], PAL["grey_hi"], mask=m_grey)
    # banda inferior oscura de la máscara (la mandíbula pesa)
    mg_low = []
    for x in xs:
        if 150 <= x <= 216:
            mg_low.append((x, boca(x) - 16))
    for x in reversed(xs):
        if 150 <= x <= 216:
            mg_low.append((x, boca(x) - 2))
    L.poly(mg_low, PAL["grey_dark"], mask=m_grey)
    # LA COSTURA: la línea dura de transición rojo->gris
    L.line([(160, 44), (154, 52), (150, 62), (150, 78), (154, 90)],
           PAL["grey_deep"], 2.0)

    # ---- LA GARGANTA OSCURA bajo el labio (se queda con la cabeza: cubre
    #      la rendija superior cuando la mandíbula gira)
    throat = []
    for x in xs:
        throat.append((x, boca(x)))
    for x in reversed(xs):
        if x <= 216:
            throat.append((x, boca(x) + 13))
    L.poly(throat, PAL["mouth"], mask=m_throat)

    # ---- LA CORONA DE 5 LLAMAS (la central la más alta, todas hacia atrás)
    # (raíz x, alto, ancho base) — 36 = línea del cráneo donde arraigan
    crown = [(58, 26, 20), (74, 38, 22), (92, 46, 24), (110, 36, 20), (124, 24, 16)]
    m_crown = Image.new("L", (W * SS, H * SS), 0)
    for (rx, alt, bw) in crown:
        x0 = rx - bw / 2
        x1 = rx + bw / 2
        tip = (rx - bw * 0.62, 36 - alt)
        # llama: base ancha, curva hacia atrás, punta fina
        L.poly([(x0, 38), (x0 + bw * 0.18, 38 - alt * 0.55),
                (tip[0], tip[1]), (x1, 38 - alt * 0.35), (x1, 38)],
               PAL["red_mid"], mask=m_crown)
    # puntas a brasa
    for (rx, alt, bw) in crown:
        tipx, tipy = rx - bw * 0.62, 36 - alt
        L.poly([(tipx + 4, tipy + 14), (tipx, tipy), (tipx + 7, tipy + 13)],
               PAL["ember"], mask=m_crown)

    # ---- LOS OJOS: almendra dorada rabiosa (v2: brasa viva — el núcleo
    #      casi blanco, el borde oro profundo, la visera negra gruesa)
    eye = [(114, 55), (128, 51), (147, 56), (151, 62), (138, 66), (120, 63)]
    L.poly(eye, (10, 5, 5))
    eye_in = [(118, 57), (130, 54), (145, 58), (147, 62), (135, 63), (121, 61)]
    L.poly(eye_in, PAL["gold_deep"])
    # el núcleo ardiente
    L.poly([(123, 58), (136, 56), (142, 60), (132, 62), (124, 61)], PAL["gold"])
    L.poly([(128, 59), (136, 58), (133, 61), (129, 61)], (255, 250, 225))
    # pupila rendija
    L.poly([(133, 57), (137, 59), (136, 62), (132, 61)], (26, 14, 3))
    # chispa fría (la carta: el acento cian)
    L.ellipse((141, 57, 145, 60), (168, 236, 255))
    # visera furiosa
    L.line([(113, 54), (128, 49), (149, 55)], (10, 5, 5), 4)
    L.line([(113, 54), (128, 50), (148, 55)], PAL["red_shadow"], 2)

    # ---- LA GEMA AZUL (rombo en la frente, base del pincho central)
    gx, gy = 103, 36
    L.poly([(gx, gy - 9), (gx + 7, gy), (gx, gy + 9), (gx - 7, gy)], PAL["gem_deep"])
    L.poly([(gx, gy - 6), (gx + 4.5, gy), (gx, gy + 6), (gx - 4.5, gy)], PAL["gem"])
    L.poly([(gx, gy - 3), (gx + 2, gy), (gx, gy + 3), (gx - 2, gy)], (210, 242, 255))

    # ---- FOSA NASAL (rendija oscura sobre el hocico gris)
    L.poly([(205, 57), (216, 60), (214, 63), (204, 60)], (44, 48, 52))

    # ---- DIENTES SUPERIORES (v2: cada clavija con SU sombra en la raíz,
    #      huecos oscuros entre dientes, tamaños alternos)
    tx = 118
    k = 0
    while tx <= 214:
        h = [5, 8, 6, 9, 5, 7][k % 6]
        wd = [5, 6, 5, 6, 5, 5][k % 6]
        yb = boca(tx + wd * 0.5) - 1
        # sombra de la encía sobre la raíz
        L.poly([(tx, yb), (tx + wd, yb), (tx + wd, yb + 3), (tx, yb + 3)],
               PAL["mouth_deep"])
        L.poly([(tx + 0.5, yb + 2), (tx + wd - 0.5, yb + 2),
                (tx + wd * 0.5, yb + h)], PAL["ivory"])
        # lado en sombra del diente
        L.poly([(tx + wd * 0.45, yb + 2), (tx + wd - 0.5, yb + 2),
                (tx + wd * 0.5, yb + h)], PAL["ivory_sh"])
        tx += wd + 3
        k += 1

    # ---- LOS COLMILLOS SABLE (sobresalen de la boca cerrada) — v2:
    #      dos tonos duros + línea de arranque oscura
    for (fx, fh, fw) in [(168, 34, 9), (206, 27, 8)]:
        yb = boca(fx) - 2
        curva = [(fx, yb), (fx + fw, yb),
                 (fx + fw - 1, yb + fh * 0.6),
                 (fx + fw * 0.55, yb + fh)]
        curva += [(fx + 1.5, yb + fh * 0.55)]
        L.poly(curva, PAL["ivory"])
        # la media caña en sombra (lado derecho)
        L.poly([(fx + fw * 0.5, yb), (fx + fw, yb),
                (fx + fw - 1, yb + fh * 0.6), (fx + fw * 0.55, yb + fh)],
               PAL["ivory_sh"])
        # el filo de luz (lado izquierdo)
        L.line([(fx + 1.5, yb + 2), (fx + 2.2, yb + fh * 0.5)],
               (255, 252, 240), 1.6)
        # arranque oscuro en la encía
        L.line([(fx, yb + 1), (fx + fw, yb + 1)], PAL["mouth_deep"], 2.2)

    # ---- SOMBRAS Y LUCES (v2: duras y cortas — cero embarrado)
    # AO bajo la ceja (la visera que sombra el ojo)
    L.poly([(112, 36), (150, 44), (148, 58), (110, 54)], PAL["red_dark"], mask=m_red)
    # AO de la mejilla
    L.poly([(96, 76), (140, 72), (132, 92), (94, 96)], PAL["red_mid"], mask=m_red)
    # AO del arranque de la corona
    L.ao([[(52, 30), (128, 30), (122, 40), (58, 40)]], 3, 0.4)
    # rim light del lomo (fino y duro)
    L.glow([[(20, 36), (80, 30), (148, 44), (222, 62), (150, 50), (30, 42)]],
           2, PAL["red_hi"], 1)

    # pliegues sutiles del hocico (la referencia: sin franjas duras)
    for i in range(3):
        x0 = 150 + i * 22
        L.line([(x0, boca(x0) - 16), (x0 + 5, boca(x0) - 6)], PAL["red_dark"], 1.6)

    # musculatura de la mejilla
    arco(L.dr, 108, 66, 34, math.pi * 0.62, math.pi * 0.95, PAL["red_dark"], 2.2)

    L.outline(2.0)
    return L.final()


# ----------------------------------------------------------------------------
#  2. LA MANDÍBULA — el interior de la boca + la segunda boca + los dientes
# ----------------------------------------------------------------------------
def gen_mandibula():
    W, H = 170, 134
    L = Lienzo(W, H)
    m_jaw = Image.new("L", (W * SS, H * SS), 0)
    m_int = Image.new("L", (W * SS, H * SS), 0)

    # LA BISAGRA está en (12, 50). La encía cerrada sigue la línea
    # labio+10 de la cabeza; el TRIÁNGULO INTERIOR sube hasta la altura
    # que la boca abierta necesita (se esconde tras el hocico al cerrar).
    hinge = (12, 50)

    # ---- LA BOCA OSCURA: triángulo desde la bisagra hasta la punta
    interior = [hinge, (160, 12), (156, 66), (108, 70), (40, 66), (16, 58)]
    L.poly(interior, PAL["mouth"], mask=m_int)

    # garganta más profunda atrás
    L.poly([hinge, (60, 44), (120, 46), (156, 66), (108, 70), (40, 66), (16, 58)],
           PAL["mouth_deep"], mask=m_int)
    # carne laterales
    L.poly([(24, 50), (70, 44), (120, 48), (150, 62), (140, 68), (60, 62), (26, 58)],
           PAL["flesh"], mask=m_int)

    # ---- LA SEGUNDA BOCA (la mandíbula interior plateada con dientes)
    sjx, sjy = 74, 50
    L.poly([(sjx - 20, sjy + 6), (sjx + 44, sjy - 2), (sjx + 50, sjy + 8),
            (sjx + 40, sjy + 16), (sjx - 18, sjy + 14)], PAL["grey_dark"])
    L.poly([(sjx - 16, sjy + 7), (sjx + 40, sjy + 1), (sjx + 44, sjy + 8),
            (sjx + 34, sjy + 13), (sjx - 14, sjy + 12)], PAL["grey"])
    # dientes de la segunda boca
    for i in range(6):
        tx = sjx - 10 + i * 9
        L.poly([(tx, sjy + 2 + i * 0.4), (tx + 4, sjy + 3 + i * 0.4),
                (tx + 2, sjy + 9)], PAL["ivory"])
    # interior rosa de la segunda boca
    L.poly([(sjx + 14, sjy + 5), (sjx + 34, sjy + 3), (sjx + 30, sjy + 11),
            (sjx + 16, sjy + 11)], PAL["flesh"])

    # ---- EL CUERPO DE LA MANDÍBULA (plateada entera — el canon) — v3:
    #      el VIENTRE CURVO (un arco que cae hacia el mentón y remonta a
    #      la punta: biología, no ladrillo) y el mentón en PICO redondeado
    jaw = [(20, 46), (60, 52), (110, 60), (148, 65),
           (157, 70), (156, 77), (146, 86), (122, 94), (92, 95),
           (62, 90), (38, 78), (24, 64), (18, 54)]
    L.poly(jaw, PAL["grey"], mask=m_jaw)
    # banda de encía superior
    L.poly([(20, 46), (60, 52), (110, 60), (148, 65), (144, 73),
            (104, 67), (56, 61), (22, 55)], PAL["grey_deep"], mask=m_jaw)
    # la VENTRECHA — arco inferior oscuro (el volumen curvo de abajo)
    L.poly([(150, 74), (128, 88), (96, 94), (64, 88), (40, 76), (34, 70),
            (66, 82), (100, 88), (132, 82), (148, 72)], PAL["grey_dark"], mask=m_jaw)
    # las líneas de flujo del hueso (las vetas del mentón)
    L.line([(46, 66), (78, 72), (112, 79), (140, 82)], PAL["grey_deep"], 1.4)
    L.line([(40, 72), (72, 79), (104, 86), (130, 88)], PAL["grey_dark"], 1.2)

    # ---- DIENTES INFERIORES (v3: tamaños e INCLINACIONES alternos — el
    #      patrón de sierra muere; colmillo pequeño al frente)
    pat = [(5, 7, 0.12), (6, 10, -0.08), (5, 8, 0.10), (7, 12, -0.12),
           (5, 8, 0.08), (6, 11, -0.10), (5, 7, 0.12), (6, 13, -0.15),
           (5, 8, 0.10), (6, 10, -0.08), (5, 7, 0.12)]
    tx = 26
    k = 0
    while tx <= 138:
        wd, h, tilt = pat[k % len(pat)]
        base_y = 50 + (tx / 150.0) * 15
        tipx = tx + wd * 0.5 + tilt * 10
        L.poly([(tx, base_y + 2), (tx + wd, base_y + 2.5),
                (tipx, base_y - h)], PAL["ivory"])
        L.poly([(tx + wd * 0.5, base_y + 2.2), (tx + wd, base_y + 2.5),
                (tipx, base_y - h)], PAL["ivory_sh"])
        tx += wd + 4
        k += 1

    # ---- LA CARNE ROJA DE LA BISAGRA (v2: nudo pequeño y limpio)
    L.ellipse((6, 40, 26, 62), PAL["red_dark"])
    L.ellipse((10, 45, 22, 58), PAL["red_mid"])
    L.ellipse((13, 48, 19, 55), PAL["red"])

    # ---- SOMBREADO (v2: bandas duras)
    L._shade(m_jaw, 10, (148, 154, 160), 1, "mul")   # gris oscurece abajo
    L.glow([[(24, 50), (80, 56), (140, 70)]], 2, PAL["grey_hi"], 1)
    L.outline(2.0)
    return L.final()


# ----------------------------------------------------------------------------
#  3. LA VÉRTEBRA — el anillo del cuerpo con aleta dorsal y vientre de pizarra
# ----------------------------------------------------------------------------
def gen_vertebra():
    W, H = 124, 130
    L = Lienzo(W, H)
    m_body = Image.new("L", (W * SS, H * SS), 0)
    m_fin = Image.new("L", (W * SS, H * SS), 0)
    m_belly = Image.new("L", (W * SS, H * SS), 0)

    # ---- EL ANILLO (v3: ANGULOSO — el caparazón segmentado del DoG: los
    #      costados APLANADOS y los hombros en CUÑA, no un óvalo de judía)
    ring = [(8, 62), (10, 46), (20, 34), (40, 28), (58, 27), (62, 27),
            (84, 27), (104, 34), (114, 46), (116, 62), (114, 80), (104, 94),
            (84, 100), (62, 101), (58, 101), (40, 100), (20, 94), (10, 80)]
    L.poly(ring, PAL["red"], mask=m_body)
    # las CUÑAS laterales (las costillas que marcan el segmento)
    L.poly([(10, 46), (24, 38), (28, 62), (24, 88), (10, 80)],
           PAL["red_mid"], mask=m_body)
    L.poly([(116, 46), (102, 38), (98, 62), (102, 88), (116, 80)],
           PAL["red_mid"], mask=m_body)

    # ---- LA ALETA DORSAL (triangular, en teja hacia atrás, ancha de base)
    fin = [(30, 32), (44, 6), (58, 4), (52, 30)]
    L.poly(fin, PAL["red_mid"], mask=m_fin)
    # filo claro de la aleta
    L.line([(30, 32), (44, 7), (57, 5)], PAL["red_light"], 2.4)
    # brasa en la punta
    L.poly([(48, 4), (58, 5), (52, 12)], PAL["ember"])

    # ---- LAS BANDAS DE PIZARRA DEL VIENTRE (3 collares blindados)
    for i, (bx, bw) in enumerate([(16, 28), (50, 30), (86, 26)]):
        by = 72 + (i % 2) * 2
        band = [(bx, by), (bx + bw, by - 3), (bx + bw + 6, by + 8),
                (bx + bw - 4, by + 18), (bx + 2, by + 18)]
        L.poly(band, PAL["slate"], mask=m_belly)
        # filo superior de la banda
        L.line([(bx + 1, by + 1.5), (bx + bw, by - 1.5)], PAL["slate_hi"], 2)
    # sombra inferior del vientre
    L.poly([(14, 88), (110, 84), (104, 98), (76, 100), (48, 100), (24, 96)],
           PAL["slate_dark"], mask=m_belly)

    # ---- EL FILADO FRONTAL (borde derecho claro: el anillo que se lee
    #      encadenado cuando los segmentos se solapan)
    L.poly([(104, 36), (116, 48), (118, 76), (104, 90), (98, 84),
            (108, 62), (98, 44)], PAL["red_light"], mask=m_body)
    # sombra trasera (izquierda)
    L.poly([(8, 62), (12, 44), (24, 32), (34, 30), (26, 62), (34, 94),
            (24, 92), (12, 80)], PAL["red_dark"], mask=m_body)

    # ---- COSTILLAS DE ESCAMA (v2: CHEVRONES en dos filas — el lomo
    #      escamado que lee «sierpe», con dos tonos duros por chevrón)
    for row in range(2):
        ry = 44 + row * 22
        rh = 11 - row
        cx = 30 + row * 7
        while cx < 104:
            # el chevrón: arco apuntando atrás (hacia la izquierda)
            L.poly([(cx, ry), (cx + 13, ry - 4), (cx + 15, ry + rh * 0.4),
                    (cx + 6, ry + rh), (cx - 3, ry + rh * 0.5)],
                   PAL["red_mid"], mask=m_body)
            L.line([(cx + 1, ry + 1), (cx + 13, ry - 3)], PAL["red_light"], 1.4)
            cx += 15

    # ---- SOMBRAS Y LUCES (v2: cilindro por BANDAS DURAS)
    # la banda de luz del lomo (el escalón superior del cilindro)
    L.poly([(12, 42), (30, 32), (92, 32), (110, 42), (100, 46),
            (30, 38), (18, 46)], PAL["red_light"], mask=m_body)
    # la banda de sombra inferior (el escalón que funde con el vientre)
    L.poly([(14, 78), (108, 74), (100, 92), (76, 98), (48, 98), (24, 92)],
           PAL["red_dark"], mask=m_body)
    L.glow([[(20, 34), (62, 28), (102, 34)]], 2, PAL["red_hi"], 1)
    L.ao([[(24, 30), (60, 30), (56, 38), (28, 38)]], 3, 0.45)  # base de la aleta
    L.outline(2.0)
    return L.final()


# ----------------------------------------------------------------------------
#  4. LA COLA — la pala espatulada con muescas
# ----------------------------------------------------------------------------
def gen_cola():
    W, H = 196, 122
    L = Lienzo(W, H)
    m_body = Image.new("L", (W * SS, H * SS), 0)
    m_fluke = Image.new("L", (W * SS, H * SS), 0)
    m_belly = Image.new("L", (W * SS, H * SS), 0)

    # ---- EL CUERPO DE LA COLA (base ancha a la derecha, menguando):
    # silueta simétrica respecto del eje y=61 — v2: el borde derecho
    #      ENCURVADO (la unión con la última vértebra, no un corte recto)
    body = [(192, 30), (194, 44), (194, 78), (192, 92), (160, 92), (128, 82),
            (102, 70), (86, 61), (102, 52), (132, 40), (164, 30)]
    L.poly(body, PAL["red"], mask=m_body)

    # ---- LA PALA DEL FINAL (espatulada, vertical, con muescas) — v3:
    #      el lóbulo SUPERIOR más grande (la asimetría viva del anime)
    fluke = [(92, 56), (74, 38), (56, 18), (36, 8), (18, 5), (6, 20),
             (4, 40), (13, 49), (10, 59), (17, 69), (12, 79), (22, 92),
             (40, 103), (58, 95), (74, 78), (90, 64)]
    L.poly(fluke, PAL["red_mid"], mask=m_fluke)
    # muescas (los dientes de la pala)
    for (nx, ny, w, h) in [(20, 12, 7, 10), (9, 45, 8, 8), (10, 74, 8, 10),
                            (28, 97, 7, 8), (44, 12, 7, 9), (46, 96, 7, 7)]:
        L.poly([(nx, ny), (nx + w, ny - h * 0.4), (nx + w * 0.5, ny + h)],
               PAL["mouth_deep"])
    # nervio central de la pala
    L.line([(88, 58), (40, 54), (12, 50)], PAL["red_dark"], 3)
    L.line([(88, 57), (44, 53), (14, 49)], PAL["red_light"], 1.4)
    # puntas a brasa
    L.poly([(6, 38), (14, 42), (8, 46)], PAL["ember"])
    L.poly([(6, 74), (14, 70), (8, 66)], PAL["ember"])

    # ---- ALETAS DORSALES MENGUANTES hacia la pala
    for (fx, fh) in [(172, 22), (150, 18), (130, 14), (112, 10)]:
        L.poly([(fx + 8, 27), (fx - fh * 0.7, 27 - fh), (fx - 4, 28)],
               PAL["red_mid"])
        L.line([(fx + 7, 27), (fx - fh * 0.7 + 2, 27 - fh + 2)], PAL["red_light"], 1.6)

    # ---- BANDAS DEL VIENTRE (continúan las del cuerpo, menguando)
    for (bx, bw, by, bh) in [(160, 30, 82, 14), (128, 24, 76, 11), (104, 16, 68, 8)]:
        L.poly([(bx, by), (bx + bw, by - 2), (bx + bw - 5, by + bh), (bx + 2, by + bh)],
               PAL["slate"], mask=m_belly)
        L.line([(bx + 1, by + 1), (bx + bw - 1, by - 1)], PAL["slate_hi"], 1.6)

    # ---- FILADO y sombras
    L.poly([(190, 30), (194, 40), (194, 84), (188, 92), (182, 86), (188, 60), (182, 36)],
           PAL["red_light"], mask=m_body)
    L._shade(m_body, 12, (196, 208, 216), 1, "mul")
    L._shade(m_fluke, 10, (188, 200, 210), 1, "mul")
    L.glow([[(160, 24), (130, 30), (104, 44), (106, 50), (136, 36), (162, 30)]],
           3, PAL["red_hi"], 1)
    L.ao([[(100, 50), (92, 40), (80, 34), (76, 44), (88, 60)]], 5, 0.4)
    L.outline(2.0)
    return L.final()


# ----------------------------------------------------------------------------
#  5. EL ALA — el ala-brazo de murciélago (base abajo, se abre hacia arriba)
# ----------------------------------------------------------------------------
def gen_ala():
    W, H = 266, 214
    L = Lienzo(W, H)
    m_mem = Image.new("L", (W * SS, H * SS), 0)
    m_bone = Image.new("L", (W * SS, H * SS), 0)

    root = (168, 202)
    wrist = (96, 88)

    def strut(p0, p1, bow, w0, w1, color, mask):
        """Un hueso curvado: polilínea con grosor menguante."""
        (x0, y0), (x1, y1) = p0, p1
        mx, my = (x0 + x1) / 2 + bow[0], (y0 + y1) / 2 + bow[1]
        n = 14
        pts = []
        for i in range(n + 1):
            t = i / n
            # bezier cuadrática
            bx = (1 - t) ** 2 * x0 + 2 * (1 - t) * t * mx + t ** 2 * x1
            by = (1 - t) ** 2 * y0 + 2 * (1 - t) * t * my + t ** 2 * y1
            pts.append((bx, by))
        for i in range(n):
            w = w0 + (w1 - w0) * (i / n)
            a, b = pts[i], pts[i + 1]
            dx, dy = b[0] - a[0], b[1] - a[1]
            ln = math.hypot(dx, dy) or 1
            nx, ny = -dy / ln * w / 2, dx / ln * w / 2
            L.poly([(a[0] - nx, a[1] - ny), (b[0] - nx, b[1] - ny),
                    (b[0] + nx, b[1] + ny), (a[0] + nx, a[1] + ny)], color, mask)

    # ---- EL PAÑO PRIMERO (v2 — el veredicto VLM: el paño TAPABA los
    #      huesos; ahora los dedos se pintan ENCIMA y el ala tiene osatura)
    # PANEL A (F1 -> F2): festones hacia la muñeca
    F1 = (238, 26)   # delantero
    F2 = (168, 10)   # central
    F3 = (54, 26)    # trasero

    pa = [wrist, F1]
    edge = [F1]
    for (k, bow) in enumerate([(28, 26), (12, 22)]):
        t = (k + 1) / 3
        x = F1[0] + (F2[0] - F1[0]) * t
        y = F1[1] + (F2[1] - F1[1]) * t
        cx = x + (wrist[0] - x) * 0.30
        cy = y + (wrist[1] - y) * 0.30
        edge += [(cx, cy), (x + bow[0] * 0.2, y + bow[1] * 0.2)]
    edge.append(F2)
    L.poly([wrist] + pa[:1] + edge + [wrist], PAL["membrane"], mask=m_mem)

    # PANEL B (F2 -> F3)
    edgeb = [F2]
    for (k, bow) in enumerate([(-6, 18), (-14, 8)]):
        t = (k + 1) / 3
        x = F2[0] + (F3[0] - F2[0]) * t
        y = F2[1] + (F3[1] - F2[1]) * t
        cx = x + (wrist[0] - x) * 0.30
        cy = y + (wrist[1] - y) * 0.30
        edgeb += [(cx, cy), (x, y)]
    edgeb.append(F3)
    L.poly([wrist, F2] + edgeb + [wrist], PAL["membrane"], mask=m_mem)

    # PANEL C (F3 -> raíz): el paño interno
    edgec = [F3]
    for (k, bow) in enumerate([(-18, 30)]):
        t = (k + 1) / 2
        x = F3[0] + (root[0] - F3[0]) * t
        y = F3[1] + (root[1] - F3[1]) * t
        cx = x + (wrist[0] - x) * 0.26
        cy = y + (wrist[1] - y) * 0.26
        edgec += [(cx, cy), (x, y)]
    edgec.append(root)
    L.poly([wrist, F3] + edgec + [root, wrist], PAL["membrane"], mask=m_mem)

    # venas del paño (v3: LÍNEAS DE TENSIÓN CURVAS — arcos que se comban
    # hacia el borde de fuga, como el paño tenso de verdad)
    for (tip, sag) in [(F1, 14), (F2, 10), (F3, 8)]:
        mx = (wrist[0] + tip[0]) / 2 + sag
        my = (wrist[1] + tip[1]) / 2 + sag * 0.55
        n = 10
        pts = []
        for i in range(n + 1):
            t = i / n
            bx = (1 - t) ** 2 * wrist[0] + 2 * (1 - t) * t * mx + t ** 2 * tip[0]
            by = (1 - t) ** 2 * wrist[1] + 2 * (1 - t) * t * my + t ** 2 * tip[1]
            pts.append((bx, by))
        L.line(pts, PAL["membrane_d"], 1.8)

    # degradado del paño: más oscuro hacia los bordes de fuga
    L._shade(m_mem, 18, (172, 184, 196), 1, "mul")

    # ---- LOS TRES DEDOS (ganchudos) — ENCIMA del paño
    strut(wrist, F1, (30, 26), 9, 3.4, PAL["bone"], m_bone)
    strut(wrist, F2, (6, -10), 9, 3.2, PAL["bone"], m_bone)
    strut(wrist, F3, (-16, 14), 8.4, 3.0, PAL["bone"], m_bone)
    # ---- EL BRAZO (hombro -> muñeca)
    strut(root, wrist, (46, -18), 15, 9.5, PAL["bone"], m_bone)
    # la muñeca (nudillo limpio: círculo con núcleo de luz)
    L.ellipse((wrist[0] - 11, wrist[1] - 11, wrist[0] + 11, wrist[1] + 11),
              PAL["bone"], mask=m_bone)
    L.ellipse((wrist[0] - 7, wrist[1] - 7, wrist[0] + 7, wrist[1] + 7),
              PAL["red_light"], mask=m_bone)

    # ---- LA GARRA DE MARFIL (pulgar en el borde de ataque)
    L.poly([(wrist[0] - 4, wrist[1] - 10), (wrist[0] - 26, wrist[1] - 34),
            (wrist[0] - 20, wrist[1] - 38), (wrist[0] - 1, wrist[1] - 18)],
           PAL["ivory"])
    L.poly([(wrist[0] - 6, wrist[1] - 14), (wrist[0] - 22, wrist[1] - 32),
            (wrist[0] - 20, wrist[1] - 35)], PAL["ivory_sh"])

    # ---- SOMBRAS Y LUCES del hueso
    L._shade(m_bone, 6, (176, 188, 198), 1, "mul")
    L.glow([[(root[0] - 8, root[1] - 4), (120, 140), (wrist[0] + 4, wrist[1] + 6),
             (170, 40), (236, 30), (170, 16), (58, 30)]], 4, (255, 120, 80), 1)
    # brasa en las puntas de los dedos
    for tip in [F1, F2, F3]:
        L.ellipse((tip[0] - 3.4, tip[1] - 3.4, tip[0] + 3.4, tip[1] + 3.4), PAL["ember"])

    # el borde de ataque con filo cálido
    L.line([root, (128, 130), wrist], PAL["red_light"], 2.2)
    L.line([wrist, (172, 52), F1], PAL["red_light"], 2.0)

    L.outline(2.0)
    return L.final()


# ----------------------------------------------------------------------------
#  6. EL RETRATO y EL ICONO
# ----------------------------------------------------------------------------
def gen_retrato(cabeza, mandibula):
    """Compuesto: cabeza + mandíbula cerrada, encuadrado para el bestiario."""
    W, H = 128, 104
    comp = Image.new("RGBA", (240, 152), (0, 0, 0, 0))
    # mandíbula cerrada: bisagra de la mandíbula (12,50) -> bisagra cabeza (102,96)
    comp.alpha_composite(mandibula, (102 - 12, 96 - 50))
    comp.alpha_composite(cabeza, (0, 0))
    # encuadre: recorte del cráneo (de la nuca a los colmillos)
    box = (4, 2, 238, 118)
    comp = comp.crop(box)
    comp = comp.resize((W, H), Image.LANCZOS)
    return comp


def gen_icono(cabeza, mandibula):
    """v3 — el icono 32×32 de GOLPE: silhouette BOLD — la cuña roja LLENA
    el lienzo, la máscara ocupa el tercio delantero, el ojo ORO 3×2 con
    núcleo blanco, DOS colmillos gruesos, corona de 3 llamas altas."""
    im = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)

    def P(pts, c):
        d.polygon(pts, fill=c)

    # LA CUÑA del cráneo — grande, llena el marco
    P([(0, 16), (3, 10), (10, 7), (18, 8), (25, 11), (30, 16), (31, 20),
       (27, 24), (18, 27), (8, 27), (2, 23), (0, 20)], PAL["red"])
    # luz del lomo
    P([(2, 13), (10, 9), (18, 10), (24, 13), (18, 13), (10, 12)], PAL["red_light"])
    # LA CORONA — 3 llamas altas con brasa
    P([(5, 10), (7, 0), (11, 9)], PAL["red_mid"])
    P([(10, 9), (14, -1), (18, 9)], PAL["red_mid"])
    P([(17, 9), (22, 2), (24, 11)], PAL["red_mid"])
    d.point((7, 1), PAL["ember"])
    d.point((14, 0), PAL["ember_hi"])
    d.point((22, 3), PAL["ember"])
    # LA MÁSCARA — tercio delantero, dos tonos
    P([(24, 10), (30, 16), (31, 20), (26, 24), (19, 24), (17, 18), (19, 12)],
       PAL["grey"])
    P([(24, 12), (29, 16), (27, 20), (20, 21), (19, 15)], PAL["grey_hi"])
    # EL OJO — oro 4×3, núcleo blanco (el pixel que CANTA)
    P([(17, 14), (22, 12), (25, 15), (20, 18)], (10, 5, 5))
    P([(18, 15), (21, 13), (23, 15), (20, 17)], PAL["gold"])
    d.point((21, 14), (255, 250, 225))
    # LA BOCA + DOS COLMILLOS gruesos
    d.line([(18, 23), (28, 22)], PAL["red_shadow"], 1)
    P([(21, 23), (24, 23), (23, 30)], PAL["ivory"])
    P([(26, 23), (29, 23), (28, 29)], PAL["ivory"])
    # contorno
    a = im.split()[3]
    dil = a.filter(ImageFilter.MaxFilter(3))
    edge = ImageChops.subtract(dil, a)
    ring = Image.new("RGBA", (32, 32), PAL["outline"] + (255,))
    ring.putalpha(edge)
    im = Image.alpha_composite(ring, im)
    return im


# ----------------------------------------------------------------------------
#  LA HOJA DE CONTACTO y EL MONTAJE (para el control de calidad con VLM)
# ----------------------------------------------------------------------------
def checker(w, h, s=8):
    bg = Image.new("RGB", (w, h), (58, 54, 62))
    d = ImageDraw.Draw(bg)
    for y in range(0, h, s):
        for x in range(0, w, s):
            if (x // s + y // s) % 2 == 0:
                d.rectangle([x, y, x + s - 1, y + s - 1], fill=(48, 44, 52))
    return bg


def hoja_contacto(sprites):
    tiles = [(name, im.copy()) for name, im in sprites.items()]
    W = 1180
    H = 700
    sheet = checker(W, H)
    d = ImageDraw.Draw(sheet)
    x, y = 24, 30
    for name, im in tiles:
        w, h = im.size
        sheet.paste(im, (x, y), im)
        d.text((x, y + h + 6), "%s (%dx%d)" % (name, w, h), fill=(230, 225, 210))
        x += max(w, 150) + 40
        if x > W - 300:
            x = 24
            y += 210
    return sheet


def montaje(sprites):
    """Simulación in-game aproximada: cielo atardecer + la cadena montada."""
    cabeza = sprites["AethonSierpeCabeza.png"]
    mandibula = sprites["AethonSierpeMandibula.png"]
    vertebra = sprites["AethonSierpeVertebra.png"]
    cola = sprites["AethonSierpeCola.png"]
    ala = sprites["AethonSierpeAla.png"]

    W, H = 1180, 640
    sky = Image.new("RGB", (W, H), (86, 44, 66))
    d = ImageDraw.Draw(sky)
    for i in range(H):
        t = i / H
        c = (int(60 + 70 * t), int(26 + 30 * t), int(48 + 24 * t))
        d.line([(0, i), (W, i)], fill=c)
    # suelo lejano
    d.rectangle([0, H - 90, W, H], fill=(34, 24, 30))
    d.rectangle([0, H - 90, W, H - 84], fill=(70, 46, 52))
    scene = sky.convert("RGBA")

    import random
    rnd = random.Random(2026)

    def draw_seg(im, cx, cy, rot, scale=1.0, tint=None, flip=False):
        w, h = im.size
        im2 = im.resize((int(w * scale), int(h * scale)), Image.LANCZOS)
        if flip:
            im2 = im2.transpose(Image.FLIP_TOP_BOTTOM)
        im2 = im2.rotate(math.degrees(rot), Image.BICUBIC, expand=True)
        if tint:
            r, g, b, a = im2.split()
            solid = Image.new("RGBA", im2.size, tint + (255,))
            solid.putalpha(a.point(lambda v: v))
            im2 = Image.blend(im2, solid, 0.0)
        scene.alpha_composite(im2, (int(cx - im2.width / 2), int(cy - im2.height / 2)))

    # LA COLUMNA: una onda serpenteante de 12 segmentos + cola, cabeza al frente
    pts = []
    x0, y0 = 940, 240
    for i in range(13):
        t = i / 12.0
        px = x0 - t * 780
        py = y0 + math.sin(t * math.pi * 2.1 + 0.6) * 90 + t * 60
        pts.append((px, py))

    # alas (detrás): ancladas al 3er segmento
    sx, sy = pts[2]
    flap = math.radians(-24)
    ala_far = ala.transpose(Image.FLIP_LEFT_RIGHT)
    for (im_al, extra) in [(ala_far, -0.30), (ala, 0.22)]:
        w, h = im_al.size
        im2 = im_al.resize((int(w * 0.9), int(h * 0.9)), Image.LANCZOS)
        im2 = im2.rotate(math.degrees(flap + extra), Image.BICUBIC, expand=True)
        if im_al is ala_far:
            r, g, b, a = im2.split()
            dark = Image.new("RGBA", im2.size, (40, 10, 10, 255))
            dark.putalpha(a.point(lambda v: int(v * 0.62)))
            im2 = dark
        scene.alpha_composite(im2, (int(sx - im2.width / 2 + 26), int(sy - im2.height / 2 - 46)))

    # segmentos (de la cola a la cabeza: los de adelante encima)
    for i in range(len(pts) - 1, -1, -1):
        px, py = pts[i]
        nxt = pts[max(0, i - 1)]
        rot = math.degrees(math.atan2(nxt[1] - py, nxt[0] - px))
        taper = 1.06 - i * 0.028
        draw_seg(vertebra, px, py, rot, scale=taper)
    # la cola
    tx, ty = pts[-1][0] - 60, pts[-1][1] + 26
    draw_seg(cola, tx, ty, 14, scale=0.82)
    # la cabeza
    hx, hy = pts[0][0] + 58, pts[0][1] - 8
    rumbo = math.degrees(math.atan2(pts[0][1] - pts[1][1], pts[0][0] - pts[1][0]))
    # mandíbula entreabierta
    w, h = mandibula.size
    mand = mandibula.rotate(math.degrees(0.20), Image.BICUBIC, expand=True)
    # bisagra mandíbula local (12,50) tras rotación ≈ desplazada; aproximo
    hxm, hym = hx - 102 + 12, hy - 96 + 50
    mand.putalpha(mand.split()[3])
    mm = mand
    scene.alpha_composite(mm, (int(hxm - (mm.width - w) / 2), int(hym - (mm.height - h) / 2)))
    draw_seg(cabeza, hx, hy, rumbo)
    return scene.convert("RGB")


# ----------------------------------------------------------------------------
def main():
    print("Generando el set de sprites de Slifer…")
    cabeza = gen_cabeza()
    mandibula = gen_mandibula()
    vertebra = gen_vertebra()
    cola = gen_cola()
    ala = gen_ala()
    retrato = gen_retrato(cabeza, mandibula)
    icono = gen_icono(cabeza, mandibula)

    out = {
        "AethonSierpeCabeza.png": cabeza,
        "AethonSierpeMandibula.png": mandibula,
        "AethonSierpeVertebra.png": vertebra,
        "AethonSierpeCola.png": cola,
        "AethonSierpeAla.png": ala,
        "AethonBoss.png": retrato,
        "AethonBoss_Head_Boss.png": icono,
    }
    for name, im in out.items():
        path = os.path.join(NPCS, name)
        im.save(path)
        print("  ✓ %s (%dx%d) -> %s" % (name, im.width, im.height, path))

    hoja_contacto(out).save(OUT_PREVIEW)
    montaje(out).save(OUT_MONTAGE)
    print("  ✓ hoja de contacto -> %s" % OUT_PREVIEW)
    print("  ✓ montaje -> %s" % OUT_MONTAGE)


if __name__ == "__main__":
    main()
