#!/usr/bin/env python3
"""v5.96 — Generación de texturas para EL OJO DEL VACÍO (VoidEye).

Todas se generan a 4x y se reducen con LANCZOS para anti-aliasing de calidad.
Paleta: marfil enfermizo + venas oscuras + iris ámbar + carne muerta.
"""
import math
import random
from PIL import Image, ImageDraw, ImageFilter

random.seed(20260711)  # semilla fija: determinista entre regeneraciones

S = 512  # tamaño final de las texturas del ojo
SS = 4   # supersampling


def lerp(a, b, t):
    return a + (b - a) * t


def smoothstep(e0, e1, x):
    t = max(0.0, min(1.0, (x - e0) / (e1 - e0)))
    return t * t * (3 - 2 * t)


# ================================================================
#  1. EYESCLERA — el blanco del ojo: marfil enfermizo + venas
# ================================================================
def gen_sclera():
    W = S * SS
    cx = cy = W / 2
    R = W * 0.495
    img = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    px = img.load()

    # Base radial: marfil pálido -> inyectado en sangre hacia el borde
    c_in = (240, 232, 208)   # marfil enfermizo
    c_mid = (232, 214, 186)
    c_out = (206, 152, 138)  # borde bloodshot

    for y in range(W):
        for x in range(W):
            dx, dy = x - cx, y - cy
            r = math.hypot(dx, dy) / R
            if r > 1.02:
                continue
            if r < 0.72:
                t = smoothstep(0.0, 0.72, r)
                c = tuple(int(lerp(c_in[i], c_mid[i], t)) for i in range(3))
            else:
                t = smoothstep(0.72, 1.0, r)
                c = tuple(int(lerp(c_mid[i], c_out[i], t)) for i in range(3))
            # alpha: núcleo sólido, borde suave
            a = 255
            if r > 0.93:
                a = int(255 * (1.0 - smoothstep(0.93, 1.0, r)))
            px[x, y] = (*c, a)

    # --- VENAS: caminos ramificados desde el borde hacia dentro ---
    draw = ImageDraw.Draw(img)

    def vein(angle0, r0, r1, width0, curve):
        """Camino curvado del radio r0 al r1 en el ángulo angle0 (+curva)."""
        steps = 46
        pts = []
        for i in range(steps + 1):
            t = i / steps
            rr = lerp(r0, r1, t)
            ang = angle0 + curve * t * t
            pts.append((cx + math.cos(ang) * rr, cy + math.sin(ang) * rr))
        for i in range(steps):
            t = i / steps
            wline = max(1.2, lerp(width0, width0 * 0.28, t))
            alpha = int(215 * (1.0 - t * 0.45))
            col = (138, 22, 28, alpha)
            draw.line([pts[i], pts[i + 1]], fill=col, width=int(wline))
        # rama secundaria en el 40% del recorrido
        if random.random() < 0.85:
            ti = 0.42
            rr = lerp(r0, r1, ti)
            ang = angle0 + curve * ti * ti
            bx, by = cx + math.cos(ang) * rr, cy + math.sin(ang) * rr
            ang2 = ang + random.uniform(-0.7, 0.7)
            r2 = rr - R * random.uniform(0.10, 0.20)
            pts2 = [(bx, by)]
            steps2 = 22
            for j in range(1, steps2 + 1):
                t = j / steps2
                rr2 = lerp(rr, r2, t)
                a2 = ang2 + curve * 0.4 * t
                pts2.append((cx + math.cos(a2) * rr2, cy + math.sin(a2) * rr2))
            for j in range(steps2):
                wl = max(1.0, lerp(width0 * 0.55, 0.9, j / steps2))
                draw.line([pts2[j], pts2[j + 1]], fill=(146, 26, 30, 185), width=int(wl))

    for k in range(15):
        ang = random.uniform(0, math.tau)
        r0 = R * random.uniform(0.80, 0.95)
        r1 = R * random.uniform(0.46, 0.58)
        w = random.uniform(3.0, 5.2) * SS * 0.5
        vein(ang, r0, r1, w, random.uniform(-0.9, 0.9))

    # sombra interna sutil alrededor del iris (receptáculo ocular)
    shadow = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    sd = ImageDraw.Draw(shadow)
    sd.ellipse([cx - R * 0.56, cy - R * 0.56, cx + R * 0.56, cy + R * 0.56],
               outline=(70, 14, 20, 90), width=int(10 * SS))
    shadow = shadow.filter(ImageFilter.GaussianBlur(9 * SS))
    img = Image.alpha_composite(img, shadow)

    return img.resize((S, S), Image.LANCZOS)


