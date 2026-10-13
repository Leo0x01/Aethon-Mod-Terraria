#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# docs_v650115.py — CHANGES entrada .115 + STABLE-SNAPSHOT (header .115/.114/.113
# — la .112 SALE — + fila HISTORIAL + item 1 de PRÓXIMOS con renumeración
# .114→2/.113→3/.112→4 y la .111 PODADA).
# NOTA: README INTACTO (legal-only desde el Task 32 — la versión vive AQUÍ).

CHANGES = """# AethonMod — Historial de Cambios

## Commit v6.50.115 — LA CORONA DE CONJURO, LA SOMBRA VUELVE, EL GUARDIÁN NO CRUZA Y EL ESCUADRÓN DE 10

**La letra**: «La sombra de la página es necesaria ya que es la forma
en la que el grimorio debe comerse a los jefes, en esta nueva
animación de muerte primero quita los 3 anillos de fotones, eso no es
de lo que estaba hablando, yo lo que me refería era a los anillos de
los agujeros rúnicos, eran esos anillos los que tenías que poner más
los anillos de los soles que sí están bien, estos anillos de agujero
negro rúnico deben brillar» + «la mayor parte de la animación de
devorar a un jefe está bien, solo que no está en el centro de la
pantalla, se centra en el jugador, creo que en este caso es mejor
ubicar al jefe encima del jugador y que el grimorio se ponga a un lado
para devorar al jefe, la misma animación pero con la sombra de la
página saliendo del libro y con el libro mirando fijamente a su
víctima en este caso los jefes» + «también crea una animación
especial para el guardián de la mazmorra en su fase de guardián, es la
fase cuando tiene una defensa muy alta, esto sería como un easteregg
donde el guardia intenta atacar al jugador pero antes de que toque al
jugador el grimorio lo detiene y hace la animación de devorarlo, esto
solo lo hace una vez y el grimorio lo dice, solo lo devorará una sola
vez ya que con eso es suficiente, esta acción con el guardián le da 1
nivel completo» + «sobre los minions, en el caso de los minions del
grimorio, 30 son demasiado… cuando llegue a 11 minions del grimorio en
la siguiente invocación no invoca minion, lo que hace es fortalecer a
uno que ya existe con un 50% de daño y una mejora visual en su
proyectil con efectos visuales, visualmente cada minion con cada
mejora debe ganar un anillo rúnico de agujero negro extra, la máxima
mejora sería cuando un minion gana 2 anillos rúnicos de agujero negro
cada uno con un color diferente y además 2 anillos rúnicos de sol a su
alrededor, así que en total cada minion tendría 4 mejoras… cada mejora
aumenta el ataque del minion en un 50%».

**EL CAMBIO**: cuatro entregas — la corona de conjuro, el puesto nuevo
con la sombra de vuelta, el easteregg del guardián y el escuadrón de 10:

- **LA CORONA DE CONJURO (NO LOS FOTONES)**: los TRES ANILLOS DE
  FOTONES RGB de la .114 (la textura Ring con aberración cromática)
  MURIERON — no eran los de la letra. Ahora el cerco es **LA TRIPLE
  CORONA RÚNICA LITERAL de los agujeros negros** (`OrbitaLib.CoronaConjuro`,
  la técnica exacta del halo dorsal): el círculo **DORADO** de 8 runas
  girando, el **VIOLETA** de 6 contrarrotando (el contrarroto arcano)
  y el **BLANCO** íntimo de 6 rápido — las runas DE PIE con sus
  resplandores y sus PERLAS, `glyphMul 2,1` para que SE LEAN a escala
  de jefe. Y **DEBEN BRILLAR** (la letra): el doble resplandor dorado
  + violeta pulsando DETRÁS de la corona, que ARDE al comer y al
  tragar; la rueda CORRE al comer (giro ×1,9) y se CIÑE (radio
  1,10→0,46·radioJefe) hasta colapsar DENTRO del libro en el trago.
  **LOS ANILLOS DE LOS SOLES SE QUEDAN** (`SigiloLib.SistemaAnillos` —
  los que sí estaban bien, dijo la letra).
- **EL PUESTO NUEVO Y LA SOMBRA DE LA PÁGINA VUELVE**: el preso ya no
  vuela al centro de la pantalla — cuelga **ENCIMA DEL PORTADOR**
  (`FaucesGlobalNPC.AnclaSobrePortador`: margen 0,62·jefe + 120 px
  sobre su cabeza, lo SIGUE si camina; `_cenaPortador` viaja con la
  cena), y el libro gigante toma su puesto **A UN COSTADO** (el lado
  que mira el portador, a radioJefe + 118, INCLINADO −0,30 hacia su
  víctima, el iris CLAVADO de siempre). Y **LA SOMBRA DE LA PÁGINA
  SALE DEL LIBRO** (la raíz ya NO es la mano del portador:
  `PosicionVivaDelLibro` es COMPARTIDA — el dibujo del libro y la
  raíz de la sombra respiran IGUAL): el cuerpo vivo
  (`ColumnaViva`+`Masa`+bruma doble+`OjosDeMasa` con TODOS los ojos
  mirando a la víctima+`CabezaDeBruma`) trepa del libro al preso y lo
  DEVORA (el `Devorador` v2: la elipse completa que crece con el
  festín y se inhala al trago, la herida roja en la punta, la
  corriente de bruma volviendo al libro) — el patrón .113 enraizado en
  el libro gigante de la .114.
- **EL GUARDIÁN DE LA MAZMORRA NO CRUZA (el easteregg)**:
  `IntentarDetenerGuardian` en el `PreAI` del DungeonGuardian — en su
  fase guardián (la defensa 9999) carga contra el portador del Eterno,
  y a **250 px** (un paso antes de tocarlo, rumbo > 0,35 y velocidad
  viva) **EL GRIMORIO LO DETIENE EN SECO** y arranca LA CENA COMPLETA
  (los anillos lo aprisionan encima del portador, el libro al costado,
  la sombra lo devora, loot íntegro). **+1 NIVEL COMPLETO** al Eterno
  del inventario (`SubirNivelDirecto` — con su celebración), **LA VOZ**
  lo anuncia (`Armas.GuardianDetenido` ×3 idiomas: «A ti solo te
  devoraré UNA vez… con eso me basta.») y la marca
  `ShardPlayer.GuardianMazmorraDevorado` persiste en el .plr — **SOLO
  UNA VEZ por portador** (la Página en Blanco también la borra: el
  probador de primeras veces las borra TODAS). El guardián queda
  EXCLUIDO de la cena normal (`PrimeraCenaPendiente` lo rechaza: su
  festín es EXCLUSIVO del easteregg).
- **EL ESCUADRÓN DE 10 Y EL FORTALECIMIENTO**: el 20+equipo+nivel de
  la .114 (30 con todo puesto — «demasiado») MUERE con su escalera
  `BonusMinionSlots`; ahora `WeaponScaling.EscuadronEterno = 10`
  LLANO (ni el equipo de invocador ni el nivel inflan el escuadrón del
  Eterno; los minions VANILLA siguen con su `maxMinions` de siempre) y
  la compuerta cuenta **SOLO OJOS** del Eterno. **LA 11.ª INVOCACIÓN
  YA NO INVOCA: FORTALECE** — se elige el ojo con MENOS mejoras de la
  banda (`OjoEternoMinion.MejorasDe/Fortalecer`: las mejoras viven en
  la centésima de `ai[2]` — nivel 5,03 = nivel 5 con 3 mejoras — y
  viajan solas con el `netUpdate`): **CADA MEJORA +50% DE DAÑO DEL
  RAYO** (`MultMejoras`, hasta ×3 con las cuatro) y su ANILLO: mejora
  1 el **ANILLO RÚNICO DE AGUJERO NEGRO DORADO** (8 runas CW, 15 px),
  mejora 2 el **VIOLETA contrarrotante** (6 runas, 20 px — OTRO color
  como pide la letra), mejora 3 **EL ANILLO DEL SOL dorado**
  (`SigiloLib.AnilloRunico`, la elipse inclinada de los soles, 25 px)
  y mejora 4 **EL SEGUNDO SOL azul-estelar** contrainclinado (30 px) —
  más el resplandor del ojo que CRECE con cada mejora (el sello con el
  que NACE sigue siendo su anillo de siempre). Con las 4 mejoras en
  los 10 ojos: la voz `Armas.OjosAlMaximo` y no se cobra maná.
  Tooltips reescritos: «Escuadrón de 10 ojos · lleno el escuadrón,
  cada invocación FORTALECE a un ojo (+50% daño · 4 mejoras)» y LA
  PRIMERA CENA («los anillos rúnicos lo alzan sobre tu cabeza; el
  libro, a un costado, lo mira fijo y LA SOMBRA DE LA PÁGINA lo
  devora»).

**VERIFICACIÓN**: oráculo 0/0 (79 refs, 311 .cs) · build real 0/0 ·
.tmod 2.624.989 B md5 63e76ada6466d3be5cd60ade32ae4dab (390 entradas —
los mismos nombres de la .114: solo cambia código —, EOF exacto) ·
headless 0 excepciones (serverconfig115, mundo guardian115, puerto
7784) · `audit_v650115.py` TODO OK (307 ✓): heap
(`AnclaSobrePortador`/`_cenaPortador`/`PosicionVivaDelLibro`/
`IntentarDetenerGuardian`/`GuardianPaso`/`GuardianMazmorraDevorado`/
`MejorasDe`/`Fortalecer`/`MultMejoras`/`EscuadronEterno` VIVOS;
`AnilloFotones`/`CentroDePantalla`/`BonusMinionSlots` MUERTOS), ldstr
UTF-16 ×3 idiomas + `guardianDevorado`/`jefesDevorados`, constantes
.104–.115 (250/0,35/120/118/1,10/0,46/2,1/15/20/25/30/−0,55 en el IL),
el árbol con las 4 piezas + la regresión .110–.114 ÍNTEGRA, hjson ×3
semántico + es-MX byte a byte (3 claves nuevas, `SlotsMinionLlenos`
muerta), los iconos del usuario SIN cambios.

"""


