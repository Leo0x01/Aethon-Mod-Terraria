#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# docs_v650110.py — CHANGES entrada .110 + README 6.50.110 + STABLE-SNAPSHOT
# (header + fila HISTORIAL + item 1 de PRÓXIMOS con la .109→2, .108→3,
# .107→4 y la .106 PODADA — doc-rot de la casa).
import re

BASE = "/home/z/my-project/download/Aethon-Mod-Terraria/"

CHANGES = """# AethonMod — Historial de Cambios

## Commit v6.50.110 — LA PRIMERA CENA, LAS TRES VENTANAS, LA ESENCIA DE HAMBRE Y EL ANÁLISIS

**La letra**: «analisa si las estadisticas del grimorio eterno aplican
realmente» + «la segunda ventana de info del grimorio tiene demasiada
informacion, crea una nueva ventana de informacion para repartir mejor
la informacion» + «crea un item que le de hambre al grimorio eterno
para comprobar si funciona, que suba el hambre de 5 en 5 y que no sea
consumible» + «asegurate de que el grimorio eterno tenga la sombra de
la pagina para cuando sale a cazar y que ademas cuando se mata un jefe
su animacion de muerte se detenga y sea deborado por el grimorio usando
la sombra de la pagina, el jugador recibe loot ya que cuenta como una
muerte normal, lo que sucede es que el jugador ataca al jefe y cuando
llegue a 100 de vida el jefe se detiene circulos magicos como los de
los agujeros negros aprisionan al jefe, lo levantan y llevan al centro
de la pantalla y luego del grimorio sale la sombra de la pagina y
devora por completo al jefe, que esto suceda la primera vez que se
mate un jefe y solo la primera vez, ya despues de la primera vez el
jefe muere de forma normal sin que el grimorio intervenga».

**EL CAMBIO**: cinco entregas — el análisis, las ventanas, el ítem, la
verificación de la caza y EL CINE del primer jefe:

- **EL ANÁLISIS (aplican TODAS)**: las 24 líneas de la segunda ventana
  verificadas UNA A UNA contra su punto de aplicación real — daño
  mágico/knockback/uso (`GrimoireEternal.ModifyWeaponDamage·Knockback·
  UseTimeMultiplier`), invocación+crítico+penetración+vida/maná/slots
  (`ShardPlayer.PostUpdateEquips`), regeneración (`PostUpdateBuffs`),
  daño recibido (`ModifyHurt`), los ojos (`OjoEternoMinion`: daño del
  rayo, velocidad, cadencia, rango), la cadena
  (`TruenoPerlinProjectile.Strike`), el área (`CosmicProjectileFX`), el
  lifesteal (`GlobalNPCXP.OnHitByItem·Projectile`) y el mana bajo (por
  golpe + persistente). Las etiquetas quedaron HONESTAS: «daño mágico
  (este libro)», «crítico mágico», «penetración de armadura mágica»,
  la nota al pie de postura en EL ARMA (sostenido vs barra rápida) y
  la del nivel de invocación en EL FESTÍN.
- **LAS TRES VENTANAS**: la vista completa de la .108 se PARTE en dos
  y el clic derecho cicla BÁSICA → **EL ARMA** (DAÑO·RECURSOS·
  PROYECTIL·BONUS + notas) → **EL FESTÍN** (OJOS DEL ETERNO·LA PRIMERA
  CENA·PROGRESIÓN con la barra de hambre y su estado debajo) → BÁSICA
  (`TooltipToggleItem.VistaEstadisticas` 0/1/2; la línea final del
  tooltip dice SIEMPRE a cuál vas).
- **LA ESENCIA DE HAMBRE (ítem de pruebas)**: +5% de hambre AL ETERNO
  por uso («de 5 en 5»), pila 1, **NO consumible**, la voz del apetito
  con su cifra real, en MP la barra viaja
  (`EcoRed.SincronizarHambre`), en la **Bolsa Probador**, textura
  24×24 propia (la página negra de borde violáceo con el iris rojo del
  hambre) + hjson ×3 con la indentación de CADA archivo (tabs es-ES/
  en-US, espacios es-MX).
- **LA SOMBRA DE LA CAZA — VERIFICADA**: la cadena
  `UpdateInventory → PasoCaza → espíritu (ai[2]=Eterno) →
  SombraPaginaCaza` vive intacta: del 25% de hambre el Eterno sale a
  cazar y SU sombra de la página devora presas enteras (XP del libro +
  la dieta por rareza).
- **LA PRIMERA CENA — el festín del primer jefe** (`FaucesGlobalNPC`
  estado 3 + `PrimeraCenaProjectile`): el jefe llega a **100 de vida**
  → SE DETIENE (vida clavada en 100, inmune, sin IA ni animación) si
  el portador lo peleó (`playerInteraction`), lleva el Eterno en la
  barra rápida y ese tipo de jefe aún no ha sido devorado
  (`ShardPlayer.JefesDevorados` — persiste en el .plr: «solo la
  primera vez»; después, muerte normal sin intervención). LA
  COREOGRAFÍA (430 t ≈ 7,2 s): TRES sellos erosionados (los de los
  agujeros negros, `OrbitaLib.SigiloErosionado`) se materializan
  alrededor del jefe (0-40), CONTRAEN y APRISIONAN (40-60), lo
  LEVANTAN (−90 px) y lo LLEVAN al CENTRO VIVO de la pantalla (60-150
  — si el portador camina, el preso lo sigue colgado del centro), el
  sello íntimo se cierra (150-175), **DEL GRIMORIO sale LA SOMBRA DE
  LA PÁGINA** (la mano del portador) que lo devora por completo
  (175-345: `ColumnaViva`+`Masa` fusiforme+`Devorador` v2 de elipse
  completa+almas-ojo volando) mientras los sellos SE ENCIENEN en él
  (radio → 0), y EL TRAGO (345-430) inhala la elipse entera hacia la
  punta. A los 430 LA MUERTE REAL (2ª pasada de `CheckDead`): **LOOT
  COMPLETO**, bestiario, banners y flags downed* — la
  `PurgaVisualMuerte` tapa el gore («quedando solo su loot»). Los
  worms enroscan sus segmentos alrededor del centro (determinista por
  whoAmI); UNA cena a la vez en el mundo; y LAS SOMBRAS RESPETAN EL
  FESTÍN AJENO: `SombraPaginaCaza` y `SombraPaginaProjectile` SUELTAN
  a cualquier presa `EnFauces` y `AbsorberPresa` la rechaza (defensa
  en profundidad — sin eso la sombra de la caza se comía al jefe del
  cine sin loot).

**VERIFICACIÓN**: audit_texturas 0 · oráculo 0/0 · build real 0/0 ·
headless «Sandboxing v6.50.110 → world gen → Server started» 0
excepciones · audit_v650110 TODO OK (184 ✓): .tmod 2.601.415 B md5
e8f1ba78facfc4208a83c475c0cd1f7f, 388 entradas (387 +
EsenciaHambre.rawimg), EOF exacto, hjson semántico == árbol ×3 + es-MX
byte a byte, PrimeraCenaProjectile/ArrancarPrimeraCena/EnFauces/
JefesDevorados/VistaEstadisticas VIVOS en #Strings, los ldstr de las
tres ventanas y LA PRIMERA CENA VIVOS en #US (UTF-16), las constantes
del cine en el IL (430/345/175/150/−90/3.1/0.92/0.80/0.45/62/0.05) +
la regresión .103–.109 ÍNTEGRA.

**PUBLICACIÓN**: release 410075869 + asset AethonMod.tmod 632401101
2.601.415 B (md5 e8f1ba78…), CDN verificado byte a byte,
/releases/latest = v6.50.110.

"""


