#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
docs_v6176.py — v6.176 — LA DOCUMENTACIÓN DE LA VERSIÓN: el bump de
build.txt, la letra de la sesión en CHANGES.md y las reglas nuevas en
MAESTRO.md (R21 — el patrón del lote del PreDraw, la lección de los
dos crashes del client.log).
"""
import pathlib
import sys

RAIZ = pathlib.Path(__file__).resolve().parent.parent
F_BUILD = RAIZ / "AethonMod" / "build.txt"
F_CHANGES = RAIZ / "CHANGES.md"
F_MAESTRO = RAIZ / "MAESTRO.md"

# ---------- (1) LA VERSIÓN ----------
V1 = "version = 6.175"
N1 = "version = 6.176"

# ---------- (2) CHANGES.md ----------
ENTRADA = """## v6.176 — LA ASPIRADORA QUE ESPERA, EL GUSANO QUE CALLA Y EL ARMA DEL CÍCLOPE — la letra del usuario (sesión 29: «algo fallo en el mundo interior al inveocar al Devoramundos la barra de vida se desborda» + los crashes del client.log + la lista: los textos desbordados · el checklist del trío · el Faro horrible · los jefes devorados siguen disparando · el asco en la mente · la animación del Devorador · el Cíclope arma · la carpeta tools · la bruma de la base · las 194 claves · el azar del dibujo)

