#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# docs_v650112.py — CHANGES entrada .112 + README 6.50.112 + STABLE-SNAPSHOT
# (header con el doc-rot de los bloques Última actualización .112/.111/.110
# — la .109 SALE — + fila HISTORIAL + item 1 de PRÓXIMOS con renumeración
# POR LÍNEAS: .111→2, .110→3, .109→4 y la .108 PODADA — la lección .110:
# JAMÁS regex sobre «\nN. **», se confunde con las sublistas internas).
import re

BASE = "/home/z/my-project/download/Aethon-Mod-Terraria/"

CHANGES = """# AethonMod — Historial de Cambios

## Commit v6.50.112 — EL PEREZOSO Y LA PÁGINA EN BLANCO

**La letra**: «el libro cuando esta fuera se mueve muy rapido, debe ser
mas lento» + «necesito un item que reinicie el contador de los jefes
para verificar como devora los jefes, este iten no debe ser
consumible».

**EL CAMBIO**: dos entregas — el muelle del libro flotante y el
probador de LA PRIMERA CENA:

- **EL MUELLE DEL PEREZOSO**: el espíritu flotante de la .111 (tirón
  `0.02`, freno `0.90` y **SIN TOPE** en MODO_CAZA) pegaba el libro a
  la cabeza del jugador con un tirón al 27% MÁS rápido que la carrera
  y lo convertía en **MISIL** al teletransportarse o caer — **127 px/t
  medidos en `tools/sim_libro_v650112.py`**, la simulación del muelle
  con los escenarios reales (caminar 3 / correr 6,25 / esprintar 9 /
  caer 10,5 / vagoneta 20 px/t + teletransporte de 1500 px). EL MUELLE
  NUEVO: tirón a menos de la mitad (`0.009` — el libro se queda ATRÁS
  con elegancia: 42 px al caminar, 88 al correr, 127 al esprintar),
  freno `0.88` y **TOPE GRADUADO** `Clamp(dist×0.01, 10, 20)`: 10 px/t
  cerca del dueño (nunca más rápido que una caída), 20 sólo si quedó
  muy lejos — un teletransporte NO lo pierde (1500 px en ~110 t de
  deriva serena, no en 15 de misil). **LA VUELTA deja de ser zumazo**:
  empuje `0.32` (era `0.90`), tope `12` (era `22`) — planea a casa en
  ~15 t desde la vela (era un misil de 9). Y el perdedor de
  `SoyDuplicado` ahora se **DISUELVE EN POLVO** de llama de sombra
  (con la vuelta lenta es más fácil que un espíritu que planea a casa
  pierda contra un cazador nuevo — sin polvo era un chasquido seco
  invisible).
- **LA PÁGINA EN BLANCO (ítem de pruebas)**: borra
  `ShardPlayer.JefesDevorados` del portador — la lista de LA PRIMERA
  CENA de la .110 («que esto suceda la primera vez que se mate un jefe
  y solo la primera vez»): la página donde el grimorio escribió sus
  festines queda EN BLANCO y **cada tipo de jefe vuelve a estrenar su
  cena completa** (el jefe se detiene a 100 de vida → los sellos lo
  aprisionan y lo llevan al centro de la pantalla → LA SOMBRA DE LA
  PÁGINA lo devora → el loot cae como una muerte normal). **NO
  CONSUMIBLE** (pila 1, el patrón de la Esencia de Hambre), la voz
  cuenta cuántos festines se borraron («Reset») o que no había ninguno
  («Nada»), autoridad SP/server con `EcoRed.AnunciarAlPortador` en MP.
  Textura 24×24 por `tools/gen_pagina_blanca_v650112.py` — la hermana
  de la Esencia de Hambre: el MISMO pliego negro torcido del grimorio
  con la tinta BORRÁNDOSE de izquierda a derecha y las motas violáceas
  alzándose del borde (la memoria en humo). hjson ×3 por
  `tools/insert_hjson_v650112.py` (DETECTA el formato post-packager:
  es-ES/en-US claves planas top-level, es-MX bloque + espacios).
  Registrada en la **BOLSA DEL PROBADOR** (la lección v6.50.63: cada
  ítem nuevo en DOS sitios).

**VERIFICACIÓN**: audit_texturas 0 · oráculo 0/0 · build real 0/0 ·
headless «Sandboxing v6.50.112 → Adding Content → Configuring →
Finalizing → Adding Recipes → Server started» 0 excepciones ·
audit_v650112 TODO OK (188 ✓): .tmod 2.605.378 B md5
d46aa5d53086005e12301a0059063ed9, 390 entradas (389 +
PaginaEnBlanco.rawimg 24×24 píxel-idéntica), EOF exacto, PaginaEnBlanco
+ las constantes del muelle nuevo (0.009/0.88/0.01/10/20/0.32/12)
VIVAS en el IL, los muertos del muelle viejo (0.02/0.90/22) AUSENTES
del árbol, las claves Reset/Nada VIVAS en #US y en el paquete hjson
×3 + la regresión .104–.111 ÍNTEGRA.

**PUBLICACIÓN**: release 410136787 + asset AethonMod.tmod 632578719
2.605.378 B (md5 d46aa5d5…), CDN verificado byte a byte,
/releases/latest = v6.50.112.

"""

