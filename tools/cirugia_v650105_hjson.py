#!/usr/bin/env python3
"""
v6.50.105 — CIRUGÍA hjson ×3: el Orbe Cósmico muere, nacen LOS OJOS.

es-MX es el PRIMARIO (espacios); es-ES/en-US son TABS — se operan POR
BYTES (la lección de la .103: el tool de edición normaliza tabs→espacios
en el archivo entero — jamás Edit sobre hjson con tabs).

La cirugía:
  · Buffs.CosmicOrbBuff → OjoEternoBuff (DisplayName/Description).
  · ProjectileNames: CosmicOrbMinion.DisplayName → OjoEternoMinio.DisplayName
    («El Ojo del Eterno»); la línea de CosmicOrbBolt.DisplayName muere
    (el proyectil fue borrado).
"""
import io

BASE = '/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod/Localization/'

# (archivo, eol-marker para detectar tab-vs-espacio no hace falta: se
#  preservan los bytes tal cual — sólo reemplazos por subcadenas EXACTAS)

CIRUGIAS = {
    'es-MX_Mods.AethonMod.hjson': [
        # 1. el buff (bloque indentado con ESPACIOS en es-MX)
        ("""        CosmicOrbBuff: {
                DisplayName: Orbe Cósmico
                Description: Un orbe de luz cósmica te sigue y dispara a tus enemigos
        }""",
         """        OjoEternoBuff: {
                DisplayName: Ojos del Eterno
                Description: Los ojos del grimorio flotan en corona y disparan su rayo negro de bruma
        }"""),
        # 2. el nombre del proyectil del minion
        ("        CosmicOrbMinion.DisplayName: Orbe Cósmico\n"
         "        CosmicOrbBolt.DisplayName: Bolt Cósmico\n",
         "        OjoEternoMinion.DisplayName: El Ojo del Eterno\n"),
    ],
    'es-ES_Mods.AethonMod.hjson': [
        ("""\tCosmicOrbBuff: {
\t\tDisplayName: Orbe Cósmico
\t\tDescription: Un orbe de luz cósmica te sigue y dispara a tus enemigos
\t}""",
         """\tOjoEternoBuff: {
\t\tDisplayName: Ojos del Eterno
\t\tDescription: Los ojos del grimorio flotan en corona y disparan su rayo negro de bruma
\t}"""),
        ("\tCosmicOrbMinion.DisplayName: Orbe Cósmico\n"
         "\tCosmicOrbBolt.DisplayName: Bolt Cósmico\n",
         "\tOjoEternoMinion.DisplayName: El Ojo del Eterno\n"),
    ],
    'en-US_Mods.AethonMod.hjson': [
        ("""\tCosmicOrbBuff: {
\t\tDisplayName: Cosmic Orb
\t\tDescription: A cosmic orb of light follows you and shoots at your enemies
\t}""",
         """\tOjoEternoBuff: {
\t\tDisplayName: Eyes of the Eternal
\t\tDescription: The grimoire's eyes hover in a crown and fire their misty black ray
\t}"""),
        ("\tCosmicOrbMinion.DisplayName: Cosmic Orb\n"
         "\tCosmicOrbBolt.DisplayName: Cosmic Bolt\n",
         "\tOjoEternoMinion.DisplayName: The Eye of the Eternal\n"),
    ],
}


def main():
    for archivo, operaciones in CIRUGIAS.items():
        ruta = BASE + archivo
        with io.open(ruta, 'r', encoding='utf-8', newline='') as f:
            data = f.read()
        original = data
        for viejo, nuevo in operaciones:
            if nuevo in data and viejo not in data:
                print(f'  {archivo}: ya aplicada (idempotente)')
                continue
            assert viejo in data, f'{archivo}: no encontré:\n{viejo[:80]}...'
            assert data.count(viejo) == 1, f'{archivo}: patrón no único'
            data = data.replace(viejo, nuevo)
        with io.open(ruta, 'w', encoding='utf-8', newline='') as f:
            f.write(data)
        print(f'{archivo}: {len(operaciones)} cirugías · '
              f'{len(original)} → {len(data)} bytes')


if __name__ == '__main__':
    main()
