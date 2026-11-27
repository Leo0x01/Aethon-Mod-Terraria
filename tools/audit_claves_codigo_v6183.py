#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
v6.183 — EL GUARDIÁN DE LAS LLAVES (la letra: «hay texto en crudo...
analiza que sucedio con eso por que hay tanto texto en crudo y coloca
lo que debe ir en esos textos»).

LA CAUSA (la autopsia de la v6.183): el mod arma MUCHAS claves de
localización CONCATENANDO ("Categoría." + clave). El Guardian de
texturas vigila las IMÁGENES; NADIE vigilaba que TODA clave que el
código ARMA exista de verdad en los 3 hjson. Una clave usada y viva
en NINGÚN idioma = EL JUEGO PINTA LA CLAVE CRUDA («ChatSellado0»,
«Items.TrioMarcaPuesta» — la casilla que vive en Altar).

ESTE GATE (corre en cada release, junto al Guardian):
  1. DIRECTAS: todo literal "Mods.AethonMod.X.Y" completo que el
     código escribe debe vivir en los 3 idiomas.
  2. HELPERS: cada helper que arma "Prefijo." + clave se declara en la
     tabla de abajo; TODAS sus llamadas con literal se resuelven y se
     verifican en los 3 idiomas.
  3. NUMÉRICAS/DINÁMICAS: cada base concatenada («ChatSellado» + (n-14))
     se lista con las claves que existen — verificación manual de
     rangos (el gate exige: la base existe en los 3 y hay >0 sufijos).
  4. PARIDAD: los 3 hjson tienen EXACTAMENTE las mismas claves.