HEADER_112 = """> Última actualización: v6.50.112 (EL PEREZOSO Y LA PÁGINA EN
> BLANCO. La letra: «el libro cuando esta fuera se mueve muy rapido,
> debe ser mas lento» + «necesito un item que reinicie el contador de
> los jefes para verificar como devora los jefes, este iten no debe ser
> consumible»). Dos entregas: (1) EL MUELLE DEL PEREZOSO — el libro
> flotante (tirón 0.02, freno 0.90, SIN TOPE) pegaba a la cabeza del
> jugador con un tirón al 27% más rápido que la carrera y era MISIL al
> teletransportarse (127 px/t medidos en tools/sim_libro_v650112.py,
> la simulación con los escenarios reales): muelle nuevo tirón 0.009
> (se queda ATRÁS: 42 px al caminar, 127 al esprintar) + freno 0.88 +
> TOPE GRADUADO clamp(dist·0.01, 10, 20) — 10 px/t cerca del dueño, 20
> sólo si quedó muy lejos; LA VUELTA planea (empuje 0.32, tope 12 —
> era el misil 0.90/22); el perdedor del espíritu único se DISUELVE EN
> POLVO. (2) LA PÁGINA EN BLANCO — el probador de LA PRIMERA CENA:
> borra ShardPlayer.JefesDevorados («solo la primera vez») y cada tipo
> de jefe vuelve a estrenar su cena completa con su loot; NO
> CONSUMIBLE, la voz Reset/Nada, textura 24×24 (la tinta borrándose +
> las motas — la hermana de la Esencia de Hambre), en la Bolsa
> Probador. .tmod 2.605.378 B
> md5 d46aa5d53086005e12301a0059063ed9, 390 entradas, oráculo 0/0,
> build real 0/0, headless 0 excepciones, audit 188 ✓.)
"""

