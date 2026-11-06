#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
v6.108 — LA CIRUGÍA DE LOS TOOLTIPS ×3 + build.txt + docs del polvo.

La letra: «la parte abombada de la medialuna tiene que mirarme a mi en
todo momento, deben nacer del centro de la medialuna en la parte
abombada y extenderse en todas las direcciones pero siempre alejandose
de mi, a mayor cercania mas particulas y la medialuna cambia de color
volviendose ligeramente morada negra, y la bruma debe hacer lo mismo
que las particulas» — EL CALENTAMIENTO AERODINÁMICO.

  (1) PolvoExpulsionJuicio.cs — SOLO la documentación del contrato
      (nace del PUNTO DE ESTANCAMIENTO, abanico de fuga ±90°): la
      geometría de la clase (Centro/Dir/Vel/Acel/DistMax) sirve igual.
  (2) Los tooltips ×3: la Linea3 del aura 2 cuenta el ARCO DE CHOQUE
      mirando al portador + el plasma de la parte abombada (es-ES
      editado + en-US traducido + es-MX := es-ES, la regla del sync).
  (3) build.txt 6.107 → 6.108.
"""
import io, sys

def reemplaza_en(s, viejo, nuevo, etiqueta, ruta):
    if viejo not in s:
        print(f"FALLO: {etiqueta} — no hallado en {ruta}")
        sys.exit(1)
    if s.count(viejo) != 1:
        print(f"FALLO: {etiqueta} — {s.count(viejo)} ocurrencias en {ruta}")
        sys.exit(1)
    print(f"OK: {etiqueta} ({ruta.rsplit('/', 1)[-1]})")
    return s.replace(viejo, nuevo)

# ================================================================
# (1) PolvoExpulsionJuicio.cs — las docs del contrato
# ================================================================
RUTA_POLVO = "/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod/Content/Dusts/PolvoExpulsionJuicio.cs"
s = io.open(RUTA_POLVO, encoding="utf-8").read()
orig = s

s = reemplaza_en(s,
    """    /// <summary>
    /// PolvoExpulsionJuicio — v6.107 — LA CHISPA EXPULSADA DEL CORAZÓN.
    ///
    /// LA LETRA DEL USUARIO (la corrección del aura de juicio 2): «las
    /// particulas deben nacer del centro de los enemigos y luego
    /// dispersarce hacia fuera pero siempre en la direccion opuesta al
    /// jugador».
    ///
    /// EL CONTRATO: cada chispa NACE EN EL CORAZÓN del impío (una cuna
    /// con un temblor mínimo) y sale EXPULSADA hacia FUERA, SIEMPRE en
    /// la dirección OPUESTA al portador (el jugador a la derecha → la
    /// chispa nace del pecho y vuela a la IZQUIERDA, abriéndose en
    /// abanico hacia afuera — así para TODAS las direcciones). La
    /// geometría queda CONGELADA al nacer (LIBERADA: aunque el impío
    /// camine, la chispa termina su vuelo sola) — el rumbo es RECTO y
    /// la velocidad ACELERA suavecito cada tick (el vacío la escupe
    /// cada vez con más rabia) hasta disolverse en la última cuarta
    /// parte del camino.
    ///
    /// Tuberías de la casa: el Update devuelve FALSE — TODO el
    /// movimiento lo escribe la geometría de la expulsión (la física
    /// vanilla no participa: ni gravedad ni velocidad). El dibujo es
    /// el de la casa: NÚCLEO NEGRO (el vacío que oscurece) + HALO
    /// VIOLETA ESTIRADO por el rumbo (la estela del vuelo — la chispa
    /// SE LEE en movimiento) + PERLA SÓLIDA al centro.
    /// </summary>""",
    """    /// <summary>
    /// PolvoExpulsionJuicio — v6.107 → v6.108 — LA CHISPA DEL PLASMA
    /// (la reentrada del impío).
    ///
    /// LA LETRA DEL USUARIO (v6.108): «deben nacer del centro de la
    /// medialuna en la parte abombada y extenderse en todas las
    /// direcciones pero siempre alejandose de mi, a mayor cercania mas
    /// particulas» — EL CALENTAMIENTO AERODINÁMICO: el portador es el
    /// planeta, su aura la atmósfera, el impío cae en reentrada y ESTA
    /// chispa es el plasma que se desprende del choque.
    ///
    /// EL CONTRATO: cada chispa NACE EN EL PUNTO DE ESTANCAMIENTO (el
    /// tope ABOMBADO de la medialuna — el punto del arco que MIRA al
    /// portador, donde el choque comprime y arde; una cuna con un
    /// temblor mínimo) y se derrama hacia ATRÁS en TODAS las
    /// direcciones de fuga (el abanico trasero COMPLETO ±90°: cualquier
    /// rumbo que SE ALEJE del portador — el plasma rodea el cuerpo y
    /// deja la estela de la reentrada). La geometría queda CONGELADA
    /// al nacer (LIBERADA: aunque el impío camine, la chispa termina
    /// su vuelo sola) — el rumbo es RECTO y la velocidad ACELERA
    /// suavecito cada tick (el vacío la escupe cada vez con más rabia)
    /// hasta disolverse en la última cuarta parte del camino.
    ///
    /// Tuberías de la casa: el Update devuelve FALSE — TODO el
    /// movimiento lo escribe la geometría de la expulsión (la física
    /// vanilla no participa: ni gravedad ni velocidad). El dibujo es
    /// el de la casa: NÚCLEO NEGRO (el vacío que oscurece) + HALO
    /// VIOLETA ESTIRADO por el rumbo (la estela del vuelo — la chispa
    /// SE LEE en movimiento) + PERLA SÓLIDA al centro.
    /// </summary>""",
    "polvo: doc de clase", RUTA_POLVO)

