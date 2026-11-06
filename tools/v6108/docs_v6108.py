#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""v6.108 — LOS DOCS: README (versión) + STABLE-SNAPSHOT (reescritura completa)."""
import io, sys

# ================================================================
# (1) README — la versión
# ================================================================
RUTA = "/home/z/my-project/download/Aethon-Mod-Terraria/README.md"
s = io.open(RUTA, encoding="utf-8").read()
viejo = "> **Versión actual:** 6.107 · **Target:** tModLoader 2026.9.3.1 (Terraria 1.4.4.9, .NET 8) · **Idiomas:** español siempre (es-ES fuente + espejos es-MX/en-US)"
nuevo = "> **Versión actual:** 6.108 · **Target:** tModLoader 2026.9.3.1 (Terraria 1.4.4.9, .NET 8) · **Idiomas:** español siempre (es-ES fuente + espejos es-MX/en-US)"
assert viejo in s, "README: la línea de la versión no hallada"
io.open(RUTA, "w", encoding="utf-8", newline="").write(s.replace(viejo, nuevo))
print("OK: README versión 6.108")

# ================================================================
# (2) STABLE-SNAPSHOT — la reescritura completa
# ================================================================
SNAP = """# SNAPSHOT ESTABLE — v6.108 — EL ARCO DE CHOQUE Y EL CALENTAMIENTO AERODINÁMICO

> **PUBLICADA — release 436513721 + CDN verificado byte a byte (md5 ea9d8a65890ca91334740141035eda1e)** — .tmod v6.108 (11.576.884 B, 514 entradas, EOF exacto) — **EL CONCEPTO** (el portador es EL PLANETA, su aura LA ATMÓSFERA y cada impío un objeto CAYENDO EN REENTRADA: la física de la reentrada traducida al teatro del vacío) · **LA MEDIALUNA = EL ARCO DE CHOQUE** (la parte ABOMBADA MIRA AL PORTADOR EN TODO MOMENTO: rot = dir.ToRotation() − π/2 + bamboleo — la v6.107 la dejaba DE PIE fija sobre la cabeza; ahora el impío siempre «cae hacia ti» y si lo rodeas, el arco gira contigo; el CENTRO del semicírculo cae sobre el corazón — la parte plana disuelta lo abraza por detrás — y la CONCHA SE ABOMBA hacia el portador hasta ~RadioDomo de su pecho: el bow shock plantado DELANTE del cuerpo) · **EL COLOR DEL CHOQUE** («la medialuna cambia de color volviendose ligeramente morada negra»: cuanto más HONDO cae el impío, el arco SE ENFRÍA EN SOMBRA — el pase alfa Lerp(negroVioleta→moradaNegra(46,20,78)) y el aditivo Lerp(violeta→moradaNegra·0.85k): de lejos la medialuna violeta de siempre, pegado una concha morada-negra) · **EL PLASMA DEL PUNTO DE ESTANCAMIENTO** (cada chispa NACE EN EL TOPE ABOMBADO del arco — el punto que MIRA al portador, donde el choque comprime y arde: cuna = n.Center − dir·RadioDomo·0.95 + temblor — y se derrama hacia ATRÁS en TODAS las direcciones de fuga, el abanico trasero COMPLETO ±π/2: cualquier rumbo que se aleje del portador, el flujo del choque rodeando el cuerpo y dejando la estela de la reentrada, más larga y desgarrada: RadioDomo×1.6–2.6) · **LA DENSIDAD DE LA ATMÓSFERA** («a mayor cercania mas particulas»: ritmo Clamp(6−5k) — cada 6 ticks en el borde → CADA TICK pegado, en ráfagas de 1→3) · **LA BRUMA HACE LO MISMO QUE LAS PARTÍCULAS** (cada puff NACE en el MISMO punto de estancamiento, se derrama por el MISMO abanico de fuga ±π/2 y su densidad crece 2→5 puffs con la cercanía; la v6.107 clavaba 3+1) · **TOOLTIPS ×3** (la Linea3 del aura 2 cuenta el CALENTAMIENTO AERODINÁMICO: el meteoro cayendo hacia ti, el arco mirándote, el plasma de la parte abombada y el morado negro — es-ES + en-US traducido + el espejo es-MX := es-ES) — oráculo 0/0 (453 .cs), -build REAL 0/0 ×2, AUDIT v6.108 TODO VERIFICADO (firmas VIVAS: el arco rot=dir−π/2, la cuna del estancamiento, el abanico ±π/2 ×2, el ritmo 6→1, las ráfagas 1→3, los puffs 2→5, la estela 1.6–2.6, el morado negro Lerp ×2 · MUERTAS: el abanico ±0.85, el ritmo 4→2, los puffs 3+1, la bruma del corazón, la estela corta · la medialuna Perlin intacta en el paquete: corazón 0, concha 255, isopletas, cuernos, premultiplicado · hjson ×3 paquete==fuente byte a byte + tooltip nuevo ×3 + 0 IP y 0 CJK en 453 archivos), headless 0 EXCEPCIONES.

## LA LETRA (el usuario)

«la parte abombada de la medialuna tiene que mirarme a mi en todo momento, deben nacer del centro de la medialuna en la parte abombada y extenderse en todas las direcciones pero siempre alejandose de mi, a mayor cercania mas particulas y la medialuna cambia de color volviendose ligeramente morada negra, y la bruma debe hacer lo mismo que las particulas. el objetivo es que parezca calentamiento aerodinamico igual que las cosas que entran a la atmosfera del planeta, pero en este caso yo seria el planeta y todo lo que entre en mi atmosfera que seria al aura del juicio, sufriria de calentamiento aerodinamico, entiendes el concepto»

## PARA VERIFICAR EN JUEGO (la checklist v6.108)

1. **EL ARCO DE CHOQUE**: equipa el Aura de Juicio 2 y quema un enemigo — la medialuna morada y negra tiene ahora LA PARTE ABOMBADA MIRÁNDOTE EN TODO MOMENTO: el impío «cae» hacia ti como un meteoro y el arco se planta DELANTE de su cuerpo (entre él y tú, abombándose hacia tu posición). RODEA al enemigo lentamente: el arco GIRA contigo, siempre mirándote — solo el bamboleo lento lo mece (la v6.107 lo dejaba fijo coronándolo de arriba: eso MURIÓ).
2. **EL PLASMA**: las chispas nacen del PUNTO MÁS CERCANO A TI del arco (el tope abombado — el punto de estancamiento de la reentrada) y se derraman HACIA ATRÁS en TODAS las direcciones que se alejan de ti (el abanico trasero ±90° rodea el cuerpo del enemigo y deja la estela, como el plasma desgarrándose de un objeto en reentrada). Si el enemigo camina, las chispas ya nacidas terminan su vuelo solas (liberadas).
3. **EL COLOR**: acerca al enemigo a tu atmósfera — cuanto más HONDO cae, la medialuna SE ENFRÍA EN SOMBRA: el violeta brillante se apaga hacia el MORADO NEGRO (de lejos es la medialuna violeta de siempre; pegado a ti, una concha morada-negra).
4. **LA DENSIDAD Y LA BRUMA**: a mayor cercanía, MÁS PARTÍCULAS — en el borde de tu aura caen escasas (una cada 6 ticks); pegado a ti HIERVEN (cada tick, en ráfagas de hasta 3, con la estela más larga). La BRUMA hace LO MISMO que las chispas: nace del mismo punto abombado, se derrama por los mismos rumbos de fuga y engorda de 2 a 5 puffs con la cercanía. Y el tooltip del aura 2 (español o inglés) ya cuenta el calentamiento aerodinámico.
"""
RUTA2 = "/home/z/my-project/download/Aethon-Mod-Terraria/STABLE-SNAPSHOT.md"
io.open(RUTA2, "w", encoding="utf-8", newline="").write(SNAP)
print("OK: STABLE-SNAPSHOT reescrito a la v6.108")