def changes():
    p = "CHANGES.md"
    s = open(p, encoding="utf-8").read()
    s = s.replace("# AethonMod — Historial de Cambios\n\n", "", 1)
    s = CHANGES + s
    open(p, "w", encoding="utf-8").write(s)
    assert "v6.50.115 — LA CORONA DE CONJURO" in s
    print("CHANGES.md OK")


HEADER_115 = """> Última actualización: v6.50.115 (LA CORONA DE CONJURO, LA
> SOMBRA VUELVE, EL GUARDIÁN NO CRUZA Y EL ESCUADRÓN DE 10. La letra:
> «quita los 3 anillos de fotones… yo me refería a los anillos de los
> agujeros rúnicos… más los anillos de los soles que sí están bien,
> estos anillos de agujero negro rúnico deben brillar» + «es mejor
> ubicar al jefe encima del jugador y que el grimorio se ponga a un
> lado para devorar al jefe, la misma animación pero con la sombra de
> la página saliendo del libro y con el libro mirando fijamente a su
> víctima» + «crea una animación especial para el guardián de la
> mazmorra… antes de que toque al jugador el grimorio lo detiene y
> hace la animación de devorarlo, esto solo lo hace una vez y el
> grimorio lo dice… esta acción con el guardián le da 1 nivel
> completo» + «en el caso de los minions del grimorio, 30 son
> demasiado… cuando llegue a 11 minions del grimorio en la siguiente
> invocación no invoca minion, lo que hace es fortalecer a uno que ya
> existe con un 50% de daño… 4 mejoras, cada mejora aumenta el ataque
> en un 50%»). Cuatro entregas: (1) LA CORONA DE CONJURO — los tres
> anillos de fotones RGB de la .114 MUEREN: nace LA TRIPLE CORONA
> RÚNICA LITERAL de los agujeros negros (OrbitaLib.CoronaConjuro: el
> dorado de 8 + el violeta de 6 contrarrotando + el blanco íntimo de
> 6, runas DE PIE con perlas, glyphMul 2,1) ABRILLANTADA (doble
> resplandor dorado+violeta pulsante que ARDE al comer; la rueda corre
> al comer y se ciñe 1,10→0,46) — los anillos de los soles se quedan.
> (2) EL PUESTO NUEVO + LA SOMBRA VUELVE — el preso cuelga ENCIMA DEL
> PORTADOR (AnclaSobrePortador: 0,62·jefe+120 px sobre su cabeza, lo
> sigue si camina; _cenaPortador viaja con la cena) y el libro A UN
> COSTADO (radioJefe+118, inclinado −0,30, iris clavado); LA SOMBRA DE
> LA PÁGINA SALE DEL LIBRO (PosicionVivaDelLibro compartida:
> ColumnaViva+Masa+bruma+OjosDeMasa+CabezaDeBruma+el Devorador v2 — el
> patrón .113 enraizado en el libro gigante). (3) EL GUARDIÁN NO CRUZA
> — IntentarDetenerGuardian en el PreAI del DungeonGuardian: a 250 px
> del portador cargando, el grimorio LO DETIENE y arranca LA CENA
> COMPLETA; +1 NIVEL COMPLETO (SubirNivelDirecto), LA VOZ
> (Armas.GuardianDetenido ×3: «A ti solo te devoraré UNA vez… con eso
> me basta») y la marca ShardPlayer.GuardianMazmorraDevorado persiste
> en el .plr (UNA vez por portador; PaginaEnBlanco la borra); el
> guardián excluido de la cena normal. (4) EL ESCUADRÓN DE 10 —
> EscuadronEterno=10 LLANO (BonusMinionSlots muere; la compuerta
> cuenta SOLO ojos) y LA 11.ª INVOCACIÓN FORTALECE al ojo con menos
> mejoras: +50% daño por mejora (hasta ×3) y su anillo (1: agujero
> negro DORADO · 2: VIOLETA contrarrotante · 3: SOL dorado · 4: segundo
> sol AZUL-estelar) + el resplandor crece. .tmod 2.624.989 B md5
> 63e76ada6466d3be5cd60ade32ae4dab, 390 entradas, oráculo 0/0, build
> real 0/0, headless 0 excepciones, audit_v650115 TODO OK (307 ✓).
> PENDIENTE EN JUEGO: (a) la cena: la CORONA RÚNICA brillante + los
> soles, el preso ENCIMA del jugador, el libro A UN COSTADO mirándolo
> fijo y LA SOMBRA DE LA PÁGINA saliendo del libro devorándolo, (b)
> entrar a la Mazmorra sin matar a Skeletron con el Eterno a mano: el
> guardián carga → el grimorio lo DETIENE, lo devora y el libro SUBE 1
> NIVEL con su voz — y NUNCA más repite el truco, (c) invocar 11
> veces: a la 11.ª NO aparece un ojo nuevo — uno existente GANA su
> anillo dorado (+50%), a la 12.ª el violeta, a la 13.ª el sol dorado,
> a la 14.ª el sol azul (×3 de daño al tope) y «Los diez ojos ya
> llevan sus cuatro mejoras» cuando todo está al máximo, (d) el
> escuadrón JAMÁS pasa de 10 (ni con equipo de invocador ni con nivel),
> (e) Página en Blanco borra también el pacto del guardián.
"""


