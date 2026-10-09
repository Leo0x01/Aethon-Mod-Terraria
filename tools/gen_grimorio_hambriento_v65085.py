#!/usr/bin/env python3
"""
v6.50.85 — LA BASE LIMPIA + EL ROJO SOLO EN EL IRIS.

La letra del usuario: «cuando recortas el iris toda la esclerotica queda con
el agujero del iris en ves de estar completamente blanco como el resto de la
esclerotica, aunque no es con el agujero es mas bien con una sombra del irirs,
pero hay algo mas, lo que se pone rojo es solo el iris, el libro se debe quedar
de color normal por lo tanto deja el libro normal, para mejorar la situacion
te dare un consejo, cuando remuevas el iris has que los pixeles donde estaba el
iris tomen el color de la esclerotica. tambien te dare una imagen png de como
debe ser y el codigo de la imagen, usa el codigo»

EL DIAGNÓSTICO: la .82 rellenó el socket del iris con RUIDO GAUSSIANO alrededor
de la media de la esclera (200,175,163 σ54) — al reducir a 36×49 ese ruido se
lee como una SOMBRA gris en el socket (el usuario la vio en el mundo y en la
mano, donde el libro se dibuja más grande). El usuario entregó SU PROPIA
limpieza: sprite libro v3 - sin iris.png + codigo sin iris.txt — la MISMA
corrida de arte que codigo 2.txt (verificado: byte-idénticos FUERA del ojo,
|Δ| = 0,0 — la fuente autoritativa).

LA CURA (5 texturas — las 2 rojas del LIBRO desaparecen):
  GrimorioHambriento.png          36×49  LA BASE = el sprite sin iris EXACTO
                                         (socket completamente blanco 247,235,220 σ5)
  GrimorioHambriento_Iris.png     32×32  disco circular (la .84 — SIN CAMBIO,
                                         recorte de codigo 2.txt centrado en el iris)
  GrimorioHambriento_IrisRojo.png 32×32  ídem rojo (hue −45° en los dorados)
  GrimorioHambriento_Medio.png    36×49  párpado a medias = base sin iris +
                                         (zona del ojo del frame f02 del GIF, emplumada)
  GrimorioHambriento_Cerrado.png  36×49  ojo sellado = base sin iris + zona f03

EL ROJO: SOLO la capa IrisRojo (alpha = nivel de hambre). El libro — párpados
incluidos — queda SIEMPRE a color normal: las Rojo_Medio/Rojo_Cerrado de la .82
teñían de rojo TODO el dorado del libro al parpadear con hambre.

LOS PÁRPADOS COMPUESTOS: los frames del GIF son OTRA corrida de cuantización
(Δ~17/canal en todo el libro) — si se usaran tal cual, el libro entero
titilaría entre corridas en cada parpadeo. La zona animada del GIF (diff
f01/f03 dilatado) se PEGA sobre la base sin iris con pluma gaussiana: el cuerpo
del libro queda de la corrida del código y solo el ojo cambia al parpadear.
"""
import base64
import colorsys
import math
import os
import re

import numpy as np
from PIL import Image, ImageChops, ImageFilter

UP = '/home/z/my-project/upload'
GIF = '/tmp/gif_ffmpeg'
DEST = '/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod/Content/Weapons'
PREV = '/home/z/my-project/download/Aethon-Mod-Terraria/tools/tools/preview_hambriento_v65085.png'
W, H = 36, 49
SRC_W, SRC_H = 706, 967

# El iris del arte (ajuste de círculo verificado en la .84 — se re-verifica abajo)
CX_IRIS, CY_IRIS, R_IRIS = 374.3, 443.1, 84.0
PLUMA_IRIS = 2.5
TEX = 32


