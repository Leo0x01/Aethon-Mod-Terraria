# INFORME — EL DESGARRO EN LA REALIDAD (reality tear / spacetime rift)
## Task 42-b · Agente: general-purpose (investigación desgarro de realidad + diseño RiftLib)

**Mandato del usuario:** "un bastón que su proyectil sea un DESGARRO EN LA REALIDAD
(reality tear / spacetime rift) que dañe con eso. Investiga mods populares que hagan
cosas así, profundo."
**Este documento es INVESTIGACIÓN + DISEÑO puro. NO se implementa nada aquí.**

### Método y fuentes

| Tipo | Fuentes |
|---|---|
| **Código local descompilado (lectura línea a línea)** | MEAC (253 archivos ILSpy — el jackpot: `SCCut`, `LineWarp`/`LineWarp2`, `RingWarp`, `Wave_Warp`, `VisualEffect`, `MEAC.cs` EndCapture con `UseWarp`/`UseSCCut`/`glurTimer`), WoTE + Luminance (`RainbowRiftArrow`, `DistortionMetaball`, `PrismaticBurst`, `EventideLances`), Everglow (`RockPortal`+pipeline), Terraria vanilla 1.4.4.9 (`Main.cs` portales 600/PortalHelper, dust 263) |
| **Repos públicos clonados (git clone --depth 1)** | **CalamityModPublic** (18.407 archivos — el jackpot nº2: `DoGTeleportRift`, `DoGRiftCrack`, `DoGRealityCrackShader.fx`, `DoGDistortionMetaball`, `DoGSky`, `DoGVisualsManager`, `BigRipMetaball`, `RealityRupture`/`Stealth`/`Lance`/`Mini`, `CrackParticle`, `TechyHoloysquareParticle`, `DrawingUtils.DrawChromaticAberration`, `ArkOfTheCosmos_BlastAttack`), **ProjectStarlight/StarlightRiver** (sin contenido rift: solo 3 hits de "Rift" en tiles de guía — negativo documentado) |
| **Informes previos del proyecto** | `research/humo_v625/ANALISIS_HUECOS.md` (formato de contratos + metaballs WoTE), `research/humo_v625/INFORME_MODS_HUMO.md`, `research/luz_v622/` (MEAC warps), `research/storm_v621/INFORME.md` (rayos = caminos fractales) |
| **Búsquedas web (18 consultas, JSONs en `busquedas_web_42b/`)** | Stellamod/Wrath of the Gods (Nameless Deity: "Reality Shatter Punches" + fase 30% "reality starts cracking"), Calamity wiki (Reality Rupture, SCal), Fargo's Souls (Eridanus), Genshin Impact (Abyss/Void, cutscene "A Void Gnawed Into Stillness"), Zelda BotW (malice), Hollow Knight (Dream Nail), Doom Eternal, glitch VFX (glitchology.com RGB split, agatedragon.blog displacement lines, godotshaders VHS), Lichtenberg, The Stars Above, Wikszilla (buscado, sin fuentes concretas) |
| **Inventario propio (leído completo)** | VFXCore.cs (272 L), EstelaLib.cs (456 L), OndaLib.cs (349 L) + OndaSystem, PyraLib.cs (514 L), StormLib.cs (628 L), LumenLib.cs (493 L), BrumaFX.cs (BeginMass/BeginGlow v2), BlackHoleLensSystem.cs (816 L), Content/Particles/ParticlePresets (Explosion/**Implosion**/RingPulse/VortexSwirl), Content/Effects/Procedural/ (¡ya existe `Star.png` 16×16, `Slash.png` 64×64, `BlackDisk.png` 256×256, `Noise.png` 128×128!) |

> REGLA DE HONESTIDAD: todo el código ajeno se cita como **pseudocódigo parafraseado
> propio**; ninguna línea se copia. Los números concretos (radios, factores, Hz) sí se
> citan porque son datos, no expresión creativa. Código GPL no existe en las fuentes
> usadas (Calamity es pública sin licencia GPL de código; se cita como documento de
> estudio, no se reutiliza expresión).

---

# PARTE 1 — HALLAZGOS POR FUENTE: CÓMO SE HACE UN "DESGARRO DE REALIDAD"

## 1.1 CALAMITY — el sistema de grietas más completo del ecosistema

Calamity tiene **tres implementaciones independientes** de "desgarro", una por nivel de
escala: el arma (Reality Rupture), el telegraph de jefe (DoGTeleportRift) y la herida en
el cielo (DoGSky + DoGVisualsManager). Es la referencia #1.

### 1.1.1 `DoGTeleportRift` — el desgarro que abre el Devorador de Dioses

**Dónde:** `Projectiles/Boss/DoGTeleportRift.cs` (290 L, leído completo).

El jefe deposita una grieta a **500 px del jugador en su dirección de movimiento** (±48 px
de ruido; ±960 en GFB) que telegrafía su teletransporte durante `RiftLifetime` ticks. La
grieta NO daña (`CanDamage() => false`): es pura telegrafía — pero su ANATOMÍA es la
receta visual del desgarro. Timeline por tercios (`crackInterval = RiftLifetime/3`):

| Elemento | Técnica | Números exactos |
|---|---|---|
| **El agrietado** | textura `CrackedGlass_Glowing` dibujada **3 veces a rotaciones i·2π/3** (simetría triple = patrón de fractura) con shader propio | `CrackScale += 0.525 · interpolante · 0.75` por tercio; `MaxExposure += 0.25·interpolante` (cap 0.95); `CrackExposure = Lerp(actual, máx, 0.075)` |
| **La estrella de 4 puntas** | sprite de estrella dibujado **2 veces**: vertical `(1, 8)` y rotado π/2 `(1, 5)`, escala `· chargeInterpolant · 3.25` — el "punto donde la realidad se rompe" | + copia interior ×0.8 en color de la saga |
| **El anillo que IMPLOSIONA** | `BloomRing` con `ringScale = Lerp(8f, 0f, chargeInterpolant)` — el anillo SE CIERRA hacia dentro mientras la grieta se abre (telegrafía de implosión) | 8 → 0 |
| **El sonido que sube** | pitch del crujido `Lerp(-0.75, 0, Timer/RiftLifetime)` — la grieta "grita" cada vez más agudo al crecer | −0.75 → 0 |
| **Chispas por tercio** | 12 `SparkParticle` (vel 8-12·f, escala 1.2-1.6·f) + 8 `SquishyLightParticle` (vel 12-14·f) + screenshake `6·f·0.8` | color `Lerp(Select(Fuchsia, LightBlue, Twilight), White, 0.65)` |
| **Fade-in** | `Opacity += 0.05`, `scale += 0.02` | lineal |

**La explosión al abrirse de verdad** (`SpawnExplosionVisuals`): 25 proyectiles
`DoGRiftCrack` radiales (offset 20-80 px, ancho 20-30 px) + **35 metaballs CUADRADAS de
500 px** + 15-21 metaballs volantes (vel 25-35, tamaño 30-50) + 25 chispas (vel 12-16) +
20 luces + 3 explosiones de plasma + 2 shines + 1 bloom + screenshake **14**. Sonido de
rotura. (¡y factor de fotosensibilidad: ×0.6 en brillo — casa: lo respetamos con
intensidad configurable.)

**Colores de la saga DoG** (`Skies/DoGSky.cs`): `DoGTwlight = (147, 24, 204)` (violeta),
`DoGLightBlue = (0, 221, 250)` (cian), + Fuchsia `(255, 0, 255)`. El borde del metaball
de distorsión: `(136, 26, 186)`.

### 1.1.2 `DoGRiftCrack` — la grieta procedural (¡Lichtenberg en 30 líneas!)

**Dónde:** `Projectiles/Boss/DoGRiftCrack.cs` (86 L, leído completo).

Cada grieta radial es un **CAMINO GENERADO POR PASEO** (25 puntos) y dibujado como ribbon
con taper. El generador (paráfrasis propia):

```
punto[0] = origen
punto[1] = origen + velocidad·(0.8..1.2) + randomCirc(30)
para i ≥ 2:
    dir     = (punto[i-2] → punto[i-1])              // PERSISTENCIA de dirección
    paso    = random(25..50)                          // MECS 25-50 px (×0.5 en el último)
    giro    = random(-5°..5°) · i · 0.25              // CURVATURA ACUMULADA: las grietas
                                                      // se curvan MÁS cuanto más largas
    punto[i] = punto[i-1] + dir·paso girado giro
    si random(1/9): punto[i] += randomCirc(10)        // SALTO: micro-fallas del vidrio
```

Vida 30 ticks, `scale = Lerp(1, 0, Timer/30)` (muere entera encogiéndose), anchura del
ribbon `Lerp(MaxWidth, 0, completion)` (gorda en la raíz, fina en la punta). **Este
generador es EXACTAMENTE el patrón fractal de Lichtenberg** (misma matemática que la
quema de madera por alto voltaje — confirmado en búsqueda 10): persistencia de dirección
+ curvatura acumulada + pasos aleatorios. Nuestro StormLib.ZigPath ya hace la mitad; la
curvatura acumulada por índice es el ingrediente que falta.

### 1.1.3 `DoGRealityCrackShader.fx` — el shader de la rotura

**Dónde:** `Effects/DoGRealityCrackShader.fx` (53 L, leído completo). ps_2_0, 1 sampler:

- Muestrea la textura `CrackedGlass_Glowing`, calcula su brillo medio →
  `colorFromBrightness = lerp(darkerPixelColor, brighterPixelColor, brightnessRatio)`
  (mapa de brillo de textura → degradado de 2 colores: Twilight→Blanco).
