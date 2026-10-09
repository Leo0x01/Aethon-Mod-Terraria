#!/usr/bin/env python3
"""
v6.50.82 — EL GRIMORIO HAMBRIENTO: generador de sprites (v4 — CAPAS, iris geométrico).

Anatomía del ojo (medida sobre el arte, zona = diff f01/f03 del GIF):
  r  0-30 px: PUPILA (estrella negra)
  r 30-70 px: IRIS (disco dorado)          ← LA CAPA QUE SE MUEVE
  r    80 px: hueco de esclera
  r 100-140 : decoración dorada estática (se queda en la base)
  r   160+  : banda de esclera blanca

Salida (7 texturas):
  GrimorioHambriento.png           36×49  socket VACÍO (textura del ítem)
  GrimorioHambriento_Iris.png      ~9×9   capa iris (dorado) — la mueve el código
  GrimorioHambriento_IrisRojo.png  ~9×9   capa iris (rojo, hue −45°)
  GrimorioHambriento_Medio.png     36×49  párpado a medias (+ roja)
  GrimorioHambriento_Cerrado.png   36×49  ojo sellado (+ roja)
"""
import glob
import os
import random
import statistics as st
from PIL import Image, ImageChops, ImageFilter

UP = '/home/z/my-project/upload'
DEST = '/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod/Content/Weapons'
PREV = '/home/z/my-project/download/Aethon-Mod-Terraria/tools/tools/preview_hambriento_v4.png'
W, H = 36, 49
TOL = 12
R_IRIS = 75          # radio del disco de iris (fuente, px)
PAD = 8              # margen del recorte
CX, CY = 378, 448    # centro del ojo (fuente) — lo mide la zona, se recalcula abajo


def diff_mascara(a, b, tol=TOL):
    d = ImageChops.difference(a, b)
    px = d.load()
    m = Image.new('L', d.size, 0)
    mp = m.load()
    for y in range(d.size[1]):
        for x in range(d.size[0]):
            r, g, bb, al = px[x, y]
            if r > tol or g > tol or bb > tol or al > tol:
                mp[x, y] = 255
    return m


def zona_ojo(f01, f03):
    z = diff_mascara(f01, f03).filter(ImageFilter.MaxFilter(9))
    return z.point(lambda v: 255 if v > 127 else 0)


def es_dorado(rgb):
    import colorsys
    r, g, b = rgb
    h, s, v = colorsys.rgb_to_hsv(r / 255.0, g / 255.0, b / 255.0)
    return 0.05 <= h <= 0.20 and s >= 0.25 and v >= 0.2


def hue_rojo(img):
    """Dorado → rojo (hue −45°) en toda la imagen (la capa iris / el párpado)."""
    import colorsys
    px = img.load()
    out = img.copy()
    op = out.load()
    n = 0
    for y in range(img.size[1]):
        for x in range(img.size[0]):
            r, g, b, a = px[x, y]
            if a == 0:
                continue
            h, s, v = colorsys.rgb_to_hsv(r / 255.0, g / 255.0, b / 255.0)
            if 0.05 <= h <= 0.20 and s >= 0.25 and v >= 0.2:
                h2 = (h - 0.125) % 1.0
                r2, g2, b2 = colorsys.hsv_to_rgb(h2, s, v)
                op[x, y] = (int(r2 * 255), int(g2 * 255), int(b2 * 255), a)
                n += 1
    return out, n


