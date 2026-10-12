#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
regen_oracle.py — REGENERADOR DEL ORÁCLE /home/z/.verify (v6.50.113+).

La casa: /home/z/.verify/verify.csproj compila TODOS los .cs del mod
contra TODAS las DLLs de /tmp/tml/Libraries (MENOS tModCodeAssist y
Tests — tModCodeAssist choca con tModLoader: CS0433). El oráculo debe
dar 0 errores / 0 warnings sobre el árbol LIMPIO antes de tocar nada.

Uso: python3 tools/regen_oracle.py   (requiere /tmp/tml extraído y
/tmp/sdk; luego: cd /home/z/.verify && DOTNET_ROOT=/tmp/sdk
DOTNET_ROLL_FORWARD=LatestMajor /tmp/sdk/dotnet build -v q)
"""
import glob
import os

RAIZ_MOD = os.path.join(os.path.dirname(__file__), '..', 'AethonMod')
TML = '/tmp/tml'
VERIFY = '/home/z/.verify'

def refs():
    """TODAS las DLLs gestionadas de tML (la lección de la casa:
    Libraries/** además de /tmp/tml/tModLoader.dll — el código de
    Terraria VIVE ahí en 2026.x) MENOS tModCodeAssist/tModPorter/Tests
    (CS0433 con tModLoader) y Native/ (nativos de plataforma, no
    gestionados). Dedupe por nombre de referencia."""
    lib = os.path.join(TML, 'Libraries')
    excl = ('tModCodeAssist', 'tModPorter', 'Tests')
    out = []
    vistos = set()
    # /tmp/tml/tModLoader.dll PRIMERO (el rey: Terraria + ModLoader)
    tml_core = os.path.join(TML, 'tModLoader.dll')
    if os.path.exists(tml_core):
        out.append(('tModLoader', tml_core))
        vistos.add('tModLoader')
    for dll in sorted(glob.glob(os.path.join(lib, '**', '*.dll'), recursive=True)):
        rel = os.path.relpath(dll, lib)
        # nativos de plataforma (Native/) y runtime-packs (runtimes/…)
        if rel.split(os.sep)[0] == 'Native' or 'runtimes' in rel.split(os.sep):
            continue
        nombre = os.path.splitext(os.path.basename(dll))[0]
        if nombre in excl or nombre in vistos:
            continue
        out.append((nombre, dll))
        vistos.add(nombre)
    return out

def main():
    os.makedirs(VERIFY, exist_ok=True)
    cs = sorted(glob.glob(os.path.join(RAIZ_MOD, '**', '*.cs'), recursive=True))
    referencias = refs()
    print(f'  {len(cs)} .cs · {len(referencias)} referencias')

    csproj = ['<Project Sdk="Microsoft.NET.Sdk">',
              '  <PropertyGroup>',
              '    <TargetFramework>net8.0</TargetFramework>',
              '    <LangVersion>latest</LangVersion>',
              '    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>',
              '    <GenerateDependencyFile>false</GenerateDependencyFile>',
              '    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>',
              '    <NoWarn></NoWarn>',
              '  </PropertyGroup>',
              '  <ItemGroup>']
    for c in cs:
        csproj.append(f'    <Compile Include="{c}" />')
    csproj.append('  </ItemGroup>')
    csproj.append('  <ItemGroup>')
    for nombre, dll in referencias:
        csproj.append(f'    <Reference Include="{nombre}">'
                      f'<HintPath>{dll}</HintPath>'
                      f'<Private>false</Private></Reference>')
    csproj.append('  </ItemGroup>')
    csproj.append('</Project>')
    with open(os.path.join(VERIFY, 'verify.csproj'), 'w') as f:
        f.write('\n'.join(csproj) + '\n')
    print(f'  verify.csproj OK -> {VERIFY}')

if __name__ == '__main__':
    print('REGEN ORACLE:')
    main()
