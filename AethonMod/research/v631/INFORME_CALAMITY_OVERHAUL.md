# INFORME R2 — CALAMITY OVERHAUL (灾厄重铸 / CWR) COMO ARQUITECTURA Y DISEÑO
**Task ID 51 · Agente R2 (investigación, sin código de producción) · v6.31**

Petición del usuario: *"analizarlo bien por si puedes aprender algo de ese mod y llevarlo a
nuestro proyecto"* — CWR como **arquitectura y diseño**, no como sus 2 armas estrella
(Star Tomb y Dragon's Word ya analizadas a fondo por R3/Task 50, informe
`INFORME_STAR_TOMB_DRAGON_WORD.md`, fuentes en `search_results/t50/`).

Método: catálogo COMPLETO del sitio oficial descargado (calamity-overhaul.cc, 10 páginas
CN nuevas + las 5 de R3), árbol completo del repo oficial (`repo_tree.json`, 8.068 entradas,
v0.9209) analizado por carpetas, **9 archivos de arquitectura nuevos descargados** a
`search_results/t51/src/` (GsGunRecoil, ShockRingDraw, ProjectileLayerRender, DrawStateJanitor,
CWRSound, ZoomOpticModule, RecoilStockModule, PlayerCloneRenderer, DynamicCameraTrack),
re-lectura generalizadora de las fuentes de t49/t50 (BaseHeldGun, OniFlashStep 1.156 líneas,
CrimsonRendSlash 1.933, TimeFreezeSystem 1.243, EffectLoader 442, WarpEffectRender,
RenderQualitySafety, EocTelegraph.fx, EocScreenFX…) y señales de popularidad (Steam Workshop:
6.602 valoraciones, 4/5 estrellas, 3.970 comentarios, 414 hilos de discusión; reseñas
textuales requieren login → no disponible, se usan títulos de discusiones + inversión del
changelog como proxy). z-ai `web_search` siguió en 429; Reddit/pullpush/wikigg bloqueados
por IP (403); DuckDuckGo/Bing funcionaron para descubrimiento.

---

## 0) EL MOD EN CIFRAS — de qué tamaño hablamos

| Métrica | Valor | Fuente |
|---|---|---|
| Archivos .cs | **4.678** | repo_tree.json |
| Shaders .fx | **499** (todos en Assets/Effects) | repo_tree.json |
| Tipos de partícula PRT | **116** | Content/PRTTypes |
| Armas del catálogo | **96** (41 melee · 28 ranged · 18 magic · 9 summon) | sitio oficial /cn/items/ |
| Accesorios / armaduras | 45 (18 "reliquias brutales") / 7 | idem |
| Armas legendarias (sistemas completos) | **4**: Halibut, SHPC, Kikasa, Onikiri | /cn/legend/ |
| Re-works de armas VANILLA (GodSmith 神工开物) | **412 archivos** en 23 familias | repo GameModes/GodSmith |
| Jefes vanilla re- hechos (Brutal 残酷) | 17 jefes, 342 archivos | wiki + repo BrutalMobs |
| Chips de protocolo (Hack Time) | 63 (mundo) + 14 (PvP) | wiki /cn/mechanics/hack-time/ |
| Escenarios/submundos de historia | 15 carpetas (OldNet 86, Dungeonworld 169, SupCal 38…) | repo Content/Scenarios |
| Localización | 521 archivos hjson (zh + en + ru) | repo Localization |
| Steam | 6.602 ratings, 4/5★, 3.070 comentarios, 414 hilos de discusión | steamcommunity 3161388997 |

**La idea-fuerza del diseño**: CWR no añade "más armas a Terraria" — convierte armas en
**sistemas con progresión propia**. Las 4 legendarias tienen su propio HUD, sus propias
monedas internas, sus 22–24 "pruebas de jefe" que suben el nivel DEL ARMA (Onikiri: daño
12 → 6.002), y hasta su propia pantalla de tutorial con opción de saltar. El resto del mod
es el mismo principio a otras escalas: GodSmith re-hace TODAS las armas vanilla con combos
y dos botones; Brutal re-hace los jefes; Hack Time convierte cualquier objetivo del mundo
en un "hackeable" de Cyberpunk. **Todo lo que tocan, lo convierten en un sistema con dos
formas de uso y una economía visible.**

---

## 1) INVENTARIO DE CONTENIDO

### 1.1 — Catálogo de armas (96) por mecánica destacada

Selección de las más notorias (nombres oficiales CN/EN, daño, mecánica — todo del catálogo
oficial; la línea neutrón completa comparte el patrón **dos formas de uso**):

