# INFORME DE INVESTIGACIÓN — BOSS "SUPERNOVA FRAGMENT" (Lunar Veil)
**Task ID: 2-b · Mod destino: AethonMod (tModLoader 1.4.4) · Propósito: replicarlo como PROYECTIL/MINION de prueba**
Fuente primaria: clon local completo de Lunar Veil v1.6.1.1 en `/tmp/Stella` (7876 archivos). Todo lo que sigue fue extraído leyendo el C# y analizando los PNG píxel a píxel. Los PNG copiados a `ref/` son SOLO referencia visual (copyright Lunar Veil) — AethonMod debe generar sus propios assets.

---

## A) QUÉ ES

**El mod**: *Lunar Veil* (namespace interno `Stellamod`), autor **Zenovia**, versión **1.6.1.1**, `modReferences = ParticleLibrary`. Según su `description.txt` está hecho "pretty much by three people: Me, Azzalea and Scolon" (más Azza en música). Es un crossover del juego personal del autor, *Diari*, ambientado en el mundo de Aimacra: la protagonista Sirestias "quiere eliminar a todos los falsos dioses del planeta" — dioses que poseen singularidades. El clon local es el repo de backup chino *Veluriyam-Attic/Stellamodbackpack* (commit `b6a7c67` "1.6.1.1备份", 17-sep-2024).

**El boss en la progresión**: Supernova Fragment es uno de los **4 bosses post-Plantera → post-Moon Lord** añadidos en la *Green Sun Update 1.6* (junto a Gothivia, Rek y Niivi). Se invoca con el consumible **Voidal Passageway** (rareza Lime, tooltip: "Use this to open a doorway, calling down Supernova Fragment!") que dispara un `MagicDoor` que spawnea el NPC.

**Números del NPC** (`SetDefaults`): **61000 HP** base (escala con `balance` en experto vía `ApplyDifficultyAndPlayerScaling`), **52 defensa**, hitbox 100×60, escala 0.9, `boss=true`, `noGravity`, `noTileCollide`, `npcSlots=10`, knockbackResist 0, valor 60g. Barra de boss propia (`SUPBossBar`, compartida con Verlia). Música exclusiva `Assets/Music/SupernovaFragment.ogg` (2.0 MB). Sonidos: hit `VoidHit`, muerte `VoidDead1` (ambos con PitchVariance 0.1).

**Bestiario** (hjson + `SetBestiary`): bioma SolarPillar, flavor text: **"A powerful gift from Lumi V. to her trusted sisters."** — un regalo/artefacto de la diosa Lumi, no una criatura viva. Al golpearte aplica el debuff `SFBuff` 200 ticks (ceguera + blackout + sangrado + drenaje nocturno).

**Loot**: `Gambit` (5-13), `Superfragment` (20-45), `TempleKeyPart`, boss bag `SupernovaBag`, relic de maestro `SupernovaBossRel`, armas `Iknoctstein`/`Dulahaun` (50%, no-experto).

---

## B) EL LOOK EXACTO

### B.1 El sprite del boss
- **`SupernovaFragment.png`: 128×1680 = 30 marcos de 128×56 px.** La textura de un solo frame es un **"ojo alado" horizontal** (crescente estelar con alas):
  - Colores medidos en los píxeles (reparto del frame 0, se mantiene en los 30):
    - **Crema-oro (240,216,144) ≈ 30%** — el cuerpo central y los arcos superior/inferior.
    - **Negro (0,0,0) ≈ 28.6%** — el núcleo central oscuro (un "pupila" horizontal).
    - **Naranja (240,120,72) ≈ 20%** — las alas que se extienden a izquierda y derecha.
    - **Azul marino (0,0,120) ≈ 13.3%** — acentos junto al núcleo (y media 25 = centro del frame).
    - **Rojo oscuro (168,48,48) ≈ 8.3%** — las puntas extremas de las alas.
  - Silueta (mapa ASCII del frame): dos arcos cóncavos arriba/abajo unidos por un núcleo negro, con dos alas anchas que se abren en diagonal a los lados (como un ángel de fuego de 128 px de envergadura × 56 de alto). Onda de brillo: núcleo brillante (G) rodeado de K con borde G/O.
