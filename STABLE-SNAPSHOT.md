# AethonMod — ESTADO ACTUAL (v6.50.41)

> **Este documento = "¿por dónde nos quedamos?"** — se actualiza en cada entrega.
> Última actualización: 2026-09-29 (tag `v6.50.41`, release publicada).

## ✅ ESTADO VERIFICADO (build/forense, NO en juego)

- **GitHub = fuente de la verdad** (regla de la casa, re-confirmada por el usuario:
  "el repo de github siempre es el verdadero"): tag `v6.50.41`.
- **v6.50.41 = EL MEDIO DÍA DEL DESTELLO** (cuatro pedidos en uno):
  (1) **LA CAPA DE OSCURIDAD MUERE DE RAÍZ** («mejor quita la capa de
  oscuridad, no se ve nada bien, se ve horrible»): VeloLib.cs +
  VeloDona.rawimg BORRADOS (paquete 396→395, diff quirúrgico de una
  textura); fuera el pintor, EL SOL NEGRO, las luces del frame, el aviso
  del Grimorio y el flash blanco de pantalla completa — AethonLlegadaSistema
  queda con EL ESPEJO + EL RELOJ + el temblor; los telegraphs vuelven a
  dibujarse SIEMPRE en el pase del mundo; el horizonte vuelve al DORADO
  vivo (el violeta muerto fuera; el violeta del ECLIPSE —el ataque— se
  queda); los textos `LlegadaOscuridad/OscuridadGrimorio/OscuridadSinGrimorio`
  ELIMINADOS y `Muerte` reescrita. (2) **EL SOL SIN TELETRANSPORTE DE
  VERDAD** — el bug de la v6.50.40: con el sol en la TARDE el aterrizaje
  (`time >= 26999`) se disparaba al INSTANTE y el sol saltaba HACIA ATRÁS
  al mediodía; ahora la ventana es **[26999, 27001]** (en el jefe Y en el
  espejo) y `ModifyTimeRate` mide EL RESTANTE hasta el PRÓXIMO mediodía
  **POR LA NOCHE** (`restante = resto del día + nightLength + mañana`):
  rate = restante × 0.08 (techo 110×, piso 1) — tarde → ocaso → NOCHE
  COMPLETA → amanecer → mañana → mediodía (~12-14 s el peor caso, timelapse
  VISIBLE); antes del centro → directo; aterrizaje desacelerado en 27000.
  SIMULADO en 5 escenarios: JAMÁS salta hacia atrás. (3) **EL DESTELLO NACE
  DEL SOL**: brillo radial CENTRADO en el sol (brazo hasta la esquina más
  lejana ×1.06 — TRANSPARENTE en los bordes; halo cálido; núcleo cegador;
  8 rayos), curva crece(0-45)/ARDE(45-75)/disuelve(75-120) con smoothstep,
  pintado por ColaSierpeSky sobre la posición REAL del sol — y el sol NO SE
  APAGA (aditivo encima del sol vivo). (4) **AETHON NO NACE DEL CENTRO DEL
  SOL**: `PosicionBajoElSol(220)` lo materializa BAJO él (espacio del fondo
  → mundo vía `Matrix.Invert(GameViewMatrix)` + red anti-NaN); el acto 13
  es EL DESCENSO (sin apagón ni mensaje).
- **v6.50.40 = LA OSCURIDAD BAJO LA INTERFAZ, CON AGUJEROS DE LUZ** (la
  retroalimentación del usuario sobre la v6.50.39: «bueno, parece estar
  bien, pero la capa de oscuridad no debe estar sobre todo, la capa debe
  estar por debajo de la interfaz de usuario, ademas no debe cubrir ni al
  jugador ni al jefe, la luz que tienen se supone que quita esa
  oscuridad"): (1) el velo MUDA DE PUNTO — de `Main.OnPostDraw` (la capa
  MÁS ALTA del frame, sobre el mundo Y la interfaz) a **LA PRIMERA CAPA
  DE LA INTERFAZ** (`ModifyInterfaceLayers` + `LegacyGameInterfaceLayer`
  índice 0 — la técnica probada de v6.50.37/.38, con el contrato del lote
  del decompile en un `finally` inquebrantable): el mundo se apaga, el
  HUD/el mapa/el chat/el cursor quedan USABLES (el mapa a pantalla
  completa tampoco se apaga). (2) **EL MOSAICO**: el velo ya no es un
  rectángulo entero — bandas de velo pleno que esquivan las PLAZAS de las
  luces + una DONA radial por luz (`VeloDona.png`: núcleo limpio 0.76 /
  penumbra smoothstep / pleno 0.90 hasta las esquinas) dibujada de la luz
  CHICA a la GRANDE y recortada alrededor de las ya pintadas (resta de
  rectángulos con ping-pong de buffers): **cada píxel del velo lo pinta
  UNA sola pieza** — sin doble oscurecimiento, sin costuras, la luz chica
  JAMÁS tapada (verificado por simulación: cobertura completa, cero
  dobles, oscuridad 0 en cada centro). (3) LOS AGUJEROS: Aethon 780 px
  (ENTERO visible; 360 violeta en eclipse) + brillo 430; el jugador 235
  px SOLO con Grimorio ≥50; las balas 88/130. (4) las luces viven por
  TICK y no por frame (sello de `GameUpdateCount`: sin parpadeo a 144 Hz,
  sin apagón al pausar; `PintoresSiempre` vence a los 2 ticks). (5) el
  contrato anti-crash INTACTO: cero RTs/shaders/blends custom — solo
  rectángulos y UNA textura; cerrojo de 3 caídas + log.
