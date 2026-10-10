#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""hjson_v65094.py — LA LETRA DEL NERVIOSO .94 en es-MX + en-US (tabs intactos).

La letra del usuario:
  · «el hambre se pausa» → el reloj SIEMPRE (sin gracia de 10 s)
  · «su ataque sigue relanzandose al matar» → compás: un ataque cada 10 s
    (o nada antes de 2 s tras terminar el anterior)
  · «la fuga simplemente la quitamos el libro no se fuga, solo queda
    flotando cerca del jugador cazando por si mismo»
  · «esto de la caza por su cuenta es cuando el jugador esta en afk o
    simplemente lleva mucho tiempo sin atacar criaturas con el libro»
  · «el festin compartido no tiene sentido […] el libro debe ser egoista
    y comer toda la criatura loot incluido por eso el libro recibe la
    exp y el jugador nada»

Cirugía byte-exacta: el en-US conserva SUS tabs (el 19º incidente del
espejo jamás se olvida). Aserciones de unicidad por edición.
"""
import io
import sys

BASE = "/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod/Localization/"

TOOLTIP_MX = """\
La copia HAMBRIENTA — el mismo ojo, pero este libro
es APETITO CON CUERPO: intranquilo, enojado, con
hambre. Su hambre sube SIEMPRE (tasa de prueba; la
versión real irá más lenta): sólo la presa comida la
baja (−1%) y el clic derecho la reinicia.
Desde el 25% sale a cazar POR SU CUENTA si te ausentas
(AFK 5 s, o 30 s sin atacar criaturas con el libro):
flota cerca de ti y su Sombra de la Página devora a
la criatura que se mueva cerca — su ataque se lanza
UNA vez cada 10 s (y nunca antes de 2 s de terminar
el anterior).
El festín es EGOÍSTA: se come a la presa ENTERA, botín
incluido — la XP es de ESTE libro (sube su nivel);
tú no recibes nada. Mientras está fuera, sus ataques
salen DEL LIBRO (tu mano ataca vacía).
NUNCA se escapa: al 100% se queda flotando cerca de
ti, cazando por sí mismo.
Clic izq: descarga perseguidora (pruebas, sin maná).
Clic der: reinicia el apetito.
Objeto de pruebas — comparar con el original.
"""

TOOLTIP_EN = """\
The HUNGRY copy — the same eye, but this book is
APPETITE WITH A BODY: restless, angry, starving. Its
hunger ALWAYS rises (test rate; the real one will be
slower): only a devoured prey lowers it (−1%) and
right click resets it.
From 25% hunger on it hunts ON ITS OWN whenever you
step away (AFK 5 s, or 30 s without attacking
creatures with the book): it floats near you and its
Page Shadow devours whatever creature moves nearby —
its attack fires ONCE every 10 s (and never sooner
than 2 s after the previous one ends).
The feast is SELFISH: it eats the prey WHOLE, loot
included — the XP belongs to THIS book (it raises its
own level); you get nothing. While it is out, its
attacks come FROM THE BOOK (your hand swings empty).
It NEVER escapes: at 100% it just keeps floating
near you, hunting on its own.
Left click: homing bolt (testing, no mana).
Right click: resets the appetite.
Testing item — compare against the original.
"""


def reemplazar_tooltip(src, cuerpo_nuevo, nombre_bloque):
    """Sustituye el cuerpo del Tooltip (entre ''' y ''') del bloque dado."""
    ancla = nombre_bloque + ": {"
    i0 = src.index(ancla)
    it = src.index("Tooltip:", i0)
    ia = src.index("'''", it) + 3
    ib = src.index("'''", ia)
    viejo = src[ia:ib]
    return src[:ia] + "\n" + cuerpo_nuevo + src[ib:], viejo


def linea_unica(src, vieja, nueva, etiqueta):
    n = src.count(vieja)
    assert n == 1, f"{etiqueta}: esperaba 1, hallé {n}"
    return src.replace(vieja, nueva)


def parchear(archivo, tooltip, ind2, fugitivo, escapa, nivel, cmt_viejo, cmt_nuevo):
    p = BASE + archivo
    src = io.open(p, encoding="utf-8").read()
    ediciones = 0

    # (1) EL TOOLTIP DEL NERVIOSO — rehecho a la letra .94
    src, viejo = reemplazar_tooltip(src, tooltip, "GrimorioHambrientoNervioso")
    assert "100%, IT ESCAPES" in viejo or "SE ESCAPA" in viejo, "tooltip viejo inesperado"
    ediciones += 1

    # (2) LA FUGA MUERE EN LAS FRASES: fuera Fugitivo y Escapa
    src = linea_unica(src, ind2 + fugitivo + "\n", "", archivo + " Fugitivo")
    src = linea_unica(src, ind2 + escapa + "\n", "", archivo + " Escapa")
    ediciones += 2

    # (3) EL NIVEL DEL LIBRO (el festín egoísta: la XP es del Nervioso)
    ancla = ind2 + ("Absorbe: −1% hunger" if archivo.startswith("en-") else "Absorbe: −1% hambre")
    assert src.count(ancla) == 1, "ancla Absorbe"
    src = src.replace(ancla, ancla + "\n" + nivel)
    ediciones += 1

    # (4) EL COMENTARIO DEL ESPÍRITU — la fuga murió
    src = linea_unica(src, cmt_viejo, cmt_nuevo, archivo + " comentario espíritu")
    ediciones += 1

    io.open(p, "w", encoding="utf-8", newline="\n").write(src)
    print(f"OK {archivo}: {ediciones} ediciones")


parchear(
    "es-MX_Mods.AethonMod.hjson",
    TOOLTIP_MX,
    " " * 16,
    "Fugitivo: ¡SE ESCAPÓ! La hartura de hambre lo alejó de ti.",
    "Escapa: ¡ESTOY HARTO! ¡Me largo!",
    " " * 16 + "Nivel: Nivel {0} · XP {1}/{2}",
    "        // — vive ahora en el ESPÍRITU del Grimorio Nervioso (caza y fuga).",
    "        // — vive ahora en el ESPÍRITU del Grimorio Nervioso (caza — la\n"
    "        // fuga murió en la .94: el libro nunca se escapa).",
)

parchear(
    "en-US_Mods.AethonMod.hjson",
    TOOLTIP_EN,
    "\t\t",
    "Fugitivo: IT ESCAPED! Sick of hunger, it keeps away from you.",
    "Escapa: I've HAD IT! I'm off!",
    "\t\tNivel: Level {0} · XP {1}/{2}",
    "\t// lives in the NERVOUS GRIMOIRE's spirit (hunt and escape).",
    "\t// lives in the NERVOUS GRIMOIRE's spirit (hunt — the escape died\n"
    "\t// in .94: the book never runs away).",
)

print("hjson .94 listo (es-MX + en-US; es-ES se regenera con el sync)")