- **Animación**: `Main.npcFrameCount = 30`; `FindFrame`: `frameCounter += 0.5`/tick, avanza marco cada 3 → **6 ticks/marco, ciclo completo 180 ticks (3 s)**. 23 de los 30 marcos son únicos: la silueta es constante y solo **flickerea el interior** (parpadeo de llamas/detalles, el contorno de píxeles opacos es idéntico en todos).
- **`SupernovaFragment_Glow.png`** (128×1680, mismas 30 frames): versión brillo — crema 51%, naranja 34%, rojo 16%, sin negro.
- **`SupernovaFragment_Head_Boss.png`** 60×28 (icono de mapa, [AutoloadBossHead]). **`SupernovaFragmentBestiary.png`** 276×190 (retrato del bestiario: blanco-negro-naranja-rojo-azul con núcleo blanco).

### B.2 El render (PreDraw + PostDraw) — el "look" real en juego
1. **Luz**: `Lighting.AddLight(Center, Color.Orange.ToVector3() × 1.25 × Main.essScale)` — naranja fuerte permanente.
2. **Rotación**: `NPC.rotation = NPC.velocity.X × 0.03` — se inclina (banking) según velocidad horizontal.
3. **Afterimages orbitales — LA firma visual del boss** (en `PreDraw`, antes del sprite normal):
   - Anillo 1: **4 clones** (`for i=0; i<1; i+=0.25`), color **(255,233,197,50)** (crema pálido, alfa 50), cada clone en `drawPos + new Vector2(0,8).RotatedBy(radians) × time` — orbitan en círculo de radio 8 px alrededor del centro.
   - Anillo 2: **3 clones** (`i+=0.34`), color **(244,142,72,77)** (naranja, alfa 77), radio 16 px.
   - `radians = (i + timer) × TwoPi` con `timer = GlobalTimeWrappedHourly/2 + time×0.04`; `time` es un pulso triangular (ciclo 4 s: `t%=4; t/=2; si ≥1 → 2-t; t = t×0.5+0.5` → oscila 0.5..1). Efecto: los clones **orbitan y respiran** alrededor del boss.
4. **Glow pulsante** (`PostDraw` con `_Glow`): tintado **`Color.Goldenrod × (cos(GlobalTime%1.4/1.4×2π)/2+0.5) × 0.8`** — pulso 0..1 con periodo **1.4 s**. Encima, un **eco** del glow desplazado `(4×pulse+2)` px en órbita, tintado `(127,127,127,0)×Goldenrod×(1-pulse)`.
5. **Spawn-in**: el boss aparece con `scale=0` y crece `+0.010/tick` (100 ticks = 1.7 s) mientras la cámara hace zoom al punto (`FocusOn(Center, 8f)`), suena `SingularityFragment_TPIn`, y al completarse: **filtro de pantalla "Shockwave"** (rippleCount 20, rippleSize 5, rippleSpeed 15, distortStrength 300 — distorsión de onda expansiva), **114 polvos DustID.Torch** en ráfagas (14 grandes gravedad-0 escala 4, 40 escala 1, 40 escala 6 no-gravedad, 20 escala 2), sonido `SunStalker_Bomb_Explode`, temblor de cámara y contacto 999.
6. **El backdrop gigante — `IncresianDisc`** (NPC hermano invisible a daño, 500000 HP, sigue el Center del boss): hoja **1600×2000 = 8×5 celdas de 200×400** (40 marcos, anim `trueFrame += 0.3`, loop 1..40 ≈ 2.2 s), dibujado **rotado `MathHelper.PiOver2`** con escala ×0.8 → un **disco/élipse oscura de ~320×160 px** (≈3× el boss) siempre detrás: 72% negro, rojos oscuros y chispas naranjas (24-96,0,0 + 240,72,0). Mantiene el filtro Shockwave de pantalla SIEMPRE activo (alternando estados Wait/Speed de 60 ticks). Es el "halo de distorsión" que rodea al boss durante todo el combate.
7. **Teletransporte (case 15 / desaparición)**: encoge `scale -= 0.015/tick` (~67 ticks) con `SingularityFragment_TPOut`, reaparece **EN el centro del jugador** con `SingularityFragment_TPIn` creciendo al mismo ritmo.