| Arma (CN / slug EN) | Clase | Daño | Mecánica de identidad |
|---|---|---|---|
| **星墓之光 / neutronwand** | Magia | 355 | Sembrar púlsar anclado al cursor / frenado magnético → starquake (R3 la cubrió) |
| **龙言 / dragonsword** | Magia | 682 | Lágrimas de dragón / decreto circular que ejecuta en área (R3 la cubrió) |
| **黑洞使者 / neutronscythe** | Melee | 482 | Guadaña-neutrón que vuela y dispara rayos gamma al enemigo más cercano, deja puntos de warp detonantes / **der.: 黑洞爆发 (ráfaga de agujero negro)** |
| **黑域斩切 / neutronglaive** | Melee | 855 | Onda de espada gravitatoria que al impactar genera una explosión de estrella de neutrones / der.: carga |
| **恒星猎手 / neutrongun** | Ranged | 580 | Balas gravitatorias que generan puntos warp / **der.: modo francotirador** |
| **洛希之弦 / neutronbow** | Ranged | 152 | Flechas gravitatorias que sueltan rayos gamma / der.: carga (Tidal/flecha-marea) |
| **群星巨舰 / starship** | Ranged | 186 | Disparo sostenido acumula CALOR y la cadencia sube con el calor (sin recarga) |
| **湮宇星矢** | Ranged | 1.050 | Las flechas dejan una estrella clavada en el punto de impacto |
| **MG42 / mg42** | Ranged | 882 | El cañón se recalienta con fuego sostenido y convierte balas de mosquete en **perforantes** |
| **雪崩M60 / avalanchem60** | Ranged | 62 | Ametralladora de bolas de nieve (guiño Chainsaw Man: "电次，我们来打雪仗吧") |
| **扶柩者 / pallbearer** | Ranged | 666 | "El portador del ataúd" — lanzador fúnebre |
| **统帅链锯EX / commanderschainsawex** | Melee | 2.840 | Motosierra del Destructor (0.5 knockback, useTime 3 — sierra literal) |
| **统帅之杖EX / commandersstaffex** | Magia | 202 | **5 rayos que convergen girando y cortan en espiral** |
| **寰宇咏叹调 / ariaofthecosmos** | Magia | 285 | Mantener izq. condensa un cuásar; soltar = daño devastador |
| **万魔殿 / pandemonium** | Magia | 320 | Carga de "magia de ultra-posición" (Overlord) |
| **朗基努斯之矛 / spearoflonginus** | Melee | 2.480 | Al sostener: cruz sagrada bajo el jugador que absorbe energía |
| **刻心者 / heartcarver** | Melee | 1.666 | "Mientras la sostienes, el cuch late con tu corazón" |
| **化境 / flawless** | Melee | 920 | "La punta cambió siete veces; el blanco tiene un solo agujero" |
| **苍穹破晓 / cosmiccalamity** | Melee | 11.200 | La espada de daño absurdo del final |
| **纠缠之怨 / weavergrievances** | Melee | 455 | Garras de espectros; efectos especiales en infierno/mazmorra |
| **银河鞭挞 / whiplashgalactica** | Summon | 302 | Látigo = columna de la serpiente cósmica |
| **血色鞭挞 / bleedingscourge** | Summon | 191 | Marca de muerte + lluvia de sangre |

(Fuente: catálogo oficial completo en `t51/pages/cn_items_index.html` — 271 ítems con
descripción, daño, knockback y useTime.)

**Detalle de identidad**: TODA entrada del catálogo lleva una **frase poética de una línea**
("龙一生只能流一次泪" — un dragón solo llora una vez en la vida; "深处的东西，从不松钳" —
lo de las profundidades nunca suelta la pinza). El lore-by-tooltip es parte del producto.

### 1.2 — Las 4 armas legendarias (el contenido estrella, 740 archivos)

| | Qué ES realmente | Sub-sistemas |
|---|---|---|
| **大比目鱼 Halibut** | Un pez-arma de INVOCACIÓN que crece investigando peces | 43 "peces-habilidad" coleccionables (se equipan en barra de 10), **dominio de océano** con 9 ojos periféricos + ojo central (suben capas y nivel de "bloqueo"), 6 habilidades de dominio, barra de "resurrección abisal" que al llenarse **te mata**, 14 pruebas de jefe, rueda de habilidades en combate, tutorial integrado |
| **SHPC** | Una pistola inteligente **totalmente modular** | **6 slots de mods** (cañón/óptica/energía/culata/empuñadura/corredor) — cada mod cambia el game feel (velocidad de haz, vida, cadencia, homing con TRADEOFFS: p. ej. ZoomOptic: +60% vel. haz / −18% cadencia / −24% homing), diccionario de mods, **Cyberspace** de 3 capas (campo con teleport/reinicio/destierro/congelación; la 3ª cambia el cielo), mesa de moldes (desmonta 3 / aleatorio 4 / fija 6), 22 pruebas |
| **鬼伞 Kikasa** | Una sombrilla de invocación que crece | Lluvia de tinta al sostener izq.; der.: **cascada de tinta cargada**; los esbirros golpean MÁS a objetivos con marca de tinta; **lago de sangre** (dominio con superficie/interior, "giro表里" a mundo-espejo 鬼梦), teleport entre charcos, ahogar enemigos o guardar ítems en el lago, **27 talismanes** (cuerda de lluvia máx. 3), 4 niveles por nº de esbirros (1-3 llovizna / 4-6 tiro lateral / 7-9 andanada / 10-12 diluvio con charcos), 24 pruebas |
| **鬼切 Onikiri** | Una katana melee de complejidad de juego de lucha | Combo 緋红裂空 de **5 tiempos** (cada tiempo = 1 golpe/objetivo), **2 barras propias: 气力 stamina (100, regen 6/s tras 0.8s) y 架势 postura (100)**; postura 50 = 灭世一闪; postura 100 = **ejecución con i-frames totales**; 神威疾走 dash (30 stamina, 38→94 tiles); 樱流 vuelo de pétalos; **鬼域 dominio con DOS mundos** (表 = papel washi amarillo / 里 = inframundo de tinta; los vivos dejan "sombras de papel" que se queman al volver); desmembramiento en el mundo 里 pagando 25% de vida; **36 inscripciones** en 3 huecos del filo (nakago/hi/bori); 点鬼簿 registro de 6 fantasmas (se sellan 3), daño 12→6.002, 22 pruebas |

