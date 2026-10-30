#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
purga_armas_v6566.py — LA PURGA DE LAS ARMAS NUEVAS en localización.

La letra del usuario: «borra todas las nuevas armas y solo deja al rayo
concentrado 2». Este script retira del es-ES (fuente única del español)
los BLOQUES enteros de los ítems muertos y las LÍNEAS de proyectiles
muertos del hjson del Escriba — tab-safe (lee y escribe líneas crudas,
sin tocar la indentación: la lección de la .56.5 — el editor de la casa
expande tabs).

También:
  · Añade Biomes.HollowSanctumBiome.BiomeName (la única clave automática
    faltante que la auditoría de la .56.6 encontró).
  · Actualiza el tooltip de BolsaDefinitivas (ya no hay escalera).
"""
import os
import sys

RAIZ = '/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod/Localization'
MAIN = os.path.join(RAIZ, 'es-ES_Mods.AethonMod.hjson')
ESCR = os.path.join(RAIZ, 'Escriba', 'es-ES_Mods.AethonMod.hjson')

ITEMS_MUERTOS = [
    'PrismaPrimordial', 'PartidorDeMundos', 'RequiemEstelar', 'IraDelCielo',
    'RayoDelAlba', 'RayoDelVeredicto', 'RayoDelJuicio', 'IndiceDelJuicio',
    'PlumaDelVeredicto', 'OjoDelJuicio',
]

PROY_MUERTOS = [
    'RayoConcentradoNivelProjectile', 'PrismaPrimordialProjectile',
    'PartidorDeMundosProjectile', 'RequiemEstelarProjectile',
    'IraDelCieloProjectile', 'ColumnaIraProjectile',
    'IndiceDelJuicioProjectile', 'PaginaIndiceProjectile',
    'PlumaDelVeredictoProjectile', 'LanzaVeredictoProjectile',
    'OjoDelJuicioProjectile',
]


def quitar_bloques(lineas, nombres):
    """Elimina bloques `Nombre: { ... }` (equilibrando llaves) de una lista
    de líneas crudas. Devuelve (nuevas_lineas, quitados)."""
    out = []
    i = 0
    quitados = []
    n = len(lineas)
    while i < n:
        ln = lineas[i]
        s = ln.strip()
        match = None
        for nom in nombres:
            if s == f'{nom}: {{':
                match = nom
                break
        if match:
            # consumir hasta la llave que cierra el bloque a profundidad 1
            prof = 0
            j = i
            while j < n:
                sj = lineas[j]
                prof += sj.count('{') - sj.count('}')
                j += 1
                if prof == 0:
                    break
            quitados.append((match, i + 1, j))
            i = j
            # tragar una línea en blanco posterior si la hay (no doble)
            if i < n and lineas[i].strip() == '' and (not out or out[-1].strip() != ''):
                i += 1
            continue
        out.append(ln)
        i += 1
    return out, quitados


def main():
    # ---- 1) el hjson principal: bloques de ítems ----
    with open(MAIN, 'r', encoding='utf-8', newline='') as f:
        lineas = f.readlines()
    nuevas, quitados = quitar_bloques(lineas, ITEMS_MUERTOS)
    print(f'MAIN: {len(quitados)} bloques quitados:')
    for nom, a, b in quitados:
        print(f'   · {nom}  (líneas {a}-{b})')
    faltan = set(ITEMS_MUERTOS) - {q[0] for q in quitados}
    if faltan:
        print(f'   !!! NO ENCONTRADOS: {faltan}')
        sys.exit(1)

    # ---- 2) el tooltip de la BolsaDefinitivas y la bolsa maestra ----
    texto = ''.join(nuevas)
    viejo_bolsa = None
    for ln in nuevas:
        if 'LA ESCALERA DEL RAYO CONCENTRADO: el Alba' in ln:
            viejo_bolsa = True
    if viejo_bolsa:
        # reemplazo del tooltip multilínea de BolsaDefinitivas
        nuevo_tooltip = (
            "\t\t\t'''\n"
            "\t\t\tEL RAYO CONCENTRADO 2 — el ataque definitivo de la casa\n"
            "\t\t\ty sus NUEVE VARIANTES VFX (cada una con su técnica y su\n"
            "\t\t\tcarácter: prisma, filo, eclipse, tormenta, vórtice…).\n"
            "\t\t\tEl plasma de la casa (carne fBm anti-picos) y los COLORES\n"
            "\t\t\tDEL JUICIO siguen siendo ley.\n"
            "\t\t\t'''\n"
        )
        import re
        patron = re.compile(
            r"(\tBolsaDefinitivas: \{\n(?:.*\n)*?\t\tTooltip:\n)(\t\t\t'''\n(?:.*?\n)*?\t\t\t'''\n)",
        )
        m = patron.search(texto)
        if m:
            texto = texto[:m.start(2)] + nuevo_tooltip + texto[m.end(2):]
            print('MAIN: tooltip de BolsaDefinitivas actualizado')
        else:
            print('MAIN: !!! tooltip de BolsaDefinitivas NO hallado — patrón')
    with open(MAIN, 'w', encoding='utf-8', newline='') as f:
        f.write(texto)

    # ---- 3) el hjson del Escriba: líneas de proyectiles ----
    with open(ESCR, 'r', encoding='utf-8', newline='') as f:
        lineas_e = f.readlines()
    vivos = []
    quitados_e = 0
    for ln in lineas_e:
        s = ln.strip()
        muerta = False
        for p in PROY_MUERTOS:
            if s.startswith(f'{p}.DisplayName:'):
                muerta = True
                break
        if muerta:
            quitados_e += 1
            continue
        vivos.append(ln)
    print(f'ESCRIBA: {quitados_e} líneas de proyectiles quitadas')

    # ---- 4) AÑADIR la clave del bioma (la única automática faltante) ----
    # La añade dentro del bloque Biomes: { ... } del hjson principal si
    # existe; si no, la crea al final del archivo.
    with open(MAIN, 'r', encoding='utf-8', newline='') as f:
        lineas = f.readlines()
    if any(l.strip().startswith('HollowSanctumBiome:') for l in lineas):
        print('MAIN: HollowSanctumBiome ya existe — nada que hacer')
    else:
        # busca el bloque 'Biomes: {' y si no existe, crea uno al final
        idx = None
        for k, ln in enumerate(lineas):
            if ln.rstrip() == 'Biomes: {':
                idx = k
                break
        if idx is not None:
            # insertar justo antes del cierre del bloque
            prof = 0
            j = idx
            while j < len(lineas):
                prof += lineas[j].count('{') - lineas[j].count('}')
                j += 1
                if prof == 0:
                    break
            lineas.insert(j - 1, '\tHollowSanctumBiome.BiomeName: El Santuario Hueco\n')
            print('MAIN: HollowSanctumBiome.BiomeName añadida al bloque Biomes')
        else:
            lineas.append('\nBiomes: {\n\tHollowSanctumBiome.BiomeName: El Santuario Hueco\n}\n')
            print('MAIN: bloque Biomes creado con HollowSanctumBiome')
        with open(MAIN, 'w', encoding='utf-8', newline='') as f:
            f.writelines(lineas)

    with open(ESCR, 'w', encoding='utf-8', newline='') as f:
        f.writelines(vivos)
    print('OK')


if __name__ == '__main__':
    main()