def decodificar(ruta):
    """RLE [count,R,G,B,A] base64 → array RGBA 706×967 (el sprite EXACTO)."""
    data = open(ruta, encoding='utf-8').read()
    rle = base64.b64decode(re.search(r'rle:"([^"]+)"', data).group(1))
    assert len(rle) % 5 == 0, f'RLE truncado: {len(rle)} bytes'
    px = bytearray(SRC_W * SRC_H * 4)
    p = i = 0
    while i + 5 <= len(rle):
        n, r, g, b, a = rle[i:i + 5]
        i += 5
        for _ in range(n):
            px[p] = r; px[p + 1] = g; px[p + 2] = b; px[p + 3] = a
            p += 4
    assert p == SRC_W * SRC_H * 4, f'pixeles {p // 4} != {SRC_W * SRC_H}'
    return np.frombuffer(bytes(px), dtype=np.uint8).reshape(SRC_H, SRC_W, 4)


def ajuste_circulo_iris(con_iris):
    """Re-verifica el centro/radio del disco dorado por CENTROIDE DE MÁSCARA.

    Máscara = píxeles cálidos (R−B>60) dentro de r<100 de la semilla — el iris
    dorado completo (la pupila oscura queda fuera de la máscara y el anillo
    decorativo de r~118-145 también). Centroide → centro; área → radio
    equivalente. Más robusto que los rayos (el iris no es cálido uniforme).
    """
    cx, cy = CX_IRIS, CY_IRIS
    yy, xx = np.mgrid[0:SRC_H, 0:SRC_W]
    d = np.sqrt((xx - cx) ** 2 + (yy - cy) ** 2)
    dentro = d < 100
    rgba = con_iris.astype(int)
    calido = (rgba[..., 0] - rgba[..., 2] > 60) & (rgba[..., 3] > 0) & dentro
    npx = int(calido.sum())
    fx = float(xx[calido].mean())
    fy = float(yy[calido].mean())
    fr = math.sqrt(npx / math.pi)
    # radio del borde: percentil de la distancia de los píxeles cálidos
    dd = d[calido]
    return fx, fy, fr, float(np.percentile(dd, 98)), npx


def hue_rojo(arr):
    """Dorado → rojo (hue −45°) en los píxeles cálidos saturados (patrón .82/.84)."""
    out = arr.copy()
    n = 0
    h, w = arr.shape[:2]
    for y in range(h):
        for x in range(w):
            r, g, b, a = arr[y, x]
            if a == 0:
                continue
            hh, s, v = colorsys.rgb_to_hsv(r / 255.0, g / 255.0, b / 255.0)
            if 0.05 <= hh <= 0.20 and s >= 0.25 and v >= 0.2:
                r2, g2, b2 = colorsys.hsv_to_rgb((hh - 0.125) % 1.0, s, v)
                out[y, x] = (round(r2 * 255), round(g2 * 255), round(b2 * 255), a)
                n += 1
    return out, n