- **v6.50.39 = LA TÉCNICA DE WRATH OF THE GODS** (tres pedidos en uno):
  (1) la máscara de luz MUERE — el usuario la rechazó («solo hace que todo
  este negro y no es el oscurecer que quiero»); la ingeniería inversa del
  addon (TheFifthCircle/WrathOfTheGodsPublic, su `TotalScreenOverlaySystem`:
  velo sobre el frame en `Main.OnPostDraw` + contenido dibujado DESPUÉS)
  dio la técnica, y **VELOLIB** la librería nueva de la casa:
  `Velo.Ver/Apagar/Luz/SobreElVelo/PintoresSiempre` — el velo violeta-negro
  al 93% con EL FLASH del climax como crossfade vivo blanco→negro, Aethon
  dorado 430 px (210 violeta en eclipse), el círculo 235 px SOLO con Grimorio
  ≥50, las balas, EL SOL NEGRO y los telegraphs por el pintor.
  (2) **el sol ya no se teletransporta**: sin corte al alba y sin snap — la
  noche entera corre a 300× (la luna barre, el alba llega sola) y el mediodía
  se reacha con aterrizaje desacelerado (`rate = distancia×0.08`, piso 1):
  el sol se POSA, y el espejo es CONVERGENTE (un cliente atrasado termina su
  carrera antes de congelarse).
  (3) **«al compilar el juego se cierra»**: la clase de riesgo muere con la
  máscara — cero GraphicsDevice/RTs/capas de interfaz; OnPostDraw con Begin/End
  propios; cerrojo de tres caídas + TODO escrito en el log.
- **v6.50.38 = LA OSCURIDAD ENFOCADA** (el reporte del usuario tras probar la
  v6.50.37: «la oscuridad solo hace que la pantalla se apague»): los agujeros
  de luz NO salían — la máscara se estampaba con `ZoomMatrix` aplicada DOS
  VECES (los agujeros ya están en píxeles de dispositivo; el quad volvía a
  transformarse). Con zoom 100% coincidía POR CASUALIDAD, pero Terraria
  FUERZA zoom > 1 en pantallas grandes (`ForcedMinimumZoom =
  max(ancho/1920, alto/1200)` — 1440p = 1.33×, 4K = 2×) y los agujeros
  volaban fuera de la pantalla: apagón plano. **EL FIX**: la máscara se
  estampa con `Matrix.Identity` sobre el rect del VIEWPORT — UNA sola
  transformación, a CUALQUIER zoom. **EL BLINDAJE**: el render target se
  devuelve en un `finally` inquebrantable (una excepción ya no puede amarrar
  la máscara y matar la pantalla), el lote de la capa se restaura con los
  parámetros EXACTOS del decompile (`DepthStencilState.None`), y si la
  máscara falla una vez el sistema cae al MODO VELO y **lo escribe en el
  log** (`Logging.PublicLogger.Error` + stack — cero `catch {}` ciegos).
