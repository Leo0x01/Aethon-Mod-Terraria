#!/usr/bin/env python3
"""v6.50.86 — LOS PÁRPADOS PUROS DE CÓDIGO + LAS DOS COPIAS ANIMADAS.

LA LETRA DEL USUARIO: «al usar el gif para abrir y cerrar el ojo hace que se
note el cambio en cuanto a calidad, el codigo puro da mejor calidad ya que al
ser codigo puedes recrear los pixeles fielmente, asi que te dare el codigo del
ojo entrecerrado y cerrado… te volvere a dar el resto de codigos… dime que
opinas de este grimorio… haz una copia del grimorio pero con alguna animacion
que creas que sea correcta, luego has otra copia pero en esta tercera copia
quiero que animes las estrellas que tiene el grimorio en la tapa y animes las
venas de luz morada que recorren el libro».

(1) LOS PÁRPADOS PUROS — el usuario entregó el ojo MEDIO CERRADO y el CERRADO
    en código: verificado |Δ| = 0,0000 y 0 px distintos fuera del ojo contra
    la base sin iris = la MISMA corrida de arte. El GIF (y su compostado con
    pluma de la .85 — la costura que el usuario VIO) muere: los párpados son
    ahora LANCZOS puro de la misma fuente que la base — calidad idéntica al
    parpadear, píxel sobre píxel, sin transición de corrida.
(2) LA COPIA RÚNICA (la animación del autor) — GrimorioHambrientoRunico:
    3 runas doradas orbitando el ojo + aura que respira (más rápido y rojo
    con hambre). Runas procedurales 11×11 (diamante, ojo menor, chispa).
(3) LA COPIA ESTELAR — GrimorioHambrientoEstelar: las ESTRELLAS de la tapa
    (las cruces doradas + los signos + morados sobre la tapa negra — NO los
    remaches del anillo ni los brillos del marco dorado) titilan en 3 grupos
    desfasados; las VENAS de luz morada pulsan en 4 bandas radiales desde el
    ojo — la ola de luz RECORRE el libro. Máscaras = píxeles del arte × brillo
    + halo (fidelidad al arte del usuario).
"""
import base64
import colorsys
import math
import os
import re

import numpy as np
from PIL import Image, ImageFilter
from scipy import ndimage

UP = '/home/z/my-project/upload'
DEST = '/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod/Content/Weapons'
PREV = '/home/z/my-project/download/Aethon-Mod-Terraria/tools/tools/preview_v65086.png'
W, H = 36, 49
SRC_W, SRC_H = 706, 967
CX, CY = 374.3, 443.1
FONDO_MAX = 55          # el anillo alrededor de una estrella debe ser TAPA NEGRA


def decodificar(ruta):
    data = open(ruta, encoding='utf-8').read()
    rle = base64.b64decode(re.search(r'rle:"([^"]+)"', data).group(1))
    assert len(rle) % 5 == 0
    px = bytearray(SRC_W * SRC_H * 4)
    p = i = 0
    while i + 5 <= len(rle):
        n, r, g, b, a = rle[i:i + 5]
        i += 5
        for _ in range(n):
            px[p] = r; px[p + 1] = g; px[p + 2] = b; px[p + 3] = a
            p += 4
    assert p == SRC_W * SRC_H * 4
    return np.frombuffer(bytes(px), dtype=np.uint8).reshape(SRC_H, SRC_W, 4)


def hsv_vec(rgba):
    r, g, b, a = rgba[..., 0], rgba[..., 1], rgba[..., 2], rgba[..., 3]
    rr, gg, bb = r / 255.0, g / 255.0, b / 255.0
    mxn = np.maximum(np.maximum(rr, gg), bb)
    mnn = np.minimum(np.minimum(rr, gg), bb)
    v = mxn
    s = np.where(mxn > 0, (mxn - mnn) / np.maximum(mxn, 1e-6), 0)
    dr = np.where(mxn == rr, (gg - bb) / np.maximum(mxn - mnn, 1e-6), 0)
    dg = np.where(mxn == gg, 2.0 + (bb - rr) / np.maximum(mxn - mnn, 1e-6), 0)
    db = np.where(mxn == bb, 4.0 + (rr - gg) / np.maximum(mxn - mnn, 1e-6), 0)
    h = (np.where(mxn == rr, dr, np.where(mxn == gg, dg, db)) / 6.0) % 1.0
    return h, s, v, a


