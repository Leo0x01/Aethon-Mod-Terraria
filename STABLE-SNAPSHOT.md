# AethonMod — ESTADO ACTUAL (v6.50.51)

> **Este documento = "¿por dónde nos quedamos?"** — se actualiza en cada entrega.
> Última actualización: 2026-10-01 (v6.50.51 local: commit + tag + bundle en /home/sync —
> **PUSH PENDIENTE: el token GitHub se perdió con el wipe del sandbox** (el usuario ya dio
> uno nuevo temporal en la ronda .50 — si caducó, pedir otro); al tener token:
> `git push origin main --tags` + release con `AethonMod.tmod` + verificación CDN.

## ✅ ESTADO VERIFICADO (build/forense, NO en juego)

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

La v6.50.51 está implementada y build-verificada. Checklist de la .51:

1. **EL ARCOÍRIS SE VE (EL PRIMERO)**: equipar la Forma Ascendida 3 —
   la banda de SIETE franjas alrededor del trono (~80 px de grosor,
   rojo fuera→violeta dentro) con sus perlas montadas; en la PELEA el
   jefe lleva SU anillo arcoíris alrededor; la MASCOTA trae el suyo en
   miniatura.
2. **LA FORMA 3 CELESTIAL**: el NIMBO dorado (disco + aro detrás del
   dios) + la banda arcoíris — ¿ahora SÍ se ve divino?
3. **LA ENTRADA SOLO LUZ Y TODO EL CIELO**: invocar al jefe de DÍA —
   la lluvia de la presentación es BLANCA y DORADA (nada multicolor) y
   cae por TODO el cielo (todo el ancho de la pantalla).
4. **LA CARRERA BIDIRECCIONAL**: invocar al jefe a distintas horas:
   - Por la TARDE (pasado el mediodía): el tiempo RETROCEDE — el sol
     vuelve por donde vino hasta POSARSE en el centro.
   - De NOCHE TEMPRANA (la noche acaba de empezar): RETROCEDE — el sol
     reaparece por el atardecer y vuelve al mediodía.
   - De MADRUGADA (la noche por terminar) o al AMANECER: AVANZA — el
     día entero pasa visiblemente hasta el mediodía.
   - En los CINCO actos: presentación → temblor (la luz sube al cielo)
     → carrera → EL PILAR cae + EL DESTELLO del sol → el jefe desciende
     con su lluvia dorada → pelea (con el sol clavado en el centro).
5. **LA MASCOTA YA NO CRASHEA** (el fix .50 sigue vivo) y **EL REGALO
   DE PRUEBAS** al entrar al mundo sigue intacto.


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

1. **El usuario prueba v6.50.42 en juego** con el checklist de arriba — LA
   PRUEBA DEL FIX: el jefe APARECE tras el destello (en el cielo sobre el
   jugador) y el sol SE QUEDA clavado en el centro toda la pelea (en SP y,
   si se puede, en host MP — el bug vivía en el servidor).
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