- **Erosión radial animable**: en coords polares alrededor del centro, si
  `-polarCoords.y < opacityCutoffValue` multiplica por
  `pow(-polarCoords.y / (polarCoords.y - opacityCutoffValue), -fadeoutPower)` —
  la textura se EROSIONA desde fuera hacia dentro según `opacityCutoffValue` (que crece
  0→0.95 por tercios de vida): **la grieta "come" la textura al abrirse**.
- `minBrightnessValue` 0.75 = umbral de brillo para considerar "cristal roto".
- OverallOpacity base **0.1** (¡los cracks son SUTILES! el 90% del punch es la forma).

**Lección para casa (sin shader):** la erosión por umbral se puede replicar con 2-3
copias de la misma textura a escalas 1.0/0.72/0.45 y alphas escalonados (la técnica de
bloom apilado invertido de LumenLib leída al revés). El mapa brillo→2 colores se puede
cocinar en una textura procedural NUEVA (generada por código en un PNG 1×N tipo LUT —
casa ya sabe hacer LUTs, PyraPalettes).

### 1.1.4 `DoGSky` + `DoGVisualsManager` — la GRAN grieta en el cielo + el interior

**Dónde:** `Skies/DoGSky.cs` (499 L) + `Systems/Graphic/DoGVisualsManager.cs` (357 L),
leídos en las zonas de grieta.

**La herida del cielo** (aparece a 2000 px sobre el jugador, ±2000 de ruido):
- **3 `RealityCrack`**: la textura de cristal roto a `Scale = 1.55·(0.9..1.1)`,
  rotaciones i·2π/3, "Depth" 30 — en la capa de fondo más profunda (depth ≥ 6).
- **14 brazos principales** de grieta: 8 puntos cada uno, `MaxWidth` 100-120 px, pasos
  100-140 px × Depth·0.1 (= 300-420 px reales), varianza angular ±6°, ángulo de salida
  `i·2π/14` — **forman el agujero central**.
- **12-16 grietas exteriores**: 6-9 puntos, `MaxWidth` 6-9 px, pasos 400-500 × 0.1
  (= 1200-1500 px — ¡GRIETAS LARGUÍSIMAS que alcanzan media pantalla!), varianza ±5-10°.
- **Paralaje por profundidad**: cada punto se dibuja en
  `(punto − centroPantalla) · (1/Depth, 0.9/Depth) + centroPantalla` — el interior de la
  grieta se mueve MÁS LENTO que el mundo (X e Y con paralaje distinto: 1/30 vs 0.9/30).
- Ribbon blanco puro (alpha 1) dibujado a un **RENDER TARGET de "forma"**; otro RT guarda
  el "contenido" (nubes + rayos); el MetaballEdgeShader compone forma×contenido.

**EL INTERIOR DE LA GRIETA (¿qué se ve dentro?)** — la respuesta de Calamity:
1. Un **respaldo NEGRO** (`BlackTile` a pantalla del RT) — "el shader de nubes se ve
   transparente con colores oscuros y revelaría el cielo bajo la grieta".
2. **Nubes rodantes**: shader con `RealisticClouds` + `Neurons2` (distorsión) +
   `HarshNoise` (erosión), escala de nube 2, distorsión 0.24, erosiónMin 0.06,
   pixelationFactor = pantalla·0.5 (nubes SEMI-PIXELADAS para que casen con Terraria),
   colores oscuro `Lerp(Black, DarkGray, 0.3)` y claro `Lerp(Black, DoGSkyColor, 0.6)`.
3. **Relámpagos interiores**: el backdrop alterna negro↔blanco — mientras
   `BackgroundLightningTimer > 0` el fill se re-sortea CADA FRAME
   `random(0.5·máx, máx)` (máx 0.7-0.9), y las nubes se apagan ×(1 − fill·0.8):
   el interior de la grieta PARPADEA con tormenta. Duración 5-20 o 30-45 ticks,
   probabilidad 1/200 por tick.
4. **Vientos en primer plano**: 2 capas de "viento" (textura Neurons2 + highlights
   SharpNoise + MeltyNoise + Pebbles): capa fucsia `time·1.075, distorsión 0.3,
   erosionMin 0.5·SkyIntensity` y capa cian `time·0.8, distorsión 0.6, erosionMin 0.75`.
5. **El aura**: textura `Smudges` girando a `2π/270` rad/s con el DoGRiftAuraShader.
6. **EL MUNDO SE OSCURECE** (`ModifySunLightColor`): el fondo se lerpea a
   `White·0.035` (+ skyColor·0.025), los tiles a brillo 35 (modo Fancy), FillProgress
   += 0.05/tick — la presencia de la grieta APAGA el mundo. El sol y la luna se
   desvanecen ×(1 − SkyIntensity).

**Lecciones para casa:** (a) el interior de un desgarro = NEGRO + contenido que se ve
SOLO dentro (nosotros: `BlackDisk.png` 256² ya existe + estrellas procedurales);
(b) paralaje X≠Y para el interior (0.9/1.0); (c) el mundo reacciona oscureciendo —
nosotros podemos hacerlo sin shader con un velo alfa de pantalla (OndaSystem.Flash con
color casi negro en AlphaBlend... o mejor: un velo Multiplicativo por ModifySunLightColor
casa— factible: GlobalTile/ModSystem con ModifySunLightColor ya lo permite).

### 1.1.5 `RealityRupture` — EL ARMA "desgarro de realidad" del ecosistema

**Dónde:** `Items/Weapons/Rogue/RealityRupture.cs` + `Projectiles/Rogue/
RealityRuptureStealth.cs` (leídos) + `RealityRuptureLance.cs` + `RealityRuptureMini.cs`.

- **Qué es**: jabalina rogue post-Moon Lord (daño 420), mejora directa de la "Spear of
  Destiny" (wiki Calamity, búsqueda 03). El stealth strike (`RealityRuptureStealth`,
  extraUpdates 7) es el que "rompe la realidad": al impactar spawnea
  `SpearofDestinyStealthExplosion` (daño/2 + knockback ×2) + **screenshake 8**.
- **El impacto del stealth** = 6 `CrackParticle` pequeñas (escala 1.8-2.3, colores
  Orchid/Plum) + 1 GRANDE (2.9-3.2) — el "sello de impacto" de cristal roto.
- **`CrackParticle`** (leído completo): textura `Crack.png`, ADITIVO, aparece con easing
  **PolyOut(4)** (instantáneo), rotación = `velocidad + π/2`, la velocidad se congela
  tras 1 tick (¡aparece donde toca y NO viaja!), alpha = `sin(π/2 + t·π/2)` (empieza
  llena, muere suave). Es el "impact crack" estático perfecto.
- **La lanza normal** (`RealityRuptureLance`): 9 dusts/tick orbitando en anillo de radio
  `(20 + cos(t/3)·12)·radiusFactor` con `offsetRotationAngle = vel.ToRotation() + t/20`
  (¡el anillo de partículas GIRA mientras avanza!), dusts 234/310, `velocity·0.8` de
  arrastre, escala 1.1-1.7. Luz violeta `(0.6, 0.2, 0.9)`. Daño decae ×0.95 por hit
  (penetración 8). Muerte: dusts `WitherLightning` (262/242/310).
- **`RealityRuptureMini`**: homing suave (radio 350), chispa Plum cada 2 ticks con
  prob. `SparkChance`.

### 1.1.6 `BigRipMetaball` + `ArkOfTheCosmos_BlastAttack` — el desgarro del arma final

**Dónde:** `Graphics/Metaballs/BigRipMetaball.cs` + `Projectiles/Melee/
ArkOfTheCosmos_BlastAttack.cs` (leídos). "Big Rip" = ¡el nombre cosmológico literal de
la realidad desgarrándose (teoría del Gran Desgarro)!

El ataque de estallido del Arca del Cosmos deposita en el metaball del Big Rip una
"línea" con **LA PROPIA TEXTURA DEL PROYECTIL como forma del metaball**: rotada
`vel.ToRotation() + π/2`, escala `(0.25·thrustRatio, 1·thrustRatio)`, tamaño 242,
`SizeScaling 0.925` (encoge 7.5%/tick), posición desplazada a lo largo de la velocidad
`Lerp(0, thrustRatio·242, 0.5)`. EdgeColor `(239, 111, 85)` (coral). Screenshake 7.5.
**Lección: un desgarro puede ser una forma ARBITRARIA (no solo círculos) dentro del
sistema de distorsión.** Casa: nuestra lente consume "fuentes" con posición/radio —
una fuente-LÍNEA es una extensión natural.

### 1.1.7 `DrawChromaticAberration` — la aberración cromática SIN shader (¡de Spirit!)

**Dónde:** `Utilities/DrawingUtils.cs` L794-825 (leído completo). Crédito textual en el
código: *"Thanks spirit <3"* — la técnica la popularizó Spirit Mod.

```
para i en {-1, 0, +1}:
    tint    = (255,0,0,0) si i=-1 · (0,255,0,0) si i=0 · (0,0,255,0) si i=+1
    offset  = dirección.perpendicular · i · strength
    dibujar(sprite en posición+offset, color = tint · alpha)
```

**3 draws con tinte de canal puro y offset perpendicular = RGB SPLIT 100% SpriteBatch,
cero shaders.** Calamity la usa en toda la familia Wulfrum/Marnite (tema mecánico) y en
`TechyHoloysquareParticle` (strength **1.5 px**, rotación alineada a la velocidad,
`Velocity *= 0.875`, `Scale *= 0.96`, luz proporcional al alpha, 6 variantes de marco
6×6..18×12 — "holo-cuadrados"). La web corrobora la teoría (glitchology.com:
"dupliqua la imagen 3×, aísla un canal por copia, desplaza cada capa"; godotshaders
VHS: jitter horizontal + RGB split + scanlines).

