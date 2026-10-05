# AethonMod — ESTADO ACTUAL (v6.50.62)

> **Este documento = "¿por dónde nos quedamos?"** — se actualiza en cada entrega.
> Última actualización: v6.50.62 (LAS FAUCES DEL GRIMORIO — publicada con
> release 403485786, md5 aff34d9e…, CDN verificado byte a byte). La .61 fue
> release 403243039, md5 2228b216…, también CDN verificado byte a byte.

## ✅ ESTADO VERIFICADO (build/forense, NO en juego)

- **v6.50.61 = LA PURGA DE NPC + EL VIGÍA DEL FESTÍN + EL LORE NUEVO** (la
  letra de la .60):
  (1) **EL VIGÍA DEL FESTÍN** (`NacerChusmaVigia` en `FaseMonstruos`): la
  oleada ya NO llega vacía al SPAWN ORIGINAL — LA CAUSA (cazada al IL del
  decompile de `NPC.SpawnNPC` con Mono.Cecil): con `Player.townNPCs >= 1`
  el motor natural de spawns queda BLOQUEADO SIN CONDICIÓN (y el spawn
  original del mundo ES el pueblo del Guía) — la furia aceleraba un motor
  que ni arrancaba. LA CURA: cada 15 t se cuenta la chusma sellada viva;
  si tras 150 t de gracia hay <3 vivos, el vigía SIRVE la comida del pool
  ponderado de la oleada en el anillo 640–960 px, en cunas `PosicionLimpia`
  (ahora `internal`) — sellados por OnSpawn como toda la chusma (stats,
  aura, XP y puntos idénticos). El motor natural sigue mandando donde
  puede trabajar.
  (2) **LA PURGA DE NPC**: BORRADOS HollowTitan, RiftKeeper, EchoArcher,
  EchoBlade y TheWitness + sus 4 llamados + sus 4 esencias +
  PresenciaNPCSystem + la tienda del Testigo (Condition/MsgPedirResonancia/
  handler) + el anuncio DerechoEsencias + ~90 líneas de localización por
  idioma. SOLO QUEDA AETHON (el CazadorAstral vive: es el CUERPO del
  ataque LA MANADA ASTRAL del jefe, no un NPC independiente). Los .plr
  viejos cargan igual (DerrotaOleada10/Cronica siguen como constancia) y
  el contador de esencias de Aethon conserva su índice (slot 4).
  (3) **EL LORE NUEVO**: Aethon ES el propio grimorio — el arma que come
  es el jefe final; la pelea de 5 fases es LA PRUEBA del libro (fase 5 =
  «El Veredicto»); su esencia es la página que se arranca al caer. Mod
  renombrado «Aethon, el Grimorio Eterno» + ~15 textos reescritos es+en
  (tooltips del libro/llamado/esencia/forma/mascota, voces de derrota,
  sabores, muerte y veredicto) + description.txt entera.
- **v6.50.60 = LA DECIMASÉPTIMA RONDA — EL ATAQUE ESPECIAL DE CADA FASE +
  EL SOL DE VERDAD + LA CUNA LIMPIA + LA NOCHE DEL GRIMORIO** (la letra de
  la .59):
  (1) **UN ESPECIAL POR FASE** (`EspecialDeFase` + `_especialPendiente` +
  `EntrarEspecial`): P1 **EL SOL** (¡Y LA APERTURA DE TODA PELEA — al
  materializarse, lo PRIMERO que hace el dios es volverse sol y
  lanzarlo!), P2 **LA CATEDRAL** (el reloj permanente), P3 **EL TELAR**,
  P4 **EL VÓRTICE**, P5 **LA CORONA**. La firma ABRE cada fase (tras el
  decreto, directa, sin órbita) y EL SOL entra al menú de TODAS las fases
  — el «no hace nada» era el arsenal encerrado en P3+ (960k de daño).
  (2) **EL SOL DE VERDAD**: asunción 80→50 t, CAÑONAZO 11 px/t, caza
  giro 0.11 + crucero 10.5 + relevo 14 (>500 px) y **ESPOLETA DE
  PROXIMIDAD** (<240 px → gigante ×6 en 30 t que DERIVA hacia la presa →
  detonación 640 px con toda la bruma; render por ESTADO no por reloj) —
  SIMULADO: parado→51 px, botas→270, montura→636; fusión por tiempo a
  los 380 t de red de seguridad.
  (3) **LA CUNA LIMPIA** (`PosicionLimpia`): TODO convocado por la furia
  nace en aire 3×3 validado (superficie: sin muro y sobre el suelo del
  portador; subsuelo: muros OK, hueco siempre) — el ESCUPITajo del
  Devorador (la causa: nacía dentro del terreno) + todas las
  coreografías; y la CAPA DE TIERRA cierra el hueco de la .56 (nada nace
  >10 tiles bajo el suelo del portador).
  (4) **LA NOCHE DEL GRIMORIO**: `RegistrarKill(tipo, deOleada, esJefe)`
  — la chusma nocturna NATURAL (`NPCID.Sets.Zombies` + ojos/licántropo/
  luna de sangre) YA NO alimenta (la causa raíz: cada kill nocturna
  borraba el hambre), la comida de la oleada no toca el reloj, el libro
  FURIOSO solo admite bocados de 45 s con piso en el umbral, y EL RELOJ
  DE LA FURIA reintenta cada 5 s: furioso + mundo libre = festín YA,
  día o noche.
- **v6.50.59 = LA DECIMASEXTA RONDA — EL RELOJ DE TODA LA FASE 2 + LA CAZA
  DEL SOL QUE NO SE PIERDE + EL TEMBLOR Y LA AURORA + LAS OLEADAS ×5 CON
  NIVELES Y MULTIBIOMA + EL DEVORADOR ESCUPE** (la letra de la .58):
  (1) **EL RELOJ DE TODA LA FASE 2**: en fase 2 el reloj gigante nace
  PERMANENTE (ai[1]=2) — INMORTAL mientras la fase 2 viva (cabalga al
  jefe por TODA la fase: la arena cae en ciclos, el peso de 560 px
  activo, los demás platos siguen saliendo CON el reloj encima) y se
  DISUELVE en 40 t al primer tick de fase 3 o si el dios muere. Reloj
  único respetado (no nace otro) y el plato SALE del menú de fase 2
  mientras el permanente viva. En fases 3+ la catedral clásica de 620 t.
  (2) **LA CAZA DEL SOL QUE NO SE PIERDE**: el diagnóstico de la .58 — la
  persiga frenaba a 4.4 px/t, MÁS LENTA que el jugador corriendo: giraba
  pero NUNCA ALCANZABA. CURA: lanzamiento suave 7.2→4.8 (sin cañonazo),
  giro Lerp 0.075/t (converge ~0.8 s), gravedad comba +0.012 y CRUCERO
  DE CAZA EN TRES RITMOS: cerca <220 px PESA a 6.4 (se esquiva), media
  7.6 (más rápido que la carrera máxima), lejos >700 px EL RELEVO 9.8
  (remonta tras montura/gancho/dash — NUNCA pierde).
  (3) **EL TEMBLOR VUELVE + LA AURORA EN TODO EL CIELO**: el Item161 de
  la Emperatriz MURIÓ — vuelve el Item122 grave de la .48 (t=11 pitch
  −0.25 + t=130 pitch −0.40, kicks 9/22 y 11/24: el temblor CRECE). Y la
  presentación enciende EL CIELO ENTERO en ColaSierpeSky: EL VELO
  blanco-dorado cubriendo la pantalla COMPLETA + LAS ONCE CORTINAS DE
  AURORA (cuatro tramos curvados cada una, blanco y oro alternos, el
  brillo VIVE abajo) + Lighting.AddLight bañando el suelo del jugador.
  (4) **LAS OLEADAS ×5 (EL CASTIGO)**: aparición ×0.039 (≈5× la furia de
  la .58), tope de vivos 50+20k con techo del motor 170, duración del
  festín 5→25 min (mínimo 50 s por oleada) y puntos 60+30k (90 la 1, 360
  la 10) — la calle se LLENA.
  (5) **EL SISTEMA DE NIVELES DE FURIA**: la furia natural trae EL NIVEL
  DEL PORTADOR (ShardPlayer.FuriaNivel, persistente): la PRIMERA es
  SIEMPRE nivel 1; VENCER el festín completo sube a N+1 (tope 10) con
  anuncio al mundo; MORIR congela. Nivel N = N oleadas y TODO escala:
  vida/daño ×(k+1)·(1+0.20·(N−1)), defensa +2k+3(N−1)/+6k+8(N−1), XP
  ×(k+N), pago k+(N−1), el indicador canta el nivel y cada oleada lo
  anuncia.
  (6) **LA OLEADA SIGVE AL BIOMA EN TIEMPO REAL + MULTIBIOMA**: la firma
  del bioma se relee cada 30 t — cambia el pool AL VUELO (el bioma nuevo
  PESA EL DOBLE). Nivel N = tu bioma + (N−1) acompañantes de LA MESA
  DISPONIBLE (nieve/jungla/desierto/playa/cielo/subsuelo/granito/
  mármol/el mal del mundo EXISTENTE — y TRAS EL MURO DE CARNE EL
  SANTUARIO: pixies y unicornios, antes NO EXISTE). Nuevos principales:
  Santuario (ZoneHallow) y Meteorito (ZoneMeteor).
  (7) **TODO MÁS AGRESIVO**: chusma re-objetiva 30→20 t, empuje
  0.26+0.026k+0.02(N−1), techo 13+0.6k+0.4(N−1); jefes re-objetivan
  cada 12 t, homing 0.09+0.012k+0.01(N−1), embite más frecuente y
  fuerte, coreografía 240−24k−10(N−1) tope 40 (≈el DOBLE).
  (8) **EL DEVORADOR ESCUPE**: la CABEZA escupe 2-3 monstruos de la
  Corrupción DESDE SU BOCA hacia el jugador cada vez que pasa DELANTE
  (<520 px y volando hacia él, cada 80 t) — y su coreografía periódica
  también sale de la boca en abanico (ya no anillo lejano). En el Juicio
  también.
- **v6.50.58 = LA DECIMOQUINTA RONDA — EL SOL PERSIGUE DE VERDAD + EL DISCO
  DEL PROYECTIL SOL + LA BRUMA MASIVA + EL CRASH DE LA ARQUERA MUERTO** (la
  letra de la .57):
  (1) **LA PERSIGA DE VERDAD** («el sol que lanza el jefe, no persigue al
  jugador»): la aceleración de 0.05/t quedaba ENTERRADA bajo la inercia del
  cañonazo (7.2 px/t rectos) — el sol cruzaba sin cazar. AHORA: STEERING —
  el rumbo GIRA hacia la presa (Lerp 0.045/t, converge ~1.2 s), la rapidez
  DECAE 7.2 → 4.4 px/t (persiga LENTA) y la gravedad es la COMBA del rumbo
  (+0.018/t: el peso se nota, la caza GANA).
  (2) **EL DISCO DEL PROYECTIL SOL** («no es el proyectil Sol modificado,
  es simplemente una bola de luz y brillo»): el sol del dios VISTE el
  MISMO pipeline del arma — glow coronal (DrawCoronalGlowSprites
  reutilizado), backglow doble dorado, aura de ruido RadialShine y EL DISCO
  del SunShader (superficie de plasma con DendriticNoiseZoomedOut + slots
  1/2 de ruido), recolor BLANCO-DORADO que ENROJECE en la gigante (Lerp con
  el ease). Disco 144 px en vuelo → ~860 px en la gigante; se FUNDE en 50 t
  al explotar. ENCIMA de rayos/perlas/núcleo existentes (la combinación
  pedida: lo de ahora MÁS el proyectil Sol).
  (3) **LA EXPLOSIÓN SIN CAMBIOS** («que no use la explocion del proyectil
  sol, que usa la explocion del brillo actual»): la StyleNova del arma NO —
  sigue el estallido de la casa (Item74+Item122, Kick 13/28, cruz, ondas
  640/460, 12 esquirlas, AoE honesto HerirJugador 640 px ×1.15).
  (4) **LA BRUMA MASIVA** («necesitan mucha bruma al explotar ya que la
  explocion de una estrella libera mucha energie y polvo»): 120 NUBES en
  TRES CAPAS (velo interior 40 densas escala 3.6-5.8 · frente medio 40 a
  2.2-5.5 px/t · polvo exterior 40 a 5.5-10.5 px/t) + goteo de 3 nubes/tick
  los últimos 50 t + chispas 70→90 + 18 nubes luminosas del draw (antes 10).
  (5) **EL CRASH DE LA ARQUERA** (la InvalidOperationException del
  client.log 12:31:33 — Begin sobre Begin CADA frame, deduplicada a 1 línea
  por tML): el EchoArcher dejaba su lote de pantalla ABIERTO al llamar
  FlushAdditive(null,false) → su corona/aliento/estela JAMÁS se dibujaban.
  DOBLE CURA: la cadena correcta en la arquera (FlushAdditive true +
  reapertura del lote) + LA SONDA INCONDICIONAL en FlushAdditive/FlushAlpha
  (nunca más Begin sobre Begin, venga quien venga).
- **v6.50.57 = LA DECIMOCUARTA RONDA — EL SOL DEL DIOS + LA COREOGRAFÍA + LA IA
  DEL DUELISTA + LA BOLA FINAL SIN PERSECUCIÓN + LAS CALAVERAS POTENCIADAS +
  EL VOLTEO DE GRAVEDAD MUERTO** (la letra de la .56):
  (1) **EL SOL DEL DIOS** (el ataque especial pedido: «debe convertirse en
  solo y luego lanzar ese sol al jugador»): EST_SOL + EstiloSolJefe en CUATRO
  ACTOS — LA ASUNCIÓN (80 t: el sol nace EN el jefe y lo viste mientras el
  dios se detiene), EL LANZAMIENTO (cañonazo + retroceso del dios), EL VUELO
  (persiga LENTA 0.05 tope 5.2 + GRAVEDAD +0.055 — comba hacia el suelo,
  contacto honesto 96×96, estela dorada), LA GIGANTE ROJA (×6 hasta ~590 px,
  paleta blanco-dorado → brasa, tercera corona de 14 perlas, rayos con
  jitter) y LA EXPLOSIÓN (luz 4.2/2.4/1.4, BRUMA de 10 nubes deterministas,
  12 FORMAS-estrella volando, cruz anamórfica, dos ondas 640/460 px, AoE
  honesto HerirJugador 640 px ×1.15 — iframes/escudos respetados).
  (2) **LA COREOGRAFÍA** (la investigación de los god-bosses: la cadena de la
  Emperatriz): desde P3, cada 6-9 ataques el jefe DANZA — SOL → TELAR →
  DANZA → LANZAS → CORONA → ESTALLIDO según fase, encadenados por
  CerrarEstado() SIN pasar por la órbita (la danza muere en el cambio de
  fase; el decreto no encadena).
  (3) **LA IA DEL DUELISTA**: órbita que LEE la distancia (se acerca si
  huyes), EL QUIEBRO (voltea el sentido de giro ante la embestida —
  determinista) y MEMORIA DOBLE (ni el último plato ni el de atrás).
  (4) **LA BOLA FINAL SIN HOMING** (línea recta, velocidad constante) y el
  dios DETENIDO DEL TODO en su muerte (velocidad CERO desde el t=20) con el
  flash final MÁS LARGO Y GRANDE (50 t, blooms hasta 4800 px).
  (5) **LAS CALAVERAS DEL LIBRO POTENCIADAS** (Skeletron de la oleada): el
  proyectil 837 REAL de vanilla (verificado por decompile), gigante
  (×2.1→2.8), veloz (extraUpdates 1), brillante (luz 0.9), daño ×1.35 y
  ESTELA DORADO-VIOLETA por el GlobalProjectile CalaveraPotenciadaFX (solo
  hostiles — las del jugador intactas).
  (6) **EL VOLTEO DE GRAVEDAD MUERTO**: FlipGravity + contador + campo
  eliminados; anuncio retirado en es+en (simétrico).
  (7) **EL client.log LEÍDO**: LIMPIO — 2 FormatException de vanilla leyendo
  .plr corruptos (ajeno al mod) y 1 h 50 min de partida SIN excepciones.
