# INFORME — Técnicas de LUZ en Wrath of the Empress (WoTE)
**Task 38-a · Investigación para AethonMod v6.22 · Fecha: sandbox actual**
Repo analizado: `/tmp/research/wote` (WoTE v1.0.3, autor Lucille Karma, tModLoader 1.4.4 / .NET 8)
Dependencia clave: **Luminance** (librería gráfica del mismo autor, DLL externa) → `PrimitiveRenderer`, `ManagedShader`, `Particle`, `MetaballType`, `ManagedRenderTarget`, `AtlasManager`.

> OBJETIVO: extraer CÓMO hace WoTE sus efectos de luz (lances, rayos, arcoíris, bloom, partículas) para **re-implementarlos de forma propia** con nuestra pila (SpriteBatch + quads aditivos + texturas procedurales + shaders propios). Todo el código de abajo está **parafraseado en pseudocódigo** — no es copia literal.

---

## 0) Hallazgo estructural nº1: la luz casi NUNCA es `Lighting.AddLight`

En TODO el mod hay **UNA sola llamada** a `Lighting.AddLight` (en `Content/NPCs/EoL/EmpressOfLight.cs:448`):

```
cada frame de AI del boss:
    Lighting.AddLight(NPC.Center, Vector3.One * NPC.Opacity)   // luz blanca 0..1 según opacidad
```

**Lección maestra:** el "brillo" de WoTE es 100% RENDER EMISIVO (sprites aditivos por capas), no iluminación del mundo. Los proyectiles de luz no emiten luz al mundo en absoluto. La sensación de "luz" viene de: capas aditivas + bloom multicapa + colores vivos ciclando + partículas + screen shake. La única luz real es un halo ambiental blanco constante del jefe (intensidad = opacidad del NPC, actualizada cada frame).

Además, casi todos los proyectiles anulan `GetAlpha()` → devuelven `Color.White * Opacity` (o `lightColor * Opacity`): **son full-bright, inmunes a la iluminación del mundo**. La luz no los apaga.

---

## 1) INVENTARIO DE ARCHIVOS ESTUDIADOS (todos leídos completos)

### Proyectiles de luz (núcleo de la misión)
| Archivo | Qué hace |
|---|---|
| `Content/NPCs/EoL/Projectiles/LightLance.cs` | Lanza de luz (daga). Telegraph de 2100px + bloom 2 capas + estela de 30 copias del sprite. Textura vanilla `ProjectileID.FairyQueenLance`. |
| `Content/NPCs/EoL/Projectiles/DazzlingDeathray.cs` | Rayo de la muerte de fase 2. Primitive trail + shader + `Collision.LaserScan` + partículas perpendiculares. |
| `Content/NPCs/EoL/Projectiles/PrismaticBolt.cs` | Bolt prismático guiado. Trail sinusoidal + shader de paleta + núcleo cruz de glow (4 draws de `Extra[98]`). |
| `Content/NPCs/EoL/Projectiles/StarBolt.cs` | Bolt estrella (misma receta que PrismaticBolt, otra paleta, sin núcleo). |
| `Content/NPCs/EoL/Projectiles/AcceleratingRainbow.cs` | Arcoíris que acelera exponencialmente (×1.029/frame). Trail con RainbowTrailShader. |
| `Content/NPCs/EoL/Projectiles/RainbowRiftArrow.cs` | Flecha-rift: frena (×0.7), el hue gira +0.2/frame, deja metaball de distorsión y estalla en PrismaticBurst. |
| `Content/NPCs/EoL/Projectiles/SpinningTerraprisma.cs` | Espada prismática orbital con fake-3D (escala oscilante), bloom 2 capas, halo de 4 espadas, trail post-dash. |
| `Content/NPCs/EoL/Projectiles/EmpressOrbitingTerraprisma.cs` | Variante con **ZPosition real** (división perspectiva 1/(z+1)) y órbita elíptica (1, 0.45). |
| `Content/NPCs/EoL/Projectiles/PrismaticBurst.cs` | Explosión: shockwave full-screen por shader + 8 partículas/frame en arco creciente. |
| `Content/NPCs/EoL/Projectiles/DazzlingPetal.cs` | Pétalo-lanza giratorio (sun dance): trail que se ENROLLA (oldRot twirl) + transformación a "fuego". |
| `Content/NPCs/EoL/Projectiles/ConvergingMoonlight.cs` | Chorro de luz lunar que espirala hacia el boss (π/2 de rotación) con posiciones relativas. |
| `Content/NPCs/EoL/Projectiles/MagicCircle.cs` | Círculo mágico 3D: RenderTarget 1024² + Quaternion Slerp + cilindro 240 segmentos + blur gaussiano. |
| `Content/NPCs/EoL/Projectiles/LanceWallTelegraph.cs` | Aviso de muro de lanzas: línea vertical 4000px con falloff cuadrático simple. |
| `Content/NPCs/EoL/Projectiles/GlowingAurora.cs` | Aurora: 25 copias apiladas de la textura vanilla `HallowBossDeathAurora` con offsets sinusoidales. |
| `Content/NPCs/EoL/Projectiles/HomingLacewing.cs` | Mariposa guiada con halo arcoíris de 6+6 copias fantasma + trail sinusoidal. |

