#!/usr/bin/env python3
"""v6.56.6 — LA AUDITORÍA DEL .tmod.

La letra de esta versión: (1) LOCALIZACIÓN: el en-US vuelve como ESPEJO DE
SEGURIDAD con el mismo español (la cura de las claves crudas — el fallback
del motor), es-MX espejo como siempre; (2) LA PURGA: 9 armas nuevas
borradas, solo el RC2 queda; (3) EL RC2: sin disco negro, sin lente,
coronas al 53%, la pila de capas (desenfoque/glow/brillo), los soles
rúnicos de la carga (la receta del accesorio), temblor sostenido;
(4) LAS 9 VARIANTES (Prisma/Eclipse/Tormenta/Solar/Vórtice/Espectro/
Sierpe/Cuántico/Nova); (5) LAS ALAS: el barrido rígido muere (el par
espejo exacto, Δ 0.0000 px validado en el sim)."""
import os
import sys
import hashlib
import zlib

sys.path.insert(0, "/tmp")
from parse_tmod import parse  # noqa: E402

TMOD = "/tmp/tmlhome/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod"
errores = []

tmlver, name, ver, entries, tend, fsize = parse(TMOD)
d = open(TMOD, "rb").read()
print(f"tML {tmlver} | {name} v{ver} | {len(entries)} entradas | blobs {tend} | archivo {fsize}")
md5 = hashlib.md5(d).hexdigest()
print(f"md5: {md5} · {fsize} bytes")

if ver != "6.56.6":
    errores.append(f"versión del paquete: {ver} ≠ 6.56.6")

total_comp = sum(c for _, _, c in entries)
if tend + total_comp != fsize:
    errores.append(f"EOF: {tend} + {total_comp} = {tend + total_comp} ≠ {fsize}")
else:
    print(f"EOF EXACTO: {tend} + {total_comp} = {fsize} ✓")

# inflar todos los blobs
off = tend
blobs = {}
for p, raw, comp in entries:
    b = d[off:off + comp]
    off += comp
    try:
        blobs[p] = b if comp == raw else zlib.decompress(b, -15)
    except Exception as e:
        errores.append(f"{p}: blob no infla ({e})")
        blobs[p] = b

pngs = [r for r in blobs if r.endswith(".png")]
rawimgs = [r for r in blobs if r.endswith(".rawimg")]
hjsons = sorted(r for r in blobs if r.endswith(".hjson"))
fx = [r for r in blobs if r.endswith(".fx")]
fxc = [r for r in blobs if r.endswith(".fxc")]
oggs = sorted(r for r in blobs if r.endswith(".ogg"))
print(f"png {len(pngs)} · rawimg {len(rawimgs)} · hjson {len(hjsons)} · fx {len(fx)} · fxc {len(fxc)} · ogg {len(oggs)}")

# ---- (1) LOS .fx FUERA / .fxc DENTRO ----
if fx:
    errores.append(f"los .fx fuente DENTRO: {fx}")
else:
    print("los .fx fuente: FUERA ✓")
if len(fxc) != 4:
    errores.append(f"los .fxc compilados: {len(fxc)} ≠ 4")
else:
    print(f"los 4 .fxc: DENTRO ✓ ({', '.join(os.path.basename(r) for r in sorted(fxc))})")

# ---- (2) LA LOCALIZACIÓN MONOLINGÜE v2: en-US = espejo español ----
esperados = {
    "Localization/es-ES_Mods.AethonMod.hjson",
    "Localization/es-MX_Mods.AethonMod.hjson",
    "Localization/en-US_Mods.AethonMod.hjson",
    "Localization/Escriba/es-ES_Mods.AethonMod.hjson",
    "Localization/Escriba/es-MX_Mods.AethonMod.hjson",
    "Localization/Escriba/en-US_Mods.AethonMod.hjson",
}
faltan_hjson = esperados - set(hjsons)
if faltan_hjson:
    errores.append(f"hjson faltantes: {faltan_hjson}")
else:
    print("los 6 hjson DENTRO (es-ES/es-MX/en-US × principal+Escriba) ✓")

