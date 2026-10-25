#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""docs_v6520.py — LA ENTRADA DE CHANGES.md + EL SNAPSHOT de la v6.52.0
(LA BOLSA DEL ESCRIBA — Task 84). Placeholder ⏳ para los IDs del release
(la lección de Task 67/83: el ID real SOLO tras crear el release)."""

CAMBIOS = """# v6.52.0 — LA BOLSA DEL ESCRIBA: EL RAYO MASIVO · EL ARMA VIVA · LA COREOGRAFÍA — ⏳ PUBLICACIÓN PENDIENTE

**La letra del usuario**: «no puedo revisar ninguno de los 9 puntos anteriores… mientras tanto deja todo eso sin tocar y ve recordando los 9 puntos… Estuve viendo un mod relacionado a Calamity… ese mod propone que la conciencia de un NPC del mod está en diferentes armas y además hay un árbol de habilidades completo y funcional para cada uno, recuerda que intentamos crear algo así y no funcionó… [video de YouTube] he visto en varios mods que sus jefes son capaces de lanzar rayos de energía masivos, que en teoría serían texturas de ruido de Perlin moviéndose en una dirección con iluminación, glow, desenfoque, desenfoque de movimiento… ¿por qué no podemos hacer eso?… para nuestro jefe siento que le falta una coreografía clara y precisa además de más efectos visuales, más luces… investiga a profundidad, pero NO toques nada del mod, en su lugar crea algo nuevo que podamos usar, algo por cada concepto investigado… todo lo creado nuevo lo puedes meter en una nueva bolsa y dárselo al jugador».

**LA REGLA DEL CICLO SE CUMPLIÓ AL BYTE**: git diff contra HEAD = `build.txt` (el bump de versión) y NADA MÁS — los TRES hjson de raíz INTACTOS byte a byte (audit_v6520 lo verifica contra git HEAD), ni una línea de código existente tocada. TODO el ciclo vive en 26 ARCHIVOS NUEVOS.

