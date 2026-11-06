#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""v6.107 — LA CIRUGÍA DEL ANCLA (parte 1): comentarios y cabecera."""
import io, sys

RUTA = "/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod/Content/Projectiles/AuraJuicio2Ancla.cs"
s = io.open(RUTA, encoding="utf-8").read()
orig = s

def reemplaza(viejo, nuevo, etiqueta):
    global s
    if viejo not in s:
        print(f"FALLO: {etiqueta} — no hallado")
        sys.exit(1)
    if s.count(viejo) != 1:
        print(f"FALLO: {etiqueta} — {s.count(viejo)} ocurrencias")
        sys.exit(1)
    s = s.replace(viejo, nuevo)
    print(f"OK: {etiqueta}")

# ---------------------------------------------------------------- (1) cabecera
# (ya aplicada por el MultiEdit previo — se verifica y se salta)
if "//  AuraJuicio2Ancla — v6.105 → v6.106 → v6.107 — EL ANCLA INVISIBLE DEL" in s:
    print("OK: cabecera versión (ya estaba)")
else:
    print("FALLO: cabecera versión — no hallada en ninguna de sus formas")
    sys.exit(1)

# ------------------------------------------------- (2) la letra nueva + la cura
reemplaza(
    """    //  ir cada vez mas rapido a medida que se acercan al jugador».
    //
    //  LA CURA v6.106 — TODO EL TEATRO COMPARTE EL MISMO ARCO:
    //   · LA MEDIALUNA — la textura REFORJADA disuelve la parte recta
    //     HACIA ADENTRO (isopletas circulares DENTRO de la cúpula; el
    //     rabo de la v6.105 que salía bajo el diámetro MURIÓ): solo
    //     vive la CONCHA del arco — una medialuna de Perlin morada y
    //     negra cuya parte PLANA (la disuelta) mira AL PORTADOR y cuyo
    //     arco se abomba EN DIRECCIÓN OPUESTA a él (el jugador a la
    //     derecha → la medialuna abraza la espalda del impío abriéndose
    //     a la IZQUIERDA).
    //   · LAS CHISPAS DEL ARCO — cada una nace LIBERADA (su centro y su
    //     radio quedan congelados al nacer) y RECORRE el mismo
    //     semicírculo: parte del cuerno de arranque ALEJÁNDOSE del
    //     portador, envuelve la cúpula por la espalda y llega al cuerno
    //     de llegada ACELERANDO — cada vez más rápida en el tramo que
    //     VUELVE hacia el jugador, donde se disuelve.
    //   · LA BRUMA — el MISMO recorrido del arco y la MISMA dirección
    //     (nace lenta en el cuerno de arranque y acelera por la cúpula),
    //     con INTENSIDAD VARIABLE (alfa, tamaño y número de puffs crecen
    //     con la cercanía del impío).
    //
""",
    """    //  ir cada vez mas rapido a medida que se acercan al jugador».
    //
    //  LA CORRECCIÓN DE LA LETRA (v6.107): «tanto las particulas como
    //  la medialuna en los enemigos al usar el aura del juicio, esta
    //  mal, intenta darle una rotacion vertical de 90 grados, o sea
    //  darle la vuelte y las particulas deben nacer del centro de los
    //  enemigos y luego dispersarce hacia fuera pero siempre en la
    //  direccion opuesta al jugador».
    //
    //  LA CURA v6.107 — EL TEATRO SE PONE DE PIE Y EXPULSA DEL CORAZÓN:
    //   · LA MEDIALUNA VERTICAL (LA CÚPULA) — LA ROTACIÓN VERTICAL DE
    //     90° de la letra: la v6.106 ACOSTABA el arco en horizontal (la
    //     parte plana mirando al portador, el domo abombándose lejos de
    //     él) — AHORA la parte PLANA (la disuelta) cae ABAJO, sobre el
    //     corazón del impío, y la concha se YERGUE cubriéndolo desde
    //     arriba: LA CÚPULA DEL JUICIO que pesa sobre su cabeza (la
    //     medialuna YA NO gira con el portador — solo el bamboleo lento
    //     la mece).
    //   · LAS CHISPAS DEL CORAZÓN (PolvoExpulsionJuicio — el polvo
    //     NUEVO): cada una NACE EN EL CORAZÓN del impío y sale
    //     EXPULSADA hacia FUERA, SIEMPRE en la dirección OPUESTA al
    //     portador (jugador a la derecha → nacen del pecho y vuelan a
    //     la IZQUIERDA, abriéndose en abanico — así para TODAS las
    //     direcciones), LIBERADA (la cuna congelada al nacer) y
    //     ACELERANDO suavecito hasta disolverse al final del camino —
    //     la furia escala con la cercanía.
    //   · LA BRUMA DEL CORAZÓN — cada puff NACE en el corazón del
    //     impío y se derrama HACIA FUERA por el MISMO abanico de fuga
    //     (la MISMA dirección de las chispas), con INTENSIDAD VARIABLE
    //     (alfa, tamaño y número de puffs crecen con la cercanía).
    //
""",
    "letra v6.107 + cura")

# ------------------------------------------------------- (3) el vestido índice
reemplaza(
    """    //   2) VISTE A LOS IMPIOS (solo cuando se ACERCAN — el efecto
    //      solo existe en el enemigo): LA MEDIALUNA PERLIN + LAS
    //      CHISPAS QUE LA RECORREN + LA BRUMA QUE LA ENVUELVE — todo
    //      sobre EL MISMO ARCO (ver LA CURA v6.106 arriba).""",
    """    //   2) VISTE A LOS IMPIOS (solo cuando se ACERCAN — el efecto
    //      solo existe en el enemigo): LA MEDIALUNA VERTICAL (la
    //      cúpula) + LAS CHISPAS EXPULSADAS DEL CORAZÓN + LA BRUMA QUE
    //      SE DERRAMA POR EL MISMO RUMBO (ver LA CURA v6.107 arriba).""",
    "índice del vestido")

# ------------------------------------------------------------- (4) AI() índice
reemplaza(
    """            //  2) LAS PARTÍCULAS PEQUEÑAS + LA BRUMA (solo cliente — el
            //     teatro de los impíos; la MISMA DIRECCIÓN que las
            //     partículas originales del aura: expulsadas del flanco
            //     que MIRA al portador, ALEJÁNDOSE de él).""",
    """            //  2) LAS CHISPAS DEL CORAZÓN (solo cliente — el teatro
            //     de los impíos: NACEN del corazón y salen EXPULSADAS
            //     hacia fuera, SIEMPRE en la dirección OPUESTA al
            //     portador — la MISMA fuga de la bruma).""",
    "índice AI()")

# --------------------------------------------------- (5) la dirección de casa
reemplaza(
    """                // LA DIRECCIÓN DE LA CASA — portador → impío: las chispas
                // nacen en el flanco que MIRA al portador y salen
                // EXPULSADAS alejándose (la misma dirección de las
                // partículas originales del aura de juicio).
                Vector2 hacia = n.Center - dueno.MountedCenter;""",
    """                // LA DIRECCIÓN DE LA CASA — portador → impío: las chispas
                // NACEN DEL CORAZÓN del impío y salen EXPULSADAS hacia
                // fuera, SIEMPRE en la dirección OPUESTA al portador
                // (la MISMA fuga de la bruma y de las originales).
                Vector2 hacia = n.Center - dueno.MountedCenter;""",
    "dirección de casa")

io.open(RUTA, "w", encoding="utf-8", newline="\n").write(s)
print(f"\nESCRITO: {len(orig)} → {len(s)} chars")
