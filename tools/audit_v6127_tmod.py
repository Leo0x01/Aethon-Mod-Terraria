#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
audit_v6127_tmod.py — LA AUDITORÍA DEL .tmod v6.127 (la obra de la sesión):

  · LA SIERRA MUERTA DE VERDAD (la letra: «las alas rúnicas del vacío
    siguen con picos, investiga a fondo la causa raíz»): el sim 1:1
    (scripts/sim_alas_v6127.py) cazó la causa — el CONTORNO PERCIBIDO
    lo dibujaban las CINCO PLUMAS (31,9 px de zigzag): la paleta
    VIAJERA pintaba el velo y la banda del borde con el FONDO NEGRO de
    la paleta (26,14,44) en la mitad exterior, y la aguja de cada pluma
    (el perfil SUBÍA hacia la punta) clavaba cada diente. LA CURA
    (4 actos): la aguja muere dentro (Suave en EmitirCapa), la vela
    CLAREA hacia el bordillo (0,55→1,17 ×CuerpoInten, blanco 0,05→0,43),
    el bordillo LUMINOSO (×1,5, tope 1,05, +0,12/+0,20 blanco, 16 px) y
    la runa corona el bordillo DESDE DENTRO (9 px adentro, glow ×0,75).
    Zigzag medido: 31,9 → 3,5 px (spread 1,0) · 13,8 → 2,4 px (0,6).
  · EL CETRO DE LAS DAGAS DEL ETERNO (la letra: «las dagas no van en el
    grimorio por ahora… crea un arma nueva que invoque las 5 dagas en la
    posición correcta, que tenga su propio contador de minions, y ese
    contador invoque los 11 ojos Y a la vez las 5 dagas simultáneamente»).
  · EL JUICIO MEJORADO (la letra: «la animación actual es
    insatisfactoria, mejórala» + «máximo 1 anillo por sol; al fusionarse
    deben verse 3 anillos»).
  · LOS COSMÉTICOS PEGADOS (la letra: «las alas giran en una posición
    diferente a la del jugador y se separan de él… el mismo bug afecta a
    otros ítems cosméticos»).
  · LOS TEXTOS CRUDOS CURADOS (la letra: «los textos crudos contienen
    los diálogos del grimorio al entrar al mundo interior — prohibido
    eliminarlos, solo coloca el texto definitivo»): Eco.Interior1..8 y
    Cultistas.* vivían MAL ANIDADOS (Eco.Interior.Interior1 y
    Furia.Cultistas.*) — el juego mostraba LA CLAVE CRUDA. Ahora anidadas
    donde el juego las busca, con TODO su texto intacto.
