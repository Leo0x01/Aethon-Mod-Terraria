#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""v6.50.93 — parche quirúrgico hjson ×3 (tooltip del Nervioso + fase Hambriento).
es-MX/es-ES llevan ESPACIOS (8/16/24); en-US lleva TABS (lección del 19º
incidente: la herramienta de edición JAMÁS toca los hjson — Python puro)."""
import io, sys

BASE = "/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod/Localization/"

def parchar(ruta, viejo, nuevo, etiqueta):
    with io.open(BASE + ruta, "r", encoding="utf-8", newline="") as f:
        t = f.read()
    if t.count(viejo) != 1:
        print("FALLO [%s]: %d apariciones de -> %s" % (ruta, t.count(viejo), etiqueta))
        sys.exit(1)
    t = t.replace(viejo, nuevo)
    with io.open(BASE + ruta, "w", encoding="utf-8", newline="") as f:
        f.write(t)
    print("OK [%s] -> %s" % (ruta, etiqueta))

# ================================================================ es-MX + es-ES (espacios)
tt_es_viejo = u"""                        La copia HAMBRIENTA \u2014 el mismo ojo, pero este libro
                        es APETITO CON CUERPO: intranquilo, enojado, con
                        hambre. Debajo del 50% es EL CAZADOR: si te quedas
                        quieto, sale a flotar sobre ti y su Sombra de la
                        P\u00e1gina devora a la criatura que se mueva cerca
                        (sin bot\u00edn; la XP s\u00ed es del grimorio) \u2014 cada presa
                        lo sacia 1% y, mientras come, el ojo se clava en su
                        comida. Del 50% en adelante es EL IMPACIENTE
                        (habla, tiembla, provoca oleadas); al 100%, SE ESCAPA.
                        El hambre s\u00f3lo sube si pasan 10 segundos sin comer.
                        Clic izq: descarga perseguidora (pruebas, sin man\u00e1).
                        Clic der: reinicia el apetito (y lo calma 10 s).
                        Objeto de pruebas \u2014 comparar con el original.
"""

tt_es_nuevo = u"""                        La copia HAMBRIENTA \u2014 el mismo ojo, pero este libro
                        es APETITO CON CUERPO: intranquilo, enojado, con
                        hambre. Desde el 25% de hambre es EL CAZADOR: si te
                        quedas quieto, sale a flotar sobre ti y su Sombra
                        de la P\u00e1gina devora a la criatura que se mueva
                        cerca (sin bot\u00edn; la XP s\u00ed es del grimorio) \u2014 un
                        SOLO ataque por criatura, con su pausa de digesti\u00f3n
                        entre presa y presa; cada presa lo sacia 1% y,
                        mientras come, el ojo se clava en su comida.
                        Mientras est\u00e1 fuera, sus ataques salen DEL LIBRO
                        (tu mano ataca vac\u00eda) y su hambre NO se congela:
                        sube coma o no coma \u2014 ahora es prueba (r\u00e1pida);
                        la versi\u00f3n real ir\u00e1 m\u00e1s lenta. Del 50% en adelante
                        es EL IMPACIENTE (habla, tiembla, provoca oleadas);
                        al 100%, SE ESCAPA (cazando o no). El hambre s\u00f3lo
                        sube si pasan 10 segundos sin comer.
                        Clic izq: descarga perseguidora (pruebas, sin man\u00e1).
                        Clic der: reinicia el apetito (y lo calma 10 s).
                        Objeto de pruebas \u2014 comparar con el original.
"""

fase_es_viejo = u"""                Tranquilo: El cazador espera. Vigila, paciente, con apetito.
                Cazando: FUERA DE CAZA \u2014 flota sobre ti buscando su propia comida.
"""
fase_es_nuevo = u"""                Tranquilo: El cazador espera. Vigila, paciente, con apetito.
                Hambriento: HAMBRIENTO \u2014 con el 25% ya quiere salir a cazar.
                Cazando: FUERA DE CAZA \u2014 flota sobre ti buscando su propia comida.
"""

for ruta in ("es-MX_Mods.AethonMod.hjson", "es-ES_Mods.AethonMod.hjson"):
    parchar(ruta, tt_es_viejo, tt_es_nuevo, "tooltip Nervioso .93")
    parchar(ruta, fase_es_viejo, fase_es_nuevo, "fase Hambriento")

# ================================================================ en-US (tabs)
tt_en_viejo = u"""\t\t\tThe HUNGRY copy \u2014 the same eye, but this book is
\t\t\tAPPETITE WITH A BODY: restless, angry, starving. Below
\t\t\t50% it is THE HUNTER: stand still and it floats out
\t\t\tover you, and its Page Shadow devours whatever creature
\t\t\tmoves nearby (no loot; the XP still goes to the grimoire)
\t\t\t\u2014 each prey eases it 1%, and while it eats, its eye
\t\t\tlocks onto its food. From 50% on it is THE IMPATIENT one
\t\t\t(it talks, trembles, summons waves); at 100%, IT ESCAPES.
\t\t\tHunger only rises if 10 seconds pass without eating.
\t\t\tLeft click: homing bolt (testing, no mana).
\t\t\tRight click: resets the appetite (and calms it 10 s).
\t\t\tTesting item \u2014 compare against the original.
"""

tt_en_nuevo = u"""\t\t\tThe HUNGRY copy \u2014 the same eye, but this book is
\t\t\tAPPETITE WITH A BODY: restless, angry, starving. From
\t\t\t25% hunger on it is THE HUNTER: stand still and it
\t\t\tfloats out over you, and its Page Shadow devours
\t\t\twhatever creature moves nearby (no loot; the XP still
\t\t\tgoes to the grimoire) \u2014 ONE attack per creature, with
\t\t\ta digesting pause between prey; each prey eases it 1%,
\t\t\tand while it eats, its eye locks onto its food. While
\t\t\tit is out, its attacks come FROM THE BOOK (your hand
\t\t\tswings empty) and its hunger does NOT freeze: it rises
\t\t\twhether it eats or not \u2014 this is a test build (fast);
\t\t\tthe real one will be slower. From 50% on it is THE
\t\t\tIMPATIENT one (it talks, trembles, summons waves); at
\t\t\t100%, IT ESCAPES (hunting or not). Hunger only rises
\t\t\tif 10 seconds pass without eating.
\t\t\tLeft click: homing bolt (testing, no mana).
\t\t\tRight click: resets the appetite (and calms it 10 s).
\t\t\tTesting item \u2014 compare against the original.
"""

fase_en_viejo = u"""\t\tTranquilo: The hunter waits. It watches, patient, hungry.
\t\tCazando: OUT HUNTING \u2014 it floats above you, seeking its own food.
"""
fase_en_nuevo = u"""\t\tTranquilo: The hunter waits. It watches, patient, hungry.
\t\tHambriento: HUNGRY \u2014 at 25% it already wants out to hunt.
\t\tCazando: OUT HUNTING \u2014 it floats above you, seeking its own food.
"""

parchar("en-US_Mods.AethonMod.hjson", tt_en_viejo, tt_en_nuevo, "tooltip Nervioso .93")
parchar("en-US_Mods.AethonMod.hjson", fase_en_viejo, fase_en_nuevo, "fase Hambriento")

print("hjson ×3: parche v6.50.93 completo")