### B.3 Las texturas de los ataques
| Textura | Tamaño | Uso real |
|---|---|---|
| `SupernovaOrb.png` / `_Glow` | 30×56 / 30×56 | Orb vertical tipo "ojo/llave" (crema+naranja+negro+azul) — orbita al boss en PH2 |
| `NovaBomb.png` | 28×32 | Bomba negra con banda dorada y núcleo azul — mina giratoria |
| `NovaBlast.png` | 18×168 = **4 marcos de 18×42** | Dardo/blast naranja (54% naranja, 21% crema, 19% rojo) |
| `NovaFlame.png` | 42×280 = **5 marcos de 42×56** | Bola de fuego naranja (61% naranja, 28% crema, 11% rojo) |
| `SupernovaBeam(Final).png` | 30×190 | **Casi decorativo** (rojo 240,0,48): el láser REAL se dibuja 100% con primitivas |
| `Supernova*(God|)Explosion.png` | 18×34 | **Placeholder**: las explosiones se dibujan 100% con primitivas de fuego |
| `SupernovaZapwarn(Final).png` | **2×2** | ¡Invisible! El telegraph se dibuja con la máscara `Effects/Masks/Extra_47.png` (30×1028, línea vertical) |
| `Effects/Masks/DimLight.png` | 64×64 | Blob de glow radial reutilizado por NovaBomb y NovaFlame (tinte (85,45,15)) |

---

## C) EL CEREBRO — máquina de estados

**Campos**: `NPC.ai[2]` = macro-fase (0 init, 1 bucle de ataques, 5 despawn) · `NPC.ai[1]` = nº de ataque · `NPC.ai[0]` = timer del ataque. Sincronización MP extra: `Attack`, `LazerType`, `_invincible`. Flags: `PH2` (vida < 60%), `PH2TP` (cutscene ya vista), `SingularityPhaze` (0 normal, 1 fase de orbes, 2 post).

**Temperamento general**: boss **RELENTLESS/SPAM** — entre ataque y ataque solo espera ~3 ticks y elige otro al azar (PH1: 1-3, PH2: 1-5, `Main.rand.Next(1,4/6)`); además **te chupa** constantemente: todos los jugadores a ≤4000 px reciben `-0.1f` de velocidad hacia el boss cada tick (singularidad). **El contacto mata** (9999 de daño mientras ataca, `ai[1] < 5`).

**Fase 1 (>60% vida)**: bucle aleatorio de ataques 1-3 con hover suavísimo (`CasuallyApproachChild`: `velocity.Y ×= 0.94`, `Lerp(velocity, MovemontVelocity(...hacia el 4% del jugador..., 0.2×distancia), 0.009)` — se desliza lento y pesado).

**Fase 2 (<60%)**: al cruzar el umbral → **cutcase 15**: se encoge hasta 0, se teletransporta AL JUGADOR, y despliega **7 SupernovaOrbs en círculo de radio 900** (ángulos `i×2π/7`, rotación lenta `ai[1] += 0.005 rad/tick`). El boss se vuelve **invulnerable** (`_invincible` → `dontTakeDamage`) y queda "encadenado" (buff `SupernovaChained` → proyectiles de cadena + anillo de radio 900 que **empuja/teletransporta** a los jugadores que se alejan). Durante SingularityPhaze 1 los ataques 4/5 se convierten en **paredes de láser verticales**. Cuando matas los 7 orbes (cada uno resta `SingularityOrbs`): vida se resetea a `lifeMax/2`, explosión + 114 polvos, vuelve a la fase 2 normal (Phaze 2) con ataques 1-5.

**Ciclo de ataques (nombres y timing exactos)**:

| # | Ataque | Duración | Detalle |
|---|---|---|---|
| 0 | Idle/hover + sorteo | ~3 ticks | Drift + random del siguiente |
| 1 | **Volea de NovaBlast** | 250 ticks | t=50/150: **5 blasts** en abanico (±1 rad, paso 0.5) vel 6; t=100/200: **3 blasts**; sonido `SingularityFragment_Shot` + shake. Dmg 50/68 |
| 2 | **Bolas NovaFlame** | 150 ticks | t=70/100/130: 1 fireball hacia el jugador vel ~12.75 (8.5×1.5) + jitter ±1; sonido Betsy fireball |
| 3 | **Abanico de NovaBomb** | 100 ticks | t=70: **8 bombas** en abanico exótico espejado (`sin/cos(angle)×9` y su negativo, con ángulos cuadráticos + offset aleatorio), CopperCoin dust, `SunStalker_Sun_Shot1/2`, shake 2048/124. Dmg 45 |
| 4 | **Pared de láseres / Cruz final** | 250-300 ticks | Phaze 1: **7 columnas Zapwarn** en X = boss−1050…+1050 (paso 350, +175 offset), t=70..130 una cada 10 ticks → cada una avisa y suelta un `SupernovaBeam` desde 900 px ARRIBA que **cae a 10 px/tick** (2400 de largo, 130 de ancho, dmg 500). Normal: t=50/150/250 → **4 SupernovaZapwarnFinal** en ángulos `Ofset, ±45°, 90°` (offset aleatorio 1-100): retícula que **gira desde 10.4 rad frenando** (Lerp 0.06) y a t=75 dispara `SupernovaFinalExplosion` (blanca, dmg 800) + `SupernovaBeamFinal` (7400 de largo, dmg 500) desde 1200 px atrás del ángulo |
| 5 | **God Explosion** | 200 ticks | t=5 sonido `SingularityFragment_Charge`; t=150: `SupernovaGodExplosion` — **explosión de fuego de radio 500, dmg 800** + shake 2048/424. En Phaze 1 = misma pared de 7 columnas que el 4 |
| 15 | **TP cutscene PH2** | ~130 ticks | Encoge→0, aparece en el jugador, despliega los 7 orbes |
| 6 | (vacío, sin implementar) | — | — |

**Despawn**: jugador muerto o >3000 px → `Disappear()`: sonido TPOut, cae con `velocity.Y += 0.1`, `scale -= 0.01` hasta 0 y se desactiva.

---

## D) LOS ATAQUES AL DETALLE (biblioteca de movimientos)