def changes():
    p = BASE + "CHANGES.md"
    s = open(p, encoding="utf-8").read()
    # quita el header viejo y pon el nuevo
    s = s.replace("# AethonMod — Historial de Cambios\n\n", "", 1)
    s = CHANGES + s
    open(p, "w", encoding="utf-8").write(s)
    print("CHANGES.md OK")


def readme():
    p = BASE + "README.md"
    s = open(p, encoding="utf-8").read()
    s = re.sub(r"\*\*Versión actual:\*\* 6\.50\.109",
               "**Versión actual:** 6.50.110", s, count=1)
    open(p, "w", encoding="utf-8").write(s)
    assert "6.50.110" in s
    print("README.md OK")


def snapshot():
    p = BASE + "STABLE-SNAPSHOT.md"
    s = open(p, encoding="utf-8").read()

    # 1) el header
    m = re.search(r"(# AethonMod — ESTADO ACTUAL \(v6\.50\.109\)\n\n"
                  r"> \*\*Este documento.*?\n)(.*?)(\n## )", s, re.S)
    assert m, "no encontré el header del snapshot"
    header_nuevo = """# AethonMod — ESTADO ACTUAL (v6.50.110)

> **Este documento = "¿por dónde nos quedamos?"** — se actualiza en cada entrega.
> Última actualización: v6.50.110 (LA PRIMERA CENA, LAS TRES VENTANAS, LA
> ESENCIA DE HAMBRE Y EL ANÁLISIS. La letra: «analisa si las estadisticas
> del grimorio eterno aplican realmente» + «la segunda ventana de info del
> grimorio tiene demasiada informacion, crea una nueva ventana de
> informacion para repartir mejor la informacion» + «crea un item que le
> de hambre al grimorio eterno para comprobar si funciona, que suba el
> hambre de 5 en 5 y que no sea consumible» + «asegurate de que el
> grimorio eterno tenga la sombra de la pagina para cuando sale a cazar y
> que ademas cuando se mata un jefe su animacion de muerte se detenga y
> sea deborado por el grimorio usando la sombra de la pagina… circulos
> magicos como los de los agujeros negros aprisionan al jefe, lo levantan
> y llevan al centro de la pantalla… la primera vez que se mate un jefe y
> solo la primera vez»). Cinco entregas: (1) EL ANÁLISIS — las 24 líneas
> verificadas UNA A UNA contra su aplicación real, TODAS aplican, las
> etiquetas HONESTAS. (2) LAS TRES VENTANAS — clic der cicla BÁSICA → EL
> ARMA → EL FESTÍN. (3) LA ESENCIA DE HAMBRE — +5% por uso, no
> consumible, en la Bolsa Probador. (4) LA SOMBRA DE LA CAZA VERIFICADA.
> (5) LA PRIMERA CENA — el jefe se detiene a 100, los sellos lo
> aprisionan/levantan/llevan al centro y LA SOMBRA DE LA PÁGINA lo devora
> con LOOT COMPLETO; solo la primera vez por tipo (persiste en el .plr).
"""
    s = s[:m.start()] + header_nuevo + m.group(3) + s[m.end():]

    # 2) la fila del HISTORIAL (antes de la fila de la .109)
    fila = ("| **v6.50.110** | ✅ Build-verificada (audit_texturas 0, oráculo 0/0, "
            "build real 0/0, .tmod 2.601.415 B md5 e8f1ba78facfc4208a83c475c0cd1f7f, "
            "388 entradas (387 de la .109 + EsenciaHambre.rawimg 24×24), EOF exacto, "
            "hjson semántico == árbol ×3 (el packager normaliza llaves planas↔bloques "
            "y borra comentarios — es-MX byte a byte), PrimeraCenaProjectile/"
            "ArrancarPrimeraCena/PrimeraCenaPendiente/EnFauces/JefesDevorados/"
            "EsenciaHambre/HambrePorUso/VistaEstadisticas VIVOS en el heap #Strings + "
            "los ldstr «Click der para ver EL ARMA»·«Click der para ver EL FESTÍN»·"
            "«═══ LA PRIMERA CENA ═══»·«crítico mágico»·«lo devora entero» VIVOS en el "
            "heap #US (UTF-16LE), las constantes del cine en el IL (430 el largo · 345 "
            "el trago · 175 la sombra · 150 el centro · −90 la elevación · 3.1/0.92/0.80 "
            "los tres sellos · 0.45 el velo · 62 el fusiforme · 0.05 el +5% del ítem) + "
            "la regresión .103–.109 ÍNTEGRA (0.375·46/34·0.42·1/216000·0.10·2.55·0.5), "
            "headless «Sandboxing v6.50.110 → world gen → Server started» 0 excepciones), "
            "✔ PUBLICADA (release 410075869 + asset AethonMod.tmod 632401101 2.601.415 B, "
            "CDN verificado byte a byte md5 e8f1ba78…, /releases/latest = v6.50.110, "
            "respaldo /home/sync/AethonMod-v6.50.110.tmod), ⏳ en juego | "
            "LA LETRA (2026-10-12): «analisa si las estadisticas del grimorio eterno "
            "aplican realmente» + «la segunda ventana de info del grimorio tiene "
            "demasiada informacion, crea una nueva ventana de informacion para repartir "
            "mejor la informacion» + «crea un item que le de hambre al grimorio eterno "
            "para comprobar si funciona, que suba el hambre de 5 en 5 y que no sea "
            "consumible» + «asegurate de que el grimorio eterno tenga la sombra de la "
            "pagina para cuando sale a cazar y que ademas cuando se mata un jefe su "
            "animacion de muerte se detenga y sea deborado por el grimorio usando la "
            "sombra de la pagina, el jugador recibe loot ya que cuenta como una muerte "
            "normal… cuando llegue a 100 de vida el jefe se detiene circulos magicos "
            "como los de los agujeros negros aprisionan al jefe, lo levantan y llevan "
            "al centro de la pantalla y luego del grimorio sale la sombra de la pagina "
            "y devora por completo al jefe, que esto suceda la primera vez que se mate "
            "un jefe y solo la primera vez». (1) EL ANÁLISIS: TODAS aplican — la "
            "etiqueta honesta («este libro», «mágico», las notas de postura y de "
            "invocación). (2) TRES VENTANAS: TooltipToggleItem.VistaEstadisticas 0/1/2 "
            "(EL ARMA = poder+notas; EL FESTÍN = ojos+LA PRIMERA CENA+progresión). "
            "(3) LA ESENCIA DE HAMBRE: el probador del reloj (Bolsa Probador, pila 1, "
            "consumable=false). (4) LA CAZA VERIFICADA: la cadena del espíritu con "
            "ai[2]=Eterno intacta. (5) LA PRIMERA CENA: FaucesGlobalNPC estado 3 "
            "(gatillo ≤100 vida + playerInteraction + Eterno visible + tipo sin "
            "devorar en ShardPlayer.JefesDevorados que PERSISTE en el .plr), vida "
            "clavada en 100, tres sellos erosionados que aprisionan/elevan −90 px/"
            "llevan al centro vivo, la sombra de la página desde la mano del "
            "portador, EL TRAGO que inhala, y a los 430 t la 2ª pasada de CheckDead "
            "con LOOT COMPLETO y la purga del gore; worms enroscados; UNA cena a la "
            "vez; las sombras de la página SUELTAN a las presas EnFauces (la cena "
            "jamás es robada). LECCIONES: (a) el packager de tML REESCRIBE los hjson "
            "fuente (llaves planas↔bloques, stubs de DisplayName, comentarios "
            "borrados del paquete) — el audit compara SEMÁNTICO (pares llave:valor) "
            "y byte a byte SOLO es-MX; (b) el upload del asset por urllib.multipart "
            "CORROMPE el archivo (GitHub guarda el body crudo: +206 B) — el asset "
            "sube con curl --data-binary + Content-Type: application/octet-stream; "
            "(c) el dato de tamaño del commit se copia SIEMPRE del build FINAL (la "
            "primera cifra 2.601.361 era del build previo a pulir los stubs: la "
            "final es 2.601.415 — el commit se corrigió con amend+force) |\n")
    ancla = "| **v6.50.109** |"
    i = s.find(ancla)
    assert i > 0, "no encontré la fila .109 del HISTORIAL"
    s = s[:i] + fila + s[i:]

    # 3) PRÓXIMOS PASOS: el item 1 nuevo; .109→2, .108→3, .107→4; la .106 PODADA
    nuevo_item = """1. **✅ v6.50.110 PUBLICADA — LA PRIMERA CENA, LAS TRES VENTANAS, LA
   ESENCIA DE HAMBRE Y EL ANÁLISIS** — la letra: «analisa si las
   estadisticas del grimorio eterno aplican realmente» + «la segunda
   ventana de info del grimorio tiene demasiada informacion, crea una
   nueva ventana de informacion para repartir mejor la informacion» +
   «crea un item que le de hambre al grimorio eterno para comprobar si
   funciona, que suba el hambre de 5 en 5 y que no sea consumible» +
   «asegurate de que el grimorio eterno tenga la sombra de la pagina
   para cuando sale a cazar y que ademas cuando se mata un jefe su
   animacion de muerte se detenga y sea deborado por el grimorio usando
   la sombra de la pagina… circulos magicos como los de los agujeros
   negros aprisionan al jefe, lo levantan y llevan al centro de la
   pantalla… la primera vez que se mate un jefe y solo la primera vez».
   (1) EL ANÁLISIS: las 24 líneas verificadas UNA A UNA — TODAS aplican;
   etiquetas honestas. (2) LAS TRES VENTANAS: clic der cicla BÁSICA →
   EL ARMA → EL FESTÍN. (3) LA ESENCIA DE HAMBRE: +5% por uso, no
   consumible, Bolsa Probador. (4) LA CAZA VERIFICADA. (5) LA PRIMERA
   CENA: el jefe se detiene a 100 de vida, TRES sellos erosionados lo
   aprisionan/levantan/llevan al CENTRO de la pantalla, LA SOMBRA DE LA
   PÁGINA del grimorio lo devora y a los 430 t muere DE VERDAD (2ª
   pasada de CheckDead: loot completo); sólo la primera vez por tipo de
   jefe (ShardPlayer.JefesDevorados, persiste en el .plr).
   VERIFICACIÓN: audit_texturas 0 · oráculo 0/0 · build real 0/0 ·
   headless 0 excepciones · audit_v650110 TODO OK (184 ✓) — .tmod
   2.601.415 B md5 e8f1ba78facfc4208a83c475c0cd1f7f, 388 entradas, EOF
   exacto, hjson semántico ×3, el heap + los ldstr + las constantes del
   IL verificados. CHECKLIST en juego: (a) con la Esencia de Hambre el
   apetito sube de 5 en 5 y la voz lo dice, (b) el ciclo de ventanas
   BÁSICA→EL ARMA→EL FESTÍN→BÁSICA con el indicador final, (c) al
   PRIMER jefe de cada tipo llevado a 100 de vida: se detiene, los
   círculos lo aprisionan, lo elevan al centro y la sombra lo devora
   con el loot en el suelo, (d) al MATARLO OTRA VEZ: muerte NORMAL sin
   intervención, (e) la sombra de la página sigue saliendo a cazar del
   25% de hambre.
   PUBLICACIÓN: commit 38e3f44 + tag v6.50.110 + push · release
   **410075869** + asset AethonMod.tmod **632401101** 2.601.415 B (md5
   e8f1ba78facfc4208a83c475c0cd1f7f), CDN VERIFICADO BYTE A BYTE,
   /releases/latest = v6.50.110, respaldo /home/sync/AethonMod-v6.50.110.tmod.
"""
    ancla2 = "## 🚧 PRÓXIMOS PASOS SUGERIDOS (en orden)\n\n"
    i = s.find(ancla2)
    assert i > 0, "no encontré PRÓXIMOS PASOS"
    j = i + len(ancla2)
    # quita el item 1 viejo (hasta el «2. »)
    k = s.find("\n2. **", j)
    assert k > 0
    s = s[:j] + nuevo_item + s[k + 1:]

    # renumera: el viejo 2 (la .109) → 2 OK; .108 3→? — busca los items
    # «2. **✅ v6.50.109» → sigue 2; «3. **✅ v6.50.108» → 3; el 4 (la .107)
    # queda 4; PODAR el item de la .106 (el quinto)
    m5 = re.search(r"\n5\. \*\*✅ v6\.50\.106.*?(?=\n6\. |\Z)", s, re.S)
    if m5:
        s = s[:m5.start()] + s[m5.end():]
        # renumera los que siguen (6→5, 7→6, …)
        def renum(m):
            return f"\n{int(m.group(1)) - 1}. **"
        s = re.sub(r"\n(\d+)\. \*\*(?=[✅🚧⏳])", renum, s[m5.start():], ) if False else s
        # renumera manual desde el punto de corte
        cola = s[m5.start():]
        cola = re.sub(r"\n(\d+)\. \*\*", lambda m: f"\n{int(m.group(1)) - 1}. **", cola)
        s = s[:m5.start()] + cola

    open(p, "w", encoding="utf-8").write(s)
    print("STABLE-SNAPSHOT.md OK")


if __name__ == "__main__":
    changes()
    readme()
    snapshot()