s = reemplaza_en(s,
    """            /// <summary>El corazón del impío AL NACER la chispa —
            /// LIBERADA: ya no lo sigue, termina su vuelo sola.</summary>""",
    """            /// <summary>El punto de estancamiento AL NACER la chispa
            /// (el tope abombado de la medialuna) — LIBERADA: ya no lo
            /// sigue, termina su vuelo sola.</summary>""",
    "polvo: doc Centro", RUTA_POLVO)

s = reemplaza_en(s,
    """            /// <summary>EL RUMBO — la dirección de la expulsión (SIEMPRE
            /// dentro del abanico OPUESTO al portador).</summary>""",
    """            /// <summary>EL RUMBO — la dirección de la expulsión (SIEMPRE
            /// dentro del HEMISFERIO DE FUGA: el abanico ±90° que se
            /// aleja del portador).</summary>""",
    "polvo: doc Dir", RUTA_POLVO)

if s != orig:
    io.open(RUTA_POLVO, "w", encoding="utf-8", newline="").write(s)
    print(f"GUARDADO: {RUTA_POLVO}")

# ================================================================
# (2) Los tooltips ×3 — LA Linea3 del aura 2
# ================================================================
COMENTARIO_VIEJO = """		// v6.105 → v6.107: EL AURA DE JUICIO 2 — la copia silenciosa
		// (la letra: partículas más pequeñas, el efecto del estandarte,
		// NADA sobre el jugador, morado y negro, bruma y el semicírculo
		// Perlin; la v6.107 lo pone DE PIE — la cúpula vertical, las
		// chispas naciendo del corazón y huyendo del portador)."""
COMENTARIO_NUEVO = """		// v6.105 → v6.108: EL AURA DE JUICIO 2 — la copia silenciosa
		// (la letra: partículas más pequeñas, el efecto del estandarte,
		// NADA sobre el jugador, morado y negro, bruma y el semicírculo
		// Perlin; la v6.108 lo vuelve CALENTAMIENTO AERODINÁMICO — el
		// arco de choque mirando al portador, el plasma naciendo de su
		// parte abombada y el morado negro de la reentrada)."""

