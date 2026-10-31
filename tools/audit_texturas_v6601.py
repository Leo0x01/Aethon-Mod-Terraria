#!/usr/bin/env python3
"""v6.60.1 — EL AUDITOR DE TEXTURAS: simula la carga del CLIENTE.

La lección del client.log del usuario: IraSerExteriorCeremoniaProjectile
nacía sin textura y tML desactivaba el mod ENTERO («Recurso esperado no
encontrado: Content/Projectiles/Definitivas/IraSerExteriorCeremoniaProjectile»).
El headless NO lo caza (los servers no cargan texturas) — ESTE es el
sustituto: para cada clase de contenido (ModItem/ModProjectile/ModNPC/
ModDust/ModBuff/ModTile/ModWall) resuelve su textura EXACTAMENTE como
tML (override Texture o el path por defecto namespace+clase) y comprueba
que el asset exista en la fuente (.png/.rawimg) o sea vanilla (Terraria/).
"""
import os
import re
import sys

BASE = "/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod"
MODNAME = "AethonMod"

# Las bases con textura obligatoria (todo lo que herede de estas)
BASES = ["ModItem", "ModProjectile", "ModNPC", "ModDust", "ModBuff",
         "ModTile", "ModWall", "ModPylon", "ModMount"]

# ModBiome NO exige textura al cargar (la pide perezosamente para el
# icono del bestiario) — HollowSanctumBiome vive SIN textura desde la
# v6.50 y el cliente lo ha cargado bien 100+ versiones: SOLO aviso.
BASES_AVISO = ["ModBiome"]

DECL = re.compile(
    r"^\s*(?:public|internal|private)?\s*(?:sealed\s+|abstract\s+|static\s+|partial\s+)*"
    r"class\s+(\w+)\s*(?::\s*([^{]+?))?\s*\{",
    re.MULTILINE)

TEX_OVR = re.compile(r"override\s+string\s+Texture\s*=>\s*(.+?);",
                     re.DOTALL)

errores = []
avisos = []
clases_contenido = 0

def clase_body(src, llave_idx):
    """Devuelve el cuerpo de la clase (brace matching desde llave_idx)."""
    prof = 0
    i = llave_idx
    n = len(src)
    while i < n:
        c = src[i]
        if c == '{':
            prof += 1
        elif c == '}':
            prof -= 1
            if prof == 0:
                return src[llave_idx:i + 1]
        elif c == '"':  # salta strings
            i += 1
            while i < n and src[i] != '"':
                if src[i] == '\\':
                    i += 1
                i += 1
        i += 1
    return src[llave_idx:]

def hereda_de(base_list, nombre_base):
    """¿Alguna de las bases listadas es (directa o indirectamente visible) nombre_base?"""
    for b in base_list:
        b = b.strip()
        # la última parte de la herencia (p.ej. "global::X.Y.ModProjectile" → "ModProjectile")
        if b.split('.')[-1] == nombre_base:
            return True
    return False

def existe_asset(ruta_tex):
    """¿El asset existe en la fuente? (ruta_tex sin el prefijo del mod)"""
    if ruta_tex.startswith(MODNAME + "/"):
        ruta_tex = ruta_tex[len(MODNAME) + 1:]
    for ext in (".png", ".rawimg", ".jpg", ".jpeg"):
        if os.path.exists(os.path.join(BASE, ruta_tex + ext)):
            return True
    return False

for root, dirs, files in os.walk(BASE):
    dirs[:] = [d for d in dirs if d not in ("bin", "obj", "research", "tools",
                                            "tests", "_masters", "Investigacion")]
    for fn in files:
        if not fn.endswith(".cs"):
            continue
        ruta = os.path.join(root, fn)
        with open(ruta, "r", encoding="utf-8", errors="replace") as f:
            src = f.read()

        # namespace del archivo (el primero que aparezca)
        mns = re.search(r"namespace\s+([\w.]+)", src)
        ns = mns.group(1) if mns else ""

        for m in DECL.finditer(src):
            clase, herencia = m.group(1), (m.group(2) or "")
            bases = [h.strip() for h in herencia.split(",")] if herencia else []
            es_abstract = "abstract" in m.group(0)
            tocado = False
            solo_aviso = False
            for base_name in BASES:
                if hereda_de(bases, base_name):
                    tocado = True
                    break
            if not tocado:
                for base_name in BASES_AVISO:
                    if hereda_de(bases, base_name):
                        tocado = True
                        solo_aviso = True
                        break
            if not tocado:
                continue
            clases_contenido += 1
            if es_abstract:
                continue   # las abstractas nunca hacen SetupContent

            body = clase_body(src, m.end() - 1)
            mt = TEX_OVR.search(body)
            es_fallo = errores if not solo_aviso else avisos
            prefijo = "FALTA" if not solo_aviso else "AVISO-BIOME"
            if mt:
                expr = " ".join(mt.group(1).split())
                lit = re.findall(r'"([^"]+)"', expr)
                if not lit:
                    avisos.append(f"{ruta} :: {clase} — Texture COMPUTADA ({expr[:60]}…) — revisar a mano")
                    continue
                for tex in lit:
                    if tex.startswith("Terraria/"):
                        pass   # vanilla: siempre existe
                    elif tex.startswith(MODNAME + "/"):
                        if not existe_asset(tex):
                            es_fallo.append(f"{ruta} :: {clase} — Texture '{tex}' NO EXISTE")
                    else:
                        avisos.append(f"{ruta} :: {clase} — Texture rara '{tex}' — revisar")
            else:
                # PATH POR DEFECTO: namespace de la clase + su nombre
                # (tML: la carpeta del namespace, NO la del archivo — pero
                # en la casa SIEMPRE coinciden; se comprueban AMBAS)
                rutas = set()
                if ns:
                    rutas.add(ns.replace(".", "/") + "/" + clase)
                rel = os.path.relpath(ruta, BASE)[:-3].replace(os.sep, "/")
                rel = rel[: rel.rfind("/")] + "/" + clase
                rutas.add(rel)
                if not any(existe_asset(r) for r in rutas):
                    es_fallo.append(
                        f"{ruta} :: {clase} ({', '.join(sorted(rutas))}) — SIN textura (ni override ni archivo) [{prefijo}]")

print(f"clases de contenido escaneadas: {clases_contenido}")
for a in avisos:
    print("AVISO:", a)
if errores:
    print(f"\nERRORES: {len(errores)}")
    for e in errores:
        print("  FALTA:", e)
    sys.exit(1)
print("\nTODAS las texturas de contenido resuelven — el cliente cargará")
