#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""v6.80 — AUDITORÍA DEL .tmod: LA GRAN LIMPIEZA ejecutada y verificada.
  (0) v6.80 — los dos bugs del Grupo B curados + la fusión nº1 + la purga + los techos.
  (1) tabla + EOF exacto (blobs íntegros, zlib OK, cuenta viva: 477)
  (2) LOS SONIDOS intactos y RunaCompas SEGUIR ausente
  (3) LA TEXTURA DEL CIELO + LAS 15 DEL CRECIMIENTO DENTRO
  (4) EL CALOR INTACTO: .fxc BYTE-IDÉNTICO + RuidoPlasmaCalor COMPLETO
  (5) LA DLL EXTRAÍDA — LO NUEVO VIVO (PerfilSupremo · GradienteEnTodo ·
      Diag.Hitstop) y LOS PURGADOS AUSENTES (SelloVacio · GradienteAlba ·
      DeltaAngle · PlanoLiangBarsky · SeedCell · PuntoEnCamino ·
      VeredictoDe · CerrarVeredictosVencidos · ConRadio/ConPatron/
      ConLados/ConHumo · CapCeldas) + TODO el repertorio v6.77-79 VIVO.
  (6) EN LAS FUENTES — LOS CINCO FRENTES v6.80 (repo Y ModSources):
      (a) EL VELO DE TINTE (PulsoSistema lo DIBUJA — la cura del flash invisible)
      (b) LA CONVERGENCIA (Golpe por Pantalla.Sacudir con calibración exacta; OndaLib.Kick AUSENTE de ImpactoLib)
      (c) LA FUSIÓN (el shim del Aurora delgado; el core parametrizado; GradienteEnTodo ×4)
      (d) LOS TECHOS (≥50 Presupuesto() en Content/VFX) + LOS 6 EMPUJES INTACTOS
      (e) EL F8 (Diag.Hitstop en el sistema + la clave en los 3 hjson)
  (7) EL HJSON: la clave Hitstop en los TRES idiomas.
  (8) CADENA DE CUSTODIA: repo == ModSources en los editados.