def main():
    print('=== 1. LOS DOS CÓDIGOS (la MISMA corrida — el autoritativo) ===')
    con_iris = decodificar(f'{UP}/codigo 2.txt')
    sin_iris = decodificar(f'{UP}/codigo sin iris.txt')
    delta = np.abs(con_iris.astype(int) - sin_iris.astype(int))
    fuera_ojo = np.sqrt(
        (np.mgrid[0:SRC_H, 0:SRC_W][1] - CX_IRIS) ** 2 +
        (np.mgrid[0:SRC_H, 0:SRC_W][0] - CY_IRIS) ** 2) > 200
    d_fuera = delta[:, :, :3][fuera_ojo].mean()
    print(f'  con_iris y sin_iris: |Δ| medio FUERA del ojo = {d_fuera:.3f} '
          f'(byte-idénticos = misma corrida)')
    assert d_fuera < 0.05, '¡NO son la misma corrida!'

    print('=== 2. RE-VERIFICACIÓN DEL IRIS (ajuste de círculo) ===')
    fx, fy, fr, p98, npx = ajuste_circulo_iris(con_iris)
    print(f'  ajuste fresco: centroide ({fx:.1f},{fy:.1f}) · R equiv {fr:.1f} · borde p98 {p98:.1f} · {npx} px cálidos')
    print(f'  .84 documentado: centro ({CX_IRIS},{CY_IRIS}) R={R_IRIS}')
    assert abs(fx - CX_IRIS) < 3 and abs(fy - CY_IRIS) < 3 and 80 < p98 < 92, \
        'el ajuste no coincide con la .84 — revisar'
    # la constante OJO apunta AL IRIS DEL ARTE (donde el usuario lo tenía)
    ojo_x, ojo_y = CX_IRIS * W / SRC_W, CY_IRIS * H / SRC_H
    print(f'  OJO = ({ojo_x:.2f}f, {ojo_y:.2f}f)   [antes: (19.27f, 22.70f) — el centro del GIF]')

    print('=== 3. LA BASE LIMPIA (socket blanco del usuario) ===')
    # blancura del socket en fuente
    yy, xx = np.mgrid[0:SRC_H, 0:SRC_W]
    d_ojo = np.sqrt((xx - CX_IRIS) ** 2 + (yy - CY_IRIS) ** 2)
    socket = sin_iris[(d_ojo < 60) & (sin_iris[..., 3] > 0)]
    print(f'  socket fuente r<60: RGB ({socket[:, 0].mean():.0f},'
          f'{socket[:, 1].mean():.0f},{socket[:, 2].mean():.0f}) '
          f'σ ({socket[:, 0].std():.1f},{socket[:, 1].std():.1f},{socket[:, 2].std():.1f})')
    base_ch = Image.fromarray(sin_iris, 'RGBA').resize((W, H), Image.LANCZOS)
    b = np.array(base_ch)
    # socket a escala de juego: alrededor de (ojo_x, ojo_y)
    sy, sx = int(round(ojo_y)), int(round(ojo_x))
    caja = b[max(0, sy - 4):sy + 5, max(0, sx - 4):sx + 5]
    vis = caja[caja[..., 3] > 0]
    print(f'  socket 36×49 (caja 9×9): RGB ({vis[:, 0].mean():.0f},'
          f'{vis[:, 1].mean():.0f},{vis[:, 2].mean():.0f}) · '
          f'oscuros(<120): {int((vis[:, :3].mean(axis=1) < 120).sum())}/{len(vis)}')

    print('=== 4. LA CAPA IRIS (la .84 — recorte circular, SIN CAMBIO) ===')
    half = R_IRIS + PLUMA_IRIS
    x0, x1 = int(math.floor(CX_IRIS - half)), int(math.ceil(CX_IRIS + half))
    y0, y1 = int(math.floor(CY_IRIS - half)), int(math.ceil(CY_IRIS + half))
    caja_iris = con_iris[y0:y1, x0:x1].astype(np.float64)
    ch, cw = caja_iris.shape[:2]
    ys, xs = np.mgrid[y0:y1, x0:x1]
    d = np.sqrt((xs - CX_IRIS) ** 2 + (ys - CY_IRIS) ** 2)
    mascara = np.clip((R_IRIS + PLUMA_IRIS - d) / (2 * PLUMA_IRIS), 0.0, 1.0)
    caja_iris[..., 3] = caja_iris[..., 3] * mascara
    rojo_src, n_rojo = hue_rojo(caja_iris.astype(np.uint8))
    iris = Image.fromarray(caja_iris.astype(np.uint8), 'RGBA').resize((TEX, TEX), Image.LANCZOS)
    iris_rojo = Image.fromarray(rojo_src, 'RGBA').resize((TEX, TEX), Image.LANCZOS)
    # ¿byte-idéntica a la .84?
    iris_actual = Image.open(f'{DEST}/GrimorioHambriento_Iris.png')
    mismo_iris = (iris.tobytes() == iris_actual.tobytes())
    print(f'  Iris.png nueva == .84: {mismo_iris} · {n_rojo} px dorados→rojos')

    print('=== 5. LOS PÁRPADOS COMPUESTOS (zona del GIF sobre la base limpia) ===')
    f01 = Image.open(f'{GIF}/f_01.png').convert('RGBA')
    f02 = Image.open(f'{GIF}/f_02.png').convert('RGBA')
    f03 = Image.open(f'{GIF}/f_03.png').convert('RGBA')

    def zona(a, bimg, tol=12):
        """máscara de la zona que ANIMA entre dos frames (dilatada + emplumada)."""
        dif = ImageChops.difference(a, bimg)
        m = dif.convert('L').point(lambda v: 255 if v > tol else 0)
        m = m.filter(ImageFilter.MaxFilter(15))     # dilatación generosa
        m = m.filter(ImageFilter.GaussianBlur(5))   # la pluma de la costura
        return m

    m_zona = zona(f01, f03)
    mz = np.asarray(m_zona, dtype=np.float64)[..., None] / 255.0

    def componer(frame):
        fr = np.asarray(frame, dtype=np.float64)
        si = sin_iris.astype(np.float64)
        out = si * (1.0 - mz) + fr * mz
        return Image.fromarray(np.clip(np.round(out), 0, 255).astype(np.uint8), 'RGBA')

    medio = componer(f02)
    cerrado = componer(f03)
    px_zona = int((np.asarray(m_zona) > 127).sum())
    print(f'  zona animada: {px_zona} px fuente ({100.0 * px_zona / (SRC_W * SRC_H):.1f}%) · '
          f'emplume σ5')
    # el cuerpo del libro NO cambia entre base y párpados fuera de la zona
    dmedio = np.abs(np.asarray(medio).astype(int) - sin_iris.astype(int)).sum(axis=2)
    dcerr = np.abs(np.asarray(cerrado).astype(int) - sin_iris.astype(int)).sum(axis=2)
    fuera = np.asarray(m_zona) < 64
    print(f'  fuera de la zona |Δ| medio: medio {dmedio[fuera].mean():.2f} · '
          f'cerrado {dcerr[fuera].mean():.2f} (≈0 = cuerpo estable)')

    print('=== 6. SALIDA (5 texturas — SIN Rojo_Medio/Rojo_Cerrado) ===')
    medio_ch = medio.resize((W, H), Image.LANCZOS)
    cerrado_ch = cerrado.resize((W, H), Image.LANCZOS)
    salida = [
        ('GrimorioHambriento.png', base_ch),
        ('GrimorioHambriento_Iris.png', iris),
        ('GrimorioHambriento_IrisRojo.png', iris_rojo),
        ('GrimorioHambriento_Medio.png', medio_ch),
        ('GrimorioHambriento_Cerrado.png', cerrado_ch),
    ]
    for n, im in salida:
        im.save(f'{DEST}/{n}', optimize=True)
        print(f'  {n:44s} {im.size[0]}×{im.size[1]} · {os.path.getsize(f"{DEST}/{n}"):5d} B')
    for n in ('GrimorioHambriento_Rojo_Medio.png', 'GrimorioHambriento_Rojo_Cerrado.png'):
        p = f'{DEST}/{n}'
        if os.path.exists(p):
            os.remove(p)
            print(f'  ELIMINADA {n} (el libro queda a color normal)')

    print('=== 7. CONSTANTES C# ===')
    juego_w = cw * W / SRC_W
    print(f'  OJO = new Vector2({ojo_x:.2f}f, {ojo_y:.2f}f)   // el iris del ARTE (codigo 2)')
    print(f'  IRIS_ESC = {juego_w / TEX:.4f}f                    (igual — caja {cw}px)')

    print('=== 8. PREVIEW 6× ===')
    E = 6
    fw, fh = W * E, H * E
    irisE = iris.resize((round(juego_w * E), round(juego_w * E)), Image.LANCZOS)
    irisRE = iris_rojo.resize((round(juego_w * E), round(juego_w * E)), Image.LANCZOS)

    def panel(img36):
        return img36.resize((fw, fh), Image.NEAREST)

    def con_iris(dx, dy, rojo=False):
        p = panel(base_ch)
        t = irisRE if rojo else irisE
        p.paste(t, (round((ojo_x + dx) * E - t.width / 2),
                     round((ojo_y + dy) * E - t.height / 2)), t)
        return p

    paneles = [con_iris(0, 0), con_iris(3.5, 0), con_iris(-3.5, 0),
               con_iris(2.4, -2.0), con_iris(3.5, 0, rojo=True),
               panel(medio_ch), panel(cerrado_ch)]
    lienzo = Image.new('RGBA', (fw * 7 + 64, fh + 8), (26, 24, 36, 255))
    for i, im in enumerate(paneles):
        lienzo.paste(im, (i * (fw + 8) + 8, 4))
    lienzo.save(PREV)
    print(f'  {PREV}')
    print('LISTO')


if __name__ == '__main__':
    main()
