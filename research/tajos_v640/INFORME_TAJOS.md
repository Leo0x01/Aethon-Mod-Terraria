# INFORME TAJOS — El tajo anime (katana slash) para el bastón v6.40
Subagente de investigación 2-a · fuentes: 46 búsquedas web + 14 lecturas profundas + 4 fuentes C# descargadas + código vanilla decompilado local (research/v633/src/vanilla). Material prima en `research/tajos_v640/busquedas/`.

---

## 0. RESUMEN EJECUTIVO (lo que hay que saber para implementar)

1. **El efecto tiene nombre**: es la combinación de DOS tropos documentados — **Sword Lines** ("líneas luminescentes o degradados que ilustran la velocidad y trayectoria de un golpe rapidísimo"; una forma de motion blur) + **Delayed Causality** ("el golpe ya terminó; los efectos simplemente decidieron esperar unos segundos antes de aparecer"). El retardo entre corte y tajo NO es un bug de percepción del usuario: ES la esencia del efecto (TVTropes lo define así palabra por palabra).
2. **El tajo anime NO es una línea recta**: es un **arco/crescente** (media luna). Los juegos lo llaman "crescent" (Roblox: "Crecent Sword Slash"; TVTropes lista "purple crescent lines", "yellow crescent lines", "solid blue curves"). Crece a lo largo del arco (revelado direccional), brilla con **núcleo blanco + halo de color**, y dura **poco**: 6 frames de animación en el pack CC0 de referencia (≈0.1-0.4 s), pico de brillo al 60% de la vida.
3. **El patrón vanilla 1.4.4 EXISTE y está replicado oficialmente en tML**: el Excalibur/aiStyle 190 es literalmente un "tajo" — sprite crescente de 4 frames barrido en semicírculo π, escala 1.0→1.6, opacidad en campana `Remap(p,0,0.6,0,1)·Remap(p,0.6,1,1,0)`, y **7 capas apiladas** con 3 "líneas finas blancas" (frame 3) con offsets de rotación ±0.05/±0.1 rad y escalas 1/0.8/0.6 — las "líneas blancas múltiples" del anime, hechas por el propio Re-Logic. ExampleMod lo expone entero (PR #3620, archivo descargado y leído: `excal.cs`).
4. **La técnica 2D recomendada para nuestro mod**: geometría de **anillo parcial (arco procedural) con UV continua** + revelado left→right por UV (la técnica "shader cuts in from two sides, revealing and hiding the head and tail" de Niels Dewitte, realtimevfx) + **núcleo/halo en dos capas aditivas premultiplicadas** (la semántica del lote aditivo que la casa ya dominó en v6.39: RGB=perfil, el alfa no existe). Nada de Main.rand (regla de la casa): las variaciones con Hash01 por semilla.
5. **El bastón**: arma mágica que "corta primero, taja después" — cortes invisibles marcados al apuntar, y los tajos (arcos blancos) APARECEN con retardo, crecen de izquierda a derecha y desaparecen. Las mecánicas más potentes encontradas: Judgement Cut (apilado → ráfaga retardada, The Stars Above), corte diagonal retardado (el enemigo se parte tras la pausa), y el "click" del enfundado como detonador (Single-Stroke Battle).

---

## 1. ANATOMÍA VISUAL DEL TAJO ANIME

### 1.1 Terminología (por si hay que buscar más)
- **Sword Lines** (TVTropes, https://tvtropes.org/pmwiki/pmwiki.php/Main/SwordLines): "Líneas luminescentes o degradados que añaden los artistas para ilustrar la velocidad y el movimiento de un golpe de arma realmente rápido". "Una forma de Motion Blur". Útil además como **indicador visual del alcance del arma** (los Dynasty Warriors las usan literalmente para pintar la hitbox). "Truth in Television: por persistencia de visión, tras un golpe rápido a veces puedes ver un arco real".
- **Delayed Causality** (TVTropes, .../DelayedCausality): "Una Dramatic Pause para escenas de acción. El héroe golpea... Pausa... y la explosión lo destruye. Para todos los efectos, la pelea terminó cuando cayó el golpe; los efectos simplemente decidieron esperar unos segundos". Cita emblemática: "You're already dead" (Kenshiro, Fist of the North Star).
- **Diagonal Cut** (TVTropes, .../DiagonalCut): el golpe "parece no haber hecho nada... unos segundos después la mitad superior empieza a deslizarse: el corte es tan limpio que el objeto se mantiene junto hasta que una fuerza externa (gravedad, viento, el rival usando su cuerpo) actúa". El origen del nombre: en los medios japoneses el corte canónico es diagonal de hombro a cadera contraria. En japonés el concepto está ligado al **iaijutsu/iaido** (el arte real del desenvaine-instantáneo; Reddit r/SWORDS: "the quick cut thing comes down to the art of iaijutsu").
- **Single-Stroke Battle** (TVTropes, .../SingleStrokeBattle): el duelo de un solo golpe: ambos pasan, uno enfunda con "click" audible, y **ese click ES la señal para que el perdedor se desarme**.
- **Judgement Cut** (Devil May Cry, wiki Fandom): "una serie de tajos que termina en un eco (ripple) azul claro" que aparecen instantáneamente alrededor del objetivo; la versión boss de DMC3: "decenas de distorsiones Judgement Cut barriendo la pantalla". Judgement Cut End: ráfaga multi-tajo de teletransporte.
- Vocabulario técnico de juegos: *trail/swoosh* (estela), *slash arc* (arco), *crescent* (media luna), *afterimage* (postimagen), *impact frame* (frame de impacto de anime — frame blanco/negro de 1-2 frames).

### 1.2 Forma, tamaño, brillo (medido + fuentes)
- **Forma**: arco de circunferencia parcial — **crescente (media luna)**. Grosor MÁXIMO EN EL CENTRO del arco y fino en las puntas (perfil de lente/hoja). Pack de referencia medido con numpy (Cethiel CC0, opengameart.org/content/weapon-slash-effect): canvas 126×150 px, bbox del trazo ~63-85×50-86 px, área visible pico 2867 px², **6 frames** por efecto, 5 efectos × 4 variantes de color. El **núcleo blanco solo existe en los frames intermedios** (frames 4-5: 48 y 60 px blancos >200 de luminancia; frames 1-3: 0) — el blanco "aparece" con el pico, no desde el inicio.
- **Brillo**: borde exterior/halo de COLOR (p.ej. dorado en Excalibur vanilla: 255,255,80 y 255,240,150) + **núcleo blanco** (las 3 líneas finas del Excalibur son `Color.White * 0.6/0.5/0.4`). Los anime suelen ser **blanco puro con núcleo de 2-4 px y halo de 8-20 px** que se disuelve.
- **Curvatura**: la circunferencia completa del Excalibur vanilla mide 94·escala px de radio efectivo y el sprite ES la media luna; el barrido cubre π (semicírculo, 180°) con offset de textura π/4 (cone máximo 45°). En anime el arco cubre típicamente 90°-150°.
- **Cuántos a la vez**: en el efecto "Judgement Cut" son **3-12 tajos simultáneos radiando en direcciones desacopladas** alrededor del objetivo (DMC wiki: "slashes fill the screen"); en el golpe normal anime: 1-3 tajos paralelos con offsets (el Excalibur pinta 3 líneas paralelas con offsets de rotación: +0.01, −0.05, −0.1 rad — el equivalente procedural exacto de las líneas múltiples del anime).

### 1.3 Duraciones (traducidas a ticks de 60 Hz)
- Anime estándar: 24 fps, animación "on twos" (12 dibujos/s, sakuga.fandom + wavemotioncannon.com) → un tajo de 2-6 dibujos = **0.17-0.5 s = 10-30 ticks a 60 Hz**.
- Pack Cethiel: 6 frames → a 15 fps efectivos = 0.4 s = **24 ticks** (4 ticks por frame si se quiere el feel de flipbook).
- Excalibur vanilla: vida = `ai[1] = player.itemAnimationMax` (≈20 con useTime 20) → **20-30 ticks totales**, con campana de opacidad con pico al 60% (tick 12 de 20).
- **Retardo corte→tajo (Delayed Causality)**: en anime "a few seconds" (1-3 s de pantalla); para un videojuego ágil la lectura correcta es **8-30 ticks (0.13-0.5 s)** — bastante menos que el anime, porque el jugador necesita causalidad legible. The Stars Above usa "a short delay" para su Judgement Finale; DMC5 da el cue en el momento del enfundado.
- Flicker: opcional; la casa ya tiene el patrón FlickTick 15 Hz + IsLit del v6.39 (HeridaElectrica) — un tajo anime NO parpadea normalmente (aparece fuerte y se disuelve); el flicker es más bien de impacto.

### 1.4 Crecimiento
- El usuario describe "crecimiento de izquierda a derecha". Técnicamente es **revelado direccional a lo largo del arco**: Niels Dewitte (realtimevfx, hilo leído completo): "The shaders cuts in from two sides, revealing and hiding the head and tail of the texture" — el shader recorta desde los dos extremos, revelando la cabeza y ocultando la cola, y "the mesh scales up over time, the center of rotation and scale is at the center of the circle it represents".
- En 2D sin shader: revelar por UV (recorte del sourceRectangle horizontal: `width·p`) o por **ángulo creciente** (dibujar el arco solo hasta el ángulo barrido). El Excalibur usa la segunda de forma implícita: la punta del semicírculo avanza con `rotation = π·dir·p`.

---

## 2. LAS FASES DE LA ANIMACIÓN

Tabla maestra (60 ticks/s). T = vida total del tajo (recomendado 20-24 ticks); R = retardo corte→tajo (recomendado 8-12 ticks).

| Fase | Duración típica | Transform | Fuente |
|---|---|---|---|
| (a) El corte en sí | 0-2 frames de anime (2-6 ticks) | El arma pasa (o NI SIQUIERA se ve el arma: flash step); el "impacto" real no muestra nada | Single-Stroke Battle, Diagonal Cut |
| (b) Retardo | R = 8-30 ticks (anime: 1-3 s) | Nada visible, o un indicio sutil (marca tenue, distorsión mínima); el juego sigue | Delayed Causality |
| (c) "Pop" de aparición | 1-2 ticks (0-6 ticks de rampa) | Alfa 0→1 brusco; el tajo entra a grosor/alto ~80-100% ya formado (no crece desde cero) | Cethiel frames 1→2 (área salta de 1678→2092); Excalibur Remap(p,0,0.6,0,1) |
| (d) Crecimiento/expand | 60% de la vida (12-14 ticks de 20) | Revelado left→right por UV; escala 1.0→1.6; rotación del arco avanza π·dir·p; núcleo blanco al máximo en el pico | Excalibur (escala 1+0.6p), Niels Dewitte (reveal dos lados + escala + rotación) |
| (e) Fade-out | últimos 40% (8-12 ticks) | Alfa cae Remap(p,0.6,1,1,0); grosor se estrecha (taper); puntas se disuelven primero; SIN flicker (o 1 parpadeo final opcional) | Excalibur; Cethiel frame 6 (maxA 172 vs 248) |

**Curva de opacidad canónica (vanilla Excalibur, copiada literal del decompile):**
```csharp
float p     = localAI[0] / ai[1];                                  // 0..1 vida
float brillo = Utils.Remap(p, 0f, 0.6f, 0f, 1f) * Utils.Remap(p, 0.6f, 1f, 1f, 0f);
// pico EXACTO en p=0.6: subida rápida 60%, caída 40%. Simétrica en p pero asimétrica en tiempo.
```
**Curva de rotación (el barrido del arco):**
```csharp
// ai[0]=direccion(+1/-1), ai[1]=vidaMax, localAI[0]=ticks vividos, velocity=vector de puntería
Projectile.rotation = MathHelper.Pi * ai[0] * (localAI[0] / ai[1])
                    + Projectile.velocity.ToRotation() + ai[0] * MathHelper.Pi + player.fullRotation;
```
**Curva de escala:** `scale = 1f + p * 0.6f` (Excalibur; True Excalibur: 1.2 + p·1.0).

---

## 3. TÉCNICAS DE RENDERIZADO 2D (procedural vs sprite; el cómo exacto)

### 3.1 Las cuatro escuelas encontradas
1. **Flipbook (sprites dibujados a mano)**: 5-8 frames pintados; máx. libertad artística, coste por color/variante. (Unity forum: "hand painted textures, colored flipbook"; pack Cethiel: 6 frames; wangray0110.itch.io: "11 slash effects, all VFX are flipbook textures").
2. **Sprite estirado/rotado (lo que hace vanilla)**: UNA textura crescente de 4 frames dibujada 7 veces apilada con colores/rotaciones/escalas distintas. Barata, sin geometría custom, look "Terraria".
3. **Mallado a lo largo de una curva + shader (la escuela VFX de Unreal/Unity)**: mesh de arco con UV continua, disolución por umbral, ruido desplazable. (Limeslushie: "Created a mesh along a curve... shader that dissolves the texture, in a similar way that photoshop threshold does"; Unity forum: "texture of slash scrolled through flat disc mesh and alpha erosion (or other mask) is used to fade the slash").
4. **Trail de segmentos (ribbon de posiciones)**: grabar punta+y base del arma cada frame y tender quads entre pares consecutivos. (GameCreators forum: "This code records the positions of two points on the sword. I make one recording at frame X and one at frame X + 1. This give me four points"; GameMaker forum: "make a ds_list" de base/tip). Es EXACTAMENTE el ribbon de StormLib v6.39 de la casa (vértices compartidos + UV continua + normal media).

### 3.2 RECOMENDACIÓN para AethonMod (y por qué)
**Arco procedural (geometría de anillo parcial) + textura de banda perfil, en lote aditivo** — reúsa TODO lo aprendido en v6.39:
- **El arco como geometría**: N quads (N=14-24) sobre la circunferencia de radio `Rad` y apertura `Apertura` (π/2..π). Cada quad: centro `C + Dir(ang)·Rad`, largo de arco `Rad·Δang`, grosor variable `G(u)` con perfil lente: `G(u) = Gmax·(0.25 + 0.75·sin(π·u))` — fino en puntas, gordo en centro (la forma crescente, SIN textura que la hornee).
- **UV continua a lo largo** (`u = 0..1` del inicio a fin del arco): permite el **revelado left→right** recortando el rango de dibujo: dibujar solo el sub-arco `u ∈ [p·(-0.15) .. p]` (cabeza revelándose, cola retrasándose — el "cuts in from two sides" de Dewitte, implementado por geometría en vez de shader).
- **Semántica del lote aditivo (lección v6.39)**: en aditivo el alfa NO existe; el perfil va en RGB premultiplicado. Textura de banda uniforme a lo ancho (grosor por geometría, no por textura), 2-3 px de fundido antialias en los bordes radiales.
- **Núcleo + halo = 2 capas**: capa A (halo): mismo arco con grosor `G·2.2`, tinte de color, factor 0.35; capa B (núcleo): grosor `G·0.45`, BLANCO, factor 1.0. Ambas aditivas. El "pop" = el núcleo multiplica su factor por la campana del §2 (entra fuerte al 60%).
- **Las "líneas múltiples del anime"**: 3 pasadas del núcleo con offsets angulares **+0.01, −0.05, −0.1 rad** y escalas 1.0/0.8/0.6 (los números LITERALES del DrawProj_Excalibur vanilla — Re-Logic pintó las líneas múltiples del anime en 2014-2022 con esto).
- **Chispas del borde**: 4-8 chispas en el borde exterior a ángulos deterministas `ang = a0 + i·(π/2)/N + Hash01(semilla+i)·ruido`, brillo con campana — la casa ya tiene SparkBurst/GlowOrb.
- **El flicker** (si se pide): FlickTick 15 Hz + IsLit del v6.39, aplicado SOLO al halo, nunca al núcleo.

### 3.3 El crecimiento left→right — las 3 formas vistas
1. **Recorte de UV/árido**: `sourceRect.Width = (int)(tex.Width·p)` con `origin` anclado al extremo de inicio — el sprite se revela. (Es como se hace el "reveal" clásico; el equivalente shader: `clip(uv.x - p)`.)
2. **Ángulo creciente** (vanilla): la punta del arco avanza `ang = a0 + Apertura·SmoothStep(p)`; el cuerpo del arco queda detrás. Con SmoothStep el final se frena (la easing del ExampleCustomSwingSword usa `MathHelper.SmoothStep` exactamente para eso: "slows down at the end", la queja de RenzoF en el hilo de Dewitte resuelta con easing).
3. **Escala + rotación del mesh** (Dewitte): "mesh rotation rate over time, mesh scales up over time, center of rotation and scale at the center of the circle". Para tajos concéntricos múltiples (Judgement Cut) es la mejor: cada tajo con su propio radio y delay.

### 3.4 Números de easing del swing (ExampleMod oficial, ExampleCustomSwingProjectile.cs descargado)
```csharp
SWINGRANGE     = 1.67f * Math.PI;  // 300° de barrido
FIRSTHALFSWING = 0.45f;            // 45% antes del ángulo objetivo
SPINRANGE      = 3.5f  * Math.PI;  // 630° (ataque giro completo)
WINDUP         = 0.15f;            // retroceso del 15% del rango
UNWIND         = 0.4f;             // empieza a desaparecer al 40%
prepTime = execTime = hideTime = 12 ticks  // 36 ticks = 0.6 s de swing completo
// Prepare: Progress = WINDUP*SWINGRANGE*(1 - t/prepTime); Size = SmoothStep(0,1,t/prepTime)
// Execute: Progress = SmoothStep(0, SWINGRANGE, 0.6f * t/execTime)
// Unwind : Progress = SmoothStep(0, SWINGRANGE, 0.6f + 0.4f*t/hideTime); Size = 1-SmoothStep(0,1,t/hideTime)
```

---

## 4. CÓMO LO HACE TERRARIA VANILLA (el sistema completo, verificado en 3 fuentes)

### 4.1 La familia aiStyle 190 ("Nights Edge") — LOS tajos de 1.4.4
1.4.4 rehízo las espadas top con proyectiles de tipo "slash arc" (issue tML #3405 "Allow modded swords to have the cool swing projectiles", cerrado; PR #3620 añadió el clon al ExampleMod). Miembros: **982 Excalibur, 983 True Excalibur, 984 Night's Edge, 972 Terra Blade (cuerpo), 997 Horseman's Blade**. Verificado contra el decompile local (Projectile.cs `AI_190_NightsEdge()` línea 34097, Main.cs `DrawProj_Excalibur` línea 25779) y contra `excal.cs` del ExampleMod:

**AI (cada tick):**
```csharp
localAI[0]++;                                  // reloj
float p  = localAI[0] / ai[1];                 // ai[1] = itemAnimationMax del arma
rotation = MathHelper.Pi * ai[0] * p + velocity.ToRotation() + ai[0] * MathHelper.Pi + player.fullRotation;
Center   = player.RotatedRelativePoint(player.MountedCenter) - velocity;   // anclado al jugador
scale    = 1f + p * 0.6f;                      // crece 60% durante el swing
// polvo DENTRO del arco: ángulo = rotation ± rand·(π/2)·0.7, radio = 20..84·scale, velocidad tangencial
if (localAI[0] >= ai[1]) Kill();
```
**Spawn (desde el Item.Shoot, excalitem.cs):** `NewProjectile(src, player.MountedCenter, new Vector2(player.direction, 0), tipo, dmg, kb, whoAmI, ai0: player.direction * player.gravDir, ai1: player.itemAnimationMax, ai2: escalaDelItem)` — la `velocity` (1,0) solo define la rotación base.

**Colisión = 2 CONOS (Colliding override):**
```csharp
float coneLength = 94f * Projectile.scale;        // radio del tajo
float maximumAngle = MathHelper.PiOver4;          // 45° de abertura
float collisionRotation = MathHelper.Pi * 2f / 25f * ai[0];
if (targetHitbox.IntersectsConeSlowMoreAccurate(Center, coneLength, rotation + collisionRotation, maximumAngle)) return true;
// segundo cono para la PARTE TRASERA del arco, que se encoge al avanzar:
float back = Utils.Remap(localAI[0], ai[1]*0.3f, ai[1]*0.5f, 1f, 0f);
if (back > 0f && targetHitbox.IntersectsConeSlowMoreAccurate(Center, coneLength,
    (rotation + collisionRotation) - MathHelper.PiOver4 * ai[0] * back, maximumAngle)) return true;
```
(El segundo cono existe porque el arco "cubre" más que un cono de 45°: es LA técnica para colisionar un arco sin geometría custom.)

**Flags clave (SetDefaults):** `ownerHitCheck = true` + `ownerHitCheckDistance = 300f` (línea de visión 18.75 tiles), `usesLocalNPCImmunity = true`, `localNPCHitCooldown = -1`, `penetrate = 3`, `stopsDealingDamageAfterPenetrateHits = true` (**el proyectil SIGUE VIVO para el visual después de penetrar** — patrón directamente aplicable al bastón: el tajo visual dura aunque ya no dañe), `tileCollide = false`, `noEnchantmentVisuals = true` + `EmitEnchantmentVisualsAt` manual en el arco.

**Draw (DrawProj_Excalibur, 7 capas):** ver §3.2 — la pila exacta: (1) dorado oscuro `Color(180,160,60)` rotado `rotation + ai[0]·(π/4)·−1·(1−p)` (la capa trasera retrasada), (2) capa tenue afectada por luz, (3) amarillo `Color(255,255,80)·0.3`, (4) dorado claro `Color(255,240,150)·0.5` escala 0.975, (5-7) **3 líneas blancas** con `val.Frame(1,4,0,3)` (el frame 4 de la textura = línea fina), rotaciones `+0.01/−0.05/−0.1`, escalas `1/0.8/0.6`, blancos `0.6/0.5/0.4`. Encima: 8 destellos `DrawPrettyStarSparkle` alrededor de la circunferencia (ángulos `rotation + ai[0]·i·(−2π)·0.025 + Remap(p,0,1,0,π/4)·ai[0]`) + 1 destello grande al frente (escala `Vector2(2, Remap(p,0,1,4,1))`).

**Adaptación a la iluminación (detalle de la casa que valorará):** `lightingColor = Remap(sqrt(|colorLuz|)/√3, 0.2, 1, 0, 1)` modula todas las capas; la capa "faint" `color4.A *= (1-lightingColor)` — vanilla compensa que el aditivo brille en oscuridad total.

### 4.2 Los otros slashes vanilla
- **SuperStarSlash (Zenith, AI_152, Projectile.cs línea 18523)**: opacidad `Remap(t,0,10,0,1)·Remap(t,30,60,1,0)` (misma estructura de campana), velocidad girada por `ai[0]/(10·MaxUpdates)` por tick, sprites recortados con `frameCounter` cada 2 ticks, polvo 172/40.
- **Los "melee beam" clásicos** (Terra Blade beam, True Night's Edge): son sprites-espada que vuelan, NO arcos — el "arc look" vanilla pertenece a la familia 190.
- **El swing de las espadas normales** no es un proyectil: el item rota en la mano (PlayerDrawLayers, itemAnimation), y ahí el modder no pinta nada extra salvo polvo. Para un tajo custom el camino oficial tML ES el proyectil (forum: "Your only option is to make it a held projectile with a custom swing animation").

---

## 5. CÓMO LO HACEN OTROS MODS (y juegos)

- **ExampleMod (tML oficial)**: `ExampleCustomSwingSword/Projectile` (PR #3403, swing custom con held-projectile, easing SmoothStep, WINDUP/UNWIND — §3.4) y `ExampleSwingingEnergySword/Projectile` (PR #3620, clon del Excalibur — §4.1). FUENTES DESCARGADAS: `busquedas/excal.cs`, `excalitem.cs`, `exampleswing.cs`, `exampleswingproj.cs`.
- **The Stars Above** (el mod anime por excelencia; wiki leída: starsabovemod.wiki.gg/wiki/Bury_The_Light): **Bury The Light** — espada Yamato/Vergil con **stacks de Judgement** (20 stacks → ráfaga de tajos), "50% of the damage is dealt as an additional slash at your cursor", **Mirage Blades** invocadas al cursor que curan, y **Judgement Finale**: dash + ráfaga de tajos + "resolves after a short delay, instantly defeating any non-boss foe struck" (el Delayed Causality como mecánica letal). Mezcla de "Rapid Slash" + "Judgement Cut End" de Vergil.
- **Katana ZERO Weapons** (Steam Workshop, 29/07/2025): añade las 7 katanas de Katana Zero con "unique slashing animations" y deflección — demuestra que el estilo Katana Zero (tajos limpios + trail azul breve) encaja en tML.
- **Calamity**: las espadas top disparan proyectiles (135 espadas, wiki.gg); los "slash" de Calamity son en su mayoría proyectiles-sprite + polvo denso, no arcos procedurales.
- **Fargo's Souls / Soul of Eternity**: efectos melee masivos (explosiones, shards 50 dmg, embers que sanan) — la filosofía "stacking + burst" que usa Stars Above.
- **Devil May Cry (la referencia estética nº1 del usuario)**: Judgement Cut = distorsión espacial → tajos alrededor del objetivo → ripple azul final (wiki Fandom leída); en DMC3 boss: "dozens of Judgement Cut distortions sweeping across the screen". Blade Mode (MGR): los cortes se hacen en slow-mo con líneas de plane (PlatinumGames blog: animador Hirokazu Takeuchi explica cómo se corta a los enemigos y reaccionan).
- **Katana ZERO**: "Whenever Dragon attacks an enemy with his blade, a blue trail is briefly seen once slashed" (Sword Lines, TVTropes) — el trail POST-slash, exactamente el retardo que pide el usuario.
- **Dynasty Warriors 9**: "colorless sword lines... visualized by the air becoming distorted following the swing" (variante minimalista); DW clásico: líneas de color = hitbox visible.
- **FFXIV Dark Knight** (Kaisteile, realtimevfx): "they swipe and leave a small trail behind after the swipe" — otro caso de trail retardado.
- **Fruit Ninja / Wind Waker / God of War**: las Sword Lines como identidad visual (la wiki Sword Lines lista ~40 juegos: 3000th Duel "purple crescent lines", AWAKEN "yellow crescent lines", Bladed Fury "solid blue curves", Sephiroth "LINES STILL APPEAR" donde ni movió la espada).

---

## 6. IDEAS DE DISEÑO PARA EL BASTÓN (recopiladas; el diseño final es del orquestador)

1. **"El Corte Retrasado" (core, la petición literal del usuario)**: el uso marca un tajo invisible en un punto/área; tras R=8-12 ticks los tajos (arcos blancos) APARECEN en todas direcciones (3-8 arcos a ángulos desacoplados `Hash01`, radios 60-140 px), crecen left→right y se disuelven. Daño aplicado en el tick de aparición (p=0 del tajo), no en el marcaje — Delayed Causality puro. (TVTropes + DMC Judgement Cut.)
2. **Apilado → Ráfaga (Judgement)**: cada impacto en enemigos dentro de los tajos aplica "Judiciio" (stack invisible); a 20 stacks, la siguiente aplicación detona una **ráfaga de tajos encadenados** alrededor del enemigo (Stars Above: "burst in a barrage of slashes while removing all stacks"). Escala de sonido por stack.
3. **"Click" del enfundado (Single-Stroke Battle)**: mantener el bastón "enfundado" cargando (iaijutsu); al soltar, el corte es una línea invisible; el CHASQUIDO audible (SoundID de vaina) al final de la carga es el detonador de todos los tajos marcados durante la carga. Cargar más = más tajos marcados. (TVTropes Single-Stroke: el click ES la señal de que el perdedor se desarma.)
4. **Tajos persistentes-lázaro**: los arcos no dañan al aparecer sino que QUEDAN como heridas luminosas 2-4 s y cortan a los enemigos que los CRUZAN (colisión de línea del arco, `Colliding` con `IntersectsCone...` o segmentos); al expirar se cierran con un micro-tajo de cierre. (Idea recopilada del cruce "trail de segmentos" + hazard persistente.)
5. **Corte diagonal retardado (Diagonal Cut mecánico)**: los enemigos grandes golpeados quedan "marcados" con una línea diagonal tenue; tras R ticks se parten visualmente (el NPC recibe el daño en ese momento + partículas de bisección). Para no-boss: ejecución instantánea (Stars Above Judgement Finale).
6. **Dimensiones de corte (Slash Dimension)**: cada tajo deja una LÍNEA en el suelo/aire que delata el plano del corte (DMC4: "tracing a visible line on the floor"); al segundo uso, el bastón corta TODAS las líneas trazadas simultáneamente — el jugador "dibuja" el campo con cortes diferidos.
7. **Abanico/espiral de carga**: cargar 1s = abanico de 3 tajos (30° entre sí); 2s = 6 tajos en espiral (radios crecientes estilo Dewitte: "mesh scales up over time"); 3s = anillo completo 12 tajos radiando. El "crecimiento" de cada arco arranca escalonado (stagger 3 ticks por arco → efecto "olas de tajos").
8. **Réplica del bastón (Mirage Blades)**: los tajos pueden invocar mini-bastones fantasma en el cursor que repiten el último corte (Stars Above: "summoning Mirage Blades at your cursor which heal 5 HP upon striking foes") — versión defensa/robo de vida para el kit mágico.
9. **(Riesgo a evitar, documentado)**: el retardo NUNCA debe aplicar al daño de forma que se sienta injusto en combate rápido — The Stars Above hace el Finale "resolve after a short delay" pero con invulnerabilidad durante el burst; DMC da el cue audible. **Regla práctica: retardo del VISUAL ≤ 12 ticks (0.2 s), o daño instantáneo + visual retardado.**

---

## 7. FUENTES (por sección)

**§1-2 Anatomía y tropos:**
- https://tvtropes.org/pmwiki/pmwiki.php/Main/SwordLines (definición + 40 ejemplos de juegos; "purple/yellow crescent lines")
- https://tvtropes.org/pmwiki/pmwiki.php/Main/DelayedCausality ("You're already dead"; la definición del retardo)
- https://tvtropes.org/pmwiki/pmwiki.php/Main/DiagonalCut (el corte limpio que se desliza después)
- https://tvtropes.org/pmwiki/pmwiki.php/Main/SingleStrokeBattle (el click del enfundado; iaijutsu)
- https://www.reddit.com/r/SWORDS/comments/18lfssa/ (iaijutsu como origen del one-shot slice)
- https://sakuga.fandom.com + https://wavemotioncannon.com/2016/12/31/an-introduction-to-framerate-modulation/ (animar "on twos/threes", 24 fps)
- https://opengameart.org/content/weapon-slash-effect (pack CC0 Cethiel: 6 frames, 5 efectos, 4 colores; ZIP descargado y medido con numpy — datos en busquedas/opengameart_classic.zip)

**§3 Técnicas 2D:**
- https://realtimevfx.com/t/niels-dewitte-sketch-14-wip/5152 (hilo leído COMPLETO vía .json de Discourse: UV unwrap, scroll noise × mask, reveal de dos lados, escala+rotación del mesh, bloom)
- https://realtimevfx.com/c/38 (categoría "14 - Sword Trail" del VFX Sketch Challenge) + https://realtimevfx.com/t/limeslushie-sketch-42-weapon-swipe/16890 (mesh along a curve + threshold dissolve; timing por frames) + https://realtimevfx.com/t/kaisteile-sketch-42-weapon-swipe/16922 (FFXIV DRK trail retardado)
- https://realtimevfx.com/t/sword-slash-texture/31720 (hilo "sword slash texture")
- https://discussions.unity.com/t/how-do-i-make-vfx-for-attacks-in-anime-style/879490 ("flat disc mesh + alpha erosion"; breakdowns)
- https://forum.gdevelop.io/t/sword-and-other-melee-weapon-swinging/75120 (centro de rotación en la empuñadura; "attack effect as collider")
- https://forumfiles.thegamecreators.com + https://forum.gamemaker.io (trail por pares de puntos base/tip → 4 puntos → quad)
- https://devforum.roblox.com (hilo "Crecent Sword Slash Effect": dibujar líneas con grosor/opacidad/color variables)

**§4 Vanilla Terraria:**
- Decompile local de la casa: research/v633/src/vanilla/Projectile.cs (AI_190_NightsEdge L34097, AI_152_SuperStarSlash L18523) y Main.cs (DrawProj_Excalibur L25779, DrawPrettyStarSparkle)
- https://github.com/tModLoader/tModLoader/pull/3620 (ExampleSwingingEnergySword = clon Excalibur; código descargado a busquedas/excal.cs + excalitem.cs)
- https://github.com/tModLoader/tModLoader/pull/3403 (ExampleCustomSwingSword; descargado exampleswing.cs/exampleswingproj.cs)
- https://github.com/tModLoader/tModLoader/issues/3405 (petición "cool swing projectiles" para mods — cerrada con la exposición 1.4.4)
- https://terraria.wiki.gg/wiki/Swords + https://docs.tmodloader.net (docs Projectile / ItemUseStyleID)

**§5 Otros mods/juegos:**
- https://starsabovemod.wiki.gg/wiki/Bury_The_Light (Judgement stacks, Mirage Blades, Judgement Finale — leída completa)
- https://devilmaycry.fandom.com/wiki/Judgement_Cut (leída: distorsión, ripple, "slashes fill the screen")
- https://www.platinumgames.com (blog MGR Blade Mode, animador Takeuchi) + https://metalgear.fandom.com/wiki/Blade_Mode
- https://steamcommunity.com (Workshop: Katana ZERO Weapons)
- https://calamitymod.wiki.gg + https://fargosmods.wiki.gg (filosofía de stack/burst)
- https://forum.gamemaker.io (hilo Katana Zero 2020 — el dash de ataque)

**Nota de método:** el buscador devuelve solo el host para algunos resultados; los hilos de realtimevfx/gdevelop se leyeron por el endpoint `.json` de Discourse (contenido íntegro de los posts). Los 46 JSON de búsqueda crudos + 14 páginas + 4 fuentes .cs + el zip medido quedan en `research/tajos_v640/busquedas/`.