### Partículas
| Archivo | Qué hace |
|---|---|
| `Content/Particles/BloomCircleParticle.cs` | Partícula = pixel/quad con bloom trasero (2 draws: bloom grande + núcleo). Aditiva. |
| `Content/Particles/BloomPixelParticle.cs` | Píxel 1×1 con bloom trasero ×0.04 y opción de homing con giro suave. |
| `Content/Particles/PrismaticLacewingParticle.cs` | Mariposa-partícula con 3 frames de aleteo, halo de 4 copias y desaceleración por tramos. |
| `Content/Particles/Metaballs/DistortionMetaball.cs` (+`System`) | Metaballs de refracción: se dibujan aditivas a un target y un filtro de pantalla distorsiona el mundo. |

### Paletas / color
| Archivo | Qué hace |
|---|---|
| `Content/NPCs/EoL/Palettes/EmpressPalettes.cs` | Registro de 5 SETS de paletas (Default/Día/Eclipse/LunaSangre/Enfurecida) elegidas por prioridad+condición. |
| `Content/NPCs/EoL/Palettes/EmpressPaletteSet.cs` | Paleta = `Vector4[]` por tipo de efecto + `MulticolorLerp` (lerp cíclico por índice). |
| `Content/NPCs/EoL/Palettes/EmpressPaletteType.cs` | Enum de tipos: ButterflyAvatar, Wings, Phase2Dress, PrismaticBolt, StarBolt, RainbowArrow, LacewingTrail, DazzlingPetal. |

### Shaders (¡fuente HLSL .fx incluida en el repo!)
| Archivo | Qué hace |
|---|---|
| `Assets/.../Shaders/Primitives/PrismaticBoltShader.fx` | Trail prismático: paleta en uniform array, ruido de "corte", glow 1/d, hue a lo largo del trail. |
| `Assets/.../Shaders/Primitives/LacewingTrailShader.fx` | Variante (hue ×0.7, fade `pow(1-x,2)`, glow 0.16/d). |
| `Assets/.../Shaders/Primitives/RainbowTrailShader.fx` | Arcoíris con "tip" ruidoso, inner glow con `pow(x,3.5)`, hue = ruido+glow+posición. |
| `Assets/.../Shaders/Primitives/DazzlingDeathrayShader.fx` | Rayo: bump cuadrático + pulso sin(t×54) + 1/d pulsante. |
| `Assets/.../Shaders/Primitives/TerraprismaDashTrailShader.fx` | Estela de dash: glow `pow((1-x)*0.3/d, 3)` + doble ruido de color. |
| `Assets/.../Shaders/Primitives/DazzlingPetalShader.fx` | Pétalo→fuego: dos modos mezclados por `fireColorInterpolant`, oscurecido por streaks. |
| `Assets/.../Shaders/Primitives/ConvergingMoonlightShader.fx` | 5 taps de ruido suavizado → chorro de rayos finos. |
| `Assets/.../Shaders/Primitives/LanceWallTelegraphShader.fx` | Falloff transversal: `color * pow(1 - dist_y*2, 2)`. |
| `Assets/.../Shaders/Objects/ShockwaveShader.fx` | Onda de choque screen-space: anillo 1/distancia con borde ruidoso. |
| `Assets/.../Filters/MetaballDistortionFilter.fx` | Refracción: canal G = ángulo, A = fuerza, offset ×0.0051. |
| `Assets/.../OverlayEffects/EmpressWingGradientShader.fx` | Colorea un sprite gris usando sus canales XY como índice de paleta + tiempo. |
| `Assets/.../OverlayEffects/EmpressDressShader.fx` | Anillos arcoíris radiales: hue = r^0.75 − t×0.7 + dist×0.96, pixelado 2px. |