**Casa ya tiene el 90%:** OndaLib.Shock hace franjas R/B al ±1.8% del radio con tinte
`R×1.15/G×0.25/B×0.25`. FALTA: la versión "sprite completo ×3 con canal puro" para los
labios del desgarro.

### 1.1.8 Negativos documentados (Calamity)

- **"Reality Slasher" NO EXISTE** (el nombre que barajaba el usuario): el arma real es
  **Reality Rupture** (búsqueda 03 + wiki). "Sundering Scissors" (Ark of the Cosmos)
  es otra familia: tijeras que "rajan" (texturas de hoja + glow, sin grieta procedural).
- SCal (Supreme Calamitas) **NO tiene "reality sunder"** en el código público: sus
  efectos son el arena-metaball (ScalArenaMetaball con WavyLineLayer), el bullet hell
  y el aviso translúcido previo a la carga (wiki, búsqueda 02). La sensación de
  "romper la realidad" de SCal viene del CONTRASTE blanco/negro del fondo, no de grietas.
  El fandom asocia "reality sunder" a la fase final de DoG (la grieta del cielo) —
  búsqueda 02 no devuelve un ataque llamado así.

## 1.2 MEAC — EL DESGARRO QUE PARTE LA PANTALLA (la técnica #1 del informe)

**Dónde:** `MEAC/Projectiles/SCCut.cs` (327 L, leído COMPLETO) + `MEAC/MEAC.cs`
(EndCapture L439-570, UseWarp L817-839, StartGlur L360) + `MEAC/Entities/LineWarp.cs`,
`LineWarp2.cs`, `RingWarp.cs`, `Wave_Warp.cs`, `VisualEffect.cs`.

MEAC (el rework chino de la Emperatriz) tiene **la implementación literal de "cortar la
pantalla en dos"** — es exactamente el "desgarro en la realidad" con daño:

### 1.2.1 `SCCut` — el proyectil-corte

- **DAÑO POR LÍNEA INFINITA**: `Collision.CheckAABBvLineCollision(hitboxNPC,
  Center − velocity·1000, Center + velocity·1000)` — el hitbox del "corte" es una línea
  de **2000 px** a lo largo de la velocidad. `ShouldUpdatePosition() => false` (no se
  mueve: ES el corte). `DrawScreenCheckFluff = 10000`.
- **El dibujo del corte**: textura `Extra_196` estirada a 2000 px de largo con
  **RECTÁNGULO FUENTE ANIMADO** `new Rectangle(−timeLeft·2, 0, width·2, height)` —
  ¡el INTERIOR del corte FLUYE (scroll de 2 px/tick dentro del desgarro)! Color rojo
  `(200, 10, 0)`, scaleY = `0.5 · ai[0]`, batch aditivo con `PointWrap`.
- **Timeline**: ai[0] `+= 0.333/tick` al abrir (3 ticks a plenitud) → sostiene →
  `−= 0.05/tick` al morir (20 ticks) → **`hostile = false` cuando timeLeft < 8:
  EL DAÑO CESA 8 TICKS ANTES DE QUE EL VISUAL DESAPAREZCA** (lección de juego justo).
- Variante ai[1]==1 (el corte del boss final): abre a 0.2/tick durante 265 ticks y al
  morir siembra 10 dusts + un proyectil de caída.

### 1.2.2 `UseSCCutShader` — LA PANTALLA SE PARTE EN DOS

El pipeline (paráfrasis del código leído):

```
para cada proyectil SCCut activo:
    centro  = (proj.Center − screenPosition) / pantalla        // UV 0..1
    para cada línea acumulada:                                 // ¡se ACUMULAN los cortes!
        si centro.Y > k·centro.X + b:  centro += desplazamiento
        si no:                          centro −= desplazamiento
    k, b    = recta del corte en UV (pendiente corregida por aspecto 1920/1080)
    offset  = perpendicular del corte · (0.02 · ai[0])          // 2% de la pantalla MÁX
    shader.parametros["line0"]  = (k, b)
    shader.parametros["offset"] = perpendicular
    shader.parametros["halfWidth"] = 0.02 · ai[0]
    dibujar(copia del mundo) con el shader → la MITAD SUPERIOR se desliza a un lado
    y la INFERIOR al otro, en una banda de ±2%·apertura alrededor de la línea
```

**El efecto visual: la pantalla se ABRE por la línea del corte** — cada mitad del mundo
se desplaza en direcciones opuestas, perpendicular al corte. Los cortes se acumulan (una
lista de rectas): cada corte añade su propio desplazamiento → con 2-3 cortes la pantalla
queda hecha jirones. Es EL look "reality tear" de pantalla completa más barato conocido:
**1 shader de 1 pasada que evalúa N rectas por píxel** (nada de máscaras por render
target para esto).

### 1.2.3 El sistema de WARP por máscara (KScreen0) — distorsión direccional

Todas las entidades `VisualEffect` con `isWarp = true` se dibujan a un RT máscara y el
filtro de pantalla `KScreen0` re-muestrea el mundo con `intensidad i = 0.02` (2% UV):

| Entidad | Codificación de la máscara | Vida | Comportamiento |
|---|---|---|---|
| `LineWarp` | `Color(rotación/2π, 1, 1)` — R = **ÁNGULO** de la línea, G = fuerza | 20 | X crece +2/tick (la línea se ALARGA), al morir `alpha −= 0.1` y retrocede `Center −= Velocity` |
| `LineWarp2` | igual (textura Ball) | 10 | X crece +3/tick (más rápido) |
| `RingWarp` | `Color(1, alpha, 1)` — R = 1 (¿radial?) | — | anillo de distorsión |
| `CircleWarp` | `Color(1, alpha, 1)` | — | círculo |
| `Wave_Warp` | `Color(2, alpha, 1)` + textura **Perlin 512²** + shader ForceField | 15 | escala ×(1, 0.7), `ai1 += 0.4/tick`, alpha −0.2 en los últimos 5 — ONDA de distorsión con ruido |

**La técnica: la máscara codifica DIRECCIÓN en R y FUERZA en G/alpha; el shader
desplaza el mundo en esa dirección.** SCCut además se registra en la máscara (DrawWarp:
`Color(rotación/2π normalizada, 0.5, 1)` en un quad de 2000 px × `0.4·ai[0]`) — el corte
distorsiona el mundo alrededor Y lo parte. Nuestra `BlackHoleLensSystem` ya hace
"máscara+re-muestreo" (pantalla completa, 2 pases, 5 fuentes) pero con fuentes RADIALES
fijas: **una fuente DIRECCIONAL (línea) es el hueco exacto.**

### 1.2.4 `glurTimer` — el GLITCH de eco (feedback de pantalla)

`StartGlur(20/60)` (llamado por RedGiantLaser y MeleeProj): un anillo de **5 RTs de
pantalla completa** — cada frame copia la pantalla a `Images[índice]`, los alphas
decaen `−= 0.2/frame`, y se re-componen **ADITIVAMENTE** sobre la pantalla con brillo
por conteo activo (1→1.0, 2→0.78, 3→0.71, ≥4→0.68). Resultado: ecos fantasma de los
últimos 5 frames que se desvanecen — el look "señal rota". **Versión sin RT posible en
casa: fantasmas de sprites con offset horizontal ±2-3 px y tinte de canal (R/B) —
el RGB-split de 1.1.7 en modo "eco"** (así lo hacen los godotshaders VHS: jitter +
RGB + scanlines).

### 1.2.5 Otros efectos de pantalla de MEAC (inventario de shaders)