# ================================================================
#  2. EYEIRIS — iris ámbar con estrías radiales + anillo limbal
# ================================================================
def gen_iris():
    W = S * SS
    cx = cy = W / 2
    R = W * 0.49
    img = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    px = img.load()

    for y in range(W):
        for x in range(W):
            dx, dy = x - cx, y - cy
            r = math.hypot(dx, dy) / R
            if r > 1.01:
                continue
            # base ámbar con gradiente radial (más claro cerca de la pupila)
            base = (168, 104, 30)
            inner = (236, 178, 92)
            dark = (118, 66, 16)
            if r < 0.30:
                t = smoothstep(0.16, 0.30, r)
                c = tuple(int(lerp(inner[i], base[i], t)) for i in range(3))
            elif r < 0.84:
                t = smoothstep(0.30, 0.84, r)
                c = tuple(int(lerp(base[i], dark[i], t)) for i in range(3))
            else:
                # anillo limbal oscuro
                t = smoothstep(0.84, 0.96, r)
                limbal = (44, 20, 8)
                c = tuple(int(lerp(dark[i], limbal[i], t)) for i in range(3))
            a = 255 if r < 0.97 else int(255 * (1 - smoothstep(0.97, 1.0, r)))
            px[x, y] = (*c, a)

    draw = ImageDraw.Draw(img)

    # --- ESTRÍAS RADIALES (fibras del iris) ---
    n = 130
    for k in range(n):
        ang = (k / n) * math.tau + random.uniform(-0.012, 0.012)
        r_in = R * random.uniform(0.20, 0.30)
        r_out = R * random.uniform(0.82, 0.94)
        curve = random.uniform(-0.10, 0.10)
        steps = 30
        bright = random.uniform(0.0, 1.0)
        if bright > 0.72:
            col = (255, 214, 120, 190)   # fibra clara brillante
        elif bright > 0.40:
            col = (232, 170, 78, 165)
        else:
            col = (128, 74, 22, 150)     # fibra oscura (surco)
        w = random.uniform(1.2, 3.4) * SS * 0.5
        pts = []
        for i in range(steps + 1):
            t = i / steps
            rr = lerp(r_in, r_out, t)
            a = ang + curve * t
            pts.append((cx + math.cos(a) * rr, cy + math.sin(a) * rr))
        for i in range(steps):
            wl = max(0.8, w * lerp(1.15, 0.55, i / steps))
            draw.line([pts[i], pts[i + 1]], fill=col, width=int(wl))

    # --- CRIPTAS: manchas oscuras orgánicas a radio medio ---
    for k in range(22):
        ang = random.uniform(0, math.tau)
        rr = R * random.uniform(0.42, 0.72)
        x0 = cx + math.cos(ang) * rr
        y0 = cy + math.sin(ang) * rr
        rw = random.uniform(4, 11) * SS * 0.5
        rh = rw * random.uniform(1.6, 2.8)
        rot = ang + math.pi / 2
        blob = Image.new("RGBA", (W, W), (0, 0, 0, 0))
        bd = ImageDraw.Draw(blob)
        bd.ellipse([x0 - rw, y0 - rh, x0 + rw, y0 + rh], fill=(86, 44, 12, 120))
        blob = blob.rotate(math.degrees(rot), center=(x0, y0), resample=Image.BICUBIC)
        blob = blob.filter(ImageFilter.GaussianBlur(2.2 * SS))
        img = Image.alpha_composite(img, blob)

    # brillo especular sutil (ojo húmedo): luna arriba-izquierda
    spec = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    sd = ImageDraw.Draw(spec)
    sx, sy = cx - R * 0.34, cy - R * 0.38
    sd.ellipse([sx - R * 0.16, sy - R * 0.10, sx + R * 0.16, sy + R * 0.10],
               fill=(255, 244, 214, 70))
    spec = spec.filter(ImageFilter.GaussianBlur(7 * SS))
    img = Image.alpha_composite(img, spec)

    return img.resize((S, S), Image.LANCZOS)