# ---- es-ES
RUTA_ES = "/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod/Localization/es-ES_Mods.AethonMod.hjson"
s = io.open(RUTA_ES, encoding="utf-8").read()
orig = s
s = reemplaza_en(s, COMENTARIO_VIEJO, COMENTARIO_NUEVO, "es-ES: comentario", RUTA_ES)
s = reemplaza_en(s,
    """		Linea3: "[c/B96EEA:Sobre cada impío: LA CÚPULA PERLIN que se yergue vertical sobre su cabeza (la parte plana disuelta abajo), chispas moradas y negras que NACEN DE SU CORAZÓN y salen expulsadas SIEMPRE en tu dirección contraria, y bruma que se derrama por el mismo rumbo]\"""",
    """		Linea3: "[c/B96EEA:CALENTAMIENTO AERODINÁMICO: cada impío cae hacia ti como un meteoro — EL ARCO DE CHOQUE (la medialuna) lo envuelve con la parte abombada MIRÁNDOTE EN TODO MOMENTO; de ese punto brotan chispas y bruma que se alejan de ti en TODAS direcciones (cuanto más cerca, más densas) y el arco se enfría en MORADO NEGRO]\"""",
    "es-ES: Linea3", RUTA_ES)
if s != orig:
    io.open(RUTA_ES, "w", encoding="utf-8", newline="").write(s)
    print(f"GUARDADO: {RUTA_ES}")

# ---- es-MX := es-ES (la regla del sync de la casa)
RUTA_MX = "/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod/Localization/es-MX_Mods.AethonMod.hjson"
s = io.open(RUTA_MX, encoding="utf-8").read()
orig = s
s = reemplaza_en(s, COMENTARIO_VIEJO, COMENTARIO_NUEVO, "es-MX: comentario", RUTA_MX)
s = reemplaza_en(s,
    """		Linea3: "[c/B96EEA:Sobre cada impío: LA CÚPULA PERLIN que se yergue vertical sobre su cabeza (la parte plana disuelta abajo), chispas moradas y negras que NACEN DE SU CORAZÓN y salen expulsadas SIEMPRE en tu dirección contraria, y bruma que se derrama por el mismo rumbo]\"""",
    """		Linea3: "[c/B96EEA:CALENTAMIENTO AERODINÁMICO: cada impío cae hacia ti como un meteoro — EL ARCO DE CHOQUE (la medialuna) lo envuelve con la parte abombada MIRÁNDOTE EN TODO MOMENTO; de ese punto brotan chispas y bruma que se alejan de ti en TODAS direcciones (cuanto más cerca, más densas) y el arco se enfría en MORADO NEGRO]\"""",
    "es-MX: Linea3 (espejo := es-ES)", RUTA_MX)
if s != orig:
    io.open(RUTA_MX, "w", encoding="utf-8", newline="").write(s)
    print(f"GUARDADO: {RUTA_MX}")

# ---- en-US (la traducción)
RUTA_EN = "/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod/Localization/en-US_Mods.AethonMod.hjson"
s = io.open(RUTA_EN, encoding="utf-8").read()
orig = s
s = reemplaza_en(s, COMENTARIO_VIEJO, COMENTARIO_NUEVO, "en-US: comentario", RUTA_EN)
s = reemplaza_en(s,
    """		Linea3: "[c/B96EEA:Upon each fiend: THE PERLIN DOME standing upright over its head (the flat side dissolved below), tiny purple-and-black sparks BORN FROM ITS HEART and cast forth ALWAYS away from you, and haze pouring down the same trail]\"""",
    """		Linea3: "[c/B96EEA:AERODYNAMIC HEATING: each fiend falls toward you like a meteor — the SHOCK ARC (the crescent) wraps it with its bulging side ALWAYS FACING YOU; from that very point spring sparks and haze drifting away from you in EVERY direction (the closer, the denser) while the arc cools into BLACK PURPLE]\"""",
    "en-US: Linea3", RUTA_EN)
if s != orig:
    io.open(RUTA_EN, "w", encoding="utf-8", newline="").write(s)
    print(f"GUARDADO: {RUTA_EN}")

# ================================================================
# (3) build.txt — la versión
# ================================================================
RUTA_BUILD = "/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod/build.txt"
s = io.open(RUTA_BUILD, encoding="utf-8").read()
orig = s
s = reemplaza_en(s, "version = 6.107", "version = 6.108", "build.txt: versión", RUTA_BUILD)
if s != orig:
    io.open(RUTA_BUILD, "w", encoding="utf-8", newline="").write(s)
    print(f"GUARDADO: {RUTA_BUILD}")

print("\nCIRUGÍA DE TOOLTIPS COMPLETA")