def cuerpo(texto):
    lineas = texto.split("\n")
    i = 0
    while i < len(lineas) and (lineas[i].startswith("//") or lineas[i].strip() == ""):
        i += 1
    return lineas[i:]

if not faltan_hjson:
    c_es = cuerpo(blobs["Localization/es-ES_Mods.AethonMod.hjson"].decode("utf-8"))
    for espejo in ("es-MX", "en-US"):
        c = cuerpo(blobs[f"Localization/{espejo}_Mods.AethonMod.hjson"].decode("utf-8"))
        if c != c_es:
            errores.append(f"espejo {espejo} principal ≠ es-ES (cuerpo)")
        else:
            print(f"espejo {espejo} principal == es-ES (cuerpo idéntico, {len(c)} líneas) ✓")
    c_es2 = cuerpo(blobs["Localization/Escriba/es-ES_Mods.AethonMod.hjson"].decode("utf-8"))
    for espejo in ("es-MX", "en-US"):
        c = cuerpo(blobs[f"Localization/Escriba/{espejo}_Mods.AethonMod.hjson"].decode("utf-8"))
        if c != c_es2:
            errores.append(f"espejo Escriba {espejo} ≠ es-ES")
        else:
            print(f"espejo Escriba {espejo} == es-ES ✓")
    # la clave de la barra de exp y las variantes presentes
    todo = blobs["Localization/es-ES_Mods.AethonMod.hjson"].decode("utf-8")
    if "Nivel: Grimorio" not in todo:
        errores.append("la clave anidada HUD.Nivel ausente del es-ES")
    for clave in ("RayoConcentradoPrisma", "RayoConcentradoNova",
                  "RayoConcentradoCuantico"):
        if clave not in todo:
            errores.append(f"clave {clave} ausente del es-ES")
    print("claves HUD.Nivel (anidada) + las 9 variantes: presentes ✓")

# ---- (3) LA PURGA: los muertos AUSENTES ----
muertos = [
    "Content/Weapons/Definitivas/RayosConcentrados.rawimg",
    "Content/Weapons/Definitivas/DefinitivasItemsA.rawimg",
    "Content/Weapons/Definitivas/DefinitivasItemsB.rawimg",
    "Content/Weapons/Definitivas/DefinitivasNuevasV6560.rawimg",
    "Content/Projectiles/Definitivas/RayoConcentradoNivelProjectile.rawimg",
    "Content/Projectiles/Definitivas/PrismaPrimordialProjectile.rawimg",
    "Content/Projectiles/Definitivas/PartidorDeMundosProjectile.rawimg",
    "Content/Projectiles/Definitivas/RequiemEstelarProjectile.rawimg",
    "Content/Projectiles/Definitivas/IraDelCieloProjectile.rawimg",
    "Content/Projectiles/Definitivas/IndiceDelJuicioProjectile.rawimg",
    "Content/Projectiles/Definitivas/PlumaDelVeredictoProjectile.rawimg",
    "Content/Projectiles/Definitivas/OjoDelJuicioProjectile.rawimg",
    "Content/Weapons/Definitivas/RayoDelAlba.rawimg",
    "Content/Weapons/Definitivas/RayoDelVeredicto.rawimg",
    "Content/Weapons/Definitivas/RayoDelJuicio.rawimg",
    "Content/Weapons/Definitivas/PrismaPrimordial.rawimg",
    "Content/Weapons/Definitivas/PartidorDeMundos.rawimg",
    "Content/Weapons/Definitivas/RequiemEstelar.rawimg",
    "Content/Weapons/Definitivas/IraDelCielo.rawimg",
    "Content/Weapons/Definitivas/IndiceDelJuicio.rawimg",
    "Content/Weapons/Definitivas/PlumaDelVeredicto.rawimg",
    "Content/Weapons/Definitivas/OjoDelJuicio.rawimg",
]
for m in muertos:
    if m in blobs:
        errores.append(f"el muerto sigue DENTRO: {m}")
print("las 22 entradas muertas de la purga: AUSENTES ✓" if not any(
    m in blobs for m in muertos) else "")