`Screen/Screen2` (RSScreen), `ScreenColor`, `Bloom1` (bloom GlurV/GlurH a 1/3 de
resolución, uRange 1.5, uIntensity 1.05 — ¡dos pases de blur separables + composite
aditivo ×3 de escala!), `Mirror`, `Gray`, `Invert` (invertir colores = "visión del
otro lado"), `SCCut`, `Cosmic`. Registrados como `Filters.Scene` con prioridad 2-4.

## 1.3 WOTE (Wrath of the Elements + Luminance) — la flecha de grieta arcoíris

**Dónde:** `Content/NPCs/EoL/Projectiles/RainbowRiftArrow.cs` (138 L, completo) +
`EventideLances.cs` + `Particles/Metaballs/DistortionMetaball(.cs/System)`.

- El "Rift Arrow" de las lanzas del crepúsculo: proyectil con `TrailCacheLength 54`,
  `DrawScreenCheckFluff 1600`, MaxUpdates 3, lifetime 0.5 s. Fade-in `InverseLerp(0,12,t)`,
  escala `InverseLerp(0,6,t)`. Tras 10 updates `velocity *= 0.7` (¡frena en seco al
  "clavarse" en la realidad!) y `HueInterpolant += 0.2`; antes `velocity *= 1.04`.
- **Mientras viaja**: `DistortionMetaball.CreateParticle(center, 0, 4f, 0.75f, 0.25f,
  0.03f)` CADA FRAME si vel ≥ 0.2 — la flecha deja un **túnel de distorsión** (4 px de
  tamaño, decae 0.03/frame → dura ~25 frames, crece 0.25/frame, vel.X ×0.97).
- El ribbon: shader RainbowTrail con `localTime = −GlobalTime·1.5 + identity·0.51`
  (anti-sincronía por identidad), `hueSpectrum 1.3`, textura WavyBlotchNoise, anchura
  con **tipCutFactor** `pow(InverseLerp(0.04, 0.3, p), 0.6)` (punta de flecha) ×
  `slownessFactor = Remap(|vel|, 0.3, 1, 0.23, 1)` — **la estela se ENGORDA cuando el
  proyectil frena** (energía acumulada).
- Al morir: PrismaticBurst + el jefe SE TELETRANSPORTA al punto de muerte (la flecha
  ES una grieta de entrada). Sonido Item165.
- `PrismaticBurst`: radio ideal 780 px, `Radius = Lerp(actual, ideal, 0.075)`,
  `Opacity = InverseLerp(2, 10, timeLeft)`, **`damage = 0` cuando Opacity ≤ 0.8** (¡deja
  de dañar mientras aún brilla — MISMA lección que SCCut!), hitbox = Radius·1.1,
  metaball de distorsión de 30 px + 8 partículas de fuego en arco espiral.
- El `DistortionMetaball` (Luminance): textura AngularBloomRing, `Velocity.X *= 0.97`,
  `Size *= 1 + growth`, fuerza `Saturate(fuerza − decaimiento)`, muere a 0.01. El
  filtro se activa SOLO si hay partículas (cero coste en reposo) y recibe
  `screenZoom` (¡respeta el zoom del jugador!).

## 1.4 EVERGLOW — el portal de roca (datos por COLOR DE VÉRTICE)

**Dónde:** `Sources/Modules/Yggdrasil/YggdrasilTown/VFXs/RockPortal.cs` (50 L).

Portal elíptico = **4 vértices** (diamante achatado ×0.5 horizontal) con UV animada
(`timeValue = Main.time·0.001`, +0.4 de desfase entre vértices = ROTACIÓN del interior)
y **el COLOR del vértice codifica datos**: `(0,0,p), (0,1,p), (1,0,p), (1,1,p)` — el
shader lee qué vértice es por su color. Escala `+= 2/tick`. Casa: VFXCore.GlowQuad no
lleva color-de-datos pero SpriteBatch sí soporta vertex color arbitrario — técnica
compatible con quads si algún día hacemos primitivas custom; hoy NO la necesitamos.

### 1.4bis (reintento 42-b) — Everglow TAMBIÉN tiene grietas procedurales: 2 hallazgos nuevos

**A) `GoldenCrack`** (`MEAC/PlanetBeFall/Projectiles/NonIIIDProj/GoldenCrack/GoldenCrack.cs`,
255 L) — un generador de GRIETA RECURSIVO (árbol de nodos), la contrapartida del
Lichtenberg de Calamity pero con RAMIFICACIÓN EXPLÍCITA:
- `Tree`/`Node{rad, size, length, ismaster, isbrunch, isterminal, isfork}`; raíz de
  `length = 300·Rand()`; `Buildmaster` hasta **profundidad 10** (terminal en dep 10).
- **Bifurcación**: con probabilidad 1/5 o al llegar a dep 5 → nodo fork con ángulo
  `±|Rand(π/4)|` (el SIGNO hereda el del padre → la rama sale al MISMO lado); las
  ramas mueren a **profundidad 2** (cortas).
- **Continuación del tronco**: ángulo `Rand(±π/6)`, longitud `×Rand()·0.9` (taper del
  10% por segmento) — la grieta se estrecha Y serpentea ±30°.
- `Rand()` = Box-Muller `√u·cos(v)·0.3 + 0.5`, `+0.4`, **clamp 0.7..1** — los tamaños
  nunca caen por debajo del 70% (las puntas nunca se vuelven invisibles).
- El proyectil (6×20, timeLeft 180, scale 0.8) **crece `ai[1] += 1/60`/tick mientras
  timeLeft ≥ 28 y se cierra `−1/24`/tick después** → abre en ~60 ticks, cierra en 24
  (asimetría apertura:cierre 2.5:1 — MISMA escuela que SCCut, signo invertido).
- Recorre el árbol con un puntero `j` que sube/baja por terminales/forks → convierte
  el árbol en una POLILÍNEA dibujable (¡el truco de serialización árbol→camino!).

**B) `LanternCrackingRay`** (`Myth/LanternMoon/NPCs/LanternGhostKing/LanternCrackingRay.cs`,
117 L) — el RAYO QUE AGRIETA el escudo dorado del rey farol:
- `Visual` en **`CodeLayer.PostDrawBG`** — la grieta se dibuja DETRÁS del mundo (entre
  fondo y tiles): eso es lo que la hace parecer "en la realidad", no "sobre ella".
- 80 segmentos de anillo, `pos.Y ×= 0.6` (elipse achatada), **vértice interior
  `(0.7, 0.7, 0.4)·mul·fluctuation` → vértice exterior `(0,0,0,0)`** (se desvanece
  radialmente hacia fuera) con `Noise_flame_3` en TriangleStrip.
- **`fluctuation = 1 + Sin(t·2π·18)·0.45`** — parpadeo a 18 Hz con ±45% de amplitud
  (el "chisporroteo eléctrico" barato y convincente).
- Expansión: `range = 300 + Timer·30` (cap 600); **en Timer 65-70: `range +=
  (Timer−65)·200` y ADEMÁS el batch re-dibuja TODA la tira (Timer−65) veces** — el
  eco aditivo de la MISMA geometría N veces = pulso de brillo sin partículas. Fade
  final `(90−Timer)/20`.
- Recorta por altura (`pos.Y < targetValueY → color 0`) para "cortar" la grieta contra
  el terreno — enmascaramiento por posición de vértice, cero coste.

**RiftLib se queda con 3 lecciones nuevas:** (1) la rama Lichtenberg puede ser ÁRBOL
(no solo camino) con signo heredado del padre; (2) parpadeo 18 Hz/±45% para labios
vivos; (3) re-dibujar la misma geometría k veces = flash de eco gratis (sin RT).

## 1.5 VANILLA 1.4.4 — el Portal Gun (lo mínimo viable)

`Projectile 600`: elipse GlowMask[173] tintada `PortalHelper.GetPortalColor(owner, ai[1])`
con **alpha 70** — el cuerpo del portal es una textura glow elíptica SENCILLA. La
apertura (`Main.DoEffects case 4`): burst de **dusts 263** coloreados con el color del
portal, `noLight, noGravity, scale 1.2, fadeIn 0.4`, densidad `w·h/5·mult`, sonido
Item8. **Lección: un portal/desgarro válido = forma glow + estallido de partículas del
color propio.** (La "magia" real de los portales vanilla es la teletransportación, no
el VFX.)

## 1.6 WRATH OF THE GODS / STELLAMOD — Nameless Deity: la referencia del usuario

(búsquedas 01/12/18; el decompile local de LunarVeil es una versión ANTIGUA sin este
contenido — documentado). Evidencia de wiki + Steam + Reddit:

- **"Reality Shatter Punches"**: "Dos de los puños del Nameless Deity aparecen a
  ambos lados del jugador y golpean juntos, creando **un desgarro en la realidad de
  corta vida que daña** en el impacto" — wiki terrariamods.wiki.gg (búsqueda 18).
  **ES LITERALMENTE LA MECÁNICA PEDIDA POR EL USUARIO.**
- Fase del 30% de vida: "**consume el fondo** (el background desaparece) y el jefe se
  esfuma; después **la realidad empieza a agrietarse** y una serie de **tajos de luz**
  persiguen al enemigo" — Steam Workshop (búsqueda 01).