| Proyectil/NPC | Visual | Comportamiento | Impacto |
|---|---|---|---|
| **SupernovaBeam** (ModProj) | **Primitiva** `PrimitiveTrailCopy` + shader `GenericLaserVertexShader` con textura `STARTRAIL2`, color base `PaleVioletRed`; anchura = `130×scale×2`; color `Lerp(Goldenrod, OrangeRed, 0.65)×Opacity×pow(lerp(0,0.1,completion),3)` (se afila en la punta); luz `Color.Red×1.4`; 6 polvos CrimsonTorch/verde en el origen por tick | Cae vertical vel (0,10) desde 900 px arriba; largo **2400**, alfa fade-in −25/tick, scale `sin(Time/200×π)×3` cap 1, timeLeft **200**, colisión AABB-vs-línea ancho 65 | Dmg **500**, `CooldownSlot=Bosses`, daña cuando `Time ≥ 8` |
| **SupernovaBeamFinal** (ModProj) | Primitiva + `LaserShader` con `WaterTrail`, `UseColor(OrangeRed)`; blanco → naranja con `Time/120`; anchura `40×scale×2` | Estático (`ShouldUpdatePosition=false`), nace a **1200 px atrás** del ángulo de la retícula (warn.rotation − 90°), largo **7400**, timeLeft **15** (ráfaga de 0.25 s), scale `sin(Time/15×π)×3` | Dmg **500**, daña cuando `Time ≥ 8` |
| **SupernovaZapwarn** (ModNPC) | **Aviso vertical**: máscara `Extra_47` (30×1028) dibujada **3×** con color `(55×alpha, 45×alpha, 15×alpha, 0)` y escala `0.2×(8.3)` — línea dorado-oscuro creciente | NPC 0×0, invulnerable, quieto; alfa crece +0.04/tick hasta 4 (≈2 s de aviso), luego suena trueno + `LightingRain` random y dispara el Beam desde arriba; alfa decae −0.29/tick | Sin daño (el daño lo pone el Beam) |
| **SupernovaZapwarnFinal** (ModNPC) | Misma máscara 3× con tinte más rojo `(75, 35, 15)×alpha`; **rota** desde 10.4 rad frenando (Lerp 0.06) — retícula giratoria que se asienta en el ángulo final | Se ubica en `SingularityPos` (posición estática del boss); a t=75 dispara FinalExplosion + BeamFinal; muere a t=200 | — |
| **SupernovaExplosion** (ModProj) | **Primitiva de fuego**: 4 sectores (ángulos −π/2..π/2 paso π/3, girados −0.2π), cada uno una tira de 16 puntos a lo largo del diámetro; anchura `Radius×sin(π×completion)` (lengüetas); `FireVertexShader` + `WaterTrail`, saturación 0.45; color `Lerp(Goldenrod, OrangeRed, sin(π×c)×0.5+0.3)×Opacity` | `MaxRadius=200`, `Radius Lerp 0.1`, `MaxUpdates=4` (timeLeft 84 → **0.35 s reales**), scale +0.08, opacity 0.55 | Dmg 45 (hereda de la bomba); colisión circular `Radius×0.725` solo mientras `Time ≤ 30` |
| **SupernovaExplosionSmall** | Idéntica a la anterior | `MaxRadius=100` | Dmg 60 (de los orbes) |
| **SupernovaGodExplosion** | Idéntica | `MaxRadius=500` | Dmg **800** |
| **SupernovaFinalExplosion** | Idéntica pero **BLANCA**: `Lerp(White, White, …)` con `FireWhiteVertexShader` `UseColor(White)` | `MaxRadius=200` | Dmg **800** |
| **NovaBomb** (ModProj) | Sprite 28×32 + **trail de primitivas** `PrimDrawer` con shader `VampKnives:BasicTrail`/textura `LightningTrail`, color `Lerp(Goldenrod, Blue, c)×0.7` (¡rastro dorado→azul!); glow `DimLight` 3×+1 tinte (85,45,15); GlowDust OrangeRed cada 3 ticks; luz Orange | Mina: vel ×0.98, `rotation += 0.08/tick` (gira), scale 1.5, timeLeft 180-190 random; **a los 90 ticks se encoge** (−0.22/tick) y estalla en ~97 ticks | Dmg 45; al morir → `SupernovaExplosion` + shake 2048/124; aplica debuff **AbyssalFlame** 200 ticks |
| **NovaBlast** (ModProj) | Sprite 4 marcos 18×42, anim **7 ticks/marco** (ciclo 0.47 s), alfa fade-in −40, scale 1.2, rotación = velocidad+π+π/2 (vuela "de cola"); luz `OrangeRed×1.75×essScale` | Vel inicial 6, **acelera ×1.01/tick**, penetra 10, timeLeft 900 | Dmg 45 (el boss pide 50/68); OnKill: 20 polvos Dirt |
| **NovaFlame** (ModProj) | Sprite 5 marcos 42×56, anim **4 ticks/marco** (ciclo 0.33 s), glow `DimLight` (85,45,15), **emisión masiva de Torch dust** cada tick (velocidades aleatorias 2-4, escala 0.8-1.5) + shake continuo 256/16 | Vel 12.75 hacia el jugador, `tileCollide=true`, penetra 10, timeLeft 500 | Dmg 50/68; OnKill: GlowDust naranja + TSmokeDust rojo + sonido impacto Betsy + shake |
| **SupernovaOrb** (ModNPC) | Sprite 30×56 + **afterimages AZULES aditivas**: 4 clones `(49,39,124,0)` radio 4 + 3 clones `(50,74,255,0)` radio 6 (mismo truco orbital del boss); trail LightningTrail `Lerp(OrangeRed, DeepPink, c)×0.7`; glow OrangeRed pulsante 1.4 s; luz Orange×1.25 | **Orbita al boss**: `Center = parent.Center + 900×ai[1].ToRotationVector2()`, `ai[1] += 0.005 rad/tick` (una vuelta ≈ **21 s**); alfa fade-in; 2000 HP; si el padre muere, se desactiva | Contacto 60; al morir → SmallExplosion + ring de 20 polvos SomethingRed + resta `SingularityOrbs` |
| **IncresianDisc** (ModNPC) | Hoja 8×5 marcos de 200×400 (anim 0.3/frame, loop 1..40), rotado 90°, escala 0.8 — **disco negro-rojo de ~320×160 px de fondo**, alpha `(255,255,255,0)×(1−alpha/80)` | Sigue el Center del boss (busca al NPC con buff StarSuper); invulnerable (500000 HP); alterna Wait/Speed de 60 ticks solo para alternar el **filtro Shockwave** permanente | Sin daño; puramente estético |