"""
import sys, zlib, os, subprocess

def cuenta(raiz, patron, subdir="Content"):
    """Cuenta matches de rg en un árbol (0 si rg no encuentra nada)."""
    r = subprocess.run(["rg", "-c", "--no-filename", patron, os.path.join(raiz, subdir)],
                       capture_output=True, text=True)
    lineas = [x for x in r.stdout.strip().splitlines() if x.strip()]
    return sum(int(x) for x in lineas)


sys.path.insert(0, '/home/z/.tmp-ml')
from parse_tmod import parse

TMOD = "/home/z/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod"
REPO = "/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod"
MSRC = "/home/z/.local/share/Terraria/tModLoader/ModSources/AethonMod"

tmlver, name, ver, entries, tend, fsize = parse(TMOD)
print(f"tML {tmlver} | {name} v{ver} | {len(entries)} entradas | tabla fin {tend} | archivo {fsize}")
assert str(ver) == "6.80", f"versión {ver} != 6.80"
assert len(entries) == 477, f"esperaba 477 entradas, hay {len(entries)}"

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
assert k, "falta OjoDelCieloItem"
print(f"[OK] {k[0]} DENTRO ({len(files[k[0]])} bytes)")
nuevas = sorted(set([p for p, _, _ in entries if 'Crecimiento' in p and p.endswith('.rawimg')]))
assert len(nuevas) == 15, f"esperaba 15 rawimg del crecimiento, hay {len(nuevas)}"
print(f"[OK] LAS 15 rawimg DEL CRECIMIENTO DENTRO: {len(nuevas)}")

# (4) EL CALOR INTACTO
FXC = "Content/Effects/Shaders/CalorDistorsionShader.fxc"
assert FXC in files, "falta el CalorDistorsionShader.fxc"
fxc_repo = open(os.path.join(REPO, "Content/Effects/Shaders/CalorDistorsionShader.fxc"), 'rb').read()
assert files[FXC] == fxc_repo, "el .fxc del .tmod NO es byte-idéntico al repo"
print(f"[OK] CalorDistorsionShader.fxc BYTE-IDÉNTICO ({len(fxc_repo)} B)")
RUIDO = "Content/Effects/Procedural/RuidoPlasmaCalor.rawimg"
assert RUIDO in files and len(files[RUIDO]) == 512 * 512 * 4 + 12
print(f"[OK] RuidoPlasmaCalor.rawimg COMPLETO ({len(files[RUIDO])} B)")

# (5) LA DLL — lo NUEVO vivo, lo purgado ausente
dll = files.get("AethonMod.dll")
pdb = files.get("AethonMod.pdb")
assert dll and pdb
def tiene(buf, s):
    return ((s.encode('utf-16-le') + b'\x00\x00') in buf or
            s.encode('utf-16-le') in buf or
            s.encode('utf-8') in buf)

nuevas_v680 = ["PerfilSupremo", "GradienteEnTodo", "GradSplit", "TendrilImpar",
               "NebImpar", "RuneInBody", "AuraOscura", "Polvo1", "JetDownHot",
               "Diag.Hitstop", "CongeladoRestante"]
for s in nuevas_v680:
    ok = tiene(dll, s)
    print(f"[{'OK' if ok else 'FALLO'}] firma VIVA (v6.80): {s}")
    assert ok, f"falta {s} en la DLL"

purgados_v680 = ["SelloVacio", "GradienteAlba", "DeltaAngle", "PlanoLiangBarsky",
                 "SeedCell", "PuntoEnCamino", "VeredictoDe", "CerrarVeredictosVencidos",
                 "ConRadio", "ConPatron", "ConLados", "ConHumo", "CapCeldas",
                 "RayitosDelCorazon", "enRitualIra", "MicroPoder", "DanoEscuadronMult",
                 "TonosDeIris"]
for s in purgados_v680:
    mal = tiene(dll, s)
    print(f"[{'OK' if not mal else 'FALLO'}] purgado AUSENTE de la DLL: {s}")
    assert not mal, f"{s} sigue VIVO en la DLL"

# el repertorio vivo de las v6.77-79 (muestra dura)
vivas = ["EmpujarPantalla", "TintePantalla", "ActualizarPantalla", "LatidoTrauma",
         "GolpearCamara", "SupremoAuroraBlackHoleRenderer", "SupremoBlackHoleRenderer",
         "CrecimientoEstallidoProjectile", "BolsaDelCrecimiento", "GradienteVeredicto",
         "CalorLib", "PrepararPaseDeCalor", "AethonCalor", "Chaikin", "BuildTree",
         "DibujarDivino", "CirculoRunico", "FajaTexturadaEnLote"]
for s in vivas:
    ok = tiene(dll, s)
    print(f"[{'OK' if ok else 'FALLO'}] firma VIVA (repertorio): {s}")
    assert ok, f"falta {s}"

# (6) EN LAS FUENTES (repo Y ModSources)
def lee(raiz, rel):
    return open(os.path.join(raiz, rel), encoding='utf-8').read()

for raiz, etiqueta in ((REPO, "repo"), (MSRC, "ModSources")):
    # (6a) EL VELO DE TINTE — la cura del flash invisible (bug nº1 del Grupo B)
    sis = lee(raiz, "Content/VFX/PulsoSistema.cs")
    ok_velo = ("PostDrawInterface" in sis and
               "PulsoLib.FuerzaPantalla" in sis and
               "PulsoLib.TintePantalla" in sis and
               "new Rectangle(0, 0, Main.screenWidth, Main.screenHeight)" in sis and
               "CalidadPermitida(VFXCore.CalidadFX.PostProceso)" in sis and
               "Main.UIScaleMatrix" in sis)
    print(f"[{'OK' if ok_velo else 'FALLO'}] {etiqueta}: EL VELO DE TINTE (PulsoSistema lo DIBUJA — quad plano + puerta PostProceso + el baile de lote)")
    assert ok_velo, f"el velo no está completo en {etiqueta}"

    # (6b) LA CONVERGENCIA — Golpe por el dueño del shake, calibrado
    imp = lee(raiz, "Content/VFX/ImpactoLib.cs")
    ok_conv = ('Pantalla.Sacudir(n, (8f + 6f * i) / 60f, "ImpactoLib")' in imp and
               "Math.Cbrt(100f * picoKickViejo)" in imp and
               "OndaLib.Kick(" not in imp)   # sin LLAMADA (la mención en la doc del header sí vive)
    print(f"[{'OK' if ok_conv else 'FALLO'}] {etiqueta}: LA CONVERGENCIA (Sacudir calibrado por ∛; Kick AUSENTE de ImpactoLib)")
    assert ok_conv, f"la convergencia no está en {etiqueta}"

    # (6c) LA FUSIÓN — el shim delgado y el core parametrizado
    shim = lee(raiz, "Content/VFX/SupremoAuroraBlackHoleRenderer.cs")
    core = lee(raiz, "Content/VFX/SupremoBlackHoleRenderer.cs")
    nl_shim = shim.count('\n')
    ok_fusion = (nl_shim <= 60 and
                 "SupremoBlackHoleRenderer.Draw(center, scale, time, seed," in shim and
                 "SupremoBlackHoleRenderer.Aurora" in shim and
                 core.count("GradienteEnTodo") >= 5 and   # campo + 4 bloques de estrategia
                 "public readonly struct PerfilSupremo" in core and
                 core.count("if (!VFXCore.Presupuesto(") >= 3)
    print(f"[{'OK' if ok_fusion else 'FALLO'}] {etiqueta}: LA FUSIÓN (shim {nl_shim} l + core con PerfilSupremo + {core.count('GradienteEnTodo')} firmas de estrategia)")
    assert ok_fusion, f"la fusión no está completa en {etiqueta}"

    # (6d) LOS TECHOS + LOS 6 EMPUJES INTACTOS
    total_techos = cuenta(raiz, r"if \(!VFXCore\.Presupuesto\(", "Content/VFX")
    ok_techos = total_techos >= 50
    print(f"[{'OK' if ok_techos else 'FALLO'}] {etiqueta}: LOS TECHOS ({total_techos} Presupuesto() en Content/VFX — antes 13)")
    assert ok_techos, f"techos insuficientes en {etiqueta}: {total_techos}"

    empujes = cuenta(raiz, r"PulsoLib\.EmpujarPantalla\(")
    ok_emp = empujes == 6
    print(f"[{'OK' if ok_emp else 'FALLO'}] {etiqueta}: LOS 6 EMPUJES INTACTOS ({empujes} — las 5 armas no se tocaron)")
    assert ok_emp, f"empujes = {empujes}, esperaba 6 en {etiqueta}"

    # (6e) EL F8
    diag = lee(raiz, "Content/Systems/DiagnosticoVFXSystem.cs")
    ok_f8 = "Diag.Hitstop" in diag and "CongeladoRestante" in diag
    print(f"[{'OK' if ok_f8 else 'FALLO'}] {etiqueta}: EL F8 GANA EL HITSTOP (el doc-rot curado)")
    assert ok_f8

# (7) EL HJSON — la clave Hitstop en los TRES idiomas
for loc in ["es-MX", "es-ES", "en-US"]:
    h = lee(REPO, f"Localization/{loc}_Mods.AethonMod.hjson")
    ok = "Hitstop:" in h
    print(f"[{'OK' if ok else 'FALLO'}] hjson {loc}: la clave Hitstop DENTRO")
    assert ok, f"falta Hitstop en {loc}"

# (8) CADENA DE CUSTODIA — repo == ModSources en los 12 tocados de verdad
tocados = ["Content/VFX/PulsoSistema.cs", "Content/VFX/PulsoLib.cs",
           "Content/VFX/ImpactoLib.cs", "Content/VFX/PantallaLib.cs",
           "Content/VFX/SupremoBlackHoleRenderer.cs",
           "Content/VFX/SupremoAuroraBlackHoleRenderer.cs",
           "Content/VFX/StormLib.cs", "Content/VFX/PyraLib.cs",
           "Content/Systems/DiagnosticoVFXSystem.cs", "build.txt"]
for rel in tocados:
    a = open(os.path.join(REPO, rel), 'rb').read()
    b = open(os.path.join(MSRC, rel), 'rb').read()
    assert a == b, f"CUSTODIA ROTA: {rel} difiere entre repo y ModSources"
print(f"[OK] CUSTODIA: repo == ModSources en {len(tocados)} archivos clave")

print("\n=== AUDITORÍA v6.80: TODO OK — LA GRAN LIMPIEZA VERIFICADA ===")