- El mod también trae a **Noxus, Entropic God** (un "sol negro" con efectos de
  distorsión; los usuarios reportan "visuals make it look way harder than it actually
  is" — la LECCIÓN: el 90% del terror de estos jefes es TELEGRAFÍA AMBIENTAL, no daño).
- Ambos jefes usan pantallas de advertencia/cutscenes (el usuario los recordaba bien).

**Diseño extraíble sin código fuente:** el momento "reality tear" de élite = (1) quitar
el fondo/oscurecer, (2) grietas que se extienden por la pantalla, (3) tajos de luz
blancos delgados como cuchillas, (4) TODO A LA VEZ con el jugador libre de moverse.

## 1.7 JUEGOS AAA (búsquedas 04-10) — el vocabulario visual fuera de Terraria

| Juego | Mecánica "desgarro" | Qué copiamos (100% factible SpriteBatch) |
|---|---|---|
| **Genshin Impact** | El Abismo/Vacío: cutscenes "A Void Gnawed Into Stillness" — el vacío entra por RASGADURAS verticales negras con borde blanco-azulado; los enemigos del Abismo emergen de grietas | La silueta: banda oscura + labio claro + partículas que EMERGEN hacia fuera |
| **Zelda BotW/TotK** | La "malicia" (malice): charcos negros con textura burbujeante + OJOS rojos que se abren; grietas de malicia en suelos/paredes | Pools persistentes de "materia hostil" (casa: BrumaFX.FlushNonPremultiplied ya ocluye); lo del "ojo" ya lo descartamos una vez (v5.96) |
| **Hollow Knight** | Dream Nail: tajo BLANCO puro de 1 frame + destello; los golpes de esencia son trazos blancos finos con núcleo | El FLASH de 1 frame + trazo fino (OndaLib.Flash + un quad TrailTex blanco) — el "impacto limpio" antes del desgarro sucio |
| **Doom Eternal** | Portales del infierno: anillo de brasas + interior negro con "carne"; los "hell rifts" marcan spawn-points con columnas de luz | Columna vertical de luz (LumenLib.Lance YA lo hace) como telegrafía de spawn |
| **Arte glitch (web)** | RGB split ×3 copias canal puro (glitchology.com); displacement lines horizontales (agatedragon.blog); VHS: jitter + RGB + scanlines (godotshaders); datamoshing/pixel sorting (designmd.app) | RGB split de 1.1.7 (3 draws); "bandas" = quads horizontales con offset ±1-3 px alternado y tinte R/B; scanlines NO (rompe el pixel-art de Terraria) |
| **Lichtenberg (física real)** | Figuras fractales por descarga de alto voltaje: ramificación con persistencia de dirección + curvatura acumulada | EL GENERADOR de DoGRiftCrack 1.1.2 es exactamente esto — con semilla determinista |

## 1.8 TRANVERSAL — ¿CÓMO HACE DAÑO UN DESGARRO? (las 4 escuelas)

| Escuela | Quién | Cómo | Números |
|---|---|---|---|
| **A. Línea hitscan persistente** | MEAC SCCut; Nameless Deity (Reality Shatter Punches) | El "proyectil" es la LÍNEA: AABBvLineCollision de ±1000 px, dibujada encima; el daño vive mientras la línea está abierta | 2000 px de línea, inmunidad local, daño OFF 8 ticks antes del final visual |
| **B. Zona flotante que respira** | (la petición: grieta persistente) | Rect/elipse de daño flotante con DoT dentro; el visual "respira" ±6-8% | cooldown local 15-20 ticks, radio 24-32 px |
| **C. Impacto único de área** | Calamity RealityRuptureStealth, DoG explosion | La grieta ES la explosión: cracks visuales 30 ticks + shake + partículas | shake 7.5-14, 6-25 cracks |
| **D. Telegraph puro (0 daño)** | Calamity DoGTeleportRift | La grieta anuncia ALGO que vendrá (teleport, ataque) | RiftLifetime ticks |

**El bastón del usuario pide A+B**: disparas → se abre un desgarro (A: la línea daña al
pasar el "corte" inicial) → queda una grieta persistente (B: DoT a quien la cruce/toque)
→ se cierra con shards. Todas las piezas existen arriba con números.

---

# PARTE 2 — TÉCNICAS FACTIBLES 100% EN CASA (SpriteBatch + glow + quads)

Presupuesto de referencia de la casa: ~1-2k quads por efecto rico (los agujeros negros
giran 400-600). Tabla de viabilidad (todo contra nuestro stack real):

| # | Técnica | Fuente | Implementación en casa | Coste | Prioridad |
|---|---|---|---|---|---|
| T1 | **Labios de luz triples** (velo/cuerpo/núcleo a lo largo de una línea) | EstelaLib.Ribbon (casa) | Re-utilizar el motor de ribbon con perfil "lens" (gordo al centro, fino en puntas) | ~60 quads | YA EXISTE |
| T2 | **Interior negro que OCLUYE** (el vacío se ve NEGRO aunque haya sol) | DoG BlackTile; BrumaFX v2 | Quad negro no-premultiplicado (`BlackDisk.png` 256²) estirado dentro del desgarro | 1-4 quads | YA EXISTE |
| T3 | **Estrellas dentro** (espacio profundo visible por la rasgadura) | DoG contents RT | N=16-28 quads diminutos (1-3 px) blancos/cian POSICIONADOS DENTRO de la banda, con parallax X≠Y (0.9/1.0) y parpadeo por hash | ~28 quads | NUEVO (fácil) |
| T4 | **Scroll del interior** (lo de dentro FLUYE) | MEAC SCCut (sourceRect −2 px/tick) | Dibujar las estrellas con offset = (t·velocidad) mod longitud a lo largo del eje del desgarro — "el vacío se cae hacia dentro" | 0 extra | NUEVO (fácil) |
| T5 | **Aberración R/B en los labios** | Calamity DrawChromaticAberration (¡3 draws!) | El borde del desgarro se dibuja 3× con tinte canal puro (255,0,0,0)/(0,255,0,0)/(0,0,255,0) y offset perpendicular ±1.5-3 px | ×3 en los labios | NUEVO (fácil) |
| T6 | **La estrella de 4 puntas** (el punto de ruptura) | DoG (1,8)+(1,5)×3.25 | `Star.png` (16²) 2 draws: vertical escala (1,8)·c·3.25 y rotada π/2 (1,5)·c·3.25 | 2-4 quads | NUEVO (trivial) |
| T7 | **Grieta procedural Lichtenberg** | DoGRiftCrack | Camino por paseo: persistencia de dirección + giro ±5°·i·0.25 acumulado + salto 1/9; ribbon con taper MaxWidth→0 | 24-32 quads | NUEVO (medio — ZigPath ya tiene la mitad) |
| T8 | **Shards de cristal** (impact cracks) | Calamity CrackParticle | Quads con `Slash.png`/star rotados a la velocidad, aparecen PolyOut(4), congelan 1 tick, alpha sin(π/2+t·π/2); o paquete ParticleData con gravedad | 6-10 quads | NUEVO (fácil) |
| T9 | **Anillo que IMPLOSIONA** (telegrafía) | DoG BloomRing Lerp(8,0) | `Ring.png` con radio `Lerp(maxR, 0, progreso)` + `ParticlePresets.Implosion` YA EXISTE en casa | 1-2 quads | YA EXISTE |
| T10 | **Kick + Flash + sonido con pitch ascendente** | DoG Lerp(-0.75,0,t); OndaLib | `OndaLib.Kick(6-8px)` + `OndaLib.Flash(color, 0.22)` + SoundStyle pitch −0.75→0 (es dato, no código) | 0 quads | YA EXISTE |
| T11 | **El mundo se oscurece** con la grieta presente | DoG ModifySunLightColor | ModSystem.ModifySunLightColor: lerp tiles→brillo 35/255 y fondo→0.035 con FillProgress ±0.05/tick | 0 quads | NUEVO (fácil, sin RT) |
| T12 | **Chispas de anomalía** (violeta/cian lerp blanco 0.65) | DoG crack sparks | Paquete determinista para ParticleManager (vel 8-14, drag 0.96, luz por vértice) | ~24 partículas | NUEVO (fácil) |
| T13 | **Eco glitch sin RT** (5-frame feedback) | MEAC glur; VHS web | Fantasmas del desgarro con offset horizontal ±2-3 px alternado y tinte de canal puro — 2-3 draws extra | ×2-3 | NUEVO (fácil) |
| T14 | **Pantalla partida por la línea del corte** | MEAC SCCut shader | Fase 2 opcional: fuente-LÍNEA en `BlackHoleLensSystem` (desplazamiento perpendicular ±2% UV a cada lado de la recta) — el shader es NUESTRO, añadir un tipo de fuente es una extensión natural | 1 pasada extra | NUEVO (medio-alto: shader + lente) |
| T15 | **Distorsión direccional en máscara** (ángulo en R) | MEAC KScreen0 | Misma vía que T14: codificar dirección/fuerza; NO hacer un pipeline nuevo paralelo al de la lente (regla de la casa: UN solo sistema de pantalla) | — | fusionado con T14 |
| T16 | **Burbujas cuadradas de distorsión** | DoG 35 squares 500px | OndaLib.Pulse con aberración cromática ya lo aproxima; opcional: quads cuadrados (no glow) en la lente | 4-8 quads | OPCIONAL |

**Descartado con razones:** scanlines/pixel-sorting (rompe el pixel-art); "ojo" en la
grieta (ya lo vetó el usuario en v5.96); invertir colores de pantalla (MEAC tiene el
shader Invert pero un desgarro INVERTIDO completo marearía y compite con la lente);
cutscenes con texto de advertencia estilo SCal (no hay jefes en AethonMod aún; es
contenido de boss, no de arma).

---

# PARTE 3 — CONTRATO DE DISEÑO: **RIFTLIB** (`Content/VFX/RiftLib.cs`)

## 3.0 Qué es

La librería de los DESGARROS DE REALIDAD: la familia de efectos "reality tear" para el
bastón del usuario y todo lo que venga después. Cubre las tres piezas del vocabulario:

1. **EL DESGARRO (Tear)** — la línea que se abre de golpe: labios de luz con aberración
   R/B, interior negro con estrellas que fluyen, estrella de 4 puntas en el punto de
   ruptura, shards de vidrio, paquete de impacto (kick+flash). Daña como LÍNEA
   (escuela A de 1.8).
2. **LA GRIETA (Rift)** — la herida persistente que respira: camino fractal
   Lichtenberg, chispas de anomalía, interior profundo con paralaje. Daña como ZONA
   con DoT (escuela B).
3. **LOS COMPLEMENTOS** — el oscurecer del mundo (T11), el eco glitch (T13) y la
   extensión de lente pantalla-partida (T14, fase 2 opcional).

**Columna vertebral de la casa que se re-utiliza:** VFXCore (Hash01/Breathe/Quad),
EstelaLib (Sanitize/Smooth/Resample/SegQuad-motor del ribbon), OndaLib
(Kick/Flash/Expansion), BrumaFX (batch no-premultiplicado para el vacío),
ParticleManager (chispas/shards), texturas procedurales existentes:
`Star.png`, `Slash.png`, `BlackDisk.png`, `SoftGlow`, `TrailGlow`, `Ring`, `Noise.png`.

## 3.1 Contrato de diseño (API pública, estilo de la casa)

```csharp
namespace AethonMod.Content.VFX
{
    /// <summary>Fase vital del desgarro (la línea de tiempo del corte).</summary>
    public enum RiftFase
    {
        /// <summary>Telegrafía: la estrella de ruptura crece y el anillo implosiona (0 daño).</summary>
        Telegrafo,
        /// <summary>EL GOLPE: el desgarro se abre en 3 ticks (MEAC 0.333/tick) — aquí va el Kick.</summary>
        Apertura,
        /// <summary>Sostenido: el desgarro está abierto y DAÑA (la línea viva).</summary>
        Sostenido,
        /// <summary>Cierre: los labios se cierran a −0.05/tick; el daño cesó 8 ticks antes.</summary>
        Cierre,
    }

    /// <summary>La personalidad cromática del desgarro (una por "qué hay al otro lado").</summary>
    public static class RiftPaletas
    {
        /// <summary>El VACÍO frío (violeta→cian→blanco) — la casa de VoidCold.</summary>
        public static readonly Color[] Vacio = { new(150, 80, 255), new(60, 80, 220), new(80, 200, 255), new(235, 245, 255) };

        /// <summary>El desgarro CARMESÍ (MEAC SCCut rojo 200,10,0 + fucsia DoG) — realidad herida.</summary>
        public static readonly Color[] Carmesi = { new(200, 10, 0), new(255, 60, 120), new(255, 200, 220) };

        /// <summary>La ENTROPÍA (Twilight 147,24,204 + LightBlue 0,221,250 de DoG — con permiso de cita de datos).</summary>
        public static readonly Color[] Entropia = { new(147, 24, 204), new(0, 221, 250), new(255, 255, 255) };
    }

    /// <summary>
    /// RiftLib — v6.26 — LA LIBRERÍA DE LOS DESGARROS DE REALIDAD.
    ///
    /// Nace de la investigación del ecosistema (Calamity DoG/RealityRupture/BigRip +
    /// MEAC SCCut/warps/glur + WoTE RainbowRiftArrow + Wrath of the Gods "Reality
    /// Shatter") re-implementada 100% con código PROPIO sobre la pila de la casa:
    /// SpriteBatch + texturas procedurales + hash determinista. Cero dependencias
    /// externas, cero código ajeno.
    ///
    /// EL VOCABULARIO (qué es un desgarro de verdad):
    ///   · EL DESGARRO ES UNA LÍNEA con LABIOS DE LUZ (3 capas por segmento — velo/
    ///     cuerpo/núcleo, la doble pasada de la casa) y ABERRACIÓN R/B en el borde
    ///     (3 draws con tinte de canal puro y offset perpendicular ±2 px — lección
    ///     Calamity/Spirit: DrawChromaticAberration).
    ///   · EL INTERIOR ES VACÍO PROFUNDO: banda que OCLUYE (negro de verdad, batch
    ///     no-premultiplicado) con ESTRELLAS que fluyen a lo largo del eje (scroll
    ///     MEAC) y parallax X≠Y (lección DoG: 0.9/1.0).
    ///   · LA APERTURA ES UN GOLPE: 3 ticks de apertura (MEAC 0.333/tick), Kick de
    ///     cámara, Flash, y la ESTRELLA DE 4 PUNTAS del punto de ruptura (DoG:
    ///     vertical ×8 + horizontal ×5).
    ///   · EL CIERRE ES JUSTO: el daño cesa 8 ticks ANTES de que el visual muera
    ///     (lección MEAC SCCut + WoTE PrismaticBurst).
    ///
    /// CONTRATO (idéntico al de EstelaLib/OndaLib): los métodos de DIBUJO dibujan en
    /// el SpriteBatch ABIERTO que el llamador tenga (aditivo recomendado) y NO lo
    /// tocan; el interior negro se vuelca con BrumaFX (lote propio). TODO
    /// determinista por semilla: misma secuencia SIEMPRE. La librería NO guarda
    /// estado de desgarros (progress/fase los lleva el llamador).
    /// </summary>
    public static class RiftLib
    {
        // ==================================================================
        //  EL DESGARRO — la línea que se abre (escuela A de daño)
        // ==================================================================

        /// <summary>
        /// DIBUJA EL DESGARRO: la línea de <paramref name="origin"/> a
        /// origin+dir·<paramref name="length"/>, con anchura viva
        /// Apertura(progress)·maxWidth. ANATOMÍA por segmento (~cada 48 px):
        ///   1. EL VACÍO: banda negra que OCLUYE (vuelco BrumaFX, grosor 0.62·w).
        ///   2. LAS ESTRELLAS: 16-28 motas dentro con scroll y parallax.
        ///   3. LOS LABIOS ×2 (arriba/abajo): velo ×1.6 α0.30 · cuerpo α0.60 ·
        ///      NÚCLEO blanco ×0.3 α0.90 (EstelaLib re-utilizado).
        ///   4. LA ABERRACIÓN: labios re-dibujados ±2 px con tinte de canal.
        /// La apertura sigue la curva de la casa Apertura(progress).
        /// </summary>
        /// <param name="batch">Batch ABIERTO (aditivo recomendado).</param>
        /// <param name="progress">0..1 vida del desgarro (0-0.08 apertura, 0.85-1 cierre).</param>
        /// <param name="maxWidth">Ancho MÁXIMO del desgarro abierto (8..16 px recomendado).</param>
        public static void Tear(SpriteBatch batch, Vector2 origin, Vector2 dir,
            float length, float progress, float maxWidth, Color[] paleta,
            float intensity, int seed, float time);

        /// <summary>
        /// LA ESTRELLA DE 4 PUNTAS del punto de ruptura (lección DoG): 2 draws de
        /// Star.png — vertical (1, 8)·charge·3.25 y rotada π/2 (1, 5)·charge·3.25 —
        /// + copia interior ×0.8 en el color de la paleta. `charge` 0..1.
        /// </summary>
        public static void Star(SpriteBatch batch, Vector2 center, float charge,
            Color[] paleta, float intensity, int seed, float time);

        /// <summary>
        /// EL PAQUETE DE IMPACTO del desgarro (3 líneas en el call-site):
        /// Tear + estrella + OndaLib.Kick(7, 12, perpendicular) + OndaLib.Flash
        /// (tinte de paleta, 0.22, 8) + ráfaga de chispas de anomalía.
        /// EL MOMENTO: se llama UNA VEZ, al abrir.
        /// </summary>
        public static void TearImpacto(Vector2 origin, Vector2 dir, float length,
            Color[] paleta, int seed);

        // ==================================================================
        //  LA GRIETA — la herida persistente (escuela B de daño)
        // ==================================================================

        /// <summary>
        /// CAMINO FRACTAL DE GRIETA (generador Lichtenberg, lección DoGRiftCrack):
        /// paseo con PERSISTENCIA de dirección, curvatura ACUMULADA
        /// (giro ±curvatura·i·0.25°) y micro-fallas (1/9 de salto). Determinista.
        /// </summary>
        /// <param name="points">8..25 puntos (24 recomendado).</param>
        /// <param name="pasoMin/pasoMax">25..50 px por punto (0.5× en el último).</param>
        /// <param name="curvatura">5° = vidrio; 9° = caos.</param>
        public static Vector2[] CaminoGrieta(Vector2 origin, Vector2 dir,
            int seed, int points = 24, float pasoMin = 25f, float pasoMax = 50f,
            float curvatura = 5f);

        /// <summary>
        /// DIBUJA LA GRIETA PERSISTENTE sobre el camino: ribbon con taper
        /// (MaxWidth→0 hacia la punta), RESPIRA ±8% (Breathe), labios con aberración,
        /// interior negro con estrellas fijas (la grieta NO scrollea: es una HERIDA),
        /// y chispas de anomalía opcionales ya dibujadas (máx 1 cada 3 ticks).
        /// </summary>
        public static void Grieta(SpriteBatch batch, Vector2[] camino, float progress,
            float maxWidth, Color[] paleta, float intensity, int seed, float time,
            bool chispas = true);

        // ==================================================================
        //  EL INTERIOR — el vacío que se ve dentro (re-utilizable solo)
        // ==================================================================

        /// <summary>
        /// EL VACÍO INTERIOR: banda que OCLUYE + N estrellas deterministas con
        /// PARALLAX (dx/depth, 0.9·dx/depth — lección DoG) y parpadeo por hash.
        /// `scroll` px/tick a lo largo del eje (el desgarro fluye; la grieta no).
        /// Se dibuja: vacío al lote no-premultiplicado (BrumaFX), estrellas al
        /// aditivo del llamador.
        /// </summary>
        public static void Interior(Vector2 origin, Vector2 dir, float length,
            float width, float progress, Color[] paleta, int seed, float time,
            float scroll = 2f, int estrellas = 22);

        // ==================================================================
        //  LAS PIEZAS SUELTAS (los quiere todo el mundo)
        // ==================================================================

        /// <summary>
        /// SHARDS DE CRISTAL: 6-10 esquirlas + 1 grande (números Calamity
        /// RealityRuptureStealth): aparecen con PolyOut(4), rotadas a la dirección
        /// ± jitter, se congelan 1 tick y mueren con alpha sin(π/2+t·π/2).
        /// Devuelve el paquete para el ParticleManager (la librería NO spawnea).
        /// </summary>
        public static void Shards(Vector2 origin, Vector2 dir, Color[] paleta,
            int seed, out ParticleData[] outParticles);

        /// <summary>
        /// CHISPAS DE ANOMALÍA: paquete determinista de motas (vel 8-14 radial,
        /// drag 0.96, color Lerp(paleta random, Blanco, 0.65) — lección DoG).
        /// </summary>
        public static void ChispasAnomalia(Vector2 center, int count, Color[] paleta,
            int seed, out ParticleData[] outParticles);

        /// <summary>
        /// EL ECO GLITCH (lección MEAC glur, sin render targets): el llamador
        /// re-dibuja su sprite/desgarro N=3 veces con offset horizontal alternado
        /// ±(2..3) px y tinte de CANAL PURO (R/B) con alpha 0.30·(1−i/3).
        /// Devuelve los offsets+tintes; el llamador dibuja.
        /// </summary>
        public static void EcoGlitch(int seed, float time, out Vector2[] offsets,
            out Color[] tintes);

        // ==================================================================
        //  LAS CURVAS (públicas: el contrato numérico)
        // ==================================================================

        /// <summary>
        /// Curva de APERTURA del desgarro: sube a 1 en los primeros 8% de la vida
        /// (equivalente MEAC de 0.333/tick ≈ 3 ticks) con ease-out cúbico, sostiene
        /// y cierra a −0.05·vida en el último 15% (MEAC). Clampeada 0..1.
        /// </summary>
        public static float Apertura(float progress);

        /// <summary>¿El desgarro DAÑA en este progress? (false en el último 10% — lección MEAC/WoTE).</summary>
        public static bool Daña(float progress);

        /// <summary>
        /// COLISIÓN DE LÍNEA DEL DESGARRO (escuela A): cápsula de radio
        /// 0.5·maxWidth a lo largo de origin→origin+dir·length. Listo para
        /// Colliding() del proyectil-bastón (paráfrasis AABBvLineCollision).
        /// </summary>
        public static bool LineaToca(Vector2 origin, Vector2 dir, float length,
            float width, Rectangle hitbox);
    }
}
```

## 3.2 Técnicas internas (parámetros concretos, los números del contrato)

### 3.2.1 El desgarro (Tear) — la línea de tiempo completa

| Fase | Duración | Qué pasa | Números |
|---|---|---|---|
| **Telegrafo** | 10-14 ticks | Estrella de ruptura crece (charge 0→1, ease-out), anillo implosiona `Lerp(maxR, 0, charge)`, 2-3 chispas/tick, sonido pitch −0.75→0 (DoG) | estrella ×3.25, anillo 90→0 px |
| **Apertura** | 3 ticks | Apertura(progress) 0→1 (0.333/tick MEAC), **Kick(7 px, 12 ticks, perpendicular)**, Flash(paleta, 0.22, 8), EcoGlitch 3 frames, primeras chispas de anomalía (12, vel 8-12) | labios pasan de 1 px a maxWidth |
| **Sostenido** | 26-32 ticks | Daño por línea activo (Daña()==true), interior scrollea 2 px/tick, estrellas parpadean, chispas 1/3 ticks, luz de mundo violeta/cian (0.6, 0.2, 0.9) a lo largo (StormLib.AddLightAlong patrón) | maxWidth 8-16 px |
| **Cierre** | 20 ticks | Apertura baja −0.05/tick; **el daño cesó 8 ticks ANTES** (Daña()=false desde el 90%); shards caen al 30% del cierre | eco glitch final ×1 |

**Geometría de la línea:** length 620 px recomendado para el bastón (≈ media pantalla;
MEAC usa 2000 px para el corte de jefe — el our es un ARMA). Segmentos:
`Math.Clamp((int)(length/48), 8, 24)`. Labios = 2 ribbons espejados con offset
perpendicular ±0.5·w (curva "lens": `sin(p·π)^0.6` — gorda al centro, aguja en las
puntas). Aberración: offset perpendicular ±2 px, grosor ×0.55, alpha ×0.45 (los números
exactos que ya usa OndaLib.Shock en sus franjas R/B — consistencia de casa).

### 3.2.2 El interior (qué se ve dentro — la respuesta de la casa)

1. **La banda que OCLUYE**: quad `BlackDisk.png` estirado (length+2·w × 0.62·w), lote
   no-premultiplicado (BrumaFX v2 — el vacío es negro aunque sea mediodía, lección DoG
   del respaldo negro). El grosor del vacío respira: `w·(0.94..1.06)` (Breathe 2.2).
2. **22 estrellas** (16-28): posición determinista por `Hash01(seed, i, j)` dentro de la
   banda, tamaño 1-3 px (GlowOrb, núcleo sólido), color = paleta[3] (blanco-cian) o
   blanco puro, **parallax**: `x_eff = x·(1/depth)`, `y_eff = y·(0.9/depth)` con
   depth = 30 fijo (DoG) — y **scroll**: `x = (x_base − time·scroll) mod length`
   (MEAC −2 px/tick) — el espacio interior FLUYE hacia un lado: el vacío "tira".
3. **El velo del borde**: SoftGlow ×1.6 en cada labio con alpha 0.18·intensity — el
   "bloom de la herida" que integra el negro con el mundo.
4. **Opcional "profundidad"**: 4-6 estrellas MÁS grandes (3-5 px) con depth 60 → se
   mueven a mitad de velocidad = sensación de caverna infinita (parallax por capas).

### 3.2.3 La grieta persistente (Grieta) — la zona de daño que respira

- **Camino**: CaminoGrieta con 24 puntos, pasos 25-50 px (longitud resultante
  ~700-900 px), curvatura 5°. El camino se genera UNA vez (el llamador lo guarda en
  ai[] locales o lo recalcula con la MISMA semilla).
- **Anchura**: `Lerp(maxWidth, 0, completion)` — gorda en la raíz (donde se originó el
  desgarro), aguja en la punta (DoGRiftCrack). maxWidth 10-14 px.
- **Respira**: `Breathe(time, 2.2, seed)` ±8% en la anchura; y el INTERIOR no scrollea
  (es una herida abierta, no una boca tragando).
- **Chispas de anomalía**: máx 1 cada 3 ticks en un punto aleatorio-del-camino
  determinista: mota 2-4 px, vel radial 8-14, drag 0.96, vida 30-45, color
  `Lerp(paleta[hash], Blanco, 0.65)` (DoG exacto), `AddLight` proporcional a vida
  (Everglow).
- **Daño (escuela B)**: el proyectil-grieta hace DoT con `usesLocalNPCImmity = true,
  localNPCHitCooldown = 15` dentro de la CÁPSULA del camino (LineaToca por segmento).
- **Vida**: 5-8 s (300-480 ticks), alpha final con falloff Slow (OndaFalloff).

### 3.2.4 El bastón (el arma del usuario) — receta de ensamblado

**"Bastón del Desgarro Primordial"** (magic, tier Eclipse/vacío):
1. **Uso: mantener para cargar** (Telegrafo): la estrella de ruptura crece en la punta
   del bastón (RiftLib.Star, charge 0→1 en 30 ticks), anillo que implosiona, el mundo
   se oscurece sutilmente (T11, FillProgress hasta 0.35).
2. **Soltar**: nace el proyectil DESGARRO (tileCollide=false, **atraviesa paredes** —
   es un desgarro del ESPACIO, no un objeto): Apertura→Sostenido con daño por línea
   (escuela A) mientras "corta" — 1 tick de invulnerabilidad local por NPC.
3. **Al final del Sostenido**: el desgarro SE CONVIERTE en la GRIETA persistente en el
   mismo lugar (el proyectil muta su AI: camino = CaminoGrieta desde el centro del
   desgarro) → DoT 15 ticks a quien toque la cápsula (escuela B).
4. **Cierre**: shards + eco glitch + Flash suave. El mundo recupera luz en ~1 s.
5. **Números de arma**: mana 18, daño base ~90 (escalado tier), cooldown de uso 45
   ticks, knockback 2 (el vacío apenas empuja: succiona — opcional: atrae 0.3 px/tick²
   a NPCs en un radio de 120 px alrededor de la línea durante el Sostenido, el
   "empuje negativo" del desgarro).

### 3.2.5 Efectos de pantalla (fases)

- **Fase 1 (sin tocar la lente — incluida en el contrato):** Kick + Flash + EcoGlitch
  (3 draws con offset/canal) + oscurecer por ModifySunLightColor (T11). CERO render
  targets nuevos.
- **Fase 2 (opcional, requiere tocar BlackHoleLensSystem):** fuente-LÍNEA en la lente:
  el shader de la casa recibe (punto, dir, halfWidth, fuerza) y desplaza el mundo
  perpendicular a la línea ±2% UV a cada lado (la matemática MEAC SCCut re-implementada
  en NUESTRO shader — la lección 1.2.2). El desgarro de jefe final partiría la
  pantalla de verdad. **Decisión explícita: NO hacerlo ahora** (la lente es el sistema
  más delicado del mod — v5.89-v5.91 enseñaron el precio); documentado para v6.3+.

### 3.2.6 Presupuesto de rendimiento (la casa mide quads)

| Efecto | Quads | Partículas | ¿Aceptable? |
|---|---|---|---|
| Tear (1 desgarro) | ~96 (24 seg × 3 capas × 2 labios −LOD) + 22 estrellas + 4 estrella/vacío | 12-24 chispas | SÍ (un agujero negro gasta más) |
| Grieta persistente (1) | ~72 (24 seg × 3 capas) + 18 estrellas | 1 chispa/3 ticks | SÍ |
| 3 desgarros simultáneos (multicast) | ~300 | ~72 | SÍ — con LOD: >2 desgarros → capas 2, >3 → 1 (lección EstelaLib) |

Determinismo: TODO por `Hash01(seed, a, b)` — cero `Main.rand` (regla de la casa).
Cero alocaciones por frame: buffer de estrellas re-utilizable (static, tamaño fijo 32).
Cero estado de desgarros en la librería (progress/fase = del llamador).

## 3.3 Casos de uso en AethonMod (más allá del bastón)

1. **El bastón del usuario** (3.2.4) — el caso original.
2. **Muerte del Eclipse Primordial**: el desgarro del cierre (Tear en la dirección del
   golpe final + grieta que persiste 4 s mientras el sol se apaga) — sustituye/completa
   la onda actual.
3. **Los agujeros negros**: la "muerte" de un agujero supremo puede DESGARRAR al morir
   (escuela C): Tear radial + 25 cracks Lichtenberg a i·2π/25 (los números exactos de
   la explosión DoG).
4. **Telegrafía de jefes futuros**: la grieta persistente como marcador de "aquí va a
   pasar algo" (escuela D — la lección de Nameless Deity: el terror es la telegrafía).
5. **Armas de vacío existentes**: el "Olvido" gana chispas de anomalía y eco glitch en
   sus golpes (2 líneas por call-site).

## 3.4 Riesgos y mitigaciones

| Riesgo | Prob. | Mitigación |
|---|---|---|
| El vacío no-premultiplicado tapa MAL (bordes duros sobre el mundo) | media | Grosor 0.62·w con SoftGlow encima (3.2.2-1); ya lo validamos en BrumaFX v6.25 (el fix del rectángulo) |
| La aberración ×3 duplica el coste de los labios | alta (por diseño) | LOD: aberración solo en Sostenido (no en Telegrafo/Cierre) y solo si intensity > 0.5 |
| La estrella de 4 puntas se lee "sprite plano" | media | 2 copias rotadas + respiración 3.25·(1±0.06) + color interior ×0.8 (DoG lo resuelve así) |
| El DoT de la grieta invisible (jugador no ve la zona) | media | Chispas SIEMPRE activas + el vacío oscuro ES el área: la cápsula de daño = grosor visual +6 px (franja de gracia) |
| ModifySunLightColor pelea con otros mods | baja | Solo activo mientras hay desgarros (contador estático), decaimiento ±0.05/tick (DoG) |
| Fase 2 (lente) rompe la pantalla | — | NO SE HACE en v6.26 (decisión 3.2.5) |
| Griefing estético con muchas grietas | baja | Cap global de grietas persistentes simultáneas = 4 (más antiguas cierran) |

## 3.5 Estimación

| Pieza | LOC | Notas |
|---|---|---|
| `RiftLib.cs` (Tear + Star + Interior + curvas + LineaToca) | ~340 | re-utiliza motor EstelaLib (SegQuad privado → exponer o duplicar 20 L) |
| `CaminoGrieta` + `Grieta` | ~160 | ZigPath-adjacent; curvatura acumulada nueva |
| `Shards` + `ChispasAnomalia` + `EcoGlitch` | ~120 | paquetes ParticleData (patrón PyraLib.Sparks) |
| **Total RiftLib** | **~620 L** | entre EstelaLib (456) y StormLib (628) — tamaño de librería de la casa |
| Bastón + proyectil + renderer + item + loc + icono | ~600 L | trabajo del agente principal (NO de este informe) |

---

# BIBLIOGRAFÍA Y VERIFICABILIDAD

**Archivos leídos línea a línea (locales):**
- MEAC: `decompiled/MEAC/Projectiles/SCCut.cs`, `MEAC/MEAC.cs` (L439-570, L817-905),
  `MEAC/Entities/{VisualEffect,LineWarp,LineWarp2,RingWarp,Wave_Warp}.cs`
- WoTE: `Content/NPCs/EoL/Projectiles/{RainbowRiftArrow,PrismaticBurst}.cs`,
  `Content/Particles/Metaballs/{DistortionMetaball,DistortionMetaballSystem}.cs`,
  `Content/NPCs/EoL/Behaviors/Attacks/Phase2/EmpressOfLight.EventideLances.cs`
- Everglow: `Sources/Modules/Yggdrasil/YggdrasilTown/VFXs/RockPortal.cs`
- Vanilla: `terraria_src/Main.cs` (portales: L42400-42560, L12900-12990),
  `terraria_src/Projectile.cs` (refs PortalHelper)
- Casa: `Content/VFX/{VFXCore,EstelaLib,OndaLib,PyraLib,StormLib,LumenLib}.cs`,
  `Content/VFX/OndaSystem.cs`, `Content/Effects/BlackHoleLensSystem.cs`,
  `Content/Effects/Bruma/BrumaFX.cs`, `Content/Particles/ParticlePresets.cs`,
  `Content/Effects/Procedural/*.png` (dimensiones verificadas con PIL)

**Repos públicos clonados a /tmp/repos/ (esta sesión):**
- `CalamityModPublic` (CalamityTeam) — `DoGTeleportRift.cs`, `DoGRiftCrack.cs`,
  `DoGRealityCrackShader.fx`, `DoGDistortionMetaball.cs`, `BigRipMetaball.cs`,
  `Skies/DoGSky.cs`, `Systems/Graphic/DoGVisualsManager.cs`,
  `Items/Weapons/Rogue/RealityRupture.cs`, `Projectiles/Rogue/RealityRupture{Stealth,Lance,Mini}.cs`,
  `Particles/{CrackParticle,TechyHoloysquareParticle}.cs`,
  `Utilities/DrawingUtils.cs` (L794-825), `Projectiles/Melee/ArkOfTheCosmos_BlastAttack.cs`,
  `NPCs/DevourerofGods/DevourerofGodsHead.cs` (L2185-2255), `Graphics/Metaballs/ScalArenaMetaball.cs`
- `ProjectStarlight/StarlightRiver` — grep Rift/Reality/Glitch: NEGATIVO (solo tiles)

**Búsquedas web (18 JSONs en `busquedas_web_42b/`):**
01 Stellamod Nameless · 02 SCal sunder · 03 rift weapons (Reality Rupture wiki) ·
04 Genshin rift · 05 glitch VFX (glitchology) · 06 Zelda malice · 07 Hollow Knight ·
08 Doom · 09 shards · 10 Lichtenberg · 11 Stars Above · 12 WoTG Noxus ·
13 Genshin void · 14 Wikszilla (sin hallazgos concretos) · 15 Starlight repo (URL
correcta) · 16 Fargo Eridanus · 17 glitch bands (agatedragon/godotshaders) ·
18 Nameless attacks (wiki: "Reality Shatter Punches").

**Informes previos re-leídos:** `humo_v625/ANALISIS_HUECOS.md` (formato de contratos),
`humo_v625/INFORME_MODS_HUMO.md`, contexto de worklog.md Tasks 38-41.

---

# RESUMEN EJECUTIVO (para el agente principal)

1. **La técnica #1 está en casa de un chino**: MEAC `SCCut` — un proyectil cuyo hitbox
   es una LÍNEA de 2000 px, con textura de interior que FLUYE (scroll), y un shader que
   PARTE LA PANTALLA en dos mitades desplazadas perpendicular al corte (±2% UV).
   El daño cesa 8 ticks antes de morir el visual. → RiftLib.Tear + LineaToca.
2. **La anatomía visual de élite es de Calamity DoG**: estrella de 4 puntas (1,8)/(1,5)×3.25
   + textura de cristal roto ×3 rotaciones con shader de erosión + anillo que IMPLOSIONA
   + grietas Lichtenberg procedurales (paseo con curvatura acumulada ±5°·i·0.25) +
   interior = negro + nubes + relámpagos con paralaje X≠Y + EL MUNDO SE OSCURECE.
   → RiftLib.Star + CaminoGrieta + Interior + ModifySunLightColor.
3. **La aberración cromática es 3 DRAWS sin shader** (Calamity "Thanks spirit <3"):
   tinte de canal puro + offset perpendicular. → los labios R/B del desgarro.
4. **El arma de referencia es Calamity Reality Rupture** (rogue post-ML, 420 dmg) y la
   mecánica de referencia es Nameless Deity "Reality Shatter Punches" (golpe que deja
   un desgarro DAÑANTE de corta vida) — exactamente la petición del usuario.
5. **RiftLib: ~620 L**, 100% SpriteBatch + texturas existentes (¡ya hay Star.png y
   BlackDisk.png!), cero RT nuevos en fase 1, lente intacta, todo determinista.
   Fase 2 documentada (fuente-línea en la lente) y aplazada a propósito.

---

# ADDENDA — REINTENTO 42-b (verificación del agente de reintentos)

**Estado:** reintento disparado tras un fallo de entrega del intento original. Los
entregables ya existían y estaban COMPLETOS; el reintentor los ha VERIFICADO y
AMPLIADO en lugar de rehacerlos:

- Re-ejecutados los greps de verificación sobre `/tmp/research/` (patrones
  Rift/Reality/Tear/Crack/Glitch/Distort, case-insensitive): confirmados
  `RainbowRiftArrow` + `DistortionMetaball(System)` (WoTE/Luminance — pantalla
distorsionada por máscara de metaballs, respeta `screenZoom`, filtro SOLO activo
  si hay partículas), `VortexVanquisher` (arma-vórtice MEAC), `GoldenCrack` y
  `LanternCrackingRay` (Everglow — ver §1.4bis, técnicas NUEVAS añadidas en este
  reintento). lunarveil/lunarveil2 (Stellamod local) siguen SIN contenido de
  desgarro (versión antigua — coherente con lo documentado en §1.6).
- Verificado el archivo del contrato contra las librerías reales de la casa
  (VFXCore.cs 273 L: GlowQuad/Begin/Quad/FlushAdditive/AppendToPlayerDraw/
  Breathe/Sway/Hash01/Ellipse; StormLib/OndaLib/EstelaLib/LumenLib presentes;
  BlackHoleLensSystem.cs 816 L con 2 RT). El contrato de RiftLib usa el convenio
  "dibuja en el batch ABIERTO del llamador" (mismo que StormLib/PyraLib) — válido.
- Las 18 búsquedas web del intento original están guardadas y son correctas
  (`busquedas_web_42b/01…18_*.json`, 18/18 presentes). No se gastaron búsquedas
  nuevas del presupuesto: la cobertura pedida (SCal sunder, Nameless/Stars Above,
  rift weapons, glitch spritebatch, reality crack, Fargo) ya estaba completa.
- Conclusión del reintento: INFORME COMPLETO Y VERIFICADO, con 2 técnicas nuevas
  (árbol-de-grieta con ramas de signo heredado + taper 10%/segmento; parpadeo
  18 Hz ±45%; eco por re-dibujo N veces; PostDrawBG = "detrás de la realidad")
  integradas en §1.4bis y reflejadas en las lecciones del contrato.