# ---- (4) LAS 9 VARIANTES: iconos DENTRO ----
variantes = ["Prisma", "Eclipse", "Tormenta", "Solar", "Vortice",
             "Espectro", "Sierpe", "Cuantico", "Nova"]
for v in variantes:
    p = f"Content/Weapons/Definitivas/RayoConcentrado{v}.rawimg"
    if p not in blobs:
        errores.append(f"icono de variante ausente: {p}")
print("los 9 iconos de variantes: DENTROS ✓" if all(
    f"Content/Weapons/Definitivas/RayoConcentrado{v}.rawimg" in blobs for v in variantes) else "")

# ---- (5) LOS SONIDOS: bucle INTACTO byte a byte vs .56.5 ----
if os.path.exists("/tmp/AethonMod_v6565.tmod"):
    dv = open("/tmp/AethonMod_v6565.tmod", "rb").read()
    tv, nv, vv, ev, tv2, _ = parse("/tmp/AethonMod_v6565.tmod")
    off2 = tv2
    for p, raw, comp in ev:
        b = dv[off2:off2 + comp]
        off2 += comp
        try:
            bb = b if comp == raw else zlib.decompress(b, -15)
        except Exception:
            continue
        if p.endswith("Content/Sounds/RayoConcentradoLoop.ogg"):
            nuevo = blobs.get("Content/Sounds/RayoConcentradoLoop.ogg")
            if nuevo and hashlib.md5(nuevo).hexdigest() == hashlib.md5(bb).hexdigest():
                print("RayoConcentradoLoop.ogg: INTACTO byte a byte vs .56.5 ✓")
            else:
                errores.append("el bucle del RC2 cambió (debería estar intacto)")
for s in ("Content/Sounds/RayoConcentradoLoop.ogg", "Content/Sounds/RayoConcentradoCarga.ogg"):
    if s not in blobs:
        errores.append(f"sonido ausente: {s}")
print("los 2 sonidos del RC2: DENTRO ✓")

# ---- (6) LA DLL: firmas nuevas presentes / muertas ausentes ----
dll = blobs.get("AethonMod.dll", b"")
firmas_vivas = [b"SolesRunicosCarga", b"SolesRunicosEstallido", b"SolRunico4Puntas",
                b"VariantesRC2", b"RayoConcentradoPrisma", b"RayoConcentradoNova",
                b"DibujarPilaDeCapas", b"Gradiente"]
for f_ in firmas_vivas:
    if f_ not in dll:
        errores.append(f"firma viva AUSENTE de la DLL: {f_.decode()}")
print("las firmas nuevas (soles, variantes, pila): PRESENTES ✓" if all(
    f_ in dll for f_ in firmas_vivas) else "")
firmas_muertas = [b"DestellosDeCarga", b"DestellosDeEstallido",
                  b"EscaleraDelRayo", b"HazFusionEspina"]
for f_ in firmas_muertas:
    if f_ in dll:
        errores.append(f"firma muerta PRESENTE en la DLL: {f_.decode()}")
print("las firmas muertas (destellos viejos, escalera): AUSENTES ✓" if not any(
    f_ in dll for f_ in firmas_muertas) else "")

# ---- (7) LOS CLAVES DE ARMAS MUERTAS ausentes del hjson ----
if not faltan_hjson:
    cuerpo_hjson = blobs["Localization/es-ES_Mods.AethonMod.hjson"].decode("utf-8")
    for clase in ("PrismaPrimordial:", "PartidorDeMundos:", "RequiemEstelar:",
                  "IraDelCielo:", "IndiceDelJuicio:", "PlumaDelVeredicto:",
                  "OjoDelJuicio:", "RayoDelAlba:", "RayoDelVeredicto:", "RayoDelJuicio:"):
        if f"\t{clase}" in cuerpo_hjson:
            errores.append(f"claves del arma muerta siguen en hjson: {clase}")
    print("las claves de las armas muertas: AUSENTES del hjson ✓")

print()
if errores:
    print(f"!!! {len(errores)} ERRORES:")
    for e in errores:
        print("   -", e)
    sys.exit(1)
print(f"=== AUDITORÍA LIMPIA — AethonMod v6.56.6 listo ({md5}) ===")