(Fuente: `t51/pages/legend_*.txt` — 5 páginas oficiales con listas de keys, tablas de
niveles y descripciones de combate.)

**Por qué son lo más celebrado**: dominan el discurso comunitario (títulos de discusiones
Steam: "Legendary Weapons need a QOL change heavily (Overwhelming asf)", "Onikiri or kikasa
not spawning", "Can we have an option to reduce the effects of Legendary Weapons?", "I miss
the Murasama" — 14 respuestas, nostalgia por la 5ª legendaria retirada cuando Calamity
implementó la suya propia). La queja recurrente NO es "son malas" sino **"abarrotan"**:
lanzan al jugador decenas de sistemas a la vez (OP del hilo de Kikasa: *"I just spawned in.
Why can I do ALL of this when I just showed up???"*). Y el changelog 0.9208 dedica secciones
enteras a pulir SU game feel (v. §4) — la inversión de desarrollo señala qué aprecian.

### 1.3 — GodSmith (神工开物 / "Arte Divino"): 412 re-works de armas VANILLA

Un flag de mundo independiente que **no añade ítems**: reescribe el COMPORTAMIENTO de todo
el arsenal vanilla por familias, con una línea dorada "神匠重铸" añadida al tooltip. Sin
tocar la dificultad. Familias (wiki /cn/mechanics/godsmith/):

- **Melee**: broadswords (combo), shortswords (estocadas rápidas), spears (estocada a
  media distancia + lance montada), **flails (mantener = molinetear la cadena; soltar = lanzar)**,
  **boomerangs (trayectoria en 3 fases + clic der. para redirigir en el aire)**, yoyos (clic
  der. = órdenes), "melee oddities" (cada una a su manera — recreación del aiStyle 75 de
  Arkhalis, base `GsOdditiesFlurryHeldBase` en t49).
- **Ranged**: arcos de carga, **arcos de andanada (acumular andanadas; der. = dispararlas
  antes)**, **pistolas primitivas con cargador virtual + recarga con clic derecho**,
  armas hardmode (der. = alternar modo de fuego), **lanzadores (der. = detonar tus propias
  balas)**, arrojadizos (lanzar seguido acelera; recuperación con tope).
- **Magic — 4 arquetipos**: **灾变 Cataclismo** (izq. = vanilla; a capas llenas der. =
  cataclismo en 3 fases), **咏chant Cantar** (golpear al ritmo acumula resonancia; a tope
  potencia el siguiente disparo), **导流 Conducir** (el CALOR es el recurso; der. = válvula
  de escape), **塑形 Moldear** (forma A / forma B cargada).
- **Summon**: esbirros (der. = órdenes), centinelas (der. = overclock manual), **látigos
  (combo al ritmo; marcas llenas = EJECUCIÓN)**.
- **Armadura**: cada SET vanilla gana una "dotación" (神赋) adicional — nunca reemplaza la
  original desde 0.9208 ("加法 no 算 sustitución"); cada metal tiene una **firma de
  material** (cobre: cada 6º golpe salta un arco estático al 2º enemigo; hierro: quieto 1s
  = postura de hierro con inmunidad a knockback; titanio: barrera de tormenta cuyos
  fragmentos suben crit; adamantita: 12 cargas = andanada de brasas) + 4 debuffs de firma.
- Prefijos de "dotación" en el pool de re-forja cuando el modo está activo.

### 1.4 — Brutal Bosses (残酷Boss重制): 17 jefes vanilla rehechos

Reglas transversales (wiki /cn/mechanics/brutal-bosses/): estadísticas primero a línea
maestra y luego escaladas por nivel; **cada ataque tiene telegrafía visible que BLOQUEA la
dirección — "预告锁住方向后，这一击不会再跟着你转"** (cerrado el telégrafo, el golpe YA
NO TE SIGUE: justicia legible); agarres con círculo/línea/grieta en el suelo (escapar del
área = garra al aire; ser agarrado = cinemática que un compañero puede romper atacando la
extremidad); fase nueva = ventana sin daño de contacto y corona/vestuario que se quita;
**enrage por abandonar el arena** (no cambia el moveset: sube daño, baja el que haces, la
pared persigue más rápido); muerte con lock de daño y cinemática (la corona cae primero; un
ninja-sombra escapa del cuerpo del Rey Slime antes de derretirse); cada jefe suelta una
**reliquia brutal** (18 accesorios con identidad: 黑闪印记 "cuando el destello se vuelve
negro, el instante domina a la eternidad" — el Moon Lord abre con un destello negro).
Ambiente: **cuervos del bosque que se alborotan cuando un enemigo fuera de pantalla te
fija** (aviso natural de combate — "Damn crows", hilo Steam, quejándose de que no se puede
desactivar).

### 1.5 — El resto del ecosistema

- **Hack Time (骇客时间)**: tecla N → el mundo se congela (SP), la cámara acerca al cursor,
  escaneas por hover CUALQUIER cosa (NPC, drops, proyectiles, tiles, líquidos, torretas,
  contenedores, PARTES de jefe, estados del mundo, tu propia cibergánica, jugadores PvP),
  panel de protocolos con coste en RAM y tiempo de subida (0.7–4 s; cuanto más fuerte, más
  lento y más caro; multiplicador por peligro del objetivo hasta ×3 en jefes). 63+14
  protocolos en chips de un uso vendidos por TBUG.
- **Sistema industrial** (236 archivos): generadores eólicos/hidro/térmicos con MK2, cañerías
  de energía y de ítems, taladro eléctrico, reciclador, forestador, **versiones "salvajes"
  hostiles que aparecen en asteroides**.
- **Ciber-órganos** (63): instalados en la clínica del NPC Victor; Sandevistan (ralentiza tu
  percepción 70% durante 5 s, enfriamiento 15 s — el accesorio del catálogo), ojos de red,
  esqueleto autorreparable…
- **Escenarios/historia** (15 submundos): la cazadora triple de la Bruja de Azufre, el
  protocolo Achero­n de Draedon, el Duque Viejo, 2 NPCs-regalo (Mayo/Shenyo), el OldNet
  (inmersión de red a lo Cyberpunk con "Blackwall"), la gamba de cristal abisal…
- **Rueda del Tránsito (往生轮)**, quest log con 42 carpetas, 12 buffs, dificultad "Asura".

---

## 2) PATRONES DE CÓDIGO — lo que CWR hace mejor, generalizado

*(Los ficheros citados: `t51/src/` = nuevos de esta tarea; `src/` = sesiones previas;
`t49/`, `t50/` = agentes R1/R3. Los números de línea son reales.)*

### 2.1 — Arquitectura de render por CAPAS con interfaces minimalistas
`src/Content_Renders_ProjectileLayerRender.cs` (85 líneas): los proyectiles implementan
**IPrimitiveDrawable / IAdditiveDrawable / IOverlayDrawable** (8/10/13 líneas cada una).
Un RenderHandle con `Weight` ordenado recorre `Main.projectile` UNA vez por frame, llena 3
buffers preasignados (64/64/16) y dibuja: primitivas (cada cual gestiona su SpriteBatch),
**capa aditiva** (solo hace Begin si hay contenido), **capa de OCLUSIÓN encima** (para que
el cuerpo tape su propio brillo). Cuarta interfaz **IWarpDrawable** para el warp de pantalla
(`src/Content_Renders_WarpEffectRender.cs`: copia pantalla→RT, dibuja fuentes de warp en el
RT swap, el shader reescribe, luego capas custom; bandas separadas con/sin blueshift).
→ **Regla**: el orden de dibujo NO se negocia en cada proyectil; es una canalización global.

### 2.2 — El holdout de dos botones como plataforma universal
`t50/Content_Projectiles_BaseHeldGun.cs` (467 líneas): `SpawnHeldProj<T>()` crea UN
proyectil "en la mano" (timeLeft=2 auto-renovado, se destruye al soltar). El ítem es
`noUseGraphic + ItemUseStyleID.Shoot` y su Shoot solo hace spawn. Todo vive en el holdout:
**WantsFireLeft / WantsFireRight** (`CanRightClick` virtual), posiciones de mano
idle/apuntando (4 constantes + ángulo de reposo 12°), **recoil declarativo** (GunPressure,
ControlForce 0.01, RecoilRetroForceMagnitude, recuperación 0.6), offsets de boca, luz de
boca, `AmmoState` (VISTA PREVIA de munición sin consumirla), `CanDamage()=>false`,
`ShouldUpdatePosition()=>false` ("no sigue la velocidad, evita el tirón"). **Patrón sistémico**:
la línea neutrón ENTERA (5 armas) y las 4 legendarias usan dos formas de uso; es la firma
del mod. MP: `CanMouseNet` reduce los paquetes de pose de apuntado de ~60 Hz a ~15 Hz
(`(GameUpdateCount + whoAmI) % 4 == 0`) "solo afecta la suavidad del brazo en los
espectadores; durante el fuego va a plena velocidad".

### 2.3 — Retroceso y cámara como MATEMÁTICA DOCUMENTADA (el archivo-joya)
`t51/src/Content_GameModes_GodSmith_Weapons_Guns_GsGunRecoil.cs` (139 líneas, leerlo entero):
- **GsGunRecoilProfile** (readonly struct): Shift (retroceso px), Kick (rad), Shake/ShakeRot
  (sacudida amortiguada), ShakeFreq 2.1 rad/frame ("≈ un vaivén cada 3 frames"), Damping
  0.84, Screen (fuerza de PunchCameraModifier).
- **Derive(shift, kick)**: perfil completo desde 2 parámetros — la sacudida escala con el
  retroceso, y **la sacudida de pantalla SOLO para armas pesadas** (fórmula
  `clamp((shift−3)·0.8, 0, 3.2)`: escopeta 6px→2.4, cuatro-cañones 4px→0.8, ráfaga SIEMPRE 0).
- Envolvente = (animación restante)² — pico en el frame de salida y caída.
- **Wobble**: seno amortiguado con **semilla = whoAmI** (misma fase en todos los clientes:
  determinismo MP sin sincronizar nada); wobble=0 en el frame de salida ("primero el
  retroceso limpio, DESPUÉS la sacudida — no le roba el frame").
- Rotación por **diferencial de objetivo absoluto** (Δ = deseado − ya-aplicado) porque
  itemRotation se "snapea" una vez y luego nadie lo recalcula: la contabilidad se
  auto-cura al llegar a cero → "sin puerta myPlayer: los espectadores TAMBIÉN ven el patada".
- **ScreenPunch**: solo jugador local + solo si el config de cliente ScreenVibration está
  activo (accesibilidad); dirección = inversa de la puntería ("la cámara recibe el empujón
  del arma"); PunchCameraModifier(strength, 6f, 7 frames).

### 2.4 — Hit-stop global con "leases": TimeFreezeSystem
`src/Content_TimeFreezes_TimeFreezeSystem.cs` (1.243 líneas): por-entidad (NPC Y proyectil):
fuentes de congelación en [Flags] (World, Cinematic), fuentes temporales con caducidad,
**fuentes mantenidas con arrendamiento (lease)** = (tipo, InstanceId de entidad, época,
velocidad-de-reanudación) para que un lease huérfano no congele a la entidad equivocada;
**prioridades de anclaje** (Default 0 / Effect 100 / Authoritative 200) y de reanudación;
snapshot de pose lógica → pose congelada; timeScales por fuente. `WorldFreezeSystem`
(209 líneas) congela el mundo ENTERO enganchando (VaultHook) Liquid.UpdateLiquid,
Player.UpdateEquips, ScrollHotbar, TrySwitchingLoadout y Quick* — **congelar el mundo sin
congelar la interfaz**, con propiedad por "reason" idempotente y fase de deshielo.

### 2.5 — El juicio diferido del iaijutsu (daño post-hoc)
`src/Content_LegendWeapon_OnikiriLegend_OniFlashSteps_OniFlashStep.cs` (1.156 líneas):
el dash (神威疾走, 170 px/frame, distancia fijada UNA vez por el cursor, sub-pasos de
colisión de 14 px "menores que el ancho del jugador, anti-tunelado", snap a suelo <17°,
roce <25°):
- Durante el dash los enemigos **solo se MARCAN** (tinta, cero daño) + micro-hitstop +
  chispas + sonido **con pitch ASCENDENTE por objetivo** (`0.55 + marked.Count·0.04`).
- **Todo el daño se liquida en el frame de envainar** (JudgmentFrame = inicio + frames del
  dash + 8): "clang" único, todas las marcas se abren a la vez, empuje de ambiente
  `min(0.16+n·0.03, 0.30)` y **shake escalado** `min(3+n·0.8, 8)`.
- **Fallo = silencio**: "挥空不响也不闪" — si marked==0 no hay clang ni flash (el juego te
  dice que fallaste NO sonando).
- El ancho del corredor de marcas (140 px) está **alineado al ancho visual de la cinta**:
  "el jugador juzga 'pasé a través' POR la cinta" (la hitbox debe igualar al dibujo).
- El jugador se OCULTA durante el dash (`HidePlayerDuringDash`): "la persona se convierte
  en un relámpago divino".

### 2.6 — La línea de tiempo de POSE del arma (el corazón del feel melee)
`src/Content_LegendWeapon_OnikiriLegend_CrimsonRendSlashs_CrimsonRendSlash.cs` (1.933
líneas), comentario de cabecera del método `UpdateBladePose` (línea 806):
> "Línea temporal de pose del cuchillo实体 (puramente visual): **amartillar-parada muerta →
> estallido-barrido → caída-congelación → parada/zanshin → envaine corto**; la profundidad
> conduce primer plano/segundo plano y perspectiva. **La sensación de fuerza viene del
> CONTRASTE: tras amartillar, quietud REAL; en el frame de caída el filo está totalmente
> sólido con UN frame de sobre-impulso; tras la caída NO hay ningún desplazamiento visible
> salvo el temblor de respiración**."

Cada beat del combo define rotación objetivo, profundidad (±px de z con amplitud por beat),
inclinación (lean) del cuerpo, estiramiento de brazo (`CompositeArmStretchAmount`),
sobre-impulso de un frame, pulso de escala de impacto y frames de retención de impacto
(recoil de impacto / impactHoldFrames). El 0.9208 re-hízo EXACTAMENTE esto: "重做绯红裂空斩
手感…出刀与收招的顿挫感更明确，命中判定也更跟手" + "el HUD del filo vibra con el beat
durante el final".

### 2.7 — Lenguaje de TELEGRAFÍA en shader
`src/Assets_Effects_EocTelegraph.fx` (95 líneas, con comentarios de diseño del autor):
carril de carga (LaneTech) y anillo de salida (RingTech); **color por material**: "aviso de
sangre: rojo vino profundo, NO naranja mecánico; el borde gotea húmedo"; **pulso que
acelera con el progreso** (`sin(uTime·(4.6+7.5·uProgress))`); línea dorsal fina + borde
suave + **hebras de sangre que fluyen hacia adelante** (franjas que avanzan) + **barrido de
progreso: la cabeza brillante corre del inicio al final y AL LLEGAR arranca el ataque**
(el telegraph ES la condición de disparo); anillo: borde principal + **círculo de cuenta
atrás que se contrae** + arcos que fluyen + goteo. Además EocScreenFX (85 líneas):
estática push/decay — `PushVignette/PushPulse/PushFlash` escriben objetivos, "si nadie lo
empuja decae solo" (vignette de compresión visual, flash de fase/muerte de 14 frames,
pulso cardíaco en vida baja). La regla brutal-boss §1.4 ("el golpe no te sigue tras el
telégrafo") es la contraparte de gameplay.

### 2.8 — 499 shaders y la higiene del pipeline
`src/Common_EffectLoader.cs`: TODOS los efectos viven en una clase estática con
`[VaultLoaden]` (auto-escaneo de carpeta) — nombres semánticos por concepto
(GolemSunTelegraph, FishronGrabVeil, MLordBlackFlash, BrainRift, WarpShader…).
`src/Content_Renders_DrawStateJanitor.cs` (37 líneas): **limpiador de estado** — como
decenas de shaders enlazan ruido en `GraphicsDevice.Textures[1..3]` y no lo devuelven
(= "jefes con texturas corrompidas"), un ModSystem pone `null` en los slots 1-3 en
`Main.OnPostDraw` CADA frame. `src/Common_RenderQualitySafety.cs` (137 líneas): puerta
técnica del pipeline — Retro/Trippy lighting y agua en baja hacen que vanilla dibuje
directo a pantalla (sin screenTarget): todo post-proceso debe preguntar
`ScreenTargetUnavailable()`; resolución REFLEJA del miembro de calidad de agua (los nombres
mutan entre versiones; se resuelve una vez y se cachea por frame). `DomainVisuals.Concise`:
config de cliente para dominios en modo simple — **y Onikiri lo ignora a propósito**.
`ShockRingDraw` (109 líneas): anillo de choque PARAMÉTRICO compartido (radio/grosor/3
colores/alpha/tear/squish/innerGlow/semilla) con **reglas de diseño en la firma**:
"el borde NO puede ser un círculo matemático limpio" (tear=0.9×grosor por defecto),
"innerGlow: valor pequeño para florecimientos, **0 para telegrafías**", margen 0.82/0.86
anti-recorte, **fallback a sprites si el shader falta** (y en el fallback: "forzar A=255
en lote aditivo o la capa desaparece", "SpriteBatch escala-antes-de-rotar: el squish solo
puede rotar si es círculo").

### 2.9 — Partículas como MINI-SISTEMAS (PRT, 116 tipos)
InnoVault PRT: pool + `Configure(...)` fluido + modo de blend por partícula. Los nombres
del catálogo revelan la filosofía: PRT_GravityVortex (vórtices keplerianos),
PRT_AccretionDiskImpact, PRT_GammaIonize, PRT_StarPulseRing (anillo con perlas de Baily),
PRT_GhostRainYank, PRT_DefLaserAfterline… R3 ya documentó el patrón "la partícula es un
mini-sistema completo, no un sprite". ZoomOpticModule enseña la coreografía de impacto:
"chispas en cono estrecho blanco-caliente estiradas A TRAVÉS del objetivo + 3 esquirlas
cuadradas perpendiculares para leer la dirección + anillo fino que CLAVA el punto de
impacto".

### 2.10 — Sonido: biblioteca curada + escalado + presupuesto
`t51/src/Common_CWRSound.cs` (116 SoundStyles con `[VaultLoaden]`): sonidos POR MATERIAL de
impacto (**HitTheFlesh_1/2 vs HitTheSteel**), mecánica de arma completa (CaseEjection,
ClipIn/ClipOut/ClipLocked, SlideForward, Shotgun_Pump, BoltAction), familia katana
(Sprint/A/B/Swing/Hit/HitB), Cyberpunk (Sandevistan, Scanning, Hacker), perro (Wuff/Worry).
Reglas de uso vistas en el código: pitch como CONTADOR (0.55+n·0.04 en el dash), presupuesto
global (máx 2 sonidos de corte por frame, R3), "el clang solo si hubo golpe".

### 2.11 — Persistencia por ÍTEM (LegendData)
`src/Content_LegendWeapon_OnikiriLegend_OnikiriData.cs`: cada instancia de arma lleva su
`InstanceId` (long) + revisión de edición + tienda de inscripciones + ruta de pruebas —
SaveData/LoadData/Clone con valores por defecto defendidos ("instanceId != 0 ? : crear").
El arma ES la partida. (Aethon ya hace esto con TestingPlayer/bolsas, pero no por-ítem.)

### 2.12 — Determinismo visual como política transversal
(consolidado de t49+t50+esta): fase de ruido anclada a longitud de arco mundial
(uLenPx/uOffPx — anti-deslizamiento de estelas), semillas whoAmI para fase de wobble MP,
"el ángulo polar jamás entra en ruido", "toda máscara muere antes del borde del quad",
salida premultiplicada, hash-gating de eventos aleatorios (parpadeos por ventana), y el
"mismo brazo para daño y dibujo" (Geombrazo). En OniFlashStep: `JudgmentFrame` calculado
como valor ABSOLUTO estable ("los tiempos se fijan de forma estable según la distancia decidida al salir").

---

## 3) LECCIONES DE GAME FEEL — por qué sus armas "se sienten" potentes

1. **La fuerza es CONTRASTE, no cantidad** (lema textual del autor en CrimsonRendSlash):
   parada muerta → estallido → congelación de aterrizaje. Un arma que siempre se mueve no
   pesa; el frame de quietud REAL antes del golpe es el que pesa. Ídem retroceso: envolvente
   cuadrada + wobble que EMPIEZA EN CERO.
2. **El golpe se liquida TARDE y de golpe** (iaijutsu): marcar en silencio con pitch
   ascendente → un solo "clang" que abre todas las marcas + shake escalado por nº de
   víctimas. El clímax es único, medible por el oído (pitch) y la cámara (shake n).
3. **El fallo también se diseña**: "挥空不响也不闪" — si no tocaste nada, no suena ni
   brilla. La confirmación de acierto es binaria y honesta.
4. **La hitbox sigue al DIBUJO, al revés de lo habitual**: ancho de corredor = ancho de la
   cinta visual, "porque el jugador juzga el pase por la cinta". Murasama ya lo hacía
   (200-320 px de colisión vs 14 de dibujo, R1) — CWR lo formaliza.
5. **Telegrafía = contrato**: el aviso acelera su pulso, el barrido llega al final Y
   ARRANCA el ataque; pasado el cierre, el ataque no persigue. Legible = justo = satisfactorio.
6. **La cámara es un instrumento con diner**: PunchCameraModifier solo en armas pesadas,
   dirección = reacción (opuesta al disparo), 7 frames, tope por fórmula, config OFF
   disponible. Y "los espectadores también ven el patada" (self-healing bookkeeping).
7. **Cada recurso tiene barra y ritmo**: stamina/postura de Onikiri, calor de MG42/Starship,
   resonancia de Cantar, RAM de Hack Time, carga de starquake. La economía visible hace que
   el jugador ENTIENDA cuándo su arma está fuerte.
8. **Sonido por material + mecánica**: carne vs acero, cargador entra/sale/cerroujo, pitch
   como contador de racha. El arma suena como máquina de verdad.
9. **El VFX se AUTO-LIMPIA por respeto al lector**: el 0.9208 cambió las salpicaduras de
   tinta a "floraciones instantáneas" porque las manchas persistentes tapaban el combate;
   una sola marca de tinta por enemigo (las worm-secciones dejaban "un collar de anillos");
   contornos hostiles solamente bajo el agua. Claridad > espectáculo.
10. **Dos formas de uso = dos TRANSFORMACIONES, no dos ataques**: en TODA la línea neutrón la forma
    der. es una TRANSFORMACIÓN (francotirador, carga, agujero negro, starquake) que cambia
    la relación con el recurso, no un segundo proyectil.

---

## 4) LO TRASLADABLE A AETHON — 14 técnicas concretas

*(mapeadas a nuestras librerías: LumenLib, PyraLib, EstelaLib, BrumaFX, RiftLib, OndaLib,
QuemaduraCósmica, VFXCore)*

1. **Perfil de retroceso derivado (`GsGunRecoilProfile.Derive`)** → en VFXCore o una nueva
   RetroLib mínima: struct con Shift/Kick/Shake/ShakeRot/Freq/Damping/Screen; `Derive(shift,
   kick)` para que CADA arma nueva configure feel con 2 números; shake de pantalla SOLO por
   encima de umbral de peso (fórmula clamp). PORQUÉ: es la diferencia entre "arma con stats"
   y "arma con peso"; CWR lo comparte entre 412 re-works. CÓMO: struct readonly + un Apply
   por frame en el holdout/proyectil con envolvente (animRestante)² y wobble seno·damping^t
   con semilla whoAmI.
2. **PunchCameraModifier con dirección de reacción y presupuesto** → reemplaza nuestros
   kicks actuales: dir = −aim, strength por fórmula, 7 frames, identidad única por arma,
   y config de cliente "vibración de pantalla" (accesibilidad — CWR lo tiene y Steam se lo
   agradece). Ya tenemos punch en TearImpacto: sistematizarlo.
3. **El "juicio diferido" (marcar → liquidar al envainar)** → para el arma de desgarro y
   cualquier katana/futuro melee estelar: marcar sin daño + pitch ascendente + flash/shake
   escalado por nº de marcas EN UN SOLO frame de clímax + silencio absoluto al fallar.
   PORQUÉ: es el momento más elogiado de Onikiri y ya tenemos la maquinaria (golpes
   manuales escuela A). CÓMO: HashSet<int> marcas + CheckAABBvLineCollision por sub-pasos
   de 14 px + frame de juicio absoluto.
4. **Línea de tiempo de pose con "parada muerta" y frame de sobre-impulso** → para las
   animaciones de cast (Bastón del Eclipse, Rencor): quietud REAL ≥3 frames antes del
   estallido, UN frame de overshoot al aterrizar, y luego SOLO temblor de respiración.
   Es gratis (nuestras armas ya son VFX puras) y es LA lección de feel #1 del archivo.
5. **Telegrafía-shader con barrido-que-dispara** → copiar EocTelegraph.fx como contrato:
   pulso acelerando con progreso (4.6+7.5p), franjas que fluyen hacia el destino, cabeza
   brillante que ARRANCA el evento al llegar — y la regla de gameplay "cerrado el telégrafo
   no te persigo" para cualquier futuro miniboss/jefe eclipse. Color por MATERIAL (nuestro
   eclipse: negro-violeta profundo, nunca naranja).
6. **Capas de render centralizadas por interfaces** → nuestros lotes aditivos/alfa están
   bien, pero falta la CAPA DE OCLUSIÓN (IOverlayDrawable) para que el cuerpo tape su
   propio glow (problema real v6.29 de la Eminencia) y un RenderHandle con Weight único.
   CÓMO: 3 interfaces de 8-13 líneas + un sistema que escanea Main.projectile una vez.
7. **DrawStateJanitor + RenderQualitySafety** → antes de meternos en post-proceso de
   pantalla (warp del desgarro, distorsión de agujero negro): limpiar Textures[1..3] en
   OnPostDraw y una puerta "¿screenTarget disponible?" (Retro/Trippy lighting) con fallback
   a sprite. Nos ahorra la familia entera de bugs "textura corrupta".
8. **Estado de pantalla push/decay (EocScreenFX)** → RiftLib.Oscurecer ya es push; añadir
   el modelo completo: Vignette (compresión de bordes) + Pulse (cardíaco en vida baja del
   objetivo) + Flash one-shot con vida en frames, estáticos con "empuja o decae". Para el
   eclipse y la supergigante.
9. **Sonido por material + pitch-contador** → migrar nuestros sonidos a una tabla curada:
   carne vs metal vs vacío para TODAS las armas (hoy usamos pitch fijo), y usar
   `pitch = base + n·k` como contador de racha visible por el oído. Mantener el presupuesto
   2/frame de R3.
10. **Anillo de choque paramétrico con reglas de diseño** → ShockRingDraw como plantilla
    para OndaLib: 3 colores (brillo/cuerpo/cola), tear SIEMPRE (≥0.9×grosor: "ningún borde
    de onda es círculo matemático limpio"), squish para ondas pegadas al suelo, innerGlow=0
    en telegrafías, fallback a sprites DiffusionCircle si falta el shader.
11. **Sistema de 2 formas como FIRMA del mod** → ya lo hacemos con Star Tomb/Dragon's Word
    recreations; formalizarlo: TODO arma nueva de Aethon nace con forma A/B donde B
    transforma la RELACIÓN con el recurso (carga, francotirador, colapso), nunca "otro
    proyectil". Es la marca de la casa CWR y encaja con nuestro cosmos (sembrar/desgarrar,
    encender/colapsar).
12. **Barra de recurso por familia** → el patrón MG42/Starship/Cantar: una barra visible
    (calor/resonancia) que la CADENCIA o el DAÑO consumen y que la forma B descarga.
    Traducción Aethon: "presión de fusión" para los soles rúnicos (a tope = fulgor que
    quema en área pero enfría 2 s), "marea" para estrellas. UI mínima: dos rects + números,
    como el HUD de Onikiri (气力/架势 en la esquina).
13. **Wobble/warp determinista con semilla whoAmI + throttle de red de puntería** → para
    MP: fase de cualquier oscilación visual = f(whoAmI) (cero sincronización) y pose de
    apuntado a 15 Hz para espectadores, plena durante fuego. Barato y elimina toda una
    clase de bugs.
14. **Changelog como documento de diseño** → el 0.9208 es un manual de prioridades:
    re-hacer el feel de UN ataque por versión, limpiar VFX que molestan la lectura, poner
    nombre serio a las mecánicas ("renombrado 攥→掐喉"), accesibilidad (config de vibración,
    dominios simples). Adoptar la disciplina: cada versión de Aethon pule UN momento de
    feel y remueve UN estorbo visual.

**Bonus (contenido, no código)**: la idea de 4-5 "armas-sistema" con pruebas propias está
fuera de alcance de Aethon hoy, pero el PATRÓN HALIBUT de "colección que se equipa" (43
peces-habilidad, barra de 10) sí es escalable: nuestras 10 bolsas por categoría podrían
algún día convertirse en "constelaciones equipables" (estrellas reales coleccionadas que
dan habilidades pasivas). Y la lección negativa de Steam ("Overwhelming asf"): **dos
sistemas nuevos por arma, no doce** — el usuario de CWR se ahoga; Aethon debe dosificar.

---

## 5) QUÉ NO COPIAR / RIESGOS

- La sobrecarga de sistemas de las legendarias (la queja #1 del workshop) — Aethon es un
  mod de armas cósmicas, no un RPG de armas.
- Sonidos de Calamity/asset-rips (R3 ya lo anotó) — CWR trae su propia biblioteca grabada;
  nosotros usamos vanilla + pitch.
- Easter eggs de otras IP (Chainsaw Man, Evangelion, FMA — los "EX" del catálogo) — nuestra
  temática es propia.
- 499 shaders es una CURVA de mantenimiento brutal: cada efecto un .fx con contrato escrito
  a mano. Aethon: pocas técnicas MUY paramétricas (el camino ShockRingDraw, no el camino
  "un shader por jefe").
- El inglés del wiki oficial es traducción parcial (p. ej. la rama EN aún lista "Murasama"
  como retirada sin contenido); para números, usar siempre el CN + el código.

## 6) FUENTES (todas en research/v631/)

- `search_results/t51/pages/` (NUEVO, 10 páginas oficiales CN + steam): cn_items_index
  (catálogo 271 ítems completo con textos), legend_index/halibut/shpc/kikasa/onikiri,
  mech_godsmith, mech_brutal, mech_hacktime, mech_index, changelog_09208,
  github_README.md, steam_workshop.html + steam_reviews/steam_thread_big (sentimiento),
  steam_disc*.html + steam_discussions.json.
- `search_results/t51/src/` (NUEVO, 9 .cs): GsGunRecoil, ShockRingDraw,
  ProjectileLayerRender, DrawStateJanitor, CWRSound, ZoomOpticModule, RecoilStockModule,
  PlayerCloneRenderer, DynamicCameraTrack.
- Pre-existentes re-leídas: `search_results/repo_tree.json` (8.068 entradas, base de todos
  los conteos), `search_results/src/` (TimeFreezeSystem, WorldFreezeSystem, EffectLoader,
  WarpEffectRender, RenderQualitySafety, EocScreenFX+EocTelegraph.fx, OniFlashStep,
  CrimsonRendSlash, OnikiriData, CWRItem/Projectile/Utils, CWRMod/CWRLoad),
  `search_results/t50/` (R3: BaseHeldGun, NeutronWand/Pulsar/Starquake, DragonsWord,
  CWRConstant, 5 .fx neutrones, partículas PRT, hjson zh/en, páginas wiki),
  `search_results/t49/` (R1: OniSlash 92 KB, CyberRiftSlashProj, Abyssrend* con hit-stop,
  Shatterfang, Dawnshatter, GsOdditiesFlurryHeldBase, 14 .fx de slashes).
- Búsquedas: `busquedas/r2_*.json` + `busquedas/t51/`, `busquedas/t50/` (sesiones previas).
- wikigg terrariamods.wiki.gg: bloqueado por Cloudflare (403 por reputación de IP) — el
  catálogo oficial lo suple.
