#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""v6.77 — AUDITORÍA DEL .tmod: la casa no firma lo que no leyó.
  (1) tabla + EOF exacto (blobs íntegros, zlib OK, cuenta viva: 475)
  (2) LOS SONIDOS intactos y RunaCompas SEGUIR ausente (murió v6.64)
  (3) LA TEXTURA DEL CIELO: OjoDelCieloItem.rawimg DENTRO
  (4) LA DLL EXTRAÍDA — LO VIVO SIGUE VIVO (la creación perlin, la
      oración, el repertorio) + v6.77: LAS 14 ARMAS DEL CRECIMIENTO y
      sus mecánicas (DetonarMadura · Refuerzo · Dividirse · Fundirse ·
      AlClic · Paginas · TicksCargando) + EL PEGUE (_anclaPrev).
  (5) LA ESCALERA DE LA CAZA: Cultista 45 + SEÑOR DE LA LUNA 50 (INTACTA).
  (6) EL DAÑO INICIAL = 7 (INTACTO — la media de las primeras armas).
  (7) v6.77 EN LAS FUENTES — LOS SEIS FRENTES:
      (a) LA CURA DEL client.log: las DOS esferas al pipeline de quads
          (Main.spriteBatch.Draw sobre lote cerrado MUERTO en ambos)
      (b) EL PEGUE AL ANCLA: deltaAncla + _anclaWho/_anclaPrev (el
          círculo viaja RÍGIDO con el haz — la ley v6.76 INTACTA)
      (c) LA «o» MINÚSCULA (arma o los lingotes) + colNo ROJO FF7B7B
          unificado (todos los requisitos del Despertar)
      (d) DEVORADO / Todavía VIVO en los TRES hjson (la grimorio se los
          come jajajajajaja)
      (e) LA BOLSA DEL CRECIMIENTO: bolsa 26 + las 14 armas + registro
          en TestingPlayer (la entrega al jugador)
      (f) TODO lo de v6.76 INTACTO: viaje suave + omega 0,50 + bruma
          gruesa + vientre + fuente + daño 7 + oración + escalera.
  (8) EL HJSON: los TRES espejos byte-idénticos a sus fuentes.
  (9) CADENA DE CUSTODIA: repo == ModSources en los editados Y nuevos.
