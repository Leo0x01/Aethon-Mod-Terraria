#!/usr/bin/env python3
"""v6.00 — Texturas para LA GALAXIA VIVIENTE (LivingGalaxyStaff).

Investigación web (petición del usuario: "investiga galaxias en internet"):
galaxias espirales de DISEÑO PERFECTO (grand-design) como M51 (Remolino),
M101 (Molino), M74 y M100. Rasgos visuales clave confirmados por la
investigación (NASA/Caltech/COSMOS):
  - NÚCLEO/Bulbo CENTRAL AMARILLENO: estrellas VIEJAS (rojo-amarillo)
  - BRAZOS AZULES: estrellas jóvenes calientes trazan los brazos
  - NUDOS ROSAS (regiones HII): formación estelar brillante en los brazos
  - CARRILES DE POLVO OSCURO: a lo largo del borde INTERNO de los brazos
  - DISCO: brazos logarítmicos que se enrollan (2 grandes + secundarios)

v2 (crítica VLM 7.5/10 aplicada): brazos ASIMÉTRICOS (A grueso, B fino),
POLVO como POLILÍNEAS oscuras continuas que CORTAN el azul de los brazos,
HII VÍVIDOS ("que brillen como letreros de neón"), bulbo elíptico con
moteado + filamentos de polvo cruzando, icono de ALTO contraste con el
remolino en S grueso y muescas oscuras.

Genera (supersampleado x4 -> LANCZOS):
  - Effects/Procedural/SpiralGalaxy.png  (512px, el disco completo)
  - Weapons/Cosmic/LivingGalaxyStaff.png (30x30, icono)
  - Projectiles/Cosmic/LivingGalaxyProjectile.png (1x1 transparente)
  - Projectiles/Cosmic/GalaxyStarProjectile.png    (1x1 transparente)
"""
import math
import random
from PIL import Image, ImageDraw, ImageFilter

random.seed(20260601)  # semilla fija: la MISMA galaxia siempre

BASE = "/home/z/my-project/AethonMod/AethonMod"
SS = 4                 # supersampling


def blob(draw, x, y, r, color):
    """Elipse suave (se difumina después en capas)."""
    draw.ellipse([x - r, y - r, x + r, y + r], fill=color)