def snapshot():
    p = "STABLE-SNAPSHOT.md"
    lineas = open(p, encoding="utf-8").read().splitlines(keepends=True)

    # ---------- 1) EL HEADER: título .115 + bloques .115/.114/.113 ----------
    i_b114 = next(i for i, l in enumerate(lineas)
                  if l.startswith("> Última actualización: v6.50.114 ("))
    i_b113 = next(i for i, l in enumerate(lineas)
                  if l.startswith("> Última actualización: v6.50.113 ("))
    i_b112 = next(i for i, l in enumerate(lineas)
                  if l.startswith("> Última actualización: v6.50.112 ("))
    bloque_114 = "".join(lineas[i_b114:i_b113])
    bloque_113 = "".join(lineas[i_b113:i_b112])
    # el fin del bloque .112: la primera línea que NO empieza con '>' tras i_b112
    i_fin112 = next(i for i in range(i_b112 + 1, len(lineas))
                    if not lineas[i].startswith(">"))
    header_nuevo = [
        "# AethonMod — ESTADO ACTUAL (v6.50.115)\n",
        "\n",
        "> **Este documento = \"¿por dónde nos quedamos?\"** — se actualiza en cada entrega.\n",
        HEADER_115,
        bloque_114,
        bloque_113,
    ]
    lineas = header_nuevo + lineas[i_fin112:]
    s = "".join(lineas)
    assert "ESTADO ACTUAL (v6.50.115)" in s
    assert s.count("Última actualización: v6.50.115") == 1
    assert "Última actualización: v6.50.112" not in s.split("## ✅ ESTADO VERIFICADO")[0]
    print("  header OK (bloques .115/.114/.113 — la .112 salió)")

    # ---------- 2) LA FILA DEL HISTORIAL (antes de la fila de la .114) ----------
    ancla = "| **v6.50.114** |"
    i = s.find(ancla)
    assert i > 0, "no encontré la fila .114 del HISTORIAL"
    s = s[:i] + FILA_115 + s[i:]
    assert s.count("| **v6.50.115** |") == 1
    print("  fila HISTORIAL OK")

    # ---------- 3) PRÓXIMOS: doc-rot POR LÍNEAS con anclas por versión ----------
    # 3a. PODAR la .111 (de su línea de inicio a la línea previa a la .100)
    lin = s.splitlines(keepends=True)
    i_111 = next(i for i, l in enumerate(lin)
                 if l.startswith("4. **✅ v6.50.111 PUBLICADA"))
    i_100 = next(i for i, l in enumerate(lin)
                 if l.startswith("1. **✅ v6.50.100 PUBLICADA"))
    assert i_111 < i_100, "la .111 debería estar antes que la .100"
    del lin[i_111:i_100]
    s = "".join(lin)
    assert "v6.50.111 PUBLICADA" not in s
    print("  .111 PODADA de PRÓXIMOS")

    # 3b. RENUMERAR (del 3 hacia arriba — anclas únicas por versión)
    for viejo, nuevo, version in [
        ("3. **✅ v6.50.112 PUBLICADA", "4. **✅ v6.50.112 PUBLICADA", ".112"),
        ("2. **✅ v6.50.113 PUBLICADA", "3. **✅ v6.50.113 PUBLICADA", ".113"),
        ("1. **✅ v6.50.114 PUBLICADA", "2. **✅ v6.50.114 PUBLICADA", ".114"),
    ]:
        assert s.count(viejo) == 1, f"ancla no única para {version}: {viejo!r}"
        s = s.replace(viejo, nuevo, 1)
    print("  renumeración .114→2 · .113→3 · .112→4")

    # 3c. INSERTAR la .115 como item 1 (tras el header de PRÓXIMOS)
    ancla = "## 🚧 PRÓXIMOS PASOS SUGERIDOS (en orden)\n\n"
    i = s.find(ancla)
    assert i > 0
    i += len(ancla)
    s = s[:i] + ITEM_115 + s[i:]
    assert s.count("v6.50.115 PUBLICADA — LA CORONA DE CONJURO") == 1
    print("  item 1 (.115) insertado")

    open(p, "w", encoding="utf-8").write(s)
    print("STABLE-SNAPSHOT.md OK")


