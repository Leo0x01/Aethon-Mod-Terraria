#!/usr/bin/env python3
"""v6.56.3 — LA CURA DEL MURO DE MANÁ.

El reporte del usuario: «el rayo concentrado 2 no funciona» y «el ojo del
juicio no funciona» — los DOS únicos con mana = 100 (los 3 rayos que SÍ
lanzó cuestan 40/60/80). El personaje nuevo tenía <100 de maná: el muro
invisible de vanilla (el ítem no se usa, sin sonido, sin feedback).

LA CURA: TODO el arsenal definitivo a mana = 10 — la convención endgame
de vanilla (el Cénit cuesta 0, el Prisma Final 10): a esas alturas el
maná es ruido; el useTime (30-40 t) ya parea el arma.
"""
import re, sys

BASE = "/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod/Content/Weapons/Definitivas/"
ARCHIVOS = [
    "RayosConcentrados.cs",        # Alba 40 / Veredicto 60 / Juicio 80
    "RayoConcentradoDos.cs",       # RC2 100 ← el reportado
    "DefinitivasNuevasV6560.cs",   # Índice 60 / Ojo 100 ← el reportado / Pluma 70
    "DefinitivasItemsA.cs",        # Prisma 14 / Partidor 80
    "DefinitivasItemsB.cs",        # Réquiem 60 / Ira 75
]

total = 0
for nombre in ARCHIVOS:
    ruta = BASE + nombre
    with open(ruta, "r", encoding="utf-8") as f:
        src = f.read()
    # casa: Item.mana = N;  y  ((ModItem)this).Item.mana = N;
    nuevo, n = re.subn(
        r"((?:\(\(ModItem\)this\)\.)?Item\.mana = )\d+(f?;)",
        r"\g<1>10\g<2>",
        src,
    )
    if n:
        # C# int literal — quitar la 'f' si la hubiera dejado
        nuevo = nuevo.replace("Item.mana = 10f;", "Item.mana = 10;")
        with open(ruta, "w", encoding="utf-8", newline="") as f:
            f.write(nuevo)
    print(f"{nombre}: {n} sitio(s)")
    total += n

print(f"TOTAL: {total} muros de maná derribados")
sys.exit(0 if total >= 10 else 1)