# ================================================================
#  3. EYELID — párpado superior (carne muerta); el inferior = flip
# ================================================================
def gen_lid():
    W = S * SS
    img = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    px = img.load()

    # margen del párpado: arco que baja por el centro (bulge)
    def margin_y(x):
        t = (x / W) * 2 - 1
        return W * (0.455 - 0.085 * (1 - t * t))

    # domo superior: silueta redondeada (superellipse suave)
    def dome_y(x):
        t = max(-1.0, min(1.0, (x / W) * 2 - 1))
        return W * (0.05 + 0.05 * abs(t) ** 1.5)

    for y in range(W):
        for x in range(W):
            my = margin_y(x)
            dy = dome_y(x)
            if y < dy or y > my:
                continue  # fuera de la carne
            # color: carne oscura, más clara cerca del margen (húmeda)
            # (v2: base algo más clara para que el domo se lea sobre la estrella)
            t = smoothstep(dy, my, y)  # 0 arriba, 1 en el margen
            base = (36, 17, 20)
            wet = (92, 40, 42)
            c = tuple(int(lerp(base[i], wet[i], t ** 2.0)) for i in range(3))
            # alpha: dura en el margen, suave en el domo superior
            a = 255
            if y < dy + W * 0.03:
                a = int(255 * smoothstep(dy, dy + W * 0.03, y))
            if y > my - W * 0.008:
                # borde AA del margen
                a = int(a * (1 - smoothstep(my - W * 0.008, my, y)))
            px[x, y] = (*c, a)

    draw = ImageDraw.Draw(img)

    # --- MARGEN del párpado: línea carmesí (reborde húmedo) ---
    for x in range(0, W, 2):
        my = margin_y(x)
        draw.line([(x, my - 2.6 * SS), (x, my + 2.0 * SS)],
                  fill=(128, 26, 32, 235), width=max(2, int(1.7 * SS)))
    for x in range(0, W, 3):
        my = margin_y(x)
        draw.line([(x, my - 5.2 * SS), (x, my - 2.8 * SS)],
                  fill=(165, 62, 62, 130), width=max(2, int(1.2 * SS)))

    # --- PLIEGUES horizontales de piel vieja ---
    for k in range(6):
        off = W * (0.10 + 0.055 * k)
        col = (14, 5, 7, 60)
        pts = []
        for x in range(0, W, 8):
            my = margin_y(x) - off
            pts.append((x, my))
        draw.line(pts, fill=col, width=max(1, int(0.9 * SS)), joint="curve")

    # --- espinas carnosas irregulares (sigilos del vacío), sutiles ---
    for k in range(9):
        x0 = random.uniform(0.12, 0.88) * W
        my = margin_y(x0)
        ln = random.uniform(5, 13) * SS * 0.5
        ang = random.uniform(-0.25, 0.25)
        draw.line([(x0, my - 2 * SS), (x0 + math.sin(ang) * ln, my - 2 * SS - math.cos(ang) * ln)],
                  fill=(60, 16, 22, 160), width=max(1, int(1.1 * SS)))

    img = img.filter(ImageFilter.GaussianBlur(0.6 * SS))
    return img.resize((S, S), Image.LANCZOS)


