#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
v650128_hjson.py — LAS CLAVES NUEVAS DE v6.50.128 en los 3 idiomas
(es-MX primario, es-ES espejo, en-US).

Añade (por archivo, con su indentación EXACTA — espacios, como el resto):
  · Caida.ItemCorrompido        — el aviso del haz al corromper
  · Caida.DevoradoSabor1..4     — la VOZ del Eterno según el bocado
  · Caida.DevoradoCuenta        — el aviso de las estadísticas
  · Caida.DevoradoTooltip       — la línea del tooltip del Eterno
  · Caida.DevoradoTooltipOjos   — el sufijo de ojos extra
  · Items.ItemCorrupto.*        — la esencia corrupta (nombre + tooltip)
  · Tiles.AltarDeLaCaida.MapEntry — la primera tile del mod en el mapa

Idempotente: si la clave ya existe, no la duplica.
"""
import re, sys

BASE = "/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod/Localization/"

# === LOS TEXTOS POR IDIOMA ===
ES_MX = {
    "caida_extra": '''
        // v6.50.128 — EL ALTAR DEVORADOR (la mecánica de comer items)
        ItemCorrompido: \\u2726 El haz del altar PUDRE tu ofrenda\\u2026 {0} esencias corruptas nacen.
        DevoradoSabor1: Mmm. Sabe a casi nada. Aire con forma de recuerdo. A\\u00fan as\\u00ed: tragona.
        DevoradoSabor2: Pasable. Un fondo a lo que fue. La corrupci\\u00f3n le da un regusto\\u2026 interesante.
        DevoradoSabor3: \\u00a1Sabroso! Lo que sea que era, el haz lo COCIN\\u00d3 bien. M\\u00e1s de eso, portador.
        DevoradoSabor4: \\u00a1EXQUISITO! Notas de oro, final eterno, un crujido de mundos. \\u00a1M\\u00c1S!
        DevoradoCuenta: \\u2726 El Eterno devor\\u00f3 {0} esencias corruptas: +{1}% de da\\u00f1o permanente.
        DevoradoTooltip: Devorado en el altar: {0} esencias corruptas (+{1}% de da\\u00f1o{2})
        DevoradoTooltipOjos: , +{0} ojos
''',
    "item_corrupto": '''
        ItemCorrupto: {
                DisplayName: Esencia corrupta
                Tooltip:
                        '''
                        +
                        "Lo que queda de un item tras el haz del ALTAR DE LA CAÍDA.\n"
                        "No se usa con la mano: es COMIDA del Grimorio del Eterno.\n"
                        "Suéltala de nuevo sobre el altar y el libro se la comerá…\n"
                        "si su portador anda cerca."
                        +
                        '''
        }
''',
    "tiles_block": '''Tiles: {
        AltarDeLaCaida: {
                MapEntry: Altar de la Caída
        }
}
''',
}

ES_ES = {
    "caida_extra": '''
        // v6.50.128 — EL ALTAR DEVORADOR (la mecánica de comer items)
        ItemCorrompido: \\u2726 El haz del altar PUDRE tu ofrenda\\u2026 {0} esencias corruptas nacen.
        DevoradoSabor1: Mmm. Sabe a casi nada. Aire con forma de recuerdo. A\\u00fan as\\u00ed: tragona.
        DevoradoSabor2: Pasable. Un fondo a lo que fue. La corrupci\\u00f3n le da un regusto\\u2026 interesante.
        DevoradoSabor3: \\u00a1Sabroso! Lo que sea que era, el haz lo COCIN\\u00d3 bien. M\\u00e1s de eso, portador.
        DevoradoSabor4: \\u00a1EXQUISITO! Notas de oro, final eterno, un crujido de mundos. \\u00a1M\\u00c1S!
        DevoradoCuenta: \\u2726 El Eterno devor\\u00f3 {0} esencias corruptas: +{1}% de da\\u00f1o permanente.
        DevoradoTooltip: Devorado en el altar: {0} esencias corruptas (+{1}% de da\\u00f1o{2})
        DevoradoTooltipOjos: , +{0} ojos
''',
    "item_corrupto": ES_MX["item_corrupto"],
    "tiles_block": ES_MX["tiles_block"],
}

EN_US = {
    "caida_extra": '''
        // v6.50.128 — THE DEVOURING ALTAR (the eat-items mechanic)
        ItemCorrompido: \\u2726 The altar's beam ROTS your offering\\u2026 {0} corrupted essences are born.
        DevoradoSabor1: Hmm. Tastes like almost nothing. Air shaped like a memory. Still: gulp.
        DevoradoSabor2: Passable. A hint of what it was. The corruption gives it an\\u2026 interesting aftertaste.
        DevoradoSabor3: \\u00a1Tasty! Whatever it was, the beam COOKED it right. More of that, bearer.
        DevoradoSabor4: EXQUISITE! Notes of gold, an eternal finish, a crunch of worlds. MORE!
        DevoradoCuenta: \\u2726 The Eternal devoured {0} corrupted essences: +{1}% permanent damage.
        DevoradoTooltip: Devoured at the altar: {0} corrupted essences (+{1}% damage{2})
        DevoradoTooltipOjos: , +{0} eyes
''',
    "item_corrupto": '''
        ItemCorrupto: {
                DisplayName: Corrupted Essence
                Tooltip:
                        '''
                        +
                        "What remains of an item after the FALLING ALTAR's beam.\n"
                        "Not used by hand: it is FOOD for the Eternal Grimoire.\n"
                        "Drop it back onto the altar and the book will eat it…\n"
                        "as long as its bearer is nearby."
                        +
                        '''
        }
''',
    "tiles_block": '''Tiles: {
        AltarDeLaCaida: {
                MapEntry: Altar of the Fall
        }
}
''',
}

ARCHIVOS = [
    ("es-MX_Mods.AethonMod.hjson", ES_MX),
    ("es-ES_Mods.AethonMod.hjson", ES_ES),
    ("en-US_Mods.AethonMod.hjson", EN_US),
]

def clave_presente(texto, clave):
    # Busca "Clave:" como línea propia (con indentación cualquiera).
    return re.search(rf"^\s*{re.escape(clave)}:\s*$", texto, re.M) is not None

def bloque_presente(texto, clave):
    return re.search(rf"^\s*{re.escape(clave)}:\s*\{{", texto, re.M) is not None

for nombre, textos in ARCHIVOS:
    ruta = BASE + nombre
    with open(ruta, "r", encoding="utf-8") as f:
        original = f.read()
    texto = original
    cambios = []

    # === 1. LAS CLAVES DE CAIDA (dentro del bloque "Caida: {") ===
    # Se insertan justo antes del cierre del bloque: buscamos la línea
    # "TintaEnVuelo:" y metemos después (la última clave del bloque hoy).
    if not clave_presente(texto, "DevoradoSabor1"):
        ancla = re.search(r"^(\s*)TintaEnVuelo:.*$", texto, re.M)
        if ancla:
            extra = textos["caida_extra"].lstrip("\n")
            texto = texto[:ancla.end()] + "\n" + extra.rstrip("\n") + texto[ancla.end():]
            cambios.append("Caida.* (8 claves)")
        else:
            print(f"[{nombre}] ERROR: no hallé el ancla TintaEnVuelo"); sys.exit(1)

    # === 2. EL ITEM CORRUPTO (dentro del bloque "Items: {") ===
    if not bloque_presente(texto, "ItemCorrupto"):
        # Al final del archivo (los bloques de primer nivel están al pie).
        bloque = textos["item_corrupto"].strip("\n")
        texto = texto.rstrip("\n") + "\n\n" + bloque + "\n"
        cambios.append("Items.ItemCorrupto")

    # === 3. EL BLOQUE TILES (nuevo, al final del archivo) ===
    if not bloque_presente(texto, "AltarDeLaCaida"):
        bloque = textos["tiles_block"].strip("\n")
        texto = texto.rstrip("\n") + "\n\n" + bloque + "\n"
        cambios.append("Tiles.AltarDeLaCaida")

    if texto != original:
        with open(ruta, "w", encoding="utf-8", newline="\n") as f:
            f.write(texto)
        print(f"[{nombre}] OK: {', '.join(cambios)}")
    else:
        print(f"[{nombre}] sin cambios (ya estaba todo)")

# === LA VERIFICACIÓN: los 3 archivos tienen las MISMAS claves nuevas ===
def claves_devoradas(ruta):
    with open(ruta, encoding="utf-8") as f:
        t = f.read()
    halladas = set()
    for clave in ["ItemCorrompido", "DevoradoSabor1", "DevoradoSabor2", "DevoradoSabor3",
                  "DevoradoSabor4", "DevoradoCuenta", "DevoradoTooltip", "DevoradoTooltipOjos"]:
        if clave_presente(t, clave):
            halladas.add(clave)
    for bloque in ["ItemCorrupto", "AltarDeLaCaida"]:
        if bloque_presente(t, bloque):
            halladas.add(bloque)
    return halladas

mx = claves_devoradas(BASE + "es-MX_Mods.AethonMod.hjson")
es = claves_devoradas(BASE + "es-ES_Mods.AethonMod.hjson")
en = claves_devoradas(BASE + "en-US_Mods.AethonMod.hjson")
print(f"claves mx={len(mx)} es={len(es)} en={len(en)} → {'SIMÉTRICAS' if mx == es == en and len(mx) == 10 else '¡REVISAR!'}")
