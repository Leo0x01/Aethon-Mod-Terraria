#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""v6.50.65 — ARREGLO DE DIÁLOGOS + ANGLICISMOS DEL GRIMORIO.
Reemplazos por SUBCADENA sobre el texto completo (preserva tabs e indent
de cada línea tal cual está en el archivo). Cada patrón se verifica
único antes de reemplazar. Aplica a es-ES y (tras el espejo) queda
listo para copiar a es-MX."""
import io, sys

ES = "/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod/Localization/es-ES_Mods.AethonMod.hjson"

SUB = [
    # ===== DIÁLOGOS INCOHERENTES =====
    ("KingSlime2: Un rey de gelatina: todo el reino, ninguno el hueso. El grimorio lo bebe de un trago y eructa un trueno azul.",
     "KingSlime2: Un rey de gelatina: todo el reino en el cuerpo, ni un hueso en el trono. El grimorio lo bebe de un trago y eructa un trueno azul."),
    ("Deerclops2: El ciervo de un solo ojo ya no vera la ventisca. El grimorio sorbe su invierno y las letras del índice escarchan.",
     "Deerclops2: El ciervo de un solo ojo ya no verá la ventisca. El grimorio sorbe su invierno y las letras del índice escarchan."),
    ("Arco1: ¿Un arco? La Arquera usaba uno. Acabó en una página.",
     "Arco1: ¿Un arco? Ya me comí arqueros mejores. Todos acabaron en una página."),
    ("KingSlime2: Sabe a corona y a azúcar húmedo. La realeza SIEMPRE cae blanda.",
     "KingSlime2: Sabe a corona y a azúcar derretida. La realeza SIEMPRE cae blanda."),
    ("Arrojadiza2: Boomerangs. Yo ya comí al que los inventó.",
     "Arrojadiza2: Bumeranes. Yo ya me comí al que los inventó."),
    ("Sagrado3: Unicorns: la carne más presumida del menú.",
     "Sagrado3: Unicornios: la carne más presumida del menú."),
    ("Cielo2: Islas flotantes: harpastos con vista. MENÚ CON VISTA.",
     "Cielo2: Islas flotantes: arpías con vista al cielo. MENÚ CON VISTA."),
    ("Superficie1: El grimorio tiene hambre… y la superficie es el bufet.",
     "Superficie1: El grimorio tiene hambre… y la superficie es el bufé."),
    ("Mazmorra3: Calaveras con siglos. Añejo, sí… pero añejo.",
     "Mazmorra3: Calaveras con siglos. Añejo, sí… demasiado añejo."),
    ("Carmesi3: Sangre llaman a esta lluvia. Yo le llamo CLIMA.",
     "Carmesi3: Sangre llaman a esta lluvia. Yo lo llamo CLIMA."),
    ("Invocacion3: Minions. Plural. Mientras YO llevo siglos esperando mi turno.",
     "Invocacion3: Esbirros. En plural. Mientras YO llevo siglos esperando mi turno."),
    ("Sol: EL SOL DEL DIOS — Aethon SE CONVIERTE en sol y lo LANZA: aléjate del gigante roja.",
     "Sol: EL SOL DEL DIOS — Aethon SE CONVIERTE en sol y lo LANZA: aléjate de la gigante roja."),
    # ===== ANGLICISMOS / TÉRMINOS =====
    ("Clic der: prepara 1..11 oleadas (11 = EL JUICIO: los 7 guardianes ×15).",
     "Clic der: prepara 1..11 oleadas (11 = EL JUICIO: los 6 guardianes ×15)."),
    ("Item de testing — eliminar antes de release.",
     "Objeto de pruebas — eliminar antes de publicar.", 3),
    ("Material para craftear el Grimorio del Eterno.",
     "Material para fabricar el Grimorio del Eterno."),
    ("Dropea de King Slime y Eye of Cthulhu.",
     "Se obtiene al vencer al Rey Slime y al Ojo de Cthulhu."),
    ("Click izq: bolt arcano. Click der: invoca minion.",
     "Clic izq: descarga arcana. Clic der: invoca un esbirro."),
    ("mide su valor, la PRIMERA kill de cada especie paga ×3 y el hardmode",
     "mide su valor, la PRIMERA baja de cada especie paga ×3 y el modo difícil"),
    ("sigue comiendo XP, conserva los slots de minion y vida/maná (tope",
     "sigue comiendo XP, conserva los espacios de esbirro y vida/maná (tope"),
    ("+100); guardado = nada (los minions ya invocados permanecen).",
     "+100); guardado = nada (los esbirros ya invocados permanecen)."),
    ("El gauge, el péndulo, el coro y demás rarezas (los 6).",
     "El medidor, el péndulo, el coro y demás rarezas (los 6)."),
    ("los soles de tier alto.", "los soles de nivel alto."),
    ("en caza embiste (mordida de minion).", "en caza embiste (mordida de esbirro)."),
    ("Ocupa 1 espacio de sirviente · nunca expira · sin coste de maná.",
     "Ocupa 1 espacio de esbirro · nunca expira · sin coste de maná."),
    ("afterimages orbitando y respirando a su alrededor (la firma).",
     "estelas fantasma orbitando y respirando a su alrededor (la firma)."),
    ("BEAM telegrafiado de 900px (×2.5)", "HAZ telegrafiado de 900px (×2.5)"),
    ("MINION: ocupa 1 espacio de sirviente · nunca expira.",
     "ESBIRO: ocupa 1 espacio de esbirro · nunca expira."),
    ("gestan 2 segundos sin dañar (pulsos que imploden) y luego azotan",
     "maduran 2 segundos sin dañar (pulsos que implosionan) y luego azotan"),
    ("SlotsMinionLlenos: Slots de minion llenos: {0}/{1}.",
     "SlotsMinionLlenos: Espacios de esbirro llenos: {0}/{1}."),
]

def main(path):
    src = io.open(path, encoding="utf-8").read()
    for tup in SUB:
        viejo, nuevo = tup[0], tup[1]
        esperado = tup[2] if len(tup) > 2 else 1
        n = src.count(viejo)
        if n != esperado:
            print("PATRÓN %s (%d veces): %r" % ("FALTA" if n == 0 else "AMBIGÚO", n, viejo[:70]))
            sys.exit(1)
        src = src.replace(viejo, nuevo)
    io.open(path, "w", encoding="utf-8").write(src)
    print("OK: %d reemplazos aplicados a %s" % (len(SUB), path))

if __name__ == "__main__":
    main(ES)