# ================================================================
#  4. VOIDEYESTAFF — icono 28x30 (staff oscuro con el ojo arriba)
# ================================================================
def gen_staff_icon():
    W, H = 28 * 6, 30 * 6  # 6x para nitidez
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    # vara: de abajo-izquierda a arriba-derecha
    x0, y0 = int(W * 0.22), int(H * 0.96)
    x1, y1 = int(W * 0.60), int(H * 0.34)
    draw.line([(x0, y0), (x1, y1)], fill=(38, 26, 34, 255), width=int(W * 0.11))
    # highlight de la vara
    draw.line([(x0 + 2, y0), (x1 + 2, y1 - 2)], fill=(74, 52, 66, 200), width=int(W * 0.045))
    # nudo de la vara
    kn = int(W * 0.075)
    kx, ky = int(W * 0.42), int(H * 0.66)
    draw.ellipse([kx - kn, ky - kn, kx + kn, ky + kn], fill=(30, 18, 26, 255))

    # aura carmesí de fondo
    glow = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    gd = ImageDraw.Draw(glow)
    ex, ey = int(W * 0.66), int(H * 0.26)
    gr = int(W * 0.30)
    gd.ellipse([ex - gr, ey - gr, ex + gr, ey + gr], fill=(120, 10, 26, 110))
    glow = glow.filter(ImageFilter.GaussianBlur(W * 0.09))
    img = Image.alpha_composite(img, glow)
    draw = ImageDraw.Draw(img)

    # esclera
    r_s = int(W * 0.155)
    draw.ellipse([ex - r_s, ey - r_s, ex + r_s, ey + r_s], fill=(226, 216, 190, 255))
    # vena
    draw.arc([ex - r_s, ey - r_s, ex + r_s, ey + r_s], 200, 260, fill=(150, 30, 36, 255), width=2)
    # iris
    r_i = int(W * 0.085)
    draw.ellipse([ex - r_i, ey - r_i, ex + r_i, ey + r_i], fill=(196, 122, 34, 255))
    # pupila
    r_p = int(W * 0.042)
    draw.ellipse([ex - r_p, ey - r_p, ex + r_p, ey + r_p], fill=(8, 4, 6, 255))
    # párpado superior cayendo (carne): elipse achatada que cubre la parte alta
    draw.ellipse([ex - r_s, ey - r_s * 1.62, ex + r_s, ey + r_s * 0.62],
                 fill=(30, 12, 16, 255))
    # borde carmesí del margen del párpado
    draw.arc([ex - r_s, ey - r_s * 1.62, ex + r_s, ey + r_s * 0.62], 20, 160,
             fill=(130, 28, 34, 255), width=2)

    # brillo especular
    draw.ellipse([ex - r_s * 0.45, ey - r_s * 0.10, ex - r_s * 0.15, ey + r_s * 0.16],
                 fill=(255, 246, 220, 210))

    # dos zarcillos oscuros bajo el ojo
    draw.arc([ex - int(W * 0.34), ey - int(W * 0.02), ex - int(W * 0.10), ey + int(W * 0.30)],
             250, 330, fill=(52, 22, 32, 235), width=3)
    draw.arc([ex + int(W * 0.10), ey - int(W * 0.02), ex + int(W * 0.34), ey + int(W * 0.30)],
             210, 290, fill=(52, 22, 32, 235), width=3)

    return img.resize((28, 30), Image.LANCZOS)


# ================================================================
#  5. VOIDEYEPROJECTILE — 1x1 transparente (nunca se dibuja su sprite)
# ================================================================
def gen_transparent_pixel(path):
    Image.new("RGBA", (1, 1), (0, 0, 0, 0)).save(path)


if __name__ == "__main__":
    base = "/home/z/my-project/AethonMod/AethonMod/Content"
    gen_sclera().save(f"{base}/Effects/Procedural/EyeSclera.png")
    print("EyeSclera.png OK")
    gen_iris().save(f"{base}/Effects/Procedural/EyeIris.png")
    print("EyeIris.png OK")
    gen_lid().save(f"{base}/Effects/Procedural/EyeLid.png")
    print("EyeLid.png OK")
    gen_staff_icon().save(f"{base}/Weapons/Cosmic/VoidEyeStaff.png")
    print("VoidEyeStaff.png OK")
    gen_transparent_pixel(f"{base}/Projectiles/Cosmic/VoidEyeProjectile.png")
    print("VoidEyeProjectile.png OK")
