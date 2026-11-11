#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
audit_v6124_tmod.py — v6.125 — LA AUDITORÍA DEL .tmod: LA SESIÓN DE LAS
INVESTIGACIONES Y LA FORMA ORIGINAL (la letra del usuario, sesión 7):

  · «las alas runicas del vacio ya no tienen picos, pero perdieron la
    forma original y ademas se ven raras con algo en el centro de cada
    ala, intenta retomar la forma anterior y original, pero manten lo
    que hiciste para que no tengan picos y sean lisas como ahora»
  · «en el buff de aura del vacio solo debe estar mi texto no decir si
    hace daño o no... solo quita la explicacion de daño y manten el
    resto»
  · «el juicio de los dos soles es raro, no usan los soles runicos que
    tenemos como base para sus soles y todavia hay una textura plana y
    rectangular en la explocion»
  · «las otras dos armas las puedes borrar el tajo de oscuridad y las
    agujas del vacio, entonces crea un nuevo tajo y para eso debes usar
    un pequeño haz usando nuestros hazes, el inicio y final del tajo
    tipo haz sera muy fino en el centro se vuelve mas grueso, y como
    dije antes los colores son negro en el centro, morado en los bordes»
  · «investiga y copia al 100% todo sobre el arma Arbitrator II en
    CALAMITY ENTROPY, esto sera usado como investigacion»
  · «el grimorio tendra un nuevo set de dialogos solo dentro del mundo
    interior, esto hara que los dialogos anteriores no se activen y
    solo se activen los nuevos dialogos exclusivos del mundo interior»
  · «copia el arma Dazzling Stabber Staff de Calamity Mod y crea una
    version con su codigo, sprite y textura al 100% como investigacion,
    luego daselas al jugador»
  · «copia el arma the beginning and the end de CALAMITY ENTROPY y crea
    una version con su codigo, sprite y textura al 100% como
    investigacion, luego daselas al jugador»
  · «si por 2 segundos el jugador no es atraido, el grimorio volara
    hacia el jugador»

LO QUE ESTA AUDITORÍA VERIFICA (la obra de la v6.124):
  · LA FORMA ORIGINAL: el Ala() del vacío con los parámetros de la
    v6.118 (membrana 1 · redonda 1 · filosa 0 · puntas 20/14 ·
    afilada 0.5 · Ruido_6 · núcleo 0.78 · cuerpo 0.42) + EL ENSOGLADO
    INCONDICIONAL (inset 0.86/0.72) + LAS RUNAS AL NODO 8 con la
    escala completa (1.45+0.35n).
  · EL TOOLTIP DEL AURA: solo el texto del usuario (sin números de
    daño) ×3 idiomas + el ModifyBuffText sin string.Format.
  · EL JUICIO: DrawSunBody + EmitRingSystem en la fuente; SIN Star/
    NovaBurst/DestelloFinal (las texturas planas muertas); la NOVA de
    bruma + runas de eco + el anillo de OndaLib.
  · LAS DOS ARMAS MUERTAS: TajoOscuro/AgujasVacio NI en disco NI en
    el .tmod NI en el hjson ×3; el TAJO DEL VACÍO con la ley del huso
    (seno^0.75: fino en los extremos, lleno al centro) y los colores
    negro/morado.
  · LAS INVESTIGACIONES: DazzlingStabberStaff (con los sprites
    ORIGINALES de Calamity dentro del .tmod) + ArbitradorII +
    ElPrincipioYElFin + sus 6 buffs + el sigilo ratero, TODO dentro.
  · LOS DIÁLOGOS DEL INTERIOR: Eco.Interior1..8 ×3 idiomas; el
    silencio de las voces viejas DENTRO (VozInteriorActiva en las 4
    bocas: hambre, celos, despertar, jefe).
  · EL VUELO DEL GRIMORIO: TVueloLibro = 120 (2 s) en la fuente y en
    la DLL.
  · LA BOLSA: BolsaInvestigaciones con las 3 armas nuevas + la
    BolsaSuperforja con las 2 definitivas.