FILA_115 = """| **v6.50.115** | ✅ Build-verificada (oráculo 0/0 sobre 311 .cs con 79 refs, build real 0/0, .tmod 2.624.989 B md5 63e76ada6466d3be5cd60ade32ae4dab, 390 entradas (los mismos nombres de la .114: solo cambia código), EOF exacto, heap/ldstr/constantes .104–.115 (AnclaSobrePortador/_cenaPortador/PosicionVivaDelLibro/IntentarDetenerGuardian/GuardianPaso/GuardianMazmorraDevorado/MejorasDe/Fortalecer/MultMejoras/EscuadronEterno VIVOS; AnilloFotones/CentroDePantalla/BonusMinionSlots MUERTOS; los ldstr «Mods.AethonMod.Armas.GuardianDetenido»·«OjoFortalecido»·«OjosAlMaximo»·«guardianDevorado»·«jefesDevorados» VIVOS en #US UTF-16; «Mods.AethonMod.Armas.SlotsMinionLlenos» y el tooltip del escuadrón de 20 MUERTOS; las constantes 250/0,35/120/118/1,10/0,46/2,1/15/20/25/30/−0,55 VIVAS en el IL), el árbol con las 4 piezas + la regresión .110–.114 ÍNTEGRA (la corona de conjuro en la cena, la sombra enraizada en el libro, el guardián excluido de la cena normal, la compuerta que cuenta solo ojos, el fix masivo y el retroceso .114 vivos), hjson ×3 semántico + es-MX byte a byte (3 claves nuevas: GuardianDetenido/OjoFortalecido/OjosAlMaximo; SlotsMinionLlenos muerta), iconos del usuario SIN cambios, headless «Sandboxing: Aethon, the Eternal Grimoire v6.50.115 → Adding Content → Server started» 0 excepciones (serverconfig115, mundo guardian115, puerto 7784)), ✔ PUBLICADA (release 411926874 + asset AethonMod.tmod 637722392 2.624.989 B, CDN verificado byte a byte md5 63e76ada…, /releases/latest = v6.50.115, respaldo /home/sync/AethonMod-v6.50.115.tmod), ⏳ en juego | LA LETRA (2026-10-13): «La sombra de la página es necesaria… primero quita los 3 anillos de fotones… yo me refería a los anillos de los agujeros rúnicos… más los anillos de los soles que sí están bien, estos anillos de agujero negro rúnico deben brillar» + «es mejor ubicar al jefe encima del jugador y que el grimorio se ponga a un lado para devorar al jefe, la misma animación pero con la sombra de la página saliendo del libro y con el libro mirando fijamente a su víctima» + «crea una animación especial para el guardián de la mazmorra en su fase de guardián… antes de que toque al jugador el grimorio lo detiene y hace la animación de devorarlo, esto solo lo hace una vez y el grimorio lo dice… le da 1 nivel completo» + «en el caso de los minions del grimorio, 30 son demasiado… cuando llegue a 11 minions… la siguiente invocación no invoca minion, fortalece a uno que ya existe con un 50% de daño… 2 anillos rúnicos de agujero negro cada uno con un color diferente y además 2 anillos rúnicos de sol… 4 mejoras, cada mejora aumenta el ataque en un 50%». CUATRO ENTREGAS: (1) LA CORONA DE CONJURO — los fotones RGB de la .114 mueren: OrbitaLib.CoronaConjuro (el dorado de 8 + violeta de 6 contrarrotando + blanco íntimo, runas DE PIE con perlas, glyphMul 2,1) ABRILLANTADA (doble resplandor dorado+violeta pulsante, rueda que corre al comer, radio 1,10→0,46) — los soles se quedan. (2) EL PUESTO + LA SOMBRA — AnclaSobrePortador (0,62·jefe+120 px sobre la cabeza, sigue al portador) + el libro A UN COSTADO (radioJefe+118, inclinado, iris clavado) + LA SOMBRA DE LA PÁGINA sale DEL LIBRO (PosicionVivaDelLibro compartida; ColumnaViva+Masa+OjosDeMasa+CabezaDeBruma+Devorador v2 — el patrón .113 enraizado en el libro). (3) EL GUARDIÁN NO CRUZA — IntentarDetenerGuardian (PreAI del DungeonGuardian, 250 px, rumbo>0,35): lo DETIENE, LA CENA completa, +1 NIVEL (SubirNivelDirecto), LA VOZ ×3 idiomas y ShardPlayer.GuardianMazmorraDevorado persiste (UNA vez por portador; PaginaEnBlanco la borra); el guardián excluido de la cena normal. (4) EL ESCUADRÓN DE 10 — EscuadronEterno=10 LLANO (BonusMinionSlots muere; compuerta cuenta SOLO ojos) y la 11.ª invocación FORTALECE al ojo con menos mejoras (la centésima de ai[2]): +50% daño por mejora (MultMejoras, hasta ×3) y su anillo (1: agujero negro DORADO CirculoRunico 8·CW·15 px · 2: VIOLETA 6·CCW·20 px · 3: SOL dorado SigiloLib.AnilloRunico 25 px · 4: segundo sol AZUL-estelar 30 px) + resplandor creciente. LECCIONES: (a) cuando la letra corrige UN anillo («no era eso, eran los RÚNICOS») el repositorio YA tiene el patrón exacto — OrbitaLib.CoronaConjuro era el círculo de conjuro LITERAL desde la v6.37: buscar en la PROPIA casa antes de inventar; (b) la raíz de una sombra que sale de un objeto MÓVIL debe leer la MISMA posición viva que su dibujo (PosicionVivaDelLibro compartida — un ancla estática desincroniza sombra y libro); (c) las mejoras de un minion viajan Gratis en la fracción de un ai[] YA sincronizado (nivel 5,03 = nivel 5 + 3 mejoras) — cero campos extra, cero paquetes nuevos; (d) un easteregg de UNA vez exige la marca ANTES del espectáculo (si el cine se interrumpe, el «solo una vez» ya está sellado) |
"""

