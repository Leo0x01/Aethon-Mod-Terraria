#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""v6.88 — EL HJSON DE LA v6.88 (es-ES, la fuente única; luego el sync).
   (1) Altar.* → EL CORRUPTOR; (2) ArmaCorruptaTag «(Corrupto)»;
   (3) hitos del escuadrón en 20/40/60/80/100 (Hito* y Camino*);
   (4) CreditoPuntos muere; (5) JefeDuqueFishron; (6) LibroAzul en
   lugar de CodiceHabilidades; (7) el árbol habla del Libro Azul."""
import io

R = "AethonMod/Localization/es-ES_Mods.AethonMod.hjson"
t = io.open(R, encoding="utf-8").read()

# ---------- (1) el Altar → EL CORRUPTOR ----------
i0 = t.index("Altar: {")
i1 = t.index("}", i0) + 1
nuevo_altar = """Altar: {
	Titulo: EL ALTAR DE LA CORRUPCIÓN
	Subtitulo: v6.88 — ya sin vínculo con el grimorio: corrompe armas
	SlotVacio: arma
	EtiquetaArma: EL ARMA — limpia y de pelea (no herramienta)
	FiltroArma: No vale ya corrupta, ni herramienta, ni el grimorio
	Lista: Lista para corromper
	YaCorrupta: Ya está corrupta
	NoArma: Eso no es un arma corruptible
	Corromper: CORROMPER
	CorruptaHecha: ✦ {0} nace corrupta: su nombre viste el negro y el brillo morado-negro la cubre.
}"""
t = t[:i0] + nuevo_altar + t[i1:]

# ---------- (2) la etiqueta al nombre ----------
t = t.replace("\tArmaCorruptaTag: Corrupto\n", "\tArmaCorruptaTag: (Corrupto)\n")

# ---------- (3) los hitos del escuadrón (cada 20 niveles, en %) ----------
subs_hitos = [
    ("HitoMascota: ✧ ¡DESPERTAR 20: EL GRIMORIO TE ESCUPE AL MINIGRIMORIO como mascota!",
     "HitoMascota: ✧ ¡DESPERTAR 20: la mascota minigrimorio + TODO el ataque del escuadrón +10 %!"),
    ("HitoMejora2: ✧ ¡DESPERTAR 40: la MEJORA 2 de los ojos desbloqueada!",
     "HitoMejora2: ✧ ¡DESPERTAR 40: la MEJORA 2 de los ojos + TODO el ataque del escuadrón +15 %!"),
    ("HitoAscendidos: ✧ ¡DESPERTAR 60: los agujeros ASCIENDEN — aún más daño!",
     "HitoAscendidos: ✧ ¡DESPERTAR 60: los agujeros ASCENDIDOS + TODO el ataque del escuadrón +20 %!"),
    ("HitoMejora4: ✧ ¡DESPERTAR 80: la MEJORA FINAL de los ojos!",
     "HitoMejora4: ✧ ¡DESPERTAR 80: la MEJORA FINAL de los ojos + TODO el ataque del escuadrón +25 %!"),
    ("HitoMaximo: ✧✧ ¡DESPERTAR 100: LA IRA DE UN SER EXTERIOR despierta del todo… y ahora exige su caza: el Señor de la Luna 5 veces y Aethon 1. EL HAZ responde!",
     "HitoMaximo: ✧✧ ¡DESPERTAR 100: LA IRA DE UN SER EXTERIOR + TODO el ataque del escuadrón +30 %! La cima completa: Aethon ×1, Moon Lord ×10 y Emperatriz ×5 pagadas en la puerta — EL HAZ responde!"),
    ("CaminoMascota: LA MASCOTA — el grimorio escupe al minigrimorio",
     "CaminoMascota: la mascota + TODO el ataque del escuadrón +10 %"),
    ("CaminoMejora2: la MEJORA 2 de los ojos",
     "CaminoMejora2: la MEJORA 2 + el escuadrón +15 %"),
    ("CaminoAscendidos: los agujeros ASCENDIDOS (más daño)",
     "CaminoAscendidos: los agujeros ASCENDIDOS + el escuadrón +20 %"),
    ("CaminoMejora4: la MEJORA FINAL de los ojos",
     "CaminoMejora4: la MEJORA FINAL + el escuadrón +25 %"),
    ("CaminoMaximo: LA IRA DE UN SER EXTERIOR — el círculo del escuadrón invoca EL HAZ (daño 1000 + mágico + 50% invocación)",
     "CaminoMaximo: LA IRA DE UN SER EXTERIOR + el escuadrón +30 % — el círculo invoca EL HAZ (daño 1000 + mágico + 50% invocación)"),
]
for v, n in subs_hitos:
    if t.count(v) == 1: t = t.replace(v, n)
    else: print("  (hito no hallado ×1:", v[:44], ")")

# ---------- (4) CreditoPuntos muere ----------
i0 = t.find("\tCreditoPuntos:")
if i0 >= 0:
    i1 = t.index("\n", i0) + 1
    t = t[:i0] + t[i1:]
    print("OK — CreditoPuntos extinta")

# ---------- (5) JefeDuqueFishron ----------
t = t.replace("\tJefeAethon: Aethon, el fragmento de un recuerdo lejano\n",
              "\tJefeAethon: Aethon, el fragmento de un recuerdo lejano\n"
              "\tJefeDuqueFishron: Duque Fishron, el cerdo volador del mar\n")

# ---------- (6) LibroAzul en lugar de CodiceHabilidades ----------
i0 = t.index("CodiceHabilidades: {")
i1 = t.index("}", i0) + 1
nuevo_item = """LibroAzul: {
	DisplayName: Libro Azul
	Tooltip: 'El arma del árbol de habilidades.\\nClic derecho: abre el ÁRBOL DE HABILIDADES (los puntos nacen del nivel de ESTE arma: 3 + nivel/5).\\nClic izquierdo: la descarga azul (daño 12 + nivel·2).\\nSube de nivel con las bajas de su portador (8 por nivel, tope 100).\\nNada que ver con el grimorio: el árbol es del LIBRO.'
}"""
t = t[:i0] + nuevo_item + t[i1:]

# ---------- (7) el árbol habla del Libro Azul ----------
t = t.replace("\tNodo0: El Despertar del Portador\n",
              "\tNodo0: El Libro Azul del Portador\n")
# Desc9 (la cima del árbol) y la ayuda de puntos
t = t.replace("Desc9: El árbol entero cabe en el despertar 100",
              "Desc9: El árbol entero cabe en el Libro Azul 100")
if "PuntosLibresTexto" not in t:
    # la línea de puntos de la UI (Arbol.Puntos ya existe con formato {0}{1}{2})
    pass

io.open(R, "w", encoding="utf-8").write(t)
print("OK — es-ES v6.88 completo")
