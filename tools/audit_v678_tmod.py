#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""v6.78 — AUDITORÍA DEL .tmod: la casa no firma lo que no leyó.
  (1) tabla + EOF exacto (blobs íntegros, zlib OK, cuenta viva: 477)
  (2) LOS SONIDOS intactos y RunaCompas SEGUIR ausente
  (3) LA TEXTURA DEL CIELO + LAS 15 DEL CRECIMIENTO DENTRO
  (4) v6.78 — EL CALOR: CalorDistorsionShader.fxc BYTE-IDÉNTICO al
      ensamblado (fx20_build + vkd3d, validado contra MojoShader) y
      RuidoPlasmaCalor.rawimg COMPLETO (512²×4 = 1048588).
  (5) LA DLL EXTRAÍDA — LO VIVO SIGUE VIVO: la librería de calor
      (CalorLib · CalorSistema · PrepararPaseDeCalor · Estallido ·
      AethonCalor · RuidoPlasmaCalor · CalorDistorsionShader) + TODO el
      repertorio de la v6.77 (la creación perlin, la oración, las 14
      armas, el pegue al ancla).
  (6) EN LAS FUENTES — LOS SIETE FRENTES:
      (a) v6.78 — EL CALOR DEL RAYO C: CalorLib.Haz registrado en la IA
          SOLO sin EsIra (la ira NO hierve — la letra), el estallido de
          ignición (15 px / 430) + el suspiro final (5.5 / 280)
      (b) v6.78 — EL HOOK ÚNICO: BlackHoleLensSystem llama
          CalorLib.PrepararPaseDeCalor dentro de ComponerLenteBajoFiltros
      (c) v6.78 — EL FILTRO DE CADENA: ScreenShaderData + Filter +
          Filters.Scene["AethonCalor"] + IsHidden en la descarga
      (d) TODO lo de v6.77 INTACTO: esferas en quads (la cura del
          client.log), el pegue al ancla, la «o» minúscula, el rojo
          unificado, DEVORADO/Todavía VIVO, la bolsa de las 14
      (e) LA ESCALERA + EL DAÑO 7 (INTACTOS)
  (7) EL HJSON: los TRES espejos byte-idénticos a sus fuentes.
  (8) CADENA DE CUSTODIA: repo == ModSources en los editados Y nuevos.