def main():
    print('=== 1. LOS 4 CÓDIGOS (todos ya verificados md5 contra la .84/.85) ===')
    base = decodificar(f'{UP}/codigo de sprite libro v3 sin iris.txt')
    medio = decodificar(f'{UP}/codigo de sprite libro v3 - ojo medio cerrado sin iris .txt')
    cerrado = decodificar(f'{UP}/codigo de sprite libro v3 - ojo cerrado.txt')
    con_iris = decodificar(f'{UP}/codigo sprite libro v3.txt')
    yy, xx = np.mgrid[0:SRC_H, 0:SRC_W]
    d_ojo = np.sqrt((xx - CX) ** 2 + (yy - CY) ** 2)
    fuera = d_ojo > 200
    for nombre, arr in (('medio', medio), ('cerrado', cerrado)):
        d = np.abs(arr.astype(int) - base.astype(int))
        assert d[..., :3][fuera].mean() == 0.0 and d[..., 3][fuera].mean() == 0.0, \
            f'{nombre} NO es la misma corrida que la base'
        zona = int((d.sum(axis=2) > 12).sum())
        print(f'  {nombre}: MISMA CORRIDA que la base (|Δ|=0 fuera del ojo) · zona animada {zona} px')

    print('=== 2. LOS PÁRPADOS PUROS (LANCZOS de la MISMA fuente — sin GIF) ===')
    medio_ch = Image.fromarray(medio).resize((W, H), Image.LANCZOS)
    cerrado_ch = Image.fromarray(cerrado).resize((W, H), Image.LANCZOS)
    base_ch = Image.fromarray(base).resize((W, H), Image.LANCZOS)
    # continuidad: fuera del ojo (a escala de juego) los tres SON el mismo libro
    bm, bc = np.asarray(medio_ch, int), np.asarray(cerrado_ch, int)
    bb = np.asarray(base_ch, int)
    dm = np.abs(bm - bb).sum(axis=2)
    dc = np.abs(bc - bb).sum(axis=2)
    yy2, xx2 = np.mgrid[0:H, 0:W]
    d_ojo2 = np.sqrt((xx2 - CX * W / SRC_W) ** 2 + (yy2 - CY * H / SRC_H) ** 2)
    fuera2 = d_ojo2 > 200 * W / SRC_W
    print(f'  36×49 fuera del ojo: |Δ| medio/medio {dm[fuera2].mean():.3f} · '
          f'cerrado {dc[fuera2].mean():.3f} · px>8: '
          f'{int((dm[fuera2] > 8).sum())}/{int((dc[fuera2] > 8).sum())} (la .85 compostaba con pluma)')

    print('=== 3. LAS ESTRELLAS (la tapa NEGRA — ni remaches ni marco) ===')
    rgba = con_iris.astype(float)
    lum = 0.299 * rgba[..., 0] + 0.587 * rgba[..., 1] + 0.114 * rgba[..., 2]
    claro = (lum > 130) & (rgba[..., 3] > 200) & (d_ojo > 235)
    etiquetas, n = ndimage.label(claro)
    tams = ndimage.sum(claro, etiquetas, range(1, n + 1))
    estrellas_px = []
    for i in range(1, n + 1):
        m = etiquetas == i
        t = int(tams[i - 1])
        if not (5 <= t <= 600):
            continue
        anillo = ndimage.binary_dilation(m, iterations=7) & ~m
        if anillo.sum() == 0 or lum[anillo].mean() > FONDO_MAX:
            continue  # fondo brillante = marco dorado / esquinas
        ys, xs = np.nonzero(m)
        estrellas_px.append((xs, ys, m, rgba[m][:, :3].mean(axis=0)))
    print(f'  estrellas: {len(estrellas_px)}')
    assert len(estrellas_px) >= 15, 'muy pocas estrellas — revisar criterio'

    # --- máscara estrella: píxeles del ARTE × 1,8 + HALO alrededor ---
    NGRUPOS = 3
    mascaras_est = [np.zeros((SRC_H, SRC_W, 4), np.float64) for _ in range(NGRUPOS)]
    orden = sorted(estrellas_px, key=lambda e: (e[1].mean(), e[0].mean()))
    for gi, (xs, ys, m, rgbm) in enumerate(orden):
        g = gi % NGRUPOS
        ma = mascaras_est[g]
        art = rgba[m]
        ma[m] = np.concatenate([np.minimum(art[:, :3] * 1.8, 255), art[:, 3:4]], axis=1)
        # halo: cae linealmente en 7 px alrededor del radio de la estrella
        cy, cx = ys.mean(), xs.mean()
        rad = math.sqrt(m.sum() / math.pi)
        y0, y1 = max(0, int(cy - rad - 8)), min(SRC_H, int(cy + rad + 9))
        x0, x1 = max(0, int(cx - rad - 8)), min(SRC_W, int(cx + rad + 9))
        gy, gx = np.mgrid[y0:y1, x0:x1]
        dd = np.sqrt((gx - cx) ** 2 + (gy - cy) ** 2)
        banda = np.clip(1.0 - (dd - rad) / 7.0, 0.0, 1.0)
        banda[dd <= rad] = 0.0
        zona = banda > 0
        ma[y0:y1, x0:x1][zona] = np.concatenate([
            np.minimum(np.outer(np.ones(int(zona.sum())), np.minimum(rgbm * 1.4, 255)), 255),
            (banda[zona] * 0.5 * 255)[:, None]], axis=1)
    for g in range(NGRUPOS):
        nm = int((mascaras_est[g][..., 3] > 8).sum())
        print(f'  grupo {g + 1}: {nm} px con masa')

    print('=== 4. LAS VENAS DE LUZ MORADA (4 bandas radiales desde el ojo) ===')
    h, s, v, a = hsv_vec(rgba)
    venas = (h > 0.56) & (h < 0.90) & (s > 0.20) & (v > 0.25) & (a > 50) & (d_ojo > 160)
    vy, vx = np.nonzero(venas)
    dv = np.sqrt((vx - CX) ** 2 + (vy - CY) ** 2)
    q = np.percentile(dv, [25, 50, 75])
    print(f'  venas: {int(venas.sum())} px · cuartiles {q[0]:.0f}/{q[1]:.0f}/{q[2]:.0f} (max {dv.max():.0f})')
    # halo de la vena: 3 px alrededor, para que la ola LEA a 36×49
    dist = ndimage.distance_transform_edt(~venas)
    halo = np.clip(1.0 - dist / 3.5, 0.0, 1.0) * 0.55
    rgb_vena = rgba[venas][:, :3].mean(axis=0)
    NBANDAS = 4
    mascaras_ven = []
    for k in range(NBANDAS):
        lim0 = 0 if k == 0 else q[k - 1]
        lim1 = dv.max() + 1 if k == NBANDAS - 1 else q[k]
        banda = venas & (d_ojo >= lim0) & (d_ojo < lim1)
        ma = np.zeros((SRC_H, SRC_W, 4), np.float64)
        ma[banda] = np.concatenate([np.minimum(rgba[banda][:, :3] * 1.6, 255), rgba[banda][:, 3:4]], axis=1)
        hb = halo.copy()
        hb[~((d_ojo >= lim0) & (d_ojo < lim1))] = 0.0
        hb[venas] = 0.0
        zonah = hb > 0.01
        ma[zonah] = np.concatenate([
            np.outer(np.ones(int(zonah.sum())), np.minimum(rgb_vena * 1.3, 255)),
            (hb[zonah] * 255)[:, None]], axis=1)
        mascaras_ven.append(ma)
        print(f'  banda {k + 1} (r {lim0:.0f}-{lim1:.0f}): {int(banda.sum())} px vena + halo')

    print('=== 5. LA COPIA BASE ×2 + DOWNSCALE DE MÁSCARAS ===')
    salida = [
        ('GrimorioHambriento_Medio.png', medio_ch),
        ('GrimorioHambriento_Cerrado.png', cerrado_ch),
    ]
    # las copias: la MISMA base byte a byte (el ojo funciona igual en las 3)
    base_actual = open(f'{DEST}/GrimorioHambriento.png', 'rb').read()
    for nombre in ('GrimorioHambrientoRunico.png', 'GrimorioHambrientoEstelar.png'):
        with open(f'{DEST}/{nombre}', 'wb') as f:
            f.write(base_actual)
        print(f'  {nombre} = base byte a byte ({len(base_actual)} B)')
    for g in range(NGRUPOS):
        im = Image.fromarray(np.clip(np.round(mascaras_est[g]), 0, 255).astype(np.uint8))
        salida.append((f'GrimorioHambrientoEstelar_Estrellas{g + 1}.png',
                       im.resize((W, H), Image.LANCZOS)))
    for k in range(NBANDAS):
        im = Image.fromarray(np.clip(np.round(mascaras_ven[k]), 0, 255).astype(np.uint8))
        salida.append((f'GrimorioHambrientoEstelar_Venas{k + 1}.png',
                       im.resize((W, H), Image.LANCZOS)))

    print('=== 6. LAS RUNAS DEL AUTOR (3 glifos 11×11 con halo) ===')

    def glifo(pinta, glow=1.0):
        n = 24
        im = Image.new('RGBA', (n, n), (0, 0, 0, 0))
        px = im.load()
        pinta(px, n)
        im = im.filter(ImageFilter.GaussianBlur(0.8))
        # recorte al cuadro 11×11 con el centro del glifo
        im = im.resize((11, 11), Image.LANCZOS)
        arr = np.asarray(im, float)
        arr[..., :3] = np.minimum(arr[..., :3] * (1.0 + glow * 0.35), 255)
        return Image.fromarray(arr.astype(np.uint8))

    oro = (255, 216, 138, 255)

    # RunaA — EL SIGILO DIAMANTE
    def pinta_a(px, n):
        c = n // 2
        for i in range(-6, 7):
            px[c + i, c] = oro
            px[c, c + i] = oro
        px[c, c] = (255, 248, 220, 255)

    # RunaB — EL OJO MENOR (círculo + pupila)
    def pinta_b(px, n):
        c = n // 2
        for ang in range(0, 360, 6):
            x = c + round(5.6 * math.cos(math.radians(ang)))
            y = c + round(5.6 * math.sin(math.radians(ang)))
            px[x, y] = oro
        px[c, c] = (255, 120, 110, 255)
        px[c - 1, c] = (255, 150, 140, 255)
        px[c + 1, c] = (255, 150, 140, 255)

    # RunaC — LA CHISPA (cruz aspada)
    def pinta_c(px, n):
        c = n // 2
        for i in range(-6, 7):
            d = abs(i)
            px[c + i, c + i] = oro if d < 5 else (0, 0, 0, 0)
            px[c + i, c - i] = oro if d < 5 else (0, 0, 0, 0)
        px[c, c] = (255, 252, 235, 255)

    for nombre, fn in (('RunaA', pinta_a), ('RunaB', pinta_b), ('RunaC', pinta_c)):
        im = glifo(fn)
        salida.append((f'GrimorioHambrientoRunico_{nombre}.png', im))

    print('=== 7. SALIDA ===')
    for n_, im in salida:
        im.save(f'{DEST}/{n_}', optimize=True)
        print(f'  {n_:48s} {im.size[0]}×{im.size[1]} · {os.path.getsize(f"{DEST}/{n_}"):5d} B')

    print('=== 8. PREVIEW 6× (parpadeo puro + estelar + rúnico) ===')
    E = 6
    fw, fh = W * E, H * E

    def panel(im36):
        return im36.resize((fw, fh), Image.NEAREST)

    iris = Image.open(f'{DEST}/GrimorioHambriento_Iris.png')
    irisE = iris.resize((round(174 * W / SRC_W * E), round(174 * W / SRC_W * E)), Image.LANCZOS)
    ojo_x, ojo_y = CX * W / SRC_W, CY * H / SRC_H

    def con_iris_panel():
        p = panel(base_ch)
        p.paste(irisE, (round(ojo_x * E - irisE.width / 2), round(ojo_y * E - irisE.height / 2)), irisE)
        return p

    # paneles estelares: base + estrellas al pico + venas en ola (banda 2 al pico)
    def estelar_panel(fase):
        p = con_iris_panel().convert('RGBA')
        for g in range(NGRUPOS):
            m = salida[2 + g][1] if False else Image.open(f'{DEST}/GrimorioHambrientoEstelar_Estrellas{g + 1}.png')
            alfa = (0.35 + 0.65 * math.cos(fase - g * 2 * math.pi / 3)) / 2 + 0.30
            if alfa > 0.02:
                cap = m.copy()
                ca = np.asarray(cap, float)
                ca[..., 3] *= min(1.0, max(0.0, alfa))
                cap = Image.fromarray(ca.astype(np.uint8))
                cap = cap.resize((fw, fh), Image.NEAREST)
                p = Image.alpha_composite(p, cap)
        for k in range(NBANDAS):
            m = Image.open(f'{DEST}/GrimorioHambrientoEstelar_Venas{k + 1}.png')
            alfa = max(0.0, math.sin(fase * 2 - k * math.pi / 2)) ** 1.6
            if alfa > 0.02:
                cap = m.copy()
                ca = np.asarray(cap, float)
                ca[..., 3] *= alfa
                cap = Image.fromarray(ca.astype(np.uint8))
                cap = cap.resize((fw, fh), Image.NEAREST)
                p = Image.alpha_composite(p, cap)
        return p

    def runa_panel():
        p = con_iris_panel().convert('RGBA')
        for i, nom in enumerate(('RunaA', 'RunaB', 'RunaC')):
            ru = Image.open(f'{DEST}/GrimorioHambrientoRunico_{nom}.png')
            ru = ru.resize((round(ru.width * E * 0.72), round(ru.height * E * 0.72)), Image.LANCZOS)
            ang = i * 2 * math.pi / 3 + 0.7
            x = ojo_x + 11.5 * math.cos(ang)
            y = ojo_y + 8.0 * math.sin(ang)
            halo = ru.resize((round(ru.width * 1.9), round(ru.height * 1.9)), Image.LANCZOS)
            ha = np.asarray(halo, float)
            ha[..., 3] *= 0.35
            halo = Image.fromarray(ha.astype(np.uint8))
            p = Image.alpha_composite(p, halo,
                                      ) if False else _composite(p, halo, (x - 0.95, y - 0.95))
            p = _composite(p, ru, (x, y))
        return p

    def _composite(base, cap, centro):
        capa = Image.new('RGBA', (fw, fh), (0, 0, 0, 0))
        capa.paste(cap, (round(centro[0] * E - cap.width / 2), round(centro[1] * E - cap.height / 2)), cap)
        return Image.alpha_composite(base, capa)

    paneles = [
        con_iris_panel(), panel(medio_ch), panel(cerrado_ch),           # el parpadeo PURO
        estelar_panel(0.0), estelar_panel(1.1), estelar_panel(2.2),     # el cielo animado
        runa_panel(),                                                    # las runas del autor
    ]
    lienzo = Image.new('RGBA', (fw * len(paneles) + 8 * (len(paneles) + 1), fh + 8), (26, 24, 36, 255))
    for i, im in enumerate(paneles):
        lienzo.paste(im, (i * (fw + 8) + 8, 4))
    lienzo.save(PREV)
    print(f'  {PREV}')
    print('LISTO')


if __name__ == '__main__':
    main()