**Sonidos** (rutas `Stellamod/Assets/Sounds/`): `VoidHit`, `VoidDead1`, `SingularityFragment_TPIn/TPOut/Shot/Shot1/Charge`, `SupernovaFragment_EndLazer1/2`, `SunStalker_Bomb_Explode`, `SunStalker_Sun_Shot1/2`, `StormDragon_LightingZap`, `Dreadmire__LightingRain1-3`, `SoftSummon2`, más `SoundID.DD2_BetsyFireballShot/DD2_BetsyFireballImpact`. Música: `Assets/Music/SupernovaFragment`.

---

## E) CÓMO LO COPIAMOS (recomendación para AethonMod)

**Contexto propio**: AethonMod YA tiene un `SupernovaProjectile` (v5.85+, el arma "estrella que colapsa y estalla") — para no colisionar, llamar al nuevo **`FragmentoSupernovaProjectile`** (o `EsbirroSupernova`) y ubicarlo en `Content/Projectiles/Cosmic/`. Recordatorio de contrato de la casa: coordenadas de **mundo** en los compositores, `FlushAdditive` con lote cerrado→cerrado (try/finally), **cero `Main.rand` en render** (variación = `Hash01(seed, i, t)` o senos inconmensurables), primitivas `VFXCore.Quad/Ring`, paletas de `VFXPalettes`.

**Qué priorizar del visual (el 80% del look con 3 capas)**:
1. **EL NÚCLEO "ojo alado"**: 4-5 `Quad`s con `SoftGlow` en FlushAdditive: un quad horizontal ancho (≈110×40 px, crema-oro (255,220,150)) como cuerpo, 2 quads naranjas (240,120,72) inclinados ±25-30° como alas (puntas con fade a rojo oscuro (168,48,48)), y un quad negro-azulado pequeño de "pupila" en AlphaBlend (o simplemente restar brillo con un quad oscuro en el pase normal). Respiración: escala ×(0.9..1.1) con `sin(GlobalTime×2π/1.4)` — el MISMO periodo de 1.4 s del glow original.
2. **LAS AFTERIMAGES ORBITALES — LA firma**: trasplante directo del bucle del boss, pero sin textura ajena: 4 quads crema (255,233,197) alfa 50 y 3 quads naranjas (244,142,72) alfa 77 en `pos + Vector2(0, 8|16).RotatedBy((i + GlobalTime/2 + GlobalTime×0.04)×2π) × pulse` con pulse triangular 4 s. Cero random, puro reloj — encaja 1:1 con la convención Hash01/senos.
3. **EL HALO**: `VFXCore.Ring` (RingQuadSize(≈90)) dorado Girasol al 15-25% de alfa + luz `Color.Orange×1.25` — sustituye barato al IncresianDisc (el disco negro de fondo se puede evocar con UN quad oscuro grande en AlphaBlend girando lento si se quiere el aire de "singularidad").

**Cómo se movería (minion)**: hover sobre el hombro del jugador — target = `player.Center + (±56, −64)` (lado según `direction`), con el LERP SUAVE del boss (`Vector2.Lerp(velocity, dir, 0.1)` + amortiguación Y ×0.94, suave y pesado, no un minion nervioso), `rotation = velocity.X×0.03` para el banking.