### Render del jefe y escena
| Archivo | Qué hace |
|---|---|
| `Content/NPCs/EoL/Rendering/EmpressOfLight.Rendering.cs` | PreDraw del boss: backglow 4 capas, afterimages arcoíris, blur direccional 18 taps, teleport ring, proyección mariposa. |
| `Content/NPCs/EoL/Rendering/EmpressOfLightTargetManager.cs` | RT 672² del boss (se dibuja una vez, luego se post-procesa). |
| `Content/NPCs/EoL/EmpressOfLight.cs` | AI central + la única `AddLight`. |
| `Content/NPCs/EoL/SpecificManagers/EmpressSky.cs` | Cielo custom: luna con bloom 3 capas, nubes/m niebla por shader, lluvia custom 2048 partículas. |
| `Content/NPCs/EoL/Behaviors/Attacks/Phase2/EmpressOfLight.EventideLances.cs` | Ataque de lances: 5/side @ 16.7°, arco iris de flecha, brillo del arco (flare que rota 2π). |
| `Content/NPCs/EoL/Behaviors/Attacks/Phase2/EmpressOfLight.PrismaticOverload.cs` | Ataque del círculo mágico (sincronía con beat musical exacto ±3 frames). |
| `Content/NPCs/EoL/Behaviors/Attacks/EmpressOfLight.SpinSwirlRainbows.cs` | Espiral de arcoíris: 1 rainbow/frame en dirección que rota 2π/25 frames. |
| `Content/NPCs/EoL/Behaviors/Attacks/Phase2/EmpressOfLight.BeatSyncedBolts.cs` | Bolts al beat + metaball 32px en cada disparo. |
| `Content/NPCs/EoL/Lacewing.cs` / `GracedWings.cs` | Crítico mariposa: polvo vanilla 261 recolorado con `hslToRgb(rand, 1, 0.5)` + mismo halo 6+6. |
| `Common/ShapeCurves/ShapeCurve.cs` | Polilíneas transformables (para dibujar formas custom). |

### Atlas propio (`Assets/Atlases/MainAtlas.json`) — texturas procedurales medidas
- **BloomCircle.png: 200×200** (gradiente radial — LA textura de bloom universal)
- **AngularBloomRing.png: 190×190** (anillo para metaballs)
- **Lacewing.png: 24×70** (3 frames de 24×23 aprox)
- **Pixel.png: 1×1**
Además usa texturas de Luminance (`WavyBlotchNoise`, `TurbulentNoise`, `DendriticNoiseZoomedOut`, `BloomCircleSmall`) y **vanilla** (`Extra[98]`, `ExtrasID.FlameLashTrailShape`, `MagicMissileTrailShape`, `MagicMissileTrailErosion`, `HallowBossGradient`).

---

## 2) TÉCNICAS EXPLICADAS (pseudocódigo parafraseado)

### T1 — Paletas MulticolorLerp: el corazón del color prismático
Paleta = array de 4-9 colores (Vector4). Función de muestreo cíclica:
```
MulticolorLerp(palette, t):
    t = t mod 0.999
    i = int(t * n); f = frac(t * n)
    return lerp(palette[i], palette[(i+1) mod n], f)     // ¡el último enlaza con el primero!
```
**Paleta por tipo de efecto** (no un solo arcoíris global). Valores exactos del set Default:
- PrismaticBolt (5): magenta(207,0,151) · crema(255,220,154) · cian(0,255,255) · azul(35,175,255) · violeta(144,61,196)
- LacewingTrail (6): (189,126,255) · (255,174,240) · (255,44,196) · (148,38,187) · (10,105,187) · (109,200,252)
- StarBolt (7): (255,147,176) · (255,162,252) · (177,48,209) · (255,79,196) · (255,72,75) · (255,156,67) · (255,216,184)
- RainbowArrow (9): hslToRgb(0, .125, .25, .375, .5, .625, .75, .75, .875) con S=1, L=0.5
- Wings (4): HotPink · White · Aqua · (240,243,184)

Los SETS se eligen por prioridad (Eclipse > BloodMoon > Daytime > Default) y cambian TODO (colores de proyectiles, cielo, nubes, luna, ¡incluso sprites del cuerpo!).

### T2 — Fórmulas de color cycling (velocidades medidas, 60 fps)
| Efecto | Fórmula de hue | Velocidad efectiva |
|---|---|---|
| LightLance | `(hueSemilla − Time×0.01) mod 1` | 0.6 hue/s por lanza (cada lanza distinta semilla) |
| PrismaticBolt (núcleo) | `(identity×0.23 + GlobalTime×0.5) mod 1` | 0.5 hue/s + offset por proyectil |
| PrismaticBoltShader (a lo largo del trail) | `u×1.3 − localTime×0.4` (localTime = t + id×1.8) | 0.4 hue/s + 1.3 ciclos a lo largo de la estela |
| LacewingTrailShader | `u×0.7 − localTime×0.45` (localTime = t×1.56) | 0.7 hue/s |
| RainbowTrailShader | `(ruido×0.2 + glow×0.2 + u×0.08)×espectrum + hueOffset` | espectrum = 1.0 (rainbow) / 1.3 (rift) |
| TeleportRing | `hslToRgb(t×2 mod 1, 1, 0.95)` | 2 hue/s (¡el más rápido!) |
| Backglow del boss | `MulticolorLerp(Wings, t×0.5)` | 0.5 hue/s |
| Afterimages del boss | `MulticolorLerp(RainbowArrow, (i+5)/10 − t×0.2)` | 0.2 hue/s hacia atrás |
| DazzlingPetal | `t×0.13 + sin(ángulo)×0.08` | 0.13 hue/s + wobble espacial |
| Aurora | `(sin×0.5+0.5)×0.6 + (t mod 10)/10` | ciclo de 10 s |
| MagicCircle lances | `(Time/30 + rand×0.19) mod 1` | 2 hue/s escalonado |
| SpinSwirlRainbows | `AITimer/20 mod 1` | 3 hue/s (1 por proyectil disparado) |