- **v6.50.37 = EL MEDIO DÍA DE LA OSCURIDAD** (la petición: «has que sea mas
  grande el jefe… cuando Aethon aparece el mundo debe temblar… si es de noche
  se hace de dia y si es de dia el tiempo avanza hasta que el sol quede
  centrado… destellos de luz aparecen en el cielo… el sol brilla con
  intensidad y de ahi aparece Aethon, luego el sol se vuelve negro… toda la
  luz a sido concentrada en un lugar… usa la oscuridad de dont starve y
  mejora esa oscuridad… el mundo se vuelve oscuro menos los alrededores del
  jugador y Aethon… solo un pequeño circulo si el grimorio es nivel 50 o
  superior"):
  1. **MÁS GRANDE ×1.5**: hitbox 220×220, núcleo 130 px, halo 295, rayos
     255-420, coronas 218×134/300×90, luz del mundo 1.55/1.35/0.95.
  2. **LA LLEGADA EN CUATRO ACTOS** (ai[1]=10·11·12·13): EL MUNDO TIEMBLA
     (corte al alba si era de noche + kicks 4→13 px) → EL TIEMPO CORRE
     (240×: el sol atraviesa el cielo y queda CLAVADO en el mediodía exacto
     — time=27000, x=centro EXACTO, congelado mientras la luz viva) →
     EL SOL BRILLA CON INTENSIDAD (la ventana cegadora de 490 px + EL FLASH
     blanco de pantalla completa — Aethon SE MATERIALIZA DE ÉL) → EL SOL SE
     VUELVE NEGRO (el mensaje «TODA LA LUZ HA SIDO CONCENTRADA EN UN
     LUGAR… SOLO ÉL BRILLA» + LA OSCURIDAD entra).
  3. **LA OSCURIDAD PRIMORDIAL** (OscuridadSistema.cs, NUEVO): la máscara de
     luz — RenderTarget a media resolución limpiado a gris (1→0.035) con
     AGUJEROS ADITIVOS (AgujeroLuz.png) multiplicado sobre la escena
     (BlendState multiplicativo): AETHON brilla dorado a 1.060 px (ES luz
     pura; en su ECLIPSE se encoge a 360 violeta), EL JUGADOR solo un
     círculo de 235 px **si el Grimorio ≥ 50** (NivelGrimorioPublico), LAS
     BALAS del jefe abren agujeros pequeños, LOS TELEGRAPHS se redibujan
     sobre la oscuridad y EL HUD queda USABLE (capa #0 de la interfaz).
  4. **EL SOL NEGRO**: disco absoluto (cubre al sol real — posición LITERAL
     del decompile de DrawSunAndMoon vía BackgroundViewMatrix.EffectMatrix)
     + rim dorado latiendo + corona de 12 filamentos + destellos fantasma.
  5. **EL CIELO DE LA LLEGADA** (ColaSierpeSky): 18 destellos con estrella,
     la VENTANA SIGUIENDO AL SOL en su carrera, el horizonte MUERTO a
     violeta (0.028) cuando la oscuridad manda.
  6. **LA SINCRONÍA**: PreUpdateTime (todas las máquinas, tras la IA y antes
     de UpdateTime — verificado en decompile) reconstruye el estado desde
     ai[]: el servidor manda, cada cliente padece su propia carrera del sol.
     Al morir: el tiempo REANUDA, la oscuridad se disuelve en 45 t DURANTE
     la contracción (el FLASH final CEGA) y el sol negro se despide lento.
- **v6.50.36 = AETHON, LA LUZ PRIMORDIAL, LA ENCARNACIÓN** (la petición: «el
  jefe se ve feo… mejor hacerlo una luz brillante, el jefe es una potente luz
  que ataca al jugador con ataques devastadores» + «Aethon es masculino»):
  1. **LA SIERPE MUERE, LA CUARTA VEZ ES LA DEFINITIVA**: tres anatomías
     fracasaron (dragón código «no se parece en nada», dragón sprites «se ve
     horrible», sierpe «se ve feo») — la lección: no dibujar CRIATURAS,
     dibujar FENÓMENOS. El jefe ES LA LUZ: un SOL VIVO (núcleo blanco +
     halo dorado + rayos radiales 10→18 + dos coronas de perlas elípticas
     girando en sentidos opuestos + chispas 5+fase — todo aditivo, todo
     código). UN SOLO NPC 160×160 (la cadena de 68 vértebras, los archivos
     de segmentos y arte de la sierpe y sus 4 sprites BORRADOS: el paquete
     baja a 395 entradas).
  2. **LOS SEIS ATAQUES DEVASTADORES**: EL JUICIO DE LUZ (columnas que
     nacen 1.200 px arriba de la posición PREDICHA y aceleran a 52 px/t
     tras 45 t de caída lenta — el telegraph ES el proyectil), EL RAYO
     PRIMORDIAL (arco PerlinBolt grueso + lluvia densa), LA NOVA (anillos
     con 4 HUECOS de 30°), LA CRUZ giratoria (P3+), EL DESTELLO encadenado
     (P3+, con línea guía) y EL ECLIPSE (P4+: la luz se apaga, la
     atracción tira de ti, solo las balas brillan — y EL REGRESO con nova
     gratis). La rotación heredada: nunca el mismo dos veces.
  3. **LA LLEGADA**: EL CIELO SE ENCIENDE (resplandor creciente + 7 columnas
     lejanas + LA VENTANA del núcleo) y el resplandor dorado PERMANENTE
     mientras vive — en el eclipse el cielo también se apaga.
  4. **EL GÉNERO DE AETHON**: Él/He/lo/him en los sabores del Grimorio
     (el título "La Luz Primordial" queda); los iconos regenerados = EL SOL
     (barra y bestiario).
- **v6.50.35 = EL SEÑOR DEL MUNDO (la corrección de género)** (la petición: «porque
  señora, la sierpe es macho asi que seria señor del mundo»): (1) el título
  ESTRENA EN EL JUEGO — el anuncio de aparición del jefe ahora dice «LA SIERPE
  DE HUESO DE LA LUZ — EL SEÑOR DEL MUNDO — se alza del subsuelo» (es-ES) /
  «THE BONE SERPENT OF THE LIGHT — THE LORD OF THE WORLD» (en-US); (2) "her
  line" → "his line" (la embestida en-US); (3) 7 comentarios de código
  (6× Señora→Señor + 1× Diosa→Dios); (4) el release v6.50.34 RENOMBRADO en
  GitHub (nombre + body vía API, 0 "Señora" residual). "Ella me RECONOCIÓ"
  queda: es Aethon, LA LUZ (sustantivo femenino), no la sierpe.
- **v6.50.34 = LA SIERPE ESTELAR, SEÑOR DEL MUNDO** (la petición: «se ve horrible
  jajajajaja, mejor borra a ese jefe y olvidemonos de el — en cambio crea como jefe
  a la misma sierpe, pero mas grande y mas largo, y mejora su IA»):
  1. **EL DRAGÓN, BORRADO**: reversión a v6.50.31 del arte completo (los 4 .cs +
     los 6 sprites) y `AethonSierpeAla.png` eliminado — el paquete vuelve a 399
     entradas. El bug del Guía muere con él (ya no existe `Segmento()` — DLL verificada).
  2. **MÁS GRANDE**: ESC 1.4→1.85 (cada hueso +32%); cráneo 168 px, vértebra 100,
     cola 52; las ALETAS del abanico recorren toda la columna (6/14/22/30/38/46).
  3. **MÁS LARGA**: 46→68 vértebras (54 de mundo + 14 del fondo), HUECO 84 — la
     columna ~5.700 px (tres pantallas y media de 1080p); el cine de muerte se
     estira (276 t de desarticulación: TODOS los huesos, uno a uno).
  4. **LA IA MEJORADA**: EL CLAVADO AÉREO (nuevo estado vertical: telegraph 20 t +
     caída a través de la presa), LA ROTACIÓN (fase 2+: ram ↔ clavado, NUNCA el
     mismo dos veces — impredecible), EL RAM EN CADENA (fase 3+: 2-3 embestidas
     desde lados opuestos — la vuelta en U del DoG), LA PREDICCIÓN ADAPTATIVA
     (lead 10-34 t según distancia), EL ANTI-CAMPING (presa quieta 1,5 s →
     paciencia 24 t), LA FURIA P5 (todo más rápido + EL ALIENTO DOBLE por arco),
     y el render 100% sincronizado (ai[0]/ai[2]/ai[3] — el contrato de la casa).
- **v6.50.33 = el dragón de sprites** (veredicto del usuario: «se ve horrible» —
  BORRADO por completo en v6.50.34).
- **v6.50.32 = el intento 100 % código** (veredicto del usuario: «no se parece
  en nada» — sustituido en v6.50.33 y ahora revertido del todo).
- **v6.50.31 = FIXES FORENSES del client.log** (verificados por el usuario: "bien,
  ya no hay errores"): Begin-sobre-Begin del PreDraw, funeral de texturas al hilo
  principal, .plr corrupto documentado, simetría hjson ×18. **El fix del
  CerrarLoteSiAbierto viaja con la reversión de v6.50.34** (vivió en v6.50.31).
- **Build headless 0 errores / 0 warnings** contra tModLoader 2026.07.3.0 REAL
  (verify.csproj 10 refs + el `-build` real con `DOTNET_ROLL_FORWARD=Minor`).
- **`.tmod` v6.50.41**: 6.318.486 bytes, md5 f18eea6f…, **395 entradas** auditadas
  (diff quirúrgico contra la v6.50.40: SOLO muere `VeloDona.rawimg`; EOF exacto
  tabla+blobs = fsize, TODAS inflan, 24 planas); hjson: las 3 claves muertas
  (`LlegadaOscuridad/OscuridadGrimorio/OscuridadSinGrimorio`) AUSENTES en ambos
  idiomas y la baja SIMÉTRICA (es 496→493, en 498→495 — la brecha restante es
  la de siempre: los 6 falsos positivos multilineales Ciclo/Telegrafiada vs
  Cycle/Telegraphed/Afterwards/Hold); DLL: 13 símbolos muertos AUSENTES
  (DibujarSolNegro/DibujarVeloMosaico/VeloSistema/PintarSobreElVelo/
  RegistrarLuces/OscuridadObjetivo/…) y los nuevos PRESENTES
  (`PosicionBajoElSol`/`SUB_DESCENSO`/`ModifyTimeRate`/…); literales UTF-16
  verificados DENTRO del paquete (los muertos fuera, LlegadaLuz/Presentacion/
  Muerte dentro); **la simulación del reloj en 5 escenarios** (mañana/tarde/
  noche/mediodía exacto/tarde+1: JAMÁS salto hacia atrás).
- **Servidor headless CARGA sin excepciones** (Sandboxing → Finalizing → Choose World).
- **Release de GitHub** con el `AethonMod.tmod` adjunto:
  <https://github.com/Leo0x01/Aethon-Mod-Terraria/releases/tag/v6.50.41>

## 🎮 PENDIENTE DE VERIFICACIÓN EN JUEGO (por el usuario)

Toda la cadena v6.50.24 → v6.50.41 está implementada y build-verificada. El usuario vio la
v6.50.39/.40 en juego y pidió: quitar la capa de oscuridad + el sol sin teletransporte +
el destello nacido del sol + el jefe fuera del centro del sol.
Checklist de la v6.50.41:

1. **LA LLEGADA LIMPIA — EL MEDIO DÍA DEL DESTELLO (v6.50.41, EL PRIMERO)**:
   invocar a Aethon y verificar: (a) **CERO OSCURIDAD** — el mundo NUNCA se
   apaga (ni velo, ni sol negro, ni apagón, ni mensaje «TODA LA LUZ HA SIDO
   CONCENTRADA…» — todo eso murió); (b) **EL SOL SIN TELETRANSPORTE**:
   invocarlo EN LA TARDE (sol pasado el centro) y ver la carrera COMPLETA —
   el sol sigue su tarde, el OCASO, la NOCHE ENTERA (la luna barre el cielo),
   el AMANECER y la mañana del nuevo día hasta que el sol SE POSA en el
   centro con frenada progresiva (~12-14 s, timelapse legible — JAMÁS un
   salto, JAMÁS hacia atrás); invocarlo DE MAÑANA: directo al centro;
   invocarlo DE NOCHE: la noche corre y el alba llega sola; (c) **EL
   DESTELLO NACE DEL SOL**: en el climax, un BRILLO RADIAL centrado EN EL
   SOL que crece hasta inundar la pantalla y se difumina hasta ser
   TRANSPARENTE justo en los bordes (nada de flash blanco de pantalla
   completa); (d) **EL SOL NO SE APAGA**: sigue visible y ardiendo TODO el
   combate (clavado en el centro mientras la luz viva); (e) **AETHON NO
   NACE DEL CENTRO DEL SOL**: se materializa BAJO él, en el borde inferior
   de su halo, envuelto en el pico del destello — y luego desciende a su
   órbita de pelea; (f) el temblor del acto 1 + los destellos del cielo +
   las siete columnas lejanas + la ventana siguiendo al sol durante la
   carrera (todo lo heredado que SIGUE vivo).
2. **EL JEFE AETHON — LO HEREDADO VIVO**: (a) MÁS GRANDE (el sol de código
   ×1.5 — núcleo 130 px); (b) LOS SEIS ATAQUES (juicio/rayo/nova/cruz/
   destello/eclipse — el ECLIPSE sigue encogiendo SU propia luz y poniendo
   el cielo violeta mientras dura el ATAQUE); (c) LA MUERTE: la contracción,
   EL ESTALLIDO final CEGANDO, el sol RECUPERANDO su curso y el anuncio
   «La luz ESTALLA y su resplandor REGRESA al mundo…»; (d) que NO hay
   excepciones nuevas en el client.log.
3. **EL COMPILE SIN CIERRES (v6.50.39)**: compilar el mod en tModLoader
   (Develop Mods → Build & Reload) VARIAS veces seguidas — el juego YA NO se
   cierra (y ahora con aún MENOS superficie: la oscuridad entera fue
   borrada).
4. **LOS FIXES v6.50.31 (ya confirmados por el usuario: "bien, ya no hay errores")**: garganta
   al cargar el aliento, corona de anillos, motas, arco boca→presa y salida de mundo limpia.
5. **Las 5 armas de rayo** (bolsa 18): el Bastón de Rayo Primordial, el Arco de Sobretensión
   (anclado a la mano), el **Colmillo de Vena Trueno** (trío naranja+amarillo cayendo con
   recada/parpadeo), el Rúnico y el Perlin (el arco que sigue al cursor).
6. **LA FURIA con el motor de vanilla (v6.50.29)**: la chusma del bioma NACE (5º intento —
   ahora via el motor natural de spawn de Terraria, anillo 0.52-0.7× pantalla, nunca en
   paredes), el **indicador de oleada** abajo-derecha («Oleada k: X %» + barra estilo
   invasión), el guardián por zona al borde del cuadro, el avance por muertes (18 en la 1).
7. **El dado del invierno (v6.50.30)**: Deerclops SOLO al 1 % por jefe de oleada (borde
   opuesto, anuncio propio) — ya NO es guardián de nieve ni del Juicio.
8. **Los diálogos de devorar** (v6.50.30): cada esencia usa la voz del SABOR DE SU JEFE
   (12 jefes × 3 variantes), con tiempo de lectura 4-10 s.
9. **El libro YA NO RUGE al hablar** (v6.50.30 — el rugido solo suena cuando LLEGA un jefe).
10. **El jefe Aethon — combate**: la avalancha del EMERGER murió (una sola volleada por
    emersión), la barra de vida con icono, el RAM y el Aliento.
11. **El destello del Sol / Supernova / BlackHole**: la cruz de 8 rayos (`DestelloFinal`) sin
    disco plano (los velos de la capa de UI murieron en v6.50.28).
12. **El brillo de los rayos**: funda gaussiana al 24 % + soft-add — dos rayos cruzándose ya
    no hacen "cortes" ni clipean.


## 🗑️ DOC-ROT / DEUDA TÉCNICA CONOCIDA (detectada, sin arreglar)

Prioridad baja — arreglar en la próxima sesión de código si el usuario aprueba:

1. **Tooltip de La Carnada** (es-ES y en-US) aún dice "11 = EL JUICIO: los **7** guardianes
   ×15" — desde v6.50.30 son **SEIS** (los anuncios ya dicen SEIS; el tooltip quedó viejo).
2. **Precio de las esencias**: `Item.value = buyPrice(0,10,0,0)` = **10 de ORO**, pero los
   comentarios (TheWitness.cs, EsenciasJefes.cs) y docs dicen "10 de PLATINO". Decidir cuál
   es el valor deseado y alinear código+comentarios+hjson.
3. **~~Sprites dormidos~~ RESUELTO en v6.50.36**: los 4 sprites de la sierpe
   (Cabeza/Mandíbula/Vértebra/Cola) fueron BORRADOS con el rediseño de LA LUZ —
   el paquete bajó a 395 entradas (solo viven los del ARMA La Sierpe Estelar).
4. **Los otros 4 jefes no tienen icono `_Head_Boss`** (solo Aethon tiene barra con icono).
5. **Sin bestiario** (cero `SetBestiary` en jefes; segmentos ocultos).
6. **`ParticlePresets.NovaFlash` + `VFXCore.NovaBurst` sin llamadores** (comentarios viejos
   de SunProjectile/BlackHoleProjectile aún las citan).
7. **Código muerto**: `RayoLib.Quad/Angulo/Junta` (pincel de banda pre-primitivas); assets
   retirados aún cargados por convención (BoltHalo/BoltCore/BoltChain → `BandaTex`/`VenaTex`).
8. **TODOs de red**: voces del libro en servidor DEDICADO; `ShardSyncSystem.SyncResonance`
   es un stub heredado (la resonancia viaja por `MsgCronica`).
9. Ítems de prueba pendientes de retirar antes de un release "público": `LevelUpTester`,
   `CarnadaDelGrimorio`, `BossSummonBag`, los 4 tests de VFX.

## 🚧 PRÓXIMOS PASOS SUGERIDOS (en orden)

1. **El usuario prueba v6.50.41 en juego** con el checklist de arriba — LA
   PRUEBA DEL FIX: el sol SIN teletransporte (en la tarde → día completo por
   la noche), el destello radial nacido del sol y Aethon materializándose
   BAJO él, con CERO oscuridad.
2. Según lo que reporte: pulir lo que falle (rayos/oleadas/diálogos/destello son los frentes
   calientes).
3. Limpieza de doc-rot (la lista de arriba, ~1 sesión pequeña).
4. Ideas ya investigadas y LISTAS para implementar (material en `research/`):
   - **5 ideas de Coralite** (worklog 2-a): rayo-trío (YA hecho = Vena Trueno), cañón
     electromagnético, rayo persecutor de ReverseFlash, dash-relámpago de la ThunderveinBlade,
     capa "flow" ¼ de ancho.
   - **Eventos cósmicos por nivel** (esqueleto en ShardLevelSystem: "Lluvia de luz estelar,
     Rifts… Fase 10").
   - Accesorio con aura que evoluciona negro→dorado→rojo (idea antigua del usuario;
     AuraLib ya soporta perfiles).

## 🔒 REGLAS INVIOLABLES AL RETOMAR

- **GitHub manda**: `git fetch` + comparar ANTES de tocar nada (el sandbox se resetea y el
  repo local puede quedar atrás).
- **Leer el worklog** (`/home/z/my-project/worklog.md`): historial completo de sesiones
  (R1…R64 + exploraciones 3-a…3-d).
- **No tocar**: el icono del Orbe Cósmico (`CosmicOrbMinion.png` — imagen aportada por el
  usuario, intacta desde v6.01); la fórmula del sello idempotente de `OleadaNPC.Marcar`;
  el motor de spawn de vanilla de la furia (la lección de los 5 intentos — worklog v6.50.29).
- **Al entregar**: build 0/0, auditoría `.tmod`, commit+tag+push+release con `.tmod`,
  entrada en worklog, y actualizar este snapshot + README.

---

## 📜 HISTORIAL DE ESTADO (contexto de versiones)

| Versión | Estado | Notas |
|---|---|---|
| **v6.50.41** | ✅ Build-verificada, ⏳ en juego | EL MEDIO DÍA DEL DESTELLO (cuatro pedidos): (1) LA CAPA DE OSCURIDAD MUERE DE RAÍZ (VeloLib + VeloDona borradas; fuera el sol negro, las luces, el aviso del Grimorio y el flash de pantalla completa — paquete 396→395) · (2) EL SOL SIN TELETRANSPORTE DE VERDAD (bug de la v6.50.40: la tarde disparaba el aterrizaje al instante y el sol saltaba HACIA ATRÁS; ventana [26999, 27001] + restante hasta el PRÓXIMO mediodía POR LA NOCHE: tarde → ocaso → noche completa → amanecer → mediodía, 110× máx con aterrizaje — simulado en 5 escenarios, jamás hacia atrás) · (3) EL DESTELLO NACE DEL SOL (brillo radial centrado en él, difuminado a TRANSPARENTE en los bordes — y el sol NO se apaga) · (4) AETHON NO NACE DEL CENTRO DEL SOL (se materializa BAJO él vía la inversa de la matriz de vista; acto 13 = EL DESCENSO) |
| **v6.50.40** | ✅ Build-verificada, ⏳ probada (retirada) | LA OSCURIDAD BAJO LA INTERFAZ, CON AGUJEROS DE LUZ (feedback de la v6.50.39: «la capa no debe estar sobre todo… no debe cubrir ni al jugador ni al jefe, la luz que tienen se supone que quita esa oscuridad»): el velo muda de `OnPostDraw` a **LA PRIMERA CAPA DE LA INTERFAZ** (HUD/mapa/chat/cursor usables) y deja de ser un rectángulo entero — **EL MOSAICO DISJUNTO** (bandas de velo pleno + una DONA radial por luz, de la chica a la grande y recortada: cada píxel pintado UNA vez) · Aethon ENTERO visible (agujero 780 px; 360 violeta en eclipse), círculo del jugador 235 px SOLO Grimorio ≥50, balas 88/130 · luces por TICK (sin parpadeo a 144 Hz ni apagón en pausa) · contrato anti-crash intacto (cero RTs/shaders/blends, cerrojo + log) — el usuario la vio en juego y pidió QUITAR la oscuridad → v6.50.41 |
| **v6.50.39** | ✅ Build-verificada, ✔ probada en juego | LA TÉCNICA DE WRATH OF THE GODS: la máscara de luz MUERE (rechazada por el usuario) y nace **VELOLIB** — la oscuridad como en WotG (velo sobre el frame en `OnPostDraw` + las luces dibujadas DESPUÉS) · EL SOL ya no se teletransporta (noche entera a 300× + aterrizaje desacelerado al mediodía, sin corte al alba y sin snap) · el fix de «al compilar el juego se cierra» (cero GraphicsDevice/RTs, cerrojo de 3 caídas + log) — probada por el usuario: «bueno, parece estar bien», con dos correcciones pedidas → v6.50.40 |
| **v6.50.38** | ✅ Build-verificada, ⏳ en juego | LA OSCURIDAD ENFOCADA (fix del reporte «la oscuridad solo hace que la pantalla se apague»): la máscara de luz se estampaba con el zoom aplicado DOS VECES y en pantallas grandes (ForcedMinimumZoom > 1) los agujeros volaban fuera — ahora identidad + viewport, a cualquier zoom · blindaje del render target (finally inquebrantable) · cerrojo con log real (cae al velo simple si falla) — el usuario la probó y la técnica entera fue reemplazada en v6.50.39 |
| **v6.50.37** | ✅ Build-verificada, ⏳ parcialmente probada | EL MEDIO DÍA DE LA OSCURIDAD: jefe ×1.5 + LA LLEGADA en cuatro actos (temblor → carrera del sol a 240× → flash → sol negro) + LA OSCURIDAD PRIMORDIAL (máscara de luz Don't-Starve mejorada). El usuario la probó y reportó el bug de la oscuridad → fix en v6.50.38 |
| **v6.50.36** | ✅ Build-verificada, ⏳ en juego | AETHON, LA LUZ PRIMORDIAL: la sierpe MUERE — el jefe es UNA LUZ BRILLANTE (sol vivo de código) + SEIS ataques devastadores (juicio de columnas + rayo + nova con huecos + cruz + destello + ECLIPSE) + el cielo se ENCIENDE + género de Aethon (Él) |
| **v6.50.35** | ✅ Build-verificada, ⏳ en juego | EL SEÑOR DEL MUNDO (corrección de género): el título estrena EN EL JUEGO (anuncio de aparición es-ES/en-US) · her→his line · 7 comentarios · release v6.50.34 renombrado en GitHub |
| **v6.50.34** | ✅ Build-verificada, ⏳ en juego | LA SIERPE ESTELAR, SEÑOR DEL MUNDO: el dragón BORRADO (reversión a v6.50.31) + ESC 1.85 y 68 vértebras (~5.700 px) + IA mejorada (clavado aéreo + rotación + ram en cadena + predicción adaptativa + anti-camping + furia con aliento doble) |
| **v6.50.33** | ✅ (borrada por decreto) | EL DRAGÓN DEL CIELO, ENCARNACIÓN SPRITE: set de 7 sprites por segmento (DoG) — veredicto: «se ve horrible»; revertida por completo en v6.50.34 |
| **v6.50.32** | ✅ (sustituida) | El intento 100 % código de Slifer — «no se parece en nada»; su arte fue reemplazado por el set de sprites en v6.50.33 |
| **v6.50.31** | ✅ Build-verificada, ✔ en juego | FIXES FORENSES del client.log: Begin-sobre-Begin del jefe (garganta/corona/arco no se dibujaban) · funeral de texturas al hilo principal (leak de GPU) · simetría hjson (18 claves es-ES) |
| **v6.50.30** (`1d26716`) | ✅ Build-verificada, ⏳ en juego | El dado del invierno (Deerclops 1 %) · diálogos de devorar por jefe · voz en silencio · avalancha del emerger |
| v6.50.29 | ✅ Build-verificada | **EL MOTOR DE OLEADAS DE VANILLA** (5ª y buena) + indicador de oleada |
| v6.50.28 | ✅ | Muerte del velo: el destello circular plano de la UI |
| v6.50.27 | ✅ | El arte del jefe Aethon 100 % código (la Sierpe Estelar) + halo de rayos al 24 % |
| v6.50.26 | ✅ | Aethon: barra, tamaño, RAM, aliento · rayos: kinks agudos + brillo sin cortes |
| v6.50.24 (`71b35b4`) | ✅ | Colmillo de Vena Trueno (Coralite) + la bolsa 18 + RayoLib al pixel |
| v6.50.18 | ✅ | LOS RAYOS DE TERRARIA DE VERDAD (RayoLib 1:1) + Arc Surge + círculo plano desterrado |
| **stable-v6.01** (`0cc89cf`) | 🔒 PUNTO DE RETORNO SEGURO | "LA GRAN LIMPIEZA" — confirmada estable por el usuario en su día; tag `stable-v6.01` |

> Si todo lo nuevo se rompiera de forma catastrófica: `git checkout stable-v6.01` es el
> punto de retorno documentado (esa versión la jugó y validó el usuario de punta a punta).
> NOTA: entre v6.01 y v6.50.x hay reescrituras masivas (grimorio, furia, rayos) — volver
> atrás también pierde contenido y GUARDADOS del libro/oleadas (el formato de persistencia
> cambió). Volver solo en emergencia.