"""
import os
import re
import sys

RAIZ = os.path.join(os.path.dirname(__file__), "..", "AethonMod")
TMOD = "/tmp/tmlhome/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod"

fallo = 0


def check(nombre, ok, det=""):
    global fallo
    print(("  ✓ " if ok else "  ✗ ") + nombre + (f" — {det}" if det else ""))
    if not ok:
        fallo += 1


def leer_tmod(ruta):
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
            v = self.d[self.o:self.o+n].decode("utf-8", "replace")
            self.o += n
            return v
        def i32(self):
            v = st.unpack_from("<i", self.d, self.o)[0]; self.o += 4
            return v

    d = open(ruta, "rb").read()
    r = R(d, 4)
    r.s()
    r.o += 20 + 256 + 4
    r.s(); r.s()
    n = r.i32()
    tabla = [(r.s(), r.i32(), r.i32()) for _ in range(n)]
    off = r.o
    entradas = {}
    for ruta_e, raw, comp in tabla:
        blob = d[off:off+comp]; off += comp
        entradas[ruta_e] = blob if comp == raw else zlib.decompress(blob, -15)
    return d, entradas


print("═══ LA AUDITORÍA v6.127 ═══")
check("el .tmod existe", os.path.exists(TMOD),
      f"{os.path.getsize(TMOD):,} B" if os.path.exists(TMOD) else TMOD)
data, entradas = leer_tmod(TMOD)
check("la versión 6.127 en el .tmod", b"6.127" in entradas.get("Info", b""),
      entradas.get("Info", b"").decode("latin1", "replace")[:48])

f_lib = open(os.path.join(RAIZ, "Content/VFX/AlasAuroraLib.cs"), encoding="utf-8").read()
f_sigilo = open(os.path.join(RAIZ, "Content/VFX/SigiloLib.cs"), encoding="utf-8").read()
f_cetro = open(os.path.join(RAIZ, "Content/Weapons/Investigaciones/CetroDagasEterno.cs"), encoding="utf-8").read()
f_grim = open(os.path.join(RAIZ, "Content/Weapons/GrimoireEternal.cs"), encoding="utf-8").read()
f_juicio = open(os.path.join(RAIZ, "Content/Weapons/Definitivas/JuicioDosSoles.cs"), encoding="utf-8").read()
f_dev = open(os.path.join(RAIZ, "Content/Projectiles/Definitivas/DevoracionEterna.cs"), encoding="utf-8").read()
f_alas_halo = open(os.path.join(RAIZ, "Content/Projectiles/Cosmetic/AlasAuroraHalo.cs"), encoding="utf-8").read()
dll = entradas.get("AethonMod.dll", b"")

# ================================================================
# 1) LA SIERRA MUERTA — LA CAUSA RAÍZ Y LOS CUATRO ACTOS
# ================================================================
print("── LA SIERRA MUERTA ──")
check("ACTO I · la aguja muere dentro (Suave en EmitirCapa tras Membrana>0)",
      "Suave(0.60f, 1f, num4)" in f_lib and "num10 *= 1f - Suave" in f_lib)
check("ACTO II · la vela clarea hacia el bordillo (0.55+0.62·sube, blanco 0.05+0.38)",
      "(0.55f + 0.62f * sube0)" in f_lib and "hb0 = 0.05f + 0.38f * sube0" in f_lib)
check("ACTO III · el bordillo luminoso (×1.5 tope 1.05, 16 px, +0.20 blanco)",
      "CuerpoInten * 1.55f * 1.5f" in f_lib and "grosor = 12f + 4f * fuerza" in f_lib
      and "rimBlanco = 0.20f" in f_lib)
check("ACTO IV · la runa corona el bordillo desde dentro (9 px, glow ×0.75)",
      "radial / rl * 9f" in f_lib and "glowK = 0.75f" in f_lib)
check("SigiloLib.Runa con el glowK de la casa (param opcional)",
      "float glowK = 1f" in f_sigilo and "34f * escala * glowK" in f_sigilo)
check("LA FORMA ORIGINAL INTACTA (el fan v6.118: membrana 1 · redonda 1 · puntas 20/14)",
      '"Ruido_6", 5, 90f, 52f, 3.5f, 20f, 14f' in
      open(os.path.join(RAIZ, "Content/VFX/AlasEstilo.cs"), encoding="utf-8").read())
check("las firmas en la DLL (Suave · EmitirBordeMaestro · EmitirMembrana)",
      b"Suave" in dll and b"EmitirBordeMaestro" in dll and b"EmitirMembrana" in dll)

# ================================================================
# 2) EL CETRO DE LAS DAGAS DEL ETERNO
# ================================================================
print("── EL CETRO DE LAS DAGAS ──")
check("el arma en disco y en la DLL", os.path.exists(os.path.join(RAIZ,
      "Content/Weapons/Investigaciones/CetroDagasEterno.cs")) and b"CetroDagasEterno" in dll)
check("el sprite del cetro DENTRO del .tmod",
      "Content/Weapons/Investigaciones/CetroDagasEterno.rawimg" in entradas)
check("SU PROPIO CONTADOR plano (TechoOjos = 11, sin llamar a TechoOjosDe)",
      "public const int TechoOjos = 11;" in f_cetro and
      "ShardPlayer.TechoOjosDe" not in f_cetro)
check("TODO A LA VEZ: los ojos que faltan hasta 11 Y las dagas que faltan",
      "for (int k = ojosVivos; k < TechoOjos; k++)" in f_cetro and
      "ViveTipo(player, k)" in f_cetro)
check("la posición correcta: las dagas nacen sobre el portador (su anillo se ordena solo)",
      "player.Center - Vector2.UnitY * 30f" in f_cetro)
check("mana 0 (la regla de las armas de investigación)", "Item.mana = 0;" in f_cetro)
check("LOS ICONOS del escuadrón (los dos buffs)", "OjoEternoBuff>(), 18000" in f_cetro
      and "DagaEternaBuff>(), 18000" in f_cetro)
check("LA BOLSA 26 lo lleva",
      "Weapons.Investigaciones.CetroDagasEterno" in
      open(os.path.join(RAIZ, "Content/Items/Bolsas/BolsasCategorias.cs"), encoding="utf-8").read())
check("LA ESCALERA MUERTA EN EL GRIMORIO (las dagas YA NO se invocan desde el libro)",
      "DagaInvocada" not in f_grim and "TechoDagas" not in f_grim)
check("el grimorio solo cuenta SUS ojos (tooltip sin dagas)",
      "Ojos: {ojosVivos}/{techoOjos}" in f_grim and "Dagas: {dagas" not in f_grim)
check("la tira de las dagas + su icono siguen DENTRO",
      "Content/Projectiles/DagaEterna_Hoja.rawimg" in entradas and
      "Content/Buffs/DagaEternaBuff.rawimg" in entradas)

# ================================================================
# 3) EL JUICIO — LA LEY DE LOS ANILLOS + LA ANIMACIÓN MEJORADA
# ================================================================
print("── EL JUICIO DE LOS DOS SOLES ──")
check("LA LEY: UN anillo por sol (tier 1 en los dos soles, siempre)",
      f_juicio.count("DibujarSol(luz, pos, r, gt, 1,") == 2 and
      "DibujarSol(luz, pos, r, gt, 8," not in f_juicio)
check("LA LEY: TRES anillos en el sol fusionado (nace, carga, colapsa y estalla con 3)",
      f_juicio.count("DibujarSolFusionado(Punto") == 4 and
      f_juicio.count(", 3, rg") == 4 and
      "DibujarSolFusionado(Punto, r, gt, 12" not in f_juicio and
      "DibujarSolFusionado(Punto, r, gt, 14" not in f_juicio and
      "DibujarSolFusionado(Punto, r, gt, 16" not in f_juicio)
check("LA ESPIRAL DE MUERTE: el giro ACELERA (pc·pc·10.5) y la órbita SE CAE (^1.45)",
      "pc * pc * 10.5f" in f_juicio and "MathF.Pow(caida, 1.45f)" in f_juicio)
check("EL CONTACTO: P_CONTACTO 0.78 + el DESTELLO de fusión (Tint de la casa)",
      "P_CONTACTO = 0.78f" in f_juicio and "SigiloLib.Tint" in f_juicio)
check("EL COLAPSO: la nova estalla AL FONDO (T_COLAPSO 7, hitbox tras el pliegue)",
      "T_COLAPSO = 7" in f_juicio and "_t == T_COLAPSO) NacerLaNova()" in f_juicio)
check("los soles CRECEN durante la convocatoria (0.5→0.92)",
      "MathHelper.Lerp(0.5f, 0.92f, suave)" in f_juicio)
check("las firmas en la DLL (PosicionSolSencillo)",
      b"PosicionSolSencillo" in dll)

# ================================================================
# 4) LOS COSMÉTICOS PEGADOS AL JUGADOR
# ================================================================
print("── LA DEVORACIÓN EN CONJUNTO ──")
check("EL PEGAMENTO: DevoracionCosmeticosSistema (PostUpdateProjectiles)",
      "class DevoracionCosmeticosSistema : ModSystem" in f_dev and
      "PostUpdateProjectiles" in f_dev)
check("LA VENTANA DEL ARRASTRE (Arrastrando, −1 al trago/desiste/kill)",
      "public static int Arrastrando = -1;" in f_dev and f_dev.count("Arrastrando = -1") >= 4
      and "Arrastrando = p.whoAmI" in f_dev)
check("las alas se PLEGAN también durante el ARRASTRE",
      "Arrastrando == val.whoAmI" in f_alas_halo)
check("los halos ocultos durante el TRAGO (×5 clases)",
      all("DevoracionEterna.Tragado" in
          open(os.path.join(RAIZ, f), encoding="utf-8").read() for f in [
              "Content/Projectiles/Cosmetic/AuraPortadorHalo.cs",
              "Content/Projectiles/Cosmetic/AnilloRunicoDorsalHalo.cs",
              "Content/Projectiles/Cosmetic/BrilloSolarHalo.cs",
              "Content/Projectiles/Cosmetic/SellosCosmeticosHalos.cs"]))
check("las firmas en la DLL (DevoracionCosmeticosSistema · Arrastrando)",
      b"DevoracionCosmeticosSistema" in dll and b"Arrastrando" in dll)

# ================================================================
# 5) LOS TEXTOS CRUDOS — LOS DIÁLOGOS DEL INTERIOR
# ================================================================
print("── LOS TEXTOS CRUDOS CURADOS ──")
import hjson


def plano(d, pila=()):
    out = {}
    for k, v in d.items():
        ruta = ".".join(pila + (k,))
        if isinstance(v, dict):
            out.update(plano(v, pila + (k,)))
        else:
            out[ruta] = v
    return out


hjson_tmod = {}
for idioma in ("es-ES", "es-MX", "en-US"):
    clave = f"Localization/{idioma}_Mods.AethonMod.hjson"
    check(f"el hjson {idioma} DENTRO del .tmod", clave in entradas)
    hjson_tmod[idioma] = plano(hjson.loads(entradas[clave].decode("utf-8")))

for idioma in ("es-ES", "es-MX", "en-US"):
    d = hjson_tmod[idioma]
    interior = [k for k in d if re.fullmatch(r"Eco\.Interior[1-8]", k)]
    check(f"{idioma}: Eco.Interior1..8 planas y VIVAS (los diálogos NO eliminados)",
          len(interior) == 8, f"{len(interior)}/8 · «{str(d.get('Eco.Interior1', ''))[:44]}…»")
    check(f"{idioma}: el viejo nest Eco.Interior.Interior1 MUERTO",
          "Eco.Interior.Interior1" not in d)
    cult = [k for k in d if k.startswith("Cultistas.")]
    check(f"{idioma}: Cultistas.* en la RAÍZ (8 claves: Burla×3 · Vencido×2 · Devorado · ElJefe · Puerta)",
          len(cult) == 8, f"{len(cult)} claves")
    check(f"{idioma}: el viejo nest Furia.Cultistas.* MUERTO",
          not any(k.startswith("Furia.Cultistas.") for k in d))
    for k in ("Armas.CetroEscuadron", "Armas.CetroCompleto",
              "Items.CetroDagasEterno.DisplayName", "Items.CetroDagasEterno.Linea1"):
        check(f"{idioma}: {k}", k in d)

es = set(hjson_tmod["es-ES"]); mx = set(hjson_tmod["es-MX"]); en = set(hjson_tmod["en-US"])
check("PARIDAD ×3: es-ES == es-MX == en-US (claves idénticas)",
      es == mx == en, f"{len(es)} == {len(mx)} == {len(en)}")

# ================================================================
# 6) EL RESTO DE LA CASA (la música, los ojos, el mundo interior)
# ================================================================
print("── LA CASA ENTERA ──")
musica = sorted(k for k in entradas if k.endswith(".ogg"))
check("las 5 pistas de la banda sonora DENTRO",
      sum(1 for k in musica if k.startswith("Music/")) == 5, str([k for k in musica if k.startswith("Music/")]))
check("las runas del abismo y el alfabeto viven (OrbitaLib en la DLL)",
      b"RunasVacio" in dll and b"SlotDeFormacion" in dll)
check("el mundo interior vivo (SubmundoGrimorio en la DLL)",
      b"SubmundoGrimorio" in dll and b"HablarInterior" in dll)

print(f"entradas: {len(entradas)} · {os.path.getsize(TMOD):,} B · "
      f"{'TODO VERIFICADO ✔' if fallo == 0 else str(fallo) + ' FALLOS ✗'}")
sys.exit(1 if fallo else 0)