FILA_112 = (
    "| **v6.50.112** | ✅ Build-verificada (audit_texturas 0, oráculo 0/0, build real 0/0, "
    ".tmod 2.605.378 B md5 d46aa5d53086005e12301a0059063ed9, 390 entradas (389 de la .111 + "
    "PaginaEnBlanco.rawimg 24×24 2316 B = 12+24×24×4 magic 1), EOF exacto, hjson semántico == "
    "árbol ×3 (es-MX byte a byte), PaginaEnBlanco VIVO en el heap #Strings + los ldstr "
    "«Mods.AethonMod.PaginaEnBlanco.Reset»·«Mods.AethonMod.PaginaEnBlanco.Nada» VIVOS en el "
    "heap #US (UTF-16LE), las constantes del muelle nuevo en el IL (0.009 el tirón · 0.88 el "
    "freno · 0.01/10/20 el tope graduado · 0.32/12 el planeo de vuelta), los muertos del "
    "muelle viejo AUSENTES del árbol (0.02/0.90 en velocity · 22), la rawimg píxel-idéntica "
    "al .png (RGB nulo sólo en alfa 0) + la regresión .104–.111 ÍNTEGRA (Ceiling vivo, el "
    "icono 32×32, la cena, las tres ventanas), headless «Sandboxing v6.50.112 → Adding "
    "Content → Server started» 0 excepciones), ✔ PUBLICADA (release 410136787 + asset "
    "AethonMod.tmod 632578719 2.605.378 B, CDN verificado byte a byte md5 d46aa5d5…, "
    "/releases/latest = v6.50.112, respaldo /home/sync/AethonMod-v6.50.112.tmod), ⏳ en juego "
    "| LA LETRA (2026-10-12): «el libro cuando esta fuera se mueve muy rapido, debe ser mas "
    "lento» + «necesito un item que reinicie el contador de los jefes para verificar como "
    "devora los jefes, este iten no debe ser consumible». DOS ENTREGAS: (1) EL MUELLE DEL "
    "PEREZOSO — GrimorioNerviosoFlotante: el muelle de la .111 (0.02/0.90/SIN tope en "
    "MODO_CAZA) tiraba al 27% más rápido que la carrera y convertía el libro en MISIL al "
    "teletransportarse (127 px/t — tools/sim_libro_v650112.py: caminar 3/correr 6,25/"
    "esprintar 9/caer 10,5/vagoneta 20 px/t + teletransporte 1500 px); el nuevo: tirón 0.009 "
    "(menos de la mitad — el libro se queda ATRÁS: 42/88/127 px), freno 0.88, TOPE GRADUADO "
    "MathHelper.Clamp(dist×0.01, 10, 20) — 10 px/t cerca del dueño (nunca más rápido que una "
    "caída), 20 lejos (un teletransporte NO lo pierde: 1500 px en ~110 t de deriva); LA "
    "VUELTA (MODO_VOLVER) planea: empuje 0.32 (era 0.90), tope 12 (era 22) — ~15 t desde la "
    "vela (era zumazo de 9); el perdedor de SoyDuplicado se DISUELVE EN POLVO de llama de "
    "sombra (antes: chasquido seco invisible). (2) LA PÁGINA EN BLANCO (ítem de pruebas): "
    "UseItem borra ShardPlayer.JefesDevorados del portador (la lista de LA PRIMERA CENA .110 "
    "«solo la primera vez», persiste en el .plr) — cada tipo de jefe vuelve a estrenar su "
    "cena completa (100 de vida → sellos → centro de pantalla → la sombra devora → loot "
    "normal); NO CONSUMIBLE (pila 1, el patrón de la Esencia), la voz Reset (cuántos "
    "festines borró)/Nada (lista ya vacía), autoridad SP/server + EcoRed.AnunciarAlPortador "
    "en MP; textura 24×24 tools/gen_pagina_blanca_v650112.py (el pliego de la Esencia con la "
    "tinta BORRÁNDOSE y las motas alzándose — la memoria en humo); hjson ×3 "
    "tools/insert_hjson_v650112.py (detecta el formato post-packager: es-ES/en-US planas, "
    "es-MX bloque); registrada en la BOLSA DEL PROBADOR (la lección v6.50.63). LECCIONES: "
    "(a) el muelle de un seguidor se DISEÑA con simulación (los números del tirón/freno/"
    "tope salen de tools/sim_libro_v650112.py, no del ojo); (b) el doc-rot del header del "
    "snapshot mantiene los ÚLTIMOS 3 bloques «Última actualización» (.112/.111/.110 — la "
    ".109 sale) y la cirugía de PRÓXIMOS es POR LÍNEAS con anclas únicas por versión "
    "(jamás regex «\\nN. **») |\n"
)