1. **LA CURA DE LOS DOS CRASHES DEL client.log** (211 repeticiones registradas ≈ 42.000 frames de spam — «Draw was called, but Begin has not yet been called»): la volea de la arena cerraba el lote de vanilla y dibujaba SIN abrir el suyo; el halo de la aspiradora dibujaba tras el End del libro. LA CURA es el patrón de la casa blindado: `VFXCore.AbrirAdditive()` al entrar y `VFXCore.RestaurarLoteVanilla()` al salir — y NUEVA REGLA R21 en el MAESTRO: el PreDraw que dibuja directo JAMÁS dibuja con el lote cerrado (los crashes de render no los caza el oráculo: solo el patrón).
2. **LA BARRA DEL DEVORADOR NO REBOSA**: el pilar solo sellaba a la CABEZA — ahora el gusano se sella ENTERO (cada cuerpo/cola que la IA engendra hereda el sello por SetDefaults, como SEGMENTO: stats ×1.5 y SU barra, sin aura, sin motor, sin regen, sin contar kills) y BarraJefeInterior para EoW/Destructor suma la vida de TODAS las piezas (el número total VIVO, la fracción verdadera) con la vida CLAMPADA al máximo — el rebose es matemáticamente imposible.
3. **LOS JEFES DEVORADOS CALLAN** («en el momento en el que empiezan a ser devorado ellos deben dejar de lanzar proyectiles»): el festín y la cena ya congelaban a su preso, pero la ASPIRADORA dejaba viva la IA del gusano (seguía escupiendo mientras el libro comía). LA CURA: `FaucesGlobalNPC.CongeladoPorAspiradora` — el cine lo pone al capturar, el PreAI lo respeta ANTES que nada (ni IA, ni UN proyectil) y el motor de combate lo consulta; al morir el cine, `DescongelarAspiradora()` barre el mundo (jamás un gusano zombi).
4. **LA ASPIRADORA REESCRITA** («el Devoramundos flota completo en el aire por piezas, cada pieza individual flota y todas son devoradas al mismo tiempo… debe esperar a que el Devoramundos se genere por completo, tal vez en 5 segundos»): LA VIGILIA (5 s de la letra o 90 t de cadena estable — el ojo del libro SIGUE a la bestia y el ANILLO DE LA ESPERA se llena) → LA CAPTURA (todas las piezas congeladas e intangibles) → LA FLOTACIÓN (cada pieza SUBE con SU vaivén propio y la cadena se DESENREDA) → EL TRAGO (TODAS vuelan a la boca A LA VEZ y mueren A LA VEZ — muerte real, loot normal) → EL ASCO (el libro se estremece, se cierra y… → punto 5) → EL POSO.
5. **EL ASCO HABLA EN LA MENTE**: el comentario del grimorio pasa del chat a `EcoRed.HablarAlPortador` — la máquina de escribir dramática del EcoLib (la letra: «debe ser dicho con la caracteristica forma del grimorio de hablar en la mente del jugador no en el chat»).
6. **LA INTERFAZ DEL PILAR Y EL ALTAR**: LAS TIJERAS DEL LIBRO — el pase del cuerpo corre con recorte de tijeras sobre las dos páginas (lo que no cabe en el papel NO SE DIBUJA: el desborde es imposible, venga de la fuente dinámica o de lo que venga) · los botones RE-MIDEN al momento del trazo · TintaApagado OSCURECE (118,96,70→82,60,40) y el halo blanco del texto se vuelve MANSO (grosor que escala, un punto menos de alfa — «el color de los textos es demasiado claro») · EL CHECKLIST UNIFORME: la casilla «LAS TRES A LA VEZ» pierde la palomita y vive como las del albedrío — el cuadradito se RELLENA de morado.
7. **EL FARO REDISEÑADO**: «se ve horrible rediseñarla» — la torre afinada con broches dorados y ventanas ámbar, la linterna con EL ECLIPSE (disco negro + corona de picos), los rayos continuos con gradiente y núcleo (QA del OJO: 6.5 → 7.5 SHIP).
8. **EL ARMA DEL CÍCLOPE PROYECTADO**: «lo que se tenia que crear era un arma» — ProyecciónDelCiclope (la cabeza pálida del maniquí y SU ojo: el telegraph del vídeo — el ojo GRITA creciendo sobre el portador — y el doble rayo magenta de StormLib, penetración infinita; el ítem por la forja, 5 QAs del OJO) + la entrega DIRECTA al inventario (el patrón del Faro) + la bolsa de investigaciones (R8).
9. **LA CARPETA tools/ SALE DEL MOD**: las herramientas son DEL DESARROLLO (mías), no del mod — `buildIgnore` ya las excluía del .tmod; ahora viven donde manda el protocolo: `tools/` de la RAÍZ del repo (audit_assets.py y gen_absoluto_v684.py movidas con su historia git).
10. **LA BRUMA DE LA BASE DE LOS TENTÁCULOS**: «cada tentaculo de fondo debe tener en su base una bruma de esa forma no se ve cortado» — las tres capas del cielo interior (lejos · medio · cerca) disuelven su punta en BrumaFX.Puff de SU MISMA tinta: el tentáculo se apaga, no se corta.
11. **LAS 194 CLAVES EN-ONLY CLASIFICADAS Y PURGADAS**: 189 MUERTAS purgadas del en-US (179 DisplayName de proyectiles — placeholders del nombre de clase que nadie dibuja jamás · 7 Tooltips vacíos · 3 Pestana del pergamín muerto) + 5 VIVAS añadidas al es-ES (los nombres de Bolsa Maestra, Bolsa de la Forma Eterna, Coro de los Profundos, Pluma del Escriba Santo y El Cronista — el español los recibía por el fallback). PARIDAD PERFECTA: 1849 = 1849, cero huérfanas (antes 194).
12. **EL AZAR DEL DIBUJO SEMBRADO** («semilla fija por entidad (whoAmI·edad), como ya hace TentaculoCosmico»): las 7 llamadas REALES de Main.rand en el dibujo (3 motas de ElCronista + 4 migas del JuicioDosSoles) ahora nacen de `VFXCore.Hash01(whoAmI·edad)` — la misma migaja en todas las máquinas; las otras 3 menciones eran COMENTARIOS (PrimerTrazoHeld y AlasAuroraHalo ya estaban sembrados desde su era) y el loop ahora mide SOLO código (limpia comentarios antes de contar).

**GATES**: oráculo 0/0 · -build REAL 0/0 · GUARDIÁN TODO VIVE ✔ (ProyeccionDelCiclope.png con su clase) · AUDIT v6.176 TODO VERIFICADO ✔ · headless 0 excepciones · loop_retro sin regresiones (azar 10→0 medido en código · claves EN-sin-ES 194→0).