- **v6.50.56 = LA DECIMOTERCERA RONDA — LOS 2 CRASHES + EL HALO PLANO + LA
  MINI-EXPLOSIÓN QUE CABALGA + LA MUERTE QUE SE ENCIENDE (LA BOLA FINAL) + EL
  REY GELATINA DE VERDAD + LA CHUSMA FUERA DE LOS MUROS** (la letra de la .55):
  (1) **EL GEL INVISIBLE** (crash 1 del client.log: `Draw was called, but Begin
  has not yet been called` en AtaqueOleadaProjectile:240): la bola de gel
  dibujaba el sprite del ítem SIN lote abierto → excepción cada frame y el gel
  JAMÁS se pintaba (por eso «no usaba el ítem Gel»: volaba invisible) — ahora
  SU PROPIO LOTE ALFA en la FASE 1.
  (2) **EL FUNERAL EN HILO AJENO** (crash 2: `ThreadStateException` al salir del
  mundo): VFXCore.Reiniciar disponía la textura _arcoiris desde el hilo del
  guardado — ahora EL PATRÓN DE AURALIB (QueueMainThreadAction) con el hilo
  principal aprendido en PostDrawTiles (AprenderHiloPrincipal).
  (3) **EL HALO ARCOÍRIS**: 30→13 px sobre la cabeza + el aro PLANO (rotación 0
  — antes t·0.10 lo VOLTEABA vertical a los 15 s); el giro vive en las 12 perlas
  recorriendo el aro (0.55 rad/s) — giro 100% horizontal.
  (4) **LA MINI-EXPLOSIÓN CABALGA**: ai[1] del MiniEstallidoPet = whoAmI del pet;
  el destello LE PEGA el centro a su fuente cada tick (determinista en todas las
  pantallas); EL ARO CIRCULAR MURIÓ (ni el AnilloFino de la carga ni el anillo
  segmentado del estallido: SOLO BRILLO — rayos, cruz, núcleo, estrella, onda,
  bokeh); y la mascota ALUMBRA ×1.6 (1.35/1.15/0.78).
  (5) **LA MUERTE DEL DIOS**: el eclipse del cine de muerte MURIÓ DEL TODO — la
  agonía SE ENCIENDE (colapso con piso 0.55, brillo ×1→×2.4, rueda ×4, luz
  (3.8, 3.45, 2.55), motas cayendo) y al tick 120 dispara LA BOLA FINAL
  (EstiloBolaFinal=23): el proyectil sol pero BLANCO-DORADO — 88×88, 7 s con
  homing suave (tope 5 px/t), daño 1.3×, luz (2.6, 2.35, 1.7), rueda de 12
  rayos + 2 coronas de perlas + núcleo late + DOS DestelloFinal + 8 chispas +
  estela GoldFlame + nacimiento/apagado con estampido.
  (6) **EL REY GELATINA**: los limos NACEN DE SU CUERPO (abanico ±60° hacia la
  presa — antes un anillo a 260 px EN EL AIRE alrededor del jugador); el gel
  salpica CON CADA SALTO (velocidad: −8/−6/−13 al despegar + aterrizaje) Y CON
  CADA TELETRANSPORTE (ai[1] 5→6 del aiStyle 15, verificado en decompile:
  4 bolas al desvanecer + 6 al materializar + las 12 de la caída) — y SOLO la
  autoridad dispara (guard MP).
  (7) **LA CASA DE LA CHUSMA**: con el portador en SUPERFICIE, el motor natural
  YA NO nazca enemigos en tiles con MURO de fondo ni bajo sus pies
  (SpawnTileY > PlayerFloorY+6 → pool.Clear() → el motor reintenta en otro
  tile) — la chusma llega por el AIRE LIBRE; en el subsuelo todo sigue igual.
- **v6.50.55 = LA MINI-EXPLOSIÓN DE LA MASCOTA** — la duodécima ronda sobre la .54
  («ya que la mascota seria aburrida si fuera un punto de luz constante, has que
  tenga una pequeña animacion, de vez en cuando y de forma aleatoria…»):
  (1) **EL SALUDO DE LUZ**: lo PRIMERO que hace el Aethon Menor al ser invocado
  (45 t de vida) es DISPARAR su mini-estallido — y luego sigue el compás para siempre.
  (2) **EL COMPÁS**: base ~1 vez cada 20 min (1/72000 por tick); cada enemigo a
  1300 px del dueño suma 1/60000 (10 enemigos ≈ cada 90 s, tope 20 contados);
  EL GARANTE: a las 216000 t (1 hora exacta) del último destello sale SÍ o SÍ.
  Solo el cliente del dueño decide el cuándo (cero azar por el cable).
  (3) **EL ESTALLIDO EN MINIATURA (~0.13×)** — las SIETE PIEZAS de la .53:
  14 rayos (34-130 px, núcleo blanco + halo oro→ámbar→brasa, vida propia por
  rayo), anillo segmentado de 7 emisores (20→105), cruz anamórfica, núcleo Bloom
  hasta ~130 px respirando, estrella DestelloFinal de 8 rayos, onda y 8 bokeh +
  chispas convergentes/voladoras, luz de mundo pequeña (1.25/1.08/0.70) y el
  estampido chiquito (Item122 vol 0.35 pitch +0.42 + kick 2 px).
  (4) **LA RECOGIDA (45 t, regla Fargo)**: aro fino de 135 px pulsando (LA MISMA
  cifra de la hitbox), núcleo que se llena, 6 agujas convergiendo — y la FUENTE
  SE ENCIENDE: el pet brilla ×1.45 y gotea chispas ×4 mientras carga.
  (5) **EL DAÑO SIMBÓLICO**: 15 plano, hitbox honesta 270×270 (radio 135) SOLO
  los primeros 8 t de onda, cada enemigo UNA vez (localNPCImmunity −1).
  (6) **LA GRATITUD**: cada enemigo tocado cura 1% de la vida MÁXIMA del dueño
  (tope 5% por destello — HealEffect + statLife, solo el cliente del dueño).
  (7) **SINCRONIZACIÓN**: semilla en ai[0] → todas las pantallas dibujan EL MISMO
  destello (Hash01); CERO texturas nuevas (todo con las primitivas de la casa).
