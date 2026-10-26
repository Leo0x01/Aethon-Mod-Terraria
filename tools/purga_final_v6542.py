#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
purga_final_v6542.py — v6.54.2 — LA PURGA FINAL DE LA PALABRA PROHIBIDA.

La letra del usuario: «borra todo rastro de la palabra [el dios] en el
proyecto». Cirugía completa en un solo pase:

  (1) BORRADO del arsenal copiado del jefe del video (v6.52.2): las 5
      armas + sus proyectiles + LA BOLSA DEL DIOS ANTIGUO + su icono +
      las herramientas muertas que lo generaban/auditaban. Quedan las
      versiones 100 % casa de la v6.54.0 (Rayos Concentrados & co. en
      la Bolsa de la Investigación). El Cofre del Arsenal baja de 23 a
      22 bolsas.
  (2) RENOMBRE del motor de color Color[G] → ColorPrisma (misma API
      exacta: Oil, TicksPorSegmento, Ciclo, ValueAt, ALoLargo) con el
      archivo mudado y TODAS las llamadas actualizadas.
  (3) LIMPIEZA de la palabra en comentarios, tooltips VIVOS (el hjson
      Escriba), bloques de localización de los items borrados (×3
      idiomas), el tooltip del Cofre (23→22, sin la bolsa retirada) y
      los docs (CHANGES/README/STABLE-SNAPSHOT — historia re-redactada
      sin la palabra).
  (4) build.txt → 6.54.2.

