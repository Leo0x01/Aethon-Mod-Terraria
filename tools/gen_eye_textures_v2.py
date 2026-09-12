#!/usr/bin/env python3
"""v5.99 — Texturas v2b del OJO DEL VACÍO (segunda iteración tras review).

Mejoras sobre v2 tras crítica visual:
  - IRIS: generación PER-PIXEL con campos de ruido (turbulencia orgánica),
    no arcos dibujados "estampados": bandas onduladas por ruido angular +
    fibras radiales finas + criptas caóticas + grano estelar.
  - ESCLERÓTICA: venas AUDACES (núcleo oscuro + halo claro, tonos rojo→
    violeta, ramificación real), moteado biológico, especular EN MEDIA LUNA
    (distorsión de córnea), mojado (brillo húmedo).
  - PÁRPADO/ICONO sin cambios (ya aprobados visualmente).
"""
import math
import random
from PIL import Image, ImageDraw, ImageFilter

random.seed(20260913)

ROOT = "/home/z/my-project/AethonMod/AethonMod"


def lerp(a, b, t):
    return a + (b - a) * t


def lerp3(c1, c2, t):
    return tuple(int(round(lerp(c1[i], c2[i], t))) for i in range(3))


def clamp(x, lo, hi):
    return max(lo, min(hi, x))


def smoothstep(e0, e1, x):
    t = clamp((x - e0) / (e1 - e0), 0.0, 1.0)
    return t * t * (3.0 - 2.0 * t)


# --- Ruido de valor 2D (turbulencia orgánica, determinista) ---
_PSIZE = 256
_perm = list(range(_PSIZE))
random.shuffle(_perm)
_perm = _perm + _perm
_grads = []
for i in range(_PSIZE):
    a = i * math.tau / _PSIZE
    _grads.append((math.cos(a), math.sin(a)))


def vnoise(x, y):
    xi, yi = int(math.floor(x)) % _PSIZE, int(math.floor(y)) % _PSIZE
    xf, yf = x - math.floor(x), y - math.floor(y)
    u = xf * xf * (3 - 2 * xf)
    v = yf * yf * (3 - 2 * yf)
    g = lambda gx, gy: _grads[_perm[(_perm[gx % _PSIZE] + gy) % _PSIZE]]
    n00 = g(xi, yi)
    n10 = g(xi + 1, yi)
    n01 = g(xi, yi + 1)
    n11 = g(xi + 1, yi + 1)
    d = lambda gg, dx, dy: gg[0] * dx + gg[1] * dy
    nx0 = lerp(d(n00, xf, yf), d(n10, xf - 1, yf), u)
    nx1 = lerp(d(n01, xf, yf - 1), d(n11, xf - 1, yf - 1), u)
    return lerp(nx0, nx1, v)  # ≈ [-1, 1]


def fbm(x, y, octaves=4):
    val = 0.0
    amp = 0.5
    freq = 1.0
    for o in range(octaves):
        val += amp * vnoise(x * freq, y * freq)
        freq *= 2.17
        amp *= 0.5
    return val  # ≈ [-1, 1]