**Regla de oro:** hue base ALEATORIO/por-identidad en el spawn + drift temporal lento (0.2–0.6 hue/s) + variación espacial a lo largo de la estela (×0.7–×1.3). NUNCA un arcoíris uniforme sincronizado.

### T3 — Bloom por capas aditivas (la "luz" sin Lighting)
Patrón universal: pila de N quads con la textura BloomCircle (gradiente radial 200×200), tamaño decreciente y opacidad creciente:

**Backglow del boss (4 capas):**
```
escala:  4.1   → 2.85  → 1.5   → 0.8
alpha:   0.25  → 0.67  → 0.7   → 1.0     (× backglowOpacity global 0.5..0.03)
capa 3 usa color arcoíris ciclando (MulticolorLerp(Wings, t×0.5))
```
**Luna (3 capas):** escalas 2 / 3.3 / 6 con alphas 0.6 / 0.31 / 0.15.
**LightLance (2 capas):** white×0.51 @ (2,1)×1.01 · hueShift(+0.05) @ (2,1)×0.6; alpha global = lerp(0.75→0.51, appear) × opacity.
**Terraprisma (2 capas):** color @ 1.0 con alpha=opacity² · color @ 1.3 con alpha=0.5×opacity².
**ButterflyProjection (2):** white×0.43 @ 6× · color×0.8 @ 9×.
**Núcleo del PrismaticBolt (¡sin textura propia!):** 4 draws de la textura vanilla `Extra[98]` formando CRUZ:
```
2 orientaciones (rot 0 y π/2) × 2 escalas: grande (0.8, 9) y pequeña (0.8, 3) ×0.6 interior ×0.5 alpha
pulso: escala ×= lerp(0.8, 1.2, cos01(2π×6×t))    // 6 Hz
```

### T4 — Trails prismáticos sinusoidales (la firma visual de WoTE)
Cada proyectil con `TrailingMode=2` y `TrailCacheLength=8..54` guarda `oldPos[]`. En el render:
```
perp = rot90(vel_normalizada) × remap(speed, 4→20, 12→90 px)
para cada punto i del trail:
    fase = sin(2π×(i/N) − GlobalTime×12 + identity)
    amplitud = inverselerp(0.01, 0.9, i/N)         // crece hacia la cola
    trailPos[i] = oldPos[i] + perp × fase × amplitud
```
→ la estela ONDEA en serpentina. El ancho:
```
width(ratio) = baseWidth × tipCut × slownessFactor
tipCut      = inverselerp(0.02..0.134, ratio)         // punta estrecha (o pow 0.6 en rainbows)
slowness    = remap(speed, 3→9, 0.18→1.0)             // ¡los lentos dejan estela FINA!
color(ratio)= lerp(White, Black, sin×0.5+0.5)          // máscara de brillo sinusoidal
```
El color blanco↔negro de la máscara lo multiplica el shader de paleta. **Resolución de renderizado: 25–54 segmentos.**

### T5 — Shaders de primitive (recetas HLSL extraídas)
Todos comparten: coordenada `y = (v−0.5)/anchoRelativo + 0.5` (0..1 transversal), `u` = a lo largo.

- **Glow de borde:** `glow = smoothstep(0.5, 0.25, distY + u) × k / distY` con k=0.3 (bolt) / 0.16 (lacewing) → centro brillante con caída ~1/distancia.
- **Corte por ruido (streak cull):** ruido desplazándose a `−0.54..−0.7/s` en U; `opacity = smoothstep(0.7, 0.5, u + lerp(−0.1, 0.6, ruido))` → la estela se ROMPE en fragmentos que viajan hacia atrás.
- **Bump cuadrático (rayo):** `bump(y) = y×(4−4y)` (máx 1 en el centro), `glow = bump² + smoothstep(0.5,0,distY×escala)/distY×0.2`.
- **Pulso del rayo:** `pulso = sin(localTime×54)×0.5+0.5; escala = lerp(1,2,pulso)` → 8.6 Hz (vibración agresiva).
- **Rainbow tip:** innerGlow = `pow(lerp(0.12,0.9,ruido)/(distY−ruido×0.3), 3.5)` — filamentos internos brillantes.
- **Brillo hacia la cabeza:** `× lerp(1, 2, u)` — la punta del trail es el doble de brillante.
- **Fade de cola:** `× pow(1−u, 2)` (lacewing), `× smoothstep(0, 0.5, u)` mezclando halo↔estela (bolt).
- **LanceWall:** `color × pow(1 − distY×2, 2)` — línea suave en UNA línea de shader.

