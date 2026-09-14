# AethonMod — Historial de Cambios

# AethonMod — Historial de Cambios

## Commit v6.27 — LA BOLSA DEL ARSENAL + EL OCASO DE AETHON + LOS 6 BUGS DE RUNTIME MUERTOS

**Petición del usuario**: "hay demasiados errores, además todo lo que le
vas a dar al jugador ponlo en una bolsa o cofre y dale solo la bolsa con
todos los objetos dentro · dime que piensas de los biomas · crea una
nueva arma para aplicar el patrón gauge del Cosmic Destroyer a un arma
suprema · dime cuales son las librerías actuales del proyecto y si con
esta ultima investigación creaste o no librerías o mejoraste alguna ·
revisa los .md en el proyecto y actualízalos según corresponda · dime
que mas se puede agregar al mod según lo que investigaste".

### A. LOS 6 BUGS DE RUNTIME DEL client.log — MUERTOS POR LA RAÍZ
  Análisis del client.log subido (v6.26 en juego): 5 clases de excepción
  real + 1 warn sistémico. TODAS arregladas:
  1. **`Projectile.ai[3]` NO EXISTE** — el array `ai` de tModLoader
     SOLO tiene 3 ranuras (0..2). El Coro Espectral y el Péndulo del
     Juicio guardaban el espejo del daño en `ai[3]` →
     `IndexOutOfRangeException` en OnSpawn Y en cada tick de AI (las dos
     armas directamente rotas en juego). FIX: el espejo vive en
     `localAI[2]` (ranura libre, con `Projectile.damage` —que SÍ
     viaja— como fallback).
  2. **`PyraPalettes.Sample` NO cortaba NaN** — `MathHelper.Clamp` deja
     pasar NaN (las comparaciones con NaN son falsas) y `(int)NaN` =
     `int.MinValue` en x64 → índice negativo → excepción. FIX: guard
     `float.IsFinite` + tablas de 1 color + `Math.Clamp` final. A prueba
     de balas para TODA la librería PyraLib.
  3. **`CicloEstelarRenderer.DrawConveccion` dibujaba con el lote
     CERRADO** — venía de `DrawSunBody` (cierra tras FlushAdditive) y
     llamaba a `PyraLib.Tongue` que espera el lote ABIERTO del llamador
     → "Draw was called, but Begin has not yet been called" +
     "End was called..." (las 2 excepciones del log de las 18:08). FIX:
     `BeginAdditive()` + `End()` envolviendo las lenguas (el mismo
     contrato del resto del renderer).
  4. **`BrumaBrushes.Unload` disponía texturas en hilo del POOL** —
     ModContent.UnloadModContent corre fuera del hilo principal y FNA
     exige Texture.Dispose en el hilo principal → ThreadStateException
     en cada recarga. FIX: las referencias se cortan YA y la disposición
     real se encola con `Main.QueueMainThreadAction` (el canal oficial).
  5. (El mismo fix del punto 1 cubría las 4 trazas del log: OnSpawn +
     BaseDamage × 2 armas.)
  6. El warn "Image loading failed: unknown image type" del empaquetado
     existe desde v6.21 y NO es de nuestros PNGs (auditoría PIL: los 200+
     PNGs del mod son RGBA estándar, esquinas transparentes verificadas)
     — documentado, sin acción.

### B. LA BOLSA DEL ARSENAL PRIMORDIAL — el kit en UNA ranura
  Petición literal: todo lo que se le da al jugador, DENTRO de una bolsa;
  al jugador SOLO la bolsa. `ArsenalBag` (ítem permanente, rango rojo,
  PNG 30×30 procedural): `TestingPlayer.OnEnterWorld` pasó de 40+
  `EnsureItem` individuales a **UNA sola entrega garantizada** — la bolsa.
  Clic derecho la abre: despliega el arsenal COMPLETO (kit base + las 47
  armas + cosméticos) con semántica de garantía (solo lo que falte —
  reabrirla repone armas perdidas sin duplicar). Apertura con 30 chispas
  oro/violeta + texto de casa. `ArsenalBag.Contenido()` es ahora EL
  punto único de la verdad del arsenal (dar de alta un arma nueva = 1
  línea ahí). El inventario del jugador deja de inundarse en cada
  entrada al mundo.

### C. EL OCASO DE AETHON — el patrón gauge del Cosmic Destroyer, aplicado
  El arma suprema de la investigación v6.26 (INFORME_EXOELECTRIC_NAMELESS,
  lección 10): LA TRINIDAD carga → burst → lockout, con los NÚMEROS del
  informe (gauge 100, +3 por impacto, 480 ticks de modo, ×3 de daño,
  execute <50% HP, lockout de castigo):
  · **CARGA**: el bastón dispara Fragmentos del Ocaso (150, búsqueda
    suave, estela ribbon + cruz de destello). CADA IMPACTO llena el aro.
  · **BURST**: aro lleno → CLIC DERECHO (AltFunctionUse) igniciona EL
    OCASO: 8 s donde cada disparo es una MUERTE DE ESTRELLA — mini-eclipse
    de 90 px con anillos rúnicos del EMISOR COMPARTIDO (tier 3, el mismo
    trazo de los soles), corona SolarFire (PyraPalettes), arcos de
    StormLib, anillo de OndaLib y cadena violeta a 2 vecinos (50%) —
    daño ×3 (450) con EJECUCIÓN (+50%) bajo el 50% de vida del objetivo
    (`ModifyHitNPC` + `FinalDamage` — sin dados). Al morir abre un
    DESGARRO en la realidad (RiftLib.Tear vía OcasoBurstFX, 40 ticks).
  · **LOCKOUT**: 2 s de Sobrecalentada — el bastón no dispara, humea.
  · **LA UI DEL GAUGE, 100% CÓDIGO** (`OcasoSystem.PostDrawInterface`,
    lote de interfaz TAL CUAL — el patrón aprobado de OndaSystem): aro
    de 20 segmentos tipo aureola sobre la cabeza; violeta→dorado al
    cargar; LISTO = pulso + 3 puntas orbitando + rótulo; ACTIVO = drenaje
    oro→rojo con chispas StormLib; LOCKOUT = rojo apagándose + vapor.
    Fade-out −0.05/tick (la lección de UI del TSA).
  · 2 buffs indicadores con PNGs 32×32 procedurales (OcasoActivo /
    Sobrecalentado, con cuenta atrás). `OcasoPlayer` es la máquina de
    estados; cadencia normal 14 ticks, ocaso 20. Sin maná (el costo ES
    el gauge).
  · 6 PNGs nuevos (2 iconos + 2 buffs + 2 sombras 76×76 de proyectil)
    por `tools/gen_v627_assets.py`.

### D. DOCUMENTACIÓN — los .md al día con la investigación
  · CARACTERISTICAS.md: nueva sección **LIBRERÍAS VFX** (el inventario
    completo de las 12 librerías + los 2 sistemas de soporte) + arsenal
    y sistemas al día (bolsa, gauge, contadores reales).
  · README.md: funciones/cómo-jugar actualizados (la bolsa, el arsenal
    de 47, el Ocaso).
  · Este CHANGES.md v6.27.
  · worklog.md (sandbox): Task 45 completo.
  · La opinión de biomas + el backlog priorizado de la investigación
    top-100 quedan en research/estrategia_v626/ (INFORME_BIOMAS.md con
    5 conceptos con plan de fases; INFORME_TOP100.md con 15 lecciones y
    tablas de ideas priorizadas A/B/C).

### E. ESTADO TÉCNICO
  · Compilación contra tModLoader 2026.07.3.0 real: **0 errores,
    0 warnings** (tras los fixes + el arma nueva + la bolsa).
  · build.txt → 6.27. hjson es/EN con las 8 claves nuevas (bolsa,
    arma, 2 proyectiles, 2 buffs). Sin referencias externas (auditoría
    v6.26 se mantiene: modReferences vacío).

## Commit v6.26 — EL SOL DE LOS 20 ANILLOS + RIFTLIB + 14 ARMAS NUEVAS + LAS CORONAS DE VERDAD

**Petición del usuario**: "el baston del eclipse primordial cámbialo,
esta nueva versión sera el sol de 20 anillos, y la mescla de todos los
agujeros negros rúnicos · oculta los soles de 6 a 19, y el sol 20 dale
una mejora mayor · crea nueva variantes de soles basado en estrellas
reales (estrellas de neutrones, pulsares, enanas blancas, estrellas
muertas...) · crea un baston que simule el ciclo de vida completo de una
estrella que se convierte en super nova · el baston sinfonia primordial
hace un destello en toda la pantalla, pero esto deberia estar
concentrado en el proyectil · luego crea mas bastones con nuevos tipos
de proyectiles creativos, al menos 5 · un baston que su proyectil sea un
desgarro en la realidad (investiga mods populares) · el anillo runico
estelar y corona de anillos runicos... tienen que ser creados por
codigos y tienen que copiar los anillos de los soles, pero no lo hacen ·
investiga el ExoElectric Disentegrator y el Nameless Destroyer · ideas
para un bioma nuevo + investigación de biomas · investiga los 100 mods
mas populares · limpia las referencias externas de las librerías".

### A. EL EMISOR COMPARTIDO DE LOS ANILLOS — LAS CORONAS DE VERDAD
  El problema de fondo de las coronas: eran COPIAS A MANO (constantes
  trasladadas) que se "parecían" a los anillos de los soles. v6.26 crea
  el EMISOR COMPARTIDO: `RuneSunRenderer.EmitRingSystem(center, R, time,
  seed, tier, rg, lifeT, alpha)` emite el sistema rúnico EXACTO al
  buffer de VFXCore (coords de mundo). Los soles del mundo, la CORONA DE
  ANILLOS RÚNICOS (aureola = Sol I literal sobre la cabeza) y el ANILLO
  RÚNICO ESTELAR (alas = Sol III literal en la espalda, 3 anillos con
  giros alternos) dibujan con EL MISMO CÓDIGO — si el sol cambia, las
  coronas cambian con él. GetGlyphPosition también usa la matemática
  compartida (RingGlyphWorld). DrawOrbitalSystem re-traduce a pantalla.

### B. EL ECLIPSE PRIMORDIAL — AHORA ES EL SOL DE LOS 20 ANILLOS
  El bastón cambia de dueño: ya no es un agujero negro con anillos — ES
  EL SOL DE LOS 20 ANILLOS (RuneSunRenderer tier 20, sistema ×1.30) con
  LA MEZCLA DE TODOS LOS AGUJEROS NEGROS RÚNICOS orbitando por fuera:
  los 3 círculos del Supremo (blanco/dorado/violeta contrarrotantes
  entrelazados con el anillo 20), el anillo de bandas del Cósmico, los
  brazos espirales del Olvido ALIMENTANDO al sol, el halo de bruma y las
  volutas de acreción de la Bruma, el anillo de fotones del Umbral, el
  gradiente aurora completo, la corona de descarga de StormLib, la luz
  prismática de LumenLib y los jets polares dobles. Muere en LA NOVA DEL
  ECLIPSE (anillo de Einstein + nova ×1.6 + 766 px — todo concentrado en
  el proyectil, sin flash de pantalla). Daño 700.

### C. LA FAMILIA DE LOS SOLES — OCULTAS Y MEJORADAS
  · Soles 6..19 OCULTOS (no se entregan ni garantizan; el código sigue).
  · EL SOL 20 RECIBE LA MEJORA MAYOR: daño 536 → 820, sistema ×1.30,
    vida 15,3 s (+90 ticks), aura ardiente al 55%, LA NOVA DEL SELLADO
    ×1.6 con radio récord 766 px y sacudida 12.

### D. LA SINFONÍA PRIMORDIAL — EL DESTELLO CONCENTRADO
  Fuera el flash de pantalla completa (OndaLib.Flash): el destello vive
  AHORA en el proyectil — ImpactFlash local de 110 px con cruz larga,
  Flare ampliado y 10 ticks de caída. La sacudida de cámara se queda.

### E. RIFTLIB + EL BASTÓN DEL DESGARRO EN LA REALIDAD (nueva librería)
  RiftLib (~1000 L, según el contrato del informe de investigación):
  Tear (grieta lineal con labios ×3 capas velo/cuerpo/núcleo +
  aberración cromática R/B ±2 px + vacío OCLUSIVO), Interior con
  Estrellas (16-28 con scroll y paralaje), Grieta persistente (fractal
  Lichtenberg que respira), Shards, ChispasAnomalia, EcoGlitch y
  Oscurecer (el mundo se oscurece herido, máx 0.35, RiftMundoSystem).
  EL BASTÓN DEL DESGARRO EN LA REALIDAD (250): el proyectil ES el
  desgarro — telégrafo → apertura → sostenido → cierre; la LÍNEA de
  daño atraviesa PAREDES (Collision.CheckAABBvLineCollision) con DoT;
  al cerrarse deja una GRIETA PERSISTENTE que drena ~8 s.

### F. LAS SEIS ESTRELLAS REALES (astrofísica estilizada de la casa)
  · ESTRELLA DE NEUTRONES (240): núcleo de 12 px ultradenso, aura 60 px
    con daño TRIPLE, starquakes periódicos (Kick + onda + aberración).
  · PÚLSAR (280): el FARO — dos haces polares barriendo a 1 rev/s; cada
    barrido que toca un enemigo golpea ×1.5 (cooldown 30 t por NPC).
  · ENANA BLANCA (180): rescoldo cristalino de 7 facetas, anillo de
    acreción que ROBA brillo a los enemigos.
  · ESTRELLA MUERTA (200): la enana negra — masa oscura que devora luz,
    entropía 8/s, brasas frías PyraLib.ColdFire, runas a 0,2 Hz.
  · SUPERGIGANTE ROJA (260): coloso de 90 px con celdas de convección;
    al morir se COLAPSA (implosión 20 t) y revienta en NOVA ×1.8.
  · MAGNETAR (320): campo violeta con líneas RETORCIDAS (r=L·sen²θ con
    torsión), cadenas de rayo automáticas cada 20 t, ARRITMIA
    determinista (seno de seno).

### G. EL BASTÓN DEL CICLO ESTELAR — UNA VIDA COMPLETA EN 18 s
  Máquina de 5 actos: NEBULOSA (contracción + Wisps, aura fría) →
  SECUENCIA PRINCIPAL (ignición con flash + cuerpo SunShader + 3 anillos
  tenues) → GIGANTE ROJA (hincha ×2.2, 9 celdas de convección, nebulosa
  planetaria desprendiéndose) → COLAPSO + SUPERNOVA (implosión ×0.3 y
  nova ×2 en 650 px con TODAS las librerías) → EL REMANENTE (estrella de
  neutrones enana pulsando que se apaga). La ESTELA cuenta la historia
  en 5 colores.

### H. LOS CINCO BASTONES CREATIVOS
  · RELOJ DE ARENA CÓSMICO: el reloj VIVO — motas de luz cayendo por el
    cuello, el montículo creciendo, y CUANDO LA CÁMARA SE VACÍA EL
    TIEMPO SE INVIERTA (gira 180°): 720 ticks, varios ciclos.
  · MAREA GRAVITARIA: ola de luz que CABALGA el terreno (sube y baja
    colinas), arrastra enemigos, y al golpear pared se rompe en 3 olas
    menores (MareaChicaProjectile).
  · ENJAMBRE PRISMÁTICO: 12 avispas de luz con boids simple (cohesión/
    separación/migración de presa), cada una con su color del prisma y
    picaduras con cooldown.
  · PÉNDULO DEL JUICIO: péndulo con física REAL (θ'' = -g/L·sen θ), el
    arco SE AMPLÍA con cada vaivén (+8% hasta ±150°) y a los 8 s el hilo
    SE CORTA: la maza vuela balística y estalla.
  · CORO ESPECTRAL: 6 notas de luz cantando en órbita; cada una emite su
    anillo de onda (OndaLib.Pulse) a su turno con tono de campana; el
    coro se despide apagándose de a una.

### I. LA INVESTIGACIÓN (4 informes en research/estrategia_v626/)
  · INFORME_EXOELECTRIC_NAMELESS (42-a): el Exo Disintegrator (IER, con
    el patrón Mars/WoTG: rayo de 5600 px con telegraph 40 f + carga
    150 f, núcleo oscuro + bordes brillantes) y el Nameless/Cosmic
    Destroyer (The Stars Above: gauge carga→burst→lockout ×3). 14
    lecciones aplicadas al diseño del arsenal.
  · INFORME_DESGARRO_REALIDAD (42-b): técnicas de desgarro de 16 fuentes
    + el CONTRATO de RiftLib (implementado en E).
  · INFORME_TOP100 (42-c): los ~150 mods más populares del Workshop con
    suscriptores reales, análisis por categoría y 15 lecciones
    estratégicas (nicho VFX-first, GIF de arma como canal #1...).
  · INFORME_BIOMAS (42-d): estado del Sagrario Hueco (stub), checklist
    técnico completo de biomas tML y 5 conceptos diseñados para
    AethonMod (Sagrario realizado, Campo Estelar, Veta Rúnica, Cenizas
    del Eclipse, Falla del Vacío — pendientes de implementar).

### J. LIMPIEZA DE REFERENCIAS EXTERNAS
  Auditoría completa: CERO dependencias de código (modReferences vacío,
  sin TryGetMod ni imports de terceros) y las menciones a otros mods en
  COMENTARIOS neutralizadas — las librerías son 100% propias y las
  técnicas están parafraseadas (nada de código GPL).

### K. ENTREGA
  · EnsureItem: +14 armas nuevas (Desgarro, 6 estrellas reales, Ciclo
    Estelar, 5 creativos) y soles 6-19 fuera del kit.
  · Localización es-ES/en-US completa de las 14 armas y sus proyectiles.
  · 28 PNGs nuevos (14 iconos 30×30 + 14 sombras 76×76, PIL+numpy
    determinista) + scripts reproducibles en tools/.
  · Compilación contra tML real: 0 errores / 0 warnings.

## Commit v6.25 — EL HUMO DE VERDAD: EL FIX PREMULTIPLICADO + LA INVESTIGACIÓN DE 23 FUENTES + TRES LIBRERÍAS NUEVAS

**Petición del usuario**: "¿qué es este error? se ve mal, se supone que es
humo o bruma... acaso es un sprite, deberías hacer humo o bruma mayormente
por código y de ser sprite estos deben ser transparentes con fondo
invisible, ¿acaso la librería está mal o los sprites? de ser así,
investiga a fondo y con profundidad todos los mods populares, al menos 20
mods y crea librerías que creas que nos falten y mejora la librería para
humo, bruma, niebla · si te fijas todo lo que lleva esto de humo o bruma
tiene el mismo error · la corona rúnica estelar del commit anterior estaba
bien no la cambies, la que tenías que cambiar era corona de anillos
rúnicos, regresa la corona rúnica estelar a como estaba en el commit
anterior".

### A. EL BUG DE LOS RECTÁNGULOS — LA CAUSA RAÍZ (y NO eran los PNGs)
  Los PNGs del mod (SoftGlow/Ring/BlackDisk) están BIEN: fondos
  transparentes, verificados píxel a píxel. El error estaba en las
  TEXTURAS HORNEADAS EN RUNTIME de la librería de bruma:
  · BrumaBrushes escribía `new Color(255, 255, 255, alfa)` — RGB LLENO en
    TODOS los píxeles (incluso donde alfa ≈ 0), SIN premultiplicar. En el
    pipeline FNA/tModLoader (que espera alfa premultiplicada): en lotes
    ADITIVOS (Blend One/One) el canal alfa se IGNORA por completo → TODO
    el quad se pintaba como RECTÁNGULO SÓLIDO de color; en lotes alfa los
    bordes quedaban duros (el RGB no muere con el alfa). ESO eran las
    "sábanas rectangulares" de las capturas — TODO lo que usaba BrumaFX
    tenía el mismo error porque TODOS comparten los mismos pinceles.
  · EL FIX: horneado PREMULTIPLICADO (`RGB = blanco × alfa`) + el Tinte
    de BrumaFX ahora premultiplicado (RGB × f además del alfa × f) — la
    INTENSIDAD manda también en lotes aditivos y los bordes son suaves en
    CUALQUIER lote. El humo de TODO el mod se ve suave de verdad.

### B. LA INVESTIGACIÓN — 23 FUENTES (informes en research/humo_v625/)
  · INFORME_MODS_HUMO.md (Task 41-a): vanilla 1.4.4.9 decompilada (Dust
    completo), Everglow/Coralite/LunarVeil/WoTE/MEAC locales + 7 repos
    públicos clonados (Calamity, StarlightRiver, Spirit, SOTS, Fargo's,
    Overhaul, ParticleLibrary) + web (Thorium, Redemption, AA, Orchid,
    Avalon...). 24 lecciones con números y 8 anti-patrones.
  · ANALISIS_HUECOS.md (Task 41-b): gap analysis de 17 capacidades contra
    26 fuentes — qué tienen los mods premium que nos faltaba.

### C. BRUMAFX v2 — LA SEGUNDA GENERACIÓN DE LA LIBRERÍA DE HUMO
  · FLIPBOOK de ruido evolucionado: cada pincel es una TIRA VERTICAL de
    4-6 frames del MISMO campo fBm (dominio desplazándose +0.3 celdas y
    contraste creciendo por frame) — el humo SE DESGARRA, no solo rota
    (anti-patrón nº3 de la investigación). Puff cicla en ping-pong lento;
    AnimatedPuff avanza POR VIDA (Calamity).
  · ESCALERA de texturas 64/128/160 px por radio (lección Calamity).
  · VAPOR: el pincel de LUT DURA (núcleo denso, caída 255→0 al 74% —
    Everglow) + luz del mundo por defecto + muerte rápida.
  · LUZ DEL MUNDO con piso: WorldTint(pos) — factor 0.25..0.85 por canal
    (en pleno día sin cambio; en cueva tenue pero visible; bajo antorcha
    el humo se TINTA cálido). Activada en la Tormenta Nebular.
  · VIENTO del mundo (Main.windSpeedCurrent) en Column/Tendril/MistBand/
    Vapor — humo de un mundo con clima.
  · SMEAR vanilla (dusts 130-134): hasta 6 copias del NÚCLEO cayendo
    atrás por la velocidad.
  · AnimatedPuff: envolvente nacimiento-rápido/muerte-lenta + crecimiento
    ×1.5 + RAMPA DE ENFRIAMIENTO (color→gris — la historia térmica) +
    brasa que EMITE luz mientras arde.
  · PRESUPUESTO de 500 quads/frame con LOD automático (menos sub-blobs).
  · Wisps: voluta + motas de brasa (lección nº24). API VIEJA 100%
    compatible (Puff/Cloud/Tendril/Column/MistBand sin cambios de firma).

### D. TRES LIBRERÍAS NUEVAS (los huecos del análisis 41-b)
  · ESTELALIB (Content/VFX/EstelaLib.cs): los RIBBONS de grosor variable
    que TODOS los mods premium tienen — Sanitize→Smooth→Resample del
    camino (oldPos sucios, teleports, escalera de ticks) + Ribbon de
    triple capa (velo/cuerpo/núcleo) con perfiles Head/Center/Comet/Alive
    + fantasmas con squash (MEAC) + EstelaTrack (ring-buffer por
    identidad que se AUTO-PODRE a los 2 ticks).
  · ONDALIB (OndaLib.cs + OndaSystem.cs): las ONDAS EXPANSIVAS de
    impacto — frente ROTO en 12-16 segmentos con radio vivo + doble anillo
    (frente + retaguardia ×0.5) + grosor que engorda mientras muere +
    expansión fast-out + aberración cromática ±1.8% + EL PAQUETE DE
    IMPACTO: Kick (sacudida de cámara centralizada en el hook oficial
    ModifyScreenPosition, cap 14px, máx 2 impulsos) y Flash (velo radial
    en PostDrawInterface, máx 1 activo + cooldown).
  · PYRALIB (PyraLib.cs): EL FUEGO — recuperada la técnica Doom Fire
    perdida en la purga de v6.24. PyraPalettes (3 tablas de 37 niveles:
    SolarFire/ColdFire/VoidFire con los colores de la casa) + Tongue (la
    lengua: parpadeo inconmensurable 7.1/17.3, punta vaga con reversión,
    erosión de ruido, triple capa, ROTABLE) + Flame (racimo) + EmberField
    (campo de brasas con propagación determinista a 30 Hz, cap 600 celdas)
    + Sparks (ascuas físicas → ParticleManager) + Light.

### E. EL ECLIPSE — EL HUMO DEL VACÍO DE VERDAD
  Con el fix premultiplicado la bruma interior ya es SUAVE; y ahora con
  VOLUMEN COMPLETO: 8 volátiles orbitando (eran 6, más grandes) + las dos
  espirales con BRASAS (Wisps) + 4 JETS DE VAPOR del limbo (LUT dura,
  naciendo en el borde y CAYENDO al centro con aceleración gravitacional
  — la masa del vacío se come el gas). El centro del eclipse respira.

### F. LAS CORONAS — LA CORRECCIÓN DEL DESTINATARIO
  · LA CORONA RÚNICA ESTELAR (RuneCrownItem): REVERTIDA a v6.23 exacta —
    el arco de ocho glifos fucsia con perlas y chispas rosas que estaba
    BIEN (renderer + capa + ítem + icono + tooltips + chispas y luz del
    CosmeticPlayer, todo al estado del commit anterior).
  · LA CORONA DE ANILLOS RÚNICOS (RuneRingCrownItem): la que DEBÍA subir
    a la cabeza — ahora ES la aureola: EL ANILLO DEL SOL RÚNICO I LITERAL
    ringiendo la CABEZA (plano 1.62R×0.34, inclinación −0.55, giro CW
    0.26, radio base de cabeza 12px) con sus 6 glifos y perlas al brillo
    EXACTO de los soles; la capa ancla al CENTRO DE LA CABEZA.

### G. LAS LIBRERÍAS CABLEADAS EN LAS ARMAS (v6.24 → vivas)
  · LA SINFONÍA PRIMORDIAL: la estela de vuelo ahora es un RIBBON de
    EstelaLib (perfil Comet, prismático) sobre el camino REAL del track +
    la detonación lleva LA ONDA EXPANSIVA de OndaLib (frente roto con
    aberración cromática) + el paquete de impacto centralizado (Kick 9px +
    Flash del velo radial — sustituye al PunchCameraModifier ad-hoc).
  · LA LANZA DEL ALBA: la punta ARDE — PyraLib.Tongue con la tabla
    SolarFire (la llama ancla en la punta y crece contra la marcha) + el
    PULSO de OndaLib en cada golpe + las ASCUAS de PyraLib.Sparks
    (ParticleManager) + Kick suave de 3px.
  · LA TORMENTA NEBULAR: la nube física VIVE por la luz del mundo.

### H. CICLO DE VIDA
  BrumaSystem descarga ahora también los campos de brasas de PyraLib y
  los tracks de EstelaLib — cero fugas entre recargas.

— Compilación contra tModLoader 2026.07.3.0 real: 0 errores, 0 warnings.

## Commit v6.24 — EL SOL DEL ECLIPSE + EL HUMO DEL VACÍO + LA AUREOLA DEL SOL I + LOS CÍRCULOS DE LOS AGUJEROS + LAS TRES ARMAS DE LAS LIBRERÍAS

**Petición del usuario**: "al agujero negro eclipse no se le ve el sol ·
ahora crea un arma que use todas nuestras librerías, sé creativo con eso ·
el accesorio de la corona rúnica debe estar en la cabeza del jugador como
una aureola, además brilla mucho y no se parece en nada al aro que usa el
sol 1, tiene que ser una aureola igual al anillo del sol rúnico 1 · el
otro ítem cosmético anillo rúnico estelar tiene los mismos problemas,
además estos aros no deben estar en esa forma, la forma correcta es la
misma forma que la de los agujeros negros · el agujero negro eclipse no
debe ser completamente oscuro en el centro, debe tener alguna animación o
mejor, que tenga mucho humo o bruma · no olvides crear varias armas nuevas
que usen todas nuestras librerías · borra la envoltura de fuego y rayo".

### A. EL ECLIPSE — EL SOL SE VE (la corona solar del eclipse)
  v6.23 dejó "el sol asomando" como DOS AROS FINOS (alpha 0.20/0.11) — el
  usuario: "NO SE LE VE EL SOL". Ahora es UN ECLIPSE TOTAL DE VERDAD:
  · LA CORONA SOLAR (DrawSolarCorona, pintada ANTES del repintado negro):
    halo caliente 3.4r (alpha 0.44) + núcleo interno 2.5r + EL BLOOM del
    sol (LumenLib, 2 capas) + 9 STREAMERS de luz radiando del limbo
    (LumenLib.Ray con grosor animado y pulso por streamer). El repintado
    negro se come el centro y el SOL queda como el resplandor anular
    cegador alrededor de la luna negra.
  · EL LIMBO: el filo blanco-cálido al borde del disco SUBE a alpha 0.55
    (antes 0.20) + el jade dorado a 0.16.
  · EL ANILLO DE DIAMANTE: LumenLib.Flare de 4 puntas latiendo SOBRE la
    luna negra (sutil: 0.14..0.24 — el centro sigue siendo la luna).

### B. EL ECLIPSE — EL HUMO DEL VACÍO (el centro ya no está muerto)
  "No debe ser completamente oscuro en el centro, que tenga mucho humo o
  bruma": LA BRUMA DE LA LIBRERÍA VIVE DENTRO DEL DISCO (pase ALFA =
  masa de verdad sobre el negro absoluto — DrawVoidSmoke):
  · LA MASA CENTRAL que respira (Puff 0.40r, alpha 0.32 ± latido).
  · SEIS VOLÁTILES orbitando CW/CCW alternos (radios 0.30..0.60r,
    violeta/púrpura/brasa, cada uno con su semilla y pulso).
  · DOS VÓLUTAS espiralando hacia el centro (Tendril sobre camino
    espiral 0.92r→0.18r).
  · LA BRASA VIOLETA: glow tenue bajo el humo (0.09..0.14) — el centro
    NUNCA es un punto muerto del todo.
  Validado en mock 1:1 (tools/mock_eclipse_v624.py): VLM 8/10 humo
  visible, 7/10 "centro negro-pero-vivo", sol reforzado tras la ronda.

### C. LA CORONA RÚNICA = LA AUREOLA DEL SOL I (RuneCrownRenderer)
  "Debe estar en la CABEZA como una AUREOLA, igual al anillo del sol
  rúnico 1": el arco fucsia de 8 glifos de v6.03 QUEDA BORRADO — la
  corona es ahora EL ANILLO DEL SOL RÚNICO I LITERAL ringiendo la cabeza
  (la MISMA geometría de RuneSunRenderer tier 1): el aro elíptico de
  cápsulas con profundidad (semiejes 1.62×R / 0.34×R, inclinación −0.55,
  giro CW 0.26 rad/s) con SUS 6 glifos solares cabalgando la órbita
  rotados a la tangente, perlas y latidos — LOS ALPHAS EXACTOS del sol
  (aro (0.30+0.30·depth)·pulse, resplandor 0.20, trazos 0.85, perlas
  0.60/0.90). Chispas doradas + luz cálida tenue en CosmeticPlayer. Icono
  regenerado (la aureola dorada inclinada). El brillo ES el del sol —
  nada del bloom fucsia cegador.

### D. EL ANILLO RÚNICO ESTELAR = LA FORMA DE LOS AGUJEROS (RunicHaloRenderer)
  "Estos aros no deben estar en esa forma, la forma correcta es LA MISMA
  FORMA QUE LA DE LOS AGUJEROS NEGROS": los 4 anillos elípticos del sol
  IV QUEDAN BORRADOS — el ítem de alas es ahora LOS CÍRCULOS RÚNICOS DE
  LOS AGUJEROS, LITERALES (la técnica de SupremoBlackHoleRenderer):
  · TRES círculos PLANOS (RingQuad — el aro fino): BLANCO íntimo 2.02R
    CW rápido (6), DORADO 2.62R CW lento (8), VIOLETA 3.30R CCW (6 — el
    contrarroto arcano).
  · Las runas flotando ALREDEDOR de cada círculo (radio respirando por
    glifo + mecido — la runa del agujero vive DE PIE, no tangencial),
    con la tabla de glifos S0..S7 del supremo y perlas.
  · LOS ALPHAS EXACTOS de los agujeros: aros 0.24/0.20/0.18, trazos
    0.85·pulse, perlas 0.62/0.90.
  · La energía de vuelo VIVE acotada (0.85..1.20, giro ×1..1.5, runas
    ardiendo al blanco). Icono regenerado (los 3 aros concéntricos).

### E. LAS TRES ARMAS DE LAS LIBRERÍAS (la petición doble: "un arma" + "varias armas")
  CADA ARMA USA TODAS LAS LIBRERÍAS DEL PROYECTO — StormLib (rayos) +
  BrumaFX (humo) + LumenLib (luz) + las runas — cada una con su
  personalidad de juego:
  · EL BASTÓN DE LA SINFONÍA PRIMORDIAL (120 dmg, SinfoniaPrimordial
    Staff/Projectile): LA CHISPA DE LA CREACIÓN — corazón prismático de
    drift (BloomPulse + Flare + Aurora + 5 Ray de sol) + estela de
    BrumaFX + arcos de corona StormLib + cadenas a enemigos al vuelo
    (55% dmg, Electrified) + 6 runas orbitando (forma de agujero). AL
    MORIR — LA SINFONÍA: daño en área 140px + ImpactFlash + Aurora
    completa de 15 bandas + SEIS MultiBolt radiales reventando + la
    NUBE de bruma de la detonación creciendo + las runas VOLANDO +
    doble onda de choque + cámara.
  · EL CETRO DE LA TORMENTA NEBULAR (80 dmg/golpe, TormentaNebular
    Staff/Projectile): LA TORMENTA PERSISTENTE (5 s sobre el cursor,
    560px) — nube viva de BrumaFX (2 Cloud + 2 Puffs, pase ALFA = masa
    que ocluye) + aurora LumenLib latiendo dentro + runas orbitando el
    borde (8 oro CW + 6 violeta CCW) + arcos ambientales + HASTA TRES
    DESCARGAS simultáneas con ciclo propio: telegraph
    (LumenLib.Telegraph 10 ticks) → MultiBolt de la panza de la nube →
    daño 85px + Electrified + trueno + ImpactFlash + onda.
  · LA LANZA DEL ALBA RÚNICA (90 dmg, LanzaAlba Staff/Projectile): el
    FILO del amanecer — la hoja de luz (LumenLib.Lance + LanceTrail de
    8 fantasmas) + el Rayo de sol que la precede + Flare en la punta +
    la runa estrella girando en el corazón + la VÓLUTA de bruma sobre
    su estela real (Tendril, ring buffer de 8 posiciones) + EndCap
    eléctrico. Perfora 4 enemigos; CADA GOLPE encadena ChainBolt a 2
    cercanos (60% dmg + Electrified).
  Entrega completa: EnsureItem ×3, recetas 5 madera, iconos 30×30
  procedurales + sombras 76×76 (patrón del estilo de la casa), hjson
  es/EN, tools/gen_v624_assets.py reproducible, hoja de contacto VLM
  verificada (8/8 legibles).

### F. LA PURGA — LA ENVOLTURA DE FUEGO Y LA DE RAYOS BORRADAS
  "Borra la envoltura de fuego y rayo": git rm de FireVeilItem/
  Player/DrawLayer/Renderer + StormVeilItem/Player/DrawLayer/Renderer +
  FlameBrush.png (solo la usaba el fuego) + entradas hjson es/EN +
  EnsureItem. Auditoría post-purga: 0 referencias .cs restantes.

### G. CALIDAD
  · Auditoría de assets: 70 clases ModProjectile/ModItem → 0 PNGs de
    sombra faltantes (la lección v6.14.1).
  · Compilación contra tModLoader 2026.07.3.0 REAL (/tmp/verify +
    /tmp/tml, dotnet 8.0.425): Build succeeded · 0 errores · 0 warnings.
  · Mock del eclipse (tools/mock_eclipse_v624.py) con VLM: humo 8/10,
    centro vivo 7/10, sol reforzado tras la ronda de feedback.

## Commit v6.23 — EL AJUSTE FINO: el abrazo del fuego + la ENVOLTURA DE RAYOS + los anillos de los soles + el eclipse de verdad + los círculos rúnicos completos

**Petición del usuario**: "el fuego es muy grande, debe estar limitado a solo
unos 3 o 4 píxeles por encima del personaje · de la misma forma que haces con
el fuego, crea un item cosmético que sea una envoltura de rayos, usa nuestras
librerías para darle el toque especial · que los anillos de vuelo sean los
anillos del sol número 4, además el anillo es muy brillante, reduce el brillo
a como se ve en los soles · que la corona de anillos rúnicos sea la del sol
número 1 · el bastón del eclipse primordial debe ser negro en su centro, el
sol debe verse ligeramente · los dos bastones de agujeros negros supremos
deben tener sus agujeros 3 anillos rúnicos (tienen dos) · todos los agujeros
avanzados deben tener 2 anillos rúnicos de tipo agujero (solo uno de los
bastones tiene 2, el resto solo 1)".

### A. EL FUEGO — EL ABRAZO JUSTO (fix de tamaño)
  El campo de propagación de intensidades pasa de 26×38 celdas (una COLUMNA
  de 167 px — 4× la altura del jugador) a **11×11 celdas** (48 px): la
  envoltura vive PEGADA A LA SILUETA — la base arde sobre los pies cubriendo
  el ancho del cuerpo y las puntas LAMEN la coronilla 3-4 px por encima,
  nada más. Re-calibrado completo: decay más rápido (muere al pasar la
  cabeza), viento ±1.2 celdas, inercia vertical ±5..7 px, pincel ×1.18/×1.30,
  1 pase extra al volar (antes 2), el humo nace justo sobre la coronilla
  y las chispas a lo largo del cuerpo. TODAS las interacciones vivas:
  viento en contra al correr, avivo, aplastado del salto, estirada en caída.

### B. LA ENVOLTURA DE RAYOS PRIMORDIAL (cosmético nuevo — StormLib)
  `StormVeilRenderer/Player/Item/DrawLayer` — la HERMANA ELÉCTRICA del fuego:
  la SILUETA del jugador (una elipse del tamaño exacto del cuerpo) es el
  CARRIL de una tormenta construida con STORMLIB (nuestra librería):
  · **LOS CHISPAZOS** — rayos de verdad entre dos anclas del contorno:
    ZigPath + REFINO FRACTAL multi-escala (la MISMA matemática del rayo del
    cielo aprobado), oro solar y azul-estelar alternando.
  · **LOS ARCOS** — descargas abrazando el contorno con jitter radial hash
    (el patrón ArcRing): media silueta arriba, media abajo.
  · **LOS PELOS** — filamentos caóticos finísimos hacia afuera (el "hair"
    de las descargas reales).
  · **EL RENDER** — cada segmento = TRES CAPAS por VFXCore (halo + cuerpo +
    núcleo blanco razor) con las texturas BoltHalo/BoltCore procedurales;
    gorros de descarga en los extremos.
  · **INTERACTIVA**: la velocidad acumula ENERGÍA (quieto = brisa eléctrica,
    corriendo = tormenta encendida con estela a contra de la marcha); al
    saltar los arcos caen a los pies, al caer suben a la cabeza. Chispas
    DustID.Electric + luz fría-oro con stutter de descarga.
  Icono 30×30 procedural (tools/gen_storm_veil_icon_v623.py), localización
  es/EN, EnsureItem al entrar al mundo.

### C. LOS ANILLOS DE VUELO = LOS ANILLOS DEL SOL IV (brillo de soles)
  `RunicHaloRenderer` REESCRITO: el halo de la espalda ya NO es un anillo
  gigante con bloom cegador — es **EL SISTEMA ORBITAL DEL SOL RÚNICO IV,
  LITERAL**: los CUATRO anillos del sol nº4 (semiejes 1.62+0.44k ×R, achatado
  0.34..0.48, inclinaciones −0.55..+0.05, giro alterno CW/CCW, 6/8/10/12
  glifos con perlas y latidos) orbitando la espalda, a los **ALPHAS EXACTOS
  del sol** (aro (0.30+0.30·depth)·pulse, glifos 0.85·pulse, perlas
  0.60/0.90). El corazón pasa de bloom ×2.1 a un latido discreto de 22 px.
  La ENERGÍA DE VUELO sigue viva pero ACOTADA: multiplicador 0.85..1.20,
  giro ×1..1.5, runas ardiendo al blanco al volar. La luz del mundo y las
  chispas también se contienen al nivel solar.

### D. LA CORONA = EL ANILLO DEL SOL I (brillo de soles)
  `RuneRingCrownRenderer` REESCRITO: de tres aros propios con corazón
  brillante a **EL ANILLO DEL SOL RÚNICO I, LITERAL** — un solo aro
  (1.62R × 0.34, inclinación −0.55, CW 0.26 rad/s) con sus 6 glifos,
  perlas y latidos al BRILLO EXACTO del sol. Icono regenerado (un anillo
  inclinado con 6 perlas). CosmeticPlayer actualizado (chispas doradas +
  luz cálida única).

### E. EL ECLIPSE PRIMORDIAL — EL SOL ASOMA
  El corazón ya NO se lava de dorado: el AURA FINAL (SoftGlow 6.6×rr) se
  pinta AHORA **ANTES** del repintado negro — el vacío la DEVORA en el
  centro y queda como halo alrededor del disco. Y nace **LA CORONA DEL
  ECLIPSE**: un aro fino de luz blanco-cálida JUSTO al borde del disco
  negro (1.055R, 0.20±0.07) + un jade dorado más afuera (1.13R, 0.11±0.04)
  — el sol vivo asomando tras la luna negra: un eclipse REAL.

### F. LOS CÍRCULOS RÚNICOS DE LOS AGUJEROS NEGROS
  · **Los DOS SUPREMOS → 3 anillos**: el Supremo añade el CÍRCULO BLANCO
    íntimo (6 runas @2.02R, CW 0.16 — entre el anillo de fotones y el
    dorado); el Supremo Aurora añade el CÍRCULO MORADO íntimo (6 runas
    @2.02R — el color que faltaba del gradiente negro→morado→azul→dorado).
    REFACTOR: DrawRune ya no hardcodea el viejo if de dos círculos —
    count/orbit son parámetros (el bug que habría roto el tercer anillo).
  · **Los ASCENDIDOS → 2 anillos CLAROS cada uno**: el Cósmico ya los
    tenía (intacto); el Umbral SUBE su anillo íntimo (radio 1.66R→1.95R,
    glifos ×0.62→×0.80 — ahora se LEE); la Bruma Ascendida recibe SU
    SISTEMA RÚNICO desde cero (8 teal CW @2.55R + 6 hielo-blanca CCW
    @3.15R, paleta RimTeal/FrostMote/PhotonWhite); el Olvido añade el
    círculo VIOLETA contrarrotante (6 runas @3.20R, −0.12 rad/s).

### G. ENTREGA
  Compilación **0 errores / 0 warnings** contra tModLoader v2026.07.3.0
  real. build.txt 6.23. Localización es/EN de la Envoltura de Rayos.
  EnsureItem de la Envoltura al entrar al mundo.

## Commit v6.22 — LA LUZ Y EL FUEGO: LumenLib + el Eclipse Primordial + los soles 11-20 + 3 cosméticos interactivos

**Petición del usuario**: "crea un item cosmético que envuelva al personaje
con fuego creado por código, el fuego debe interactuar con las acciones del
personaje cuando se mueva · el bastón de rayos está perfecto, ahora añade
ese rayo a todo lo que usaba la librería de rayos anterior, mantén la
concordancia, la consistencia y los tamaños correctos · crea 10 bastones
más de sol con anillos rúnicos, del 11 hasta el 20 · crea un bastón nuevo
que fusione el sol de 20 anillos rúnicos más todos los agujeros negros,
dale efectos de luz, bruma, humo, rayos y otros efectos · crea un cosmético
que sea una corona de anillos rúnicos que rodee al jugador · crea un
cosmético de un anillo rúnico en la espalda que funcione como alas y halo;
cuando el jugador vaya a volar este anillo brilla con intensidad ·
pregunta: StormLib ¿sirve para haces de luz y otros efectos o solo rayos?
en cuyo caso crea más librerías con el conocimiento de los mods estudiados
· investiga super profundo Wrath of the Empress y MEAC (empress of light)
y crea una librería para manejar la luz como ellos".

### A. LA INVESTIGACIÓN DE LUZ SUPERPROFUNDA (3 informes nuevos)
  · **WoTE** (`research/luz_v622/INFORME_WOTE_LUZ.md`) — 31 archivos
    leídos + 12 shaders HLSL .fx incluidos en el repo: el BLOOM APILADO
    INVERTIDO (textura radial 200×200 en 2-4 capas: escalas 4.1/2.85/1.5/0.8
    con alfas 0.25/0.67/0.7/1.0), paletas cíclicas MulticolorLerp con wrap,
    hue drift 0.2-0.6/s + semilla por identidad, estela sinusoidal
    perpendicular, LightLance con telegraph de 2100px + 30 fantasmas,
    deathray con pulso 8.6 Hz. Casi CERO Lighting.AddLight: todo es render
    emisivo.
  · **La Emperatriz VANILLA extraída del binario real** (INFORME_EOL_
    VANILLA.md — tModLoader.dll decompilado): el color = hslToRgb(hue%1,
    S=1, L por capa: 0.5 cuerpo/0.85 luz/1.0 núcleos), la DOBLE PASADA
    universal (color A÷2 ×1.1-1.4 SOBRE blanca A÷2 ×1.0), afterimages que
    CRECEN hacia atrás (×1.4, 39-79 fantasmas), la telegrafía de lanza de
    3600px, SunDance = sprite estirado 4 capas con grosor animado
    0.25→0.7 + LUZ MUESTREADA cada 800/12 px, el aurora de muerte de 15
    bandas espejadas π·i, el enrage dorado (255,231,69).
  · **MEAC (el rework chino de la Emperatriz)** (INFORME_MEAC_LUZ.md —
    .tmod descargado por 10 rangos paralelos + parser propio del formato
    + ILSpy + VLM): el LUT arcoíris 1×256 (HSL S=1 L=0.5 — valida nuestra
    matemática EXACTA), hue en ai[0] EN GRADOS con voleas desfasadas,
    lanza = sprite + triángulo de 2500px + 8 afterimages con SQUASH Y,
    doble dibujado con A=0, fuego = LUT de 40 niveles, tiras 1×N de
    perfil DURO tintadas en runtime, warp de pantalla con RT.
  · CERO código copiado de ninguno: las TÉCNICAS re-implementadas 100%.

### B. LUMENLIB — LA LIBRERÍA DE LA LUZ (la respuesta a la pregunta del usuario)
  StormLib ERA solo de rayos (filamentos eléctricos). Ahora el proyecto
  tiene el TRÍO completo: **StormLib** (rayos) + **BrumaFX** (humo/niebla)
  + **LumenLib** (la luz que EMANA). `Content/VFX/LumenLib.cs`:
  · **Motor de color**: Hue (HSL propio con las L firmadas por capa),
    Drift (el hue que camina 0.2-0.6/s con semilla por identidad), Cycle
    (paletas cíclicas con wrap) + LumenPalettes (PrismRose/PrismDay/
    SolarGold/VoidCold/EclipseFire).
  · **Bloom** — el apilado invertido de 2-4 capas con los NÚMEROS medidos
    del ecosistema (4.1/2.85/1.5/0.8 · 0.25/0.67/0.7/1.0) + BloomPulse.
  · **DoublePass** — la doble pasada universal (color ×1.15 A÷2 sobre
    blanca A÷2).
  · **Flare** — el destello de 4 puntas (cruz + diagonal ×0.62 + punto
    caliente).
  · **Ray** — el rayo de sol: bloom ESTIRADO en 3 capas (velo ×1.6 /
    cuerpo / núcleo ×0.3) con GROSOR ANIMADO y la boca cegadora.
  · **Lance + LanceTrail + Telegraph** — la lanza de luz con hoja
    (LumenBlade), N fantasmas que crecen ×1.4 con alpha (1-i/N)^1.6 y la
    línea de aviso con anillo objetivo.
  · **Aurora** — las 15 bandas espejadas π·i con dos vueltas de hue y dos
    frecuencias.
  · **LightAlong** — la luz del mundo MUESTREADA cada N px (la lección
    del muestreo de la Emperatriz).
  · **4 texturas procedurales nuevas**: LumenBloom (200×200), LumenBlade
    (64×256 con taper y vetas), LumenFlare (128×128) y FlameBrush (24×36).

### C. LA MIGRACIÓN COMPLETA: STORMLIB EN TODAS PARTES
  TODO lo que usaba la librería vieja de rayos ahora usa la 2ª generación
  (misma concordancia, mismos tamaños): los 4 Ascendidos + el Supremo +
  el Supremo Aurora + el sol rúnico (prominencias, rayos fugitivos, jets)
  — Bolt→Bolt, Arc→ArcRing, Flicker→IsLit, y los caminos suavizados
  (Smooth+JitterPath) ahora son BÉZIER + REFINO FRACTAL (la rugosidad
  multi-escala). **LightningCore.cs ELIMINADA** del mod: una sola librería
  de rayos, la buena.

### D. LOS SOLES RÚNICOS 11-20 (la segunda década, una capa nueva por tier)
  11 · COMETA ORBITAL (cabeza + cola cruzando los anillos) · 12 · LLUVIA
  DE RUNAS cayendo al sol · 13 · AURORA POLAR (cortinas LumenLib.Ray con
  drift de hue) · 14 · ESTRELLA COMPAÑERA azul + PUENTE DE LUZ · 15 ·
  CINTURÓN DE ASTEROIDES kepleriano con brecha · 16 · TORMENTA TOTAL
  (multi-boltos + arco corona) · 17 · CORONA PRISMÁTICA (rayos de luz de
  colores) · 18 · LANZAS PRISMÁTICAS orbitando con estelas · 19 · NÚCLEO
  DE NUEVA (latido a estallido + destellos de limbo) · 20 · EL GRAN
  SELLADO (los 8 glifos maestros en un aro ecuatorial + contrasello
  violeta retrógrado). Packing más tighto del 10º anillo arriba, runas
  +1/anillo (435 glifos en la XX), gigante final ×1.75 en la XX, daño
  80→536.

### E. EL ECLIPSE PRIMORDIAL — LA FUSIÓN TOTAL
  `Bastón del Eclipse Primordial`: un agujero negro SUPREMO (esfera 55px,
  atracción 600px — la mayor del mod) con EL SISTEMA SOLAR RÚNICO XX
  COMPLETO orbitando el horizonte (RuneSunRenderer.DrawOrbitalSystem con
  tier 20: los 20 anillos + gran sellado + cometa) + TODAS las herencias:
  disco Doppler con GRADIENTE AURORA, anillo de bandas sin(θ·20+t·5),
  brazos espirales, halo de BRUMA (Cloud×2 + Puff de HUMO que respira),
  volutas Tendril cayendo, corona de RAYOS StormLib (arcos + multi-boltos),
  jets polares, doble círculo de runas, RAYOS PRISMÁTICOS LumenLib + el
  destello del corazón + el velo aurora. Muerte = ANILLO DE EINSTEIN +
  NOVA RÚNICA (las dos explosiones juntas) + 34 runas de eco. Lente
  gravitacional ×4.2 registrada en BlackHoleLensSystem.

### F. LA ENVOLTURA DE FUEGO PRIMORDIAL (cosmético interactivo)
  `FireVeilItem` + `FireVeilPlayer` + `FireVeilRenderer` +
  `FireVeilDrawLayer`: un CAMPO DE 26×38 celdas de intensidades 0..36 vive
  sobre el jugador — el algoritmo clásico de PROPAGACIÓN DE FUEGO (base
  siempre encendida, decaimiento aleatorio, deriva lateral) con TABLA DE
  37 COLORES propia (brasa→carmesí→naranja→ámbar→oro→blanco). EL FUEGO
  INTERACTÚA: al CORRER el viento inclina las llamas EN CONTRA de la
  marcha y las AVIVA (más intensidad + brasas sueltas) · al SALTAR se
  APLASTAN y se retrasan por debajo (la inercia) · al CAER se ESTIRAN
  hacia arriba (pases extra de propagación) · al VOLAR se vuelven COLUMNA
  (pases dobles) · quieto: la lumbre calma. Chispas + humo + luz cálida
  respirando. 100% código: cero sprites de fuego.

### G. LA CORONA DE ANILLOS RÚNICOS (cosmético)
  `RuneRingCrownItem`: TRES anillos rúnicos orbitando el CUERPO (el aro
  dorado casi vertical del pecho, el blanco-estelar inclinado en
  contrarroto y el ecuatorial azul de la cintura) — la técnica de los
  anillos del Sol puesta sobre el jugador, con glifos a la tangente,
  perlas y latidos. Chispas doradas/azules + luz mixta.

### H. EL ANILLO RÚNICO ESTELAR (alas + halo)
  `RunicHaloWings` (vuelan de verdad: 180 ticks, velocidad 9, ×2.5 — el
  patrón AutoloadEquip + PNG 8×8 en blanco de v6.12): un GRAN ANILLO
  RÚNICO vertical tras la espalda con contraro, glifos y corazón de luz.
  AL VOLAR SE ENCIENDE (la ENERGÍA DE VUELO de RunicHaloPlayer: +0.09/tick
  volando): el bloom ×2.2, la CRUZ DE LUZ, los 8 rayos radiales, el doble
  ancho del aro y las runas ardiendo al blanco. Chispas tangenciales a
  borbotones + luz del motor.

### I. ENTREGA
  · 15 nuevos PNGs procedurales (LumenBloom/LumenBlade/LumenFlare/
    FlameBrush + iconos XI-XX + Eclipse + 3 cosméticos + sombra del
    proyectil + _Wings 8×8) — validados VLM en 2 rondas ("LISTOS PARA
    INTEGRACIÓN").
  · Localización es-ES + en-US completa (15 DisplayName nuevos).
  · EnsureItem ×2 sitios para TODO lo nuevo (10 bastones + Eclipse + 3
    cosméticos).
  · build.txt 6.22 · compilación 0 errores / 0 warnings contra tML real.

## Commit v6.21 — LA TORMENTA: rayos de verdad + el fix de los soles + sin maná

**Petición del usuario**: "la imagen muestra cómo se ve el sol, solo se ve su
textura, no tiene animación, solo un cuadrado con textura · en cuanto al
bastón de rayos, eso no son rayos de verdad, no se parecen en nada a rayos,
es momento de investigar y mejorar · todos los bastones que crees son de
prueba, por lo tanto no necesitan usar mana · investiga Coralite, Everglow,
Wrath of the Empress y Lunar Veil — sus librerías y técnicas — y crea tus
propias librerías con todo lo aprendido de la investigación profunda y
metódica".

### A. LA INVESTIGACIÓN PROFUNDA (4 repos clonados y estudiados a fondo)
  · **Coralite** (360 MB, 78 archivos de rayos) — el jefe eléctrico y su
    librería de descargas: jitter perpendicular con extremos anclados,
    parpadeo con APAGADO del ~50% a 15 Hz, MULTI-FILAMENTO superpuesto
    (2 colores), muerte violenta (el jitter REVIENTA al disolverse),
    gorros a 2 escalas, daño en la LÍNEA RECTA (el jitter es cosmético).
  · **Everglow** (658 MB) — árbol de rayos RECURSIVO con AUTO-CORRECCIÓN
    de curvatura (rot −= totalRot·0.3), el "hervir" de todos los puntos,
    el FLASH MULTI-DRAW (redibujar la misma geometría N veces), ancho
    empaquetado en las coords de textura, la capa negra bajo las estelas.
  · **Wrath of the Empress** (16 MB) — la CRUZ DE LUZ de 4 draws en los
    impactos (2 orientaciones × 2 escalas con pulso), el TELEGRAPH como
    contrato (línea de aviso + daño/movimiento gateados), paletas por datos.
  · **Lunar Veil** (27 MB, linaje Stellamod) — el sándwich de batch
    Immediate (valida nuestro contrato v6.10), endcaps redondeados,
    `extraUpdates` para densidad, screen-shake con atenuación por distancia.
  · Los 4 informes completos (Task IDs 37-a…37-d) viven en el worklog;
    la síntesis operativa en `research/storm_v621/INFORME.md`.

### B. STORMLIB — LA SEGUNDA GENERACIÓN DE RAYOS (librería nueva, 100% propia)
  · **4 texturas procedurales nuevas** (RGB blanco + perfil en alfa):
    `BoltHalo` (banda gaussiana suave), `BoltCore` (EL FILAMENTO: núcleo
    blanco que serpentea dentro de la textura con GRIETAS de alta
    frecuencia y nodos brillantes — el análisis VLM de las texturas reales
    del ecosistema medido y replicado), `BoltChain` (eslabones) y
    `BoltImpact` (estallido radial de 7 rayos desiguales).
  · **`Content/VFX/StormLib.cs`** (LightningCore queda INTACTA para los
    agujeros negros): ZigPath (perpendicular + dispersión + envolvente,
    extremos EXACTOS), **Refine** (subdivisión de punto medio con sesgo
    cúbico — la rugosidad MULTI-ESCALA que rompe el zigzag geométrico),
    Boil, ForkTree (ramas con auto-corrección, ×0.40 de ancho),
    **MultiBolt** (tronco + 2 acompañantes + 2 PELOS caóticos + ramas),
    Strand (TRIPLE CAPA: halo contenido ×2.0 / cuerpo / NÚCLEO BLANCO
    razor ×0.26 — la nitidez vive en la TEXTURA), ChainBolt, ArcRing,
    **ImpactFlash** (cruz de luz 4-draw + destello apilado), EndCap,
    AddLightAlong (luz estrangulada), IsLit (apagado intermitente) y
    DeathGrow (la muerte violenta).
  · Verificación visual (patrón de la casa): `tools/mock_storm_v621.py`
    traduce StormLib 1:1 a Python sobre las texturas reales → mock VLM
    **8/10 "reads as real lightning, release-quality, excellent in
    motion"** (v1 6.5 → v2 8 tras afinar: núcleo fino, fractal, pelos).

### C. EL CETRO DEL TRUENO RECONSTRUIDO — EL RAYO DEL CIELO
  · `RunicLightning` v6.21: ya no dispara una línea horizontal borrosa —
    **INVOCA UN RAYO QUE CAE DEL CIELO** sobre el cursor: TELEGRAPH de 10
    ticks (línea fina de aviso + anillo objetivo pulsante + carga a
    ráfagas) → el GOLPE (daño en la COLUMNA cielo→suelo por línea recta +
    estallido radial de 110 px + CADENA a 3 enemigos al 60% + Electrified
    240) → LA DESCARGA (MultiBolt de 3 filamentos + pelos + ramas,
    regenerado a 15 Hz con apagado intermitente, muerte violenta con
    DeathGrow) → EL IMPACTO (cruz de luz + destello apilado + onda de
    choque + arcos crispados + 40 chispas + puñetazo de cámara VERTICAL +
    trueno grave por capa de zaps con pitch −0.45).
  · `StormRuneStaff`: **MANA 0** (regla del usuario: todos los bastones
    son de prueba — el resto de la familia ya estaba en 0), disparo al
    cursor clampeado a 560 px, tooltip actualizado, sonido de uso grave.

### D. EL FIX DEL SOL RÚNICO (el cuadrado con textura)
  · **CAUSA RAÍZ**: `RuneSunRenderer.BeginAdditive/BeginAlpha` usaban
    `SpriteSortMode.Deferred` → el pase `Passes[0].Apply()` se IGNORA
    (con Deferred el SpriteBatch enlaza su PROPIO efecto al hacer flush)
    → el `DendriticNoise` se pintaba CRUDO como un cuadrado estático sin
    animación (exacto el reporte del usuario); el RadialShine del aura
    también se perdía.
  · **EL FIX**: ambos helpers pasan a `SpriteSortMode.Immediate` con
    `LinearWrap` — IDÉNTICOS al sol original que sí se ve bien → el disco
    de plasma vuelve a ser la esfera animada con SunShader y el aura
    vuelve a ser el resplandor de ruido vivo. (LECCIÓN v6.21: cualquier
    pase de shader exige Immediate — el contrato del batch no es opcional.)

## Commit v6.20 — FIX DE LOS SOLES RÚNICOS + EL CETRO DEL TRUENO + EL AGUJERO NEGRO SUPREMO AURORA

**Petición del usuario**: "que raro, no veo los soles, los bastones sí
invocan al sol, pero el sol no se ve, o sea el proyectil es invisible,
lo mismo con el bastón de rayo, no se ve nada y además dio error · el
sol original sí se ve, pero los nuevos soles no se ven ni el bastón de
rayos · en el agujero negro supremo, cambiar el color: centro negro y
que vaya cambiando de color — a morado cerca del centro, azul y dorado
en los bordes; guardar una copia del original y crear uno nuevo con
estos cambios".

### A. EL BUG DE LOS PROYECTILES INVISIBLES (v6.19 corregido)
  · **CAUSA RAÍZ** (del client.log): `InvalidOperationException: Begin
    has been called before calling End` — `RuneSunProjectile.PreDraw`
    y `RunicLightning.PreDraw` llamaban a sus renderers CON EL
    SpriteBatch de tML todavía ABIERTO; el primer `BeginAdditive()`
    interno re-abría un batch ya abierto → excepción CADA FRAME → el
    dibujo abortaba → proyectil INVISIBLE (los bastones sí invocaban).
  · **EL FIX**: el CONTRATO DE BATCH A PRUEBA DE BALAS (v6.10, el mismo
    de la familia de agujeros negros) aplicado a los dos PreDraw:
    `End()` defensivo → renderer (cerrado→cerrado) → `RestoreSpriteBatch()`
    con los parámetros EXACTOS del pase de proyectiles de vanilla
    (`Main.Rasterizer` + `Main.Transform`).
  · Los 10 Bastones del Sol Rúnico I..X y el Cetro del Trueno Rúnico
    ahora pintan su sistema completo (anillos, runas, rayos, efectos).

### B. EL AGUJERO NEGRO SUPREMO AURORA (el original queda INTACTO)
  · **SupremoAuroraBlackHoleRenderer.cs** (VFX): la copia NUEVA del
    Supremo con EL GRADIENTE DEL USUARIO como FUNCIÓN —
    `AuroraGrad(t)`: MORADO (185,105,255) en t=0 → AZUL (92,150,255) →
    DORADO (255,195,90) en t=1 — aplicado a CADA CAPA por su distancia
    radial al núcleo:
    · EL NÚCLEO → NEGRO ABSOLUTO (BlackDisk, repintado final intacto).
    · JUNTO AL NÚCLEO → MORADO: el rim del horizonte, el anillo de
      fotones, los arcos LightningCore, el interior del disco Doppler
      (blanco frío el lado que acerca, morado profundo el que aleja) y
      la nube interna de Bruma.
    · EL MEDIO → AZUL: los brazos espirales (gradiente A LO LARGO del
      brazo), el círculo interior de runas, el contrarroto del aura y
      el lado que acerca del disco de acreción.
    · LOS BORDES → DORADO: el anillo de bandas (cada SEGMENTO coloreado
      por su distancia radial real 1.24..1.95R — la petición hecha
      geometría), las puntas de los brazos y de los JETS POLARES
      (morado→azul→dorado a lo largo del haz), el círculo exterior de
      runas, la nube externa y el aura final.
    · LAS ONDAS DE DISTORSIÓN NACEN MORADAS y MUEREN DORADAS (el
      gradiente expandiéndose por el espacio-tiempo), y los rayos
      fugitivos son 2 AZULES + 1 DORADO.
  · **SupremoAuroraBlackHoleProjectile.cs**: la MISMA física suprema
    (esfera 55px, atracción 550px, devora balas, anillo de Einstein)
    con la paleta aurora en luz (centro morado, polo azul, base
    dorada), partículas, disco de acreción e impactos.
  · **SupremoAuroraBlackHoleStaff.cs**: "Bastón del Agujero Supremo
    Aurora" — 300 de daño, tooltips cortos, localización es/EN.
  · Registro completo: BlackHoleLensSystem (lente gravitacional:
    fuente + radio + núcleo encima de la lente), EnsureItem al entrar
    al mundo, sombra 76×76 (rim morado→azul, peak 78) e icono 28×30
    (bastón morado + mini-agujero con el gradiente pintado por píxel +
    runa dorada + destellos fríos) — tools/gen_supremo_aurora_tex_v620.py.

## Commit v6.19 — LOS SOLES RÚNICOS + EL CETRO DEL TRUENO

**Petición del usuario**: "ahora hagamos que el sol sea más mágico: al
igual que los agujeros negros tienen anillos con runas, crea varias
copias del sol y ponles anillos con runas — la primera un solo anillo,
la segunda 2 (uno rodea el sol y el otro en OTRA dirección), la tercera
3 y así hasta 10 copias, cada una mejorada un poquito más hasta la
copia 10 con muchas mejoras y animaciones · el SOL ORIGINAL NO SE TOCA
· crea un arma que use rayos con NUESTRA librería, con varios efectos
en los rayos".

### A. LA FAMILIA DE LOS SOLES RÚNICOS (10 copias — el Sol original INTACTO)
  · **RuneSunRenderer.cs** (VFX): UNA sola clase parametrizada por
    copia (tier 1..10) — el cuerpo solar hereda la TÉCNICA del sol
    (backglow BloomCircle + aura RadialShineShader + disco de plasma
    SunShader con granulación dendrítica, re-implementada — cero
    dependencia del SunProjectile) + el SISTEMA RÚNICO:
  · **N ANILLOS = el número de la copia**: cada anillo vive en SU
    PROPIO plano orbital (semiejes, achatado e inclinación distintos)
    con **GIRO ALTERNO** — el par gira horario, el impar antihorario
    ("el otro rodea el sol en otra dirección").
  · Cada anillo: aro elíptico de cápsulas con PROFUNDIDAD (el frente
    más brillante) + glifos de la **ESCRITURA SOLAR** (8 diseños
    nuevos: el Astro, la Llama, la Rueda, la Espiga, la Puerta del
    Día, la Corona, el Cometa y el Sigilo) cabalgando la órbita
    ROTADOS A LA TANGENTE, con perlas y latidos propios.
  · **MEJORAS PROGRESIVAS** (una capa por copia): 2·chispas orbitales
    3·destellos de 4 puntas · 4·prominencias de plasma del limbo ·
    5·viento solar · 6·rayos fugitivos entre anillos (LightningCore) ·
    7·precesión de los planos + acentos azul-estelar cada 3er anillo ·
    8·núcleo pulsante + ondas de eco · 9·corona de pétalos de plasma ·
    10·ERUPCIÓN RÚNICA (runas desprendiéndose y volando) + JETS
    POLARES con rayo interno — el sistema completo.
  · **RuneSunProjectile.cs**: UN proyectil parametrizado (ai[0] =
    copia): vida 10 s + 0.2 s por copia, pop elástico, persecución
    suave de enemigos, aura ardiente cada 10 ticks, GIGANTE FINAL
    (hinchazón ×1.5 + daño ×1.5 en los últimos 1.5 s) y UNA SOLA NOVA
    RÚNICA al morir (onda nova + AoE + ráfaga de runas de eco — la
    lección v5.97 de "una sola explosión").
  · **RuneSunStaves.cs**: 10 bastones (Bastón del Sol Rúnico I..X),
    daño 80→296, tooltips cortos de 2 líneas, localización es/EN.
  · Iconos 28×30 procedurales: bastón dorado + cabeza solar + N
    anillos elípticos de inclinaciones alternas (tools/
    gen_rune_suns_v619.py).

### B. EL CETRO DEL TRUENO RÚNICO (el arma de rayos — LightningCore)
  · **RunicLightning.cs** — un LINE-STRIKE: el rayo nace en la punta
    del jugador y golpea AL INSTANTE el punto del cursor (el proyectil
    NUNCA se mueve). EFECTOS APLICADOS AL RAYO:
    1. Rayo principal zigzag vivo ~14 Hz con DOBLE TIRA cuerpo-oro +
       núcleo-blanco, ramas y gorros (LightningCore.Bolt).
    2. DAÑO EN LÍNEA: Collision.CheckAABBvLineCollision barre todo lo
       que cruza la descarga.
    3. CADENA ELÉCTRICA: hasta 3 saltos a enemigos cercanos (60% del
       daño, rayos secundarios azul-estelar).
    4. ARCOS DE IMPACTO: dos coronas eléctricas vibrando (Arc CW+CCW).
    5. ELECTRIFICADO (240 ticks) a todo lo tocado.
    6. ONDA DE CHOQUE expandiéndose + luz a lo largo de toda la línea.
  · **StormRuneStaff.cs**: Cetro del Trueno Rúnico — daño 95, mana 12,
    alcance 560 px, icono procedural con núcleo eléctrico y rayo
    dentado.
  · Sombras PNG 76×76 de ambos proyectiles nuevos (la lección v6.14.1:
    tModLoader auto-requesta la textura de TODO ModProjectile).

### C. ENTREGA
  · TestingPlayer: los 10 bastones + el cetro con EnsureItem.
  · Localización es-ES + en-US (nombres y proyectiles).
  · Compilación 0 errores / 0 warnings contra tModLoader
    v2026.07.3.0 real.

## Commit v6.18 — LA GRAN CONSOLIDACIÓN: 9 AGUJEROS + LIBRERÍA DE RAYOS

**Petición del usuario**: "revisa la ventana de información de todos los
agujeros negros (demasiado larga, no se ve el nombre) · borra todas las
alas · oculta el agujero base · quita el del vacío y la fusión · los 4
definitivos (Umbral, Bruma, Cósmico, Olvido) se quedan sin partículas
en el centro · Cósmico rojo-naranja, Olvido morado-azul · crea el
AGUJERO NEGRO SUPREMO combinando los 4, mejorado y potenciado · crea
librerías y actualiza las que hay · copia los 4 y mejora las copias
dejando los originales intactos · limpia referencias externas — las
técnicas son NUESTRAS".

### A. LA LIMPIEZA
  · **TOOLTIPS CORTOS**: todas las ventanas de info de los agujeros
    reducidas a 2 líneas + nombres en español por localización (antes:
    5 párrafos y el nombre auto-generado en inglés).
  · **TODAS LAS ALAS BORRADAS**: 8 items + 9 VFX + draw layer + anim
    player + localización ("todas están mal, no se ven nada bien").
  · **Agujero BASE OCULTO** (no borrado — el código sigue en el mod).
  · **Agujero del VACÍO y FUSIÓN ELIMINADOS** (archivos + refs + PNGs).

### B. LOS 4 DEFINITIVOS — UMBRAL · BRUMA · CÓSMICO · OLVIDO
  · Centro de la bola negra LIMPIO en Umbral/Cósmico/Olvido (fuera
    filamentos interiores y rayos del núcleo — como el Bruma).
  · **CÓSMICO RECOLOREADO a ROJO-NARANJA** (paleta, partículas, sombra,
    icono — antes magenta del script Unity).
  · **OLVIDO RECOLOREADO a MORADO-AZUL** (paleta, partículas, sombra,
    icono — antes púrpura-rosa).

### C. LIGHTNINGCORE — LA LIBRERÍA DE RAYOS (nueva, 100% propia)
  `Content/VFX/LightningCore.cs`: rayos con jitter perpendicular de
  cuerda, DOBLE TIRA cuerpo+núcleo, ramificación heredada con
  decaimiento, arcos circulares eléctricos, suavizado Catmull-Rom,
  parpadeo determinista (FlickTick/Flicker) — síntesis de toda la
  investigación del ecosistema, re-implementada con código propio.

### D. EL AGUJERO NEGRO SUPREMO (el 5to definitivo)
  `SupremoBlackHoleRenderer.cs` (998 líneas, 15 capas): la combinación
  de los 4 — disco Doppler (Umbral) + anillo de bandas sin(θ·20+t·5)
  (Cósmico) + brazos espirales con flujo (Olvido) + halo/volutas de
  humo (Bruma) + ⚡coronas de descarga LightningCore + doble círculo
  rúnico contrarrotante + jets polares con rayos dentro. Esfera de 55px
  (el más grande), atracción de 550px, TRES velocidades de tiempo.

### E. LOS 4 ASCENDIDOS (copias mejoradas — originales INTACTOS)
  · **Umbral Ascendido**: lluvia de 4 rayos ramificados + arco dorado
    giratorio + Doppler a la 5ª potencia + doble anillo rúnico.
  · **Bruma Ascendida**: 3 coronas de escarcha eléctrica + rayos
    gelidos + 5 volutas con curl noise + cristales de hielo.
  · **Cósmico Ascendido**: tormenta de 4-6 rayos PRO + doble anillo
    de bandas + jets polares + doble círculo rúnico.
  · **Olvido Ascendido**: 3 arcos del vacío + rayos ESPIRALES que
    siguen los brazos + brazos reforzados + fotones-micro-rayo.
  Todos con aura 15% más rápida y muerte más rica.

### F. INTEGRACIÓN
  · 5 nuevos registrados en BlackHoleLensSystem (lente gravitacional
    propia para cada uno) + TestingPlayer (entrega) + localización
    es-ES/en-US + sombras 76×76 + iconos 28×30 (generadores Python).
  · **Limpieza de referencias externas**: todo comentario citando
    otros mods/técnicas externas eliminado — las librerías (Bruma,
    LightningCore, VFXCore) y sus técnicas son 100% NUESTRAS.
  · Compilación 0 errores / 0 warnings.

**ESTADO FINAL: 9 agujeros negros** — 4 definitivos + 4 ascendidos +
el Supremo. La base oculta. Vacío y fusión eliminados.

## Commit v6.16 — TRES AGUJEROS NUEVOS + LA LIBRERÍA DE BRUMA (humo/niebla procedural)

**Petición del usuario** (dos tareas en un mensaje):
1. "es momento de crear otro, esta vez crea dos agujeros negros: en uno
usa esto [script Unity CosmicBlackHole.cs + shader Custom/CosmicRing] y
agrega lo que falta; y el otro hazlo usando la referencia como base...
todo lo debes hacer por código. No olvides darle los 2 nuevos agujeros
negros al jugador y todo en español."
2. "crea una librería especializada en humo, niebla, bruma y todo eso
de forma procedural y con calidad, que sea capaz de usarse en cualquier
proporción ya sea grande o pequeño y en todo se vea bien; para esto
investiga otros mods y busca recursos en internet... luego crea un 3er
agujero con todo lo aprendido y librerías creadas en el proyecto, todo
por código."

**Los 4 agujeros existentes (base, vacío, fusión, olvido): INTACTOS.**
El mod tiene ahora SIETE agujeros negros.

### A. EL AGUJERO NEGRO CÓSMICO (#5) — nacido del script Unity del usuario

`CosmicBlackHoleRenderer.cs` + `CosmicBlackHoleProjectile.cs` +
`CosmicBlackHoleStaff.cs` (Bastón del Agujero Negro Cósmico). El script
Unity traducido FIEL al sistema de pinceles del mod (100% código):

  · **El shader, exacto**: `glow = sin(uv.x·20 + t·5)·0.5+0.5` → VEINTE
    BANDAS de brillo recorriendo el anillo a 5 rad/s — la emisión
    pulsa VIAJANDO, como el CosmicRing del usuario.
  · **El color, exacto**: magenta (1.0, 0.2, 0.8) = (255,51,204).
  · **Rotación 20°/s** (rotationSpeed del script), **distorsión
    sinusoidal global** (sin(t)·0.3 — el SetGlobalFloat del shader),
    **lightningParticles** (2 rayos violeta en el núcleo + 2 magenta
    escapando del anillo), **runeParticles** (8 runas doradas de
    CIRCUITO orbitando + perlas), **coreSphere** (BlackDisk absoluto).
  · **Lo que faltaba, añadido**: aura oscura, nebulosas violeta/azul,
    ecos del anillo (la resonancia del shader), corredores de fotones
    con estela, destellos polares, ondas de distorsión, 9 partículas
    radiales y aura final pulsante.

### B. EL AGUJERO NEGRO DEL UMBRAL (#6) — el agujero de LA REFERENCIA

`UmbralBlackHoleRenderer.cs` + proyectil + bastón. La referencia del
usuario (imagen 1536×1024) medida PÍXEL A PÍXEL (perfiles radial y
angular, distribución de tonos, localización de núcleos blancos y
píxeles dorados) y reconstruida 100% por código:

  · **La geometría medida — la topología "∞"**: esfera de vacío +
    disco fino cruzando POR DEBAJO (línea delantera a ~1.4R) + **ARCO
    DE LENTE DOBLE sobre la esfera** (el lado lejano doblado ARRIBA:
    banda salmón a 1.45R + segundo anillo de fotones a 1.22R con línea
    de filo fina e incandescente) + **ARCO INFERIOR magenta** (la
    imagen lenseda de abajo, 1.28R) + **ALA BARRIDA** (banda circular
    GORDA a 2.45R barriendo de abajo-derecha al extremo izquierdo —
    "a broad, sweeping wing of light").
  · **El Doppler medido**: máximo en el extremo IZQUIERDO (lum 185 vs
    128 del derecho — cúbico), núcleos blanco-rosado (249,210,220)
    concentrados en 120-180°, y la **CUÑA OSCURA** de 240-270°
    (t≈4.45, el sector muerto medido).
  · **La paleta medida** (¡rosa/magenta, NO naranja!): blanco-rosado
    (250,210,220), rosa caliente (243,128,149), rosa (225,74,127),
    carmesí-rosa (183,29,83), magenta profundo (153,14,76), vino
    (110,17,51), ala magenta (240,41,168), arco salmón (248,110,95).
  · **El círculo de runas**: dorado-ámbar MEDIDO (240,124,65), 12
    glifos de SIGILO ANTIGUO (colmillos, coronas, garras) con HUECOS
    por hash + glifos apagados donde la PÚA cruza el círculo; aro roto
    en 30 segmentos.
  · La PÚA de energía blanco-rosa (sup-derecha), el RAYO naranja-rojo
    dentado ramificando abajo, 12 BRASAS con estelas de movimiento,
    filamentos violeta cayendo al vacío y el vacío FINAL repintado
    (el centro queda del NEGRO MÁS ABSOLUTO: lum 3.4 en el mock).

### C. LA LIBRERÍA DE BRUMA — humo/niebla/bruma procedural (Content/Effects/Bruma)

Investigación previa REAL (subagente de investigación web, 34 búsquedas
+ 12 fuentes leídas completas: JangaFX/Diablo 3 — reglas anti-fase y
"Scale by Mids" de Julian Love —, The Book of Shaders, Inigo Quilez —
fBm/warping/band-limiting —, VFXDoc — erosión de alfa —, vfxlabs —
overdraw/paralaje —, vanilla Terraria DECOMPILADO — tinte por
iluminación, smear 130-134, LOD por conteo —, Calamity — flipbooks y 3
lotes de blending —, Starlight River — partículas por GPU —,
ParticleLibrary). Informe completo: `research/smoke_research_v616/`.

  · **`BrumaNoise.cs`** — hash determinista + value noise con QUINTIC
    de Perlin + fBm con LACUNARIDAD 2 EXACTA (anti-fase Diablo 3) +
    **domain warping** de IQ (el look "humo vivo") + **curl noise**
    (remolinos sin divergencia) + **OctavesForRadius** (band-limiting:
    el detalle fino mide SIEMPRE ~3px) + Erode (la erosión de alfa de
    VFXDoc: el humo muere en GRUMOS, no se desvanece) + ByMids.
  · **`BrumaBrushes.cs`** — 8 texturas de puff 128×128 NACIDAS DE
    CÓDIGO EN RUNTIME (Texture2D+SetData, CERO PNGs): RGB blanco +
    alfa = falloffRadial(blando) × ByMids(WarpedFbm) — la máscara
    blandita y el ruido con detalle (regla Diablo 3). Disposición en
    BrumaSystem.Unload (cero fugas de VRAM).
  · **`BrumaFX.cs`** — LA API: `Puff` (núcleo texturizado + sub-blobs
    ∝ PERÍMETRO con respiración DESFASADA y presupuesto de alfa de
    COBERTURA CONSTANTE 1−(1−A)^(1/(n+1)) — la invariancia de escala
    por construcción), `Cloud` (racimo con deriva por senos
    INCONMENSURABLES 0.31/0.71 + curl), `Tendril` (voluta por ruta con
    balanceo y erosión), `Column` (nace, crece al ascender, se erosiona
    al morir) y `MistBand` (capas de niebla con paralaje y gradiente
    vertical). Contrato: dibuja en el lote ABIERTO que el llamador
    elija (aditivo = bruma LUMINOSA; alfa = bruma QUE OCLUYE).
  · **`BrumaSystem.cs`** — el ciclo de vida (disposición al recargar).

### D. EL AGUJERO NEGRO DE LA BRUMA (#7) — la demostración de la librería

`BrumaBlackHoleRenderer.cs` + proyectil + bastón. Un vacío GELIDO
envuelto en bruma nebular fría (teal/cian/violeta — identidad única
entre los 7): HALO con `BrumaFX.Cloud`, ANILLO de fumarelitos con
`BrumaFX.Puff` (densidad VIVA por fBm), VOLUTAS espiralando al núcleo
con `BrumaFX.Tendril` (la materia devorada se disuelve en humo) y
CHIMENEAS polares + anillo de fotones cian + escarcha flotante.

### E. ENTREGA Y REGISTRO (el checklist completo de lecciones)

  · `TestingPlayer.OnEnterWorld`: EnsureItem × 3 (Cósmico, Umbral,
    Bruma) — el kit garantiza los TRES bastones en cada entrada al mundo.
  · `BlackHoleLensSystem`: los 3 nuevos registrados (fuente de lente +
    multiplicador + DrawCoreVisuals encima de la distorsión).
  · PNGs de sombra 76×76 de los 3 proyectiles (patrón v6.09:
    rim de identidad por agujero — magenta Unity / naranja Doppler /
    teal gelido) + iconos 28×30 (generadores reproducibles
    `tools/gen_cosmic_umbral_tex_v616.py` y `tools/gen_bruma_tex_v617.py`).
  · Auditoría anti-recurrencia: 49/49 clases ModProjectile/ModItem con
    su PNG (0 faltantes).
  · Recetas: 5 madera (como los hermanos).

### F. VALIDACIÓN

  · **Compilación contra tModLoader 2026.07.3.0 REAL**: 0 errores,
    0 warnings (7 iteraciones durante el desarrollo del Umbral).
  · **Mock exacto** (`tools/mock_3agujeros_v616.py`, 1:1 con los
    renderers sobre los pinceles reales + blending XNA modelado):
    hoja comparativa de los 3 + Umbral vs referencia (7 rondas de
    medición/iteración) + Bruma a 0.35× y 1.0× (la invariancia de
    escala de la librería demostrada).
  · VLM sobre la hoja: Cósmico 9/10, Umbral 10/10 como pieza, Bruma
    7.5/10 (mock en `research/agujeros_v616/`).

---

## Commit v6.15 — EL OLVIDO 100% CÓDIGO: LA REFERENCIA ROJA BORRADA, NUEVA REFERENCIA MÁGICA

**Feedback del usuario**: "creaste OlvidoVortex.png y usaste la misma
referencia para crear el agujero negro, eso no puede ser, borra todo
rastro de la referencia OlvidoVortex.png del proyecto, el agujero negro
no puede ser creado por sprite, debe ser creado enteramente por codigo
… borra todas las referencias del agujero negro rojo del proyecto
incluyendo el sprite, los otros agujeros negros no los toques. Entonces
esta vez en el agujero negro del olvido crealo y sustituye todas las
referencias por la nueva referencia que te doy [imagen + prompt:
núcleo oscuro, anillo energético púrpura/rosa, rayos, partículas,
runas doradas y distorsión espacial]".

### A. LA PURGA — TODO rastro de la referencia roja, BORRADO

  · **5 PNGs eliminados** (el arte extraído píxel a píxel de la imagen
    de Reddit en v6.14): OlvidoVortex.png (1024), OlvidoHalo.png,
    OlvidoSphere.png, OlvidoBackplate.png, OlvidoWisps.png.
  · **research/olvido/ eliminado** (3.1 MB): el pipeline entero de
    extracción (scripts, máscaras, drafts, comparaciones, curvas de
    optimización).
  · Comentarios y tooltips que mencionaban la extracción/Regicide
    reescritos. **Los otros agujeros negros (base, vacío, fusión):
    NI UNA LÍNEA TOCADA.**

### B. EL NUEVO OLVIDO — compuesto por CÓDIGO cada frame

`OlvidoBlackHoleRenderer.cs` reescrito de cero: ~380 cuadros de luz por
frame usando SOLO los tres pinceles genéricos GENERADOS POR CÓDIGO de
la biblioteca VFX (SoftGlow = degradé radial, Ring = anillo fino,
BlackDisk = disco negro — los mismos de BoltRenderer y las coronas).
CERO sprites de arte. Cero estado, cero red: hash puro determinista.

Las 13 capas (según la nueva imagen + prompt del usuario):

  · **0. Aura oscura mística** (alfa) — el vacío absorbe la luz.
  · **1. Nebulosas púrpura/azul** difusas girando + polvo carmesí/magenta.
  · **2/5. ANILLO DE PLASMA** — 44 cápsulas por mitad sobre la elipse
    inclinada: hotspot Doppler incandescente + turbulencia hash a 12 Hz
    (zonas brillantes intercaladas con sombras), gradiente térmico
    blanco-amarillo → rosa → violeta; la mitad delantera CRUZA POR
    DELANTE de la esfera.
  · **3. Brazos espirales** del vórtice (magenta → violeta).
  · **4. Núcleo** — disco NEGRO ABSOLUTO + filo violeta respirando.
  · **6. Corredores de fotones** orbitando y acelerando.
  · **7. RAYOS ELÉCTRICOS** — una TORMENTA de 4 rayos violeta con núcleo
    casi blanco y RAMAS fractales DENTRO del vacío (regenerados a ~6 Hz)
    + 2 rayos rosa escapando del anillo.
  · **8. Destellos polares** — agujas ahusadas en los polos del vórtice.
  · **9. RUNAS DORADAS** — 10 glifos angulares ORIGINALES (lanza, cáliz,
    puerta, estrella, rayo, arco, espiral, trono, llave, ojo) orbitando
    en círculo perfecto con latido/flotación propios + anillo rúnico.
  · **10. Ondas de distorsión** expandiéndose (el espacio-tiempo late).
  · **11. Partículas luminosas** con deriva radial hacia afuera.
  · **12. Aura mística** violeta pulsante.

**La lección del brillo**: `Color * f` de XNA escala los 4 canales → el
blending aditivo queda CUADRÁTICO (f²) y todo se apaga (ronda VLM 1:
"too dim, bolts missing"). FIX: helper `Tint(c, f)` con rgb PLENO +
alfa = f → brillo LINEAL (el patrón validado del Cometa Estelar).
Rondas VLM: 5/10 → 7/10 → 8/10 → **9/10 "highly matches"**.

`OlvidoBlackHoleProjectile.cs`: física probada INTACTA, paleta
recoloreada al violeta/fucsia/dorado (dusts, partículas de biblioteca,
iluminación magenta-violeta, impactos y muerte). Icono del bastón
regenerado (28×30, bastón violeta + mini-agujero púrpura/rosa + runa
dorada, VLM ✓). Tooltips nuevos ("100% creado por código").

**Verificación**: mock Python EXACTO (tools/mock_olvido_v615.py,
texturas reales + blending del juego) validado por VLM en 4 rondas →
renders archivados en research/olvido_codigo/. Compilación contra
tModLoader v2026.07.3.0 REAL: **0 errores · 0 warnings**. Auditoría de
assets: 0 texturas faltantes.

## Commit v6.14.2 — LOS BASTONES NUEVOS SE ENTREGAN AL JUGADOR (el kit de pruebas los olvidó)

**Feedback del usuario**: "te olvidaste que debes darselo al jugador".

**Causa**: el sistema de entrega del mod es `TestingPlayer.OnEnterWorld`
— cada arma cósmica en desarrollo se GARANTIZA en el inventario en cada
entrada al mundo (`EnsureItem`, v5.98: "venga de la versión que venga el
guardado del jugador"). La v6.14 añadió los dos bastones nuevos con
receta (5 de madera, como todos) pero jamás los registró en el kit → el
jugador entraba al mundo y NO recibía ni el de FUSIÓN ni el del OLVIDO.

**El fix** (2 líneas en `TestingPlayer.cs`):

```csharp
// v6.14: LOS DOS AGUJEROS NUEVOS — la FUSIÓN (base+vacío) y el
// OLVIDO (100% exacto a la referencia Regicide)
EnsureItem(ModContent.ItemType<Weapons.Cosmic.FusionBlackHoleStaff>());
EnsureItem(ModContent.ItemType<Weapons.Cosmic.OlvidoBlackHoleStaff>());
```

Desde ahora, al entrar a cualquier mundo (single player), el jugador
recibe ambos bastones si no los tiene — mismo protocolo que el carmesí
desde v6.02. La receta de 5 de madera sigue como vía alternativa.

**LECCIÓN anti-recurrencia**: cada arma nueva debe registrarse en DOS
sitios — su archivo (defaults + receta) Y el kit de `TestingPlayer`
(`EnsureItem`). Añadir al checklist de entrega.

**Verificación**: compilación contra tModLoader v2026.07.3.0 REAL:
**0 errores · 0 warnings**.

## Commit v6.14.1 — FIX DE CARGA: las 2 texturas de sombra olvidadas (el mod no cargaba)

**Feedback del usuario**: "hay varios errores" + client.log — el mod se
desactivaba automáticamente al cargar la v6.14 con
`MissingResourceException` × 2:

```
ReLogic.AssetLoadException: Asset could not be found:
    "Content\Projectiles\Cosmic\FusionBlackHoleProjectile"
    "Content\Projectiles\Cosmic\OlvidoBlackHoleProjectile"
```

**Causa raíz**: tModLoader AUTO-REQUESTA la textura por defecto de todo
`ModProjectile` (ruta = namespace + nombre de clase) durante
`TransferAllAssets()`, aunque su `PreDraw` devuelva `false` y jamás se
dibuje. El commit v6.14 entregó los dos agujeros nuevos con todo su arte
procedural/VFX (5 texturas del Olvido + 2 iconos de bastón) pero olvidó
los DOS PNGs de sombra visual por defecto → ambos errores se agregaban
en un `MultipleException` → el mod entero quedaba deshabilitado.

**El fix** (sin tocar NI UNA línea de los agujeros):

  · **FusionBlackHoleProjectile.png** (76×76 RGBA) — sombra con la
    identidad de la FUSIÓN: disco negro sólido (r≤14) + rim DOBLE, ámbar
    del Gargantua base (255,175,80) por dentro + carmesí del vacío
    (200,20,90) por fuera.
  · **OlvidoBlackHoleProjectile.png** (76×76 RGBA) — sombra con la
    identidad del OLVIDO: rim magenta profundo de la referencia Regicide
    (255,45,110) desvaneciendo a rojo oscurísimo (110,0,45).

Ambas calcan el patrón EXACTO del CrimsonBlackHoleProjectile.png medido
píxel a píxel (disco negro r≤14 alpha 255 · rim pico r≈18-22 alpha ~70 ·
desvanecido hasta r=37 · 4184 px visibles vs 4181 del carmesí) —
generadas por `tools/gen_fusion_olvido_projectile_tex.py` (reproducible)
y validadas por VLM como sombras limpias sin artefactos.

**Auditoría completa anti-recurrencia**: script que resuelve la textura
esperada de las 55 clases de contenido por namespace+clase y verifica la
existencia del PNG → 0 faltantes tras el fix (AethonWingsItem es
abstract → tModLoader no la registra). La lección queda registrada:
**cada ModProjectile/ModItem nuevo SIEMPRE necesita su PNG de sombra,
aunque nunca se dibuje.**

**Verificación**: compilación contra tModLoader v2026.07.3.0 REAL
(DLLs del release): **0 errores · 0 warnings**. Pendiente: el usuario
reconstruye en Develop Mods → Build → el mod debe cargar limpio y los
TRES bastones (base/fusión/olvido) funcionar.

## Commit v6.14 — LOS DOS AGUJEROS NEGROS NUEVOS: LA FUSIÓN (base+vacío) Y EL OLVIDO (100% EXACTO a la referencia)

**Feedback del usuario**: "para que el agujero negro sea exacto, has 100
rondas de revisiones profundas con la imagen de referencia… deja este
agujero negro del vacío sin tocarlo, luego crea un tercero que sea la
fusión del agujero negro del vacío con el agujero negro base, y luego
crea un 4to agujero negro que sea 100% exacto a la referencia, este se
debe llamar agujero negro del olvido, asegúrate de que sea 100% exacto,
usa todas las técnicas que sean necesarias".

### A. EL AGUJERO NEGRO DEL OLVIDO — EL ARTE EXTRAÍDO DE LA PROPIA REFERENCIA

**El cambio de técnica decisivo**: tras cuatro versiones intentando
RECREAR el vórtice proceduralmente (v6.09 analítico, v6.10 cresientes,
v6.11 blobs, v6.13 personalidad), el arte del Olvido se EXTRAE
DIRECTAMENTE de los píxeles de la imagen de referencia original
(Ancients Awakened — Regicide, "Oblivion, God of the Void", 1080×795)
y se descompone en CAPAS ANIMABLES. Las "100 rondas de revisiones
profundas" se materializaron como **12 rondas de validación VLM** +
**130 rondas de optimización automatizada** (descenso por coordenadas
sobre 10 parámetros de ganancia minimizando EMA + perfil radial +
calidez contra la referencia) → **EMA final 16.4/255** y veredicto VLM
8/10: "sí, un jugador diría que es el mismo agujero".

El pipeline de extracción (research/olvido/):

  1. **Medición** — esfera negra R=34px en (495,224) por región oscura
     encerrada por plasma; perfil radial del plasma (gap 0.8-1.3R, pico
     1.55-2.55R, brazos hasta 6.4R); elipse del anillo ajustada por
     tracking angular (a=1.70R, b=1.64R, casi circular); hotspot a 125°;
     estrella interior en (-0.31R,-0.25R); rayo púrpura en el cuadrante
     inferior-derecho de la esfera.
  2. **Separación personaje/agujero** — el boss Regicide está EN DELANTE
     del agujero en la referencia. Máscara estructural: plasma = magenta
     (R≫G, B intermedio) + NARANJAS del disco (255,155,85 — el gradiente
     caliente que faltaba) + blancos calientes del hotspot; personaje =
     SOLO colores equilibrados (plata/máscara/cuernos). Verificada por
     VLM con overlay de clasificación (4/10 → estrategia corregida).
  3. **Inpainting angular** — donde el personaje tapa plasma esperado,
     interpolación bilateral del perfil angular del mismo radio (2781→
     1062 celdas polares reconstruidas tras refinar la máscara).
  4. **Suavizado** — cierre morfológico (anti sal-y-pimienta), máscara
     gaussiana σ=1.15px (bordes antialias), color extendido por EDT
     (sin franjas oscuras al muestrear bilineal), RGB muestreado
     BILINEAL (sin bloques), esfera supersampleada ×4.
  5. **Bloom horneado** — el plasma brillante difuminado (σ=16) y
     sumado al halo: el "glow" desbordado del anillo de la referencia.
  6. **Optimización 130 rondas** — bg rojizo (35.5,0,6.2), halo_g 2.05,
     halo_a 1.78, vortex_g 0.95, ring_g 1.04 → EMA 29→16.9, calidez
     errónea 25.8→0.0 (la pérdida solo mide la zona del agujero, sin
     los píxeles irreplicables del personaje).

Las CINCO texturas nuevas (Content/Effects/Procedural/), todas en
formato premultiplicado A=255 para las aditivas (el RGB lleva la
cobertura horneada → blending aditivo LINEAL, la lección del
OblivionBlob v6.11):

  · **OlvidoVortex.png** (1024) — EL ARTE EXACTO: anillo de fotones +
    disco + brazos espirales + aguja + velos, con inpainting donde el
    boss tapaba y ganancias horneadas.
  · **OlvidoHalo.png** (256) — resplandor ambiental + bloom del anillo.
  · **OlvidoSphere.png** (160) — esfera de NEGRO PROFUNDO con la
    ESTRELLA rosa y el RAYO púrpura interiores, tal cual.
  · **OlvidoBackplate.png** (256) — el vacío rojizo de la referencia
    (placa oscura de fondo: de día el agujero lleva SU oscuridad
    consigo — validado VLM como "bolsillo de oscuridad" legible al
    100% en cielo diurno).
  · **OlvidoWisps.png** (512) — velos exteriores que ROTAN lento.

**OlvidoBlackHoleRenderer.cs** (contrato de batch cerrado→cerrado, la
misma garantía a prueba de balas del v6.10): backplate (alfa) → halo
pulsante + velos girando + vortex exacto respirando (aditivo) → esfera
(alfa) → overlays vivos (aditivos, sutiles, no tocan el arte exacto):
pulsos de fotones recorriendo el anillo a 72°/s, llamarada del hotspot
cada 4.2s con decaimiento exponencial, y seis chispas cayendo en
espiral hacia el horizonte. Esfera GIGANTE: 52px de radio, arte de
811px de envergadura.

**OlvidoBlackHoleProjectile.cs** — física 100% probada (copia del
carmesí: pop elástico, atracción, aura con ticks acelerados, devora
balas, persecución, evaporación, anillo de Einstein final) con lente
propia (mult 3.4) y paleta del olvido. **El agujero del vacío queda
INTACTO** (ni una línea tocada).

**OlvidoBlackHoleStaff** (daño 150) con icono 28×30 generado (bastón +
mini-agujero carmesí, validado VLM) y tooltips completos.

### B. EL AGUJERO NEGRO DE FUSIÓN — LA FUSIÓN LITERAL DE LOS DOS PADRES

**FusionBlackHoleProjectile** — petición: "la fusión del agujero negro
del vacío con el agujero negro base". Su DrawCoreVisuals encadena AMBOS
renderizadores originales en el MISMO centro, cada uno con su identidad
intacta:

  1. **DETRÁS** — `BlackHoleProjectile.DrawCoreVisuals(p, false)`: el
     Gargantua de marcha de luz del BASE (RealBlackHoleShader de 75
     pasos, disco naranja lensado, halo ámbar, refuerzo del horizonte).
  2. **DELANTE** — `CrimsonBlackHoleRenderer.Draw(·, scale×0.68, ·)`: el
     VÓRTICE OBLIVION del VACÍO con sus SIETE capas de personalidad
     v6.13 (ondas de espacio-tiempo, pulsos de fotones, chorros
     relativistas, corrientes de materia, llamaradas, arcos de Einstein,
     rim violeta) a 0.68× — su esfera negra se alinea con el horizonte
     del Gargantua y el ANILLO NARANJA LENSADO asoma alrededor.

El resultado: fuego y vacío en un solo cuerpo. Física idéntica probada
con radio de atracción ampliado (480px — "la suma de ambas masas"),
paleta de partículas DOBLE (carmesí del vacío + naranja del Gargantua
entremezcladas en dusts, estelas, implosiones y explosiones), lente
propia (mult 3.2) e icono propio (anillo naranja+carmesí, validado VLM
como "solar-void" distinguible).

**FusionBlackHoleStaff** (daño 130) con tooltips de la doble estirpe.

### C. REGISTRO EN LA LENTE GRAVITACIONAL

`BlackHoleLensSystem` (ediciones aditivas, sin tocar el comportamiento
de los agujeros existentes): los tipos Fusion y Olvido se recogen como
fuentes de distorsión con sus propios multiplicadores (3.2 y 3.4) y se
dibujan ENCIMA de la lente con sus DrawCoreVisuals propios (mismo
protocolo que el carmesí desde v6.02).

### D. VERIFICACIÓN

  · **Compilación**: Build succeeded · 0 errores · 0 warnings contra
    tModLoader v2026.07.3.0 REAL (DLLs del release, /tmp/verify).
  · **12 rondas VLM**: máscara (r3), comparaciones v1→final (r4-r10),
    iconos (r11), cielo diurno (r12) — final 8/10 "mismo agujero".
  · **130 rondas de optimización** con pérdida restringida a la zona
    del agujero (sin píxeles del personaje) — curva guardada en
    research/olvido/optim_result.json.
  · **Pendiente (el usuario prueba)**: Develop Mods → Build → los TRES
    bastones (BlackHoleStaff base intacto, FusionBlackHoleStaff nuevo,
    OlvidoBlackHoleStaff nuevo) → client.log limpio.

## Commit v6.13 — EL AGUJERO NEGRO CON PERSONALIDAD + LAS 8 ALAS RE DISEÑADAS DE CERO CON LA TÉCNICA DE LAS CORONAS

**Feedback del usuario**: "el agujero negro no se parece en nada a la
referencia, veo que te cuesta mucho crear el agujero negro, solo debes
tomar el agujero funcional que tenemos como base y adaptarlo, darle más
personalidad, más efectos y todo eso. Y las alas se siguen viendo muy
feas, crea nuevos diseños de alas con la técnica de la corona".

### A. EL AGUJERO NEGRO — SIETE CAPAS NUEVAS DE IDENTIDAD (base intacta)

Nueva estrategia (directiva del usuario): YA NO perseguir la réplica
píxel-exacta de la referencia — tomar el agujero FUNCIONAL v6.12 (que ya
dibuja esfera + anillo + vórtice sin errores) y darle PERSONALIDAD. La
física, la lente gravitacional, las partículas y el contrato de batch
(cerrado→cerrado) quedan INTACTOS. Siete capas nuevas, todas deterministas
(cero estado, cero red):

  · **0.5 ONDAS DE ESPACIO-TIEMPO** — anillos finos (textura Ring real,
    aplastada e inclinada como el vórtice) que nacen pegados al horizonte
    y se expanden hasta 7R: el vacío "late". Ciclo de 2.8s, dos ondas
    desfasadas. Color magenta saturado (visible en cielo diurno y noche).
  · **2.5 PULSOS DE FOTONES** — dos destellos blanco-candente que CORREN
    por el anillo interior a 2.4× la velocidad del vórtice, con estela
    corta rosa: luz orbitando y acelerando.
  · **3.5 CHORROS RELATIVISTAS** — dos haces polares (dirección = normal
    del plano del disco, como M87): núcleo blanco-rosa + manto violeta,
    afinándose hacia la punta, con TRES bolas de plasma viajando hacia
    fuera por haz. Se dibujan ANTES de la esfera → sus bases quedan
    TRAGADAS por el horizonte.
  · **3.6 CORRIENTES DE MATERIA** — cinco riachuelos de plasma que caen
    en espiral desde 5.4R (aceleración gravitatoria: ease u^1.45) hasta
    1.44R y DESAPARECEN TRAS EL HORIZONTE; se vuelven blanco-rosa al
    rozarlo (Doppler).
  · **3.7 LLAMARADAS DEL DISCO** — prominencias periódicas (ciclo 3.4s):
    arcos de cápsulas que se alzan del borde de la hoja superior NORMAL
    al plano y se pliegan de vuelta, naranja→pálido en la cresta.
  · **3.8 ARCOS DE EINSTEIN** — filamentos pálidos arqueados por encima
    y por debajo a 1.8R: la lente gravitacional insinuada sin shaders.
  · **6.5 RIM VIOLETA** — el borde del horizonte RESPIRA: 18 cápsulas
    violetas a 1.045R latiendo a 1.7 rad/s (la última luz atrapada).

Validación: mock Python exacto (tools/mock_blackhole_v613.py, texturas
reales + modelo aditivo del juego) → VLM: chorros ✓, corrientes ✓, ondas
✓, aro violeta ✓, 8/10 "vivo y con personalidad" (primera ronda de alfas
subidas para jets/corrientes tras feedback VLM). En cielo diurno: núcleo
y vórtice perfectamente visibles; ondas recalibradas a magenta saturado.

### B. LAS ALAS — OCHO DISEÑOS NUEVOS, EL VOCABULARIO DE LAS CORONAS

Diagnóstico: las alas v6.12 usaban blobs radiales apilados sobre curvas
polares → "manchas difusas", no alas. ¿Qué hace que las CORONAS se vean
bien? Cuatro primitivas con identidad: EL TRAZO (cápsula estirada con
gradiente), LA PERLA (núcleo casi blanco + halo), EL DESTELLO DE 4 PUNTAS
(dos glows en cruz) y EL VOLUMEN OSCURO (silueta). **WingStrokes.cs**
(nuevo) las empaqueta + LA PLUMA (Bézier con volumen, trazo, nervio y
perla en la punta). Los 8 renderers REESCRITOS de cero:

  · **Horizonte de Sucesos** — 7 PLUMAS violeta→magenta→rosa naciendo de
    un MINI-HORIZONTE en el hombro (disco negro + anillo de fotones
    blanco), puntas dobladas al vacío, perlas de fotón + polvo.
  · **Anillo de Fotones** — DOS HUESOS gruesos con MEMBRANA violeta entre
    ellos (la superficie alar) + dos anillos elípticos con filo de ataque
    grueso (Doppler: el frente arde) y 4 fotones orbitando con estela.
  · **Mariposa Cósmica** — VITRAL: contorno dorado en cadena de trazos,
    venas como glifos, celdas de cristal violeta, ojo de ala con anillo.
  · **Hada de Polvo Estelar** — 4 PÉTALOS con contorno de dos trazos,
    3 venas internas, relleno ámbar translúcido y perlas titilantes.
  · **Corona Solar** — TRES LAZOS de prominencia (ArcCrown como alas):
    gradiente de temperatura rojo→oro, filamento eco, NUDO con DESTELLO
    DE 4 PUNTAS en cada ápice, brasas flotando.
  · **Nebulosa Viva** — ESQUELETO de 6 plumas maestras púrpura (la
    silueta) + nube de blobs en deriva + 5 ESTRELLAS con perla y CRUZ DE
    DIFRACCIÓN (Hubble) + filamentos fucsia serpentean.
  · **Eclipse Total** — plumas NEGRAS azul-noche casi opacas con puntas
    CROMOSFÉRICAS blanco-caliente (perlas + micro destellos), rayos de
    corona pálidos por detrás y mini disco de eclipse en el hombro.
  · **Cometa Carmesí** — TRES VELAS gordas de plasma (7.5px base) con
    MEMBRANA de sustentación entre ellas, onda de brillo viajando,
    CABEZAS con perla + destello 4 puntas, cola sensible a la velocidad.

Validación iterativa (tools/mock_wing_render_v613.py, AlphaBlend del pase
de jugador + cielo de día + silueta): RONDA 1: 6/10, 5/10 y 4/10 en
Horizonte/Nebulosa/Eclipse (trazos finos, "humo", "globos") → plumas más
corpulentas + esqueleto de plumas maestras + rediseño total de Eclipse.
RONDA 2: Anillo 6/10 y Cometa 5/10 ("halo", "jets") → huesos + filio de
ataque, velas + membrana. RONDA 3 FINAL: **8/8 APROBADAS** (Anillo 8,
Mariposa 10, Eclipse 9.5, Horizonte 9, Nebulosa 8.5, Hada 8, Corona 7.5,
Cometa pasa claro). Tooltips de 4 alas actualizados a los nuevos diseños.

### C. DOCUMENTACIÓN Y VERIFICACIÓN

  · build.txt 6.13; este CHANGES.md; research/wings_v613 + research/
    blackhole (mocks + validaciones VLM) al repo.
  · Compilación contra tModLoader 2026.07.3.0 REAL: **Build succeeded ·
    0 errores · 0 warnings**.

**Prueba del usuario**: git pull → Develop Mods → Build → (1) CrimsonBlackHoleStaff:
vórtice + esfera + chorros + corrientes cayendo + ondas + llamaradas +
aro violeta respirando; (2) las 8 alas nuevas en el inventario (plumas,
anillos con huesos, vitral, pétalos, prominencias, nebulosa con estrellas,
eclipse con puntas blancas, velas de cometa).

---

## Commit v6.12 — EL AGUJERO NEGRO SIN ERROR + TODAS LAS ALAS CON LA TÉCNICA DE LAS CORONAS

**Reporte del usuario** (con client.log): "el agujero negro dio error, y
las alas se ven horribles, intenta hacer las alas de la misma forma que
hiciste las coronas, usando la misma técnica".

### A. EL ERROR DEL AGUJERO NEGRO — UN `BeginAdditive()` PERDIDO

El client.log mostraba DOS `InvalidOperationException` silenciosas por
frame con el agujero en pantalla:
  · "Draw was called, but Begin has not yet been called" —
    `CrimsonBlackHoleRenderer.Cap()` línea 198 ← `Draw()` línea 235 (¡el
    primer quad del HALO!).
  · "End was called, but Begin has not yet been called" — el `End()`
    defensivo del catch.

**Causa raíz**: la reescritura v6.11 del renderer PERDIÓ la llamada a
`BeginAdditive()` al principio de `Draw()`. Las secciones 1-4 (halo, anillo
interior, las dos hojas del vórtice, hotspot) dibujaban cuadros sobre un
batch CERRADO → el primer `spriteBatch.Draw()` lanzaba, el catch lo tragaba
…y **NI EL VÓRTICE NI LA ESFERA NEGRA SE DIBUJABAN NUNCA**. El usuario solo
veía el hueco de la lente gravitacional ("es solo un agujero") Y los errores
en el log. El arte Oblivion calibrado v6.10/v6.11 era correcto — jamás se
mostró.

**Fix**: `BeginAdditive()` como sección 0 del try. El contrato de batch
queda: llega CERRADO → aditivo (1-4) → End → alpha (esfera negra) → End →
aditivo (rayos, chispas, bloom) → End CERRADO. Ambos caminos (PreDraw del
pase de mundo y RenderLens de la lente) funcionan; 0 excepciones.

### B. LAS ALAS — DE VUELTA A LA LUZ PROCEDURAL (petición expresa)

Los sprites de arte IA (v6.10/v6.11) no convencieron. El usuario pidió
expresamente la técnica de las coronas (la verificada perfecta): accesorio +
PlayerDrawLayer + renderizador VFX + textura de equipo en blanco. Se
RESTAURA el sistema completo v6.08 con la LECCIÓN DE VISIBILIDAD aprendida:

  · **WingVFX.cs** — WingDrawContext / WingMotionProfile / WingStyles /
    VFXWingSlots (8 estilos, personalidades de vuelo intactas: mariposa
    asimétrica 1.7, hada colibrí 0.52 con AlwaysFlutter, cometa sensible a
    la velocidad, muelles por estilo).
  · **WingAnimPlayer.cs** — máquina de estados (volar/planeo/caída/reposo)
    con muelles, golpe asimétrico y cadencia de sonido/dust.
  · **VFXWingsDrawLayer.cs** — UNA capa AfterParent(PlayerDrawLayers.Wings):
    las alas quedan DETRÁS del cuerpo como alas de verdad. Anclaje en los
    omóplatos (p.height·0.145, escala con el sprite).
  · Los 8 renderizadores (BlackHole/Butterfly/Fairy/SolarCorona/Nebula/
    Eclipse/Comet) con las 8 clases [AutoloadEquip] intactas (stats
    end-game, tooltips, recetas, entrega por TestingPlayer).

**LA LECCIÓN (por qué las v6.08 "no parecían alas")**: el pase de jugador
compone los DrawData con **AlphaBlend** (NO aditivo). Las alfas tenues del
v6.08 (0.085 en membranas, pensadas para aditivo) eran INVISIBLES — las alas
se leían como manchas. Las coronas leen bien porque usan alfas casi totales
(pulse·alpha ≈ 0.75-1.0). v6.12 aplica el estándar de corona a TODO:
  · Membranas 0.085 → **0.45-0.55** + VOLUMEN oscuro debajo (la silueta
    sólida que recorta la forma del ala contra el cielo — mariposa violeta
    noche, hada ámbar, eclipse noche, nebulosa púrpura).
  · Venas/filos/bordes 0.38 → **0.85-0.95**; núcleos a 1.0.
  · Envergaduras ×1.2-1.35 (escala de alas vanilla).
  · La luz del mundo ya NO apaga las alas: piso 0.88 (son fuentes de luz).

**REDISEÑOS ESTRUCTURALES tras validación VLM (mock AlphaBlend exacto con
las texturas reales, 24 escenas, cielo de día = peor caso)**:
  · **Anillo de Fotones** (3/10 → **9/10**): antes "campo de energía con
    forma de corazón". Ahora tiene FILO DE ATAQUE — una cinta dorada
    continua del hombro a la punta — y las órbitas son PLUMAS BARRIDAS
    alineadas al filo (3 elipses alargadas, cada una más lejos y más
    grande) con los fotones corriendo por ellas.
  · **Eclipse Total** (4/10 → **8/10**): antes "orbs sueltos junto a la
    cabeza". Ahora: los discos van AFUERA (lóbulo superior apenas arriba,
    lóbulo inferior abajo-afuera) + MEMBRANA de noche violeta que CONECTA
    raíz→ambos lóbulos (el cuerpo del ala) + filo cromosférico pálido.
  · **Nebulosa Viva**: los 6 blobs trazan el ARCO de un ala (exteriores
    más altos), 3 filamentos (antes 2).
  · **Corona Solar**: piso 0.52 en reposo — los lazos NUNCA colapsan a
    mancha.
  · Verificación final VLM: Mariposa 9 · Cometa 9 · Anillo de Fotones 9 ·
    Eclipse 8 · Hada 8 · Horizonte 7 · Corona Solar 7 · Nebulosa 6 (estilo
    nube, inherentemente etéreo).

**Los PNG de equipo** (los 8 `{Nombre}_Wings.png`) son ahora 8×8
TOTALMENTE transparentes (el truco de Calamity): vanilla no dibuja NADA —
ni sprite, ni caja, ni fondo, ni animación que arreglar. Los 122 PNG del
mod validados (ninguno corrupto — el "Image loading failed" del log viejo
era de la v6.10).

### C. VERIFICACIÓN

  · Compilación contra tModLoader v2026.07.3.0 REAL: **0 errores /
    0 warnings**.
  · mock_wing_render_v612.py: simulación AlphaBlend exacta (quads rotados,
    perfiles de SoftGlow/Ring/GlowOrb, lerp hacia el tinte) — 24 escenas
    validadas por VLM.

## Commit v6.11 — EL VÓRTICE INVISIBLE ARREGLADO + LAS ALAS CON EL CORTE VANILLA CORRECTO (4 frames)

**Reporte del usuario**: "el diseño de las alas se ve bien, pero están mal
animadas y programadas… además debes crearlas con fondo transparente. En
cuanto al agujero negro, es solo un agujero, no se parece en nada a la
imagen de referencia" (con dos capturas: las alas como 3 bandas con fondos
oscuros, y el agujero como un simple círculo negro con rayos).

### A. LAS ALAS — DOS BUGS REALES (ambos visibles en la captura)

**Bug A1 — el troceado**: v6.10 creyó (decompilando) que vanilla corta las
alas con `Height()/7`. ¡Era el caso especial de las alas 22/43/44! El
camino POR DEFECTO de `DrawPlayer_09_Wings` usa `num13 = 4` → las alas
moddeadas se cortan con **Height()/4** y origen **(Width/2, Height/8)** =
centro del frame. Las tiras de 7 frames cortadas en cuartos mostraban
FRAGMENTOS de 2-3 alas con huecos (exactamente las "3 bandas" de la
captura). La animación real (Player.cs decompilado): reposo = frame 0,
vuelo = ciclo 0→1→2 cada 5 ticks, planeo = frame 2 fijo; el frame 3 nunca
se usa en alas normales.

**Bug A2 — el fondo no transparente**: el pipeline v6.10 extraía el alfa
con un umbral de LUMINANCIA (7→42), pero los artes IA con fondo GRIS
oscuro (photonring (25,24,29), fairy (22,14,13), eclipse (14,15,20),
comet (21,6,9)) quedaban por encima → una CAJA RECTANGULAR semitransparente
cubría todo el frame (PhotonRingWings tenía el 79% del sprite opaco).

**Fix (tools/gen_ai_wings_v611.py)**:
  · **4 frames** (f0 plegada · f1 media · f2 apertura total · f3 copia de
    la f1) con la RAÍZ en el CENTRO del frame — el origen exacto de
    vanilla. Tiras 138-146 × 416-512.
  · **Fondo eliminado por CONECTIVIDAD**: color de fondo = mediana del
    borde; región de fondo = flood-fill desde los 4 bordes a través de
    píxeles con distancia de color < 58 (mata viñetas y gradientes);
    fuera de esa región alfa = smoothstep(dist, 22, 58) + cierre
    morfológico + relleno de huecos. Fondo 100% transparente (alfa dura:
    <0.03 → 0). PhotonRing baja del 79% al 55% de opacidad (solo alas).
  · **Aleteo sin clipping**: el margen se calcula con el ALCANCE REAL por
    píxel (reach = |dy|·sq·cos(ang) + |dx|·|sin(ang)|; ojo al abs() — el
    v6.11 inicial sin él subestimaba las poses de ángulo negativo) y el
    arte se escala hasta caber — las alas quedan GRANDES y las puntas
    nunca tocan el borde del frame (verificado 8/8).
  · **Rotación sobre la raíz de verdad**: el squash se aplica al
    contenido y el pivote vive en rootY·sq (el v6.10 rotaba alrededor de
    rootY sin squash — desplazaba el pivote). Mitades compuestas con
    máscara dura en la costura + fusión ponderada → la unión de la raíz
    es perfecta y las puntas divergen = el aleteo.
  · Iconos regenerados como PAR COMPLETO (~30×22, transparentes).
  · Simulación del render vanilla (frame centrado en el torso del
    jugador) validada con visión AI: 8/8 APROBADAS — anclaje, forma de
    ala, transparencia y progresión del aleteo.

### B. EL AGUJERO — EL VÓRTICE ERA INVISIBLE (14 píxeles magenta)

La captura del usuario mostraba SOLO la esfera negra + el halo tenue + el
lens + partículas: **el vórtice carmesí entero medía 14 píxeles magenta**
en la imagen. Tres bugs del renderer v6.10, todos en `Cap()`:

  · **TAMAÑO**: dibujaba SoftGlow con (len, wid) como TAMAÑO TOTAL del
    quad, pero esos números son las SIGMAS gaussianas del prototipo
    (add_blob es visible hasta ~1.5×sigma) y SoftGlow concentra su brillo
    en un núcleo diminuto (alfa 134 a r=6/32) → cada cápsula brillaba en
    2-3px. El disco negro sí se veía porque usa 2r explícito.
  · **ALFA²**: con texturas premultiplicadas + Additive(SourceAlpha, One)
    el color `c*alpha` aplicaba el alfa DOS veces (rgb·a·a) → aún más
    tenue.
  · **KeyLerp equiespaciado**: las tablas calibradas del prototipo usan
    t-claves explícitas (0.25, 0.45, 0.65…) — el v6.10 las leía como
    equiespaciadas y distorsionaba la geometría de las hojas.

**Fix (CrimsonBlackHoleRenderer.cs v6.11 + OblivionBlob.png nuevo)**:
  · Textura **OblivionBlob.png** (128×128): el perfil gaussiano EXACTO del
    prototipo horneado en el RGB (g = (exp(-3.6d²)+0.4·exp(-1.2d²))·
    (1-d²)²/1.4), alfa 255 en toda la textura → el premultiply de tML no
    la toca y el aditivo queda LINEAL.
  · `Cap()` dibuja el quad con TAMAÑO TOTAL = (2·sigma, 2·sigma): d =
    distancia/sigma reproduce add_blob píxel a píxel; el color lleva el
    brillo m=alfa·1.4 en el RGB con alfa 255 (el mismo clip del
    acumulador del prototipo).
  · `KeyLerp`/`KeyColor` con T-CLAVES explícitas — geometría idéntica al
    prototipo calibrado contra la referencia.
  · Simulación Python EXACTA del código C# (mock_renderer_v611.py:
    muestreo de la textura real, quads rotados, tinte clampeado,
    composición aditiva): validada con visión AI sobre fondo NEGRO
    (esfera + anillo + dos crescientes + aguja — EMA radial 32/255 vs
    27/255 del prototipo) y sobre CIELO AZUL (el vórtice brilla, la
    esfera sigue siendo negra pura, sin partes invisibles).
  · La física de juego, la lente gravitacional, las partículas y el
    contrato de batch (cerrado→cerrado, el fix del crash v6.10) quedan
    INTACTOS.

## Commit v6.10 — EL AGUJERO "OBLIVION" DE LA REFERENCIA REAL + TODAS LAS ALAS REHECHAS CON ARTE IA

**Reporte del usuario**: "el agujero negro sigue sin ser exacto y además dio
un error" (client.log con InvalidOperationException: Begin called before
End en CrimsonBlackHoleProjectile.RestoreSpriteBatch) + la imagen ORIGINAL
de la referencia (Reddit: Ancients Awakened — Regicide, Oblivion God of the
Void) + "también cambia todas las alas, esas alas no parecen alas, se ven
feas".

### A. EL ERROR DEL CLIENT.LOG — CAUSA RAÍZ Y FIX

El crash ocurría al final de la vida del agujero: la secuencia de
evaporación componía `scale *= 1-collapse` hacia ~0 → `rSh < 2` → el
renderer v6.09 hacía EARLY RETURN sin cerrar el batch → RestoreSpriteBatch
hacía un `Begin()` DUPLICADO sobre un batch ya activo → FNA lanza
InvalidOperationException y tML desactiva el dibujado del proyectil.

**Fix a prueba de balas (PreDraw v6.10)**: cerramos el batch del pase de
proyectiles nosotros (`try End catch` — si ya estaba cerrado por el hook de
otro mod, lo respetamos), dibujamos con el renderer (que exige batch
CERRADO y lo deja CERRADO pase lo que pase con try/catch defensivo) y
restauramos con los parámetros EXACTOS de `Main.DrawProjectiles`
(decompilado de tML 2026.07.3.0: Deferred, AlphaBlend, DefaultSamplerState,
None, **Main.Rasterizer**, null, **Main.Transform**). Además la escala del
colapso ahora tiene piso 0.06 (esfera mínima de 5.5px).

### B. EL AGUJERO "OBLIVION" — LA REFERENCIA ORIGINAL POR FIN ENTENDIDA

La imagen de Reddit NO es un Gargantua de disco delgado (lo que v6.09
calibró): es un VÓRTICE DE PLASMA. Mediciones numpy píxel-exactas (esfera
R=35px en (493,223), ajuste circular 84%):
  · Esfera negra compacta + GAP oscuro 1.0–1.25R (el brillo NO la toca).
  · Anillo interior 360° a ~1.5R con borde interno blanco-caliente
    (hotspot medido: (255,246,137) a 355°, 3R).
  · UNA HOJA GRUESA EN CRESCIENTE que barre POR ARRIBA (O→NO→N→NNE):
    se mantiene a ~3R hasta 330° y SE DISPARA en aguja hasta 6.1R a 345°.
  · Segundo cresiente BAJO (ESE→S→SSW) con borde exterior a 6.3R por el
    sur (verificado: la masa lejana SE ES plasma rosa, no el cuello
    plateado del jefe — este último se distinguió por color r≈g).
  · TODO inclinado SW→NE (~24°) y fluyendo en sentido HORARIO.
  · Rayos azul-violeta RAMIFICADOS dentro de la esfera (no cruzan el
    centro), chispas blanco-amarillas, mechones hasta 6.5R.

**CrimsonBlackHoleRenderer.cs v6.10**: reescrito completo — 8 capas (halo
→ anillo+rim caliente → hoja superior → cresiente inferior → hotspot/nudo/
aguja/mechones → ESFERA NEGRA opaca (garantiza el gap) → rayos ramificados
→ chispas+bloom), todo en unidades de R=46px (GIGANTE autorizado:
envergadura ~580px), girando en sentido horario a 0.16 rad/s, con
turbulencia de pinceladas, filamentos calientes dentro de las hojas, colas
de velocidad y jitter de borde. BlackHolePhysics.cs ELIMINADO (la nueva
geometría es art-directed, no GR; queda en git history). Prototipo Python
calibrado (tools/mock_oblivion_v610.py): perfil radial EMA 27/255 y
extensión angular por cuadrantes emparejada (aguja NNE 6.0R vs 6.1R, sur
5.7R vs 6.4R, oeste 3.2R vs 2.3R).

### C. LAS 8 ALAS — ARTE IA REFINADO, SISTEMA VANILLA

Las alas de luz procedural "no parecían alas". Ahora son SPRITES de verdad:
  1. 8 diseños generados con IA (temas: horizonte de sucesos, anillo de
     fotones, mariposa cósmica, hada estelar, corona solar, nebulosa viva,
     eclipse total, cometa carmesí).
  2. Pipeline tools/gen_ai_wings_v610.py: alfa desde fondo negro (umbral
     suave + cierre morfológico para no agujerear plumas oscuras) →
     SIMETRÍA PERFECTA por espejo (verificada numéricamente: diff=0.0000)
     → contorno oscuro Terraria → limpieza de píxeles sueltos → supresión
     del cuerpo central (mariposa/hada/eclipse/solar) → ANIMACIÓN de 7
     frames (cada mitad rota sobre la RAÍZ: reposo plegado, planeo, apex
     alzado, ciclo) → tira {Nombre}_Wings.png + icono.
  3. **DESCUBRIMIENTO al decompilar DrawPlayer_09_Wings**: vanilla corta
     las alas en **Height()/7 — ¡SIETE frames, no 4!** (origen (W/2,
     H/14)). v6.06 usó 4 frames: por eso las alas salían "mal ubicadas".
     El pipeline genera las 7 correctamente.
  4. AethonWingItems.cs (antes VFXWingItems.cs): base estándar con
     [AutoloadEquip(EquipType.Wings)] + WingStats (stats end-game
     conservadas: 180-200 ticks, 9-10.5 velocidad, ×2.6-3.2, FLOTADO en
     mariposa/hada). BORRADOS: WingVFX.cs, los 7 renderers VFX de alas,
     VFXWingsDrawLayer.cs, WingAnimPlayer.cs, VFXWingItems.cs — el
     sistema vanilla hace toda la animación (frame 0 reposo, 1 planeo,
     2 apex, ciclo 0-2 al volar).
  5. Validación VLM: 8/8 alas con silueta legible de alas reales,
     animación reposo≠apex visible, simetría numérica perfecta.

## Commit v6.09 — EL AGUJERO NEGRO CARMESÍ "SUPER IGUAL": LA OTRA LIBRERÍA CON LAS FÍSICAS CORRECTAS

**Reporte del usuario**: "todavía no se parece a la referencia, el disco de
acreción debe ser más denso y debe rodear por completo a la bola negra, esta
debe ser de un negro profundo con bordes de color. Investiga más sobre
agujeros negros, investiga las matemáticas de cómo crear un agujero negro,
crea otra librería de ser necesario con las físicas correctas, que el agujero
negro sea igual a la referencia — y cuando digo igual es que sea igual SUPER
IGUAL; asegúrate de que sea igual que la referencia, si tiene que ser
gigante para eso que así sea".

### A. LA INVESTIGACIÓN (lo que faltaba entender)

El shader de marcha de rayos produce un TORO lensado genérico — por eso
"todavía no se parecía": la referencia no es un toro, es un DISCO DELGADO
INCLINADO visto a ~70° con la geometría de oclusión clásica (Luminet 1979;
James et al. 2015, el paper de DNGR/Interstellar): el lado CERCANO cruza por
delante de la cara inferior de la esfera, el lado LEJANO se oculta detrás,
y el anillo de fotones + su eco lensado abrazan la silueta. ESO es "rodear
por completo a la bola negra".

Mediciones píxel-exactas de la referencia (347×173, R_sh≈14px): banda de
fotones sólida 1.16–1.49·R_sh (blanco 255), eco lensado 1.68–1.96, foso
oscuro 2.1–2.9 (el lado lejano nace a 2.7), borde interno CALIENTE a 2.2
que cruza la esfera a +0.76·R_sh bajo el centro, pico del disco a 4.6
(magenta saturado), fade exterior 6.5, elipse b/a=0.345, Doppler izquierdo
+30%, 18 rayos cian de fondo.

### B. LA OTRA LIBRERÍA (2 archivos nuevos)

- **`Content/VFX/BlackHolePhysics.cs`** — la matemática GR pura en unidades
  r_s=1: horizonte, esfera de fotones (1.5 r_s), sombra (√27/2 ≈ 2.598 r_s),
  ISCO (3 r_s), velocidad kepleriana v=√(GM/r), ω∝r^(−3/2), Doppler δ y
  δ³, Shakura–Sunyaev T∝r^(−3/4) e I∝r^(−3), redshift g=√(1−r_s/r), y la
  proyección del disco inclinado con el test cercano/lejano (la clave).
- **`Content/VFX/CrimsonBlackHoleRenderer.cs`** — el render ANALÍTICO POR
  CAPAS con esa geometría: (1) halo ambiental + 18 rayos cian radiales,
  (2) lado LEJANO del disco (comprimido, nace a 2.7·R_sh), (3) halo de
  fotones, (4) **ESFERA NEGRA OPURA** (BlackDisk.png nuevo: negro profundo
  #000000 que COME la luz — oculta el disco lejano dentro de su silueta),
  (5) **ANILLOS DE FOTONES** = el borde de color (13 rungs solapados
  1.16–1.96·R_sh + arco de eco sobre la esfera), (6) **LADO CERCANO que
  CRUZA POR DELANTE** de la cara inferior de la esfera con su borde blanco
  caliente a 2.2·R_sh, (7) bloom (arco cercano + hotspot Doppler izquierdo).
- **`Content/Effects/Procedural/BlackDisk.png`** — textura nueva (256px,
  núcleo opaco + borde de 4px).

### C. EL DISCO DENSO QUE RODEA (la petición textual)

32 líneas de corriente keplerianas dibujadas como CÁPSULAS SoftGlow
solapadas ×2.2 (tangente a la elipse) → banda CONTINUA y DENSA, no un
donut: brillo pico a 4.6·R_sh (magenta 255,105,255), carmesí en los bordes,
grano de plasma orbitando con ω∝r^(−3/2) (el interior hierve más rápido),
Doppler δ suavizado (izquierda cegadora), lado cercano aclarado a
rosa-blanco en su núcleo, lado lejano comprimido al 72% con su borde
blanqueado. La calibración es 1:1 con el prototipo Python
(`tools/mock_blackhole_v609.py`) que usa las texturas REALES del mod y fue
validado numéricamente contra la referencia (error medio 17/255 en el
perfil radial de 23 puntos — el pico del mock 212 vs ref 207).

### D. ESCALA GIGANTE + INTEGRACIÓN

- **GIGANTE autorizado**: sombra de 38px de radio (esfera de 76px), disco
  de 494px de envergadura a escala 1 — domina la pantalla como la
  referencia domina su encuadre. El aura de daño sube 2.2× → 4.6× sobre la
  sombra para abrazar la mitad interior del disco (mult. del escudo).
- **Lente de pantalla**: su radio de distorsión ahora usa el multiplicador
  propio del carmesí (2.9× en vez de 1.4×) para ABRAZAR el disco completo.
- Partículas (dusts + librería) re-escaladas a la banda del disco nuevo
  (2.3–5.8·R_sh, elipse 0.345, ω kepleriano); iluminación en 3 puntos;
  tooltips del staff actualizados.
- **Física de juego INTACTA** (copia exacta): atracción 10× el sol, aura de
  ticks acelerados, devora balas, persecución lenta, anillo de Einstein
  final. El BlackHoleProjectile ORIGINAL queda INTACTO con su shader.
- Verificación compilada contra tModLoader v2026.07.3.0 REAL: **0 errores,
  0 warnings**. Confirmado decompilando que tML premultiplica las texturas
  (ReLogic PngReader → PreMultiplyAlpha): el blending aditivo del renderer
  replica exactamente el modelo del prototipo validado.

**Prueba del usuario**: Develop Mods → Build → CrimsonBlackHoleStaff →
disparar: la bola negra profunda con su anillo blanco, el disco magenta
denso rodeándola por completo (cruce frontal abajo), los rayos cian y la
lente curvando el fondo. (Las 8 alas de la v6.08 siguen pendientes de
prueba en juego.)

## Commit v6.08 — TODAS LAS ALAS SON AHORA DE LUZ (8, técnica coronas) + EL AGUJERO NEGRO CON EL HORIENTE PEQUEÑO Y EL DISCO ALARGADO

**Reporte del usuario**: "el agujero negro se ve bastante bien, pero es igual
al original solo con otro color... el horizonte de eventos debe ser mas
pequeño, y el disco de acreción mas alargado" + "todas las alas se ven mal y
las 8 alas con sprite estan mal ubicadas... crea alas de mariposa y alas de
hadas, y a partir de ahora que todas las alas sean con el mismo estilo que
las coronas; el resto de alas con sprite borralas; rediseña todas y mejora
sus animaciones, tomate tu tiempo, crea mas alas como las 2 especiales".

### A. EL AGUJERO NEGRO CARMESÍ (recalibrado con mediciones píxel-exactas)

Análisis de las 2 referencias del usuario (numpy + relleno de huecos): la
sombra es un núcleo COMPACTO (84×64 px, ~20% del rastro total) con el anillo
de fotones ABRAZÁNDOLO (1.1-1.3× la sombra) y el disco como un RASTRO LARGO
Y FINO que llega a ~5× la sombra y cruza en DIAGONAL (lado que se acerca
abajo-izquierda, el que se aleja arriba-derecha). El render anterior fallaba
en las proporciones: el refuerzo negro del C# inflaba la sombra a 2.15× el
horizonte y el toro era un donut gordo pegado al horizonte.

- **Horizonte más pequeño**: `blackHoleRadius` 0.25 → **0.17** + refuerzo
  negro 2.15× → **1.45×** (alpha 235, núcleo compacto sólido ≈20% del
  rastro, como la referencia). El anillo de fotones overlay queda a 1.18×
  del núcleo visible — abrazándolo, como en la referencia.
- **Disco más alargado**: `accretionDiskScale` = (1, 0.28, 1) →
  **(1.15, 0.17, 1)**: estirón horizontal ×1.15 (el rastro de la diagonal)
  + achatado vertical 0.17 (banda de canto ~6:1). El tubo baja 0.48 → 0.36:
  el annulus nace a 2.3× el horizonte (lejos, como el rastro) y el borde
  exterior queda en (0.75+0.36)×1.15 = 1.28 unidades ≈ IGUAL que antes —
  **el agujero MANTIENE su tamaño total** (petición explícita).
- **Doppler diagonal**: los velos se inclinan a favor de la diagonal
  (acercándose abajo-izquierda cegador / alejándose arriba-derecha brasa) y
  se estiran siguiendo el rastro.
- Partículas del disco recalibradas al annulus nuevo (2.3..5.5× el
  horizonte compacto). La lente de pantalla (BlackHoleLensSystem) no
  cambia: se dimensiona por el hitbox (p.width), no por el shader — sigue
  abrazando el borde del disco. Física 100% intacta.

### B. LAS ALAS: DE 10 MIXTAS A 8 DE LUZ (sistema unificado)

**BORRADO**: las 8 alas de spritesheet (clases SheetWings.cs + AethonWings.cs
+ las 16 PNG de ítems y tiras de frames + sus entradas de localización + la
garantía de entrega). El usuario las vio "mal ubicadas" (el anclaje vanilla
de la tira quedaba 8-12 px por debajo de los omóplatos — medido en su
captura) y pidió eliminarlas.

**SISTEMA NUEVO (todo técnica coronas — PNG en blanco + render VFX):**

- `WingVFX.cs` — el núcleo: `WingDrawContext` (anclaje, apertura, aleteo,
  dirección, gravedad, luz, VELOCIDADES para el sweep aerodinámico y el
  diedro), `WingMotionProfile` (la personalidad de vuelo de cada estilo) y
  `WingStyles` (registro estilo→renderizador, 8 estilos).
- `VFXCore.Quad` con rotación + textura (NUEVO): cintas de luz orientadas
  por la tangente — la base de los rastros, colas y lazos.
- `VFXWingsDrawLayer` — UNA capa para las 8 (antes había 2), después de la
  capa vanilla de alas, anclada a la ESPALDA ALTA (omóplatos, -6px).
- `WingAnimPlayer` REESCRITO: muelles por estilo, **golpe asimétrico**
  (la mariposa baja el ala rápido y sube lento — vuelo real),
  **AlwaysFlutter** (el hada y la nebulosa nunca dejan de latir), cadencia
  de sonido por estilo (el hada suena cada 2 ciclos porque aletea ~10×/s).

**LAS 8 ALAS** (todas end-game ≥ Solar Wings, mariposa y hada con FLOTADO):

1. **Horizonte de Sucesos** (rediseñada) — rastro de acreción ALARGADO por
   lado (cinta por tangente con Doppler δ³ y vetas de plasma), mini
   horizonte con anillo de fotones a 1.18×, cuentas de materia orbitando y
   eco de anillo de Einstein en la punta.
2. **Anillo de Fotones** (rediseñada) — 3 aros de órbita elípticos en
   abanico con FOTONES corriendo por ellos (con estelas) y un pulso que
   recorre el aro mayor con cada golpe de aleteo.
3. **Mariposa Cósmica** (NUEVA) — lobo superior grande con ojo de ala +
   lobo inferior caído (fase de aleteo independiente), membrana translúcida
   de retícula, 5 venas curvas, borde dorado festoneado, golpe asimétrico
   real y las alas casi aplaudiendo sobre la espalda al subir.
4. **Hada de Polvo Estelar** (NUEVA) — 4 lóbulos puntiagudos de membrana
   dorada con borde ámbar, 7 chispas de polvo estelar titilando con fases
   deterministas y vibración de colibrí que NUNCA cesa.
5. **Corona Solar** (NUEVA) — 4 lazos de prominencia por lado con gradiente
   de temperatura (blanco → dorado → naranja → braza), puntas en
   llamaradas, mancha solar en la raíz y estirón al empujar.
6. **Nebulosa Viva** (NUEVA) — 6 blobs de gas en deriva turbulenta
   individual, filamentos serpenteantes y estrellas con cruces de
   difracción; la nube RESPIRA en lugar de aletear.
7. **Eclipse Total** (NUEVA) — discos negros sólidos con anillo
   cromosférico exacto en el borde, 6 rayos de corona DESIGUALES ondeando
   con fases propias y llamarada rosa en el limbo.
8. **Cometa Carmesí** (NUEVA) — núcleo blanco-dorado + cola iónica cónica
   con onda viajera, gradiente blanco→dorado→carmesí→braza, vetas de plasma
   y motas de polvo; la cola se BARRRE al correr (máxima respuesta a la
   velocidad).

**Ítems**: `VFXWingItems.cs` (base + las 8 clases con [AutoloadEquip]),
stats 180-200 ticks / velocidad 9-10.5 / aceleración ×2.6-3.2, flotado en
mariposa y hada, tooltips de color estilo coronas, recetas de madera,
entrega garantizada en TestingPlayer, localización es-ES + en-US.

**Iconos**: 8 iconos procedurales de 30×24 (supersampling ×4, composición
aditiva sobre transparente) con la silueta de cada estilo — generados por
`research/wings/gen_vfx_wing_icons.py` y validados por VLM (7-10/10; el de
nebulosa reforzado tras el feedback: núcleo denso + estrellas ancla).

### C. VERIFICACIÓN

- **Compilación completa contra tModLoader.dll REAL v2026.07.3.0:
  0 errores, 0 warnings.**
- **Simulación Python de los renderizadores** (`research/wings/
  mock_wing_render.py`): réplica exacta de la matemática de mariposa, hada,
  anillo de fotones, horizonte y cometa en 11 escenas (volando/reposo/golpe)
  validada por VLM — anclaje al hombro ✓, extensión arriba/afuera ✓, formas
  reconocibles ✓, simetría ✓.
- Validación PIL de las 16 PNGs nuevas: 8 iconos 30×24 RGBA + 8 texturas de
  equipo 8×8 RGBA totalmente transparentes (truco Calamity, el mismo de las
  coronas que ya funcionaba).
- Firma de `EquipLoader.GetEquipSlot(Mod, string, EquipType)` verificada
  decompilando el tML real (la usan el animador, la capa y el mapeador de
  slots).

### D. PRUEBA DEL USUARIO

Develop Mods → Build → disparar CrimsonBlackHoleStaff (núcleo negro
compacto + anillo pegado + RASTRO largo fino en diagonal + partículas
barriendo el annulus) → entrar al mundo → las 8 alas en el inventario →
equipar cada una: mariposa (golpe lento profundo + flotado), hada
(vibración rápida + flotado), cometa (la cola barre al correr), eclipse
(majestuosa), etc. — y las de agujero negro con el rastro y los fotones.

---

## Commit v6.07 — FIX: el mod no cargaba (IndexOutOfRangeException en las 8 alas de spritesheet)

**Reporte del usuario**: "estaba comenzando las pruebas y está lleno de
errores" (client.log de tModLoader 2026.07.3.0).

### A. EL ERROR (del log real)

```
System.IndexOutOfRangeException: Index was outside the bounds of the array.
   at AethonMod.Content.Items.Wings.AethonWings.SetStaticDefaults() in AethonWings.cs:line 45
   at Terraria.ModLoader.ModItem.SetupContent()
```

El error se repetía **8 veces** (una por cada ala de spritesheet: Nova Solar,
Plasma Cuántico, Vacío Etéreo, Éter Glacial, Fósiles del Génesis, Pilar de
Nebulosa, Eclipse y Supernova) y desactivaba el mod al cargar. Las 2 alas
"técnica coronas" (Horizonte de Sucesos y Anillo de Fotones) SÍ pasaban.

### B. LA CAUSA RAÍZ

Las 8 clases de `SheetWings.cs` **no llevaban el atributo
`[AutoloadEquip(EquipType.Wings)]`**. Sin ese atributo, tModLoader nunca
reserva el slot de equipo de alas → `Item.wingSlot` queda en `-1` → la línea
`ArmorIDs.Wing.Sets.Stats[Item.wingSlot]` indexa fuera del array y revienta
el SetupContent de TODO el mod. Las 2 alas de coronas sí lo tenían (por eso
pasaban): la diferencia entre los dos grupos lo confirmó al 100%.

**Por qué no se detectó antes**: la compilación C# pasa perfecto — el
atributo es metadata de autoload, no código. Solo revienta en runtime, en la
fase "Configurando contenido" del arranque del juego.

### C. EL FIX

- `[AutoloadEquip(EquipType.Wings)]` añadido a las 8 clases de
  `SheetWings.cs`. Con el atributo, tML reserva el slot leyendo la textura
  `Nombre_Wings.png` (las 8 existen y son válidas: RGBA, altura múltiplo de
  4 para la tira de 4 frames) y `Item.wingSlot` llega con valor real a
  `SetStaticDefaults`.
- Nada más tocado: stats, dusts, tooltips, recetas, localización, entrega al
  jugador y las 2 alas procedurales quedaron igual (ya eran correctas).

### D. VERIFICACIÓN

- **Compilación completa del mod contra tModLoader.dll REAL v2026.07.3.0**
  (el mismo release que usa el usuario, descargado de GitHub releases):
  **0 errores, 0 warnings** (entorno de verificación del sandbox reconstruido
  desde cero: .NET 8 SDK + tModLoader.zip 2026.07.3.0 + referencias
  tModLoader/FNA/ReLogic/TerrariaHooks/Steamworks.NET).
- Validación PIL de las 10 texturas de equipo `_Wings.png`: todas PNG RGBA
  válidas, alturas múltiplo de 4 (tira de 4 frames ✓), y las 2 de coronas
  totalmente transparentes (8×8, el truco Calamity para que el dibujo vanilla
  no pinte nada) ✓.
- Diagnóstico diferencial del log: 8 fallos = exactamente las 8 clases sin
  atributo; las 2 con atributo pasaron su `SetStaticDefaults` — evidencia
  concluyente de la causa.

### E. NOTA SOBRE EL AVISO DEL ICONO

El log del usuario contiene un aviso NO fatal durante el empaquetado:
`[FNA]: Image loading failed: unknown image type` (aparece en la fase
"Empaquetando: AethonMod"). El `icon.png` (80×80 RGBA) y `icon_small.png`
(30×30 RGBA) del repo son válidos; si vuelve a aparecer, restaurarlos con
`git checkout -- icon.png icon_small.png`. El empaquetado terminó bien y no
afecta al juego.

---

## Commit v6.06 — LAS 10 ALAS DE PRUEBA END-GAME (2 con técnica de las coronas + 8 con spritesheet procedural)

**Petición del usuario**: "investiga como funcionan las alas en terraria,
como es su animación y su reacción frente a las acciones del jugador, como
volar, saltar, caer, o estar en reposo, investiga el código de su
funcionamiento y crea 7 alas, quiero que para al menos 2 pares de alas uses
la misma técnica que usaste para crear las coronas y estas alas deben ser
con la temática del agujero negro nuevo" (+ "dame las 7 propuestas y crea
otras que tu creas conveniente no te limites solo a las 7" + "De momento
son todas de pruebas, así que sean alas de end-game" + "se las tienes que
dar al jugador, pues son de pruebas").

### A. INVESTIGACIÓN (código real de tModLoader en GitHub)

- **`ExampleCustomDrawWings.cs`** del ExampleMod oficial: hook `WingUpdate`
  (control total de frames/dusts/sonidos), `ModifyEquipTextureDraw`,
  `VerticalWingSpeeds` (el planeo), `ArmorIDs.Wing.Sets.Stats[Item.wingSlot]
  = new WingStats(tiempo, velocidad, aceleración)`.
- **Patch de `Player.cs`** (lógica vanilla extraída): animación vanilla =
  frame 0 en reposo · ciclo 1→2→3 cada 4 ticks al volar · frame 2 al caer ·
  frame 1 al planear · frame 0 flotando en agua; sonido de aleteo
  (SoundID.Item32) por ciclo; `ShouldDrawWingsThatAreAlwaysAnimated()`.
- **Calamity `WingsofRebirth` + `WingsofRebirthLayer`**: EL PATRÓN para las
  alas "técnica coronas" — textura de equipo EN BLANCO + `PlayerDrawLayer`
  (`AfterParent(PlayerDrawLayers.Wings)`) + visibilidad por
  `drawPlayer.wings == EquipLoader.GetEquipSlot(...)`.
- **Formato del spritesheet** validado contra la imagen de referencia del
  usuario (5 alas × 4 estados): tira vertical de 4 frames.

### B. LAS 2 ALAS "TÉCNICA CORONAS" (temática agujero negro carmesí)

PNG de equipo EN BLANCO (como Calamity) + TODO el dibujado por la
biblioteca VFX + animación PROCEDURAL por MUELLES (sin frames):

1. **Alas del Horizonte de Sucesos** (200 ticks · 9.5 · ×3): por lado, un
   mini horizonte de sucesos negro con anillo de fotones + TRES ARCOS DE
   ACRECIÓN anidados (paleta VoidQueen del agujero) + DOPPLER δ³ (el lado
   que avanza arde más) + CUENTAS DE MATERIA orbitando al volar. Se pliegan
   en reposo, se despliegan al caer, aleteo de onda continua al volar.
2. **Alas del Anillo de Fotones** (200 · 9 · ×3.2): por lado, una
   micro-singularidad + CINCO HOJAS DE LUZ curvadas que se comprimen en
   reposo y abren en abanico al volar + PULSOS DE FOTONES viajando hacia
   las puntas + mini-anillos de Einstein rotando en las puntas.

Implementación: `WingAnimPlayer` (ModPlayer: muelle de apertura con
overshoot orgánico, fase de aleteo, dusts y sonido solo si funcionales) +
`EventHorizonWingRenderer` / `PhotonRingWingRenderer` (VFX) +
`EventHorizonWingsDrawLayer` / `PhotonRingWingsDrawLayer`
(`AfterParent(PlayerDrawLayers.Wings)` → `VFXCore.AppendToPlayerDraw`, el
camino de las coronas) + iluminación del mundo muestreada (arden de noche,
se integran de día) + gravedad invertida respetada.

### C. LAS 8 ALAS CON SPRITESHEET PROCEDURAL (flujo vanilla 100%)

`research/wings/gen_wings.py` — motor de arte con 7 ESTILOS (emplumadas /
membrana / llama / cristal / nebulosa / eclipse / nova), supersampleado 4×,
outline estilo Terraria, sombreado superior, simetría espejo TOTAL (misma
semilla rng por lado) y AUTO-ENCAJE por recorte (imposible cortarse;
alturas múltiplo de 4 por `texture.Frame(1,4)`). Dos rondas de validación
VLM (8.8/8.4/8.0/7.0/7.0/8.0/7.4/8.4 → ajustes → **8/8 APROBADAS**):

| Alas | Estilo | Vuelo |
|---|---|---|
| Nova Solar | lenguadas de plasma blanco→dorado→rojo | 190 · 9.5 |
| Plasma Cuántico | shards cian facetados + destellos | 180 · **10** (récord) |
| Vacío Etéreo | membrana púrpura + venas fucsia + estrellas | 180 · 9 |
| Éter Glacial | plumas celestes + carámbanos (planeo lento) | 180 · 8.5 |
| Fósiles del Génesis | hueso + vetas ámbar + sedimento | 180 · 9 |
| Pilar de Nebulosa | cuerpo magenta translúcido + borde cian | 185 · 9 |
| Eclipse | discos negros + rayos de corona oro | 190 · 9.2 |
| Supernova | plumas de choque + anillos de onda | 185 · 9.4 |

Clase base `AethonWings` (stats por `WingStats`, `VerticalWingSpeeds` con
personalidad, dusts de vuelo por ala — todos DustIDs ya usados por el mod),
subclases finas en `SheetWings.cs`, tooltips de color tipo coronas.

### D. ENTREGA Y LOCALIZACIÓN

- `TestingPlayer`: las 10 alas garantizadas en el inventario en cada
  entrada al mundo (patrón `EnsureItem` de las armas cósmicas).
- Recetas de madera (5) para recuperarlas si se pierden (como las coronas).
- Localización es-ES + en-US (DisplayName + Tooltip de las 10).
- build.txt → 6.06.

### E. VERIFICACIÓN

Compilado contra tModLoader **v2025.06.3.0 real** (.NET 8 SDK + dlls del
release de GitHub en /tmp/verify; referencia `tModLoader.dll` +
`TerrariaHooks.dll` + FNA + ReLogic): **Build succeeded · 0 errores ·
0 warnings**. Nota de arquitectura descubierta: desde las versiones 2025
la API de mods vive en `tModLoader.dll` (raíz del zip), NO en
`TerrariaHooks.dll` (que solo es la vanilla con ganchos).

## Commit v6.05 — EL AGUJERO NEGRO CARMESÍ: COPIA EXACTA + PARÁMETROS

**Petición del usuario**: "primero toma una copia exacta del agujero negro
funcional que tenemos, y a partir de ahí modifica sus parámetros, disco de
acreción mas grande y de otro color, el agujero un poco mas pequeño, mejorar
la animación, etc. además te pido que investigues en internet como se crean
matematicamente un agujero negro, investiga formulas e investiga la
estructura de nuestro agujero negro funcional, luego replica el agujero
negro de la referencia" (+ "haz lo mejor posible con todo lo aprendido y
todos los recursos que puedas conseguir de cualquier lugar de Internet").

### A. EL MÉTODO (radicalmente distinto al v6.04)

El v6.04 (render Gargantua con texturas PNG pre-generadas) se veía horrible:
SE ELIMINÓ COMPLETO (GargantuaRenderer.cs + GargantuaBack/Front/Shadow.png).
El v6.05 es EXACTAMENTE lo que pidió el usuario: **el MISMO render del
agujero funcional** (RealBlackHoleShader — marcha de luz de 75 pasos con
lensing gravitacional real, dibujado sobre el lienzo InvisiblePixel, mismo
halo, mismo refuerzo del horizonte, misma lente de pantalla) con los
**PARÁMETROS recalibrados** — sin recompilar el .fxc (los parámetros del
shader se establecen por nombre desde C# en cada frame).

### B. INVESTIGACIÓN MATEMÁTICA (internet)

Documentada en `research/blackhole/MATEMATICA_AGUJEROS_NEGROS.md`:
r_s = 2GM/c² (Schwarzschild), esfera de fotones 1.5·r_s, sombra aparente
(√27/2)·r_s ≈ 2.6·r_s, ISCO = 3·r_s (borde interno del disco), Kepler
v = √(GM/r) → 0.41c en el ISCO, Doppler beaming δ = 1/(γ(1−β·cosθ)) con
brillo ~δ³ (contraste ~13× entre lados), lente α = 4GM/(c²·b), Shakura–Sunyaev
T(r) ∝ r^(−3/4) (núcleo caliente blanco → borde rojo), y el paper de
Interstellar (James et al. 2015) para el Gargantua.

### C. PARÁMETROS: FUNCIONAL → CARMESÍ

| Parámetro | Funcional | Carmesí v6.05 | Por qué |
|---|---|---|---|
| `blackHoleRadius` | 0.30 | **0.25** | "el agujero un poco más pequeño" |
| `accretionDiskRadius` (tubo del toro) | 0.40 | **0.48** | "disco más grande": borde ext. 3.8×→4.9× la sombra; borde interno ~0.27 queda pegado a la sombra (≈ISCO) |
| `accretionDiskColor` | (245,105,61) | **(255,45,100)** | "otro color": paleta de la referencia (blanco-rosado→magenta→carmesí) |
| `accretionDiskScale.y` | 0.33 | **0.28** | banda fina casi de canto (referencia ~15-20°) |
| `cameraAngle` | 0.32 | **0.30** | inclinación ~17° como la referencia |
| `globalTime` | t | **t×1.35** | "mejorar la animación": el plasma HIERVE más vivo |
| `cameraRotationAxis` | fijo | **+ precesión ±0.05/±0.06 rad (2 frecuencias incommensurables)** | el plano del disco bambolea orgánico |
| lienzo | 256·escala | **256·escala·1.10·(respiración ±1.8%)** | disco +10% en pantalla; el conjunto respira |
| Doppler beaming | — | **velos aditivos: izq. blanco-rosado (δ³), der. carmesí tenue** | física real + la referencia (lado izquierdo brillante) |
| Anillo de fotones | interno del shader | **+ refuerzo Ring rosa pálido pulsante a 1.7·r_h** | firma visual de la referencia |

La FÍSICA DE JUEGO queda COPIA EXACTA del funcional (pop elástico,
crecimiento→evaporación→anillo de Einstein, aura con ticks que aceleran
cerca del centro, atracción 2.6 (10× el sol), devora balas enemigas,
persecución lenta). El BlackHoleProjectile original queda INTACTO.

### D. RESPUESTA AL USUARIO: ¿assets o código?

El agujero funcional es **~100% código**: el visual es el shader
RealBlackHoleShader.fxc (marcha de luz de 75 pasos) sobre un píxel
transparente escalado (InvisiblePixel.png) + ruido FireNoiseB + dusts
vanilla + la lente de pantalla (otro shader). No hay "assets" del agujero
como tal — por eso el usuario no los encontraba. (Sección 6 del doc de
investigación.)

## Commit v6.04 — EL AGUJERO NEGRO EXACTO: EL RENDER GARGANTUA

**Petición del usuario**: "el agujero negro no se parece en nada... te
mostraré el agujero negro de terraria que hiciste primera imagen y la
referencia segunda imagen. usa todo el conocimiento que tienes, y todas las
librerías disponibles para crear una copia exacta del agujero negro de la
segunda imagen, no olvides que tenemos un agujero negro funcional puedes
usar una copia como base para eso" (las coronas ya estaban perfectas).

### A. ANÁLISIS MÉTRICO DE LA REFERENCIA (píxel-exacto)

Análisis VLM doble (imagen completa + recortes) + medición píxel a píxel
con Python (numpy) del recorte del agujero. La estructura REAL de la
referencia (el brillo del lado derecho de la imagen es un PERSONAJE en
primer plano, NO parte del agujero — no se replica):

- SOMBRA negra central: Ø~95px sobre estructura de ~335px → 28% del ancho,
  borde NÍTIDO, hueco redondeado.
- DISCO de acreción COMPACTO: extensión total ~3.5× el diámetro de la
  sombra (±3.55 r_sh), NO un anillo extendido.
- Banda frontal GRUESA: ~40px de alto = ±0.43 r_sh, que CRUZA por delante
  de la esfera en el ecuador (el clásico Gargantua de Interstellar).
- ARCO DE LENTE superior (disco trasero lensado): cima a 1.34 r_sh sobre
  el polo, BRILLANTE.
- Tilt del sistema: ~-12° (extremo derecho hacia arriba).
- DOPPLER: el lado que se acerca (izquierda) blanco-dorado CEGADOR; el
  que se aleja (derecha) carmesí tenue.
- Paleta medida: blanco #FFFFFF, dorado #FDCB7C, coral #FA7069, rosa
  #FF5DAE, magenta #FF26B0, carmesí #F82960, granate #8B0000, halo
  #1D0007.

### B. EL GENERADOR PROCEDURAL (`tools/gen_gargantua.py`)

Un mini "ray-tracer artístico" de lente gravitacional (la técnica de los
shaders de Gargantua, sin geodésicas reales): 8 iteraciones de refinado
con comparación visual iterativa + autodiagnóstico ASCII contra el mapa
de brillo de la referencia. Técnicas:

- **Proyección física**: disco edge-on elíptico con SKEW radial |Xr|^0.74
  (puntas de aguja), grosor 3D del torus (más grueso al frente), banda
  que cruza el ecuador por DEBAJO del centro de la sombra.
- **Paleta DUAL por Doppler**: gradiente CÁLIDO (negro→granate→coral→
  naranja→dorado→blanco) en el lado que se acerca, magenta en el que se
  aleja — interpoladas por píxel.
- **Turbulencia con DOMAIN WARPING** (plasma fluido, no estática digital)
  + filamentos blancos finos + VETAS OSCURAS entre filamentos (el
  contraste duro de la referencia).
- **RIM INTERIOR ardiendo**: franja blanco-dorada en el borde interno del
  disco (donde el gas orbita más rápido) + HOTSPOT cegador en el cruce.
- **Anillo de fotones** naranja-blanco (la parte MÁS brillante) con picos
  de relámpago deterministas.
- **Tendrilas de gas** (wisps) y neblina roja atmosférica alrededor.

Salida calibrada (2048×1024, sombra a R_SH=150px):
- `GargantuaBack.png` — todo lo que vive DETRÁS de la esfera.
- `GargantuaFront.png` — SOLO la banda que cruza por delante.
- `GargantuaShadow.png` — círculo negro de borde NÍTIDO (caída de 6px).

### C. EL RENDERER (`Content/VFX/GargantuaRenderer.cs`)

Render por 3 pasos con el contrato de batch heredado (mundo + lente):

1. **GARGANTUABACK** (aditivo): neblina + wisps + arcos de lente +
   anillo de fotones + penumbra.
2. **LA SOMBRA** (alpha): negro absoluto NÍTIDO — come el fondo del mundo
   y el brillo trasero: el vacío.
3. **GARGANTUAFRONT** (aditivo): la banda que CRUZA el ecuador con sus
   filamentos y su hotspot blanco-dorado.

El conjunto BAMBOLEA (±1.1°) y RESPIRA (0.97..1.03). La distorsión del
fondo la sigue aportando BlackHoleLensSystem (abraza el disco completo:
134px vs ±102px del disco) y las partículas orbitales viven ahora en el
radio del disco visible (1.4..3.1 R) con la paleta cálida coral/dorado.

### D. SIN CAMBIOS DE FÍSICA

El CrimsonBlackHoleProjectile conserva TODA la física probada (aura de
daño con ticks acelerados, atracción 10× el sol, devora balas enemigas,
persecución lenta, anillo de Einstein final). Solo cambian: el render
(StylizedVoidRenderer queda como miembro de la biblioteca para usos
futuros), la paleta de las partículas de materia (coral→dorado→carmesí
en vez de fucsia) y la iluminación (coral-cálida en vez de magenta).

Compilado contra tModLoader real: 0 errores, 0 warnings.

## Commit v6.03 — LA BIBLIOTECA VISUAL AETHON + EL AGUJERO NEGRO DE LA REFERENCIA + LAS DOS CORONAS

**Peticiones del usuario**: (1) "es momento de diseñar el nuevo sistema...
es el momento de diseñar las nuevas librerias y assets" (sobre la
investigación de técnicas de los mods populares); (2) "toma la corona actual
del agujero negro y quítala, en cambio toma esa corona y conviértela en un
ítem cosmético que ubica la corona justo detrás de la cabeza del jugador";
(3) "con respecto al agujero negro este no se parece en nada a la referencia,
tiene que ser exactamente igual pero sin la corona"; (4) "intenta recrear
esa corona como un item extra, esta sera una nueva corona que no tiene nada
que ver con la corona actual del agujero negro y esta sera un nuevo
cosmetico" (dos imágenes de referencia nuevas: la entidad cósmica y el
portal rosa).

### A. LA BIBLIOTECA VISUAL AETHON (`Content/VFX/`)

El nuevo sistema de librerías visuales propio — 6 módulos que ya usan (y
usarán) todas las armas del mod:

- **VFXCore** — el núcleo: los efectos se describen como listas de CUADROS
  DE LUZ (GlowQuad: posición mundo/color/escala/rotación/textura) y se
  vuelcan con UNA llamada al destino que haga falta: dibujo aditivo directo
  (proyectiles, neón real) o emisión de DrawData para las capas de dibujado
  del jugador (el camino oficial de tML, sin tocar el batch del renderer).
  Un MISMO renderizador sirve en un proyectil y sobre la cabeza de un
  jugador. Buffer estático reutilizable (cero GC por frame) + helpers de
  respiración/balanceo/hash determinista/elipses.
- **VFXPalettes** — paletas nombradas: VoidQueen (la del agujero de la
  referencia, MEDIDA por píxel), CrimsonCourt (la corona de arcos) y
  RuneStars (la corona rúnica nueva).
- **ArcCrownRenderer** — la corona de arcos de neón (5 lazos con asimetría,
  ecos interiores y nudos naranjas con destello de 4 puntas), promotida de
  código de proyectil a MIEMBRO DE LA BIBLIOTECA reutilizable.
- **RuneCrownRenderer** — la corona rúnica estelar: 8 glifos rúnicos
  ANGULARES diseñados desde cero (la lanza, el cáliz, la puerta, la
  estrella, el rayo, el arco, la espiral y el trono) flotando en arco sobre
  la cabeza, con perlas rosa pálido en las puntas y halo tenue.
- **StylizedVoidRenderer** — EL AGUJERO NEGRO DE LA REFERENCIA (ver B).
- **BoltRenderer** — relámpagos deterministas reutilizables (zigzag por
  hash puro: todas las máquinas ven el MISMO rayo; se regenera cada ~9
  ticks — VIVE).

### B. EL AGUJERO NEGRO — EXACTAMENTE LA REFERENCIA (SIN CORONA)

Geometría MEDIDA por píxel en las dos imágenes de referencia (perfil
horizontal/vertical del agujero del pecho de la entidad + elipse del
portal): lente plana brillante de aspecto ~2:1, zona oscura interior hasta
±0.23× el semieje, aro BLANCO-CÁLIDO a 0.47×, banda fucsia SATURADA de 0.7
a 1.0× (picos medidos 255,0,255 y 248,0,73 — paleta fucsia intermedia
exacta), núcleo negro PURO pequeño (~5% de la lente) y media luna inferior
de destello frontal (pico medido 255,26,255).

Render por capas: halo púrpura compacto que respira → relleno SÓLIDO
(GlowOrb, lente saturada) → GRADIENTE CONTINUO de 10 anillos de Ring
ESTIRADOS y solapados (blanco-cálido → caliente → fucsia; banda CONTINUA,
nada de puntos) → EL VACÍO (4 elipses negras apiladas: negro absoluto y
penumbra) → cruce frontal de los 3 anillos interiores + MEDIA LUNA →
temblor de materia fluyendo (rotación ANTIHORARIA con leve Doppler
izquierda-caliente, medido en la referencia) → ONCE rayos superiores
sutiles (los del portal) → DOS relÁMPAGOS de los flancos. El shader
raymarchado YA NO se usa en el carmesí (el original lo conserva intacto):
el look de la referencia es luz por capas, no 3D realista. 8 iteraciones
de verificación con simulación PIL espejo del algoritmo + crítica visual +
medición de perfiles normalizados contra el recorte de la referencia.

LA CORONA fue RETIRADA del proyectil (DrawCoronaCrown y SpawnCrownEmbers
ELIMINADOS — 0 ocurrencias en el binario).

### C. LA CORONA DE LA REINA DEL VACÍO (cosmético 1)

La corona ORIGINAL del agujero negro (v6.02), retirada del proyectil y
convertida en ÍTEM COSMÉTICO: **VoidCrownItem** — accesorio puro (cero
estadísticas, huecos funcionales O de vanidad) que dibuja la corona de
arcos JUSTO DETRÁS de la cabeza del jugador (capa VoidCrownDrawLayer,
ANTES de la capa Head: la cabeza tapa lo que cruza — la corona ENVUELVE).
Escala a la cabeza, respira, se balancea, suelta ASCUAS ROSAS sobre los
ápices (la corona es energía viva) e ilumina la noche con su carmesí.
Ícono procedural de 3 arcos + nudos (3 iteraciones de crítica visual).

### D. LA CORONA RÚNICA ESTELAR (cosmético 2)

Diseño NUEVO DESDE CERO sobre la referencia (no tiene nada que ver con la
corona de arcos): **RuneCrownItem** — ocho glifos rúnicos de luz fucsia
flotando en arco ALTO sobre la cabeza (capa RuneCrownDrawLayer, tras las
capas de cara: es un halo), cada uno con su PERLA blanca-rosada en la
punta. Los glifos flotan con oscilación desfasada, pulsan su brillo con
fase propia y EMITEN CHISPAS ASCENDENTES desde las perlas. El arco flota
alto para no competir con la corona de arcos si el jugador lleva AMBAS.
Ícono procedural de arco fucsia + 5 glifos simples + perlas (3 iteraciones
de crítica visual).

### E. INFRAESTRUCTURA Y VERIFICACIÓN

- **CosmeticPlayer** (ModPlayer): escanea los 14 huecos de accesorio
  (funcionales 3..9 + vanidad 13..19) — un cosmético es un cosmético viva
  dónde lo pongas; hace vivir las coronas (ascuas, chispas) y las ilumina.
- **TestingPlayer**: el kit de pruebas entrega las DOS coronas además del
  agujero carmesí.
- **Localización** en-US/es-ES completa (2 ítems nuevos + agujero
  actualizado); tooltips reescritos.
- **Compilación contra tModLoader v2026.07.3.0 real**: 0 errores, 0
  warnings (Debug y Release). **Auditoría de binario**: las 11 clases
  nuevas + 21 símbolos presentes; DrawCoronaCrown/SpawnCrownEmbers = 0;
  el shader del agujero ORIGINAL intacto (UTF-16 verificado).

## Commit v6.02 — INVESTIGACIÓN VISUAL + LIMPIEZA TOTAL DE REFERENCIAS + EL AGUJERO NEGRO CARMESÍ

**Peticiones del usuario**: (1) "quiero que hagas una investigacion super
profunda de todas las librerias y recursos visuales de los mods populares"
(prestando especial atención al referente de
calidad visual que el usuario citó), "todo en pos de mejorar el aspecto futuro
de nuestro mod"; (2) "luego has 100 pasadas al proyecto completo para limpiar
y depurar, recuerda eliminar cualquier mención de cualquier otro mod o
referencias externas en cualquier sentido"; (3) "revisa bien que el codigo
sea super correcto y ademas sin errores ni fallas ni faltas"; (4) "copiar el
arma de agujero negro en una nueva arma de agujero negro para darle un poco
mas de personalidad... modificarla para que se vea exactamente igual a como
esta en la imagen de referencia, recuerda dejar al agujero negro original
intacto" (imagen de referencia: agujero negro carmesí con corona de arcos).

### A. INVESTIGACIÓN VISUAL (conocimiento para el futuro del mod)

Estudio profundo de las técnicas de los mods visuales top (el mod de
referencia pedido por el usuario, más los sistemas de partículas y VFX
públicos del ecosistema) — conclusiones ACCIONABLES documentadas en el
documento del proyecto (sección 15, neutralizada de nombres): técnicas de
marching de luz, lente a pantalla completa, capas aditivas, partículas por
componentes, texturas de ruido procedurales. El mod ya implementa su propio
pipeline equivalente (shader de 75 pasos + LensSystem + librería de
partículas propia + generador de texturas).

### B. LIMPIEZA TOTAL — 0 REFERENCIAS EXTERNAS

- **Carpeta `research/` ELIMINADA del repo** (25 archivos: ejemplos de
  código de otros mods y notas con nombres externos — nunca formaron parte
  del mod compilado, pero vivían en el repo)
- **Shaders muertos eliminados**: BlackOnlyShader (.fx+.fxc), Shockwave.fx,
  Bloom.fx, ChromaticAberration.fx (0 referencias en el código) — quedan
  SOLO los 4 activos: RealBlackHoleShader, SunShader, RadialShineShader y
  BlackHoleDistortionShader
- **Las 6 texturas del pipeline REGENERADAS 100% proceduralmente**
  (`tools/gen_effects_textures.py`: ruido de valor periódico + deformación
  de dominio — FireNoiseB, DendriticNoiseZoomedOut, WavyBlotchNoise,
  PsychedelicWingTextureOffsetMap, BloomCircleSmall, InvisiblePixel) con
  estadísticas calibradas al uso de cada shader; 4 texturas muertas fuera
  (BloomCircle, BloomFlare, FireNoiseA, WavyBlotchNoiseDetailed)
- **Los 4 .fx REESCRITOS como fuente propia** (misma matemática, expresión y
  comentarios propios, parámetros idénticos; los .fxc compilados se
  mantienen — el pipeline fx_2_0 documentado en COMPILACION.md)
- **0 menciones externas en TODO el mod**: comentarios .cs (citas de libro
  de referencia, URLs, notas de inspiración), CHANGES.md (nombres de mods,
  organismos y películas), documento del proyecto (58
  menciones neutralizadas), DISEÑO/ROADMAP (compatibilidad con otros mods
  reescrita genérica), COMPILACION.md (enlaces externos → notas propias del
  pipeline); README/description.txt actualizados a la realidad (eventos
  eliminados en v5.27 fuera, arsenal actual)
- **Auditoría de binario**: 0 ocurrencias de nombres externos en el DLL

### C. 100 PASADAS DE DEPURACIÓN

- **11 usings muertos eliminados** (verificado compilando: solo 1 falso
  positivo restaurado por Point16)
- **Localización COMPLETADA**: 38 entradas DisplayName que faltaban (19
  clases × 2 idiomas — BlackHoleStaff, SunStaff, los 4 V20, tests,
  proyectiles...) + clave muerta Items.Placeables.AncientAltarItem corregida
  a la ruta real + armas nuevas; el inventario ya no muestra nombres crudos
- **Constante muerta eliminada** (FlareInterval en SunProjectile)
- **Balance Begin/End verificado** (los 8 "excesos" son los cierres
  defensivos documentados — correctos)
- **Texturas**: 41/41 clases con su .png ✓
- **Compilación**: 0 errores / 0 warnings contra tModLoader v2026.07.3.0

### D. EL AGUJERO NEGRO CARMESÍ (arma nueva — la corona de la reina)

- **CrimsonBlackHoleStaff** (daño 110, cadencia 50): copia CON
  personalidad del BlackHoleStaff — el ORIGINAL QUEDA INTACTO. Dispara
  **CrimsonBlackHoleProjectile**: misma física probada (aura de daño con
  ticks que aceleran cerca del centro 6→24, atracción 2.6 en 450px, devora
  balas enemigas al cruzar el horizonte con chispas ROSAS, persecución
  lenta, anillo de Einstein final con el daño completo) y el visual de la
  imagen de referencia:
  - **Disco de acreción MAGENTA ELÉCTRICO** (#FF0055 vía el parámetro del
    shader) más de canto (cameraAngle 0.42) y más prominente (0.44)
  - **Anillo de fotones ROSA-INCANDESCENTE**: halo rosa + núcleo fino
    blanco-rosa (#FFBB90) pulsando a 4.5 rad/s justo fuera del horizonte
  - **LA CORONA**: 5 lazos de neón carmesí→magenta sobre el anillo (el
    exterior el más alto), CON ASIMETRÍA dinámica por lazo (semianchos izq/
    der distintos + balanceo por índice), ECO interior tenue por lazo
    (filamentos encajados), grosor variable (fino en bases, corpulento al
    subir) y NUDOS NARANJA incandescentes con DESTELLO DE 4 PUNTAS pulsante
  - **Ascuas rosas** alzándose sobre la corona (la energía es VIVA)
  - Partículas/halo/iluminación en toda la paleta carmesí/magenta/rosa
    (dusts Crimson + Enchanted_Pink, estelas TrailGlow magenta)
- **Integración completa**: LensSystem (misma lente gravitacional + dibujo
  propio encima), kit de TestingPlayer con EnsureItem, localización ES/EN,
  texturas procedurales (icono 28×30 con orbe coronado + placeholder 76×76)

### E. VERIFICACIÓN

- Compilación contra tModLoader v2026.07.3.0 REAL: 0 errores, 0 warnings
- Binario: clases nuevas presentes, muertas ausentes, 0 nombres externos
- 41/41 clases con textura ✓; localización 100% completa ES/EN ✓
- Arsenal de pruebas: 14 → **15 armas** (el Carmesí se entrega siempre)

## Commit v6.01 — LA GRAN LIMPIEZA: EL USUARIO ELIGE QUÉ SE QUEDA (21 ARMAS FUERA)

**Petición del usuario**: "es momento de seleccionar que se queda en el
proyecto" + lista explícita de borrado (números del inventario) + sobre el
Quásar y la Galaxia Viviente: "la galaxia se ve horrible y la lanza igual,
ademas las dos son tan simple que no vale la pena que continue en el mod".

El arsenal de pruebas pasó de 35 a **14 armas**. Se listó el inventario
completo agrupado por generación (8 tests originales + Grimorio + 19 V20 +
7 cósmicas), el usuario seleccionó, se confirmaron las dudas (el "191"
era el 19 = AbyssalEyeStaff; Quásar/Galaxia confirmadas fuera) y se
ejecutó la purga con auditoría completa.

### A. FUERA — 21 ARMAS ELIMINADAS

- **4 ARMAS DE COLOR** (las más viejas, pruebas de sistema de partículas):
  ColorRainbow, ColorRed, ColorYellow, ColorGreen — clases extirpadas de
  TestAdvanced.cs, texturas borradas, y sus 4 handlers (modos 5004-5007)
  limpiados de TestAdvancedFX.cs junto al helper huérfano
  DrawColoredSprite (solo lo usaban ellas)
- **15 ARMAS V20** (de las 20 de partículas): TornadoStaff,
  PrismBeamStaff, EarthquakeStaff, MirrorDimensionStaff,
  GravityPulseStaff, ShadowCloneStaff, CrystalShatterStaff,
  VortexChainStaff (con su VortexMineProjectile interno), AbyssalEyeStaff,
  SpectralMirageStaff, TemporalRiftStaff, InfernoTornadoStaff,
  VoidEaterStaff, PlasmaOrbStaff, BlackHoleMiniStaff — arma + proyectil +
  texturas, mapeo 1:1 verificado sin huérfanos
- **2 CÓSMICAS NUEVAS** (v5.99/v6.00, "se ven horrible y son muy
  simples"): LA LANZA DEL QUÁSAR (QuasarLance + QuasarJetProjectile) y LA
  GALAXIA VIVIENTE (LivingGalaxyStaff + LivingGalaxyProjectile +
  GalaxyStarProjectile + SpiralGalaxy.png procedural)
- **BlackHoleLensSystem — cirugía del bloque galaxia**: recolección,
  arrays _galaxyIndices/_galaxyCount, fuente del pase B y draw loop
  extirpados; el protocolo de dibujado-encima-de-la-lente vive intacto en
  soles, medusas, cometas y púlsares
- **Localización en-US/es-ES**: entradas del Quásar y la Galaxia eliminadas
- **TestingPlayer**: 21 líneas de entrega eliminadas del kit

### B. SE QUEDAN — LOS 14 ELEGIDOS

- **4 tests clásicos**: TestMagicRing, TestSparkle, ProjBeam,
  TestMagicRingV2 (con sus handlers 3003/3004/4001/4006 intactos)
- **El Grimorio del Eterno** (con su Orbe Cósmico — la imagen del
  usuario, INTACTA)
- **4 V20**: SupernovaStaff, PlasmaStormStaff, PhoenixNovaStaff,
  QuantumSplitStaff
- **5 cósmicas**: BlackHoleStaff, SunStaff, MedusaNebularStaff,
  LivingCometStaff, LivingPulsarStaff (todas con su EnsureItem individual
  garantizado)
- Quien tenga armas borradas en un guardado viejo LAS CONSERVA (no se
  eliminan del inventario existente — solo dejan de entregarse)

### C. VERIFICACIÓN

- Auditoría de referencias: 0 menciones a las 40 clases borradas en todo
  el .cs del mod
- Auditoría de texturas: 39 clases ModItem/ModProjectile/ModBuff, TODAS
  con su .png en la ruta por defecto ✓
- Compilación contra tModLoader v2026.07.3.0 REAL: 0 errores, 0 warnings
- Auditoría de binario (búsqueda de bytes ASCII+UTF-16): 30 nombres
  borrados → 0 restos; 20 conservados → todos presentes

### D. MARCADA COMO ESTABLE

- **2026-09-12**: el usuario verificó v6.01 como ESTABLE y pidió guardarla
  como punto de retorno en GitHub. Tag `stable-v6.01` + rama
  `stable-v6.01-backup` + STABLE-SNAPSHOT.md regenerado (manifiesto
  SHA-256 completo del paquete: 186 archivos).

## Commit v6.00 — SOL Y AGUJERO PULSAN MÁS FUERTE + EL LÁTIGO DE LA MEDUSA + ADIÓS OJO, LLEGA LA GALAXIA VIVIENTE

**Peticiones del usuario**: (1) "creo que deberías aumentar los tick de
daños del sol y el agujero negro, tambien aumentar el area de daño del
agujero negro y del sol; en el caso del agujero negro los tick de daño
deben aumentar a medida te acercas al centro"; (2) "tanto el sol como el
agujero negro deben perseguir ligeramente a los enemigos y tambien deben
ser capas de afectar los proyectiles con su gravedad"; (3) "en cuanto a
la medusa el rayo debe salir de medusa no del cielo, y debe tener mas
brillo"; (4) "ademas mejora las nuevas armas que creaste y borra el ojo,
se ve feo, mejor crea un arma nueva con un proyectil cosmico, este debe
ser una galaxia, investiga galaxias en internet, recuerda todas estas son
armas de prueba no requieren mana".

### A. EL AGUJERO NEGRO Y EL SOL — MÁS TICKS, MÁS ÁREA, PERSIGUEN Y DOBLAN BALAS

- **AGUJERO NEGRO — TICKS QUE ACELERAN CERCA DEL CENTRO** (petición
  explícita): adiós al pulso global cada 0.5 s — ahora CADA ENEMIGO tiene
  su PROPIO intervalo según su distancia al horizonte: en el borde del
  aura ~24 ticks (0.4 s), PEGADO AL CENTRO 6 ticks (10 golpes/s): el campo
  te MACHACA cuanto más te hundes. El área además creció: 1.15→1.9× el
  campo (antes 0.75→1.2×)
- **EL SOL**: pulso de aura 15→10 ticks (+50% de golpes/s) y área
  1.75→2.30× el radio visual (la gigante roja la arrastra: ~150→310 px);
  daño del aura 40→45%
- **PERSIGUEN LIGERAMENTE A LOS ENEMIGOS** (ambos): el agujero SE DESLIZA
  hacia la presa más cercana (accel 0.07/t, tope 2.4 px/t — deriva
  amenazante) y el sol igual (0.09/t, tope 3 px/t)
- **LA GRAVEDAD AHORA DOBLA PROYECTILES ENEMIGOS** (ambos): las balas
  hostiles caen en espiral hacia el agujero y AL TOCAR EL HORIZONTE SON
  ABSORBIDAS (chispas doradas — defensa gravitacional pura: el agujero SE
  COME las balas); el sol las curva débilmente y si tocan el plasma SE
  EVAPORAN en polvo de fuego

### B. EL LÁTIGO ELÉCTRICO DE LA MEDUSA (v6.00)

Petición: "el rayo debe salir de medusa no del cielo, y debe tener mas
brillo". NebulaLightning REESCRITO: el rayo NACE BAJO LA CAMPANA (la
"boca") y VUELA RECTO hacia la víctima — un LÁTIGO de plasma frío que se
desenrosca de la medusa. MÁS BRILLO: TRES capas aditivas (halo aqua
ancho + funda azul-blanco + NÚCLEO blanco puro a 255), luz real
proyectada cada paso (1.3/1.55/1.75), micro-parpadeo vivo, ramas cortas
laterales, frente de 4 puntas y DESCARGA en el origen (la campana
chispea al soltarlo); al clavarse: trueno + estallado de hielo y el trazo
LIGERA chisporroteando mientras se funde. Daño del rayo 0.8→1.0× (es EL
ataque de la medusa).

### C. EL OJO DEL VACÍO — ELIMINADO

Petición: "borra el ojo, se ve feo". Borrado COMPLETO: VoidEyeStaff +
VoidEyeProjectile (.cs y .png), las 3 texturas del ojo (EyeSclera/
EyeIris/EyeLid), los generadores PIL, las referencias del LensSystem
(_eyeIndices/pase B/dibujado encima), TestingPlayer y la localización.
El binario verifica 0 ocurrencias de VoidEye.

### D. LA GALAXIA VIVIENTE (arma nueva — el proyectil ES una galaxia)

Petición: "crea un arma nueva con un proyectil cosmico, este debe ser una
galaxia, investiga galaxias en internet". Diseño de galaxia
espiral REALISTA:
 bulbo AMARILLO de estrellas viejas, brazos
AZULES de estrellas jóvenes, NUDOS ROSAS HII ("beads-on-a-string"),
CARRILES DE POLVO oscuros al borde interno de los brazos. SpiralGalaxy.png
512 px PIL ×4 supersampling con 2 iteraciones de crítica VLM (7.5→8.5/10:
polvo como polilíneas oscuras que CORTAN el azul, HII vívidos "como
letreros de neón", brazos asimétricos, bulbo elíptico moteado con
filamentos) + icono 30×30 de alto contraste (remolino en S grueso).

- **LivingGalaxyStaff** (Magic, daño 110, mana 0, useTime 30) →
  **LivingGalaxyProjectile** (~9 s): nace con pop elástico, VUELA y SE
  ESTACIONA donde la lanzaste; el disco GIRA (0.02 rad/t) y CABECEA EN 3D
  (escala Y 0.55→1.0 — la moneda espacial de canto a cara) + eco tenue
  rotado (imagen secundaria); ARRASTRA enemigos (gravedad 0.4, radio
  300), AURA estelar cada 10 ticks (45%), SEMBRADO estelar cada 24 ticks
  (los 2 brazos sueltan GalaxyStarProjectile tangencialmente — rociador
  cósmico), ACECHA (el ancla deriva hacia la presa 0.7 px/t)
- **LA EXPLOSIÓN ESTELLAR** (OnKill): AoE 90% + 14 semillas estelares
  radiales + destello + sonidos (nada de ondas: el sol tiene SU nova y el
  agujero SU anillo — la galaxia estalla en SEMILLAS)
- **GalaxyStarProjectile**: estrellas de 4 puntas girando con halo y eco,
  cada una con el COLOR de su origen (azul de brazo / oro de bulbo /
  rosa de HII)
- **Lente**: pase B respirando con el giro (0.07→0.12 — masa de cien mil
  millones de soles), dibujada ENCIMA de la lente (protocolo del arsenal)

### E. MEJORA DE LAS ARMAS NUEVAS + TODAS SIN MANA

Petición: "mejora las nuevas armas que creaste, recuerda todas estas son
armas de prueba no requieren mana".

- **Cometa Estelar**: daño 38→46, nova 92→130 px al 75% (antes 60%),
  picado 16.5→19 px/t, cooldown 70→60
- **Púlsar Vivo**: daño 30→38, haces 340→420 px, daño del haz 55→65%
- **Lanza del Quásar**: daño 85→100, ATRAVIESA 10→14 enemigos, vida
  90→120 (más alcance), florecimiento 130→170 px al 65%
- **MANA = 0 en TODAS las armas de prueba cósmicas**: MedusaNebularStaff,
  LivingCometStaff, LivingPulsarStaff, QuasarLance (SunStaff y
  BlackHoleStaff ya lo eran) — y la nueva LivingGalaxyStaff nace sin mana

### F. Verificación

Compilación contra tModLoader real v2026.07.3.0 (/tmp/verify): 0 errores,
0 warnings. Auditoría del binario: LivingGalaxyProjectile/
GalaxyStarProjectile/LivingGalaxyStaff/DrawGalaxyVisuals/
GetGalaxyLensStrength/FindNearestEnemy presentes; 0 ocurrencias de
VoidEye. Auditoría de texturas: las 86 clases del arsenal con su asset ✓
(85 de v5.99 + 3 nuevas − 2 del ojo).

## Commit v5.99 — EL OJO REDISEÑADO + RAYOS para la Medusa + EL COMETA ESTELAR + EL PÚLSAR VIVO + LA LANZA DEL QUÁSAR

**Peticiones del usuario**: (1) "el ojo no se ve nada bien, intenta
mejorarlo para que se vea bien, investiga en internet para conseguir ideas
de assets o como hacerlo"; (2) "la medusa es interesante, pero sus
proyectiles son aburridos, es mejor que el proyectil que usa la medusa sean
rayos, ya sabes, los rayos que caen del cielo"; (3) "usando la segunda
imagen como referencia, crea un minion cosmico con efectos, de la misma
forma a como creaste la medusa, pero cuidado ya existe un minion con el
nombre minion cosmico que usa exactamente la misma imagen, no lo toques";
(4) "luego crea otro minion cosmico, y crea una nueva arma con un proyectil
super cosmico"; (5) "no olvides arreglar y mejorar el ojo".

### A. EL OJO DEL VACÍO — rediseño TOTAL del render (investigado en internet)

Investigación de diseño (técnicas de ojos realistas + anillo de fotones y
disco de acreción curvándose sobre y bajo la esfera, como se ve en los
agujeros negros del cine) + análisis VLM de la captura del usuario: la esclerótica
era un "plato de cerámica plano con garabatos", la pupila "una PUERTA DE
MADERA" (el RealBlackHoleShader mini a escala pequeña era papilla
ilegible), los párpados "brackets pesados sueltos".

- **Texturas v2 (PIL ×4 supersampling, 2 iteraciones con crítica VLM
  8/10 y 9/10)**: EyeSclera — sombreado ESFÉRICO (limbo oscuro + luz
  arriba-izq), venas AUDACES con núcleo oscuro + halo (rojo carmesí y
  azul-violeta, ramificación orgánica que se desvanecen antes del iris),
  moteado biológico, subsurface cálido abajo, ESPECULAR EN MEDIA LUNA
  (córnea); EyeIris — generación PER-PIXEL con campos de ruido
  (turbulencia orgánica): bandas orbitales onduladas + fibras radiales
  finas + criptas caóticas + grano estelar + anillo limbal violeta +
  borde interno ARDIENTE (nada de "arcos estampados"); EyeLid — placas de
  armadura de carbón con rim light cálido, pliegues y grietas; icono del
  arma rehecho.
- **LA PUPILA DEL AGUJERO** (método procedural, sustituye al shader mini):
  esfera negra con borde suave + halo de absorción + ANILLO DE FOTONES
  fino blanco-caliente (micro-pulso) + banda de acreción horizontal
  CRUZANDO por delante + BANDA VERTICAL lenteada detrás (los arcos sobre
  y bajo la esfera — la imagen lenteada del disco) + chispa de beaming
  relativista. Legible a CUALQUIER escala.
- **El dibujado**: vignetta de cavidad suave (el ojo ASIENTA en la
  estrella — adiós anillo duro suelto), FALLOFF iris→pupila (la pupila se
  HUNDE), CATCHLIGHT unificado (media luna húmeda sobre iris+pupila),
  RIM GLOW aditivo en los párpados (la luz del ojo baña la armadura) y
  halo que RESPIRA lento. El _bhShader y su carga ELIMINADOS.

### B. LA MEDUSA — RAYOS QUE CAEN DEL CIELO (adiós agujas aburridas)

`JellyfishStingBolt` (agujas de luz) ELIMINADO → **`NebulaLightning`**:
cuando la campana se contrae junto a una víctima, la medusa DESCARGA un
rayo cósmico que CAE DEL CIELO sobre ella — nace 420 px arriba, cae
vertical a ~90 px/t, con ZIGZAG dentado REGENERADO cada pocos ticks
(vive), ramas laterales cortas, frente brillante con destello de 4 puntas,
chispas de hielo al caer y TRUENO + destello de impacto al clavarse.
QUEMADURA DE HIELO (Frostburn) intacta (la firma de la medusa). Zigzag
determinista por hash (semilla, tick, segmento) → mismo rayo en todas las
máquinas. Tooltips y localización actualizados (Rayo Nebular).

### C. EL COMETA ESTELAR (LivingCometStaff → StellarCometMinion)

Petición: crear un minion cósmico con la imagen de referencia (la
criatura-estrella de 8 puntas) — SIN tocar el CosmicOrbMinion existente
(que usa ESA imagen): esta es una criatura ORIGINAL hermana. Un cometa
VIVO: núcleo de plasma blanco-oro con granulación (CometHead.png) +
**CORONA DE 8 PUNTAS lanceoladas cian→violeta GIRANDO** (CometCrown.png —
el homenaje a la referencia) + **COLA de polvo estelar** (historial de 18
posiciones, cálida cerca → fría lejos) + **chispas orbitando** (el campo
de partículas de la referencia). **NO persigue: ORBITA al jugador en una
elipse excéntrica** (apoapsis/periapsis, fase por minionPos) y para
atacar **CAE EN PICADO** (aceleración 0.46/t hasta 16.5) — al rozar a la
víctima **ESTALLA EN UNA PEQUEÑA NOVA** (AoE 92 px al 60% + OnFire —
materia estelar CALIENTE, la firma opuesta a la medusa) y rebota de
vuelta a la órbita. Lente del pase B que CRECE CON LA VELOCIDAD
(velocidad = momento = curvatura, 0.05→0.17). Buff StellarCometBuff
(patrón del arsenal) + iconos PIL.

### D. EL PÚLSAR VIVO (LivingPulsarStaff → LivingPulsarMinion)

El segundo minion: una **estrella de neutrones VIVA** (PulsarCore.png:
núcleo blanco-azul extremo + arcos magnéticos nítidos + polos brillantes
+ bandas de giro) que **GIRA barriendo el campo con DOS HACES DE FARO
opuestos** (340 px, rotación vuelta cada ~6.9 s) — el ataque más raro del
arsenal: el daño NO es contacto ni proyectil, son LOS RAYOS GIRANDO
(comprobación angular por tick, tolerancia que se abre con la distancia,
55% del daño cada 5 ticks + **ELECTRIFIED** — radiación de sincrotrón).
Deriva en un lissajous perezoso sobre el hombro; con objetivo se coloca
EN ALTO a media distancia jugador-víctima para RAÑARLA en cada giro.
Haces dibujados como rayos cónicos blancos-cian con pulso viajero +
rastro de remolino + chispas tangenciales. Lente del pase B PULSANDO con
el giro (0.05→0.12). Buff LivingPulsarBuff + iconos PIL.

### E. LA LANZA DEL QUÁSAR (QuasarLance → QuasarJetProjectile)

El arma nueva con "un proyectil super cosmico": dispara un **CHORRO
RELATIVISTA** — el objeto más brillante del universo (los chorros de los
quásares superan el brillo de galaxias enteras). Una lanza de luz de 132
px velocísima (26 px/t ×3 updates) que **ATRAVIESA hasta 10 enemigos**,
con **5 NUDOS DE SHOCK** (los knots de Herbig-Haro) pulsando hacia la
punta, retorción HELICOIDAL sutil, 3 capas (filo violeta → halo cian →
núcleo blanco) y estela de polvo estelar. Al disiparse: **EL
FLORECIMIENTO DEL QUÁSAR** — AoE 130 px al 55% + destello + temblor.
DamageClass.Magic, damage 85, mana 14.

### F. Infraestructura

BlackHoleLensSystem: _cometIndices/_pulsarIndices recogidos SIEMPRE y
dibujados ENCIMA de la lente (corona, núcleo y haces jamás deformados) +
fuentes del pase B (cometa: velocidad; púlsar: pulso del giro).
TestingPlayer: las 3 armas nuevas garantizadas individualmente
(EnsureItem). Localización en-US/es-ES completa (armas, minions, buffs,
proyectil del quásar). Auditoría de texturas: las 85 clases del arsenal
con su asset ✓. Compilación contra tModLoader v2026.07.3.0 real:
**0 errores, 0 warnings**.

---

## Commit v5.98 — FIX: el mod NO CARGABA (texturas de la Medusa ausentes) + la Medusa SIEMPRE en el inventario

**Peticiones del usuario**: (1) "mira estos errores" (capturas del juego:
`MissingResourceException: Content/Projectiles/Cosmic/NebulaJellyfishMinion`
y `Content/Projectiles/Cosmic/JellyfishStingBolt` — el mod se desactivaba
automáticamente al cargar); (2) "recuerda que el invocador de medusa se lo
debes dar al jugador desde el inicio".

### A. LA CAUSA — texturas de clase ausentes

v5.97 añadió los `.cs` de la Medusa pero **NO sus dos texturas de clase**.
tModLoader exige un asset para CADA `ModProjectile` en su ruta por defecto
(`Content/Projectiles/Cosmic/<Clase>`); al no existir, el cargador lanzaba
`MissingResourceException` (dos inner exceptions en un `MultipleException`)
y **desactivaba el mod entero** — por eso el jugador no veía NADA de v5.97
(ni las explosiones únicas, ni el Ojo, ni la Medusa).

**El fix**: `NebulaJellyfishMinion.png` y `JellyfishStingBolt.png` —
placeholders 1×1 RGBA transparentes (70 bytes), el MISMO patrón que ya usan
`VoidEyeProjectile.png` y `CosmicShockwaveProjectile.png`: ambos proyectiles
se dibujan 100% proceduralmente (`PreDraw` devuelve `false`: campana +
galaxia + cuentas del minion; destello de 4 puntas del nematocisto), así que
la textura de clase JAMÁS se muestra — solo tiene que existir.

**Auditoría preventiva**: script propio que escanea las 77 clases con
textura obligatoria (`ModItem`/`ModProjectile`/`ModBuff`/`ModNPC`/…) del
arsenal completo — las dos de la Medusa eran las ÚNICAS ausentes (sin más
errores escondidos esperando al siguiente arranque).

### B. La Medusa SIEMPRE desde el inicio (kit "congelado" reparado)

El kit de `TestingPlayer` tiene gate de una sola vez (¿ya tienes el
`GenesisShard`?) — quien entró al mundo con una versión ANTERIOR jamás
recibía las armas añadidas después: el kit quedaba "congelado" en la
versión con la que se entregó (el mismo hoyo del VoidEyeStaff en v5.97, y
esta vez LA MEDUSA habría vuelto a quedarse fuera para quien ya tenía el
kit).

**El fix**: además del kit base (gate GenesisShard intacto), las cuatro
armas cósmicas en desarrollo — `BlackHoleStaff`, `SunStaff`, `VoidEyeStaff`
y **`MedusaNebularStaff`** — se garantizan INDIVIDUALMENTE en cada entrada
al mundo (`EnsureItem`: si no está en el inventario, vuelve). El invocador
de la Medusa SIEMPRE está ahí desde el inicio, venga del guardado que venga.

### C. Prueba del usuario

Develop Mods → Build → entrar al mundo: el mod CARGA sin errores (client.log
limpio) y el inventario trae el kit completo CON el Báculo de la Medusa
Nebular → invocarla: la medusa nada a pulsos colgando del hombro, galaxia
girando en el corazón, tentáculos ondeando con física propia, nematocistos
de quemadura fría al picar.

---

## Commit v5.97 — UNA SOLA explosión final + LA MEDUSA NEBULAR (invocador de minion)

**Peticiones del usuario**: (1) "el sol y el agujero negro tienen dos, digamos
explosiones al terminar, solo deben tener una donde suceda todo, los anillos
rgb del agujero negro quedan mal, lo mejor es un anillo de lente
gravitacional"; (2) "no veo el arma nueva, te olvidaste de dársela al
jugador"; (3) "crea una nueva arma que sea un invocador para un minion, este
minion debe ser algo que hayas creado, crea un proyectil super creativo y
cosmico, y este proyectil sera la invocacion".

### A. UNA SOLA EXPLOSIÓN FINAL — el anillo de lente ES la explosión

Las tres armas cósmicas mayores ya NO tienen "dos explosiones" al terminar
(onda de materia + onda de lente después). Ahora **TODO sucede en UNA**:

- **AGUJERO NEGRO**: la explosión YA NO es la onda cromática RGB que
  engendraba el anillo de Einstein al final — **ES EL ANILLO DE EINSTEIN
  MISMO** (`OnKill` → una única onda `StyleEinstein` con el daño COMPLETO del
  proyectil). El anillo ganó el **FLASH DE LIBERACIÓN**: durante sus primeros
  ~16 ticks dibuja en su centro un brillo cálido que se apaga mientras el
  anillo despega (la luz del colapso escapando) — flash + anillo + daño +
  ShadowFlame viven en la MISMA onda. La lógica "cromática engendra Einstein
  al terminar" se ELIMINÓ del AI de la onda. Los estilos cromáticos quedan
  como legado documentado (nada del arsenal actual los invoca).
- **SOL**: el OnKill ya NO suelta 3 ondas de fuego + 1 onda de lente — suelta
  **UNA SOLA ONDA NOVA DE LENTE** (nuevo `StyleNova`): el frente de
  espaciotiempo QUE LLEVA EL FUEGO — triple anillo ardiente (FireRing con su
  color propio) + frente fino blanco de choque + **aberración CÁLIDA**
  (oro por fuera, brasa por dentro — la nova dispersa LUZ DE FUEGO, no RGB).
  Daño de la nova COMPLETO en la banda 0.72-1.02·frente cada 0.1 s +
  quemadura 10 s; fuente del `BlackHoleLensSystem` (el fondo se curva a su
  paso). El AoE del núcleo (260 px) golpea en el mismo instante.
- **OJO DEL VACÍO**: su GRITO también se unificó — ya no son la cromática
  inversa del colapso + el anillo retardado: es **UN ÚNICO DESGARRO**
  (anillo de Einstein con el daño del grito COMPLETO, radio 460). Los dusts
  de implosión/sangre y el AoE del núcleo (380 px, ×1.6) ocurren en el MISMO
  instante.

### B. FIX — el Ojo del Vacío ya está en el inventario del jugador

`TestingPlayer.OnEnterWorld` (el kit de prueba del SP) NO incluía el
`VoidEyeStaff` de v5.96 — jamás llegó al inventario (por eso "no se veía el
arma nueva"). Ahora se entrega junto al resto del arsenal. También se
añadieron sus entradas de localización (en-US + es-ES: "Báculo del Ojo del
Vacío") para que sea localizable por nombre.

### C. LA MEDUSA NEBULAR — el invocador (el proyectil ES la invocación)

Petición: "crea una nueva arma que sea un invocador para un minion… crea un
proyectil super creativo y cosmico, y este proyectil sera la invocacion".

**LA CRIATURA** (`MedusaNebularStaff` → `NebulaJellyfishMinion`,
DamageClass.Summon, minionSlots 1, se apilan medusas):

- **Cuerpo**: una medusa nacida en el corazón de una nebulosa. Campana
  translúcida de gas interestelar **generada con PIL a 4× supersampling**
  (`JellyfishBell.png` 256: degradado radial aqua→esmeralda→violeta, 16
  costillas radiales con curvatura orgánica, anillos de crecimiento tenues,
  margen bioluminiscente ROSA y semillitas estelares dentro).
- **Corazón**: una **MINIGALAXIA ESPIRAL** (`JellyfishGalaxy.png` 128: dos
  brazos logarítmicos sembrados de ~140 estrellas cálidas/frías + núcleo
  blanco-dorado) que GIRA lentamente en el centro de la campana y DESTELLA
  con cada contracción del nado.
- **Tentáculos**: 6 tentáculos × 9 cuentas estelares (`JellyfishBead.png`:
  cuentas blancas tintables con destello de 4 puntas) con **física de cuerda
  propia**: anclas en el margen de la campana, relajación rígida cerca de la
  campana y laxa hacia la punta, gravedad suave, vaivén per-tentáculo y
  restricción de longitud — ondean con vida propia y quedan A LA ESTELA
  cuando la medusa nada. Colores bioluminiscentes alternos teal/rosa/menta.
- **Locomoción por PULSOS** (nadie más del arsenal se mueve así): cada 48
  ticks la campana SE CONTRAE (squash/stretch visual: scaleY -20%, scaleX
  +9%) y dispara un IMPULSO hacia su ancla; entre pulsos deriva con arrastre
  acuático (×0.955/tick) y hundimiento sutil. En caza nada hacia un punto
  SOBRE la víctima (impulso 6.6); en reposo cuelga del hombro del jugador
  (impulso 2.9, apilada con sus hermanas por minionPos). Fase de pulso
  desfaseada por minionPos → las medusas nunca pulsan al unísono.
- **EL ESPACIOTIEMPO**: la medusa es una fuente del pase B del
  `BlackHoleLensSystem` — la MÁS SUTIL del arsenal (fuerza 0.06→0.14
  RESPIRANDO con el pulso): donde nada, el fondo se dobla apenas. Su
  campana translúcida se dibuja ENCIMA de la lente (nunca deformada).
- **ATAQUE — NEMATOCISTOS**: cuando la contracción ocurre junto a una
  víctima (≤190 px) dispara 3 AGUJAS DE LUZ (`JellyfishStingBolt`: destello
  de 4 puntas + estela estelar, 12.5 px/tick, daño 50% del minion) con
  **QUEMADURA DE HIELO** (Frostburn — la quemadura fría del vacío, firma que
  NINGÚN otro arma cósmica usa) + daño de contacto de la campana (hitbox
  46×46, cooldown 18 ticks) con Frostburn al tocar.
- **Muerte**: se disuelve en polvo de estrellas (26 dusts teal/rosa).
- **Ciclo de sirviente**: patrón CosmicOrb — buff `NebulaJellyfishBuff`
  (localizado en ambos idiomas, icono 32×32 generado) aplicado por el Shoot
  del báculo y sostenido por la propia medusa (inmortal mientras el buff
  viva: timeLeft 2, semántica vanilla de minion). Teletransporte de vuelta
  si queda a >1100 px del dueño.
- **Icono del arma**: `MedusaNebularStaff.png` 28×30 (vara carbón-teal con
  la medusa encendida en la cima y estrellitas).

### D. Verificación

- Compilación contra tModLoader v2026.07.3.0 real (`/tmp/verify`): **0
  errores, 0 warnings**. Auditoría del binario: símbolos nuevos presentes
  (`NebulaJellyfishMinion`, `JellyfishStingBolt`, `MedusaNebularStaff`,
  `NebulaJellyfishBuff`, `StyleNova`, `DrawJellyfishVisuals`) y las tres
  rutas de textura del minion en el montón de user-strings.

### E. Prueba del usuario

Develop Mods → Build → kit de inicio (ahora con Báculo del Ojo del Vacío y
Báculo de la Medusa Nebular) → **SunStaff**: estrella → gigante roja → UNA
sola explosión: onda nova de lente (fuego + aberración cálida) curvando el
fondo → **BlackHoleStaff**: agujero → al morir: flash de liberación + EL
ANILLO DE EINSTEIN (nada de RGB) → **VoidEyeStaff**: el ojo se abre, te
mira, parpadea → EL GRITO: un único desgarro → **MedusaNebularStaff**: la
medusa emerge, su galaxia gira, NADA a pulsos colgando de tu hombro (con el
fondo curvándose sutilmente), y al acercarse a un enemigo dispara sus
nematocistos de hielo → client.log limpio.

## Commit v5.96 — El brillo del sol crece sin parpadear + ANILLO DE EINSTEIN + daño de área creciente + EL OJO DEL VACÍO

**Peticiones del usuario**: (1) "el PhoenixNovaStaff parpadea, creo que lo
mejor es que el brillo de PhoenixNovaStaff ya no parpadee, este debe comenzar
a crecer lentamente y que su crecimiento esté sincronizado con el ciclo de
vida del sol y con el tamaño del mismo"; (2) "en cuanto al agujero negro,
creo que es mejor que quites el campo de fuerza de las columnas, se ve mejor
si eso. En su lugar, al final de las explosiones debe crear un lente
gravitacional en forma de anillo que se expanda"; (3) "ahora todo el daño de
ambos proyectiles deben ser daño de área y este debe extenderse por fuera del
proyectil y crecer conforme el proyectil crece, se expande y explota"; (4)
"crea otra arma nueva de prueba con la que has aprendido y esta nueva arma
debe tener un proyectil lo más cósmico y de terror cósmico que se te ocurra,
lo dejo a tu imaginación y capacidad de creación".

### A. EL BRILLO DEL SOL YA NO PARPADEA — GLOW CORONAL PERSISTENTE

Las llamaradas PhoenixNova periódicas (una nova de 60 frames cada 2 s — cada
una nacía y moría: un PARPADEO por diseño) se **ELIMINARON por completo** del
sol. En su lugar, `SunProjectile.DrawStarVisuals` dibuja un **GLOW CORONAL
PERSISTENTE** (`DrawCoronalGlowSprites`): dos capas de SoftGlow aditivas que
- **nacen tenues con la estrella** (halo a 1.30× su radio visual, alpha 70),
- **crecen LENTO durante toda su vida** (función PURA de lifeT — CERO sin(),
  CERO flashes, CERO oscilación: halo hasta 2.35×, corona 1.05→1.55×),
- **se sincronizan con el TAMAÑO del sol**: todo se dimensiona con `starR`
  (width×scale×0.75) → la GIGANTE ROJA (×1.85) ARRASTRA al glow consigo,
- **enrojecen** con la gigante (Lerp naranja→rojo con rg).
La carga de la Supernova hija se sigue dibujando detrás del disco. La
PhoenixNovaStaff **standalone** también se suavizó: el pulso sinusoidal
(0.93±0.07) y el flash del pico (frames 25-35) se eliminaron — la nova nace
contenida (0.75×) y **crece de forma continua** hasta 2.3× con una
envolvente lisa (encendido 12f → plena → desvanecido 8f).

### B. ADIÓS CAMPO DE FUERZA — EL ANILLO DE EINSTEIN (agujero negro)

La burbuja Perlin/ForceField vanilla (shader de las Columnas Lunares) que
cabalgaba la onda cromática **se ELIMINÓ por completo** (textura, shader,
bloque de dibujado y el `localAI[0]` del radio del escudo — el binario queda
con 0 ocurrencias de ForceField/Perlin). **En su lugar**: cuando la onda
cromática del agujero **TERMINA de expandirse** (el final de la explosión),
engendra una onda **StyleEinstein** — el **LENTE GRAVITACIONAL ANULAR**:
- **Frente fino BLANCO incandescente** (el anillo de Einstein puro: la luz
  de todo lo que quedó detrás, doblada en un círculo perfecto, alpha 230).
- **Franjas R/B MUY juntas** (separación 1.8% vs 3.5-9.5% de la cromática —
  la imagen lensada se dispersa justo en el borde) + **halo interior pálido**
  (la luz lensada esmealada por dentro) + **imagen secundaria tenue** fuera.
- **Se expande MÁS RÁPIDO que la materia**: 26 px/tick con expansión CASI
  LINEAL (vs 20 ease-out de las ondas de materia) — es el ripple del
  espaciotiempo. Radio 520: SOBREPASA a la propia explosión.
- **CURVA EL FONDO del juego**: fuente del BlackHoleLensSystem con radio que
  ABRAZA al anillo (0.85× el frente) y fuerza que decae lento (0.9→0.4).
- **Banda de daño FINA** (0.88-1.06× el frente, cada 0.1 s) + **ShadowFlame**
  (la luz lensada quema el alma, no la carne).

### C. TODO EL DAÑO ES DAÑO DE ÁREA QUE CRECE (sol + agujero)

**SOL**: aura de daño cada 0.25 s (15 ticks) — radio = `starR×(1.35+0.30×
lifeT)` (110→205px, **se extiende POR FUERA del cuerpo** y CRECE con el
ciclo de vida), daño 40% (que ya rampa ×1.75 con la gigante), + OnFire que
**DOBLA en gigante** (600 ticks). La explosión final ya era área (3 ondas de
fuego con banda 0.72-1.02×frente cada 0.1 s + onda de lente + AoE del
núcleo). **AGUJERO NEGRO**: el aura pasa de fija (50% cada 0.5 s en
escudo×1.3) a **CRECIENTE**: radio = escudo×(0.75+0.45×lifeProgress)
(sobre la hinchazón +60% de la muerte, vía localAI[1] pre-colapso) y daño
35%→65% — y el clímax del área es la explosión (cromática + Einstein). La
**PhoenixNova standalone** también: aura 45→155px (60% + OnFire cada
0.166 s) creciendo con la expansión de la nova.

### D. EL OJO DEL VACÍO — VoidEyeStaff (arma nueva de terror cósmico)

**"Una estrella muerta con un ojo vivo"** — todo lo aprendido condensado en
un solo horror de 12 segundos (`VoidEyeProjectile`, 720 ticks):
- **CUERPO**: estrella MUERTA — el SunShader con **paleta invertida**
  (carbón oscuro + vetas carmesí: el gemelo maligno del sol). Emergencia
  SIN pop elástico (un peso siniestro: smoothstep lento).
- **OJO** (texturas generadas: EyeSclera/EyeIris/EyeLid 512px, supersampled
  ×4): esclerótica marfil enfermo con **VENAS ramificadas** e inyección de
  sangre en el terror; **iris ÁMBAR que ROTA lentamente** (los iris no
  deberían rotar) con estrías radiales y anillo limbal; **MIRA a la víctima**
  — el offset del iris SIGUE al enemigo más cercano… **y si no hay nadie,
  TE MIRA A TI** (al jugador).
- **PUPILA**: un **MICRO AGUJERO NEGRO** — el RealBlackHoleShader (lensing
  real de 75 pasos) en miniatura con su **disco de acreción CARMESÍ**. Se
  **DILATA** con el terror (0.55→1.35) y con ella crecen TODOS:
  el **aura de daño** (140→300px), la **lente gravitacional** (fuente del
  pase B, como la gigante roja: el espacio se curva alrededor del ojo) y la
  **gravedad del arrastre**.
- **PÁRPADOS de carne muerta**: se abren LENTO (smoothstep 0.8-3 s),
  **PARPADEAN** cada ~3.3 s — **en la oscuridad daña EL DOBLE** (es cuando
  alimenta) y la gravedad tira ×2.5 — y en el terror se **RETRAEN DE PAR EN
  PAR** (ojo desorbitado + iris ámbar→SANGRE).
- **AURA DE TERROR** (daño de área creciente, filosofía v5.96): 0.2 s al
  28-62% del daño + **ShadowFlame** + **ralentización por pavor** (×0.92/tick);
  en el terror: cada 0.1 s al 75%, radio 380px.
- **EL GRITO (muerte)**: chillido (ScaryScream) + AoE del núcleo (380px,
  ×1.6, ShadowFlame 8 s + Weak) + **ONDA CROMÁTICA INVERSA** (el mundo
  COLAPSA hacia el ojo muerto) + **ANILLO DE EINSTEIN** (el desgarro de la
  realidad, 12 ticks tras el colapso) + implosión de materia oscura +
  explosión de sangre + temblor fuerte.
- **Sonidos del horror**: MoonLord (nacimiento y terror), ZombieMoan
  (despertar, quejidos susurrados cada 2.8 s, parpadeos), ScaryScream (el
  grito). Lágrimas de sangre (DustID.Blood) desde el párpado inferior,
  zarcillos de materia oscura orbitando (librería: Orbit+ColorShift),
  brasa corrupta, llama sombría (DustID.Shadowflame).
- **MP coherente**: TODO (dilatación, apertura de párpados, terror) se
  deriva DETERMINISTA de la edad (ai[0]) — el parpadeo y el daño en la
  oscuridad coinciden en todas las máquinas sin sincronizar nada.

### E. Tooltips actualizados

SunStaff (glow coronal que crece + aura de área), BlackHoleStaff (aura
creciente 35→65% + el anillo de Einstein al final de la explosión),
PhoenixNovaStaff (crecimiento continuo + aura), VoidEyeStaff (nuevo).

**Arte nuevo**: EyeSclera.png, EyeIris.png, EyeLid.png (512px, generados con
PIL supersampled ×4 — venas ramificadas procedurales, estrías radiales del
iris, carne muerta con margen carmesí), VoidEyeStaff.png (icono 28×30),
VoidEyeProjectile.png (1×1 transparente — dibujado 100% manual).

**Compilación**: verificada contra tModLoader v2026.07.3.0 real — 0 errores,
0 warnings. Auditoría del binario: 0 ocurrencias de ForceField/Perlin
(eliminación total confirmada), texturas del ojo presentes.

## Commit v5.95 — Efectos del sol DETRÁS de él + fix del error del agujero + el campo de fuerza COMO onda + lente del sol + ondas de lente

**Peticiones del usuario**: (1) "sus efectos SupernovaStaff y PhoenixNovaStaff
deben estar detras de el, ademas parece que tiene un extraño parpadeo que
supongo que es PhoenixNovaStaff el cual no esta detras del sol"; (2) "aumentar
el tamaño de PhoenixNovaStaff y que iguale el tamaño del sol"; (3) "dale al
sol un poco de lente gravitacional a medida que vaya creciendo como gigante
roja"; (4) el agujero negro "tiene un error" (IndexOutOfRangeException en el
client.log) "y el campo de fuerza debe ser usado como onda expansiva"; (5) "en
ambas explosiones del sol y agujero negro tambien debe de haber una onda
expansiva creada con lente gravitacional que tenga una ligera distorsion
cromatica en rgb".

### A. FIX DEL ERROR DEL AGUJERO NEGRO (IndexOutOfRangeException del client.log)

v5.94 guardaba el radio de la burbuja del campo de fuerza en
`Projectile.ai[3]` — **un índice que NO EXISTE**: el array `ai` de tModLoader
tiene SOLO 3 ranuras (0-2). Tres stack traces lo confirmaban (OnKill al
escribir + DrawWaveVisual al leer, desde PreDraw y desde la lente):
la burbuja del escudo JAMÁS llegó a dibujarse y el OnKill abortaba antes de
los sonidos/dusts/temblor. Ahora el radio viaja en `localAI[0]` (parámetro
visual de cliente que fija el OnKill local) con **fallback determinista**
derivado del radio máximo sincronizado (`ai[2]×0.22`) para clientes remotos
de MP. FIX adicional: `localAI[1]` (radio pre-colapso del escudo) se
**captura UNA SOLA VEZ** al empezar la evaporación — antes se recalculaba
cada tick con la escala ya colapsada y el "escudo que mantiene su tamaño"
decaía de 101px a 5px.

### B. EL PARPADEO DEL SOL — CAUSA RAÍZ (decompilación de Main.DrawProjectiles)

La llamarada (PhoenixNova) usaba `DrawBehind` → caché
`drawCacheProjsBehindProjectiles`… **pero NUNCA ponía `hide = true`**, y el
bucle principal de Terraria solo excluye a los proyectivos con `hide`:
la llamarada se dibujaba **DOS VECES por frame — una de ellas ENCIMA del
sol** con brillo aditivo duplicado = el "extraño parpadeo". Y la Supernova
hija (spawned después → índice MAYOR que el sol) se pintaba directamente
encima de la estrella. **Solución definitiva**: los hijos van con
`hide = true` (ni tML ni ningún mod — Luminance incluida — los dibuja) y
**EL SOL LOS DIBUJA ÉL MISMO** (`SunProjectile.DrawStarVisuals`, capa 0,
ANTES de sus propias capas): detrás del disco SIEMPRE, inmune al orden de
índices. El disco del SunShader (alpha≈1 en el cuerpo) los OCULTA en el
centro → la llamarada se lee como un **backlight real asomando por el limbo**
de la estrella. La SupernovaStaff standalone y la PhoenixNovaStaff
standalone conservan su dibujado propio.

### C. LA LLAMARADA IGUALA EL TAMAÑO DEL SOL

`DrawFlareSprites` se dimensiona con `starR`, el **radio visual REAL** de la
estrella invocadora (`width×scale×0.75` — crece con la gigante roja): el
núcleo caliente mide lo mismo que el disco (queda oculto tras él) y el halo
lo envuelve como backlight (≈2.6× su radio, extendiéndose con la edad). El
destello del pico (frames 25-35) pasó de "pantalla blanca completa" (el
parpadeo) a un **rim de luz suave** alrededor del limbo (alpha 120 tras el
sol; 235 standalone). Paleta: naranja solar → ROJO gigante (Lerp con el
progreso de la gigante). El pulso bajó a ±0.07.

### D. EL CAMPO DE FUERZA ES LA ONDA EXPANSIVA (agujero negro)

El escudo Perlin/ForceField **ya NO vive alrededor del agujero durante su
vida** (DrawForceField y su flash de golpe eliminados) — al explotar, la
burbuja **PARTE del radio que tenía el escudo al morir** (localAI[0]) y
**CABALGA el frente de la onda** expandiéndose con él
(radio = max(escudo, frente)) mientras se desvanece
(fade = 1-progress×0.9, brillo ×2→×1): la secuencia de destrucción del
escudo de una Columna Lunar **CONVERTIDA en onda expansiva**, acompañada de
las franjas R/G/B de aberración cromática. El aura de daño dentro del campo
se conserva (daño cada 0.5 s, radio del escudo ×1.3).

### E. LENTE GRAVITACIONAL DEL SOL EN LA GIGANTE ROJA (dos pases)

El shader de distorsión solo acepta UNA fuerza global, así que el
`BlackHoleLensSystem` ahora renderiza **DOS pases**: el pase A fuerte
(agujeros + ondas cromáticas/de lente, fuerza 0.55×max) y el **pase B débil
del sol** (fuerza 0.55×(progreso de la gigante×0.4) ≈ un cuarto del
agujero — "un poco de lente") sobre el resultado del pase A, con su propio
render target. El radio de la fuente = 1.4× el radio visual real de la
estrella (crece mientras se hincha). El sol (con llamarada y carga de la
nova) se dibuja **ENCIMA de la lente** (`DrawStarVisuals`) igual que el
núcleo del agujero. **Modo identidad**: si las fuentes desaparecen mientras
un sol sigue en pantalla, ese último frame los proyectiles que se saltaron
el pase del mundo se dibujan encima SIN distorsión (verificado contra el
decompile: el backbuffer contiene el mundo tras EndCapture) — sin él habría
un frame de invisibilidad (parpadeo) al morir la última fuente.

### F. ONDA DE LENTE GRAVITACIONAL EN AMBAS EXPLOSIONES (StyleLens)

Nuevo estilo 3 del CosmicShockwaveProjectile: **onda expansiva creada CON
lente gravitacional** con **ligera** distorsión cromática RGB (separación
×0.65 de la cromática) + anillo blanco tenue en el frente; se registra como
fuente del BlackHoleLensSystem → **curva el fondo del juego a su paso**; se
dibuja encima de la lente (como las cromáticas) y barre daño cada 0.1 s.
El sol la lanza en su OnKill SIN retardo (la onda gravitacional viaja
DELANTE de la materia), radio 400 — envuelve a sus 3 ondas de fuego
(240/300/360) como firma final del estallido. El agujero negro ya la tenía:
su onda cromática ES lente + RGB (más la burbuja ForceField de la sección D).
La SupernovaStaff standalone hereda el redimensionado que le faltaba:
ondas 360/450/540 → 240/300/360 y AoE 340 → 260.

## Commit v5.94 — El campo de fuerza REAL de las Columnas + Gigante Roja + anillos a su tamaño

**Peticiones del usuario**: (1) los anillos de alta calidad de v5.93 quedaron
DEMASIADO GRANDES — redimensionar los del sol y el agujero; (2) los anillos
son parte de la ONDA EXPANSIVA — solo deben salir AL FINAL (salían durante
toda la vida de ambos proyectiles, con imagen de prueba incluida); (3) el sol
al explotar debe CRECER EN ROJO — "una estrella amarilla que se convierte en
gigante roja y luego explota, todo esto haciendo que su daño en area crezca
junto con la estrella"; (4) el agujero negro debe mejorar los LÍMITES de su
daño en área; (5) "no hiciste el efecto que tienen los escudos de las
columnas en terraria... deberías comenzar a investigar profundamente el
código de terraria y de cualquier mods que tengan escudos o campos de fuerza".

### A. INVESTIGACIÓN PROFUNDA DEL CÓDIGO REAL DE TERRARIA (decompilación)

Entorno reconstruido desde cero (.NET 8 + ilspycmd + tModLoader v2026.07.3.0
descargado de GitHub). Decompile REAL de `Terraria.NPC` (112k líneas),
`Terraria.Main` (85k), `Terraria.Projectile` (93k), `MiscShaderData`,
`GameShaders` + grep global del assembly completo. **El mecanismo EXACTO del
escudo de las Columnas Lunares** (Main.DrawNPCDirect_Inner, torres
422/493/507/517):

- El escudo NO es una textura de burbuja: es **ruido Perlin**
  ("Images/Misc/Perlin", la textura del juego) en un **quad de 600×600**
  (sourceRect 0,0,600,600, origen 300,300) dibujado con el shader
  **`GameShaders.Misc["ForceField"]`** — registrado por DyeInitializer como
  `new MiscShaderData(Main.PixelShaderRef, "ForceField")`, sin imágenes extra.
- Batch EXACTO: Immediate + AlphaBlend + **PointWrap** + DepthStencil.Default
  + CullNone + transform del mundo.
- **Vivo**: alpha = fuerza·0.8 + 0.2; **flash al golpe** (el proyectil 629
  baja la fuerza y pone npc.ai[3]=1; el AI lo incrementa hasta 120): pop de
  escala +5% y UseColor(1+flash·0.5) durante 30 ticks.
- **Destruido** (ai[3] > 0 con fuerza 0): la burbuja se **EXPANDE hasta 2×**
  (escala·(1+grow), grow=min(t/30,1)), **brillo ×2** (UseColor(2)) y se
  **DESVANECE** (color de la DrawData = 1-sqrt(grow)) — la secuencia que el
  usuario pedía desde el principio.
- tModLoader expone `GameShaders.Misc` públicamente → **el mod usa el MISMO
  shader del juego con las MISMAS llamadas**: look idéntico garantizado.

### B. EL CAMPO DE FUERZA DEL AGUJERO NEGRO (el efecto REAL de las Columnas)

`BlackHoleProjectile.DrawForceField` REESCRITO con el mecanismo vanilla:
Perlin del juego + shader ForceField + geometría/parámetros exactos
(detalles en A). La "fuerza" del escudo = la carga hacia la muerte (alpha
0.2→1.0 en los 10 s); **flash** al absorber un golpe (OnHitNPC →
localAI[0]=1, timer de 30 ticks idéntico al npc.ai[3] de la torre); el radio
(2.2× el horizonte) **mantiene su tamaño durante la evaporación** (escala
pre-colapso en localAI[1]) como el escudo de una torre mientras muere; y al
explotar, el OnKill pasa ese radio a la onda cromática (ai[3]): **la onda
dibuja la burbuja del escudo EXPANDIÉNDOSE (2×) y DESAPARECIENDO** — la
destrucción del escudo de la Columna, con las franjas R/G/B de aberración
cromática encima. El inventado campo v5.93 (RingShieldNebula + aros finos)
se ELIMINÓ (era "un anillo que sale en todo momento", justo lo que el
usuario no quería).

### C. AURA DE DAÑO DEL CAMPO (límites de daño en área mejorados)

El agujero antes solo dañaba por contacto (hitbox 96 px) y en la onda final.
Ahora el CAMPO DE FUERZA desgasta a todo enemigo atrapado dentro: **daño del
50% cada 0.5 s** dentro del radio del escudo ×1.3 (crece con la hinchazón
de la muerte, +60%), con empuje suave hacia el centro. La gravedad los
arrastra, el campo los exprime y la onda final los barre.

### D. GIGANTE ROJA (el sol antes de explotar)

Últimos 3 s (t=7-10): la estrella amarilla se **HINCHA hasta ×1.85**
(smoothstep continuo — sustituye a la compresión ×1.0008 y al hinchazón
final ×1.025) y **ENROJECE TODO**: backglow, aura RadialShine, cuerpo
SunShader (mainColor/darkerColor), luz, dusts y partículas de la librería
(tinte `ToRedGiant`). **El daño de área crece con la estrella**: hitbox de
contacto ×1.85 (Projectile.Resize, mantiene el centro — verificado en el
código de Terraria) y daño ×1.75 (base en ai[2]), quemadura 10 s.

### E. ANILLOS: SOLO EN LA EXPLOSIÓN FINAL + REDIMENSIONADOS

Causa raíz de "demasiado grandes y en todo momento": v5.93 reemplazó
Ring.png de 64→1024 px y TODOS los dibujados con escala fija quedaron 16×:

- **Sol en vida** (los anillos naranjas gigantes de la captura):
  PhoenixNova dibujaba 5 anillos por llamarada (hasta 4710 px) y la Supernova
  sus anillos de contención (hasta 2458 px) → **ELIMINADOS** (la llamarada
  queda como explosión de brillo: halo + núcleo + flash; la carga como halo
  dorado condensándose + temblor).
- **Agujero en vida** (el anillo cian gigante de la captura): anillo de
  fotones pulsante cada 36 ticks (1178 px) → **ELIMINADO**; campo v5.93 →
  reemplazado por el ForceField real (B).
- **Ondas**: sol 360/450/540 → **240/300/360**; agujero 620 → **420**;
  AoE del núcleo del sol 340 → 260; pulsos de la librería 280/380 → 200/270.
- **Librería (ParticlePresets)**: `radius/64` asumía la textura de 64 px →
  **`radius/512`** (radio real de la textura HD) en Explosion y RingPulse.
- **V20**: AbyssalEye (2.0→0.125), GravityPulse (Lerp 0..5→0..0.3125),
  Earthquake (÷16) y BlackHoleMini (0.35→0.0219) — restaurados a su tamaño.
- **DrawFallback del agujero**: anillo de fotones ahora por radio (1.3× el
  horizonte) en vez de escala fija 0.6.

**Compilación verificada contra tModLoader real (v2026.07.3.0): 0 errores,
0 warnings.**

## Commit v5.93 — El campo de fuerza de la Columna de Nebulosa + anillos de ALTA CALIDAD

**Peticiones del usuario**: el anillo del agujero negro (Ring.png) tenía MUY
baja calidad y era solo blanco; el agujero negro necesita el CAMPO DE FUERZA
de la Columna de Nebulosa (https://terraria.wiki.gg/es/wiki/Columna_de_nebulosa)
— la burbuja con aberración cromática que rodea al pilar y que AL DESTRUIRSE
SE EXPANDE Y DESAPARECE; los anillos de fuego del sol también debían mejorar
su calidad. "Si te faltan assets puedes generarlos o buscarlos y recrear tus
versiones."

### A. DIAGNÓSTICO DE LA CALIDAD

El `Ring.png` era de **64×64 px** — al escalarlo al radio de la onda (hasta
620 px) se pixelaba y su anillo fino teñido se veía como línea blanca plana.
Análisis del escudo real del Nebula Pillar (búsqueda de imágenes + VLM sobre
capturas del juego): burbuja translúcida con **borde exterior cian-azul
brillante, cuerpo magenta, interior rosado**, textura interna de energía,
semitransparente — la aberración vive en el BORDE.

### B. TEXTURAS NUEVAS (1024×1024, generadas proceduralmente — funciones suaves, cero aliasing)

1. **`Ring.png` (REEMPLAZO directo, 105 KB)**: misma geometría del viejo
   (núcleo fino, teñible blanco) pero a 1024 px con halo suave y modulación
   de energía sutil. Los 9 usos existentes (nova, PhoenixNova, librería de
   partículas, V20...) ganan calidad automáticamente.
2. **`RingShieldNebula.png` (nueva)**: el CUERPO del campo de fuerza con
   COLOR horneado — gradiente radial rosa interior → magenta → cian brillante
   en el borde + arcos de energía + wisps nebulosos.
3. **`FireRing.png` (nueva, 356 KB)**: anillo de LLAMAS con color propio
   (núcleo blanco-amarillo incandescente → naranja → rojo profundo en las
   puntas) con lengüetas internas/externas moduladas por fBm periódico.
   Bug de generación corregido en el proceso (clamp01 recortaba los canales
   0-255 a 1 → textura negra) + fix del corte de borde (todo el contenido
   queda ≤ 0.995 del canvas).

### C. EL CAMPO DE FUERZA DEL AGUJERO NEGRO (estilo Nebula, durante su vida)

`BlackHoleProjectile.DrawForceField` (llamado desde `DrawCoreVisuals` — funciona
en el pase del mundo Y encima de la lente): burbuja a 1.9× el horizonte
(envuelve el disco de acreción) = **cuerpo RingShieldNebula translúcido +
aros FINOS cian (exterior) y rosa (interior) cuya separación "respira"**
(±5-6.5% del radio, proporcional). El radio sigue al horizonte → crece con la
secuencia de evaporación → al morir, la onda cromática del OnKill continúa la
historia: el campo "destruido" expandiéndose hasta desvanecerse.

### D. LA ONDA CROMÁTICA = EL CAMPO EXPANDIÉNDOSE (y las de fuego del sol)

`DrawWaveVisual` reescrita con el diseño VALIDADO POR SIMULACIÓN (VLM +
estadísticas de píxel: el primer intento de 3 pasadas RGB de banda ancha se
LAVABA a blanco porque la base de la banda solapaba al 100% en additive):

- **Cromática (agujero)**: cuerpo translúcido RingShieldNebula (tenue) + TRES
  AROS FINOS R/G/B con desfase radial 3.5%→9.5% del frente (crece con la
  edad = dispersión real; invertido en la convergente legada) — franjas de
  aberración SEPARADAS y VISIBLES sobre el cuerpo, sin lavado.
- **Fuego (sol)**: FireRing ×2 pasadas (exterior a 1.08·front, interior a
  0.93·front) + Ring fino blanco como frente de choque — llamas reales con
  lengüetas y gradiente de color propio.
- Compensación `thinComp = 1/0.92` (el núcleo del Ring vive a 0.92 del radio
  de la textura → un radio pedido R aparece exactamente a R).

### E. VERIFICACIÓN

- Simulación del look in-game validada con VLM: campo = "translucent magenta
  body, bright cyan edge, pink inner ring, Nebula Pillar style, saturated
  colors"; onda = "red/green/blue rings clearly separated over translucent
  body". Estadísticas de saturación: 74-79 (colores vivos, no blanco).
- Compilación contra tModLoader v2026.07.3.0 real: **0 errores, 0 warnings**.
- `RingShield.png` (versión blanca intermedia) eliminada: sin referencias.

## Commit v5.92 — FIX: "el sol dio un error" (InvalidOperationException del SpriteBatch)

**Reporte del usuario**: el client.log mostraba 2 "Excepción silenciosa" por cada
explosión del sol:

```
System.InvalidOperationException: Begin has been called before calling End after
the last call to Begin. Begin cannot be called again until End has been
successfully called.
   at Microsoft.Xna.Framework.Graphics.SpriteBatch.Begin(...)
   at AethonMod.Content.Projectiles.Cosmic.CosmicShockwaveProjectile.PreDraw(...)
   at Terraria.Main.DrawProj_Inner / DrawProjectiles / Draw ...
```

### A. CAUSA RAÍZ (trazada tick a tick)

1. El `OnKill` del sol genera sus 3 ondas de fuego con **retardo escalonado**
   (`ai[0] = 0, -8, -16` — así la explosión es una secuencia, no un solo destello).
2. Mientras la edad es negativa, `PreDraw` retorna temprano (`Age < 0f`) sin tocar
   el `spriteBatch` → correcto.
3. **El tick EXACTO en que un retardo expira** (edad pasa de -1 a 0):
   `FrontRadius(0, estilo, maxR) = maxR·(1-(1-0)²) = 0 px` → `DrawWaveVisual`
   entra en su salida temprana `if (front <= 1f) return;` y **devuelve SIN tocar
   el `spriteBatch`** — que sigue ABIERTO (el del pase del mundo de Terraria).
4. De vuelta en `PreDraw`, el `Begin` de restauración **INCONDICIONAL** (línea 367
   de v5.91) intentaba re-abrir un batch **YA ABIERTO** → `InvalidOperationException`.
5. Ese `Begin` estaba FUERA del try/catch → la excepción subía hasta
   `Main.DrawProjectiles` → tML la registraba como "Excepción silenciosa" y
   **abortaba el dibujado de TODOS los proyectiles del frame** (parpadeo/pérdida
   de efectos un frame). Ocurre 2 veces por explosión (ondas con retardo -8 y
   -16); la onda cromática del agujero negro nace SIN retardo (`ai[0]=0`, su edad
   jamás es 0 en el PreDraw) → por eso SOLO el sol disparaba el error.

### B. FIX (CosmicShockwaveProjectile.cs)

- `DrawWaveVisual` ahora **devuelve `bool`** en vez de `void`:
  - `false` = NO tocó el batch (onda inactiva o frente aún invisible) → el
    llamador NO debe restaurar nada;
  - `true` = lo tomó y lo dejó **CERRADO** (su `End` propio, o el defensivo del
    catch) → el llamador debe re-abrirlo con los parámetros estándar de tML.
- `PreDraw` restaura el batch **SOLO cuando `DrawWaveVisual` devuelve `true`**;
  cuando devuelve `false` el batch sigue exactamente como tML lo dejó (abierto en
  el pase del mundo) → no hay nada que re-abrir y el bug desaparece.
- El `BlackHoleLensSystem` (que llama `DrawWaveVisual(wave, false)` con el batch
  ya cerrado) sigue siendo compatible: ignora el valor de retorno y en ambos
  casos recibe el batch cerrado, tal como espera.
- Invariante nueva verificada en TODOS los caminos: edad negativa / frente
  invisible / dibujado completo / excepción interceptada → el `spriteBatch` nunca
  queda desbalanceado.

### C. VERIFICACIÓN

- Compilación contra tModLoader v2026.07.3.0 real: **0 errores, 0 warnings**.
- Auditados los Begin/End de los demás efectos del arsenal cósmico (sol, nova,
  agujero, PhoenixNova, lente): sus salidas tempranas ocurren ANTES de tocar el
  batch → ningún otro proyectil tiene este patrón.

## Commit v5.91 — El SOL autorita su explosión final (Supernova sincronizada) + agujero negro orientado al centro + ondas que dañan cada 0.1 s

Recordatorio: Puedo coger los recursos de nuestro github si los datos de mi versión local se borran

**Peticiones del usuario** (aprobadas en el plan v5.91, "ejecuta todo lo demás"):

1. *"La explosión final es SupernovaStaff, puedes verificar en commit anteriores,
   esta debe estar sincronizada y debe ser la explosión final, las partículas
   deben ser del color del sol"* — verificado en el historial: el sol SIEMPRE
   invocó `SupernovaProjectile` (la estrella del SupernovaStaff) en su segundo 7.
   Ahora la sincronización es POR CONSTRUCCIÓN y las partículas son doradas.
2. *"Las partículas absorbidas por el agujero negro deben estar ubicadas en
   dirección hacia el centro del agujero"* — antes viajaban TANGENCIALES.
3. *"La lente gravitacional debe ser ligeramente más grande"* — 1.1× → 1.4×.
4. *"La aberración cromática debe ser transparente"* — alpha 230 → 140.
5. *"La velocidad de las partículas debe acelerarse en el momento de explotar"* —
   PullToGlobalBoost (hasta ×6 en la evaporación).
6. *"La onda expansiva (del agujero negro y del sol) debe hacer daño a medida
   que avanza, daño en área y daño por cada 0.1 segundo"* — daño por BANDA del
   frente cada 6 ticks.
7. *"El agujero negro tiene más fuerza de atracción que el sol, y el sol solo
   tiene 10 veces menos fuerza de gravedad"* — confirmado y documentado: 2.6
   vs 0.26 (el pico de carga del sol ×4 = 1.04 jamás lo alcanza).
8. *"El efecto PhoenixNovaStaff debe estar detrás del sol"* — DrawBehind.
9. *"La onda expansiva del sol debe quemar causando el debuff quemadura por
   10 segundos"* — OnFire 600 ticks en todas las golpes de la explosión final.

### A. SOL — LA EXPLOSIÓN FINAL ES LA SUPERNOVA, SINCRONIZADA POR CONSTRUCCIÓN

**Diagnóstico** (decompilando Terraria.Projectile/Main contra tML v2026.07.3.0):
el diseño v5.88 hacia depender la onda de fuego del `OnKill` de la Supernova
hija, que a su vez dependía de la sincronización por ÍNDICE (`ai[1]`) + un clamp
de `timeLeft` en los últimos 2 ticks — un punto único de fallo: cualquier
desviación (índice reciclado, clamp que no aplica, muerte descuadrada) y la onda
se perdía o estallaba fuera de tiempo ("no se procesó correctamente o no se ve").

**Rediseño v5.91** (SunProjectile.OnKill es ahora la AUTORIDAD — el sol muere
EXACTAMENTE a los 10 s, su timeLeft es fijo y nada lo mata antes):

1. **`TryKillSupernova()`**: el OnKill del sol mata la Supernova hija EN EL MISMO
   TICK → su flash + estallido + viento estelar ocurren EXACTAMENTE con la muerte
   del sol. Sincronización perfecta sin depender de índices ni clamps (el clamp
   `timeLeft <= 2` se conserva como red de seguridad).
2. **Las 3 ondas de fuego las genera EL SOL** (radii 360/450/540, retardo de 8
   ticks) con el daño de la nova (sol × 1.25 × 0.5) — cada una barre daño cada
   0.1 s + quemadura 10 s (ver sección C).
3. **AoE del núcleo** (340 px) con el daño de la nova (sol × 1.25) + quemadura 10 s.
4. **Flag `ai[2]=1` (SunInvoked)**: la nova hija NACE marcada como "invocada por
   el sol" → su OnKill NO genera ondas/AoE propios (cero dobles explosiones);
   aporta SOLO el espectáculo final (flash + estallido + viento + temblor).
   La SupernovaStaff standalone (`ai[2]=0`) conserva su explosión COMPLETA.

### B. SOL — PARTÍCULAS DEL COLOR DEL SOL (SupernovaProjectile)

La carga de la nova ya NO se blanquea hacia el azul (255,255,245 de v5.88):
- Halo: dorado (255,200,90) → **blanco dorado** (255,235,115) — sin canal azul.
- Núcleo: **blanco-dorado** (255,250,215).
- Luz de carga: familia cálida (1, 0.85→0.8, 0.55→0.45) — se intensifica sin
  virar al azul, como la corona del SunShader.
- Quemadura de la nova al contacto: 5 s → **10 s** (600 ticks).

### C. ONDAS EXPANSIVAS — DAÑO CADA 0.1 s A MEDIDA QUE AVANZAN (ambas armas)

`CosmicShockwaveProjectile.ApplyWaveDamage` reescrito:

- ANTES (v5.90): un bool[] `_hitNPCs` marcaba a cada NPC golpeado UNA sola vez
  en toda la vida de la onda (daño cuando el frente lo alcanzaba).
- AHORA (v5.91): un int[] `_nextHitAt` de COOLDOWNS — cada NPC dentro de la
  **BANDA del frente** (el anillo visible que avanza: [0.72·front, 1.02·front]
  en expansivas; [0.98·front, 1.25·front] en la convergente legada) recibe
  daño cada **6 ticks = 0.1 s EXACTOS** mientras la onda lo barre. La onda
  expansiva nace en el centro (radio 0) → el disco completo queda cubierto
  ("daño en área"). `SimpleStrikeNPC` no usa immunity frames → cada tick de la
  banda registra un golpe limpio (verificado decompilando NPC.StrikeNPC).
- **Estilo Fuego (sol/SupernovaStaff): quemadura 10 s** (OnFire 600, era 300).
- **Estilo Cromático (agujero negro): TRANSPARENTE** — canales RGB alpha
  230 → 140, núcleo blanco 150 → 95: un velo que deja ver el mundo a través
  del anillo (antes era un anillo aditivo casi opaco).
- `NewInstance`/`SetDefaults` siguen dando a cada onda su array FRESCO
  (MemberwiseClone comparte arrays del prototipo — bug v5.88 documentado).

### D. AGUJERO NEGRO — MATERIA ORIENTADA AL CENTRO + ACELERACIÓN AL EXPLOTAR

1. **Orientación al centro** (`SpawnLibraryAbsorbedMatter`): las estelas ya no
   viajan tangenciales — nacen con velocidad RADIAL hacia dentro (inward
   1.8-2.8 + tangencial sutil 0.25-0.5 para la espiral de infalling) y su eje
   largo apunta AL CENTRO (`Rotation = angle + π`): "ubicadas en dirección
   hacia el centro del agujero".
2. **Aceleración al explotar**: nuevo `ParticleManager.PullToGlobalBoost` —
   el agujero lo dispara durante su secuencia de muerte
   (`1 + expansion·5`, hasta ×6 en la evaporación): TODAS las partículas
   absorbidas aceleran hacia el centro justo en el momento de explotar. El
   OnKill lo resetea a 1f (nunca queda "colgado"). El polvo dorado también se
   acelera (deathSpeedBoost hasta ×3).
3. **Lente ligeramente más grande**: radio 1.1× → **1.4×** el tamaño visual
   (BlackHoleLensSystem) — el ángulo pico SE MANTIENE en ~0.8 rad: sigue siendo
   delgada, solo el anillo de deformación abraza el disco de acreción completo.
4. **Vida 10 s confirmada** (600 ticks, ya estaba) y **gravedad 2.6 = 10× la
   del sol** (0.26) documentado en el propio código.

### E. PHOENIXNOVA — DETRÁS DEL SOL + FIX CRÍTICO DEL SPRITEBATCH

1. **DrawBehind → `drawCacheProjsBehindProjectiles`** (firma verificada por
   reflexión contra tModLoader real): tML dibuja esa cache ANTES de
   `DrawProjectiles()` (verificado decompilando Main.DrawCachedProjs, línea del
   pipeline: DrawNPCs → … → DrawCachedProjs(BehindProjectiles) →
   DrawProjectiles) → la llamarada queda DETRÁS del cuerpo del sol y por
   delante de los NPC. El disco de la estrella tapa el núcleo de los anillos y
   estos se abren alrededor de la silueta — una llamarada ERUPCIONANDO por
   detrás (el flash queda como backlight). A diferencia de la v5.89 (que usaba
   `hide` + movió TODOS los dusts a otra capa y rompió el sol entero), este
   cambio es quirúrgico: SOLO el orden de dibujado del proyectil llamarada.
2. **Fix del bug del SpriteBatch (presente desde v5.88 — causa real de los
   "círculos que subían")**: el PreDraw del PhoenixNova abría el batch SIN
   `Main.GameViewMatrix.TransformationMatrix` (y sin sampler/rasterizer) y el
   `Begin(Deferred, AlphaBlend)` final restauraba el batch SIN TRANSFORMAR para
   TODOS los proyectiles posteriores del frame → dibujados en coordenadas de
   mundo sin la vista, "flotando/subiendo" por la pantalla. Ahora todos los
   Begin llevan el transform del mundo y el restore es EXACTO al estado que
   tML espera (Deferred, AlphaBlend, DefaultSamplerState,
   CullCounterClockwise, GameViewMatrix) — el mismo patrón del resto del mod.

### F. VERIFICACIÓN

- COMPILACIÓN contra tModLoader v2026.07.3.0 real (/tmp/verify): **0 errores,
  0 warnings**.
- Revisión de referencias huérfanas: `_hitNPCs` (renombrado `_nextHitAt`) sin
  usos restantes; el hook `DrawBehind` existe en esta versión de tML con la
  firma exacta usada; `Kill()` llama `ProjectileLoader.OnKill` incondicionalmente
  (la fuerza-kill de la nova dispara su espectáculo en el mismo tick).

**Prueba del usuario**: Develop Mods → Build → lanzar el SunStaff: llamaradas
ERUPCIONANDO POR DETRÁS de la estrella (t=2,4,6,8 s), carga dorada (t=7-10 s,
sin azul), y a los 10 s EXACTOS la Supernova estalla con el sol (flash dorado +
3 ondas de fuego que BARRAN dañando cada 0.1 s + quemadura de 10 s). Lanzar el
BlackHoleStaff: materia ámbar CAYENDO AL CENTRO (radial), lente más ancha,
aceleración frenética de la materia al evaporarse y UNA onda cromática
transparente que barre daño cada 0.1 s. Sin "círculos que suban" con otros
proyectiles en pantalla.

---

## Commit v5.90 — REVERT del sol a v5.88 + agujero negro rehecho (partículas absorbidas, UNA explosión cromática, lente delgada, sin corte)

Recordatorio: Puedo coger los recursos de nuestro github si los datos de mi versión local se borran

**Reporte del usuario** (tras probar la v5.89 en juego):

1. *"Creo que ahora empeoró ya que arruinaste los efectos del sol"* — la v5.89
   sustituyó los dusts ambientales del sol por partículas de la librería en la
   capa BeforeProjectiles: el humo cálido se convirtió en **círculos (SoftGlow)
   que suben desde el sol** y las llamaradas (PhoenixNova con hide+DrawBehind)
   quedaron TAPADAS por el cuerpo de la estrella → *"le quitaste su onda
   expansiva"*. El usuario pidió **volver al commit 8781aa4** (v5.88) para el
   sol, sin tocarlo, y aplicar el resto de los cambios.
2. *"En cuanto al agujero negro, al crecer para desaparecer se CORTA por los
   lados"* — la captura mostraba el anillo naranja/blanco del disco de acreción
   terminado en **líneas verticales duras** a izquierda y derecha.
3. Cambios pedidos para el agujero negro: quitar las partículas moradas,
   agregar partículas que parezcan absorbidas, quitar las explosiones
   cromáticas (que solo haya UNA al desaparecer, con aberración cromática y
   daño), y hacer DELGADA la lente gravitacional para que no mueva toda la
   pantalla.

### A. REVERT DEL SOL A 8781aa4 (v5.88) — SunProjectile, Supernova, PhoenixNova

- `git checkout 8781aa4 -- SunProjectile.cs SupernovaProjectile.cs PhoenixNovaProjectile.cs`
  — el sol vuelve EXACTAMENTE a su estado de la v5.88: dusts vanilla (llamas
  GoldFlame, chispas Torch, humo Smoke, twinkles Enchanted_Gold), supernova
  hija dibujándose por sí misma (sin ai[1]=1), llamaradas PhoenixNova en el
  pase normal de proyectiles (sin hide/DrawBehind), y su onda expansiva de
  fuego intacta.
- Se conservó UNA sola cosa de la v5.89 en esos archivos: el fix de la
  **"Excepción silenciosa"** (el End defensivo ahora vive SOLO en el catch del
  path de error, no un try{End} incondicional cada frame) — cambio invisible
  al juego que mantiene el client.log limpio.

### B. FIX DEL CORTE POR LOS LADOS (disco de acreción al crecer)

**Causa raíz** (verificada contra el .fx): el canvas del RealBlackHoleShader
era FIJO de 256px y la lupa interna (`zoom = width/256 * scale * 2`) crecía
con la escala. La cobertura del shader en unidades de mundo es `1/zoom`: al
hincharse para morir (scale hasta 1.6) la cobertura caía a 0.83 mientras el
toro del disco de acreción crecía hasta 1.39 → el disco cruzaba el borde del
canvas y quedaba recortado con líneas verticales duras exactamente donde
terminaba el quad.

**Fix**: el canvas AHORA CRECE con la escala (`256 * max(scale, 0.08)`) y el
zoom es CONSTANTE (`width/256*2`). La cobertura del shader ya no cambia y el
radio del disco lleva un tope (`min(scale,1)*0.4`) → el toro NUNCA cruza el
borde a ninguna escala. El horizonte de sucesos en píxeles es idéntico a
antes (0.3 · width · scale): cero cambio visual salvo que el disco ya no se
corta — el agujero entero crece en pantalla al hincharse.

### C. PARTÍCULAS: FUERA LAS MORADAS, DENTRO LAS ABSORBIDAS

- ELIMINADO: succión espiral multicolor (violeta/cian/magenta con PurpleTorch),
  humo púrpura, espiral de librería multicolor (end violeta), halo de
  distorsión violeta (era el ÚNICO usuario de Noise.png en juego — el
  "efecto que se veía mal" desaparece por completo).
- NUEVO componente de librería **PullTo** (ComponentFlag bit 13): aceleración
  hacia un punto fijo (UserData0/1 = centro, UserData3 = fuerza); la partícula
  MUERE al llegar al centro. Con velocidad inicial tangencial dibuja una
  espiral de infalling perfecta.
- NUEVO **SpawnLibraryAbsorbedMatter**: estelas TrailGlow cálidas (ámbar →
  blanco incandescente vía ColorShift) que nacen a 95-165px con velocidad
  tangencial y PullTo hacia el centro — caen en espiral cada vez más rápido y
  desaparecen al cruzar el horizonte: materia siendo ABSORBIDA.
- NUEVO **SpawnAbsorbedDusts**: polvo GoldFlame ámbar/oro/brasa en espiral
  (mismo movimiento de la vieja succión, paleta incandescente), blanco
  incandescente cerca del borde.
- RECARENTADOS a cálido: halo exterior del núcleo (púrpura → ámbar profundo),
  presets de OnHitNPC/OnKill (violeta → ámbar/blanco), dusts de implosión
  (PurpleTorch → GoldFlame).
- Se conservan: chispas doradas capturadas, disco de acreción naranja
  (TrailGlow orbital), anillo de fotones blanco-azul, devoración de dusts
  ambiente, luz naranja pulsante.

### D. UNA SOLA EXPLOSIÓN CROMÁTICA AL DESAPARECER (con daño)

- ELIMINADAS las 4 ondas de la secuencia de muerte v5.86: la onda cromática
  intermedia (t-90, con estruendo y sacudida) y las 3 ondas cromáticas
  inversas escalonadas del final. La secuencia ahora es pura: crecimiento
  (+60% escala y radio de gravedad, SIN ondas) → evaporación (colapso a 0) →
  explosión.
- En **OnKill** nace UNA SOLA `CosmicShockwaveProjectile` estilo 0
  (cromático) con el **daño COMPLETO** del proyectil y radio máximo 620px:
  anillo con **aberración cromática real** (canales R/G/B separados
  radialmente, separación creciente con la edad) + **distorsión del fondo a
  su paso** (se registra como fuente del BlackHoleLensSystem) + **daño por
  frente de onda** a cada NPC una única vez. Estruendo Item88 y sacudida
  de cámara acompañan la liberación.
- El estilo 1 (inverso) queda documentado como LEGADO sin uso.

### E. LENTE GRAVITACIONAL DELGADA (ya no mueve toda la pantalla)

**Causa raíz** (verificada contra el .fx): el shader rota las coordenadas de
muestreo ALREDEDOR DEL CENTRO DE PANTALLA (`RotatedBy(coords - 0.5, ángulo)`),
así que un píxel se desplaza proporcional a SU distancia al centro. La v5.89
usaba `maxLensingAngle = 24 rad` con fuerza 0.62 → ángulo pico ~14.9 rad: a
2 radios del agujero el ángulo seguía siendo 0.27 rad → píxeles a 800px del
centro se movían 213px. Eso era "mover toda la pantalla".

**Fix (solo parámetros — el .fxc no se puede recompilar sin mgfxc/wine)**:
- `maxLensingAngle`: 24 → **1.5 rad** y fuerza 0.62 → **0.55** → ángulo pico
  **~0.8 rad** (18× menos).
- Radio de influencia: 0.75× → **1.1×** el tamaño visual (con ángulos sanos,
  el anillo de distorsión debe abrazar el borde del núcleo para seguir siendo
  visible; con 0.75 quedaría oculto tras el disco de acreción).
- Resultado (1080p, agujero a escala 1): deformación visible en el anillo
  0-180px alrededor del agujero, imperceptible (<1px) más allá de ~3 radios,
  resto de la pantalla pixel-perfect. La explosión cromática final también
  distorsiona su interior con el mismo tope sano.

### F. Documentación

- Headers de BlackHoleProjectile/BlackHoleLensSystem/CosmicShockwaveProjectile
  reescritos para documentar la v5.90 (secuencia de muerte, PullTo, canvas que
  crece, lente delgada).
- ParticleData/ParticleManager: PullTo documentado en la lista de componentes.

---

## Commit v5.89 — FIX CRÍTICO: la "pantalla negra" del agujero negro + efectos del sol DETRÁS de la estrella

Recordatorio: Puedo coger los recursos de nuestro github si los datos de mi versión local se borran

**Errores reportados por el usuario** (captura + client.log de la v5.88 en juego):

1. *"Mira como se ve el agujero negro... toda la pantalla se oscurece y tiene
   algún problema de transparencia"* — la captura mostraba el mundo visible SOLO
   dentro de un cuadrado alrededor del agujero; el resto de la pantalla era
   NEGRO PURO (esquinas a RGB 0,0,0). Además, dentro del cuadrado el mundo se
   veía pixelado (media resolución) con bordes duros y banding.
2. *"En el caso del sol, sus efectos deben estar detrás del sol"* — las llamas,
   chispas, humo y la carga de la supernova se dibujaban ENCIMA del cuerpo de
   la estrella tapándola.
3. *"Debes mejorar el archivo Noise.png para que sea más suave"* — el ruido del
   halo de distorsión era grano duro y se veía mal.
4. El client.log registraba "Excepción silenciosa:
   InvalidOperationException: End was called, but Begin has not yet been called"
   en SupernovaProjectile:364, CosmicShockwaveProjectile:334, SunProjectile:658
   y BlackHoleProjectile:598.
5. Warning FNA al empaquetar: "Image loading failed: unknown image type".

### A. LA CAUSA RAÍZ de la pantalla negra (verificada decompilando FNA.dll)

La semántica oculta de FNA: **`GraphicsDevice.SetRenderTarget(null)` LIMPIA el
backbuffer** al volver a bindearlo. El final del método (decompilado de
`Microsoft.Xna.Framework.Graphics.GraphicsDevice` en FNA 1.0.0 de tML 2026.7.3):

```csharp
Viewport = new Viewport(0, 0, width, height);
if (renderTargetUsage == RenderTargetUsage.DiscardContents)
    Clear(ClearOptions.Target | DepthBuffer | Stencil, DiscardColor, ...);
```

Y `PresentationParameters` usa **DiscardContents por defecto** (verificado en
su constructor). Por eso el propio Terraria hace `Clear` + redraw completo en
su `FilterManager.EndCapture`: al bindear el backbuffer para presentar el mundo,
el contenido previo se destruye y hay que redibujarlo entero.

**El bug**: la lente v5.86-5.88 componía por REGIONES para "conservar el
backbuffer intacto fuera de la zona del agujero"... pero al restaurar el binding
(`SetRenderTarget(null)`) FNA ya había BORRADO el backbuffer. El mundo que
EndCapture acababa de dibujar se destruía y solo quedaban nuestras regiones →
pantalla negra con un cuadrado brillante, EXACTAMENTE lo que mostraba la captura
(cuadrado perfecto de 225×225 px = radio*2.2 del BuildRegion).

**El FIX (pipeline completo de pantalla)**:
- `_lensTarget` ahora a **RESOLUCIÓN NATIVA** (el shader de distorsión es barato:
  una sola lectura de textura por píxel; la media resolución era innecesaria).
- Tras restaurar el binding (el wipe es inevitable y esperado), se dibuja
  `_lensTarget` **A PANTALLA COMPLETA**: el mundo vuelve a pantalla a resolución
  nativa, distorsionado solo cerca de las fuentes. Es exactamente el mismo blit
  que hace el EndCapture de Terraria con screenTarget.
- ELIMINADA toda la lógica de regiones (BuildRegion/ClampRegion/srcRect):
  sin regiones, sin bordes duros, sin pixelado, sin banding de escalado.
  Código más simple y visual idéntico al pipeline de referencia.

### B. Los efectos del sol ahora van DETRÁS de la estrella

- **Dusts vanilla → partículas de la librería (capa BeforeProjectiles)**: los
  dusts de Terraria se dibujan en la capa de polvo (DESPUÉS de los proyectiles),
  es decir, ENCIMA del sol. Todas las partículas ambientales (chispas de la
  corona, llamas de la superficie, humo cálido, llamarada periódica) son ahora
  TrailGlow/SoftGlow de la librería propia, que se pintan en PostDrawTiles
  (ANTES de los proyectiles): se ven igual de bonitas pero DETRÁS del cuerpo.
- **La carga de la supernova detrás del sol**: la nova se invoca con `ai[1]=1`
  ("pertenece al sol") y YA NO se dibuja por sí misma (su índice de proyectil
  es mayor → se pintaba encima del sol). El SunProjectile pinta su carga
  PRIMERO (capa más profunda) vía el nuevo `SupernovaProjectile.DrawChargeVisuals`
  (método extraído y reutilizable). Invocada en solitario (SupernovaStaff),
  se dibuja ella misma como siempre.
- **Las llamaradas (PhoenixNova) detrás de todo**: `Projectile.hide = true` +
  override de `DrawBehind` enrutándolas a la capa `behindProjectiles` de
  Terraria (se dibuja ANTES que los proyectiles normales). El estallido ilumina
  ALREDEDOR de la estrella sin tapar su cuerpo.
- De regalo: el Begin aditivo de la llamarada ahora usa
  `Main.GameViewMatrix.TransformationMatrix` (antes Begin por defecto sin
  transform: con zoom ≠ 1 se descolocaba) y su restore deja el batch en el
  estado exacto que tML espera (con sampler y matriz del juego).

### C. "Excepción silenciosa" cada frame — patrón de End defensivo corregido

Las 4 stack traces del log (Supernova:364, CosmicShockwave:334, Sun:658,
BlackHole:598) venían del `try { Main.spriteBatch.End(); } catch { }` incondicional
del restore v5.88: el path NORMAL deja el batch CERRADO (todas las capas están
balanceadas Begin→End), así que el End defensivo lanzaba una
InvalidOperationException CAPTURADA cada frame. tML la registraba vía su handler
de first-chance exceptions ("Excepción silenciosa", deduplicada — por eso solo
4 entradas en todo el log).

**FIX**: el cierre defensivo ahora SOLO vive en los `catch` (el path de error,
donde de verdad puede haber un Begin interrumpido). El restore normal es un
Begin directo. Aplicado a BlackHoleProjectile, SunProjectile, SupernovaProjectile,
CosmicShockwaveProjectile y PhoenixNovaProjectile.

### D. Texturas de ruido suavizadas (Noise.png y FireNoiseB)

- **`Noise.png` REGENERADO** (128×128): antes era ruido gris duro (desviación
  estándar 42, rugosidad 4.2) con alfa totalmente opaco → grano visible en todo
  el halo de distorsión. Nuevo: ruido fractal multioctava suavizado con
  gaussian blur MODO WRAP (tileable sin costuras, el sampler es LinearWrap):
  stdev 12, rugosidad 0.58 — un halo brumoso y suave.
- **`FireNoiseB.png` suavizado** (512×512): el ruido del disco de acreción del
  agujero negro (único usuario de esta textura, verificado) tenía rugosidad
  8.0 → bandas/motear duro en el disco. Gaussian blur sigma 2.0 con wrap:
  rugosidad 2.38 — el disco fluye cremoso manteniendo su carácter.

### E. Warning FNA "Image loading failed: unknown image type" — ELIMINADO

Los 23 archivos de `Content/_masters/` eran **JPEGs con extensión .png** (2.1 MB):
tML los empaquetaba en el .tmod e intentaba cargarlos como PNG → warning de FNA
en cada build y 2.1 MB de basura dentro del mod. **MOVIDOS a `_masters/` en la
raíz del repo** (fuera de la carpeta del mod): siguen en git como arte de
referencia, pero ya no se empaquetan ni rompen el loader.

### F. Prueba del usuario (v5.88 → v5.89)

1. `Develop Mods → Build` (limpio, sin el warning de imagen de FNA).
2. Lanzar el agujero negro: **la pantalla ya NO se ennegrece** — el mundo se ve
   entero a resolución nativa con la distorsión gravitacional alrededor del
   agujero, y el log ya no acumula "Excepción silenciosa".
3. Lanzar el sol: los efectos (llamas, chispas, humo, llamaradas, carga de la
   nova) se ven DETRÁS del cuerpo de la estrella — el disco solar queda limpio
   y visible todo el ciclo.
4. La supernova del Grimorio/staff suelta sigue viéndose igual.

---

## Commit v5.88 — FIX CRÍTICO: el mod NO CARGABA (textura faltante) + revisión profunda (10 pasadas)

Recordatorio: Puedo coger los recursos de nuestro github si los datos de mi versión local se borran

**Error reportado por el usuario** (captura + client.log al cargar v5.87):

```
Terraria.ModLoader.Exceptions.MissingResourceException: Recurso esperado no
encontrado: Content/Projectiles/Cosmic/CosmicShockwaveProjectile
  at Terraria.ModLoader.Mod.TransferAllAssets()
  at Terraria.ModLoader.ModContent.Load() → ModLoader.Load()
"Se ha producido un error al cargar AethonMod. Los mods se han desactivado
automáticamente."
```

**El mod NO llegaba a cargar desde la v5.86** — el CosmicShockwaveProjectile.cs
(nuevo en v5.86) se creó SIN su textura .png. La compilación C# pasa sin texturas
(verificada en sandbox), pero tML las exige al CARGAR el mod → por eso la v5.86
nunca llegó a probarse en juego (los reportes de la v5.86 nunca pudieron verse).

### A. Fix crítico — textura del proyectil de ondas

- **NUEVO `Content/Projectiles/Cosmic/CosmicShockwaveProjectile.png`** (copia del
  InvisiblePixel 1x1): el dibujado es 100% manual (DrawWaveVisual), la textura
  solo necesita existir. Con esto el mod VUELVE A CARGAR.
- Auditoría COMPLETA de las 71 clases de contenido vs texturas (script propio):
  **era la única faltante**. Auditoría de las 21 rutas de `ModContent.Request`
  en runtime: todas existen (png y .fxc).

### B. Bug REAL de daño encontrado en la revisión — marcas de golpe compartidas

- tML crea cada proyectil CLONANDO el prototipo (`MemberwiseClone`) → el array
  `_hitNPCs` inicializado como campo se COMPARTÍA entre todas las ondas
  simultáneas del mismo tipo. Con las 3 ondas inversas de la implosión naciendo
  escalonadas (retardos de 9 ticks): cada nacimiento BORRABA las marcas de sus
  hermanas → las ondas mayores podían golpear a los mismos NPC 2-3 veces.
- **FIX**: override de `NewInstance(Projectile)` dando a cada onda su PROPIO
  array fresco (API verificada contra tModLoader.dll v2026.07.3.0: virtual ✓).
  El `Array.Clear` de SetDefaults se mantiene como defensa extra.

### C. Robustez de render — End defensivo antes de restaurar el batch

- `RestoreSpriteBatch` (BlackHole, Sun) y el restore de Supernova: si una
  excepción interna dejaba un `Begin` abierto, el `Begin` de restauración
  lanzaba "Begin has already been called" y ROMPÍA el frame (crash de render).
  Ahora: `try { End(); } catch {}` antes del Begin (el patrón que ya usaba
  CosmicShockwaveProjectile).
- `Main.Transform` (alias DEPRECADO) → `Main.GameViewMatrix.TransformationMatrix`
  en los 4 sitios de restauración (verificado por decompilación: Transform es
  un alias legacy de GameViewMatrix).

### D. Warning del log — "AethonMod spent 61ms blocking on asset loading"

- `ParticleManager.RegisterTexture` llamaba `.Value` al registrar las 11
  texturas DURANTE la carga del mod (bloquea la fase async de tML).
- **FIX**: `_textures` ahora es `Asset<Texture2D>[]` — se guarda el Asset sin
  resolver y la textura se resuelve al DIBUJAR (ya en juego la carga de fondo
  terminó: `.Value` instantáneo, cero bloqueo de la carga).
- Guard extra en `DrawParticle`: un Asset fallido se salta sin romper el pase.

### E. Warning del log — "Failed to load icon_small.png"

- **NUEVO `icon_small.png` (30x30 exacto)**, redimensionado del icon.png 80x80
  con LANCZOS. Verificado contra tML decompilado: `GetModIcon(File, "icon_small.png", 30)`
  exige 30x30 exacto (y icon.png 80x80 — ya lo era).

### F. Limpieza total de warnings del build del usuario

- CS0672 (Kill obsoleto) ×4: migrados a `OnKill` (hook moderno): CosmicProjectileFX,
  CosmicOrbBolt, QuantumSplitProjectile, GenesisLight.
- CS8632 (anotaciones `?` sin contexto nullable) ×21 en 8 archivos: quitadas
  (neutral: los reference types ya admiten null sin contexto; ningún `Vector2?`
  de tipo valor fue tocado).
- **Build verificado SIN supresiones: 0 warnings, 0 errores** contra tModLoader
  v2026.07.3.0 real.

### G. Pasadas de revisión (lo verificado y quedó OK)

1. Texturas de contenido: 71 clases auditadas → solo faltaba la de arriba.
2. Rutas de assets en runtime: 21/21 existen.
3. Render/SpriteBatch: Begin/End balanceados en todos los efectos; lente
   restaura los render targets y el batch correctamente (verificado v5.86).
4. Multijugador: daño `SimpleStrikeNPC` solo en autoridad (server/SP), spawns
   de ondas con guard de owner/netmode correcto, dusts/luz solo cliente.
5. Fases: explosión BH t-90 → crecimiento 450→720px → evaporación t-36 → 3
   ondas inversas t-0; sol: llamaradas t=2,4,6,8s, supernova t=7s, explosión
   simultánea t=10s — todos los timings verificados en código.
6. `CosmicProjectileFX` (GlobalProjectile, afecta a todos): seguro (AppliesTo
   Nightglow 931, solo dusts).
7. `_masters/`: solo sprites maestros png, sin .cs → no interfiere.
8. `NewInstance`/`SetDefaults` de tML: semántica verificada por decompilación
   (MemberwiseClone + SetDefaults por spawn → origen del bug B).
9. `icon.png` 80x80 ✓ requerido por `GetModIcon(iconSize=80)`.
10. Compilación final limpia contra el binario real.

### H. Nota sobre el aviso "AssemblyLoadContext still using memory"

- Aparecía en el log TRAS la carga fallida (estado parcial del mod). Con el mod
  cargando correctamente no debería volver. Si reaparece al DESACTIVAR tras
  usar la lente en juego, es transitorio: el cierre del RenderTarget2D de la
  lente se encola al hilo principal (v5.87) y se drena en el frame siguiente.

### I. Prueba recomendada para el usuario

1. tModLoader → Develop Mods → Build (v5.88) — debe compilar SIN warnings.
2. Activar el mod: **debe cargar sin error** (el diálogo de "recurso esperado
   no encontrado" desaparece).
3. Probar BlackHoleStaff/SunStaff: las 4 ondas del agujero (cromática + 3
   inversas) hacen daño UNA vez cada una por NPC; las 3 de fuego queman.
4. Desactivar/Reload: desactivación limpia (fix v5.87 vigente).

---

## Commit v5.87 — FIX: ThreadStateException al desactivar el mod (FNA3D + hilo principal)

Recordatorio: Puedo coger los recursos de nuestro github si los datos de mi versión local se borran

**Error reportado por el usuario** (captura al desactivar el mod en tModLoader
v2026.7.3.0):

```
System.Threading.ThreadStateException: most FNA3D audio/graphics functions must
be called on the main thread
  at Microsoft.Xna.Framework.ThreadCheck.CheckThread()
  at Microsoft.Xna.Framework.Graphics.Texture.Dispose(Boolean)
  at Microsoft.Xna.Framework.Graphics.RenderTarget2D.Dispose(Boolean)
  at AethonMod.Content.Effects.BlackHoleLensSystem.Unload()
  at Terraria.ModLoader.Mod.UnloadContent() → ModContent.UnloadModContent() →
     ModLoader.Mods.Unload() → ModLoader.Unload()
```

tModLoader mostraba: "Uno o más errores ocurrieron durante la desactivación y
tModLoader debe reiniciarse para evitar más problemas. AethonMod no se ha
desactivado correctamente."

### A. Causa raíz

- tModLoader **descarga los mods en un hilo de carga secundario** (async load).
- FNA3D exige que **todo Dispose() de recursos gráficos corra en el hilo
  principal** (ThreadCheck lo lanza y aborta la desactivación del mod).
- La v5.85 introdujo la lente gravitacional con su `RenderTarget2D` propio, y el
  `Unload()` de v5.86 hacía `_lensTarget?.Dispose()` directamente → excepción.

### B. Solución (verificada contra el binario real de tModLoader)

- **`Main.QueueMainThreadAction(Action)`** es la API oficial de tML para ejecutar
  acciones en el hilo principal: encola en `ConcurrentQueue<Action>
  _mainThreadActions`, que **se drena al final de `Main.Update()` cada frame** —
  también mientras la pantalla de carga del reload sigue dibujándose (el propio
  tML la usa para operaciones de ventana). Verificado por reflection +
  decompilación (ilspycmd) contra `tModLoader.dll` v2026.07.3.0 real:
  - `Main.QueueRenderAction` NO existe en esta versión; `QueueMainThreadAction` sí.
  - `ConsumeAllMainThreadActions()` se llama al final de `Main.Update(GameTime)`.
- `BlackHoleLensSystem.Unload()` ahora:
  1. Desconecta el hook `On_TimeLogger.DetailedDrawTime` dentro de try/catch.
  2. **Encola** `target.Dispose()` al hilo principal (el closure captura una
     variable local, no el ModSystem ni estado estático → acción autosuficiente;
     la cola mantiene vivo el ensamblado del mod hasta ejecutarla).
  3. Anula todas las referencias estáticas de inmediato (`_lensTarget`,
     `_distortionShader`, `_shaderFailed`, `LensActive`).
- **Programación defensiva total** (como pide el diálogo de tML): el encolado y
  el propio Dispose van envueltos en try/catch — la desactivación del mod jamás
  puede volver a romperse, ni siquiera si el juego se está apagando del todo.

### C. Auditoría del mismo patrón de error en todo el mod

- `BlackHoleLensSystem.RenderLens()` re-crea el target al cambiar la resolución
  con `_lensTarget?.Dispose()` — **seguro**: corre dentro del hook del punto 36
  (hilo de render), no en el hilo de carga.
- `ParticleManager.Unload()` solo anula referencias (las texturas son Assets
  propiedad de tML, no hay que disponerlas). **Seguro**.
- `AethonMod.Unload()` está vacío. **Seguro**.
- Único `new RenderTarget2D` del mod: el de la lente. No hay otros recursos GPU
  propios.

### D. Verificación

- Compilación: **0 errores** contra tModLoader v2026.07.3.0 real (dotnet 10,
  proyecto de verificación contra las DLLs del release de GitHub). Mismos 4
  warnings benignos preexistentes (Kill obsoleto en archivos antiguos, sin
  relación con este fix).
- APIs confirmadas contra el binario real antes de escribir el código.

### E. Prueba recomendada para el usuario

1. Abrir tModLoader → Develop Mods → Build (recompilar la v5.87).
2. Activar el mod, entrar al mundo, disparar BlackHoleStaff/SunStaff.
3. **Desactivar el mod o hacer Mods → Reload**: la desactivación debe completarse
   en silencio (sin diálogo de error y sin pedir reinicio).

---

## Commit v5.86 — LA LENTE VA DETRÁS DEL AGUJERO NEGRO + ONDAS CROMÁTICAS/DE FUEGO CON DAÑO REAL

Recordatorio: Puedo coger los recursos de nuestro github si los datos de mi versión local se borran

### A. Arreglos de la lente gravitacional (reportes del usuario)

**Problema reportado**: "la lente gravitacional no puede afectar al agujero negro y
debe estar detrás de la animación del agujero negro y sus efectos".

**Causa raíz**: en v5.85 la lente volcaba la pantalla distorsionada ENCIMA de todo el
frame renderizado — y el núcleo del agujero negro (dibujado en el pase del mundo)
quedaba DENTRO de esa pantalla, así que la lente deformaba al propio agujero negro.

**Solución (arquitectura nueva en el punto 36 del pipeline)**:
1. Con la lente activa, `BlackHoleProjectile.PreDraw` **se salta el dibujado del
   núcleo** en el pase del mundo (bandera estática `BlackHoleLensSystem.LensActive`,
   que refleja "la lente se renderizó en el frame anterior").
2. El mundo se renderiza en `screenTarget` SIN el núcleo del agujero.
3. Tras compositar la distorsión, el sistema de lente dibuja ENCIMA, en orden:
   **a)** partículas de la nueva capa `AboveLens` (los efectos del agujero: disco de
   acreción, anillo de fotones, espiral de succión, halo de distorsión — ahora la
   lente queda DETRÁS de los efectos del agujero negro), **b)** el NÚCLEO completo
   del agujero (`BlackHoleProjectile.DrawCoreVisuals`, refactorizado a método
   estático compartido: halo + RealBlackHoleShader + refuerzo del horizonte),
   **c)** los anillos de las ondas cromáticas.
4. **Fallback automático**: si la lente falla/no hay fuentes, `LensActive` cae a
   false y todo vuelve al dibujado normal del mundo — el agujero jamás desaparece.
5. **BONUS de calidad — composición por regiones**: antes la pantalla COMPLETA se
   volcaba a media resolución (todo el juego quedaba emborronado); ahora solo se
   re-dibuja distorsionada la zona alrededor de cada fuente (±2.2× su radio), el
   resto del mundo conserva la resolución nativa.
6. Se elimina `SpawnAccretionDiskParticles` (GoldFlame a 10-40px del horizonte,
   redundante con el disco del shader + el disco de la librería y en plena zona de
   curvatura): los efectos cercanos ahora viven en la capa AboveLens.

### B. CosmicShockwaveProjectile (NUEVO) — ondas expansivas con daño real por frente

Archivo nuevo `Content/Projectiles/Cosmic/CosmicShockwaveProjectile.cs`. Tres estilos:

1. **ESTILO 0 — ONDA CROMÁTICA (explosión del agujero negro)**: anillo RGB con
   aberración cromática real (los canales R/G/B se separan radialmente, el desfase
   crece con la edad = dispersión) + núcleo blanco unificador. Se registra como
   fuente del sistema de lente → **el fondo del juego se distorsiona a su paso**
   ("que distorsione un poco"). Daña a cada NPC una única vez cuando el frente lo
   alcanza (knockback hacia fuera). Se dibuja ENCIMA de la lente.
2. **ESTILO 1 — ONDA CROMÁTICA INVERSA (implosión del agujero negro)**: nace en el
   radio máximo y CONVERGE hacia el centro (ease-in cuadrático), con el desfase RGB
   INVERTIDO (azul por delante de rojo) y knockback NEGATIVO que arrastra hacia el
   centro. El daño barre hacia dentro (golpea a quien estaba dentro del radio
   inicial). También distorsiona el fondo al pasar y se dibuja encima de la lente.
3. **ESTILO 2 — ONDA DE FUEGO (nova final del sol)**: triple anillo ardiente
   (rojo/naranja/amarillo) + llamas GoldFlame vivas a lo largo del frente + luz
   cálida. **Cada onda hace daño al pasar y aplica QUEMADURA (OnFire 5s)**.

Detalles técnicos:
- Campos AI: ai[0]=edad (negativa = retardo escalonado), ai[1]=estilo, ai[2]=radio
  máximo; la **duración se deriva del radio** (maxR/20 ticks ≈ frente de ~40px/tick)
  porque la API de NewProjectile solo acepta 3 slots de ai — determinista en todas
  las máquinas (multiplayer seguro).
- Daño manual vía `SimpleStrikeNPC` (guard de autoridad) + marca de golpes por NPC
  reiniciada en SetDefaults (tML puede reutilizar instancias de ModProjectile).
- `friendly=false` + `CanDamage()=>false`: sin colisión vanilla, solo daño de frente.
- Render reutilizable desde el pase del mundo (PreDraw) y desde el pase posterior a
  la lente (parámetro `endActiveBatch` para el estado del SpriteBatch).

### C. BlackHoleProjectile — secuencia de muerte completa (explosión → evaporación → implosión)

Pedido del usuario: "cuando el agujero explota necesita una onda expansiva cromática
que distorsione un poco, con esta onda expansiva también crece el área de efecto de
forma momentánea del agujero negro hasta que se evapora en una implosión con 3 ondas
expansivas cromáticas inversas, cada onda hace daño".

1. **t-90 (EXPLOSIÓN)**: nace la ONDA CROMÁTICA (estilo 0, 520px, daño 75% del
   agujero) + estruendo (Item88) + sacudida de cámara (5f "AethonBlackHoleBlast").
2. **t-90..t-36 (crecimiento momentáneo)**: la escala visual del agujero crece
   hasta **+60%** y el **área de efecto de la gravedad se expande de 450px a 720px**
   mientras la onda avanza.
3. **t-36..t-0 (EVAPORACIÓN)**: colapso acelerado de la escala hacia 0 (el área
   crecida se mantiene hasta evaporarse).
4. **t-0 (IMPLOSIÓN FINAL)**: OnKill genera **3 ONDAS CROMÁTICAS INVERSAS**
   (estilo 1, radios 460/520/580px, retardos escalonados de 9 ticks, daño 50% cada
   una) que barren el daño hacia el centro. Se conservan los presets de
   implosión/explosión de la librería, dusts convergentes y screenshake (se retiran
   los 2 RingPulse decorativos, sustituidos por las ondas reales con daño).
5. La intensidad de la lente sigue la escala del agujero: **se enciende con la
   explosión, crece con la onda y muere con la evaporación**.

### D. SunProjectile — arreglos del sol

1. **Primera llamarada corregida** (reporte del usuario: "lanza la primera
   PhoenixNova en la posición del jugador lo cual está mal"): en t=0 el sol aún está
   sobre el jugador (nace en su posición y deriva con el disparo), así que la nova
   estallaba "en la posición del jugador". Ahora la primera llamarada espera al
   **segundo 2**: llamaradas en t=2, 4, 6 y 8s (4 en total).
2. **3 ONDAS EXPANSIVAS DE FUEGO al final de la explosión del sol** (reporte:
   "falta la onda expansiva de fuego, debe hacer 3 ondas expansivas de fuego y cada
   onda debe hacer daño y provocar el debuff quemadura"): la SupernovaProjectile
   (que ES la explosión final del sol, sincronizada al tick con él en el segundo 10,
   y también el proyectil del SupernovaStaff standalone) genera 3 ondas de fuego
   (estilo 2, radios 360/450/540px, retardos de 8 ticks, daño 50% cada una,
   **quemadura OnFire 5s**). Sustituyen a las 2 RingPulse decorativas anteriores.

### E. Librería de partículas — capa AboveLens

- `LayerPriorities.AboveLens = 950` (encima de AboveAll): partículas que pinta el
  BlackHoleLensSystem tras compositar la distorsión.
- `ParticleManager.RenderAboveLensLayer()` (estático): mismos pases de batching por
  blend + frustum culling que PostDrawTiles, filtrando solo la capa AboveLens.
- `PostDrawTiles` se salta las partículas AboveLens cuando la lente está activa
  (si no, las dibuja normalmente — fallback sin lente).
- `DrawParticle` pasa a estático (compartido por ambos pases).

### F. Otros

- Tooltips actualizados: BlackHoleStaff (lente detrás del agujero + secuencia de
  muerte con ondas cromáticas), SunStaff (llamaradas desde t=2s + 3 ondas de fuego
  con quemadura), SupernovaStaff (3 ondas de fuego con daño + quemadura).
- build.txt: v5.85 → **v5.86**.
- COMPILACIÓN VERIFICADA: 0 errores contra tModLoader v2026.07.3.0 real (.NET 10,
  DLLs del release de GitHub) — solo los 4 warnings benignos preexistentes.

## Commit v5.85 — SOL COMPLETO (10s) + LENTE GRAVITACIONAL + SUPERNOVA MEJORADA + limpieza de referencias

Recordatorio: Puedo coger los recursos de nuestro github si los datos de mi versión local se borran

### A. SunProjectile — el sol como cuerpo celeste completo (ciclo de 10 segundos)

El sol ahora tiene un ciclo de vida determinista de **10 segundos exactos** (600 ticks)
y conjuga los proyectiles de dos bastones existentes + gravedad propia:

1. **LLAMARADAS SOLARES cada 2 segundos** — desde t=0 (nace con el sol), se invoca
   `PhoenixNovaProjectile` (el del PhoenixNovaStaff) **centrado en el sol**:
   anillos naranjas expansivos + flash + lluvia de Torch + OnFire. 5 llamaradas
   en total (t=0, 2, 4, 6 y 8s), daño = 50% del sol.
2. **SUPERNOVA SINCRONIZADA en el segundo 7** — cuando al sol le quedan 180 ticks,
   invoca `SupernovaProjectile` (el del SupernovaStaff) en su centro y lo mantiene
   **perfectamente centrado** tick a tick (ai[1] guarda el índice del hijo y el sol
   le impone su posición y velocidad).
3. **GRAVEDAD DEL SOL** — como cuerpo celeste atrae enemigos (radio 280px) con una
   fuerza **10 veces menor que la del agujero negro** (0.26 vs 2.6). Durante la
   carga de la supernova (segundos 7→10) la fuerza **aumenta progresivamente hasta
   x4** (1.04 en el pico): los enemigos son arrastrados hacia la nova.
4. **EXPLOSIÓN SIMULTÁNEA en el segundo 10** — el sol (OnKill) y la supernova
   (OnKill) estallan el mismo tick: nova masiva combinada con doble onda expansiva.
5. **QUEMADURA** — inflama enemigos al contacto (OnFire 5s). La variante potenciada
   por daño mágico se implementará al integrarlo en el Grimorio (arma definitiva).
6. Visuales de carga: el sol se comprime sutilmente, su luz crece hasta x1.8 y
   estelas doradas convergen en espiral hacia el núcleo (SpawnSupernovaChargeIntake).

### B. SupernovaProjectile — reescrito: 3 segundos de carga + explosión masiva

- **timeLeft 90 → 180** (3 s exactos, sincronizable con el sol).
- **Carga (0..180)**: contracción acelerada hacia blanco-azulado, atracción de
  enemigos con fuerza creciente (0.5 → 2.2, radio 300px), espiral de GoldFlame
  cada vez más rápida (2→4/frame), **sacudidas de cámara anticipatorias** cada 40
  ticks (intensidad creciente) y anillos de contención pulsantes.
- **Explosión (OnKill, mejorada y más vistosa)**:
  - DOBLE onda expansiva de la librería (blanca-dorada 320px veloz + naranja
    profunda 460px retardada)
  - Flash blanco gigante (SoftGlow aditivo x6.5) + Explosion(200px, 46 partículas)
  - 26 estelas de viento estelar radiales largas
  - 70 lenguas de GoldFlame + 25 chispas blancas + 18 brasas con gravedad + 14 humos
  - AoE real de 340px (SimpleStrikeNPC + OnFire, solo en autoridad)
  - Temblor de cámara fuerte (10f, "AethonSupernovaBlast") + doble sonido
- `OnHitNPC`: ahora inflama (OnFire 300).

### C. BlackHoleProjectile + LENTE GRAVITACIONAL de pantalla (nuevo sistema)

1. **`BlackHoleLensSystem` (archivo nuevo, `Content/Effects/`)** — la pieza clave:
   - Hook MonoMod `Terraria.On_TimeLogger.DetailedDrawTime` en el punto 36 —
     verificado por decompilación contra tModLoader v2026.07.3.0: es el punto
     EXACTO tras `Filters.Scene.EndCapture` (el mundo ya está en
     `Main.screenTarget`) y antes de la UI.
   - Recopila hasta 5 agujeros activos (posición UV de pantalla + radio
     `width*scale/screenW*0.75`), copia `Main.screenTarget` a través de
     `BlackHoleDistortionShader` (el .fxc del pipeline propio) hacia un render
     target a media resolución y lo devuelve cubriendo la pantalla → **el fondo
     real del juego se curva alrededor del horizonte de sucesos**.
   - "Pequeña lente" deliberada: distortionStrength 0.62 ligada a la escala del
     agujero (nace y muere con él), maxLensingAngle 24, decaimiento exponencial.
   - APIs verificadas por reflexión + decompilación: `Main.screenTarget` ✓,
     `Main.screenWidth/Height` ✓, hook event `DetailedDrawTime` ✓, RT bindings
     preservados/restaurados ✓, try/catch total (nunca rompe el render).
2. **Fuerza gravitatoria mayor**: 2.0 → 2.6 y radio 350 → **450px**.
3. **Área de daño mayor**: width/height 76 → 96 (hitbox + canvas del shader).
4. **Succión espiral multicolor**: la materia devorada cubre violeta/cian/
   magenta/oro (dusts + partículas de librería) y se vuelve incandescente cerca
   del horizonte.
5. **Devora el polvo** en radio ampliado 190 → 260px.
6. disco de acreción de estelas + anillo de fotones + halo de distorsión +
   implosión/doble onda expansiva al colapsar (de v5.84, intactos).

### D. LIMPIEZA TOTAL de nombres de carpetas

- Carpeta de texturas de efectos renombrada a **`Content/Effects/Textures/`**
  (10 texturas) y las 6 rutas de código actualizadas.
- Tooltips de BlackHoleStaff/SunStaff reescritos: describen las capacidades
  propias (nada de "render idéntico a...").
- `AethonMod.csproj`: eliminado un `Compile Remove` de una carpeta que ni
  existía y su comentario asociado.
- `.gitignore`: eliminada una línea de una carpeta inexistente.
- `TestingPlayer.cs` y `CHANGES.md`: comentarios/histórico neutralizados.
- `SupernovaStaff.cs`: docblock y tooltip actualizados a la nueva conducta.

### E. Versionado

- `build.txt`: versión 5.84 → **5.85**.
- **Compilación verificada**: 0 errores contra tModLoader v2026.07.3.0 real
  (4 warnings benignos preexistentes de `Kill()` obsoleto en archivos viejos).

---

## Commit v5.84 — LIBRERÍA DE PARTÍCULAS COMPLETA + capas de VFX en BlackHole/Sun

Recordatorio: Puedo coger los recursos de nuestro github si los datos de mi versión local se borran

### A. Librería de partículas completada según el libro de referencia

El usuario entregó el documento "particle_library_implementation_book.pdf" (88 páginas,
"Librería de Partículas para Terraria - Referencia para IA"). El sistema existente desde
v5.69 (ParticleData + ParticleBuffer + ParticleManager) estaba basado en ese libro pero
INCOMPLETO. v5.84 lo completa al diseño completo del documento:

**Archivos nuevos en `Content/Particles/`:**
- `ShapeDescriptor.cs` — API de spawn genérica por forma (sección 13.3 del libro):
  Box, Circle, HollowBox, HollowCircle, Cone, Sphere, Vortex y Line con
  `GenerateRandomPoint()` y factory methods estáticos.
- `CameraBounds.cs` — frustum culling (sección 16.1): rectángulo visible de la cámara
  con margen para partículas grandes parcialmente fuera de pantalla.
- `ParticlePresets.cs` — efectos pre-empaquetados (apéndice A del libro):
  `Explosion()` (ráfaga radial + anillo de shockwave + chispas),
  `Implosion()` (colapso espiral convergente),
  `RingPulse()` (onda expansiva) y
  `VortexSwirl()` (brazos espirales).

**Componentes implementados en el update loop (antes solo había 3):**
- `FadeIn` — el alpha sube durante los primeros UserData0 ticks (default 10)
- `ScaleUp` — la escala crece de 0 a UserData1/UserData2 (anillos expansivos)
- `ColorShift` — interpola PackedStartColor → PackedEndColor durante la vida
  (¡los campos existían desde v5.69 pero nadie los usaba!)
- `Homing` — persigue un NPC: explícito por whoAmI o el más cercano (sección 12.2)
- `Orbit` — orbita un centro (UserData0/1=centro, UserData2=vel. angular,
  UserData3=radio); stateless: el ángulo se deriva de la posición actual cada tick,
  y RotationSpeed sincronizada mantiene las estelas alineadas tangencialmente
- `EmitLight` — emite luz del color de la partícula; la intensidad deriva de la
  escala (sin UserData → combinable con cualquier otro componente)

**Mejoras del orquestador (ParticleManager):**
- Capacidad 2000 → 4000 partículas (~320 KB, sigue siendo cero GC)
- `SpawnShape(center, shape, count, template)` — API genérica (sección 14)
- Auto-fill de PackedStartColor/UserData en Spawn para todos los componentes nuevos
- Frustum culling en el render (margen 320px) — no se dibujan partículas
  fuera de pantalla (sección 16.1)
- 3 texturas nuevas registradas: GlowOrb (ID 8), SparkleStar (ID 9), TrailGlow (ID 10)
- Constantes `ParticleTex` (patrón TextureRegistry de la sección 21.2)

**APIs verificadas por reflection contra tModLoader.dll v2026.07.3.0 real:**
- `Terraria.Graphics.CameraModifiers.PunchCameraModifier(Vector2, Vector2, float, float, int, float, string)` ✓
- `Main.CameraModifiers` (campo de instancia, tipo CameraModifierStack) ✓
- `Lighting.AddLight(Vector2, Vector3)` ✓

### B. BlackHoleProjectile — capa de partículas de la librería (4 efectos nuevos)

Arquitectura por capas de profundidad: partículas de la librería (PostDrawTiles,
fondo aditivo) + dusts vanilla (capa frontal) + canvas del shader (encima):
1. **Espiral de succión** — 2 partículas/frame SoftGlow aditivas naciendo en el borde
   del campo gravitatorio con velocidad tangencial+radial (espiral natural) y
   ColorShift blanco incandescente → violeta cósmico al morir
2. **Disco de acreción de estelas** — TrailGlow (32x8) estiradas tangencialmente
   orbitando con el componente Orbit; la rotación avanza al mismo ritmo que la
   órbita (RotationSpeed = angVel) → las estelas quedan SIEMPRE alineadas con la
   tangente; ColorShift naranja dorado → rojo profundo
3. **Anillo de fotones pulsante** — Ring con ScaleUp (0→1.15×scale) + FadeOut cada
   36 ticks: destello circular azulado expandiéndose en el horizonte de sucesos
4. **Halo de distorsión** — Noise procedural rotando lento con alpha 26 y tinte
   violeta: sugiere la curvatura del espacio

Impacto y muerte:
- `OnHitNPC`: micro-colapso con Implosion + RingPulse sobre el objetivo
- `OnKill`: Implosion(165px, 46 partículas violetas) + Explosion(130px, blanco→naranja)
  + doble RingPulse (250px violeta + 320px dorada retardada) + screenshake con
  PunchCameraModifier ("AethonBlackHoleCollapse")

### C. SunProjectile — capa de partículas de la librería (4 efectos nuevos)

Misma arquitectura de capas (librería al fondo + dusts frontales + canvas del shader):
1. **Corona de plasma orbitando** — SoftGlow con Orbit (radio 48-60×scale, deriva
   lenta 0.045-0.075 rad/tick) y ColorShift amarillo incandescente → naranja profundo
2. **Viento solar** — estelas TrailGlow alineadas radialmente fluyendo hacia fuera
   desde la fotosfera, desvaneciéndose blanco-amarillo → naranja
3. **Destellos luminosos** — SparkleStar con FadeIn (8 ticks) + FadeOut + EmitLight
   (la partícula ILUMINA su entorno, intensidad según escala)
4. **Arcos de prominencia** — cada 45 ticks (sincronizado con las llamaradas de dust),
   7 estrellas orbitando en el borde de la llamarada con dirección alternante

Impacto y muerte:
- `OnHitNPC`: estallido solar con Explosion + RingPulse sobre el objetivo
- `OnKill`: nova masiva = Explosion(170px, 40 partículas blanco→naranja) + doble
  RingPulse (280px dorada + 380px roja retardada) + ráfaga de 22 estelas de viento
  solar radiales (velocidad 3.5-7 px/tick) + screenshake ("AethonSunNova")

### D. Otros cambios
- Tooltips de BlackHoleStaff y SunStaff actualizados con las nuevas capas de VFX
- `version = 5.84` en build.txt
- **Compilación verificada**: 0 errores contra tModLoader v2026.07.3.0 real
  (mismos 4 warnings benignos preexistentes de Kill() obsoleto en archivos viejos)

---

## Commit v5.83 — AGUJERO NEGRO + SOL con render de referencia + FIX CRÍTICO de shaders

Recordatorio: Puedo coger los recursos de nuestro github si los datos de mi versión local se borran

### A. FIX CRÍTICO: "Failed to load asset 'Content\Effects\Shaders\SunShader'"

**Causa raíz descubierta analizando el código fuente de tModLoader v2026.07.3.0:**
- tModLoader NO compila los archivos `.fx` durante el build (verificado en el código
  fuente de tML y en el issue abierto #3326 de tModLoader).
- Los mods DEBEN incluir los shaders **ya compilados como `.fxc`** — así lo hace
  el mod de shaders de referencia (270 archivos .fxc commiteados en su repo).
- Nuestros antiguos `.xnb` (generados con dxc en el sandbox) no eran XNB válidos de
  MonoGame: el XnbReader de tML fallaba al parsearlos → "Asset could not be found".
- Además tML no tiene reader para `.fx` (el FxReader vanilla es solo XNA, verificado
  por reflection contra tModLoader.dll real), por lo que los `.fx` solos jamás se
  registran como assets.

**Solución aplicada:**
- Borrados los 8 `.xnb` inválidos de `Content/Effects/Shaders/`.
- Añadidos los 5 `.fxc` compilados junto a sus `.fx`:
  `RealBlackHoleShader.fxc`, `SunShader.fxc`, `RadialShineShader.fxc`,
  `BlackOnlyShader.fxc`, `BlackHoleDistortionShader.fxc`.
- Los `.fx` se mantienen como fuente junto a los `.fxc` (sin conflicto: `.fx` no se
  registra, `.fxc` sí). Los shaders propios `Bloom.fx`, `ChromaticAberration.fx` y
  `Shockwave.fx` NO se usan desde C# y no tienen `.fxc` — si algún día se usan,
  habrá que compilarlos con mgfxc/2MGFX primero.
- Verificado por reflection contra `tModLoader.dll` v2026.07.3.0 (la versión exacta
  del usuario): `Terraria.Testing.FxReader` NO existe (`.fx` sin reader) y
  `Terraria.ModLoader.Assets.FxcReader` SÍ existe (`.fxc` se carga con
  `new Effect(device, bytes)`).

### B. BlackHoleProjectile — réplica EXACTA del agujero negro de referencia

Reescrito siguiendo el renderer de referencia al pie de la letra:
- **Zoom dinámico**: `width / 256 * scale * 2` (antes un 0.12 fijo — el error que
  hacía que no se pareciera en nada al original).
- **accretionDiskRadius**: `scale * 0.4` (antes 0.33 fijo).
- **cameraRotationAxis**: `(velocity.Y * -0.022 + 1, 0, rotation)` — el eje se inclina
  con el movimiento vertical, como el pet.
- Canvas de InvisiblePixel de 256px (mismo tamaño del render target del pet).
- `globalTime` ahora usa `Main.GlobalTimeWrappedHourly` (antes GameUpdateCount*0.0167).
- Carga del shader con `AssetRequestMode.ImmediateLoad` + flag anti-reintento.

Mejoras propias añadidas:
- **Pop elástico de aparición** (ElasticOut — réplica de la curva elástica de referencia):
  el agujero rebota al nacer.
- **Colapso final**: los últimos 40 ticks se encoge antes de explotar.
- **Devora el polvo del entorno**: los dusts cercanos (radio 190) caen en espiral
  hacia el horizonte de sucesos.
- Succión espiral de partículas mejorada (más rápidas cuanto más cerca).
- Disco de acreción con GoldFlame + chispas Enchanted_Gold capturadas + humo.
- Refuerzo manual del event horizon (sustituye al BlackOnlyShader, que requiere
  render targets): radio calculado desde los parámetros del shader.
- Implosión + explosión + doble sonido al morir (OnKill).
- Restauración correcta del SpriteBatch (`Main.Transform` +
  `RasterizerState.CullCounterClockwise` — `Main.CullCurrentScissor` NO existe en
  tML 2026, error CS0117 corregido).

### C. SunProjectile — réplica EXACTA de la estrella de referencia

Reescrito siguiendo el draw de la estrella de referencia al pie de la letra:
- **Canvas correcto**: `DendriticNoiseZoomedOut.png` (¡la textura de referencia que
  NOS FALTABA! antes usábamos WavyBlotchNoise como canvas — otra razón del parecido
  nulo). Copiada a la carpeta de texturas de efectos (10 texturas de referencia ahora;
  carpeta renombrada a `Content/Effects/Textures/` en v5.85).
- Backglow doble con BloomCircleSmall (amarillo*0.7 @0.95 + rojo*0.45 @1.61).
- RadialShine sobre WavyBlotchNoise con color (252,212,112)*0.24 y escala
  `width*scale*2.72` (dibujado en Additive para que el brillo radial sume).
- SunShader con parámetros exactos: corona=0.05, mainColor=blanco,
  darkerColor=(204,92,25), accent=(181,0,0), sphereSpinTime=GlobalTimeWrappedHourly*0.9.
- s1=WavyBlotchNoise, s2=PsychedelicWingTextureOffsetMap, sampler LinearWrap.

Mejoras propias añadidas:
- Pop elástico de aparición (ElasticOut).
- **Hinchazón previa a la nova**: se expande los últimos 30 ticks antes de morir.
- **Llamaradas solares periódicas** cada ~0.75s (burst radial de GoldFlame).
- Chispas Torch orbitando + llamas GoldFlame + humo cálido + destellos Enchanted_Gold.
- Iluminación `Vector3(1, 0.9, 0.5) * 3.2` con pulso sutil (como la estrella de referencia).
- **Nova final**: 60 GoldFlame + 35 Torch + 20 destellos + 15 humos + doble sonido.

### D. Otros cambios

- `AethonMod.csproj`: eliminado el Import roto a `/tmp/tmodloader/tMLMod.targets`
  (ruta del sandbox que no existe en la máquina del usuario); ahora usa el patrón
  oficial `..\tModLoader.targets` con `Condition="Exists(...)"`.
- `CosmicWeapons.cs`: tooltips actualizados describiendo los nuevos efectos.
- `build.txt`: versión 5.81 → 5.83.
- **Verificación de compilación**: el mod completo compila con **0 errores** contra
  tModLoader v2026.07.3.0 real (descargado y compilado con .NET 10 SDK: 4 warnings
  benignos preexistentes de `Kill()` obsoleto en archivos viejos; los proyectiles
  cósmicos nuevos migrados a `OnKill()`).

## Commit v5.82 — reescribir BlackHole + Sun con recursos exactos de referencia

## Commit v5.29 — Bastones de prueba + efectos cósmicos + recreación estelar de la imagen de referencia

Sistema completo de bastones de prueba para testear todos los efectos cósmicos
aprendidos. Todos usan el proyectil Nightglow (#931) como base.

### A. Helper de efectos cósmicos reutilizables (CosmicEffects.cs)

Nuevo archivo `Content/Globals/CosmicEffects.cs` con métodos estáticos:
- `SpawnCosmicTrail(center, velocity, scale)` — estela dorada/cian/magenta/índigo
- `SpawnMagicRing(center, color, count, radius, speed)` — anillo expansivo
- `SpawnMagicRingMulti(center, speed)` — 4 anillos cósmicos de colores
- `SpawnSparkles(center, count, spread)` — destellos ambientales
- `SpawnLightBeams(center, count, length)` — rayos de luz radiantes
- `SpawnStarfall(target, count, spread)` — estrellas cayendo del cielo
- `SpawnImpactSphere(center, intensity)` — esfera aditiva blanco/cian/azul
- `SpawnSupernova(center, scale)` — explosión cósmica completa (4 colores + blanco)
- `SpawnStarEffect(center)` — EFECTO COMPLETO (combina todos los anteriores)
- `SpawnRainbowTrail(center, velocity)` — estela arcoíris cambiante

### B. 4 bastones protegidos (baseline, no modificar)

En `Content/Weapons/TestStaffs/`:
1. **TestMagicRing** — Nightglow + anillo dorado básico
2. **TestSparkle** — Nightglow + sparkles ambientales (+ HoldItem aura)
3. **ProjBeam** — Nightglow + rayo concentrado jugador→cursor
4. **TestMagicRingV2** — Nightglow + múltiples anillos cósmicos

### C. 12 bastones nuevos con Nightglow #931

1. **TestNightglowBasic** — baseline vanilla sin efectos (comparación)
2. **TestNightglowCosmicTrail** — estela cósmica densa
3. **TestNightglowStar** ⭐ — RECREA EL EFECTO DE LA IMAGEN:
   esfera de impacto + starfall + sparkles + light beams + anillo dorado
4. **TestNightglowRingBurst** — 4 anillos cósmicos expansivos
5. **TestNightglowSparkleTrail** — estela continua de sparkles
6. **TestNightglowLightBeams** — rayos de luz radiantes (8 rayos)
7. **TestNightglowStarfall** — 5-7 estrellas cayendo del cielo
8. **TestNightglowLifesteal** — 5% lifesteal mientras se sostiene
9. **TestNightglowEmpower** — concede Empoderamiento Cósmico (+10% dmg, +5% crit, 1% lifesteal)
10. **TestNightglowMultishot** — 3 proyectiles en abanico
11. **TestNightglowRainbowTrail** — estela arcoíris cambiante
12. **TestNightglowSupernova** — supernova cósmica completa + esfera de impacto

### D. Cofre de Pruebas Cósmico (TestStaffChest)

Nuevo item `Content/Items/TestStaffChest.cs`:
- Al usarlo, despliega en el inventario: 16 bastones + 6 items ceremoniales
- Items incluidos: 4 bastones protegidos + 12 Nightglow + StellarDust x50 +
  AethonSigil + ResonanceShard x20 + GenesisShard + SeerOrb
- Reutilizable (no consumible)
- Efectos visuales al abrir (40 partículas doradas + sonido)

### E. Cambios en TestingPlayer

- Cambió el marcador de "kit ya entregado" de GenesisShard → TestStaffChest
- Ahora entrega: GenesisShard, 100 GoldBar, LevelUpTester, BossSummonBag, SeerOrb,
  StellarDust x50, AethonSigil, ResonanceShard x20, TestStaffChest
- El jugador recibe TODOS los items al entrar al mundo

### F. Lifesteal mejorado (ShardPlayer + GlobalNPCXP)

- Nuevo flag `HasEnhancedLifesteal` en ShardPlayer (resetado en ResetEffects)
- `ApplyCosmicEmpowermentLifesteal` ahora soporta lifesteal combinado:
  - 1% si HasCosmicEmpowerment (Sello de Aethon)
  - +4% si HasEnhancedLifesteal (TestNightglowLifesteal)
  - Total máximo: 5%

### G. Texturas (17 nuevas)

Generadas a 1024×1024 con z-ai image, downscale LANCZOS a 30×30 (bastones)
y 32×32 (cofre), con background transparency:
- 4 texturas bastones protegidos
- 12 texturas bastones Nightglow
- 1 textura TestStaffChest
- Maestros preservados en Content/_masters/

### H. Localization ES/EN

Añadidas 34 claves nuevas (17 Display + 17 Tooltip) en ambos idiomas.

Versión bump: 5.28 → 5.29

---

## Commit v5.28 — Mejoras profesionales: texturas HQ + nuevo contenido ceremonial

Mejoras aplicadas sobre el baseline estable v5.27 (sin tocar la lógica del Grimorio).

### A. Texturas regeneradas en alta calidad (1024×1024 → downscale LANCZOS)

Workflow: generadas a 1024×1024 (maestros preservados en `Content/_masters/`),
luego reescaladas con PIL LANCZOS al tamaño requerido por el juego, con
remoción de fondo (transparencia) basada en el color dominante del borde.

**14 texturas de items/proyectiles/buffs/tile/icon:**
- icon.png (80×80) — icono del mod
- GrimoireEternal.png (30×38) — arma principal
- GenesisShard.png (24×24) — item clave
- SeerOrb.png (24×24) — item
- CosmicOrbMinion.png (32×32) — minion
- CosmicOrbBolt.png (16×16) — proyectil
- CosmicOrbBuff.png (32×32) — buff icon
- AncientAltar.png (16×16) — tile
- AncientAltarItem.png (24×24) — item placeable
- BossSummonBag.png (24×24) — item de testing
- LevelUpTester.png (24×24) — item de testing
- ResonanceShard.png (24×24) — moneda cósmica
- ArcaneBolt.png (16×16) — proyectil del arma
- GenesisLight.png (22×22) — proyectil del Fragmento Génesis

**6 texturas de NPCs:**
- AethonBoss.png (48×48) — jefe final
- HollowTitan.png (48×48) — mini-jefe del Sagrario Hueco
- TheWitness.png (24×40) — NPC del pueblo
- RiftKeeper.png (36×36) — mini-jefe dimensional
- EchoArcher.png (36×36) — enemigo
- EchoBlade.png (36×36) — enemigo

**3 texturas para contenido nuevo:**
- AethonSigil.png (28×28) — accesorio nuevo
- StellarDust.png (18×18) — material nuevo
- CosmicEmpowermentBuff.png (32×32) — buff nuevo

### B. Nuevo contenido ceremonial

1. **Polvo Estelar (StellarDust.cs)** — material cósmico fino
   - Recetas: 3 ResonanceShard → 1 StellarDust (y viceversa) en Anvil
   - Drops: jefes cósmicos y enemigos del Sagrario Hueco
     * AethonBoss: 10-15 StellarDust (garantizado)
     * HollowTitan: 5-8 StellarDust (garantizado)
     * RiftKeeper: 3-5 StellarDust (garantizado)
     * EchoArcher/EchoBlade: 1-2 StellarDust (25% chance)

2. **Sello de Aethon (AethonSigil.cs)** — accesorio ceremonial
   - Crafteo: 1 GenesisShard + 5 StellarDust + 3 ResonanceShard + 3 GoldBar/PlatinumBar en Anvil
   - Efectos mientras esté equipado:
     * +5% daño mágico
     * +5% daño de invocación
     * +1 slot de minion
     * +5/s regeneración de mana
   - Confiere buff "Empoderamiento Cósmico" (mantenido por el accesorio)

3. **Empoderamiento Cósmico (CosmicEmpowermentBuff.cs)** — buff ceremonial
   - +10% daño (todas las clases)
   - +5% probabilidad de crítico (todas las clases)
   - +5% velocidad de ataque (todas las clases)
   - 1% de lifesteal (aplicado via ModPlayer.OnHitAnything)

### C. Cambios de código

- `Content/Players/ShardPlayer.cs`:
  * Nuevo flag `HasCosmicEmpowerment` reseteado en `ResetEffects()`
  * Override de `OnHitAnything(float, float, bool)` para aplicar 1% lifesteal
- `Content/Globals/GlobalNPCXP.cs`:
  * Nuevo bloque en `OnKill` para drops de StellarDust según tipo de NPC
- `Localization/es-ES` y `en-US`: añadidas 6 claves nuevas
  (StellarDust, AethonSigil, CosmicEmpowermentBuff en ambas Display + Tooltip/Description)

### D. Seguridad

- Tag `stable-pre-improvements-v5.27` + rama `stable-pre-improvements-v5.27-backup`
  creadas ANTES de aplicar estas mejoras.
- Restaurar baseline estable: `git checkout stable-pre-improvements-v5.27`

Versión bump: 5.27 → 5.28

---

## Commit v5.1 — autoReuse + tooltip rediseñado + proyectil cósmico

3 mejoras solicitadas por el usuario:

1. DISPARO CONTINUO (mantener click):
   - Item.autoReuse cambiado de false → true
   - Ahora se puede mantener el click izquierdo para disparar continuo
   - El Shoot retorna true (tModLoader dispara 1 proyectil, sin doble)

2. TOOLTIP REDISEÑADO COMPLETAMENTE:
   - Antes: 6 líneas con abreviaturas crípticas (+4% mag, -20%tb, 13f, Hilo nv25, ump)
   - Ahora: 6 secciones organizadas con cabeceras de colores y texto claro:
     * PROGRESIÓN (verde): Nivel + barra XP + próximo hito
     * DAÑO (dorado): daño mágico/summon + crit + armor pen + minion slots + knockback
     * RECURSOS (azul): mana/vida max + regen + reducción de daño
     * PROYECTIL (dorado): bolts + área + costo mana
     * ORBE CÓSMICO (magenta): contacto + velocidad + rango + cooldown + costo
     * BONUS (rojo): mana bajo + lifesteal
   - Sin abreviaturas: todo el texto es legible

3. PROYECTIL CÓSMICO (CosmicProjectileFX.cs — NUEVO):
   - GlobalProjectile que afecta SOLO al Nightglow (ID 931)
   - Estela cósmica con paleta del Grimorio:
     * Dorado (cada frame) — núcleo de galaxia
     * Cian (cada 2 frames) — estrella guía
     * Magenta (cada 3 frames) — gemas
     * Índigo (cada 4 frames) — fondo del portal
   - Luz cósmica intensa (violeta-dorada)
   - Explosión cósmica al impactar enemigos (4 colores + supernova blanca)
   - Explosión al morir sin impacto

Versión bump: 5.0 → 5.1

## Commit FIX-COSMIC-EVENTS-ELIMINADOS — Quitar sistema de eventos cósmicos (Hitos + Lluvia de Luz + Rifts)
- CosmicEventSystem.cs ELIMINADO por completo (165 líneas):
  * Anuncios "Hitos cósmico: Lluvia de Luz Estelar / Sagrario Hueco / Rifts Dimensionales / Aethon se agita / El Despertar" al alcanzar niveles 25/50/75/100/150
  * UpdateStarlightRain: spawn de meteoros dorados cada 10s al nivel 25+
  * UpdateDimensionalRifts: spawn de NPC RiftKeeper bajo tierra al nivel 75+
- AethonConfig.cs: eliminadas flags EnableCosmicEvents, EnableStarlightRain, EnableDimensionalRifts (ya no se usan)
- Se conservan EnableCosmicEvents/StarlightRain/Rifts eliminados del config (cualquier config.json antiguo simplemente ignora esas claves)
- Motivo: el usuario reportó que los mensajes "Hitos cósmico" seguían apareciendo en el juego y debían estar eliminados (formaban parte de la misma familia de eventos cinematográficos que ya se quitó)

## Commit FIX-SPRITES-FALTANTES — Agregar sprites PNG para LevelUpTester y BossSummonBag
- LevelUpTester.png generado (24x24, saco dorado con flecha ascendente)
- BossSummonBag.png generado (24x24, saco purpura con calavera roja)
- MissingResourceException al cargar el mod resuelto
- Verificado: 18/18 ModItem/Projectile/NPC/Buff/Tile tienen su .png

## Commit FIX-EVENTOS-ELIMINADOS — Quitar lore y eventos cinematográficos de subida de nivel
- LevelUpEventSystem.cs ELIMINADO por completo (temblor de pantalla, overlay con grano, time-skip de 1 día, texto de lore centrado)
- ShardLevelItem.OnLevelUp: removido el bloque que llamaba a LevelUpEventSystem.Trigger() en la primera subida de nivel
- GrimoireEternal.OnCraft: método eliminado (su único propósito era disparar el Trigger(showLore:false) al craftear)
- Se conservan los efectos simples de subida de nivel: mensaje dorado "✦ Nivel X!", sonido Item4, 40 partículas doradas, y hito cada 50 niveles
- ShardPlayer.FirstLevelUpTriggered: ahora es flag legacy (se persiste para no romper saves antiguos pero ya no dispara nada)
- Motivo: request directo del usuario de quitar el lore y los eventos como el que mueve la pantalla

## Commit FIX-7ERRORES-COMPILACION — Corregir 7 errores CS0103 reportados por el usuario
- TheWitness.OnChatButtonClicked: declarada variable 'level' (CS0103)
- GrimoireEternal.OnCraft: eliminada sp.ActiveBranch (propiedad inexistente)
- LevelUpEventSystem: agregada sobrecarga Trigger(bool showLore) — luego eliminada en el commit siguiente
- Items/LevelUpTester.cs CREADO: +10 niveles al Grimorio (reemplaza TestSlayer perdido en force-push)
- Items/BossSummonBag.cs CREADO: 999 invocadores de 16 jefes vanilla (recreado tras force-push)
- TestingPlayer reescrito: kit de testing con GenesisShard + 100 GoldBar + LevelUpTester + BossSummonBag
- Localization es-ES/en-US: limpiadas claves huérfanas (CosmicPetItem, TestSlayer, CosmicPet), añadidas LevelUpTester + BossSummonBag

## Commit b70d655 — Correcciones del commit 48688dd
- XPForNextLevel: eliminado if(Level<=1) return 1, fórmula normal para todos
- ExtraProjectiles: cambiado de level/5 a level/3 (cada 3 niveles)
- CanUseItem: click izquierdo retorna true (Mana Flower)
- CanUseItem: click derecho permite Mana Flower
- Shoot minion: maneja Mana Flower
- autoReuse = false (previene doble disparo)
- Item.shoot = 931 (Nightglow)
- Eliminado código duplicado en CanUseItem

## Commit d205222 — autoReuse false + Nightglow 931
- autoReuse cambiado a false (causa del doble disparo)
- Item.shoot = 931 (Nightglow restaurado)

## Commit 3aaec4f — GrimorioTest creada
- Nueva arma de prueba con lógica diferente
- Sin AltFunctionUse, sin autoReuse
- Click derecho en UseItem, click izquierdo en Shoot return true
- Sprite generado con AI

## Commit bd3a55c — Doble disparo solucionado con return true + reuseDelay
- Click izquierdo: return true (tModLoader dispara 1)
- Click derecho: return false + reuseDelay=10

## Commit 48688dd — Doble uso + Mana Flower para minion
- CanUseItem del minion permite Mana Flower
- Shoot del minion maneja 3 casos de mana

## Commit 1a1da42 — Mana Flower no permite disparar con 0 mana
- CanUseItem retorna true para click izquierdo

## Commit 2f4ae26 — Quitar disparo doble + XP nivel 1→2 normal
- Eliminada probabilidad de disparo doble
- XPForNextLevel: fórmula normal para nivel 1→2

## Commit b37ef0e — Proyectil sale doble: return false → return true
- return false causaba que tModLoader disparara adicional

## Commit 482a19d — Proyectil doble: return true → return false
- Cambio inicial de return true a return false

## Commit b507628 — Restaurar sprites + Nightglow + tooltip compacto
- GrimoireEternal.png = sprite libro.png
- CosmicOrbMinion.png = minion cosmico.png
- Item.shoot = 931 (Nightglow)
- Tooltip compactado (12→8 líneas)

## Commit 117bd03 — Trigger(bool) restaurada
- Sobrecarga Trigger(bool showLore) se perdió en force push
- _showLore flag restaurado

## Commit a81445b — ActiveBranch rezagado eliminado
- sp.ActiveBranch = BranchType.Magic eliminado

## Commit 480761e — NPC.HitInfo KnockBack eliminado
- KnockBack no existe en HitInfo

## Commit 982ea23 — BuffID.Terraprisma → ID 322
- Nombre constante no existe, usar ID numérico

## Commit c74e56a — Projectile.color eliminado
- Projectile no tiene propiedad .color

## Commit c8638c9 — WeaponScaling using agregado a CosmicOrbMinion
- Falta using AethonMod.Content.Systems

## Commit be0fee8 — Nightglow cósmico + Terraprisma minion + CosmicEventSystem eliminado
- CosmicProjectileFX mejorado con tinte dorado
- Grimorio invoca proyectil vanilla 946 (Terraprisma)
- CosmicMinionFX creado
- CosmicEventSystem.cs eliminado

## Commit 2ffddc7 — Minion cooldown quitado del tooltip + hitos mejorados
- Eliminado 'Minion cooldown: Xf' del tooltip
- Hitos actualizados: Mejora de velocidad, Mejora de minion

## Commit f284177 — Mana max + vida max + hit cooldown en tooltip
- +1 mana cada 4 niveles
- +2 vida cada 20 niveles
- Hit cooldown del minion en tooltip
- WeaponScaling: BonusMana, BonusLife, MinionHitCooldown

## Commit b728a96 — Bolts en línea + hit cooldown mejora con nivel
- Separación reducida de 0.08 a 0.02 rad
- Hit cooldown: 15 base, -1 cada 10 niveles, min 1

## Commit 52b6d4b — 1 bolt por click + minion custom con sprite
- return true cambiado a return false
- CosmicOrbMinion reescrito con IA tipo Terraprisma
- CosmicMinionFX eliminado

## Commit f6db599 — TODAS las mejoras del Grimorio (23 funciones)
- WeaponScaling: 23 funciones de escalado + MilestoneRewards
- GrimoireEternal: ModifyWeaponKnockback, disparo doble, tooltip completo
- ShardPlayer: PostUpdate (regen), ModifyHurt (reducción daño)
- CosmicOrbMinion: velocidad y rango escalados
- CosmicProjectileFX: recreado con daño en área

## Commit 093826c — 3 blockers de REVIEW-FINAL arreglados
- TestingPlayer: eliminadas refs a items inexistentes
- TheWitness: level declarado en OnChatButtonClicked
- CosmicOrbMinion: held redeclarado (CS0136)

## Commit 2f8a128 — BossSummonBag recreado
- Se perdió en force push, recreado

## Commit cb9e6cd — LevelUpTester recreado
- Se perdió en force push, recreado

## Commit 167ddec — Minions persisten al cambiar arma + Mana Flower
- Slots de minion en PostUpdateEquips (busca en todo el inventario)
- CanUseItem permite Mana Flower

## Commit 1038516 — Limpiar repositorio + sprite libro
- Eliminadas carpetas del sandbox de GitHub
- GrimoireEternal.png = sprite libro.png

## Commit c44a62b — Minions desaparecían al exceder limite
- Slots de minion movidos a ModifyWeaponDamage
- Conteo cambiado a ownedProjectileCounts

## Commit 66b6533 — Tooltip solo muestra próximo hito
- Eliminado el bloque que listaba todos los hitos acumulados

## Commit b0998c5 — Armas se craftean sin yunque
- Eliminado AddTile(TileID.Anvils) de las recetas

## Commit 5c684b4 — Quitar eventos + partículas + minion sprite + XP normal
- Eventos de subida de nivel removidos
- Partículas reducidas (scale + alpha)
- Minion sprite = minion cosmico.png
- XP nivel 1→2 normal

## Commit 458d30d — ThreadStateException en Unload
- Dispose envuelto en try/catch

## Commit b9905cd — Partículas carga minion + bolts 1+cada3 + LevelUpTester
- Partículas de carga restauradas
- Bolts: 1 base + 1 cada 3 niveles
- LevelUpTester: da +10 niveles al Grimorio

## Commit 224bb62 — LevelUpTester busca en todo el inventario
- No requiere sostener el Grimorio

## Commit 936add3 — Minion requiere mana (15 + nivel, tope 100)
- WeaponScaling.MinionManaCost creada
- CanUseItem verifica mana del minion
- Shoot cobra mana al invocar

## Commit 8e93990 — NPC.HitInfo no contiene KnockBack
- Eliminado hit.KnockBack

## Commit f3d84dc — CosmicOrbMinion namespace corregido
- global::AethonMod.Content.Projectiles.CosmicOrbMinion

## Commit 7c30f7a — BranchChoiceUI eliminado de UISystem
- UISystem simplificado

## Commit 1e9b277 — BranchChoiceUI eliminado de UIScrollBlockPlayer
- Simplificado a métodos vacíos

## Commit 4d67ba0 — try sin catch en CosmicOrbMinion
- Estructura try/catch reparada

## Commit 8eb24c7 — Grimorio reescrito desde cero
- Sin dependencias de ActiveBranch/IsImprinted
- WeaponScaling reescrito sin BranchType
- GlobalNPCXP solo otorga XP al Grimorio
- CosmicOrbMinion sin BranchType

## Commit 71fd964 — Rediseño: crafteo + boss drop
- BranchChoiceUI removido
- GenesisShard es arma de luz + material
- Las 3 armas se craftean con GenesisShard
- OnCraft dispara evento cinematográfico
- Boss drops de King Slime/Eye of Cthulhu

## Commit fa240e1 — HJSON malformado + revisión profunda
- HJSON con múltiples cierres } arreglado
- 3 riesgos arreglados (TestingPlayer, BossSummonBag, TestSlayer)