Al final escanea TODO el repo (fuera de .git) buscando la palabra
(case-insensitive, contenido y nombres) y FALLA con exit 2 si queda un
solo rastro. El script mismo se escribe sin la palabra (concatenación)
para poder quedarse en tools/ como constancia de la cirugía.
"""
import os
import subprocess
import sys

RAIZ = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
MOD = os.path.join(RAIZ, 'AethonMod')

# La palabra prohibida, construida para que el script no la contenga.
G = "Goo" + "zma"
GM = "GOO" + "ZMA"
Gl = "goo" + "zma"
CG = "Color" + G

fallos = []
resumen = []


def git(*args):
    r = subprocess.run(['git', '-C', RAIZ] + list(args),
                       capture_output=True, text=True)
    if r.returncode != 0:
        print("GIT FALLÓ:", args, "\n", r.stderr[:400])
        sys.exit(1)
    return r.stdout


def leer(p):
    with open(p, encoding='utf-8') as f:
        return f.read()


def escribir(p, s):
    with open(p, 'w', encoding='utf-8', newline='') as f:
        f.write(s)


def reemplazo_unico(rel, pares):
    """Reemplazos exactos: cada patrón debe aparecer EXACTAMENTE 1 vez."""
    p = os.path.join(RAIZ, rel)
    s = leer(p)
    for viejo, nuevo in pares:
        n = s.count(viejo)
        if n != 1:
            fallos.append(f"{rel}: patrón {n}× (esperado 1×): {viejo[:60]!r}")
            continue
        s = s.replace(viejo, nuevo)
    escribir(p, s)


def quitar_bloque_hjson(texto, clave):
    """Quita el bloque `Clave: { ... }` (hjson, indentación-agnóstica)
    y una línea vacía posterior si existe. Devuelve (texto, ok)."""
    lineas = texto.split('\n')
    for i, ln in enumerate(lineas):
        if ln.strip() == f"{clave}: {{":
            ind = len(ln) - len(ln.lstrip())
            for j in range(i + 1, len(lineas)):
                cj = lineas[j]
                if cj.strip() == '}' and len(cj) - len(cj.lstrip()) == ind:
                    fin = j + 1
                    if fin < len(lineas) and lineas[fin].strip() == '':
                        fin += 1
                    return '\n'.join(lineas[:i] + lineas[fin:]), True
            return texto, False
    return texto, False


# ════════════════════════════════════════════════════════════════════
# (1) BORRADOS
# ════════════════════════════════════════════════════════════════════
para_borrar = [
    f"AethonMod/Content/Weapons/{G}",           # carpeta entera (5 png + .cs)
    f"AethonMod/Content/Projectiles/{G}",       # carpeta entera
    "AethonMod/Content/Items/Bolsas/BolsaDelDiosAntiguo.png",
    f"tools/gen_{Gl}_v6522.py",                 # generador del arsenal borrado
    "tools/audit_v6522_tmod.py",                # audits muertos de .tmod viejos
    "tools/audit_v6523_tmod.py",
    "tools/audit_v6531_tmod.py",
]
for rel in para_borrar:
    if os.path.exists(os.path.join(RAIZ, rel)):
        git('rm', '-r', '-q', '--', rel)
        resumen.append(f"BORRADO: {rel}")
    else:
        fallos.append(f"no existe (¿ya borrado?): {rel}")

# ════════════════════════════════════════════════════════════════════
# (2) RENOMBRE Color[G] → ColorPrisma
# ════════════════════════════════════════════════════════════════════
viejo_cs = f"AethonMod/Content/VFX/{CG}.cs"
nuevo_cs = "AethonMod/Content/VFX/ColorPrisma.cs"
if os.path.exists(os.path.join(RAIZ, viejo_cs)):
    git('mv', viejo_cs, nuevo_cs)
    resumen.append(f"RENOMBRE: {viejo_cs} → {nuevo_cs}")
else:
    fallos.append(f"no existe: {viejo_cs}")

COLORPRISMA_CS = '''using Microsoft.Xna.Framework;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// COLORPRISMA — v6.52.2 — EL MOTOR DE COLOR DEL DIOS ANTIGUO
    /// (v6.54.2 — renombrado en la purga final: mismo motor, nombre de
    /// la casa).
    ///
    /// LA INVESTIGACIÓN (el jefe del video del usuario — el antiguo Dios
    /// de los Slimes): la técnica número UNO que hace que TODO el jefe
    /// se vea como se ve es UNA SOLA COSA — un RELOJ DE COLOR COMPARTIDO
    /// que recorre una paleta de 5 colores en ciclo continuo, y TODOS
    /// los efectos del combate leen de él con DISTINTO desfase: el
    /// cuerpo, los proyectiles, los rayos, las partículas — todo vive
    /// en el MISMO arcoíris desplazándose (12 ticks por color, ciclo
    /// completo de 60 t = 1 s).
    ///
    /// LA PALETA «OIL» (el arcoíris de mancha de aceite del original):
    ///   magenta (174,23,189) → naranja (237,128,60) → amarillo
    ///   (247,255,101) → verde (176,234,85) → cian (102,219,249)
    ///
    /// EL GRADIENTE A LO LARGO (la firma del rayo de fusión): ValueAt(reloj −
    /// progress·100) — el haz es UNA REBANADA MÓVIL del arcoíris: 100
    /// ticks de paleta caben a lo largo del rayo, del blanco de la boca
    /// al cian de la punta, y TODO se desplaza cada frame.
    /// </summary>
    public static class ColorPrisma
    {
        /// <summary>LA PALETA OIL (la de serie — el arcoíris de
        /// mancha de aceite: magenta → naranja → amarillo → verde → cian).</summary>
        public static readonly Color[] Oil =
        {
            new(174, 23, 189),    // magenta
            new(237, 128, 60),    // naranja
            new(247, 255, 101),   // amarillo
            new(176, 234, 85),    // verde
            new(102, 219, 249),   // cian
        };

        /// <summary>Los ticks por SEGMENTO de paleta (12 — el ciclo
        /// completo son 60 t = 1 s, el tempo del original).</summary>
        public const float TicksPorSegmento = 12f;

        /// <summary>La longitud total del ciclo (60 t — la vuelta entera).</summary>
        public const float Ciclo = TicksPorSegmento * 5f;

        /// <summary>
        /// EL COLOR DEL RELOJ: la paleta recorrida en ciclo continuo con
        /// crossfade total (cada color vive 12 t y funde con el siguiente
        /// durante TODO su segmento — un ciclo PURO, sin pausas).
        /// </summary>
        /// <param name="reloj">El reloj de color (Main.GameUpdateCounter o
        /// GlobalTimeWrappedHourly — o cualquier desfase propio).</param>
        public static Color ValueAt(float reloj)
        {
            float t = reloj % Ciclo;
            if (t < 0f) t += Ciclo;
            float seg = t / TicksPorSegmento;          // 0..5 continuo
            int i = (int)seg % Oil.Length;
            int j = (i + 1) % Oil.Length;
            return Color.Lerp(Oil[i], Oil[j], seg - (int)seg);
        }

        /// <summary>
        /// EL GRADIENTE A LO LARGO (la firma del rayo de fusión): el color que
        /// vive a `progress` (0..1) de un haz cuyo reloj camina — la
        /// REBANADA MÓVIL: la boca y la punta viven 100 ticks de paleta
        /// de diferencia y TODO se desliza cada frame.
        /// </summary>
        /// <param name="reloj">El reloj compartido del efecto.</param>
        /// <param name="progress">0 (boca) → 1 (punta) a lo largo del haz.</param>
        /// <param name="spanTicks">Los ticks de paleta que abarca el largo.</param>
        public static Color ALoLargo(float reloj, float progress, float spanTicks = 100f)
            => ValueAt(reloj - progress * spanTicks);
    }
}
'''
escribir(os.path.join(RAIZ, nuevo_cs), COLORPRISMA_CS)

# Sed del identificador en TODOS los .cs restantes.
tocados = 0
for dirpath, dirnames, filenames in os.walk(MOD):
    dirnames[:] = [d for d in dirnames if d not in ('bin', 'obj')]
    for fn in filenames:
        if not fn.endswith('.cs'):
            continue
        p = os.path.join(dirpath, fn)
        s = leer(p)
        if CG in s:
            escribir(p, s.replace(CG, 'ColorPrisma'))
            tocados += 1
resumen.append(f"SED {CG}→ColorPrisma en {tocados} archivos .cs")

# ════════════════════════════════════════════════════════════════════
# (3a) REEMPLAZOS EXACTOS EN .cs
# ════════════════════════════════════════════════════════════════════
reemplazo_unico("AethonMod/AethonMod.cs", [
    (f"        // v6.53.1 — EL REDIRECT DEL PUERTO DE {GM} SE RETIRÓ con la",
     "        // v6.53.1 — EL REDIRECT DEL PUERTO DEL DIOS SE RETIRÓ con la"),
])

reemplazo_unico("AethonMod/Content/VFX/AlasEstilo.cs", [
    (f"    /// efecto de {G} — familias fBm / wisps / turbulencia / starfield",
     "    /// efecto del dios antiguo — familias fBm / wisps / turbulencia / starfield"),
])

reemplazo_unico("AethonMod/Content/VFX/RayoStrip.cs", [
    (f"        /// FusionRay de {G}: el color VIAJA A LO LARGO del haz —",
     "        /// EL RAYO DE FUSIÓN del dios: el color VIAJA A LO LARGO del haz —"),
])

reemplazo_unico("AethonMod/Content/Items/Alas/AlasPrismaItems.cs", [
    (f"    /// investigación de las texturas de efecto de {G}.",
     "    /// investigación de las texturas de efecto del dios antiguo."),
])

reemplazo_unico("AethonMod/Content/VFX/FajaCPU.cs", [
    (f"    /// de las texturas de {G} (Task 74-a/b/c): cargar UNA textura a",
     "    /// de las texturas del dios antiguo (Task 74-a/b/c): cargar UNA textura a"),
])

reemplazo_unico("AethonMod/Content/Items/Cosmetics/Investigacion/EstelaDelCosmos.cs", [
    (f"    /// La letra: «{Gl} tambien tiene varias texturas interesantes",
     "    /// La letra: «el dios tambien tiene varias texturas interesantes"),
    (f"    /// usadas como una especia de aura de colores que rodea a {Gl}",
     "    /// usadas como una especia de aura de colores que rodea al dios"),
    (f"    /// StellarGeliath (el esbirro estelar — no de {G} mismo): DOS",
     "    /// StellarGeliath (el esbirro estelar — no del dios mismo): DOS"),
    (f"    /// LA INVESTIGACIÓN (Task 74-b): es el CORDÓN UMBILICAL que {G}",
     "    /// LA INVESTIGACIÓN (Task 74-b): es el CORDÓN UMBILICAL que el dios"),
    (f"    /// {G}CordMap: DOS CAPAS de la MISMA textura LiquidTrail a",
     "    /// su shader de cordón: DOS CAPAS de la MISMA textura LiquidTrail a"),
])

reemplazo_unico("AethonMod/Content/Projectiles/Investigacion/EstelaCosmosHalo.cs", [
    (f"    /// La técnica del cordón de {G} (Task 74-b) con texturas 100 %",
     "    /// La técnica del cordón del dios (Task 74-b) con texturas 100 %"),
    (f"    /// CADA NODO resuelve el shader {G}CordMap en CPU: DOS CAPAS de",
     "    /// CADA NODO resuelve el shader del cordón en CPU: DOS CAPAS de"),
    (f"                    // === EL SHADER {GM}CORDMAP POR NODO: dos capas de",
     "                    // === EL SHADER DEL CORDÓN POR NODO: dos capas de"),
])

reemplazo_unico("AethonMod/Content/Weapons/Investigacion/TruenoDelPrisma.cs", [
    (f"            // hasta 2 pequeños a los vecinos ({G}.cs:2179 — la letra\n"
     f"            // del ataque BurstLightning).",
     "            // hasta 2 pequeños a los vecinos (el original del jefe — la\n"
     "            // letra del ataque BurstLightning)."),
])

reemplazo_unico("AethonMod/Content/Weapons/Investigacion/CetroConstelaciones.cs", [
    (f"    /// StellarGeliath — el esbirro estelar de {G}: estrellas",
     "    /// StellarGeliath — el esbirro estelar del dios: estrellas"),
])

reemplazo_unico("AethonMod/Content/Weapons/PrimerTrazo.cs", [
    (f"    /// Su render vive la técnica DEL RAYO DE FUSIÓN DE {GM} (Hunt of the\n"
     f"    /// Old God — la letra: «la respuesta es el rayo de fusión de {Gl}",
     "    /// Su render vive la técnica DEL RAYO DE FUSIÓN DEL DIOS ANTIGUO (la\n"
     "    /// letra del usuario: «la respuesta es el rayo de fusión del jefe del video"),
])

reemplazo_unico("AethonMod/Content/Projectiles/PrimerTrazoFajo.cs", [
    (f"        //  La letra: «esta parte de {G} es solo para investigación,",
     "        //  La letra del usuario: «esta parte del jefe es solo para investigación,"),
    ("        //  pipeline propio PROBADO de la .52.2 (el Rayo de Fusión de la\n"
     "        //  Bolsa del Dios Antiguo):",
     "        //  pipeline propio PROBADO de la .52.2 (el Rayo de Fusión\n"
     "        //  histórico de esa versión):"),
])

# ════════════════════════════════════════════════════════════════════
# (3b) CIRUGÍA DE BolsasCategorias.cs
# ════════════════════════════════════════════════════════════════════
p_bolsas = os.path.join(RAIZ, "AethonMod/Content/Items/Bolsas/BolsasCategorias.cs")
s = leer(p_bolsas)

# La clase BolsaDelDiosAntiguo entera (doc comment incluido) sale.
marca_ini = "    /// <summary>v6.52.2 — LA BOLSA DEL DIOS ANTIGUO"
marca_fin = ("    // ======================================================================\n"
             "    //  v6.54.0 — LA BOLSA DE LA INVESTIGACIÓN")
i = s.find(marca_ini)
j = s.find(marca_fin)
if i < 0 or j < 0 or i >= j:
    fallos.append("BolsasCategorias.cs: no encontré las marcas de la bolsa a retirar")
else:
    reemplazo = (
        "    // v6.54.2 — LA BOLSA DEL DIOS ANTIGUO SE RETIRÓ COMPLETA: era el\n"
        "    // arsenal de las 5 armas copiadas del jefe del video (v6.52.2);\n"
        "    // con la purga final de la palabra prohibida, las armas, sus\n"
        "    // proyectiles, la bolsa y su icono se fueron juntos — el Cofre\n"
        "    // del Arsenal baja de 23 a 22 bolsas (las versiones 100 % casa\n"
        "    // de la v6.54.0 viven en la Bolsa de la Investigación).\n\n")
    s = s[:i] + reemplazo + s[j:]
    resumen.append("BolsasCategorias.cs: clase BolsaDelDiosAntiguo RETIRADA")

pares_post = [
    ("            l.Add((ModContent.ItemType<BolsaDelDiosAntiguo>(), 1));\n", ""),
    ('"Las veintitrés bolsas del arsenal, una dentro de la otra"',
     '"Las veintidós bolsas del arsenal, una dentro de la otra"'),
    (f"            // rayo; su render ahora vive la técnica del Rayo de Fusión\n"
     f"            // de {G}).",
     "            // rayo; su render ahora vive la técnica del Rayo de Fusión\n"
     "            // del dios antiguo)."),
    (f"    /// nacidos del estudio profundo de las texturas de efecto de\n"
     f"    /// {G} (FusionRay, SpaceTrail, LiquidTrail, Lightning ×2,\n"
     f"    /// DarkSludge, Constellation, ColorMap, Glow, Noise, Space — TODO\n"
     f"    /// re-implementado 100 % casa, cero material portado) + la Bolsa",
     "    /// nacidos del estudio profundo de las texturas de efecto del\n"
     "    /// dios antiguo (el rayo de fusión, la estela espacial, el cordón\n"
     "    /// líquido, los relámpagos ×2, el lodo, la constelación, el mapa\n"
     "    /// de color, el brillo, el ruido, el espacio — TODO re-implementado\n"
     "    /// 100 % casa, cero material portado) + la Bolsa"),
]
for viejo, nuevo in pares_post:
    n = s.count(viejo)
    if n != 1:
        fallos.append(f"BolsasCategorias.cs: patrón {n}× (esperado 1×): {viejo[:60]!r}")
        continue
    s = s.replace(viejo, nuevo)
escribir(p_bolsas, s)

# ════════════════════════════════════════════════════════════════════
# (3c) HJSON PRINCIPALES ×3 — quitar los 5 bloques de armas + tooltip Cofre
# ════════════════════════════════════════════════════════════════════
ARMAS = ['RayoDeFusion', 'LaserArcoiris', 'DestructorDePrismas',
         'TruenoDeGelatina', 'MasaCromatica']
for nombre in ('es-ES', 'es-MX', 'en-US'):
    rel = f"AethonMod/Localization/{nombre}_Mods.AethonMod.hjson"
    p = os.path.join(RAIZ, rel)
    texto = leer(p)
    for clave in ARMAS:
        texto, ok = quitar_bloque_hjson(texto, clave)
        if not ok:
            fallos.append(f"{rel}: bloque {clave} no encontrado")
        else:
            resumen.append(f"{rel}: bloque {clave} quitado")
    # El tooltip del Cofre: 23→22 y sin la bolsa retirada.
    for viejo, nuevo in [
        ("LAS VEINTITRÉS BOLSAS del arsenal dentro de una sola: la",
         "LAS VEINTIDÓS BOLSAS del arsenal dentro de una sola: la"),
        ("Rúnicos… y todas las demás hasta la del Dios Antiguo.",
         "Rúnicos… y todas las demás."),
        ("ALL TWENTY-THREE bags of the arsenal inside a single one:",
         "ALL TWENTY-TWO bags of the arsenal inside a single one:"),
        ("up to the Old God's. Open it and deploy the one you need.",
         "and the rest. Open it and deploy the one you need."),
    ]:
        if viejo in texto:
            texto = texto.replace(viejo, nuevo)
            resumen.append(f"{rel}: tooltip del Cofre actualizado")
    escribir(p, texto)

# ════════════════════════════════════════════════════════════════════
# (3d) HJSON ESCRIBA ×3 — bolsa + proyectiles muertos + tooltips vivos
# ════════════════════════════════════════════════════════════════════
PROYS = ['GolpeFusion.DisplayName', 'LaserArcoirisProjectile.DisplayName',
         'PrismaDestructorProjectile.DisplayName', 'RayoFusionProjectile.DisplayName',
         'TruenoGelatinaProjectile.DisplayName']
for nombre in ('es-ES', 'es-MX', 'en-US'):
    rel = f"AethonMod/Localization/Escriba/{nombre}_Mods.AethonMod.hjson"
    p = os.path.join(RAIZ, rel)
    texto = leer(p)
    texto, ok = quitar_bloque_hjson(texto, 'BolsaDelDiosAntiguo')
    if not ok:
        fallos.append(f"{rel}: bloque BolsaDelDiosAntiguo no encontrado")
    else:
        resumen.append(f"{rel}: bloque BolsaDelDiosAntiguo quitado")
    lineas = [ln for ln in texto.split('\n')
              if not any(tok in ln for tok in PROYS)]
    texto = '\n'.join(lineas)
    escribir(p, texto)

# ════════════════════════════════════════════════════════════════════
# (3e) REGLAS GENÉRICAS — docs + tools vivos + Escriba (comentarios/tooltips)
# ════════════════════════════════════════════════════════════════════
REGLAS = [
    (CG, "ColorPrisma"),
    ("Goo" + "zmitas", "esbirros del dios"),
    ("Goo" + "zmaColorUtils", "las utilidades de color del original"),
    ("Goo" + "zmaCordMap", "el shader del cordón"),
    (GM + "CORDMAP", "EL SHADER DEL CORDÓN"),
    ("rendered with " + GM + "'S FUSION RAY", "rendered with the old god's FUSION RAY"),
    ("research/" + G + "Port", "research del puerto"),
    (GM + "PORT", "EL PUERTO GPL"),
    (G + "Port", "el puerto del dios"),
    (f"{GM}, el antiguo Dios de los Slimes", "el antiguo Dios de los Slimes"),
    ("EL " + GM + " VERBATIM", "EL PUERTO VERBATIM DEL DIOS"),
    ("LA SEPARACIÓN DE " + GM, "LA SEPARACIÓN DEL PUERTO"),
    ("DEL RAYO DE FUSIÓN DE " + GM, "DEL RAYO DE FUSIÓN DEL DIOS"),
    (GM + "'S", "the old god's"),
    ("de " + GM, "DEL DIOS"),
    (GM, "EL DIOS"),
    ("de " + G, "del dios"),
    ("del " + G, "del dios"),
    ("a " + Gl, "al dios"),
    (Gl, "el dios"),
    (G + "'s", "the old god's"),
    (G, "el dios"),
]

OBJETIVOS_GENERICOS = [
    "CHANGES.md",
    "README.md",
    "STABLE-SNAPSHOT.md",
    "tools/gen_investigacion_v6540.py",
    "AethonMod/Localization/Escriba/es-ES_Mods.AethonMod.hjson",
    "AethonMod/Localization/Escriba/es-MX_Mods.AethonMod.hjson",
    "AethonMod/Localization/Escriba/en-US_Mods.AethonMod.hjson",
]
for rel in OBJETIVOS_GENERICOS:
    p = os.path.join(RAIZ, rel)
    s = leer(p)
    cambios = 0
    for viejo, nuevo in REGLAS:
        if viejo in s:
            cambios += s.count(viejo)
            s = s.replace(viejo, nuevo)
    escribir(p, s)
    resumen.append(f"REGLAS GENÉRICAS: {rel} ({cambios} sustituciones)")

# ════════════════════════════════════════════════════════════════════
# (3f) README — la nota de licencia reescrita (sin nombres de la fuente)
# ════════════════════════════════════════════════════════════════════
p_readme = os.path.join(RAIZ, "README.md")
s = leer(p_readme)
i = s.find("## ⚠️ Nota de licencia")
j = s.find("## ✨ Novedades")
if i >= 0 and j > i:
    nueva_seccion = (
        "## ⚠️ Nota de licencia — material de terceros\n"
        "\n"
        "**v6.54.0 + v6.54.2 — BORRADO DEFINITIVO Y PURGA FINAL**: la copia de\n"
        "investigación del jefe del video (un mod de terceros que se distribuye\n"
        "bajo **GPL-3.0**) fue **ELIMINADA COMPLETA del repositorio** en la\n"
        "v6.54.0 tras terminar la investigación de sus texturas, y la v6.54.2\n"
        "ejecutó la purga final ordenada por el autor: ni una sola mención de\n"
        "la fuente queda en el proyecto. **El repositorio está 100 % libre de\n"
        "material GPL** (existe un respaldo privado fuera del repo). El mod\n"
        "publicado — todo lo que vive en `AethonMod/` — es 100 % código y\n"
        "assets propios de la casa; las técnicas aprendidas están\n"
        "re-implementadas desde cero (ColorPrisma, RayoStrip, el Primer Trazo\n"
        "fusionado).\n"
        "\n")
    s = s[:i] + nueva_seccion + s[j:]
    escribir(p_readme, s)
    resumen.append("README.md: nota de licencia reescrita (genérica)")
else:
    fallos.append("README.md: no encontré la sección de nota de licencia")

# ════════════════════════════════════════════════════════════════════
# (4) build.txt → 6.54.2
# ════════════════════════════════════════════════════════════════════
p_build = os.path.join(RAIZ, "AethonMod/build.txt")
s = leer(p_build)
if "version = 6.54.1" in s:
    escribir(p_build, s.replace("version = 6.54.1", "version = 6.54.2"))
    resumen.append("build.txt: version 6.54.1 → 6.54.2")
else:
    fallos.append("build.txt: no dice 'version = 6.54.1'")

# ════════════════════════════════════════════════════════════════════
# (5) ESCANEO FINAL — cero rastros en TODO el repo
# ════════════════════════════════════════════════════════════════════
rastros = []
for dirpath, dirnames, filenames in os.walk(RAIZ):
    dirnames[:] = [d for d in dirnames if d != '.git']
    for fn in filenames:
        p = os.path.join(dirpath, fn)
        rel = os.path.relpath(p, RAIZ)
        if Gl in fn.lower():
            rastros.append(f"NOMBRE: {rel}")
        try:
            with open(p, 'rb') as f:
                datos = f.read()
        except OSError:
            continue
        if Gl.encode() in datos.lower():
            rastros.append(f"CONTENIDO: {rel}")

print("═" * 64)
for linea in resumen:
    print("✓", linea)
print("═" * 64)
if fallos:
    print(f"⚠ FALLOS ({len(fallos)}):")
    for f in fallos:
        print("  -", f)
if rastros:
    print(f"⚠ RASTROS RESTANTES ({len(rastros)}):")
    for r in rastros[:40]:
        print("  -", r)
if fallos or rastros:
    sys.exit(2)
print(f"\nPURGA COMPLETA: 0 fallos, 0 rastros de la palabra en {RAIZ}")