**Los 2 ataques icónicos** (cíclicos, 3 fases de 90 ticks c/u, elegidas con `Hash01` o rotación fija):
- **Fase A — Volea NovaBlast**: 5 proyectiles hostiles-a-enemigos en abanico ±0.5 rad (paso 0.5) vel 6, acelerando ×1.01 — cada uno: quad naranja estirado (escala X por la velocidad) + rastro de 3-5 quads decrecientes crema.
- **Fase B — Láser barrido**: telegraph = `Ring` contraído + línea fina (análogo del Extra_47: un quad de 4×900 px alfa creciente 0→0.6 durante 45 ticks, tinte (255,180,60)), luego el beam: **quad estirado** de núcleo blanco (2 quads: núcleo 60% + halo naranja 160%, `Lerp(Goldenrod, OrangeRed)`) a lo largo de 900-2400 px con daño tipo línea (distancia punto-segmento, igual que el `Collision.CheckAABBvLineCollision` original).
- **Fase C (opcional, la cereza)**: mini "flor de fuego" al matar enemigos: 4 tiras de 8-16 quads cruzadas con anchura `R×sin(π×c)` y color Goldenrod→OrangeRed — es exactamente la explosión del boss y se construye con el mismo buffer de quads (la primitiva "lengüeta" ya existe de facto en los compositores de soles de la casa).

**Daños de prueba**: blast ≈ 40% del minion, beam ≈ 300% con cooldown 0.4 s (`CooldownSlot` propio o iFrame corto) — mantener la proporción del boss (blast 45-68 / beam 500) pero sin el 9999 de contacto (eso es de boss, no de prueba).

---

## F) FUENTES (rutas exactas leídas)

Código del boss y sus 13 satélites (todos en `/tmp/Stella/NPCs/Bosses/SupernovaFragment/`):
- `SupernovaFragment.cs` (969 líneas — leído completo)
- `SupernovaBeam.cs`, `SupernovaBeamFinal.cs` (láseres primitivos)
- `SupernovaZapwarn.cs`, `SupernovaZapwarnFinal.cs` (telegraphs)
- `SupernovaExplosion.cs`, `SupernovaExplosionSmall.cs`, `SupernovaGodExplosion.cs`, `SupernovaFinalExplosion.cs` (explosiones)
- `NovaBomb.cs`, `NovaBlast.cs`, `NovaFlame.cs` (proyectiles de atacar)
- `SupernovaOrb.cs` (orbes orbitales PH2), `IncresianDisc.cs` (disco de fondo)

Contexto del mod:
- `/tmp/Stella/description.txt`, `/tmp/Stella/build.txt` (autor Zenovia, v1.6.1.1, equipo, progresión "Post plant - Post ML")
- `/tmp/Stella/Localization/en-US/Mods.Stellamod.NPCs.hjson` (líneas 330-332: DisplayNames)
- `/tmp/Stella/Localization/en-US/Mods.Stellamod.Items.hjson` (línea 7527: VoidalPassageway)
- `/tmp/Stella/Items/Consumables/VoidalPassageway.cs` + `/tmp/Stella/Projectiles/MagicDoor.cs` (invocación)
- `/tmp/Stella/Buffs/SFBuff.cs`, `StarSuper.cs`, `SupernovaChained.cs`
- `/tmp/Stella/Projectiles/Chains/SupernovaChainCircle.cs` (anillo-cadena de la fase de orbes, radio 900)
- `/tmp/Stella/Helpers/DownedBossSystem.cs`, `/tmp/Stella/NPCs/Town/Sirestias.cs` (flag de victoria + diálogo)
- `/tmp/Stella/NPCs/Bosses/Verlia/SUPBossBar.cs` (barra compartida)
- `git log` del clon: commit `b6a7c67` "1.6.1.1备份" (Henceforth, 17-sep-2024)
- Assets verificados: `/tmp/Stella/Assets/Music/SupernovaFragment.ogg`, `/tmp/Stella/Assets/Sounds/` (12 .ogg referenciados), `/tmp/Stella/Effects/Masks/Extra_47.png` (30×1028), `DimLight.png` (64×64)
- Análisis de PNG propio (PIL: dimensiones, paletas dominantes, conteo de frames únicos, mapas ASCII de silueta) sobre los 17 PNG de la carpeta del boss.

**PNGs de referencia copiados** (solo consulta, NO para usar en el mod) en `ref/`: SupernovaFragment.png, SupernovaFragment_Glow.png, SupernovaFragmentBestiary.png, SupernovaFragment_Head_Boss.png, SupernovaOrb.png, SupernovaOrb_Glow.png, NovaBomb.png, NovaBlast.png, NovaFlame.png, SupernovaBeam.png, SupernovaBeamFinal.png, IncresianDisc.png, SupernovaExplosion.png, SupernovaGodExplosion.png, Extra_47.png, DimLight.png.
