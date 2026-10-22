#!/usr/bin/env python3
# v6.51.5 — CIRUGÍA DE LOCALIZACIÓN (la regla de la casa: JAMÁS el
# Edit/MultiEdit sobre hjson grandes — normaliza tabs y los destroza).
# La segunda audición: mueren las 5 armas feas (items + proyectiles)
# y nacen las 4 nuevas, en es-MX/es-ES/en-US.
import re, sys, os

LOC = os.path.join(os.path.dirname(__file__), "..", "AethonMod", "Localization")

# === LOS NUEVOS NOMBRES ===
NUEVOS = {
    "DignosIndiceGrimorio":  ("El Índice del Grimorio",  "The Grimoire Index"),
    "DignosLluviaVersos":    ("La Lluvia de Versos",     "The Rain of Verses"),
    "DignosOjoQueJuzga":     ("El Ojo que Juzga",        "The Judging Eye"),
    "DignosCorazonMagnetar": ("El Corazón del Magnetar", "The Heart of the Magnetar"),
}
NUEVOS_PROY = {
    "IndiceGrimorioProjectile":  "Indice Grimorio Projectile",
    "LluviaVersosProjectile":    "Lluvia Versos Projectile",
    "OjoQueJuzgaProjectile":     "Ojo Que Juzga Projectile",
    "CorazonMagnetarProjectile": "Corazon Magnetar Projectile",
}
MUERTOS_ITEM = ["DignosComasQueMuerden", "DignosPaginaArrancada",
                "DignosOjoQueCaza", "DignosLineaDeLectura", "DignosMarcapaginas"]
MUERTOS_PROY = ["AgujeroEnViajeProjectile", "ComaRunicaProjectile",
                "EquinoccioBinarioProjectile", "LineaLecturaProjectile",
                "MarcapaginasProjectile", "OjoQueCazaProjectile",
                "PaginaArrancadaProjectile"]

def cirugia(archivo, es_en):
    path = os.path.join(LOC, archivo)
    with open(path, encoding="utf-8") as f:
        lines = f.readlines()

    # ---------- 1 · EL BLOQUE DE ITEMS: las 5 muertas → las 4 nuevas ----------
    ini = fin = None
    for i, l in enumerate(lines):
        if re.match(r"\tDignosComasQueMuerden: \{", l):
            ini = i
        if ini is not None and re.match(r"\tBolsaProyectilesDignos: \{", l):
            fin = i
            break
    if ini is None or fin is None:
        print(f"!! {archivo}: no encontré el bloque de items"); sys.exit(1)

    # el estilo del archivo (¿tooltip vivo o comentado?)
    estilo = 'Tooltip: ""' if es_en else '// Tooltip: ""'
    # es-MX no lleva línea de tooltip (bloques de una línea+nombre)
    con_tooltip = any("Tooltip" in l for l in lines[ini:fin])

    nuevos_items = []
    for clave, (es, en) in NUEVOS.items():
        nombre = en if es_en else es
        nuevos_items.append(f"\t{clave}: {{\n")
        nuevos_items.append(f"\t\tDisplayName: {nombre}\n")
        if con_tooltip:
            nuevos_items.append(f"\t\t{estilo}\n")
        nuevos_items.append("\t}\n\n")

    lines[ini:fin] = nuevos_items
    print(f"  {archivo}: items {MUERTOS_ITEM} → {list(NUEVOS)}")

    # ---------- 2 · EL COMENTARIO DEL BLOQUE (los seis → la segunda audición) ----------
    for i, l in enumerate(lines):
        if "CANDIDATOS DIGNOS" in l or "WORTHY CANDIDATES" in l:
            # reescribir la línea de la versión
            lines[i] = l.replace("v6.51.4", "v6.51.4 → v6.51.5")
            break

    # ---------- 3 · EL BLOQUE DE PROJECTILES ----------
    out, borradas, agregadas = [], 0, 0
    for l in lines:
        borrar = False
        for muerto in MUERTOS_PROY:
            # línea viva o comentada — ambas mueren
            pat_viva = re.compile(r"^\t" + muerto + r"\.DisplayName:")
            pat_coment = re.compile(r"^\t// " + muerto + r"\.DisplayName:")
            if pat_viva.match(l) or pat_coment.match(l):
                borrar = True
                break
        if borrar:
            borradas += 1
            continue
        out.append(l)
        # insertar las nuevas después de DignosFXProjectile (viva o comentada)
        m = re.match(r"^(\t(?:// )?)DignosFXProjectile\.DisplayName:", l)
        if m and agregadas == 0:
            prefijo = m.group(1)  # "\t" o "\t// "
            # orden alfabético de la casa: Corazon, Indice, Lluvia, OjoQueJuzga
            for clave in ["CorazonMagnetarProjectile", "IndiceGrimorioProjectile",
                          "LluviaVersosProjectile", "OjoQueJuzgaProjectile"]:
                out.append(f"{prefijo}{clave}.DisplayName: {NUEVOS_PROY[clave]}\n")
            agregadas = 4
    lines = out
    print(f"  {archivo}: proyectiles borrados={borradas} agregados={agregadas}")

    with open(path, "w", encoding="utf-8") as f:
        f.writelines(lines)

cirugia("en-US_Mods.AethonMod.hjson", es_en=True)
cirugia("es-ES_Mods.AethonMod.hjson", es_en=False)
cirugia("es-MX_Mods.AethonMod.hjson", es_en=False)
print("CIRUGÍA COMPLETA")