- **v6.50.54 = EL DECRETO CABALGA + EL ESTALLIDO CON COMPÁS + EL DASH CORTO + EL RELOJ ÚNICO
  + LAS TRES ARMAS DE LA EMPERATRIZ + EL HALO ARCOÍRIS + LA FORMA 3 MÁS VIVA + EL CRASH DEL
  REGALO MUERTO** — la undécima ronda sobre la .53:
  (1) **EL DECRETO CABALGA**: el círculo del cambio de fase nace EN `NPC.Center` y LO SIGUE
  (ai[2] = whoAmI, Lerp 0.30) mientras el jefe ORBITA LENTO en vez de clavarse — el anuncio
  actualizado (es+en) lo dice: «el círculo CABALGA CON EL DIOS».
  (2) **EL ESTALLIDO CON COMPÁS**: P1 = 1 uso por fase (menú), P2 = 2, y P3+ = FUERZA un
  contador aleatorio 1-9 de ataques de otro tipo (re-tirado tras cada estallido y en cada
  cambio de fase); cada activación = 1/2/3 detonaciones (t=60/105/150) con SU aro telegrafiado
  antes de CADA una (ai[3] = la próxima explosión) y LA ESCALA por fase viajando en ai[2]
  (fase·8192+seed — 1.15 en P1 → 1.39 en P5: rayos, anillo, hitbox 1200·esc y la LUZ).
  (3) **EL DASH CORTO Y CENTRADO**: rumbo DIRECTO a PredPresa (el corte lateral de la .44
  murió), 44→34 px/t, y muere al ALEJARSE / 58 t / 1200 px (antes 1900/90).
  (4) **EL RELOJ ÚNICO GIGANTE**: ×7.5 (300×380), nace EN el jefe, LO SIGUE, arena desde el
  tick 0, peso radio 560 (600 en inversión), daño 0.60× — ya no hay 4 relojes alrededor del jugador.
  (5) **LAS TRES ARMAS DE LA EMPERATRIZ** (la investigación: Sun Dance/Ethereal Lance/
  Everlasting Rainbow de vanilla + Fargo's Eternity como el mod popular que la rehace):
  LA DANZA SOLAR (EST_DANZA f3+ — 6 rayos de 860 px girando 0.0125 rad/t en 3 tandas
  desfasadas, translúcidos los primeros 25 t — la regla Fargo — cabalgando con el jefe, daño
  por hitbox de BARRA 0.55×, peso 5 al volador), LAS LANZAS ETERNAS (EST_LANZAS f4+ —
  8-14 lanzas sembradas a 620 px DETRÁS de tu carrera, telegrafo translúcido 50 t + vuelo 15 px/t,
  daño 0.75× manual, dos tandas — la segunda LA SENTENCIA sobre tu posición actual, peso 5 al
  corredor) y LA CORONA ETERNA (EST_CORONA f4+ — 14 plumas prismáticas espiralando 240↔640
  girando 0.024 rad/t, ancla que deriva hacia ti, daño 0.50× por pluma, peso 5 al quieto) —
  16 ESTADOS en total, 3 anuncios nuevos (es+en) y la rueda del jefe ACELERA ×4 en la danza.
  (6) **EL HALO ARCOÍRIS**: ítem NUEVO (accessory+vanity, modo 7 del portador, patrón 8 de
  PatronAura): la banda `VFXCore.Arcoiris` (rx 42) alrededor de la cabeza girando (t·0.10) con
  12 perlas zodiacales de color, rim blanco, atmósfera y luz discreta; PNG 30×30 horneado,
  anillo animado en el icono, SIN RECETA (regalo al entrar).
  (7) **LA FORMA 3 MÁS VIVA**: banda a t·0.10 + EL SEGUNDO ARO fino al revés (0.74·radio),
  perlas 11→13 px (alfa 0.95), joyas 34→46 px, LOS 12 RAYOS DEL MANDORLA en el nimbo,
  lámparas ×1.25, aleteo ×1.35, y la LUZ DE MUNDO del trono (1.25/1.15/0.90 respirando).
  (8) **EL CRASH DEL REGALO MUERTO**: `YaLoTiene` con bucles `.Length` (el IndexOutOfRange
  del client.log — la entrega MORÍA en el índice 58 y los ítems no llegaban).
- **v6.50.53 = EL ARCOÍRIS DE LA FORMA 3 + EL SERAFÍN + EL FANTASMA DE LOS NPCS MUERTO
  + LA MASCOTA-JEFE + EL ESTALLIDO RADIANTE** — la décima ronda sobre la .52:
  (1) **EL ARCOÍRIS DE LA FORMA 3 POR FIN EXISTE** (bug de TRES versiones): el
  dispatcher `DibujarJugadorAditivo` NUNCA llamaba a `DibujarDivino3` — el trono
  entero JAMÁS se dibujó en .49→.52; UNA línea de dispatcher lo cura.
  (2) **LA FORMA ASCENDIDA 4: EL SERAFÍN** (Isaías 6 — "el que arde"): SEIS ALAS
  en tres pares (8+10+7 plumas con cálamo y punta de luz), HALO TRIPLE con
  trisagión contrarrotando, CORONA del Rey de Gloria, NIMBO mayor (Radio×3.4),
  RAYOS DE DIOS (12 haces), BRUMA SANTA (6 velos), CUERPO QUE ARDE (8 lenguas),
  destello + plumas que caen; Radio 140 (el mayor), luz de mundo 1.55/1.38/1.02,
  PNG horneado + anillo dorado animado en inventario, se ENTREGA al entrar al
  mundo, VUELO INFINITO.
  (3) **EL FANTASMA DE LOS NPCS MUERTO**: el PerlinBolt del RAYO dejaba el lote
  ABIERTO EN ADITIVO (RayoStrip.CerrarLote reabría para sus gorros y el
  ReabrirLoteVanilla era un NO-OP) → TODO NPC tras el jefe salía TRANSPARENTE;
  el finally de PreDraw ahora cierra cualquier lote ajeno y devuelve SIEMPRE el
  lote NPC de vanilla.
  (4) **LA MASCOTA ES EL JEFE EN MINIATURA**: las SEIS secciones del sol de
  código replicadas a escala 0.22 con el MISMO compás (latido 1.6 Hz, giro 0.10,
  coronas de 13 perlas +0.55/−0.38, 6 chispas, núcleo blanco+oro).
  (5) **EL ESTALLIDO RADIANTE** (la imagen del usuario hecha ataque por código):
  EST_ESTALLIDO (13) en menús de fase 3/4/5 leyendo al PEGADO (peso 5) — 60 t de
  RECOGIDA telegrafiada (EL LÍMITE: aro de 600 px pulsando · 14 BRASAS cayendo
  en espiral · EL NÚCLEO que se llena · 10 AGUJAS convergiendo) y al t=60 EL
  PUNTO DE LUZ (proyectil 19): daño 0,95× en radio 600 los primeros 12 t (la
  ONDA, hitbox 1200×1200 autocurada) + 152 t de espectáculo TODO determinista
  (Hash01): 44 RAYOS en 360° (250-980 px, línea núcleo blanca + halo oro→ámbar→
  brasa), ANILLO SEGMENTADO de 14 emisores (150→760), CRUZ ANAMÓRFICA, NÚCLEO
  Bloom ×4 hasta ~950 px RESPIRANDO, ESTRELLA de 8 rayos girando lento, ONDA
  expansiva, 26 BOKEH titilando + 42 chispas GoldFlame + LA LUZ QUE INUNDA EL
  MUNDO (2,4/2,1/1,5 en 2,5 s) + estampido (Item122 + kick 13 px); anuncio
  propio `Jefe.Aethon.Estallido` (es+en).

- **v6.50.52 = EL ARCOÍRIS DEL ÍTEM + EL JEFE SIN ARCOÍRIS + LA ENTRADA
  EN DOS ACTOS Y EL ESPEJO PURO** — la novena ronda sobre la .51:
  (1) **EL ARCOÍRIS DEL ÍTEM** («no se ve el arcoíris en el item de la
  forma ascendida 3»): el png traía un borde desaturado de 4-5 px que
  nadie veía. AHORA: EL ARO ANGULAR HORNEADO EN EL PNG (rojo arriba
  girando por el espectro, 78% saturado/22% original, núcleo blanco
  intacto — visible en inventario/hotbar/suelo SIN código) + EL ANILLO
  ANIMADO (`PostDrawInInventory`: la banda `VFXCore.Arcoiris` de siete
  franjas girando alrededor del icono con su pulso, en el MISMO lote
  de la UI — cero Begin/End, centro independiente de la convención de
  origin).
  (2) **EL JEFE SIN ARCOÍRIS** («el jefe no necesita tener un
  arcoiris»): la sección 4.5 del PreDraw (la corona del espectro)
  MURIÓ — el dios de la luz es oro y núcleo blanco. La banda vive en
  la Forma 3 y la mascota.
  (3) **LA ENTRADA EN DOS ACTOS** («la presentación debe durar hasta
  que el sol llegue al centro, luego aparece el jefe» + «demora mucho
  el suelo temblando y todo eso»): ACTO 1 — LA PRESENTACIÓN (= LA
  CARRERA): la caída (0,5)·0.95 de la Emperatriz + Item161 + la lluvia
  blanco/oro en TODO EL CIELO (vive hasta el final) + el temblor
  creciendo (kicks hasta 13 px, solo aquí) + la carrera del reloj
  TODO JUNTO desde el tick 1, el jefe INVISIBLE, mínimo 150 t. ACTO 2
  — EL APARECER (80 t): PILAR (Item117) + DESTELLO del sol (curva
  comprimida 20/40/80 de ColaSierpeSky) + materialización DENTRO del
  pilar (fade 24 t) directo a la órbita de pelea (nada fuera de
  pantalla — la cúspide −760 de la .51 murió) + estampido (Item122 +
  Roar) al t=26 → pelea. El pilar se disuelve al empezar la pelea
  (`ai[1] == 10` ahora); el temblor MUERE con la aparición (el mundo
  se calma); ColaSierpeSky: destellos y puerta con sub 9-10.
  (4) **EL ESPEJO PURO + EL PARACAÍDAS (la cura del «no aparece»)**:
  la .51 avanzaba con el rate de vanilla y retrocedía a mano — si
  vanilla no aplicaba el rate (Journey/sundial/mods/orden interno),
  la carrera se colgaba y el jefe JAMÁS aparecía. AHORA vanilla queda
  a rate 0 durante TODA la carrera y EL ESPEJO mueve Main.time a mano
  en AMBAS direcciones (cruces de alba/ocaso en ambos sentidos, time
  jamás negativa) — rate = distancia×0.25, techo 220× (peor caso
  ~3,6 s vs 7,5 s de la .51), el sol SE POSA en 27000 y queda QUIETO.
  Y EL PARACAÍDAS de la IA: a los 570 t de presentación el sol se posa
  A MANO y aparece igual — en juego normal jamás dispara (la carrera
  más larga: ~220 t).
- **v6.50.51 = EL ARCOÍRIS DE VERDAD + LA ENTRADA EN BLANCO Y TODO EL CIELO +
  LA CARRERA QUE RETROCEDE + EL NIMBO** — la octava ronda sobre la .50:
  (1) **EL ARCOÍRIS DE VERDAD**: el diagnóstico — la .50 curó el «humo» con
  micro-joyería (hilos de 2 px, perlas de 10 px = CONFETI a distancia de
  juego) y los «hilos» del arcoíris eran Ring BLANCOS. LA CURA: **LA BANDA
  HORNEADA** `VFXCore.Arcoiris` (512² generada EN CÓDIGO — cero assets, el
  set de entradas del .tmod NO se toca): la banda anular [0.58,0.92] con
  SIETE FRANJAS saturadas (rojo fuera→violeta dentro), bordes smoothstep,
  RGB premultiplicado (convención (q,q,q,q)), ~80 px de grosor al radio
  del trono. EN LOS TRES SITIOS: la Forma 3 (Ap 4:3 con 24 perlas montadas
  SOBRE la banda + 4 joyas DestelloFinal + rim interior), EL JEFE en plena
  pelea (sección 4.5 del PreDraw, rx 348, colapsa con la muerte) y LA
  MASCOTA (mini anillo rx 38).
  (2) **EL NIMBO DEL PANTOCRÁTOR** (la Forma 3 celestial): el disco dorado
  (GlowOrb Radio×2.7) + su ARO (Ring Radio×1.26) — la hoja de oro de los
  iconos: la forma que el ojo lee como «esto es un dios».
  (3) **LA ENTRADA SOLO COLOR LUZ Y TODO EL CIELO**: la lluvia de la
  presentación ya no recorre el espectro (hslToRgb muerto) — BLANCO y ORO
  de la casa alternados, naciendo en el rectángulo ENTERO de la cámara
  alrededor del JUGADOR (±62% del ancho, 4 polvos/tick + clon); el AURORA
  874 HallowBossDeathAurora MURIÓ (proyectil prisma = multicolor por
  naturaleza, incompatible con «solo color luz»).
  (4) **LA CARRERA AL MEDIODÍA DE VUELTA — EL RELOJ BIDIRECCIONAL** (la
  petición palabra por palabra: «el tiempo avanza o retrocede en
  consecuencia de qué tan lejos o cerca esté el sol del objetivo que es
  tenerlo en centro»): LA LLEGADA EN CINCO ACTOS — presentación (180 t,
  sub 9) → TEMBLOR (150 t, sub 10: kicks hasta 13 px, la luz ASCIENDE
  con `SeguirCielo` 560 px sobre la presa) → CARRERA (sub 11:
  `TicksHaciaElMediodia` — la posición del sol en el ciclo de 24 h
  contra las 12:00 POR EL CAMINO MÁS CORTO: madrugada/mañana→AVANZA
  (vanilla aplica el rate, el camino probado .41-.48); tarde/noche
  nueva→RETROCEDE (el paso lo da el ESPEJO a mano en PreUpdateTime —
  vanilla solo sabe sumar — con los cruces de alba/ocaso en reversa
  resueltos ahí; rate = distancia×0.08, techo 110×, piso 1, el sol SE
  POSA en 27000) → CLIMAX (120 t, sub 12: EL PILAR `EstiloPilarAparicion`
  sobre `PosicionAparicion` + EL DESTELLO del sol de ColaSierpeSky) →
  DESCENSO (90 t, sub 13: cae a la órbita con chispas doradas) → pelea.
  El CERROJO .42 sigue (mediodía eterno en la pelea; el cliente atrasado
  ATERRIZA, no salta). ColaSierpeSky: destellos y puerta ahora con sub
  9-12. **SIMULACIÓN VERIFICADA: 12/12 casos** (los 4 ejemplos del
  usuario + medianoche/mediodía/amanecer/atardecer/peores casos) todos
  aterrizan en [26999,27001] con la dirección correcta, sin overshoot.

- **v6.50.50 = EL CRASH DE LA MASCOTA + EL REGALO DE PRUEBAS + EL TRONO SIN
  HUMO + LA EMPERATRIZ PALABRA POR PALABRA** — la séptima ronda sobre la .49:
  (1) **EL CRASH DE LA MASCOTA** (el client.log del usuario lo cazó):
  `AethonMenorPet.PreDraw` hacía `FlushAdditive(null, true); return true;`
  — el Flush deja el lote CERRADO y el `true` mandaba a tML a dibujar
  ENCIMA → «Draw was called…» + «End without Begin» en
  `Main.DrawProjectiles:18973` → **Main engine crash al invocarla**.
  El contrato de la casa restaurado: `FlushAdditive` +
  `ReabrirLoteVanilla()` + `return false`. Y las DOS «Excepciones
  silenciosas» del mismo log (SolVivo línea 741 / LenteAbismo 510:
  Begin pelado tras helper que deja el lote abierto — mordidas/lenguas
  JAMÁS dibujadas) curadas con la sonda `CerrarLoteSiAbierto()`.
  (2) **EL REGALO DE PRUEBAS** («no pongas recetas, daselos directamente
  al jugador»): las tres recetas BORRADAS; `ShardPlayer.OnEnterWorld`
  entrega Forma 2 + Forma 3 + Aethon Menor al entrar al mundo (una
  copia por ítem — `YaLoTiene` mira inventario+armadura+misc — con
  mensaje `Mensajes.RegaloPruebas` es+en).
  (3) **EL TRONO SIN HUMO** («la menos divina de todas, solo es humo»):
  la .49 era TODO SoftGlow — 13 capas difusas = UNA MANCHA; ahora cada
  estructura lleva SU NÚCLEO NÍTIDO (aros de Ring, cuentas de GlowOrb,
  trazos sólidos de Pixel, estrellas de DestelloFinal): el arcoíris ES
  UNA LÍNEA, la cruz lleva SU beam, los ofanim tienen PUPILA y cada
  pluma SU CÁLAMO. Llaves huérfanas Presentacion/LlegadaLuz BORRADAS.
  (4) **LA ENTRADA EXACTA DE LA EMPERATRIZ** (AI_120 case 0, LITERAL,
  180 t): proyectil vanilla 874 HallowBossDeathAurora en Center+(0,−80)
  + SoundID.Item161 al t=10 + LA LLUVIA ARCOÍRIS (dust 267 RainbowMk2,
  hslToRgb(t/180)) + caída (0,5)·0.95 + FADE IN alpha=255·(1−t/180) +
  TargetClosest al t=180; anuncio SOLO el de SpawnBoss. `GetSource_FromAI`
  reemplaza al interno `GetSpawnSource_ForProjectile`.
  (5) **EL .plr CORRUPTO del usuario** («Expected Re-Logic file format»):
  archivo de personaje dañado, ajeno al mod — borrar el .plr roto de
  la carpeta Players si un personaje no carga.

- **v6.50.49 = EL FIX DEL VUELO INFINITO + LA FORMA ASCENDIDA 3: EL TRONO +
  EL SEGUNDO JEFE BORRADO + LA ENTRADA DE LA EMPERATRIZ PARA EL PRIMER JEFE +
  EL AETHON MENOR (la mascota de luz)** — la sexta ronda sobre la .48:
  (1) **EL VUELO INFINITO QUE NUNCA CORRIÓ**: el código de la .48 estaba
  MUERTO por timing (las banderas se encendían en `PostUpdate`, un hook
  TARDE — `ResetEffects` 24723 → `PostUpdateEquips` 24914 → `PostUpdate`
  27293, sellado con el decompile); ahora los ítems las encienden en
  `UpdateAccessory`/`UpdateVanity` (DENTRO de `UpdateEquips`, el mismo
  patrón que vanilla usa para `empressBrooch`) — y el vuelo es infinito
  DE VERDAD: la Insignia del Alba (aceleración ×1.75 en el aire) + el
  relleno duro de `wingTime` + la física de alas de Mothron (wingsLogic
  27) inyectada sin sprite (`Player.wings` queda 0: las plumas del aura
  SON las alas). El diagnóstico del «parpadea/intenta avanzar pero
  reinicia su posición» dejó cero escrituras del mod a la física del
  jugador + DOS fixes de render (el velo frontal allocaba un AuraPerfil
  POR FRAME — ahora cacheado; keysets y símbolos auditados).
  (2) **LA FORMA ASCENDIDA 3: EL TRONO** (`FormaAscendidaTresItem`:
  Forma 2 + 20 Fragmento Génesis): la iconografía del trono del
  Apocalipsis — EL ARCOÍRIS alrededor del trono (dos aros de 36+28
  perlas, cada una SU color del espectro), EL MAR DE VIDRIO (la placa
  de cristal + su retícula de destellos), LAS SIETE LÁMPARAS DE FUEGO
  orbitando, LAS RUEDAS DE OFANIM (16+10 ojos de luz contrarrotando),
  EL HALO TRIPLE (30+20+12 perlas + las doce marcas del zodíaco), LA
  CORONA DE VEINTICUATRO ESTRELLAS (tres arcos de ocho), LA CRUZ DE LUZ
  de la Maiestas Domini (la columna del cielo + el brazo del horizonte
  DETRÁS del dios) y LAS ALAS PRISMÁTICAS (los 4 bancos de la Forma 2,
  28 plumas/lado, CADA PLUMA SU MATIZ del arcoíris). Radio 132, luz de
  mundo blanca entera, 36 orbes, vuelo infinito.
  (3) **EL SEGUNDO JEFE MURIÓ** («se ve horrible, dejemos al primero,
  es mucho mejor»): AethonSegundo + AtaqueJefe2Projectile + su
  invocador + 3 texturas + 12 claves es/en — todo borrado; LA FORMA 2
  SE QUEDÓ intacta (receta de 10 maderas + su apoteosis).
  (4) **LA ENTRADA DE LA EMPERATRIZ para el Aethon original**: el
  invocador usa el case 661 literal (Center+(0,−200)+NextVector2Circular
  (50,50)+SpawnBoss) y el jefe presenta ~45 t (materialización con
  destello prisma) antes de pelear — LA CARRERA AL MEDIODÍA MURIÓ
  ENTERA (temblor/reloj/pilar/descenso borrados; el reloj del mundo YA
  NO SE TOCA; el jefe nace DENTRO del rectángulo de CheckActive: el
  bug del jefe esfumado murió con la llegada vieja).
  (5) **EL AETHON MENOR**: la mascota de luz de vanilla (Main.lightPet
  + Sets.LightPet + NeedsUUID) — el sprite del jefe a media escala con
  su corona de ocho perlas y LA LUZ DEL ARCOÍRIS girando por el
  espectro (drop 20% del jefe + 5 maderas + bolsa de cosméticos).
  VERIFICACIÓN: verify 0/0 · build real 0/0 · .tmod 6.353.286 bytes
  (md5 27df0c846b776021671cfb036d821397), 398 entradas con diff
  QUIRÚRGICO (−3/+3), EOF exacto, 374 inflan + 24 planas · DLL:
  vivos PRESENTES / muertos AUSENTES · keysets es 611 / en 613 ·
  headless hasta «Choose World» con 0 excepciones.

- **v6.50.48 = LA FORMA ASCENDIDA 2 + LA BARRA XP ADAPTATIVA + AETHON, LA
  SEGUNDA LUZ + LOS TRES FIXES DE LA OLEADA + LAS COREOGRAFÍAS TEMÁTICAS** —
  la quinta ronda sobre la .47, la más grande del ciclo: (1) **LA FORMA
  ASCENDIDA 2** es un ítem NUEVO (la 1 quedó como estaba): la apoteosis
  ABSOLUTA — las once capas elevadas (Radio 116, 30 orbes) + LA MANDORLA,
  LA CORONA DE DOCE ESTRELLAS, LOS SIETE CANDELEROS y EL RÍO DE LUZ +
  LAS ALAS con VEINTIOCHO plumas por lado en CUATRO bancos y la
  extensión COMPLETA −82°..+78° (por encima de la cabeza y por debajo de
  los pies — la .47 abarcaba solo la mitad superior) + VUELO INFINITO en
  AMBAS formas (el empressBrooch de vanilla prestado; sin alas puestas,
  la física se inyecta y las plumas del aura SON las alas). (2) **LA
  BARRA XP** baila debajo de la última fila de buffs (ceil(buffs/11)
  filas a 50 px) y nunca más pisa los iconos. (3) **AETHON, LA SEGUNDA
  LUZ** (el invocador número 2, `NombreDeAethonSegundo`): la entrada
  LITERAL de la Emperatriz (200 px arriba + jitter circular 50 +
  SpawnBoss — del decompile del case 661), CINCO FASES QUE HEREDAN TODO
  (el Paseo del Ocho/lemniscata → la Carrera Prismática → el Parpadeo →
  el Pentagrama → la Furia Blanca con LA CORONA/galaxia), el arsenal
  `AtaqueJefe2Projectile` (Astilla/Prisma cromático/Anillo Solar/Lluvia/
  Corona/CambioPrisma) y el drop garantizado de la Forma 2 — con la
  investigación pedida (Supreme Calamitas: fases que añaden; Calamitas:
  cargas encadenadas; Fargo: variedad de movimiento; la Emperatriz: sus
  ciclos de carga). (4) **LOS TRES FIXES**: el préstamo de zona (el
  Cerebro y el Devorador ya NO SE VAN: vanilla los mataba/levantaba si
  la presa no estaba en su bioma), el día ya no viste a Skeletron de
  9999/1000 (el modo guardián de su aiStyle 11), y el hook de la barra
  multiplica la referencia fresca por el multiplicador de la oleada × el
  factor de largo real (el desborde era ÷ vida vanilla × vidas ×oleada).
  (5) **LAS COREOGRAFÍAS TEMÁTICAS**: seis dientes de librería muertos;
  cada guardián convoca a LOS SUYOS con material vanilla (abejas 566/181
  + aguijón 719 · sirvientes 5 · limos + BOLAS DE GEL (el ítem Gel como
  proyectil con gravedad, rebote y salpicón) · creepers + monstruos del
  Carmesí · la cadena del Devorador 3× (67→192 con el enlaze AI_06) +
  monstruos de la Corrupción · esqueletos 21 + HUESOS 21 en tres
  figuras).
- **v6.50.47 = LOS NPC FANTASMA + LA APOTEOSIS DE LA FORMA ASCENDIDA** —
  **Probada EN JUEGO — con feedback**: el playtest reveló que la apoteosis
  aún no leía como divina («aun sigue sin verse divino»), que las alas se
  quedaban a media altura con pocas plumas, y que había que crear un ítem
  NUEVO en vez de cambiar el existente — todo eso es la .48. El fix de
  profundidad (DrawBehind) y el resto de la .47 se conservan intactos.
  la cuarta ronda de feedback sobre la .46, DOS pedidos: (1) **EL FIX DE
  PROFUNDIDAD** («algunos ataques del jefe hacen que los NPC sean
  semitransparentes»): los CUATRO ataques de estructura (RELOJ GIGANTE ×4,
  CORO, TELAR, DECRETO) dibujaban en el pase normal de proyectiles —
  DESPUÉS de los NPCs — y sus velos gigantes (la MASA alpha-blend del
  reloj, «el polvo que OCLUYE», + la arena ×5.2) cubrían a los NPCs del
  pueblo: vistos A TRAVÉS del ataque = semitransparentes. El fix es UN
  override: `DrawBehind → DrawCacheProjsBehindNPCs` — el pase de tML que
  dibuja DESPUÉS de los tiles y ANTES de TODOS los NPCs y el jugador
  (verificado en el decompile: `DrawCachedProjs` abre su propio lote, el
  contrato de la casa cerrar→dibujar→reabrir sobrevive, su `End` cierra
  limpio): las criaturas SIEMPRE sólidas encima; los proyectiles rápidos
  (pernos/flechas) siguen en el pase normal pasando por delante como toda
  bala de vanilla; la MASA del reloj VIVE (detrás de todos ya es pura
  atmósfera). (2) **LA APOTEOSIS** («más divino, más sagrado»): el patrón
  Divino pasa de 7 a ONCE capas — EL CORAZÓN BLANCO (el alma ardiendo a
  través del pecho) · **LA COLUMNA DEL CIELO** (el rayo que cae del cielo
  SOBRE el portador y lo SIGUE: 3 velos anidados 640-750 px + EL CHARCO
  de luz — la puerta del cielo abierta para uno solo) · SIETE rayos
  divinos (5→7, 430-580 px) · EL CÍRCULO RÚNICO TRIPLE (+ LA BANDA DE
  ESCRITURAS: 20 glifos contragirando al borde) · **EL GRAN HALO
  CELESTIAL DOBLE** (30 perlas rx 40 — era 26 a 26 px — con 12 ticks
  rúnicos + EL ARO FANTASMA de 18 perlas contragirando + doble aureola +
  4 chispas) · **LA CORONA DE LA SANTIDAD** (5 estrellas-cruz en arco
  sobre el halo) · **LAS ALAS DEL SERAFÍN** (7→12 plumas por lado en DOS
  bancos: 8 primarias hasta Radio×1.55 + 4 secundarias; aleteo majestuoso
  que acelera al correr) · los ecos · OCHO chispas (6→8) · **LAS PLUMAS
  QUE CAEN** (3 plumas doradas descendiendo en cámara lenta, meciéndose,
  naciendo y muriendo suaves) · 22 orbes (14→22). Perfil: Radio 74→88,
  AlfaTrasera 0.28→0.34, Ascenso 12→16; la luz del mundo 0.34/0.27/0.13 →
  0.46/0.36/0.17 (un FARO). Tooltip es+en de la apoteosis. La HUELLA y el
  PULSO de la .45 siguen vivos.
  Verificación: verify 0/0 · build real 0/0 · .tmod 6.333.537 bytes
  (md5 176da964dba2cbc8789efa0fc6044016), 394 entradas (set idéntico a
  la .46), EOF exacto, 24 planas, DLL con **DrawBehind PRESENTE** +
  EstadoVortice PRESENTE / EstadoEclipse AUSENTE (los vivos/muertos de la
  .46 intactos), keysets es+en IDÉNTICOS git↔paquete (los tooltips
  cambian TEXTO, no claves), literales de la apoteosis DENTRO del paquete
  inflado · servidor headless carga sin excepciones.
  **Pendiente: verificación EN JUEGO** (los NPCs del pueblo sólidos
  durante el reloj/coro/telar/decreto; la apoteosis completa de la Forma
  Ascendida — columna del cielo, gran halo doble, corona, alas de serafín,
  plumas cayendo).
- **v6.50.46 = LAS ARMAS QUE NO FUNCIONABAN + EL VÓRTICE PRIMORDIAL + EL
  ANILLO DEL TIEMPO + EL TELAR QUE TRAZA LA ESTRELLA** — la ronda de
  feedback sobre la .45, SIETE pedidos: (0) **EL HALLAZGO**: el estilo de
  cada proyectil vive en `ai[0]` y el CORO, el TELAR y el DECRETO de la
  .45 LO SOBRESCRIBÍAN con sus propios datos en sus primeros ticks → el
  ataque moría INVISIBLE E INERTE (por eso «el jefe lo hace mal» y «no
  lo usa al cambio de fase»); LEY DE ORO: nadie toca ai[0] — el ancla
  ES el cuerpo, el centro ES el proyectil, el radio se COMPUTA de la
  edad; (1) LOS TAJOS DEL JEFE MUEREN (las runas disparan abanicos de 3
  pernos — `EstiloTajoPortador` vive, es del Portador); (2) EL ANILLO
  DEL TIEMPO: CUATRO relojes ×5.2 CAEN del cielo en cruz diagonal
  alrededor de la presa (anillo 330 px, caída 26 t + freno flotante; la
  arena corre solo YA ATERRIZADO y el renderer recibe la MISMA edad de
  arena — giro visual = pulso de daño); (3) EL TELAR TRAZA LA ESTRELLA:
  el jefe vuela de PUNTA EN PUNTA por el SALTO (pentagrama 5 puntas en
  fase 4 · heptagrama 7 en furia — 0→2→4→1→3→0) y cada punta que toca
  CLAVA su estrella (el proyectil lo vigila vía ai[2]); la JAULA conecta
  las puntas en orden natural (`EstrellasClavadas` compacta — el
  veredicto y los hilos dibujan la MISMA figura); con la figura cerrada
  el jefe sigue el CÍRCULO VELOZ vigilando su trampa; (4) EL DECRETO
  FUNCIONA: cambio de fase → jefe INMÓVIL + círculo 80→600+120·(fase−2)
  px (P2 600 → P5 960), MARCA DEL OJO y ejecuciones cada 15 t (la FINAL
  doble); (5) EL ECLIPSE MURIÓ DE RAÍZ (estado, apagón, orbes guiados,
  anuncio es+en, contrato ai[1]=3 — el look oscuro solo vive en el cine
  de muerte) y su relevo es **EL VÓRTICE PRIMORDIAL**: el jefe se alza,
  sus rayos giran ×4 y suelta LA GALAXIA — 3-4 brazos de 12 pernos en
  espiral (150→630 px) con velocidad TANGENCIAL + hundimiento: el
  remolino que gira y COLAPSA (2 oleadas, 3 en furia, cada una girada);
  (6) LA GRAVEDAD DE VERDAD (volteo inmediato `gravDir=−1` + buff 3 s)
  y los diálogos AUDITADOS: Nombre2 → «La Canción del Tiempo»,
  Nombre4 → «El Telar» (los que prometían cosas que no pasaban).
  Verificación: verify 0/0 · build real 0/0 · .tmod 6.335.925 bytes
  (md5 af79edf3be572fea503d33bf24dfd66c), 394 entradas (set idéntico
  a la .45), EOF exacto, 24 planas, DLL con EstadoVortice/SoltarGalaxia/
  PuntaTelar/BuscarTelar/EstrellasClavadas PRESENTES y EstadoEclipse
  AUSENTE, keysets es 497→497 / en 498→498 con diff SIMÉTRICA EXACTA
  (−Eclipse +Vortice en ambos), literales verificados en el paquete
  inflado · servidor headless carga sin excepciones.
  **Pendiente: verificación EN JUEGO** (las tres armas despertadas: el
  coro cantando, el telar con su estrella y su jaula, el decreto en el
  cambio de fase; el anillo del tiempo cayendo; el vórtice; el volteo
  de gravedad REAL; y que las runas ya no tiran tajos).
- **v6.50.45 = EL JEFE QUE SE ESFUMABA + LAS CINCO ARMAS DEL MOD + LA
  FORMA ASCENDIDA DIVINA + EL ALTAR REESCRITO** — la segunda ronda sobre
  la .44, NUEVE pedidos: (1-2) los DOS bugs de despawn con UNA causa
  (decompile de `NPC.CheckActive`: `timeLeft` 937 t refrescado solo con
  jugador a ±(1180,760) px — el jefe nacía 860 px fuera del rectángulo
  vertical y, desde que la .43 lo hizo invocable A CUALQUIER HORA, la
  llegada podía durar la noche entera → moría en secreto a los 15,6 s):
  la llegada y la muerte son CINE (`CheckActive` false) y la sombra
  SIGUE AL JUGADOR cada tick (`SeguirEscondido` — nace sobre TI, no
  sobre el punto del llamado); (3) La Forma Ascendida con el patrón
  `Divino` NUEVO (`EmitirDivino` en 7 capas: cuerpo flipbook, rayos
  divinos, círculo rúnico, HALO de 26 perlas con profundidad, ALAS que
  se despliegan con la velocidad, ecos, chispas estelares) y LOS DOS
  CÍRCULOS PLANOS MUERTOS (`Glow=0/Rayos=0/Anillos=0` — jamás pasa por
  el glow de `Emitir`); en CosmeticPlayer murieron la corona de 7 puntos
  y el rayo de polvo de la .44 (ruido sobre el patrón); (4) el Altar
  Antiguo reescrito (cristal gema 13×14 con filo de oro + satélites +
  sombra, runas talladas «F»/«∠» con marco hundido, MEDALLÓN DEL SOL
  con sigilo, vetas kintsugi, esquinas desportilladas — ítem 24×24
  idem); (5) EL RELOJ DE ARENA CÓSMICO GIGANTE (el renderer del bastón
  escalado ×2.6 + EL PESO que aplasta y hunde, la inversión como pulso);
  (6) EL CORO ESPECTRAL (6 notas orbitando al ancla que deriva hacia la
  presa; cada ciclo la nota siguiente canta su anillo y el FRENTE
  corta); (7) LA MANADA ASTRAL como NPC nuevo `CazadorAstral` (vida
  5000 = MENOS, velocidad 6.5 = la mitad del bastón, camadas de 5 hasta
  10 vivos = MAYOR NÚMERO; 24 s de vida; se apagan si Aethon muere;
  textura de bestiario 34×20); (8) EL TELAR DE CONSTELACIONES en fase 4+
  (el jefe CORRE el círculo — órbita 470 px, una vuelta cada ~2 s —
  mientras 7 estrellas se clavan cada 24 t y la figura encendida corta
  a TODO jugador DENTRO por ray-casting); (9) EL DECRETO DEL ECLIPSE en
  CADA cambio de fase (círculo 80→660+90·(fase−2) px creciendo +2 px/t,
  ejecución cada 15 t, LA MARCA DEL OJO, y el jefe INMÓVIL TODO el
  decreto — la ventana de escape literal). Estados 8-12 también son
  «pelea» para el cerrojo del mediodía del espejo. Anuncios es+en nuevos
  (Reloj/Coro/Manada/Telar/Decreto) + `CazadorAstral.DisplayName`.
  Verificación: verify 0/0 · build real 0/0 · .tmod 6.337.827 bytes
  (md5 28a196d66a2cd70fbce8085c985be2aa), 394 entradas (393+CazadorAstral.
  rawimg), EOF exacto, 24 planas, símbolos vivos en DLL + muertos de la
  .44 ausentes, keysets es 491→496 / en 493→498 (+5 simétricas), literales
  nuevos dentro del paquete · servidor headless carga sin excepciones.
  **Probada EN JUEGO — con bug reportado**: la llegada y el despawn
  quedaron BIEN (el jefe ya no se esfuma), el divino y el altar también —
  pero el CORO/TELAR/DECRETO resultaron MUERTOS al probarlos (el bug
  ai[0] cazado y enterrado en la .46) y el feedback de la ronda lleva a
  la v6.50.46 (tajos fuera, reloj más grande y múltiple, telar con figura
  de estrella, decreto visible, eclipse muerto, gravedad real).
- **v6.50.44 = EL DESCENSO DEL CIELO + EL JEFE QUE TE LEE + dos ítems
  borrados + sprites del origen** — cinco pedidos: (1) la aparición estilo
  EMPERATRIZ: pilar de luz cayendo del cielo + jefe materializado en la
  cúspide bajando por él (PosicionBajoElSol muerta — jamás matemática de
  pantalla en la IA); (2) IA mejorada: memoria de ritmo + fintas leídas +
  bolsa ponderada por comportamiento + órbita que respira + los SEIS
  ataques mejorados (2 oleadas de juicio, lanzas en abanico, nova espiral
  con segunda nova en furia, doble cruz, destello con corte de huida +
  minas, eclipse con pernos guiados); (3) Anillos del Horizonte BORRADOS;
  (4) Corona Rúnica de Aura fusionada en La Forma Ascendida (la VERSIÓN
  DIVINA: aura 74px/16 agujas/18 orbes + corona de 7 luces + rayo divino
  + huella de luz + pulso); (5) sprites de Fragmento Génesis y Altar
  Antiguo regenerados (gen_sprites_v65044.py). Verificación: verify 0/0 ·
  build real 0/0 · .tmod 6.315.842 bytes (md5 1cdfedde53cbe244e4d7a48c2
  4a783ad), 393 entradas (−2 quirúrgicas), EOF exacto, símbolos
  vivos/muertos verificados en DLL, keysets es=491/en=493 (−2 simétricas),
  tooltips nuevos dentro del paquete · servidor headless carga sin
  excepciones.
- **v6.50.43 = EL LLAMADO A CUALQUIER HORA (y el Verdugo que ya no se
  gasta)** — feedback sobre la .42: «el jefe no puedo invocarlo de noche,
  ya que dice solo de día… no tiene sentido eso ya que al invocar el jefe
  el tiempo pasa hasta que el sol está en el centro del cielo, así que no
  importa la hora de invocarlo» + «el item que sube de nivel el grimorio
  es un consumible, has que no sea consumible que cada vez que lo active
  suba 10 niveles sin consumirse». (1) `LlamadoDeJefe` gana
  `ConvocableDeNoche` (virtual, false): los CUATRO GUARDIANES siguen de
  día (lore + mensaje intactos — su presencia NO mueve el reloj) y **EL
  NOMBRE DE AETHON responde A CUALQUIER HORA** — su llegada YA sabía
  correr la noche (rama nocturna simulada en .41, recorrida empíricamente
  en .42-S2; despawn solo por presa, nunca por hora); el gate del ítem
  era el único bloqueo. Tooltip: «A CUALQUIER HORA — la llegada corre el
  tiempo hasta el mediodía.» / «WORKS AT ANY HOUR». (2) `LevelUpTester`:
  `consumable=false`, `maxStack=1` (patrón Carnada) — **+10 niveles por
  uso, SIN consumirse**. Verificación: verify 0/0 (64 refs) · build real
  0/0 · .tmod 6.317.960 bytes (md5 ecdfa08e1297dd6873f6b1388a5c8c45),
  395 entradas (set idéntico), EOF exacto, `ConvocableDeNoche` en DLL,
  cirugía de tooltips verificada por bloque dentro del paquete, keysets
  es=493/en=495 CERO deriva vs .42 · servidor headless carga sin
  excepciones. **Pendiente: verificación EN JUEGO** (invocar de noche →
  timelapse nocturno → sol posado en el centro → jefe; Verdugo martillado
  sin gastarse).
- **GitHub = fuente de la verdad** (regla de la casa, re-confirmada por el usuario:
  "el repo de github siempre es el verdadero"): tag `v6.50.42`.
- **v6.50.42 = EL JEFE QUE NO APARECÍA (y el sol que no se quedaba fijo)** —
  feedback sobre la .41: «el sol avanza como está previsto, pero al llegar
  al centro no queda fijo en el centro y el jefe no aparece». DOS síntomas,
  UNA causa, **reproducida empíricamente en servidor headless**: la
  materialización bajo el sol computaba la posición con la matemática
  pantalla→mundo EN LA MÁQUINA QUE CORRE LA IA — y el servidor (host MP /
  dedicado) NO tiene pantalla (`screenWidth=0`, matrices identidad) → el
  jefe nacía en (0, ~5516), FUERA DEL MUNDO, y MORÍA en el tick 44 del
  climax (antes del fade) → el espejo soltaba el reloj → el sol seguía su
  curso. **EL FIX (PosicionBajoElSol con TRES reglas)**: server-segura
  (sobre el jugador: `target.Center + (0, −420)`, la posición .40 probada),
  exacta en cliente, NUNCA enterrada (mínimo 300 px sobre el jugador) y
  NUNCA fuera del mundo (clamp `[320, maxTiles·16−320]`). **EL CERROJO DEL
  MEDIODÍA**: con el climax/pelea vivos, un reloj pasado de 27001 VUELVE
  activamente a 27000 (jamás la vuelta entera a 110× de la .41).
  **VALIDACIÓN EMPÍRICA COMPLETA** (la técnica nueva de la casa: el
  CLIENTE FANTASMA — un `ISocket` falso en `Netplay.Clients[0]` enciende el
  loop vanilla del servidor): mañana → transición → materialización −420
  sobre el jugador → fade → **pelea de 360 t con time=27000.00/rate=0.00**
  → deriva inyectada 32400 → CURADA a 27000 en el mismo tick → reanudación
  al despawn; tarde → día completo por la noche (rate 110 → desaceleración
  1.43 → aterrizaje 27000) → congelación; .tmod 6.312.422 bytes (md5
  2cf800cee5e1429e8226174dbde1d123), 395 entradas, EOF exacto, sonda
  JAMÁS empaquetada.
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

La v6.50.60 está implementada y build-verificada. Checklist de la .60:

1. **LA APERTURA ES EL SOL**: invocar al jefe — apenas termine de aparecer
   (unos segundos de órbita), lo PRIMERO que debe hacer es CONVERTIRSE EN
   SOL y LANZARLO contra ti (el ataque pedido desde la .57, ahora en CADA
   pelea). Y EL SOL SALE VIOLENTO (11 px/t de cañonazo), te CAZA más rápido
   de lo que corres (correr en línea recta NO salva) y cuando te ALCANZA
   (o se le acaba la vida) SE HINCHA ×6 rojo CERCA de ti y EXPLOTA con toda
   la bruma — ya NUNCA estalla en el vacío lejísimos.
2. **EL ESPECIAL DE CADA FASE**: al bajar al 80% (P2) sale el RELOJ
   PERMANENTE (la catedral de toda la fase); al 60% (P3), tras el decreto,
   EL TELAR (la jaula estrella) ARRANCA la fase; al 40% (P4) EL VÓRTICE
   (la galaxia); al 20% (P5) LA CORONA (el prisma). Cada fase abre con SU
   gala — el jefe «hace TODO lo que tiene que hacer» desde el minuto uno.
3. **NI MUROS NI SÓTANO**: en una oleada con DEVORADOR, sus escupitajos y
   TODAS las convocaciones (limos del Rey, sirvientes del Ojo, creepers del
   Cerebro, esqueletos de Skeletron) deben nacer SIEMPRE en aire libre
   —jamás dentro de un muro ni bajo el suelo— y en la capa de tierra nada
   nace más de 10 tiles por debajo de tus pies.
4. **LA NOCHE**: con el libro a nivel 25+, ESPERAR DE NOCHE sin matar nada
   que no sea la chusma nocturna de siempre (zombies/ojos VALEN: matarlos
   YA NO alimenta el libro) — a los ~5 min LA FURIA DEBE ESTALLAR DE
   NOCHE igual que de día (antes jamás salía). Y si al momento justo hay
   un jefe vivo, apenas muera el jefe el festín sale EN 5 s.
5. **REGRESIÓN**: el reloj permanente de P2, la aurora y el temblor de la
   presentación, los niveles de furia, el multibioma y la agresión de la
   .59; el disco de plasma y la bruma del sol (.58) — todo intacto.

Checklist de la .59 (vigente hasta que el usuario la recorra):

1. **EL RELOJ DE TODA LA FASE 2**: pelear al jefe y esperar el RELOJ DE ARENA
   en fase 2 (80-60% de vida) — el reloj gigante debe quedarse CON EL JEFE
   DURANTE TODA LA FASE (los otros ataques siguen saliendo con el reloj
   encima y la arena cayendo) y SOLO disolverse cuando el jefe pasa a fase 3.
2. **EL SOL CAZA DE VERDAD**: provocar EL SOL DEL DIOS (P3+) y CORRER CON
   BOTAS — el sol sale SUAVE (sin cañonazo), gira hacia ti RÁPIDO y NO TE
   PIERDE: cerca va lento y pesado (se esquiva), a media distancia CAZA más
   rápido que tu carrera, y si algo te lo quita de encima REMONTA. Correr en
   línea recta ya NO salva.
3. **EL TEMBLOR Y LA AURORA**: invocar al jefe — la presentación debe sonar
   EL RUGIDO DE TEMBLOR de antes (NO el sonido de la Emperatriz; suena DOS
   veces, creciendo) y EL CIELO ENTERO ILUMINARSE: velo blanco-dorado de
   borde a borde + ONCE CORTINAS DE AURORA ondulando + el suelo bañado de
   luz (no solo partículas).
4. **LA OLEADA SIGVE AL BIOMA**: durante una oleada, CAMBIAR DE BIOMA (p.ej.
   bosque → corrupto) — los NUEVOS monstruos deben ser DEL BIOMA NUEVO al
   segundo (devoradores de almas, no más limos del bosque).
5. **EL CASTIGO ×5**: la oleada 1 debe sentirse como UNA INVASIÓN: ~70
   monstruos simultáneos, spawns casi continuos, y las muertes para pasar
   de oleada MUCHAS más (90 en la 1). Si no abarrote la pantalla, algo falló.
6. **LOS NIVEALES DE FURIA**: vencer la furia natural COMPLETA (nivel 1) →
   el mundo anuncia «{tu nombre} VENCIÓ la furia de nivel 1 — la próxima
   será de NIVEL 2»; la SIGUIENTE furia trae 2 OLEADAS y todo golpea más
   duro (y el indicador dice «Furia del grimorio · NIVEL 2»). Morir NO
   sube el nivel (repite).
7. **EL MULTIBIOMA**: en furia de nivel 3+, la MISMA oleada mezcla
   monstruos de VARIOS biomas (los de tu zona dominan); en hardmode deben
   aparecer PIXIES/UNICORNIOS del Santuario en la mezcla (en pre-hardmode
   JAMÁS).
8. **EL DEVORADOR ESCUPE**: en una oleada con Devorador de Mundos, cuando
   su CABEZA pase cerca y de frente, debe ESCUPIR monstruos de la
   Corrupción DESDE LA BOCA hacia ti (2-3 por pasada, con sonido de
   escupitajo).
9. **REGRESIÓN**: el disco de plasma y la bruma masiva del sol siguen ahí
   (.58); coreografía, calaveras del Libro, sin volteo de gravedad, bola
   final en línea recta — todo intacto.

Checklist de la .58 (vigente hasta que el usuario la recorra):

1. **EL SOL PERSIGUE**: provocar EL SOL DEL DIOS (P3+; solo o en la danza) y
   CORRER — el sol debe GIRAR hacia ti y SEGUIRTE de verdad (lento, ~4.4
   px/t, con la comba del peso) durante los ~5 s de vuelo; ya NO te cruza
   de largo. Si estás quieto, llega en arco suave hacia el suelo.
   *(NOTA .59: la persiga de la .58 era MÁS LENTA que tu carrera — este
   punto quedó CURADO en la .59: el checklist nuevo de arriba manda.)*
2. **EL DISCO DEL PROYECTIL SOL**: el sol que vuela ahora tiene LA
   SUPERFICIE DE PLASMA DEL ARMA (el mismo disco del proyectil Sol del
   bastón — con su textura convectiva y su aura de ruido) pero
   BLANCO-DORADO, ADEMÁS de los rayos, las perlas y el brillo de siempre —
   y al hincharse en GIGANTE ROJA el disco ENTERO se tiñe de brasa.
3. **LA BRUMA DE LA ESTRELLA**: cuando el sol EXPLOTA debe quedar un
   NUBARRÓN DE VERDAD — más de cien nubes de humo en tres capas (el velo
   denso en el centro, el frente que empuja, el polvo que vaya lejos) que
   queda flotando/desvaneciéndose unos segundos después de la luz.
4. **LA ARQUERA ENTERA**: en LA FURIA, la Arquera Eco debe verse con SU
   CORONA DE ESTRELLAS, el aliento del pecho y la estela de ecos detrás
   (antes JAMÁS se dibujaban — el crash invisible); y el client.log NO debe
   tener NINGUNA "InvalidOperationException: Begin has been called" nueva.
5. **REGRESIÓN**: la explosión del sol sigue siendo la DE SIEMPRE (cruz,
   ondas, esquirlas, daño en área) — NO la del arma; el resto de la .57
   (coreografía, calaveras del Libro, sin volteo de gravedad, bola final en
   línea recta) sigue intacto.

Checklist de la .57 (vigente hasta que el usuario la recorra):

1. **EL SOL DEL DIOS (EL ATAQUE ESPECIAL)**: en la pelea (P3+), esperar a que
   el jefe SE DETENGA y UN SOL BLANCO-DORADO CREZCA SOBRE ÉL (~1.3 s — el
   dios SE CONVIERTE en sol) — luego lo LANZA: el sol vuela hacia ti LENTO,
   COMBA hacia el suelo (gravedad) y te persigue con calma; tras ~5 s se
   HINCHA ×6 volviéndose ROJO y EXPLOTA en luz, bruma y formas — la explosión
   DUELE si estás dentro (~640 px).
2. **LA COREOGRAFÍA**: cada 6-9 ataques (P3+) el jefe encadena VARIOS ataques
   SEGUIDOS sin volver a flotar tranquilo — EL SOL abre y EL ESTALLIDO cierra
   (en P5 la cadena es la más larga: sol → telar → danza → lanzas → corona →
   estallido).
3. **LA IA NUEVA**: el jefe MANTIENE la distancia (se acerca si lo dejas),
   VOLTEA el sentido de giro si lo embistes, y nunca repite el mismo ataque
   dos veces (ni el de atrás).
4. **LA MUERTE DEL JEFE**: el dios queda CLAVADO (quieto del todo) ardiendo —
   y su bola final YA NO TE PERSIGUE: va en LÍNEA RECTA a donde estabas (se
   esquiva a un lado) mientras el flash final es más largo y grande.
5. **EL SKELETRON DE LA OLEADA**: sus proyectiles son LAS CALAVERAS DEL LIBRO
   DE LAS CALAVERAS (las del arma vanilla) PERO GIGANTES, más rápidas,
   brillantes y con estela de fuego dorado-violeta.
6. **SIN VOLTEO DE GRAVEDAD**: pelear entero (P3+) y verificar que NUNCA MÁS
   tu gravedad se invierte ni aparece el anuncio «EL SUELO YA NO ES TUYO».
7. **REGRESIÓN**: todo lo de la .56/.55 sigue intacto (halo plano, mini-
   explosión que sigue a la mascota, Rey Gelatina con limos desde su cuerpo +
   gel visible, oleadas sin muros, saludo/compás de la mascota).

Checklist de la .56 (vigente hasta que el usuario la recorra):

1. **SIN CRASHES**: jugar con la oleada del Rey Gelatina (bolas de gel visibles
   volando/rebotando) y SALIR DEL MUNDO (guardar y salir) — el client.log NO
   debe tener NINGUNA «Excepción silenciosa» nueva ni el ThreadStateException.
2. **EL HALO ARCOÍRIS**: equiparlo — el aro queda MÁS CERCA de la cabeza y
   PLANO para siempre (jamás se voltea vertical); las perlas dan la vuelta al
   aro (giro horizontal).
3. **LA MINI-EXPLOSIÓN DE LA MASCOTA**: cuando estalle, el destello SIGUE a la
   mascota mientras vuela (no se queda atrás), NO hay aro circular (solo el
   brillo: rayos, cruz, núcleo, estrella, onda) y la mascota alumbra MÁS el
   entorno (un farol andante).
4. **LA MUERTE DEL JEFE**: al matarlo, NUNCA se oscurece — ARDE cada vez más
   (brillo subiendo, rueda acelerando, motas cayendo a su cuerpo) y al final
   DISPARA LA BOLA FINAL: una bola blanco-dorada como el proyectil sol, con
   MUCHO brillo, que persigue al jugador ~7 segundos y se apaga con un destello.
5. **EL REY GELATINA**: sus limos SALEN DE SU CUERPO (escupidos hacia ti, no en
   el aire alrededor) y con CADA salto y CADA teletransporte salpica BOLAS DE
   GEL (el ítem Gel, ahora VISIBLE) hacia todas direcciones.
6. **LAS OLEADAS EN SUPERFICIE**: ningún enemigo de la oleada nace ENTRE LOS
   MUROS ni BAJO TIERRA — llegan por el aire libre (con el portador en cuevas
   el comportamiento subterráneo sigue como siempre).
7. **REGRESIÓN**: todo lo de la .55/.54 sigue intacto (saludo y compás de la
   mascota, daño/cura del destello, decreto que cabalga, estallido con compás,
   dash, reloj único, tres armas de la Emperatriz, regalo completo).

Checklist de la .55 (vigente hasta que el usuario la recorra):

1. **EL SALUDO DE LUZ**: invocar al Aethon Menor (o entrar al mundo con el buff) y
   VER que a ~1 segundo de nacer hace su MINI-EXPLOSIÓN (primero el aro fino que
   pulsa con las agujas convergiendo ~0.75 s, luego el destello pequeño).
2. **EL COMPÁS**: con la mascota fuera, esperar — de base sale ~1 vez cada 20 min;
   EN COMBATE (muchos enemigos en pantalla) sale mucho más seguido; y JAMÁS pasa
   más de 1 HORA sin que salga una.
3. **LA MINI-EXPLOSIÓN EN SÍ**: es pequeña (~130 px), ilumina alredor, tiene los
   rayos/anillo/estrella del jefe en miniatura, suena bajito, apenas empuja la
   pantalla (kick 2 px) y la mascota BRILLA más mientras la carga.
4. **EL DAÑO Y LA GRATITUD**: acercar enemigos al destello — reciben un daño
   pequeño (15) UNA vez cada uno, y por CADA enemigo golpeado sube el número
   verde de cura (1% de la vida máxima; máximo 5% por destello).
5. **REGRESIÓN**: todo lo de la .54 sigue intacto (el decreto que cabalga, el
   estallido con compás, el dash, el reloj único, las tres armas de la Emperatriz,
   el halo arcoíris, el regalo sin crash).

*(El checklist de la .54 — decreto que cabalga, estallido con compás, dash corto,
reloj único, las tres armas de la Emperatriz, halo arcoíris, forma 3 viva y el
regalo completo — sigue justo abajo, aún vigente.)*

Checklist de la .54 (vigente hasta que el usuario la recorra):

1. **EL DECRETO CABALGA (EL CAMBIO DE FASE)**: bajar al jefe a 80% de vida y VER el
   círculo del eclipse NACIENDO EN EL JEFE y MOVIÉNDOSE CON ÉL (el jefe orbita lento,
   ya no se clava) — el aro crece mientras cabalga.
2. **EL ESTALLIDO CON COMPÁS**: en fase 1 el estallido sale UNA vez; en fase 2, DOS; y
   en fase 3+ aparece cada POCOS ataques (1-9 aleatorio) — y cada activación son 1/2/3
   explosiones seguidas con su telegrafo ANTES de cada una; todo un 15-39% MÁS GRANDE
   según la fase.
3. **EL DASH**: el destello SIEMPRE embiste hacia el jugador (nunca «a ninguna parte»),
   más corto y frena al pasar.
4. **EL RELOJ ÚNICO**: el ataque del Bastón del Reloj de Arena Cósmico es UN SOLO reloj
   GIGANTE centrado en el jefe que se MUEVE con él (ya no son 4 alrededor del jugador).
5. **LAS TRES ARMAS NUEVAS (fase 3+)**: LA DANZA SOLAR (la rueda de 6 rayos girando,
   translúcida al nacer), LAS LANZAS ETERNAS (las líneas translúcidas que se vuelven
   lanzas — castigan correr en línea recta) y LA CORONA ETERNA (el anillo de plumas de
   colores que espirala alrededor tuyo) — fase 4+ para las dos últimas.
6. **EL HALO ARCOÍRIS**: al entrar al mundo llega EL HALO ARCOÍRIS (regalo) — equiparlo
   y ver el aro pequeño de siete franjas girando alrededor de la cabeza (combina con
   cualquier forma ascendida).
7. **LA FORMA 3 MÁS VIVA + EL REGALO COMPLETO**: el trono con la banda arcoíris DOBLE
   (girando más rápido), los rayos del nimbo, las perlas mayores y la iluminación de
   mundo; y que el regalo entregue TODO sin errores (el crash del log, muerto).
8. **REGRESIÓN**: la Forma 4 (Serafín), la mascota-jefe, el rayo sin fantasmas y la
   entrada en dos actos siguen intactos.

*(El checklist de la .53 — trono con arcoíris, Serafín, rayo sin fantasmas, mascota-jefe
y estallido — sigue en el historial de abajo: todo cubierto por los puntos 1, 7 y 8 de
arriba y por la regresión.)*


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

1. **El usuario prueba v6.50.62 en juego** — LOS TESTS CRÍTICOS de la .62 (LAS FAUCES):
   (a) LAS ARMAS: 5 de madera cada una (La Fauce del Grimorio, El Tajo de las
   Sombras, La Sombra de la Página); (b) EL LATIGAZO QUE SIEMPRE LLEGA: invoca
   un jefe, aléjate AL OTRO LADO DEL MAPA y dispara — el tentáculo cruza el
   mundo; (c) EL DREN: la barra del jefe baja A MORDIDAS; (d) LA DEVORACIÓN:
   al 1 HP el jefe se POSA (ni IA ni muerte) y el festín lo come (bruma,
   almas, y el LOOT CAE dentro de la bruma — la kill cuenta: bestiario,
   downed flags, XP del grimorio); (e) LA ESFERA del tajo: los ojos TE MIRAN
   a TI mientras comen; (f) Aethon (el del mod) muere con SU cine de siempre;
   (g) la chusma muere normal (solo jefes se devoran).
   (Y el checklist .61 aún vigente: oleada en el spawn, la purga, el lore.)
2. **El usuario prueba v6.50.61 en juego** — LOS TESTS CRÍTICOS de la .61:
   (a) LA OLEADA EN EL SPAWN ORIGINAL: quédate EN el pueblo (junto a las
   casas del Guía y compañía) y desata la furia con la Carnada — la chusma
   debe LLEGAR sin moverte del sitio (el vigía tarda ~2,5 s en arrancar);
   (b) LA PURGA: la Bolsa de Invocadores solo entrega el Nombre de Aethon,
   el bestiario SIN los 5 NPC muertos, y matar a Aethon suelta SU esencia
   (la única de jefe de mod que queda); (c) EL LORE: el tooltip del
   Grimorio del Eterno cuenta que el libro ES Aethon, la fase 5 se anuncia
   «El Veredicto» y la Forma Ascendida cae con «LA PRUEBA ESTÁ CERRADA».
   (Y el checklist de la .60 aún vigente: firma de cada fase, sol con
   espoleta, cuna limpia, furia nocturna.)
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
| **v6.50.62** | ✅ Build-verificada (0/0, .tmod auditado 392 entradas EOF/dataLength exactos, DLL con las 9 clases nuevas vía Cecil, headless 0 excepciones), ✔ publicada (release 403485786, CDN byte a byte), ⏳ en juego | LAS FAUCES DEL GRIMORIO — la letra del usuario (armas de sombras estilo Pride de FMA:B que devoran jefes): (1) **LA DEVORACIÓN** (FaucesGlobalNPC: CheckDead 1ª pasada → life=1+dontTakeDamage+return false — la muerte vanilla NUNCA arranca; PreAI false = jefe POSADO sin IA ni animación; motor 210 t: FauceDevoradorProjectile con erupción→envolver→festín (mordidas+oscuridad+almas)→bruma y polvo; final: life=0 + 2ª checkDead = muerte REAL con loot íntegro; AethonBoss excluido — su CheckDead propio corre antes; worms via DueñoDelPool; MP: BitWriter en SendExtraAI, 1 bit por paquete) (2) **ARMA 1 LA FAUCE DEL GRIMORIO** (el gif del usuario como spritesheet 6×3 re-troceado limpio: emergencia f0-5 → caza a CUALQUIER distancia (22-56 px/t escalado) → mordisco que drena 2.5%/6t → 1 HP = festín) (3) **ARMA 2 EL TAJO DE LAS SOMBRAS** (tajo.png del usuario vuela → ESFERA negra con 11-14 ojos que TE MIRAN + boca ecuatorial de colmillos, 3 mordidas de 12% → bruma y polvo) (4) **ARMA 3 LA SOMBRA DE LA PÁGINA** (100% código: masa/olas/fauces/garganta por quads, nace de la sombra del SUELO) (5) **SOMBRASLIB** (Columna Bézier viva, Masa con picos de sierra, Ojo con pupila roja, OjosDeMasa de reparto áureo, Fauces procedurales, DeGolpe k=4, Bruma alfa) |
| **v6.50.61** | ✅ Build-verificada (0/0, .tmod auditado 387 entradas EOF exacto, 13 muertos AUSENTES, DLL con NacerChusmaVigia y SIN PedirResonancia, headless 0 excepciones), ✔ publicada (release 403243039, CDN byte a byte), ⏳ en juego | LA PURGA DE NPC + EL VIGÍA + EL LORE NUEVO — feedback de la .60 (TRES frentes): (1) **EL VIGÍA DEL FESTÍN** (la oleada vacía en el spawn original: Player.townNPCs>=1 BLOQUEA el motor natural sin condición — decompile IL verificado; NacerChusmaVigia sirve la comida del pool en cunas PosicionLimpia cuando el festín ayuna <3 vivos tras 150 t, cada 15 t) (2) **LA PURGA** (borrados HollowTitan/RiftKeeper/EchoArcher/EchoBlade/TheWitness + 4 llamados + 4 esencias + PresenciaNPCSystem + tienda + MsgPedirResonancia + ~90 líneas de localización por idioma; SOLO Aethon — el CazadorAstral es su ataque Manada; .plr y contadores de esencias respetados) (3) **EL LORE** (Aethon ES el grimorio: pelea = LA PRUEBA, fase 5 «El Veredicto», esencia = la página arrancada, mod renombrado «Aethon, el Grimorio Eterno», ~15 textos es+en + description.txt) |
| **v6.50.60** | ✅ Build-verificada (0/0, .tmod auditado 400 entradas EOF exacto, headless 0 excepciones, caza del sol SIMULADA en 5 escenarios), ✔ publicada (release 402696157, CDN byte a byte), ⏳ en juego | LA DECIMASÉPTIMA RONDA — feedback de la .59 (CUATRO frentes): (1) **EL ATAQUE ESPECIAL DE CADA FASE** (EspecialDeFase/EntrarEspecial/_especialPendiente: P1 SOL — también APERTURA de toda pelea — / P2 CATEDRAL / P3 TELAR / P4 VÓRTICE / P5 CORONA; la firma abre la fase tras el decreto con prioridad absoluta; EST_SOL en TODOS los menús — el arsenal estaba encerrado en P3+) (2) **EL SOL DE VERDAD** (asunción 50 t + cañonazo 11 px/t + giro 0.11 + crucero 10.5 + relevo 14 >500 px + ESPOLETE <240 px → gigante 30 t que deriva a la presa → explosión 640 px; render por _solGigante/_tickGigante/_solExploto; SIMULADO: botas→270 px, montura→636) (3) **LA CUNA LIMPIA** (PosicionLimpia: aire 3×3, sin muro en superficie, sobre el suelo — escupitajo del Devorador y TODAS las coreografías; capa de tierra: nada >10 tiles bajo el portador) (4) **LA NOCHE DEL GRIMORIO** (chusma nocturna natural NO alimenta — NPCID.Sets.Zombies + ojos/licántropo/luna de sangre; comida de oleada no toca el reloj; furioso = piso en el umbral con bocados de 45 s; reintento cada 5 s: furioso+mundo libre = festín YA) |
| **v6.50.59** | ✅ Build-verificada, ✔ publicada (release 402633707, CDN byte a byte), ✔ probada (con feedback → .60) | LA DECIMASEXTA RONDA — feedback de la .58 (DIEZ frentes): (1) **EL RELOJ DE TODA LA FASE 2** (permanente ai[1]=2: inmortal mientras la fase 2 viva, disolución 40 t al pasar a fase 3; plato fuera del menú con el permanente vivo; el estado suelta a los 120 t) (2) **LA CAZA DEL SOL QUE NO SE PIERDE** (sin cañonazo 7.2→4.8, giro 0.075 converge ~0.8 s, gravedad comba +0.012, TRES RITMOS: 6.4 cerca / 7.6 crucero / 9.8 relevo lejos — la .58 frenaba a 4.4, MÁS LENTA que la carrera) (3) **EL TEMBLOR VUELVE** (Item161 EoL MUERTO → Item122 grave ×2 creciendo + kicks) (4) **LA AURORA EN TODO EL CIELO** (el VELO blanco-dorado de borde a borde + 11 CORTINAS de 4 tramos curvados + luz de mundo — no solo partículas) (5) **OLEADAS ×5** (spawn ×0.039, tope 50+20k techo 170, festín 25 min, puntos 60+30k) (6) **NIVELES DE FURIA** (ShardPlayer.FuriaNivel persistente: primera=1, vencer sube N+1 tope 10, morir congela; vida/daño ×(k+1)·(1+0.20(N−1)), defensa +2k+3(N−1)/+6k+8(N−1), anuncios e indicador con nivel) (7) **BIOMA EN TIEMPO REAL** (firma releída cada 30 t → pool reconstruido al vuelo) (8) **MULTIBIOMA POR NIVEL** (tu bioma peso doble + (N−1) acompañantes de la MESA DISPONIBLE — Santuario SOLO tras el Muro de Carne; nuevos principales Hallow y Meteorito) (9) **TODO MÁS AGRESIVO** (chusma 20 t/empuje 0.26/techo 13+; jefes 12 t/homing 0.09/embite y coreografía ≈×2) (10) **EL DEVORADOR ESCUPE** (cabeza <520 px de frente → 2-3 monstruos de la Corrupción DESDE LA BOCA cada 80 t + coreografía en abanico desde la boca) |
| **v6.50.58** | ✅ Build-verificada, ✔ publicada (release 402604951, CDN byte a byte), ✔ probada (con feedback → .59) | LA DECIMOQUINTA RONDA — feedback de la .57 (CUATRO frentes): (1) **LA PERSIGA DE VERDAD** (steering: el rumbo GIRA hacia el jugador 0.045/t, rapidez 7.2→4.4 px/t, gravedad=comba +0.018 — la aceleración vieja quedaba enterrada bajo la inercia) (2) **EL DISCO DEL PROYECTIL SOL** (el pipeline del arma ENCIMA del sol del dios: glow coronal + backglow + RadialShine + EL DISCO SunShader, blanco-dorado→brasa, 144→860 px, fundido en 50 t al explotar — la explosión sigue siendo la de la casa) (3) **LA BRUMA MASIVA** (120 nubes en 3 capas + 3/tick de goteo + 90 chispas + 18 quads: la estrella LIBERA su materia) (4) **EL CRASH DE LA ARQUERA** (Begin-sobre-Begin cada frame: cadena correcta de lotes + SONDA INCONDICIONAL en FlushAdditive/FlushAlpha) |
| **v6.50.57** | ✅ Build-verificada, ✔ publicada (release 402409678, CDN byte a byte), ✔ probada (con feedback → .58) | LA DECIMOCUARTA RONDA — feedback de la .56 (SIETE frentes): (1) **EL SOL DEL DIOS** (el ataque especial: el jefe SE CONVIERTE en sol — asunción 80 t — y LO LANZA: persiga lenta + gravedad + GIGANTE ROJA ×6 + explosión de luz/bruma/formas con daño AoE 640 px honesto) (2) **LA COREOGRAFÍA** (la cadena de los god-bosses: SOL→TELAR→DANZA→LANZAS→CORONA→ESTALLIDO cada 6-9 ataques en P3+, sin pasar por la órbita) (3) **LA IA DEL DUELISTA** (órbita que lee distancia + quiebro del sentido + memoria doble) (4) **LA BOLA FINAL SIN PERSECUCIÓN** (línea recta) + **EL DIOS MUERE DETENIDO** (velocidad 0 desde t=20, flash final 50 t hasta 4800 px) (5) **LAS CALAVERAS DEL LIBRO POTENCIADAS** (proyectil 837 real: gigante ×2.1-2.8, veloz, brillante, ×1.35 daño, estela dorado-violeta por CalaveraPotenciadaFX) (6) **EL VOLTEO DE GRAVEDAD MUERTO** (FlipGravity eliminado, anuncio retirado es+en) (7) **EL client.log LIMPIO** (2 FormatException de .plr corruptos de vanilla, 1 h 50 min sin excepciones del mod) |
| **v6.50.56** | ✅ Build-verificada, ✔ publicada (release 402301259, CDN byte a byte), ✔ probada (con feedback → .57) | LA DECIMOTERCERA RONDA — feedback de la .55 (SIETE frentes): (1) **EL GEL INVISIBLE** (crash 1: Draw sin Begin en la bola de gel — SU PROPIO LOTE ALFA) (2) **EL FUNERAL EN HILO AJENO** (crash 2: ThreadStateException al salir del mundo — QueueMainThreadAction + AprenderHiloPrincipal) (3) **EL HALO ARCOÍRIS PLANO Y MÁS CERCA** (13 px, rotación 0, giro en las perlas) (4) **LA MINI-EXPLOSIÓN CABALGA CON LA MASCOTA** (ai[1]=whoAmI) **SIN ARO CIRCULAR** (solo brillo) (5) **LA MASCOTA ALUMBRA ×1.6** (6) **LA MUERTE QUE SE ENCIENDE + LA BOLA FINAL BLANCO-DORADA** (EstiloBolaFinal=23: el proyectil sol en blanco-oro, 7 s, luz 2.6/2.35/1.7) (7) **EL REY GELATINA**: limos DESDE SU CUERPO + gel con cada salto Y teletransporte + LA CHUSMA sin muros ni subsuelo (SpawnTileY/WallType) |
| **v6.50.55** | ✅ Build-verificada, ✔ publicada (release 402236002, CDN byte a byte), ⏳ probada (con feedback → .56) | LA DUODÉCIMA RONDA — LA MINI-EXPLOSIÓN DE LA MASCOTA (un pedido): «la mascota seria aburrida si fuera un punto de luz constante» → EL SALUDO DE LUZ (lo primero al invocar: 45 t) + EL COMPÁS (base ~1/20 min, +1/60000 por enemigo cerca, tope 20; EL GARANTE de 1 hora) + EL ESTALLIDO DEL JEFE EN MINIATURA ~0.13× (las SIETE piezas de la .53: 14 rayos, anillo de 7 emisores, cruz, núcleo Bloom ~130 px, estrella, onda, 8 bokeh + chispas + luz pequeña + estampido bajito) + LA RECOGIDA translúcida (aro 135 px, 6 agujas, el pet ×1.45 mientras) + EL DAÑO SIMBÓLICO (15 plano, hitbox honesta 270×270 solo 8 t, 1 golpe por enemigo) + LA GRATITUD (cura 1% de vida máx por enemigo, tope 5%) — TODO determinista (semilla en ai[0]), CERO texturas nuevas |
| **v6.50.54** | ✅ Build-verificada, ✔ publicada (release 402229633, CDN byte a byte), ⏳ en juego | LA UNDÉCIMA RONDA — feedback de la .53 (OCHO frentes): (1) **EL DECRETO CABALGA**: el ataque del cambio de fase centrado EN EL JEFE y moviéndose CON él (2) **EL ESTALLIDO CON COMPÁS**: 1×P1, 2×P2 + más grande, P3+ cada 1-9 ataques ALEATORIO, y 1/2/3 detonaciones por activación (3) **EL DASH CORTO Y CENTRADO** (4) **EL RELOJ ÚNICO GIGANTE** ×7.5 siguiendo al dios (5) **LAS TRES ARMAS DE LA EMPERATRIZ**: DANZA SOLAR + LANZAS ETERNAS + CORONA ETERNA (6) **EL HALO ARCOÍRIS** (ítem nuevo, la banda de la Forma 3 alrededor de la cabeza) (7) **LA FORMA 3 MÁS VIVA** (banda doble, perlas mayores, rayos del mandorla, luz de mundo) (8) **EL CRASH DEL REGALO MUERTO** (YaLoTiene con .Length — el IndexOutOfRange del client.log) |
| **v6.50.53** | ✅ Build-verificada, ✔ publicada (release 402101571, CDN byte a byte), ⏳ en juego | LA DÉCIMA RONDA — feedback de la .52 (CINCO frentes): (1) **EL ARCOÍRIS DE LA FORMA 3** («el arcoíris debería estar en la forma ascendida 3»): BUG DE TRES VERSIONES — el dispatcher `DibujarJugadorAditivo` despachaba `Divino`/`Divino2` pero el caso `Divino3` NO EXISTÍA: `DibujarDivino3` compilaba y viajaba en la DLL… y NADIE lo llamaba (el trono entero — nimbo, cruz, mar de vidrio, lámparas, ofanim, alas prismáticas y EL ARCOÍRIS de Ap 4:3 — JAMÁS se dibujó en .49→.52); LA CURA = UNA línea de dispatcher · (2) **LA FORMA 4: EL SERAFÍN** («crea una 4 forma ascendida, asegúrate de que sea algo divino: alas, halo, corona, aura celestial, luz, bruma, más luz y destello, mejor iluminación» — Isaías 6): SEIS ALAS en tres pares (8+10+7, cálamo+punta), HALO TRIPLE trisagión, CORONA Ap 19:12, NIMBO Radio×3.4, RAYOS DE DIOS, BRUMA SANTA Is 6:4, CUERPO QUE ARDE, destello, plumas; Radio 140, luz 1.55/1.38/1.02, ítem horneado+anillo animado, regalo al entrar, vuelo infinito · (3) **EL FANTASMA DE LOS NPCS** («hay algún ataque que vuelve transparentes a los NPCs, creo que cuando lanza rayo»): el PerlinBolt dejaba el lote ABIERTO EN ADITIVO (RayoStrip reabría para sus gorros; ReabrirLoteVanilla = NO-OP) → todo NPC tras el jefe salía aditivo/transparente; el finally cierra y devuelve SIEMPRE el lote de vanilla · (4) **LA MASCOTA-JEFE** («que sea exactamente el jefe pero más pequeño»): las SEIS secciones del sol de código a escala 0.22 con el MISMO compás · (5) **EL ESTALLIDO RADIANTE** («te envié una imagen, crea por código un ataque igual a la imagen»): EST_ESTALLIDO (fase 3+, peso 5 al pegado) — 60 t de recogida (aro 600 px + 14 brasas en espiral + núcleo + agujas) y EL PUNTO DE LUZ (proyectil 19): onda 0,95× radio 600 (12 t, hitbox autocurada) + 152 t de 44 rayos 360° (250-980 px, núcleo blanco + halo oro→ámbar→brasa), anillo segmentado 14 emisores, cruz anamórfica, núcleo Bloom×4 ~950 px respirando, estrella 8 rayos, onda, 26 bokeh, 42 GoldFlame, LUZ QUE INUNDA 2,4/2,1/1,5, Item122+kick 13 px — TODO determinista (Hash01), anuncio `Jefe.Aethon.Estallido` es+en |
| **v6.50.52** | ✅ Build-verificada, ✔ publicada (release 401940561, CDN byte a byte), ⏳ en juego | LA NOVENA RONDA — feedback de la .51 (CUATRO frentes): (1) **EL ARCOÍRIS DEL ÍTEM** («no se ve el arcoíris en el item de la forma ascendida 3»): el borde desaturado del png → EL ARO ANGULAR HORNEADO (rojo arriba girando por el espectro, núcleo blanco intacto) + EL ANILLO ANIMADO en `PostDrawInInventory` (la banda `VFXCore.Arcoiris` girando alrededor del icono, MISMO lote de la UI, cero Begin/End) · (2) **EL JEFE SIN ARCOÍRIS** («el jefe no necesita tener un arcoiris»): sección 4.5 del PreDraw MUERTA — el dios es oro y núcleo blanco; la banda vive en Forma 3 + mascota · (3) **LA ENTRADA EN DOS ACTOS** («la presentación debe durar hasta que el sol llegue al centro, luego aparece el jefe» + «demora mucho el suelo temblando»): ACTO 1 PRESENTACIÓN=CARRERA (lluvia todo el cielo hasta el final + temblor creciente solo aquí + reloj bidireccional TODO JUNTO, jefe INVISIBLE, mín 150 t) → ACTO 2 EL APARECER (80 t: PILAR + DESTELLO curva 20/40/80 + materialización 24 t DENTRO del pilar directo a la órbita — nada fuera de pantalla) · (4) **EL ESPEJO PURO + PARACAÍDAS** (la cura del «no aparece»: la .51 avanzaba vía rate de vanilla y si vanilla no lo aplicaba se colgaba): vanilla a rate 0 durante TODA la carrera, el espejo mueve Main.time a mano en AMBAS direcciones (cruces alba/ocaso en ambos sentidos; rate=dist×0.25 techo 220× — peor caso 3,6 s; POSADO en 27000 sin deriva) + a los 570 t la IA posa el sol a mano — el jefe APARECE SIEMPRE · SIMULACIÓN 17/17 + 20.000 aleatorias × 400 t |
| **v6.50.51** | ✅ Build-verificada, ⏳ en juego | LA OCTAVA RONDA — feedback de la .50 (CUATRO frentes): (1) **EL ARCOÍRIS DE VERDAD** («yo no veo nada, ni en la forma ascendida 3 ni en el jefe, ni en la mascota»): la .50 lo prometía con hilos BLANCOS + perlas de 10 px = confeti; LA CURA = **LA BANDA HORNEADA** `VFXCore.Arcoiris` (512² EN CÓDIGO, cero assets: siete franjas saturadas rojo→violeta, banda [0.58,0.92] del semiancho, RGB premultiplicado, ~80 px de grosor) en los TRES sitios: el trono de la Forma 3 (24 perlas SOBRE la banda + 4 joyas + rim), el jefe en plena pelea (sección 4.5 del PreDraw) y la mascota en miniatura · (2) **EL NIMBO DEL PANTOCRÁTOR** («no se ve nada celestial ni divino»): el disco dorado + aro de los iconos bizantinos detrás del dios · (3) **LA ENTRADA SOLO COLOR LUZ Y TODO EL CIELO** («es multicolor… que sea solo color luz y que se reprodusca en todo el cielo»): lluvia BLANCA y DORADA alternada (hslToRgb muerto) naciendo en el rectángulo entero de la cámara alrededor del jugador (4/tick + clon); el AURORA 874 MURIÓ (prisma por naturaleza) · (4) **LA CARRERA AL MEDIODÍA DE VUELTA — RELOJ BIDIRECCIONAL** («mantén temblor/reloj/pilar/descenso… el tiempo avanza o retrocede según qué tan lejos o cerca esté el sol del centro»): CINCO ACTOS (presenta 180 t sub 9 → temblor 150 t sub 10 con la luz ascendiendo `SeguirCielo` → carrera sub 11 `TicksHaciaElMediodia` camino más corto: tarde/noche nueva RETROCEDE (el espejo resta a mano en PreUpdateTime con cruces en reversa), madrugada/mañana AVANZA (rate vanilla), rate=dist×0.08 techo 110× piso 1, el sol SE POSA en 27000 → climax 120 t sub 12 (PILAR + DESTELLO del sol) → descenso 90 t sub 13 con chispas doradas); cerrojo .42 vivo; ColaSierpeSky sub 9-12; SIMULACIÓN 12/12 casos con la dirección exacta de los 4 ejemplos del usuario |
| **v6.50.50** | ✅ Build-verificada, ⏳ en juego | LA SÉPTIMA RONDA — feedback de la .49 (CUATRO frentes): (1) **EL CRASH DE LA MASCOTA** (client.log): `AethonMenorPet.PreDraw` hacía `FlushAdditive(null,true); return true` — el Flush deja el lote CERRADO y el `true` mandaba a tML a dibujar ENCIMA → «Draw was called, but Begin has not yet been called» + «End was called, but Begin has not yet been called» en Main.DrawProjectiles:18973 = Main engine crash AL INVOCARLA; contrato de la casa restaurado (`FlushAdditive` + `ReabrirLoteVanilla()` + `return false`) · las DOS «Excepciones silenciosas» del mismo log (SolVivo línea 741 / LenteAbismo 510: Begin pelado tras helper que deja el lote ABIERTO — mordidas/lenguas JAMÁS dibujadas) curadas con `CerrarLoteSiAbierto()` · el «Expected Re-Logic file format» del log = un .plr CORRUPTO del usuario (ajeno al mod) · (2) **EL REGALO DE PRUEBAS** («no pongas recetas, daselos directamente al jugador»): las tres recetas BORRADAS (Forma 2: 10 maderas · Forma 3: Forma 2+20 Fragmento Génesis · Menor: 5 maderas); `ShardPlayer.OnEnterWorld` entrega Forma 2 + Forma 3 + Aethon Menor al ENTRAR AL MUNDO (una copia por ítem — `YaLoTiene` inventario 0-58 + armadura 0-19 + misc 0-9; al hueco libre o QuickSpawnItem; mensaje `Mensajes.RegaloPruebas` es+en) · (3) **EL TRONO SIN HUMO** («la menos divina de todas, solo es humo»): la .49 era TODO SoftGlow (13 capas difusas = UNA MANCHA); EL NÚCLEO NÍTIDO — el arcoíris son DOS LÍNEAS continuas de Ring con 36+14 perlas GlowOrb + 4 joyas DestelloFinal · la CRUZ lleva SU beam sólido de Pixel (7 px, 0.9) + flare en el cruce · el MAR DE VIDRIO tiene SU ARO (Ring) + retícula de Pixel · los OFANIM: GlowOrb 18×11 + PUPILA sólida de Pixel · el HALO TRIPLE son TRES aros de Ring + 12 perlas + ticks de Pixel · la CORONA de 24 estrellas y las chispas son DestelloFinal + cuenta GlowOrb · cada pluma del serafín SU CÁLAMO de Pixel + punta de luz · llaves huérfanas Presentacion/LlegadaLuz BORRADAS (es+en) · (4) **LA ENTRADA DE LA EMPERATRIZ, PALABRA POR PALABRA** (AI_120 case 0 del decompile, 180 t): proyectil vanilla 874 HallowBossDeathAurora en Center+(0,−80) + SoundID.Item161 al t=10 + LLUVIA ARCOÍRIS (dust 267 RainbowMk2, hslToRgb(t/180), 2/tick + clon blanco) + caída (0,5)·×0.95 + FADE IN alpha=255·(1−t/180) + TargetClosest al t=180; anuncio SOLO el de SpawnBoss («ha despertado»); `GetSource_FromAI` por el interno `GetSpawnSource_ForProjectile` |
| **v6.50.49** | ✅ Build-verificada, ⏳ probada (con feedback) | LA SEXTA RONDA — feedback de la .48 (CUATRO frentes): (1) **EL FIX DEL VUELO INFINITO QUE NUNCA CORRIÓ**: el código de la .48 estaba muerto por timing (banderas encendidas en `PostUpdate`, un hook TARDE — decompile: ResetEffects 24723 → PostUpdateEquips 24914 → PostUpdate 27293); ahora los ítems las encienden en `UpdateAccessory`/`UpdateVanity` (dentro de UpdateEquips, como vanilla con empressBrooch) + relleno duro de wingTime + wingsLogic 27 (Mothron) sin sprite (Player.wings=0: las plumas del aura son las alas) · el diagnóstico del «parpadea/reinicia su posición» = cero escrituras del mod a la física del jugador + el velo frontal allocaba un AuraPerfil POR FRAME (ahora cacheado) · (2) **LA FORMA ASCENDIDA 3: EL TRONO** (FormaAscendidaTresItem = Forma 2 + 20 Fragmento Génesis): la iconografía del Apocalipsis — ARCOÍRIS alrededor del trono (2 aros de 36+28 perlas, cada una su color del espectro) · MAR DE VIDRIO (placa + retícula de 24 destellos) · SIETE LÁMPARAS DE FUEGO orbitando · RUEDAS DE OFANIM (16+10 ojos de luz contrarrotando) · HALO TRIPLE (30+20+12 + las 12 marcas del zodíaco) · CORONA DE VEINTICUATRO ESTRELLAS (3 arcos de 8) · LA CRUZ DE LUZ de la Maiestas Domini (columna del cielo + brazo del horizonte DETRÁS del dios) · ALAS PRISMÁTICAS (4 bancos, 28 plumas/lado, cada pluma SU matiz del arcoíris) · luz de mundo blanca entera · Radio 132 · 36 orbes · vuelo infinito · (3) **EL SEGUNDO JEFE BORRADO** (AethonSegundo + AtaqueJefe2Projectile + invocador + 3 texturas + 12 claves; la Forma 2 SE QUEDÓ) · (4) **LA ENTRADA DE LA EMPERATRIZ para el primer jefe**: case 661 literal (200px+jitter50+SpawnBoss) + presentación de ~45 t — LA CARRERA AL MEDIODÍA MURIÓ (temblor/reloj/pilar/descenso borrados; el reloj del mundo YA NO SE TOCA; CheckActive ya no puede matarlo: nace dentro del rectángulo) · (5) **EL AETHON MENOR**: la mascota de luz de vanilla (lightPet+NeedsUUID) — el sprite del jefe a media escala, corona de 8 perlas, luz del arcoíris girando (drop 20% + 5 maderas) — probada en juego: el vuelo infinito SÍ voló, pero la mascota CRASHEABA al invocarla (el contrato del lote), la Forma 3 se leía como humo, la entrada del jefe no tenía NADA de la Emperatriz y los ítems nuevos venían con recetas que nadie pidió → v6.50.50 |
| **v6.50.48** | ✅ Build-verificada, ⏳ probada (con feedback) | LA QUINTA RONDA (la más grande del ciclo) — feedback de la .47: (1) **LA FORMA ASCENDIDA 2** (un ítem NUEVO, la 1 intacta): la apoteosis ABSOLUTA — las once capas elevadas (Radio 88→116, orbes 22→30, columna 760 px, 12 rayos, halo 36+24+16) + LAS CINCO CAPAS DEL ARTE SACRO (MANDORLA · CORONA DE DOCE ESTRELLAS · SIETE CANDELEROS · RÍO DE LUZ · plumas dobles) + LAS ALAS 28 plumas/lado en 4 bancos con extensión −82°..+78° COMPLETA (la .47 abarcaba la mitad superior) + VUELO INFINITO en ambas (empressBrooch; sin alas: wingsLogic inyectada, wings=0 — las plumas del aura SON las alas) · (2) **BARRA XP ADAPTATIVA** (baila bajo la última fila de buffs: ceil/11 a 50 px) · (3) **AETHON, LA SEGUNDA LUZ** (invocador #2, entrada LITERAL de la Emperatriz: 200px+jitter50+SpawnBoss del case 661; 5 fases que HEREDAN TODO: lemniscata→carrera prismática→parpadeo→pentagrama→furia con LA CORONA; arsenal Astilla/Prisma/Anillo/Lluvia/Corona/CambioPrisma; drop la Forma 2; texturas por remapa HSL violeta→rosa/oro→cian) · (4) **TRES FIXES DE OLEADA**: préstamo de zona (Cerebro/Devorador ya NO SE VAN — vanilla los mataba fuera de su bioma), Skeletron sin 9999 de día (aiStyle 11 lo vestía de guardián), y el hook de la barra escala la referencia fresca por multiplicador×factor de largo (el desborde era vida×oleada ÷ vida-vanilla; fill sin Clamp) · (5) **COREOGRAFÍAS TEMÁTICAS**: 6 dientes de librería MUERTOS; cada guardián convoca a LOS SUYOS con material vanilla (abejas 566/181+aguijón 719 · sirvientes 5 · limos+GEL como proyectil con gravedad/rebote/salpicón · creepers+Carmesí · Devorador 3× (67→192, enlaze AI_006)+Corrupción · esqueletos 21+HUESOS 21 en 3 figuras) — probada en juego: el vuelo infinito que prometía salía MUERTO (las banderas se encendían un hook tarde: ver la .49), el movimiento de las formas no se leía fluido, el segundo jefe «se ve horrible» y la entrada de la Emperatriz había que dársela al PRIMERO → v6.50.49 |
| **v6.50.47** | ✅ Build-verificada, ⏳ probada (con feedback) | LOS NPC FANTASMA + LA APOTEOSIS — feedback de la .46 (DOS pedidos): (1) «algunas ataques del jefe, hace que los NPC sean semitransparente»: los CUATRO ataques de estructura (reloj gigante ×4, coro, telar, decreto) dibujaban en el pase normal de proyectiles, DESPUÉS de los NPCs — sus velos (la MASA alpha-blend «el polvo que OCLUYE» + la arena ×5.2) cubrían a los NPC del pueblo = fantasmas. FIX: `DrawBehind → behindNPCs` (el pase que dibuja tras los tiles y ANTES de toda criatura, verificado en el decompile): NPCs y jugador SIEMPRE sólidos encima; balas rápidas siguen pasando por delante · (2) «la forma Ascendida no se ve tan divino y sagrado… tiene que ser mas divino, mas sagrado»: LA APOTEOSIS — el patrón Divino de 7 a ONCE capas (EL CORAZÓN BLANCO ardiendo en el pecho · LA COLUMNA DEL CIELO que sigue al portador + su charco de luz · 7 rayos divinos · CÍRCULO RÚNICO TRIPLE con banda de escrituras · GRAN HALO CELESTIAL DOBLE de 30 perlas + aro fantasma contragirando + 12 ticks · CORONA DE LA SANTIDAD de 5 estrellas · ALAS DEL SERAFÍN de 12 plumas en dos bancos con aleteo majestuoso · 8 chispas · LAS PLUMAS QUE CAEN · 22 orbes); Radio 74→88, luz del mundo ×1.35, tooltip es+en nuevo — probada en juego: el fix de profundidad quedó BIEN, pero la apoteosis aún no leía como divina, las alas se quedaban a media altura y había que crear un ítem NUEVO en vez de cambiar el existente → v6.50.48 |
| **v6.50.46** | ✅ Build-verificada, ⏳ probada (con feedback) | LAS ARMAS QUE NO FUNCIONABAN + EL VÓRTICE PRIMORDIAL — feedback de la .45 (SIETE pedidos): (0) EL HALLAZGO: el estilo del proyectil vive en ai[0] y el coro/telar/decreto de la .45 LO SOBRESCRIBÍAN → los TRES ataques estaban MUERTOS (invisible e inerte desde sus primeros ticks — por eso «el jefe lo hace mal» y «no lo usa al cambio de fase»); LEY DE ORO: nadie toca ai[0] · (1) LOS TAJOS DEL JEFE MUEREN (runas con abanico de 3 pernos) · (2) EL ANILLO DEL TIEMPO: 4 relojes ×5.2 cayendo del cielo alrededor de la presa (arena corre ya aterrizado; renderer con la MISMA edad de arena) · (3) EL TELAR TRAZA LA ESTRELLA: pentagrama (5) en fase 4 · heptagrama (7) en furia — cada punta que el jefe toca CLAVA su estrella; jaula en orden natural (EstrellasClavadas compacta) + círculo veloz vigilando · (4) EL DECRETO FUNCIONA: inmóvil + 80→600+120·(fase−2) px (P5 960), ejecuciones cada 15 t · (5) EL ECLIPSE MURIÓ de raíz → EL VÓRTICE PRIMORDIAL (galaxia de 3-4 brazos × 12 pernos, tangencial + colapso; rayos del sol ×4 mientras) · (6) LA GRAVEDAD DE VERDAD (volteo inmediato) + diálogos auditados (Nombre2 «La Canción del Tiempo», Nombre4 «El Telar») — probada en juego: la ronda quedó BIEN, pero los ataques de estructura volvían fantasmas a los NPCs y la Forma Ascendida seguía sin verse divina → v6.50.47 |
| **v6.50.45** | ✅ Build-verificada, ⏳ probada (con bug) | EL JEFE QUE SE ESFUMABA (CheckActive cine + sombra que sigue) + LAS CINCO ARMAS DEL MOD (reloj ×2.6, coro, manada CazadorAstral, telar, decreto) + LA FORMA ASCENDIDA DIVINA (patrón de 7 capas) + EL ALTAR REESCRITO — probada en juego: el despawn quedó BIEN, pero el CORO/TELAR/DECRETO resultaron MUERTOS (el bug ai[0] de la .46), el reloj era uno solo y pequeño, el telar corría el círculo sin figura, los tajos de las runas eran feos, el eclipse final se veía mal y la gravedad no se notaba → v6.50.46 |
| **v6.50.44** | ✅ Build-verificada, ⏳ en juego | EL DESCENSO DEL CIELO (la entrada de la Emperatriz: pilar de luz del cielo + jefe bajando por él) + EL JEFE QUE TE LEE (memoria de ritmo, fintas, bolsa ponderada, seis ataques mejorados con espirales/guiados/minas/contracruz) + Anillos del Horizonte BORRADOS + Corona Rúnica fusionada en La Forma Ascendida DIVINA (corona de 7 luces, rayo del cielo, huella, pulso) + sprites de Génesis y Altar regenerados |
| **v6.50.43** | ✅ Build-verificada, ⏳ en juego | EL LLAMADO A CUALQUIER HORA (y el Verdugo que ya no se gasta) — feedback de la .42: «no puedo invocar al jefe de noche… no tiene sentido ya que al invocar el jefe el tiempo pasa hasta que el sol está en el centro del cielo» + «el item que sube de nivel el grimorio es un consumible, que no sea consumible, +10 niveles por uso sin consumirse»: (1) `ConvocableDeNoche` en `LlamadoDeJefe` — EL NOMBRE DE AETHON responde A CUALQUIER HORA (la llegada ya sabía correr la noche: rama nocturna de la carrera simulada en .41 + recorrida empíricamente en .42-S2; despawn solo por presa); los cuatro guardianes siguen de día (lore intacto, su presencia no mueve el reloj) · (2) Verdugo de Niveles: `consumable=false`, `maxStack=1` (patrón Carnada) — +10 niveles por uso SIN gastarse · tooltips actualizados es+en («A CUALQUIER HORA» / «Reutilizable») |
| **v6.50.42** | ✅ Build-verificada + validada en headless, ⏳ en juego | EL JEFE QUE NO APARECÍA (y el sol que no se quedaba fijo) — feedback de la .41: «el sol avanza como está previsto, pero al llegar al centro no queda fijo y el jefe no aparece»: UNA causa para ambos — la materialización usaba la matemática pantalla→mundo en la máquina que corre la IA y el servidor NO tiene pantalla → el jefe nacía FUERA DEL MUNDO y moría en el tick 44 del climax → el espejo soltaba el reloj → el sol seguía su curso. REPRODUCIDO en servidor headless (la técnica del CLIENTE FANTASMA: un `ISocket` falso enciende el loop vanilla del servidor) y VERIFICADO el fix: PosicionBajoElSol server-segura (sobre el jugador), exacta en cliente, NUNCA enterrada (≥300 px sobre el jugador), NUNCA fuera del mundo (clamp) · EL CERROJO DEL MEDIODÍA (un reloj pasado de 27001 VUELVE a 27000; jamás la vuelta entera) · limpieza ColaSierpe en servidor |
| **v6.50.41** | ✅ Build-verificada, ⏳ probada (con bug cazado) | EL MEDIO DÍA DEL DESTELLO (cuatro pedidos): (1) LA CAPA DE OSCURIDAD MUERE DE RAÍZ (VeloLib + VeloDona borradas; fuera el sol negro, las luces, el aviso del Grimorio y el flash de pantalla completa — paquete 396→395) · (2) EL SOL SIN TELETRANSPORTE DE VERDAD (bug de la v6.50.40: la tarde disparaba el aterrizaje al instante y el sol saltaba HACIA ATRÁS; ventana [26999, 27001] + restante hasta el PRÓXIMO mediodía POR LA NOCHE: tarde → ocaso → noche completa → amanecer → mediodía, 110× máx con aterrizaje — simulado en 5 escenarios, jamás hacia atrás) · (3) EL DESTELLO NACE DEL SOL (brillo radial centrado en él, difuminado a TRANSPARENTE en los bordes — y el sol NO se apaga) · (4) AETHON NO NACE DEL CENTRO DEL SOL (se materializa BAJO él vía la inversa de la matriz de vista; acto 13 = EL DESCENSO) — el usuario la probó: la carrera bien, PERO el jefe no aparecía y el sol no se quedaba fijo (la materialización server-rota) → v6.50.42 |
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