"""

# ---------- (3) MAESTRO.md ----------
V_MAESTRO_V = "**Versión actual** | **6.175** (vive en `AethonMod/build.txt`, línea `version = 6.175`)"
N_MAESTRO_V = "**Versión actual** | **6.176** (vive en `AethonMod/build.txt`, línea `version = 6.176`)"

V_R20 = """| R20 | **EL LOOP DE RETROALIMENTACIÓN corre antes de cada release** (`tools/loop_retro_v6175.py`): mide catches vacíos · azar en el dibujo · claves de loc · PNGs y COMPARA con la medición anterior | medir → comparar → arreglar |"""

N_R20 = V_R20 + """
| R21 | **EL PREQUEL DEL PREDRAW** (v6.176, la lección de los dos crashes del client.log — 42.000 frames de «Draw was called, but Begin has not yet been called»): TODO PreDraw/PostDraw que dibuja DIRECTO con `Main.spriteBatch.Draw` abre SU lote con `VFXCore.AbrirAdditive()` (o un `Begin` propio) y sale con `VFXCore.RestaurarLoteVanilla()` — JAMÁS un Draw tras un `End` sin `Begin`. El oráculo NO caza los errores de estado de dibujo: solo el patrón los evita | Los dos crashes nacieron el mismo día que el patrón se escribió: la regla es el único antídoto |
| R22 | **EL AZAR DEL DIBUJO SE SIEMBRA** (v6.176, la letra: «semilla fija por entidad (whoAmI·edad), como ya hace TentaculoCosmico»): el polvo decorativo que nace DENTRO del dibujo usa `VFXCore.Hash01(whoAmI·edad, a, b)` — NUNCA `Main.rand` en PreDraw/PostDraw. El loop mide SOLO código (los comentarios no cuentan) | R5 medido y blindado |"""

V_CRONO = """**La era actual (v6.12x-135, el Mundo Interior y las alas)**"""
N_CRONO = """**La era v6.176**: LA ASPIRADORA QUE ESPERA (la vigilias de 5 s de la letra + el trago simultáneo) · EL GUSANO QUE CALLA (CongeladoPorAspiradora) · EL ARMA DEL CÍCLOPE (ProyecciónDelCiclope) · LAS TIJERAS DEL LIBRO (nada desborda el papel) · LA PURGA DEL EN (194→0) · EL AZAR SEMBRADO (7 llamadas reales) · R21/R22 en el MAESTRO.

**La era actual (v6.12x-135, el Mundo Interior y las alas)**"""


def main() -> int:
    # (1) build.txt
    t = F_BUILD.read_text(encoding="utf-8")
    if N1 not in t:
        assert V1 in t
        F_BUILD.write_text(t.replace(V1, N1, 1), encoding="utf-8")
        print("✔ build.txt → 6.176")

    # (2) CHANGES.md
    t = F_CHANGES.read_text(encoding="utf-8")
    if "## v6.176 —" not in t:
        F_CHANGES.write_text(ENTRADA + t, encoding="utf-8")
        print("✔ CHANGES.md: la letra de la sesión 29 documentada")

    # (3) MAESTRO.md
    t = F_MAESTRO.read_text(encoding="utf-8")
    if N_MAESTRO_V not in t:
        assert V_MAESTRO_V in t
        t = t.replace(V_MAESTRO_V, N_MAESTRO_V, 1)
        print("✔ MAESTRO: versión 6.176")
    if "R22" not in t:
        assert V_R20 in t
        t = t.replace(V_R20, N_R20, 1)
        print("✔ MAESTRO: R21 (el lote del PreDraw) y R22 (el azar sembrado)")
    if "La era v6.176" not in t:
        assert V_CRONO in t
        t = t.replace(V_CRONO, N_CRONO, 1)
        print("✔ MAESTRO: cronología v6.176")
    F_MAESTRO.write_text(t, encoding="utf-8")
    return 0


if __name__ == "__main__":
    sys.exit(main())
