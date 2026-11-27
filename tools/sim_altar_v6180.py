#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""sim_altar_v6180.py — v6.180 — EL SIM DEL LAYOUT DEL ALTAR.

La letra: «no veo el boton de ir al mundo interior y el altar dice que
ya esta activo» — la autopsia: los textos grandes de la v6.179
EMPUJABAN la sección del MUNDO fuera del papel (rec ~624 px: la sección
vivía al final y el Cabe() la saltaba entera: ni título ni botón).

EL SIM: reproduce el FLUJO EXACTO de AltarDelLibro.Izquierda (las
escalas nuevas de la v6.180) en los DOS casos extremos:
  (a) altar VACÍO con la puerta ACTIVA (el caso de la letra);
  (b) altar con ARMA + precio (el caso más alto de la página).
GATE: el BOTÓN DE LA PUERTA cae SIEMPRE dentro del papel.
"""
from PIL import Image, ImageDraw

# ═══ LA GEOMETRÍA DEL LIBRO (LibroDelEternoUI.Draw) ═══
H = 720          # min(720, screenH-40) — el libro a 1080p+
PAGE_TOP = 8     # pag.Y relativo = y+8
CUERPO_Y = PAGE_TOP + 26 + 30          # cy inicial del cuerpo +30 (el margen)
CUERPO_H = (H - 20) - CUERPO_Y - 12    # (y+h-20) - cuerpoY - 12
REC_BOTTOM = CUERPO_Y + CUERPO_H
print(f"papel del cuerpo: y={CUERPO_Y} → {REC_BOTTOM} ({CUERPO_H} px)")

# ═══ LA FUENTE (MouseText: «Ag» ≈ 21 px de línea) ═══
ALTO = 21.0

def linea(esc):
    return ALTO * esc + 3.0

def titulo(esc=0.95):
    return ALTO * esc + 9.0 + 12.0    # título + filete + aire

def boton():
    return 46.0 + 8.0                  # el rectángulo + el aire

def flujo(con_precio: bool, prestada: bool = False):
    """El flujo de cy de la página izquierda del altar (v6.180)."""
    marcas = {}
    cy = CUERPO_Y + 6.0
    cy += titulo()                     # Titulo de LA OFRENDA
    cy += 52.0 + 58.0                  # el slot + su aire
    cy += linea(0.95)                  # EtiquetaArma
    cy += linea(0.85)                  # FiltroArma
    if prestada: cy += linea(0.85)     # OfrendaPrestada
    cy += 6.0
    cy += titulo()                     # SelloTitulo
    if con_precio:
        cy += linea(0.92) * 3          # Cuesta + Llevas + Falta
    else:
        cy += linea(0.92)              # SelloSinOfrenda
    cy += 4.0 + boton()                # BOTÓN CONSAGRAR
    cy += 6.0
    cy += titulo()                     # MundoTitulo
    marcas["puerta_y"] = cy
    cy += boton()                      # ★ EL BOTÓN DE LA PUERTA
    marcas["puerta_fin"] = cy
    return cy, marcas

ok = True
casos = {
    "(a) altar VACÍO + puerta ACTIVA (la letra)": flujo(con_precio=False),
    "(b) altar con ARMA + precio (la página más alta)": flujo(con_precio=True),
    "(c) prestada + precio (el máximo)": flujo(con_precio=True, prestada=True),
}
print()
for nombre, (cy, marcas) in casos.items():
    cabe = marcas["puerta_fin"] <= REC_BOTTOM
    ok = ok and cabe
    print(f"{nombre}: botón de la puerta y={marcas['puerta_y']:.0f}→"
          f"{marcas['puerta_fin']:.0f} · {'DENTRO del papel ✔' if cabe else 'FUERA (MAL)'}")

# ═══ EL MOCK (la página con la puerta al fondo — ilustrativo) ═══
W = 500
img = Image.new("RGB", (W, H), (216, 200, 166))
dr = ImageDraw.Draw(img)
def bloq(y, h, t, c=(190, 172, 134)):
    dr.rectangle([30, y, W - 30, y + h], fill=c)
    dr.text((40, y + h / 2 - 6), t, fill=(28, 20, 16))
cy = 40
for t, h in [("TITULO — LA OFRENDA", 30), ("[ SLOT DE LA OFRENDA ]", 52),
            ("la etiqueta + el filtro", 44), ("EL SELLO", 30),
            ("Cuesta / Llevas / Falta", 60), ("[ CONSAGRAR ]", 46)]:
    bloq(cy, h, t); cy += h + 10
bloq(cy, 30, "EL MUNDO INTERIOR DEL ETERNO"); cy += 38
dr.rectangle([W/2 - 113, cy, W/2 + 113, cy + 46], fill=(214, 178, 106), outline=(120, 80, 30), width=3)
dr.text((W/2 - 70, cy + 16), "IR AL MUNDO INTERIOR", fill=(30, 20, 8))
cy += 46
dr.text((36, min(cy + 12, H - 30)),
        "GANA / CÓMO — ceden el papel si hace falta (la puerta NUNCA)",
        fill=(120, 96, 70))
out = "/tmp/sim_altar_v6180.png"
img.save(out)
print(f"\nmock: {out}")
print(f"GATE: {'SHIP ✔ — la puerta SIEMPRE dentro del papel' if ok else 'FALLA'}")
raise SystemExit(0 if ok else 1)
