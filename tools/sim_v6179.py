#!/usr/bin/env python3
"""sim_v6179.py — SIM 1:1 de la ventana flotante + slot del trío + botones.

Los parámetros REALES del .cs portados a PIL (gate 6 de la casa):
  · SelectorFlotante: celda 44→50 · icono 32→36 · título 1.0→1.3 (banda 50)
    · placa del nombre 0.92→1.2 · pila 0.78→0.95 · pista 0.78→1.0
  · El SLOT DEL TRÍO: MUDO (sin nombre de 60px que se encogía a lo invisible)
  · Botones: base 1.05→1.2 · piso de legibilidad 0.72→1.0 (con elipsis)
El texto vanilla de Terraria (MouseText) se aproxima con DejaVu Sans a la
escala 1:1 — lo que se mide son LAS ESCALAS y los ENCAJES, no la caligrafía.
"""
from PIL import Image, ImageDraw, ImageFont

ESCALA = 2  # render 2× para nitidez; las medidas se reportan en unidades 1×
FACTOR = 18  # px por línea de texto a escala 1.0 (aprox. MouseText in-game)

W, H = 1000, 760
img = Image.new("RGB", (W, H), (28, 24, 34))
d = ImageDraw.Draw(img)


def fuente(esc):
    return ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf",
                              max(7, int(round(FACTOR * esc * ESCALA * 0.62))))


def texto(x, y, s, esc, tinta, ancla="mm"):
    f = fuente(esc)
    bb = d.textbbox((0, 0), s, font=f)
    w, h = bb[2] - bb[0], bb[3] - bb[1]
    if ancla == "mm":
        x, y = x - w / 2, y - h / 2
    d.text((x, y), s, font=f, fill=tinta)
    return w, h


# ============================ LA VENTANA FLOTANTE ============================
Columnas, Celda, AltoTitulo = 6, 50, 50
w_ventana = Columnas * Celda + 20
x0, y0 = 60, 60
d.rectangle([x0 + 3, y0 + 3, x0 + 3 + w_ventana, y0 + 3 + 150],
            fill=(150, 132, 100))
d.rectangle([x0, y0, x0 + w_ventana, y0 + 150], fill=(216, 200, 166))
d.rectangle([x0, y0, x0 + w_ventana, y0 + 150], outline=(168, 108, 44), width=3)

# EL TÍTULO a 1.3 — debe caber en la banda de 50
titulo = "ELIGE UN ARMA"
w_t, h_t = texto(x0 + w_ventana / 2, y0 + AltoTitulo * ESCALA * 0.31, titulo, 1.3, (148, 72, 178))
d.line([x0 + 10, y0 + AltoTitulo - 6, x0 + w_ventana - 10, y0 + AltoTitulo - 6],
       fill=(168, 108, 44), width=2)
print(f"[título 1.3] alto={h_t / ESCALA:.0f}px (banda 50px, margen "
      f"{AltoTitulo - h_t / ESCALA:.0f}px) → {'CABE ✔' if h_t / ESCALA <= 46 else 'DESBORDA ✗'}")

# LAS CELDAS (6 columnas de 50) con iconos de 36 y la PILA a 0.95
vitrina_y = y0 + AltoTitulo
d.rectangle([x0 + 8, vitrina_y, x0 + w_ventana - 8, vitrina_y + Celda],
            fill=(190, 172, 134))
for c in range(Columnas):
    cx = x0 + 11 + c * Celda
    d.rectangle([cx, vitrina_y + 2, cx + Celda - 4, vitrina_y + Celda - 2],
                fill=(216, 200, 166), outline=(196, 110, 228), width=2)
    # el icono de 36 (una gema simple)
    d.ellipse([cx + 7, vitrina_y + 7, cx + Celda - 11, vitrina_y + Celda - 11],
              outline=(148, 72, 178), width=3)
    if c == 0:
        texto(cx + Celda - 8, vitrina_y + Celda - 8, "999", 0.95, (28, 20, 16), "rm")
print(f"[celdas] 6×{Celda}px con icono 36 y pila 0.95 (antes: celda 44 · icono 32 · pila 0.78)")

# LA PLACA DEL NOMBRE a 1.2
nombre = "Mirada del Eterno"
placa_w = int(len(nombre) * 10)
px = int(x0 + w_ventana / 2 - placa_w / 2)
py = vitrina_y - 52
d.rectangle([px, py, px + placa_w, py + 34], fill=(216, 200, 166),
            outline=(168, 108, 44), width=2)
w_n, h_n = texto(px + placa_w / 2, py + 17, nombre, 1.2, (28, 20, 16))
print(f"[placa nombre 1.2] alto={h_n / ESCALA:.0f}px (antes 0.92: "
      f"{h_n / ESCALA * 0.92 / 1.2:.0f}px) → legible de un vistazo")

# ============================ EL SLOT DEL TRÍO (MUDO) =========================
sx, sy = 60, 320
for i in range(3):
    x = sx + i * 66
    d.rectangle([x, sy, x + 62, sy + 62], fill=(216, 200, 166),
                outline=(168, 108, 44), width=3)
    if i == 0:  # LLENA: SOLO el arma (icono centrado, 48px) — SIN texto
        d.ellipse([x + 10, sy + 10, x + 52, sy + 52], outline=(148, 72, 178), width=4)
        d.ellipse([x + 24, sy + 24, x + 38, sy + 38], fill=(255, 90, 70))
    # else: VACÍA y MUDA (antes: el dígito 1/2/3)
print("[slot trío] LLENO = solo el arma de 48px · VACÍO = MUDO (el dígito murió)")

# EL TEXTO DE LAS LÍNEAS: antes 0.88 → ahora 1.1
y = sy + 90
for s, esc_vieja, esc_nueva in [("Mantén tres armas consagradas aquí (1.1)", 0.88, 1.1),
                                ("El albedrío de cada ranura (1.0)", 0.84, 1.0)]:
    texto(240, y, s, 1.0, (216, 200, 166), "lm")
    y += 34

# ============================ EL BOTÓN (base 1.2, piso 1.0) ===================
bx, by, bw, bh = 300, 480, 226, 46
d.rectangle([bx, by, bx + bw, by + bh], fill=(60, 40, 70), outline=(196, 110, 228), width=3)
texto(bx + bw / 2, by + bh / 2, "IR AL MUNDO INTERIOR", 1.2, (240, 230, 255))
d.line([bx, by - 12, bx + bw, by - 12], fill=(120, 100, 90), width=2)
texto(bx + bw / 2, by - 12, "piso de legibilidad: 1.0 (antes 0.72)", 0.8, (150, 140, 130))
print("[botón] base 1.2 · piso 1.0 con elipsis — el texto JAMÁS baja de la legibilidad")

# ============================ EL RÓTULO DE FASE ================================
d.rectangle([W / 2 - 88, 600, W / 2 + 88, 616], fill=(20, 12, 16),
            outline=(90, 60, 90), width=2)
texto(W / 2, 608, "Fase 2 · la proyección", 0.85, (255, 255, 255))
print("[HUD fase] el rótulo de la fase viva junto a los hechizos (la barra de energía murió)")

d.text((60, 700), "SIM v6.179 · la ventana que se lee · el slot mudo · los botones a 1.2",
       font=fuente(0.8), fill=(170, 160, 150))

img.save("/home/z/my-project/download/sim_ui_v6179.png")
print("OK: download/sim_ui_v6179.png")