### T6 — LightLance: anatomía completa de una lanza de luz
1. **Telegraph** (fase de aviso): línea de 2100 px de largo × 30 px de ancho, color paleta con drift de hue, alpha = `sqrt(1−appear)` — se desvanece justo cuando la daga materializa.
2. **Aparición de la daga:** interpolante `appear = inverselerp(TelegraphTime−16, TelegraphTime−3, Time)` (16 frames de materialización).
3. **Bloom bajo la daga** (2 capas, ver T3).
4. **Estela materializada sin trail-cache:** 30 copias del sprite dibujadas DETRÁS:
```
para i en 0..30:
    escala_i  = lerp(1.0 → 0.48, i/29)
    offset_i  = −dir × appear × i × escala_i × (speed×0.2)
    alpha_i   = paleta × appear × (1 − i/10)^1.6 × opacity × 1.8
```
5. **Daga principal:** color Wheat con alpha÷9 (casi aditivo), escala ×1.
6. Daña solo tras el telegraph; `ShouldUpdatePosition()=false` hasta entonces (flota estática apuntando).
7. Opacity global: `inverselerp(0,6,Time) × inverselerp(0,24,timeLeft)` — entra en 6 frames, sale en 24.
8. **Volea:** 10 lanzas (5 por lado a 16.7°, velocidad exterior lerp 18.6→9) + flecha central RainbowRift + PrismaticBurst + retroceso del boss de 120.

### T7 — DazzlingDeathray: el rayo de luz
```
AI cada frame:
    screenshake = inverselerp(25→0, Time)×7 + 1.6           // amaina con el tiempo
    girar hacia target: AngleLerp(0.02) suave
    largo: Collision.LaserScan(centro, dir, ancho=70, max=5000) → media de 10 muestras
           DeathrayLength += 120 (clamp a ideal)             // se EXTIEDE 120px/frame
    3 mariposas/frame (inicio 10% y punta del rayo, vel 30-250 rot±0.5)
    6 píxeles bloom/frame PERPENDICULARES (±π/2) a 8-19/speed px/s
render (primitive trail, 10 puntos láser, 54 segmentos, capa AfterNPCs pixelada):
    ancho(u) = 70×escala × sqrt(1 − inverselerp(0.07, 0.02, u)²)   // ¡boca circular tipo muzzle-flash!
    color(u) = alpha × inverselerp(1, 0.94, u)                      // desvanece la punta
    shader: pulso 8.6 Hz + bump² + 1/d + ruido wavy desplazado −t+id×0.383
    escala de ancho: inverselerp(0,28,Time)²  (se hincha cuadrático)
```

### T8 — Partículas de luz (sistema propio, todo aditivo)
**BloomCircleParticle** (la "chispa" universal):
```
update: vel ×= 0.96 · escala = inicial × cuadrática_OUT(1→0) · alpha = cuadrática_IN(inicial→0) sobre el 54% inicial de vida
        rotación = ángulo de la velocidad
draw:   bloom (BloomCircle) a escala×bloomFactor (1.5–2.0) con color BLOOM
        + núcleo a escala×1 con color propio
```
**BloomPixelParticle** (píxel 1×1 con halo ×0.04):
```
hasta 65% de vida: crece suavemente
después: alpha ×= 0.91 · escala ×= 0.96 · vel ×= 0.94 por frame
últimos 5 frames: alpha ×= 0.75 · escala ×= 0.75
opción homing: gira hacia destino con AngleLerp(0.014×inverselerp(0,120,t)) y acelera a 25+t×0.05
```
**PrismaticLacewingParticle** (mariposa animada):
```
alpha = bump(0, 0.03, 0.5, 1.0, ratio)     // aparece en 3% de la vida
deceleración por tramos: >50 px/s ×0.65 · >15 ×0.85 · resto ×0.93
rotación = lerp(actual, clamp(vel.x×0.02, ±0.46), 0.18)
frame de aleteo: cada 5 ticks (3 frames)
draw: 4 copias en cruz de 2px con color³ + copia central blanca
```
**Tasas de emisión medidas:** PrismaticBolt 1 cada 2 frames (si vel≥11) · StarBolt 2/frame espejadas (si vel≥12) · Deathray 3+6/frame · MagicCircle 2/frame + 6 lanzas/frame · PrismaticBurst 8/frame × 15 frames · SequentialDashes 1/frame × 120.

### T9 — Metaballs de distorsión (refracción de luz)
```
createParticle(centro, vel=0, tamaño 4–120, fuerza 0.75–2, decay 0.1–0.03, crecimiento 0.009–0.03)
update: vel.x ×= 0.97 · tamaño ×= 1+crecimiento · fuerza −= decay (kill a 0.01)
render: todas las instancias se pintan ADITIVAS con AngularBloomRing en un render target
        (el canal G del anillo codifica ángulo de refracción, A la fuerza)
filtro de pantalla: color_final = screen[uv + ángulo×fuerza×0.0051]
```
→ un "lente" de calor/burbuja donde pasa la luz. Se usa en cada dash, burst y rift.

