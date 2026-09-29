# AethonMod — ESTADO ACTUAL (v6.50.39)

> **Este documento = "¿por dónde nos quedamos?"** — se actualiza en cada entrega.
> Última actualización: 2026-09-29 (tag `v6.50.39`, release publicada).

## ✅ ESTADO VERIFICADO (build/forense, NO en juego)

- **GitHub = fuente de la verdad** (regla de la casa, re-confirmada por el usuario:
  "el repo de github siempre es el verdadero"): tag `v6.50.39`.
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
  (verify.csproj reconstruido tras el wipe del sandbox + el `-build` real).
- **`.tmod` v6.50.39**: 6.322.305 bytes, md5 1d7b8c44…, **395 entradas** auditadas
  (EOF exacto, set de la 38 menos AgujeroLuz.rawimg); cadenas del cerrojo verificadas
  DENTRO DEL PAQUETE en UTF-16 («VeloLib: el velo falló al dibujarse» / «el pintor
  del velo falló») y símbolos clave PRESENTES (VeloSistema, AethonLlegadaSistema,
  PintarSobreElVelo, DibujarSolNegro) con TODA la máscara AUSENTE
  (OscuridadSistema/DibujarOscuridad/DibujarAgujero/AsegurarMascara/FaseOscuridad).
- **hjson es-ES/en-US simétricos** (487=487 claves, parser con soporte de bloques ''').
- **Servidor headless CARGA sin excepciones** (Sandboxing → Finalizing → Choose World).
- **Release de GitHub** con el `AethonMod.tmod` adjunto:
  <https://github.com/Leo0x01/Aethon-Mod-Terraria/releases/tag/v6.50.39>

## 🎮 PENDIENTE DE VERIFICACIÓN EN JUEGO (por el usuario)

Toda la cadena v6.50.24 → v6.50.39 está implementada y build-verificada. El usuario probó la
v6.50.37 y reportó el bug de la oscuridad — el fix v6.50.38 no bastó («solo hace que todo
este negro y no es el oscurecer que quiero») y la técnica entera fue REMPLAZADA por la de
Wrath of the Gods. Checklist de la v6.50.39:

1. **LA OSCURIDAD — LA TÉCNICA DE WOTG (v6.50.39, EL PRIMERO)**: invocar a
   Aethon y verificar que AHORA SÍ (como en Wrath of the Gods): (a) EL VELO
   cayendo sobre TODO (mundo, interfaz y cursor — como su TotalScreenOverlay-
   System) con el mundo a siluetas del 7%; (b) AETHON ARDIENDO DORADO encima
   de la oscuridad (430 px de luz pura con su brasa — se ve SU brillo, no
   un agujero); (c) el PEQUEÑO círculo del jugador SOLO con Grimorio 50+
   (con libro < 50: oscuridad total y el texto lo explica); (d) LAS BALAS
   visibles; (e) EL SOL NEGRO con rim y corona + los telegraphs encima de
   todo; (f) EL FLASH del climax: blanco ciego que se apaga EN GRIS hasta
   el negro (el crossfade vivo — «la oscuridad toma el control»);
   (g) LA MUERTE: el velo disolviéndose DURANTE la contracción, el sol negro
   despidiéndose LENTO (más lento que la oscuridad) y el sol RECUPERANDO su
   curso. Si algo falla: el client.log LO CUENTA («VeloLib: el velo falló
   al dibujarse» / «el pintor del velo falló») — mandar el log.
2. **EL SOL NO SE TELETRANSPORTA (v6.50.39)**: invocarlo DE NOCHE y verificar
   que la luna barre el cielo, el alba llega SOLA y el sol POSA en el centro
   con frenada progresiva — SIN salto. De día igual: nada de teletransporte
   al mediodía, ni en SP ni (si se puede) en MP con clientes atrasados.
3. **EL COMPILE SIN CIERRES (v6.50.39)**: compilar el mod en tModLoader
   (Develop Mods → Build & Reload) VARIAS veces seguidas — el juego YA NO se
   cierra: la nueva oscuridad no toca el GraphicsDevice y todo vive con
   cerrojo + log.
4. **EL JEFE AETHON — EL MEDIO DÍA DE LA OSCURIDAD (lo heredado de v6.50.37)**:
   invocarlo (de día Y de noche, para ver los dos caminos) y verificar:
   (a) MÁS GRANDE (el sol de código ×1.5 — núcleo 130 px); (b) LA LLEGADA
   COMPLETA: EL MUNDO TIEMBLA (la pantalla sacudida ~2.5 s — como la sierpe),
   EL TIEMPO CORRE (el sol ATRAVIESANDO el cielo — ahora con aterrizaje
   natural, ver ítem 2 arriba) hasta quedar CLAVADO EN EL CENTRO,
   LOS DESTELLOS en el cielo, EL SOL BRILLANDO CON INTENSIDAD (la ventana
   cegadora creciendo)… EL FLASH BLANCO… y AETHON NACIENDO DE ÉL; (c) EL SOL
   NEGRO (disco oscuro + rim dorado + corona, centrado en el cielo TODO el
   combate) y el mensaje «TODA LA LUZ HA SIDO CONCENTRADA EN UN LUGAR…»; (d)
   LA OSCURIDAD: el mundo ENTERO apagado salvo AETHON (brillando dorado e
   intenso) y — SOLO si el Grimorio va nivel 50+ — el PEQUEÑO círculo del
   jugador (probar con libro < 50: oscuridad TOTAL); las balas del jefe se
   ven venir (los telegraphs también); el HUD (mapa/barra) SIGUE USABLE;
   (e) LOS SEIS ATAQUES (heredados) + EL ECLIPSE ahora ENCOGE el brillo del
   propio Aethon; (f) LA MUERTE: la oscuridad disolviéndose DURANTE la
   contracción, EL FLASH final CEGANDO, el sol RECUPERANDO su curso y el
   anuncio «La luz ESTALLA…»; (g) que NO hay «Excepción silenciosa» nuevas
   en el client.log (en especial nada del render de la máscara/oscuridad).
5. **LOS FIXES v6.50.31 (ya confirmados por el usuario: "bien, ya no hay errores")**: garganta
   al cargar el aliento, corona de anillos, motas, arco boca→presa y salida de mundo limpia.
6. **Las 5 armas de rayo** (bolsa 18): el Bastón de Rayo Primordial, el Arco de Sobretensión
   (anclado a la mano), el **Colmillo de Vena Trueno** (trío naranja+amarillo cayendo con
   recada/parpadeo), el Rúnico y el Perlin (el arco que sigue al cursor).
7. **LA FURIA con el motor de vanilla (v6.50.29)**: la chusma del bioma NACE (5º intento —
   ahora via el motor natural de spawn de Terraria, anillo 0.52-0.7× pantalla, nunca en
   paredes), el **indicador de oleada** abajo-derecha («Oleada k: X %» + barra estilo
   invasión), el guardián por zona al borde del cuadro, el avance por muertes (18 en la 1).
8. **El dado del invierno (v6.50.30)**: Deerclops SOLO al 1 % por jefe de oleada (borde
   opuesto, anuncio propio) — ya NO es guardián de nieve ni del Juicio.
9. **Los diálogos de devorar** (v6.50.30): cada esencia usa la voz del SABOR DE SU JEFE
   (12 jefes × 3 variantes), con tiempo de lectura 4-10 s.
10. **El libro YA NO RUGE al hablar** (v6.50.30 — el rugido solo suena cuando LLEGA un jefe).
11. **El jefe Aethon — combate**: la avalancha del EMERGER murió (una sola volleada por
   emersión), la barra de vida con icono, el RAM y el Aliento.
12. **El destello del Sol / Supernova / BlackHole**: la cruz de 8 rayos (`DestelloFinal`) sin
   disco plano (los velos de la capa de UI murieron en v6.50.28).
13. **El brillo de los rayos**: funda gaussiana al 24 % + soft-add — dos rayos cruzándose ya
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

1. **El usuario prueba v6.50.38 en juego** con el checklist de arriba — LA
   PRUEBA DEL FIX: la oscuridad con sus agujeros de luz ENFOCADOS (Aethon
   dorado + el círculo del Grimorio + las balas).
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
| **v6.50.39** | ✅ Build-verificada, ⏳ en juego | LA TÉCNICA DE WRATH OF THE GODS: la máscara de luz MUERE (rechazada por el usuario) y nace **VELOLIB** — la oscuridad como en WotG (velo sobre el frame en `OnPostDraw` + las luces dibujadas DESPUÉS) · EL SOL ya no se teletransporta (noche entera a 300× + aterrizaje desacelerado al mediodía, sin corte al alba y sin snap) · el fix de «al compilar el juego se cierra» (cero GraphicsDevice/RTs, cerrojo de 3 caídas + log) |
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