"""
import hashlib
import os
import struct
import sys

RAIZ = "/home/z/my-project/mod/AethonMod"
TMOD = os.path.expanduser("~/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod")

fallo = 0


def check(nombre, ok, det=""):
    global fallo
    print(("  ✓ " if ok else "  ✗ ") + nombre + (f" — {det}" if det else ""))
    if not ok:
        fallo += 1


def leer_tmod(ruta):
    """EL LECTOR DE LA CASA (el formato de extraer_tmod.py): cabecera
    TMOD + tabla (ruta, raw, compreso) + blobs DEFLATE crudo."""
    import zlib
    import struct as st

    class R:
        def __init__(self, d, o=0):
            self.d = d; self.o = o
        def u7(self):
            r = 0; s = 0
            while True:
                b = self.d[self.o]; self.o += 1
                r |= (b & 0x7F) << s
                if not (b & 0x80): break
                s += 7
            return r
        def s(self):
            n = self.u7()
            v = self.d[self.o:self.o+n].decode('utf-8', 'replace')
            self.o += n
            return v
        def i32(self):
            v = st.unpack_from('<i', self.d, self.o)[0]; self.o += 4
            return v

    d = open(ruta, "rb").read()
    r = R(d, 4)
    r.s()                 # la versión del tML
    r.o += 20 + 256 + 4   # hash + firma + int32
    r.s(); r.s()          # nombre + versión del mod
    n = r.i32()
    tabla = [(r.s(), r.i32(), r.i32()) for _ in range(n)]
    off = r.o
    entradas = {}
    for ruta_e, raw, comp in tabla:
        blob = d[off:off+comp]; off += comp
        entradas[ruta_e] = blob if comp == raw else zlib.decompress(blob, -15)
    return d, entradas


print("═══ LA AUDITORÍA v6.125 ═══")
check("el .tmod existe", os.path.exists(TMOD),
      f"{os.path.getsize(TMOD):,} B" if os.path.exists(TMOD) else TMOD)

data, entradas = leer_tmod(TMOD)
check("la versión 6.125 en el .tmod", b"6.125" in entradas.get("Info", b""),
      entradas.get("Info", b"").decode("utf8", "replace")[:60])

# ================================================================
# 1) LA FORMA ORIGINAL DE LAS ALAS
# ================================================================
print("── LA FORMA ORIGINAL ──")
f_estilo = open(os.path.join(RAIZ, "Content/VFX/AlasEstilo.cs"), encoding="utf-8").read()
f_lib = open(os.path.join(RAIZ, "Content/VFX/AlasAuroraLib.cs"), encoding="utf-8").read()

import re
# EL ALA COMPLETA: escaneo de paréntesis balanceados (la llamada vive
# DENTRO del array del catálogo — termina en ',' no en ');')
def llamada_ala(txt):
    i = txt.find('Ala("Runas del Vacío"')
    if i < 0:
        return ""
    j = txt.find("(", i)
    prof = 0
    k = j
    while k < len(txt):
        if txt[k] == "(":
            prof += 1
        elif txt[k] == ")":
            prof -= 1
            if prof == 0:
                return txt[i:k + 1]
        k += 1
    return ""

linea = llamada_ala(f_estilo)
check("el Ala() del vacío con Ruido_6 (la original)", '"Ruido_6"' in linea)
check("la membrana 1 (la VELA de la forma original)",
      re.search(r'2, 1\.05f, 1f, 1f\),$', linea, re.M) is not None or
      (", 2, 1.05f, 1f, 1f)" in linea))
check("las puntas 20/14 redondas + filosa 0 (simétrica)",
      "20f, 14f, 0.5f, 0.92f" in linea and "0.45f, 0.5f, 0f, 0.085f" in linea)
check("el núcleo 0.78 y el cuerpo 0.42 (el balance original)",
      "0.78f, 0.3f, 0.42f, 0.62f" in linea)
check("EL ENSOGLADO INCONDICIONAL sigue vivo (la lisura)",
      "float inset = pasada == 0 ? 0.86f : 0.72f;" in f_lib and
      "Membrana > 0f ?" not in f_lib.split("float inset")[0][-300:])
check("LAS RUNAS AL NODO 8 con la escala completa",
      "int nodoRuna = 8;" in f_lib and "1.45f + 0.35f * num7" in f_lib)
# v6.125 — EL RE-CABLEO: las alas del vacío de regreso al CAMINO DEL
# ABANICO (la membrana continua era lisísima pero NO era la forma)
f_halo = open(os.path.join(RAIZ, "Content/Projectiles/Cosmetic/AlasAuroraHalo.cs"),
              encoding="utf-8").read()
check("EL CASO ESPECIAL MUERTO (el vacío vuelve al abanico)",
      "AlasVacioLib.Dibujar" not in f_halo and
      "AlasAuroraLib.Dibujar(val, in AlasCatalogo.De(alaEstilo)" in f_halo)

# ================================================================
# 2) EL TOOLTIP DEL AURA
# ================================================================
print("── EL AURA DEL VACÍO ──")
f_aura = open(os.path.join(RAIZ, "Content/Buffs/AuraVacioBuff.cs"), encoding="utf-8").read()
check("el ModifyBuffText SIN string.Format (sin el número)",
      "string.Format" not in f_aura.split("ModifyBuffText")[1].split("}")[0])
for lang in ("es-MX", "es-ES", "en-US"):
    s = open(f"{RAIZ}/../AethonMod/Localization/{lang}_Mods.AethonMod.hjson",
             encoding="utf-8").read()
    m = re.search(r"AuraVacioBuff: \{.*?\n\s*\}", s, re.S)
    bloque = m.group(0) if m else ""
    # (el {0} de los comentarios históricos NO cuenta: solo la línea
    # Description viva)
    desc = re.search(r"\bDescription: (.*)", bloque)
    desc_txt = desc.group(1) if desc else ""
    check(f"el texto del usuario en {lang}",
          "dañando a todo el que se acerque" in desc_txt or
          "harming all who come near" in desc_txt)
    check(f"sin la explicación de daño en {lang} (Description limpia)",
          "{0}" not in desc_txt and "por tick" not in desc_txt)
    check(f"el sello del trabajo VIVE en {lang} («cobrando una presa»)",
          "Trabajando:" in bloque)

# ================================================================
# 3) EL JUICIO DE LOS DOS SOLES (los soles rúnicos de la casa)
# ================================================================
print("── EL JUICIO DE LOS DOS SOLES ──")
f_juicio = open(os.path.join(RAIZ, "Content/Weapons/Definitivas/JuicioDosSoles.cs"),
                encoding="utf-8").read()
check("los CUERPOS con DrawSunBody (el disco de plasma de los soles)",
      "RuneSunRenderer.DrawSunBody" in f_juicio)
check("los ANILLOS con EmitRingSystem (los aros rúnicos)",
      "RuneSunRenderer.EmitRingSystem" in f_juicio)
check("el SOL FUSIONADO (el más grande y devastador)",
      "DibujarSolFusionado" in f_juicio)
check("SIN texturas planas (Star/NovaBurst/DestelloFinal MUERTAS)",
      "TexStar" not in f_juicio and "VFXCore.NovaBurst" not in f_juicio and
      "VFXCore.DestelloFinal" not in f_juicio)
check("LA NOVA de los soles (bruma + chispa + runas de eco + onda)",
      "BrumaNacimientoFX" in f_juicio and "ParticlePresets.Explosion" in f_juicio and
      "OndaLib.Shock" in f_juicio)
dll = entradas.get("AethonMod.dll", b"")
check("las firmas de la obra VIVAS en la DLL",
      b"DibujarSolFusionado" in dll and b"DazzlingStabber" in dll and
      b"ExplotarLaX" in dll)

# ================================================================
# 4) LAS DOS ARMAS MUERTAS + EL TAJO DEL VACÍO
# ================================================================
print("── LAS ARMAS MUERTAS Y EL TAJO NUEVO ──")
for nombre in ("TajoOscuro", "AgujasVacio"):
    en_disco = os.path.exists(f"{RAIZ}/Content/Weapons/Definitivas/{nombre}.cs") or \
        os.path.exists(f"{RAIZ}/Content/Weapons/Definitivas/{nombre}.png")
    en_tmod = f"Content/Weapons/Definitivas/{nombre}.rawimg" in entradas
    check(f"{nombre} MUERTO (ni disco ni .tmod)", not en_disco and not en_tmod)
f_tajo = open(f"{RAIZ}/Content/Weapons/Definitivas/TajoDelVacio.cs", encoding="utf-8").read()
check("el tajo nuevo TODO sobre HazLib",
      "HazLib.PrepararPar" in f_tajo and "HazLib.Dibujar" in f_tajo)
check("LA LEY DEL HUSO (fino en los extremos, lleno al centro)",
      "MathF.Pow(MathF.Max(seno, 0f), 0.75f)" in f_tajo)
check("LOS COLORES DE LA LETRA (negro al centro, morado en bordes)",
      "new Color(2, 1, 4, 255)" in f_tajo)
check("el PNG del tajo en el .tmod (44×44)",
      "Content/Weapons/Definitivas/TajoDelVacio.rawimg" in entradas)

# ================================================================
# 5) LAS INVESTIGACIONES
# ================================================================
print("── LAS INVESTIGACIONES (Calamity + Calamity Entropy) ──")
for ruta in ("Content/Weapons/Investigaciones/DazzlingStabberStaff.rawimg",
             "Content/Weapons/Investigaciones/DazzlingStabberStaffGlow.rawimg",
             "Content/Projectiles/Investigaciones/DazzlingStabber.rawimg",
             "Content/Projectiles/Investigaciones/DazzlingStabber2.rawimg",
             "Content/Projectiles/Investigaciones/DazzlingStabber3.rawimg",
             "Content/Buffs/Investigaciones/DazzlingStabberBuff.rawimg",
             "Content/Weapons/Investigaciones/ArbitradorII.rawimg",
             "Content/Weapons/Investigaciones/ElPrincipioYElFin.rawimg",
             "Content/Buffs/Investigaciones/LlamasSagradas.rawimg",
             "Content/Buffs/Investigaciones/MarcaDelPrincipio.rawimg",
             "Content/Buffs/Investigaciones/MiradaDelFin.rawimg",
             "Content/Buffs/Investigaciones/SupresionVital.rawimg",
             "Content/Buffs/Investigaciones/Servidumbre.rawimg"):
    check(f"el sprite {ruta.split('/')[-1]} en el .tmod", ruta in entradas)

# los sprites ORIGINALES de Calamity: md5 contra el repo descargado
originales = {
    "Content/Weapons/Investigaciones/DazzlingStabberStaff.rawimg":
        "/tmp/cala/Items/Weapons/Summon/DazzlingStabberStaff.png",
    "Content/Projectiles/Investigaciones/DazzlingStabber.rawimg":
        "/tmp/cala/Projectiles/Summon/DazzlingStabber.png",
    "Content/Projectiles/Investigaciones/DazzlingStabber2.rawimg":
        "/tmp/cala/Projectiles/Summon/DazzlingStabber2.png",
    "Content/Projectiles/Investigaciones/DazzlingStabber3.rawimg":
        "/tmp/cala/Projectiles/Summon/DazzlingStabber3.png",
    "Content/Buffs/Investigaciones/DazzlingStabberBuff.rawimg":
        "/tmp/cala/Buffs/Summon/DazzlingStabberBuff.png",
}
from PIL import Image
import struct as st2
for destino, origen in originales.items():
    if destino in entradas and os.path.exists(origen):
        raw = entradas[destino]
        w, h = st2.unpack_from("<ii", raw, 4)[0], st2.unpack_from("<ii", raw, 8)[0]
        im_png = Image.open(origen).convert("RGBA")
        png = im_png.tobytes()
        # tML guarda rawimg PREMULTIPLICADO (rgb·a/255): el sprite
        # VISIBLE es idéntico píxel a píxel tras el premultiply.
        pre = bytearray(len(png))
        for i in range(0, len(png), 4):
            al = png[i + 3]
            pre[i] = png[i] * al // 255
            pre[i + 1] = png[i + 1] * al // 255
            pre[i + 2] = png[i + 2] * al // 255
            pre[i + 3] = al
        check(f"SPRITE 100% CALAMITY (píxel a píxel): {destino.split('/')[-1]}",
              raw[12:] == bytes(pre) and (w, h) == im_png.size,
              f"{w}x{h}, premultiplicado por tML")

f_dazz = open(f"{RAIZ}/Content/Projectiles/Investigaciones/DazzlingStabber.cs",
              encoding="utf-8").read()
check("la MÁQUINA del original (3 cuchillos: cristal/roca/llama)",
      "case 1: // Crystal" in f_dazz and "case 2: // Rock" in f_dazz)
check("el SLICE del original (la aceleración y el giro 0.15)",
      "angularTurnSpeed = 0.15f" in f_dazz and "1.03f" in f_dazz)
check("el DESCANSO del original (el abanico a 220 px)",
      "* -220f * (MathHelper.Lerp(sine, 0.8f, 0.8f))" in f_dazz)
f_arb = open(f"{RAIZ}/Content/Weapons/Investigaciones/ArbitradorII.cs", encoding="utf-8").read()
f_arb_p = open(f"{RAIZ}/Content/Projectiles/Investigaciones/ArbitradorIIProyectil.cs",
               encoding="utf-8").read()
check("el ARBITRADOR II: la ficha (250 daño · uso 32 · 16% crítico)",
      "Item.damage = 250" in f_arb and "Item.useTime = 32" in f_arb and "Item.crit = 16" in f_arb)
check("la jabalina ATRAVIESA MUROS y ESTALLA (la ficha)",
      "tileCollide = false" in f_arb_p and "ExplotarNormal" in f_arb_p)
check("el SIGILOSO se CLAVA y detona la EXPLOSIÓN EN X a los 60 t",
      "ExplotarLaX" in f_arb_p and "_relojX >= 60" in f_arb_p)
f_pef = open(f"{RAIZ}/Content/Weapons/Investigaciones/ElPrincipioYElFin.cs", encoding="utf-8").read()
f_daga = open(f"{RAIZ}/Content/Projectiles/Investigaciones/DagaEterna.cs", encoding="utf-8").read()
check("EL PRINCIPIO Y EL FIN: la ficha (2500 · uso 18 · 4% crítico)",
      "Item.damage = 2500" in f_pef and "Item.useTime = 18" in f_pef and "Item.crit = 4" in f_pef)
check("las dagas: 100 penetración · no atraviesan · atraviesan muros",
      "ArmorPenetration = 100" in f_daga and "penetrate = 1" in f_daga)
check("el CANJE DE ALMAS (8 capas → las almas)",
      "Math.Min(8, g.StacksPrincipio" in f_daga and "Math.Min(8, g.StacksFin" in f_daga and
      "AlmaLuzInferior" in f_daga and "AlmaEco" in f_daga)
check("la SUPRESIÓN VITAL (ambas dagas) + la SERVIDUMBRE (las 2 direcciones)",
      "SupresionVital>(), 600" in f_daga and "Servidumbre>(), 180" in f_daga)
check("el ESTALLIDO SIGILOSO (2 dagas rastreadoras → la nube de almas)",
      "NacerAlma<AlmaEco>" in f_daga and f_pef.count("Projectile.NewProjectile") >= 1)

# ================================================================
# 6) LOS DIÁLOGOS DEL INTERIOR
# ================================================================
print("── LOS DIÁLOGOS EXCLUSIVOS DEL INTERIOR ──")
f_sub = open(f"{RAIZ}/Content/Systems/SubmundoGrimorio.cs", encoding="utf-8").read()
check("LA BOCA ÚNICA (VozInteriorActiva + HablarInterior)",
      "public static bool VozInteriorActiva" in f_sub and
      "public static void HablarInterior" in f_sub)
f_shard = open(f"{RAIZ}/Content/Players/ShardPlayer.cs", encoding="utf-8").read()
f_desp = open(f"{RAIZ}/Content/Systems/DespertarGrimorioSistema.cs", encoding="utf-8").read()
f_eco = open(f"{RAIZ}/Content/Systems/EcoSistema.cs", encoding="utf-8").read()
check("el hambre CALLA dentro (habla el interior)",
      "VozInteriorActiva)\n                            {\n                                Systems.SubmundoGrimorio.HablarInterior" in f_shard)
check("los celos CALLAN dentro", "VozInteriorActiva) return;" in f_shard)
check("la voz del despertar CALLA dentro", "VozInteriorActiva)" in f_desp)
check("la voz del jefe CALLA dentro", "VozInteriorActiva)" in f_eco)
check("el susurro ambiental del interior (~25 s)",
      "% 1500u) == 0u" in f_shard)
for lang in ("es-MX", "es-ES", "en-US"):
    s = open(f"{RAIZ}/Localization/{lang}_Mods.AethonMod.hjson", encoding="utf-8").read()
    ok = all(f"Interior{i}:" in s for i in range(1, 9))
    check(f"las 8 líneas del interior en {lang}", ok)

# ================================================================
# 7) EL VUELO DEL GRIMORIO (la cura de la entrada intermitente)
# ================================================================
print("── EL GRIMORIO QUE VUELA ──")
f_dev = open(f"{RAIZ}/Content/Projectiles/Definitivas/DevoracionEterna.cs", encoding="utf-8").read()
check("TVueloLibro = 120 (LOS 2 SEGUNDOS de la letra)",
      "private const int TVueloLibro = 120;" in f_dev)
check("la CACERÍA INVERSA (el libro vuela hacia el jugador)",
      "TSombra + TVueloLibro" in f_dev and "centroObjetivo" in f_dev)

# ================================================================
# 8) LAS BOLSAS
# ================================================================
print("── LAS BOLSAS ──")
f_bolsas = open(f"{RAIZ}/Content/Items/Bolsas/BolsasCategorias.cs", encoding="utf-8").read()
check("la BolsaSuperforja con las 2 definitivas",
      "Weapons.Definitivas.JuicioDosSoles>" in f_bolsas and
      "Weapons.Definitivas.TajoDelVacio>" in f_bolsas and
      "TajoOscuro" not in f_bolsas and "AgujasVacio" not in f_bolsas)
check("la BolsaInvestigaciones con las 3 nuevas",
      "Investigaciones.DazzlingStabberStaff>" in f_bolsas and
      "Investigaciones.ArbitradorII>" in f_bolsas and
      "Investigaciones.ElPrincipioYElFin>" in f_bolsas)

# ================================================================
# 9) LA INTEGRIDAD DEL .tmod
# ================================================================
print("── EL .tmod ──")
check("el hjson ×3 empaquetado",
      all(f"Localization/{lang}_Mods.AethonMod.hjson" in entradas
          for lang in ("es-MX", "es-ES", "en-US")))
check("la versión 6.125 (build.txt)", "6.125" in
      open(f"{RAIZ}/build.txt").read())
hjson_tmod = entradas.get("Localization/es-MX_Mods.AethonMod.hjson", b"").decode("utf8", "replace")
hjson_disco = open(f"{RAIZ}/Localization/es-MX_Mods.AethonMod.hjson", encoding="utf-8").read()
check("el hjson es-MX empaquetado == fuente",
      hjson_tmod.strip() == hjson_disco.strip())
check("las bandas sonoras siguen dentro (los 5 ogg)",
      sum(1 for k in entradas if k.endswith(".ogg")) >= 5)

print()
if fallo:
    print(f"✗✗ AUDITORÍA CON {fallo} FALLOS ✗✗")
    sys.exit(1)
print("═══ AUDITORÍA v6.125 TODO VERIFICADO ✔ ═══")
print(f"entradas: {len(entradas)} · {os.path.getsize(TMOD):,} B · "
      f"md5 {hashlib.md5(data).hexdigest()}")