ITEM_112 = """1. **✅ v6.50.112 PUBLICADA — EL PEREZOSO Y LA PÁGINA EN BLANCO**
   — la letra: «el libro cuando esta fuera se mueve muy rapido, debe
   ser mas lento» + «necesito un item que reinicie el contador de los
   jefes para verificar como devora los jefes, este iten no debe ser
   consumible».
   (1) EL MUELLE DEL PEREZOSO: el espíritu flotante (tirón 0.02, freno
   0.90, SIN TOPE en MODO_CAZA) pegaba el libro a la cabeza con un
   tirón al 27% MÁS rápido que la carrera y lo convertía en MISIL al
   teletransportarse (127 px/t medidos en tools/sim_libro_v650112.py —
   la simulación del muelle: caminar/correr/esprintar/caer/vagoneta/
   teletransporte); el nuevo: tirón 0.009 (el libro se queda ATRÁS con
   elegancia: 42 px al caminar, 88 al correr, 127 al esprintar), freno
   0.88 y TOPE GRADUADO Clamp(dist×0.01, 10, 20) — 10 px/t cerca del
   dueño, 20 sólo si quedó muy lejos (un teletransporte NO lo pierda:
   1500 px en ~110 t de deriva serena); LA VUELTA planea (empuje 0.32,
   tope 12 — era el misil 0.90/22: llega de la vela en ~15 t); y el
   perdedor de SoyDuplicado se DISUELVE EN POLVO de llama de sombra.
   (2) LA PÁGINA EN BLANCO (ítem de pruebas): borra la lista
   ShardPlayer.JefesDevorados del portador (la «solo la primera vez»
   de LA PRIMERA CENA) — cada tipo de jefe vuelve a estrenar su cena
   completa con su loot; NO CONSUMIBLE (pila 1), la voz Reset/Nada,
   textura 24×24 (la tinta borrándose + las motas — la hermana de la
   Esencia de Hambre), en la BOLSA DEL PROBADOR.
   VERIFICACIÓN: audit_texturas 0 · oráculo 0/0 · build real 0/0 ·
   headless 0 excepciones · audit_v650112 TODO OK (188 ✓) — .tmod
   2.605.378 B md5 d46aa5d53086005e12301a0059063ed9, 390 entradas
   (389 + PaginaEnBlanco.rawimg), EOF exacto, las constantes del
   muelle nuevo VIVAS en el IL (0.009/0.88/0.01/10/20/0.32/12), los
   muertos del viejo AUSENTES (0.02/0.90/22), Reset/Nada VIVAS en #US
   y en el hjson ×3 + la regresión .104–.111 ÍNTEGRA. CHECKLIST en
   juego: (a) con el libro fuera: al caminar se queda atrás con calma
   y al parar flota de vuelta — sin tirones; (b) teletransporte o
   caída larga: el libro LLEGA EN DERIVA, no como misil; (c)
   «saciado»: la vuelta a casa es un planeo, no un zumazo; (d) la
   Página en Blanco (Bolsa Probador): úsala → «olvida sus N
   festines» → el próximo jefe de cada tipo vuelve a estrenar LA
   PRIMERA CENA con su loot; (e) con la lista vacía → «no recuerda
   ningún festín».
   PUBLICACIÓN: commit 1a8a696 + tag v6.50.112 + push · release
   **410136787** + asset AethonMod.tmod **632578719** 2.605.378 B (md5
   d46aa5d53086005e12301a0059063ed9), CDN VERIFICADO BYTE A BYTE,
   /releases/latest = v6.50.112, respaldo /home/sync/AethonMod-v6.50.112.tmod.
"""


def changes():
    p = BASE + "CHANGES.md"
    s = open(p, encoding="utf-8").read()
    s = s.replace("# AethonMod — Historial de Cambios\n\n", "", 1)
    s = CHANGES + s
    open(p, "w", encoding="utf-8").write(s)
    assert s.count("## Commit v6.50.112") == 1
    print("CHANGES.md OK")


def readme():
    p = BASE + "README.md"
    s = open(p, encoding="utf-8").read()
    s = re.sub(r"\*\*Versión actual:\*\* 6\.50\.111",
               "**Versión actual:** 6.50.112", s, count=1)
    open(p, "w", encoding="utf-8").write(s)
    assert "6.50.112" in s
    print("README.md OK")