### T10 — MagicCircle (magia de alto presupuesto)
- Se dibuja UNA VEZ a un RenderTarget 1024² (solo el primer círculo existente — patrón singleton).
- Contenido: círculo base + polígonos procedural de 3/5/6 lados (shader con `polygonSides`) + 5 mini-círculos orbitando a 436×escala + mariposa central.
- Rotación **3D real**: `Quaternion.Slerp` entre perspectiva frontal → picada (rotX 1.14 rad) → apuntando al jugador.
- Anillo-cilindro vertical: 240 segmentos, vértices con `cos(ángulo)` como pseudo-profundidad, textura strip que scrollea con `rotación×−0.75`.
- **Underglow con blur gaussiano:** 11 pesos = `gauss(i−5, σ=2)/13`, offset = escala×0.004 — se dibuja el RT desenfocado DETRÁS y nítido encima.
- Escala con easing ELÁSTICO (overshoot rebote) ×0.425 + ×0.25 al disparar.

### T11 — Fake 3D barato (2 técnicas)
1. **SpinningTerraprisma:** `escala = lerp(0.5, 1.5, sin01(ángulo_orbital))` — al orbitar, la espada se agranda al pasar "por delante". + órbita elíptica `(1, 1−squish)` con squish 0.25–0.55.
2. **EmpressOrbitingTerraprisma:** ZPosition = lerp(1.2, −0.25, sin01(ángulo)); `escala = base/(z+1)` (¡división perspectiva!) y se recupera a z=0 tras el dash.
3. **Boss Z:** opacity = lerp(1, 0.43, inverselerp(1, 2.6, z)) + desenfoque creciente con z (5 taps σ0.8, offset z×0.004).

### T12 — Afterimages y estelas del jefe
- **Cadena oldPos:** alpha = inverselerp(N, 1, i) × 0.8 × intensidad; **color arcoíris que DERIVA hacia atrás** (`MulticolorLerp(RainbowArrow, (i+5)/10 − t×0.2)`) — cada fantasma tiene un color distinto del arcoíris que se desplaza.
- Se activan por VELOCIDAD: `intensidad = inverselerp(32, 80, |vel|)` (Eventide) o (4,30) en SpinSwirl — la estela aparece sola al ir rápido.
- **Ilusión de teleport (25 instancias):** matrices de rotación 3D (X,Y,Z a velocidades distintas ×0.7/×0.1) transformando `Vector3.Forward` × 150 px + arrastre `−vel×i×0.23` — rémora de "copias girando en espiral".
- **Blur direccional** en el post-proceso del sprite del boss: 18 taps gaussianos σ=6 a lo largo de `vel × 0.0016 × blurInterpolant`.

### T13 — GlowingAurora (bosque de luz con UNA textura vanilla)
25 copias apiladas de `HallowBossDeathAurora`:
```
para i en 0..25:
    offset.x = sin(ciclo10s + π/2 + i/2) × (300 − i×3)
    offset.y = sin(ciclo10s×2 + π/3 + i) × 30 − i×3
    hue_i    = (sin×0.5+0.5)×0.6 + ciclo10s
    escala_i = creciente con i
    draw(textura, centro+offset, paleta(hue_i), rot=π/2 + sin×π/4×−0.3 + π×i, escala(3,6)×s)
alpha global × opacityA(0..1 fade) × opacityB(0.2..0.5)
color.A /= 4
```

### T14 — Shockwave full-screen (PrismaticBurst)
```
radius: lerp(actual, 780, 0.075/frame)      // explosión de 0.25 s
quad a PANTALLA COMPLETA con textura de ruido; shader:
    ángulo = ruido(uv + t×0.25)×8
    dist_al_anillo = |length((uv+offsetRuido)×pantalla − centro) − radius| / pantalla.x
    color = colorPaleta × opacity / dist × 0.041
```
+ 8 partículas/frame en el frente de onda con velocidad rotada por un arco que CRECE con el tiempo (efecto remolino).

### T15 — Feel: screen shake + música + accesibilidad
- **ScreenShake en TODO:** disparo 50 (dirección de disparo ±π×0.16), beat 6 (decay 0.5), muerte de lanza 7 puntual, rayo continuo 1.6–8.6, suspense ×4.1 creciente. El "golpe de luz" se VENDE con shake.
- **Sincronía musical exacta:** el PrismaticOverload se dispara cuando `|musicTimer − beat| ≤ 3 frames`; los BeatSyncedBolts disparan en `AITimer % shootRate == 0`. La luz baila con la música.
- **Fotosensibilidad:** config `PhotosensitivityMode` que reduce la opacidad del trail ×0.5 — accesibilidad integrada.
- **Pixelación:** los trails se renderizan a un target de baja resolución y se escalan (chunky retro) — capas BeforeProjectiles / AfterNPCs / AfterProjectiles.