def gen_spiral_galaxy():
    W = 512 * SS
    cx = cy = W / 2
    R = W * 0.46                      # radio del disco

    # --- capa 1: HALO difuso (el disco viejo que envuelve todo) ---
    halo = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    d = ImageDraw.Draw(halo)
    steps = 90
    for i in range(steps, 0, -1):
        t = i / steps
        r = R * 1.05 * t
        a = int(26 * (1 - t) ** 1.6) + 2
        d.ellipse([cx - r, cy - r * 0.92, cx + r, cy + r * 0.92],
                  fill=(70, 85, 150, a))
    halo = halo.filter(ImageFilter.GaussianBlur(W * 0.03))

    # --- capa 2: BRAZOS ESPIRALES (logarítmicos, diseño perfecto) ---
    arms = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    d = ImageDraw.Draw(arms)

    def arm_points(base_angle, wind, r0, r1, b):
        """Espiral logarítmica: r = r0·e^(b·θ)."""
        pts = []
        th_max = math.log(r1 / r0) / b
        n = int(th_max * 70)
        for k in range(n):
            th = th_max * k / max(1, n - 1)
            r = r0 * math.exp(b * th)
            ang = base_angle + wind * th
            x = cx + r * math.cos(ang)
            y = cy + r * math.sin(ang) * 0.94   # ligeramente elíptico
            pts.append((x, y, r, th / th_max))
        return pts

    # 2 brazos principales (opuestos, ASIMÉTRICOS: A más denso que B)
    # + 2 secundarios tenues (v2 VLM: "real galaxies have slight
    # asymmetries, secondary spurs, one arm thicker")
    configs = [
        (0.0, 1, 1.0, 0.240),                  # brazo A (el GRUESO)
        (math.pi, 1, 0.78, 0.215),             # brazo B (más fino y abierto)
        (math.pi * 0.55, 1, 0.42, 0.26),       # secundario 1
        (math.pi * 1.55, 1, 0.42, 0.26),       # secundario 2
    ]
    for base, wind, power, bb in configs:
        pts = arm_points(base, wind, W * 0.045, R, bb)
        for idx, (x, y, r, t) in enumerate(pts):
            # la densidad CAE hacia fuera (los brazos se difuminan)
            dens = (1.0 - t * 0.55) * power
            n_stars = max(1, int(12 * dens))
            for _ in range(n_stars):
                # jitter perpendicular (grosor del brazo crece con r)
                wobble = random.gauss(0, 1) * (W * 0.012 + r * 0.10)
                ang = math.atan2(y - cy, x - cx) + math.pi / 2
                jx = x + wobble * math.cos(ang)
                jy = y + wobble * math.sin(ang) * 0.94
                rr = random.uniform(2.5, 8.0) * SS * (0.7 + dens * 0.5)
                # ESTRELLAS JÓVENES AZULES (la firma de los brazos)
                blue = random.choice([
                    (150, 195, 255, int(130 * dens) + 35),
                    (175, 215, 255, int(160 * dens) + 40),
                    (120, 175, 250, int(110 * dens) + 30),
                ])
                blob(d, jx, jy, rr, blue)
            # niebla azul tenue del brazo (gas)
            if idx % 2 == 0:
                rr = (W * 0.010 + r * 0.085) * (0.9 + dens * 0.4)
                blob(d, x, y, rr, (110, 150, 230, int(28 * dens) + 7))
            # NUDOS HII ROSAS VÍVIDOS (v2 VLM: "make them pop like neon
            # signs" — saturación y brillo +100%): cada ~14 pasos
            if idx % 14 == 7 and t > 0.15:
                wobble = random.gauss(0, 1) * (W * 0.010 + r * 0.08)
                ang = math.atan2(y - cy, x - cx) + math.pi / 2
                jx = x + wobble * math.cos(ang)
                jy = y + wobble * math.sin(ang) * 0.94
                blob(d, jx, jy, (11 + random.uniform(0, 6)) * SS,
                     (255, 110, 165, int(190 * power) + 55))
                blob(d, jx, jy, 5.5 * SS, (255, 175, 205, 210))
                blob(d, jx, jy, 2.2 * SS, (255, 240, 248, 255))

    arms = arms.filter(ImageFilter.GaussianBlur(3 * SS))

    # --- capa 2b: CARRILES DE POLVO OSCUROS (v2 VLM: "thin, very dark
    #     curved lines hugging the inner edges") — POLILÍNEAS conectadas
    #     que CORTAN el azul de los brazos, compuestas ENCIMA de ellos ---
    dust = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    d = ImageDraw.Draw(dust)
    for base, wind, power, bb in configs[:2]:        # solo los brazos principales
        pts = arm_points(base, wind, W * 0.06, R * 0.96, bb)
        for k in range(0, len(pts), 2):
            x, y, r, t = pts[k]
            if t < 0.10:
                continue
            inward = -1 if wind > 0 else 1
            ang = math.atan2(y - cy, x - cx) + math.pi / 2
            off = inward * (W * 0.004 + r * 0.045)
            dx = x + off * math.cos(ang)
            dy = y + off * math.sin(ang) * 0.94
            # filamento continuo: segmento hacia el siguiente punto
            if k + 2 < len(pts):
                x2, y2, r2, t2 = pts[k + 2]
                ang2 = math.atan2(y2 - cy, x2 - cx) + math.pi / 2
                off2 = inward * (W * 0.004 + r2 * 0.045)
                dx2 = x2 + off2 * math.cos(ang2)
                dy2 = y2 + off2 * math.sin(ang2) * 0.94
                wdt = max(2.5 * SS, (2.2 + r * 0.012) * SS) * (0.6 + 0.4 * power)
                d.line([(dx, dy), (dx2, dy2)],
                       fill=(16, 10, 9, int(150 * power) + 60),
                       width=int(wdt))
    dust = dust.filter(ImageFilter.GaussianBlur(1.1 * SS))

    # --- capa 3: el BULBO CENTRAL (amarillento, estrellas viejas; v2 VLM:
    #     estirado elíptico x1.28 + textura interna + filamentos de polvo
    #     cruzando cerca del centro) ---
    core = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    d = ImageDraw.Draw(core)
    for i in range(120, 0, -1):
        t = i / 120
        r = W * 0.088 * t
        if t > 0.75:
            col = (255, 252, 240, int(235 * t))
        elif t > 0.4:
            col = (255, 238, 195, int(150 * t))
        else:
            col = (255, 222, 160, int(60 * t))
        d.ellipse([cx - r * 1.28, cy - r, cx + r * 1.28, cy + r], fill=col)
    # moteado interno (estrellas viejas resueltas)
    for _ in range(160):
        ang = random.uniform(0, math.tau)
        rr = W * 0.07 * math.sqrt(random.uniform(0, 1))
        x = cx + rr * 1.28 * math.cos(ang)
        y = cy + rr * math.sin(ang)
        blob(d, x, y, random.uniform(1.5, 3.5) * SS,
             (255, 245, 220, random.randint(60, 150)))
    core = core.filter(ImageFilter.GaussianBlur(2 * SS))
    # filamentos de polvo cruzando el bulbo (los "grandes carriles" de M51)
    d = ImageDraw.Draw(core)
    d.arc([cx - W * 0.075, cy - W * 0.062, cx + W * 0.075, cy + W * 0.062],
          200, 340, fill=(30, 20, 17, 120), width=int(2.6 * SS))
    d.arc([cx - W * 0.082, cy - W * 0.068, cx + W * 0.082, cy + W * 0.068],
          15, 150, fill=(30, 20, 17, 95), width=int(2.2 * SS))
    # núcleo puntual extra nítido
    d2 = ImageDraw.Draw(core)
    blob(d2, cx, cy, 5 * SS, (255, 255, 252, 255))
    blob(d2, cx, cy, 2.4 * SS, (255, 255, 255, 255))

    # --- campo de estrellas disperso (disco difuso entre brazos) ---
    field = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    d = ImageDraw.Draw(field)
    for _ in range(900):
        ang = random.uniform(0, math.tau)
        rr = R * math.sqrt(random.uniform(0.05, 1.0)) * 0.96
        x = cx + rr * math.cos(ang)
        y = cy + rr * math.sin(ang) * 0.94
        b = random.randint(30, 110)
        col = random.choice([
            (b + 60, b + 80, min(255, b + 120), b),
            (255, 240, 210, max(20, b - 30)),
        ])
        blob(d, x, y, random.uniform(1.2, 3.2) * SS, col)
    field = field.filter(ImageFilter.GaussianBlur(1.2 * SS))

    # --- COMPOSICIÓN: halo → campo → brazos → POLVO (corta el azul) → bulbo ---
    out = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    out = Image.alpha_composite(out, halo)
    out = Image.alpha_composite(out, field)
    out = Image.alpha_composite(out, arms)
    out = Image.alpha_composite(out, dust)
    out = Image.alpha_composite(out, core)

    # estrellas BRILLANTES individuales (chispas legibles)
    d = ImageDraw.Draw(out)
    for _ in range(26):
        ang = random.uniform(0, math.tau)
        rr = R * random.uniform(0.15, 0.92)
        x = cx + rr * math.cos(ang)
        y = cy + rr * math.sin(ang) * 0.94
        blob(d, x, y, 1.6 * SS, (235, 245, 255, 235))
        blob(d, x, y, 0.8 * SS, (255, 255, 255, 255))

    return out.resize((512, 512), Image.LANCZOS)