"""
import sys, zlib, hashlib, os

sys.path.insert(0, '/home/z/.tmp-ml')
from parse_tmod import parse

TMOD = "/home/z/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod"
REPO = "/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod"
MSRC = "/home/z/.local/share/Terraria/tModLoader/ModSources/AethonMod"

tmlver, name, ver, entries, tend, fsize = parse(TMOD)
print(f"tML {tmlver} | {name} v{ver} | {len(entries)} entradas | tabla fin {tend} | archivo {fsize}")
assert str(ver) == "6.78", f"versión {ver} != 6.78"
assert len(entries) == 477, f"esperaba 477 entradas (475 + ruido + fxc), hay {len(entries)}"

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

# (3) LA TEXTURA DEL CIELO + LAS 15 DEL CRECIMIENTO
k = [p for p in files if "OjoDelCieloItem" in p]
assert k and k[0] in files, "falta OjoDelCieloItem"
print(f"[OK] {k[0]} DENTRO ({len(files[k[0]])} bytes)")
nuevas = sorted(set([p for p, _, _ in entries if 'Crecimiento' in p and p.endswith('.rawimg')]))
assert len(nuevas) == 15, f"esperaba 15 rawimg del crecimiento, hay {len(nuevas)}"
print(f"[OK] LAS 15 rawimg DEL CRECIMIENTO DENTRO: {len(nuevas)}")

# (4) v6.78 — EL CALOR: el .fxc BYTE-IDÉNTICO + el ruido COMPLETO
FXC = "Content/Effects/Shaders/CalorDistorsionShader.fxc"
assert FXC in files, "falta el CalorDistorsionShader.fxc"
fxc_repo = open(os.path.join(REPO, "Content/Effects/Shaders/CalorDistorsionShader.fxc"), 'rb').read()
assert files[FXC] == fxc_repo, "el .fxc del .tmod NO es byte-idéntico al repo"
print(f"[OK] CalorDistorsionShader.fxc BYTE-IDÉNTICO al ensamblado ({len(fxc_repo)} B — fx20_build + vkd3d, MojoShader-validado)")
RUIDO = "Content/Effects/Procedural/RuidoPlasmaCalor.rawimg"
assert RUIDO in files, "falta el RuidoPlasmaCalor.rawimg"
# 512×512 RGBA + 12 B de cabecera rawimg
assert len(files[RUIDO]) == 512 * 512 * 4 + 12, f"el ruido mide {len(files[RUIDO])}, esperaba 1048588"
print(f"[OK] RuidoPlasmaCalor.rawimg COMPLETO ({len(files[RUIDO])} B = 512² RGBA — no es stub)")

# (5) LA DLL — firmas UTF-16
dll = files.get("AethonMod.dll")
assert dll, "sin dll"
pdb = files.get("AethonMod.pdb")
assert pdb, "sin pdb empaquetado"
def tiene(buf, s):
    return ((s.encode('utf-16-le') + b'\x00\x00') in buf or
            s.encode('utf-16-le') in buf or
            s.encode('utf-8') in buf)

# v6.78 — LA LIBRERÍA DE CALOR VIVA
calor_vivas = ["CalorLib", "CalorSistema", "PrepararPaseDeCalor",
               "Estallido", "AethonCalor", "RuidoPlasmaCalor",
               "CalorDistorsionShader", "VaciarCampo",
               "uTexturaCampo", "uTexturaRuido", "uCAFuerza", "uConveccion",
               "uCalidad", "PaseCalor"]
for s in calor_vivas:
    ok = tiene(dll, s)
    print(f"[{'OK' if ok else 'FALLO'}] firma VIVA (calor): {s}")
    assert ok, f"falta {s} en la DLL"

# LOS MUERTOS: la bola del corazón sigue muerta
for s in ["RayitosDelCorazon"]:
    for buf, etiqueta in ((dll, "DLL"), (pdb, "PDB")):
        mal = tiene(buf, s)
        print(f"[{'OK' if not mal else 'FALLO'}] {etiqueta}: «{s}» AUSENTE (la bola del corazón, muerta)")
        assert not mal

# LO VIVO (el repertorio de la v6.77 — INTACTO)
vivas = ["DispararRayoRitual", "_rayosLanzados", "TruenoPerlinProjectile",
         "EstaRezando", "FactoresDeOracion", "_oleadaRitual", "_rayoRitualVivo",
         "RitualDe", "CanUseItem", "SigiloErosionado", "CirculoRunico", "AnilloRunico",
         "SlotDelRitual", "CirculoDelEscuadron",
         "DespertarReqJefe", "ReqJefeCaido", "ReqJefeViva",
         "SemillaRunicaProjectile", "PalabraHuecaProjectile", "VelaEngrineProjectile",
         "CometaPalabrasProjectile", "SelloDigitalProjectile", "LatigoPerlinProjectile",
         "DevoradorMenorProjectile", "EstrellaDomesticaProjectile", "PaginaCortanteProjectile",
         "PrismaFragmentadoProjectile", "RunaOrbitalProjectile", "EsporaDelEscribaProjectile",
         "EcoDelTrazoProjectile", "TintaVivaProjectile", "CrecimientoEstallidoProjectile",
         "BolsaDelCrecimiento",
         "DetonarMadura", "EsRunaMadura", "Refuerzo", "Dividirse", "Fundirse",
         "AlClic", "Paginas", "TicksCargando", "_ruta",
         "_anclaPrev", "_anclaWho"]
for s in vivas:
    ok = tiene(dll, s)
    print(f"[{'OK' if ok else 'FALLO'}] firma VIVA: {s}")
    assert ok, f"falta {s} en la DLL"
muertos_viejos = ["enRitualIra", "MicroPoder", "DanoEscuadronMult", "TonosDeIris"]
for s in muertos_viejos:
    mal = tiene(dll, s)
    print(f"[{'OK' if not mal else 'FALLO'}] muerto viejo AUSENTE: {s}")
    assert not mal

# (6) — EN LAS FUENTES (repo Y ModSources)
def lee(raiz, rel):
    return open(os.path.join(raiz, rel), encoding='utf-8').read()

for raiz, etiqueta in ((REPO, "repo"), (MSRC, "ModSources")):
    # (6a) v6.78 — EL CALOR DEL RAYO C (solo el arma — la ira NO hierve)
    haz = lee(raiz, "Content/Projectiles/Definitivas/RayosPruebaProjectile.cs")
    ok_reg = ("CalorLib.Haz(Projectile.Center, Projectile.Center + dirCalor * largoCalor" in haz and
              "if (!EsIra && !Main.dedServ && Time >= ChargeTime - 20" in haz)
    print(f"[{'OK' if ok_reg else 'FALLO'}] {etiqueta}: EL CALOR DEL CONDUCTO (CalorLib.Haz en la IA — SOLO el arma-C)")
    assert ok_reg, f"el registro del calor no está en {etiqueta}"
    # el gate EsIra: la línea del registro vive DENTRO del bloque !EsIra
    ok_gate = haz.index("if (!EsIra && !Main.dedServ && Time >= ChargeTime - 20") < \
              haz.index("CalorLib.Haz(Projectile.Center")
    assert ok_gate
    ok_est = ("CalorLib.Estallido(Projectile.Center, 15f, 430f)" in haz and
              "CalorLib.Estallido(Projectile.Center, 5.5f, 280f)" in haz and
              haz.count("if (!EsIra)\n                        CalorLib.Estallido") == 1)
    print(f"[{'OK' if ok_est else 'FALLO'}] {etiqueta}: LOS DOS ESTALLIDOS (ignición 15/430 + suspiro 5.5/280 — el gate EsIra en ambos)")
    assert ok_est, f"los estallidos no están en {etiqueta}"

    # (6b) v6.78 — EL HOOK ÚNICO (la lente llama al pase del calor)
    lens = lee(raiz, "Content/Effects/BlackHoleLensSystem.cs")
    ok_hook = ("CalorLib.PrepararPaseDeCalor();" in lens and
               lens.count("On_FilterManager.EndCapture += ") == 1)
    print(f"[{'OK' if ok_hook else 'FALLO'}] {etiqueta}: EL HOOK ÚNICO (la lente dispara el pase del calor — UN solo EndCapture)")
    assert ok_hook, f"el hook del calor no está en {etiqueta}"

    # (6c) v6.78 — EL FILTRO DE CADENA
    calor = lee(raiz, "Content/VFX/CalorLib.cs")
    ok_filtro = ('Filters.Scene[NombreFiltro] = _filtro;' in calor and
                 'new Filter(new ScreenShaderData(fx, "PaseCalor")' in calor and
                 "_filtro.IsHidden = true;" in calor and
                 'Filters.Scene.Activate(NombreFiltro' in calor and
                 'Filters.Scene.Deactivate(NombreFiltro' in calor)
    print(f"[{'OK' if ok_filtro else 'FALLO'}] {etiqueta}: EL FILTRO DE CADENA (registro + Activate/Deactivate + la defensa IsHidden del reload)")
    assert ok_filtro, f"el filtro de cadena no está completo en {etiqueta}"
    ok_gpu = ("_shaderFallo" in calor and "VFXCore.CalidadPermitida(VFXCore.CalidadFX.WarpPantalla)" in calor)
    print(f"[{'OK' if ok_gpu else 'FALLO'}] {etiqueta}: LA GPU-UNIVERSAL (bandera de fallo + la puerta WarpPantalla)")
    assert ok_gpu

    # (6d) v6.77 INTACTO — las esferas en quads + la fuente + el pegue
    ok_esf_h = ("VFXCore.Quad(Projectile.Center," in haz and
                "Main.spriteBatch.Draw(VFXCore.SoftGlow, Projectile.Center" not in haz)
    print(f"[{'OK' if ok_esf_h else 'FALLO'}] {etiqueta}: LA ESFERA del haz al PIPELINE DE QUADS (v6.77 INTACTA)")
    assert ok_esf_h
    ok_fuente = "1700 + i * 53" in haz
    print(f"[{'OK' if ok_fuente else 'FALLO'}] {etiqueta}: LA FUENTE DEL LANZAMIENTO (v6.76, INTACTA)")
    assert ok_fuente

    ojo = lee(raiz, "Content/Projectiles/OjoEternoMinion.cs")
    ok_pegue = ("deltaAncla" in ojo and "_anclaPrev" in ojo and "_anclaWho" in ojo)
    print(f"[{'OK' if ok_pegue else 'FALLO'}] {etiqueta}: EL PEGUE AL ANCLA (v6.77, INTACTO)")
    assert ok_pegue
    ok_giro = "const float omega = 0.50f;" in ojo
    assert ok_giro

    # (6e) LA ESCALERA + EL DAÑO + LA «o» + EL ROJO + DEVORADO + LA BOLSA
    desp = lee(raiz, "Content/Systems/DespertarGrimorio.cs")
    ok_cima = ("JefeDelCamino(45" in desp and "JefeDelCamino(50" in desp and
               "50 => NPC.downedMoonlord" in desp)
    assert ok_cima
    print(f"[OK] {etiqueta}: LA CIMA DE LA CAZA + el daño y el repertorio v6.77 (INTACTOS)")

    grim = lee(raiz, "Content/Weapons/GrimoireEternal.cs")
    ok_v77 = ("Item.damage = 7;" in grim and
              "con el arma o los lingotes basta" in grim and
              'colNo = "FF7B7B"' in grim)
    assert ok_v77
    for hjson, caido, viva in (
        ("es-MX_Mods.AethonMod.hjson", "DEVORADO", "Todavía VIVO"),
        ("es-ES_Mods.AethonMod.hjson", "DEVORADO", "Todavía VIVO"),
        ("en-US_Mods.AethonMod.hjson", "DEVOURED", "Still ALIVE")):
        h = lee(raiz, "Localization/" + hjson)
        assert caido in h and viva in h
    bolsas = lee(raiz, "Content/Items/Bolsas/BolsasCategorias.cs")
    assert "class BolsaDelCrecimiento" in bolsas
    print(f"[OK] {etiqueta}: LA LETRA v6.77 ENTERA (daño 7 · «o» minúscula · rojo unificado · DEVORADO · la bolsa)")

# (7) EL HJSON ×3
hj = sorted(p for p in files if p.endswith("Mods.AethonMod.hjson") and "/Escriba/" not in p)
assert len(hj) == 3, f"esperaba 3 espejos hjson, hay {len(hj)}"
for h in hj:
    nombre = os.path.basename(h)
    fuente = os.path.join(REPO, "Localization", nombre)
    assert os.path.exists(fuente), f"falta la fuente {nombre}"
    assert files[h] == open(fuente, 'rb').read(), f"{nombre} difiere de su fuente"
    print(f"[OK] {nombre} == fuente del repo ({len(files[h])} bytes)")

# (8) CADENA DE CUSTODIA — los editados Y los nuevos de v6.78
editados = [
    "Content/VFX/CalorLib.cs",                                   # NUEVO — la librería
    "Content/Effects/Shaders/CalorDistorsionShader.fxc",         # NUEVO — el shader
    "Content/Effects/Shaders/CalorDistorsionShader.fx",          # NUEVO — la fuente HLSL
    "Content/Effects/Procedural/RuidoPlasmaCalor.png",           # NUEVO — el ruido de plasma
    "Content/Projectiles/Definitivas/RayosPruebaProjectile.cs", # EDITADO — el registro del calor
    "Content/Effects/BlackHoleLensSystem.cs",                    # EDITADO — el hook único
    "build.txt",
]
for rel in editados:
    a = open(os.path.join(REPO, rel), 'rb').read()
    b = open(os.path.join(MSRC, rel), 'rb').read()
    assert a == b, f"repo != ModSources en {rel}"
    print(f"[OK] custodia: {os.path.basename(rel)} byte-idéntico")

# resumen md5
md5 = hashlib.md5(d).hexdigest()
print(f"\nmd5 {md5} | {fsize} B | v{ver}")
print("AUDITORÍA v6.78: TODO OK — LA LIBRERÍA DE CALOR (el aire que hierve + la aberración del estallido, en el filtro de cadena) · el hook único de la lente · TODO v6.77 INTACTO")
