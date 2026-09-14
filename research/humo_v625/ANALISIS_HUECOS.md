# ANÁLISIS DE HUECOS DE LIBRERÍAS VFX — v6.25
## Task 41-b · Agente: general-purpose (análisis de huecos de librerías)

**Mandato del usuario:** "investiga a fondo todos los mods populares, al menos 20, y crea
librerías que creas que nos falten y mejora la librería para humo, bruma, niebla".
**Este documento es la FASE 1: análisis y diseño puro. NO se implementa nada aquí.**

### Método y fuentes

| Tipo | Fuentes |
|---|---|
| **Código local descompilado (lectura línea a línea)** | WoTE v1.0.3 (+ framework Luminance), Coralite (+ lib InnoVault), Everglow (VFXBatch + CommonVFXDusts + HeatMaps), Lunar Veil (Particles/Primitives/ScreenTarget/Waters), MEAC (253 archivos ILSpy + shaders .xnb), Terraria vanilla 1.4.4.9 (Main 84k líneas, Dust, Projectile, DelegateMethods) |
| **Informes previos del proyecto** | `research/smoke_research_v616/INFORME.md` (Calamity + Starlight River + ParticleLibrary + vanilla), `research/luz_v622/` (WoTE + MEAC + Emperatriz vanilla), `research/storm_v621/INFORME.md` (Coralite + Everglow + WoTE + LunarVeil) |
| **Búsquedas web (16 consultas, JSONs en `busquedas_web/`)** | Calamity, Thorium, Starlight River, Fargo's Souls, Spirit Reforged, Redemption, Shadows of Abaddon, The Stars Above/Stellamod, Elements Awoken, Ancients Awakened, Polarities, Aequus, Verdant, Terraria Overhaul, ParticleLibrary, Doom Fire (fabiensanglard.net), VFX+, tML docs/ExampleMod |
| **Inventario propio (leído completo)** | VFXCore.cs (272 L), LumenLib.cs (493 L), StormLib.cs (628 L), VFXPalettes.cs (144 L), BrumaFX/Noise/Brushes/System (707 L) **+ el sistema que el inventario olvidó**: `Content/Particles/` (ParticleManager/ParticleData/ParticleBuffer/ParticlePresets ≈ 1200 L) y `BlackHoleLensSystem.cs` (817 L) |

> REGLA DE HONESTIDAD: todo el código ajeno se cita como **pseudocódigo parafraseado
> propio**; ninguna línea se copia. Los números concretos (radios, factores, Hz) sí se
> citan porque son datos, no expresión creativa.

---

# PARTE 1 — QUÉ TIENEN LOS MODS POPULARES QUE NOSOTROS NO

## 1.1 Sistemas de partículas (pools, emisores, física, muerte)

| Mod | Arquitectura | Detalles medidos | Rendimiento |
|---|---|---|---|
| **Calamity** | Lista global + `Particle` base + presets tipados | Flipbooks PNG (`HeavySmoke`: 7 variantes × 6 frames de 80×80); **3 lotes de blending en orden fijo** (aditivo/alfa/non-premult); curvas de escala/opacidad por tipo; **cap configurable** en el mod config ("Sets the maximum of particle effects… Turn down to improve performance") | CPU; muy estable, cap configurable por el usuario |
| **Starlight River** | **Partículas por VERTEX BUFFER en GPU + ~30 shaders** (el sistema de partículas más premiado de tML) | Cada sistema de partículas escribe vértices a un buffer dinámico; shaders por tipo | El techo de rendimiento del ecosistema; PERO usuarios reportan fugas de memoria/costes en Steam ("severe performance issues and memory leaks" — comentario de Steam) |
| **Lunar Veil / Stellamod (linaje)** | `ParticleSystem` (ModSystem) con **cap 500**, 2 listas (aditiva + "negra"), hook `On_Main.DrawDust` | Update en `PostUpdateDusts`; kill si fuera de pantalla, `Scale < 0.001`, `fadeIn > 1000`; **shader por partícula** (`ArmorShaderData.Apply`, reabriendo el batch solo cuando cambia el shader); culling por pantalla | List-based, try/catch por partícula (robusto pero con GC por lista) |
| **Coralite (+ InnoVault)** | Sistema **PRT** ("Particle Render Type"): `PRTGroup` con pool, `PRTDrawModeEnum` (Additive/Alpha), partículas tipo `ModTexturedType` registradas en loader | `FireParticle`: flipbook 16 frames, `Frame.Y++` cada 5 ticks, `velocity *= 0.95`, `color *= 0.96`, doble dibujado (color + color×0.5) | Pooling real; el sistema gráfico vive en la lib InnoVault (pública) |
| **Everglow** | **`Visual` base + Pipelines encadenados** (`[Pipeline(typeof(FireSparkPipeline), typeof(BloomPipeline))]`) | Cada "dust" declara su shader + blend + sampler en un pipeline; `DrawLayer = PostDrawDusts`; física con **viento del mundo** (`Main.windSpeedCurrent * 0.4`), gravedad 0.01, `velocity *= 0.98`, `scale *= 0.995`, **muerte acelerada en agua** (consulta `tile.LiquidAmount`), rebote en sólido (×−0.2 + vida +10), `AddLight` proporcional a posesión de vida | Pipelines = estado de GPU agrupado por tipo → mínimos cambios de estado |
| **WoTE (+ Luminance)** | `Particle` base con `BlendState` virtual, atlas de texturas (`AtlasManager`), easings (`EasingCurves.Quadratic`) | `BloomCircleParticle`: `velocity *= 0.96`, escala por curva cuadrática Out, opacidad por curva In en el último 54% de vida, rotación = dirección de la velocidad; **doble draw integrado** (bloom trasero + núcleo) | Manager central con conteo; atlas reduce cambios de textura |
| **MEAC** | "Dusts" gestionados + shaders por tipo (Fire, Sun…) | Brillo por LUT, partículas full-bright (GetAlpha override) | — |
| **ParticleLibrary (SnowyStarfall)** | Lib GPL-3 independiente del juego de dusts; **la usan Redemption, Shadows of Abaddon y Lunar Veil** | API: spawn por tipo genérico + cap global | Diseño de referencia para "librería de partículas como dependencia" |
| **Vanilla Terraria** | Array fijo de dusts con atlas 8×8, 3 variantes por frame | **Autodegradación**: si el conteo supera 50–90% del máximo, el umbral de muerte sube de 0.001 a 0.02 (el juego se protege solo); trail dusts (130-134, 219-223, 226, 272, 278) dibujan hasta 10 copias en `pos − vel·j` con `scale·(1−j/10)` | El estándar de robustez ante sobrecarga |
| **Terraria Overhaul** | Sistema propio de partículas + clima + física | Partículas de suelo/impacto por material | Re-escritura del render entero; no portable |

**Conclusión 1.1:** la arquitectura de pooling con cap duro + lotes por blend + degradación
por presión YA la tenemos (`ParticleManager`: buffer plano de 4000 structs, componentes
bitmask, culling por cámara, 2 pases). Lo que nos falta de verdad: **presets curados de
calidad** (chispas/ascuas/escombros), **determinismo** (usa `Main.rand` → distinto en cada
máquina; la casa es determinista por semilla), **muerte contextual** (agua/sólido) y el
**componente de drag/fricción exponencial** (los demás lo tienen todos: 0.95–0.99/frame).

## 1.2 Trails / ribbons (estelas de proyectil con grosor variable)

