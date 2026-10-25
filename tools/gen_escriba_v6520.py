#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
v6.52.0 — LAS TEXTURAS DEL CICLO DEL ESCRIBA (Task 84).

Genera (PIL, determinista — el patrón de la casa: tools/gen_*.py):
  · Content/Items/Bolsas/BolsaDelEscriba.png      (30×30 — la bolsa)
  · Content/Weapons/Escriba/PrimerTrazo.png       (38×38 — el arma de rayo)
  · Content/Weapons/Escriba/PlumaConsciente.png   (38×38 — el arma viva)
  · Content/Items/Escriba/PlumaConvocadora.png    (30×30 — el convocador)
  · Content/NPCs/Escriba/EscribaJefe_Head_Boss.png  (32×32 — el icono del mapa)
  · Content/NPCs/Escriba/EscribaJefe_PoseBestiario.png (112×112 — la pose)
  · Content/Effects/Procedural/RuidoTinta.png     (256×64 — LA TEXTURA DE
    RUIDO DEL RAYO: fBm horizontal elongado + caída gaussiana vertical,
    la receta R2 §1d — «el Perlin moviéndose en una dirección»)
"""
import math
import os
import random
from PIL import Image, ImageDraw, ImageFilter

RAIZ = "/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod"

# LA PALETA DEL ESCRIBA (tinta / página / violeta)
TINTA = (24, 14, 38)
TINTA_MEDIA = (58, 38, 92)
VIOLETA = (140, 96, 214)
VIOLETA_CLARO = (186, 150, 240)
BLANCO = (245, 242, 252)
ORO = (255, 226, 160)


def png(ruta, img):
    camino = os.path.join(RAIZ, ruta)
    os.makedirs(os.path.dirname(camino), exist_ok=True)
    img.save(camino)
    print("·", ruta, img.size)


# ==================================================================
#  LA PLUMA (la forma base de TODO el ciclo: un dibujo a mano con
#  curvas — el asta y la barba del plumaje)
# ==================================================================
def dibujar_pluma(draw, cx, cy, escala, color_asto, color_barba, brillo=None):
    """La pluma diagonal: punta abajo-izquierda, plumero arriba-derecha."""
    # EL ASTA: línea diagonal con curva suave.
    punta = (cx - 12 * escala, cy + 13 * escala)
    tope = (cx + 11 * escala, cy - 13 * escala)
    # la barba: un óvalo alargado sobre el asta + muescas (3 arcos)
    draw.line([punta, tope], fill=color_asto, width=max(1, int(2 * escala)))
    # EL PLUMERO: óvalo rotado aprox por puntos (polígono).
    import math as m
    puntos = []
    for i in range(28):
        t = i / 27
        ang = m.pi * 0.25
        # elipse alargada a lo largo del asta, un lado
        lx = (t - 0.5) * 20 * escala
        ly = -m.sin(t * m.pi) * 7.5 * escala
        x = cx + lx * m.cos(ang) - ly * m.sin(ang) * 0.6
        y = cy - 4 * escala + lx * m.sin(ang) * 0.55 + ly * m.cos(ang)
        puntos.append((x, y))
    draw.polygon(puntos, fill=color_barba)
    # LAS MUESCAS del plumaje (dos cortes oscuros).
    for k in (0.35, 0.62):
        cort = (cx + (k - 0.5) * 20 * escala * 0.95, cy - 4 * escala + (k - 0.5) * 11 * escala)
        draw.line([cort, (cort[0] + 3 * escala, cort[1] + 5 * escala)],
                  fill=TINTA, width=max(1, int(1.4 * escala)))
    if brillo:
        draw.ellipse([cx - 1 * escala, cy - 1 * escala, cx + 2 * escala, cy + 2 * escala],
                     fill=brillo)


def bolsa_del_escriba():
    img = Image.new("RGBA", (30, 30), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    # EL SACO: forma de bolsa redondeada violeta oscura.
    d.polygon([(8, 10), (22, 10), (26, 18), (24, 26), (6, 26), (4, 18)], fill=TINTA_MEDIA)
    d.polygon([(10, 11), (20, 11), (23, 17), (21, 24), (9, 24), (7, 17)], fill=(80, 54, 128))
    # EL CORDel dorado.
    d.line([(9, 10), (12, 6), (15, 9), (18, 6), (21, 10)], fill=ORO, width=2)
    # LA PLUMA BLANCA asomando del saco.
    dibujar_pluma(d, 15, 18, 0.55, BLANCO, (222, 214, 240))
    png("Content/Items/Bolsas/BolsaDelEscriba.png", img)


def primer_trazo():
    img = Image.new("RGBA", (38, 38), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    # LA PLUMA (violeta, enérgica).
    dibujar_pluma(d, 20, 15, 1.0, BLANCO, VIOLETA, brillo=BLANCO)
    # EL TRAZO DE LUZ saliendo de la punta: un rayo multicolor grueso.
    punta = (8, 28)
    for i, color in enumerate([(255, 90, 90), (255, 200, 90), (120, 255, 150),
                               (110, 200, 255), (200, 120, 255)]):
        desvio = (i - 2) * 2.2
        d.line([punta, (1, 34 + desvio)], fill=color + (230,), width=2)
    # EL DESTELLO de la boca.
    d.ellipse([punta[0] - 4, punta[1] - 4, punta[0] + 4, punta[1] + 4],
              fill=(255, 250, 240, 255))
    png("Content/Weapons/Escriba/PrimerTrazo.png", img)


def pluma_consciente():
    img = Image.new("RGBA", (38, 38), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    # LA PLUMA BLANCA (la conciencia).
    dibujar_pluma(d, 20, 15, 1.05, BLANCO, (226, 220, 242), brillo=BLANCO)
    # EL OJO VIOLETA en el plumero (la conciencia que MIRA).
    d.ellipse([16, 9, 24, 15], fill=TINTA)
    d.ellipse([18, 10, 22, 14], fill=VIOLETA_CLARO)
    d.ellipse([19, 11, 21, 13], fill=(20, 12, 30))
    # LAS TRES INSCRIPCIONES: tres rayitas doradas (las ranuras).
    for i in range(3):
        d.line([(24 + i * 3, 8 + i), (27 + i * 3, 5 + i)], fill=ORO, width=1)
    png("Content/Weapons/Escriba/PlumaConsciente.png", img)


def pluma_convocadora():
    img = Image.new("RGBA", (30, 30), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    # EL CÍRCULO DE TINTA (el portal de la convocatoria).
    d.ellipse([4, 4, 26, 26], outline=VIOLETA, width=2)
    d.ellipse([6, 6, 24, 24], outline=(90, 60, 140), width=1)
    # LA PLUMA OSCURA dentro (la que llama).
    dibujar_pluma(d, 15, 15, 0.62, VIOLETA_CLARO, TINTA_MEDIA)
    # LA CHISPA de la convocatoria.
    d.ellipse([13, 4, 17, 8], fill=ORO)
    png("Content/Items/Escriba/PlumaConvocadora.png", img)


def head_boss():
    img = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    # LA MÁSCARA DE TINTA (la silueta alta del Escriba en miniatura).
    d.polygon([(10, 4), (22, 4), (24, 14), (20, 30), (12, 30), (8, 14)],
              fill=TINTA_MEDIA)
    d.polygon([(12, 6), (20, 6), (21, 13), (18, 27), (14, 27), (11, 13)],
              fill=(72, 48, 112))
    # EL OJO ÚNICO blanco (la firma del jefe).
    d.ellipse([12, 10, 20, 16], fill=BLANCO)
    d.ellipse([15, 11, 18, 15], fill=(30, 18, 45))
    # EL COLLAR de dos páginas.
    d.rectangle([6, 20, 9, 25], fill=BLANCO)
    d.rectangle([23, 22, 26, 27], fill=BLANCO)
    png("Content/NPCs/Escriba/EscribaJefe_Head_Boss.png", img)


def pose_bestiario():
    img = Image.new("RGBA", (112, 112), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    import math as m
    # LA SILUETA ALTA (el cuerpo de tinta).
    cuerpo = []
    for i in range(40):
        t = i / 39
        y = 14 + t * 86
        # ancho del cuerpo: estrecho arriba, capa abajo
        ancho = 10 + 22 * m.sin(t * m.pi * 0.9) ** 2 + 8 * t
        cuerpo.append((56 - ancho, y))
    for i in range(40):
        t = 1 - i / 39
        y = 14 + t * 86
        ancho = 10 + 22 * m.sin(t * m.pi * 0.9) ** 2 + 8 * t
        cuerpo.append((56 + ancho, y))
    d.polygon(cuerpo, fill=TINTA_MEDIA)
    d.polygon([(p[0] + 3, p[1]) for p in cuerpo[:20]] + [(p[0] - 3, p[1]) for p in cuerpo[20:]],
              outline=(96, 64, 148))
    # EL OJO ÚNICO (grande, blanco, mirando al frente).
    d.ellipse([40, 30, 72, 48], fill=BLANCO)
    d.ellipse([50, 34, 62, 44], fill=(30, 18, 45))
    d.ellipse([53, 36, 59, 42], fill=VIOLETA_CLARO)
    # EL COLLAR DE PÁGINAS (seis, orbitando).
    rng = random.Random(6520)
    for i in range(6):
        ang = i * m.pi / 3 + 0.3
        px = 56 + m.cos(ang) * 34
        py = 62 + m.sin(ang) * 26
        rot = [(px - 5, py - 7), (px + 5, py - 6), (px + 6, py + 7), (px - 4, py + 6)]
        d.polygon(rot, fill=(228, 224, 240))
    # LA PLUMA GIGANTE (en la mano derecha, alzada).
    dibujar_pluma(d, 88, 44, 1.5, BLANCO, VIOLETA)
    # LA BASE: el charco de tinta.
    d.ellipse([28, 94, 84, 106], fill=TINTA)
    png("Content/NPCs/Escriba/EscribaJefe_PoseBestiario.png", img)


# ==================================================================
#  LA TEXTURA DE RUIDO DEL RAYO (la receta R2 §1d): fBm value-noise
#  2D elongado horizontalmente + caída gaussiana vertical + vetas
#  calientes — al muestrearla con uvFlow la carne AVANZA.
# ==================================================================
def _hash_entero(x, y, semilla):
    h = (x * 374761393 + y * 668265263 + semilla * 1274126177) & 0xFFFFFFFF
    h ^= h >> 13
    h = (h * 1274126177) & 0xFFFFFFFF
    h ^= h >> 16
    return h / 4294967295.0


def _ruido(x, y, semilla):
    x0, y0 = math.floor(x), math.floor(y)
    fx, fy = x - x0, y - y0
    fx = fx * fx * (3 - 2 * fx)
    fy = fy * fy * (3 - 2 * fy)
    a = _hash_entero(x0, y0, semilla)
    b = _hash_entero(x0 + 1, y0, semilla)
    c = _hash_entero(x0, y0 + 1, semilla)
    d2 = _hash_entero(x0 + 1, y0 + 1, semilla)
    return (a * (1 - fx) + b * fx) * (1 - fy) + (c * (1 - fx) + d2 * fx) * fy


def ruido_tinta():
    W, H = 256, 64
    img = Image.new("RGBA", (W, H))
    px = img.load()
    for y in range(H):
        # LA CAÍDA GAUSSIANA vertical (brillante al centro, oscura en bordes).
        v = y / (H - 1)
        gauss = math.exp(-((v - 0.5) ** 2) / (2 * 0.16 ** 2))
        for x in range(W):
            u = x / W
            # EL fBm ELONGADO: 4 octavas, x comprimido (franjas largas).
            f = 0.0
            amp, freq, total = 1.0, 1.0, 0.0
            for octava in range(4):
                f += amp * _ruido(u * 3 * freq, v * 9 * freq, 77 + octava * 131)
                total += amp
                amp *= 0.55
                freq *= 2.1
            f /= total
            # EL FILO DURO del núcleo (la vena central viva).
            nucleo = math.exp(-((v - 0.5) ** 2) / (2 * 0.045 ** 2))
            brillo = 0.22 + 0.78 * gauss * (0.45 + 0.55 * f) + 0.5 * nucleo * f
            brillo = max(0.0, min(1.0, brillo))
            r = int(255 * min(1.0, brillo * 1.05))
            g = int(240 * brillo)
            b = int(255 * min(1.0, brillo * 1.1))
            px[x, y] = (r, g, b, 255)
    png("Content/Effects/Procedural/RuidoTinta.png", img)


if __name__ == "__main__":
    bolsa_del_escriba()
    primer_trazo()
    pluma_consciente()
    pluma_convocadora()
    head_boss()
    pose_bestiario()
    ruido_tinta()
    print("TODO GENERADO")