# ================================================================
#  1. ESCLERÓTICA v2b
# ================================================================
def gen_sclera():
    S = 512
    SS = 4
    W = S * SS
    cx = cy = W / 2
    R = W * 0.49
    img = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    px = img.load()

    c_core = (244, 240, 228)
    c_mid = (230, 222, 204)
    c_limb = (172, 156, 134)
    c_edge = (106, 90, 78)

    for y in range(W):
        for x in range(W):
            dx, dy = x - cx, y - cy
            d = math.sqrt(dx * dx + dy * dy)
            if d > R:
                continue
            rn = d / R
            nx, ny = dx / R, dy / R
            nz = math.sqrt(max(0.0, 1.0 - nx * nx - ny * ny))
            light = clamp(0.62 + 0.38 * (nz * 0.82 + (-nx - ny) * 0.13), 0.0, 1.0)
            # moteado biológico (manchas de tejido, MUY sutil)
            mottle = fbm(x * 0.008, y * 0.008, 3) * 7.0
            limb = smoothstep(0.60, 0.98, rn)
            base = lerp3(c_core, c_mid, smoothstep(0.08, 0.6, rn) * 0.5)
            base = lerp3(base, c_limb, limb * 0.75)
            if rn > 0.86:
                base = lerp3(base, c_edge, smoothstep(0.86, 1.0, rn) * 0.85)
            r = int(base[0] * (0.55 + 0.45 * light) + mottle)
            g = int(base[1] * (0.55 + 0.45 * light) + mottle * 0.8)
            b = int(base[2] * (0.58 + 0.42 * light) + mottle * 0.6)
            # subsurface cálido abajo
            warm = clamp((-ny) * 0.5 + 0.5, 0.0, 1.0) * clamp(nz, 0, 1) \
                * smoothstep(0.4, 0.0, rn) * 0.4
            r = int(r + warm * 52)
            g = int(g + warm * 20)
            b = int(b + warm * 2)
            a = 255 if rn < 0.985 else int(255 * clamp((1 - rn) / 0.015, 0, 1))
            px[x, y] = (r, g, b, a)

    # --- VENAS AUDACES: núcleo oscuro + halo, rojo→violeta, 3D-ish ---
    veins = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    vd = ImageDraw.Draw(veins)
    branches = []
    for i in range(22):
        ang = random.uniform(0, math.tau)
        start_r = R * random.uniform(0.78, 0.95)
        x = cx + math.cos(ang) * start_r
        y = cy + math.sin(ang) * start_r
        dirr = ang + math.pi + random.uniform(-0.45, 0.45)
        # tono de vena: rojo carmesí o azul-violeta (como ojos reales)
        if random.random() < 0.6:
            core, halo = (128, 34, 40), (196, 96, 92)
        else:
            core, halo = (96, 54, 108), (168, 116, 172)
        branches.append([x, y, dirr, random.uniform(0.85, 1.2), core, halo,
                         random.uniform(1.6, 3.2)])

    for step in range(72):
        for br in branches:
            x, y, dirr, life, core, halo, wdt = br
            speed = random.uniform(2.0, 3.4) * SS * 0.6
            dirr += random.uniform(-0.34, 0.34)
            nx = x + math.cos(dirr) * speed
            ny = y + math.sin(dirr) * speed
            dxn, dyn = nx - cx, ny - cy
            dn = math.sqrt(dxn * dxn + dyn * dyn)
            if dn < R * 0.38:
                life -= 0.10  # se desvanecen ANTES del iris
            if dn > R * 0.995 or life <= 0:
                br[3] = -1
                continue
            w = max(1, int(wdt * SS * life))
            va = int(clamp(200 * life, 20, 220))
            # HALO (halo claro alrededor de la vena — efecto 3D de vasos)
            vd.line([(x, y), (nx, ny)],
                    fill=(halo[0], halo[1], halo[2], va // 3), width=w * 3)
            # NÚCLEO oscuro
            vd.line([(x, y), (nx, ny)],
                    fill=(core[0], core[1], core[2], va), width=w)
            br[0], br[1] = nx, ny
            br[2] = dirr
            br[3] = life - 0.02
            br[6] = wdt * 0.985
            if random.random() < 0.06 and life > 0.4:
                branches.append([nx, ny, dirr + random.choice([-1, 1]) *
                                 random.uniform(0.55, 1.0), life * 0.65,
                                 core, halo, wdt * 0.62])
        branches = [b for b in branches if b[3] > 0]
        if len(branches) > 110:
            branches = branches[:110]
    veins = veins.filter(ImageFilter.GaussianBlur(SS * 0.55))
    img = Image.alpha_composite(img, veins)

    # --- ESPECULAR EN MEDIA LUNA (córneal, distorsionada) ---
    spec = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    sd = ImageDraw.Draw(spec)
    # arco grueso arriba-izquierda (la córnea curva la luz en creciente)
    arcR = R * 0.62
    for k, (w2, a2) in enumerate([(SS * 10, 88), (SS * 5, 120), (SS * 2, 150)]):
        sd.arc([cx - arcR, cy - arcR, cx + arcR, cy + arcR],
               196, 258, fill=(255, 255, 250, a2), width=w2)
    # micro-brillo inferior derecho (luz reflejada secundaria, tenue)
    sd.ellipse([cx + R * 0.30, cy + R * 0.26,
                cx + R * 0.44, cy + R * 0.40],
               fill=(255, 255, 250, 46))
    img = Image.alpha_composite(img, spec.filter(ImageFilter.GaussianBlur(SS * 2.6)))

    # --- viñeta del limbo ---
    vin = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    vdn = ImageDraw.Draw(vin)
    vdn.ellipse([cx - R, cy - R, cx + R, cy + R],
                outline=(30, 16, 20, 200), width=int(R * 0.05))
    img = Image.alpha_composite(img, vin.filter(ImageFilter.GaussianBlur(SS * 6)))

    return img.resize((S, S), Image.LANCZOS)


# ================================================================
#  2. IRIS v2b — PER-PIXEL con turbulencia (materia en órbita caótica)
# ================================================================
def gen_iris():
    S = 512
    SS = 4
    W = S * SS
    cx = cy = W / 2
    R = W * 0.49
    img = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    px = img.load()

    c_outer = (92, 56, 30)
    c_mid = (168, 108, 46)
    c_hot = (232, 176, 84)
    c_inner = (255, 214, 122)
    c_limbal = (42, 24, 40)

    for y in range(W):
        for x in range(W):
            dx, dy = x - cx, y - cy
            d = math.sqrt(dx * dx + dy * dy)
            if d > R:
                continue
            rn = d / R
            ang = math.atan2(dy, dx)

            # --- TURBULENCIA: el radio se deforma con ruido angular ---
            # (esto convierte las bandas concéntricas en remolinos orgánicos)
            swirl = fbm(math.cos(ang) * 2.2 + 5.0, math.sin(ang) * 2.2 + 5.0, 4)
            rn_w = rn + swirl * 0.045 * (0.35 + 0.65 * rn)  # más caos fuera

            # --- BANDAS ORBITALES: brillo de materia girando en anillos ---
            band_t = rn_w * 9.0 + swirl * 2.0
            bands = 0.5 + 0.5 * math.sin(band_t * math.tau * 0.5 +
                                         fbm(x * 0.006, y * 0.006, 3) * 3.0)
            bands = bands ** 1.4  # contraste: filas brillantes y surcos

            # --- FIBRAS RADIALES finas (la firma de un iris) ---
            fib_n = 96.0
            fiber = 0.5 + 0.5 * math.sin(ang * fib_n + rn_w * 18.0 +
                                         fbm(math.cos(ang) * 3.5,
                                             math.sin(ang) * 3.5, 2) * 4.0)
            fiber *= smoothstep(0.28, 0.6, rn) * (1.0 - smoothstep(0.86, 1.0, rn))

            # --- CRIPTAS: huecos oscuros caóticos (blotches angulares) ---
            crypt = 0.5 + 0.5 * fbm(math.cos(ang) * 3.0 + 11.0,
                                    math.sin(ang) * 3.0 + 11.0, 3)
            crypt = smoothstep(0.55, 0.9, crypt) * \
                smoothstep(0.3, 0.55, rn) * (1.0 - smoothstep(0.88, 1.0, rn))

            # --- gradiente radial base ---
            if rn_w < 0.55:
                base = lerp3(c_inner, c_hot, clamp(rn_w / 0.55, 0, 1))
            elif rn_w < 0.82:
                base = lerp3(c_hot, c_mid, clamp((rn_w - 0.55) / 0.27, 0, 1))
            else:
                base = lerp3(c_mid, c_outer, clamp((rn_w - 0.82) / 0.18, 0, 1))

            # mezclar: bandas oscurecen surcos, fibras aclaran, criptas oscurecen
            mix = 0.72 + bands * 0.38 + fiber * 0.20 - crypt * 0.55
            r = int(clamp(base[0] * mix, 0, 255))
            g = int(clamp(base[1] * mix, 0, 255))
            b = int(clamp(base[2] * mix, 0, 255))

            # grano estelar: puntos brillantes dispersos (materia fina)
            grain = fbm(x * 0.045, y * 0.045, 2)
            if grain > 0.42 and rn < 0.9:
                boost = (grain - 0.42) * 3.0
                r = int(clamp(r + boost * 90, 0, 255))
                g = int(clamp(g + boost * 70, 0, 255))
                b = int(clamp(b + boost * 34, 0, 255))

            # anillo limbal violeta-negro
            if rn > 0.90:
                lm = smoothstep(0.90, 0.99, rn)
                r = int(lerp(r, c_limbal[0], lm))
                g = int(lerp(g, c_limbal[1], lm))
                b = int(lerp(b, c_limbal[2], lm))

            # borde interno ARDIENTE (cerca de la pupila la materia arde)
            if rn < 0.14:
                heat = smoothstep(0.14, 0.0, rn)
                r = int(clamp(r + heat * 60, 0, 255))
                g = int(clamp(g + heat * 46, 0, 255))
                b = int(clamp(b + heat * 20, 0, 255))

            px[x, y] = (r, g, b, 255)

    return img.resize((S, S), Image.LANCZOS)


# ================================================================
#  3. PÁRPADO (idéntico a v2 — ya aprobado)
#  4. ICONO (idéntico a v2)
# ================================================================
def gen_lid():
    S = 512
    SS = 4
    W = S * SS
    cx = W / 2
    img = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    px = img.load()
    outerR = W * 0.485
    for y in range(W):
        for x in range(W):
            dx, dy = x - cx, y - W / 2
            d = math.sqrt(dx * dx + dy * dy)
            if d > outerR or y > W * 0.5:
                continue
            edgeLift = math.pow(abs(dx) / outerR, 1.6) * W * 0.16
            edgeY = W * 0.5 - edgeLift
            if y > edgeY:
                thick = (outerR - abs(dx)) / outerR
                if thick < 0.06:
                    continue
                t = (y - (W * 0.5 - outerR)) / outerR
                if t < 0.35:
                    base = (30, 22, 26)
                elif t < 0.75:
                    base = lerp3((30, 22, 26), (58, 38, 40), (t - 0.35) / 0.4)
                else:
                    base = lerp3((58, 38, 40), (86, 52, 48), (t - 0.75) / 0.25)
                rim = smoothstep(0.78, 0.97, t)
                r = int(base[0] + rim * 120)
                g = int(base[1] + rim * 58)
                b = int(base[2] + rim * 34)
                a = 255
                if t > 0.94:
                    a = int(255 * clamp((1.0 - t) / 0.06, 0.0, 1.0))
                px[x, y] = (r, g, b, a)
    folds = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    fd = ImageDraw.Draw(folds)
    for i in range(7):
        off = W * (0.16 + i * 0.055)
        fd.arc([cx - outerR + off * 0.4, W * 0.5 - outerR + off * 0.55,
                cx + outerR - off * 0.4, W * 0.5 + outerR * 0.2],
               180, 360, fill=(16, 10, 12, 70), width=SS * 2)
    img = Image.alpha_composite(img, folds.filter(ImageFilter.GaussianBlur(SS * 1.5)))
    cracks = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    kd = ImageDraw.Draw(cracks)
    for i in range(9):
        x0 = cx + random.uniform(-outerR * 0.8, outerR * 0.8)
        y0 = W * 0.5 - random.uniform(outerR * 0.55, outerR * 0.9)
        x, y = x0, y0
        dirr = random.uniform(math.pi * 0.35, math.pi * 0.65)
        pts = [(x, y)]
        for s in range(14):
            dirr += random.uniform(-0.35, 0.35)
            x += math.cos(dirr) * SS * 3.2
            y += math.sin(dirr) * SS * 2.4
            pts.append((x, y))
        kd.line(pts, fill=(140, 66, 58, 60), width=SS)
    img = Image.alpha_composite(img, cracks.filter(ImageFilter.GaussianBlur(SS * 0.8)))
    return img.resize((S, S), Image.LANCZOS)


def gen_staff_icon():
    W = 116
    img = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    cx = cy = W / 2
    d.ellipse([cx - W * 0.42, cy - W * 0.42, cx + W * 0.42, cy + W * 0.42],
              fill=(38, 26, 30, 255))
    for i in range(5):
        ang = i * math.tau / 5 + 0.4
        x0 = cx + math.cos(ang) * W * 0.06
        y0 = cy + math.sin(ang) * W * 0.06
        x1 = cx + math.cos(ang) * W * 0.40
        y1 = cy + math.sin(ang) * W * 0.40
        d.line([(x0, y0), (x1, y1)], fill=(96, 20, 28, 140), width=3)
    d.ellipse([cx - W * 0.26, cy - W * 0.20, cx + W * 0.26, cy + W * 0.20],
              fill=(234, 226, 208, 255))
    d.ellipse([cx - W * 0.13, cy - W * 0.13, cx + W * 0.13, cy + W * 0.13],
              fill=(198, 128, 52, 255))
    d.ellipse([cx - W * 0.06, cy - W * 0.06, cx + W * 0.06, cy + W * 0.06],
              fill=(12, 4, 8, 255))
    d.ellipse([cx - W * 0.075, cy - W * 0.075, cx + W * 0.075, cy + W * 0.075],
              outline=(255, 236, 180, 220), width=2)
    d.ellipse([cx - W * 0.17, cy - W * 0.15, cx - W * 0.09, cy - W * 0.09],
              fill=(255, 255, 250, 130))
    return img.resize((29, 29), Image.LANCZOS)


if __name__ == "__main__":
    base = f"{ROOT}/Content"
    gen_sclera().save(f"{base}/Effects/Procedural/EyeSclera.png")
    gen_iris().save(f"{base}/Effects/Procedural/EyeIris.png")
    gen_lid().save(f"{base}/Effects/Procedural/EyeLid.png")
    gen_staff_icon().save(f"{base}/Weapons/Cosmic/VoidEyeStaff.png")
    print("OK — texturas del ojo v2b generadas")