| Mod | Técnica | Números |
|---|---|---|
| **Lunar Veil** | `TrailRenderer` (clase): ribbon **TriangleStrip con `VertexPositionColorTexture`**, delegados `GetWidth(float t)` / `GetColor(float t)`, textura de estela, sanitizado del path (`RemoveZerosAndDoOffset`: corta en NaN/ceros/teleports >1000px), matriz mundo por `CreateLookAt × Translation × RotationZ × Scale(zoom)` | `NewRenderer(type, width, color)` → anchura decrece `(1−p)` y color decae `(1−p)`; respeta gravedad invertida del jugador |
| **Starlight River** | Trails por vertex buffer + shaders (misma pila que sus partículas) | — |
| **Coralite/InnoVault** | Clase `Trail` en la lib + `TrailParticle` (`IDrawParticlePrimitive.DrawPrimitive()`) | Trails como PRIMITIVAS por partícula |
| **Everglow** | VFXBatch con vértices custom (8192 verts/buffer, TriangleStrip) + shaders de estela | — |
| **WoTE/Luminance** | `PrimitiveRenderer` + shaders de primitivas (`LanceWallTelegraphShader`, `ConvergingMoonlightShader`) | Telegraphs verticales de 4000px con falloff cuadrático |
| **MEAC** | Estela = **8 afterimages con squash**: copias a `−velocity·0.8·i`, alpha `(0.6 − i/15)`, escala Y aplastada (0.5 aliado / 1.2 hostil) → lee "rasguño de luz" | 8 copias, paso 0.8·vel |
| **WoTE** | LightLance: **30 copias fantasma del sprite** del proyectil | 30 draws |
| **Vanilla** | Láseres de escalera (Rainbow Rod/Turret/Lightning en `DelegateMethods`): un **strip repetitivo CPU** — cabeza (26×22), cuerpo (26×28 cada ~22–33px con avance de frame animado), cola (26×22), color por delegado | Rejilla de tiles de textura a lo largo del haz |
| **Vanilla (proyectiles)** | `oldPos/oldRot/oldSpriteDirection` documentados para "drawing trails" (docs tML) | Arrays de 10 por proyectil |

**Conclusión 1.2:** TODOS los mods de VFX serios tienen una forma de **ribbon con grosor
variable a lo largo del camino**. Nosotros solo tenemos (a) `LumenLib.LanceTrail`
(fantasmas rectos por dirección, sin camino) y (b) `StormLib.Strand` (¡la tecnología
YA existe en casa! dibuja banda con halo/núcleo/núcleo-blanco por sub-segmento sobre una
polilínea — pero está atada al look "rayo" y a las texturas Bolt). **El gap es de
EMPAQUETADO, no de motor.**

## 1.3 Shockwaves / ondas expansivas

| Mod | Técnica | Números |
|---|---|---|
| **WoTE** | `PrismaticBurst`: quad **full-screen** + `ShockwaveShader` (ps_2_0): distancia al centro con **offset de ruido** (ángulo = ruido×8 rad, magnitud 0.007 UV → el anillo no es círculo perfecto), brillo ∝ `opacityFactor / distancia / screenSize.x × 0.041`, disipación `smoothstep(0.01, 0.09)`; el radio hace `Lerp(Radius, IdealRadius, 0.075)` por frame | **Radio ideal 780px, vida 0.25 s (15 frames)**, opacidad por `InverseLerp(2,10,timeLeft)`; en el primer frame mete un metaball de distorsión de 30px + shake |
| **Calamity** | Anillos de partículas + "Flare" (dusts tipo explosión radial) + shockwaves de jefe en pantalla | — |
| **Vanilla** | Sin shockwave genérica; explosiones = dusts radiales + flash | — |
| **Aequus / otros** | Sistemas pequeños de "explosion rings" reutilizables por arma | — |

**Conclusión 1.3:** el estándar del ecosistema premium es **onda con borde roto por
ruido, falloff asimétrico delante/detrás, y acompañamiento (flash + shake + distorsión)**.
Nosotros tenemos `CosmicShockwaveProjectile` (5 estilos con lente + franjas R/B —
espectacular pero PESADO: es un proyectil, solo para muertes de jefes) y
`ParticlePresets.RingPulse` (un Ring con ScaleUp — correcto pero plano: un solo anillo,
sin grosor, sin borde roto, sin frente/retaguardia). **Falta la onda "de trabajo"**: la
que pueda soltar cualquier impacto de arma sin montar un proyectil.

## 1.4 Distorsión / heat haze / refracción (render targets + shaders)

| Mod | Técnica | Números |
|---|---|---|
| **WoTE/Luminance** | **Metaballs de distorsión**: partículas de metaball dibujadas (aditivo, `Matrix.Identity`, fijas a pantalla) a un **render target por capas**; luego un **filtro de pantalla** (`MetaballDistortionFilter`, ps_2_0) re-muestrea el mundo: `data = tex(metaball, (coords−0.5)/zoom+0.5)`; ángulo = `smoothstep(0.1,1,data.g)·2π`; **desplazamiento UV = dirección·data.a·0.0051** | El metaball decae `ExtraInfo[0] −= 0.02/frame`, crece `Size ×= 1+ExtraInfo[1]`, `Velocity.X ×= 0.97`; el filtro se activa SOLO si hay partículas activas (cero coste en reposo) |
| **MEAC** | Shaders de pantalla `Screen/EoLWarp`, `Screen/Cosmic` (warp de la Emperatriz) | — |
| **Lunar Veil** | `ScreenTarget` + handler: **render targets gestionados** con orden, resize y semáforo; hook en `On_Main.CheckMonoliths` (el momento correcto del frame: antes del dibujado del mundo) | RT con `PreserveContents`, `DepthFormat.None`; recreación en resize; 20 ticks de gracia tras entrar al mundo |
| **Everglow** | Pipelines de post-proceso encadenados (`BloomPipeline` tras el pipeline base) | — |
| **Terraria Overhaul** | Heat haze + refracción por material | — |
| **Vanilla** | `Main.screenShader` + `Terraria.Graphics.Effects.Filter` (filtros de pantalla por eventos: sandstorm, monolitos, luna de sangre) + pipeline de captura (`captureEntities/captureBackground` + shimmer) | — |

**Conclusión 1.4:** ya tenemos la distorsión MÁS avanzada del ecosistema
(`BlackHoleLensSystem`: pantalla completa, 2 pases, 5 fuentes, correcto frente a FNA).
El gap es que está **acoplada a los cósmicos** (pases A/B fijos: agujeros+ondas / sol
gigante). La "heat haze" de WoTE es más barata (solo re-muestrear donde hay metaballs)
pero requiere un 3er pase. **No es prioridad** — el coste de generalizar es medio-alto
y el beneficio se solapa con la lente.

## 1.5 Metaballs / fluidos fake

- **WoTE/Luminance**: sistema completo (`MetaballType` con `LayerTextures`, `LayerIsFixedToScreen`, `EdgeColor`, `DrawnManually`, instancias con `ExtraInfo[]` para física, `ShouldRender = ActiveParticleCount >= 1`). Se usa para el "hue" de la RainbowRiftArrow y estallidos.
- **Nadie más del ecosistema local** lo tiene; en el ecosistema general (Starlight River) hay fluidos por shader.
- **Nosotros:** NO (aunque la lente comparte la idea "máscara en RT + shader de pantalla").

**Conclusión 1.5:** metaballs = RT + additive + filtro. El 80% del valor visual se
obtiene ya con nuestra lente; el 20% restante (bordes que se FUSIONAN al solaparse) exige
`BlendState.Additive` a RT + umbral. **Diferido**: alto esfuerzo, caso de uso aún no
definido en AethonMod.

## 1.6 Fuego

| Mod | Técnica | Números |
|---|---|---|
| **Everglow** | **Fuego con LUT (heat map)**: vertex shader custom (VFX2D con noise-coords en COLOR) + pixel shader: `light = 1 − halo.r × noise.r × (1−t)`; `color = tex(uHeatMap, float2(light, 0))` — **una rampa 1D de 1×N px mapea "temperatura" a color** | HeatMaps/: 12+ LUTs PNG (fire, spark, curseFlame, frostFlame, crystal, blood, ichor, electric…) — UNA rampa por material |
| **Coralite** | **Flipbook** de llama (16 frames, sprite sheet vertical, frame++ cada 5 ticks) + doble dibujado | — |
| **Calamity** | Flipbooks de fuego + humo (variante × frames), 3 lotes | — |
| **Vanilla** | Dusts de fuego (Atlas 8×8, variantes), `Main.windSpeedCurrent` mueve las brasas | — |
| **Doom Fire (PSX, técnica clásica)** | **Campo de propagación**: rejilla W×H de "temperatura" 0..36; cada frame: `dst[x,y] = src[x−rand(0..3)+1, y+1] − 1` (sube, se enfría, vaga); **LUT de 37 colores** negro→rojo→amarillo→blanco | 37 entradas; nosotros lo implementamos en v6.22 como campo 26×38 interactivo (viento al correr, avivo, salto) — **y se perdió en la purga de envolturas de v6.24** |