def snapshot():
    p = BASE + "STABLE-SNAPSHOT.md"
    lineas = open(p, encoding="utf-8").read().splitlines(keepends=True)

    # ---------- 1) EL HEADER: título .112 + bloques .112/.111/.110 ----------
    i_b111 = next(i for i, l in enumerate(lineas)
                  if l.startswith("> Última actualización: v6.50.111 ("))
    i_b110 = next(i for i, l in enumerate(lineas)
                  if l.startswith("> Última actualización: v6.50.110 ("))
    i_b109 = next(i for i, l in enumerate(lineas)
                  if l.startswith("> Última actualización: v6.50.109 ("))
    bloque_111 = "".join(lineas[i_b111:i_b110])
    bloque_110 = "".join(lineas[i_b110:i_b109])
    # el fin del bloque .109: la primera línea que NO empieza con '>' tras i_b109
    i_fin109 = next(i for i in range(i_b109 + 1, len(lineas))
                    if not lineas[i].startswith(">"))
    header_nuevo = [
        "# AethonMod — ESTADO ACTUAL (v6.50.112)\n",
        "\n",
        "> **Este documento = \"¿por dónde nos quedamos?\"** — se actualiza en cada entrega.\n",
        HEADER_112,
        bloque_111,
        bloque_110,
    ]
    lineas = header_nuevo + lineas[i_fin109:]
    s = "".join(lineas)
    assert "ESTADO ACTUAL (v6.50.112)" in s
    assert s.count("Última actualización: v6.50.112") == 1
    assert "Última actualización: v6.50.109" not in s.split("## ✅ ESTADO VERIFICADO")[0]
    print("  header OK (bloques .112/.111/.110 — la .109 salió)")

    # ---------- 2) LA FILA DEL HISTORIAL (antes de la fila de la .111) ----------
    ancla = "| **v6.50.111** |"
    i = s.find(ancla)
    assert i > 0, "no encontré la fila .111 del HISTORIAL"
    s = s[:i] + FILA_112 + s[i:]
    assert s.count("| **v6.50.112** |") == 1
    print("  fila HISTORIAL OK")

    # ---------- 3) PRÓXIMOS: doc-rot POR LÍNEAS con anclas por versión ----------
    # 3a. PODAR la .108 (de su línea de inicio a la línea previa a la .100)
    lin = s.splitlines(keepends=True)
    i_108 = next(i for i, l in enumerate(lin)
                 if l.startswith("4. **✅ v6.50.108 PUBLICADA"))
    i_100 = next(i for i, l in enumerate(lin)
                 if l.startswith("1. **✅ v6.50.100 PUBLICADA"))
    assert i_108 < i_100, "la .108 debería estar antes que la .100"
    del lin[i_108:i_100]
    s = "".join(lin)
    assert "v6.50.108 PUBLICADA — EL FIX DE LOS OJOS" not in s
    print("  .108 PODADA de PRÓXIMOS")

    # 3b. RENUMERAR (del 3 hacia arriba — anclas únicas por versión)
    for viejo, nuevo, version in [
        ("3. **✅ v6.50.109 PUBLICADA", "4. **✅ v6.50.109 PUBLICADA", ".109"),
        ("2. **✅ v6.50.110 PUBLICADA", "3. **✅ v6.50.110 PUBLICADA", ".110"),
        ("1. **✅ v6.50.111 PUBLICADA", "2. **✅ v6.50.111 PUBLICADA", ".111"),
    ]:
        assert s.count(viejo) == 1, f"ancla no única para {version}: {viejo!r}"
        s = s.replace(viejo, nuevo, 1)
    print("  renumeración .111→2 · .110→3 · .109→4")

    # 3c. INSERTAR la .112 como item 1 (tras el header de PRÓXIMOS)
    ancla = "## 🚧 PRÓXIMOS PASOS SUGERIDOS (en orden)\n\n"
    i = s.find(ancla)
    assert i > 0
    i += len(ancla)
    s = s[:i] + ITEM_112 + s[i:]
    assert s.count("v6.50.112 PUBLICADA — EL PEREZOSO") == 1
    print("  item 1 (.112) insertado")

    open(p, "w", encoding="utf-8").write(s)
    print("STABLE-SNAPSHOT.md OK")


def main():
    changes()
    readme()
    snapshot()
    print("DOCS .112 OK")


if __name__ == "__main__":
    main()