---

## 3) TABLA FINAL — LECCIONES SINTETIZABLES PARA AETHONMOD
(pila propia: SpriteBatch + quads aditivos + texturas procedurales; shaders opcionales)

| # | Lección | Implementación concreta en nuestra pila | Parámetros medidos |
|---|---|---|---|
| 1 | **Cero AddLight, todo emisivo** | No llamar a Lighting.AddLight en proyectiles; solo 1 halo blanco por entidad grande si hace falta | `AddLight(center, White × opacity)` 1×/frame; el resto = sprites aditivos |
| 2 | **BloomCircle radial 200×200 como textura universal** | Generar proceduralmente (gradiente radial^2); usarlo para TODO: partículas, backglows, núcleos | PNG 200×200, alpha central 255→0; origen = centro |
| 3 | **Bloom = pila de 2–4 capas invertida** | Quad aditivo repetido: grande+tenue fuera, pequeño+brillante dentro | Boss: esc 4.1/2.85/1.5/0.8 con alpha 0.25/0.67/0.7/1.0 · Lance: (2,1)×1.01 y ×0.6 con alpha 0.75→0.51 · Luna: 2/3.3/6 con 0.6/0.31/0.15 |
| 4 | **Paleta cíclica MulticolorLerp** | Array de 4–9 colores + lerp por índice con WRAP (último↔primero); una paleta por tipo de efecto | PrismaticBolt: (207,0,151),(255,220,154),(0,255,255),(35,175,255),(144,61,196) · StarBolt: 7 rosas/naranjas · RainbowArrow: 9×hsl(S1,L0.5) |
| 5 | **Hue drift lento + semilla por-entidad** | `hue = (semilla + t×0.2..0.6) mod 1`; semilla = rand en spawn o id×0.23 | Drift: 0.2–0.6 hue/s (nunca >2 salvo énfasis puntual) · variación espacial ×0.7–×1.3 a lo largo de la estela |
| 6 | **Full-bright con GetAlpha** | `GetAlpha → Color.White × Opacity` en todo proyectil de luz | Entra en 6–12 frames, sale en 15–45 (según vida) |
| 7 | **Estela sinusoidal perpendicular** | trailPos[i] = oldPos[i] + rot90(dir)×remap(speed 4–20 → 12–90)×sin(2π·i/N − t×12 + id)×inverselerp(0.01,0.9,i/N) | N=8..54 puntos, remap 12–90 px, freq temporal 12 rad/s |
| 8 | **Ancho de trail con tip-cut + slowness** | width = base × inverselerp(0.02..0.134, u) [o ^0.6] × remap(speed 3–9 → 0.18–1.0) | Bolt 28px · Star 35 · Rainbow 32 · Lance 24 hitbox |
| 9 | **Estela materializada por copias de sprite** | 30 draws del sprite detrás: escala lerp(1→0.48), offset i×escala×(speed×0.2), alpha (1−i/10)^1.6 ×1.8 | 30 copias, decay potencia 1.6, factor velocidad 0.2 |
| 10 | **Telegraph de línea de luz** | Línea 2100×30 px color paleta, alpha sqrt(1−appear), ancho ∝ opacity; desaparece al materializar el arma | 2100 px de largo, 16 frames de aparición |
| 11 | **Glow de borde 1/distancia** | En shader: smoothstep(0.5,0.25,distY+u)×k/distY con k=0.16–0.3; alternativa SpriteBatch: 2 quads cruzados | k=0.3 bolt / 0.16 lacewing; brillo cabeza ×lerp(1,2,u) |
| 12 | **Núcleo cruz (4 draws de glow, sin textura propia)** | `Extra[98]`-like: 2 rotaciones (0, π/2) × 2 escalas (0.8,9)/(0.8,3), interior ×0.6 y alpha ×0.5; pulso 6 Hz lerp(0.8,1.2) | Escalas (0.8,9) y (0.8,3); pulso cos01(2π×6t) |
| 13 | **Partícula bloom doble-draw** | Quad bloom (×1.5–2.0 de escala, color secundario) + quad núcleo (color propio); decay: vel×0.96, escala cuadrática-out, alpha cuadrática-in en 54% de vida | BloomFactor 1.5–2.0; vida 18–60 frames |
| 14 | **Píxel con halo y muerte acelerada** | 1×1 px + bloom ×0.04; tras 65% vida: alpha×0.91, esc×0.96, vel×0.94; últimos 5 frames ×0.75 | Vidas 20–45 frames; spawn 1–8/frame según efecto |
| 15 | **Halo fantasma arcoíris de 6 copias** | 6 draws del sprite en círculo con radio pulsante `sin(t×2.4+id×3)×12 (mín 4)` y color paleta(t+i/6) | 6 copias, radio 4–16 px, +6 fijas a 2 px en gris |
| 16 | **Fake 3D con escala sinusoidal** | escala = lerp(0.5,1.5,sin01(ángulo orbital)); órbita elíptica (1, 1−0.45); o división perspectiva 1/(z+1) con z∈[−0.25,1.2] | Radio orbital 245 px |
| 17 | **Rayo láser con LaserScan** | Extender largo +120 px/frame hasta media de 10 muestras de colisión; ancho con boca circular `sqrt(1−il(0.07,0.02,u)²)`; punta desvanecida inverselerp(1,0.94,u) | Max 5000 px, hitbox 70, 10 puntos de control, 54 segmentos |
| 18 | **Pulso rápido en el rayo** | Escala de glow × lerp(1,2,sin(t×54)×0.5+0.5) → 8.6 Hz | sin(t×54) |
| 19 | **Chorro convergente espiral** | offset = (destino−origen).rotado(π/2×(1−spin²))×spin con spin=pow(il(0,vida,t),1.3) | 0.8 s de vida, 20 puntos, ancho rand 36–100 |
| 20 | **Metaball de refracción** | Anillos aditivos en RT + filtro screen-space: offset uv = ángulo(G)×fuerza(A)×0.0051; tamaño crece 1–3%/frame, fuerza decae 0.01–0.03/frame | Tamaños 4–120 px |
| 21 | **Aurora apilada** | 25 copias de textura alargada con offsets sinusoidales dobles (freq 1 y 2), rot alternando π, escala (3,6) | Amplitud X 300−3i, Y 30−3i, ciclo 10 s |
| 22 | **Shockwave full-screen** | Quad pantalla con shader anillo: `color/dist×0.041`, borde ruidoso `ruido×8 rad`; radio lerp→780 @0.075 | Radio máx 780, vida 15 frames |
| 23 | **Underglow gaussiano** | Dibujar el RT del efecto desenfocado detrás (11 taps σ=2/13, offset 0.004×escala) y nítido encima | 11 taps, σ=2 |
| 24 | **Afterimages con arcoíris que deriva hacia atrás** | Copias de oldPos con color paleta((i+5)/10 − t×0.2), alpha il(len,1,i)×0.8; umbral por velocidad | Activa si vel 32–80 |
| 25 | **Estela que se enrolla (twirl)** | Re-rotar los control points por `wrapAngle(oldRot[i] − rot)` lerp por intensidad de flare | 30 puntos |
| 26 | **Círculos mágicos 3D** | RT 1024² + Quaternion.Slerp (rotX 1.14) + cilindro 240 seg con cos como depth + polígonos shader N-lados | 240 segmentos, offset 380×escala |
| 27 | **Screen shake vende la luz** | Shake en cada evento: beat 6, disparo 50, rayo 1.6–8.6 continuo, burst 7 | decay 0.33–0.6 |
| 28 | **Sincronía musical** | Disparar en beat exacto (±3 frames de musicTimer); VFX con fase de identidad (`id×0.23..1.9`) para desincronizar copias | offsets id ×0.23/×0.374/×0.383/×1.8 |
| 29 | **Sonidos vanilla EoL** | Item122 (disparo), Item160/163/164/165 (varios), Item162 en loop cada 20 frames | — |
| 30 | **Accesibilidad fotosensible** | Multiplicador global ×0.5 en trails si config activa | ×0.5 |