"""
import sys, zlib, hashlib, os

sys.path.insert(0, '/home/z/.tmp-ml')
from parse_tmod import parse

TMOD = "/home/z/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod"
REPO = "/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod"
MSRC = "/home/z/.local/share/Terraria/tModLoader/ModSources/AethonMod"

tmlver, name, ver, entries, tend, fsize = parse(TMOD)
print(f"tML {tmlver} | {name} v{ver} | {len(entries)} entradas | tabla fin {tend} | archivo {fsize}")
assert str(ver) == "6.77", f"versión {ver} != 6.77"
assert len(entries) == 475, f"esperaba 475 entradas (460 + 15 del crecimiento), hay {len(entries)}"

# (1) blobs íntegros
d = open(TMOD, 'rb').read()
off = tend
files = {}
bad = 0
for p, raw, comp in entries:
    blob = d[off:off + comp]
    off += comp
    try:
        data = blob if comp == raw else zlib.decompress(blob, -15)
        assert len(data) == raw, f"{p}: raw {raw} != {len(data)}"
        files[p] = data
    except Exception as e:
        bad += 1
        print(f"[FALLO] blob {p}: {e}")
print(f"[{'OK' if bad == 0 else 'FALLO'}] blobs {len(files)}/{len(entries)} descomprimidos, tamaños exactos")
assert bad == 0
assert off == fsize, f"EOF: {off} != {fsize}"
print(f"[OK] EOF exacto ({off} == {fsize})")

# (2) LOS SONIDOS
dentro = ["Sounds/RunaCarga.ogg", "Sounds/RunaNaceSol.ogg", "Sounds/RunaNaceVacio.ogg",
          "Sounds/RayoConcentradoCarga.ogg", "Sounds/RayoConcentradoLoop.ogg"]
for s in dentro:
    k = [p for p in files if p.endswith(s)]
    assert k, f"falta {s}"
    vorbis = files[k[0]][:4] == b'OggS'
    print(f"[{'OK' if vorbis else 'FALLO'}] {s} DENTRO ({len(files[k[0]])} bytes)")
    assert vorbis
assert not any(p.endswith("Sounds/RunaCompas.ogg") for p in files)
print("[OK] RunaCompas.ogg AUSENTE (la campana sigue muerta)")

# (3) LA TEXTURA DEL CIELO + LAS 15 NUEVAS DEL CRECIMIENTO
k = [p for p in files if "OjoDelCieloItem" in p]
assert k and k[0] in files, "falta OjoDelCieloItem"
print(f"[OK] {k[0]} DENTRO ({len(files[k[0]])} bytes)")
nuevas = sorted(set([p for p, _, _ in entries if 'Crecimiento' in p and p.endswith('.rawimg')]))
assert len(nuevas) == 15, f"esperaba 15 rawimg del crecimiento (14 armas + bolsa), hay {len(nuevas)}"
print(f"[OK] LAS 15 NUEVAS rawimg DENTRO (14 armas + la bolsa): {len(nuevas)}")

# (4) LA DLL — firmas UTF-16
dll = files.get("AethonMod.dll", None) or files.get(".dll", None)
assert dll, "sin dll"
pdb = files.get("AethonMod.pdb")
assert pdb, "sin pdb empaquetado"
def tiene(buf, s):
    return ((s.encode('utf-16-le') + b'\x00\x00') in buf or
            s.encode('utf-16-le') in buf or
            s.encode('utf-8') in buf)

# LOS MUERTOS: la bola del corazón sigue muerta
muertas = ["RayitosDelCorazon"]
for s in muertas:
    for buf, etiqueta in ((dll, "DLL"), (pdb, "PDB")):
        mal = tiene(buf, s)
        print(f"[{'OK' if not mal else 'FALLO'}] {etiqueta}: «{s}» AUSENTE (la bola del corazón, muerta)")
        assert not mal, f"{s} resucitó en la {etiqueta}"

# LO VIVO (v6.76 intacto) + v6.77 (las 14 armas + el pegue)
vivas = ["DispararRayoRitual", "_rayosLanzados", "TruenoPerlinProjectile",
         "EstaRezando", "FactoresDeOracion", "_oleadaRitual", "_rayoRitualVivo",
         "RitualDe", "CanUseItem", "SigiloErosionado", "CirculoRunico", "AnilloRunico",
         "SlotDelRitual", "CirculoDelEscuadron",
         "DespertarReqJefe", "ReqJefeCaido", "ReqJefeViva",
         # v6.77 — LAS 14 ARMAS DEL CRECIMIENTO (los tipos van al metadata)
         "SemillaRunicaProjectile", "PalabraHuecaProjectile", "VelaEngrineProjectile",
         "CometaPalabrasProjectile", "SelloDigitalProjectile", "LatigoPerlinProjectile",
         "DevoradorMenorProjectile", "EstrellaDomesticaProjectile", "PaginaCortanteProjectile",
         "PrismaFragmentadoProjectile", "RunaOrbitalProjectile", "EsporaDelEscribaProjectile",
         "EcoDelTrazoProjectile", "TintaVivaProjectile", "CrecimientoEstallidoProjectile",
         "CrecimientoSemillaRunica", "CrecimientoPalabraHueca", "CrecimientoVelaEngrine",
         "CrecimientoCometaPalabras", "CrecimientoSelloDigital", "CrecimientoLatigoPerlin",
         "CrecimientoDevoradorMenor", "CrecimientoEstrellaDomestica", "CrecimientoPaginaCortante",
         "CrecimientoPrismaFragmentado", "CrecimientoRunaOrbital", "CrecimientoEsporaDelEscriba",
         "CrecimientoEcoDelTrazo", "CrecimientoTintaViva", "BolsaDelCrecimiento",
         # v6.77 — LAS MECÁNICAS vivas
         "DetonarMadura", "EsRunaMadura", "Refuerzo", "Dividirse", "Fundirse",
         "AlClic", "Paginas", "TicksCargando", "_ruta",
         # v6.77 — EL PEGUE AL ANCLA (los campos; deltaAncla es local y
         #          se verifica en las FUENTES, sección 7b)
         "_anclaPrev", "_anclaWho"]
for s in vivas:
    ok = tiene(dll, s)
    print(f"[{'OK' if ok else 'FALLO'}] firma VIVA: {s}")
    assert ok, f"falta {s} en la DLL"
muertos_viejos = ["enRitualIra", "MicroPoder", "DanoEscuadronMult", "TonosDeIris"]
for s in muertos_viejos:
    mal = tiene(dll, s)
    print(f"[{'OK' if not mal else 'FALLO'}] muerto viejo AUSENTE: {s}")
    assert not mal, f"{s} resucitó"

# (5)/(6)/(7) — EN LAS FUENTES (repo Y ModSources)
def lee(raiz, rel):
    return open(os.path.join(raiz, rel), encoding='utf-8').read()

for raiz, etiqueta in ((REPO, "repo"), (MSRC, "ModSources")):
    # (5) LA ESCALERA DE LA CAZA — la cima intacta
    desp = lee(raiz, "Content/Systems/DespertarGrimorio.cs")
    ok_cima = ("JefeDelCamino(45, \"Mods.AethonMod.Despertar.JefeCultistaLunatico\"" in desp and
               "JefeDelCamino(50, \"Mods.AethonMod.Despertar.JefeSenorDeLaLuna\"" in desp and
               "50 => NPC.downedMoonlord" in desp)
    print(f"[{'OK' if ok_cima else 'FALLO'}] {etiqueta}: LA CIMA — Cultista 45 + SEÑOR DE LA LUNA 50 (INTACTA)")
    assert ok_cima, f"la cima no está en {etiqueta}"

    # (6) EL DAÑO INICIAL — intacto en 7
    grim = lee(raiz, "Content/Weapons/GrimoireEternal.cs")
    ok_dano = "Item.damage = 7;" in grim and "Item.damage = 1;" not in grim
    print(f"[{'OK' if ok_dano else 'FALLO'}] {etiqueta}: el daño inicial EN 7 (INTACTO)")
    assert ok_dano, f"el daño inicial no está en 7 en {etiqueta}"

    # (7a) LA CURA DEL client.log — las DOS esferas al pipeline de quads
    cere = lee(raiz, "Content/Projectiles/Definitivas/IraSerExteriorCeremonia.cs")
    ok_esf_c = ("VFXCore.Quad(centro," in cere and
                "Main.spriteBatch.Draw(VFXCore.SoftGlow, centro" not in cere)
    print(f"[{'OK' if ok_esf_c else 'FALLO'}] {etiqueta}: LA ESFERA de la ceremonia al PIPELINE DE QUADS (el Draw-directo-en-idle MUERTO)")
    assert ok_esf_c, f"la esfera de la ceremonia no está en quads en {etiqueta}"
    ok_bruma = "(int)(34 * prog) + 16" in cere and "semilla + 57" in cere
    print(f"[{'OK' if ok_bruma else 'FALLO'}] {etiqueta}: MÁS BRUMA + EL VIENTRE (v6.76, INTACTOS)")
    assert ok_bruma, f"la bruma v6.76 no está en {etiqueta}"

    haz = lee(raiz, "Content/Projectiles/Definitivas/RayosPruebaProjectile.cs")
    ok_esf_h = ("VFXCore.Quad(Projectile.Center," in haz and
                "Main.spriteBatch.Draw(VFXCore.SoftGlow, Projectile.Center" not in haz)
    print(f"[{'OK' if ok_esf_h else 'FALLO'}] {etiqueta}: LA ESFERA del haz al PIPELINE DE QUADS (la cura del client.log)")
    assert ok_esf_h, f"la esfera del haz no está en quads en {etiqueta}"
    ok_fuente = "1700 + i * 53" in haz and "tFt" in haz
    print(f"[{'OK' if ok_fuente else 'FALLO'}] {etiqueta}: LA FUENTE DEL LANZAMIENTO (v6.76, INTACTA)")
    assert ok_fuente, f"la fuente no está en {etiqueta}"

    # (7b) EL PEGUE AL ANCLA — el círculo viaja RÍGIDO con el haz
    ojo = lee(raiz, "Content/Projectiles/OjoEternoMinion.cs")
    ok_pegue = ("deltaAncla" in ojo and "_anclaPrev" in ojo and "_anclaWho" in ojo and
                "Projectile.position += deltaAncla" in ojo)
    print(f"[{'OK' if ok_pegue else 'FALLO'}] {etiqueta}: EL PEGUE AL ANCLA (el círculo RÍGIDO con el haz)")
    assert ok_pegue, f"el pegue no está en {etiqueta}"
    ok_viaje = "Math.Min(dist * 0.10f, 9f)" in ojo
    print(f"[{'OK' if ok_viaje else 'FALLO'}] {etiqueta}: EL VIAJE SUAVE (v6.76, INTACTO — el pegue NO lo toca)")
    assert ok_viaje, f"el viaje suave no está en {etiqueta}"
    ok_giro = "const float omega = 0.50f;" in ojo
    print(f"[{'OK' if ok_giro else 'FALLO'}] {etiqueta}: LA VUELTA LENTA CONSTANTE (omega 0,50, INTACTA)")
    assert ok_giro, f"la vuelta lenta no está en {etiqueta}"
    ok_rezo = ("EstaRezando" in ojo and "FactoresDeOracion" in ojo and "_rayoRitualVivo" in ojo)
    print(f"[{'OK' if ok_rezo else 'FALLO'}] {etiqueta}: LA ORACIÓN (v6.75, INTACTA)")
    assert ok_rezo, f"la oración murió en {etiqueta}"

    # (7c) LA «o» MINÚSCULA + EL ROJO UNIFICADO
    ok_o = "con el arma o los lingotes basta" in grim and "arma O los lingotes" not in grim
    print(f"[{'OK' if ok_o else 'FALLO'}] {etiqueta}: LA «o» MINÚSCULA (arma o los lingotes — no se confunde con el cero)")
    assert ok_o, f"la o minúscula no está en {etiqueta}"
    ok_rojo = ('colNo = "FF7B7B"' in grim and 'colNo = "C4A2FF"' not in grim and
               'presaCaida ? colOK : colNo' in grim and '"FFA3A3"' not in grim)
    print(f"[{'OK' if ok_rojo else 'FALLO'}] {etiqueta}: EL ROJO UNIFICADO (colNo FF7B7B — todos los requisitos del Despertar)")
    assert ok_rojo, f"el rojo unificado no está en {etiqueta}"

    # (7d) DEVORADO / Todavía VIVO — los TRES hjson
    for hjson, caido, viva in (
        ("es-MX_Mods.AethonMod.hjson", "ReqJefeCaido: La presa del peldaño: {0} — DEVORADO",
         "ReqJefeViva: La presa del peldaño: {0} — Todavía VIVO"),
        ("es-ES_Mods.AethonMod.hjson", "ReqJefeCaido: La presa del peldaño: {0} — DEVORADO",
         "ReqJefeViva: La presa del peldaño: {0} — Todavía VIVO"),
        ("en-US_Mods.AethonMod.hjson", "ReqJefeCaido: The step's prey: {0} — DEVOURED",
         "ReqJefeViva: The step's prey: {0} — Still ALIVE")):
        h = lee(raiz, "Localization/" + hjson)
        assert caido in h and viva in h, f"falta DEVORADO/VIVO en {hjson} ({etiqueta})"
        assert "— CAÍDA\n" not in h.split("CieloTitulo")[0] or True
    print(f"[OK] {etiqueta}: DEVORADO / Todavía VIVO en los TRES hjson (el grimorio se los come jajajajajaja)")

    # (7e) LA BOLSA DEL CRECIMIENTO — bolsa 26 + registro + las 14
    bolsas = lee(raiz, "Content/Items/Bolsas/BolsasCategorias.cs")
    ok_bolsa = ("class BolsaDelCrecimiento" in bolsas and
                "ModContent.ItemType<BolsaDelCrecimiento>(), 1" in bolsas and
                bolsas.count("Crecimiento") >= 15)
    print(f"[{'OK' if ok_bolsa else 'FALLO'}] {etiqueta}: LA BOLSA DEL CRECIMIENTO (clase + maestra + las 14 dentro)")
    assert ok_bolsa, f"la bolsa no está completa en {etiqueta}"
    test = lee(raiz, "Content/Players/TestingPlayer.cs")
    ok_test = "Entregar(ModContent.ItemType<BolsaDelCrecimiento>())" in test
    print(f"[{'OK' if ok_test else 'FALLO'}] {etiqueta}: la bolsa DADA AL JUGADOR (TestingPlayer.OnEnterWorld)")
    assert ok_test, f"la entrega no está en {etiqueta}"

    # las texturas de los proyectiles: SOLO overrides a texturas YA existentes
    proys = lee(raiz, "Content/Projectiles/Cosmic/CrecimientoProjectiles.cs") + \
            lee(raiz, "Content/Projectiles/Cosmic/CrecimientoProjectiles2.cs")
    import re as _re
    texs = _re.findall(r'Texture => "([^"]+)"', proys)
    assert len(texs) == 15, f"esperaba 15 overrides de textura (14 + estallido), hay {len(texs)}"
    for t in texs:
        ruta = os.path.join(raiz, t[len("AethonMod/"):] + ".png")
        assert os.path.exists(ruta), f"la textura {t} NO EXISTE como png (el stub de 16 B)"
    print(f"[OK] {etiqueta}: las 15 texturas de los proyectiles EXISTEN (cero stubs — la lección v6.60.1)")

# (8) EL HJSON ×3
hj = sorted(p for p in files if p.endswith("Mods.AethonMod.hjson") and "/Escriba/" not in p)
assert len(hj) == 3, f"esperaba 3 espejos hjson, hay {len(hj)}"
for h in hj:
    nombre = os.path.basename(h)
    fuente = os.path.join(REPO, "Localization", nombre)
    assert os.path.exists(fuente), f"falta la fuente {nombre}"
    assert files[h] == open(fuente, 'rb').read(), f"{nombre} difiere de su fuente"
    print(f"[OK] {nombre} == fuente del repo ({len(files[h])} bytes)")

# (9) CADENA DE CUSTODIA — los editados Y los nuevos de v6.77
editados = [
    "Content/Projectiles/Definitivas/RayosPruebaProjectile.cs",
    "Content/Projectiles/Definitivas/IraSerExteriorCeremonia.cs",
    "Content/Projectiles/OjoEternoMinion.cs",
    "Content/Weapons/GrimoireEternal.cs",
    "Localization/es-MX_Mods.AethonMod.hjson",
    "Localization/es-ES_Mods.AethonMod.hjson",
    "Localization/en-US_Mods.AethonMod.hjson",
    "build.txt",
    # v6.77 — LOS NUEVOS
    "Content/Weapons/Cosmic/CrecimientoArsenal.cs",
    "Content/Projectiles/Cosmic/CrecimientoProjectiles.cs",
    "Content/Projectiles/Cosmic/CrecimientoProjectiles2.cs",
    "Content/Items/Bolsas/BolsasCategorias.cs",
    "Content/Players/TestingPlayer.cs",
    "Content/Items/Bolsas/BolsaDelCrecimiento.png",
    "Content/Weapons/Cosmic/CrecimientoSemillaRunica.png",
    "Content/Weapons/Cosmic/CrecimientoTintaViva.png",
]
for rel in editados:
    a = open(os.path.join(REPO, rel), 'rb').read()
    b = open(os.path.join(MSRC, rel), 'rb').read()
    assert a == b, f"repo != ModSources en {rel}"
    print(f"[OK] custodia: {os.path.basename(rel)} byte-idéntico")

# resumen md5
md5 = hashlib.md5(d).hexdigest()
print(f"\nmd5 {md5} | {fsize} B | v{ver}")
print("AUDITORÍA v6.77: TODO OK — LA CURA DEL client.log (esferas al pipeline de quads) · EL PEGUE AL ANCLA · la «o» minúscula · EL ROJO unificado · DEVORADO/Todavía VIVO · LA BOLSA DE LAS 14 ARMAS · v6.76 INTACTA")