**Conclusión 1.6:** hoy nuestro fuego = textura `FireRing.png` + color LumenLib + humo
BrumaFX. Las TRES técnicas del ecosistema (LUT-ramp, flipbook, campo de propagación) son
baratas y 100% compatibles con nuestra pila (el LUT es un quad estirado; el campo se
validó en v6.22; el flipbook ya lo propuso smoke_v616 §G.2 para la bruma). **Gap real y
barato.**

## 1.7 Lluvia / nieve / clima (partículas de pantalla)

- **Vanilla**: `DrawRain()` en el pipeline (después de tiles, antes de gore/dust); lluvia/nieve son partículas de pantalla con viento global.
- **Terraria Overhaul**: clima volumétrico + partículas por material.
- **Lunar Veil**: Skies custom (`CloudySky`, `DesertSky`) + Foreground/ParallaxHelper (capas en primer plano con paralaje).
- **Nosotros**: NO (solo `BrumaFX.MistBand` como niebla estática de escena).

**Conclusión 1.7:** la nieve/lluvia de pantalla es un caso restringido de sistema de
partículas con spawn en los bordes de pantalla + viento global. **Prioridad baja** (no hay
biomas/clima en la hoja de ruta cercana), pero `MistBand` ya cubre la mitad estética.

## 1.8 Efectos de pantalla completa (flash, viñeta, shake, aberración cromática)

| Mod | Técnica |
|---|---|
| **WoTE** | `OverlayEffects`: `EmpressPostProcessingShader`, `BlurUnderglowShader` (post-proceso de la Emperatriz); shake por offsets de cámara en AI |
| **MEAC** | Shaders de pantalla (`Screen/Cosmic`, `Screen/EoLWarp`) |
| **Terraria Overhaul** | Shake por capas con masa, viñeta, aberración |
| **Vanilla** | `Main.screenShader`/`Filters` (filtros de eventos) + `Main.screenPosition` shakes hardcodeados (p. ej. muertes de jefes) |
| **Nosotros** | Shake **ad-hoc en 3 proyectiles** (p. ej. Supernova: `Main.screenPosition += rand(−1,1)·shake·2.2`); aberración R/B solo dentro de CosmicShockwave; sin flash ni viñeta gestionados |

**Conclusión 1.8:** el "impacto de verdad" = onda + **sacudida** + **flash** +
**aberración**. El shake ad-hoc actual padece: (a) se aplica dentro de PreDraw (tras el
cálculo de cámara de vanilla → acumulación impredecible), (b) no tiene decay unificado,
(c) 3 implementaciones separadas. **Gap barato y de altísimo impacto percibido.**

## 1.9 Flipbooks / sprites animados procedurales

- **Calamity/Coralite/vanilla**: flipbooks PNG horneados por el artista.
- **smoke_research_v616 §G.2 (nosotros)**: propuesta de **flipbook fBm evolutivo horneado en runtime** (6–8 frames, offsets de dominio en potencias exactas de 2 → sin phasing) — **nunca se implementó**: `BrumaBrushes` hornea 8 variantes × 1 frame.
- **Nosotros hoy:** cero animación interna de textura; todo el movimiento es rotación/escala/deriva del quad.

**Conclusión 1.9:** el flipbook evolutivo es LA mejora pendiente de BrumaFX (el usuario
la pidió explícitamente: "mejora la librería para humo, bruma, niebla").

## 1.10 Otras capacidades descubiertas (sin categoría propia)

1. **Paletas por DATOS con prioridad+condición** (WoTE `EmpressPalettes`): 5 sets
   (Default/Día/Eclipse/LunaSangre/Enfurecida) elegidos automáticamente por estado del
   boss; `MulticolorLerp` cíclico por índice. → Nosotros: `LumenPalettes`+`VFXPalettes`
   equivalentes (SÍ, en paridad).
2. **Easings como ciudadano de primera clase** (WoTE `EasingCurves`, LunarVeil
   `EaseFunction/Easing`): catálogo curvo (Quadratic In/Out…) para escala/opacidad de
   partículas. → Nosotros: solo `Breathe/Sway` y pow-manual. **Mini-gap** (una docena de
   líneas, se puede doblar dentro de la lib que toque).
3. **Viento del mundo** (`Main.windSpeedCurrent`) alimentando partículas (Everglow:
   `velocity += wind×0.4`; vanilla: las mismas). → Nosotros: viento propio por senos
   (BrumaFX) pero NO acoplado al viento real del mundo. **Mini-gap** de realismo.
4. **Agua contextual** (Everglow: `tile.LiquidAmount` mata/acelera partículas). → Mini-gap.
5. **Atlas de texturas** (WoTE `AtlasManager`): reduce cambios de textura por lote. →
   Nosotros: `SpriteSortMode.Immediate` + texturas pocas → no necesario aún.
6. **Water addons** (Lunar Veil: 7 addons de agua por bioma) — fuera de alcance VFX puro.

---

# PARTE 2 — GAP ANALYSIS CONTRA NUESTRAS LIBRERÍAS

Inventario real (incluyendo lo olvidado del enunciado): **VFXCore** (quads+flush),
**LumenLib** (luz), **StormLib** (rayos), **BrumaFX** (humo), **VFXPalettes** (color),
**ParticleManager** (partículas data-oriented, 4000 cap), **BlackHoleLensSystem**
(distorsión de pantalla), **CosmicShockwaveProjectile** (ondas de jefe).