ITEM_115 = """1. **✅ v6.50.115 PUBLICADA — LA CORONA DE CONJURO, LA SOMBRA
   VUELVE, EL GUARDIÁN NO CRUZA Y EL ESCUADRÓN DE 10** (la letra:
   «quita los 3 anillos de fotones… yo me refería a los anillos de los
   agujeros rúnicos… más los anillos de los soles que sí están bien,
   estos anillos de agujero negro rúnico deben brillar» + «ubicar al
   jefe encima del jugador y que el grimorio se ponga a un lado…
   con la sombra de la página saliendo del libro y con el libro
   mirando fijamente a su víctima» + «el guardia intenta atacar al
   jugador pero antes de que toque al jugador el grimorio lo detiene y
   hace la animación de devorarlo, esto solo lo hace una vez y el
   grimorio lo dice… le da 1 nivel completo» + «30 son demasiado…
   cuando llegue a 11 minions del grimorio en la siguiente invocación
   no invoca minion, lo que hace es fortalecer a uno que ya existe con
   un 50% de daño… 4 mejoras, cada mejora aumenta el ataque en un
   50%»). (1) LA CORONA DE CONJURO — los fotones RGB mueren: la TRIPLE
   CORONA RÚNICA de los agujeros negros (dorado 8 + violeta 6
   contrarrotando + blanco íntimo, runas con perlas) ABRILLANTADA; los
   soles se quedan. (2) El preso ENCIMA del portador + el libro A UN
   COSTADO inclinado mirándolo fijo + LA SOMBRA DE LA PÁGINA sale DEL
   LIBRO y lo devora. (3) El GUARDIÁN DE LA MAZMORRA: a 250 px de
   tocar al portador el grimorio lo DETIENE, lo devora y el libro
   gana +1 NIVEL COMPLETO — una sola vez por portador (la voz lo
   anuncia; PaginaEnBlanco reinicia el pacto). (4) EL ESCUADRÓN DE 10:
   la 11.ª invocación FORTALECE a un ojo (+50% daño por mejora, 4
   mejoras = ×3): anillo de agujero negro DORADO → VIOLETA → SOL
   dorado → segundo sol AZUL-estelar. **PENDIENTE EN JUEGO**: (a) la
   cena con la corona rúnica BRILLANTE + los soles, el preso sobre tu
   cabeza, el libro al costado y la sombra saliendo de él, (b) entrar
   a la Mazmorra temprano con el Eterno a mano: el guardián carga →
   DETENIDO → devorado → +1 nivel y su voz — y no repite jamás, (c)
   invocar más allá de 10 ojos: cada cast FORTALECE (los anillos
   aparecen de a uno, ×3 de daño al tope) y «Los diez ojos ya llevan
   sus cuatro mejoras» al final, (d) el escuadrón nunca pasa de 10
   (ni equipo ni nivel), (e) Página en Blanco borra también el pacto
   del guardián.

"""


def main():
    changes()
    snapshot()
    print("DOCS .115 OK (README intocado — legal-only)")


if __name__ == "__main__":
    main()