Sale con código 1 si algo falta — NADA se publica con texto en crudo.
"""
import os
import re
import sys

import hjson

RAIZ = os.path.dirname(os.path.abspath(__file__))  # .../mod/tools
MOD = os.path.join(RAIZ, '..', 'AethonMod')
CULTURAS = ['es-MX', 'es-ES', 'en-US']

# ── LA TABLA DE HELPERS: (archivo, método, prefijo de categoría) ──
# Todo helper NUEVO que arme claves se declara aquí o el gate no lo ve
# (y el assert de abajo muere si el prefijo desaparece del archivo).
HELPERS = [
    ('Content/UI/AltarOfrendasUI.cs', 'Decir',     'Mods.AethonMod.Altar.'),
    ('Content/UI/AltarOfrendasUI.cs', 'DecirItem', 'Mods.AethonMod.Items.'),
    ('Content/UI/ArbolHabilidadesUI.cs', 'Clave', 'Mods.AethonMod.Arbol.'),
    ('Content/UI/CieloDespiertoUI.cs', 'Clave',   'Mods.AethonMod.Despertar.'),
    ('Content/UI/LibroDelEternoUI.cs', 'T',       'Mods.AethonMod.Pergamino.'),
    ('Content/UI/LibroDelEternoUI.cs', 'At',      'Mods.AethonMod.Altar.'),
]

# Prefijos que el código concatena SIN helper (base + expr) — se
# reportan con sus sufijos vivos para verificación de rangos.
BASES_DYNAMICAS = [
    'Mods.AethonMod.Cronista.ChatGenerica',
    'Mods.AethonMod.Cronista.ChatPortador',
    'Mods.AethonMod.Cronista.ChatOleadas',
    'Mods.AethonMod.Cronista.ChatSellado',
    'Mods.AethonMod.Cronista.ChatObjetos',
    'Mods.AethonMod.Cronista.ChatCierre',
    'Mods.AethonMod.Cronista.LoreHabla',
    'Mods.AethonMod.PilarInterior.IraSellado',
    'Mods.AethonMod.Items.TrioAlbedrio',
    'Mods.AethonMod.Armas.EscuadronMejora',
    'Mods.AethonMod.Hambre.',
    'Mods.AethonMod.Hambre.Nervioso.',
    'Mods.AethonMod.Eco.VozJefe.',
    'Mods.AethonMod.Eco.Bioma.',
    'Mods.AethonMod.Eco.Celos.',
    'Mods.AethonMod.Eco.Hambre.Susurro',
    'Mods.AethonMod.Duende.Burla',
    'Mods.AethonMod.Duende.Miedo',
    'Mods.AethonMod.Esencia.Sabor.',
]


def aplanar(d, p=''):
    out = set()
    for k, v in d.items():
        kk = f'{p}{k}'
        if isinstance(v, dict):
            out |= aplanar(v, kk + '.')
        else:
            out.add(kk)
    return out


def claves_hjson(cultura):
    ruta = os.path.join(MOD, 'Localization',
                        f'{cultura}_Mods.AethonMod.hjson')
    with open(ruta, encoding='utf-8') as f:
        # el ARCHIVO es el namespace «Mods.AethonMod.» — se aplanan las
        # claves YA PREFIJADAS para comparar 1:1 contra el código
        pref = 'Mods.AethonMod.'
        return {pref + k for k in aplanar(hjson.load(f))}


def archivos_cs():
    for dirpath, _, archivos in os.walk(os.path.join(MOD, 'Content')):
        for a in sorted(archivos):
            if a.endswith('.cs'):
                yield os.path.join(dirpath, a)


# Bases que viajan por VARIABLE a HablarVariante* (la regex solo ve
# literales — la 148 de EcoSistema: claveBase = «…Desconocido»). La
# base sola NUNCA se pide: viven sus +1..+3.
BASES_SOLO_VARIANTES = {
    'Mods.AethonMod.Eco.VozJefe.Desconocido',
}


def bases_variantes():
    """LA CONVENCIÓN DE ECOLIB: ElegirClave(clave, n) arma clave+1..clave+n
    (la BASE sola NUNCA se pide). Toda clave pasada a un Hablar*/Elegir*
    de variantes necesita sus 1..n, no ella misma."""
    llamadas = re.compile(
        r'(?:HablarVariante\w*|ElegirVariante|ElegirClave)\([^;]{0,200}?'
        r'"(Mods\.AethonMod\.[A-Za-z0-9_.]+)"\s*,\s*(\d+)')
    bases = {}
    for ruta in archivos_cs():
        with open(ruta, encoding='utf-8', errors='replace') as f:
            texto = f.read()
        for m in llamadas.finditer(texto):
            bases[m.group(1)] = int(m.group(2))
    return bases


def main():
    vivas = {c: claves_hjson(c) for c in CULTURAS}
    fallos = []
    avisos = []
    variantes = bases_variantes()

    # ── 4. PARIDAD ──────────────────────────────────────────────
    # es-MX == es-ES EXACTO (fatal). en-US: lo que FALTA es fatal;
    # lo que SOBRA en en-US son los fósiles autogenerados por el
    # build de tML (Projectiles.*.DisplayName / Items.*.Tooltip
    # con valor por defecto) — INFO, no crudo para nadie.
    mx, es, us = (vivas[c] for c in CULTURAS)
    for k in sorted(mx - es):
        fallos.append(f'PARIDAD es-ES sin clave: {k}')
    for k in sorted(es - mx):
        fallos.append(f'PARIDAD es-MX sin clave: {k}')
    for k in sorted((mx | es) - us):
        fallos.append(f'PARIDAD en-US sin clave (fallback roto): {k}')
    fossil = sorted(us - mx - es)
    if fossil:
        malos = [k for k in fossil
                 if not re.search(r'\.(DisplayName|Tooltip)$', k)]
        if malos:
            for k in malos:
                fallos.append(f'PARIDAD en-US fósil raro: {k}')
        avisos.append(f'INFO en-US fósiles autogenerados por el build: '
                      f'{len(fossil)} (todos .DisplayName/.Tooltip)')

    todas = mx | es | us

    # ── 1. DIRECTAS ─────────────────────────────────────────────
    directo = re.compile(r'"(Mods\.AethonMod\.[A-Za-z0-9_]+(?:\.[A-Za-z0-9_]+)+)"')
    concatenando = re.compile(r'"Mods\.AethonMod\.[A-Za-z0-9_.]*"\s*\+')
    for ruta in archivos_cs():
        with open(ruta, encoding='utf-8', errors='replace') as f:
            texto = f.read()
        rel = os.path.relpath(ruta, MOD)
        for m in directo.finditer(texto):
            clave = m.group(1)
            # las que siguen concatenándose van en la fase dinámica
            tramo = texto[m.start():m.start() + len(clave) + 8]
            if concatenando.match(tramo):
                continue
            if clave in variantes or clave in BASES_SOLO_VARIANTES:
                continue   # base de variantes: la pide clave+N, no sola
            if clave not in todas:
                fallos.append(f'DIRECTA muerta: {clave} ← {rel}')

    # ── 1b. LAS VARIANTES DE ECOLIB: base+N → las N deben vivir ──
    for base, n in variantes.items():
        for i in range(1, n + 1):
            if f'{base}{i}' not in todas:
                fallos.append(f'VARIANTE muerta: {base}{i} (de {n})')

    # ── 2. HELPERS ──────────────────────────────────────────────
    for arch, metodo, prefijo in HELPERS:
        ruta = os.path.join(MOD, arch)
        assert os.path.exists(ruta), f'GUARDIÁN DE LAS LLAVES: {arch} no existe'
        with open(ruta, encoding='utf-8', errors='replace') as f:
            texto = f.read()
        # el prefijo DEBE seguir vivo en el archivo (assert de la casa)
        assert f'"{prefijo[:-1]}"' in texto or f'"{prefijo}' in texto, \
            f'GUARDIÁN DE LAS LLAVES: {metodo} perdió su prefijo {prefijo}'
        # LA LLAMADA COMPLETA (hasta su cierre): los ternarios
        # (ahora ? "Puesta" : "Quitada") también cuentan — la lección
        # de la v6.183: «Items.TrioMarcaPuesta» crudo en el chat.
        llama = re.compile(re.escape(metodo) + r'\([^;]{0,500}')
        ident = re.compile(r'"([A-Za-z0-9_]+)"')
        for m in llama.finditer(texto):
            llamada = m.group(0)
            for lit in ident.finditer(llamada):
                sufijo = lit.group(1)
                despues = llamada[lit.end():lit.end() + 3]
                if despues.strip().startswith('+'):
                    # base dinámica («Nodo» + n): exige Nodo<digito> vivos
                    vivos_base = [k for k in todas
                                   if k.startswith(prefijo + sufijo)
                                   and k[len(prefijo) + len(sufijo):].isdigit()]
                    if not vivos_base:
                        fallos.append(f'HELPER base dinámica sin variantes: '
                                      f'{prefijo}{sufijo}+n ← {metodo}() en {arch}')
                    continue
                clave = prefijo + sufijo
                if clave in variantes:
                    continue
                if clave not in todas:
                    fallos.append(f'HELPER muerta: {clave} '
                                  f'← {metodo}() en {arch}')

    # ── 3. BASES NUMÉRICAS/DINÁMICAS ────────────────────────────
    for base in BASES_DYNAMICAS:
        if base.endswith('.'):
            sufijos = sorted(k[len(base):] for k in todas
                             if k.startswith(base))
            if not sufijos:
                fallos.append(f'DINÁMICA sin claves: {base}*')
            else:
                avisos.append(f'DINÁMICA {base}* → vivas: {sufijos[:12]}')
        else:
            vivas_base = sorted(k for k in todas if k.startswith(base)
                                and k != base)
            if not vivas_base:
                fallos.append(f'NUMÉRICA sin variantes: {base}+n')
            else:
                sufs = [k[len(base):] for k in vivas_base]
                avisos.append(f'NUMÉRICA {base}+n → vivos: {sufs[:10]}')

    # ── INFORME ─────────────────────────────────────────────────
    for a in avisos:
        print(f'  · {a}')
    if fallos:
        print(f'\nGUARDIÁN DE LAS LLAVES: {len(fallos)} CLAVE(S) CRUDA(S):')
        for f_ in fallos:
            print(f'  ✗ {f_}')
        sys.exit(1)
    print(f'GUARDIÁN DE LAS LLAVES: TODO VIVE ✔ '
          f'({len(mx)} claves × {len(CULTURAS)} idiomas, '
          f'{len(HELPERS)} helpers, {len(BASES_DYNAMICAS)} bases dinámicas)')


if __name__ == '__main__':
    main()