---

## 4) RECETAS EXPRESS para v6.22 (qué copiar primero)

1. **"Light Lance" Aethon** = T3(bloom 2 capas) + T9(estela 30 copias) + T4/T5(hue drift 0.01/frame) + telegraph T10. Todo con 2 texturas: BloomCircle procedural + sprite de daga.
2. **"Prismatic Bolt" Aethon** = trail sinusoidal T7 con quads: dibujar la estela como quads orientados por `velocity` con ancho decreciente (T8) y color por paleta T4/T5; núcleo = cruz de 4 quads T12 con pulso 6 Hz.
3. **"Deathray" Aethon** = quads consecutivos a lo largo del láser con ancho T17; en modo shader: bump²+1/d+pulso 8.6 Hz; partículas perpendiculares T8/T13 a 6/frame.
4. **Partículas** = T13/T14 en nuestro sistema data-oriented (ya existe): añadir "bloom trasero" y decay exponencial 0.91/0.96/0.94 tras 65% de vida.
5. **Backglow de armas grandes** = T3 pila de 4 capas 4.1/2.85/1.5/0.8.

## 5) NO-copiables / a evitar
- No copiar texto/artwork del repo (licencia del autor: código propio re-implementable, arte NO).
- El sistema de Luminance (PrimitiveRenderer/ManagedShader) es infraestructura ajena: reimplementar SOLO las fórmulas y parámetros de arriba, con nuestra VFXCore.
- Los .fx son HLSL compilado ps_3_0: nuestras versiones deben reescribirse (tenemos ya experiencia con dxc del task anterior).

— Fin del informe. 31 archivos leídos completos, 12 shaders HLSL analizados, ~60 parámetros numéricos extraídos.
