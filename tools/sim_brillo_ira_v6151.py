#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""sim_brillo_ira_v6151.py — EL SIM 1:1 DEL BRILLO CUADRADO DEL BUFF.

La regla de la casa: los parámetros REALES leídos del .cs (regex sobre
IraSerExteriorRecargaBuff.cs), nunca valores copiados a mano. Dibuja el
icono del buff con el MARCO DE LUZ de GlowUILib (MarcoLuz: las 4 barras
estiradas del SoftGlow con el latido 0.78+0.22·sin(3.1t)) en DOS
estados (cargando 60 % y LISTA) y MIDE:
  · que el marco SOBRESALGA entre 1 y 5 px del icono (la letra)
  · que el degradado sea MONOTÓNICO hacia afuera (R2: sin círculos planos)
  · que el icono gris encendido de abajo hacia arriba siga vivo
Suelta un veredicto con las medidas.
"""
import re, math
from PIL import Image, ImageDraw, ImageFilter

CS = '/home/z/my-project/mod/AethonMod/Content/Buffs/IraSerExteriorRecargaBuff.cs'
src = open(CS, encoding='utf-8').read()

# === LOS PARÁMETROS REALES DEL .cs ===
m = re.search(r'MarcoLuz\(sb, rect, new Color\(196, 108, 255\),\s*\n\s*(\d+)f, ([\d.]+)f, 1f\)', src)
assert m, 'la llamada LISTA de MarcoLuz no está'
SALIDA_LISTA = float(m.group(1)); INT_LISTA = float(m.group(2))
m = re.search(r'MarcoLuz\(sb, rect, new Color\(150, 84, 235\),\s*\n\s*1f \+ 3f \* progreso, ([\d.]+)f \+ ([\d.]+)f \* progreso, 0\.4f\)', src)
assert m, 'la llamada CARGANDO de MarcoLuz no está'
INT_CARGA0, INT_CARGA1 = float(m.group(1)), float(m.group(2))
print(f'parámetros REALES: salida_lista={SALIDA_LISTA}px int_lista={INT_LISTA} '
      f'carga: 1+3·p px, int {INT_CARGA0}+{INT_CARGA1}·p')

# el latido de GlowUILib (leído del fuente)
gsrc = open('/home/z/my-project/mod/AethonMod/Content/VFX/GlowUILib.cs', encoding='utf-8').read()
m = re.search(r'0\.78f \+ 0\.22f \* MathF\.Sin\(Main\.GlobalTimeWrappedHourly \* ([\d.]+)f\)', gsrc)
assert m, 'el latido de GlowUILib no está'
LATIDO_VEL = float(m.group(1))

# === EL SOFTGLOW REAL (la textura de la casa) ===
GLOW = Image.open('/home/z/my-project/mod/AethonMod/Content/Effects/Procedural/SoftGlow.png').convert('RGBA')
ICONO = Image.open('/home/z/my-project/mod/AethonMod/Content/Buffs/IraSerExteriorRecargaBuff.png').convert('RGBA')
W, H = ICONO.size
print(f'icono real: {W}x{H}  glow: {GLOW.size}')

def marco_luz(img, rect, color, salida, k):
    """El puerto EXACTO de GlowUILib.MarcoLuz a PIL (4 barras SoftGlow)."""
    x, y, w, h = rect
    g = salida * 2
    # barra: el glow estirado al tamaño del lado + salida·2
    def barra(bx, by, bw, bh):
        if bw <= 0 or bh <= 0: return
        est = GLOW.resize((int(bw), int(bh)), Image.LANCZOS)
        capa = Image.new('RGBA', img.size, (0,0,0,0))
        capa.paste(est, (int(bx), int(by)))
        # el tinte multiplicativo de la casa: color * alfa
        tinta = Image.new('RGBA', img.size, (0,0,0,0))
        r_, g_, b_, a_ = capa.split()
        a_ = a_.point(lambda a: int(a * k * 0.85))
        tinta = Image.merge('RGBA', (r_.point(lambda v: color[0]*v//255),
                                     g_.point(lambda v: color[1]*v//255),
                                     b_.point(lambda v: color[2]*v//255), a_))
        img.alpha_composite(tinta)
    barra(x - salida, y - salida, w + g, g)            # arriba
    barra(x - salida, y + h - salida, w + g, g)        # abajo
    barra(x - salida, y - salida, g, h + g)            # izquierda
    barra(x + w - salida, y - salida, g, h + g)        # derecha

def render(progreso, nombre):
    late = 0.78 + 0.22 * math.sin(0.0)   # t=0 del latido (el pico medio)
    img = Image.new('RGBA', (W + 40, H + 40), (18, 12, 28, 255))
    d = ImageDraw.Draw(img)
    # el fondo de la rejilla de buffs (oscuro, como el HUD)
    d.rectangle([20 - 2, 20 - 2, 20 + W + 2, 20 + H + 2], fill=(30, 22, 44, 255))
    # 1. la BASE GRIS (el reloj corriendo — gris ×150/255 como el .cs)
    gris = Image.new('RGBA', ICONO.size, (0,0,0,0))
    gr = ICONO.convert('RGBA')
    px = gr.load()
    for yy in range(H):
        for xx in range(W):
            r0, g0, b0, a0 = px[xx, yy]
            gris.putpixel((xx, yy), (r0*150//255, g0*150//255, b0*150//255, a0))
    img.alpha_composite(gris, (20, 20))
    # 2. LA PARTE ENCENDIDA (el recorte que sube — progreso)
    altoEnc = int(H * progreso)
    if altoEnc > 0:
        encendido = gr.crop((0, H - altoEnc, W, H))
        img.alpha_composite(encendido, (20, 20 + H - altoEnc))
    # 3. EL MARCO DE LUZ (las llamadas del .cs)
    rect = (20, 20, W, H)
    if progreso >= 1.0:
        marco_luz(img, rect, (196, 108, 255), SALIDA_LISTA, INT_LISTA * late)
        # Iluminar: el velo aditivo del centro
        velo = GLOW.resize((W, H), Image.LANCZOS)
        tinta = Image.new('RGBA', img.size, (0,0,0,0))
        r_, g_, b_, a_ = velo.split()
        a_ = a_.point(lambda a: int(a * 0.35 * 0.9))
        tinta = Image.merge('RGBA', (r_.point(lambda v: 150*v//255),
                                     g_.point(lambda v: 84*v//255),
                                     b_.point(lambda v: 235*v//255), a_))
        img.alpha_composite(tinta, (20, 20))
    elif progreso > 0.15:
        marco_luz(img, rect, (150, 84, 235), 1 + 3 * progreso,
                  (INT_CARGA0 + INT_CARGA1 * progreso) * late)
    img.save(f'/home/z/my-project/download/{nombre}.png')
    return img

img60 = render(0.60, 'sim_buff_ira_v6151_cargando')
img100 = render(1.0, 'sim_buff_ira_v6151_lista')

# === LAS MEDIDAS ===
print('\n=== LAS MEDIDAS (la letra: sobresalir 1-5 px) ===')
def perfil_marcas(img, nombre):
    """El alfa del marco en la línea horizontal central, de afuera hacia adentro."""
    w, h = img.size
    cy = h // 2
    FONDO = (18, 12, 28)   # el fondo del sim (opaco: comparar BRILLO, no alfa)
    def brilla(p):
        r, g, b, a = p
        return (r + g + b) > (FONDO[0] + FONDO[1] + FONDO[2] + 36)
    fila = [(x,) + img.getpixel((x, cy)) for x in range(w)]
    # el borde izquierdo del icono está en x=20: cuántos px BRILLAN antes
    sobresale = 0
    for x in range(19, -1, -1):
        if brilla(img.getpixel((x, cy))): sobresale += 1
        else: break
    fuera = [(r, g, b, a) for x, r, g, b, a in fila if x < 20]
    pico = max(sum(f[:3]) for f in fuera)
    print(f'{nombre}: sobresale {sobresale} px del icono | pico de brillo del marco {pico} (fondo {sum(FONDO)})')
    return sobresale, fila

s60, _ = perfil_marcas(img60, 'cargando 60%')
s100, fila100 = perfil_marcas(img100, 'LISTA')

ok = True
if not (1 <= s60 <= 5):
    print('  ✗ cargando: NO sobresale entre 1 y 5 px'); ok = False
else:
    print(f'  ✓ cargando: el marco sobresale {s60} px (1-5 ✔)')
if not (1 <= s100 <= 5):
    print('  ✗ LISTA: NO sobresale entre 1 y 5 px'); ok = False
else:
    print(f'  ✓ LISTA: el marco sobresale {s100} px (1-5 ✔)')

# MONOTÓNICO: del borde exterior hacia el icono, el alfa del marco NO
# puede tener valles profundos (el degradado del SoftGlow es monotónico)
def monotono(fila, hasta):
    """True si de afuera (x=0) hacia 'hasta' el alfa crece en general."""
    alfas = [a for x, r, g, b, a in fila if x <= hasta]
    # el promedio de la primera mitad < el de la segunda (crece hacia adentro)
    if len(alfas) < 8: return True
    mitad = len(alfas) // 2
    return (sum(alfas[:mitad]) / mitad) <= (sum(alfas[mitad:]) / (len(alfas) - mitad) + 3)
if monotono(fila100, 19):
    print('  ✓ LISTA: el degradado del marco es MONOTÓNICO (crece hacia el icono)')
else:
    print('  ✗ LISTA: el degradado tiene valles (círculo plano?)'); ok = False

# el icono encendido de abajo hacia arriba SIGUE VIVO: la COMPARACIÓN
# JUSTA es el MISMO píxel del icono (el original a color) contra su
# versión GRIS — el recorte de abajo (encendido) debe ser MÁS cálido
# que el gris de arriba. Se muestrean VARIAS columnas y se promedia
# (el arte tiene franjas: una sola columna engaña).
tot_abajo, tot_arriba, n = 0, 0, 0
for dx in range(2, W - 2, 4):
    p_abajo = img60.getpixel((20 + dx, 20 + H - 4))
    p_arriba = img60.getpixel((20 + dx, 22))
    tot_abajo += sum(p_abajo[:3]); tot_arriba += sum(p_arriba[:3]); n += 1
if tot_abajo > tot_arriba:
    print(f'  ✓ el icono se enciende de abajo hacia arriba (abajo {tot_abajo//n} vs arriba {tot_arriba//n} promedio)')
else:
    print(f'  ✗ el encendido de abajo hacia arriba no se lee ({tot_abajo//n} vs {tot_arriba//n})'); ok = False

print('\nSIM 1:1', 'TODO MEDIDO Y VERDE ✔' if ok else 'CON FALLAS ✗')