def main():
    print('=== 0. LIMPIEZA ===')
    for f in glob.glob(f'{DEST}/GrimorioHambriento*.png'):
        os.remove(f)
        print(f'  borrada {os.path.basename(f)}')

    print('=== 1. CARGA ===')
    # La base y el parpadeo salen del GIF (misma corrida de generación — consistente).
    # El PNG base del usuario es otra corrida: difiere del GIF en todo el libro.
    f01 = Image.open('/tmp/gif_ffmpeg/f_01.png').convert('RGBA')   # ABIERTO (estado normal)
    f02 = Image.open('/tmp/gif_ffmpeg/f_02.png').convert('RGBA')   # MEDIO
    f03 = Image.open('/tmp/gif_ffmpeg/f_03.png').convert('RGBA')   # CERRADO

    print('=== 2. ZONA + CENTRO DEL OJO ===')
    zona = zona_ojo(f01, f03)
    zp = zona.load()
    zpts = [(x, y) for y in range(967) for x in range(706) if zp[x, y]]
    zx = [p[0] for p in zpts]; zy = [p[1] for p in zpts]
    bx0, bx1, by0, by1 = min(zx), max(zx), min(zy), max(zy)
    cx, cy = (bx0 + bx1) // 2, (by0 + by1) // 2
    print(f'  zona: X {bx0}-{bx1} Y {by0}-{by1} centro ({cx},{cy}) | {len(zpts)} px')

    print('=== 3. GLOBO DE REFERENCIA (banda r 75-115, no dorada no negra — la zona clara del ojo) ===')
    px = f01.load()
    banda = []
    for y in range(by0, by1 + 1):
        for x in range(bx0, bx1 + 1):
            if not zp[x, y]:
                continue
            d = ((x - cx) ** 2 + (y - cy) ** 2) ** 0.5
            if 75 <= d <= 115 and px[x, y][3] > 0:
                r, g, b = px[x, y][:3]
                if not es_dorado((r, g, b)) and max(r, g, b) >= 70:
                    banda.append((r, g, b))
    mr = st.mean(p[0] for p in banda); mg = st.mean(p[1] for p in banda); mb = st.mean(p[2] for p in banda)
    sd = st.pstdev(p[0] for p in banda)
    print(f'  banda: {len(banda)} px | esclera RGB≈({mr:.0f},{mg:.0f},{mb:.0f}) σ={sd:.1f}')

    print('=== 4. BASE — SOCKET VACÍO (disco r≤R_IRIS+PAD relleno de esclera) ===')
    base = f01.copy()
    bp = base.load()
    random.seed(65_082)
    rellenos = 0
    for y in range(cy - R_IRIS - PAD, cy + R_IRIS + PAD + 1):
        for x in range(cx - R_IRIS - PAD, cx + R_IRIS + PAD + 1):
            if not (0 <= x < 706 and 0 <= y < 967):
                continue
            d2 = (x - cx) ** 2 + (y - cy) ** 2
            if d2 > (R_IRIS + PAD) ** 2:
                continue
            if not zp[x, y]:
                continue  # no invadir el aro ni salir de la zona
            n = random.gauss(0, max(sd, 7.0))
            bp[x, y] = (min(255, max(0, int(mr + n))),
                        min(255, max(0, int(mg + n))),
                        min(255, max(0, int(mb + n))), 255)
            rellenos += 1
    print(f'  rellenos: {rellenos} px')

    print('=== 5. CAPA IRIS (disco r=R_IRIS+PAD del arte, + variante roja) ===')
    caja = (cx - R_IRIS - PAD, cy - R_IRIS - PAD, cx + R_IRIS + PAD, cy + R_IRIS + PAD)
    iris = f01.crop(caja)
    iris_rojo, nrojo = hue_rojo(iris)
    fiw = max(3, round((caja[2] - caja[0]) * W / 706))
    fih = max(3, round((caja[3] - caja[1]) * H / 967))
    iris_ch = iris.resize((fiw, fih), Image.LANCZOS)
    iris_rojo_ch = iris_rojo.resize((fiw, fih), Image.LANCZOS)
    print(f'  capa: {caja[2]-caja[0]}×{caja[3]-caja[1]} fuente → {fiw}×{fih} juego | {nrojo} px dorados→rojos')

    print('=== 6. PARPADEO + ROJAS ===')
    medio, medio_rojo = f02, hue_rojo(f02)[0]
    cerrado, cerrado_rojo = f03, hue_rojo(f03)[0]

    print('=== 7. SALIDA ===')
    out = []

    def guardar(nombre, im):
        ruta = os.path.join(DEST, nombre)
        im.save(ruta, optimize=True)
        out.append((nombre, os.path.getsize(ruta)))

    base_ch = base.resize((W, H), Image.LANCZOS)
    guardar('GrimorioHambriento.png', base_ch)
    guardar('GrimorioHambriento_Iris.png', iris_ch)
    guardar('GrimorioHambriento_IrisRojo.png', iris_rojo_ch)
    guardar('GrimorioHambriento_Medio.png', f02.resize((W, H), Image.LANCZOS))
    guardar('GrimorioHambriento_Rojo_Medio.png', medio_rojo.resize((W, H), Image.LANCZOS))
    guardar('GrimorioHambriento_Cerrado.png', f03.resize((W, H), Image.LANCZOS))
    guardar('GrimorioHambriento_Rojo_Cerrado.png', cerrado_rojo.resize((W, H), Image.LANCZOS))
    for n, s in out:
        print(f'  {n:44s} {s:6d} B')

    print('=== 8. CONSTANTES C# ===')
    ox, oy = cx * W / 706, cy * H / 967
    print(f'  OJO_X={ox:.2f}f  OJO_Y={oy:.2f}f  (36×49)')
    print(f'  IRIS {fiw}×{fih}, dibujada con origen en su centro, en (OJO_X,OJO_Y)+desp')

    print('=== 9. PREVIEW 4× ===')
    esc = 4
    fw, fh = W * esc, H * esc

    def peg(dst, im, x):
        dst.paste(im.resize((fw, fh), Image.NEAREST), (x, 0))

    def peg_iris(dst, im, dx, dy, x):
        t = im.resize((fiw * esc, fih * esc), Image.NEAREST)
        dst.paste(t, (x + int(ox * esc) - t.width // 2 + int(dx * esc),
                      int(oy * esc) - t.height // 2 + int(dy * esc)), t)

    lienzo = Image.new('RGBA', (fw * 6 + 50, fh), (28, 26, 38, 255))
    peg(lienzo, base_ch, 0); peg_iris(lienzo, iris_ch, 0, 0, 0)              # centro
    peg(lienzo, base_ch, fw + 10); peg_iris(lienzo, iris_ch, 4.0, 0, fw + 10)  # derecha
    peg(lienzo, base_ch, 2 * (fw + 10)); peg_iris(lienzo, iris_ch, -4.0, 0, 2 * (fw + 10))  # izq
    peg(lienzo, base_ch, 3 * (fw + 10)); peg_iris(lienzo, iris_ch, 2.8, -2.2, 3 * (fw + 10))  # arr-der
    peg(lienzo, f03.resize((W, H), Image.LANCZOS), 4 * (fw + 10))             # cerrado
    peg(lienzo, base_ch, 5 * (fw + 10)); peg_iris(lienzo, iris_rojo_ch, 4.0, 0, 5 * (fw + 10))  # rojo
    lienzo.save(PREV)
    print(f'  {PREV}')
    print('LISTO')


if __name__ == '__main__':
    main()