def gen_staff_icon():
    """Icono 30x30 (v2 VLM: espiral perdida a esa escala -> contraste ALTO:
    núcleo blanco-oro brillante + remolino azul GRUESO en S + 2 muescas
    oscuras insinuando los brazos)."""
    W = 30 * SS
    img = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    # asta diagonal (madera oscura/obsidiana)
    d.line([(W * 0.30, W * 0.97), (W * 0.62, W * 0.34)],
           fill=(58, 42, 46, 255), width=int(3.4 * SS))
    d.line([(W * 0.335, W * 0.95), (W * 0.645, W * 0.36)],
           fill=(96, 70, 66, 255), width=int(1.6 * SS))

    # la GALAXIA en la cabeza: remolino GRUESO en S (2 arcos contrapuestos)
    gx, gy = W * 0.55, W * 0.30
    for i in range(30, 0, -1):
        t = i / 30
        r = W * 0.34 * t
        a = int(185 * (1 - t) ** 1.2) + 22
        col = (60, 105, 210, a) if t < 0.8 else (140, 190, 255, int(160 * (1 - t)))
        d.ellipse([gx - r * 1.08, gy - r, gx + r * 1.08, gy + r], fill=col)
    # los DOS brazos gruesos en S (contrapuestos, alta legibilidad)
    d.arc([gx - W * 0.30, gy - W * 0.28, gx + W * 0.30, gy + W * 0.28],
          95, 265, fill=(160, 210, 255, 255), width=int(3.0 * SS))
    d.arc([gx - W * 0.30, gy - W * 0.28, gx + W * 0.30, gy + W * 0.28],
          275, 85, fill=(120, 175, 250, 255), width=int(3.0 * SS))
    # muescas oscuras (el polvo que insinúa los brazos)
    d.arc([gx - W * 0.215, gy - W * 0.20, gx + W * 0.215, gy + W * 0.20],
          120, 240, fill=(15, 10, 20, 200), width=int(1.8 * SS))
    # núcleo brillante
    blob(d, gx, gy, W * 0.085, (255, 248, 225, 255))
    blob(d, gx, gy, W * 0.045, (255, 255, 255, 255))
    # chispas
    for (sx, sy, sr) in [(0.36, 0.14, 0.034), (0.74, 0.42, 0.030),
                         (0.62, 0.10, 0.024), (0.42, 0.46, 0.022),
                         (0.70, 0.20, 0.020)]:
        blob(d, W * sx, W * sy, W * sr, (225, 240, 255, 235))

    img = img.filter(ImageFilter.GaussianBlur(0.4 * SS))
    return img.resize((30, 30), Image.LANCZOS)


def gen_transparent_pixel(path):
    Image.new("RGBA", (1, 1), (0, 0, 0, 0)).save(path)


if __name__ == "__main__":
    gen_spiral_galaxy().save(f"{BASE}/Content/Effects/Procedural/SpiralGalaxy.png")
    print("SpiralGalaxy.png OK")
    gen_staff_icon().save(f"{BASE}/Content/Weapons/Cosmic/LivingGalaxyStaff.png")
    print("LivingGalaxyStaff.png OK")
    gen_transparent_pixel(f"{BASE}/Content/Projectiles/Cosmic/LivingGalaxyProjectile.png")
    print("LivingGalaxyProjectile.png OK")
    gen_transparent_pixel(f"{BASE}/Content/Projectiles/Cosmic/GalaxyStarProjectile.png")
    print("GalaxyStarProjectile.png OK")