| # | Capacidad | Quién la tiene (top) | ¿La tenemos? | Calidad relativa nuestra | Esfuerzo de construir | Prioridad |
|---|---|---|---|---|---|---|
| 1 | Partículas con pooling/cap/física | Calamity, Starlight, LunarVeil, Coralite, Everglow, ParticleLibrary | **SÍ** (ParticleManager: 4000, bitmask, culling, 2 lotes) | Media-alta en motor; **baja en presets/determinismo** (Main.rand, sin drag, sin muerte por agua/tile) | Extensión: bajo (~150 L) | MEDIA |
| 2 | Presets curados (chispas/ascuas/escombros) | Todos | PARCIAL (4 presets genéricos) | Baja | Bajo | MEDIA (via extensión) |
| 3 | **Ribbon trail con grosor variable** | LunarVeil, Starlight, Coralite, Everglow, vanilla-láser | **NO** (solo fantasmas LanceTrail + Strand de rayo) | Baja | **Medio (~450 L)** | **ALTA** |
| 4 | Afterimages con squash | MEAC (8, squash Y), WoTE (30) | PARCIAL (LanceTrail: escala creciente ×1.4) | Media | Bajo (mejora) | MEDIA (se dobla en #3) |
| 5 | **Onda expansiva ligera reutilizable** | WoTE (shader full-screen) | PARCIAL (CosmicShockwave = proyectil de jefe; RingPulse = 1 anillo plano) | Media en jefes / **nula en impacto de arma** | **Bajo (~300 L)** | **ALTA** |
| 6 | Shake/flash/aberración gestionados | WoTE, MEAC, Overhaul, vanilla | PARCIAL (shake ad-hoc ×3; aberración solo en shockwave) | Baja | Bajo (~120 L, dentro de la lib de ondas) | MEDIA-ALTA |
| 7 | **Fuego (LUT + campo + lenguas)** | Everglow (LUT), Coralite (flipbook), Doom Fire (campo) | **NO** (FireRing estático; campo v6.22 PERDIDO) | Baja | **Medio (~420 L)** | **ALTA** |
| 8 | Flipbook evolutivo en runtime | (propuesta propia v616) | NO | — | Bajo (~120 L sobre BrumaBrushes) | MEDIA (mejora BrumaFX) |
| 9 | Humo/niebla | Calamity (flipbook), Everglow (shaders) | **SÍ** (BrumaFX: mejor del ecosistema local en procedural puro) | **Alta** | — | — (mejoras puntuales) |
| 10 | Distorsión de pantalla | WoTE (metaballs), MEAC (warp) | **SÍ** (BlackHoleLensSystem: superior pero acoplada) | Alta (acoplada) | Generalizar: medio-alto | BAJA (diferir) |
| 11 | Metaballs | WoTE | NO | — | Alto | BAJA |
| 12 | Clima de pantalla | Vanilla, Overhaul | NO | — | Medio | BAJA |
| 13 | Bloom apilado / luz emitida | Todos (shader) | SÍ (LumenLib) | Alta | — | — |
| 14 | Rayos | Coralite/Everglow/WoTE | SÍ (StormLib) | Alta | — | — |
| 15 | Paletas cíclicas + LUT de color | WoTE, MEAC | SÍ (LumenPalettes/VFXPalettes) | Alta | — | — |
| 16 | Easings curados | WoTE, LunarVeil | PARCIAL (pow manual) | Media-baja | Mini (~30 L) | Empaquetar con la lib que toque |
| 17 | Luz de mundo muestreada | Todos (limitada) | SÍ (LightAlong) | Alta | — | — |

**Síntesis del gap:** el proyecto tiene el "peso pesado" cubierto (luz, rayos, humo,
distorsión de lente). Los huecos son las **capas de "impacto"** — lo que pasa en el
frame en el que algo GOLPEA (estela → onda → shake/flash) y el **fuego** — más una serie
de mini-gaps de "vida contextual" (drag, agua, viento del mundo, easings) que pertenecen
a la extensión de ParticleManager.

---

# PARTE 3 — RECOMENDACIÓN CONCRETA

## 3.0 Metodología

Impacto visual × esfuerzo razonable, con la regla de la casa: **todo determinista por
semilla, cero GC por frame, contrato "batch abierto no se toca", texturas procedurales**.

Recomendadas (en orden): **EstelaLib → PyraLib → OndaLib**.
Descartadas razonadamente: ChispaLib (ver 3.4), distorsión generalizada (3.5), metaballs
y clima (3.6).

---

## 3.1 ESTELA LIB — la librería de las estelas (`Content/VFX/EstelaLib.cs`)

### Qué es
La librería de ESTELAS de movimiento: ribbons de grosor variable que siguen el CAMINO
real de una entidad (polilínea suavizada), con perfiles de anchura, doble pasada de la
casa (color fuera, corazón blanco dentro), fantasmas con squash (MEAC) y estelas de
polvo. Re-utiliza la tecnología ya probada de `StormLib.Strand` (banda por sub-segmento
con textura propia + halo/cuerpo/núcleo) desacoplándola del look "rayo".

### Contrato de diseño (API pública, estilo de la casa)

```csharp
namespace AethonMod.Content.VFX
{
    /// <summary>Perfil de anchura a lo largo de la estela (0=nacimiento, 1=cabeza).</summary>
    public enum EstelaProfile
    {
        /// <summary>Fina al nacer, GRUESA en la cabeza (la lanza en vuelo).</summary>
        Head,
        /// <summary>Gruesa al centro, fina en ambos extremos (el arco de cometa).</summary>
        Center,
        /// <summary>Cola que MUERE hacia atrás: máx en la cabeza, decae cúbico.</summary>
        Comet,
        /// <summary>Anchura viva por ruido (la estela que respira).</summary>
        Alive,
    }

    /// <summary>
    /// EstelaLib — v6.25 — LA LIBRERÍA DE LAS ESTELAS.
    ///
    /// CONTRATO (idéntico al de StormLib/LumenLib): los métodos de DIBUJO reciben el
    /// batch ABIERTO en modo aditivo y NO lo tocan — se pueden anidar dentro de un
    /// renderer mayor. Coordenadas tal cual lleguen. TODO determinista por semilla.
    ///
    /// EL CAMINO ES DEL LLAMADOR: la librería no guarda historia; acepta la polilínea
    /// que el proyectil ya tiene (oldPos) o el ring-buffer propio (EstelaTrack).
    /// </summary>
    public static class EstelaLib
    {
        // ============ EL TRACK (historia del camino, opcional) ============

        /// <summary>
        /// Ring-buffer de puntos por IDENTIDAD (whoAmI del proyectil): Push cada
        /// tick en AI(); el track se auto-limpia si no se empuja en 2 ticks
        /// (teleports y muertes no dejan estelas fantasma).
        /// </summary>
        public static EstelaTrack Track(int ownerId, int capacity = 24);

        // ============ EL RIBBON (el corazón de la librería) ============

        /// <summary>
        /// DIBUJA LA ESTELA: banda con GROSOR VARIABLE a lo largo de la polilínea
        /// (remuestreada cada ~14 px y suavizada), TRES capas por tramo: velo de
        /// color ×1.6 (alpha 0.30) · cuerpo de color ×1.0 (alpha 0.60) · NÚCLEO
        /// blanco ×0.30 (alpha 0.90) — la doble pasada de la casa, en versión
        /// triple. La punta final puede llevar CABEZA (bloom pequeño).
        /// </summary>
        /// <param name="pts">Camino (2..n puntos, coords tal cual del lote).</param>
        /// <param name="width">Ancho MÁXIMO de la estela en px.</param>
        /// <param name="profile">Curva de anchura a lo largo del camino.</param>
        /// <param name="color">Color del cuerpo (el núcleo vira a blanco).</param>
        /// <param name="intensity">Multiplicador global 0..1.</param>
        /// <param name="head">True = dibuja cabeza brillante en pts[0].</param>
        public static void Ribbon(SpriteBatch batch, Vector2[] pts, float width,
            EstelaProfile profile, Color color, float intensity, int seed,
            float time, bool head = true);

        /// <summary>Versión con ANCHURA por delegado (el CONTRATO del taper).</summary>
        public static void Ribbon(SpriteBatch batch, Vector2[] pts,
            Func<float, float> widthAt, Func<float, Color> colorAt,
            float intensity, int seed, float time);

        // ============ LOS FANTASMAS (afterimages con squash, lección MEAC) ============

        /// <summary>
        /// FANTASMAS: N copias del SPRITE que el llamador dibuje, posicionadas a
        /// lo largo del camino con alpha decreciente y SQUASH vertical (Y ×0.55):
        /// leen "rasguño de luz", no "cola de sprites".
        /// Devuelve la transform del fantasma i para que el llamador dibuje su
        /// sprite con ella (la librería no conoce el sprite).
        /// </summary>
        public static void GhostTransforms(Vector2[] pts, int ghosts, float squashY,
            out Vector2[] positions, out float[] alphas, out float[] scales);

        // ============ LA ESTELA DE POLVO (la firma de "algo pasó por aquí") ============

        /// <summary>
        /// ESTELA DE POLVO: emisión decorativa DETERMINISTA (chispas que caen con
        /// drag + motas que flotan) a lo largo del camino, lista para volcarse al
        /// ParticleManager (la librería NO spawnea: devuelve el paquete).
        /// </summary>
        public static void DustTrail(Vector2[] pts, Color color, int seed,
            float rate, out ParticleData[] outParticles);

        // ============ HELPERS DE CAMINO (públicos: los quiere todo el mundo) ============

        /// <summary>Remuestreo por longitud de arco cada step px (denso y uniforme).</summary>
        public static Vector2[] Resample(Vector2[] pts, float step = 14f);

        /// <summary>Suavizado por promedio móvil 0.25/0.5/0.25, iterations veces.</summary>
        public static Vector2[] Smooth(Vector2[] pts, int iterations = 2);

        /// <summary>Sanitizado: corta en NaN/cero/teleports &gt; 1000 px (lección LunarVeil).</summary>
        public static Vector2[] Sanitize(Vector2[] pts);
    }
}
```

### Técnicas internas (parámetros concretos)

1. **Remuestreo por longitud de arco** cada **14 px** (paso ≈ media anchura mínima
   visible; menos = bolas, más = esquinas), reutilizando `PuntoEnRuta` de BrumaFX
   (misma matemática, se puede extraer a VFXCore).
2. **Suavizado** 2 iteraciones de kernel (0.25, 0.5, 0.25) — el camino de `oldPos`
   de tML es una escalera de ticks; sin esto toda estela "rasegura".
3. **Anchura por perfil** (contrato del taper de StormLib):
   - `Head`: `w(t) = pow(sin(t·π/2), 0.7)` (fina→gruesa, sin golpe seco en la cabeza);
   - `Center`: `sin(t·π)^0.8` (la actual de StormLib, generalizada);
   - `Comet`: `(1−t)^1.5` (muerte cúbica hacia atrás);
   - `Alive`: base × (0.75 + 0.25·`BrumaNoise.Fbm(t·3 + time·0.31, seed, 2)`) — la
     estela respira con las DOS frecuencias de la casa (0.31/0.71 Hz inconmensurables).
4. **Triple capa por tramo** (hereda los números medidos de StormLib): velo ×1.6 ancho
   alpha 0.30 · cuerpo ×1.0 alpha 0.60 · núcleo blanco ×0.30 alpha 0.90, con la textura
   **`TrailGlow.png` existente** (degradada 32×8) para el cuerpo y `SoftGlow` para el
   velo; el núcleo con `BoltCore` NO (demasiado "rayo") — usar `TrailGlow` estirada.
5. **Rotación por tangente** (ya existe: `VFXCore.Quad(pos, color, scale, rot, tex)` v6.08
   "cintas de luz orientadas por la tangente" — ¡la infraestructura se anunció para esto
   mismo y nunca tuvo librería!).
6. **Fantasmas con squash**: N=6–8, alpha `(0.6 − i/15)` (MEAC), escala X ×1.0→×1.4
   creciente hacia atrás (Emperatriz vanilla, ya en `LanceTrail`), squash Y ×0.55.
7. **Cabeza**: `LumenLib.Bloom` (2 capas) en `pts[0]` — integra las dos librerías.

### Casos de uso en AethonMod

- **Lanza del Alba** (v6.24): su estela actual es fantasmas rectos → Ribbon `Head` con
  paleta SolarGold + fantasmas squash.
- **Tormenta Nebular**: ribbon `Alive` teal + `DustTrail` de motas.
- **Cometas/alas Cometa** (mock v613): estela `Comet` (es la firma del ala).
- **Eclipse Primordial / agujeros**: el disco de acrección podría llevar una estela
  `Center` tenue al final del doppler.
- **Proyectiles V20 genéricos**: hoy solo dusts vanilla; un `Ribbon` de 3 capas los
  sube de tier visual sin tocar AI.

### Riesgos y mitigaciones

| Riesgo | Mitigación |
|---|---|
| Coste por tramo (3 draws × ~30 tramos) | Presupuesto: tramos = `clamp(len/14, 4, 32)`; LOD: si `width < 6px` → 2 capas; si `width < 3px` → 1 capa núcleo |
| `oldPos` sucios (teleports, spawn) | `Sanitize()` obligatorio (lección LunarVeil: cortar en >1000px/NaN/ceros) |
| Estela visible en pausa/menú | Dibujar solo si `!Main.gamePaused` y el dueño sigue activo; el `EstelaTrack` se pudre en 2 ticks |
| Tirones al girar 180° | El suavizado + remuestreo disimula; opcional "twirl" del ángulo (MEAC enrolla oldRot) |

### Estimación
**~450–550 líneas** (EstelaLib.cs) + ~80 si se quiere `EstelaTrack` en archivo propio.

---

## 3.2 PYRA LIB — la librería del fuego (`Content/VFX/PyraLib.cs`)

### Qué es
La librería del FUEGO 100% procedural: **rampas de color por temperatura (LUT)**,
**lenguas de llama** con parpadeo inconmensurable y punta viva, el **campo de brasas**
(técnica Doom Fire, recuperada de la envoltura v6.22 perdida) y **chispas/ascuas**
físicas. El humo lo pone BrumaFX (composición entre librerías, como hace el ecosistema).

### Contrato de diseño (API pública)

```csharp
namespace AethonMod.Content.VFX
{
    /// <summary>Tabla de fuego de la casa (37 niveles de temperatura).</summary>
    public static class PyraPalettes
    {
        /// <summary>Fuego noble: negro→granate→naranja→oro→blanco (37 colores).</summary>
        public static readonly Color[] SolarFire;
        /// <summary>Fuego frío: azul profundo→cian→blanco (para soles/arcos fríos).</summary>
        public static readonly Color[] ColdFire;
        /// <summary>Fuego maldito: verde→lima→blanco (mirra/abyss).</summary>
        public static readonly Color[] VoidFire;
        /// <summary>Muestreo de la tabla por temperatura 0..1 (con wrap suave).</summary>
        public static Color Sample(Color[] ramp, float temperature);
    }

    /// <summary>
    /// PyraLib — v6.25 — LA LIBRERÍA DEL FUEGO.
    ///
    /// Tres motores, un contrato:
    ///   · RAMP: el color es una TEMPERATURA muestreada en tabla (lección Everglow:
    ///     una rampa 1D por material; aquí las tablas son CÓDIGO, no PNG).
    ///   · LENGUAS: la llama es una columna de quads con temperatura que SUBE hacia
    ///     la base y una PUNTA que vaga (paseo aleatorio con reversión a la media —
    ///     la técnica del filamento de BoltCore), parpadeo por dos senos
    ///     inconmensurables, viento del mundo opcional.
    ///   · CAMPO: rejilla de brasas con PROPAGACIÓN (técnica Doom Fire, validada en
    ///     la envoltura de fuego v6.22: cada celda enfría 1 nivel al subir y vaga
    ///     ±1 columna) — para zonas de fuego persistente.
    ///
    /// CONTRATO (idéntico al de BrumaFX): los métodos dibujan en el lote ABIERTO
    /// que el llamador tenga (aditivo para fuego LUMINOSO; alpha para fuego que
    /// OCLUYE, p. ej. brasas negras de carbón). TODO determinista por semilla.
    /// </summary>
    public static class PyraLib
    {
        // ============ LA LENGUA (la unidad de llama) ============

        /// <summary>
        /// DIBUJA UNA LENGUA de fuego de <paramref name="height"/> px anclada en
        /// <paramref name="basePos"/>: temperatura MÁXIMA en la base, decreciente
        /// hacia la punta con erosión de ruido (la llama se come en grumos, como
        /// el humo — BrumaNoise.Erode); la PUNTA vaga (±width·0.3) con reversión
        /// 0.3 (lección de auto-corrección de Everglow); parpadeo de altura
        /// 0.85..1.15 por dos senos inconmensurables; viento del mundo opcional.
        /// </summary>
        /// <param name="temperature">0..1 — nivel base de la tabla.</param>
        /// <param name="wind">Px/s de empuje lateral (0 = sin viento).</param>
        public static void Tongue(SpriteBatch batch, Vector2 basePos, float height,
            float width, Color[] ramp, float temperature, int seed, float time,
            float intensity = 1f, float wind = 0f);

        /// <summary>Racimo de 3-5 lenguas (una hoguera, un estallido).</summary>
        public static void Flame(SpriteBatch batch, Vector2 center, float radius,
            Color[] ramp, int seed, float time, float intensity = 1f);

        // ============ EL CAMPO DE BRASAS (propagación Doom Fire) ============

        /// <summary>
        /// DIBUJA UN CAMPO DE BRASAS de <paramref name="w"/>×<paramref name="h"/>
        /// celdas (celda = <paramref name="cellSize"/> px) alrededor de
        /// <paramref name="center"/>: rejilla de temperaturas 0..36 que PROPAGA
        /// (cada celda toma el valor de abajo−1 con desfase lateral por hash
        /// determinista — la técnica PSX clásica, aquí determinista y SIN
        /// textura dinámica: cada celda ES un quad pequeño con el SoftGlow y el
        /// color muestreado en la tabla). Las SEMILLAS (focos) las define el
        /// llamador con SeedCell(...).
        /// </summary>
        public static void EmberField(SpriteBatch batch, Vector2 center, int w, int h,
            float cellSize, Color[] ramp, int seed, float time,
            float intensity = 1f);

        /// <summary>Enciende/apaga la celda (x,y) de un campo por semilla (foco).</summary>
        public static void SeedCell(int seed, int x, int y, int temperature);

        // ============ LAS CHISPAS / ASCUAS (física de verdad) ============

        /// <summary>
        /// PAQUETE DE CHISPAS determinista (no spawnea: DEVUELVE ParticleData[]
        /// listo para ParticleManager.Spawn): ascuas con GRAVEDAD suave, DRAG
        /// 0.97, parpadeo de color por temperatura decreciente (la ascua se APAGA:
        /// blanco→naranja→rojo→gris humo), rotación por velocidad, muerte al
        /// tocar tile (opcional) o al agotar temperatura.
        /// </summary>
        public static void Sparks(Vector2 center, Vector2 burstDir, int count,
            Color[] ramp, int seed, out ParticleData[] outParticles);

        // ============ LA LUZ (el fuego ilumina) ============

        /// <summary>Luz de mundo muestreada (color de la tabla a L=0.85, cada 80 px).</summary>
        public static void Light(Vector2 pos, float radius, Color[] ramp,
            float temperature, float strength = 1f);
    }
}
```

### Técnicas internas (parámetros concretos)

1. **La tabla de 37 colores** (formato Doom Fire, colores de la CASA): nivel 0 negro
   absoluto → 8 granate (112,0,58 = `VoidQueen.InnerDark`) → 18 naranja (255,138,60 =
   `CrimsonCourt.Knot`) → 26 oro (255,195,85 = SolarGold) → 32 casi-blanco
   (255,250,235) → 36 blanco. `Sample()` con interpolación LINEAL entre niveles (las
   tablas del ecosistema son texturas de 1×N con filtro LINEAR — aquí el equivalente
   numérico).
2. **La lengua**: 5–9 quads apilados (paso `height/8`); temperatura de cada tramo
   `T·(1 − f·0.55)` más `BrumaNoise.Erode` (la llama se DESGARRA, no se desvanece);
   anchura `width·sin(f·π+0.3)^0.6`; la punta = paseo aleatorio determinista por
   tramo con reversión a la media 0.3; **parpadeo** de altura por `1 + 0.15·sin(t·7.1)
   + 0.05·sin(t·17.3)` (inconmensurables). Triple capa: velo ×1.7 alpha 0.35 · cuerpo
   alpha 0.70 · núcleo blanco ×0.35 en el 40% inferior (donde el fuego es más caliente).
3. **El campo**: rejilla típica 26×38 (la validada en v6.22), `cellSize` 3–4 px;
   actualización un tick sí un no (30 Hz de simulación, 60 de dibujado — imperceptible
   y mitad de coste); cada celda: `temp[y][x] = max(0, temp[y+1][x + hashOffset] − 1)`
   con `hashOffset ∈ {−1,0,+1}` por `Hash01(seed, x, y, tick)` (determinista).
   Render: solo celdas con temp > 3 (negro = no dibujar) → quad `cellSize×1.6` con
   SoftGlow y color de tabla; en la práctica ~300–600 quads activos por campo.
4. **Las chispas**: gravedad 0.12 (más lenta que vanilla 0.2 — las ascuas flotan),
   drag ×0.97, `EmitLight` on, `ColorShift` NO (la temperatura se muestrea por tick —
   más vivo), vida 30–90 ticks, rotación = `velocity.ToRotation()` (lección
   BloomCircleParticle).
5. **Integración con viento del mundo**: `wind = Main.windSpeedCurrent × K` opcional
   (lección Everglow `velocity += wind×0.4`).

### Casos de uso en AethonMod

- **La envoltura de fuego perdida** (v6.22→v6.24): el cosmético "abrazo de fuego" se
  re-crea sobre `EmberField` en 1 tarde en vez de re-escribir el campo a mano.
- **Nova de los soles** (StyleNova ya dibuja FireRing): la nova gana lenguas radiales
  `Tongue` orientadas hacia fuera (el frente de espaciotiempo QUE LLEVA EL FUEGO).
- **Sinfonía** (arma v6.24): chispas musicales con `ColdFire`.
- **Eclipse Primordial**: brasas del disco de acreción con `VoidFire`.
- **Armas V20**: cualquier arma de fuego gana `Flame` en el cañón + `Sparks` al golpear.

### Riesgos y mitigaciones

| Riesgo | Mitigación |
|---|---|
| El campo puede parecer "juego de 1993" | La tabla con colores de la CASA + núcleo blanco + erosión de ruido; solo usar el campo para ZONAS (suelos persistentes), lenguas para actores |
| Coste del campo (quads) | Cap de celdas activas (600); simular a 30 Hz; matar celdas temp≤3 |
| Fuego que "flota" sin anclaje | La lengua ancla la BASE exacta (`basePos` es el foco); smoke test con VLM |
| Exceso de rojo-naranja saturado | Las tablas de la casa ya evitan el RGB puro (granate→oro); `ColdFire`/`VoidFire` para variedad |

### Estimación
**~420–480 líneas** (PyraLib.cs + PyraPalettes.cs ~80). El campo puede ir en archivo
propio (`PyraField.cs`, ~140 L) si se prefiere.

---

## 3.3 ONDA LIB — la librería de las ondas (`Content/VFX/OndaLib.cs`)

### Qué es
La librería de ONDAS EXPANSIVAS de impacto: anillos con frente/retaguardia, borde roto
por ruido, falloff correcto, y el paquete de impacto completo (onda + **sacudida de
cámara** + **destello de pantalla**) — lo que el ecosistema premium hace en TODO golpe
importante y que nosotros solo hacemos en las muertes de los cósmicos.

### Contrato de diseño (API pública)

```csharp
namespace AethonMod.Content.VFX
{
    /// <summary>Cómo muere el grosor del frente a lo largo de la vida.</summary>
    public enum OndaFalloff
    {
        /// <summary>Lineal (impactos físicos).</summary>
        Linear,
        /// <summary>Cuadrático (energía que se disipa — el estándar).</summary>
        Quadratic,
        /// <summary>Lento (mágico: la onda se resiste a morir).</summary>
        Slow,
    }

    /// <summary>
    /// OndaLib — v6.25 — LA LIBRERÍA DE LAS ONDAS DE IMPACTO.
    ///
    /// El CONTRATO DE IMPACTO: una onda de verdad son DOS anillos (frente brillante
    /// + retaguardia ×0.5 desfasada), el borde ROTO por ruido (nunca un círculo
    /// perfecto — lección del ShockwaveShader del ecosistema), el GROSOR que nace
    /// fino y engorda mientras el alpha muere, y el PAQUETE: sacudida de cámara +
    /// destello de pantalla opcionales.
    ///
    /// CONTRATO (idéntico al de la casa): dibujo en el lote ABIERTO (aditivo);
    /// `progress` lo lleva el llamador (0..1, típicamente timeLeft invertido) —
    /// la librería NO guarda estado de ondas. TODO determinista por semilla.
    ///
    /// Se dibuja con la textura Ring + RingQuadSize de VFXCore (2.174×radio).
    /// </summary>
    public static class OndaLib
    {
        // ============ LA ONDA ============

        /// <summary>
        /// DIBUJA UNA ONDA EXPANSIVA en <paramref name="center"/> con radio actual
        /// = <paramref name="progress"/>·maxRadius: frente fino-brillante +
        /// retaguardia gruesa-tenue (×0.5 alpha, +6% de radio), borde roto por
        /// N segmentos con radio vivo (±8% por hash), aberración cromática opcional
        /// (franjas R/B al ±1.8% — el idioma de las ondas cósmicas de la casa).
        /// </summary>
        /// <param name="progress">0..1 avance de la vida de la onda.</param>
        /// <param name="maxRadius">Radio FINAL en px.</param>
        /// <param name="thickness">Grosor máximo del frente en px (4..14 recomendado).</param>
        /// <param name="chromatic">True = añade las franjas R/B.</param>
        public static void Shock(SpriteBatch batch, Vector2 center, float progress,
            float maxRadius, Color color, float intensity, int seed,
            float thickness = 10f, OndaFalloff falloff = OndaFalloff.Quadratic,
            bool chromatic = false);

        /// <summary>PULSO simple: UN anillo que crece y muere (el RingPulse de
        /// ParticlePresets, con borde roto y falloff correcto — 4× mejor por el
        /// mismo precio de 2 quads × segmentos).</summary>
        public static void Pulse(SpriteBatch batch, Vector2 center, float progress,
            float maxRadius, Color color, float intensity, int seed);

        /// <summary>
        /// ONDA DE SUELO: medio-anillo pegado al piso + POLVO que levanta
        /// (paquete determinista para ParticleManager) — el impacto físico
        /// de las armas cuerpo a cuerpo.
        /// </summary>
        public static void Ground(SpriteBatch batch, Vector2 center, float progress,
            float maxRadius, Color color, float intensity, int seed,
            out ParticleData[] dustKick);

        // ============ EL PAQUETE DE IMPACTO (cámara y pantalla) ============

        /// <summary>
        /// SACUDIDA de cámara gestionada: registra un impulso (strength px,
        /// decay cuadrático, vida en ticks) en el ACUMULADOR central que un
        /// ModSystem aplica SOLO en Pre-ScreenPosition (un único punto del frame,
        /// sin las 3 implementaciones ad-hoc actuales que se pisan).
        /// Máx 2 sacudidas simultáneas (la mayor gana).
        /// </summary>
        public static void Kick(float strengthPx, int durationTicks = 12,
            float directionAngle = -1f);   // -1 = omnidireccional

        /// <summary>
        /// DESTELLO de pantalla: velo aditivo full-screen con alpha decayente
        /// (0.18 → 0 en 8 ticks), dibujado por el mismo sistema en
        /// PostDrawTiles_OverTiles — SIN render targets (quad pantalla-completa
        /// sobre el batch del mundo, camino seguro de la casa).
        /// </summary>
        public static void Flash(Color color, float strength = 0.18f,
            int durationTicks = 8);

        // ============ HELPERS ============

        /// <summary>Curva de expansión: fast-out (1−(1−t)^2.2) — sale como una
        /// explosión y frena al final (así se lee "energía").</summary>
        public static float Expansion(float progress);

        /// <summary>Curva de falloff de alpha por tipo.</summary>
        public static float Falloff(float progress, OndaFalloff type);
    }
}
```

### Técnicas internas (parámetros concretos)

1. **Expansión fast-out**: `r(t) = maxRadius·(1−(1−t)^2.2)` — velocidad inicial
   enorme que frena (física de explosión leída por el ojo). La onda de WoTE usa
   `Lerp(r, ideal, 0.075)/frame` — equivalente exponencial; documentamos la nuestra.
2. **Doble anillo**: frente con `Ring` + `RingQuadSize(r)` alpha `I·(1−t)^1.6`
   (muerte cuadrática — estándar del ecosistema), retaguardia a `r·1.06` alpha ×0.5
   y grosor ×1.9 (la "onda de presión" detrás del frente de choque).
3. **Borde roto**: el anillo se dibuja en **12–16 segmentos** de arco (arco =
   `ArcRing` de StormLib ya sabe hacerlo) con radio `r·(1 + 0.08·(Hash01(seed,i,tick)−0.5))`
   → el círculo vivo del ShockwaveShader (offset de ruido ×8 rad del shader original,
   traducido a variación de radio por segmento).
4. **Grosor del frente**: `thickness·(0.4 + 0.6·t)` engorda mientras muere el alpha
   (la onda se "gasta" ensanchándose) — p. ej. 4px→14px en una de 300px.
5. **Aberración cromática**: 2 segmentos extra desplazados ±1.8% del radio con los
   canales R/B recortados (la receta de las franjas del CosmicShockwave v5.96).
6. **Sacudida**: acumulador estático `Kick { strength, age, dir }`; el ModSystem
   (OndaSystem, ~60 L) aplica `offset = dir·strength·(1−age/duration)^2 · sin(age·2.1)`
   (frecuencia de sacudida ~13 Hz decae cuadrático) en un punto ÚNICO del frame
   (modificando `Main.screenPosition` antes del cálculo de los draws de mundo — el
   mismo patrón que ya usan los 3 proyectivos actuales, pero UNA sola vez y
   combinado); cap de amplitud 14 px.
7. **Destello**: quad pantalla-completa en el lote aditivo del sistema, alpha
   `strength·(1−age/dur)` — cero render targets, cero shaders.

### Casos de uso en AethonMod

- **TODOS los impactos de las armas V20/cósmicas**: `Shock` + `Kick` + `Flash` en un
  solo call-site de 3 líneas.
- **Reemplazo gradual de CosmicShockwave**: los estilos ligeros (0/1) migran a
  `OndaLib.Shock(chromatic:true)` + fuente de lente; el proyectil queda SOLO para las
  muertes de jefes (donde el daño en banda lo justifica).
- **Armas cuerpo a cuerpo**: `Ground` con dustKick.
- **Muerte de NPC genérica**: `Pulse` barato en vez de nada.

### Riesgos y mitigaciones

| Riesgo | Mitigación |
|---|---|
| El shake central pisando el de otros mods (Overhaul etc.) | Amplitud cap 14px + decay corto; documentar compatibilidad (Overhaul ya reescribe el pipeline — igual que ya pasa con la lente) |
| Momento del frame del Kick | MISMO patrón que ya usan Supernova/RuneSun/Sun (probado en casa); solo se CENTRALIZA |
| Flash demasiado frecuente → mareo | Presupuesto: máx 1 flash activo; strength 0.18 por defecto; cooldown 30 ticks |
| Segmentos = más draw calls | 12–16 segmentos × 2 anillos = 32 quads/onda — despreciable frente a un puff de BrumaFX (núcleo+14 blobs) |

### Estimación
**~300–360 líneas** (OndaLib.cs) + OndaSystem.cs ~70 L.

---

## 3.4 CHISPALIB — evaluada y DESCARTADA como librería nueva (extensión en su lugar)

**Motivo:** ya tenemos `ParticleManager` (4000 cap, bitmask, culling, 2 lotes) —
construir "ChispaLib" sería duplicar el motor. Lo que falta son **calidades**, y van
mejor como extensión del sistema existente (Task futura "ParticleManager v2"):

1. **Determinismo**: sustituir `Main.rand` de los presets por `Hash01(seed,…)` (regla
   de la casa — hoy la misma explosión es distinta en cada máquina; en MP eso es
   asimetría visual, no bug, pero contradice nuestra identidad).
2. **Componente Drag**: `p.Velocity *= (1 − drag)` con drag 0.97–0.99 (lo tienen
   TODOS los mods del ecosistema; es EL parámetro que separa "chispa viva" de
   "meteorito"). ~15 líneas.
3. **Muerte contextual**: bit `DieInLiquid` (consulta `Main.tile[x,y].LiquidAmount`,
   lección Everglow) y `BounceOnTile` ya está reservado como flag — implementarlo
   (rebote ×−0.2 + vida −10).
4. **Presets curados** (el verdadero hueco): `Chispas()` (las de PyraLib), `Ascuas()`,
   `Escombros()` (gravedad + bounce + rotación), `Motas()` (flotación + curl noise de
   BrumaNoise). ~120 líneas de presets.
5. **Easings**: mini-catálogo `Eases.QuadOut/QuadIn/CubicOut/BackOut` (~30 L, lo
   usan ParticleManager, EstelaLib y OndaLib por igual — podría vivir en VFXCore).

**Estimación de la extensión:** ~200 líneas sobre archivos existentes. **Prioridad
media** (los presets de PyraLib ya cubren el 60% del caso de uso "chispas").

## 3.5 DISTORSIÓN generalizada — DESCARTADA por ahora

Ya tenemos la mejor distorsión del ecosistema local (BlackHoleLensSystem). Generalizarla
a "heat haze barato" (estilo metaballs de WoTE) exige: un 3er pase de pantalla, un RT
extra fijo a pantalla, y otro shader .fxc. **Beneficio visual medio** (el heat haze es
sutil), **riesgo medio-alto** (tocar el pipeline de FNA que tan caro nos costó domar en
v5.89). DECISIÓN: diferir hasta que exista un efecto que LO NECESITE (p. ej. un arma de
calor). El contrato de puertas queda anotado: `BlackHoleLensSystem.RegisterSource(pos,
radius, strength, pass)` ya existe de facto para los cósmicos — si algún día se
generaliza, ese es el punto de extensión.

## 3.6 METABALLS y CLIMA — DESCARTADAS

- **Metaballs**: alto esfuerzo, sin caso de uso en la hoja de ruta (la lente cubre el
  "wow" gravitatorio).
- **Clima de pantalla**: no hay biomas/eventos con clima en el roadmap cercano;
  `MistBand` cubre la parte estética; cuando llegue, es un spawn-perimetral sobre
  ParticleManager + viento global (~150 L, no librería propia).

---

## 3.7 MEJORAS CONCRETAS A BRUMA FX (petición explícita del usuario)

De `smoke_research_v616` §H (nunca materializadas) + hallazgos de esta investigación:

| # | Mejora | Detalle | Coste |
|---|---|---|---|
| B1 | **Flipbook fBm evolutivo** | Hornear 6 frames × 8 variantes (48 texturas 128² ≈ 3 MB VRAM) con offsets de dominio en potencias exactas de 2 (`uv·4 + t·dir`, t en pasos 1/6) — anti-phasing garantizado; `frame = (int)(time·fps + seed) % 6`. El humo MORFA de verdad en vez de solo rotar | ~120 L sobre BrumaBrushes |
| B2 | **Escalera LOD de texturas** | Hornear además 64 y 256 (además de 128) y elegir por radio (`OctavesForRadius` ya existe): borde con densidad de texel constante — nunca escalar >2× | ~60 L |
| B3 | **`FlushNonPremultiplied()` en VFXCore** | El lote gemelo para humo QUE OCLUYE (BlendState.NonPremultiplied + texturas RGB-blanco/alfa — ya son así): masa y sombra de verdad. Decision 1 del §H de v616, sigue sin existir | ~45 L |
| B4 | **Tinte por iluminación del mundo** | `col *= Lighting.GetColor(tile)` con piso 0.15 en Puff/Cloud/MistBand (la lección vanilla: el humo pertenece al mundo) | ~25 L |
| B5 | **Viento del mundo** | `wind += Main.windSpeedCurrent·K` en Column/MistBand (hoy solo senos internos) | ~10 L |
| B6 | **`Puff` con `velocity`-smear real** | Ya existe; falta exponerlo en `Column` (el viento actual no estira) | ~15 L |

**Total de la mejora de bruma: ~275 líneas** — es la continuación natural de la
investigación de humo y la respuesta directa a la mitad de la petición del usuario.

---

# BIBLIOGRAFÍA (≥20 mods + fuentes)

**Código leído directamente (descompilado/clonado en `/tmp/research/`):**
1. **WoTE — Wrath of the Empress** v1.0.3 (LucilleKarma) — EoL rework + framework Luminance; metaballs de distorsión, ShockwaveShader, post-processing de la Emperatriz.
2. **Coralite** (CoraIite/Coralite-Mod) + lib **InnoVault** — sistema PRT, TrailParticle, FireParticle flipbook.
3. **Everglow** (CycloneClub/Everglow) — VFXBatch (8192 verts), CommonVFXDusts con 12+ HeatMaps LUT, pipelines encadenados.
4. **Lunar Veil** (AzaleaThePhantomWitch/LunarVeil, linaje Stellamod) — ParticleSystem (cap 500), TrailRenderer (ribbon TriangleStrip), ScreenTarget/Handler, Waters, Easings.
5. **MEAC — 原版内容重置 demo** (yiyang233) — .tmod parseado + ILSpy (253 archivos); shaders Fire/RainbowLaser/Sun/Screen-EoLWarp; afterimages con squash.
6. **Terraria vanilla 1.4.4.9** (decompilada) — Main (pipeline de dibujado/captura/filtros), Dust (atlas+LOD), DelegateMethods (láseres en strip), Projectile (oldPos).
7. **Luminance** (lib gráfica del autor de WoTE, vía decompile) — ManagedScreenFilter, MetaballType, AtlasManager, EasingCurves.

**Estudiados en informes previos del proyecto (repos leídos entonces):**
8. **Calamity Mod** (GitHub LucilleKarma/CalamityMod; wiki — config de cap de partículas) — flipbooks HeavySmoke 7×6×80px, 3 lotes.
9. **Starlight River** (GitHub ProjectStarlight/StarlightRiver) — partículas GPU por vertex buffer + ~30 shaders.
10. **ParticleLibrary** (GitHub SnowyStarfall, GPL-3) — usada por Redemption, SoA y LunarVeil.

**Web (búsquedas de esta tarea, JSONs en `busquedas_web/`):**
11. **Thorium Mod** (thoriummod.wiki.gg — config visual) — cerrado, referencia de settings.
12. **Fargo's Souls Mod** (Steam Workshop / forums.terraria.org).
13. **Spirit: Reforged** (GitHub Leemyy/SpiritMod — espejo público; Steam Workshop Collection).
14. **Mod of Redemption** (usuario de ParticleLibrary, confirmado en GitHub).
15. **Shadows of Abaddon** (usuario de ParticleLibrary, confirmado en GitHub; wiki Fandom).
16. **The Stars Above / Stellamod** (GitHub + Steam Workshop).
17. **Terraria Overhaul** (forums.terraria.org + Steam Workshop) — partículas/clima/screen FX masivos.
18. **Ancients Awakened** (GitHub AncientsAwakened/AAModEXAI).
19. **Polarities** (GitHub nicobrownmath/Polarities).
20. **Elements Awoken** (GitHub ElementsAwokenTeam + Steam Workshop).
21. **Aequus** (terrariamods.wiki.gg + forums).
22. **Verdant** (GitHub GabeHasWon/VerdantMod).
23. **VFX+** (Steam Workshop — mod de efectos del taller).
24. **tModLoader ExampleMod + docs** (docs.tmodloader.net — Filter class, Projectile.oldPos para trails).
25. **Doom Fire — fabiensanglard.net** "How DOOM fire was done" (la técnica del campo 37 colores).
26. **JangaFX/Diablo 3 VFX, IQ, VFXDoc, vfxlabs** — vía `research/smoke_research_v616/` (los cimientos teóricos ya digeridos).

---

# RESUMEN EJECUTIVO

1. **Los 3 huecos que más impacto×esfuerzo dan:** ESTELAS (EstelaLib ~500 L), FUEGO
   (PyraLib ~450 L, recupera la técnica perdida de v6.22), ONDAS DE IMPACTO con
   shake+flash (OndaLib ~380 L). Las tres son empaquetados de tecnología que ya
   sabemos hacer (Strand/BoltCore/texturas/LUT) sobre el contrato de la casa.
2. **ChispaLib NO**: el motor ya existe (ParticleManager); extenderlo
   (determinismo+drag+agua+presets ≈ 200 L).
3. **Distorsión/metaballs/clima**: diferir — la lente ya gana ese round; sin caso de
   uso que lo exija.
4. **BrumaFX (petición explícita):** flipbook evolutivo + LOD + lote NonPremultiplied
   + tinte por iluminación ≈ 275 L de mejora medible (§3.7).
5. **Orden sugerido de implementación:** EstelaLib → OndaLib (rápida, se nota en TODO)
   → mejora BrumaFX → PyraLib → extensión ParticleManager.

**FIN DEL ANÁLISIS — ninguna línea de código se ha modificado.**