**LA INVESTIGACIÓN (profunda, profesional y exhaustiva — Task 84, tres agentes)**: (R1) EL MOD DEL VIDEO DE FACEBOOK ERA **CALAMITY OVERHAUL** (github hocha113/CalamityOverhaul, clonado y auditado — 4.739 .cs): sus CUATRO armas legendarias guardan conciencias (Himura Mayo en la katana Onikiri, Shenyo en el paraguas Kikasa…), cada una con su sistema de skills: 36 INSCRIPCIONES OniMei en 3 ranuras (Nakago/Hi/Horimono), desbloqueadas por HECHOS («blade karma»: matar quieto, bajo la lluvia…) o REGALOS (24 diálogos por jefe), con el nivel del ARMA viajando en el ÍTEM (LegendData: 22 pruebas por jefes vencidos PERSONALMENTE) y la biblioteca en el JUGADOR — reglas de diseño extraídas verbatim: solo composición multiplicativa, todo poder con carga, claves estables, nodos bloqueados muestran progreso no efectos, CERO moneda de puntos. (R2) LOS RAYOS MASIVOS NO SON UNA TÉCNICA — SON SIETE CAPAS (vanilla + Calamity + Fargo's + Stars Above + CWR DECOMPILADOS al IL con ilspycmd): raycast con longitud suavizada (el motion-blur del barrido) → geometría de cinta → RUIDO (fBm con fases lentas; MITO CAÍDO: ReLogic NO tiene PerlinNoise — vanilla regala la textura Images/Misc/Perlin, CWR usa una 512×512 propia, la casa YA tenía el fBm CPU en StormLib) → capas aditivas halo/cuerpo/core → luz a lo largo de TODO el rayo → envolventes de vida → diamantes viajeros sin(along/92−t·9)^7 (la firma del GammaRayBeam de CWR). (R3) EL VIDEO ERA EL **MEOWZERS BOSS FIGHT** (Steam Workshop 3823322489) DE **ZENOVIA, EL DEV DE LUNAR VEIL** (confirmado por el RSS del canal de Layla + los comentarios del workshop + el clon del código del propio dev, LunarVeilLegacy): la gramática destilada con NÚMEROS — telegraph 20-30 t (rápido) / 45-60 (normal) / 90-120 (pesado) / 216 (el nuke estándar de Calamity), recuperación = la ventana de oportunidad VISIBLE, transiciones de fase como EVENTOS de 200-300 t con invulnerabilidad y cámara, el fakeout que NUNCA miente sobre el lanzamiento, el agarrón con cooldown a la mitad si falla, la muerte-performance con firma.

**LO CREADO (todo nuevo, todo en LA BOLSA DEL ESCRIBA — entregada al entrar al mundo, como todas las bolsas de la casa)**:

· **EL PRIMER TRAZO — EL ARMA DEL RAYO MASIVO** (la respuesta armada a «¿por qué no podemos hacer eso?»: PORQUE NADIE LA HABÍA ESCRIBIDO — ya está): canalizada (mantén clic), SIN maná (arma de prueba, como la Sombra II), CINCO fajos convergen en UN rayo con el motor nuevo **BeamLib** — espina fBm al 3,5 % del largo con fases LENTAS (la tabla bolt-vs-beam: 1,4 ciclos de meandro + jitter ×0,25, anclas exp(−1200·d⁸)), TRES capas aditivas (halo ×2,3 → cuerpo con LA TEXTURA DE RUIDO PROPIA RuidoTinta.png generada a mano, fBm elongado + caída gaussiana — «el Perlin moviéndose en una dirección» literal, con uvFlow la carne AVANZA raíz→punta → core blanco al 42 %), envolventes de boca/punta/pared, LOS DIAMANTES VIAJEROS sin^7, luz cada 64 px (la MITAD del presupuesto vanilla), telegraph que LATA (BeamLib.Telegrafo, 2→4 Hz), el apunte CON INERCIA (Lerp 0,92 — la punta se DESLIZA al barrer), convergencia de anillo 20→4 px estilo Last Prism, daño ×3 al bloquearse, hitbox de LÍNEA (CheckAABBvLine), DrawScreenCheckFluff 2600, CutTiles, MP con SendExtraAI — y TODO bajo el CONTRATO del lote (solo encola quads de VFXCore: un solo volcado + finally ReabrirLoteVanilla — la clase de crash de la .51.13 NO PUEDE volver a nacer de aquí).

· **LA PLUMA CONSCIENTE — EL ARMA VIVA CON ÁRBOL DE HABILIDADES** (el «recuerda que intentamos crear algo así y no funcionó» — AHORA FUNCIONA): 18 INSCRIPCIONES en 3 ranuras (FILIGRANA utilidad / VERSO ofensiva / COLOFÓN remate), cada una con PODER y CARGA (todo poder lleva su trade-off — la regla CWR): 3 de nacimiento + 5 por HECHOS (Primeras Palabras 20 kills · Mano Firme 25 golpes al mismo jefe · Al Margen 5 jefes distintos · Tinta Seca 10 min sostenida · Racha Viva 8 kills en cadena) + 10 por REGALOS (la conciencia REACCIONA a cada jefe vencido con la pluma: EoC→Subrayado, EoW/BoC→Letra Capital, Skeletron→Tinta Corrosiva, WoF→Firma (robo de vida), Gemelos→Verso Ciego (buscapresas), Plantera→Rima Interna (estela), Golem→Caligrafía, Cultista→Hemistiquio (perforación), Señor Lunar→Tinta Madre, EL ESCRIBA→El Colofón (+35 % daño, +2 proyectiles, la última palabra)). **EL ÁRBOL se abre con [P]** (keybind registrado, configurable): tres columnas con nodos de tres estados (equipada con anillo blanco latiendo · desbloqueada violeta · bloqueada con silueta discontinua y «n/N» — progreso, NUNCA efectos), panel de lectura con nombre/descripción/PODER/CARGA, fila de hechos con progreso vivo. **EL NIVEL VIAJA CON EL ARMA** (GlobalItem SaveData/NetSend/Clone: cada jefe vencido con la pluma suma +12 % de daño permanente — tira la pluma, véndela, guárdala: su alma viaja con ella; la biblioteca queda con el JUGADOR — el modelo asimétrico de CWR). **LA CONCIENCIA**: el Escriba HABLA al primer encuentro (4 líneas espaciadas por marcas de frame), susurra de vez en cuando (20 % con cooldown de 3 min) y CELEBRA cada desbloqueo. Ejecución (Punto Final ×2,5 bajo el 10 % de vida), robo de vida (Firma 4 %), buscapresas (Verso Ciego/Tinta Madre) — el perfil se compone SOLO multiplicando (jamás asignando).

· **EL ESCRIBA, LA MANO QUE DEVORÓ EL LIBRO — EL JEFE DEMO DE LA COREOGRAFÍA** (340.000 HP, post-Señor Lunar, se convoca con la PLUMA CONVOCADORA — madera de siempre): la mano que ESCRIBIÓ las páginas que Aethon devoró; sus ataques SE ESCRIBEN en el aire antes de existir (la línea de tinta ES el telegraph). TODO bajo el motor nuevo **CoreoGuion** (la gramática R3): cada ataque = TELEGRAPH→ACTIVO→RECUPERACIÓN con UNA señal por ataque (un sonido, un anuncio en la BARRA DE CONJURO nueva, una sacudida, un golpe de luz) — 8 ataques: EL TRAZO (la línea que se dibuja y el Escriba la RECORRE de un tajo), LLUVIA DE VERSOS (páginas que caen con CHARCO DE SOMBRA — el aviso se VE en el suelo), LAS PLUMAS (anillo orbital de 12 que cierra hacia dentro), EL BORRÓN (EL NUKE DE 216 T con cast bar, vórtice que converge y retumbo que crece), EL TRAZO CORREGIDO (EL FAKEOUT: la misma línea que SE AGUANTA dos veces antes de soltar, y el golpe es doble), LA JAULA DE TINTA (dos paredes que cierran la arena), EL VERSO (EL RAYO MASIVO DEL JEFE — BeamLib hostil en tinta violeta, barriendo el campo) y LA ANOTACIÓN (EL AGARRÓN: si estás a 230 px al soltar el telegraph, te ESCRIBE DENTRO de una página, cámara enfocada, daño por tick y te ESCUPE con onda — cooldown 25 s, A LA MITAD si falla). DOS TRANSICIONES-EVENTO (66 % LA CORRECCIÓN de 6 beats con cámara + tinta que sube + silencio; 33 % EL INTERLINEADO con la RE-TINTA +10 % de vida y la luz invertida) y LA MUERTE-PROFORMANCE (EL PUNTO FINAL: pausa dramática, LA FIRMA CALIGRÁFICA — una S gigante que se escribe sola cruzando la pantalla — y el jefe deshecho en páginas, con la muerte REAL al final para que el botín y el bestiario corran — la lección v6.51.2). CON EL: **LuzEscenica** (la luz del jefe es un GUION: violeta latiendo a 4 Hz en telegraph → destello blanco-caliente en el golpe → ámbar tenue en recuperación — el «más luces» de la letra), **CamaraCine** (el FocusOn de Lunar Veil casa-ificado: ModifyScreenPosition con ease-in/out, JAMÁS a más de 420 px del jugador), **BarraConjuroUI** (el cast bar de Stars Above: el NOMBRE del ataque y su reloj, con doble parpadeo blanco en los últimos 10 t). El cuerpo es 100 % CÓDIGO (SombrasLib.ColumnaViva + Masa + Bruma + el ojo único que sigue a la presa + el collar de 6 páginas + la pluma gigante — ni un sprite del jefe), boss head + bestiario con arte generado, MusicID.LunarBoss.

· **LA BOLSA DEL ESCRIBA + LA ENTREGA**: la bolsa nueva (BolsaDelEscriba — clic derecho, solo entrega lo que falte, el patrón de las 22 bolsas de la casa) contiene LAS TRES PIEZAS; EscribaKitPlayer (ModPlayer NUEVO — el kit de TestingPlayer NO se tocó) la entrega al entrar al mundo (una copia; modelo ShardPlayer.EntregarRegaloDePruebas).

**LOCALIZACIÓN ×3 SIN TOCAR LA RAÍZ**: el descubrimiento técnico del ciclo — LocalizationLoader.TryGetCultureAndPrefixFromPath (verificado contra el tModLoader.dll decompilado) acepta MÚLTIPLES archivos hjson con el MISMO prefijo `Mods.AethonMod`: el ciclo estrena `Localization/Escriba/{es-MX,es-ES,en-US}_Mods.AethonMod.hjson` — 150 CLAVES NUEVAS por idioma (ítems, proyectiles, jefe, 18 inscripciones ×4 líneas, 5 hechos, los 10 regalos con SU VOZ, susurros, UI del árbol) con paridad EXACTA de fábrica (tools/loc_escriba_v6520.py es la única fuente de verdad); los TRES de raíz INTACTOS byte a byte contra HEAD (auditado).

· **Verificación**: oráculo 0/0 (3 checkpoints) · build REAL 0/0 (2.915.598 B, md5 4ed5c314bb6d8417f4d744800305968e) · tools/audit_v6520_tmod.py TODO LIMPIO (411 entradas — las 401 de la .51.14 + los 26 archivos del ciclo; EOF exacto, 411/411 inflan, hjson raíz ×3 INTACTOS contra HEAD + los 3 nuevos paquete==árbol BYTE A BYTE, paridad ×3, los 34 vivos del ciclo en la DLL + REGRESIÓN: los 13 vivos clave de la .51.14 intactos, las 7 texturas presentes) · headless (Sandboxing→Constructing→Adding→Resizing→Configuring→Finalizing→Recipes→Creando mundo, 0 EXCEPCIONES — el keybind, la reflexión de inscripciones y los hjson nuevos cargan limpios) · texturas verificadas con VLM (montaje 6 iconos: sin nada roto, 5-9/10).

**PUBLICADA**: ⏳ PUBLICACIÓN PENDIENTE (los IDs REALES se escriben SOLO tras verificar el CDN — la lección refundida de Task 67/83).
"""

SNAPSHOT_INTRO = """> **Este documento = "¿por dónde nos quedamos?"** — se actualiza en cada entrega.
> Última actualización: v6.52.0 (LA BOLSA DEL ESCRIBA: EL RAYO MASIVO ·
> EL ARMA VIVA · LA COREOGRAFÍA. La letra: «no toques nada del mod, en
> su lugar crea algo nuevo… todo lo creado nuevo lo puedes meter en una
> nueva bolsa y dárselo al jugador» — tras investigar Calamity Overhaul
> (armas con conciencia + árboles de skills), los rayos masivos de
> Calamity/Fargo's/Stars Above/CWR (decompilados) y el Meowzers Boss
> Fight del dev de Lunar Veil (coreografía). CUMPLIDO AL BYTE: git diff
> = build.txt y NADA MÁS — 26 archivos NUEVOS, los hjson de raíz
> INTACTOS byte a byte. LO NUEVO: (1) EL PRIMER TRAZO — el arma del
> rayo masivo de 7 capas (BeamLib: fBm, halo/cuerpo/core, diamantes
> viajeros, luz, convergencia Last Prism — SIN maná), (2) LA PLUMA
> CONSCIENTE — el arma viva con 18 inscripciones en 3 ranuras, árbol
> con [P], hechos + regalos + el nivel que VIAJA CON EL ARMA, (3) EL
> ESCRIBA — el jefe demo de la coreografía (CoreoGuion: telegraph→
> activo→recuperación, barra de conjuro, luz escénica por beats,
> cámara cine, 2 transiciones-evento, el fakeout, el nuke de 216 t,
> el agarrón La Anotación y la muerte con firma caligráfica), TODO en
> LA BOLSA DEL ESCRIBA entregada al entrar al mundo. .tmod 2.915.598 B
> md5 4ed5c314bb6d8417f4d744800305968e, 411 entradas, oráculo 0/0,
> build real 0/0, headless 0 excepciones, audit_v6520 TODO LIMPIO.
> PENDIENTE EN JUEGO: el checklist de la .52.0 abajo (ítem 1) — y el
> de la .51.14 (ítem 2, QUE NADIE HA PODIDO PROBAR AÚN).
"""

PASO_1 = """1. **⏳ v6.52.0 PUBLICADA — LA BOLSA DEL ESCRIBA: EL RAYO MASIVO · EL
   ARMA VIVA · LA COREOGRAFÍA — EN JUEGO (TODO ES NUEVO, NADA DE LO
   ANTERIOR SE TOCÓ).**
   Checklist: (a) LA BOLSA — al entrar al mundo aparece «La Bolsa del
   Escriba» en el inventario (una copia; ábrela con CLIC DERECHO:
   entrega El Primer Trazo + La Pluma Consciente + la Pluma
   Convocadora, solo lo que falte), (b) EL RAYO MASIVO — sostén El
   Primer Trazo y MANTÉN clic: cinco trazos convergen en UN rayo de
   ruido Perlin vivo (3 s para el bloqueo: el daño se TRIPLICA y la
   pantalla destella), barre con el ratón (la punta se DESLIZA, no
   salta), apunta a una pared (el estallido de impacto + los
   diamantes de energía RECORRIENDO el cuerpo), y el maná JAMÁS baja
   (arma de prueba), (c) EL ARMA VIVA — dispara La Pluma Consciente:
   el ESCRIBA habla (4 líneas al primer encuentro), pulsa [P] con la
   pluma EN LA MANO: el ÁRBOL DE INSCRIPCIONES (3 columnas:
   Filigrana/Verso/Colofón; equipa con clic, los bloqueados muestran
   n/N), los HECHOS avanzan en la fila de abajo (20 kills abre
   Verso Doble; 10 minutos sostenida abre Pluma Ajena…), (d) LOS
   REGALOS — vence cualquier JEFE con la pluma en el inventario: el
   Escriba REACCIONA con su línea y desbloquea una inscripción (EoC →
   Subrayado +8 crítico…), (e) EL NIVEL DEL ARMA — vence jefes con la
   pluma: «La pluma sube al nivel N» (+12 % por nivel) — tira la pluma
   al suelo y recógela: el nivel VIAJA con ella, (f) EL ESCRIBA — usa
   la Pluma Convocadora: LA PORTADA (la tinta llueve, la cámara se
   acerca, el anuncio), y luego la coreografía: cada ataque con SU
   NOMBRE en la barra de conjuro, la línea de tinta que SE DIBUJA antes
   del tajo (El Trazo), las páginas con charco de sombra (Lluvia de
   Versos), el anillo de plumas (Las Plumas), EL NUKE de 216 t (El
   Borrón — el retumbo crece), el FAKEOUT (El Trazo Corregido: la
   línea se AGUANTA… y golpea doble), la jaula de paredes (La Jaula),
   EL RAYO DEL JEFE barriendo (El Verso), y en la fase final EL
   AGARRÓN (La Anotación: acércate a 230 px al final del telegraph y
   te ESCRIBE dentro de una página), las TRANSICIONES con cámara y
   baño de luz al 66 % y 33 %, y LA MUERTE: la pausa, LA FIRMA
   CALIGRÁFICA que se escribe sola, y el jefe deshecho en páginas,
   (g) LA LUZ ESCÉNICA — observa al Escriba en la oscuridad: violeta
   LATIENDO mientras telegrafía, DESTELLO blanco al golpear, ámbar
   tenue al recuperar — el «más luces» de la letra.
2. **⏳ v6.51.14 PUBLICADA (release 422974533 + asset 673276658 · CDN
   verificado byte a byte · `/releases/latest` = v6.52.0 AHORA) — LA
   CURA DEL CRASH DE LA SOMBRA II — EN
   JUEGO: PRIMERO LA SOMBRA II (que ya no crashea) Y LUEGO EL CHECKLIST
   COMPLETO DE LA .51.13 QUE EL CRASH SE LLEVÓ (SIGUE SIN PROBARSE).**
   *(Checklist completo en el ítem — sin cambios: (0) Sombra II sin
   crash, (a) festín chico, (b) escuadrón, (c) paridad, (d) hotbar,
   (e) presencia 100/25/0, (f) lazo, (g) concierto, (h) rayitos,
   (i) Sombra II entregada, (j) sin maná, (k) receta.)*"""


def main():
    # === CHANGES.md: la entrada nueva ARRIBA ===
    with open("CHANGES.md", encoding="utf-8") as f:
        cambios = f.read()
    with open("CHANGES.md", "w", encoding="utf-8") as f:
        f.write(CAMBIOS + "\n" + cambios)

    # === STABLE-SNAPSHOT.md: título + intro + PRÓXIMOS PASOS ===
    with open("STABLE-SNAPSHOT.md", encoding="utf-8") as f:
        snap = f.read()

    snap = snap.replace(
        "# AethonMod — ESTADO ACTUAL (v6.51.14)",
        "# AethonMod — ESTADO ACTUAL (v6.52.0)", 1)

    # La intro vieja (del bloque '>' hasta el primer '- **Tag**') se reemplaza.
    ini = snap.index("> **Este documento")
    fin = snap.index("- **Tag**:")
    snap = snap[:ini] + SNAPSHOT_INTRO + "\n" + snap[fin:]

    # PRÓXIMOS PASOS: el ítem 1 viejo (.51.14) pasa a ser ítem 2 (ya
    # reescrito arriba); insertar el nuevo ítem 1 tras el encabezado.
    marca = "## 🚧 PRÓXIMOS PASOS SUGERIDOS (en orden)\n\n"
    assert marca in snap
    # quitar el ítem 1 y 2 viejos (hasta el ítem 3)
    i1 = snap.index("1. **⏳ v6.51.14 PUBLICADA")
    i3 = snap.index("3. **✅ v6.50.115 PUBLICADA")
    snap = snap[:i1] + snap[i3:]
    # insertar los dos ítems nuevos
    snap = snap.replace(
        marca,
        marca + PASO_1.replace("{", "{{").replace("}", "}}") + "\n", 1)
    # (el replace anterior insertó con llaves escapadas — des-escapar)
    snap = snap.replace("{{", "{").replace("}}", "}")

    # renumerar el viejo 3+ hacia 3+ (ya está bien: era 3)
    with open("STABLE-SNAPSHOT.md", "w", encoding="utf-8") as f:
        f.write(snap)
    print("docs escritas")


if __name__ == "__main__":
    main()
